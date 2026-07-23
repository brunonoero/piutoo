namespace FeedWorker.Configuration;

/// <summary>
/// Opzioni per l'aggregazione delle candele 1m in timeframe superiori
/// </summary>
public class AggregationOptions
{
    /// <summary>
    /// Abilita/disabilita l'aggregazione automatica delle candele 1m
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Aggrega le candele 1m in timeframe 15m
    /// </summary>
    public bool AggregateTo15M { get; set; } = true;

    /// <summary>
    /// Aggrega le candele 1m in timeframe 30m
    /// </summary>
    public bool AggregateTo30M { get; set; } = true;

    /// <summary>
    /// Aggrega le candele 1m in timeframe 1h
    /// </summary>
    public bool AggregateTo1H { get; set; } = true;

    /// <summary>
    /// Aggrega le candele 1m in timeframe 4h
    /// </summary>
    public bool AggregateTo4H { get; set; } = true;

    /// <summary>
    /// Aggrega le candele 1m in timeframe Daily
    /// </summary>
    public bool AggregateToDaily { get; set; } = true;

    /// <summary>
    /// Aggrega le candele 1m in timeframe Weekly
    /// </summary>
    public bool AggregateToWeekly { get; set; } = true;
}
