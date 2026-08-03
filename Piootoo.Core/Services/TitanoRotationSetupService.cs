using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Models.Optimization;

namespace Piootoo.Core.Services;

public sealed class TitanoRotationSetupService
{
    private readonly string _setupsPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public TitanoRotationSetupService(PiootooSettings settings)
    {
        _setupsPath = Path.Combine(settings.GetSettingsPath(), "titano-rotation-setups");
        Directory.CreateDirectory(_setupsPath);
        SeedProfessionalSetups();
    }

    public IReadOnlyList<TitanoSetupInfo> ListSetups() =>
        Directory.EnumerateFiles(_setupsPath, "*.json")
            .Select(Load)
            .Select(setup => new TitanoSetupInfo
            {
                Id = setup.Id,
                Name = setup.Name,
                Description = setup.Description,
                FileName = $"{setup.Id}.json",
                UpdatedAt = setup.UpdatedAt
            })
            .OrderBy(setup => setup.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public TitanoRotationSetup GetSetup(string setupId)
    {
        var path = Path.Combine(_setupsPath, $"{SafeId(setupId)}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Setup rotazione Titano '{setupId}' non trovato.");
        return Load(path);
    }

    public TitanoRotationSetup SaveSetup(TitanoRotationSetup setup)
    {
        if (string.IsNullOrWhiteSpace(setup.Name))
            throw new ArgumentException("Il nome del setup è obbligatorio.");

        setup.Id = string.IsNullOrWhiteSpace(setup.Id) ? SafeId(setup.Name) : SafeId(setup.Id);
        setup.UpdatedAt = DateTime.UtcNow;
        Validate(setup);
        AtomicFileWriter.WriteAllText(
            Path.Combine(_setupsPath, $"{setup.Id}.json"),
            JsonSerializer.Serialize(setup, _jsonOptions));
        return setup;
    }

    /// <summary>
    /// Elimina un setup salvato.
    ///
    /// <para>I tre setup predefiniti non sono eliminabili: il costruttore li ricrea a ogni avvio del
    /// servizio, quindi cancellarli darebbe una eliminazione riuscita e un setup che riappare al
    /// riavvio. Meglio rifiutare che mentire.</para>
    ///
    /// <para>Un setup eliminato non invalida i run gia' calcolati: il run porta i propri parametri
    /// nel manifest, e il riferimento nel piano e' solo tracciamento della ricetta usata.</para>
    /// </summary>
    public void DeleteSetup(string setupId)
    {
        var id = SafeId(setupId);
        if (SeededSetupIds.Contains(id))
        {
            throw new InvalidOperationException(
                $"Il setup '{id}' e' predefinito e viene ricreato a ogni avvio del server: " +
                "non e' eliminabile. Creane uno nuovo se ti serve una variante.");
        }

        var path = Path.Combine(_setupsPath, $"{id}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Setup rotazione Titano '{setupId}' non trovato.");

        File.Delete(path);
    }

    private static readonly HashSet<string> SeededSetupIds =
        new(StringComparer.OrdinalIgnoreCase) { "conservativo", "bilanciato", "dinamico" };

    private TitanoRotationSetup Load(string path) =>
        JsonSerializer.Deserialize<TitanoRotationSetup>(File.ReadAllText(path), _jsonOptions)
        ?? throw new InvalidDataException($"Setup rotazione Titano non valido: {path}");

    private void SeedProfessionalSetups()
    {
        foreach (var setup in ProfessionalSetups())
        {
            var path = Path.Combine(_setupsPath, $"{setup.Id}.json");
            if (!File.Exists(path))
                AtomicFileWriter.WriteAllText(path, JsonSerializer.Serialize(setup, _jsonOptions));
        }
    }

    private static IEnumerable<TitanoRotationSetup> ProfessionalSetups()
    {
        yield return new TitanoRotationSetup
        {
            Id = "conservativo",
            Name = "Conservativo",
            Description = "Protezione del capitale e bassa rotazione. Richiede tutti i filtri, usa isteresi ampia e cooldown lungo; indicato per portafogli maturi e mandate con drawdown contenuto.",
            MinimumTrades = 2,
            ShortWindowDays = 90,
            LongWindowDays = 365,
            MovingAverageWindowDays = 90,
            MinimumZScore = -1.5m,
            MaximumZScore = 2.5m,
            MaximumCurrentDrawdown = 0.15m,
            MaximumObservedDrawdown = 0.25m,
            MaximumReturnVolatility = 0.10m,
            ReenableMaximumCurrentDrawdown = 0.08m,
            DisableCompositeScore = 0.42m,
            ReenableCompositeScore = 0.62m,
            MinimumPassingFilters = 5,
            CooldownPeriodsAfterOff = 3,
            MinimumOnPeriods = 2,
            HardStopDrawdown = 0.30m,
            // Tetto sotto 1: anche la migliore del periodo resta sotto-investita. È la traduzione in
            // sizing della prudenza dichiarata nella descrizione.
            CrossSectionalSizing = true,
            MinimumAllocationMultiplier = 0.20m,
            MaximumAllocationMultiplier = 0.80m,
            AllocationStep = 0.05m,
            SizingTiers =
            [
                new() { MinimumScore = 0.85m, AllocationMultiplier = 1m },
                new() { MinimumScore = 0.70m, AllocationMultiplier = 0.50m },
                new() { MinimumScore = 0.50m, AllocationMultiplier = 0.25m },
                new() { MinimumScore = 0m, AllocationMultiplier = 0m }
            ],
            CalibrationPeriods = 8,
            EvaluationPeriods = 4
        };

        yield return new TitanoRotationSetup
        {
            Id = "bilanciato",
            Name = "Bilanciato",
            Description = "Configurazione baseline professionale: compromesso tra partecipazione, stabilità e controllo del rischio. È il riferimento consigliato per confrontare nuove calibrazioni.",
            MinimumTrades = 1,
            ShortWindowDays = 90,
            LongWindowDays = 365,
            MovingAverageWindowDays = 90,
            MinimumZScore = -1.5m,
            MaximumZScore = 2.5m,
            MaximumCurrentDrawdown = 0.15m,
            MaximumObservedDrawdown = 0.25m,
            MaximumReturnVolatility = 0.10m,
            ReenableMaximumCurrentDrawdown = 0.10m,
            DisableCompositeScore = 0.40m,
            ReenableCompositeScore = 0.60m,
            MinimumPassingFilters = 4,
            CooldownPeriodsAfterOff = 2,
            MinimumOnPeriods = 1,
            HardStopDrawdown = 0.35m,
            CalibrationPeriods = 8,
            EvaluationPeriods = 4
        };

        yield return new TitanoRotationSetup
        {
            Id = "dinamico",
            Name = "Dinamico",
            Description = "Reazione più rapida ai cambi di regime e maggiore partecipazione. Finestre corte, cooldown ridotto e sizing progressivo; adatto a portafogli diversificati che accettano più turnover.",
            MinimumTrades = 1,
            ShortWindowDays = 60,
            LongWindowDays = 270,
            MovingAverageWindowDays = 60,
            MinimumShortReturn = 0.01m,
            MinimumZScore = -1.2m,
            MaximumZScore = 2.0m,
            MaximumCurrentDrawdown = 0.12m,
            MaximumObservedDrawdown = 0.22m,
            MaximumReturnVolatility = 0.12m,
            ReenableMaximumCurrentDrawdown = 0.08m,
            DisableCompositeScore = 0.38m,
            ReenableCompositeScore = 0.55m,
            MinimumPassingFilters = 3,
            CooldownPeriodsAfterOff = 1,
            MinimumOnPeriods = 1,
            HardStopDrawdown = 0.28m,
            // Pavimento più alto e passo più fine: partecipa di più e ridistribuisce più spesso.
            CrossSectionalSizing = true,
            MinimumAllocationMultiplier = 0.35m,
            MaximumAllocationMultiplier = 1m,
            AllocationStep = 0.05m,
            SizingTiers =
            [
                new() { MinimumScore = 0.75m, AllocationMultiplier = 1m },
                new() { MinimumScore = 0.55m, AllocationMultiplier = 0.75m },
                new() { MinimumScore = 0.35m, AllocationMultiplier = 0.50m },
                new() { MinimumScore = 0m, AllocationMultiplier = 0m }
            ],
            CalibrationPeriods = 6,
            EvaluationPeriods = 3
        };
    }

    private static void Validate(TitanoRotationSetup setup)
    {
        if (setup.ShortWindowDays <= 0 || setup.LongWindowDays < setup.ShortWindowDays)
            throw new ArgumentException("Le finestre devono essere positive e la finestra lunga non può essere inferiore alla breve.");
        if (setup.MinimumPassingFilters is < 0 or > 5)
            throw new ArgumentException("MinimumPassingFilters deve essere compreso tra 0 e 5.");
        if (setup.ReenableCompositeScore < setup.DisableCompositeScore)
            throw new ArgumentException("Lo score di riattivazione deve essere almeno pari allo score di disattivazione.");
        if (setup.HardStopDrawdown <= setup.MaximumCurrentDrawdown)
            throw new ArgumentException("L'hard stop deve essere maggiore del drawdown corrente massimo.");
        if (setup.SizingTiers.Count == 0)
            throw new ArgumentException("È richiesto almeno un tier di sizing.");
        if (setup.MinimumAllocationMultiplier is < 0 or > 1 || setup.MaximumAllocationMultiplier is < 0 or > 1)
            throw new ArgumentException("I moltiplicatori di allocazione devono essere compresi tra 0 e 1.");
        if (setup.MinimumAllocationMultiplier > setup.MaximumAllocationMultiplier)
            throw new ArgumentException("L'allocazione minima non può superare quella massima.");
        if (setup.AllocationStep is < 0 or > 1)
            throw new ArgumentException("Il passo di allocazione deve essere compreso tra 0 e 1.");
    }

    private static string SafeId(string value)
    {
        var id = new string(value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray()).Trim('-');
        while (id.Contains("--", StringComparison.Ordinal))
            id = id.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(id) ? $"setup-{DateTime.UtcNow:yyyyMMddHHmmss}" : id;
    }
}
