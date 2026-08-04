using System.Text.Json;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;

namespace Piootoo.Core.Services;

/// <summary>Persistenza dei piani operativi sotto il workspace proprietario.</summary>
public sealed class TradingPlanService
{
    private const string PlansDirectoryName = "plans";
    private const string PlansFileName = "plans.json";
    private readonly WorkspaceService _workspaces;
    private readonly object _gate = new();
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public TradingPlanService(WorkspaceService workspaces) => _workspaces = workspaces;

    public IReadOnlyList<TradingPlan> List(string workspaceId)
    {
        lock (_gate)
            return Read(workspaceId).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public TradingPlan Get(string workspaceId, string code)
    {
        lock (_gate)
            return Find(Read(workspaceId), code)
                ?? throw new KeyNotFoundException($"Piano '{code}' non trovato nel workspace '{workspaceId}'.");
    }

    /// <summary>Il codice è globale: consente al cBot di configurare soltanto PlanCode.</summary>
    public TradingPlan Resolve(string code)
    {
        var normalized = NormalizeCode(code);
        lock (_gate)
        {
            var matches = _workspaces.List()
                .SelectMany(workspace => Read(workspace.Id))
                .Where(plan => plan.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return matches.Length switch
            {
                0 => throw new KeyNotFoundException($"Piano '{normalized}' non trovato."),
                1 => matches[0],
                _ => throw new InvalidOperationException(
                    $"Il codice piano '{normalized}' non è univoco. Correggere i file dei workspace.")
            };
        }
    }

    public TradingPlan Save(string workspaceId, SaveTradingPlanRequest request)
    {
        var groups = NormalizeAndValidateGroups(request);
        var code = NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Il nome del piano è obbligatorio.");
        if (request.InitialCapital <= 0)
            throw new ArgumentException("InitialCapital deve essere maggiore di zero.");
        if (request.CommissionPerContract < 0)
            throw new ArgumentException("CommissionPerContract non può essere negativa.");

        // Mirror della prima riga (e, per Titano di sessione, della prima riga con run) così i
        // piani multi-gruppo restano leggibili anche dai client che non conoscono ancora Groups.
        var primary = SelectPrimaryRow(groups);
        lock (_gate)
        {
            var collision = _workspaces.List()
                .Where(workspace => !workspace.Id.Equals(workspaceId, StringComparison.OrdinalIgnoreCase))
                .SelectMany(workspace => Read(workspace.Id))
                .FirstOrDefault(plan => plan.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (collision is not null)
                throw new InvalidOperationException(
                    $"Il codice piano '{code}' è già usato dal workspace '{collision.WorkspaceId}'.");

            var plans = Read(workspaceId);
            var existing = Find(plans, code);
            var now = DateTime.UtcNow;
            var plan = new TradingPlan
            {
                WorkspaceId = workspaceId,
                Code = code,
                Name = request.Name.Trim(),
                Groups = groups,
                GroupId = primary.GroupId,
                AccountNumber = primary.AccountNumber,
                MaxConcurrentTrades = primary.MaxConcurrentTrades,
                RotationSetupId = primary.RotationSetupId,
                TitanoBacktestFolder = primary.TitanoBacktestFolder,
                ApplyTitanoFilters = primary.ApplyTitanoFilters,
                EnforceConcurrencyLimits = request.EnforceConcurrencyLimits,
                InitialCapital = request.InitialCapital,
                CommissionPerContract = request.CommissionPerContract,
                PositionSizing = request.PositionSizing,
                Instruments = request.Instruments,
                CreatedUtc = existing?.CreatedUtc ?? now,
                UpdatedUtc = now
            };
            if (existing is not null) plans.Remove(existing);
            plans.Add(plan);
            Write(workspaceId, plans);
            return plan;
        }
    }

    public void Delete(string workspaceId, string code)
    {
        lock (_gate)
        {
            var plans = Read(workspaceId);
            var existing = Find(plans, code)
                ?? throw new KeyNotFoundException($"Piano '{code}' non trovato nel workspace '{workspaceId}'.");
            plans.Remove(existing);
            Write(workspaceId, plans);
        }
    }

    private List<TradingPlan> Read(string workspaceId)
    {
        var file = GetFile(workspaceId);
        if (!File.Exists(file)) return [];
        var plans = JsonSerializer.Deserialize<List<TradingPlan>>(File.ReadAllText(file), _json) ?? [];
        return plans.Select(NormalizeLoadedPlan).ToList();
    }

    private void Write(string workspaceId, List<TradingPlan> plans)
    {
        var file = GetFile(workspaceId);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        AtomicFileWriter.WriteAllText(file, JsonSerializer.Serialize(
            plans.OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase), _json));
    }

    private string GetFile(string workspaceId) =>
        Path.Combine(_workspaces.GetWorkspacePath(workspaceId), PlansDirectoryName, PlansFileName);

    private static TradingPlan? Find(IEnumerable<TradingPlan> plans, string code)
    {
        var normalized = NormalizeCode(code);
        return plans.FirstOrDefault(x => x.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Accetta <see cref="SaveTradingPlanRequest.Groups"/> oppure i campi legacy singoli;
    /// valida account univoci e profili Titano coerenti per gruppo.
    /// </summary>
    public static IReadOnlyList<TradingGroupRow> NormalizeAndValidateGroups(SaveTradingPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var groups = request.Groups.Count > 0
            ? request.Groups.Select(CloneRow).ToList()
            : BuildLegacyRows(request);

        if (groups.Count == 0)
            throw new ArgumentException("Il piano richiede almeno una riga gruppo/account.");

        foreach (var row in groups)
        {
            if (string.IsNullOrWhiteSpace(row.GroupId) || string.IsNullOrWhiteSpace(row.AccountNumber))
                throw new ArgumentException("GroupId e AccountNumber sono obbligatori per ogni riga del piano.");
            if (row.MaxConcurrentTrades < 0)
                throw new ArgumentException(
                    $"MaxConcurrentTrades non può essere negativo per l'account '{row.AccountNumber}'.");
            if (row.ApplyTitanoFilters && string.IsNullOrWhiteSpace(row.TitanoBacktestFolder))
                throw new ArgumentException(
                    $"Applica Titano richiede una cartella di backtest per il gruppo '{row.GroupId}'.");
        }

        var duplicatedAccount = groups.GroupBy(r => r.AccountNumber, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicatedAccount != null)
            throw new ArgumentException($"Account '{duplicatedAccount.Key}' configurato più di una volta nel piano.");

        foreach (var group in groups.GroupBy(r => r.GroupId, StringComparer.OrdinalIgnoreCase))
        {
            var signatures = group.Select(r => (
                RotationSetupId: r.RotationSetupId ?? string.Empty,
                TitanoBacktestFolder: r.TitanoBacktestFolder ?? string.Empty,
                r.ApplyTitanoFilters)).Distinct().ToArray();
            if (signatures.Length > 1)
                throw new ArgumentException(
                    $"Profilo Titano inconsistente tra le righe del gruppo '{group.Key}'.");
        }

        return groups;
    }

    private static List<TradingGroupRow> BuildLegacyRows(SaveTradingPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupId) || string.IsNullOrWhiteSpace(request.AccountNumber))
            return [];

        return
        [
            new TradingGroupRow
            {
                GroupId = request.GroupId.Trim(),
                AccountNumber = request.AccountNumber.Trim(),
                MaxConcurrentTrades = request.MaxConcurrentTrades,
                RotationSetupId = TrimOrNull(request.RotationSetupId),
                TitanoBacktestFolder = TrimOrNull(request.TitanoBacktestFolder),
                ApplyTitanoFilters = request.ApplyTitanoFilters
            }
        ];
    }

    private static TradingPlan NormalizeLoadedPlan(TradingPlan plan)
    {
        var groups = plan.Groups.Count > 0
            ? plan.Groups.Select(CloneRow).ToList()
            : string.IsNullOrWhiteSpace(plan.GroupId) || string.IsNullOrWhiteSpace(plan.AccountNumber)
                ? []
                :
                [
                    new TradingGroupRow
                    {
                        GroupId = plan.GroupId.Trim(),
                        AccountNumber = plan.AccountNumber.Trim(),
                        MaxConcurrentTrades = plan.MaxConcurrentTrades,
                        RotationSetupId = TrimOrNull(plan.RotationSetupId),
                        TitanoBacktestFolder = TrimOrNull(plan.TitanoBacktestFolder),
                        ApplyTitanoFilters = plan.ApplyTitanoFilters
                    }
                ];

        if (groups.Count == 0)
            return plan;

        var primary = SelectPrimaryRow(groups);
        return new TradingPlan
        {
            WorkspaceId = plan.WorkspaceId,
            Code = plan.Code,
            Name = plan.Name,
            Groups = groups,
            GroupId = primary.GroupId,
            AccountNumber = primary.AccountNumber,
            MaxConcurrentTrades = primary.MaxConcurrentTrades,
            RotationSetupId = primary.RotationSetupId,
            TitanoBacktestFolder = primary.TitanoBacktestFolder,
            ApplyTitanoFilters = primary.ApplyTitanoFilters,
            EnforceConcurrencyLimits = plan.EnforceConcurrencyLimits,
            InitialCapital = plan.InitialCapital,
            CommissionPerContract = plan.CommissionPerContract,
            PositionSizing = plan.PositionSizing,
            Instruments = plan.Instruments,
            CreatedUtc = plan.CreatedUtc,
            UpdatedUtc = plan.UpdatedUtc
        };
    }

    /// <summary>
    /// Prima riga con una cartella Titano configurata, altrimenti la prima riga: alimenta i campi
    /// mirror e la modalità Titano di sessione in <c>OpenFromPlan</c>.
    /// </summary>
    public static TradingGroupRow SelectPrimaryRow(IReadOnlyList<TradingGroupRow> groups) =>
        groups.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row.TitanoBacktestFolder)) ?? groups[0];

    private static TradingGroupRow CloneRow(TradingGroupRow row) => new()
    {
        GroupId = row.GroupId.Trim(),
        AccountNumber = row.AccountNumber.Trim(),
        MaxConcurrentTrades = row.MaxConcurrentTrades,
        RotationSetupId = TrimOrNull(row.RotationSetupId),
        TitanoBacktestFolder = TrimOrNull(row.TitanoBacktestFolder),
        ApplyTitanoFilters = row.ApplyTitanoFilters
    };

    private static string NormalizeCode(string code)
    {
        var normalized = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length == 0) throw new ArgumentException("Il codice piano è obbligatorio.");
        if (normalized.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("Il codice piano può contenere solo lettere, numeri, '-' e '_'.");
        return normalized;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
