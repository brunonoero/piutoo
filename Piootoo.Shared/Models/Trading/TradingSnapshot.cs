namespace Piootoo.Shared.Models.Trading;

/// <summary>
/// Snapshot dello stato di trading in un momento specifico
/// </summary>
public class TradingSnapshot
{
    public DateTime DateTime { get; set; }
    public decimal Equity { get; set; }
    public decimal Balance { get; set; }
    public decimal Drawdown { get; set; }
    public decimal Profit { get; set; }
    public int OpenPositionsCount { get; set; }
    public Dictionary<string, decimal> StrategyEquities { get; set; } = new();
}
