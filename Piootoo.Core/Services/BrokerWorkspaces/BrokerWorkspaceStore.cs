using System.Text.Json;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.BrokerWorkspaces;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services.BrokerWorkspaces;

/// <summary>
/// Il deposito dei broker workspace: <c>{radice}\{BROKER}\broker-workspace.json</c> e
/// <c>{radice}\{BROKER}\plans\plans.json</c>, nello stesso formato dei piani di workspace.
///
/// <para>Solo lettura e scrittura, senza regole: le regole stanno in <see cref="BrokerWorkspaceService"/>.
/// E' separato perche' anche <see cref="TradingPlanService"/> deve leggerlo — il codice di un piano e'
/// unico fra workspace e broker workspace — e il servizio dipende a sua volta dai piani di workspace.</para>
/// </summary>
public sealed class BrokerWorkspaceStore
{
    private const string PlansDirectoryName = "plans";
    private const string PlansFileName = "plans.json";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _root;

    public BrokerWorkspaceStore(PiootooSettings settings)
        : this(settings.GetBrokerWorkspacesPath())
    {
    }

    /// <summary>Radice iniettata: per i test.</summary>
    public BrokerWorkspaceStore(string root) => _root = Path.GetFullPath(root);

    /// <summary>I broker workspace esistenti, per codice di broker.</summary>
    public IReadOnlyList<BrokerWorkspace> List()
    {
        if (!Directory.Exists(_root))
            return [];

        return Directory.EnumerateDirectories(_root)
            .Select(directory => Read(Path.GetFileName(directory)))
            .OfType<BrokerWorkspace>()
            .OrderBy(workspace => workspace.BrokerCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public BrokerWorkspace? Read(string brokerCode)
    {
        var file = Path.Combine(GetPath(brokerCode), BrokerWorkspace.FileName);
        return File.Exists(file) ? JsonSerializer.Deserialize<BrokerWorkspace>(File.ReadAllText(file), Json) : null;
    }

    public List<TradingPlan> ReadPlans(string brokerCode)
    {
        var file = GetPlansFile(brokerCode);
        if (!File.Exists(file))
            return [];

        var plans = JsonSerializer.Deserialize<List<TradingPlan>>(File.ReadAllText(file), Json) ?? [];
        return plans;
    }

    /// <summary>Tutti i piani di tutti i broker workspace, ritirati compresi.</summary>
    public IEnumerable<(string BrokerCode, TradingPlan Plan)> ReadAllPlans()
        => List().SelectMany(workspace => ReadPlans(workspace.BrokerCode)
            .Select(plan => (workspace.BrokerCode, plan)));

    /// <summary>Scrive la scheda del broker workspace e i suoi piani, ordinati per codice.</summary>
    public void Write(BrokerWorkspace workspace, IEnumerable<TradingPlan> plans)
    {
        var directory = GetPath(workspace.BrokerCode);
        Directory.CreateDirectory(Path.Combine(directory, PlansDirectoryName));
        AtomicFileWriter.WriteAllText(
            Path.Combine(directory, BrokerWorkspace.FileName),
            JsonSerializer.Serialize(workspace, Json));
        AtomicFileWriter.WriteAllText(
            GetPlansFile(workspace.BrokerCode),
            JsonSerializer.Serialize(plans.OrderBy(plan => plan.Code, StringComparer.OrdinalIgnoreCase), Json));
    }

    /// <summary>La cartella del broker workspace, con il controllo che il codice non esca dalla radice.</summary>
    public string GetPath(string brokerCode)
    {
        var code = brokerCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (code.Length == 0 || code.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException($"Codice broker non valido per un broker workspace: '{brokerCode}'.");

        return Path.Combine(_root, code);
    }

    private string GetPlansFile(string brokerCode)
        => Path.Combine(GetPath(brokerCode), PlansDirectoryName, PlansFileName);
}
