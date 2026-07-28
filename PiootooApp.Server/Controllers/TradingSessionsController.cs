using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    /// <summary>
    /// Log diagnostico di rotazione (una riga per barra) per sessioni collegate a un run Titano: spiega
    /// per ciascuna strategia se è stata inclusa nella valutazione e perché, incrociato con i segnali
    /// effettivamente generati. Utile per verificare che le esclusioni Titano siano corrette e che le
    /// strategie eseguano trade coerentemente con quanto progettato.
    /// </summary>
    [HttpGet("{sessionId}/rotation-log")]
    public ActionResult<IReadOnlyList<RotationLogEntry>> RotationLog(
        string sessionId,
        [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<IReadOnlyList<RotationLogEntry>>(
            () => Ok(_sessions.GetRotationLog(sessionId, token)));

    [HttpPost("{sessionId}/execution-reports")]
    public ActionResult<TradingSessionSnapshot> Report(string sessionId, ExecutionReportRequest request)
        => ExecuteResult<TradingSessionSnapshot>(() => Ok(_sessions.ApplyReport(sessionId, request)));

    /// <summary>Configura (sostituendola) la mappa account -> gruppo usata per l'anti copy-trading.</summary>
    [HttpPut("{sessionId}/account-groups")]
    public ActionResult<TradingSessionSnapshot> SetAccountGroups(string sessionId, SetAccountGroupsRequest request)
        => ExecuteResult<TradingSessionSnapshot>(() =>
        {
            _sessions.SetAccountGroups(sessionId, request.SessionToken, request.Accounts);
            return Ok(_sessions.GetSnapshot(sessionId, request.SessionToken));
        });

    /// <summary>Legge la mappa account -> gruppo corrente.</summary>
    [HttpGet("{sessionId}/account-groups")]
    public ActionResult<IReadOnlyList<AccountGroupMapping>> GetAccountGroups(
        string sessionId, [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<IReadOnlyList<AccountGroupMapping>>(() => Ok(_sessions.GetAccountGroups(sessionId, token)));

    /// <summary>Configura gruppi, account e profilo Titano per gruppo (sostituisce l'intera configurazione).</summary>
    [HttpPut("{sessionId}/groups")]
    public ActionResult<TradingSessionSnapshot> SetTradingGroups(string sessionId, SetTradingGroupsRequest request)
        => ExecuteResult<TradingSessionSnapshot>(() =>
        {
            _sessions.SetTradingGroups(sessionId, request.SessionToken, request.Rows);
            return Ok(_sessions.GetSnapshot(sessionId, request.SessionToken));
        });

    /// <summary>Legge la configurazione gruppi/account/Titano corrente.</summary>
    [HttpGet("{sessionId}/groups")]
    public ActionResult<IReadOnlyList<TradingGroupRow>> GetTradingGroups(
        string sessionId, [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<IReadOnlyList<TradingGroupRow>>(() => Ok(_sessions.GetTradingGroups(sessionId, token)));

    /// <summary>
    /// Chiamata dal cBot di un singolo account cTrader: restituisce il prossimo segnale da eseguire
    /// (chiusura di una posizione già assegnata, oppure un nuovo ingresso libero nel proprio gruppo,
    /// in ordine di priorità), oppure nessun segnale se l'account è già occupato o non c'è nulla libero.
    /// </summary>
    [HttpPost("{sessionId}/accounts/{accountNumber}/signal")]
    public ActionResult<AccountSignalResponse> NextSignal(
        string sessionId,
        string accountNumber,
        [FromHeader(Name = "X-Session-Token")] string token,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AccountSignalPollRequest? request)
        => ExecuteResult<AccountSignalResponse>(() => Ok(request is null
            ? _sessions.GetNextSignalForAccount(sessionId, token, accountNumber)
            : _sessions.PollSignalForAccount(sessionId, accountNumber, request)));

    /// <summary>
    /// Registra un intent di chiusura (OrderIntentKind.Close) per una posizione che un cBot ExternalBroker ha
    /// già deciso di chiudere in locale (Stop Loss/Take Profit nativi del broker, limite di barre) e per
    /// gia' chiuso applicando la specifica di uscita dell'intent di ingresso. Il client referenzia l'IntentId
    /// restituito nel normale POST /execution-reports per completare la chiusura.
    /// </summary>
    [HttpPost("{sessionId}/intents/close-external")]
    public ActionResult<OrderIntent> CreateExternalCloseIntent(string sessionId, CreateExternalCloseIntentRequest request)
        => ExecuteResult<OrderIntent>(() => Ok(_sessions.CreateExternalCloseIntent(sessionId, request)));

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
