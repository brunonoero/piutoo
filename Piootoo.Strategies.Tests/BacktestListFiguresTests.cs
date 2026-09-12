using System.Text.Json;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Le cifre di chiusura nell'elenco dei backtest — P&amp;L in percentuale del capitale iniziale e
/// drawdown massimo in percentuale dal picco — vengono da <c>backtest-summary.json</c> e da
/// nient'altro: il file di risultato le porta dopo l'equity ora per ora, e leggerle da li'
/// riporterebbe l'elenco al costo che la voce del 2026-08-04 in <c>decisioni.md</c> ha tolto. Il
/// <c>maxDrawdown</c> del summary e' gia' una percentuale (<c>TradingState.UpdateDrawdown</c>) e va
/// riportato tale e quale: diviso per il capitale darebbe un numero cento volte piu' piccolo. Una
/// cartella senza summary — run interrotto, run del cBot — ha le celle vuote, non zero: uno zero
/// sarebbe un run in pari.
/// </summary>
public sealed class BacktestListFiguresTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-figures-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void FiguresAreReadFromTheSummaryAsPercentOfInitialCapital()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest { Name = "figures" });

        WriteSummary(workspace.Id, "with-summary", initialCapital: 100_000m, totalNetProfit: 12_500m, maxDrawdownPercent: 8.25m);

        var backtest = Assert.Single(workspaces.ListBacktests(workspace.Id));

        Assert.Equal("with-summary", backtest.FolderName);
        Assert.Equal(100_000m, backtest.InitialCapital);
        Assert.Equal(12_500m, backtest.TotalNetProfit);
        Assert.Equal(12.5m, backtest.NetProfitPercent);
        Assert.Equal(8.25m, backtest.MaxDrawdownPercent);
    }

    [Fact]
    public void FolderWithoutSummaryHasNoFigures()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest { Name = "figures" });

        Directory.CreateDirectory(BacktestPath(workspace.Id, "interrupted"));

        var backtest = Assert.Single(workspaces.ListBacktests(workspace.Id));

        Assert.Null(backtest.InitialCapital);
        Assert.Null(backtest.TotalNetProfit);
        Assert.Null(backtest.NetProfitPercent);
        Assert.Null(backtest.MaxDrawdownPercent);
    }

    /// <summary>Il drawdown e' dal picco e non ha bisogno del capitale: resta anche quando l'equity % no.</summary>
    [Fact]
    public void ZeroInitialCapitalGivesNoNetProfitPercentButKeepsDrawdown()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest { Name = "figures" });

        WriteSummary(workspace.Id, "no-capital", initialCapital: 0m, totalNetProfit: 500m, maxDrawdownPercent: 3.5m);

        var backtest = Assert.Single(workspaces.ListBacktests(workspace.Id));

        Assert.Equal(500m, backtest.TotalNetProfit);
        Assert.Null(backtest.NetProfitPercent);
        Assert.Equal(3.5m, backtest.MaxDrawdownPercent);
    }

    [Fact]
    public void UnreadableSummaryLeavesTheFolderInTheListWithoutFigures()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var workspace = workspaces.Create(new CreateWorkspaceRequest { Name = "figures" });

        var path = BacktestPath(workspace.Id, "truncated");
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, BacktestDiagnosticsSchema.SummaryFileName), "{ \"initialCapital\": 100000, \"tot");

        var backtest = Assert.Single(workspaces.ListBacktests(workspace.Id));

        Assert.Equal("truncated", backtest.FolderName);
        Assert.Null(backtest.NetProfitPercent);
        Assert.Null(backtest.MaxDrawdownPercent);
    }

    private string BacktestPath(string workspaceId, string folderName)
        => Path.Combine(_root, workspaceId, WorkspaceBacktestPaths.BacktestsDirectoryName, folderName);

    /// <summary>Lo stesso camelCase con cui <c>BacktestDiagnosticsLogger</c> scrive il summary.</summary>
    private void WriteSummary(string workspaceId, string folderName, decimal initialCapital, decimal totalNetProfit, decimal maxDrawdownPercent)
    {
        var maxDrawdown = maxDrawdownPercent;
        var path = BacktestPath(workspaceId, folderName);
        Directory.CreateDirectory(path);
        var summary = new
        {
            schemaVersion = BacktestDiagnosticsSchema.Version,
            jobId = "job-figures",
            initialCapital,
            finalEquity = initialCapital + totalNetProfit,
            totalNetProfit,
            maxDrawdown,
            totalTrades = 3,
            diagnostics = Array.Empty<string>()
        };
        File.WriteAllText(
            Path.Combine(path, BacktestDiagnosticsSchema.SummaryFileName),
            JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }
}
