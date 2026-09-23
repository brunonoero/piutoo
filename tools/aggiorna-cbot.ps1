<#
.SYNOPSIS
  Aggiorna il cBot PiootooDistributedExecutionBot in cTrader dal sorgente del repository e lo compila.

.DESCRIPTION
  Il sorgente di riferimento e' piootoo-repository\ctrader\PiootooDistributedExecutionBot.cs (in git).
  cTrader tiene il proprio progetto in Documenti\cAlgo\Sources\Robots\PiootooDistributedExecutionBot\
  PiootooDistributedExecutionBot\: un .csproj con il pacchetto cTrader.Automate, che dotnet build
  compila nel file .algo che cTrader carica.

  Lo script:
    1. confronta il sorgente del repository con quello in cTrader (il BOM non conta);
    2. se sono diversi e la copia in cTrader e' piu' recente si ferma, perche' vuol dire che il
       cBot e' stato modificato dentro cTrader: la modifica va riportata nel repository, oppure si
       rilancia con -Force per sovrascriverla. Prima di sovrascrivere lascia una copia .bak;
    3. compila il progetto di cTrader.

  Le istanze del cBot gia' avviate in cTrader continuano con la versione vecchia: vanno fermate e
  riavviate.

.PARAMETER Nome
  Il cBot da aggiornare: il nome del file in piootoo-repository\ctrader e del progetto in cTrader.
  Di default il bot operativo; per il raccoglitore -Nome PiootooDatafeedSyncBot.

.PARAMETER Force
  Sovrascrive anche una copia in cTrader piu' recente del repository.

.PARAMETER NoBuild
  Aggiorna solo il sorgente, senza compilare.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\aggiorna-cbot.ps1
#>
param(
    [string]$Nome = 'PiootooDistributedExecutionBot',
    [switch]$Force,
    [switch]$NoBuild,
    [string]$CtraderRobots = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'cAlgo\Sources\Robots')
)

$ErrorActionPreference = 'Stop'
$nome = $Nome
$checkout = Split-Path -Parent $PSScriptRoot
$sorgente = Join-Path $checkout "piootoo-repository\ctrader\$nome.cs"
$progetto = Join-Path $CtraderRobots "$nome\$nome"
$destinazione = Join-Path $progetto "$nome.cs"

if (-not (Test-Path -LiteralPath $sorgente)) { throw "Sorgente non trovato: $sorgente" }
if (-not (Test-Path -LiteralPath (Join-Path $progetto "$nome.csproj"))) {
    throw "Progetto cTrader non trovato in $progetto. Crea il cBot una volta da cTrader (New cBot, nome $nome), poi rilancia."
}

# ReadAllText toglie il BOM: cTrader salva senza, il repository con.
$testoRepository = [IO.File]::ReadAllText($sorgente)
$testoCtrader = if (Test-Path -LiteralPath $destinazione) { [IO.File]::ReadAllText($destinazione) } else { $null }

if ($testoCtrader -ceq $testoRepository) {
    Write-Host "sorgente in cTrader gia' uguale al repository"
}
else {
    if ($null -ne $testoCtrader) {
        $ctrader = Get-Item -LiteralPath $destinazione
        $repository = Get-Item -LiteralPath $sorgente
        if ($ctrader.LastWriteTimeUtc -gt $repository.LastWriteTimeUtc -and -not $Force) {
            throw ("La copia in cTrader ($($ctrader.LastWriteTime)) e' diversa dal repository ed e' piu' recente " +
                   "($($repository.LastWriteTime)): probabilmente il cBot e' stato modificato in cTrader. " +
                   "Riporta la modifica nel repository, oppure rilancia con -Force per sovrascriverla.")
        }
        Copy-Item -LiteralPath $destinazione -Destination "$destinazione.bak" -Force
        Write-Host "copia di sicurezza: $destinazione.bak"
    }
    Copy-Item -LiteralPath $sorgente -Destination $destinazione -Force
    Write-Host "sorgente aggiornato: $destinazione"
}

if ($NoBuild) { Write-Host 'compilazione saltata (-NoBuild).'; return }

# Si compila una copia del progetto, senza config.json: quando cTrader crea il progetto ci mette il
# modello "Hello world" di robot e parametri, e il generatore di cTrader.Automate rifiuta config.json
# e attributi nel codice insieme (CGEN006). Il robot lo dichiara il codice.
#
# Il pacchetto cTrader.Automate, finita la compilazione, scrive da se' il .algo in
# Documenti\cAlgo\Sources\Robots, con il nome della cartella che contiene il progetto: per questo la
# copia sta in <temp>\<nome>\<nome>, e il file che esce e' esattamente quello che cTrader carica.
$build = Join-Path ([IO.Path]::GetTempPath()) ("piootoo-cbot-" + [Guid]::NewGuid().ToString('N'))
$copia = Join-Path $build "$nome\$nome"
$installato = Join-Path $CtraderRobots "$nome.algo"
New-Item -ItemType Directory -Force $copia | Out-Null
try {
    Get-ChildItem -LiteralPath $progetto -File | Where-Object { $_.Extension -in '.cs', '.csproj' } |
        Copy-Item -Destination $copia
    $csproj = Join-Path $copia "$nome.csproj"
    $xml = [IO.File]::ReadAllText($csproj) -replace '(?s)\s*<ItemGroup>\s*<AdditionalFiles Include="config.json"\s*/>\s*</ItemGroup>', ''
    [IO.File]::WriteAllText($csproj, $xml)

    if (Test-Path -LiteralPath $installato) {
        Copy-Item -LiteralPath $installato -Destination "$installato.bak" -Force
        Write-Host "copia di sicurezza: $installato.bak"
    }

    $inizio = (Get-Date).AddSeconds(-2)
    Push-Location $copia
    try {
        & dotnet build -nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw "dotnet build fallito (codice $LASTEXITCODE)" }
    }
    finally {
        Pop-Location
    }

    # Se il pacchetto non l'ha scritto da se', si copia quello rimasto nella cartella di build.
    if (-not (Test-Path -LiteralPath $installato) -or (Get-Item -LiteralPath $installato).LastWriteTime -lt $inizio) {
        $algo = Get-ChildItem -LiteralPath $build -Recurse -File |
            Where-Object { $_.Name -eq "$nome.algo" } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if (-not $algo) { throw "la compilazione e' riuscita ma non ha prodotto $nome.algo" }
        Copy-Item -LiteralPath $algo.FullName -Destination $installato -Force
    }

    $file = Get-Item -LiteralPath $installato
    Write-Host "compilato e installato: $installato ($([Math]::Round($file.Length / 1KB)) KB, $($file.LastWriteTime))"
}
finally {
    cmd /c "rd /s /q `"$build`"" | Out-Null
}

Write-Host "In cTrader ferma e riavvia le istanze del cBot perche' carichino la nuova versione."
