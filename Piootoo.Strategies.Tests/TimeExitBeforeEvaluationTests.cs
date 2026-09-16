using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Una strategia intraday la cui barra chiude esattamente a fine sessione deve vedersi <b>flat</b>
/// quando quella barra viene valutata: la ricerca esce a fine barra e poi entra, e l'ordine per
/// la prima barra della sessione dopo nasce da lì.
///
/// <para><b>Il difetto misurato.</b> Il loop valutava la barra sul tick 00:59 e applicava la
/// deadline 00:59 nello stesso tick ma dopo: la strategia si vedeva in posizione e taceva. Sul
/// future 2022-2025, <c>PT2_FDAX_PCH_001_240</c>: 525 sessioni chiuse a fine sessione, zero segnali
/// sulla loro barra delle 21:00, contro 192 su 316 quando la posizione era già chiusa prima.</para>
/// </summary>
public sealed class TimeExitBeforeEvaluationTests : IDisposable
{
    private const string Code = "PTS_TEST_TE_240";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-te-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void TheEngineClosesADueDeadlineBeforeTheStrategyLooksAtItsPosition()
    {
        var service = new PiootooTradingService();
        service.Initialize(100_000m, commissionPerContract: 0m);

        var signalTime = new DateTime(2024, 1, 8, 6, 0, 0, DateTimeKind.Utc);
        var fillBar = signalTime.AddHours(4);
        var deadline = fillBar.AddHours(8);
        var level = 16_000m;

        service.ProcessSignals(
            [new TradeSignal
            {
                Date = signalTime, Type = SignalType.Buy, Price = level, Symbol = "@NQ",
                StrategyName = Code, StrategyCode = Code, Quantity = 1m,
                OrderType = TradeOrderType.Stop, ValidFromUtc = fillBar, ExpiresAtUtc = fillBar,
                TimeframeMinutes = 240, CloseAtUtc = deadline, Reason = "test"
            }],
            Prices(level - 10m), Bars(signalTime, level - 10m, level - 5m, level - 15m, level - 8m), signalTime);
        service.UpdateMarketPrices(Prices(level + 5m), Bars(fillBar, level - 2m, level + 5m, level - 3m, level + 2m), fillBar);
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", fillBar).Position);

        // Un minuto prima della deadline non chiude nulla.
        Assert.Equal(0, service.ApplyDueTimeExits(Prices(level + 3m), Bars(deadline.AddMinutes(-1), level + 3m, level + 4m, level + 2m, level + 3m), deadline.AddMinutes(-1)));
        Assert.NotNull(service.GetExecutionSnapshot(Code, "NQ", deadline.AddMinutes(-1)).Position);

        // Sul tick della deadline chiude PRIMA che la strategia guardi la posizione.
        Assert.Equal(1, service.ApplyDueTimeExits(Prices(level + 3m), Bars(deadline, level + 3m, level + 4m, level + 2m, level + 3m), deadline));
        Assert.Null(service.GetExecutionSnapshot(Code, "NQ", deadline).Position);
        var trade = Assert.Single(service.GetClosedTrades());
        Assert.Equal(TradeExitReason.TimeExit, trade.ExitReason);
        Assert.Equal(deadline, trade.ExitDate);

        // Il mark-to-market dello stesso tick non trova piu' nulla da chiudere.
        service.UpdateMarketPrices(Prices(level + 3m), Bars(deadline, level + 3m, level + 4m, level + 2m, level + 3m), deadline);
        Assert.Single(service.GetClosedTrades());
    }

    [Theory]
    [InlineData(2, true)]   // deadline alla chiusura della barra valutata: la strategia si vede flat
    [InlineData(3, false)]  // deadline una barra dopo: la strategia si vede ancora in posizione
    public void TheSessionShowsAPositionAsFlatWhenItsDeadlineFallsWithinTheEvaluatedBar(int deadlineBars, bool expectsFlat)
    {
        var (sessions, d, seen) = Session(deadlineBars);
        var t0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        var step = strategy.TimeframeMinutes;

        PushBar(sessions, d, t0);
        var primo = sessions.GetNextSignalForAccount(d.SessionId, d.SessionToken, "1001").Intent;
        Assert.NotNull(primo);
        sessions.ApplyReport(d.SessionId, new ExecutionReportRequest
        {
            SessionToken = d.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "r-1", IntentId = primo!.IntentId, Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = primo.Quantity, FillPrice = 100m, EventTimeUtc = t0.AddMinutes(step)
            }
        });

        // La barra t0+step chiude a t0+2·step: con deadlineBars = 2 la deadline e' esattamente li'.
        seen.Clear();
        PushBar(sessions, d, t0.AddMinutes(step));

        var snapshot = Assert.Single(seen);
        Assert.Equal(expectsFlat, snapshot.Position is null);
    }

    // ------------------------------------------------------------------------------ helper

    private (TradingSessionService, TradingSessionDescriptor, List<StrategyExecutionSnapshot>) Session(int deadlineBars)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"te-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        var conti = new List<TestAccountRow> { new("1001", MaxConcurrentTrades: 1) };
        TestAccountRegistry.Register(workspaces, conti);

        var seen = new List<StrategyExecutionSnapshot>();
        var sessions = new TradingSessionService(
            workspaces, new MarketConDeadline(deadlineBars, seen), new PositionSizingService());
        var d = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            EnforceConcurrencyLimits = false,
            MaxConcurrentTrades = TestSessionAccounts.MaxConcurrentTrades(conti),
            ConcurrencyCountMode = TestSessionAccounts.CountMode(conti)
        });
        sessions.SetSessionAccounts(d.SessionId, d.SessionToken, TestSessionAccounts.Numbers(conti));
        sessions.SetStatus(d.SessionId, d.SessionToken, TradingSessionStatus.Running);
        return (sessions, d, seen);
    }

    private static void PushBar(TradingSessionService sessions, TradingSessionDescriptor d, DateTime quando)
    {
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        sessions.PushBars(new PushBarsRequest
        {
            SessionId = d.SessionId, SessionToken = d.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = strategy.Symbol, TimeframeMinutes = strategy.TimeframeMinutes,
                    BarTimeUtc = quando, Sequence = quando.Ticks, IdempotencyKey = $"bar-{quando:O}",
                    Bar = new OhlcvData { DateTime = quando, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1 }
                }
            ]
        });
    }

    private static Dictionary<string, decimal> Prices(decimal price) =>
        new(StringComparer.OrdinalIgnoreCase) { ["NQ"] = price };

    private static Dictionary<string, OhlcvData> Bars(DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["NQ"] = new OhlcvData { DateTime = time, Open = open, High = high, Low = low, Close = close, Volume = 1 }
        };

    /// <summary>Un market "next bar" per barra con deadline a N barre, e la posizione che la strategia vede.</summary>
    private sealed class MarketConDeadline(int deadlineBars, List<StrategyExecutionSnapshot> seen) : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];
            seen.Add(executionSnapshot(strategy));
            var prossima = closedBar.BarTimeUtc.AddMinutes(strategy.TimeframeMinutes);
            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name, StrategyName = strategy.Name,
                    Symbol = strategy.Symbol, Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy, Quantity = 1m, Price = closedBar.Bar.Close,
                    OrderType = TradeOrderType.Market,
                    ValidFromUtc = prossima, ExpiresAtUtc = prossima,
                    CloseAtUtc = closedBar.BarTimeUtc.AddMinutes(strategy.TimeframeMinutes * deadlineBars)
                }
            ];
        }
    }
}
