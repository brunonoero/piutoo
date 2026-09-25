using System.Reflection;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT6EXOStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il motore REG (regime di volatilita') della serie PT6EXO, su NQ a 60 minuti con parametri corti:
/// ATR a 3 barre, regime su 50, canale e bande su 10, percentili 20/80. Le serie di prova hanno
/// chiusure costanti a 100: il true range e' l'ampiezza della barra, e ogni caso cambia l'ampiezza
/// delle ultime barre per spostare l'ATR corrente in fondo o in cima alla finestra di regime.
/// </summary>
public sealed class VolatilityRegimeEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Regime calmo. Sfondo largo 4, ultime tre barre larghe 1: l'ATR corrente (1) e' il minimo unico
    /// fra i 50, rango medio 0,5/50 = 1° percentile, sotto 20. Breakout: buy stop al massimo delle
    /// ultime 10 barre (102, dallo sfondo) e sell stop al minimo (98), in OCO sulla barra dopo.
    /// </summary>
    [Fact]
    public void ACalmRegimeArmsTheChannelBreakout()
    {
        var bars = Calm();

        var signal = Evaluate(new TestReg(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(102m, signal.Price);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        var companion = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, companion.Type);
        Assert.Equal(TradeOrderType.Stop, companion.OrderType);
        Assert.Equal(98m, companion.Price);
    }

    /// <summary>
    /// Regime agitato, short. Sfondo largo 1, ultima barra 100–110 che chiude a 109: true range 10, ATR
    /// (1 + 1 + 10) / 3 = 4, il massimo unico, 99° percentile. Bande su 10 chiusure (nove a 100, una a
    /// 109): media 100,9, deviazione 2,7, banda superiore 106,3. La chiusura e' sopra: short a mercato.
    /// </summary>
    [Fact]
    public void AnAgitatedRegimeSellsACloseAboveTheUpperBand()
    {
        var signal = Evaluate(new TestReg(), Agitated(high: 110m, low: 100m, close: 109m), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Null(signal.CompanionSignals);
    }

    /// <summary>Lo specchio: barra 90–100 che chiude a 91, sotto la banda inferiore a 93,7. Long a mercato.</summary>
    [Fact]
    public void AnAgitatedRegimeBuysACloseBelowTheLowerBand()
    {
        var signal = Evaluate(new TestReg(), Agitated(high: 100m, low: 90m, close: 91m), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
    }

    /// <summary>
    /// Niente fuori dai due rami: volatilita' costante (tutti gli ATR uguali, 50° percentile); regime
    /// agitato con la chiusura dentro le bande (barra 90–110 che chiude a 100, deviazione nulla); regime
    /// calmo con il ramo spento (percentile basso a 0).
    /// </summary>
    [Fact]
    public void NothingHappensOutsideTheTwoRegimes()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestReg(), Flat(200, 100.5m, 99.5m), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestReg(), Agitated(high: 110m, low: 90m, close: 100m), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestReg { Low = 0m }, Calm(), null).Type);
    }

    /// <summary>
    /// Con un lato dichiarato: nel regime calmo nasce solo quella gamba, senza compagna; nel regime
    /// agitato lo short non nasce se si tiene solo il long.
    /// </summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var longOnly = Evaluate(new TestReg { Side = 1 }, Calm(), null);

        Assert.Equal(SignalType.Buy, longOnly.Type);
        Assert.Null(longOnly.CompanionSignals);
        Assert.Equal(SignalType.Hold,
            Evaluate(new TestReg { Side = 1 }, Agitated(high: 110m, low: 100m, close: 109m), null).Type);
    }

    /// <summary>In posizione non nasce nulla, in nessuno dei due regimi.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestReg(), Calm(), SignalType.Buy).Type);
        Assert.Equal(SignalType.Hold,
            Evaluate(new TestReg(), Agitated(high: 110m, low: 100m, close: 109m), SignalType.Sell).Type);
    }

    /// <summary>
    /// La finestra di regime e' il <c>RequiredCandles</c>: con 500 barre di regime 200 barre di storia non
    /// bastano, e il motore aspetta invece di misurare il percentile su una finestra corta.
    /// </summary>
    [Fact]
    public void AShortHistoryWaitsForTheWholeRegimeWindow()
    {
        var strategy = new TestReg { Regime = 500 };

        Assert.Equal(SignalType.Hold, Evaluate(strategy, Calm(), null).Type);
        Assert.True(strategy.RequiredCandles >= 500 + 3 + 1);
    }

    /// <summary>Percentili invertiti o oltre 100 sono errori di configurazione.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestReg { Low = 80m, High = 20m }.GenerateSignal(Calm(), Start));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestReg { High = 120m }.GenerateSignal(Calm(), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e la finestra di regime fa la storia minima.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_REG();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60,
            ["AtrBars"] = 20, ["RegimeBars"] = 250, ["LowPercentile"] = 10, ["HighPercentile"] = 90,
            ["ChannelBars"] = 30, ["BandBars"] = 25, ["BandDevs"] = 2.5m, ["Direction"] = 1,
            ["StopAtr"] = 1.5m, ["MaxBars"] = 6, ["IntradayOnly"] = 1,
            ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(20, Read<int>(strategy, "AtrBars"));
        Assert.Equal(250, Read<int>(strategy, "RegimeBars"));
        Assert.Equal(10m, Read<decimal>(strategy, "LowPercentile"));
        Assert.Equal(90m, Read<decimal>(strategy, "HighPercentile"));
        Assert.Equal(30, Read<int>(strategy, "ChannelBars"));
        Assert.Equal(25, Read<int>(strategy, "BandBars"));
        Assert.Equal(2.5m, Read<decimal>(strategy, "BandDevs"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
        Assert.True(strategy.RequiredCandles >= 250 + 20 + 1);
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestReg strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Sfondo largo 4 (98–102), ultime tre barre larghe 1 (99,5–100,5).</summary>
    private static OhlcvData[] Calm()
    {
        var bars = Flat(200, 102m, 98m);
        for (var index = bars.Length - 3; index < bars.Length; index++)
            bars[index] = Bar(bars[index].DateTime, 100.5m, 99.5m, 100m);
        return bars;
    }

    /// <summary>Sfondo largo 1 (99,5–100,5) e un'ultima barra data.</summary>
    private static OhlcvData[] Agitated(decimal high, decimal low, decimal close)
    {
        var bars = Flat(200, 100.5m, 99.5m);
        bars[^1] = Bar(bars[^1].DateTime, high, low, close);
        return bars;
    }

    private static OhlcvData[] Flat(int count, decimal high, decimal low) =>
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), high, low, 100m)).ToArray();

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

    /// <summary>REG di prova su NQ a 60 minuti, parametri corti. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestReg : VolatilityRegimeEngine
    {
        public TestReg()
        {
            IntradayOnly = false;
            AtrBars = 3;
            RegimeBars = 50;
            ChannelBars = 10;
            BandBars = 10;
            BandDevs = 2m;
            LowPercentile = 20m;
            HighPercentile = 80m;
        }

        public int Regime { set => RegimeBars = value; }
        public decimal Low { set => LowPercentile = value; }
        public decimal High { set => HighPercentile = value; }
        public int Side { set => Direction = value; }

        public override string Name => "REG_TEST";
        public override string Description => "REG di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
