# compare-0045 — cBot cTrader contro backtest interno (feed FTMO)

Generato: 2026-09-15 08:00Z. Finestra di sovrapposizione usata per il confronto trade: `2025-09-01` → `2026-08-31` (ingressi).

## 0. Perimetro dei due run

| | interno | cBot |
|---|---|---|
| trade totali | 669 | 663 |
| primo ingresso | 2025-09-01 08:45 | 2025-09-01 08:45 |
| ultimo ingresso | 2026-08-30 23:45 | 2026-08-30 23:22 |
| trade nella sovrapposizione | 669 | 663 |
| contratti per trade | 1 valori: 1 | 1 valori: 0.1 |
| netto totale | 261,079 | 18,028 |
| netto per contratto, sovrapposizione | 261,079 | 180,281 |
| lordo per contratto, sovrapposizione | 263,755 | 209,293 |
| commissioni per contratto, sovrapposizione | 2,676 | 20,602 |
| swap, sovrapposizione (grezzo) | 0 | -841 |

Log cBot: 0 righe; chiusure 0, fill con spread 0, ingressi scartati 0, annullati 0, bracket riancorati 0, chiusure per flat weekend 0, righe non interpretate 0.
Chiusure del log senza trade corrispondente: 0; fill senza trade: 0.

### 0b. Commissioni per contratto e per trade (sovrapposizione)

| simbolo | int trade | int comm/trade/ctr | cBot trade | cBot comm/trade/ctr | cBot swap/trade/ctr | cBot lordo/ctr | cBot netto/ctr |
|---|---:|---:|---:|---:|---:|---:|---:|
| ES | 42 | 4.0 | 42 | 23.0 | -143.8 | 16,365 | 9,361 |
| GC | 281 | 4.0 | 273 | 26.8 | -1.2 | 24,124 | 16,476 |
| NQ | 346 | 4.0 | 348 | 35.4 | -5.9 | 168,805 | 154,444 |


## 1. Differenze di trade per strategia (finestra di sovrapposizione)

Match = stessa strategia, stesso verso, ingresso entro una barra della strategia. Netto per contratto future.

| strategia | tf | int | cbot | match | solo int | solo cbot | Δentry mediano (min) | netto/ctr int | netto/ctr cbot | Δ netto/ctr | int in match (netto) | cbot in match (netto) | int in match (lordo) | cbot in match (lordo) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | 15 | 42 | 42 | 42 | 0 | 0 | 0.0 | 16,051 | 9,361 | -6,690 | 16,051 | 9,361 | 16,219 | 16,365 |
| PTS_GC_PCH_004_240 | 240 | 281 | 273 | 263 | 18 | 10 | 0.3 | 72,414 | 16,476 | -55,938 | 47,375 | 8,154 | 48,427 | 15,534 |
| PTS_NQ_PCH_003_30 | 30 | 70 | 70 | 64 | 6 | 6 | 0.3 | 19,673 | 14,839 | -4,834 | 3,901 | -1,844 | 4,157 | 677 |
| PTS_NQ_PCH_004_30 | 30 | 50 | 50 | 48 | 2 | 2 | 0.3 | 23,273 | 21,683 | -1,590 | 19,110 | 17,763 | 19,302 | 19,718 |
| PTS_NQ_PCH_008_240 | 240 | 50 | 51 | 50 | 0 | 1 | 0.2 | 5,410 | 74 | -5,336 | 5,410 | 2,168 | 5,610 | 4,448 |
| PTS_NQ_TFM_003_15 | 15 | 42 | 42 | 38 | 4 | 4 | 0.2 | 34,832 | 34,022 | -810 | 23,848 | 23,124 | 24,000 | 24,979 |
| PTS_NQ_TFU_001_15 | 15 | 33 | 33 | 32 | 1 | 1 | 0.2 | 31,523 | 30,151 | -1,372 | 26,527 | 25,172 | 26,655 | 26,687 |
| PTS_NQ_TFU_003_15 | 15 | 54 | 55 | 52 | 2 | 3 | 0.3 | 13,915 | 11,716 | -2,198 | 11,378 | 10,941 | 11,586 | 12,909 |
| PTS_NQ_TFU_006_1440 | 1440 | 47 | 47 | 46 | 1 | 1 | 0.2 | 43,989 | 41,960 | -2,029 | 44,993 | 43,012 | 45,177 | 44,641 |
| **TOTALE** | | 669 | 663 | 635 | 34 | 28 | | 261,079 | 180,281 | -80,799 | 198,593 | 137,851 | 201,133 | 165,958 |

Trade abbinati: 635. Scarto di ingresso: ≤1 min 564, ≤5 min 594, ≤15 min 613, oltre 22.
Prezzo di ingresso cBot peggiore dell'interno (punti, positivo = cBot paga di più): long mediana -0.47 media -0.4119 (n=479); short mediana -0.275 media -0.1854 (n=156).

### 1b. Perché un trade interno non esiste sul cBot

| causa | trade | netto/ctr interno di quei trade |
|---|---:|---:|
| nessun evento nel log (livello non toccato o intent non consegnato) | 18 | 15,728 |
| cBot già in posizione (nessuna inversione) | 16 | 46,759 |

Uscite per segnale opposto nell'interno (inversione che il cBot non fa): 0 trade chiusi così, netto/ctr 0.

Trade solo cBot: 28, di cui 1 mentre l'interno era già in posizione sulla stessa strategia; netto/ctr dei solo-cBot 42,430.

## 2. Uscite

### 2a. Motivo di uscita dichiarato in trades.json

| interno | n | | cBot | n |
|---|---:|---|---|---:|
| StopLoss | 354 | | LocalExit:StopLoss | 531 |
| TrailingStop | 120 | | LocalExit:TakeProfit | 77 |
| TakeProfit | 77 | | LocalExit:Closed | 45 |
| TimeExit | 51 | | BrokerExit:TakeProfit | 6 |
| BreakEven | 47 | | BrokerExit:StopLoss | 4 |
| MaxBars | 20 | |  |  |

### 2b. Esito reale dal log del cBot (riga `Chiuso`)

| esito log | n | netto/ctr | di cui trades.json=Closed | StopLoss | TakeProfit |
|---|---:|---:|---:|---:|---:|
| (senza riga Chiuso) | 663 | 180,281 | 45 | 535 | 83 |

### 2c. Trade abbinati: motivo interno × esito cBot

| interno \ cBot | BrokerExit:StopLoss | BrokerExit:TakeProfit | LocalExit:Closed | LocalExit:StopLoss | LocalExit:TakeProfit | tot |
|---|---:|---:|---:|---:|---:|---:|
| BreakEven | 0 | 0 | 0 | 44 | 0 | 44 |
| MaxBars | 4 | 6 | 8 | 0 | 1 | 19 |
| StopLoss | 0 | 0 | 1 | 350 | 0 | 351 |
| TakeProfit | 0 | 0 | 0 | 3 | 69 | 72 |
| TimeExit | 0 | 0 | 34 | 0 | 0 | 34 |
| TrailingStop | 0 | 0 | 0 | 115 | 0 | 115 |

Impatto sui trade abbinati (netto/ctr cBot − interno) per coppia di esiti, prime 12 per valore assoluto:

| interno | cBot | n | Δ netto/ctr totale | Δ medio | Δ uscita mediano (min) |
|---|---|---:|---:|---:|---:|
| TakeProfit | LocalExit:StopLoss | 3 | -31,655 | -10,552 | -738 |
| StopLoss | LocalExit:StopLoss | 350 | -20,227 | -58 | 0 |
| TrailingStop | LocalExit:StopLoss | 115 | -10,121 | -88 | 1 |
| TimeExit | LocalExit:Closed | 34 | 4,213 | 124 | 0 |
| StopLoss | LocalExit:Closed | 1 | -4,041 | -4,041 | 758 |
| MaxBars | LocalExit:TakeProfit | 1 | 910 | 910 | -344 |
| TakeProfit | LocalExit:TakeProfit | 69 | 443 | 6 | 0 |
| BreakEven | LocalExit:StopLoss | 44 | -130 | -3 | 1 |
| MaxBars | LocalExit:Closed | 8 | -111 | -14 | 0 |
| MaxBars | BrokerExit:StopLoss | 4 | -81 | -20 | 0 |
| MaxBars | BrokerExit:TakeProfit | 6 | 59 | 10 | 0 |

### 2d. Stop loss: perdita realizzata contro distanza dichiarata (per contratto, lordo)

Solo trade usciti per stop (interno `StopLoss`; cBot esito `StopLoss` senza trailing). Rapporto = perdita lorda / stop dichiarato (allargato dove previsto). >1 = stop eseguito oltre il livello (gap, spread sugli short).

| simbolo | int n | int rapporto mediano | int oltre 1,1 | cBot n | cBot rapporto mediano | cBot oltre 1,1 | cBot short mediano | cBot long mediano |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| ES | 0 | NaN | 0 | 0 | NaN | 0 | NaN | NaN |
| GC | 237 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| NQ | 96 | 1.00 | 1 | 0 | NaN | 0 | NaN | NaN |

### 2f. Trade abbinati: quanto distano le uscite

| |Δ uscita| | n | Σ Δ lordo/ctr (cBot − int) | Σ Δ netto/ctr | Δ netto medio |
|---|---:|---:|---:|---:|
| ≤ 1 min | 490 | -8,300 | -29,724 | -61 |
| 1–5 min | 72 | -2,330 | -4,333 | -60 |
| 5–60 min | 38 | 231 | -882 | -23 |
| 1–24 h | 35 | -24,776 | -25,803 | -737 |

Nota: per le chiusure che il cBot scopre in ritardo (`BrokerExit`), l'esito nel log è dedotto dal segno del netto (`DeduceCloseReason`), quindi `MaxBars → TakeProfit` con Δ uscita 0 è la stessa chiusura etichettata in due modi.

### 2g. Riempimenti dell'interno su un minuto senza barra nel feed

Ingresso/uscita a un istante in cui `@SYM_1.json` non ha una barra: il prezzo viene dal mark-to-market, non dal mercato. Uscite tecniche (TimeExit, MaxBars, SessionFlat, WeekEnd, EndOfRun) escluse dal conteggio delle uscite.

| strategia | ingressi senza barra / tot | netto/ctr di quei trade | uscite SL/TP/trailing/BE senza barra | esempio |
|---|---:|---:|---:|---|
| **TOTALE** | 0 | 0 | | |

Per confronto, ingressi cBot il cui minuto non ha una barra nel feed raccolto: 0 su 663 (il cBot esegue sui tick di cTrader, il feed è la raccolta a barre dello stesso broker).

Bracket riancorato al fill (ingresso slittato rispetto al livello): 0 trade; slippage mediano NaN punti, costo totale per contratto 0.

## 3. Finestra operativa (TradingWindow) e giorno saltato

Per ogni trade si ricava la barra di segnale (la barra della strategia che precede quella del fill) e si etichetta come fa il motore (`BarLabelTime` sull'orologio della finestra). Fuori finestra = un ingresso che la strategia non avrebbe dovuto emettere.

| strategia | finestra | fuso | int fuori/tot | cBot fuori/tot | int giorno saltato | cBot giorno saltato | fill fuori griglia int/cBot |
|---|---|---|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | 00:00-24:00 | Research | 0/42 | 0/42 | 0 | 0 | 0/0 |
| PTS_GC_PCH_004_240 | 00:00-12:00 | Research | 0/281 | 0/273 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_003_30 | 14:00-04:00 | Research | 0/70 | 0/70 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_004_30 | 11:00-10:00 | Research | 0/50 | 0/50 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_008_240 | 06:00-24:00 | Research | 0/50 | 0/51 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_003_15 | 13:00-05:00 | Research | 0/42 | 0/42 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_001_15 | 17:00-10:00 | Research | 0/33 | 0/33 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_003_15 | 17:00-03:00 | Research | 0/54 | 0/55 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | Research | 0/47 | 0/47 | 0 | 0 | 0/0 |
| **TOTALE** | | | 0 | 0 | 0 | 0 | 0/0 |

## 4. Sessione: ancoraggio e un fill per sessione per lato

| strategia | sessione (ancoraggio) | max ingressi/sessione | int sessioni con >max per lato | cBot sessioni con >max per lato | int ingressi weekend UTC | cBot ingressi weekend UTC |
|---|---|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_GC_PCH_004_240 | 00:00-24:00 | 1 | 4 | 0 | 10 | 1 |
| PTS_NQ_PCH_003_30 | 00:00-24:00 | 0 | 0 | 0 | 2 | 3 |
| PTS_NQ_PCH_004_30 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_PCH_008_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_003_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFU_001_15 | 00:00-24:00 | 0 | 0 | 0 | 2 | 2 |
| PTS_NQ_TFU_003_15 | 00:00-24:00 | 0 | 0 | 0 | 5 | 5 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 3 |
| **TOTALE** | | | 4 | 0 | | |

Nel log del cBot il server ha rifiutato template per `limite di ingressi per sessione raggiunto` (conteggio righe, per strategia e lato, prime 15):


## 5. Spread

Spread dell'interno, dal summary: `FTMOPLATFORM · p50Spread · per simbolo · FTMOPLATFORM_spread-by-symbol_20260801-20260901.csv (2026-09-10)`. Il cBot lo paga sui tick: costo per trade ≈ spread al fill × valore punto × contratti (una volta per round trip). Lo stop è quello eseguito (allargato ×1 dove previsto).

| strategia | simbolo | fill con spread | spread medio (punti) | stop (punti) | spread/stop | costo spread tot (per contratto, $) | netto/ctr cBot | netto/ctr senza spread | costo/trade ($/ctr) |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_ES_BSW_003_15 | ES | 0/42 | 0 | 0 | 0.0% | 0 | 9,361 | 9,361 | 0 |
| PTS_GC_PCH_004_240 | GC | 0/273 | 0 | 5 | 0.0% | 0 | 16,476 | 16,476 | 0 |
| PTS_NQ_PCH_003_30 | NQ | 0/70 | 0 | 125 | 0.0% | 0 | 14,839 | 14,839 | 0 |
| PTS_NQ_PCH_004_30 | NQ | 0/50 | 0 | 112.5 | 0.0% | 0 | 21,683 | 21,683 | 0 |
| PTS_NQ_PCH_008_240 | NQ | 0/51 | 0 | 200 | 0.0% | 0 | 74 | 74 | 0 |
| PTS_NQ_TFM_003_15 | NQ | 0/42 | 0 | 125 | 0.0% | 0 | 34,022 | 34,022 | 0 |
| PTS_NQ_TFU_001_15 | NQ | 0/33 | 0 | 200 | 0.0% | 0 | 30,151 | 30,151 | 0 |
| PTS_NQ_TFU_003_15 | NQ | 0/55 | 0 | 87.5 | 0.0% | 0 | 11,716 | 11,716 | 0 |
| PTS_NQ_TFU_006_1440 | NQ | 0/47 | 0 | 50 | 0.0% | 0 | 41,960 | 41,960 | 0 |
| **TOTALE** | | | | | | 0 | 180,281 | 180,281 | |

### 5b. Strategie con stop più stretto dello spread, o quasi

| strategia | spread/stop | stop (punti) | trade cBot | netto/ctr cBot | commento |
|---|---:|---:|---:|---:|---|

Sui trade abbinati: 0 volte il cBot esce per stop dove l'interno no; 351 il contrario.

## 6. Strategie con curva di equity crescente

Metriche su netto per contratto, trade ordinati per uscita. R² = fit lineare della curva cumulata sull'indice dei trade. Crescente = netto > 0, R² ≥ 0,6, chiusura ad almeno il 70% del picco, ≥ 8 trade. Il cBot è calcolato sull'intero run (ingressi 01/09/2025 → 30/08/2026), l'interno sull'intero run (ingressi 01/09/2025 → 30/08/2026).

| strategia | cBot n | cBot netto/ctr | PF | R² | DD max/ctr | fine/picco | **cBot** | int n | int netto/ctr | PF | R² | DD max/ctr | fine/picco | **int** |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| PTS_ES_BSW_003_15 | 42 | 9,361 | 1.14 | 0.34 | 18,680 | 0.42 | no | 42 | 16,051 | 1.25 | 0.51 | 18,024 | 0.59 | no |
| PTS_GC_PCH_004_240 | 273 | 16,476 | 1.12 | 0.51 | 20,859 | 0.64 | no | 281 | 72,414 | 1.60 | 0.87 | 11,909 | 0.91 | SÌ |
| PTS_NQ_PCH_003_30 | 70 | 14,839 | 1.32 | 0.04 | 20,882 | 0.66 | no | 70 | 19,673 | 1.48 | 0.19 | 19,581 | 0.83 | no |
| PTS_NQ_PCH_004_30 | 50 | 21,683 | 2.29 | 0.36 | 12,348 | 0.91 | no | 50 | 23,273 | 2.59 | 0.47 | 11,210 | 0.92 | no |
| PTS_NQ_PCH_008_240 | 51 | 74 | 1.00 | 0.16 | 16,836 | 0.01 | no | 50 | 5,410 | 1.14 | 0.31 | 17,021 | 0.34 | no |
| PTS_NQ_TFM_003_15 | 42 | 34,022 | 1.60 | 0.57 | 23,422 | 0.93 | no | 42 | 34,832 | 1.63 | 0.60 | 22,536 | 0.93 | no |
| PTS_NQ_TFU_001_15 | 33 | 30,151 | 1.51 | 0.75 | 14,657 | 0.88 | SÌ | 33 | 31,523 | 1.54 | 0.78 | 14,361 | 0.89 | SÌ |
| PTS_NQ_TFU_003_15 | 55 | 11,716 | 1.30 | 0.62 | 9,934 | 0.54 | no | 54 | 13,915 | 1.38 | 0.70 | 8,066 | 0.63 | no |
| PTS_NQ_TFU_006_1440 | 47 | 41,960 | 1.88 | 0.08 | 31,066 | 0.98 | no | 47 | 43,989 | 1.96 | 0.04 | 30,927 | 1.00 | no |

**Crescenti sul cBot:** PTS_NQ_TFU_001_15

**Crescenti sull'interno:** PTS_GC_PCH_004_240, PTS_NQ_TFU_001_15

**Crescenti su entrambi:** PTS_NQ_TFU_001_15

## 7. Equity aggregata per mese (netto per contratto, sovrapposizione)

| mese (uscita) | interno | cBot | Δ |
|---|---:|---:|---:|
| 2025-09 | 37,998 (cum 37,998) | 34,268 (cum 34,268) | -3,730 |
| 2025-10 | 35,249 (cum 73,246) | 32,862 (cum 67,130) | -2,387 |
| 2025-11 | 53,478 (cum 126,724) | 51,768 (cum 118,898) | -1,709 |
| 2025-12 | -33,678 (cum 93,046) | -35,653 (cum 83,245) | -1,975 |
| 2026-01 | -28,934 (cum 64,111) | -23,940 (cum 59,305) | 4,994 |
| 2026-02 | 5,655 (cum 69,767) | -10,879 (cum 48,426) | -16,534 |
| 2026-03 | 13,209 (cum 82,975) | 1,768 (cum 50,194) | -11,441 |
| 2026-04 | 58,506 (cum 141,481) | 46,268 (cum 96,462) | -12,238 |
| 2026-05 | 90,511 (cum 231,993) | 79,505 (cum 175,967) | -11,006 |
| 2026-06 | 4,254 (cum 236,247) | -8,996 (cum 166,971) | -13,250 |
| 2026-07 | 17,845 (cum 254,092) | 12,294 (cum 179,265) | -5,551 |
| 2026-08 | 6,987 (cum 261,079) | 1,016 (cum 180,281) | -5,972 |

## 9. Quando arrivano gli intent al cBot (righe `Intent ... attesa`)

`attesa` = ValidFrom − ora del server: negativo = l'intent arriva dopo l'inizio della barra su cui è valido. Per tipo di ordine e timeframe della strategia.

| tipo | tf | intent | in orario (≤ 10 s) | 10 s – 5 min | 5 min – 1 barra | ≥ 1 barra (in ritardo di una barra intera) | esempio strategie ≥ 1 barra |
|---|---:|---:|---:|---:|---:|---:|---|

### 9b. Trade cBot nati da un intent arrivato in ritardo di almeno una barra

Trade cBot con intent risolto: 0 su 663; nati da intent in ritardo ≥ 1 barra: 0, netto/ctr 0; di questi senza corrispondente interno: 0.

| strategia | trade da intent stantio | netto/ctr | non abbinati |
|---|---:|---:|---:|

Trade abbinati in cui il cBot esce per stop più di 5 minuti PRIMA dell'uscita interna (che non era uno stop): 0, Δ netto/ctr 0. Per strategia: .

## 8. Avvisi ed errori nel log del cBot (messaggi normalizzati, prime 25)


Ingressi scartati per classe:

Ingressi annullati per classe:

`Nessun intent per l'account` per motivo:

Ingressi scartati `lato sbagliato` per strategia (prime 15):

