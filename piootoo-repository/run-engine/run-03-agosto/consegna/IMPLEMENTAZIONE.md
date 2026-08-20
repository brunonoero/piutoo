# GC su cTrader — specifica di implementazione

*Generato il 19 agosto 2026 da `run_20260819_0659`.*

**3 strategie univoche** da implementare come cBot, ricavate da 1 timeframe (1h). Ogni strategia è definita qui per intero: condizioni di entrata, filtri, uscite, e la lista trade con cui verificare il port. Non serve conoscere il trading per implementarle — serve rispettare le regole della sezione 2 alla lettera.

> **Le tre cose che fanno fallire un port.** In ordine di frequenza: le sessioni ricostruite male (§2.1), l'ordine lasciato vivo più di una barra (§2.2), e il backtest fatto su barre invece che su tick (§5).

---

## 1. Cosa si costruisce

3 cBot indipendenti. Ognuno opera su un solo strumento e un solo timeframe, con un contratto per posizione. Non comunicano fra loro.

| ID | TF | Motore | Atteso/trade | P&L OOS | Drawdown | Trade | Equivalenti |
|---|---|---|---|---|---|---|---|
| [S01](#s01) | 1h | PC | $215 | $60,204 | $19,202 | 81 | 1h fam01-2, 1h fam01-3 |
| [S02](#s02) | 1h | RHL | $140 | $31,320 | $11,584 | 75 | — |
| [S03](#s03) | 1h | RHL | $92 | $21,820 | $7,516 | 80 | — |

La colonna **Equivalenti** elenca le strategie che emettono gli stessi ordini di entrata: trovate separatamente, ma sono lo stesso sistema. Se ne implementa una sola.

### Da dove vengono i numeri

| Timeframe | Righe approvate | Strategie | Univoche |
|---|---|---|---|
| 1h | 5 | 5 | 3 |

Le **righe approvate** includono la stessa strategia con stop e target diversi: non sono sistemi distinti e non compaiono qui. Le **univoche** restano dopo aver confrontato le entrate anche *fra* timeframe diversi.

---

## 2. Fondamenta comuni

Questa parte si scrive una volta e si riusa in tutti i 3 cBot. Ogni regola è vincolante: cambiarne una fa divergere il port dalla ricerca.

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

Tutti i risultati sono al netto di **$6.00 di commissione per trade** e **1 tick di slippage per lato**. Un backtest senza questi costi non è confrontabile.

### 2.5 Unità

La ricerca esprime stop e target in **dollari per contratto**; cTrader li vuole in **punti**. Su questo strumento 1 punto = **$100** e 1 tick = **0.1 punti**. Nelle schede i valori sono già convertiti.

### 2.6 Orari

Le finestre orarie sono in **ora dei dati (CET)** e si valutano sulla chiusura della barra. Una finestra il cui orario di fine è minore di quello di inizio attraversa la mezzanotte e va gestita come tale.

---

## 3. Le condizioni di pattern

I filtri sono condizioni booleane sulle grandezze di sessione della §2.1. Qui tutte quelle usate dalle 3 strategie, già in forma di formula.

| Riferimento | Condizione |
|---|---|
| `direzionale -1` | `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25` |
| `direzionale -1` | `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25` |
| `direzionale -14` | `C_d1 < O_d1` |
| `direzionale -14` | `C_d1 > O_d1` |
| `direzionale -21` | `H_d0 > H_d1` |
| `direzionale -21` | `L_d0 < L_d1` |
| `direzionale -5` | `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5` |
| `direzionale -5` | `O_d0 - L_d0 > (O_d1 - L_d1) * 1.5` |
| `neutrale 12` | `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)` |
| `neutrale 2` | `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)` |
| `neutrale 30` | `|O_d5-C_d1| > 0.75 * (HH5-LL5)` |
| `neutrale 46` | `(H_d0 < H_d1) E (L_d0 > L_d1)` |

**deve essere VERO** = condizione requisito per entrare. **deve essere FALSO** = l'entrata è vietata quando la condizione si verifica. Le voci marcate *nessun filtro* sono assenti e non vanno implementate.

---

## 4. Le 3 strategie

### S01 · GC 1h · Price channel (Donchian)  <a id='s01'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 1h |
| Motore | PC |
| Atteso/trade | $215 |
| P&L fuori campione | $60,204 |
| Drawdown | $19,202 |
| Trade | 81 |
| Stop loss | 22.5 pt |
| Take profit | 40.0 pt |

**Ordine STOP sul canale di Donchian a 30 barre**

- LONG: stop buy sul **massimo delle ultime 30 barre** + 2 tick (0.2 pt)
- SHORT: stop sell sul **minimo delle ultime 30 barre** − 2 tick (0.2 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 30: `|O_d5-C_d1| > 0.75 * (HH5-LL5)`

*Solo LONG*

- deve essere VERO — direzionale -14: `C_d1 < O_d1`
- deve essere FALSO — direzionale -21: `L_d0 < L_d1`

*Solo SHORT*

- deve essere VERO — direzionale -14: `C_d1 > O_d1`
- deve essere FALSO — direzionale -21: `H_d0 > H_d1`

**Quando può operare**

- Opera solo fra **06:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,250** per contratto = **22.50 pt**
- Take profit: **$4,000** = **40.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260819_0659/consegna/trades/fam01_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `1h fam01-2`, `1h fam01-3`: emettono gli stessi ordini di entrata.

---

### S02 · GC 1h · Ritorno alla media sugli estremi  <a id='s02'></a>

**SOLO LONG** — Limite sugli estremi della sessione precedente.

| | |
|---|---|
| Timeframe | 1h |
| Motore | RHL |
| Atteso/trade | $140 |
| P&L fuori campione | $31,320 |
| Drawdown | $11,584 |
| Trade | 75 |
| Stop loss | 20.0 pt |
| Take profit | 50.0 pt |

**Ordine LIMITE sugli estremi della sessione precedente**

- LONG: limit buy a **L_d1** − 20 tick (2 pt) (minimo della sessione precedente)
- SHORT: limit sell a **H_d1** + 80 tick (8 pt) (massimo della sessione precedente)
- I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.
- Il fill richiede penetrazione stretta del livello (`minimo < livello` per il long): il semplice tocco NON riempie.
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

*Solo LONG*

- deve essere VERO — direzionale -1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale -5: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **13:00 e 12:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,000** per contratto = **20.00 pt**
- Take profit: **$5,000** = **50.00 pt**
- Uscita a tempo dopo **12 barre** (12 ore)

**Verifica** — lista trade di riferimento: `run_20260819_0659/consegna/trades/fam02_RHL.csv`

---

### S03 · GC 1h · Ritorno alla media sugli estremi  <a id='s03'></a>

**SOLO LONG** — Limite sugli estremi della sessione precedente.

| | |
|---|---|
| Timeframe | 1h |
| Motore | RHL |
| Atteso/trade | $92 |
| P&L fuori campione | $21,820 |
| Drawdown | $7,516 |
| Trade | 80 |
| Stop loss | 20.0 pt |
| Take profit | 50.0 pt |

**Ordine LIMITE sugli estremi della sessione precedente**

- LONG: limit buy a **L_d1** − 20 tick (2 pt) (minimo della sessione precedente)
- SHORT: limit sell a **H_d1** + 80 tick (8 pt) (massimo della sessione precedente)
- I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.
- Il fill richiede penetrazione stretta del livello (`minimo < livello` per il long): il semplice tocco NON riempie.
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`
- deve essere FALSO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`

*Solo LONG*

- deve essere FALSO — direzionale -5: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **13:00 e 12:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$2,000** per contratto = **20.00 pt**
- Take profit: **$5,000** = **50.00 pt**
- Uscita a tempo dopo **12 barre** (12 ore)

**Verifica** — lista trade di riferimento: `run_20260819_0659/consegna/trades/fam03_RHL.csv`

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

Le 3 strategie sono univoche: nessuna coppia condivide più del 70% degli ordini di entrata. È questo che rende lecito farle girare su conti separati — due sistemi che mandano gli stessi ordini sono copy trading, e presso una prop firm costano il conto.

Queste coppie restano sotto soglia ma sono le più vicine. Se i conti separati sono pochi, non accoppiare queste:

| Coppia | Entrate in comune |
|---|---|
| `S02` ↔ `S03` | 0.55 |

Il vincolo si misura sulle **entrate**, non sulla correlazione dei risultati: due strategie possono avere P&L molto diversi e mandare gli stessi ordini.

*Dati fuori campione dal 12/05/2021 al 30/05/2025. Criteri di selezione in `METODO_RICERCA.md`.*