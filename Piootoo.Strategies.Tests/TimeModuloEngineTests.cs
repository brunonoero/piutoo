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
/// Il motore MOD (modulo del tempo) della serie PT6EXO, su NQ a 60 minuti. Gli indici attesi sono
/// calcolati a mano: lunedi' 08/01/2024 00:00 UTC e' il giorno 19.730 dall'epoca Unix, cioe' l'ora
/// 473.520, che modulo 7 da' 5. La prima barra con resto 0 e' quindi quella delle 02:00 UTC.
/// </summary>
public sealed class TimeModuloEngineTests
{
    /// <summary>L'indice e' minuti dall'epoca diviso il timeframe, e sulle 02:00 dell'8 gennaio vale 0 modulo 7.</summary>
    [Fact]
    public void TheIndexIsTimeSinceTheEpoch()
    {
        var strategy = new TestMod();

        Assert.Equal(473_522L, strategy.TimeIndex(Utc(2024, 1, 8, 2)));
        Assert.Equal(0L, strategy.TimeIndex(Utc(2024, 1, 8, 2)) % 7);
        Assert.Equal(473_522L / 4, new TestMod { Timeframe = 240 }.TimeIndex(Utc(2024, 1, 8, 2)));
    }

    /// <summary>
    /// La barra delle 01:00 chiude in rialzo e quella dopo ha resto 0: in modo 0 si compra, a mercato
    /// alle 02:00. In ribasso si vende; una barra senza corpo non ha verso.
    /// </summary>
    [Theory]
    [InlineData(105, SignalType.Buy)]
    [InlineData(95, SignalType.Sell)]
    [InlineData(100, SignalType.Hold)]
    public void OnTheModuloBarTheEngineFollowsTheClosedBar(int close, SignalType expected)
    {
        var signal = Evaluate(new TestMod(), EndingAt(Utc(2024, 1, 8, 1), close), null);

        Assert.Equal(expected, signal.Type);
        if (expected != SignalType.Hold)
        {
            Assert.Equal(TradeOrderType.Market, signal.OrderType);
            Assert.Equal(Utc(2024, 1, 8, 2), signal.ValidFromUtc);
        }
    }

    /// <summary>Il modo 1 va contro la barra chiusa; un lato dichiarato tiene solo quel lato.</summary>
    [Theory]
    [InlineData(1, 0, 105, SignalType.Sell)]
    [InlineData(1, 0, 95, SignalType.Buy)]
    [InlineData(0, 1, 95, SignalType.Hold)]
    [InlineData(0, 2, 95, SignalType.Sell)]
    public void ModeAndDirectionChooseTheSide(int mode, int direction, int close, SignalType expected)
    {
        var strategy = new TestMod { ModeValue = mode, DirectionValue = direction };

        Assert.Equal(expected, Evaluate(strategy, EndingAt(Utc(2024, 1, 8, 1), close), null).Type);
    }

    /// <summary>Le barre fuori modulo non sono un segnale; cambiando il resto scatta un'altra barra.</summary>
    [Fact]
    public void OnlyTheDeclaredRemainderIsASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestMod(), EndingAt(Utc(2024, 1, 8, 2), 105), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestMod(), EndingAt(Utc(2024, 1, 8, 0), 105), null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestMod { RemainderValue = 1 }, EndingAt(Utc(2024, 1, 8, 2), 105), null).Type);
    }

    /// <summary>
    /// L'indice non dipende da quante barre ha la storia: la stessa barra da' la stessa decisione con una
    /// finestra corta e con una lunga, come in backtest e in sessione live.
    /// </summary>
    [Fact]
    public void TheDecisionDoesNotDependOnTheHistoryLength()
    {
        var bars = EndingAt(Utc(2024, 1, 8, 1), 105);
        var strategy = new TestMod();

        Assert.Equal(SignalType.Buy, Evaluate(strategy, bars, null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(strategy, bars[^strategy.RequiredCandles..], null).Type);
    }

    /// <summary>
    /// Sabato 13/01/2024 alle 01:00 UTC l'indice ha resto 0, ma il sabato non e' un giorno di sessione di
    /// NQ: la barra su cui si entrerebbe non esiste, e non nasce nulla.
    /// </summary>
    [Fact]
    public void NoEntryIsScheduledOnABarThatDoesNotExist()
    {
        var strategy = new TestMod();
        Assert.Equal(0L, strategy.TimeIndex(Utc(2024, 1, 13, 1)) % 7);

        Assert.Equal(SignalType.Hold, Evaluate(strategy, EndingAt(Utc(2024, 1, 13, 0), 105), null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestMod(), EndingAt(Utc(2024, 1, 8, 1), 105), SignalType.Buy).Type);
    }

    /// <summary>Modulo sotto 2, resto fuori da [0, modulo), modo o lato fuori intervallo: errori di configurazione.</summary>
    [Theory]
    [InlineData(7, 7, 0, 0)]
    [InlineData(7, -1, 0, 0)]
    [InlineData(1, 0, 0, 0)]
    [InlineData(7, 0, 2, 0)]
    [InlineData(7, 0, 0, 3)]
    public void AnIncoherentConfigurationIsRejected(int modulo, int remainder, int mode, int direction)
    {
        var strategy = new TestMod
        {
            ModuloValue = modulo, RemainderValue = remainder, ModeValue = mode, DirectionValue = direction
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => strategy.GenerateSignal(EndingAt(Utc(2024, 1, 8, 1), 105), Utc(2024, 1, 8, 1)));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni della griglia.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_MOD();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 15,
            ["ModuloBars"] = 13, ["Remainder"] = 4, ["Mode"] = 1, ["Direction"] = 1,
            ["MaxBars"] = 4, ["IntradayOnly"] = 1, ["ExitHour"] = -1,
            ["StartHour"] = -1, ["EndHour"] = -1, ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.Equal(15, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(13, Read<int>(strategy, "ModuloBars"));
        Assert.Equal(4, Read<int>(strategy, "Remainder"));
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(1, Read<int>(strategy, "Direction"));
    }

    // ------------------------------------------------------------------ supporto

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static TradeSignal Evaluate(TestMod strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>
    /// Duecento barre orarie che finiscono con quella che apre in <paramref name="lastOpenUtc"/>: tutte
    /// piatte a 100, salvo l'ultima che chiude a <paramref name="lastClose"/>.
    /// </summary>
    private static OhlcvData[] EndingAt(DateTime lastOpenUtc, int lastClose)
    {
        var bars = Enumerable.Range(0, 200).Select(index => new OhlcvData
        {
            DateTime = lastOpenUtc.AddHours(index - 199),
            Open = 100m, High = 101m, Low = 99m, Close = 100m, Volume = 1m
        }).ToArray();

        bars[^1] = new OhlcvData
        {
            DateTime = lastOpenUtc, Open = 100m, High = 106m, Low = 94m, Close = lastClose, Volume = 1m
        };
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

    /// <summary>
    /// MOD di prova su NQ a 60 minuti, multiday: modulo 7, resto 0, segue la barra chiusa. Le proprieta'
    /// scrivono i campi protetti del motore, e il timeframe sta in un campo: la base clona i campi a ogni
    /// valutazione.
    /// </summary>
    private sealed class TestMod : TimeModuloEngine
    {
        public TestMod() => IntradayOnly = false;

        public int ModuloValue { set => ModuloBars = value; }
        public int RemainderValue { set => Remainder = value; }
        public int ModeValue { set => Mode = value; }
        public int DirectionValue { set => Direction = value; }
        public int Timeframe { set => _timeframe = value; }

        private int _timeframe = 60;

        public override string Name => "MOD_TEST";
        public override string Description => "MOD di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => _timeframe;
    }
}
