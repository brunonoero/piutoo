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
/// Ripresa dopo un riavvio del server sul percorso che il cBot usa davvero: sessione
/// <b>distribuita</b>, storia consegnata con <c>PushBarWindow</c>, ingressi reclamati col claim.
/// <c>SessionRestoreTests</c> copre il giro base sul percorso diretto; qui stanno i casi che la
/// revisione del 25/09/2026 ha trovato scoperti (vedi
/// <c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c>).
///
/// <para>Come in <c>SessionRestoreTests</c>, il riavvio è una seconda istanza del servizio sugli
/// stessi workspace.</para>
/// </summary>
public sealed class SessionRestoreRecoveryTests : IDisposable
{
    private const string PlanCode = "PIANORECUPERO";
    private const string Account = "2001";

    /// <summary>Martedì, in pieno orario: nessuna candela cade fuori sessione o fuori finestra.</summary>
    private static readonly DateTime Tuesday = new(2026, 1, 6, 13, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-recovery-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly StrategyDefinition _strategy;
    private readonly string _workspaceId;

    public SessionRestoreRecoveryTests()
    {
        var registered = StrategyFactory.GetRegisteredStrategies();
        _strategy = registered.FirstOrDefault(s => s.TimeframeMinutes == 15 &&
                                                   s.Symbol.Contains("NQ", StringComparison.OrdinalIgnoreCase))
                    ?? registered.First(s => s.TimeframeMinutes == 15);
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        _workspaceId = _workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"recovery-{Guid.NewGuid():N}",
            StrategiesFilter = [_strategy.Id]
        }).Id;

        TestAccountRegistry.Register(_workspaces, Account);
        SavePlan(AccountHoldingPolicy.Unrestricted);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // ------------------------------------------------------------------------ storia delle candele

    /// <summary>
    /// Il difetto più grave della revisione: la sessione ripresa riceveva per prima la finestra
    /// incrementale, e il riscaldamento profondo rimandato dal cBot subito dopo veniva scartato
    /// intero perché ogni sua candela era precedente all'ultima nota. La storia restava di poche
    /// barre e cresceva di una per barra: strategie mute per settimane.
    /// </summary>
    [Fact]
    public void DeepWarmUpAfterAShortFirstWindowRebuildsTheHistoryOfARestoredSession()
    {
        var first = NewService(new NoSignals());
        var session = Open(first);
        Window(first, session, Tuesday, 24, evaluate: false);
        Window(first, session, Tuesday.AddMinutes(15 * 20), 5, evaluate: true);

        var second = NewService(new NoSignals());
        Assert.True(Assert.Single(second.RestoreSessions()).Restored);

        var shortWindow = Window(second, session, Tuesday.AddMinutes(15 * 21), 5, evaluate: true);
        Assert.Equal(5, Assert.Single(shortWindow.Streams).HistoryBars);

        var warmUp = Window(second, session, Tuesday.AddMinutes(15 * 3), 24, evaluate: false);
        Assert.Equal(24, Assert.Single(warmUp.Streams).HistoryBars);

        // E la barra con cui il riscaldamento finisce si valuta sulla storia ricostruita, come fa il
        // cBot: riscaldamento fino all'ultima barra chiusa, poi la finestra corta che la valuta.
        var next = Window(second, session, Tuesday.AddMinutes(15 * 22), 5, evaluate: true);
        Assert.Equal(1, next.AcceptedBars);
        Assert.Equal(24, Assert.Single(next.Streams).HistoryBars);
    }

    /// <summary>
    /// Un riscaldamento che parte dopo l'ultima candela nota non si può ricucire alla storia, ma è
    /// contiguo da solo: sostituisce la storia staccata. Prima veniva rifiutato, e con lui ogni
    /// finestra successiva, per sempre.
    /// </summary>
    [Fact]
    public void AWarmUpDetachedFromTheHistoryReplacesIt()
    {
        var sessions = NewService(new NoSignals());
        var session = Open(sessions);
        Window(sessions, session, Tuesday, 5, evaluate: false);

        var wednesday = Tuesday.AddDays(1);
        var warmUp = Window(sessions, session, wednesday, 8, evaluate: false);
        Assert.Equal(8, Assert.Single(warmUp.Streams).HistoryBars);

        var next = Window(sessions, session, wednesday.AddMinutes(15 * 6), 3, evaluate: true);
        Assert.Equal(1, next.AcceptedBars);
        Assert.Equal(9, Assert.Single(next.Streams).HistoryBars);
    }

    /// <summary>La regola di prima resta per la finestra da valutare: un buco non si accoda.</summary>
    [Fact]
    public void AnEvaluationWindowDetachedFromTheHistoryIsStillRejected()
    {
        var sessions = NewService(new NoSignals());
        var session = Open(sessions);
        Window(sessions, session, Tuesday, 5, evaluate: false);

        Assert.Throws<ArgumentException>(() =>
            Window(sessions, session, Tuesday.AddDays(1), 5, evaluate: true));
    }

    // ------------------------------------------------------------------------ template

    /// <summary>
    /// Dopo un riavvio l'orologio dello stream è fermo all'ultima barra del dump, e il cBot reclama
    /// prima che ne arrivi una nuova: un segnale la cui barra è finita da ore diventava un ordine vero.
    /// </summary>
    [Fact]
    public void AnUnclaimedTemplateWhoseBarEndedDuringTheRestartIsDropped()
    {
        var first = NewService(new OneEntryPerBar(TimeSpan.FromMinutes(15)));
        var session = Open(first);
        Window(first, session, Tuesday, 5, evaluate: true);

        var second = NewService(new OneEntryPerBar(TimeSpan.FromMinutes(15)));
        var outcome = Assert.Single(second.RestoreSessions());
        Assert.True(outcome.Restored, outcome.Reason);
        Assert.Contains("scartati", outcome.Reason);

        Assert.Null(second.GetNextSignalForAccount(session.SessionId, session.SessionToken, Account).Intent);
    }

    [Fact]
    public void AnUnclaimedTemplateStillInsideItsBarSurvivesTheRestart()
    {
        var first = NewService(new OneEntryPerBar(TimeSpan.FromDays(3650)));
        var session = Open(first);
        Window(first, session, Tuesday, 5, evaluate: true);

        var second = NewService(new OneEntryPerBar(TimeSpan.FromDays(3650)));
        Assert.True(Assert.Single(second.RestoreSessions()).Restored);

        Assert.NotNull(second.GetNextSignalForAccount(session.SessionId, session.SessionToken, Account).Intent);
    }

    // ------------------------------------------------------------------------ ripresa rifiutata

    /// <summary>
    /// Piano cambiato: la ripresa si rifiuta, com'è giusto. Ma la sessione nuova che il cBot apre
    /// nella stessa cartella azzerava gli artefatti e, col primo dump, cancellava l'unica traccia
    /// delle posizioni che la vecchia aveva a mercato. Adesso la cartella si archivia e il presidio
    /// della sessione nuova elenca quelle posizioni.
    /// </summary>
    [Fact]
    public void ARejectedRestoreIsArchivedAndReportedByTheSessionThatReplacesIt()
    {
        var first = NewService(new OneEntryPerBar(TimeSpan.FromDays(3650)));
        var old = Open(first);
        Window(first, old, Tuesday, 5, evaluate: true);
        var entry = first.GetNextSignalForAccount(old.SessionId, old.SessionToken, Account).Intent;
        Assert.NotNull(entry);
        first.ApplyReport(old.SessionId, new ExecutionReportRequest
        {
            SessionToken = old.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{entry!.IntentId}",
                IntentId = entry.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = entry.FinalQuantity > 0 ? entry.FinalQuantity : 1m,
                FillPrice = 100m,
                EventTimeUtc = Tuesday.AddHours(2)
            }
        });

        SavePlan(new AccountHoldingPolicy { AllowOvernight = false, SessionFlatUtc = new TimeOnly(20, 45) });

        var second = NewService(new NoSignals());
        Assert.False(Assert.Single(second.RestoreSessions()).Restored);

        var replacement = Open(second);
        Assert.NotEqual(old.SessionId, replacement.SessionId);

        var sessionFolder = Assert.Single(Directory.GetDirectories(Path.Combine(_workspaces.GetWorkspacePath(_workspaceId), "sessions")));
        var archived = Assert.Single(Directory.GetDirectories(Path.Combine(sessionFolder, "archivio")));
        Assert.True(File.Exists(Path.Combine(archived, SessionStateSchema.FileName)));

        var watch = second.GetAccountWatch(Account);
        var finding = Assert.Single(watch.Rilievi, r => r.Finding == RealtimeWatchFinding.RipresaRifiutata);
        Assert.Equal(RealtimeWatchSeverity.Intervento, finding.Severity);
        Assert.Contains(entry.StrategyCode, finding.Message);
        Assert.Contains(archived, finding.Message);
    }

    /// <summary>
    /// Un dump illeggibile non è un silenzio: finisce fra gli esiti, e quindi nel log di avvio.
    /// </summary>
    [Fact]
    public void AnUnreadableDumpProducesAnOutcomeInsteadOfBeingSkipped()
    {
        var first = NewService(new NoSignals());
        Open(first);
        var sessionFolder = Assert.Single(Directory.GetDirectories(Path.Combine(_workspaces.GetWorkspacePath(_workspaceId), "sessions")));
        File.WriteAllText(Path.Combine(sessionFolder, SessionStateSchema.FileName), "{ non è json");

        var outcome = Assert.Single(NewService(new NoSignals()).RestoreSessions());

        Assert.False(outcome.Restored);
        Assert.Contains("illeggibile", outcome.Reason);
    }

    // ------------------------------------------------------------------------ dump

    /// <summary>
    /// Il dump che non si scrive non ferma la sessione, ma il presidio lo dice finché dura: un
    /// riavvio in quel momento riprenderebbe uno stato vecchio.
    /// </summary>
    [Fact]
    public void ADumpThatCannotBeWrittenIsReportedUntilItRecovers()
    {
        var sessions = NewService(new NoSignals());
        var session = Open(sessions);
        var sessionFolder = Assert.Single(Directory.GetDirectories(Path.Combine(_workspaces.GetWorkspacePath(_workspaceId), "sessions")));
        var dump = Path.Combine(sessionFolder, SessionStateSchema.FileName);

        // Una cartella al posto del file: la sostituzione atomica non può riuscire.
        File.Delete(dump);
        Directory.CreateDirectory(dump);
        Window(sessions, session, Tuesday, 5, evaluate: true);

        Assert.Contains(sessions.GetAccountWatch(Account).Rilievi,
            r => r.Finding == RealtimeWatchFinding.DumpDiRipresaNonAggiornato);

        Directory.Delete(dump);
        Window(sessions, session, Tuesday.AddMinutes(15), 5, evaluate: true);

        Assert.DoesNotContain(sessions.GetAccountWatch(Account).Rilievi,
            r => r.Finding == RealtimeWatchFinding.DumpDiRipresaNonAggiornato);
        Assert.True(File.Exists(dump));
    }

    // ------------------------------------------------------------------------ infrastruttura

    private void SavePlan(AccountHoldingPolicy holding) =>
        new TradingPlanService(_workspaces).Save(_workspaceId, new SaveTradingPlanRequest
        {
            Code = PlanCode,
            Name = "Piano recupero",
            Accounts = [Account],
            Holding = holding
        });

    private TradingSessionService NewService(IStrategyEvaluationService evaluation) => new(
        _workspaces, new TradingPlanService(_workspaces), evaluation, new PositionSizingService());

    private static TradingSessionDescriptor Open(TradingSessionService sessions) =>
        sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = PlanCode,
            ClientRunMode = ClientRunMode.Realtime,
            ExecutionKey = "LIVE",
            AccountNumber = Account,
            DistributeToAccounts = true
        });

    /// <summary>
    /// Una finestra di <paramref name="count"/> candele da 15 minuti a partire da
    /// <paramref name="from"/>, come la spedisce il cBot: l'ultima è la barra appena chiusa.
    /// </summary>
    private PushBarWindowResponse Window(
        TradingSessionService sessions, TradingSessionDescriptor session, DateTime from, int count, bool evaluate)
    {
        var candles = Enumerable.Range(0, count)
            .Select(i => from.AddMinutes(15 * i))
            .Select(t => new OhlcvData { DateTime = t, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1 })
            .ToList();
        var last = candles[^1].DateTime;
        return sessions.PushBarWindow(new PushBarWindowRequest
        {
            SessionId = session.SessionId,
            SessionToken = session.SessionToken,
            Windows =
            [
                new ClosedBarWindow
                {
                    Symbol = _strategy.Symbol,
                    TimeframeMinutes = 15,
                    Candles = candles,
                    Sequence = last.Ticks,
                    IdempotencyKey = $"{_strategy.Symbol}|15|{last:O}",
                    EvaluateLastCandle = evaluate
                }
            ]
        });
    }

    private sealed class NoSignals : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) => [];
    }

    /// <summary>Un ingresso stop per barra, valido dalla barra dopo fino a <c>validity</c> più tardi.</summary>
    private sealed class OneEntryPerBar(TimeSpan validity) : IStrategyEvaluationService
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
                    Type = SignalType.Buy, Quantity = 1m, Price = closedBar.Bar.High + 1,
                    OrderType = TradeOrderType.Stop, TimeframeMinutes = 15,
                    ValidFromUtc = next, ExpiresAtUtc = next + validity - TimeSpan.FromMinutes(15)
                }
            ];
        }
    }
}
