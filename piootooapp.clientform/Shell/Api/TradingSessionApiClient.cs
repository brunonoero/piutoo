using System.Text.Json;
using Piootoo.Shared.Models.Trading;

namespace piootooapp.clientform.Shell.Api;

/// <summary>
/// Client di <c>api/v1/trading-sessions</c>. Il token arriva alla creazione e va poi ripresentato:
/// nell'header <c>X-Session-Token</c> quasi ovunque, ma nel corpo per il PUT dei gruppi — è
/// un'asimmetria del contratto server, non una svista.
/// </summary>
public sealed class TradingSessionApiClient : ApiClientBase
{
    public TradingSessionApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    public Task<TradingSessionDescriptor> CreateAsync(
        CreateTradingSessionRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<TradingSessionDescriptor>(
            HttpMethod.Post, "api/v1/trading-sessions", request, cancellationToken);

    public Task<TradingSessionDescriptor> OpenFromPlanAsync(
        OpenTradingPlanSessionRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<TradingSessionDescriptor>(
            HttpMethod.Post, "api/v1/trading-sessions/open-plan", request, cancellationToken);

    /// <param name="action">start, stop oppure resume.</param>
    public Task<TradingSessionDescriptor> SetStatusAsync(
        string sessionId,
        string sessionToken,
        string action,
        CancellationToken cancellationToken = default)
        => SendForAsync<TradingSessionDescriptor>(
            HttpMethod.Post,
            $"api/v1/trading-sessions/{Escape(sessionId)}/{action}",
            null,
            cancellationToken,
            sessionToken);

    public Task<TradingSessionSnapshot> GetSnapshotAsync(
        string sessionId,
        string sessionToken,
        CancellationToken cancellationToken = default)
        => SendForAsync<TradingSessionSnapshot>(
            HttpMethod.Get,
            $"api/v1/trading-sessions/{Escape(sessionId)}/snapshot",
            null,
            cancellationToken,
            sessionToken);

    public Task<List<TradingGroupRow>> GetGroupsAsync(
        string sessionId,
        string sessionToken,
        CancellationToken cancellationToken = default)
        => SendForAsync<List<TradingGroupRow>>(
            HttpMethod.Get,
            $"api/v1/trading-sessions/{Escape(sessionId)}/groups",
            null,
            cancellationToken,
            sessionToken);

    public Task<TradingSessionSnapshot> SetGroupsAsync(
        string sessionId,
        string sessionToken,
        IReadOnlyList<TradingGroupRow> rows,
        CancellationToken cancellationToken = default)
        => SendForAsync<TradingSessionSnapshot>(
            HttpMethod.Put,
            $"api/v1/trading-sessions/{Escape(sessionId)}/groups",
            new SetTradingGroupsRequest { SessionToken = sessionToken, Rows = rows },
            cancellationToken);
}
