using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Strategies;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/strategies")]
public sealed class StrategiesController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<StrategyCatalogItem>> List()
        => Ok(StrategyFactory.GetRegisteredStrategies()
            .Select(strategy => new StrategyCatalogItem
            {
                Id = strategy.Id,
                Code = strategy.Id,
                Name = strategy.Name,
                Symbol = strategy.Symbol,
                TimeframeMinutes = strategy.TimeframeMinutes,
                BarType = strategy.BarType,
                Description = strategy.Description,
                Type = strategy.Type.ToString(),
                IsActive = strategy.IsActive,
                SourceFileName = strategy.FileName,
                Overnight = strategy.Holding.Overnight,
                Overweek = strategy.Holding.Overweek,
                HoldingLabel = strategy.Holding.Describe()
            })
            .OrderBy(strategy => strategy.Symbol)
            .ThenBy(strategy => strategy.Name)
            .ToList());
}
