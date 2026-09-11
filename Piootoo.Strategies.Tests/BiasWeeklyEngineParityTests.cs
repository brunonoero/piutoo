using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Tests;

public sealed class BiasWeeklyEngineParityTests
{

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
            LongEntryTime = new TimeOnly(10, 0),   // c'e': la serie ha tutte le ore piene
            LongExitDay = 1,
            LongExitTime = new TimeOnly(10, 30)    // non c'e': serie oraria, nessuna barra a :30
        };

        var morte = strategy.UnreachableScheduleLegs(SerieFitta());

        var leg = Assert.Single(morte);
        Assert.Contains("uscita LONG", leg);
        Assert.Contains("10:30", leg);
    }

    [Fact]
    public void UnreachableScheduleLegs_NonSegnalaNienteQuandoIlFeedCopreGliIstanti()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = new TimeOnly(10, 0),
            LongExitDay = 4,
            LongExitTime = new TimeOnly(15, 0)
        };

        Assert.Empty(strategy.UnreachableScheduleLegs(SerieFitta()));
    }

    /// <summary>
    /// Il segnale nasce sulla barra <b>prima</b> di quella pianificata, come <c>next bar at market</c>,
    /// con <c>ValidFromUtc</c> all'apertura della barra pianificata. Prima nasceva sulla barra
    /// pianificata stessa: il backtest la vede al suo inizio, il server solo quando il cBot la
    /// spinge chiusa, e il cBot entrava sempre una barra dopo (compare-0033).
    ///
    /// <para>L'orario e' l'etichetta di <b>chiusura</b> nell'orologio della ricerca (Roma): con
    /// ingresso lunedi' 10:00 la barra pianificata e' quella che apre alle 09:00 di Roma, cioe'
    /// alle 08:00 UTC a gennaio, e il segnale nasce sulla barra delle 07:00 UTC.</para>
    /// </summary>
    [Fact]
    public void IlSegnaleNasceSullaBarraPrimaDiQuellaPianificata()
    {
        var strategy = new TestBiasWeekly
        {
            LongEntryDay = 0,
            LongEntryTime = new TimeOnly(10, 0),
            LongExitDay = 4,
            LongExitTime = new TimeOnly(15, 0),
            LongFastYes = 47,   // chiusure di sessione crescenti: vero sulla serie in salita
            LongFastNo = 48     // chiusure decrescenti: falso
        };

        var barraDiSegnale = Utc(2024, 1, 8, 7, 0);   // lunedi' 08:00 Roma
        var barraPianificata = Utc(2024, 1, 8, 8, 0); // lunedi' 09:00 Roma, chiude alle 10:00

        var signal = strategy.GenerateSignal(SerieInSalita(barraDiSegnale), barraDiSegnale);

        Assert.Equal(SignalType.Buy, signal.Type);
        Assert.Equal(TradeOrderType.Market, signal.OrderType);
        Assert.Equal(barraPianificata, signal.ValidFromUtc);
        Assert.Equal(barraPianificata, signal.ExpiresAtUtc);
        // Uscita venerdi' 15:00 Roma = etichetta di chiusura: la deadline e' l'apertura di quella
        // barra, le 14:00 di Roma, cioe' le 13:00 UTC.
        Assert.Equal(Utc(2024, 1, 12, 13, 0), signal.CloseAtUtc);

        // Sulla barra pianificata stessa non nasce piu' niente.
        var tardi = strategy.GenerateSignal(SerieInSalita(barraPianificata), barraPianificata);
        Assert.Equal(SignalType.Hold, tardi.Type);
    }

    /// <summary>Barre orarie in salita costante dal 1° gennaio 2024 fino a <paramref name="fine"/> inclusa.</summary>
    private static OhlcvData[] SerieInSalita(DateTime fine)
    {
        var inizio = Utc(2024, 1, 1, 0, 0);
        var count = (int)(fine - inizio).TotalHours + 1;
        var barre = new OhlcvData[count];
        for (var index = 0; index < count; index++)
        {
            var open = 100m + index * 0.1m;
            barre[index] = Bar(inizio.AddHours(index), open, open + 0.05m);
        }
        return barre;
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
        public TimeOnly LongEntryTime { set => EntryTimeLong = value; }
        public int LongExitDay { set => ExitDayLong = value; }
        public TimeOnly LongExitTime { set => ExitTimeLong = value; }
        public int LongFastYes { set => FastYesLong = value; }
        public int LongFastNo { set => FastNoLong = value; }

        public override string Name => "BIASW-test";
        public override string Description => "BIASW parity test";
        public override string Symbol => "@NQ";
        public override int TimeframeMinutes => 60;
        public override int RequiredCandles => 2;
    }
}
