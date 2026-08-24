# Strategie approvate — NQ 30m

**7 strategie distinte** in **5 famiglie indipendenti**. Generato il 16/08/2026.

Le righe approvate dalla ricerca erano 24: 17 di quelle avevano **gli stessi identici parametri di entrata** e differivano solo per stop, target o durata massima. Non sono strategie diverse — sono la stessa strategia con un rischio diverso, e per il broker emettono gli stessi ordini. Stanno sotto la scheda della loro strategia, come tarature.

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
| [01](#famiglia-01) | TF_M | $658 | $128,300 | 1 | 13 |
| [02](#famiglia-02) | TF_M | $464 | $113,244 | 1 | 4 |
| [03](#famiglia-03) | TF_M | $335 | $57,321 | 1 | 0 |
| [04](#famiglia-04) | PC | $202 | $151,529 | 1 | 0 |
| [05](#famiglia-05) | PC | $140 | $67,233 | 3 | 0 |

---

## Famiglia 01 — TF_M — $658 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $128,300 su 195 trade · drawdown $22,405 · profit factor 2.46 · $658 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`
- deve essere FALSO — neutrale 33: `(H_d0-L_d0) > L_d0 * 0.01`

**Solo LONG**

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 17: `C_d1 > C_d2 * (1 + 0.015)`

**Solo SHORT**

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 17: `C_d1 < C_d2 * (1 - 0.015)`

### Quando può operare

- Opera solo fra **14:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Uscita a tempo dopo **460 barre** (9.6 giorni di calendario)

### Le altre 13 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $590 | $115,736 | $18,320 | 196 | `max_bars` 460→0 · `stop_loss` 500→1250 · `take_profit` 10000→5000 |
| $561 | $100,970 | $24,766 | 180 | `stop_loss` 500→2500 · `take_profit` 10000→4500 |
| $500 | $102,465 | $15,004 | 205 | `max_bars` 460→322 · `take_profit` 10000→7500 |
| $480 | $86,380 | $28,234 | 180 | `max_bars` 460→0 · `stop_loss` 500→1750 · `take_profit` 10000→6000 |
| $476 | $90,450 | $22,775 | 190 | `max_bars` 460→322 · `stop_loss` 500→1250 · `take_profit` 10000→6000 |
| $454 | $91,166 | $14,592 | 201 | `max_bars` 460→0 · `stop_loss` 500→1000 · `take_profit` 10000→5000 |
| $447 | $89,450 | $16,162 | 200 | `max_bars` 460→0 · `stop_loss` 500→1250 · `take_profit` 10000→4000 |
| $445 | $84,540 | $25,409 | 190 | `max_bars` 460→0 · `stop_loss` 500→1750 · `take_profit` 10000→4500 |
| $417 | $75,952 | $25,501 | 182 | `max_bars` 460→0 · `stop_loss` 500→2500 · `take_profit` 10000→4000 |
| $396 | $81,606 | $16,239 | 206 | `max_bars` 460→92 · `stop_loss` 500→1000 · `take_profit` 10000→6000 |
| $371 | $77,614 | $13,570 | 209 | `max_bars` 460→0 · `stop_loss` 500→250 · `take_profit` 10000→7500 |
| $364 | $71,371 | $20,429 | 196 | `max_bars` 460→322 · `stop_loss` 500→1000 · `take_profit` 10000→6000 |
| $357 | $72,182 | $19,515 | 202 | `max_bars` 460→0 · `stop_loss` 500→1750 · `take_profit` 10000→3000 |

---

## Famiglia 02 — TF_M — $464 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $113,244 su 244 trade · drawdown $28,296 · profit factor 1.52 · $464 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 48: `((H_d1-L_d1) < (H_d2-L_d2)) E ((H_d2-L_d2) < (H_d3-L_d3))`

**Solo LONG**

- deve essere VERO — direzionale 50: `close > O_d0 * 1.005`
- deve essere FALSO — direzionale 7: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`

**Solo SHORT**

- deve essere VERO — direzionale 50: `close < O_d0 * 0.995`
- deve essere FALSO — direzionale 7: `O_d0 - L_d0 > (O_d1 - L_d1) * 2.5`

### Quando può operare

- Opera solo fra **02:00 e 01:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$5,000** per contratto = **250.00 pt**
- Take profit: **$3,000** = **150.00 pt**
- Uscita a tempo dopo **24 barre** (12 ore)

### Le altre 4 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $450 | $110,295 | $20,996 | 245 | `max_bars` 24→0 · `stop_loss` 5000→1500 · `take_profit` 3000→2500 |
| $433 | $106,451 | $29,781 | 246 | `max_bars` 24→48 · `stop_loss` 5000→1500 · `take_profit` 3000→4000 |
| $394 | $96,465 | $27,508 | 245 | `max_bars` 24→12 · `take_profit` 3000→4000 |
| $314 | $77,447 | $29,186 | 247 | `max_bars` 24→0 · `stop_loss` 5000→2500 · `take_profit` 3000→2000 |

---

## Famiglia 03 — TF_M — $335 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $57,321 su 171 trade · drawdown $6,123 · profit factor 2.63 · $335 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`

**Solo LONG**

- deve essere VERO — direzionale 50: `close > O_d0 * 1.005`
- deve essere FALSO — direzionale -36: `(H_d1 < H_d2) E (H_d1 < H_d3) E (H_d1 < H_d4)`

**Solo SHORT**

- deve essere VERO — direzionale 50: `close < O_d0 * 0.995`
- deve essere FALSO — direzionale -36: `(L_d1 > L_d2) E (L_d1 > L_d3) E (L_d1 > L_d4)`

### Quando può operare

- Opera solo fra **09:00 e 19:00**, ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$250** per contratto = **12.50 pt**
- Take profit: **$4,000** = **200.00 pt**
- Uscita a tempo dopo **12 barre** (6 ore)

*Una sola taratura del rischio.*

---

## Famiglia 04 — PC — $202 attesi/trade

*Price channel (Donchian).* Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

**Fuori campione**: $151,529 su 279 trade · drawdown $27,120 · profit factor 1.83 · $543 per trade.

### Motore PC — Price channel (Donchian)

- Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

### Filtri pattern

**Filtro comune a long e short**

- deve essere FALSO — neutrale 8: `|O_d1-C_d1| > 0.9 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 16: `C_d1 > C_d2 * (1 + 0.01)`

**Solo SHORT**

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 16: `C_d1 < C_d2 * (1 - 0.01)`

### Quando può operare

- Opera solo fra **14:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,500** per contratto = **125.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Breakeven a **$1,000** = **50.00 pt** di utile
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---

## Famiglia 05 — PC — $140 attesi/trade  ·  strategia 1 di 3

*Price channel (Donchian).* Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

**Fuori campione**: $67,233 su 178 trade · drawdown $16,660 · profit factor 1.71 · $378 per trade.

### Motore PC — Price channel (Donchian)

- Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 3: `|O_d1-C_d1| < 0.5 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 46: `(C_d1 > O_d1) E (C_d2 < O_d2)`

**Solo SHORT**

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 46: `(C_d1 < O_d1) E (C_d2 > O_d2)`

### Quando può operare

- Opera solo fra **11:00 e 10:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,250** per contratto = **112.50 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Breakeven a **$500** = **25.00 pt** di utile
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

## Famiglia 05 — PC — $136 attesi/trade  ·  strategia 2 di 3

*Price channel (Donchian).* Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

**Fuori campione**: $65,473 su 178 trade · drawdown $21,316 · profit factor 1.80 · $368 per trade.

### Motore PC — Price channel (Donchian)

- Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 3: `|O_d1-C_d1| < 0.5 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`

**Solo LONG**

- deve essere FALSO — direzionale 46: `(C_d1 > O_d1) E (C_d2 < O_d2)`

**Solo SHORT**

- deve essere FALSO — direzionale 46: `(C_d1 < O_d1) E (C_d2 > O_d2)`

### Quando può operare

- Opera solo fra **11:00 e 10:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,250** per contratto = **112.50 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Breakeven a **$500** = **25.00 pt** di utile
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

## Famiglia 05 — PC — $116 attesi/trade  ·  strategia 3 di 3

*Price channel (Donchian).* Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

**Fuori campione**: $62,239 su 199 trade · drawdown $17,287 · profit factor 1.67 · $313 per trade.

### Motore PC — Price channel (Donchian)

- Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 3: `|O_d1-C_d1| < 0.5 * (H_d1-L_d1)`
- deve essere FALSO — neutrale 24: `|O_d5-C_d1| < 0.25 * (HH5-LL5)`

**Solo LONG**

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`

**Solo SHORT**

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`

### Quando può operare

- Opera solo fra **11:00 e 10:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,250** per contratto = **112.50 pt**
- Take profit: **$10,000** = **500.00 pt**
- Trailing stop: **$2,000** = **100.00 pt**
- Breakeven a **$500** = **25.00 pt** di utile
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---
