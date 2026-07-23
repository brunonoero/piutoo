using System.Text.Json;
using Piootoo.Shared.Models;
using Piootoo.Shared.Utilities;

namespace Piootoo.Domain.Repositories;

/// <summary>
/// Repository per accedere ai datasource OHLCV dal repository locale
/// </summary>
public class DataSourceRepository
{
    private readonly string _repositoryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public DataSourceRepository(string repositoryPath)
    {
        _repositoryPath = repositoryPath;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Ottiene la lista dei simboli disponibili nel repository
    /// Cerca sia nella struttura vecchia (ds-[symbol]) che nuova ([interval]/[symbol])
    /// </summary>
    public IEnumerable<string> GetAvailableSymbols()
    {
        if (!Directory.Exists(_repositoryPath))
            return Enumerable.Empty<string>();

        var symbols = new HashSet<string>();

        // Cerca nella struttura vecchia: ds-[symbol]
        var oldStructureDirs = Directory.GetDirectories(_repositoryPath)
            .Select(d => Path.GetFileName(d))
            .Where(name => name.StartsWith("ds-"))
            .Select(name => name.Substring(3)); // Rimuove "ds-"
        foreach (var symbol in oldStructureDirs)
        {
            symbols.Add(symbol);
        }

        // Cerca nella struttura nuova: [interval]/[symbol] o [interval]-calculate/[symbol]
        var intervalDirs = Directory.GetDirectories(_repositoryPath)
            .Select(d => Path.GetFileName(d));
        
        foreach (var intervalDir in intervalDirs)
        {
            var intervalPath = Path.Combine(_repositoryPath, intervalDir);
            if (Directory.Exists(intervalPath))
            {
                var symbolDirs = Directory.GetDirectories(intervalPath)
                    .Select(d => Path.GetFileName(d));
                foreach (var symbol in symbolDirs)
                {
                    symbols.Add(symbol);
                }
            }
        }

        return symbols;
    }

    /// <summary>
    /// Ottiene i tipi di bar disponibili per un simbolo
    /// Cerca sia nella struttura vecchia che nuova
    /// </summary>
    public IEnumerable<string> GetAvailableBarTypes(string symbol)
    {
        var barTypes = new HashSet<string>();

        // Cerca nella struttura vecchia: ds-[symbol]/[barType]
        var oldSymbolPath = Path.Combine(_repositoryPath, $"ds-{symbol}");
        if (Directory.Exists(oldSymbolPath))
        {
            var oldBarTypes = Directory.GetDirectories(oldSymbolPath)
                .Select(d => Path.GetFileName(d));
            foreach (var barType in oldBarTypes)
            {
                barTypes.Add(barType);
            }
        }

        // Cerca nella struttura nuova: [interval]/[symbol] o [interval]-calculate/[symbol]
        var intervalDirs = Directory.GetDirectories(_repositoryPath)
            .Select(d => Path.GetFileName(d));
        
        foreach (var intervalDir in intervalDirs)
        {
            var symbolPath = Path.Combine(_repositoryPath, intervalDir, symbol);
            if (Directory.Exists(symbolPath))
            {
                // Estrai il barType dal nome della cartella (rimuovi -calculate se presente)
                var barType = intervalDir.Replace("-calculate", "");
                barTypes.Add(barType);
            }
        }

        return barTypes;
    }

    /// <summary>
    /// Ottiene le date disponibili per un simbolo e tipo di bar
    /// Cerca prima nelle cartelle -calculate, poi nelle cartelle normali
    /// </summary>
    public IEnumerable<DateTime> GetAvailableDates(string symbol, string barType = "OneMinute", bool preferCalculated = true)
    {
        var dates = new HashSet<DateTime>();

        // Converte barType nel formato cartella (es. "OneHour" -> "1h")
        var folderName = ConvertBarTypeToFolderName(barType);
        
        // Cerca prima nelle cartelle -calculate se preferito
        if (preferCalculated)
        {
            var calculatedPath = Path.Combine(_repositoryPath, $"{folderName}-calculate", symbol);
            if (Directory.Exists(calculatedPath))
            {
                var calculatedDates = GetDatesFromPath(calculatedPath);
                foreach (var date in calculatedDates)
                {
                    dates.Add(date);
                }
            }
        }

        // Cerca nelle cartelle normali
        var normalPath = Path.Combine(_repositoryPath, folderName, symbol);
        if (Directory.Exists(normalPath))
        {
            var normalDates = GetDatesFromPath(normalPath);
            foreach (var date in normalDates)
            {
                dates.Add(date);
            }
        }


        return dates.OrderBy(d => d);
    }

    /// <summary>
    /// Estrae le date dai file JSON in un percorso
    /// </summary>
    private IEnumerable<DateTime> GetDatesFromPath(string path)
    {
        return Directory.GetFiles(path, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Select(name =>
            {
                // Formato: @ES-20260116 o symbol-20260116
                var datePart = name.Split('-').LastOrDefault();
                if (datePart != null && datePart.Length == 8 &&
                    DateTime.TryParseExact(datePart, "yyyyMMdd", null, 
                        System.Globalization.DateTimeStyles.None, out var date))
                {
                    return date;
                }
                return DateTime.MinValue;
            })
            .Where(d => d != DateTime.MinValue);
    }

    /// <summary>
    /// Converte un barType (es. "OneHour") nel formato cartella (es. "1h")
    /// </summary>
    private string ConvertBarTypeToFolderName(string barType)
    {
        return barType switch
        {
            "OneMinute" => "1m",
            "FiveMinute" => "5m",
            "FifteenMinute" => "15m",
            "ThirtyMinute" => "30m",
            "OneHour" => "1h",
            "FourHours" => "4h",
            "Daily" => "D",
            "Weekly" => "W",
            _ => barType.ToLower()
        };
    }

    /// <summary>
    /// Carica i dati per una specifica data
    /// Cerca prima nelle cartelle -calculate, poi nelle cartelle normali
    /// </summary>
    public async Task<DataSource?> LoadDataAsync(string symbol, DateTime date, string barType = "OneMinute", bool preferCalculated = true)
    {
        var fileName = $"{symbol}-{date:yyyyMMdd}.json";
        var folderName = ConvertBarTypeToFolderName(barType);

        // Cerca prima nelle cartelle -calculate se preferito
        if (preferCalculated)
        {
            var calculatedPath = Path.Combine(_repositoryPath, $"{folderName}-calculate", symbol, fileName);
            if (File.Exists(calculatedPath))
            {
                var json = await File.ReadAllTextAsync(calculatedPath);
                // Prova a deserializzare come AggregatedCandleResponseDto
                try
                {
                    var aggregatedResponse = JsonSerializer.Deserialize<AggregatedCandleResponseDto>(json, _jsonOptions);
                    if (aggregatedResponse != null && aggregatedResponse.Candles != null)
                    {
                        // Converte AggregatedCandleDto in DataSource
                        return FinalizeDataSource(ConvertAggregatedToDataSource(aggregatedResponse, symbol, barType));
                    }
                }
                catch
                {
                    // Se fallisce, prova come DataSource normale
                }
                // Fallback: prova a deserializzare come DataSource normale
                return FinalizeDataSource(JsonSerializer.Deserialize<DataSource>(json, _jsonOptions));
            }
        }

        // Cerca nelle cartelle normali
        var normalPath = Path.Combine(_repositoryPath, folderName, symbol, fileName);
        if (File.Exists(normalPath))
        {
            var json = await File.ReadAllTextAsync(normalPath);
            return FinalizeDataSource(JsonSerializer.Deserialize<DataSource>(json, _jsonOptions));
        }

        // Fallback alla struttura vecchia: ds-[symbol]/[barType]
        var oldPath = Path.Combine(_repositoryPath, $"ds-{symbol}", barType, fileName);
        if (File.Exists(oldPath))
        {
            var json = await File.ReadAllTextAsync(oldPath);
            return FinalizeDataSource(JsonSerializer.Deserialize<DataSource>(json, _jsonOptions));
        }

        return null;
    }

    /// <summary>
    /// Carica i dati per un range di date
    /// Cerca prima nelle cartelle -calculate, poi nelle cartelle normali
    /// </summary>
    public async Task<List<OhlcvData>> LoadDataRangeAsync(string symbol, DateTime startDate, DateTime endDate, string barType = "OneMinute", bool preferCalculated = true)
    {
        startDate = TradingDateTime.ToFeedUtc(startDate);
        endDate = TradingDateTime.ToFeedUtc(endDate);

        var allCandles = new List<OhlcvData>();
        var availableDates = GetAvailableDates(symbol, barType, preferCalculated)
            .Where(d => d >= startDate.Date && d <= endDate.Date)
            .ToList();

        foreach (var date in availableDates)
        {
            var dataSource = await LoadDataAsync(symbol, date, barType, preferCalculated);
            if (dataSource?.Candles != null)
            {
                allCandles.AddRange(dataSource.Candles);
            }
        }

        return allCandles.OrderBy(c => c.DateTime).ToList();
    }

    /// <summary>
    /// Carica tutti i dati disponibili per un simbolo
    /// </summary>
    public async Task<List<OhlcvData>> LoadAllDataAsync(string symbol, string barType = "OneMinute")
    {
        var allCandles = new List<OhlcvData>();
        var availableDates = GetAvailableDates(symbol, barType).ToList();

        foreach (var date in availableDates)
        {
            var dataSource = await LoadDataAsync(symbol, date, barType);
            if (dataSource?.Candles != null)
            {
                allCandles.AddRange(dataSource.Candles);
            }
        }

        return allCandles.OrderBy(c => c.DateTime).ToList();
    }

    /// <summary>
    /// Carica i dati per le ultime N sessioni
    /// </summary>
    public async Task<List<OhlcvData>> LoadLastSessionsAsync(string symbol, int sessions, string barType = "OneMinute")
    {
        var allCandles = new List<OhlcvData>();
        var availableDates = GetAvailableDates(symbol, barType)
            .OrderByDescending(d => d)
            .Take(sessions)
            .OrderBy(d => d)
            .ToList();

        foreach (var date in availableDates)
        {
            var dataSource = await LoadDataAsync(symbol, date, barType);
            if (dataSource?.Candles != null)
            {
                allCandles.AddRange(dataSource.Candles);
            }
        }

        return allCandles.OrderBy(c => c.DateTime).ToList();
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

    /// <summary>
    /// Converte AggregatedCandleResponseDto in DataSource
    /// </summary>
    private DataSource ConvertAggregatedToDataSource(AggregatedCandleResponseDto aggregatedResponse, string symbol, string barType)
    {
        var candles = aggregatedResponse.Candles.Select(ac => new OhlcvData
        {
            Timestamp = (long)ac.Timestamp,
            DateTime = ac.DateTime,
            DateTimeFormatted = ac.DateTimeFormatted,
            Open = (decimal)ac.Open,
            High = (decimal)ac.High,
            Low = (decimal)ac.Low,
            Close = (decimal)ac.Close,
            Volume = (decimal)ac.Volume
        }).ToList();

        TradingDateTime.NormalizeCandlesToUtc(candles);

        return new DataSource
        {
            Symbol = symbol,
            BarType = barType,
            LastUpdate = aggregatedResponse.LastUpdate,
            CandleCount = candles.Count,
            Candles = candles
        };
    }

    private static DataSource? FinalizeDataSource(DataSource? dataSource)
    {
        if (dataSource?.Candles != null)
        {
            TradingDateTime.NormalizeCandlesToUtc(dataSource.Candles);
        }

        return dataSource;
    }
}

/// <summary>
/// DTO per leggere le risposte dalle cartelle -calculate
/// </summary>
internal class AggregatedCandleResponseDto
{
    public string Symbol { get; set; } = string.Empty;
    public string BarType { get; set; } = string.Empty;
    public int CandleCount { get; set; }
    public List<AggregatedCandleDto> Candles { get; set; } = new();
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// DTO per candela aggregata con Volume High e Volume Low
/// </summary>
internal class AggregatedCandleDto
{
    public double Timestamp { get; set; }
    public DateTime DateTime { get; set; }
    public string DateTimeFormatted { get; set; } = string.Empty;
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public double Volume { get; set; }
    public double VolumeHigh { get; set; }
    public double VolumeLow { get; set; }
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
