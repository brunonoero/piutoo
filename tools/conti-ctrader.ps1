<#
.SYNOPSIS
  Avvia, ferma e controlla le istanze dei cBot Piootoo che girano con cTrader CLI, una per conto.

.DESCRIPTION
  cTrader Desktop tiene un conto per finestra; per piu' conti dello stesso broker sulla stessa
  macchina si usa ctrader-cli, un processo per istanza, senza interfaccia. Le istanze stanno in
  piootoo-repository\conti\conti.json, cartella esclusa da git (.gitignore): contiene cTrader ID e
  numeri di conto e non va mai committata. La password del cTrader ID resta fuori dal checkout, nel
  profilo utente (passwordFile). Il modello e' tools\conti-ctrader.esempio.json.

  Ogni istanza gira in un cmd nascosto che scrive l'output del cBot in <logDirectory>\<nome>.log;
  il PID sta in <logDirectory>\<nome>.pid.

  Fermare: ctrader-cli esegue il comando "stop" solo nella propria shell interattiva, quindi a
  un'istanza lanciata da qui si manda un Ctrl+C e si aspetta. Se entro -StopTimeoutSeconds non e'
  uscita, la si termina forzatamente: il cBot salva lo stato in piu' punti del run, ma OnStop non
  gira. Lo script dice quale delle due e' successa cercando nel log la riga che OnStop stampa.

  ATTENZIONE: la stessa istanza (bot, piano, conto) non va avviata anche in cTrader Desktop. Due
  bot operativi sullo stesso conto eseguono due volte ogni intent.

.PARAMETER Action
  Start, Stop, Restart, Status (default), DryRun (stampa i comandi senza eseguirli).

.PARAMETER Instance
  Nomi delle istanze su cui agire; di default tutte quelle con enabled diverso da false.

.PARAMETER Bot
  Limita alle istanze di questo cBot (es. PiootooDistributedExecutionBot): e' come aggiorna-cbot.ps1
  riavvia solo le istanze del bot appena ricompilato.

.PARAMETER Config
  Il file di configurazione. Default: piootoo-repository\conti\conti.json nel checkout di questo script.

.PARAMETER Json
  Con Status: scrive su stdout un array JSON con TUTTE le istanze, spente comprese. E' il formato che
  legge la schermata "Istanze cTrader" della console.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\conti-ctrader.ps1 -Action DryRun
.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\conti-ctrader.ps1 -Action Restart -Instance ftmo-1
#>
param(
    [ValidateSet('Start', 'Stop', 'Restart', 'Status', 'DryRun')]
    [string]$Action = 'Status',
    [string[]]$Instance,
    [string]$Bot,
    [string]$Config = (Join-Path $PSScriptRoot '..\piootoo-repository\conti\conti.json'),
    [int]$StopTimeoutSeconds = 30,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'

# La riga che OnStop di PiootooDistributedExecutionBot stampa: se c'e' nel log dopo lo stop, il cBot
# si e' fermato da se' e ha salvato lo stato.
$cleanStopMarker = 'Chiamate al server:'

function Read-Configuration([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Configurazione non trovata: $path. Parti da tools\conti-ctrader.esempio.json."
    }
    $cfg = [IO.File]::ReadAllText($path) | ConvertFrom-Json
    if (-not $cfg.ctid) { throw "ctid mancante in $path" }
    if (-not $cfg.passwordFile -or -not (Test-Path -LiteralPath $cfg.passwordFile)) {
        throw "passwordFile mancante o inesistente in $path"
    }
    if (-not $cfg.instances) { throw "nessuna istanza in $path" }
    $names = @($cfg.instances | ForEach-Object { $_.name })
    $dup = $names | Group-Object | Where-Object { $_.Count -gt 1 }
    if ($dup) { throw "istanze con lo stesso nome: $($dup.Name -join ', ')" }
    foreach ($i in $cfg.instances) {
        foreach ($field in 'name', 'account', 'bot', 'symbol', 'period') {
            if (-not $i.$field) { throw "istanza '$($i.name)': campo $field mancante" }
        }
    }
    # Stesso bot, stesso conto, stesso piano: e' la stessa istanza lanciata due volte.
    $keys = $cfg.instances | Where-Object { $_.enabled -ne $false } |
        ForEach-Object { "$($_.bot)|$($_.account)|$($_.parameters.PlanCode)" } |
        Group-Object | Where-Object { $_.Count -gt 1 }
    if ($keys) { throw "stesso bot sullo stesso conto e piano in piu' istanze: $($keys.Name -join ', ')" }
    return $cfg
}

function Resolve-Cli($cfg) {
    if ($cfg.cliPath) {
        if (-not (Test-Path -LiteralPath $cfg.cliPath)) { throw "cliPath inesistente: $($cfg.cliPath)" }
        return $cfg.cliPath
    }
    $inPath = Get-Command ctrader-cli -ErrorAction SilentlyContinue
    if ($inPath) { return $inPath.Source }
    # Il ctrader-cli.exe nella radice dell'installazione e' il lanciatore che segue gli aggiornamenti
    # di cTrader; quelli dentro app_x.y.z invecchiano con la versione.
    $found = @(Get-ChildItem (Join-Path $env:LOCALAPPDATA 'Spotware\cTrader') -Directory -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName 'ctrader-cli.exe' } | Where-Object { Test-Path -LiteralPath $_ })
    if ($found.Count -eq 1) { return $found[0] }
    if ($found.Count -eq 0) { throw 'ctrader-cli non trovato: installa cTrader o imposta cliPath.' }
    throw "piu' installazioni di cTrader, imposta cliPath: $($found -join '; ')"
}

function Resolve-Algo([string]$bot) {
    $algo = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "cAlgo\Sources\Robots\$bot.algo"
    if (-not (Test-Path -LiteralPath $algo)) { throw "cBot non compilato: $algo (tools\aggiorna-cbot.ps1 -Nome $bot)" }
    return $algo
}

function Format-Argument([string]$value) {
    if ($value -match '[\s"]') { return '"' + ($value -replace '"', '\"') + '"' }
    return $value
}

function Build-Arguments($cfg, $inst, [string]$algo) {
    $list = New-Object System.Collections.Generic.List[string]
    $list.Add('run'); $list.Add((Format-Argument $algo))
    $list.Add('--ctid=' + (Format-Argument $cfg.ctid))
    $list.Add('--pwd-file=' + (Format-Argument $cfg.passwordFile))
    $list.Add("--account=$($inst.account)")
    if ($cfg.broker) { $list.Add('--broker=' + (Format-Argument $cfg.broker)) }
    $list.Add('--symbol=' + (Format-Argument $inst.symbol))
    $list.Add("--period=$($inst.period)")
    # Il bot dichiara AccessRights.FullAccess (HTTP verso il server, stato su disco): senza, il CLI
    # lo avvierebbe con i permessi ristretti.
    if ($inst.fullAccess -ne $false) { $list.Add('--full-access') }
    $list.Add('--exit-on-stop')
    if ($inst.parameters) {
        foreach ($p in $inst.parameters.PSObject.Properties) {
            $list.Add("--$($p.Name)=" + (Format-Argument ([string]$p.Value)))
        }
    }
    return ($list -join ' ')
}

function Get-InstanceProcess([string]$pidFile) {
    if (-not (Test-Path -LiteralPath $pidFile)) { return $null }
    $saved = [IO.File]::ReadAllText($pidFile).Trim() -split '\|'
    $proc = Get-Process -Id ([int]$saved[0]) -ErrorAction SilentlyContinue
    # Un PID riusato da un altro processo non e' la nostra istanza: lo riconosce l'ora di avvio.
    if ($proc -and $proc.StartTime.ToUniversalTime().ToString('o') -eq $saved[1]) { return $proc }
    return $null
}

function Send-CtrlC([int]$processId) {
    # Il Ctrl+C si manda attaccandosi alla console del processo: lo si fa da un powershell a parte,
    # perche' chi si attacca perde la propria console.
    $script = @"
Add-Type -Namespace W -Name K -MemberDefinition '
[DllImport("kernel32.dll")] public static extern bool FreeConsole();
[DllImport("kernel32.dll")] public static extern bool AttachConsole(uint p);
[DllImport("kernel32.dll")] public static extern bool SetConsoleCtrlHandler(System.IntPtr h, bool a);
[DllImport("kernel32.dll")] public static extern bool GenerateConsoleCtrlEvent(uint e, uint g);'
[W.K]::FreeConsole() | Out-Null
if (-not [W.K]::AttachConsole($processId)) { exit 1 }
[W.K]::SetConsoleCtrlHandler([IntPtr]::Zero, `$true) | Out-Null
[W.K]::GenerateConsoleCtrlEvent(0, 0) | Out-Null
Start-Sleep -Milliseconds 500
[W.K]::FreeConsole() | Out-Null
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    $p = Start-Process powershell.exe -ArgumentList '-NoProfile', '-EncodedCommand', $encoded -WindowStyle Hidden -PassThru -Wait
    return $p.ExitCode -eq 0
}

function Start-Instance($cfg, $inst, [string]$cli, [string]$logDir) {
    $pidFile = Join-Path $logDir "$($inst.name).pid"
    if (Get-InstanceProcess $pidFile) { Write-Host "[$($inst.name)] gia' in esecuzione"; return }
    $algo = Resolve-Algo $inst.bot
    $arguments = Build-Arguments $cfg $inst $algo
    $log = Join-Path $logDir "$($inst.name).log"
    Add-Content -LiteralPath $log -Encoding UTF8 -Value ("`r`n==== avvio {0:yyyy-MM-dd HH:mm:ss}Z conto {1} ====" -f (Get-Date).ToUniversalTime(), $inst.account)
    $command = "`"`"$cli`" $arguments >> `"$log`" 2>&1`""
    $proc = Start-Process cmd.exe -ArgumentList '/c', $command -WindowStyle Hidden -PassThru
    [IO.File]::WriteAllText($pidFile, "$($proc.Id)|$($proc.StartTime.ToUniversalTime().ToString('o'))")
    Start-Sleep -Seconds 5
    if ($proc.HasExited) {
        Write-Warning "[$($inst.name)] uscito subito (codice $($proc.ExitCode)): leggi $log"
        Remove-Item -LiteralPath $pidFile -ErrorAction SilentlyContinue
        return
    }
    Write-Host "[$($inst.name)] avviato, PID $($proc.Id), log $log"
}

function Stop-Instance($inst, [string]$logDir) {
    $pidFile = Join-Path $logDir "$($inst.name).pid"
    $log = Join-Path $logDir "$($inst.name).log"
    $proc = Get-InstanceProcess $pidFile
    if (-not $proc) {
        Write-Host "[$($inst.name)] non in esecuzione"
        Remove-Item -LiteralPath $pidFile -ErrorAction SilentlyContinue
        return
    }
    $logLength = if (Test-Path -LiteralPath $log) { (Get-Item -LiteralPath $log).Length } else { 0 }
    $sent = Send-CtrlC $proc.Id
    if (-not $sent) { Write-Warning "[$($inst.name)] Ctrl+C non recapitato" }
    if (-not $proc.WaitForExit($StopTimeoutSeconds * 1000)) {
        Write-Warning "[$($inst.name)] ancora vivo dopo $StopTimeoutSeconds s: chiusura forzata, OnStop non gira"
        & taskkill.exe /PID $proc.Id /T /F | Out-Null
    }
    Remove-Item -LiteralPath $pidFile -ErrorAction SilentlyContinue

    $tail = ''
    if (Test-Path -LiteralPath $log) {
        $stream = [IO.File]::Open($log, 'Open', 'Read', 'ReadWrite')
        try {
            [void]$stream.Seek([Math]::Min($logLength, $stream.Length), 'Begin')
            $tail = (New-Object IO.StreamReader($stream)).ReadToEnd()
        }
        finally { $stream.Dispose() }
    }
    if ($tail.Contains($cleanStopMarker)) { Write-Host "[$($inst.name)] fermato, OnStop eseguito" }
    else { Write-Warning "[$($inst.name)] fermato, ma nel log non c'e' '$cleanStopMarker': OnStop probabilmente non e' girato" }
}

$cfg = Read-Configuration $Config
$logDir = if ($cfg.logDirectory) { $cfg.logDirectory } else { Join-Path (Split-Path -Parent $Config) 'log' }
New-Item -ItemType Directory -Force $logDir | Out-Null

if ($Action -eq 'Status' -and $Json) {
    $rows = foreach ($inst in $cfg.instances) {
        $proc = Get-InstanceProcess (Join-Path $logDir "$($inst.name).pid")
        [pscustomobject]@{
            name         = $inst.name
            account      = [string]$inst.account
            bot          = $inst.bot
            planCode     = if ($inst.parameters -and $inst.parameters.PlanCode) { [string]$inst.parameters.PlanCode } else { $null }
            enabled      = $inst.enabled -ne $false
            running      = $null -ne $proc
            processId    = if ($proc) { $proc.Id } else { $null }
            startedAtUtc = if ($proc) { $proc.StartTime.ToUniversalTime().ToString('o') } else { $null }
            logPath      = Join-Path $logDir "$($inst.name).log"
        }
    }
    # -InputObject e @(): con una sola istanza ConvertTo-Json scriverebbe un oggetto, non un array.
    ConvertTo-Json -InputObject @($rows) -Depth 3
    return
}

$selected = @($cfg.instances | Where-Object { $_.enabled -ne $false })
if ($Instance) {
    $unknown = $Instance | Where-Object { $_ -notin @($cfg.instances | ForEach-Object { $_.name }) }
    if ($unknown) { throw "istanze sconosciute: $($unknown -join ', ')" }
    $selected = @($cfg.instances | Where-Object { $_.name -in $Instance })
}
if ($Bot) { $selected = @($selected | Where-Object { $_.bot -eq $Bot }) }
if ($selected.Count -eq 0) { Write-Host 'nessuna istanza selezionata'; return }

switch ($Action) {
    'Status' {
        foreach ($inst in $selected) {
            $proc = Get-InstanceProcess (Join-Path $logDir "$($inst.name).pid")
            $state = if ($proc) { "in esecuzione dal $($proc.StartTime.ToString('yyyy-MM-dd HH:mm')), PID $($proc.Id)" } else { 'fermo' }
            Write-Host ("{0,-20} conto {1,-10} {2,-32} {3}" -f $inst.name, $inst.account, $inst.bot, $state)
        }
    }
    'DryRun' {
        $cli = Resolve-Cli $cfg
        foreach ($inst in $selected) {
            $algo = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "cAlgo\Sources\Robots\$($inst.bot).algo"
            Write-Host "[$($inst.name)]"
            Write-Host "  `"$cli`" $(Build-Arguments $cfg $inst $algo)"
        }
    }
    'Start' {
        $cli = Resolve-Cli $cfg
        foreach ($inst in $selected) { Start-Instance $cfg $inst $cli $logDir }
    }
    'Stop' {
        foreach ($inst in $selected) { Stop-Instance $inst $logDir }
    }
    'Restart' {
        $cli = Resolve-Cli $cfg
        foreach ($inst in $selected) {
            Stop-Instance $inst $logDir
            Start-Instance $cfg $inst $cli $logDir
        }
    }
}
