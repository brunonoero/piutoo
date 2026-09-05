using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// La migrazione dei <c>plans.json</c> scritti quando il piano era una lista di righe
/// gruppo/account (<c>docs/decisioni.md</c> 2026-09-05).
///
/// <para>Il file reale del repository aveva <b>due gruppi con un conto ciascuno</b> e tetti diversi
/// (1 e 0): è esattamente il caso che questi test tengono fermo, perché è quello in cui una
/// migrazione sbagliata cambierebbe quanto opera un conto vero senza dirlo.</para>
/// </summary>
public sealed class PlanAccountsMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-plan-migrazione", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// I conti escono nell'ordine del file e il tetto è quello della prima riga. Non il massimo:
    /// allargare in silenzio un limite che una prop impone è l'unico dei due errori che può costare
    /// un conto.
    /// </summary>
    [Fact]
    public void UnPianoAGruppi_DiventaUnaListaDiConti()
    {
        var (workspaces, workspace) = NewWorkspace();
        WriteLegacyPlan(workspaces, workspace.Id, """
              {
                "WorkspaceId": "%WS%",
                "Code": "LEGACYMULTI",
                "Name": "piano storico",
                "Groups": [
                  { "GroupId": "ICS-01", "AccountNumber": "21341234", "MaxConcurrentTrades": 1 },
                  { "GroupId": "FTMO", "AccountNumber": "1234", "MaxConcurrentTrades": 0 }
                ],
                "GroupId": "ICS-01",
                "AccountNumber": "21341234",
                "MaxConcurrentTrades": 1,
                "CommissionPerContract": 2,
                "CreatedUtc": "2026-08-05T04:13:41Z",
                "UpdatedUtc": "2026-08-05T06:36:03Z"
              }
            """);

        var plan = new TradingPlanService(workspaces).Get(workspace.Id, "LEGACYMULTI");

        Assert.Equal(["21341234", "1234"], plan.Accounts);
        Assert.Equal("21341234", plan.AccountNumber);
        Assert.Equal(1, plan.MaxConcurrentTrades);
        Assert.Null(plan.Groups);
    }

    /// <summary>
    /// Il campo dei gruppi non torna nel file: alla prima riscrittura il piano è una lista di conti
    /// e basta. Due posti che dichiarano la stessa cosa sono la premessa della divergenza.
    /// </summary>
    [Fact]
    public void RiscrittoIlPiano_IlCampoDeiGruppiNonRitorna()
    {
        var (workspaces, workspace) = NewWorkspace();
        WriteLegacyPlan(workspaces, workspace.Id, """
              {
                "WorkspaceId": "%WS%",
                "Code": "LEGACYRW",
                "Name": "piano storico",
                "Groups": [
                  { "GroupId": "ICS-01", "AccountNumber": "21341234", "MaxConcurrentTrades": 2 }
                ],
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
            MaxConcurrentTrades = letto.MaxConcurrentTrades
        });

        var file = File.ReadAllText(Path.Combine(
            workspaces.GetWorkspacePath(workspace.Id), "plans", "plans.json"));

        Assert.DoesNotContain("\"Groups\"", file);
        Assert.DoesNotContain("ICS-01", file);
        Assert.Contains("21341234", file);
        Assert.Equal(2, plans.Get(workspace.Id, "LEGACYRW").MaxConcurrentTrades);
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
            Name = $"migrazione-{Guid.NewGuid():N}",
            StrategiesFilter = [strategy.Id]
        });
        return (workspaces, workspace);
    }
}
