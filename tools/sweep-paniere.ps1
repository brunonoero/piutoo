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
    # Resta la sola fdax-4h: le altre tre celle hanno perso la classe di partenza con la rimozione
    # della serie PT2 il 22/09/2026, e stanno commentate piu' sotto con la loro taratura.
    [string[]] $Celle = @("fdax-4h"),
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
    # Due soglie di AMMISSIBILITA', non di punteggio: sotto, la configurazione non entra in
    # classifica. Il punteggio guarda utile contro drawdown e non sa niente del margine su ogni
    # trade: un profit factor di 1,01 puo' arrivare in cima a una fase, e quel margine lo mangia il
    # primo costo dimenticato - la fee di conversione dello 0,70% di FTMO, per dirne uno noto.
    # L'utile medio va tarato sul COSTO per trade: su ICS/DE40 sono ~50 dollari fra commissione e
    # spread, quindi 150 chiede tre volte il costo.
    [decimal] $ProfitFactorMinimo = 1.25,
    [decimal] $UtileMedioMinimo = 150,
    # I costi veri del broker su cui si opererebbe. Misurati, non ipotizzati:
    #  - spread: dump di tick di PiootooSpreadDumpBot, per ORA (FDAX va da 0,50 a 4,00 punti
    #    secondo l'ora, e una sweep che sceglie gli orari con una media ci si infila);
    #  - swap: scheda del simbolo, verificata su due history di backtest su tick;
    #  - commissione: PER LATO. ICS stampa 38,46 dollari di round turn, quindi qui 19,23.
    #    Passare il round turn raddoppia il costo senza che si veda.
    # Piu' broker separati da virgola: la ricerca paga il costo PEGGIORE voce per voce. Non esiste
    # il broker piu' caro - su @FDAX, FTMO costa di piu' sullo spread (1,23 contro 0,50) e ICS sullo
    # swap (5,05 contro 4,53 sul long) - quindi sceglierne uno lascerebbe fuori meta' del costo.
    # Cosi' una configurazione che sopravvive rende di piu' del previsto dove si opera, mai di meno.
    [string] $BrokerSpread = "ICS,FTMOPLATFORM",
    [string] $BrokerSwap = "ICS,FTMO",
    [decimal] $CommissionePerLato = 19.23,
    # Tenuta con cui la strategia verra' operata. Vuoto = overnight e overweek liberi (parita' con
    # il motore di ricerca). "20:45" = la ricerca gira come il piano che vieta l'overnight: deadline
    # al flat e nessun ingresso nella finestra [flat, flat + FinestraFlat minuti), che deve coprire il
    # rollover del broker (20:59 FTMO, 21:00 ICS). Cercare con una tenuta e operare con un'altra
    # valida una strategia diversa da quella che si opera.
    [string] $FlatUtc = "",
    [int] $FinestraFlat = 30,
    [switch] $SoloStima
)

$ErrorActionPreference = "Stop"
$radice = Split-Path -Parent $PSScriptRoot
$uscita = Join-Path $radice "piootoo-repository\ricerca"
$exe = Join-Path $radice "Piootoo.Sweep\bin\Release\net8.0\piootoo-sweep.exe"

# Le celle, nell'ordine in cui conviene lanciarle: prima le piu' corte, cosi' i primi risultati
# arrivano presto e un errore di impostazione si scopre in due ore invece che in dieci.
#
# LA CLASSE DI PARTENZA NON E' UN DETTAGLIO: la sweep la istanzia e le sovrascrive i parametri, ma
# eredita cio' che i parametri NON coprono - l'etichetta della barra, il fuso della finestra, il
# tick. Deve quindi essere del simbolo e del timeframe della cella.
#
# Il 22/09/2026 la serie PT2 e' stata rimossa dal progetto e con lei le classi di partenza di tre
# celle su quattro: nq-4h (PT2_NQ_PCH_001_240), nq-30m (PT2_NQ_PCH_002_30) e fdax-1h
# (PT2_FDAX_BSW_001_60). Quelle celle restano qui COMMENTATE e non cancellate, perche' la loro
# taratura - fasi spezzate, stime, il perche' - e' informazione misurata che non conviene riscrivere
# da zero. Per rilanciarle serve prima una classe di partenza PT3B su quella cella.
#
# La ricerca su NQ, nel frattempo, ha gia' dato la sua risposta: ne' la 4h ne' i 15 minuti hanno un
# edge che regga (ricerca/nq-catalogo-costi-veri.md, nq-15m-griglia-grossa.md), quindi le due celle
# NQ non sono una priorita'.
$definizioni = [ordered]@{
    "fdax-4h" = @{
        Strategia = "PT3B_FDAX_PCH_001_240"; Simbolo = "@FDAX"; Timeframe = 240; Motore = "PC"
        SpezzaPattern = $false; Stima = "~2,5 ore"
    }
    # La ricerca della 003 (23/09/2026): la griglia grossa lunga ha indicato la regione - canale 20,
    # solo long, uscita alle 21 - e qui la sweep sceglie pattern, orari e stop DENTRO quella regione
    # (--fix). Una finalista trovata qui non e' confrontabile con una dello spazio intero, e il
    # resoconto lo dichiara. Va lanciata con -Split 2021-01-01 -A 2026-09-01 come la griglia, cosi'
    # i numeri si confrontano; il default del paniere (2022) e' un altro split.
    "fdax-4h-003" = @{
        Strategia = "PT3B_FDAX_PCH_001_240"; Simbolo = "@FDAX"; Timeframe = 240; Motore = "PC"
        SpezzaPattern = $false; Stima = "~2 ore"
        Fissati = "ChannelBars=20;Direction=1;ExitHour=21"
    }
    # La seconda prova del trend following (23/09/2026), che cambia tre cose insieme rispetto alla
    # griglia TF del 22/09: unmirrored, 15 minuti, e con i pattern cercati dalla sweep. Le due fasi
    # pattern sono 152 x 153 col prodotto completo: spezzate, con il prezzo dichiarato. L'archivio
    # ICS di NQ al minuto parte dal 2022: -Da 2022-01-01 -Split 2025-01-01 -A 2026-09-01, e
    # -UtileMedioMinimo 120 (15% del range medio della barra da 15 minuti, nq-15m-griglia-grossa.md).
    "nq-15m-tfu" = @{
        Strategia = "PT3B_NQ_TFU_001_15"; Simbolo = "@NQ"; Timeframe = 15; Motore = "TFU"
        SpezzaPattern = $true; Stima = "~3 ore, fasi pattern spezzate"
    }
    # "nq-4h" = @{
    #     Strategia = "<serve una PT3B su @NQ 240>"; Simbolo = "@NQ"; Timeframe = 240; Motore = "PC"
    #     SpezzaPattern = $false; Stima = "~1,5 ore"
    # }
    # "nq-30m"  = @{
    #     # Col prodotto completo questa cella e' impraticabile, e non per stima ma per misura: il
    #     # 20/09/2026 la sola fase dei pattern neutrali (3.025 x 2 semi) ha richiesto 2 ore e 22
    #     # minuti, contro i 10 minuti della stessa fase su una 4h - 88.000 barre in campione invece
    #     # di 11.500. Con i direzionali, tre volte e mezzo piu' grandi, il totale superava le dieci
    #     # ore. Fasi spezzate, con il prezzo dichiarato sulle interazioni fra pattern.
    #     Strategia = "<serve una PT3B su @NQ 30>"; Simbolo = "@NQ"; Timeframe = 30; Motore = "PC"
    #     SpezzaPattern = $true; Stima = "~2 ore, fasi pattern spezzate"
    # }
    # "fdax-1h" = @{
    #     # Le due fasi pattern del BIASW sono 153 x 152 combinazioni ciascuna: col prodotto completo
    #     # questa cella da sola supera le nove ore. Spezzate diventano minuti, al prezzo dichiarato:
    #     # una coppia richiesto+vietato che rende solo insieme non e' piu' raggiungibile.
    #     Strategia = "<serve una PT3B su @FDAX 60 BIASW>"; Simbolo = "@FDAX"; Timeframe = 60; Motore = "BIASW"
    #     SpezzaPattern = $true; Stima = "~1 ora, fasi pattern spezzate"
    # }
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
           ", swap $BrokerSwap, commissione $CommissionePerLato per lato" +
           $(if ($FlatUtc) { ", flat di sessione $FlatUtc UTC per $FinestraFlat minuti" } else { ", overnight libero" })
Write-Host ("Campione {0} -> {1}, validazione {1} -> {2}, beam {3}, {4}, criterio {5}, feed ICS." -f $Da, $Split, $A, $Beam, $modello, $Criterio)
Write-Host ("Ammissibilita': almeno {0} trade, profit factor >= {1}, utile medio >= {2}." -f $TradeMinimi, $ProfitFactorMinimo, $UtileMedioMinimo)

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
    $costo = if ($BrokerSpread -match ",") { "costo-peggiore" } else { "costo-" + $BrokerSpread.ToLower() }
    $suffisso = "-ics-$costo"
    if ($SpreadPerOra) { $suffisso += "-per-ora" }
    if ($Criterio -eq "worst-period") { $suffisso += "-peggior-tratto" }
    if ($FlatUtc) { $suffisso += "-flat-" + $FlatUtc.Replace(":", "") }
    if ($d.Fissati) { $suffisso += "-regione" }
    $log = Join-Path $uscita "$cella$suffisso.log"
    $md = Join-Path $uscita "$cella$suffisso.md"

    $argomenti = @(
        "--strategy", $d.Strategia, "--symbol", $d.Simbolo, "--timeframe", $d.Timeframe,
        "--engine", $d.Motore, "--broker", "ICS",
        "--spread-broker", $BrokerSpread, "--swap-broker", $BrokerSwap,
        "--commission", $CommissionePerLato.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--from", $Da, "--split", $Split, "--to", $A,
        "--beam", $Beam, "--top", 5, "--min-trades", $TradeMinimi,
        "--min-profit-factor", $ProfitFactorMinimo.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--min-average-trade", $UtileMedioMinimo.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        "--objective", $Criterio, "--out", $md
    )
    if ($d.SpezzaPattern) { $argomenti += "--split-pattern-phases" }
    if ($d.Fissati) { $argomenti += @("--fix", $d.Fissati) }
    if ($SpreadPerOra) { $argomenti += "--spread-per-hour" }
    if ($FlatUtc) { $argomenti += @("--flat-utc", $FlatUtc, "--flat-window", $FinestraFlat) }

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
