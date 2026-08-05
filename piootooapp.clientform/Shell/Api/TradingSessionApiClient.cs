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

    /// <summary>Elenco leggero di tutte le sessioni vive nel processo, incluse quelle aperte da un cBot.</summary>
    public Task<List<TradingSessionSummary>> ListAsync(CancellationToken cancellationToken = default)
        => SendForAsync<List<TradingSessionSummary>>(
            HttpMethod.Get, "api/v1/trading-sessions", null, cancellationToken);

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

    /// <summary>
    /// Barre chiuse. In multi-account la risposta contiene i <b>template</b> non assegnati: sono i
    /// segnali che gli account dovranno reclamare, non ordini da eseguire.
    /// </summary>
    public Task<PushBarsResponse> PushBarsAsync(
        PushBarsRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<PushBarsResponse>(
            HttpMethod.Post,
            $"api/v1/trading-sessions/{Escape(request.SessionId)}/bars",
            request,
            cancellationToken);

    /// <summary>
    /// Poll di un singolo account, con lo stato dichiarato dal broker. Le posizioni e gli ordini
    /// pendenti passati qui sono ciò che il server conta per <c>MaxConcurrentTrades</c>: senza,
    /// ricadrebbe sul proprio conteggio interno.
    /// </summary>
    public Task<AccountSignalResponse> PollSignalAsync(
        string sessionId,
        string sessionToken,
        string accountNumber,
        AccountSignalPollRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<AccountSignalResponse>(
            HttpMethod.Post,
            $"api/v1/trading-sessions/{Escape(sessionId)}/accounts/{Escape(accountNumber)}/signal",
            request,
            cancellationToken,
            sessionToken);

    public Task<TradingSessionSnapshot> ApplyReportAsync(
        string sessionId,
        ExecutionReportRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<TradingSessionSnapshot>(
            HttpMethod.Post,
            $"api/v1/trading-sessions/{Escape(sessionId)}/execution-reports",
            request,
            cancellationToken);

    /// <summary>
    /// Registra la chiusura di una posizione decisa dal client. L'intent restituito va poi
    /// riportato con <see cref="ApplyReportAsync"/>: è quel report a liberare slot di gruppo e
    /// lucchetto account/simbolo.
    /// </summary>
    public Task<OrderIntent> CreateExternalCloseIntentAsync(
        string sessionId,
        CreateExternalCloseIntentRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<OrderIntent>(
            HttpMethod.Post,
            $"api/v1/trading-sessions/{Escape(sessionId)}/intents/close-external",
            request,
            cancellationToken);

    public Task<List<OrderIntent>> GetIntentsAsync(
        string sessionId,
        string sessionToken,
        long after = 0,
        CancellationToken cancellationToken = default)
        => SendForAsync<List<OrderIntent>>(
            HttpMethod.Get,
            $"api/v1/trading-sessions/{Escape(sessionId)}/intents?after={after}",
            null,
            cancellationToken,
            sessionToken);
}
