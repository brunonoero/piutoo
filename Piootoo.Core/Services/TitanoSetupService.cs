using System.Text.Json;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Optimization;

namespace Piootoo.Core.Services;

public class TitanoSetupService
{
    private readonly string _setupsPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public TitanoSetupService(PiootooSettings settings)
    {
        _setupsPath = Path.Combine(settings.GetSettingsPath(), "titano-setups");
        Directory.CreateDirectory(_setupsPath);
    }

    public string GetSetupsPath() => _setupsPath;

    public IReadOnlyList<TitanoSetupInfo> ListSetups()
    {
        if (!Directory.Exists(_setupsPath))
        {
            return Array.Empty<TitanoSetupInfo>();
        }

        return Directory.GetFiles(_setupsPath, "*.json")
            .Select(path =>
            {
                try
                {
                    var setup = LoadSetupFromFile(path);
                    return new TitanoSetupInfo
                    {
                        Id = setup.Id,
                        Name = setup.Name,
                        Description = setup.Description,
                        FileName = Path.GetFileName(path),
                        UpdatedAt = setup.UpdatedAt
                    };
                }
                catch
                {
                    return new TitanoSetupInfo
                    {
                        Id = Path.GetFileNameWithoutExtension(path),
                        Name = Path.GetFileNameWithoutExtension(path),
                        FileName = Path.GetFileName(path)
                    };
                }
            })
            .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public TitanoFilterSetup GetSetup(string setupId)
    {
        var path = ResolveSetupPath(setupId);
        if (path == null || !File.Exists(path))
        {
            throw new FileNotFoundException($"Setup Titano '{setupId}' non trovato.");
        }

        return LoadSetupFromFile(path);
    }

    public TitanoFilterSetup SaveSetup(TitanoFilterSetup setup)
    {
        if (string.IsNullOrWhiteSpace(setup.Id))
        {
            setup.Id = MakeSetupId(setup.Name);
        }

        setup.UpdatedAt = DateTime.UtcNow;
        var fileName = MakeFileName(setup.Id);
        var path = Path.Combine(_setupsPath, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(setup, _jsonOptions));
        return setup;
    }

    public void ApplySetupToRequest(TitanoFilterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SetupId))
        {
            return;
        }

        var setup = GetSetup(request.SetupId);
        request.LookbackWeeks = setup.LookbackWeeks;
        request.Rules = setup.Rules;
        request.TradingRules = setup.TradingRules;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            request.Name = setup.Name;
        }
    }

    private TitanoFilterSetup LoadSetupFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var setup = JsonSerializer.Deserialize<TitanoFilterSetup>(json, _jsonOptions)
            ?? throw new InvalidOperationException($"Setup non valido: {path}");

        if (string.IsNullOrWhiteSpace(setup.Id))
        {
            setup.Id = Path.GetFileNameWithoutExtension(path);
        }

        return setup;
    }

    private string? ResolveSetupPath(string setupId)
    {
        var direct = Path.Combine(_setupsPath, MakeFileName(setupId));
        if (File.Exists(direct))
        {
            return direct;
        }

        return Directory.GetFiles(_setupsPath, "*.json")
            .FirstOrDefault(path =>
            {
                try
                {
                    return string.Equals(LoadSetupFromFile(path).Id, setupId, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return string.Equals(Path.GetFileNameWithoutExtension(path), setupId, StringComparison.OrdinalIgnoreCase);
                }
            });
    }

    private static string MakeFileName(string setupId) =>
        setupId.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? setupId : $"{setupId}.json";

    private static string MakeSetupId(string name)
    {
        var chars = name.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var id = new string(chars).Trim('-');
        while (id.Contains("--", StringComparison.Ordinal))
        {
            id = id.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(id) ? $"setup-{DateTime.UtcNow:yyyyMMddHHmmss}" : id;
    }
}
