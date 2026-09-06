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
            new TestAccountRow("1001", MaxConcurrentTrades: 1),
            new TestAccountRow("2001", MaxConcurrentTrades: 1)
        ]);

        sessions.PushBars(Bars(descriptor, SessionStart.AddHours(1)));
        var claimed = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(claimed);
        Fill(sessions, descriptor, claimed!);

        sessions.PushBars(Bars(descriptor, SessionStart.AddHours(2)));

        Assert.NotNull(sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "2001").Intent);
    }

    /// <summary>
    /// Il limite è per <b>lato</b>, non per strategia: riempito il long, lo short della stessa
    /// sessione deve ancora poter entrare.
    ///
    /// <para>Il motore di ricerca dichiara <c>single_entry_per_session</c> come «una entrata per
    /// sessione <b>per direzione</b>» (<c>easy_engine_py/base.py</c>, ripetuto in §2.2 del
    /// dossier), e sui motori mirrored — TF_M, PC, BO, VBO, RBB_M, RHL, cioè quasi tutto il
    /// paniere — le due gambe nascono sulla stessa barra e sono due segnali indipendenti. Con la
    /// chiave senza lato il primo fill spegneva anche la gamba opposta per il resto della
    /// sessione.</para>
    ///
    /// <para>La posizione va chiusa prima: il lucchetto dell'OCO — un solo ingresso in volo per
    /// strategia e simbolo — è un vincolo diverso e resta.</para>
    /// </summary>
    [Fact]
    public void DopoIlFillDelLong_LoShortDellaStessaSessionePuoAncoraEntrare()
    {
        var (sessions, descriptor) = Session(evaluation: new MirroredEvaluationService());

        var primaBarra = sessions.PushBars(Bars(descriptor, SessionStart.AddHours(1))).Intents;
        var buy = Assert.Single(primaBarra, i => i.Side == SignalType.Buy);
        Assert.Contains(primaBarra, i => i.Side == SignalType.Sell);

        Fill(sessions, descriptor, buy);

        var close = sessions.CreateExternalCloseIntent(descriptor.SessionId, new CreateExternalCloseIntentRequest
        {
            SessionToken = descriptor.SessionToken,
            StrategyCode = buy.StrategyCode,
            Symbol = buy.Symbol
        });
        Fill(sessions, descriptor, close);

        var secondaBarra = sessions.PushBars(Bars(descriptor, SessionStart.AddHours(2))).Intents;

        // Lo short non ha ancora riempito in questa sessione: passa.
        Assert.Contains(secondaBarra, i => i.Side == SignalType.Sell);
        // Il long invece ha consumato il proprio limite.
        Assert.DoesNotContain(secondaBarra, i => i.Side == SignalType.Buy);
    }

    // ------------------------------------------------------------------------------ helper

    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor) Session(
        IReadOnlyList<TestAccountRow>? accounts = null,
        IStrategyEvaluationService? evaluation = null)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"entrylimit-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        TestAccountRegistry.Register(workspaces, accounts);

        var sessions = new TradingSessionService(
            workspaces, evaluation ?? new OneEntryPerSessionEvaluationService(), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Realtime,
            MaxConcurrentTrades = TestSessionAccounts.MaxConcurrentTrades(accounts),
            ConcurrencyCountMode = TestSessionAccounts.CountMode(accounts)
        });
        if (accounts is not null)
            sessions.SetSessionAccounts(
                descriptor.SessionId, descriptor.SessionToken, TestSessionAccounts.Numbers(accounts));
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

    /// <summary>
    /// Le due gambe di un motore mirrored sulla stessa barra: stesso limite, stesso secchio, lati
    /// opposti. È la forma con cui TF_M, PC, BO, VBO, RBB_M e RHL emettono.
    /// </summary>
    private sealed class MirroredEvaluationService : IStrategyEvaluationService
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

            TradeSignal Gamba(SignalType side, decimal price) => new()
            {
                StrategyCode = strategy.Name,
                StrategyName = strategy.Name,
                Symbol = strategy.Symbol,
                Date = closedBar.BarTimeUtc,
                Type = side,
                OrderType = TradeOrderType.Stop,
                Quantity = 4m,
                Price = price,
                ValidFromUtc = closedBar.BarTimeUtc.AddMinutes(closedBar.TimeframeMinutes),
                MaxEntriesPerSession = 1,
                EntrySessionStartUtc = sessionStart
            };

            return
            [
                Gamba(SignalType.Buy, closedBar.Bar.Close + 1m),
                Gamba(SignalType.Sell, closedBar.Bar.Close - 1m)
            ];
        }
    }
}
