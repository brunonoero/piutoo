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
/// Il motore CAL (anomalie di calendario) della serie PT6EXO, su NQ a 60 minuti. NQ ha la sessione della
/// ricerca ancorata alla mezzanotte di Roma, giorni di sessione da domenica a venerdi' e la finestra di
/// negoziazione 17:00→16:00 di Chicago aperta da domenica a giovedi': l'ultima barra negoziata di una
/// sessione e' quindi quella che apre un'ora prima della chiusura di Chicago, e la domenica ha barre solo
/// nelle settimane in cui l'ora legale americana ed europea sono sfasate.
/// </summary>
public sealed class CalendarAnomalyEngineTests
{
    /// <summary>
    /// Turn-of-month, gennaio 2024 (Roma UTC+1, Chicago UTC−6). L'ultimo giorno di negoziazione e'
    /// mercoledi' 31. Il segnale nasce sull'ultima barra negoziata di martedi' 30 — le 21:00 UTC, perche'
    /// Chicago chiude alle 22:00 UTC e la barra delle 22:00 non esiste — e vale dall'apertura della
    /// sessione del 31 (00:00 di Roma, 23:00 UTC del 30). Quattro sessioni: 31, 1, 2, e — saltati sabato
    /// e una domenica senza barre — lunedi' 5; esce all'apertura di martedi' 6.
    /// </summary>
    [Fact]
    public void TheLastTradingDayOfTheMonthIsEnteredFromTheLastTradedBarBefore()
    {
        var signal = Evaluate(new TestCal(), EndingAt(Utc(2024, 1, 30, 21)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(Utc(2024, 1, 30, 23), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 1, 30, 23), signal.ExpiresAtUtc);
        Assert.Equal(Utc(2024, 2, 5, 23), signal.CloseAtUtc);
    }

    /// <summary>Una barra che non e' l'ultima negoziata della sessione non emette nulla.</summary>
    [Fact]
    public void ABarThatIsNotTheLastTradedOneIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCal(), EndingAt(Utc(2024, 1, 30, 20)), null).Type);
    }

    /// <summary>Lunedi' 29 gennaio precede martedi' 30, che non e' l'ultimo giorno del mese: niente.</summary>
    [Fact]
    public void ADayThatIsNotTheAnomalyIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCal(), EndingAt(Utc(2024, 1, 29, 21)), null).Type);
    }

    /// <summary>Con <c>DaysBeforeMonthEnd</c> = 2 il penultimo giorno, martedi' 30, e' quello dell'ingresso.</summary>
    [Fact]
    public void TheSecondToLastTradingDayCanBeChosen()
    {
        var signal = Evaluate(new TestCal { BeforeEnd = 2 }, EndingAt(Utc(2024, 1, 29, 21)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2024, 1, 29, 23), signal.ValidFromUtc);
    }

    /// <summary>
    /// L'ultima barra del venerdi'. Luglio 2023 (Roma UTC+2, Chicago UTC−5): lunedi' 31 e' l'ultimo
    /// giorno del mese, e il segnale nasce venerdi' 28 sull'ultima barra negoziata, le 20:00 UTC. Il
    /// giorno dell'ingresso si trova sui giorni di negoziazione — il sabato non e' di sessione, la domenica
    /// lo e' ma senza barre — e l'ordine vale dall'apertura del lunedi', le 22:00 UTC di domenica, non
    /// dalla proiezione di un'ora nel fine settimana. Esce all'apertura di venerdi' 4 agosto.
    /// </summary>
    [Fact]
    public void FromTheLastFridayBarTheEntryIsTheMondayOpen()
    {
        var signal = Evaluate(new TestCal(), EndingAt(Utc(2023, 7, 28, 20)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2023, 7, 30, 22), signal.ValidFromUtc);
        Assert.Equal(Utc(2023, 8, 3, 22), signal.CloseAtUtc);
    }

    /// <summary>
    /// Settimana della scadenza, gennaio 2024: il terzo venerdi' e' il 19, la settimana comincia lunedi'
    /// 15. Dal venerdi' 12 la domenica 14 non ha barre (le 17:00 di Chicago sono la mezzanotte di Roma),
    /// quindi l'ingresso e' all'apertura di lunedi' 15, le 23:00 UTC di domenica.
    /// </summary>
    [Fact]
    public void TheExpiryWeekIsEnteredAtItsFirstTradingDay()
    {
        var signal = Evaluate(new TestCal { CalendarMode = 1 }, EndingAt(Utc(2024, 1, 12, 21)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2024, 1, 14, 23), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 1, 18, 23), signal.CloseAtUtc);
    }

    /// <summary>
    /// Marzo 2024, settimana sfasata: dal 10 Chicago e' in ora legale e Roma no, quindi la domenica ha
    /// una barra (le 22:00 UTC, le 23:00 di Roma) ed e' un giorno di negoziazione. Dal venerdi' 8 il
    /// giorno dopo e' allora domenica 10, che appartiene alla settimana precedente: niente. Il segnale
    /// per lunedi' 11 — settimana del terzo venerdi', il 15 — nasce sull'unica barra della domenica.
    /// </summary>
    [Fact]
    public void InAnOffsetWeekTheSundayBarEmitsTheMondayEntry()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCal { CalendarMode = 1 }, EndingAt(Utc(2024, 3, 8, 21)), null).Type);

        var signal = Evaluate(new TestCal { CalendarMode = 1 }, EndingAt(Utc(2024, 3, 10, 22)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2024, 3, 10, 23), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 3, 14, 23), signal.CloseAtUtc);
    }

    /// <summary>
    /// La settimana dopo la scadenza (venerdi' 22 marzo) non e' la settimana della scadenza: l'unica barra
    /// della domenica 17, le 22:00 UTC, e' l'ultima negoziata ma il lunedi' 18 non e' un ingresso.
    /// </summary>
    [Fact]
    public void TheWeekAfterTheExpiryIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCal { CalendarMode = 1 }, EndingAt(Utc(2024, 3, 17, 22)), null).Type);
    }

    /// <summary>La vigilia di festivo e' inerte finche' il calendario non dichiara festivi: nessun giorno feriale manca.</summary>
    [Fact]
    public void TheHolidayEveIsInertWithoutHolidaysInTheCalendar()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCal { CalendarMode = 2 }, EndingAt(Utc(2024, 1, 30, 21)), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestCal { CalendarMode = 2 }, EndingAt(Utc(2024, 1, 12, 21)), null).Type);
    }

    /// <summary>Il verso lo dichiara Direction: 2 e' short, 0 non dice nulla ed e' un errore.</summary>
    [Fact]
    public void TheSideIsDeclared()
    {
        Assert.Equal(SignalType.Sell, Evaluate(new TestCal { Side = 2 }, EndingAt(Utc(2024, 1, 30, 21)), null).Type);

        var strategy = new TestCal { Side = 0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateSignal(EndingAt(Utc(2024, 1, 30, 21)), Utc(2024, 1, 30, 21)));
    }

    /// <summary>Configurazioni incoerenti fermano la valutazione: modo sconosciuto, tenuta nulla, ora di uscita inerte.</summary>
    [Fact]
    public void AnInconsistentConfigurationIsRejected()
    {
        var bars = EndingAt(Utc(2024, 1, 30, 21));

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestCal { CalendarMode = 3 }.GenerateSignal(bars, bars[^1].DateTime));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestCal { Hold = 0 }.GenerateSignal(bars, bars[^1].DateTime));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestCal { ExitAtNoon = true }.GenerateSignal(bars, bars[^1].DateTime));
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestCal(), EndingAt(Utc(2024, 1, 30, 21)), SignalType.Buy).Type);
    }

    /// <summary>Una tenuta di piu' sessioni e' multiday: il motore non applica l'uscita di sessione.</summary>
    [Fact]
    public void TheEngineIsMultiday()
    {
        Assert.Equal(StrategyHolding.Multiday, new TestCal { Intraday = true }.Holding);
    }

    /// <summary>
    /// Le regole di calendario con un festivo, che nessun calendario oggi dichiara: venerdi' santo
    /// (29 marzo 2024) e il Memorial Day (lunedi' 27 maggio 2024).
    /// </summary>
    [Fact]
    public void TheCalendarRulesReadHolidays()
    {
        var goodFriday = new DateTime(2024, 3, 29);
        var memorialDay = new DateTime(2024, 5, 27);
        var martinLutherKing = new DateTime(2024, 1, 15);
        Func<DateTime, bool> withHolidays = day => IsWeekday(day) && day != goodFriday && day != memorialDay && day != martinLutherKing;
        Func<DateTime, bool> weekdays = IsWeekday;

        Assert.True(TestCal.Eve(new DateTime(2024, 3, 28), withHolidays));
        Assert.False(TestCal.Eve(new DateTime(2024, 3, 27), withHolidays));
        Assert.True(TestCal.Eve(new DateTime(2024, 5, 24), withHolidays));
        Assert.False(TestCal.Eve(new DateTime(2024, 5, 24), weekdays));

        // Con il venerdi' santo l'ultimo giorno di marzo 2024 e' giovedi' 28, non venerdi' 29.
        Assert.True(TestCal.NthLast(new DateTime(2024, 3, 28), 1, withHolidays));
        Assert.False(TestCal.NthLast(new DateTime(2024, 3, 28), 1, weekdays));
        Assert.True(TestCal.NthLast(new DateTime(2024, 3, 27), 2, withHolidays));

        // Con il lunedi' festivo la settimana della scadenza di gennaio comincia martedi' 16.
        Assert.True(TestCal.FirstOfExpiryWeek(new DateTime(2024, 1, 16), withHolidays));
        Assert.False(TestCal.FirstOfExpiryWeek(new DateTime(2024, 1, 16), weekdays));
        Assert.True(TestCal.FirstOfExpiryWeek(new DateTime(2024, 1, 15), weekdays));
        Assert.False(TestCal.FirstOfExpiryWeek(new DateTime(2024, 1, 22), weekdays));
    }

    /// <summary>Il contenitore legge le leve proprie e quelle comuni al valore spento.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_CAL();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60,
            ["Mode"] = 1, ["DaysBeforeMonthEnd"] = 3, ["HoldSessions"] = 5, ["Direction"] = 2,
            ["StopAtr"] = 1m, ["ExitHour"] = -1, ["StartHour"] = -1, ["EndHour"] = -1,
            ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.Equal(60, strategy.TimeframeMinutes);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(StrategyHolding.Multiday, strategy.Holding);
        Assert.Equal(1, Read<int>(strategy, "Mode"));
        Assert.Equal(3, Read<int>(strategy, "DaysBeforeMonthEnd"));
        Assert.Equal(5, Read<int>(strategy, "HoldSessions"));
        Assert.Equal(2, Read<int>(strategy, "Direction"));
    }

    // ------------------------------------------------------------------ supporto

    private static bool IsWeekday(DateTime day) => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static TradeSignal Evaluate(TestCal strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>CAL di prova su NQ a 60 minuti: turn-of-month, ultimo giorno, quattro sessioni, long.</summary>
    private sealed class TestCal : CalendarAnomalyEngine
    {
        public TestCal() => IntradayOnly = false;

        public int CalendarMode { set => Mode = value; }
        public int BeforeEnd { set => DaysBeforeMonthEnd = value; }
        public int Hold { set => HoldSessions = value; }
        public int Side { set => Direction = value; }
        public bool Intraday { set => IntradayOnly = value; }
        public bool ExitAtNoon { set => SessionExitTime = value ? new TimeOnly(12, 0) : null; }

        public static bool Eve(DateTime day, Func<DateTime, bool> isTradingDay) =>
            PrecedesWeekdayWithoutSession(day, isTradingDay);

        public static bool NthLast(DateTime day, int n, Func<DateTime, bool> isTradingDay) =>
            IsNthLastTradingDayOfMonth(day, n, isTradingDay);

        public static bool FirstOfExpiryWeek(DateTime day, Func<DateTime, bool> isTradingDay) =>
            IsFirstTradingDayOfExpiryWeek(day, isTradingDay);

        public override string Name => "CAL_TEST";
        public override string Description => "CAL di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
