using System.Text.Json;
using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Strategies.Tests;

public sealed class TradingJsonStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-json-{Guid.NewGuid():N}");

    [Fact]
    public void SignalWithoutFill_IsNotATrade_AndFilesAreSeparateArrays()
    {
        var store = new TradingJsonStore(_root);
        store.Initialize();
        store.UpsertSignals([Signal("signal-1")]);

        Assert.Single(store.ReadSignals());
        Assert.Empty(store.ReadTrades());
        Assert.NotEqual(store.SignalsPath, store.TradesPath);
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(File.ReadAllText(store.SignalsPath)).RootElement.ValueKind);
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(File.ReadAllText(store.TradesPath)).RootElement.ValueKind);
    }

    [Fact]
    public void ReplaysAreDeduplicated_AndRepeatedWritesRemainValidJson()
    {
        var store = new TradingJsonStore(_root);
        store.Initialize();
        store.UpsertSignals([Signal("same"), Signal("same")]);
        store.UpsertSignals([Signal("same")]);

        Assert.Single(store.ReadSignals());
        using var document = JsonDocument.Parse(File.ReadAllText(store.SignalsPath));
        Assert.Single(document.RootElement.EnumerateArray());
    }

    [Fact]
    public void FilledRoundTrip_ProducesOneClosedTrade()
    {
        var engine = new PiootooTradingService();
        engine.Initialize(10_000m, 2m);
        var time = new DateTime(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);
        var prices = new Dictionary<string, decimal> { ["NQ"] = 100m };
        engine.ProcessSignals([new TradeSignal
        {
            Date = time, Type = SignalType.Buy, Symbol = "NQ", StrategyCode = "s",
            StrategyName = "Strategy", Price = 100m, Quantity = 1m
        }], prices, time);
        engine.ProcessSignals([new TradeSignal
        {
            Date = time.AddMinutes(1), Type = SignalType.Sell, Symbol = "NQ", StrategyCode = "s",
            StrategyName = "Strategy", Price = 101m, Quantity = 1m, CloseOnly = true
        }], new Dictionary<string, decimal> { ["NQ"] = 101m }, time.AddMinutes(1));

        Assert.Single(engine.GetClosedTrades());
    }

    [Fact]
    public void StoreDoesNotEscapeItsServerSelectedDirectory()
    {
        var sessionDirectory = Path.Combine(_root, "workspace-a", "sessions", "session-1");
        var store = new TradingJsonStore(sessionDirectory);
        store.Initialize();

        Assert.StartsWith(Path.GetFullPath(sessionDirectory), store.SignalsPath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(Path.GetFullPath(sessionDirectory), store.TradesPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConcurrentReadersAndWriters_AlwaysObserveValidJson()
    {
        var store = new TradingJsonStore(_root);
        store.Initialize();
        var readErrors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

        var writer = Task.Run(() =>
        {
            for (var index = 0; index < 50; index++)
                store.WriteSignals(Enumerable.Range(0, index + 1).Select(i => Signal($"signal-{i}")));
        });
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!writer.IsCompleted)
            {
                try
                {
                    using var stream = AtomicFileWriter.OpenReadShared(store.SignalsPath);
                    using var document = JsonDocument.Parse(stream);
                    Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
                }
                catch (Exception exception)
                {
                    readErrors.Enqueue(exception);
                }
            }
        }));

        await Task.WhenAll(readers.Append(writer));

        Assert.Empty(readErrors);
        Assert.Equal(50, store.ReadSignals().Count);
        Assert.Empty(Directory.EnumerateFiles(_root, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void AtomicWriter_CreatesMissingDirectory_AndOverwrites()
    {
        var path = Path.Combine(_root, "missing", "artifact.json");

        AtomicFileWriter.WriteAllText(path, "[1]");
        AtomicFileWriter.WriteAllText(path, "[2]");

        Assert.Equal("[2]", File.ReadAllText(path));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public async Task AtomicWriter_RetriesTransientWindowsSharingViolation()
    {
        var path = Path.Combine(_root, "locked.json");
        AtomicFileWriter.WriteAllText(path, "[1]");
        using var exclusiveReader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        var write = Task.Run(() => AtomicFileWriter.WriteAllText(path, "[2]"));
        await Task.Delay(150);
        exclusiveReader.Dispose();
        await write;

        Assert.Equal("[2]", File.ReadAllText(path));
        Assert.Empty(Directory.EnumerateFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task AtomicWriter_SerializesConcurrentWritersForSamePath()
    {
        var path = Path.Combine(_root, "concurrent.json");
        AtomicFileWriter.WriteAllText(path, "[0]");

        var writers = Enumerable.Range(1, 20)
            .Select(value => Task.Run(() => AtomicFileWriter.WriteAllText(path, $"[{value}]")));
        await Task.WhenAll(writers);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Empty(Directory.EnumerateFiles(_root, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    private static PersistedSignal Signal(string id) => new()
    {
        SignalId = id,
        TimestampUtc = new DateTime(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc),
        StrategyCode = "s",
        StrategyName = "Strategy",
        Symbol = "NQ",
        Side = SignalType.Buy,
        OrderType = TradeOrderType.Market,
        TriggerPrice = 100m,
        Quantity = 1m
    };

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
