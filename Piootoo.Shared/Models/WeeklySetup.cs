namespace Piootoo.Shared.Models;

/// <summary>
/// Setup settimanale per il trading
/// </summary>
public class WeeklySetup
{
    public DateTime GenerationDate { get; set; }
    public int Week { get; set; }
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> EnabledStrategies { get; set; } = new();
    public List<StrategyEvaluationResult> StrategyEvaluations { get; set; } = new();
    public ScoringConfiguration Configuration { get; set; } = new();

    public string Summary => 
        $"Setup Settimana {Week}/{Year} ({StartDate:dd/MM} - {EndDate:dd/MM}): " +
        $"{EnabledStrategies.Count} strategie abilitate";

    public void PrintReport()
    {
        Console.WriteLine($"\n{'=',60}");
        Console.WriteLine($"SETUP SETTIMANALE - Settimana {Week}/{Year}");
        Console.WriteLine($"Periodo: {StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}");
        Console.WriteLine($"Generato il: {GenerationDate:dd/MM/yyyy HH:mm}");
        Console.WriteLine($"{'=',60}\n");

        Console.WriteLine($"STRATEGIE ABILITATE ({EnabledStrategies.Count}):");
        foreach (var strategy in EnabledStrategies)
        {
            var eval = StrategyEvaluations.First(e => e.StrategyName == strategy);
            Console.WriteLine($"  ✓ {strategy}");
            Console.WriteLine($"    Score: {eval.FinalScore:F2} | Rank: {eval.Rank}");
            Console.WriteLine($"    Return: {eval.AvgReturn:F2}% | Sharpe: {eval.AvgSharpeRatio:F2}");
            Console.WriteLine($"    DD: {eval.AvgDrawdown:P2} | Win Rate: {eval.AvgWinRate:P2}");
        }

        var disabled = StrategyEvaluations.Where(e => !e.IsEnabled).ToList();
        if (disabled.Any())
        {
            Console.WriteLine($"\nSTRATEGIE DISABILITATE ({disabled.Count}):");
            foreach (var eval in disabled)
            {
                Console.WriteLine($"  ✗ {eval.StrategyName} (Score: {eval.FinalScore:F2})");
                if (eval.DisqualificationReasons.Any())
                {
                    Console.WriteLine($"    Motivo: {eval.DisqualificationReasons.First()}");
                }
            }
        }

        Console.WriteLine($"\nCONFIGURAZIONE RISK MANAGEMENT:");
        Console.WriteLine($"  - Evaluation Weeks: {Configuration.EvaluationWeeks ?? 4}");
        Console.WriteLine($"  - Min Win Rate: {Configuration.MinWinRate:P0}");
        Console.WriteLine($"  - Max Drawdown: {Configuration.MaxDrawdown:P0}");
        Console.WriteLine($"  - Stop Loss: {Configuration.StopLossPercent:P0}");
        Console.WriteLine($"  - Min Net Profit: ${Configuration.MinNetProfit:F0}");
        Console.WriteLine($"{'=',60}\n");
    }
}
