namespace Piootoo.Shared.Models.Optimization;

/// <summary>
/// Richiesta per ottimizzazione avanzata
/// </summary>
public class AdvancedOptimizationRequest
{
    /// <summary>
    /// ID del backtesting da ottimizzare
    /// </summary>
    public string BacktestingId { get; set; } = string.Empty;

    /// <summary>
    /// Numero di settimane da considerare per il lookback
    /// </summary>
    public int LookbackWeeks { get; set; } = 8;

    /// <summary>
    /// Configurazione opzionale dei filtri
    /// </summary>
    public AdvancedFilterConfigDto? FilterConfig { get; set; }
}

/// <summary>
/// Configurazione filtri (DTO per frontend)
/// </summary>
public class AdvancedFilterConfigDto
{
    // Filtri base
    public decimal? MinWinRate { get; set; }
    public decimal? MaxDrawdownLimit { get; set; }
    public decimal? MinSharpeRatio { get; set; }
    public int? MinTrades { get; set; }
    public decimal? MinCompositeScore { get; set; }
    public int? MinWeeksRequired { get; set; }
    public decimal? MaxCorrelation { get; set; }

    // Pesi per score composito
    public decimal? SharpeWeight { get; set; }
    public decimal? SortinoWeight { get; set; }
    public decimal? CalmarWeight { get; set; }
    public decimal? OmegaWeight { get; set; }
    public decimal? RecoveryWeight { get; set; }
    public decimal? WinRateWeight { get; set; }
    public decimal? TailRatioWeight { get; set; }
    public decimal? GainToPainWeight { get; set; }
    public decimal? UlcerPenalty { get; set; }
    public decimal? DrawdownPenalty { get; set; }
    public decimal? StabilityBonus { get; set; }

    // Pesi ottimizzazione portafoglio
    public decimal? RiskParityWeight { get; set; }
    public decimal? KellyWeight { get; set; }
    public decimal? HRPWeight { get; set; }
}

/// <summary>
/// Risultato dell'ottimizzazione avanzata
/// </summary>
public class AdvancedOptimizationResult
{
    public string BacktestingId { get; set; } = string.Empty;
    public string SetupName { get; set; } = string.Empty;
    public DateTime OptimizationDate { get; set; }
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Numero strategie originali
    /// </summary>
    public int OriginalStrategiesCount { get; set; }

    /// <summary>
    /// Numero strategie dopo il filtro
    /// </summary>
    public int FilteredStrategiesCount { get; set; }

    /// <summary>
    /// Strategie filtrate con metriche complete
    /// </summary>
    public List<FilteredStrategyDto> FilteredStrategies { get; set; } = new();

    /// <summary>
    /// Info correlazione tra strategie
    /// </summary>
    public CorrelationInfoDto Correlation { get; set; } = new();

    /// <summary>
    /// Metriche del portafoglio ottimizzato
    /// </summary>
    public PortfolioMetricsDto PortfolioMetrics { get; set; } = new();

    /// <summary>
    /// Risultato del backtesting filtrato
    /// </summary>
    public FilteredBacktestingResult FilteredBacktesting { get; set; } = new();

    /// <summary>
    /// Configurazione utilizzata
    /// </summary>
    public object? FilterConfigUsed { get; set; }
}

/// <summary>
/// Strategia filtrata (DTO)
/// </summary>
public class FilteredStrategyDto
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }
    public decimal Weight { get; set; }
    public decimal SizeMultiplier { get; set; }
    public int Rank { get; set; }
    public StrategyAdvancedMetricsDto Metrics { get; set; } = new();
}

/// <summary>
/// Metriche avanzate per strategia (DTO)
/// </summary>
public class StrategyAdvancedMetricsDto
{
    // Base
    public decimal TotalReturn { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public decimal AvgWin { get; set; }
    public decimal AvgLoss { get; set; }
    
    // Avanzate
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal CalmarRatio { get; set; }
    public decimal OmegaRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal RecoveryFactor { get; set; }
    public decimal UlcerIndex { get; set; }
    public decimal TailRatio { get; set; }
    public decimal VaR95 { get; set; }
    public decimal CVaR95 { get; set; }
    public decimal GainToPainRatio { get; set; }
    
    // Score finale
    public decimal CompositeScore { get; set; }
}

/// <summary>
/// Info correlazione (DTO)
/// </summary>
public class CorrelationInfoDto
{
    public decimal AverageCorrelation { get; set; }
    public List<string> StrategyNames { get; set; } = new();
    public List<List<decimal>> Matrix { get; set; } = new();
}

/// <summary>
/// Metriche portafoglio (DTO)
/// </summary>
public class PortfolioMetricsDto
{
    /// <summary>
    /// Rendimento atteso annualizzato
    /// </summary>
    public decimal ExpectedReturn { get; set; }

    /// <summary>
    /// Volatilità annualizzata
    /// </summary>
    public decimal Volatility { get; set; }

    /// <summary>
    /// Sharpe Ratio del portafoglio
    /// </summary>
    public decimal SharpeRatio { get; set; }

    /// <summary>
    /// Max Drawdown del portafoglio
    /// </summary>
    public decimal MaxDrawdown { get; set; }

    /// <summary>
    /// Diversification Ratio (> 1 = buona diversificazione)
    /// </summary>
    public decimal DiversificationRatio { get; set; }
}
