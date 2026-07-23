namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Risultato aggregato per settimana
/// </summary>
public class WeeklyResult
{
    public int Year { get; set; }
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal WeeklyProfit { get; set; }
    public decimal WeeklyEquity { get; set; }
    public decimal WeeklyDrawdown { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
}
