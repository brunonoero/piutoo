# Best plans

Un best plan è un backtest messo in evidenza perché ha funzionato bene. Si promuove dal
dettaglio del backtest (*Promuovi a best plan*) e si guarda dalla voce di menu **Best plans**,
che è l'unica lista della console **trasversale ai workspace**: il workspace di provenienza è
una colonna, non un filtro.

## È una copia, non un riferimento

La promozione fotografa il backtest in `[BasePath]\best-plans\{workspaceId}__{cartella}\`,
accanto a `workspaces/` (`PiootooSettings.BestPlansPath`, default `[BasePath]\best-plans`):

- `best-plan.json` — piano, workspace, origine, prezzi, versione, date (esecuzione, inizio,
  fine, promozione), cifre globali, **una riga per anno**, strategie con trade e P&L, curva di
  equity ridotta a 1400 colonne (primo, ultimo, minimo, massimo e drawdown peggiore di ogni
  colonna: la stessa riduzione del grafico globale del report HTML).
- `artifacts/` — `origin.json`, `backtest-summary.json` o `session-summary.json`,
  `trades.json` (letto dallo store, quindi con il journal fuso), il report HTML se c'è, e
  `plan.json`: il piano **com'è al momento della promozione**, non per forza com'era al run.

Il file di risultato `backtest_*.json` non si copia (arriva a 160 MB). Cancellare la cartella del
backtest non tocca il best plan; togliere il best plan non tocca il backtest. Promuovere di nuovo
lo stesso backtest è un `409` finché non si chiede di sostituire la fotografia.

## La promozione blocca il piano

Dopo la copia il piano del run viene bloccato (`TradingPlanService.Lock`): non si salva e non si
elimina più, e la console lo mostra con il lucchetto in lista (riga ambra) e con un banner nel
dettaglio, senza Salva. Per cambiarlo si usa *Duplica…* (lista o dettaglio): la copia ha un
codice nuovo, nasce sbloccata e porta tutto il resto dell'originale. Il blocco non si toglie
dalla console.

## Da dove viene la curva

- **Run interno** con `backtest_*.json`: `HourlyResults`, mark-to-market. Lo legge
  `HourlyEquityReader` a token e si ferma alla fine dell'array, prima di `StrategyResults`.
- **Run del cBot**, o interno interrotto: dai trade chiusi, un gradino per uscita. Il
  drawdown è misurato fra chiusure ed esce più basso.

`BestPlan.EquitySource` (`mark-to-market` / `realizzata`) lo dichiara ed è una colonna della
lista: due best plan con curve diverse non si confrontano sul drawdown senza guardarla.

## Cifre per anno

Aritmetica di `BacktestHtmlReport.AppendYearlySummaryHtml`: equity di inizio anno = fine
dell'anno prima (capitale iniziale per il primo), drawdown da un picco che parte da lì, rendimento
sull'equity di inizio anno, trade contati sull'anno di uscita. La riga della lista e la tabella
del report dello stesso run dicono lo stesso numero.

## Riferimenti codice

- `Piootoo.Shared/Models/BestPlans/BestPlanContracts.cs`
- `Piootoo.Core/Services/BestPlans/` — `BestPlanService`, `BestPlanPerformance`, `HourlyEquityReader`
- `PiootooApp.Server/Controllers/BestPlansController.cs` — `api/BestPlans`
- `piootooapp.clientform/Shell/Screens/BestPlanListScreen.cs`, `BestPlanDetailScreen.cs`,
  `Shell/Controls/EquityChart.cs`; pulsante in `BacktestDetailScreen`
- Test: `BestPlanServiceTests`
