# compare-0042 — cbot-cfd-FTMO contro interno-cfd-FTMO

Cartella creata dalla console il 13/09/2026 10:29 UTC. Report dello strumento in `analisi/report.md`.
Da completare a mano a fine analisi: scheletro e regole in `compare/README.md`.

Per spread ai fill, esiti reali, ritardo degli intent e verifica delle uscite copia qui `log.txt` del cBot e l'export
Events di cTrader (`.xlsx`), poi rilancia lo strumento:

    dotnet run -c Release --project piootoo-repository/compare/strumento-confronto -- <questa cartella>

## Le due gambe

| lato | backtest | slug | serie di prezzi | versione | piano |
|---|---|---|---|---|---|
| cBot | `comp-004x-bt-20250831-0000` | `cbot-cfd-FTMO` | CFD FTMO (datafeed-external/FTMO) | 7.4.0 | COMP-004X |
| interno | `backtest-20260913-0748` | `interno-cfd-FTMO` | CFD FTMO (datafeed-external/FTMO) | 7.3.0 | - |

## Esito

## Scomposizione

## Aperto

## Chiuso

## Cosa torna

## Trappole di misura di questa cartella

