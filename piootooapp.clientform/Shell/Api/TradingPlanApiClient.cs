using System.Text.Json;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Api;

/// <summary>Client di <c>api/v1/workspaces/{id}/trading-plans</c>.</summary>
public sealed class TradingPlanApiClient : ApiClientBase
{
    public TradingPlanApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    public Task<List<TradingPlan>> ListAsync(string workspaceId, CancellationToken cancellationToken = default)
        => SendForAsync<List<TradingPlan>>(
            HttpMethod.Get, $"api/v1/workspaces/{Escape(workspaceId)}/trading-plans", null, cancellationToken);

    public Task<TradingPlan> GetAsync(string workspaceId, string code, CancellationToken cancellationToken = default)
        => SendForAsync<TradingPlan>(
            HttpMethod.Get,
            $"api/v1/workspaces/{Escape(workspaceId)}/trading-plans/{Escape(code)}",
            null,
            cancellationToken);

    public Task<TradingPlan> SaveAsync(
        string workspaceId,
        SaveTradingPlanRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<TradingPlan>(
            HttpMethod.Put,
            $"api/v1/workspaces/{Escape(workspaceId)}/trading-plans/{Escape(request.Code)}",
            request,
            cancellationToken);

    public async Task DeleteAsync(string workspaceId, string code, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"api/v1/workspaces/{Escape(workspaceId)}/trading-plans/{Escape(code)}",
            null,
            cancellationToken);
        _ = response;
    }
}
