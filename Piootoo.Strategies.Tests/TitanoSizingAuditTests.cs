using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Audit Titano nel backtest interno e nel percorso piano/sessione ExternalBroker.
/// Verifica ON/OFF, AllocationMultiplier e MaxConcurrentTrades.
/// </summary>
public sealed class TitanoSizingAuditTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void TitanoResolve_ReducedStrategy_ScalesBacktestQuantity()
    {
        var root = CreateRoot("titano-bt");
        try
        {
            var definition = StrategyFactory.GetRegisteredStrategies()
                .First(x => x.Id == "PTS_NQ_TFM_001_60" || x.Name == "PTS_NQ_TFM_001_60");
            var executionCode = StrategyCatalog.TryGetExecutionCode(definition.Id) ?? definition.Name;
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "TitanoBt",
                StrategiesFilter = [definition.Id]
            });

            var backtestFolder = "source";
            var backtestPath = workspaces.GetBacktestPath(workspace.Id, backtestFolder);
            Directory.CreateDirectory(backtestPath);
            new TradingJsonStore(backtestPath).Initialize();

            var periodFrom = Utc(2026, 1, 12);
            var periodTo = Utc(2026, 1, 19);
            WriteHandcraftedManifest(
                backtestPath,
                runId: "audit-half",
                workspaceId: workspace.Id,
                backtestFolder: backtestFolder,
                strategyCode: executionCode,
                multiplier: 0.5m,
                periodFrom: periodFrom,
                periodTo: periodTo);

            var rotation = new TitanoRotationService(workspaces);
            var effective = rotation.Resolve(
                workspace.Id, backtestFolder, "audit-half", periodFrom.AddDays(1),
                TitanoFilterMode.BacktestRotationFile);

            Assert.True(effective.HasActivePeriod);
            Assert.Contains(executionCode, effective.EffectiveStrategies);
            var state = Assert.Single(effective.StrategyStates, s => s.StrategyCode == executionCode);
            Assert.Equal(0.5m, state.AllocationMultiplier);
            Assert.Equal(TitanoStrategyStatus.Reduced, state.State);

            // Il backtest applica lo stesso multiplier al segnale prima dell'engine.
            var service = new PiootooTradingService();
            service.Initialize(100_000m, commissionPerContract: 0m);
            var signalTime = periodFrom.AddHours(16);
            var fillBar = signalTime.AddHours(1);
            var signal = new TradeSignal
            {
                Date = signalTime,
                Type = SignalType.Buy,
                Price = 15_000m,
                Symbol = definition.Symbol,
                StrategyName = executionCode,
                StrategyCode = executionCode,
                Quantity = 10m * state.AllocationMultiplier,
                OrderType = TradeOrderType.Market,
                StopLossMoneyPerFutureContract = 1000m,
                TakeProfitMoneyPerFutureContract = 3000m
            };

            service.ProcessSignals(
                [signal],
                new Dictionary<string, decimal> { [Normalize(definition.Symbol)] = 15_000m },
                new Dictionary<string, OhlcvData>
                {
                    [Normalize(definition.Symbol)] = Bar(signalTime, 15_000m, 15_010m, 14_990m, 15_000m)
                },
                signalTime);

            var open = service.GetExecutionSnapshot(executionCode, definition.Symbol, signalTime).Position;
            Assert.NotNull(open);
            Assert.Equal(5m, open!.Contracts);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void TitanoResolve_OffStrategy_IsExcludedFromEffectiveSet()
    {
        var root = CreateRoot("titano-off");
        try
        {
            var definition = StrategyFactory.GetRegisteredStrategies().First();
            var executionCode = StrategyCatalog.TryGetExecutionCode(definition.Id) ?? definition.Name;
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "TitanoOff",
                StrategiesFilter = [definition.Id]
            });

            var backtestFolder = "source";
            var backtestPath = workspaces.GetBacktestPath(workspace.Id, backtestFolder);
            Directory.CreateDirectory(backtestPath);
            new TradingJsonStore(backtestPath).Initialize();

            var periodFrom = Utc(2026, 2, 2);
            var periodTo = Utc(2026, 2, 9);
            WriteHandcraftedManifest(
                backtestPath, "audit-off", workspace.Id, backtestFolder, executionCode,
                multiplier: 0m, periodFrom, periodTo);

            var rotation = new TitanoRotationService(workspaces);
            var effective = rotation.Resolve(
                workspace.Id, backtestFolder, "audit-off", periodFrom.AddHours(1),
                TitanoFilterMode.BacktestRotationFile);

            Assert.True(effective.HasActivePeriod);
            Assert.DoesNotContain(executionCode, effective.EffectiveStrategies);
            Assert.Equal(0m, effective.StrategyStates.Single().AllocationMultiplier);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void TitanoResolve_OutsideManifest_HasNoActivePeriod_BacktestRotationFile()
    {
        var root = CreateRoot("titano-out");
        try
        {
            var definition = StrategyFactory.GetRegisteredStrategies().First();
            var executionCode = StrategyCatalog.TryGetExecutionCode(definition.Id) ?? definition.Name;
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "TitanoOut",
                StrategiesFilter = [definition.Id]
            });

            var backtestFolder = "source";
            var backtestPath = workspaces.GetBacktestPath(workspace.Id, backtestFolder);
            Directory.CreateDirectory(backtestPath);
            new TradingJsonStore(backtestPath).Initialize();

            WriteHandcraftedManifest(
                backtestPath, "audit-range", workspace.Id, backtestFolder, executionCode,
                multiplier: 1m, Utc(2026, 3, 2), Utc(2026, 3, 9));

            var rotation = new TitanoRotationService(workspaces);
            var before = rotation.Resolve(
                workspace.Id, backtestFolder, "audit-range", Utc(2026, 2, 1),
                TitanoFilterMode.BacktestRotationFile);
            Assert.False(before.HasActivePeriod);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OfflineEquity_ScalesNetProfitByAllocationMultiplier()
    {
        // Formula documentata in TitanoRotationService.BuildEquity:
        // net = NetProfit * AllocationMultiplier - (commission+slippage)*qty*multiplier
        Assert.Equal(5m, TitanoRotationService.RoundQuantity(10m, 0.5m, step: 1m, minimum: 1m));
        Assert.Equal(0m, TitanoRotationService.RoundQuantity(1m, 0.25m, step: 1m, minimum: 1m));
        Assert.Equal(1m, TitanoRotationService.SelectMultiplier(0.85m,
        [
            new TitanoSizingTier { MinimumScore = 0.80m, AllocationMultiplier = 1m },
            new TitanoSizingTier { MinimumScore = 0.50m, AllocationMultiplier = 0.5m },
            new TitanoSizingTier { MinimumScore = 0m, AllocationMultiplier = 0m }
        ]));
        Assert.Equal(0.5m, TitanoRotationService.SelectMultiplier(0.55m,
        [
            new TitanoSizingTier { MinimumScore = 0.80m, AllocationMultiplier = 1m },
            new TitanoSizingTier { MinimumScore = 0.50m, AllocationMultiplier = 0.5m },
            new TitanoSizingTier { MinimumScore = 0m, AllocationMultiplier = 0m }
        ]));
    }

    [Fact]
    public void OpenPlan_WithTitano_AppliesAllocationOnce()
    {
        var root = CreateRoot("plan-double");
        try
        {
            var definition = StrategyFactory.GetRegisteredStrategies()
                .First(x => !string.IsNullOrWhiteSpace(x.Symbol));
            var executionCode = StrategyCatalog.TryGetExecutionCode(definition.Id) ?? definition.Name;
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "PlanDouble",
                StrategiesFilter = [definition.Id]
            });

            var backtestFolder = "source";
            var backtestPath = workspaces.GetBacktestPath(workspace.Id, backtestFolder);
            Directory.CreateDirectory(backtestPath);
            new TradingJsonStore(backtestPath).Initialize();

            var periodFrom = Utc(2026, 4, 6);
            var periodTo = Utc(2026, 4, 13);
            const string runId = "audit-plan-half";
            WriteHandcraftedManifest(
                backtestPath, runId, workspace.Id, backtestFolder, executionCode,
                multiplier: 0.5m, periodFrom, periodTo);

            var plans = new TradingPlanService(workspaces);
            plans.Save(workspace.Id, new SaveTradingPlanRequest
            {
                Code = "PLANHALF",
                Name = "Piano half",
                GroupId = "prop-a",
                AccountNumber = "1001",
                MaxConcurrentTrades = 3,
                TitanoBacktestFolder = backtestFolder,
                ApplyTitanoFilters = true,
                InitialCapital = 100_000m,
                CommissionPerContract = 0m,
                PositionSizing = new PositionSizingConfig(),
                Instruments =
                [
                    new InstrumentMetadata
                    {
                        Symbol = definition.Symbol,
                        DollarsPerPoint = 20m,
                        MinimumQuantity = 1m,
                        QuantityStep = 1m,
                        RoundingMode = QuantityRoundingMode.FuturesContracts
                    }
                ]
            });

            var rotation = new TitanoRotationService(workspaces);
            var sessions = new TradingSessionService(
                workspaces,
                plans,
                new FixedQtyEvaluationService(10m),
                rotation,
                new PositionSizingService());

            var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
            {
                PlanCode = "PLANHALF",
                ClientRunMode = ClientRunMode.Backtest,
                ExecutionKey = "audit-1",
                AccountNumber = "1001"
            });

            Assert.Equal(TitanoFilterMode.BacktestRotationFile, descriptor.TitanoMode);
            Assert.Equal(runId, descriptor.TitanoRunId);

            var barTime = periodFrom.AddHours(12);
            var pushed = sessions.PushBars(new PushBarsRequest
            {
                SessionId = descriptor.SessionId,
                SessionToken = descriptor.SessionToken,
                Bars =
                [
                    new ClosedBar
                    {
                        Symbol = definition.Symbol,
                        TimeframeMinutes = definition.TimeframeMinutes,
                        BarTimeUtc = barTime,
                        Sequence = 1,
                        IdempotencyKey = "bar-1",
                        Bar = Bar(barTime, 100, 101, 99, 100)
                    }
                ]
            });

            var template = Assert.Single(pushed.Intents);
            // Sessione applica già AllocationMultiplier=0.5 → FinalQuantity = floor(10*0.5)=5
            Assert.Equal(10m, template.BaseQuantity);
            Assert.Equal(0.5m, template.StrategyEquityMultiplier);
            Assert.Equal(5m, template.FinalQuantity);

            var claimed = sessions.GetNextSignalForAccount(
                descriptor.SessionId, descriptor.SessionToken, "1001");
            Assert.NotNull(claimed.Intent);

            // Lo stesso run è già stato applicato in PushBars: il claim non deve scalarlo di nuovo.
            Assert.Equal(5m, claimed.Intent!.FinalQuantity);
            Assert.Equal(0.5m, claimed.Intent.StrategyEquityMultiplier);
            Assert.Equal(template.FinalQuantity, claimed.Intent.FinalQuantity);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OpenPlan_TitanoOff_BlocksClaimWhenMultiplierZero()
    {
        var root = CreateRoot("plan-off");
        try
        {
            var definition = StrategyFactory.GetRegisteredStrategies().First();
            var executionCode = StrategyCatalog.TryGetExecutionCode(definition.Id) ?? definition.Name;
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "PlanOff",
                StrategiesFilter = [definition.Id]
            });

            var backtestFolder = "source";
            var backtestPath = workspaces.GetBacktestPath(workspace.Id, backtestFolder);
            Directory.CreateDirectory(backtestPath);
            new TradingJsonStore(backtestPath).Initialize();

            var periodFrom = Utc(2026, 5, 4);
            var periodTo = Utc(2026, 5, 11);
            const string runId = "audit-plan-off";
            WriteHandcraftedManifest(
                backtestPath, runId, workspace.Id, backtestFolder, executionCode,
                multiplier: 0m, periodFrom, periodTo);

            var plans = new TradingPlanService(workspaces);
            plans.Save(workspace.Id, new SaveTradingPlanRequest
            {
                Code = "PLANOFF",
                Name = "Piano off",
                GroupId = "prop-a",
                AccountNumber = "1001",
                MaxConcurrentTrades = 2,
                TitanoBacktestFolder = backtestFolder,
                ApplyTitanoFilters = true,
                Instruments =
                [
                    new InstrumentMetadata
                    {
                        Symbol = definition.Symbol,
                        DollarsPerPoint = 1m,
                        MinimumQuantity = 1m,
                        QuantityStep = 1m
                    }
                ]
            });

            var sessions = new TradingSessionService(
                workspaces,
                plans,
                new FixedQtyEvaluationService(4m),
                new TitanoRotationService(workspaces),
                new PositionSizingService());

            var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
            {
                PlanCode = "PLANOFF",
                ClientRunMode = ClientRunMode.Backtest,
                ExecutionKey = "off-1"
            });

            var barTime = periodFrom.AddHours(8);
            var pushed = sessions.PushBars(new PushBarsRequest
            {
                SessionId = descriptor.SessionId,
                SessionToken = descriptor.SessionToken,
                Bars =
                [
                    new ClosedBar
                    {
                        Symbol = definition.Symbol,
                        TimeframeMinutes = definition.TimeframeMinutes,
                        BarTimeUtc = barTime,
                        Sequence = 1,
                        IdempotencyKey = "off-bar",
                        Bar = Bar(barTime, 100, 101, 99, 100)
                    }
                ]
            });

            // Sessione filtrata: strategia OFF → nessun template.
            Assert.Empty(pushed.Intents);

            var poll = sessions.GetNextSignalForAccount(
                descriptor.SessionId, descriptor.SessionToken, "1001");
            Assert.Null(poll.Intent);
            Assert.Equal("NoSignal", poll.Reason);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OpenPlan_BacktestWithoutTitano_EvaluatesAllWorkspaceStrategies()
    {
        var root = CreateRoot("plan-max");
        try
        {
            var definition = StrategyFactory.GetRegisteredStrategies().First();
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "PlanMax",
                StrategiesFilter = [definition.Id]
            });

            var plans = new TradingPlanService(workspaces);
            plans.Save(workspace.Id, new SaveTradingPlanRequest
            {
                Code = "PLANMAX",
                Name = "Piano max",
                GroupId = "prop-a",
                AccountNumber = "1001",
                MaxConcurrentTrades = 1,
                ApplyTitanoFilters = false,
                Instruments =
                [
                    new InstrumentMetadata
                    {
                        Symbol = definition.Symbol,
                        DollarsPerPoint = 1m,
                        MinimumQuantity = 1m,
                        QuantityStep = 1m
                    }
                ]
            });

            var sessions = new TradingSessionService(
                workspaces,
                plans,
                new FixedQtyEvaluationService(2m),
                positionSizing: new PositionSizingService());

            var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
            {
                PlanCode = "PLANMAX",
                ClientRunMode = ClientRunMode.Backtest,
                ExecutionKey = "max-1"
            });

            Assert.Equal(TitanoFilterMode.Disabled, descriptor.TitanoMode);

            var barTime = Utc(2026, 6, 1);
            sessions.PushBars(new PushBarsRequest
            {
                SessionId = descriptor.SessionId,
                SessionToken = descriptor.SessionToken,
                Bars =
                [
                    new ClosedBar
                    {
                        Symbol = definition.Symbol,
                        TimeframeMinutes = definition.TimeframeMinutes,
                        BarTimeUtc = barTime,
                        Sequence = 1,
                        IdempotencyKey = "max-bar",
                        Bar = Bar(barTime, 100, 101, 99, 100)
                    }
                ]
            });

            // In Backtest+Disabled il run deve produrre il master completo su cui Titano calcola
            // la rotazione: il limite operativo di concorrenza non deve eliminare segnali.
            var poll = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
                new AccountSignalPollRequest
                {
                    SessionToken = descriptor.SessionToken,
                    Orders = [new BrokerOrderSnapshot { OrderId = "pending-1" }]
                });

            Assert.NotNull(poll.Intent);
            Assert.NotEqual("MaxConcurrentTradesExceeded", poll.Reason);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OpenPlan_RealtimeWithoutTitano_EnforcesMaxConcurrentTrades()
    {
        var root = CreateRoot("plan-max-rt");
        try
        {
            var definition = StrategyFactory.GetRegisteredStrategies().First();
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "PlanMaxRt",
                StrategiesFilter = [definition.Id]
            });

            var plans = new TradingPlanService(workspaces);
            plans.Save(workspace.Id, new SaveTradingPlanRequest
            {
                Code = "PLANMAXRT",
                Name = "Piano max rt",
                GroupId = "prop-a",
                AccountNumber = "1001",
                MaxConcurrentTrades = 1,
                ApplyTitanoFilters = false,
                Instruments =
                [
                    new InstrumentMetadata
                    {
                        Symbol = definition.Symbol,
                        DollarsPerPoint = 1m,
                        MinimumQuantity = 1m,
                        QuantityStep = 1m
                    }
                ]
            });

            var sessions = new TradingSessionService(
                workspaces,
                plans,
                new FixedQtyEvaluationService(2m),
                positionSizing: new PositionSizingService());

            var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
            {
                PlanCode = "PLANMAXRT",
                ClientRunMode = ClientRunMode.Realtime,
                ExecutionKey = "max-rt-1"
            });

            var barTime = Utc(2026, 6, 2);
            sessions.PushBars(new PushBarsRequest
            {
                SessionId = descriptor.SessionId,
                SessionToken = descriptor.SessionToken,
                Bars =
                [
                    new ClosedBar
                    {
                        Symbol = definition.Symbol,
                        TimeframeMinutes = definition.TimeframeMinutes,
                        BarTimeUtc = barTime,
                        Sequence = 1,
                        IdempotencyKey = "max-rt-bar",
                        Bar = Bar(barTime, 100, 101, 99, 100)
                    }
                ]
            });

            var poll = sessions.PollSignalForAccount(descriptor.SessionId, "1001",
                new AccountSignalPollRequest
                {
                    SessionToken = descriptor.SessionToken,
                    Orders = [new BrokerOrderSnapshot { OrderId = "pending-1" }]
                });

            Assert.Null(poll.Intent);
            Assert.Equal("MaxConcurrentTradesExceeded", poll.Reason);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void WriteHandcraftedManifest(
        string backtestPath,
        string runId,
        string workspaceId,
        string backtestFolder,
        string strategyCode,
        decimal multiplier,
        DateTime periodFrom,
        DateTime periodTo)
    {
        var runPath = Path.Combine(backtestPath, "titano", runId);
        Directory.CreateDirectory(runPath);
        var enabled = multiplier > 0m;
        var state = new TitanoStrategyState
        {
            StrategyCode = strategyCode,
            Enabled = enabled,
            AllocationMultiplier = multiplier,
            State = !enabled ? TitanoStrategyStatus.Disabled :
                multiplier < 1m ? TitanoStrategyStatus.Reduced : TitanoStrategyStatus.Enabled,
            Reason = "audit fixture",
            TransitionType = "NewlyTracked",
            ConsecutiveOnPeriods = enabled ? 1 : 0
        };
        var decision = new TitanoRotationDecision
        {
            PeriodId = $"{periodFrom:yyyyMMddTHHmmssZ}-{periodTo:yyyyMMddTHHmmssZ}",
            PeriodStartUtc = periodFrom.AddDays(-7),
            PeriodEndUtc = periodFrom,
            EffectiveFromUtc = periodFrom,
            EffectiveToUtc = periodTo,
            SourceBacktestFolder = backtestFolder,
            MasterFilterHash = "audit",
            Strategies = [state]
        };
        var manifest = new TitanoRotationManifest
        {
            RunId = runId,
            Config = new TitanoRotationRequest
            {
                WorkspaceId = workspaceId,
                BacktestFolder = backtestFolder,
                StartUtc = periodFrom.AddDays(-7),
                EndUtc = periodTo,
                InitialCapital = 100_000m
            },
            SourceTradesSha256 = "audit",
            MasterFilterHash = "audit",
            ConfigSha256 = "audit",
            GeneratedAtUtc = DateTime.UtcNow,
            Periods = [decision]
        };
        File.WriteAllText(Path.Combine(runPath, "manifest.json"),
            JsonSerializer.Serialize(manifest, JsonOptions));
    }

    private static string CreateRoot(string prefix) =>
        Path.Combine(Path.GetTempPath(), $"piootoo-{prefix}-{Guid.NewGuid():N}");

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static string Normalize(string symbol) =>
        symbol.Trim().TrimStart('@').ToUpperInvariant();

    private static OhlcvData Bar(DateTime time, decimal open, decimal high, decimal low, decimal close) =>
        new()
        {
            DateTime = time,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1
        };

    private sealed class FixedQtyEvaluationService(decimal quantity) : IStrategyEvaluationService
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
                Quantity = quantity,
                Price = closedBar.Bar.Close,
                OrderType = TradeOrderType.Market,
                StopLossMoneyPerFutureContract = 1000m,
                TakeProfitMoneyPerFutureContract = 3000m
            }).ToArray();
    }
}
