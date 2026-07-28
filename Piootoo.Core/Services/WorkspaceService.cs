using System.Text.Json;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Workspaces;

namespace Piootoo.Core.Services;

/// <summary>Gestisce workspace filesystem e il relativo masterfilter.json.</summary>
public sealed class WorkspaceService
{
    private const string MasterFilterFileName = "masterfilter.json";
    private const string AccountsFileName = "accounts.json";

    /// <summary>Preset condiviso della tabella di conversione, editabile fuori dal codice.</summary>
    private const string SymbolConversionPresetFileName = "default-symbol-conversion.json";

    /// <summary>Nome dell'account neutro: mappatura 1 a 1 sui simboli del catalogo strategie.</summary>
    public const string DefaultAccountName = "Default";

    /// <summary>Balance iniziale dell'account di default.</summary>
    public const decimal DefaultAccountInitialBalance = 1_000_000m;

    private readonly string _rootPath;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public WorkspaceService(PiootooSettings settings)
    {
        _rootPath = settings.GetWorkspacesPath();
        _settingsPath = settings.GetSettingsPath();
        Directory.CreateDirectory(_rootPath);
        Directory.CreateDirectory(_settingsPath);
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

    /// <summary>Account configurati nel workspace, ordinati per nome.</summary>
    public IReadOnlyList<WorkspaceAccount> ListAccounts(string workspaceId)
        => ReadAccountsFile(GetExistingWorkspacePath(workspaceId)).Accounts
            .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public WorkspaceAccount GetAccount(string workspaceId, string accountId)
    {
        var accounts = ReadAccountsFile(GetExistingWorkspacePath(workspaceId)).Accounts;
        return FindAccount(accounts, accountId)
            ?? throw new KeyNotFoundException($"Account '{accountId}' non trovato nel workspace '{workspaceId}'.");
    }

    public WorkspaceAccount CreateAccount(string workspaceId, WorkspaceAccount account)
    {
        var path = GetExistingWorkspacePath(workspaceId);
        var file = ReadAccountsFile(path);

        var normalized = NormalizeAccount(account);
        normalized.Id = ToAccountId(normalized.Name);
        if (FindAccount(file.Accounts, normalized.Id) is not null)
            throw new InvalidOperationException($"L'account '{normalized.Id}' esiste già nel workspace '{workspaceId}'.");

        normalized.CreatedUtc = DateTime.UtcNow;
        normalized.UpdatedUtc = normalized.CreatedUtc;
        file.Accounts.Add(normalized);
        WriteAccountsFile(path, file);
        return normalized;
    }

    /// <summary>Sovrascrive un account esistente, tabella di conversione compresa.</summary>
    public WorkspaceAccount SaveAccount(string workspaceId, string accountId, WorkspaceAccount account)
    {
        var path = GetExistingWorkspacePath(workspaceId);
        var file = ReadAccountsFile(path);
        var existing = FindAccount(file.Accounts, accountId)
            ?? throw new KeyNotFoundException($"Account '{accountId}' non trovato nel workspace '{workspaceId}'.");

        var normalized = NormalizeAccount(account);
        normalized.Id = existing.Id;
        normalized.CreatedUtc = existing.CreatedUtc == default ? DateTime.UtcNow : existing.CreatedUtc;
        normalized.UpdatedUtc = DateTime.UtcNow;

        file.Accounts[file.Accounts.IndexOf(existing)] = normalized;
        WriteAccountsFile(path, file);
        return normalized;
    }

    public void DeleteAccount(string workspaceId, string accountId)
    {
        var path = GetExistingWorkspacePath(workspaceId);
        var file = ReadAccountsFile(path);
        var existing = FindAccount(file.Accounts, accountId)
            ?? throw new KeyNotFoundException($"Account '{accountId}' non trovato nel workspace '{workspaceId}'.");

        file.Accounts.Remove(existing);
        WriteAccountsFile(path, file);
    }

    /// <summary>
    /// Preset della tabella di conversione, condiviso da tutti i workspace e salvato in
    /// <c>settings/default-symbol-conversion.json</c>. Se il file non esiste viene generato 1 a 1
    /// dai simboli del catalogo strategie, così il preset è subito editabile fuori dal codice.
    /// </summary>
    public IReadOnlyList<AccountSymbolMapping> GetSymbolConversionPreset()
    {
        var file = Path.Combine(_settingsPath, SymbolConversionPresetFileName);
        if (File.Exists(file))
        {
            var stored = JsonSerializer.Deserialize<List<AccountSymbolMapping>>(File.ReadAllText(file), _jsonOptions);
            if (stored is { Count: > 0 })
                return NormalizeMappings(stored);
        }

        var generated = BuildIdentityMappings();
        AtomicFileWriter.WriteAllText(file, JsonSerializer.Serialize(generated, _jsonOptions));
        return generated;
    }

    /// <summary>Sovrascrive il preset condiviso con la tabella passata.</summary>
    public IReadOnlyList<AccountSymbolMapping> SaveSymbolConversionPreset(IReadOnlyList<AccountSymbolMapping> mappings)
    {
        var normalized = NormalizeMappings(mappings?.ToList() ?? new List<AccountSymbolMapping>());
        AtomicFileWriter.WriteAllText(
            Path.Combine(_settingsPath, SymbolConversionPresetFileName),
            JsonSerializer.Serialize(normalized, _jsonOptions));
        return normalized;
    }

    /// <summary>
    /// Restituisce l'account di default del workspace creandolo se manca: mappatura identità sui
    /// simboli del catalogo, moltiplicatore 1 e balance iniziale di un milione. È l'account da usare
    /// quando si vuole un backtest senza conversioni.
    /// </summary>
    public WorkspaceAccount EnsureDefaultAccount(string workspaceId)
    {
        var path = GetExistingWorkspacePath(workspaceId);
        var file = ReadAccountsFile(path);
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
            // Identità e non il preset condiviso: l'account 'Default' deve restare 1 a 1 anche se
            // il preset è stato modificato.
            SymbolMappings = BuildIdentityMappings()
        });
        account.Id = defaultId;
        account.CreatedUtc = DateTime.UtcNow;
        account.UpdatedUtc = account.CreatedUtc;

        file.Accounts.Add(account);
        WriteAccountsFile(path, file);
        return account;
    }

    /// <summary>
    /// Tabella identità sempre ricalcolata dal catalogo: ogni simbolo su se stesso, moltiplicatore 1.
    /// È la base dei nuovi account e resta identità anche se il preset condiviso è stato modificato,
    /// così un account appena creato non converte niente finché non lo si decide.
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
                Enabled = true
            })
            .ToList();

    private static List<AccountSymbolMapping> NormalizeMappings(List<AccountSymbolMapping> mappings)
        => NormalizeAccount(new WorkspaceAccount { Name = "preset", SymbolMappings = mappings }).SymbolMappings;

    private WorkspaceAccountsFile ReadAccountsFile(string workspacePath)
    {
        var file = Path.Combine(workspacePath, AccountsFileName);
        if (!File.Exists(file))
            return new WorkspaceAccountsFile();

        return JsonSerializer.Deserialize<WorkspaceAccountsFile>(File.ReadAllText(file), _jsonOptions)
            ?? new WorkspaceAccountsFile();
    }

    private void WriteAccountsFile(string workspacePath, WorkspaceAccountsFile file)
    {
        file.Accounts = file.Accounts
            .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        AtomicFileWriter.WriteAllText(
            Path.Combine(workspacePath, AccountsFileName),
            JsonSerializer.Serialize(file, _jsonOptions));
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

        var mappings = new List<AccountSymbolMapping>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in account.SymbolMappings ?? new List<AccountSymbolMapping>())
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
            if (!seen.Add(symbol))
                throw new ArgumentException($"Il simbolo '{symbol}' è presente più volte nella tabella di conversione.");

            mappings.Add(new AccountSymbolMapping
            {
                Symbol = symbol,
                AccountSymbol = accountSymbol,
                ContractMultiplier = mapping.ContractMultiplier,
                Enabled = mapping.Enabled
            });
        }

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
            SymbolMappings = mappings.OrderBy(mapping => mapping.Symbol, StringComparer.OrdinalIgnoreCase).ToList()
        };
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
                return new WorkspaceBacktestInfo
                {
                    FolderName = Path.GetFileName(path),
                    FullPath = path,
                    LastModifiedUtc = Directory.GetLastWriteTimeUtc(path),
                    ResultsCount = resultFiles.Length,
                    StartDateUtc = startDateUtc,
                    EndDateUtc = endDateUtc
                };
            })
            .OrderByDescending(backtest => backtest.LastModifiedUtc)
            .ToList();
    }

    private static (DateTime? StartDateUtc, DateTime? EndDateUtc) ReadBacktestPeriod(string? resultPath)
    {
        if (string.IsNullOrWhiteSpace(resultPath))
            return (null, null);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(resultPath));
            var root = document.RootElement;
            var start = root.TryGetProperty("StartDate", out var startValue) &&
                        startValue.TryGetDateTime(out var parsedStart)
                ? parsedStart.ToUniversalTime()
                : (DateTime?)null;
            var end = root.TryGetProperty("EndDate", out var endValue) &&
                      endValue.TryGetDateTime(out var parsedEnd)
                ? parsedEnd.ToUniversalTime()
                : (DateTime?)null;
            return (start, end);
        }
        catch (JsonException)
        {
            return (null, null);
        }
        catch (IOException)
        {
            return (null, null);
        }
    }

    public string GetBacktestPath(string workspaceId, string folderName)
        => WorkspaceBacktestPaths.ResolveBacktestPath(GetExistingWorkspacePath(workspaceId), folderName);

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
