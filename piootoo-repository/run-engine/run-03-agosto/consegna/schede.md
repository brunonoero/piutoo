# Strategie approvate — GC 1h

**5 strategie distinte** in **3 famiglie indipendenti**. Generato il 19/08/2026.

Dentro una famiglia le entrate coincidono per più del 70%: anche quelle, per il broker, sono lo stesso ordine mandato più volte. **Un conto, una famiglia.**

## Legenda dei simboli

| simbolo | significato |
|---|---|
| `H_d1 / L_d1 / O_d1 / C_d1` | max, min, apertura, chiusura della SESSIONE precedente |
| `H_d0 / L_d0 / O_d0` | max, min, apertura della sessione CORRENTE, fino alla barra chiusa |
| `H_d2 … H_d5` | le sessioni ancora prima (d2 = due sessioni fa) |
| `HH5 / LL5` | il massimo di H_d1..H_d5 e il minimo di L_d1..L_d5 |
| `close` | chiusura della BARRA corrente (non della sessione) |

Contratto: 1 punto = $100, 1 tick = 0.1 punti. Stop e target sono in **$ per contratto** nella ricerca, riportati anche in **punti indice** perché è l'unità di cTrader.

Le sessioni `d0..d5` sono ricostruite dalle barre intraday con inizio sessione a **00:00 CET** — non sono le barre daily del broker.

## Indice

| famiglia | motore | atteso/trade | P&L fuori campione | strategie | tarature |
|---|---|---|---|---|---|
| [01](#famiglia-01) | PC | $215 | $60,204 | 3 | 0 |
| [02](#famiglia-02) | RHL | $140 | $31,320 | 1 | 0 |
| [03](#famiglia-03) | RHL | $92 | $21,820 | 1 | 0 |

---

## Famiglia 01 — PC — $215 attesi/trade  ·  strategia 1 di 3

*Price channel (Donchian).* Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

**Fuori campione**: $60,204 su 81 trade · drawdown $19,202 · profit factor 1.63 · $743 per trade.

### Ordine STOP sul canale di Donchian a 30 barre

- LONG: stop buy sul **massimo delle ultime 30 barre** + 2 tick (0.2 pt)
- SHORT: stop sell sul **minimo delle ultime 30 barre** − 2 tick (0.2 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 30: `|O_d5-C_d1| > 0.75 * (HH5-LL5)`

**Solo LONG**

- deve essere VERO — direzionale -14: `C_d1 < O_d1`
- deve essere FALSO — direzionale -21: `L_d0 < L_d1`

**Solo SHORT**

- deve essere VERO — direzionale -14: `C_d1 > O_d1`
- deve essere FALSO — direzionale -21: `H_d0 > H_d1`

### Quando può operare

- Opera solo fra **06:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,250** per contratto = **22.50 pt**
- Take profit: **$4,000** = **40.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

## Famiglia 01 — PC — $209 attesi/trade  ·  strategia 2 di 3

*Price channel (Donchian).* Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

**Fuori campione**: $66,528 su 92 trade · drawdown $15,218 · profit factor 1.61 · $723 per trade.

### Ordine STOP sul canale di Donchian a 30 barre

- LONG: stop buy sul **massimo delle ultime 30 barre** + 2 tick (0.2 pt)
- SHORT: stop sell sul **minimo delle ultime 30 barre** − 2 tick (0.2 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 30: `|O_d5-C_d1| > 0.75 * (HH5-LL5)`

**Solo LONG**

- deve essere VERO — direzionale -14: `C_d1 < O_d1`

**Solo SHORT**

- deve essere VERO — direzionale -14: `C_d1 > O_d1`

### Quando può operare

- Opera solo fra **06:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,250** per contratto = **22.50 pt**
- Take profit: **$4,000** = **40.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

## Famiglia 01 — PC — $173 attesi/trade  ·  strategia 3 di 3

*Price channel (Donchian).* Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

**Fuori campione**: $57,464 su 96 trade · drawdown $15,218 · profit factor 1.49 · $599 per trade.

### Ordine STOP sul canale di Donchian a 30 barre

- LONG: stop buy sul **massimo delle ultime 30 barre** + 2 tick (0.2 pt)
- SHORT: stop sell sul **minimo delle ultime 30 barre** − 2 tick (0.2 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -14: `C_d1 < O_d1`
- deve essere FALSO — direzionale -21: `L_d0 < L_d1`

**Solo SHORT**

- deve essere VERO — direzionale -14: `C_d1 > O_d1`
- deve essere FALSO — direzionale -21: `H_d0 > H_d1`

### Quando può operare

- Opera solo fra **06:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,250** per contratto = **22.50 pt**
- Take profit: **$4,000** = **40.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---

## Famiglia 02 — RHL — $140 attesi/trade

*Ritorno alla media sugli estremi.* Limite sugli estremi della sessione precedente.

**Fuori campione**: $31,320 su 75 trade · drawdown $11,584 · profit factor 1.97 · $418 per trade.

### Ordine LIMITE sugli estremi della sessione precedente

- LONG: limit buy a **L_d1** − 20 tick (2 pt) (minimo della sessione precedente)
- SHORT: limit sell a **H_d1** + 80 tick (8 pt) (massimo della sessione precedente)
- I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.
- Il fill richiede penetrazione stretta del livello (`minimo < livello` per il long): il semplice tocco NON riempie.
- **Solo long**: il lato short non opera mai.

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale -5: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5`

**Solo SHORT**

- deve essere VERO — direzionale -1: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25`
- deve essere FALSO — direzionale -5: `O_d0 - L_d0 > (O_d1 - L_d1) * 1.5`

### Quando può operare

- Opera solo fra **13:00 e 12:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,000** per contratto = **20.00 pt**
- Take profit: **$5,000** = **50.00 pt**
- Uscita a tempo dopo **12 barre** (12 ore)

*Una sola taratura del rischio.*

---

## Famiglia 03 — RHL — $92 attesi/trade

*Ritorno alla media sugli estremi.* Limite sugli estremi della sessione precedente.

**Fuori campione**: $21,820 su 80 trade · drawdown $7,516 · profit factor 1.59 · $273 per trade.

### Ordine LIMITE sugli estremi della sessione precedente

- LONG: limit buy a **L_d1** − 20 tick (2 pt) (minimo della sessione precedente)
- SHORT: limit sell a **H_d1** + 80 tick (8 pt) (massimo della sessione precedente)
- I livelli vengono dalla sessione già completata: restano costanti per tutta la sessione corrente.
- Il fill richiede penetrazione stretta del livello (`minimo < livello` per il long): il semplice tocco NON riempie.
- **Solo long**: il lato short non opera mai.

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`
- deve essere FALSO — neutrale 12: `|O_d5-C_d1| < 0.75 * (H_d5-L_d1)`

**Solo LONG**

- deve essere FALSO — direzionale -5: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.5`

**Solo SHORT**

- deve essere FALSO — direzionale -5: `O_d0 - L_d0 > (O_d1 - L_d1) * 1.5`

### Quando può operare

- Opera solo fra **13:00 e 12:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,000** per contratto = **20.00 pt**
- Take profit: **$5,000** = **50.00 pt**
- Uscita a tempo dopo **12 barre** (12 ore)

*Una sola taratura del rischio.*

---
