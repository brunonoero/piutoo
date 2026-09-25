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
/// Una chiusura chiesta dalla strategia (ExitOnly) per una posizione che nel frattempo si e' chiusa
/// per un'altra via — target o stop del broker — non deve bloccare la sessione.
///
/// <para><b>Il difetto che chiude.</b> Il claim ripropone per prime, e sempre, le chiusure assegnate
/// al conto. Una chiusura rimasta orfana della sua posizione tornava quindi a ogni poll; il cBot,
/// che la teneva gia' in gestione, aspettava un esito che non sarebbe mai arrivato, e dietro di lei
/// nessun ingresso veniva piu' consegnato. Sul backtest cTrader del piano PT5DAV-S-INDICI (25/09/2026)
/// la MAC su ES chiede l'uscita il 12/09/2025 alle 16:00, la posizione si chiude sul target, e da
/// li' le 17 strategie non aprono piu' niente: 456 ingressi scaduti senza consegna, due run su due.
/// Lo stesso accadrebbe in live alla prima uscita di questo tipo.</para>
/// </summary>
public sealed class OrphanCloseIntentTests : IDisposable
{
    private const string Account = "1001";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-orphan-close-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ACloseWhosePositionClosedElsewhereDoesNotBlockTheNextEntry()
    {
        var (sessions, descriptor, strategy) = Session();

        // 1) Ingresso riempito: posizione aperta sul server.
        sessions.PushBars(Bars(descriptor, strategy, Day(5)));
        var entry = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, Account).Intent;
        Assert.NotNull(entry);
        Report(sessions, descriptor, entry!, 100m, Day(5));
        Assert.Single(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);

        // 2) La strategia chiede di uscire: il server crea la chiusura, assegnata al conto.
        sessions.PushBars(Bars(descriptor, strategy, Day(6)));
        var pendingClose = sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken)
            .PendingIntents.Single(intent => intent.IsClose);

        // 3) La posizione si chiude per un'altra via (target del broker): il cBot la riporta con una
        //    chiusura esterna, e la chiusura della strategia resta senza posizione.
        var external = sessions.CreateExternalCloseIntent(descriptor.SessionId, new CreateExternalCloseIntentRequest
        {
            SessionToken = descriptor.SessionToken,
            StrategyCode = entry!.StrategyCode,
            Symbol = entry.Symbol,
            AccountNumber = Account,
            Quantity = entry.Quantity,
            Reason = "BrokerExit:TakeProfit"
        });
        Report(sessions, descriptor, external, 110m, Day(6).AddHours(1));
        Assert.Empty(sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).Positions);

        // 4) Un nuovo ingresso deve arrivare al conto, e la chiusura orfana non deve piu' tornare.
        sessions.PushBars(Bars(descriptor, strategy, Day(8)));
        var next = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, Account).Intent;

        Assert.NotNull(next);
        Assert.Equal(OrderIntentKind.Entry, next!.Kind);
        Assert.DoesNotContain(
            sessions.GetSnapshot(descriptor.SessionId, descriptor.SessionToken).PendingIntents,
            intent => intent.IntentId == pendingClose.IntentId);
    }

    [Fact]
    public void ACloseWhosePositionIsStillOpenIsDeliveredFirst()
    {
        // Il controllo non deve toccare il caso sano: la chiusura di una posizione aperta resta la
        // prima cosa che il conto riceve.
        var (sessions, descriptor, strategy) = Session();

        sessions.PushBars(Bars(descriptor, strategy, Day(5)));
        var entry = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, Account).Intent;
        Report(sessions, descriptor, entry!, 100m, Day(5));

        sessions.PushBars(Bars(descriptor, strategy, Day(6)));
        var next = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, Account).Intent;

        Assert.NotNull(next);
        Assert.Equal(OrderIntentKind.Close, next!.Kind);
    }

    // ------------------------------------------------------------------------------ helper

    private static void Report(
        TradingSessionService sessions, TradingSessionDescriptor descriptor, OrderIntent intent, decimal price, DateTime at) =>
        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{intent.IntentId}",
                IntentId = intent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = intent.Quantity,
                FillPrice = price,
                EventTimeUtc = at
            }
        });

    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor, StrategyDefinition Strategy) Session()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"orphan-{Guid.NewGuid():N}", StrategiesFilter = [strategy.Id]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();
        TestAccountRegistry.Register(workspaces, Account);

        var sessions = new TradingSessionService(
            workspaces, new ScriptedEvaluationService(), new PositionSizingService());
        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Backtest,
            MaxConcurrentTrades = 5
        });
        sessions.SetSessionAccounts(descriptor.SessionId, descriptor.SessionToken, [Account]);
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
        return (sessions, descriptor, strategy);
    }

    private static PushBarsRequest Bars(
        TradingSessionDescriptor descriptor, StrategyDefinition strategy, DateTime barTime) => new()
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
                Bar = new OhlcvData { DateTime = barTime, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1 }
            }
        ]
    };

    private static DateTime Day(int day) => new(2026, 1, day, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Ingresso long il 5 e l'8, uscita per incrocio inverso (ExitOnly short) il 6.</summary>
    private sealed class ScriptedEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];

            var exit = closedBar.BarTimeUtc.Day == 6;
            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Symbol = strategy.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = exit ? SignalType.Sell : SignalType.Buy,
                    ExitOnly = exit,
                    Quantity = 1m,
                    Price = closedBar.Bar.Close
                }
            ];
        }
    }
}
