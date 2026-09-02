### S57 · HK 4h · Trend following, asimmetrico  <a id='s57'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 4h |
| Motore | TF_U |
| Atteso/trade | $210 |
| P&L fuori campione | $55,872 |
| Drawdown | $5,638 |
| Trade | 174 |
| Stop loss | 39.0 pt |
| Take profit | 1,170.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 70: `C_d1 > O_d1`
- deve essere FALSO — fast 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`

*Solo SHORT*

- deve essere VERO — fast 119: `O_d0 < C_d1 * (1 - 0.0025)`
- deve essere FALSO — fast 6: `|O_d1-C_d1| > 0.5 * (H_d1-L_d1)`

**Quando può operare**

- Opera solo fra **00:00 e 06:00**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **39.00 pt**
- Take profit: **$7,500** = **1,170.05 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260828_1933/consegna/trades/fam01_TF_U.csv`

---

### S84 · HK 1h · Price channel (Donchian)  <a id='s84'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 1h |
| Motore | PC |
| Atteso/trade | $119 |
| P&L fuori campione | $42,574 |
| Drawdown | $15,670 |
| Trade | 258 |
| Stop loss | 468.0 pt |
| Take profit | 390.0 pt |

**Ordine STOP sul canale di Donchian a 30 barre**

- LONG: stop buy sul **massimo delle ultime 30 barre**
- SHORT: stop sell sul **minimo delle ultime 30 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`

*Solo LONG*

- deve essere FALSO — direzionale -3: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.75`

*Solo SHORT*

- deve essere FALSO — direzionale -3: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.75`

**Quando può operare**

- Opera solo fra **04:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$3,000** per contratto = **468.02 pt**
- Take profit: **$2,500** = **390.02 pt**
- Uscita a tempo dopo **48 barre** (2.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `HK_1h/consegna/trades/fam01_PC.csv`

---

### S108 · HK 15m · Bias intraday  <a id='s108'></a>

**LONG + SHORT** — Entra e esce a orari fissi della sessione.

| | |
|---|---|
| Timeframe | 15m |
| Motore | BIAS |
| Atteso/trade | $74 |
| P&L fuori campione | $69,026 |
| Drawdown | $17,700 |
| Trade | 620 |
| Stop loss | 780.0 pt |
| Take profit | 702.0 pt |

**Breakout dentro una finestra di barre della sessione**

- LONG: stop buy sul **massimo delle 2 barre precedenti**
- SHORT: stop sell sul **minimo delle 5 barre precedenti**
- L'ordine LONG esiste solo dalla barra **5** (inclusa) alla barra **46** (esclusa) della sessione; lo SHORT dalla **5** alla **46**.
- La finestra si **arma** alla sua barra di partenza, e solo se i filtri pattern sono veri in quel preciso momento. Una volta armata resta attiva fino a fine finestra, anche se i pattern smettono di essere veri.
- Se la barra di partenza è maggiore di quella di fine, la finestra attraversa il cambio di sessione.
- Gli estremi rolling si leggono su barre **già chiuse**.
- Le barre della sessione si contano da **0**: la prima barra dopo l'inizio sessione è la numero 0.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 147: `close < O_d0 * 1.01`
- deve essere FALSO — fast 117: `O_d0 < L_d1`

*Solo SHORT*

- deve essere VERO — fast 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`
- deve essere FALSO — fast 49: `(C_d1 > C_d2) E (C_d2 > C_d3) E (C_d3 > C_d4) E (C_d4 > C_d5)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- **Non apre** posizioni LONG di giovedì
- **Non apre** posizioni SHORT di giovedì
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Uscita **obbligatoria alla barra 69** della sessione per il LONG e alla barra **91** per lo SHORT, market all'apertura di quella barra.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$5,000** per contratto = **780.03 pt**
- Take profit: **$4,500** = **702.03 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260831_0158/consegna/trades/fam01_BIAS.csv`

> ⚠ **Non mettere su conti diversi** insieme a `15m fam01-2`: emettono gli stessi ordini di entrata.

---

### S109 · HK 4h · Price channel (Donchian)  <a id='s109'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $72 |
| P&L fuori campione | $80,681 |
| Drawdown | $4,804 |
| Trade | 387 |
| Stop loss | 39.0 pt |
| Take profit | 1,170.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre** + 2 tick (2 pt)
- SHORT: stop sell sul **minimo delle ultime 1 barre** − 2 tick (2 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere FALSO — neutrale 35: `(H_d0-L_d0) > L_d0 * 0.02`

*Solo LONG*

- deve essere VERO — direzionale 27: `L_d0 > L_d1`
- deve essere FALSO — direzionale -50: `close < O_d0 * 0.995`

*Solo SHORT*

- deve essere VERO — direzionale 27: `H_d0 < H_d1`
- deve essere FALSO — direzionale -50: `close > O_d0 * 1.005`

**Quando può operare**

- Opera solo fra **00:00 e 06:00**, ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **39.00 pt**
- Take profit: **$7,500** = **1,170.05 pt**
- Trailing stop: **$1,000** = **156.01 pt**
- Breakeven a **$500** = **78.00 pt** di utile
- Uscita a tempo dopo **24 barre** (4.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260828_1933/consegna/trades/fam02_PC.csv`

---

### S111 · HK 4h · Breakout su N sessioni  <a id='s111'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 4h |
| Motore | BO |
| Atteso/trade | $71 |
| P&L fuori campione | $53,573 |
| Drawdown | $5,657 |
| Trade | 219 |
| Stop loss | 39.0 pt |
| Take profit | 624.0 pt |

**Ordine STOP sul canale a 1 sessioni**

- LONG: stop buy sul **massimo delle ultime 1 sessioni complete** e del massimo/minimo della sessione corrente **escludendo la barra in corso** + 10 tick (10 pt)
- SHORT: stop sell sul **minimo delle ultime 1 sessioni complete** e del massimo/minimo della sessione corrente **escludendo la barra in corso** − 10 tick (10 pt)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`
- deve essere FALSO — neutrale 52: `(H_d0 > H_d1) E (L_d0 < L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 2: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — direzionale -45: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT*

- deve essere VERO — direzionale 2: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.5`
- deve essere FALSO — direzionale -45: `(C_d1 > O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **00:00 e 09:00**, ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **39.00 pt**
- Take profit: **$4,000** = **624.02 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260828_1933/consegna/trades/fam03_BO.csv`

---

