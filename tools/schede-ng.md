### S45 · NG 1h · Trend following, asimmetrico  <a id='s45'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $278 |
| P&L fuori campione | $83,108 |
| Drawdown | $20,122 |
| Trade | 282 |
| Stop loss | 0.1 pt |
| Take profit | 0.2 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 145: `close > O_d0 * 1.005`
- deve essere FALSO — fast 129: `((H_d1-L_d1) < H_d2 - L_d2) E (H_d2 - L_d2 < H_d3 - L_d3)`

*Solo SHORT*

- deve essere VERO — fast 64: `H_d0 < L_d0 * (1 + 0.02)`
- deve essere FALSO — fast 73: `C_d1 < C_d2 * (1 - 0.015)`

**Quando può operare**

- Opera solo fra **08:00 e 23:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,500** per contratto = **0.15 pt**
- Take profit: **$2,500** = **0.25 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `NG_1h/consegna/trades/fam01_TF_U.csv`

---

### S59 · NG 30m · Trend following, simmetrico  <a id='s59'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_M |
| Atteso/trade | $200 |
| P&L fuori campione | $44,814 |
| Drawdown | $14,670 |
| Trade | 196 |
| Stop loss | 0.1 pt |
| Take profit | 0.3 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 4: `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 33: `(H_d0-L_d0) > L_d0 * 0.01`

*Solo LONG*

- deve essere FALSO — direzionale -37: `(C_d1 < C_d2) E (C_d2 < C_d3) E (O_d0 < C_d1)`

*Solo SHORT*

- deve essere FALSO — direzionale -37: `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)`

**Quando può operare**

- Opera solo fra **22:00 e 17:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$750** per contratto = **0.07 pt**
- Take profit: **$3,000** = **0.30 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `NG_30m/consegna/trades/fam01_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `30m fam01-2`: emettono gli stessi ordini di entrata.

---

### S68 · NG 1h · Trend following, simmetrico  <a id='s68'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $168 |
| P&L fuori campione | $26,494 |
| Drawdown | $6,644 |
| Trade | 76 |
| Stop loss | 0.1 pt |
| Take profit | 0.3 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 29: `|O_d5-C_d1| > 0.5 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale -9: `O_d0 - L_d0 < O_d1 - L_d1`
- deve essere FALSO — direzionale 46: `(C_d1 > O_d1) E (C_d2 < O_d2)`

*Solo SHORT*

- deve essere VERO — direzionale -9: `H_d0 - O_d0 < H_d1 - O_d1`
- deve essere FALSO — direzionale 46: `(C_d1 < O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **02:00 e 14:00**, ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,500** per contratto = **0.15 pt**
- Take profit: **$3,000** = **0.30 pt**
- Uscita a tempo dopo **12 barre** (12 ore)

**Verifica** — lista trade di riferimento: `NG_1h/consegna/trades/fam02_TF_M.csv`

> ⚠ **Non mettere su conti diversi** insieme a `1h fam02-2`: emettono gli stessi ordini di entrata.

---

### S70 · NG 30m · Trend following, simmetrico  <a id='s70'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_M |
| Atteso/trade | $162 |
| P&L fuori campione | $63,274 |
| Drawdown | $17,454 |
| Trade | 341 |
| Stop loss | 0.1 pt |
| Take profit | 0.3 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 26: `|O_d5-C_d1| < 0.75 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale 3: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75`
- deve essere FALSO — direzionale -37: `(C_d1 < C_d2) E (C_d2 < C_d3) E (O_d0 < C_d1)`

*Solo SHORT*

- deve essere VERO — direzionale 3: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.75`
- deve essere FALSO — direzionale -37: `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)`

**Quando può operare**

- Opera solo fra **03:00 e 23:59**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **0.10 pt**
- Take profit: **$3,000** = **0.30 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `NG_30m/consegna/trades/fam02_TF_M.csv`

---

### S95 · NG 30m · Bias settimanale  <a id='s95'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 30m |
| Motore | BIASW |
| Atteso/trade | $94 |
| P&L fuori campione | $18,000 |
| Drawdown | $8,222 |
| Trade | 75 |
| Stop loss | 0.5 pt |
| Take profit | 0.1 pt |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET all'apertura della barra delle 20:00 di lunedì**
- SHORT: **MARKET all'apertura della barra delle 23:00 di giovedì**
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 63: `H_d0 < L_d0 * (1 + 0.015)`
- deve essere FALSO — fast 134: `(H_d2 < H_d1) E (L_d2 > L_d1)`

*Solo SHORT*

- deve essere VERO — fast 132: `L_d1 > L_d2`
- deve essere FALSO — fast 99: `L_d0 < L_d1 * (1 - 0.01)`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **giovedì alle 02:00**, market all'apertura di quella barra.
- Uscita SHORT: **martedì alle 00:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$5,000** per contratto = **0.50 pt**
- Take profit: **$500** = **0.05 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `NG_30m/consegna/trades/fam03_BIASW.csv`

---

### S101 · NG 1h · Trend following, simmetrico  <a id='s101'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $90 |
| P&L fuori campione | $54,782 |
| Drawdown | $23,166 |
| Trade | 293 |
| Stop loss | 0.1 pt |
| Take profit | 0.3 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 53: `(H_d1-L_d1) < (H_d2-L_d2)`

*Solo LONG*

- deve essere FALSO — direzionale 7: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`

*Solo SHORT*

- deve essere FALSO — direzionale 7: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5`

**Quando può operare**

- Opera solo fra **00:00 e 22:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,250** per contratto = **0.12 pt**
- Take profit: **$3,000** = **0.30 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `NG_1h/consegna/trades/fam03_TF_M.csv`

---

