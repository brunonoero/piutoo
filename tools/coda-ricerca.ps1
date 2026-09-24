# Esegue la coda delle celle di ricerca (piootoo-repository/ricerca/coda.json), una alla volta, quando
# i dati dei loro simboli sono tutti pronti. Pensato per restare acceso: ogni dieci minuti guarda la
# coda, e una cella aggiunta al file parte da sola appena il raccoglitore ha portato cio' che serve.
#
# Pronto = GET api/data-status del server dice Ready per ogni simbolo della cella: barre da un minuto,
# aggregati costruiti con la finestra, spread, swap, contratto e calendario. Il server deve essere
# acceso: se non risponde, la cella aspetta.
#
# Una cella finita bene lascia ricerca/coda/<cella>.fatto e il log in ricerca/<cella>.log; una fallita
# accoda una riga a ricerca/coda/<cella>.errore e si riprova dopo mezz'ora, al massimo tre volte.
# Mai insieme a una sweep o a un'altra griglia: stessi core, tempi raddoppiati per entrambe.
#
# Uso: powershell -NoProfile -ExecutionPolicy Bypass -File tools\coda-ricerca.ps1 [-Server http://localhost:5000] [-UnGiro]

param(
    [string]$Server = 'http://localhost:5000',
    [int]$MinutiFraGiri = 10,
    [double]$MinimoGbLiberi = 6,
    [switch]$UnGiro
)

$repo = Split-Path -Parent $PSScriptRoot
$ricerca = Join-Path $repo 'piootoo-repository\ricerca'
$fatte = Join-Path $ricerca 'coda'
$logCoda = Join-Path $ricerca 'coda-ricerca.log'
New-Item -ItemType Directory -Force -Path $fatte | Out-Null

function Write-Log($testo) {
    Add-Content -Path $logCoda -Value ("{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $testo) -Encoding utf8
}

function Get-Pronti($broker) {
    try {
        $stato = Invoke-RestMethod -Uri "$Server/api/data-status?broker=$broker" -TimeoutSec 120
    } catch {
        return $null
    }
    $mappa = @{}
    foreach ($simbolo in $stato.symbols) { $mappa[$simbolo.symbol] = $simbolo }
    return $mappa
}

function Test-AltroInCorso {
    if (Get-Process piootoo-sweep -ErrorAction SilentlyContinue) { return $true }
    $studi = Get-CimInstance Win32_Process -Filter "Name='testhost.exe'" -ErrorAction SilentlyContinue
    return [bool]$studi
}

Write-Log "coda avviata (server $Server)"

while ($true) {
    $coda = Get-Content (Join-Path $ricerca 'coda.json') -Raw | ConvertFrom-Json
    $lanciata = $false

    foreach ($cella in $coda.celle) {
        $marcatore = Join-Path $fatte "$($cella.cella).fatto"
        if (Test-Path $marcatore) { continue }

        # Un fallimento non chiude la cella: il caso che l'ha insegnato e' la compilazione morta per
        # memoria esaurita mentre il raccoglitore caricava un mese di tick (24/09/2026). Si riprova
        # dopo mezz'ora, fino a tre volte; poi la cella si ferma e il file .errore dice perche'.
        $errore = Join-Path $fatte "$($cella.cella).errore"
        if (Test-Path $errore) {
            $tentativi = @(Get-Content $errore).Count
            if ($tentativi -ge 3) { continue }
            if ((Get-Item $errore).LastWriteTime -gt (Get-Date).AddMinutes(-30)) { continue }
        }

        $pronti = Get-Pronti $cella.broker
        if ($null -eq $pronti) {
            Write-Log "$($cella.cella): server non raggiungibile, riprovo al prossimo giro"
            break
        }

        $mancano = @()
        foreach ($simbolo in $cella.simboli) {
            $riga = $pronti[$simbolo]
            if ($null -eq $riga) { $mancano += "$simbolo (sconosciuto al broker $($cella.broker))"; continue }
            if (-not $riga.ready) { $mancano += "$simbolo ($($riga.missing -join ', '))" }
        }
        if ($mancano.Count -gt 0) {
            Write-Log "$($cella.cella): in attesa di $($mancano -join '; ')"
            continue
        }

        if (Test-AltroInCorso) {
            Write-Log "$($cella.cella): pronta, ma gira gia' una sweep o uno studio: aspetto"
            break
        }

        # Una griglia compila la solution e poi tiene in RAM il minuto di sei anni: sotto questa soglia
        # muore a meta' (24/09/2026, raccoglitore che caricava un mese di tick). Aspettare non conta
        # come tentativo.
        $liberaGb = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB
        if ($liberaGb -lt $MinimoGbLiberi) {
            Write-Log ("{0}: pronta, ma liberi solo {1:N1} GB di RAM (servono {2}): aspetto" -f $cella.cella, $liberaGb, $MinimoGbLiberi)
            break
        }

        $log = Join-Path $ricerca "$($cella.cella).log"
        Write-Log "$($cella.cella): parte ($($cella.test))"
        Set-Location $repo
        $env:PIOOTOO_STUDI = '1'
        dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj -o Piootoo.Strategies.Tests/bin/coda-ricerca `
            --filter "FullyQualifiedName~$($cella.test)" --logger "console;verbosity=detailed" *>&1 |
            Out-File -FilePath $log -Encoding utf8
        $esito = $LASTEXITCODE
        Remove-Item Env:\PIOOTOO_STUDI -ErrorAction SilentlyContinue

        $riga = "{0:yyyy-MM-dd HH:mm:ss} exit {1}" -f (Get-Date), $esito
        if ($esito -eq 0) {
            Set-Content -Path $marcatore -Value $riga -Encoding utf8
            Write-Log "$($cella.cella): finita, log in $log"
        } else {
            Add-Content -Path $errore -Value $riga -Encoding utf8
            Write-Log "$($cella.cella): FALLITA (exit $esito), riprovo fra mezz'ora; log in $log"
        }
        $lanciata = $true
        break
    }

    if ($UnGiro) { break }
    if (-not $lanciata) { Start-Sleep -Seconds ($MinutiFraGiri * 60) }
}
