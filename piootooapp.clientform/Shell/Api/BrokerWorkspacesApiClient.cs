using System.Text.Json;
using Piootoo.Shared.Models.BrokerWorkspaces;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Api;

/// <summary>
/// I piani in produzione, uno spazio per broker. Come i best plan non dipendono dal workspace scelto
/// in alto. Un conflitto con le regole (strategia o conto gia' in un piano attivo, piano cambiato dopo
/// il run) arriva come <see cref="InvalidOperationException"/> con le parole del server.
/// </summary>
public sealed class BrokerWorkspacesApiClient : ApiClientBase
{
    private const string Root = "api/v1/broker-workspaces";

    public BrokerWorkspacesApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    public Task<List<BrokerWorkspaceSummary>> ListAsync(CancellationToken cancellationToken = default)
        => SendForAsync<List<BrokerWorkspaceSummary>>(HttpMethod.Get, Root, null, cancellationToken);

    public Task<BrokerWorkspaceDetail> GetAsync(string brokerCode, CancellationToken cancellationToken = default)
        => SendForAsync<BrokerWorkspaceDetail>(HttpMethod.Get, $"{Root}/{Escape(brokerCode)}", null, cancellationToken);

    /// <summary>
    /// Promuove un best plan nel broker workspace del suo broker. Con <c>DryRun</c> il server risponde
    /// con il piano che nascerebbe, senza scriverlo.
    /// </summary>
    public Task<TradingPlan> PromoteAsync(PromoteToProductionRequest request, CancellationToken cancellationToken = default)
        => SendForAsync<TradingPlan>(HttpMethod.Post, $"{Root}/plans", request, cancellationToken);

    /// <summary>Duplica il piano con conti nuovi e ritira l'originale.</summary>
    public Task<TradingPlan> DuplicateAsync(
        string brokerCode, string planCode, DuplicateProductionPlanRequest request, CancellationToken cancellationToken = default)
        => SendForAsync<TradingPlan>(
            HttpMethod.Post, $"{Root}/{Escape(brokerCode)}/plans/{Escape(planCode)}/duplicate", request, cancellationToken);

    public Task<TradingPlan> RetireAsync(string brokerCode, string planCode, CancellationToken cancellationToken = default)
        => SendForAsync<TradingPlan>(
            HttpMethod.Post, $"{Root}/{Escape(brokerCode)}/plans/{Escape(planCode)}/retire", null, cancellationToken);
}
