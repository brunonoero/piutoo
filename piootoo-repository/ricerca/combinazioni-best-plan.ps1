$ws = 'C:\piootoo-dev\piootoo-repository\workspaces'
$bp = @(Invoke-RestMethod http://localhost:5000/api/BestPlans) | ForEach-Object { $_ }
# un backtest per piano: il piu' recente
$plans = $bp | Group-Object PlanCode | ForEach-Object { $_.Group | Sort-Object BacktestFolder | Select-Object -Last 1 }
$info = @{}
foreach ($p in $plans) {
    $mf = (Get-Content "$ws\$($p.WorkspaceId)\masterfilter.json" -Raw | ConvertFrom-Json).StrategiesFilter
    $def = @(Get-Content "$ws\$($p.WorkspaceId)\plans\plans.json" -Raw | ConvertFrom-Json) | ForEach-Object { $_ } | Where-Object Code -eq $p.PlanCode
    $off = @($def.DisabledStrategies)
    $active = @($mf | Where-Object { $off -notcontains $_ } | Sort-Object)
    $trades = @((Get-Content "$ws\$($p.WorkspaceId)\backtests\$($p.BacktestFolder)\trades.json" -Raw | ConvertFrom-Json) | ForEach-Object { $_ })
    $w = if ($def.StrategyWeights) { ($def.StrategyWeights.PSObject.Properties | ForEach-Object { "$($_.Name -replace 'PT5DAV_|PT3B_','')=$($_.Value)" }) -join ' ' } else { '' }
    $info[$p.PlanCode] = [pscustomobject]@{ Code = $p.PlanCode; Ws = $p.WorkspaceId; Active = $active; Trades = $trades; Size = $def.SizeMultiplier; Weights = $w; Folder = $p.BacktestFolder }
}
"== Strategie attive per piano"
foreach ($k in ($info.Keys | Sort-Object)) { $i = $info[$k]; "{0} (ws {1}, size {2}{3}): {4}" -f $k, $i.Ws, $i.Size, $(if ($i.Weights) { ", pesi $($i.Weights)" } else { '' }), (($i.Active | ForEach-Object { $_ -replace 'PT5DAV_|PT3B_','' }) -join ', ') }

# sovrapposizione dei trade: stesso simbolo, stesso lato, ingressi entro 5 minuti
function Overlap($a, $b) {
    if ($a.Count -eq 0) { return 0 }
    $idx = @{}
    foreach ($t in $b) { $key = "$($t.symbol)|$($t.direction)"; if (-not $idx[$key]) { $idx[$key] = New-Object System.Collections.ArrayList }; [void]$idx[$key].Add(([datetime]$t.entryTimeUtc).ToUniversalTime()) }
    $hit = 0
    foreach ($t in $a) {
        $list = $idx["$($t.symbol)|$($t.direction)"]; if (-not $list) { continue }
        $e = ([datetime]$t.entryTimeUtc).ToUniversalTime()
        foreach ($x in $list) { if ([Math]::Abs(($x - $e).TotalMinutes) -le 5) { $hit++; break } }
    }
    return $hit / $a.Count
}

"`n== Coppie: strategie in comune e trade quasi identici (stesso simbolo e lato, ingresso entro 5 minuti)"
$codes = $info.Keys | Sort-Object
$conflict = @{}
for ($i = 0; $i -lt $codes.Count; $i++) { for ($j = $i + 1; $j -lt $codes.Count; $j++) {
    $a = $info[$codes[$i]]; $b = $info[$codes[$j]]
    $common = @($a.Active | Where-Object { $b.Active -contains $_ })
    $ov = [Math]::Max((Overlap $a.Trades $b.Trades), (Overlap $b.Trades $a.Trades))
    $bad = $common.Count -gt 0 -or $ov -gt 0.05
    $conflict["$($codes[$i])|$($codes[$j])"] = $bad
    "{0,-18} x {1,-18} comuni: {2,-60} trade simili: {3:P0} -> {4}" -f $codes[$i], $codes[$j], $(if ($common) { ($common | ForEach-Object { $_ -replace 'PT5DAV_|PT3B_','' }) -join ', ' } else { '-' }), $ov, $(if ($bad) { 'NO' } else { 'ok' })
} }

"`n== Combinazioni massimali di piani compatibili (nessuna coppia in conflitto)"
$n = $codes.Count; $sets = @()
for ($mask = 1; $mask -lt [Math]::Pow(2, $n); $mask++) {
    $s = @(); for ($k = 0; $k -lt $n; $k++) { if ($mask -band (1 -shl $k)) { $s += $codes[$k] } }
    $ok = $true
    for ($x = 0; $x -lt $s.Count -and $ok; $x++) { for ($y = $x + 1; $y -lt $s.Count; $y++) { $key = if ($conflict.ContainsKey("$($s[$x])|$($s[$y])")) { "$($s[$x])|$($s[$y])" } else { "$($s[$y])|$($s[$x])" }; if ($conflict[$key]) { $ok = $false; break } } }
    if ($ok) { $sets += ,$s }
}
$maximal = $sets | Where-Object { $s = $_; -not ($sets | Where-Object { $_.Count -gt $s.Count -and (@($s | Where-Object { $_ -notin $args[0] }).Count -eq 0) }) }
foreach ($s in $sets) {
    $isMax = $true
    foreach ($t in $sets) { if ($t.Count -gt $s.Count -and @($s | Where-Object { $t -notcontains $_ }).Count -eq 0) { $isMax = $false; break } }
    if ($isMax) { "- " + ($s -join ' + ') }
}
