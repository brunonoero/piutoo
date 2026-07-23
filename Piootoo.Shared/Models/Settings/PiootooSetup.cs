namespace Piootoo.Shared.Models.Settings;

/// <summary>
/// Setup di configurazione Piootoo salvato
/// </summary>
public class PiootooSetup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal InitialCapital { get; set; }
    public List<string> SelectedSymbols { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
