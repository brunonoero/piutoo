# Script per disinstallare FeedWorker come servizio Windows
# Richiede privilegi di amministratore

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "FeedWorker"
)

# Verifica privilegi amministratore
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Questo script richiede privilegi di amministratore. Eseguire PowerShell come amministratore."
    exit 1
}

# Verifica se il servizio esiste
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Warning "Il servizio $ServiceName non è installato."
    exit 0
}

# Ferma il servizio se è in esecuzione
if ($service.Status -eq 'Running') {
    Write-Host "Arresto del servizio $ServiceName..."
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# Disinstalla il servizio
Write-Host "Disinstallazione del servizio $ServiceName..."
sc.exe delete $ServiceName

if ($?) {
    Write-Host "Servizio disinstallato con successo!"
} else {
    Write-Error "Errore durante la disinstallazione del servizio."
    exit 1
}
