using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Models.Sapioo;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Controller per l'ottimizzazione Sapioo
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SapiooController : ControllerBase
{
    private readonly ILogger<SapiooController> _logger;
    private readonly IPiootooSapiooService _sapiooService;

    public SapiooController(
        ILogger<SapiooController> logger,
        IPiootooSapiooService sapiooService)
    {
        _logger = logger;
        _sapiooService = sapiooService;
    }

    /// <summary>
    /// Avvia un job di ottimizzazione Sapioo
    /// </summary>
    [HttpPost("start")]
    public ActionResult<string> StartOptimization([FromBody] SapiooRequest request)
    {
        try
        {
            var jobId = _sapiooService.StartOptimization(request);
            return Ok(new { jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'avvio dell'ottimizzazione");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene lo stato di un job di ottimizzazione
    /// </summary>
    [HttpGet("status/{jobId}")]
    public ActionResult<SapiooJob> GetStatus(string jobId)
    {
        try
        {
            _logger.LogInformation("Richiesta stato per jobId: {JobId}", jobId);
            var job = _sapiooService.GetJobStatus(jobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} non trovato", jobId);
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

    /// <summary>
    /// Ottiene il risultato completo di un'ottimizzazione completata
    /// </summary>
    [HttpGet("result/{jobId}")]
    public ActionResult<SapiooResult> GetResult(string jobId)
    {
        try
        {
            _logger.LogInformation("Richiesta risultato per jobId: {JobId}", jobId);
            
            // Prova prima a ottenere dal job attivo
            var job = _sapiooService.GetJobStatus(jobId);
            if (job?.Result != null)
            {
                _logger.LogInformation("Risultato trovato nel job attivo per JobId: {JobId}", jobId);
                return Ok(job.Result);
            }
            
            // Se non trovato nel job, cerca nei file salvati
            _logger.LogInformation("Job non trovato in memoria, cercando nei file salvati per JobId: {JobId}", jobId);
            var result = _sapiooService.GetResult(jobId);
            
            if (result == null)
            {
                _logger.LogWarning("Risultato non trovato per JobId: {JobId}", jobId);
                
                // Prova a ottenere tutte le ottimizzazioni completate per debug
                var allResults = _sapiooService.GetCompletedOptimizations();
                _logger.LogInformation("Totale ottimizzazioni completate trovate: {Count}", allResults.Count);
                foreach (var r in allResults.Take(5))
                {
                    _logger.LogInformation("Ottimizzazione trovata - JobId: '{JobId}', BacktestingName: '{BacktestingName}'", 
                        r.JobId, r.BacktestingName);
                }
                
                return NotFound(new { error = $"Risultato per job {jobId} non trovato" });
            }

            _logger.LogInformation("Risultato trovato per JobId: {JobId}, BacktestingName: {BacktestingName}", jobId, result.BacktestingName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del risultato per JobId: {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene la lista dei nomi dei backtesting disponibili per l'ottimizzazione
    /// </summary>
    [HttpGet("backtestings")]
    public ActionResult<List<string>> GetAvailableBacktestings()
    {
        try
        {
            var backtestings = _sapiooService.GetAvailableBacktestings();
            return Ok(backtestings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei backtesting disponibili");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene la lista di tutte le ottimizzazioni completate
    /// </summary>
    [HttpGet("list")]
    public ActionResult<List<SapiooResult>> GetList()
    {
        try
        {
            var results = _sapiooService.GetCompletedOptimizations();
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero della lista");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un'ottimizzazione completata
    /// </summary>
    [HttpDelete("{jobId}")]
    public ActionResult DeleteOptimization(string jobId)
    {
        try
        {
            _logger.LogInformation("Richiesta eliminazione ottimizzazione per jobId: {JobId}", jobId);
            var deleted = _sapiooService.DeleteOptimization(jobId);
            if (!deleted)
            {
                return NotFound(new { error = $"Ottimizzazione {jobId} non trovata" });
            }

            _logger.LogInformation("Ottimizzazione {JobId} eliminata con successo", jobId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione dell'ottimizzazione per JobId: {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
