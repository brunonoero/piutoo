using Piootoo.Core.Services;
using Piootoo.Core.Services.Compare;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il confronto avviato dalla console crea la cartella <c>compare-NNNN</c> successiva a quelle che
/// ci sono, ci mette gli artefatti dei due run con i nomi dell'export per confronto e fa girare lo
/// strumento con il broker del cBot. Una coppia che lo strumento non sa confrontare — due run dello
/// stesso motore, feed di broker diversi, interno senza summary — si rifiuta prima di creare la
/// cartella: una <c>compare-NNNN</c> vuota ruberebbe un numero e sembrerebbe un confronto.
/// </summary>
public sealed class CompareServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-compare-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly string _workspaceId;
    private readonly List<(string Folder, string? Broker)> _runs = new();

    public CompareServiceTests()
    {
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = Path.Combine(_root, "workspaces") });
        _workspaceId = _workspaces.Create(new CreateWorkspaceRequest { Name = "confronti" }).Id;
    }

    private string CompareRoot => Path.Combine(_root, "compare");

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void NextNumberFollowsTheHighestCompareFolderAndIgnoresTheOthers()
    {
        foreach (var name in new[] { "compare-0007", "compare-0040", "last-backtest", "strumento-confronto" })
            Directory.CreateDirectory(Path.Combine(CompareRoot, name));

        Assert.Equal(41, NewService().NextFolderNumber());
    }

    [Fact]
    public void NextNumberStartsAtOneWithoutAnyFolder()
        => Assert.Equal(1, NewService().NextFolderNumber());

    [Fact]
    public async Task CbotAgainstInternalFillsTheNextFolderAndRunsTheToolWithTheBroker()
    {
        Directory.CreateDirectory(Path.Combine(CompareRoot, "compare-0040"));
        WriteRun("cbot-run", BacktestOrigin.ExternalBroker, "FTMO", withSummary: false);
        WriteRun("interno-run", BacktestOrigin.Internal, "FTMO", withSummary: true);
        var service = NewService();

        // L'ordine della selezione non conta: il cBot si riconosce dal marcatore.
        var job = service.Start(new StartCompareRequest
        {
            WorkspaceId = _workspaceId,
            FirstBacktest = "interno-run",
            SecondBacktest = "cbot-run"
        });

        Assert.Equal("compare-0041", job.FolderName);
        Assert.Equal("cbot-run", job.CbotBacktest);
        Assert.Equal("interno-run", job.InternalBacktest);

        var done = await WaitForTerminal(service, job.JobId);
        Assert.Equal(BacktestingJobStatus.Completed, done.Status);

        var files = Directory.GetFiles(job.FolderPath).Select(Path.GetFileName).Order().ToArray();
        Assert.Equal(
            new[]
            {
                "backtest-summary-interno-cfd-FTMO.json",
                CompareService.OutcomeFileName,
                "run-cbot-cfd-FTMO.json",
                "run-interno-cfd-FTMO.json",
                "trades-cbot-cfd-FTMO.json",
                "trades-interno-cfd-FTMO.json"
            },
            files);
        Assert.Equal((job.FolderPath, "FTMO"), Assert.Single(_runs));
        Assert.Contains("cbot-cfd-FTMO contro interno-cfd-FTMO", File.ReadAllText(Path.Combine(job.FolderPath, CompareService.OutcomeFileName)));
    }

    [Fact]
    public async Task AFailingAnalysisLeavesTheFolderAndReportsTheError()
    {
        WriteRun("cbot-run", BacktestOrigin.ExternalBroker, "FTMO", withSummary: false);
        WriteRun("interno-run", BacktestOrigin.Internal, "FTMO", withSummary: true);
        var service = new CompareService(_workspaces, CompareRoot, (_, _, _, _) => throw new FileNotFoundException("manca il feed"));

        var job = service.Start(new StartCompareRequest { WorkspaceId = _workspaceId, FirstBacktest = "cbot-run", SecondBacktest = "interno-run" });
        var done = await WaitForTerminal(service, job.JobId);

        Assert.Equal(BacktestingJobStatus.Failed, done.Status);
        Assert.Equal("manca il feed", done.ErrorMessage);
        Assert.True(Directory.Exists(job.FolderPath));
    }

    [Fact]
    public void TwoInternalRunsAreRejectedWithoutCreatingAFolder()
    {
        WriteRun("interno-a", BacktestOrigin.Internal, "FTMO", withSummary: true);
        WriteRun("interno-b", BacktestOrigin.Internal, "FTMO", withSummary: true);

        AssertRejected("interno-a", "interno-b");
    }

    [Fact]
    public void DifferentBrokersAreRejectedWithoutCreatingAFolder()
    {
        WriteRun("cbot-run", BacktestOrigin.ExternalBroker, "FTMO", withSummary: false);
        WriteRun("interno-run", BacktestOrigin.Internal, "RAWTRADINGLTD", withSummary: true);

        AssertRejected("cbot-run", "interno-run");
    }

    [Fact]
    public void InternalRunWithoutSummaryIsRejectedWithoutCreatingAFolder()
    {
        WriteRun("cbot-run", BacktestOrigin.ExternalBroker, "FTMO", withSummary: false);
        WriteRun("interno-run", BacktestOrigin.Internal, "FTMO", withSummary: false);

        AssertRejected("cbot-run", "interno-run");
    }

    [Fact]
    public void ARunWithoutMarkerIsRejectedWithoutCreatingAFolder()
    {
        WriteRun("cbot-run", BacktestOrigin.ExternalBroker, "FTMO", withSummary: false);
        Directory.CreateDirectory(_workspaces.GetBacktestPath(_workspaceId, "vecchio-run"));

        AssertRejected("cbot-run", "vecchio-run");
    }

    private CompareService NewService()
        => new(_workspaces, CompareRoot, (folder, _, broker, _) =>
        {
            lock (_runs) _runs.Add((folder, broker));
            return Path.Combine(folder, "analisi");
        });

    private void AssertRejected(string first, string second)
    {
        var service = NewService();

        Assert.Throws<InvalidOperationException>(() => service.Start(new StartCompareRequest
        {
            WorkspaceId = _workspaceId,
            FirstBacktest = first,
            SecondBacktest = second
        }));

        Assert.False(Directory.Exists(CompareRoot) && Directory.EnumerateDirectories(CompareRoot).Any());
        Assert.Empty(_runs);
    }

    private void WriteRun(string folder, BacktestOrigin origin, string broker, bool withSummary)
    {
        var path = _workspaces.GetBacktestPath(_workspaceId, folder);
        Directory.CreateDirectory(path);
        WorkspaceService.WriteBacktestOrigin(path, new BacktestOriginInfo
        {
            Origin = origin,
            CreatedUtc = DateTime.UtcNow,
            PriceSource = RunPriceSource.Cfd(broker),
            EngineVersion = "7.3.0"
        });
        File.WriteAllText(Path.Combine(path, "trades.json"), "[]");
        if (withSummary)
            File.WriteAllText(Path.Combine(path, "backtest-summary.json"), "{}");
    }

    private static async Task<CompareJob> WaitForTerminal(CompareService service, string jobId)
    {
        for (var i = 0; i < 200; i++)
        {
            var job = service.GetStatus(jobId)!;
            if (job.Status is BacktestingJobStatus.Completed or BacktestingJobStatus.Failed)
                return job;
            await Task.Delay(25);
        }

        throw new TimeoutException("Il confronto non e' terminato.");
    }
}
