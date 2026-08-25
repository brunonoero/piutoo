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
using Piootoo.Shared.Configuration;
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
            // Tutti i path sotto la radice temporanea: l'appsettings del server punta al repository
            // reale della macchina, che in test non deve essere né letto né creato. Sovrascrivere il
            // solo Workspaces lasciava settings/ e accounts/ sul path di produzione.
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Piootoo:BasePath"] = _root,
                ["Piootoo:Workspaces"] = _root,
                ["Piootoo:SettingsPath"] = Path.Combine(_root, "settings"),
                ["Piootoo:Accounts"] = Path.Combine(_root, "accounts"),
                ["Piootoo:RepositoryPath"] = Path.Combine(_root, "datafeed"),
                ["Piootoo:StrategiesPath"] = Path.Combine(_root, "easy")
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

    /// <summary>
    /// Il piano viene persistito e l'apertura da piano configura la sessione (gruppi, lucchetti).
    ///
    /// <para>La riapertura con la STESSA execution key non e' piu' idempotente in backtest: ogni
    /// apertura e' un run nuovo e ottiene una sessione nuova. Vedi
    /// <see cref="OgniBacktest_ApreUnaSessioneNuova"/>.</para>
    /// </summary>
    [Fact]
    public async Task TradingPlanIsPersistedAndConfiguresSessionFromPlan()
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

    /// <summary>
    /// Ogni apertura in backtest e' un run nuovo: sessione nuova, mai una ripresa.
    ///
    /// <para>La execution key di un backtest e' deterministica (il cBot la deriva dalla data di
    /// inizio del run), quindi un run rilanciato — dopo uno stop a meta', dopo un crash, o solo per
    /// rifarlo — ricade sempre sulla chiave di prima. Se il server la riprendesse, il secondo run
    /// erediterebbe barre, intent, posizioni aperte e trade del primo: un risultato plausibile,
    /// coerente e sbagliato, senza un errore da nessuna parte.</para>
    /// </summary>
    [Fact]
    public async Task OgniBacktest_ApreUnaSessioneNuova()
    {
        var plan = await SavePlan("PLAN_BT_RESTART", "PROP-BT", "9001");
        var open = new OpenTradingPlanSessionRequest
        {
            PlanCode = plan.Code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "BT-20260101000000",
            AccountNumber = plan.AccountNumber
        };

        var first = await OpenPlan(open);
        var second = await OpenPlan(open);

        Assert.NotEqual(first.SessionId, second.SessionId);
        Assert.Equal(TradingSessionStatus.Running, second.Status);

        // La vecchia sessione non e' piu' raggiungibile: e' stata tolta dal registro e messa a
        // Stopped, cosi' un push in ritardo del client morente non puo' scrivere dentro la cartella
        // che il run nuovo ha appena riazzerato.
        using var stale = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{first.SessionId}/groups", first.SessionToken);
        var staleResponse = await _client.SendAsync(stale);
        Assert.Equal(HttpStatusCode.NotFound, staleResponse.StatusCode);
    }

    /// <summary>
    /// In realtime invece la riapertura RIPRENDE: la execution key e' costante ("LIVE") e una
    /// sessione lasciata aperta e' cio' che permette a un cBot riavviato di rientrare nel proprio
    /// run — con le sue posizioni e i suoi intent — invece di aprirne uno nuovo accanto.
    /// </summary>
    [Fact]
    public async Task Realtime_RiapertoConLaStessaChiave_RiprendeLaStessaSessione()
    {
        var plan = await SavePlan("PLAN_RT_RESUME", "PROP-RT", "9002");
        var open = new OpenTradingPlanSessionRequest
        {
            PlanCode = plan.Code,
            ClientRunMode = ClientRunMode.Realtime,
            ExecutionKey = "LIVE",
            AccountNumber = plan.AccountNumber
        };

        var first = await OpenPlan(open);
        var second = await OpenPlan(open);

        Assert.Equal(first.SessionId, second.SessionId);
    }

    /// <summary>
    /// La regola non guarda l'account: vale anche per un secondo cBot che apre lo stesso piano in
    /// backtest. Non si aggiunge al run del primo, ne apre uno proprio e scarta quello di prima.
    ///
    /// <para>E' la conseguenza voluta di "ogni backtest apre una sessione nuova". In pratica riguarda
    /// solo chi apre lo stesso piano in backtest da piu' client contemporaneamente: un backtest di
    /// cTrader e' per definizione a singolo account. In realtime, dove la distribuzione multi-account
    /// e' il caso normale, la ripresa resta e i leg continuano a condividere la sessione.</para>
    /// </summary>
    [Fact]
    public async Task BacktestDistribuito_UnSecondoAccount_ApreComunqueUnaSessioneNuova()
    {
        var plan = await SavePlan("PLAN_BT_MULTI", "PROP-BT2", "9003", secondAccount: "9004");
        var first = await OpenPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = plan.Code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "BT-20260101000000",
            AccountNumber = "9003"
        });
        var second = await OpenPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = plan.Code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "BT-20260101000000",
            AccountNumber = "9004"
        });

        Assert.NotEqual(first.SessionId, second.SessionId);
    }

    private async Task<SaveTradingPlanRequest> SavePlan(
        string code, string groupId, string accountNumber, string? secondAccount = null)
    {
        var plan = secondAccount is null
            ? new SaveTradingPlanRequest
            {
                Code = code,
                Name = code,
                GroupId = groupId,
                AccountNumber = accountNumber,
                ApplyTitanoFilters = false
            }
            : new SaveTradingPlanRequest
            {
                Code = code,
                Name = code,
                ApplyTitanoFilters = false,
                Groups =
                [
                    new TradingGroupRow
                    {
                        GroupId = groupId,
                        AccountNumber = accountNumber,
                        ApplyTitanoFilters = false
                    },
                    new TradingGroupRow
                    {
                        GroupId = groupId,
                        AccountNumber = secondAccount,
                        ApplyTitanoFilters = false
                    }
                ]
            };
        var save = await _client.PutAsJsonAsync(
            $"api/v1/workspaces/{_workspace.Id}/trading-plans/{plan.Code}", plan);
        save.EnsureSuccessStatusCode();
        return plan;
    }

    private async Task<TradingSessionDescriptor> OpenPlan(OpenTradingPlanSessionRequest request)
    {
        var response = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!;
    }

    /// <summary>
    /// Percorso di <c>PiootooTradingSessionBot</c>: il piano fornisce la configurazione, ma la
    /// sessione non ha gruppi, quindi <c>POST /bars</c> restituisce intent eseguibili invece di
    /// template da reclamare. La sessione è per account, altrimenti due cBot eseguirebbero gli
    /// stessi segnali.
    /// </summary>
    [Fact]
    public async Task DirectExecutionPlan_HasNoGroups_AndReturnsExecutableIntentsFromBars()
    {
        var plan = new SaveTradingPlanRequest
        {
            Code = "PLAN_DIRECT",
            Name = "Piano diretto",
            GroupId = "PROP-D",
            AccountNumber = "777",
            ApplyTitanoFilters = false
        };
        var save = await _client.PutAsJsonAsync(
            $"api/v1/workspaces/{_workspace.Id}/trading-plans/{plan.Code}", plan);
        save.EnsureSuccessStatusCode();

        var open = new OpenTradingPlanSessionRequest
        {
            PlanCode = plan.Code,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = "direct-1",
            AccountNumber = plan.AccountNumber,
            DistributeToAccounts = false
        };
        var response = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan", open);
        response.EnsureSuccessStatusCode();
        var descriptor = (await response.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!;

        // Il piano porta con sé il workspace; il valore punto viene dal registro strumenti, non dal
        // piano (docs/decisioni.md 2026-08-05). La granularità di volume non è più qui: per una
        // sessione ExternalBroker è "differita" all'account al claim (QuantityRoundingMode.Deferred).
        Assert.Equal(_workspace.Id, descriptor.WorkspaceId);
        var metadata = descriptor.InstrumentMetadata.Single();
        Assert.Equal(QuantityRoundingMode.Deferred, metadata.RoundingMode);
        // Il descriptor espone i simboli normalizzati (senza '@'): è la forma su cui il cBot
        // confronta il simbolo del grafico per accorgersi di essere sullo strumento sbagliato.
        var instrument = descriptor.Instruments.Single();
        Assert.Equal(_strategy.Symbol.TrimStart('@'), instrument.Symbol);
        Assert.Contains(_strategy.TimeframeMinutes, instrument.TimeframesMinutes);

        using var groupsRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{descriptor.SessionId}/groups", descriptor.SessionToken);
        var groupsResponse = await _client.SendAsync(groupsRequest);
        groupsResponse.EnsureSuccessStatusCode();
        Assert.Empty((await groupsResponse.Content.ReadFromJsonAsync<List<TradingGroupRow>>(JsonOptions))!);

        var intent = Assert.Single((await Push(descriptor, 1, "direct-bar")).Intents);
        Assert.Equal(OrderIntentStatus.Pending, intent.Status);
        Assert.Equal(3.75m, intent.FinalQuantity);

        // Stessa chiave ma distribuzione attiva: è un'altra esecuzione, non la stessa ripresa.
        var distributed = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan",
            new OpenTradingPlanSessionRequest
            {
                PlanCode = plan.Code,
                ClientRunMode = ClientRunMode.Backtest,
                ExecutionKey = "direct-1",
                AccountNumber = plan.AccountNumber
            });
        distributed.EnsureSuccessStatusCode();
        Assert.NotEqual(descriptor.SessionId,
            (await distributed.Content.ReadFromJsonAsync<TradingSessionDescriptor>(JsonOptions))!.SessionId);
    }

    /// <summary>
    /// <c>MaxConcurrentTrades</c> è applicato solo dal percorso di claim. In esecuzione diretta non
    /// esiste, quindi un piano che dichiara il limite va rifiutato invece di girare senza.
    /// </summary>
    [Fact]
    public async Task DirectExecutionPlan_IsRejectedWhenTheConcurrencyLimitCannotBeApplied()
    {
        var plan = new SaveTradingPlanRequest
        {
            Code = "PLAN_DIRECT_MAX",
            Name = "Piano diretto con limite",
            GroupId = "PROP-D",
            AccountNumber = "778",
            MaxConcurrentTrades = 2,
            ApplyTitanoFilters = false
        };
        var save = await _client.PutAsJsonAsync(
            $"api/v1/workspaces/{_workspace.Id}/trading-plans/{plan.Code}", plan);
        save.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("api/v1/trading-sessions/open-plan",
            new OpenTradingPlanSessionRequest
            {
                PlanCode = plan.Code,
                ClientRunMode = ClientRunMode.Realtime,
                ExecutionKey = "LIVE",
                AccountNumber = plan.AccountNumber,
                DistributeToAccounts = false
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        // Il file contiene ancora "InitialCapital": la proprietà non esiste più sul piano ed è qui
        // di proposito, perché i plans.json già scritti la contengono e devono restare leggibili.
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
                // Il broker riporta la commissione come addebito negativo: il dominio deve
                // normalizzarla a costo positivo, non accreditarla.
                Commission = -3m,
                // Lo swap invece conserva il segno, perché può essere anche un accredito.
                Swap = -7m,
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

        // Il P&L è in denaro, non in punti: senza il valore punto del contratto il trade esterno
        // usciva in punti mentre il backtest usciva in dollari, e i due non erano confrontabili.
        var expectedGross = 5m * closeIntent.FinalQuantity * InstrumentRegistry.PointValue(entryIntent.Symbol);
        Assert.Equal(expectedGross, trade.GrossProfit);

        // Commissione come costo positivo e swap con segno: il netto è gross - commissione + swap.
        // Con il segno del broker non normalizzato la commissione veniva accreditata, e un trade in
        // perdita poteva risultare in utile.
        Assert.Equal(3m, trade.Commission);
        Assert.Equal(-7m, trade.Swap);
        Assert.Equal(expectedGross - 3m - 7m, trade.NetProfit);
    }

    /// <summary>
    /// Una chiusura riportata come <c>Filled</c> ma con quantità zero deve comunque chiudere la
    /// posizione e produrre il trade.
    ///
    /// <para>È il caso della sessione <c>51aa58a8…</c> (backtest GC 11/08–23/10/2022). Il 26/09 alle
    /// 12:00 il cBot chiude entrambe le RHL e lo scrive
    /// (<c>Chiuso PTS_GC_RHL_001_60 … netto 245,84</c>), ma da quel momento il server risponde
    /// «l'account ha già un ingresso in corso per quella strategia su quel simbolo» fino al 21/10, e
    /// quei due trade non compaiono in <c>trades.json</c>. La posizione era rimasta in
    /// <c>ExternalPositions</c>, e con lei il lucchetto d'ingresso, per il resto del run: dal 26/09
    /// al 23/10 nessun nuovo ingresso di quelle due strategie è più passato.</para>
    ///
    /// <para>La causa è a monte: tutta la gestione della chiusura viveva dentro
    /// <c>if (delta &gt; 0)</c>, e un report a zero dava <c>delta == 0</c>. Zero non è una chiusura
    /// da zero contratti — è il client che ha fallito la conversione in contratti — quindi la
    /// quantità da registrare è quella dell'intent.</para>
    /// </summary>
    [Fact]
    public async Task ExternalCloseFillWithoutQuantityStillClosesPositionAndRecordsTrade()
    {
        var descriptor = await Create(ExecutionMode.ExternalBroker);
        descriptor = await Status(descriptor, "start", HttpStatusCode.OK);

        var entryIntent = Assert.Single((await Push(descriptor, 1, "zero-close-entry")).Intents);
        var entryReport = new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "zero-close-entry-fill", IntentId = entryIntent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = entryIntent.FinalQuantity, FillPrice = 100,
                EventTimeUtc = Utc(2026, 1, 5)
            }
        };
        (await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/execution-reports", entryReport))
            .EnsureSuccessStatusCode();

        var closeIntentResponse = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/intents/close-external",
            new CreateExternalCloseIntentRequest
            {
                SessionToken = descriptor.SessionToken,
                StrategyCode = entryIntent.StrategyCode,
                Symbol = entryIntent.Symbol,
                Reason = "LocalExit:StopLoss"
            });
        closeIntentResponse.EnsureSuccessStatusCode();
        var closeIntent = (await closeIntentResponse.Content.ReadFromJsonAsync<OrderIntent>(JsonOptions))!;
        Assert.True(closeIntent.FinalQuantity > 0m);

        // Il report che il cBot manda quando ToContractQuantity non ha trovato lo strumento o la
        // History: esito Filled, quantità zero.
        var closeReport = new ExecutionReportRequest
        {
            SessionToken = descriptor.SessionToken,
            Report = new ExternalExecutionReport
            {
                ReportId = "zero-close-fill", IntentId = closeIntent.IntentId,
                Status = ExecutionReportStatus.Filled,
                CumulativeFilledQuantity = 0m, FillPrice = 105,
                Commission = -3m, Swap = -7m,
                EventTimeUtc = Utc(2026, 1, 6)
            }
        };
        var closeResponse = await _client.PostAsJsonAsync(
            $"api/v1/trading-sessions/{descriptor.SessionId}/execution-reports", closeReport);
        closeResponse.EnsureSuccessStatusCode();

        // La posizione non deve sopravvivere alla propria chiusura: finché resta qui,
        // AccountHasEntryInFlight blocca ogni nuovo ingresso di quella strategia su quel simbolo.
        var snapshot = (await closeResponse.Content.ReadFromJsonAsync<TradingSessionSnapshot>(JsonOptions))!;
        Assert.Empty(snapshot.Positions);

        // E il trade deve esistere, con la quantità dell'intent: è il trade che nel run reale
        // mancava da trades.json pur essendo stampato nel log del cBot.
        using var tradesRequest = Authorized(HttpMethod.Get,
            $"api/v1/trading-sessions/{descriptor.SessionId}/trades", descriptor.SessionToken);
        var trades = (await (await _client.SendAsync(tradesRequest))
            .Content.ReadFromJsonAsync<List<PersistedTrade>>(JsonOptions))!;
        var trade = Assert.Single(trades);
        Assert.Equal(closeIntent.FinalQuantity, trade.Quantity);
        Assert.Equal(100m, trade.EntryPrice);
        Assert.Equal(105m, trade.ExitPrice);

        var expectedGross = 5m * closeIntent.FinalQuantity * InstrumentRegistry.PointValue(entryIntent.Symbol);
        Assert.Equal(expectedGross, trade.GrossProfit);
        Assert.Equal(expectedGross - 3m - 7m, trade.NetProfit);
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
        Assert.Equal("titano-source", snapshot.Groups[0].TitanoBacktestFolder);

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
            TitanoMode = runId is null ? TitanoFilterMode.Disabled : TitanoFilterMode.BacktestRotationFile
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
