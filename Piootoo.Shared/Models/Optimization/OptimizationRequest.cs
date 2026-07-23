namespace Piootoo.Shared.Models.Optimization;

/// <summary>
/// Selezione di un simbolo con il relativo timeframe
/// </summary>
public class SymbolSelection
{
    /// <summary>
    /// Nome del simbolo (es. @ES, @NQ)
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Tipo di barra/timeframe (es. OneMinute, FiveMinute, Daily)
    /// </summary>
    public string BarType { get; set; } = "OneMinute";

    /// <summary>
    /// Chiave univoca per identificare la selezione
    /// </summary>
    public string Key => $"{Symbol}|{BarType}";

    public override string ToString() => $"{Symbol} ({BarType})";

    public override bool Equals(object? obj)
    {
        if (obj is SymbolSelection other)
            return Symbol == other.Symbol && BarType == other.BarType;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Symbol, BarType);
}

/// <summary>
/// Request per l'ottimizzazione di un setup
/// </summary>
public class OptimizationRequest
{
    /// <summary>
    /// Nome identificativo del setup
    /// </summary>
    public string SetupName { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione del setup
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ID del backtesting da cui partire per l'ottimizzazione
    /// </summary>
    public string BacktestingId { get; set; } = string.Empty;

    /// <summary>
    /// Simboli con timeframe su cui eseguire l'ottimizzazione (derivati dal backtesting)
    /// </summary>
    public List<SymbolSelection> Symbols { get; set; } = new() { new SymbolSelection { Symbol = "@ES", BarType = "OneMinute" } };

    /// <summary>
    /// Periodo di valutazione
    /// </summary>
    public EvaluationPeriod EvaluationPeriod { get; set; } = new();

    /// <summary>
    /// Parametri di ottimizzazione (obiettivi)
    /// </summary>
    public OptimizationParameters OptimizationParams { get; set; } = new();

    /// <summary>
    /// Parametri di rischio (vincoli)
    /// </summary>
    public RiskParameters RiskParams { get; set; } = new();

    /// <summary>
    /// Configurazione dell'algoritmo di ottimizzazione
    /// </summary>
    public AlgorithmSettings AlgorithmSettings { get; set; } = new();
}

/// <summary>
/// Periodo di valutazione per l'ottimizzazione
/// </summary>
public class EvaluationPeriod
{
    /// <summary>
    /// Tipo di periodo: Weeks, Months, DateRange
    /// </summary>
    public PeriodType Type { get; set; } = PeriodType.Weeks;

    /// <summary>
    /// Numero di settimane (se Type = Weeks)
    /// </summary>
    public int Weeks { get; set; } = 4;

    /// <summary>
    /// Numero di mesi (se Type = Months)
    /// </summary>
    public int Months { get; set; } = 1;

    /// <summary>
    /// Data inizio (se Type = DateRange)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Data fine (se Type = DateRange)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Calcola le date effettive del periodo
    /// </summary>
    public (DateTime Start, DateTime End) GetDateRange()
    {
        var end = DateTime.Now;
        DateTime start;

        switch (Type)
        {
            case PeriodType.Weeks:
                start = end.AddDays(-Weeks * 7);
                break;
            case PeriodType.Months:
                start = end.AddMonths(-Months);
                break;
            case PeriodType.DateRange:
                start = StartDate ?? end.AddDays(-28);
                end = EndDate ?? DateTime.Now;
                break;
            default:
                start = end.AddDays(-28);
                break;
        }

        return (start, end);
    }
}

public enum PeriodType
{
    Weeks,
    Months,
    DateRange
}

/// <summary>
/// Parametri di ottimizzazione (obiettivi da massimizzare/minimizzare)
/// </summary>
public class OptimizationParameters
{
    /// <summary>
    /// Obiettivo principale dell'ottimizzazione
    /// </summary>
    public OptimizationObjective PrimaryObjective { get; set; } = OptimizationObjective.SharpeRatio;

    /// <summary>
    /// Peso del rendimento totale (0-1)
    /// </summary>
    public decimal ReturnWeight { get; set; } = 0.25m;

    /// <summary>
    /// Peso dello Sharpe Ratio (0-1)
    /// </summary>
    public decimal SharpeWeight { get; set; } = 0.25m;

    /// <summary>
    /// Peso del rapporto Profit/Drawdown (0-1)
    /// </summary>
    public decimal ProfitDrawdownRatioWeight { get; set; } = 0.20m;

    /// <summary>
    /// Peso del Win Rate (0-1)
    /// </summary>
    public decimal WinRateWeight { get; set; } = 0.15m;

    /// <summary>
    /// Peso del Profit Factor (0-1)
    /// </summary>
    public decimal ProfitFactorWeight { get; set; } = 0.10m;

    /// <summary>
    /// Peso della consistenza (0-1)
    /// </summary>
    public decimal ConsistencyWeight { get; set; } = 0.05m;

    /// <summary>
    /// Target minimo di rendimento (%)
    /// </summary>
    public decimal? TargetReturn { get; set; }

    /// <summary>
    /// Target minimo Sharpe Ratio
    /// </summary>
    public decimal? TargetSharpe { get; set; }

    /// <summary>
    /// Target minimo rapporto Profit/DD
    /// </summary>
    public decimal? TargetProfitDdRatio { get; set; }

    /// <summary>
    /// Valida che i pesi sommino a 1
    /// </summary>
    public bool ValidateWeights()
    {
        var total = ReturnWeight + SharpeWeight + ProfitDrawdownRatioWeight + 
                    WinRateWeight + ProfitFactorWeight + ConsistencyWeight;
        return Math.Abs(total - 1.0m) < 0.01m;
    }

    /// <summary>
    /// Normalizza i pesi per farli sommare a 1
    /// </summary>
    public void NormalizeWeights()
    {
        var total = ReturnWeight + SharpeWeight + ProfitDrawdownRatioWeight + 
                    WinRateWeight + ProfitFactorWeight + ConsistencyWeight;
        
        if (total > 0)
        {
            ReturnWeight /= total;
            SharpeWeight /= total;
            ProfitDrawdownRatioWeight /= total;
            WinRateWeight /= total;
            ProfitFactorWeight /= total;
            ConsistencyWeight /= total;
        }
    }
}

public enum OptimizationObjective
{
    /// <summary>
    /// Massimizza il rendimento totale
    /// </summary>
    MaxReturn,

    /// <summary>
    /// Massimizza lo Sharpe Ratio
    /// </summary>
    SharpeRatio,

    /// <summary>
    /// Massimizza il rapporto Profit/Drawdown
    /// </summary>
    ProfitDrawdownRatio,

    /// <summary>
    /// Minimizza il Drawdown mantenendo un rendimento target
    /// </summary>
    MinDrawdown,

    /// <summary>
    /// Massimizza il Calmar Ratio
    /// </summary>
    CalmarRatio,

    /// <summary>
    /// Ottimizzazione multi-obiettivo pesata
    /// </summary>
    WeightedMultiObjective
}

/// <summary>
/// Parametri di rischio (vincoli da rispettare)
/// </summary>
public class RiskParameters
{
    /// <summary>
    /// Drawdown massimo accettabile (es. -0.15 = -15%)
    /// </summary>
    public decimal MaxDrawdown { get; set; } = -0.15m;

    /// <summary>
    /// Drawdown massimo in valore assoluto
    /// </summary>
    public decimal? MaxDrawdownValue { get; set; }

    /// <summary>
    /// Perdite consecutive massime accettabili
    /// </summary>
    public int MaxConsecutiveLosses { get; set; } = 5;

    /// <summary>
    /// Win Rate minimo richiesto (es. 0.45 = 45%)
    /// </summary>
    public decimal MinWinRate { get; set; } = 0.45m;

    /// <summary>
    /// Sharpe Ratio minimo richiesto
    /// </summary>
    public decimal MinSharpeRatio { get; set; } = 0.5m;

    /// <summary>
    /// Profit Factor minimo richiesto
    /// </summary>
    public decimal MinProfitFactor { get; set; } = 1.2m;

    /// <summary>
    /// Numero minimo di trade per validare la strategia
    /// </summary>
    public int MinTrades { get; set; } = 10;

    /// <summary>
    /// Volatilità massima accettabile
    /// </summary>
    public decimal? MaxVolatility { get; set; }

    /// <summary>
    /// Stop loss percentuale sul capitale
    /// </summary>
    public decimal StopLossPercent { get; set; } = -0.20m;

    /// <summary>
    /// Richiede balance sempre positivo
    /// </summary>
    public bool RequirePositiveBalance { get; set; } = true;

    /// <summary>
    /// Rapporto minimo Profit/Drawdown
    /// </summary>
    public decimal? MinProfitDrawdownRatio { get; set; }
}

/// <summary>
/// Configurazione dell'algoritmo di ottimizzazione
/// </summary>
public class AlgorithmSettings
{
    /// <summary>
    /// Numero di iterazioni/run dell'ottimizzazione
    /// </summary>
    public int Iterations { get; set; } = 100;

    /// <summary>
    /// Popolazione per algoritmi genetici
    /// </summary>
    public int PopulationSize { get; set; } = 50;

    /// <summary>
    /// Seed per riproducibilità (null = random)
    /// </summary>
    public int? RandomSeed { get; set; }

    /// <summary>
    /// Usa Walk-Forward optimization
    /// </summary>
    public bool UseWalkForward { get; set; } = false;

    /// <summary>
    /// Percentuale dati per in-sample (Walk-Forward)
    /// </summary>
    public decimal InSamplePercent { get; set; } = 0.7m;

    /// <summary>
    /// Salva risultati intermedi
    /// </summary>
    public bool SaveIntermediateResults { get; set; } = false;

    /// <summary>
    /// Parallelizza l'esecuzione
    /// </summary>
    public bool Parallelize { get; set; } = true;

    /// <summary>
    /// Numero massimo di thread (null = auto)
    /// </summary>
    public int? MaxThreads { get; set; }
}
