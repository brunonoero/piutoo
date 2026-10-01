using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies.Engines;

/// <summary>
/// Base comune dei motori PT5DAV: le regole che la ricerca v5.0 (consegna del 23/09/2026,
/// <c>piootoo-repository/PT5DAV/CONVENZIONI.md</c>) applica a tutte le 224 strategie e che i motori
/// condivisi con le PT3B non hanno.
///
/// <para><b>Perche' motori propri e non quelli di <c>Easy/Engines</c>.</b> Formule dei livelli,
/// segnali, tipi d'ordine e pattern sono gli stessi — e i pattern vengono da <c>EasyLib</c>, non
/// sono riscritti. Cambiano pero' cose che nei motori condivisi non si possono dichiarare senza
/// modificarli, e i motori condivisi non si toccano (decisione del 24/09/2026):</para>
/// <list type="bullet">
///   <item>stop, target e offset dei livelli in multipli di <b>ATR50 di Wilder</b> sulle sessioni
///   chiuse — i motori condivisi hanno un ATR a 14 sessioni a media semplice, costante;</item>
///   <item><b>rodaggio</b>: nessun ingresso finche' l'ATR50 non esiste, invece del ripiego sul
///   denaro fisso (che qui vale zero, cioe' un ingresso senza stop);</item>
///   <item>la <b>sessione della ricerca</b>: la data di Roma dall'ancoraggio, con le barre della
///   domenica sera nella sessione del lunedi' (misurato sui trade BIAS: gli indici di barra del
///   lunedi' sono spostati); il DAX esiste solo fra le 08:00 e le 22:00 con la sessione che parte
///   alle 08:00;</item>
///   <item>l'<b>uscita intraday al limite del CFD</b>, un orario di New York: alla chiusura
///   dell'ultima barra che finisce entro le 16:50 NY (17:00 per BP e BTC, 13:30 per CC e KC), non a
///   fine sessione della ricerca;</item>
///   <item><b>nessun ingresso a CFD chiuso</b>, con gli orari misurati sul broker.</item>
/// </list>
///
/// <para><b>Cosa resta uguale, di proposito.</b> L'etichetta della barra: la ricerca v5.0 etichetta le
/// candele sull'apertura ma scrive gli orari delle regole sulla <b>chiusura</b> ("ordini emessi sulle
/// barre che chiudono fra le 11:00 e le 13:00") — cioe' esattamente il default del sistema,
/// <see cref="EasyEngineBase.ResearchLabelsBarsOnOpen"/> a <c>false</c>. Misurato sui trade: con
/// finestra 11-13 su BP 15m gli ingressi vanno dalla barra delle 11:00 a quella delle 13:00 incluse.
/// Gli orari si riportano verbatim, come sempre.</para>
///
/// <para><b>Stato.</b> OHLC di sessione e ATR50 vivono in <see cref="Pt5DavSessionState"/>, che
/// viaggia fra le valutazioni come <c>RuntimeState</c> (in backtest, in sweep e in sessione
/// <c>ExternalBroker</c>). Quando manca si ricostruisce dalla finestra ricevuta: la media di Wilder
/// riparte allora da una media semplice delle prime 50 sessioni della finestra, ed e' meno precisa
/// finche' non ha visto altre sessioni — il motivo di <see cref="WarmupSessions"/>.</para>
/// </summary>
public abstract class Pt5DavEngineBase : EasyEngineBase
{
    /// <summary>Sessioni dell'ATR della ricerca: 50, media di Wilder, range vero.</summary>
    public const int AtrPeriod = 50;

    /// <summary>
    /// Sessioni di storia chieste a ogni valutazione (<see cref="RequiredCandles"/>). Devono essere
    /// piu' di <see cref="AtrPeriod"/>, perche' la prima sessione della finestra e' quasi sempre
    /// parziale e non entra nell'ATR; il resto serve a far convergere la media quando lo stato va
    /// ricostruito.
    ///
    /// <para><b>Perche' 60 e non di piu'.</b> A 15 minuti sono 5.760 barre, e il backtest ne copia
    /// 1,2 volte tante a ogni barra: oltre questa soglia l'array finisce nel Large Object Heap e il
    /// costo del run esplode. Lo stato incrementale rende inutile di piu' in backtest; in sessione
    /// conta solo al primo avvio e dopo un riavvio del server.</para>
    /// </summary>
    public const int WarmupSessions = 60;

    /// <summary>Il codice della strategia nella consegna PT5DAV (es. <c>NQ-15M-BOS-f7179d</c>).</summary>
    public abstract string ResearchCode { get; }

    /// <summary>Stop in multipli di ATR50 dal prezzo d'ingresso (<c>stop_atr</c>). 0 = nessuno stop.</summary>
    protected decimal StopAtr;

    /// <summary>Target in multipli di ATR50 dal prezzo d'ingresso (<c>take_profit_atr</c>). 0 = nessun target.</summary>
    protected decimal TargetAtr;

    /// <summary>Giorno escluso per entrambi i lati, pandas 0 = lunedi' (<c>skip_day</c>). -1 = nessuno.</summary>
    protected int SkipDay = -1;

    /// <summary>Giorno escluso per il long, pandas (<c>not_le_day</c>). -1 = nessuno.</summary>
    protected int NotEntryDayLong = -1;

    /// <summary>Giorno escluso per lo short, pandas (<c>not_se_day</c>). -1 = nessuno.</summary>
    protected int NotEntryDayShort = -1;

    private Pt5DavSessionState _pt5;
    private SessionClock? _newYork;

    protected Pt5DavEngineBase() => ApplyResearchMarketAnchor();

    /// <summary>
    /// La fascia del DAX della ricerca v5.0 sposta anche l'inizio della sessione: d0, d1, i pattern e
    /// l'ATR sono calcolati sulla giornata 08:00-22:00, non su quella del calendario (01:00), con cui
    /// sono state trovate le PT3B su FDAX. Le classi la ricevono dal costruttore; i contenitori di
    /// ricerca, il cui simbolo arriva dai parametri, la richiamano dopo averlo impostato.
    /// </summary>
    protected void ApplyResearchMarketAnchor()
    {
        if (Pt5DavMarket.ResearchMarketHours(Symbol) is { } hours)
        {
            OverrideSessionAnchor(
                hours.Opens,
                "PT5DAV v5.0: la ricerca taglia il DAX alla fascia 08:00-22:00 e fa partire la " +
                "sessione alle 08:00 (piootoo-repository/PT5DAV/ORARIO_MERCATO.md)");
        }
    }

    /// <inheritdoc />
    public override int RequiredCandles => SessionsToCandles(WarmupSessions);

    /// <summary>
    /// La tenuta la dichiara <c>intraday_only</c>: intraday chiude al limite del CFD, altrimenti la
    /// strategia tiene quanto dice la ricerca (vedi <see cref="Finish"/>). Il piano resta sopra.
    /// </summary>
    public override StrategyHolding Holding =>
        IntradayOnly ? StrategyHolding.Intraday : StrategyHolding.Multiday;

    /// <summary>
    /// Le PT5DAV non leggono parametri a runtime: sono configurazioni della ricerca, riportate
    /// verbatim nel costruttore. Il metodo esiste perche' il catalogo lo cerca per riflessione; lo
    /// ridefiniscono solo i contenitori di ricerca (<c>RC5_*</c>), che le leve le ricevono da qui.
    /// </summary>
    public virtual void Initialize(Dictionary<string, object>? parameters = null)
    {
    }

    // ------------------------------------------------------------------ leve per nome (contenitori)

    private int _researchStartHour = -1;
    private int _researchEndHour = -1;

    /// <summary>
    /// L'<c>Initialize</c> dei contenitori di ricerca (<c>RC5_*</c>): simbolo e timeframe dai
    /// parametri, poi ogni leva con il <b>nome della colonna</b> di <c>strategie_224.csv</c>
    /// (<c>stop_atr</c>, <c>ptn_neut_yes</c>, <c>channel_len</c>...). Una riga della consegna si
    /// riesegue cosi' com'e', e le griglie della sweep parlano la lingua delle schede.
    ///
    /// <para>Una colonna che il motore non ha <b>ferma</b> la configurazione: variarla non
    /// cambierebbe niente, e una griglia che gira tre volte la stessa strategia non se ne accorge.</para>
    /// </summary>
    protected void InitializeResearchContainer(
        Piootoo.Strategies.ResearchContainers.ResearchContainerIdentity identity, IDictionary<string, object>? parameters)
    {
        if (parameters is null)
            return;

        if (parameters.TryGetValue("Symbol", out var symbol) && symbol is string text && !string.IsNullOrWhiteSpace(text))
            identity.Symbol = "@" + text.Trim().TrimStart('@').ToUpperInvariant();
        if (parameters.TryGetValue("TimeframeMinutes", out var timeframe) && ResearchInt(timeframe) > 0)
            identity.TimeframeMinutes = ResearchInt(timeframe);

        // Il DAX della ricerca v5.0 ha la sessione dalle 08:00: il simbolo e' arrivato solo ora.
        ApplyResearchMarketAnchor();

        foreach (var (key, value) in parameters)
        {
            if (key is "Symbol" or "TimeframeMinutes")
                continue;

            if (!ApplyResearchParameter(key, value) && !IsOffValue(key, value))
            {
                throw new ArgumentException(
                    $"{GetType().Name} non ha la leva '{key}' = {value}: la griglia la varierebbe senza effetto.");
            }
        }
    }

    /// <summary>
    /// Le colonne comuni al loro valore spento: un motore che non le ha le ignora, perche' spente non
    /// cambierebbero niente. Senza, una riga della consegna non si potrebbe rieseguire intera.
    /// </summary>
    private static bool IsOffValue(string key, object value)
    {
        var number = ResearchDecimal(value);
        return key switch
        {
            "start_hour" or "end_hour" or "skip_day" or "not_le_day" or "not_se_day" => number == -1m,
            "max_bars" or "stop_atr" or "take_profit_atr" => number == 0m,
            "ptn_neut_yes" => number == 55m,
            "ptn_neut_no" => number == 56m,
            "ptn_dir_yes" => number == 52m,
            "ptn_dir_no" => number == 53m,
            "ptn_ly_yes" or "ptn_sy_yes" => number == 152m,
            "ptn_ly_no" or "ptn_sy_no" => number == 153m,
            _ => false
        };
    }

    /// <summary>
    /// Imposta una leva dal nome della sua colonna nella consegna. <c>false</c> = il motore non la
    /// ha. Le colonne comuni stanno qui; ogni motore aggiunge le proprie e rimanda alla base il resto.
    /// Le conversioni sono quelle del generatore delle classi (<c>tools/pt5dav/gen_pt5dav.py</c>).
    /// </summary>
    protected virtual bool ApplyResearchParameter(string key, object value)
    {
        switch (key)
        {
            case "start_hour":
                _researchStartHour = ResearchInt(value);
                TradingWindow = ResearchWindow(_researchStartHour, _researchEndHour);
                return true;
            case "end_hour":
                _researchEndHour = ResearchInt(value);
                TradingWindow = ResearchWindow(_researchStartHour, _researchEndHour);
                return true;
            case "stop_atr":
                StopAtr = ResearchDecimal(value);
                return true;
            case "take_profit_atr":
                TargetAtr = ResearchDecimal(value);
                return true;
            case "intraday_only":
                IntradayOnly = ResearchInt(value) != 0;
                return true;
            case "max_bars":
                MaxBars = ResearchInt(value);
                return true;
            // Colonne che la consegna porta su tutte le righe ma che nessuna strategia accende: a zero
            // non cambiano niente, accese sarebbero un motore diverso.
            case "trailing_stop" or "breakeven" or "dvol_min":
                return ResearchDecimal(value) == 0m;
            default:
                return false;
        }
    }

    /// <summary>Un intero della consegna: il CSV scrive gli interi anche come <c>5.0</c>.</summary>
    protected static int ResearchInt(object value) =>
        (int)Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Un decimale della consegna, letto senza cultura.</summary>
    protected static decimal ResearchDecimal(object value) =>
        Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// <c>skip_day</c>, <c>not_le_day</c> e <c>not_se_day</c> li leggono solo alcuni motori: chi non li
    /// ha non li dichiara, e la colonna ferma la configurazione come ogni altra leva inerte.
    /// </summary>
    protected bool ApplyDayParameter(string key, object value)
    {
        switch (key)
        {
            case "skip_day":
                SkipDay = ResearchInt(value);
                return true;
            case "not_le_day":
                NotEntryDayLong = ResearchInt(value);
                return true;
            case "not_se_day":
                NotEntryDayShort = ResearchInt(value);
                return true;
            default:
                return false;
        }
    }

    // ------------------------------------------------------------------ valutazione

    /// <summary>
    /// Aggiorna lo stato di sessione con le barre nuove della finestra e, se la barra corrente e'
    /// operabile, passa la valutazione al motore.
    /// </summary>
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length == 0)
            return Hold(0m, currentDate, "Dati insufficienti");

        if (FoldsBars)
        {
            if (!SupportsFoldedBars)
            {
                throw new NotSupportedException(
                    $"{Name}: questo motore dichiara uscite a barre che conta chi esegue, sulla serie " +
                    "ricevuta, e non puo' ragionare su barre piegate (BarMinutes diverso da TimeframeMinutes).");
            }

            // Si ragiona sulle barre della ricerca, e solo quando una si chiude: le altre barre della
            // serie non sono un momento in cui la strategia esiste.
            var received = data[^1];
            if (FoldToResearchBars(data) is not { } folded)
                return Hold(received.Close, received.DateTime, "La barra della ricerca non e' ancora chiusa");

            data = folded;
        }

        var bar = data[^1];
        Advance(data);

        if (!_pt5.LastBarInMarket)
            return Hold(bar.Close, bar.DateTime, "Barra fuori dalla fascia di mercato della ricerca");

        if (CountsMarketBarsInPosition && bar.DateTime > _pt5.MarketBarsCountedUtc)
        {
            _pt5.MarketBarsCountedUtc = bar.DateTime;
            _pt5.MarketBarsInPosition = CurrentMP != 0 ? _pt5.MarketBarsInPosition + 1 : 0;

            // La barra d'ingresso e' la prima contata e max_bars non la conta: si esce alla chiusura
            // della barra numero max_bars + 1, cioe' a mercato all'apertura della successiva.
            if (_pt5.MarketBarsInPosition > MaxBars)
            {
                var exit = EntryMarketNextBar(
                    CurrentMP > 0 ? SignalType.Sell : SignalType.Buy, bar.Close, data, bar.DateTime,
                    $"max_bars {MaxBars}: uscita a barre di mercato");
                exit.ExitOnly = true;
                RetimeToResearchBar(exit);
                return exit;
            }
        }

        if (_pt5.SessionsClosed < AtrPeriod)
            return Hold(bar.Close, bar.DateTime, "Rodaggio: ATR50 non ancora disponibile");

        return Evaluate(data, bar, BuildOhlc());
    }

    // ------------------------------------------------------------------ barre della ricerca

    /// <summary>
    /// L'ampiezza, in minuti, della barra <b>su cui il motore ragiona</b>. Coincide con
    /// <see cref="EasyEngineBase.TimeframeMinutes"/>, che e' la serie che la strategia riceve, salvo
    /// quando la barra della ricerca non esiste nel feed e il motore se la costruisce.
    ///
    /// <para><b>L'unico caso: il DAX a 4 ore.</b> La ricerca v5 lo taglia alla fascia 08-22 e fa
    /// partire sessione e barre dalle 08:00: 08-12, 12-16, 16-20, 20-22. La griglia del feed oltre
    /// l'ora e' una per <i>simbolo</i> ed e' ancorata all'inizio sessione del calendario (01:00:
    /// 01-05, 05-09, 09-13…), quella su cui sono state trovate e girano le PT3B su FDAX. Le due
    /// griglie danno barre diverse, e nessuna aritmetica ricava le une dalle altre. Provato il
    /// 01/10/2026 a far girare le cinque FDAX a 4 ore della consegna v5.1 sulla griglia 01:00 con i
    /// parametri verbatim: tre non aprono mai un trade (la loro finestra chiede una barra che chiuda
    /// entro le 12:00, e li' le barre chiudono alle 13, 17 e 21), le altre due ritrovano il 25% e il
    /// 67% dei trade della ricerca. La classe dichiara allora la serie a 60 minuti, che e' allineata
    /// all'ora su qualunque griglia, e il motore piega quattro barre orarie nella barra della
    /// ricerca (<see cref="FoldToResearchBars"/>).</para>
    ///
    /// <para><b>Cosa ne segue.</b> La strategia e' valutata a ogni barra oraria ma decide solo su
    /// quella che chiude una barra della ricerca; l'ordine nasce valido per una barra della ricerca
    /// e non per una della serie (<see cref="RetimeToResearchBar"/>); finestra, giorni, uscite e
    /// <c>max_bars</c> si leggono sulle barre della ricerca.</para>
    /// </summary>
    public virtual int BarMinutes => TimeframeMinutes;

    private bool FoldsBars => BarMinutes != TimeframeMinutes;

    /// <summary>
    /// Falso per i motori che non possono ragionare su barre piegate: quelli che lasciano contare
    /// un'uscita a chi esegue (<c>MaxBarsInPosition</c> del BIAS, sulle barre della serie) o che
    /// emettono uscite proprie con i tempi della serie (MAC). Oggi li usano solo PC, BO, BO_S, VBO,
    /// LF e LF_HL, cioe' le cinque FDAX a 4 ore.
    /// </summary>
    protected virtual bool SupportsFoldedBars => true;

    /// <summary>
    /// Le barre della ricerca ricavate dalla finestra ricevuta, o <c>null</c> se la barra corrente
    /// non ne chiude una (e' fuori fascia, o non e' l'ultima del suo gruppo). I gruppi partono
    /// dall'apertura della sessione della ricerca; l'ultimo della sessione e' tronco dove la fascia
    /// finisce (20-22 sul DAX), come nella ricerca.
    /// </summary>
    private OhlcvData[]? FoldToResearchBars(OhlcvData[] data)
    {
        var current = data[^1];
        if (!InResearchMarket(current.DateTime))
            return null;

        var bucket = ResearchBarOpenUtc(current.DateTime);
        var sessionEnd = SessionEndUtc(ResearchSessionDay(current.DateTime));
        var bucketEnd = bucket.AddMinutes(BarMinutes);
        if (bucketEnd > sessionEnd)
            bucketEnd = sessionEnd;
        if (current.DateTime.AddMinutes(TimeframeMinutes) != bucketEnd)
            return null;

        var folded = new List<OhlcvData>(data.Length * TimeframeMinutes / BarMinutes + 2);
        OhlcvData? open = null;
        foreach (var candle in data)
        {
            if (!InResearchMarket(candle.DateTime))
                continue;

            var start = ResearchBarOpenUtc(candle.DateTime);
            if (open is null || open.DateTime != start)
            {
                open = new OhlcvData
                {
                    DateTime = start,
                    Open = candle.Open,
                    High = candle.High,
                    Low = candle.Low,
                    Close = candle.Close,
                    Volume = candle.Volume
                };
                folded.Add(open);
            }
            else
            {
                open.High = Math.Max(open.High, candle.High);
                open.Low = Math.Min(open.Low, candle.Low);
                open.Close = candle.Close;
                open.Volume += candle.Volume;
            }
        }

        return folded.ToArray();
    }

    /// <summary>Apertura della barra della ricerca che contiene la barra della serie indicata.</summary>
    private DateTime ResearchBarOpenUtc(DateTime barUtc)
    {
        var first = SessionOpenUtc(ResearchSessionDay(barUtc));
        var index = Math.Floor((barUtc - first).TotalMinutes / BarMinutes);
        return first.AddMinutes(index * BarMinutes);
    }

    /// <summary>
    /// Un segnale costruito dai costruttori comuni vale "dalla barra successiva" della <i>serie</i>;
    /// dove il motore piega le barre deve valere dalla chiusura della barra della ricerca, e vivere
    /// quanto una di quelle.
    /// </summary>
    private void RetimeToResearchBar(TradeSignal signal)
    {
        if (!FoldsBars)
            return;

        var next = signal.Date.AddMinutes(BarMinutes);
        signal.ValidFromUtc = next;
        signal.ExpiresAtUtc = next;
        signal.TimeframeMinutes = BarMinutes;
    }

    /// <summary>
    /// La logica del motore sulla barra appena chiusa. <paramref name="ohlc"/> ha il layout di
    /// <c>EasyLib.OHLCMulti5</c> — d0 (sessione in corso, barra inclusa) e d1..d5 — cosi' i pattern
    /// di <c>EasyLib</c> si usano senza adattarli.
    /// </summary>
    protected abstract TradeSignal Evaluate(OhlcvData[] data, OhlcvData bar, decimal[] ohlc);

    // ------------------------------------------------------------------ lettura dello stato

    /// <summary>Indice della barra corrente nella sessione, da 0.</summary>
    protected int SessionBarIndex => _pt5.CurrentBarIndex;

    /// <summary>Massimo e minimo della sessione in corso, barra corrente inclusa.</summary>
    protected (decimal High, decimal Low) CurrentSessionRange => (_pt5.H0, _pt5.L0);

    /// <summary>
    /// Massimo e minimo della sessione in corso <b>esclusa</b> la barra corrente; <c>null</c> se la
    /// barra corrente e' la prima della sessione.
    /// </summary>
    protected (decimal High, decimal Low)? CurrentSessionRangeBeforeBar =>
        _pt5.HasBarBeforeInSession ? (_pt5.H0BeforeBar, _pt5.L0BeforeBar) : null;

    /// <summary>Close della barra precedente (anche della sessione precedente); <c>null</c> se non c'e'.</summary>
    protected decimal? PreviousBarClose => _pt5.HasPreviousBar ? _pt5.PreviousBarClose : null;

    /// <summary>
    /// Massimo e minimo delle ultime <paramref name="sessions"/> sessioni chiuse (1..5).
    /// </summary>
    protected (decimal High, decimal Low) ClosedSessionsRange(int sessions)
    {
        var high = _pt5.H1;
        var low = _pt5.L1;
        if (sessions >= 2) { high = Math.Max(high, _pt5.H2); low = Math.Min(low, _pt5.L2); }
        if (sessions >= 3) { high = Math.Max(high, _pt5.H3); low = Math.Min(low, _pt5.L3); }
        if (sessions >= 4) { high = Math.Max(high, _pt5.H4); low = Math.Min(low, _pt5.L4); }
        if (sessions >= 5) { high = Math.Max(high, _pt5.H5); low = Math.Min(low, _pt5.L5); }
        return (high, low);
    }

    /// <summary>
    /// ATR50 della sessione in corso: le 50 sessioni chiuse prima di essa. E' quello con cui la
    /// ricerca sposta i <b>livelli</b>, fissi per la sessione.
    /// </summary>
    protected decimal SessionAtr => _pt5.Atr;

    /// <summary>
    /// ATR50 della sessione in cui l'ordine si riempirebbe, cioè quello con cui la ricerca misura
    /// <b>stop e target</b> ("vale quella della sessione d'ingresso per tutto il trade"). Coincide con
    /// <see cref="SessionAtr"/> salvo quando la barra di segnale e' l'ultima della sessione: allora
    /// la sessione in corso si chiude prima dell'ingresso ed entra nella media.
    /// </summary>
    protected decimal? EntrySessionAtr(DateTime fillBarUtc)
    {
        if (ResearchSessionDay(fillBarUtc) == _pt5.CurrentDay || _pt5.CurrentIsPartial)
            return _pt5.SessionsClosed >= AtrPeriod ? _pt5.Atr : null;

        var trueRange = TrueRange(_pt5.H0, _pt5.L0, _pt5.HasPreviousSessionClose ? _pt5.PreviousSessionClose : null);
        var (count, _, atr) = AddTrueRange(_pt5.SessionsClosed, _pt5.TrueRangeSum, _pt5.Atr, trueRange, AtrPeriod);
        return count >= AtrPeriod ? atr : null;
    }

    /// <summary>
    /// Sessioni dell'<b>ATR giornaliero del motore</b>, 0 = il motore non ne usa. E' un secondo ATR di
    /// Wilder sulle sessioni chiuse, con un periodo suo: lo chiede la VBO con <c>vol_source = 2</c>
    /// (consegna v5.1), che misura la volatilita' con «l'ATR giornaliero a <c>atr_len</c> periodi,
    /// calcolato sulla serie delle sessioni complete».
    /// </summary>
    protected virtual int DailyAtrPeriod => 0;

    /// <summary>
    /// L'ATR giornaliero del motore per la sessione in corso: le sessioni chiuse prima di essa.
    /// <c>null</c> finche' non ne sono entrate <see cref="DailyAtrPeriod"/>.
    ///
    /// <para><b>Wilder, non media semplice</b>, misurato il 01/10/2026 sui prezzi d'ingresso della
    /// ricerca (livello = O_d0 + k × VOL): VOL implicito / Wilder vale 1,0000 in mediana e al primo
    /// decile su NQ-4H-VBO-cc305f (52 trade) e NQ-15M-VBO-806050; con la media semplice del range
    /// vero 0,93-0,99, con quella dei range 0,94-1,02.</para>
    /// </summary>
    protected decimal? DailySessionsAtr =>
        DailyAtrPeriod > 0 && _pt5.DailyAtrSessions >= DailyAtrPeriod ? _pt5.DailyAtr : null;

    /// <summary>
    /// Barre dell'<b>ATR di barra del motore</b>, 0 = il motore non ne usa: un ATR di Wilder sulle
    /// barre di mercato del timeframe. Lo chiede la VBO con <c>vol_source = 3</c>.
    /// </summary>
    protected virtual int BarAtrPeriod => 0;

    /// <summary>
    /// L'ATR di barra del motore <b>fino alla barra prima di quella corrente</b>
    /// (<c>atr(df, n).shift(1)</c> della ricerca); <c>null</c> finche' non sono entrate
    /// <see cref="BarAtrPeriod"/> barre.
    ///
    /// <para><b>Wilder, e senza la barra di segnale</b>, misurato il 01/10/2026 sui 120 ingressi long
    /// dell'anno broker di ES-1H-VBO-0b900e (livello = O_d0 + 0,7 × VOL): con Wilder fino alla barra
    /// prima del segnale 104 livelli tornano entro il 2 per mille (mediana e primo decile 1,0000);
    /// con la media semplice del range vero, che il motore usava fino a quel giorno, 1 su 120
    /// (mediana 1,05). Era il motivo per cui ES-1H-VBO-195d04 della v5.0 ritrovava l'82% dei trade.</para>
    /// </summary>
    protected decimal? BarAtrBeforeCurrentBar =>
        BarAtrPeriod > 0 && _pt5.BarAtrBarsBeforeBar >= BarAtrPeriod ? _pt5.BarAtrBeforeBar : null;

    /// <summary>
    /// Massimo e minimo delle ultime <paramref name="bars"/> barre di mercato, barra corrente
    /// inclusa. Sul DAX le barre fuori fascia non esistono per la ricerca e si saltano; per gli
    /// altri simboli sono semplicemente le ultime barre.
    /// </summary>
    protected (decimal High, decimal Low) RecentBarsRange(OhlcvData[] data, int bars)
    {
        var high = decimal.MinValue;
        var low = decimal.MaxValue;
        var taken = 0;
        for (var index = data.Length - 1; index >= 0 && taken < bars; index--)
        {
            if (!InResearchMarket(data[index].DateTime))
                continue;

            high = Math.Max(high, data[index].High);
            low = Math.Min(low, data[index].Low);
            taken++;
        }

        return (high, low);
    }

    /// <summary>
    /// Le ultime <paramref name="count"/> barre di mercato, in ordine cronologico, a partire da
    /// <paramref name="barsAgo"/> barre prima della corrente. <c>null</c> se la finestra non basta.
    /// </summary>
    protected OhlcvData[]? RecentMarketBars(OhlcvData[] data, int count, int barsAgo = 0)
    {
        var result = new OhlcvData[count];
        var skipped = 0;
        var filled = count;
        for (var index = data.Length - 1; index >= 0 && filled > 0; index--)
        {
            if (!InResearchMarket(data[index].DateTime))
                continue;
            if (skipped < barsAgo)
            {
                skipped++;
                continue;
            }

            result[--filled] = data[index];
        }

        return filled == 0 ? result : null;
    }

    // ------------------------------------------------------------------ finestra e calendario

    /// <summary>
    /// La finestra operativa dichiarata, estremi inclusi, letta sulla chiusura della barra. Una
    /// PT5DAV la dichiara sempre, anche a giornata piena.
    /// </summary>
    protected bool InTradingWindow(DateTime barTime) =>
        FoldsBars
            ? EasyLib.TimeWindowInclusive(
                TradingWindow!.Start, TradingWindow.End, WindowClock.BarLabelTime(barTime, BarMinutes))
            : InDeclaredWindow(barTime) ?? true;

    /// <summary>Il giorno pandas della barra (0 = lunedi'), letto sulla chiusura come la ricerca.</summary>
    protected bool IsExcludedDay(DateTime barTime, int excludedDay) =>
        excludedDay >= 0 &&
        (FoldsBars
            ? ((int)WindowClock.BarLabelDay(barTime, BarMinutes).DayOfWeek + 6) % 7
            : PythonWeekday(barTime)) == excludedDay;

    /// <summary>
    /// Finestra della ricerca da <c>start_hour</c>/<c>end_hour</c> verbatim; -1 = nessun limite da
    /// quel lato (e -1/-1 = tutta la giornata).
    /// </summary>
    protected static ZonedWindow ResearchWindow(int startHour, int endHour) =>
        ZonedWindow.Research(
            ResearchHourOrOff(startHour, TimeOnly.MinValue),
            ResearchHourOrOff(endHour, ZonedWindow.EndOfDay));

    // ------------------------------------------------------------------ chiusura del segnale

    /// <summary>
    /// Completa un ingresso con le regole comuni della ricerca. <c>null</c> = l'ingresso non nasce.
    /// <list type="bullet">
    ///   <item>nessun ordine vivo a CFD chiuso, ne' fuori dalla fascia del DAX;</item>
    ///   <item>stop e target in ATR50 della sessione d'ingresso, in denaro per contratto come ogni
    ///   altro segnale — chi esegue non sa, e non deve sapere, da dove vengono;</item>
    ///   <item>una entrata per sessione e per direzione, se il motore la prevede, applicata al fill;</item>
    ///   <item>intraday: chiusura all'ultima barra entro il limite del CFD. Un ordine che si
    ///   riempirebbe proprio su quella barra non nasce; uno che si riempie dopo (una 4h delle 20:00,
    ///   la riapertura delle 18:05 NY nelle settimane in cui l'ora legale non e' allineata) chiude a
    ///   fine sessione, come nei trade della ricerca;</item>
    ///   <item>overnight: il <c>max_bars</c> della ricerca e le uscite proprie del motore; le notti le
    ///   limita il piano.</item>
    /// </list>
    /// </summary>
    protected TradeSignal? Finish(TradeSignal? signal, bool oneEntryPerSessionPerSide)
    {
        if (signal is null)
            return null;

        // Un ordine emesso mentre si e' in posizione non esiste: la ricerca non rientra mai sulla
        // barra in cui e' uscita (0 casi sui trade), al piu' su quella dopo. Senza questo blocco
        // l'ordine si riempirebbe nella barra stessa in cui lo stop chiude la posizione — misurato
        // su NQ-15M-BOS: il 21/04/2022 short aperto nello stesso quarto d'ora dello stop del long.
        if (CurrentMP != 0)
            return null;

        // Nessun ingresso a CFD chiuso, in due condizioni misurate sui trade della ricerca:
        //  - per tutte, il CFD deve essere aperto all'APERTURA della barra d'ingresso: la prima 4h
        //    di NQ (00:00 Roma = 18:00 NY, prima delle 18:05) non ha mai ingressi, salvo le
        //    settimane di ora legale sfasata in cui apre alle 19:00 NY;
        //  - per le intraday, anche alla CHIUSURA: una posizione intraday non sta a mercato mentre
        //    il CFD e' chiuso. La 4h delle 20:00 di Roma chiude alle 18:00 NY: NQ-4H-RBBU non vi
        //    entra mai (salvo le 20 volte, tutte a ora legale sfasata, in cui chiude alle 19:00 NY),
        //    NQ-4H-TFM, che tiene la notte, si' (47 ingressi).
        RetimeToResearchBar(signal);
        var fill = signal.ValidFromUtc!.Value;
        var signalDay = ResearchSessionDay(signal.Date);

        // Un ordine STOP o LIMIT non attraversa il cambio di sessione: nato sull'ultima barra, muore
        // con lei. Un ordine A MERCATO si': si esegue all'apertura della prima barra della sessione
        // dopo. Misurato il 01/10/2026 sui trade della consegna v5.1, dove il mercato non chiude fra
        // una sessione e l'altra (la sterlina, 24 ore): sulla barra delle 00:00 i motori stop e
        // limit hanno 0 ingressi su 6.572 trade (PC, VBO, BO, RHL, RBB_M, BIAS_BO), LF a mercato 70
        // su 183. Senza la regola BP-15M-RHL-579220, che compra al minimo della sessione prima e
        // tiene fino a 10 giornate, eseguiva alle 00:00 l'ordine delle 23:45 e la domenica alle
        // 23:00 quello del venerdi': ritrovava il 53% dei trade.
        //
        // Sul DAX il cambio di sessione e' la notte: la barra dopo l'ultima della fascia e' quella
        // delle 08:00 del giorno dopo (ORARIO_MERCATO.md §3). Gli ingressi sulla barra delle 08:00
        // sono solo di motori a mercato — 52 su 475 di FDAX-1H-LFHL-1b7ca6, tutti i 98 e 120 delle
        // due FDAX a 4 ore LF e LF_HL, 74 delle MAC e LF della v5.0. Fino al 01/10/2026 li' l'ordine
        // non nasceva nemmeno a mercato.
        if (!InResearchMarket(fill))
        {
            if (signal.OrderType != TradeOrderType.Market)
                return null;

            fill = FirstMarketBarUtc(NextResearchDay(signalDay));
            signal.ValidFromUtc = fill;
            signal.ExpiresAtUtc = fill;
        }

        // Lo stesso vale per il fine settimana, che sulla sterlina non e' un cambio di sessione: la
        // barra dopo l'ultima del venerdi' (23:00 di Roma, le 17:00 di New York) porta ancora la data
        // di venerdi', ma il mercato e' chiuso e la prima barra vera e' quella della domenica sera.
        // La ricerca li' non esegue lo stop o il limit del venerdi': i suoi ingressi della domenica
        // sono alle 23:15, dall'ordine nato sulla prima barra della settimana (3 su 3 nell'anno
        // broker di BP-15M-RHL-579220; i nostri erano alle 23:00). Un ordine a mercato invece passa:
        // YM-4H-LFHL-addb33 entra sulla prima barra del lunedi' dal segnale del venerdi'.
        if (signal.OrderType != TradeOrderType.Market &&
            (ResearchSessionDay(fill) != signalDay || IsWeekendClose(fill)))
        {
            return null;
        }

        var hours = Pt5DavMarket.Hours(Symbol);
        if (!hours.IsOpenAt(NewYorkClock.TimeOfDay(fill)))
            return null;
        if (IntradayOnly && !hours.IsOpenAt(NewYorkClock.TimeOfDay(fill.AddMinutes(BarMinutes))))
            return null;

        var atr = EntrySessionAtr(fill);
        if (atr is not > 0m)
            return null;

        // Un livello gia' superato quando l'ordine nasce si esegue a mercato all'apertura della
        // barra, come fa il simulatore della ricerca. Scartarlo, la regola delle serie PT3B, toglieva
        // sull'anno broker 229 ingressi alle 41 con tenuta >= 1,5 e 235 k: su GC-15M-TFU 71 trade su
        // 81, perche' il livello e' il massimo di ieri e il prezzo lo rompe spesso prima della
        // finestra delle 10:00. Vedi CrossedLevelPolicy.
        signal.CrossedLevel = CrossedLevelPolicy.Market;

        var pointValue = InstrumentRegistry.PointValue(Symbol);
        signal.StopLoss = null;
        signal.TakeProfit = null;
        signal.StopLossMoneyPerFutureContract =
            StopAtr > 0m ? Math.Round(atr.Value * pointValue * StopAtr, 2) : null;
        signal.TakeProfitMoneyPerFutureContract =
            TargetAtr > 0m ? Math.Round(atr.Value * pointValue * TargetAtr, 2) : null;

        var day = ResearchSessionDay(fill);
        if (oneEntryPerSessionPerSide)
        {
            signal.MaxEntriesPerSession = 1;
            signal.EntrySessionStartUtc = SessionOpenUtc(day);
        }

        // Overnight: nessuna chiusura oltre a max_bars e alle uscite del motore, come la ricerca.
        // Quante notti un conto possa tenere lo decide il piano (AllowOvernight/AllowOverweek): fino
        // al 24/09/2026 qui c'era un tetto di una notte, tolto quando la serie e' stata estesa alle
        // strategie da 2-10 giornate.
        if (IntradayOnly)
        {
            var exit = IntradayExitUtc(day);
            if (fill.AddMinutes(BarMinutes) == exit)
                return null;

            signal.CloseAtUtc = (fill < exit ? exit : SessionEndUtc(day)).AddMinutes(-1);
        }

        // max_bars della ricerca NON conta la barra d'ingresso: con max_bars = 46 si esce alla
        // chiusura della 46a barra dopo quella d'ingresso. MaxBarsInPosition la conta, quindi +1.
        // Misurato sulla riconciliazione NQ: senza, 105 uscite su 116 (NQ-30M-LF) e 9 su 9
        // (NQ-4H-LFHL) cadevano esattamente una barra prima.
        signal.MaxBarsInPosition = MaxBars > 0 ? MaxBars + 1 : null;

        // Dove la ricerca taglia le barre a una fascia (il DAX, 08-22) le barre le conta il motore e
        // non chi esegue: vedi CountsMarketBarsInPosition.
        if (CountsMarketBarsInPosition)
            signal.MaxBarsInPosition = null;

        return signal;
    }

    /// <summary>
    /// Vero se l'istante cade nella chiusura di fine settimana del CFD: dalle 17:00 di New York del
    /// venerdi' alle 17:00 della domenica. Non vale per chi tratta 7 giorni su 7 (BTC). Gli orari
    /// del CFD sono orari del giorno e non lo dicono: la sterlina non ne ha («24 ore su 24 nei giorni
    /// di mercato»).
    /// </summary>
    private bool IsWeekendClose(DateTime instantUtc)
    {
        if (Pt5DavMarket.TradesOnWeekends(Symbol))
            return false;

        var weekClose = new TimeOnly(17, 0);
        var time = NewYorkClock.TimeOfDay(instantUtc);
        return NewYorkClock.SessionDay(instantUtc).DayOfWeek switch
        {
            DayOfWeek.Friday => time >= weekClose,
            DayOfWeek.Saturday => true,
            DayOfWeek.Sunday => time < weekClose,
            _ => false
        };
    }

    /// <summary>
    /// Vero dove <c>max_bars</c> lo conta il motore, sulle barre di mercato che valuta, e non chi
    /// esegue: i simboli di cui la ricerca taglia le barre a una fascia (il DAX, 08-22) con una
    /// tenuta a barre.
    ///
    /// <para><b>Perche'.</b> <c>MaxBarsInPosition</c> conta le barre che il simbolo stampa, e sul DAX
    /// il feed ne ha anche di notte, quando il future e il CFD trattano e la ricerca no: la
    /// posizione scadeva ore prima. Misurato il 01/10/2026 sulle FDAX overnight della v5.1: stessa
    /// barra d'uscita della ricerca nel 4-27% dei trade appaiati (FDAX-1H-LFHL 4%, FDAX-15M-BO 6%,
    /// FDAX-30M-LF 10%) contro il 90-100% degli altri simboli.</para>
    ///
    /// <para><b>Perche' contate e non una data.</b> Una scadenza calcolata all'ingresso sulla
    /// griglia 08-22 non conosce i festivi, e la ricerca conta le barre che esistono: provata, usciva
    /// una giornata prima a Capodanno, a Pasqua e al Primo maggio (3 uscite diverse su 29 trade di
    /// FDAX-30M-LF-b84c31, una delle quali a 345 punti di distanza). L'uscita e' un segnale
    /// <c>ExitOnly</c> a mercato, come l'incrocio inverso della MAC: senza server la posizione
    /// resta al piano, che ha il suo flat.</para>
    /// </summary>
    private bool CountsMarketBarsInPosition =>
        MaxBars > 0 && Pt5DavMarket.ResearchMarketHours(Symbol) is not null;

    // ------------------------------------------------------------------ sessioni della ricerca

    /// <summary>
    /// Il giorno di sessione della ricerca per una barra: la data di Roma dall'ancoraggio, con
    /// sabato e domenica accodati al lunedi'.
    ///
    /// <para><b>Non e' la giornata di New York</b>, ed e' stato misurato il 24/09/2026: con la
    /// sessione dalle 17:00 NY, BP-15M-BIAS (che conta le barre della sessione) passava dal 93% al
    /// 3% dei trade della ricerca ritrovati sul broker; con la mezzanotte di Roma torna. Lo scarto di
    /// BP-30M-RHL che l'aveva fatto pensare era un'altra cosa: l'ora 17-18 NY mancava nel feed.</para>
    /// </summary>
    protected DateTime ResearchSessionDay(DateTime barUtc)
    {
        var shifted = barUtc - SessionStart.ToTimeSpan();
        var day = Clock.SessionDay(shifted);
        if (Pt5DavMarket.TradesOnWeekends(Symbol))
            return day;

        return Clock.SessionDay(shifted).DayOfWeek switch
        {
            DayOfWeek.Saturday => day.AddDays(2),
            DayOfWeek.Sunday => day.AddDays(1),
            _ => day
        };
    }

    /// <summary>
    /// Il giorno di sessione successivo, saltando il fine settimana. Il giorno e' gia' un giorno di
    /// sessione (una data di Roma), non l'istante di una barra: il giorno della settimana si ricava
    /// per differenza da un lunedi' noto.
    /// </summary>
    protected static DateTime NextResearchDay(DateTime day)
    {
        var next = day.Date.AddDays(1);
        while (DaysSinceMonday(next) >= 5)
            next = next.AddDays(1);
        return next;
    }

    private static readonly DateTime KnownMonday = new(2000, 1, 3);

    /// <summary>Giorno della settimana di un giorno di sessione, pandas: 0 = lunedi' … 6 = domenica.</summary>
    protected static int DaysSinceMonday(DateTime day) =>
        (int)(((day.Date - KnownMonday).Days % 7 + 7) % 7);

    /// <summary>Apertura della sessione del giorno indicato (ancoraggio, ora di Roma).</summary>
    protected DateTime SessionOpenUtc(DateTime day) => Clock.ToUtc(day.Add(SessionStart.ToTimeSpan()));

    /// <summary>Fine della sessione: l'ancoraggio del giorno dopo, o la chiusura della fascia del DAX.</summary>
    protected DateTime SessionEndUtc(DateTime day) =>
        Pt5DavMarket.ResearchMarketHours(Symbol) is { } hours
            ? Clock.ToUtc(day.Add(hours.Closes.ToTimeSpan()))
            : Clock.ToUtc(day.AddDays(1).Add(SessionStart.ToTimeSpan()));

    /// <summary>
    /// Fine dell'ultima barra della sessione che chiude entro il limite intraday del CFD (ora di New
    /// York dello stesso giorno), o la fine della sessione se arriva prima (il DAX alle 22:00). La
    /// griglia delle barre e' quella della sessione della ricerca, ancorata in ora di Roma.
    /// </summary>
    protected DateTime IntradayExitUtc(DateTime day)
    {
        var limitUtc = NewYorkClock.ToUtc(day.Add(Pt5DavMarket.Hours(Symbol).IntradayLimit.ToTimeSpan()));
        // Le barre si contano dalla prima della sessione sulla griglia del feed: coincide con
        // l'apertura della sessione ovunque, tranne sul DAX a 4 ore (vedi FirstMarketBarUtc).
        var openLocal = Clock.ToSessionTime(FirstMarketBarUtc(day));
        var limitLocal = Clock.ToSessionTime(limitUtc);
        var bars = Math.Floor((limitLocal - openLocal).TotalMinutes / BarMinutes);
        var exitUtc = Clock.ToUtc(openLocal.AddMinutes(bars * BarMinutes));
        var end = SessionEndUtc(day);
        return exitUtc < end ? exitUtc : end;
    }

    /// <summary>
    /// Apertura della prima barra della sessione <paramref name="day"/> <b>sulla griglia su cui il
    /// motore ragiona</b>. E' l'apertura della sessione, salvo quando la griglia del feed e l'inizio
    /// della sessione della ricerca non sono allineati: la griglia oltre l'ora e' ancorata all'inizio
    /// sessione del <i>simbolo</i> (il calendario: 01:00 per il DAX), la sessione della ricerca v5 sul
    /// DAX comincia alle 08:00, e una serie a 240 minuti del feed avrebbe la prima barra dentro la
    /// fascia alle 09:00. Dove il motore piega le barre (<see cref="BarMinutes"/>) la griglia e' la
    /// sua, e parte dall'apertura della sessione.
    /// </summary>
    protected DateTime FirstMarketBarUtc(DateTime day)
    {
        if (FoldsBars)
            return SessionOpenUtc(day);

        var gridAnchor = MarketCalendarRegistry.Current.Get(Symbol).SessionStart;
        var offset = (int)(SessionStart.ToTimeSpan() - gridAnchor.ToTimeSpan()).TotalMinutes % TimeframeMinutes;
        if (offset < 0)
            offset += TimeframeMinutes;

        var shift = (TimeframeMinutes - offset) % TimeframeMinutes;
        return Clock.ToUtc(day.Add(SessionStart.ToTimeSpan()).AddMinutes(shift));
    }

    /// <summary>
    /// La chiusura del CFD nel giorno indicato (ora di New York), o il suo limite intraday dove il
    /// CFD non chiude mai (la sterlina, alle 17:00).
    /// </summary>
    protected DateTime CfdCloseUtc(DateTime day)
    {
        var hours = Pt5DavMarket.Hours(Symbol);
        return NewYorkClock.ToUtc(day.Add((hours.Closes ?? hours.IntradayLimit).ToTimeSpan()));
    }

    /// <summary>Vero se la barra che apre in <paramref name="barUtc"/> sta nella fascia di mercato della ricerca.</summary>
    protected bool InResearchMarket(DateTime barUtc)
    {
        if (Pt5DavMarket.ResearchMarketHours(Symbol) is not { } hours)
            return true;

        var time = Clock.TimeOfDay(barUtc);
        return time >= hours.Opens && time < hours.Closes;
    }

    /// <summary>
    /// Vero se il CFD tratta almeno un minuto fra <paramref name="fromUtc"/> (incluso) e
    /// <paramref name="toUtc"/> (escluso), secondo gli orari misurati sul broker: cioe' se una barra
    /// con quegli estremi <b>esiste</b>. Una barra tutta dentro la pausa giornaliera (16:50-18:05 di
    /// New York per indici, oro e petrolio) non ha minuti e la ricerca non la vede.
    /// </summary>
    protected bool CfdTradesWithin(DateTime fromUtc, DateTime toUtc)
    {
        var hours = Pt5DavMarket.Hours(Symbol);
        for (var minute = fromUtc; minute < toUtc; minute = minute.AddMinutes(1))
        {
            if (hours.IsOpenAt(NewYorkClock.TimeOfDay(minute)))
                return true;
        }

        return false;
    }

    private SessionClock NewYorkClock => _newYork ??= new SessionClock(Pt5DavMarket.NewYorkTimeZone);

    // ------------------------------------------------------------------ aggiornamento dello stato

    private void Advance(OhlcvData[] data)
    {
        var start = 0;
        if (_pt5.LastBarUtc == default || data[0].DateTime > _pt5.LastBarUtc)
        {
            // Stato assente, o finestra che non contiene piu' l'ultima barra vista: si ricostruisce.
            _pt5 = default;
        }
        else
        {
            start = data.Length;
            while (start > 0 && data[start - 1].DateTime > _pt5.LastBarUtc)
                start--;
        }

        for (var index = start; index < data.Length; index++)
            Fold(data[index]);
    }

    private void Fold(OhlcvData bar)
    {
        _pt5.LastBarUtc = bar.DateTime;
        if (!InResearchMarket(bar.DateTime))
        {
            _pt5.LastBarInMarket = false;
            return;
        }

        _pt5.LastBarInMarket = true;
        var day = ResearchSessionDay(bar.DateTime);

        if (BarAtrPeriod > 0)
        {
            // C0 e' ancora la chiusura della barra di mercato precedente, anche se di un'altra sessione.
            var trueRange = TrueRange(bar.High, bar.Low, _pt5.CurrentDay == default ? null : _pt5.C0);
            _pt5.BarAtrBarsBeforeBar = _pt5.BarAtrBars;
            _pt5.BarAtrBeforeBar = _pt5.BarAtr;
            (_pt5.BarAtrBars, _pt5.BarTrueRangeSum, _pt5.BarAtr) =
                AddTrueRange(_pt5.BarAtrBars, _pt5.BarTrueRangeSum, _pt5.BarAtr, trueRange, BarAtrPeriod);
        }

        if (_pt5.CurrentDay == default)
        {
            StartSession(day, bar, partial: true);
            return;
        }

        _pt5.PreviousBarClose = _pt5.C0;
        _pt5.HasPreviousBar = true;

        if (day != _pt5.CurrentDay)
        {
            CloseSession();
            StartSession(day, bar, partial: false);
            return;
        }

        _pt5.H0BeforeBar = _pt5.H0;
        _pt5.L0BeforeBar = _pt5.L0;
        _pt5.HasBarBeforeInSession = true;
        _pt5.H0 = Math.Max(_pt5.H0, bar.High);
        _pt5.L0 = Math.Min(_pt5.L0, bar.Low);
        _pt5.C0 = bar.Close;
        _pt5.CurrentBarIndex++;
    }

    private void StartSession(DateTime day, OhlcvData bar, bool partial)
    {
        _pt5.CurrentDay = day;
        _pt5.CurrentIsPartial = partial;
        _pt5.CurrentBarIndex = 0;
        _pt5.O0 = bar.Open;
        _pt5.H0 = bar.High;
        _pt5.L0 = bar.Low;
        _pt5.C0 = bar.Close;
        _pt5.H0BeforeBar = 0m;
        _pt5.L0BeforeBar = 0m;
        _pt5.HasBarBeforeInSession = false;
    }

    private void CloseSession()
    {
        // Una sessione cominciata prima della finestra ha OHLC parziali: non entra nell'ATR, ma la
        // sua chiusura e' vera e serve al range vero della sessione dopo.
        if (!_pt5.CurrentIsPartial)
        {
            var trueRange = TrueRange(_pt5.H0, _pt5.L0,
                _pt5.HasPreviousSessionClose ? _pt5.PreviousSessionClose : null);
            (_pt5.SessionsClosed, _pt5.TrueRangeSum, _pt5.Atr) =
                AddTrueRange(_pt5.SessionsClosed, _pt5.TrueRangeSum, _pt5.Atr, trueRange, AtrPeriod);

            if (DailyAtrPeriod > 0)
            {
                (_pt5.DailyAtrSessions, _pt5.DailyTrueRangeSum, _pt5.DailyAtr) =
                    AddTrueRange(_pt5.DailyAtrSessions, _pt5.DailyTrueRangeSum, _pt5.DailyAtr, trueRange, DailyAtrPeriod);
            }
        }

        _pt5.PreviousSessionClose = _pt5.C0;
        _pt5.HasPreviousSessionClose = true;

        (_pt5.O5, _pt5.H5, _pt5.L5, _pt5.C5) = (_pt5.O4, _pt5.H4, _pt5.L4, _pt5.C4);
        (_pt5.O4, _pt5.H4, _pt5.L4, _pt5.C4) = (_pt5.O3, _pt5.H3, _pt5.L3, _pt5.C3);
        (_pt5.O3, _pt5.H3, _pt5.L3, _pt5.C3) = (_pt5.O2, _pt5.H2, _pt5.L2, _pt5.C2);
        (_pt5.O2, _pt5.H2, _pt5.L2, _pt5.C2) = (_pt5.O1, _pt5.H1, _pt5.L1, _pt5.C1);
        (_pt5.O1, _pt5.H1, _pt5.L1, _pt5.C1) = (_pt5.O0, _pt5.H0, _pt5.L0, _pt5.C0);
        _pt5.SessionsInHistory = Math.Min(5, _pt5.SessionsInHistory + 1);
    }

    private static decimal TrueRange(decimal high, decimal low, decimal? previousClose) =>
        previousClose is { } close
            ? Math.Max(high - low, Math.Max(Math.Abs(high - close), Math.Abs(low - close)))
            : high - low;

    /// <summary>
    /// Wilder: media semplice dei primi <paramref name="period"/> range veri, poi
    /// <c>atr += (tr − atr) / period</c>. Sulla storia lunga il seme non si distingue piu' da
    /// <c>ewm(alpha=1/period, adjust=False)</c>: misurato sui 6.728 trade SL di NQ, mediana
    /// distanza di stop / (stop_atr × ATR50) = 1,0045 con entrambi (lo scarto e' lo slippage).
    /// </summary>
    private static (int Count, decimal Sum, decimal Atr) AddTrueRange(
        int count, decimal sum, decimal atr, decimal trueRange, int period)
    {
        count++;
        if (count <= period)
        {
            sum += trueRange;
            if (count == period)
                atr = sum / period;
        }
        else
        {
            atr += (trueRange - atr) / period;
        }

        return (count, sum, atr);
    }

    private decimal[] BuildOhlc() =>
    [
        _pt5.O0, _pt5.H0, _pt5.L0, _pt5.C0,
        _pt5.O1, _pt5.H1, _pt5.L1, _pt5.C1,
        _pt5.O2, _pt5.H2, _pt5.L2, _pt5.C2,
        _pt5.O3, _pt5.H3, _pt5.L3, _pt5.C3,
        _pt5.O4, _pt5.H4, _pt5.L4, _pt5.C4,
        _pt5.O5, _pt5.H5, _pt5.L5, _pt5.C5
    ];
}
