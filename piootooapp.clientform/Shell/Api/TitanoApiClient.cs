using System.Text.Json;
using Piootoo.Shared.Models.Optimization;

namespace piootooapp.clientform.Shell.Api;

/// <summary>
/// Client di <c>api/Titano</c>: setup riutilizzabili, esecuzione di una rotazione e lettura dei
/// run già calcolati. Un run vive dentro la cartella di backtest da cui è stato prodotto, quindi
/// quasi tutte le letture vogliono la coppia (workspace, cartella).
/// </summary>
public sealed class TitanoApiClient : ApiClientBase
{
    public TitanoApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    public Task<List<TitanoSetupInfo>> ListSetupsAsync(CancellationToken cancellationToken = default)
        => SendForAsync<List<TitanoSetupInfo>>(
            HttpMethod.Get, "api/Titano/rotation-setups", null, cancellationToken);

    public Task<TitanoRotationSetup> GetSetupAsync(string setupId, CancellationToken cancellationToken = default)
        => SendForAsync<TitanoRotationSetup>(
            HttpMethod.Get, $"api/Titano/rotation-setups/{Escape(setupId)}", null, cancellationToken);

    public Task<TitanoRotationSetup> SaveSetupAsync(
        TitanoRotationSetup setup,
        CancellationToken cancellationToken = default)
        => SendForAsync<TitanoRotationSetup>(
            HttpMethod.Post, "api/Titano/rotation-setups", setup, cancellationToken);

    /// <summary>I setup predefiniti non sono eliminabili: il server risponde 400 e lo dice.</summary>
    public async Task DeleteSetupAsync(string setupId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete, $"api/Titano/rotation-setups/{Escape(setupId)}", null, cancellationToken);
        _ = response;
    }

    public Task<TitanoRotationManifest> RunRotationAsync(
        TitanoRotationRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<TitanoRotationManifest>(
            HttpMethod.Post, "api/Titano/rotations", request, cancellationToken);

    public Task<List<TitanoRunInfo>> ListRunsAsync(
        string workspaceId,
        string backtestFolder,
        CancellationToken cancellationToken = default)
        => SendForAsync<List<TitanoRunInfo>>(
            HttpMethod.Get,
            $"api/Titano/rotations?workspaceId={Escape(workspaceId)}&backtestFolder={Escape(backtestFolder)}",
            null,
            cancellationToken);

    public Task<TitanoRotationManifest> GetManifestAsync(
        string runId,
        string workspaceId,
        string backtestFolder,
        CancellationToken cancellationToken = default)
        => SendForAsync<TitanoRotationManifest>(
            HttpMethod.Get,
            $"api/Titano/rotations/{Escape(runId)}" +
            $"?workspaceId={Escape(workspaceId)}&backtestFolder={Escape(backtestFolder)}",
            null,
            cancellationToken);

    public Uri GetReportUri(string runId, string workspaceId, string backtestFolder)
        => new(Http.BaseAddress!,
            $"api/Titano/rotations/{Escape(runId)}/report" +
            $"?workspaceId={Escape(workspaceId)}&backtestFolder={Escape(backtestFolder)}");

    public Task<TitanoHardStopReset> ResetHardStopAsync(
        string runId,
        string workspaceId,
        string backtestFolder,
        TitanoHardStopResetRequest request,
        CancellationToken cancellationToken = default)
        => SendForAsync<TitanoHardStopReset>(
            HttpMethod.Post,
            $"api/Titano/rotations/{Escape(runId)}/hard-stop-reset" +
            $"?workspaceId={Escape(workspaceId)}&backtestFolder={Escape(backtestFolder)}",
            request,
            cancellationToken);
}
