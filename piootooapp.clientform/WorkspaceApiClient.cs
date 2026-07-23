using System.Net.Http.Json;
using System.Text.Json;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Workspaces;

namespace piootooapp.clientform;

/// <summary>Client HTTP per <c>api/Workspace</c>.</summary>
public sealed class WorkspaceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkspaceApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _jsonOptions = jsonOptions;
    }

    public void SetBaseAddress(string serverUrl)
    {
        var url = serverUrl.Trim().TrimEnd('/') + "/";
        if (_httpClient.BaseAddress?.ToString() != url)
        {
            _httpClient.BaseAddress = new Uri(url);
        }
    }

    public async Task<IReadOnlyList<WorkspaceInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<WorkspaceInfo>>("api/Workspace", _jsonOptions, cancellationToken);
        return result ?? new List<WorkspaceInfo>();
    }

    public async Task<IReadOnlyList<StrategyCatalogItem>> ListStrategiesAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/strategies", null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<StrategyCatalogItem>>(_jsonOptions, cancellationToken)
            ?? new List<StrategyCatalogItem>();
    }

    public async Task<string> StartBacktestingAsync(
        BacktestingRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/Backtesting/start", request, cancellationToken);
        var started = await response.Content.ReadFromJsonAsync<BacktestingStartedResponse>(
            _jsonOptions, cancellationToken);
        return !string.IsNullOrWhiteSpace(started?.JobId)
            ? started.JobId
            : throw new InvalidOperationException("Il server non ha restituito l'identificativo del job.");
    }

    public async Task<BacktestingJob> GetBacktestingStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"api/Backtesting/status/{Uri.EscapeDataString(jobId)}",
            null,
            cancellationToken);
        return await response.Content.ReadFromJsonAsync<BacktestingJob>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Risposta stato job vuota.");
    }

    public async Task<BacktestingResult> GetBacktestingResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"api/Backtesting/result/{Uri.EscapeDataString(jobId)}",
            null,
            cancellationToken);
        return await response.Content.ReadFromJsonAsync<BacktestingResult>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Risultato backtesting vuoto.");
    }

    public async Task<BacktestingJob> CancelBacktestingAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/Backtesting/cancel/{Uri.EscapeDataString(jobId)}",
            null,
            cancellationToken);
        return await response.Content.ReadFromJsonAsync<BacktestingJob>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Risposta cancellazione job vuota.");
    }

    public async Task<BacktestingJob> PollBacktestingUntilTerminalAsync(
        string jobId,
        Action<BacktestingJob>? onProgress = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutSource = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value)
            : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        var delay = TimeSpan.FromMilliseconds(500);

        while (true)
        {
            linked.Token.ThrowIfCancellationRequested();
            var job = await GetBacktestingStatusAsync(jobId, linked.Token);
            onProgress?.Invoke(job);
            if (job.Status is BacktestingJobStatus.Completed
                or BacktestingJobStatus.Failed
                or BacktestingJobStatus.Cancelled)
                return job;

            await Task.Delay(delay, linked.Token);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.25, 2000));
        }
    }

    public Uri GetBacktestingReportUri(string jobId)
        => new(_httpClient.BaseAddress
            ?? throw new InvalidOperationException("URL server non configurato."),
            $"api/Backtesting/output/{Uri.EscapeDataString(jobId)}/report");

    public async Task<WorkspaceInfo> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Workspace", request, _jsonOptions, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<WorkspaceInfo>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Risposta di creazione workspace vuota.");
    }

    public async Task<WorkspaceMasterFilter> GetMasterFilterAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(workspaceId);
        var result = await _httpClient.GetFromJsonAsync<WorkspaceMasterFilter>(
            $"api/Workspace/{encoded}/masterfilter",
            _jsonOptions,
            cancellationToken);
        return result ?? new WorkspaceMasterFilter { Name = workspaceId };
    }

    public async Task<WorkspaceMasterFilter> SaveMasterFilterAsync(
        string workspaceId,
        WorkspaceMasterFilter filter,
        CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(workspaceId);
        var response = await _httpClient.PutAsJsonAsync(
            $"api/Workspace/{encoded}/masterfilter",
            filter,
            _jsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<WorkspaceMasterFilter>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Risposta di salvataggio masterfilter vuota.");
    }

    public async Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(workspaceId);
        var response = await _httpClient.DeleteAsync($"api/Workspace/{encoded}", cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<IReadOnlyList<WorkspaceBacktestInfo>> ListBacktestsAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(workspaceId);
        var result = await _httpClient.GetFromJsonAsync<List<WorkspaceBacktestInfo>>(
            $"api/Workspace/{encoded}/backtests",
            _jsonOptions,
            cancellationToken);
        return result ?? new List<WorkspaceBacktestInfo>();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        var message = TryExtractError(body) ?? $"{(int)response.StatusCode} {response.ReasonPhrase}";
        throw new InvalidOperationException(message);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, uri);
            if (body != null)
                request.Content = JsonContent.Create(body, options: _jsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response);
            return response;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Server Piootoo non raggiungibile. Verifica che sia avviato e che l'URL API sia corretto.",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("La richiesta al server è scaduta.", ex);
        }
    }

    private static string? TryExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString();
            }

            if (document.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString();
            }
        }
        catch (JsonException)
        {
            // body non JSON: restituisci raw sotto
        }

        return body.Length > 400 ? body[..400] : body;
    }

    private sealed class BacktestingStartedResponse
    {
        public string JobId { get; set; } = string.Empty;
    }
}
