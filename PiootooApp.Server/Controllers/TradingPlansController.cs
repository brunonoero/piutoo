using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId}/trading-plans")]
public sealed class TradingPlansController : ControllerBase
{
    private readonly TradingPlanService _plans;
    private readonly TitanoRotationService _titano;

    public TradingPlansController(TradingPlanService plans, TitanoRotationService titano)
    {
        _plans = plans;
        _titano = titano;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<TradingPlan>> List(string workspaceId) =>
        Execute<IReadOnlyList<TradingPlan>>(() => Ok(_plans.List(workspaceId)));

    [HttpGet("{code}")]
    public ActionResult<TradingPlan> Get(string workspaceId, string code) =>
        Execute<TradingPlan>(() => Ok(_plans.Get(workspaceId, code)));

    /// <summary>
    /// Freschezza dell'ultimo run Titano della riga primaria del piano (vedi
    /// <see cref="TradingPlanService.SelectPrimaryRow"/>): il run non si indica più sul piano, si usa
    /// sempre l'ultimo generato per la sua cartella di backtest.
    /// </summary>
    [HttpGet("{code}/rotation-status")]
    public ActionResult<TitanoRotationStatus> GetRotationStatus(string workspaceId, string code) =>
        Execute<TitanoRotationStatus>(() =>
        {
            var plan = _plans.Get(workspaceId, code);
            var folder = plan.Groups.Count == 0 ? null : TradingPlanService.SelectPrimaryRow(plan.Groups).TitanoBacktestFolder;
            return Ok(string.IsNullOrWhiteSpace(folder)
                ? new TitanoRotationStatus { WorkspaceId = workspaceId, BacktestFolder = string.Empty, Freshness = TitanoRotationFreshness.NoRun }
                : _titano.GetFreshness(workspaceId, folder));
        });

    [HttpPut("{code}")]
    public ActionResult<TradingPlan> Save(string workspaceId, string code, SaveTradingPlanRequest request) =>
        Execute(() =>
        {
            if (!code.Equals(request.Code, StringComparison.OrdinalIgnoreCase))
                return ProblemResult<TradingPlan>(400, "Richiesta non valida",
                    "Il codice piano del path non coincide con quello del payload.");
            return Ok(_plans.Save(workspaceId, request));
        });

    [HttpDelete("{code}")]
    public IActionResult Delete(string workspaceId, string code)
    {
        try
        {
            _plans.Delete(workspaceId, code);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return Problem(statusCode: 404, title: "Risorsa non trovata", detail: ex.Message); }
        catch (ArgumentException ex) { return Problem(statusCode: 400, title: "Richiesta non valida", detail: ex.Message); }
    }

    private ActionResult<T> Execute<T>(Func<ActionResult<T>> action)
    {
        try { return action(); }
        catch (KeyNotFoundException ex) { return ProblemResult<T>(404, "Risorsa non trovata", ex.Message); }
        catch (DirectoryNotFoundException ex) { return ProblemResult<T>(404, "Workspace non trovato", ex.Message); }
        catch (ArgumentException ex) { return ProblemResult<T>(400, "Richiesta non valida", ex.Message); }
        catch (InvalidOperationException ex) { return ProblemResult<T>(409, "Operazione non consentita", ex.Message); }
    }

    private ActionResult<T> ProblemResult<T>(int status, string title, string detail) =>
        StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });
}
