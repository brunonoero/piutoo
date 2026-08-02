# Backtesting

> Bozza — contenuto da scrivere. Nel frattempo `../PROGETTO.md` §4.1 e §6 coprono
> il flusso del loop, le invarianti e gli artefatti prodotti.

Il rapporto fra l'orologio del loop e le barre effettivamente presenti nel feed —
cioè quando una barra può eseguire un ordine e quando è solo un prezzo di
mark-to-market — è in [`orologio-barre-e-fill.md`](orologio-barre-e-fill.md), che
contiene anche i controlli da fare sui trade di un run nuovo.

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
`MaxBars`, `OppositeSignal`, `WeekEnd`, `EndOfRun`): senza di esso due trade con lo
stesso P&L sono indistinguibili in analisi. `CloseOnly` non viene più prodotto — le
strategie non emettono segnali di chiusura — ma il valore resta nell'enum per non
rinumerare i backtest già archiviati.

Il summary conta anche `signalsWithoutExitSpec`: ingressi emessi senza alcuna
condizione di uscita (`StopLoss`, `TakeProfit`, `CloseAtUtc`, `MaxBarsInPosition`).
L'engine non può chiuderli, quindi restano aperti fino alla chiusura tecnica di fine
settimana o fine run: è un difetto della strategia, non del motore.

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
