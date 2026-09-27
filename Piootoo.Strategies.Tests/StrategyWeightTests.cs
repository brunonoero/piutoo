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
/// Il peso di una strategia nel piano (<see cref="TradingPlan.StrategyWeights"/>): sta sul piano per
/// Id di catalogo, entra nella quantita' consegnata al conto accanto al moltiplicatore <c>k</c> del
/// piano, e come lui una volta sola.
/// </summary>
public sealed class StrategyWeightTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-strategy-weight", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Peso e <c>k</c> si moltiplicano: 2,5 × 2 = 5 volte la quantita' neutra. Se il peso entrasse
    /// anche nel template il claim lo applicherebbe due volte, e il rapporto sarebbe 12,5.
    /// </summary>
    [Fact]
    public void TheWeightScalesTheClaimedQuantityTogetherWithTheSizeMultiplier()
    {
        var baseline = ClaimedQuantity(sizeMultiplier: 1m, weight: null);
        var weighted = ClaimedQuantity(sizeMultiplier: 2m, weight: 2.5m);

        Assert.True(baseline > 0m, "il claim di riferimento non ha prodotto quantita': test inutile");
        Assert.Equal(baseline * 5m, weighted);
    }

    /// <summary>Un peso dichiarato su un'altra strategia non tocca questa.</summary>
    [Fact]
    public void AWeightOnAnotherStrategyLeavesThisOneNeutral()
    {
        var baseline = ClaimedQuantity(sizeMultiplier: 1m, weight: null);
        var other = ClaimedQuantity(sizeMultiplier: 1m, weight: 3m, weightedId: "UN_ALTRO_ID");

        Assert.Equal(baseline, other);
    }

    /// <summary>
    /// Fuori da [0,1; 10] il piano non si salva: sotto e' uno spegnimento mascherato, sopra quasi
    /// sempre una virgola sbagliata.
    /// </summary>
    [Theory]
    [InlineData("0.05")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("50")]
    public void AWeightOutOfRangeIsRejected(string weight)
    {
        var (workspaces, workspace, strategy) = NewWorkspace();
        var plans = new TradingPlanService(workspaces);

        var error = Assert.Throws<ArgumentException>(() => plans.Save(
            workspace.Id, Plan(1m, "PLANW", new Dictionary<string, decimal> { [strategy.Id] = decimal.Parse(weight, System.Globalization.CultureInfo.InvariantCulture) })));

        Assert.Contains(strategy.Id, error.Message);
    }

    /// <summary>
    /// Il file porta solo i pesi diversi da 1, con Id ripuliti; il piano riletto e la sua copia li
    /// ritrovano uguali.
    /// </summary>
    [Fact]
    public void WeightsAreNormalizedPersistedAndDuplicated()
    {
        var (workspaces, workspace, _) = NewWorkspace();
        var plans = new TradingPlanService(workspaces);

        plans.Save(workspace.Id, Plan(1m, "PLANW", new Dictionary<string, decimal>
        {
            ["  STRAT_A  "] = 0.5m,
            ["STRAT_B"] = 1m,
            ["STRAT_C"] = 4m
        }));

        var reloaded = plans.Get(workspace.Id, "PLANW");
        Assert.Equal(2, reloaded.StrategyWeights.Count);
        Assert.Equal(0.5m, TradingPlanService.WeightOf(reloaded, "strat_a"));
        Assert.Equal(1m, TradingPlanService.WeightOf(reloaded, "STRAT_B"));
        Assert.Equal(4m, TradingPlanService.WeightOf(reloaded, "STRAT_C"));

        var copy = plans.Duplicate(workspace.Id, "PLANW", new DuplicateTradingPlanRequest { NewCode = "PLANW2" });
        Assert.Equal(reloaded.StrategyWeights.OrderBy(entry => entry.Key), copy.StrategyWeights.OrderBy(entry => entry.Key));
    }

    /// <summary>La quantita' del primo intent reclamato, con k del piano e un peso sulla strategia del masterfilter (o su un altro Id).</summary>
    private decimal ClaimedQuantity(decimal sizeMultiplier, decimal? weight, string? weightedId = null)
    {
        var (workspaces, workspace, strategy) = NewWorkspace();
        var plans = new TradingPlanService(workspaces);

        var code = $"PLANW{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var weights = weight is { } value
            ? new Dictionary<string, decimal> { [weightedId ?? strategy.Id] = value }
            : new Dictionary<string, decimal>();
        plans.Save(workspace.Id, Plan(sizeMultiplier, code, weights));

        var sessions = new TradingSessionService(
            workspaces, plans, new OneSignalPerBar(), positionSizing: new PositionSizingService());

        var descriptor = sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = $"run-{Guid.NewGuid():N}"
        });

        var barTime = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            .AddMinutes(strategy.TimeframeMinutes);
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

        var response = sessions.GetNextSignalForAccount(
            descriptor.SessionId, descriptor.SessionToken, "1001");

        Assert.NotNull(response.Intent);
        return response.Intent!.FinalQuantity;
    }

    private static SaveTradingPlanRequest Plan(
        decimal sizeMultiplier, string code, IReadOnlyDictionary<string, decimal> weights) => new()
    {
        Code = code,
        Name = "Piano pesi",
        AccountNumber = "1001",
        SizeMultiplier = sizeMultiplier,
        StrategyWeights = weights
    };

    private (WorkspaceService Workspaces, WorkspaceInfo Workspace, StrategyDefinition Strategy) NewWorkspace()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategy = StrategyFactory.GetRegisteredStrategies()
            .First(x => !string.IsNullOrWhiteSpace(x.Symbol) && x.TimeframeMinutes > 0);

        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"weight-{Guid.NewGuid():N}",
            StrategiesFilter = [strategy.Id]
        });
        TestAccountRegistry.Register(workspaces, "1001");
        return (workspaces, workspace, strategy);
    }

    /// <summary>Un ingresso a mercato per barra: serve solo a far nascere un intent da reclamare.</summary>
    private sealed class OneSignalPerBar : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar bar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> execution)
            => strategies
                .Where(strategy => string.Equals(
                    strategy.Symbol.Trim().TrimStart('@'),
                    bar.Symbol.Trim().TrimStart('@'),
                    StringComparison.OrdinalIgnoreCase))
                .Select(strategy => new TradeSignal
                {
                    Date = bar.BarTimeUtc,
                    Type = SignalType.Buy,
                    Price = bar.Bar.Close,
                    Symbol = bar.Symbol,
                    StrategyCode = strategy.Name,
                    StrategyName = strategy.Name,
                    Quantity = 1m,
                    OrderType = TradeOrderType.Market,
                    ValidFromUtc = bar.BarTimeUtc
                })
                .ToList();
    }
}
