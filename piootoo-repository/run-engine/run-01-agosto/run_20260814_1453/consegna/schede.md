# Strategie approvate — NQ 15m

**11 strategie distinte** in **10 famiglie indipendenti**. Generato il 16/08/2026.

Le righe approvate dalla ricerca erano 133: 122 di quelle avevano **gli stessi identici parametri di entrata** e differivano solo per stop, target o durata massima. Non sono strategie diverse — sono la stessa strategia con un rischio diverso, e per il broker emettono gli stessi ordini. Stanno sotto la scheda della loro strategia, come tarature.

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
| [01](#famiglia-01) | TF_M | $436 | $136,408 | 1 | 35 |
| [02](#famiglia-02) | TF_M | $324 | $127,683 | 2 | 7 |
| [03](#famiglia-03) | TF_M | $287 | $92,523 | 1 | 3 |
| [04](#famiglia-04) | BO | $261 | $95,754 | 1 | 3 |
| [05](#famiglia-05) | BO | $251 | $71,987 | 1 | 1 |
| [06](#famiglia-06) | BO | $198 | $70,891 | 1 | 23 |
| [07](#famiglia-07) | TF_U | $156 | $85,485 | 1 | 6 |
| [08](#famiglia-08) | RBB_M | $110 | $104,240 | 1 | 2 |
| [09](#famiglia-09) | TF_U | $86 | $91,035 | 1 | 37 |
| [10](#famiglia-10) | TF_U | $50 | $50,526 | 1 | 5 |

---

## Famiglia 01 — TF_M — $436 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $136,408 su 153 trade · drawdown $25,934 · profit factor 1.70 · $892 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale 12: `(H_d1 > H_d2) E (L_d1 > L_d2)`
- deve essere FALSO — direzionale 39: `O_d0 > H_d1`

**Solo SHORT**

- deve essere VERO — direzionale 12: `(H_d1 < H_d2) E (L_d1 < L_d2)`
- deve essere FALSO — direzionale 39: `O_d0 < L_d1`

### Quando può operare

- Opera solo fra **09:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$3,000** per contratto = **150.00 pt**
- Take profit: **$4,000** = **200.00 pt**
- Uscita a tempo dopo **368 barre** (3.8 giorni di calendario)

### Le altre 35 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $421 | $131,513 | $22,241 | 153 | `max_bars` 368→184 · `stop_loss` 3000→4000 · `take_profit` 4000→6000 |
| $407 | $127,383 | $25,934 | 153 | `max_bars` 368→920 |
| $355 | $115,404 | $24,252 | 159 | `max_bars` 368→184 · `stop_loss` 3000→4000 |
| $355 | $109,561 | $26,870 | 151 | `take_profit` 4000→4500 |
| $353 | $114,634 | $25,934 | 159 | `max_bars` 368→184 |
| $352 | $112,832 | $24,507 | 157 | `max_bars` 368→184 · `stop_loss` 3000→4000 · `take_profit` 4000→4500 |
| $337 | $103,410 | $26,548 | 150 | `max_bars` 368→0 · `take_profit` 4000→4500 |
| $326 | $102,644 | $27,706 | 154 | `max_bars` 368→184 · `take_profit` 4000→6000 |
| $326 | $104,492 | $23,434 | 157 | `stop_loss` 3000→2500 |
| $322 | $104,013 | $26,548 | 158 | `max_bars` 368→184 · `take_profit` 4000→4500 |
| $318 | $104,591 | $23,116 | 161 | `stop_loss` 3000→4000 · `take_profit` 4000→3000 |
| $311 | $102,351 | $18,823 | 161 | `take_profit` 4000→3000 |
| $301 | $94,654 | $27,705 | 154 | `stop_loss` 3000→2500 · `take_profit` 4000→4500 |
| $290 | $95,461 | $25,170 | 161 | `stop_loss` 3000→5000 · `take_profit` 4000→3000 |
| $290 | $87,668 | $22,911 | 148 | `max_bars` 368→0 · `stop_loss` 3000→2500 · `take_profit` 4000→6000 |
| $289 | $96,914 | $28,223 | 164 | `max_bars` 368→184 · `stop_loss` 3000→5000 · `take_profit` 4000→3000 |
| $289 | $91,570 | $22,053 | 155 | `max_bars` 368→184 · `stop_loss` 3000→2500 · `take_profit` 4000→6000 |
| $286 | $86,538 | $22,911 | 148 | `max_bars` 368→920 · `stop_loss` 3000→2500 · `take_profit` 4000→6000 |
| $282 | $94,419 | $22,054 | 164 | `max_bars` 368→184 · `stop_loss` 3000→4000 · `take_profit` 4000→3000 |
| $279 | $81,678 | $29,361 | 143 | `max_bars` 368→644 · `take_profit` 4000→6000 |
| $269 | $90,099 | $19,262 | 164 | `max_bars` 368→184 · `take_profit` 4000→3000 |
| $264 | $84,016 | $29,742 | 156 | `max_bars` 368→184 · `stop_loss` 3000→2500 · `take_profit` 4000→5000 |
| $231 | $77,294 | $27,679 | 164 | `stop_loss` 3000→5000 · `take_profit` 4000→2500 |
| $229 | $76,213 | $20,066 | 163 | `stop_loss` 3000→2500 · `take_profit` 4000→3000 |
| $210 | $73,115 | $16,843 | 170 | `max_bars` 368→48 · `stop_loss` 3000→5000 · `take_profit` 4000→6000 |
| $196 | $65,784 | $20,418 | 164 | `take_profit` 4000→2500 |
| $190 | $63,524 | $28,908 | 164 | `stop_loss` 3000→4000 · `take_profit` 4000→2500 |
| $152 | $51,506 | $16,635 | 166 | `max_bars` 368→0 · `stop_loss` 3000→2000 · `take_profit` 4000→3000 |
| $151 | $50,925 | $22,391 | 165 | `max_bars` 368→184 · `take_profit` 4000→2500 |
| $151 | $52,415 | $18,004 | 170 | `max_bars` 368→48 · `stop_loss` 3000→5000 |
| $136 | $46,015 | $21,566 | 165 | `max_bars` 368→0 · `stop_loss` 3000→2500 · `take_profit` 4000→2500 |
| $122 | $42,450 | $19,772 | 170 | `max_bars` 368→48 · `stop_loss` 3000→5000 · `take_profit` 4000→3000 |
| $100 | $34,554 | $13,192 | 169 | `max_bars` 368→184 · `stop_loss` 3000→500 |
| $89 | $30,584 | $9,732 | 169 | `max_bars` 368→0 · `take_profit` 4000→500 |
| $79 | $27,470 | $7,905 | 170 | `max_bars` 368→0 · `stop_loss` 3000→250 · `take_profit` 4000→3000 |

---

## Famiglia 02 — TF_M — $324 attesi/trade  ·  strategia 1 di 2

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $127,683 su 193 trade · drawdown $24,911 · profit factor 1.48 · $662 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`
- deve essere FALSO — neutrale 9: `|O_d5-C_d1| < 0.1 * (H_d5-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 17: `C_d1 > C_d2 * (1 + 0.015)`

**Solo SHORT**

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 17: `C_d1 < C_d2 * (1 - 0.015)`

### Quando può operare

- Opera solo fra **13:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,500** per contratto = **125.00 pt**
- Take profit: **$4,500** = **225.00 pt**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

### Le altre 7 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $276 | $110,736 | $24,081 | 196 | `max_bars` 644→0 · `stop_loss` 2500→2250 |
| $243 | $94,540 | $28,590 | 190 | `stop_loss` 2500→2250 · `take_profit` 4500→5000 |
| $176 | $76,578 | $19,715 | 213 | `max_bars` 644→0 · `stop_loss` 2500→1000 |
| $173 | $75,333 | $19,030 | 213 | `max_bars` 644→0 · `stop_loss` 2500→2250 · `take_profit` 4500→3000 |
| $166 | $65,287 | $29,733 | 192 | `max_bars` 644→920 · `stop_loss` 2500→1750 · `take_profit` 4500→6000 |
| $155 | $62,412 | $29,733 | 197 | `max_bars` 644→368 · `stop_loss` 2500→1750 · `take_profit` 4500→6000 |
| $132 | $57,719 | $26,356 | 214 | `max_bars` 644→184 · `stop_loss` 2500→1250 · `take_profit` 4500→5000 |

## Famiglia 02 — TF_M — $147 attesi/trade  ·  strategia 2 di 2

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $88,354 su 294 trade · drawdown $23,408 · profit factor 1.37 · $301 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 54: `(H_d1-L_d1) > (H_d2-L_d2)`
- deve essere FALSO — neutrale 9: `|O_d5-C_d1| < 0.1 * (H_d5-L_d1)`

**Solo LONG**

- deve essere FALSO — direzionale 17: `C_d1 > C_d2 * (1 + 0.015)`

**Solo SHORT**

- deve essere FALSO — direzionale 17: `C_d1 < C_d2 * (1 - 0.015)`

### Quando può operare

- Opera solo fra **13:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$1,000** per contratto = **50.00 pt**
- Take profit: **$6,000** = **300.00 pt**
- Nessuna uscita a tempo

*Una sola taratura del rischio.*

---

## Famiglia 03 — TF_M — $287 attesi/trade

*Trend following, simmetrico.* Compra sopra il massimo della sessione precedente, vende sotto il minimo. Long e short usano lo stesso pattern, a specchio.

**Fuori campione**: $92,523 su 158 trade · drawdown $27,128 · profit factor 1.43 · $586 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale 12: `(H_d1 > H_d2) E (L_d1 > L_d2)`
- deve essere FALSO — direzionale 21: `H_d0 > H_d1`

**Solo SHORT**

- deve essere VERO — direzionale 12: `(H_d1 < H_d2) E (L_d1 < L_d2)`
- deve essere FALSO — direzionale 21: `L_d0 < L_d1`

### Quando può operare

- Opera solo fra **18:00 e 17:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$3,000** = **150.00 pt**
- Uscita a tempo dopo **368 barre** (3.8 giorni di calendario)

### Le altre 3 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $265 | $82,853 | $28,850 | 153 | `take_profit` 3000→4000 |
| $197 | $72,931 | $21,485 | 181 | `max_bars` 368→48 · `stop_loss` 4000→5000 · `take_profit` 3000→6000 |
| $173 | $62,030 | $25,317 | 175 | `max_bars` 368→644 · `stop_loss` 4000→250 · `take_profit` 3000→0 |

---

## Famiglia 04 — BO — $261 attesi/trade

*Breakout su N sessioni.* Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

**Fuori campione**: $95,754 su 124 trade · drawdown $28,286 · profit factor 1.56 · $772 per trade.

### Ordine STOP sul canale a 4 sessioni

- LONG: stop buy sul **massimo delle ultime 4 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 4 sessioni complete**

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -1: `O_d0 - L_d0 > (O_d1 - L_d1) * 0.25`
- deve essere FALSO — direzionale 38: `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)`

**Solo SHORT**

- deve essere VERO — direzionale -1: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — direzionale 38: `C_d1 - L_d1 < 0.2 * (H_d1-L_d1)`

### Quando può operare

- Opera solo fra **05:00 e 04:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$5,000** per contratto = **250.00 pt**
- Take profit: **$3,000** = **150.00 pt**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

### Le altre 3 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $240 | $89,020 | $20,552 | 125 | `max_bars` 644→184 · `stop_loss` 5000→4000 |
| $200 | $73,875 | $24,404 | 125 | `max_bars` 644→0 · `stop_loss` 5000→3000 |
| $160 | $60,718 | $15,973 | 128 | `stop_loss` 5000→4000 · `take_profit` 3000→2000 |

---

## Famiglia 05 — BO — $251 attesi/trade

*Breakout su N sessioni.* Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

**Fuori campione**: $71,987 su 97 trade · drawdown $26,043 · profit factor 1.70 · $742 per trade.

### Ordine STOP sul canale a 5 sessioni

- LONG: stop buy sul **massimo delle ultime 5 sessioni complete** + 2 tick (0.5 pt)
- SHORT: stop sell sul **minimo delle ultime 5 sessioni complete** − 2 tick (0.5 pt)

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 48: `((H_d1-L_d1) < (H_d2-L_d2)) E ((H_d2-L_d2) < (H_d3-L_d3))`

**Solo LONG**

- deve essere VERO — direzionale 35: `(H_d1 > H_d2) E (H_d1 > H_d3) E (H_d1 > H_d4)`
- deve essere FALSO — direzionale 50: `close > O_d0 * 1.005`

**Solo SHORT**

- deve essere VERO — direzionale 35: `(L_d1 < L_d2) E (L_d1 < L_d3) E (L_d1 < L_d4)`
- deve essere FALSO — direzionale 50: `close < O_d0 * 0.995`

### Quando può operare

- Opera solo fra **10:00 e 05:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$2,500** = **125.00 pt**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

### Le altre 1 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $154 | $45,670 | $15,520 | 100 | `max_bars` 644→0 · `stop_loss` 4000→250 · `take_profit` 2500→10000 |

---

## Famiglia 06 — BO — $198 attesi/trade

*Breakout su N sessioni.* Come il TF_M ma il livello è la rottura del canale delle ultime N sessioni (o del massimo/minimo in costruzione della sessione corrente).

**Fuori campione**: $70,891 su 121 trade · drawdown $21,070 · profit factor 2.25 · $586 per trade.

### Ordine STOP sul canale a 5 sessioni

- LONG: stop buy sul **massimo delle ultime 5 sessioni complete**
- SHORT: stop sell sul **minimo delle ultime 5 sessioni complete**

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 47: `(H_d1-L_d1) < ((H_d2-L_d2) + (H_d3-L_d3)) / 2`
- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -34: `L_d1 > L_d5`
- deve essere FALSO — direzionale 28: `L_d0 > L_d1 * (1 + 0.005)`

**Solo SHORT**

- deve essere VERO — direzionale -34: `H_d1 < H_d5`
- deve essere FALSO — direzionale 28: `H_d0 < H_d1 * (1 - 0.005)`

### Quando può operare

- Opera solo fra **13:00 e 06:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$500** per contratto = **25.00 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **644 barre** (6.7 giorni di calendario)

### Le altre 23 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $177 | $66,647 | $17,980 | 127 | `max_bars` 644→184 · `stop_loss` 500→2500 · `take_profit` 0→4000 |
| $177 | $60,796 | $27,174 | 116 | `max_bars` 644→920 |
| $162 | $62,200 | $13,701 | 130 | `max_bars` 644→184 · `stop_loss` 500→2500 · `take_profit` 0→3000 |
| $158 | $58,600 | $26,599 | 125 | `max_bars` 644→184 · `stop_loss` 500→3000 · `take_profit` 0→6000 |
| $158 | $60,394 | $18,692 | 129 | `max_bars` 644→184 · `stop_loss` 500→5000 · `take_profit` 0→2000 |
| $152 | $58,560 | $12,787 | 130 | `max_bars` 644→184 · `stop_loss` 500→2500 · `take_profit` 0→2500 |
| $152 | $60,229 | $14,410 | 134 | `max_bars` 644→184 · `stop_loss` 500→4000 · `take_profit` 0→1500 |
| $145 | $55,229 | $18,964 | 129 | `max_bars` 644→368 · `stop_loss` 500→5000 · `take_profit` 0→2000 |
| $133 | $49,857 | $14,923 | 127 | `max_bars` 644→0 · `stop_loss` 500→2500 · `take_profit` 0→3000 |
| $132 | $52,534 | $15,628 | 134 | `max_bars` 644→368 · `stop_loss` 500→4000 · `take_profit` 0→1500 |
| $130 | $49,839 | $14,201 | 129 | `max_bars` 644→0 · `stop_loss` 500→2250 · `take_profit` 0→3000 |
| $130 | $50,506 | $14,201 | 131 | `max_bars` 644→184 · `stop_loss` 500→2250 · `take_profit` 0→3000 |
| $128 | $49,902 | $13,787 | 132 | `max_bars` 644→184 · `stop_loss` 500→2500 · `take_profit` 0→2000 |
| $127 | $48,830 | $14,524 | 130 | `max_bars` 644→0 · `stop_loss` 500→2500 · `take_profit` 0→2500 |
| $117 | $45,211 | $16,787 | 131 | `max_bars` 644→368 · `stop_loss` 500→3000 · `take_profit` 0→2000 |
| $116 | $46,034 | $18,762 | 134 | `max_bars` 644→368 · `stop_loss` 500→5000 · `take_profit` 0→1500 |
| $112 | $43,812 | $16,390 | 132 | `max_bars` 644→0 · `stop_loss` 500→2250 · `take_profit` 0→2500 |
| $110 | $42,470 | $18,398 | 130 | `max_bars` 644→184 · `stop_loss` 500→2250 · `take_profit` 0→4000 |
| $108 | $42,879 | $14,829 | 134 | `max_bars` 644→184 · `stop_loss` 500→3000 · `take_profit` 0→1500 |
| $107 | $40,839 | $14,208 | 129 | `max_bars` 644→0 · `stop_loss` 500→2000 · `take_profit` 0→3000 |
| $106 | $42,204 | $12,329 | 134 | `max_bars` 644→184 · `stop_loss` 500→2500 · `take_profit` 0→1500 |
| $105 | $41,172 | $13,975 | 132 | `max_bars` 644→184 · `stop_loss` 500→2250 · `take_profit` 0→2500 |
| $101 | $39,091 | $14,208 | 131 | `max_bars` 644→184 · `stop_loss` 500→2000 · `take_profit` 0→3000 |

---

## Famiglia 07 — TF_U — $156 attesi/trade

*Trend following, asimmetrico.* Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

**Fuori campione**: $85,485 su 155 trade · drawdown $29,837 · profit factor 1.29 · $552 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 28: `|O_d5-C_d1| > 0.25 * (HH5-LL5)`
- deve essere FALSO — fast 77: `C_d1 > C_d2 * (1 + 0.005)`

**Solo SHORT**

- deve essere VERO — fast 114: `H_d1 - C_d1 < 0.2 * (H_d1-L_d1)`
- deve essere FALSO — fast 39: `H_d0 - O_d0 < H_d1 - O_d1`

### Quando può operare

- Opera solo fra **17:00 e 10:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$4,000** per contratto = **200.00 pt**
- Take profit: **$5,000** = **250.00 pt**
- Uscita a tempo dopo **368 barre** (3.8 giorni di calendario)

### Le altre 6 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $119 | $67,531 | $29,222 | 161 | `max_bars` 368→644 · `stop_loss` 4000→3000 |
| $105 | $60,819 | $29,222 | 164 | `stop_loss` 4000→3000 |
| $105 | $67,076 | $26,221 | 181 | `max_bars` 368→48 · `stop_loss` 4000→5000 · `take_profit` 5000→7500 |
| $96 | $56,462 | $25,577 | 167 | `max_bars` 368→644 · `stop_loss` 4000→2250 |
| $94 | $60,271 | $26,420 | 181 | `max_bars` 368→184 · `stop_loss` 4000→500 · `take_profit` 5000→0 |
| $73 | $46,384 | $20,588 | 179 | `max_bars` 368→0 · `stop_loss` 4000→500 |

---

## Famiglia 08 — RBB_M — $110 attesi/trade

*Ritorno alla media su Bollinger, simmetrico.* Compra in limite sulla banda inferiore, vende in limite sulla banda superiore. Il pattern direzionale è INVERTITO: il long cerca la fase ribassista, perché sta comprando il fondo.

**Fuori campione**: $104,240 su 520 trade · drawdown $27,622 · profit factor 1.21 · $200 per trade.

### Ordine LIMITE sulle bande di Bollinger (10 barre, 2.5 deviazioni)

- LONG: limit buy sulla **banda inferiore**, armato finché `close > banda_inf`
- SHORT: limit sell sulla **banda superiore**, armato finché `close < banda_sup`
- Il fill richiede penetrazione stretta del livello, non il semplice tocco.
- Se la banda è più stretta di un tick l'ordine NON si arma (banda a deviazione zero: il confronto deciderebbe su un pareggio).

### Filtri pattern

**Filtro comune a long e short**

- deve essere VERO — neutrale 53: `(H_d1-L_d1) < (H_d2-L_d2)`
- deve essere FALSO — neutrale 46: `(H_d0 < H_d1) E (L_d0 > L_d1)`

**Solo LONG**

- deve essere VERO — direzionale -48: `close > O_d0 * 0.995`
- deve essere FALSO — direzionale 37: `(C_d1 < C_d2) E (C_d2 < C_d3) E (O_d0 < C_d1)`

**Solo SHORT**

- deve essere VERO — direzionale -48: `close < O_d0 * 1.005`
- deve essere FALSO — direzionale 37: `(C_d1 > C_d2) E (C_d2 > C_d3) E (O_d0 > C_d1)`

### Quando può operare

- Opera solo fra **07:00 e 06:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$2,000** per contratto = **100.00 pt**
- Take profit: **$10,000** = **500.00 pt**
- Nessuna uscita a tempo

### Le altre 2 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $106 | $113,044 | $20,981 | 585 | `stop_loss` 2000→1000 · `take_profit` 10000→0 |
| $99 | $102,839 | $22,982 | 570 | `stop_loss` 2000→1250 · `take_profit` 10000→0 |

---

## Famiglia 09 — TF_U — $86 attesi/trade

*Trend following, asimmetrico.* Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

**Fuori campione**: $91,035 su 300 trade · drawdown $29,723 · profit factor 1.32 · $303 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 31: `H_d0 - O_d0 > (H_d1 - O_d1) * 0.25`
- deve essere FALSO — fast 52: `(H_d1 < H_d2) E (L_d1 < L_d2)`

**Solo SHORT**

- deve essere VERO — fast 52: `(H_d1 < H_d2) E (L_d1 < L_d2)`
- deve essere FALSO — fast 15: `|O_d5-C_d1| < 2.0 * (H_d5-L_d1)`

### Quando può operare

- Opera solo fra **17:00 e 07:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$1,250** per contratto = **62.50 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **184 barre** (1.9 giorni di calendario)

### Le altre 37 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $86 | $91,773 | $26,437 | 303 | `max_bars` 184→0 · `stop_loss` 1250→750 · `take_profit` 0→7500 |
| $82 | $92,053 | $27,833 | 318 | `stop_loss` 1250→750 · `take_profit` 0→10000 |
| $73 | $81,075 | $27,833 | 315 | `stop_loss` 1250→750 |
| $72 | $78,855 | $26,437 | 310 | `max_bars` 184→368 · `stop_loss` 1250→750 · `take_profit` 0→7500 |
| $71 | $80,286 | $23,587 | 321 | `max_bars` 184→368 · `stop_loss` 1250→500 · `take_profit` 0→7500 |
| $70 | $81,151 | $25,340 | 326 | `stop_loss` 1250→750 · `take_profit` 0→7500 |
| $58 | $67,539 | $27,264 | 329 | `max_bars` 184→920 · `stop_loss` 1250→750 · `take_profit` 0→6000 |
| $57 | $70,915 | $23,345 | 350 | `max_bars` 184→368 · `stop_loss` 1250→500 · `take_profit` 0→4500 |
| $54 | $67,451 | $23,827 | 356 | `stop_loss` 1250→500 · `take_profit` 0→4000 |
| $53 | $65,372 | $21,187 | 347 | `max_bars` 184→0 · `stop_loss` 1250→500 · `take_profit` 0→5000 |
| $53 | $69,410 | $29,338 | 370 | `max_bars` 184→48 · `stop_loss` 1250→1750 |
| $53 | $61,620 | $26,502 | 330 | `max_bars` 184→368 · `stop_loss` 1250→750 · `take_profit` 0→6000 |
| $53 | $69,554 | $21,712 | 374 | `max_bars` 184→24 · `take_profit` 0→5000 |
| $52 | $63,096 | $29,208 | 346 | `stop_loss` 1250→750 · `take_profit` 0→4500 |
| $51 | $62,514 | $29,208 | 344 | `max_bars` 184→368 · `stop_loss` 1250→750 · `take_profit` 0→4500 |
| $51 | $62,816 | $21,187 | 351 | `stop_loss` 1250→500 · `take_profit` 0→5000 |
| $50 | $60,323 | $28,753 | 338 | `stop_loss` 1250→750 · `take_profit` 0→6000 |
| $50 | $65,166 | $23,692 | 366 | `stop_loss` 1250→250 · `take_profit` 0→4000 |
| $50 | $65,511 | $23,448 | 371 | `max_bars` 184→48 · `stop_loss` 1250→750 |
| $50 | $62,210 | $22,827 | 355 | `stop_loss` 1250→500 · `take_profit` 0→4500 |
| $49 | $60,772 | $25,734 | 347 | `stop_loss` 1250→500 · `take_profit` 0→6000 |
| $49 | $58,949 | $26,308 | 339 | `max_bars` 184→920 · `stop_loss` 1250→750 · `take_profit` 0→5000 |
| $49 | $63,425 | $23,192 | 365 | `stop_loss` 1250→250 · `take_profit` 0→4500 |
| $48 | $62,625 | $23,692 | 365 | `max_bars` 184→0 · `stop_loss` 1250→250 · `take_profit` 0→4000 |
| $48 | $62,384 | $23,192 | 364 | `max_bars` 184→0 · `stop_loss` 1250→250 · `take_profit` 0→4500 |
| $48 | $63,205 | $24,303 | 370 | `max_bars` 184→48 · `stop_loss` 1250→1500 · `take_profit` 0→5000 |
| $47 | $61,431 | $26,525 | 371 | `max_bars` 184→48 · `take_profit` 0→4000 |
| $45 | $59,614 | $23,065 | 374 | `max_bars` 184→12 · `take_profit` 0→7500 |
| $45 | $57,147 | $25,355 | 362 | `stop_loss` 1250→250 · `take_profit` 0→5000 |
| $45 | $53,494 | $26,308 | 339 | `max_bars` 184→368 · `stop_loss` 1250→750 · `take_profit` 0→5000 |
| $43 | $55,900 | $25,938 | 370 | `max_bars` 184→48 · `stop_loss` 1250→1500 · `take_profit` 0→2500 |
| $42 | $53,745 | $24,480 | 360 | `max_bars` 184→920 · `stop_loss` 1250→250 · `take_profit` 0→5000 |
| $41 | $53,063 | $20,926 | 363 | `stop_loss` 1250→500 · `take_profit` 0→3000 |
| $40 | $51,823 | $23,473 | 368 | `stop_loss` 1250→250 · `take_profit` 0→3000 |
| $39 | $50,777 | $22,694 | 372 | `max_bars` 184→48 · `stop_loss` 1250→500 · `take_profit` 0→5000 |
| $35 | $46,066 | $20,998 | 371 | `max_bars` 184→48 · `stop_loss` 1250→750 · `take_profit` 0→2500 |
| $35 | $45,807 | $21,945 | 372 | `max_bars` 184→0 · `stop_loss` 1250→500 · `take_profit` 0→2000 |

---

## Famiglia 10 — TF_U — $50 attesi/trade

*Trend following, asimmetrico.* Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

**Fuori campione**: $50,526 su 286 trade · drawdown $21,924 · profit factor 1.29 · $177 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 63: `H_d0 < L_d0 * (1 + 0.015)`
- deve essere FALSO — fast 79: `C_d1 > C_d2 * (1 + 0.015)`

**Solo SHORT**

- deve essere VERO — fast 37: `H_d0 - O_d0 > (H_d1 - O_d1) * 2.5`
- deve essere FALSO — fast 137: `(C_d1 < O_d1) E (C_d2 > O_d2)`

### Quando può operare

- Opera solo fra **17:00 e 03:00** (a cavallo della mezzanotte), ora dei dati (CET)
- **Non apre** posizioni di venerdì
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$1,750** per contratto = **87.50 pt**
- Take profit: **$2,500** = **125.00 pt**
- Uscita a tempo dopo **48 barre** (12 ore)

### Le altre 5 tarature del rischio

**Stessi identici ordini di entrata.** Cambia solo l'uscita: sono la stessa strategia con un rischio diverso, non altre strategie. Servono al sizing, mai a moltiplicare i conti.

| atteso/trade | P&L OOS | DD OOS | trade | cosa cambia |
|---|---|---|---|---|
| $39 | $39,890 | $13,804 | 290 | `max_bars` 48→0 · `stop_loss` 1750→500 · `take_profit` 2500→2000 |
| $38 | $39,239 | $14,024 | 289 | `stop_loss` 1750→500 |
| $36 | $37,336 | $15,703 | 296 | `max_bars` 48→0 · `stop_loss` 1750→4000 · `take_profit` 2500→500 |
| $35 | $36,365 | $11,313 | 290 | `max_bars` 48→184 · `stop_loss` 1750→250 · `take_profit` 2500→3000 |
| $31 | $32,431 | $12,524 | 291 | `stop_loss` 1750→500 · `take_profit` 2500→2000 |

---
