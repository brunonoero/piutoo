using System.Text.Json;
using Piootoo.Core.Services;
using Piootoo.Core.Services.BestPlans;
using Piootoo.Core.Services.BrokerWorkspaces;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.BestPlans;
using Piootoo.Shared.Models.BrokerWorkspaces;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// I piani in produzione: si entra da un best plan con le strategie che il run ha eseguito, dentro
/// un broker una strategia e un conto stanno in un solo piano attivo, e il cambio di conto e' un
/// duplicato che ritira l'originale. Vedi docs/domini/broker-workspace.md.
/// </summary>
public sealed class BrokerWorkspaceServiceTests : IDisposable
{
    private const string Broker = "FTMO";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-brokerws-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly TradingPlanService _plans;
    private readonly BestPlanService _bestPlans;
    private readonly BrokerWorkspaceService _service;
    private readonly string _workspaceId;

    /// <summary>Quattro strategie vere del catalogo: il servizio le riconosce per codice di esecuzione.</summary>
    private readonly StrategyDefinition[] _strategies = StrategyFactory.GetRegisteredStrategies()
        .Where(definition => !string.IsNullOrWhiteSpace(definition.Name) && !string.IsNullOrWhiteSpace(definition.Symbol))
        .OrderBy(definition => definition.Id, StringComparer.Ordinal)
        .Take(4)
        .ToArray();

    public BrokerWorkspaceServiceTests()
    {
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = Path.Combine(_root, "workspaces") });
        var store = new BrokerWorkspaceStore(Path.Combine(_root, "broker-workspaces"));
        _plans = new TradingPlanService(_workspaces, store);
        _bestPlans = new BestPlanService(_workspaces, _plans, Path.Combine(_root, "best-plans"));
        _service = new BrokerWorkspaceService(store, _workspaces, _plans, _bestPlans);

        _workspaces.CreateBroker(new TradingBroker { Code = Broker, Name = "FTMO" });
        _workspaces.CreateBroker(new TradingBroker { Code = "ICS", Name = "ICS" });
        foreach (var number in new[] { "111", "222", "333" })
            _workspaces.CreateAccount(new WorkspaceAccount
            {
                Name = $"conto-{number}", AccountNumber = number, BrokerCode = Broker, InitialBalance = 100_000m
            });
        _workspaceId = _workspaces.Create(new CreateWorkspaceRequest { Name = "ricerca" }).Id;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void PromotionDeclaresTheRunStrategiesAndLocksThePlan()
    {
        var bestPlanId = PromotableBestPlan("EUROPA", [_strategies[0], _strategies[1]],
            weights: new Dictionary<string, decimal> { [_strategies[0].Id] = 0.5m, [_strategies[3].Id] = 2m });

        var plan = _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = bestPlanId, PlanCode = "ftmo-europa" });

        Assert.Equal("FTMO-EUROPA", plan.Code);
        Assert.Equal(string.Empty, plan.WorkspaceId);
        Assert.Equal(Broker, plan.BrokerCode);
        Assert.Equal(new[] { "111" }, plan.Accounts);
        Assert.Equal(new[] { _strategies[0].Id, _strategies[1].Id }.Order(StringComparer.OrdinalIgnoreCase), plan.EnabledStrategies);
        Assert.Empty(plan.DisabledStrategies);
        // Il peso di una strategia che il piano non esegue non passa.
        Assert.Equal(0.5m, Assert.Single(plan.StrategyWeights).Value);
        Assert.True(plan.Locked);
        Assert.Null(plan.RetiredUtc);
        Assert.Equal(bestPlanId, plan.Provenance!.BestPlanId);
        Assert.Equal("EUROPA", plan.Provenance.SourcePlanCode);
        Assert.Equal(_workspaceId, plan.Provenance.SourceWorkspaceId);
        Assert.Null(plan.Provenance.PreviousPlanCode);

        var detail = _service.Get(Broker);
        Assert.Equal("FTMO-EUROPA", Assert.Single(detail.Plans).Code);
        var summary = Assert.Single(_service.List());
        Assert.Equal(1, summary.ActivePlans);
        Assert.Equal(2, summary.ActiveStrategies);
    }

    [Fact]
    public void DryRunValidatesButWritesNothing()
    {
        var bestPlanId = PromotableBestPlan("EUROPA", [_strategies[0]]);

        var plan = _service.Promote(Broker, new PromoteToProductionRequest
        {
            BestPlanId = bestPlanId, PlanCode = "FTMO-EUROPA", DryRun = true
        });

        Assert.Equal("FTMO-EUROPA", plan.Code);
        Assert.Empty(_service.List());
    }

    [Fact]
    public void APlanSavedAfterTheRunStartedIsRejected()
    {
        var bestPlanId = PromotableBestPlan("EUROPA", [_strategies[0]], runStartedBeforeLastSave: true);

        var error = Assert.Throws<InvalidOperationException>(() =>
            _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = bestPlanId, PlanCode = "FTMO-EUROPA" }));

        Assert.Contains("dopo l'inizio del run", error.Message);
        Assert.Empty(_service.List());
    }

    [Fact]
    public void APlanGoesOnlyIntoTheWorkspaceOfItsBroker()
    {
        var bestPlanId = PromotableBestPlan("EUROPA", [_strategies[0]]);

        Assert.Throws<ArgumentException>(() =>
            _service.Promote("ICS", new PromoteToProductionRequest { BestPlanId = bestPlanId, PlanCode = "ICS-EUROPA" }));
    }

    /// <summary>La console promuove senza broker: il broker workspace e' quello del piano di origine.</summary>
    [Fact]
    public void WithoutABrokerThePlanGoesToTheWorkspaceOfItsOwnBroker()
    {
        var bestPlanId = PromotableBestPlan("EUROPA", [_strategies[0]]);

        var plan = _service.Promote(brokerCode: null, new PromoteToProductionRequest { BestPlanId = bestPlanId, PlanCode = "FTMO-EUROPA" });

        Assert.Equal(Broker, plan.BrokerCode);
        Assert.Equal("FTMO-EUROPA", Assert.Single(_service.Get(Broker).Plans).Code);
    }

    [Fact]
    public void AStrategyStaysInOneActivePlanUntilThatPlanIsRetired()
    {
        var europa = PromotableBestPlan("EUROPA", [_strategies[0], _strategies[1]]);
        var indici = PromotableBestPlan("INDICI", [_strategies[1], _strategies[2]]);
        _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = europa, PlanCode = "FTMO-EUROPA" });

        var request = new PromoteToProductionRequest { BestPlanId = indici, PlanCode = "FTMO-INDICI", Accounts = ["222"] };
        var error = Assert.Throws<InvalidOperationException>(() => _service.Promote(Broker, request));
        Assert.Contains(_strategies[1].Id, error.Message);
        Assert.Contains("FTMO-EUROPA", error.Message);

        _service.Retire(Broker, "FTMO-EUROPA");
        var indiciPlan = _service.Promote(Broker, request);

        Assert.Equal(new[] { "222" }, indiciPlan.Accounts);
        var summary = Assert.Single(_service.List());
        Assert.Equal(1, summary.ActivePlans);
        Assert.Equal(1, summary.RetiredPlans);
    }

    [Fact]
    public void AnAccountRunsOneActivePlan()
    {
        var europa = PromotableBestPlan("EUROPA", [_strategies[0]]);
        var usa = PromotableBestPlan("USA", [_strategies[1]]);
        _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = europa, PlanCode = "FTMO-EUROPA" });

        // Il piano di ricerca girava sul conto di prova comune: senza un conto proprio non entra.
        var error = Assert.Throws<InvalidOperationException>(() =>
            _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = usa, PlanCode = "FTMO-USA" }));
        Assert.Contains("111", error.Message);

        var plan = _service.Promote(Broker, new PromoteToProductionRequest
        {
            BestPlanId = usa, PlanCode = "FTMO-USA", Accounts = ["222"]
        });
        Assert.Equal(new[] { "222" }, plan.Accounts);
    }

    [Fact]
    public void ThePlanCodeIsUniqueAcrossWorkspacesAndBrokerWorkspaces()
    {
        var europa = PromotableBestPlan("EUROPA", [_strategies[0]]);

        Assert.Throws<InvalidOperationException>(() =>
            _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = europa, PlanCode = "EUROPA" }));

        _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = europa, PlanCode = "FTMO-EUROPA" });
        var error = Assert.Throws<InvalidOperationException>(() =>
            _plans.Save(_workspaceId, PlanRequest("FTMO-EUROPA", new Dictionary<string, decimal>())));
        Assert.Contains("produzione", error.Message);
    }

    [Fact]
    public void AResearchContainerInTheRunStopsThePromotion()
    {
        var container = StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true)
            .First(definition => definition.IsResearchContainer && !string.IsNullOrWhiteSpace(definition.Name));
        var bestPlanId = PromotableBestPlan("EUROPA", [_strategies[0], container]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = bestPlanId, PlanCode = "FTMO-EUROPA" }));

        Assert.Contains(container.Name, error.Message);
    }

    [Fact]
    public void DuplicatingChangesTheAccountsAndRetiresTheOriginal()
    {
        var europa = PromotableBestPlan("EUROPA", [_strategies[0], _strategies[1]]);
        var original = _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = europa, PlanCode = "FTMO-EUROPA" });

        // Il conto vecchio si disattiva: un conto disattivato non esegue piani nuovi.
        var old = _workspaces.ListAccounts().Single(account => account.AccountNumber == "111");
        old.Enabled = false;
        _workspaces.SaveAccount(old.Id, old);
        Assert.Throws<ArgumentException>(() => _service.Duplicate(Broker, "FTMO-EUROPA",
            new DuplicateProductionPlanRequest { NewCode = "FTMO-EUROPA-2", Accounts = ["111"] }));

        var copy = _service.Duplicate(Broker, "FTMO-EUROPA",
            new DuplicateProductionPlanRequest { NewCode = "FTMO-EUROPA-2", Accounts = ["333"] });

        Assert.Equal(new[] { "333" }, copy.Accounts);
        Assert.Equal(original.EnabledStrategies, copy.EnabledStrategies);
        Assert.Equal("FTMO-EUROPA", copy.Provenance!.PreviousPlanCode);
        Assert.Equal(europa, copy.Provenance.BestPlanId);
        Assert.NotNull(_service.GetPlan(Broker, "FTMO-EUROPA").RetiredUtc);
        Assert.Null(copy.RetiredUtc);
    }

    [Fact]
    public void ANeutralBestPlanHasNoPlanToPromote()
    {
        var path = _workspaces.GetBacktestPath(_workspaceId, "neutro");
        Directory.CreateDirectory(path);
        WorkspaceService.WriteBacktestOrigin(path, new BacktestOriginInfo
        {
            Origin = BacktestOrigin.Internal, CreatedUtc = DateTime.UtcNow, InitialCapital = 100_000m
        });
        new TradingJsonStore(path).WriteTrades([Trade("a", _strategies[0].Name)]);
        var bestPlan = _bestPlans.Promote(new PromoteBestPlanRequest { WorkspaceId = _workspaceId, BacktestFolder = "neutro" });

        Assert.Throws<ArgumentException>(() =>
            _service.Promote(Broker, new PromoteToProductionRequest { BestPlanId = bestPlan.Id, PlanCode = "FTMO-NEUTRO" }));
    }

    /// <summary>
    /// Un piano di ricerca con il suo run del cBot promosso a best plan: il piano si salva, poi il run
    /// parte (o, con <paramref name="runStartedBeforeLastSave"/>, era gia' partito), poi si promuove.
    /// </summary>
    private string PromotableBestPlan(
        string planCode,
        StrategyDefinition[] runStrategies,
        IReadOnlyDictionary<string, decimal>? weights = null,
        bool runStartedBeforeLastSave = false)
    {
        var saved = _plans.Save(_workspaceId, PlanRequest(planCode, weights ?? new Dictionary<string, decimal>()));

        var folder = $"{planCode.ToLowerInvariant()}-bt";
        var path = _workspaces.GetBacktestPath(_workspaceId, folder);
        Directory.CreateDirectory(path);
        WorkspaceService.WriteBacktestOrigin(path, new BacktestOriginInfo
        {
            Origin = BacktestOrigin.ExternalBroker,
            CreatedUtc = runStartedBeforeLastSave ? saved.UpdatedUtc.AddHours(-1) : DateTime.UtcNow,
            PlanCode = planCode,
            InitialCapital = 100_000m,
            PriceSource = RunPriceSource.Cfd(Broker)
        });
        File.WriteAllText(Path.Combine(path, SessionRunSummarySchema.FileName), JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            strategies = runStrategies.Select(definition => new
            {
                strategyCode = definition.Name, symbol = definition.Symbol, timeframeMinutes = definition.TimeframeMinutes
            })
        }));
        new TradingJsonStore(path).WriteTrades(runStrategies.Select((definition, i) => Trade($"t{i}", definition.Name)).ToList());

        return _bestPlans.Promote(new PromoteBestPlanRequest { WorkspaceId = _workspaceId, BacktestFolder = folder }).Id;
    }

    private static SaveTradingPlanRequest PlanRequest(string code, IReadOnlyDictionary<string, decimal> weights) => new()
    {
        Code = code,
        Name = $"Piano {code}",
        BrokerCode = Broker,
        Accounts = ["111"],
        StrategyWeights = weights
    };

    private static PersistedTrade Trade(string id, string code) => new()
    {
        TradeId = id,
        StrategyCode = code,
        StrategyName = code,
        Symbol = "NQ",
        Direction = SignalType.Buy,
        Quantity = 1,
        EntryTimeUtc = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc),
        ExitTimeUtc = new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc),
        NetProfit = 100m
    };
}
