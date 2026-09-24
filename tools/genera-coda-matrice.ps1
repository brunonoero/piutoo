# Aggiunge a piootoo-repository/ricerca/coda.json le celle della matrice delle griglie grosse
# (CoarseGridMatrixTests): ogni motore con contenitore generico, su ogni mercato, a ogni timeframe.
#
# L'ordine e' quello in cui la coda le lancia: prima un timeframe intero su tutti i mercati (4 ore,
# poi 1 ora, 30 e 15 minuti: dal piu' veloce al piu' lento, cosi' il quadro si allarga presto), e
# dentro il timeframe i mercati dal rapporto spread/range piu' basso, dove un edge ha piu' margine.
# Le celle gia' presenti nel file non si duplicano: rilanciare lo script aggiunge solo le nuove.
#
# Uso: powershell -NoProfile -ExecutionPolicy Bypass -File tools\genera-coda-matrice.ps1
# Con un sottoinsieme di timeframe o mercati serve -Command: con -File "240,60" arriva come 24060.
#      powershell -NoProfile -ExecutionPolicy Bypass -Command "& 'tools\genera-coda-matrice.ps1' -Timeframe 240,60"

param(
    [string]$Broker = 'FTMO',
    [string]$Da = '2022-01-01',
    [string[]]$Mercati = @('FDAX','Z','NQ','ES','YM','FCE','GC','NIY','FESX','RTY','BRN','CL','SI','AP','BP','NG','PL'),
    [int[]]$Timeframe = @(240, 60, 30, 15),
    [string[]]$Motori = @('PCH','TFM','TFU','BO','BOS','RBM','RBU','RHL','VBO','MAC','LF','LFHL')
)

$repo = Split-Path -Parent $PSScriptRoot
$percorso = Join-Path $repo 'piootoo-repository\ricerca\coda.json'
$coda = Get-Content $percorso -Raw | ConvertFrom-Json
$presenti = @{}
foreach ($c in $coda.celle) { $presenti[$c.cella] = $true }

$nuove = New-Object System.Collections.ArrayList
foreach ($tf in $Timeframe) {
    foreach ($m in $Mercati) {
        foreach ($motore in $Motori) {
            $id = "matrice-{0}-{1}-{2}" -f $m.ToLowerInvariant(), $tf, $motore.ToLowerInvariant()
            if ($presenti.ContainsKey($id)) { continue }
            [void]$nuove.Add([pscustomobject]@{
                cella = $id
                test = 'CoarseGridMatrixTests'
                broker = $Broker
                simboli = @("@$m")
                da = $Da
                env = [pscustomobject]@{ PIOOTOO_CELLA = "$motore|@$m|$tf|$Broker" }
            })
        }
    }
}

$coda.celle = @($coda.celle) + $nuove
$testo = $coda | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText($percorso, $testo, (New-Object Text.UTF8Encoding($false)))
"aggiunte $($nuove.Count) celle, totale $(@($coda.celle).Count)"
