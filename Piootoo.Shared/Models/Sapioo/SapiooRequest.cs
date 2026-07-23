namespace Piootoo.Shared.Models.Sapioo;

/// <summary>
/// Richiesta di avvio ottimizzazione Sapioo
/// </summary>
public class SapiooRequest
{
    public string BacktestingName { get; set; } = string.Empty;
    public RiskManagementParams RiskParams { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public int EvaluationPeriodWeeks { get; set; } = 4;
}
