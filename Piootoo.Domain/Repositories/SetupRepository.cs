using System.Text.Json;
using Piootoo.Shared.Models.Optimization;

namespace Piootoo.Domain.Repositories;

/// <summary>
/// Repository per la gestione dei setup di ottimizzazione
/// </summary>
public class SetupRepository
{
    private readonly string _settingsPath;
    private readonly string _setupsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private SetupsFile? _cache;

    public SetupRepository(string settingsPath)
    {
        _settingsPath = settingsPath;
        _setupsFilePath = Path.Combine(settingsPath, "setups.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_settingsPath))
        {
            Directory.CreateDirectory(_settingsPath);
        }
    }

    /// <summary>
    /// Carica tutti i setup dal file
    /// </summary>
    public async Task<SetupsFile> LoadAsync()
    {
        if (_cache != null)
            return _cache;

        if (!File.Exists(_setupsFilePath))
        {
            _cache = new SetupsFile();
            return _cache;
        }

        var json = await File.ReadAllTextAsync(_setupsFilePath);
        _cache = JsonSerializer.Deserialize<SetupsFile>(json, _jsonOptions) ?? new SetupsFile();
        return _cache;
    }

    /// <summary>
    /// Salva tutti i setup nel file
    /// </summary>
    public async Task SaveAsync(SetupsFile setupsFile)
    {
        setupsFile.LastUpdated = DateTime.Now;
        var json = JsonSerializer.Serialize(setupsFile, _jsonOptions);
        await File.WriteAllTextAsync(_setupsFilePath, json);
        _cache = setupsFile;
    }

    /// <summary>
    /// Ottiene tutti i setup
    /// </summary>
    public async Task<List<SavedSetup>> GetAllAsync()
    {
        var file = await LoadAsync();
        return file.Setups;
    }

    /// <summary>
    /// Ottiene un setup per ID
    /// </summary>
    public async Task<SavedSetup?> GetByIdAsync(string id)
    {
        var file = await LoadAsync();
        return file.Setups.FirstOrDefault(s => s.Id == id);
    }

    /// <summary>
    /// Ottiene un setup per nome
    /// </summary>
    public async Task<SavedSetup?> GetByNameAsync(string name)
    {
        var file = await LoadAsync();
        return file.Setups.FirstOrDefault(s => 
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Cerca setup per criteri
    /// </summary>
    public async Task<List<SavedSetup>> SearchAsync(SetupSearchCriteria criteria)
    {
        var file = await LoadAsync();
        var query = file.Setups.AsEnumerable();

        if (!string.IsNullOrEmpty(criteria.Name))
            query = query.Where(s => s.Name.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase));

        if (criteria.Status.HasValue)
            query = query.Where(s => s.Status == criteria.Status.Value);

        if (criteria.Symbols?.Any() == true)
            query = query.Where(s => s.Symbols.Select(sym => sym.Symbol).Intersect(criteria.Symbols).Any());

        if (criteria.Tags?.Any() == true)
            query = query.Where(s => s.Tags.Intersect(criteria.Tags).Any());

        if (criteria.IsActive.HasValue)
            query = query.Where(s => s.IsActive == criteria.IsActive.Value);

        if (criteria.MinScore.HasValue)
            query = query.Where(s => s.FinalScore >= criteria.MinScore.Value);

        if (criteria.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= criteria.FromDate.Value);

        if (criteria.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= criteria.ToDate.Value);

        return query.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    /// <summary>
    /// Crea un nuovo setup
    /// </summary>
    public async Task<SavedSetup> CreateAsync(SavedSetup setup)
    {
        var file = await LoadAsync();
        
        // Verifica nome univoco
        if (file.Setups.Any(s => s.Name.Equals(setup.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Esiste già un setup con il nome '{setup.Name}'");
        }

        setup.Id = Guid.NewGuid().ToString();
        setup.CreatedAt = DateTime.Now;
        setup.UpdatedAt = DateTime.Now;

        file.Setups.Add(setup);
        await SaveAsync(file);

        return setup;
    }

    /// <summary>
    /// Aggiorna un setup esistente
    /// </summary>
    public async Task<SavedSetup> UpdateAsync(SavedSetup setup)
    {
        var file = await LoadAsync();
        var index = file.Setups.FindIndex(s => s.Id == setup.Id);

        if (index < 0)
            throw new KeyNotFoundException($"Setup con ID '{setup.Id}' non trovato");

        setup.UpdatedAt = DateTime.Now;
        file.Setups[index] = setup;
        await SaveAsync(file);

        return setup;
    }

    /// <summary>
    /// Elimina un setup
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var file = await LoadAsync();
        var setup = file.Setups.FirstOrDefault(s => s.Id == id);

        if (setup == null)
            return false;

        file.Setups.Remove(setup);
        await SaveAsync(file);

        return true;
    }

    /// <summary>
    /// Aggiunge un run di ottimizzazione allo storico
    /// </summary>
    public async Task AddOptimizationRunAsync(string setupId, OptimizationRun run)
    {
        var setup = await GetByIdAsync(setupId);
        if (setup == null)
            throw new KeyNotFoundException($"Setup con ID '{setupId}' non trovato");

        setup.OptimizationHistory.Add(run);
        setup.UpdatedAt = DateTime.Now;

        // Aggiorna anche i risultati principali se è il miglior run
        if (run.Score > setup.FinalScore)
        {
            setup.FinalScore = run.Score;
            setup.Metrics = run.Metrics;
            setup.OptimalConfig = run.Config;
        }

        await UpdateAsync(setup);
    }

    /// <summary>
    /// Attiva/disattiva un setup
    /// </summary>
    public async Task<SavedSetup> SetActiveAsync(string id, bool isActive)
    {
        var setup = await GetByIdAsync(id);
        if (setup == null)
            throw new KeyNotFoundException($"Setup con ID '{id}' non trovato");

        setup.IsActive = isActive;
        setup.Status = isActive ? SetupStatus.Active : SetupStatus.Paused;
        setup.UpdatedAt = DateTime.Now;

        return await UpdateAsync(setup);
    }

    /// <summary>
    /// Esporta un setup in JSON
    /// </summary>
    public async Task<string> ExportAsync(string id)
    {
        var setup = await GetByIdAsync(id);
        if (setup == null)
            throw new KeyNotFoundException($"Setup con ID '{id}' non trovato");

        return JsonSerializer.Serialize(setup, _jsonOptions);
    }

    /// <summary>
    /// Importa un setup da JSON
    /// </summary>
    public async Task<SavedSetup> ImportAsync(string json)
    {
        var setup = JsonSerializer.Deserialize<SavedSetup>(json, _jsonOptions);
        if (setup == null)
            throw new InvalidOperationException("JSON non valido");

        // Genera nuovo ID per evitare conflitti
        setup.Id = Guid.NewGuid().ToString();
        setup.CreatedAt = DateTime.Now;
        setup.UpdatedAt = DateTime.Now;

        return await CreateAsync(setup);
    }

    /// <summary>
    /// Invalida la cache
    /// </summary>
    public void InvalidateCache()
    {
        _cache = null;
    }
}

/// <summary>
/// Criteri di ricerca per i setup
/// </summary>
public class SetupSearchCriteria
{
    public string? Name { get; set; }
    public SetupStatus? Status { get; set; }
    public List<string>? Symbols { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsActive { get; set; }
    public decimal? MinScore { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
