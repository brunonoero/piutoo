using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

/// <summary>Materializza array JSON deduplicati usando una sostituzione atomica.</summary>
public sealed class TradingJsonStore
{
    private static readonly ConcurrentDictionary<string, object> Gates =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _directory;

    public TradingJsonStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_directory);
    }

    public string SignalsPath => Path.Combine(_directory, TradingPersistenceSchema.SignalsFileName);
    public string TradesPath => Path.Combine(_directory, TradingPersistenceSchema.TradesFileName);
    public string RotationLogPath => Path.Combine(_directory, DiagnosticsSchema.RotationLogFileName);

    public void Initialize()
    {
        WriteSignals([]);
        WriteTrades([]);
        WriteRotationLog([]);
    }

    public IReadOnlyList<PersistedSignal> ReadSignals() => Read<PersistedSignal>(SignalsPath);
    public IReadOnlyList<PersistedTrade> ReadTrades() => Read<PersistedTrade>(TradesPath);
    public IReadOnlyList<RotationLogEntry> ReadRotationLog() => Read<RotationLogEntry>(RotationLogPath);

    public void UpsertSignals(IEnumerable<PersistedSignal> values)
    {
        var materialized = values.ToArray();
        ValidateSignals(materialized);
        Upsert(SignalsPath, materialized, value => value.SignalId);
    }

    public void UpsertTrades(IEnumerable<PersistedTrade> values)
    {
        var materialized = values.ToArray();
        ValidateTrades(materialized);
        Upsert(TradesPath, materialized, value => value.TradeId);
    }

    /// <param name="durable">
    /// true (default) forza la sincronizzazione su disco: usarlo per la scrittura finale.
    /// false mantiene l'atomicità verso i lettori ma non blocca sul flush: è la modalità dei
    /// checkpoint intermedi di un backtest, dove un fsync per barra costerebbe più dell'intero
    /// calcolo.
    /// </param>
    public void WriteSignals(IEnumerable<PersistedSignal> values, bool durable = true)
    {
        var materialized = values.ToArray();
        ValidateSignals(materialized);
        WriteDistinct(SignalsPath, materialized, value => value.SignalId, durable);
    }

    /// <inheritdoc cref="WriteSignals(System.Collections.Generic.IEnumerable{Piootoo.Shared.Models.Trading.PersistedSignal},bool)"/>
    public void WriteTrades(IEnumerable<PersistedTrade> values, bool durable = true)
    {
        var materialized = values.ToArray();
        ValidateTrades(materialized);
        WriteDistinct(TradesPath, materialized, value => value.TradeId, durable);
    }

    /// <summary>Sostituisce l'intero log di rotazione (una riga per barra) con l'elenco fornito, deduplicato per EntryId.</summary>
    public void WriteRotationLog(IEnumerable<RotationLogEntry> values, bool durable = true) =>
        WriteDistinct(RotationLogPath, values.ToArray(), value => value.EntryId, durable);

    private static void ValidateSignals(IEnumerable<PersistedSignal> values)
    {
        foreach (var value in values)
            if (string.IsNullOrWhiteSpace(value.StrategyCode))
                throw new InvalidDataException($"Signal '{value.SignalId}' privo di StrategyCode.");
    }

    private static void ValidateTrades(IEnumerable<PersistedTrade> values)
    {
        foreach (var value in values)
            if (string.IsNullOrWhiteSpace(value.StrategyCode))
                throw new InvalidDataException($"Trade '{value.TradeId}' privo di StrategyCode.");
    }

    private static IReadOnlyList<T> Read<T>(string path)
    {
        lock (Gates.GetOrAdd(path, _ => new object()))
            return ReadUnsafe<T>(path);
    }

    private static List<T> ReadUnsafe<T>(string path)
    {
        if (!File.Exists(path)) return [];
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), JsonOptions) ?? [];
    }

    private static void Upsert<T>(string path, IEnumerable<T> values, Func<T, string> key)
    {
        lock (Gates.GetOrAdd(path, _ => new object()))
        {
            var byId = ReadUnsafe<T>(path).ToDictionary(key, StringComparer.Ordinal);
            foreach (var value in values) byId[key(value)] = value;
            WriteAtomic(path, byId.Values, durable: true);
        }
    }

    private static void WriteDistinct<T>(string path, IEnumerable<T> values, Func<T, string> key, bool durable)
    {
        lock (Gates.GetOrAdd(path, _ => new object()))
            WriteAtomic(path, values.DistinctBy(key), durable);
    }

    private static void WriteAtomic<T>(string path, IEnumerable<T> values, bool durable = true)
    {
        AtomicFileWriter.Write(path, stream => JsonSerializer.Serialize(stream, values, JsonOptions), durable);
    }
}
