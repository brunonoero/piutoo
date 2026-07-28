using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Workspaces;

namespace PiootooApp.Server.Controllers;

/// <summary>Registro account globale, condiviso da tutti i workspace.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AccountsController(WorkspaceService workspaceService) : ControllerBase
{
    [HttpGet("groups")]
    public ActionResult<IReadOnlyList<string>> ListGroups()
        => Ok(workspaceService.ListAccountGroups());

    [HttpPost("groups")]
    public ActionResult<IReadOnlyList<string>> AddGroup([FromBody] CreateAccountGroupRequest request)
    {
        try { return Ok(workspaceService.AddAccountGroup(request.GroupId)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpDelete("groups/{groupId}")]
    public ActionResult<IReadOnlyList<string>> RemoveGroup(string groupId)
    {
        try { return Ok(workspaceService.RemoveAccountGroup(groupId)); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<WorkspaceAccount>> List()
        => Ok(workspaceService.ListAccounts());

    [HttpGet("{accountId}")]
    public ActionResult<WorkspaceAccount> Get(string accountId)
    {
        try { return Ok(workspaceService.GetAccount(accountId)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost]
    public ActionResult<WorkspaceAccount> Create([FromBody] WorkspaceAccount account)
    {
        try
        {
            var created = workspaceService.CreateAccount(account);
            return CreatedAtAction(nameof(Get), new { accountId = created.Id }, created);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("default")]
    public ActionResult<WorkspaceAccount> EnsureDefault()
    {
        try { return Ok(workspaceService.EnsureDefaultAccount()); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{accountId}")]
    public ActionResult<WorkspaceAccount> Save(string accountId, [FromBody] WorkspaceAccount account)
    {
        try { return Ok(workspaceService.SaveAccount(accountId, account)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpDelete("{accountId}")]
    public IActionResult Delete(string accountId)
    {
        try
        {
            workspaceService.DeleteAccount(accountId);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }
}
