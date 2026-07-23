namespace Piootoo.Shared.Models.Sapioo;

/// <summary>
/// Risultato dell'ottimizzazione per una settimana
/// </summary>
public class WeeklyOptimizationResult
{
    public int Year { get; set; }
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    
    // Strategie abilitate per settimana successiva
    public List<StrategyWeight> EnabledStrategies { get; set; } = new();
    
    // Metriche settimana
    public decimal WeeklyProfit { get; set; }
    public decimal WeeklyDrawdown { get; set; }
    public decimal WeeklyEquity { get; set; }
}
