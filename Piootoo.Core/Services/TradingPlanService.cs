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
    /// <para>Per la stessa ragione <see cref="TradingPlan.DisabledStrategies"/> <b>non</b> si
    /// applica qui: spegnere una strategia nel piano è una decisione operativa reversibile, e
    /// riaccenderla non deve trovare un buco nel feed lungo quanto lo spegnimento.</para>
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

        return AccountSymbolConversion.FromAccount(account, _workspaces.ResolveConversionForAccount(account));
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
        var accounts = NormalizeAndValidateAccounts(request);
        var code = NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Il nome del piano è obbligatorio.");
        if (request.CommissionPerContract < 0)
            throw new ArgumentException("CommissionPerContract non può essere negativa.");

        // Sotto 0,1 non c'è un moltiplicatore piccolo, c'è uno spegnimento silenzioso: le size
        // arrotondate alla granularità del broker cadrebbero tutte sotto il minimo e la sessione
        // girerebbe senza produrre un ordine, con l'aria di funzionare.
        var sizeMultiplier = NormalizeSizeMultiplier(request.SizeMultiplier);
        if (sizeMultiplier < MinimumSizeMultiplier)
            throw new ArgumentException(
                $"SizeMultiplier deve essere almeno {MinimumSizeMultiplier:0.###}: " +
                $"'{request.SizeMultiplier:0.####}' azzererebbe le size invece di ridurle.");

        if (request.MaxConcurrentTrades < 0)
            throw new ArgumentException("MaxConcurrentTrades non può essere negativo.");

        var brokerCode = ValidateBrokerAndAccounts(request.BrokerCode, accounts);

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
                BrokerCode = brokerCode,
                Accounts = accounts,
                AccountNumber = accounts[0],
                MaxConcurrentTrades = request.MaxConcurrentTrades,
                ConcurrencyCountMode = request.ConcurrencyCountMode,
                EnforceConcurrencyLimits = request.EnforceConcurrencyLimits,
                CommissionPerContract = request.CommissionPerContract,
                Holding = holding,
                SizeMultiplier = sizeMultiplier,
                DisabledStrategies = NormalizeDisabledStrategies(request.DisabledStrategies),
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
    /// Verifica che il broker esista e che ogni conto del piano sia suo, e restituisce il codice
    /// normalizzato.
    ///
    /// <para>Un piano senza broker resta valido: sono i piani scritti prima dell'anagrafica, che
    /// continuano a operare con la tabella dichiarata sui conti. Ma un broker <b>dichiarato</b>
    /// dev'essere vero e i conti devono essere i suoi: mescolare due broker in un piano significa
    /// eseguire lo stesso segnale su due serie di prezzi diverse, e il risultato non corrisponde a
    /// nessuno dei due conti.</para>
    /// </summary>
    private string ValidateBrokerAndAccounts(string? brokerCode, IReadOnlyList<string> accounts)
    {
        var normalized = brokerCode?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            return string.Empty;

        var broker = _workspaces.FindBroker(normalized)
            ?? throw new ArgumentException(
                $"Il broker '{normalized}' non è in anagrafica: creane la scheda prima di usarlo in un piano.");

        var registry = _workspaces.ListAccounts();
        foreach (var number in accounts)
        {
            var account = registry.FirstOrDefault(candidate => string.Equals(
                candidate.AccountNumber?.Trim(), number, StringComparison.OrdinalIgnoreCase));
            if (account is null)
                throw new ArgumentException(
                    $"Il conto '{number}' non è nel registro conti: senza anagrafica non si può " +
                    "risolvere né il capitale né la tabella dei simboli.");

            // Un conto senza broker dichiarato non blocca il piano: e' l'anagrafica non ancora
            // migrata, e il piano gli assegna di fatto il proprio. Un conto di un ALTRO broker si',
            // perche' quello e' un errore di configurazione, non un file vecchio.
            var suo = account.BrokerCode?.Trim() ?? string.Empty;
            if (suo.Length > 0 && !suo.Equals(broker.Code, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Il conto '{number}' è del broker '{suo}', ma il piano dichiara " +
                    $"'{broker.Code}'. Un piano opera su un broker solo: due broker non quotano la " +
                    "stessa serie di barre, e un run che li mescola non corrisponde a nessun conto.");
        }

        return broker.Code;
    }

    /// <summary>
    /// I conti del piano come vanno scritti: senza vuoti, senza doppioni, nell'ordine dichiarato —
    /// il primo è quello che <c>OpenFromPlan</c> usa quando il cBot non ne indica uno.
    ///
    /// <para>Accetta <see cref="SaveTradingPlanRequest.Accounts"/> oppure il singolo
    /// <see cref="SaveTradingPlanRequest.AccountNumber"/>: un piano a un conto solo non deve
    /// costruire una lista per dirlo.</para>
    /// </summary>
    public static IReadOnlyList<string> NormalizeAndValidateAccounts(SaveTradingPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var accounts = (request.Accounts.Count > 0
                ? request.Accounts
                : string.IsNullOrWhiteSpace(request.AccountNumber) ? [] : new[] { request.AccountNumber })
            .Select(account => account?.Trim() ?? string.Empty)
            .Where(account => account.Length > 0)
            .ToList();

        if (accounts.Count == 0)
            throw new ArgumentException("Il piano richiede almeno un conto.");

        var duplicated = accounts.GroupBy(account => account, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicated != null)
            throw new ArgumentException($"Conto '{duplicated.Key}' configurato più di una volta nel piano.");

        return accounts;
    }

    /// <summary>
    /// Il piano come lo vede il resto del sistema, qualunque sia la forma con cui sta su disco.
    ///
    /// <para><b>Migrazione dai gruppi.</b> Un file scritto quando il piano era una lista di righe
    /// gruppo/account diventa la lista dei suoi conti, nell'ordine in cui stavano nel file; il tetto
    /// di concorrenza e la modalità di conteggio sono quelli della <b>prima riga</b>. Dove le righe
    /// dichiaravano tetti diversi la differenza si perde: il massimo allargherebbe in silenzio un
    /// limite che una prop impone, ed è l'unico dei due errori che può costare un conto.</para>
    ///
    /// <para>Il gruppo non aveva altro effetto operativo: diceva che un segnale è consumato una
    /// volta sola per gruppo, e ora lo è una volta sola per conto. Nei piani reali ogni gruppo
    /// conteneva un conto solo, quindi la migrazione non cambia chi riceve cosa.</para>
    /// </summary>
    private static TradingPlan NormalizeLoadedPlan(TradingPlan plan)
    {
        var legacy = plan.Groups ?? [];
        var accounts = plan.Accounts.Count > 0
            ? plan.Accounts.Select(account => account.Trim()).Where(account => account.Length > 0).ToList()
            : legacy.Count > 0
                ? legacy.Select(row => row.AccountNumber?.Trim() ?? string.Empty)
                    .Where(account => account.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : string.IsNullOrWhiteSpace(plan.AccountNumber)
                    ? []
                    : [plan.AccountNumber.Trim()];

        if (accounts.Count == 0)
            return plan;

        var primaryLegacy = legacy.Count > 0 ? legacy[0] : null;
        return new TradingPlan
        {
            WorkspaceId = plan.WorkspaceId,
            Code = plan.Code,
            Name = plan.Name,
            BrokerCode = plan.BrokerCode?.Trim() ?? string.Empty,
            Accounts = accounts,
            AccountNumber = accounts[0],
            MaxConcurrentTrades = primaryLegacy?.MaxConcurrentTrades ?? plan.MaxConcurrentTrades,
            ConcurrencyCountMode = primaryLegacy?.ConcurrencyCountMode ?? plan.ConcurrencyCountMode,
            EnforceConcurrencyLimits = plan.EnforceConcurrencyLimits,
            CommissionPerContract = plan.CommissionPerContract,
            Holding = ResolveLoadedHolding(plan),
            SizeMultiplier = NormalizeSizeMultiplier(plan.SizeMultiplier),
            DisabledStrategies = NormalizeDisabledStrategies(plan.DisabledStrategies),
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
    /// Gli Id spenti come vanno scritti nel file: senza vuoti, senza doppioni, in ordine.
    ///
    /// <para><b>Non si validano contro il catalogo né contro il masterfilter.</b> Un piano puo'
    /// legittimamente tenere spenta una strategia che oggi il masterfilter non contiene: se domani
    /// vi rientra deve ritrovarsi spenta, non riaccesa di nascosto perche' nel frattempo l'Id era
    /// diventato inutile. Scartarlo qui sarebbe una modifica silenziosa del piano fatta da un
    /// salvataggio che l'utente credeva innocuo.</para>
    /// </summary>
    public static IReadOnlyList<string> NormalizeDisabledStrategies(IEnumerable<string>? ids) =>
        (ids ?? [])
        .Select(id => id?.Trim() ?? string.Empty)
        .Where(id => id.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>Valore minimo del moltiplicatore di size di un piano.</summary>
    public const decimal MinimumSizeMultiplier = 0.1m;

    /// <summary>
    /// Un moltiplicatore non valorizzato vale 1, non 0.
    ///
    /// <para>Serve ai <c>plans.json</c> scritti prima che il campo esistesse, e ai client che non
    /// lo conoscono: entrambi lo presentano a <c>0</c>, e senza questa riga il primo avvio dopo
    /// l'aggiornamento azzererebbe la size di ogni piano già configurato senza dire niente a
    /// nessuno. "Non valorizzato" comprende anche il negativo: è la stessa cosa detta peggio.</para>
    ///
    /// <para>Un valore <i>positivo</i> ma sotto il minimo non passa di qui: quello è voluto, e
    /// <see cref="Save"/> lo rifiuta invece di correggerlo di nascosto.</para>
    /// </summary>
    public static decimal NormalizeSizeMultiplier(decimal value) => value <= 0m ? 1m : value;

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
