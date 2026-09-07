using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Passo 2 del refactor del layer barre: l'aggregazione dal minuto, in un punto solo.
///
/// <para>I casi qui sotto sono quelli che, passando, darebbero una serie apparentemente normale e
/// silenziosamente diversa: l'ancoraggio non a mezzanotte, il confine calcolato sul cambio d'ora, i
/// secondi non troncati, il bucket troncato ai bordi dell'input.</para>
/// </summary>
public sealed class BarAggregatorTests
{
    private static OhlcvData Minute(string utc, decimal open, decimal high, decimal low, decimal close, decimal volume = 1) =>
        new()
        {
            DateTime = DateTime.Parse(utc, null, System.Globalization.DateTimeStyles.AdjustToUniversal |
                                             System.Globalization.DateTimeStyles.AssumeUniversal),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume
        };

    private static IEnumerable<OhlcvData> Minutes(DateTime startUtc, int count, decimal basePrice = 100m)
    {
        for (var i = 0; i < count; i++)
        {
            var price = basePrice + i;
            yield return new OhlcvData
            {
                DateTime = startUtc.AddMinutes(i),
                Open = price,
                High = price + 1,
                Low = price - 1,
                Close = price,
                Volume = 1
            };
        }
    }

    private static DateTime Utc(string text) =>
        DateTime.Parse(text, null, System.Globalization.DateTimeStyles.AdjustToUniversal |
                                   System.Globalization.DateTimeStyles.AssumeUniversal);

    // ------------------------------------------------------------------ la griglia

    /// <summary>
    /// FDAX è ancorato all'01:00 dell'orologio della ricerca, NQ a mezzanotte. È la differenza che
    /// vale un'ora su ogni bucket da 4h in su, e che non produce barre sbagliate: produce barre
    /// diverse, tutte plausibili.
    /// </summary>
    [Theory]
    // In inverno Europe/Rome è UTC+1: l'01:00 locale di FDAX è mezzanotte UTC.
    [InlineData("FDAX", "2026-01-15T00:30:00Z", 240, "2026-01-15T00:00:00Z")]
    [InlineData("FDAX", "2026-01-14T23:30:00Z", 240, "2026-01-14T20:00:00Z")]
    // NQ è ancorato a mezzanotte locale, cioè 23:00 UTC del giorno prima.
    [InlineData("NQ", "2026-01-15T00:30:00Z", 240, "2026-01-14T23:00:00Z")]
    [InlineData("NQ", "2026-01-15T03:30:00Z", 240, "2026-01-15T03:00:00Z")]
    public void BucketStartFollowsTheSymbolAnchor(
        string symbol, string openUtc, int timeframe, string expected)
    {
        var grid = SessionGrid.For(symbol);

        Assert.Equal(Utc(expected), grid.BucketStartUtc(Utc(openUtc), timeframe));
    }

    /// <summary>
    /// Sotto l'ora i due ancoraggi coincidono comunque, perché lo scarto di un fuso è un numero
    /// intero di ore. È il motivo per cui il difetto dell'ancoraggio è rimasto invisibile finché il
    /// paniere non ha avuto strategie a 4h.
    /// </summary>
    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public void BelowTheHourTheAnchorDoesNotMatter(int timeframe)
    {
        var withAnchor = SessionGrid.For("FDAX");
        var withoutAnchor = SessionGrid.For("NQ");
        var instant = Utc("2026-01-15T09:37:00Z");

        Assert.Equal(
            withoutAnchor.BucketStartUtc(instant, timeframe),
            withAnchor.BucketStartUtc(instant, timeframe));
    }

    /// <summary>
    /// I secondi si buttano prima di contare. Nel cBot questo difetto produceva un bucket monco
    /// ogni blocco di backfill — il 19,7% dei giornalieri dell'archivio FTMOPLATFORM — perché il
    /// confine usciva a "inizio bucket + qualche secondo" e una sola barra base lo superava.
    /// </summary>
    [Fact]
    public void SecondsAreDroppedBeforeComputingTheBoundary()
    {
        var grid = SessionGrid.For("NQ");

        Assert.Equal(
            grid.BucketStartUtc(Utc("2026-01-15T03:00:00Z"), 240),
            grid.BucketStartUtc(Utc("2026-01-15T03:00:00Z").AddSeconds(37).AddTicks(11), 240));
    }

    /// <summary>
    /// Un timeframe che non divide il giorno farebbe scivolare i bucket rispetto alla sessione. È
    /// lo stesso rifiuto dell'aggregatore Python e dei cBot, scritto una volta sola.
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(50)]
    [InlineData(10080)]
    [InlineData(0)]
    [InlineData(-15)]
    public void TimeframesThatDoNotDivideTheDayAreRejected(int timeframe)
    {
        Assert.False(SessionGrid.DividesTheDay(timeframe));
        Assert.Throws<ArgumentException>(
            () => new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), timeframe));
    }

    // ------------------------------------------------------------------ l'aggregazione

    [Fact]
    public void OhlcvIsFoldedOverTheBucket()
    {
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 15);
        var minutes = new[]
        {
            Minute("2026-01-15T09:00:00Z", 100, 105, 99, 104, 10),
            Minute("2026-01-15T09:01:00Z", 104, 110, 103, 108, 20),
            Minute("2026-01-15T09:02:00Z", 108, 109, 95, 96, 30),
            // barra del bucket successivo: chiude il primo
            Minute("2026-01-15T09:15:00Z", 96, 97, 95, 97, 5)
        };

        var bars = aggregator.Aggregate(minutes);

        Assert.Equal(2, bars.Count);
        var first = bars[0];
        Assert.Equal(Utc("2026-01-15T09:00:00Z"), first.Bar.DateTime);
        Assert.Equal(100, first.Bar.Open);   // apertura della prima
        Assert.Equal(110, first.Bar.High);   // massimo del bucket
        Assert.Equal(95, first.Bar.Low);     // minimo del bucket
        Assert.Equal(96, first.Bar.Close);   // chiusura dell'ultima
        Assert.Equal(60, first.Bar.Volume);  // somma
        Assert.Equal(3, first.MinuteCount);
    }

    /// <summary>
    /// L'etichetta è l'inizio del bucket, non la fine: è la chiave con cui il feed deduplica ed è la
    /// convenzione di tutto il sistema. La formula della ricerca dice la stessa cosa su etichette di
    /// chiusura, ed è per questo che il "meno un minuto" del Python qui non compare.
    /// </summary>
    [Fact]
    public void TheLabelIsTheBucketOpenNotItsClose()
    {
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 60);

        var bars = aggregator.Aggregate(Minutes(Utc("2026-01-15T09:00:00Z"), 120));

        Assert.Equal(2, bars.Count);
        Assert.Equal(Utc("2026-01-15T09:00:00Z"), bars[0].Bar.DateTime);
        Assert.Equal(Utc("2026-01-15T10:00:00Z"), bars[1].Bar.DateTime);
    }

    /// <summary>
    /// Un buco interno — il mercato chiuso — <b>non</b> rende incompleto un bucket: il mercato
    /// chiuso non è storia mancante. A dirlo è <c>MinuteCount</c>, che è un fatto.
    /// </summary>
    [Fact]
    public void AnInternalGapDoesNotMakeTheBucketIncomplete()
    {
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 60);
        var minutes = new List<OhlcvData>();
        minutes.AddRange(Minutes(Utc("2026-01-15T09:00:00Z"), 10));
        // trenta minuti di nulla in mezzo
        minutes.AddRange(Minutes(Utc("2026-01-15T09:40:00Z"), 20));
        minutes.AddRange(Minutes(Utc("2026-01-15T10:00:00Z"), 1));

        var bars = aggregator.Aggregate(minutes);

        var full = bars[0];
        Assert.True(full.Complete);
        Assert.Equal(30, full.MinuteCount);
    }

    /// <summary>
    /// Le due sole barre che il metro trova sempre diverse: la prima, troncata perché l'input
    /// comincia a metà bucket, e l'ultima, ancora in formazione. Marcarle è ciò che permette a un
    /// consumatore di scartarle invece di trattarle come barre vere.
    /// </summary>
    [Fact]
    public void OnlyTheEdgesAreIncomplete()
    {
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 60);

        // Comincia a 09:30, cioe' a meta' del bucket delle 09:00, e finisce a meta' di quello
        // delle 11:00.
        var bars = aggregator.Aggregate(Minutes(Utc("2026-01-15T09:30:00Z"), 100));

        Assert.Equal(3, bars.Count);
        Assert.False(bars[0].Complete);                      // troncata a sinistra
        Assert.True(bars[1].Complete);                       // intera
        Assert.False(bars[^1].Complete);                     // in formazione
    }

    /// <summary>
    /// In live il bucket in formazione non si spedisce: <c>Flush</c> non si chiama e
    /// <c>TryPush</c> restituisce solo bucket chiusi da una barra successiva. Un bucket a metà
    /// salvato nel feed è un dato falso che poi nessuno distingue da uno vero.
    /// </summary>
    [Fact]
    public void WithoutFlushOnlyClosedBucketsAreEmitted()
    {
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 60);
        var emitted = new List<AggregatedBar>();

        foreach (var minute in Minutes(Utc("2026-01-15T09:00:00Z"), 90))
        {
            if (aggregator.TryPush(minute, out var closed))
                emitted.Add(closed);
        }

        Assert.Single(emitted);
        Assert.Equal(Utc("2026-01-15T09:00:00Z"), emitted[0].Bar.DateTime);
        Assert.True(aggregator.HasPendingBucket);
    }

    [Fact]
    public void OutOfOrderInputIsRejected()
    {
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 60);
        aggregator.TryPush(Minute("2026-01-15T09:05:00Z", 100, 101, 99, 100), out _);

        Assert.Throws<ArgumentException>(
            () => aggregator.TryPush(Minute("2026-01-15T09:04:00Z", 100, 101, 99, 100), out _));
    }

    /// <summary>
    /// Il salto in avanti dell'ora legale: alle 02:00 locali di quella domenica l'orologio va alle
    /// 03:00 e l'ora fra le due non esiste. Contare in locale e riconvertire non ha quel caso; la
    /// scorciatoia <c>openUtc − resto</c> del Python lo scavalca e produce due bucket che si
    /// accavallano. Il controllo qui è che le etichette restino strettamente crescenti.
    /// </summary>
    [Fact]
    public void BucketsStayOrderedAcrossTheSpringForward()
    {
        // 29 marzo 2026: Europe/Rome passa da UTC+1 a UTC+2 alle 01:00 UTC.
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 240);

        var bars = aggregator.Aggregate(Minutes(Utc("2026-03-28T20:00:00Z"), 60 * 12));

        for (var i = 1; i < bars.Count; i++)
        {
            Assert.True(
                bars[i].Bar.DateTime > bars[i - 1].Bar.DateTime,
                $"Bucket non ordinati sul cambio d'ora: {bars[i - 1].Bar.DateTime:O} poi " +
                $"{bars[i].Bar.DateTime:O}.");
        }
    }

    /// <summary>Lo stesso, sul ritorno all'ora solare, dove l'ora locale esiste due volte.</summary>
    [Fact]
    public void BucketsStayOrderedAcrossTheFallBack()
    {
        // 25 ottobre 2026: Europe/Rome torna da UTC+2 a UTC+1 all'01:00 UTC.
        var aggregator = new BarAggregator(MarketCalendarRegistry.Current.Get("NQ"), 240);

        var bars = aggregator.Aggregate(Minutes(Utc("2026-10-24T20:00:00Z"), 60 * 12));

        for (var i = 1; i < bars.Count; i++)
        {
            Assert.True(
                bars[i].Bar.DateTime > bars[i - 1].Bar.DateTime,
                $"Bucket non ordinati sul cambio d'ora: {bars[i - 1].Bar.DateTime:O} poi " +
                $"{bars[i].Bar.DateTime:O}.");
        }
    }
}
