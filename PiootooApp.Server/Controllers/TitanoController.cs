using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Models.Optimization;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TitanoController : ControllerBase
{
    private readonly IPiootooBacktestingService _backtestingService;
    private readonly TitanoFilterService _titanoFilterService;
    private readonly TitanoSetupService _titanoSetupService;
    private readonly TitanoRotationSetupService _rotationSetupService;
    private readonly TitanoRotationService _rotationService;

    public TitanoController(
        IPiootooBacktestingService backtestingService,
        TitanoFilterService titanoFilterService,
        TitanoSetupService titanoSetupService,
        TitanoRotationSetupService rotationSetupService,
        TitanoRotationService rotationService)
    {
        _backtestingService = backtestingService;
        _titanoFilterService = titanoFilterService;
        _titanoSetupService = titanoSetupService;
        _rotationSetupService = rotationSetupService;
        _rotationService = rotationService;
    }

    [HttpGet("setups")]
    public ActionResult<IReadOnlyList<TitanoSetupInfo>> ListSetups() =>
        Ok(_titanoSetupService.ListSetups());

    [HttpGet("setups/{setupId}")]
    public ActionResult<TitanoFilterSetup> GetSetup(string setupId)
    {
        try
        {
            return Ok(_titanoSetupService.GetSetup(setupId));
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("setups")]
    public ActionResult<TitanoFilterSetup> SaveSetup([FromBody] TitanoFilterSetup setup)
    {
        if (string.IsNullOrWhiteSpace(setup.Name))
        {
            return BadRequest(new { error = "Name e' obbligatorio" });
        }

        var saved = _titanoSetupService.SaveSetup(setup);
        return Ok(saved);
    }

    [HttpPost("apply-filter")]
    public ActionResult<TitanoFilterResult> ApplyFilter([FromBody] TitanoFilterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BacktestingId))
        {
            return BadRequest(new { error = "BacktestingId e' obbligatorio" });
        }

        var backtesting = _backtestingService.GetResult(request.BacktestingId);
        if (backtesting == null)
        {
            return NotFound(new { error = $"Backtesting '{request.BacktestingId}' non trovato" });
        }

        var result = _titanoFilterService.Apply(backtesting, request);
        return Ok(result);
    }

    [HttpGet("rotation-setups")]
    public ActionResult<IReadOnlyList<TitanoSetupInfo>> ListRotationSetups() =>
        Ok(_rotationSetupService.ListSetups());

    [HttpGet("rotation-setups/{setupId}")]
    public ActionResult<TitanoRotationSetup> GetRotationSetup(string setupId)
    {
        try { return Ok(_rotationSetupService.GetSetup(setupId)); }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("rotation-setups")]
    public ActionResult<TitanoRotationSetup> SaveRotationSetup([FromBody] TitanoRotationSetup setup)
    {
        try { return Ok(_rotationSetupService.SaveSetup(setup)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("rotations")]
    public ActionResult<TitanoRotationManifest> StartRotation([FromBody] TitanoRotationRequest request)
    {
        try { return Ok(_rotationService.Run(request)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("rotations")]
    public ActionResult<IReadOnlyList<TitanoRunInfo>> ListRotations(
        [FromQuery] string workspaceId, [FromQuery] string backtestFolder)
    {
        try { return Ok(_rotationService.ListRuns(workspaceId, backtestFolder)); }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("rotations/{runId}")]
    public ActionResult<TitanoRotationManifest> GetRotation(
        string runId, [FromQuery] string workspaceId, [FromQuery] string backtestFolder)
    {
        try { return Ok(_rotationService.Get(workspaceId, backtestFolder, runId)); }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("rotations/{runId}/effective-strategies")]
    public ActionResult<TitanoEffectiveStrategies> EffectiveStrategies(
        string runId, [FromQuery] string workspaceId, [FromQuery] string backtestFolder,
        [FromQuery] DateTime timestampUtc)
    {
        try { return Ok(_rotationService.Resolve(workspaceId, backtestFolder, runId, timestampUtc)); }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("rotations/{runId}/manifest")]
    public IActionResult DownloadManifest(
        string runId, [FromQuery] string workspaceId, [FromQuery] string backtestFolder)
    {
        try
        {
            var manifest = _rotationService.Get(workspaceId, backtestFolder, runId);
            return File(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(manifest,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), "application/json", "manifest.json");
        }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("rotations/{runId}/report")]
    public IActionResult GetRotationReport(
        string runId, [FromQuery] string workspaceId, [FromQuery] string backtestFolder)
    {
        try
        {
            var path = _rotationService.GetHtmlReportPath(workspaceId, backtestFolder, runId);
            return PhysicalFile(path, "text/html; charset=utf-8");
        }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("rotations/{runId}/hard-stop-reset")]
    public ActionResult<TitanoHardStopReset> ResetHardStop(
        string runId, [FromQuery] string workspaceId, [FromQuery] string backtestFolder,
        [FromBody] TitanoHardStopResetRequest request)
    {
        try { return Ok(_rotationService.ResetHardStop(workspaceId, backtestFolder, runId, request)); }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex) when (ex is ArgumentException or IOException)
        { return BadRequest(new { error = ex.Message }); }
    }
}
