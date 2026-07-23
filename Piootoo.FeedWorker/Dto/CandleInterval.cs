namespace FeedWorker.Dto;

/// <summary>
/// Tipo di intervallo per le candele
/// </summary>
public enum CandleInterval
{
    OneMinute,
    FiveMinutes,
    FifteenMinutes,
    ThirtyMinutes,
    OneHour,
    FourHours,
    OneDay,
    OneWeek
}

/// <summary>
/// Estensioni per CandleInterval per la conversione in stringhe di folder naming
/// </summary>
public static class CandleIntervalExtensions
{
    /// <summary>
    /// Converte CandleInterval in una stringa per il naming delle cartelle
    /// Convenzione: 1m, 5m, 15m, 30m, 1h, 4h, D, W
    /// </summary>
    public static string ToFolderName(this CandleInterval interval)
    {
        return interval switch
        {
            CandleInterval.OneMinute => "1m",
            CandleInterval.FiveMinutes => "5m",
            CandleInterval.FifteenMinutes => "15m",
            CandleInterval.ThirtyMinutes => "30m",
            CandleInterval.OneHour => "1h",
            CandleInterval.FourHours => "4h",
            CandleInterval.OneDay => "D",
            CandleInterval.OneWeek => "W",
            _ => interval.ToString()
        };
    }
}
