using System.Globalization;
using System.Text.Json;
using Piootoo.Domain.Repositories;

namespace Piootoo.Strategies.Tests;

public sealed class DataSourceRepositoryHierarchyTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-datafeed-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ReadsFeedWorkerHierarchyByTimeframeAndSymbol()
    {
        WriteFeed("1h", "@GC", new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc), 4100);
        var repository = new DataSourceRepository(_root);

        var candles = await repository.LoadDataRangeAsync(
            "@GC",
            new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc),
            "OneHour",
            preferCalculated: false);

        var candle = Assert.Single(candles);
        Assert.Equal(4100m, candle.Open);
        Assert.Contains("@GC", repository.GetAvailableSymbols());
        Assert.Contains("OneHour", repository.GetAvailableBarTypes("@GC"));
    }

    [Fact]
    public async Task PrefersCalculatedFolderWhenRequested()
    {
        var time = new DateTime(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc);
        WriteFeed("1h", "@GC", time, 4100);
        WriteFeed("1h-calculate", "@GC", time, 4200);
        var repository = new DataSourceRepository(_root);

        var candles = await repository.LoadAllDataAsync("@GC", "OneHour");

        Assert.Equal(4200m, Assert.Single(candles).Open);
    }

    /// <summary>
    /// Dichiara l'orologio del feed sintetico. Senza, <c>DataSourceRepository</c> si rifiuta di
    /// leggerlo — ed e' voluto: un feed di fuso ignoto verrebbe interpretato come UTC, che per i
    /// feed veri di questo repository e' falso. Qui <c>UTC</c> e' la dichiarazione giusta, perche'
    /// gli istanti del feed sintetico sono costruiti in UTC.
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

    private void WriteFeed(string timeframe, string symbol, DateTime dateTime, decimal open)
    {
        WriteFeedClocks(symbol);
        var directory = Path.Combine(_root, timeframe, symbol);
        Directory.CreateDirectory(directory);
        var payload = new
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
        File.WriteAllText(
            Path.Combine(directory, $"{symbol}-{dateTime:yyyyMMdd}.json"),
            JsonSerializer.Serialize(payload));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
