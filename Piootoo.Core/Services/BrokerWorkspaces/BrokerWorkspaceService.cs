using System.Text.Json;
using Piootoo.Core.Services.BestPlans;
using Piootoo.Shared.Models.BrokerWorkspaces;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services.BrokerWorkspaces;

/// <summary>
/// I piani in produzione, uno spazio per broker. Qui stanno le regole: ci si entra solo da un best
/// plan (o duplicando un piano di produzione per cambiarne i conti), il piano dichiara le strategie
/// attive, e dentro lo stesso broker una strategia o un conto stanno in un solo piano attivo.
/// Vedi <c>docs/domini/broker-workspace.md</c>.
/// </summary>
/// <remarks>
/// Le violazioni sono <see cref="InvalidOperationException"/> (<c>409</c>) che nominano il piano e la
/// strategia o il conto in conflitto; i dati mancanti o malformati <see cref="ArgumentException"/>.
/// </remarks>
public sealed class BrokerWorkspaceService
{
    private readonly BrokerWorkspaceStore _store;
    private readonly WorkspaceService _workspaces;
    private readonly TradingPlanService _plans;
    private readonly BestPlanService _bestPlans;
    private readonly object _gate = new();

    public BrokerWorkspaceService(
        BrokerWorkspaceStore store, WorkspaceService workspaces, TradingPlanService plans, BestPlanService bestPlans)
    {
        _store = store;
        _workspaces = workspaces;
        _plans = plans;
        _bestPlans = bestPlans;
    }

    public IReadOnlyList<BrokerWorkspaceSummary> List()
    {
        lock (_gate)
        {
            return _store.List().Select(workspace =>
            {
                var plans = _store.ReadPlans(workspace.BrokerCode);
                var active = plans.Where(plan => plan.RetiredUtc is null).ToList();
                return new BrokerWorkspaceSummary
                {
                    BrokerCode = workspace.BrokerCode,
                    BrokerName = _workspaces.FindBroker(workspace.BrokerCode)?.Name ?? workspace.BrokerCode,
                    CreatedUtc = workspace.CreatedUtc,
                    ActivePlans = active.Count,
                    RetiredPlans = plans.Count - active.Count,
                    ActiveStrategies = active.Sum(plan => plan.EnabledStrategies?.Count ?? 0),
                    ActiveAccounts = active.Sum(plan => plan.Accounts.Count)
                };
            }).ToList();
        }
    }

    public BrokerWorkspaceDetail Get(string brokerCode)
    {
        lock (_gate)
        {
            var workspace = _store.Read(brokerCode)
                ?? throw new KeyNotFoundException($"Il broker '{brokerCode}' non ha un broker workspace.");
            return new BrokerWorkspaceDetail
            {
                BrokerCode = workspace.BrokerCode,
                BrokerName = _workspaces.FindBroker(workspace.BrokerCode)?.Name ?? workspace.BrokerCode,
                CreatedUtc = workspace.CreatedUtc,
                Plans = _store.ReadPlans(workspace.BrokerCode)
                    .OrderBy(plan => plan.RetiredUtc is not null)
                    .ThenBy(plan => plan.Code, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }
    }

    public TradingPlan GetPlan(string brokerCode, string code)
    {
        lock (_gate)
            return FindPlan(_store.ReadPlans(brokerCode), code)
                ?? throw new KeyNotFoundException($"Piano '{code}' non trovato nel broker workspace '{brokerCode}'.");
    }

    /// <summary>
    /// Promuove un best plan a piano di produzione del broker <paramref name="brokerCode"/>, che deve
    /// essere il broker del piano di origine. Null = il broker del piano di origine: e' come promuove
    /// la console, che dal best plan non vede il broker. Il broker workspace nasce alla prima promozione.
    /// </summary>
    /// <remarks>
    /// <para><b>Il piano e' quello che il run ha misurato.</b> Le strategie attive vengono dal summary
    /// del run (<c>backtest-summary.json</c> o <c>session-summary.json</c>), il resto dal piano copiato
    /// alla promozione a best plan. Quella copia e' il piano al momento della promozione, non del run:
    /// se e' stato salvato dopo l'inizio del run non si puo' sapere che cosa il run abbia eseguito, e la
    /// promozione si rifiuta.</para>
    /// </remarks>
    public TradingPlan Promote(string? brokerCode, PromoteToProductionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.BestPlanId))
            throw new ArgumentException("Il best plan da promuovere è obbligatorio.");

        var bestPlan = _bestPlans.Get(request.BestPlanId.Trim());
        if (string.IsNullOrWhiteSpace(bestPlan.PlanCode))
            throw new ArgumentException(
                $"Il best plan '{bestPlan.Id}' viene da un run neutro sul masterfilter: non c'è un piano da promuovere.");

        var artifacts = _bestPlans.GetArtifactsDirectory(bestPlan.Id);
        var source = ReadSourcePlan(artifacts, bestPlan.Id);
        var origin = WorkspaceService.ReadBacktestOrigin(artifacts)
            ?? throw new InvalidOperationException(
                $"Il best plan '{bestPlan.Id}' non ha origin.json: non si sa quando è partito il run, quindi " +
                "nemmeno se il piano copiato è quello che il run ha eseguito.");

        if (source.UpdatedUtc > origin.CreatedUtc)
            throw new InvalidOperationException(
                $"Il piano '{source.Code}' è stato salvato il {source.UpdatedUtc:yyyy-MM-dd HH:mm} UTC, dopo l'inizio " +
                $"del run ({origin.CreatedUtc:yyyy-MM-dd HH:mm} UTC): il best plan '{bestPlan.Id}' non dice quale " +
                "configurazione è stata misurata. Rifare il backtest del piano e promuovere quello.");

        var broker = ResolveBroker(brokerCode, source);
        var strategies = ReadRunStrategies(artifacts, bestPlan.Id);
        var disabledButRun = strategies.Where(id => source.DisabledStrategies.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();
        if (disabledButRun.Count > 0)
            throw new InvalidOperationException(
                $"Il run del best plan '{bestPlan.Id}' ha eseguito strategie che il piano copiato tiene spente " +
                $"({string.Join(", ", disabledButRun)}): run e piano non corrispondono.");

        var accounts = request.Accounts.Count > 0 ? request.Accounts : source.Accounts.ToList();
        var now = DateTime.UtcNow;
        var plan = BuildPlan(
            request.PlanCode,
            string.IsNullOrWhiteSpace(request.Name) ? source.Name : request.Name.Trim(),
            broker.Code,
            ValidateAccounts(broker.Code, accounts),
            source,
            strategies,
            new PlanProvenance
            {
                BestPlanId = bestPlan.Id,
                SourceWorkspaceId = bestPlan.WorkspaceId,
                SourcePlanCode = source.Code,
                BacktestFolder = bestPlan.BacktestFolder,
                RunCreatedUtc = origin.CreatedUtc,
                PromotedUtc = now
            },
            now);

        return Add(broker.Code, plan, replaces: null, request.DryRun);
    }

    /// <summary>
    /// Duplica un piano di produzione con conti nuovi e ritira l'originale: e' il cambio di numero di
    /// conto del broker. Tutto il resto — strategie, pesi, tenuta, size — resta quello misurato.
    /// </summary>
    public TradingPlan Duplicate(string brokerCode, string code, DuplicateProductionPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Accounts.Count == 0)
            throw new ArgumentException("Il duplicato serve a cambiare i conti: indicare almeno un conto.");

        var source = GetPlan(brokerCode, code);
        var now = DateTime.UtcNow;
        var plan = BuildPlan(
            request.NewCode,
            string.IsNullOrWhiteSpace(request.NewName) ? source.Name : request.NewName.Trim(),
            source.BrokerCode,
            ValidateAccounts(source.BrokerCode, request.Accounts),
            source,
            source.EnabledStrategies ?? [],
            new PlanProvenance
            {
                BestPlanId = source.Provenance?.BestPlanId ?? string.Empty,
                SourceWorkspaceId = source.Provenance?.SourceWorkspaceId ?? string.Empty,
                SourcePlanCode = source.Provenance?.SourcePlanCode ?? string.Empty,
                BacktestFolder = source.Provenance?.BacktestFolder ?? string.Empty,
                RunCreatedUtc = source.Provenance?.RunCreatedUtc ?? default,
                PromotedUtc = now,
                PreviousPlanCode = source.Code
            },
            now);

        return Add(source.BrokerCode, plan, replaces: source.Code, request.DryRun);
    }

    /// <summary>
    /// Ritira un piano: resta leggibile con le sue sessioni, non ne apre di nuove e libera strategie e
    /// conti. Ritirare un piano gia' ritirato non cambia nulla.
    /// </summary>
    public TradingPlan Retire(string brokerCode, string code)
    {
        lock (_gate)
        {
            var workspace = _store.Read(brokerCode)
                ?? throw new KeyNotFoundException($"Il broker '{brokerCode}' non ha un broker workspace.");
            var plans = _store.ReadPlans(workspace.BrokerCode);
            var existing = FindPlan(plans, code)
                ?? throw new KeyNotFoundException($"Piano '{code}' non trovato nel broker workspace '{brokerCode}'.");
            if (existing.RetiredUtc is not null)
                return existing;

            var retired = WithRetirement(existing, DateTime.UtcNow);
            plans[plans.IndexOf(existing)] = retired;
            _store.Write(workspace, plans);
            return retired;
        }
    }

    /// <summary>
    /// Aggiunge il piano applicando le regole che dipendono dagli altri piani: codice unico, una
    /// strategia e un conto in un solo piano attivo. <paramref name="replaces"/> e' il piano che
    /// questo sostituisce e che viene ritirato nello stesso passaggio: non conta come conflitto.
    /// </summary>
    private TradingPlan Add(string brokerCode, TradingPlan plan, string? replaces, bool dryRun)
    {
        lock (_gate)
        {
            if (_plans.FindWorkspaceUsingCode(plan.Code) is { } workspaceId)
                throw new InvalidOperationException(
                    $"Il codice piano '{plan.Code}' è già usato nel workspace '{workspaceId}'.");

            var used = _store.ReadAllPlans()
                .FirstOrDefault(entry => entry.Plan.Code.Equals(plan.Code, StringComparison.OrdinalIgnoreCase));
            if (used.Plan is not null)
                throw new InvalidOperationException(
                    $"Il codice piano '{plan.Code}' è già usato da un piano di produzione del broker '{used.BrokerCode}'.");

            var workspace = _store.Read(brokerCode)
                ?? new BrokerWorkspace { BrokerCode = brokerCode, CreatedUtc = DateTime.UtcNow };
            var plans = _store.ReadPlans(brokerCode);
            var active = plans
                .Where(other => other.RetiredUtc is null)
                .Where(other => replaces is null || !other.Code.Equals(replaces, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var other in active)
            {
                var shared = (other.EnabledStrategies ?? [])
                    .Intersect(plan.EnabledStrategies ?? [], StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (shared.Count > 0)
                    throw new InvalidOperationException(
                        $"Le strategie {string.Join(", ", shared)} sono già nel piano attivo '{other.Code}' del broker " +
                        $"'{brokerCode}': dentro un broker una strategia sta su un conto solo. Ritirare '{other.Code}' " +
                        "o promuovere un piano senza quelle strategie.");

                var sharedAccounts = other.Accounts
                    .Intersect(plan.Accounts, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (sharedAccounts.Count > 0)
                    throw new InvalidOperationException(
                        $"Il conto {string.Join(", ", sharedAccounts)} esegue già il piano attivo '{other.Code}' del " +
                        $"broker '{brokerCode}': un conto esegue un piano solo.");
            }

            if (dryRun)
                return plan;

            if (replaces is not null && FindPlan(plans, replaces) is { RetiredUtc: null } replaced)
                plans[plans.IndexOf(replaced)] = WithRetirement(replaced, plan.CreatedUtc);

            plans.Add(plan);
            _store.Write(workspace, plans);
            return plan;
        }
    }

    private static TradingPlan BuildPlan(
        string code,
        string name,
        string brokerCode,
        IReadOnlyList<string> accounts,
        TradingPlan source,
        IReadOnlyList<string> strategies,
        PlanProvenance provenance,
        DateTime now)
    {
        var enabled = strategies
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Solo i pesi delle strategie del piano: un peso di una strategia che il piano non esegue
        // sarebbe una riga del file che non dice niente di vero.
        var weights = source.StrategyWeights
            .Where(entry => enabled.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        return new TradingPlan
        {
            WorkspaceId = string.Empty,
            Code = TradingPlanService.NormalizeCode(code),
            Name = name,
            BrokerCode = brokerCode,
            Accounts = accounts,
            AccountNumber = accounts[0],
            MaxConcurrentTrades = source.MaxConcurrentTrades,
            ConcurrencyCountMode = source.ConcurrencyCountMode,
            EnforceConcurrencyLimits = source.EnforceConcurrencyLimits,
            CommissionPerContract = source.CommissionPerContract,
            Holding = source.Holding,
            SizeMultiplier = TradingPlanService.NormalizeSizeMultiplier(source.SizeMultiplier),
            DisabledStrategies = [],
            StrategyWeights = TradingPlanService.NormalizeStrategyWeights(weights),
            PositionSizing = source.PositionSizing,
            CreatedUtc = now,
            UpdatedUtc = now,
            // Un piano di produzione non si modifica: nasce bloccato, e per cambiarlo si promuove altro.
            Locked = true,
            LockedUtc = now,
            EnabledStrategies = enabled,
            Provenance = provenance
        };
    }

    private static TradingPlan WithRetirement(TradingPlan plan, DateTime retiredUtc) => new()
    {
        WorkspaceId = plan.WorkspaceId,
        Code = plan.Code,
        Name = plan.Name,
        BrokerCode = plan.BrokerCode,
        Accounts = plan.Accounts,
        AccountNumber = plan.AccountNumber,
        MaxConcurrentTrades = plan.MaxConcurrentTrades,
        ConcurrencyCountMode = plan.ConcurrencyCountMode,
        EnforceConcurrencyLimits = plan.EnforceConcurrencyLimits,
        CommissionPerContract = plan.CommissionPerContract,
        Holding = plan.Holding,
        SizeMultiplier = plan.SizeMultiplier,
        DisabledStrategies = plan.DisabledStrategies,
        StrategyWeights = plan.StrategyWeights,
        PositionSizing = plan.PositionSizing,
        CreatedUtc = plan.CreatedUtc,
        UpdatedUtc = plan.UpdatedUtc,
        Locked = plan.Locked,
        LockedUtc = plan.LockedUtc,
        EnabledStrategies = plan.EnabledStrategies,
        Provenance = plan.Provenance,
        RetiredUtc = retiredUtc
    };

    private TradingBroker ResolveBroker(string? brokerCode, TradingPlan source)
    {
        if (string.IsNullOrWhiteSpace(source.BrokerCode))
            throw new InvalidOperationException(
                $"Il piano '{source.Code}' non dichiara un broker: non si sa in quale broker workspace metterlo. " +
                "Dichiarare il broker sul piano, rifare il backtest e promuovere quello.");

        var broker = _workspaces.FindBroker(source.BrokerCode)
            ?? throw new InvalidOperationException($"Il broker '{source.BrokerCode}' del piano '{source.Code}' non è in anagrafica.");

        if (!string.IsNullOrWhiteSpace(brokerCode)
            && !broker.Code.Equals(brokerCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Il piano '{source.Code}' è del broker '{broker.Code}', non di '{brokerCode}': un piano va nel broker workspace del proprio broker.");

        return broker;
    }

    /// <summary>
    /// I conti come vanno scritti: senza vuoti e senza doppioni, tutti in anagrafica, attivi e del
    /// broker. Un conto disattivato non esegue piani nuovi: e' il conto vecchio di un cambio di numero.
    /// </summary>
    private IReadOnlyList<string> ValidateAccounts(string brokerCode, IEnumerable<string> accounts)
    {
        var normalized = TradingPlanService.NormalizeAndValidateAccounts(new SaveTradingPlanRequest
        {
            Code = "-",
            Name = "-",
            Accounts = accounts.ToList()
        });
        _plans.ValidateBrokerAndAccounts(brokerCode, normalized);

        var registry = _workspaces.ListAccounts();
        var disabled = normalized
            .Where(number => registry.Any(account =>
                !account.Enabled && string.Equals(account.AccountNumber?.Trim(), number, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (disabled.Count > 0)
            throw new ArgumentException($"Il conto {string.Join(", ", disabled)} è disattivato in anagrafica.");

        return normalized;
    }

    private static TradingPlan ReadSourcePlan(string artifacts, string bestPlanId)
    {
        var file = Path.Combine(artifacts, BestPlanService.PlanSnapshotFileName);
        if (!File.Exists(file))
            throw new InvalidOperationException(
                $"Il best plan '{bestPlanId}' non ha la copia del piano ({BestPlanService.PlanSnapshotFileName}): " +
                "il piano non esisteva più quando il run è stato promosso.");

        try
        {
            return JsonSerializer.Deserialize<TradingPlan>(File.ReadAllText(file), BestPlanService.Json)
                ?? throw new InvalidOperationException($"La copia del piano del best plan '{bestPlanId}' è vuota.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"La copia del piano del best plan '{bestPlanId}' non è leggibile: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Le strategie eseguite dal run, per Id di catalogo, dal summary copiato nel best plan. Un
    /// contenitore di ricerca o un codice che il catalogo non conosce piu' fermano la promozione.
    /// </summary>
    private static IReadOnlyList<string> ReadRunStrategies(string artifacts, string bestPlanId)
    {
        var summaryFile = new[] { BacktestDiagnosticsSchema.SummaryFileName, SessionRunSummarySchema.FileName }
            .Select(name => Path.Combine(artifacts, name))
            .FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException(
                $"Il best plan '{bestPlanId}' non ha il summary del run: non si sa quali strategie ha eseguito.");

        using var document = JsonDocument.Parse(File.ReadAllText(summaryFile));
        if (!document.RootElement.TryGetProperty("strategies", out var entries) || entries.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException(
                $"Il summary del best plan '{bestPlanId}' non elenca le strategie del run.");

        var byCode = StrategyFactory.GetRegisteredStrategies(includeResearchContainers: true)
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Name))
            .GroupBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var ids = new List<string>();
        var unknown = new List<string>();
        var containers = new List<string>();
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("strategyCode", out var value) || value.GetString() is not { Length: > 0 } code)
                continue;

            if (!byCode.TryGetValue(code, out var definition))
                unknown.Add(code);
            else if (definition.IsResearchContainer)
                containers.Add(code);
            else
                ids.Add(definition.Id);
        }

        if (containers.Count > 0)
            throw new InvalidOperationException(
                $"Il run del best plan '{bestPlanId}' contiene contenitori di ricerca ({string.Join(", ", containers)}): " +
                "un contenitore non va su un conto.");
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"Il catalogo non conosce più le strategie {string.Join(", ", unknown)} del best plan '{bestPlanId}'.");
        if (ids.Count == 0)
            throw new InvalidOperationException($"Il run del best plan '{bestPlanId}' non ha strategie.");

        return ids;
    }

    private static TradingPlan? FindPlan(IEnumerable<TradingPlan> plans, string code)
    {
        var normalized = TradingPlanService.NormalizeCode(code);
        return plans.FirstOrDefault(plan => plan.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }
}
