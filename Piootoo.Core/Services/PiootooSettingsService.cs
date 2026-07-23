using System.Text.Json;
using Piootoo.Core.Services.Interfaces;
using Piootoo.Domain.Repositories;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Settings;

namespace Piootoo.Core.Services;

/// <summary>
/// Servizio per la gestione dei settings Piootoo
/// </summary>
public class PiootooSettingsService : IPiootooSettingsService
{
    private readonly PiootooSettings _settings;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<PiootooSetup>? _cachedSetups;

    public PiootooSettingsService(PiootooSettings settings)
    {
        _settings = settings;
        _settingsFilePath = Path.Combine(settings.GetSettingsPath(), "PiootooSettings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        
        // Assicura che la directory esista
        var settingsDir = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }
    }

    public List<string> GetAvailableSymbols()
    {
        var dataRepository = new DataSourceRepository(_settings.GetRepositoryPath());
        return dataRepository.GetAvailableSymbols().ToList();
    }

    public List<PiootooSetup> GetAllSetups()
    {
        if (_cachedSetups != null)
            return _cachedSetups;

        if (!File.Exists(_settingsFilePath))
        {
            _cachedSetups = new List<PiootooSetup>();
            return _cachedSetups;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _cachedSetups = new List<PiootooSetup>();
                return _cachedSetups;
            }

            _cachedSetups = JsonSerializer.Deserialize<List<PiootooSetup>>(json, _jsonOptions) ?? new List<PiootooSetup>();
            return _cachedSetups;
        }
        catch
        {
            _cachedSetups = new List<PiootooSetup>();
            return _cachedSetups;
        }
    }

    public PiootooSetup? GetSetupById(string id)
    {
        return GetAllSetups().FirstOrDefault(s => s.Id == id);
    }

    public PiootooSetup CreateSetup(PiootooSetup setup)
    {
        if (string.IsNullOrEmpty(setup.Id))
            setup.Id = Guid.NewGuid().ToString();
        
        setup.CreatedAt = DateTime.UtcNow;
        setup.UpdatedAt = DateTime.UtcNow;

        var setups = GetAllSetups();
        setups.Add(setup);
        SaveSetups(setups);
        
        return setup;
    }

    public PiootooSetup UpdateSetup(PiootooSetup setup)
    {
        var setups = GetAllSetups();
        var existing = setups.FirstOrDefault(s => s.Id == setup.Id);
        
        if (existing == null)
            throw new ArgumentException($"Setup con ID {setup.Id} non trovato");

        existing.Name = setup.Name;
        existing.Code = setup.Code;
        existing.InitialCapital = setup.InitialCapital;
        existing.SelectedSymbols = setup.SelectedSymbols;
        existing.UpdatedAt = DateTime.UtcNow;

        SaveSetups(setups);
        
        return existing;
    }

    public bool DeleteSetup(string id)
    {
        var setups = GetAllSetups();
        var existing = setups.FirstOrDefault(s => s.Id == id);
        
        if (existing == null)
            return false;

        setups.Remove(existing);
        SaveSetups(setups);
        
        return true;
    }

    private void SaveSetups(List<PiootooSetup> setups)
    {
        var json = JsonSerializer.Serialize(setups, _jsonOptions);
        File.WriteAllText(_settingsFilePath, json);
        _cachedSetups = setups; // Aggiorna cache
    }
}
