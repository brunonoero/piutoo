using System.Collections.Concurrent;

namespace Piootoo.Core.Services;

/// <summary>
/// Traduce tra i due identificatori di una strategia. Il sistema ne usa due e confonderli è
/// storicamente la fonte di bug più costosa del progetto (vedi docs/PROGETTO.md §3.2):
///
/// - <b>Id</b>: il nome della classe, es. <c>PTS_NQ_TFM_001_60</c>. È la chiave di SELEZIONE:
///   masterfilter del workspace, catalogo, <see cref="StrategyFactory"/>.
/// - <b>Codice di esecuzione</b> (<c>ITradingStrategy.Name</c>), es. <c>PTS_NQ_TFM_001_60</c>. È la
///   chiave di ESECUZIONE: segnali, trade, chiavi di posizione, report.
///
/// Ogni volta che il masterfilter va confrontato con dati di esecuzione (trades.json, segnali)
/// bisogna passare da qui.
/// </summary>
public static class StrategyCatalog
{
    private static readonly ConcurrentDictionary<string, string> IdToExecutionCode =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> ExecutionCodeToId =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly object BuildGate = new();
    private static bool _built;

    private static void EnsureBuilt()
    {
        if (_built) return;
        lock (BuildGate)
        {
            if (_built) return;
            foreach (var definition in StrategyFactory.GetRegisteredStrategies())
            {
                if (string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name))
                    continue;
                IdToExecutionCode[definition.Id] = definition.Name;
                ExecutionCodeToId[definition.Name] = definition.Id;
            }
            _built = true;
        }
    }

    /// <summary>Codice di esecuzione (Name) a partire dall'Id di catalogo; null se l'Id non esiste.</summary>
    public static string? TryGetExecutionCode(string strategyId)
    {
        if (string.IsNullOrWhiteSpace(strategyId)) return null;
        EnsureBuilt();
        return IdToExecutionCode.TryGetValue(strategyId.Trim(), out var code) ? code : null;
    }

    /// <summary>Id di catalogo a partire dal codice di esecuzione; null se il codice non esiste.</summary>
    public static string? TryGetId(string executionCode)
    {
        if (string.IsNullOrWhiteSpace(executionCode)) return null;
        EnsureBuilt();
        return ExecutionCodeToId.TryGetValue(executionCode.Trim(), out var id) ? id : null;
    }

    /// <summary>
    /// Converte una lista di Id di masterfilter nei corrispondenti codici di esecuzione,
    /// deduplicati e ordinati in modo stabile.
    ///
    /// Gli elementi che sono già un codice di esecuzione valido vengono lasciati passare: rende
    /// la funzione idempotente e compatibile con masterfilter salvati prima della distinzione.
    /// Gli elementi non risolvibili vengono restituiti invariati, così il chiamante può
    /// segnalarli invece di perderli in silenzio.
    /// </summary>
    public static string[] ResolveExecutionCodes(IEnumerable<string> strategyIds)
    {
        EnsureBuilt();
        return strategyIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => TryGetExecutionCode(id) ?? id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Id del masterfilter che non corrispondono ad alcuna strategia del catalogo.</summary>
    public static string[] FindUnknownIds(IEnumerable<string> strategyIds)
    {
        EnsureBuilt();
        return strategyIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Where(id => TryGetExecutionCode(id) is null && TryGetId(id) is null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
