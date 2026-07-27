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
    private static readonly Dictionary<string, Type> _easyStrategyCache = new();
    private static bool _cacheInitialized = false;

    /// <summary>
    /// Restituisce tutte le strategie C# registrate che implementano ITradingStrategy.
    /// Questa e' la sorgente ufficiale per UI/backtesting: i file EasyLanguage sono solo sorgenti/metadati.
    ///
    /// <para>Le strategie <see cref="ITradingStrategy.IsPositionCloseDependent"/> sono ESCLUSE: decidono
    /// l'uscita a runtime e non possono descriverla nel segnale di ingresso, mentre l'engine gestisce
    /// solo uscite autonome (SL, TP, CloseAtUtc, MaxBarsInPosition). Vedi
    /// <see cref="GetCloseDependentStrategyIds"/> per l'elenco degli esclusi.</para>
    /// </summary>
    public static List<StrategyDefinition> GetRegisteredStrategies(string? name = null, string? symbol = null)
    {
        InitializeEasyStrategyCache();

        var normalizedSymbol = NormalizeSymbol(symbol);
        var definitions = new List<StrategyDefinition>();

        foreach (var (className, strategyType) in _easyStrategyCache.OrderBy(item => item.Key))
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
        InitializeEasyStrategyCache();

        return _easyStrategyCache
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
    /// Crea un'istanza di strategia basata sul nome e parametri
    /// Supporta sia strategie C# native che strategie EasyLanguage convertite
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

        // Strategie EasyLanguage convertite - prova a creare dinamicamente tramite pattern legacy
        var easyStrategy = CreateEasyLanguageStrategy(strategyName, symbol, timeframeMinutes, parameters);
        if (easyStrategy != null)
        {
            Console.WriteLine($"[StrategyFactory] Strategia EasyLanguage creata con successo: {easyStrategy.GetType().Name}");
            return easyStrategy;
        }
        
        Console.WriteLine($"[StrategyFactory] Nessuna strategia trovata per '{strategyName}'");
        return null;
    }

    private static ITradingStrategy? CreateRegisteredStrategy(string strategyName, string symbol, int timeframeMinutes, Dictionary<string, object>? parameters)
    {
        InitializeEasyStrategyCache();

        var normalizedSymbol = NormalizeSymbol(symbol);
        foreach (var (className, strategyType) in _easyStrategyCache)
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
    /// Crea dinamicamente una strategia EasyLanguage basata sul pattern del nome
    /// Pattern: Easy_{number}_{SYMBOL}_{timeframe} o TOP_UA_{number}_{SYMBOL}_{timeframe}
    /// </summary>
    private static ITradingStrategy? CreateEasyLanguageStrategy(string strategyName, string symbol, int timeframeMinutes, Dictionary<string, object>? parameters)
    {
        InitializeEasyStrategyCache();
        
        // Normalizza il simbolo per il matching (rimuovi @ e converti in maiuscolo)
        var normalizedSymbol = symbol.Replace("@", "").ToUpper();
        
        // Cerca pattern nel nome della strategia
        // Pattern 1: Easy_{number}_{SYMBOL}_{timeframe} -> Easy_643_FDAX_60
        // Pattern 2: TOP_UA_{number}_{SYMBOL}_{timeframe} -> TOP_UA_643_FDAX_60
        // Pattern 3: s_TOP_UA_{number}_{SYMBOL}_{timeframe}__{version} -> s_TOP_UA_643_FDAX_60__7
        
        string? className = null;
        
        // Prova pattern Easy_{number}_{SYMBOL}_{timeframe}
        var easyMatch = System.Text.RegularExpressions.Regex.Match(strategyName, @"Easy[_\s]*(\d+)[_\s]*(\w+)[_\s]*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (easyMatch.Success)
        {
            var number = easyMatch.Groups[1].Value;
            var symbolInName = easyMatch.Groups[2].Value.ToUpper();
            var timeframeInName = easyMatch.Groups[3].Value;
            
            // Verifica che symbol e timeframe corrispondano
            if (symbolInName == normalizedSymbol && int.TryParse(timeframeInName, out var tf) && tf == timeframeMinutes)
            {
                className = $"Easy_{number}_{symbolInName}_{timeframeInName}";
            }
        }
        
        // Prova pattern TOP_UA_{number}_{SYMBOL}_{timeframe}
        if (className == null)
        {
            var topMatch = System.Text.RegularExpressions.Regex.Match(strategyName, @"TOP[_\s]*UA[_\s]*(\d+)[_\s]*(\w+)[_\s]*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (topMatch.Success)
            {
                var number = topMatch.Groups[1].Value;
                var symbolInName = topMatch.Groups[2].Value.ToUpper();
                var timeframeInName = topMatch.Groups[3].Value;
                
                // Verifica che symbol e timeframe corrispondano
                if (symbolInName == normalizedSymbol && int.TryParse(timeframeInName, out var tf) && tf == timeframeMinutes)
                {
                    className = $"Easy_{number}_{symbolInName}_{timeframeInName}";
                }
            }
        }
        
        // Prova pattern s_TOP_UA_{number}_{SYMBOL}_{timeframe}__{version}
        if (className == null)
        {
            var sMatch = System.Text.RegularExpressions.Regex.Match(strategyName, @"s[_\s]*TOP[_\s]*UA[_\s]*(\d+)[_\s]*(\w+)[_\s]*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (sMatch.Success)
            {
                var number = sMatch.Groups[1].Value;
                var symbolInName = sMatch.Groups[2].Value.ToUpper();
                var timeframeInName = sMatch.Groups[3].Value;
                
                // Verifica che symbol e timeframe corrispondano
                if (symbolInName == normalizedSymbol && int.TryParse(timeframeInName, out var tf) && tf == timeframeMinutes)
                {
                    className = $"Easy_{number}_{symbolInName}_{timeframeInName}";
                }
            }
        }
        
        // Se non trovato con pattern, prova a cercare direttamente nel nome
        if (className == null)
        {
            // Estrai numero, symbol e timeframe dal nome se possibile
            var numberMatch = System.Text.RegularExpressions.Regex.Match(strategyName, @"(\d{2,4})");
            if (numberMatch.Success)
            {
                var number = numberMatch.Groups[1].Value;
                className = $"Easy_{number}_{normalizedSymbol}_{timeframeMinutes}";
            }
        }
        
        if (className == null)
        {
            Console.WriteLine($"[StrategyFactory] Impossibile determinare className per strategia '{strategyName}'");
            return null;
        }
        
        Console.WriteLine($"[StrategyFactory] Tentativo di creare classe EasyLanguage: {className}");
        
        // Cerca la classe nel cache
        if (_easyStrategyCache.TryGetValue(className, out var strategyType))
        {
            return CreateStrategyInstance(strategyType, symbol, timeframeMinutes, parameters);
        }
        
        // Cerca nel namespace Piootoo.Strategies.Easy
        var assembly = Assembly.GetAssembly(typeof(Easy_643_FDAX_60));
        if (assembly != null)
        {
            strategyType = assembly.GetType($"Piootoo.Strategies.Easy.{className}");
            if (strategyType != null)
            {
                _easyStrategyCache[className] = strategyType;
                return CreateStrategyInstance(strategyType, symbol, timeframeMinutes, parameters);
            }
        }
        
        Console.WriteLine($"[StrategyFactory] Classe '{className}' non trovata nell'assembly");
        return null;
    }

    /// <summary>
    /// Inizializza la cache delle strategie EasyLanguage disponibili
    /// </summary>
    private static void InitializeEasyStrategyCache()
    {
        if (_cacheInitialized) return;
        
        try
        {
            var assembly = Assembly.GetAssembly(typeof(Easy_643_FDAX_60));
            if (assembly != null)
            {
                var easyTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && 
                                !t.IsAbstract && 
                                typeof(ITradingStrategy).IsAssignableFrom(t))
                    .ToList();
                
                foreach (var type in easyTypes)
                {
                    _easyStrategyCache[type.Name] = type;
                }
                
                Console.WriteLine($"[StrategyFactory] Cache inizializzata con {_easyStrategyCache.Count} strategie EasyLanguage");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StrategyFactory] Errore durante l'inizializzazione della cache: {ex.Message}");
        }
        
        _cacheInitialized = true;
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


    
    private static ITradingStrategy CreateEasy643FDAX60(string symbol, int timeframeMinutes, Dictionary<string, object>? parameters)
    {
        var strategy = new Easy_643_FDAX_60();
        var initParams = new Dictionary<string, object>
        {
            { "Symbol", symbol },
            { "TimeframeMinutes", timeframeMinutes }
        };
        
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                initParams[param.Key] = param.Value;
            }
        }
        
        strategy.Initialize(initParams);
        return strategy;
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
