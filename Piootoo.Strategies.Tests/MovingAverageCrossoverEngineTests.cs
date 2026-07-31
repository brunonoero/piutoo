using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class MovingAverageCrossoverEngineTests
{
    [Fact]
    public void CrossAbove_EmitsMarketLongForNextBar()
    {
        var strategy = new TestMac();
        var bars = Bars(new DateTime(2024, 1, 8, 9, 0, 0, DateTimeKind.Utc), 1m, 1m, 1m, 2m);

        var signal = Evaluate(strategy, bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.False(signal.ExitOnly);
        Assert.Equal("LE_CROSS", signal.Reason);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
    }

    [Fact]
    public void ReverseCross_EmitsExitOnlyOnNextBar()
    {
        var strategy = new TestMac();
        var bars = Bars(new DateTime(2024, 1, 8, 9, 0, 0, DateTimeKind.Utc), 1m, 2m, 2m, 1m);

        var signal = Evaluate(strategy, bars, SignalType.Buy);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.True(signal.ExitOnly);
        Assert.Equal("LX_REVERSE_CROSS", signal.Reason);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
    }

    [Fact]
    public void FridaySessionEnd_EmitsImmediateExitOnly()
    {
        var strategy = new TestMac();
        var bars = Bars(new DateTime(2024, 1, 5, 13, 0, 0, DateTimeKind.Utc), 1m, 1m, 1m, 1m);

        var signal = Evaluate(strategy, bars, SignalType.Sell);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.True(signal.ExitOnly);
        Assert.Equal("SX_FRIDAY_EOD", signal.Reason);
        Assert.Null(signal.ValidFromUtc);
    }

    [Fact]
    public void DeferredExitOnly_ClosesButDoesNotReversePosition()
    {
        var service = new PiootooTradingService();
        service.Initialize(10_000m, 0m);
        var time = new DateTime(2024, 1, 8, 9, 0, 0, DateTimeKind.Utc);
        var entry = new TradeSignal
        {
            Date = time,
            Type = SignalType.Buy,
            Price = 100m,
            Symbol = "CL",
            StrategyCode = "MAC_TEST",
            StrategyName = "MAC_TEST"
        };
        service.ProcessSignals([entry], new Dictionary<string, decimal> { ["CL"] = 100m }, time);

        var exit = new TradeSignal
        {
            Date = time,
            Type = SignalType.Sell,
            Price = 100m,
            Symbol = "CL",
            StrategyCode = "MAC_TEST",
            StrategyName = "MAC_TEST",
            ExitOnly = true,
            ValidFromUtc = time.AddHours(1),
            ExpiresAtUtc = time.AddHours(1)
        };
        service.ProcessSignals([exit], new Dictionary<string, decimal> { ["CL"] = 100m }, time);
        service.UpdateMarketPrices(
            new Dictionary<string, decimal> { ["CL"] = 105m },
            new Dictionary<string, OhlcvData>
            {
                ["CL"] = new() { DateTime = time.AddHours(1), Open = 105m, High = 105m, Low = 105m, Close = 105m }
            },
            time.AddHours(1));

        var snapshot = service.GetSnapshot();
        Assert.Equal(0, snapshot.OpenPositionsCount);
        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.OppositeSignal, trade.ExitReason);
    }

    private static TradeSignal Evaluate(TestMac strategy, OhlcvData[] bars, SignalType? position) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "CL",
                BarTimeUtc = bars[^1].DateTime,
                Position = position.HasValue ? new StrategyPositionSnapshot { Direction = position.Value } : null
            }
        });

    private static OhlcvData[] Bars(DateTime first, params decimal[] closes) =>
        closes.Select((close, index) => new OhlcvData
        {
            DateTime = first.AddHours(index),
            Open = close,
            High = close,
            Low = close,
            Close = close,
            Volume = 1m
        }).ToArray();

    private sealed class TestMac : MovingAverageCrossoverEngine
    {
        public TestMac()
        {
            FastPeriod = 2;
            SlowPeriod = 3;
            GradientPeriod = 0;
            UseDailyFilter = false;
            SessionEndTime = 1700;
        }

        public override string Name => "MAC_TEST";
        public override string Description => "MAC di prova";
        public override string Symbol => "@CL";
        public override int TimeframeMinutes => 60;
    }
}
