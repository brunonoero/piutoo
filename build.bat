@echo off
REM ---------------------------------------------------------------------------
REM Build + test con log su file, cosi l'output e' leggibile senza copia-incolla.
REM Doppio clic, oppure da terminale:  build.bat
REM Risultato in: build.log
REM ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0"

echo Build in corso... (l'output completo finisce in build.log)
echo.

> build.log echo ===== SDK INSTALLATI =====
dotnet --list-sdks >> build.log 2>&1
>> build.log echo.
>> build.log echo ===== BUILD =====
dotnet build PiootooApp.sln >> build.log 2>&1
set BUILD_EXIT=%errorlevel%
>> build.log echo.
>> build.log echo ===== ESITO BUILD: %BUILD_EXIT% =====

if not "%BUILD_EXIT%"=="0" goto :fine

>> build.log echo.
>> build.log echo ===== TEST =====
dotnet test Piootoo.Strategies.Tests\Piootoo.Strategies.Tests.csproj >> build.log 2>&1
>> build.log echo.
>> build.log echo ===== ESITO TEST: %errorlevel% =====

:fine
echo.
if "%BUILD_EXIT%"=="0" (echo Build OK.) else (echo Build FALLITA - vedi build.log)
echo Log scritto in: %~dp0build.log
echo.
pause
