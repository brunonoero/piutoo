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
    private readonly ILogger<TradingSessionsController> _log;

    public TradingSessionsController(ITradingSessionService sessions, ILogger<TradingSessionsController> log)
    {
        _sessions = sessions;
        _log = log;
    }

    [HttpPost]
    public ActionResult<TradingSessionDescriptor> Create(CreateTradingSessionRequest request)
        => ExecuteResult<TradingSessionDescriptor>(() => Ok(_sessions.Create(request)));

    /// <summary>Elenco leggero di tutte le sessioni vive, incluse quelle aperte da un cBot.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<TradingSessionSummary>> List()
        => ExecuteResult<IReadOnlyList<TradingSessionSummary>>(() => Ok(_sessions.ListSessions()));

    /// <summary>
    /// Presidio realtime di un conto: cosa il server sta governando e dove serve un intervento a
    /// mano su cTrader. Senza token: la si apre proprio quando la sessione non c'è più.
    /// Vedi <c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c> §8.
    /// </summary>
    [HttpGet("accounts/{accountNumber}/watch")]
    public ActionResult<AccountRealtimeWatch> AccountWatch(string accountNumber)
        => ExecuteResult<AccountRealtimeWatch>(() => Ok(_sessions.GetAccountWatch(accountNumber)));

    /// <summary>Crea o riprende idempotentemente una sessione usando il solo codice piano.</summary>
    [HttpPost("open-plan")]
    public ActionResult<TradingSessionDescriptor> OpenPlan(OpenTradingPlanSessionRequest request)
        => ExecuteResult<TradingSessionDescriptor>(() => Ok(_sessions.OpenFromPlan(request)));

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
            var result = _sessions.PushBars(request);
            LogIntents(sessionId, result.Intents);
            return Ok(result);
        });

    /// <summary>
    /// Come <c>POST /bars</c>, ma il client invia per ogni stream l'intera finestra di candele che le
    /// strategie richiedono: il server accoda quelle che gli mancano e valuta solo l'ultima.
    /// </summary>
    [HttpPost("{sessionId}/bars/window")]
    public ActionResult<PushBarWindowResponse> PushBarWindow(string sessionId, PushBarWindowRequest request)
        => ExecuteResult<PushBarWindowResponse>(() =>
        {
            if (sessionId != request.SessionId)
                return ProblemResult<PushBarWindowResponse>(400, "SessionId non coerente", "Il SessionId del path non coincide con il payload.");
            var result = _sessions.PushBarWindow(request);
            LogStreams(sessionId, result.Streams);
            LogIntents(sessionId, result.Intents);
            return Ok(result);
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
        => ExecuteResult<TradingSessionSnapshot>(() =>
        {
            var snapshot = _sessions.ApplyReport(sessionId, request);
            LogFillCost(sessionId, request.Report);
            return Ok(snapshot);
        });

    /// <summary>
    /// Costo di esecuzione di un fill: prezzo e spread dichiarati dal client. È l'unica traccia
    /// lato server di quanto costa davvero eseguire su quello strumento — il cBot lo stampa nel
    /// proprio log, ma quel log vive quanto il backtest e non finisce da nessuna parte.
    ///
    /// <para>Solo sui fill: un report di rifiuto o annullamento non ha un prezzo di esecuzione, e
    /// stamparlo riempirebbe il log delle stesse righe che la deduplica del claim toglie.</para>
    /// </summary>
    private void LogFillCost(string sessionId, ExternalExecutionReport report)
    {
        if (report.Status != ExecutionReportStatus.Filled || report.SpreadAtFill is not { } spread)
            return;

        _log.LogInformation(
            "[{SessionId}] fill {IntentId} @ {Price} spread {Spread}",
            sessionId, report.IntentId,
            report.FillPrice?.ToString("0.#####") ?? "-", spread.ToString("0.###"));
    }

    /// <summary>
    /// Configura (sostituendoli) i conti che eseguono la sessione. Sostituisce i vecchi
    /// <c>PUT /account-groups</c> e <c>PUT /groups</c>: i gruppi non esistono piu', e il tetto di
    /// concorrenza lo dichiara il piano una volta sola.
    /// </summary>
    [HttpPut("{sessionId}/accounts")]
    public ActionResult<TradingSessionSnapshot> SetSessionAccounts(string sessionId, SetSessionAccountsRequest request)
        => ExecuteResult<TradingSessionSnapshot>(() =>
        {
            _sessions.SetSessionAccounts(sessionId, request.SessionToken, request.Accounts);
            return Ok(_sessions.GetSnapshot(sessionId, request.SessionToken));
        });

    /// <summary>Legge i conti configurati sulla sessione.</summary>
    [HttpGet("{sessionId}/accounts")]
    public ActionResult<IReadOnlyList<string>> GetSessionAccounts(
        string sessionId, [FromHeader(Name = "X-Session-Token")] string token)
        => ExecuteResult<IReadOnlyList<string>>(() => Ok(_sessions.GetSessionAccounts(sessionId, token)));

    /// <summary>
    /// Chiamata dal cBot di un singolo account cTrader: restituisce il prossimo segnale da eseguire
    /// (chiusura di una posizione già assegnata, oppure un ingresso che quel conto non ha ancora
    /// reclamato, in ordine di priorità), oppure nessun segnale se il conto ha esaurito il budget o
    /// non c'è nulla di libero.
    /// </summary>
    [HttpPost("{sessionId}/accounts/{accountNumber}/signal")]
    public ActionResult<AccountSignalResponse> NextSignal(
        string sessionId,
        string accountNumber,
        [FromHeader(Name = "X-Session-Token")] string token,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AccountSignalPollRequest? request)
        => ExecuteResult<AccountSignalResponse>(() =>
        {
            var response = request is null
                ? _sessions.GetNextSignalForAccount(sessionId, token, accountNumber)
                : _sessions.PollSignalForAccount(sessionId, accountNumber, request);
            LogClaim(sessionId, accountNumber, response);
            return Ok(response);
        });

    /// <summary>
    /// Registra un intent di chiusura (OrderIntentKind.Close) per una posizione che un cBot ExternalBroker ha
    /// già deciso di chiudere in locale (Stop Loss/Take Profit nativi del broker, limite di barre) e per
    /// gia' chiuso applicando la specifica di uscita dell'intent di ingresso. Il client referenzia l'IntentId
    /// restituito nel normale POST /execution-reports per completare la chiusura.
    /// </summary>
    [HttpPost("{sessionId}/intents/close-external")]
    public ActionResult<OrderIntent> CreateExternalCloseIntent(string sessionId, CreateExternalCloseIntentRequest request)
        => ExecuteResult<OrderIntent>(() => Ok(_sessions.CreateExternalCloseIntent(sessionId, request)));

    /// <summary>
    /// Copia i trade della sessione in <c>&lt;workspace&gt;/backtests/{cartella}/</c>, così che un
    /// run sull'engine esterno sia confrontabile con uno interno: senza questo passaggio sessioni e
    /// backtest restano in due alberi diversi.
    /// </summary>
    [HttpPost("{sessionId}/promote-to-backtest")]
    public ActionResult<PromoteSessionToBacktestResult> PromoteToBacktest(
        string sessionId,
        PromoteSessionToBacktestRequest request)
        => ExecuteResult<PromoteSessionToBacktestResult>(() => Ok(_sessions.PromoteToBacktest(sessionId, request)));

    /// <summary>
    /// Gli ultimi eventi della sessione, per il monitor della console. Il client passa in
    /// <c>since</c> il progressivo dell'ultimo evento gia' mostrato e riceve solo il nuovo.
    ///
    /// <para>Complementare a <c>/snapshot</c>, non alternativo: lo snapshot dice cos'e' aperto
    /// adesso, questo dice cosa e' successo e perche' — in particolare quale filtro ha svuotato un
    /// claim, che non e' uno stato e quindi nello snapshot non c'e'.</para>
    /// </summary>
    [HttpGet("{sessionId}/activity")]
    public ActionResult<SessionActivityResponse> Activity(
        string sessionId,
        [FromHeader(Name = "X-Session-Token")] string token,
        [FromQuery] long since = 0)
        => ExecuteResult<SessionActivityResponse>(() => Ok(_sessions.GetActivity(sessionId, token, since)));

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

    /// <summary>
    /// Una riga sulla console del server per ogni intent nato da una barra. È l'unico punto in cui si
    /// vede un segnale nel momento in cui viene generato: la sessione persiste su file signal e trade,
    /// ma a run finito, e il cBot vede solo ciò che gli viene consegnato — non un intent annullato dal
    /// sizing o dal limite di ingressi, che è proprio il caso da capire quando "non arriva niente".
    ///
    /// <para>Il livello è Information perché è ciò che l'operatore guarda mentre il run gira; se
    /// diventa rumoroso si alza a Debug la voce <c>PiootooApp.Server.Controllers</c> in
    /// appsettings.</para>
    /// </summary>
    private void LogIntents(string sessionId, IReadOnlyList<OrderIntent> intents)
    {
        foreach (var intent in intents)
        {
            // Quantità: quella finale è ciò che verrà eseguito, quella base è ciò che la strategia
            // aveva chiesto. Vederle insieme distingue "segnale assente" da "segnale azzerato".
            var quantity = intent.FinalQuantity == intent.BaseQuantity
                ? intent.FinalQuantity.ToString("0.####")
                : $"{intent.FinalQuantity:0.####} (base {intent.BaseQuantity:0.####})";

            _log.LogInformation(
                "[{SessionId}] {Kind} {Strategy} {Symbol} {Side} {OrderType} @ {Price} qty {Quantity} -> {Status}{Reason}",
                sessionId, intent.Kind, intent.StrategyCode, intent.Symbol, intent.Side, intent.OrderType,
                intent.Price.ToString("0.#####"), quantity, intent.Status,
                string.IsNullOrWhiteSpace(intent.SizingReason) ? string.Empty : $" ({intent.SizingReason})");
        }
    }

    /// <summary>
    /// Esito del claim di un account. Il segnale consegnato si vede già in <see cref="LogIntents"/>
    /// come template, ma fra "template generato" e "ordine sul broker" c'è tutto il secondo layer di
    /// filtro (gruppi, slot, lucchetto per simbolo, conversione dell'account): è lì che un run può
    /// restare muto pur avendo prodotto i segnali, ed è l'unico punto in cui il motivo è noto.
    ///
    /// <para>Il rifiuto è loggato una sola volta per motivo, non a ogni poll: il cBot chiama questo
    /// endpoint a ogni barra e ogni pochi secondi, quindi ripeterlo sommergerebbe tutto il
    /// resto.</para>
    /// </summary>
    private void LogClaim(string sessionId, string accountNumber, AccountSignalResponse response)
    {
        if (response.Intent is { } intent)
        {
            _log.LogInformation(
                "[{SessionId}] claim {Account}: {Strategy} {Symbol} {Side} {OrderType} @ {Price} qty {Quantity}",
                sessionId, accountNumber, intent.StrategyCode, intent.Symbol, intent.Side,
                intent.OrderType, intent.Price.ToString("0.#####"), intent.FinalQuantity.ToString("0.####"));
            LastClaimRefusal.TryRemove($"{sessionId}|{accountNumber}", out _);
            return;
        }

        var detail = string.IsNullOrWhiteSpace(response.ReasonDetail) ? response.Reason : response.ReasonDetail;
        if (string.IsNullOrWhiteSpace(detail))
            return;

        var key = $"{sessionId}|{accountNumber}";
        if (LastClaimRefusal.TryGetValue(key, out var previous) && previous == detail)
            return;

        LastClaimRefusal[key] = detail;
        _log.LogInformation("[{SessionId}] claim {Account}: nessun intent — {Detail}",
            sessionId, accountNumber, detail);
    }

    /// <summary>
    /// Ultimo rifiuto di claim già stampato, per (sessione, account). È statico perché il controller
    /// è transiente: senza, la deduplica non sopravviverebbe alla singola richiesta. Contiene solo
    /// stringhe diagnostiche e non influenza nessuna decisione.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string>
        LastClaimRefusal = new();

    /// <summary>
    /// Storia ancora insufficiente per valutare. È a livello Debug e non Information perché ricorre a
    /// ogni barra finché il riscaldamento non è completo — per una strategia a 15 minuti sono 576
    /// righe — e sommergerebbe proprio i segnali che si sta cercando di vedere. La stessa
    /// informazione il cBot la stampa una volta sola per stream, quindi di default non si perde
    /// niente; qui serve quando si vuole seguire il riempimento barra per barra.
    /// </summary>
    private void LogStreams(string sessionId, IReadOnlyList<StreamHistoryStatus> streams)
    {
        if (!_log.IsEnabled(LogLevel.Debug))
            return;

        foreach (var stream in streams.Where(x => x.SkippedForInsufficientHistory > 0))
            _log.LogDebug(
                "[{SessionId}] {Symbol}/{Timeframe}m: {Skipped} strategie non valutate, storia {History}/{Required} candele.",
                sessionId, stream.Symbol, stream.TimeframeMinutes, stream.SkippedForInsufficientHistory,
                stream.HistoryBars, stream.RequiredCandles);
    }

    private ActionResult<T> ExecuteResult<T>(Func<ActionResult<T>> action)
    {
        try { return action(); }
        catch (UnauthorizedAccessException ex) { return ProblemResult<T>(401, "Session token non valido", ex.Message); }
        catch (KeyNotFoundException ex) { return ProblemResult<T>(404, "Risorsa non trovata", ex.Message); }
        catch (DirectoryNotFoundException ex) { return ProblemResult<T>(404, "Workspace non trovato", ex.Message); }
        // PRIMA di ArgumentException: ArgumentOutOfRangeException ne e' una sottoclasse, e finche'
        // ricadeva nello stesso catch un difetto del server (un indice negativo calcolato male)
        // usciva come "Richiesta non valida" 400. Il client non puo' correggere niente e chi legge
        // il suo log cerca l'errore dalla parte sbagliata. Vedi BugResult.
        catch (ArgumentOutOfRangeException ex) { return BugResult<T>(ex); }
        catch (ArgumentException ex) { return ProblemResult<T>(400, "Richiesta non valida", ex.Message); }
        catch (InvalidOperationException ex) { return ProblemResult<T>(409, "Operazione non consentita", ex.Message); }
        catch (Exception ex) when (IsServerDefect(ex)) { return BugResult<T>(ex); }
    }

    private IActionResult ExecuteAction(Func<IActionResult> action)
    {
        try { return action(); }
        catch (UnauthorizedAccessException ex) { return ProblemResult(401, "Session token non valido", ex.Message); }
        catch (KeyNotFoundException ex) { return ProblemResult(404, "Risorsa non trovata", ex.Message); }
        catch (ArgumentOutOfRangeException ex) { return BugResult(ex); }
        catch (ArgumentException ex) { return ProblemResult(400, "Richiesta non valida", ex.Message); }
        catch (InvalidOperationException ex) { return ProblemResult(409, "Operazione non consentita", ex.Message); }
        catch (Exception ex) when (IsServerDefect(ex)) { return BugResult(ex); }
    }

    /// <summary>
    /// Eccezioni che non descrivono mai una richiesta sbagliata: se arrivano qui, il difetto e'
    /// nel server. Sono elencate per tipo invece di catturare <see cref="Exception"/> perche' un
    /// catch-all nasconderebbe anche gli errori che il middleware deve poter vedere.
    /// </summary>
    private static bool IsServerDefect(Exception exception) =>
        exception is IndexOutOfRangeException or NullReferenceException or InvalidCastException
            or FormatException or OverflowException or InvalidDataException;

    /// <summary>
    /// Difetto del server, riportato come tale: 500, con tipo dell'eccezione e punto d'origine
    /// nel testo della risposta.
    ///
    /// <para><b>Perche' il dettaglio finisce nella risposta e non solo nel log.</b> Il client
    /// tipico e' un cBot che gira in backtest sulla macchina di qualcun altro: di questo scambio
    /// vede soltanto la riga che stampa nel proprio log. Un messaggio come
    /// <c>"Non-negative number required. (Parameter 'index')"</c> senza tipo ne' frame non dice da
    /// dove venga, e la diagnosi riparte dal cBot — cioe' dalla parte che non ha colpe.</para>
    /// </summary>
    private ActionResult<T> BugResult<T>(Exception exception)
    {
        LogDefect(exception);
        return StatusCode(500, BuildDefectProblem(exception));
    }

    /// <inheritdoc cref="BugResult{T}"/>
    private IActionResult BugResult(Exception exception)
    {
        LogDefect(exception);
        return StatusCode(500, BuildDefectProblem(exception));
    }

    private void LogDefect(Exception exception) =>
        _log.LogError(exception, "Difetto interno su {Method} {Path}",
            HttpContext?.Request.Method, HttpContext?.Request.Path.Value);

    private static ProblemDetails BuildDefectProblem(Exception exception) => new()
    {
        Status = 500,
        Title = "Errore interno del server",
        Detail = $"{exception.GetType().Name}: {exception.Message} | {DescribeOrigin(exception)}"
    };

    /// <summary>
    /// I frame piu' profondi dello stack, quelli che dicono davvero dove e' scoppiato. Sono tre e
    /// non tutto lo stack perche' la riga deve restare leggibile nel log di un cBot.
    /// </summary>
    private static string DescribeOrigin(Exception exception)
    {
        var stack = exception.StackTrace;
        if (string.IsNullOrWhiteSpace(stack))
            return "stack non disponibile";

        var frames = stack
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length != 0)
            .Take(3)
            .ToArray();

        return frames.Length == 0 ? "stack non disponibile" : string.Join(" <- ", frames);
    }

    private ActionResult<T> ProblemResult<T>(int status, string title, string detail)
        => StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });

    private IActionResult ProblemResult(int status, string title, string detail)
        => StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });
}
