# Lancia le sweep PT5DAV del pilota NQ una alla volta, nei momenti in cui non gira nessuno studio ne'
# altra sweep (stessi core, tempi raddoppiati), alternandosi con la coda delle griglie
# (tools/coda-ricerca.ps1). Mentre una sweep gira e' la coda delle griglie ad aspettare: la riconosce
# dal processo piootoo-sweep.
#
# Il pilota: ottimizzare sulla storia 2008-2016 del feed interno al minuto e verificare sul 2017 ->
# 05/2025, con le griglie RICOSTRUITE di Pt5DavSweepSpaces (vedi docs/domini/mappa-strategie-pt5dav.md).
# Una sweep finita bene lascia ricerca/coda-pt5dav/<nome>.fatto e il resoconto ricerca/<nome>.md.
#
# Uso: Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File','C:\piootoo-dev\tools\coda-sweep-pt5dav.ps1' -WindowStyle Hidden

param([int]$MinutiFraControlli = 15)

$repo = Split-Path -Parent $PSScriptRoot
$ricerca = Join-Path $repo 'piootoo-repository\ricerca'
$stato = Join-Path $ricerca 'coda-pt5dav'
$logCoda = Join-Path $ricerca 'coda-sweep-pt5dav.log'
$bin = Join-Path $repo 'Piootoo.Sweep\bin\coda-sweep-pt5dav'
New-Item -ItemType Directory -Force -Path $stato | Out-Null

# Le tre celle NQ 15m del piano scelto sulla storia. Costi del paniere FTMO, soglie della skill sweep-cella.
$celle = @(
    @{ nome = 'pt5dav-sweep-nq-15-bos'; engine = 'P5-BOS'; strategia = 'RC5_BOS' },
    @{ nome = 'pt5dav-sweep-nq-15-vbo'; engine = 'P5-VBO'; strategia = 'RC5_VBO' },
    @{ nome = 'pt5dav-sweep-nq-15-rhl'; engine = 'P5-RHL'; strategia = 'RC5_RHL' }
)

function Write-Log($testo) {
    Add-Content -Path $logCoda -Value ("{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $testo) -Encoding utf8
}

function Test-AltroInCorso {
    if (Get-Process piootoo-sweep -ErrorAction SilentlyContinue) { return $true }
    return [bool](Get-CimInstance Win32_Process -Filter "Name='testhost.exe'" -ErrorAction SilentlyContinue)
}

Write-Log "coda PT5DAV avviata: $($celle.Count) sweep, eseguibile in $bin"

foreach ($cella in $celle) {
    $marcatore = Join-Path $stato "$($cella.nome).fatto"
    if (Test-Path $marcatore) { continue }

    # Dal 26/09/2026 si parte nei momenti liberi, non a matrice chiusa: la matrice finiva sei celle
    # al giorno (si ferma a ogni studio) e ha celle che non saranno mai pronte (NQ chiede il minuto
    # FTMO dal 01/01/2022, l'archivio parte dal 16/05/2022), quindi "dopo la matrice" voleva dire
    # mai. Le due code si alternano: ciascuna parte solo se non gira gia' una sweep o uno studio.
    # Il secondo controllo, mezzo minuto dopo, evita di partire nello stesso istante della coda
    # delle griglie, che guarda ogni dieci minuti.
    while ($true) {
        if (-not (Test-AltroInCorso)) {
            Start-Sleep -Seconds 30
            if (-not (Test-AltroInCorso)) { break }
        }
        Start-Sleep -Seconds ($MinutiFraControlli * 60)
    }

    $log = Join-Path $ricerca "$($cella.nome).log"
    $out = Join-Path $ricerca "$($cella.nome).md"
    Write-Log "$($cella.nome): parte ($($cella.engine) su @NQ 15m, 2008 -> 2017 -> 05/2025)"
    & (Join-Path $bin 'piootoo-sweep.exe') --strategy $cella.strategia --engine $cella.engine `
        --symbol '@NQ' --timeframe 15 --from 2008-01-01 --split 2017-01-01 --to 2025-05-30 `
        --spread-broker FTMO --spread-per-hour --swap-broker FTMO --commission 2 `
        --min-profit-factor 1.25 --min-average-trade 120 --out $out *>&1 |
        Out-File -FilePath $log -Encoding utf8
    $esito = $LASTEXITCODE
    $riga = "{0:yyyy-MM-dd HH:mm:ss} exit {1}" -f (Get-Date), $esito
    # piootoo-sweep esce con 0 se una finalista sopravvive, 1 se la ricerca e' finita senza: e' un
    # esito legittimo, non un errore. 2 e 3 sono argomenti o costi mancanti.
    if ($esito -eq 0 -or $esito -eq 1) {
        Set-Content -Path $marcatore -Value $riga -Encoding utf8
        $come = if ($esito -eq 0) { 'con una finalista' } else { 'senza finalista' }
        Write-Log "$($cella.nome): finita $come, resoconto in $out"
    } else {
        Add-Content -Path (Join-Path $stato "$($cella.nome).errore") -Value $riga -Encoding utf8
        Write-Log "$($cella.nome): FALLITA (exit $esito), log in $log; passo alla successiva"
    }
}

Write-Log "coda PT5DAV finita"
