using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class LevelFaderEngineParityTests
{
    [Fact]
    public void LevelFader_EmitsNextBarRecrossWithPythonLevelsAndDollarExits()
    {
        var strategy = new TestLevelFader
        {
            Tick = 0.5m,
            Shift = 2m,
            Stop = 1_200,
            Profit = 2_400,
            MaximumBars = 5,
            CloseTime = 1700
        };
        var bars = BuildBars(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc), 95m, 100m);

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddMinutes(15), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
        Assert.Equal(1_200m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(2_400m, signal.TakeProfitMoneyPerFutureContract);
        Assert.Equal(5, signal.MaxBarsInPosition);
        Assert.Equal(new DateTime(2024, 1, 8, 17, 0, 0, DateTimeKind.Utc), signal.CloseAtUtc);
    }

    [Fact]
    public void LevelFader_UsesPivotOrExtremesAndPythonCalendarFilters()
    {
        var current = new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc); // Monday
        var bars = BuildBars(current, 95m, 98m);
        var pivot = new TestLevelFader { Shift = 2m, Tick = 0.5m, Start = 10, End = 10 };

        Assert.Equal(SignalType.Buy, pivot.GenerateSignal(bars, current).Type);

        var extremes = new TestLevelFader
        {
            Choice = LevelFaderLevel.PreviousSessionExtremes,
            Shift = 2m,
            Tick = 0.5m,
            Start = 10,
            End = 10
        };
        Assert.Equal(SignalType.Hold, extremes.GenerateSignal(bars, current).Type);

        var outsideHour = new TestLevelFader { Start = 11, End = 11 };
        Assert.Equal(SignalType.Hold, outsideHour.GenerateSignal(bars, current).Type);

        var excludedMonday = new TestLevelFader { NotLongDay = 0 };
        Assert.Equal(SignalType.Hold, excludedMonday.GenerateSignal(bars, current).Type);
    }

    private static OhlcvData[] BuildBars(DateTime current, decimal previousClose, decimal currentClose)
    {
        var bars = new List<OhlcvData>();
        for (var day = -7; day <= -2; day++)
        {
            var evening = current.Date.AddDays(day).AddHours(18).AddMinutes(1);
            bars.Add(Bar(evening, 110m, 115m, 105m, 110m));
            bars.Add(Bar(evening.AddDays(1).Date.AddHours(16), 110m, 115m, 105m, 110m));
        }

        // d1: H=130, L=100, C=110. Con tick 0,5 e shift 2:
        // pivot S1 = 96,666..., quindi 95 -> 98 è un recross; L_d1 - 1 = 99 non lo è.
        var previousSession = current.Date.AddDays(-2).AddHours(18).AddMinutes(1);
        bars.Add(Bar(previousSession, 110m, 130m, 100m, 110m));
        bars.Add(Bar(previousSession.AddDays(1).Date.AddHours(16), 110m, 130m, 100m, 110m));

        var sessionStart = current.Date.AddDays(-1).AddHours(18).AddMinutes(1);
        bars.Add(Bar(sessionStart, 110m, 112m, 108m, 110m));
        bars.Add(Bar(current.AddMinutes(-15), previousClose, previousClose, previousClose, previousClose));
        bars.Add(Bar(current, currentClose, currentClose, currentClose, currentClose));
        return bars.OrderBy(bar => bar.DateTime).ToArray();
    }

    private static OhlcvData Bar(DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new()
        {
            DateTime = time,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1m
        };

    private sealed class TestLevelFader : LevelFaderEngine
    {
        public decimal Tick { set => TickSize = value; }
        public decimal Shift { set => LevelShift = value; }
        public LevelFaderLevel Choice { set => LevelChoice = value; }
        public int Start { set => StartTrade = value; }
        public int End { set => EndTrade = value; }
        public int NotLongDay { set => NotEntryDayLong = value; }
        public int Stop { set => StopMoney = value; }
        public int Profit { set => ProfitMoney = value; }
        public int MaximumBars { set => MaxBars = value; }
        public int CloseTime { set => CloseAtTime = value; }

        public override string Name => "LF-test";
        public override string Description => "LF parity test";
        public override string Symbol => "@GC";
        public override int TimeframeMinutes => 15;
        public override int RequiredCandles => 1;
    }
}
