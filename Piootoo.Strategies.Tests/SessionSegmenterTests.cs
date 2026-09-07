using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Passo 2 del refactor: la segmentazione in sessioni, in un punto solo.
///
/// <para>È la funzione destinata a sostituire <c>EasyLib.FullDaySessionDay</c> e i sei helper di
/// <c>EasyEngineBase</c> che ricostruiscono la sessione dai timestamp con aritmetica a giorni di
/// calendario. I casi qui sotto sono quelli in cui quella aritmetica sbaglia.</para>
/// </summary>
public sealed class SessionSegmenterTests
{
    private static DateTime Utc(string text) =>
        DateTime.Parse(text, null, System.Globalization.DateTimeStyles.AdjustToUniversal |
                                   System.Globalization.DateTimeStyles.AssumeUniversal);

    private static AggregatedBar Bar(string utc, decimal price = 100m) =>
        new(
            new OhlcvData
            {
                DateTime = Utc(utc),
                Open = price,
                High = price + 1,
                Low = price - 1,
                Close = price,
                Volume = 1
            },
            MinuteCount: 60,
            Complete: true);

    private static SessionSegmenter For(string symbol, int timeframe) =>
        new(MarketCalendarRegistry.Current.Get(symbol), timeframe);

    /// <summary>
    /// Il confine di sessione è l'ancoraggio, non la mezzanotte UTC e non la mezzanotte locale.
    /// Su FDAX la barra che apre a mezzanotte locale appartiene alla sessione <b>precedente</b>: è
    /// il taglio <c>(timestamp − 1 min − session_start_hour)</c> del motore di ricerca, e la stessa
    /// regola che <c>EasyLib.OHLCMulti5</c> riproduce.
    /// </summary>
    [Theory]
    // FDAX, ancoraggio 01:00 Europe/Rome. In gennaio Roma è UTC+1.
    [InlineData("FDAX", "2026-01-15T00:00:00Z", "2026-01-15")]  // 01:00 locali: apre la sessione
    [InlineData("FDAX", "2026-01-14T23:00:00Z", "2026-01-14")]  // 00:00 locali: sessione precedente
    [InlineData("FDAX", "2026-01-15T12:00:00Z", "2026-01-15")]
    // NQ, ancoraggio 00:00 Europe/Rome.
    [InlineData("NQ", "2026-01-14T23:30:00Z", "2026-01-15")]    // 00:30 locali: sessione nuova
    [InlineData("NQ", "2026-01-14T22:30:00Z", "2026-01-14")]    // 23:30 locali: ancora la vecchia
    public void SessionIdFollowsTheAnchorNotTheCalendarDay(
        string symbol, string barUtc, string expectedSessionDay)
    {
        var segmenter = For(symbol, 60);

        var context = segmenter.Next(Bar(barUtc));

        Assert.Equal(DateTime.Parse(expectedSessionDay), context.SessionId);
    }

    [Fact]
    public void SessionOpenAndMinutesSinceOpenAreMeasuredFromTheAnchor()
    {
        var segmenter = For("NQ", 60);

        var context = segmenter.Next(Bar("2026-01-15T02:00:00Z"));

        // Ancoraggio 00:00 Europe/Rome; in gennaio Roma e' UTC+1, quindi la sessione apre alle
        // 23:00 UTC del giorno prima.
        Assert.Equal(Utc("2026-01-14T23:00:00Z"), context.SessionOpenUtc);
        Assert.Equal(180, context.MinutesSinceSessionOpen);
    }

    [Fact]
    public void BarIndexRestartsWithEachSession()
    {
        var segmenter = For("NQ", 60);

        // Tre barre nella sessione del 15, poi la prima del 16.
        var first = segmenter.Next(Bar("2026-01-14T23:00:00Z"));
        var second = segmenter.Next(Bar("2026-01-15T00:00:00Z"));
        var third = segmenter.Next(Bar("2026-01-15T01:00:00Z"));
        var newSession = segmenter.Next(Bar("2026-01-15T23:00:00Z"));

        Assert.Equal(1, first.BarIndexInSession);
        Assert.True(first.StartsNewSession);
        Assert.Equal(2, second.BarIndexInSession);
        Assert.Equal(3, third.BarIndexInSession);
        Assert.Equal(1, newSession.BarIndexInSession);
        Assert.True(newSession.StartsNewSession);
        Assert.NotEqual(third.SessionId, newSession.SessionId);
    }

    /// <summary>
    /// Il campo che permette a un CFD di non fabbricare la sessione domenicale che il future non
    /// ha. Non è "ignorare la domenica": su NQ la domenica è una sessione vera, su FDAX non esiste
    /// mai, e il calendario lo dice simbolo per simbolo.
    /// </summary>
    [Theory]
    // 2026-01-18 è una domenica.
    [InlineData("NQ", "2026-01-17T23:30:00Z", true)]
    [InlineData("FDAX", "2026-01-18T06:00:00Z", false)]
    // 2026-01-17 è un sabato: nessuno dei due, salvo SB.
    [InlineData("NQ", "2026-01-16T23:30:00Z", false)]
    [InlineData("SB", "2026-01-17T06:00:00Z", true)]
    public void SessionDayIsReadFromTheCalendar(string symbol, string barUtc, bool expected)
    {
        var segmenter = For(symbol, 60);

        var context = segmenter.Next(Bar(barUtc));

        Assert.Equal(expected, context.IsSessionDay);
    }

    /// <summary>
    /// Un simbolo di cui il dossier non dichiara i giorni resta <c>null</c>: chi legge non deve
    /// inventarli. Trattare "non dichiarato" come "nessun giorno" spegnerebbe lo strumento.
    /// </summary>
    [Fact]
    public void UndeclaredSessionDaysStayNull()
    {
        var segmenter = For("MES", 60);

        var context = segmenter.Next(Bar("2026-01-18T06:00:00Z"));

        Assert.Null(context.IsSessionDay);
    }

    /// <summary>
    /// Il buco si dichiara, non si riempie. Il layer non distingue una pausa di mercato da un invio
    /// perso — e non deve: dice che le due barre non sono contigue, e chi consuma decide.
    /// </summary>
    [Fact]
    public void AGapBetweenBarsIsFlagged()
    {
        var segmenter = For("NQ", 60);

        var first = segmenter.Next(Bar("2026-01-15T08:00:00Z"));
        var contiguous = segmenter.Next(Bar("2026-01-15T09:00:00Z"));
        var afterGap = segmenter.Next(Bar("2026-01-15T13:00:00Z"));

        Assert.True(first.IsFirstBarAfterGap);   // della prima non si sa cosa c'era prima
        Assert.False(contiguous.IsFirstBarAfterGap);
        Assert.True(afterGap.IsFirstBarAfterGap);
    }

    [Fact]
    public void PreviousSessionCloseIsTheLastBarOfTheSessionBefore()
    {
        var segmenter = For("NQ", 60);

        segmenter.Next(Bar("2026-01-14T23:00:00Z"));
        var last = segmenter.Next(Bar("2026-01-15T22:00:00Z"));
        var newSession = segmenter.Next(Bar("2026-01-15T23:00:00Z"));

        Assert.Equal(last.OpenUtc, newSession.PreviousSessionCloseUtc);
    }

    [Fact]
    public void TheFirstSessionHasNoPreviousClose()
    {
        var segmenter = For("NQ", 60);

        var first = segmenter.Next(Bar("2026-01-14T23:00:00Z"));

        Assert.Null(first.PreviousSessionCloseUtc);
    }

    /// <summary>
    /// OHLC tutti uguali: su un CFD è tipicamente una quotazione ferma, non un mercato fermo. Si
    /// marca e si lascia passare — la strategia decide se ignorarla, l'analisi vuole vederla.
    /// </summary>
    [Fact]
    public void FlatBarsAreMarkedStaleNotDropped()
    {
        var segmenter = For("NQ", 60);
        var flat = new AggregatedBar(
            new OhlcvData
            {
                DateTime = Utc("2026-01-15T08:00:00Z"),
                Open = 100, High = 100, Low = 100, Close = 100, Volume = 1
            },
            MinuteCount: 60,
            Complete: true);

        var context = segmenter.Next(flat);

        Assert.Equal(BarQualityFlags.Stale, context.Quality);
        Assert.Equal(100, context.Bar.Close);
    }

    [Fact]
    public void OutOfOrderBarsAreRejected()
    {
        var segmenter = For("NQ", 60);
        segmenter.Next(Bar("2026-01-15T09:00:00Z"));

        Assert.Throws<ArgumentException>(() => segmenter.Next(Bar("2026-01-15T08:00:00Z")));
    }

    /// <summary>
    /// L'aggregatore e il segmentatore devono vedere la stessa griglia: la sessione di una barra
    /// aggregata comincia sempre a o prima della sua apertura, mai dopo. Se le due divergessero,
    /// <c>MinutesSinceSessionOpen</c> uscirebbe negativo — che è il modo in cui un ancoraggio
    /// sbagliato si manifesta per primo.
    /// </summary>
    [Theory]
    [InlineData("FDAX", 240)]
    [InlineData("NQ", 240)]
    [InlineData("NQ", 60)]
    [InlineData("CC", 240)]
    public void AggregatorAndSegmenterShareTheSameGrid(string symbol, int timeframe)
    {
        var calendar = MarketCalendarRegistry.Current.Get(symbol);
        var minutes = new List<OhlcvData>();
        for (var i = 0; i < 60 * 24 * 9; i++)
        {
            minutes.Add(new OhlcvData
            {
                DateTime = Utc("2026-01-12T00:00:00Z").AddMinutes(i),
                Open = 100 + i % 17, High = 110, Low = 90, Close = 100, Volume = 1
            });
        }

        var bars = new BarAggregator(calendar, timeframe).Aggregate(minutes);
        var contexts = new SessionSegmenter(calendar, timeframe).Segment(bars);

        foreach (var context in contexts)
        {
            Assert.True(
                context.MinutesSinceSessionOpen >= 0,
                $"{symbol}/{timeframe}m: la barra {context.OpenUtc:O} sta {context.MinutesSinceSessionOpen} " +
                $"minuti PRIMA dell'apertura della sua sessione ({context.SessionOpenUtc:O}).");
            Assert.True(context.MinutesSinceSessionOpen < 1440);
        }
    }
}
