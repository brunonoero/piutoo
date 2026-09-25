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
/// Il motore VLM della serie PT6EXO. Le serie di prova hanno barre orarie da 99 a 101 (ampiezza 2) con
/// volume 100: la mediana delle venti barre prima dell'ultima vale 100 per il volume e 2 per l'ampiezza,
/// quindi con il rapporto 2,5 la soglia e' 250 di volume o 5 punti di ampiezza. Ogni caso cambia l'ultima
/// barra, o una barra della finestra.
/// </summary>
public sealed class VolumeSpikeEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Volume 300 oltre la soglia 250 su una barra che sale: si segue, long a mercato sulla barra dopo.</summary>
    [Fact]
    public void AVolumeSpikeOnARisingBarBuysAtMarketOnTheNextBar()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 300m);

        var signal = Evaluate(new TestVlm(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Lo specchio: barra che scende con il volume anomalo, short.</summary>
    [Fact]
    public void AVolumeSpikeOnAFallingBarSells()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, open: 100m, high: 100.5m, low: 98.5m, close: 99m, volume: 300m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestVlm(), bars, null).Type);
    }

    /// <summary>Con <c>Mode = 1</c> la barra si sfuma: sale con il volume anomalo, si vende.</summary>
    [Fact]
    public void TheFadeModeGoesAgainstTheBar()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 300m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestVlm { Fade = true }, bars, null).Type);
    }

    /// <summary>
    /// La soglia e' stretta (250 esatti non bastano), una barra senza direzione non produce nulla anche
    /// con il volume anomalo, e un feed senza volume (mediana nulla) non e' un riferimento.
    /// </summary>
    [Fact]
    public void AtTheThresholdOrWithoutDirectionOrVolumeNothingIsEmitted()
    {
        var atThreshold = Series(200);
        atThreshold[^1] = Bar(atThreshold[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 250m);
        var doji = Series(200);
        doji[^1] = Bar(doji[^1].DateTime, open: 100m, high: 102m, low: 98m, close: 100m, volume: 1000m);
        var noVolume = Series(200, volume: 0m);
        noVolume[^1] = Bar(noVolume[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 300m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm(), atThreshold, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm(), doji, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm(), noVolume, null).Type);
    }

    /// <summary>
    /// E' la mediana, non la media: una barra da 10.000 nella finestra porterebbe la media a 595 e la soglia
    /// a 1.487,5, ma la mediana resta 100 e il volume 300 e' ancora un'anomalia.
    /// </summary>
    [Fact]
    public void OneOutlierInTheWindowDoesNotMoveTheMedian()
    {
        var bars = Series(200);
        bars[^2] = Bar(bars[^2].DateTime, open: 100m, high: 101m, low: 99m, close: 100m, volume: 10_000m);
        bars[^1] = Bar(bars[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 300m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestVlm(), bars, null).Type);
    }

    /// <summary>
    /// La leva di controllo: con <c>ActivitySource = 1</c> conta l'ampiezza, non il volume. La barra con il
    /// volume anomalo e l'ampiezza normale (2) non scatta piu'; quella con il volume normale e l'ampiezza 6
    /// (oltre 5) scatta, e sul volume no.
    /// </summary>
    [Fact]
    public void TheRangeSourceReplacesTheVolume()
    {
        var volumeOnly = Series(200);
        volumeOnly[^1] = Bar(volumeOnly[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 300m);
        var rangeOnly = Series(200);
        rangeOnly[^1] = Bar(rangeOnly[^1].DateTime, open: 100m, high: 104m, low: 98m, close: 103m, volume: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm { Source = 1 }, volumeOnly, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestVlm { Source = 1 }, rangeOnly, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm(), rangeOnly, null).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce, anche quando lo decide la sfumatura.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 300m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm { Side = 2 }, bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestVlm { Side = 1 }, bars, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm { Side = 1, Fade = true }, bars, null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, open: 100m, high: 101.5m, low: 99.5m, close: 101m, volume: 300m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestVlm(), bars, SignalType.Buy).Type);
    }

    /// <summary>Configurazioni incoerenti fermano la valutazione invece di rendere il motore muto.</summary>
    [Fact]
    public void InconsistentConfigurationsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestVlm { Ratio = 0m }.GenerateSignal(Series(200), Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestVlm { Source = 2 }.GenerateSignal(Series(200), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e la finestra allarga il minimo di barre.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_VLM();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "GC", ["TimeframeMinutes"] = 60,
            ["SpikeRatio"] = 3m, ["LookbackBars"] = 300, ["Mode"] = 1,
            ["ActivitySource"] = 1, ["Direction"] = 2,
            ["StopAtr"] = 1m, ["MaxBars"] = 4, ["IntradayOnly"] = 1,
            ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@GC", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(3m, Read<decimal>(strategy, "SpikeRatio"));
        Assert.Equal(300, Read<int>(strategy, "LookbackBars"));
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(1, Read<int>(strategy, "ActivitySource"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
        Assert.True(strategy.RequiredCandles >= 301);
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestVlm strategy, OhlcvData[] bars, SignalType? position) =>
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

    private static OhlcvData[] Series(int count, decimal volume = 100m) =>
        Enumerable.Range(0, count)
            .Select(index => Bar(Start.AddHours(index), open: 100m, high: 101m, low: 99m, close: 100m, volume: volume))
            .ToArray();

    private static OhlcvData Bar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume) =>
        new() { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = volume };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>VLM di prova su NQ a 60 minuti, rapporto 2,5 sulla mediana di 20 barre. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestVlm : VolumeSpikeEngine
    {
        public TestVlm() => IntradayOnly = false;

        public decimal Ratio { set => SpikeRatio = value; }
        public bool Fade { set => Mode = value ? 1 : 0; }
        public int Source { set => ActivitySource = value; }
        public int Side { set => Direction = value; }

        public override string Name => "VLM_TEST";
        public override string Description => "VLM di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
