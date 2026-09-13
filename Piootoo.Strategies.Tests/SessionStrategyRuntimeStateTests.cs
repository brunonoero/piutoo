using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La memoria di una strategia sopravvive da una barra all'altra anche in sessione
/// <c>ExternalBroker</c>, e anche quando la barra si chiude senza segnale.
///
/// <para><b>Il bug che questi test impediscono.</b> Due difetti sommati. Il server salvava lo stato
/// solo dai segnali restituiti, quindi mai sulle barre in <c>Hold</c>; e in <c>ExternalBroker</c>
/// <c>GetExecution</c> costruiva lo snapshot senza <c>RuntimeState</c>, quindi la strategia partiva
/// sempre da uno stato vuoto. Il backtest non aveva il problema perche' salva dopo ogni valutazione.</para>
///
/// <para>Misurato su <c>compare-0041</c>: <c>PTS_BTC_BIA_001_60</c> conta le barre della sessione in
/// <c>_mycount</c>, e in sessione il contatore restava a 1. Il cBot ha ricevuto un <c>Buy Stop</c> a
/// ogni ora del giorno (2.088 intent) e nessuno short, contro 102 short del backtest.</para>
/// </summary>
public sealed class SessionStrategyRuntimeStateTests : IDisposable
{
    private static readonly DateTime Day1 = new(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-runtimestate-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ExternalBroker_TheStrategyStateSurvivesBarsWithoutSignals()
    {
        var (sessions, descriptor, spy, strategy) = Session();

        for (var bar = 0; bar < 3; bar++)
            sessions.PushBars(Bars(descriptor, strategy, Day1.AddHours(bar)));

        // Ogni barra legge il contatore lasciato dalla precedente: 0, 1, 2. Con il difetto erano
        // tre zeri — nessun Hold salvato, e in ExternalBroker nessuno stato riletto.
        Assert.Equal([0, 1, 2], spy.Seen);
    }

    // ------------------------------------------------------------------------------ helper

    private (TradingSessionService Sessions,
             TradingSessionDescriptor Descriptor,
             CountingEvaluation Spy,
             StrategyDefinition Strategy) Session()
    {
        var strategy = StrategyFactory.GetRegisteredStrategies().OrderBy(s => s.Name).First();
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"runtimestate-{Guid.NewGuid():N}",
            StrategiesFilter = [strategy.Id]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        TestAccountRegistry.Register(workspaces, (IEnumerable<TestAccountRow>?)null);

        var spy = new CountingEvaluation();
        var sessions = new TradingSessionService(workspaces, spy, new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            MaxConcurrentTrades = TestSessionAccounts.MaxConcurrentTrades(null),
            ConcurrencyCountMode = TestSessionAccounts.CountMode(null)
        });
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
        return (sessions, descriptor, spy, strategy);
    }

    private static PushBarsRequest Bars(
        TradingSessionDescriptor descriptor, StrategyDefinition strategy, DateTime barTime) =>
        new()
        {
            SessionId = descriptor.SessionId,
            SessionToken = descriptor.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = strategy.Symbol,
                    TimeframeMinutes = strategy.TimeframeMinutes,
                    BarTimeUtc = barTime,
                    Sequence = barTime.Ticks,
                    IdempotencyKey = $"bar-{barTime:O}",
                    Bar = new OhlcvData
                    {
                        DateTime = barTime, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1
                    }
                }
            ]
        };

    /// <summary>
    /// Una strategia con un contatore: legge dallo snapshot quanto vale, lo incrementa, lo consegna
    /// come memoria e chiude la barra in <c>Hold</c>. Registra il valore letto a ogni barra.
    /// </summary>
    private sealed class CountingEvaluation : IStrategyEvaluationService
    {
        private const string CounterField = "_count";

        public List<int> Seen { get; } = [];

        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) =>
            throw new InvalidOperationException(
                "La sessione deve usare l'overload che consegna la memoria delle strategie.");

        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot,
            Action<ITradingStrategy, IReadOnlyDictionary<string, object?>> captureRuntimeState)
        {
            foreach (var strategy in strategies)
            {
                var state = executionSnapshot(strategy).RuntimeState;
                var count = state is not null && state.TryGetValue(CounterField, out var value) && value is int stored
                    ? stored
                    : 0;
                Seen.Add(count);
                captureRuntimeState(strategy, new Dictionary<string, object?> { [CounterField] = count + 1 });
            }

            return [];
        }
    }
}
