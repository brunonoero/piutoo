using Piootoo.Shared.MarketData;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Strategies.Easy.Engines;

/// <summary>
/// Base comune ai motori Unger portati da EasyLanguage.
///
/// <para><b>Perché esiste.</b> Le prime 44 strategie sono state tradotte una per una, a mano, e
/// hanno riprodotto gli stessi errori 40 volte: <c>OrderType</c> mai impostato (ogni
/// <c>next bar at ... stop</c> diventava un market sulla barra corrente), <c>ValidFromUtc</c>
/// assente, stop loss in denaro scritti sul campo che l'engine legge come punti, uscite decise a
/// runtime invece che dichiarate all'ingresso. Questa classe rende quegli errori non
/// esprimibili: un ingresso si costruisce solo passando dai metodi qui sotto, che impongono
/// tipo d'ordine, validità e specifica di uscita completa.</para>
///
/// <para><b>Idempotenza rispetto al contratto.</b> Stop, target e breakeven si dichiarano in
/// <b>denaro sul contratto di riferimento</b>, esattamente come <c>setstopcontract</c> +
/// <c>setstoploss(N)</c> dell'originale. La conversione in punti avviene una volta sola nel
/// confine di esecuzione, usando <c>InstrumentRegistry</c>. La strategia non sa, e non deve
/// sapere, se sta girando su future, mini, micro o CFD: quella differenza la assorbono i punti
/// e il moltiplicatore di quantità dell'account.</para>
/// </summary>
public abstract class EasyEngineBase : StatelessEasyStrategyBase
{
    // ------------------------------------------------------------------ parametri di sessione

    /// <summary>
    /// <b>Il confine di sessione della strategia, con il proprio fuso.</b> Governa gli OHLC
    /// <c>d0..d5</c> di <see cref="BuildSessionOhlc"/>, il secchio di
    /// <see cref="MaxEntriesPerSession"/> e la chiusura di fine sessione.
    ///
    /// <para>È una proprietà <b>della strategia</b>, non del simbolo. Prima il fuso veniva dedotto
    /// da <c>InstrumentSpec.SessionTimeZone</c>, cioè una strategia su GC non poteva dichiarare una
    /// sessione diversa da quella che il registro attribuiva a GC. Le sorgenti originali potevano
    /// permetterselo perché ogni sorgente girava su un simbolo solo; qui no.</para>
    ///
    /// <para>Con <see cref="ZonedWindow.TimeZoneId"/> a <c>null</c> il fuso resta quello del
    /// registro: è la compatibilità per le classi non ancora migrate, non la forma consigliata.</para>
    ///
    /// <para><b>È pubblica</b> per la stessa ragione di <see cref="SessionAnchorOverrideReason"/>:
    /// il confine su cui una strategia taglia le sessioni deve potersi leggere senza aprire il
    /// sorgente — la scheda oraria del catalogo la mostra, ed è così che un ancoraggio sbagliato
    /// si vede invece di restare in silenzio.</para>
    /// </summary>
    public ZonedWindow Session => _session ??= ResolveSession();

    /// <summary>
    /// L'ancoraggio del simbolo, o quello dichiarato da <see cref="OverrideSessionAnchor"/>.
    ///
    /// <para>La forma e' sempre quella della ricerca — <c>(ancoraggio, fine giornata)</c>, giornata piena a
    /// partire dall'ancoraggio — perche' e' l'unico modello di sessione che il sistema esegue. La
    /// sessione <b>di borsa</b>, dove le barre fuori orario non appartengono a nessuna sessione, e'
    /// un modello diverso e non e' supportata: nessuna strategia la usa, e costruirla ora
    /// significherebbe far rinascere la seconda fonte di verita' che il calendario elimina.</para>
    /// </summary>
    private ZonedWindow ResolveSession()
    {
        if (_sessionAnchorOverride is { } overridden)
            return ZonedWindow.ResearchSession(overridden);

        return ZonedWindow.ResearchSession(
            MarketCalendarRegistry.Current.Get(Symbol).SessionStart);
    }

    /// <summary>
    /// Sposta l'ancoraggio di sessione di <b>questa</b> strategia, dichiarando perche'.
    ///
    /// <para><b>Quando serve.</b> Quasi mai: l'ancoraggio e' una proprieta' dello strumento e il
    /// calendario lo dichiara per simbolo. Serve se un run di ricerca ha tagliato le sessioni a
    /// un'ora diversa da quella con cui lo strumento e' registrato — cioe' se la strategia e' stata
    /// <i>trovata</i> su una segmentazione diversa. In quel caso riprodurla e' il porting corretto,
    /// e non dichiararla sarebbe l'errore.</para>
    ///
    /// <para><b>Il motivo e' obbligatorio</b> perche' un override senza motivo scritto e'
    /// indistinguibile da una distrazione, e questa e' la sola cosa che, sbagliata, sposta il
    /// confine di sessione senza produrre alcun messaggio.</para>
    ///
    /// <para>Gli override sono elencati in <c>SessionAnchorOverrideTests</c>: uno nuovo fa fallire
    /// il test finche' non viene messo in lista, cosi' nasce da una decisione e non da un merge.</para>
    /// </summary>
    /// <param name="anchor">Orario di inizio sessione nell'orologio della ricerca.</param>
    /// <param name="reason">Perche' questa strategia non segue l'ancoraggio del proprio simbolo.</param>
    protected void OverrideSessionAnchor(TimeOnly anchor, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                $"{GetType().Name}: un override dell'ancoraggio di sessione deve dichiarare il " +
                "motivo. Senza, e' indistinguibile da una distrazione — ed e' la sola cosa che, " +
                "sbagliata, sposta il confine di sessione senza produrre alcun messaggio.",
                nameof(reason));
        }

        _sessionAnchorOverride = anchor;
        SessionAnchorOverrideReason = reason.Trim();
        _session = null;
        _grid = null;
        _clock = null;
        _windowClock = null;
    }

    /// <summary>
    /// Perche' questa strategia non segue l'ancoraggio del proprio simbolo. Null = lo segue, che e'
    /// il caso normale. E' pubblico perche' l'override deve vedersi senza leggere il codice.
    /// </summary>
    public string? SessionAnchorOverrideReason { get; private set; }

    /// <summary>
    /// <b>La finestra operativa, con il proprio fuso.</b> È <c>start_hour</c>/<c>end_hour</c> dei
    /// run di ricerca, che li scrivono <b>sempre nell'orologio della ricerca</b> qualunque sia il
    /// simbolo — vedi <see cref="ZonedWindow.ResearchTimeZone"/>. Dichiararla qui è ciò che elimina
    /// la conversione a mano verso l'ora di borsa (meno sette ore per NQ, meno sei per GC), che è
    /// esatta solo fuori dalle settimane in cui l'ora legale americana ed europea non sono
    /// allineate.
    ///
    /// <para><c>null</c> significa "non dichiarata": i motori ricadono sul fuso della sessione,
    /// che è il comportamento storico.</para>
    ///
    /// <para><b>Si legge da fuori, si scrive solo da dentro</b>: la dichiara il costruttore della
    /// strategia, ma la scheda oraria del catalogo deve poterla mostrare senza riflessione.</para>
    /// </summary>
    public ZonedWindow? TradingWindow
    {
        get => _tradingWindow;
        protected set
        {
            _tradingWindow = value;
            _windowClock = null;
        }
    }

    private ZonedWindow? _session;
    private SessionGrid? _grid;
    private TimeOnly? _sessionAnchorOverride;
    private ZonedWindow? _tradingWindow;

    /// <summary>
    /// Orario di inizio sessione, nell'orologio della ricerca. <b>Sola lettura</b>: lo dichiara
    /// il calendario del simbolo, non la strategia. Per spostarlo esiste
    /// <see cref="OverrideSessionAnchor"/>.
    ///
    /// <para><b>Non e' l'orario di borsa della sorgente EasyLanguage.</b> Un <c>SessBegin = 1700</c>
    /// e' la riapertura Globex in ora di Chicago e appartiene a un modello di sessione diverso:
    /// non c'e' nessuna aritmetica che lo porti a un ancoraggio, e cercarne una e' l'errore. Vedi
    /// <c>docs/domini/porting-da-report-sweep.md</c> §2.1.</para>
    /// </summary>
    protected TimeOnly SessionStart => Session.Start;

    /// <summary>
    /// Orario di fine sessione. Sola lettura, come <see cref="SessionStart"/>; vale
    /// <see cref="ZonedWindow.EndOfDay"/> per le sessioni a giornata piena della ricerca.
    /// </summary>
    protected TimeOnly SessionEnd => Session.End;

    /// <summary>Contratti dichiarati dalla strategia, prima di sizing e conversione account.</summary>
    protected int Contracts = 1;

    // ------------------------------------------------------------------ overnight e overweek

    /// <summary>
    /// <c>intraday_only</c> del motore di ricerca: la posizione viene chiusa alla fine della
    /// sessione della strategia. Dichiarato qui una volta sola invece che in ogni motore, perche'
    /// e' anche cio' da cui il catalogo deriva <see cref="Holding"/>: sei copie della stessa
    /// variabile erano sei semantiche libere di divergere, e lo avevano gia' fatto.
    /// </summary>
    protected bool IntradayOnly = true;

    /// <summary>
    /// Vero quando questo motore chiude davvero a fine sessione. I motori che leggono
    /// <see cref="IntradayOnly"/> usano <see cref="SessionExitFromIntradayOnly"/>; quelli che
    /// dichiarano l'uscita in altro modo (BIAS, BIASW, MAC) lo lasciano falso e restano multiday.
    /// </summary>
    protected virtual bool AppliesSessionExit => false;

    /// <summary>
    /// Orario a cui l'uscita di sessione chiude la posizione. <c>null</c> = la fine della sessione
    /// (<see cref="SessionEnd"/>), che e' il comportamento del motore di ricerca e resta il default.
    ///
    /// <para><b>Perche' non basta <see cref="SessionEnd"/>.</b> La sessione della ricerca e' il
    /// giorno di calendario europeo, quindi per FDAX finisce alle 00:59 locali — <b>dopo</b> il
    /// rollover del broker (21:00 su ICS, 20:59 su FTMO). Una strategia dichiarata <i>intraday</i>
    /// paga percio' il finanziamento tutti i giorni: su <c>PT3B_FDAX_PCH_001_240</c> sono 879 trade
    /// su 1.317 e il 28% del lordo. Chiudere qualche ora prima non e' una taratura di comodo, e'
    /// togliere un costo che non compra nulla — la posizione resta aperta nelle ore in cui il
    /// future e' chiuso e quota solo il CFD.</para>
    ///
    /// <para><b>Non e' <see cref="SessionEnd"/> con un altro valore</b>, ed e' il motivo per cui e'
    /// un campo a se'. <see cref="SessionEnd"/> definisce l'arco su cui si ricostruiscono gli OHLC
    /// di sessione (<see cref="BuildSessionOhlc"/>), la chiave del limite di ingressi per sessione
    /// e i bucket: spostarlo cambierebbe i <i>pattern</i>, cioe' un'altra strategia. Questo campo
    /// tocca la sola deadline di chiusura.</para>
    ///
    /// <para><b>Deviazione dichiarata dal motore di ricerca.</b> <c>price_channel.py</c> ha il solo
    /// <c>exit_on_session_end</c> booleano e non conosce un'ora di uscita: una configurazione che
    /// valorizza questo campo non e' riproducibile dal motore Python. Vedi
    /// <c>docs/domini/ricerca-parametri.md</c>.</para>
    ///
    /// <para><b>La legge un punto solo, <see cref="WithSessionExit"/>, per tutti i motori.</b> Fino al
    /// 23/09/2026 la leggeva il solo <c>PriceChannelEngine</c>: il campo stava qui, quindi ogni motore
    /// lo ereditava e ogni classe poteva dichiararlo, ma sei motori su sette chiudevano su
    /// <see cref="SessionEnd"/> e basta. Impostare <c>ExitHour</c> su una trend following non faceva
    /// niente, in silenzio, e la griglia TF del 22/09 ha misurato 25 combinazioni credendo di
    /// misurarne 250 (<c>ricerca/nq-4h-tf-griglia-grossa.md</c>). Un motore che risolve la deadline
    /// da solo, senza passare da <see cref="WithSessionExit"/>, riaprirebbe il difetto:
    /// <c>SessionExitHourTests.EveryEngineResolvesTheSessionExitInOnePlace</c> lo impedisce.</para>
    /// </summary>
    protected TimeOnly? SessionExitTime;

    /// <summary>
    /// Applica al segnale l'uscita di sessione, se questo motore la prevede
    /// (<see cref="AppliesSessionExit"/>): la deadline cade a <see cref="SessionExitTime"/> quando e'
    /// dichiarata, altrimenti a fine sessione (<see cref="SessionEnd"/>), che e' il comportamento del
    /// motore di ricerca e resta il default.
    ///
    /// <para>Restituisce <c>null</c> quando l'ingresso nascerebbe <b>dopo</b> la propria ora di
    /// chiusura: con un'ora propria la finestra utile della sessione finisce li', e un ordine che non
    /// potrebbe stare aperto nemmeno un minuto non e' un ordine. Rimandare la deadline alla sessione
    /// dopo — il comportamento storico di <see cref="ResolveCloseAtUtc(DateTime, TimeOnly)"/> —
    /// trasformerebbe in overnight proprio la posizione che quell'ora esiste per chiudere prima della
    /// notte, e la <see cref="Holding"/> dichiarata non sarebbe piu' vera. Il chiamante accoda con
    /// <see cref="AddEntry"/>, che scarta il <c>null</c>.</para>
    /// </summary>
    protected TradeSignal? WithSessionExit(TradeSignal signal)
    {
        if (!AppliesSessionExit)
            return signal;

        var closeAt = ResolveCloseAtUtc(
            signal.ValidFromUtc!.Value,
            SessionExitTime ?? SessionEnd,
            rollToNextSession: SessionExitTime is null);

        if (closeAt is null)
            return null;

        signal.CloseAtUtc = closeAt;
        return signal;
    }

    /// <summary>
    /// Accoda un ingresso, se esiste. Il <c>null</c> e' l'ingresso che <see cref="WithSessionExit"/>
    /// ha scartato perche' nascerebbe dopo la propria ora di uscita: non e' un errore, e' un ordine
    /// che non deve nascere.
    /// </summary>
    protected static void AddEntry(List<TradeSignal> entries, TradeSignal? entry)
    {
        if (entry is not null)
            entries.Add(entry);
    }

    /// <summary>
    /// L'ora di uscita come la scrive la ricerca (<c>ExitHour</c>): un intero, l'ora piena
    /// nell'orologio della ricerca, con <c>-1</c> per "fine sessione". Vale per ogni motore, perche'
    /// <see cref="SessionExitTime"/> vale per ogni motore.
    /// </summary>
    protected static TimeOnly? ResearchExitHourOrOff(object value)
    {
        var hour = Convert.ToInt32(value);
        return hour < 0 ? null : new TimeOnly(hour, 0);
    }

    /// <summary>
    /// L'uscita di sessione dei motori che portano <c>intraday_only</c>, con la regola di parita'
    /// del motore di ricerca: <b>su D1 quell'uscita non viene applicata</b>, quindi una daily resta
    /// multiday anche dichiarando <c>intraday_only = 1</c>.
    ///
    /// <para><b>Perche' e' qui e non ripetuta nei motori.</b> Cinque motori su sei scrivevano
    /// <c>IntradayOnly &amp;&amp; TimeframeMinutes &lt; 1440</c> dentro il proprio corpo e il sesto
    /// (RBB) no: la stessa dichiarazione valeva o non valeva secondo il motore che la leggeva.
    /// Scritta una volta sola, la regola e' una — e resta una regola di parita' dichiarata, non una
    /// deduzione dal timeframe nascosta in un <c>&amp;&amp;</c>.</para>
    ///
    /// <para>Il ramo daily e' comunque inerte sul catalogo attuale: tutte e dieci le strategie a
    /// 1440 dichiarano gia' <c>IntradayOnly = false</c>, e
    /// <c>HoldingPolicyTests.LeStrategieDailyDelCatalogoNonDipendonoDallEsenzioneD1</c> impedisce
    /// che ne compaia una che si affida all'esenzione senza saperlo.</para>
    /// </summary>
    protected bool SessionExitFromIntradayOnly => IntradayOnly && TimeframeMinutes < 1440;

    /// <summary>
    /// La strategia dichiara l'uscita di sessione ma non la ottiene, perche' e' daily: resta
    /// multiday soltanto grazie alla regola di parita' di
    /// <see cref="SessionExitFromIntradayOnly"/>. Non e' un errore in se' — e' il comportamento del
    /// motore di ricerca — ma e' una tenuta decisa dal timeframe invece che dal report, e il test
    /// di conformita' la segnala perche' non accada per distrazione.
    /// </summary>
    public bool DependsOnDailySessionExitExemption =>
        IntradayOnly && TimeframeMinutes >= 1440 && AppliesSessionExitDeclared;

    /// <summary>Se questo motore userebbe <see cref="SessionExitFromIntradayOnly"/> a timeframe intraday.</summary>
    protected virtual bool AppliesSessionExitDeclared => false;

    /// <summary>
    /// Cosa la strategia vuole tenere. Derivata da <see cref="AppliesSessionExit"/>: chi chiude a
    /// fine sessione non tiene ne' la notte ne' il fine settimana, chi non chiude tiene entrambi
    /// finche' il piano glielo concede. Vedi <see cref="AccountHoldingPolicy"/> per la gerarchia.
    /// </summary>
    public virtual StrategyHolding Holding =>
        AppliesSessionExit ? StrategyHolding.Intraday : StrategyHolding.Multiday;

    /// <summary>
    /// Contenitore di ricerca: vedi <see cref="ITradingStrategy.IsResearchContainer"/>. Dichiarata
    /// qui come membro virtuale — e non lasciata al solo default dell'interfaccia — perche' un
    /// contenitore deve poterla scrivere con <c>override</c> accanto a simbolo e timeframe, dove
    /// chi legge la classe la trova.
    /// </summary>
    public virtual bool IsResearchContainer => false;

    // ------------------------------------------------------------------ orologio di borsa

    private SessionClock? _clock;
    private SessionClock? _windowClock;

    /// <summary>
    /// Converte l'istante della barra nell'ora di borsa dello strumento, ed e' l'unico punto da cui
    /// il motore puo' leggere l'ora di una barra.
    ///
    /// <para><b>Perche' non si legge <c>barTime.Hour</c>.</b> L'istante della barra e' UTC, gli
    /// orari della strategia sono in ora di borsa: confrontarli direttamente funziona solo se il
    /// feed e' per caso stampato nello stesso orologio degli orari dichiarati, e smette di
    /// funzionare quando il feed cambia o quando la stessa strategia gira in live su un feed
    /// diverso. E' successo: il feed @NQ di backtest e' in ora europea mentre il cBot consegna UTC
    /// vero, cioe' due confini di sessione diversi per la stessa classe. Vedi
    /// <c>docs/domini/orari-di-sessione-e-fusi.md</c>.</para>
    ///
    /// <para>Il fuso e' quello dichiarato da <see cref="Session"/>. Se la sessione non lo dichiara
    /// si ricade sul registro strumenti (<c>InstrumentSpec.SessionTimeZone</c>), dove un simbolo
    /// senza specifica verificata fallisce subito invece di scegliere un fuso a caso. L'istanza e'
    /// creata alla prima lettura, viene invalidata quando <see cref="Session"/> cambia, e non e'
    /// thread-safe come documentato in <see cref="SessionClock"/>: l'ipotesi e' una per strategia,
    /// che e' come l'engine la usa.</para>
    /// </summary>
    protected SessionClock Clock => _clock ??= ResolveClock(Session.Clock);

    /// <summary>
    /// Orologio della <see cref="TradingWindow"/>. È separato da <see cref="Clock"/> perché il
    /// confine di sessione e la finestra operativa vivono in orologi diversi: il primo nell'ora di
    /// borsa, la seconda in quella in cui la ricerca l'ha scritta. Quando la finestra non dichiara
    /// un fuso proprio, i due coincidono e il comportamento è quello storico.
    /// </summary>
    protected SessionClock WindowClock =>
        _windowClock ??= ResolveClock(_tradingWindow?.Clock ?? Session.Clock);

    /// <summary>
    /// Traduce l'orologio dichiarato da una finestra nel fuso vero, prendendolo dal <b>calendario
    /// del simbolo</b>: l'unico posto in cui un identificatore IANA compare verificato.
    ///
    /// <para>Un simbolo ne ha due e non sono intercambiabili — <c>1700</c> da una sorgente
    /// EasyLanguage sono le 17:00 di Chicago, <c>17</c> da un run di ricerca sono le 17:00 CET, sei
    /// o sette ore di differenza sullo stesso strumento. Quale dei due usare lo dice la finestra,
    /// non il simbolo: è una proprietà della sua provenienza.</para>
    ///
    /// <para>Un simbolo che il calendario non conosce fa fallire qui, e va bene: senza calendario
    /// non si sa in che orologio leggere alcun orario di quella strategia, e sceglierne uno a caso
    /// sposterebbe ogni confronto senza produrre un messaggio.</para>
    /// </summary>
    private SessionClock ResolveClock(InstrumentClock clock)
    {
        var calendar = MarketCalendarRegistry.Current.Get(Symbol);
        return new SessionClock(
            clock == InstrumentClock.Exchange ? calendar.ExchangeTimeZone : calendar.ResearchTimeZone);
    }

    // ------------------------------------------------------------------ specifica di uscita

    /// <summary>Perdita massima in denaro per contratto di riferimento. 0 = nessuno stop.</summary>
    protected int StopMoney;

    /// <summary>Target in denaro per contratto di riferimento. 0 = nessun target.</summary>
    protected int ProfitMoney;

    /// <summary>
    /// Stop in multipli dell'ATR delle sessioni chiuse, al posto di <see cref="StopMoney"/>.
    /// 0 = spento (vale il denaro fisso), che e' il default e il comportamento di sempre.
    ///
    /// <para><b>Perche' esiste (22/09/2026).</b> Uno stop in denaro fisso e' tarato sul regime del
    /// campione: 200 punti di FDAX nel 2022 a volatilita' bassa e nel 2026 a volatilita' doppia sono
    /// lo stesso numero e due stop diversi. La griglia NQ 15 lo ha mostrato: stop 4000 vince in
    /// campione e stop 1000 fuori, perche' la volatilita' e' cambiata. In ATR e' un parametro solo
    /// che vale in ogni regime — un parametro in meno da tarare e' selezione in meno.</para>
    ///
    /// <para><b>Come.</b> L'ATR e' quello a <see cref="AtrSessions"/> sessioni <b>chiuse</b>, lo
    /// stesso del filtro <c>DvolMin</c> (Python <c>session_atr(df, 14, shift=1)</c>): la sessione in
    /// corso non entra mai, che e' la Legge Zero. Il denaro per contratto si risolve <b>al momento
    /// del segnale</b> — ATR in punti × valore del punto × multiplo — e il segnale esce con
    /// <c>StopLossMoneyPerFutureContract</c> gia' numerico, come oggi: chi esegue, backtest o cBot,
    /// riceve uno stop in denaro e non sa ne' deve sapere da dove viene. Nessun cambio di contratto.
    /// Se l'ATR non e' ancora calcolabile (meno di 15 sessioni di storia) si ripiega sul denaro fisso.</para>
    ///
    /// <para>Non e' nel corso di Unger — che usa stop in denaro scelti sul plateau e l'ATR per la
    /// size (%Vol) — ma e' coerente con la sua struttura, e con la size a %f sullo stop da' un
    /// rischio in dollari costante per trade qualunque sia la fase: e' il %Vol scritto dalla parte
    /// dello stop. Deviazione dichiarata, come <c>ExitHour</c>.</para>
    /// </summary>
    protected decimal StopAtrMultiplier;

    /// <summary>Target in multipli dell'ATR delle sessioni chiuse. 0 = spento (vale <see cref="ProfitMoney"/>).</summary>
    protected decimal TargetAtrMultiplier;

    /// <summary>
    /// Sessioni chiuse su cui si misura l'ATR di <see cref="StopAtrMultiplier"/>: 14, come il
    /// filtro <c>DvolMin</c>. Fisso e non in griglia: ogni parametro in piu' e' selezione in piu'.
    /// </summary>
    protected const int AtrSessions = 14;

    /// <summary>Soglia di breakeven in denaro per contratto di riferimento. 0 = disattivo.</summary>
    protected int BreakEvenMoney;

    /// <summary>Trailing stop in denaro per contratto di riferimento. 0 = disattivo.</summary>
    protected int TrailingStopMoney;

    /// <summary>
    /// Massimo numero di fill per sessione. 0 = nessun limite. Viene dichiarato sul segnale e
    /// applicato dall'engine al fill, così uno stop non eseguito può essere riemesso.
    /// </summary>
    protected int MaxEntriesPerSession;

    /// <summary>Numero massimo di barre in posizione. 0 = nessun limite.</summary>
    protected int MaxBars;

    /// <summary>
    /// Giorni di calendario massimi in posizione (<c>MaxDaysInTrade</c>). 0 = nessun limite.
    ///
    /// <para>Nell'originale è un contatore incrementato al cambio di giornata e confrontato a ogni
    /// barra; qui diventa una deadline <c>CloseAtUtc</c> calcolata all'ingresso, così l'engine e
    /// il cBot la applicano senza interrogare la strategia.</para>
    /// </summary>
    protected int MaxDaysInTrade;

    /// <summary>
    /// Soglia di utile per contratto sotto la quale eseguire la chiusura a tempo. Null = chiusura
    /// incondizionata. Vedi <c>TradeSignal.TimeExitOnlyIfProfitBelowMoneyPerContract</c>.
    /// </summary>
    protected decimal? TimeExitOnlyIfProfitBelow;

    // ------------------------------------------------------------------ stato per barra

    // I due campi seguenti sono popolati per riflessione da StatelessEasyStrategyBase prima di
    // ogni valutazione, e la ricerca avviene per NOME: devono chiamarsi esattamente così.
    // Sono la lettura autorevole dello stato broker — la strategia non deve mai scriverli.

    /// <summary>Posizione corrente vista dall'engine: +1 long, -1 short, 0 flat.</summary>
    protected int _currentMP;

    /// <summary>Ingressi già eseguiti nella sessione corrente, forniti dall'engine.</summary>
    protected int _entriesToday;

    /// <summary>Alias leggibile di <see cref="_currentMP"/>.</summary>
    protected int CurrentMP => _currentMP;

    /// <summary>Alias leggibile di <see cref="_entriesToday"/>.</summary>
    protected int EntriesTodayCount => _entriesToday;

    // ------------------------------------------------------------------ helper di sessione

    /// <summary>
    /// Ricostruisce gli OHLC di sessione (d0..d5) e dice se la barra corrente apre una sessione
    /// nuova. Wrapper su <see cref="EasyLib.OHLCMulti5"/> con i parametri di sessione del motore.
    /// </summary>
    protected bool BuildSessionOhlc(OhlcvData[] data, DateTime barTime, out decimal[] ohlc) =>
        EasyLib.OHLCMulti5(Clock, SessionStart, SessionEnd, data, barTime, out ohlc);

    /// <summary>Orario di apertura della barra, letto in ora di borsa.</summary>
    protected TimeOnly TimeOfDay(DateTime barTime) => Clock.TimeOfDay(barTime);

    /// <summary>
    /// Riproduce l'etichettatura vecchia: la barra si confronta con le soglie usando la propria
    /// <b>apertura</b>, com'era prima dell'08/09/2026. Default <c>false</c>, cioè il comportamento
    /// corretto — chi dimentica di impostarlo ottiene quello giusto.
    ///
    /// <para>Serve a una cosa sola: rimettere a confronto un run archiviato con uno nuovo. Non è
    /// una scelta di porting — la strategia deve fare quello che faceva l'originale, e confrontare
    /// un'etichetta di apertura con una soglia tarata sulla chiusura è un difetto, non un'opzione.</para>
    /// </summary>
    public bool LegacyBarOpenLabels { get; set; }

    /// <summary>
    /// <b>Con quale etichetta la ricerca da cui viene questa strategia chiamava le barre.</b>
    /// <c>false</c> (default): sulla <b>chiusura</b>, come TradeStation e i run Python dei dossier di
    /// agosto e settembre da cui vengono le <c>PTS_*</c>. <c>true</c>: sull'<b>apertura</b>, come il
    /// feed Piootoo e come i run di <c>run-engine-v2/</c> (§2.6 del dossier: «le candele sono
    /// etichettate al loro inizio»), da cui vengono le <c>PT2_*</c>.
    ///
    /// <para><b>Perche' lo dichiara la strategia e non il run.</b> E' una proprieta' della
    /// <i>provenienza</i> dei numeri, come l'orologio della finestra: <c>start_hour = 12</c> in un run
    /// che etichetta sulla chiusura e' la barra 08:00-12:00, in uno che etichetta sull'apertura e' la
    /// 12:00-16:00, e sono due strategie diverse. Un run che contenga strategie delle due serie deve
    /// confrontare ognuna con l'etichetta della propria ricerca; un interruttore unico per run
    /// (<see cref="LegacyBarOpenLabels"/>, che resta per confrontare gli archivi) ne sposterebbe
    /// meta' di una barra. Riportare i numeri verbatim e dichiarare l'etichetta e' la stessa regola
    /// della finestra: mai convertire a mano.</para>
    ///
    /// <para>Si scrive nel costruttore, si legge da fuori: la scheda oraria del catalogo deve poter
    /// dire con quale etichetta una strategia legge i propri orari senza aprire il sorgente.</para>
    /// </summary>
    public bool ResearchLabelsBarsOnOpen { get; protected set; }

    /// <summary>
    /// L'etichetta davvero in uso: quella dichiarata dalla strategia, oppure l'apertura se il run
    /// chiede il confronto con gli archivi. E' l'unico punto che i motori interrogano.
    /// </summary>
    protected bool BarLabelsOnOpen => LegacyBarOpenLabels || ResearchLabelsBarsOnOpen;

    /// <summary>
    /// L'orario con cui questa barra va confrontata con le soglie dei parametri
    /// (<c>start_hour</c>, <c>end_hour</c>, pause, orari di uscita), letto sull'orologio di sessione.
    ///
    /// <para><b>Non è <see cref="TimeOfDay"/>.</b> Quello è l'orario di apertura della barra e serve a
    /// dire a quale <i>sessione</i> appartiene, che è una domanda diversa e ha già la sua risposta
    /// in <see cref="EasyLib"/>. Questo è il nome con cui la ricerca chiamava la stessa barra, e la
    /// regola vive in un punto solo: <see cref="SessionClock.BarLabelTime"/>, salvo che la strategia
    /// dichiari <see cref="ResearchLabelsBarsOnOpen"/>.</para>
    /// </summary>
    protected TimeOnly ParamTime(DateTime barTime) =>
        BarLabelsOnOpen ? Clock.TimeOfDay(barTime) : Clock.BarLabelTime(barTime, TimeframeMinutes);

    /// <summary>
    /// Come <see cref="ParamTime"/>, ma sull'orologio della <see cref="TradingWindow"/>. I due fusi
    /// non coincidono e non vanno riconciliati a mano: la finestra dichiara il proprio.
    /// </summary>
    protected TimeOnly WindowParamTime(DateTime barTime) =>
        BarLabelsOnOpen
            ? WindowClock.TimeOfDay(barTime)
            : WindowClock.BarLabelTime(barTime, TimeframeMinutes);

    /// <summary>
    /// Confronto di un orario con una finestra a estremi opzionali: <c>null</c> da un lato vuol
    /// dire "nessun limite" da quel lato, e senza limiti la finestra è tutta la giornata. Con
    /// <paramref name="inclusiveEnd"/> la fine è inclusa (semantica della ricerca), altrimenti
    /// esclusa (semantica <c>tw()</c>). Gli orari sono già sull'orologio giusto: qui non si converte.
    /// </summary>
    protected static bool InWindow(TimeOnly? start, TimeOnly? end, TimeOnly time, bool inclusiveEnd)
    {
        if (start is null && end is null)
            return true;

        var from = start ?? TimeOnly.MinValue;
        var to = end ?? ZonedWindow.EndOfDay;
        return inclusiveEnd
            ? EasyLib.TimeWindowInclusive(from, to, time)
            : EasyLib.TimeWindow(from, to, time);
    }

    /// <summary>
    /// Pausa intraday a estremi inclusi; una pausa senza uno dei due estremi, o invertita, è
    /// inattiva come nelle sorgenti EasyLanguage.
    /// </summary>
    protected static bool InPause(TimeOnly? pauseStart, TimeOnly? pauseEnd, TimeOnly time) =>
        pauseStart is { } from && pauseEnd is { } to && time >= from && time <= to;

    /// <summary>
    /// Converte un parametro storico espresso come intero <c>HHMM</c> (le sorgenti EasyLanguage
    /// e i report sweep lo scrivono così) nell'orario che rappresenta. È l'unico punto in cui
    /// quella codifica entra nel codice: da qui in poi esiste solo <see cref="TimeOnly"/>.
    /// </summary>
    protected static TimeOnly TimeFromLegacyHhmm(object value)
    {
        var hhmm = Convert.ToInt32(value);
        return new TimeOnly(hhmm / 100, hhmm % 100);
    }

    /// <summary>
    /// Un'ora della ricerca (<c>start_hour</c>/<c>end_hour</c>) come la scrive un report sweep: un
    /// intero 0..23, oppure la sentinella <b>-1</b> che significa "nessun limite da questo lato"
    /// (<c>OFF_SENTINELS</c> di <c>base.py</c>).
    ///
    /// <para>Serve a <c>Initialize</c>: senza, un parametro a -1 — che nelle griglie della ricerca e'
    /// il <i>primo</i> valore, cioe' il default di ogni sweep — costruirebbe un <c>TimeOnly(-1, 0)</c>
    /// e farebbe eccezione. Sta qui e non in ogni classe perche' e' la stessa conversione di
    /// <see cref="TimeFromLegacyHhmm"/>, per l'altra codifica.</para>
    /// </summary>
    /// <param name="value">Il valore grezzo del parametro.</param>
    /// <param name="off">L'estremo da usare quando il parametro e' spento: inizio o fine giornata.</param>
    protected static TimeOnly ResearchHourOrOff(object value, TimeOnly off)
    {
        var hour = Convert.ToInt32(value);
        return hour < 0 ? off : new TimeOnly(hour, 0);
    }

    /// <summary>
    /// Valuta la <see cref="TradingWindow"/> dichiarata sull'orologio che essa dichiara.
    /// Restituisce <c>null</c> quando la strategia non la dichiara: in quel caso il motore ricade
    /// sul proprio percorso storico, che confronta i propri campi sull'orologio di sessione.
    ///
    /// <para>Gli estremi sono <b>inclusi</b> e il confronto è sull'orario pieno, come
    /// <c>time_window</c> del motore Python: la barra esattamente su <c>end_hour:00</c> entra
    /// nella finestra.</para>
    /// </summary>
    protected bool? InDeclaredWindow(DateTime barTime) =>
        TradingWindow is { } window
            ? EasyLib.TimeWindowInclusive(window.Start, window.End, WindowParamTime(barTime))
            : null;

    /// <summary>
    /// Giorno della settimana nella convenzione pandas (0 = lunedì … 4 = venerdì), letto
    /// sull'<b>orologio della finestra operativa</b>.
    ///
    /// <para>È lì che va letto, non in ora di borsa: <c>day_filter</c> del motore Python lavora
    /// sull'indice del dataframe, cioè sull'orologio in cui il feed è stampato, lo stesso in cui
    /// la ricerca ha scritto <c>skip_day</c>. Un giorno letto nell'orologio sbagliato cambia
    /// giorno nel mezzo della sessione serale americana.</para>
    /// </summary>
    protected int PythonWeekday(DateTime barTime) =>
        ((int)(BarLabelsOnOpen
            ? WindowClock.SessionDay(barTime)
            : WindowClock.BarLabelDay(barTime, TimeframeMinutes)).DayOfWeek + 6) % 7;

    /// <summary>
    /// Giorno della settimana nella convenzione EasyLanguage <c>dayofweek()</c>:
    /// 0 = domenica … 6 = sabato. Coincide con <see cref="DayOfWeek"/> di .NET, ma va detto
    /// esplicitamente perché i motori Unger portati dal Python usano invece 0 = lunedì.
    /// </summary>
    protected int EasyDayOfWeek(DateTime barTime) => (int)Clock.SessionDay(barTime).DayOfWeek;

    // ------------------------------------------------------------------ costruttori di segnale

    /// <summary>
    /// Ordine stop valido <b>esclusivamente sulla barra successiva</b>, come
    /// <c>buy next bar at LEVEL stop</c>. Il trigger non viene valutato qui: la strategia
    /// descrive l'intent, l'engine decide il fill (gap-aware, a <c>max(open, livello)</c> per un
    /// long). Questo è il punto in cui la traduzione a mano sbagliava sistematicamente,
    /// verificando il livello sulla barra corrente e degradando l'ordine a market.
    /// </summary>
    protected TradeSignal EntryStopNextBar(
        SignalType side, decimal level, OhlcvData[] data, DateTime barTime, string reason) =>
        BuildEntry(side, level, TradeOrderType.Stop, data, barTime, reason);

    /// <summary>Ordine limit valido solo sulla barra successiva (<c>next bar at LEVEL limit</c>).</summary>
    protected TradeSignal EntryLimitNextBar(
        SignalType side, decimal level, OhlcvData[] data, DateTime barTime, string reason) =>
        BuildEntry(side, level, TradeOrderType.Limit, data, barTime, reason);

    /// <summary>
    /// Ordine a mercato eseguito all'apertura della barra successiva
    /// (<c>next bar at market</c>). Il prezzo indicato è solo di riferimento: l'engine riempie
    /// all'apertura effettiva.
    /// </summary>
    protected TradeSignal EntryMarketNextBar(
        SignalType side, decimal referencePrice, OhlcvData[] data, DateTime barTime, string reason) =>
        BuildEntry(side, referencePrice, TradeOrderType.Market, data, barTime, reason);

    private TradeSignal BuildEntry(
        SignalType side,
        decimal level,
        TradeOrderType orderType,
        OhlcvData[] data,
        DateTime barTime,
        string reason)
    {
        // Timeframe DICHIARATO, non dedotto dalla serie: attraverso il fine settimana la
        // distanza fra le ultime due barre è il buco, e un ordine "next bar" nascerebbe valido
        // giorni dopo invece che sulla barra seguente.
        var nextBar = EasyLib.EstimateNextBarUtc(data, barTime, TimeframeMinutes);
        var signal = new TradeSignal
        {
            Date = barTime,
            Type = side,
            Price = level,
            StrategyName = Name,
            Quantity = Contracts,
            OrderType = orderType,
            // L'ordine nasce alla chiusura della barra di segnale, vive una sola barra e scade
            // con essa: è la semantica di "next bar" di EasyLanguage, dove l'ordine viene
            // riemesso a ogni barra finché la condizione resta valida.
            ValidFromUtc = nextBar,
            ExpiresAtUtc = nextBar,
            // Quanto dura quella "sola barra". Serve a chi esegue: il backtest gira al timeframe
            // minimo del portafoglio, e senza questo numero terrebbe l'ordine vivo per un tick del
            // portafoglio invece che per una barra della strategia.
            TimeframeMinutes = TimeframeMinutes,
            StopLossMoneyPerFutureContract = ResolveMoneyPerContract(StopAtrMultiplier, StopMoney, data, barTime),
            TakeProfitMoneyPerFutureContract = ResolveMoneyPerContract(TargetAtrMultiplier, ProfitMoney, data, barTime),
            BreakEvenMoneyPerFutureContract = BreakEvenMoney > 0 ? BreakEvenMoney : null,
            TrailingStopMoneyPerFutureContract = TrailingStopMoney > 0 ? TrailingStopMoney : null,
            MaxBarsInPosition = MaxBars > 0 ? MaxBars : null,
            CloseAtUtc = MaxDaysInTrade > 0
                ? Clock.SessionInstantUtc(barTime.AddDays(MaxDaysInTrade), TimeOnly.MinValue)
                : null,
            TimeExitOnlyIfProfitBelowMoneyPerContract = TimeExitOnlyIfProfitBelow,
            Reason = reason
        };

        if (MaxEntriesPerSession > 0)
        {
            signal.MaxEntriesPerSession = MaxEntriesPerSession;
            signal.EntrySessionStartUtc = ResolveEntrySessionStartUtc(nextBar);
        }

        return signal;
    }

    /// <summary>
    /// Stop o target in denaro per contratto per QUESTO segnale: il multiplo dell'ATR delle sessioni
    /// chiuse se dichiarato, altrimenti il denaro fisso. Vedi <see cref="StopAtrMultiplier"/>.
    /// </summary>
    private decimal? ResolveMoneyPerContract(decimal atrMultiplier, int fixedMoney, OhlcvData[] data, DateTime barTime)
    {
        if (atrMultiplier > 0m)
        {
            var atr = ClosedSessionAtrPoints(data, barTime);
            if (atr.HasValue && atr.Value > 0m)
                return Math.Round(atr.Value * InstrumentRegistry.PointValue(Symbol) * atrMultiplier, 2);
        }

        return fixedMoney > 0 ? fixedMoney : null;
    }

    /// <summary>
    /// ATR in punti sulle ultime <see cref="AtrSessions"/> sessioni <b>chiuse</b> prima della barra:
    /// Python <c>session_atr(df, 14, shift=1)</c>. La sessione in corso non entra mai nel calcolo.
    /// <c>null</c> se la storia non copre <c>AtrSessions + 1</c> sessioni. Le sessioni sono quelle
    /// della strategia (<see cref="SessionStart"/>), le stesse dei pattern.
    /// </summary>
    protected decimal? ClosedSessionAtrPoints(OhlcvData[] data, DateTime barTime)
    {
        var currentSession = ResolveEntrySessionStartUtc(barTime);
        var sessions = new List<(decimal High, decimal Low, decimal Close)>();
        DateTime? key = null;
        decimal high = 0m, low = 0m, close = 0m;

        foreach (var candidate in data)
        {
            var candidateKey = ResolveEntrySessionStartUtc(candidate.DateTime);
            if (candidateKey >= currentSession)
                break;

            if (key != candidateKey)
            {
                if (key.HasValue)
                    sessions.Add((high, low, close));
                key = candidateKey;
                high = candidate.High;
                low = candidate.Low;
            }
            else
            {
                high = Math.Max(high, candidate.High);
                low = Math.Min(low, candidate.Low);
            }

            close = candidate.Close;
        }

        if (key.HasValue)
            sessions.Add((high, low, close));
        if (sessions.Count < AtrSessions + 1)
            return null;

        decimal sum = 0m;
        for (var index = sessions.Count - AtrSessions; index < sessions.Count; index++)
        {
            var session = sessions[index];
            var previousClose = sessions[index - 1].Close;
            sum += Math.Max(session.High - session.Low,
                Math.Max(Math.Abs(session.High - previousClose), Math.Abs(session.Low - previousClose)));
        }

        return sum / AtrSessions;
    }

    /// <summary>
    /// Inizio della sessione di trading che contiene <paramref name="timeUtc"/>. Usa
    /// <see cref="SessionStart"/>/<see cref="SessionEnd"/> del motore, così il limite
    /// di fill per sessione coincide con il calendario dei pattern.
    /// </summary>
    protected virtual DateTime ResolveEntrySessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = Clock.SessionInstantUtc(timeUtc, SessionStart);
        return SessionStart > SessionEnd && timeUtc < sessionStart
            ? Clock.SessionInstantUtc(timeUtc.AddDays(-1), SessionStart)
            : sessionStart;
    }

    /// <summary>
    /// Unisce due ingressi contemporanei (long e short armati sulla stessa barra) in un segnale
    /// primario con companion. L'engine li tratta come intent indipendenti in OCO: il fill di
    /// uno cancella l'altro.
    /// </summary>
    protected static TradeSignal Combine(List<TradeSignal> entries, TradeSignal fallbackHold)
    {
        if (entries.Count == 0) return fallbackHold;
        var primary = entries[0];
        if (entries.Count > 1)
            primary.CompanionSignals = entries.GetRange(1, entries.Count - 1);
        return primary;
    }

    /// <summary>Segnale neutro.</summary>
    protected TradeSignal Hold(decimal price, DateTime barTime, string? reason = null) =>
        new()
        {
            Date = barTime,
            Type = SignalType.Hold,
            Price = price,
            StrategyName = Name,
            Reason = reason
        };

    /// <summary>
    /// La griglia di sessione di <b>questa strategia</b>: quella del simbolo, salvo che
    /// <see cref="OverrideSessionAnchor"/> abbia spostato l'ancoraggio.
    ///
    /// <para>L'override va applicato <b>anche qui</b> e non solo su <see cref="Session"/>: la
    /// griglia decide a quale sessione appartiene una barra e dove quella sessione finisce, quindi
    /// una griglia che lo ignorasse farebbe convivere due confini diversi nella stessa strategia —
    /// la finestra li dichiarerebbe spostati e le deadline no.</para>
    ///
    /// <para>Un'istanza per strategia, come l'orologio: <c>SessionGrid</c> non e' thread-safe.</para>
    /// </summary>
    protected SessionGrid Grid => _grid ??= BuildGrid();

    private SessionGrid BuildGrid()
    {
        var calendar = MarketCalendarRegistry.Current.Get(Symbol);
        if (_sessionAnchorOverride is { } anchor && anchor != calendar.SessionStart)
            calendar = calendar with { SessionStart = anchor };

        return new SessionGrid(calendar);
    }

    /// <summary>
    /// Vero se la barra che apre in <paramref name="barTime"/> è l'ultima della propria sessione:
    /// la barra successiva appartiene a un altro giorno di sessione della griglia. Sostituisce il
    /// confronto <c>orario di chiusura == fine sessione</c>, che con la sessione a giornata piena
    /// confrontava un orario con la sentinella di fine giornata e non era mai vero.
    /// </summary>
    protected bool IsLastBarOfSession(DateTime barTime) =>
        Grid.SessionDayOf(barTime) != Grid.SessionDayOf(barTime.AddMinutes(TimeframeMinutes));

    /// <summary>
    /// Deadline di chiusura a un orario di sessione, risolta <b>dentro la sessione</b> che contiene la
    /// barra. Serve a esprimere <c>setexitonclose</c> e le uscite di fine sessione come
    /// <c>CloseAtUtc</c> sull'ingresso, invece che come segnale di chiusura a runtime — che in
    /// <c>ExternalBroker</c> non verrebbe mai eseguito, perché il server emette solo intent di
    /// ingresso.
    ///
    /// <para><b>Perché non basta il giorno di calendario.</b> Fino al 07/09/2026 l'orario veniva
    /// risolto su <c>SessionClock.SessionDay</c>, cioè la data locale, mentre la sessione è ancorata
    /// a <c>sessionStartHour</c>. Sui simboli ad ancoraggio 0 le due coincidono; sui cinque ad
    /// ancoraggio 1 — FDAX, CC, CT, KC, SB — no, e la deadline cadeva <b>61 minuti prima</b> della
    /// fine sessione invece che uno, cioè dentro l'ultimo bucket di una 4h. Peggio: per una barra
    /// nella fascia 00:00–01:00 locali cadeva <b>22h59m dopo</b> la chiusura della propria sessione,
    /// e la posizione sopravviveva a una sessione intera. Misurato in
    /// <c>SessionCloseAtAnchorTests</c>.</para>
    ///
    /// <para><b><see cref="ZonedWindow.EndOfDay"/> significa "l'ultimo minuto della sessione"</b>,
    /// qualunque sia l'ancoraggio: si risolve sulla chiusura vera della sessione meno un minuto.
    /// Trattarlo come le 23:59 di un giorno di calendario è precisamente l'errore corretto qui.</para>
    /// </summary>
    protected DateTime ResolveCloseAtUtc(DateTime barTime, TimeOnly time) =>
        ResolveCloseAtUtc(barTime, time, rollToNextSession: true)!.Value;

    /// <summary>
    /// Come <see cref="ResolveCloseAtUtc(DateTime, TimeOnly)"/>, ma con il controllo su cosa fare
    /// quando l'orario e' <b>gia' passato</b> nella sessione della barra.
    ///
    /// <para><paramref name="rollToNextSession"/> a <c>true</c> e' il comportamento storico: la
    /// deadline vale per la sessione successiva. E' quello che serve a <c>MaxDaysInTrade</c> e alle
    /// uscite programmate, dove l'orario e' un appuntamento futuro.</para>
    ///
    /// <para>A <c>false</c> restituisce <c>null</c>: e' quello che serve a un'uscita di sessione
    /// con un'ora propria (<see cref="SessionExitTime"/>). Rimandare li' alla sessione dopo
    /// trasformerebbe in overnight proprio la posizione che quell'ora esiste per chiudere prima
    /// della notte, e la <see cref="Holding"/> dichiarata dalla strategia — <c>Intraday</c> — non
    /// sarebbe piu' vera. Il chiamante scarta l'ingresso.</para>
    /// </summary>
    protected DateTime? ResolveCloseAtUtc(DateTime barTime, TimeOnly time, bool rollToNextSession)
    {
        var sessionDay = Grid.SessionDayOf(barTime);

        // La barra puo' essere PROIETTATA, non vera: un ordine "next bar" nasce con
        // ValidFromUtc = barTime + timeframe, e dall'ultima barra del venerdi' quella proiezione
        // cade di sabato, in una sessione che il calendario non ha. Risolvere la deadline li'
        // dava una chiusura gia' passata quando l'ordine si riempiva davvero, il lunedi': la
        // posizione moriva nello stesso minuto del fill (36 trade su 873 su PT2_FDAX_PCH_001_240,
        // feed interno 2022-2025). La ricerca tiene l'ordine sulla prima barra vera e chiude a fine
        // della SUA sessione: si avanza al primo giorno in cui il simbolo ha una sessione. Un
        // calendario che non dichiara i giorni (null) lascia tutto com'e'.
        while (Grid.IsSessionDay(sessionDay) == false)
            sessionDay = sessionDay.AddDays(1);

        var open = Grid.SessionOpenUtc(sessionDay);
        var close = Grid.SessionOpenUtc(sessionDay.AddDays(1));

        if (time == ZonedWindow.EndOfDay)
            return close.AddMinutes(-1);

        var offset = time.ToTimeSpan();

        // L'orario si colloca dentro l'arco della sessione, non del giorno di calendario: una
        // sessione ancorata all'01:00 contiene le 00:30 del giorno DOPO, non quelle del proprio.
        var target = Clock.ToUtc(sessionDay.Add(offset));
        if (target < open)
            target = Clock.ToUtc(sessionDay.AddDays(1).Add(offset));

        // Orario gia' passato per questa sessione: vale per la prossima. Si riparte dal giorno di
        // sessione successivo e non da "+1 giorno" sull'istante, perche' fra i due c'e' il cambio
        // d'ora.
        if (target <= barTime)
        {
            if (!rollToNextSession)
                return null;

            var next = sessionDay.AddDays(1);
            target = Clock.ToUtc(next.Add(offset));
            if (target < Grid.SessionOpenUtc(next))
                target = Clock.ToUtc(next.AddDays(1).Add(offset));
        }

        return target;
    }

    // ------------------------------------------------------------------ contratto ITradingStrategy

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Symbol { get; }
    public abstract int TimeframeMinutes { get; }

    /// <summary>
    /// Barre di storia necessarie. Il default copre sei sessioni piene al timeframe dichiarato,
    /// perché <see cref="EasyLib.OHLCMulti5"/> ricostruisce d0..d5 dalla sola finestra ricevuta e
    /// riparte da zero a ogni valutazione: con una finestra più corta le sessioni più vecchie
    /// risultano troncate e i pattern che leggono d4/d5 lavorano su valori parziali.
    /// </summary>
    public virtual int RequiredCandles => Math.Max(SessionsToCandles(6), AtrWarmupCandles);

    /// <summary>
    /// Barre che servono allo stop o al target in ATR: le <see cref="AtrSessions"/> sessioni chiuse
    /// piu' quella in corso, e una di margine. Zero se la strategia non ne usa.
    ///
    /// <para><b>Perche' esiste.</b> Dal 22/09/2026 al 24/09/2026 la finestra minima era di sei sessioni,
    /// e chi valuta la strategia le passa poco piu' di quelle: <see cref="ClosedSessionAtrPoints"/> non
    /// trovava mai quindici sessioni chiuse, restituiva null, e lo stop ripiegava sul denaro fisso — che
    /// nelle configurazioni in ATR e' zero. Nessuno stop, nessun target, nessun errore: la prima
    /// griglia in ATR (RHL su UK100 a 4 ore) dava lo stesso netto al centesimo per tre stop e tre target.
    /// Un motore che ridefinisce <see cref="RequiredCandles"/> senza passare dalla base deve includere
    /// questo numero.</para>
    /// </summary>
    protected int AtrWarmupCandles =>
        StopAtrMultiplier > 0m || TargetAtrMultiplier > 0m ? SessionsToCandles(AtrSessions + 2) : 0;

    /// <summary>Barre necessarie a coprire <paramref name="sessions"/> sessioni piene.</summary>
    protected int SessionsToCandles(int sessions)
    {
        var barsPerDay = Math.Max(1, 1440 / Math.Max(1, TimeframeMinutes));
        return sessions * barsPerDay;
    }
}
