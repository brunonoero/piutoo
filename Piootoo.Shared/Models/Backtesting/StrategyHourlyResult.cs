using Piootoo.Shared.Enums;

namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Risultato per strategia per ogni ora
/// </summary>
public class StrategyHourlyResult
{
    public string StrategyName { get; set; } = string.Empty;
    public string StrategyCode { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public decimal Equity { get; set; }
    public decimal Profit { get; set; }
    public int Contracts { get; set; } = 1; // sempre 1 per ora
    public SignalType? Signal { get; set; }
    public decimal? EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
}
