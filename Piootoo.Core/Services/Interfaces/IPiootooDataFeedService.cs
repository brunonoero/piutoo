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
    /// <param name="broker">
    /// Archivio esterno da cui leggere (<c>datafeed-external/{broker}</c>). Null o vuoto = datafeed
    /// interno. Vedi <see cref="IDatafeedCatalog"/>.
    /// </param>
    /// <returns>Array di dati OHLCV ordinati cronologicamente</returns>
    Task<OhlcvData[]> GetCandlesAsync(
        string symbol,
        DateTime currentDate,
        int numberOfCandles,
        int timeframeMinutes = 60,
        string? broker = null);

    /// <summary>
    /// Carica tutte le candele comprese in un intervallo esplicito.
    ///
    /// Da preferire a <see cref="GetCandlesAsync"/> per il pre-caricamento di un backtest: quel
    /// metodo deduce l'inizio dal numero di candele richieste assumendo una densità 24/7, mentre
    /// i future hanno weekend e sessioni non continue. L'inizio calcolato risultava troppo
    /// recente e la prima parte dell'intervallo richiesto restava senza dati.
    /// </summary>
    /// <param name="broker">
    /// Archivio esterno da cui leggere (<c>datafeed-external/{broker}</c>). Null o vuoto = datafeed
    /// interno. Un run legge sempre da una sola radice: il parametro e' lo stesso per tutti i
    /// datasource dello stesso backtest.
    /// </param>
    Task<OhlcvData[]> GetCandlesRangeAsync(
        string symbol,
        DateTime startUtc,
        DateTime endUtc,
        int timeframeMinutes,
        string? broker = null);
}
