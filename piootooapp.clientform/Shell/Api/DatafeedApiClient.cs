using System.Text.Json;
using Piootoo.Shared.Models;

namespace piootooapp.clientform.Shell.Api;

/// <summary>
/// Lettura del datafeed locale (<c>piootoo-repository/datafeed</c>) attraverso il server: la
/// console non apre i file del repository, li chiede via HTTP come qualsiasi altro dato.
/// </summary>
public sealed class DatafeedApiClient : ApiClientBase
{
    public DatafeedApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Broker con un archivio esterno in <c>piootoo-repository/datafeed-external</c>. Il datafeed
    /// interno non è nell'elenco: è l'assenza di broker.
    /// </summary>
    public Task<List<DatafeedBrokerInfo>> ListBrokersAsync(CancellationToken cancellationToken = default)
        => SendForAsync<List<DatafeedBrokerInfo>>(
            HttpMethod.Get,
            "api/Datafeed/brokers",
            null,
            cancellationToken);

    /// <summary>
    /// Feed disponibili con il periodo che coprono, di tutti gli archivi quando
    /// <paramref name="broker"/> è null: ogni riga dichiara la propria sorgente.
    /// </summary>
    public Task<List<DatafeedFeedInfo>> ListFeedsAsync(
        string? broker = null,
        CancellationToken cancellationToken = default)
        => SendForAsync<List<DatafeedFeedInfo>>(
            HttpMethod.Get,
            broker == null ? "api/Datafeed/feeds" : $"api/Datafeed/feeds?broker={Escape(broker)}",
            null,
            cancellationToken);

    /// <summary>
    /// Ultime <paramref name="sessions"/> sessioni di mercato per il simbolo, nel timeframe
    /// indicato da <paramref name="barType"/> (<c>OneMinute</c>, <c>OneHour</c>, <c>Daily</c>…).
    /// </summary>
    public Task<List<OhlcvData>> GetLatestAsync(
        string symbol,
        int sessions,
        string barType,
        CancellationToken cancellationToken = default)
        => SendForAsync<List<OhlcvData>>(
            HttpMethod.Get,
            $"api/PiootooRealtime/data/{Escape(symbol)}/latest?sessions={sessions}&barType={Escape(barType)}",
            null,
            cancellationToken);

    /// <summary>Timeframe in minuti → barType accettato dall'API. Vedi <c>DataSourceRepository</c>.</summary>
    public static string BarTypeFor(int timeframeMinutes) => timeframeMinutes switch
    {
        1 => "OneMinute",
        5 => "FiveMinute",
        15 => "FifteenMinute",
        30 => "ThirtyMinute",
        60 => "OneHour",
        240 => "FourHour",
        1440 => "Daily",
        10080 => "Weekly",
        _ => throw new InvalidOperationException(
            $"Timeframe {timeframeMinutes} minuti non è fra quelli del datafeed.")
    };
}
