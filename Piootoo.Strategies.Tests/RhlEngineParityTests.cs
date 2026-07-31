using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;
using Xunit;

namespace Piootoo.Strategies.Tests;

public sealed class RhlEngineParityTests
{
    [Fact]
    public void Rhl_UsesPythonOffsetsMirroredPatternsAndExitSpec()
    {
        var strategy = new TestRhl
        {
            Tick = 0.25m,
            LongOffset = 4,
            ShortOffset = 6,
            Stop = 1_200,
            Profit = 2_400,
            MaximumBars = 5
        };
        var bars = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc));
        var previousSessionStart = bars[^1].DateTime.Date.AddDays(-2).AddHours(17);
        var expectedLow = SessionLow(bars, previousSessionStart);
        var expectedHigh = SessionHigh(bars, previousSessionStart);

        var signal = strategy.GenerateSignal(bars, bars[^1].DateTime);
        var shortSignal = Assert.Single(signal.CompanionSignals!);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Limit, signal.OrderType);
        Assert.Equal(expectedLow - 1m, signal.Price);
        Assert.Equal(expectedHigh + 1.5m, shortSignal.Price);
        Assert.Equal(SignalType.Sell, shortSignal.Type);
        Assert.Equal(1_200m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(2_400m, signal.TakeProfitMoneyPerFutureContract);
        Assert.Equal(5, signal.MaxBarsInPosition);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
    }

    [Fact]
    public void Rhl_UsesPythonHourAndMondayBasedSkipDayConventions()
    {
        var strategy = new TestRhl { DirectionValue = 1, Start = 10, End = 10 };
        var inWindow = BuildSessions(new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc)); // Monday
        var outsideWindow = BuildSessions(new DateTime(2024, 1, 8, 11, 0, 0, DateTimeKind.Utc));

        Assert.Equal(SignalType.Buy, strategy.GenerateSignal(inWindow, inWindow[^1].DateTime).Type);
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(outsideWindow, outsideWindow[^1].DateTime).Type);

        strategy.Skip = 4; // Python: Friday (Monday = 0)
        var friday = BuildSessions(new DateTime(2024, 1, 12, 10, 0, 0, DateTimeKind.Utc));
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(friday, friday[^1].DateTime).Type);
    }

    [Fact]
    public void RhlLimit_FillsOnlyAfterStrictPenetration_AndChecksSameBarStop()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);
        var signalTime = new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc);
        var fillTime = signalTime.AddMinutes(5);
        var signal = LimitBuy(100m, signalTime, fillTime);

        service.ProcessSignals([signal], Prices(101m), Bars(signalTime, 101m, 102m, 100m, 101m), signalTime);
        service.UpdateMarketPrices(Prices(100m), Bars(fillTime, 100m, 101m, 100m, 100m), fillTime);
        Assert.Null(service.GetExecutionSnapshot("RHL-test", "NQ", fillTime).Position);

        var opened = false;
        service.PositionOpened += _ => opened = true;
        var penetrating = LimitBuy(100m, signalTime, fillTime);
        service.ProcessSignals([penetrating], Prices(101m), Bars(signalTime, 101m, 102m, 100m, 101m), signalTime);
        service.UpdateMarketPrices(Prices(99m), Bars(fillTime, 100m, 101m, 99m, 99m), fillTime);

        Assert.True(opened);
        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.StopLoss, trade.ExitReason);
        Assert.Equal(99m, trade.ExitPrice);
    }

    private static TradeSignal LimitBuy(decimal level, DateTime signalTime, DateTime validFrom) =>
        new()
        {
            Date = signalTime,
            Type = SignalType.Buy,
            Price = level,
            Symbol = "@NQ",
            StrategyName = "RHL-test",
            StrategyCode = "RHL-test",
            Quantity = 1m,
            OrderType = TradeOrderType.Limit,
            ValidFromUtc = validFrom,
            ExpiresAtUtc = validFrom,
            StopLoss = 1m
        };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(
        DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData
            {
                DateTime = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = 1
            }
        };

    private static OhlcvData[] BuildSessions(DateTime current)
    {
        var bars = new List<OhlcvData>();
        for (var day = -7; day <= -1; day++)
        {
            var sessionDate = current.Date.AddDays(day);
            var basePrice = 100m + (day + 7) * 10m;
            bars.Add(Bar(sessionDate.AddHours(17).AddMinutes(5), basePrice, basePrice + 8m, basePrice - 4m));
            bars.Add(Bar(sessionDate.AddDays(1).AddHours(16), basePrice + 2m, basePrice + 6m, basePrice - 2m));
        }

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
            Volume = 1
        };

    private static decimal SessionLow(IEnumerable<OhlcvData> bars, DateTime sessionStart) =>
        bars.Where(bar => bar.DateTime >= sessionStart && bar.DateTime < sessionStart.AddDays(1)).Min(bar => bar.Low);

    private static decimal SessionHigh(IEnumerable<OhlcvData> bars, DateTime sessionStart) =>
        bars.Where(bar => bar.DateTime >= sessionStart && bar.DateTime < sessionStart.AddDays(1)).Max(bar => bar.High);

    private sealed class TestRhl : RhlEngine
    {
        public decimal Tick { set => TickSize = value; }
        public int LongOffset { set => LongLevelOffsetTicks = value; }
        public int ShortOffset { set => ShortLevelOffsetTicks = value; }
        public int DirectionValue { set => Direction = value; }
        public int Start { set => StartHour = value; }
        public int End { set => EndHour = value; }
        public int Skip { set => SkipDay = value; }
        public int Stop { set => StopMoney = value; }
        public int Profit { set => ProfitMoney = value; }
        public int MaximumBars { set => MaxBars = value; }

        public override string Name => "RHL-test";
        public override string Description => "RHL parity test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 1;
    }
}
