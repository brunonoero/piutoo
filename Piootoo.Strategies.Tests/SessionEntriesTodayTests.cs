using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// <c>EntriesToday</c> visto dalla strategia in <c>ExternalBroker</c>.
///
/// <para><b>Il bug che questi test impediscono.</b> <c>GetExecution</c> passava
/// <c>session.Entries</c>, che e' il totale della sessione: ogni riempimento di <b>qualunque</b>
/// strategia su <b>qualunque</b> simbolo, e mai azzerato. I motori con un tetto di ingressi —
/// <c>VolatilityBreakoutEngine</c>, <c>MovingAverageCrossoverEngine</c>,
/// <c>TrendDeveloperEngine</c> — leggono quel numero <i>prima</i> di emettere
/// (<c>EntriesTodayCount &gt;= MaxEntriesPerSession</c>), quindi si spegnevano tutti dopo il primo
/// riempimento del portafoglio e restavano muti per il resto del run.</para>
///
/// <para>Misurato su <c>piootoo-repository/compare/compare-0021</c>:
/// <c>PTS_FDAX_VBO_001_240</c> ha emesso <b>zero</b> intent in due mesi di sessione contro dodici
/// trade del backtest sullo stesso piano, il 57% del divario fra le due gambe. Il backtest non ha
/// mai avuto il problema perche' <c>PiootooTradingService._entriesByDay</c> conta per
/// <c>positionKey</c> e per giorno: e' quella la semantica da riprodurre.</para>
///
/// <para>Da non confondere con <see cref="SessionEntryLimitTests"/>: quello copre il rifiuto di un
/// intent <b>gia' emesso</b> (<c>session.EntryFills</c>, per secchio di sessione e per account).
/// Sono due conteggi distinti e servono a due momenti diversi.</para>
/// </summary>
public sealed class SessionEntriesTodayTests : IDisposable
{
    private static readonly DateTime Day1 = new(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-entriestoday-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void BeforeAnyFill_TheStrategyReadsZero()
    {
        var (sessions, descriptor, spy, strategies) = Session(strategyCount: 1);

        sessions.PushBars(Bars(descriptor, strategies, Day1));

        Assert.Equal(0, spy.Last(strategies[0].Name));
    }

    [Fact]
    public void AfterItsOwnFill_TheStrategyReadsOne()
    {
        var (sessions, descriptor, spy, strategies) = Session(strategyCount: 1);

        var intent = Assert.Single(sessions.PushBars(Bars(descriptor, strategies, Day1)).Intents);
        Fill(sessions, descriptor, intent, Day1.AddMinutes(1));

        sessions.PushBars(Bars(descriptor, strategies, Day1.AddHours(1)));

        Assert.Equal(1, spy.Last(strategies[0].Name));
    }

    [Fact]
    public void TheFillOfAnotherStrategyDoesNotRaiseTheCount()
    {
        // È la metà del bug che azzerava le strategie a tetto: il primo riempimento del
        // portafoglio, di chiunque fosse, spegneva tutte le altre.
        var (sessions, descriptor, spy, strategies) = Session(strategyCount: 2);

        var intents = sessions.PushBars(Bars(descriptor, strategies, Day1)).Intents;
        var first = intents.First(x => x.StrategyCode == strategies[0].Name);
        Fill(sessions, descriptor, first, Day1.AddMinutes(1));

        sessions.PushBars(Bars(descriptor, strategies, Day1.AddHours(1)));

        Assert.Equal(1, spy.Last(strategies[0].Name));
        Assert.Equal(0, spy.Last(strategies[1].Name));
    }

    [Fact]
    public void TheCountRestartsOnTheNextDay()
    {
        // L'altra metà: session.Entries non si azzerava mai, quindi il tetto valeva per sempre
        // invece che per la giornata.
        var (sessions, descriptor, spy, strategies) = Session(strategyCount: 1);

        var intent = Assert.Single(sessions.PushBars(Bars(descriptor, strategies, Day1)).Intents);
        Fill(sessions, descriptor, intent, Day1.AddMinutes(1));

        sessions.PushBars(Bars(descriptor, strategies, Day1.AddDays(1)));

        Assert.Equal(0, spy.Last(strategies[0].Name));
    }

    // ------------------------------------------------------------------------------ helper

    /// <summary>
    /// Strategie del catalogo che condividono stream: servono due strategie sulla stessa coppia
    /// (simbolo, timeframe) perché le barre spinte le raggiungano entrambe.
    /// </summary>
    private static IReadOnlyList<StrategyDefinition> SameStream(int count)
    {
        var group = StrategyFactory.GetRegisteredStrategies()
            .GroupBy(s => (s.Symbol, s.TimeframeMinutes))
            .First(g => g.Count() >= count);
        return group.OrderBy(s => s.Name).Take(count).ToList();
    }

    private (TradingSessionService Sessions,
             TradingSessionDescriptor Descriptor,
             SnapshotSpy Spy,
             IReadOnlyList<StrategyDefinition> Strategies) Session(int strategyCount)
    {
        var strategies = SameStream(strategyCount);
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"entriestoday-{Guid.NewGuid():N}",
            StrategiesFilter = strategies.Select(s => s.Id).ToList()
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        TestAccountRegistry.Register(workspaces, (IEnumerable<TestAccountRow>?)null);

        var spy = new SnapshotSpy();
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
        return (sessions, descriptor, spy, strategies);
    }

    private static void Fill(
        TradingSessionService sessions,
        TradingSessionDescriptor descriptor,
        OrderIntent intent,
        DateTime whenUtc) =>
        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{intent.IntentId}",
                IntentId = intent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = intent.Quantity,
                FillPrice = 100m,
                EventTimeUtc = whenUtc
            }
        });

    private static PushBarsRequest Bars(
        TradingSessionDescriptor descriptor,
        IReadOnlyList<StrategyDefinition> strategies,
        DateTime barTime) =>
        new()
        {
            SessionId = descriptor.SessionId,
            SessionToken = descriptor.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = strategies[0].Symbol,
                    TimeframeMinutes = strategies[0].TimeframeMinutes,
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
    /// Emette un ingresso per strategia e per barra, e <b>registra</b> l'<c>EntriesToday</c> che il
    /// server le ha mostrato. È l'unico modo di osservare quel numero: lo snapshot non compare in
    /// nessun artefatto, ed è esattamente per questo che il bug è passato inosservato.
    /// </summary>
    private sealed class SnapshotSpy : IStrategyEvaluationService
    {
        private readonly Dictionary<string, int> _last = new(StringComparer.OrdinalIgnoreCase);

        public int Last(string strategyName) =>
            _last.TryGetValue(strategyName, out var value)
                ? value
                : throw new InvalidOperationException(
                    $"La strategia '{strategyName}' non è mai stata valutata: il test non misura nulla.");

        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var signals = new List<TradeSignal>();
            foreach (var strategy in strategies)
            {
                _last[strategy.Name] = executionSnapshot(strategy).EntriesToday;
                signals.Add(new TradeSignal
                {
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Symbol = strategy.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy,
                    OrderType = TradeOrderType.Stop,
                    Quantity = 1m,
                    Price = closedBar.Bar.Close + 1m,
                    ValidFromUtc = closedBar.BarTimeUtc.AddMinutes(closedBar.TimeframeMinutes)
                });
            }
            return signals;
        }
    }
}
