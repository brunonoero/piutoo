using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Macchina a stati della rotazione: isteresi, cooldown, MinimumOnPeriods, latch dello hard stop e
/// sizing per percentile. Era l'unica parte stateful del sistema senza copertura, ed è quella in cui
/// si annidavano B4 (stato Enabled irraggiungibile) e B5 (reset che scavalcava i cancelli).
/// </summary>
public sealed class TitanoStateMachineTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"titano-fsm-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // ---------------------------------------------------------------- percentile e allocazione

    [Fact]
    public void ZScoreVoteScore_IsContinuous_SoTiesDoNotCollapseThePercentile()
    {
        var request = Request() with { MinimumZScore = -1.5m, MaximumZScore = 2.5m };

        // Centro banda (0,5) = punteggio pieno; estremi = 0; fuori banda = 0.
        Assert.Equal(1m, TitanoRotationService.ZScoreVoteScore(0.5m, request));
        Assert.Equal(0m, TitanoRotationService.ZScoreVoteScore(-1.5m, request));
        Assert.Equal(0m, TitanoRotationService.ZScoreVoteScore(2.5m, request));
        Assert.Equal(0m, TitanoRotationService.ZScoreVoteScore(9m, request));

        // Due z distinti dentro banda devono produrre punteggi distinti: è ciò che permette a una
        // strategia di raggiungere il percentile 1 e quindi l'allocazione massima.
        Assert.NotEqual(
            TitanoRotationService.ZScoreVoteScore(0.4m, request),
            TitanoRotationService.ZScoreVoteScore(1.9m, request));
    }

    [Fact]
    public void MaximumAllocation_FollowsConfiguredCap_NotTheConstantOne()
    {
        Assert.Equal(1m, TitanoRotationService.MaximumAllocation(Request()));
        Assert.Equal(0.8m, TitanoRotationService.MaximumAllocation(
            Request() with { MaximumAllocationMultiplier = 0.8m }));

        // Senza percentile il tetto è il tier più alto.
        Assert.Equal(1m, TitanoRotationService.MaximumAllocation(
            Request() with { CrossSectionalSizing = false }));
    }

    [Theory]
    // percentile, tetto atteso -> stato
    [InlineData(1.0, TitanoStrategyStatus.Enabled)]
    [InlineData(0.5, TitanoStrategyStatus.Reduced)]
    [InlineData(0.0, TitanoStrategyStatus.Reduced)]
    public void ClassifyStatus_UsesTheConfiguredCap(double score, TitanoStrategyStatus expected)
    {
        var request = Request();
        var allocation = TitanoRotationService.ComputeAllocation((decimal)score, request);
        Assert.Equal(expected, TitanoRotationService.ClassifyStatus(allocation, hardStopped: false, request));
    }

    [Fact]
    public void ClassifyStatus_HardStopAndZeroAllocationAreDistinct()
    {
        var request = Request();
        Assert.Equal(TitanoStrategyStatus.HardStopped,
            TitanoRotationService.ClassifyStatus(0m, hardStopped: true, request));
        Assert.Equal(TitanoStrategyStatus.Disabled,
            TitanoRotationService.ClassifyStatus(0m, hardStopped: false, request));
    }

    [Fact]
    public void AllocationCapIsReachable_AndProducesTheEnabledState()
    {
        // Regressione di B4. Con una sola strategia il rango non ha significato e il percentile vale
        // 1 per definizione: l'allocazione deve quindi arrivare al tetto e lo stato essere Enabled.
        // Prima il confronto era con la costante 1 e, appena il tetto era diverso da 1, Enabled non
        // compariva mai.
        var manifest = RunSingleStrategy(
            trades: [(Utc(2026, 1, 6), 100m), (Utc(2026, 1, 13), 100m), (Utc(2026, 1, 20), 100m)],
            configure: request => request with { MaximumAllocationMultiplier = 0.80m });

        var cap = TitanoRotationService.MaximumAllocation(manifest.Config);
        Assert.Equal(0.80m, cap);

        var enabled = manifest.Periods
            .SelectMany(period => period.Strategies)
            .Where(state => state.Enabled)
            .ToList();

        Assert.NotEmpty(enabled);
        Assert.All(enabled, state =>
        {
            Assert.Equal(cap, state.AllocationMultiplier);
            Assert.Equal(TitanoStrategyStatus.Enabled, state.State);
        });
    }

    [Fact]
    public void PercentileDiscriminatesBetweenStrategies_AndRawScoreStaysAbsolute()
    {
        // La leader è regolare e sempre positiva, la laggard è erratica: differiscono su performance,
        // drawdown e volatilità. Il punto del test non è chi vince, ma che il percentile PRODUCA
        // punteggi distinti — prima i pari merito sullo z-score binario appiattivano il confronto.
        var manifest = RunWithTwoStrategies(
            leaderNetProfits: [100m, 100m, 100m],
            laggardNetProfits: [5m, -80m, 60m]);

        var lastPeriod = manifest.Periods.MaxBy(period => period.EffectiveFromUtc)!;
        Assert.Equal(2, lastPeriod.Strategies.Count);

        var scores = lastPeriod.Strategies.Select(state => state.Score).ToList();
        Assert.NotEqual(scores[0], scores[1]);
        Assert.All(lastPeriod.Strategies, state => Assert.InRange(state.Score, 0m, 1m));

        // RawScore è la media dei voti assoluti: resta valorizzato anche col percentile, altrimenti
        // "è la peggiore del gruppo" e "va male" diventano indistinguibili.
        Assert.All(lastPeriod.Strategies, state => Assert.NotEqual(0m, state.RawScore));
        Assert.NotEqual(
            lastPeriod.Strategies[0].RawScore,
            lastPeriod.Strategies[1].RawScore);
    }

    // ---------------------------------------------------------------- isteresi e cooldown

    [Fact]
    public void CooldownHoldsTheStrategyOff_ForTheConfiguredNumberOfPeriods()
    {
        // Perdita larga a inizio storia: la strategia va OFF e deve restare ferma per il cooldown.
        var manifest = RunSingleStrategy(
            trades:
            [
                (Utc(2026, 1, 6), 40m),
                (Utc(2026, 1, 13), -600m),
                (Utc(2026, 1, 20), 10m),
                (Utc(2026, 1, 27), 10m),
                (Utc(2026, 2, 3), 10m)
            ],
            configure: request => request with
            {
                CooldownPeriodsAfterOff = 2, MinimumOnPeriods = 0, MaximumCurrentDrawdown = 0.05m,
                ReenableMaximumCurrentDrawdown = 0.02m, HardStopDrawdown = 0.90m
            });

        var timeline = manifest.Periods
            .OrderBy(period => period.EffectiveFromUtc)
            .Select(period => period.Strategies.Single())
            .ToList();

        var firstOff = timeline.FindIndex(state => !state.Enabled);
        Assert.True(firstOff >= 0, "La strategia doveva spegnersi dopo la perdita.");

        // Al periodo dello spegnimento il contatore è armato al valore configurato e poi scende.
        Assert.Equal(2, timeline[firstOff].CooldownRemaining);
        if (firstOff + 1 < timeline.Count)
        {
            Assert.False(timeline[firstOff + 1].Enabled);
            Assert.Equal(1, timeline[firstOff + 1].CooldownRemaining);
        }
        if (firstOff + 2 < timeline.Count)
            Assert.Equal(0, timeline[firstOff + 2].CooldownRemaining);
    }

    [Fact]
    public void MinimumOnPeriods_DelaysTheFirstShutdown()
    {
        (DateTime Exit, decimal Net)[] trades =
        [
            (Utc(2026, 1, 6), 50m),
            (Utc(2026, 1, 13), -400m),
            (Utc(2026, 1, 20), -50m),
            (Utc(2026, 1, 27), -50m)
        ];
        var strict = RunSingleStrategy(trades, r => r with
        {
            MinimumOnPeriods = 0, MaximumCurrentDrawdown = 0.05m,
            ReenableMaximumCurrentDrawdown = 0.02m, HardStopDrawdown = 0.90m
        });
        var lenient = RunSingleStrategy(trades, r => r with
        {
            MinimumOnPeriods = 5, MaximumCurrentDrawdown = 0.05m,
            ReenableMaximumCurrentDrawdown = 0.02m, HardStopDrawdown = 0.90m
        });

        var strictOn = strict.Periods.Count(p => p.Strategies.Single().Enabled);
        var lenientOn = lenient.Periods.Count(p => p.Strategies.Single().Enabled);

        // Un MinimumOnPeriods alto trattiene lo spegnimento: non può produrre MENO periodi accesi.
        Assert.True(lenientOn >= strictOn,
            $"MinimumOnPeriods alto ha ridotto i periodi ON ({lenientOn} < {strictOn}).");
    }

    [Fact]
    public void HardStopIsLatched_AndDoesNotArmTheCooldown()
    {
        var manifest = RunSingleStrategy(
            trades:
            [
                (Utc(2026, 1, 6), 100m),
                (Utc(2026, 1, 13), -800m),
                (Utc(2026, 1, 20), 500m),
                (Utc(2026, 1, 27), 500m),
                (Utc(2026, 2, 3), 500m)
            ],
            configure: request => request with
            {
                HardStopDrawdown = 0.30m, MaximumCurrentDrawdown = 0.15m,
                ReenableMaximumCurrentDrawdown = 0.10m, CooldownPeriodsAfterOff = 2
            });

        var timeline = manifest.Periods
            .OrderBy(period => period.EffectiveFromUtc)
            .Select(period => period.Strategies.Single())
            .ToList();

        var stopped = timeline.FindIndex(state => state.HardStopped);
        Assert.True(stopped >= 0, "Il drawdown doveva far scattare l'hard stop.");

        // Latched: una volta scattato non si sblocca da solo, nemmeno recuperando l'equity.
        Assert.All(timeline.Skip(stopped), state =>
        {
            Assert.True(state.HardStopped);
            Assert.False(state.Enabled);
            Assert.Equal(0m, state.AllocationMultiplier);
            Assert.Equal(TitanoStrategyStatus.HardStopped, state.State);
            // Regressione di B9: un cooldown che scende a zero faceva leggere come "libera di
            // rientrare" una strategia bloccata a tempo indeterminato.
            Assert.Equal(0, state.CooldownRemaining);
        });
    }

    [Fact]
    public void AnomalyFlags_AreEmptyOnAHealthyRun()
    {
        // Regressione di B10: MinimumOnPeriods che tratteneva uno spegnimento veniva segnalato come
        // anomalia, cioè il caso più frequente faceva rumore e il campo perdeva significato.
        var manifest = RunSingleStrategy(
            trades: [(Utc(2026, 1, 6), 100m), (Utc(2026, 1, 13), 80m), (Utc(2026, 1, 20), 60m)],
            configure: request => request with { MinimumOnPeriods = 5 });

        Assert.All(manifest.Periods.SelectMany(period => period.Strategies),
            state => Assert.Empty(state.AnomalyFlags));
    }

    // ---------------------------------------------------------------- reset dello hard stop

    [Fact]
    public void HardStopReset_DoesNotReadmitAStrategyThatStillFailsTheGates()
    {
        // Regressione di B5: il reset ricalcolava l'allocazione dal solo score e, col sizing per
        // percentile, ComputeAllocation restituisce almeno il pavimento per QUALUNQUE punteggio.
        // Una strategia con drawdown enorme tornava operativa al 25% appena resettata.
        var (workspaces, workspaceId, folder, code) = Workspace();
        WriteTrades(workspaces, workspaceId, folder, code,
        [
            (Utc(2026, 1, 6), 100m),
            (Utc(2026, 1, 13), -900m),
            (Utc(2026, 1, 20), -10m),
            (Utc(2026, 1, 27), -10m)
        ]);
        var rotation = new TitanoRotationService(workspaces);
        var manifest = rotation.Run(BaseRequest(workspaceId, folder) with
        {
            HardStopDrawdown = 0.30m, MaximumCurrentDrawdown = 0.15m,
            ReenableMaximumCurrentDrawdown = 0.10m
        });

        var stoppedPeriod = manifest.Periods
            .OrderBy(period => period.EffectiveFromUtc)
            .FirstOrDefault(period => period.Strategies.Any(state => state.HardStopped));
        Assert.NotNull(stoppedPeriod);

        rotation.ResetHardStop(workspaceId, folder, manifest.RunId, new TitanoHardStopResetRequest
        {
            StrategyCode = code, RequestedBy = "test", Reason = "verifica",
            RequestedAtUtc = stoppedPeriod!.EffectiveFromUtc
        });

        var after = manifest.Periods
            .Where(period => period.EffectiveFromUtc > stoppedPeriod.EffectiveFromUtc)
            .OrderBy(period => period.EffectiveFromUtc)
            .FirstOrDefault();
        Assert.NotNull(after);

        var effective = rotation.Resolve(workspaceId, folder, manifest.RunId, after!.EffectiveFromUtc);
        var state = effective.StrategyStates.Single(x => x.StrategyCode == code);

        // Il latch è tolto...
        Assert.False(state.HardStopped);
        // ...ma i cancelli no: la strategia resta ferma finché non li supera.
        Assert.Equal(0m, state.AllocationMultiplier);
        Assert.Equal(TitanoStrategyStatus.Disabled, state.State);
        Assert.DoesNotContain(code, effective.EffectiveStrategies);
        Assert.Contains("cancelli", state.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- copertura e walk-forward

    [Fact]
    public void ManifestDeclaresTradesOutsideCoverage_SoTheTwoCurvesAreNotConfusedForOneSample()
    {
        // Regressione di B6: i trade entrati nel primo periodo (solo osservazione) contano nella
        // curva originale e non in quella filtrata. Il report li presentava come "eliminati da
        // Titano", che è un'altra cosa.
        var manifest = RunSingleStrategy(
            trades: [(Utc(2026, 1, 6), 100m), (Utc(2026, 1, 13), 50m), (Utc(2026, 1, 20), 25m)],
            configure: request => request);

        Assert.True(manifest.TradesOutsideCoverage > 0,
            "Il primo periodo non è efficace: almeno un trade deve risultare fuori copertura.");

        // I trade fuori copertura sono nella curva originale e non in quella filtrata, quindi la
        // differenza fra le due non può essere inferiore a quel numero.
        Assert.True(
            manifest.OriginalEquity.Count - manifest.FilteredEquity.Count >= manifest.TradesOutsideCoverage,
            $"Originale {manifest.OriginalEquity.Count}, filtrata {manifest.FilteredEquity.Count}, " +
            $"fuori copertura {manifest.TradesOutsideCoverage}.");
    }

    [Fact]
    public void WalkForwardNoteExplainsAnEmptyTable()
    {
        // Regressione di B11: con pochi periodi il walk-forward non veniva calcolato e la tabella
        // vuota era indistinguibile da "nessun problema rilevato".
        var manifest = RunSingleStrategy(
            trades: [(Utc(2026, 1, 6), 100m), (Utc(2026, 1, 13), 50m)],
            configure: request => request with { CalibrationPeriods = 50, EvaluationPeriods = 4 });

        Assert.Empty(manifest.WalkForward);
        Assert.NotEmpty(manifest.WalkForwardNote);
        Assert.Contains("CalibrationPeriods", manifest.WalkForwardNote, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- cache

    [Fact]
    public void ManifestCacheIsBounded_AndKeepsTheMostRecentlyUsedEntry()
    {
        // La cache è statica e vive quanto il processo: senza tetto un server acceso per mesi, su
        // cui ogni cambio di parametro genera un runId nuovo, accumulava manifest da megabyte
        // all'infinito.
        TitanoRotationService.ClearManifestCache();

        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"cache-{Guid.NewGuid():N}", StrategiesFilter = [strategy.Id]
        });
        var code = StrategyCatalog.TryGetExecutionCode(strategy.Id) ?? strategy.Id;
        var rotation = new TitanoRotationService(workspaces);

        var runs = new List<(string Folder, string RunId)>();
        var total = TitanoRotationService.ManifestCacheCapacity + 4;
        for (var i = 0; i < total; i++)
        {
            // Cartelle distinte: la chiave di cache è il percorso del manifest.
            var folder = $"source-{i:D3}";
            Directory.CreateDirectory(workspaces.GetBacktestPath(workspace.Id, folder));
            WriteTrades(workspaces, workspace.Id, folder, code,
                [(Utc(2026, 1, 6), 100m + i), (Utc(2026, 1, 13), 50m)]);

            var request = BaseRequest(workspace.Id, folder) with { EndUtc = Utc(2026, 2, 2) };
            var manifest = rotation.Run(request);
            runs.Add((folder, manifest.RunId));
            // Run() non popola la cache di lettura: serve un Get per farlo.
            rotation.Get(workspace.Id, folder, manifest.RunId);
        }

        Assert.True(
            TitanoRotationService.CachedManifestCount <= TitanoRotationService.ManifestCacheCapacity,
            $"In cache {TitanoRotationService.CachedManifestCount} voci, " +
            $"tetto {TitanoRotationService.ManifestCacheCapacity}.");

        // L'ultimo usato deve essere ancora servibile, e comunque nessun run va perso: l'eviction
        // costa una rilettura da disco, non un errore.
        var last = runs[^1];
        Assert.NotNull(rotation.Get(workspace.Id, last.Folder, last.RunId));

        var first = runs[0];
        Assert.NotNull(rotation.Get(workspace.Id, first.Folder, first.RunId));
    }

    [Fact]
    public void ResolveLatestRun_ReadsTheFolderOnce_AndSeesANewRotationImmediately()
    {
        // ResolveLatestRun sta sul percorso di OGNI barra di una sessione e, sul claim
        // multi-account, di ogni template di ogni account. Passa da ListRuns, che apriva ogni
        // manifest della cartella con un ReadAllBytes: nel repository quei file stanno fra i 600 KB
        // e 1 MB, e su un run di un anno erano decine di GB riletti per rispondere sempre lo stesso
        // runId. La cache è invalidata sull'ultima modifica della cartella titano/.
        //
        // Il test fissa le DUE metà dell'invariante insieme, perché una senza l'altra è inutile:
        // niente riletture quando non cambia niente, e la rotazione nuova vista subito quando cambia.
        TitanoRotationService.ClearManifestCache();

        var (workspaces, workspaceId, folder, code) = Workspace();
        WriteTrades(workspaces, workspaceId, folder, code,
            [(Utc(2026, 1, 6), 100m), (Utc(2026, 1, 13), 50m)]);
        var rotation = new TitanoRotationService(workspaces);
        var primo = rotation.Run(BaseRequest(workspaceId, folder));

        var scansioniPrima = TitanoRotationService.RunListingScans;
        for (var i = 0; i < 50; i++)
            Assert.Equal(primo.RunId, rotation.ResolveLatestRun(workspaceId, folder)?.RunId);

        Assert.Equal(1, TitanoRotationService.RunListingScans - scansioniPrima);

        // Rotazione nuova sulla stessa cartella: un runId diverso (è l'hash degli input, e qui
        // cambia la configurazione), quindi una sottocartella nuova sotto titano/.
        var secondo = rotation.Run(BaseRequest(workspaceId, folder) with { EndUtc = Utc(2026, 4, 1) });
        Assert.NotEqual(primo.RunId, secondo.RunId);

        // "Dalla barra successiva, senza riaprire la sessione": nessuno ha svuotato la cache, ed
        // entrambi i run sono elencati. Il rilevamento passa dall'ultima modifica di titano/, che la
        // creazione della sottocartella del run nuovo ha toccato.
        Assert.NotNull(rotation.ResolveLatestRun(workspaceId, folder));
        var elencati = rotation.ListRuns(workspaceId, folder).Select(x => x.RunId).ToArray();
        Assert.Contains(primo.RunId, elencati);
        Assert.Contains(secondo.RunId, elencati);

        // Una sola rilettura in più: quella provocata dal run nuovo, non una per chiamata.
        Assert.Equal(2, TitanoRotationService.RunListingScans - scansioniPrima);
    }

    // ---------------------------------------------------------------- helper

    private static TitanoRotationRequest Request() => new()
    {
        WorkspaceId = "w", BacktestFolder = "b",
        StartUtc = Utc(2026, 1, 5), EndUtc = Utc(2026, 3, 1)
    };

    private static TitanoRotationRequest BaseRequest(string workspaceId, string folder) => new()
    {
        WorkspaceId = workspaceId, BacktestFolder = folder,
        StartUtc = Utc(2026, 1, 5), EndUtc = Utc(2026, 3, 1),
        InitialCapital = 1000m, MinimumTrades = 1
    };

    private (WorkspaceService Workspaces, string WorkspaceId, string Folder, string Code) Workspace(
        int strategyCount = 1)
    {
        var strategies = StrategyFactory.GetRegisteredStrategies().Take(strategyCount).ToArray();
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"fsm-{Guid.NewGuid():N}",
            StrategiesFilter = strategies.Select(x => x.Id).ToList()
        });
        const string folder = "source";
        Directory.CreateDirectory(workspaces.GetBacktestPath(workspace.Id, folder));
        var code = StrategyCatalog.TryGetExecutionCode(strategies[0].Id) ?? strategies[0].Id;
        return (workspaces, workspace.Id, folder, code);
    }

    private static void WriteTrades(
        WorkspaceService workspaces, string workspaceId, string folder, string code,
        IReadOnlyList<(DateTime Exit, decimal Net)> trades)
    {
        var store = new TradingJsonStore(workspaces.GetBacktestPath(workspaceId, folder));
        store.Initialize();
        store.WriteTrades(trades
            .Select((trade, index) => Trade($"t{index}", code, trade.Exit, trade.Net))
            .ToArray());
    }

    private TitanoRotationManifest RunSingleStrategy(
        IReadOnlyList<(DateTime Exit, decimal Net)> trades,
        Func<TitanoRotationRequest, TitanoRotationRequest> configure)
    {
        var (workspaces, workspaceId, folder, code) = Workspace();
        WriteTrades(workspaces, workspaceId, folder, code, trades);
        return new TitanoRotationService(workspaces).Run(configure(BaseRequest(workspaceId, folder)));
    }

    private TitanoRotationManifest RunWithTwoStrategies(
        IReadOnlyList<decimal> leaderNetProfits, IReadOnlyList<decimal> laggardNetProfits)
    {
        var strategies = StrategyFactory.GetRegisteredStrategies().Take(2).ToArray();
        Assert.True(strategies.Length == 2, "Servono almeno due strategie registrate.");
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"fsm2-{Guid.NewGuid():N}",
            StrategiesFilter = strategies.Select(x => x.Id).ToList()
        });
        const string folder = "source";
        Directory.CreateDirectory(workspaces.GetBacktestPath(workspace.Id, folder));

        var leader = StrategyCatalog.TryGetExecutionCode(strategies[0].Id) ?? strategies[0].Id;
        var laggard = StrategyCatalog.TryGetExecutionCode(strategies[1].Id) ?? strategies[1].Id;
        var rows = new List<PersistedTrade>();
        for (var i = 0; i < leaderNetProfits.Count; i++)
            rows.Add(Trade($"l{i}", leader, Utc(2026, 1, 6).AddDays(7 * i), leaderNetProfits[i]));
        for (var i = 0; i < laggardNetProfits.Count; i++)
            rows.Add(Trade($"g{i}", laggard, Utc(2026, 1, 6).AddDays(7 * i), laggardNetProfits[i]));

        var store = new TradingJsonStore(workspaces.GetBacktestPath(workspace.Id, folder));
        store.Initialize();
        store.WriteTrades(rows);
        return new TitanoRotationService(workspaces).Run(BaseRequest(workspace.Id, folder));
    }

    private static PersistedTrade Trade(string id, string code, DateTime exit, decimal net) => new()
    {
        TradeId = id, StrategyCode = code, StrategyName = code, Symbol = "NQ",
        Direction = SignalType.Buy, EntryTimeUtc = exit.AddHours(-1), ExitTimeUtc = exit,
        GrossProfit = net, NetProfit = net, Quantity = 1
    };

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
