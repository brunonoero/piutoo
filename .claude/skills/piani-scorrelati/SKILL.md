---
name: piani-scorrelati
description: >
  Comporre i piani di trading Piootoo (uno per conto e per istanza di cBot) da strategie scorrelate con
  piootoo-plan-builder: quali run dare in ingresso, i vincoli (correlazione, code, tetti per simbolo e
  famiglia, perdita giornaliera del conto), la lettura del resoconto e il passaggio al TradingPlan. Usala
  quando si chiede di "fare i piani", "raggruppare le strategie", "quali strategie insieme",
  "correlazione fra strategie" o di preparare i conti.
---

# Piani di strategie scorrelate

**Bozza del 25/09/2026.** Lo strumento: `Piootoo.PlanBuilder` (eseguibile `piootoo-plan-builder`),
logica in `Piootoo.Core/Planning/PlanBuilder.cs`, test in `PlanBuilderTests`. Il perche':
`docs/domini/catalogo-idee-pt6exo.md` §"Dalle strategie ai piani".

## Prima di lanciarlo

1. **Le candidate sono gia' giudicate.** Il costruttore non decide se una strategia ha un edge:
   entrano solo strategie passate dal metodo (`lettura-risultati`, e per le famiglie bizzarre
   `controllo-ran`).
2. **Un run neutro con le stesse size**: un backtest del catalogo senza piano (`PlanCode` null,
   `BalanceScale` 1), stesso feed e stesso periodo per tutte. Il costruttore somma il denaro dei trade
   come lo trova: run con size diverse danno piani falsi.
3. **Una strategia in un run solo**: se compare in due, lo strumento si ferma.
4. **Il periodo**: quello fuori campione, o l'anno del broker. Piani costruiti sul campione in cui le
   strategie sono state scelte sono ottimisti due volte.

## Lancio

```powershell
dotnet run --project Piootoo.PlanBuilder -- --run <cartella di backtest> [--run ...] --out piootoo-repository\ricerca\piani-<data> --plans 3 --max-strategies 8 --max-daily-loss <denaro>
```

Vincoli (default fra parentesi): `--plans` (3), `--max-strategies` (8), `--max-per-symbol` (2),
`--max-per-family` (2, la sigla del motore nel nome), `--max-corr` (0.3), `--max-tail-corr` (0.3),
`--tail` (0.10, la quota dei giorni peggiori), `--max-dd` e `--max-daily-loss` (le regole del conto, in
denaro: per FTMO la perdita giornaliera massima), `--min-trades` (30), `--from`/`--to`.

## Cosa fa

- P&L per giorno UTC di uscita; l'asse sono i soli giorni con almeno un trade.
- Correlazione di Pearson, e **nelle code**: la stessa sui soli giorni fra i peggiori dell'una o
  dell'altra. E' la seconda che conta per un conto: strategie scorrelate in media perdono spesso insieme
  nei crash. Le correlazioni negative passano sempre.
- Piani **disgiunti** e golosi: seme = la migliore per netto/DD, poi la candidata che porta il piano al
  miglior netto/DD rispettando correlazioni, tetti, drawdown e perdita giornaliera.

## Lettura del resoconto (`plan-builder.md`)

- Per ogni piano: netto, drawdown giornaliero, **giorno peggiore** (contro la regola del conto),
  correlazione massima fra i membri e nelle code.
- **Correlazione fra i piani**: piani quasi scorrelati sono il punto — una famiglia che smette di
  funzionare deve toccare un conto, non tutti.
- Le coppie piu' correlate nelle code: sono le strategie che non vanno mai nello stesso conto.
- Guardare i membri deboli: la scelta golosa puo' aggiungere una strategia con netto/DD basso se migliora
  quello del piano. Toglierla a mano e rilanciare con `--run` filtrati e' legittimo; dirlo nel rapporto.
- **Limite noto**: nessuna normalizzazione per rischio. GC pesa piu' di BP perche' il suo contratto vale
  di piu'. Se un piano e' dominato da un simbolo, e' da decidere una normalizzazione, non da ignorare.

## Dopo

Ogni piano diventa un `TradingPlan` (strategie, broker, `Holding`, commissione; vedi
`docs/domini/trading-plans.md`). Poi un backtest **con il piano** (`BacktestingRequest.PlanCode`), che
ne esegue universo, tenuta e costi, e solo dopo il conto. `plans.json` porta l'elenco delle strategie
per piano.
