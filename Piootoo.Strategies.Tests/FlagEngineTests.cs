using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT7CLPStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il motore FLG (bandiera) della serie PT7CLP, su NQ a 60 minuti con tick di 0,25. Lo sfondo e' piatto
/// — alto 101, basso 99, chiusura 100 —, quindi l'ATR delle barre prima del palo vale 2. Il palo sale in
/// tre barre fino a 112, con base il 99 dello sfondo: altezza 13, oltre le 3 ATR (6) di default. La
/// bandiera di riferimento scende di un punto a barra su massimi e minimi, rette parallele:
/// massimi 111, 110, 109, 108 e minimi 109, 108, 107, 106.
/// </summary>
public sealed class FlagEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    private static readonly (decimal High, decimal Low, decimal Close)[] Pole =
        [(104m, 100m, 103.5m), (108m, 104m, 107.5m), (112m, 108m, 111.5m)];

    private static readonly (decimal High, decimal Low, decimal Close)[] ParallelFlag =
        [(111m, 109m, 109.5m), (110m, 108m, 108.5m), (109m, 107m, 107.5m), (108m, 106m, 106.5m)];

    /// <summary>
    /// La retta dei massimi, 111 − x, vale 108 sull'ultima barra (chiusura 106,5: non ancora rotta) e 107
    /// sulla barra dopo: li' nasce il buy stop, valido una barra. La lunghezza 3 non e' una bandiera,
    /// perche' la sua "cima" (111) sta sotto il 112 del palo.
    /// </summary>
    [Fact]
    public void ABullFlagArmsABuyStopOnTheProjectedUpperLine()
    {
        var bars = Series(ParallelFlag);

        var signal = Evaluate(new TestFlag(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(107m, signal.Price);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
        Assert.Null(signal.CompanionSignals);
    }

    /// <summary>Lo specchio attorno a 100: palo in discesa, bandiera che risale, sell stop a 200 − 107.</summary>
    [Fact]
    public void ABearFlagIsTheMirror()
    {
        var signal = Evaluate(new TestFlag(), Mirror(Series(ParallelFlag)), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(93m, signal.Price);
        Assert.Null(signal.CompanionSignals);
    }

    /// <summary>
    /// Non e' una bandiera: un palo sotto la soglia (7 ATR = 14 contro 13), un ritracciamento oltre il
    /// massimo (40% del palo: il minimo 106 sta sotto 112 − 5,2), una bandiera che sale con il palo invece
    /// di andargli contro.
    /// </summary>
    [Fact]
    public void AShortPoleADeepFlagOrARisingFlagAreNotFlags()
    {
        var bars = Series(ParallelFlag);
        var rising = Series([(110m, 108m, 109m), (110.5m, 108.5m, 109.5m), (111m, 109m, 110m), (111.5m, 109.5m, 110.5m)]);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFlag { PoleHeightAtr = 7m }, bars, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestFlag { Retrace = 0.4m }, bars, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestFlag(), rising, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestFlag(), Flat(200), null).Type);
    }

    /// <summary>
    /// Rette convergenti (massimi −1 a barra, minimi +0,5): differenza di pendenza 1,5, oltre la tolleranza
    /// di 0,1 ATR (0,2) ma dentro quella di 1 ATR (2). Con la tolleranza larga il pennant passa.
    /// </summary>
    [Fact]
    public void ConvergingLinesPassOnlyWithAWideParallelTolerance()
    {
        var pennant = Series([(111m, 106m, 107.5m), (110m, 106.5m, 107.5m), (109m, 107m, 107.5m), (108m, 107.5m, 107.6m)]);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFlag(), pennant, null).Type);

        var signal = Evaluate(new TestFlag { Parallel = 1m }, pennant, null);
        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(107m, signal.Price);
    }

    /// <summary>
    /// Stop oltre il minimo della bandiera di due tick (106 − 0,5) dal livello 107: 1,5 punti. Target pari
    /// all'altezza del palo, 13 punti. In denaro per contratto.
    /// </summary>
    [Fact]
    public void StopAtTheFlagAndTargetAtThePoleAreMeasuredFromTheEntryLevel()
    {
        var signal = Evaluate(new TestFlag { StopFlag = 1, Buffer = 2, Target = 1m }, Series(ParallelFlag), null);

        var pointValue = InstrumentRegistry.PointValue("@NQ");
        Assert.Equal(Math.Round(1.5m * pointValue, 2), signal.StopLossMoneyPerFutureContract);
        Assert.Equal(Math.Round(13m * pointValue, 2), signal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>In posizione, o con il lato opposto dichiarato, non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPositionOrOnTheOtherSide()
    {
        var bars = Series(ParallelFlag);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFlag(), bars, SignalType.Buy).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestFlag { Side = 2 }, bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestFlag { Side = 1 }, bars, null).Type);
    }

    /// <summary>Bandiera di una barra, massimo sotto il minimo, ritracciamento nullo: errori di configurazione.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestFlag { ShortestFlag = 1 }.GenerateSignal(Flat(200), Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestFlag { ShortestFlag = 6, LongestFlag = 5 }.GenerateSignal(Flat(200), Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestFlag { Retrace = 0m }.GenerateSignal(Flat(200), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e prende il tick dal registro strumenti.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_FLG();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 60,
            ["PoleBars"] = 4, ["PoleAtr"] = 2.5m, ["AtrBars"] = 30, ["FlagMinBars"] = 4, ["FlagMaxBars"] = 12,
            ["MaxRetrace"] = 0.38m, ["ParallelTolerance"] = 0.2m, ["OffsetTicks"] = 1, ["StopAtFlag"] = 1,
            ["ExtremeBufferTicks"] = 3, ["TargetPole"] = 1m, ["Direction"] = 1,
            ["StopAtr"] = 1.5m, ["TargetAtr"] = 0m, ["IntradayOnly"] = 1, ["ExitHour"] = 21,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(4, Read<int>(strategy, "PoleBars"));
        Assert.Equal(2.5m, Read<decimal>(strategy, "PoleAtr"));
        Assert.Equal(30, Read<int>(strategy, "AtrBars"));
        Assert.Equal(4, Read<int>(strategy, "FlagMinBars"));
        Assert.Equal(12, Read<int>(strategy, "FlagMaxBars"));
        Assert.Equal(0.38m, Read<decimal>(strategy, "MaxRetrace"));
        Assert.Equal(0.2m, Read<decimal>(strategy, "ParallelTolerance"));
        Assert.Equal(1, Read<int>(strategy, "OffsetTicks"));
        Assert.Equal(1, Read<int>(strategy, "StopAtFlag"));
        Assert.Equal(3, Read<int>(strategy, "ExtremeBufferTicks"));
        Assert.Equal(1m, Read<decimal>(strategy, "TargetPole"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
        Assert.Equal(1, Read<int>(strategy, "MaxEntriesPerSession"));
        Assert.Equal(InstrumentRegistry.Get("@FDAX").TickSize, Read<decimal>(strategy, "TickSize"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestFlag strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Sfondo piatto di 200 barre, poi il palo e la bandiera data.</summary>
    private static OhlcvData[] Series((decimal High, decimal Low, decimal Close)[] flag)
    {
        var bars = Flat(200).ToList();
        foreach (var (high, low, close) in Pole.Concat(flag))
            bars.Add(Bar(Start.AddHours(bars.Count), high, low, close));
        return bars.ToArray();
    }

    /// <summary>Lo specchio attorno a 100: il rialzo diventa ribasso, massimi e minimi si scambiano.</summary>
    private static OhlcvData[] Mirror(OhlcvData[] bars) =>
        bars.Select(bar => new OhlcvData
        {
            DateTime = bar.DateTime,
            Open = 200m - bar.Open,
            High = 200m - bar.Low,
            Low = 200m - bar.High,
            Close = 200m - bar.Close,
            Volume = bar.Volume
        }).ToArray();

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

    /// <summary>Bandiera di prova su NQ a 60 minuti con i default del contenitore e tick 0,25.</summary>
    private sealed class TestFlag : FlagEngine
    {
        public TestFlag()
        {
            IntradayOnly = false;
            TickSize = 0.25m;
        }

        public decimal PoleHeightAtr { set => PoleAtr = value; }
        public decimal Retrace { set => MaxRetrace = value; }
        public decimal Parallel { set => ParallelTolerance = value; }
        public int ShortestFlag { set => FlagMinBars = value; }
        public int LongestFlag { set => FlagMaxBars = value; }
        public int StopFlag { set => StopAtFlag = value; }
        public int Buffer { set => ExtremeBufferTicks = value; }
        public decimal Target { set => TargetPole = value; }
        public int Side { set => Direction = value; }

        public override string Name => "FLG_TEST";
        public override string Description => "FLG di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
