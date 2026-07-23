using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Piootoo.Core;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Strategies;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// Controller per il backtesting delle strategie
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
    /// Esegue un backtest con rotazione settimanale
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<BacktestResult>> RunBacktest([FromBody] BacktestRequest request)
    {
        try
        {
            var repository = new DataSourceRepository(_settings.GetRepositoryPath());
            var data = await repository.LoadDataRangeAsync(
                request.Symbol, 
                request.StartDate, 
                request.EndDate, 
                request.BarType ?? "OneMinute");

            if (!data.Any())
            {
                return NotFound($"Nessun dato trovato per {request.Symbol} nel periodo specificato");
            }

            var config = request.ScoringConfig ?? new ScoringConfiguration();
            var rotationManager = new StrategyRotationManager(config)
            {
                EvaluationWeeks = request.EvaluationWeeks ?? 4,
                TopStrategiesToEnable = request.TopStrategies ?? 2
            };

            var engine = new TradingEngine(
                rotationManager, 
                request.InitialBalance ?? 10000m, 
                request.CommissionPerTrade ?? 2m);


            // Esegui backtest
            var result = engine.RunBacktestWithWeeklyRotation(
                data.ToArray(), 
                request.StartDate, 
                request.EndDate);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il backtest");
            return StatusCode(500, $"Errore: {ex.Message}");
        }
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
/// Request per il backtest
/// </summary>
public class BacktestRequest
{
    public string Symbol { get; set; } = "@ES";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? BarType { get; set; }
    public decimal? InitialBalance { get; set; }
    public decimal? CommissionPerTrade { get; set; }
    public int? EvaluationWeeks { get; set; }
    public int? TopStrategies { get; set; }
    public ScoringConfiguration? ScoringConfig { get; set; }
    
    // Parametri Moving Average
    public int? MaShortPeriod { get; set; }
    public int? MaLongPeriod { get; set; }
    
    // Parametri RSI
    public int? RsiPeriod { get; set; }
    public decimal? RsiOversold { get; set; }
    public decimal? RsiOverbought { get; set; }
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
