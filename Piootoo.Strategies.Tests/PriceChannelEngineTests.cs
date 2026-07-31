using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class PriceChannelEngineTests
{
    [Fact]
    public void Engine_EmitsCurrentClosedBarChannelStops_WithOffsetsAndExecutionSettings()
    {
        var strategy = new TestPriceChannel();
        var bars = BuildBars();
        var last = bars[^1];

        var signal = strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = last.DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = last.DateTime
            }
        });

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(1000.75m, signal.Price); // max incl. barra chiusa (999) + 0,50 + 1,25.
        Assert.Equal(last.DateTime.AddMinutes(15), signal.ValidFromUtc);
        Assert.Equal(signal.ValidFromUtc, signal.ExpiresAtUtc);
        Assert.Equal(2, signal.MaxEntriesPerSession);
        Assert.Equal(new DateTime(2024, 1, 7, 17, 0, 0, DateTimeKind.Utc), signal.EntrySessionStartUtc);
        Assert.Equal(250m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(500m, signal.TakeProfitMoneyPerFutureContract);
        Assert.Equal(750m, signal.BreakEvenMoneyPerFutureContract);
        Assert.Equal(1_000m, signal.TrailingStopMoneyPerFutureContract);

        var shortSignal = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, shortSignal.Type);
        Assert.Equal(-1000.75m, shortSignal.Price); // min incl. barra chiusa (-999) - 0,50 - 1,25.
        Assert.Equal(signal.EntrySessionStartUtc, shortSignal.EntrySessionStartUtc);
    }

    [Fact]
    public void PythonParity_UsesPriorClosedSessionAtrInDollars_AndSetsIntradayExit()
    {
        var bars = BuildHourlyBars(new DateTime(2024, 1, 18, 12, 0, 0, DateTimeKind.Utc));
        var strategy = new PythonPriceChannel { DvolMinValue = 200m };

        var signal = Evaluate(strategy, bars);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(new DateTime(2024, 1, 18, 16, 0, 0, DateTimeKind.Utc), signal.CloseAtUtc);

        // L'ATR daily è 10 punti × $20 NQ = $200; il range della sessione d0 non deve alterarlo.
        bars[^1].High = 9_999m;
        bars[^1].Low = 1m;
        Assert.Equal(SignalType.Buy, Evaluate(strategy, bars).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new PythonPriceChannel { DvolMinValue = 201m }, bars).Type);
    }

    [Fact]
    public void PythonParity_UsesMondayZeroSkipDay_AndDirectionSelection()
    {
        var bars = BuildHourlyBars(new DateTime(2024, 1, 22, 12, 0, 0, DateTimeKind.Utc)); // lunedì

        Assert.Equal(SignalType.Hold, Evaluate(new PythonPriceChannel { SkipDayValue = 0 }, bars).Type);

        var longOnly = Evaluate(new PythonPriceChannel { DirectionValue = 1 }, bars);
        Assert.Equal(SignalType.Buy, longOnly.Type);
        Assert.Null(longOnly.CompanionSignals);

        var shortOnly = Evaluate(new PythonPriceChannel { DirectionValue = 2 }, bars);
        Assert.Equal(SignalType.Sell, shortOnly.Type);
        Assert.Null(shortOnly.CompanionSignals);
    }

    private static TradeSignal Evaluate(PriceChannelEngine strategy, OhlcvData[] bars) =>
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

    private static OhlcvData[] BuildBars()
    {
        var bars = new OhlcvData[577];
        var lastTime = new DateTime(2024, 1, 8, 13, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < bars.Length; index++)
        {
            bars[index] = new OhlcvData
            {
                DateTime = lastTime.AddMinutes((index - bars.Length + 1) * 15),
                Open = 95m,
                High = 100m,
                Low = 90m,
                Close = 95m,
                Volume = 1m
            };
        }

        bars[^4].High = 110m;
        bars[^4].Low = 40m;
        bars[^3].High = 120m;
        bars[^3].Low = 55m;
        bars[^2].High = 105m;
        bars[^2].Low = 50m;
        bars[^1].High = 999m; // Entra nel canale alla close, come highest(high, N) EL/Python.
        bars[^1].Low = -999m;
        return bars;
    }

    private static OhlcvData[] BuildHourlyBars(DateTime lastTime)
    {
        var bars = new OhlcvData[400];
        for (var index = 0; index < bars.Length; index++)
        {
            bars[index] = new OhlcvData
            {
                DateTime = lastTime.AddHours(index - bars.Length + 1),
                Open = 100m,
                High = 110m,
                Low = 100m,
                Close = 105m,
                Volume = 1m
            };
        }

        return bars;
    }

    private sealed class TestPriceChannel : PriceChannelEngine
    {
        public TestPriceChannel()
        {
            SessionStartTime = 1700;
            SessionEndTime = 1600;
            ChannelBars = 3;
            TickSize = 0.25m;
            OffsetTicks = 2;
            OffsetPoints = 1.25m;
            StopMoney = 250;
            ProfitMoney = 500;
            BreakEvenMoney = 750;
            TrailingStopMoney = 1_000;
            MaxEntriesPerSession = 2;
            UseLegacyVariant = true;
        }

        public override string Name => "TEST_PC_NQ_15";
        public override string Description => "Strategia di prova Price Channel";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 15;
    }

    private sealed class PythonPriceChannel : PriceChannelEngine
    {
        public PythonPriceChannel()
        {
            SessionStartTime = 1700;
            SessionEndTime = 1600;
            ChannelBars = 3;
            TickSize = 0.25m;
            IntradayOnly = true;
        }

        public decimal DvolMinValue { set => DvolMin = value; }
        public int SkipDayValue { set => SkipDay = value; }
        public int DirectionValue { set => Direction = value; }

        public override string Name => "TEST_PYTHON_PC_NQ_60";
        public override string Description => "Strategia di prova Price Channel Python";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
