using System.Text.Json;

namespace piootooapp.clientform.Shell;

/// <summary>
/// Impostazioni della console letta da <c>appsettings.json</c> accanto all'eseguibile: l'URL di
/// default del server e i due percorsi della schermata "Istanze cTrader". L'URL prima veniva
/// ridichiarato come stringa costante in più punti (<see cref="AppServices"/>, la console legacy) e
/// cambiare ambiente richiedeva una ricompilazione.
/// </summary>
internal static class ClientSettings
{
    private const string FallbackServerBaseUrl = "https://localhost:7116";

    private static readonly IReadOnlyDictionary<string, string> Values = Load();

    public static string ServerBaseUrl { get; } =
        Values.TryGetValue("ServerBaseUrl", out var url) ? url.TrimEnd('/') : FallbackServerBaseUrl;

    /// <summary>
    /// Percorso di <c>tools\conti-ctrader.ps1</c>. Null se non impostato: la schermata lo cerca
    /// allora risalendo dalla cartella dell'eseguibile, che basta quando la console gira dal
    /// checkout ma non dall'installazione.
    /// </summary>
    public static string? CtraderInstancesScript { get; } =
        Values.TryGetValue("CtraderInstancesScript", out var script) ? script : null;

    /// <summary>File di configurazione delle istanze; null = il default dello script.</summary>
    public static string? CtraderInstancesConfig { get; } =
        Values.TryGetValue("CtraderInstancesConfig", out var config) ? config : null;

    private static IReadOnlyDictionary<string, string> Load()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return values;

            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(property.Value.GetString()))
                {
                    values[property.Name] = property.Value.GetString()!.Trim();
                }
            }
        }
        catch
        {
            // appsettings.json assente o malformato: si prosegue con i default, la console non
            // deve rifiutarsi di avviarsi per un file di configurazione opzionale.
        }

        return values;
    }
}
