using System.Collections.Concurrent;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;

namespace Piootoo.Core.Services;

public interface IStrategyEvaluationService
{
    IReadOnlyList<TradeSignal> Evaluate(
        IReadOnlyList<ITradingStrategy> strategies,
        ClosedBar closedBar,
        IReadOnlyList<OhlcvData> history,
        Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot);
}

public sealed class StrategyEvaluationService : IStrategyEvaluationService
{
    public IReadOnlyList<TradeSignal> Evaluate(
        IReadOnlyList<ITradingStrategy> strategies,
        ClosedBar closedBar,
        IReadOnlyList<OhlcvData> history,
        Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
    {
        var result = new List<TradeSignal>();
        foreach (var strategy in strategies.Where(s =>
                     Normalize(s.Symbol) == Normalize(closedBar.Symbol) &&
                     s.TimeframeMinutes == closedBar.TimeframeMinutes))
        {
            if (history.Count < strategy.RequiredCandles)
                continue;

            var signal = strategy.Evaluate(new StrategyEvaluationRequest
            {
                Ohlcv = history.ToArray(),
                BarTimeUtc = closedBar.BarTimeUtc,
                Execution = executionSnapshot(strategy)
            });
            if (signal?.RuntimeState is not null)
                signal.StrategyCode = string.IsNullOrWhiteSpace(signal.StrategyCode) ? strategy.Name : signal.StrategyCode;
            if (signal is null || signal.Type == SignalType.Hold)
                continue;

            Prepare(signal, strategy, closedBar);
            result.Add(signal);
            if (signal.CompanionSignals is null) continue;
            foreach (var companion in signal.CompanionSignals)
            {
                Prepare(companion, strategy, closedBar);
                result.Add(companion);
            }
        }
        return result;
    }

    private static void Prepare(TradeSignal signal, ITradingStrategy strategy, ClosedBar bar)
    {
        signal.Date = bar.BarTimeUtc;
        signal.Symbol = string.IsNullOrWhiteSpace(signal.Symbol) ? Normalize(bar.Symbol) : Normalize(signal.Symbol);
        signal.StrategyCode = string.IsNullOrWhiteSpace(signal.StrategyCode) ? strategy.Name : signal.StrategyCode;
        signal.StrategyName = string.IsNullOrWhiteSpace(signal.StrategyName) ? strategy.Name : signal.StrategyName;
    }

    private static string Normalize(string value) => value.Trim().TrimStart('@').ToUpperInvariant();
}

public interface ITradingSessionService
{
    TradingSessionDescriptor Create(CreateTradingSessionRequest request);
    TradingSessionDescriptor SetStatus(string sessionId, string token, TradingSessionStatus status);
    PushBarsResponse PushBars(PushBarsRequest request);
    IReadOnlyList<OrderIntent> GetIntents(string sessionId, string token, long after = 0);
    IReadOnlyList<PersistedSignal> GetPersistedSignals(string sessionId, string token);
    IReadOnlyList<PersistedTrade> GetPersistedTrades(string sessionId, string token);

    /// <summary>
    /// Log diagnostico di rotazione (una riga per barra) per sessioni collegate a un run Titano: per
    /// ciascuna strategia del masterfilter riporta se è stata inclusa nella valutazione, lo stato/motivo
    /// Titano corrente e i segnali effettivamente generati. Pensato per verificare che le strategie
    /// eseguano (o non eseguano) trade coerentemente con la rotazione, e per individuare bug.
    /// </summary>
    IReadOnlyList<RotationLogEntry> GetRotationLog(string sessionId, string token);
    TradingSessionSnapshot ApplyReport(string sessionId, ExecutionReportRequest request);

    TradingSessionSnapshot GetSnapshot(string sessionId, string token);
    void CancelIntent(string sessionId, string token, string intentId);

    /// <summary>Configura (sostituendola interamente) la mappa account -> gruppo per l'anti copy-trading. Solo ExternalBroker.</summary>
    void SetAccountGroups(string sessionId, string token, IReadOnlyList<AccountGroupMapping> accounts);

    /// <summary>Legge la mappa account -> gruppo corrente.</summary>
    IReadOnlyList<AccountGroupMapping> GetAccountGroups(string sessionId, string token);

    /// <summary>
    /// Chiamata dal cBot di un singolo account: restituisce il prossimo segnale da eseguire (chiusura di
    /// una posizione già assegnata, oppure un nuovo ingresso libero nel gruppo, in ordine di priorità),
    /// oppure nessun segnale se l'account è già occupato o non c'è nulla di disponibile.
    /// </summary>
    AccountSignalResponse GetNextSignalForAccount(string sessionId, string token, string accountNumber);

    /// <summary>
    /// Registra un intent di chiusura (<see cref="OrderIntentKind.Close"/>) per una posizione che un
    /// client ExternalBroker ha già chiuso applicando la specifica di uscita ricevuta con l'intent di
    /// ingresso (SL/TP nativi, CloseAtUtc, MaxBarsInPosition). Richiede che la posizione
    /// StrategyCode/Symbol (eventualmente per account, se sono configurati gruppi) risulti aperta lato
    /// sessione. Il client referenzia l'IntentId restituito nel normale ApplyReport.
    /// </summary>
    OrderIntent CreateExternalCloseIntent(string sessionId, CreateExternalCloseIntentRequest request);
}

public sealed class TradingSessionService : ITradingSessionService
{
    private sealed class Session
    {
        public required string Id { get; init; }
        public required string Token { get; init; }
        public required string WorkspaceId { get; init; }
        public required ExecutionMode Mode { get; init; }
        public required decimal InitialCapital { get; init; }
        public required List<ITradingStrategy> Strategies { get; init; }
        public required PiootooTradingService SimulatedEngine { get; init; }
        public required TradingJsonStore Store { get; init; }
        public string? TitanoRunId { get; init; }
        public string? TitanoBacktestFolder { get; init; }
        public TitanoFilterMode TitanoMode { get; init; }
        public ClientRunMode ClientRunMode { get; init; }
        public required PositionSizingConfig PositionSizing { get; init; }
        public required Dictionary<string, InstrumentMetadata> InstrumentMetadata { get; init; }
        public decimal PeakEquity { get; set; }
        public TradingSessionStatus Status { get; set; }
        public object Gate { get; } = new();
        public Dictionary<string, List<OhlcvData>> History { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, long> LastSequence { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> BarKeys { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ReportIds { get; } = new(StringComparer.Ordinal);
        public List<OrderIntent> Intents { get; } = [];
        public List<RotationLogEntry> RotationLog { get; } = [];
        public Dictionary<string, TradingPositionSnapshot> ExternalPositions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, (DateTime EntryTimeUtc, string IntentId, decimal? StopLoss, decimal? TakeProfit)>
            ExternalPositionDetails { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<PersistedTrade> ExternalTrades { get; } = [];
        public int Entries { get; set; }
        public int Fills { get; set; }
        public DateTime? LastEvaluatedBarTimeUtc { get; set; }
        public int IntentSequence { get; set; }

        // --- Distribuzione multi-account / anti copy-trading (solo ExecutionMode.ExternalBroker) ---

        /// <summary>Mappa AccountNumber -> GroupId configurata dal tab Trading Session.</summary>
        public Dictionary<string, string> AccountGroups { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Template di segnali di apertura non ancora reclamati: ogni gruppo può reclamarne una copia indipendente.</summary>
        public List<OrderIntent> EntryTemplates { get; } = [];

        /// <summary>Per ogni template (IntentId), l'insieme dei gruppi che ne hanno già ricevuto una copia.</summary>
        public Dictionary<string, HashSet<string>> TemplateClaimedGroups { get; } = new(StringComparer.Ordinal);

        /// <summary>Slot occupato per (gruppo, strategia, simbolo): quale account lo detiene e con quale IntentId.</summary>
        public Dictionary<string, (string AccountNumber, string IntentId)> GroupStrategySlots { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Account -> IntentId dell'assegnazione attiva (ingresso in corso o posizione aperta). Autolimitazione lato server.</summary>
        public Dictionary<string, string> AccountActiveIntent { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Posizione "canonica" (Symbol|StrategyCode) usata per alimentare la valutazione strategie in modalità multi-account,
        /// indipendente da quale account specifico la detiene realmente.</summary>
        public Dictionary<string, TradingPositionSnapshot> CanonicalPositions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> StrategyHolderCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly WorkspaceService _workspaces;
    private readonly IStrategyEvaluationService _evaluation;
    private readonly TitanoRotationService? _titano;
    private readonly IPositionSizingService _positionSizing;

    public TradingSessionService(
        WorkspaceService workspaces, IStrategyEvaluationService evaluation,
        TitanoRotationService? titano = null, IPositionSizingService? positionSizing = null)
    {
        _workspaces = workspaces;
        _evaluation = evaluation;
        _titano = titano;
        _positionSizing = positionSizing ?? new PositionSizingService();
    }

    public TradingSessionDescriptor Create(CreateTradingSessionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.TitanoRunId) &&
            string.IsNullOrWhiteSpace(request.TitanoBacktestFolder))
            throw new ArgumentException("TitanoRunId richiede TitanoBacktestFolder.");

        // Le modalità filtrate non possono degradare in silenzio a "nessun filtro": senza rotazione
        // la sessione eseguirebbe tutto il masterfilter, cioè l'opposto di quanto richiesto.
        if (request.TitanoMode != TitanoFilterMode.Disabled && string.IsNullOrWhiteSpace(request.TitanoRunId))
            throw new ArgumentException(
                $"La modalità {request.TitanoMode} richiede TitanoRunId e TitanoBacktestFolder. " +
                "Usa TitanoFilterMode.Disabled per eseguire senza filtro Titano.");

        RequireCoherentRunMode(request.TitanoMode, request.ClientRunMode);

        var filter = _workspaces.GetMasterFilter(request.WorkspaceId);
        if (filter.StrategiesFilter.Count == 0)
            throw new ArgumentException("Il masterfilter del workspace è vuoto.");

        var definitions = StrategyFactory.GetRegisteredStrategies();
        var byId = definitions.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var invalid = filter.StrategiesFilter.Where(id => !byId.ContainsKey(id)).ToArray();
        if (invalid.Length != 0)
            throw new ArgumentException($"ID strategia non validi nel masterfilter: {string.Join(", ", invalid)}");

        var strategies = filter.StrategiesFilter.Select(id =>
        {
            var d = byId[id];
            return StrategyFactory.CreateStrategy(d.Id, d.Symbol, d.TimeframeMinutes, d.Parameters)
                   ?? throw new InvalidOperationException($"Impossibile creare la strategia '{id}'.");
        }).ToList();
        var suppliedMetadata = request.Instruments.ToDictionary(x => Normalize(x.Symbol), StringComparer.OrdinalIgnoreCase);
        foreach (var item in suppliedMetadata.Values)
            if (item.DollarsPerPoint <= 0 || item.MinimumQuantity <= 0 || item.QuantityStep <= 0)
                throw new ArgumentException($"Metadata non validi per {item.Symbol}.");
        var instrumentMetadata = strategies.Select(x => Normalize(x.Symbol)).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(symbol => symbol, symbol => suppliedMetadata.GetValueOrDefault(symbol) ?? new InstrumentMetadata
            {
                Symbol = symbol, DollarsPerPoint = 1m, MinimumQuantity = 1m,
                QuantityStep = 1m, RoundingMode = QuantityRoundingMode.FuturesContracts
            }, StringComparer.OrdinalIgnoreCase);

        var engine = new PiootooTradingService();
        engine.Initialize(request.InitialCapital, request.CommissionPerContract);
        var sessionId = Guid.NewGuid().ToString("N");
        var sessionDirectory = Path.Combine(_workspaces.GetWorkspacePath(request.WorkspaceId), "sessions", sessionId);
        var store = new TradingJsonStore(sessionDirectory);
        store.Initialize();
        var session = new Session
        {
            Id = sessionId,
            Token = string.IsNullOrWhiteSpace(request.ClientSessionToken)
                ? Convert.ToHexString(Guid.NewGuid().ToByteArray())
                : request.ClientSessionToken,
            WorkspaceId = request.WorkspaceId,
            Mode = request.ExecutionMode,
            InitialCapital = request.InitialCapital,
            Strategies = strategies,
            SimulatedEngine = engine,
            Store = store,
            TitanoRunId = request.TitanoRunId,
            TitanoBacktestFolder = request.TitanoBacktestFolder,
            TitanoMode = request.TitanoMode,
            ClientRunMode = request.ClientRunMode,
            PositionSizing = request.PositionSizing,
            InstrumentMetadata = instrumentMetadata,
            PeakEquity = request.InitialCapital,
            Status = TradingSessionStatus.Created
        };
        _sessions[session.Id] = session;
        return Describe(session);
    }

    /// <summary>
    /// Rifiuta le combinazioni modalità Titano / contesto di esecuzione che non possono essere
    /// corrette. Non è pignoleria: entrambe producono risultati plausibili ma sbagliati, e il primo
    /// segnale del problema arriverebbe dai numeri, non da un errore.
    ///
    /// Con <see cref="ClientRunMode.Unknown"/> non si verifica nulla: il client non ha dichiarato il
    /// contesto e inventarne uno sarebbe peggio che lasciare la responsabilità a chi configura.
    /// </summary>
    private static void RequireCoherentRunMode(TitanoFilterMode titanoMode, ClientRunMode runMode)
    {
        if (runMode == ClientRunMode.Unknown) return;

        if (titanoMode == TitanoFilterMode.Realtime && runMode == ClientRunMode.Backtest)
            throw new ArgumentException(
                "TitanoFilterMode.Realtime non è utilizzabile da un client in backtest: la rotazione " +
                "'corrente' verrebbe applicata a barre storiche e, oltre la fine del manifest, resterebbe " +
                "congelata sull'ultimo periodo calcolato — cioè look-ahead. Usa BacktestRotationFile per " +
                "filtrare con le rotazioni calcolate offline, oppure Disabled per non filtrare.");

        if (titanoMode == TitanoFilterMode.BacktestRotationFile && runMode == ClientRunMode.Realtime)
            throw new ArgumentException(
                "TitanoFilterMode.BacktestRotationFile non è utilizzabile in tempo reale: il manifest " +
                "copre l'intervallo del backtest da cui è stato generato, quindi il tempo live ne esce " +
                "quasi subito e la sessione si fermerebbe alla prima barra scoperta. Usa Realtime.");
    }

    public TradingSessionDescriptor SetStatus(string sessionId, string token, TradingSessionStatus status)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            session.Status = status;
            Persist(session);
            return Describe(session);
        }
    }

    public PushBarsResponse PushBars(PushBarsRequest request)
    {
        var session = Get(request.SessionId, request.SessionToken);
        lock (session.Gate)
        {
            if (session.Status != TradingSessionStatus.Running)
                throw new InvalidOperationException("La sessione non è in esecuzione.");

            var accepted = 0;
            var duplicates = 0;
            var emitted = new List<OrderIntent>();
            foreach (var bar in request.Bars)
            {
                ValidateBar(bar);
                if (!session.BarKeys.Add(bar.IdempotencyKey))
                {
                    duplicates++;
                    continue;
                }

                var stream = StreamKey(bar.Symbol, bar.TimeframeMinutes);
                if (session.LastSequence.TryGetValue(stream, out var last) && bar.Sequence <= last)
                {
                    session.BarKeys.Remove(bar.IdempotencyKey);
                    throw new ArgumentException($"Barra out-of-order per {stream}: sequence {bar.Sequence}, ultima {last}.");
                }
                session.LastSequence[stream] = bar.Sequence;
                accepted++;

                var normalizedBar = CloneUtc(bar);
                if (!session.History.TryGetValue(stream, out var history))
                    session.History[stream] = history = [];
                history.Add(normalizedBar.Bar);

                var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    { [Normalize(bar.Symbol)] = bar.Bar.Close };
                var bars = new Dictionary<string, OhlcvData>(StringComparer.OrdinalIgnoreCase)
                    { [Normalize(bar.Symbol)] = normalizedBar.Bar };

                // Ordering autorevole: prima exit/pending, poi valutazione, infine intent.
                if (session.Mode == ExecutionMode.ServerSimulated)
                    session.SimulatedEngine.UpdateMarketPrices(prices, bars, bar.BarTimeUtc);

                IReadOnlyList<ITradingStrategy> evaluationStrategies = session.Strategies;
                var allocations = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                TitanoEffectiveStrategies? effective = null;
                string? rotationNote = null;
                if (!string.IsNullOrWhiteSpace(session.TitanoRunId))
                {
                    var service = _titano ?? throw new InvalidOperationException("Servizio Titano non disponibile.");
                    effective = service.Resolve(session.WorkspaceId, session.TitanoBacktestFolder!,
                        session.TitanoRunId, bar.BarTimeUtc, session.TitanoMode);
                    foreach (var state in effective.StrategyStates)
                        allocations[state.StrategyCode] = state.AllocationMultiplier;

                    if (session.TitanoMode == TitanoFilterMode.Disabled)
                    {
                        // Rotazione risolta e registrata, ma non applicata: le allocazioni restano
                        // neutre e tutte le strategie del masterfilter vengono valutate. È il run che
                        // produce i trade su cui l'analisi Titano calcolerà le rotazioni.
                        allocations.Clear();
                        rotationNote = "modalità Disabled: rotazione risolta solo a scopo diagnostico, nessun filtro applicato";
                    }
                    else if (!effective.HasActivePeriod)
                    {
                        // Nessun periodo copre questa barra. In Realtime il fallback sull'ultimo periodo
                        // è già stato tentato dentro Resolve, quindi qui siamo davvero scoperti: è un
                        // manifest non allineato all'intervallo che si sta eseguendo. Fermarsi è meglio
                        // che eseguire senza filtri una sessione che l'utente ha chiesto filtrata.
                        throw new InvalidOperationException(
                            $"Nessun periodo Titano copre la barra {bar.BarTimeUtc:O}: il manifest '{session.TitanoRunId}' " +
                            $"copre {effective.ManifestFromUtc:O} → {effective.ManifestToUtc:O}. " +
                            "Rigenera la rotazione su un backtest che copra questo intervallo, oppure " +
                            "esegui la sessione in modalità Disabled.");
                    }
                    else
                    {
                        evaluationStrategies = session.Strategies
                            .Where(x => effective.EffectiveStrategies.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                            .ToArray();

                        if (effective.UsedLatestPeriod)
                            rotationNote =
                                $"barra {bar.BarTimeUtc:O} oltre la fine del manifest ({effective.ManifestToUtc:O}): " +
                                "applicata la rotazione dell'ultimo periodo calcolato. Rigenera l'analisi Titano.";
                    }
                }
                var signals = _evaluation.Evaluate(
                    evaluationStrategies,
                    normalizedBar,
                    history,
                    strategy => GetExecution(session, strategy, bar.BarTimeUtc));

                if (effective is not null)
                    session.RotationLog.Add(BuildRotationLogEntry(
                        session, bar.BarTimeUtc, effective, evaluationStrategies, signals, rotationNote));
                var sized = new Dictionary<TradeSignal, PositionSizingResult>();
                foreach (var signal in signals)
                {
                    var multiplier = allocations.TryGetValue(signal.StrategyCode, out var value) ? value : 1m;
                    var snapshot = Snapshot(session);
                    session.PeakEquity = Math.Max(session.PeakEquity, snapshot.Equity);
                    var result = _positionSizing.Calculate(new PositionSizingRequest
                    {
                        BaseQuantity = signal.Quantity, StrategyEquityMultiplier = multiplier,
                        Instrument = session.InstrumentMetadata[Normalize(signal.Symbol)],
                        Config = session.PositionSizing, AvailableBars = history,
                        TimestampUtc = bar.BarTimeUtc, InitialCapital = session.InitialCapital,
                        Equity = snapshot.Equity, PeakEquity = session.PeakEquity,
                        GrossExposureFraction = session.InitialCapital <= 0 ? 1m :
                            session.ExternalPositions.Values.Sum(x => x.Quantity * x.EntryPrice) / session.InitialCapital
                    });
                    sized[signal] = result;
                    signal.Quantity = result.FinalQuantity;
                }
                session.LastEvaluatedBarTimeUtc = bar.BarTimeUtc;
                var multiAccount = session.AccountGroups.Count > 0;
                foreach (var signal in signals)
                {
                    if (signal.RuntimeState is not null)
                        session.SimulatedEngine.CaptureStrategyRuntimeState(
                            signal.StrategyCode, signal.Symbol, signal.RuntimeState);
                    var result = sized.GetValueOrDefault(signal);

                    // Ogni segnale è un ingresso: le uscite non passano più dal server, ogni intent
                    // porta con sé la propria specifica di chiusura e il client la esegue.
                    if (multiAccount)
                    {
                        // Template non assegnato: resta disponibile finché non viene reclamato da un
                        // account libero di un gruppo (vedi GetNextSignalForAccount).
                        var template = AddIntent(session, signal, result, addToIntents: false);
                        if (result?.Reason is not null) template.Status = OrderIntentStatus.Cancelled;
                        else session.EntryTemplates.Add(template);
                        emitted.Add(template);
                        continue;
                    }

                    var intent = AddIntent(session, signal, result);
                    if (result?.Reason is not null) intent.Status = OrderIntentStatus.Cancelled;
                    emitted.Add(intent);
                }

                var executableSignals = signals.Where(x => x.Quantity > 0).ToList();
                if (session.Mode == ExecutionMode.ServerSimulated && executableSignals.Count != 0)
                {
                    session.SimulatedEngine.ProcessSignals(executableSignals, prices, bars, bar.BarTimeUtc);
                    foreach (var intent in emitted.Where(i => i.Status == OrderIntentStatus.Pending))
                        intent.Status = OrderIntentStatus.Filled;
                }
            }
            Persist(session);
            return new PushBarsResponse { AcceptedBars = accepted, DuplicateBars = duplicates, Intents = emitted };
        }
    }

    public IReadOnlyList<OrderIntent> GetIntents(string sessionId, string token, long after = 0)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Intents.Skip((int)Math.Max(0, after)).ToArray();
    }

    public IReadOnlyList<PersistedSignal> GetPersistedSignals(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Store.ReadSignals();
    }

    public IReadOnlyList<PersistedTrade> GetPersistedTrades(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Store.ReadTrades();
    }

    public IReadOnlyList<RotationLogEntry> GetRotationLog(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return session.Store.ReadRotationLog();
    }

    public TradingSessionSnapshot ApplyReport(string sessionId, ExecutionReportRequest request)
    {
        var session = Get(sessionId, request.SessionToken);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("Gli execution report sono ammessi solo in ExternalBroker.");
            var report = request.Report;
            RequireUtc(report.EventTimeUtc, nameof(report.EventTimeUtc));
            if (!session.ReportIds.Add(report.ReportId))
                return Snapshot(session);
            var intent = session.Intents.SingleOrDefault(x => x.IntentId == report.IntentId)
                         ?? throw new KeyNotFoundException($"Intent '{report.IntentId}' non trovato.");
            if (report.CumulativeFilledQuantity < intent.FilledQuantity || report.CumulativeFilledQuantity > intent.Quantity)
                throw new ArgumentException("CumulativeFilledQuantity non valida.");

            var delta = report.CumulativeFilledQuantity - intent.FilledQuantity;
            intent.FilledQuantity = report.CumulativeFilledQuantity;
            intent.ExternalOrderId = report.ExternalOrderId ?? intent.ExternalOrderId;
            intent.Status = report.Status switch
            {
                ExecutionReportStatus.Accepted => OrderIntentStatus.Accepted,
                ExecutionReportStatus.PartiallyFilled => OrderIntentStatus.PartiallyFilled,
                ExecutionReportStatus.Filled => OrderIntentStatus.Filled,
                ExecutionReportStatus.Rejected => OrderIntentStatus.Rejected,
                _ => OrderIntentStatus.Cancelled
            };

            if (!intent.IsClose && intent.FilledQuantity == 0 &&
                intent.Status is OrderIntentStatus.Rejected or OrderIntentStatus.Cancelled &&
                intent.AssignedAccountNumber is { } rejectedAccount)
            {
                // Ingresso mai eseguito (rifiutato/annullato dal broker): libera subito lo slot di gruppo
                // e l'autolimitazione dell'account SU QUESTO SIMBOLO, altrimenti resterebbero bloccati per
                // sempre (l'account può comunque avere posizioni aperte in parallelo su altri simboli).
                session.AccountActiveIntent.Remove(ActiveIntentKey(rejectedAccount, intent.Symbol));
                if (session.AccountGroups.TryGetValue(rejectedAccount, out var freedGroupId))
                    session.GroupStrategySlots.Remove(SlotKey(freedGroupId, intent.StrategyCode, intent.Symbol));
            }

            if (delta > 0)
            {
                session.Fills++;
                var accountNumber = intent.AssignedAccountNumber;
                // Legacy (nessun gruppo configurato): chiave invariata rispetto al comportamento storico.
                // Multi-account: chiave per-account, così più account possono detenere indipendentemente
                // la stessa strategia/simbolo senza sovrascriversi a vicenda.
                var key = accountNumber is null
                    ? $"{intent.Symbol}|{intent.StrategyCode}"
                    : $"{accountNumber}|{intent.Symbol}|{intent.StrategyCode}";
                var canonicalKey = $"{intent.Symbol}|{intent.StrategyCode}";

                if (intent.IsClose)
                {
                    if (session.ExternalPositions.TryGetValue(key, out var position) &&
                        session.ExternalPositionDetails.TryGetValue(key, out var details))
                    {
                        var exitPrice = report.FillPrice ?? intent.Price;
                        var gross = position.Direction == SignalType.Buy
                            ? (exitPrice - position.EntryPrice) * delta
                            : (position.EntryPrice - exitPrice) * delta;
                        session.ExternalTrades.Add(new PersistedTrade
                        {
                            TradeId = report.ReportId,
                            OrderId = report.ExternalOrderId,
                            IntentId = intent.IntentId,
                            CorrelationId = details.IntentId,
                            SessionId = session.Id,
                            StrategyCode = intent.StrategyCode,
                            StrategyName = intent.StrategyCode,
                            Symbol = intent.Symbol,
                            Direction = position.Direction,
                            Quantity = delta,
                            EntryTimeUtc = details.EntryTimeUtc,
                            ExitTimeUtc = report.EventTimeUtc,
                            EntryPrice = position.EntryPrice,
                            ExitPrice = exitPrice,
                            ExitReason = "ExternalBrokerCloseFill",
                            GrossProfit = gross,
                            NetProfit = gross - report.Commission,
                            Commission = report.Commission,
                            StopLoss = details.StopLoss,
                            TakeProfit = details.TakeProfit,
                            AccountNumber = accountNumber
                        });
                    }
                    session.ExternalPositions.Remove(key);
                    session.ExternalPositionDetails.Remove(key);

                    if (accountNumber != null)
                    {
                        // Libera lo slot di gruppo e l'autolimitazione dell'account SU QUESTO SIMBOLO:
                        // torna disponibile per un nuovo ingresso su questo simbolo (le posizioni aperte
                        // su altri simboli dallo stesso account non sono influenzate).
                        session.AccountActiveIntent.Remove(ActiveIntentKey(accountNumber, intent.Symbol));
                        if (session.AccountGroups.TryGetValue(accountNumber, out var groupId))
                            session.GroupStrategySlots.Remove(SlotKey(groupId, intent.StrategyCode, intent.Symbol));

                        if (session.StrategyHolderCounts.TryGetValue(canonicalKey, out var count) && count > 0)
                        {
                            count--;
                            if (count <= 0)
                            {
                                session.StrategyHolderCounts.Remove(canonicalKey);
                                session.CanonicalPositions.Remove(canonicalKey);
                            }
                            else session.StrategyHolderCounts[canonicalKey] = count;
                        }
                    }
                }
                else
                {
                    if (!session.ExternalPositions.ContainsKey(key)) session.Entries++;
                    var snapshot = new TradingPositionSnapshot
                    {
                        StrategyCode = intent.StrategyCode,
                        Symbol = intent.Symbol,
                        Direction = intent.Side,
                        Quantity = report.CumulativeFilledQuantity,
                        EntryPrice = report.FillPrice ?? intent.Price,
                        AccountNumber = accountNumber ?? string.Empty
                    };
                    session.ExternalPositions[key] = snapshot;
                    session.ExternalPositionDetails[key] =
                        (report.EventTimeUtc, intent.IntentId, intent.StopLoss, intent.TakeProfit);

                    if (accountNumber != null)
                    {
                        var holders = session.StrategyHolderCounts.GetValueOrDefault(canonicalKey);
                        session.StrategyHolderCounts[canonicalKey] = holders + 1;
                        if (holders == 0)
                        {
                            // Primo holder in assoluto per questa strategia/simbolo: diventa il riferimento
                            // canonico usato dalla valutazione strategie (GetExecution).
                            session.CanonicalPositions[canonicalKey] = snapshot;
                        }
                    }
                }
            }
            Persist(session);
            return Snapshot(session);
        }
    }

    public TradingSessionSnapshot GetSnapshot(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate) return Snapshot(session);
    }

    public void CancelIntent(string sessionId, string token, string intentId)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            var intent = session.Intents.SingleOrDefault(x => x.IntentId == intentId)
                         ?? throw new KeyNotFoundException($"Intent '{intentId}' non trovato.");
            if (intent.Status is OrderIntentStatus.Filled or OrderIntentStatus.Rejected or OrderIntentStatus.Cancelled)
                throw new InvalidOperationException("L'intent non è cancellabile.");
            intent.Status = OrderIntentStatus.Cancelled;
            Persist(session);
        }
    }

    public void SetAccountGroups(string sessionId, string token, IReadOnlyList<AccountGroupMapping> accounts)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("I gruppi account sono configurabili solo per sessioni ExternalBroker.");
            if (accounts.Any(a => string.IsNullOrWhiteSpace(a.AccountNumber) || string.IsNullOrWhiteSpace(a.GroupId)))
                throw new ArgumentException("AccountNumber e GroupId sono obbligatori per ogni voce.");
            var duplicated = accounts.GroupBy(a => a.AccountNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicated != null)
                throw new ArgumentException($"Account '{duplicated.Key}' configurato più di una volta.");

            session.AccountGroups.Clear();
            foreach (var mapping in accounts)
                session.AccountGroups[mapping.AccountNumber.Trim()] = mapping.GroupId.Trim();
            Persist(session);
        }
    }

    public IReadOnlyList<AccountGroupMapping> GetAccountGroups(string sessionId, string token)
    {
        var session = Get(sessionId, token);
        lock (session.Gate)
            return session.AccountGroups
                .Select(kv => new AccountGroupMapping { AccountNumber = kv.Key, GroupId = kv.Value })
                .OrderBy(x => x.GroupId).ThenBy(x => x.AccountNumber).ToArray();
    }

    public AccountSignalResponse GetNextSignalForAccount(string sessionId, string token, string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("AccountNumber obbligatorio.");
        var session = Get(sessionId, token);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("La distribuzione multi-account è disponibile solo in modalità ExternalBroker.");
            if (session.Status != TradingSessionStatus.Running)
                return new AccountSignalResponse { Reason = "SessionNotRunning" };
            if (!session.AccountGroups.TryGetValue(accountNumber, out var groupId))
                throw new ArgumentException(
                    $"Account '{accountNumber}' non configurato per questa sessione. Aggiungilo nel tab Trading Session.");

            // 1) C'è già un intent concreto pendente assegnato a questo account (ingresso appena reclamato
            //    non ancora confermato, oppure una chiusura da eseguire)? Il poll è idempotente: lo ripropone.
            var assigned = session.Intents
                .Where(i => string.Equals(i.AssignedAccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase)
                            && i.Status == OrderIntentStatus.Pending)
                .OrderBy(i => i.CreatedAtUtc)
                .FirstOrDefault();
            if (assigned != null)
                return new AccountSignalResponse { Intent = assigned };

            // 2) Autolimitazione lato server: un segnale alla volta PER SIMBOLO (l'account può gestire in
            //    parallelo posizioni su simboli diversi, mai due ingressi sullo stesso simbolo insieme).
            //    Scartiamo quindi solo i template dei simboli su cui l'account è già occupato.
            var now = DateTime.UtcNow;
            var priorities = ComputeStrategyPriority(session);
            var template = session.EntryTemplates
                .Where(t => t.Status == OrderIntentStatus.Pending)
                .Where(t => !t.ExpiresAtUtc.HasValue || t.ExpiresAtUtc.Value >= now)
                .Where(t => !(session.TemplateClaimedGroups.TryGetValue(t.IntentId, out var claimed)
                              && claimed.Contains(groupId)))
                .Where(t => !session.GroupStrategySlots.ContainsKey(SlotKey(groupId, t.StrategyCode, t.Symbol)))
                .Where(t => !session.AccountActiveIntent.ContainsKey(ActiveIntentKey(accountNumber, t.Symbol)))
                .OrderByDescending(t => priorities.GetValueOrDefault(t.StrategyCode, 0m))
                .ThenBy(t => t.CreatedAtUtc)
                .FirstOrDefault();

            if (template is null)
                return new AccountSignalResponse { Reason = "NoSignal" };

            var claim = CloneForClaim(template, accountNumber, groupId);
            session.Intents.Add(claim);
            if (!session.TemplateClaimedGroups.TryGetValue(template.IntentId, out var claimedGroups))
                session.TemplateClaimedGroups[template.IntentId] = claimedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            claimedGroups.Add(groupId);
            session.GroupStrategySlots[SlotKey(groupId, claim.StrategyCode, claim.Symbol)] = (accountNumber, claim.IntentId);
            session.AccountActiveIntent[ActiveIntentKey(accountNumber, claim.Symbol)] = claim.IntentId;
            Persist(session);
            return new AccountSignalResponse { Intent = claim };
        }
    }

    public OrderIntent CreateExternalCloseIntent(string sessionId, CreateExternalCloseIntentRequest request)
    {
        var session = Get(sessionId, request.SessionToken);
        lock (session.Gate)
        {
            if (session.Mode != ExecutionMode.ExternalBroker)
                throw new InvalidOperationException("Gli intent di chiusura esterni sono ammessi solo in ExternalBroker.");
            if (string.IsNullOrWhiteSpace(request.StrategyCode) || string.IsNullOrWhiteSpace(request.Symbol))
                throw new ArgumentException("StrategyCode e Symbol sono obbligatori.");

            var symbol = Normalize(request.Symbol);
            var accountNumber = string.IsNullOrWhiteSpace(request.AccountNumber) ? null : request.AccountNumber.Trim();
            if (session.AccountGroups.Count > 0 && accountNumber is null)
                throw new ArgumentException("AccountNumber obbligatorio quando la sessione ha gruppi account configurati.");

            var key = accountNumber is null
                ? $"{symbol}|{request.StrategyCode}"
                : $"{accountNumber}|{symbol}|{request.StrategyCode}";
            if (!session.ExternalPositions.TryGetValue(key, out var position))
                throw new KeyNotFoundException($"Nessuna posizione aperta per '{key}'.");

            var quantity = request.Quantity > 0 ? Math.Min(request.Quantity, position.Quantity) : position.Quantity;

            session.IntentSequence++;
            var intent = new OrderIntent
            {
                IntentId = $"{session.Id}-{session.IntentSequence:D10}",
                SessionId = session.Id,
                StrategyCode = request.StrategyCode,
                StrategyName = request.StrategyCode,
                Symbol = symbol,
                CreatedAtUtc = DateTime.UtcNow,
                Side = position.Direction,
                OrderType = TradeOrderType.Market,
                Quantity = quantity,
                BaseQuantity = quantity,
                FinalQuantity = quantity,
                Price = position.EntryPrice,
                Kind = OrderIntentKind.Close,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? "ClientLocalExit" : request.Reason,
                AssignedAccountNumber = accountNumber,
                Status = OrderIntentStatus.Pending
            };
            session.Intents.Add(intent);
            Persist(session);
            return intent;
        }
    }

    /// <summary>
    /// Priorità per strategia usata per decidere quale segnale offrire per primo quando un account libero
    /// ha più template di ingresso disponibili in contemporanea: usa il ranking Titano se la sessione è
    /// collegata a una rotazione, altrimenti il PnL netto live accumulato dalla strategia nella sessione.
    /// </summary>
    private Dictionary<string, decimal> ComputeStrategyPriority(Session session)
    {
        if (!string.IsNullOrWhiteSpace(session.TitanoRunId) && _titano != null)
        {
            try
            {
                var effective = _titano.Resolve(
                    session.WorkspaceId, session.TitanoBacktestFolder!, session.TitanoRunId!,
                    session.LastEvaluatedBarTimeUtc ?? DateTime.UtcNow, session.TitanoMode);
                var map = effective.StrategyStates.ToDictionary(
                    s => s.StrategyCode, s => s.AllocationMultiplier, StringComparer.OrdinalIgnoreCase);
                if (map.Count > 0) return map;
            }
            catch (Exception)
            {
                // Rotazione non risolvibile (es. dati mancanti): fallback sul PnL live sotto.
            }
        }

        var pnl = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var trade in session.ExternalTrades)
            pnl[trade.StrategyCode] = pnl.GetValueOrDefault(trade.StrategyCode) + trade.NetProfit;
        return pnl;
    }

    private static OrderIntent AddIntent(
        Session session, TradeSignal signal, PositionSizingResult? sizing, bool addToIntents = true)
    {
        session.IntentSequence++;
        var intent = new OrderIntent
        {
            IntentId = $"{session.Id}-{session.IntentSequence:D10}",
            SessionId = session.Id,
            StrategyCode = signal.StrategyCode,
            StrategyName = signal.StrategyName,
            Symbol = Normalize(signal.Symbol),
            CreatedAtUtc = signal.Date,
            Side = signal.Type,
            OrderType = signal.OrderType,
            Quantity = signal.Quantity,
            AllocationMultiplier = sizing?.StrategyEquityMultiplier ?? 1m,
            BaseQuantity = sizing?.BaseQuantity ?? signal.Quantity,
            StrategyEquityMultiplier = sizing?.StrategyEquityMultiplier ?? 1m,
            MarketVolatilityMultiplier = sizing?.MarketVolatilityMultiplier ?? 1m,
            PortfolioRiskMultiplier = sizing?.PortfolioRiskMultiplier ?? 1m,
            FinalQuantity = sizing?.FinalQuantity ?? signal.Quantity,
            SizingReason = sizing?.Reason,
            Price = signal.Price,
            Kind = OrderIntentKind.Entry,
            // Specifica di uscita completa: e' l'unica cosa con cui il client chiudera' la posizione.
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            BreakEven = signal.BreakEven,
            MaxBarsInPosition = signal.MaxBarsInPosition,
            ValidFromUtc = signal.ValidFromUtc,
            ExpiresAtUtc = signal.ExpiresAtUtc,
            CloseAtUtc = signal.CloseAtUtc,
            Reason = signal.Reason
        };
        if (addToIntents) session.Intents.Add(intent);
        return intent;
    }

    /// <summary>Clona un template di ingresso in un intent concreto assegnato a un account/gruppo specifico.</summary>
    private static OrderIntent CloneForClaim(OrderIntent template, string accountNumber, string groupId) => new()
    {
        IntentId = $"{template.IntentId}::{groupId}",
        SessionId = template.SessionId,
        StrategyCode = template.StrategyCode,
        StrategyName = template.StrategyName,
        Symbol = template.Symbol,
        CreatedAtUtc = template.CreatedAtUtc,
        Side = template.Side,
        OrderType = template.OrderType,
        Quantity = template.Quantity,
        AllocationMultiplier = template.AllocationMultiplier,
        BaseQuantity = template.BaseQuantity,
        StrategyEquityMultiplier = template.StrategyEquityMultiplier,
        MarketVolatilityMultiplier = template.MarketVolatilityMultiplier,
        PortfolioRiskMultiplier = template.PortfolioRiskMultiplier,
        FinalQuantity = template.FinalQuantity,
        SizingReason = template.SizingReason,
        Price = template.Price,
        Kind = OrderIntentKind.Entry,
        StopLoss = template.StopLoss,
        TakeProfit = template.TakeProfit,
        BreakEven = template.BreakEven,
        MaxBarsInPosition = template.MaxBarsInPosition,
        ValidFromUtc = template.ValidFromUtc,
        ExpiresAtUtc = template.ExpiresAtUtc,
        CloseAtUtc = template.CloseAtUtc,
        Reason = template.Reason,
        AssignedAccountNumber = accountNumber,
        AssignedGroupId = groupId
    };

    private static string SlotKey(string groupId, string strategyCode, string symbol) =>
        $"{groupId}|{strategyCode}|{Normalize(symbol)}";

    /// <summary>Chiave per l'autolimitazione "un segnale alla volta PER SIMBOLO" di un account.</summary>
    private static string ActiveIntentKey(string accountNumber, string symbol) =>
        $"{accountNumber}|{Normalize(symbol)}";

    private static void Persist(Session session)
    {
        session.Store.WriteSignals(session.Intents.Select(intent => new PersistedSignal
        {
            SignalId = intent.IntentId,
            IntentId = intent.IntentId,
            CorrelationId = intent.IntentId,
            SessionId = session.Id,
            TimestampUtc = intent.CreatedAtUtc,
            StrategyCode = intent.StrategyCode,
            StrategyName = string.IsNullOrWhiteSpace(intent.StrategyName)
                ? intent.StrategyCode
                : intent.StrategyName,
            Symbol = intent.Symbol,
            Side = intent.Side,
            OrderType = intent.OrderType,
            TriggerPrice = intent.Price,
            Quantity = intent.Quantity,
            BaseQuantity = intent.BaseQuantity,
            StrategyEquityMultiplier = intent.StrategyEquityMultiplier,
            MarketVolatilityMultiplier = intent.MarketVolatilityMultiplier,
            PortfolioRiskMultiplier = intent.PortfolioRiskMultiplier,
            FinalQuantity = intent.FinalQuantity,
            SizingReason = intent.SizingReason,
            ValidFromUtc = intent.ValidFromUtc,
            ExpiresAtUtc = intent.ExpiresAtUtc,
            StopLoss = intent.StopLoss,
            TakeProfit = intent.TakeProfit,
            TimeExitUtc = intent.CloseAtUtc,
            Reason = intent.Reason,
            MaxBarsInPosition = intent.MaxBarsInPosition,
            IsClose = intent.IsClose,
            Status = intent.Status,
            FilledQuantity = intent.FilledQuantity,
            ExternalOrderId = intent.ExternalOrderId,
            AssignedAccountNumber = intent.AssignedAccountNumber,
            AssignedGroupId = intent.AssignedGroupId
        }));

        var trades = session.Mode == ExecutionMode.ExternalBroker
            ? session.ExternalTrades
            : session.SimulatedEngine.GetClosedTrades().Select((trade, index) => new PersistedTrade
            {
                TradeId = $"{session.Id}-trade-{index + 1:D10}",
                SessionId = session.Id,
                CorrelationId = session.Id,
                StrategyCode = trade.StrategyCode,
                StrategyName = trade.StrategyName,
                Symbol = trade.Symbol,
                Direction = trade.Direction,
                Quantity = trade.Quantity,
                EntryTimeUtc = TradingDateTime.ToFeedUtc(trade.EntryDate),
                ExitTimeUtc = TradingDateTime.ToFeedUtc(trade.ExitDate),
                EntryPrice = trade.EntryPrice,
                ExitPrice = trade.ExitPrice,
                GrossProfit = trade.GrossProfit,
                NetProfit = trade.NetProfit,
                Commission = trade.Commission
            }).ToList();
        session.Store.WriteTrades(trades);
        session.Store.WriteRotationLog(session.RotationLog);
    }

    /// <summary>
    /// Costruisce la riga di log diagnostico per la barra corrente, incrociando lo stato Titano
    /// (chi è stato incluso/escluso e perché) con i segnali effettivamente generati dalle strategie
    /// valutate. Serve a verificare che le esclusioni Titano corrispondano a strategie che non hanno
    /// generato trade, e viceversa che le strategie incluse si comportino come progettato.
    /// </summary>
    private static RotationLogEntry BuildRotationLogEntry(
        Session session, DateTime barTimeUtc, TitanoEffectiveStrategies effective,
        IReadOnlyList<ITradingStrategy> evaluationStrategies, IReadOnlyList<TradeSignal> signals,
        string? note = null)
    {
        var masterStrategies = session.Strategies.Select(s => s.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
        var evaluatedNames = evaluationStrategies.Select(s => s.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
        var skipped = masterStrategies
            .Where(x => !evaluatedNames.Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var statesByCode = effective.StrategyStates.ToDictionary(x => x.StrategyCode, StringComparer.OrdinalIgnoreCase);
        var strategyStates = masterStrategies.Select(code =>
        {
            statesByCode.TryGetValue(code, out var state);
            return new RotationStrategyState
            {
                StrategyCode = code,
                Included = evaluatedNames.Contains(code, StringComparer.OrdinalIgnoreCase),
                AllocationMultiplier = state?.AllocationMultiplier ?? 0m,
                State = state?.State.ToString(),
                HardStopped = state?.HardStopped ?? false,
                CooldownRemaining = state?.CooldownRemaining ?? 0,
                Score = state?.Score ?? 0m,
                PassingFilters = state?.PassingFilters ?? 0,
                TotalFilters = state?.TotalFilters ?? 0,
                Reason = state?.Reason ?? "strategia assente dal run Titano corrente"
            };
        }).ToArray();

        return new RotationLogEntry
        {
            EntryId = $"{session.Id}-{barTimeUtc:yyyyMMddTHHmmssfffZ}",
            SessionId = session.Id,
            BarTimeUtc = barTimeUtc,
            TitanoRunId = session.TitanoRunId,
            TitanoBacktestFolder = session.TitanoBacktestFolder,
            PeriodId = effective.PeriodId,
            MasterStrategies = masterStrategies,
            EvaluatedStrategies = evaluatedNames,
            SkippedByTitano = skipped,
            StrategyStates = strategyStates,
            SignalsEmitted = signals.Select(s => $"{s.StrategyCode}:{s.Type}").ToArray(),
            FiltersApplied = session.TitanoMode != TitanoFilterMode.Disabled && effective.HasActivePeriod,
            TitanoMode = session.TitanoMode,
            ClientRunMode = session.ClientRunMode,
            Note = note
        };
    }

    private static StrategyExecutionSnapshot GetExecution(Session session, ITradingStrategy strategy, DateTime time)
    {
        if (session.Mode == ExecutionMode.ServerSimulated)
            return session.SimulatedEngine.GetExecutionSnapshot(strategy.Name, strategy.Symbol, time);
        var key = $"{Normalize(strategy.Symbol)}|{strategy.Name}";
        // In modalità multi-account la valutazione strategie usa la posizione "canonica" (indipendente
        // da quale account la detiene realmente); in modalità legacy usa le posizioni dirette come prima.
        var positions = session.AccountGroups.Count > 0 ? session.CanonicalPositions : session.ExternalPositions;
        positions.TryGetValue(key, out var position);
        return new StrategyExecutionSnapshot
        {
            StrategyCode = strategy.Name,
            Symbol = Normalize(strategy.Symbol),
            BarTimeUtc = time,
            EntriesToday = session.Entries,
            Position = position is null ? null : new StrategyPositionSnapshot
            {
                Direction = position.Direction,
                EntryPrice = position.EntryPrice,
                EntryTimeUtc = time,
                Contracts = (int)position.Quantity
            }
        };
    }

    private Session Get(string id, string token)
    {
        if (!_sessions.TryGetValue(id, out var session)) throw new KeyNotFoundException($"Sessione '{id}' non trovata.");
        if (!CryptographicEquals(session.Token, token)) throw new UnauthorizedAccessException("Session token non valido.");
        return session;
    }

    private static bool CryptographicEquals(string left, string right) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(left),
            System.Text.Encoding.UTF8.GetBytes(right ?? string.Empty));

    private static TradingSessionDescriptor Describe(Session session) => new()
    {
        SessionId = session.Id,
        SessionToken = session.Token,
        WorkspaceId = session.WorkspaceId,
        ExecutionMode = session.Mode,
        Status = session.Status,
        TitanoRunId = session.TitanoRunId,
        TitanoMode = session.TitanoMode,
        ClientRunMode = session.ClientRunMode,
        PositionSizing = session.PositionSizing,
        InstrumentMetadata = session.InstrumentMetadata.Values.OrderBy(x => x.Symbol).ToArray(),
        Instruments = session.Strategies.GroupBy(s => Normalize(s.Symbol))
            .Select(g => new TradingInstrument
            {
                Symbol = g.Key,
                TimeframesMinutes = g.Select(x => x.TimeframeMinutes).Distinct().Order().ToArray()
            }).ToArray()
    };

    private static TradingSessionSnapshot Snapshot(Session session)
    {
        var simulation = session.SimulatedEngine.GetSnapshot();
        return new TradingSessionSnapshot
        {
            SessionId = session.Id,
            ExecutionMode = session.Mode,
            Status = session.Status,
            Balance = session.Mode == ExecutionMode.ServerSimulated ? simulation.Balance : session.InitialCapital,
            Equity = session.Mode == ExecutionMode.ServerSimulated ? simulation.Equity : session.InitialCapital,
            Entries = session.Mode == ExecutionMode.ExternalBroker ? session.Entries : 0,
            Fills = session.Mode == ExecutionMode.ExternalBroker ? session.Fills : 0,
            Positions = session.ExternalPositions.Values.ToArray(),
            PendingIntents = session.Intents.Where(x => x.Status is OrderIntentStatus.Pending
                or OrderIntentStatus.Accepted or OrderIntentStatus.PartiallyFilled).ToArray(),
            AccountGroups = session.AccountGroups
                .Select(kv => new AccountGroupMapping { AccountNumber = kv.Key, GroupId = kv.Value })
                .OrderBy(x => x.GroupId).ThenBy(x => x.AccountNumber).ToArray()
        };
    }

    private static ClosedBar CloneUtc(ClosedBar bar)
    {
        bar.Bar.DateTime = bar.BarTimeUtc;
        return bar;
    }

    private static void ValidateBar(ClosedBar bar)
    {
        if (string.IsNullOrWhiteSpace(bar.Symbol) || bar.Symbol.Contains('/') || bar.Symbol.Contains('\\'))
            throw new ArgumentException("Symbol non valido.");
        if (bar.TimeframeMinutes <= 0 || bar.Sequence < 0 || string.IsNullOrWhiteSpace(bar.IdempotencyKey))
            throw new ArgumentException("Timeframe, sequence e idempotency key sono obbligatori.");
        RequireUtc(bar.BarTimeUtc, nameof(bar.BarTimeUtc));
    }

    private static void RequireUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc) throw new ArgumentException($"{name} deve essere UTC.");
    }

    private static string StreamKey(string symbol, int timeframe) => $"{Normalize(symbol)}|{timeframe}";
    private static string Normalize(string value) => value.Trim().TrimStart('@').ToUpperInvariant();
}
