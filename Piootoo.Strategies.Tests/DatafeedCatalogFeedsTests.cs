using System.Globalization;
using System.Text;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'elenco dei feed con il periodo che coprono, quello che la console mostra prima di lanciare
/// un backtest. Due cose contano: che il range venga dalle barre e non dal filesystem, e che
/// leggerlo non costi come caricare il feed.
/// </summary>
public sealed class DatafeedCatalogFeedsTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-datafeed-catalog-tests", Guid.NewGuid().ToString("N"));

    private string Internal => Path.Combine(_root, "datafeed");

    private string External => Path.Combine(_root, "datafeed-external");

    [Fact]
    public void ReadsRangeFromFirstAndLastCandle()
    {
        WriteClocks(Internal, ("NQ", "UTC"));
        WriteFlatFeed(Internal, "@NQ", 15, new DateTime(2020, 1, 2, 9, 0, 0), bars: 500);

        var feed = Assert.Single(Catalog().GetFeeds(null));

        Assert.Equal("@NQ", feed.Symbol);
        Assert.Equal(15, feed.TimeframeMinutes);
        Assert.Equal(new DateTime(2020, 1, 2, 9, 0, 0, DateTimeKind.Utc), feed.FirstBarUtc);
        Assert.Equal(new DateTime(2020, 1, 2, 9, 0, 0, DateTimeKind.Utc).AddMinutes(15 * 499), feed.LastBarUtc);
        Assert.Equal(500, feed.CandleCount);
        Assert.Equal("UTC", feed.FeedClock);
        Assert.Equal(DatafeedCatalog.InternalLabel, feed.Source);
        Assert.Null(feed.Broker);
        Assert.Null(feed.Problem);
    }

    /// <summary>
    /// Il timestamp nel file è un orario di parete nell'orologio dichiarato, non un istante: su un
    /// feed dichiarato <c>Europe/Rome</c> la prima barra di gennaio sta un'ora indietro in UTC.
    /// Mostrare l'etichetta grezza qui vorrebbe dire dare un periodo che non è quello che il
    /// backtest vedrà.
    /// </summary>
    [Fact]
    public void ConvertsRangeWithTheDeclaredFeedClock()
    {
        WriteClocks(Internal, ("GC", "Europe/Rome"));
        WriteFlatFeed(Internal, "@GC", 60, new DateTime(2020, 1, 2, 9, 0, 0), bars: 2);

        var feed = Assert.Single(Catalog().GetFeeds(null));

        Assert.Equal(new DateTime(2020, 1, 2, 8, 0, 0, DateTimeKind.Utc), feed.FirstBarUtc);
        Assert.Equal("Europe/Rome", feed.FeedClock);
    }

    /// <summary>
    /// Un feed non dichiarato non fa fallire l'elenco — sparirebbe dalla vista proprio mentre lo
    /// si sta cercando — ma compare senza fuso e con la nota che lo dice.
    /// </summary>
    [Fact]
    public void UndeclaredClockIsReportedNotHidden()
    {
        WriteClocks(Internal, ("NQ", "UTC"));
        WriteFlatFeed(Internal, "@ES", 30, new DateTime(2021, 3, 1, 0, 0, 0), bars: 3);

        var feed = Assert.Single(Catalog().GetFeeds(null));

        Assert.Null(feed.FeedClock);
        Assert.NotNull(feed.Problem);
        Assert.Contains(FeedClockRegistry.ManifestFileName, feed.Problem);
    }

    /// <summary>
    /// Interno ed esterno hanno gli stessi simboli con prezzi diversi: l'elenco li tiene insieme
    /// ma ogni riga dichiara da quale archivio viene, perché un run ne legge uno solo.
    /// </summary>
    [Fact]
    public void AllFeedsDeclareTheirArchive()
    {
        WriteClocks(Internal, ("NQ", "UTC"));
        WriteFlatFeed(Internal, "@NQ", 15, new DateTime(2020, 1, 2, 9, 0, 0), bars: 10);
        var broker = Path.Combine(External, "RAWTRADINGLTD");
        WriteClocks(broker, ("NQ", "UTC"));
        WriteFlatFeed(broker, "@NQ", 15, new DateTime(2024, 6, 13, 0, 0, 0), bars: 10);

        var feeds = Catalog().GetAllFeeds();

        Assert.Equal(2, feeds.Count);
        var internalFeed = Assert.Single(feeds, feed => feed.Broker == null);
        var externalFeed = Assert.Single(feeds, feed => feed.Broker == "RAWTRADINGLTD");
        Assert.Equal("esterno/RAWTRADINGLTD", externalFeed.Source);
        Assert.NotEqual(internalFeed.FirstBarUtc, externalFeed.FirstBarUtc);
    }

    /// <summary>Un file estraneo lasciato nella cartella si ignora, non diventa una riga vuota.</summary>
    [Fact]
    public void IgnoresFilesOutsideTheNamingConvention()
    {
        WriteClocks(Internal, ("NQ", "UTC"));
        WriteFlatFeed(Internal, "@NQ", 15, new DateTime(2020, 1, 2, 9, 0, 0), bars: 2);
        File.WriteAllText(Path.Combine(Internal, "@appunti_vecchi.json"), "{}");

        Assert.Single(Catalog().GetFeeds(null));
    }

    /// <summary>
    /// Il range si legge in testa e in coda, non scorrendo il file: un feed grande deve costare
    /// come uno piccolo, altrimenti popolare l'elenco costa più del backtest che prepara.
    /// </summary>
    [Fact]
    public void DoesNotReadTheWholeFile()
    {
        WriteClocks(Internal, ("NQ", "UTC"));
        WriteFlatFeed(Internal, "@NQ", 15, new DateTime(2010, 1, 4, 9, 0, 0), bars: 200_000);
        var size = new FileInfo(Path.Combine(Internal, "@NQ_15.json")).Length;
        Assert.True(size > 8 * 1024 * 1024, $"Il feed di prova deve essere grande, è {size} byte.");

        var start = DateTime.UtcNow;
        var feed = Assert.Single(Catalog().GetFeeds(null));
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(200_000, feed.CandleCount);
        Assert.Equal(new DateTime(2010, 1, 4, 9, 0, 0, DateTimeKind.Utc), feed.FirstBarUtc);
        Assert.True(elapsed < TimeSpan.FromSeconds(2), $"Letto in {elapsed.TotalSeconds:F1} s: sta scorrendo il file.");
    }

    private DatafeedCatalog Catalog() => new(new PiootooSettings
    {
        RepositoryPath = Internal,
        ExternalRepositoryPath = External
    });

    private static void WriteClocks(string root, params (string Symbol, string Zone)[] clocks)
    {
        Directory.CreateDirectory(root);
        var entries = string.Join(",", clocks.Select(clock => $"\"{clock.Symbol}\":\"{clock.Zone}\""));
        File.WriteAllText(
            Path.Combine(root, FeedClockRegistry.ManifestFileName),
            $"{{\"orologi\":{{{entries}}}}}");
    }

    private static void WriteFlatFeed(string root, string symbol, int timeframeMinutes, DateTime start, int bars)
    {
        Directory.CreateDirectory(root);
        var builder = new StringBuilder();
        builder.Append(
            $"{{\"symbol\":\"{symbol}\",\"timeframeMinutes\":{timeframeMinutes},\"lastUpdate\":\"2026-08-30T00:00:00Z\",\"candles\":[");
        for (var index = 0; index < bars; index++)
        {
            var time = start.AddMinutes(timeframeMinutes * index);
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append("\n{\"timestamp\":0,\"dateTime\":\"")
                .Append(time.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
                .Append("\",\"dateTimeFormatted\":\"")
                .Append(time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .Append("\",\"open\":1,\"high\":2,\"low\":0.5,\"close\":1.5,\"volume\":10}");
        }

        builder.Append($"],\"candleCount\":{bars}}}");
        File.WriteAllText(Path.Combine(root, $"{symbol}_{timeframeMinutes}.json"), builder.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
