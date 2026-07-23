namespace Piootoo.Shared.Models;

/// <summary>
/// Performance tracking per una strategia
/// </summary>
public class StrategyPerformance
{
    public string StrategyName { get; set; } = string.Empty;
    public int Week { get; set; }
    public int Year { get; set; }
    
    // Metriche di rendimento
    public decimal Return { get; set; }
    public decimal CumulativeReturn { get; set; }
    
    // Metriche di rischio
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownValue { get; set; }
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    
    // Metriche di trading
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades : 0;
    
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal ProfitFactor => AverageLoss != 0 ? Math.Abs(AverageWin / AverageLoss) : 0;
    
    // Metriche di consistenza
    public decimal ConsecutiveWins { get; set; }
    public decimal ConsecutiveLosses { get; set; }
    
    // Balance tracking
    public decimal InitialBalance { get; set; }
    public decimal FinalBalance { get; set; }
    public decimal PeakBalance { get; set; }
    public decimal NetProfit => FinalBalance - InitialBalance;
    public decimal NetProfitPercent => InitialBalance != 0 ? (NetProfit / InitialBalance) * 100 : 0;
    
    // Metriche aggiuntive
    public decimal CalmarRatio => MaxDrawdown != 0 ? Return / Math.Abs(MaxDrawdown) : 0;
    public decimal RecoveryFactor => MaxDrawdown != 0 ? Return / Math.Abs(MaxDrawdown) : 0;
    public int DaysInMarket { get; set; }
    public int MaxConsecutiveDrawdown { get; set; }
    
    // Dettaglio trade
    public decimal LargestWin { get; set; }
    public decimal LargestLoss { get; set; }
    
    // Equity curve (opzionale, per analisi dettagliate)
    public List<EquityPoint> EquityCurve { get; set; } = new();
    
    // Verifica balance minimo raggiunto
    public decimal MinBalance => EquityCurve.Any() ? EquityCurve.Min(e => e.Balance) : InitialBalance;
    public bool HitStopLoss { get; set; }
    public decimal StopLossLevel { get; set; }
}
