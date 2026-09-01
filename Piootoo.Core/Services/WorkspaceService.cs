using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>Gestisce workspace filesystem e il relativo masterfilter.json.</summary>
public sealed class WorkspaceService
{
    /// <summary>
    /// Nome del file di masterfilter dentro la cartella del workspace. Pubblico perché chi lo
    /// rilegge a ogni barra (<see cref="TitanoRotationService"/>) deve poterne guardare il timestamp
    /// senza duplicarne il nome.
    /// </summary>
    public const string MasterFilterFileName = "masterfilter.json";
    private const string AccountsFileName = "accounts.json";

    /// <summary>Registro globale delle tabelle di conversione simboli, fuori da account e workspace.</summary>
    private const string SymbolConversionsFileName = "symbol-conversions.json";

    /// <summary>Nome dell'account neutro: mappatura 1 a 1 sui simboli del catalogo strategie.</summary>
    public const string DefaultAccountName = "Default";

    /// <summary>
    /// Balance iniziale dell'account di default: è il capitale di riferimento delle strategie, ed è
    /// per questo che l'account neutro opera 1 a 1 (<c>BalanceScale = 1</c>).
    /// </summary>
    public const decimal DefaultAccountInitialBalance = TradingConventions.StrategyReferenceBalance;

    private readonly string _rootPath;
    private readonly string _settingsPath;
    private readonly string _accountsPath;
    private readonly object _accountsGate = new();
    private readonly object _symbolConversionsGate = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public WorkspaceService(PiootooSettings settings)
    {
        _rootPath = settings.GetWorkspacesPath();
        var repositoryRoot = Directory.GetParent(Path.GetFullPath(_rootPath))?.FullName
            ?? throw new InvalidOperationException("Il path dei workspace non ha una cartella radice valida.");
        _settingsPath = string.IsNullOrWhiteSpace(settings.GetSettingsPath())
            ? Path.Combine(repositoryRoot, "settings")
            : settings.GetSettingsPath();
        _accountsPath = string.IsNullOrWhiteSpace(settings.GetAccountsPath())
            ? (string.IsNullOrWhiteSpace(settings.BasePath)
                ? Path.Combine(_rootPath, "accounts")
                : Path.Combine(repositoryRoot, "accounts"))
            : settings.GetAccountsPath();
        Directory.CreateDirectory(_rootPath);
        Directory.CreateDirectory(_settingsPath);
        Directory.CreateDirectory(_accountsPath);
    }

    public IReadOnlyList<WorkspaceInfo> List()
        => Directory.EnumerateDirectories(_rootPath)
            .Select(path =>
            {
                var id = Path.GetFileName(path);
                var filter = GetMasterFilter(id);
                return new WorkspaceInfo { Id = id, Name = filter.Name, StrategiesCount = filter.StrategiesFilter.Count };
            })
            .OrderBy(workspace => workspace.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public WorkspaceInfo Create(CreateWorkspaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Il nome del workspace è obbligatorio.");

        var id = ToId(request.Name);
        var path = GetWorkspacePath(id);
        if (Directory.Exists(path))
            throw new InvalidOperationException($"Il workspace '{id}' esiste già.");

        Directory.CreateDirectory(path);
        SaveMasterFilter(id, new WorkspaceMasterFilter
        {
            Name = request.Name.Trim(),
            StrategiesFilter = request.StrategiesFilter.Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList()
        });

        return new WorkspaceInfo { Id = id, Name = request.Name.Trim(), StrategiesCount = request.StrategiesFilter.Count };
    }

    public WorkspaceMasterFilter GetMasterFilter(string workspaceId)
    {
        var file = Path.Combine(GetExistingWorkspacePath(workspaceId), MasterFilterFileName);
        if (!File.Exists(file))
            return new WorkspaceMasterFilter { Name = workspaceId };

        return JsonSerializer.Deserialize<WorkspaceMasterFilter>(File.ReadAllText(file), _jsonOptions)
            ?? new WorkspaceMasterFilter { Name = workspaceId };
    }

    public WorkspaceMasterFilter SaveMasterFilter(string workspaceId, WorkspaceMasterFilter filter)
    {
        var path = GetExistingWorkspacePath(workspaceId);
        filter.Name = string.IsNullOrWhiteSpace(filter.Name) ? workspaceId : filter.Name.Trim();
        filter.StrategiesFilter = filter.StrategiesFilter.Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
        AtomicFileWriter.WriteAllText(
            Path.Combine(path, MasterFilterFileName),
            JsonSerializer.Serialize(filter, _jsonOptions));
        return filter;
    }

    /// <summary>Account globali, condivisi da tutti i workspace e ordinati per nome.</summary>
    public IReadOnlyList<WorkspaceAccount> ListAccounts()
    {
        lock (_accountsGate)
            return ReadGlobalAccountsFile().Accounts
                .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public WorkspaceAccount GetAccount(string accountId)
    {
        lock (_accountsGate)
        {
            var accounts = ReadGlobalAccountsFile().Accounts;
            return FindAccount(accounts, accountId)
                ?? throw new KeyNotFoundException($"Account globale '{accountId}' non trovato.");
        }
    }

    public WorkspaceAccount CreateAccount(WorkspaceAccount account)
    {
        lock (_accountsGate)
        {
            var file = ReadGlobalAccountsFile();

            var normalized = NormalizeAccount(account);
            normalized.Id = ToAccountId(normalized.Name);
            if (FindAccount(file.Accounts, normalized.Id) is not null)
                throw new InvalidOperationException($"L'account globale '{normalized.Id}' esiste già.");

            normalized.CreatedUtc = DateTime.UtcNow;
            normalized.UpdatedUtc = normalized.CreatedUtc;
            file.Accounts.Add(normalized);
            if (normalized.GroupId.Length > 0 &&
                !file.Groups.Contains(normalized.GroupId, StringComparer.OrdinalIgnoreCase))
                file.Groups.Add(normalized.GroupId);
            WriteGlobalAccountsFile(file);
            return normalized;
        }
    }

    /// <summary>Sovrascrive un account esistente, tabella di conversione compresa.</summary>
    public WorkspaceAccount SaveAccount(string accountId, WorkspaceAccount account)
    {
        lock (_accountsGate)
        {
            var file = ReadGlobalAccountsFile();
            var existing = FindAccount(file.Accounts, accountId)
                ?? throw new KeyNotFoundException($"Account globale '{accountId}' non trovato.");

            var normalized = NormalizeAccount(account);
            normalized.Id = existing.Id;
            normalized.CreatedUtc = existing.CreatedUtc == default ? DateTime.UtcNow : existing.CreatedUtc;
            normalized.UpdatedUtc = DateTime.UtcNow;

            file.Accounts[file.Accounts.IndexOf(existing)] = normalized;
            if (normalized.GroupId.Length > 0 &&
                !file.Groups.Contains(normalized.GroupId, StringComparer.OrdinalIgnoreCase))
                file.Groups.Add(normalized.GroupId);
            WriteGlobalAccountsFile(file);
            return normalized;
        }
    }

    public void DeleteAccount(string accountId)
    {
        lock (_accountsGate)
        {
            var file = ReadGlobalAccountsFile();
            var existing = FindAccount(file.Accounts, accountId)
                ?? throw new KeyNotFoundException($"Account globale '{accountId}' non trovato.");

            file.Accounts.Remove(existing);
            WriteGlobalAccountsFile(file);
        }
    }

    public IReadOnlyList<string> ListAccountGroups()
    {
        lock (_accountsGate)
            return NormalizeGroups(ReadGlobalAccountsFile()).ToList();
    }

    public IReadOnlyList<string> AddAccountGroup(string groupId)
    {
        var normalized = NormalizeGroupId(groupId);
        lock (_accountsGate)
        {
            var file = ReadGlobalAccountsFile();
            if (!NormalizeGroups(file).Contains(normalized, StringComparer.OrdinalIgnoreCase))
                file.Groups.Add(normalized);
            WriteGlobalAccountsFile(file);
            return NormalizeGroups(file).ToList();
        }
    }

    public IReadOnlyList<string> RemoveAccountGroup(string groupId)
    {
        var normalized = NormalizeGroupId(groupId);
        lock (_accountsGate)
        {
            var file = ReadGlobalAccountsFile();
            if (file.Accounts.Any(account =>
                    account.GroupId.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    $"Il gruppo '{normalized}' è utilizzato da uno o più account e non può essere eliminato.");

            file.Groups.RemoveAll(group => group.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            WriteGlobalAccountsFile(file);
            return NormalizeGroups(file).ToList();
        }
    }

    /// <summary>Tabelle di conversione simboli globali, condivise da tutti i workspace e ordinate per nome.</summary>
    public IReadOnlyList<SymbolConversion> ListSymbolConversions()
    {
        lock (_symbolConversionsGate)
            return ReadSymbolConversionsFile().Conversions
                .OrderBy(conversion => conversion.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public SymbolConversion GetSymbolConversion(string code)
    {
        lock (_symbolConversionsGate)
            return FindSymbolConversion(ReadSymbolConversionsFile().Conversions, code)
                ?? throw new KeyNotFoundException($"Tabella di conversione '{code}' non trovata.");
    }

    public SymbolConversion CreateSymbolConversion(SymbolConversion conversion)
    {
        lock (_symbolConversionsGate)
        {
            var file = ReadSymbolConversionsFile();
            var normalized = NormalizeSymbolConversion(conversion);
            if (FindSymbolConversion(file.Conversions, normalized.Code) is not null)
                throw new InvalidOperationException($"La tabella di conversione '{normalized.Code}' esiste già.");

            normalized.CreatedUtc = DateTime.UtcNow;
            normalized.UpdatedUtc = normalized.CreatedUtc;
            file.Conversions.Add(normalized);
            WriteSymbolConversionsFile(file);
            return normalized;
        }
    }

    public SymbolConversion SaveSymbolConversion(string code, SymbolConversion conversion)
    {
        lock (_symbolConversionsGate)
        {
            var file = ReadSymbolConversionsFile();
            var existing = FindSymbolConversion(file.Conversions, code)
                ?? throw new KeyNotFoundException($"Tabella di conversione '{code}' non trovata.");

            var normalized = NormalizeSymbolConversion(conversion);
            normalized.Code = existing.Code;
            normalized.CreatedUtc = existing.CreatedUtc == default ? DateTime.UtcNow : existing.CreatedUtc;
            normalized.UpdatedUtc = DateTime.UtcNow;

            file.Conversions[file.Conversions.IndexOf(existing)] = normalized;
            WriteSymbolConversionsFile(file);
            return normalized;
        }
    }

    /// <summary>
    /// Rifiuta la cancellazione se un account la referenzia ancora: altrimenti l'account resterebbe
    /// con un codice orfano, che <see cref="ResolveSymbolConversionMappings"/> tratta come errore.
    /// </summary>
    public void DeleteSymbolConversion(string code)
    {
        lock (_accountsGate)
        lock (_symbolConversionsGate)
        {
            var inUse = ReadGlobalAccountsFile().Accounts.FirstOrDefault(account =>
                account.SymbolConversionCode.Equals(code?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (inUse is not null)
                throw new InvalidOperationException(
                    $"La tabella di conversione '{code}' è usata dall'account '{inUse.Name}' e non può essere eliminata.");

            var file = ReadSymbolConversionsFile();
            var existing = FindSymbolConversion(file.Conversions, code)
                ?? throw new KeyNotFoundException($"Tabella di conversione '{code}' non trovata.");
            file.Conversions.Remove(existing);
            WriteSymbolConversionsFile(file);
        }
    }

    /// <summary>
    /// Tabella risolta dal codice di conversione di un account: vuota se il codice è vuoto (nessuna
    /// conversione, 1 a 1 come nell'account inesistente). Un codice valorizzato ma assente dal
    /// registro è un errore esplicito, non un 1 a 1 silenzioso.
    /// </summary>
    public IReadOnlyList<AccountSymbolMapping> ResolveSymbolConversionMappings(string? code)
        => ResolveSymbolConversion(code).Mappings;

    /// <summary>
    /// La tabella intera, non le sole righe: l'arrotondamento è una proprietà della tabella (del
    /// broker che descrive) e chi converte una quantità ha bisogno di entrambi. Codice vuoto =
    /// tabella vuota con l'arrotondamento di default, cioè nessuna conversione.
    /// </summary>
    public SymbolConversion ResolveSymbolConversion(string? code)
        => string.IsNullOrWhiteSpace(code) ? new SymbolConversion() : GetSymbolConversion(code);

    /// <summary>
    /// Restituisce l'account di default globale creandolo se manca: mappatura identità sui
    /// simboli del catalogo, moltiplicatore 1 e balance iniziale di un milione. È l'account da usare
    /// quando si vuole un backtest senza conversioni.
    /// </summary>
    public WorkspaceAccount EnsureDefaultAccount()
    {
        lock (_accountsGate)
        {
            var file = ReadGlobalAccountsFile();
            var defaultId = ToAccountId(DefaultAccountName);
            var existing = FindAccount(file.Accounts, defaultId);
            if (existing is not null)
                return existing;

            var account = NormalizeAccount(new WorkspaceAccount
            {
            Name = DefaultAccountName,
            AccountNumber = DefaultAccountName,
            GroupId = DefaultAccountName,
            Broker = "Piootoo",
            Currency = "USD",
            InitialBalance = DefaultAccountInitialBalance,
            Enabled = true,
            Notes = "Account neutro: simboli identici a quelli delle strategie e moltiplicatore contratto 1.",
            // Nessun codice di conversione: un codice assente è già 1 a 1, e resta tale
            // indipendentemente da come evolve il registro delle tabelle di conversione.
            SymbolConversionCode = string.Empty
            });
            account.Id = defaultId;
            account.CreatedUtc = DateTime.UtcNow;
            account.UpdatedUtc = account.CreatedUtc;

            file.Accounts.Add(account);
            if (!file.Groups.Contains(account.GroupId, StringComparer.OrdinalIgnoreCase))
                file.Groups.Add(account.GroupId);
            WriteGlobalAccountsFile(file);
            return account;
        }
    }

    /// <summary>
    /// Tabella identità sempre ricalcolata dal catalogo: ogni simbolo su se stesso, moltiplicatore 1.
    /// Punto di partenza comodo per popolare una nuova tabella di conversione nel registro globale.
    /// </summary>
    public IReadOnlyList<AccountSymbolMapping> GetIdentitySymbolMappings() => BuildIdentityMappings();

    /// <summary>Mappatura identità: ogni simbolo del catalogo su se stesso, moltiplicatore 1.</summary>
    private static List<AccountSymbolMapping> BuildIdentityMappings()
        => StrategyFactory.GetRegisteredSymbols()
            .Select(symbol => symbol?.Trim() ?? string.Empty)
            .Where(symbol => symbol.Length > 0)
            .Select(symbol => symbol.StartsWith('@') ? symbol : $"@{symbol}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .Select(symbol => new AccountSymbolMapping
            {
                Symbol = symbol,
                AccountSymbol = symbol,
                ContractMultiplier = 1m,
                PriceScale = 1m,
                Enabled = true
            })
            .ToList();

    private WorkspaceAccountsFile ReadGlobalAccountsFile()
    {
        var file = Path.Combine(_accountsPath, AccountsFileName);
        if (!File.Exists(file))
            MigrateWorkspaceAccounts(file);
        if (!File.Exists(file))
            return new WorkspaceAccountsFile();

        var result = JsonSerializer.Deserialize<WorkspaceAccountsFile>(File.ReadAllText(file), _jsonOptions)
            ?? new WorkspaceAccountsFile();
        result.Groups = NormalizeGroups(result).ToList();
        return result;
    }

    private void WriteGlobalAccountsFile(WorkspaceAccountsFile file)
    {
        file.Groups = NormalizeGroups(file).ToList();
        file.Accounts = file.Accounts
            .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        AtomicFileWriter.WriteAllText(
            Path.Combine(_accountsPath, AccountsFileName),
            JsonSerializer.Serialize(file, _jsonOptions));
    }

    /// <summary>
    /// Importa una sola volta i vecchi account per-workspace nel registro globale. I file originali
    /// restano al loro posto come backup; in caso di collisione prevale il record aggiornato più di recente.
    /// </summary>
    private void MigrateWorkspaceAccounts(string globalFile)
    {
        if (File.Exists(globalFile))
            return;

        var merged = new Dictionary<string, WorkspaceAccount>(StringComparer.OrdinalIgnoreCase);
        foreach (var workspacePath in Directory.EnumerateDirectories(_rootPath).OrderBy(path => path))
        {
            var legacyFile = Path.Combine(workspacePath, AccountsFileName);
            if (!File.Exists(legacyFile))
                continue;

            var legacy = JsonSerializer.Deserialize<WorkspaceAccountsFile>(
                File.ReadAllText(legacyFile), _jsonOptions) ?? new WorkspaceAccountsFile();
            foreach (var account in legacy.Accounts)
            {
                var normalized = NormalizeAccount(account);
                normalized.Id = string.IsNullOrWhiteSpace(account.Id)
                    ? ToAccountId(normalized.Name)
                    : account.Id.Trim();
                if (!merged.TryGetValue(normalized.Id, out var existing) ||
                    normalized.UpdatedUtc > existing.UpdatedUtc)
                    merged[normalized.Id] = normalized;
            }
        }

        if (merged.Count > 0)
            WriteGlobalAccountsFile(new WorkspaceAccountsFile { Accounts = merged.Values.ToList() });
    }

    private static IEnumerable<string> NormalizeGroups(WorkspaceAccountsFile file)
        => (file.Groups ?? new List<string>())
            .Concat(file.Accounts.Select(account => account.GroupId))
            .Select(group => group?.Trim() ?? string.Empty)
            .Where(group => group.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group, StringComparer.OrdinalIgnoreCase);

    private static string NormalizeGroupId(string groupId)
    {
        var normalized = groupId?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException("Il codice gruppo è obbligatorio.");
        return normalized;
    }

    private static WorkspaceAccount? FindAccount(List<WorkspaceAccount> accounts, string accountId)
        => accounts.FirstOrDefault(account =>
            account.Id.Equals(accountId?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static WorkspaceAccount NormalizeAccount(WorkspaceAccount account)
    {
        if (account is null)
            throw new ArgumentException("Account non valido.");
        if (string.IsNullOrWhiteSpace(account.Name))
            throw new ArgumentException("Il nome dell'account è obbligatorio.");
        if (account.InitialBalance < 0)
            throw new ArgumentException("Il balance iniziale non può essere negativo.");

        return new WorkspaceAccount
        {
            Id = account.Id?.Trim() ?? string.Empty,
            Name = account.Name.Trim(),
            AccountNumber = account.AccountNumber?.Trim() ?? string.Empty,
            GroupId = account.GroupId?.Trim() ?? string.Empty,
            Broker = account.Broker?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(account.Currency) ? "USD" : account.Currency.Trim().ToUpperInvariant(),
            InitialBalance = account.InitialBalance,
            Enabled = account.Enabled,
            Notes = account.Notes?.Trim() ?? string.Empty,
            CreatedUtc = account.CreatedUtc,
            UpdatedUtc = account.UpdatedUtc,
            SymbolConversionCode = account.SymbolConversionCode?.Trim() ?? string.Empty
        };
    }

    private static SymbolConversion NormalizeSymbolConversion(SymbolConversion conversion)
    {
        if (conversion is null)
            throw new ArgumentException("Tabella di conversione non valida.");
        if (string.IsNullOrWhiteSpace(conversion.Code))
            throw new ArgumentException("Il codice della tabella di conversione è obbligatorio.");
        if (string.IsNullOrWhiteSpace(conversion.Name))
            throw new ArgumentException("Il nome della tabella di conversione è obbligatorio.");

        var mappings = new List<AccountSymbolMapping>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in conversion.Mappings ?? new List<AccountSymbolMapping>())
        {
            var symbol = mapping.Symbol?.Trim() ?? string.Empty;
            var accountSymbol = mapping.AccountSymbol?.Trim() ?? string.Empty;
            if (symbol.Length == 0 && accountSymbol.Length == 0)
                continue;
            if (symbol.Length == 0)
                throw new ArgumentException("Ogni riga della tabella di conversione deve indicare il simbolo Piootoo.");
            if (accountSymbol.Length == 0)
                throw new ArgumentException($"Il simbolo account per '{symbol}' è obbligatorio.");
            if (mapping.ContractMultiplier <= 0)
                throw new ArgumentException($"Il moltiplicatore contratto per '{symbol}' deve essere maggiore di zero.");
            if (mapping.PriceScale <= 0)
                throw new ArgumentException($"La scala di prezzo per '{symbol}' deve essere maggiore di zero.");
            if (!seen.Add(symbol))
                throw new ArgumentException($"Il simbolo '{symbol}' è presente più volte nella tabella di conversione.");

            mappings.Add(new AccountSymbolMapping
            {
                Symbol = symbol,
                AccountSymbol = accountSymbol,
                ContractMultiplier = mapping.ContractMultiplier,
                PriceScale = mapping.PriceScale,
                MinimumQuantity = mapping.MinimumQuantity,
                QuantityStep = mapping.QuantityStep,
                // RoundingMode non si copia: da qui in poi vive sulla tabella. La proprietà di riga
                // resta null e sparisce dal file al primo salvataggio.
                Enabled = mapping.Enabled
            });
        }

        return new SymbolConversion
        {
            Code = conversion.Code.Trim(),
            Name = conversion.Name.Trim(),
            RoundingMode = ResolveTableRoundingMode(conversion),
            Mappings = mappings.OrderBy(mapping => mapping.Symbol, StringComparer.OrdinalIgnoreCase).ToList(),
            CreatedUtc = conversion.CreatedUtc,
            UpdatedUtc = conversion.UpdatedUtc
        };
    }

    /// <summary>
    /// Arrotondamento della tabella, con migrazione dai file scritti prima del 24/08/2026, dove il
    /// valore stava sulle singole righe.
    ///
    /// <para>Se la tabella non lo dichiara ma le righe sì, vince la <b>maggioranza</b> delle righe:
    /// il caso reale è un file dove tutte le righe portano lo stesso valore, e prendere la prima
    /// darebbe lo stesso risultato — ma su una tabella mista la maggioranza è l'unica scelta che
    /// non dipende dall'ordinamento alfabetico dei simboli. Le righe rimaste in minoranza sono un
    /// dato che si perde: era esattamente l'incoerenza che questa modifica vuole rendere
    /// impossibile.</para>
    /// </summary>
    private static QuantityRoundingMode ResolveTableRoundingMode(SymbolConversion conversion)
    {
        var declared = conversion.Mappings?
            .Where(mapping => mapping.RoundingMode.HasValue)
            .Select(mapping => mapping.RoundingMode!.Value)
            .ToList() ?? new List<QuantityRoundingMode>();

        if (declared.Count == 0)
            return conversion.RoundingMode;

        return declared
            .GroupBy(mode => mode)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => (int)group.Key)
            .First().Key;
    }

    private static SymbolConversion? FindSymbolConversion(List<SymbolConversion> conversions, string code)
        => conversions.FirstOrDefault(conversion =>
            conversion.Code.Equals(code?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase));

    private SymbolConversionsFile ReadSymbolConversionsFile()
    {
        var file = Path.Combine(_accountsPath, SymbolConversionsFileName);
        if (!File.Exists(file))
            return new SymbolConversionsFile();

        var content = JsonSerializer.Deserialize<SymbolConversionsFile>(File.ReadAllText(file), _jsonOptions)
            ?? new SymbolConversionsFile();

        // Migrazione in lettura, non solo in salvataggio: un file mai più riaperto dalla UI deve
        // comunque essere interpretato con l'arrotondamento giusto. Il file su disco resta
        // invariato finché qualcuno non salva quella tabella.
        foreach (var conversion in content.Conversions)
        {
            conversion.RoundingMode = ResolveTableRoundingMode(conversion);
            foreach (var mapping in conversion.Mappings ?? new List<AccountSymbolMapping>())
                mapping.RoundingMode = null;
        }

        return content;
    }

    private void WriteSymbolConversionsFile(SymbolConversionsFile file)
    {
        file.Conversions = file.Conversions
            .OrderBy(conversion => conversion.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        AtomicFileWriter.WriteAllText(
            Path.Combine(_accountsPath, SymbolConversionsFileName),
            JsonSerializer.Serialize(file, _jsonOptions));
    }

    private static string ToAccountId(string value)
    {
        var id = string.Concat(value.Trim().ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) ? character : '-')).Trim('-');
        while (id.Contains("--", StringComparison.Ordinal)) id = id.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Nome account non valido.") : id;
    }

    public string GetWorkspacePath(string workspaceId)
    {
        var id = ToId(workspaceId);
        var path = Path.GetFullPath(Path.Combine(_rootPath, id));
        if (!path.StartsWith(Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Workspace non valido.");
        return path;
    }

    public IReadOnlyList<WorkspaceBacktestInfo> ListBacktests(string workspaceId)
    {
        var workspacePath = GetExistingWorkspacePath(workspaceId);
        var backtestsPath = WorkspaceBacktestPaths.GetBacktestsPath(workspacePath);
        if (!Directory.Exists(backtestsPath))
            return Array.Empty<WorkspaceBacktestInfo>();

        return Directory.EnumerateDirectories(backtestsPath)
            .Select(path =>
            {
                var resultFiles = Directory.EnumerateFiles(path, "backtest_*.json", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ToArray();
                var (startDateUtc, endDateUtc) = ReadBacktestPeriod(resultFiles.FirstOrDefault());
                var origin = ReadBacktestOrigin(path);
                return new WorkspaceBacktestInfo
                {
                    FolderName = Path.GetFileName(path),
                    FullPath = path,
                    LastModifiedUtc = Directory.GetLastWriteTimeUtc(path),
                    ResultsCount = resultFiles.Length,
                    StartDateUtc = startDateUtc,
                    EndDateUtc = endDateUtc,
                    Origin = origin?.Origin ?? BacktestOrigin.Unknown,
                    PlanCode = origin?.PlanCode
                };
            })
            .OrderByDescending(backtest => backtest.LastModifiedUtc)
            .ToList();
    }

    /// <summary>
    /// Scrive il marcatore di origine nella cartella del backtest. Va chiamato da chi crea la
    /// cartella: dedurre l'origine dopo, dai file presenti, darebbe risposte sbagliate sui run
    /// interrotti.
    /// </summary>
    public static void WriteBacktestOrigin(string backtestPath, BacktestOriginInfo origin)
    {
        try
        {
            Directory.CreateDirectory(backtestPath);
            AtomicFileWriter.WriteAllText(
                Path.Combine(backtestPath, BacktestOriginInfo.FileName),
                JsonSerializer.Serialize(origin, BacktestOriginJsonOptions));
        }
        catch (Exception)
        {
            // Il marcatore è informativo: non deve far fallire un backtest o l'apertura di una
            // sessione. Chi legge tratta l'assenza come origine sconosciuta.
        }
    }

    private static readonly JsonSerializerOptions BacktestOriginJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Marcatore di origine della cartella, <c>null</c> se assente o illeggibile (origine ignota).
    /// </summary>
    public static BacktestOriginInfo? ReadBacktestOrigin(string backtestPath)
    {
        try
        {
            var path = Path.Combine(backtestPath, BacktestOriginInfo.FileName);
            return File.Exists(path)
                ? JsonSerializer.Deserialize<BacktestOriginInfo>(File.ReadAllText(path), BacktestOriginJsonOptions)
                : null;
        }
        catch (Exception)
        {
            // Marcatore illeggibile: origine sconosciuta, non un errore di elenco.
            return null;
        }
    }

    /// <summary>
    /// Quanto si legge dalla testa di un file di risultato per trovarne il periodo.
    /// <c>StartDate</c> ed <c>EndDate</c> sono la quarta e la quinta proprietà di
    /// <c>BacktestingResult</c>, quindi stanno nei primi duecento byte: 64 KB sono margine, non
    /// una stima.
    /// </summary>
    private const int BacktestPeriodProbeBytes = 64 * 1024;

    /// <summary>
    /// Legge <c>StartDate</c>/<c>EndDate</c> dalla sola testa del file di risultato.
    ///
    /// Prima qui c'era <c>JsonDocument.Parse(File.ReadAllText(path))</c>: per due date leggeva e
    /// deserializzava il risultato intero, che contiene l'equity ora per ora di ogni strategia ed
    /// è dell'ordine delle decine o centinaia di MB. Su un workspace reale l'elenco dei backtest
    /// arrivava così a leggere centinaia di MB — e ad allocarne il doppio, perché
    /// <c>ReadAllText</c> materializza tutto in una stringa UTF-16 — a ogni chiamata di
    /// <see cref="ListBacktests"/>, cioè a ogni apertura della lista. Era quello il ritardo.
    /// </summary>
    private static (DateTime? StartDateUtc, DateTime? EndDateUtc) ReadBacktestPeriod(string? resultPath)
    {
        if (string.IsNullOrWhiteSpace(resultPath))
            return (null, null);

        try
        {
            using var stream = new FileStream(
                resultPath,
                FileMode.Open,
                FileAccess.Read,
                // Un backtest in corso sta scrivendo in questa cartella: l'elenco non deve
                // contendere il lock con chi produce.
                FileShare.ReadWrite,
                bufferSize: 4096,
                FileOptions.SequentialScan);

            var head = new byte[BacktestPeriodProbeBytes];
            var read = stream.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
            return ReadPeriodFromHead(head.AsSpan(0, read));
        }
        catch (IOException)
        {
            return (null, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (null, null);
        }
    }

    private static (DateTime? StartDateUtc, DateTime? EndDateUtc) ReadPeriodFromHead(ReadOnlySpan<byte> head)
    {
        DateTime? start = null;
        DateTime? end = null;

        // isFinalBlock: false — il buffer taglia il JSON a metà per costruzione, e un token
        // troncato in fondo non è un file corrotto: è la fine della finestra che si è scelta.
        var reader = new Utf8JsonReader(head, isFinalBlock: false, state: default);
        try
        {
            while (reader.Read())
            {
                // Solo le proprietà di primo livello: dentro HourlyResults non c'è niente da
                // cercare, e una proprietà omonima annidata darebbe la data sbagliata.
                if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
                    continue;

                var isStart = reader.ValueTextEquals("StartDate"u8);
                if (!isStart && !reader.ValueTextEquals("EndDate"u8))
                    continue;

                if (!reader.Read())
                    break;

                if (reader.TokenType == JsonTokenType.String && reader.TryGetDateTime(out var parsed))
                {
                    if (isStart)
                        start = parsed.ToUniversalTime();
                    else
                        end = parsed.ToUniversalTime();
                }

                if (start.HasValue && end.HasValue)
                    break;
            }
        }
        catch (JsonException)
        {
            // Testa illeggibile: periodo ignoto, non un errore di elenco.
        }

        return (start, end);
    }

    public string GetBacktestPath(string workspaceId, string folderName)
        => WorkspaceBacktestPaths.ResolveBacktestPath(GetExistingWorkspacePath(workspaceId), folderName);

    /// <summary>
    /// Legge i trade chiusi prodotti dal backtest indicato. Il path resta risolto e validato
    /// lato server: il client riceve soltanto il contenuto di <c>trades.json</c>.
    /// </summary>
    public IReadOnlyList<PersistedTrade> GetBacktestTrades(string workspaceId, string folderName)
    {
        var backtestPath = GetBacktestPath(workspaceId, folderName);
        if (!Directory.Exists(backtestPath))
            throw new DirectoryNotFoundException($"Backtest '{folderName}' non trovato nel workspace '{workspaceId}'.");

        return new TradingJsonStore(backtestPath).ReadTrades()
            .OrderByDescending(trade => trade.ExitTimeUtc)
            .ThenByDescending(trade => trade.TradeId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Legge <c>backtest-summary.json</c> come testo grezzo.
    ///
    /// <para>Non viene deserializzato in un modello: il summary evolve con le diagnostiche, e un
    /// contratto tipizzato qui costringerebbe ad aggiornarlo a ogni campo aggiunto — nel frattempo
    /// il client mostrerebbe un summary incompleto senza accorgersene. Il client lo rende come
    /// albero di proprietà.</para>
    /// </summary>
    public string GetBacktestSummary(string workspaceId, string folderName)
    {
        var backtestPath = GetBacktestPath(workspaceId, folderName);
        if (!Directory.Exists(backtestPath))
            throw new DirectoryNotFoundException($"Backtest '{folderName}' non trovato nel workspace '{workspaceId}'.");

        var summaryPath = Path.Combine(backtestPath, BacktestDiagnosticsSchema.SummaryFileName);
        if (!File.Exists(summaryPath))
            throw new FileNotFoundException($"Il backtest '{folderName}' non ha un {BacktestDiagnosticsSchema.SummaryFileName}.", summaryPath);

        return File.ReadAllText(summaryPath);
    }

    /// <summary>
    /// Impacchetta gli artefatti del run per un confronto: <c>trades.json</c>, il summary e
    /// <c>origin.json</c>, rinominati con lo slug del tipo di run
    /// (<c>trades-interno-futures.json</c>, …). Chi confronta scompatta in una cartella e ha già i
    /// nomi giusti — la convenzione è in <c>piootoo-repository/compare/README.md</c>.
    ///
    /// <para><b>Compatta prima di leggere.</b> Durante il run i trade si accodano al journal
    /// <c>.jsonl</c> affiancato e l'array è indietro: esportarlo senza <c>CompactAll</c> darebbe un
    /// file che sembra completo e non lo è.</para>
    ///
    /// <para><b>Un run che non sa dire su quali prezzi è girato non si esporta.</b> Le cartelle
    /// scritte prima di <c>PriceSource</c> non hanno modo di dichiararlo, e un artefatto senza tipo
    /// nel confronto vale meno di zero: rinominato a mano diventa un'affermazione che nessuno ha
    /// verificato. Meglio un errore parlante.</para>
    ///
    /// <para><c>signals.json</c> resta fuori: nei run di portafoglio arriva a centinaia di
    /// megabyte, e il confronto lavora sui trade. Chi ne ha bisogno se lo prende dalla
    /// cartella.</para>
    /// </summary>
    public CompareExportBundle CreateCompareExport(string workspaceId, string folderName)
    {
        var backtestPath = GetBacktestPath(workspaceId, folderName);
        if (!Directory.Exists(backtestPath))
            throw new DirectoryNotFoundException($"Backtest '{folderName}' non trovato nel workspace '{workspaceId}'.");

        var origin = ReadBacktestOrigin(backtestPath);
        if (origin is null || !origin.IdentifiesRun)
            throw new InvalidOperationException(
                $"Il backtest '{folderName}' non dichiara di che tipo è: " +
                (origin is null
                    ? $"manca {BacktestOriginInfo.FileName}."
                    : $"il marcatore lo descrive solo come '{origin.RunSlug}'.") +
                " È una cartella prodotta prima che il marcatore dichiarasse motore e serie di " +
                "prezzi, e senza quel dato l'artefatto non è confrontabile — un CFD senza il nome " +
                "del broker non identifica una serie di prezzi. Rifai il run, oppure copia i file " +
                "a mano assumendoti il nome che gli dai.");

        var slug = origin.RunSlug;

        new TradingJsonStore(backtestPath).CompactAll();

        var sorgenti = new (string Source, string Exported)[]
        {
            (TradingPersistenceSchema.TradesFileName, $"trades-{slug}.json"),
            (BacktestDiagnosticsSchema.SummaryFileName, $"backtest-summary-{slug}.json"),
            (BacktestOriginInfo.FileName, $"run-{slug}.json")
        };

        using var buffer = new MemoryStream();
        var inclusi = new List<string>();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (source, exported) in sorgenti)
            {
                var path = Path.Combine(backtestPath, source);
                // Il summary manca nei run interrotti e in quelli dell'engine esterno: è
                // un'assenza normale, non un motivo per non esportare i trade.
                if (!File.Exists(path))
                    continue;

                using (var entry = archive.CreateEntry(exported, CompressionLevel.Optimal).Open())
                using (var artefatto = File.OpenRead(path))
                    artefatto.CopyTo(entry);
                inclusi.Add(exported);
            }
        }

        if (inclusi.Count == 0)
            throw new FileNotFoundException(
                $"Il backtest '{folderName}' non contiene nessun artefatto da esportare.", backtestPath);

        return new CompareExportBundle(slug, $"{slug}.zip", inclusi, buffer.ToArray());
    }

    /// <summary>
    /// Percorso del report HTML del backtest, se il run ne ha prodotto uno.
    ///
    /// <para>Il nome del file non è fisso: <c>GenerateStrategyEquityHtmlReport</c> lo costruisce dal
    /// prefisso del run, quindi si cerca per estensione invece di indovinarlo. Se ce n'è più d'uno
    /// — cartella riusata da run successivi — vince il più recente, che è quello che l'utente si
    /// aspetta di vedere aprendo il dettaglio.</para>
    /// </summary>
    /// <exception cref="FileNotFoundException">
    /// La cartella esiste ma non contiene alcun HTML: succede nei run interrotti e in quelli
    /// prodotti dall'engine esterno, che i trade li scrive ma il report no. È un'assenza normale, e
    /// il client la distingue da un errore proprio perché è un 404 e non un 500.
    /// </exception>
    public string GetBacktestHtmlReportPath(string workspaceId, string folderName)
    {
        var backtestPath = GetBacktestPath(workspaceId, folderName);
        if (!Directory.Exists(backtestPath))
            throw new DirectoryNotFoundException($"Backtest '{folderName}' non trovato nel workspace '{workspaceId}'.");

        var report = new DirectoryInfo(backtestPath)
            .EnumerateFiles("*.html", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (report is null)
            throw new FileNotFoundException(
                $"Il backtest '{folderName}' non ha un report HTML.", Path.Combine(backtestPath, "*.html"));

        return report.FullName;
    }

    /// <summary>
    /// Elimina una cartella di backtest con tutto il suo contenuto.
    ///
    /// <para>Comprende <c>titano/&lt;run-id&gt;/</c>: i run calcolati su quel campione spariscono
    /// con esso. Il servizio non lo impedisce — un backtest sbagliato deve poter essere buttato —
    /// ma i piani che referenziano quei run falliranno all'apertura della sessione, non prima. Chi
    /// chiama deve avvisare, ed è il motivo per cui <see cref="ListBacktestTitanoRunIds"/> esiste.</para>
    /// </summary>
    public void DeleteBacktest(string workspaceId, string folderName)
    {
        var backtestPath = GetBacktestPath(workspaceId, folderName);
        if (!Directory.Exists(backtestPath))
            throw new DirectoryNotFoundException($"Backtest '{folderName}' non trovato nel workspace '{workspaceId}'.");

        Directory.Delete(backtestPath, recursive: true);
    }

    /// <summary>Id dei run Titano presenti nel backtest, per avvisare prima di cancellarlo.</summary>
    public IReadOnlyList<string> ListBacktestTitanoRunIds(string workspaceId, string folderName)
    {
        var titanoPath = Path.Combine(GetBacktestPath(workspaceId, folderName), "titano");
        if (!Directory.Exists(titanoPath))
            return [];

        return Directory.EnumerateDirectories(titanoPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void Delete(string workspaceId)
        => Directory.Delete(GetExistingWorkspacePath(workspaceId), recursive: true);

    private string GetExistingWorkspacePath(string workspaceId)
    {
        var path = GetWorkspacePath(workspaceId);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Workspace '{workspaceId}' non trovato.");
        return path;
    }

    private static string ToId(string value)
    {
        var id = string.Concat(value.Trim().ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) ? character : '-')).Trim('-');
        while (id.Contains("--", StringComparison.Ordinal)) id = id.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Nome workspace non valido.") : id;
    }
}
