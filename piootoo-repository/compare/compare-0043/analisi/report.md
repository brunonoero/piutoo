# compare-0043 — cBot cTrader contro backtest interno (feed FTMO)

Generato: 2026-09-13 15:13Z. Finestra di sovrapposizione usata per il confronto trade: `2025-09-01` → `2026-08-31` (ingressi).

## 0. Perimetro dei due run

| | interno | cBot |
|---|---|---|
| trade totali | 667 | 682 |
| primo ingresso | 2025-09-01 08:45 | 2025-09-01 01:08 |
| ultimo ingresso | 2026-08-30 23:45 | 2026-08-30 23:45 |
| trade nella sovrapposizione | 667 | 682 |
| contratti per trade | 1 valori: 1 | 1 valori: 0.1 |
| netto totale | 260,581 | 17,726 |
| netto per contratto, sovrapposizione | 260,581 | 177,260 |
| lordo per contratto, sovrapposizione | 263,249 | 206,782 |
| commissioni per contratto, sovrapposizione | 2,668 | 21,111 |
| swap, sovrapposizione (grezzo) | 0 | -841 |

Log cBot: 236,610 righe; chiusure 682, fill con spread 685, ingressi scartati 853, annullati 0, bracket riancorati 31, chiusure per flat weekend 0, righe non interpretate 0.
Chiusure del log senza trade corrispondente: 0; fill senza trade: 3.

### 0b. Commissioni per contratto e per trade (sovrapposizione)

| simbolo | int trade | int comm/trade/ctr | cBot trade | cBot comm/trade/ctr | cBot swap/trade/ctr | cBot lordo/ctr | cBot netto/ctr |
|---|---:|---:|---:|---:|---:|---:|---:|
| ES | 42 | 4.0 | 42 | 23.0 | -143.8 | 16,365 | 9,361 |
| GC | 281 | 4.0 | 292 | 26.8 | -1.1 | 22,869 | 14,711 |
| NQ | 344 | 4.0 | 348 | 35.4 | -5.9 | 167,549 | 153,188 |


## 1. Differenze di trade per strategia (finestra di sovrapposizione)

Match = stessa strategia, stesso verso, ingresso entro una barra della strategia. Netto per contratto future.

| strategia | tf | int | cbot | match | solo int | solo cbot | Δentry mediano (min) | netto/ctr int | netto/ctr cbot | Δ netto/ctr | int in match (netto) | cbot in match (netto) | int in match (lordo) | cbot in match (lordo) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | 15 | 42 | 42 | 42 | 0 | 0 | 0.0 | 16,051 | 9,361 | -6,690 | 16,051 | 9,361 | 16,219 | 16,365 |
| PTS_GC_PCH_004_240 | 240 | 281 | 292 | 278 | 3 | 14 | 0.3 | 61,785 | 14,711 | -47,074 | 49,881 | 6,671 | 50,993 | 14,453 |
| PTS_NQ_PCH_003_30 | 30 | 69 | 70 | 63 | 6 | 7 | 0.3 | 16,842 | 14,839 | -2,003 | 1,086 | -4,612 | 1,338 | -2,126 |
| PTS_NQ_PCH_004_30 | 30 | 50 | 50 | 48 | 2 | 2 | 0.3 | 23,183 | 21,683 | -1,500 | 19,028 | 17,763 | 19,220 | 19,718 |
| PTS_NQ_PCH_008_240 | 240 | 50 | 51 | 49 | 1 | 2 | 0.2 | 16,215 | 74 | -16,141 | 6,219 | 3,210 | 6,415 | 5,455 |
| PTS_NQ_TFM_003_15 | 15 | 42 | 42 | 38 | 4 | 4 | 0.2 | 34,832 | 34,022 | -810 | 23,848 | 23,124 | 24,000 | 24,979 |
| PTS_NQ_TFU_001_15 | 15 | 33 | 33 | 32 | 1 | 1 | 0.2 | 31,523 | 30,151 | -1,372 | 26,527 | 25,172 | 26,655 | 26,687 |
| PTS_NQ_TFU_003_15 | 15 | 53 | 55 | 50 | 3 | 5 | 0.3 | 11,339 | 10,460 | -879 | 9,413 | 9,032 | 9,613 | 10,929 |
| PTS_NQ_TFU_006_1440 | 1440 | 47 | 47 | 46 | 1 | 1 | 0.2 | 48,812 | 41,960 | -6,852 | 49,816 | 43,012 | 50,000 | 44,641 |
| **TOTALE** | | 667 | 682 | 646 | 21 | 36 | | 260,581 | 177,260 | -83,321 | 201,868 | 132,733 | 204,452 | 161,101 |

Trade abbinati: 646. Scarto di ingresso: ≤1 min 576, ≤5 min 607, ≤15 min 626, oltre 20.
Prezzo di ingresso cBot peggiore dell'interno (punti, positivo = cBot paga di più): long mediana -0.54 media -0.6123 (n=482); short mediana -0.29 media -0.2215 (n=164).

### 1b. Perché un trade interno non esiste sul cBot

| causa | trade | netto/ctr interno di quei trade |
|---|---:|---:|
| cBot già in posizione (nessuna inversione) | 15 | 42,557 |
| nessun evento nel log (livello non toccato o intent non consegnato) | 5 | 6,849 |
| cBot scartato: lato sbagliato | 1 | 9,308 |

Uscite per segnale opposto nell'interno (inversione che il cBot non fa): 0 trade chiusi così, netto/ctr 0.

Trade solo cBot: 36, di cui 1 mentre l'interno era già in posizione sulla stessa strategia; netto/ctr dei solo-cBot 44,528.

## 2. Uscite

### 2a. Motivo di uscita dichiarato in trades.json

| interno | n | | cBot | n |
|---|---:|---|---|---:|
| StopLoss | 355 | | LocalExit:StopLoss | 532 |
| TrailingStop | 118 | | LocalExit:TakeProfit | 77 |
| TakeProfit | 76 | | LocalExit:Closed | 63 |
| TimeExit | 51 | | BrokerExit:TakeProfit | 6 |
| BreakEven | 47 | | BrokerExit:StopLoss | 4 |
| MaxBars | 20 | |  |  |

### 2b. Esito reale dal log del cBot (riga `Chiuso`)

| esito log | n | netto/ctr | di cui trades.json=Closed | StopLoss | TakeProfit |
|---|---:|---:|---:|---:|---:|
| StopLoss | 536 | -409,269 | 0 | 536 | 0 |
| TakeProfit | 83 | 466,409 | 0 | 0 | 83 |
| Closed | 63 | 120,120 | 63 | 0 | 0 |

### 2c. Trade abbinati: motivo interno × esito cBot

| interno \ cBot | Closed | StopLoss | TakeProfit | tot |
|---|---:|---:|---:|---:|
| BreakEven | 0 | 44 | 0 | 44 |
| MaxBars | 7 | 4 | 7 | 18 |
| StopLoss | 1 | 351 | 0 | 352 |
| TakeProfit | 0 | 2 | 68 | 70 |
| TimeExit | 49 | 0 | 0 | 49 |
| TrailingStop | 0 | 113 | 0 | 113 |

Impatto sui trade abbinati (netto/ctr cBot − interno) per coppia di esiti, prime 12 per valore assoluto:

| interno | cBot | n | Δ netto/ctr totale | Δ medio | Δ uscita mediano (min) |
|---|---|---:|---:|---:|---:|
| StopLoss | StopLoss | 351 | -25,080 | -71 | 0 |
| TakeProfit | StopLoss | 2 | -21,126 | -10,563 | -458 |
| TimeExit | Closed | 49 | -10,276 | -210 | 0 |
| TrailingStop | StopLoss | 113 | -9,758 | -86 | 1 |
| StopLoss | Closed | 1 | -4,041 | -4,041 | 758 |
| MaxBars | TakeProfit | 7 | 999 | 143 | 0 |
| TakeProfit | TakeProfit | 68 | 474 | 7 | 0 |
| BreakEven | StopLoss | 44 | -130 | -3 | 1 |
| MaxBars | Closed | 7 | -129 | -18 | 0 |
| MaxBars | StopLoss | 4 | -69 | -17 | 0 |

### 2d. Stop loss: perdita realizzata contro distanza dichiarata (per contratto, lordo)

Solo trade usciti per stop (interno `StopLoss`; cBot esito `StopLoss` senza trailing). Rapporto = perdita lorda / stop dichiarato (allargato dove previsto). >1 = stop eseguito oltre il livello (gap, spread sugli short).

| simbolo | int n | int rapporto mediano | int oltre 1,1 | cBot n | cBot rapporto mediano | cBot oltre 1,1 | cBot short mediano | cBot long mediano |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| ES | 0 | NaN | 0 | 0 | NaN | 0 | NaN | NaN |
| GC | 238 | 1.00 | 0 | 246 | 1.02 | 22 | 1.02 | 1.02 |
| NQ | 96 | 1.00 | 0 | 102 | 1.01 | 1 | 1.00 | 1.01 |

### 2f. Trade abbinati: quanto distano le uscite

| |Δ uscita| | n | Σ Δ lordo/ctr (cBot − int) | Σ Δ netto/ctr | Δ netto medio |
|---|---:|---:|---:|---:|
| ≤ 1 min | 502 | -12,360 | -34,072 | -68 |
| 1–5 min | 73 | -2,286 | -4,328 | -59 |
| 5–60 min | 37 | 287 | -804 | -22 |
| 1–24 h | 33 | -24,198 | -25,105 | -761 |
| > 24 h | 1 | -4,794 | -4,825 | -4,825 |

Nota: per le chiusure che il cBot scopre in ritardo (`BrokerExit`), l'esito nel log è dedotto dal segno del netto (`DeduceCloseReason`), quindi `MaxBars → TakeProfit` con Δ uscita 0 è la stessa chiusura etichettata in due modi.

### 2g. Riempimenti dell'interno su un minuto senza barra nel feed

Ingresso/uscita a un istante in cui `@SYM_1.json` non ha una barra: il prezzo viene dal mark-to-market, non dal mercato. Uscite tecniche (TimeExit, MaxBars, SessionFlat, WeekEnd, EndOfRun) escluse dal conteggio delle uscite.

| strategia | ingressi senza barra / tot | netto/ctr di quei trade | uscite SL/TP/trailing/BE senza barra | esempio |
|---|---:|---:|---:|---|
| **TOTALE** | 0 | 0 | | |

Per confronto, ingressi cBot il cui minuto non ha una barra nel feed raccolto: 0 su 682 (il cBot esegue sui tick di cTrader, il feed è la raccolta a barre dello stesso broker).

Bracket riancorato al fill (ingresso slittato rispetto al livello): 31 trade; slippage mediano 0.16 punti, costo totale per contratto 1,250.

## 3. Finestra operativa (TradingWindow) e giorno saltato

Per ogni trade si ricava la barra di segnale (la barra della strategia che precede quella del fill) e si etichetta come fa il motore (`BarLabelTime` sull'orologio della finestra). Fuori finestra = un ingresso che la strategia non avrebbe dovuto emettere.

| strategia | finestra | fuso | int fuori/tot | cBot fuori/tot | int giorno saltato | cBot giorno saltato | fill fuori griglia int/cBot |
|---|---|---|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | 00:00-24:00 | Research | 0/42 | 0/42 | 0 | 0 | 0/0 |
| PTS_GC_PCH_004_240 | 00:00-12:00 | Research | 0/281 | 0/292 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_003_30 | 14:00-04:00 | Research | 0/69 | 0/70 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_004_30 | 11:00-10:00 | Research | 0/50 | 0/50 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_008_240 | 06:00-24:00 | Research | 0/50 | 0/51 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_003_15 | 13:00-05:00 | Research | 0/42 | 0/42 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_001_15 | 17:00-10:00 | Research | 0/33 | 0/33 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_003_15 | 17:00-03:00 | Research | 0/53 | 1/55 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | Research | 0/47 | 0/47 | 0 | 0 | 0/0 |
| **TOTALE** | | | 0 | 1 | 0 | 0 | 0/0 |

Esempi fuori finestra (primi 25):

| lato | strategia | ingresso UTC | etichetta barra di segnale | finestra |
|---|---|---|---:|---|
| cBot | PTS_NQ_TFU_003_15 | 2025-12-03 02:26 | 03:15 | 17:00-03:00 |

## 4. Sessione: ancoraggio e un fill per sessione per lato

| strategia | sessione (ancoraggio) | max ingressi/sessione | int sessioni con >max per lato | cBot sessioni con >max per lato | int ingressi weekend UTC | cBot ingressi weekend UTC |
|---|---|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_GC_PCH_004_240 | 00:00-24:00 | 1 | 4 | 4 | 10 | 13 |
| PTS_NQ_PCH_003_30 | 00:00-24:00 | 0 | 0 | 0 | 2 | 3 |
| PTS_NQ_PCH_004_30 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_PCH_008_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_003_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFU_001_15 | 00:00-24:00 | 0 | 0 | 0 | 2 | 2 |
| PTS_NQ_TFU_003_15 | 00:00-24:00 | 0 | 0 | 0 | 4 | 5 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 3 |
| **TOTALE** | | | 4 | 4 | | |

Nel log del cBot il server ha rifiutato template per `limite di ingressi per sessione raggiunto` (conteggio righe, per strategia e lato, prime 15):

- PTS_NQ_TFU_003_15 Buy: 428
- PTS_NQ_TFU_001_15 Buy: 381
- PTS_NQ_TFM_003_15 Buy: 360
- PTS_NQ_PCH_004_30 Buy: 236
- PTS_NQ_PCH_003_30 Buy: 226
- PTS_NQ_TFM_003_15 Sell: 201
- PTS_GC_PCH_004_240 Buy: 190
- PTS_GC_PCH_004_240 Sell: 164
- PTS_NQ_PCH_008_240 Buy: 91
- PTS_NQ_TFU_003_15 Sell: 78

## 5. Spread

Spread dell'interno, dal summary: `FTMOPLATFORM · p50Spread · per ora UTC · FTMOPLATFORM_spread-by-symbol_20260801-20260901.csv (2026-09-10)`. Il cBot lo paga sui tick: costo per trade ≈ spread al fill × valore punto × contratti (una volta per round trip). Lo stop è quello eseguito (allargato ×1 dove previsto).

| strategia | simbolo | fill con spread | spread medio (punti) | stop (punti) | spread/stop | costo spread tot (per contratto, $) | netto/ctr cBot | netto/ctr senza spread | costo/trade ($/ctr) |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | ES | 42/42 | 0.532 | 0 | 0.0% | 1,118 | 9,361 | 10,479 | 27 |
| PTS_GC_PCH_004_240 | GC | 292/292 | 0.669 | 5 | 13.4% | 19,527 | 14,711 | 34,238 | 67 |
| PTS_NQ_PCH_003_30 | NQ | 70/70 | 1.53 | 125 | 1.2% | 2,143 | 14,839 | 16,982 | 31 |
| PTS_NQ_PCH_004_30 | NQ | 50/50 | 1.553 | 112.5 | 1.4% | 1,553 | 21,683 | 23,236 | 31 |
| PTS_NQ_PCH_008_240 | NQ | 51/51 | 1.449 | 200 | 0.7% | 1,478 | 74 | 1,552 | 29 |
| PTS_NQ_TFM_003_15 | NQ | 42/42 | 1.502 | 125 | 1.2% | 1,261 | 34,022 | 35,283 | 30 |
| PTS_NQ_TFU_001_15 | NQ | 33/33 | 1.532 | 200 | 0.8% | 1,011 | 30,151 | 31,162 | 31 |
| PTS_NQ_TFU_003_15 | NQ | 55/55 | 1.396 | 87.5 | 1.6% | 1,536 | 10,460 | 11,996 | 28 |
| PTS_NQ_TFU_006_1440 | NQ | 47/47 | 1.65 | 50 | 3.3% | 1,551 | 41,960 | 43,511 | 33 |
| **TOTALE** | | | | | | 31,178 | 177,260 | 208,439 | |

### 5b. Strategie con stop più stretto dello spread, o quasi

| strategia | spread/stop | stop (punti) | trade cBot | netto/ctr cBot | commento |
|---|---:|---:|---:|---:|---|
| PTS_GC_PCH_004_240 | 13.4% | 5 | 292 | 14,711 | 10-20% |

Sui trade abbinati: 163 volte il cBot esce per stop dove l'interno no; 1 il contrario.

## 6. Strategie con curva di equity crescente

Metriche su netto per contratto, trade ordinati per uscita. R² = fit lineare della curva cumulata sull'indice dei trade. Crescente = netto > 0, R² ≥ 0,6, chiusura ad almeno il 70% del picco, ≥ 8 trade. Il cBot è calcolato sull'intero run (ingressi 01/09/2025 → 30/08/2026), l'interno sull'intero run (ingressi 01/09/2025 → 30/08/2026).

| strategia | cBot n | cBot netto/ctr | PF | R² | DD max/ctr | fine/picco | **cBot** | int n | int netto/ctr | PF | R² | DD max/ctr | fine/picco | **int** |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| PTS_ES_BSW_003_15 | 42 | 9,361 | 1.14 | 0.34 | 18,680 | 0.42 | no | 42 | 16,051 | 1.25 | 0.51 | 18,024 | 0.59 | no |
| PTS_GC_PCH_004_240 | 292 | 14,711 | 1.10 | 0.46 | 21,797 | 0.61 | no | 281 | 61,785 | 1.51 | 0.88 | 11,917 | 0.90 | SÌ |
| PTS_NQ_PCH_003_30 | 70 | 14,839 | 1.32 | 0.04 | 20,882 | 0.66 | no | 69 | 16,842 | 1.41 | 0.10 | 19,621 | 0.81 | no |
| PTS_NQ_PCH_004_30 | 50 | 21,683 | 2.29 | 0.36 | 12,348 | 0.91 | no | 50 | 23,183 | 2.58 | 0.47 | 11,229 | 0.92 | no |
| PTS_NQ_PCH_008_240 | 51 | 74 | 1.00 | 0.16 | 16,836 | 0.01 | no | 50 | 16,215 | 1.44 | 0.50 | 17,061 | 0.79 | no |
| PTS_NQ_TFM_003_15 | 42 | 34,022 | 1.60 | 0.57 | 23,422 | 0.93 | no | 42 | 34,832 | 1.63 | 0.60 | 22,536 | 0.93 | no |
| PTS_NQ_TFU_001_15 | 33 | 30,151 | 1.51 | 0.75 | 14,657 | 0.88 | SÌ | 33 | 31,523 | 1.54 | 0.78 | 14,361 | 0.89 | SÌ |
| PTS_NQ_TFU_003_15 | 55 | 10,460 | 1.26 | 0.55 | 9,934 | 0.51 | no | 53 | 11,339 | 1.31 | 0.69 | 8,078 | 0.58 | no |
| PTS_NQ_TFU_006_1440 | 47 | 41,960 | 1.88 | 0.08 | 31,066 | 0.98 | no | 47 | 48,812 | 2.19 | 0.12 | 26,104 | 1.00 | no |

**Crescenti sul cBot:** PTS_NQ_TFU_001_15

**Crescenti sull'interno:** PTS_GC_PCH_004_240, PTS_NQ_TFU_001_15

**Crescenti su entrambi:** PTS_NQ_TFU_001_15

## 7. Equity aggregata per mese (netto per contratto, sovrapposizione)

| mese (uscita) | interno | cBot | Δ |
|---|---:|---:|---:|
| 2025-09 | 35,406 (cum 35,406) | 34,042 (cum 34,042) | -1,364 |
| 2025-10 | 35,195 (cum 70,600) | 32,783 (cum 66,825) | -2,412 |
| 2025-11 | 53,424 (cum 124,024) | 51,358 (cum 118,183) | -2,066 |
| 2025-12 | -33,731 (cum 90,293) | -37,388 (cum 80,795) | -3,658 |
| 2026-01 | -28,954 (cum 61,339) | -23,940 (cum 56,855) | 5,014 |
| 2026-02 | 5,630 (cum 66,969) | -11,186 (cum 45,669) | -16,816 |
| 2026-03 | 13,193 (cum 80,162) | 1,769 (cum 47,438) | -11,424 |
| 2026-04 | 60,601 (cum 140,763) | 46,268 (cum 93,706) | -14,333 |
| 2026-05 | 90,457 (cum 231,220) | 79,505 (cum 173,211) | -10,952 |
| 2026-06 | -6,319 (cum 224,901) | -9,155 (cum 164,056) | -2,836 |
| 2026-07 | 17,810 (cum 242,711) | 12,294 (cum 176,349) | -5,516 |
| 2026-08 | 17,870 (cum 260,581) | 911 (cum 177,260) | -16,959 |

## 9. Quando arrivano gli intent al cBot (righe `Intent ... attesa`)

`attesa` = ValidFrom − ora del server: negativo = l'intent arriva dopo l'inizio della barra su cui è valido. Per tipo di ordine e timeframe della strategia.

| tipo | tf | intent | in orario (≤ 10 s) | 10 s – 5 min | 5 min – 1 barra | ≥ 1 barra (in ritardo di una barra intera) | esempio strategie ≥ 1 barra |
|---|---:|---:|---:|---:|---:|---:|---|
| Market | 15 | 42 | 39 | 3 | 0 | 0 |  |
| Stop | 15 | 17222 | 16763 | 9 | 3 | 447 | PTS_NQ_TFM_003_15 (269), PTS_NQ_TFU_001_15 (90), PTS_NQ_TFU_003_15 (88) |
| Stop | 30 | 6667 | 6533 | 0 | 0 | 134 | PTS_NQ_PCH_003_30 (91), PTS_NQ_PCH_004_30 (43) |
| Stop | 240 | 897 | 639 | 190 | 0 | 68 | PTS_GC_PCH_004_240 (66), PTS_NQ_PCH_008_240 (2) |
| Stop | 1440 | 163 | 16 | 115 | 1 | 31 | PTS_NQ_TFU_006_1440 (31) |

### 9b. Trade cBot nati da un intent arrivato in ritardo di almeno una barra

Trade cBot con intent risolto: 681 su 682; nati da intent in ritardo ≥ 1 barra: 29, netto/ctr 19,017; di questi senza corrispondente interno: 6.

| strategia | trade da intent stantio | netto/ctr | non abbinati |
|---|---:|---:|---:|
| PTS_GC_PCH_004_240 | 19 | -1,764 | 4 |
| PTS_NQ_TFU_006_1440 | 9 | 22,592 | 1 |
| PTS_NQ_TFU_003_15 | 1 | -1,811 | 1 |

Trade abbinati in cui il cBot esce per stop più di 5 minuti PRIMA dell'uscita interna (che non era uno stop): 2, Δ netto/ctr -21,126. Per strategia: PTS_GC_PCH_004_240 2.

## 8. Avvisi ed errori nel log del cBot (messaggi normalizzati, prime 25)


Ingressi scartati per classe:
-     853  lato sbagliato

Ingressi annullati per classe:

`Nessun intent per l'account` per motivo:
-   16388  già reclamati dal conto '17188650'
-    2216  l'account ha già un ingresso in corso per quella strategia su quel simbolo e lato

Ingressi scartati `lato sbagliato` per strategia (prime 15):
-     365  PTS_NQ_TFU_001_15
-     339  PTS_NQ_TFU_003_15
-      82  PTS_NQ_TFM_003_15
-      36  PTS_GC_PCH_004_240
-      28  PTS_NQ_TFU_006_1440
-       2  PTS_NQ_PCH_008_240
-       1  PTS_NQ_PCH_003_30

