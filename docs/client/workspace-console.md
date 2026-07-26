# Workspace console (piootooapp.clientform)

> Bozza — contenuto da scrivere.

Console WinForms interna con quattro tab: Workspaces, Backtesting, Titano,
Trading Session. Punto d'ingresso per creare workspace, lanciare backtest,
eseguire rotazioni Titano e pilotare sessioni di trading (vedi
`../domini/trading-sessions-api.md` per il funzionamento del tab Trading
Session).

Da coprire:

- Come i quattro tab si passano dati (workspace selezionato, backtest,
  Titano RunId → sessione).
- Convenzioni UI adottate (gruppi tematici + tooltip per ogni campo, vedi tab
  Titano e Trading Session come riferimento per nuovi tab).

Riferimenti codice: `piootooapp.clientform/WorkspaceBacktestingForm.cs`.
