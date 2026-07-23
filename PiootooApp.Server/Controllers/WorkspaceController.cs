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
