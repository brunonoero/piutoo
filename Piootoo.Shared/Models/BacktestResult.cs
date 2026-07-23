namespace Piootoo.Shared.Models;

/// <summary>
/// Risultato di un backtest
/// </summary>
public class BacktestResult
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<WeeklySetup> WeeklySetups { get; set; } = new();
    public List<TradeSignal> AllSignals { get; set; } = new();
    public List<StrategyPerformance> WeeklyPerformances { get; set; } = new();
    
    // Metriche aggregate
    public decimal TotalReturn { get; set; }
    public decimal AverageSharpe { get; set; }
    public decimal MaxDrawdown { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }

    public void CalculateSummary()
    {
        if (WeeklyPerformances.Any())
        {
            TotalReturn = WeeklyPerformances.Sum(p => p.Return);
            AverageSharpe = WeeklyPerformances.Average(p => p.SharpeRatio);
            MaxDrawdown = WeeklyPerformances.Min(p => p.MaxDrawdown);
            TotalTrades = WeeklyPerformances.Sum(p => p.TotalTrades);
            
            var totalWins = WeeklyPerformances.Sum(p => p.WinningTrades);
            WinRate = TotalTrades > 0 ? (decimal)totalWins / TotalTrades : 0;
        }
    }

    public void PrintSummary()
    {
        Console.WriteLine($"\n{'=',60}");
        Console.WriteLine("RIEPILOGO BACKTEST");
        Console.WriteLine($"{'=',60}");
        Console.WriteLine($"Periodo: {StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}");
        Console.WriteLine($"Settimane testate: {WeeklySetups.Count}");
        Console.WriteLine($"\nPERFORMANCE TOTALE:");
        Console.WriteLine($"  Return totale: {TotalReturn:F2}%");
        Console.WriteLine($"  Sharpe medio: {AverageSharpe:F2}");
        Console.WriteLine($"  Max Drawdown: {MaxDrawdown:P2}");
        Console.WriteLine($"  Total Trades: {TotalTrades}");
        Console.WriteLine($"  Win Rate: {WinRate:P2}");
        
        Console.WriteLine($"\nROTAZIONI SETTIMANALI:");
        foreach (var setup in WeeklySetups.OrderBy(s => s.StartDate))
        {
            Console.WriteLine($"  Sett. {setup.Week}/{setup.Year}: " +
                $"{string.Join(", ", setup.EnabledStrategies)}");
        }
        Console.WriteLine($"{'=',60}\n");
    }
}
