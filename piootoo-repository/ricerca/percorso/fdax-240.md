# Percorso v4 — @FDAX 240m

- ricerca: feed interno 2008-01-01 → 2020-11-09 (16,571 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2020-11-09 → 2026-09-01 (8,869 barre), mai vista dal percorso
- costi FTMO: spread 1.33 punti (mediana), swap long 4.5288 short 0.0457 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 240 nella ricerca, 364 nella prova

## PCH Price Channel

646 simulazioni in 42.8 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | avg smussato 47.8, netto 42,655, 893 trade |
| finestra: inizio | StartHour | -1 | 19 | ✔ | avg smussato 152.0, netto 68,682, 452 trade |
| finestra: fine | EndHour | -1 | 2 | · | avg smussato 156.7, netto 70,827, 452 trade; annullato dall'ultimo terzo (1,161 -> -17,746) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 86,465 (migliore 86,465), 492 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 164.1, netto 80,713, 492 trade |
| YES | PtnNeutYes | 55 | 12 | ✔ | batte lo spento: netto 83,754 contro 80,713, net/DD 6.26 contro 4.64 |
| NO | PtnNeutNo | 56 | 53 | · | batte lo spento: netto 70,388 contro 83,754, net/DD 12.28 contro 6.26; annullato dall'ultimo terzo (29,122 -> 25,576) |
| direzionale YES | PtnDirYes | 52 | -1 | ✔ | batte lo spento: netto 94,654 contro 83,754, net/DD 9.24 contro 6.26 |
| direzionale NO | PtnDirNo | 53 |  | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 83,203 contro 94,654, net/DD 9.66 contro 9.24; annullato dall'ultimo terzo (37,245 -> 19,284) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 97,440 (migliore 97,440), 187 trade |

**Storia di ricerca**: 271 trade, netto 131,899, DD 13,920, average trade 487, UngerFit 2.66.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 271 su almeno 50 |
| average trade | passa | 487 contro la soglia 240 |
| anni | passa | 13 anni, minimo 6 trade in un anno, 21.1 all'anno, 12 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 37,245 |
| ultimo terzo | passa | netto 37,245, net/DD 2.68 (serve 1,3) |
| due terzi su tre | passa | 35,231 / 59,423 / 37,245 |
| outlier | **no** | trade migliore 10% del netto sulla storia, 35% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 443 con i pattern, 71 senza |
| pattern casuali | passa | 2 estrazioni su 17 fanno almeno altrettanto, p 0.17, Wilson 0.08 |
| plateau | passa | media 0.86 su 4 vicini, minimo 0.51 |

**Prova su FTMO**: 85 trade, netto 4,705, DD 37,112, net/DD 0.13, average trade 55 (soglia 364), UngerFit 0.15, finestre in utile 2/4 (28,169 / 9,092 / -17,292 / -15,265).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=19, EndHour=-1, PtnNeutYes=12, PtnNeutNo=56, PtnDirYes=-1, PtnDirNo=53, SkipDay=-1, ChannelBars=10, OffsetTicks=0, Direction=1`

## TFM Trend following mirrored

639 simulazioni in 38.5 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 19 | ✔ | avg smussato 11.1, netto 19,248, 1736 trade |
| finestra: fine | EndHour | -1 | 10 | · | avg smussato 18.9, netto 40,822, 2164 trade; annullato dall'ultimo terzo (-42,736 -> -70,878) |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato 55,864 (migliore 55,864), 1736 trade; annullato dall'ultimo terzo (-42,736 -> -73,440) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 11.1, netto 19,248, 1736 trade |
| YES | PtnNeutYes | 55 | 3 | · | batte lo spento: netto 91,272 contro 19,248, net/DD 2.66 contro 0.31; annullato dall'ultimo terzo (-42,736 -> -105,253) |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 100,211 contro 19,248, net/DD 2.97 contro 0.31; annullato dall'ultimo terzo (-42,736 -> -102,937) |
| direzionale YES | PtnDirYes | 52 | 37 | · | batte lo spento: netto 48,534 contro 19,248, net/DD 2.56 contro 0.31; annullato dall'ultimo terzo (-42,736 -> -59,745) |
| direzionale NO | PtnDirNo | 53 | 22 | ✔ | batte lo spento: netto 65,470 contro 19,248, net/DD 2.51 contro 0.31 |
| calendario | SkipDay | -1 | 4 | ✔ | batte lo spento: netto 96,608 contro 65,470, net/DD 3.45 contro 2.51 |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 109,333 contro 96,608, net/DD 4.12 contro 3.45 |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 117,554 contro 109,333, net/DD 5.10 contro 4.12; annullato dall'ultimo terzo (11,136 -> 8,808) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 109,352 (migliore 109,352), 401 trade; annullato dall'ultimo terzo (11,136 -> -15,133) |

**Storia di ricerca**: 586 trade, netto 120,560, DD 26,679, average trade 206, UngerFit 0.81.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 586 su almeno 50 |
| average trade | **no** | 206 contro la soglia 240 |
| anni | passa | 13 anni, minimo 20 trade in un anno, 45.6 all'anno, 11 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 11,228 |
| ultimo terzo | **no** | netto 11,136, net/DD 0.42 (serve 1,3) |
| due terzi su tre | passa | 23,192 / 86,141 / 11,136 |
| outlier | **no** | trade migliore 6% del netto sulla storia, 65% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 61 con i pattern, -2 senza |
| pattern casuali | **no** | 8 estrazioni su 20 fanno almeno altrettanto, p 0.43, Wilson 0.30 |
| plateau | passa | media 0.89 su 3 vicini, minimo 0.84 |

**Prova su FTMO**: 143 trade, netto -42,295, DD 75,990, net/DD -0.56, average trade -296 (soglia 364), UngerFit , finestre in utile 1/4 (7,333 / -6,531 / -19,427 / -23,670).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=19, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=22, SkipDay=4`

## TFU Trend following unmirrored

929 simulazioni in 56.9 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 19 | ✔ | avg smussato 11.1, netto 19,248, 1736 trade |
| finestra: fine | EndHour | -1 | 10 | · | avg smussato 18.9, netto 40,822, 2164 trade; annullato dall'ultimo terzo (-42,736 -> -70,878) |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato 55,864 (migliore 55,864), 1736 trade; annullato dall'ultimo terzo (-42,736 -> -73,440) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 11.1, netto 19,248, 1736 trade |
| YES long | FastYesLong | 152 | 95 | ✔ | batte lo spento: netto 93,749 contro 19,248, net/DD 2.01 contro 0.31 |
| YES short | FastYesShort | 152 | 112 | ✔ | batte lo spento: netto 128,327 contro 93,749, net/DD 10.07 contro 2.01 |
| NO long | FastNoLong | 153 | 145 | ✔ | batte lo spento: netto 115,363 contro 128,327, net/DD 11.82 contro 10.07 |
| NO short | FastNoShort | 153 | 16 | ✔ | batte lo spento: netto 105,748 contro 115,363, net/DD 23.09 contro 11.82 |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 111,581 contro 105,748, net/DD 24.36 contro 23.09 |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 110,618 (migliore 110,939), 99 trade |

**Storia di ricerca**: 162 trade, netto 130,780, DD 11,625, average trade 807, UngerFit 4.84.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 162 su almeno 50 |
| average trade | passa | 807 contro la soglia 240 |
| anni | **no** | 13 anni, minimo 4 trade in un anno, 12.6 all'anno, 12 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 19,649 |
| ultimo terzo | passa | netto 19,649, net/DD 1.69 (serve 1,3) |
| due terzi su tre | passa | 29,778 / 81,354 / 19,649 |
| outlier | **no** | trade migliore 6% del netto sulla storia, 43% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 312 con i pattern, -51 senza |
| pattern casuali | passa | 18 estrazioni su 187 fanno almeno altrettanto, p 0.10, Wilson 0.08 |
| plateau | passa | media 1.00 su 3 vicini, minimo 0.99 |

**Prova su FTMO**: 19 trade, netto -28,189, DD 30,281, net/DD -0.93, average trade -1,484 (soglia 364), UngerFit , finestre in utile 1/4 (2,092 / -3,698 / -10,486 / -16,097).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=19, EndHour=-1, FastYesLong=95, FastYesShort=112, FastNoLong=145, FastNoShort=16, SkipDay=-1`

## BO Breakout di N sessioni

710 simulazioni in 46.1 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | avg smussato 13.6, netto 18,388, 1356 trade |
| finestra: inizio | StartHour | -1 | 11 | · | avg smussato 24.5, netto 31,475, 1286 trade; annullato dall'ultimo terzo (22,137 -> -32,447) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 13.6, netto 18,388, 1356 trade |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato 6,991 (migliore 6,991), 1339 trade; annullato dall'ultimo terzo (22,137 -> -10,382) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 13.6, netto 18,388, 1356 trade |
| YES | PtnNeutYes | 55 | 11 | ✔ | batte lo spento: netto 76,915 contro 18,388, net/DD 3.53 contro 0.33 |
| NO | PtnNeutNo | 56 | 24 | · | batte lo spento: netto 72,459 contro 76,915, net/DD 7.91 contro 3.53; annullato dall'ultimo terzo (37,570 -> 25,323) |
| direzionale YES | PtnDirYes | 52 | -14 | · | batte lo spento: netto 69,359 contro 76,915, net/DD 4.94 contro 3.53; annullato dall'ultimo terzo (37,570 -> -8,866) |
| direzionale NO | PtnDirNo | 53 | 2 | · | batte lo spento: netto 81,661 contro 76,915, net/DD 4.78 contro 3.53; annullato dall'ultimo terzo (37,570 -> 24,383) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 92,169 contro 76,915, net/DD 4.32 contro 3.53; annullato dall'ultimo terzo (37,570 -> 20,988) |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 83,637 contro 76,915, net/DD 3.97 contro 3.53 |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 87,679 contro 83,637, net/DD 4.61 contro 3.97; annullato dall'ultimo terzo (41,526 -> 17,338) |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 85,851 (migliore 85,851), 475 trade |

**Storia di ricerca**: 710 trade, netto 134,826, DD 25,427, average trade 190, UngerFit 0.77.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 710 su almeno 50 |
| average trade | **no** | 190 contro la soglia 240 |
| anni | passa | 13 anni, minimo 43 trade in un anno, 55.2 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 50,477 |
| ultimo terzo | passa | netto 50,477, net/DD 1.99 (serve 1,3) |
| due terzi su tre | passa | 2,720 / 81,629 / 50,477 |
| outlier | passa | trade migliore 10% del netto sulla storia, 27% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 215 con i pattern, 77 senza |
| pattern casuali | passa | 1 estrazioni su 25 fanno almeno altrettanto, p 0.08, Wilson 0.03 |
| plateau | passa | media 0.87 su 6 vicini, minimo 0.71 |

**Prova su FTMO**: 329 trade, netto 44,276, DD 36,143, net/DD 1.23, average trade 135 (soglia 364), UngerFit 0.37, finestre in utile 2/4 (-23,385 / 16,710 / -2,003 / 52,954).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=0, PtnNeutYes=11, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=5, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

531 simulazioni in 33.7 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | nessuna in utile: netto massimo -46,119 |
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -46,119 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -46,119 |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato -46,030 (migliore -46,030), 2262 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -46,119 |
| YES | PtnNeutYes | 55 | 10 | · | batte lo spento: netto 51,131 contro -46,119, net/DD 1.93 contro -0.42; annullato dall'ultimo terzo (5,243 -> -14,187) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 51,131 contro -46,119, net/DD 1.93 contro -0.42; annullato dall'ultimo terzo (5,243 -> -14,187) |
| direzionale YES | PtnDirYes | 52 | 42 | ✔ | batte lo spento: netto 63,692 contro -46,119, net/DD 2.56 contro -0.42 |
| direzionale NO | PtnDirNo | 53 |  | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 60,864 contro 63,692, net/DD 4.30 contro 2.56; annullato dall'ultimo terzo (36,367 -> -1,606) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 75,523 contro 63,692, net/DD 3.01 contro 2.56; annullato dall'ultimo terzo (36,367 -> 17,645) |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 60,070 (migliore 60,070), 145 trade |

**Storia di ricerca**: 186 trade, netto 106,376, DD 26,624, average trade 572, UngerFit 2.26.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 186 su almeno 50 |
| average trade | passa | 572 contro la soglia 240 |
| anni | **no** | 13 anni, minimo 3 trade in un anno, 14.5 all'anno, 11 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 44,704 |
| ultimo terzo | passa | netto 44,704, net/DD 1.68 (serve 1,3) |
| due terzi su tre | passa | 36,156 / 25,516 / 44,704 |
| outlier | **no** | trade migliore 28% del netto sulla storia, 67% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 1,090 con i pattern, 15 senza |
| pattern casuali | passa | 3 estrazioni su 98 fanno almeno altrettanto, p 0.04, Wilson 0.02 |
| plateau | passa | media 0.92 su 4 vicini, minimo 0.81 |

**Prova su FTMO**: 28 trade, netto 4,100, DD 44,496, net/DD 0.09, average trade 146 (soglia 364), UngerFit 0.36, finestre in utile 2/4 (-15,564 / 7,619 / -890 / 12,935).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=42, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=10`

## VBO Volatility breakout

453 simulazioni in 41.6 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | avg smussato 13.7, netto 16,731, 1220 trade |
| finestra: inizio | StartHour | -1 | 15 | · | avg smussato 25.9, netto 23,735, 917 trade; annullato dall'ultimo terzo (42,668 -> -12,703) |
| finestra: fine | EndHour | -1 | 6 | · | avg smussato 33.2, netto 29,819, 897 trade; annullato dall'ultimo terzo (42,668 -> 22,002) |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 12,105 (migliore 12,105), 1220 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 13.7, netto 16,731, 1220 trade |
| YES | PtnNeutYes | 55 | 10 | · | batte lo spento: netto 45,154 contro 16,731, net/DD 3.45 contro 0.32; annullato dall'ultimo terzo (42,668 -> 440) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 56,334 contro 16,731, net/DD 2.28 contro 0.32; annullato dall'ultimo terzo (42,668 -> 12,732) |
| direzionale YES | PtnDirYes | 52 | 40 | · | batte lo spento: netto 67,289 contro 16,731, net/DD 3.48 contro 0.32; annullato dall'ultimo terzo (42,668 -> 17,944) |
| direzionale NO | PtnDirNo | 53 | -13 | · | batte lo spento: netto 74,549 contro 16,731, net/DD 3.13 contro 0.32; annullato dall'ultimo terzo (42,668 -> 17,993) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 53,474 contro 16,731, net/DD 0.98 contro 0.32; annullato dall'ultimo terzo (42,668 -> -1,939) |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 76,285 contro 16,731, net/DD 1.90 contro 0.32 |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 82,632 contro 76,285, net/DD 2.05 contro 1.90; annullato dall'ultimo terzo (67,415 -> 59,039) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 70,320 (migliore 70,320), 1220 trade |

**Storia di ricerca**: 1864 trade, netto 142,676, DD 40,153, average trade 77, UngerFit 0.25.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 1864 su almeno 50 |
| average trade | **no** | 77 contro la soglia 240 |
| anni | passa | 13 anni, minimo 129 trade in un anno, 145.0 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 66,391 |
| ultimo terzo | passa | netto 67,415, net/DD 1.68 (serve 1,3) |
| due terzi su tre | passa | 27,468 / 48,897 / 67,415 |
| outlier | passa | trade migliore 9% del netto sulla storia, 19% sull'ultimo terzo |
| plateau | **no** | media 0.56 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 936 trade, netto 91,210, DD 112,196, net/DD 0.81, average trade 97 (soglia 364), UngerFit 0.15, finestre in utile 3/4 (-84,745 / 4,779 / 66,712 / 104,464).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.3, Direction=1`

## MAC Incrocio di medie

125 simulazioni in 9.1 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | avg smussato 673.8, netto 73,440, 109 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 62,693 (migliore 62,693), 112 trade; annullato dall'ultimo terzo (52,513 -> 23,616) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 673.8, netto 73,440, 109 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 59,871 (migliore 59,871), 106 trade |

**Storia di ricerca**: 167 trade, netto 102,650, DD 33,306, average trade 615, UngerFit 2.18.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 167 su almeno 50 |
| average trade | passa | 615 contro la soglia 240 |
| anni | passa | 13 anni, minimo 7 trade in un anno, 13.0 all'anno, 10 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 58,733 |
| ultimo terzo | passa | netto 58,733, net/DD 4.46 (serve 1,3) |
| due terzi su tre | passa | 22,354 / 21,563 / 58,733 |
| outlier | **no** | trade migliore 27% del netto sulla storia, 47% sull'ultimo terzo |
| plateau | passa | media 0.69 su 5 vicini, minimo 0.15 |

**Prova su FTMO**: 92 trade, netto -73,100, DD 81,220, net/DD -0.90, average trade -795 (soglia 364), UngerFit , finestre in utile 0/4 (-21,634 / -13,936 / -22,160 / -15,369).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=15, SlowPeriod=200, Direction=0`

## RBM Reversal Bollinger mirrored

572 simulazioni in 37.1 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | nessuna in utile: netto massimo -608 |
| finestra: inizio | StartHour | -1 | 15 | ✔ | avg smussato 57.6, netto 11,640, 202 trade |
| finestra: fine | EndHour | -1 | 21 | ✔ | avg smussato 65.5, netto 11,640, 202 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 8,890 (migliore 8,890), 202 trade |
| target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 32,165 contro 11,640, net/DD 1.54 contro 0.48; annullato dall'ultimo terzo (-7,285 -> -17,140) |
| YES | PtnNeutYes | 55 | 18 | ✔ | batte lo spento: netto 21,291 contro 11,640, net/DD 1.16 contro 0.48 |
| NO | PtnNeutNo | 56 | 45 | · | batte lo spento: netto 36,597 contro 21,291, net/DD 4.70 contro 1.16; annullato dall'ultimo terzo (16,866 -> 8,721) |
| direzionale YES | PtnDirYes | 52 | 2 | ✔ | batte lo spento: netto 21,862 contro 21,291, net/DD 1.59 contro 1.16 |
| direzionale NO | PtnDirNo | 53 |  | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 26,981 contro 21,862, net/DD 2.73 contro 1.59; annullato dall'ultimo terzo (20,005 -> 9,192) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 455.5, netto 27,785, 61 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 24,597 (migliore 24,597), 61 trade |
| affinamento target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 28,177 contro 27,785, net/DD 2.23 contro 2.20; annullato dall'ultimo terzo (20,153 -> 14,320) |
| durata | MaxBars | 4 | 20 | · | avg smussato 816.2, netto 40,922, 54 trade; annullato dall'ultimo terzo (20,153 -> -14,321) |

**Storia di ricerca**: 87 trade, netto 47,939, DD 12,609, average trade 551, UngerFit 3.17.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 87 su almeno 50 |
| average trade | passa | 551 contro la soglia 240 |
| anni | **no** | 13 anni, minimo 1 trade in un anno, 6.8 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 20,153 |
| ultimo terzo | passa | netto 20,153, net/DD 2.40 (serve 1,3) |
| due terzi su tre | passa | 3,071 / 24,714 / 20,153 |
| outlier | **no** | trade migliore 26% del netto sulla storia, 62% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 775 con i pattern, -68 senza |
| pattern casuali | passa | 2 estrazioni su 23 fanno almeno altrettanto, p 0.12, Wilson 0.06 |
| plateau | passa | media 0.64 su 7 vicini, minimo 0.05 |

**Prova su FTMO**: 31 trade, netto -26,223, DD 34,143, net/DD -0.77, average trade -846 (soglia 364), UngerFit , finestre in utile 0/4 (-6,286 / -6,749 / -7,074 / -6,115).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=0, ExitHour=-1, StartHour=15, EndHour=21, PtnNeutYes=18, PtnNeutNo=56, PtnDirYes=2, PtnDirNo=53, SkipDay=-1, BbLength=30, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

970 simulazioni in 69.9 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | nessuna in utile: netto massimo -608 |
| finestra: inizio | StartHour | -1 | 15 | ✔ | avg smussato 57.6, netto 11,640, 202 trade |
| finestra: fine | EndHour | -1 | 21 | ✔ | avg smussato 65.5, netto 11,640, 202 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 8,890 (migliore 8,890), 202 trade |
| target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 32,165 contro 11,640, net/DD 1.54 contro 0.48; annullato dall'ultimo terzo (-7,285 -> -17,140) |
| YES long | FastYesLong | 152 | 137 | · | batte lo spento: netto 25,737 contro 11,640, net/DD 2.47 contro 0.48; annullato dall'ultimo terzo (-7,285 -> -32,134) |
| YES short | FastYesShort | 152 | 117 | ✔ | batte lo spento: netto 27,916 contro 11,640, net/DD 1.22 contro 0.48 |
| NO long | FastNoLong | 153 | 43 | · | batte lo spento: netto 36,933 contro 27,916, net/DD 3.72 contro 1.22; annullato dall'ultimo terzo (14,681 -> -2,695) |
| NO short | FastNoShort | 153 | 29 | ✔ | batte lo spento: netto 29,271 contro 27,916, net/DD 1.28 contro 1.22 |
| calendario | SkipDay | -1 | 3 | ✔ | batte lo spento: netto 29,352 contro 29,271, net/DD 1.54 contro 1.28 |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 362.6, netto 36,981, 102 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 33,438 (migliore 33,438), 102 trade |
| affinamento target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 44,251 contro 36,981, net/DD 2.49 contro 1.95; annullato dall'ultimo terzo (23,036 -> 5,744) |
| durata | MaxBars | 4 | 4 | · | avg smussato 360.2, netto 36,981, 102 trade |

**Storia di ricerca**: 150 trade, netto 60,017, DD 20,012, average trade 400, UngerFit 1.83.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 150 su almeno 50 |
| average trade | passa | 400 contro la soglia 240 |
| anni | **no** | 13 anni, minimo 7 trade in un anno, 11.7 all'anno, 9 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 23,036 |
| ultimo terzo | **no** | netto 23,036, net/DD 1.15 (serve 1,3) |
| due terzi su tre | passa | -11,942 / 48,923 / 23,036 |
| outlier | **no** | trade migliore 26% del netto sulla storia, 68% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 480 con i pattern, 36 senza |
| pattern casuali | passa | 15 estrazioni su 68 fanno almeno altrettanto, p 0.23, Wilson 0.17 |
| plateau | **no** | media 0.60 su 7 vicini, minimo 0.09 |

**Prova su FTMO**: 37 trade, netto 32,540, DD 28,315, net/DD 1.15, average trade 879 (soglia 364), UngerFit 2.74, finestre in utile 2/4 (368 / -5,465 / -6,936 / 44,573).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=0, ExitHour=-1, StartHour=15, EndHour=21, FastYesLong=152, FastYesShort=117, FastNoLong=153, FastNoShort=29, SkipDay=3, BbLength=30, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

470 simulazioni in 32.3 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | nessuna in utile: netto massimo -32,910 |
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -32,910 |
| finestra: fine | EndHour | -1 | 5 | ✔ | nessuna in utile: netto massimo -21,668 |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato -15,720 (migliore -15,720), 490 trade |
| target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 13,655 contro -10,988, net/DD 0.41 contro -0.21; annullato dall'ultimo terzo (38,931 -> 13,992) |
| YES | PtnNeutYes | 55 | 51 | · | batte lo spento: netto 34,596 contro -10,988, net/DD 2.57 contro -0.21; annullato dall'ultimo terzo (38,931 -> -13,569) |
| NO | PtnNeutNo | 56 | 50 | · | batte lo spento: netto 35,999 contro -10,988, net/DD 2.68 contro -0.21; annullato dall'ultimo terzo (38,931 -> -9,684) |
| direzionale YES | PtnDirYes | 52 | -35 | · | batte lo spento: netto 37,082 contro -10,988, net/DD 2.00 contro -0.21; annullato dall'ultimo terzo (38,931 -> 17,226) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 35,167 contro -10,988, net/DD 1.38 contro -0.21; annullato dall'ultimo terzo (38,931 -> 15,281) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 19,498 contro -10,988, net/DD 0.42 contro -0.21; annullato dall'ultimo terzo (38,931 -> 17,247) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -10,988 |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 10,158 contro -10,988, net/DD 0.34 contro -0.21; annullato dall'ultimo terzo (38,931 -> 21,806) |
| affinamento stop | StopAtr | 1.0 | 1.0 | · | D2: netto smussato -15,720 (migliore -15,720), 490 trade |
| affinamento target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 13,655 contro -10,988, net/DD 0.41 contro -0.21; annullato dall'ultimo terzo (38,931 -> 13,992) |
| durata | MaxBars | 4 | 2 | · | avg smussato -8.8, netto 2,316, 490 trade; annullato dall'ultimo terzo (38,931 -> 20,017) |

**Storia di ricerca**: 769 trade, netto 27,943, DD 66,802, average trade 36, UngerFit 0.09.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 769 su almeno 50 |
| average trade | **no** | 36 contro la soglia 240 |
| anni | passa | 13 anni, minimo 45 trade in un anno, 59.8 all'anno, 7 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 38,931 |
| ultimo terzo | **no** | netto 38,931, net/DD 0.84 (serve 1,3) |
| due terzi su tre | **no** | -5,104 / -5,884 / 38,931 |
| outlier | **no** | trade migliore 74% del netto sulla storia, 53% sull'ultimo terzo |
| plateau | passa | media 0.65 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 392 trade, netto 112,855, DD 78,205, net/DD 1.44, average trade 288 (soglia 364), UngerFit 0.54, finestre in utile 3/4 (11,421 / 17,430 / -19,952 / 103,957).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=5, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=20, Direction=1`

## LF Level fader sul pivot di ieri

303 simulazioni in 19.9 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | avg smussato 61.6, netto 22,530, 366 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 20,726 (migliore 21,134), 371 trade |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 25,839 contro 20,414, net/DD 1.72 contro 1.36; annullato dall'ultimo terzo (16,121 -> 9,901) |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 34,050 contro 20,414, net/DD 5.88 contro 1.36; annullato dall'ultimo terzo (16,121 -> 13,624) |
| NO | PtnNeutNo | 56 | 47 | · | batte lo spento: netto 20,023 contro 20,414, net/DD 3.03 contro 1.36; annullato dall'ultimo terzo (16,121 -> 10,484) |
| direzionale YES | PtnDirYes | 52 | 49 | · | batte lo spento: netto 38,833 contro 20,414, net/DD 7.19 contro 1.36; annullato dall'ultimo terzo (16,121 -> 9,907) |
| calendario long | NotEntryDayLong | -1 | 1 | ✔ | batte lo spento: netto 23,273 contro 20,414, net/DD 2.12 contro 1.36 |
| calendario short | NotEntryDayShort | -1 | 2 | ✔ | batte lo spento: netto 28,969 contro 23,273, net/DD 3.21 contro 2.12 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 102.7, netto 28,969, 282 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 26,965 (migliore 26,965), 282 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 105.0, netto 29,620, 282 trade |

**Storia di ricerca**: 460 trade, netto 60,757, DD 21,247, average trade 132, UngerFit 0.59.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 460 su almeno 50 |
| average trade | **no** | 132 contro la soglia 240 |
| anni | passa | 13 anni, minimo 25 trade in un anno, 35.8 all'anno, 11 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 31,137 |
| ultimo terzo | passa | netto 31,137, net/DD 1.47 (serve 1,3) |
| due terzi su tre | passa | 16,840 / 12,780 / 31,137 |
| outlier | passa | trade migliore 18% del netto sulla storia, 25% sull'ultimo terzo |
| plateau | passa | media 0.74 su 3 vicini, minimo 0.54 |

**Prova su FTMO**: 234 trade, netto 12,495, DD 46,943, net/DD 0.27, average trade 53 (soglia 364), UngerFit 0.13, finestre in utile 2/4 (17,413 / -4,665 / 440 / -693).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=1, NotEntryDayShort=2, LevelShift=10`

## LFHL Level fader sugli estremi di ieri

309 simulazioni in 20.4 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | System.Collections.Generic.Dictionary`2[System.String,System.Object] | System.Collections.Generic.Dictionary`2[System.String,System.Object] | ✔ | nessuna in utile: netto massimo -15,741 |
| stop | StopAtr | 0.8 | 0.4 | · | D2: netto smussato -11,529 (migliore -11,529), 530 trade; annullato dall'ultimo terzo (22,519 -> 13,134) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 48 | · | batte lo spento: netto 33,800 contro -15,741, net/DD 4.48 contro -0.31; annullato dall'ultimo terzo (22,519 -> -22,766) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 22,571 contro -15,741, net/DD 1.16 contro -0.31; annullato dall'ultimo terzo (22,519 -> 2,903) |
| direzionale YES | PtnDirYes | 52 | 40 | · | batte lo spento: netto 20,019 contro -15,741, net/DD 3.75 contro -0.31; annullato dall'ultimo terzo (22,519 -> -13,051) |
| calendario long | NotEntryDayLong | -1 | 2 | ✔ | batte lo spento: netto 7,865 contro -15,741, net/DD 0.19 contro -0.31 |
| calendario short | NotEntryDayShort | -1 | 0 | ✔ | batte lo spento: netto 16,105 contro 7,865, net/DD 0.43 contro 0.19 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 37.6, netto 16,105, 428 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 20,449 (migliore 20,449), 427 trade; annullato dall'ultimo terzo (37,555 -> 30,384) |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 18,710 contro 16,105, net/DD 0.53 contro 0.43; annullato dall'ultimo terzo (37,555 -> 34,927) |
| durata | MaxBars | 4 | 2 | · | avg smussato 39.3, netto 18,030, 441 trade; annullato dall'ultimo terzo (37,555 -> 4,171) |

**Storia di ricerca**: 664 trade, netto 53,660, DD 37,263, average trade 81, UngerFit 0.27.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 664 su almeno 50 |
| average trade | **no** | 81 contro la soglia 240 |
| anni | passa | 13 anni, minimo 34 trade in un anno, 51.6 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 37,555 |
| ultimo terzo | passa | netto 37,555, net/DD 1.57 (serve 1,3) |
| due terzi su tre | passa | -16,981 / 33,086 / 37,555 |
| outlier | passa | trade migliore 14% del netto sulla storia, 20% sull'ultimo terzo |
| plateau | passa | media 0.73 su 5 vicini, minimo 0.43 |

**Prova su FTMO**: 350 trade, netto 154,844, DD 26,441, net/DD 5.86, average trade 442 (soglia 364), UngerFit 1.43, finestre in utile 4/4 (56,871 / 45,197 / 7,913 / 44,864).

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=2, NotEntryDayShort=0, LevelShift=0`

## Riepilogo

- PCH: ricerca 271 trade netto 131,899 avg 487; cancelli falliti outlier; prova 85 trade netto 4,705 net/DD 0.13 avg 55 fin 2/4
- TFM: ricerca 586 trade netto 120,560 avg 206; cancelli falliti average trade, ultimo terzo, outlier, pattern casuali; prova 143 trade netto -42,295 net/DD -0.56 avg -296 fin 1/4
- TFU: ricerca 162 trade netto 130,780 avg 807; cancelli falliti anni, outlier; prova 19 trade netto -28,189 net/DD -0.93 avg -1,484 fin 1/4
- BO: ricerca 710 trade netto 134,826 avg 190; cancelli falliti average trade; prova 329 trade netto 44,276 net/DD 1.23 avg 135 fin 2/4
- BOS: ricerca 186 trade netto 106,376 avg 572; cancelli falliti anni, outlier; prova 28 trade netto 4,100 net/DD 0.09 avg 146 fin 2/4
- VBO: ricerca 1864 trade netto 142,676 avg 77; cancelli falliti average trade, plateau; prova 936 trade netto 91,210 net/DD 0.81 avg 97 fin 3/4
- MAC: ricerca 167 trade netto 102,650 avg 615; cancelli falliti outlier; prova 92 trade netto -73,100 net/DD -0.90 avg -795 fin 0/4
- RBM: ricerca 87 trade netto 47,939 avg 551; cancelli falliti anni, outlier; prova 31 trade netto -26,223 net/DD -0.77 avg -846 fin 0/4
- RBU: ricerca 150 trade netto 60,017 avg 400; cancelli falliti anni, ultimo terzo, outlier, plateau; prova 37 trade netto 32,540 net/DD 1.15 avg 879 fin 2/4
- RHL: ricerca 769 trade netto 27,943 avg 36; cancelli falliti average trade, ultimo terzo, due terzi su tre, outlier; prova 392 trade netto 112,855 net/DD 1.44 avg 288 fin 3/4
- LF: ricerca 460 trade netto 60,757 avg 132; cancelli falliti average trade; prova 234 trade netto 12,495 net/DD 0.27 avg 53 fin 2/4
- LFHL: ricerca 664 trade netto 53,660 avg 81; cancelli falliti average trade; prova 350 trade netto 154,844 net/DD 5.86 avg 442 fin 4/4
