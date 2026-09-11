@echo off
REM ---------------------------------------------------------------------------
REM Pubblica server e console e copia i publish nelle cartelle operative.
REM
REM   PiootooApp.Server      -> C:\piootoo\server\publish_run
REM   piootooapp.clientform  -> C:\piootoo\client\win-x64
REM
REM Opzioni:
REM   /solocopia   salta "dotnet publish", copia i publish gia' presenti
REM   /config      copia anche appsettings*.json (di default NON si copiano:
REM                quelli di dev puntano a C:\piootoo-dev, quelli operativi a
REM                C:\piootoo, e sovrascriverli cambia repository e url server)
REM   /pulisci     cancella nella destinazione i file che il publish non ha piu'
REM                (robocopy /PURGE) - utile dopo aver rimosso dipendenze
REM
REM Doppio clic, oppure da terminale:  pubblica.bat [opzioni]
REM Log completo in: pubblica.log
REM ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0"

set "SOLO_COPIA=0"
set "CON_CONFIG=0"
set "PULISCI=0"

:parse
if "%~1"=="" goto :fineargs
if /i "%~1"=="/solocopia" (
  set "SOLO_COPIA=1"
  shift
  goto :parse
)
if /i "%~1"=="/config" (
  set "CON_CONFIG=1"
  shift
  goto :parse
)
if /i "%~1"=="/pulisci" (
  set "PULISCI=1"
  shift
  goto :parse
)
echo Opzione sconosciuta: %~1
echo Uso: pubblica.bat [/solocopia] [/config] [/pulisci]
exit /b 2
:fineargs

set "SRV_PROJ=PiootooApp.Server\PiootooApp.Server.csproj"
set "SRV_OUT=%~dp0PiootooApp.Server\bin\Release\net8.0\publish"
set "SRV_DEST=C:\piootoo\server\publish_run"

set "CLI_PROJ=piootooapp.clientform\piootooapp.clientform.csproj"
set "CLI_OUT=%~dp0piootooapp.clientform\bin\Release\net8.0-windows\publish\win-x64"
set "CLI_DEST=C:\piootoo\client\win-x64"

set "LOG=%~dp0pubblica.log"

set "ESCLUDI="
if "%CON_CONFIG%"=="0" set "ESCLUDI=/XF appsettings.json appsettings.Development.json"

set "PURGE="
if "%PULISCI%"=="1" set "PURGE=/PURGE"

REM --- i file in destinazione sono in uso se server o console sono avviati ----
tasklist /FI "IMAGENAME eq PiootooApp.Server.exe" 2>nul | find /i "PiootooApp.Server.exe" >nul
if not errorlevel 1 goto :inuso
tasklist /FI "IMAGENAME eq piootooapp.clientform.exe" 2>nul | find /i "piootooapp.clientform.exe" >nul
if not errorlevel 1 goto :inuso
goto :avvia

:inuso
echo.
echo Server o console sono in esecuzione: chiudili prima di copiare,
echo altrimenti i file nella destinazione risultano bloccati.
echo.
pause
exit /b 1

:avvia
> "%LOG%" echo ===== %DATE% %TIME% =====
>> "%LOG%" echo solocopia=%SOLO_COPIA% config=%CON_CONFIG% pulisci=%PULISCI%

if "%SOLO_COPIA%"=="1" goto :copia

echo [1/4] publish server...
>> "%LOG%" echo.
>> "%LOG%" echo ===== PUBLISH SERVER =====
dotnet publish "%SRV_PROJ%" -c Release -r win-x64 --self-contained true -o "%SRV_OUT%" >> "%LOG%" 2>&1
if errorlevel 1 goto :errpublish

echo [2/4] publish console...
>> "%LOG%" echo.
>> "%LOG%" echo ===== PUBLISH CONSOLE =====
dotnet publish "%CLI_PROJ%" -c Release -r win-x64 --self-contained true -o "%CLI_OUT%" >> "%LOG%" 2>&1
if errorlevel 1 goto :errpublish

:copia
if not exist "%SRV_OUT%\PiootooApp.Server.exe" goto :errsorgente
if not exist "%CLI_OUT%\piootooapp.clientform.exe" goto :errsorgente

echo [3/4] copia server  -^> %SRV_DEST%
>> "%LOG%" echo.
>> "%LOG%" echo ===== COPIA SERVER =====
robocopy "%SRV_OUT%" "%SRV_DEST%" /E %PURGE% %ESCLUDI% /R:2 /W:2 /NFL /NDL /NP >> "%LOG%" 2>&1
if errorlevel 8 goto :errcopia

echo [4/4] copia console -^> %CLI_DEST%
>> "%LOG%" echo.
>> "%LOG%" echo ===== COPIA CONSOLE =====
robocopy "%CLI_OUT%" "%CLI_DEST%" /E %PURGE% %ESCLUDI% /R:2 /W:2 /NFL /NDL /NP >> "%LOG%" 2>&1
if errorlevel 8 goto :errcopia

echo.
echo Fatto.
if "%CON_CONFIG%"=="0" echo appsettings*.json NON copiati: la configurazione operativa resta quella delle destinazioni.
>> "%LOG%" echo.
>> "%LOG%" echo ===== ESITO: OK =====
goto :fine

:errpublish
echo.
echo PUBLISH FALLITO - vedi pubblica.log
>> "%LOG%" echo ===== ESITO: PUBLISH FALLITO =====
set "USCITA=1"
goto :fine

:errsorgente
echo.
echo Publish mancante: esegui pubblica.bat senza /solocopia.
>> "%LOG%" echo ===== ESITO: SORGENTE MANCANTE =====
set "USCITA=1"
goto :fine

:errcopia
echo.
echo COPIA FALLITA - vedi pubblica.log
>> "%LOG%" echo ===== ESITO: COPIA FALLITA =====
set "USCITA=1"
goto :fine

:fine
echo Log scritto in: %LOG%
echo.
pause
if defined USCITA exit /b 1
exit /b 0
