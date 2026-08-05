using System.Text.Json;
using System.Text.Json.Serialization;
using piootooapp.clientform.Shell.Api;

namespace piootooapp.clientform.Shell;

/// <summary>
/// Dipendenze condivise dalle schermate della console. Istanziato una sola volta in
/// <see cref="Program"/> e passato ai controlli con <see cref="IShellScreen.Initialize"/>:
/// le schermate hanno un costruttore senza parametri perché il designer WinForms deve
/// poterle istanziare da solo.
/// </summary>
public sealed class AppServices : IDisposable
{
    public static string DefaultServerUrl => ClientSettings.ServerBaseUrl;

    private readonly HttpClient _httpClient = new();

    public AppServices()
    {
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        Api = new WorkspaceApiClient(_httpClient, JsonOptions);
        Plans = new TradingPlanApiClient(_httpClient, JsonOptions);
        Titano = new TitanoApiClient(_httpClient, JsonOptions);
        Sessions = new TradingSessionApiClient(_httpClient, JsonOptions);
        Datafeed = new DatafeedApiClient(_httpClient, JsonOptions);
        SetServerUrl(DefaultServerUrl);
    }

    public JsonSerializerOptions JsonOptions { get; }

    public WorkspaceApiClient Api { get; }

    public TradingPlanApiClient Plans { get; }

    public TitanoApiClient Titano { get; }

    public TradingSessionApiClient Sessions { get; }

    public DatafeedApiClient Datafeed { get; }

    public HttpClient Http => _httpClient;

    public string ServerUrl { get; private set; } = DefaultServerUrl;

    public void SetServerUrl(string serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new InvalidOperationException("L'URL del server è obbligatorio.");
        }

        ServerUrl = serverUrl.Trim().TrimEnd('/');
        Api.SetBaseAddress(ServerUrl);
    }

    public void Dispose() => _httpClient.Dispose();
}
