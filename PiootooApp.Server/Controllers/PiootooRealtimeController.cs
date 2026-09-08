using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Letture del repository dati usate dalla console. Il trading in tempo reale non passa
/// di qui: sta su api/v1/trading-sessions.
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
