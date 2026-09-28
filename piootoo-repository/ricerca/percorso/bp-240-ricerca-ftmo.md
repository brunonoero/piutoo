# Percorso v4 — @BP 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (6,262 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,341 barre), mai vista dal percorso
- costi FTMO: spread 0.00003 punti (mediana), swap long 0.000059 short 0.0000435 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 38 nella ricerca, 38 nella prova

## PCH Price Channel

649 simulazioni in 22.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=1, OffsetTicks=5, Direction=0 | ✔ | avg smussato 12.8, netto 9,386, 734 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 12.8, netto 9,386, 734 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 12.8, netto 9,386, 734 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 7,069 (migliore 7,069), 734 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 12.8, netto 9,386, 734 trade |
| YES | PtnNeutYes | 55 | 6 | · | batte lo spento: netto 12,676 contro 9,386, net/DD 2.11 contro 1.08; annullato dall'ultimo terzo (-1,726 -> -2,259) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 12,707 contro 9,386, net/DD 2.11 contro 1.08; annullato dall'ultimo terzo (-1,726 -> -2,259) |
| direzionale YES | PtnDirYes | 52 | -36 | ✔ | batte lo spento: netto 17,057 contro 9,386, net/DD 4.08 contro 1.08 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 15,096 contro 17,057, net/DD 4.20 contro 4.08; annullato dall'ultimo terzo (-12 -> -2,299) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.75 | ✔ | batte lo spento: netto 18,121 contro 17,057, net/DD 8.39 contro 4.08 |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 18,927 (migliore 19,331), 350 trade |

**Storia di ricerca**: 536 trade, netto 19,484, DD 2,437, average trade 36, UngerFit 1.20.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 536 su almeno 50 |
| average trade | **no** | 36 contro la soglia 38 |
| anni | passa | 5 anni, minimo 22 trade in un anno, 140.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 999 |
| ultimo terzo | **no** | netto 999, net/DD 0.50 (serve 1,3) |
| due terzi su tre | passa | 6,668 / 11,817 / 999 |
| outlier | **no** | trade migliore 5% del netto sulla storia, 41% sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade 5 con i pattern, 9 senza |
| pattern casuali | **no** | 5 estrazioni su 17 fanno almeno altrettanto, p 0.33, Wilson 0.21 |
| plateau | passa | media 0.77 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 307 trade, netto 4,617, DD 2,693, net/DD 1.71, average trade 15 (soglia 38), UngerFit 0.47, finestre in utile 3/4 (-55 / 1,635 / 979 / 1,909).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.71 (serve 1), average trade 15 (soglia 38), finestre 3/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 31% del netto (massimo 40%); average trade / soglia per anno: 2020:2.9 2021:0.7 2022:1.0 2023:1.4 2024:-0.4 2025:0.6 2026:0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0.75, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=-36, PtnDirNo=53, SkipDay=-1, ChannelBars=1, OffsetTicks=5, Direction=0`

## TFM Trend following mirrored

394 simulazioni in 15.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 5 | · | nessuna in utile: netto massimo -15,023; annullato dall'ultimo terzo (-750 -> -4,005) |
| finestra: fine | EndHour | -1 | 4 | ✔ | nessuna in utile: netto massimo -11,207 |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -5,675 (migliore -5,675), 502 trade; annullato dall'ultimo terzo (-643 -> -2,765) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -11,207 |
| YES | PtnNeutYes | 55 | 10 | · | batte lo spento: netto 1,803 contro -11,207, net/DD 1.05 contro -0.92; annullato dall'ultimo terzo (-643 -> -1,571) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 1,803 contro -11,207, net/DD 1.05 contro -0.92; annullato dall'ultimo terzo (-643 -> -1,571) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 5,682 contro -11,207, net/DD 4.73 contro -0.92; annullato dall'ultimo terzo (-643 -> -1,433) |
| direzionale NO | PtnDirNo | 53 | 14 | · | batte lo spento: netto 1,813 contro -11,207, net/DD 0.40 contro -0.92; annullato dall'ultimo terzo (-643 -> -1,908) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (699 trade, netto -11,850).

## TFU Trend following unmirrored

684 simulazioni in 24.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 5 | · | nessuna in utile: netto massimo -15,023; annullato dall'ultimo terzo (-750 -> -4,005) |
| finestra: fine | EndHour | -1 | 4 | ✔ | nessuna in utile: netto massimo -11,207 |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -5,675 (migliore -5,675), 502 trade; annullato dall'ultimo terzo (-643 -> -2,765) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -11,207 |
| YES long | FastYesLong | 152 | 36 | · | batte lo spento: netto 760 contro -11,207, net/DD 0.12 contro -0.92; annullato dall'ultimo terzo (-643 -> -4,354) |
| YES short | FastYesShort | 152 | 152 | · | nessun valore sopra lo spento |
| NO long | FastNoLong | 153 | 70 | · | batte lo spento: netto 229 contro -11,207, net/DD 0.04 contro -0.92; annullato dall'ultimo terzo (-643 -> -5,559) |
| NO short | FastNoShort | 153 | 153 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (699 trade, netto -11,850).

## BO Breakout di N sessioni

703 simulazioni in 21.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=10, IncludeCurrentSession=1 | ✔ | avg smussato 15.2, netto 4,334, 285 trade |
| finestra: inizio | StartHour | -1 | 18 | ✔ | avg smussato 64.0, netto 4,541, 71 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 64.0, netto 4,541, 71 trade |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 6,138 (migliore 6,138), 71 trade; annullato dall'ultimo terzo (829 -> 500) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 64.0, netto 4,541, 71 trade |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 3 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 47 | ✔ | batte lo spento: netto 6,379 contro 4,541, net/DD 3.26 contro 2.27 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 6,322 contro 6,379, net/DD 3.30 contro 3.26 |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 8,691 contro 6,322, net/DD 11.14 contro 3.30 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 8,791 (migliore 9,088), 50 trade |

**Storia di ricerca**: 64 trade, netto 10,104, DD 780, average trade 158, UngerFit 9.23.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 64 su almeno 50 |
| average trade | passa | 158 contro la soglia 38 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 16.8 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,413 |
| ultimo terzo | passa | netto 1,413, net/DD 3.45 (serve 1,3) |
| due terzi su tre | passa | 2,589 / 6,102 / 1,413 |
| outlier | **no** | trade migliore 17% del netto sulla storia, 67% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 101 con i pattern, 38 senza |
| pattern casuali | passa | 0 estrazioni su 4 fanno almeno altrettanto, p 0.20, Wilson 0.05 |
| plateau | passa | media 0.88 su 9 vicini, minimo 0.49 |

**Prova su FTMO**: 39 trade, netto -1,947, DD 1,957, net/DD -1.00, average trade -50 (soglia 38), UngerFit , finestre in utile 1/4 (-1,363 / 864 / -515 / -933).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -1.00 (serve 1), average trade -50 (soglia 38), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 68% del netto (massimo 40%); average trade / soglia per anno: 2020:0.0 2021:4.5 2022:4.5 2023:2.5 2024:-0.5 2025:-0.2 2026:-1.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=18, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=47, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=5, BreakoutOffsetTicks=10`

## BOS Breakout della sessione in corso

643 simulazioni in 24.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=10 | ✔ | avg smussato 1.0, netto 751, 742 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 1.0, netto 751, 742 trade |
| finestra: fine | EndHour | -1 | 9 | · | avg smussato 2.5, netto 1,626, 662 trade; annullato dall'ultimo terzo (-1,334 -> -7,817) |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 594 (migliore 594), 705 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -1,331 |
| YES | PtnNeutYes | 55 | 6 | ✔ | batte lo spento: netto 9,033 contro -1,331, net/DD 1.12 contro -0.10 |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 7,454 contro 9,033, net/DD 1.66 contro 1.12; annullato dall'ultimo terzo (434 -> -1,993) |
| direzionale YES | PtnDirYes | 52 | -9 | · | batte lo spento: netto 12,861 contro 9,033, net/DD 1.83 contro 1.12; annullato dall'ultimo terzo (434 -> -238) |
| direzionale NO | PtnDirNo | 53 | -4 | · | batte lo spento: netto 13,076 contro 9,033, net/DD 1.87 contro 1.12; annullato dall'ultimo terzo (434 -> -238) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 12,725 contro 9,033, net/DD 2.14 contro 1.12; annullato dall'ultimo terzo (434 -> -1,085) |
| uscita a ora fissa | ExitHour | -1 | 19 | · | batte lo spento: netto 13,086 contro 9,033, net/DD 1.98 contro 1.12; annullato dall'ultimo terzo (434 -> -198) |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 9,042 contro 9,033, net/DD 1.12 contro 1.12; annullato dall'ultimo terzo (434 -> 102) |
| affinamento stop | StopAtr | 1.0 | 0.6 | · | D2: netto smussato 12,444 (migliore 12,444), 379 trade; annullato dall'ultimo terzo (434 -> -893) |

**Storia di ricerca**: 489 trade, netto 9,467, DD 8,041, average trade 19, UngerFit 0.35.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 489 su almeno 50 |
| average trade | **no** | 19 contro la soglia 38 |
| anni | passa | 5 anni, minimo 20 trade in un anno, 128.3 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 434 |
| ultimo terzo | **no** | netto 434, net/DD 0.11 (serve 1,3) |
| due terzi su tre | passa | -2,419 / 11,451 / 434 |
| outlier | **no** | trade migliore 18% del netto sulla storia, 282% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 3 con i pattern, -3 senza |
| pattern casuali | passa | 2 estrazioni su 11 fanno almeno altrettanto, p 0.25, Wilson 0.12 |
| plateau | passa | media 1.00 su 4 vicini, minimo 1.00 |

**Prova su FTMO**: 273 trade, netto -1,797, DD 5,127, net/DD -0.35, average trade -7 (soglia 38), UngerFit , finestre in utile 2/4 (-2,228 / -823 / 874 / 444).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.35 (serve 1), average trade -7 (soglia 38), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 122% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.7 2021:-0.3 2022:1.4 2023:0.2 2024:0.0 2025:-0.4 2026:0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=6, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=10`

## VBO Volatility breakout

656 simulazioni in 19.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.5, Direction=2 | ✔ | avg smussato 15.4, netto 1,852, 120 trade |
| finestra: inizio | StartHour | -1 | 2 | · | avg smussato 29.8, netto 3,492, 117 trade; annullato dall'ultimo terzo (-1,721 -> -2,080) |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 20.4, netto 2,371, 116 trade; annullato dall'ultimo terzo (-1,721 -> -1,738) |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 1,211 (migliore 1,211), 120 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 21.8, netto 2,618, 120 trade; annullato dall'ultimo terzo (-1,721 -> -2,789) |
| YES | PtnNeutYes | 55 | 26 | ✔ | batte lo spento: netto 2,628 contro 1,852, net/DD 1.60 contro 0.65 |
| NO | PtnNeutNo | 56 | 48 | · | batte lo spento: netto 4,030 contro 2,628, net/DD 3.31 contro 1.60; annullato dall'ultimo terzo (-1,295 -> -1,782) |
| direzionale YES | PtnDirYes | 52 | 21 | · | batte lo spento: netto 3,056 contro 2,628, net/DD 1.91 contro 1.60; annullato dall'ultimo terzo (-1,295 -> -1,395) |
| direzionale NO | PtnDirNo | 53 | -7 | ✔ | batte lo spento: netto 3,788 contro 2,628, net/DD 2.80 contro 1.60 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 4,913 contro 3,788, net/DD 3.64 contro 2.80 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 4,592 (migliore 4,592), 71 trade; annullato dall'ultimo terzo (-361 -> -528) |

**Storia di ricerca**: 105 trade, netto 4,552, DD 1,749, average trade 43, UngerFit 1.69.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 105 su almeno 50 |
| average trade | passa | 43 contro la soglia 38 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 27.6 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -361 |
| ultimo terzo | **no** | netto -361, net/DD -0.31 (serve 1,3) |
| due terzi su tre | passa | 1,117 / 3,796 / -361 |
| outlier | passa | trade migliore 24% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -11 con i pattern, -31 senza |
| pattern casuali | passa | 1 estrazioni su 12 fanno almeno altrettanto, p 0.15, Wilson 0.06 |
| plateau | **no** | media 0.53 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 50 trade, netto -49, DD 1,573, net/DD -0.03, average trade -1 (soglia 38), UngerFit , finestre in utile 2/4 (-869 / 431 / -336 / 725).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.03 (serve 1), average trade -1 (soglia 38), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 91% del netto (massimo 40%); average trade / soglia per anno: 2020:1.8 2021:0.6 2022:3.0 2023:-1.0 2024:0.6 2025:-0.8 2026:0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=26, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=-7, SkipDay=-1, AtrMultiplierLong=1.5, Direction=2`

## MAC Incrocio di medie

89 simulazioni in 3.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=20, SlowPeriod=100, Direction=0 | ✔ | avg smussato 42.7, netto 2,137, 50 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 1,310 (migliore 1,310), 50 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 42.7, netto 2,137, 50 trade |

**Abbandonato**: R9: la base non guadagna su tutta la storia (77 trade, netto -594).

## RBM Reversal Bollinger mirrored

678 simulazioni in 25.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=20, BbNumDevs=1.5 | ✔ | avg smussato 14.9, netto 8,434, 567 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 14.9, netto 8,434, 567 trade |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 71.9, netto 8,916, 124 trade; annullato dall'ultimo terzo (1,584 -> -2,763) |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 8,751 (migliore 9,095), 567 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 3 | ✔ | batte lo spento: netto 14,546 contro 8,850, net/DD 5.74 contro 1.17 |
| NO | PtnNeutNo | 56 | 1 | · | batte lo spento: netto 16,191 contro 14,546, net/DD 7.24 contro 5.74; annullato dall'ultimo terzo (3,918 -> 2,761) |
| direzionale YES | PtnDirYes | 52 | 44 | · | batte lo spento: netto 14,638 contro 14,546, net/DD 6.81 contro 5.74; annullato dall'ultimo terzo (3,918 -> 2,325) |
| direzionale NO | PtnDirNo | 53 | -21 | · | batte lo spento: netto 14,932 contro 14,546, net/DD 6.71 contro 5.74; annullato dall'ultimo terzo (3,918 -> 2,460) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 13,883 contro 14,546, net/DD 5.97 contro 5.74; annullato dall'ultimo terzo (3,918 -> 2,857) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 47.0, netto 15,425, 328 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 1.25 | 0.8 | · | D2: netto smussato 15,682 (migliore 15,682), 329 trade; annullato dall'ultimo terzo (4,671 -> 4,078) |
| affinamento target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 16,430 contro 15,425, net/DD 6.14 contro 5.76; annullato dall'ultimo terzo (4,671 -> 4,332) |
| durata | MaxBars | 4 | 6 | · | avg smussato 37.0, netto 11,647, 283 trade; annullato dall'ultimo terzo (4,671 -> 952) |

**Storia di ricerca**: 494 trade, netto 19,930, DD 3,049, average trade 40, UngerFit 1.19.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 494 su almeno 50 |
| average trade | passa | 40 contro la soglia 38 |
| anni | passa | 5 anni, minimo 18 trade in un anno, 129.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,505 |
| ultimo terzo | passa | netto 4,671, net/DD 2.03 (serve 1,3) |
| due terzi su tre | passa | 8,561 / 6,715 / 4,671 |
| outlier | passa | trade migliore 8% del netto sulla storia, 20% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 28 con i pattern, 9 senza |
| pattern casuali | passa | 1 estrazioni su 6 fanno almeno altrettanto, p 0.29, Wilson 0.12 |
| plateau | passa | media 0.80 su 7 vicini, minimo 0.34 |

**Prova su FTMO**: 251 trade, netto 937, DD 4,785, net/DD 0.20, average trade 4 (soglia 38), UngerFit 0.09, finestre in utile 2/4 (3,646 / -1,939 / 796 / -1,551).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.20 (serve 1), average trade 4 (soglia 38), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 43% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.4 2021:1.8 2022:0.6 2023:1.0 2024:0.8 2025:0.3 2026:-0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=0, MaxBars=4, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=3, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=20, BbNumDevs=1.5`

## RBU Reversal Bollinger unmirrored

969 simulazioni in 43.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=20, BbNumDevs=1.5 | ✔ | avg smussato 14.9, netto 8,434, 567 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 14.9, netto 8,434, 567 trade |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 71.9, netto 8,916, 124 trade; annullato dall'ultimo terzo (1,584 -> -2,763) |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 8,751 (migliore 9,095), 567 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES long | FastYesLong | 152 | 29 | · | batte lo spento: netto 10,591 contro 8,850, net/DD 1.93 contro 1.17; annullato dall'ultimo terzo (2,071 -> 975) |
| YES short | FastYesShort | 152 | 144 | · | batte lo spento: netto 14,260 contro 8,850, net/DD 2.49 contro 1.17; annullato dall'ultimo terzo (2,071 -> 608) |
| NO long | FastNoLong | 153 | 25 | · | batte lo spento: netto 10,591 contro 8,850, net/DD 1.93 contro 1.17; annullato dall'ultimo terzo (2,071 -> 975) |
| NO short | FastNoShort | 153 | 41 | ✔ | batte lo spento: netto 14,987 contro 8,850, net/DD 3.72 contro 1.17 |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 14,870 contro 14,987, net/DD 3.80 contro 3.72; annullato dall'ultimo terzo (3,053 -> 1,500) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 34.3, netto 14,987, 437 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 16,033 contro 14,987, net/DD 4.00 contro 3.72 |
| affinamento stop | StopAtr | 1.25 | 1.25 | · | D2: netto smussato 15,832 (migliore 16,546), 437 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 8 | · | avg smussato 48.9, netto 21,042, 430 trade; annullato dall'ultimo terzo (3,551 -> 1,851) |

**Storia di ricerca**: 657 trade, netto 19,584, DD 4,991, average trade 30, UngerFit 0.69.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 657 su almeno 50 |
| average trade | **no** | 30 contro la soglia 38 |
| anni | passa | 5 anni, minimo 23 trade in un anno, 172.4 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 3,551 |
| ultimo terzo | **no** | netto 3,551, net/DD 0.71 (serve 1,3) |
| due terzi su tre | passa | 11,822 / 4,211 / 3,551 |
| outlier | passa | trade migliore 10% del netto sulla storia, 23% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 16 con i pattern, 8 senza |
| pattern casuali | **no** | 22 estrazioni su 54 fanno almeno altrettanto, p 0.42, Wilson 0.34 |
| plateau | passa | media 0.74 su 8 vicini, minimo 0.16 |

**Prova su FTMO**: 333 trade, netto 401, DD 5,742, net/DD 0.07, average trade 1 (soglia 38), UngerFit 0.03, finestre in utile 2/4 (426 / -2,208 / -504 / 2,693).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.07 (serve 1), average trade 1 (soglia 38), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 69% del netto (massimo 40%); average trade / soglia per anno: 2020:-2.2 2021:2.2 2022:-0.2 2023:1.2 2024:0.3 2025:-0.3 2026:0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=-1, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=41, SkipDay=-1, BbLength=20, BbNumDevs=1.5`

## RHL Reversal sui livelli di ieri

672 simulazioni in 26.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=2, Direction=0 | ✔ | avg smussato 21.6, netto 14,547, 673 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 21.6, netto 14,547, 673 trade |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 48.4, netto 13,114, 271 trade; annullato dall'ultimo terzo (-1,488 -> -3,547) |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 12,129 (migliore 12,129), 683 trade; annullato dall'ultimo terzo (-1,488 -> -2,113) |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 14,971 contro 14,547, net/DD 2.36 contro 2.30; annullato dall'ultimo terzo (-1,488 -> -1,591) |
| YES | PtnNeutYes | 55 | 29 | · | batte lo spento: netto 12,617 contro 14,547, net/DD 3.49 contro 2.30; annullato dall'ultimo terzo (-1,488 -> -6,032) |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 12,617 contro 14,547, net/DD 3.49 contro 2.30; annullato dall'ultimo terzo (-1,488 -> -6,032) |
| direzionale YES | PtnDirYes | 52 | -4 | · | batte lo spento: netto 12,171 contro 14,547, net/DD 3.16 contro 2.30; annullato dall'ultimo terzo (-1,488 -> -4,395) |
| direzionale NO | PtnDirNo | 53 | -44 | ✔ | batte lo spento: netto 14,653 contro 14,547, net/DD 3.94 contro 2.30 |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 15,004 contro 14,653, net/DD 4.04 contro 3.94; annullato dall'ultimo terzo (75 -> -1,837) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 46.8, netto 15,009, 321 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 14,025 (migliore 14,353), 331 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 2 candidati batte lo spento |
| durata | MaxBars | 4 | 8 | ✔ | avg smussato 57.4, netto 15,544, 287 trade |

**Storia di ricerca**: 432 trade, netto 19,506, DD 4,824, average trade 45, UngerFit 1.06.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 432 su almeno 50 |
| average trade | passa | 45 contro la soglia 38 |
| anni | passa | 5 anni, minimo 14 trade in un anno, 113.4 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 3,961 |
| ultimo terzo | **no** | netto 3,961, net/DD 0.85 (serve 1,3) |
| due terzi su tre | passa | 5,258 / 9,718 / 3,961 |
| outlier | **no** | trade migliore 10% del netto sulla storia, 40% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 27 con i pattern, -6 senza |
| pattern casuali | passa | 1 estrazioni su 10 fanno almeno altrettanto, p 0.18, Wilson 0.08 |
| plateau | passa | media 0.87 su 6 vicini, minimo 0.47 |

**Prova su FTMO**: 218 trade, netto -2,091, DD 5,920, net/DD -0.35, average trade -10 (soglia 38), UngerFit , finestre in utile 3/4 (645 / -4,186 / 227 / 924).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.35 (serve 1), average trade -10 (soglia 38), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 46% del netto (massimo 40%); average trade / soglia per anno: 2020:2.7 2021:1.0 2022:0.5 2023:1.7 2024:0.7 2025:-0.7 2026:0.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=8, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=-44, SkipDay=-1, LevelOffsetTicks=2, Direction=0`

## LF Level fader sul pivot di ieri

496 simulazioni in 12.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -1,266 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -627 (migliore -627), 122 trade |
| target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 1,333 contro -351, net/DD 0.50 contro -0.13 |
| YES | PtnNeutYes | 55 | 49 | ✔ | batte lo spento: netto 3,060 contro 1,333, net/DD 2.55 contro 0.50 |
| NO | PtnNeutNo | 56 | 33 | ✔ | batte lo spento: netto 3,794 contro 3,060, net/DD 6.63 contro 2.55 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 3 | ✔ | batte lo spento: netto 3,830 contro 3,794, net/DD 6.69 contro 6.63 |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 76.6, netto 3,830, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 3,830 (migliore 3,830), 50 trade |
| affinamento target | TargetAtr | 0.5 | 0.5 | · | batte lo spento: netto 3,830 contro 2,541, net/DD 6.69 contro 3.22 |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 76.6, netto 3,830, 50 trade |

**Storia di ricerca**: 67 trade, netto 3,883, DD 572, average trade 58, UngerFit 3.96.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 67 su almeno 50 |
| average trade | passa | 58 contro la soglia 38 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 17.6 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 53 |
| ultimo terzo | **no** | netto 53, net/DD 0.17 (serve 1,3) |
| due terzi su tre | passa | 2,177 / 1,653 / 53 |
| outlier | **no** | trade migliore 11% del netto sulla storia, 456% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 3 con i pattern, -11 senza |
| pattern casuali | passa | 2 estrazioni su 19 fanno almeno altrettanto, p 0.15, Wilson 0.07 |
| plateau | **no** | media 0.56 su 3 vicini, minimo 0.00 |

**Prova su FTMO**: 37 trade, netto 570, DD 556, net/DD 1.03, average trade 15 (soglia 38), UngerFit 1.07, finestre in utile 3/4 (-169 / 323 / 286 / 130).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.03 (serve 1), average trade 15 (soglia 38), finestre 3/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 39% del netto (massimo 40%); average trade / soglia per anno: 2020:1.5 2021:2.6 2022:0.5 2023:2.2 2024:-0.1 2025:0.3 2026:1.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=49, PtnNeutNo=33, PtnDirYes=52, NotEntryDayLong=3, NotEntryDayShort=-1, LevelShift=20`

## LFHL Level fader sugli estremi di ieri

506 simulazioni in 13.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=0 | ✔ | nessuna in utile: netto massimo -238 |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato -876 (migliore -876), 256 trade |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 595 contro -238, net/DD 0.18 contro -0.07 |
| YES | PtnNeutYes | 55 | 21 | ✔ | batte lo spento: netto 3,795 contro 595, net/DD 1.70 contro 0.18 |
| NO | PtnNeutNo | 56 | 33 | · | batte lo spento: netto 5,011 contro 3,795, net/DD 3.44 contro 1.70; annullato dall'ultimo terzo (-847 -> -1,195) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 2 | ✔ | batte lo spento: netto 3,815 contro 3,795, net/DD 1.71 contro 1.70 |
| calendario short | NotEntryDayShort | -1 | 2 | ✔ | batte lo spento: netto 5,211 contro 3,815, net/DD 3.92 contro 1.71 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 66.0, netto 5,211, 79 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 5,143 (migliore 5,143), 79 trade; annullato dall'ultimo terzo (-135 -> -339) |
| affinamento target | TargetAtr | 2.0 | 2.0 | · | batte lo spento: netto 5,211 contro 4,378, net/DD 3.92 contro 3.29 |
| durata | MaxBars | 4 | 8 | ✔ | avg smussato 69.3, netto 5,472, 79 trade |

**Storia di ricerca**: 108 trade, netto 5,389, DD 1,430, average trade 50, UngerFit 2.15.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 108 su almeno 50 |
| average trade | passa | 50 contro la soglia 38 |
| anni | passa | 5 anni, minimo 10 trade in un anno, 28.3 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -83 |
| ultimo terzo | **no** | netto -83, net/DD -0.10 (serve 1,3) |
| due terzi su tre | passa | 5,184 / 288 / -83 |
| outlier | passa | trade migliore 20% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -3 con i pattern, -17 senza |
| pattern casuali | **no** | 11 estrazioni su 34 fanno almeno altrettanto, p 0.34, Wilson 0.25 |
| plateau | passa | media 0.89 su 7 vicini, minimo 0.56 |

**Prova su FTMO**: 48 trade, netto -1,226, DD 2,316, net/DD -0.53, average trade -26 (soglia 38), UngerFit , finestre in utile 1/4 (-416 / -782 / -401 / 373).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.53 (serve 1), average trade -26 (soglia 38), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 67% del netto (massimo 40%); average trade / soglia per anno: 2020:4.6 2021:2.8 2022:0.0 2023:0.6 2024:-0.4 2025:-1.1 2026:0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=8, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=21, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=2, NotEntryDayShort=2, LevelShift=0`

## Riepilogo

- PCH: scartata; ricerca 536 trade netto 19,484 avg 36; prova 307 trade netto 4,617 net/DD 1.71 avg 15 fin 3/4; falliti: average trade, ultimo terzo, outlier, pattern utile, pattern casuali, prova sul broker
- TFM: abbandonato (R9: la base non guadagna su tutta la storia (699 trade, netto -11,850))
- TFU: abbandonato (R9: la base non guadagna su tutta la storia (699 trade, netto -11,850))
- BO: scartata; ricerca 64 trade netto 10,104 avg 158; prova 39 trade netto -1,947 net/DD -1.00 avg -50 fin 1/4; falliti: anni, outlier, prova sul broker, anni
- BOS: scartata; ricerca 489 trade netto 9,467 avg 19; prova 273 trade netto -1,797 net/DD -0.35 avg -7 fin 2/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, anni
- VBO: scartata; ricerca 105 trade netto 4,552 avg 43; prova 50 trade netto -49 net/DD -0.03 avg -1 fin 2/4; falliti: anni, utile recente, ultimo terzo, plateau, prova sul broker, anni
- MAC: abbandonato (R9: la base non guadagna su tutta la storia (77 trade, netto -594))
- RBM: scartata; ricerca 494 trade netto 19,930 avg 40; prova 251 trade netto 937 net/DD 0.20 avg 4 fin 2/4; falliti: prova sul broker, anni
- RBU: scartata; ricerca 657 trade netto 19,584 avg 30; prova 333 trade netto 401 net/DD 0.07 avg 1 fin 2/4; falliti: average trade, ultimo terzo, pattern casuali, prova sul broker, anni
- RHL: scartata; ricerca 432 trade netto 19,506 avg 45; prova 218 trade netto -2,091 net/DD -0.35 avg -10 fin 3/4; falliti: ultimo terzo, outlier, prova sul broker, anni
- LF: scartata; ricerca 67 trade netto 3,883 avg 58; prova 37 trade netto 570 net/DD 1.03 avg 15 fin 3/4; falliti: anni, ultimo terzo, outlier, plateau, prova sul broker
- LFHL: scartata; ricerca 108 trade netto 5,389 avg 50; prova 48 trade netto -1,226 net/DD -0.53 avg -26 fin 1/4; falliti: utile recente, ultimo terzo, pattern casuali, prova sul broker, anni
