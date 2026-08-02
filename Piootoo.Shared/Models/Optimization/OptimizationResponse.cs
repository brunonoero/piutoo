namespace Piootoo.Shared.Models.Optimization;

/// <summary>
/// Risposta dell'ottimizzazione
/// </summary>
public class OptimizationResponse
{
    /// <summary>
    /// Nome del setup ottimizzato
    /// </summary>
    public string SetupName { get; set; } = string.Empty;

    /// <summary>
    /// Data/ora di esecuzione
    /// </summary>
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Durata dell'ottimizzazione
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Stato dell'ottimizzazione
    /// </summary>
    public OptimizationStatus Status { get; set; } = OptimizationStatus.Pending;

    /// <summary>
    /// Messaggio di stato
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Configurazione ottimale trovata
    /// </summary>
    public OptimalConfiguration? OptimalConfig { get; set; }

    /// <summary>
    /// Metriche di performance con la configurazione ottimale
    /// </summary>
    public PerformanceMetrics? Metrics { get; set; }

    /// <summary>
    /// Top N configurazioni alternative
    /// </summary>
    public List<RankedConfiguration> TopConfigurations { get; set; } = new();

    /// <summary>
    /// Strategie abilitate con questa configurazione
    /// </summary>
    public List<string> EnabledStrategies { get; set; } = new();

    /// <summary>
    /// Dettaglio valutazione per strategia
    /// </summary>
    public List<StrategyEvaluationResult> StrategyEvaluations { get; set; } = new();

    /// <summary>
    /// Parametri usati per l'ottimizzazione
    /// </summary>
    public OptimizationRequest? RequestParameters { get; set; }

    /// <summary>
    /// Statistiche dell'ottimizzazione
    /// </summary>
    public OptimizationStats? Stats { get; set; }

    /// <summary>
    /// Strategie trovate per i simboli selezionati
    /// </summary>
    public List<StrategyInfo> StrategiesFound { get; set; } = new();
}

/// <summary>
/// Informazioni su una strategia (DTO per la response)
/// </summary>
public class StrategyInfo
{
    /// <summary>
    /// ID della strategia
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nome della strategia
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Simbolo
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Timeframe in minuti
    /// </summary>
    public int TimeframeMinutes { get; set; }

    /// <summary>
    /// Tipo di barra (es. OneMinute, FifteenMinute)
    /// </summary>
    public string BarType { get; set; } = string.Empty;

    /// <summary>
    /// Tipo di strategia
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Se i dati sono stati trovati per questa strategia
    /// </summary>
    public bool HasData { get; set; }
}

public enum OptimizationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    PartialSuccess
}

/// <summary>
/// Configurazione ottimale trovata
/// </summary>
public class OptimalConfiguration
{
    /// <summary>
    /// Score finale della configurazione
    /// </summary>
    public decimal FinalScore { get; set; }

    /// <summary>
    /// Pesi ottimizzati
    /// </summary>
    public OptimizedWeights Weights { get; set; } = new();

    /// <summary>
    /// Parametri di rischio effettivi
    /// </summary>
    public RiskParameters RiskParams { get; set; } = new();

    /// <summary>
    /// Configurazione scoring completa
    /// </summary>
    public ScoringConfiguration ScoringConfig { get; set; } = new();
}

/// <summary>
/// Pesi ottimizzati
/// </summary>
public class OptimizedWeights
{
    public decimal ReturnWeight { get; set; }
    public decimal SharpeWeight { get; set; }
    public decimal DrawdownWeight { get; set; }
    public decimal WinRateWeight { get; set; }
    public decimal ProfitFactorWeight { get; set; }
    public decimal ConsistencyWeight { get; set; }
    public decimal CalmarWeight { get; set; }
}

/// <summary>
/// Metriche di performance
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Rendimento totale (%)
    /// </summary>
    public decimal TotalReturn { get; set; }

    /// <summary>
    /// Rendimento annualizzato (%)
    /// </summary>
    public decimal AnnualizedReturn { get; set; }

    /// <summary>
    /// Sharpe Ratio
    /// </summary>
    public decimal SharpeRatio { get; set; }

    /// <summary>
    /// Sortino Ratio
    /// </summary>
    public decimal SortinoRatio { get; set; }

    /// <summary>
    /// Calmar Ratio
    /// </summary>
    public decimal CalmarRatio { get; set; }

    /// <summary>
    /// Drawdown massimo (%)
    /// </summary>
    public decimal MaxDrawdown { get; set; }

    /// <summary>
    /// Drawdown massimo (valore)
    /// </summary>
    public decimal MaxDrawdownValue { get; set; }

    /// <summary>
    /// Rapporto Profit/Drawdown
    /// </summary>
    public decimal ProfitDrawdownRatio { get; set; }

    /// <summary>
    /// Win Rate (%)
    /// </summary>
    public decimal WinRate { get; set; }

    /// <summary>
    /// Profit Factor
    /// </summary>
    public decimal ProfitFactor { get; set; }

    /// <summary>
    /// Numero totale trade
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    /// Trade vincenti
    /// </summary>
    public int WinningTrades { get; set; }

    /// <summary>
    /// Trade perdenti
    /// </summary>
    public int LosingTrades { get; set; }

    /// <summary>
    /// Guadagno medio per trade
    /// </summary>
    public decimal AverageWin { get; set; }

    /// <summary>
    /// Perdita media per trade
    /// </summary>
    public decimal AverageLoss { get; set; }

    /// <summary>
    /// Perdite consecutive massime
    /// </summary>
    public int MaxConsecutiveLosses { get; set; }

    /// <summary>
    /// Volatilità
    /// </summary>
    public decimal Volatility { get; set; }
}

/// <summary>
/// Configurazione con ranking
/// </summary>
public class RankedConfiguration
{
    public int Rank { get; set; }
    public decimal Score { get; set; }
    public OptimalConfiguration Configuration { get; set; } = new();
    public PerformanceMetrics Metrics { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// Statistiche dell'ottimizzazione
/// </summary>
public class OptimizationStats
{
    /// <summary>
    /// Iterazioni eseguite
    /// </summary>
    public int IterationsRun { get; set; }

    /// <summary>
    /// Configurazioni valutate
    /// </summary>
    public int ConfigurationsEvaluated { get; set; }

    /// <summary>
    /// Configurazioni valide (rispettano vincoli)
    /// </summary>
    public int ValidConfigurations { get; set; }

    /// <summary>
    /// Miglioramento rispetto alla baseline (%)
    /// </summary>
    public decimal ImprovementOverBaseline { get; set; }

    /// <summary>
    /// Score iniziale (baseline)
    /// </summary>
    public decimal BaselineScore { get; set; }

    /// <summary>
    /// Score finale (ottimizzato)
    /// </summary>
    public decimal FinalScore { get; set; }

    /// <summary>
    /// Convergenza raggiunta
    /// </summary>
    public bool ConvergenceReached { get; set; }

    /// <summary>
    /// Iterazione di convergenza
    /// </summary>
    public int? ConvergenceIteration { get; set; }
}
