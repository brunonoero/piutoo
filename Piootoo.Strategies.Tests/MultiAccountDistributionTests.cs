using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il layer che decide chi esegue quale segnale.
///
/// <para>Copre i comportamenti che la matrice di
/// <c>docs/domini/distribuzione-multi-account.md</c> descrive: il fan-out fra gruppi diversi sullo
/// stesso template, il template perso dopo un rifiuto del broker, e il disaccoppiamento fra il
/// limite di trade concorrenti.</para>
///
/// <para>Dall'11/08/2026 copre anche il budget di concorrenza riscritto: <b>per account e
/// trasversale ai simboli</b>, deduplicato per IntentId, con le due modalità di conteggio
/// (<c>ConcurrencyCountMode</c>). I test che verificavano il comportamento precedente — un solo
/// intent pendente per account, un solo ingresso per account per simbolo — sono stati sostituiti,
/// non rimossi: descrivevano un vincolo che rendeva <c>MaxConcurrentTrades</c> inapplicabile su una
/// sessione a simbolo singolo. Vedi <c>docs/decisioni.md</c> 2026-08-11.</para>
/// </summary>
public sealed class MultiAccountDistributionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-distrib-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void DifferentGroups_BothClaimTheSameTemplate()
    {
        // Il lucchetto sul template è PER GRUPPO: due gruppi sono due portafogli paralleli sullo
        // stesso flusso di segnali. È il comportamento più caratteristico del layer.
        var (sessions, descriptor) = Session(signalsPerBar: 1,
        [
            Row("1001", maxConcurrent: 1),
            Row("2001", maxConcurrent: 1)
        ]);

        var pushed = sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
        var template = Assert.Single(pushed.Intents);

        var first = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");
        var second = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "2001");

        Assert.NotNull(first.Intent);
        Assert.NotNull(second.Intent);
        Assert.Equal(template.StrategyCode, first.Intent!.StrategyCode);
        Assert.Equal(template.StrategyCode, second.Intent!.StrategyCode);
        // Sono due claim distinti dello stesso template, non lo stesso intent.
        Assert.NotEqual(first.Intent.IntentId, second.Intent.IntentId);
        Assert.Equal("1001", first.Intent.AssignedAccountNumber);
        Assert.Equal("2001", second.Intent.AssignedAccountNumber);
    }

    /// <summary>
    /// Ogni conto del piano riceve ogni segnale, una volta sola.
    ///
    /// <para>Fino alla rimozione dei gruppi (<c>docs/decisioni.md</c> 2026-09-05) il template era
    /// consumato una volta per <i>gruppo</i>, quindi il secondo conto dello stesso gruppo non lo
    /// vedeva. Ora l'unità è il conto: due conti sul piano sono due portafogli paralleli sullo
    /// stesso flusso, ed è la configurazione che i piani reali avevano già — un conto per gruppo.</para>
    /// </summary>
    [Fact]
    public void OgniConto_RiceveLoStessoTemplate_UnaVoltaSola()
    {
        var (sessions, descriptor) = Session(signalsPerBar: 1,
        [
            Row("1001", maxConcurrent: 1),
            Row("1002", maxConcurrent: 1)
        ]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var primo = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        var secondo = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1002").Intent;

        Assert.NotNull(primo);
        Assert.NotNull(secondo);
        // Stesso segnale, due claim distinti: uno per conto.
        Assert.Equal(primo!.StrategyCode, secondo!.StrategyCode);
        Assert.NotEqual(primo.IntentId, secondo.IntentId);

        // E nessuno dei due ne ottiene un secondo: al poll successivo il conto a budget pieno si
        // vede riproporre l'ingresso che ha gia' in mano (stesso IntentId), non un claim nuovo.
        var ancora = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1002");
        Assert.Equal(secondo.IntentId, ancora.Intent?.IntentId);
    }

    [Fact]
    public void AccountDrainsUpToItsBudget_NotOneIntentAtATime()
    {
        // Fino all'11/08/2026 il passo 1 faceva da tappo: qualunque intent pendente fermava il
        // poll, quindi un account ne deteneva uno solo alla volta qualunque fosse
        // MaxConcurrentTrades. Ora drena finché ha budget.
        var (sessions, descriptor) = Session(signalsPerBar: 2, [Row("1001", maxConcurrent: 5)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var first = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");
        var second = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.NotNull(first.Intent);
        Assert.NotNull(second.Intent);
        // Due intent distinti, di strategie distinte, sullo STESSO simbolo: è esattamente ciò che
        // il lucchetto (account, simbolo) impediva.
        Assert.NotEqual(first.Intent!.IntentId, second.Intent!.IntentId);
        Assert.NotEqual(first.Intent.StrategyCode, second.Intent.StrategyCode);
        Assert.Equal(first.Intent.Symbol, second.Intent.Symbol);
    }

    [Fact]
    public void AtTheCap_ThePendingEntryIsRedelivered()
    {
        // A budget esaurito il claim ripropone l'ingresso pendente invece di rispondere
        // MaxConcurrentTradesExceeded: è come si recupera un claim la cui risposta si è persa in
        // rete. Il client lo riconosce come già inviato e smette di drenare.
        var (sessions, descriptor) = Session(signalsPerBar: 2, [Row("1001", maxConcurrent: 1)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var first = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");
        var again = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.NotNull(first.Intent);
        Assert.NotNull(again.Intent);
        Assert.Equal(first.Intent!.IntentId, again.Intent!.IntentId);
    }

    [Fact]
    public void BudgetIsPerAccountNotPerSymbol_AndSurvivesTheFill()
    {
        // Il vecchio lucchetto (account, simbolo) non si liberava al fill ma alla chiusura: un
        // secondo template sullo stesso simbolo restava irraggiungibile anche con budget libero.
        var (sessions, descriptor) = Session(signalsPerBar: 2, [Row("1001", maxConcurrent: 5)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(claimed);

        Fill(sessions, descriptor, claimed!);

        var afterFill = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.NotNull(afterFill.Intent);
        Assert.Equal(claimed!.Symbol, afterFill.Intent!.Symbol);
        Assert.NotEqual(claimed.StrategyCode, afterFill.Intent.StrategyCode);
    }

    [Fact]
    public void InFlightCount_DeduplicatesTheSameIntentSeenTwice()
    {
        // Lo stesso ordine è insieme un intent Pending sul server e un pending order nello
        // snapshot del broker. Sommare i due conteggi grezzi — com'era prima — lo contava due
        // volte e dimezzava il tetto configurato: con max 2 il secondo claim non passava.
        var (sessions, descriptor) = Session(signalsPerBar: 3, [Row("1001", maxConcurrent: 2)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var first = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
            new AccountSignalPollRequest { SessionToken = descriptor.SessionToken });
        Assert.NotNull(first.Intent);

        // Il broker ora dichiara l'ordine piazzato per quell'intent: è la stessa cosa, non una
        // seconda esposizione.
        var second = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
            new AccountSignalPollRequest
            {
                SessionToken = descriptor.SessionToken,
                Orders = [new BrokerOrderSnapshot { OrderId = "o-1", IntentId = first.Intent!.IntentId }]
            });

        Assert.NotNull(second.Intent);
        Assert.NotEqual(first.Intent.IntentId, second.Intent!.IntentId);
    }

    [Fact]
    public void PositionsOnly_PendingOrdersDoNotConsumeBudget()
    {
        // Su un motore breakout un ordine stop non è esposizione ma un'opzione: bloccarne uno per
        // "occupazione di slot" significa perdere il solo livello che sarebbe partito. Il tetto lo
        // fa valere il cBot al primo fill.
        var (sessions, descriptor) = Session(signalsPerBar: 2,
            [Row("1001", maxConcurrent: 1, countMode: ConcurrencyCountMode.PositionsOnly)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var first = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
            new AccountSignalPollRequest { SessionToken = descriptor.SessionToken });
        Assert.NotNull(first.Intent);

        var second = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
            new AccountSignalPollRequest
            {
                SessionToken = descriptor.SessionToken,
                Orders = [new BrokerOrderSnapshot { OrderId = "o-1", IntentId = first.Intent!.IntentId }]
            });

        // Con PositionsAndPendingOrders questo sarebbe MaxConcurrentTradesExceeded.
        Assert.NotNull(second.Intent);
    }

    [Fact]
    public void PositionsOnly_AFilledPositionStillConsumesBudget()
    {
        var (sessions, descriptor) = Session(signalsPerBar: 2,
            [Row("1001", maxConcurrent: 1, countMode: ConcurrencyCountMode.PositionsOnly)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(claimed);

        Fill(sessions, descriptor, claimed!);

        var afterFill = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
            new AccountSignalPollRequest
            {
                SessionToken = descriptor.SessionToken,
                Positions = [new BrokerPositionSnapshot { PositionId = "p-1", IntentId = claimed!.IntentId }]
            });

        Assert.Null(afterFill.Intent);
        Assert.Equal("MaxConcurrentTradesExceeded", afterFill.Reason);
    }

    /// <summary>
    /// Un rifiuto del broker libera i lucchetti del conto ma non gli restituisce il template: quel
    /// segnale, per quel conto, è stato servito. Gli altri conti del piano non ne sono toccati —
    /// ognuno consuma il proprio.
    /// </summary>
    [Fact]
    public void RejectedEntry_FreesTheAccount_ButTheTemplateStaysConsumedByThatAccount()
    {
        var (sessions, descriptor) = Session(signalsPerBar: 1,
        [
            Row("1001", maxConcurrent: 1),
            Row("1002", maxConcurrent: 1)
        ]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(claimed);

        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "r-reject", IntentId = claimed!.IntentId,
                Status = ExecutionReportStatus.Rejected,
                CumulativeFilledQuantity = 0m, EventTimeUtc = Utc(2026, 1, 5)
            }
        });

        // Il conto che lo ha consumato non lo riprende: il rifiuto libera slot e budget, non
        // restituisce il template.
        Assert.Null(sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent);
        // L'altro conto non lo aveva mai consumato, quindi lo prende: è il fan-out per conto.
        Assert.NotNull(sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1002").Intent);
    }

    // ------------------------------------------------------------------- orologio del claim

    [Fact]
    public void TemplateWithExpiry_OnHistoricalBars_IsStillClaimable()
    {
        // Regressione: il claim scartava i template scaduti confrontando ExpiresAtUtc con
        // DateTime.UtcNow invece che con la barra appena valutata. In un replay storico le due date
        // distano mesi, quindi OGNI ordine "next bar" dei motori Unger nasceva già scaduto: il
        // server generava i segnali, il claim rispondeva sempre NoSignal e sul broker non arrivava
        // mai un ordine.
        var barTime = Utc(2025, 11, 3);
        var (sessions, descriptor) = Session(
            signalsPerBar: 1,
            [Row("1001", maxConcurrent: 1)],
            // Scadenza alla barra successiva, come la emette un motore Unger.
            expiresAtUtc: barTime.AddMinutes(15));

        var pushed = sessions.PushBars(Bars(descriptor, barTime));
        Assert.Single(pushed.Intents);

        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.NotNull(claimed.Intent);
        Assert.Equal("1001", claimed.Intent!.AssignedAccountNumber);
    }

    [Fact]
    public void TemplateExpiredBeforeTheCurrentBar_IsNotClaimable()
    {
        // L'altra metà: la scadenza deve continuare a valere: un template la cui finestra è già
        // chiusa rispetto alla barra corrente non va eseguito al proprio livello.
        var barTime = Utc(2025, 11, 3);
        var (sessions, descriptor) = Session(
            signalsPerBar: 1,
            [Row("1001", maxConcurrent: 1)],
            expiresAtUtc: barTime.AddMinutes(-15));

        sessions.PushBars(Bars(descriptor, barTime));

        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.Null(claimed.Intent);
        Assert.Equal("NoSignal", claimed.Reason);
    }

    // ------------------------------------------------- il flag esplicito di concorrenza

    [Fact]
    public void ConcurrencyLimitDefault_IsOffOnlyForTheSourceBacktest()
    {
        Assert.False(TradingSessionService.DefaultEnforceConcurrencyLimits(ClientRunMode.Backtest));
        Assert.True(TradingSessionService.DefaultEnforceConcurrencyLimits(ClientRunMode.Realtime));
    }

    [Fact]
    public void ConcurrencyLimitCanBeForcedOn_InABacktest()
    {
        // Il flag e' esplicito proprio per poter misurare il costo del limite in isolamento.
        var (sessions, descriptor) = Session(
            signalsPerBar: 1,
            [Row("1001", maxConcurrent: 1)],
            runMode: ClientRunMode.Backtest,
            enforceConcurrencyLimits: true);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var response = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
            new AccountSignalPollRequest
            {
                SessionToken = descriptor.SessionToken,
                Orders = [new BrokerOrderSnapshot { OrderId = "pending-1" }]
            });

        Assert.Null(response.Intent);
        Assert.Equal("MaxConcurrentTradesExceeded", response.Reason);
    }

    [Fact]
    public void ConcurrencyLimitCanBeForcedOff_InRealtime()
    {
        var (sessions, descriptor) = Session(
            signalsPerBar: 1,
            [Row("1001", maxConcurrent: 1)],
            runMode: ClientRunMode.Realtime,
            enforceConcurrencyLimits: false);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var response = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
            new AccountSignalPollRequest
            {
                SessionToken = descriptor.SessionToken,
                Orders = [new BrokerOrderSnapshot { OrderId = "pending-1" }]
            });

        Assert.NotNull(response.Intent);
    }

    // ------------------------------------------------------------------------------ helper

    private static TestAccountRow Row(
        string account,
        int maxConcurrent,
        ConcurrencyCountMode countMode = ConcurrencyCountMode.PositionsAndPendingOrders) =>
        new(account, maxConcurrent, countMode);

    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor) Session(
        int signalsPerBar,
        IReadOnlyList<TestAccountRow> accounts,
        ClientRunMode runMode = ClientRunMode.Realtime,
        bool? enforceConcurrencyLimits = null,
        DateTime? expiresAtUtc = null)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"distrib-{Guid.NewGuid():N}", StrategiesFilter = [strategyId]
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();
        TestAccountRegistry.Register(workspaces, accounts);

        var sessions = new TradingSessionService(
            workspaces, new MultiSignalEvaluationService(signalsPerBar, expiresAtUtc), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = runMode,
            EnforceConcurrencyLimits = enforceConcurrencyLimits,
            MaxConcurrentTrades = TestSessionAccounts.MaxConcurrentTrades(accounts),
            ConcurrencyCountMode = TestSessionAccounts.CountMode(accounts)
        });
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
                FillPrice = 100m, EventTimeUtc = Utc(2026, 1, 5)
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

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Emette N segnali per barra sullo stesso simbolo ma con <b>codici strategia distinti</b>.
    ///
    /// <para>I codici distinti servono a isolare i vincoli: con lo stesso codice sarebbero lo slot
    /// di gruppo <c>(gruppo, strategia, simbolo)</c> e la guardia <c>AccountHasEntryInFlight</c> a
    /// bloccare il secondo claim, e non si potrebbe osservare il budget per account. Il primo
    /// segnale conserva il nome reale della strategia, così i test che si limitano a un segnale
    /// restano aderenti al catalogo.</para>
    /// </summary>
    private sealed class MultiSignalEvaluationService(int signalsPerBar, DateTime? expiresAtUtc = null)
        : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault();
            if (strategy is null) return [];
            return Enumerable.Range(0, signalsPerBar)
                .Select(index =>
                {
                    var code = index == 0 ? strategy.Name : $"{strategy.Name}-{index}";
                    return new TradeSignal
                    {
                        StrategyCode = code,
                        StrategyName = code,
                        Symbol = strategy.Symbol,
                        Date = closedBar.BarTimeUtc,
                        Type = SignalType.Buy,
                        Quantity = 4m,
                        Price = closedBar.Bar.Close,
                        ExpiresAtUtc = expiresAtUtc
                    };
                })
                .ToArray();
        }
    }
}
