# Strategie approvate — GC 30m

**1 strategie distinte** in **1 famiglie indipendenti**. Generato il 19/08/2026.

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
| [01](#famiglia-01) | TF_U | $889 | $176,500 | 1 | 0 |

---

## Famiglia 01 — TF_U — $889 attesi/trade

*Trend following, asimmetrico.* Stesse entrate del TF_M (massimo/minimo della sessione precedente), ma il filtro pattern del long e quello dello short sono indipendenti: una delle due direzioni può essere spenta.

**Fuori campione**: $176,500 su 150 trade · drawdown $20,054 · profit factor 2.04 · $1,177 per trade.

### Ordine STOP sugli estremi della sessione precedente

- LONG: stop buy a **H_d1** (massimo della sessione precedente)
- SHORT: stop sell a **L_d1** (minimo della sessione precedente)

### Filtri pattern

**Solo LONG**

- deve essere VERO — fast 34: `H_d0 - O_d0 > (H_d1 - O_d1) * 1.0`
- deve essere FALSO — fast 25: `|O_d5-C_d1| < 0.5 * (HH5-LL5)`

**Solo SHORT**

- deve essere VERO — fast 128: `(H_d1-L_d1) < (H_d2 - L_d2 + H_d3 - L_d3) / 3`
- deve essere FALSO — fast 1: `|O_d1-C_d1| < 0.1 * (H_d1-L_d1)`

### Quando può operare

- Opera solo fra **16:00 e 08:00** (a cavallo della mezzanotte), ora dei dati (CET)
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

### Uscite

- Stop loss: **$1,750** per contratto = **17.50 pt**
- Take profit: **$7,500** = **75.00 pt**
- Uscita a tempo dopo **460 barre** (9.6 giorni di calendario)

*Una sola taratura del rischio.*

---
