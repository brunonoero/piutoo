namespace Piootoo.Shared.Models.Optimization;

/// <summary>
/// Job di ottimizzazione in esecuzione
/// </summary>
public class OptimizationJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public OptimizationJobStatus Status { get; set; } = OptimizationJobStatus.Pending;
    public int ProgressPercent { get; set; }
    public string? CurrentStep { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Risultato dell'ottimizzazione base
    /// </summary>
    public FilteredBacktestingResult? BasicResult { get; set; }
    
    /// <summary>
    /// Risultato dell'ottimizzazione avanzata
    /// </summary>
    public AdvancedOptimizationResult? AdvancedResult { get; set; }
    
    /// <summary>
    /// Tipo di ottimizzazione (Basic o Advanced)
    /// </summary>
    public OptimizationType Type { get; set; } = OptimizationType.Basic;
    
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Stato di un job di ottimizzazione
/// </summary>
public enum OptimizationJobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Tipo di ottimizzazione
/// </summary>
public enum OptimizationType
{
    Basic,
    Advanced
}
