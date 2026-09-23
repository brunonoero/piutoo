using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Cosa c'e' in <c>piootoo-repository/spread/</c>. Come <see cref="DatafeedController"/>: la
/// console non apre le cartelle del repository, chiede qui.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SpreadController : ControllerBase
{
    private readonly ISpreadCatalog _catalog;
    private readonly SpreadMeasurementStore _measurements;
    private readonly ILogger<SpreadController> _logger;

    public SpreadController(
        ISpreadCatalog catalog, SpreadMeasurementStore measurements, ILogger<SpreadController> logger)
    {
        _catalog = catalog;
        _measurements = measurements;
        _logger = logger;
    }

    /// <summary>
    /// Una misura di <c>PiootooSpreadDumpBot</c>: il server la scrive in <c>spread/{BROKER}/</c>, con
    /// la cartella ricavata dal conto, e la unisce alla misura che c'era. Vedi
    /// <see cref="SpreadMeasurementStore"/>.
    /// </summary>
    [HttpPost("measurements")]
    public ActionResult<SpreadMeasurementResponse> PostMeasurement([FromBody] SpreadMeasurementRequest request)
    {
        try
        {
            var response = _measurements.Ingest(request);
            _logger.LogInformation(
                "[spread] {Broker}: misura di {Measured} simboli scritta in {File}, {Kept} simboli della misura precedente tenuti.",
                response.Broker, response.Measured.Count, response.SymbolFile, response.Kept.Count);
            return Ok(response);
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Misura non valida", detail: error.Message, statusCode: 400);
        }
        catch (InvalidOperationException error)
        {
            return Problem(title: "Conto non utilizzabile", detail: error.Message, statusCode: 400);
        }
        catch (InvalidDataException error)
        {
            return Problem(title: "Misura non unibile", detail: error.Message, statusCode: 409);
        }
    }

    /// <summary>
    /// Broker con una misura. Elenco vuoto = nessuna misura raccolta, che non e' un errore: un run
    /// senza spread e' un run valido, e la schermata lo dice invece di sembrare rotta.
    /// </summary>
    [HttpGet("brokers")]
    public ActionResult<IReadOnlyList<SpreadBrokerInfo>> GetBrokers()
        => Ok(_catalog.GetBrokers());

    /// <summary>
    /// Gli spread che un run applicherebbe con questa combinazione. Gli errori sono gli stessi che
    /// il backtest darebbe all'avvio, e arrivano qui prima — che e' il punto: scoprire dopo un run
    /// intero che la misura non c'era costa il run.
    /// </summary>
    [HttpGet("table")]
    public ActionResult<SpreadTableInfo> GetTable(
        [FromQuery] string broker,
        [FromQuery] SpreadStatistic statistic = SpreadStatistic.Median,
        [FromQuery] SpreadResolution resolution = SpreadResolution.PerSymbol)
    {
        try
        {
            return Ok(_catalog.GetTable(broker, statistic, resolution));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DirectoryNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (FileNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
