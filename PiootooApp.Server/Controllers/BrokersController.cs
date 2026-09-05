using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Workspaces;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Anagrafica dei broker, globale come quella dei conti: chi quota gli strumenti su cui i conti
/// operano. Da qui vengono la tabella dei simboli e la cartella del datafeed raccolto.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class BrokersController(WorkspaceService workspaceService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<TradingBroker>> List()
        => Ok(workspaceService.ListBrokers());

    [HttpGet("{code}")]
    public ActionResult<TradingBroker> Get(string code)
    {
        try { return Ok(workspaceService.GetBroker(code)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost]
    public ActionResult<TradingBroker> Create([FromBody] TradingBroker broker)
    {
        try
        {
            var created = workspaceService.CreateBroker(broker);
            return CreatedAtAction(nameof(Get), new { code = created.Code }, created);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{code}")]
    public ActionResult<TradingBroker> Save(string code, [FromBody] TradingBroker broker)
    {
        try { return Ok(workspaceService.SaveBroker(code, broker)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpDelete("{code}")]
    public IActionResult Delete(string code)
    {
        try
        {
            workspaceService.DeleteBroker(code);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
