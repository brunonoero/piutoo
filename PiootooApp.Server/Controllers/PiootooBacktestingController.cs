using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Piootoo.Core;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Letture del repository dati e calcolo di performance su una lista di trade. Il backtest
/// vero e' PiootooBacktestingService, su api/backtesting: qui non gira nessun motore.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PiootooBacktestingController : ControllerBase
{
    private readonly ILogger<PiootooBacktestingController> _logger;
    private readonly PiootooSettings _settings;

    public PiootooBacktestingController(ILogger<PiootooBacktestingController> logger, IOptions<PiootooSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _settings.ResolvePaths();
    }

    /// <summary>
    /// Calcola la performance per una lista di trade
    /// </summary>
    [HttpPost("calculate-performance")]
    public ActionResult<StrategyPerformance> CalculatePerformance([FromBody] PerformanceRequest request)
    {
        try
        {
            var calculator = new PerformanceCalculator(
                request.InitialBalance ?? 10000m, 
                request.CommissionPerTrade ?? 2m);

            var performance = calculator.CalculatePerformance(
                request.StrategyName,
                request.Trades,
                request.Week,
                request.Year);

            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel calcolo della performance");
            return StatusCode(500, $"Errore: {ex.Message}");
        }
    }

    /// <summary>
    /// Ottiene i dati storici per un backtest
    /// </summary>
    [HttpGet("data/{symbol}")]
    public async Task<ActionResult<List<OhlcvData>>> GetHistoricalData(
        string symbol,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string barType = "OneMinute")
    {
        var repository = new DataSourceRepository(_settings.GetRepositoryPath());
        var data = await repository.LoadDataRangeAsync(symbol, startDate, endDate, barType);
        
        if (!data.Any())
        {
            return NotFound($"Nessun dato trovato per {symbol} nel periodo {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
        }

        return Ok(data);
    }

    /// <summary>
    /// Ottiene le date disponibili per un simbolo
    /// </summary>
    [HttpGet("available-dates/{symbol}")]
    public ActionResult<IEnumerable<DateTime>> GetAvailableDates(
        string symbol,
        [FromQuery] string barType = "OneMinute")
    {
        var repository = new DataSourceRepository(_settings.GetRepositoryPath());
        var dates = repository.GetAvailableDates(symbol, barType);
        return Ok(dates);
    }
}

/// <summary>
/// Request per il calcolo della performance
/// </summary>
public class PerformanceRequest
{
    public string StrategyName { get; set; } = string.Empty;
    public List<TradingResult> Trades { get; set; } = new();
    public int Week { get; set; }
    public int Year { get; set; }
    public decimal? InitialBalance { get; set; }
    public decimal? CommissionPerTrade { get; set; }
}
