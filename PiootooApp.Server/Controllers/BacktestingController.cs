using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Controller per il backtesting
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BacktestingController : ControllerBase
{
    private readonly ILogger<BacktestingController> _logger;
    private readonly IPiootooBacktestingService _backtestingService;
    private readonly WorkspaceService _workspaceService;

    public BacktestingController(
        ILogger<BacktestingController> logger,
        IPiootooBacktestingService backtestingService,
        WorkspaceService workspaceService)
    {
        _logger = logger;
        _backtestingService = backtestingService;
        _workspaceService = workspaceService;
    }

    /// <summary>
    /// Avvia un job di backtesting
    /// </summary>
    [HttpPost("start")]
    public ActionResult<string> StartBacktesting([FromBody] BacktestingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.WorkspaceId))
            {
                return BadRequest(new { error = "WorkspaceId è obbligatorio." });
            }

            request.BacktestFolderName = WorkspaceBacktestPaths.NormalizeFolderName(
                string.IsNullOrWhiteSpace(request.BacktestFolderName) ? request.Name : request.BacktestFolderName);
            request.Name = string.IsNullOrWhiteSpace(request.Name) ? request.BacktestFolderName : request.Name.Trim();
            if (request.EndDate <= request.StartDate)
            {
                return BadRequest(new { error = "La data finale deve essere successiva alla data iniziale." });
            }

            var masterFilter = _workspaceService.GetMasterFilter(request.WorkspaceId);
            if (masterFilter.StrategiesFilter.Count == 0)
            {
                return BadRequest(new { error = "Il workspace non contiene strategie abilitate." });
            }

            var catalogIds = StrategyFactory.GetRegisteredStrategies()
                .Select(strategy => strategy.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var invalidIds = masterFilter.StrategiesFilter
                .Where(id => !catalogIds.Contains(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id)
                .ToList();
            if (invalidIds.Count > 0)
            {
                return BadRequest(new
                {
                    error = "Il masterfilter contiene ID strategia non eseguibili: " +
                            string.Join("; ", invalidIds.Select(StrategyFactory.DescribeUnusableId))
                });
            }

            // Il payload client non è mai una fonte di selezione: si usa soltanto il masterfilter server-side.
            request.SelectedStrategyIds = masterFilter.StrategiesFilter.ToList();
            request.SelectedSymbols = new List<string>();
            var jobId = _backtestingService.StartBacktesting(request);
            return Ok(new { jobId });
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'avvio del backtesting");
            return StatusCode(500, new { error = "Errore interno durante l'avvio del backtesting." });
        }
    }

    /// <summary>
    /// Ottiene lo stato di un job di backtesting
    /// </summary>
    [HttpGet("status/{jobId}")]
    public ActionResult<BacktestingJob> GetStatus(string jobId)
    {
        try
        {
            _logger.LogInformation("Richiesta stato per jobId: {JobId}", jobId);
            var job = _backtestingService.GetJobStatus(jobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} non trovato nel dizionario", jobId);
                // Prova a cercare il risultato nei file salvati
                var result = _backtestingService.GetResult(jobId);
                if (result != null)
                {
                    _logger.LogInformation("Job {JobId} trovato nei file salvati, restituisco job completato", jobId);
                    // Restituisci un job completato
                    return Ok(new BacktestingJob
                    {
                        JobId = jobId,
                        Status = BacktestingJobStatus.Completed,
                        ProgressPercent = 100,
                        Result = result,
                        CompletedAt = DateTime.UtcNow
                    });
                }
                return NotFound(new { error = $"Job {jobId} non trovato" });
            }

            _logger.LogInformation("Job {JobId} trovato con status: {Status}", jobId, job.Status);
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dello stato per jobId: {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Richiede la cancellazione idempotente di un job.</summary>
    [HttpPost("cancel/{jobId}")]
    public ActionResult<BacktestingJob> Cancel(string jobId)
    {
        var job = _backtestingService.CancelBacktesting(jobId);
        if (job == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Job di backtesting non trovato",
                Detail = $"Il job '{jobId}' non esiste."
            });
        }

        return Ok(job);
    }

    /// <summary>
    /// Ottiene il risultato completo di un backtesting completato
    /// </summary>
    [HttpGet("result/{jobId}")]
    public ActionResult<BacktestingResult> GetResult(string jobId)
    {
        try
        {
            _logger.LogInformation("Richiesta risultato per jobId: {JobId}", jobId);
            
            // Prova prima a ottenere dal job attivo
            var job = _backtestingService.GetJobStatus(jobId);
            if (job?.Result != null)
            {
                _logger.LogInformation("Risultato trovato nel job attivo per JobId: {JobId}", jobId);
                return Ok(job.Result);
            }
            
            // Se non trovato nel job, cerca nei file salvati
            _logger.LogInformation("Job non trovato in memoria, cercando nei file salvati per JobId: {JobId}", jobId);
            var result = _backtestingService.GetResult(jobId);
            
            if (result == null)
            {
                _logger.LogWarning("Risultato non trovato per JobId: {JobId}", jobId);
                
                // Prova a ottenere tutti i backtesting completati per debug
                var allResults = _backtestingService.GetCompletedBacktestings();
                _logger.LogInformation("Totale backtesting completati trovati: {Count}", allResults.Count);
                foreach (var r in allResults.Take(5))
                {
                    _logger.LogInformation("Backtesting trovato - JobId: '{JobId}', SetupName: '{SetupName}', StartDate: {StartDate}", 
                        r.JobId, r.SetupName, r.StartDate);
                }
                
                return NotFound(new { error = $"Risultato per job {jobId} non trovato" });
            }

            _logger.LogInformation("Risultato trovato per JobId: {JobId}, SetupName: {SetupName}", jobId, result.SetupName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del risultato per JobId: {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Restituisce il report HTML prodotto dal job, senza esporre path server al client.</summary>
    [HttpGet("output/{jobId}/report")]
    public IActionResult GetHtmlReport(string jobId)
    {
        var result = _backtestingService.GetJobStatus(jobId)?.Result ?? _backtestingService.GetResult(jobId);
        if (result == null || string.IsNullOrWhiteSpace(result.HtmlReportFilePath) ||
            !System.IO.File.Exists(result.HtmlReportFilePath))
        {
            return NotFound(new { error = $"Report del job {jobId} non disponibile." });
        }

        return SharedFile(result.HtmlReportFilePath, "text/html; charset=utf-8");
    }

    [HttpGet("output/{jobId}/signals")]
    public IActionResult GetSignals(string jobId)
        => GetTradingJson(jobId, TradingPersistenceSchema.SignalsFileName);

    [HttpGet("output/{jobId}/trades")]
    public IActionResult GetTrades(string jobId)
        => GetTradingJson(jobId, TradingPersistenceSchema.TradesFileName);

    /// <summary>
    /// Ottiene la lista di tutti i backtesting completati
    /// </summary>
    [HttpGet("list")]
    public ActionResult<List<BacktestingResult>> GetList()
    {
        try
        {
            var summaries = _backtestingService.GetCompletedBacktestingSummaries();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero della lista");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un backtesting completato
    /// </summary>
    [HttpDelete("{jobId}")]
    public ActionResult DeleteBacktesting(string jobId)
    {
        try
        {
            _logger.LogInformation("Richiesta eliminazione backtesting per jobId: {JobId}", jobId);
            var deleted = _backtestingService.DeleteBacktesting(jobId);
            if (!deleted)
            {
                return NotFound(new { error = $"Backtesting {jobId} non trovato" });
            }

            _logger.LogInformation("Backtesting {JobId} eliminato con successo", jobId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione del backtesting per JobId: {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private IActionResult GetTradingJson(string jobId, string fileName)
    {
        var result = _backtestingService.GetJobStatus(jobId)?.Result ?? _backtestingService.GetResult(jobId);
        var outputDirectory = result?.ResultFilePath is null
            ? null
            : Path.GetDirectoryName(result.ResultFilePath);
        var path = outputDirectory is null ? null : Path.Combine(outputDirectory, fileName);
        if (path is null || !System.IO.File.Exists(path))
            return NotFound(new { error = $"{fileName} del job {jobId} non disponibile." });
        return SharedFile(path, "application/json; charset=utf-8", fileName);
    }

    private FileStreamResult SharedFile(string path, string contentType, string? downloadName = null)
    {
        var stream = AtomicFileWriter.OpenReadShared(path);
        return File(stream, contentType, downloadName);
    }

}
