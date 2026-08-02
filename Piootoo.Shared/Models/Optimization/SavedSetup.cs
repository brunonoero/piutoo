namespace Piootoo.Shared.Models.Optimization;

/// <summary>
/// Setup salvato con risultati dell'ottimizzazione
/// </summary>
public class SavedSetup
{
    /// <summary>
    /// ID univoco del setup
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nome del setup
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Data creazione
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data ultima modifica
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Stato del setup
    /// </summary>
    public SetupStatus Status { get; set; } = SetupStatus.Draft;

    /// <summary>
    /// Simboli con timeframe associati
    /// </summary>
    public List<SymbolSelection> Symbols { get; set; } = new();

    /// <summary>
    /// Parametri di ottimizzazione usati
    /// </summary>
    public OptimizationParameters OptimizationParams { get; set; } = new();

    /// <summary>
    /// Parametri di rischio
    /// </summary>
    public RiskParameters RiskParams { get; set; } = new();

    /// <summary>
    /// Periodo di valutazione
    /// </summary>
    public EvaluationPeriod EvaluationPeriod { get; set; } = new();

    /// <summary>
    /// Configurazione ottimale trovata
    /// </summary>
    public OptimalConfiguration? OptimalConfig { get; set; }

    /// <summary>
    /// Metriche di performance
    /// </summary>
    public PerformanceMetrics? Metrics { get; set; }

    /// <summary>
    /// Strategie abilitate
    /// </summary>
    public List<string> EnabledStrategies { get; set; } = new();

    /// <summary>
    /// Score finale
    /// </summary>
    public decimal FinalScore { get; set; }

    /// <summary>
    /// Note aggiuntive
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Tag per categorizzazione
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Se il setup è attivo per il trading
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Storico delle ottimizzazioni eseguite
    /// </summary>
    public List<OptimizationRun> OptimizationHistory { get; set; } = new();
}

public enum SetupStatus
{
    Draft,
    Optimizing,
    Optimized,
    Active,
    Paused,
    Archived
}

/// <summary>
/// Singola esecuzione di ottimizzazione
/// </summary>
public class OptimizationRun
{
    public string RunId { get; set; } = Guid.NewGuid().ToString();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public decimal Score { get; set; }
    public PerformanceMetrics? Metrics { get; set; }
    public OptimalConfiguration? Config { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// File JSON contenente tutti i setup
/// </summary>
public class SetupsFile
{
    public string Version { get; set; } = "1.0";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<SavedSetup> Setups { get; set; } = new();
}
