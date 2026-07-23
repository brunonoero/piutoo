using Piootoo.Shared.Models;

namespace Piootoo.Shared.Models.Sapioo;

/// <summary>
/// Risultato completo dell'ottimizzazione Sapioo
/// </summary>
public class SapiooResult
{
    public string JobId { get; set; } = string.Empty;
    public string BacktestingName { get; set; } = string.Empty;
    public RiskManagementParams Parameters { get; set; } = new();
    
    // Per ogni settimana
    public List<WeeklyOptimizationResult> WeeklyResults { get; set; } = new();
    
    // Risultato finale filtrato
    public Trading.TradingSnapshot FinalResult { get; set; } = new();
    public List<EquityPoint> FilteredEquityCurve { get; set; } = new();
    
    // File path dove è salvato
    public string? ResultFilePath { get; set; }
}
