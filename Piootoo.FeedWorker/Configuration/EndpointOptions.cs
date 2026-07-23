namespace FeedWorker.Configuration;

/// <summary>
/// Opzioni di configurazione per un endpoint
/// </summary>
public class EndpointOptions
{
    /// <summary>
    /// Codice identificativo dell'endpoint
    /// </summary>
    public string EndpointCode { get; set; } = string.Empty;

    /// <summary>
    /// URL base dell'API
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Chiave API per l'autenticazione
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
