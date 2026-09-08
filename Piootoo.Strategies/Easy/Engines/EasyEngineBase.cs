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
    /// </summary>
    protected ZonedWindow Session => _session ??= ResolveSession();

    /// <summary>
    /// L'ancoraggio del simbolo, o quello dichiarato da <see cref="OverrideSessionAnchor"/>.
    ///
    /// <para>La forma e' sempre quella della ricerca — <c>(ancoraggio, 2359)</c>, giornata piena a
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
            MarketCalendarRegistry.Current.Get(Symbol).SessionStartHour);
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
    /// <param name="hour">Ora di inizio sessione nell'orologio della ricerca, 0-23.</param>
    /// <param name="reason">Perche' questa strategia non segue l'ancoraggio del proprio simbolo.</param>
    protected void OverrideSessionAnchor(int hour, string reason)
    {
        if (hour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), hour, "Ora di sessione fuori da 0-23.");

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                $"{GetType().Name}: un override dell'ancoraggio di sessione deve dichiarare il " +
                "motivo. Senza, e' indistinguibile da una distrazione — ed e' la sola cosa che, " +
                "sbagliata, sposta il confine di sessione senza produrre alcun messaggio.",
                nameof(reason));
        }

        _sessionAnchorOverride = hour;
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
    /// </summary>
    protected ZonedWindow? TradingWindow
    {
        get => _tradingWindow;
        set
        {
            _tradingWindow = value;
            _windowClock = null;
        }
    }

    private ZonedWindow? _session;
    private SessionGrid? _grid;
    private int? _sessionAnchorOverride;
    private ZonedWindow? _tradingWindow;

    /// <summary>
    /// Orario HHMM di inizio sessione, nell'orologio della ricerca. <b>Sola lettura</b>: lo dichiara
    /// il calendario del simbolo, non la strategia. Per spostarlo esiste
    /// <see cref="OverrideSessionAnchor"/>.
    ///
    /// <para><b>Non e' l'orario di borsa della sorgente EasyLanguage.</b> Un <c>SessBegin = 1700</c>
    /// e' la riapertura Globex in ora di Chicago e appartiene a un modello di sessione diverso:
    /// non c'e' nessuna aritmetica che lo porti a un ancoraggio, e cercarne una e' l'errore. Vedi
    /// <c>docs/domini/porting-da-report-sweep.md</c> §2.1.</para>
    /// </summary>
    protected int SessionStartTime => Session.StartHhmm;

    /// <summary>Orario HHMM di fine sessione. Sola lettura, come <see cref="SessionStartTime"/>.</summary>
    protected int SessionEndTime => Session.EndHhmm;

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
        EasyLib.OHLCMulti5(Clock, SessionStartTime, SessionEndTime, data, barTime, out ohlc);

    /// <summary>Orario HHMM della barra, letto in ora di borsa.</summary>
    protected int Hhmm(DateTime barTime) => Clock.Hhmm(barTime);

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
    /// L'orario <c>HHMM</c> con cui questa barra va confrontata con le soglie dei parametri
    /// (<c>start_hour</c>, <c>end_hour</c>, pause, orari di uscita), letto sull'orologio di sessione.
    ///
    /// <para><b>Non è <see cref="Hhmm"/>.</b> Quello è l'orario di apertura della barra e serve a
    /// dire a quale <i>sessione</i> appartiene, che è una domanda diversa e ha già la sua risposta
    /// in <see cref="EasyLib"/>. Questo è il nome con cui la ricerca chiamava la stessa barra, e la
    /// regola vive in un punto solo: <see cref="SessionClock.BarLabelHhmm"/>.</para>
    /// </summary>
    protected int ParamHhmm(DateTime barTime) =>
        LegacyBarOpenLabels ? Clock.Hhmm(barTime) : Clock.BarLabelHhmm(barTime, TimeframeMinutes);

    /// <summary>
    /// Come <see cref="ParamHhmm"/>, ma sull'orologio della <see cref="TradingWindow"/>. I due fusi
    /// non coincidono e non vanno riconciliati a mano: la finestra dichiara il proprio.
    /// </summary>
    protected int WindowParamHhmm(DateTime barTime) =>
        LegacyBarOpenLabels
            ? WindowClock.Hhmm(barTime)
            : WindowClock.BarLabelHhmm(barTime, TimeframeMinutes);

    /// <summary>
    /// Valuta la <see cref="TradingWindow"/> dichiarata sull'orologio che essa dichiara.
    /// Restituisce <c>null</c> quando la strategia non la dichiara: in quel caso il motore ricade
    /// sul proprio percorso storico, che confronta i campi interi sull'orologio di sessione.
    ///
    /// <para>Gli estremi sono <b>inclusi</b> e il confronto è su HHMM pieni, come
    /// <c>time_window</c> del motore Python: la barra esattamente su <c>end_hour:00</c> entra
    /// nella finestra.</para>
    /// </summary>
    protected bool? InDeclaredWindow(DateTime barTime) =>
        TradingWindow is { } window
            ? EasyLib.TimeWindowInclusive(window.StartHhmm, window.EndHhmm, WindowParamHhmm(barTime))
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
        ((int)(LegacyBarOpenLabels
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
            StopLossMoneyPerFutureContract = StopMoney > 0 ? StopMoney : null,
            TakeProfitMoneyPerFutureContract = ProfitMoney > 0 ? ProfitMoney : null,
            BreakEvenMoneyPerFutureContract = BreakEvenMoney > 0 ? BreakEvenMoney : null,
            TrailingStopMoneyPerFutureContract = TrailingStopMoney > 0 ? TrailingStopMoney : null,
            MaxBarsInPosition = MaxBars > 0 ? MaxBars : null,
            CloseAtUtc = MaxDaysInTrade > 0
                ? Clock.SessionInstantUtc(barTime.AddDays(MaxDaysInTrade), 0)
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
    /// Inizio della sessione di trading che contiene <paramref name="timeUtc"/>. Usa
    /// <see cref="SessionStartTime"/>/<see cref="SessionEndTime"/> del motore, così il limite
    /// di fill per sessione coincide con il calendario dei pattern.
    /// </summary>
    protected virtual DateTime ResolveEntrySessionStartUtc(DateTime timeUtc)
    {
        var sessionStart = Clock.SessionInstantUtc(timeUtc, SessionStartTime);
        return SessionStartTime > SessionEndTime && timeUtc < sessionStart
            ? Clock.SessionInstantUtc(timeUtc.AddDays(-1), SessionStartTime)
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
    private SessionGrid Grid => _grid ??= BuildGrid();

    private SessionGrid BuildGrid()
    {
        var calendar = MarketCalendarRegistry.Current.Get(Symbol);
        if (_sessionAnchorOverride is { } hour && hour != calendar.SessionStartHour)
            calendar = calendar with { SessionStartHour = hour };

        return new SessionGrid(calendar);
    }

    /// <summary>
    /// Deadline di chiusura a un orario HHMM, risolta <b>dentro la sessione</b> che contiene la
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
    /// <para><b><c>2359</c> è una sentinella, non un orario.</b> È il valore che
    /// <c>ZonedWindow.ResearchSession</c> mette come fine, e significa "l'ultimo minuto della
    /// sessione": si risolve quindi sulla chiusura vera della sessione meno un minuto, qualunque sia
    /// l'ancoraggio. Trattarlo come le 23:59 di un giorno è precisamente l'errore corretto qui.</para>
    /// </summary>
    protected DateTime ResolveCloseAtUtc(DateTime barTime, int hhmm)
    {
        var sessionDay = Grid.SessionDayOf(barTime);
        var open = Grid.SessionOpenUtc(sessionDay);
        var close = Grid.SessionOpenUtc(sessionDay.AddDays(1));

        if (hhmm >= 2359)
            return close.AddMinutes(-1);

        // L'orario si colloca dentro l'arco della sessione, non del giorno di calendario: una
        // sessione ancorata all'01:00 contiene le 00:30 del giorno DOPO, non quelle del proprio.
        var target = Clock.ToUtc(sessionDay.AddMinutes(hhmm / 100 * 60 + hhmm % 100));
        if (target < open)
            target = Clock.ToUtc(sessionDay.AddDays(1).AddMinutes(hhmm / 100 * 60 + hhmm % 100));

        // Orario gia' passato per questa sessione: vale per la prossima. Si riparte dal giorno di
        // sessione successivo e non da "+1 giorno" sull'istante, perche' fra i due c'e' il cambio
        // d'ora.
        if (target <= barTime)
        {
            var next = sessionDay.AddDays(1);
            target = Clock.ToUtc(next.AddMinutes(hhmm / 100 * 60 + hhmm % 100));
            if (target < Grid.SessionOpenUtc(next))
                target = Clock.ToUtc(next.AddDays(1).AddMinutes(hhmm / 100 * 60 + hhmm % 100));
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
    public virtual int RequiredCandles => SessionsToCandles(6);

    /// <summary>Barre necessarie a coprire <paramref name="sessions"/> sessioni piene.</summary>
    protected int SessionsToCandles(int sessions)
    {
        var barsPerDay = Math.Max(1, 1440 / Math.Max(1, TimeframeMinutes));
        return sessions * barsPerDay;
    }
}
