using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Copre la traduzione di <c>data2</c>: la serie di sessione ricostruita dal feed intraday, l'ATR
/// arretrato e il latch su <c>sessionlastbar data2</c>.
/// </summary>
public class SessionSeriesTests
{
    /// <summary>
    /// Orologio neutro: queste prove costruiscono serie sintetiche i cui orari UTC coincidono per
    /// costruzione con gli orari di sessione dichiarati, e verificano la <b>segmentazione</b> di
    /// EasyLib, non la conversione di fuso. Con l'orologio dello strumento le barre finirebbero in
    /// sessioni diverse e la prova non direbbe piu' nulla sulla logica che vuole coprire. La
    /// conversione ha le sue prove in <c>SessionClockTests</c>.
    /// </summary>
    private static readonly SessionClock Orologio = SessionClock.Utc;

    private const int SessionStart = 1800;
    private const int SessionEnd = 1700;

    [Fact]
    public void BuildSessionSeries_SegmentsLikeOHLCMulti5()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 4);
        var last = bars[^1].DateTime;

        OHLCMulti5(Orologio, SessionStart, SessionEnd, bars, last, out var ohlc);
        var sessions = BuildSessionSeries(Orologio, SessionStart, SessionEnd, bars, last);

        // Se le due funzioni segmentassero diversamente, d0/d1/d2 e la coda della serie
        // parlerebbero di sessioni diverse: è l'invariante che tiene insieme la traduzione.
        Assert.Equal(4, sessions.Length);
        AssertSameSession(ohlc, dayIndex: 0, sessions[^1]);
        AssertSameSession(ohlc, dayIndex: 1, sessions[^2]);
        AssertSameSession(ohlc, dayIndex: 2, sessions[^3]);
    }

    [Fact]
    public void BuildSessionSeries_LastElementIsTheFormingSession()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 3);

        // Ci si ferma a metà dell'ultima sessione: l'aggregato deve fermarsi lì, non anticipare.
        var midSession = bars.Last(bar => GetHhmm(bar.DateTime) == 900);
        var truncated = bars.Where(bar => bar.DateTime <= midSession.DateTime).ToArray();

        var sessions = BuildSessionSeries(Orologio, SessionStart, SessionEnd, truncated, midSession.DateTime);
        var forming = truncated.Where(bar => bar.DateTime >= LastSessionStart(truncated)).ToArray();

        Assert.Equal(3, sessions.Length);
        Assert.Equal(midSession.Close, sessions[^1].Close);
        Assert.Equal(forming.Max(bar => bar.High), sessions[^1].High);
        Assert.Equal(forming.Min(bar => bar.Low), sessions[^1].Low);
    }

    [Fact]
    public void BuildSessionSeries_DoesNotMergeAcrossTheSessionGap()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 3);
        var sessions = BuildSessionSeries(Orologio, SessionStart, SessionEnd, bars, bars[^1].DateTime);

        // Ogni sessione ha un livello di prezzo suo: aggregati fusi darebbero estremi condivisi.
        Assert.Equal(3, sessions.Select(session => session.High).Distinct().Count());
        Assert.All(sessions, session => Assert.True(session.High >= session.Low));
        Assert.True(sessions[1].Low > sessions[0].High);
    }

    [Fact]
    public void BuildSessionSeries_ReordersASeriesOutOfOrder()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 3);
        var last = bars.Max(bar => bar.DateTime);
        var shuffled = bars.OrderBy(bar => bar.DateTime.Ticks % 7).ThenBy(bar => bar.Close).ToArray();

        var expected = BuildSessionSeries(Orologio, SessionStart, SessionEnd, bars, last);
        var actual = BuildSessionSeries(Orologio, SessionStart, SessionEnd, shuffled, last);

        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(
            expected.Select(session => (session.Open, session.High, session.Low, session.Close)),
            actual.Select(session => (session.Open, session.High, session.Low, session.Close)));
    }

    [Fact]
    public void AvgTrueRange_BarsAgoExcludesTheBarsItSkips()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 2);

        // Un'esplosione di volatilità sull'ultima barra non deve entrare in una media arretrata:
        // è il punto di `AvgTrueRange(5)[1] of data2`, che confronta senza autoincludersi.
        var withoutSpike = AvgTrueRange(bars, periods: 5, barsAgo: 1);
        bars[^1].High += 500m;
        var withSpike = AvgTrueRange(bars, periods: 5, barsAgo: 1);

        Assert.Equal(withoutSpike, withSpike);
        Assert.True(AvgTrueRange(bars, periods: 5) > withSpike);
        Assert.Equal(AvgTrueRange(bars, periods: 5), AvgTrueRange(bars, periods: 5, barsAgo: 0));
    }

    [Fact]
    public void LastBarOfPreviousSession_IsTheSessionCloseNotThePreviousBar()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 3);
        var last = bars[^1].DateTime;

        var previous = LastBarOfPreviousSession(Orologio, SessionStart, SessionEnd, bars, last);

        var lastSessionStart = LastSessionStart(bars);
        var expected = bars.Last(bar => bar.DateTime < lastSessionStart);

        Assert.NotNull(previous);
        Assert.Equal(expected.DateTime, previous.DateTime);
        Assert.NotEqual(bars[^2].DateTime, previous.DateTime);
    }

    /// <summary>
    /// Sessione ancorata alla mezzanotte, il confine usato da <c>PTS_NQ_PCH_001_15</c> per allinearsi al
    /// motore di riferimento. Il confronto stretto <c>t &gt; sessionStartTime</c> scarterebbe la barra
    /// delle 00:00 da ogni sessione, perdendone una al giorno da d0..d5 senza che nulla protesti:
    /// qui si verifica che entri nel giorno e che le due funzioni segmentino ancora allo stesso modo.
    /// </summary>
    [Fact]
    public void CalendarDaySession_KeepsTheMidnightBarAndSplitsOnTheDate()
    {
        var bars = CalendarDays(dayCount: 3);
        var last = bars[^1].DateTime;

        OHLCMulti5(Orologio, 0, 2359, bars, last, out var ohlc);
        var sessions = BuildSessionSeries(Orologio, 0, 2359, bars, last);

        Assert.Equal(3, sessions.Length);
        AssertSameSession(ohlc, dayIndex: 0, sessions[^1]);
        AssertSameSession(ohlc, dayIndex: 1, sessions[^2]);

        // Nessuna barra fuori da tutte le sessioni: i volumi aggregati sono quelli della serie.
        Assert.Equal(bars.Sum(bar => bar.Volume), sessions.Sum(session => session.Volume));

        // L'apertura del giorno è quella delle 00:00. Con il confine stretto sarebbe delle 00:15.
        var midnight = bars.Single(bar => bar.DateTime == bars[0].DateTime.Date.AddDays(1));
        Assert.Equal(midnight.Open, sessions[1].Open);
    }

    /// <summary>Serie continua a 15 minuti su giorni di calendario pieni, senza buchi di sessione.</summary>
    private static OhlcvData[] CalendarDays(int dayCount)
    {
        var bars = new List<OhlcvData>();
        var first = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var day = 0; day < dayCount; day++)
        {
            // Ogni giorno su un gradino di prezzo suo, così gli aggregati sono distinguibili.
            var price = 1000m + day * 100m;
            var start = first.AddDays(day);

            for (var cursor = start; cursor < start.AddDays(1); cursor = cursor.AddMinutes(15))
            {
                bars.Add(new OhlcvData
                {
                    DateTime = cursor,
                    Open = price,
                    High = price + 3m,
                    Low = price - 2m,
                    Close = price + 1m,
                    Volume = 10m
                });
                price += 0.5m;
            }
        }

        return bars.ToArray();
    }

    [Fact]
    public void LastBarOfPreviousSession_IsNullWithoutACompletedSession()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 1);
        Assert.Null(LastBarOfPreviousSession(Orologio, SessionStart, SessionEnd, bars, bars[^1].DateTime));
    }

    private static void AssertSameSession(decimal[] ohlc, int dayIndex, OhlcvData session)
    {
        var offset = dayIndex * 4;
        Assert.Equal(ohlc[offset], session.Open);
        Assert.Equal(ohlc[offset + 1], session.High);
        Assert.Equal(ohlc[offset + 2], session.Low);
        Assert.Equal(ohlc[offset + 3], session.Close);
    }

    private static DateTime LastSessionStart(OhlcvData[] bars)
    {
        for (var index = bars.Length - 1; index > 0; index--)
        {
            if (GetHhmm(bars[index].DateTime) > SessionStart &&
                GetHhmm(bars[index - 1].DateTime) <= SessionStart)
            {
                return bars[index].DateTime;
            }
        }

        return bars[0].DateTime;
    }
}

internal static class OvernightSeries
{
    internal static OhlcvData[] Build(int timeframeMinutes, int sessionCount)
    {
        var bars = new List<OhlcvData>();
        var firstEvening = new DateTime(2024, 1, 1, 18, 0, 0, DateTimeKind.Utc);

        for (var session = 0; session < sessionCount; session++)
        {
            var start = firstEvening.AddDays(session).AddMinutes(timeframeMinutes);
            var end = start.Date.AddDays(1).AddHours(17);
            var price = 1000m + session * 100m;

            for (var cursor = start; cursor < end; cursor = cursor.AddMinutes(timeframeMinutes))
            {
                bars.Add(new OhlcvData
                {
                    DateTime = cursor,
                    Open = price,
                    High = price + 3m,
                    Low = price - 2m,
                    Close = price + 1m,
                    Volume = 10m
                });
                price += 0.5m;
            }
        }

        return bars.ToArray();
    }
}
