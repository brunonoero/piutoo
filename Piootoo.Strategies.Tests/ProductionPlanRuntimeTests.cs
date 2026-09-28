using Piootoo.Core.Services;
using Piootoo.Core.Services.BrokerWorkspaces;
using Piootoo.Core.Services.Plans;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.BrokerWorkspaces;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Un piano di produzione si esegue come uno di workspace, con due differenze: le strategie sono
/// quelle che dichiara, non un masterfilter, e sessioni e backtest vivono nel broker workspace. Il cBot
/// non cambia: nomina il piano per codice. Vedi docs/domini/broker-workspace.md, fase 3.
/// </summary>
public sealed class ProductionPlanRuntimeTests : IDisposable
{
    private const string Broker = "FTMO";
    private const string PlanCode = "FTMO-EUROPA";
    private const string Account = "1001";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-produzione-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly BrokerWorkspaceStore _store;
    private readonly StrategyDefinition _strategy;

    /// <summary>Una strategia che il masterfilter di ricerca ha e il piano di produzione no.</summary>
    private readonly StrategyDefinition _researchOnly;

    public ProductionPlanRuntimeTests()
    {
        var catalog = StrategyFactory.GetRegisteredStrategies();
        _strategy = catalog[0];
        _researchOnly = catalog.First(definition =>
            !definition.Id.Equals(_strategy.Id, StringComparison.OrdinalIgnoreCase)
            && definition.Symbol.Equals(_strategy.Symbol, StringComparison.OrdinalIgnoreCase));

        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = Path.Combine(_root, "workspaces") });
        _store = new BrokerWorkspaceStore(Path.Combine(_root, "broker-workspaces"));
        _workspaces.CreateBroker(new TradingBroker { Code = Broker, Name = "FTMO" });
        _workspaces.CreateAccount(new WorkspaceAccount
        {
            Name = $"conto-{Account}", AccountNumber = Account, BrokerCode = Broker, InitialBalance = 100_000m
        });

        // Un workspace di ricerca con piu' strategie: non deve entrare in niente di cio' che segue.
        _workspaces.Create(new CreateWorkspaceRequest
        {
            Name = "ricerca",
            StrategiesFilter = [_strategy.Id, _researchOnly.Id]
        });

        WritePlan(retired: false);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void TheResolverFindsTheProductionPlanByCodeWithItsDeclaredStrategies()
    {
        var resolved = Resolver().Resolve(PlanCode.ToLowerInvariant());

        Assert.Equal(PlanHomeKind.Broker, resolved.Home.Kind);
        Assert.Equal(Broker, resolved.Home.Id);
        Assert.Equal(_store.GetPath(Broker), resolved.Home.RootPath);
        Assert.Equal(new[] { _strategy.Id }, resolved.UniverseStrategyIds);
        Assert.Equal(new[] { _strategy.Id }, resolved.ActiveStrategyIds);
        Assert.Equal(new[] { PlanCode }, Resolver().PlanCodesForAccount(Account));
    }

    [Fact]
    public void TheBotOpensTheProductionPlanInsideTheBrokerWorkspace()
    {
        var sessions = NewService();

        var descriptor = Open(sessions, ClientRunMode.Realtime);

        var sessionsFolder = Path.Combine(_store.GetPath(Broker), "sessions");
        var folder = Assert.Single(Directory.GetDirectories(sessionsFolder));
        Assert.StartsWith(PlanCode, Path.GetFileName(folder));
        Assert.Equal(PlanCode, Assert.Single(sessions.ListSessions()).PlanCode);
        // Solo la strategia dichiarata: quella che il masterfilter di ricerca ha in piu' non entra.
        Assert.Equal(_strategy.Name, Assert.Single(descriptor.Strategies).StrategyCode);
        Assert.Equal(string.Empty, descriptor.WorkspaceId);
    }

    [Fact]
    public void AProductionSessionIsRestoredAfterARestart()
    {
        var opened = Open(NewService(), ClientRunMode.Realtime);

        var restarted = NewService();
        var outcome = Assert.Single(restarted.RestoreSessions());

        Assert.True(outcome.Restored, outcome.Reason);
        Assert.Equal(opened.SessionId, outcome.SessionId);
        Assert.Equal(opened.SessionId, Open(restarted, ClientRunMode.Realtime).SessionId);
    }

    [Fact]
    public void ARetiredPlanOpensNoNewRealtimeSessionButCanStillBeMeasured()
    {
        WritePlan(retired: true);
        var sessions = NewService();

        var error = Assert.Throws<InvalidOperationException>(() => Open(sessions, ClientRunMode.Realtime));
        Assert.Contains("ritirato", error.Message);
        Assert.Empty(Resolver().PlanCodesForAccount(Account));

        Open(sessions, ClientRunMode.Backtest);
        Assert.Single(Directory.GetDirectories(Path.Combine(_store.GetPath(Broker), "backtests")));
    }

    /// <summary>
    /// Il backtest interno nomina un piano di produzione con il solo codice: un workspace non ce l'ha,
    /// e indicarne uno lo cerca li' — dove non c'e'.
    /// </summary>
    [Fact]
    public void ABacktestNamesAProductionPlanByCodeAlone()
    {
        var resolved = Resolver().ResolveForBacktest(workspaceId: null, PlanCode);
        Assert.Equal(PlanHomeKind.Broker, resolved.Home.Kind);

        var research = _workspaces.List().Single(workspace => workspace.Name == "ricerca").Id;
        Assert.Throws<KeyNotFoundException>(() => Resolver().ResolveForBacktest(research, PlanCode));
    }

    [Fact]
    public void TheDatafeedCollectorFollowsTheDeclaredStrategies()
    {
        var instruments = Plans().ResolveDatafeedInstruments(PlanCode, accountNumber: null);

        var instrument = Assert.Single(instruments.Instruments);
        Assert.Equal(new[] { _strategy.TimeframeMinutes }, instrument.TimeframesMinutes);
        Assert.Equal(string.Empty, instruments.WorkspaceId);
    }

    private void WritePlan(bool retired)
    {
        var now = DateTime.UtcNow;
        _store.Write(new BrokerWorkspace { BrokerCode = Broker, CreatedUtc = now },
        [
            new TradingPlan
            {
                WorkspaceId = string.Empty,
                Code = PlanCode,
                Name = "Europa",
                BrokerCode = Broker,
                Accounts = [Account],
                AccountNumber = Account,
                EnabledStrategies = [_strategy.Id],
                CreatedUtc = now,
                UpdatedUtc = now,
                Locked = true,
                LockedUtc = now,
                RetiredUtc = retired ? now : null
            }
        ]);
    }

    private TradingPlanService Plans() => new(_workspaces, _store);

    private PlanResolver Resolver() => new(_workspaces, Plans());

    private TradingSessionService NewService() => new(
        _workspaces, Plans(), new StrategyEvaluationService(), positionSizing: new PositionSizingService());

    private static TradingSessionDescriptor Open(TradingSessionService sessions, ClientRunMode mode) =>
        sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = PlanCode,
            ClientRunMode = mode,
            ExecutionKey = mode == ClientRunMode.Backtest ? "BT-20260101000000" : "LIVE",
            AccountNumber = Account
        });
}
