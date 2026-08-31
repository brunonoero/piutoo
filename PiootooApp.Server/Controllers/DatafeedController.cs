using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Models;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Cosa c'e' nel repository di barre. La console non apre le cartelle del repository: chiede qui
/// quali archivi esistono, come per qualsiasi altro dato.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DatafeedController : ControllerBase
{
    private readonly IDatafeedCatalog _catalog;

    public DatafeedController(IDatafeedCatalog catalog) => _catalog = catalog;

    /// <summary>
    /// Broker con un archivio esterno sotto <c>datafeed-external</c>. Il datafeed interno non
    /// compare: e' l'assenza di broker, non una voce dell'elenco.
    /// </summary>
    [HttpGet("brokers")]
    public ActionResult<IReadOnlyList<DatafeedBrokerInfo>> GetBrokers()
        => Ok(_catalog.GetBrokers());

    /// <summary>
    /// Feed disponibili con il periodo che coprono. Senza <paramref name="broker"/> risponde con
    /// tutti gli archivi, interno compreso; con un broker solo il suo. Il periodo viene dalla prima
    /// e dall'ultima barra dei file, non dalla data di modifica.
    /// </summary>
    [HttpGet("feeds")]
    public ActionResult<IReadOnlyList<DatafeedFeedInfo>> GetFeeds([FromQuery] string? broker)
    {
        try
        {
            return Ok(broker == null ? _catalog.GetAllFeeds() : _catalog.GetFeeds(broker));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DirectoryNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }
}
