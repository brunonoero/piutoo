using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La migrazione dei <c>plans.json</c> scritti fino alla 7.2, quando gli orari della
/// <c>Holding</c> erano interi <c>HHMM</c> (<c>SessionFlatUtcHhmm</c>, <c>FromUtcHhmm</c>,
/// <c>UntilUtcHhmm</c>). Oggi sono orari (<c>HH:mm:ss</c>), e i campi vecchi non esistono piu' nel
/// modello: senza traduzione un piano gia' salvato tornerebbe con i default, cioe' con orari
/// diversi da quelli scelti, senza che nulla lo dica. Vedi <c>docs/decisioni.md</c> 2026-09-11.
/// </summary>
public sealed class PlanHoldingTimesMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-plan-orari", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Orari volutamente diversi dai default: e' il solo modo di distinguere "migrato" da
    /// "ripiegato sul default", che sono indistinguibili quando il file dichiara 20:45 e 23:00.
    /// </summary>
    [Fact]
    public void LegacyHhmmTimesBecomeTimesOfDay()
    {
        var (workspaces, workspace) = NewWorkspace();
        WriteLegacyPlan(workspaces, workspace.Id, """
              {
                "WorkspaceId": "%WS%",
                "Code": "LEGACYHHMM",
                "Name": "piano storico",
                "Accounts": ["21341234"],
                "AccountNumber": "21341234",
                "Holding": {
                  "AllowOvernight": false,
                  "AllowOverweek": false,
                  "SessionFlatUtcHhmm": 2130,
                  "WeekEnd": { "FromUtcHhmm": 1915, "UntilUtcHhmm": 2205 }
                },
                "CreatedUtc": "2026-08-05T04:13:41Z",
                "UpdatedUtc": "2026-08-05T06:36:03Z"
              }
            """);

        var plan = new TradingPlanService(workspaces).Get(workspace.Id, "LEGACYHHMM");

        Assert.False(plan.Holding.AllowOvernight);
        Assert.Equal(new TimeOnly(21, 30), plan.Holding.SessionFlatUtc);
        Assert.Equal(new TimeOnly(19, 15), plan.Holding.WeekEnd.FromUtc);
        Assert.Equal(new TimeOnly(22, 5), plan.Holding.WeekEnd.UntilUtc);
    }

    /// <summary>Un file gia' nella forma nuova si legge cosi' com'e', e i campi vecchi non tornano alla riscrittura.</summary>
    [Fact]
    public void RewrittenPlanCarriesOnlyTimesOfDay()
    {
        var (workspaces, workspace) = NewWorkspace();
        WriteLegacyPlan(workspaces, workspace.Id, """
              {
                "WorkspaceId": "%WS%",
                "Code": "LEGACYRW",
                "Name": "piano storico",
                "Accounts": ["21341234"],
                "AccountNumber": "21341234",
                "Holding": { "AllowOvernight": false, "SessionFlatUtcHhmm": 2130 },
                "CreatedUtc": "2026-08-05T04:13:41Z",
                "UpdatedUtc": "2026-08-05T06:36:03Z"
              }
            """);

        var plans = new TradingPlanService(workspaces);
        var letto = plans.Get(workspace.Id, "LEGACYRW");
        plans.Save(workspace.Id, new SaveTradingPlanRequest
        {
            Code = letto.Code,
            Name = letto.Name,
            Accounts = letto.Accounts,
            Holding = letto.Holding
        });

        var file = File.ReadAllText(Path.Combine(
            workspaces.GetWorkspacePath(workspace.Id), "plans", "plans.json"));

        Assert.DoesNotContain("Hhmm", file);
        Assert.Contains("\"SessionFlatUtc\": \"21:30:00\"", file);
        Assert.Equal(new TimeOnly(21, 30), plans.Get(workspace.Id, "LEGACYRW").Holding.SessionFlatUtc);
    }

    /// <summary>
    /// Un piano scritto prima che il flat fosse una finestra non dichiara la durata: vale il default
    /// di trenta minuti, che copre il rollover dei broker misurati, e alla riscrittura si salva la
    /// durata e non l'ora di fine, che e' derivata.
    /// </summary>
    [Fact]
    public void APlanWithoutFlatWindowGetsTheDefaultDuration()
    {
        var (workspaces, workspace) = NewWorkspace();
        WriteLegacyPlan(workspaces, workspace.Id, """
              {
                "WorkspaceId": "%WS%",
                "Code": "NOWINDOW",
                "Name": "piano senza finestra",
                "Accounts": ["21341234"],
                "AccountNumber": "21341234",
                "Holding": { "AllowOvernight": false, "AllowOverweek": false, "SessionFlatUtc": "20:45:00" },
                "CreatedUtc": "2026-08-05T04:13:41Z",
                "UpdatedUtc": "2026-08-05T06:36:03Z"
              }
            """);

        var plans = new TradingPlanService(workspaces);
        var letto = plans.Get(workspace.Id, "NOWINDOW");
        Assert.Equal(TradingConventions.SessionFlatWindowMinutes, letto.Holding.SessionFlatWindowMinutes);
        Assert.Equal(new TimeOnly(21, 15), letto.Holding.SessionFlatUntilUtc);

        plans.Save(workspace.Id, new SaveTradingPlanRequest
        {
            Code = letto.Code,
            Name = letto.Name,
            Accounts = letto.Accounts,
            Holding = letto.Holding with { SessionFlatWindowMinutes = 45 }
        });

        var file = File.ReadAllText(Path.Combine(
            workspaces.GetWorkspacePath(workspace.Id), "plans", "plans.json"));

        Assert.Contains("\"SessionFlatWindowMinutes\": 45", file);
        Assert.DoesNotContain("SessionFlatUntilUtc", file);
        Assert.Equal(new TimeOnly(21, 30), plans.Get(workspace.Id, "NOWINDOW").Holding.SessionFlatUntilUtc);
    }

    private void WriteLegacyPlan(WorkspaceService workspaces, string workspaceId, string plan)
    {
        var directory = Path.Combine(workspaces.GetWorkspacePath(workspaceId), "plans");
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "plans.json"),
            "[\n" + plan.Replace("%WS%", workspaceId) + "\n]");
    }

    private (WorkspaceService Workspaces, WorkspaceInfo Workspace) NewWorkspace()
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var strategy = StrategyFactory.GetRegisteredStrategies().First();
        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"orari-{Guid.NewGuid():N}",
            StrategiesFilter = [strategy.Id]
        });
        return (workspaces, workspace);
    }
}
