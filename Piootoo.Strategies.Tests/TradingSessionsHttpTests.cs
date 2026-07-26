using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Piootoo.Core.Services;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Optimization;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Strategies.Tests;

public sealed class TradingSessionsHttpTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-http-{Guid.NewGuid():N}");
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly WorkspaceInfo _workspace;
    private readonly StrategyDefinition _strategy;

    public TradingSessionsHttpTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Piootoo:Workspaces"] = _root
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IStrategyEvaluationService>();
                services.AddSingleton<IStrategyEvaluationService, HttpFixedSignalEvaluation>();
            });
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var workspaces = _factory.Services.GetRequiredService<WorkspaceService>();
        _strategy = StrategyFactory.GetRegisteredStrategies().First();
        _workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = "HTTP", StrategiesFilter = [_strategy.Id]
        });
    }

    [Theory]
    [InlineData(ExecutionMode.ServerSimulated)]
    [InlineData(ExecutionMode.ExternalBroker)]
    public async Task FullLifecycleUsesSharedSizingAndProblemDetails(ExecutionMode mode)
    {
        var descriptor = await Create(mode);
        Assert.Equal(0.25m, descriptor.InstrumentMetadata.Single().QuantityStep);
        descriptor = await Status(descriptor, "start", HttpStatusCode.OK);

        var first = await Push(descriptor, 1, $"{mode}-one");
        var intent = Assert.Single(first.Intents);
        Assert.Equal(3.75m, intent.FinalQuantity);
        Assert.Equal(3.9m, intent.BaseQuantity);
        Assert.Equal(1m, intent.StrategyEquityMultiplier);

        var replay = await Push(descriptor, 1, $"{mode}-one");
        Assert.Equal(1, replay.DuplicateBars);
        Assert.Empty(replay.Intents);

        var bad = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/bars",
            Bars(descriptor, 0, $"{mode}-old"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal("Richiesta non valida", (await bad.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(JsonOptions))!.Title);

        if (mode == ExecutionMode.ExternalBroker)
        {
            var report = new ExecutionReportRequest
            {
                SessionToken = descriptor.SessionToken,
                Report = new ExternalExecutionReport
                {
                    ReportId = "fill", IntentId = intent.IntentId, Status = ExecutionReportStatus.Filled,
                    CumulativeFilledQuantity = intent.FinalQuantity, FillPrice = 100,
                    EventTimeUtc = Utc(2026, 1, 5)
                }
            };
            var response = await _client.PostAsJsonAsync(
                $"api/v1/trading-sessions/{descriptor.SessionId}/execution-reports", report);
            response.EnsureSuccessStatusCode();
        }

        await Status(descriptor, "stop", HttpStatusCode.OK);
        await Status(descriptor, "resume", HttpStatusCode.OK);
        using var snapshotRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{descriptor.SessionId}/snapshot", descriptor.SessionToken);
        var snapshotResponse = await _client.SendAsync(snapshotRequest);
        snapshotResponse.EnsureSuccessStatusCode();
        Assert.Equal(TradingSessionStatus.Running,
            (await snapshotResponse.Content.ReadFromJsonAsync<TradingSessionSnapshot>(JsonOptions))!.Status);
    }

    [Fact]
    public async Task ExternalCloseIntentAllowsReportingLocallyDecidedExit()
    {
        var descriptor = await Create(ExecutionMode.ExternalBroker);
        descriptor = await Status(descriptor, "start", HttpStatusCode.OK);

        var pushed = await Push(descriptor, 1, "close-external-entry");
        var entryIntent = Assert.Single(pushed.Intents);

        var entryReport = new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "entry-fill", IntentId = entryIntent.IntentId, Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = entryIntent.FinalQuantity, FillPrice = 100,
                EventTimeUtc = Utc(2026, 1, 5)
            }
        };
        var entryResponse = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/execution-reports", entryReport);
        entryResponse.EnsureSuccessStatusCode();

        // Il cBot ha deciso in locale di chiudere (es. Stop Loss nativo o limite di barre): non esiste
        // un OrderIntent CloseOnly del server, quindi registra prima l'intent client-originated...
        var closeIntentRequest = new CreateExternalCloseIntentRequest
        {
            SessionToken = descriptor.SessionToken,
            StrategyCode = entryIntent.StrategyCode,
            Symbol = entryIntent.Symbol,
            Reason = "LocalMaxBars"
        };
        var closeIntentResponse = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/intents/close-external", closeIntentRequest);
        closeIntentResponse.EnsureSuccessStatusCode();
        var closeIntent = (await closeIntentResponse.Content.ReadFromJsonAsync<OrderIntent>(JsonOptions))!;
        Assert.True(closeIntent.CloseOnly);
        Assert.Equal(entryIntent.FinalQuantity, closeIntent.FinalQuantity);

        // ...e poi lo referenzia nel normale execution-report, esattamente come per un intent CloseOnly
        // emesso dal server.
        var closeReport = new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "close-fill", IntentId = closeIntent.IntentId, Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = closeIntent.FinalQuantity, FillPrice = 105,
                EventTimeUtc = Utc(2026, 1, 6)
            }
        };
        var closeResponse = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/execution-reports", closeReport);
        closeResponse.EnsureSuccessStatusCode();

        using var tradesRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{descriptor.SessionId}/trades", descriptor.SessionToken);
        var tradesResponse = await _client.SendAsync(tradesRequest);
        tradesResponse.EnsureSuccessStatusCode();
        var trades = (await tradesResponse.Content.ReadFromJsonAsync<List<PersistedTrade>>(JsonOptions))!;
        var trade = Assert.Single(trades);
        Assert.Equal(entryIntent.StrategyCode, trade.StrategyCode);
        Assert.Equal(100m, trade.EntryPrice);
        Assert.Equal(105m, trade.ExitPrice);
    }

    [Fact]
    public async Task ExternalCloseIntentRejectedWithoutOpenPosition()
    {
        var descriptor = await Create(ExecutionMode.ExternalBroker);
        descriptor = await Status(descriptor, "start", HttpStatusCode.OK);

        var request = new CreateExternalCloseIntentRequest
        {
            SessionToken = descriptor.SessionToken,
            StrategyCode = _strategy.Id,
            Symbol = _strategy.Symbol
        };
        var response = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/intents/close-external", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TitanoRunFiltersSignalsThroughHttpBoundary()
    {
        var workspaces = _factory.Services.GetRequiredService<WorkspaceService>();
        var backtest = workspaces.GetBacktestPath(_workspace.Id, "titano-source");
        var store = new TradingJsonStore(backtest);
        store.Initialize();
        var manifest = _factory.Services.GetRequiredService<TitanoRotationService>().Run(new TitanoRotationRequest
        {
            WorkspaceId = _workspace.Id, BacktestFolder = "titano-source",
            StartUtc = Utc(2026, 1, 1), EndUtc = Utc(2026, 2, 1), MinimumTrades = 1
        });
        var descriptor = await Create(ExecutionMode.ServerSimulated, manifest.RunId, "titano-source");
        descriptor = await Status(descriptor, "start", HttpStatusCode.OK);
        var result = await Push(descriptor, 1, "titano-filter");
        Assert.Empty(result.Intents);
    }

    private async Task<TradingSessionDescriptor> Create(
        ExecutionMode mode, string? runId = null, string? folder = null)
    {
        var response = await _client.PostAsJsonAsync("api/v1/trading-sessions", new CreateTradingSessionRequest
        {
            WorkspaceId = _workspace.Id, ExecutionMode = mode, TitanoRunId = runId,
            TitanoBacktestFolder = folder,
            Instruments =
            [
                new InstrumentMetadata
                {
                    Symbol = _strategy.Symbol, DollarsPerPoint = 50,
                    MinimumQuantity = 0.25m, QuantityStep = 0.25m,
                    RoundingMode = QuantityRoundingMode.BrokerVolumeStep
                }
            ]
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!;
    }

    private async Task<TradingSessionDescriptor> Status(
        TradingSessionDescriptor descriptor, string action, HttpStatusCode expected)
    {
        using var request = Authorized(HttpMethod.Post,
            $"api/v1/trading-sessions/{descriptor.SessionId}/{action}", descriptor.SessionToken);
        var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!;
    }

    private async Task<PushBarsResponse> Push(TradingSessionDescriptor descriptor, long sequence, string key)
    {
        var response = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/bars", Bars(descriptor, sequence, key));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PushBarsResponse>(JsonOptions))!;
    }

    private PushBarsRequest Bars(TradingSessionDescriptor descriptor, long sequence, string key) => new()
    {
        SessionId = descriptor.SessionId, SessionToken = descriptor.SessionToken,
        Bars =
        [
            new ClosedBar
            {
                Symbol = _strategy.Symbol, TimeframeMinutes = _strategy.TimeframeMinutes,
                BarTimeUtc = Utc(2026, 1, 5), Sequence = sequence, IdempotencyKey = key,
                Bar = new OhlcvData { DateTime = Utc(2026, 1, 5), Open = 100, High = 101, Low = 99, Close = 100 }
            }
        ]
    };

    private static HttpRequestMessage Authorized(HttpMethod method, string uri, string token)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Session-Token", token);
        return request;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch (IOException) { }
    }

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private sealed class HttpFixedSignalEvaluation : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies, ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) =>
            strategies.Take(1).Select(strategy => new TradeSignal
            {
                StrategyCode = strategy.Name, StrategyName = strategy.Name, Symbol = strategy.Symbol,
                Date = closedBar.BarTimeUtc, Type = SignalType.Buy, Quantity = 3.9m, Price = closedBar.Bar.Close
            }).ToArray();
    }
}
