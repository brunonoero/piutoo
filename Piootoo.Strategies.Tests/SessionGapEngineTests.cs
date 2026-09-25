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
/// Il motore GAP (gap di apertura di sessione) della serie PT6EXO, su NQ a 60 minuti. Le serie di prova
/// sono piatte — alto 101, basso 99, chiusura 100 — quindi ogni sessione chiusa ha un vero range di 2
/// punti, l'ATR vale 2 e con il default di 0,3 ATR il gap minimo e' 0,6. L'ultima barra apre la sessione
/// di giovedi' 11 gennaio 2024: la mezzanotte di Roma, le 23:00 UTC del 10.
/// </summary>
public sealed class SessionGapEngineTests
{
    private static readonly DateTime SessionOpen = new(2024, 1, 10, 23, 0, 0, DateTimeKind.Utc);

    /// <summary>Gap al rialzo di 2 punti, fade: short a mercato sulla barra dopo.</summary>
    [Fact]
    public void AGapUpIsFadedWithAShort()
    {
        var bars = WithLast(SessionOpen, open: 102m, high: 102.5m, low: 101.2m, close: 101.5m);

        var signal = Evaluate(new TestGap(), bars, null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(SessionOpen.AddHours(1), signal.ValidFromUtc);
    }

    /// <summary>Lo specchio: gap al ribasso, fade long.</summary>
    [Fact]
    public void AGapDownIsFadedWithALong()
    {
        var bars = WithLast(SessionOpen, open: 98m, high: 98.8m, low: 97.5m, close: 98.5m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestGap(), bars, null).Type);
    }

    /// <summary>In go si entra a favore del gap.</summary>
    [Fact]
    public void InGoModeTheGapIsFollowed()
    {
        var up = WithLast(SessionOpen, open: 102m, high: 102.5m, low: 101.2m, close: 101.5m);
        var down = WithLast(SessionOpen, open: 98m, high: 98.8m, low: 97.5m, close: 98.5m);

        Assert.Equal(SignalType.Buy, Evaluate(new TestGap { GapMode = 1 }, up, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestGap { GapMode = 1 }, down, null).Type);
    }

    /// <summary>Un gap di 0,4 punti sta sotto 0,3 ATR (0,6): niente. Con 0,1 ATR (0,2) basta.</summary>
    [Fact]
    public void AGapBelowTheAtrThresholdIsIgnored()
    {
        var bars = WithLast(SessionOpen, open: 100.4m, high: 100.8m, low: 100m, close: 100.5m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestGap(), bars, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestGap { Threshold = 0.1m }, bars, null).Type);
    }

    /// <summary>Il gap si guarda solo sulla prima barra della sessione: la seconda, anche lontana da ieri, non emette.</summary>
    [Fact]
    public void OnlyTheFirstBarOfTheSessionIsASignal()
    {
        var bars = WithLast(SessionOpen.AddHours(1), open: 102m, high: 102.5m, low: 101.2m, close: 101.5m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestGap(), bars, null).Type);
    }

    /// <summary>
    /// Il target al riempimento del gap: chiusura della barra 101,5, chiusura di ieri 100, un punto e
    /// mezzo per il valore del punto di NQ. Lo specchio al ribasso da' la stessa distanza.
    /// </summary>
    [Fact]
    public void TheTargetIsTheGapFillFromTheSignalClose()
    {
        var up = WithLast(SessionOpen, open: 102m, high: 102.5m, low: 101.2m, close: 101.5m);
        var down = WithLast(SessionOpen, open: 98m, high: 98.8m, low: 97.5m, close: 98.5m);

        var shortSignal = Evaluate(new TestGap { FillTarget = 1 }, up, null);
        var longSignal = Evaluate(new TestGap { FillTarget = 1 }, down, null);

        Assert.Equal(SignalType.Sell, shortSignal.Type);
        Assert.Equal(1.5m * InstrumentRegistry.PointValue("@NQ"), shortSignal.TakeProfitMoneyPerFutureContract);
        Assert.Equal(SignalType.Buy, longSignal.Type);
        Assert.Equal(1.5m * InstrumentRegistry.PointValue("@NQ"), longSignal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>
    /// Un gap che la prima barra ha gia' chiuso (chiusura 99,8 sotto i 100 di ieri): con il target al
    /// riempimento non c'e' niente da sfumare; senza, il fade entra lo stesso.
    /// </summary>
    [Fact]
    public void AGapAlreadyFilledHasNoTargetToReach()
    {
        var bars = WithLast(SessionOpen, open: 102m, high: 102m, low: 99.5m, close: 99.8m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestGap { FillTarget = 1 }, bars, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestGap(), bars, null).Type);
    }

    /// <summary>Con un lato dichiarato l'altro non nasce.</summary>
    [Fact]
    public void ADeclaredDirectionBlocksTheOtherSide()
    {
        var bars = WithLast(SessionOpen, open: 102m, high: 102.5m, low: 101.2m, close: 101.5m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestGap { Side = 1 }, bars, null).Type);
        Assert.Equal(SignalType.Sell, Evaluate(new TestGap { Side = 2 }, bars, null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        var bars = WithLast(SessionOpen, open: 102m, high: 102.5m, low: 101.2m, close: 101.5m);

        Assert.Equal(SignalType.Hold, Evaluate(new TestGap(), bars, SignalType.Sell).Type);
    }

    /// <summary>Configurazioni incoerenti: modo sconosciuto, soglia negativa, target al gap in go.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        var bars = WithLast(SessionOpen, open: 102m, high: 102.5m, low: 101.2m, close: 101.5m);

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestGap { GapMode = 2 }.GenerateSignal(bars, SessionOpen));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestGap { Threshold = -1m }.GenerateSignal(bars, SessionOpen));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestGap { GapMode = 1, FillTarget = 1 }.GenerateSignal(bars, SessionOpen));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni della griglia.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_GAP();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "FDAX", ["TimeframeMinutes"] = 60,
            ["GapAtr"] = 0.5m, ["Mode"] = 0, ["TargetAtGapFill"] = 1, ["Direction"] = 1,
            ["StopAtr"] = 1m, ["IntradayOnly"] = 1, ["ExitHour"] = 17,
            ["StartHour"] = -1, ["EndHour"] = -1, ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@FDAX", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(0.5m, Read<decimal>(strategy, "GapAtr"));
        Assert.Equal(0, Read<int>(strategy, "Mode"));
        Assert.Equal(1, Read<int>(strategy, "TargetAtGapFill"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
    }

    // ------------------------------------------------------------------ supporto

    private static TradeSignal Evaluate(TestGap strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Cinquecento barre orarie piatte che finiscono con quella indicata, che apre in <paramref name="lastOpenUtc"/>.</summary>
    private static OhlcvData[] WithLast(DateTime lastOpenUtc, decimal open, decimal high, decimal low, decimal close)
    {
        var bars = Enumerable.Range(0, 500).Select(index => new OhlcvData
        {
            DateTime = lastOpenUtc.AddHours(index - 499),
            Open = 100m, High = 101m, Low = 99m, Close = 100m, Volume = 1m
        }).ToArray();

        bars[^1] = new OhlcvData { DateTime = lastOpenUtc, Open = open, High = high, Low = low, Close = close, Volume = 1m };
        return bars;
    }

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>GAP di prova su NQ a 60 minuti: fade, soglia 0,3 ATR, multiday. Le proprieta' scrivono i campi del motore.</summary>
    private sealed class TestGap : SessionGapEngine
    {
        public TestGap() => IntradayOnly = false;

        public int GapMode { set => Mode = value; }
        public decimal Threshold { set => GapAtr = value; }
        public int FillTarget { set => TargetAtGapFill = value; }
        public int Side { set => Direction = value; }

        public override string Name => "GAP_TEST";
        public override string Description => "GAP di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
