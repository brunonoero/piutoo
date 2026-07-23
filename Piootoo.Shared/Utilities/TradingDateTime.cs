namespace Piootoo.Shared.Utilities;

using Piootoo.Shared.Models;

/// <summary>
/// Utility per allineare date/ora al feed JSON (sempre UTC).
/// </summary>
public static class TradingDateTime
{
    /// <summary>
    /// Converte un DateTime al wall-clock UTC usato dal feed, senza shift di fuso.
    /// I componenti anno/mese/giorno/ora restano identici; cambia solo il Kind.
    /// </summary>
    public static DateTime ToFeedUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : new DateTime(value.Ticks, DateTimeKind.Utc);

    /// <summary>
    /// Arrotonda verso il basso al timeframe, mantenendo Kind UTC.
    /// </summary>
    public static DateTime RoundDownToTimeframeUtc(DateTime date, int timeframeMinutes)
    {
        date = ToFeedUtc(date);

        if (timeframeMinutes >= 1440)
        {
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        if (timeframeMinutes >= 60)
        {
            var hours = date.Hour;
            if (timeframeMinutes == 240)
            {
                hours = (hours / 4) * 4;
            }

            return new DateTime(date.Year, date.Month, date.Day, hours, 0, 0, DateTimeKind.Utc);
        }

        var totalMinutes = date.Hour * 60 + date.Minute;
        var roundedMinutes = (totalMinutes / timeframeMinutes) * timeframeMinutes;
        var roundedHours = roundedMinutes / 60;
        var minutes = roundedMinutes % 60;
        return new DateTime(date.Year, date.Month, date.Day, roundedHours, minutes, 0, DateTimeKind.Utc);
    }

    public static void NormalizeCandlesToUtc(IEnumerable<OhlcvData> candles)
    {
        foreach (var candle in candles)
        {
            candle.DateTime = ToFeedUtc(candle.DateTime);
        }
    }

    /// <summary>
    /// Chiave giorno UTC (YYYYMMDD) per raggruppare candele/signal senza ambiguita' di fuso.
    /// </summary>
    public static int GetUtcDateKey(DateTime value)
    {
        var utc = ToFeedUtc(value);
        return utc.Year * 10000 + utc.Month * 100 + utc.Day;
    }

    public static bool IsSameUtcDay(DateTime left, DateTime right) =>
        GetUtcDateKey(left) == GetUtcDateKey(right);

    public static void NormalizeSignalToUtc(TradeSignal signal) =>
        signal.Date = ToFeedUtc(signal.Date);

    public static DateTime CreateUtc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);
}
