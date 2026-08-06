using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Secondo layer di filtro, quello che agisce dopo Titano: chi esegue quale segnale.
///
/// <para>Copre i tre comportamenti che la matrice di
/// <c>docs/domini/distribuzione-multi-account.md</c> descrive e che non erano verificati: il
/// fan-out fra gruppi diversi sullo stesso template, il lucchetto account/simbolo che sopravvive al
/// fill, e il template perso dopo un rifiuto del broker. In più il disaccoppiamento fra il limite di
/// trade concorrenti e la modalità Titano.</para>
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
            Row("g1", "1001", maxConcurrent: 1),
            Row("g2", "2001", maxConcurrent: 1)
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

    [Fact]
    public void SameGroup_SecondAccountDoesNotGetTheSameTemplate()
    {
        var (sessions, descriptor) = Session(signalsPerBar: 1,
        [
            Row("g1", "1001", maxConcurrent: 1),
            Row("g1", "1002", maxConcurrent: 1)
        ]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        Assert.NotNull(sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent);

        var sibling = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1002");
        Assert.Null(sibling.Intent);
        Assert.Equal("NoSignal", sibling.Reason);
    }

    [Fact]
    public void PollIsIdempotent_AnAccountHoldsOnlyOnePendingIntent()
    {
        var (sessions, descriptor) = Session(signalsPerBar: 2, [Row("g1", "1001", maxConcurrent: 5)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));

        var first = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");
        var again = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.NotNull(first.Intent);
        Assert.NotNull(again.Intent);
        // Il secondo poll ripropone lo stesso intent invece di assegnarne un altro, anche se il
        // limite di concorrenza lo permetterebbe e un secondo template è disponibile.
        Assert.Equal(first.Intent!.IntentId, again.Intent!.IntentId);
    }

    [Fact]
    public void SymbolLockSurvivesTheFill_AndIsReleasedOnlyOnClose()
    {
        // Il lucchetto (account, simbolo) NON si libera al fill: si libera alla chiusura. È il motivo
        // per cui MaxConcurrentTrades non si materializza mai su un singolo simbolo.
        var (sessions, descriptor) = Session(signalsPerBar: 2, [Row("g1", "1001", maxConcurrent: 5)]);

        sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent;
        Assert.NotNull(claimed);

        Fill(sessions, descriptor, claimed!);

        // Secondo template disponibile sullo stesso simbolo, limite di concorrenza non raggiunto,
        // eppure nessun segnale: il simbolo è occupato.
        var afterFill = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");
        Assert.Null(afterFill.Intent);
        Assert.Equal("NoSignal", afterFill.Reason);
    }

    [Fact]
    public void RejectedEntry_FreesTheAccount_ButTheTemplateStaysConsumedByTheGroup()
    {
        var (sessions, descriptor) = Session(signalsPerBar: 1,
        [
            Row("g1", "1001", maxConcurrent: 1),
            Row("g1", "1002", maxConcurrent: 1)
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

        // Nessuno dei due account riprende quel segnale: il gruppo lo ha già consumato. Il rifiuto
        // libera i lucchetti di slot e simbolo, non restituisce il template.
        Assert.Null(sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001").Intent);
        Assert.Null(sessions.GetNextSignalForAccount(
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
            [Row("g1", "1001", maxConcurrent: 1)],
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
            [Row("g1", "1001", maxConcurrent: 1)],
            expiresAtUtc: barTime.AddMinutes(-15));

        sessions.PushBars(Bars(descriptor, barTime));

        var claimed = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.Null(claimed.Intent);
        Assert.Equal("NoSignal", claimed.Reason);
    }

    // ------------------------------------------------- disaccoppiamento dal filtro Titano

    [Fact]
    public void ConcurrencyLimitDefault_IsOffOnlyForTheSourceBacktest()
    {
        Assert.False(TradingSessionService.DefaultEnforceConcurrencyLimits(
            ClientRunMode.Backtest, TitanoFilterMode.Disabled));
        Assert.True(TradingSessionService.DefaultEnforceConcurrencyLimits(
            ClientRunMode.Backtest, TitanoFilterMode.BacktestRotationFile));
        Assert.True(TradingSessionService.DefaultEnforceConcurrencyLimits(
            ClientRunMode.Realtime, TitanoFilterMode.Disabled));
    }

    [Fact]
    public void ConcurrencyLimitCanBeForcedOn_InABacktestWithoutTitano()
    {
        // Il punto del disaccoppiamento: prima il limite era cablato su TitanoMode, quindi non era
        // possibile misurare il costo del limite in isolamento dalla rotazione.
        var (sessions, descriptor) = Session(
            signalsPerBar: 1,
            [Row("g1", "1001", maxConcurrent: 1)],
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
            [Row("g1", "1001", maxConcurrent: 1)],
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

    private static TradingGroupRow Row(string groupId, string account, int maxConcurrent) => new()
    {
        GroupId = groupId, AccountNumber = account,
        MaxConcurrentTrades = maxConcurrent, ApplyTitanoFilters = false
    };

    private (TradingSessionService Sessions, TradingSessionDescriptor Descriptor) Session(
        int signalsPerBar,
        IReadOnlyList<TradingGroupRow> groups,
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
        TestAccountRegistry.Register(workspaces, groups);

        var sessions = new TradingSessionService(
            workspaces, new MultiSignalEvaluationService(signalsPerBar, expiresAtUtc),
            new TitanoRotationService(workspaces), new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = runMode,
            TitanoMode = TitanoFilterMode.Disabled,
            EnforceConcurrencyLimits = enforceConcurrencyLimits
        });
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
    /// <para>I codici distinti servono a isolare i lucchetti: con lo stesso codice sarebbe lo slot
    /// di gruppo <c>(gruppo, strategia, simbolo)</c> a bloccare il secondo claim, e non si potrebbe
    /// osservare separatamente il lucchetto <c>(account, simbolo)</c>. Il primo segnale conserva il
    /// nome reale della strategia, così i test che si limitano a un segnale restano aderenti al
    /// catalogo.</para>
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
