using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// L'intervallo di un backtest del cBot nell'elenco dei backtest.
///
/// <para>Il periodo si leggeva solo da <c>backtest_*.json</c>, che scrive il solo motore interno:
/// per i run esterni la colonna <i>Intervallo</i> restava vuota. Ora l'inizio viene dall'execution key
/// (l'avvio del backtest in cTrader, non la prima barra di riscaldamento) e la fine da
/// <c>lastBarUtc</c> di <c>session-summary.json</c>.</para>
/// </summary>
public sealed class BacktestListExternalPeriodTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-extperiod-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ExternalRun_StartsAtTheExecutionKeyAndEndsAtTheLastBar()
    {
        var (workspaces, workspaceId) = Workspace();
        var path = ExternalRun(workspaceId, "comp-bt", "BT-20250831000001");
        File.WriteAllText(Path.Combine(path, SessionRunSummarySchema.FileName),
            "{ \"schemaVersion\": 1, \"firstBarUtc\": \"2025-08-28T22:00:00Z\", \"lastBarUtc\": \"2026-08-31T23:30:00Z\" }");

        var backtest = Assert.Single(workspaces.ListBacktests(workspaceId));

        Assert.Equal(new DateTime(2025, 8, 31, 0, 0, 1, DateTimeKind.Utc), backtest.StartDateUtc);
        Assert.Equal(new DateTime(2026, 8, 31, 23, 30, 0, DateTimeKind.Utc), backtest.EndDateUtc);
    }

    [Fact]
    public void ExternalRunStillRunning_HasAStartButNoEnd()
    {
        var (workspaces, workspaceId) = Workspace();
        ExternalRun(workspaceId, "comp-bt-running", "BT-20250831000001");

        var backtest = Assert.Single(workspaces.ListBacktests(workspaceId));

        Assert.Equal(new DateTime(2025, 8, 31, 0, 0, 1, DateTimeKind.Utc), backtest.StartDateUtc);
        Assert.Null(backtest.EndDateUtc);
    }

    [Fact]
    public void ExecutionKeyInAnotherFormat_FallsBackToTheFirstBar()
    {
        var (workspaces, workspaceId) = Workspace();
        var path = ExternalRun(workspaceId, "comp-bt-custom", "prova-manuale");
        File.WriteAllText(Path.Combine(path, SessionRunSummarySchema.FileName),
            "{ \"firstBarUtc\": \"2025-08-28T22:00:00Z\", \"lastBarUtc\": \"2026-08-31T23:30:00Z\" }");

        var backtest = Assert.Single(workspaces.ListBacktests(workspaceId));

        Assert.Equal(new DateTime(2025, 8, 28, 22, 0, 0, DateTimeKind.Utc), backtest.StartDateUtc);
    }

    private (WorkspaceService, string) Workspace()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest { Name = "extperiod" });
        return (workspaces, workspace.Id);
    }

    private string ExternalRun(string workspaceId, string folderName, string executionKey)
    {
        var path = Path.Combine(_root, workspaceId, WorkspaceBacktestPaths.BacktestsDirectoryName, folderName);
        WorkspaceService.WriteBacktestOrigin(path, new BacktestOriginInfo
        {
            Origin = BacktestOrigin.ExternalBroker,
            CreatedUtc = DateTime.UtcNow,
            PlanCode = "COMP",
            ExecutionKey = executionKey
        });
        return path;
    }
}
