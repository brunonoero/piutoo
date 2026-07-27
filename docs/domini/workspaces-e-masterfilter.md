# Workspace e master filter

> Bozza — contenuto da scrivere.

Da coprire:

- Cos'è un workspace, cosa contiene sul filesystem.
- `masterfilter.json`: chi lo scrive, chi lo legge, cosa succede se è vuoto o
  contiene strategie fuori catalogo.
- Relazione con Titano: `WorkspaceMasterFilter ∩ TitanoEnabledStrategies` (vedi
  `titano-rotation.md`).
- Relazione con le sessioni di trading: la sessione viene creata a partire dal
  masterfilter del workspace (vedi `trading-sessions-api.md`).

Riferimenti codice: `Piootoo.Core/Services/WorkspaceService.cs`,
`Piootoo.Core/Services/WorkspaceBacktestPaths.cs`.
