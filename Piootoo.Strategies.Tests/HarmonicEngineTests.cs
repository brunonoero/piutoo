using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PTARMStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I motori armonici della serie PTARM, su NQ a 60 minuti con tick di 0,25 e pivot a 3 barre. Le serie
/// sono barre senza range (alto = basso = chiusura) che percorrono gambe rettilinee: sfondo piatto a 1050,
/// un massimo a 1100 che chiude lo swing X, poi X = 1000 e A = 1500 (XA = 500), e B, C e la gamba CD in
/// corso secondo la figura. L'ultima gamba ha tre barre, quante ne servono a confermare C.
/// </summary>
public sealed class HarmonicEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gartley: B = 1191 (AB/XA = 309/500 = 0,618), C = 1376 (BC/AB = 185/309 = 0,599), D = 1500 − 0,786 ×
    /// 500 = 1107, CD/BC = 269/185 = 1,45. La gamba CD scende fino a 1250, lontana da D.
    /// </summary>
    private static readonly (decimal Target, int Steps)[] Gartley =
        [(1100m, 5), (1000m, 5), (1500m, 10), (1191m, 6), (1376m, 4), (1250m, 3)];

    /// <summary>Bat: B = 1250 (0,5), C = 1375 (0,5), D = 1500 − 0,886 × 500 = 1057, CD/BC = 318/125 = 2,54.</summary>
    private static readonly (decimal Target, int Steps)[] Bat =
        [(1100m, 5), (1000m, 5), (1500m, 10), (1250m, 6), (1375m, 4), (1300m, 3)];

    /// <summary>Butterfly: B = 1107 (0,786), C = 1343 (236/393 = 0,60), D = 1500 − 1,27 × 500 = 865, sotto X; CD/BC = 2,03.</summary>
    private static readonly (decimal Target, int Steps)[] Butterfly =
        [(1100m, 5), (1000m, 5), (1500m, 10), (1107m, 6), (1343m, 4), (1200m, 3)];

    /// <summary>La Gartley rialzista arma un buy limit su D, valido una barra, con il livello superato a mercato.</summary>
    [Fact]
    public void ABullishGartleyArmsABuyLimitOnD()
    {
        var bars = Series(Gartley);

        var signal = Evaluate(new TestGartley(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Limit, signal.OrderType);
        Assert.Equal(1107m, signal.Price);
        Assert.Equal(CrossedLevelPolicy.Market, signal.CrossedLevel);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Lo specchio attorno a 1500: sell limit a 3000 − 1107.</summary>
    [Fact]
    public void ABearishGartleyIsTheMirror()
    {
        var signal = Evaluate(new TestGartley(), Mirror(Series(Gartley)), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(TradeOrderType.Limit, signal.OrderType);
        Assert.Equal(1893m, signal.Price);
    }

    /// <summary>Le figure non si confondono: la Bat vede la sua serie e non la Gartley, e viceversa.</summary>
    [Fact]
    public void EachPatternRecognisesOnlyItsOwnRatios()
    {
        var bat = Evaluate(new TestBat(), Series(Bat), null);

        Assert.Equal(SignalType.Buy, bat.Type);
        Assert.Equal(1057m, bat.Price);
        Assert.Equal(SignalType.Hold, Evaluate(new TestGartley(), Series(Bat), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestBat(), Series(Gartley), null).Type);
    }

    /// <summary>
    /// Il controllo con i rapporti falsati (scala 0,9: B cercato fra 0,53 e 0,58) non vede la Gartley vera:
    /// la leva cambia davvero la figura.
    /// </summary>
    [Fact]
    public void TheRatioScaleControlLooksForADifferentFigure()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestGartley { Scale = 0.9m }, Series(Gartley), null).Type);
    }

    /// <summary>
    /// Una D gia' toccata (la gamba CD scende a 1100) e' una figura consumata, e una C piu' vecchia di
    /// <c>MaxDBars</c> barre e' scaduta: niente ordine.
    /// </summary>
    [Fact]
    public void ATouchedOrExpiredDEmitsNothing()
    {
        var touched = Series([(1100m, 5), (1000m, 5), (1500m, 10), (1191m, 6), (1376m, 4), (1100m, 3)]);

        Assert.Equal(SignalType.Hold, Evaluate(new TestGartley(), touched, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestGartley { MaxLegBars = 2 }, Series(Gartley), null).Type);
    }

    /// <summary>
    /// Stop strutturale oltre X (1000) dal livello 1107: 107 punti. Target a 0,618 della gamba AD
    /// (1500 − 1107 = 393): 242,874 punti. In denaro per contratto.
    /// </summary>
    [Fact]
    public void TheStructuralStopAndTheAdTargetAreMeasuredFromD()
    {
        var signal = Evaluate(new TestGartley { Structure = 1, Target = 0.618m }, Series(Gartley), null);

        var pointValue = InstrumentRegistry.PointValue("@NQ");
        Assert.Equal(Math.Round(107m * pointValue, 2), signal.StopLossMoneyPerFutureContract);
        Assert.Equal(Math.Round(242.874m * pointValue, 2), signal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>
    /// Butterfly: D = 865 sta oltre X, e lo stop strutturale senza margine cadrebbe sull'ingresso: resta
    /// quello comune, qui nessuno.
    /// </summary>
    [Fact]
    public void AButterflyHasDBeyondXAndNeedsAMarginForTheStructuralStop()
    {
        var signal = Evaluate(new TestButterfly { Structure = 1 }, Series(Butterfly), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(865m, signal.Price);
        Assert.Null(signal.StopLossMoneyPerFutureContract);
    }

    /// <summary>In posizione, o con il lato opposto dichiarato, non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPositionOrOnTheOtherSide()
    {
        var bars = Series(Gartley);

        Assert.Equal(SignalType.Hold, Evaluate(new TestGartley(), bars, SignalType.Buy).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestGartley { Side = 2 }, bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestGartley { Side = 1 }, bars, null).Type);
    }

    /// <summary>Pivot nulli, tolleranza piena, scala nulla: errori di configurazione.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        var bars = Series(Gartley);

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestGartley { Pivot = 0 }.GenerateSignal(bars, Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestGartley { Tolerant = 1m }.GenerateSignal(bars, Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestGartley { Scale = 0m }.GenerateSignal(bars, Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e prende il tick dal registro strumenti.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_GAR();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 60,
            ["PivotBars"] = 5, ["Tolerance"] = 0.08m, ["MaxDBars"] = 20, ["LookbackBars"] = 400,
            ["RatioScale"] = 0.9m, ["StopStructure"] = 1, ["StopBufferAtr"] = 0.5m, ["AtrBars"] = 14,
            ["TargetAd"] = 0.382m, ["Direction"] = 2,
            ["StopAtr"] = 1.5m, ["TargetAtr"] = 0m, ["IntradayOnly"] = 1, ["ExitHour"] = 21,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(5, Read<int>(strategy, "PivotBars"));
        Assert.Equal(0.08m, Read<decimal>(strategy, "Tolerance"));
        Assert.Equal(20, Read<int>(strategy, "MaxDBars"));
        Assert.Equal(400, Read<int>(strategy, "LookbackBars"));
        Assert.Equal(0.9m, Read<decimal>(strategy, "RatioScale"));
        Assert.Equal(1, Read<int>(strategy, "StopStructure"));
        Assert.Equal(0.5m, Read<decimal>(strategy, "StopBufferAtr"));
        Assert.Equal(14, Read<int>(strategy, "AtrBars"));
        Assert.Equal(0.382m, Read<decimal>(strategy, "TargetAd"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
        Assert.Equal(1, Read<int>(strategy, "MaxEntriesPerSession"));
        Assert.Equal(InstrumentRegistry.Get("@FDAX").TickSize, Read<decimal>(strategy, "TickSize"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(HarmonicEngine strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Sfondo piatto di 300 barre a 1050, poi le gambe: ogni gamba arriva al suo estremo in N barre uguali.</summary>
    private static OhlcvData[] Series((decimal Target, int Steps)[] legs)
    {
        var bars = Enumerable.Range(0, 300).Select(index => Bar(Start.AddHours(index), 1050m)).ToList();
        var current = 1050m;
        foreach (var (target, steps) in legs)
        {
            for (var step = 1; step <= steps; step++)
                bars.Add(Bar(Start.AddHours(bars.Count), current + (target - current) * step / steps));
            current = target;
        }

        return bars.ToArray();
    }

    /// <summary>Lo specchio attorno a 1500.</summary>
    private static OhlcvData[] Mirror(OhlcvData[] bars) =>
        bars.Select(bar => Bar(bar.DateTime, 3000m - bar.Close)).ToArray();

    private static OhlcvData Bar(DateTime time, decimal price) =>
        new() { DateTime = time, Open = price, High = price, Low = price, Close = price, Volume = 1m };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    private sealed class TestGartley : GartleyEngine
    {
        public TestGartley()
        {
            IntradayOnly = false;
            TickSize = 0.25m;
        }

        public int Pivot { set => PivotBars = value; }
        public decimal Tolerant { set => Tolerance = value; }
        public decimal Scale { set => RatioScale = value; }
        public int MaxLegBars { set => MaxDBars = value; }
        public int Structure { set => StopStructure = value; }
        public decimal Target { set => TargetAd = value; }
        public int Side { set => Direction = value; }

        public override string Name => "GAR_TEST";
        public override string Description => "Gartley di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }

    private sealed class TestBat : BatEngine
    {
        public TestBat()
        {
            IntradayOnly = false;
            TickSize = 0.25m;
        }

        public override string Name => "BAT_TEST";
        public override string Description => "Bat di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }

    private sealed class TestButterfly : ButterflyEngine
    {
        public TestButterfly()
        {
            IntradayOnly = false;
            TickSize = 0.25m;
        }

        public int Structure { set => StopStructure = value; }

        public override string Name => "BUT_TEST";
        public override string Description => "Butterfly di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
