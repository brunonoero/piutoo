namespace Piootoo.Shared.Models.Sapioo;

/// <summary>
/// Job di ottimizzazione Sapioo in esecuzione
/// </summary>
public class SapiooJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public SapiooJobStatus Status { get; set; } = SapiooJobStatus.Pending;
    public int ProgressPercent { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public SapiooResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
