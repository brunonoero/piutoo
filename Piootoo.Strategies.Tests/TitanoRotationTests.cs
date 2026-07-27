using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

public sealed class TitanoRotationTests
{
    [Theory]
    [InlineData(TitanoRotationPeriod.Weekly, "2026-07-20", "2026-07-27")]
    [InlineData(TitanoRotationPeriod.Biweekly, "2026-07-20", "2026-08-03")]
    [InlineData(TitanoRotationPeriod.Monthly, "2026-07-01", "2026-08-01")]
    public void CalendarsHaveDeterministicUtcBoundaries(
        TitanoRotationPeriod rotation, string expectedStart, string expectedEnd)
    {
        var request = new TitanoRotationRequest
        {
            WorkspaceId = "w", BacktestFolder = "b", RotationPeriod = rotation,
            StartUtc = Utc(2026, 7, 20), EndUtc = Utc(2026, 9, 1),
            BiweeklyAnchorUtc = Utc(2026, 7, 6)
        };

        var first = TitanoRotationService.BuildPeriods(request).First();

        Assert.Equal(DateTime.Parse(expectedStart).Date, first.Start.Date);
        Assert.Equal(DateTime.Parse(expectedEnd).Date, first.End.Date);
        Assert.Equal(DateTimeKind.Utc, first.Start.Kind);
    }

    [Fact]
    public void PersistenceRejectsMissingStrategyCode()
    {
        var root = Path.Combine(Path.GetTempPath(), $"titano-validation-{Guid.NewGuid():N}");
        try
        {
            var store = new TradingJsonStore(root);
            Assert.Throws<InvalidDataException>(() => store.WriteTrades([new PersistedTrade
            {
                TradeId = "t", StrategyCode = "", StrategyName = "legacy", Symbol = "NQ",
                Direction = SignalType.Buy, EntryTimeUtc = Utc(2026, 7, 1),
                ExitTimeUtc = Utc(2026, 7, 2)
            }]));
            Assert.Throws<InvalidDataException>(() => store.WriteSignals([new PersistedSignal
            {
                SignalId = "s", StrategyCode = " ", StrategyName = "legacy", Symbol = "NQ",
                TimestampUtc = Utc(2026, 7, 1)
            }]));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void EquityLineMetricsUseOnlyClosedTradesBeforeCutoff()
    {
        var request = new TitanoRotationRequest
        {
            WorkspaceId = "w", BacktestFolder = "b", StartUtc = Utc(2026, 1, 1),
            EndUtc = Utc(2026, 2, 1), InitialCapital = 100m,
            ShortWindowDays = 30, LongWindowDays = 365, MovingAverageWindowDays = 30
        };
        var trades = new[]
        {
            Trade("one", Utc(2026, 1, 2), 10m),
            Trade("two", Utc(2026, 1, 3), -5m),
            Trade("at-cutoff", Utc(2026, 1, 4), 999m)
        };

        var metrics = TitanoRotationService.CalculateMetrics(trades, Utc(2026, 1, 4), request);

        Assert.Equal(2, metrics.Trades);
        Assert.Equal(105m, metrics.CurrentEquity);
        Assert.Equal(107.5m, metrics.MovingAverageEquity);
        Assert.Equal(2.5m, metrics.EquityStandardDeviation);
        Assert.Equal(-1m, metrics.ZScore);
        Assert.Equal(5m / 110m, metrics.CurrentDrawdown);
        Assert.Equal(metrics.CurrentDrawdown, metrics.MaximumDrawdown);
        Assert.Equal(0.05m, metrics.ShortReturn);
    }

    [Fact]
    public void ExternalFillCommissionContributesToAuthoritativeNetContract()
    {
        var report = new ExternalExecutionReport
        {
            ReportId = "r", IntentId = "i", Status = ExecutionReportStatus.Filled,
            CumulativeFilledQuantity = 1, FillPrice = 100, Commission = 2.5m,
            EventTimeUtc = Utc(2026, 1, 2)
        };
        Assert.Equal(2.5m, report.Commission);
    }

    [Fact]
    public void SameRunCanBeAttachedToBothExecutionModes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"titano-session-{Guid.NewGuid():N}");
        try
        {
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var strategyId = StrategyFactory.GetRegisteredStrategies().First().Id;
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "Integration", StrategiesFilter = [strategyId]
            });
            var backtest = workspaces.GetBacktestPath(workspace.Id, "source");
            var store = new TradingJsonStore(backtest);
            store.Initialize();
            var rotation = new TitanoRotationService(workspaces);
            var manifest = rotation.Run(new TitanoRotationRequest
            {
                WorkspaceId = workspace.Id, BacktestFolder = "source",
                StartUtc = Utc(2026, 1, 5), EndUtc = Utc(2026, 1, 26),
                MinimumTrades = 1
            });
            var sessions = new TradingSessionService(workspaces, new StrategyEvaluationService(), rotation);

            foreach (var mode in new[] { ExecutionMode.ServerSimulated, ExecutionMode.ExternalBroker })
            {
                var descriptor = sessions.Create(new CreateTradingSessionRequest
                {
                    WorkspaceId = workspace.Id, ExecutionMode = mode,
                    TitanoRunId = manifest.RunId, TitanoBacktestFolder = "source",
                    TitanoMode = TitanoFilterMode.BacktestRotationFile
                });
                Assert.Equal(manifest.RunId, descriptor.TitanoRunId);
                Assert.Equal(mode, descriptor.ExecutionMode);
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(10, 1, 1, 1, 10)]
    [InlineData(10, 0.5, 1, 1, 5)]
    [InlineData(3, 0.25, 1, 1, 0)]
    [InlineData(7, 0.5, 0.25, 1, 3.5)]
    public void SizingUsesDeterministicFloorAndMinimum(
        decimal quantity, decimal multiplier, decimal step, decimal minimum, decimal expected)
    {
        Assert.Equal(expected, TitanoRotationService.RoundQuantity(quantity, multiplier, step, minimum));
    }

    [Fact]
    public void SizingTiersIncludeBinaryCompatibility()
    {
        var tiers = new TitanoRotationRequest
        {
            WorkspaceId = "w", BacktestFolder = "b", StartUtc = Utc(2026, 1, 1), EndUtc = Utc(2026, 2, 1)
        }.SizingTiers;

        Assert.Equal(1m, TitanoRotationService.SelectMultiplier(0.9m, tiers));
        Assert.Equal(0.5m, TitanoRotationService.SelectMultiplier(0.7m, tiers));
        Assert.Equal(0.25m, TitanoRotationService.SelectMultiplier(0.5m, tiers));
        Assert.Equal(0m, TitanoRotationService.SelectMultiplier(0.1m, tiers));
    }

    [Fact]
    public void SessionSizingIsIdenticalAcrossModesAndIdempotent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"sizing-session-{Guid.NewGuid():N}");
        try
        {
            var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = root });
            var definition = StrategyFactory.GetRegisteredStrategies().First();
            var workspace = workspaces.Create(new CreateWorkspaceRequest
            {
                Name = "Sizing", StrategiesFilter = [definition.Id]
            });
            foreach (var mode in new[] { ExecutionMode.ServerSimulated, ExecutionMode.ExternalBroker })
            {
                var sessions = new TradingSessionService(
                    workspaces, new FixedSignalEvaluationService(), positionSizing: new PositionSizingService());
                var descriptor = sessions.Create(new CreateTradingSessionRequest
                {
                    WorkspaceId = workspace.Id, ExecutionMode = mode,
                    Instruments =
                    [
                        new InstrumentMetadata
                        {
                            Symbol = definition.Symbol, DollarsPerPoint = 1,
                            MinimumQuantity = 1, QuantityStep = 1,
                            RoundingMode = QuantityRoundingMode.FuturesContracts
                        }
                    ]
                });
                sessions.SetStatus(descriptor.SessionId, descriptor.SessionToken, TradingSessionStatus.Running);
                var request = new PushBarsRequest
                {
                    SessionId = descriptor.SessionId, SessionToken = descriptor.SessionToken,
                    Bars =
                    [
                        new ClosedBar
                        {
                            Symbol = definition.Symbol, TimeframeMinutes = definition.TimeframeMinutes,
                            BarTimeUtc = Utc(2026, 1, 5), Sequence = 1, IdempotencyKey = $"{mode}-1",
                            Bar = new Piootoo.Shared.Models.OhlcvData
                            {
                                DateTime = Utc(2026, 1, 5), Open = 100, High = 101, Low = 99, Close = 100
                            }
                        }
                    ]
                };
                var first = sessions.PushBars(request);
                var replay = sessions.PushBars(request);
                Assert.Equal(3, first.Intents.Single().FinalQuantity);
                Assert.Equal(3.9m, first.Intents.Single().BaseQuantity);
                Assert.Empty(replay.Intents);
                Assert.Single(sessions.GetPersistedSignals(descriptor.SessionId, descriptor.SessionToken));
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class FixedSignalEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<Piootoo.Shared.Models.TradeSignal> Evaluate(
            IReadOnlyList<Piootoo.Shared.Interfaces.ITradingStrategy> strategies,
            ClosedBar closedBar, IReadOnlyList<Piootoo.Shared.Models.OhlcvData> history,
            Func<Piootoo.Shared.Interfaces.ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) =>
            strategies.Take(1).Select(strategy => new Piootoo.Shared.Models.TradeSignal
            {
                StrategyCode = strategy.Name, StrategyName = strategy.Name, Symbol = strategy.Symbol,
                Date = closedBar.BarTimeUtc, Type = SignalType.Buy, Quantity = 3.9m, Price = closedBar.Bar.Close
            }).ToArray();
    }

    private static PersistedTrade Trade(string id, DateTime exit, decimal net) => new()
    {
        TradeId = id, StrategyCode = "S", StrategyName = "S", Symbol = "NQ",
        Direction = SignalType.Buy, EntryTimeUtc = exit.AddHours(-1), ExitTimeUtc = exit,
        GrossProfit = net, NetProfit = net
    };

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
