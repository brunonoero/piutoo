using Piootoo.Shared.Configuration;

namespace Piootoo.Strategies.Tests;

public sealed class SessionClockTests
{
    /// <summary>
    /// Il fatto che rende necessario tutto il resto: la sessione NQ apre alle 17:00 di Chicago, che
    /// non è un'ora UTC fissa. Se questo test passasse con lo stesso valore nei due regimi vorrebbe
    /// dire che un orario di sessione in UTC sarebbe stato sufficiente.
    /// </summary>
    [Theory]
    [InlineData(1, 15, 23)] // ora solare a Chicago (UTC-6): la riapertura cade alle 23:00 UTC
    [InlineData(7, 15, 22)] // ora legale a Chicago (UTC-5): la stessa riapertura cade alle 22:00
    public void NasdaqSessionOpensAtADifferentUtcHourInEachDaylightRegime(int month, int day, int utcHour)
    {
        var clock = InstrumentRegistry.CreateSessionClock("@NQ");
        var instant = new DateTime(2025, month, day, utcHour, 0, 0, DateTimeKind.Utc);

        Assert.Equal(1700, clock.Hhmm(instant));
    }

    [Fact]
    public void GoldAndNasdaqSessionsOpenAtTheSameInstantWithDifferentDeclaredHours()
    {
        // Le sorgenti scrivono 1700 per NQ e 1800 per GC: numeri diversi, stesso istante. È la
        // ragione per cui il registro tiene il fuso e non riscrive gli orari.
        var open = new DateTime(2025, 1, 15, 23, 0, 0, DateTimeKind.Utc);

        Assert.Equal(1700, InstrumentRegistry.CreateSessionClock("@NQ").Hhmm(open));
        Assert.Equal(1800, InstrumentRegistry.CreateSessionClock("@GC").Hhmm(open));
    }

    [Fact]
    public void DaxSessionIsReadInFrankfurtTime()
    {
        // Le sorgenti FDAX dichiarano 0800->2200, cioè l'orario Eurex in ora di Francoforte.
        var clock = InstrumentRegistry.CreateSessionClock("@FDAX");

        Assert.Equal(800, clock.Hhmm(new DateTime(2025, 1, 15, 7, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(800, clock.Hhmm(new DateTime(2025, 7, 15, 6, 0, 0, DateTimeKind.Utc)));
    }

    /// <summary>
    /// La cache dell'offset vive per giorno UTC, ma nei due giorni all'anno in cui l'ora legale
    /// cambia l'offset non è costante dentro la giornata. Senza il controllo di uniformità metà
    /// delle barre di quel giorno userebbe l'offset dell'altra metà, e il confine di sessione
    /// scivolerebbe di un'ora proprio nel giorno peggiore.
    /// </summary>
    [Theory]
    [InlineData(3, 9)]  // inizio dell'ora legale a Chicago
    [InlineData(11, 2)] // fine dell'ora legale a Chicago
    public void OffsetCacheStaysCorrectAcrossADaylightSavingTransition(int month, int day)
    {
        var clock = InstrumentRegistry.CreateSessionClock("@NQ");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var cursor = new DateTime(2025, month, day, 0, 0, 0, DateTimeKind.Utc);
        var end = cursor.AddDays(1);

        var offsetsSeen = new HashSet<TimeSpan>();
        while (cursor < end)
        {
            var expected = TimeZoneInfo.ConvertTimeFromUtc(cursor, zone);
            Assert.Equal(expected, clock.ToSessionTime(cursor));
            offsetsSeen.Add(expected - cursor);
            cursor = cursor.AddMinutes(15);
        }

        // Se il giorno scelto non contenesse davvero il cambio d'ora, il test non proverebbe nulla.
        Assert.Equal(2, offsetsSeen.Count);
    }

    [Fact]
    public void ConsecutiveBarsAcrossManyDaysMatchTheTimeZoneDatabase()
    {
        // La cache di un solo giorno è un'ottimizzazione: deve essere indistinguibile dal calcolo
        // diretto anche percorrendo entrambi i cambi d'ora dell'anno.
        var clock = InstrumentRegistry.CreateSessionClock("@GC");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var cursor = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor < end)
        {
            Assert.Equal(TimeZoneInfo.ConvertTimeFromUtc(cursor, zone), clock.ToSessionTime(cursor));
            cursor = cursor.AddMinutes(35);
        }
    }

    [Fact]
    public void SessionDayIsTheCalendarDayInExchangeTime()
    {
        var clock = InstrumentRegistry.CreateSessionClock("@NQ");

        // 03:00 UTC del 5 marzo è ancora il 4 marzo a Chicago: il cambio di giorno è una delle
        // condizioni che aprono una sessione nuova, quindi va letto nello stesso orologio.
        var instant = new DateTime(2025, 3, 5, 3, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2025, 3, 4), clock.SessionDay(instant));
    }

    [Fact]
    public void KindIsIgnoredBecauseTheDomainIsAlwaysUtc()
    {
        var clock = InstrumentRegistry.CreateSessionClock("@NQ");
        var value = new DateTime(2025, 1, 15, 23, 0, 0);

        Assert.Equal(
            clock.ToSessionTime(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
            clock.ToSessionTime(DateTime.SpecifyKind(value, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void UnknownTimeZoneFailsExplicitly()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new SessionClock("Mars/Olympus"));

        Assert.Contains("IANA", error.Message);
    }

    [Fact]
    public void EveryRegisteredInstrumentDeclaresAResolvableTimeZone()
    {
        // Un fuso inventato nel registro non darebbe errore fino al primo backtest, e lì si
        // manifesterebbe come confine di sessione spostato invece che come eccezione.
        foreach (var symbol in InstrumentRegistry.RegisteredSymbols)
        {
            var spec = InstrumentRegistry.Get(symbol);
            Assert.False(string.IsNullOrWhiteSpace(spec.SessionTimeZone), symbol);
            Assert.Contains("/", spec.SessionTimeZone);
            _ = new SessionClock(spec.SessionTimeZone);
        }
    }
}
