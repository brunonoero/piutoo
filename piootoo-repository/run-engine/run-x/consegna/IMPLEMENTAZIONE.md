# NQ su cTrader — specifica di implementazione

*Generato il 20 agosto 2026 da `run_20260820_0856`.*

**6 strategie univoche** da implementare come cBot, ricavate da 1 timeframe (1h). Ogni strategia è definita qui per intero: condizioni di entrata, filtri, uscite, e la lista trade con cui verificare il port. Non serve conoscere il trading per implementarle — serve rispettare le regole della sezione 2 alla lettera.

> **Le tre cose che fanno fallire un port.** In ordine di frequenza: le sessioni ricostruite male (§2.1), l'ordine lasciato vivo più di una barra (§2.2), e il backtest fatto su barre invece che su tick (§5).

---

## 1. Cosa si costruisce

6 cBot indipendenti. Ognuno opera su un solo strumento e un solo timeframe, con un contratto per posizione. Non comunicano fra loro.

| ID | TF | Motore | Atteso/trade | P&L OOS | Drawdown | Trade | Equivalenti |
|---|---|---|---|---|---|---|---|
| [S01](#s01) | 1h | TF_M | $583 | $175,532 | $5,774 | 287 | — |
| [S02](#s02) | 1h | TF_M | $422 | $49,592 | $20,581 | 112 | — |
| [S03](#s03) | 1h | BO | $283 | $39,469 | $18,908 | 59 | — |
| [S04](#s04) | 1h | TF_U | $250 | $203,832 | $22,478 | 232 | — |
| [S05](#s05) | 1h | TF_M | $242 | $45,941 | $15,665 | 181 | — |
| [S06](#s06) | 1h | TF_U | $91 | $68,525 | $24,928 | 215 | — |

La colonna **Equivalenti** elenca le strategie che emettono gli stessi ordini di entrata: trovate separatamente, ma sono lo stesso sistema. Se ne implementa una sola.

### Da dove vengono i numeri

| Timeframe | Righe approvate | Strategie | Univoche |
|---|---|---|---|
| 1h | 6 | 6 | 6 |

Le **righe approvate** includono la stessa strategia con stop e target diversi: non sono sistemi distinti e non compaiono qui. Le **univoche** restano dopo aver confrontato le entrate anche *fra* timeframe diversi.

---

## 2. Fondamenta comuni

Questa parte si scrive una volta e si riusa in tutti i 6 cBot. Ogni regola è vincolante: cambiarne una fa divergere il port dalla ricerca.

### 2.1 Sessioni

Le condizioni usano massimi e minimi di **sessione**, non di barra. Le sessioni si ricostruiscono dalle barre intraday, **non** si leggono dalle candele giornaliere del broker.

- Una sessione inizia alle **00:00 CET** e dura fino all'inizio della successiva.
- `H_d1`, `L_d1`, `O_d1`, `C_d1` = massimo, minimo, apertura e chiusura della sessione **precedente**.
- `H_d2` … `H_d5` = le quattro sessioni ancora prima.
- `H_d0`, `L_d0`, `O_d0` = massimo, minimo e apertura della sessione **corrente**, sulle sole barre già **chiuse**.
- `HH5` = massimo di `H_d1..H_d5`; `LL5` = minimo di `L_d1..L_d5`.
- `close` (minuscolo) = chiusura della **barra** corrente, non della sessione.

### 2.2 Ciclo di vita dell'ordine

- Le condizioni si valutano **alla chiusura della barra**.
- L'ordine emesso vive **una sola barra**: alla barra successiva va cancellato e, se le condizioni reggono, ri-emesso. In cTrader: *cancel & replace* ad ogni `OnBar`.
- Nessuna condizione può usare il prezzo della barra su cui si entra.
- Al massimo **una entrata per sessione e per direzione**. Se la posizione si chiude dentro la stessa sessione, non si rientra sullo stesso lato.

### 2.3 Riempimento

- **Ordine stop** (rottura di un livello): riempie a `max(apertura, livello)` per il long, `min(apertura, livello)` per lo short. Se il prezzo apre già oltre il livello, il fill è all'apertura — mai al livello superato.
- **Ordine limite** (ritorno alla media): serve una penetrazione **stretta** del livello (`minimo < livello` per il long). Il semplice tocco non riempie.

### 2.4 Costi

Tutti i risultati sono al netto di **$4.00 di commissione per trade** e **1 tick di slippage per lato**. Un backtest senza questi costi non è confrontabile.

### 2.5 Unità

La ricerca esprime stop e target in **dollari per contratto**; cTrader li vuole in **punti**. Su questo strumento 1 punto = **$20** e 1 tick = **0.25 punti**. Nelle schede i valori sono già convertiti.

### 2.6 Orari

Le finestre orarie sono in **ora dei dati (CET)** e si valutano sulla chiusura della barra. Una finestra il cui orario di fine è minore di quello di inizio attraversa la mezzanotte e va gestita come tale.

---

## 3. Le condizioni di pattern

I filtri sono condizioni booleane sulle grandezze di sessione della §2.1. Qui tutte quelle usate dalle 6 strategie, già in forma di formula.

| Riferimento | Condizione |
|---|---|
| `direzionale -34` | `H_d1 < H_d5` |
| `direzionale -34` | `L_d1 > L_d5` |
| `direzionale 16` | `C_d1 < C_d2 * (1 - 0.01)` |
| `direzionale 16` | `C_d1 > C_d2 * (1 + 0.01)` |
| `direzionale 28` | `H_d0 < H_d1 * (1 - 0.005)` |
| `direzionale 28` | `L_d0 > L_d1 * (1 + 0.005)` |
| `direzionale 44` | `H_d1 < H_d2` |
| `direzionale 44` | `L_d1 > L_d2` |
| `direzionale 50` | `close < O_d0 * 0.995` |
| `direzionale 50` | `close > O_d0 * 1.005` |
| `direzionale 8` | `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0` |
| `direzionale 8` | `O_d0 - L_d0 > (O_d1 - L_d1) * 3.0` |
| `fast 107` | `L_d1 > L_d5` |
| `fast 137` | `(C_d1 < O_d1) E (C_d2 > O_d2)` |
| `fast 2` | `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)` |
| `fast 21` | `|O_d5-C_d1| > 2.0 * (H_d5-L_d1)` |
| `fast 32` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5` |
| `fast 38` | `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0` |
| `fast 39` | `H_d0 - O_d0 < H_d1 - O_d1` |
| `fast 83` | `H_d0 > H_d1 * (1 + 0.005)` |
| `neutrale 1` | `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)` |
| `neutrale 11` | `|O_d5-C_d1| < 0.5 * (H_d5-L_d1)` |
| `neutrale 24` | `|O_d5-C_d1| < 0.25 * (HH5-LL5)` |
| `neutrale 32` | `(H_d0-L_d0) > L_d0 * 0.0075` |
| `neutrale 4` | `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)` |
| `neutrale 47` | `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2` |

**deve essere VERO** = condizione requisito per entrare. **deve essere FALSO** = l'entrata è vietata quando la condizione si verifica. Le voci marcate *nessun filtro* sono assenti e non vanno implementate.

---

## 4. Le 6 strategie

### S01 · NQ 1h · Trend following, simmetrico  <a id='s01'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $583 |
| P&L fuori campione | $175,532 |
| Drawdown | $5,774 |
| Trade | 287 |
| Stop loss | 12.5 pt |
| Take profit | 150.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 50: `close > O_d0 * 1.005`
- deve essere FALSO — direzionale 8: `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0`

*Solo SHORT*

- deve essere VERO — direzionale 50: `close < O_d0 * 0.995`
- deve essere FALSO — direzionale 8: `O_d0 - L_d0 > (O_d1 - L_d1) * 3.0`

**Quando può operare**

- Opera solo fra **14:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$250** per contratto = **12.50 pt**
- Take profit: **$3,000** = **150.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam01_TF_M.csv`

---

### S02 · NQ 1h · Trend following, simmetrico  <a id='s02'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $422 |
| P&L fuori campione | $49,592 |
| Drawdown | $20,581 |
| Trade | 112 |
| Stop loss | 125.0 pt |
| Take profit | 250.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 11: `|O_d5-C_d1| < 0.5 * (H_d5-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **00:00 e 17:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,500** per contratto = **125.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **48 barre** (2.0 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam02_TF_M.csv`

---

### S03 · NQ 1h · Breakout su N sessioni  <a id='s03'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 1h |
| Motore | BO |
| Atteso/trade | $283 |
| P&L fuori campione | $39,469 |
| Drawdown | $18,908 |
| Trade | 59 |
| Stop loss | 25.0 pt |
| Take profit | — |

**Ordine STOP sul canale a 4 sessioni**

- LONG: stop buy sul **massimo delle ultime 4 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 4 sessioni complete**

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 4: `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 32: `(H_d0-L_d0) > L_d0 * 0.0075`

*Solo LONG*

- deve essere VERO — direzionale 44: `L_d1 > L_d2`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — direzionale 44: `H_d1 < H_d2`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

**Quando può operare**

- Opera solo fra **22:00 e 21:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam03_BO.csv`

---

### S04 · NQ 1h · Trend following, asimmetrico  <a id='s04'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $250 |
| P&L fuori campione | $203,832 |
| Drawdown | $22,478 |
| Trade | 232 |
| Stop loss | 37.5 pt |
| Take profit | 500.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 32: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — fast 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`

*Solo SHORT*

- deve essere VERO — fast 38: `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0`
- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

**Quando può operare**

- Opera solo fra **17:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$750** per contratto = **37.50 pt**
- Take profit: **$10,000** = **500.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam04_TF_U.csv`

---

### S05 · NQ 1h · Trend following, simmetrico  <a id='s05'></a>

**LONG + SHORT** — Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_M |
| Atteso/trade | $242 |
| P&L fuori campione | $45,941 |
| Drawdown | $15,665 |
| Trade | 181 |
| Stop loss | 62.5 pt |
| Take profit | 200.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

*Solo SHORT*

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 16: `C_d1 < C_d2 * (1 - 0.01)`

**Quando può operare**

- Opera solo fra **21:00 e 14:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,250** per contratto = **62.50 pt**
- Take profit: **$4,000** = **200.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam05_TF_M.csv`

---

### S06 · NQ 1h · Trend following, asimmetrico  <a id='s06'></a>

**LONG + SHORT** — Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

| | |
|---|---|
| Timeframe | 1h |
| Motore | TF_U |
| Atteso/trade | $91 |
| P&L fuori campione | $68,525 |
| Drawdown | $24,928 |
| Trade | 215 |
| Stop loss | 200.0 pt |
| Take profit | 250.0 pt |

**Ordine STOP sugli estremi della sessione precedente**

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 107: `L_d1 > L_d5`
- deve essere FALSO — fast 83: `H_d0 > H_d1 * (1 + 0.005)`

*Solo SHORT*

- deve essere VERO — fast 21: `|O_d5-C_d1| > 2.0 * (H_d5-L_d1)`
- deve essere FALSO — fast 39: `H_d0 - O_d0 < H_d1 - O_d1`

**Quando può operare**

- Opera solo fra **16:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **46 barre** (1.9 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260820_0856/consegna/trades/fam06_TF_U.csv`

---

## 5. Come si verifica un port

Il port è corretto quando le **entrate** coincidono — timestamp e prezzo. I P&L sono una conseguenza.

1. Backtest sullo stesso periodo della lista di riferimento, **su dati tick**.
2. Confronta le entrate: devono coincidere al minuto e al prezzo.
3. Se le **entrate** non coincidono, il problema è nelle condizioni o nella ricostruzione delle sessioni (§2.1). Isola stampando `H_d1` e `L_d1` per qualche giorno.
4. Se le entrate coincidono ma i **P&L** no, il problema è nelle uscite o nei costi (§2.3, §2.4).

> ⚠ **Il backtest su barre non è attendibile per queste strategie.** Su dati a barre il simulatore di cTrader valuta lo stop anche contro la barra d'ingresso, percorso pre-entrata incluso, e chiude a prezzi mai esistiti. Su un port già fatto, 201 trade su 359 uscivano nello stesso minuto del fill, con slippage medio di 23 punti oltre lo stop. Usare **Tick data (accurate)**.

---

## 6. Vincolo operativo

Le 6 strategie sono univoche: nessuna coppia condivide più del 70% degli ordini di entrata. È questo che rende lecito farle girare su conti separati — due sistemi che mandano gli stessi ordini sono copy trading, e presso una prop firm costano il conto.

Il vincolo si misura sulle **entrate**, non sulla correlazione dei risultati: due strategie possono avere P&L molto diversi e mandare gli stessi ordini.

*Dati fuori campione dal 12/05/2021 al 30/05/2025. Criteri di selezione in `METODO_RICERCA.md`.*