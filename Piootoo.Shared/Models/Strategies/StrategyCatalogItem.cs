namespace Piootoo.Shared.Models.Strategies;

/// <summary>Contratto pubblico e serializzabile di una strategia disponibile sul server.</summary>
public sealed class StrategyCatalogItem
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }
    public string BarType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
}
