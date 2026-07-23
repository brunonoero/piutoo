namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Stato corrente del trading emulator
/// </summary>
public class TradingState
{
    public decimal Equity { get; set; }
    public decimal Balance { get; set; }
    public decimal MaxEquity { get; set; }
    public decimal Drawdown { get; set; }
    public Dictionary<string, OpenPosition> OpenPositions { get; set; } = new();
    
    public void UpdateDrawdown()
    {
        if (MaxEquity > 0)
        {
            Drawdown = ((MaxEquity - Equity) / MaxEquity) * 100m;
        }
        else
        {
            Drawdown = 0;
        }
    }
}
