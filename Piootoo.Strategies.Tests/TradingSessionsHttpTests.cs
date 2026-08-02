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

    [Fact]
    public async Task TradingPlanIsPersistedAndOpensIdempotentSession()
    {
        var plan = new SaveTradingPlanRequest
        {
            Code = "PLAN_HTTP",
            Name = "Piano HTTP",
            GroupId = "PROP-A",
            AccountNumber = "12345",
            MaxConcurrentTrades = 2,
            ApplyTitanoFilters = false
        };
        var save = await _client.PutAsJsonAsync(
            $"api/v1/workspaces/{_workspace.Id}/trading-plans/{plan.Code}", plan);
        save.EnsureSuccessStatusCode();

        var open = new OpenTradingPlanSessionRequest
        {
            PlanCode = plan.Code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "run-1",
            AccountNumber = plan.AccountNumber
        };
        var firstResponse = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan", open);
        firstResponse.EnsureSuccessStatusCode();
        var first = (await firstResponse.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!;
        var secondResponse = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan", open);
        secondResponse.EnsureSuccessStatusCode();
        var second = (await secondResponse.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!;

        Assert.Equal(first.SessionId, second.SessionId);
        Assert.Equal(plan.Code, first.PlanCode);
        Assert.Equal(TradingSessionStatus.Running, first.Status);

        using var groupsRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{first.SessionId}/groups", first.SessionToken);
        var groupsResponse = await _client.SendAsync(groupsRequest);
        groupsResponse.EnsureSuccessStatusCode();
        var group = Assert.Single(
            (await groupsResponse.Content.ReadFromJsonAsync<List<TradingGroupRow>>(JsonOptions))!);
        Assert.Equal(plan.AccountNumber, group.AccountNumber);
        Assert.Equal(2, group.MaxConcurrentTrades);
    }

    [Fact]
    public async Task MultiGroupTradingPlan_PersistsAllRowsAndOpensWithAllAccounts()
    {
        var plan = new SaveTradingPlanRequest
        {
            Code = "PLAN_MULTI",
            Name = "Piano multi",
            ApplyTitanoFilters = false,
            Groups =
            [
                new TradingGroupRow
                {
                    GroupId = "PROP-A",
                    AccountNumber = "111",
                    MaxConcurrentTrades = 2,
                    ApplyTitanoFilters = false
                },
                new TradingGroupRow
                {
                    GroupId = "PROP-B",
                    AccountNumber = "222",
                    MaxConcurrentTrades = 1,
                    ApplyTitanoFilters = false
                }
            ]
        };
        var save = await _client.PutAsJsonAsync(
            $"api/v1/workspaces/{_workspace.Id}/trading-plans/{plan.Code}", plan);
        save.EnsureSuccessStatusCode();
        var saved = (await save.Content.ReadFromJsonAsync<TradingPlan>(JsonOptions))!;
        Assert.Equal(2, saved.Groups.Count);
        Assert.Equal("PROP-A", saved.GroupId);
        Assert.Equal("111", saved.AccountNumber);

        var open = new OpenTradingPlanSessionRequest
        {
            PlanCode = plan.Code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "multi-1",
            AccountNumber = "222"
        };
        var response = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan", open);
        response.EnsureSuccessStatusCode();
        var descriptor = (await response.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!;

        using var groupsRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{descriptor.SessionId}/groups", descriptor.SessionToken);
        var groupsResponse = await _client.SendAsync(groupsRequest);
        groupsResponse.EnsureSuccessStatusCode();
        var groups = (await groupsResponse.Content.ReadFromJsonAsync<List<TradingGroupRow>>(JsonOptions))!;
        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, row => row.AccountNumber == "111" && row.GroupId == "PROP-A");
        Assert.Contains(groups, row => row.AccountNumber == "222" && row.GroupId == "PROP-B" &&
                                       row.MaxConcurrentTrades == 1);

        var foreign = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan",
            new OpenTradingPlanSessionRequest
            {
                PlanCode = plan.Code,
                ClientRunMode = ClientRunMode.Backtest,
                ExecutionKey = "multi-foreign",
                AccountNumber = "999"
            });
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
    }

    [Fact]
    public async Task LegacySingleRowPlanJson_IsNormalizedToGroupsOnRead()
    {
        var workspaces = _factory.Services.GetRequiredService<WorkspaceService>();
        var plansDir = Path.Combine(workspaces.GetWorkspacePath(_workspace.Id), "plans");
        Directory.CreateDirectory(plansDir);
        await File.WriteAllTextAsync(Path.Combine(plansDir, "plans.json"),
            $$"""
            [
              {
                "WorkspaceId": "{{_workspace.Id}}",
                "Code": "LEGACY1",
                "Name": "Piano legacy",
                "GroupId": "PROP-L",
                "AccountNumber": "555",
                "MaxConcurrentTrades": 3,
                "ApplyTitanoFilters": false,
                "InitialCapital": 100000,
                "CommissionPerContract": 2,
                "CreatedUtc": "2026-01-01T00:00:00Z",
                "UpdatedUtc": "2026-01-01T00:00:00Z"
              }
            ]
            """);

        var response = await _client.GetAsync(
            $"api/v1/workspaces/{_workspace.Id}/trading-plans/LEGACY1");
        response.EnsureSuccessStatusCode();
        var plan = (await response.Content.ReadFromJsonAsync<TradingPlan>(JsonOptions))!;
        var row = Assert.Single(plan.Groups);
        Assert.Equal("PROP-L", row.GroupId);
        Assert.Equal("555", row.AccountNumber);
        Assert.Equal(3, row.MaxConcurrentTrades);
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
        // un intent di chiusura del server, quindi lo registra come OrderIntentKind.Close...
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
        Assert.True(closeIntent.IsClose);
        Assert.Equal(entryIntent.FinalQuantity, closeIntent.FinalQuantity);

        // ...e poi lo referenzia nel normale execution-report, come qualsiasi altro intent
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
    public async Task ExitOnlySignalEmitsBrokerCloseIntentForConfirmedPosition()
    {
        var descriptor = await Create(ExecutionMode.ExternalBroker);
        descriptor = await Status(descriptor, "start", HttpStatusCode.OK);

        var entry = Assert.Single((await Push(descriptor, 1, "exit-only-entry")).Intents);
        var entryReport = new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "exit-only-entry-fill",
                IntentId = entry.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = entry.FinalQuantity,
                FillPrice = 100,
                EventTimeUtc = Utc(2026, 1, 5)
            }
        };
        (await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/execution-reports", entryReport))
            .EnsureSuccessStatusCode();

        var close = Assert.Single((await Push(descriptor, 2, "exit-only-close")).Intents);
        Assert.True(close.IsClose);
        Assert.Equal(OrderIntentKind.Close, close.Kind);
        Assert.Equal(entry.StrategyCode, close.StrategyCode);
        Assert.Equal(entry.FinalQuantity, close.FinalQuantity);
        Assert.Equal(SignalType.Buy, close.Side);
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

    [Fact]
    public async Task TradingGroupsEndpoint_PersistsProfileAndKeepsAccountGroupsCompatible()
    {
        var descriptor = await Create(ExecutionMode.ExternalBroker);
        using var putRequest = new HttpRequestMessage(HttpMethod.Put,
            $"api/v1/trading-sessions/{descriptor.SessionId}/groups")
        {
            Content = JsonContent.Create(new SetTradingGroupsRequest
            {
                SessionToken = descriptor.SessionToken,
                Rows =
                [
                    new TradingGroupRow
                    {
                        GroupId = "prop-a",
                        AccountNumber = "1001",
                        RotationSetupId = "bilanciato",
                        TitanoRunId = "run-test",
                        TitanoBacktestFolder = "titano-source",
                        ApplyTitanoFilters = true
                    }
                ]
            }, options: JsonOptions)
        };
        var putResponse = await _client.SendAsync(putRequest);
        putResponse.EnsureSuccessStatusCode();
        var snapshot = await putResponse.Content.ReadFromJsonAsync<TradingSessionSnapshot>(JsonOptions);
        Assert.NotNull(snapshot);
        Assert.Single(snapshot!.Groups);
        Assert.Equal("run-test", snapshot.Groups[0].TitanoRunId);

        using var getGroupsRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{descriptor.SessionId}/groups", descriptor.SessionToken);
        var groupsResponse = await _client.SendAsync(getGroupsRequest);
        groupsResponse.EnsureSuccessStatusCode();
        var groups = await groupsResponse.Content.ReadFromJsonAsync<List<TradingGroupRow>>(JsonOptions);
        Assert.Equal("1001", Assert.Single(groups!).AccountNumber);

        using var legacyRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{descriptor.SessionId}/account-groups", descriptor.SessionToken);
        var legacyResponse = await _client.SendAsync(legacyRequest);
        legacyResponse.EnsureSuccessStatusCode();
        var legacy = await legacyResponse.Content.ReadFromJsonAsync<List<AccountGroupMapping>>(JsonOptions);
        Assert.Equal("prop-a", Assert.Single(legacy!).GroupId);
    }

    /// <summary>
    /// Il cBot serializza <c>Bars.Last(1).OpenTime</c>, che cTrader restituisce senza flag Kind
    /// anche con <c>[Robot(TimeZone = TimeZones.UTC)]</c>. Senza <c>SpecifyKind</c> il JSON parte
    /// privo del suffisso "Z", il server rilegge Kind=Unspecified e la barra va rifiutata: se
    /// passasse, un cBot configurato su un fuso diverso vedrebbe il proprio wall-clock locale
    /// accettato come UTC da <c>ToFeedUtc</c>, spostando tutto il feed in silenzio.
    /// </summary>
    [Theory]
    [InlineData("2026-01-05T00:00:00", false)]
    [InlineData("2026-01-05T00:00:00Z", true)]
    public async Task BarTimeWithoutTheUtcSuffixIsRejectedAtTheHttpBoundary(
        string barTime, bool accepted)
    {
        var descriptor = await Create(ExecutionMode.ServerSimulated);
        descriptor = await Status(descriptor, "start", HttpStatusCode.OK);

        var payload = $$"""
        {
          "sessionId": "{{descriptor.SessionId}}",
          "sessionToken": "{{descriptor.SessionToken}}",
          "bars": [
            {
              "symbol": "{{_strategy.Symbol}}",
              "timeframeMinutes": {{_strategy.TimeframeMinutes}},
              "barTimeUtc": "{{barTime}}",
              "sequence": 1,
              "idempotencyKey": "kind-{{barTime}}",
              "bar": {
                "dateTime": "{{barTime}}",
                "open": 100, "high": 101, "low": 99, "close": 100, "volume": 1
              }
            }
          ]
        }
        """;

        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/bars", content);

        if (!accepted)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }

        response.EnsureSuccessStatusCode();
        var pushed = (await response.Content.ReadFromJsonAsync<PushBarsResponse>(JsonOptions))!;
        Assert.Equal(1, pushed.AcceptedBars);
    }

    private async Task<TradingSessionDescriptor> Create(
        ExecutionMode mode, string? runId = null, string? folder = null)
    {
        var response = await _client.PostAsJsonAsync("api/v1/trading-sessions", new CreateTradingSessionRequest
        {
            WorkspaceId = _workspace.Id, ExecutionMode = mode, TitanoRunId = runId,
            TitanoBacktestFolder = folder,
            // Con un run collegato la sessione va filtrata: e' lo scopo del test che lo passa.
            TitanoMode = runId is null ? TitanoFilterMode.Disabled : TitanoFilterMode.BacktestRotationFile,
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
            strategies.Take(1).Select(strategy =>
            {
                var inPosition = executionSnapshot(strategy).Position is not null;
                return new TradeSignal
                {
                    StrategyCode = strategy.Name, StrategyName = strategy.Name, Symbol = strategy.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = inPosition ? SignalType.Sell : SignalType.Buy,
                    ExitOnly = inPosition,
                    Quantity = 3.9m,
                    Price = closedBar.Bar.Close
                };
            }).ToArray();
    }
}
