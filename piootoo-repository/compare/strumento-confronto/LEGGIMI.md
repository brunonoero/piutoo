# Strumento di confronto cBot / backtest interno

Confronta trade per trade un run cBot cTrader e un backtest interno sullo stesso feed di broker,
e scrive un report con le sezioni che hanno guidato l'indagine di compare-0033: differenze di
trade e loro cause, uscite (anche contro l'export eventi di cTrader), finestra operativa,
sessione, spread e rapporto spread/stop, curve di equity crescenti, ritardo degli intent.

## Uso

Dalla console: lista dei backtest, seleziona il run del cBot e il backtest interno, **Confronta…**.
Il server crea `compare-NNNN`, ci copia gli artefatti dei due run e lancia l'analisi (vedi
`compare/README.md`).

Da riga di comando, per esempio dopo aver aggiunto `log.txt` e l'export Events a una cartella:

```bash
dotnet run -c Release --project piootoo-repository/compare/strumento-confronto -- <cartella compare> [cartella output] [broker del feed]
```

La cartella compare (`piootoo-repository/compare/compare-NNNN`) deve contenere:

| file | obbligatorio | da dove |
|---|---|---|
| `trades-cbot-*.json` | sì | console, esportazione trade della sessione cBot |
| `trades-interno-*.json` | sì | console, esportazione trade del backtest |
| `backtest-summary-*.json` | sì | cartella del backtest interno |
| `run-cbot-*.json` | no | `origin.json` della sessione: dichiara il broker del feed |
| `log.txt` | no | log del cBot in cTrader; senza, saltano spread ai fill, esiti reali e ritardo degli intent |
| `*.xlsx` | no | export «Events» di cTrader; senza, salta la verifica delle uscite contro la piattaforma |

Il feed a barre viene da `piootoo-repository/datafeed-external/{broker}/`; la finestra di
confronto è la sovrapposizione degli ingressi delle due gambe. Le strategie, i calendari e i
valori punto vengono dalle assembly del server. La logica sta in
`Piootoo.Core/Services/Compare/CompareRunner.cs`, la stessa che usa il server; questo progetto
è solo la riga di comando, referenzia `Piootoo.Core` e va lanciato dal checkout. Il passaggio
dallo script alla libreria (13/09/2026) non ha cambiato il report: su compare-0040 `report.md` e i
tre CSV escono identici riga per riga.

Output in `<cartella compare>/analisi/`: `report.md`, `trade-abbinati.csv`,
`trade-non-abbinati.csv`, `strategie.csv`.

## Come leggere il report

La lettura di riferimento è `compare-0033/analisi-2026-09-11.md`; per l'anno intero
`compare-0040/analisi-2026-09-13.md`. In breve: tutte le cifre sono per
contratto future; il cBot opera in lotti e il rapporto lotti/contratto viene dalla tabella di
conversione del piano; le uscite del cBot etichettate `BrokerExit` hanno il motivo dedotto dal
segno del netto, non da un vero stop o target.
