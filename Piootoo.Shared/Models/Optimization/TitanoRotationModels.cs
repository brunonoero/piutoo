namespace Piootoo.Shared.Models.Optimization;

public enum TitanoRotationPeriod { Weekly, Biweekly, Monthly }
public enum TitanoRunStatus { Completed, Failed }
public enum TitanoStrategyStatus { Enabled, Reduced, Disabled, HardStopped }
public enum TitanoWalkForwardMode { Rolling, Expanding }

public sealed class TitanoRotationRequest
{
    public required string WorkspaceId { get; init; }
    public required string BacktestFolder { get; init; }
    public TitanoRotationPeriod RotationPeriod { get; init; } = TitanoRotationPeriod.Weekly;
    public required DateTime StartUtc { get; init; }
    public required DateTime EndUtc { get; init; }
    public DateTime? BiweeklyAnchorUtc { get; init; }
    public string TimeZoneId { get; init; } = "UTC";
    public decimal InitialCapital { get; init; } = 100_000m;
    public int MinimumTrades { get; init; } = 1;
    public int ShortWindowDays { get; init; } = 90;
    public int LongWindowDays { get; init; } = 365;
    public int MovingAverageWindowDays { get; init; } = 90;
    public decimal MinimumShortReturn { get; init; } = 0m;
    public decimal MinimumLongReturn { get; init; } = 0m;
    public decimal MinimumZScore { get; init; } = -1.5m;
    public decimal MaximumZScore { get; init; } = 2.5m;
    public decimal MaximumCurrentDrawdown { get; init; } = 0.15m;
    public decimal MaximumObservedDrawdown { get; init; } = 0.25m;
    public decimal MaximumReturnVolatility { get; init; } = 0.10m;
    public bool RequireEquityAboveMovingAverage { get; init; } = true;
    public decimal ReenableMaximumCurrentDrawdown { get; init; } = 0.10m;
    public decimal DisableCompositeScore { get; init; } = 0.40m;
    public decimal ReenableCompositeScore { get; init; } = 0.60m;
    public int MinimumPassingFilters { get; init; } = 4;
    public int CooldownPeriodsAfterOff { get; init; } = 2;
    public int MinimumOnPeriods { get; init; } = 1;
    public decimal HardStopDrawdown { get; init; } = 0.35m;
    public decimal CommissionPerUnit { get; init; }
    public decimal SlippagePerUnit { get; init; }
    public decimal MinimumIntentQuantity { get; init; } = 1m;
    public decimal QuantityStep { get; init; } = 1m;
    public IReadOnlyList<TitanoSizingTier> SizingTiers { get; init; } =
    [
        new() { MinimumScore = 0.80m, AllocationMultiplier = 1m },
        new() { MinimumScore = 0.60m, AllocationMultiplier = 0.50m },
        new() { MinimumScore = 0.40m, AllocationMultiplier = 0.25m },
        new() { MinimumScore = 0m, AllocationMultiplier = 0m }
    ];
    public int CalibrationPeriods { get; init; } = 8;
    public int EvaluationPeriods { get; init; } = 4;
    public TitanoWalkForwardMode WalkForwardMode { get; init; } = TitanoWalkForwardMode.Rolling;
}

public sealed class TitanoSizingTier
{
    public decimal MinimumScore { get; init; }
    public decimal AllocationMultiplier { get; init; }
}

public sealed class TitanoRunInfo
{
    public required string RunId { get; init; }
    public required string WorkspaceId { get; init; }
    public required string BacktestFolder { get; init; }
    public TitanoRunStatus Status { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public required string ManifestPath { get; init; }
    public int PeriodCount { get; init; }
}

public sealed class TitanoRotationManifest
{
    public const int CurrentSchemaVersion = 2;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string RunId { get; init; }
    public required TitanoRotationRequest Config { get; init; }
    public required string SourceTradesSha256 { get; init; }
    public required string MasterFilterHash { get; init; }
    public required string ConfigSha256 { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public List<TitanoRotationDecision> Periods { get; init; } = [];
    public List<TitanoEquityPoint> FilteredEquity { get; init; } = [];
    public List<TitanoWalkForwardResult> WalkForward { get; init; } = [];
    public List<TitanoHardStopReset> HardStopResets { get; init; } = [];
}

public sealed class TitanoRotationDecision
{
    public required string PeriodId { get; init; }
    public DateTime PeriodStartUtc { get; init; }
    public DateTime PeriodEndUtc { get; init; }
    public DateTime EffectiveFromUtc { get; init; }
    public DateTime EffectiveToUtc { get; init; }
    public required string SourceBacktestFolder { get; init; }
    public required string MasterFilterHash { get; init; }
    public List<TitanoStrategyState> Strategies { get; init; } = [];
}

public sealed class TitanoStrategyState
{
    public required string StrategyCode { get; init; }
    public bool Enabled { get; init; }
    public decimal AllocationMultiplier { get; init; }
    public TitanoStrategyStatus State { get; init; }
    public int CooldownRemaining { get; init; }
    public int ConsecutiveOnPeriods { get; init; }
    public bool HardStopped { get; init; }
    public int PassingFilters { get; init; }
    public int TotalFilters { get; init; }
    public IReadOnlyList<TitanoFilterVote> Votes { get; init; } = [];
    public decimal Score { get; init; }
    public required string Reason { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public TitanoPeriodMetrics Metrics { get; init; } = new();
}

public sealed class TitanoFilterVote
{
    public required string Filter { get; init; }
    public bool Passed { get; init; }
    public decimal Score { get; init; }
    public required string Reason { get; init; }
}

public sealed class TitanoWalkForwardResult
{
    public required string EvaluationPeriodId { get; init; }
    public DateTime CalibrationFromUtc { get; init; }
    public DateTime CalibrationToUtc { get; init; }
    public DateTime EvaluationFromUtc { get; init; }
    public DateTime EvaluationToUtc { get; init; }
    public decimal InSampleNetProfit { get; init; }
    public decimal OutOfSampleNetProfit { get; init; }
    public bool InSampleOnlyImprovementWarning { get; init; }
}

public sealed class TitanoHardStopReset
{
    public required string ResetId { get; init; }
    public required string StrategyCode { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public DateTime EffectiveFromUtc { get; init; }
    public required string RequestedBy { get; init; }
    public required string Reason { get; init; }
}

public sealed class TitanoHardStopResetRequest
{
    public required string StrategyCode { get; init; }
    public required string RequestedBy { get; init; }
    public required string Reason { get; init; }
    public required DateTime RequestedAtUtc { get; init; }
}

public sealed class TitanoPeriodMetrics
{
    public int Trades { get; init; }
    public int WinningTrades { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal NetProfit { get; init; }
    public decimal Commission { get; init; }
    public decimal CurrentEquity { get; init; }
    public decimal ShortStartEquity { get; init; }
    public decimal LongStartEquity { get; init; }
    public decimal ShortReturn { get; init; }
    public decimal LongReturn { get; init; }
    public decimal MovingAverageEquity { get; init; }
    public decimal EquityStandardDeviation { get; init; }
    public decimal ZScore { get; init; }
    public decimal CurrentDrawdown { get; init; }
    public decimal MaximumDrawdown { get; init; }
    public decimal ReturnVolatility { get; init; }
}

public sealed class TitanoEquityPoint
{
    public DateTime TimestampUtc { get; init; }
    public required string TradeId { get; init; }
    public required string StrategyCode { get; init; }
    public decimal NetProfit { get; init; }
    public decimal AllocationMultiplier { get; init; }
    public decimal Costs { get; init; }
    public decimal Balance { get; init; }
    public decimal Equity { get; init; }
}

public sealed class TitanoEffectiveStrategy
{
    public required string StrategyCode { get; init; }
    public decimal AllocationMultiplier { get; init; }
    public TitanoStrategyStatus State { get; init; }
    public int CooldownRemaining { get; init; }
    public bool HardStopped { get; init; }
}

public sealed class TitanoEffectiveStrategies
{
    public required string RunId { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string? PeriodId { get; init; }
    public IReadOnlyList<string> MasterStrategies { get; init; } = [];
    public IReadOnlyList<string> TitanoEnabledStrategies { get; init; } = [];
    public IReadOnlyList<string> EffectiveStrategies { get; init; } = [];
    public IReadOnlyList<TitanoEffectiveStrategy> StrategyStates { get; init; } = [];
}
