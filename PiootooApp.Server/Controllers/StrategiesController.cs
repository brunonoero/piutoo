using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Strategies;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/strategies")]
public sealed class StrategiesController : ControllerBase
{
    private readonly StrategyExportService _export;

    public StrategiesController(StrategyExportService export)
    {
        _export = export;
    }

    /// <summary>
    /// L'export è l'unica risposta di questo server che viene <b>salvata come file e letta da una
    /// persona</b>, quindi la si serializza qui invece di lasciarla al pipeline MVC: serve
    /// l'indentazione, e i caratteri non ASCII devono restare tali — i commenti di conversione sono
    /// in italiano e con <c>è</c> ovunque il file diventa illeggibile.
    /// </summary>
    private static readonly JsonSerializerOptions ExportJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpGet]
    public ActionResult<IReadOnlyList<StrategyCatalogItem>> List()
        => Ok(StrategyFactory.GetRegisteredStrategies()
            .Select(strategy => new StrategyCatalogItem
            {
                Id = strategy.Id,
                Code = strategy.Id,
                Name = strategy.Name,
                Symbol = strategy.Symbol,
                TimeframeMinutes = strategy.TimeframeMinutes,
                BarType = strategy.BarType,
                Description = strategy.Description,
                Type = strategy.Type.ToString(),
                IsActive = strategy.IsActive,
                SourceFileName = strategy.FileName,
                Overnight = strategy.Holding.Overnight,
                Overweek = strategy.Holding.Overweek,
                HoldingLabel = strategy.Holding.Describe()
            })
            .OrderBy(strategy => strategy.Symbol)
            .ThenBy(strategy => strategy.Name)
            .ToList());

    /// <summary>
    /// Scheda completa di una strategia: parametri della traduzione, commenti di conversione,
    /// sorgente C# e motore Python di provenienza. Vedi <see cref="StrategyExportService"/> per cosa
    /// entra e da dove viene.
    ///
    /// <para><c>id</c> accetta sia l'Id di classe sia il codice di esecuzione, come
    /// <c>StrategyFactory.CreateStrategy</c>.</para>
    /// </summary>
    [HttpGet("{id}/export")]
    [Produces("application/json")]
    public ActionResult Export(string id)
    {
        try
        {
            var export = _export.Build(id);
            return Content(JsonSerializer.Serialize(export, ExportJson), "application/json");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Strategia inesistente", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Le schede di più strategie in un <b>array JSON</b>, nell'ordine in cui sono state chieste.
    /// È la forma che usa la console per esportare la griglia intera, filtro compreso.
    ///
    /// <para>POST e non GET con la lista in query: un export completo sono 124 identificativi, e
    /// una query string di quella lunghezza è il tipo di limite che si scopre in produzione.</para>
    ///
    /// <para><b>Un id sconosciuto fa fallire tutta la richiesta.</b> Saltarlo restituirebbe un array
    /// più corto di quanto chiesto, e chi lo salva non ha modo di accorgersene: è la stessa regola
    /// del datafeed mancante, meglio un errore esplicito di un artefatto incompleto.</para>
    /// </summary>
    [HttpPost("export")]
    [Produces("application/json")]
    public ActionResult ExportMany([FromBody] string[] ids)
    {
        if (ids is null || ids.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Nessuna strategia da esportare",
                Detail = "La richiesta non contiene identificativi."
            });
        }

        try
        {
            var exports = ids.Select(id => _export.Build(id)).ToList();
            return Content(JsonSerializer.Serialize(exports, ExportJson), "application/json");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Strategia inesistente", Detail = ex.Message });
        }
    }
}
