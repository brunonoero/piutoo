using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT6EXOStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il motore FBO (falso breakout) della serie PT6EXO. Le serie di prova sono piatte — alto 101, basso
/// 99, chiusura 100 — cosi' il canale vale 101/99 e ogni caso muove solo le ultime barre.
/// </summary>
public sealed class FailedBreakoutEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Il caso classico: la barra buca il massimo e chiude di nuovo sotto. Short a mercato sulla barra dopo.</summary>
    [Fact]
    public void ABreakAndReentryInTheSameBarSellsAtMarketOnTheNextBar()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 105m, low: 99m, close: 100m);

        var signal = Evaluate(new TestFbo(), bars, null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Null(signal.CompanionSignals);
    }

    /// <summary>Lo specchio sui bassi: buca il minimo e chiude di nuovo sopra. Long.</summary>
    [Fact]
    public void ABreakBelowAndReentryBuys()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 101m, low: 95m, close: 100m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestFbo(), bars, null).Type);
    }

    /// <summary>Una rottura che chiude fuori e' un breakout riuscito, per ora: niente.</summary>
    [Fact]
    public void ABreakThatClosesOutsideIsNotASignal()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 105m, low: 100m, close: 103m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFbo(), bars, null).Type);
    }

    /// <summary>
    /// Rientro su piu' barre: la barra prima ha chiuso sopra il livello, questa chiude sotto senza
    /// bucarlo. E' il secondo ramo della condizione di primo rientro.
    /// </summary>
    [Fact]
    public void AReentryAFewBarsLaterSells()
    {
        var bars = Flat(200);
        bars[^2] = Bar(bars[^2].DateTime, high: 105m, low: 100m, close: 103m);
        bars[^1] = Bar(bars[^1].DateTime, high: 100.8m, low: 99.5m, close: 100m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestFbo { Reentry = 3 }, bars, null).Type);
    }

    /// <summary>
    /// Il rientro si segnala una volta: la barra dopo, gia' dentro e senza bucare il livello, non ne
    /// produce un altro. Altrimenti ogni rottura darebbe un segnale per barra.
    /// </summary>
    [Fact]
    public void OnlyTheFirstReentryIsASignal()
    {
        var bars = Flat(200);
        bars[^3] = Bar(bars[^3].DateTime, high: 105m, low: 100m, close: 103m);
        bars[^2] = Bar(bars[^2].DateTime, high: 102m, low: 99.5m, close: 100m);
        bars[^1] = Bar(bars[^1].DateTime, high: 100.5m, low: 99.5m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFbo { Reentry = 3 }, bars, null).Type);
    }

    /// <summary>Una rottura uscita dalla finestra di rientro non conta piu'.</summary>
    [Fact]
    public void ABreakOlderThanTheReentryWindowIsIgnored()
    {
        var bars = Flat(200);
        bars[^3] = Bar(bars[^3].DateTime, high: 105m, low: 100m, close: 103m);
        bars[^2] = Bar(bars[^2].DateTime, high: 104m, low: 102m, close: 103m);
        bars[^1] = Bar(bars[^1].DateTime, high: 103m, low: 99.5m, close: 100m);

        // Con finestra 2 la rottura di tre barre fa entra nel canale: il livello diventa 105 e la
        // chiusura a 100 non e' un rientro da nessuna rottura.
        Assert.Equal(SignalType.Hold, Evaluate(new TestFbo { Reentry = 2 }, bars, null).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 105m, low: 99m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFbo { Side = 1 }, bars, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestFbo { Side = 2 }, bars, null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 105m, low: 99m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFbo(), bars, SignalType.Buy).Type);
    }

    /// <summary>
    /// Lo stop oltre l'estremo: massimo della rottura 105, due tick di margine, chiusura 100 → sette
    /// punti, convertiti nel denaro per contratto del simbolo.
    /// </summary>
    [Fact]
    public void TheStopBeyondTheExtremeIsTheDistanceFromTheClose()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 105m, low: 99m, close: 100m);

        var signal = Evaluate(new TestFbo { ExtremeStop = true, BufferTicks = 2 }, bars, null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(7m * InstrumentRegistry.PointValue("@NQ"), signal.StopLossMoneyPerFutureContract);
    }

    /// <summary>
    /// La profondita' minima in ATR: la serie ha un vero range di 2 punti, quindi con 3 ATR la rottura
    /// deve superare il livello di 6. Quattro punti non bastano, sette si'.
    /// </summary>
    [Fact]
    public void AMinimumBreakInAtrFiltersShallowBreaks()
    {
        var shallow = Flat(500);
        shallow[^1] = Bar(shallow[^1].DateTime, high: 105m, low: 99m, close: 100m);
        var deep = Flat(500);
        deep[^1] = Bar(deep[^1].DateTime, high: 108m, low: 99m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFbo { MinBreak = 3m }, shallow, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestFbo { MinBreak = 3m }, deep, null).Type);
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni della griglia.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_FBO();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 240,
            ["ChannelBars"] = 10, ["ReentryBars"] = 3, ["MinBreakAtr"] = 0.25m,
            ["StopAtExtreme"] = 1, ["ExtremeBufferTicks"] = 5, ["Direction"] = 2,
            ["StopAtr"] = 1.5m, ["TargetAtr"] = 2m, ["MaxBars"] = 6, ["IntradayOnly"] = 1,
            ["ExitHour"] = 21, ["StartHour"] = 3, ["EndHour"] = 18,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.Equal(240, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(10, Read<int>(strategy, "ChannelBars"));
        Assert.Equal(3, Read<int>(strategy, "ReentryBars"));
        Assert.Equal(0.25m, Read<decimal>(strategy, "MinBreakAtr"));
        Assert.Equal(1, Read<int>(strategy, "StopAtExtreme"));
        Assert.Equal(5, Read<int>(strategy, "ExtremeBufferTicks"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
        Assert.Equal(InstrumentRegistry.Get("@FDAX").TickSize, Read<decimal>(strategy, "TickSize"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestFbo strategy, OhlcvData[] bars, SignalType? position) =>
        strategy.Evaluate(new StrategyEvaluationRequest
        {
            Ohlcv = bars,
            BarTimeUtc = bars[^1].DateTime,
            Execution = new StrategyExecutionSnapshot
            {
                StrategyCode = strategy.Name,
                Symbol = "NQ",
                BarTimeUtc = bars[^1].DateTime,
                Position = position.HasValue ? new StrategyPositionSnapshot { Direction = position.Value } : null
            }
        });

    private static OhlcvData[] Flat(int count) =>
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), 101m, 99m, 100m)).ToArray();

    private static OhlcvData Bar(DateTime time, decimal high, decimal low, decimal close) =>
        new() { DateTime = time, Open = 100m, High = high, Low = low, Close = close, Volume = 1m };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>FBO di prova su NQ a 60 minuti, canale di 20 barre. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestFbo : FailedBreakoutEngine
    {
        public TestFbo()
        {
            IntradayOnly = false;
            ChannelBars = 20;
            ReentryBars = 1;
        }

        public int Reentry { set => ReentryBars = value; }
        public int Side { set => Direction = value; }
        public decimal MinBreak { set => MinBreakAtr = value; }
        public bool ExtremeStop { set => StopAtExtreme = value ? 1 : 0; }
        public int BufferTicks { set => ExtremeBufferTicks = value; }

        public override string Name => "FBO_TEST";
        public override string Description => "FBO di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
