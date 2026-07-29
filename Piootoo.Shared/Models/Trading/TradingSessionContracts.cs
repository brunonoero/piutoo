using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Shared.Models.Trading;

public enum ExecutionMode { ServerSimulated, ExternalBroker }
public enum TradingSessionStatus { Created, Running, Stopped }
public enum ExecutionReportStatus { Accepted, PartiallyFilled, Filled, Rejected, Cancelled }
public enum OrderIntentStatus { Pending, Accepted, PartiallyFilled, Filled, Rejected, Cancelled }
public enum QuantityRoundingMode { FuturesContracts, BrokerVolumeStep }

/// <summary>
/// Natura di un <see cref="OrderIntent"/>.
///
/// <para><see cref="Entry"/> — ingresso deciso dal server a partire da un segnale di strategia. Porta
/// con sé l'intera specifica di uscita (SL, TP, uscita a tempo, massimo di barre): il client la
/// applica e chiude in autonomia.</para>
///
/// <para><see cref="Close"/> — <b>registrazione</b> di una chiusura già decisa dal client
/// (SL/TP nativi del broker, uscita a tempo, limite barre). Non è un ordine: serve solo ad avere un
/// intent a cui agganciare l'execution report e generare il <c>PersistedTrade</c>. Il server non
/// emette mai intent di chiusura a partire da un segnale: le strategie che avrebbero bisogno di
/// farlo sono close-dependent e quindi escluse dal catalogo.</para>
/// </summary>
public enum OrderIntentKind { Entry, Close }

/// <summary>
/// Le tre modalità operative dell'engine rispetto al filtro Titano. Valgono identiche per l'engine
/// interno (backtest in-process, <c>ServerSimulated</c>) e per l'engine esterno cTrader
/// (<c>ExternalBroker</c>): il cBot invia le barre e riceve i segnali già filtrati, quindi non
/// conosce Titano e non cambia comportamento tra una modalità e l'altra.
/// </summary>
/// <summary>
/// Contesto in cui sta girando il client che ha creato la sessione. Non è una preferenza: il cBot
/// lo ricava dalla piattaforma (<c>Robot.IsBacktesting</c>), non da un parametro, quindi non può
/// sbagliarlo per distrazione.
///
/// <para>Serve a rendere verificabile lato server una coerenza che altrimenti resterebbe negli
/// occhi dell'operatore: una rotazione storica applicata a una sessione live, o una modalità
/// <see cref="TitanoFilterMode.Realtime"/> usata in backtest, sono errori silenziosi che si notano
/// solo dai risultati sbagliati. Vedi <c>CreateTradingSessionRequest.ClientRunMode</c>.</para>
/// </summary>
public enum ClientRunMode
{
    /// <summary>
    /// Client che non sa dichiararlo (console WinForms, test, integrazioni terze). Le combinazioni
    /// non vengono verificate: la responsabilità resta di chi configura.
    /// </summary>
    Unknown,

    /// <summary>Replay di dati storici: cTrader in backtesting/ottimizzazione, o backtest interno.</summary>
    Backtest,

    /// <summary>Mercato reale, barre che arrivano man mano che si chiudono.</summary>
    Realtime
}

public enum TitanoFilterMode
{
    /// <summary>
    /// Nessun filtro: vengono valutate tutte le strategie del masterfilter. È la modalità con cui si
    /// produce il <c>trades.json</c> su cui l'analisi Titano calcola offline le rotazioni.
    /// </summary>
    Disabled,

    /// <summary>
    /// Backtest con filtro: le rotazioni sono lette dal manifest calcolato <b>offline</b> sui trade
    /// di tutte le strategie del workspace. Per ogni barra vale la decisione del periodo che la
    /// contiene. Una barra fuori dai periodi del manifest è un errore di configurazione (manifest
    /// non allineato all'intervallo di backtest), non un "tutto abilitato".
    /// </summary>
    BacktestRotationFile,

    /// <summary>
    /// Realtime: vale la decisione del periodo corrente prodotto dall'ultima analisi Titano. Le barre
    /// successive alla fine del manifest ricadono sull'ultimo periodo disponibile — è la rotazione
    /// in vigore finché non se ne calcola una nuova — e la cosa viene dichiarata nel rotation-log.
    /// </summary>
    Realtime
}

public sealed class InstrumentMetadata
{
    public required string Symbol { get; init; }
    public decimal DollarsPerPoint { get; init; } = 1m;
    public decimal MinimumQuantity { get; init; } = 1m;
    public decimal QuantityStep { get; init; } = 1m;
    public QuantityRoundingMode RoundingMode { get; init; } = QuantityRoundingMode.FuturesContracts;
}

public sealed class MarketVolatilitySizingConfig
{
    public bool Enabled { get; init; }
    public int AtrPeriods { get; init; } = 14;
    public decimal TargetRiskDollars { get; init; } = 1_000m;
}

public sealed class PortfolioRiskSizingConfig
{
    public bool Enabled { get; init; }
    public decimal MaximumDrawdown { get; init; } = 0.20m;
    public decimal MaximumGrossExposure { get; init; } = 1m;
    public bool EnableCppi { get; init; }
    public decimal CppiFloorFraction { get; init; } = 0.80m;
    public decimal CppiMultiplier { get; init; } = 1m;
    public bool EnableAggressiveModules { get; init; }
    public decimal FractionalFactor { get; init; } = 0.25m;
    public decimal MaximumMultiplier { get; init; } = 1m;
}

public sealed class PositionSizingConfig
{
    public bool ClampMultipliersToUnitInterval { get; init; } = true;
    public MarketVolatilitySizingConfig MarketVolatility { get; init; } = new();
    public PortfolioRiskSizingConfig PortfolioRisk { get; init; } = new();
}

public sealed class CreateTradingSessionRequest
{
    public required string WorkspaceId { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public decimal InitialCapital { get; init; } = 100_000m;
    public decimal CommissionPerContract { get; init; } = 2m;
    public string? ClientSessionToken { get; init; }
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }

    /// <summary>
    /// Modalità operativa rispetto al filtro Titano. Vedi <see cref="TitanoFilterMode"/>.
    ///
    /// <para><see cref="TitanoFilterMode.Disabled"/> non richiede un <see cref="TitanoRunId"/>. Se il
    /// RunId è comunque indicato, la rotazione viene risolta e registrata nel rotation-log senza
    /// essere applicata: serve a confrontare "cosa avrebbe fatto Titano" senza subirne gli effetti.</para>
    ///
    /// <para>Le altre due modalità richiedono <see cref="TitanoRunId"/> e
    /// <see cref="TitanoBacktestFolder"/>.</para>
    /// </summary>
    public TitanoFilterMode TitanoMode { get; init; } = TitanoFilterMode.Disabled;

    /// <summary>
    /// Contesto dichiarato dal client (vedi <see cref="ClientRunMode"/>). Il server lo incrocia con
    /// <see cref="TitanoMode"/> e rifiuta le combinazioni incoerenti:
    ///
    /// <list type="bullet">
    /// <item><see cref="TitanoFilterMode.Realtime"/> in <see cref="ClientRunMode.Backtest"/>: la
    /// rotazione "corrente" verrebbe applicata a barre storiche, e oltre la fine del manifest
    /// resterebbe congelata sull'ultimo periodo — cioè look-ahead mascherato da fallback.</item>
    /// <item><see cref="TitanoFilterMode.BacktestRotationFile"/> in
    /// <see cref="ClientRunMode.Realtime"/>: il tempo live esce quasi subito dall'intervallo del
    /// manifest e la sessione si fermerebbe alla prima barra scoperta.</item>
    /// </list>
    ///
    /// <see cref="TitanoFilterMode.Disabled"/> è legittimo in entrambi i contesti.
    /// </summary>
    public ClientRunMode ClientRunMode { get; init; } = ClientRunMode.Unknown;

    public PositionSizingConfig PositionSizing { get; init; } = new();
    public IReadOnlyList<InstrumentMetadata> Instruments { get; init; } = [];
}

public sealed class TradingSessionDescriptor
{
    public required string SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required string WorkspaceId { get; init; }
    public string? PlanCode { get; init; }
    public string? ExecutionKey { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public required TradingSessionStatus Status { get; init; }
    public string? TitanoRunId { get; init; }

    /// <summary>Modalità operativa rispetto al filtro Titano. Vedi <see cref="TitanoFilterMode"/>.</summary>
    public TitanoFilterMode TitanoMode { get; init; }

    /// <summary>Contesto dichiarato dal client alla creazione. Vedi <see cref="Trading.ClientRunMode"/>.</summary>
    public ClientRunMode ClientRunMode { get; init; }

    public PositionSizingConfig PositionSizing { get; init; } = new();
    public IReadOnlyList<InstrumentMetadata> InstrumentMetadata { get; init; } = [];
    public IReadOnlyList<TradingInstrument> Instruments { get; init; } = [];
}

public sealed class TradingInstrument
{
    public required string Symbol { get; init; }
    public required IReadOnlyList<int> TimeframesMinutes { get; init; }
}

public sealed class ClosedBar
{
    public required string Symbol { get; init; }
    public required int TimeframeMinutes { get; init; }
    public required DateTime BarTimeUtc { get; init; }
    public required long Sequence { get; init; }
    public required string IdempotencyKey { get; init; }
    public required OhlcvData Bar { get; init; }
}

public sealed class PushBarsRequest
{
    public required string SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required IReadOnlyList<ClosedBar> Bars { get; init; }
}

public sealed class PushBarsResponse
{
    public int AcceptedBars { get; init; }
    public int DuplicateBars { get; init; }
    public IReadOnlyList<OrderIntent> Intents { get; init; } = [];
}

public sealed class OrderIntent
{
    public required string IntentId { get; init; }
    public required string SessionId { get; init; }
    public required string StrategyCode { get; init; }
    public string StrategyName { get; init; } = string.Empty;
    public required string Symbol { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public SignalType Side { get; init; }
    public TradeOrderType OrderType { get; init; }
    public decimal Quantity { get; init; }
    public decimal AllocationMultiplier { get; init; } = 1m;
    public decimal BaseQuantity { get; init; }
    public decimal StrategyEquityMultiplier { get; init; } = 1m;
    public decimal MarketVolatilityMultiplier { get; init; } = 1m;
    public decimal PortfolioRiskMultiplier { get; init; } = 1m;
    public decimal FinalQuantity { get; init; }
    public string? SizingReason { get; init; }
    public decimal Price { get; init; }

    /// <summary>Ingresso, oppure registrazione di una chiusura decisa dal client. Vedi <see cref="OrderIntentKind"/>.</summary>
    public OrderIntentKind Kind { get; init; } = OrderIntentKind.Entry;

    /// <summary>Vero per gli intent che chiudono una posizione. Comodità di lettura su <see cref="Kind"/>.</summary>
    public bool IsClose => Kind == OrderIntentKind.Close;

    // --- Specifica di uscita completa, copiata dal segnale di ingresso ---
    // Il client DEVE applicarla per intero: sono le sole informazioni con cui l'engine esterno
    // chiude la posizione, dato che il server non emette più intent di chiusura.

    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }

    /// <summary>Livello di break even in punti: raggiunto il profitto, lo stop va spostato all'entry.</summary>
    public decimal? BreakEven { get; init; }

    /// <summary>Numero massimo di barre in posizione. Null o 0 = nessun limite.</summary>
    public int? MaxBarsInPosition { get; init; }

    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }

    /// <summary>Istante entro cui la posizione va chiusa (uscita a tempo). Null = nessuna scadenza.</summary>
    public DateTime? CloseAtUtc { get; init; }
    public string? Reason { get; init; }
    public OrderIntentStatus Status { get; set; } = OrderIntentStatus.Pending;
    public decimal FilledQuantity { get; set; }
    public string? ExternalOrderId { get; set; }
    /// <summary>Account cTrader a cui questo intent concreto è stato assegnato. Null per i template non ancora reclamati da nessun gruppo.</summary>
    public string? AssignedAccountNumber { get; set; }
    /// <summary>Gruppo (prop firm) a cui appartiene l'account assegnato.</summary>
    public string? AssignedGroupId { get; set; }
}

public sealed class ExternalExecutionReport
{
    public required string ReportId { get; init; }
    public required string IntentId { get; init; }
    public string? ExternalOrderId { get; init; }
    public required ExecutionReportStatus Status { get; init; }
    public decimal CumulativeFilledQuantity { get; init; }
    public decimal? FillPrice { get; init; }
    public decimal Commission { get; init; }
    public required DateTime EventTimeUtc { get; init; }
}

public sealed class ExecutionReportRequest
{
    public required string SessionToken { get; init; }
    public required ExternalExecutionReport Report { get; init; }
}

public sealed class TradingPositionSnapshot
{
    public required string StrategyCode { get; init; }
    public required string Symbol { get; init; }
    public required SignalType Direction { get; init; }
    public decimal Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    /// <summary>Account cTrader proprietario della posizione. Vuoto in modalità legacy senza gruppi.</summary>
    public string AccountNumber { get; init; } = string.Empty;
}

public sealed class TradingSessionSnapshot
{
    public required string SessionId { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public required TradingSessionStatus Status { get; init; }
    public decimal Balance { get; init; }
    public decimal Equity { get; init; }
    public int Entries { get; init; }
    public int Fills { get; init; }
    public IReadOnlyList<TradingPositionSnapshot> Positions { get; init; } = [];
    public IReadOnlyList<OrderIntent> PendingIntents { get; init; } = [];
    /// <summary>Mappa account -> gruppo configurata per la distribuzione dei segnali (anti copy-trading).</summary>
    public IReadOnlyList<AccountGroupMapping> AccountGroups { get; init; } = [];

    /// <summary>Profilo completo gruppo/account/Titano quando configurato via PUT /groups.</summary>
    public IReadOnlyList<TradingGroupRow> Groups { get; init; } = [];
}

/// <summary>
/// Associa un account cTrader a un gruppo (tipicamente una prop firm): gli account dello stesso
/// gruppo non ricevono mai lo stesso segnale di ingresso, account di gruppi diversi sì.
/// </summary>
public sealed class AccountGroupMapping
{
    public required string AccountNumber { get; init; }
    public required string GroupId { get; init; }
}

public sealed class SetAccountGroupsRequest
{
    public required string SessionToken { get; init; }
    public required IReadOnlyList<AccountGroupMapping> Accounts { get; init; }
}

/// <summary>
/// Riga di configurazione gruppo/account con profilo Titano opzionale. Più righe con lo stesso
/// <see cref="GroupId"/> condividono lo stesso profilo Titano del gruppo.
/// </summary>
public sealed class TradingGroupRow
{
    public required string GroupId { get; init; }
    public required string AccountNumber { get; init; }

    /// <summary>
    /// Massimo numero di posizioni contemporanee per questo account. Zero significa illimitato.
    /// Ignorato nel backtest senza Titano.
    /// </summary>
    public int MaxConcurrentTrades { get; init; }

    /// <summary>Riferimento al setup salvato (rotation-setups); metadata per il client, non usato a runtime.</summary>
    public string? RotationSetupId { get; init; }

    /// <summary>Run Titano eseguibile (manifest). Obbligatorio se si applicano filtri Titano al gruppo.</summary>
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }

    /// <summary>
    /// true: al polling account il manifest del gruppo filtra i template e scala la quantità.
    /// false: il gruppo riceve tutti i template compatibili con l'anti copy-trading, indipendentemente dal manifest.
    ///
    /// <para>Non va confuso con <see cref="CreateTradingSessionRequest.TitanoMode"/>: la modalità dice
    /// <i>dove</i> si sta girando ed è della sessione, questo flag dice soltanto se <i>questo</i> gruppo
    /// subisce il filtro del proprio run. Un gruppo senza <see cref="TitanoRunId"/> proprio eredita la
    /// decisione della sessione — filtrata in tutte le modalità tranne
    /// <see cref="TitanoFilterMode.Disabled"/>.</para>
    /// </summary>
    public bool ApplyTitanoFilters { get; init; } = true;
}

public sealed class SetTradingGroupsRequest
{
    public required string SessionToken { get; init; }
    public required IReadOnlyList<TradingGroupRow> Rows { get; init; }
}

/// <summary>Risposta al polling di un account per il prossimo segnale da eseguire.</summary>
public sealed class AccountSignalResponse
{
    /// <summary>Intent da eseguire ora, oppure null se non c'è nulla per questo account.</summary>
    public OrderIntent? Intent { get; init; }
    /// <summary>
    /// Motivo diagnostico quando Intent è null: "NoSignal" (nessun segnale libero per i simboli non
    /// occupati dall'account) o "SessionNotRunning" (la sessione non è in esecuzione).
    /// </summary>
    public string? Reason { get; init; }
    public int OpenPositions { get; init; }
    public int PendingOrders { get; init; }
    public int MaxConcurrentTrades { get; init; }
}

/// <summary>Posizione Piootoo attualmente presente sulla piattaforma del broker.</summary>
public sealed class BrokerPositionSnapshot
{
    public string PositionId { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public string StrategyCode { get; init; } = string.Empty;
}

/// <summary>Ordine Piootoo ancora pendente sulla piattaforma del broker.</summary>
public sealed class BrokerOrderSnapshot
{
    public string OrderId { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public string StrategyCode { get; init; } = string.Empty;
}

/// <summary>Trade Piootoo presente nello storico della piattaforma broker.</summary>
public sealed class BrokerTradeSnapshot
{
    public string PositionId { get; init; } = string.Empty;
    public DateTime ClosingTimeUtc { get; init; }
}

/// <summary>Stato broker inviato dal cBot insieme a ogni richiesta del prossimo segnale.</summary>
public sealed class AccountSignalPollRequest
{
    public required string SessionToken { get; init; }
    public IReadOnlyList<BrokerPositionSnapshot> Positions { get; init; } = [];
    public IReadOnlyList<BrokerOrderSnapshot> Orders { get; init; } = [];
    public IReadOnlyList<BrokerTradeSnapshot> Trades { get; init; } = [];
}

/// <summary>
/// Registrazione di una chiusura che un client ExternalBroker ha già eseguito applicando la specifica
/// di uscita ricevuta con l'intent di ingresso: Stop Loss/Take Profit nativi del broker, uscita a
/// tempo (<c>CloseAtUtc</c>) o limite di barre (<c>MaxBarsInPosition</c>).
///
/// <para>È l'<b>unico</b> canale di chiusura in ExternalBroker: il server non emette intent di
/// chiusura. Registra un intent <see cref="OrderIntentKind.Close"/> per la posizione aperta
/// corrispondente, che il client referenzia nel normale POST /execution-reports per completare la
/// chiusura e generare il PersistedTrade (input delle rotazioni Titano).</para>
/// </summary>
public sealed class CreateExternalCloseIntentRequest
{
    public required string SessionToken { get; init; }
    public required string StrategyCode { get; init; }
    public required string Symbol { get; init; }
    /// <summary>Account cTrader che ha deciso la chiusura in locale. Obbligatorio se la sessione ha gruppi account configurati.</summary>
    public string? AccountNumber { get; init; }
    /// <summary>Quantità da chiudere; se omessa o &lt;= 0 il server usa l'intera quantità della posizione aperta.</summary>
    public decimal Quantity { get; init; }
    public string? Reason { get; init; }
}
