using Piootoo.Core.Services.Interfaces;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Utilities;

namespace Piootoo.Core.Services;

/// <summary>
/// Servizio per la lettura dei dati feed OHLCV
/// </summary>
public class PiootooDataFeedService : IPiootooDataFeedService
{
    private readonly DataSourceRepository _dataRepository;

    public PiootooDataFeedService(PiootooSettings settings)
    {
        _dataRepository = new DataSourceRepository(settings.GetRepositoryPath());
    }

    public async Task<OhlcvData[]> GetCandlesAsync(string symbol, DateTime currentDate, int numberOfCandles, int timeframeMinutes = 60)
    {
        currentDate = TradingDateTime.ToFeedUtc(currentDate);
        // Converte timeframe in bar type
        var barType = timeframeMinutes switch
        {
            1 => "OneMinute",
            5 => "FiveMinute",
            15 => "FifteenMinute",
            30 => "ThirtyMinute",
            60 => "OneHour",
            240 => "FourHour",
            1440 => "Daily",
            10080 => "Weekly",
            _ => "OneHour" // Default
        };
        
        // Per timeframe aggregati (15m, 30m, 1h, 4h), preferisci le cartelle -calculate.
        // Daily/weekly: usa il feed raw (D-calculate è spesso incompleto e non allineato al raw).
        var preferCalculated = timeframeMinutes >= 15 && timeframeMinutes != 5 && timeframeMinutes < 1440;
        
        // Carica i dati fino alla data corrente
        var endDate = currentDate;
        // Calcola startDate basandosi sul timeframe per avere abbastanza dati
        var daysBack = Math.Max(30, (numberOfCandles * timeframeMinutes) / (24 * 60) + 7); // Almeno 7 giorni extra
        var startDate = endDate.AddDays(-daysBack);
        
        var allData = await _dataRepository.LoadDataRangeAsync(symbol, startDate, endDate, barType, preferCalculated);
        
        if (!allData.Any())
            return Array.Empty<OhlcvData>();

        // Filtra i dati fino alla data corrente e prendi gli ultimi N
        var filteredData = allData
            .Where(d => d.DateTime <= currentDate)
            .OrderByDescending(d => d.DateTime)
            .Take(numberOfCandles)
            .OrderBy(d => d.DateTime) // Riordina cronologicamente
            .ToArray();

        return filteredData;
    }
}
