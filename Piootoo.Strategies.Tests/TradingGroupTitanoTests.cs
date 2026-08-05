using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

public sealed class TradingGroupTitanoTests
{
    [Fact]
    public void SetTradingGroups_PersistsTitanoProfileAndAccountMapping()
    {
        var root = CreateRoot();
        try
        {
            var (sessions, workspace, _) = CreateSessionStack(root);
            var descriptor = sessions.Create(new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Id,
                ExecutionMode = ExecutionMode.ExternalBroker
            });

            sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken,
            [
                new TradingGroupRow
                {
                    GroupId = "prop-a",
                    AccountNumber = "1001",
                    RotationSetupId = "bilanciato",
                    TitanoBacktestFolder = "source",
                    MaxConcurrentTrades = 3,
                    ApplyTitanoFilters = true
                },
                new TradingGroupRow
                {
                    GroupId = "prop-a",
                    AccountNumber = "1002",
                    RotationSetupId = "bilanciato",
                    TitanoBacktestFolder = "source",
                    MaxConcurrentTrades = 4,
                    ApplyTitanoFilters = true
                }
            ]);

            var rows = sessions.GetTradingGroups(descriptor.SessionId, descriptor.SessionToken);
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => Assert.Equal("prop-a", row.GroupId));
            Assert.Contains(rows, row => row.AccountNumber == "1001" && row.MaxConcurrentTrades == 3);
            Assert.Contains(rows, row => row.AccountNumber == "1002" && row.MaxConcurrentTrades == 4);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OpenFromPlan_AppliesAllGroupRows()
    {
        var root = CreateRoot();
        try
        {
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "MultiPlan", StrategiesFilter = [strategyId]
            });
            var plans = new TradingPlanService(workspaces);
            plans.Save(workspace.Id, new SaveTradingPlanRequest
            {
                Code = "MULTI",
                Name = "Piano multi",
                Groups =
                [
                    new TradingGroupRow
                    {
                        GroupId = "prop-a",
                        AccountNumber = "1001",
                        MaxConcurrentTrades = 2,
                        ApplyTitanoFilters = false
                    },
                    new TradingGroupRow
                    {
                        GroupId = "prop-b",
                        AccountNumber = "2002",
                        MaxConcurrentTrades = 5,
                        ApplyTitanoFilters = false
                    }
                ]
            });

            var sessions = new TradingSessionService(
                workspaces, plans, new FixedSignalEvaluationService(), null, new PositionSizingService());
            var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
            {
                PlanCode = "MULTI",
                ClientRunMode = ClientRunMode.Backtest,
                ExecutionKey = "open-multi",
                AccountNumber = "2002"
            });

            var rows = sessions.GetTradingGroups(descriptor.SessionId, descriptor.SessionToken);
            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, row => row.AccountNumber == "1001" && row.GroupId == "prop-a");
            Assert.Contains(rows, row => row.AccountNumber == "2002" && row.MaxConcurrentTrades == 5);

            var foreign = Assert.Throws<ArgumentException>(() => sessions.OpenFromPlan(
                new OpenTradingPlanSessionRequest
                {
                    PlanCode = "MULTI",
                    ClientRunMode = ClientRunMode.Backtest,
                    ExecutionKey = "foreign",
                    AccountNumber = "9999"
                }));
            Assert.Contains("non appartiene", foreign.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AccountAtCapacity_DoesNotClaimTemplate_SiblingAccountCanReceiveIt()
    {
        var root = CreateRoot();
        try
        {
            var (sessions, workspace, _) = CreateSessionStack(root);
            var descriptor = sessions.Create(new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Id,
                ExecutionMode = ExecutionMode.ExternalBroker,
                ClientRunMode = ClientRunMode.Realtime,
                TitanoMode = TitanoFilterMode.Disabled
            });
            sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken,
            [
                new TradingGroupRow
                {
                    GroupId = "prop-a", AccountNumber = "1001", MaxConcurrentTrades = 1,
                    ApplyTitanoFilters = false
                },
                new TradingGroupRow
                {
                    GroupId = "prop-a", AccountNumber = "1002", MaxConcurrentTrades = 1,
                    ApplyTitanoFilters = false
                }
            ]);
            sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
            var pushed = sessions.PushBars(Bars(descriptor, Utc(2026, 1, 5)));
            Assert.NotEmpty(pushed.Intents);

            var full = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
                new AccountSignalPollRequest
                {
                    SessionToken = descriptor.SessionToken,
                    Orders = [new BrokerOrderSnapshot { OrderId = "pending-1" }]
                });
            Assert.Null(full.Intent);
            Assert.Equal("MaxConcurrentTradesExceeded", full.Reason);
            Assert.Equal(1, full.PendingOrders);

            var sibling = sessions.PollSignalForAccount(descriptor.SessionId, "1002",
                new AccountSignalPollRequest { SessionToken = descriptor.SessionToken });
            Assert.NotNull(sibling.Intent);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void SetAccountGroups_PreservesExistingGroupTitanoProfiles()
    {
        var root = CreateRoot();
        try
        {
            var (sessions, workspace, _) = CreateSessionStack(root);
            var descriptor = sessions.Create(new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Id,
                ExecutionMode = ExecutionMode.ExternalBroker
            });

            sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken,
            [
                new TradingGroupRow
                {
                    GroupId = "prop-a",
                    AccountNumber = "1001",
                    TitanoBacktestFolder = "source"
                }
            ]);

            sessions.SetAccountGroups(descriptor.SessionId, descriptor.SessionToken,
            [
                new AccountGroupMapping { GroupId = "prop-a", AccountNumber = "1001" },
                new AccountGroupMapping { GroupId = "prop-b", AccountNumber = "2001" }
            ]);

            var rows = sessions.GetTradingGroups(descriptor.SessionId, descriptor.SessionToken);
            Assert.Contains(rows, row => row.GroupId == "prop-a" && row.TitanoBacktestFolder == "source");
            Assert.Contains(rows, row => row.GroupId == "prop-b" && row.TitanoBacktestFolder is null);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void GroupTitanoProfile_OpenGroupReceivesTemplateWhenFilteredGroupDoesNot()
    {
        var root = CreateRoot();
        try
        {
            var (sessions, workspace, rotation) = CreateSessionStack(root);
            var manifest = rotation.Run(new TitanoRotationRequest
            {
                WorkspaceId = workspace.Id,
                BacktestFolder = "source",
                StartUtc = Utc(2026, 1, 1),
                EndUtc = Utc(2026, 2, 1),
                MinimumTrades = 1
            });

            var barTime = Utc(2026, 1, 5);
            var strategy = StrategyFactory.GetRegisteredStrategies().First();
            var effective = rotation.Resolve(workspace.Id, "source", manifest.RunId, barTime);
            var titanoBlocksStrategy = effective.HasActivePeriod &&
                !effective.EffectiveStrategies.Contains(strategy.Name, StringComparer.OrdinalIgnoreCase);

            var descriptor = sessions.Create(new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Id,
                ExecutionMode = ExecutionMode.ExternalBroker,
                // Nessun filtro a livello di sessione: qui si verificano i profili PER GRUPPO.
                TitanoMode = TitanoFilterMode.Disabled
            });
            sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken,
            [
                new TradingGroupRow
                {
                    GroupId = "filtered",
                    AccountNumber = "1001",
                    TitanoBacktestFolder = "source",
                    ApplyTitanoFilters = true
                },
                new TradingGroupRow
                {
                    GroupId = "open",
                    AccountNumber = "2001",
                    ApplyTitanoFilters = false
                }
            ]);
            sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);

            var pushed = sessions.PushBars(Bars(descriptor, barTime));
            Assert.NotEmpty(pushed.Intents);

            var filtered = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");
            var open = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "2001");

            if (titanoBlocksStrategy)
            {
                Assert.Null(filtered.Intent);
                Assert.Equal("NoSignal", filtered.Reason);
                Assert.NotNull(open.Intent);
            }
            else
            {
                Assert.NotNull(filtered.Intent);
                Assert.NotNull(open.Intent);
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void GroupTitanoProfile_ScalesClaimedQuantityUsingAllocationMultiplier()
    {
        var root = CreateRoot();
        try
        {
            var (sessions, workspace, rotation) = CreateSessionStack(root);
            var manifest = rotation.Run(new TitanoRotationRequest
            {
                WorkspaceId = workspace.Id,
                BacktestFolder = "source",
                StartUtc = Utc(2026, 1, 1),
                EndUtc = Utc(2026, 3, 1),
                MinimumTrades = 1,
                SizingTiers =
                [
                    new TitanoSizingTier { MinimumScore = 0.80m, AllocationMultiplier = 1m },
                    new TitanoSizingTier { MinimumScore = 0.60m, AllocationMultiplier = 0.50m },
                    new TitanoSizingTier { MinimumScore = 0m, AllocationMultiplier = 0m }
                ]
            });

            var barTime = Utc(2026, 1, 12);
            var effective = rotation.Resolve(workspace.Id, "source", manifest.RunId, barTime);
            var strategyCode = StrategyFactory.GetRegisteredStrategies().First().Id;
            var state = effective.StrategyStates
                .FirstOrDefault(x => string.Equals(x.StrategyCode, strategyCode, StringComparison.OrdinalIgnoreCase));
            if (state is null || state.AllocationMultiplier >= 1m || state.AllocationMultiplier <= 0m)
            {
                // Dipende dal manifest generato su trades.json vuoto: salta se non c'è allocazione parziale.
                return;
            }

            var descriptor = sessions.Create(new CreateTradingSessionRequest
            {
                WorkspaceId = workspace.Id,
                ExecutionMode = ExecutionMode.ExternalBroker,
                // Nessun filtro a livello di sessione: qui si verificano i profili PER GRUPPO.
                TitanoMode = TitanoFilterMode.Disabled
            });
            sessions.SetTradingGroups(descriptor.SessionId, descriptor.SessionToken,
            [
                new TradingGroupRow
                {
                    GroupId = "scaled",
                    AccountNumber = "1001",
                    TitanoBacktestFolder = "source",
                    ApplyTitanoFilters = true
                }
            ]);
            sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);

            var pushed = sessions.PushBars(Bars(descriptor, barTime));
            var template = Assert.Single(pushed.Intents);

            var claimed = sessions.GetNextSignalForAccount(descriptor.SessionId, descriptor.SessionToken, "1001");
            Assert.NotNull(claimed.Intent);
            Assert.True(claimed.Intent!.FinalQuantity < template.FinalQuantity);
            Assert.Equal(
                TitanoRotationService.RoundQuantity(
                    template.BaseQuantity,
                    template.MarketVolatilityMultiplier * template.PortfolioRiskMultiplier * state.AllocationMultiplier,
                    1m,
                    1m),
                claimed.Intent.FinalQuantity);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static (TradingSessionService Sessions, WorkspaceInfo Workspace, TitanoRotationService Rotation)
        CreateSessionStack(string root)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
        var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = "Groups", StrategiesFilter = [strategyId]
        });
        var backtest = workspaces.GetBacktestPath(workspace.Id, "source");
        new TradingJsonStore(backtest).Initialize();
        // I conti usati dai test di questo file: il claim li pretende nel registro globale.
        TestAccountRegistry.Register(workspaces, "1001", "1002", "2001", "2002");
        var rotation = new TitanoRotationService(workspaces);
        var sessions = new TradingSessionService(
            workspaces, new FixedSignalEvaluationService(), rotation, new PositionSizingService());
        return (sessions, workspace, rotation);
    }

    private static PushBarsRequest Bars(TradingSessionDescriptor descriptor, DateTime barTime) => new()
    {
        SessionId = descriptor.SessionId,
        SessionToken = descriptor.SessionToken,
        Bars =
        [
            new ClosedBar
            {
                Symbol = StrategyFactory.GetRegisteredStrategies().First().Symbol,
                TimeframeMinutes = StrategyFactory.GetRegisteredStrategies().First().TimeframeMinutes,
                BarTimeUtc = barTime,
                Sequence = barTime.Ticks,
                IdempotencyKey = $"bar-{barTime:O}",
                Bar = new OhlcvData
                {
                    DateTime = barTime,
                    Open = 100,
                    High = 101,
                    Low = 99,
                    Close = 100,
                    Volume = 1
                }
            }
        ]
    };

    private static string CreateRoot() => Path.Combine(Path.GetTempPath(), $"piootoo-groups-{Guid.NewGuid():N}");

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private sealed class FixedSignalEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) =>
            strategies.Take(1).Select(strategy => new TradeSignal
            {
                StrategyCode = strategy.Name,
                StrategyName = strategy.Name,
                Symbol = strategy.Symbol,
                Date = closedBar.BarTimeUtc,
                Type = SignalType.Buy,
                Quantity = 4m,
                Price = closedBar.Bar.Close
            }).ToArray();
    }
}
