# La sweep NQ 15m TF unmirrored rifatta SENZA le soglie di ammissibilita' nelle fasi (23/09/2026).
#
# Perche': la prima corsa (nq-15m-tfu-ics-costo-peggiore-per-ora-peggior-tratto.md) ha avuto zero
# ammissibili in ogni fase - uscita base, i quattro pattern spezzati, orari, uscita - con fino a 796
# trade per configurazione. Non era la soglia dei trade: erano profit factor >= 1,25 e utile medio
# >= 120, che la configurazione di partenza (entrambi i lati, nessun filtro) non passa e che un solo
# pattern alla volta non basta a far passare. I semi non si sono mai mossi: la sweep non ha cercato.
# Qui le soglie di profit factor e utile medio si tolgono dalle fasi, cosi' l'ordinamento puo'
# salire un gradino alla volta; restano il minimo di trade (250) e i vincoli per tratto, e la
# validazione fuori campione resta quella di sempre.
#
# L'utile medio va a un valore NEGATIVO, non a zero: WorstSubPeriodObjective scarta
# `AverageTrade < MinAverageTrade`, quindi con 0 una configurazione in perdita resta non ammissibile
# e la partenza (-21.923 in campione) non si muove comunque. Misurato: il primo lancio con 0, alle
# 17:28, ha di nuovo zero ammissibili nella prima fase. Con un minimo negativo le configurazioni in
# perdita entrano in classifica con un punteggio negativo, e la meno peggiore fa da seme.
#
# Parte da sola quando la coda delle griglie ha scritto "=== coda finita". NB: solo ASCII.

$ErrorActionPreference = "Continue"
$radice = Split-Path -Parent $PSScriptRoot
$ricerca = Join-Path $radice "piootoo-repository\ricerca"
$exe = Join-Path $radice "Piootoo.Sweep\bin\Release\net8.0\piootoo-sweep.exe"
$attesa = Join-Path $ricerca "coda-griglie-2026-09-23.log"
$md = Join-Path $ricerca "nq-15m-tfu-senza-soglie-nelle-fasi.md"
$log = Join-Path $ricerca "nq-15m-tfu-senza-soglie-nelle-fasi.log"

while (-not (Test-Path $attesa) -or -not (Select-String -Path $attesa -Pattern "=== coda finita" -Quiet)) {
    Start-Sleep -Seconds 120
}
while (Get-Process piootoo-sweep, testhost -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 30 }

"=== avvio {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm") | Out-File $log -Encoding utf8
& $exe --strategy PT3B_NQ_TFU_001_15 --symbol "@NQ" --timeframe 15 --engine TFU --broker ICS `
    --spread-broker ICS,FTMOPLATFORM --spread-per-hour --swap-broker ICS,FTMO --commission 19.23 `
    --from 2022-01-01 --split 2025-01-01 --to 2026-09-01 --beam 2 --top 5 --min-trades 250 `
    --min-profit-factor 0 --min-average-trade -1000000 --objective worst-period --split-pattern-phases `
    --out $md *>&1 | Out-File $log -Append -Encoding utf8
"=== finita {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm") | Out-File $log -Append -Encoding utf8
