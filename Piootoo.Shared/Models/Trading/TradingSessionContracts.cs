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
}
