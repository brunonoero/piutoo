namespace Piootoo.Shared.Models;

/// <summary>
/// Punto della equity curve
/// </summary>
public class EquityPoint
{
    public DateTime Date { get; set; }
    public decimal Balance { get; set; }
    public TradingResult? Trade { get; set; }
}
