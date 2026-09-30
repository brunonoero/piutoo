# Percorso v4 — @NIY 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (22,537 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,267 barre), mai vista dal percorso
- costi FTMO: spread 10.00 punti (mediana), swap long 8.4766 short 3.6329 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 15,000 nella ricerca, 15,770 nella prova

## PCH Price Channel

554 simulazioni in 20.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=30, OffsetTicks=10, Direction=1 | ✔ | avg smussato 12,853.2, netto 2,866,265, 223 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 19,014.2, netto 2,866,265, 223 trade |
| finestra: fine | EndHour | -1 | 6 | · | avg smussato 21,152.0, netto 3,031,214, 141 trade; annullato dall'ultimo terzo (757,139 -> 197,323) |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 2,664,134 (migliore 2,801,619), 223 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 12,702.0, netto 2,832,551, 223 trade |
| YES | PtnNeutYes | 55 | 54 | · | batte lo spento: netto 1,952,164 contro 2,832,551, net/DD 4.07 contro 3.38; annullato dall'ultimo terzo (1,104,812 -> -264,686) |
| NO | PtnNeutNo | 56 | 45 | · | batte lo spento: netto 3,219,658 contro 2,832,551, net/DD 4.23 contro 3.38; annullato dall'ultimo terzo (1,104,812 -> 814,812) |
| direzionale YES | PtnDirYes | 52 | -44 | · | batte lo spento: netto 2,129,835 contro 2,832,551, net/DD 4.13 contro 3.38; annullato dall'ultimo terzo (1,104,812 -> 172,548) |
| direzionale NO | PtnDirNo | 53 | 12 | · | batte lo spento: netto 2,575,340 contro 2,832,551, net/DD 3.66 contro 3.38; annullato dall'ultimo terzo (1,104,812 -> 644,459) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 2,896,942 contro 2,832,551, net/DD 3.44 contro 3.38; annullato dall'ultimo terzo (1,104,812 -> 573,384) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 2,664,134 (migliore 2,801,619), 223 trade |

**Storia di ricerca**: 371 trade, netto 3,937,362, DD 1,502,331, average trade 10,613, UngerFit 0.71.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 371 su almeno 50 |
| average trade | **no** | 10,613 contro la soglia 15,000 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 97.3 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,104,812 |
| ultimo terzo | **no** | netto 1,104,812, net/DD 0.74 (serve 1,3) |
| due terzi su tre | passa | 1,247,180 / 1,585,371 / 1,104,812 |
| outlier | **no** | trade migliore 12% del netto sulla storia, 43% sull'ultimo terzo |
| plateau | passa | media 0.74 su 5 vicini, minimo 0.59 |

**Prova su FTMO**: 253 trade, netto 1,591,495, DD 4,305,161, net/DD 0.37, average trade 6,290 (soglia 15,770), UngerFit 0.24, finestre in utile 3/4 (-481,804 / 136,036 / 1,440,658 / 496,605).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.37 (serve 1), average trade 6,290 (soglia 15,770), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 40% del netto (massimo 40%); average trade / soglia per anno: 2020:4.3 2021:0.7 2022:0.6 2023:1.4 2024:0.7 2025:-1.1 2026:1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=30, OffsetTicks=10, Direction=1`

## TFM Trend following mirrored

435 simulazioni in 16.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 1 | ✔ | nessuna in utile: netto massimo -675,950 |
| finestra: fine | EndHour | -1 | 5 | ✔ | nessuna in utile: netto massimo -376,714 |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 160,845 (migliore 160,845), 434 trade; annullato dall'ultimo terzo (4,037,307 -> 3,263,729) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -376,714 |
| YES | PtnNeutYes | 55 | 2 | · | batte lo spento: netto 1,870,600 contro -376,714, net/DD 1.32 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> -506,004) |
| NO | PtnNeutNo | 56 | 5 | · | batte lo spento: netto 1,870,600 contro -376,714, net/DD 1.32 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> -506,004) |
| direzionale YES | PtnDirYes | 52 | 37 | · | batte lo spento: netto 1,234,486 contro -376,714, net/DD 1.32 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 578,857) |
| direzionale NO | PtnDirNo | 53 | 49 | · | batte lo spento: netto 1,426,186 contro -376,714, net/DD 1.04 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 1,287,564) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 498,571 contro -376,714, net/DD 0.17 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 1,840,237) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 531,268 contro -376,714, net/DD 0.14 contro -0.10 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 885,287 (migliore 885,287), 432 trade |

**Storia di ricerca**: 670 trade, netto 4,622,089, DD 3,684,479, average trade 6,899, UngerFit 0.29.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 670 su almeno 50 |
| average trade | **no** | 6,899 contro la soglia 15,000 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 175.8 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,090,821 |
| ultimo terzo | passa | netto 4,090,821, net/DD 2.88 (serve 1,3) |
| due terzi su tre | passa | 2,385,782 / -1,854,514 / 4,090,821 |
| outlier | passa | trade migliore 11% del netto sulla storia, 13% sull'ultimo terzo |
| plateau | passa | media 0.89 su 4 vicini, minimo 0.70 |

**Prova su FTMO**: 323 trade, netto -3,873,771, DD 4,345,877, net/DD -0.89, average trade -11,993 (soglia 15,770), UngerFit , finestre in utile 1/4 (-1,545,427 / -866,106 / 1,218,274 / -2,755,551).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.89 (serve 1), average trade -11,993 (soglia 15,770), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 302% del netto (massimo 40%); average trade / soglia per anno: 2020:1.7 2021:0.9 2022:-0.9 2023:0.8 2024:0.4 2025:0.1 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=1, EndHour=5, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

729 simulazioni in 26.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 1 | ✔ | nessuna in utile: netto massimo -675,950 |
| finestra: fine | EndHour | -1 | 5 | ✔ | nessuna in utile: netto massimo -376,714 |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 160,845 (migliore 160,845), 434 trade; annullato dall'ultimo terzo (4,037,307 -> 3,263,729) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -376,714 |
| YES long | FastYesLong | 152 | 87 | · | batte lo spento: netto 450,657 contro -376,714, net/DD 0.20 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 3,434,981) |
| YES short | FastYesShort | 152 | 10 | · | batte lo spento: netto 3,126,079 contro -376,714, net/DD 2.06 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 1,941,214) |
| NO long | FastNoLong | 153 | 81 | · | batte lo spento: netto 450,657 contro -376,714, net/DD 0.20 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 3,215,981) |
| NO short | FastNoShort | 153 | 16 | · | batte lo spento: netto 3,126,079 contro -376,714, net/DD 2.06 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 1,941,214) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 498,571 contro -376,714, net/DD 0.17 contro -0.10; annullato dall'ultimo terzo (4,037,307 -> 1,840,237) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 531,268 contro -376,714, net/DD 0.14 contro -0.10 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 885,287 (migliore 885,287), 432 trade |

**Storia di ricerca**: 670 trade, netto 4,622,089, DD 3,684,479, average trade 6,899, UngerFit 0.29.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 670 su almeno 50 |
| average trade | **no** | 6,899 contro la soglia 15,000 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 175.8 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,090,821 |
| ultimo terzo | passa | netto 4,090,821, net/DD 2.88 (serve 1,3) |
| due terzi su tre | passa | 2,385,782 / -1,854,514 / 4,090,821 |
| outlier | passa | trade migliore 11% del netto sulla storia, 13% sull'ultimo terzo |
| plateau | passa | media 0.89 su 4 vicini, minimo 0.70 |

**Prova su FTMO**: 323 trade, netto -3,873,771, DD 4,345,877, net/DD -0.89, average trade -11,993 (soglia 15,770), UngerFit , finestre in utile 1/4 (-1,545,427 / -866,106 / 1,218,274 / -2,755,551).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.89 (serve 1), average trade -11,993 (soglia 15,770), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 302% del netto (massimo 40%); average trade / soglia per anno: 2020:1.7 2021:0.9 2022:-0.9 2023:0.8 2024:0.4 2025:0.1 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=1, EndHour=5, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1`

## BO Breakout di N sessioni

510 simulazioni in 34.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=4, BreakoutOffsetTicks=0, IncludeCurrentSession=1 | ✔ | avg smussato 589.3, netto 200,955, 341 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 10,270.7, netto 200,955, 341 trade |
| finestra: fine | EndHour | -1 | 0 | · | avg smussato 740.6, netto 513,021, 97 trade; annullato dall'ultimo terzo (3,435,284 -> 784,824) |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 832,834 (migliore 832,834), 343 trade; annullato dall'ultimo terzo (3,435,284 -> 2,474,849) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 589.3, netto 200,955, 341 trade |
| YES | PtnNeutYes | 55 | 19 | · | batte lo spento: netto 1,234,682 contro 200,955, net/DD 1.23 contro 0.07; annullato dall'ultimo terzo (3,435,284 -> 2,035,084) |
| NO | PtnNeutNo | 56 | 13 | · | batte lo spento: netto 1,234,682 contro 200,955, net/DD 1.23 contro 0.07; annullato dall'ultimo terzo (3,435,284 -> 2,035,084) |
| direzionale YES | PtnDirYes | 52 | -5 | · | batte lo spento: netto 2,173,269 contro 200,955, net/DD 4.79 contro 0.07; annullato dall'ultimo terzo (3,435,284 -> 1,775,106) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 1,502,675 contro 200,955, net/DD 4.01 contro 0.07; annullato dall'ultimo terzo (3,435,284 -> 548,441) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 1,681,822 contro 200,955, net/DD 0.78 contro 0.07; annullato dall'ultimo terzo (3,435,284 -> 2,511,370) |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 2,415,491 contro 200,955, net/DD 1.89 contro 0.07; annullato dall'ultimo terzo (3,435,284 -> 2,088,124) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 1,180,063 contro 200,955, net/DD 0.52 contro 0.07; annullato dall'ultimo terzo (3,435,284 -> 1,353,460) |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 839,257 (migliore 839,257), 341 trade |

**Storia di ricerca**: 523 trade, netto 4,534,092, DD 2,627,987, average trade 8,669, UngerFit 0.44.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 523 su almeno 50 |
| average trade | **no** | 8,669 contro la soglia 15,000 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 137.2 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 3,822,838 |
| ultimo terzo | passa | netto 3,822,838, net/DD 2.94 (serve 1,3) |
| due terzi su tre | passa | 1,535,682 / -824,428 / 3,822,838 |
| outlier | passa | trade migliore 17% del netto sulla storia, 20% sull'ultimo terzo |
| plateau | passa | media 0.85 su 5 vicini, minimo 0.67 |

**Prova su FTMO**: 264 trade, netto -1,107,310, DD 3,488,937, net/DD -0.32, average trade -4,194 (soglia 15,770), UngerFit , finestre in utile 2/4 (1,536,800 / 285,672 / -2,051,900 / -877,882).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.32 (serve 1), average trade -4,194 (soglia 15,770), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 147% del netto (massimo 40%); average trade / soglia per anno: 2020:0.6 2021:1.0 2022:-0.6 2023:0.4 2024:2.5 2025:-1.4 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=4, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

441 simulazioni in 27.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=20 | ✔ | nessuna in utile: netto massimo -20,710 |
| finestra: inizio | StartHour | -1 | 3 | · | avg smussato 4,501.8, netto 1,346,931, 200 trade; annullato dall'ultimo terzo (3,646,072 -> 491,063) |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -20,710 |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 316,019 (migliore 316,019), 441 trade; annullato dall'ultimo terzo (3,646,072 -> 3,006,497) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -20,710 |
| YES | PtnNeutYes | 55 | 53 | · | batte lo spento: netto 2,532,505 contro -20,710, net/DD 1.99 contro -0.01; annullato dall'ultimo terzo (3,646,072 -> 1,980,237) |
| NO | PtnNeutNo | 56 | 54 | · | batte lo spento: netto 2,104,562 contro -20,710, net/DD 1.38 contro -0.01; annullato dall'ultimo terzo (3,646,072 -> 2,005,237) |
| direzionale YES | PtnDirYes | 52 | -5 | · | batte lo spento: netto 1,786,279 contro -20,710, net/DD 1.87 contro -0.01; annullato dall'ultimo terzo (3,646,072 -> 246,077) |
| direzionale NO | PtnDirNo | 53 | 1 | · | batte lo spento: netto 1,516,479 contro -20,710, net/DD 0.70 contro -0.01; annullato dall'ultimo terzo (3,646,072 -> 991,011) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 2,267,897 contro -20,710, net/DD 1.12 contro -0.01; annullato dall'ultimo terzo (3,646,072 -> 1,359,680) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 182,969 contro -20,710, net/DD 0.07 contro -0.01 |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 688,063 (migliore 688,063), 435 trade; annullato dall'ultimo terzo (4,413,607 -> 3,191,745) |

**Storia di ricerca**: 750 trade, netto 4,596,576, DD 2,797,894, average trade 6,129, UngerFit 0.30.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 750 su almeno 50 |
| average trade | **no** | 6,129 contro la soglia 15,000 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 196.8 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,413,607 |
| ultimo terzo | passa | netto 4,413,607, net/DD 1.79 (serve 1,3) |
| due terzi su tre | passa | -248,746 / 431,714 / 4,413,607 |
| outlier | passa | trade migliore 23% del netto sulla storia, 24% sull'ultimo terzo |
| plateau | passa | media 0.74 su 5 vicini, minimo 0.32 |

**Prova su FTMO**: 536 trade, netto 3,949,921, DD 6,286,460, net/DD 0.63, average trade 7,369 (soglia 15,770), UngerFit 0.23, finestre in utile 3/4 (1,611,252 / -1,807,987 / 3,955,428 / 307,617).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.63 (serve 1), average trade 7,369 (soglia 15,770), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 82% del netto (massimo 40%); average trade / soglia per anno: 2020:3.2 2021:0.1 2022:-0.6 2023:0.6 2024:1.9 2025:-0.6 2026:0.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=20`

## VBO Volatility breakout

453 simulazioni in 17.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.7, Direction=1 | ✔ | avg smussato 15,756.6, netto 2,883,450, 183 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 40,232.4, netto 2,883,450, 183 trade |
| finestra: fine | EndHour | -1 | 10 | · | avg smussato 19,863.4, netto 2,343,018, 115 trade; annullato dall'ultimo terzo (1,505,861 -> 1,011,922) |
| stop | StopAtr | 0.8 | 2.0 | · | D2: netto smussato 2,996,984 (migliore 3,063,607), 183 trade; annullato dall'ultimo terzo (1,505,861 -> 1,264,045) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 15,756.6, netto 2,883,450, 183 trade |
| YES | PtnNeutYes | 55 | 12 | · | batte lo spento: netto 2,406,297 contro 2,883,450, net/DD 4.08 contro 3.54; annullato dall'ultimo terzo (1,505,861 -> 622,270) |
| NO | PtnNeutNo | 56 | 18 | · | batte lo spento: netto 2,406,297 contro 2,883,450, net/DD 4.08 contro 3.54; annullato dall'ultimo terzo (1,505,861 -> 622,270) |
| direzionale YES | PtnDirYes | 52 | 8 | · | batte lo spento: netto 2,209,350 contro 2,883,450, net/DD 14.98 contro 3.54; annullato dall'ultimo terzo (1,505,861 -> 598,630) |
| direzionale NO | PtnDirNo | 53 | 21 | · | batte lo spento: netto 2,617,860 contro 2,883,450, net/DD 4.39 contro 3.54; annullato dall'ultimo terzo (1,505,861 -> 719,750) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 3,812,978 contro 2,883,450, net/DD 6.89 contro 3.54; annullato dall'ultimo terzo (1,505,861 -> 213,746) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 2,729,972 (migliore 2,868,025), 183 trade |

**Storia di ricerca**: 286 trade, netto 4,850,270, DD 859,448, average trade 16,959, UngerFit 1.49.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 286 su almeno 50 |
| average trade | passa | 16,959 contro la soglia 15,000 |
| anni | passa | 5 anni, minimo 5 trade in un anno, 75.0 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,833,892 |
| ultimo terzo | passa | netto 1,833,892, net/DD 2.13 (serve 1,3) |
| due terzi su tre | passa | 721,699 / 2,294,680 / 1,833,892 |
| outlier | passa | trade migliore 8% del netto sulla storia, 22% sull'ultimo terzo |
| plateau | **no** | media 0.50 su 4 vicini, minimo 0.12 |

**Prova su FTMO**: 138 trade, netto 11,159,966, DD 1,261,571, net/DD 8.85, average trade 80,869 (soglia 15,770), UngerFit 5.73, finestre in utile 4/4 (1,529,830 / 2,046,577 / 2,061,542 / 5,522,017).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 8.85 (serve 1), average trade 80,869 (soglia 15,770), finestre 4/4 (servono 3) |
| anni | **no** | 7/7 anni in utile (serve 60%), anno migliore 48% del netto (massimo 40%); average trade / soglia per anno: 2020:2.8 2021:0.6 2022:1.5 2023:1.0 2024:2.9 2025:1.4 2026:7.2 |
| altri mercati | **no** | 3/7 con net/DD ≥ 1 (serve meta'): @FDAX 2.41, @NQ 3.21, @ES 2.27, @YM -0.08, @FESX 0.95, @FCE -0.14, @Z -0.20 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.7, Direction=1`

## MAC Incrocio di medie

126 simulazioni in 3.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=15, SlowPeriod=50, Direction=1 | ✔ | avg smussato 10,752.9, netto 1,655,943, 154 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 2,699,345 (migliore 2,699,345), 154 trade; annullato dall'ultimo terzo (753,421 -> 379,899) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 10,752.9, netto 1,655,943, 154 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 1,715,690 (migliore 1,715,690), 154 trade |

**Storia di ricerca**: 233 trade, netto 2,706,298, DD 705,759, average trade 11,615, UngerFit 1.13.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 233 su almeno 50 |
| average trade | **no** | 11,615 contro la soglia 15,000 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 61.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 952,968 |
| ultimo terzo | passa | netto 952,968, net/DD 1.48 (serve 1,3) |
| due terzi su tre | passa | -348,696 / 2,102,027 / 952,968 |
| outlier | **no** | trade migliore 16% del netto sulla storia, 34% sull'ultimo terzo |
| plateau | **no** | media 0.47 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 136 trade, netto 5,650,811, DD 2,500,503, net/DD 2.26, average trade 41,550 (soglia 15,770), UngerFit 2.09, finestre in utile 3/4 (-1,484,287 / 251,432 / 3,257,735 / 3,625,930).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 2.26 (serve 1), average trade 41,550 (soglia 15,770), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 64% del netto (massimo 40%); average trade / soglia per anno: 2020:-3.6 2021:-0.2 2022:1.2 2023:1.6 2024:-0.5 2025:1.1 2026:5.4 |
| altri mercati | **no** | 1/7 con net/DD ≥ 1 (serve meta'): @FDAX 0.16, @NQ 1.11, @ES -0.56, @YM 0.12, @FESX -0.93, @FCE -0.83, @Z -0.75 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=15, SlowPeriod=50, Direction=1`

## RBM Reversal Bollinger mirrored

464 simulazioni in 15.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -2,368,423 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | avg smussato -1,258.9, netto 97,524, 60 trade |
| finestra: fine | EndHour | -1 | 21 | ✔ | avg smussato 5,314.5, netto 272,789, 50 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 295,733 (migliore 295,733), 50 trade; annullato dall'ultimo terzo (40,510 -> -1,909) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 5,455.8, netto 272,789, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 263,780 (migliore 272,789), 50 trade; annullato dall'ultimo terzo (40,510 -> -118,987) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 5,089.1, netto 245,289, 50 trade; annullato dall'ultimo terzo (40,510 -> -66,990) |

**Storia di ricerca**: 70 trade, netto 313,299, DD 357,209, average trade 4,476, UngerFit 0.61.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 70 su almeno 50 |
| average trade | **no** | 4,476 contro la soglia 15,000 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 18.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 40,510 |
| ultimo terzo | **no** | netto 40,510, net/DD 0.19 (serve 1,3) |
| due terzi su tre | passa | 356,579 / -83,790 / 40,510 |
| outlier | **no** | trade migliore 81% del netto sulla storia, 630% sull'ultimo terzo |
| plateau | **no** | media 0.31 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 33 trade, netto -772,088, DD 1,466,252, net/DD -0.53, average trade -23,397 (soglia 15,770), UngerFit , finestre in utile 2/4 (-424,124 / 345,245 / 16,654 / -709,862).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.53 (serve 1), average trade -23,397 (soglia 15,770), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:0.5 2021:0.1 2022:0.6 2023:-0.3 2024:-1.0 2025:1.6 2026:-3.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=17, EndHour=21, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

758 simulazioni in 24.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -2,368,423 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | avg smussato -1,258.9, netto 97,524, 60 trade |
| finestra: fine | EndHour | -1 | 21 | ✔ | avg smussato 5,314.5, netto 272,789, 50 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 295,733 (migliore 295,733), 50 trade; annullato dall'ultimo terzo (40,510 -> -1,909) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 152 | · | nessun valore sopra lo spento |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 153 | · | nessun valore sopra lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 5,455.8, netto 272,789, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 263,780 (migliore 272,789), 50 trade; annullato dall'ultimo terzo (40,510 -> -118,987) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 5,089.1, netto 245,289, 50 trade; annullato dall'ultimo terzo (40,510 -> -66,990) |

**Storia di ricerca**: 70 trade, netto 313,299, DD 357,209, average trade 4,476, UngerFit 0.61.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 70 su almeno 50 |
| average trade | **no** | 4,476 contro la soglia 15,000 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 18.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 40,510 |
| ultimo terzo | **no** | netto 40,510, net/DD 0.19 (serve 1,3) |
| due terzi su tre | passa | 356,579 / -83,790 / 40,510 |
| outlier | **no** | trade migliore 81% del netto sulla storia, 630% sull'ultimo terzo |
| plateau | **no** | media 0.31 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 33 trade, netto -772,088, DD 1,466,252, net/DD -0.53, average trade -23,397 (soglia 15,770), UngerFit , finestre in utile 2/4 (-424,124 / 345,245 / 16,654 / -709,862).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.53 (serve 1), average trade -23,397 (soglia 15,770), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:0.5 2021:0.1 2022:0.6 2023:-0.3 2024:-1.0 2025:1.6 2026:-3.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=17, EndHour=21, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

667 simulazioni in 19.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=1 | ✔ | nessuna in utile: netto massimo -1,077,707 |
| finestra: inizio | StartHour | -1 | 16 | · | avg smussato 5,112.6, netto 1,026,007, 134 trade; annullato dall'ultimo terzo (-137,079 -> -158,607) |
| finestra: fine | EndHour | -1 | 6 | · | nessuna in utile: netto massimo -823,764; annullato dall'ultimo terzo (-137,079 -> -204,383) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -1,035,776 (migliore -1,035,776), 189 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 35 | · | batte lo spento: netto 943,952 contro -924,309, net/DD 2.27 contro -0.78; annullato dall'ultimo terzo (289,815 -> -590,345) |
| NO | PtnNeutNo | 56 | 42 | · | batte lo spento: netto 943,952 contro -924,309, net/DD 2.27 contro -0.78; annullato dall'ultimo terzo (289,815 -> -590,345) |
| direzionale YES | PtnDirYes | 52 | 29 | · | batte lo spento: netto 441,746 contro -924,309, net/DD 0.64 contro -0.78; annullato dall'ultimo terzo (289,815 -> -42,490) |
| direzionale NO | PtnDirNo | 53 | 2 | ✔ | batte lo spento: netto 126,812 contro -924,309, net/DD 0.25 contro -0.78 |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 322,486 contro 126,812, net/DD 0.89 contro 0.25; annullato dall'ultimo terzo (458,517 -> 427,815) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 1,565.6, netto 126,812, 81 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 179,120 contro 126,812, net/DD 0.35 contro 0.25 |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato -14,254 (migliore -14,254), 80 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | · | avg smussato 7,710.4, netto 616,830, 80 trade; annullato dall'ultimo terzo (544,218 -> -857,479) |

**Storia di ricerca**: 124 trade, netto 723,337, DD 516,761, average trade 5,833, UngerFit 0.66.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 124 su almeno 50 |
| average trade | **no** | 5,833 contro la soglia 15,000 |
| anni | passa | 4 anni, minimo 19 trade in un anno, 32.5 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 544,218 |
| ultimo terzo | passa | netto 544,218, net/DD 1.69 (serve 1,3) |
| due terzi su tre | passa | 139,518 / 39,602 / 544,218 |
| outlier | **no** | trade migliore 40% del netto sulla storia, 42% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 12,369 con i pattern, 136 senza |
| pattern casuali | passa | 1 estrazioni su 15 fanno almeno altrettanto, p 0.12, Wilson 0.05 |
| plateau | **no** | media 0.45 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 81 trade, netto 501,760, DD 1,842,434, net/DD 0.27, average trade 6,195 (soglia 15,770), UngerFit 0.36, finestre in utile 2/4 (-122,143 / -609,973 / 554,163 / 679,713).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.27 (serve 1), average trade 6,195 (soglia 15,770), finestre 2/4 (servono 3) |
| anni | **no** | 4/6 anni in utile (serve 60%), anno migliore 68% del netto (massimo 40%); average trade / soglia per anno: 2021:0.8 2022:-0.2 2023:0.4 2024:0.7 2025:-0.6 2026:1.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=2, SkipDay=-1, LevelOffsetTicks=20, Direction=1`

## LF Level fader sul pivot di ieri

500 simulazioni in 11.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | avg smussato 3,714.1, netto 735,386, 198 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 744,842 (migliore 752,250), 198 trade; annullato dall'ultimo terzo (-1,054,357 -> -1,257,005) |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 756,118 contro 735,386, net/DD 1.30 contro 1.27 |
| YES | PtnNeutYes | 55 | 35 | ✔ | batte lo spento: netto 953,982 contro 756,118, net/DD 7.27 contro 1.30 |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 19,079.6, netto 953,982, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 955,768 (migliore 955,768), 50 trade; annullato dall'ultimo terzo (-635,307 -> -769,643) |
| affinamento target | TargetAtr | 1.0 | 0 | ✔ | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 4 | · | avg smussato 16,092.0, netto 933,250, 50 trade |

**Storia di ricerca**: 91 trade, netto 297,943, DD 1,049,807, average trade 3,274, UngerFit 0.26.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 91 su almeno 50 |
| average trade | **no** | 3,274 contro la soglia 15,000 |
| anni | passa | 4 anni, minimo 21 trade in un anno, 23.9 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -635,307 |
| ultimo terzo | **no** | netto -635,307, net/DD -0.61 (serve 1,3) |
| due terzi su tre | passa | 283,750 / 649,500 / -635,307 |
| outlier | **no** | trade migliore 81% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -15,495 con i pattern, -8,860 senza |
| pattern casuali | **no** | 22 estrazioni su 27 fanno almeno altrettanto, p 0.82, Wilson 0.71 |
| plateau | **no** | media 0.33 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 105 trade, netto -834,416, DD 2,434,766, net/DD -0.34, average trade -7,947 (soglia 15,770), UngerFit , finestre in utile 2/4 (-1,287,134 / 303,285 / 1,187,855 / -1,038,421).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.34 (serve 1), average trade -7,947 (soglia 15,770), finestre 2/4 (servono 3) |
| anni | **no** | 4/6 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2021:0.3 2022:2.2 2023:0.2 2024:-2.4 2025:1.9 2026:-0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=35, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=20`

## LFHL Level fader sugli estremi di ieri

503 simulazioni in 11.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=5 | ✔ | nessuna in utile: netto massimo -818,957 |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato -703,000 (migliore -703,000), 400 trade; annullato dall'ultimo terzo (-2,435,221 -> -2,612,908) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 49 | ✔ | batte lo spento: netto 594,921 contro -818,957, net/DD 1.51 contro -0.49 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 1 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| calendario short | NotEntryDayShort | -1 | 2 | · | batte lo spento: netto 686,171 contro 594,921, net/DD 2.26 contro 1.51; annullato dall'ultimo terzo (15,550 -> 13,550) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 10,816.8, netto 594,921, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 636,439 (migliore 636,439), 55 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 8 | · | avg smussato 9,078.9, netto 488,616, 50 trade; annullato dall'ultimo terzo (15,550 -> -187,625) |

**Storia di ricerca**: 82 trade, netto 659,800, DD 345,000, average trade 8,046, UngerFit 1.12.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 82 su almeno 50 |
| average trade | **no** | 8,046 contro la soglia 15,000 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 21.5 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 15,550 |
| ultimo terzo | **no** | netto 15,550, net/DD 0.05 (serve 1,3) |
| due terzi su tre | passa | -175,000 / 819,250 / 15,550 |
| outlier | **no** | trade migliore 36% del netto sulla storia, 939% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 576 con i pattern, -9,243 senza |
| pattern casuali | **no** | 12 estrazioni su 45 fanno almeno altrettanto, p 0.28, Wilson 0.21 |
| plateau | **no** | media 0.47 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 56 trade, netto 299,466, DD 956,980, net/DD 0.31, average trade 5,348 (soglia 15,770), UngerFit 0.44, finestre in utile 2/4 (258,590 / -25,249 / 815,855 / -749,730).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.31 (serve 1), average trade 5,348 (soglia 15,770), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 68% del netto (massimo 40%); average trade / soglia per anno: 2020:0.7 2021:-0.6 2022:2.2 2023:0.6 2024:0.3 2025:1.2 2026:-0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=49, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=5`

## Riepilogo

- PCH: scartata; ricerca 371 trade netto 3,937,362 avg 10,613; prova 253 trade netto 1,591,495 net/DD 0.37 avg 6,290 fin 3/4; falliti: average trade, anni, ultimo terzo, outlier, prova sul broker, anni
- TFM: scartata; ricerca 670 trade netto 4,622,089 avg 6,899; prova 323 trade netto -3,873,771 net/DD -0.89 avg -11,993 fin 1/4; falliti: average trade, prova sul broker, anni
- TFU: scartata; ricerca 670 trade netto 4,622,089 avg 6,899; prova 323 trade netto -3,873,771 net/DD -0.89 avg -11,993 fin 1/4; falliti: average trade, prova sul broker, anni
- BO: scartata; ricerca 523 trade netto 4,534,092 avg 8,669; prova 264 trade netto -1,107,310 net/DD -0.32 avg -4,194 fin 2/4; falliti: average trade, prova sul broker, anni
- BOS: scartata; ricerca 750 trade netto 4,596,576 avg 6,129; prova 536 trade netto 3,949,921 net/DD 0.63 avg 7,369 fin 3/4; falliti: average trade, prova sul broker, anni
- VBO: scartata; ricerca 286 trade netto 4,850,270 avg 16,959; prova 138 trade netto 11,159,966 net/DD 8.85 avg 80,869 fin 4/4; falliti: plateau, anni, altri mercati
- MAC: scartata; ricerca 233 trade netto 2,706,298 avg 11,615; prova 136 trade netto 5,650,811 net/DD 2.26 avg 41,550 fin 3/4; falliti: average trade, anni, outlier, plateau, anni, altri mercati
- RBM: scartata; ricerca 70 trade netto 313,299 avg 4,476; prova 33 trade netto -772,088 net/DD -0.53 avg -23,397 fin 2/4; falliti: average trade, anni, ultimo terzo, outlier, plateau, prova sul broker, anni
- RBU: scartata; ricerca 70 trade netto 313,299 avg 4,476; prova 33 trade netto -772,088 net/DD -0.53 avg -23,397 fin 2/4; falliti: average trade, anni, ultimo terzo, outlier, plateau, prova sul broker, anni
- RHL: scartata; ricerca 124 trade netto 723,337 avg 5,833; prova 81 trade netto 501,760 net/DD 0.27 avg 6,195 fin 2/4; falliti: average trade, outlier, plateau, prova sul broker, anni
- LF: scartata; ricerca 91 trade netto 297,943 avg 3,274; prova 105 trade netto -834,416 net/DD -0.34 avg -7,947 fin 2/4; falliti: average trade, utile recente, ultimo terzo, outlier, pattern utile, pattern casuali, plateau, prova sul broker, anni
- LFHL: scartata; ricerca 82 trade netto 659,800 avg 8,046; prova 56 trade netto 299,466 net/DD 0.31 avg 5,348 fin 2/4; falliti: average trade, anni, ultimo terzo, outlier, pattern casuali, plateau, prova sul broker, anni
