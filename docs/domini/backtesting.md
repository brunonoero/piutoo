# Backtesting

> Bozza — contenuto da scrivere.

Da coprire:

- Cosa produce un backtest: `signals.json` e `trades.json`, dove vengono
  salvati (`<workspace>/backtests/<backtest>/`).
- Schema di `signals.json` v2 (base, coefficienti sizing, quantità finale,
  motivo) e di `trades.json`.
- Cosa distingue un trade valido da uno rifiutato/non filled, e perché questi
  ultimi non entrano mai nei calcoli a valle (Titano, metriche).
- Come backtest interno, backtest cTrader e live cTrader convergono sullo
  stesso contratto (vedi `titano-rotation.md`, sezione "Contratto comune
  cross-engine").

Riferimenti codice: `Piootoo.Core/Services/PiootooBacktestingService.cs`.
