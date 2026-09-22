using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un template mai reclamato durante la propria barra non si consegna dopo.
///
/// <para><b>Il difetto</b> (compare-0043, PTS_NQ_TFU_003_15 del 03/12/2025). Mentre il conto e' in
/// posizione i template della strategia restano in lista non reclamati. Il claim li teneva validi finche'
/// <c>ExpiresAtUtc &gt;= LastEvaluatedBarTimeUtc</c>, cioe' per una barra oltre la loro, e fra due
/// template validi sceglieva il piu' VECCHIO. Chiusa la posizione, il cBot riceveva il template della
/// barra precedente, lo piazzava (log: «attesa -900s ... intent non allineato») e da li' in poi ogni
/// barra consegnava quello di una barra prima: 447 intent da 15 minuti in ritardo di una barra intera,
/// 29 trade nati cosi', uno fuori finestra operativa.</para>
///
/// <para><b>Il vincolo da non rompere</b>: attraverso un buco della serie il template resta valido,
/// perche' la barra che "next bar" nomina e' la prima che arriva davvero. Per questo la fine della
/// barra si misura sull'ultima barra dello stream del template, non sull'orologio.</para>
/// </summary>
public sealed class StaleTemplateClaimTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-stale-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ATemplateNotClaimedDuringItsBar_IsNotServedAfterIt()
    {
        var (sessions, d) = Session();
        var t0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        // Barra t0: template valido sulla barra t0+15. Nessuno lo reclama (il conto era in posizione).
        PushBar(sessions, d, t0);
        // Barra t0+15 chiusa: la barra di validita' del primo template e' finita.
        PushBar(sessions, d, t0.AddMinutes(15));

        var served = Claim(sessions, d);
        Assert.NotNull(served);
        Assert.Equal(t0.AddMinutes(30), served!.ValidFromUtc);
    }

    [Fact]
    public void ATemplateStaysClaimableUntilTheNextBarOfItsStreamArrives()
    {
        // Il caso del fine settimana: nessuna barra nuova dopo il segnale, per quanto tempo passi.
        var (sessions, d) = Session();
        var friday = new DateTime(2026, 1, 9, 21, 45, 0, DateTimeKind.Utc);

        PushBar(sessions, d, friday);

        var served = Claim(sessions, d);
        Assert.NotNull(served);
        Assert.Equal(friday.AddMinutes(15), served!.ValidFromUtc);
    }

    // ------------------------------------------------------------------------------ helper

    private (TradingSessionService, TradingSessionDescriptor) Session()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"stale-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        var conti = new List<TestAccountRow> { new("1001", MaxConcurrentTrades: 0) };
        TestAccountRegistry.Register(workspaces, conti);

        var sessions = new TradingSessionService(
            workspaces, new OneEntryPerBar(), new PositionSizingService());
        var d = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            EnforceConcurrencyLimits = false,
            MaxConcurrentTrades = TestSessionAccounts.MaxConcurrentTrades(conti),
            ConcurrencyCountMode = TestSessionAccounts.CountMode(conti),
            // Il caso del venerdi' sera vuole che l'ingresso ESISTA: con il piano di default, che
            // vieta l'overweek, un ingresso valido dentro la finestra del fine settimana non nasce
            // (HoldingResolver.BlocksEntry). Qui si misura la validita' attraverso un buco, non il
            // flat: la tenuta e' quella dei run di parita'.
            Holding = AccountHoldingPolicy.Unrestricted
        });
        sessions.SetSessionAccounts(d.SessionId, d.SessionToken, TestSessionAccounts.Numbers(conti));
        sessions.SetStatus(d.SessionId, d.SessionToken, TradingSessionStatus.Running);
        return (sessions, d);
    }

    private static void PushBar(TradingSessionService sessions, TradingSessionDescriptor d, DateTime when)
    {
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        sessions.PushBars(new PushBarsRequest
        {
            SessionId = d.SessionId, SessionToken = d.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = strategy.Symbol, TimeframeMinutes = 15,
                    BarTimeUtc = when, Sequence = when.Ticks,
                    IdempotencyKey = $"bar-{when:O}",
                    Bar = new OhlcvData { DateTime = when, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1 }
                }
            ]
        });
    }

    private static OrderIntent? Claim(TradingSessionService sessions, TradingSessionDescriptor d) =>
        sessions.GetNextSignalForAccount(d.SessionId, d.SessionToken, "1001").Intent;

    /// <summary>Un ingresso per barra a 15 minuti, valido solo per la barra successiva.</summary>
    private sealed class OneEntryPerBar : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];
            var next = closedBar.BarTimeUtc.AddMinutes(15);
            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name, StrategyName = strategy.Name,
                    Symbol = strategy.Symbol, Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy, Quantity = 4m, Price = closedBar.Bar.High + closedBar.BarTimeUtc.Minute,
                    OrderType = TradeOrderType.Stop, TimeframeMinutes = 15,
                    ValidFromUtc = next, ExpiresAtUtc = next
                }
            ];
        }
    }
}
