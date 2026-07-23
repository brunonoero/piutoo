using System.Text.RegularExpressions;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using Piootoo.Shared.Enums;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Generatore automatico di classi strategia da file EasyLanguage
/// </summary>
public static class StrategyGenerator
{
    /// <summary>
    /// Genera il codice C# per una strategia basandosi sul file EasyLanguage
    /// </summary>
    public static string GenerateStrategyClass(string filePath, string className)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File non trovato: {filePath}");

        var content = File.ReadAllText(filePath);
        var fileName = Path.GetFileName(filePath);
        
        // Estrai informazioni dal nome file: s_TOP_UA_643_FDAX_60__7.txt o s_UA_MC_DAX_UngerFirstHour_STOP2MARKET_DATA2__7.txt
        // Pattern più flessibile: cerca l'ultimo _SYMBOL_TIMEFRAME prima di __numero.txt
        var nameMatch = Regex.Match(fileName, @"^s_(.+?)_([A-Z]+)_(\d+)(?:__\d+)?\.txt$", RegexOptions.IgnoreCase);
        if (!nameMatch.Success)
        {
            // Prova pattern alternativo: cerca DAX/NQ/CL/GC seguito da _numero
            nameMatch = Regex.Match(fileName, @"^s_(.+?)_(DAX|NQ|CL|GC|FDAX|FNQ|FCL|FGC)_(\d+)(?:__\d+)?\.txt$", RegexOptions.IgnoreCase);
        }
        if (!nameMatch.Success)
            throw new ArgumentException($"Nome file non valido: {fileName}");

        var strategyName = nameMatch.Groups[1].Value;
        var symbolShort = nameMatch.Groups[2].Value;
        var timeframeStr = nameMatch.Groups[3].Value;
        
        // Normalizza symbol
        var symbol = symbolShort.StartsWith("@") ? symbolShort : $"@{symbolShort}";
        var timeframe = int.Parse(timeframeStr);
        
        // Estrai descrizione (prima riga non vuota dopo commenti)
        var description = ExtractDescription(content);
        
        // Estrai input e variabili
        var inputs = ExtractInputs(content);
        var variables = ExtractVariables(content);
        
        // Genera codice classe
        return GenerateClassCode(className, strategyName, symbol, timeframe, description, inputs, variables);
    }
    
    private static string ExtractDescription(string content)
    {
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && 
                !trimmed.StartsWith("//") && 
                !trimmed.StartsWith("input:") && 
                !trimmed.StartsWith("var:") &&
                !trimmed.StartsWith("array:") &&
                !trimmed.StartsWith("{"))
            {
                // Rimuovi commenti inline
                var desc = trimmed.Split("//")[0].Trim();
                if (!string.IsNullOrEmpty(desc))
                    return desc;
            }
            else if (trimmed.StartsWith("//") && trimmed.Length > 2)
            {
                var desc = trimmed.Substring(2).Trim();
                if (desc.Length > 10 && !desc.Contains(">>>>") && !desc.Contains("Michael"))
                    return desc;
            }
        }
        return "Strategia EasyLanguage convertita";
    }
    
    private static Dictionary<string, string> ExtractInputs(string content)
    {
        var inputs = new Dictionary<string, string>();
        var inputPattern = new Regex(@"input:\s*(\w+)\(([^)]+)\)", RegexOptions.IgnoreCase);
        var inputsPattern = new Regex(@"inputs:\s*(\w+)\(([^)]+)\)", RegexOptions.IgnoreCase);
        
        foreach (Match match in inputPattern.Matches(content))
        {
            inputs[match.Groups[1].Value] = match.Groups[2].Value;
        }
        
        foreach (Match match in inputsPattern.Matches(content))
        {
            inputs[match.Groups[1].Value] = match.Groups[2].Value;
        }
        
        return inputs;
    }
    
    private static Dictionary<string, string> ExtractVariables(string content)
    {
        var variables = new Dictionary<string, string>();
        var varPattern = new Regex(@"var:\s*(\w+)\(([^)]+)\)", RegexOptions.IgnoreCase);
        var varsPattern = new Regex(@"vars:\s*(\w+)\(([^)]+)\)", RegexOptions.IgnoreCase);
        var variablePattern = new Regex(@"variable:\s*(\w+)\(([^)]+)\)", RegexOptions.IgnoreCase);
        
        foreach (Match match in varPattern.Matches(content))
        {
            variables[match.Groups[1].Value] = match.Groups[2].Value;
        }
        
        foreach (Match match in varsPattern.Matches(content))
        {
            variables[match.Groups[1].Value] = match.Groups[2].Value;
        }
        
        foreach (Match match in variablePattern.Matches(content))
        {
            variables[match.Groups[1].Value] = match.Groups[2].Value;
        }
        
        return variables;
    }
    
    private static string GenerateClassCode(string className, string strategyName, string symbol, int timeframe, 
        string description, Dictionary<string, string> inputs, Dictionary<string, string> variables)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("using Piootoo.Shared.Enums;");
        sb.AppendLine("using Piootoo.Shared.Interfaces;");
        sb.AppendLine("using Piootoo.Shared.Models;");
        sb.AppendLine("using static Piootoo.Strategies.Easy.EasyLib;");
        sb.AppendLine();
        sb.AppendLine("namespace Piootoo.Strategies.Easy;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Strategia EasyLanguage convertita: {strategyName}");
        sb.AppendLine($"/// {description}");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class {className} : StatelessEasyStrategyBase");
        sb.AppendLine("{");
        
        // Fields per inputs
        sb.AppendLine("    // INPUTS");
        foreach (var input in inputs)
        {
            var csharpType = InferCSharpType(input.Value);
            var fieldName = $"_{char.ToLower(input.Key[0])}{input.Key.Substring(1)}";
            sb.AppendLine($"    private {csharpType} {fieldName} = {ConvertDefaultValue(input.Value, csharpType)};");
        }
        
        // Fields per variables
        sb.AppendLine();
        sb.AppendLine("    // VARIABLES");
        foreach (var variable in variables)
        {
            var csharpType = InferCSharpType(variable.Value);
            var fieldName = $"_{char.ToLower(variable.Key[0])}{variable.Key.Substring(1)}";
            sb.AppendLine($"    private {csharpType} {fieldName} = {ConvertDefaultValue(variable.Value, csharpType)};");
        }
        
        // Properties
        sb.AppendLine();
        sb.AppendLine("    // STATE");
        sb.AppendLine($"    private string _symbol = \"{symbol}\";");
        sb.AppendLine($"    private int _timeframeMinutes = {timeframe};");
        sb.AppendLine($"    private string _name = \"{strategyName}\";");
        sb.AppendLine($"    private string _description = \"{description}\";");
        sb.AppendLine();
        sb.AppendLine("    public string Name => _name;");
        sb.AppendLine("    public string Description => _description;");
        sb.AppendLine("    public string Symbol => _symbol;");
        sb.AppendLine("    public int TimeframeMinutes => _timeframeMinutes;");
        sb.AppendLine("    public int RequiredCandles => 100; // TODO: Calcolare in base alla strategia");
        sb.AppendLine();
        
        // Initialize method
        sb.AppendLine("    public void Initialize(Dictionary<string, object>? parameters = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (parameters != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (parameters.TryGetValue(\"Symbol\", out var sym))");
        sb.AppendLine("                _symbol = sym?.ToString() ?? _symbol;");
        sb.AppendLine("            if (parameters.TryGetValue(\"TimeframeMinutes\", out var tf))");
        sb.AppendLine("                _timeframeMinutes = Convert.ToInt32(tf);");
        foreach (var input in inputs)
        {
            var fieldName = $"_{char.ToLower(input.Key[0])}{input.Key.Substring(1)}";
            var csharpType = InferCSharpType(input.Value);
            sb.AppendLine($"            if (parameters.TryGetValue(\"{input.Key}\", out var {input.Key.ToLower()}))");
            sb.AppendLine($"                {fieldName} = Convert.To{csharpType}({input.Key.ToLower()});");
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // GenerateSignal method stub
        sb.AppendLine("    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (data == null || data.Length < RequiredCandles)");
        sb.AppendLine("        {");
        sb.AppendLine("            return new TradeSignal");
        sb.AppendLine("            {");
        sb.AppendLine("                Date = currentDate,");
        sb.AppendLine("                Type = SignalType.Hold,");
        sb.AppendLine("                Price = data?.LastOrDefault()?.Close ?? 0,");
        sb.AppendLine("                StrategyName = Name,");
        sb.AppendLine("                Reason = \"Dati insufficienti\"");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var currentPrice = data.Last().Close;");
        sb.AppendLine("        // TODO: Implementare logica della strategia");
        sb.AppendLine("        // Convertire condizioni buy/sellshort in TradeSignal");
        sb.AppendLine();
        sb.AppendLine("        return new TradeSignal");
        sb.AppendLine("        {");
        sb.AppendLine("            Date = currentDate,");
        sb.AppendLine("            Type = SignalType.Hold,");
        sb.AppendLine("            Price = currentPrice,");
        sb.AppendLine("            StrategyName = Name");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    private static string InferCSharpType(string value)
    {
        value = value.Trim();
        if (bool.TryParse(value, out _) || value.Equals("true", StringComparison.OrdinalIgnoreCase) || 
            value.Equals("false", StringComparison.OrdinalIgnoreCase))
            return "bool";
        if (int.TryParse(value, out _))
            return "int";
        if (decimal.TryParse(value, out _))
            return "decimal";
        return "string";
    }
    
    private static string ConvertDefaultValue(string value, string csharpType)
    {
        value = value.Trim();
        return csharpType switch
        {
            "bool" => value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "true" : "false",
            "int" => int.TryParse(value, out var i) ? i.ToString() : "0",
            "decimal" => decimal.TryParse(value, out var d) ? d.ToString() + "m" : "0m",
            _ => $"\"{value}\""
        };
    }
}
