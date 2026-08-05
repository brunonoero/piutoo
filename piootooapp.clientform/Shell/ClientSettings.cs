using System.Text.Json;

namespace piootooapp.clientform.Shell;

/// <summary>
/// Impostazioni della console letta da <c>appsettings.json</c> accanto all'eseguibile. Oggi
/// contiene solo l'URL di default del server: prima veniva ridichiarato come stringa costante
/// in più punti (<see cref="AppServices"/>, la console legacy) e cambiare ambiente richiedeva
/// una ricompilazione.
/// </summary>
internal static class ClientSettings
{
    private const string FallbackServerBaseUrl = "https://localhost:7116";

    public static string ServerBaseUrl { get; } = LoadServerBaseUrl();

    private static string LoadServerBaseUrl()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return FallbackServerBaseUrl;

            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("ServerBaseUrl", out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString()!.Trim().TrimEnd('/');
            }
        }
        catch
        {
            // appsettings.json assente o malformato: si prosegue con il default, la console non
            // deve rifiutarsi di avviarsi per un file di configurazione opzionale.
        }

        return FallbackServerBaseUrl;
    }
}
