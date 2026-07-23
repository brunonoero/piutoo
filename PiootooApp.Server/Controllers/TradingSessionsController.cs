using Microsoft.AspNetCore.Mvc;
using Piootoo.Core.Services;
using Piootoo.Shared.Models.Trading;

namespace PiootooApp.Server.Controllers;

[ApiController]
[Route("api/v1/trading-sessions")]
public sealed class TradingSessionsController : ControllerBase
{
    private readonly ITradingSessionService _sessions;
    public TradingSessionsController(ITradingSessionService sessions) => _sessions = sessions;

    [HttpPost]
    public ActionResult<TradingSessionDescriptor> Create(CreateTradingSessionRequest request)
        => ExecuteResult<TradingSessionDescriptor>(() => Ok(_sessions.Create(request)));

    [HttpPost("{sessionId}/start")]
    public ActionResult<TradingSessionDescriptor> Start(string sessionId, [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<TradingSessionDescriptor>(() => Ok(_sessions.SetStatus(sessionId, token, TradingSessionStatus.Running)));

    [HttpPost("{sessionId}/stop")]
    public ActionResult<TradingSessionDescriptor> Stop(string sessionId, [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<TradingSessionDescriptor>(() => Ok(_sessions.SetStatus(sessionId, token, TradingSessionStatus.Stopped)));

    [HttpPost("{sessionId}/resume")]
    public ActionResult<TradingSessionDescriptor> Resume(string sessionId, [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<TradingSessionDescriptor>(() => Ok(_sessions.SetStatus(sessionId, token, TradingSessionStatus.Running)));

    [HttpPost("{sessionId}/bars")]
    public ActionResult<PushBarsResponse> PushBars(string sessionId, PushBarsRequest request)
        => ExecuteResult<PushBarsResponse>(() =>
        {
            if (sessionId != request.SessionId)
                return ProblemResult<PushBarsResponse>(400, "SessionId non coerente", "Il SessionId del path non coincide con il payload.");
            return Ok(_sessions.PushBars(request));
        });

    [HttpGet("{sessionId}/intents")]
    public ActionResult<IReadOnlyList<OrderIntent>> Intents(
        string sessionId,
        [FromHeader(Name = "X-Session-Token")] string token,
        [FromQuery] long after = 0)
        => ExecuteResult<IReadOnlyList<OrderIntent>>(() => Ok(_sessions.GetIntents(sessionId, token, after)));

    [HttpGet("{sessionId}/signals")]
    public ActionResult<IReadOnlyList<PersistedSignal>> Signals(
        string sessionId,
        [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<IReadOnlyList<PersistedSignal>>(
            () => Ok(_sessions.GetPersistedSignals(sessionId, token)));

    [HttpGet("{sessionId}/trades")]
    public ActionResult<IReadOnlyList<PersistedTrade>> Trades(
        string sessionId,
        [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<IReadOnlyList<PersistedTrade>>(
            () => Ok(_sessions.GetPersistedTrades(sessionId, token)));

    [HttpPost("{sessionId}/execution-reports")]
    public ActionResult<TradingSessionSnapshot> Report(string sessionId, ExecutionReportRequest request)
        => ExecuteResult<TradingSessionSnapshot>(() => Ok(_sessions.ApplyReport(sessionId, request)));

    [HttpGet("{sessionId}/snapshot")]
    public ActionResult<TradingSessionSnapshot> Snapshot(
        string sessionId,
        [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<TradingSessionSnapshot>(() => Ok(_sessions.GetSnapshot(sessionId, token)));

    [HttpDelete("{sessionId}/intents/{intentId}")]
    public IActionResult Cancel(
        string sessionId,
        string intentId,
        [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteAction(() =>
        {
            _sessions.CancelIntent(sessionId, token, intentId);
            return NoContent();
        });

    private ActionResult<T> ExecuteResult<T>(Func<ActionResult<T>> action)
    {
        try { return action(); }
        catch (UnauthorizedAccessException ex) { return ProblemResult<T>(401, "Session token non valido", ex.Message); }
        catch (KeyNotFoundException ex) { return ProblemResult<T>(404, "Risorsa non trovata", ex.Message); }
        catch (DirectoryNotFoundException ex) { return ProblemResult<T>(404, "Workspace non trovato", ex.Message); }
        catch (ArgumentException ex) { return ProblemResult<T>(400, "Richiesta non valida", ex.Message); }
        catch (InvalidOperationException ex) { return ProblemResult<T>(409, "Operazione non consentita", ex.Message); }
    }

    private IActionResult ExecuteAction(Func<IActionResult> action)
    {
        try { return action(); }
        catch (UnauthorizedAccessException ex) { return ProblemResult(401, "Session token non valido", ex.Message); }
        catch (KeyNotFoundException ex) { return ProblemResult(404, "Risorsa non trovata", ex.Message); }
        catch (ArgumentException ex) { return ProblemResult(400, "Richiesta non valida", ex.Message); }
        catch (InvalidOperationException ex) { return ProblemResult(409, "Operazione non consentita", ex.Message); }
    }

    private ActionResult<T> ProblemResult<T>(int status, string title, string detail)
        => StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });

    private IActionResult ProblemResult(int status, string title, string detail)
        => StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });
}
