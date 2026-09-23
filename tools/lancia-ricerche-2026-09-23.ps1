# Le due ricerche del 23/09/2026, in SEQUENZA: la 003 su FDAX dentro la regione della griglia grossa,
# poi il trend following unmirrored su NQ 15 minuti. Ogni corsa usa tutti i core: due insieme
# raddoppiano il tempo di entrambe.
#
# Uso:
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\lancia-ricerche-2026-09-23.ps1
#
# NB: niente caratteri fuori ASCII in questo file (vedi sweep-paniere.ps1).

$ErrorActionPreference = "Continue"
$paniere = Join-Path $PSScriptRoot "sweep-paniere.ps1"
$log = Join-Path (Split-Path -Parent $PSScriptRoot) "piootoo-repository\ricerca\lancio-2026-09-23.log"

"=== avvio {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm") | Tee-Object -FilePath $log -Append

# 1. La 003 su FDAX: stesso split della griglia lunga (2021-01-01), cosi' i numeri si confrontano
#    con le celle della regione (canale 20, solo long, uscita alle 21: IS +96k / OOS +102k nude).
& $paniere -Celle fdax-4h-003 -Split 2021-01-01 -A 2026-09-01 *>&1 | Tee-Object -FilePath $log -Append

"=== fdax-4h-003 finita {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm") | Tee-Object -FilePath $log -Append

# 2. Il trend following unmirrored su NQ 15 minuti: l'archivio ICS al minuto parte dal 2022.
#    Utile medio minimo 120: il 15% del range medio della barra da 15 minuti (nq-15m-griglia-grossa.md).
& $paniere -Celle nq-15m-tfu -Da 2022-01-01 -Split 2025-01-01 -A 2026-09-01 -UtileMedioMinimo 120 *>&1 | Tee-Object -FilePath $log -Append

"=== nq-15m-tfu finita {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm") | Tee-Object -FilePath $log -Append
