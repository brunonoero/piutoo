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
/// Il motore HOD (deriva oraria) della serie PT6EXO, su NQ a 60 minuti. Il punto che conta e' che
/// l'orario sia letto nel fuso dichiarato e mai in UTC: gli stessi orari locali cadono su istanti UTC
/// diversi d'inverno e d'estate, e l'ingresso deve seguirli.
/// </summary>
public sealed class HourOfDayEngineTests
{
    /// <summary>
    /// Lunedi' 8 gennaio 2024, Roma a UTC+1: le 10:00 della ricerca sono le 09:00 UTC. L'ordine nasce
    /// sulla barra delle 08:00 UTC, a mercato sulla barra dopo, e chiude quattro ore dopo.
    /// </summary>
    [Fact]
    public void InWinterTheEntryAtTenRomeIsAtNineUtc()
    {
        var bars = EndingAt(Utc(2024, 1, 8, 8));

        var signal = Evaluate(new TestHod(), bars, null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(Utc(2024, 1, 8, 9), signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 1, 8, 13), signal.CloseAtUtc);
    }

    /// <summary>Lunedi' 8 luglio 2024, Roma a UTC+2: le 10:00 della ricerca sono le 08:00 UTC.</summary>
    [Fact]
    public void InSummerTheEntryAtTenRomeIsAtEightUtc()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestHod(), EndingAt(Utc(2024, 7, 8, 8)), null).Type);

        var signal = Evaluate(new TestHod(), EndingAt(Utc(2024, 7, 8, 7)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2024, 7, 8, 8), signal.ValidFromUtc);
    }

    /// <summary>Fuori dall'orario non nasce nulla.</summary>
    [Fact]
    public void AnyOtherHourIsNotASignal()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestHod(), EndingAt(Utc(2024, 1, 8, 9)), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestHod(), EndingAt(Utc(2024, 1, 8, 7)), null).Type);
    }

    /// <summary>Nell'ora di borsa di NQ (Chicago) le 09:00 dell'8 gennaio sono le 15:00 UTC.</summary>
    [Fact]
    public void TheExchangeClockReadsTheHourInChicago()
    {
        var chicago = new SessionClock(MarketCalendarRegistry.Current.Get("@NQ").ExchangeTimeZone);
        var entry = chicago.ToUtc(new DateTime(2024, 1, 8, 9, 0, 0));

        var signal = Evaluate(new TestHod { Hour = 9, Exchange = true }, EndingAt(entry.AddHours(-1)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(entry, signal.ValidFromUtc);
        Assert.Equal(Utc(2024, 1, 8, 15), entry);
    }

    /// <summary>
    /// Una barra pianificata che non esiste non e' un ingresso: il sabato non e' un giorno di sessione,
    /// e la domenica mattina lo e' ma il future apre solo la sera.
    /// </summary>
    [Fact]
    public void NoEntryIsScheduledOnABarThatDoesNotExist()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestHod(), EndingAt(Utc(2024, 1, 13, 8)), null).Type);
        Assert.Equal(SignalType.Hold, Evaluate(new TestHod(), EndingAt(Utc(2024, 1, 14, 8)), null).Type);
    }

    /// <summary>L'uscita di sessione piu' stretta vince sulla tenuta: chiude alle 12 di Roma, non alle 14.</summary>
    [Fact]
    public void AnEarlierSessionExitWinsOverTheHolding()
    {
        var signal = Evaluate(new TestHod { SessionExitAtNoon = true }, EndingAt(Utc(2024, 1, 8, 8)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2024, 1, 8, 11), signal.CloseAtUtc);
    }

    /// <summary>Senza tenuta propria e senza uscita di sessione non c'e' una scadenza: restano le uscite comuni.</summary>
    [Fact]
    public void WithoutHoldHoursThereIsNoTimeExit()
    {
        var signal = Evaluate(new TestHod { Hold = 0 }, EndingAt(Utc(2024, 1, 8, 8)), null);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Null(signal.CloseAtUtc);
    }

    /// <summary>Il verso dal movimento recente: segue (modo 1) o va contro (modo 2); un lato dichiarato filtra.</summary>
    [Theory]
    [InlineData(1, 0, true, SignalType.Buy)]
    [InlineData(1, 0, false, SignalType.Sell)]
    [InlineData(2, 0, true, SignalType.Sell)]
    [InlineData(2, 0, false, SignalType.Buy)]
    [InlineData(1, 1, false, SignalType.Hold)]
    [InlineData(1, 2, false, SignalType.Sell)]
    public void MomentumChoosesTheSide(int mode, int direction, bool rising, SignalType expected)
    {
        var bars = EndingAt(Utc(2024, 1, 8, 8));
        bars[^1] = new OhlcvData
        {
            DateTime = bars[^1].DateTime, Open = 100m, High = 106m, Low = 94m,
            Close = rising ? 105m : 95m, Volume = 1m
        };

        var strategy = new TestHod { Mode = mode, Side = direction, Momentum = 1 };

        Assert.Equal(expected, Evaluate(strategy, bars, null).Type);
    }

    /// <summary>Senza momentum il verso deve essere dichiarato: Direction 0 e' un errore di configurazione.</summary>
    [Fact]
    public void AFixedSideMustBeDeclared()
    {
        var strategy = new TestHod { Side = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => strategy.GenerateSignal(EndingAt(Utc(2024, 1, 8, 8)), Utc(2024, 1, 8, 8)));
    }

    /// <summary>Il giorno escluso si legge sull'orologio dell'orario: lunedi' fuori.</summary>
    [Fact]
    public void ASkippedDayIsSkipped()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestHod { Skip = 0 }, EndingAt(Utc(2024, 1, 8, 8)), null).Type);
        Assert.Equal(SignalType.Buy, Evaluate(new TestHod { Skip = 1 }, EndingAt(Utc(2024, 1, 8, 8)), null).Type);
    }

    /// <summary>In posizione non nasce nulla.</summary>
    [Fact]
    public void NothingIsEmittedWhileInPosition()
    {
        Assert.Equal(SignalType.Hold, Evaluate(new TestHod(), EndingAt(Utc(2024, 1, 8, 8)), SignalType.Buy).Type);
    }

    /// <summary>Il contenitore legge le leve proprie, l'ora come intero della griglia e l'orologio come numero.</summary>
    [Fact]
    public void TheContainerTakesItsLevers()
    {
        var strategy = new RC_HOD();
        strategy.Initialize(new Dictionary<string, object>
        {
            ["Symbol"] = "NQ", ["TimeframeMinutes"] = 60,
            ["EntryHour"] = 15, ["HoldHours"] = 3, ["ScheduleClock"] = 1,
            ["Direction"] = 0, ["MomentumMode"] = 1, ["MomentumBars"] = 8, ["SkipDay"] = 4,
            ["StopAtr"] = 1m, ["IntradayOnly"] = 0, ["ExitHour"] = -1,
            ["StartHour"] = -1, ["EndHour"] = -1, ["PtnNeutYes"] = 55
        });

        Assert.Equal("@NQ", strategy.Symbol);
        Assert.True(((ITradingStrategy)strategy).IsResearchContainer);
        Assert.Equal(new TimeOnly(15, 0), Read<TimeOnly>(strategy, "EntryTime"));
        Assert.Equal(3, Read<int>(strategy, "HoldHours"));
        Assert.Equal(InstrumentClock.Exchange, Read<InstrumentClock>(strategy, "ScheduleClock"));
        Assert.Equal(1, Read<int>(strategy, "MomentumMode"));
        Assert.Equal(8, Read<int>(strategy, "MomentumBars"));
        Assert.Equal(4, Read<int>(strategy, "SkipDay"));
    }

    // ------------------------------------------------------------------ supporto

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private static TradeSignal Evaluate(TestHod strategy, OhlcvData[] bars, SignalType? position) =>
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

    /// <summary>HOD di prova su NQ a 60 minuti: long alle 10:00 della ricerca, quattro ore, multiday.</summary>
    private sealed class TestHod : HourOfDayEngine
    {
        public TestHod() => IntradayOnly = false;

        public int Hour { set => EntryHour = value; }
        public int Hold { set => HoldHours = value; }
        public bool Exchange { set => ScheduleClock = value ? InstrumentClock.Exchange : InstrumentClock.Research; }
        public int Side { set => Direction = value; }
        public int Mode { set => MomentumMode = value; }
        public int Momentum { set => MomentumBars = value; }
        public int Skip { set => SkipDay = value; }

        public bool SessionExitAtNoon
        {
            set
            {
                if (!value) return;
                IntradayOnly = true;
                SessionExitTime = new TimeOnly(12, 0);
            }
        }

        public override string Name => "HOD_TEST";
        public override string Description => "HOD di prova";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
    }
}
