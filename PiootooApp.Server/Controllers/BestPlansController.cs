using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Core.Services.BestPlans;
using Piootoo.Shared.Models.BestPlans;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// I best plan: backtest messi in evidenza, trasversali ai workspace. Vedi <see cref="BestPlanService"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class BestPlansController(BestPlanService bestPlans) : ControllerBase
{
    /// <summary>Tutti i best plan, dal piu' recente, con la curva ridotta a miniatura.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<BestPlan>> List() => Ok(bestPlans.List());

    [HttpGet("{id}")]
    public ActionResult<BestPlan> Get(string id)
    {
        try { return Ok(bestPlans.Get(id)); }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>
    /// Promuove un backtest a best plan copiandone le cifre e gli artefatti. <c>409</c> se e' gia'
    /// promosso e la richiesta non chiede di sovrascrivere, o se non ha niente da fotografare.
    /// </summary>
    [HttpPost]
    public ActionResult<BestPlan> Promote([FromBody] PromoteBestPlanRequest request)
    {
        try
        {
            var plan = bestPlans.Promote(request);
            return CreatedAtAction(nameof(Get), new { id = plan.Id }, plan);
        }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>Toglie il best plan dall'elenco, con la sua copia degli artefatti. Il backtest non si tocca.</summary>
    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        try { bestPlans.Delete(id); return NoContent(); }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>Il report HTML copiato alla promozione; <c>404</c> se il run non ne aveva uno.</summary>
    [HttpGet("{id}/report")]
    public IActionResult GetReport(string id)
    {
        try { return File(AtomicFileWriter.OpenReadShared(bestPlans.GetHtmlReportPath(id)), "text/html; charset=utf-8"); }
        catch (FileNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
