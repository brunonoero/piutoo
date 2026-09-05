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
/// Dump e ripresa di una sessione realtime dopo il riavvio del processo server
/// (<c>docs/domini/riavvio-del-server-e-ripresa-sessione.md</c>, fasi 0 e 1).
///
/// <para>Il riavvio si simula con una <b>seconda istanza</b> di <c>TradingSessionService</c> sugli
/// stessi workspace: è esattamente ciò che accade al riavvio, dove <c>_sessions</c> riparte vuoto e
/// l'unica cosa che sopravvive è quello che sta su disco.</para>
///
/// <para>La proprietà che conta più di tutte è la conservazione di <b>session id e token</b>: il
/// file di stato locale del cBot è ancorato al session id, e con un id nuovo il bot scarta
/// break-even, trailing e uscite a tempo di ogni posizione aperta — un riavvio del server che
/// degrada silenziosamente le uscite sul client.</para>
/// </summary>
public sealed class SessionRestoreTests : IDisposable
{
    private const string PlanCode = "PIANORIPRESA";
    private const string Account = "1001";
    private const string ExecutionKey = "LIVE";

    private static readonly DateTime Origin = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-restore-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly StrategyDefinition _strategy;

    public SessionRestoreTests()
    {
        _strategy = StrategyFactory.GetRegisteredStrategies()[0];
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = _workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"restore-{Guid.NewGuid():N}",
            StrategiesFilter = [_strategy.Id]
        });

        TestAccountRegistry.Register(_workspaces, Account);
        SavePlan(workspace.Id, AccountHoldingPolicy.Default);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void LaSessioneRipresaConservaIdETokenDiPrima()
    {
        var primo = NewService();
        var aperta = Open(primo);

        var secondo = NewService();
        var esiti = secondo.RestoreSessions();

        var esito = Assert.Single(esiti);
        Assert.True(esito.Restored, esito.Reason);
        Assert.Equal(aperta.SessionId, esito.SessionId);

        // Il token vale ancora: se non valesse, il cBot riceverebbe 401 su ogni chiamata e
        // riaprirebbe da zero, che è il caso che tutto questo lavoro esiste per evitare.
        var snapshot = secondo.GetSnapshot(aperta.SessionId, aperta.SessionToken);
        Assert.Equal(aperta.SessionId, snapshot.SessionId);
        Assert.Equal(TradingSessionStatus.Running, snapshot.Status);
    }

    /// <summary>
    /// La proprietà end-to-end: il cBot che si riaggancia con <c>open-plan</c> deve rientrare nella
    /// PROPRIA sessione, non aprirne una seconda accanto. Se la chiave di indice ricomposta dal dump
    /// non coincide con quella che <c>OpenFromPlan</c> calcola, questo test è l'unico punto in cui
    /// la differenza si vede.
    /// </summary>
    [Fact]
    public void IlCBotCheSiRiagganciaRientraNellaSessioneRipresa()
    {
        var primo = NewService();
        var aperta = Open(primo);

        var secondo = NewService();
        secondo.RestoreSessions();

        var riagganciata = Open(secondo);

        Assert.Equal(aperta.SessionId, riagganciata.SessionId);
        Assert.Equal(aperta.SessionToken, riagganciata.SessionToken);
        Assert.Single(secondo.ListSessions());
    }

    [Fact]
    public void LePosizioniAperteSopravvivonoAlRiavvio()
    {
        var primo = NewService();
        var aperta = Open(primo);
        var intent = PushBarAndClaim(primo, aperta);
        Fill(primo, aperta, intent);

        var secondo = NewService();
        secondo.RestoreSessions();

        var snapshot = secondo.GetSnapshot(aperta.SessionId, aperta.SessionToken);
        var posizione = Assert.Single(snapshot.Positions);
        Assert.Equal(intent.StrategyCode, posizione.StrategyCode);
        Assert.Equal(1, snapshot.Entries);
        Assert.Equal(1, snapshot.Fills);
    }

    /// <summary>
    /// L'<c>IntentId</c> è <c>{sessionId}-{progressivo}</c>: se il progressivo ripartisse da zero,
    /// il primo intent dopo la ripresa avrebbe lo stesso id di uno già scritto in
    /// <c>signals.json</c>, e i due si fonderebbero in uno solo al primo upsert.
    /// </summary>
    [Fact]
    public void IlProgressivoDegliIntentNonRipartaDaZero()
    {
        var primo = NewService();
        var aperta = Open(primo);
        var prima = PushBarAndClaim(primo, aperta);

        var secondo = NewService();
        secondo.RestoreSessions();
        var dopo = PushBarAndClaim(secondo, aperta);

        Assert.NotEqual(prima.IntentId, dopo.IntentId);
        Assert.True(
            string.CompareOrdinal(dopo.IntentId, prima.IntentId) > 0,
            $"l'id dopo la ripresa ({dopo.IntentId}) deve venire dopo quello di prima ({prima.IntentId})");
    }

    /// <summary>
    /// Su una sessione ripresa <c>session.Intents</c> contiene i soli ordini in volo del dump: una
    /// riscrittura completa degli artefatti cancellerebbe la storia precedente. Qui si passa dal
    /// percorso più innocuo che ci sia — qualcuno che apre i segnali per leggerli — che internamente
    /// forza proprio quella scrittura.
    /// </summary>
    [Fact]
    public void LaLetturaDegliArtefattiNonCancellaLaStoriaPrecedente()
    {
        var primo = NewService();
        var aperta = Open(primo);
        var intent = PushBarAndClaim(primo, aperta);
        Fill(primo, aperta, intent);

        var primaDelRiavvio = primo.GetPersistedSignals(aperta.SessionId, aperta.SessionToken);
        Assert.Contains(primaDelRiavvio, signal => signal.IntentId == intent.IntentId);

        var secondo = NewService();
        secondo.RestoreSessions();

        var dopoLaRipresa = secondo.GetPersistedSignals(aperta.SessionId, aperta.SessionToken);
        Assert.Contains(dopoLaRipresa, signal => signal.IntentId == intent.IntentId);
    }

    /// <summary>
    /// Il piano cambiato è il caso che va rifiutato: una posizione aperta sotto "niente overnight"
    /// non va sorvegliata con "overnight libero", perché cambia quando quel trade si chiude. E il
    /// rifiuto non deve lasciare mezza sessione in memoria.
    /// </summary>
    [Fact]
    public void SeIlPianoCambiaLaSessioneNonSiRiprende()
    {
        var primo = NewService();
        var aperta = Open(primo);

        SavePlan(aperta.WorkspaceId, new AccountHoldingPolicy
        {
            AllowOvernight = false,
            SessionFlatUtcHhmm = 2045
        });

        var secondo = NewService();
        var esito = Assert.Single(secondo.RestoreSessions());

        Assert.False(esito.Restored);
        Assert.Contains("cambiat", esito.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(secondo.ListSessions());
    }

    /// <summary>
    /// Una sessione fermata a mano resta ferma: riprenderla in esecuzione la rimetterebbe a mercato
    /// senza che nessuno l'abbia chiesto.
    /// </summary>
    [Fact]
    public void UnaSessioneFermataNonSiRiprende()
    {
        var primo = NewService();
        var aperta = Open(primo);
        primo.SetStatus(aperta.SessionId, aperta.SessionToken, TradingSessionStatus.Stopped);

        var secondo = NewService();
        var esito = Assert.Single(secondo.RestoreSessions());

        Assert.False(esito.Restored);
        Assert.Empty(secondo.ListSessions());
    }

    /// <summary>
    /// Il presidio deve poter dire che le posizioni elencate vengono da un file e non da una lettura
    /// del conto, e deve smettere di dirlo appena il cBot ricomincia a spingere barre.
    /// </summary>
    [Fact]
    public void IlPresidioSegnalaLaRipresaFinoAllaPrimaBarra()
    {
        var primo = NewService();
        var aperta = Open(primo);
        var intent = PushBarAndClaim(primo, aperta);
        Fill(primo, aperta, intent);

        var secondo = NewService();
        secondo.RestoreSessions();

        var subito = secondo.GetAccountWatch(Account);
        Assert.Contains(subito.Rilievi, r => r.Finding == RealtimeWatchFinding.SessioneRipresaSenzaFlusso);
        Assert.NotNull(Assert.Single(subito.Sessioni).RipresaDaDumpAtUtc);

        // Le barre della fixture partono da un'origine del 2026 e sono più vecchie della ripresa:
        // per far scadere il rilievo serve una barra successiva a "adesso meno un attimo".
        PushBar(secondo, aperta, DateTime.UtcNow.AddMinutes(1));

        var dopo = secondo.GetAccountWatch(Account);
        Assert.DoesNotContain(dopo.Rilievi, r => r.Finding == RealtimeWatchFinding.SessioneRipresaSenzaFlusso);
    }

    // ------------------------------------------------------------------------------ infrastruttura

    private void SavePlan(string workspaceId, AccountHoldingPolicy holding) =>
        new TradingPlanService(_workspaces).Save(workspaceId, new SaveTradingPlanRequest
        {
            Code = PlanCode,
            Name = "Piano ripresa",
            AccountNumber = Account,
            Holding = holding
        });

    /// <summary>
    /// Un servizio nuovo sugli stessi workspace: è il riavvio del processo, dove la mappa delle
    /// sessioni riparte vuota e resta solo ciò che è su disco.
    /// </summary>
    private TradingSessionService NewService() => new(
        _workspaces,
        new TradingPlanService(_workspaces),
        new OneSignalPerBarEvaluationService(_strategy),
        new PositionSizingService());

    private TradingSessionDescriptor Open(TradingSessionService sessions) =>
        sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = PlanCode,
            ClientRunMode = ClientRunMode.Realtime,
            ExecutionKey = ExecutionKey,
            AccountNumber = Account,
            // Esecuzione diretta: POST /bars restituisce intent già assegnati, senza passare dal
            // claim. È il percorso più corto per avere una posizione aperta da riprendere.
            DistributeToAccounts = false
        });

    private int _bar;

    private OrderIntent PushBarAndClaim(TradingSessionService sessions, TradingSessionDescriptor descriptor)
    {
        var response = PushBar(sessions, descriptor, Origin.AddMinutes(++_bar * _strategy.TimeframeMinutes));
        return Assert.Single(response.Intents);
    }

    private PushBarsResponse PushBar(
        TradingSessionService sessions, TradingSessionDescriptor descriptor, DateTime barTimeUtc) =>
        sessions.PushBars(new PushBarsRequest
        {
            SessionId = descriptor.SessionId,
            SessionToken = descriptor.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = _strategy.Symbol,
                    TimeframeMinutes = _strategy.TimeframeMinutes,
                    BarTimeUtc = barTimeUtc,
                    Sequence = barTimeUtc.Ticks,
                    IdempotencyKey = $"bar-{_strategy.Symbol}-{barTimeUtc:O}",
                    Bar = new OhlcvData
                    {
                        DateTime = barTimeUtc, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1
                    }
                }
            ]
        });

    private static void Fill(
        TradingSessionService sessions, TradingSessionDescriptor descriptor, OrderIntent intent) =>
        sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = $"r-{Guid.NewGuid():N}",
                IntentId = intent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = intent.FinalQuantity > 0 ? intent.FinalQuantity : 1m,
                FillPrice = 100m,
                EventTimeUtc = Origin
            }
        });

    /// <summary>Un segnale per barra sulla strategia della fixture, così ogni push produce un intent.</summary>
    private sealed class OneSignalPerBarEvaluationService(StrategyDefinition definition) : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var strategy = strategies.FirstOrDefault(x =>
                string.Equals(x.Symbol, closedBar.Symbol, StringComparison.OrdinalIgnoreCase));
            if (strategy is null) return [];

            return
            [
                new TradeSignal
                {
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Symbol = definition.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy,
                    Price = closedBar.Bar.Close,
                    Quantity = 1m
                }
            ];
        }
    }
}
