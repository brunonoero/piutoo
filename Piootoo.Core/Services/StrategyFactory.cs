using System.Reflection;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Strategies;
using Piootoo.Strategies.Easy;

namespace Piootoo.Core.Services;

/// <summary>
/// Factory per creare istanze di strategie C#
/// </summary>
public static class StrategyFactory
{
    private static readonly Dictionary<string, Type> _strategyCache = new();
    private static readonly object CacheInitializationLock = new();
    private static bool _cacheInitialized = false;

    /// <summary>
    /// Restituisce le strategie eseguibili del catalogo. E' la sorgente ufficiale per UI e
    /// backtesting.
    ///
    /// <para>Le strategie <see cref="ITradingStrategy.IsPositionCloseDependent"/> sono ESCLUSE: decidono
    /// l'uscita a runtime e non possono descriverla nel segnale di ingresso, mentre l'engine gestisce
    /// solo uscite autonome (SL, TP, CloseAtUtc, MaxBarsInPosition). Vedi
    /// <see cref="GetCloseDependentStrategyIds"/> per l'elenco degli esclusi.</para>
    ///
    /// <para>Sono escluse anche quelle marcate <see cref="StrategiaDisabilitataAttribute"/>: non
    /// sono rotte, si e' scelto di non eseguirle. Il motivo e' nell'attributo.</para>
    /// </summary>
    public static List<StrategyDefinition> GetRegisteredStrategies(string? name = null, string? symbol = null)
    {
        InitializeStrategyCache();

        var normalizedSymbol = NormalizeSymbol(symbol);
        var definitions = new List<StrategyDefinition>();

        foreach (var (className, strategyType) in _strategyCache.OrderBy(item => item.Key))
        {
            var instance = CreateStrategyInstance(strategyType, string.Empty, 0, null);
            if (instance == null)
            {
                continue;
            }

            if (instance.IsPositionCloseDependent)
            {
                // Uscita decisa a runtime (pattern di uscita): non esprimibile nel segnale di ingresso.
                continue;
            }

            if (strategyType.GetCustomAttribute<StrategiaDisabilitataAttribute>() is not null)
            {
                // Disabilitata deliberatamente: corretta ma non da eseguire. Resta istanziabile per
                // nome da CreateStrategy, cosi' i test di parita' e i confronti storici la vedono.
                continue;
            }

            var strategyName = instance.Name;
            var strategySymbol = NormalizeSymbol(instance.Symbol);

            if (!string.IsNullOrWhiteSpace(name) &&
                !strategyName.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                !className.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalizedSymbol) &&
                !strategySymbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            definitions.Add(new StrategyDefinition
            {
                Id = className,
                Name = strategyName,
                FileName = $"{className}.cs",
                Symbol = strategySymbol,
                TimeframeMinutes = instance.TimeframeMinutes,
                Description = instance.Description,
                IsActive = true,
                LastModified = DateTime.MinValue,
                FilePath = strategyType.FullName ?? className
            });
        }

        return definitions
            .OrderBy(strategy => strategy.Symbol)
            .ThenBy(strategy => strategy.TimeframeMinutes)
            .ThenBy(strategy => strategy.Name)
            .ToList();
    }

    /// <summary>
    /// Id delle strategie escluse dal catalogo perché close-dependent. Serve a spiegare all'utente
    /// perché un Id presente nei sorgenti non compare più tra le strategie selezionabili, invece di
    /// farlo sparire in silenzio.
    /// </summary>
    public static List<string> GetCloseDependentStrategyIds()
    {
        InitializeStrategyCache();

        return _strategyCache
            .Where(item => CreateStrategyInstance(item.Value, string.Empty, 0, null)?.IsPositionCloseDependent == true)
            .Select(item => item.Key)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    public static List<string> GetRegisteredSymbols()
    {
        return GetRegisteredStrategies()
            .Select(strategy => strategy.Symbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol)
            .ToList();
    }

    /// <summary>
    /// Crea un'istanza di strategia dal suo Id di classe o dal suo Name.
    ///
    /// <para>Risolve anche le strategie marcate <see cref="StrategiaDisabilitataAttribute"/>, che
    /// il catalogo non elenca: servono ai test di parita' e ai confronti con artefatti storici.</para>
    /// </summary>
    public static ITradingStrategy? CreateStrategy(string strategyName, string symbol, int timeframeMinutes = 60, Dictionary<string, object>? parameters = null)
    {
        Console.WriteLine($"[StrategyFactory] Tentativo di creare strategia: Name='{strategyName}', Symbol='{symbol}', Timeframe={timeframeMinutes}");

        var registeredStrategy = CreateRegisteredStrategy(strategyName, symbol, timeframeMinutes, parameters);
        if (registeredStrategy != null)
        {
            Console.WriteLine($"[StrategyFactory] Strategia registrata creata con successo: {registeredStrategy.GetType().Name}");
            return registeredStrategy;
        }

        Console.WriteLine($"[StrategyFactory] Nessuna strategia trovata per '{strategyName}'");
        return null;
    }

    private static ITradingStrategy? CreateRegisteredStrategy(string strategyName, string symbol, int timeframeMinutes, Dictionary<string, object>? parameters)
    {
        InitializeStrategyCache();

        var normalizedSymbol = NormalizeSymbol(symbol);
        foreach (var (className, strategyType) in _strategyCache)
        {
            if (!className.Equals(strategyName, StringComparison.OrdinalIgnoreCase) &&
                !(strategyType.FullName?.Equals(strategyName, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                var metadataInstance = CreateStrategyInstance(strategyType, string.Empty, 0, null);
                if (metadataInstance == null ||
                    !metadataInstance.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var metadataSymbol = NormalizeSymbol(metadataInstance.Symbol);
                if (!string.IsNullOrWhiteSpace(normalizedSymbol) &&
                    !metadataSymbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (timeframeMinutes > 0 && metadataInstance.TimeframeMinutes != timeframeMinutes)
                {
                    continue;
                }
            }

            var created = CreateStrategyInstance(strategyType, symbol, timeframeMinutes, parameters);
            if (created?.IsPositionCloseDependent == true)
            {
                // Esclusa dal catalogo: non deve poter rientrare da un masterfilter salvato in passato.
                throw new InvalidOperationException(
                    $"La strategia '{className}' è close-dependent (uscita decisa a runtime) e non è più " +
                    "eseguibile: l'engine gestisce solo uscite descritte nel segnale di ingresso " +
                    "(StopLoss, TakeProfit, CloseAtUtc, MaxBarsInPosition). Rimuovila dal masterfilter.");
            }

            return created;
        }

        return null;
    }

    /// <summary>
    /// Popola la cache con tutti i tipi ITradingStrategy concreti dell'assembly delle strategie.
    /// L'ancora e' <see cref="EasyLib"/> perche' e' un tipo che non appartiene a nessuna
    /// strategia e quindi non si sposta quando il catalogo cambia.
    /// </summary>
    private static void InitializeStrategyCache()
    {
        if (_cacheInitialized) return;

        lock (CacheInitializationLock)
        {
            if (_cacheInitialized) return;

            try
            {
                var assembly = Assembly.GetAssembly(typeof(EasyLib));
                if (assembly != null)
                {
                    var strategyTypes = assembly.GetTypes()
                        .Where(t => t.IsClass &&
                                    !t.IsAbstract &&
                                    typeof(ITradingStrategy).IsAssignableFrom(t))
                        .ToList();

                    foreach (var type in strategyTypes)
                    {
                        _strategyCache[type.Name] = type;
                    }

                    Console.WriteLine($"[StrategyFactory] Cache inizializzata con {_strategyCache.Count} strategie");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StrategyFactory] Errore durante l'inizializzazione della cache: {ex.Message}");
            }

            _cacheInitialized = true;
        }
    }

    /// <summary>
    /// Crea un'istanza di strategia usando reflection
    /// </summary>
    private static ITradingStrategy? CreateStrategyInstance(Type strategyType, string symbol, int timeframeMinutes, Dictionary<string, object>? parameters)
    {
        try
        {
            var instance = Activator.CreateInstance(strategyType) as ITradingStrategy;
            if (instance == null)
            {
                Console.WriteLine($"[StrategyFactory] Impossibile creare istanza di {strategyType.Name}");
                return null;
            }
            
            // Inizializza la strategia se ha un metodo Initialize
            var initParams = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                initParams["Symbol"] = symbol;
            }

            if (timeframeMinutes > 0)
            {
                initParams["TimeframeMinutes"] = timeframeMinutes;
            }
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    initParams[param.Key] = param.Value;
                }
            }
            
            var initializeMethod = strategyType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
            if (initializeMethod != null)
            {
                initializeMethod.Invoke(instance, new object[] { initParams });
            }
            
            return instance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StrategyFactory] Errore durante la creazione dell'istanza {strategyType.Name}: {ex.Message}");
            return null;
        }
    }

    private static string NormalizeSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return string.Empty;
        }

        var normalized = symbol.Trim().ToUpperInvariant();
        return normalized.StartsWith("@", StringComparison.Ordinal) ? normalized : $"@{normalized}";
    }
}
