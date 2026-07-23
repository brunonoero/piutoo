namespace Piootoo.Shared.Models.Sapioo;

/// <summary>
/// Peso/multiplier di una strategia dopo ottimizzazione
/// </summary>
public class StrategyWeight
{
    public string StrategyName { get; set; } = string.Empty;
    public decimal Multiplier { get; set; } = 1.0m;  // Coefficiente contratti
    public bool IsEnabled { get; set; } = true;
    public string? DisabledReason { get; set; }  // Se disabilitata, perché
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
}
