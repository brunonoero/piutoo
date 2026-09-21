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
#
# Con piu' celle serve -Command e non -File: con -File gli argomenti sono stringhe letterali, la
# lista "a,b" arriva come una cella sola e "a b" finisce meta' sul parametro dopo.
#   powershell -NoProfile -ExecutionPolicy Bypass -Command "& 'C:\piootoo-dev\tools\sweep-paniere.ps1' -Celle fdax-1h,nq-30m"
#   powershell ... -Command "& '...\sweep-paniere.ps1' -Beam 1"       # dimezza i tempi, perde interazioni
#   powershell ... -Command "& '...\sweep-paniere.ps1' -SoloStima"    # dice cosa farebbe e si ferma
#
# I resoconti finiscono in piootoo-repository\ricerca\<cella>-ics-ftmo.md, i log accanto.

[CmdletBinding()]
param(
    # In ordine di durata: un errore di impostazione si scopre in venti minuti invece che in due ore.
    [string[]] $Celle = @("fdax-1h", "fdax-4h", "nq-4h", "nq-30m"),
    # Spread per ora e non costante. E' il default perche' una sweep SCEGLIE gli orari: con una
    # costante giornaliera le fasce a spread largo sembrano economiche e la ricerca ci si infila.
    # Misurato il 20/09/2026: FDAX 1,13-1,33 punti di giorno e 2,93-3,33 di notte, e due celle su
    # due avevano scelto proprio le ore notturne.
    [bool] $SpreadPerOra = $true,
    # Criterio di ricerca: "worst-period" giudica una configurazione sul PEGGIORE dei tratti del
    # campione, "net-over-dd" sul totale. Il secondo premiava la fortuna: il 20/09/2026, su quattro
    # celle, le tre bocciate avevano i punteggi in campione piu' ALTI (fino a 26,2 contro l'1,0
    # fuori campione) e l'unica promossa il piu' basso.
    [string] $Criterio = "worst-period",
    [int] $Beam = 2,
    [string] $Da = "2014-07-17",
    [string] $Split = "2022-01-01",
    [string] $A = "2026-09-17",
    [int] $TradeMinimi = 50,
    # I costi veri del broker su cui si opererebbe. Misurati, non ipotizzati:
    #  - spread: dump di tick di PiootooSpreadDumpBot, per ORA (FDAX va da 0,50 a 4,00 punti
    #    secondo l'ora, e una sweep che sceglie gli orari con una media ci si infila);
    #  - swap: scheda del simbolo, verificata su due history di backtest su tick;
    #  - commissione: PER LATO. ICS stampa 38,46 dollari di round turn, quindi qui 19,23.
    #    Passare il round turn raddoppia il costo senza che si veda.
    [string] $BrokerSpread = "ICS",
    [string] $BrokerSwap = "ICS",
    [decimal] $CommissionePerLato = 19.23,
    [switch] $SoloStima
)

$ErrorActionPreference = "Stop"
$radice = Split-Path -Parent $PSScriptRoot
$uscita = Join-Path $radice "piootoo-repository\ricerca"
$exe = Join-Path $radice "Piootoo.Sweep\bin\Release\net8.0\piootoo-sweep.exe"

# Le celle, nell'ordine in cui conviene lanciarle: prima le piu' corte, cosi' i primi risultati
# arrivano presto e un errore di impostazione si scopre in due ore invece che in dieci.
$definizioni = [ordered]@{
    "nq-4h" = @{
        Strategia = "PT2_NQ_PCH_001_240"; Simbolo = "@NQ"; Timeframe = 240; Motore = "PC"
        SpezzaPattern = $false; Stima = "~1,5 ore"
    }
    "fdax-4h" = @{
        Strategia = "PT2_FDAX_PCH_001_240"; Simbolo = "@FDAX"; Timeframe = 240; Motore = "PC"
        SpezzaPattern = $false; Stima = "~2,5 ore"
    }
    "nq-30m"  = @{
        # Col prodotto completo questa cella e' impraticabile, e non per stima ma per misura: il
        # 20/09/2026 la sola fase dei pattern neutrali (3.025 x 2 semi) ha richiesto 2 ore e 22
        # minuti, contro i 10 minuti della stessa fase su una 4h - 88.000 barre in campione invece
        # di 11.500. Con i direzionali, tre volte e mezzo piu' grandi, il totale superava le dieci
        # ore. Fasi spezzate, con il prezzo dichiarato sulle interazioni fra pattern.
        Strategia = "PT2_NQ_PCH_002_30"; Simbolo = "@NQ"; Timeframe = 30; Motore = "PC"
        SpezzaPattern = $true; Stima = "~2 ore, fasi pattern spezzate"
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
$modello = "spread $BrokerSpread" + $(if ($SpreadPerOra) { " PER ORA" } else { " costante" }) +
           ", swap $BrokerSwap, commissione $CommissionePerLato per lato"
Write-Host ("Campione {0} -> {1}, validazione {1} -> {2}, beam {3}, {4}, criterio {5}, feed ICS." -f $Da, $Split, $A, $Beam, $modello, $Criterio)

if ($SoloStima) { return }

# La build va fatta ORA, con nessuna corsa in esecuzione: un piootoo-sweep attivo tiene lockate le
# DLL e la compilazione fallisce a meta'.
Write-Host ""
Write-Host "Compilo..."
& dotnet build (Join-Path $radice "Piootoo.Sweep\Piootoo.Sweep.csproj") -c Release -v quiet
if ($LASTEXITCODE -ne 0) { throw "compilazione fallita: non lancio niente." }

foreach ($cella in $Celle) {
    $d = $definizioni[$cella]

    # Modello di costo e criterio finiscono nel NOME del file: sovrascrivere un resoconto gia' letto
    # con uno prodotto sotto altre ipotesi e' il modo piu' rapido per confrontare due cose diverse
    # credendo di confrontare la stessa.
    $suffisso = "-ics-" + $BrokerSpread.ToLower()
    if ($SpreadPerOra) { $suffisso += "-per-ora" }
    if ($Criterio -eq "worst-period") { $suffisso += "-peggior-tratto" }
    if ($BrokerSwap) { $suffisso += "-swap" }
    $log = Join-Path $uscita "$cella$suffisso.log"
    $md = Join-Path $uscita "$cella$suffisso.md"

    $argomenti = @(
        "--strategy", $d.Strategia, "--symbol", $d.Simbolo, "--timeframe", $d.Timeframe,
        "--engine", $d.Motore, "--broker", "ICS",
        "--spread-broker", $BrokerSpread, "--swap-broker", $BrokerSwap,
        "--commission", $CommissionePerLato.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--from", $Da, "--split", $Split, "--to", $A,
        "--beam", $Beam, "--top", 5, "--min-trades", $TradeMinimi,
        "--objective", $Criterio, "--out", $md
    )
    if ($d.SpezzaPattern) { $argomenti += "--split-pattern-phases" }
    if ($SpreadPerOra) { $argomenti += "--spread-per-hour" }

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
