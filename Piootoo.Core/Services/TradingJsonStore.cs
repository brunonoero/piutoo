using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

/// <summary>
/// Materializza array JSON deduplicati usando una sostituzione atomica.
///
/// <para><b>Perche' esiste un journal.</b> La riscrittura completa dell'array e' corretta ma costa
/// quanto TUTTO cio' che si e' accumulato dall'inizio del run, non quanto e' cambiato dall'ultimo
/// checkpoint. Su un backtest lungo un anno <c>signals.json</c> arriva a 40-80 MB e circa 24.000
/// record: riscriverlo ogni due secondi significa riproiettare, validare, deduplicare e
/// serializzare 24.000 oggetti per salvarne due nuovi. E' la causa del rallentamento progressivo
/// che si vedeva a occhio verso la fine dei run lunghi — il costo per barra cresce linearmente con
/// le barre gia' fatte, quindi il run e' quadratico.</para>
///
/// <para>I checkpoint intermedi usano quindi <c>Append*</c>: una riga JSON per record (non
/// indentata, altrimenti non sarebbe una riga) in coda a un file <c>.jsonl</c> affiancato
/// all'array. Il costo e' proporzionale al delta e resta costante per tutto il run.</para>
///
/// <para>L'array vero viene <b>materializzato</b> (<see cref="CompactAll"/>, o implicitamente al
/// primo <c>Read*</c>) fondendo il journal: per ogni record, l'ultima versione vince e l'ordine
/// originale e' preservato — un record gia' presente viene sostituito sul posto, uno nuovo
/// accodato. Il risultato e' byte per byte quello che avrebbe prodotto la riscrittura completa.</para>
///
/// <para><b>Invariante.</b> Il journal e' uno stato transitorio del run: ogni scrittura completa
/// (<c>Write*</c>) e ogni materializzazione lo cancellano. Un run che termina normalmente chiude
/// sempre con una scrittura completa e durabile, quindi in una cartella di backtest chiusa il
/// <c>.jsonl</c> non esiste. Chi legge l'array senza passare da qui — <c>TitanoRotationService</c>,
/// che gli fa l'hash — deve chiamare prima <see cref="CompactAll"/>: e' un no-op quando non c'e'
/// journal.</para>
/// </summary>
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

    /// <summary>
    /// Opzioni del journal. Identiche a <see cref="JsonOptions"/> tranne l'indentazione, che qui
    /// non e' una scelta estetica: un record per riga e' cio' che rende il file appendibile e
    /// rileggibile riga per riga.
    /// </summary>
    private static readonly JsonSerializerOptions JournalOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly UTF8Encoding Utf8NoBom = new(false);

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

    /// <summary>
    /// Fonde nei rispettivi array tutti i journal ancora aperti e li cancella. No-op quando non
    /// ce ne sono, cioe' nel caso normale di una cartella di run gia' chiusa.
    ///
    /// <para>Serve a chi legge i file senza passare dai metodi <c>Read*</c> di questo store.</para>
    /// </summary>
    public void CompactAll()
    {
        Compact<PersistedSignal>(SignalsPath, value => value.SignalId);
        Compact<PersistedTrade>(TradesPath, value => value.TradeId);
        Compact<RotationLogEntry>(RotationLogPath, value => value.EntryId);
    }

    public IReadOnlyList<PersistedSignal> ReadSignals() => Read<PersistedSignal>(SignalsPath, value => value.SignalId);
    public IReadOnlyList<PersistedTrade> ReadTrades() => Read<PersistedTrade>(TradesPath, value => value.TradeId);
    public IReadOnlyList<RotationLogEntry> ReadRotationLog() => Read<RotationLogEntry>(RotationLogPath, value => value.EntryId);

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

    /// <summary>
    /// Accoda al journal dei signal i soli record nuovi o modificati dall'ultimo checkpoint.
    /// Non tocca <c>signals.json</c>: la fusione avviene alla prima lettura o scrittura completa.
    /// </summary>
    public void AppendSignals(IEnumerable<PersistedSignal> values)
    {
        var materialized = values.ToArray();
        if (materialized.Length == 0) return;
        ValidateSignals(materialized);
        Append(SignalsPath, materialized);
    }

    /// <inheritdoc cref="AppendSignals"/>
    public void AppendTrades(IEnumerable<PersistedTrade> values)
    {
        var materialized = values.ToArray();
        if (materialized.Length == 0) return;
        ValidateTrades(materialized);
        Append(TradesPath, materialized);
    }

    /// <inheritdoc cref="AppendSignals"/>
    public void AppendRotationLog(IEnumerable<RotationLogEntry> values)
    {
        var materialized = values.ToArray();
        if (materialized.Length == 0) return;
        Append(RotationLogPath, materialized);
    }

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

    /// <summary>Il journal che affianca un array: <c>signals.json</c> -> <c>signals.jsonl</c>.</summary>
    private static string JournalPath(string path) => path + "l";

    private static IReadOnlyList<T> Read<T>(string path, Func<T, string> key)
    {
        lock (Gates.GetOrAdd(path, _ => new object()))
        {
            Compact(path, key);
            return ReadUnsafe<T>(path);
        }
    }

    private static List<T> ReadUnsafe<T>(string path)
    {
        if (!File.Exists(path)) return [];
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), JsonOptions) ?? [];
    }

    private static void Append<T>(string path, IReadOnlyList<T> values)
    {
        var journal = JournalPath(path);
        lock (Gates.GetOrAdd(path, _ => new object()))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(journal)!);

            // Append semplice, senza fsync e senza temporaneo: il journal non e' un artefatto che
            // qualcuno legge a meta' run, e' il quaderno da cui si ricostruisce l'artefatto.
            using var stream = new FileStream(
                journal, FileMode.Append, FileAccess.Write, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
            using var writer = new StreamWriter(stream, Utf8NoBom);
            foreach (var value in values)
                writer.WriteLine(JsonSerializer.Serialize(value, JournalOptions));
        }
    }

    /// <summary>
    /// Fonde il journal nell'array e lo cancella. L'ultima versione di un record vince, l'ordine
    /// dell'array e' preservato: un id gia' presente viene sostituito sul posto, uno nuovo accodato.
    /// </summary>
    private static void Compact<T>(string path, Func<T, string> key)
    {
        var journal = JournalPath(path);
        lock (Gates.GetOrAdd(path, _ => new object()))
        {
            if (!File.Exists(journal)) return;

            var lines = File.ReadAllLines(journal);
            var merged = ReadUnsafe<T>(path);
            var index = new Dictionary<string, int>(merged.Count, StringComparer.Ordinal);
            for (var i = 0; i < merged.Count; i++)
                index[key(merged[i])] = i;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                T? value = default;
                try
                {
                    value = JsonSerializer.Deserialize<T>(line, JournalOptions);
                }
                catch (JsonException) when (i == lines.Length - 1)
                {
                    // Ultima riga tronca: il processo e' morto a meta' append. E' l'unico punto in
                    // cui una riga incompleta e' spiegabile, e scartarla perde al piu' l'ultimo
                    // record del checkpoint interrotto. Una riga rotta in mezzo al file sarebbe
                    // invece corruzione vera, e deve esplodere.
                    break;
                }

                if (value is null) continue;
                var id = key(value);
                if (index.TryGetValue(id, out var at)) merged[at] = value;
                else
                {
                    index[id] = merged.Count;
                    merged.Add(value);
                }
            }

            // Durabile: la materializzazione avviene fuori dal loop caldo (prima lettura, o
            // scrittura autorevole di fine run), quindi qui l'fsync e' quello giusto.
            WriteAtomic(path, merged, durable: true);
            DeleteJournal(journal);
        }
    }

    private static void DeleteJournal(string journal)
    {
        try
        {
            if (File.Exists(journal)) File.Delete(journal);
        }
        catch (IOException)
        {
            // Un journal che resta su disco viene rifuso al giro dopo: e' idempotente, perche' i
            // suoi record sono gia' nell'array e la fusione tiene l'ultima versione di ognuno.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void Upsert<T>(string path, IEnumerable<T> values, Func<T, string> key)
    {
        lock (Gates.GetOrAdd(path, _ => new object()))
        {
            Compact(path, key);
            var byId = ReadUnsafe<T>(path).ToDictionary(key, StringComparer.Ordinal);
            foreach (var value in values) byId[key(value)] = value;
            WriteAtomic(path, byId.Values, durable: true);
        }
    }

    private static void WriteDistinct<T>(string path, IEnumerable<T> values, Func<T, string> key, bool durable)
    {
        lock (Gates.GetOrAdd(path, _ => new object()))
        {
            WriteAtomic(path, values.DistinctBy(key), durable);

            // L'array appena scritto e' autorevole: il journal accumulato fino a qui e' gia' dentro
            // (chi riscrive tutto parte dallo stato completo in memoria) e tenerlo lo farebbe
            // rifondere, resuscitando versioni vecchie di record nel frattempo cambiati.
            DeleteJournal(JournalPath(path));
        }
    }

    private static void WriteAtomic<T>(string path, IEnumerable<T> values, bool durable = true)
    {
        AtomicFileWriter.Write(path, stream => JsonSerializer.Serialize(stream, values, JsonOptions), durable);
    }
}
