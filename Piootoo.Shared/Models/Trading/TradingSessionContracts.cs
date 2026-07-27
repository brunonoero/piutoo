using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;

namespace Piootoo.Shared.Models.Trading;

public enum ExecutionMode { ServerSimulated, ExternalBroker }
public enum TradingSessionStatus { Created, Running, Stopped }
public enum ExecutionReportStatus { Accepted, PartiallyFilled, Filled, Rejected, Cancelled }
public enum OrderIntentStatus { Pending, Accepted, PartiallyFilled, Filled, Rejected, Cancelled }
public enum QuantityRoundingMode { FuturesContracts, BrokerVolumeStep }

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
    /// Interruttore esplicito dei filtri Titano.
    ///
    /// true (default quando è indicato un <see cref="TitanoRunId"/>): la rotazione decide quali
    /// strategie vengono valutate e con quale allocazione.
    /// false: le strategie del masterfilter vengono valutate tutte, ma la rotazione viene comunque
    /// risolta e registrata nel rotation-log — utile per confrontare "cosa avrebbe fatto Titano"
    /// senza subirne gli effetti.
    ///
    /// Prima l'unico modo per disattivare Titano era non passare il RunId, e quindi rinunciare
    /// anche alla diagnostica.
    /// </summary>
    public bool ApplyTitanoFilters { get; init; } = true;

    public PositionSizingConfig PositionSizing { get; init; } = new();
    public IReadOnlyList<InstrumentMetadata> Instruments { get; init; } = [];
}

public sealed class TradingSessionDescriptor
{
    public required string SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required string WorkspaceId { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public required TradingSessionStatus Status { get; init; }
    public string? TitanoRunId { get; init; }

    /// <summary>Se i filtri Titano sono realmente applicati o solo registrati in diagnostica.</summary>
    public bool ApplyTitanoFilters { get; init; }

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
    public bool CloseOnly { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
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

    /// <summary>Riferimento al setup salvato (rotation-setups); metadata per il client, non usato a runtime.</summary>
    public string? RotationSetupId { get; init; }

    /// <summary>Run Titano eseguibile (manifest). Obbligatorio se si applicano filtri Titano al gruppo.</summary>
    public string? TitanoRunId { get; init; }
    public string? TitanoBacktestFolder { get; init; }

    /// <summary>
    /// true: al polling account il manifest del gruppo filtra i template e scala la quantità.
    /// false: il gruppo riceve tutti i template compatibili con l'anti copy-trading, indipendentemente dal manifest.
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
}

/// <summary>
/// Richiesta di un client ExternalBroker (tipicamente un cBot) che ha già deciso in locale di chiudere
/// una posizione senza che il server abbia emesso un OrderIntent CloseOnly corrispondente: succede per
/// condizioni meccaniche gestite lato client (Stop Loss/Take Profit nativi del broker, limite di barre).
/// Il server registra un intent CloseOnly "client-originated" per la posizione aperta corrispondente,
/// così il client può referenziarlo nel normale POST /execution-reports per completare la chiusura e
/// generare un PersistedTrade (usato anche dalle rotazioni Titano).
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
