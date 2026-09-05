using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Piootoo.Core.Services;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'endpoint di export visto dalla console: <c>api/strategies/export</c>, che restituisce un array
/// JSON con una scheda per strategia chiesta.
///
/// <para>Il <c>BasePath</c> punta al repository dati vero e non a una cartella temporanea: dossier e
/// motori Python sono allegati reali, e un test che li togliesse verificherebbe soltanto che
/// l'endpoint risponde.</para>
/// </summary>
public sealed class StrategyExportHttpTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StrategyExportHttpTests()
    {
        var repository = Path.Combine(FindRepositoryRoot(), "piootoo-repository");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Piootoo:BasePath"] = repository }));
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Export_DiPiuStrategie_RestituisceUnArrayNellOrdineChiesto()
    {
        var ids = new[] { "PTS_NQ_TFM_002_15", "PTS_ES_PCH_001_60", "PTS_FDAX_MAC_001_240" };

        using var response = await _client.PostAsJsonAsync("api/strategies/export", ids);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(
            ids,
            document.RootElement.EnumerateArray()
                .Select(element => element.GetProperty("identity").GetProperty("id").GetString())
                .ToArray());

        // Ogni voce dell'array e' la scheda intera, non un riassunto: e' il contenuto per cui
        // l'export esiste, e un array di sole intestazioni sarebbe inutile e non lo si vedrebbe.
        var prima = document.RootElement[0];
        Assert.NotEmpty(prima.GetProperty("parameters").EnumerateObject());
        Assert.Contains(
            prima.GetProperty("sources").EnumerateArray(),
            source => source.GetProperty("role").GetString() == "engine-python");
    }

    /// <summary>
    /// L'export della griglia intera: e' il caso normale del pulsante, ed e' anche quello che rilegge
    /// il dossier una volta per strategia se la cache non funziona.
    /// </summary>
    [Fact]
    public async Task Export_DiTuttoIlCatalogo_RestituisceUnaSchedaPerStrategia()
    {
        var ids = StrategyFactory.GetRegisteredStrategies().Select(strategy => strategy.Id).ToArray();

        using var response = await _client.PostAsJsonAsync("api/strategies/export", ids);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ids.Length, document.RootElement.GetArrayLength());
    }

    /// <summary>
    /// Un id sconosciuto fa fallire tutta la richiesta. Saltarlo darebbe un array piu' corto di
    /// quanto chiesto, e chi lo salva non avrebbe modo di accorgersene.
    /// </summary>
    [Fact]
    public async Task Export_ConUnIdSconosciuto_Fallisce()
    {
        using var response = await _client.PostAsJsonAsync(
            "api/strategies/export", new[] { "PTS_ES_PCH_001_60", "PTS_NON_ESISTE_000_1" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_SenzaIdentificativi_Fallisce()
    {
        using var response = await _client.PostAsJsonAsync("api/strategies/export", Array.Empty<string>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_DiUnaSolaStrategia_RestaRaggiungibileSullaRisorsa()
    {
        using var response = await _client.GetAsync("api/strategies/PTS_ES_PCH_001_60/export");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.Equal(
            "PTS_ES_PCH_001_60",
            document.RootElement.GetProperty("identity").GetProperty("id").GetString());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PiootooApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                $"PiootooApp.sln non trovata risalendo da {AppContext.BaseDirectory}.");
    }
}
