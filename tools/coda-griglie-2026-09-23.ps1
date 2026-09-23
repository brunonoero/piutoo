# La coda di griglie del 23/09/2026: parte quando le due sweep di lancia-ricerche-2026-09-23.ps1
# hanno finito, e gira in SEQUENZA (ogni studio usa tutti i core).
#
# Ordine, dal segnale piu' forte al piu' debole:
#   A. i filtri della 002 sulla regione FDAX 4h nuda (cinque misure con --params, un minuto);
#   B. altri motori sulla cella FDAX 4h: breakout di sessione, reversal Bollinger, volatility breakout;
#   C. il Price Channel su FDAX a 1 ora.
# Altri simboli (GC, NQ, CL, BP) solo se B o C dicono qualcosa: con il Price Channel nudo hanno
# gia' detto no su dodici anni.
#
# Uso:
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\coda-griglie-2026-09-23.ps1
#
# NB: niente caratteri fuori ASCII in questo file (vedi sweep-paniere.ps1).

$ErrorActionPreference = "Continue"
$radice = Split-Path -Parent $PSScriptRoot
$ricerca = Join-Path $radice "piootoo-repository\ricerca"
$exe = Join-Path $radice "Piootoo.Sweep\bin\Release\net8.0\piootoo-sweep.exe"
$tests = Join-Path $radice "Piootoo.Strategies.Tests\Piootoo.Strategies.Tests.csproj"
$attesa = Join-Path $ricerca "lancio-2026-09-23.log"
$log = Join-Path $ricerca "coda-griglie-2026-09-23.log"

function Scrivi($testo) { $testo | Tee-Object -FilePath $log -Append }

Scrivi ("=== coda avviata {0}, aspetto la fine delle sweep" -f (Get-Date -Format "yyyy-MM-dd HH:mm"))
while (-not (Test-Path $attesa) -or -not (Select-String -Path $attesa -Pattern "=== nq-15m-tfu finita" -Quiet)) {
    Start-Sleep -Seconds 120
}
# Un piootoo-sweep ancora vivo terrebbe le DLL: si aspetta che non ce ne siano.
while (Get-Process piootoo-sweep -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 30 }
Scrivi ("=== sweep finite, parto {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm"))

# ---------------------------------------------------------------- A. i filtri della 002 sulla regione
# Stessi costi della griglia lunga (spread ICS costante, swap ICS, 19,23 per lato), stesso split:
# i numeri si confrontano con fdax-4h-griglia-grossa-lunga.csv. La regione nuda con stop 2500 e
# target 9000 fa +96.324 IS / +102.480 OOS. Le ipotesi sono a priori: i filtri sono quelli della 002,
# validati su dodici anni su un'altra cella, non scelti guardando questa.
$regione = "ChannelBars=20;Direction=1;ExitHour=21;StopLoss=2500;TakeProfit=9000;OffsetTicks=0;DvolMin=0;SkipDay=-1;IntradayOnly=1;MaxBars=0;TrailingStop=0;BreakEven=0"
$misure = [ordered]@{
    "nuda"                       = "PtnNeutYes=55;PtnNeutNo=56;PtnDirYes=52;PtnDirNo=53;StartHour=-1;EndHour=-1"
    "pattern 44"                 = "PtnNeutYes=44;PtnNeutNo=56;PtnDirYes=52;PtnDirNo=53;StartHour=-1;EndHour=-1"
    "finestra 03-18"             = "PtnNeutYes=55;PtnNeutNo=56;PtnDirYes=52;PtnDirNo=53;StartHour=3;EndHour=18"
    "pattern 44 + finestra"      = "PtnNeutYes=44;PtnNeutNo=56;PtnDirYes=52;PtnDirNo=53;StartHour=3;EndHour=18"
    "tutti i gate 002 + finestra" = "PtnNeutYes=44;PtnNeutNo=52;PtnDirYes=52;PtnDirNo=-18;StartHour=3;EndHour=18"
}
$logA = Join-Path $ricerca "fdax-4h-regione-filtri-002.log"
"# I filtri della 002 sulla regione FDAX 4h nuda (canale 20, solo long, uscita 21, stop 2500, target 9000). {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm") | Out-File -FilePath $logA -Encoding utf8
foreach ($nome in $misure.Keys) {
    "" | Out-File -FilePath $logA -Append -Encoding utf8
    "=== {0}" -f $nome | Out-File -FilePath $logA -Append -Encoding utf8
    & $exe --strategy PT3B_FDAX_PCH_001_240 --symbol "@FDAX" --timeframe 240 --broker ICS `
        --spread-broker ICS --swap-broker ICS --commission 19.23 `
        --from 2014-07-18 --split 2021-01-01 --to 2026-09-01 --min-trades 250 `
        --params ($regione + ";" + $misure[$nome]) *>&1 | Out-File -FilePath $logA -Append -Encoding utf8
}
Scrivi ("=== A finita {0}: {1}" -f (Get-Date -Format "HH:mm"), $logA)

# ---------------------------------------------------------------- B e C. le griglie
# dotnet test compila da solo: nessuna corsa deve essere in esecuzione quando parte.
$griglie = [ordered]@{
    "FdaxEnginesCoarseGridTests.CoarseGridOnFdaxSessionBreakout"    = "fdax-4h-bo-griglia-grossa"
    "FdaxEnginesCoarseGridTests.CoarseGridOnFdaxReversalBollinger"  = "fdax-4h-rbm-griglia-grossa"
    "FdaxEnginesCoarseGridTests.CoarseGridOnFdaxVolatilityBreakout" = "fdax-4h-vbo-griglia-grossa"
    "Fdax1hCoarseGridTests"                                         = "fdax-1h-griglia-grossa-lunga"
}
$env:PIOOTOO_STUDI = "1"
foreach ($filtro in $griglie.Keys) {
    $nome = $griglie[$filtro]
    $logG = Join-Path $ricerca ($nome + ".log")
    Scrivi ("=== {0} - avvio {1}" -f $nome, (Get-Date -Format "HH:mm"))
    $inizio = Get-Date
    & dotnet test $tests --filter ("FullyQualifiedName~" + $filtro) --logger "console;verbosity=detailed" *>&1 |
        Out-File -FilePath $logG -Encoding utf8
    Scrivi ("=== {0} finita in {1:N0} minuti: {2}" -f $nome, ((Get-Date) - $inizio).TotalMinutes, $logG)
}
Scrivi ("=== coda finita {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm"))
