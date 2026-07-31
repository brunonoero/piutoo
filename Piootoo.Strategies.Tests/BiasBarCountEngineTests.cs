using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class BiasBarCountEngineTests
{
    [Fact]
    public void Type1_UsesPreviousClosedBarPattern_AndPythonMondayDayFilter()
    {
        var bars = BuildBars(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 9 * 24);
        var strategy = new TestBias(BiasEntryType.MarketOnArmBar, notEntryDayLong: -1);
        var mondayStart = new DateTime(2024, 1, 8, 19, 0, 0, DateTimeKind.Utc);

        // La barra precedente arma il fill market all'open della seconda barra di lunedì.
        var signal = strategy.GenerateSignal(BarsThrough(bars, mondayStart), mondayStart);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(mondayStart.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(new DateTime(2024, 1, 8, 22, 0, 0, DateTimeKind.Utc), signal.CloseAtUtc);

        var blocked = new TestBias(BiasEntryType.MarketOnArmBar, notEntryDayLong: 0);
        Assert.Equal(SignalType.Hold, blocked.GenerateSignal(BarsThrough(bars, mondayStart), mondayStart).Type);
    }

    [Theory]
    [InlineData(BiasEntryType.BreakoutStop, TradeOrderType.Stop, 20)]
    [InlineData(BiasEntryType.RetracementLimit, TradeOrderType.Limit, 5)]
    public void Types2And3_ArmAtTrigger_AndUseOnlyPreviousCompletedBars(
        BiasEntryType entryType, TradeOrderType orderType, decimal expectedLevel)
    {
        var bars = BuildBars(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 9 * 24);
        var sessionStart = new DateTime(2024, 1, 8, 19, 0, 0, DateTimeKind.Utc);
        var strategy = new TestBias(entryType);

        // Prima barra della sessione: mycount = 1.
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(BarsThrough(bars, sessionStart), sessionStart).Type);

        var trigger = sessionStart.AddHours(1);
        var triggerBars = BarsThrough(bars, trigger);
        triggerBars[^3].High = 10m;
        triggerBars[^3].Low = 5m;
        triggerBars[^2].High = 20m;
        triggerBars[^2].Low = 8m;
        triggerBars[^1].High = 9_999m; // Non deve contaminare il rolling level.
        triggerBars[^1].Low = 1m;

        var signal = strategy.GenerateSignal(triggerBars, trigger);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(orderType, signal.OrderType);
        Assert.Equal(expectedLevel, signal.Price);
        Assert.Equal(trigger.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(sessionStart.AddHours(3), signal.CloseAtUtc);
    }

    [Fact]
    public void ArmedType2_CancelsOnSameDirectionPosition_AndDoesNotRearmAfterFlat()
    {
        var bars = BuildBars(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 9 * 24);
        var sessionStart = new DateTime(2024, 1, 8, 19, 0, 0, DateTimeKind.Utc);
        var strategy = new TestBias(BiasEntryType.BreakoutStop);

        strategy.GenerateSignal(BarsThrough(bars, sessionStart), sessionStart);
        strategy.SetPosition(1);
        var trigger = sessionStart.AddHours(1);
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(BarsThrough(bars, trigger), trigger).Type);

        strategy.SetPosition(0);
        var later = trigger.AddHours(1);
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(BarsThrough(bars, later), later).Type);
    }

    private static OhlcvData[] BarsThrough(OhlcvData[] bars, DateTime last) =>
        bars.Where(bar => bar.DateTime <= last).ToArray();

    private static OhlcvData[] BuildBars(DateTime first, int count)
    {
        var bars = new OhlcvData[count];
        for (var index = 0; index < bars.Length; index++)
        {
            bars[index] = new OhlcvData
            {
                DateTime = first.AddHours(index),
                Open = 100m,
                High = 110m,
                Low = 90m,
                Close = 101m,
                Volume = 1m
            };
        }

        return bars;
    }

    private sealed class TestBias : BiasBarCountEngine
    {
        public TestBias(BiasEntryType entryType, int notEntryDayLong = -1)
        {
            SessionStartTime = 1800;
            SessionEndTime = 1700;
            ArmBarLong = 2;
            ArmBarShort = 99;
            ExitBarLong = 4;
            EndLong = 4;
            EntryType = entryType;
            PatternLongYes = 152;
            PatternLongNo = 153;
            PatternShortYes = 153;
            PatternShortNo = 153;
            NotEntryDayLong = notEntryDayLong;
            BreakoutBarsHigh = 2;
            BreakoutBarsLow = 2;
        }

        public override string Name => "TEST_BIAS";
        public override string Description => "Strategia BIAS di prova";
        public override string Symbol => "@GC";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 1;

        public void SetPosition(int position) => _currentMP = position;
    }
}
