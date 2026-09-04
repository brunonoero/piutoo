using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class SessionBreakoutEngineTests
{
    [Fact]
    public void PythonBo_UsesClosedSessions_OffsetsLevels_AndExcludesCurrentBarFromSess0()
    {
        var bars = BuildBars();
        SetSessionRange(bars, new DateTime(2024, 1, 18, 17, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 19, 16, 0, 0, DateTimeKind.Utc), 140m, 80m);
        bars[^1].High = 9_999m;
        bars[^1].Low = 1m;
        var strategy = new Sess0OffsetBo();

        var signal = Evaluate(strategy, bars);

        Assert.True(signal.Type == SignalType.Buy, signal.Reason);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(140.5m, signal.Price);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Equal(1, signal.MaxEntriesPerSession);
        Assert.Equal(new DateTime(2024, 1, 19, 17, 0, 0, DateTimeKind.Utc), signal.EntrySessionStartUtc);
        Assert.Equal(new DateTime(2024, 1, 20, 16, 0, 0, DateTimeKind.Utc), signal.CloseAtUtc);

        var shortSignal = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, shortSignal.Type);
        Assert.Equal(79.5m, shortSignal.Price);
    }

    [Fact]
    public void PythonBo_AppliesHourWindowAndPandasDayFilter()
    {
        var bars = BuildBars(); // ultima barra: sabato 20 gennaio, ore 12 UTC.

        Assert.Equal(SignalType.Hold, Evaluate(new WindowBlockedBo(), bars).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new DayBlockedBo(), bars).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new WindowBo(), bars).Type);
    }

    /// <summary>
    /// <c>level_source = 1</c> di <c>breakout.py</c>: il livello e' il running massimo/minimo della
    /// sola sessione corrente, <b>inclusa la barra in valutazione</b>, e ignora sia <c>n_sess</c>
    /// sia <c>lev_include_sess0</c>.
    ///
    /// <para>Il test misura proprio la differenza che i parametri esistenti non sanno esprimere:
    /// con <c>level_source = 0</c> la barra in corso non contribuisce mai (il sorgente usa
    /// <c>cummax().shift(1)</c>), quindi il suo estremo non puo' diventare il livello. Era la
    /// traduzione sbagliata di <c>PTS_KC_SBO_001_240</c>.</para>
    /// </summary>
    [Fact]
    public void PythonBo_LevelSource1_UsesRunningSessionExtremeIncludingCurrentBar()
    {
        var conCorrente = Evaluate(new RunningLevelBo(), BarsWithBreakoutOnLastBar());
        var senzaCorrente = Evaluate(new Sess0Bo(), BarsWithBreakoutOnLastBar());

        Assert.True(conCorrente.Type == SignalType.Buy, conCorrente.Reason);
        Assert.True(senzaCorrente.Type == SignalType.Buy, senzaCorrente.Reason);

        // level_source = 1: l'estremo della barra in valutazione E' il livello.
        Assert.Equal(200m, conCorrente.Price);
        Assert.Equal(50m, Assert.Single(conCorrente.CompanionSignals!).Price);

        // level_source = 0: la barra in valutazione non contribuisce, resta l'estremo precedente.
        Assert.Equal(110m, senzaCorrente.Price);
        Assert.Equal(90m, Assert.Single(senzaCorrente.CompanionSignals!).Price);
    }

    private static OhlcvData[] BarsWithBreakoutOnLastBar()
    {
        var bars = BuildBars();
        bars[^1].High = 200m;
        bars[^1].Low = 50m;
        return bars;
    }

    [Fact]
    public void PythonBo_DailyDoesNotAttachAnIntradayClose()
    {
        var bars = BuildBars();
        var strategy = new DailyBo();

        var signal = Evaluate(strategy, bars);

        Assert.True(signal.Type == SignalType.Buy, signal.Reason);
        Assert.Null(signal.CloseAtUtc);
    }

    private static TradeSignal Evaluate(TestBo strategy, OhlcvData[] bars) =>
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
        var first = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var bars = new OhlcvData[469];
        for (var index = 0; index < bars.Length; index++)
        {
            bars[index] = new OhlcvData
            {
                DateTime = first.AddHours(index),
                Open = 100m,
                High = 110m,
                Low = 90m,
                Close = 100m,
                Volume = 1m
            };
        }

        return bars;
    }

    private static void SetSessionRange(OhlcvData[] bars, DateTime from, DateTime to, decimal high, decimal low)
    {
        foreach (var bar in bars)
        {
            if (bar.DateTime >= from && bar.DateTime <= to)
            {
                bar.High = high;
                bar.Low = low;
            }
        }
    }

    private class TestBo : SessionBreakoutEngine
    {
        protected int Timeframe = 60;

        public TestBo()
        {
            SessionStartTime = 1700;
            SessionEndTime = 1600;
            Sessions = 1;
            TickSize = 0.25m;
            IntradayOnly = true;
            AdxLength = 0;
        }

        public override string Name => "TEST_BO_NQ";
        public override string Description => "Strategia di prova BO Python";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => Timeframe;
    }

    private sealed class Sess0OffsetBo : TestBo
    {
        public Sess0OffsetBo()
        {
            IncludeCurrentSession = true;
            BreakoutOffsetTicks = 2;
        }
    }

    private sealed class WindowBlockedBo : TestBo
    {
        public WindowBlockedBo() => StartTime = EndTime = 1300;
    }

    private sealed class DayBlockedBo : TestBo
    {
        public DayBlockedBo() => SkipDay = 5;
    }

    private sealed class WindowBo : TestBo
    {
        public WindowBo() => StartTime = EndTime = 1200;
    }

    private sealed class DailyBo : TestBo
    {
        public DailyBo() => Timeframe = 1440;
    }

    /// <summary>level_source = 1: running della sessione corrente, barra in corso inclusa.</summary>
    private sealed class RunningLevelBo : TestBo
    {
        public RunningLevelBo()
        {
            LevelSource = 1;
            // Dichiarati apposta con valori che il ramo level_source = 1 deve IGNORARE.
            Sessions = 5;
            IncludeCurrentSession = false;
            BreakoutOffsetTicks = 0;
        }
    }

    /// <summary>level_source = 0 con sessione corrente inclusa: la barra in corso resta fuori.</summary>
    private sealed class Sess0Bo : TestBo
    {
        public Sess0Bo()
        {
            IncludeCurrentSession = true;
            BreakoutOffsetTicks = 0;
        }
    }
}
