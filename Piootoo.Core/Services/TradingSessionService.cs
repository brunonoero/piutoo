using System.Collections.Concurrent;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
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
        signal.IsPositionCloseDependent = strategy.IsPositionCloseDependent;
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
    TradingSessionSnapshot ApplyReport(string sessionId, ExecutionReportRequest request);
    TradingSessionSnapshot GetSnapshot(string sessionId, string token);
    void CancelIntent(string sessionId, string token, string intentId);
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
        public Dictionary<string, TradingPositionSnapshot> ExternalPositions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, (DateTime EntryTimeUtc, string IntentId, decimal? StopLoss, decimal? TakeProfit)>
            ExternalPositionDetails { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<PersistedTrade> ExternalTrades { get; } = [];
        public int Entries { get; set; }
        public int Fills { get; set; }
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
            PositionSizing = request.PositionSizing,
            InstrumentMetadata = instrumentMetadata,
            PeakEquity = request.InitialCapital,
            Status = TradingSessionStatus.Created
        };
        _sessions[session.Id] = session;
        return Describe(session);
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
                if (!string.IsNullOrWhiteSpace(session.TitanoRunId))
                {
                    var service = _titano ?? throw new InvalidOperationException("Servizio Titano non disponibile.");
                    var effective = service.Resolve(session.WorkspaceId, session.TitanoBacktestFolder!,
                        session.TitanoRunId, bar.BarTimeUtc);
                    foreach (var state in effective.StrategyStates)
                        allocations[state.StrategyCode] = state.AllocationMultiplier;
                    evaluationStrategies = session.Strategies
                        .Where(x => effective.EffectiveStrategies.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
                }
                var signals = _evaluation.Evaluate(
                    evaluationStrategies,
                    normalizedBar,
                    history,
                    strategy => GetExecution(session, strategy, bar.BarTimeUtc));
                var sized = new Dictionary<TradeSignal, PositionSizingResult>();
                foreach (var signal in signals.Where(x => !x.CloseOnly))
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
                foreach (var signal in signals)
                {
                    if (signal.RuntimeState is not null)
                        session.SimulatedEngine.CaptureStrategyRuntimeState(
                            signal.StrategyCode, signal.Symbol, signal.RuntimeState);
                    var result = sized.GetValueOrDefault(signal);
                    var intent = AddIntent(session, signal, result);
                    if (result?.Reason is not null) intent.Status = OrderIntentStatus.Cancelled;
                    emitted.Add(intent);
                }

                var executableSignals = signals.Where(x => x.CloseOnly || x.Quantity > 0).ToList();
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
            if (delta > 0)
            {
                session.Fills++;
                var key = $"{intent.Symbol}|{intent.StrategyCode}";
                if (intent.CloseOnly)
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
                            TakeProfit = details.TakeProfit
                        });
                    }
                    session.ExternalPositions.Remove(key);
                    session.ExternalPositionDetails.Remove(key);
                }
                else
                {
                    if (!session.ExternalPositions.ContainsKey(key)) session.Entries++;
                    session.ExternalPositions[key] = new TradingPositionSnapshot
                    {
                        StrategyCode = intent.StrategyCode,
                        Symbol = intent.Symbol,
                        Direction = intent.Side,
                        Quantity = report.CumulativeFilledQuantity,
                        EntryPrice = report.FillPrice ?? intent.Price
                    };
                    session.ExternalPositionDetails[key] =
                        (report.EventTimeUtc, intent.IntentId, intent.StopLoss, intent.TakeProfit);
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

    private static OrderIntent AddIntent(Session session, TradeSignal signal, PositionSizingResult? sizing)
    {
        var intent = new OrderIntent
        {
            IntentId = $"{session.Id}-{session.Intents.Count + 1:D10}",
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
            CloseOnly = signal.CloseOnly,
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            ValidFromUtc = signal.ValidFromUtc,
            ExpiresAtUtc = signal.ExpiresAtUtc,
            CloseAtUtc = signal.CloseAtUtc,
            Reason = signal.Reason
        };
        session.Intents.Add(intent);
        return intent;
    }

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
            CloseOnly = intent.CloseOnly,
            Status = intent.Status,
            FilledQuantity = intent.FilledQuantity,
            ExternalOrderId = intent.ExternalOrderId
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
    }

    private static StrategyExecutionSnapshot GetExecution(Session session, ITradingStrategy strategy, DateTime time)
    {
        if (session.Mode == ExecutionMode.ServerSimulated)
            return session.SimulatedEngine.GetExecutionSnapshot(strategy.Name, strategy.Symbol, time);
        var key = $"{Normalize(strategy.Symbol)}|{strategy.Name}";
        session.ExternalPositions.TryGetValue(key, out var position);
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
                or OrderIntentStatus.Accepted or OrderIntentStatus.PartiallyFilled).ToArray()
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
