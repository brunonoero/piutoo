using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class ReversalBollingerBandEngineTests
{
    [Fact]
    public void RbbU_RearmsLimitOrdersAndDeclaresSessionPolicies()
    {
        var strategy = new TestRbbUnmirrored();
        var bars = BuildBars();
        bars[^3].Close = 100m;
        bars[^2].Close = 110m;
        bars[^1].Close = 111m; // Era già sopra la banda inferiore: non è un nuovo cross.

        var signal = Evaluate(strategy, bars);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Limit, signal.OrderType);
        Assert.Equal(110.25m, signal.Price);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
        Assert.Equal(1, signal.MaxEntriesPerSession);
        Assert.Equal(new DateTime(2024, 1, 7, 17, 0, 0, DateTimeKind.Utc), signal.EntrySessionStartUtc);
        Assert.Equal(new DateTime(2024, 1, 8, 16, 0, 0, DateTimeKind.Utc), signal.CloseAtUtc);
    }

    [Fact]
    public void RbbU_DisabledTimeWindowAndMultidayModeMatchPython()
    {
        var strategy = new TestRbbUnmirrored
        {
            Intraday = false
        };
        var bars = BuildBars();
        bars[^2].Close = 110m;
        bars[^1].Close = 111m;

        var signal = Evaluate(strategy, bars);

        Assert.Equal(TradeOrderType.Limit, signal.OrderType);
        Assert.Null(signal.CloseAtUtc);
    }

    [Fact]
    public void RbbU_UsesExclusivePythonTimeWindowEnd()
    {
        var strategy = new TestRbbUnmirrored
        {
            Start = 1000,
            End = 1100
        };
        var bars = BuildBars(lastTime: new DateTime(2024, 1, 8, 11, 0, 0, DateTimeKind.Utc));
        bars[^2].Close = 110m;
        bars[^1].Close = 111m;

        var signal = Evaluate(strategy, bars);

        Assert.Equal(SignalType.Hold, signal.Type);
    }

    [Fact]
    public void RbbLimit_FillsOnlyOnStrictPenetrationThenHonorsSessionFlatAndFillCap()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m);

        var emittedAt = new DateTime(2024, 1, 8, 10, 0, 0, DateTimeKind.Utc);
        var signal = RbbLimit(emittedAt, emittedAt.AddMinutes(5));
        service.ProcessSignals([signal], Prices(101m), Bars(emittedAt, 101m, 102m, 100m), emittedAt);

        // Il solo contatto al limite non è un fill Python.
        var touchTime = emittedAt.AddMinutes(5);
        service.UpdateMarketPrices(Prices(100m), Bars(touchTime, 101m, 102m, 100m), touchTime);
        Assert.Null(service.GetExecutionSnapshot("TEST_RBB_U", "NQ", touchTime).Position);

        // Alla barra seguente l'ordine viene riarmato e la penetrazione stretta lo riempie.
        var rearmedAt = touchTime;
        var fillAt = rearmedAt.AddMinutes(5);
        service.ProcessSignals([RbbLimit(rearmedAt, fillAt)], Prices(101m), Bars(rearmedAt, 101m, 102m, 100m), rearmedAt);
        service.UpdateMarketPrices(Prices(100m), Bars(fillAt, 101m, 102m, 99m), fillAt);
        Assert.Equal(100m, service.GetExecutionSnapshot("TEST_RBB_U", "NQ", fillAt).Position!.EntryPrice);

        var sessionClose = new DateTime(2024, 1, 8, 16, 0, 0, DateTimeKind.Utc);
        service.UpdateMarketPrices(Prices(100m), Bars(sessionClose, 100m, 101m, 99m), sessionClose);
        Assert.Null(service.GetExecutionSnapshot("TEST_RBB_U", "NQ", sessionClose).Position);

        // Un secondo limit nella stessa sessione non può produrre un altro fill.
        var secondFillAt = sessionClose.AddMinutes(10);
        service.ProcessSignals(
            [RbbLimit(sessionClose.AddMinutes(5), secondFillAt)],
            Prices(101m),
            Bars(sessionClose.AddMinutes(5), 101m, 102m, 100m),
            sessionClose.AddMinutes(5));
        service.UpdateMarketPrices(Prices(100m), Bars(secondFillAt, 101m, 102m, 99m), secondFillAt);
        Assert.Null(service.GetExecutionSnapshot("TEST_RBB_U", "NQ", secondFillAt).Position);
    }

    private static TradeSignal Evaluate(TestRbbUnmirrored strategy, OhlcvData[] bars) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = bars[^1].DateTime
            }
        });

    private static OhlcvData[] BuildBars(DateTime? lastTime = null)
    {
        var end = lastTime ?? new DateTime(2024, 1, 8, 14, 0, 0, DateTimeKind.Utc);
        var bars = new OhlcvData[145];
        for (var index = 0; index < bars.Length; index++)
        {
            var close = 100m;
            bars[index] = new OhlcvData
            {
                DateTime = end.AddHours(index - bars.Length + 1),
                Open = close,
                High = close + 1m,
                Low = close - 1m,
                Close = close,
                Volume = 1m
            };
        }

        return bars;
    }

    private static TradeSignal RbbLimit(DateTime emittedAt, DateTime validFrom) =>
        new()
        {
            Date = emittedAt,
            Type = SignalType.Buy,
            Price = 100m,
            Symbol = "NQ",
            StrategyName = "TEST_RBB_U",
            StrategyCode = "TEST_RBB_U",
            Quantity = 1m,
            OrderType = TradeOrderType.Limit,
            ValidFromUtc = validFrom,
            ExpiresAtUtc = validFrom,
            MaxEntriesPerSession = 1,
            EntrySessionStartUtc = new DateTime(2024, 1, 7, 17, 0, 0, DateTimeKind.Utc),
            CloseAtUtc = new DateTime(2024, 1, 8, 16, 0, 0, DateTimeKind.Utc),
            StopLossMoneyPerFutureContract = 1_000m
        };

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(DateTime time, decimal open, decimal high, decimal low) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData
            {
                DateTime = time,
                Open = open,
                High = high,
                Low = low,
                Close = open
            }
        };

    private sealed class TestRbbUnmirrored : RbbUnmirroredEngine
    {
        public bool Intraday
        {
            set => IntradayOnly = value;
        }

        public int Start
        {
            set => StartTrade = value;
        }

        public int End
        {
            set => EndTrade = value;
        }

        public TestRbbUnmirrored()
        {
            SessionStartTime = 1700;
            SessionEndTime = 1600;
            BollingerLength = 2;
            BollingerNumDevs = 0.5m;
            FastYesLong = 152;
            FastNoLong = 153;
            FastYesShort = 153;
            FastNoShort = 153;
            StopMoney = 1_000;
        }

        public override string Name => "TEST_RBB_U";
        public override string Description => "RBB_U di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;

        public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
            EvaluateCore(data, currentDate);
    }
}
