using System.Net.Http.Json;
using System.Text.Json;

namespace piootooapp.clientform.Shell.Api;

/// <summary>
/// Invio HTTP con traduzione degli errori in messaggi leggibili: il server risponde
/// <c>ProblemDetails</c>, e un "500 Internal Server Error" nudo non aiuta nessuno.
/// </summary>
public abstract class ApiClientBase
{
    protected ApiClientBase(HttpClient httpClient, JsonSerializerOptions jsonOptions)
    {
        Http = httpClient;
        JsonOptions = jsonOptions;
    }

    protected HttpClient Http { get; }

    protected JsonSerializerOptions JsonOptions { get; }

    protected async Task<T> SendForAsync<T>(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken,
        string? sessionToken = null)
    {
        using var response = await SendAsync(method, uri, body, cancellationToken, sessionToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Risposta vuota da {uri}.");
    }

    protected async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken,
        string? sessionToken = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, uri);
            if (sessionToken != null)
            {
                request.Headers.Add("X-Session-Token", sessionToken);
            }

            if (body != null)
            {
                request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
            }

            var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    ExtractError(payload) ?? $"{(int)response.StatusCode} {response.ReasonPhrase}");
            }

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

    protected static string Escape(string value) => Uri.EscapeDataString(value);

    private static string? ExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("detail", out var detail)
                && detail.GetString() is { Length: > 0 } detailText)
            {
                return detailText;
            }

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
            // corpo non JSON: sotto viene restituito grezzo
        }

        return body.Length > 400 ? body[..400] : body;
    }
}
