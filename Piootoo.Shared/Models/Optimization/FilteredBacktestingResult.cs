using Piootoo.Shared.Models.Backtesting;

namespace Piootoo.Shared.Models.Optimization;

/// <summary>
/// Risultato del backtesting filtrato dall'ottimizzazione
/// Contiene solo i dati delle strategie che hanno superato i filtri settimanali
/// </summary>
public class FilteredBacktestingResult
{
    /// <summary>
    /// ID del backtesting originale
    /// </summary>
    public string OriginalBacktestingId { get; set; } = string.Empty;

    /// <summary>
    /// Nome del setup di ottimizzazione
    /// </summary>
    public string SetupName { get; set; } = string.Empty;

    /// <summary>
    /// Data e ora dell'ottimizzazione
    /// </summary>
    public DateTime OptimizationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date del backtesting originale
    /// </summary>
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Capitale iniziale
    /// </summary>
    public decimal InitialCapital { get; set; }

    /// <summary>
    /// Risultati orari aggregati (solo strategie filtrate)
    /// </summary>
    public List<HourlyResult> HourlyResults { get; set; } = new();

    /// <summary>
    /// Risultati settimanali aggregati (solo strategie filtrate)
    /// </summary>
    public List<FilteredWeeklyResult> WeeklyResults { get; set; } = new();

    /// <summary>
    /// Metriche globali del backtesting filtrato
    /// </summary>
    public decimal FinalEquity { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal TotalReturn { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }

    /// <summary>
    /// Elenco delle strategie abilitate per la prossima settimana con il fattore moltiplicativo delle size
    /// </summary>
    public List<StrategyAllocation> EnabledStrategiesForNextWeek { get; set; } = new();

    /// <summary>
    /// Dettaglio per ogni strategia: settimane in cui è stata attiva
    /// </summary>
    public List<StrategyWeeklyStatus> StrategyStatuses { get; set; } = new();

    /// <summary>
    /// Parametri di filtro usati
    /// </summary>
    public RiskParameters FilterParameters { get; set; } = new();

    /// <summary>
    /// Statistiche di ottimizzazione
    /// </summary>
    public FilteredOptimizationStats Stats { get; set; } = new();
}

/// <summary>
/// Risultato settimanale filtrato con elenco strategie attive e allocazioni
/// </summary>
public class FilteredWeeklyResult
{
    public int Year { get; set; }
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    
    /// <summary>
    /// Profit della settimana (solo strategie filtrate)
    /// </summary>
    public decimal WeeklyProfit { get; set; }
    
    /// <summary>
    /// Equity a fine settimana
    /// </summary>
    public decimal WeeklyEquity { get; set; }
    
    /// <summary>
    /// Drawdown della settimana
    /// </summary>
    public decimal WeeklyDrawdown { get; set; }
    
    /// <summary>
    /// Win rate della settimana
    /// </summary>
    public decimal WinRate { get; set; }
    
    /// <summary>
    /// Numero totale di trade della settimana
    /// </summary>
    public int TotalTrades { get; set; }
    
    /// <summary>
    /// Trade vincenti della settimana
    /// </summary>
    public int WinningTrades { get; set; }
    
    /// <summary>
    /// Strategie attive in questa settimana (solo nomi per retrocompatibilità)
    /// </summary>
    public List<string> ActiveStrategies { get; set; } = new();

    /// <summary>
    /// Allocazioni complete per la settimana SUCCESSIVA (strategie + moltiplicatori)
    /// Usare questo per il trading realtime
    /// </summary>
    public List<StrategyAllocation> AllocationsForNextWeek { get; set; } = new();
    
    /// <summary>
    /// Strategie disabilitate in questa settimana con motivo
    /// </summary>
    public List<StrategyDisqualification> DisabledStrategies { get; set; } = new();
}

/// <summary>
/// Status di una strategia settimana per settimana
/// </summary>
public class StrategyWeeklyStatus
{
    /// <summary>
    /// Nome della strategia
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Simbolo
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Timeframe in minuti
    /// </summary>
    public int TimeframeMinutes { get; set; }

    /// <summary>
    /// Settimane in cui la strategia era attiva
    /// </summary>
    public List<int> ActiveWeeks { get; set; } = new();

    /// <summary>
    /// Settimane in cui la strategia era disabilitata
    /// </summary>
    public List<int> DisabledWeeks { get; set; } = new();

    /// <summary>
    /// Profit totale quando attiva
    /// </summary>
    public decimal TotalProfitWhenActive { get; set; }

    /// <summary>
    /// Profit totale se fosse stata sempre attiva
    /// </summary>
    public decimal TotalProfitIfAlwaysActive { get; set; }

    /// <summary>
    /// Score medio
    /// </summary>
    public decimal AverageScore { get; set; }

    /// <summary>
    /// È attiva per la prossima settimana?
    /// </summary>
    public bool IsEnabledForNextWeek { get; set; }

    /// <summary>
    /// Fattore moltiplicativo delle size per la prossima settimana
    /// </summary>
    public decimal SizeMultiplier { get; set; } = 1.0m;
}

/// <summary>
/// Motivo di disqualifica di una strategia
/// </summary>
public class StrategyDisqualification
{
    public string StrategyName { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = new();
    public decimal Score { get; set; }
}

/// <summary>
/// Allocazione di una strategia con fattore moltiplicativo delle size
/// </summary>
public class StrategyAllocation
{
    /// <summary>
    /// Nome della strategia
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Simbolo
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Timeframe in minuti
    /// </summary>
    public int TimeframeMinutes { get; set; }

    /// <summary>
    /// Fattore moltiplicativo delle size (1.0 = base, 2.0 = doppio, 0.5 = metà)
    /// </summary>
    public decimal SizeMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Percentuale di allocazione del capitale (0-100)
    /// </summary>
    public decimal AllocationPercent { get; set; }

    /// <summary>
    /// Score della strategia che ha determinato l'allocazione
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// Rank della strategia (1 = migliore)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Metriche chiave della strategia
    /// </summary>
    public StrategyMetricsSummary Metrics { get; set; } = new();
}

/// <summary>
/// Riepilogo metriche chiave di una strategia
/// </summary>
public class StrategyMetricsSummary
{
    public decimal WinRate { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal ProfitFactor { get; set; }
    public int TotalTrades { get; set; }
}

/// <summary>
/// Statistiche dell'ottimizzazione filtrata
/// </summary>
public class FilteredOptimizationStats
{
    /// <summary>
    /// Numero totale di strategie nel backtesting originale
    /// </summary>
    public int TotalStrategiesInBacktesting { get; set; }

    /// <summary>
    /// Numero medio di strategie attive per settimana
    /// </summary>
    public decimal AverageActiveStrategiesPerWeek { get; set; }

    /// <summary>
    /// Settimane analizzate
    /// </summary>
    public int WeeksAnalyzed { get; set; }

    /// <summary>
    /// Settimane di lookback per filtro
    /// </summary>
    public int LookbackWeeks { get; set; }

    /// <summary>
    /// Profit totale del backtesting originale (non filtrato)
    /// </summary>
    public decimal OriginalTotalProfit { get; set; }

    /// <summary>
    /// Profit totale dopo il filtro
    /// </summary>
    public decimal FilteredTotalProfit { get; set; }

    /// <summary>
    /// Differenza percentuale
    /// </summary>
    public decimal ProfitDifferencePercent { get; set; }

    /// <summary>
    /// Max drawdown originale
    /// </summary>
    public decimal OriginalMaxDrawdown { get; set; }

    /// <summary>
    /// Max drawdown filtrato
    /// </summary>
    public decimal FilteredMaxDrawdown { get; set; }
}
