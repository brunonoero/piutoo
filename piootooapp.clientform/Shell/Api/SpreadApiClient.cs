using System.Text.Json;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;

namespace piootooapp.clientform.Shell.Api;

/// <summary>
/// Le misure di spread (<c>piootoo-repository/spread</c>) attraverso il server: la console non apre
/// i file del repository, li chiede via HTTP come qualsiasi altro dato.
/// </summary>
public sealed class SpreadApiClient : ApiClientBase
{
    public SpreadApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    /// <summary>
    /// Broker che hanno una misura. Elenco vuoto = nessuna misura raccolta: è un'assenza normale,
    /// non un errore.
    /// </summary>
    public Task<List<SpreadBrokerInfo>> ListBrokersAsync(CancellationToken cancellationToken = default)
        => SendForAsync<List<SpreadBrokerInfo>>(
            HttpMethod.Get,
            "api/Spread/brokers",
            null,
            cancellationToken);

    /// <summary>
    /// Gli spread che un run applicherebbe con questa combinazione, prima di lanciarlo.
    /// </summary>
    public Task<SpreadTableInfo> GetTableAsync(
        string broker,
        SpreadStatistic statistic,
        SpreadResolution resolution,
        CancellationToken cancellationToken = default)
        => SendForAsync<SpreadTableInfo>(
            HttpMethod.Get,
            $"api/Spread/table?broker={Escape(broker)}&statistic={statistic}&resolution={resolution}",
            null,
            cancellationToken);
}
