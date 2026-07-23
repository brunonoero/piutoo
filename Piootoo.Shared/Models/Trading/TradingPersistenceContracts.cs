using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Trading;

public static class TradingPersistenceSchema
{
    public const int Version = 2;
    public const string SignalsFileName = "signals.json";
    public const string TradesFileName = "trades.json";
}

public sealed class PersistedSignal
{
    public int SchemaVersion { get; init; } = TradingPersistenceSchema.Version;
    public required string SignalId { get; init; }
    public string? IntentId { get; init; }
    public string? CorrelationId { get; init; }
    public string? SessionId { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public SignalType Side { get; init; }
    public TradeOrderType OrderType { get; init; }
    public decimal TriggerPrice { get; init; }
    public decimal Quantity { get; init; }
    public decimal BaseQuantity { get; init; }
    public decimal StrategyEquityMultiplier { get; init; } = 1m;
    public decimal MarketVolatilityMultiplier { get; init; } = 1m;
    public decimal PortfolioRiskMultiplier { get; init; } = 1m;
    public decimal FinalQuantity { get; init; }
    public string? SizingReason { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public DateTime? TimeExitUtc { get; init; }
    public string? Reason { get; init; }
    public bool CloseOnly { get; init; }
    public OrderIntentStatus? Status { get; init; }
    public decimal FilledQuantity { get; init; }
    public string? ExternalOrderId { get; init; }
}

public sealed class PersistedTrade
{
    public int SchemaVersion { get; init; } = TradingPersistenceSchema.Version;
    public required string TradeId { get; init; }
    public string? OrderId { get; init; }
    public string? IntentId { get; init; }
    public string? CorrelationId { get; init; }
    public string? SessionId { get; init; }
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public SignalType Direction { get; init; }
    public decimal Quantity { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required DateTime ExitTimeUtc { get; init; }
    public decimal EntryPrice { get; init; }
    public decimal ExitPrice { get; init; }
    public string? ExitReason { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal NetProfit { get; init; }
    public decimal Commission { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
}
