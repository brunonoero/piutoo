# Sweep con split sulle sei strategie di PT5DAV-P1, una alla volta, nei momenti in cui non gira
# nessuno studio ne' altra sweep (stessa regola di tools/coda-sweep-pt5dav.ps1, con cui si alterna).
#
# Perche': il P1 e' tutto in campione. La consegna PT5DAV ha ottimizzato su 2012-2025, le 41 sono
# state filtrate sull'anno broker e il piano e' stato composto sullo stesso anno. Qui si ottimizza il
# motore sulla parte vecchia della storia e si guarda il resto una volta sola.
#
# Il feed dipende dal simbolo, perche' il minuto non c'e' ovunque:
#  - NQ: feed interno al minuto 2007 -> 05/2025, stesso split del pilota (2017-01-01);
#  - GC: il feed interno non ha il minuto; ICS al minuto dal 22/09/2014, split 2021-01-01;
#  - ES: il feed interno non ha ne' il minuto ne' i 30; FTMO al minuto dal 09/11/2020, split
#    2024-01-01. Campione corto: un verdetto su ES vale meno degli altri.
# Costi FTMO come il pilota (spread per ora, swap, commissione 2) e le stesse soglie: nessuna soglia di
# qualita' nella ricerca, il giudizio e' fuori campione.
#
# NB: niente caratteri fuori ASCII (PowerShell 5.1 legge senza BOM come ANSI).
#
# Uso: Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File','C:\piootoo-dev\tools\coda-sweep-p1.ps1' -WindowStyle Hidden

param([int]$MinutiFraControlli = 15)

$repo = Split-Path -Parent $PSScriptRoot
$ricerca = Join-Path $repo 'piootoo-repository\ricerca'
$stato = Join-Path $ricerca 'coda-p1'
$logCoda = Join-Path $ricerca 'coda-sweep-p1.log'
# Eseguibile congelato proprio (27/09/2026): e' quello con il conteggio delle prove nel resoconto.
$bin = Join-Path $repo 'Piootoo.Sweep\bin\coda-sweep-p1'
New-Item -ItemType Directory -Force -Path $stato | Out-Null

# Prima l'oro: fa il 62% del netto del piano.
$celle = @(
    @{ nome = 'p1-sweep-gc-240-bos'; engine = 'P5-BOS'; simbolo = '@GC'; tf = 240; broker = 'ICS';  da = '2014-10-01'; split = '2021-01-01'; a = '2026-09-20' },
    @{ nome = 'p1-sweep-gc-60-pch';  engine = 'P5-PCH'; simbolo = '@GC'; tf = 60;  broker = 'ICS';  da = '2014-10-01'; split = '2021-01-01'; a = '2026-09-20' },
    @{ nome = 'p1-sweep-nq-30-rhl';  engine = 'P5-RHL'; simbolo = '@NQ'; tf = 30;  broker = '';     da = '2008-01-01'; split = '2017-01-01'; a = '2025-05-30' },
    @{ nome = 'p1-sweep-nq-240-bsw'; engine = 'P5-BSW'; simbolo = '@NQ'; tf = 240; broker = '';     da = '2008-01-01'; split = '2017-01-01'; a = '2025-05-30' },
    @{ nome = 'p1-sweep-es-30-rhl';  engine = 'P5-RHL'; simbolo = '@ES'; tf = 30;  broker = 'FTMO'; da = '2020-11-15'; split = '2024-01-01'; a = '2026-09-24' },
    @{ nome = 'p1-sweep-es-30-rbm';  engine = 'P5-RBM'; simbolo = '@ES'; tf = 30;  broker = 'FTMO'; da = '2020-11-15'; split = '2024-01-01'; a = '2026-09-24' }
)

function Write-Log($testo) {
    Add-Content -Path $logCoda -Value ("{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $testo) -Encoding utf8
}

function Test-AltroInCorso {
    if (Get-Process piootoo-sweep -ErrorAction SilentlyContinue) { return $true }
    return [bool](Get-CimInstance Win32_Process -Filter "Name='testhost.exe'" -ErrorAction SilentlyContinue)
}

Write-Log "coda P1 avviata: $($celle.Count) sweep, eseguibile in $bin"

foreach ($cella in $celle) {
    $marcatore = Join-Path $stato "$($cella.nome).fatto"
    if (Test-Path $marcatore) { continue }

    # Il secondo controllo, 45 secondi dopo, evita di partire nello stesso istante delle altre code.
    while ($true) {
        if (-not (Test-AltroInCorso)) {
            Start-Sleep -Seconds 45
            if (-not (Test-AltroInCorso)) { break }
        }
        Start-Sleep -Seconds ($MinutiFraControlli * 60)
    }

    $strategia = 'RC5_' + $cella.engine.Substring(3)
    $log = Join-Path $ricerca "$($cella.nome).log"
    $out = Join-Path $ricerca "$($cella.nome).md"
    $argomenti = @(
        '--strategy', $strategia, '--engine', $cella.engine,
        '--symbol', $cella.simbolo, '--timeframe', $cella.tf,
        '--from', $cella.da, '--split', $cella.split, '--to', $cella.a,
        '--spread-broker', 'FTMO', '--spread-per-hour', '--swap-broker', 'FTMO', '--commission', 2,
        '--min-average-trade', -1000000000, '--out', $out
    )
    if ($cella.broker) { $argomenti += @('--broker', $cella.broker) }
    $feed = if ($cella.broker) { $cella.broker } else { 'interno' }

    Write-Log "$($cella.nome): parte ($($cella.engine) su $($cella.simbolo) $($cella.tf)m, feed $feed, $($cella.da) -> $($cella.split) -> $($cella.a))"
    & (Join-Path $bin 'piootoo-sweep.exe') @argomenti *>&1 | Out-File -FilePath $log -Encoding utf8
    $esito = $LASTEXITCODE
    $riga = "{0:yyyy-MM-dd HH:mm:ss} exit {1}" -f (Get-Date), $esito
    if ($esito -eq 0 -or $esito -eq 1) {
        Set-Content -Path $marcatore -Value $riga -Encoding utf8
        $come = if ($esito -eq 0) { 'con una finalista' } else { 'senza finalista' }
        Write-Log "$($cella.nome): finita $come, resoconto in $out"
    } else {
        Add-Content -Path (Join-Path $stato "$($cella.nome).errore") -Value $riga -Encoding utf8
        Write-Log "$($cella.nome): FALLITA (exit $esito), log in $log; passo alla successiva"
    }
}

Write-Log "coda P1 finita"
