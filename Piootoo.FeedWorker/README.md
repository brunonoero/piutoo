# FeedWorker

Servizio Windows Worker Service per .NET che può essere eseguito come eseguibile o installato come servizio Windows.

## Requisiti

- .NET 9.0 SDK
- Windows (per l'installazione come servizio)

## Esecuzione come Eseguibile

Per eseguire il progetto direttamente:

```bash
dotnet run
```

Oppure dopo la pubblicazione:

```bash
dotnet publish -c Release -r win-x64 --self-contained
.\bin\Release\net9.0\win-x64\publish\FeedWorker.exe
```

## Installazione come Servizio Windows

### Prerequisiti

1. Pubblicare il progetto come eseguibile self-contained:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

2. Eseguire PowerShell come **Amministratore**

### Installazione

Eseguire lo script di installazione:

```powershell
.\Install-Service.ps1
```

Lo script installerà il servizio con:
- Nome: `FeedWorker`
- Tipo di avvio: Automatico
- Display Name: `FeedWorker Service`

### Gestione del Servizio

Dopo l'installazione, puoi gestire il servizio tramite:

**PowerShell:**
```powershell
# Avviare il servizio
Start-Service -Name FeedWorker

# Fermare il servizio
Stop-Service -Name FeedWorker

# Verificare lo stato
Get-Service -Name FeedWorker

# Riavviare il servizio
Restart-Service -Name FeedWorker
```

**Services.msc:**
- Aprire `services.msc` e cercare "FeedWorker Service"

### Disinstallazione

Per disinstallare il servizio:

```powershell
.\Uninstall-Service.ps1
```

## Configurazione

La configurazione può essere modificata nel file `appsettings.json` o `appsettings.Development.json`.

## Sviluppo

Il progetto utilizza:
- .NET 9.0
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Hosting.WindowsServices

Il worker principale è implementato nella classe `Worker.cs` che estende `BackgroundService`.
