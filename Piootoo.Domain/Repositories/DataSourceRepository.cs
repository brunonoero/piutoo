using System.Text.Json;
using Piootoo.Shared.Models;
using Piootoo.Shared.Utilities;

namespace Piootoo.Domain.Repositories;

/// <summary>
/// Repository per accedere ai datasource OHLCV dal repository locale.
///
/// Struttura primaria prodotta da Piootoo.FeedWorker:
///
///     datafeed/{timeframe}/{symbol}/{symbol}-{yyyyMMdd}.json
///
/// Esempio: datafeed/1h/@GC/@GC-20260724.json. I vecchi file flat Yahoo
/// restano supportati come fallback durante la migrazione.
/// </summary>
public class DataSourceRepository
{
    private readonly string _repositoryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>Mappatura indicativa root future strategia -> ticker Yahoo Finance.</summary>
    private static readonly Dictionary<string, string> RootSymbolToTicker = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GC"] = "GC=F",       // Gold
        ["CL"] = "CL=F",       // Crude Oil
        ["NQ"] = "NQ=F",       // Nasdaq
        ["ES"] = "ES=F",       // S&P 500
        ["HG"] = "HG=F",       // Copper
        ["PL"] = "PL=F",       // Platinum
        ["NG"] = "NG=F",       // Natural Gas
        ["RB"] = "RB=F",       // RBOB Gasoline
        ["HO"] = "HO=F",       // Heating Oil
        ["S"] = "ZS=F",        // Soybeans
        ["US"] = "ZB=F",       // 30y T-Bond
        ["EC"] = "6E=F",       // Euro FX
        ["BP"] = "6B=F",       // British Pound
        ["JY"] = "6J=F",       // Japanese Yen
        ["LC"] = "LE=F",       // Live Cattle
        ["FC"] = "GF=F",       // Feeder Cattle
        ["FDAX"] = "^GDAXI",   // DAX (indice, non il future)
        ["BTCUSDT"] = "BTC-USD",
        ["ETHUSDT"] = "ETH-USD",
        ["TSLA"] = "TSLA",
    };

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

    /// <summary>Risolve il ticker feed (Yahoo Finance) a partire dal simbolo root della strategia.</summary>
    private static string ResolveTicker(string symbol)
    {
        var root = NormalizeRootSymbol(symbol);
        return RootSymbolToTicker.TryGetValue(root, out var ticker) ? ticker : root;
    }

    /// <summary>Stessa normalizzazione di datafeed-downloader/core.py:safe_filename.</summary>
    private static string BuildSafeFileSymbol(string ticker)
        => ticker.Replace("=", string.Empty).Replace("^", string.Empty).Replace("/", "-");

    /// <summary>Nome file feed per symbol+timeframe, es. "@GC" + 15 -> "GCF_15.json".</summary>
    private static string BuildFeedFileName(string symbol, int timeframeMinutes)
        => $"{BuildSafeFileSymbol(ResolveTicker(symbol))}_{timeframeMinutes}.json";

    private string GetFeedFilePath(string symbol, int timeframeMinutes)
        => Path.Combine(_repositoryPath, BuildFeedFileName(symbol, timeframeMinutes));

    /// <summary>Estrae il "safe symbol" (es. "GCF") dal nome file "GCF_15" (senza estensione).</summary>
    private static string? ExtractSafeSymbolFromFileName(string fileNameWithoutExtension)
    {
        var lastUnderscore = fileNameWithoutExtension.LastIndexOf('_');
        if (lastUnderscore <= 0 || lastUnderscore == fileNameWithoutExtension.Length - 1)
            return null;

        var minutesPart = fileNameWithoutExtension[(lastUnderscore + 1)..];
        return int.TryParse(minutesPart, out _) ? fileNameWithoutExtension[..lastUnderscore] : null;
    }

    private FeedFileDto? ReadFeedFile(string symbol, int timeframeMinutes)
    {
        var path = GetFeedFilePath(symbol, timeframeMinutes);
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<FeedFileDto>(json, _jsonOptions);
    }

    private async Task<FeedFileDto?> ReadFeedFileAsync(string symbol, int timeframeMinutes)
    {
        var path = GetFeedFilePath(symbol, timeframeMinutes);
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<FeedFileDto>(json, _jsonOptions);
    }

    private static List<OhlcvData> ConvertToOhlcv(FeedFileDto? feed)
    {
        if (feed?.Bars == null || feed.Bars.Count == 0)
            return new List<OhlcvData>();

        var candles = feed.Bars.Select(bar =>
        {
            var utcDateTime = DateTime.SpecifyKind(bar.DateTime, DateTimeKind.Utc);
            return new OhlcvData
            {
                Timestamp = new DateTimeOffset(utcDateTime).ToUnixTimeSeconds(),
                DateTime = utcDateTime,
                DateTimeFormatted = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Open = (decimal)bar.Open,
                High = (decimal)bar.High,
                Low = (decimal)bar.Low,
                Close = (decimal)bar.Close,
                Volume = (decimal)bar.Volume
            };
        }).ToList();

        TradingDateTime.NormalizeCandlesToUtc(candles);
        return candles;
    }

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

        var candles = new List<OhlcvData>();
        foreach (var path in Directory.EnumerateFiles(symbolDirectory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var json = await File.ReadAllTextAsync(path);
            var feed = JsonSerializer.Deserialize<WorkerFeedFileDto>(json, _jsonOptions);
            if (feed?.Candles == null)
                continue;

            candles.AddRange(feed.Candles.Select(ConvertWorkerCandle));
        }

        TradingDateTime.NormalizeCandlesToUtc(candles);
        return candles
            .GroupBy(candle => candle.Timestamp)
            .Select(group => group.Last())
            .OrderBy(candle => candle.DateTime)
            .ToList();
    }

    private static OhlcvData ConvertWorkerCandle(WorkerCandleDto candle)
    {
        var utcDateTime = candle.DateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(candle.DateTime, DateTimeKind.Utc)
            : candle.DateTime.ToUniversalTime();
        var timestamp = candle.Timestamp != 0
            ? Convert.ToInt64(candle.Timestamp)
            : new DateTimeOffset(utcDateTime).ToUnixTimeSeconds();

        return new OhlcvData
        {
            Timestamp = timestamp,
            DateTime = utcDateTime,
            DateTimeFormatted = string.IsNullOrWhiteSpace(candle.DateTimeFormatted)
                ? utcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
                : candle.DateTimeFormatted,
            Open = (decimal)candle.Open,
            High = (decimal)candle.High,
            Low = (decimal)candle.Low,
            Close = (decimal)candle.Close,
            Volume = (decimal)candle.Volume
        };
    }

    private async Task<List<OhlcvData>> ReadCandlesAsync(
        string symbol,
        int timeframeMinutes,
        bool preferCalculated)
    {
        var hierarchical = await ReadHierarchicalFeedAsync(symbol, timeframeMinutes, preferCalculated);
        if (hierarchical.Count != 0)
            return hierarchical;

        return ConvertToOhlcv(await ReadFeedFileAsync(symbol, timeframeMinutes))
            .OrderBy(candle => candle.DateTime)
            .ToList();
    }

    /// <summary>
    /// Ottiene la lista dei simboli (root strategia, quando mappabili) per cui esiste
    /// almeno un file feed nel repository.
    /// </summary>
    public IEnumerable<string> GetAvailableSymbols()
    {
        if (!Directory.Exists(_repositoryPath))
            return Enumerable.Empty<string>();

        var hierarchicalSymbols = TimeframeFolders.Values
            .SelectMany(folder => new[] { folder, folder + "-calculate" })
            .Select(folder => Path.Combine(_repositoryPath, folder))
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateDirectories(path))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.StartsWith('@') ? name : "@" + name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (hierarchicalSymbols.Count != 0)
            return hierarchicalSymbols;

        var tickerToRoot = RootSymbolToTicker
            .GroupBy(pair => BuildSafeFileSymbol(pair.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(_repositoryPath, "*_*.json"))
        {
            var safeSymbol = ExtractSafeSymbolFromFileName(Path.GetFileNameWithoutExtension(file));
            if (safeSymbol == null)
                continue;

            var root = tickerToRoot.TryGetValue(safeSymbol, out var mappedRoot) ? mappedRoot : safeSymbol;
            symbols.Add("@" + root.TrimStart('@'));
        }

        return symbols;
    }

    /// <summary>Ottiene i timeframe (in minuti, come stringa) disponibili per un simbolo.</summary>
    public IEnumerable<string> GetAvailableBarTypes(string symbol)
    {
        if (!Directory.Exists(_repositoryPath))
            return Enumerable.Empty<string>();

        var hierarchical = TimeframeFolders
            .Where(pair =>
                GetHierarchicalSymbolDirectory(symbol, pair.Key, preferCalculated: true) != null)
            .Select(pair => CanonicalBarTypes[pair.Key])
            .ToList();
        if (hierarchical.Count != 0)
            return hierarchical;

        var safeSymbol = BuildSafeFileSymbol(ResolveTicker(symbol));
        var prefix = safeSymbol + "_";

        return Directory.GetFiles(_repositoryPath, $"{safeSymbol}_*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(name => name[prefix.Length..])
            .Where(minutes => int.TryParse(minutes, out _))
            .Select(minutes => CanonicalBarTypes.TryGetValue(int.Parse(minutes), out var barType)
                ? barType
                : minutes)
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
/// DTO per il file feed "flat" prodotto da datafeed-downloader/core.py.
/// </summary>
internal class FeedFileDto
{
    public string Symbol { get; set; } = string.Empty;
    public int TimeframeMinutes { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public DateTime RequestedStartUtc { get; set; }
    public DateTime EffectiveStartUtc { get; set; }
    public string? Note { get; set; }
    public List<FeedBarDto> Bars { get; set; } = new();
}

/// <summary>DTO per una singola barra OHLCV del file feed (schema OhlcvDto).</summary>
internal class FeedBarDto
{
    public DateTime DateTime { get; set; }
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
