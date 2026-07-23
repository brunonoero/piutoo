namespace Piootoo.Shared.Models;

/// <summary>
/// Configurazione del sistema di scoring per la valutazione delle strategie
/// </summary>
public class ScoringConfiguration
{
    // Pesi per il calcolo dello score (devono sommare a 1.0)
    public decimal ReturnWeight { get; set; } = 0.25m;
    public decimal SharpeRatioWeight { get; set; } = 0.20m;
    public decimal DrawdownWeight { get; set; } = 0.15m;
    public decimal WinRateWeight { get; set; } = 0.10m;
    public decimal ProfitFactorWeight { get; set; } = 0.10m;
    public decimal ConsistencyWeight { get; set; } = 0.10m;
    public decimal CalmarRatioWeight { get; set; } = 0.10m;
    
    // Soglie minime per considerare una strategia
    public decimal MinWinRate { get; set; } = 0.40m; // 40%
    public int MinTotalTrades { get; set; } = 5;
    public decimal MinSharpeRatio { get; set; } = 0.5m;
    public decimal MaxDrawdown { get; set; } = -0.20m; // -20%
    
    // Soglie basate su balance
    public decimal MinFinalBalance { get; set; } = 0;
    public decimal MinNetProfit { get; set; } = 0;
    public decimal MinNetProfitPercent { get; set; } = 0;
    public bool RequirePositiveBalance { get; set; } = true;
    public decimal StopLossPercent { get; set; } = -0.30m;
    
    // Numero di settimane da valutare
    public int? EvaluationWeeks { get; set; } = null;
    
    // Penalità
    public bool PenalizeHighVolatility { get; set; } = true;
    public bool PenalizeConsecutiveLosses { get; set; } = true;
    public bool PenalizeDrawdownRecovery { get; set; } = true;
    public decimal ConsecutiveLossesPenalty { get; set; } = 0.1m;
    public decimal DrawdownRecoveryPenalty { get; set; } = 0.05m;
    
    // Normalizzazione
    public bool UseNormalization { get; set; } = true;
    public bool UseRankBasedScoring { get; set; } = false;

    public void ValidateWeights()
    {
        var totalWeight = ReturnWeight + SharpeRatioWeight + DrawdownWeight + 
                         WinRateWeight + ProfitFactorWeight + ConsistencyWeight + CalmarRatioWeight;
        
        if (Math.Abs(totalWeight - 1.0m) > 0.01m)
        {
            throw new InvalidOperationException(
                $"I pesi devono sommare a 1.0. Totale corrente: {totalWeight}");
        }
    }
}
