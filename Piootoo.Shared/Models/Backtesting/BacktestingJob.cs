namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Job di backtesting in esecuzione
/// </summary>
public class BacktestingJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public BacktestingJobStatus Status { get; set; } = BacktestingJobStatus.Pending;
    public int ProgressPercent { get; set; }
    public string Phase { get; set; } = "Pending";
    public string? ProgressMessage { get; set; }
    public bool CancellationRequested { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public BacktestingResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
