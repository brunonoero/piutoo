using Piootoo.Shared.Models;

namespace Piootoo.Core.Services.Interfaces;

/// <summary>
/// Servizio per la lettura dei dati feed OHLCV
/// </summary>
public interface IPiootooDataFeedService
{
    /// <summary>
    /// Ottiene N candle indietro da una data specifica
    /// </summary>
    /// <param name="symbol">Simbolo (es. @ES, @NQ)</param>
    /// <param name="currentDate">Data corrente</param>
    /// <param name="numberOfCandles">Numero di candle da recuperare</param>
    /// <param name="timeframeMinutes">Timeframe in minuti (es. 60 per 1 ora, 15 per 15 minuti)</param>
    /// <returns>Array di dati OHLCV ordinati cronologicamente</returns>
    Task<OhlcvData[]> GetCandlesAsync(string symbol, DateTime currentDate, int numberOfCandles, int timeframeMinutes = 60);
}
