namespace Piootoo.Shared.Models.Backtesting;

/// <summary>
/// Risultato completo di un backtesting
/// </summary>
public class BacktestingResult
{
    public string JobId { get; set; } = string.Empty;
    public string SetupName { get; set; } = string.Empty;
    public string SetupId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    
    /// <summary>
    /// Data e ora di creazione del risultato del backtesting
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Risultati globali per ora
    public List<HourlyResult> HourlyResults { get; set; } = new();
    
    // Risultati per strategia per ora
    public List<StrategyHourlyResult> StrategyResults { get; set; } = new();
    
    // Aggregati settimanali
    public List<WeeklyResult> WeeklyResults { get; set; } = new();
    
    // Metriche globali
    public decimal FinalEquity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal TotalReturn { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    
    // Lista strategie utilizzate (mantenuta per retrocompatibilità)
    public List<string> StrategiesUsed { get; set; } = new();
    
    // Lista dettagliata strategie utilizzate con symbol e timeframe
    public List<StrategyInfo> StrategiesInfo { get; set; } = new();
    
    // File path dove è salvato
    public string? ResultFilePath { get; set; }

    // File HTML con andamento equity per strategia
    public string? HtmlReportFilePath { get; set; }

    // File JSON con tutti i TradeSignal emessi dalle strategie
    public string? TradeSignalsFilePath { get; set; }
}
