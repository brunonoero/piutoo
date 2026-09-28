using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services.Plans;

/// <summary>Dove vive un piano: un workspace di ricerca o, dalla fase 3, un broker workspace.</summary>
public enum PlanHomeKind
{
    Workspace,
    Broker
}

/// <summary>
/// La casa di un piano: la cartella sotto cui stanno <c>sessions/</c> e <c>backtests/</c> delle sue
/// esecuzioni.
/// </summary>
public sealed record PlanHome(PlanHomeKind Kind, string Id, string RootPath);

/// <summary>
/// Un piano con cio' che serve per eseguirlo, risolto in un punto solo.
/// </summary>
/// <param name="UniverseStrategyIds">
/// Le strategie da cui il piano parte, per Id di catalogo: il masterfilter del workspace, oppure
/// l'elenco dichiarato di un piano di produzione. E' anche cio' che il raccoglitore di datafeed segue,
/// spente comprese: riaccendere una strategia non deve trovare un buco nel feed.
/// </param>
public sealed record ResolvedPlan(TradingPlan Plan, PlanHome Home, IReadOnlyList<string> UniverseStrategyIds)
{
    /// <summary>Le strategie che il piano esegue: l'universo meno quelle che tiene spente.</summary>
    public IReadOnlyList<string> ActiveStrategyIds
    {
        get
        {
            var disabled = new HashSet<string>(
                TradingPlanService.NormalizeDisabledStrategies(Plan.DisabledStrategies), StringComparer.OrdinalIgnoreCase);
            return UniverseStrategyIds.Where(id => !disabled.Contains(id)).ToArray();
        }
    }
}

/// <summary>
/// L'unico punto che risponde a "dammi il piano con questo codice": sessioni, ripresa, backtest,
/// raccolta del datafeed e presidio del conto passano da qui.
///
/// <para><b>Perche' esiste.</b> Le strategie di un piano di workspace sono
/// <c>masterfilter − DisabledStrategies</c>, e ogni chiamante le ricavava per conto proprio. Un
/// piano di produzione le dichiara invece da se' (<see cref="TradingPlan.EnabledStrategies"/>): con
/// due regole scritte in sette posti, prima o poi una sessione e il suo backtest avrebbero eseguito
/// due insiemi diversi senza che niente lo dicesse. Vedi <c>docs/domini/broker-workspace.md</c>.</para>
///
/// <para>Il masterfilter di un workspace si legge qui e, fuori da qui, solo dove non c'e' un piano:
/// le sessioni manuali e il backtest neutro. <c>PlanResolutionConformanceTests</c> lo impone.</para>
/// </summary>
public sealed class PlanResolver
{
    private readonly WorkspaceService _workspaces;
    private readonly TradingPlanService _plans;

    public PlanResolver(WorkspaceService workspaces, TradingPlanService plans)
    {
        _workspaces = workspaces;
        _plans = plans;
    }

    /// <summary>
    /// Il piano per codice, cercato ovunque: il codice e' globale, ed e' cio' che lascia al cBot un
    /// solo parametro.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Nessun piano con quel codice.</exception>
    public ResolvedPlan Resolve(string planCode) => ForWorkspacePlan(_plans.Resolve(planCode));

    /// <summary>
    /// Il piano di un workspace indicato: e' come lo nomina una richiesta di backtest, che porta il
    /// workspace accanto al codice.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Il workspace non ha un piano con quel codice.</exception>
    public ResolvedPlan Resolve(string workspaceId, string planCode) => ForWorkspacePlan(_plans.Get(workspaceId, planCode));

    /// <summary>
    /// Le cartelle <c>sessions/</c> in cui la ripresa dopo un riavvio cerca le sessioni realtime. Una
    /// casa illeggibile si salta: non deve impedire la ripresa delle altre.
    /// </summary>
    public IReadOnlyList<string> SessionDirectories()
    {
        var directories = new List<string>();
        foreach (var workspace in _workspaces.List())
        {
            try
            {
                var path = Path.Combine(_workspaces.GetWorkspacePath(workspace.Id), "sessions");
                if (Directory.Exists(path))
                    directories.Add(path);
            }
            catch (Exception)
            {
                // Un workspace con un id non valido non e' il problema della ripresa.
            }
        }

        return directories;
    }

    /// <summary>
    /// I codici dei piani che nominano il conto, ovunque stiano. Un workspace illeggibile si salta:
    /// chi chiede (il presidio del conto) non deve fallire per un file che non c'entra.
    /// </summary>
    public IReadOnlyList<string> PlanCodesForAccount(string accountNumber)
    {
        var codes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var workspace in _workspaces.List())
        {
            try
            {
                foreach (var plan in _plans.List(workspace.Id))
                    if (plan.Accounts.Any(number =>
                            string.Equals(number?.Trim(), accountNumber, StringComparison.OrdinalIgnoreCase)))
                        codes.Add(plan.Code);
            }
            catch (Exception)
            {
                // Un piano illeggibile non e' il problema che il presidio sta cercando.
            }
        }

        return codes.ToList();
    }

    private ResolvedPlan ForWorkspacePlan(TradingPlan plan)
    {
        var home = new PlanHome(PlanHomeKind.Workspace, plan.WorkspaceId, _workspaces.GetWorkspacePath(plan.WorkspaceId));
        var universe = _workspaces.GetMasterFilter(plan.WorkspaceId).StrategiesFilter.ToArray();
        return new ResolvedPlan(plan, home, universe);
    }
}
