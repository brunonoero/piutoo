namespace Piootoo.Shared.Models;

/// <summary>
/// Risultato dell'ottimizzazione dei pesi
/// </summary>
public class OptimizationResult
{
    public ScoringConfiguration BestConfiguration { get; set; } = new();
    public decimal BestScore { get; set; }
    public List<(ScoringConfiguration Config, decimal Score)> AllResults { get; set; } = new();
}
