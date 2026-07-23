# Script per installare FeedWorker come servizio Windows
# Richiede privilegi di amministratore

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "FeedWorker",
    
    [Parameter(Mandatory=$false)]
    [string]$DisplayName = "FeedWorker Service",
    
    [Parameter(Mandatory=$false)]
    [string]$Description = "FeedWorker Background Service"
)

# Verifica privilegi amministratore
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Questo script richiede privilegi di amministratore. Eseguire PowerShell come amministratore."
    exit 1
}

# Trova il percorso dell'eseguibile
$exePath = Join-Path $PSScriptRoot "bin\Release\net9.0\win-x64\publish\FeedWorker.exe"

if (-not (Test-Path $exePath)) {
    Write-Warning "Eseguibile non trovato in $exePath"
    Write-Host "Eseguire prima: dotnet publish -c Release -r win-x64 --self-contained"
    exit 1
}

# Verifica se il servizio esiste già
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Warning "Il servizio $ServiceName esiste già."
    $response = Read-Host "Vuoi disinstallarlo prima? (S/N)"
    if ($response -eq "S" -or $response -eq "s") {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $ServiceName
        Start-Sleep -Seconds 2
    } else {
        Write-Host "Installazione annullata."
        exit 0
    }
}

# Installa il servizio
Write-Host "Installazione del servizio $ServiceName..."
New-Service -Name $ServiceName `
    -BinaryPathName $exePath `
    -DisplayName $DisplayName `
    -Description $Description `
    -StartupType Automatic

if ($?) {
    Write-Host "Servizio installato con successo!"
    Write-Host "Per avviare il servizio: Start-Service -Name $ServiceName"
    Write-Host "Per fermare il servizio: Stop-Service -Name $ServiceName"
} else {
    Write-Error "Errore durante l'installazione del servizio."
    exit 1
}
