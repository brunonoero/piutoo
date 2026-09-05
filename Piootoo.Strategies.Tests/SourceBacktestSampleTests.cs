using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il campione sorgente: un backtest con <c>EnforceConcurrencyLimits = false</c> deve eseguire
/// <b>tutte</b> le strategie del masterfilter: è il <c>trades.json</c> di riferimento con cui si
/// confronta ogni run filtrato. I due livelli di filtro vanno tenuti separati:
///
/// <list type="number">
/// <item><b>La strategia</b> — <c>MaxEntriesPerSession</c>. Sempre rispettato, in ogni profilo: è
/// una regola del motore, non della piattaforma.</item>
/// <item><b>La piattaforma</b> — <c>MaxConcurrentTrades</c>, slot di gruppo,
/// <c>AccountHasEntryInFlight</c>. Sono i vincoli operativi del setup del server, e nel run sorgente
/// si spengono: applicarli falserebbe il campione.</item>
/// </list>
///
/// <para>Il caso che ha portato a questi test è un backtest sorgente NQ del 17/03/2026: nove
/// template di ingresso per barra, un solo claim servito, e per tutti gli altri <i>l'account ha già
/// un ingresso in corso per quella strategia su quel simbolo</i>. Il lucchetto di identità era
/// incondizionato (livello 2 applicato in un profilo che lo spegne) e nessuno annullava gli intent
/// reclamati e mai eseguiti, che restavano <c>Pending</c> per il resto del run.</para>
///
/// <para>Riferimenti: <c>docs/domini/distribuzione-multi-account.md</c> §4.3,
/// <c>TradingSessionService.PurgeExpiredEntryIntents</c>.</para>
/// </summary>
public sealed class SourceBacktestSampleTests : IDisposable
{
    private static readonly DateTime Origin = new(2026, 3, 17, 8, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-sorgente-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // ------------------------------------------------------- il campione sorgente è completo

    [Fact]
    public void WithoutOperationalLocks_EveryStrategyOfTheBarIsServed()
    {
        // La regressione: tre strategie sullo stesso simbolo, un account che drena. Deve portarsi via
        // tutti e tre i template della barra, non uno.
        var f = New(strategies: 3, enforceConcurrencyLimits: false);

        f.PushBar();

        var claimed = f.Drain("1001");

        Assert.Equal(3, claimed.Count);
        Assert.Equal(3, claimed.Select(x => x.StrategyCode).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void WithoutOperationalLocks_TheStrategyIsServedAgainOnEveryBar()
    {
        // Barra dopo barra il campione non si degrada: gli stop della barra precedente scadono, e la
        // stessa strategia torna a mercato con il livello ricalcolato. È il punto in cui il run
        // vecchio si spegneva dopo il primo claim.
        var f = New(strategies: 3, enforceConcurrencyLimits: false);

        for (var bar = 0; bar < 5; bar++)
        {
            f.PushBar();
            Assert.Equal(3, f.Drain("1001").Count);
        }
    }

    [Fact]
    public void WithTheLocksOn_TheAccountStillGetsOneEntryPerStrategyAtATime()
    {
        // La controprova: con i vincoli operativi attivi il lucchetto di identità torna a mordere
        // entro la barra. Un ingresso per coppia (strategia, simbolo), come in produzione.
        var f = New(strategies: 3, enforceConcurrencyLimits: true);

        f.PushBar();
        var first = f.Drain("1001");
        Assert.Equal(3, first.Count);           // tre strategie diverse: nessuna coppia ripetuta

        // Stessa barra ripetuta: i template nuovi esistono, ma le tre coppie sono tutte in volo.
        f.PushBarWithoutExpiry();
        Assert.Empty(f.Drain("1001"));
    }

    // ------------------------------------------- la scadenza libera i lucchetti (livello 2)

    // Nota sulla finestra: un ordine "next bar" dichiara ExpiresAtUtc = apertura della barra
    // successiva, e il confronto è conservativo (`>= now`), quindi resta vivo *durante* quella barra
    // e muore su quella dopo. Sono due PushBar, non uno, e non è un dettaglio del test: è la
    // convenzione descritta in docs/domini/orologio-barre-e-fill.md, che tiene in vita un template un
    // po' più a lungo invece di scartarne uno ancora valido.

    [Fact]
    public void AnExpiredEntry_ReleasesTheStrategyOnceItsWindowCloses()
    {
        // Uno stop "next bar" mai toccato dal prezzo: chiusa la finestra l'intent è annullato e la
        // coppia (strategia, simbolo) torna libera. Senza questo lo stesso claim bloccava la strategia
        // per il resto del run, anche a lucchetti accesi.
        var f = New(strategies: 1, enforceConcurrencyLimits: true);

        f.PushBar();
        var first = Assert.Single(f.Drain("1001"));

        f.PushBar();
        Assert.Equal(OrderIntentStatus.Pending, f.Intent(first.IntentId).Status);   // ancora nella finestra
        f.PushBar();

        Assert.Equal(OrderIntentStatus.Cancelled, f.Intent(first.IntentId).Status);
        var second = Assert.Single(f.Drain("1001"));
        Assert.NotEqual(first.IntentId, second.IntentId);
        Assert.Equal(first.StrategyCode, second.StrategyCode);
    }

    [Fact]
    public void AnExpiredEntry_ReleasesTheAccountSlotToo()
    {
        // Lo slot (conto, strategia, simbolo, lato) segue l'intent: se restasse occupato dopo la
        // scadenza, quel conto non prenderebbe mai il template delle barre successive.
        var f = New(strategies: 1, enforceConcurrencyLimits: true);

        f.PushBar();
        Assert.Single(f.Drain("1001"));

        f.PushBar();
        Assert.Empty(f.Drain("1001"));          // slot ancora occupato: l'ordine è nella sua finestra
        f.PushBar();

        Assert.Single(f.Drain("1001"));
    }

    [Fact]
    public void AFilledEntry_IsNotTouchedByTheExpirySweep()
    {
        // La scadenza vale per gli ordini ancora in attesa, non per le posizioni aperte: annullare un
        // ingresso riempito perderebbe la posizione.
        var f = New(strategies: 1, enforceConcurrencyLimits: true);

        f.PushBar();
        var entry = Assert.Single(f.Drain("1001"));
        f.Fill(entry);

        f.PushBar();
        f.PushBar();

        Assert.Equal(OrderIntentStatus.Filled, f.Intent(entry.IntentId).Status);
    }

    [Fact]
    public void ALateFillOnAnExpiredEntry_IsStillRecorded()
    {
        // Annullare lato server non perde un fill reale: se il broker riempie comunque, il report
        // passa e la posizione esiste. È la garanzia che rende sicuro lo sweep.
        var f = New(strategies: 1, enforceConcurrencyLimits: true);

        f.PushBar();
        var entry = Assert.Single(f.Drain("1001"));
        f.PushBar();
        f.PushBar();
        Assert.Equal(OrderIntentStatus.Cancelled, f.Intent(entry.IntentId).Status);

        f.Fill(entry);

        Assert.Equal(OrderIntentStatus.Filled, f.Intent(entry.IntentId).Status);
        Assert.Equal(entry.Quantity, f.Intent(entry.IntentId).FilledQuantity);
    }

    // ------------------------------------- il tetto della strategia vale in ogni profilo

    [Fact]
    public void TheStrategyLimitHoldsEvenWithoutOperationalLocks()
    {
        // Livello 1: la strategia dichiara un ingresso per sessione. Il run sorgente spegne i vincoli
        // della piattaforma, non questo — altrimenti il campione conterrebbe trade che il motore non
        // avrebbe mai fatto.
        var f = New(strategies: 1, enforceConcurrencyLimits: false, maxEntriesPerSession: 1);

        f.PushBar();
        var entry = Assert.Single(f.Drain("1001"));
        f.Fill(entry);

        f.PushBar();

        Assert.Empty(f.Drain("1001"));
    }

    [Fact]
    public void TheStrategyLimitCountsFills_NotUnexecutedOrders()
    {
        // Uno stop non eseguito non consuma il tetto: è il caso normale del Price Channel, che
        // riemette il livello sulla barra dopo.
        var f = New(strategies: 1, enforceConcurrencyLimits: false, maxEntriesPerSession: 1);

        f.PushBar();
        Assert.Single(f.Drain("1001"));

        f.PushBar();

        Assert.Single(f.Drain("1001"));
    }

    [Fact]
    public void TheStrategyLimitStaysPerAccount()
    {
        // Gruppi diversi sono portafogli paralleli sullo stesso flusso: il fill di uno non consuma il
        // tetto dell'altro.
        var f = New(strategies: 1, enforceConcurrencyLimits: false, maxEntriesPerSession: 1,
            accounts: [("g1", "1001"), ("g2", "2001")]);

        f.PushBar();
        f.Fill(Assert.Single(f.Drain("1001")));

        Assert.Single(f.Drain("2001"));
    }

    // ------------------------------------------------------------------------------ helper

    private Fixture New(
        int strategies,
        bool enforceConcurrencyLimits,
        int? maxEntriesPerSession = null,
        IReadOnlyList<(string GroupId, string Account)>? accounts = null)
    {
        // Strategie dello stesso simbolo E dello stesso timeframe: è la forma del masterfilter che ha
        // prodotto il caso reale (nove PTS su NQ a 15 minuti), ed è l'unica in cui una barra sola le
        // mette tutte in valutazione — la sessione instrada le barre per stream (simbolo, timeframe).
        var selected = StrategyFactory.GetRegisteredStrategies()
            .GroupBy(x => (x.Symbol, x.TimeframeMinutes))
            .OrderByDescending(g => g.Count())
            .First()
            .Take(strategies)
            .ToArray();
        Assert.Equal(strategies, selected.Length);

        // Il tetto numerico non è ciò che questi test misurano: resta a zero (illimitato).
        IReadOnlyList<TestAccountRow> rows = (accounts ?? [("g1", "1001")])
            .Select(x => new TestAccountRow(x.Account))
            .ToArray();

        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"sorgente-{Guid.NewGuid():N}",
            StrategiesFilter = selected.Select(x => x.Id).ToList()
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();

        TestAccountRegistry.Register(workspaces, rows);

        var evaluation = new EveryStrategyEveryBarEvaluationService(maxEntriesPerSession);
        var sessions = new TradingSessionService(
            workspaces, evaluation, new PositionSizingService());

        var descriptor = sessions.Create(new CreateTradingSessionRequest
        {
            WorkspaceId = workspace.Id,
            ExecutionMode = ExecutionMode.ExternalBroker,
            ClientRunMode = ClientRunMode.Backtest,
            EnforceConcurrencyLimits = enforceConcurrencyLimits
        });
        sessions.SetSessionAccounts(
            descriptor.SessionId, descriptor.SessionToken, TestSessionAccounts.Numbers(rows));
        sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);

        return new Fixture(sessions, descriptor, selected[0], evaluation);
    }

    private sealed class Fixture(
        TradingSessionService sessions,
        TradingSessionDescriptor descriptor,
        StrategyDefinition strategy,
        EveryStrategyEveryBarEvaluationService evaluation)
    {
        private int _bar;

        /// <summary>Una barra che produce un template per strategia, valido fino alla barra dopo.</summary>
        public void PushBar() => Push(withExpiry: true);

        /// <summary>
        /// Una barra i cui template non scadono: serve a osservare due template della stessa coppia
        /// vivi insieme, che è la condizione in cui il lucchetto di identità deve mordere.
        /// </summary>
        public void PushBarWithoutExpiry() => Push(withExpiry: false);

        private void Push(bool withExpiry)
        {
            var barTime = Origin.AddMinutes(_bar++ * strategy.TimeframeMinutes);
            evaluation.ExpireAtNextBar = withExpiry;
            sessions.PushBars(new PushBarsRequest
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
            });
        }

        /// <summary>
        /// Il drenaggio del cBot: polla finché il server ha qualcosa da consegnare. Con i vincoli
        /// operativi spenti è l'unico modo di raccogliere il campione — fermarsi al primo intent
        /// significherebbe eseguire una strategia per barra.
        /// </summary>
        public List<OrderIntent> Drain(string account)
        {
            var claimed = new List<OrderIntent>();
            for (var guard = 0; guard < 50; guard++)
            {
                var response = sessions.GetNextSignalForAccount(
                    descriptor.SessionId, descriptor.SessionToken, account);
                if (response.Intent is not { } intent) return claimed;
                if (claimed.Any(x => x.IntentId == intent.IntentId)) return claimed;  // ripresa di un pendente
                claimed.Add(intent);
            }

            Assert.Fail("il drenaggio non termina: il claim continua a consegnare intent nuovi.");
            return claimed;
        }

        public OrderIntent Intent(string intentId) =>
            sessions.GetIntents(descriptor.SessionId, descriptor.SessionToken)
                .Single(x => x.IntentId == intentId);

        public void Fill(OrderIntent intent) =>
            sessions.ApplyReport(descriptor.SessionId, new ExecutionReportRequest
            {
                SessionToken = descriptor.SessionToken,
                Report = new ExternalExecutionReport
                {
                    ReportId = $"r-{Guid.NewGuid():N}",
                    IntentId = intent.IntentId,
                    Status = ExecutionReportStatus.Filled,
                    CumulativeFilledQuantity = intent.Quantity,
                    FillPrice = 100m,
                    EventTimeUtc = Origin
                }
            });
    }

    /// <summary>
    /// Ogni strategia della sessione emette un ingresso a ogni barra, come fa un masterfilter di
    /// motori breakout: uno stop sopra il mercato valido per la barra successiva.
    /// </summary>
    private sealed class EveryStrategyEveryBarEvaluationService(int? maxEntriesPerSession)
        : IStrategyEvaluationService
    {
        public bool ExpireAtNextBar { get; set; } = true;

        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot)
        {
            var nextBar = closedBar.BarTimeUtc.AddMinutes(closedBar.TimeframeMinutes);

            return strategies
                .Where(x => string.Equals(x.Symbol, closedBar.Symbol, StringComparison.OrdinalIgnoreCase))
                .Select(strategy => new TradeSignal
                {
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Symbol = strategy.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy,
                    OrderType = TradeOrderType.Stop,
                    Quantity = 1m,
                    Price = closedBar.Bar.Close + 1m,
                    ValidFromUtc = nextBar,
                    ExpiresAtUtc = ExpireAtNextBar ? nextBar : null,
                    MaxEntriesPerSession = maxEntriesPerSession,
                    EntrySessionStartUtc = maxEntriesPerSession is null ? null : (DateTime?)Origin.Date
                })
                .ToArray();
        }
    }
}
