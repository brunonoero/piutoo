using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.OpeningStudy.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests.OpeningStudy;

/// <summary>
/// Il motore ORB dello studio dell'apertura, su NQ a 15 minuti con tick di 0,25 e apertura alle 08:30 di
/// Chicago (le 09:30 di New York). Lo sfondo e' piatto a 105 (104,5-105,5); il range di 30 minuti sono le
/// due barre dell'apertura, 102-110 e 100-108, quindi massimo 110 e minimo 100.
/// </summary>
public sealed class OpeningRangeEngineTests
{
    // Lunedi' 8 gennaio 2024: Chicago a UTC-6, l'apertura e' alle 14:30 UTC.
    private static readonly DateTime WinterOpen = Utc(2024, 1, 8, 14, 30);

    /// <summary>Sull'ultima barra del range nascono i due stop, validi dalla barra che chiude il range.</summary>
    [Fact]
    public void AtTheEndOfTheRangeBothStopsAreArmed()
    {
        var bars = WithRange(EndingAt(WinterOpen.AddMinutes(15)), WinterOpen);

        var signal = Evaluate(new TestOrb(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Stop, signal.OrderType);
        Assert.Equal(110m, signal.Price);
        Assert.Equal(WinterOpen.AddMinutes(30), signal.ValidFromUtc);
        var companion = Assert.Single(signal.CompanionSignals!);
        Assert.Equal(SignalType.Sell, companion.Type);
        Assert.Equal(100m, companion.Price);
    }

    /// <summary>
    /// L'apertura segue l'ora di borsa: l'11 marzo 2024 Chicago e' gia' in ora legale (UTC-5) e Roma no, e
    /// New York apre alle 13:30 UTC — le 14:30 di Roma, non le 15:30.
    /// </summary>
    [Fact]
    public void InTheMismatchedDaylightWeekTheOpenFollowsChicago()
    {
        var open = Utc(2024, 3, 11, 13, 30);
        var bars = WithRange(EndingAt(open.AddMinutes(15)), open);

        var signal = Evaluate(new TestOrb(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(open.AddMinutes(30), signal.ValidFromUtc);
    }

    /// <summary>Prima che il range sia chiuso e dopo la finestra d'ingresso non nasce nulla.</summary>
    [Fact]
    public void OutsideTheEntryWindowNothingIsArmed()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestOrb(), WithRange(EndingAt(WinterOpen), WinterOpen), null).Type);

        // Finestra di 120 minuti dopo le 15:00 UTC: la barra delle 16:45 arma la 17:00, che ne e' fuori.
        Assert.NotEqual(SignalType.Hold, Evaluate(new TestOrb(), WithRange(EndingAt(Utc(2024, 1, 8, 16, 30)), WinterOpen), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestOrb(), WithRange(EndingAt(Utc(2024, 1, 8, 16, 45)), WinterOpen), null).Type);
    }

    /// <summary>Un range con una barra mancante non e' un range: il giorno non opera.</summary>
    [Fact]
    public void ARangeWithAMissingBarIsNotARange()
    {
        var bars = WithRange(EndingAt(WinterOpen.AddMinutes(15)), WinterOpen)
            .Where(bar => bar.DateTime != WinterOpen).ToArray();

        Assert.Equal(SignalType.Hold, Evaluate(new TestOrb(), bars, null).Type);
    }

    /// <summary>
    /// Un lato gia' toccato dopo il range e' speso: la barra delle 15:00 sale a 111 e richiude a 109, e
    /// sulla barra dopo resta armato il solo short.
    /// </summary>
    [Fact]
    public void ASideAlreadyBrokenIsSpent()
    {
        var bars = WithBreakAndReentry();

        var signal = Evaluate(new TestOrb(), bars, null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(100m, signal.Price);
        Assert.Null(signal.CompanionSignals);
    }

    /// <summary>
    /// Il fade: la stessa barra che buca il massimo e richiude dentro e' uno short a mercato, con lo stop
    /// sulla struttura oltre l'estremo: 111 − 109 = 2 punti, 40 dollari per contratto NQ.
    /// </summary>
    [Fact]
    public void TheFadeSellsTheReentryWithTheStopBeyondTheExtreme()
    {
        var signal = Evaluate(new TestOrb { Fade = true, Structure = true }, WithBreakAndReentry(), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(40m, signal.StopLossMoneyPerFutureContract);
    }

    /// <summary>Il fade non guarda dentro il range: sull'ultima barra del range non nasce nulla.</summary>
    [Fact]
    public void TheFadeIgnoresTheRangeItself()
    {
        var bars = WithRange(EndingAt(WinterOpen.AddMinutes(15)), WinterOpen);

        Assert.Equal(SignalType.Hold, Evaluate(new TestOrb { Fade = true }, bars, null).Type);
    }

    /// <summary>Il breakout con stop sulla struttura mette lo stop sul lato opposto: 10 punti, 200 dollari; target 1 range.</summary>
    [Fact]
    public void TheStructureStopIsTheOppositeSideOfTheRange()
    {
        var bars = WithRange(EndingAt(WinterOpen.AddMinutes(15)), WinterOpen);

        var signal = Evaluate(new TestOrb { Structure = true, Target = 1m }, bars, null);

        Assert.Equal(200m, signal.StopLossMoneyPerFutureContract);
        Assert.Equal(200m, signal.TakeProfitMoneyPerFutureContract);
    }

    /// <summary>Il lato dichiarato filtra, e in posizione non nasce nulla.</summary>
    [Fact]
    public void DirectionAndPositionAreRespected()
    {
        var bars = WithRange(EndingAt(WinterOpen.AddMinutes(15)), WinterOpen);

        var shortOnly = Evaluate(new TestOrb { Side = 2 }, bars, null);
        Assert.Equal(SignalType.Sell, shortOnly.Type);
        Assert.Null(shortOnly.CompanionSignals);

        Assert.Equal(SignalType.Hold, Evaluate(new TestOrb(), bars, SignalType.Buy).Type);
    }

    /// <summary>Un range che non e' multiplo del timeframe, o un timeframe oltre l'ora, e' una configurazione sbagliata.</summary>
    [Fact]
    public void AnIncoherentConfigurationIsRejected()
    {
        var bars = WithRange(EndingAt(WinterOpen.AddMinutes(15)), WinterOpen);

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestOrb { Range = 20 }.GenerateSignal(bars, bars[^1].DateTime));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestOrb { Open = 835 }.GenerateSignal(bars, bars[^1].DateTime));
    }

    /// <summary>Il contenitore legge le leve proprie, l'apertura come HHMM della griglia e l'orologio come numero.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_ORB();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 15,
            ["OpenHhmm"] = 830, ["OpenClock"] = 1, ["RangeMinutes"] = 15, ["EntryWindowMinutes"] = 60,
            ["Mode"] = 1, ["MaxRangeAtr"] = 0.5m, ["StructureStop"] = 1, ["TargetRange"] = 2m, ["Direction"] = 2,
            ["StopAtr"] = 1m, ["IntradayOnly"] = 1, ["ExitHour"] = 21, ["StartHour"] = -1, ["EndHour"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(new TimeOnly(8, 30), Read<TimeOnly>(strategy, "OpenTime"));
        Assert.Equal(InstrumentClock.Exchange, Read<InstrumentClock>(strategy, "OpenClock"));
        Assert.Equal(15, Read<int>(strategy, "RangeMinutes"));
        Assert.Equal(60, Read<int>(strategy, "EntryWindowMinutes"));
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(0.25m, Read<decimal>(strategy, "TickSize"));
    }

    /// <summary>
    /// La bandiera con finestra: la finestra e' in ora di borsa e sopravvive a <c>StartHour</c>/<c>EndHour</c>
    /// spenti, che la griglia passa sempre; accesi insieme a lei sono un errore, come una finestra a meta'.
    /// </summary>
    [Fact]
    public void TheFlagWindowContainerDeclaresAnExchangeWindow()
    {
        var strategy = new RC_FLGW();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 15,
            ["WindowStartHhmm"] = 830, ["WindowEndHhmm"] = 1030, ["StartHour"] = -1, ["EndHour"] = -1
        });

        Assert.Equal(ZonedWindow.Exchange(new TimeOnly(8, 30), new TimeOnly(10, 30)), strategy.TradingWindow);

        Assert.Throws<ArgumentException>(() => new RC_FLGW().Initialize(new Dictionary<string, object>
        {
            ["WindowStartHhmm"] = 830, ["WindowEndHhmm"] = 1030, ["StartHour"] = 9
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RC_FLGW().Initialize(new Dictionary<string, object>
        {
            ["WindowStartHhmm"] = 830
        }));

        var withoutWindow = new RC_FLGW();
        withoutWindow.Initialize(new Dictionary<string, object> { ["StartHour"] = -1, ["EndHour"] = -1 });
        Assert.True(withoutWindow.TradingWindow!.IsAllDay);
    }

    // ------------------------------------------------------------------ supporto

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static TradeSignal Evaluate(TestOrb strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>Settecento barre da 15 minuti, piatte a 105, che finiscono con quella che apre in <paramref name="lastOpenUtc"/>.</summary>
    private static OhlcvData[] EndingAt(DateTime lastOpenUtc) =>
        Enumerable.Range(0, 700).Select(index => new OhlcvData
        {
            DateTime = lastOpenUtc.AddMinutes(15 * (index - 699)),
            Open = 105m, High = 105.5m, Low = 104.5m, Close = 105m, Volume = 1m
        }).ToArray();

    /// <summary>Le due barre del range che comincia in <paramref name="open"/>: 102-110 e 100-108.</summary>
    private static OhlcvData[] WithRange(OhlcvData[] bars, DateTime open)
    {
        for (var index = 0; index < bars.Length; index++)
        {
            if (bars[index].DateTime == open)
                bars[index] = new OhlcvData { DateTime = open, Open = 105m, High = 110m, Low = 102m, Close = 104m, Volume = 1m };
            else if (bars[index].DateTime == open.AddMinutes(15))
                bars[index] = new OhlcvData { DateTime = open.AddMinutes(15), Open = 104m, High = 108m, Low = 100m, Close = 105m, Volume = 1m };
        }

        return bars;
    }

    /// <summary>Il range, poi la barra delle 15:00 UTC che buca il massimo a 111 e richiude a 109.</summary>
    private static OhlcvData[] WithBreakAndReentry()
    {
        var bars = WithRange(EndingAt(WinterOpen.AddMinutes(30)), WinterOpen);
        bars[^1] = new OhlcvData { DateTime = WinterOpen.AddMinutes(30), Open = 106m, High = 111m, Low = 105m, Close = 109m, Volume = 1m };
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

    /// <summary>ORB di prova su NQ a 15 minuti: 08:30 Chicago, range 30, finestra 120, multiday.</summary>
    private sealed class TestOrb : OpeningRangeEngine
    {
        public TestOrb()
        {
            IntradayOnly = false;
            OpenTime = new TimeOnly(8, 30);
            OpenClock = InstrumentClock.Exchange;
            RangeMinutes = 30;
            EntryWindowMinutes = 120;
            TickSize = 0.25m;
        }

        public int Open { set => OpenHhmm = value; }
        public int Range { set => RangeMinutes = value; }
        public int Side { set => Direction = value; }
        public bool Fade { set => Mode = value ? 1 : 0; }
        public bool Structure { set => StructureStop = value ? 1 : 0; }
        public decimal Target { set => TargetRange = value; }

        public override string Name => "ORB_TEST";
        public override string Description => "ORB di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 15;
    }
}
