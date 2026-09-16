using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il rollover visto dal claim: un template dello stesso verso, valido dall'istante in cui la
/// posizione aperta della stessa strategia scade (<c>CloseAtUtc</c> dell'intent che l'ha aperta),
/// non e' «un ingresso in corso» e va consegnato. E' il ciclo settimanale del BIASW con uscita e
/// ingresso sulla stessa barra: il motore interno esce e rientra alla stessa apertura dal
/// 16/09/2026, e senza questa eccezione il vivo entrava una settimana si' e una no.
///
/// <para>Il controllo opposto resta: una posizione che scade <b>dopo</b> l'istante di validita'
/// del template e' esposizione viva, e il template e' un doppione.</para>
/// </summary>
public sealed class RolloverClaimTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-roll-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ATemplateValidWhenThePositionExpiresIsClaimable()
    {
        // La posizione aperta sulla barra t0 scade a t0+30: e' l'istante di validita' del template
        // nato sulla barra t0+15.
        var (sessions, d) = Session(deadlineBars: 2);
        var t0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        PushBar(sessions, d, t0);
        var primo = Claim(sessions, d);
        Assert.NotNull(primo);
        Fill(sessions, d, primo!, t0.AddMinutes(15));

        PushBar(sessions, d, t0.AddMinutes(15));
        var rollover = Claim(sessions, d);

        Assert.NotNull(rollover);
        Assert.NotEqual(primo.IntentId, rollover!.IntentId);
        Assert.Equal(primo.Side, rollover.Side);
        Assert.Equal(t0.AddMinutes(30), rollover.ValidFromUtc);
        Assert.Equal(t0.AddMinutes(30), primo.CloseAtUtc);
    }

    [Fact]
    public void ATemplateValidBeforeThePositionExpiresIsStillRefused()
    {
        // La posizione scade a t0+45, il template e' valido da t0+30: esposizione viva, doppione.
        var (sessions, d) = Session(deadlineBars: 3);
        var t0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        PushBar(sessions, d, t0);
        var primo = Claim(sessions, d);
        Assert.NotNull(primo);
        Fill(sessions, d, primo!, t0.AddMinutes(15));

        PushBar(sessions, d, t0.AddMinutes(15));

        Assert.Null(Claim(sessions, d));
    }

    // ------------------------------------------------------------------------------ helper

    private (TradingSessionService, TradingSessionDescriptor) Session(int deadlineBars)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"roll-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        var conti = new List<TestAccountRow> { new("1001", MaxConcurrentTrades: 1) };
        TestAccountRegistry.Register(workspaces, conti);

        var sessions = new TradingSessionService(
            workspaces, new MarketOgniBarra(deadlineBars), new PositionSizingService());
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
        return (sessions, d);
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
                    BarTimeUtc = quando, Sequence = quando.Ticks,
                    IdempotencyKey = $"bar-{quando:O}",
                    Bar = new OhlcvData { DateTime = quando, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1 }
                }
            ]
        });
    }

    private static void Fill(TradingSessionService sessions, TradingSessionDescriptor d, OrderIntent intent, DateTime quando) =>
        sessions.ApplyReport(d.SessionId, new ExecutionReportRequest
        {
            SessionToken = d.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{intent.IntentId}", IntentId = intent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = intent.Quantity,
                FillPrice = 100m, EventTimeUtc = quando
            }
        });

    private static OrderIntent? Claim(TradingSessionService sessions, TradingSessionDescriptor d) =>
        sessions.GetNextSignalForAccount(d.SessionId, d.SessionToken, "1001").Intent;

    /// <summary>
    /// Un market "next bar" per barra, sempre long, con la deadline a <c>deadlineBars</c> barre
    /// dopo quella di segnale: e' la forma del BIASW, ridotta al minimo.
    /// </summary>
    private sealed class MarketOgniBarra(int deadlineBars) : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];
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
