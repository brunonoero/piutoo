@echo off
REM Scarica lo storico GC=F (Gold future) a 5 e 15 minuti, ultimi 2 anni.
REM Doppio click per lanciarlo, oppure da terminale:
REM     download_gc.bat
REM Parametri extra vengono passati a download_cli.py, es:
REM     download_gc.bat --days-back 365

cd /d "%~dp0"
python download_cli.py --symbols GC=F --timeframes 5,15 %*
pause
