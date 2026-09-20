# Lancia in sequenza la ricerca sulle celle del paniere, su feed ICS e spread FTMO.
#
# In SEQUENZA e non in parallelo: ogni corsa usa gia' tutti i core, e farne girare due insieme
# raddoppia il tempo di entrambe invece di dimezzarlo.
#
# NB: niente caratteri fuori ASCII in questo file. PowerShell 5.1 legge uno script senza BOM come
# ANSI, e un trattino lungo o una lettera accentata rompono il parser con un errore che indica la
# riga sbagliata.
#
# Uso:
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\sweep-paniere.ps1
#   powershell ... -File tools\sweep-paniere.ps1 -Celle fdax-4h,nq-30m
#   powershell ... -File tools\sweep-paniere.ps1 -Beam 1        # dimezza i tempi, perde interazioni
#   powershell ... -File tools\sweep-paniere.ps1 -SoloStima     # dice cosa farebbe e si ferma
#
# I resoconti finiscono in piootoo-repository\ricerca\<cella>-ics-ftmo.md, i log accanto.

[CmdletBinding()]
param(
    [string[]] $Celle = @("fdax-4h", "nq-30m", "fdax-1h"),
    [int] $Beam = 2,
    [string] $Da = "2014-07-17",
    [string] $Split = "2022-01-01",
    [string] $A = "2026-09-17",
    [int] $TradeMinimi = 50,
    [switch] $SoloStima
)

$ErrorActionPreference = "Stop"
$radice = Split-Path -Parent $PSScriptRoot
$uscita = Join-Path $radice "piootoo-repository\ricerca"
$exe = Join-Path $radice "Piootoo.Sweep\bin\Release\net8.0\piootoo-sweep.exe"

# Le celle, nell'ordine in cui conviene lanciarle: prima le piu' corte, cosi' i primi risultati
# arrivano presto e un errore di impostazione si scopre in due ore invece che in dieci.
$definizioni = [ordered]@{
    "fdax-4h" = @{
        Strategia = "PT2_FDAX_PCH_001_240"; Simbolo = "@FDAX"; Timeframe = 240; Motore = "PC"
        SpezzaPattern = $false; Stima = "~2,5 ore"
    }
    "nq-30m"  = @{
        Strategia = "PT2_NQ_PCH_002_30"; Simbolo = "@NQ"; Timeframe = 30; Motore = "PC"
        SpezzaPattern = $false; Stima = "~7,5 ore"
    }
    "fdax-1h" = @{
        # Le due fasi pattern del BIASW sono 153 x 152 combinazioni ciascuna: col prodotto completo
        # questa cella da sola supera le nove ore. Spezzate diventano minuti, al prezzo dichiarato:
        # una coppia richiesto+vietato che rende solo insieme non e' piu' raggiungibile.
        Strategia = "PT2_FDAX_BSW_001_60"; Simbolo = "@FDAX"; Timeframe = 60; Motore = "BIASW"
        SpezzaPattern = $true; Stima = "~1 ora, fasi pattern spezzate"
    }
}

New-Item -ItemType Directory -Force $uscita | Out-Null

Write-Host "Celle da lanciare, in ordine:"
foreach ($cella in $Celle) {
    if (-not $definizioni.Contains($cella)) {
        throw "cella sconosciuta: $cella (note: $($definizioni.Keys -join ', '))"
    }
    $d = $definizioni[$cella]
    Write-Host ("  {0,-8} {1,-22} {2,-6} {3,4}m  {4}" -f $cella, $d.Strategia, $d.Motore, $d.Timeframe, $d.Stima)
}
Write-Host ("Campione {0} -> {1}, validazione {1} -> {2}, beam {3}, spread FTMOPLATFORM, feed ICS." -f $Da, $Split, $A, $Beam)

if ($SoloStima) { return }

# La build va fatta ORA, con nessuna corsa in esecuzione: un piootoo-sweep attivo tiene lockate le
# DLL e la compilazione fallisce a meta'.
Write-Host ""
Write-Host "Compilo..."
& dotnet build (Join-Path $radice "Piootoo.Sweep\Piootoo.Sweep.csproj") -c Release -v quiet
if ($LASTEXITCODE -ne 0) { throw "compilazione fallita: non lancio niente." }

foreach ($cella in $Celle) {
    $d = $definizioni[$cella]
    $log = Join-Path $uscita "$cella-ics-ftmo.log"
    $md = Join-Path $uscita "$cella-ics-ftmo.md"

    $argomenti = @(
        "--strategy", $d.Strategia, "--symbol", $d.Simbolo, "--timeframe", $d.Timeframe,
        "--engine", $d.Motore, "--broker", "ICS", "--spread-broker", "FTMOPLATFORM",
        "--from", $Da, "--split", $Split, "--to", $A,
        "--beam", $Beam, "--top", 5, "--min-trades", $TradeMinimi, "--out", $md
    )
    if ($d.SpezzaPattern) { $argomenti += "--split-pattern-phases" }

    Write-Host ""
    Write-Host ("=== {0} ({1}) - avvio {2}" -f $cella, $d.Stima, (Get-Date -Format "HH:mm"))
    $inizio = Get-Date
    & $exe @argomenti *>&1 | Tee-Object -FilePath $log
    $esito = $LASTEXITCODE
    $durata = (Get-Date) - $inizio

    $verdetto = switch ($esito) {
        0 { "una finalista sopravvive al fuori campione" }
        1 { "nessuna finalista sopravvive (esito legittimo, non un errore)" }
        default { "ERRORE (codice $esito)" }
    }
    Write-Host ("=== {0} finita in {1:N1} minuti: {2}" -f $cella, $durata.TotalMinutes, $verdetto)
}

Write-Host ""
Write-Host "Resoconti in $uscita"
