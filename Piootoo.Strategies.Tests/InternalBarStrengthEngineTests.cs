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
/// Il motore IBS della serie PT6EXO. Le serie di prova hanno barre da 99 a 101 che chiudono a 100
/// (IBS 0,5); ogni caso cambia l'ultima barra o il livello delle chiusure.
/// </summary>
public sealed class InternalBarStrengthEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Chiusura vicino al minimo (IBS 0,1): long a mercato sulla barra dopo.</summary>
    [Fact]
    public void ACloseNearTheLowBuysAtMarketOnTheNextBar()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 101m, low: 99m, close: 99.2m);

        var signal = Evaluate(new TestIbs(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.False(signal.ExitOnly);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Chiusura vicino al massimo (IBS 0,9): short.</summary>
    [Fact]
    public void ACloseNearTheHighSells()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 101m, low: 99m, close: 100.8m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestIbs(), bars, null).Type);
    }

    /// <summary>A meta' range, o su una barra senza range, niente.</summary>
    [Fact]
    public void AMidRangeOrFlatBarIsNotASignal()
    {
        var mid = Series(200);
        var flat = Series(200);
        flat[^1] = Bar(flat[^1].DateTime, high: 100m, low: 100m, close: 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestIbs(), mid, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestIbs(), flat, null).Type);
    }

    /// <summary>
    /// Il filtro di trend: si compra la debolezza solo sopra la media. Con le chiusure precedenti a 110
    /// la media e' sopra la barra di segnale e il long non nasce; con quelle a 90 si'.
    /// </summary>
    [Fact]
    public void TheTrendFilterKeepsTheLongAboveTheAverage()
    {
        var belowAverage = Series(200, close: 110m, high: 111m, low: 109m);
        belowAverage[^1] = Bar(belowAverage[^1].DateTime, high: 101m, low: 99m, close: 99.2m);
        var aboveAverage = Series(200, close: 90m, high: 91m, low: 89m);
        aboveAverage[^1] = Bar(aboveAverage[^1].DateTime, high: 101m, low: 99m, close: 99.2m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestIbs { Trend = 50 }, belowAverage, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestIbs { Trend = 50 }, aboveAverage, null).Type);
    }

    /// <summary>
    /// L'uscita classica: in long, la prima chiusura con IBS almeno <c>ExitIbs</c> chiude a mercato
    /// sulla barra dopo, e il segnale e' di sola uscita — non apre uno short.
    /// </summary>
    [Fact]
    public void ALongExitsOnTheFirstStrongClose()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 101m, low: 99m, close: 100.6m);

        var signal = Evaluate(new TestIbs { Exit = 0.7m }, bars, SignalType.Buy);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.True(signal.ExitOnly);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
    }

    /// <summary>Lo specchio: lo short esce alla prima chiusura con IBS al massimo 1 − ExitIbs.</summary>
    [Fact]
    public void AShortExitsOnTheFirstWeakClose()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 101m, low: 99m, close: 99.4m);

        var signal = Evaluate(new TestIbs { Exit = 0.7m }, bars, SignalType.Sell);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.True(signal.ExitOnly);
    }

    /// <summary>Senza <c>ExitIbs</c>, e in posizione, non nasce nulla: ne' uscite a segnale ne' ingressi.</summary>
    [Fact]
    public void InPositionWithoutExitIbsNothingIsEmitted()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 101m, low: 99m, close: 99.2m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestIbs(), bars, SignalType.Buy).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestIbs(), bars, SignalType.Sell).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = Series(200);
        bars[^1] = Bar(bars[^1].DateTime, high: 101m, low: 99m, close: 99.2m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestIbs { Side = 2 }, bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestIbs { Side = 1 }, bars, null).Type);
    }

    /// <summary>Soglie incoerenti sono un errore di configurazione, non un motore muto.</summary>
    [Fact]
    public void InconsistentThresholdsAreRejected()
    {
        var strategy = new TestIbs { Low = 0.9m, High = 0.1m };

        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateSignal(Series(200), Start));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni, e la media allarga la finestra minima.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_IBS();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 1440,
            ["LowThreshold"] = 0.15m, ["HighThreshold"] = 0.85m, ["TrendBars"] = 200,
            ["ExitIbs"] = 0.7m, ["Direction"] = 1,
            ["StopAtr"] = 2m, ["MaxBars"] = 5, ["IntradayOnly"] = 0,
            ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.Equal(1440, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(0.15m, Read<decimal>(strategy, "LowThreshold"));
        Assert.Equal(0.85m, Read<decimal>(strategy, "HighThreshold"));
        Assert.Equal(200, Read<int>(strategy, "TrendBars"));
        Assert.Equal(0.7m, Read<decimal>(strategy, "ExitIbs"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
        Assert.True(strategy.RequiredCandles >= 201);
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestIbs strategy, OhlcvData[] bars, SignalType? position) =>
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

    private static OhlcvData[] Series(int count, decimal close = 100m, decimal high = 101m, decimal low = 99m) =>
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), high, low, close)).ToArray();

    private static OhlcvData Bar(DateTime time, decimal high, decimal low, decimal close) =>
        new() { DateTime = time, Open = close, High = high, Low = low, Close = close, Volume = 1m };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>IBS di prova su NQ a 60 minuti, soglie 0,2/0,8. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestIbs : InternalBarStrengthEngine
    {
        public TestIbs() => IntradayOnly = false;

        public decimal Low { set => LowThreshold = value; }
        public decimal High { set => HighThreshold = value; }
        public int Trend { set => TrendBars = value; }
        public decimal Exit { set => ExitIbs = value; }
        public int Side { set => Direction = value; }

        public override string Name => "IBS_TEST";
        public override string Description => "IBS di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
