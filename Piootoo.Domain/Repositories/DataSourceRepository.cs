using System.Text.Json;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Utilities;

namespace Piootoo.Domain.Repositories;

/// <summary>
/// Repository per accedere ai datasource OHLCV dal repository locale.
///
/// <para><b>Struttura primaria: un file per coppia (simbolo, timeframe)</b>, tutti nella radice
/// del datafeed, col timeframe espresso in minuti:</para>
///
/// <code>
///     datafeed/@NQ_15.json
///     datafeed/@GC_240.json
///     datafeed/@ES_1440.json
/// </code>
///
/// <para>La produce <c>piootoo-repository/datafeed-future/aggregate_flat_feed.py</c> a partire dai
/// CSV minute del vendor. Il contenuto e' gia' in UTC vero — lo script risolve l'ora legale
/// europea del sorgente una volta sola, in conversione — e <c>feed-clocks.json</c> lo dichiara
/// come <c>UTC</c>.</para>
///
/// <para><b>Fallback: la vecchia gerarchia</b> <c>datafeed/{timeframe}/{symbol}/{symbol}-{yyyyMMdd}.json</c>
/// prodotta da Piootoo.FeedWorker. Resta leggibile per i simboli non ancora convertiti, ma il
/// file piatto vince sempre quando esiste: e' l'unico dei due generato con il bucket giusto (il
/// timestamp del CSV sorgente e' la <i>fine</i> del minuto, vedi lo script).</para>
/// </summary>
public class DataSourceRepository
{
    private readonly string _repositoryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    // Orologio dichiarato di ogni feed, letto una volta sola. Serve a convertire i timestamp in
    // UTC vero al caricamento: senza, ogni conversione a valle parte da un istante che mente.
    private FeedClockRegistry? _feedClocks;

    private FeedClockRegistry FeedClocks => _feedClocks ??= FeedClockRegistry.Load(_repositoryPath);

    /// <summary>Conversione barType (usato dalle API) -> timeframe in minuti.</summary>
    private static readonly Dictionary<string, int> BarTypeToTimeframeMinutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OneMinute"] = 1,
        ["FiveMinute"] = 5,
        ["FifteenMinute"] = 15,
        ["ThirtyMinute"] = 30,
        ["OneHour"] = 60,
        ["FourHour"] = 240,
        ["FourHours"] = 240,
        ["Daily"] = 1440,
        ["Weekly"] = 10080,
    };

    private static readonly Dictionary<int, string> TimeframeFolders = new()
    {
        [1] = "1m",
        [5] = "5m",
        [15] = "15m",
        [30] = "30m",
        [60] = "1h",
        [240] = "4h",
        [1440] = "D",
        [10080] = "W"
    };

    private static readonly Dictionary<int, string> CanonicalBarTypes = new()
    {
        [1] = "OneMinute",
        [5] = "FiveMinute",
        [15] = "FifteenMinute",
        [30] = "ThirtyMinute",
        [60] = "OneHour",
        [240] = "FourHour",
        [1440] = "Daily",
        [10080] = "Weekly"
    };

    public DataSourceRepository(string repositoryPath)
    {
        _repositoryPath = repositoryPath;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>Converte un barType (es. "OneHour") nel timeframe in minuti (es. 60).</summary>
    private static int ConvertBarTypeToMinutes(string barType)
    {
        if (BarTypeToTimeframeMinutes.TryGetValue(barType, out var minutes))
            return minutes;

        // Fallback: il chiamante potrebbe già passare un numero di minuti come stringa.
        return int.TryParse(barType, out var parsed) ? parsed : 60;
    }

    /// <summary>Rimuove il prefisso "@" e normalizza il simbolo strategia (es. "@GC" -> "GC").</summary>
    private static string NormalizeRootSymbol(string symbol)
        => symbol.Trim().TrimStart('@').ToUpperInvariant();

    /// <summary>
    /// Nome del file piatto per una coppia (simbolo, timeframe): <c>"@GC" + 240 -> "@GC_240.json"</c>.
    /// Il timeframe e' in minuti, la stessa unita' con cui il resto del repository lo tratta: le
    /// sigle <c>4h</c>/<c>D</c> restano confinate ai nomi di cartella della vecchia gerarchia.
    /// </summary>
    private static string BuildFlatFileName(string symbol, int timeframeMinutes)
        => $"@{NormalizeRootSymbol(symbol)}_{timeframeMinutes}.json";

    private string GetFlatFilePath(string symbol, int timeframeMinutes)
        => Path.Combine(_repositoryPath, BuildFlatFileName(symbol, timeframeMinutes));

    /// <summary>
    /// Scompone il nome di un file piatto (senza estensione) in simbolo e minuti. Restituisce
    /// null se il nome non segue la convenzione, cosi' un file estraneo lasciato nella cartella
    /// viene ignorato invece di comparire come simbolo inesistente.
    /// </summary>
    private static (string Symbol, int TimeframeMinutes)? ParseFlatFileName(string fileNameWithoutExtension)
    {
        if (!fileNameWithoutExtension.StartsWith('@'))
            return null;

        var lastUnderscore = fileNameWithoutExtension.LastIndexOf('_');
        if (lastUnderscore <= 1 || lastUnderscore == fileNameWithoutExtension.Length - 1)
            return null;

        return int.TryParse(fileNameWithoutExtension[(lastUnderscore + 1)..], out var minutes)
            ? (fileNameWithoutExtension[..lastUnderscore], minutes)
            : null;
    }

    private async Task<List<OhlcvData>> ReadFlatFeedAsync(string symbol, int timeframeMinutes)
    {
        var path = GetFlatFilePath(symbol, timeframeMinutes);
        if (!File.Exists(path))
            return new List<OhlcvData>();

        // Risolto prima di leggere: un feed di fuso non dichiarato deve fermare la lettura
        // subito, non dopo aver deserializzato mezzo storico.
        var feedClock = FeedClocks.For(symbol);

        await using var stream = File.OpenRead(path);
        var feed = await JsonSerializer.DeserializeAsync<WorkerFeedFileDto>(stream, _jsonOptions);
        if (feed?.Candles == null || feed.Candles.Count == 0)
            return new List<OhlcvData>();

        var candles = new List<OhlcvData>(feed.Candles.Count);
        foreach (var candle in feed.Candles)
            candles.Add(ConvertWorkerCandle(candle, feedClock));

        return candles;
    }

    /// <summary>
    /// Trasforma il timestamp scritto nel file — che e' un orario di parete nell'orologio
    /// dichiarato dal feed — nell'istante UTC che gli corrisponde davvero.
    ///
    /// <para>Prima qui c'era <c>TradingDateTime.ToFeedUtc</c>, che <b>ri-etichetta</b> il
    /// <c>Kind</c> senza spostare nulla: e' il punto in cui l'etichetta <c>Z</c> falsa del feed
    /// veniva presa per buona. Vedi <see cref="FeedClockRegistry"/> per la misura che lo
    /// dimostra.</para>
    /// </summary>
    private static DateTime ToTrueUtc(DateTime feedWallClock, SessionClock feedClock) =>
        feedClock.ToUtc(DateTime.SpecifyKind(feedWallClock, DateTimeKind.Unspecified));

    private string? GetHierarchicalSymbolDirectory(
        string symbol,
        int timeframeMinutes,
        bool preferCalculated)
    {
        if (!TimeframeFolders.TryGetValue(timeframeMinutes, out var timeframeFolder))
            return null;

        var candidates = preferCalculated
            ? new[] { timeframeFolder + "-calculate", timeframeFolder }
            : new[] { timeframeFolder };
        var normalizedSymbol = "@" + NormalizeRootSymbol(symbol);

        foreach (var candidate in candidates)
        {
            var timeframePath = Path.Combine(_repositoryPath, candidate);
            if (!Directory.Exists(timeframePath))
                continue;

            var symbolPath = Directory.EnumerateDirectories(timeframePath)
                .FirstOrDefault(path =>
                    Path.GetFileName(path).Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase));
            if (symbolPath != null)
                return symbolPath;
        }

        return null;
    }

    private async Task<List<OhlcvData>> ReadHierarchicalFeedAsync(
        string symbol,
        int timeframeMinutes,
        bool preferCalculated)
    {
        var symbolDirectory = GetHierarchicalSymbolDirectory(symbol, timeframeMinutes, preferCalculated);
        if (symbolDirectory == null)
            return new List<OhlcvData>();

        // Risolto prima del ciclo: un feed di fuso non dichiarato deve fermare la lettura subito,
        // non dopo aver caricato mezzo storico.
        var feedClock = FeedClocks.For(symbol);
        var candles = new List<OhlcvData>();
        foreach (var path in Directory.EnumerateFiles(symbolDirectory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var json = await File.ReadAllTextAsync(path);
            var feed = JsonSerializer.Deserialize<WorkerFeedFileDto>(json, _jsonOptions);
            if (feed?.Candles == null)
                continue;

            candles.AddRange(feed.Candles.Select(candle => ConvertWorkerCandle(candle, feedClock)));
        }

        return candles
            .GroupBy(candle => candle.Timestamp)
            .Select(group => group.Last())
            .OrderBy(candle => candle.DateTime)
            .ToList();
    }

    private static OhlcvData ConvertWorkerCandle(WorkerCandleDto candle, SessionClock feedClock)
    {
        var utcDateTime = ToTrueUtc(candle.DateTime, feedClock);

        // Il campo `timestamp` del file e' derivato dall'etichetta letta come UTC, quindi porta la
        // stessa bugia: va ricalcolato dall'istante vero, altrimenti Timestamp e DateTime
        // descrivono due momenti diversi e la deduplica raggruppa sulla chiave sbagliata.
        var timestamp = new DateTimeOffset(utcDateTime).ToUnixTimeSeconds();

        return new OhlcvData
        {
            Timestamp = timestamp,
            DateTime = utcDateTime,
            // Come sopra: la stringa del file e' nell'orologio del feed, non in UTC.
            DateTimeFormatted = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Open = (decimal)candle.Open,
            High = (decimal)candle.High,
            Low = (decimal)candle.Low,
            Close = (decimal)candle.Close,
            Volume = (decimal)candle.Volume
        };
    }

    /// <summary>
    /// Il file piatto vince sulla vecchia gerarchia quando esiste, e non e' una preferenza
    /// arbitraria: e' l'unico dei due generato col bucket giusto. Il timestamp del CSV sorgente e'
    /// la <i>fine</i> del minuto, mentre <c>aggregate_nq_ascii.py</c> lo trattava come inizio, per
    /// cui la gerarchia e' sfasata di un minuto per barra. Il fallback resta solo per i simboli
    /// non ancora convertiti.
    /// </summary>
    private async Task<List<OhlcvData>> ReadCandlesAsync(
        string symbol,
        int timeframeMinutes,
        bool preferCalculated)
    {
        var flat = await ReadFlatFeedAsync(symbol, timeframeMinutes);
        if (flat.Count != 0)
            return flat.OrderBy(candle => candle.DateTime).ToList();

        return await ReadHierarchicalFeedAsync(symbol, timeframeMinutes, preferCalculated);
    }

    /// <summary>Simboli per cui esiste almeno un file piatto nella radice del datafeed.</summary>
    private IEnumerable<(string Symbol, int TimeframeMinutes)> EnumerateFlatFeeds()
    {
        if (!Directory.Exists(_repositoryPath))
            yield break;

        foreach (var file in Directory.EnumerateFiles(_repositoryPath, "@*_*.json"))
        {
            var parsed = ParseFlatFileName(Path.GetFileNameWithoutExtension(file));
            if (parsed != null)
                yield return parsed.Value;
        }
    }

    /// <summary>
    /// Ottiene la lista dei simboli per cui esiste almeno un feed nel repository, unendo le due
    /// strutture. L'unione, e non "la prima che risponde": durante la conversione le due
    /// convivono, e restituire solo i simboli piatti nasconderebbe dalla console tutto cio' che
    /// non e' ancora stato convertito.
    /// </summary>
    public IEnumerable<string> GetAvailableSymbols()
    {
        if (!Directory.Exists(_repositoryPath))
            return Enumerable.Empty<string>();

        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var feed in EnumerateFlatFeeds())
            symbols.Add(feed.Symbol);

        foreach (var directory in TimeframeFolders.Values
                     .SelectMany(folder => new[] { folder, folder + "-calculate" })
                     .Select(folder => Path.Combine(_repositoryPath, folder))
                     .Where(Directory.Exists)
                     .SelectMany(Directory.EnumerateDirectories))
        {
            var name = Path.GetFileName(directory);
            if (!string.IsNullOrWhiteSpace(name))
                symbols.Add(name.StartsWith('@') ? name : "@" + name);
        }

        return symbols.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Ottiene i barType disponibili per un simbolo, dalle due strutture.</summary>
    public IEnumerable<string> GetAvailableBarTypes(string symbol)
    {
        if (!Directory.Exists(_repositoryPath))
            return Enumerable.Empty<string>();

        var normalized = "@" + NormalizeRootSymbol(symbol);
        var minutes = new HashSet<int>();

        foreach (var feed in EnumerateFlatFeeds())
        {
            if (feed.Symbol.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                minutes.Add(feed.TimeframeMinutes);
        }

        foreach (var pair in TimeframeFolders)
        {
            if (GetHierarchicalSymbolDirectory(symbol, pair.Key, preferCalculated: true) != null)
                minutes.Add(pair.Key);
        }

        return minutes
            .OrderBy(value => value)
            .Select(value => CanonicalBarTypes.TryGetValue(value, out var barType)
                ? barType
                : value.ToString())
            .ToList();
    }

    /// <summary>
    /// Ottiene le date (giorno) disponibili per un simbolo e tipo di bar, ricavandole
    /// dalle candele contenute nel file feed corrispondente.
    /// </summary>
    /// <param name="preferCalculated">Mantenuto per compatibilità con i chiamanti esistenti;
    /// non più applicabile con la struttura "flat" attuale (nessuna cartella -calculate).</param>
    public IEnumerable<DateTime> GetAvailableDates(string symbol, string barType = "OneMinute", bool preferCalculated = true)
    {
        var timeframeMinutes = ConvertBarTypeToMinutes(barType);
        var candles = ReadCandlesAsync(symbol, timeframeMinutes, preferCalculated)
            .GetAwaiter().GetResult();
        return candles.Select(c => c.DateTime.Date).Distinct().OrderBy(d => d);
    }

    /// <summary>
    /// Carica i dati per una specifica data dal file feed corrispondente a symbol+barType.
    /// </summary>
    public async Task<DataSource?> LoadDataAsync(string symbol, DateTime date, string barType = "OneMinute", bool preferCalculated = true)
    {
        var timeframeMinutes = ConvertBarTypeToMinutes(barType);
        var targetDate = TradingDateTime.ToFeedUtc(date).Date;
        var candles = (await ReadCandlesAsync(symbol, timeframeMinutes, preferCalculated))
            .Where(c => c.DateTime.Date == targetDate)
            .ToList();
        if (candles.Count == 0)
            return null;

        return new DataSource
        {
            Symbol = "@" + NormalizeRootSymbol(symbol),
            BarType = barType,
            LastUpdate = candles.Max(candle => candle.DateTime),
            CandleCount = candles.Count,
            Candles = candles
        };
    }

    /// <summary>
    /// Carica i dati per un range di date dal file feed corrispondente a symbol+barType.
    /// </summary>
    /// <param name="preferCalculated">Mantenuto per compatibilità con i chiamanti esistenti;
    /// non più applicabile con la struttura "flat" attuale (nessuna cartella -calculate).</param>
    public async Task<List<OhlcvData>> LoadDataRangeAsync(string symbol, DateTime startDate, DateTime endDate, string barType = "OneMinute", bool preferCalculated = true)
    {
        startDate = TradingDateTime.ToFeedUtc(startDate);
        endDate = TradingDateTime.ToFeedUtc(endDate);

        var timeframeMinutes = ConvertBarTypeToMinutes(barType);
        var candles = await ReadCandlesAsync(symbol, timeframeMinutes, preferCalculated);

        return candles
            .Where(c => c.DateTime >= startDate && c.DateTime <= endDate)
            .OrderBy(c => c.DateTime)
            .ToList();
    }

    /// <summary>
    /// Carica tutti i dati disponibili per un simbolo dal file feed corrispondente a symbol+barType.
    /// </summary>
    public async Task<List<OhlcvData>> LoadAllDataAsync(string symbol, string barType = "OneMinute")
    {
        var timeframeMinutes = ConvertBarTypeToMinutes(barType);
        return (await ReadCandlesAsync(symbol, timeframeMinutes, preferCalculated: true))
            .OrderBy(c => c.DateTime)
            .ToList();
    }

    /// <summary>
    /// Carica i dati per le ultime N sessioni (giorni) disponibili nel file feed.
    /// </summary>
    public async Task<List<OhlcvData>> LoadLastSessionsAsync(string symbol, int sessions, string barType = "OneMinute")
    {
        var allCandles = await LoadAllDataAsync(symbol, barType);
        var lastDates = allCandles
            .Select(c => c.DateTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(sessions)
            .ToHashSet();

        return allCandles
            .Where(c => lastDates.Contains(c.DateTime.Date))
            .OrderBy(c => c.DateTime)
            .ToList();
    }

    /// <summary>
    /// Ottiene informazioni sul repository
    /// </summary>
    public RepositoryInfo GetRepositoryInfo()
    {
        var symbols = GetAvailableSymbols().ToList();
        var info = new RepositoryInfo
        {
            RepositoryPath = _repositoryPath,
            TotalSymbols = symbols.Count,
            Symbols = symbols
        };

        foreach (var symbol in symbols)
        {
            var barTypes = GetAvailableBarTypes(symbol).ToList();
            foreach (var barType in barTypes)
            {
                var dates = GetAvailableDates(symbol, barType).ToList();
                info.SymbolDetails.Add(new SymbolInfo
                {
                    Symbol = symbol,
                    BarType = barType,
                    FirstDate = dates.FirstOrDefault(),
                    LastDate = dates.LastOrDefault(),
                    TotalDays = dates.Count
                });
            }
        }

        return info;
    }
}

/// <summary>DTO del file giornaliero prodotto da Piootoo.FeedWorker.</summary>
internal sealed class WorkerFeedFileDto
{
    public string Symbol { get; set; } = string.Empty;
    public string BarType { get; set; } = string.Empty;
    public DateTime? LastUpdate { get; set; }
    public List<WorkerCandleDto> Candles { get; set; } = new();
}

internal sealed class WorkerCandleDto
{
    public double Timestamp { get; set; }
    public DateTime DateTime { get; set; }
    public string DateTimeFormatted { get; set; } = string.Empty;
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public double Volume { get; set; }
}

/// <summary>
/// Informazioni sul repository
/// </summary>
public class RepositoryInfo
{
    public string RepositoryPath { get; set; } = string.Empty;
    public int TotalSymbols { get; set; }
    public List<string> Symbols { get; set; } = new();
    public List<SymbolInfo> SymbolDetails { get; set; } = new();
}

/// <summary>
/// Informazioni su un simbolo
/// </summary>
public class SymbolInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string BarType { get; set; } = string.Empty;
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
    public int TotalDays { get; set; }
}
