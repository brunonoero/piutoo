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
/// Il motore NRX (compressione) della serie PT6EXO, su NQ a 60 minuti con tick di 0,25. Le serie di
/// prova sono piatte — alto 101, basso 99, range 2 —, quindi nessuna barra e' strettamente la piu'
/// stretta: ogni caso stringe una barra sola.
/// </summary>
public sealed class CompressionEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// NR7: la barra appena chiusa ha range 1 contro il 2 delle sei prima. Buy stop al massimo piu' due
    /// tick (100,5 + 0,5 = 101) e sell stop al minimo meno due tick (99,5 − 0,5 = 99), in OCO, validi
    /// sulla barra dopo.
    /// </summary>
    [Fact]
    public void ANarrowestBarArmsAnOcoOfStopsOnItsExtremes()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 100.5m, low: 99.5m);

        var signal = Evaluate(new TestNrx { Offset = 2 }, bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(101m, signal.Price);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        var companion = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, companion.Type);
        Assert.Equal(TradeOrderType.Stop, companion.OrderType);
        Assert.Equal(99m, companion.Price);
    }

    /// <summary>
    /// Non e' una compressione: una barra piu' stretta fra le sei prima (range 0,8), un pareggio con la
    /// piu' stretta, una barra senza range, e lo sfondo piatto dove tutte le barre sono uguali.
    /// </summary>
    [Fact]
    public void ABarThatIsNotStrictlyTheNarrowestIsNotACompression()
    {
        var narrowerBefore = Flat(200);
        narrowerBefore[^4] = Bar(narrowerBefore[^4].DateTime, high: 100.4m, low: 99.6m);
        narrowerBefore[^1] = Bar(narrowerBefore[^1].DateTime, high: 100.5m, low: 99.5m);
        var tie = Flat(200);
        tie[^4] = Bar(tie[^4].DateTime, high: 100.5m, low: 99.5m);
        tie[^1] = Bar(tie[^1].DateTime, high: 100.5m, low: 99.5m);
        var noRange = Flat(200);
        noRange[^1] = Bar(noRange[^1].DateTime, high: 100m, low: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx(), narrowerBefore, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx(), tie, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx(), noRange, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx(), Flat(200), null).Type);
    }

    /// <summary>
    /// La validita': la compressione e' tre barre fa. Con validita' 1 o 2 e' scaduta (l'ordine per la
    /// barra dopo sarebbe il terzo), con 3 gli stop nascono ancora sui suoi estremi.
    /// </summary>
    [Fact]
    public void TheStopsStayValidForTheDeclaredBars()
    {
        var bars = Flat(200);
        bars[^3] = Bar(bars[^3].DateTime, high: 100.5m, low: 99.5m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx { Valid = 1 }, bars, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx { Valid = 2 }, bars, null).Type);

        var signal = Evaluate(new TestNrx { Valid = 3 }, bars, null);
        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(100.5m, signal.Price);
        Assert.Equal(99.5m, Assert.Single(signal.CompanionSignals!).Price);
    }

    /// <summary>
    /// Due compressioni nella finestra di validita': vale la piu' recente. La barra appena chiusa (range
    /// 0,5) e' piu' stretta anche di quella di tre barre fa (range 1).
    /// </summary>
    [Fact]
    public void TheMostRecentCompressionWins()
    {
        var bars = Flat(200);
        bars[^3] = Bar(bars[^3].DateTime, high: 100.5m, low: 99.5m);
        bars[^1] = Bar(bars[^1].DateTime, high: 100.25m, low: 99.75m);

        var signal = Evaluate(new TestNrx { Valid = 3 }, bars, null);

        Assert.Equal(100.25m, signal.Price);
        Assert.Equal(99.75m, Assert.Single(signal.CompanionSignals!).Price);
    }

    /// <summary>
    /// Inside bar: massimo e minimo dentro quelli della barra prima. Una barra che esce sopra il massimo
    /// precedente non lo e'.
    /// </summary>
    [Fact]
    public void AnInsideBarIsACompressionAndAnOutsideBarIsNot()
    {
        var inside = Flat(200);
        inside[^1] = Bar(inside[^1].DateTime, high: 100.75m, low: 99.5m);
        var outside = Flat(200);
        outside[^1] = Bar(outside[^1].DateTime, high: 101.5m, low: 99.5m);

        var signal = Evaluate(new TestNrx { Kind = 1 }, inside, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(100.75m, signal.Price);
        Assert.Equal(99.5m, Assert.Single(signal.CompanionSignals!).Price);
        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx { Kind = 1 }, outside, null).Type);
    }

    /// <summary>Con un lato dichiarato nasce solo quella gamba, senza compagna.</summary>
    [Fact]
    public void ADeclaredDirectionKeepsOneLeg()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 100.5m, low: 99.5m);

        var longOnly = Evaluate(new TestNrx { Side = 1 }, bars, null);
        var shortOnly = Evaluate(new TestNrx { Side = 2 }, bars, null);

        Assert.Equal(SignalType.Buy, longOnly.Type);
        Assert.Null(longOnly.CompanionSignals);
        Assert.Equal(SignalType.Sell, shortOnly.Type);
        Assert.Equal(99.5m, shortOnly.Price);
        Assert.Null(shortOnly.CompanionSignals);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 100.5m, low: 99.5m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx(), bars, SignalType.Buy).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestNrx(), bars, SignalType.Sell).Type);
    }

    /// <summary>Una finestra di confronto di una barra, o una validita' nulla, sono errori di configurazione.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestNrx { Lookback = 1 }.GenerateSignal(Flat(200), Start));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestNrx { Valid = 0 }.GenerateSignal(Flat(200), Start));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestNrx { Kind = 2 }.GenerateSignal(Flat(200), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e prende il tick dal registro strumenti.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_NRX();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 240,
            ["CompressionKind"] = 1, ["LookbackBars"] = 4, ["ValidBars"] = 3,
            ["OffsetTicks"] = 5, ["Direction"] = 2,
            ["StopAtr"] = 1.5m, ["TargetAtr"] = 2m, ["MaxBars"] = 6, ["IntradayOnly"] = 1,
            ["ExitHour"] = 21, ["StartHour"] = 3, ["EndHour"] = 18,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.Equal(240, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(1, Read<int>(strategy, "CompressionKind"));
        Assert.Equal(4, Read<int>(strategy, "LookbackBars"));
        Assert.Equal(3, Read<int>(strategy, "ValidBars"));
        Assert.Equal(5, Read<int>(strategy, "OffsetTicks"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
        Assert.Equal(1, Read<int>(strategy, "MaxEntriesPerSession"));
        Assert.Equal(InstrumentRegistry.Get("@FDAX").TickSize, Read<decimal>(strategy, "TickSize"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestNrx strategy, OhlcvData[] bars, SignalType? position) =>
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
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), 101m, 99m)).ToArray();

    private static OhlcvData Bar(DateTime time, decimal high, decimal low) =>
        new() { DateTime = time, Open = 100m, High = high, Low = low, Close = 100m, Volume = 1m };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>NRX di prova su NQ a 60 minuti: NR7, validita' 1, tick 0,25. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestNrx : CompressionEngine
    {
        public TestNrx()
        {
            IntradayOnly = false;
            CompressionKind = 0;
            LookbackBars = 7;
            ValidBars = 1;
            TickSize = 0.25m;
        }

        public int Kind { set => CompressionKind = value; }
        public int Lookback { set => LookbackBars = value; }
        public int Valid { set => ValidBars = value; }
        public int Offset { set => OffsetTicks = value; }
        public int Side { set => Direction = value; }

        public override string Name => "NRX_TEST";
        public override string Description => "NRX di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
