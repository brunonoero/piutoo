using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Una sessione realtime nata PRIMA che il server rifiutasse i contenitori di ricerca, e che ne ha
/// uno acceso, al riavvio del server si riprende comunque: scartarla lasciava senza sorveglianza le
/// posizioni gia' a mercato (23/09/2026, <c>PT3B_NQ_PCH_001_15</c> short su US100 rimasto orfano).
/// Il contenitore resta nella sessione, ma i suoi ingressi si scartano.
///
/// <para>La sessione "di prima" non si puo' piu' aprire dal percorso normale — e' proprio il
/// rifiuto che la regola ha introdotto — quindi il test ne fabbrica il dump: apre la sessione con
/// la sola strategia vera, poi scrive su disco il masterfilter col contenitore e l'impronta che la
/// sessione avrebbe avuto. L'impronta ricalca <c>BuildConfigurationFingerprint</c>: se quella
/// cambia forma, questo test lo dice con "piano cambiato" invece che con un falso verde.</para>
/// </summary>
public sealed class ResearchContainerRestoreTests : IDisposable
{
    private const string PlanCode = "PIANOCONTENITORE";
    private const string Account = "1002";
    private const string Container = "PT3B_NQ_PCH_001_15";
    private const string RealStrategy = "PT3B_FDAX_PCH_002_240";

    private static readonly DateTime Origin = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"piootoo-cont-restore-{Guid.NewGuid():N}");
    private readonly WorkspaceService _workspaces;
    private readonly string _workspaceId;

    public ResearchContainerRestoreTests()
    {
        _workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        _workspaceId = _workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"cont-restore-{Guid.NewGuid():N}",
            StrategiesFilter = [RealStrategy]
        }).Id;

        TestAccountRegistry.Register(_workspaces, Account);
        new TradingPlanService(_workspaces).Save(_workspaceId, new SaveTradingPlanRequest
        {
            Code = PlanCode,
            Name = "Piano con contenitore",
            AccountNumber = Account,
            Holding = AccountHoldingPolicy.Default
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ASessionWithAContainerIsRestoredAfterTheServerRestart()
    {
        var aperta = OpenSessionBornBeforeTheRule();

        var riavviato = NewService();
        var esito = Assert.Single(riavviato.RestoreSessions());

        Assert.True(esito.Restored, esito.Reason);
        Assert.Contains(Container, esito.Reason);
        Assert.Equal(aperta.SessionId, riavviato.GetSnapshot(aperta.SessionId, aperta.SessionToken).SessionId);
    }

    [Fact]
    public void TheRestoredContainerOpensNothingNewWhileTheOtherStrategiesDo()
    {
        var aperta = OpenSessionBornBeforeTheRule();
        var riavviato = NewService();
        riavviato.RestoreSessions();

        var nq = PushBar(riavviato, aperta, "NQ", 15);
        var fdax = PushBar(riavviato, aperta, "FDAX", 240);

        Assert.Empty(nq.Intents);
        Assert.Contains(fdax.Intents, intent => intent.StrategyCode == RealStrategy);
    }

    /// <summary>Senza ripresa di mezzo il rifiuto resta: la deroga vale solo per cio' che c'era gia'.</summary>
    [Fact]
    public void ANewSessionWithAContainerIsStillRejected()
    {
        WriteMasterFilter(RealStrategy, Container);

        var errore = Assert.Throws<ArgumentException>(() => Open(NewService()));
        Assert.Contains(Container, errore.Message);
    }

    // ------------------------------------------------------------------------------ infrastruttura

    private TradingSessionDescriptor OpenSessionBornBeforeTheRule()
    {
        var aperta = Open(NewService());

        WriteMasterFilter(RealStrategy, Container);
        var statePath = Directory.EnumerateFiles(
                Path.Combine(_workspaces.GetWorkspacePath(_workspaceId), "sessions"),
                SessionStateSchema.FileName, SearchOption.AllDirectories)
            .Single();
        var state = JsonNode.Parse(File.ReadAllText(statePath))!.AsObject();
        state["configurationFingerprint"] = Fingerprint([RealStrategy, Container], AccountHoldingPolicy.Default);
        File.WriteAllText(statePath, state.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return aperta;
    }

    private void WriteMasterFilter(params string[] ids) =>
        // Scritto a mano: SaveMasterFilter rifiuta i contenitori, ed e' il masterfilter di prima
        // della regola che si vuole riprodurre.
        File.WriteAllText(
            Path.Combine(_workspaces.GetWorkspacePath(_workspaceId), "masterfilter.json"),
            JsonSerializer.Serialize(new { Name = _workspaceId, StrategiesFilter = ids }));

    private static string Fingerprint(IEnumerable<string> ids, AccountHoldingPolicy holding)
    {
        var definitions = StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true)
            .ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
        var codici = ids
            .Select(id => definitions[id])
            .Select(d => StrategyFactory.CreateStrategy(d.Id, d.Symbol, d.TimeframeMinutes, d.Parameters)!)
            .Select(s => $"{s.Name}@{s.Symbol.Trim().TrimStart('@').ToUpperInvariant()}/{s.TimeframeMinutes}")
            .OrderBy(value => value, StringComparer.Ordinal);
        var canonico = string.Join(";", codici) +
                       $"|overnight={holding.AllowOvernight}|overweek={holding.AllowOverweek}" +
                       $"|flat={holding.SessionFlatUtc:HH\\:mm}" +
                       (holding.AllowOvernight ? string.Empty : $"+{holding.SessionFlatWindowMinutes}") +
                       $"|weekend={holding.WeekEnd.FromUtc:HH\\:mm}-{holding.WeekEnd.UntilUtc:HH\\:mm}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonico)));
    }

    private TradingSessionService NewService() => new(
        _workspaces,
        new TradingPlanService(_workspaces),
        new EntryOnEveryBarEvaluationService(),
        new PositionSizingService());

    private static TradingSessionDescriptor Open(TradingSessionService sessions) =>
        sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = PlanCode,
            ClientRunMode = ClientRunMode.Realtime,
            ExecutionKey = "LIVE",
            AccountNumber = Account,
            DistributeToAccounts = false
        });

    private static PushBarsResponse PushBar(
        TradingSessionService sessions, TradingSessionDescriptor descriptor, string symbol, int timeframeMinutes)
    {
        var barTimeUtc = Origin.AddMinutes(timeframeMinutes);
        return sessions.PushBars(new PushBarsRequest
        {
            SessionId = descriptor.SessionId,
            SessionToken = descriptor.SessionToken,
            Bars =
            [
                new ClosedBar
                {
                    Symbol = symbol,
                    TimeframeMinutes = timeframeMinutes,
                    BarTimeUtc = barTimeUtc,
                    Sequence = barTimeUtc.Ticks,
                    IdempotencyKey = $"bar-{symbol}-{barTimeUtc:O}",
                    Bar = new OhlcvData
                    {
                        DateTime = barTimeUtc, Open = 100, High = 101, Low = 99, Close = 100, Volume = 1
                    }
                }
            ]
        });
    }

    /// <summary>Un ingresso per ogni strategia della barra: cosi' si vede chi e' stato scartato.</summary>
    private sealed class EntryOnEveryBarEvaluationService : IStrategyEvaluationService
    {
        public IReadOnlyList<TradeSignal> Evaluate(
            IReadOnlyList<ITradingStrategy> strategies,
            ClosedBar closedBar,
            IReadOnlyList<OhlcvData> history,
            Func<ITradingStrategy, StrategyExecutionSnapshot> executionSnapshot) =>
            strategies
                .Where(x => string.Equals(x.Symbol.TrimStart('@'), closedBar.Symbol, StringComparison.OrdinalIgnoreCase) &&
                            x.TimeframeMinutes == closedBar.TimeframeMinutes)
                .Select(x => new TradeSignal
                {
                    StrategyCode = x.Name,
                    StrategyName = x.Name,
                    Symbol = closedBar.Symbol,
                    Date = closedBar.BarTimeUtc,
                    Type = SignalType.Buy,
                    Price = closedBar.Bar.Close,
                    Quantity = 1m
                })
                .ToList();
    }
}
