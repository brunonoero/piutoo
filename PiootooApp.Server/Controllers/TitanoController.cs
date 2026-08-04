using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Optimization;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TitanoController : ControllerBase
{
    private readonly TitanoRotationSetupService _rotationSetupService;
    private readonly TitanoRotationService _rotationService;

    public TitanoController(
        TitanoRotationSetupService rotationSetupService,
        TitanoRotationService rotationService)
    {
        _rotationSetupService = rotationSetupService;
        _rotationService = rotationService;
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

    [HttpDelete("rotation-setups/{setupId}")]
    public IActionResult DeleteRotationSetup(string setupId)
    {
        try
        {
            _rotationSetupService.DeleteSetup(setupId);
            return NoContent();
        }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
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

    /// <summary>
    /// Run del workspace. Senza <c>backtestFolder</c> l'elenco è quello di tutte le cartelle:
    /// i run vivono dentro il backtest da cui derivano, ma per chi ne referenzia uno la
    /// gerarchia è un dettaglio di archiviazione.
    /// </summary>
    [HttpGet("rotations")]
    public ActionResult<IReadOnlyList<TitanoRunInfo>> ListRotations(
        [FromQuery] string workspaceId, [FromQuery] string? backtestFolder = null)
    {
        try
        {
            return Ok(string.IsNullOrWhiteSpace(backtestFolder)
                ? _rotationService.ListRuns(workspaceId)
                : _rotationService.ListRuns(workspaceId, backtestFolder));
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException)
        { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Elimina un run con tutto il suo contenuto. Il server non verifica quali piani lo
    /// referenziano: il controllo sta al client, che li conosce e può nominarli prima di
    /// chiedere conferma.
    /// </summary>
    [HttpDelete("rotations/{runId}")]
    public IActionResult DeleteRotation(
        string runId, [FromQuery] string workspaceId, [FromQuery] string backtestFolder)
    {
        try { _rotationService.DeleteRun(workspaceId, backtestFolder, runId); return NoContent(); }
        catch (DirectoryNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (IOException ex) { return BadRequest(new { error = ex.Message }); }
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
