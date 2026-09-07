using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Passo 3 del refactor: il backtest interno fa passare il proprio feed dal layer e ne riporta la
/// struttura di sessione nel riepilogo.
///
/// <para><b>Cosa questo controllo aggiunge.</b> Il conteggio delle candele e la copertura
/// dell'intervallo — le due diagnosi che il summary aveva già — non vedono l'errore che conta: un
/// feed nato su un ancoraggio diverso ha il numero di barre giusto e copre l'intervallo giusto,
/// semplicemente sono barre <i>altre</i>. È il difetto che su <c>@KC_240</c> è passato inosservato
/// finché non l'ha trovato il metro.</para>
/// </summary>
public sealed class FeedCalendarReportTests
{
    private static DateTime Utc(string text) =>
        DateTime.Parse(text, null, System.Globalization.DateTimeStyles.AdjustToUniversal |
                                   System.Globalization.DateTimeStyles.AssumeUniversal);

    private static OhlcvData Bar(DateTime openUtc, decimal price = 100m, bool flat = false) => new()
    {
        DateTime = openUtc,
        Open = price,
        High = flat ? price : price + 1,
        Low = flat ? price : price - 1,
        Close = price,
        Volume = 1
    };

    /// <summary>Serie regolare, un bucket dopo l'altro sulla griglia del simbolo.</summary>
    private static List<OhlcvData> Series(string symbol, int timeframe, DateTime fromUtc, int count)
    {
        var grid = SessionGrid.For(symbol);
        var start = grid.BucketStartUtc(fromUtc, timeframe);
        var bars = new List<OhlcvData>();
        for (var i = 0; i < count; i++)
            bars.Add(Bar(start.AddMinutes((double)i * timeframe), 100 + i));

        return bars;
    }

    private static FeedCalendarReport Analyze(string symbol, int timeframe, IReadOnlyList<OhlcvData> bars) =>
        FeedCalendarReport.Analyze(MarketCalendarRegistry.Current.Get(symbol), timeframe, bars);

    /// <summary>
    /// Quattro giorni feriali di fila, dal lunedi': nessun sabato, che per NQ non e' sessione.
    /// La prima stesura di questo test ne usava dieci e falliva — correttamente, perche' una serie
    /// continua che attraversa un sabato contiene barre che il future non ha. E' la stessa
    /// segnalazione che serve sui feed CFD veri.
    /// </summary>
    [Fact]
    public void ASeriesOnItsOwnGridHasNoFindings()
    {
        // 2026-01-12 e' un lunedi': 24 bucket da 4h coprono lunedi'-giovedi'.
        var report = Analyze("NQ", 240, Series("NQ", 240, Utc("2026-01-12T00:00:00Z"), 24));

        Assert.Equal(0, report.BarsOffGrid);
        Assert.Equal(0, report.BarsOnNonSessionDay);
        Assert.False(report.HasFindings);
        Assert.Equal(24, report.Bars);
        Assert.True(report.Sessions > 0);
    }

    /// <summary>
    /// Il contrario del test sopra, e vale la pena averlo esplicito: una serie <b>continua</b> che
    /// attraversa un sabato contiene barre che il future non ha, e il layer lo dice. E' il caso di
    /// un feed CFD, che quota anche a mercato dei future chiuso.
    /// </summary>
    [Fact]
    public void AContinuousSeriesAcrossASaturdayIsFlagged()
    {
        // Dieci giorni dal lunedi' 12 gennaio: dentro c'e' sabato 17.
        var report = Analyze("NQ", 240, Series("NQ", 240, Utc("2026-01-12T00:00:00Z"), 60));

        Assert.Equal(0, report.BarsOffGrid);
        Assert.True(report.BarsOnNonSessionDay > 0);
        Assert.True(report.HasFindings);
    }

    /// <summary>
    /// Il difetto di <c>@KC_240</c>: barre etichettate su un ancoraggio diverso da quello del
    /// simbolo. Il conteggio delle candele non lo vede — le barre ci sono tutte — e la copertura
    /// nemmeno, perché l'intervallo è lo stesso.
    /// </summary>
    [Fact]
    public void BarsBornOnAnotherAnchorAreCounted()
    {
        // FDAX e' ancorato alle 01:00 dell'orologio della ricerca. Una serie costruita sulla
        // griglia di NQ — ancorata a mezzanotte — e' sfasata di un'ora su ogni bucket da 4h.
        var wrongAnchor = Series("NQ", 240, Utc("2026-01-12T00:00:00Z"), 30);

        var report = Analyze("FDAX", 240, wrongAnchor);

        Assert.Equal(30, report.Bars);
        Assert.Equal(30, report.BarsOffGrid);
        Assert.True(report.HasFindings);
    }

    /// <summary>
    /// La griglia dichiarata finisce nel report per esteso: senza, due report identici nella forma
    /// e calcolati su ancoraggi diversi non si distinguerebbero.
    /// </summary>
    [Fact]
    public void TheGridIsDeclaredInFull()
    {
        var report = Analyze("FDAX", 240, Series("FDAX", 240, Utc("2026-01-12T00:00:00Z"), 10));

        Assert.Contains("240m", report.Grid);
        Assert.Contains("01:00", report.Grid);
        Assert.Contains("Europe/Rome", report.Grid);
    }

    /// <summary>
    /// Le sessioni che un CFD fabbrica e il future non ha. Il dossier lo misura sul DAX all'11% del
    /// P&amp;L: quelle barre non generano trade, ma spezzano la sessione e con l'uscita di fine
    /// sessione chiudono posizioni ancora valide.
    /// </summary>
    [Fact]
    public void BarsOnDaysTheInstrumentDoesNotTradeAreCounted()
    {
        // 2026-01-18 e' una domenica: FDAX non ha sessione, NQ si'.
        var sunday = Series("FDAX", 240, Utc("2026-01-18T06:00:00Z"), 4);

        var fdax = Analyze("FDAX", 240, sunday);
        Assert.Equal(4, fdax.BarsOnNonSessionDay);
        Assert.True(fdax.HasFindings);

        var nq = Analyze("NQ", 240, Series("NQ", 240, Utc("2026-01-18T06:00:00Z"), 4));
        Assert.Equal(0, nq.BarsOnNonSessionDay);
    }

    /// <summary>
    /// Un simbolo di cui il dossier non dichiara i giorni non produce mai questa segnalazione: chi
    /// legge non deve inventarli, e contarli come "non di sessione" spegnerebbe lo strumento.
    /// </summary>
    [Fact]
    public void UndeclaredSessionDaysNeverProduceFindings()
    {
        var report = Analyze("MES", 240, Series("MES", 240, Utc("2026-01-18T06:00:00Z"), 4));

        Assert.False(report.SessionDaysDeclared);
        Assert.Equal(0, report.BarsOnNonSessionDay);
        Assert.False(report.HasFindings);
    }

    [Fact]
    public void GapsAreCountedButTheFirstBarIsNot()
    {
        var bars = Series("NQ", 60, Utc("2026-01-12T08:00:00Z"), 3);
        // tre ore di nulla, poi altre due barre
        bars.AddRange(Series("NQ", 60, Utc("2026-01-12T14:00:00Z"), 2));

        var report = Analyze("NQ", 60, bars);

        Assert.Equal(1, report.Gaps);
    }

    [Fact]
    public void FlatBarsAreCountedAsStale()
    {
        var bars = Series("NQ", 60, Utc("2026-01-12T08:00:00Z"), 4);
        bars[1] = Bar(bars[1].DateTime, 100, flat: true);
        bars[2] = Bar(bars[2].DateTime, 100, flat: true);

        Assert.Equal(2, Analyze("NQ", 60, bars).StaleBars);
    }

    [Fact]
    public void AnEmptySeriesIsDescribedWithoutFindings()
    {
        var report = Analyze("NQ", 240, []);

        Assert.Equal(0, report.Bars);
        Assert.False(report.HasFindings);
        Assert.NotEqual(string.Empty, report.Grid);
    }
}
