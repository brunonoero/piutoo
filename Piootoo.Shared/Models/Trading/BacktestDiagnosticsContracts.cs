using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Schema dei log diagnostici prodotti da un backtest locale.
///
/// Sono due artefatti complementari, entrambi pensati per essere letti in modo automatico
/// (anche da un agente) dopo l'esecuzione:
///
/// - <see cref="LogFileName"/>: JSON Lines append-only, una riga per evento rilevante
///   (segnale, ingresso, uscita, anomalia). Non contiene le valutazioni che restituiscono
///   Hold né gli skip: sarebbero milioni di righe e nasconderebbero il segnale utile.
/// - <see cref="SummaryFileName"/>: un solo oggetto con i contatori aggregati per strategia
///   (incluse valutazioni e skip per motivo) e una lista di diagnosi generate automaticamente.
///   È il file da leggere per capire perché un backtest non ha prodotto trade.
/// </summary>
public static class BacktestDiagnosticsSchema
{
    public const int Version = 1;
    public const string LogFileName = "backtest-log.jsonl";
    public const string SummaryFileName = "backtest-summary.json";
}

/// <summary>Tipo di evento registrato in <see cref="BacktestDiagnosticsSchema.LogFileName"/>.</summary>
public enum BacktestLogEventType
{
    /// <summary>Inizio o fine del job, con la configurazione effettiva.</summary>
    Run,

    /// <summary>Esito del pre-caricamento di un datasource (symbol + timeframe).</summary>
    DataSource,

    /// <summary>Una strategia ha emesso un segnale diverso da Hold.</summary>
    Signal,

    /// <summary>L'engine ha aperto una posizione.</summary>
    Entry,

    /// <summary>L'engine ha chiuso una posizione.</summary>
    Exit,

    /// <summary>Incoerenza rilevata dall'engine o dalla diagnostica.</summary>
    Anomaly
}

/// <summary>Motivo per cui l'engine ha chiuso una posizione.</summary>
public enum TradeExitReason
{
    Unknown,

    /// <summary>Stop loss iniziale raggiunto.</summary>
    StopLoss,

    /// <summary>Take profit raggiunto.</summary>
    TakeProfit,

    /// <summary>Orario assoluto di flat richiesto dalla strategia (CloseAtUtc).</summary>
    TimeExit,

    /// <summary>Superato il numero massimo di barre in posizione.</summary>
    MaxBars,

    /// <summary>Segnale opposto della stessa strategia sullo stesso simbolo.</summary>
    OppositeSignal,

    /// <summary>
    /// Non più prodotto: le strategie non emettono segnali di chiusura. Lo slot resta per non
    /// rinumerare i valori successivi nei log e nei backtest già archiviati.
    /// </summary>
    [Obsolete("Meccanismo CloseOnly rimosso: l'uscita è descritta nel segnale di ingresso.")]
    CloseOnly,

    /// <summary>Chiusura forzata all'ultima barra della settimana di trading.</summary>
    WeekEnd,

    /// <summary>Chiusura forzata a fine backtest.</summary>
    EndOfRun,

    /// <summary>Stop spostato al prezzo di ingresso dopo l'attivazione del break even.</summary>
    BreakEven,

    /// <summary>Trailing stop protettivo raggiunto.</summary>
    TrailingStop,

    /// <summary>
    /// Flat di sessione imposto dal piano a una posizione che la strategia avrebbe tenuto.
    ///
    /// <para>Distinto da <see cref="TimeExit"/> apposta: quella e' la deadline della strategia,
    /// questa e' un troncamento del conto. Sommarle renderebbe invisibile proprio la differenza
    /// fra cio' che la strategia misura e cio' che il conto le concede.</para>
    /// </summary>
    SessionFlat
}

/// <summary>
/// Riga del log JSONL. I campi non pertinenti al tipo di evento restano null: lo schema è
/// volutamente piatto e uniforme, così una riga si legge senza conoscere il tipo in anticipo.
/// </summary>
public sealed class BacktestLogEvent
{
    public int SchemaVersion { get; init; } = BacktestDiagnosticsSchema.Version;

    /// <summary>Progressivo di scrittura, ricostruisce l'ordine anche dopo un merge di file.</summary>
    public long Sequence { get; set; }

    public required BacktestLogEventType Type { get; init; }
    public string? JobId { get; init; }

    /// <summary>Timestamp della barra in elaborazione (non l'ora di sistema).</summary>
    public DateTime? BarTimeUtc { get; init; }

    public string? StrategyCode { get; init; }
    public string? StrategyName { get; init; }
    public string? Symbol { get; init; }
    public int? TimeframeMinutes { get; init; }

    public SignalType? Side { get; init; }
    public TradeOrderType? OrderType { get; init; }
    public decimal? Price { get; init; }
    public decimal? Quantity { get; init; }

    /// <summary>Stop loss in punti dal prezzo di ingresso, come lo vede l'engine.</summary>
    public decimal? StopLossPoints { get; init; }

    /// <summary>Take profit in punti dal prezzo di ingresso, come lo vede l'engine.</summary>
    public decimal? TakeProfitPoints { get; init; }

    public DateTime? EntryTimeUtc { get; init; }
    public decimal? EntryPrice { get; init; }
    public int? BarsInPosition { get; init; }

    public TradeExitReason? ExitReason { get; init; }
    public decimal? GrossProfit { get; init; }
    public decimal? NetProfit { get; init; }
    public decimal? Commission { get; init; }

    public decimal? Equity { get; init; }
    public decimal? Balance { get; init; }
    public int? OpenPositionsCount { get; init; }

    /// <summary>Motivo dichiarato dalla strategia (campo Reason del segnale) o descrizione dell'evento.</summary>
    public string? Message { get; init; }

    /// <summary>Coppie chiave/valore libere per contesto aggiuntivo, senza allargare lo schema.</summary>
    public IReadOnlyDictionary<string, string>? Data { get; init; }
}

/// <summary>Esito del pre-caricamento di un datasource, riportato nel riepilogo.</summary>
public sealed class BacktestDataSourceSummary
{
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }
    public int CandleCount { get; init; }
    public DateTime? FirstBarUtc { get; init; }
    public DateTime? LastBarUtc { get; init; }

    /// <summary>False quando il feed non copre l'intero intervallo richiesto dal backtest.</summary>
    public bool CoversRequestedRange { get; init; }

    /// <summary>Descrizione del problema quando il datasource è vuoto o incompleto.</summary>
    public string? Warning { get; init; }
}

/// <summary>
/// Contatori aggregati di una singola strategia. Gli skip non finiscono nel log evento per
/// evento: si contano qui, ed è qui che si vede se una strategia non ha mai avuto dati a
/// sufficienza per essere valutata davvero.
/// </summary>
public sealed class BacktestStrategySummary
{
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }

    /// <summary>
    /// Quante candele la strategia pretende prima di poter essere valutata
    /// (<c>ITradingStrategy.RequiredCandles</c>).
    ///
    /// <para><b>Perche' e' qui.</b> <see cref="SkippedNotEnoughCandles"/> da solo non si legge: dice
    /// quante barre sono state saltate ma non contro quale soglia, e la soglia varia di venti volte
    /// fra strategie sullo stesso stream. <c>PTS_NQ_VBO_002_240</c> ne chiede 606 e sullo stesso
    /// <c>@NQ_240</c> le altre ne chiedono 36: senza questo numero nell'artefatto, un riscaldamento
    /// che mangia meta' del run sembra un difetto del feed invece che della strategia. E' anche il
    /// numero che spiega perche' una strategia non opera mai in sessione, dove la storia la spinge
    /// il client.</para>
    /// </summary>
    public int RequiredCandles { get; set; }

    /// <summary>Barre in cui la strategia era allineata al proprio timeframe e quindi candidata alla valutazione.</summary>
    public long Scheduled { get; set; }

    /// <summary>Valutazioni realmente eseguite (Evaluate chiamata).</summary>
    public long Evaluations { get; set; }

    /// <summary>Skip perché il datasource non esiste o è vuoto.</summary>
    public long SkippedNoData { get; set; }

    /// <summary>Skip perché le candele disponibili sono meno di RequiredCandles.</summary>
    public long SkippedNotEnoughCandles { get; set; }

    /// <summary>Skip perché l'ultima candela disponibile è troppo vecchia rispetto alla barra corrente.</summary>
    public long SkippedStaleCandle { get; set; }

    /// <summary>Eccezioni catturate durante la valutazione.</summary>
    public long Errors { get; set; }

    public long HoldSignals { get; set; }
    public long BuySignals { get; set; }
    public long SellSignals { get; set; }

    /// <summary>
    /// Segnali di ingresso privi di qualsiasi condizione di uscita (StopLoss, TakeProfit,
    /// CloseAtUtc, MaxBarsInPosition). L'engine non può chiuderli: la posizione resta aperta fino
    /// alla chiusura tecnica di fine settimana o fine run. Va letto come un difetto della strategia.
    /// </summary>
    public long SignalsWithoutExitSpec { get; set; }

    public int Trades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal Commission { get; set; }

    /// <summary>Conteggio delle uscite per motivo (chiave = <see cref="TradeExitReason"/>).</summary>
    public Dictionary<string, int> ExitReasons { get; init; } = new(StringComparer.Ordinal);

    public DateTime? FirstSignalUtc { get; set; }
    public DateTime? LastSignalUtc { get; set; }

    /// <summary>Diagnosi automatica quando i contatori indicano un problema; null se tutto coerente.</summary>
    public string? Diagnosis { get; set; }
}

/// <summary>Riepilogo completo di un run di backtest.</summary>
public sealed class BacktestRunSummary
{
    public int SchemaVersion { get; init; } = BacktestDiagnosticsSchema.Version;
    public required string JobId { get; init; }
    public string SetupName { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public string BacktestFolder { get; init; } = string.Empty;

    public DateTime RequestedStartUtc { get; init; }
    public DateTime RequestedEndUtc { get; init; }
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public double DurationSeconds { get; set; }

    public int MinTimeframeMinutes { get; init; }
    public long PlannedIterations { get; init; }
    public long ProcessedIterations { get; set; }

    /// <summary>Barre in cui almeno un prezzo era disponibile e l'engine ha aggiornato il mark-to-market.</summary>
    public long MarkedToMarketBars { get; set; }

    public decimal InitialCapital { get; init; }
    public decimal FinalEquity { get; set; }
    public decimal TotalNetProfit { get; set; }
    public decimal MaxDrawdown { get; set; }

    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public int OpenPositionsAtEnd { get; set; }

    /// <summary>
    /// Quanti ingressi il motore ha scartato perche' il livello era gia' oltrepassato quando
    /// l'ordine nasceva (<c>RejectWrongSideLevels</c>).
    ///
    /// <para>Il contatore esisteva gia' sul motore e finiva solo in <c>backtest-log.jsonl</c>: qui
    /// serve perche' e' il numero da mettere accanto a quello del cBot, che nel confronto
    /// 2026-08-28 scartava 3.401 ingressi per lo stesso motivo. Il filtro e' acceso da entrambe le
    /// parti ma decide su dati diversi — il motore sull'apertura della barra, il bot su Bid/Ask
    /// live — e la differenza fra i due conteggi e' la selezione di trade che diverge.</para>
    /// </summary>
    public int WrongSideLevelsRejected { get; set; }

    /// <summary>
    /// Cosa il conto simulato permetteva di tenere, e a che ora tagliava.
    ///
    /// <para>Sta nel summary perche' e' una regola che cambia i risultati senza comparire nei
    /// trade: due run identici con permessi o orari diversi non sono confrontabili, e chi li
    /// rilegge mesi dopo non ha altro modo di accorgersene. Null nei summary scritti prima che la
    /// policy esistesse.</para>
    /// </summary>
    public AccountHoldingPolicy? Holding { get; init; }

    /// <summary>
    /// Archivio di barre da cui il run ha letto: null = datafeed interno, altrimenti il nome del
    /// broker sotto <c>datafeed-external</c>.
    ///
    /// <para>Sta qui per lo stesso motivo di <see cref="Holding"/>: cambia i risultati senza
    /// comparire nei trade. Due run identici su feed diversi divergono sui riempimenti — sono
    /// candele chiuse su prezzi diversi — e chi li rilegge non ha altro modo di saperlo.</para>
    /// </summary>
    public string? DatafeedBroker { get; init; }

    /// <summary>
    /// Conto di cui il run ha applicato l'universo operativo: null = nessuno, il run ha eseguito
    /// l'intero masterfilter.
    ///
    /// <para>Stessa ragione di <see cref="DatafeedBroker"/>: cambia <i>quali</i> strategie girano
    /// senza lasciare traccia nei trade. Un run con conto e uno senza si distinguono solo contando
    /// le strategie, e solo sapendo quante avrebbero dovuto essercene.</para>
    /// </summary>
    public string? AccountNumber { get; init; }

    /// <summary>
    /// Strategie del masterfilter escluse perche' il conto di <see cref="AccountNumber"/> non
    /// prevede il loro simbolo. Vuoto quando non c'e' un conto, o quando li supporta tutti.
    /// </summary>
    public IReadOnlyList<string> StrategiesNotSupportedByAccount { get; init; } = [];

    /// <summary>
    /// Quale tabella di conversione il run ha davvero risolto per <see cref="AccountNumber"/>.
    ///
    /// <para><b>Perche' non basta la lista delle escluse.</b> Una lista vuota ha due significati
    /// opposti — «il conto li supporta tutti» e «la tabella non si e' risolta, quindi passa
    /// tutto» — e i due producono run diversi con lo stesso artefatto. In compare-0017 lo stesso
    /// file di conversione su disco ha dato tre esclusioni diverse in tre run, e non c'era modo di
    /// accorgersene dal summary. Null nei summary scritti prima di questo campo.</para>
    /// </summary>
    public BacktestAccountUniverse? AccountUniverse { get; init; }

    /// <summary>
    /// Le convenzioni di riempimento con cui questo run e' stato eseguito.
    ///
    /// <para>Stessa ragione di <see cref="Holding"/>: cambiano i risultati senza comparire nei
    /// trade. Due run con priorita' intrabarra o passo di trailing diversi non sono confrontabili,
    /// e chi li rilegge non ha altro modo di saperlo. Null nei summary precedenti.</para>
    /// </summary>
    public BacktestFillConventions? FillConventions { get; init; }

    /// <summary>
    /// Quante classi il catalogo espone in tutto, contro quante il masterfilter ne ha selezionate.
    ///
    /// <para>Un run riporta solo cio' che ha schedulato: una classe fuori dal masterfilter e una
    /// classe inesistente hanno lo stesso aspetto — nessuno. I due numeri qui, piu'
    /// <see cref="StrategiesNotInMasterfilter"/>, rendono la differenza leggibile senza aprire il
    /// masterfilter.</para>
    /// </summary>
    public int CatalogStrategies { get; init; }

    /// <summary>Strategie selezionate dal masterfilter, prima del filtro del conto.</summary>
    public int MasterfilterStrategies { get; init; }

    /// <summary>Classi presenti nel catalogo che il masterfilter non ha selezionato.</summary>
    public IReadOnlyList<string> StrategiesNotInMasterfilter { get; init; } = [];

    public string Outcome { get; set; } = "Unknown";
    public string? ErrorMessage { get; set; }

    public IReadOnlyList<BacktestDataSourceSummary> DataSources { get; set; } = [];
    public IReadOnlyList<BacktestStrategySummary> Strategies { get; set; } = [];

    /// <summary>
    /// Problemi rilevati automaticamente confrontando i contatori: datasource vuoti, strategie
    /// mai valutate, segnali che non si trasformano mai in trade, e simili.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; set; } = [];
}

/// <summary>
/// L'universo operativo che il run ha risolto per il conto dichiarato.
///
/// <para>Esiste perche' <c>AccountSymbolConversion.SupportsSymbol</c> ammette <b>tutto</b> quando
/// la tabella e' vuota — e' il conto neutro, quello non ancora mappato — mentre un conto che
/// dichiara un codice tabella e ne risolve zero righe e' una configurazione rotta, non un conto
/// neutro. Finche' i due casi non erano distinguibili nell'artefatto, un run poteva far girare
/// strategie su simboli che il conto ha disabilitati senza che niente lo dicesse.</para>
/// </summary>
public sealed class BacktestAccountUniverse
{
    /// <summary>Conto dichiarato dal run.</summary>
    public string? AccountNumber { get; init; }

    /// <summary>Codice della tabella di conversione dichiarato dal conto. Vuoto = conto non mappato.</summary>
    public string? SymbolConversionCode { get; init; }

    /// <summary>Righe risolte in quella tabella. Zero con un codice dichiarato e' un errore, non un conto neutro.</summary>
    public int MappedSymbols { get; init; }

    /// <summary>Quante di quelle righe sono abilitate: sono i simboli su cui il conto puo' operare.</summary>
    public int EnabledSymbols { get; init; }

    /// <summary>
    /// Vero quando il run non ha ristretto niente perche' la tabella e' assente o vuota. E' il
    /// caso in cui la lista delle escluse e' vuota <i>senza</i> che il conto le supporti davvero.
    /// </summary>
    public bool AppliedAsNeutralAccount { get; init; }
}

/// <summary>
/// Le convenzioni con cui l'engine ha riempito ordini e uscite in questo run.
///
/// <para>Sono gia' documentate nel codice di <c>PiootooTradingService</c>, ma finche' non
/// comparivano nell'artefatto due run con convenzioni diverse erano indistinguibili a posteriori,
/// e la loro differenza vale la stessa grandezza del risultato.</para>
/// </summary>
public sealed class BacktestFillConventions
{
    /// <summary>
    /// Chi vince quando la stessa barra contiene sia lo stop protettivo sia il target.
    /// <c>ProtectiveBeforeTarget</c> e' la convenzione conservativa dell'engine: con sole OHLC
    /// l'ordine reale dei tick non e' ricostruibile e si assume il percorso avverso.
    /// </summary>
    public string IntrabarPriority { get; init; } = "ProtectiveBeforeTarget";

    /// <summary>
    /// Se il picco del trailing include l'estremo della barra in corso, cioe' se lo stop trascinato
    /// puo' scattare sulla stessa barra che ha segnato il nuovo massimo. Il motore di ricerca
    /// Python non lo fa: la differenza si misura solo sapendo com'era impostato il run.
    /// </summary>
    public bool TrailingPeakIncludesCurrentBar { get; init; }

    /// <summary>Passo minimo del trailing, in frazione della distanza dichiarata.</summary>
    public decimal TrailingMinStepFraction { get; init; }

    /// <summary>Se gli ingressi su un livello gia' scavalcato sono stati scartati.</summary>
    public bool RejectWrongSideLevels { get; init; }

    /// <summary>Simboli per cui il run ha applicato uno slippage sul riempimento degli stop protettivi.</summary>
    public IReadOnlyList<string> StopFillSlippageSymbols { get; init; } = [];
}
