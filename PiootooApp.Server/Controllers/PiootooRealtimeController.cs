using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Piootoo.Core;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Strategies;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Controller per il trading in tempo reale
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PiootooRealtimeController : ControllerBase
{
    private readonly ILogger<PiootooRealtimeController> _logger;
    private readonly PiootooSettings _settings;

    public PiootooRealtimeController(ILogger<PiootooRealtimeController> logger, IOptions<PiootooSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _settings.ResolvePaths();
    }

    /// <summary>
    /// Ottiene il setup settimanale corrente
    /// </summary>
    [HttpGet("current-setup")]
    public ActionResult<WeeklySetup> GetCurrentSetup()
    {
        var config = new ScoringConfiguration();
        var rotationManager = new StrategyRotationManager(config);
        var scheduler = new WeeklyRotationScheduler(rotationManager);
        
        // Registra le strategie
        rotationManager.RegisterStrategy("Moving Average Crossover");
        rotationManager.RegisterStrategy("RSI Strategy");
        
        var setup = scheduler.GetCurrentSetup();
        return Ok(setup);
    }


    /// <summary>
    /// Esegue la rotazione settimanale delle strategie
    /// </summary>
    [HttpPost("rotate")]
    public ActionResult<WeeklySetup> ExecuteRotation([FromBody] ScoringConfiguration? config = null)
    {
        config ??= new ScoringConfiguration();
        
        var rotationManager = new StrategyRotationManager(config)
        {
            EvaluationWeeks = 4,
            TopStrategiesToEnable = 2
        };
        
        rotationManager.RegisterStrategy("Moving Average Crossover");
        rotationManager.RegisterStrategy("RSI Strategy");
        
        var scheduler = new WeeklyRotationScheduler(rotationManager);
        var setup = scheduler.ExecuteWeeklyRotation(DateTime.UtcNow);
        
        return Ok(setup);
    }

    /// <summary>
    /// Ottiene i simboli disponibili nel repository
    /// </summary>
    [HttpGet("symbols")]
    public ActionResult<IEnumerable<string>> GetAvailableSymbols()
    {
        var repository = new DataSourceRepository(_settings.GetRepositoryPath());
        var symbols = repository.GetAvailableSymbols();
        return Ok(symbols);
    }

    /// <summary>
    /// Ottiene le informazioni del repository
    /// </summary>
    [HttpGet("repository-info")]
    public ActionResult<RepositoryInfo> GetRepositoryInfo()
    {
        var repository = new DataSourceRepository(_settings.GetRepositoryPath());
        var info = repository.GetRepositoryInfo();
        return Ok(info);
    }

    /// <summary>
    /// Ottiene gli ultimi dati per un simbolo
    /// </summary>
    [HttpGet("data/{symbol}/latest")]
    public async Task<ActionResult<List<OhlcvData>>> GetLatestData(
        string symbol,
        [FromQuery] int sessions = 1,
        [FromQuery] string barType = "OneMinute")
    {
        var repository = new DataSourceRepository(_settings.GetRepositoryPath());
        var data = await repository.LoadLastSessionsAsync(symbol, sessions, barType);
        
        if (!data.Any())
        {
            return NotFound($"Nessun dato trovato per il simbolo {symbol}");
        }

        return Ok(data);
    }
}
