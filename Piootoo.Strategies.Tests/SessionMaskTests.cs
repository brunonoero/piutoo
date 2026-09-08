using Piootoo.Shared.MarketData;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La maschera di sessione: quando lo strumento negoziava davvero.
///
/// <para>Gli istanti sono scritti in UTC e commentati con l'ora di borsa, perché è l'unico modo di
/// leggere un test su due orologi senza doverli riconvertire a mente ogni volta. Le date sono scelte
/// dentro le quattro configurazioni che nascono dal disallineamento fra ora legale europea e
/// americana: il caso interessante non è il giorno qualunque, sono le tre settimane l'anno in cui il
/// blackout comune si sposta.</para>
/// </summary>
public sealed class SessionMaskTests
{
    private static SessionMask Mask(string symbol) => SessionMask.For(symbol);

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    // ---------------------------------------------------------------------------------------
    // CME: la pausa sta alle 16:00-17:00 di Chicago tutto l'anno, quindi in UTC si sposta.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void CmeBreakIsClosedInSummer()
    {
        // 2026-07-01 è un mercoledì, Chicago in CDT (UTC-5).
        Assert.False(Mask("NQ").IsOpen(Utc(2026, 7, 1, 21, 30)));   // 16:30 Chicago
        Assert.True(Mask("NQ").IsOpen(Utc(2026, 7, 1, 20, 30)));    // 15:30 Chicago
        Assert.True(Mask("NQ").IsOpen(Utc(2026, 7, 1, 22, 30)));    // 17:30 Chicago, giorno dopo
    }

    [Fact]
    public void CmeBreakIsClosedInWinterOneHourLaterInUtc()
    {
        // 2026-01-07 è un mercoledì, Chicago in CST (UTC-6).
        Assert.False(Mask("NQ").IsOpen(Utc(2026, 1, 7, 22, 30)));   // 16:30 Chicago
        Assert.True(Mask("NQ").IsOpen(Utc(2026, 1, 7, 21, 30)));    // 15:30 Chicago
        Assert.True(Mask("NQ").IsOpen(Utc(2026, 1, 7, 23, 30)));    // 17:30 Chicago
    }

    /// <summary>
    /// L'halt di quindici minuti degli equity index — 15:15-15:30 di Chicago — è il regime
    /// <b>pre-2015</b> e non esiste più: misurato su <c>@NQ_15</c> 2024+, lo slot 15:15 ha la stessa
    /// copertura dei vicini (80%), mentre sull'intera storia sta al 26%. La finestra non lo
    /// dichiara, e questo test fissa la scelta perché non torni per abitudine.
    /// </summary>
    [Fact]
    public void EquityIndexSettlementHaltIsNotDeclaredAnyMore()
    {
        Assert.True(Mask("NQ").IsOpen(Utc(2026, 7, 1, 20, 20)));    // 15:20 Chicago
        Assert.True(Mask("ES").IsOpen(Utc(2026, 7, 1, 20, 20)));
    }

    [Fact]
    public void CmeWeekOpensSundayEveningAndClosesFridayAfternoon()
    {
        var nq = Mask("NQ");

        Assert.True(nq.IsOpen(Utc(2026, 7, 5, 22, 30)));            // domenica 17:30 Chicago
        Assert.False(nq.IsOpen(Utc(2026, 7, 5, 21, 30)));           // domenica 16:30, non ancora
        Assert.False(nq.IsOpen(Utc(2026, 7, 3, 21, 30)));           // venerdì 16:30, già chiuso
        Assert.True(nq.IsOpen(Utc(2026, 7, 3, 20, 30)));            // venerdì 15:30
        Assert.False(nq.IsOpen(Utc(2026, 7, 4, 12, 0)));            // sabato
    }

    // ---------------------------------------------------------------------------------------
    // FDAX: apertura ancorata in UTC, chiusura ancorata in locale. È il caso che rompe
    // qualunque implementazione che accoppi i due bordi sulla stessa data di calendario.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void FdaxOpensAtTheSameUtcInstantInBothSeasons()
    {
        var fdax = Mask("FDAX");

        // Estate: 00:15 UTC = 02:15 a Berlino.
        Assert.False(fdax.IsOpen(Utc(2026, 7, 1, 0, 10)));
        Assert.True(fdax.IsOpen(Utc(2026, 7, 1, 0, 20)));

        // Inverno: lo stesso istante UTC, che a Berlino è 01:15.
        Assert.False(fdax.IsOpen(Utc(2026, 1, 7, 0, 10)));
        Assert.True(fdax.IsOpen(Utc(2026, 1, 7, 0, 20)));
    }

    [Fact]
    public void FdaxClosesAtTheSameLocalTimeSoTheUtcInstantMoves()
    {
        var fdax = Mask("FDAX");

        // Estate: 22:00 a Berlino = 20:00 UTC.
        Assert.True(fdax.IsOpen(Utc(2026, 7, 1, 19, 30)));
        Assert.False(fdax.IsOpen(Utc(2026, 7, 1, 20, 30)));

        // Inverno: 22:00 a Berlino = 21:00 UTC.
        Assert.True(fdax.IsOpen(Utc(2026, 1, 7, 20, 30)));
        Assert.False(fdax.IsOpen(Utc(2026, 1, 7, 21, 30)));
    }

    /// <summary>
    /// D'estate la pausa notturna del FDAX dura 4h15 e non 3h15, perché l'apertura resta agganciata
    /// a Singapore mentre la chiusura segue l'orologio locale. È la differenza che si vede
    /// nell'archivio: 12,5% di barre tolte d'estate contro 8,3% d'inverno.
    /// </summary>
    [Fact]
    public void FdaxNightPauseIsLongerInSummer()
    {
        var fdax = Mask("FDAX");

        // 21:30 UTC del 1° luglio è 23:30 a Berlino: dentro la pausa estiva, che va da 20:00 UTC
        // a 00:15 UTC del giorno dopo.
        Assert.False(fdax.IsOpen(Utc(2026, 7, 1, 21, 30)));

        // Lo stesso istante UTC d'inverno è 22:30 a Berlino, e la pausa è già cominciata comunque:
        // il confronto che conta è l'ora prima, dove le due stagioni divergono.
        Assert.True(fdax.IsOpen(Utc(2026, 1, 7, 20, 30)));          // 21:30 Berlino, ancora aperto
        Assert.False(fdax.IsOpen(Utc(2026, 7, 1, 20, 30)));         // 22:30 Berlino, già chiuso
    }

    [Fact]
    public void FdaxHasNoSundayEveningReopen()
    {
        var fdax = Mask("FDAX");

        Assert.False(fdax.IsOpen(Utc(2026, 7, 5, 23, 0)));          // domenica sera
        Assert.True(fdax.IsOpen(Utc(2026, 7, 6, 0, 20)));           // lunedì 00:15 UTC
        Assert.False(fdax.IsOpen(Utc(2026, 7, 4, 12, 0)));          // sabato
    }

    // ---------------------------------------------------------------------------------------
    // Cotone ICE: apre domenica sera a New York, e in ora di borsa quella è la giornata di
    // LUNEDI'. È il caso in cui 'opensOn' e 'sessionDays' danno risposte diverse ed entrambe
    // giuste — il dossier del paniere conta zero sessioni domenicali su CT.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void CottonOpensSundayEveningInExchangeTime()
    {
        var ct = Mask("CT");

        Assert.True(ct.IsOpen(Utc(2026, 7, 6, 1, 30)));             // domenica 21:30 New York
        Assert.False(ct.IsOpen(Utc(2026, 7, 6, 0, 30)));            // domenica 20:30, non ancora
        Assert.True(ct.IsOpen(Utc(2026, 7, 6, 14, 0)));             // lunedì 10:00 New York
        Assert.False(ct.IsOpen(Utc(2026, 7, 6, 19, 0)));            // lunedì 15:00, chiuso alle 14:20
    }

    [Fact]
    public void CottonHasNoTradingDayOpeningOnFriday()
    {
        var ct = Mask("CT");

        Assert.True(ct.IsOpen(Utc(2026, 7, 3, 14, 0)));             // venerdì 10:00 NY, giornata aperta giovedì
        Assert.False(ct.IsOpen(Utc(2026, 7, 4, 1, 30)));            // venerdì 21:30 NY: non riapre
    }

    // ---------------------------------------------------------------------------------------
    // Il formato
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void SymbolWithoutWindowLetsEverythingThrough()
    {
        var calendar = MarketCalendarRegistry.Parse(
            """
            {"specVersion":"test.1","symbols":{"NQ":{
              "exchangeTz":"America/Chicago","researchTz":"Europe/Rome","sessionStartHour":0
            }}}
            """,
            "test");

        var mask = new SessionMask(calendar.Get("NQ"));
        Assert.False(mask.DeclaresWindow);
        Assert.Null(mask.IsOpen(Utc(2026, 7, 1, 21, 30)));
    }

    [Fact]
    public void OverlappingPeriodsAreRejected()
    {
        var error = Assert.Throws<MarketCalendarException>(() => MarketCalendarRegistry.Parse(
            """
            {"specVersion":"test.1","symbols":{"NQ":{
              "exchangeTz":"America/Chicago","researchTz":"Europe/Rome","sessionStartHour":0,
              "tradingWindows":[
                {"open":{"at":"17:00","anchor":"local"},"close":{"at":"16:00","anchor":"local"},
                 "opensOn":["Sun","Mon","Tue","Wed","Thu"]},
                {"open":{"at":"18:00","anchor":"local"},"close":{"at":"17:00","anchor":"local"},
                 "opensOn":["Sun","Mon","Tue","Wed","Thu"],"from":"06-01","to":"08-31"}
              ]
            }}}
            """,
            "test"));

        Assert.Contains("disgiunti", error.Message);
    }

    [Fact]
    public void PeriodsThatLeaveADayUncoveredAreRejected()
    {
        var error = Assert.Throws<MarketCalendarException>(() => MarketCalendarRegistry.Parse(
            """
            {"specVersion":"test.1","symbols":{"NQ":{
              "exchangeTz":"America/Chicago","researchTz":"Europe/Rome","sessionStartHour":0,
              "tradingWindows":[
                {"open":{"at":"17:00","anchor":"local"},"close":{"at":"16:00","anchor":"local"},
                 "opensOn":["Sun","Mon","Tue","Wed","Thu"],"from":"01-01","to":"06-30"}
              ]
            }}}
            """,
            "test"));

        Assert.Contains("scoperto", error.Message);
    }

    /// <summary>
    /// Ogni simbolo del calendario dichiara la propria finestra. Il numero è fissato perché
    /// aggiungere un simbolo senza finestra sia una scelta esplicita: senza, la maschera lo
    /// lascerebbe passare tutto in silenzio, che è il difetto che questo lavoro esiste per chiudere.
    /// </summary>
    [Fact]
    public void EverySymbolInTheCalendarDeclaresATradingWindow()
    {
        var senza = MarketCalendarRegistry.Current.Symbols
            .Where(symbol => MarketCalendarRegistry.Current.Get(symbol).TradingWindows.Count == 0)
            .ToArray();

        Assert.Empty(senza);
    }
}
