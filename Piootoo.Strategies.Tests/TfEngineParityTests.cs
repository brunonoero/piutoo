using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class TfEngineParityTests
{
    [Fact]
    public void TfM_UsesCompletedPreviousSessionLevelsAndDeclaresPythonSessionPolicies()
    {
        var strategy = new TestTfMirrored();
        var bars = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc));

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);
        var shortSignal = Assert.Single(signal.CompanionSignals!);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(158m, signal.Price); // H_d1 della sessione 06/01 17:00–07/01 16:59.
        Assert.Equal(146m, shortSignal.Price); // L_d1 della stessa sessione completa.
        Assert.Equal(SignalType.Sell, shortSignal.Type);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
        Assert.Equal(1, signal.MaxEntriesPerSession);
        Assert.Equal(1, shortSignal.MaxEntriesPerSession);
        Assert.Equal(new DateTime(2024, 1, 7, 17, 0, 0, DateTimeKind.Utc), signal.EntrySessionStartUtc);
        Assert.Equal(signal.EntrySessionStartUtc, shortSignal.EntrySessionStartUtc);
        Assert.Equal(new DateTime(2024, 1, 8, 16, 59, 0, DateTimeKind.Utc), signal.CloseAtUtc);
        Assert.Equal(signal.CloseAtUtc, shortSignal.CloseAtUtc);
    }

    [Fact]
    public void TfM_UsesPythonHourWindowAndMondayBasedDayFilter()
    {
        var strategy = new TestTfMirrored { Start = 16, End = 3 };

        var outside = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc));
        var overnight = BuildSessions(new DateTime(2024, 1, 8, 2, 0, 0, DateTimeKind.Utc));
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(outside, outside[^1].DateTime).Type);
        Assert.Equal(SignalType.Buy, strategy.GenerateSignal(overnight, overnight[^1].DateTime).Type);

        strategy.Skip = 4; // Python: Monday = 0, Friday = 4.
        var friday = BuildSessions(new DateTime(2024, 1, 12, 2, 0, 0, DateTimeKind.Utc));
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(friday, friday[^1].DateTime).Type);
    }

    [Fact]
    public void TfU_AppliesIndependentFastPatternsAndLeavesDailyPositionsMultiday()
    {
        var strategy = new TestTfUnmirrored
        {
            LongNo = 152 // Il gate negativo long diventa vero e inibisce solo il long.
        };
        var bars = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc));

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Null(signal.CompanionSignals);

        var daily = new TestTfUnmirroredDaily();
        var dailySignal = daily.GenerateSignal(bars, bars[^1].DateTime);
        Assert.Null(dailySignal.CloseAtUtc);
    }

    private static OhlcvData[] BuildSessions(DateTime current)
    {
        var bars = new List<OhlcvData>();
        for (var day = -7; day <= -1; day++)
        {
            var sessionDate = current.Date.AddDays(day);
            var basePrice = 100m + (day + 7) * 10m;
            var start = sessionDate.AddHours(17).AddMinutes(5);
            var end = sessionDate.AddDays(1).AddHours(16);
            if (start <= current)
                bars.Add(Bar(start, basePrice, basePrice + 8m, basePrice - 4m));
            if (end <= current)
                bars.Add(Bar(end, basePrice + 2m, basePrice + 6m, basePrice - 2m));
        }

        // Due barre consecutive preservano la stima next-bar del motore.
        bars.Add(Bar(current.AddHours(-1), 199m, 204m, 194m));
        bars.Add(Bar(current, 200m, 205m, 195m));
        return bars.OrderBy(bar => bar.DateTime).ToArray();
    }

    private static OhlcvData Bar(DateTime time, decimal open, decimal high, decimal low) =>
        new()
        {
            DateTime = time,
            Open = open,
            High = high,
            Low = low,
            Close = open + 1m,
            Volume = 1m
        };

    private sealed class TestTfMirrored : TfMirroredEngine
    {
        public int Start { set => StartHour = value; }
        public int End { set => EndHour = value; }
        public int Skip { set => SkipDay = value; }

        public override string Name => "TEST_TF_M";
        public override string Description => "TF_M parity test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 1;
    }

    private sealed class TestTfUnmirrored : TfUnmirroredEngine
    {
        public int LongNo { set => FastNoLong = value; }

        public override string Name => "TEST_TF_U";
        public override string Description => "TF_U parity test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 1;
    }

    private sealed class TestTfUnmirroredDaily : TfUnmirroredEngine
    {
        public override string Name => "TEST_TF_U_DAILY";
        public override string Description => "TF_U daily parity test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 1440;
        public override int RequiredCandles => 1;
    }
}
