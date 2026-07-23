using System.Text.Json;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Strategies;
using Piootoo.Shared.Models.Workspaces;
using Piootoo.Shared.Utilities;

namespace Piootoo.Strategies.Tests;

public sealed class WorkspaceBacktestingContractsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-tests-{Guid.NewGuid():N}");

    [Fact]
    public void EmptyWorkspace_HasNoImplicitStrategies()
    {
        var service = CreateWorkspaceService();
        var workspace = service.Create(new CreateWorkspaceRequest { Name = "Vuoto" });

        Assert.Empty(service.GetMasterFilter(workspace.Id).StrategiesFilter);
    }

    [Fact]
    public void Masterfilter_PreservesOnlyExplicitStrategyIds()
    {
        var service = CreateWorkspaceService();
        var workspace = service.Create(new CreateWorkspaceRequest
        {
            Name = "Selezione",
            StrategiesFilter = ["easy-152", "easy-156"]
        });

        Assert.Equal(["easy-152", "easy-156"], service.GetMasterFilter(workspace.Id).StrategiesFilter);
    }

    [Theory]
    [InlineData(" Backtest Luglio ", "backtest-luglio")]
    [InlineData("Easy 152/156", "easy-152-156")]
    public void BacktestName_IsNormalizedAndContained(string name, string expected)
    {
        var normalized = WorkspaceBacktestPaths.NormalizeFolderName(name);
        var path = WorkspaceBacktestPaths.ResolveBacktestPath(_root, normalized);

        Assert.Equal(expected, normalized);
        Assert.StartsWith(Path.GetFullPath(_root), path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JobAndCatalogContracts_RoundTripThroughJson()
    {
        var payload = new
        {
            Job = new BacktestingJob { JobId = "job-1", Status = BacktestingJobStatus.Running, ProgressPercent = 42 },
            Strategy = new StrategyCatalogItem { Id = "easy-152", Code = "easy-152", Name = "Easy 152", Symbol = "@ES", TimeframeMinutes = 15 }
        };

        var json = JsonSerializer.Serialize(payload);

        Assert.Contains("\"ProgressPercent\":42", json);
        Assert.Contains("\"Code\":\"easy-152\"", json);
    }

    private WorkspaceService CreateWorkspaceService()
    {
        Directory.CreateDirectory(_root);
        return new WorkspaceService(new PiootooSettings
        {
            BasePath = _root,
            Workspaces = "[BasePath]\\workspaces"
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
