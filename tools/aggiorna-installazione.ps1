<#
.SYNOPSIS
  Aggiorna le DLL dell'installazione in C:\piootoo (server e client) con quelle compilate dal checkout.

.DESCRIPTION
  Server e client sono pubblicati self-contained win-x64: la cartella contiene anche il runtime .NET.
  Lo script pubblica il progetto in una cartella temporanea e copia nell'installazione soltanto:
    - le DLL dell'applicazione (Piootoo*.dll, PiootooApp*.dll, piootooapp*.dll) e i relativi .pdb;
    - le DLL che nell'installazione non esistono ancora (dipendenze nuove).
  Non tocca appsettings*.json, gli .exe, il runtime e le altre DLL di terze parti. Se il deps.json
  pubblicato e' diverso da quello installato lo segnala: una dipendenza cambiata di versione puo'
  richiedere una pubblicazione completa.

  Se server o client stanno girando dalla cartella di installazione vengono fermati prima della copia
  e riavviati dopo. Il cBot non e' coinvolto: si aggiorna a mano in cTrader.

.PARAMETER Target
  server, client oppure entrambi (default).

.PARAMETER NoRestart
  Non riavvia i processi fermati.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\aggiorna-installazione.ps1
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\aggiorna-installazione.ps1 -Target server
#>
param(
    [ValidateSet('entrambi', 'server', 'client')]
    [string]$Target = 'entrambi',
    [switch]$NoRestart
)

$ErrorActionPreference = 'Stop'
$checkout = Split-Path -Parent $PSScriptRoot

$installazioni = @(
    [pscustomobject]@{
        Nome        = 'server'
        Progetto    = Join-Path $checkout 'PiootooApp.Server\PiootooApp.Server.csproj'
        Destinazione = 'C:\piootoo\server\publish_run'
        Eseguibile  = 'PiootooApp.Server.exe'
        Framework   = 'net8.0'
        DepsJson    = 'PiootooApp.Server.deps.json'
    },
    [pscustomobject]@{
        Nome        = 'client'
        Progetto    = Join-Path $checkout 'piootooapp.clientform\piootooapp.clientform.csproj'
        Destinazione = 'C:\piootoo\client\win-x64'
        Eseguibile  = 'piootooapp.clientform.exe'
        Framework   = 'net8.0-windows'
        DepsJson    = 'piootooapp.clientform.deps.json'
    }
) | Where-Object { $Target -eq 'entrambi' -or $_.Nome -eq $Target }

# Le assembly dell'applicazione: sempre sostituite. Tutto il resto solo se manca.
$assemblyApplicazione = '^(Piootoo|PiootooApp|piootooapp)[^\\]*\.(dll|pdb)$'

function Stop-Installazione($inst) {
    $exe = Join-Path $inst.Destinazione $inst.Eseguibile
    $processi = @(Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -and ($_.ExecutablePath -ieq $exe) })
    foreach ($p in $processi) {
        Write-Host "  fermo $($inst.Eseguibile) (PID $($p.ProcessId))"
        Stop-Process -Id $p.ProcessId -Force
        Wait-Process -Id $p.ProcessId -Timeout 30 -ErrorAction SilentlyContinue
    }
    if ($processi.Count -gt 0) {
        # Wait-Process torna quando il processo e' uscito, non quando il sistema ha rilasciato
        # i suoi file: copiare subito dopo fallisce con "file in uso" su Piootoo.Core.dll e
        # lascia l'installazione a meta' (16/09/2026, due volte). Si aspetta che una DLL
        # dell'applicazione si apra in scrittura.
        $sonda = Join-Path $inst.Destinazione 'Piootoo.Core.dll'
        $scadenza = (Get-Date).AddSeconds(60)
        while ((Get-Date) -lt $scadenza) {
            try {
                $h = [IO.File]::Open($sonda, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                $h.Close()
                break
            }
            catch { Start-Sleep -Milliseconds 500 }
        }
    }
    return $processi.Count -gt 0
}

$fermati = @()
foreach ($inst in $installazioni) {
    Write-Host "=== $($inst.Nome): $($inst.Destinazione)"
    if (-not (Test-Path -LiteralPath $inst.Destinazione)) { throw "Cartella di installazione assente: $($inst.Destinazione)" }

    $publish = Join-Path ([IO.Path]::GetTempPath()) ("piootoo-publish-$($inst.Nome)-" + [Guid]::NewGuid().ToString('N'))
    Write-Host "  pubblico in $publish"
    & dotnet publish $inst.Progetto -c Release -f $inst.Framework -r win-x64 --self-contained true -o $publish -nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallito per $($inst.Nome) (codice $LASTEXITCODE)" }

    try {
        if (Stop-Installazione $inst) { $fermati += $inst }

        $copiati = 0; $nuovi = 0
        foreach ($file in Get-ChildItem -LiteralPath $publish -File) {
            $destinazione = Join-Path $inst.Destinazione $file.Name
            if ($file.Name -like 'appsettings*.json') { continue }
            if ($file.Name -match $assemblyApplicazione) {
                Copy-Item -LiteralPath $file.FullName -Destination $destinazione -Force
                $copiati++
            }
            elseif ($file.Extension -eq '.dll' -and -not (Test-Path -LiteralPath $destinazione)) {
                Copy-Item -LiteralPath $file.FullName -Destination $destinazione
                Write-Host "  DLL nuova: $($file.Name)"
                $nuovi++
            }
        }
        Write-Host "  sostituiti $copiati file dell'applicazione, aggiunte $nuovi DLL"

        $depsPubblicato = Join-Path $publish $inst.DepsJson
        $depsInstallato = Join-Path $inst.Destinazione $inst.DepsJson
        if ((Test-Path -LiteralPath $depsPubblicato) -and (Test-Path -LiteralPath $depsInstallato) -and
            ((Get-FileHash -LiteralPath $depsPubblicato).Hash -ne (Get-FileHash -LiteralPath $depsInstallato).Hash)) {
            Write-Warning "  $($inst.DepsJson) e' cambiato: se l'applicazione non parte, serve una pubblicazione completa della cartella."
        }
    }
    finally {
        Remove-Item -LiteralPath $publish -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if (-not $NoRestart) {
    foreach ($inst in $fermati) {
        $exe = Join-Path $inst.Destinazione $inst.Eseguibile
        Write-Host "  riavvio $exe"
        Start-Process -FilePath $exe -WorkingDirectory $inst.Destinazione | Out-Null
    }
}
Write-Host 'fatto.'
