using System.Text.RegularExpressions;
using Piootoo.Shared.Models;

namespace Piootoo.Domain.Repositories;

/// <summary>
/// Repository per accedere alle definizioni delle strategie
/// </summary>
public class StrategyRepository
{
    private readonly string _strategiesPath;
    private List<StrategyDefinition>? _cachedStrategies;
    private DateTime _lastCacheUpdate;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    // Pattern per estrarre info dal nome file: s_TOP_UA_746_ES_15__7.txt
    // Gruppo 1: nome (TOP_UA_746), Gruppo 2: symbol (ES), Gruppo 3: timeframe (15)
    private static readonly Regex FileNamePattern = new(
        @"^s_(.+?)_([A-Z]+\d*)_(\d+)(?:___?\d+)?\.txt$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Pattern alternativo: s_UA_MC_DAX_Unger_FirstHour_Stop2Market__7.txt
    private static readonly Regex AlternativePattern = new(
        @"^s_(.+?)_(DAX|ES|NQ|CL|GC|EC|FDAX|FGBL|FC|HO|NG|RB|SI|TY|US|VX|YM|AD|BP|CD|JY|CT|KC|LC|LH|PL|C)_.+__(\d+)\.txt$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Mapping dei simboli da formato breve a formato con @
    private static readonly Dictionary<string, string> SymbolMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ES", "@ES" },
        { "NQ", "@NQ" },
        { "CL", "@CL" },
        { "GC", "@GC" },
        { "EC", "@EC" },
        { "FDAX", "@FDAX" },
        { "FGBL", "@FGBL" },
        { "FC", "@FC" },
        { "HO", "@HO" },
        { "NG", "@NG" },
        { "RB", "@RB" },
        { "SI", "@SI" },
        { "TY", "@TY" },
        { "US", "@US" },
        { "VX", "@VX" },
        { "YM", "@YM" },
        { "AD", "@AD" },
        { "BP", "@BP" },
        { "CD", "@CD" },
        { "JY", "@JY" },
        { "CT", "@CT" },
        { "KC", "@KC" },
        { "LC", "@LC" },
        { "LH", "@LH" },
        { "PL", "@PL" },
        { "C", "@C" },
        { "HG", "@HG" },
        { "DAX", "@FDAX" },
        { "BTCUSDT", "BTCUSDT" }
    };

    public StrategyRepository(string strategiesPath)
    {
        _strategiesPath = strategiesPath;
    }

    /// <summary>
    /// Ottiene tutte le strategie disponibili
    /// </summary>
    public List<StrategyDefinition> GetAllStrategies(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedStrategies != null && 
            DateTime.Now - _lastCacheUpdate < _cacheExpiration)
        {
            return _cachedStrategies;
        }

        _cachedStrategies = LoadStrategiesFromDisk();
        _lastCacheUpdate = DateTime.Now;
        return _cachedStrategies;
    }

    /// <summary>
    /// Ottiene le strategie per un simbolo specifico
    /// </summary>
    public List<StrategyDefinition> GetStrategiesBySymbol(string symbol)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        return GetAllStrategies()
            .Where(s => s.Symbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Ottiene le strategie per una lista di simboli
    /// </summary>
    public List<StrategyDefinition> GetStrategiesBySymbols(IEnumerable<string> symbols)
    {
        var symbolList = symbols.ToList();
        Console.WriteLine($"[StrategyRepository] Richiesta strategie per simboli: {string.Join(", ", symbolList)}");
        
        var normalizedSymbols = symbolList.Select(NormalizeSymbol).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Console.WriteLine($"[StrategyRepository] Simboli normalizzati: {string.Join(", ", normalizedSymbols)}");
        
        var allStrategies = GetAllStrategies();
        Console.WriteLine($"[StrategyRepository] Totale strategie disponibili: {allStrategies.Count}");
        
        // Log dei simboli unici nelle strategie
        var uniqueSymbols = allStrategies.Select(s => s.Symbol).Distinct().ToList();
        Console.WriteLine($"[StrategyRepository] Simboli unici nelle strategie: {string.Join(", ", uniqueSymbols)}");
        
        var matched = allStrategies
            .Where(s => normalizedSymbols.Contains(s.Symbol))
            .ToList();
        
        Console.WriteLine($"[StrategyRepository] Strategie trovate: {matched.Count}");
        foreach (var strategy in matched.Take(10))
        {
            Console.WriteLine($"[StrategyRepository]   - {strategy.Name} (Symbol: {strategy.Symbol}, Timeframe: {strategy.TimeframeMinutes})");
        }
        
        return matched;
    }

    /// <summary>
    /// Ottiene le strategie raggruppate per simbolo
    /// </summary>
    public List<SymbolStrategiesInfo> GetStrategiesGroupedBySymbol()
    {
        return GetAllStrategies()
            .GroupBy(s => s.Symbol)
            .Select(g => new SymbolStrategiesInfo
            {
                Symbol = g.Key,
                TotalStrategies = g.Count(),
                ActiveStrategies = g.Count(s => s.IsActive),
                AvailableTimeframes = g.Select(s => s.TimeframeMinutes).Distinct().OrderBy(t => t).ToList(),
                Strategies = g.ToList()
            })
            .OrderBy(s => s.Symbol)
            .ToList();
    }

    /// <summary>
    /// Ottiene i simboli unici che hanno strategie
    /// </summary>
    public List<string> GetSymbolsWithStrategies()
    {
        return GetAllStrategies()
            .Select(s => s.Symbol)
            .Distinct()
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>
    /// Cerca una strategia per ID
    /// </summary>
    public StrategyDefinition? GetById(string id)
    {
        return GetAllStrategies().FirstOrDefault(s => s.Id == id);
    }

    private List<StrategyDefinition> LoadStrategiesFromDisk()
    {
        var strategies = new List<StrategyDefinition>();

        if (!Directory.Exists(_strategiesPath))
            return strategies;

        var files = Directory.GetFiles(_strategiesPath, "s_*.txt");

        foreach (var filePath in files)
        {
            var strategy = ParseStrategyFile(filePath);
            if (strategy != null)
            {
                strategies.Add(strategy);
            }
        }

        return strategies.OrderBy(s => s.Symbol).ThenBy(s => s.TimeframeMinutes).ThenBy(s => s.Name).ToList();
    }

    private StrategyDefinition? ParseStrategyFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        
        // Prova con il pattern principale
        var match = FileNamePattern.Match(fileName);
        if (!match.Success)
        {
            // Prova con il pattern alternativo
            match = AlternativePattern.Match(fileName);
        }

        if (!match.Success)
            return null;

        var name = match.Groups[1].Value;
        var symbolShort = match.Groups[2].Value;
        var timeframeStr = match.Groups[3].Value;

        // Il gruppo 3 per AlternativePattern contiene la versione, non il timeframe
        // Devo estrarre il timeframe dal nome in modo diverso
        if (!int.TryParse(timeframeStr, out var timeframe))
        {
            timeframe = 15; // Default
        }

        // Se il timeframe è troppo piccolo (versione), cerca nel nome
        if (timeframe < 1 || timeframe > 10080)
        {
            // Cerca un numero nel nome che potrebbe essere il timeframe
            var tfMatch = Regex.Match(fileName, @"_(\d{1,4})(?:__|_\d+\.txt)");
            if (tfMatch.Success && int.TryParse(tfMatch.Groups[1].Value, out var tf) && tf >= 1 && tf <= 1440)
            {
                timeframe = tf;
            }
            else
            {
                timeframe = 15; // Default
            }
        }

        var symbol = NormalizeSymbol(symbolShort);
        
        // Leggi il file per estrarre descrizione e parametri
        var (description, strategyType, parameters) = ParseStrategyContent(filePath);

        return new StrategyDefinition
        {
            Id = Path.GetFileNameWithoutExtension(fileName),
            Name = name,
            FileName = fileName,
            Symbol = symbol,
            TimeframeMinutes = timeframe,
            Description = description,
            Type = strategyType,
            Parameters = parameters,
            FilePath = filePath,
            LastModified = File.GetLastWriteTime(filePath),
            IsActive = true
        };
    }

    private (string Description, StrategyType Type, Dictionary<string, object> Parameters) ParseStrategyContent(string filePath)
    {
        var description = string.Empty;
        var strategyType = StrategyType.Unknown;
        var parameters = new Dictionary<string, object>();

        try
        {
            var lines = File.ReadAllLines(filePath);
            var inCommentBlock = false;
            var commentBuilder = new List<string>();

            foreach (var line in lines.Take(50)) // Leggi solo le prime 50 righe
            {
                var trimmedLine = line.Trim();

                // Commenti EasyLanguage
                if (trimmedLine.StartsWith("{"))
                {
                    inCommentBlock = true;
                    commentBuilder.Add(trimmedLine.TrimStart('{').TrimEnd('}'));
                    if (trimmedLine.EndsWith("}"))
                        inCommentBlock = false;
                }
                else if (inCommentBlock)
                {
                    if (trimmedLine.EndsWith("}"))
                    {
                        commentBuilder.Add(trimmedLine.TrimEnd('}'));
                        inCommentBlock = false;
                    }
                    else
                    {
                        commentBuilder.Add(trimmedLine);
                    }
                }
                else if (trimmedLine.StartsWith("//"))
                {
                    commentBuilder.Add(trimmedLine.TrimStart('/').Trim());
                }
                // Input parameters
                else if (trimmedLine.StartsWith("input:", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("Input:", StringComparison.OrdinalIgnoreCase))
                {
                    ParseInputLine(trimmedLine, parameters);
                }

                // Detect strategy type from content
                if (trimmedLine.Contains("trend following", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("trend-following", StringComparison.OrdinalIgnoreCase))
                {
                    strategyType = StrategyType.TrendFollowing;
                }
                else if (trimmedLine.Contains("counter trend", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.Contains("countertrend", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.Contains("counter-trend", StringComparison.OrdinalIgnoreCase))
                {
                    strategyType = StrategyType.CounterTrend;
                }
                else if (trimmedLine.Contains("breakout", StringComparison.OrdinalIgnoreCase))
                {
                    strategyType = StrategyType.Breakout;
                }
                else if (trimmedLine.Contains("mean reversion", StringComparison.OrdinalIgnoreCase))
                {
                    strategyType = StrategyType.MeanReversion;
                }
            }

            description = string.Join(" ", commentBuilder.Where(c => !string.IsNullOrWhiteSpace(c)).Take(3));
        }
        catch
        {
            // Ignore parsing errors
        }

        return (description, strategyType, parameters);
    }

    private void ParseInputLine(string line, Dictionary<string, object> parameters)
    {
        // input: MyStop(1100), MyProfit(3100);
        // Input: Mycontracts(1);
        var inputPart = line.Substring(line.IndexOf(':') + 1).TrimEnd(';');
        var inputs = inputPart.Split(',');

        foreach (var input in inputs)
        {
            var match = Regex.Match(input.Trim(), @"(\w+)\s*\(([^)]+)\)");
            if (match.Success)
            {
                var paramName = match.Groups[1].Value;
                var paramValue = match.Groups[2].Value.Trim();

                // Try to parse as number
                if (decimal.TryParse(paramValue, out var decimalValue))
                {
                    parameters[paramName] = decimalValue;
                }
                else
                {
                    parameters[paramName] = paramValue;
                }
            }
        }
    }

    private string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return symbol;

        // Se già inizia con @, ritorna così com'è
        if (symbol.StartsWith("@"))
            return symbol;

        // Cerca nel mapping
        if (SymbolMapping.TryGetValue(symbol, out var mapped))
            return mapped;

        // Altrimenti aggiungi @ come prefisso
        return $"@{symbol}";
    }
}
