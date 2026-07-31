using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class BiasWeeklyEngineParityTests
{
    [Fact]
    public void MondayZero_EntersAtTheScheduledBarOpen_AndCarriesFridayExit()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = 1000,
            LongExitDay = 4,
            LongExitTime = 1500
        };
        var entryTime = Utc(2024, 1, 8, 10, 0); // Monday

        var signal = strategy.GenerateSignal(Bars(entryTime, 101m), entryTime);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(101m, signal.Price);
        Assert.Equal(entryTime, signal.ValidFromUtc);
        Assert.Equal(entryTime, signal.ExpiresAtUtc);
        Assert.Equal(Utc(2024, 1, 12, 15, 0), signal.CloseAtUtc);
    }

    [Fact]
    public void EntryTime_IsAnExactSchedule_NotAnOperatingWindow()
    {
        var strategy = new TestBiasWeekly { LongEntryDay = 0, LongEntryTime = 1000 };
        var atScheduledTime = Utc(2024, 1, 8, 10, 0);
        var oneMinuteLater = atScheduledTime.AddMinutes(1);

        Assert.Equal(SignalType.Buy, strategy.GenerateSignal(Bars(atScheduledTime, 101m), atScheduledTime).Type);
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(Bars(oneMinuteLater, 101m), oneMinuteLater).Type);
    }

    [Fact]
    public void FastGates_RequireYesAndRejectNo()
    {
        var strategy = new TestBiasWeekly { LongEntryDay = 0, LongEntryTime = 1000 };
        var entryTime = Utc(2024, 1, 8, 10, 0);

        strategy.LongFastYes = 153; // Sentinel Fast: always false.
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(Bars(entryTime, 101m), entryTime).Type);

        strategy.LongFastYes = 152; // Sentinel Fast: always true.
        strategy.LongFastNo = 152;
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(Bars(entryTime, 101m), entryTime).Type);

        strategy.LongFastNo = 153; // Sentinel Fast: always false.
        Assert.Equal(SignalType.Buy, strategy.GenerateSignal(Bars(entryTime, 101m), entryTime).Type);
    }

    [Fact]
    public void FastGate_IsEvaluatedOnTheBarBeforeTheScheduledEntry()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = 1000,
            LongFastYes = 142
        };
        var entryTime = Utc(2024, 1, 8, 10, 0);

        // Il pattern 142 e' vero sulla barra precedente (100 > 99), ma sarebbe falso
        // sulla barra di ingresso (1 non e' > 99): equivale allo shift(1) Python.
        var signal = strategy.GenerateSignal(Bars(entryTime, 100m, currentClose: 1m), entryTime);

        Assert.Equal(SignalType.Buy, signal.Type);
    }

    [Fact]
    public void FridayToMonday_ResolvesExitInTheFollowingWeek()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 4,
            LongEntryTime = 1500,
            LongExitDay = 0,
            LongExitTime = 1000
        };
        var entryTime = Utc(2024, 1, 12, 15, 0); // Friday

        var signal = strategy.GenerateSignal(Bars(entryTime, 101m), entryTime);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2024, 1, 15, 10, 0), signal.CloseAtUtc);
    }

    private static OhlcvData[] Bars(DateTime current, decimal currentOpen, decimal? currentClose = null) =>
    [
        Bar(current.AddHours(-1), 100m),
        Bar(current, currentOpen, currentClose)
    ];

    private static OhlcvData Bar(DateTime time, decimal open, decimal? close = null) =>
        new()
        {
            DateTime = time,
            Open = open,
            High = open + 1m,
            Low = open - 1m,
            Close = close ?? open,
            Volume = 1m
        };

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private sealed class TestBiasWeekly : BiasWeeklyEngine
    {
        public int LongEntryDay { set => EntryDayLong = value; }
        public int LongEntryTime { set => EntryTimeLong = value; }
        public int LongExitDay { set => ExitDayLong = value; }
        public int LongExitTime { set => ExitTimeLong = value; }
        public int LongFastYes { set => FastYesLong = value; }
        public int LongFastNo { set => FastNoLong = value; }

        public override string Name => "BIASW-test";
        public override string Description => "BIASW parity test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 2;
    }
}
