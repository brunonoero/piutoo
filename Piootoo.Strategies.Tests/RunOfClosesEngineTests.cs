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
/// Il motore RUN (serie di chiusure) della serie PT6EXO, su NQ a 60 minuti. Lo sfondo delle serie di
/// prova alterna chiusure a 100 e 101: nessuna serie di due chiusure nello stesso verso, e un RSI a due
/// periodi che oscilla fra 33 e 67. Ogni caso cambia solo le ultime chiusure.
/// </summary>
public sealed class RunOfClosesEngineTests
{
    private static readonly DateTime Start = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Tre chiusure di fila sotto la precedente: long a mercato sulla barra dopo.</summary>
    [Fact]
    public void ThreeLowerClosesBuyAtMarketOnTheNextBar()
    {
        var bars = WithTail(Alternating(200), 103m, 102m, 101m, 100m);

        var signal = Evaluate(new TestRun(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.False(signal.ExitOnly);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(bars[^1].DateTime.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Lo specchio: tre chiusure di fila sopra la precedente vendono.</summary>
    [Fact]
    public void ThreeHigherClosesSell()
    {
        var bars = WithTail(Alternating(200), 100m, 101m, 102m, 103m);

        Assert.Equal(SignalType.Sell, Evaluate(new TestRun(), bars, null).Type);
    }

    /// <summary>
    /// Una chiusura uguale alla precedente interrompe la serie, e due chiusure nello stesso verso non
    /// ne fanno tre. Lo sfondo alternato non e' mai una serie.
    /// </summary>
    [Fact]
    public void AnInterruptedOrShortRunIsNotASignal()
    {
        var equalClose = WithTail(Alternating(200), 100m, 101m, 102m, 102m);
        var shortRun = WithTail(Alternating(200), 102m, 100m, 101m, 102m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestRun(), equalClose, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRun(), shortRun, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRun(), Alternating(200), null).Type);
    }

    /// <summary>
    /// RSI a due periodi: due ribassi di cinque punti dopo lo sfondo alternato portano l'RSI sotto 5
    /// (guadagno medio al piu' 1/6, perdita media almeno 3,75), quindi sotto la soglia di 10: long. Lo
    /// specchio con due rialzi lo porta sopra 95: short. Sullo sfondo, fra 33 e 67, niente.
    /// </summary>
    [Fact]
    public void AnExtremeWilderRsiEntersAgainstTheMove()
    {
        var falling = Alternating(200);
        falling[^2] = Bar(falling[^2].DateTime, falling[^3].Close - 5m);
        falling[^1] = Bar(falling[^1].DateTime, falling[^3].Close - 10m);
        var rising = Alternating(200);
        rising[^2] = Bar(rising[^2].DateTime, rising[^3].Close + 5m);
        rising[^1] = Bar(rising[^1].DateTime, rising[^3].Close + 10m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestRun { RsiMode = true }, falling, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestRun { RsiMode = true }, rising, null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRun { RsiMode = true }, Alternating(200), null).Type);
    }

    /// <summary>
    /// Il filtro di trend: il long nasce solo sopra la media. Con lo sfondo a 110 la media delle ultime
    /// 50 chiusure e' sopra la chiusura di segnale (100) e il long non nasce; con lo sfondo a 90 la media
    /// e' 90,92 e si'.
    /// </summary>
    [Fact]
    public void TheTrendFilterKeepsTheLongAboveTheAverage()
    {
        var belowAverage = WithTail(Constant(200, 110m), 103m, 102m, 101m, 100m);
        var aboveAverage = WithTail(Constant(200, 90m), 103m, 102m, 101m, 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestRun { Trend = 50 }, belowAverage, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestRun { Trend = 50 }, aboveAverage, null).Type);
    }

    /// <summary>
    /// L'uscita alla prima chiusura contraria: in long una chiusura sopra la precedente chiude a mercato
    /// sulla barra dopo, e il segnale e' di sola uscita. Una chiusura sotto la precedente non chiude.
    /// </summary>
    [Fact]
    public void ALongExitsOnTheFirstHigherClose()
    {
        var higher = WithTail(Alternating(200), 100m, 101m);
        var lower = WithTail(Alternating(200), 101m, 100m);

        var signal = Evaluate(new TestRun { OppositeCloseExit = true }, higher, SignalType.Buy);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.True(signal.ExitOnly);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRun { OppositeCloseExit = true }, lower, SignalType.Buy).Type);
    }

    /// <summary>Lo specchio: lo short esce alla prima chiusura sotto la precedente.</summary>
    [Fact]
    public void AShortExitsOnTheFirstLowerClose()
    {
        var bars = WithTail(Alternating(200), 101m, 100m);

        var signal = Evaluate(new TestRun { OppositeCloseExit = true }, bars, SignalType.Sell);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.True(signal.ExitOnly);
    }

    /// <summary>In posizione, senza uscita a segnale, non nasce nulla: nemmeno su una serie nuova.</summary>
    [Fact]
    public void InPositionWithoutTheSignalExitNothingIsEmitted()
    {
        var bars = WithTail(Alternating(200), 103m, 102m, 101m, 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestRun(), bars, SignalType.Buy).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestRun(), bars, SignalType.Sell).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = WithTail(Alternating(200), 103m, 102m, 101m, 100m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestRun { Side = 2 }, bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestRun { Side = 1 }, bars, null).Type);
    }

    /// <summary>Soglie RSI invertite o un modo che non esiste sono errori di configurazione.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestRun { RsiLowValue = 90m, RsiHighValue = 10m }.GenerateSignal(Alternating(200), Start));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestRun { ModeValue = 2 }.GenerateSignal(Alternating(200), Start));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestRun { RunValue = 0 }.GenerateSignal(Alternating(200), Start));
    }

    /// <summary>
    /// Il contenitore legge le leve proprie e quelle comuni, e in modo RSI la finestra minima copre le
    /// dieci volte il periodo (141 barre a 14 periodi, piu' delle 6 della base su D1).
    /// </summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_RUN();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 1440,
            ["Mode"] = 1, ["RunBars"] = 4, ["RsiPeriod"] = 14, ["RsiLow"] = 5m, ["RsiHigh"] = 95m,
            ["TrendBars"] = 100, ["ExitOnFirstOppositeClose"] = 1, ["Direction"] = 1,
            ["StopAtr"] = 2m, ["MaxBars"] = 5, ["IntradayOnly"] = 0,
            ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.Equal(1440, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(4, Read<int>(strategy, "RunBars"));
        Assert.Equal(14, Read<int>(strategy, "RsiPeriod"));
        Assert.Equal(5m, Read<decimal>(strategy, "RsiLow"));
        Assert.Equal(95m, Read<decimal>(strategy, "RsiHigh"));
        Assert.Equal(100, Read<int>(strategy, "TrendBars"));
        Assert.Equal(1, Read<int>(strategy, "ExitOnFirstOppositeClose"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
        Assert.True(strategy.RequiredCandles >= 141);
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestRun strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Chiusure alternate 100, 101, 100, ...: nessuna serie.</summary>
    private static OhlcvData[] Alternating(int count) =>
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), index % 2 == 0 ? 100m : 101m)).ToArray();

    private static OhlcvData[] Constant(int count, decimal close) =>
        Enumerable.Range(0, count).Select(index => Bar(Start.AddHours(index), close)).ToArray();

    /// <summary>Sostituisce le ultime chiusure della serie con <paramref name="closes"/>.</summary>
    private static OhlcvData[] WithTail(OhlcvData[] bars, params decimal[] closes)
    {
        for (var offset = 0; offset < closes.Length; offset++)
        {
            var index = bars.Length - closes.Length + offset;
            bars[index] = Bar(bars[index].DateTime, closes[offset]);
        }

        return bars;
    }

    private static OhlcvData Bar(DateTime time, decimal close) =>
        new() { DateTime = time, Open = close, High = close + 1m, Low = close - 1m, Close = close, Volume = 1m };

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>RUN di prova su NQ a 60 minuti, serie di tre chiusure. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestRun : RunOfClosesEngine
    {
        public TestRun()
        {
            IntradayOnly = false;
            RunBars = 3;
        }

        public bool RsiMode { set => Mode = value ? 1 : 0; }
        public int ModeValue { set => Mode = value; }
        public int RunValue { set => RunBars = value; }
        public decimal RsiLowValue { set => RsiLow = value; }
        public decimal RsiHighValue { set => RsiHigh = value; }
        public int Trend { set => TrendBars = value; }
        public bool OppositeCloseExit { set => ExitOnFirstOppositeClose = value ? 1 : 0; }
        public int Side { set => Direction = value; }

        public override string Name => "RUN_TEST";
        public override string Description => "RUN di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
