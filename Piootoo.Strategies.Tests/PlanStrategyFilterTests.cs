using Piootoo.Core.Services;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;
using Xunit;

namespace Piootoo.Strategies.Tests;

/// <summary>
/// Il secondo filtro: il masterfilter del workspace dice quali strategie esistono, il piano ne
/// spegne un sottoinsieme (<c>TradingPlan.DisabledStrategies</c>).
///
/// <para>Il punto che questi test tengono fermo è che lo spegnimento sia <b>rumoroso</b> nei due
/// modi che contano: le strategie spente non entrano nella sessione (e si vede nel descriptor, che
/// è ciò che il cBot mostra a chart), e spegnerle <i>tutte</i> non apre una sessione muta ma fa
/// fallire l'apertura. Una sessione senza strategie è indistinguibile da una che non produce
/// segnali, e il posto peggiore per accorgersene è il conto vero.</para>
/// </summary>
public sealed class PlanStrategyFilterTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "piootoo-plan-strategie", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Due strategie nel masterfilter, una spenta dal piano: la sessione ne valuta una sola, ed è
    /// quella accesa. Il confronto passa dal <c>Name</c> perché è ciò che il descriptor pubblica
    /// (CLAUDE.md, «Id ≠ Name»): l'Id serve solo a selezionare dal catalogo.
    /// </summary>
    [Fact]
    public void UnaStrategiaSpentaNonEntraNellaSessione()
    {
        var (workspaces, workspace, definitions) = NewWorkspace(strategie: 2);
        var plans = new TradingPlanService(workspaces);
        var code = NewPlanCode();
        plans.Save(workspace.Id, Plan(code, disabled: [definitions[0].Id]));

        var descriptor = OpenSession(workspaces, plans, code);

        Assert.Equal(
            [definitions[1].Name],
            descriptor.Strategies.Select(strategy => strategy.StrategyCode).ToArray());
    }

    /// <summary>
    /// Senza spegnimenti il piano continua a valere quanto il masterfilter: è il comportamento di
    /// ogni piano scritto prima che il campo esistesse, e non deve cambiare.
    /// </summary>
    [Fact]
    public void SenzaSpegnimenti_LaSessioneValeIlMasterfilter()
    {
        var (workspaces, workspace, definitions) = NewWorkspace(strategie: 2);
        var plans = new TradingPlanService(workspaces);
        var code = NewPlanCode();
        plans.Save(workspace.Id, Plan(code, disabled: []));

        var descriptor = OpenSession(workspaces, plans, code);

        Assert.Equal(
            definitions.Select(definition => definition.Name).OrderBy(x => x).ToArray(),
            descriptor.Strategies.Select(strategy => strategy.StrategyCode).OrderBy(x => x).ToArray());
    }

    /// <summary>
    /// Spente tutte, l'apertura fallisce. È la differenza fra un piano che non opera e un piano che
    /// sembra operare: il secondo lo si scopre solo contando i trade che non ci sono.
    /// </summary>
    [Fact]
    public void SpegnerleTutte_FaFallireLApertura()
    {
        var (workspaces, workspace, definitions) = NewWorkspace(strategie: 2);
        var plans = new TradingPlanService(workspaces);
        var code = NewPlanCode();
        plans.Save(workspace.Id, Plan(code, disabled: definitions.Select(d => d.Id).ToArray()));

        var errore = Assert.Throws<ArgumentException>(() => OpenSession(workspaces, plans, code));

        Assert.Contains("spente", errore.Message);
    }

    /// <summary>
    /// Uno spegnimento su un Id che il masterfilter non contiene non è un errore e non viene
    /// scartato al salvataggio: il masterfilter cambia, e se quella strategia vi rientra deve
    /// ritrovarsi spenta invece di riaccendersi di nascosto.
    /// </summary>
    [Fact]
    public void UnoSpegnimentoFuoriDalMasterfilterRestaScritto()
    {
        var (workspaces, workspace, definitions) = NewWorkspace(strategie: 2);
        var plans = new TradingPlanService(workspaces);
        var code = NewPlanCode();

        plans.Save(workspace.Id, Plan(code, disabled: [definitions[0].Id, "Strategia_Che_Non_Esiste"]));

        Assert.Equal(
            new[] { "Strategia_Che_Non_Esiste", definitions[0].Id }
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(),
            plans.Get(workspace.Id, code).DisabledStrategies.ToArray());

        // E non impedisce alla sessione di aprirsi con ciò che resta acceso.
        var descriptor = OpenSession(workspaces, plans, code);
        Assert.Equal([definitions[1].Name], descriptor.Strategies.Select(s => s.StrategyCode).ToArray());
    }

    /// <summary>Doppioni, spazi e vuoti non arrivano al file: l'elenco è normalizzato in scrittura.</summary>
    [Fact]
    public void GliIdSpentiSonoNormalizzati()
    {
        var (workspaces, workspace, definitions) = NewWorkspace(strategie: 2);
        var plans = new TradingPlanService(workspaces);
        var code = NewPlanCode();

        var salvato = plans.Save(workspace.Id, Plan(code, disabled:
            [$"  {definitions[0].Id}  ", definitions[0].Id.ToUpperInvariant(), "   ", string.Empty]));

        Assert.Equal([definitions[0].Id], salvato.DisabledStrategies.ToArray());
    }

    private static TradingSessionDescriptor OpenSession(
        WorkspaceService workspaces, TradingPlanService plans, string planCode)
    {
        var sessions = new TradingSessionService(
            workspaces, plans, new StrategyEvaluationService(), positionSizing: new PositionSizingService());

        return sessions.OpenFromPlan(new OpenTradingPlanSessionRequest
        {
            PlanCode = planCode,
            ClientRunMode = ClientRunMode.Backtest,
            ExecutionKey = $"run-{Guid.NewGuid():N}"
        });
    }

    private static SaveTradingPlanRequest Plan(string code, IReadOnlyList<string> disabled) => new()
    {
        Code = code,
        Name = "Piano strategie",
        AccountNumber = "1001",
        DisabledStrategies = disabled
    };

    /// <summary>Il codice piano è globale su tutti i workspace della stessa radice.</summary>
    private static string NewPlanCode() => $"PLANSTR{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    private (WorkspaceService Workspaces, WorkspaceInfo Workspace, StrategyDefinition[] Strategies)
        NewWorkspace(int strategie)
    {
        var workspaces = new WorkspaceService(new PiootooSettings { Workspaces = _root });
        var definitions = StrategyFactory.GetRegisteredStrategies()
            .Where(x => !string.IsNullOrWhiteSpace(x.Symbol) && x.TimeframeMinutes > 0)
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Take(strategie)
            .ToArray();
        Assert.Equal(strategie, definitions.Length);

        var workspace = workspaces.Create(new CreateWorkspaceRequest
        {
            Name = $"strategie-{Guid.NewGuid():N}",
            StrategiesFilter = definitions.Select(x => x.Id).ToList()
        });
        TestAccountRegistry.Register(workspaces, "1001");
        return (workspaces, workspace, definitions);
    }
}
