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
/// Il motore FIB (conte di Fibonacci nel tempo) della serie PT6EXO, su NQ a 60 minuti. Il punto che
/// conta e' che la conta sia sulle barre della serie e parta dall'ultimo pivot confermato: tredici barre
/// dopo, non dodici ne' quattordici, e non tredici ore.
/// </summary>
public sealed class FibonacciTimeEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    // Il pivot sta sempre qui; con la conta di default il segnale nasce sulla barra Pivot + 13.
    private const int Pivot = 180;

    /// <summary>
    /// Dopo un pivot di massimo il movimento e' al ribasso: in modo 0 (contro) si compra, a mercato
    /// sulla barra dopo quella che chiude la tredicesima barra.
    /// </summary>
    [Fact]
    public void ThirteenBarsAfterASwingHighTheEngineBuysAgainstTheMove()
    {
        var bars = WithSwingHigh(Flat(220), Pivot);

        var signal = Evaluate(new TestFib(), bars[..(Pivot + 14)], null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[Pivot + 13].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Una barra prima o una dopo la conta non e' un segnale: la conta e' esatta.</summary>
    [Fact]
    public void OnlyTheExactCountIsASignal()
    {
        var bars = WithSwingHigh(Flat(220), Pivot);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFib(), bars[..(Pivot + 13)], null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestFib(), bars[..(Pivot + 15)], null).Type);
    }

    /// <summary>Il verso: dopo un minimo il movimento e' al rialzo; modo 0 va contro, modo 1 a favore.</summary>
    [Theory]
    [InlineData(true, 0, SignalType.Buy)]
    [InlineData(true, 1, SignalType.Sell)]
    [InlineData(false, 0, SignalType.Sell)]
    [InlineData(false, 1, SignalType.Buy)]
    public void TheModeChoosesTheSide(bool swingHigh, int mode, SignalType expected)
    {
        var bars = swingHigh ? WithSwingHigh(Flat(220), Pivot) : WithSwingLow(Flat(220), Pivot);

        Assert.Equal(expected, Evaluate(new TestFib { ModeValue = mode }, bars[..(Pivot + 14)], null).Type);
    }

    /// <summary>Un lato dichiarato tiene solo quel lato: dopo un massimo, in modo 0, lo short-only tace.</summary>
    [Theory]
    [InlineData(1, SignalType.Buy)]
    [InlineData(2, SignalType.Hold)]
    public void ADeclaredDirectionFiltersTheSide(int direction, SignalType expected)
    {
        var bars = WithSwingHigh(Flat(220), Pivot);

        Assert.Equal(expected, Evaluate(new TestFib { DirectionValue = direction }, bars[..(Pivot + 14)], null).Type);
    }

    /// <summary>
    /// Un pivot confermato dopo quello della conta cambia lo swing e ferma la conta; uno non ancora
    /// confermato (meno di tre barre dopo) non conta.
    /// </summary>
    [Fact]
    public void AConfirmedLaterSwingRestartsTheCount()
    {
        var confirmedLater = WithSwingLow(WithSwingHigh(Flat(220), Pivot), Pivot + 8);
        Assert.Equal(SignalType.Hold, Evaluate(new TestFib(), confirmedLater[..(Pivot + 14)], null).Type);

        var unconfirmedLater = WithSwingLow(WithSwingHigh(Flat(220), Pivot), Pivot + 12);
        Assert.Equal(SignalType.Buy, Evaluate(new TestFib(), unconfirmedLater[..(Pivot + 14)], null).Type);
    }

    /// <summary>
    /// Si contano le barre stampate, non il tempo: un fine settimana di 48 ore fra il pivot e il segnale
    /// non cambia la barra su cui la conta finisce.
    /// </summary>
    [Fact]
    public void TheCountIsOnPrintedBarsNotOnTime()
    {
        var bars = WithSwingHigh(Flat(220), Pivot);
        for (var index = Pivot + 6; index < bars.Length; index++)
            bars[index] = Copy(bars[index], bars[index].DateTime.AddHours(48));

        var signal = Evaluate(new TestFib(), bars[..(Pivot + 14)], null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(bars[Pivot + 13].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Una serie piatta non ha pivot: i pareggi non sono swing.</summary>
    [Fact]
    public void AFlatSeriesHasNoSwing()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestFib(), Flat(200), null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        var bars = WithSwingHigh(Flat(220), Pivot);

        Assert.Equal(SignalType.Hold, Evaluate(new TestFib(), bars[..(Pivot + 14)], SignalType.Sell).Type);
    }

    /// <summary>
    /// Configurazioni incoerenti fermano la valutazione: una conta che non e' di Fibonacci, un pivot che
    /// si conferma dopo la fine della conta, un modo o un lato fuori intervallo.
    /// </summary>
    [Theory]
    [InlineData(3, 10, 0, 0)]
    [InlineData(5, 3, 0, 0)]
    [InlineData(0, 13, 0, 0)]
    [InlineData(3, 13, 2, 0)]
    [InlineData(3, 13, 0, 3)]
    public void AnIncoherentConfigurationIsRejected(int pivotBars, int countBars, int mode, int direction)
    {
        var strategy = new TestFib
        {
            PivotValue = pivotBars, CountValue = countBars, ModeValue = mode, DirectionValue = direction
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateSignal(Flat(200), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni della griglia.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_FIB();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60,
            ["PivotBars"] = 2, ["CountBars"] = 21, ["Mode"] = 1, ["Direction"] = 2,
            ["StopAtr"] = 1m, ["IntradayOnly"] = 0, ["ExitHour"] = -1,
            ["StartHour"] = -1, ["EndHour"] = -1, ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(2, Read<int>(strategy, "PivotBars"));
        Assert.Equal(21, Read<int>(strategy, "CountBars"));
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestFib strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Barre orarie piatte: alti e bassi tutti uguali, nessun pivot.</summary>
    private static OhlcvData[] Flat(int count) =>
        Enumerable.Range(0, count).Select(index => new OhlcvData
        {
            DateTime = Start.AddHours(index),
            Open = 100m, High = 101m, Low = 99m, Close = 100m, Volume = 1m
        }).ToArray();

    private static OhlcvData[] WithSwingHigh(OhlcvData[] bars, int index)
    {
        bars[index] = new OhlcvData
        {
            DateTime = bars[index].DateTime, Open = 100m, High = 110m, Low = 99m, Close = 100m, Volume = 1m
        };
        return bars;
    }

    private static OhlcvData[] WithSwingLow(OhlcvData[] bars, int index)
    {
        bars[index] = new OhlcvData
        {
            DateTime = bars[index].DateTime, Open = 100m, High = 101m, Low = 90m, Close = 100m, Volume = 1m
        };
        return bars;
    }

    private static OhlcvData Copy(OhlcvData bar, DateTime time) => new()
    {
        DateTime = time, Open = bar.Open, High = bar.High, Low = bar.Low, Close = bar.Close, Volume = bar.Volume
    };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>FIB di prova su NQ a 60 minuti, multiday: pivot di 3 barre, conta 13, contro il movimento.</summary>
    private sealed class TestFib : FibonacciTimeEngine
    {
        public TestFib() => IntradayOnly = false;

        public int PivotValue { set => PivotBars = value; }
        public int CountValue { set => CountBars = value; }
        public int ModeValue { set => Mode = value; }
        public int DirectionValue { set => Direction = value; }

        public override string Name => "FIB_TEST";
        public override string Description => "FIB di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
