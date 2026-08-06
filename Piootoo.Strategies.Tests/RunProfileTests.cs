using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il profilo del run (<see cref="TradingRunProfile"/>): l'interruttore con cui il cBot dichiara
/// QUALE backtest sta aprendo, invece di farlo dedurre da <c>ApplyTitanoFilters</c> nel piano più
/// <c>EnforceConcurrencyLimits</c> nella sessione.
///
/// <para>Il comportamento che questi test bloccano è quello che rendeva incomparabili il backtest
/// del cBot distribuito e il backtest interno: i lucchetti di distribuzione — passo 1 (un solo
/// intent pendente per account), slot gruppo/strategia/simbolo, lucchetto account/simbolo — sono
/// vincoli OPERATIVI, e il campione sorgente su cui Titano calcola le rotazioni non deve averli.
/// Prima seguivano solo <c>MaxConcurrentTrades</c>, quindi restavano attivi anche nel run sorgente e
/// ne mutilavano i segnali in silenzio.</para>
///
/// <para>Il lucchetto che NON deve mai spegnersi è <c>TemplateClaimedGroups</c>: non limita quanto
/// si opera in parallelo, dice che un template è già stato servito a quel gruppo. Senza, il cBot che
/// drena la coda riceverebbe lo stesso segnale all'infinito. Vedi
/// <c>docs/domini/distribuzione-multi-account.md</c> §2 e §4.</para>
/// </summary>
public sealed class RunProfileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-profile-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // --------------------------------------------------------------- risoluzione del profilo

    [Fact]
    public void BacktestSorgente_SpegneTitanoEILucchettiDiConcorrenza()
    {
        var f = New(applyTitanoFilters: true, maxConcurrent: 3);

        var descriptor = f.Open(TradingRunProfile.BacktestSorgente);

        // Il profilo prevale sul piano: il piano chiedeva il filtro Titano e un limite di 3 trade.
        Assert.Equal(TitanoFilterMode.Disabled, descriptor.TitanoMode);
        Assert.False(descriptor.EnforceConcurrencyLimits);
        Assert.Equal(TradingRunProfile.BacktestSorgente, descriptor.RunProfile);
    }

    [Fact]
    public void BacktestTitano_TieneLeRotazioniEIVincoliOperativi()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 3, titanoFolder: "source");

        var descriptor = f.Open(TradingRunProfile.BacktestTitano);

        // Anche qui il profilo prevale: il piano diceva "niente filtro".
        Assert.Equal(TitanoFilterMode.BacktestRotationFile, descriptor.TitanoMode);
        Assert.True(descriptor.EnforceConcurrencyLimits);
        Assert.Equal(3, descriptor.MaxConcurrentTrades);
    }

    [Fact]
    public void DalPiano_ConservaIlComportamentoStorico()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 3);

        var descriptor = f.Open(TradingRunProfile.DalPiano);

        // Default storico: in Backtest senza filtro Titano il limite era già disattivo.
        Assert.Equal(TitanoFilterMode.Disabled, descriptor.TitanoMode);
        Assert.False(descriptor.EnforceConcurrencyLimits);
        Assert.Equal(TradingRunProfile.DalPiano, descriptor.RunProfile);
    }

    // --------------------------------------------------------------- il run sbagliato è rifiutato

    [Fact]
    public void UnProfiloDiBacktest_InRealtimeVieneRifiutato()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 0);

        // Mandare a mercato un run configurato come campione sorgente significa operare senza
        // nessuno dei vincoli che il piano dichiara. Deve fallire all'apertura, non a mercato.
        var error = Assert.Throws<ArgumentException>(() =>
            f.Open(TradingRunProfile.BacktestSorgente, ClientRunMode.Realtime));

        Assert.Contains("solo in backtest", error.Message);
    }

    [Fact]
    public void BacktestTitano_SenzaRotazioniVieneRifiutato()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 0);

        // Senza cartella di run girerebbe identico a un backtest senza filtro, e la differenza si
        // vedrebbe solo confrontando due trades.json mesi dopo.
        var error = Assert.Throws<ArgumentException>(() => f.Open(TradingRunProfile.BacktestTitano));

        Assert.Contains("rotazioni storiche", error.Message);
    }

    [Fact]
    public void ProfiliDiversi_NonSiRiprendonoAVicenda()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 0, titanoFolder: "source");

        // Stessa ExecutionKey, profilo diverso: se la chiave non includesse il profilo, il secondo
        // run riprenderebbe il primo e continuerebbe con il Titano e i lucchetti di quello vecchio.
        var sorgente = f.Open(TradingRunProfile.BacktestSorgente);
        var titano = f.Open(TradingRunProfile.BacktestTitano);

        Assert.NotEqual(sorgente.SessionId, titano.SessionId);
        Assert.Equal(TitanoFilterMode.Disabled, sorgente.TitanoMode);
        Assert.Equal(TitanoFilterMode.BacktestRotationFile, titano.TitanoMode);
    }

    // --------------------------------------------------------------- il drenaggio della coda

    [Fact]
    public void BacktestSorgente_UnAccountReclamaTuttiISegnaliDellaBarra()
    {
        // Tutte le strategie sono sullo stesso simbolo e timeframe: con i lucchetti attivi
        // l'account ne otterrebbe UNO solo e resterebbe fermo fino alla chiusura della posizione
        // (lucchetto account/simbolo, che al fill non si libera). È il caso 5a del documento.
        var f = New(applyTitanoFilters: false, maxConcurrent: 1);
        var descriptor = f.Open(TradingRunProfile.BacktestSorgente);
        f.PushBar(descriptor);

        var claimed = f.Drain(descriptor);

        Assert.Equal(f.StrategyCount, claimed.Count);
        // Intent distinti, uno per strategia: il campione sorgente contiene ogni segnale della barra.
        Assert.Equal(claimed.Count, claimed.Select(i => i.IntentId).Distinct().Count());
        Assert.Equal(
            f.StrategyCodes.OrderBy(x => x, StringComparer.Ordinal),
            claimed.Select(i => i.StrategyCode).OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void BacktestSorgente_IlLucchettoDelGruppoRestaAttivo()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 1);
        var descriptor = f.Open(TradingRunProfile.BacktestSorgente);
        f.PushBar(descriptor);

        f.Drain(descriptor);

        // Svuotata la coda il server deve dire "non ho altro", non ricominciare da capo: è
        // TemplateClaimedGroups a garantirlo, e senza di lui il drenaggio del cBot non finirebbe.
        var extra = f.Poll(descriptor);
        Assert.Null(extra.Intent);
    }

    [Fact]
    public void BacktestSorgente_NonConsegnaDueIngressiDellaStessaStrategia()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 1);
        var descriptor = f.Open(TradingRunProfile.BacktestSorgente);

        f.PushBar(descriptor);
        var prima = f.Drain(descriptor);
        Assert.Equal(f.StrategyCount, prima.Count);

        // Seconda barra: gli stessi codici segnalano di nuovo, ma gli ordini della prima sono
        // ancora sul broker, non riempiti. `MaxEntriesPerSession` conta i fill e quindi non li
        // vede: senza questo filtro il claim li consegnerebbe, e il broker aprirebbe due posizioni
        // sullo stesso motivo di ingresso. È successo davvero — PTS_NQ_PCH_002_15, 14/10/2024
        // 13:15, due stop order riempiti allo stesso prezzo.
        f.PushBar(descriptor);
        var seconda = f.Drain(descriptor);

        Assert.Empty(seconda);
    }

    [Fact]
    public void ConILucchettiAttivi_LAccountNeOttieneUnoSolo()
    {
        // Il contrappunto del test precedente: stessa barra, stesse strategie, profilo diverso.
        var f = New(applyTitanoFilters: false, maxConcurrent: 1, titanoFolder: "source");
        var descriptor = f.Open(TradingRunProfile.BacktestTitano);
        f.PushBar(descriptor);

        var first = f.Poll(descriptor);
        Assert.NotNull(first.Intent);

        // Il passo 1 ripropone lo stesso intent invece di consegnarne un secondo.
        var second = f.Poll(descriptor);
        Assert.Equal(first.Intent!.IntentId, second.Intent?.IntentId);
    }

    // --------------------------------------------------------------- la guardia sul poll

    [Fact]
    public void IlPushDichiaraQuantoCEDaReclamare()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 0);
        var descriptor = f.Open(TradingRunProfile.BacktestSorgente);

        // Riscaldamento: il server accoda e non valuta, quindi non c'è ancora niente da reclamare.
        var warmUp = f.PushWindow(descriptor, evaluateLastCandle: false);
        Assert.Equal(0, warmUp.ClaimableIntents);

        // Barra valutata: un template per strategia, tutti reclamabili.
        var evaluated = f.PushWindow(descriptor, evaluateLastCandle: true);

        // È la garanzia su cui il cBot salta il poll: zero deve voler dire davvero zero, altrimenti
        // salterebbe una chiamata che aveva qualcosa da consegnare e il segnale sparirebbe in
        // silenzio. Il conteggio è quindi allineato a ciò che il claim trova.
        Assert.Equal(f.StrategyCount, evaluated.ClaimableIntents);
        Assert.NotNull(f.Poll(descriptor).Intent);
    }

    // --------------------------------------------------------------- il descriptor per il pannello

    [Fact]
    public void IlDescriptorElencaLeStrategieConSimboloETimeframe()
    {
        var f = New(applyTitanoFilters: false, maxConcurrent: 0);

        var descriptor = f.Open(TradingRunProfile.BacktestSorgente);

        // È quello che il cBot stampa a chart: senza, un bot che esegue un piano diverso da quello
        // che dichiara è indistinguibile da uno che funziona finché non si leggono i trade.
        Assert.Equal(f.StrategyCount, descriptor.Strategies.Count);
        Assert.All(descriptor.Strategies, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.StrategyCode));
            Assert.False(string.IsNullOrWhiteSpace(s.Symbol));
            Assert.True(s.TimeframeMinutes > 0);
        });
        Assert.Equal(
            f.StrategyCodes.OrderBy(x => x, StringComparer.Ordinal),
            descriptor.Strategies.Select(s => s.StrategyCode).OrderBy(x => x, StringComparer.Ordinal));
    }

    // --------------------------------------------------------------------------------- fixture

    /// <summary>
    /// Un piano su un solo account e un masterfilter di strategie tutte sulla stessa coppia
    /// (simbolo, timeframe): è la configurazione in cui i lucchetti mordono di più, quindi quella in
    /// cui la differenza fra i profili si misura meglio.
    /// </summary>
    private Fixture New(bool applyTitanoFilters, int maxConcurrent, string? titanoFolder = null)
    {
        var selected = StrategyFactory.GetRegisteredStrategies()
            .GroupBy(x => (Symbol: x.Symbol.Trim().TrimStart('@').ToUpperInvariant(), x.TimeframeMinutes))
            .OrderByDescending(g => g.Count())
            .First()
            .Take(3)
            .ToArray();
        Assert.True(selected.Length > 1,
            "serve più di una strategia sulla stessa coppia simbolo/timeframe per osservare i lucchetti");

        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"profilo-{Guid.NewGuid():N}",
            StrategiesFilter = selected.Select(x => x.Id).ToList()
        });
        new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, "source")).Initialize();
        TestAccountRegistry.Register(workspaces, "1001");

        var plans = new TradingPlanService(workspaces);
        plans.Save(workspace.Id, new SaveTradingPlanRequest
        {
            Code = "PLANPROFILO",
            Name = "Piano profilo",
            GroupId = "g1",
            AccountNumber = "1001",
            MaxConcurrentTrades = maxConcurrent,
            ApplyTitanoFilters = applyTitanoFilters,
            TitanoBacktestFolder = titanoFolder
        });

        var sessions = new TradingSessionService(
            workspaces, plans, new AllStrategiesEvaluationService(), positionSizing: new PositionSizingService());

        return new Fixture(sessions, selected);
    }

    private sealed class Fixture(TradingSessionService sessions, IReadOnlyList<StrategyDefinition> strategies)
    {
        private static readonly DateTime Origin = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        private int _bar;

        public int StrategyCount => strategies.Count;
        public IEnumerable<string> StrategyCodes => strategies.Select(x => x.Name);

        public TradingSessionDescriptor Open(
            TradingRunProfile profile, ClientRunMode runMode = ClientRunMode.Backtest) =>
            sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
            {
                PlanCode = "PLANPROFILO",
                ClientRunMode = runMode,
                // Volutamente la STESSA chiave per ogni apertura: è così che si verifica che il
                // profilo entri nell'identità dell'esecuzione (ProfiliDiversi_NonSiRiprendonoAVicenda).
                ExecutionKey = "run-1",
                RunProfile = profile
            });

        /// <summary>Una barra sulla coppia comune: un segnale per ciascuna strategia.</summary>
        public void PushBar(TradingSessionDescriptor descriptor)
        {
            var strategy = strategies[0];
            // Ogni chiamata avanza di una barra: due push allo stesso istante sarebbero un
            // duplicato e il secondo verrebbe scartato dall'idempotenza.
            var barTime = Origin.AddMinutes(++_bar * strategy.TimeframeMinutes);
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
        /// La stessa barra spedita come finestra, che è la via che usa il cBot. Con
        /// <paramref name="evaluateLastCandle"/> a false è solo riscaldamento: il server accoda e non
        /// valuta, quindi non consuma la chiave di idempotenza e la stessa barra può tornare dopo.
        /// </summary>
        public PushBarWindowResponse PushWindow(
            TradingSessionDescriptor descriptor, bool evaluateLastCandle)
        {
            var strategy = strategies[0];
            var barTime = Origin.AddMinutes(++_bar * strategy.TimeframeMinutes);
            return sessions.PushBarWindow(new PushBarWindowRequest
            {
                SessionId = descriptor.SessionId,
                SessionToken = descriptor.SessionToken,
                Windows =
                [
                    new ClosedBarWindow
                    {
                        Symbol = strategy.Symbol,
                        TimeframeMinutes = strategy.TimeframeMinutes,
                        Sequence = barTime.Ticks,
                        IdempotencyKey = $"win-{barTime:O}",
                        EvaluateLastCandle = evaluateLastCandle,
                        Candles =
                        [
                            new OhlcvData
                            {
                                DateTime = barTime, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1
                            }
                        ]
                    }
                ]
            });
        }

        public AccountSignalResponse Poll(TradingSessionDescriptor descriptor) =>
            sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");

        /// <summary>
        /// Quello che fa il cBot a lucchetti spenti: reclama finché il server ha qualcosa. Il tetto
        /// riproduce <c>MaxSignalsPerDrain</c> e serve a far fallire il test invece di appenderlo se
        /// un lucchetto che deve restare attivo venisse spento.
        /// </summary>
        public List<OrderIntent> Drain(TradingSessionDescriptor descriptor)
        {
            var claimed = new List<OrderIntent>();
            for (var i = 0; i < 200; i++)
            {
                var response = Poll(descriptor);
                if (response.Intent is null)
                    return claimed;
                claimed.Add(response.Intent);
            }

            Assert.Fail("il drenaggio non termina: il server continua a consegnare intent.");
            return claimed;
        }
    }

    /// <summary>Un segnale per ogni strategia valutata: una barra = tutti i template della barra.</summary>
    private sealed class AllStrategiesEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) =>
            strategies.Select(strategy => new TradeSignal
            {
                StrategyCode = strategy.Name,
                StrategyName = strategy.Name,
                Symbol = strategy.Symbol,
                Date = closedBar.BarTimeUtc,
                Type = SignalType.Buy,
                Quantity = 1m,
                Price = closedBar.Bar.Close
            }).ToArray();
    }
}
