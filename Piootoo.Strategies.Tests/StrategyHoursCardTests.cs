using Piootoo.Core.Services;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La scheda oraria del catalogo: <see cref="StrategyHoursService"/>.
///
/// <para><b>Cosa protegge.</b> La scheda esiste per far vedere un ancoraggio sbagliato, e un errore
/// <i>dentro la scheda</i> lo nasconderebbe invece di mostrarlo — con l'aggravante che sembrerebbe
/// una verifica fatta. I casi qui sotto sono i due che il resto del sistema tratta come diversi: un
/// simbolo ancorato alle 01:00 (FDAX) e uno a mezzanotte (NQ), letti in ora solare e in ora legale,
/// dove lo stesso orario locale cade in due istanti UTC diversi e a volte nel giorno prima.</para>
/// </summary>
public sealed class StrategyHoursCardTests
{
    private readonly StrategyHoursService _service = new();

    /// <summary>
    /// FDAX è ancorato alle 01:00 di Roma: in UTC la sessione comincia a mezzanotte d'inverno e alle
    /// 23:00 del <b>giorno prima</b> d'estate. È lo scarto che separa un run dal grafico del broker.
    /// </summary>
    [Fact]
    public void FdaxSessionAnchorIsOneAmRomeInBothSeasons()
    {
        var card = _service.Build("PTS_FDAX_PCH_001_240");

        Assert.Equal(1, card.SessionAnchorHour);
        Assert.Equal(1, card.CalendarSessionStartHour);
        Assert.Null(card.SessionAnchorOverrideReason);
        Assert.Equal("Europe/Rome", card.Session.TimeZoneId);
        Assert.Equal("inizio 00:00Z", card.Session.WinterUtc);
        Assert.Equal("inizio 23:00Z (giorno prima)", card.Session.SummerUtc);
    }

    /// <summary>
    /// La finestra operativa si riporta verbatim dal run di ricerca e si legge nell'orologio della
    /// ricerca, non in quello di borsa: su FDAX i due sono lo stesso fuso solo per caso, e il test
    /// vuole vedere Roma anche se la borsa è Berlino.
    /// </summary>
    [Fact]
    public void TradingWindowIsReportedInTheResearchClock()
    {
        var card = _service.Build("PTS_FDAX_PCH_001_240");

        Assert.NotNull(card.TradingWindow);
        Assert.Equal(700, card.TradingWindow!.StartHhmm);
        Assert.Equal(1300, card.TradingWindow.EndHhmm);
        Assert.Equal("ricerca", card.TradingWindow.Clock);
        Assert.Equal("Europe/Rome", card.TradingWindow.TimeZoneId);
        Assert.Equal("Europe/Berlin", card.ExchangeTimeZone);
        Assert.Equal("06:00Z → 12:00Z", card.TradingWindow.WinterUtc);
        Assert.Equal("05:00Z → 11:00Z", card.TradingWindow.SummerUtc);
    }

    /// <summary>
    /// La finestra di negoziazione dello strumento arriva dal calendario, con l'ancoraggio bordo per
    /// bordo: sul FDAX l'apertura è fissa in UTC e la chiusura segue Berlino, ed è la differenza che
    /// la scheda deve mostrare invece di appiattire.
    /// </summary>
    [Fact]
    public void InstrumentWindowKeepsTheAnchorOfEachEdge()
    {
        var card = _service.Build("PTS_FDAX_PCH_001_240");

        var window = Assert.Single(card.InstrumentTradingWindows);
        Assert.Equal("00:15 UTC", window.Open);
        Assert.Equal("22:00 Europe/Berlin", window.Close);
        Assert.Equal(["Lun", "Mar", "Mer", "Gio", "Ven"], window.OpensOn);
    }

    /// <summary>
    /// NQ è ancorato a mezzanotte di Roma, che in UTC è sempre il giorno prima. Il caso serve a
    /// distinguere "nessuno scarto" da "scarto non calcolato": entrambi darebbero 00:00Z se lo
    /// scarto di data non venisse detto.
    /// </summary>
    [Fact]
    public void MidnightAnchoredSessionStartsOnThePreviousUtcDay()
    {
        var card = _service.Build("PTS_NQ_TFM_014_240");

        Assert.Equal(0, card.SessionAnchorHour);
        Assert.Equal("inizio 23:00Z (giorno prima)", card.Session.WinterUtc);
        Assert.Equal("inizio 22:00Z (giorno prima)", card.Session.SummerUtc);
    }

    /// <summary>
    /// Una finestra <c>0000-2359</c> è la forma in cui un run che non ha filtrato per ora scrive i
    /// propri orari: la scheda deve dire che non esclude nulla, non elencare una fascia vuota fra le
    /// 23:59 e la mezzanotte che manderebbe a cercare un vincolo inesistente.
    /// </summary>
    [Fact]
    public void FullDayWindowIsReportedAsNoFilter()
    {
        var card = _service.Build("PTS_CT_TFU_001_240");

        Assert.NotNull(card.TradingWindow);
        Assert.Equal(0, card.TradingWindow!.StartHhmm);
        Assert.Equal(2359, card.TradingWindow.EndHhmm);
        Assert.Contains("non esclude nulla", card.TradingWindowNote);
    }

    [Fact]
    public void UnknownStrategyIsRejected()
        => Assert.Throws<KeyNotFoundException>(() => _service.Build("PTS_NON_ESISTE_001_60"));
}
