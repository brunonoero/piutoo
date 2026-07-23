namespace Piootoo.Shared.Models;

/// <summary>
/// Dati sul drawdown
/// </summary>
public class DrawdownData
{
    public decimal MaxDrawdownPercent { get; set; }
    public decimal MaxDrawdownValue { get; set; }
    public int MaxConsecutiveDrawdown { get; set; }
}
