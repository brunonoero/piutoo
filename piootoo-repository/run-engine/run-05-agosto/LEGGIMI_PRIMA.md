# ES — le 6 strategie da implementare

Questa cartella è **tutto quello che serve**. Non servono le consegne delle singole run.

- **`dossier_ctrader_ES.html`** ← apri questo. È la specifica completa: fondamenta comuni
  (sessioni, vita dell'ordine, fill, costi, orari), le formule dei pattern per esteso, e
  una scheda per ognuna delle 6 strategie con entrata, filtri e uscite.
  `dossier_ctrader_ES.md` è lo stesso testo in markdown.
- **`trades/`** — la lista trade di riferimento di ogni strategia, fuori campione.
  È con questa che si verifica il port: **contano le entrate** (timestamp e prezzo),
  non i P&L.

## Le 6

| ID | TF | motore | atteso/trade | lista trade |
|---|---|---|---|---|
| S01 | 1h | BIASW | $390 | `trades/S01_1h_BIASW.csv` |
| S02 | 15m | BO | $268 | `trades/S02_15m_BO.csv` |
| S03 | 15m | BIASW | $193 | `trades/S03_15m_BIASW.csv` |
| S04 | 1h | PC | $181 | `trades/S04_1h_PC.csv` |
| S05 | 15m | BIASW | $148 | `trades/S05_15m_BIASW.csv` |
| S06 | 1h | PC | $77 | `trades/S06_1h_PC.csv` |

Nel dossier la riga "Verifica" di ogni scheda cita il percorso originale del file
(`run_.../consegna/trades/famNN_*.csv`): è lo stesso file, rinominato qui con l'ID della
strategia. La corrispondenza è quella della tabella sopra.

## Le due cose da non sbagliare

1. **Fuso orario.** Tutti gli orari sono ora dell'orologio **europeo, con l'ora legale
   europea** (Europe/Rome) — non UTC, non l'ora della borsa, non il server time del
   broker. Convertire prima di confrontare. Verifica: se le entrate sono sfalsate di
   un'ora **costante** è la conversione; se lo sono **solo in certi periodi dell'anno**
   è stato usato un offset fisso invece della zona.
2. **Backtest su tick, non su barre.** Su barre il simulatore di cTrader valuta lo stop
   contro la barra d'ingresso e chiude a prezzi mai esistiti (misurato su un port
   precedente: 201 trade su 359 uscivano nello stesso minuto del fill). Usare
   **Tick data (accurate)**.

## Vincolo operativo

Le 6 sono univoche: nessuna coppia condivide più del 70% degli ordini di entrata, e il
confronto è stato fatto **anche fra timeframe diversi**. Per questo sono 6 e non 8: la
BIASW a 1h (S01) e una delle BIASW a 15m emettevano gli stessi ordini, e così due delle
PC a 1h. Implementarle entrambe su conti diversi sarebbe copy trading.
