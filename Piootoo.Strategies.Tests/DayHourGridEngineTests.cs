using System.Reflection;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Strategies.PT6EXOStrategies.Engines;
using Piootoo.Strategies.ResearchContainers;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il motore DXH (griglia giorno × ora) della serie PT6EXO, su NQ a 60 minuti, con la tabella
/// <c>0-10:L;2-15:S;4-20:S</c>: long il lunedi' alle 10, short il mercoledi' alle 15 e il venerdi' alle
/// 20, ore di Roma. Come per HOD, il punto che conta e' che giorno e ora si leggano nel fuso dichiarato:
/// le stesse ore locali cadono su istanti UTC diversi d'inverno e d'estate.
/// </summary>
public sealed class DayHourGridEngineTests
{
    private const string Table = "0-10:L;2-15:S;4-20:S";

    /// <summary>
    /// Lunedi' 8 gennaio 2024, Roma a UTC+1: le 10:00 sono le 09:00 UTC. L'ordine nasce sulla barra
    /// delle 08:00, a mercato sulla barra dopo, e chiude un'ora dopo.
    /// </summary>
    [Fact]
    public void OnMondayAtTenRomeTheEngineBuys()
    {
        var signal = Evaluate(new TestDxh(), EndingAt(Utc(2024, 1, 8, 8)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(Utc(2024, 1, 8, 9), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 1, 8, 10), signal.CloseAtUtc);
    }

    /// <summary>Mercoledi' 10 luglio 2024, Roma a UTC+2: le 15:00 sono le 13:00 UTC, e la regola e' short.</summary>
    [Fact]
    public void InSummerTheWednesdayRuleIsTwoHoursBeforeInUtc()
    {
        var signal = Evaluate(new TestDxh(), EndingAt(Utc(2024, 7, 10, 12)), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(Utc(2024, 7, 10, 13), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 7, 10, 14), signal.CloseAtUtc);
    }

    /// <summary>Venerdi' 12 gennaio 2024 alle 20:00 di Roma (19:00 UTC, le 13:00 a Chicago): short.</summary>
    [Fact]
    public void TheFridayEveningRuleSells()
    {
        var signal = Evaluate(new TestDxh(), EndingAt(Utc(2024, 1, 12, 18)), null);

        Assert.Equal(SignalType.Sell, signal.Type);
        Assert.Equal(Utc(2024, 1, 12, 19), signal.ValidFromUtc);
    }

    /// <summary>Un'ora senza regola, o l'ora giusta nel giorno sbagliato, non e' un segnale.</summary>
    [Fact]
    public void CellsWithoutARuleAreNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestDxh(), EndingAt(Utc(2024, 1, 8, 9)), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestDxh(), EndingAt(Utc(2024, 1, 8, 7)), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestDxh(), EndingAt(Utc(2024, 1, 9, 8)), null).Type);
    }

    /// <summary>Nell'ora di borsa di NQ (Chicago) le 09:00 di lunedi' 8 gennaio sono le 15:00 UTC.</summary>
    [Fact]
    public void TheExchangeClockReadsDayAndHourInChicago()
    {
        var chicago = new SessionClock(MarketCalendarRegistry.Current.Get("@NQ").ExchangeTimeZone);
        var entry = chicago.ToUtc(new DateTime(2024, 1, 8, 9, 0, 0));
        Assert.Equal(Utc(2024, 1, 8, 15), entry);

        var signal = Evaluate(new TestDxh { RulesValue = "0-9:L", Exchange = true }, EndingAt(entry.AddHours(-1)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(entry, signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 1, 8, 16), signal.CloseAtUtc);
    }

    /// <summary>La stringa vuota e' "nessuna regola": il motore non entra mai, senza errori.</summary>
    [Fact]
    public void AnEmptyTableNeverEnters()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestDxh { RulesValue = string.Empty }, EndingAt(Utc(2024, 1, 8, 8)), null).Type);
    }

    /// <summary>Spazi, minuscole e un punto e virgola finale sono ammessi.</summary>
    [Fact]
    public void TheParserToleratesSpacesCaseAndATrailingSeparator()
    {
        var table = DayHourGridEngine.ParseRules(" 0-10:l ; 2-15:S ;");

        Assert.Equal(1, table[0, 10]);
        Assert.Equal(-1, table[2, 15]);
        Assert.Equal(0, table[4, 20]);
    }

    /// <summary>Un elemento malformato, fuori intervallo o ripetuto ferma tutto, e il messaggio lo nomina.</summary>
    [Theory]
    [InlineData("0-10:X", "0-10:X")]
    [InlineData("5-10:L", "5-10:L")]
    [InlineData("0-24:L", "0-24:L")]
    [InlineData("0-10L", "0-10L")]
    [InlineData("010:L", "010:L")]
    [InlineData("a-10:L", "a-10:L")]
    [InlineData("-1-10:L", "-1-10:L")]
    [InlineData("0-10:L;0-10:S", "0-10:S")]
    public void AMalformedRuleIsRejectedByName(string rules, string element)
    {
        var error = Assert.Throws<ArgumentException>(() => DayHourGridEngine.ParseRules(rules));

        Assert.Contains($"'{element}'", error.Message);
    }

    /// <summary>La stessa tabella malformata ferma anche la valutazione, non solo il parser.</summary>
    [Fact]
    public void AMalformedTableStopsTheEvaluation()
    {
        var strategy = new TestDxh { RulesValue = "0-10:L;2-15:Z" };

        Assert.Throws<ArgumentException>(() => strategy.GenerateSignal(EndingAt(Utc(2024, 1, 8, 8)), Utc(2024, 1, 8, 8)));
    }

    /// <summary>La tabella in cache si ricostruisce quando la stringa cambia: non resta quella vecchia.</summary>
    [Fact]
    public void TheCachedTableFollowsTheRules()
    {
        var strategy = new TestDxh();
        var bars = EndingAt(Utc(2024, 1, 8, 8));

        Assert.Equal(SignalType.Buy, strategy.GenerateSignal(bars, bars[^1].DateTime).Type);

        strategy.RulesValue = "0-10:S";
        Assert.Equal(SignalType.Sell, strategy.GenerateSignal(bars, bars[^1].DateTime).Type);
    }

    /// <summary>L'uscita di sessione piu' stretta vince sulla tenuta: chiude alle 10:30 di Roma, non alle 11.</summary>
    [Fact]
    public void AnEarlierSessionExitWinsOverTheHolding()
    {
        var signal = Evaluate(new TestDxh { SessionExitAtHalfPastTen = true }, EndingAt(Utc(2024, 1, 8, 8)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(new DateTime(2024, 1, 8, 9, 30, 0, DateTimeKind.Utc), signal.CloseAtUtc);
    }

    /// <summary>Senza tenuta propria non c'e' scadenza; una tenuta negativa e' un errore.</summary>
    [Fact]
    public void HoldHoursZeroMeansNoTimeExitAndNegativeIsRejected()
    {
        var signal = Evaluate(new TestDxh { Hold = 0 }, EndingAt(Utc(2024, 1, 8, 8)), null);
        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Null(signal.CloseAtUtc);

        var strategy = new TestDxh { Hold = -1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateSignal(EndingAt(Utc(2024, 1, 8, 8)), Utc(2024, 1, 8, 8)));
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestDxh(), EndingAt(Utc(2024, 1, 8, 8)), SignalType.Buy).Type);
    }

    /// <summary>Il contenitore legge la tabella come stringa, la tenuta e l'orologio come numero.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_DXH();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60,
            ["Rules"] = "0-10:L;4-20:S", ["HoldHours"] = 3, ["ScheduleClock"] = 1,
            ["StopAtr"] = 1m, ["IntradayOnly"] = 0, ["ExitHour"] = -1,
            ["StartHour"] = -1, ["EndHour"] = -1, ["PtnNeutYes"] = 55, ["SkipDay"] = -1
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal("0-10:L;4-20:S", Read<string>(strategy, "Rules"));
        Assert.Equal(3, Read<int>(strategy, "HoldHours"));
        Assert.Equal(InstrumentClock.Exchange, Read<InstrumentClock>(strategy, "ScheduleClock"));
    }

    // ------------------------------------------------------------------ supporto

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static TradeSignal Evaluate(TestDxh strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>DXH di prova su NQ a 60 minuti con la tabella di esempio, ore di Roma, un'ora di tenuta, multiday.</summary>
    private sealed class TestDxh : DayHourGridEngine
    {
        public TestDxh()
        {
            IntradayOnly = false;
            Rules = Table;
        }

        public string RulesValue { set => Rules = value; }
        public int Hold { set => HoldHours = value; }
        public bool Exchange { set => ScheduleClock = value ? InstrumentClock.Exchange : InstrumentClock.Research; }

        public bool SessionExitAtHalfPastTen
        {
            set
            {
                if (!value) return;
                IntradayOnly = true;
                SessionExitTime = new TimeOnly(10, 30);
            }
        }

        public override string Name => "DXH_TEST";
        public override string Description => "DXH di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
