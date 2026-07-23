using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Shared.Models.Optimization;

public class TitanoFilterRequest
{
    public string BacktestingId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? SetupId { get; set; }
    public int LookbackWeeks { get; set; } = 4;
    public TitanoFilterRules Rules { get; set; } = new();
    public TitanoTradingRules TradingRules { get; set; } = new();
}

/// <summary>
/// Setup Titano persistito su JSON (parametri filtro + regole trading).
/// </summary>
public class TitanoFilterSetup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LookbackWeeks { get; set; } = 4;
    public TitanoFilterRules Rules { get; set; } = new();
    public TitanoTradingRules TradingRules { get; set; } = new();
    public DateTime? UpdatedAt { get; set; }
}

public class TitanoSetupInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}

public class TitanoTradingRules
{
    /// <summary>Chiude tutte le posizioni aperte all'ultima barra della settimana (UTC, lun-ven).</summary>
    public bool CloseAllPositionsAtWeekEnd { get; set; } = true;
}

public class TitanoFilterRules
{
    public decimal MinRollingProfit { get; set; } = 0m;
    public decimal MaxRollingDrawdown { get; set; } = -0.15m;
    /// <summary>Percentuale settimane positive nel lookback (legacy alias).</summary>
    public decimal MinWinRate { get; set; } = 0.40m;
    public int MinTrades { get; set; } = 1;
    public decimal MinProfitFactor { get; set; } = 1.05m;
    public decimal MinPositiveWeeksRatio { get; set; } = 0.45m;
    public int MaxConsecutiveLosingWeeks { get; set; } = 3;

    /// <summary>Win rate sui segnali/trade nel lookback (0 = disabilitato).</summary>
    public decimal MinTradeWinRate { get; set; } = 0m;
    /// <summary>Perdita max ultima settimana del lookback, frazione capitale (es. -0.03 = -3%). 0 = disabilitato.</summary>
    public decimal MaxWeeklyLoss { get; set; } = 0m;
    /// <summary>Return settimanale max nel lookback, frazione capitale (anti-spike). 0 = disabilitato.</summary>
    public decimal MaxSingleWeekReturn { get; set; } = 0m;
    public int CooldownWeeksAfterOff { get; set; } = 0;
    /// <summary>Max strategie ON contemporaneamente per settimana. 0 = illimitato.</summary>
    public int MaxStrategiesOn { get; set; } = 0;
    /// <summary>Sharpe rolling sui rendimenti settimanali. 0 = disabilitato.</summary>
    public decimal MinSharpeRatio { get; set; } = 0m;
    /// <summary>Settimane minime di storia prima di applicare le regole. 0 = usa LookbackWeeks.</summary>
    public int MinWeeksBeforeRulesApply { get; set; } = 0;
}

public class TitanoFilterResult
{
    public string BacktestingId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? SetupId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    public int LookbackWeeks { get; set; }
    public TitanoFilterRules Rules { get; set; } = new();
    public TitanoTradingRules TradingRules { get; set; } = new();
    public List<TitanoWeeklyResult> WeeklyResults { get; set; } = new();
    public List<TitanoStrategySummary> StrategySummaries { get; set; } = new();
    public decimal OriginalFinalEquity { get; set; }
    public decimal FilteredFinalEquity { get; set; }
    public decimal OriginalTotalProfit { get; set; }
    public decimal FilteredTotalProfit { get; set; }
    public decimal OriginalMaxDrawdown { get; set; }
    public decimal FilteredMaxDrawdown { get; set; }
    public int OriginalTotalTrades { get; set; }
    public int FilteredTotalTrades { get; set; }
    public int SuspendedStrategyTrades { get; set; }
    public string? ResultFilePath { get; set; }
    public string? HtmlReportFilePath { get; set; }
}

public class TitanoWeeklyResult
{
    public int Year { get; set; }
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal OriginalEquity { get; set; }
    public decimal FilteredEquity { get; set; }
    public decimal OriginalWeeklyProfit { get; set; }
    public decimal FilteredWeeklyProfit { get; set; }
    public decimal FilteredDrawdown { get; set; }
    public int OriginalWeeklyTrades { get; set; }
    public int FilteredWeeklyTrades { get; set; }
    public int OriginalWinningTrades { get; set; }
    public int OriginalLosingTrades { get; set; }
    public int FilteredWinningTrades { get; set; }
    public int FilteredLosingTrades { get; set; }
    public List<string> EnabledStrategies { get; set; } = new();
    public List<TitanoStrategyDecision> StrategyDecisions { get; set; } = new();
}

public class TitanoStrategyDecision
{
    public string StrategyKey { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public decimal Score { get; set; }
    public TitanoStrategyMetrics Metrics { get; set; } = new();
    public List<string> Reasons { get; set; } = new();
}

public class TitanoStrategyMetrics
{
    public decimal RollingProfit { get; set; }
    public decimal RollingMaxDrawdown { get; set; }
    public decimal WinRate { get; set; }
    public decimal TradeWinRate { get; set; }
    public int Trades { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal PositiveWeeksRatio { get; set; }
    public int ConsecutiveLosingWeeks { get; set; }
    public decimal SharpeRatio { get; set; }
}

public class TitanoStrategySummary
{
    public string StrategyKey { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int EnabledWeeks { get; set; }
    public int DisabledWeeks { get; set; }
    public decimal ProfitWhenEnabled { get; set; }
    public decimal ProfitIfAlwaysEnabled { get; set; }
}
