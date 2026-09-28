using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services.BrokerWorkspaces;
using Piootoo.Shared.Models.BrokerWorkspaces;
using Piootoo.Shared.Models.Trading;

namespace PiootooApp.Server.Controllers;

/// <summary>
/// I piani in produzione, uno spazio per broker. Nessun <c>PUT</c> e nessun <c>DELETE</c>: un piano
/// di produzione nasce da un best plan, si duplica per cambiare conti, si ritira. Vedi
/// <see cref="BrokerWorkspaceService"/>.
/// </summary>
[ApiController]
[Route("api/v1/broker-workspaces")]
public sealed class BrokerWorkspacesController(BrokerWorkspaceService brokerWorkspaces) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<BrokerWorkspaceSummary>> List() =>
        Execute<IReadOnlyList<BrokerWorkspaceSummary>>(() => Ok(brokerWorkspaces.List()));

    [HttpGet("{brokerCode}")]
    public ActionResult<BrokerWorkspaceDetail> Get(string brokerCode) =>
        Execute<BrokerWorkspaceDetail>(() => Ok(brokerWorkspaces.Get(brokerCode)));

    [HttpGet("{brokerCode}/plans/{code}")]
    public ActionResult<TradingPlan> GetPlan(string brokerCode, string code) =>
        Execute<TradingPlan>(() => Ok(brokerWorkspaces.GetPlan(brokerCode, code)));

    /// <summary>
    /// Promuove un best plan a piano di produzione. Con <c>DryRun</c> restituisce il piano che
    /// nascerebbe senza scriverlo; un conflitto e' un <c>409</c> in entrambi i casi.
    /// </summary>
    [HttpPost("{brokerCode}/plans")]
    public ActionResult<TradingPlan> Promote(string brokerCode, [FromBody] PromoteToProductionRequest request) =>
        Execute<TradingPlan>(() => Ok(brokerWorkspaces.Promote(brokerCode, request)));

    /// <summary>
    /// Come la promozione per broker, con il broker del piano di origine: la console promuove dal best
    /// plan, che il broker non lo porta.
    /// </summary>
    [HttpPost("plans")]
    public ActionResult<TradingPlan> PromoteToItsBroker([FromBody] PromoteToProductionRequest request) =>
        Execute<TradingPlan>(() => Ok(brokerWorkspaces.Promote(brokerCode: null, request)));

    /// <summary>Duplica il piano con conti nuovi e ritira l'originale: il cambio di numero di conto.</summary>
    [HttpPost("{brokerCode}/plans/{code}/duplicate")]
    public ActionResult<TradingPlan> Duplicate(
        string brokerCode, string code, [FromBody] DuplicateProductionPlanRequest request) =>
        Execute<TradingPlan>(() => Ok(brokerWorkspaces.Duplicate(brokerCode, code, request)));

    /// <summary>Ritira il piano. Idempotente.</summary>
    [HttpPost("{brokerCode}/plans/{code}/retire")]
    public ActionResult<TradingPlan> Retire(string brokerCode, string code) =>
        Execute<TradingPlan>(() => Ok(brokerWorkspaces.Retire(brokerCode, code)));

    private ActionResult<T> Execute<T>(Func<ActionResult<T>> action)
    {
        try { return action(); }
        catch (KeyNotFoundException ex) { return ProblemResult<T>(404, "Risorsa non trovata", ex.Message); }
        catch (DirectoryNotFoundException ex) { return ProblemResult<T>(404, "Best plan non trovato", ex.Message); }
        catch (ArgumentException ex) { return ProblemResult<T>(400, "Richiesta non valida", ex.Message); }
        catch (InvalidOperationException ex) { return ProblemResult<T>(409, "Operazione non consentita", ex.Message); }
    }

    private ActionResult<T> ProblemResult<T>(int status, string title, string detail) =>
        StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });
}
