using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Piootoo.Core;
using Piootoo.Core.Optimization;
using Piootoo.Core.Services;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Strategies;
using System.Diagnostics;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Controller per l'ottimizzazione delle strategie
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PiootooOptimizationController : ControllerBase
{
    private readonly ILogger<PiootooOptimizationController> _logger;
    private readonly PiootooSettings _settings;
    private readonly SetupRepository _setupRepository;
    private readonly PiootooOptimizationService _optimizationService;

    public PiootooOptimizationController(
        ILogger<PiootooOptimizationController> logger, 
        IOptions<PiootooSettings> settings,
        PiootooOptimizationService optimizationService)
    {
        _logger = logger;
        _settings = settings.Value;
        _settings.ResolvePaths();
        _setupRepository = new SetupRepository(_settings.GetSettingsPath());
        _optimizationService = optimizationService;
    }

    #region Setup CRUD Endpoints

    [HttpGet("setups")]
    public async Task<ActionResult<List<SavedSetup>>> GetAllSetups()
    {
        var setups = await _setupRepository.GetAllAsync();
        return Ok(setups);
    }

    [HttpGet("setups/{id}")]
    public async Task<ActionResult<SavedSetup>> GetSetup(string id)
    {
        var setup = await _setupRepository.GetByIdAsync(id);
        if (setup == null)
            return NotFound($"Setup con ID '{id}' non trovato");
        return Ok(setup);
    }

    [HttpPost("setups/search")]
    public async Task<ActionResult<List<SavedSetup>>> SearchSetups([FromBody] SetupSearchCriteria criteria)
    {
        var setups = await _setupRepository.SearchAsync(criteria);
        return Ok(setups);
    }

    [HttpPost("setups")]
    public async Task<ActionResult<SavedSetup>> CreateSetup([FromBody] SavedSetup setup)
    {
        try
        {
            var created = await _setupRepository.CreateAsync(setup);
            return CreatedAtAction(nameof(GetSetup), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("setups/{id}")]
    public async Task<ActionResult<SavedSetup>> UpdateSetup(string id, [FromBody] SavedSetup setup)
    {
        if (id != setup.Id)
            return BadRequest("ID nel path non corrisponde all'ID nel body");
        try
        {
            var updated = await _setupRepository.UpdateAsync(setup);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("setups/{id}")]
    public async Task<ActionResult> DeleteSetup(string id)
    {
        var deleted = await _setupRepository.DeleteAsync(id);
        if (!deleted)
            return NotFound($"Setup con ID '{id}' non trovato");
        return NoContent();
    }

    [HttpPatch("setups/{id}/activate")]
    public async Task<ActionResult<SavedSetup>> ActivateSetup(string id, [FromQuery] bool active = true)
    {
        try
        {
            var setup = await _setupRepository.SetActiveAsync(id, active);
            return Ok(setup);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("setups/{id}/export")]
    public async Task<ActionResult<string>> ExportSetup(string id)
    {
        try
        {
            var json = await _setupRepository.ExportAsync(id);
            return Ok(json);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("setups/import")]
    public async Task<ActionResult<SavedSetup>> ImportSetup([FromBody] string json)
    {
        try
        {
            var setup = await _setupRepository.ImportAsync(json);
            return CreatedAtAction(nameof(GetSetup), new { id = setup.Id }, setup);
        }
        catch (Exception ex)
        {
            return BadRequest($"Errore nell'importazione: {ex.Message}");
        }
    }

    [HttpGet("setups/active")]
    public async Task<ActionResult<List<SavedSetup>>> GetActiveSetups()
    {
        var setups = await _setupRepository.SearchAsync(new SetupSearchCriteria { IsActive = true });
        return Ok(setups);
    }

    #endregion

    #region Async Optimization Endpoints

    /// <summary>
    /// Avvia un'ottimizzazione BASE in background
    /// </summary>
    [HttpPost("start")]
    public ActionResult<object> StartOptimization([FromBody] OptimizationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.BacktestingId))
            {
                return BadRequest(new { Error = "BacktestingId è obbligatorio" });
            }

            var jobId = _optimizationService.StartBasicOptimization(
                request.BacktestingId,
                request.SetupName,
                request.EvaluationPeriod.Weeks,
                request.RiskParams);

            _logger.LogInformation("Avviata ottimizzazione BASE, JobId: {JobId}", jobId);

            return Ok(new { jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio dell'ottimizzazione");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Avvia un'ottimizzazione AVANZATA in background
    /// </summary>
    [HttpPost("start-advanced")]
    public ActionResult<object> StartAdvancedOptimization([FromBody] AdvancedOptimizationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.BacktestingId))
            {
                return BadRequest(new { Error = "BacktestingId è obbligatorio" });
            }

            // Mappa la configurazione dal DTO
            AdvancedFilterConfig? filterConfig = null;
            if (request.FilterConfig != null)
            {
                filterConfig = new AdvancedFilterConfig
                {
                    MinWinRate = request.FilterConfig.MinWinRate ?? 0.40m,
                    MaxDrawdownLimit = request.FilterConfig.MaxDrawdownLimit ?? -0.25m,
                    MinSharpeRatio = request.FilterConfig.MinSharpeRatio ?? 0.3m,
                    MinTrades = request.FilterConfig.MinTrades ?? 5,
                    MinCompositeScore = request.FilterConfig.MinCompositeScore ?? 0.3m,
                    MinWeeksRequired = request.FilterConfig.MinWeeksRequired ?? 3,
                    MaxCorrelation = request.FilterConfig.MaxCorrelation ?? 0.7m,
                    SharpeWeight = request.FilterConfig.SharpeWeight ?? 0.15m,
                    SortinoWeight = request.FilterConfig.SortinoWeight ?? 0.15m,
                    CalmarWeight = request.FilterConfig.CalmarWeight ?? 0.10m,
                    OmegaWeight = request.FilterConfig.OmegaWeight ?? 0.10m,
                    RecoveryWeight = request.FilterConfig.RecoveryWeight ?? 0.10m,
                    WinRateWeight = request.FilterConfig.WinRateWeight ?? 0.10m,
                    TailRatioWeight = request.FilterConfig.TailRatioWeight ?? 0.05m,
                    GainToPainWeight = request.FilterConfig.GainToPainWeight ?? 0.10m,
                    UlcerPenalty = request.FilterConfig.UlcerPenalty ?? 0.05m,
                    DrawdownPenalty = request.FilterConfig.DrawdownPenalty ?? 0.5m,
                    StabilityBonus = request.FilterConfig.StabilityBonus ?? 0.10m,
                    RiskParityWeight = request.FilterConfig.RiskParityWeight ?? 0.4m,
                    KellyWeight = request.FilterConfig.KellyWeight ?? 0.3m,
                    HRPWeight = request.FilterConfig.HRPWeight ?? 0.3m
                };
            }

            var jobId = _optimizationService.StartAdvancedOptimization(
                request.BacktestingId,
                request.LookbackWeeks,
                filterConfig);

            _logger.LogInformation("Avviata ottimizzazione AVANZATA, JobId: {JobId}", jobId);

            return Ok(new { jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio dell'ottimizzazione avanzata");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Ottiene lo stato di un job di ottimizzazione
    /// Include il risultato se il job è completato
    /// </summary>
    [HttpGet("status/{jobId}")]
    public ActionResult<OptimizationJob> GetStatus(string jobId)
    {
        var job = _optimizationService.GetJobStatus(jobId);
        if (job == null)
        {
            return NotFound(new { Error = $"Job con ID '{jobId}' non trovato" });
        }
        
        // Log per debug
        _logger.LogInformation("GetStatus per job {JobId}: Status={Status}, Type={Type}, HasBasicResult={HasBasic}, HasAdvancedResult={HasAdvanced}, Error={Error}", 
            jobId, job.Status, job.Type, job.BasicResult != null, job.AdvancedResult != null, job.ErrorMessage ?? "none");
        
        // Restituisce il job completo con risultato incluso
        return Ok(job);
    }

    /// <summary>
    /// Ottiene il risultato BASE di un job completato
    /// </summary>
    [HttpGet("result/{jobId}")]
    public ActionResult<FilteredBacktestingResult> GetResult(string jobId)
    {
        var job = _optimizationService.GetJobStatus(jobId);
        if (job == null)
        {
            _logger.LogWarning("GetResult: Job {JobId} non trovato nel dizionario", jobId);
            return NotFound(new { Error = $"Job '{jobId}' non trovato" });
        }
        
        _logger.LogInformation("GetResult: Job {JobId} trovato, Status={Status}, HasResult={HasResult}", 
            jobId, job.Status, job.BasicResult != null);
        
        if (job.BasicResult == null)
        {
            _logger.LogWarning("GetResult: Job {JobId} non ha BasicResult. Status={Status}, Error={Error}", 
                jobId, job.Status, job.ErrorMessage ?? "none");
            return NotFound(new { Error = $"Risultato per job '{jobId}' non ancora disponibile. Status: {job.Status}" });
        }
        
        return Ok(job.BasicResult);
    }

    /// <summary>
    /// Ottiene il risultato AVANZATO di un job completato
    /// </summary>
    [HttpGet("result-advanced/{jobId}")]
    public ActionResult<AdvancedOptimizationResult> GetAdvancedResult(string jobId)
    {
        var result = _optimizationService.GetAdvancedResult(jobId);
        if (result == null)
        {
            return NotFound(new { Error = $"Risultato avanzato per job '{jobId}' non trovato" });
        }
        return Ok(result);
    }

    /// <summary>
    /// Ottiene tutte le ottimizzazioni salvate
    /// </summary>
    [HttpGet("list")]
    public ActionResult<List<FilteredBacktestingResult>> GetSavedOptimizations()
    {
        var results = _optimizationService.GetSavedOptimizations();
        _logger.LogInformation("Trovate {Count} ottimizzazioni salvate", results.Count);
        return Ok(results);
    }

    /// <summary>
    /// Ottiene un'ottimizzazione salvata per ID
    /// </summary>
    [HttpGet("detail/{optimizationId}")]
    public ActionResult<FilteredBacktestingResult> GetOptimization(string optimizationId)
    {
        var result = _optimizationService.GetSavedOptimization(optimizationId);
        if (result == null)
        {
            return NotFound(new { Error = $"Ottimizzazione '{optimizationId}' non trovata" });
        }
        return Ok(result);
    }

    /// <summary>
    /// Elimina un'ottimizzazione salvata
    /// </summary>
    [HttpDelete("detail/{optimizationId}")]
    public ActionResult DeleteOptimization(string optimizationId)
    {
        var deleted = _optimizationService.DeleteOptimization(optimizationId);
        if (!deleted)
        {
            return NotFound(new { Error = $"Ottimizzazione '{optimizationId}' non trovata" });
        }
        return NoContent();
    }

    /// <summary>
    /// Ottiene tutte le ottimizzazioni AVANZATE salvate
    /// </summary>
    [HttpGet("list-advanced")]
    public ActionResult<List<AdvancedOptimizationResult>> GetSavedAdvancedOptimizations()
    {
        var results = _optimizationService.GetSavedAdvancedOptimizations();
        _logger.LogInformation("Trovate {Count} ottimizzazioni avanzate salvate", results.Count);
        return Ok(results);
    }

    /// <summary>
    /// Ottiene un'ottimizzazione AVANZATA salvata per ID
    /// </summary>
    [HttpGet("detail-advanced/{optimizationId}")]
    public ActionResult<AdvancedOptimizationResult> GetAdvancedOptimization(string optimizationId)
    {
        var result = _optimizationService.GetSavedAdvancedOptimization(optimizationId);
        if (result == null)
        {
            return NotFound(new { Error = $"Ottimizzazione avanzata '{optimizationId}' non trovata" });
        }
        return Ok(result);
    }

    /// <summary>
    /// Elimina un'ottimizzazione AVANZATA salvata
    /// </summary>
    [HttpDelete("detail-advanced/{optimizationId}")]
    public ActionResult DeleteAdvancedOptimization(string optimizationId)
    {
        var deleted = _optimizationService.DeleteAdvancedOptimization(optimizationId);
        if (!deleted)
        {
            return NotFound(new { Error = $"Ottimizzazione avanzata '{optimizationId}' non trovata" });
        }
        return NoContent();
    }

    #endregion

    #region Legacy Sync Optimization Endpoints (kept for backward compatibility)

    /// <summary>
    /// Ottimizzazione BASE sincrona (legacy) - filtra il backtesting con parametri di rischio
    /// </summary>
    [HttpPost("optimize")]
    public ActionResult<FilteredBacktestingResult> Optimize([FromBody] OptimizationRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(request.BacktestingId))
            {
                return BadRequest(new { Error = "BacktestingId è obbligatorio" });
            }

            var result = _optimizationService.OptimizeBasic(
                request.BacktestingId,
                request.SetupName,
                request.EvaluationPeriod.Weeks,
                request.RiskParams);

            stopwatch.Stop();
            _logger.LogInformation("Ottimizzazione BASE completata in {Duration}ms", stopwatch.ElapsedMilliseconds);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'ottimizzazione");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Ottimizzazione AVANZATA sincrona (legacy) - algoritmi sofisticati
    /// </summary>
    [HttpPost("optimize-advanced")]
    public ActionResult<AdvancedOptimizationResult> OptimizeAdvanced([FromBody] AdvancedOptimizationRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(request.BacktestingId))
            {
                return BadRequest(new { Error = "BacktestingId è obbligatorio" });
            }

            // Mappa la configurazione dal DTO
            AdvancedFilterConfig? filterConfig = null;
            if (request.FilterConfig != null)
            {
                filterConfig = new AdvancedFilterConfig
                {
                    MinWinRate = request.FilterConfig.MinWinRate ?? 0.40m,
                    MaxDrawdownLimit = request.FilterConfig.MaxDrawdownLimit ?? -0.25m,
                    MinSharpeRatio = request.FilterConfig.MinSharpeRatio ?? 0.3m,
                    MinTrades = request.FilterConfig.MinTrades ?? 5,
                    MinCompositeScore = request.FilterConfig.MinCompositeScore ?? 0.3m,
                    MinWeeksRequired = request.FilterConfig.MinWeeksRequired ?? 3,
                    MaxCorrelation = request.FilterConfig.MaxCorrelation ?? 0.7m,
                    SharpeWeight = request.FilterConfig.SharpeWeight ?? 0.15m,
                    SortinoWeight = request.FilterConfig.SortinoWeight ?? 0.15m,
                    CalmarWeight = request.FilterConfig.CalmarWeight ?? 0.10m,
                    OmegaWeight = request.FilterConfig.OmegaWeight ?? 0.10m,
                    RecoveryWeight = request.FilterConfig.RecoveryWeight ?? 0.10m,
                    WinRateWeight = request.FilterConfig.WinRateWeight ?? 0.10m,
                    TailRatioWeight = request.FilterConfig.TailRatioWeight ?? 0.05m,
                    GainToPainWeight = request.FilterConfig.GainToPainWeight ?? 0.10m,
                    UlcerPenalty = request.FilterConfig.UlcerPenalty ?? 0.05m,
                    DrawdownPenalty = request.FilterConfig.DrawdownPenalty ?? 0.5m,
                    StabilityBonus = request.FilterConfig.StabilityBonus ?? 0.10m,
                    RiskParityWeight = request.FilterConfig.RiskParityWeight ?? 0.4m,
                    KellyWeight = request.FilterConfig.KellyWeight ?? 0.3m,
                    HRPWeight = request.FilterConfig.HRPWeight ?? 0.3m
                };
            }

            var result = _optimizationService.OptimizeAdvanced(
                request.BacktestingId,
                request.LookbackWeeks,
                filterConfig);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Ottimizzazione AVANZATA completata in {Duration}ms", stopwatch.ElapsedMilliseconds);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'ottimizzazione avanzata");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    #endregion

    #region Validation & Presets

    [HttpPost("validate")]
    public ActionResult<ValidationResponse> ValidateRequest([FromBody] OptimizationRequest request)
    {
        var response = new ValidationResponse { IsValid = true };
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(request.SetupName))
            errors.Add("Il nome del setup è obbligatorio");

        if (request.RiskParams.MaxDrawdown > 0)
            errors.Add("MaxDrawdown deve essere negativo (es. -0.15 per -15%)");

        if (request.RiskParams.MinWinRate > 1 || request.RiskParams.MinWinRate < 0)
            errors.Add("MinWinRate deve essere tra 0 e 1");

        response.IsValid = !errors.Any();
        response.Errors = errors;
        response.Warnings = warnings;

        return Ok(response);
    }

    [HttpGet("presets")]
    public ActionResult<List<OptimizationPreset>> GetPresets()
    {
        var presets = new List<OptimizationPreset>
        {
            new()
            {
                Name = "Conservativo",
                Description = "Basso rischio, priorità a stabilità e Sharpe Ratio",
                RiskParams = new RiskParameters
                {
                    MaxDrawdown = -0.10m,
                    MaxConsecutiveLosses = 3,
                    MinWinRate = 0.55m,
                    MinSharpeRatio = 1.0m,
                    MinProfitFactor = 1.5m
                }
            },
            new()
            {
                Name = "Bilanciato",
                Description = "Equilibrio tra rischio e rendimento",
                RiskParams = new RiskParameters
                {
                    MaxDrawdown = -0.15m,
                    MaxConsecutiveLosses = 5,
                    MinWinRate = 0.45m,
                    MinSharpeRatio = 0.7m,
                    MinProfitFactor = 1.3m
                }
            },
            new()
            {
                Name = "Aggressivo",
                Description = "Alto rendimento, maggiore tolleranza al rischio",
                RiskParams = new RiskParameters
                {
                    MaxDrawdown = -0.25m,
                    MaxConsecutiveLosses = 7,
                    MinWinRate = 0.40m,
                    MinSharpeRatio = 0.5m,
                    MinProfitFactor = 1.1m
                }
            }
        };

        return Ok(presets);
    }

    #endregion

    #region Strategies

    [HttpGet("strategies")]
    public ActionResult<List<StrategyDefinition>> GetAllStrategies([FromQuery] string? name = null, [FromQuery] string? symbol = null)
    {
        var strategies = StrategyFactory.GetRegisteredStrategies(name, symbol);
        return Ok(strategies);
    }

    [HttpGet("strategies/symbol/{symbol}")]
    public ActionResult<List<StrategyDefinition>> GetStrategiesBySymbol(string symbol)
    {
        var strategies = StrategyFactory.GetRegisteredStrategies(symbol: symbol);
        return Ok(strategies);
    }

    [HttpPost("strategies/symbols")]
    public ActionResult<List<StrategyDefinition>> GetStrategiesBySymbols([FromBody] List<string> symbols)
    {
        var normalizedSymbols = symbols
            .Select(NormalizeSymbolWithPrefix)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var strategies = StrategyFactory.GetRegisteredStrategies()
            .Where(strategy => normalizedSymbols.Contains(NormalizeSymbolWithPrefix(strategy.Symbol)))
            .ToList();
        return Ok(strategies);
    }

    [HttpGet("strategies/grouped")]
    public ActionResult<List<SymbolStrategiesInfo>> GetStrategiesGroupedBySymbol()
    {
        var grouped = StrategyFactory.GetRegisteredStrategies()
            .GroupBy(s => s.Symbol)
            .Select(g => new SymbolStrategiesInfo
            {
                Symbol = g.Key,
                TotalStrategies = g.Count(),
                ActiveStrategies = g.Count(s => s.IsActive),
                AvailableTimeframes = g.Select(s => s.TimeframeMinutes).Distinct().OrderBy(t => t).ToList(),
                Strategies = g.ToList()
            })
            .OrderBy(s => s.Symbol)
            .ToList();
        return Ok(grouped);
    }

    [HttpGet("strategies/available-symbols")]
    public ActionResult<List<string>> GetSymbolsWithStrategies()
    {
        var symbols = StrategyFactory.GetRegisteredSymbols();
        return Ok(symbols);
    }

    #endregion

    #region Symbols

    [HttpGet("symbols")]
    public ActionResult<List<SymbolInfo>> GetAvailableSymbols()
    {
        var repository = new DataSourceRepository(_settings.GetRepositoryPath());
        var info = repository.GetRepositoryInfo();
        return Ok(info.SymbolDetails);
    }

    #endregion

    private static string NormalizeSymbolWithPrefix(string symbol)
    {
        var normalized = symbol.Trim().TrimStart('@').ToUpperInvariant();
        return string.IsNullOrEmpty(normalized) ? normalized : $"@{normalized}";
    }
}

/// <summary>
/// Preset di ottimizzazione
/// </summary>
public class OptimizationPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OptimizationParameters OptimizationParams { get; set; } = new();
    public RiskParameters RiskParams { get; set; } = new();
}

/// <summary>
/// Risposta validazione
/// </summary>
public class ValidationResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
