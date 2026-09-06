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

## Il piano di un run (2026-09-05)

Un backtest puo' dichiarare il **piano** che riproduce (`BacktestingRequest.PlanCode`).
Quando lo fa, dal piano vengono *tutte* le regole che governano il run:

| Cosa | Da dove | Effetto |
|---|---|---|
| Universo operativo | tabella di conversione del **broker** del piano | le strategie sui simboli che la tabella non prevede, o che vi sono disabilitati, non girano affatto |
| Strategie spente | `TradingPlan.DisabledStrategies` (Id di catalogo) | tolte dal masterfilter prima di istanziarle |
| Tenuta | `TradingPlan.Holding` | sovrascrive `BacktestingRequest.Holding` |
| Commissione | `TradingPlan.CommissionPerContract` | sovrascrive quella della richiesta |

Il **conto** non entra: la tabella dei simboli e' dichiarata sul broker
(`TradingBroker.SymbolConversionCode`) e tutti i conti di un piano sono di quel
broker, quindi nominarne uno significava sceglierlo a caso fra conti che danno la
stessa risposta. La lettura per sola tabella e' `AccountSymbolConversion.FromTable`:
lascia `BalanceScale` a 1, perche' il backtest interno resta neutro sulle size —
`SizeMultiplier` e `PositionSizing` del piano non toccano un solo contratto.

Il **datasource resta una scelta a parte** (`DatafeedBroker`): il broker del piano
dice con che tabella si opera, non da quale archivio di barre si legge. Misurare lo
stesso piano sul feed interno e su quello del suo broker e' il confronto che
quantifica lo spread.

Un piano inesistente, o un broker che il registro non conosce, **fanno fallire
l'avvio**: vale la regola del datafeed mancante, mai proseguire in silenzio.

Nel summary il run lo dichiara con `planCode`, `brokerCode`, `planUniverse`,
`strategiesDisabledByPlan` e `strategiesNotSupportedByBroker` — due liste separate
perche' le due esclusioni hanno cause opposte: una scelta operativa reversibile
contro un simbolo che quel broker non opera.

Il **report HTML** ha una scheda «Piano» con codice, broker, tabella, orari di tenuta e
commissione, piu' numero ed **elenco delle strategie attive** — quelle che hanno operato,
per nome di esecuzione. Le escluse non ci sono: il report deve dire cosa ha prodotto
quella equity, e i due conteggi delle esclusioni stanno nel summary. Un run senza piano
lo dichiara: il silenzio si leggerebbe come «il piano c'era, non l'abbiamo scritto».

## Spread all'ingresso

`SpreadBroker` carica la misura di un broker da `piootoo-repository/spread/{BROKER}/` e
`SpreadStatistic` sceglie quale colonna diventa il numero (mediana di default);
`SpreadPoints` scavalca la tabella un simbolo alla volta. Lo spread peggiora il **solo
prezzo di ingresso** — long `+spread`, short `-spread` — e lascia dove sono trigger,
livelli e uscite: stessa perdita quando lo stop salta, ma stop piu' vicino. Come il
datasource e' una **scelta separata** dal piano e dal `DatafeedBroker`: girare sul feed
interno con lo spread del broker e' il confronto che quantifica quel broker. Il run lo
dichiara in `fillConventions.spreadPoints` / `spreadSource` e in una scheda del report HTML.
Regole complete in [`spread-e-costo-di-transazione.md`](spread-e-costo-di-transazione.md).

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
