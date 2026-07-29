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
        Validate(request);
        var code = NormalizeCode(request.Code);
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
                GroupId = request.GroupId.Trim(),
                AccountNumber = request.AccountNumber.Trim(),
                MaxConcurrentTrades = request.MaxConcurrentTrades,
                RotationSetupId = TrimOrNull(request.RotationSetupId),
                TitanoRunId = TrimOrNull(request.TitanoRunId),
                TitanoBacktestFolder = TrimOrNull(request.TitanoBacktestFolder),
                ApplyTitanoFilters = request.ApplyTitanoFilters,
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
        return JsonSerializer.Deserialize<List<TradingPlan>>(File.ReadAllText(file), _json) ?? [];
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

    private static void Validate(SaveTradingPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Il nome del piano è obbligatorio.");
        if (string.IsNullOrWhiteSpace(request.GroupId)) throw new ArgumentException("Il gruppo del piano è obbligatorio.");
        if (string.IsNullOrWhiteSpace(request.AccountNumber)) throw new ArgumentException("Il codice account è obbligatorio.");
        if (request.MaxConcurrentTrades < 0) throw new ArgumentException("MaxConcurrentTrades non può essere negativo.");
        if (request.InitialCapital <= 0) throw new ArgumentException("InitialCapital deve essere maggiore di zero.");
        if (request.CommissionPerContract < 0) throw new ArgumentException("CommissionPerContract non può essere negativa.");
        if (!string.IsNullOrWhiteSpace(request.TitanoRunId) &&
            string.IsNullOrWhiteSpace(request.TitanoBacktestFolder))
            throw new ArgumentException("TitanoRunId richiede TitanoBacktestFolder.");
        if (request.ApplyTitanoFilters && string.IsNullOrWhiteSpace(request.TitanoRunId))
            throw new ArgumentException("Applica Titano richiede un run Titano.");
    }

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
