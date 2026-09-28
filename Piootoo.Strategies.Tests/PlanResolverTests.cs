using Piootoo.Core.Services;
using Piootoo.Core.Services.Plans;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il risolutore unico dei piani. Per un piano di workspace deve dire esattamente cio' che sessione,
/// ripresa e backtest ricavavano da soli: universo = masterfilter, attive = masterfilter meno le
/// spente, casa = il workspace. Se cambiasse, cambierebbe l'impronta delle sessioni vive e la ripresa
/// dopo un riavvio le rifiuterebbe.
/// </summary>
public sealed class PlanResolverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-resolver-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly TradingPlanService _plans;
    private readonly PlanResolver _resolver;
    private readonly string _workspaceId;

    private readonly string[] _ids = StrategyFactory.GetRegisteredStrategies()
        .Select(definition => definition.Id)
        .OrderBy(id => id, StringComparer.Ordinal)
        .Take(3)
        .ToArray();

    public PlanResolverTests()
    {
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = Path.Combine(_root, "workspaces") });
        _plans = new TradingPlanService(_workspaces);
        _resolver = new PlanResolver(_workspaces, _plans);
        TestAccountRegistry.Register(_workspaces, "111", "222");

        _workspaceId = _workspaces.Create(new CreateWorkspaceRequest { Name = "ricerca" }).Id;
        _workspaces.SaveMasterFilter(_workspaceId, new WorkspaceMasterFilter { StrategiesFilter = _ids.ToList() });
        _plans.Save(_workspaceId, new SaveTradingPlanRequest
        {
            Code = "P-RISOLTO",
            Name = "risolto",
            Accounts = ["111"],
            DisabledStrategies = [_ids[1]]
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void AWorkspacePlanRunsTheMasterfilterMinusItsDisabledStrategies()
    {
        var resolved = _resolver.Resolve("p-risolto");

        Assert.Equal("P-RISOLTO", resolved.Plan.Code);
        Assert.Equal(_ids, resolved.UniverseStrategyIds);
        Assert.Equal(new[] { _ids[0], _ids[2] }, resolved.ActiveStrategyIds);
        Assert.Equal(PlanHomeKind.Workspace, resolved.Home.Kind);
        Assert.Equal(_workspaceId, resolved.Home.Id);
        Assert.Equal(_workspaces.GetWorkspacePath(_workspaceId), resolved.Home.RootPath);
    }

    [Fact]
    public void TheWorkspaceScopedLookupFindsOnlyThatWorkspace()
    {
        var other = _workspaces.Create(new CreateWorkspaceRequest { Name = "altro" }).Id;

        Assert.Equal("P-RISOLTO", _resolver.Resolve(_workspaceId, "P-RISOLTO").Plan.Code);
        Assert.Throws<KeyNotFoundException>(() => _resolver.Resolve(other, "P-RISOLTO"));
        Assert.Throws<KeyNotFoundException>(() => _resolver.Resolve("NON-ESISTE"));
    }

    [Fact]
    public void TheDatafeedFollowsTheUniverseDisabledStrategiesIncluded()
    {
        var instruments = _plans.ResolveDatafeedInstruments(_resolver.Resolve("P-RISOLTO"), accountNumber: null);
        var bySymbol = StrategyFactory.GetRegisteredStrategies()
            .Where(definition => _ids.Contains(definition.Id)
                                 && !string.IsNullOrWhiteSpace(definition.Symbol) && definition.TimeframeMinutes > 0)
            .Select(definition => "@" + definition.Symbol.Trim().TrimStart('@').ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(bySymbol, instruments.Instruments.Select(instrument => instrument.Symbol));
    }

    [Fact]
    public void PlansAreFoundByAccount()
    {
        Assert.Equal(new[] { "P-RISOLTO" }, _resolver.PlanCodesForAccount("111"));
        Assert.Empty(_resolver.PlanCodesForAccount("222"));
    }

    [Fact]
    public void RestoreLooksInTheSessionFoldersOfTheWorkspaces()
    {
        Assert.Empty(_resolver.SessionDirectories());

        var sessions = Path.Combine(_workspaces.GetWorkspacePath(_workspaceId), "sessions");
        Directory.CreateDirectory(sessions);

        Assert.Equal(new[] { sessions }, _resolver.SessionDirectories());
    }
}
