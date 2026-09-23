using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Cosa c'e' e cosa manca, simbolo per simbolo, per lavorare su un broker: barre, spread, swap,
/// scheda, contratto. Vedi <see cref="BrokerDataStatusService"/>.
/// </summary>
[ApiController]
[Route("api/data-status")]
public class DataStatusController : ControllerBase
{
    private readonly BrokerDataStatusService _status;

    public DataStatusController(BrokerDataStatusService status) => _status = status;

    [HttpGet]
    public ActionResult<BrokerDataStatus> Get([FromQuery] string broker)
    {
        try
        {
            return Ok(_status.Get(broker));
        }
        catch (ArgumentException error)
        {
            return Problem(title: "Broker non valido", detail: error.Message, statusCode: 400);
        }
    }
}
