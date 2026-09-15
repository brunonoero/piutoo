using Piootoo.Core.Services;
using Piootoo.Shared;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La versione di chi ha generato i fill nell'elenco dei backtest.
///
/// <para><c>origin.json</c> registrava la sola versione del server, anche per i run del cBot: ma in
/// quei run i fill li genera il bot, e due run con lo stesso server e bot diversi sembravano lo
/// stesso run. Ora il cBot dichiara la propria versione in <c>open-plan</c>, il marcatore la
/// conserva accanto a quella del server e l'elenco espone entrambe: la griglia mostra il server per
/// un run interno e il bot per un run esterno.</para>
/// </summary>
public sealed class BacktestListVersionTests : IDisposable
{
    private const string PlanCode = "PIANOVERSIONE";
    private const string Account = "1002";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-version-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly StrategyDefinition _strategy;
    private readonly string _workspaceId;

    public BacktestListVersionTests()
    {
        _strategy = StrategyFactory.GetRegisteredStrategies()[0];
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        _workspaceId = _workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"version-{Guid.NewGuid():N}",
            StrategiesFilter = [_strategy.Id]
        }).Id;

        TestAccountRegistry.Register(_workspaces, Account);
        new TradingPlanService(_workspaces).Save(_workspaceId, new SaveTradingPlanRequest
        {
            Code = PlanCode,
            Name = "Piano versione",
            AccountNumber = Account
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ListExposesBothVersionsFromTheOriginMarker()
    {
        WriteRun("interno", new BacktestOriginInfo
        {
            Origin = BacktestOrigin.Internal,
            CreatedUtc = DateTime.UtcNow,
            EngineVersion = "7.4.1"
        });
        WriteRun("cbot", new BacktestOriginInfo
        {
            Origin = BacktestOrigin.ExternalBroker,
            CreatedUtc = DateTime.UtcNow,
            EngineVersion = "7.4.1",
            ClientVersion = "7.4.2",
            PlanCode = PlanCode,
            ExecutionKey = "BT-20250831000001"
        });

        var backtests = _workspaces.ListBacktests(_workspaceId);

        var internalRun = Assert.Single(backtests, backtest => backtest.FolderName == "interno");
        Assert.Equal("7.4.1", internalRun.EngineVersion);
        Assert.Null(internalRun.ClientVersion);

        var externalRun = Assert.Single(backtests, backtest => backtest.FolderName == "cbot");
        Assert.Equal("7.4.1", externalRun.EngineVersion);
        Assert.Equal("7.4.2", externalRun.ClientVersion);
    }

    /// <summary>Un marcatore scritto prima del campo: la versione del bot manca, non e' vuota.</summary>
    [Fact]
    public void MarkerWithoutClientVersion_LeavesItNull()
    {
        WriteRun("cbot-vecchio", new BacktestOriginInfo
        {
            Origin = BacktestOrigin.ExternalBroker,
            CreatedUtc = DateTime.UtcNow,
            EngineVersion = "7.2.0",
            PlanCode = PlanCode,
            ExecutionKey = "BT-20250831000001"
        });

        var backtest = Assert.Single(_workspaces.ListBacktests(_workspaceId));

        Assert.Equal("7.2.0", backtest.EngineVersion);
        Assert.Null(backtest.ClientVersion);
    }

    /// <summary>
    /// L'apertura da piano in backtest scrive nel marcatore la versione dichiarata dal cBot accanto a
    /// quella del server: sono due numeri diversi e vanno tenuti distinti.
    /// </summary>
    [Fact]
    public void OpenPlanBacktest_WritesTheClientVersionNextToTheServerOne()
    {
        var sessions = NewService();

        sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = PlanCode,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "BT-20260101000000",
            AccountNumber = Account,
            ClientVersion = " 7.4.2 "
        });

        var backtest = Assert.Single(_workspaces.ListBacktests(_workspaceId));
        var origin = WorkspaceService.ReadBacktestOrigin(backtest.FullPath);

        Assert.NotNull(origin);
        Assert.Equal(BacktestOrigin.ExternalBroker, origin.Origin);
        Assert.Equal(PiootooVersion.Current, origin.EngineVersion);
        Assert.Equal("7.4.2", origin.ClientVersion);
        Assert.Equal("7.4.2", backtest.ClientVersion);
    }

    /// <summary>
    /// Un bot che non manda il campo, o lo manda vuoto, non lascia una stringa vuota nel marcatore:
    /// in elenco sembrerebbe una versione.
    /// </summary>
    [Fact]
    public void OpenPlanBacktest_WithoutClientVersion_LeavesItNull()
    {
        var sessions = NewService();

        sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = PlanCode,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "BT-20260101000000",
            AccountNumber = Account,
            ClientVersion = "   "
        });

        var backtest = Assert.Single(_workspaces.ListBacktests(_workspaceId));
        var origin = WorkspaceService.ReadBacktestOrigin(backtest.FullPath);

        Assert.NotNull(origin);
        Assert.Equal(PiootooVersion.Current, origin.EngineVersion);
        Assert.Null(origin.ClientVersion);
        Assert.Null(backtest.ClientVersion);
    }

    private TradingSessionService NewService() => new(
        _workspaces,
        new TradingPlanService(_workspaces),
        new NoSignalEvaluationService(),
        new PositionSizingService());

    private void WriteRun(string folderName, BacktestOriginInfo origin)
    {
        var path = Path.Combine(_root, _workspaceId, WorkspaceBacktestPaths.BacktestsDirectoryName, folderName);
        WorkspaceService.WriteBacktestOrigin(path, origin);
    }

    /// <summary>Qui non si spinge nessuna barra: il marcatore si scrive all'apertura.</summary>
    private sealed class NoSignalEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) => [];
    }
}
