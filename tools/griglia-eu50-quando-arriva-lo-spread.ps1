# Aspetta che lo spread FTMO di EU50 (@FESX) arrivi nel repository — lo scrive il server quando il
# bot degli spread manda la misura — e poi lancia la griglia grossa EU50 4h (Eu50CoarseGridTests).
# Nasce il 23/09/2026: lo spread e' l'unico costo che mancava, e un costo che manca non vale zero.
# Log in piootoo-repository/ricerca/eu50-4h-griglia-grossa.log.

$repo = 'C:\piootoo-dev'
$log = Join-Path $repo 'piootoo-repository\ricerca\eu50-4h-griglia-grossa.log'
$spreadDir = Join-Path $repo 'piootoo-repository\spread\FTMO'

function Write-Log($text) {
    Add-Content -Path $log -Value ("{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $text) -Encoding utf8
}

function Test-SpreadArrived {
    $latest = Get-ChildItem $spreadDir -Filter 'FTMO_spread-by-symbol_*.csv' -File |
        Sort-Object LastWriteTime | Select-Object -Last 1
    if ($null -eq $latest) { return $false }
    return [bool](Select-String -Path $latest.FullName -Pattern '^[^,#]*,@FESX,' -Quiet)
}

Write-Log 'in attesa dello spread FTMO di @FESX'
while (-not (Test-SpreadArrived)) { Start-Sleep -Seconds 60 }

# La misura arriva per simbolo: un minuto di margine perche' il server finisca di scrivere il gemello per ora.
Start-Sleep -Seconds 60
Write-Log 'spread arrivato: lancio la griglia grossa EU50 4h'

# Mai insieme a una sweep o a un'altra griglia: stessi core.
while (Get-Process piootoo-sweep -ErrorAction SilentlyContinue) {
    Write-Log 'una sweep sta girando: aspetto'
    Start-Sleep -Seconds 300
}

Set-Location $repo
$env:PIOOTOO_STUDI = '1'
dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj -o Piootoo.Strategies.Tests/bin/griglia-eu50 `
    --filter "FullyQualifiedName~Eu50CoarseGridTests" --logger "console;verbosity=detailed" *>&1 |
    Out-File -FilePath $log -Append -Encoding utf8
Write-Log "eu50-4h finita (exit $LASTEXITCODE)"
