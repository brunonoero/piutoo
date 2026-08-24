using System.Text.Json;
using Piootoo.Shared.Models.Diagnostics;

namespace piootooapp.clientform.Shell.Api;

/// <summary>
/// Identità del server: <c>GET /api/v1/version</c>. Senza token, perché serve prima di qualunque
/// sessione — è la domanda "con quale server sto parlando", non "cosa sta facendo".
/// </summary>
public sealed class ServerInfoApiClient : ApiClientBase
{
    public ServerInfoApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        : base(httpClient, jsonOptions)
    {
    }

    public Task<ServerVersionInfo> GetVersionAsync(CancellationToken cancellationToken = default)
        => SendForAsync<ServerVersionInfo>(HttpMethod.Get, "api/v1/version", null, cancellationToken);
}
