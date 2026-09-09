namespace Piootoo.Shared.Models.Strategies;

/// <summary>
/// La scheda oraria di una strategia: <b>quando taglia le sessioni</b>, <b>quando le è concesso
/// entrare</b> e <b>quando lo strumento sotto di lei non negozia affatto</b>.
///
/// <para><b>Perché serve una scheda e non tre campi.</b> Gli orari di una strategia vivono su tre
/// piani diversi e nessuno dei tre si legge dagli altri. L'<i>ancoraggio di sessione</i> lo dichiara
/// il calendario del simbolo (o un override della classe) e decide dove cadono <c>d0..d5</c>,
/// il secchio di <c>MaxEntriesPerSession</c> e l'uscita di fine sessione. La <i>finestra
/// operativa</i> è <c>start_hour</c>/<c>end_hour</c> del run di ricerca e decide soltanto se un
/// ingresso può nascere. La <i>finestra di negoziazione</i> è del mercato, non della strategia, e
/// toglie barre che il feed di un broker CFD stampa comunque. Tre orologi possibili, due fusi
/// diversi, e un errore su uno qualsiasi non produce barre sbagliate: produce barre <b>diverse</b>,
/// in silenzio.</para>
///
/// <para><b>Cosa è dichiarato e cosa è derivato.</b> Gli <c>Hhmm</c> sono quelli scritti nel codice,
/// verbatim. Gli orari UTC sono <i>derivati</i> dal fuso e servono a rendere confrontabile un run
/// con quello che si vede a chart: cambiano con l'ora legale, ed è per questo che ce ne sono due —
/// gennaio e luglio — invece di uno solo che sembrerebbe fisso.</para>
/// </summary>
public sealed class StrategyHoursCard
{
    /// <summary>Id di classe: la chiave di selezione (masterfilter, catalogo, factory).</summary>
    public string StrategyId { get; set; } = string.Empty;

    /// <summary>Codice di esecuzione (<c>ITradingStrategy.Name</c>): quello di segnali e trade.</summary>
    public string ExecutionCode { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public int TimeframeMinutes { get; set; }

    /// <summary>Barre di storia pretese prima della prima valutazione. Sotto questa soglia il
    /// server salta la strategia in silenzio, e nel dubbio è la prima cosa da guardare.</summary>
    public int RequiredCandles { get; set; }

    /// <summary>Quante sessioni piene coprono <see cref="RequiredCandles"/> a questo timeframe.</summary>
    public double RequiredCandlesInSessions { get; set; }

    /// <summary>Cosa la strategia dichiara di voler tenere; il piano che la esegue può troncarla.</summary>
    public string HoldingLabel { get; set; } = string.Empty;

    /// <summary>
    /// Il confine di sessione della strategia. È sempre nella forma della ricerca
    /// <c>(ancoraggio, 2359)</c>: la giornata piena a partire dall'ancoraggio.
    /// </summary>
    public StrategyHoursWindow Session { get; set; } = new();

    /// <summary>Ora di inizio sessione effettivamente usata da questa strategia.</summary>
    public int SessionAnchorHour { get; set; }

    /// <summary>Ora di inizio sessione che il calendario dichiara per il simbolo.</summary>
    public int CalendarSessionStartHour { get; set; }

    /// <summary>
    /// Perché questa strategia non segue l'ancoraggio del proprio simbolo. <c>null</c> = lo segue,
    /// che è il caso normale.
    /// </summary>
    public string? SessionAnchorOverrideReason { get; set; }

    /// <summary>
    /// La finestra in cui la strategia può aprire posizioni. <c>null</c> = non dichiarata, cioè
    /// opera per tutta la sessione — vedi <see cref="TradingWindowNote"/>.
    /// </summary>
    public StrategyHoursWindow? TradingWindow { get; set; }

    /// <summary>Frase pronta da leggere su cosa la finestra operativa concede e cosa esclude.</summary>
    public string TradingWindowNote { get; set; } = string.Empty;

    /// <summary>Fuso in cui i run di ricerca hanno scritto le finestre operative del simbolo.</summary>
    public string ResearchTimeZone { get; set; } = string.Empty;

    /// <summary>Fuso in cui tornano gli orari delle sorgenti EasyLanguage del simbolo.</summary>
    public string ExchangeTimeZone { get; set; } = string.Empty;

    /// <summary>
    /// I giorni in cui il simbolo ha una sessione nella ricerca, letti sull'orologio della ricerca.
    /// Vuoto con <see cref="DeclaresSessionDays"/> falso significa <b>non dichiarato</b>, che è
    /// diverso da "non ne ha".
    /// </summary>
    public IReadOnlyList<string> SessionDays { get; set; } = [];

    public bool DeclaresSessionDays { get; set; }

    /// <summary>
    /// Quando lo strumento negozia davvero, secondo il calendario di mercato. Vuoto = non
    /// dichiarato, e la maschera lascia passare ogni barra invece di spegnere lo strumento.
    /// </summary>
    public IReadOnlyList<StrategyHoursInstrumentWindow> InstrumentTradingWindows { get; set; } = [];

    /// <summary>Cosa non si è potuto risolvere, invece di lasciare un campo vuoto da interpretare.</summary>
    public IReadOnlyList<string> Warnings { get; set; } = [];
}

/// <summary>
/// Una finestra oraria della strategia, con l'orologio in cui è scritta e la sua resa in UTC nelle
/// due stagioni.
/// </summary>
public sealed class StrategyHoursWindow
{
    /// <summary>Orario di inizio come <c>HHMM</c>, verbatim dal codice.</summary>
    public int StartHhmm { get; set; }

    /// <summary>Orario di fine come <c>HHMM</c>, verbatim dal codice.</summary>
    public int EndHhmm { get; set; }

    /// <summary>In quale dei due orologi dello strumento è scritta: "ricerca" o "borsa".</summary>
    public string Clock { get; set; } = string.Empty;

    /// <summary>Il fuso IANA che quell'orologio risolve per questo simbolo.</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>La finestra come si legge: <c>09:00 → 17:00 Europe/Rome</c>.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gli stessi due orari in UTC a gennaio (ora solare).</summary>
    public string WinterUtc { get; set; } = string.Empty;

    /// <summary>Gli stessi due orari in UTC a luglio (ora legale).</summary>
    public string SummerUtc { get; set; } = string.Empty;
}

/// <summary>Una finestra di negoziazione dello strumento, come la dichiara il calendario.</summary>
public sealed class StrategyHoursInstrumentWindow
{
    /// <summary>Apertura, con il proprio ancoraggio: <c>17:00 locale</c> oppure <c>00:15 UTC</c>.</summary>
    public string Open { get; set; } = string.Empty;

    /// <summary>Chiusura, con il proprio ancoraggio.</summary>
    public string Close { get; set; } = string.Empty;

    /// <summary>I giorni, in ora di borsa, in cui una giornata di negoziazione può aprirsi.</summary>
    public IReadOnlyList<string> OpensOn { get; set; } = [];

    /// <summary>Periodo dell'anno in cui vale, <c>MM-dd</c>; vuoto = tutto l'anno.</summary>
    public string? From { get; set; }

    /// <summary>Ultimo giorno dell'anno in cui vale, <c>MM-dd</c>; vuoto = tutto l'anno.</summary>
    public string? To { get; set; }

    /// <summary>Come è stata accertata, quando il calendario lo dichiara.</summary>
    public string? Source { get; set; }

    /// <summary>La finestra come si legge, giorni compresi.</summary>
    public string Label { get; set; } = string.Empty;
}
