# last-backtest — cartella di lavoro, nessun esito registrato

Scarico dell'ultimo run del cBot, non un confronto numerato: sopravvive fino allo
scarico successivo. **Non appoggiarci un'analisi**: copiala in un `compare-NNNN/` prima.

Cosa c'è dentro, letto dagli artefatti (non da un'analisi):

- `trades-internal.json` — 166 trade, solo GC, 14/01/2022 → 28/12/2023, +50.076 USD grezzi.
- `trades-external.json` — 110 trade, solo GC, 14/01/2022 → 29/12/2023, +67.092 grezzi
  **in valuta del conto, non convertiti**.
- `log.txt` — log del cBot: `PiootooDistributedExecutionBot` su XAUUSD m5, conto 1075035,
  piano `X-AU-TEST`, server `http://localhost:5000`. In backtest il livello di log scende
  a Minimo, quindi le righe dell'avvio non ci sono.
- l'`.xlsx` — eventi cTrader esportati dalla piattaforma.
- `web.config` — finito qui per sbaglio, non c'entra col confronto.

I due saldi non sono confrontabili così: valuta diversa e nessuna finestra comune
applicata. Manca `origin.json` da entrambi i lati, quindi il broker e la serie di prezzi
del lato esterno sono ignoti: **il confronto non è chiudibile con questo materiale**.

Contesto: è materiale dell'indagine sulla divergenza GC (`@GC` retro-aggiustato contro
XAUUSD grezzo, offset ~350 punti nel 2022). Vedi `docs/domini/` e le note dell'indagine.
