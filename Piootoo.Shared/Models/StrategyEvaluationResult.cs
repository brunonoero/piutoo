namespace Piootoo.Shared.Models;

/// <summary>
/// Risultato della valutazione di una strategia
/// </summary>
public class StrategyEvaluationResult
{
    public string StrategyName { get; set; } = string.Empty;
    public decimal FinalScore { get; set; }
    public bool IsEnabled { get; set; }
    public int Rank { get; set; }
    
    // Score componenti
    public Dictionary<string, decimal> ComponentScores { get; set; } = new();
    
    // Metriche aggregate
    public decimal AvgReturn { get; set; }
    public decimal AvgSharpeRatio { get; set; }
    public decimal AvgDrawdown { get; set; }
    public decimal AvgWinRate { get; set; }
    public decimal AvgProfitFactor { get; set; }
    public int TotalTrades { get; set; }
    
    // Ragioni per inclusione/esclusione
    public List<string> QualificationReasons { get; set; } = new();
    public List<string> DisqualificationReasons { get; set; } = new();
    
    public string Summary => 
        $"{StrategyName} - Score: {FinalScore:F2} - Rank: {Rank} - " +
        $"{(IsEnabled ? "✓ ABILITATA" : "✗ DISABILITATA")}";
}
