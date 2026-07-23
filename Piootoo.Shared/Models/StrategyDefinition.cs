namespace Piootoo.Shared.Models;

/// <summary>
/// Definizione di una strategia di trading con i suoi parametri
/// </summary>
public class StrategyDefinition
{
    /// <summary>
    /// ID univoco della strategia
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nome della strategia (es. TOP_UA_746)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo del file sorgente
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Simbolo per cui la strategia è progettata (es. @ES, @NQ, @CL)
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Timeframe in minuti (es. 1, 5, 15, 30, 60, 1440)
    /// </summary>
    public int TimeframeMinutes { get; set; } = 15;

    /// <summary>
    /// Tipo di barra corrispondente al timeframe
    /// </summary>
    public string BarType => TimeframeMinutes switch
    {
        1 => "OneMinute",
        5 => "FiveMinute",
        15 => "FifteenMinute",
        30 => "ThirtyMinute",
        60 => "OneHour",
        240 => "FourHour",
        1440 => "Daily",
        10080 => "Weekly",
        _ => "OneMinute"
    };

    /// <summary>
    /// Descrizione della strategia (estratta dal commento del file)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tipo di strategia (TrendFollowing, CounterTrend, Breakout, etc.)
    /// </summary>
    public StrategyType Type { get; set; } = StrategyType.Unknown;

    /// <summary>
    /// Se la strategia è attiva per il trading
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Parametri della strategia (input dal file EasyLanguage)
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Data di ultima modifica del file
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Path completo del file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Chiave univoca symbol|barType
    /// </summary>
    public string Key => $"{Symbol}|{BarType}";

    public override string ToString() => $"{Name} ({Symbol} {TimeframeMinutes}min)";
}

/// <summary>
/// Tipo di strategia
/// </summary>
public enum StrategyType
{
    Unknown,
    TrendFollowing,
    CounterTrend,
    Breakout,
    MeanReversion,
    Momentum,
    Scalping,
    Swing,
    Portfolio
}

/// <summary>
/// Informazioni aggregate sulle strategie per un simbolo
/// </summary>
public class SymbolStrategiesInfo
{
    /// <summary>
    /// Simbolo
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Numero totale di strategie per questo simbolo
    /// </summary>
    public int TotalStrategies { get; set; }

    /// <summary>
    /// Strategie attive
    /// </summary>
    public int ActiveStrategies { get; set; }

    /// <summary>
    /// Timeframe disponibili per questo simbolo
    /// </summary>
    public List<int> AvailableTimeframes { get; set; } = new();

    /// <summary>
    /// Dettaglio strategie
    /// </summary>
    public List<StrategyDefinition> Strategies { get; set; } = new();
}
