using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.MarketData;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le barre nei giorni senza sessione non esistono per le strategie. La regola sta nel calendario
/// (<see cref="SessionGrid.DropNonSessionDays"/>) e la applicano allo stesso modo il backtest e la
/// sessione live.
///
/// <para><b>Il caso.</b> FTMO quota il DAX dalla domenica sera, un'ora prima dell'ancoraggio delle
/// 01:00: ne esce una barra 4h di domenica ogni settimana, che il future non ha e che il dossier
/// (§2.1.1) prescrive di scartare. Con un canale a una barra e un gate sulla sessione precedente,
/// il lunedì di <c>PT3B_FDAX_PCH_001_240</c> leggeva quella barra: −37.710 in un anno su FTMO contro
/// +25.215 in tre anni e mezzo sul future. Sui CME la domenica è una sessione vera in certe
/// settimane e il calendario di NQ la dichiara: lì non si toglie nulla.</para>
/// </summary>
public sealed class NonSessionDayBarsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-nosess-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // Domenica 07/09/2025 19:00Z = 21:00 Roma: la barra 4h con cui FTMO apre il DAX la domenica sera.
    private static readonly DateTime SundayEveningUtc = new(2025, 9, 7, 19, 0, 0, DateTimeKind.Utc);
    // Lunedì 08/09/2025 23:00Z = 01:00 Roma di lunedì: la prima barra vera della settimana.
    private static readonly DateTime MondayOpenUtc = new(2025, 9, 7, 23, 0, 0, DateTimeKind.Utc);
    // Venerdì 05/09/2025 19:00Z = 21:00 Roma: l'ultima barra del venerdì, che chiude sabato 01:00.
    private static readonly DateTime FridayEveningUtc = new(2025, 9, 5, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TheFdaxCalendarDropsSundayBarsAndKeepsTheRest()
    {
        var grid = new SessionGrid(MarketCalendarRegistry.Current.Get("@FDAX"));
        var bars = new[] { Bar(FridayEveningUtc), Bar(SundayEveningUtc), Bar(MondayOpenUtc) };

        var kept = grid.DropNonSessionDays(bars, out var dropped);

        Assert.Equal(1, dropped);
        Assert.Equal(new[] { FridayEveningUtc, MondayOpenUtc }, kept.Select(bar => bar.DateTime));
        Assert.False(grid.IsOnSessionDay(SundayEveningUtc));
        Assert.True(grid.IsOnSessionDay(FridayEveningUtc));
    }

    [Fact]
    public void TheNqCalendarDeclaresSundayAndKeepsIt()
    {
        var grid = new SessionGrid(MarketCalendarRegistry.Current.Get("@NQ"));
        var sunday = new DateTime(2025, 9, 7, 20, 0, 0, DateTimeKind.Utc); // 22:00 Roma di domenica

        var kept = grid.DropNonSessionDays([Bar(sunday), Bar(MondayOpenUtc)], out var dropped);

        Assert.Equal(0, dropped);
        Assert.Equal(2, kept.Length);
    }

    [Fact]
    public void ASessionIgnoresASundayBarButAcceptsTheDelivery()
    {
        var (sessions, d) = Session();

        var sunday = Push(sessions, d, SundayEveningUtc, sequence: 1);
        var monday = Push(sessions, d, MondayOpenUtc, sequence: 2);

        Assert.Equal(1, sunday.AcceptedBars);
        Assert.Equal(1, sunday.NonSessionBars);
        Assert.Equal(0, monday.NonSessionBars);

        // La storia dello stream contiene la sola barra di lunedì: la domenica non e' mai esistita.
        var stato = sessions.GetSnapshot(d.SessionId, d.SessionToken);
        Assert.NotNull(stato);
        Assert.Single(_evaluated);
        Assert.Equal(MondayOpenUtc, _evaluated[0]);
    }

    // ------------------------------------------------------------------------------ helper

    private readonly List<DateTime> _evaluated = [];

    private (TradingSessionService, TradingSessionDescriptor) Session()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"nosess-{Guid.NewGuid():N}", StrategiesFilter = ["PT3B_FDAX_PCH_001_240"]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        var conti = new List<TestAccountRow> { new("1001", MaxConcurrentTrades: 1) };
        TestAccountRegistry.Register(workspaces, conti);

        var sessions = new TradingSessionService(
            workspaces, new Registra(_evaluated), new PositionSizingService());
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

    private static PushBarsResponse Push(TradingSessionService sessions, TradingSessionDescriptor d, DateTime quando, long sequence) =>
        sessions.PushBars(new PushBarsRequest
        {
            SessionId = d.SessionId, SessionToken = d.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = "@FDAX", TimeframeMinutes = 240, BarTimeUtc = quando, Sequence = sequence,
                    IdempotencyKey = $"bar-{quando:O}", Bar = Bar(quando)
                }
            ]
        });

    private static OhlcvData Bar(DateTime quando) =>
        new() { DateTime = quando, Open = 24000, High = 24010, Low = 23990, Close = 24005, Volume = 1 };

    /// <summary>Registra le barre che la sessione ha davvero valutato, senza emettere nulla.</summary>
    private sealed class Registra(List<DateTime> evaluated) : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            evaluated.Add(closedBar.BarTimeUtc);
            return [];
        }
    }
}
