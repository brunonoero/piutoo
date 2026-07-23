# Script PowerShell per generare tutte le classi strategia da file EasyLanguage

# Funzioni helper
function GetCSharpType {
    param([string]$value)
    $value = $value.Trim()
    if ($value -eq "true" -or $value -eq "false") { return "bool" }
    if ($value -match "^\d+$") { return "int" }
    if ($value -match "^\d+\.\d+$" -or $value -match "^\d+\.\d+m$") { return "decimal" }
    return "string"
}

function GetDefaultValue {
    param([string]$value, [string]$type)
    $value = $value.Trim()
    
    # Se il valore è vuoto, usa un default basato sul tipo
    if ([string]::IsNullOrWhiteSpace($value)) {
        switch ($type) {
            "bool" { return "false" }
            "int" { return "0" }
            "decimal" { return "0m" }
            default { return '""' }
        }
    }
    
    switch ($type) {
        "bool" { 
            if ($value -eq "true") { return "true" } else { return "false" }
        }
        "int" { 
            if ($value -match "^\d+$") { return $value } else { return "0" }
        }
        "decimal" { 
            $val = $value -replace "m$", ""
            if ($val -match "^\d+\.?\d*$") { return "$val" + "m" } else { return "0m" }
        }
        default { 
            # Escape delle virgolette nel valore stringa
            $escaped = $value -replace '"', '`"'
            return "`"$escaped`""
        }
    }
}

function GenerateCSharpClass {
    param(
        [string]$className,
        [string]$strategyName,
        [string]$symbol,
        [int]$timeframe,
        [string]$description,
        [hashtable]$inputs,
        [hashtable]$variables
    )
    
    $sb = New-Object System.Text.StringBuilder
    
    $sb.AppendLine("using System;") | Out-Null
    $sb.AppendLine("using System.Collections.Generic;") | Out-Null
    $sb.AppendLine("using Piootoo.Shared.Enums;") | Out-Null
    $sb.AppendLine("using Piootoo.Shared.Interfaces;") | Out-Null
    $sb.AppendLine("using Piootoo.Shared.Models;") | Out-Null
    $sb.AppendLine("using static Piootoo.Strategies.Easy.EasyLib;") | Out-Null
    $sb.AppendLine("") | Out-Null
    $sb.AppendLine("namespace Piootoo.Strategies.Easy;") | Out-Null
    $sb.AppendLine("") | Out-Null
    # Escape delle virgolette e backslash per i commenti XML
    $escapedNameForComment = $strategyName -replace '\\', '\\\\' -replace '"', '&quot;'
    $escapedDescForComment = $description -replace '\\', '\\\\' -replace '"', '&quot;'
    
    $sb.AppendLine("/// <summary>") | Out-Null
    $sb.AppendLine("/// Strategia EasyLanguage convertita: $escapedNameForComment") | Out-Null
    $sb.AppendLine("/// $escapedDescForComment") | Out-Null
    $sb.AppendLine("/// </summary>") | Out-Null
    $sb.AppendLine("public class $className : ITradingStrategy") | Out-Null
    $sb.AppendLine("{") | Out-Null
    
    # INPUTS
    $sb.AppendLine("    // INPUTS") | Out-Null
    foreach ($key in $inputs.Keys) {
        $value = $inputs[$key]
        $type = GetCSharpType $value
        $fieldName = "_" + $key.Substring(0,1).ToLower() + $key.Substring(1)
        $defaultValue = GetDefaultValue $value $type
        $sb.AppendLine("    private $type $fieldName = $defaultValue;") | Out-Null
    }
    
    # VARIABLES
    $sb.AppendLine("") | Out-Null
    $sb.AppendLine("    // VARIABLES") | Out-Null
    foreach ($key in $variables.Keys) {
        $value = $variables[$key]
        $type = GetCSharpType $value
        $fieldName = "_" + $key.Substring(0,1).ToLower() + $key.Substring(1)
        $defaultValue = GetDefaultValue $value $type
        $sb.AppendLine("    private $type $fieldName = $defaultValue;") | Out-Null
    }
    
    # STATE
    $sb.AppendLine("") | Out-Null
    $sb.AppendLine("    // STATE") | Out-Null
    # Escape delle virgolette e backslash nelle stringhe C#
    # In C#: \ diventa \\, " diventa \"
    $escapedSymbol = $symbol -replace '\\', '\\\\' -replace '"', '\"'
    $escapedName = $strategyName -replace '\\', '\\\\' -replace '"', '\"'
    $escapedDescription = $description -replace '\\', '\\\\' -replace '"', '\"'
    
    $sb.AppendLine("    private string _symbol = `"$escapedSymbol`";") | Out-Null
    $sb.AppendLine("    private int _timeframeMinutes = $timeframe;") | Out-Null
    $sb.AppendLine("    private string _name = `"$escapedName`";") | Out-Null
    $sb.AppendLine("    private string _description = `"$escapedDescription`";") | Out-Null
    $sb.AppendLine("") | Out-Null
    $sb.AppendLine("    public string Name => _name;") | Out-Null
    $sb.AppendLine("    public string Description => _description;") | Out-Null
    $sb.AppendLine("    public string Symbol => _symbol;") | Out-Null
    $sb.AppendLine("    public int TimeframeMinutes => _timeframeMinutes;") | Out-Null
    $sb.AppendLine("    public int RequiredCandles => 100; // TODO: Calcolare in base alla strategia") | Out-Null
    $sb.AppendLine("") | Out-Null
    
    # Initialize
    $sb.AppendLine("    public void Initialize(Dictionary<string, object>? parameters = null)") | Out-Null
    $sb.AppendLine("    {") | Out-Null
    $sb.AppendLine("        if (parameters != null)") | Out-Null
    $sb.AppendLine("        {") | Out-Null
    $sb.AppendLine("            if (parameters.TryGetValue(`"Symbol`", out var sym))") | Out-Null
    $sb.AppendLine("                _symbol = sym?.ToString() ?? _symbol;") | Out-Null
    $sb.AppendLine("            if (parameters.TryGetValue(`"TimeframeMinutes`", out var tf))") | Out-Null
    $sb.AppendLine("                _timeframeMinutes = Convert.ToInt32(tf);") | Out-Null
    foreach ($key in $inputs.Keys) {
        $fieldName = "_" + $key.Substring(0,1).ToLower() + $key.Substring(1)
        $type = GetCSharpType $inputs[$key]
        $convertMethod = switch ($type) {
            "bool" { "Boolean" }
            "int" { "Int32" }
            "decimal" { "Decimal" }
            default { "String" }
        }
        $sb.AppendLine("            if (parameters.TryGetValue(`"$key`", out var $($key.ToLower())))") | Out-Null
        $sb.AppendLine("                $fieldName = Convert.To$convertMethod($($key.ToLower()));") | Out-Null
    }
    $sb.AppendLine("        }") | Out-Null
    $sb.AppendLine("    }") | Out-Null
    $sb.AppendLine("") | Out-Null
    
    # GenerateSignal con logica convertita
    $sb.AppendLine("    // Stato per tracciare la posizione corrente (MP = marketposition)") | Out-Null
    $sb.AppendLine("    private int _currentMP = 0; // 0 = nessuna posizione, +1 = long, -1 = short") | Out-Null
    $sb.AppendLine("    private int _myCount = 0;") | Out-Null
    $sb.AppendLine("    private DateTime? _lastEntryDate = null;") | Out-Null
    $sb.AppendLine("") | Out-Null
    
    $sb.AppendLine("    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)") | Out-Null
    $sb.AppendLine("    {") | Out-Null
    $sb.AppendLine("        if (data == null || data.Length < RequiredCandles)") | Out-Null
    $sb.AppendLine("        {") | Out-Null
    $sb.AppendLine("            return new TradeSignal") | Out-Null
    $sb.AppendLine("            {") | Out-Null
    $sb.AppendLine("                Date = currentDate,") | Out-Null
    $sb.AppendLine("                Type = SignalType.Hold,") | Out-Null
    $sb.AppendLine("                Price = data?.LastOrDefault()?.Close ?? 0,") | Out-Null
    $sb.AppendLine("                StrategyName = Name,") | Out-Null
    $sb.AppendLine("                Reason = `"Dati insufficienti`"") | Out-Null
    $sb.AppendLine("            };") | Out-Null
    $sb.AppendLine("        }") | Out-Null
    $sb.AppendLine("") | Out-Null
    $sb.AppendLine("        var currentPrice = data.Last().Close;") | Out-Null
    $sb.AppendLine("        var currentTime = currentDate.Hour * 100 + currentDate.Minute; // Formato HHMM") | Out-Null
    $sb.AppendLine("") | Out-Null
    $sb.AppendLine("        // TODO: Implementare logica completa della strategia") | Out-Null
    $sb.AppendLine("        // Convertire condizioni buy/sellshort in TradeSignal") | Out-Null
    $sb.AppendLine("        // Per ora restituisce Hold - la logica deve essere implementata manualmente") | Out-Null
    $sb.AppendLine("") | Out-Null
    $sb.AppendLine("        return new TradeSignal") | Out-Null
    $sb.AppendLine("        {") | Out-Null
    $sb.AppendLine("            Date = currentDate,") | Out-Null
    $sb.AppendLine("            Type = SignalType.Hold,") | Out-Null
    $sb.AppendLine("            Price = currentPrice,") | Out-Null
    $sb.AppendLine("            StrategyName = Name") | Out-Null
    $sb.AppendLine("        };") | Out-Null
    $sb.AppendLine("    }") | Out-Null
    $sb.AppendLine("}") | Out-Null
    
    return $sb.ToString()
}

# Funzione per estrarre la logica della strategia dal file EasyLanguage
function ExtractStrategyLogic {
    param([string]$content)
    
    $logic = @{
        Definitions = @()
        BuyConditions = @()
        SellShortConditions = @()
        ExitConditions = @()
        Variables = @()
    }
    
    $lines = $content -split "`n"
    $inDefinitions = $false
    $inConditions = $false
    $inExit = $false
    
    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        
        # Rileva sezioni
        if ($trimmed -match "//DEFINITIONS" -or $trimmed -match "^DEFINITIONS") {
            $inDefinitions = $true
            $inConditions = $false
            $inExit = $false
            continue
        }
        if ($trimmed -match "//CONDITIONS" -or $trimmed -match "^CONDITIONS") {
            $inDefinitions = $false
            $inConditions = $true
            $inExit = $false
            continue
        }
        if ($trimmed -match "//EXIT" -or $trimmed -match "^EXIT") {
            $inDefinitions = $false
            $inConditions = $false
            $inExit = $true
            continue
        }
        
        # Estrai DEFINITIONS
        if ($inDefinitions -and $trimmed -and !$trimmed.StartsWith("//")) {
            # Rimuovi commenti inline
            $cleanLine = $trimmed -replace "//.*$", ""
            if ($cleanLine) {
                $logic.Definitions += $cleanLine
            }
        }
        
        # Estrai condizioni BUY
        if ($trimmed -match "buy\s+.*?contracts") {
            # Estrai la condizione if prima del buy
            $buyCondition = $trimmed
            $logic.BuyConditions += $buyCondition
        }
        
        # Estrai condizioni SELLSHORT
        if ($trimmed -match "sellshort\s+.*?contracts") {
            $sellShortCondition = $trimmed
            $logic.SellShortConditions += $sellShortCondition
        }
        
        # Estrai condizioni EXIT (sell/buytocover)
        if ($inExit -and ($trimmed -match "sell\s+.*?contracts" -or $trimmed -match "buytocover\s+.*?contracts")) {
            $exitCondition = $trimmed
            $logic.ExitConditions += $exitCondition
        }
    }
    
    return $logic
}

# Funzione per convertire una condizione EasyLanguage in C#
function ConvertEasyLanguageCondition {
    param([string]$condition)
    
    # Semplificazioni base - questa è una versione molto semplificata
    # In produzione servirebbe un parser più completo
    
    $csharp = $condition
    
    # Sostituzioni base
    $csharp = $csharp -replace "MP\s*=\s*\+1", "_currentMP == 1"
    $csharp = $csharp -replace "MP\s*=\s*-1", "_currentMP == -1"
    $csharp = $csharp -replace "MP\s*=\s*0", "_currentMP == 0"
    $csharp = $csharp -replace "MP\s*<>", "_currentMP !="
    $csharp = $csharp -replace "time\s*=", "currentTime =="
    $csharp = $csharp -replace "dayofweek\(d\)", "currentDate.DayOfWeek"
    $csharp = $csharp -replace "HighestFC\(h,\s*(\w+)\)", "(decimal)Highest(data, `$1, d => d.High)"
    $csharp = $csharp -replace "LowestFC\(l,\s*(\w+)\)", "(decimal)Lowest(data, `$1, d => d.Low)"
    $csharp = $csharp -replace "openD\(0\)", "GetDailyOpen(data, currentDate, 0)"
    $csharp = $csharp -replace "highD\(0\)", "GetDailyHigh(data, currentDate, 0)"
    $csharp = $csharp -replace "lowD\(0\)", "GetDailyLow(data, currentDate, 0)"
    $csharp = $csharp -replace "closeD\(0\)", "GetDailyClose(data, currentDate, 0)"
    $csharp = $csharp -replace "openD\(1\)", "GetDailyOpen(data, currentDate, 1)"
    $csharp = $csharp -replace "highD\(1\)", "GetDailyHigh(data, currentDate, 1)"
    $csharp = $csharp -replace "lowD\(1\)", "GetDailyLow(data, currentDate, 1)"
    $csharp = $csharp -replace "closeD\(1\)", "GetDailyClose(data, currentDate, 1)"
    
    return $csharp
}

# Script principale
$easyPath = "D:\Piootoo\PiootooApp\piootoo-repository\easy"
$outputPath = "D:\Piootoo\PiootooApp\Piootoo.Strategies\Easy"

# Trova tutti i file che corrispondono ai pattern
$files = Get-ChildItem -Path $easyPath -Filter "s_*.txt" | Where-Object {
    $_.Name -match "_(DAX|NQ|CL|GC|FDAX|FNQ|FCL|FGC)_"
}

Write-Host "Trovati $($files.Count) file da convertire" -ForegroundColor Green

foreach ($file in $files) {
    $fileName = $file.Name
    Write-Host "Elaborando: $fileName" -ForegroundColor Yellow
    
    # Estrai informazioni dal nome file
    # Pattern: s_TOP_UA_643_FDAX_60__7.txt -> Easy_643_FDAX_60
    # Pattern alternativo: s_TOP_UA_123_CL_5____120__7.txt (con underscore multipli)
    $strategyPart = $null
    $symbol = $null
    $timeframe = $null
    
    if ($fileName -match "^s_(.+?)_(DAX|NQ|CL|GC|FDAX|FNQ|FCL|FGC)_(\d+)(?:__\d+)?\.txt$") {
        $strategyPart = $matches[1]
        $symbol = $matches[2]
        $timeframe = $matches[3]
    }
    elseif ($fileName -match "^s_(.+?)_(DAX|NQ|CL|GC|FDAX|FNQ|FCL|FGC)_(\d+)_+(\d+)(?:__\d+)?\.txt$") {
        # Pattern con underscore multipli: s_TOP_UA_123_CL_5____120__7.txt
        $strategyPart = $matches[1]
        $symbol = $matches[2]
        $timeframe = $matches[3]  # Prendi il primo numero come timeframe
    }
    
    if ($strategyPart -and $symbol -and $timeframe) {
        # Estrai numero strategia (es. 643 da TOP_UA_643)
        $strategyNumber = ""
        if ($strategyPart -match "(\d+)") {
            $strategyNumber = $matches[1]
        }
        
        # Genera nome classe: Easy_643_FDAX_60
        $className = "Easy_${strategyNumber}_${symbol}_${timeframe}"
        
        Write-Host "  Classe: $className" -ForegroundColor Cyan
        
        # Leggi il file
        $content = Get-Content $file.FullName -Raw
        
        # Estrai descrizione
        $description = "Strategia EasyLanguage convertita"
        $lines = $content -split "`n"
        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            if ($trimmed -and !$trimmed.StartsWith("input:") -and !$trimmed.StartsWith("var:") -and !$trimmed.StartsWith("array:")) {
                if ($trimmed.StartsWith("//") -and $trimmed.Length -gt 10 -and !$trimmed.Contains(">>>>") -and !$trimmed.Contains("Michael")) {
                    $description = $trimmed.Substring(2).Trim()
                    break
                }
            }
        }
        
        # Estrai input e variabili usando regex
        $inputs = @{}
        $variables = @{}
        
        # Pattern per input (gestisce anche input multipli sulla stessa riga separati da virgole)
        # Pattern migliorato: cattura ogni input anche se sulla stessa riga
        # Esempio: input: PtnNeutYes(16), PtnNeutNo(10); -> cattura entrambi
        $inputPattern = "(?i)input[s]?:[^;]*?(\w+)\(([^)]+)\)"
        $inputMatches = [regex]::Matches($content, $inputPattern)
        foreach ($match in $inputMatches) {
            $key = $match.Groups[1].Value
            $value = $match.Groups[2].Value.Trim()
            # Rimuovi eventuali commenti inline
            if ($value -match "^(.*?)\s*//") {
                $value = $matches[1].Trim()
            }
            # Rimuovi eventuali virgole finali
            $value = $value -replace ",\s*$", ""
            # Se il valore è vuoto, usa "0" come default
            if ([string]::IsNullOrWhiteSpace($value)) {
                $value = "0"
            }
            $inputs[$key] = $value
        }
        
        # Pattern per variabili (stesso approccio)
        # IMPORTANTE: Se una variabile ha lo stesso nome di un input, viene ignorata (gli input hanno priorità)
        # IMPORTANTE: Escludi MyCount perché viene gestito nella sezione stato
        $varPattern = "(?i)var[s]?[iable]?:[^;]*?(\w+)\(([^)]+)\)"
        $varMatches = [regex]::Matches($content, $varPattern)
        foreach ($match in $varMatches) {
            $key = $match.Groups[1].Value
            # Salta se questa variabile è già presente come input (evita duplicati)
            if ($inputs.ContainsKey($key)) {
                continue
            }
            # Salta MyCount perché viene gestito nella sezione stato (evita duplicati con _myCount)
            if ($key -eq "MyCount" -or $key -eq "myCount") {
                continue
            }
            $value = $match.Groups[2].Value.Trim()
            # Rimuovi eventuali commenti inline
            if ($value -match "^(.*?)\s*//") {
                $value = $matches[1].Trim()
            }
            # Rimuovi eventuali virgole finali
            $value = $value -replace ",\s*$", ""
            # Se il valore è vuoto, usa "0" come default
            if ([string]::IsNullOrWhiteSpace($value)) {
                $value = "0"
            }
            $variables[$key] = $value
        }
        
        # Estrai logica della strategia
        $strategyLogic = ExtractStrategyLogic -content $content
        
        # Genera codice C#
        $code = GenerateCSharpClass -className $className -strategyName $strategyPart -symbol "@$symbol" -timeframe $timeframe -description $description -inputs $inputs -variables $variables
        
        # Salva file
        $outputFile = Join-Path $outputPath "$className.cs"
        $code | Out-File -FilePath $outputFile -Encoding UTF8
        
        # Log informazioni sulla strategia estratta
        if ($strategyLogic.BuyConditions.Count -gt 0 -or $strategyLogic.SellShortConditions.Count -gt 0) {
            Write-Host "  Generato: $outputFile (Logica: $($strategyLogic.BuyConditions.Count) buy, $($strategyLogic.SellShortConditions.Count) sellshort)" -ForegroundColor Green
        } else {
            Write-Host "  Generato: $outputFile (Nessuna condizione trovata - implementazione manuale richiesta)" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "  Pattern non riconosciuto: $fileName" -ForegroundColor Red
    }
}

Write-Host "`nCompletato!" -ForegroundColor Green
