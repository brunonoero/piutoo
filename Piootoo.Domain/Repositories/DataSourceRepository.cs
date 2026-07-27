using System.Text.Json;
using Piootoo.Shared.Models;
using Piootoo.Shared.Utilities;

namespace Piootoo.Domain.Repositories;

/// <summary>
/// Repository per accedere ai datasource OHLCV dal repository locale.
///
/// Struttura attuale (allineata a piootoo-repository/datafeed-downloader/core.py):
/// un file "flat" per ogni combinazione symbol+timeframe, direttamente dentro
/// RepositoryPath (es. "datafeed"), con nome
///
///     {tickerSenzaCaratteriSpeciali}_{timeframeMinutes}.json   (es. "GCF_15.json")
///
/// e schema:
///
///     {
///       "symbol": "GC=F",
///       "timeframeMinutes": 15,
///       "source": "yahoo-finance",
///       "generatedAtUtc": "...",
///       "requestedStartUtc": "...",
///       "effectiveStartUtc": "...",
///       "note": "..." | null,
///       "bars": [ { "dateTime", "open", "high", "low", "close", "volume" }, ... ]
///     }
///
/// Le strategie (Piootoo.Strategies) usano un simbolo "root future" con prefisso
/// "@" (es. "@GC"), che non coincide col ticker Yahoo Finance usato per il nome
/// file (es. "GC=F" -> "GCF"): la mappatura è in <see cref="RootSymbolToTicker"/>
/// (vedi anche piootoo-repository/datafeed-downloader/README.md).
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

    /// <summary>
    /// Ottiene la lista dei simboli (root strategia, quando mappabili) per cui esiste
    /// almeno un file feed nel repository.
    /// </summary>
    public IEnumerable<string> GetAvailableSymbols()
    {
        if (!Directory.Exists(_repositoryPath))
            return Enumerable.Empty<string>();

        var tickerToRoot = RootSymbolToTicker
            .GroupBy(pair => BuildSafeFileSymbol(pair.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(_repositoryPath, "*_*.json"))
        {
            var safeSymbol = ExtractSafeSymbolFromFileName(Path.GetFileNameWithoutExtension(file));
            if (safeSymbol == null)
                continue;

            symbols.Add(tickerToRoot.TryGetValue(safeSymbol, out var root) ? root : safeSymbol);
        }

        return symbols;
    }

    /// <summary>Ottiene i timeframe (in minuti, come stringa) disponibili per un simbolo.</summary>
    public IEnumerable<string> GetAvailableBarTypes(string symbol)
    {
        if (!Directory.Exists(_repositoryPath))
            return Enumerable.Empty<string>();

        var safeSymbol = BuildSafeFileSymbol(ResolveTicker(symbol));
        var prefix = safeSymbol + "_";

        return Directory.GetFiles(_repositoryPath, $"{safeSymbol}_*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(name => name[prefix.Length..])
            .Where(minutes => int.TryParse(minutes, out _))
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
        var feed = ReadFeedFile(symbol, timeframeMinutes);
        var candles = ConvertToOhlcv(feed);
        return candles.Select(c => c.DateTime.Date).Distinct().OrderBy(d => d);
    }

    /// <summary>
    /// Carica i dati per una specifica data dal file feed corrispondente a symbol+barType.
    /// </summary>
    public async Task<DataSource?> LoadDataAsync(string symbol, DateTime date, string barType = "OneMinute", bool preferCalculated = true)
    {
        var timeframeMinutes = ConvertBarTypeToMinutes(barType);
        var feed = await ReadFeedFileAsync(symbol, timeframeMinutes);
        if (feed == null)
            return null;

        var targetDate = TradingDateTime.ToFeedUtc(date).Date;
        var candles = ConvertToOhlcv(feed).Where(c => c.DateTime.Date == targetDate).ToList();
        if (candles.Count == 0)
            return null;

        return new DataSource
        {
            Symbol = feed.Symbol,
            BarType = barType,
            LastUpdate = feed.GeneratedAtUtc,
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
        var feed = await ReadFeedFileAsync(symbol, timeframeMinutes);
        var candles = ConvertToOhlcv(feed);

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
        var feed = await ReadFeedFileAsync(symbol, timeframeMinutes);
        return ConvertToOhlcv(feed).OrderBy(c => c.DateTime).ToList();
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
