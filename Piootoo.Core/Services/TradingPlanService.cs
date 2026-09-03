using System.Text.Json;
using Piootoo.Shared.Models.Datafeed;
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

    /// <summary>
    /// Gli strumenti che il piano tocca, per un raccoglitore di datafeed: coppie (simbolo,
    /// timeframe) del masterfilter, più il nome che quel simbolo ha sul conto indicato.
    ///
    /// <para><b>Dal masterfilter.</b> Le strategie attive possono cambiare nel tempo, ma il
    /// datafeed di uno strumento serve <i>sempre</i>: anche mentre è spento, perché quando torna
    /// attivo la sua storia deve esserci già. Seguendo le strategie accese il feed si
    /// interromperebbe a ogni pausa e lascerebbe un buco lungo esattamente quanto la pausa — e
    /// nessuno lo scoprirebbe fino al primo backtest su quel periodo.</para>
    ///
    /// <para>È una lettura pura: non apre sessioni, non tocca stato. Un raccoglitore non deve
    /// avere effetti collaterali sull'operatività.</para>
    /// </summary>
    /// <param name="accountNumber">
    /// Conto di cui usare la tabella di conversione simboli. Vuoto = quello dichiarato dal piano.
    /// </param>
    public PlanDatafeedInstrumentsDto ResolveDatafeedInstruments(string code, string? accountNumber)
    {
        var plan = Resolve(code);

        var filter = _workspaces.GetMasterFilter(plan.WorkspaceId);
        if (filter.StrategiesFilter.Count == 0)
            throw new InvalidOperationException(
                $"Il masterfilter del workspace '{plan.WorkspaceId}' è vuoto: il piano '{plan.Code}' " +
                "non tocca alcuno strumento.");

        var byId = StrategyFactory.GetRegisteredStrategies()
            .ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);

        // Un id non eseguibile qui NON è fatale, a differenza dell'apertura di una sessione: là
        // significherebbe operare con meno strategie di quante il piano ne dichiara, qui al
        // massimo si raccoglie un simbolo in meno — e fermare la raccolta di venti strumenti per
        // una voce sbagliata del masterfilter è un prezzo che non vale la pena pagare.
        var pairs = filter.StrategiesFilter
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Symbol) && definition.TimeframeMinutes > 0)
            .ToArray();

        if (pairs.Length == 0)
            throw new InvalidOperationException(
                $"Nessuna strategia eseguibile nel masterfilter del piano '{plan.Code}'.");

        var account = string.IsNullOrWhiteSpace(accountNumber) ? plan.AccountNumber : accountNumber.Trim();
        var conversion = ResolveConversion(account);

        return new PlanDatafeedInstrumentsDto
        {
            PlanCode = plan.Code,
            PlanName = plan.Name,
            WorkspaceId = plan.WorkspaceId,
            AccountNumber = account ?? string.Empty,
            Instruments = pairs
                .GroupBy(definition => NormalizeSymbol(definition.Symbol), StringComparer.OrdinalIgnoreCase)
                .Select(group => new PlanDatafeedInstrumentDto
                {
                    Symbol = group.Key,
                    AccountSymbol = conversion?.GetAccountSymbol(group.Key) ?? group.Key,
                    TimeframesMinutes = group.Select(definition => definition.TimeframeMinutes)
                        .Distinct().Order().ToList()
                })
                .OrderBy(instrument => instrument.Symbol, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    /// <summary>
    /// Tabella di conversione del conto, o null se il conto non è in anagrafica. Null e non
    /// eccezione: senza tabella il raccoglitore usa il simbolo Piootoo così com'è, che è il
    /// comportamento giusto quando conto e broker chiamano lo strumento allo stesso modo. Aprire
    /// una sessione invece pretende l'anagrafica, perché lì serve anche il capitale.
    /// </summary>
    private AccountSymbolConversion? ResolveConversion(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return null;

        var account = _workspaces.ListAccounts().FirstOrDefault(candidate =>
            string.Equals(candidate.AccountNumber?.Trim(), accountNumber.Trim(), StringComparison.OrdinalIgnoreCase));
        if (account is null)
            return null;

        return AccountSymbolConversion.FromAccount(account, _workspaces.ResolveSymbolConversion(account.SymbolConversionCode));
    }

    /// <summary>Simbolo nella forma con cui il datafeed lo indicizza: <c>@NQ</c>.</summary>
    private static string NormalizeSymbol(string symbol)
        => "@" + symbol.Trim().TrimStart('@').ToUpperInvariant();

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
        if (request.CommissionPerContract < 0)
            throw new ArgumentException("CommissionPerContract non può essere negativa.");

        // Mirror della prima riga, così i piani multi-gruppo restano leggibili anche dai client
        // che non conoscono ancora Groups.
        var primary = SelectPrimaryRow(groups);

        // Una policy incoerente (overweek senza overnight, HHMM fuori scala) va rifiutata qui: se
        // passa, il primo a scoprirla e' il cBot in produzione a mercato aperto.
        var holding = request.Holding ?? AccountHoldingPolicy.Default;
        holding.Validate();

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
                ConcurrencyCountMode = primary.ConcurrencyCountMode,
                EnforceConcurrencyLimits = request.EnforceConcurrencyLimits,
                CommissionPerContract = request.CommissionPerContract,
                Holding = holding,
                PositionSizing = request.PositionSizing,
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
    /// valida che gli account siano univoci.
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
        }

        var duplicatedAccount = groups.GroupBy(r => r.AccountNumber, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicatedAccount != null)
            throw new ArgumentException($"Account '{duplicatedAccount.Key}' configurato più di una volta nel piano.");

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
                ConcurrencyCountMode = request.ConcurrencyCountMode
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
                        ConcurrencyCountMode = plan.ConcurrencyCountMode
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
            ConcurrencyCountMode = primary.ConcurrencyCountMode,
            EnforceConcurrencyLimits = plan.EnforceConcurrencyLimits,
            CommissionPerContract = plan.CommissionPerContract,
            Holding = ResolveLoadedHolding(plan),
            PositionSizing = plan.PositionSizing,
            CreatedUtc = plan.CreatedUtc,
            UpdatedUtc = plan.UpdatedUtc
        };
    }

    /// <summary>
    /// La policy di tenuta di un piano letto da disco, travasando il vecchio <c>WeekEndFlat</c> di
    /// primo livello quando il file e' anteriore a <see cref="TradingPlan.Holding"/>.
    ///
    /// <para>I piani gia' scritti non dichiaravano alcun permesso e venivano eseguiti con il flat
    /// del fine settimana sempre acceso: <see cref="AccountHoldingPolicy.Default"/> riproduce
    /// esattamente quel comportamento, quindi la migrazione non cambia un solo trade.</para>
    /// </summary>
    private static AccountHoldingPolicy ResolveLoadedHolding(TradingPlan plan)
    {
        // La presenza del vecchio campo basta a riconoscere il file legacy: da qui in avanti non
        // viene piu' scritto, quindi non puo' convivere con una policy dichiarata.
        var holding = plan.Holding;
        return plan.WeekEndFlat is { } legacy ? holding with { WeekEnd = legacy } : holding;
    }

    /// <summary>
    /// La prima riga del piano: alimenta i campi mirror legacy e i default di <c>OpenFromPlan</c>.
    /// </summary>
    public static TradingGroupRow SelectPrimaryRow(IReadOnlyList<TradingGroupRow> groups) => groups[0];

    private static TradingGroupRow CloneRow(TradingGroupRow row) => new()
    {
        GroupId = row.GroupId.Trim(),
        AccountNumber = row.AccountNumber.Trim(),
        MaxConcurrentTrades = row.MaxConcurrentTrades,
        ConcurrencyCountMode = row.ConcurrencyCountMode
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
