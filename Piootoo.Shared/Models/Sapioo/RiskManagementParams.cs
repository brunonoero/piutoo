namespace Piootoo.Shared.Models.Sapioo;

/// <summary>
/// Parametri di risk management per l'ottimizzazione Sapioo
/// </summary>
public class RiskManagementParams
{
    // Filtri eliminatori (se non rispettati, strategia esclusa)
    public decimal MaxDrawdownPercent { get; set; } = 20m;      // Max DD accettabile
    public decimal MinWinRate { get; set; } = 40m;              // Min % trade vincenti
    public decimal MinProfitFactor { get; set; } = 1.2m;        // Profit/Loss ratio
    public int MaxConsecutiveLosses { get; set; } = 5;          // Max perdite consecutive
    
    // Filtri di qualita (ponderano il peso)
    public decimal MinSharpeRatio { get; set; } = 0.5m;         // Risk-adjusted return
    public decimal MinRecoveryFactor { get; set; } = 1.0m;      // NetProfit/MaxDD
    public decimal MaxVolatility { get; set; } = 30m;           // Volatilita rendimenti
    
    // Parametri temporali
    public int WeeksLookback { get; set; } = 4;                 // Settimane da analizzare
}
