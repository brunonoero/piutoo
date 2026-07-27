# Backtesting

> Bozza — contenuto da scrivere. Nel frattempo `../PROGETTO.md` §4.1 e §6 coprono
> il flusso del loop, le invarianti e gli artefatti prodotti.

## Log diagnostici (2026-07-27)

Oltre a `signals.json` e `trades.json`, ogni backtest produce:

- `backtest-log.jsonl` — una riga JSON per evento rilevante: `Run`, `DataSource`,
  `Signal`, `Entry`, `Exit`, `Anomaly`. Append-only, schema piatto e uniforme.
  Gli skip ad alta frequenza (dati insufficienti, candela stale, timeframe non
  allineato) non producono righe: sarebbero milioni.
- `backtest-summary.json` — contatori per strategia (valutazioni, skip per motivo,
  segnali per tipo, trade, uscite per motivo) più un blocco `diagnostics` con i
  problemi rilevati automaticamente. È il file da leggere per capire *perché* un
  backtest non ha prodotto trade.

Ogni trade chiuso porta un `ExitReason` (`StopLoss`, `TakeProfit`, `TimeExit`,
`MaxBars`, `OppositeSignal`, `CloseOnly`, `WeekEnd`, `EndOfRun`): senza di esso due
trade con lo stesso P&L sono indistinguibili in analisi.

Riferimenti: `Piootoo.Core/Services/BacktestDiagnosticsLogger.cs`,
`Piootoo.Shared/Models/Trading/BacktestDiagnosticsContracts.cs`.

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
