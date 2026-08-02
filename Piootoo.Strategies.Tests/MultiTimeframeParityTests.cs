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
    private const int SessionStart = 1800;
    private const int SessionEnd = 1700;

    [Fact]
    public void BuildSessionSeries_SegmentsLikeOHLCMulti5()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 4);
        var last = bars[^1].DateTime;

        OHLCMulti5(SessionStart, SessionEnd, bars, last, out var ohlc);
        var sessions = BuildSessionSeries(SessionStart, SessionEnd, bars, last);

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

        var sessions = BuildSessionSeries(SessionStart, SessionEnd, truncated, midSession.DateTime);
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
        var sessions = BuildSessionSeries(SessionStart, SessionEnd, bars, bars[^1].DateTime);

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

        var expected = BuildSessionSeries(SessionStart, SessionEnd, bars, last);
        var actual = BuildSessionSeries(SessionStart, SessionEnd, shuffled, last);

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

        var previous = LastBarOfPreviousSession(SessionStart, SessionEnd, bars, last);

        var lastSessionStart = LastSessionStart(bars);
        var expected = bars.Last(bar => bar.DateTime < lastSessionStart);

        Assert.NotNull(previous);
        Assert.Equal(expected.DateTime, previous.DateTime);
        Assert.NotEqual(bars[^2].DateTime, previous.DateTime);
    }

    [Fact]
    public void LastBarOfPreviousSession_IsNullWithoutACompletedSession()
    {
        var bars = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 1);
        Assert.Null(LastBarOfPreviousSession(SessionStart, SessionEnd, bars, bars[^1].DateTime));
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

public class Easy661Data2Tests
{
    [Fact]
    public void HoldsWithExplicitReasonWithoutData2()
    {
        var strategy = new Easy_661_GC_30();
        var bars = OvernightSeries.Build(timeframeMinutes: 30, sessionCount: 4);

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);

        Assert.Equal(SignalType.Hold, signal.Type);
        Assert.Equal("Serie 15m (data2) non disponibile", signal.Reason);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LatchesDirectionFromLastData2BarOfPreviousSession(bool bullish)
    {
        var strategy = new Easy_661_GC_30();
        var bars = OvernightSeries.Build(timeframeMinutes: 30, sessionCount: 4);
        var data2 = OvernightSeries.Build(timeframeMinutes: 15, sessionCount: 4);

        // Si forza la direzione della barra che conta — l'ultima a 15 minuti della sessione chiusa —
        // lasciando rialzista quella immediatamente precedente nella serie. Se la traduzione
        // guardasse "la barra prima" invece del latch di fine sessione, i due casi coinciderebbero.
        var target = LastBarOfClosedSession(data2, bars[^1].DateTime);
        target.Open = bullish ? target.Low : target.High;
        target.Close = bullish ? target.High : target.Low;

        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            AdditionalOhlcv = new Dictionary<int, OhlcvData[]> { [15] = data2 },
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = "TOP_UA_661",
                Symbol = "@GC",
                BarTimeUtc = bars[^1].DateTime
            }
        });

        Assert.Equal(bullish, Assert.IsType<bool>(signal.RuntimeState!["_okLong1"]));
        Assert.Equal(!bullish, Assert.IsType<bool>(signal.RuntimeState["_okShort1"]));
    }

    private static OhlcvData LastBarOfClosedSession(OhlcvData[] data2, DateTime currentDate) =>
        LastBarOfPreviousSession(1800, 1700, data2, currentDate)
        ?? throw new InvalidOperationException("La serie di prova non ha una sessione chiusa.");
}

/// <summary>
/// Serie overnight 18:00→17:00 al timeframe richiesto, con il buco 17:00–18:00 fra una sessione e
/// l'altra. Ogni sessione sta su un gradino di prezzo suo, così gli aggregati sono distinguibili.
/// </summary>
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
