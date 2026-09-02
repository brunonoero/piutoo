### S37 · JY 30m · Trend following, asimmetrico  <a id='s37'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_U |
| Atteso/trade | $317 |
| P&L fuori campione | $36,346 |
| Drawdown | $7,730 |
| Trade | 77 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 55: `H_d0 > L_d0 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — fast 100: `L_d0 > L_d1`
- deve essere FALSO — fast 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`

**Quando può operare**

- Opera solo fra **04:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **0.01 pt**
- Take profit: **$3,000** = **0.02 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `JY_30m/consegna/trades/fam01_TF_U.csv`

> ⚠ **Non mettere su conti diversi** insieme a `30m fam01-2`: emettono gli stessi ordini di entrata.

---

### S39 · JY 30m · Trend following, asimmetrico  <a id='s39'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_U |
| Atteso/trade | $309 |
| P&L fuori campione | $31,279 |
| Drawdown | $7,005 |
| Trade | 68 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 139: `(C_d1 < O_d1) E (C_d2 < O_d2)`
- deve essere FALSO — fast 115: `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)`

*Solo SHORT*

- deve essere VERO — fast 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`
- deve essere FALSO — fast 149: `close < O_d0`

**Quando può operare**

- Opera solo fra **13:00 e 08:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$750** per contratto = **0.01 pt**
- Take profit: **$3,000** = **0.02 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `JY_30m/consegna/trades/fam02_TF_U.csv`

---

### S49 · JY 30m · Trend following, asimmetrico  <a id='s49'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 30m |
| Motore | TF_U |
| Atteso/trade | $253 |
| P&L fuori campione | $40,309 |
| Drawdown | $5,283 |
| Trade | 107 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 139: `(C_d1 < O_d1) E (C_d2 < O_d2)`
- deve essere FALSO — fast 30: `|O_d5-C_d1| > 0.75 * (HH5-LL5)`

*Solo SHORT*

- deve essere VERO — fast 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`
- deve essere FALSO — fast 111: `(L_d1 > L_d2) E (L_d1 > L_d3) E (L_d1 > L_d4)`

**Quando può operare**

- Opera solo fra **03:00 e 19:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$750** per contratto = **0.01 pt**
- Take profit: **$2,500** = **0.02 pt**
- Uscita a tempo dopo **460 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `JY_30m/consegna/trades/fam03_TF_U.csv`

---

### S69 · JY 1h · Trend following, asimmetrico  <a id='s69'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $165 |
| P&L fuori campione | $46,215 |
| Drawdown | $13,369 |
| Trade | 72 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 26: `|O_d5-C_d1| < 0.75 * (HH5-LL5)`
- deve essere FALSO — fast 111: `(L_d1 > L_d2) E (L_d1 > L_d3) E (L_d1 > L_d4)`

*Solo SHORT*

- deve essere VERO — fast 133: `(H_d1 < H_d2) O (L_d1 > L_d2)`
- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **07:00 e 01:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **0.02 pt**
- Take profit: **$2,000** = **0.02 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `JY_1h/consegna/trades/fam01_TF_U.csv`

---

### S74 · JY 1h · Trend following, asimmetrico  <a id='s74'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $149 |
| P&L fuori campione | $37,563 |
| Drawdown | $9,556 |
| Trade | 65 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 26: `|O_d5-C_d1| < 0.75 * (HH5-LL5)`
- deve essere FALSO — fast 111: `(L_d1 > L_d2) E (L_d1 > L_d3) E (L_d1 > L_d4)`

*Solo SHORT*

- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **07:00 e 01:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,000** per contratto = **0.02 pt**
- Take profit: **$2,500** = **0.02 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `JY_1h/consegna/trades/fam02_TF_U.csv`

---

### S104 · JY 1h · Trend following, asimmetrico  <a id='s104'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $81 |
| P&L fuori campione | $34,293 |
| Drawdown | $9,639 |
| Trade | 109 |
| Stop loss | 0.0 pt |
| Take profit | 0.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 55: `H_d0 > L_d0 * (1 + 0.01)`
- deve essere FALSO — fast 85: `H_d0 > H_d1 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — fast 18: `|O_d5-C_d1| > 0.75 * (H_d5-L_d1)`
- deve essere FALSO — fast 95: `L_d0 < L_d1`

**Quando può operare**

- Opera solo fra **13:00 e 10:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$5,000** per contratto = **0.04 pt**
- Take profit: **$3,000** = **0.02 pt**
- Uscita a tempo dopo **161 barre** (6.7 giorni di calendario)

**Verifica** — lista trade di riferimento: `JY_1h/consegna/trades/fam03_TF_U.csv`

---

