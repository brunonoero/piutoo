using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La maschera di negoziazione dentro l'aggregatore (7.5.0): i minuti in cui il CFD quota e il
/// future è chiuso non entrano nei bucket e non ne chiudono nessuno.
///
/// <para>Il caso è il DAX di FTMO: quota dalle 22:00 all'01:15 di Roma, l'Eurex no. Sul feed
/// pieno il bucket delle 21:00 conteneva tre ore di notte e quello dell'01:00 il quarto d'ora prima
/// dell'apertura. Misurato su <c>PT2_FDAX_PCH_001_240</c>, 09/2025-08/2026: −99.377 con quei minuti
/// dentro, −40.491 senza. Vedi <c>decisioni.md</c> 2026-09-16.</para>
/// </summary>
public sealed class BarAggregatorMaskTests
{
    private static DateTime Utc(string text) =>
        DateTime.Parse(text, null, System.Globalization.DateTimeStyles.AdjustToUniversal |
                                   System.Globalization.DateTimeStyles.AssumeUniversal);

    private static IEnumerable<OhlcvData> Minutes(DateTime fromUtc, DateTime toUtcExclusive, decimal price)
    {
        for (var t = fromUtc; t < toUtcExclusive; t = t.AddMinutes(1))
        {
            yield return new OhlcvData
            {
                DateTime = t, Open = price, High = price + 1, Low = price - 1, Close = price, Volume = 1
            };
        }
    }

    /// <summary>
    /// Mercoledì 14 gennaio 2026, Roma e Berlino in ora solare (UTC+1). Il feed CFD quota di
    /// continuo dalle 19:00 UTC (20:00 Roma) alle 04:00 UTC del giorno dopo (05:00 Roma). Eurex
    /// chiude con l'asta alle 22:0x di Berlino — la finestra chiude alle 22:10 locali = 21:10 UTC —
    /// e riapre alle 00:15 UTC.
    /// </summary>
    [Fact]
    public void MinutesOutsideTheEurexWindowDoNotEnterTheFdaxBuckets()
    {
        var calendar = MarketCalendarRegistry.Current.Get("FDAX");
        var aggregator = new BarAggregator(calendar, 240);

        // Prezzi diversi per fascia, così un minuto entrato dove non doveva si vede negli estremi.
        var input = Minutes(Utc("2026-01-14T19:00:00Z"), Utc("2026-01-14T21:10:00Z"), 100m)   // dentro, bucket 20:00Z
            .Concat(Minutes(Utc("2026-01-14T21:10:00Z"), Utc("2026-01-15T00:15:00Z"), 500m))   // notte del CFD: fuori
            .Concat(Minutes(Utc("2026-01-15T00:15:00Z"), Utc("2026-01-15T04:00:00Z"), 200m))   // dentro, bucket 00:00Z
            .Concat(Minutes(Utc("2026-01-15T04:00:00Z"), Utc("2026-01-15T04:05:00Z"), 300m));  // apre il bucket dopo

        var bars = aggregator.Aggregate(input).Where(bar => bar.Complete).ToList();

        Assert.Equal(185, aggregator.MaskedMinutes);   // 21:10 → 00:15 = 3h05m
        Assert.Equal(2, bars.Count);

        var evening = bars[0];
        Assert.Equal(Utc("2026-01-14T20:00:00Z"), evening.Bar.DateTime);   // bucket 21:00-01:00 Roma
        Assert.Equal(70, evening.MinuteCount);                             // solo 21:00-22:10 Roma (i minuti dalle 20:00 stanno nel bucket prima, troncato)
        Assert.Equal(101m, evening.Bar.High);                              // niente 500 dentro

        var night = bars[1];
        Assert.Equal(Utc("2026-01-15T00:00:00Z"), night.Bar.DateTime);     // bucket 01:00-05:00 Roma, etichetta prima dell'apertura
        Assert.Equal(225, night.MinuteCount);                              // 00:15 → 04:00
        Assert.Equal(200m, night.Bar.Open);                                // apre al primo minuto Eurex, non alle 00:00
        Assert.Equal(199m, night.Bar.Low);
    }

    /// <summary>Senza maschera dichiarata (mask: null) l'aggregatore è quello di prima: tutto entra.</summary>
    [Fact]
    public void WithoutAMaskEveryMinuteEnters()
    {
        var calendar = MarketCalendarRegistry.Current.Get("FDAX");
        var aggregator = new BarAggregator(new SessionGrid(calendar), 240, mask: null);

        var input = Minutes(Utc("2026-01-14T20:00:00Z"), Utc("2026-01-15T00:05:00Z"), 100m);
        var bars = aggregator.Aggregate(input).Where(bar => bar.Complete).ToList();

        Assert.Equal(0, aggregator.MaskedMinutes);
        Assert.Single(bars);
        Assert.Equal(240, bars[0].MinuteCount);
    }

    /// <summary>
    /// La regola sulle barre già piegate: una barra "tocca" la finestra se una parte del suo arco
    /// sta dentro. Il bucket 4h dell'01:00 di Roma apre alle 00:00 UTC, prima dell'apertura Eurex,
    /// ed è una barra vera; l'ora nativa delle 22:00 di Roma no.
    /// </summary>
    [Fact]
    public void OverlapKeepsTheBucketThatOpensBeforeTheWindowAndDropsTheDeadHour()
    {
        var mask = SessionMask.For("FDAX");

        Assert.True(mask.Overlaps(Utc("2026-01-15T00:00:00Z"), 240));   // 01:00 Roma, bucket 4h
        Assert.False(mask.Overlaps(Utc("2026-01-15T00:00:00Z"), 1));    // 01:00 Roma, minuto: ancora chiuso
        Assert.True(mask.Overlaps(Utc("2026-01-14T21:00:00Z"), 60));    // 22:00 Roma, ora nativa: porta l'asta di chiusura (fino alle 22:10)
        Assert.False(mask.Overlaps(Utc("2026-01-14T21:10:00Z"), 1));    // 22:10 Roma, minuto: chiuso
        Assert.False(mask.Overlaps(Utc("2026-01-14T22:00:00Z"), 60));   // 23:00 Roma, ora nativa: morta
        Assert.False(mask.Overlaps(Utc("2026-01-14T23:00:00Z"), 60));   // 00:00 Roma
        Assert.True(mask.Overlaps(Utc("2026-01-14T20:00:00Z"), 240));   // 21:00 Roma, bucket 4h
        Assert.True(mask.Overlaps(Utc("2026-01-15T00:00:00Z"), 60));    // 01:00 Roma, ora nativa: tocca dalle 01:15

        var bars = new[]
        {
            new OhlcvData { DateTime = Utc("2026-01-14T20:00:00Z") },
            new OhlcvData { DateTime = Utc("2026-01-14T21:00:00Z") },
            new OhlcvData { DateTime = Utc("2026-01-14T22:00:00Z") },
            new OhlcvData { DateTime = Utc("2026-01-14T23:00:00Z") },
            new OhlcvData { DateTime = Utc("2026-01-15T00:00:00Z") }
        };
        var kept = mask.DropOutsideWindow(bars, 60, out var dropped);
        Assert.Equal(2, dropped);
        Assert.Equal(
            [Utc("2026-01-14T20:00:00Z"), Utc("2026-01-14T21:00:00Z"), Utc("2026-01-15T00:00:00Z")],
            kept.Select(bar => bar.DateTime));

        // Senza finestra dichiarata non si toglie nulla: la maschera assente non spegne lo strumento.
        var undeclared = new SessionMask(new SymbolCalendar
        {
            Symbol = "TEST", ExchangeTimeZone = "Europe/Berlin", ResearchTimeZone = "Europe/Rome",
            SessionStart = new TimeOnly(0, 0)
        });
        Assert.Null(undeclared.Overlaps(Utc("2026-01-14T21:00:00Z"), 60));
        Assert.Same(bars, undeclared.DropOutsideWindow(bars, 60, out var none));
        Assert.Equal(0, none);
    }
}
