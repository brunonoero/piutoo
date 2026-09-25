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
/// Il motore CDL (candela di rifiuto su un livello) della serie PT6EXO, su NQ a 60 minuti con tick di un
/// quarto di punto. Le serie di prova sono piatte — apertura e chiusura 100, alto 101, basso 99 — quindi
/// la sessione di ieri ha massimo 101 e minimo 99 e quella di oggi apre a 100. L'ultima barra apre alle
/// 07:00 UTC di martedi' 16 gennaio 2024, a meta' della sessione della ricerca.
/// </summary>
public sealed class RejectionCandleEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Pin bar sul massimo di ieri: tocca 101, ombra superiore di 1,3 contro un corpo di 0,2 (un tick
    /// come minimo: 0,25, rapporto 2 → 0,5), chiude sotto. Short a mercato sulla barra dopo.
    /// </summary>
    [Fact]
    public void APinBarRejectedFromYesterdaysHighSells()
    {
        var bars = WithLast(open: 100.2m, high: 101.5m, low: 100m, close: 100m);

        var signal = Evaluate(new TestCdl(), bars, null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Lo specchio sul minimo di ieri: long.</summary>
    [Fact]
    public void APinBarRejectedFromYesterdaysLowBuys()
    {
        var bars = WithLast(open: 99.8m, high: 100m, low: 98.5m, close: 100m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestCdl(), bars, null).Type);
    }

    /// <summary>Una barra che arriva a 100,9 non tocca 101; con quattro tick di tolleranza (un punto) si'.</summary>
    [Fact]
    public void TheToleranceWidensTheTouch()
    {
        var bars = WithLast(open: 100.2m, high: 100.9m, low: 100m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl(), bars, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestCdl { Tolerance = 4 }, bars, null).Type);
    }

    /// <summary>Una barra che chiude sopra il livello non e' un rifiuto, e un'ombra corta non e' una pin bar.</summary>
    [Fact]
    public void ACloseBeyondTheLevelOrAShortWickIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl(), WithLast(open: 101.1m, high: 102m, low: 101m, close: 101.2m), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl(), WithLast(open: 100.8m, high: 101.2m, low: 100m, close: 100m), null).Type);
    }

    /// <summary>Una doji che tocca massimo e minimo di ieri e' respinta dai due lati: non dice il verso, niente.</summary>
    [Fact]
    public void ARejectionFromBothSidesIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl(), WithLast(open: 100m, high: 101.5m, low: 98.5m, close: 100m), null).Type);
    }

    /// <summary>
    /// Engulfing ribassista sul massimo di ieri: la precedente e' verde (100 → 100,6), questa rossa
    /// (100,8 → 99,9) con il corpo che la contiene. Come pin bar la stessa barra non basta (ombra 0,5 su
    /// un corpo di 0,9). Con la precedente rossa non e' un engulfing.
    /// </summary>
    [Fact]
    public void ABearishEngulfingOnTheLevelSells()
    {
        var bars = WithLast(open: 100.8m, high: 101.3m, low: 99.8m, close: 99.9m);
        bars[^2] = Bar(bars[^2].DateTime, open: 100m, high: 100.7m, low: 99.9m, close: 100.6m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestCdl { Candle = 1 }, bars, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl(), bars, null).Type);

        bars[^2] = Bar(bars[^2].DateTime, open: 100.6m, high: 100.7m, low: 99.9m, close: 100m);
        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl { Candle = 1 }, bars, null).Type);
    }

    /// <summary>
    /// Sull'apertura della sessione (100) il livello vale per i due lati: toccato dall'alto e chiuso
    /// sotto e' short, toccato dal basso e chiuso sopra e' long. Gli estremi di ieri non sono toccati.
    /// </summary>
    [Fact]
    public void TheSessionOpenIsALevelForBothSides()
    {
        var fromAbove = WithLast(open: 99.95m, high: 100.6m, low: 99.85m, close: 99.9m);
        var fromBelow = WithLast(open: 100.05m, high: 100.15m, low: 99.4m, close: 100.1m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestCdl { Level = 1 }, fromAbove, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestCdl { Level = 1 }, fromBelow, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl(), fromAbove, null).Type);
    }

    /// <summary>Sulla prima barra della sessione l'apertura e' la barra stessa: nessun rifiuto possibile.</summary>
    [Fact]
    public void TheSessionOpenLevelIsNotReadOnTheFirstBar()
    {
        // Ultima barra alle 23:00 UTC di lunedi' 15: la mezzanotte di Roma, prima barra della sessione del 16.
        var bars = Flat(192);
        bars[^1] = Bar(bars[^1].DateTime, open: 99.95m, high: 100.6m, low: 99.85m, close: 99.9m);

        Assert.Equal(new DateTime(2024, 1, 15, 23, 0, 0, DateTimeKind.Utc), bars[^1].DateTime);
        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl { Level = 1 }, bars, null).Type);
    }

    /// <summary>
    /// Lo stop oltre l'estremo: massimo 101,5 piu' due tick (0,5) meno la chiusura 100 = due punti; per
    /// il long, chiusura 100 meno (minimo 98,5 meno 0,5) = due punti.
    /// </summary>
    [Fact]
    public void TheStopBeyondTheExtremeIsTheDistanceFromTheClose()
    {
        var shortSignal = Evaluate(new TestCdl { ExtremeStop = true, BufferTicks = 2 },
            WithLast(open: 100.2m, high: 101.5m, low: 100m, close: 100m), null);
        var longSignal = Evaluate(new TestCdl { ExtremeStop = true, BufferTicks = 2 },
            WithLast(open: 99.8m, high: 100m, low: 98.5m, close: 100m), null);

        Assert.Equal(2m * InstrumentRegistry.PointValue("@NQ"), shortSignal.StopLossMoneyPerFutureContract);
        Assert.Equal(2m * InstrumentRegistry.PointValue("@NQ"), longSignal.StopLossMoneyPerFutureContract);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = WithLast(open: 100.2m, high: 101.5m, low: 100m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl { Side = 1 }, bars, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestCdl { Side = 2 }, bars, null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        var bars = WithLast(open: 100.2m, high: 101.5m, low: 100m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestCdl(), bars, SignalType.Buy).Type);
    }

    /// <summary>Configurazioni incoerenti: livello o candela sconosciuti, rapporto dell'ombra non positivo.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        var bars = WithLast(open: 100.2m, high: 101.5m, low: 100m, close: 100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestCdl { Level = 2 }.GenerateSignal(bars, bars[^1].DateTime));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestCdl { Candle = 2 }.GenerateSignal(bars, bars[^1].DateTime));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestCdl { Ratio = 0m }.GenerateSignal(bars, bars[^1].DateTime));
    }

    /// <summary>Il contenitore legge le leve proprie, quelle comuni della griglia e il tick dello strumento.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_CDL();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 60,
            ["LevelKind"] = 1, ["CandleKind"] = 1, ["WickRatio"] = 3m, ["ToleranceTicks"] = 2,
            ["StopAtExtreme"] = 1, ["ExtremeBufferTicks"] = 4, ["Direction"] = 2,
            ["StopAtr"] = 1m, ["TargetAtr"] = 2m, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
            ["StartHour"] = 8, ["EndHour"] = 20, ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(1, Read<int>(strategy, "LevelKind"));
        Assert.Equal(1, Read<int>(strategy, "CandleKind"));
        Assert.Equal(3m, Read<decimal>(strategy, "WickRatio"));
        Assert.Equal(2, Read<int>(strategy, "ToleranceTicks"));
        Assert.Equal(1, Read<int>(strategy, "StopAtExtreme"));
        Assert.Equal(4, Read<int>(strategy, "ExtremeBufferTicks"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
        Assert.Equal(InstrumentRegistry.Get("@FDAX").TickSize, Read<decimal>(strategy, "TickSize"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestCdl strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Duecento barre piatte, l'ultima (07:00 UTC del 16 gennaio) sostituita.</summary>
    private static OhlcvData[] WithLast(decimal open, decimal high, decimal low, decimal close)
    {
        var bars = Flat(200);
        bars[^1] = Bar(bars[^1].DateTime, open, high, low, close);
        return bars;
    }

    private static OhlcvData[] Flat(int count) =>
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), 100m, 101m, 99m, 100m)).ToArray();

    private static OhlcvData Bar(DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new() { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1m };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>CDL di prova su NQ a 60 minuti: pin bar sugli estremi di ieri, tick 0,25, multiday.</summary>
    private sealed class TestCdl : RejectionCandleEngine
    {
        public TestCdl()
        {
            IntradayOnly = false;
            TickSize = 0.25m;
        }

        public int Level { set => LevelKind = value; }
        public int Candle { set => CandleKind = value; }
        public decimal Ratio { set => WickRatio = value; }
        public int Tolerance { set => ToleranceTicks = value; }
        public bool ExtremeStop { set => StopAtExtreme = value ? 1 : 0; }
        public int BufferTicks { set => ExtremeBufferTicks = value; }
        public int Side { set => Direction = value; }

        public override string Name => "CDL_TEST";
        public override string Description => "CDL di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
