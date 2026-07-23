using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Abstractions;
using Piootoo.Core.Services;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

public sealed class BacktestingCancellationHttpTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-cancel-http-{Guid.NewGuid():N}");
    private readonly ControlledExecutionHook _hook = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly WorkspaceInfo _workspace;
    private readonly ITestOutputHelper _output;

    public BacktestingCancellationHttpTests(ITestOutputHelper output)
    {
        _output = output;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Piootoo:BasePath"] = _root,
                ["Piootoo:SettingsPath"] = "[BasePath]\\settings",
                ["Piootoo:Workspaces"] = "[BasePath]\\workspaces"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBacktestingExecutionHook>();
                services.AddSingleton<IBacktestingExecutionHook>(_hook);
            });
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var workspaceService = _factory.Services.GetRequiredService<WorkspaceService>();
        _workspace = workspaceService.Create(new CreateWorkspaceRequest
        {
            Name = "Cancellation HTTP",
            StrategiesFilter = [StrategyFactory.GetRegisteredStrategies().First().Id]
        });
    }

    [Fact]
    public async Task RunningJob_CancelBecomesCancelledWithLowLatency()
    {
        var jobId = await Start("long-running");
        await _hook.WaitUntilRunning(jobId);
        Assert.Equal(BacktestingJobStatus.Running, (await Status(jobId)).Status);

        var stopwatch = Stopwatch.StartNew();
        var cancel = await _client.PostAsync($"api/Backtesting/cancel/{jobId}", null);
        cancel.EnsureSuccessStatusCode();
        var terminal = await WaitForTerminal(jobId);
        stopwatch.Stop();
        _output.WriteLine($"HTTP cancel -> Cancelled latency: {stopwatch.Elapsed.TotalMilliseconds:F1} ms");

        Assert.Equal(BacktestingJobStatus.Cancelled, terminal.Status);
        Assert.Equal("Cancelled", terminal.Phase);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Cancel latency: {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task Cancel_IsIdempotent()
    {
        var jobId = await Start("idempotent");
        await _hook.WaitUntilRunning(jobId);
        (await _client.PostAsync($"api/Backtesting/cancel/{jobId}", null)).EnsureSuccessStatusCode();
        await WaitForTerminal(jobId);

        var repeated = await _client.PostAsync($"api/Backtesting/cancel/{jobId}", null);
        repeated.EnsureSuccessStatusCode();
        Assert.Equal(
            BacktestingJobStatus.Cancelled,
            (await repeated.Content.ReadFromJsonAsync<BacktestingJob>(JsonOptions))!.Status);
    }

    [Fact]
    public async Task Cancel_UnknownJobReturnsProblemDetails404()
    {
        var response = await _client.PostAsync("api/Backtesting/cancel/unknown-job", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.Equal(404, problem!.Status);
        Assert.Equal("Job di backtesting non trovato", problem.Title);
    }

    [Fact]
    public async Task CancelCompletionRace_HasOneStableTerminalState()
    {
        var jobId = await Start("race");
        await _hook.WaitUntilRunning(jobId);

        var release = Task.Run(() => _hook.Release(jobId));
        var cancel = _client.PostAsync($"api/Backtesting/cancel/{jobId}", null);
        await Task.WhenAll(release, cancel);
        var firstTerminal = await WaitForTerminal(jobId);
        await Task.Delay(100);
        var secondRead = await Status(jobId);

        Assert.Contains(firstTerminal.Status, new[]
        {
            BacktestingJobStatus.Completed,
            BacktestingJobStatus.Failed,
            BacktestingJobStatus.Cancelled
        });
        Assert.Equal(firstTerminal.Status, secondRead.Status);
    }

    [Fact]
    public async Task CancellingOneJob_DoesNotCancelAnother()
    {
        var first = await Start("isolated-one");
        var second = await Start("isolated-two");
        await Task.WhenAll(_hook.WaitUntilRunning(first), _hook.WaitUntilRunning(second));

        (await _client.PostAsync($"api/Backtesting/cancel/{first}", null)).EnsureSuccessStatusCode();
        Assert.Equal(BacktestingJobStatus.Cancelled, (await WaitForTerminal(first)).Status);
        Assert.Equal(BacktestingJobStatus.Running, (await Status(second)).Status);

        (await _client.PostAsync($"api/Backtesting/cancel/{second}", null)).EnsureSuccessStatusCode();
        Assert.Equal(BacktestingJobStatus.Cancelled, (await WaitForTerminal(second)).Status);
    }

    [Fact]
    public async Task Cancel_ReleasesDirectoryLockAndAllowsSameNameRerun()
    {
        const string name = "reusable";
        var first = await Start(name);
        await _hook.WaitUntilRunning(first);
        (await _client.PostAsync($"api/Backtesting/cancel/{first}", null)).EnsureSuccessStatusCode();
        await WaitForTerminal(first);

        var second = await Start(name, overwrite: true);
        await _hook.WaitUntilRunning(second);
        Assert.NotEqual(first, second);
        Assert.Equal(BacktestingJobStatus.Running, (await Status(second)).Status);
        await _client.PostAsync($"api/Backtesting/cancel/{second}", null);
        await WaitForTerminal(second);
    }

    [Fact]
    public async Task Cancel_LeavesOnlyCoherentArtifactsAndNoFinalResult()
    {
        const string name = "artifacts";
        var jobId = await Start(name);
        await _hook.WaitUntilRunning(jobId);
        await _client.PostAsync($"api/Backtesting/cancel/{jobId}", null);
        await WaitForTerminal(jobId);

        var directory = _factory.Services.GetRequiredService<WorkspaceService>()
            .GetBacktestPath(_workspace.Id, name);
        Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(directory, "backtest_*.json", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(directory, "backtest_*.html", SearchOption.TopDirectoryOnly));
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"api/Backtesting/result/{jobId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"api/Backtesting/output/{jobId}/report")).StatusCode);
    }

    [Fact]
    public async Task Status_IsLightweightAndExposesProgress()
    {
        var jobId = await Start("light-status");
        await _hook.WaitUntilRunning(jobId);
        var json = await _client.GetStringAsync($"api/Backtesting/status/{jobId}");

        Assert.Contains("\"status\":\"Running\"", json);
        Assert.Contains("\"phase\":\"LoadingData\"", json);
        Assert.Contains("\"progressPercent\":0", json);
        Assert.DoesNotContain("\"result\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"hourlyResults\"", json, StringComparison.OrdinalIgnoreCase);

        await _client.PostAsync($"api/Backtesting/cancel/{jobId}", null);
        await WaitForTerminal(jobId);
    }

    private async Task<string> Start(string name, bool overwrite = false)
    {
        var response = await _client.PostAsJsonAsync("api/Backtesting/start", new BacktestingRequest
        {
            WorkspaceId = _workspace.Id,
            BacktestFolderName = name,
            Name = name,
            OverwriteExistingBacktest = overwrite,
            StartDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            InitialCapital = 1_000_000,
            CommissionPerContract = 2
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("jobId").GetString()!;
    }

    private async Task<BacktestingJob> Status(string jobId) =>
        (await _client.GetFromJsonAsync<BacktestingJob>($"api/Backtesting/status/{jobId}", JsonOptions))!;

    private async Task<BacktestingJob> WaitForTerminal(string jobId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            var job = await Status(jobId);
            if (job.Status is BacktestingJobStatus.Completed
                or BacktestingJobStatus.Failed
                or BacktestingJobStatus.Cancelled)
                return job;
            await Task.Delay(10, timeout.Token);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch (IOException) { }
    }

    private sealed class ControlledExecutionHook : IBacktestingExecutionHook
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _entered = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _releases = new();

        public async Task OnJobRunningAsync(string jobId, CancellationToken cancellationToken)
        {
            Entry(jobId).TrySetResult();
            await ReleaseSource(jobId).Task.WaitAsync(cancellationToken);
        }

        public Task WaitUntilRunning(string jobId) => Entry(jobId).Task.WaitAsync(TimeSpan.FromSeconds(5));
        public void Release(string jobId) => ReleaseSource(jobId).TrySetResult();

        private TaskCompletionSource Entry(string jobId) =>
            _entered.GetOrAdd(jobId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

        private TaskCompletionSource ReleaseSource(string jobId) =>
            _releases.GetOrAdd(jobId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}
