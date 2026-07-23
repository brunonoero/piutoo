using FeedWorker.Configuration;
using FeedWorker.Dto;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FeedWorker.Storage;

/// <summary>
/// Servizio per aggregare candele 1m in timeframe superiori con calcolo di Volume High e Volume Low
/// </summary>
public class CandleAggregationService
{
    private readonly ILogger<CandleAggregationService> _logger;
    private readonly RepositoryOptions _repositoryOptions;
    private readonly AggregationOptions _aggregationOptions;

    private static DateTime CreateUtc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public CandleAggregationService(
        ILogger<CandleAggregationService> logger,
        IOptions<RepositoryOptions> repositoryOptions,
        IOptions<AggregationOptions> aggregationOptions)
    {
        _logger = logger;
        _repositoryOptions = repositoryOptions.Value;
        _aggregationOptions = aggregationOptions.Value;
    }

    /// <summary>
    /// Aggrega le candele 1m appena salvate in timeframe superiori (1h, 4h, D, W)
    /// </summary>
    public async Task AggregateCandlesAsync(string symbol, List<Candle> oneMinuteCandles)
    {
        if (!_aggregationOptions.Enabled)
        {
            _logger.LogDebug("Aggregazione disabilitata per {Symbol}", symbol);
            return;
        }

        if (oneMinuteCandles == null || oneMinuteCandles.Count == 0)
        {
            _logger.LogWarning("Nessuna candela 1m da aggregare per {Symbol}", symbol);
            return;
        }

        _logger.LogInformation("Inizio aggregazione per {Symbol}: {Count} candele 1m", symbol, oneMinuteCandles.Count);

        // Carica tutte le candele 1m disponibili per il simbolo (per avere dati completi per l'aggregazione)
        var allOneMinuteCandles = await LoadAllOneMinuteCandlesAsync(symbol, oneMinuteCandles);

        // Aggrega in 15m, 30m, 1h, 4h, D, W
        var tasks = new List<Task>();

        if (_aggregationOptions.AggregateTo15M)
        {
            tasks.Add(AggregateToTimeframeAsync(symbol, allOneMinuteCandles, CandleInterval.FifteenMinutes));
        }

        if (_aggregationOptions.AggregateTo30M)
        {
            tasks.Add(AggregateToTimeframeAsync(symbol, allOneMinuteCandles, CandleInterval.ThirtyMinutes));
        }

        if (_aggregationOptions.AggregateTo1H)
        {
            tasks.Add(AggregateToTimeframeAsync(symbol, allOneMinuteCandles, CandleInterval.OneHour));
        }

        if (_aggregationOptions.AggregateTo4H)
        {
            tasks.Add(AggregateToTimeframeAsync(symbol, allOneMinuteCandles, CandleInterval.FourHours));
        }

        if (_aggregationOptions.AggregateToDaily)
        {
            tasks.Add(AggregateToTimeframeAsync(symbol, allOneMinuteCandles, CandleInterval.OneDay));
        }

        if (_aggregationOptions.AggregateToWeekly)
        {
            tasks.Add(AggregateToTimeframeAsync(symbol, allOneMinuteCandles, CandleInterval.OneWeek));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Aggregazione completata per {Symbol}", symbol);
    }

    /// <summary>
    /// Carica tutte le candele 1m disponibili per il simbolo, combinando quelle nuove con quelle esistenti
    /// Per aggregazioni Weekly, carica anche i giorni precedenti della settimana
    /// </summary>
    private async Task<List<Candle>> LoadAllOneMinuteCandlesAsync(string symbol, List<Candle> newCandles)
    {
        var allCandles = new List<Candle>(newCandles);
        var dates = newCandles.Select(c => EnsureUtc(c.GetDateTime()).Date).Distinct().ToList();

        // Per aggregazioni Weekly, aggiungi anche i giorni precedenti della settimana corrente
        if (_aggregationOptions.AggregateToWeekly)
        {
            var minDate = dates.Min();
            var weekStart = GetWeekStart(minDate);
            // Aggiungi i giorni dalla settimana corrente (fino a 7 giorni prima)
            for (int i = 0; i < 7; i++)
            {
                var dateToLoad = weekStart.AddDays(i);
                if (!dates.Contains(dateToLoad) && dateToLoad <= DateTime.UtcNow.Date)
                {
                    dates.Add(dateToLoad);
                }
            }
        }

        // Per aggregazioni Daily, aggiungi anche il giorno precedente (potrebbe essere necessario per aggregazioni che iniziano a mezzanotte)
        if (_aggregationOptions.AggregateToDaily)
        {
            var minDate = dates.Min();
            var previousDay = minDate.AddDays(-1);
            if (!dates.Contains(previousDay))
            {
                dates.Add(previousDay);
            }
        }

        foreach (var date in dates.OrderBy(d => d))
        {
            try
            {
                var filePath = _repositoryOptions.GetFilePath(symbol, CandleInterval.OneMinute, date);
                if (File.Exists(filePath))
                {
                    var existingData = await File.ReadAllTextAsync(filePath);
                    var existingResponse = JsonSerializer.Deserialize<CandleResponseDto>(existingData, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (existingResponse?.Candles != null)
                    {
                        // Converte i DTO in Candle
                        var existingCandles = existingResponse.Candles.Select(dto => new Candle
                        {
                            Time = dto.Timestamp,
                            Open = dto.Open,
                            High = dto.High,
                            Low = dto.Low,
                            Close = dto.Close,
                            Volume = dto.Volume
                        }).ToList();

                        // Aggiungi solo quelle non già presenti (basandosi sul timestamp)
                        var newTimestamps = new HashSet<double>(newCandles.Select(c => c.Time));
                        var missingCandles = existingCandles.Where(c => !newTimestamps.Contains(c.Time));
                        allCandles.AddRange(missingCandles);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore durante il caricamento delle candele 1m esistenti per {Symbol} in data {Date}", symbol, date);
            }
        }

        // Rimuovi duplicati e ordina per timestamp
        return allCandles
            .GroupBy(c => c.Time)
            .Select(g => g.First())
            .OrderBy(c => c.Time)
            .ToList();
    }

    /// <summary>
    /// Aggrega le candele 1m in un timeframe specifico
    /// </summary>
    private async Task AggregateToTimeframeAsync(string symbol, List<Candle> oneMinuteCandles, CandleInterval targetInterval)
    {
        try
        {
            var aggregatedCandles = AggregateCandles(oneMinuteCandles, targetInterval);
            
            if (aggregatedCandles.Count == 0)
            {
                _logger.LogWarning("Nessuna candela aggregata per {Symbol} in timeframe {Interval}", symbol, targetInterval);
                return;
            }

            // Raggruppa per data per salvare in file separati
            var candlesByDate = aggregatedCandles
                .GroupBy(c => EnsureUtc(c.DateTime).Date)
                .ToList();

            foreach (var dateGroup in candlesByDate)
            {
                var date = dateGroup.Key;
                var candlesForDate = dateGroup.ToList();

                await SaveAggregatedCandlesAsync(symbol, targetInterval, date, candlesForDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggregazione per {Symbol} in timeframe {Interval}", symbol, targetInterval);
        }
    }

    /// <summary>
    /// Aggrega le candele 1m nel timeframe target calcolando Volume High e Volume Low
    /// </summary>
    private List<AggregatedCandleDto> AggregateCandles(List<Candle> oneMinuteCandles, CandleInterval targetInterval)
    {
        var minutesPerBar = targetInterval switch
        {
            CandleInterval.FifteenMinutes => 15,
            CandleInterval.ThirtyMinutes => 30,
            CandleInterval.OneHour => 60,
            CandleInterval.FourHours => 240,
            CandleInterval.OneDay => 1440,
            CandleInterval.OneWeek => 10080,
            _ => throw new ArgumentException($"Timeframe {targetInterval} non supportato per aggregazione")
        };

        // Ordina le candele per timestamp
        var sortedCandles = oneMinuteCandles.OrderBy(c => c.Time).ToList();

        var aggregated = new List<AggregatedCandleDto>();
        var currentGroup = new List<Candle>();

        foreach (var candle in sortedCandles)
        {
            var candleTime = EnsureUtc(candle.GetDateTime());

            // Determina il timestamp di inizio del periodo aggregato (UTC)
            DateTime periodStart = targetInterval switch
            {
                CandleInterval.FifteenMinutes => CreateUtc(candleTime.Year, candleTime.Month, candleTime.Day, candleTime.Hour, (candleTime.Minute / 15) * 15),
                CandleInterval.ThirtyMinutes => CreateUtc(candleTime.Year, candleTime.Month, candleTime.Day, candleTime.Hour, (candleTime.Minute / 30) * 30),
                CandleInterval.OneHour => CreateUtc(candleTime.Year, candleTime.Month, candleTime.Day, candleTime.Hour),
                CandleInterval.FourHours => CreateUtc(candleTime.Year, candleTime.Month, candleTime.Day, (candleTime.Hour / 4) * 4),
                CandleInterval.OneDay => CreateUtc(candleTime.Year, candleTime.Month, candleTime.Day),
                CandleInterval.OneWeek => GetWeekStart(candleTime),
                _ => candleTime
            };

            // Se è un nuovo periodo, salva il gruppo precedente e inizia uno nuovo
            if (currentGroup.Count > 0)
            {
                var lastPeriodStart = GetPeriodStart(currentGroup[0].GetDateTime(), targetInterval);
                if (periodStart != lastPeriodStart)
                {
                    aggregated.Add(CreateAggregatedCandle(currentGroup, targetInterval));
                    currentGroup.Clear();
                }
            }

            currentGroup.Add(candle);
        }

        // Aggiungi l'ultimo gruppo
        if (currentGroup.Count > 0)
        {
            aggregated.Add(CreateAggregatedCandle(currentGroup, targetInterval));
        }

        return aggregated;
    }

    /// <summary>
    /// Crea una candela aggregata da un gruppo di candele 1m
    /// </summary>
    private AggregatedCandleDto CreateAggregatedCandle(List<Candle> candles, CandleInterval targetInterval)
    {
        if (candles.Count == 0)
            throw new ArgumentException("Lista candele vuota");

        var firstCandle = candles[0];
        var lastCandle = candles[candles.Count - 1];
        var periodStart = EnsureUtc(GetPeriodStart(firstCandle.GetDateTime(), targetInterval));

        // Calcola OHLC
        var open = firstCandle.Open;
        var close = lastCandle.Close;
        var high = candles.Max(c => c.High);
        var low = candles.Min(c => c.Low);
        var totalVolume = candles.Sum(c => c.Volume);

        // Calcola Volume High (candele verdi: close > open) e Volume Low (candele rosse: close < open)
        var volumeHigh = candles
            .Where(c => c.Close > c.Open)
            .Sum(c => c.Volume);

        var volumeLow = candles
            .Where(c => c.Close <= c.Open)
            .Sum(c => c.Volume);

        // Le candele con close == open non contribuiscono né a volume high né a volume low

        return new AggregatedCandleDto
        {
            Timestamp = new DateTimeOffset(periodStart, TimeSpan.Zero).ToUnixTimeSeconds(),
            DateTime = periodStart,
            DateTimeFormatted = periodStart.ToString("yyyy-MM-dd HH:mm:ss"),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = totalVolume,
            VolumeHigh = volumeHigh,
            VolumeLow = volumeLow
        };
    }

    /// <summary>
    /// Ottiene l'inizio del periodo per un timeframe specifico
    /// </summary>
    private DateTime GetPeriodStart(DateTime dateTime, CandleInterval interval)
    {
        dateTime = EnsureUtc(dateTime);
        return interval switch
        {
            CandleInterval.FifteenMinutes => CreateUtc(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, (dateTime.Minute / 15) * 15),
            CandleInterval.ThirtyMinutes => CreateUtc(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, (dateTime.Minute / 30) * 30),
            CandleInterval.OneHour => CreateUtc(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour),
            CandleInterval.FourHours => CreateUtc(dateTime.Year, dateTime.Month, dateTime.Day, (dateTime.Hour / 4) * 4),
            CandleInterval.OneDay => CreateUtc(dateTime.Year, dateTime.Month, dateTime.Day),
            CandleInterval.OneWeek => GetWeekStart(dateTime),
            _ => dateTime
        };
    }

    /// <summary>
    /// Ottiene l'inizio della settimana (Lunedì 00:00 UTC)
    /// </summary>
    private DateTime GetWeekStart(DateTime dateTime)
    {
        dateTime = EnsureUtc(dateTime);
        var daysFromMonday = ((int)dateTime.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return CreateUtc(dateTime.Year, dateTime.Month, dateTime.Day).AddDays(-daysFromMonday);
    }

    /// <summary>
    /// Salva le candele aggregate in file con suffisso "-calculate"
    /// </summary>
    private async Task SaveAggregatedCandlesAsync(string symbol, CandleInterval interval, DateTime date, List<AggregatedCandleDto> candles)
    {
        // Genera il percorso con suffisso "-calculate"
        var folderName = $"{interval.ToFolderName()}-calculate";
        var basePath = _repositoryOptions.BasePath;
        var sanitizedSymbol = SanitizeSymbol(symbol);
        var fileName = $"{sanitizedSymbol}-{date:yyyyMMdd}.json";
        
        var directory = Path.Combine(basePath, "datafeed", folderName, sanitizedSymbol);
        var filePath = Path.Combine(directory, fileName);

        // Crea la directory se non esiste
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Creata directory: {Directory}", directory);
        }

        // Carica le candele esistenti se il file esiste
        var existingCandles = new List<AggregatedCandleDto>();
        if (File.Exists(filePath))
        {
            try
            {
                var existingData = await File.ReadAllTextAsync(filePath);
                var existingResponse = JsonSerializer.Deserialize<AggregatedCandleResponseDto>(existingData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (existingResponse?.Candles != null)
                {
                    existingCandles = existingResponse.Candles;
                    _logger.LogInformation("Caricate {Count} candele aggregate esistenti da {FilePath}", existingCandles.Count, filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore durante la lettura del file esistente {FilePath}. Verrà creato un nuovo file.", filePath);
            }
        }

        // Filtra le candele mancanti (basandosi sul timestamp)
        var existingTimestamps = new HashSet<double>(existingCandles.Select(c => c.Timestamp));
        var missingCandles = candles
            .Where(c => !existingTimestamps.Contains(c.Timestamp))
            .ToList();

        if (missingCandles.Count == 0)
        {
            _logger.LogInformation("Nessuna candela aggregata nuova da aggiungere per {Symbol} in timeframe {Interval} in data {Date}", 
                symbol, interval, date);
            return;
        }

        // Combina le candele esistenti con quelle nuove e ordina per timestamp
        var allCandles = existingCandles
            .Concat(missingCandles)
            .OrderBy(c => c.Timestamp)
            .ToList();

        // Crea il DTO di risposta completo
        var responseDto = new AggregatedCandleResponseDto
        {
            Symbol = symbol,
            BarType = $"{interval}-calculated",
            CandleCount = allCandles.Count,
            Candles = allCandles,
            LastUpdate = DateTime.UtcNow
        };

        // Salva il file
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(responseDto, jsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        _logger.LogInformation("Salvate {NewCount} nuove candele aggregate (totale: {TotalCount}) per {Symbol} in timeframe {Interval} in data {Date} in {FilePath}",
            missingCandles.Count, allCandles.Count, symbol, interval, date, filePath);
    }

    private static string SanitizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return "unknown";

        var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct();
        var sanitized = symbol;
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }
}

/// <summary>
/// DTO per candela aggregata con Volume High e Volume Low
/// </summary>
public class AggregatedCandleDto
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
/// DTO per la risposta con candele aggregate
/// </summary>
public class AggregatedCandleResponseDto
{
    public string Symbol { get; set; } = string.Empty;
    public string BarType { get; set; } = string.Empty;
    public int CandleCount { get; set; }
    public List<AggregatedCandleDto> Candles { get; set; } = new();
    public DateTime LastUpdate { get; set; }
}
