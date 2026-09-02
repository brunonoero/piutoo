### S80 · CT 4h · Trend following, asimmetrico  <a id='s80'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_U |
| Atteso/trade | $133 |
| P&L fuori campione | $37,111 |
| Drawdown | $14,835 |
| Trade | 88 |
| Stop loss | 6.0 pt |
| Take profit | 3.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 5: `|O_d1-C_d1| > 0.25 * (H_d1-L_d1)`
- deve essere FALSO — fast 114: `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)`

*Solo SHORT*

- deve essere VERO — fast 150: `close < O_d0 * 0.995`
- deve essere FALSO — fast 41: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.5`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$3,000** per contratto = **6.00 pt**
- Take profit: **$1,500** = **3.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `CT_4h/consegna/trades/fam01_TF_U.csv`

---

### S82 · YM 1h · Trend following, asimmetrico  <a id='s82'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $128 |
| P&L fuori campione | $92,720 |
| Drawdown | $22,253 |
| Trade | 170 |
| Stop loss | 500.0 pt |
| Take profit | 800.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 4: `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)`
- deve essere FALSO — fast 138: `(C_d1 > O_d1) E (C_d2 < O_d2)`

*Solo SHORT*

- deve essere VERO — fast 33: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75`
- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **19:00 e 17:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **500.00 pt**
- Take profit: **$4,000** = **800.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `YM_1h/consegna/trades/fam01_TF_U.csv`

---

### S83 · YM 1h · Trend following, asimmetrico  <a id='s83'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $120 |
| P&L fuori campione | $49,462 |
| Drawdown | $13,246 |
| Trade | 97 |
| Stop loss | 200.0 pt |
| Take profit | 1,200.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 136: `(C_d1 > O_d1) E (C_d2 > O_d2)`
- deve essere FALSO — fast 78: `C_d1 > C_d2 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — fast 136: `(C_d1 > O_d1) E (C_d2 > O_d2)`
- deve essere FALSO — fast 53: `H_d0 > L_d0 * (1 + 0.005)`

**Quando può operare**

- Opera solo fra **12:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,000** per contratto = **200.00 pt**
- Take profit: **$6,000** = **1,200.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `YM_1h/consegna/trades/fam02_TF_U.csv`

---

### S89 · SB 4h · Trend following, simmetrico  <a id='s89'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_M |
| Atteso/trade | $112 |
| P&L fuori campione | $34,954 |
| Drawdown | $4,344 |
| Trade | 104 |
| Stop loss | 2.0 pt |
| Take profit | 2.7 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 34: `(H_d0-L_d0) > L_d0 * 0.015`
- deve essere FALSO — neutrale 22: `|O_d5-C_d1| > 2.5 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale -11: `(C_d1 < C_d2) E (C_d2 < C_d3) E (C_d3 < C_d4) E (C_d4 < C_d5)`

*Solo SHORT*

- deve essere VERO — direzionale 48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale -11: `(C_d1 > C_d2) E (C_d2 > C_d3) E (C_d3 > C_d4) E (C_d4 > C_d5)`

**Quando può operare**

- Opera solo fra **13:00 e 23:59**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,250** per contratto = **2.01 pt**
- Take profit: **$3,000** = **2.68 pt**
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `SB_4h/consegna/trades/fam01_TF_M.csv`

---

### S105 · YM 1h · Trend following, asimmetrico  <a id='s105'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $81 |
| P&L fuori campione | $62,853 |
| Drawdown | $7,696 |
| Trade | 183 |
| Stop loss | 50.0 pt |
| Take profit | 1,000.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 136: `(C_d1 > O_d1) E (C_d2 > O_d2)`
- deve essere FALSO — fast 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

*Solo SHORT*

- deve essere VERO — fast 136: `(C_d1 > O_d1) E (C_d2 > O_d2)`
- deve essere FALSO — fast 95: `L_d0 < L_d1`

**Quando può operare**

- Opera solo fra **01:00 e 21:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **50.00 pt**
- Take profit: **$5,000** = **1,000.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `YM_1h/consegna/trades/fam02_TF_U.csv`

---

