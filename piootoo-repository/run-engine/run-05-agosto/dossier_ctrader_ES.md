# ES su cTrader — specifica di implementazione

*Generato il 20 agosto 2026 da `run_20260819_1008, run_20260820_0012`.*

**6 strategie univoche** da implementare come cBot, ricavate da 2 timeframe (15m, 1h). Ogni strategia è definita qui per intero: condizioni di entrata, filtri, uscite, e la lista trade con cui verificare il port. Non serve conoscere il trading per implementarle — serve rispettare le regole della sezione 2 alla lettera.

> **Le tre cose che fanno fallire un port.** In ordine di frequenza: le sessioni ricostruite male (§2.1), l'ordine lasciato vivo più di una barra (§2.2), e il backtest fatto su barre invece che su tick (§5).

---

## 1. Cosa si costruisce

6 cBot indipendenti. Ognuno opera su un solo strumento e un solo timeframe, con un contratto per posizione. Non comunicano fra loro.

| ID | TF | Motore | Atteso/trade | P&L OOS | Drawdown | Trade | Equivalenti |
|---|---|---|---|---|---|---|---|
| [S01](#s01) | 1h | BIASW | $390 | $51,386 | $24,881 | 88 | 15m fam02 |
| [S02](#s02) | 15m | BO | $268 | $90,062 | $27,908 | 72 | — |
| [S03](#s03) | 15m | BIASW | $193 | $57,538 | $15,424 | 81 | — |
| [S04](#s04) | 1h | PC | $181 | $46,003 | $8,602 | 93 | 1h fam02-2 |
| [S05](#s05) | 15m | BIASW | $148 | $93,316 | $20,886 | 171 | — |
| [S06](#s06) | 1h | PC | $77 | $34,473 | $15,623 | 163 | — |

La colonna **Equivalenti** elenca le strategie che emettono gli stessi ordini di entrata: trovate separatamente, ma sono lo stesso sistema. Se ne implementa una sola.

### Da dove vengono i numeri

| Timeframe | Righe approvate | Strategie | Univoche |
|---|---|---|---|
| 15m | 4 | 4 | 3 |
| 1h | 4 | 4 | 3 |

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

La ricerca esprime stop e target in **dollari per contratto**; cTrader li vuole in **punti**. Su questo strumento 1 punto = **$50** e 1 tick = **0.25 punti**. Nelle schede i valori sono già convertiti.

### 2.6 Orari

Le finestre orarie sono in **ora dei dati (CET)** e si valutano sulla chiusura della barra. Una finestra il cui orario di fine è minore di quello di inizio attraversa la mezzanotte e va gestita come tale.

---

## 3. Le condizioni di pattern

I filtri sono condizioni booleane sulle grandezze di sessione della §2.1. Qui tutte quelle usate dalle 6 strategie, già in forma di formula.

| Riferimento | Condizione |
|---|---|
| `direzionale -45` | `(C_d1 < O_d1) E (C_d2 < O_d2)` |
| `direzionale -45` | `(C_d1 > O_d1) E (C_d2 > O_d2)` |
| `direzionale -48` | `close < O_d0 * 1.005` |
| `direzionale -48` | `close > O_d0 * 0.995` |
| `direzionale 16` | `C_d1 < C_d2 * (1 - 0.01)` |
| `direzionale 16` | `C_d1 > C_d2 * (1 + 0.01)` |
| `direzionale 34` | `H_d1 < H_d5` |
| `direzionale 34` | `L_d1 > L_d5` |
| `direzionale 49` | `close < O_d0` |
| `direzionale 49` | `close > O_d0` |
| `fast 106` | `L_d1 < L_d5` |
| `fast 112` | `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)` |
| `fast 130` | `(H_d2 > H_d1) E (L_d2 < L_d1)` |
| `fast 139` | `(C_d1 < O_d1) E (C_d2 < O_d2)` |
| `fast 58` | `H_d0 > L_d0 * (1 + 0.025)` |
| `fast 65` | `H_d0 < L_d0 * (1 + 0.025)` |
| `fast 73` | `C_d1 < C_d2 * (1 - 0.015)` |
| `fast 94` | `H_d1 < H_d5` |
| `neutrale 1` | `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)` |
| `neutrale 12` | `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)` |
| `neutrale 24` | `|O_d5-C_d1| < 0.25 * (HH5-LL5)` |
| `neutrale 29` | `|O_d5-C_d1| > 0.5 * (HH5-LL5)` |
| `neutrale 38` | `(H_d0-L_d0) < L_d0 * 0.005` |
| `neutrale 54` | `(H_d1-L_d1) > (H_d2-L_d2)` |

**deve essere VERO** = condizione requisito per entrare. **deve essere FALSO** = l'entrata è vietata quando la condizione si verifica. Le voci marcate *nessun filtro* sono assenti e non vanno implementate.

---

## 4. Le 6 strategie

### S01 · ES 1h · Bias settimanale  <a id='s01'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 1h |
| Motore | BIASW |
| Atteso/trade | $390 |
| P&L fuori campione | $51,386 |
| Drawdown | $24,881 |
| Trade | 88 |
| Stop loss | 100.0 pt |
| Take profit | 120.0 pt |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET all'apertura della barra delle 02:00 di lunedì**
- SHORT: **spento** — questa strategia non apre mai al ribasso
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 106: `L_d1 < L_d5`
- deve essere FALSO — fast 130: `(H_d2 > H_d1) E (L_d2 < L_d1)`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **lunedì alle 01:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$5,000** per contratto = **100.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0012/consegna/trades/fam01_BIASW.csv`

> ⚠ **Non mettere su conti diversi** insieme a `15m fam02`: emettono gli stessi ordini di entrata.

---

### S02 · ES 15m · Breakout su N sessioni  <a id='s02'></a>

**LONG + SHORT** — Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

| | |
|---|---|
| Timeframe | 15m |
| Motore | BO |
| Atteso/trade | $268 |
| P&L fuori campione | $90,062 |
| Drawdown | $27,908 |
| Trade | 72 |
| Stop loss | 80.0 pt |
| Take profit | 120.0 pt |

**Ordine STOP sul canale a 3 sessioni**

- LONG: stop buy sul **massimo delle ultime 3 sessioni complete** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 3 sessioni complete** − 2 tick (0.5 pt)

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

*Solo LONG*

- deve essere VERO — direzionale 34: `H_d1 < H_d5`
- deve essere FALSO — direzionale -48: `close < O_d0 * 1.005`

*Solo SHORT*

- deve essere VERO — direzionale 34: `L_d1 > L_d5`
- deve essere FALSO — direzionale -48: `close > O_d0 * 0.995`

**Quando può operare**

- Opera solo fra **03:00 e 02:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Uscita a tempo dopo **920 barre** (9.6 giorni di calendario)

**Verifica** — lista trade di riferimento: `run_20260819_1008/consegna/trades/fam01_BO.csv`

---

### S03 · ES 15m · Bias settimanale  <a id='s03'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 15m |
| Motore | BIASW |
| Atteso/trade | $193 |
| P&L fuori campione | $57,538 |
| Drawdown | $15,424 |
| Trade | 81 |
| Stop loss | 60.0 pt |
| Take profit | 90.0 pt |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET all'apertura della barra delle 03:00 di venerdì**
- SHORT: **spento** — questa strategia non apre mai al ribasso
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 94: `H_d1 < H_d5`
- deve essere FALSO — fast 112: `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **venerdì alle 01:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$3,000** per contratto = **60.00 pt**
- Take profit: **$4,500** = **90.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260819_1008/consegna/trades/fam03_BIASW.csv`

---

### S04 · ES 1h · Price channel (Donchian)  <a id='s04'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 1h |
| Motore | PC |
| Atteso/trade | $181 |
| P&L fuori campione | $46,003 |
| Drawdown | $8,602 |
| Trade | 93 |
| Stop loss | 80.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sul canale di Donchian a 20 barre**

- LONG: stop buy sul **massimo delle ultime 20 barre**
- SHORT: stop sell sul **minimo delle ultime 20 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`
- deve essere FALSO — neutrale 38: `(H_d0-L_d0) < L_d0 * 0.005`

*Solo LONG*

- deve essere VERO — direzionale 49: `close > O_d0`
- deve essere FALSO — direzionale -45: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **03:00 e 02:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Trailing stop: **$1,000** = **20.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0012/consegna/trades/fam02_PC.csv`

> ⚠ **Non mettere su conti diversi** insieme a `1h fam02-2`: emettono gli stessi ordini di entrata.

---

### S05 · ES 15m · Bias settimanale  <a id='s05'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 15m |
| Motore | BIASW |
| Atteso/trade | $148 |
| P&L fuori campione | $93,316 |
| Drawdown | $20,886 |
| Trade | 171 |
| Stop loss | 60.0 pt |
| Take profit | 150.0 pt |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET all'apertura della barra delle 11:00 di lunedì**
- SHORT: **MARKET all'apertura della barra delle 20:00 di giovedì**
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Solo LONG*

- deve essere VERO — fast 65: `H_d0 < L_d0 * (1 + 0.025)`
- deve essere FALSO — fast 139: `(C_d1 < O_d1) E (C_d2 < O_d2)`

*Solo SHORT*

- deve essere VERO — fast 58: `H_d0 > L_d0 * (1 + 0.025)`
- deve essere FALSO — fast 73: `C_d1 < C_d2 * (1 - 0.015)`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **lunedì alle 01:00**, market all'apertura di quella barra.
- Uscita SHORT: **lunedì alle 02:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$3,000** per contratto = **60.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260819_1008/consegna/trades/fam04_BIASW.csv`

---

### S06 · ES 1h · Price channel (Donchian)  <a id='s06'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 1h |
| Motore | PC |
| Atteso/trade | $77 |
| P&L fuori campione | $34,473 |
| Drawdown | $15,623 |
| Trade | 163 |
| Stop loss | 80.0 pt |
| Take profit | 150.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre**
- SHORT: stop sell sul **minimo delle ultime 1 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Filtro comune a long e short*

- deve essere VERO — neutrale 29: `|O_d5-C_d1| > 0.5 * (HH5-LL5)`
- deve essere FALSO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`

*Solo LONG*

- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

*Solo SHORT* — **non implementare questo lato**: la strategia opera in una sola direzione, queste condizioni non vengono mai valutate.

**Quando può operare**

- Opera solo fra **08:00 e 10:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Trailing stop: **$2,000** = **40.00 pt**
- Breakeven a **$500** = **10.00 pt** di utile
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `run_20260820_0012/consegna/trades/fam03_PC.csv`

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

*Dati fuori campione dal 01/06/2021 al 30/05/2025. Criteri di selezione in `METODO_RICERCA.md`.*