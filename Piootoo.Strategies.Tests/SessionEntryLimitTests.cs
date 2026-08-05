using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Limite di fill per sessione (<c>MaxEntriesPerSession</c>) in <c>ExternalBroker</c>.
///
/// <para>Il vincolo nasce dai motori Unger — le PC del catalogo dichiarano un solo fill per sessione
/// CME — ed era applicato soltanto da <c>PiootooTradingService</c>, cioè nel backtest e in
/// <c>ServerSimulated</c>. Con un broker esterno il campo non arrivava nemmeno sull'intent, quindi
/// nessuno lo verificava e la stessa strategia poteva entrare più volte nella stessa sessione.</para>
///
/// <para>Si conta sui <b>fill confermati</b>, non sugli intent emessi: uno stop non eseguito deve
/// poter essere riemesso, ed è la stessa semantica del motore simulato.</para>
/// </summary>
public sealed class SessionEntryLimitTests : IDisposable
{
    private static readonly DateTime SessionStart = new(2026, 1, 5, 17, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-entrylimit-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void TheIntentCarriesTheLimitDeclaredByTheStrategy()
    {
        var (sessions, descriptor) = Session();

        var intent = Assert.Single(sessions.PushBars(Bars(descriptor, SessionStart.AddHours(1))).Intents);

        // Senza questi due campi il client non può nemmeno diagnosticare il limite.
        Assert.Equal(1, intent.MaxEntriesPerSession);
        Assert.Equal(SessionStart, intent.EntrySessionStartUtc);
    }

    [Fact]
    public void AfterTheConfirmedFill_NoOtherEntryIsDeliveredInTheSameSession()
    {
        var (sessions, descriptor) = Session();

        var first = Assert.Single(sessions.PushBars(Bars(descriptor, SessionStart.AddHours(1))).Intents);
        Fill(sessions, descriptor, first);

        var second = sessions.PushBars(Bars(descriptor, SessionStart.AddHours(2)));

        Assert.Empty(second.Intents);
        // L'intent esiste comunque in sessione, annullato: serve come traccia di audit e non viene
        // consegnato, così un client che ignorasse Status non può eseguirlo.
        var audited = sessions.GetIntents(descriptor.SessionId, descriptor.SessionToken)
            .Last(x => x.Kind == OrderIntentKind.Entry);
        Assert.Equal(OrderIntentStatus.Cancelled, audited.Status);
    }

    [Fact]
    public void AnUnfilledEntryDoesNotConsumeTheLimit()
    {
        // È il caso normale del Price Channel: lo stop non viene eseguito e il motore lo riemette
        // sulla barra dopo col livello ricalcolato.
        var (sessions, descriptor) = Session();

        sessions.PushBars(Bars(descriptor, SessionStart.AddHours(1)));
        var second = sessions.PushBars(Bars(descriptor, SessionStart.AddHours(2)));

        Assert.Single(second.Intents);
        Assert.Equal(OrderIntentStatus.Pending, second.Intents[0].Status);
    }

    [Fact]
    public void TheNextSessionStartsFromScratch()
    {
        var (sessions, descriptor) = Session();

        var first = Assert.Single(sessions.PushBars(Bars(descriptor, SessionStart.AddHours(1))).Intents);
        Fill(sessions, descriptor, first);

        var nextSession = sessions.PushBars(Bars(descriptor, SessionStart.AddDays(1).AddHours(1)));

        Assert.Single(nextSession.Intents);
    }

    [Fact]
    public void TheLimitIsPerAccount_NotGlobal()
    {
        // In multi-account il conteggio globale bloccherebbe tutti gli account appena uno riempie:
        // gruppi diversi sono portafogli paralleli sullo stesso flusso di segnali.
        var (sessions, descriptor) = Session(
        [
            new TradingGroupRow { GroupId = "g1", AccountNumber = "1001", MaxConcurrentTrades = 1, ApplyTitanoFilters = false },
            new TradingGroupRow { GroupId = "g2", AccountNumber = "2001", MaxConcurrentTrades = 1, ApplyTitanoFilters = false }
        ]);

        sessions.PushBars(Bars(descriptor, SessionStart.AddHours(1)));
        var claimed = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(claimed);
        Fill(sessions, descriptor, claimed!);

        sessions.PushBars(Bars(descriptor, SessionStart.AddHours(2)));

        Assert.NotNull(sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "2001").Intent);
    }

    // ------------------------------------------------------------------------------ helper

    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor) Session(
        IReadOnlyList<TradingGroupRow>? groups = null)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"entrylimit-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        TestAccountRegistry.Register(workspaces, groups);

        var sessions = new TradingSessionService(
            workspaces, new OneEntryPerSessionEvaluationService(),
            new TitanoRotationService(workspaces), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            TitanoMode = TitanoFilterMode.Disabled
        });
        if (groups is not null)
            sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken, groups);
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
        return (sessions, descriptor);
    }

    private static void Fill(
        TradingSessionService sessions, TradingSessionDescriptor descriptor, OrderIntent intent) =>
        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{intent.IntentId}", IntentId = intent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = intent.Quantity,
                FillPrice = 100m, EventTimeUtc = SessionStart.AddHours(1)
            }
        });

    private static PushBarsRequest Bars(TradingSessionDescriptor descriptor, DateTime barTime)
    {
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        return new PushBarsRequest
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
    }

    /// <summary>
    /// Un ingresso per barra che dichiara un solo fill per la sessione iniziata alle 17:00 UTC del
    /// 5 gennaio, come fanno le PC del catalogo con la sessione CME.
    /// </summary>
    private sealed class OneEntryPerSessionEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];

            var sessionStart = closedBar.BarTimeUtc < SessionStart.AddDays(1)
                ? SessionStart
                : SessionStart.AddDays(1);

            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Symbol = strategy.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy,
                    OrderType = TradeOrderType.Stop,
                    Quantity = 4m,
                    Price = closedBar.Bar.Close + 1m,
                    ValidFromUtc = closedBar.BarTimeUtc.AddMinutes(closedBar.TimeframeMinutes),
                    MaxEntriesPerSession = 1,
                    EntrySessionStartUtc = sessionStart
                }
            ];
        }
    }
}
