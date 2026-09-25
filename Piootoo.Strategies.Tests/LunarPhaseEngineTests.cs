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
/// Il motore LUN (fasi lunari) della serie PT6EXO, su NQ a 60 minuti. I confini attesi sono calcolati a
/// mano dalla lunazione media: 297 mesi sinodici dopo il 06/01/2000 18:14 UTC sono 8.770,584889341
/// giorni, cioe' la luna nuova del <b>giovedi' 11/01/2024 alle 08:16:14 UTC</b>; mezzo mese dopo la piena
/// di <b>venerdi' 26/01/2024 02:38:16</b>, poi la nuova di <b>venerdi' 09/02/2024 21:00:17</b> e la
/// piena di <b>sabato 24/02/2024 15:22:18</b>.
/// </summary>
public sealed class LunarPhaseEngineTests
{
    /// <summary>La fase e' 0 alla luna nuova di riferimento e 0,5 esatto mezzo mese dopo.</summary>
    [Fact]
    public void ThePhaseIsZeroAtTheReferenceAndHalfAtTheNextFullMoon()
    {
        Assert.Equal(0.0, LunarPhaseEngine.Phase(LunarPhaseEngine.ReferenceNewMoonUtc));
        Assert.Equal(0.5, LunarPhaseEngine.Phase(LunarPhaseEngine.BoundaryUtc(1)));
        Assert.Equal(0.0, LunarPhaseEngine.Phase(LunarPhaseEngine.BoundaryUtc(594)));
    }

    /// <summary>
    /// Il mese sinodico in tick e' esatto e pari: 29,530588853 giorni sono un numero intero di tick, e
    /// il mezzo mese pure. E' cio' che rende i confini istanti esatti su ogni macchina.
    /// </summary>
    [Fact]
    public void TheSynodicMonthIsAnExactNumberOfTicks()
    {
        Assert.Equal(LunarPhaseEngine.SynodicMonthTicks,
            (long)Math.Round(LunarPhaseEngine.SynodicMonthDays * TimeSpan.TicksPerDay));
        Assert.Equal(LunarPhaseEngine.SynodicMonthTicks, 2 * LunarPhaseEngine.HalfCycleTicks);
    }

    /// <summary>I confini calcolati a mano: la luna nuova di gennaio 2024 e la piena che la segue.</summary>
    [Fact]
    public void TheBoundariesMatchTheHandCalculation()
    {
        var newMoon = LunarPhaseEngine.BoundaryUtc(594);
        Assert.InRange(newMoon, Utc(2024, 1, 11, 8, 16, 14), Utc(2024, 1, 11, 8, 16, 15));

        var fullMoon = LunarPhaseEngine.BoundaryUtc(595);
        Assert.InRange(fullMoon, Utc(2024, 1, 26, 2, 38, 15), Utc(2024, 1, 26, 2, 38, 16));

        Assert.Equal(594L, LunarPhaseEngine.HalfCycleIndex(newMoon));
        Assert.Equal(593L, LunarPhaseEngine.HalfCycleIndex(newMoon.AddTicks(-1)));
    }

    /// <summary>
    /// La luna nuova delle 08:16 cade nella barra delle 08:00: l'ordine nasce su quella barra ed entra a
    /// mercato alle 09:00, la prima apertura del mezzo ciclo nuovo. In modo 0 e' un long, che chiude
    /// all'apertura della barra che contiene la piena successiva, le 02:00 del 26/01.
    /// </summary>
    [Fact]
    public void AtTheNewMoonTheEngineBuysUntilTheFullMoon()
    {
        var signal = Evaluate(new TestLun(), EndingAt(Utc(2024, 1, 11, 8)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(Utc(2024, 1, 11, 9), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 1, 26, 2), signal.CloseAtUtc);
    }

    /// <summary>
    /// Alla piena delle 02:38 del 26/01 si vende fino alla luna nuova del 09/02, che cade alle 21:00:17:
    /// la chiusura e' all'apertura della barra delle 21:00.
    /// </summary>
    [Fact]
    public void AtTheFullMoonTheEngineSellsUntilTheNewMoon()
    {
        var signal = Evaluate(new TestLun(), EndingAt(Utc(2024, 1, 26, 2)), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(Utc(2024, 1, 26, 3), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 2, 9, 21), signal.CloseAtUtc);
    }

    /// <summary>Le barre che non contengono il confine non sono un segnale: nemmeno quella subito dopo.</summary>
    [Fact]
    public void OnlyTheBarBeforeTheBoundaryIsASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestLun(), EndingAt(Utc(2024, 1, 11, 7)), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestLun(), EndingAt(Utc(2024, 1, 11, 9)), null).Type);
    }

    /// <summary>
    /// La piena di sabato 24/02 cade in un giorno senza sessione: il mezzo ciclo si salta, e non si
    /// recupera alla prima barra vera della domenica sera.
    /// </summary>
    [Fact]
    public void ABoundaryWithoutABarIsSkipped()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestLun(), EndingAt(Utc(2024, 2, 24, 15)), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestLun(), EndingAt(Utc(2024, 2, 25, 23)), null).Type);
    }

    /// <summary>Il modo 1 inverte i versi; un lato dichiarato tiene solo quel lato.</summary>
    [Theory]
    [InlineData(1, 0, SignalType.Sell)]
    [InlineData(0, 1, SignalType.Buy)]
    [InlineData(0, 2, SignalType.Hold)]
    [InlineData(1, 2, SignalType.Sell)]
    public void ModeAndDirectionChooseTheSide(int mode, int direction, SignalType expected)
    {
        var strategy = new TestLun { ModeValue = mode, DirectionValue = direction };

        Assert.Equal(expected, Evaluate(strategy, EndingAt(Utc(2024, 1, 11, 8)), null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestLun(), EndingAt(Utc(2024, 1, 11, 8)), SignalType.Sell).Type);
    }

    /// <summary>Modo e lato fuori intervallo sono errori di configurazione.</summary>
    [Theory]
    [InlineData(2, 0)]
    [InlineData(0, 3)]
    [InlineData(-1, 0)]
    public void AnIncoherentConfigurationIsRejected(int mode, int direction)
    {
        var strategy = new TestLun { ModeValue = mode, DirectionValue = direction };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => strategy.GenerateSignal(EndingAt(Utc(2024, 1, 11, 8)), Utc(2024, 1, 11, 8)));
    }

    /// <summary>Il contenitore nasce multiday e legge le leve proprie.</summary>
    [Fact]
    public void TheContainerIsMultidayAndTakesItsLevers()
    {
        var strategy = new RC_LUN();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60,
            ["Mode"] = 1, ["Direction"] = 2, ["StopAtr"] = 2m,
            ["StartHour"] = -1, ["EndHour"] = -1, ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.False(Read<bool>(strategy, "IntradayOnly"));
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
    }

    // ------------------------------------------------------------------ supporto

    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static TradeSignal Evaluate(TestLun strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Duecento barre orarie che finiscono con quella che apre in <paramref name="lastOpenUtc"/>.</summary>
    private static OhlcvData[] EndingAt(DateTime lastOpenUtc) =>
        Enumerable.Range(0, 200).Select(index => new OhlcvData
        {
            DateTime = lastOpenUtc.AddHours(index - 199),
            Open = 100m, High = 101m, Low = 99m, Close = 100m, Volume = 1m
        }).ToArray();

    private static T Read<T>(object instance, string name)
    {
        for (var type = instance.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null) return (T)field.GetValue(instance)!;
        }

        throw new MissingFieldException(instance.GetType().Name, name);
    }

    /// <summary>LUN di prova su NQ a 60 minuti, multiday: long dalla nuova alla piena.</summary>
    private sealed class TestLun : LunarPhaseEngine
    {
        public TestLun() => IntradayOnly = false;

        public int ModeValue { set => Mode = value; }
        public int DirectionValue { set => Direction = value; }

        public override string Name => "LUN_TEST";
        public override string Description => "LUN di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
