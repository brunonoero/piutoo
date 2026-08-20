# Strategie approvate — ES 15m

**4 strategie distinte** in **4 famiglie indipendenti**. Generato il 19/08/2026.

Dentro una famiglia le entrate coincidono per più del 70%: anche quelle, per il broker, sono lo stesso ordine mandato più volte. **Un conto, una famiglia.**

## Legenda dei simboli

| simbolo | significato |
|---|---|
| `H_d1 / L_d1 / O_d1 / C_d1` | max, min, apertura, chiusura della SESSIONE precedente |
| `H_d0 / L_d0 / O_d0` | max, min, apertura della sessione CORRENTE, fino alla barra chiusa |
| `H_d2 … H_d5` | le sessioni ancora prima (d2 = due sessioni fa) |
| `HH5 / LL5` | il massimo di H_d1..H_d5 e il minimo di L_d1..L_d5 |
| `close` | chiusura della BARRA corrente (non della sessione) |

Contratto: 1 punto = $50, 1 tick = 0.25 punti. Stop e target sono in **$ per contratto** nella ricerca, riportati anche in **punti indice** perché è l'unità di cTrader.

Le sessioni `d0..d5` sono ricostruite dalle barre intraday con inizio sessione a **00:00 CET** — non sono le barre daily del broker.

## Indice

| famiglia | motore | atteso/trade | P&L fuori campione | strategie | tarature |
|---|---|---|---|---|---|
| [01](#famiglia-01) | BO | $268 | $90,062 | 1 | 0 |
| [02](#famiglia-02) | BIASW | $195 | $42,326 | 1 | 0 |
| [03](#famiglia-03) | BIASW | $193 | $57,538 | 1 | 0 |
| [04](#famiglia-04) | BIASW | $148 | $93,316 | 1 | 0 |

---

## Famiglia 01 — BO — $268 attesi/trade

*Breakout su N sessioni.* Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

**Fuori campione**: $90,062 su 72 trade · drawdown $27,908 · profit factor 1.75 · $1,251 per trade.

### Ordine STOP sul canale a 3 sessioni

- LONG: stop buy sul **massimo delle ultime 3 sessioni complete** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 3 sessioni complete** − 2 tick (0.5 pt)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale 34: `H_d1 < H_d5`
- deve essere FALSO — direzionale -48: `close < O_d0 * 1.005`

**Solo SHORT**

- deve essere VERO — direzionale 34: `L_d1 > L_d5`
- deve essere FALSO — direzionale -48: `close > O_d0 * 0.995`

### Quando può operare

- Opera solo fra **03:00 e 02:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$4,000** per contratto = **80.00 pt**
- Take profit: **$6,000** = **120.00 pt**
- Uscita a tempo dopo **920 barre** (9.6 giorni di calendario)

*Una sola taratura del rischio.*

---

## Famiglia 02 — BIASW — $195 attesi/trade

*Bias settimanale.* Entra e esce a giorni/orari fissi della settimana.

**Fuori campione**: $42,326 su 59 trade · drawdown $18,236 · profit factor 1.50 · $717 per trade.

### Ciclo settimanale a giorno e ora fissi

- LONG: **MARKET all'apertura della barra delle 02:00 di lunedì**
- SHORT: **spento** — questa strategia non apre mai al ribasso
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 94: `H_d1 < H_d5`
- deve essere FALSO — fast 139: `(C_d1 < O_d1) E (C_d2 < O_d2)`

### Quando può operare

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

### Uscite

- Uscita LONG: **venerdì alle 01:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$5,000** per contratto = **100.00 pt**
- Take profit: **$4,000** = **80.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---

## Famiglia 03 — BIASW — $193 attesi/trade

*Bias settimanale.* Entra e esce a giorni/orari fissi della settimana.

**Fuori campione**: $57,538 su 81 trade · drawdown $15,424 · profit factor 1.50 · $710 per trade.

### Ciclo settimanale a giorno e ora fissi

- LONG: **MARKET all'apertura della barra delle 03:00 di venerdì**
- SHORT: **spento** — questa strategia non apre mai al ribasso
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 94: `H_d1 < H_d5`
- deve essere FALSO — fast 112: `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)`

### Quando può operare

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

### Uscite

- Uscita LONG: **venerdì alle 01:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$3,000** per contratto = **60.00 pt**
- Take profit: **$4,500** = **90.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---

## Famiglia 04 — BIASW — $148 attesi/trade

*Bias settimanale.* Entra e esce a giorni/orari fissi della settimana.

**Fuori campione**: $93,316 su 171 trade · drawdown $20,886 · profit factor 1.40 · $546 per trade.

### Ciclo settimanale a giorno e ora fissi

- LONG: **MARKET all'apertura della barra delle 11:00 di lunedì**
- SHORT: **MARKET all'apertura della barra delle 20:00 di giovedì**
- L'orario è l'**etichetta di chiusura** della barra, ora dei dati (CET): su timeframe 30m la barra delle 14:00 copre 13:30–14:00, e l'entrata avviene alla sua apertura.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 65: `H_d0 < L_d0 * (1 + 0.025)`
- deve essere FALSO — fast 139: `(C_d1 < O_d1) E (C_d2 < O_d2)`

**Solo SHORT**

- deve essere VERO — fast 58: `H_d0 > L_d0 * (1 + 0.025)`
- deve essere FALSO — fast 73: `C_d1 < C_d2 * (1 - 0.015)`

### Quando può operare

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

### Uscite

- Uscita LONG: **lunedì alle 01:00**, market all'apertura di quella barra.
- Uscita SHORT: **lunedì alle 02:00**, market all'apertura di quella barra.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$3,000** per contratto = **60.00 pt**
- Take profit: **$7,500** = **150.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---
