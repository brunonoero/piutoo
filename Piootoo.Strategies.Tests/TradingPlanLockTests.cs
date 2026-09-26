using Piootoo.Core.Services;
using Piootoo.Core.Services.BestPlans;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models.BestPlans;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un piano bloccato non si modifica e non si elimina; per cambiarlo si duplica, e la copia nasce
/// sbloccata con tutto il resto uguale. La promozione a best plan blocca il piano del run.
/// </summary>
public sealed class TradingPlanLockTests : IDisposable
{
    private const string Account = "7001";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-lock-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly TradingPlanService _plans;
    private readonly string _workspaceId;

    public TradingPlanLockTests()
    {
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = Path.Combine(_root, "workspaces") });
        _workspaceId = _workspaces.Create(new CreateWorkspaceRequest { Name = $"lock-{Guid.NewGuid():N}" }).Id;
        TestAccountRegistry.Register(_workspaces, Account);
        _plans = new TradingPlanService(_workspaces);
        _plans.Save(_workspaceId, Request("P-LOCK", "Piano da bloccare"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static SaveTradingPlanRequest Request(string code, string name) => new()
    {
        Code = code,
        Name = name,
        AccountNumber = Account,
        SizeMultiplier = 2.5m,
        DisabledStrategies = ["QUALCOSA_SPENTO"]
    };

    [Fact]
    public void LockedPlanCannotBeSavedOrDeleted()
    {
        var locked = _plans.Lock(_workspaceId, "P-LOCK");
        Assert.True(locked.Locked);
        Assert.NotNull(locked.LockedUtc);

        var save = Assert.Throws<InvalidOperationException>(() => _plans.Save(_workspaceId, Request("P-LOCK", "altro")));
        Assert.Contains("bloccato", save.Message);
        Assert.Throws<InvalidOperationException>(() => _plans.Delete(_workspaceId, "P-LOCK"));

        var reread = _plans.Get(_workspaceId, "P-LOCK");
        Assert.True(reread.Locked);
        Assert.Equal("Piano da bloccare", reread.Name);
    }

    [Fact]
    public void LockingTwiceKeepsTheFirstDate()
    {
        var first = _plans.Lock(_workspaceId, "P-LOCK");
        var second = _plans.Lock(_workspaceId, "P-LOCK");
        Assert.Equal(first.LockedUtc, second.LockedUtc);
    }

    [Fact]
    public void DuplicateOfALockedPlanIsUnlockedAndEditable()
    {
        _plans.Lock(_workspaceId, "P-LOCK");

        var copy = _plans.Duplicate(_workspaceId, "P-LOCK", new DuplicateTradingPlanRequest { NewCode = "P-LOCK-B" });

        Assert.False(copy.Locked);
        Assert.Null(copy.LockedUtc);
        Assert.Equal("Piano da bloccare (copia)", copy.Name);
        Assert.Equal(2.5m, copy.SizeMultiplier);
        Assert.Equal(new[] { "QUALCOSA_SPENTO" }, copy.DisabledStrategies);
        Assert.Equal(new[] { Account }, copy.Accounts);

        _plans.Save(_workspaceId, Request(copy.Code, "copia modificata"));
        Assert.Equal("copia modificata", _plans.Get(_workspaceId, copy.Code).Name);
    }

    [Fact]
    public void DuplicateRejectsAnExistingCode()
    {
        _plans.Save(_workspaceId, Request("P-ALTRO", "altro"));
        Assert.Throws<InvalidOperationException>(() =>
            _plans.Duplicate(_workspaceId, "P-LOCK", new DuplicateTradingPlanRequest { NewCode = "P-ALTRO" }));
    }

    [Fact]
    public void PromotingABacktestLocksItsPlan()
    {
        var path = _workspaces.GetBacktestPath(_workspaceId, "run");
        Directory.CreateDirectory(path);
        WorkspaceService.WriteBacktestOrigin(path, new BacktestOriginInfo
        {
            Origin = BacktestOrigin.ExternalBroker,
            CreatedUtc = DateTime.UtcNow,
            PlanCode = "P-LOCK",
            InitialCapital = 100_000m
        });
        new TradingJsonStore(path).WriteTrades(
        [
            new PersistedTrade
            {
                TradeId = "a",
                StrategyCode = "X_NQ_001_60",
                StrategyName = "X_NQ_001_60",
                Symbol = "NQ",
                Direction = SignalType.Buy,
                Quantity = 1,
                EntryTimeUtc = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc),
                ExitTimeUtc = new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc),
                NetProfit = 100m
            }
        ]);

        var bestPlans = new BestPlanService(_workspaces, _plans, Path.Combine(_root, "best-plans"));
        bestPlans.Promote(new PromoteBestPlanRequest { WorkspaceId = _workspaceId, BacktestFolder = "run" });

        Assert.True(_plans.Get(_workspaceId, "P-LOCK").Locked);
    }
}
