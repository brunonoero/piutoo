# Strategie approvate — NQ 1h

**6 strategie distinte** in **6 famiglie indipendenti**. Generato il 20/08/2026.

Dentro una famiglia le entrate coincidono per più del 70%: anche quelle, per il broker, sono lo stesso ordine mandato più volte. **Un conto, una famiglia.**

## Legenda dei simboli

| simbolo | significato |
|---|---|
| `H_d1 / L_d1 / O_d1 / C_d1` | max, min, apertura, chiusura della SESSIONE precedente |
| `H_d0 / L_d0 / O_d0` | max, min, apertura della sessione CORRENTE, fino alla barra chiusa |
| `H_d2 … H_d5` | le sessioni ancora prima (d2 = due sessioni fa) |
| `HH5 / LL5` | il massimo di H_d1..H_d5 e il minimo di L_d1..L_d5 |
| `close` | chiusura della BARRA corrente (non della sessione) |

Contratto: 1 punto = $20, 1 tick = 0.25 punti. Stop e target sono in **$ per contratto** nella ricerca, riportati anche in **punti indice** perché è l'unità di cTrader.

Le sessioni `d0..d5` sono ricostruite dalle barre intraday con inizio sessione a **00:00 CET** — non sono le barre daily del broker.

## Indice

| famiglia | motore | atteso/trade | P&L fuori campione | strategie | tarature |
|---|---|---|---|---|---|
| [01](#famiglia-01) | TF_M | $583 | $175,532 | 1 | 0 |
| [02](#famiglia-02) | TF_M | $422 | $49,592 | 1 | 0 |
| [03](#famiglia-03) | BO | $283 | $39,469 | 1 | 0 |
| [04](#famiglia-04) | TF_U | $250 | $203,832 | 1 | 0 |
| [05](#famiglia-05) | TF_M | $242 | $45,941 | 1 | 0 |
| [06](#famiglia-06) | TF_U | $91 | $68,525 | 1 | 0 |

---

## Famiglia 01 — TF_M — $583 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $175,532 su 287 trade · drawdown $5,774 · profit factor 4.20 · $612 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale 50: `close > O_d0 * 1.005`
- deve essere FALSO — direzionale 8: `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0`

**Solo SHORT**

- deve essere VERO — direzionale 50: `close < O_d0 * 0.995`
- deve essere FALSO — direzionale 8: `O_d0 - L_d0 > (O_d1 - L_d1) * 3.0`

### Quando può operare

- Opera solo fra **14:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$250** per contratto = **12.50 pt**
- Take profit: **$3,000** = **150.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---

## Famiglia 02 — TF_M — $422 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $49,592 su 112 trade · drawdown $20,581 · profit factor 1.34 · $443 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 11: `|O_d5-C_d1| < 0.5 * (H_d5-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

**Solo SHORT**

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

### Quando può operare

- Opera solo fra **00:00 e 17:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,500** per contratto = **125.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **48 barre** (2.0 giorni di calendario)

*Una sola taratura del rischio.*

---

## Famiglia 03 — BO — $283 attesi/trade

*Breakout su N sessioni.* Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

**Fuori campione**: $39,469 su 59 trade · drawdown $18,908 · profit factor 2.44 · $669 per trade.

### Ordine STOP sul canale a 4 sessioni

- LONG: stop buy sul **massimo delle ultime 4 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 4 sessioni complete**

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 4: `|O_d1-C_d1| < 0.75 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 32: `(H_d0-L_d0) > L_d0 * 0.0075`

**Solo LONG**

- deve essere VERO — direzionale 44: `L_d1 > L_d2`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

**Solo SHORT**

- deve essere VERO — direzionale 44: `H_d1 < H_d2`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

### Quando può operare

- Opera solo fra **22:00 e 21:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

*Una sola taratura del rischio.*

---

## Famiglia 04 — TF_U — $250 attesi/trade

*Trend following, asimmetrico.* Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

**Fuori campione**: $203,832 su 232 trade · drawdown $22,478 · profit factor 2.37 · $879 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 32: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.5`
- deve essere FALSO — fast 2: `|O_d1-C_d1| < 0.25 * (H_d1-L_d1)`

**Solo SHORT**

- deve essere VERO — fast 38: `H_d0 - O_d0 > (H_d1 - O_d1) * 3.0`
- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

### Quando può operare

- Opera solo fra **17:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$750** per contratto = **37.50 pt**
- Take profit: **$10,000** = **500.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

*Una sola taratura del rischio.*

---

## Famiglia 05 — TF_M — $242 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $45,941 su 181 trade · drawdown $15,665 · profit factor 1.28 · $254 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`

**Solo LONG**

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

**Solo SHORT**

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 16: `C_d1 < C_d2 * (1 - 0.01)`

### Quando può operare

- Opera solo fra **21:00 e 14:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$1,250** per contratto = **62.50 pt**
- Take profit: **$4,000** = **200.00 pt**
- Uscita a tempo dopo **230 barre** (9.6 giorni di calendario)

*Una sola taratura del rischio.*

---

## Famiglia 06 — TF_U — $91 attesi/trade

*Trend following, asimmetrico.* Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

**Fuori campione**: $68,525 su 215 trade · drawdown $24,928 · profit factor 1.21 · $319 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 107: `L_d1 > L_d5`
- deve essere FALSO — fast 83: `H_d0 > H_d1 * (1 + 0.005)`

**Solo SHORT**

- deve essere VERO — fast 21: `|O_d5-C_d1| > 2.0 * (H_d5-L_d1)`
- deve essere FALSO — fast 39: `H_d0 - O_d0 < H_d1 - O_d1`

### Quando può operare

- Opera solo fra **16:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **46 barre** (1.9 giorni di calendario)

*Una sola taratura del rischio.*

---
