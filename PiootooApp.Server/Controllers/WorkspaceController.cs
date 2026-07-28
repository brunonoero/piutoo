using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Workspaces;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkspaceController(WorkspaceService workspaceService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<WorkspaceInfo>> List() => Ok(workspaceService.List());

    [HttpPost]
    public ActionResult<WorkspaceInfo> Create([FromBody] CreateWorkspaceRequest request)
    {
        try
        {
            var workspace = workspaceService.Create(request);
            return CreatedAtAction(nameof(GetMasterFilter), new { workspaceId = workspace.Id }, workspace);
        }
        catch (Exception exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{workspaceId}/masterfilter")]
    public ActionResult<WorkspaceMasterFilter> GetMasterFilter(string workspaceId)
    {
        try { return Ok(workspaceService.GetMasterFilter(workspaceId)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
    }

    [HttpPut("{workspaceId}/masterfilter")]
    public ActionResult<WorkspaceMasterFilter> SaveMasterFilter(string workspaceId, [FromBody] WorkspaceMasterFilter filter)
    {
        try { return Ok(workspaceService.SaveMasterFilter(workspaceId, filter)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
    }

    /// <summary>Preset condiviso della tabella di conversione (settings/default-symbol-conversion.json).</summary>
    [HttpGet("accounts/symbol-preset")]
    public ActionResult<IReadOnlyList<AccountSymbolMapping>> GetSymbolPreset()
    {
        try { return Ok(workspaceService.GetSymbolConversionPreset()); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>Tabella identità dal catalogo strategie: ogni symbol su se stesso, moltiplicatore 1.</summary>
    [HttpGet("accounts/symbol-identity")]
    public ActionResult<IReadOnlyList<AccountSymbolMapping>> GetSymbolIdentity()
        => Ok(workspaceService.GetIdentitySymbolMappings());

    [HttpPut("accounts/symbol-preset")]
    public ActionResult<IReadOnlyList<AccountSymbolMapping>> SaveSymbolPreset(
        [FromBody] List<AccountSymbolMapping> mappings)
    {
        try { return Ok(workspaceService.SaveSymbolConversionPreset(mappings)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>Crea (o restituisce) l'account di default 1 a 1 con balance iniziale di un milione.</summary>
    [HttpPost("{workspaceId}/accounts/default")]
    public ActionResult<WorkspaceAccount> EnsureDefaultAccount(string workspaceId)
    {
        try { return Ok(workspaceService.EnsureDefaultAccount(workspaceId)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{workspaceId}/accounts")]
    public ActionResult<IReadOnlyList<WorkspaceAccount>> ListAccounts(string workspaceId)
    {
        try { return Ok(workspaceService.ListAccounts(workspaceId)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{workspaceId}/accounts/{accountId}")]
    public ActionResult<WorkspaceAccount> GetAccount(string workspaceId, string accountId)
    {
        try { return Ok(workspaceService.GetAccount(workspaceId, accountId)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("{workspaceId}/accounts")]
    public ActionResult<WorkspaceAccount> CreateAccount(string workspaceId, [FromBody] WorkspaceAccount account)
    {
        try
        {
            var created = workspaceService.CreateAccount(workspaceId, account);
            return CreatedAtAction(nameof(GetAccount), new { workspaceId, accountId = created.Id }, created);
        }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{workspaceId}/accounts/{accountId}")]
    public ActionResult<WorkspaceAccount> SaveAccount(string workspaceId, string accountId, [FromBody] WorkspaceAccount account)
    {
        try { return Ok(workspaceService.SaveAccount(workspaceId, accountId, account)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpDelete("{workspaceId}/accounts/{accountId}")]
    public IActionResult DeleteAccount(string workspaceId, string accountId)
    {
        try { workspaceService.DeleteAccount(workspaceId, accountId); return NoContent(); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{workspaceId}/backtests")]
    public ActionResult<IReadOnlyList<WorkspaceBacktestInfo>> ListBacktests(string workspaceId)
    {
        try { return Ok(workspaceService.ListBacktests(workspaceId)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpDelete("{workspaceId}")]
    public IActionResult Delete(string workspaceId)
    {
        try { workspaceService.Delete(workspaceId); return NoContent(); }
        catch (DirectoryNotFoundException) { return NotFound(); }
    }
}
