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
            // Segnale opposto: chiude la posizione aperta sopra. Le strategie non emettono più
            // segnali di sola chiusura.
            StrategyName = "Strategy", Price = 101m, Quantity = 1m
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

    /// <summary>
    /// Il journal e' un dettaglio interno: chi legge dallo store non deve accorgersene. Ordine
    /// dell'array preservato, record nuovi in coda, e per un id gia' presente vince l'ultima
    /// versione appesa — cioe' esattamente cio' che avrebbe prodotto la riscrittura completa.
    /// </summary>
    [Fact]
    public void AppendedSignals_AreMergedOnRead_WithLastVersionWinningAndOrderPreserved()
    {
        var store = new TradingJsonStore(_root);
        store.Initialize();
        store.WriteSignals([Signal("a"), Signal("b")]);

        store.AppendSignals([Signal("c")]);
        store.AppendSignals([Signal("b", quantity: 7m), Signal("d")]);

        var read = store.ReadSignals();
        Assert.Equal(new[] { "a", "b", "c", "d" }, read.Select(x => x.SignalId).ToArray());
        Assert.Equal(7m, read.Single(x => x.SignalId == "b").Quantity);

        // Letto una volta, il journal e' stato materializzato e non esiste piu'.
        Assert.False(File.Exists(store.SignalsPath + "l"));
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(File.ReadAllText(store.SignalsPath)).RootElement.ValueKind);
    }

    /// <summary>
    /// Una scrittura completa e' autorevole: parte dallo stato completo in memoria, quindi il
    /// journal accumulato fino a quel momento va buttato. Tenerlo rifonderebbe versioni vecchie
    /// sopra quelle appena scritte.
    /// </summary>
    [Fact]
    public void FullWrite_DiscardsThePendingJournal()
    {
        var store = new TradingJsonStore(_root);
        store.Initialize();
        store.AppendSignals([Signal("a", quantity: 1m)]);
        store.WriteSignals([Signal("a", quantity: 99m)]);

        Assert.False(File.Exists(store.SignalsPath + "l"));
        Assert.Equal(99m, Assert.Single(store.ReadSignals()).Quantity);
    }

    /// <summary>Un journal senza array di partenza non perde niente: i record diventano l'array.</summary>
    [Fact]
    public void CompactAll_MaterializesWithoutAPriorRead()
    {
        var store = new TradingJsonStore(_root);
        store.Initialize();
        store.AppendSignals([Signal("a"), Signal("b")]);

        store.CompactAll();

        Assert.False(File.Exists(store.SignalsPath + "l"));
        using var document = JsonDocument.Parse(File.ReadAllText(store.SignalsPath));
        Assert.Equal(2, document.RootElement.EnumerateArray().Count());
    }

    private static PersistedSignal Signal(string id, decimal quantity = 1m) => new()
    {
        SignalId = id,
        TimestampUtc = new DateTime(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc),
        StrategyCode = "s",
        StrategyName = "Strategy",
        Symbol = "NQ",
        Side = SignalType.Buy,
        OrderType = TradeOrderType.Market,
        TriggerPrice = 100m,
        Quantity = quantity
    };

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
