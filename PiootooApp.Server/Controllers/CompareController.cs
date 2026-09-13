using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services.Compare;
using Piootoo.Shared.Models.Workspaces;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Confronti fra un run del cBot e un backtest interno, avviati dalla console. La cartella
/// <c>compare-NNNN</c> nasce con la richiesta; l'analisi gira dopo e si segue con <c>status</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CompareController(CompareService compare) : ControllerBase
{
    /// <summary>
    /// Crea la cartella di confronto successiva, ci copia gli artefatti dei due run e avvia lo
    /// strumento. <c>409</c> quando la coppia non e' confrontabile: stessi tipi di run, broker
    /// diversi, run senza marcatore o interno senza summary.
    /// </summary>
    [HttpPost("start")]
    public ActionResult<CompareJob> Start([FromBody] StartCompareRequest request)
    {
        try { return Ok(compare.Start(request)); }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (FileNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("status/{jobId}")]
    public ActionResult<CompareJob> GetStatus(string jobId)
        => compare.GetStatus(jobId) is { } job
            ? Ok(job)
            : NotFound(new { error = $"Confronto '{jobId}' non trovato." });

    /// <summary>Il <c>report.md</c> dello strumento, come testo.</summary>
    [HttpGet("{folderName}/report")]
    public IActionResult GetReport(string folderName)
    {
        try { return Content(compare.GetReport(folderName), "text/markdown; charset=utf-8"); }
        catch (FileNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
