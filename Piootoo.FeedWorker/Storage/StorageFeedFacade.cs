using FeedWorker.Configuration;
using FeedWorker.Dto;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FeedWorker.Storage;

/// <summary>
/// Facade per gestire il salvataggio delle candele in file JSON
/// Raggruppa le candele per data e appende solo quelle mancanti
/// </summary>
public class StorageFeedFacade
{
    private readonly ILogger<StorageFeedFacade> _logger;
    private readonly RepositoryOptions _repositoryOptions;

    public StorageFeedFacade(
        ILogger<StorageFeedFacade> logger,
        IOptions<RepositoryOptions> repositoryOptions)
    {
        _logger = logger;
        _repositoryOptions = repositoryOptions.Value;
    }

    /// <summary>
    /// Salva le candele raggruppandole per data e appendendo solo quelle mancanti
    /// </summary>
    /// <param name="symbol">Simbolo</param>
    /// <param name="interval">Intervallo delle candele</param>
    /// <param name="candles">Lista di candele da salvare</param>
    public async Task SaveCandlesAsync(string symbol, CandleInterval interval, List<Candle> candles)
    {
        if (candles == null || candles.Count == 0)
        {
            _logger.LogWarning("Nessuna candela da salvare per {Symbol}", symbol);
            return;
        }

        // Raggruppa le candele per data (basandosi sulla data della candela)
        var candlesByDate = candles
            .GroupBy(c => c.GetDateTime().Date)
            .ToList();

        _logger.LogInformation("Salvataggio {Count} candele per {Symbol}, raggruppate in {DateCount} date",
            candles.Count, symbol, candlesByDate.Count);

        foreach (var dateGroup in candlesByDate)
        {
            var date = dateGroup.Key;
            var candlesForDate = dateGroup.ToList();

            try
            {
                await SaveCandlesForDateAsync(symbol, interval, date, candlesForDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio delle candele per {Symbol} in data {Date}",
                    symbol, date);
            }
        }
    }

    /// <summary>
    /// Salva le candele per una specifica data, appendendo solo quelle mancanti
    /// </summary>
    private async Task SaveCandlesForDateAsync(
        string symbol,
        CandleInterval interval,
        DateTime date,
        List<Candle> newCandles)
    {
        // Genera il percorso del file basato sulla data della candela
        var filePath = _repositoryOptions.GetFilePath(symbol, interval, date);

        // Crea la directory se non esiste
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Creata directory: {Directory}", directory);
        }

        // Carica le candele esistenti se il file esiste
        var existingCandles = new List<CandleDto>();
        if (File.Exists(filePath))
        {
            try
            {
                var existingData = await File.ReadAllTextAsync(filePath);
                var existingResponse = JsonSerializer.Deserialize<CandleResponseDto>(existingData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (existingResponse?.Candles != null)
                {
                    existingCandles = existingResponse.Candles;
                    _logger.LogInformation("Caricate {Count} candele esistenti da {FilePath}",
                        existingCandles.Count, filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore durante la lettura del file esistente {FilePath}. Verrà creato un nuovo file.",
                    filePath);
            }
        }

        // Converte le nuove candele in DTO
        var newCandlesDto = newCandles.Select(c => c.ToDto()).ToList();

        // Filtra le candele mancanti (basandosi sul timestamp)
        var existingTimestamps = new HashSet<double>(existingCandles.Select(c => c.Timestamp));
        var missingCandles = newCandlesDto
            .Where(c => !existingTimestamps.Contains(c.Timestamp))
            .ToList();

        if (missingCandles.Count == 0)
        {
            _logger.LogInformation("Nessuna candela nuova da aggiungere per {Symbol} in data {Date}",
                symbol, date);
            return;
        }

        // Combina le candele esistenti con quelle nuove e ordina per timestamp
        var allCandles = existingCandles
            .Concat(missingCandles)
            .OrderBy(c => c.Timestamp)
            .ToList();

        // Crea il DTO di risposta completo
        var responseDto = new CandleResponseDto
        {
            Symbol = symbol,
            BarType = interval.ToString(),
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

        _logger.LogInformation("Salvate {NewCount} nuove candele (totale: {TotalCount}) per {Symbol} in data {Date} in {FilePath}",
            missingCandles.Count, allCandles.Count, symbol, date, filePath);
    }
}
