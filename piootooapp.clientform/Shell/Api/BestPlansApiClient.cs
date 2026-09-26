using System.Text.Json;
using Piootoo.Shared.Models.BestPlans;

namespace piootooapp.clientform.Shell.Api;

/// <summary>I best plan, trasversali ai workspace: non dipendono dal workspace scelto in alto.</summary>
public sealed class BestPlansApiClient : ApiClientBase
{
    public BestPlansApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    /// <summary>Elenco con la curva gia' ridotta a miniatura.</summary>
    public Task<List<BestPlan>> ListAsync(CancellationToken cancellationToken = default)
        => SendForAsync<List<BestPlan>>(HttpMethod.Get, "api/BestPlans", null, cancellationToken);

    public Task<BestPlan> GetAsync(string id, CancellationToken cancellationToken = default)
        => SendForAsync<BestPlan>(HttpMethod.Get, $"api/BestPlans/{Escape(id)}", null, cancellationToken);

    /// <summary>
    /// Promuove un backtest. Se e' gia' fra i best plan e <paramref name="overwrite"/> e' falso il
    /// server risponde <c>409</c>, che arriva qui come <see cref="InvalidOperationException"/> con le
    /// sue parole.
    /// </summary>
    public Task<BestPlan> PromoteAsync(
        string workspaceId,
        string backtestFolder,
        bool overwrite,
        CancellationToken cancellationToken = default)
        => SendForAsync<BestPlan>(
            HttpMethod.Post,
            "api/BestPlans",
            new PromoteBestPlanRequest { WorkspaceId = workspaceId, BacktestFolder = backtestFolder, Overwrite = overwrite },
            cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var _ = await SendAsync(HttpMethod.Delete, $"api/BestPlans/{Escape(id)}", null, cancellationToken);
    }

    public Uri GetReportUri(string id)
        => new(Http.BaseAddress!, $"api/BestPlans/{Escape(id)}/report");
}
