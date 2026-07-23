namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Risultato aggregato per ogni ora del backtesting
/// </summary>
public class HourlyResult
{
    public DateTime DateTime { get; set; }
    public decimal Equity { get; set; }
    public decimal Balance { get; set; }
    public decimal Drawdown { get; set; }
    public decimal Profit { get; set; }
    public int OpenPositionsCount { get; set; }
}
