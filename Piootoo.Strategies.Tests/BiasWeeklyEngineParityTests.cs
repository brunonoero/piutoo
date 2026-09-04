using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class BiasWeeklyEngineParityTests
{
    [Fact]
    public void MondayZero_EntersAtTheScheduledBarOpen_AndCarriesFridayExit()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = 1000,
            LongExitDay = 4,
            LongExitTime = 1500
        };
        var entryTime = Utc(2024, 1, 8, 10, 0); // Monday

        var signal = strategy.GenerateSignal(Bars(entryTime, 101m), entryTime);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(101m, signal.Price);
        Assert.Equal(entryTime, signal.ValidFromUtc);
        Assert.Equal(entryTime, signal.ExpiresAtUtc);
        Assert.Equal(Utc(2024, 1, 12, 15, 0), signal.CloseAtUtc);
    }

    [Fact]
    public void EntryTime_IsAnExactSchedule_NotAnOperatingWindow()
    {
        var strategy = new TestBiasWeekly { LongEntryDay = 0, LongEntryTime = 1000 };
        var atScheduledTime = Utc(2024, 1, 8, 10, 0);
        var oneMinuteLater = atScheduledTime.AddMinutes(1);

        Assert.Equal(SignalType.Buy, strategy.GenerateSignal(Bars(atScheduledTime, 101m), atScheduledTime).Type);
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(Bars(oneMinuteLater, 101m), oneMinuteLater).Type);
    }

    [Fact]
    public void FastGates_RequireYesAndRejectNo()
    {
        var strategy = new TestBiasWeekly { LongEntryDay = 0, LongEntryTime = 1000 };
        var entryTime = Utc(2024, 1, 8, 10, 0);

        strategy.LongFastYes = 153; // Sentinel Fast: always false.
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(Bars(entryTime, 101m), entryTime).Type);

        strategy.LongFastYes = 152; // Sentinel Fast: always true.
        strategy.LongFastNo = 152;
        Assert.Equal(SignalType.Hold, strategy.GenerateSignal(Bars(entryTime, 101m), entryTime).Type);

        strategy.LongFastNo = 153; // Sentinel Fast: always false.
        Assert.Equal(SignalType.Buy, strategy.GenerateSignal(Bars(entryTime, 101m), entryTime).Type);
    }

    [Fact]
    public void FastGate_IsEvaluatedOnTheBarBeforeTheScheduledEntry()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = 1000,
            LongFastYes = 142
        };
        var entryTime = Utc(2024, 1, 8, 10, 0);

        // Il pattern 142 e' vero sulla barra precedente (100 > 99), ma sarebbe falso
        // sulla barra di ingresso (1 non e' > 99): equivale allo shift(1) Python.
        var signal = strategy.GenerateSignal(Bars(entryTime, 100m, currentClose: 1m), entryTime);

        Assert.Equal(SignalType.Buy, signal.Type);
    }

    [Fact]
    public void FridayToMonday_ResolvesExitInTheFollowingWeek()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 4,
            LongEntryTime = 1500,
            LongExitDay = 0,
            LongExitTime = 1000
        };
        var entryTime = Utc(2024, 1, 12, 15, 0); // Friday

        var signal = strategy.GenerateSignal(Bars(entryTime, 101m), entryTime);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(Utc(2024, 1, 15, 10, 0), signal.CloseAtUtc);
    }

    /// <summary>
    /// Una leg programmata su un istante che il feed non ha <b>non produce niente</b>: non un
    /// segnale, non uno skip, non un errore. Su compare-0017 due BIASW su HO hanno chiuso otto mesi
    /// con zero segnali perche' <c>@HO_60</c> non ha nessuna barra fra le 22:00 e le 23:00.
    /// <c>UnreachableScheduleLegs</c> e' il controllo che lo dice prima del run.
    ///
    /// <para>La serie di prova e' fitta su tutte le 24 ore e tutti i giorni, quindi il risultato non
    /// dipende dal fuso con cui il motore legge l'ora: l'unico istante irraggiungibile e' quello a
    /// mezz'ora, che su una serie oraria non esiste per costruzione.</para>
    /// </summary>
    [Fact]
    public void UnreachableScheduleLegs_SegnalaSoloLeLegSenzaBarraNelFeed()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = 1000,   // c'e': la serie ha tutte le ore piene
            LongExitDay = 1,
            LongExitTime = 1030     // non c'e': serie oraria, nessuna barra a :30
        };

        var morte = strategy.UnreachableScheduleLegs(SerieFitta());

        var leg = Assert.Single(morte);
        Assert.Contains("uscita LONG", leg);
        Assert.Contains("1030", leg);
    }

    [Fact]
    public void UnreachableScheduleLegs_NonSegnalaNienteQuandoIlFeedCopreGliIstanti()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = 1000,
            LongExitDay = 4,
            LongExitTime = 1500
        };

        Assert.Empty(strategy.UnreachableScheduleLegs(SerieFitta()));
    }

    /// <summary>Tre settimane di barre orarie senza buchi: ogni coppia (giorno, ora piena) esiste.</summary>
    private static OhlcvData[] SerieFitta()
    {
        var inizio = Utc(2024, 1, 1, 0, 0);
        var barre = new OhlcvData[21 * 24];
        for (var index = 0; index < barre.Length; index++)
            barre[index] = Bar(inizio.AddHours(index), 100m);
        return barre;
    }

    private static OhlcvData[] Bars(DateTime current, decimal currentOpen, decimal? currentClose = null) =>
    [
        Bar(current.AddHours(-1), 100m),
        Bar(current, currentOpen, currentClose)
    ];

    private static OhlcvData Bar(DateTime time, decimal open, decimal? close = null) =>
        new()
        {
            DateTime = time,
            Open = open,
            High = open + 1m,
            Low = open - 1m,
            Close = close ?? open,
            Volume = 1m
        };

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private sealed class TestBiasWeekly : BiasWeeklyEngine
    {
        public int LongEntryDay { set => EntryDayLong = value; }
        public int LongEntryTime { set => EntryTimeLong = value; }
        public int LongExitDay { set => ExitDayLong = value; }
        public int LongExitTime { set => ExitTimeLong = value; }
        public int LongFastYes { set => FastYesLong = value; }
        public int LongFastNo { set => FastNoLong = value; }

        public override string Name => "BIASW-test";
        public override string Description => "BIASW parity test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 2;
    }
}
