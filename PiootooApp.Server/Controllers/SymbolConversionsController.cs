using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Workspaces;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Registro globale delle tabelle di conversione simboli, fuori da workspace e account: un account
/// ne referenzia una per codice (<see cref="WorkspaceAccount.SymbolConversionCode"/>).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SymbolConversionsController(WorkspaceService workspaceService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<SymbolConversion>> List()
        => Ok(workspaceService.ListSymbolConversions());

    /// <summary>Tabella identità dal catalogo strategie: ogni symbol su se stesso, moltiplicatore 1.</summary>
    [HttpGet("identity")]
    public ActionResult<IReadOnlyList<AccountSymbolMapping>> GetIdentity()
        => Ok(workspaceService.GetIdentitySymbolMappings());

    [HttpGet("{code}")]
    public ActionResult<SymbolConversion> Get(string code)
    {
        try { return Ok(workspaceService.GetSymbolConversion(code)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    public ActionResult<SymbolConversion> Create([FromBody] SymbolConversion conversion)
    {
        try
        {
            var created = workspaceService.CreateSymbolConversion(conversion);
            return CreatedAtAction(nameof(Get), new { code = created.Code }, created);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{code}")]
    public ActionResult<SymbolConversion> Save(string code, [FromBody] SymbolConversion conversion)
    {
        try { return Ok(workspaceService.SaveSymbolConversion(code, conversion)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpDelete("{code}")]
    public IActionResult Delete(string code)
    {
        try
        {
            workspaceService.DeleteSymbolConversion(code);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
