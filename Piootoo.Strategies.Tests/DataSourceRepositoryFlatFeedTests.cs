using System.Globalization;
using System.Text.Json;
using Piootoo.Domain.Repositories;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il layout piatto del datafeed: un file per coppia (simbolo, timeframe) nella radice,
/// <c>@GC_60.json</c>, prodotto da <c>datafeed-future/aggregate_flat_feed.py</c>.
/// </summary>
public sealed class DataSourceRepositoryFlatFeedTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-datafeed-flat-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ReadsFlatFeedBySymbolAndTimeframeMinutes()
    {
        WriteFlatFeed("@GC", 60, new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 4100);
        var repository = new DataSourceRepository(_root);

        var candles = await repository.LoadDataRangeAsync(
            "@GC",
            new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc),
            "OneHour");

        Assert.Equal(4100m, Assert.Single(candles).Open);
        Assert.Contains("@GC", repository.GetAvailableSymbols());
        Assert.Contains("OneHour", repository.GetAvailableBarTypes("@GC"));
    }

    /// <summary>
    /// Il file piatto vince sulla vecchia gerarchia, e non e' una preferenza arbitraria: il
    /// timestamp del CSV sorgente e' la <i>fine</i> del minuto, che il vecchio aggregatore
    /// trattava come inizio. Finche' le due strutture convivono, leggere quella sbagliata
    /// significherebbe fare backtest su barre sfasate di un minuto senza alcun segnale.
    /// </summary>
    [Fact]
    public async Task FlatFeedWinsOverHierarchy()
    {
        var time = new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc);
        WriteHierarchicalFeed("1h", "@GC", time, 4200);
        WriteFlatFeed("@GC", 60, time, 4100);
        var repository = new DataSourceRepository(_root);

        var candles = await repository.LoadAllDataAsync("@GC", "OneHour");

        Assert.Equal(4100m, Assert.Single(candles).Open);
    }

    /// <summary>
    /// Durante la conversione le due strutture convivono: la console deve elencare anche i
    /// simboli non ancora convertiti, altrimenti spariscono senza che nessuno se ne accorga.
    /// </summary>
    [Fact]
    public void AvailableSymbolsUnionsBothLayouts()
    {
        var time = new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc);
        WriteFlatFeed("@NQ", 15, time, 20000);
        WriteHierarchicalFeed("4h", "@CL", time, 70);
        var repository = new DataSourceRepository(_root);

        var symbols = repository.GetAvailableSymbols().ToList();

        Assert.Contains("@NQ", symbols);
        Assert.Contains("@CL", symbols);
        Assert.Equal(new[] { "FifteenMinute" }, repository.GetAvailableBarTypes("@NQ"));
        Assert.Equal(new[] { "FourHour" }, repository.GetAvailableBarTypes("@CL"));
    }

    /// <summary>
    /// Un file che non segue la convenzione non deve diventare un simbolo fantasma nella
    /// tendina della console: <c>feed-clocks.json</c> vive nella stessa cartella.
    /// </summary>
    [Fact]
    public void IgnoresFilesThatAreNotFlatFeeds()
    {
        WriteFlatFeed("@NQ", 15, new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 20000);
        File.WriteAllText(Path.Combine(_root, "@non_un_feed.json"), "{}");

        var symbols = new DataSourceRepository(_root).GetAvailableSymbols().ToList();

        Assert.Equal(new[] { "@NQ" }, symbols);
    }

    /// <summary>
    /// Dichiara l'orologio del feed sintetico. Senza, <c>DataSourceRepository</c> si rifiuta di
    /// leggerlo — ed e' voluto: un feed di fuso ignoto verrebbe interpretato come UTC. Qui
    /// <c>UTC</c> e' la dichiarazione giusta, come per il feed vero: i file piatti contengono
    /// gia' istanti UTC, perche' l'ora legale del CSV sorgente e' risolta in conversione.
    /// </summary>
    private void WriteFeedClocks(string symbol)
    {
        Directory.CreateDirectory(_root);
        var manifest = Path.Combine(_root, "feed-clocks.json");
        var orologi = new Dictionary<string, string> { [symbol] = "UTC" };
        if (File.Exists(manifest))
        {
            var esistenti = JsonSerializer.Deserialize<ManifestDto>(File.ReadAllText(manifest));
            foreach (var voce in esistenti?.Orologi ?? new Dictionary<string, string>())
                orologi[voce.Key] = voce.Value;
        }

        File.WriteAllText(manifest, JsonSerializer.Serialize(new ManifestDto { Orologi = orologi }));
    }

    private sealed class ManifestDto
    {
        public Dictionary<string, string>? Orologi { get; set; }
    }

    private void WriteFlatFeed(string symbol, int timeframeMinutes, DateTime dateTime, decimal open)
    {
        WriteFeedClocks(symbol);
        File.WriteAllText(
            Path.Combine(_root, $"{symbol}_{timeframeMinutes}.json"),
            JsonSerializer.Serialize(BuildPayload(symbol, dateTime, open)));
    }

    private void WriteHierarchicalFeed(string timeframeFolder, string symbol, DateTime dateTime, decimal open)
    {
        WriteFeedClocks(symbol);
        var directory = Path.Combine(_root, timeframeFolder, symbol);
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, $"{symbol}-{dateTime:yyyyMMdd}.json"),
            JsonSerializer.Serialize(BuildPayload(symbol, dateTime, open)));
    }

    private static object BuildPayload(string symbol, DateTime dateTime, decimal open) => new
    {
        symbol,
        barType = "OneHour",
        lastUpdate = dateTime,
        candleCount = 1,
        candles = new[]
        {
            new
            {
                // L'istante va costruito dichiarando UTC: `new DateTimeOffset(dateTime)` su un
                // `Kind` non specificato lo leggerebbe nel fuso della macchina, e il feed
                // sintetico cambierebbe timestamp secondo dove gira il test.
                timestamp = new DateTimeOffset(dateTime, TimeSpan.Zero).ToUnixTimeSeconds(),
                dateTime,
                dateTimeFormatted = dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                open,
                high = open + 1,
                low = open - 1,
                close = open + 0.5m,
                volume = 100
            }
        }
    };

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
