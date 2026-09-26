using System.Text;
using Piootoo.Core.Services;
using Piootoo.Core.Services.BestPlans;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models.BestPlans;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un best plan e' una copia: sopravvive alla cancellazione del backtest. Le cifre per anno seguono
/// l'aritmetica del report HTML, e la curva viene dal file di risultato quando c'e' (mark-to-market)
/// e dai trade chiusi quando manca (run del cBot).
/// </summary>
public sealed class BestPlanServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-bestplans-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private (WorkspaceService Workspaces, BestPlanService BestPlans, string WorkspaceId) Create()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = Path.Combine(_root, "workspaces") });
        var workspace = workspaces.Create(new CreateWorkspaceRequest { Name = "best" });
        var service = new BestPlanService(workspaces, new TradingPlanService(workspaces), Path.Combine(_root, "best-plans"));
        return (workspaces, service, workspace.Id);
    }

    private static PersistedTrade Trade(string id, string code, DateTime exitUtc, decimal net) => new()
    {
        TradeId = id,
        StrategyCode = code,
        StrategyName = code,
        Symbol = "NQ",
        Direction = SignalType.Buy,
        Quantity = 1,
        EntryTimeUtc = exitUtc.AddHours(-2),
        ExitTimeUtc = exitUtc,
        NetProfit = net
    };

    private static string ExternalBacktest(WorkspaceService workspaces, string workspaceId, string folder)
    {
        var path = workspaces.GetBacktestPath(workspaceId, folder);
        Directory.CreateDirectory(path);
        WorkspaceService.WriteBacktestOrigin(path, new BacktestOriginInfo
        {
            Origin = BacktestOrigin.ExternalBroker,
            CreatedUtc = new DateTime(2026, 9, 26, 6, 42, 0, DateTimeKind.Utc),
            PlanCode = "P1",
            ClientVersion = "7.7.0",
            InitialCapital = 100_000m,
            PriceSource = RunPriceSource.Cfd("FTMO")
        });
        new TradingJsonStore(path).WriteTrades(
        [
            Trade("a", "PT5DAV_NQ_RHL_001_30", new DateTime(2025, 10, 1, 12, 0, 0, DateTimeKind.Utc), 1_000m),
            Trade("b", "PT5DAV_NQ_RHL_001_30", new DateTime(2025, 11, 1, 12, 0, 0, DateTimeKind.Utc), -500m),
            Trade("c", "PT5DAV_GC_BOS_002_240", new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc), 2_000m)
        ]);
        return path;
    }

    [Fact]
    public void ExternalRunBecomesARealizedSnapshotWithYearsAndStrategies()
    {
        var (workspaces, service, workspaceId) = Create();
        ExternalBacktest(workspaces, workspaceId, "run-cbot");

        var plan = service.Promote(new PromoteBestPlanRequest { WorkspaceId = workspaceId, BacktestFolder = "run-cbot" });

        Assert.Equal("P1", plan.PlanCode);
        Assert.Equal("best", plan.WorkspaceName);
        Assert.Equal("realizzata", plan.EquitySource);
        Assert.Equal(2_500m, plan.NetProfit);
        Assert.Equal(3, plan.TotalTrades);
        Assert.Equal(new[] { 2025, 2026 }, plan.Years.Select(year => year.Year));
        Assert.Equal(500m, plan.Years[0].NetProfit);
        Assert.Equal(500m, plan.Years[0].MaxDrawdown);
        Assert.Equal(2_000m, plan.Years[1].NetProfit);
        Assert.Equal(100_500m, plan.Years[1].StartEquity);
        Assert.Equal(2, plan.Strategies.Count);
        Assert.Contains(TradingPersistenceSchema.TradesFileName, plan.Artifacts);
    }

    [Fact]
    public void SnapshotSurvivesTheDeletionOfTheBacktest()
    {
        var (workspaces, service, workspaceId) = Create();
        ExternalBacktest(workspaces, workspaceId, "run-cbot");
        var promoted = service.Promote(new PromoteBestPlanRequest { WorkspaceId = workspaceId, BacktestFolder = "run-cbot" });

        workspaces.DeleteBacktest(workspaceId, "run-cbot");

        var listed = Assert.Single(service.List());
        Assert.Equal(promoted.Id, listed.Id);
        var detail = service.Get(promoted.Id);
        Assert.Equal(2_500m, detail.NetProfit);
        Assert.Equal(promoted.Equity.Count, detail.Equity.Count);
    }

    [Fact]
    public void PromotingTwiceNeedsOverwriteAndDeleteRemovesIt()
    {
        var (workspaces, service, workspaceId) = Create();
        ExternalBacktest(workspaces, workspaceId, "run-cbot");
        var request = new PromoteBestPlanRequest { WorkspaceId = workspaceId, BacktestFolder = "run-cbot" };
        var plan = service.Promote(request);

        var error = Assert.Throws<InvalidOperationException>(() => service.Promote(request));
        Assert.Contains(BestPlan.AlreadyPromotedMessage, error.Message);

        request.Overwrite = true;
        service.Promote(request);
        Assert.Single(service.List());

        service.Delete(plan.Id);
        Assert.Empty(service.List());
        Assert.True(Directory.Exists(workspaces.GetBacktestPath(workspaceId, "run-cbot")));
    }

    [Fact]
    public void InternalRunReadsTheMarkToMarketCurveFromTheResultFile()
    {
        var (workspaces, service, workspaceId) = Create();
        var path = workspaces.GetBacktestPath(workspaceId, "run-interno");
        Directory.CreateDirectory(path);
        WorkspaceService.WriteBacktestOrigin(path, new BacktestOriginInfo
        {
            Origin = BacktestOrigin.Internal,
            CreatedUtc = new DateTime(2026, 9, 24, 16, 0, 0, DateTimeKind.Utc),
            EngineVersion = "7.8.0"
        });
        new TradingJsonStore(path).WriteTrades(
            [Trade("a", "PT3B_NQ_PCH_001_15", new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc), 300m)]);

        // Il minimo indispensabile, nell'ordine in cui lo scrive il motore: la curva prima delle
        // strategie. Il punto a meta' dicembre e' un minimo intraday che una curva realizzata non vede.
        var json = new StringBuilder();
        json.Append("{\"JobId\":\"x\",\"StartDate\":\"2025-12-01T00:00:00Z\",\"InitialCapital\":1000,\"HourlyResults\":[");
        json.Append("{\"DateTime\":\"2025-12-01T00:00:00Z\",\"Equity\":1000,\"Drawdown\":0},");
        json.Append("{\"DateTime\":\"2025-12-15T00:00:00Z\",\"Equity\":800,\"Drawdown\":20},");
        json.Append("{\"DateTime\":\"2025-12-31T12:00:00Z\",\"Equity\":1300,\"Drawdown\":0},");
        json.Append("{\"DateTime\":\"2026-01-10T00:00:00Z\",\"Equity\":0,\"Drawdown\":0},");
        json.Append("{\"DateTime\":\"2026-01-15T00:00:00Z\",\"Equity\":1200,\"Drawdown\":7.69}");
        json.Append("],\"StrategyResults\":[{\"StrategyName\":\"ignorata\",\"Equity\":123}],\"FinalEquity\":1200}");
        File.WriteAllText(Path.Combine(path, "backtest_run_20260924160000.json"), json.ToString());

        var plan = service.Promote(new PromoteBestPlanRequest { WorkspaceId = workspaceId, BacktestFolder = "run-interno" });

        Assert.Equal("mark-to-market", plan.EquitySource);
        Assert.Equal(1_000m, plan.InitialCapital);
        Assert.Equal(4, plan.Equity.Count);
        Assert.Equal(200m, plan.NetProfit);
        Assert.Equal(20m, plan.MaxDrawdownPercent);
        Assert.Equal(300m, plan.Years[0].NetProfit);
        Assert.Equal(-100m, plan.Years[1].NetProfit);
        Assert.Equal(100m, plan.Years[1].MaxDrawdown);
    }

    [Fact]
    public void FolderWithoutTradesOrCurveIsRejected()
    {
        var (workspaces, service, workspaceId) = Create();
        Directory.CreateDirectory(workspaces.GetBacktestPath(workspaceId, "vuoto"));

        Assert.Throws<InvalidOperationException>(() =>
            service.Promote(new PromoteBestPlanRequest { WorkspaceId = workspaceId, BacktestFolder = "vuoto" }));
        Assert.Empty(service.List());
    }
}
