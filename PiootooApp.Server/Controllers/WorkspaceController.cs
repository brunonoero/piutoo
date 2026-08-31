using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkspaceController(
    WorkspaceService workspaceService,
    ExternalBacktestReportService externalReports) : ControllerBase
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

    /// <summary>Crea (o restituisce) l'account di default 1 a 1 con balance iniziale di un milione.</summary>
    [HttpPost("{workspaceId}/accounts/default")]
    public ActionResult<WorkspaceAccount> EnsureDefaultAccount(string workspaceId)
    {
        try { return Ok(workspaceService.EnsureDefaultAccount()); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{workspaceId}/accounts")]
    public ActionResult<IReadOnlyList<WorkspaceAccount>> ListAccounts(string workspaceId)
    {
        try { return Ok(workspaceService.ListAccounts()); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{workspaceId}/accounts/{accountId}")]
    public ActionResult<WorkspaceAccount> GetAccount(string workspaceId, string accountId)
    {
        try { return Ok(workspaceService.GetAccount(accountId)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("{workspaceId}/accounts")]
    public ActionResult<WorkspaceAccount> CreateAccount(string workspaceId, [FromBody] WorkspaceAccount account)
    {
        try
        {
            var created = workspaceService.CreateAccount(account);
            return CreatedAtAction(nameof(GetAccount), new { workspaceId, accountId = created.Id }, created);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{workspaceId}/accounts/{accountId}")]
    public ActionResult<WorkspaceAccount> SaveAccount(string workspaceId, string accountId, [FromBody] WorkspaceAccount account)
    {
        try { return Ok(workspaceService.SaveAccount(accountId, account)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpDelete("{workspaceId}/accounts/{accountId}")]
    public IActionResult DeleteAccount(string workspaceId, string accountId)
    {
        try { workspaceService.DeleteAccount(accountId); return NoContent(); }
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

    /// <summary>
    /// Espone i trade realmente chiusi archiviati nel <c>trades.json</c> del backtest.
    /// </summary>
    [HttpGet("{workspaceId}/backtests/{backtestFolder}/trades")]
    public ActionResult<IReadOnlyList<PersistedTrade>> GetBacktestTrades(
        string workspaceId,
        string backtestFolder)
    {
        try { return Ok(workspaceService.GetBacktestTrades(workspaceId, backtestFolder)); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidDataException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>
    /// Restituisce <c>backtest-summary.json</c> così com'è. Il blocco <c>diagnostics</c> in testa è
    /// la prima cosa da leggere quando un backtest non produce trade.
    /// </summary>
    [HttpGet("{workspaceId}/backtests/{backtestFolder}/summary")]
    public IActionResult GetBacktestSummary(string workspaceId, string backtestFolder)
    {
        try { return Content(workspaceService.GetBacktestSummary(workspaceId, backtestFolder), "application/json"); }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (FileNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>
    /// Report HTML del backtest, servito senza esporre al client il path sul server. Il file è
    /// autosufficiente (grafici inline), quindi si consegna così com'è.
    ///
    /// <para><c>404</c> quando il run non ne ha prodotto uno — interrotto, oppure eseguito
    /// dall'engine esterno: il client lo distingue da un errore e lo dice all'utente invece di
    /// aprire una finestra vuota.</para>
    /// </summary>
    [HttpGet("{workspaceId}/backtests/{backtestFolder}/report")]
    public IActionResult GetBacktestHtmlReport(string workspaceId, string backtestFolder)
    {
        try
        {
            var path = workspaceService.GetBacktestHtmlReportPath(workspaceId, backtestFolder);
            return File(AtomicFileWriter.OpenReadShared(path), "text/html; charset=utf-8");
        }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (FileNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>
    /// Gli artefatti del run impacchettati per un confronto, già rinominati con lo slug del tipo
    /// (<c>trades-interno-futures.json</c> e simili). Il nome dell'archivio arriva nel
    /// <c>Content-Disposition</c>, e lo slug anche in <c>X-Run-Slug</c> perché al client serve
    /// prima di aprire lo zip.
    ///
    /// <para><c>409</c> quando il run non dichiara motore e serie di prezzi: è una cartella
    /// prodotta prima del marcatore, e un artefatto senza tipo in un confronto è peggio che
    /// assente.</para>
    /// </summary>
    [HttpGet("{workspaceId}/backtests/{backtestFolder}/compare-export")]
    public IActionResult ExportBacktestForCompare(string workspaceId, string backtestFolder)
    {
        try
        {
            var bundle = workspaceService.CreateCompareExport(workspaceId, backtestFolder);
            Response.Headers["X-Run-Slug"] = bundle.RunSlug;
            return File(bundle.Content, "application/zip", bundle.FileName);
        }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (FileNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>
    /// Genera il report HTML di un backtest che non ne ha uno proprio: i run dell'engine esterno,
    /// che archiviano i trade ma non il report, e i run interni interrotti prima degli artefatti.
    /// Il report è ricostruito dal <c>trades.json</c> della cartella ed è lo stesso dei run interni.
    /// </summary>
    /// <remarks>
    /// <c>POST</c> e non <c>GET</c> perché scrive un file nella cartella del backtest, e ripeterlo
    /// sostituisce sempre lo stesso file: il client, subito dopo, lo legge dal
    /// <c>GET .../report</c> di sempre.
    /// </remarks>
    [HttpPost("{workspaceId}/backtests/{backtestFolder}/report")]
    public IActionResult GenerateBacktestHtmlReport(
        string workspaceId,
        string backtestFolder,
        [FromQuery] decimal? initialCapital = null)
    {
        try
        {
            var path = externalReports.Generate(workspaceId, backtestFolder, initialCapital);
            return Ok(new { fileName = Path.GetFileName(path) });
        }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (FileNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>Id dei run Titano contenuti nel backtest: servono ad avvisare prima di cancellarlo.</summary>
    [HttpGet("{workspaceId}/backtests/{backtestFolder}/titano-runs")]
    public ActionResult<IReadOnlyList<string>> ListBacktestTitanoRuns(string workspaceId, string backtestFolder)
    {
        try { return Ok(workspaceService.ListBacktestTitanoRunIds(workspaceId, backtestFolder)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    /// <summary>
    /// Elimina la cartella del backtest con tutto il contenuto, run Titano compresi. I piani che
    /// referenziano quei run falliranno all'apertura della sessione: la conferma sta al client.
    /// </summary>
    [HttpDelete("{workspaceId}/backtests/{backtestFolder}")]
    public IActionResult DeleteBacktest(string workspaceId, string backtestFolder)
    {
        try { workspaceService.DeleteBacktest(workspaceId, backtestFolder); return NoContent(); }
        catch (DirectoryNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpDelete("{workspaceId}")]
    public IActionResult Delete(string workspaceId)
    {
        try { workspaceService.Delete(workspaceId); return NoContent(); }
        catch (DirectoryNotFoundException) { return NotFound(); }
    }
}
