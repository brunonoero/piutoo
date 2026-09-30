# Percorso v4 — @YM 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (22,540 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,258 barre), mai vista dal percorso
- costi FTMO: spread 2.10 punti (mediana), swap long 11.7286 short 0 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 61 nella ricerca, 77 nella prova

## PCH Price Channel

559 simulazioni in 30.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=50, OffsetTicks=5, Direction=1 | ✔ | avg smussato 26.1, netto 7,274, 279 trade |
| finestra: inizio | StartHour | -1 | 14 | · | avg smussato 80.5, netto 17,935, 215 trade; annullato dall'ultimo terzo (6,332 -> 880) |
| finestra: fine | EndHour | -1 | 6 | · | avg smussato 158.3, netto 13,912, 79 trade; annullato dall'ultimo terzo (6,332 -> 5,147) |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 13,928 (migliore 13,928), 279 trade; annullato dall'ultimo terzo (6,332 -> 6,282) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 26.1, netto 7,274, 279 trade |
| YES | PtnNeutYes | 55 | 11 | · | batte lo spento: netto 25,386 contro 7,274, net/DD 6.05 contro 0.64; annullato dall'ultimo terzo (6,332 -> 1,006) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 25,386 contro 7,274, net/DD 6.05 contro 0.64; annullato dall'ultimo terzo (6,332 -> 1,006) |
| direzionale YES | PtnDirYes | 52 | 51 | · | batte lo spento: netto 16,094 contro 7,274, net/DD 5.35 contro 0.64; annullato dall'ultimo terzo (6,332 -> 541) |
| direzionale NO | PtnDirNo | 53 | -47 | · | batte lo spento: netto 16,094 contro 7,274, net/DD 5.35 contro 0.64; annullato dall'ultimo terzo (6,332 -> 541) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 16,734 contro 7,274, net/DD 2.07 contro 0.64; annullato dall'ultimo terzo (6,332 -> -2,673) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 10,779 contro 7,274, net/DD 1.10 contro 0.64; annullato dall'ultimo terzo (6,332 -> 3,181) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 15,335 (migliore 15,335), 279 trade; annullato dall'ultimo terzo (6,332 -> 6,282) |

**Storia di ricerca**: 439 trade, netto 13,607, DD 14,774, average trade 31, UngerFit 0.33.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 439 su almeno 50 |
| average trade | **no** | 31 contro la soglia 61 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 115.2 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 6,332 |
| ultimo terzo | **no** | netto 6,332, net/DD 0.55 (serve 1,3) |
| due terzi su tre | passa | 7,292 / -18 / 6,332 |
| outlier | **no** | trade migliore 24% del netto sulla storia, 46% sull'ultimo terzo |
| plateau | passa | media 0.68 su 6 vicini, minimo 0.45 |

**Prova su FTMO**: 209 trade, netto 10,784, DD 17,845, net/DD 0.60, average trade 52 (soglia 77), UngerFit 0.44, finestre in utile 3/4 (7,870 / -2,671 / 2,008 / 3,577).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.60 (serve 1), average trade 52 (soglia 77), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 50% del netto (massimo 40%); average trade / soglia per anno: 2020:-9.4 2021:1.9 2022:0.7 2023:0.1 2024:1.5 2025:0.0 2026:0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=50, OffsetTicks=5, Direction=1`

## TFM Trend following mirrored

435 simulazioni in 20.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 19 | ✔ | nessuna in utile: netto massimo -3,447 |
| finestra: fine | EndHour | -1 | 21 | · | avg smussato 30.8, netto 13,784, 435 trade; annullato dall'ultimo terzo (15,220 -> 6,197) |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 1,696 (migliore 1,696), 445 trade; annullato dall'ultimo terzo (15,220 -> 9,930) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -3,340 |
| YES | PtnNeutYes | 55 | 23 | · | batte lo spento: netto 12,106 contro -3,340, net/DD 6.67 contro -0.31; annullato dall'ultimo terzo (15,348 -> 1,937) |
| NO | PtnNeutNo | 56 | 28 | · | batte lo spento: netto 15,492 contro -3,340, net/DD 3.16 contro -0.31; annullato dall'ultimo terzo (15,348 -> 4,102) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 11,156 contro -3,340, net/DD 3.95 contro -0.31; annullato dall'ultimo terzo (15,348 -> 10,104) |
| direzionale NO | PtnDirNo | 53 | 33 | · | batte lo spento: netto 14,557 contro -3,340, net/DD 1.63 contro -0.31; annullato dall'ultimo terzo (15,348 -> 9,098) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 13,693 contro -3,340, net/DD 1.70 contro -0.31; annullato dall'ultimo terzo (15,348 -> 11,985) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 2,581 (migliore 2,581), 445 trade; annullato dall'ultimo terzo (15,348 -> 10,307) |

**Storia di ricerca**: 673 trade, netto 12,008, DD 10,779, average trade 18, UngerFit 0.22.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 673 su almeno 50 |
| average trade | **no** | 18 contro la soglia 61 |
| anni | **no** | 5 anni, minimo 7 trade in un anno, 176.6 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 15,348 |
| ultimo terzo | passa | netto 15,348, net/DD 2.53 (serve 1,3) |
| due terzi su tre | passa | -3,693 / 353 / 15,348 |
| outlier | passa | trade migliore 27% del netto sulla storia, 18% sull'ultimo terzo |
| plateau | passa | media 1.00 su 2 vicini, minimo 1.00 |

**Prova su FTMO**: 331 trade, netto 22,270, DD 8,910, net/DD 2.50, average trade 67 (soglia 77), UngerFit 0.81, finestre in utile 3/4 (4,421 / 6,246 / -5,317 / 16,919).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 2.50 (serve 1), average trade 67 (soglia 77), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 45% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.3 2021:-0.3 2022:0.0 2023:1.3 2024:1.2 2025:0.0 2026:1.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=19, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

729 simulazioni in 30.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 19 | ✔ | nessuna in utile: netto massimo -3,447 |
| finestra: fine | EndHour | -1 | 21 | · | avg smussato 30.8, netto 13,784, 435 trade; annullato dall'ultimo terzo (15,220 -> 6,197) |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 1,696 (migliore 1,696), 445 trade; annullato dall'ultimo terzo (15,220 -> 9,930) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -3,340 |
| YES long | FastYesLong | 152 | 67 | · | batte lo spento: netto 2,515 contro -3,340, net/DD 0.33 contro -0.31; annullato dall'ultimo terzo (15,348 -> 10,996) |
| YES short | FastYesShort | 152 | 37 | · | batte lo spento: netto 22,737 contro -3,340, net/DD 3.37 contro -0.31; annullato dall'ultimo terzo (15,348 -> 11,798) |
| NO long | FastNoLong | 153 | 37 | · | batte lo spento: netto 5,828 contro -3,340, net/DD 0.77 contro -0.31; annullato dall'ultimo terzo (15,348 -> 11,095) |
| NO short | FastNoShort | 153 | 16 | · | batte lo spento: netto 20,028 contro -3,340, net/DD 4.40 contro -0.31; annullato dall'ultimo terzo (15,348 -> 8,360) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 13,693 contro -3,340, net/DD 1.70 contro -0.31; annullato dall'ultimo terzo (15,348 -> 11,985) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 2,581 (migliore 2,581), 445 trade; annullato dall'ultimo terzo (15,348 -> 10,307) |

**Storia di ricerca**: 673 trade, netto 12,008, DD 10,779, average trade 18, UngerFit 0.22.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 673 su almeno 50 |
| average trade | **no** | 18 contro la soglia 61 |
| anni | **no** | 5 anni, minimo 7 trade in un anno, 176.6 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 15,348 |
| ultimo terzo | passa | netto 15,348, net/DD 2.53 (serve 1,3) |
| due terzi su tre | passa | -3,693 / 353 / 15,348 |
| outlier | passa | trade migliore 27% del netto sulla storia, 18% sull'ultimo terzo |
| plateau | passa | media 1.00 su 2 vicini, minimo 1.00 |

**Prova su FTMO**: 331 trade, netto 22,270, DD 8,910, net/DD 2.50, average trade 67 (soglia 77), UngerFit 0.81, finestre in utile 3/4 (4,421 / 6,246 / -5,317 / 16,919).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 2.50 (serve 1), average trade 67 (soglia 77), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 45% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.3 2021:-0.3 2022:0.0 2023:1.3 2024:1.2 2025:0.0 2026:1.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=19, EndHour=-1, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1`

## BO Breakout di N sessioni

466 simulazioni in 41.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=0, IncludeCurrentSession=0 | ✔ | nessuna in utile: netto massimo -30,607 |
| finestra: inizio | StartHour | -1 | 20 | · | nessuna in utile: netto massimo -336; annullato dall'ultimo terzo (15,629 -> 2,062) |
| finestra: fine | EndHour | -1 | 2 | · | nessuna in utile: netto massimo -4,527; annullato dall'ultimo terzo (15,629 -> 5,944) |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato -21,945 (migliore -21,945), 357 trade; annullato dall'ultimo terzo (15,629 -> 12,330) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -29,976 |
| YES | PtnNeutYes | 55 | 24 | · | batte lo spento: netto 9,345 contro -29,976, net/DD 1.09 contro -0.93; annullato dall'ultimo terzo (15,629 -> 10,532) |
| NO | PtnNeutNo | 56 | 28 | · | batte lo spento: netto 9,345 contro -29,976, net/DD 1.09 contro -0.93; annullato dall'ultimo terzo (15,629 -> 10,532) |
| direzionale YES | PtnDirYes | 52 | 34 | · | batte lo spento: netto 13,601 contro -29,976, net/DD 2.47 contro -0.93; annullato dall'ultimo terzo (15,629 -> 4,079) |
| direzionale NO | PtnDirNo | 53 | 33 | · | batte lo spento: netto 13,601 contro -29,976, net/DD 2.47 contro -0.93; annullato dall'ultimo terzo (15,629 -> 4,079) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (543 trade, netto -14,347).

## BOS Breakout della sessione in corso

643 simulazioni in 34.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=2 | ✔ | avg smussato 0.7, netto 524, 732 trade |
| finestra: inizio | StartHour | -1 | 11 | · | avg smussato 23.6, netto 23,420, 681 trade; annullato dall'ultimo terzo (-15,206 -> -25,470) |
| finestra: fine | EndHour | -1 | 6 | · | avg smussato 7.2, netto 4,778, 634 trade; annullato dall'ultimo terzo (-15,206 -> -22,403) |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato 851 (migliore 851), 642 trade; annullato dall'ultimo terzo (-15,206 -> -27,456) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 2.1, netto 1,554, 732 trade |
| YES | PtnNeutYes | 55 | 23 | ✔ | batte lo spento: netto 29,605 contro 1,554, net/DD 2.45 contro 0.03 |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 28,204 contro 29,605, net/DD 4.96 contro 2.45; annullato dall'ultimo terzo (394 -> -3,120) |
| direzionale YES | PtnDirYes | 52 | -48 | · | batte lo spento: netto 31,695 contro 29,605, net/DD 2.75 contro 2.45; annullato dall'ultimo terzo (394 -> 146) |
| direzionale NO | PtnDirNo | 53 | -45 | ✔ | batte lo spento: netto 47,714 contro 29,605, net/DD 8.46 contro 2.45 |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 43,806 contro 47,714, net/DD 11.77 contro 8.46; annullato dall'ultimo terzo (8,318 -> 7,533) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 47,775 (migliore 47,805), 89 trade |

**Storia di ricerca**: 115 trade, netto 54,070, DD 6,936, average trade 470, UngerFit 7.23.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 115 su almeno 50 |
| average trade | passa | 470 contro la soglia 61 |
| anni | passa | 5 anni, minimo 5 trade in un anno, 30.2 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 9,466 |
| ultimo terzo | passa | netto 9,466, net/DD 8.83 (serve 1,3) |
| due terzi su tre | passa | 22,142 / 22,462 / 9,466 |
| outlier | passa | trade migliore 9% del netto sulla storia, 22% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 364 con i pattern, -53 senza |
| pattern casuali | passa | 1 estrazioni su 65 fanno almeno altrettanto, p 0.03, Wilson 0.01 |
| plateau | passa | media 0.92 su 4 vicini, minimo 0.84 |

**Prova su FTMO**: 58 trade, netto 31,199, DD 5,566, net/DD 5.61, average trade 538 (soglia 77), UngerFit 8.21, finestre in utile 4/4 (13,765 / 4,019 / 5,783 / 7,632).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 5.61 (serve 1), average trade 538 (soglia 77), finestre 4/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 26% del netto (massimo 40%); average trade / soglia per anno: 2020:-7.1 2021:13.1 2022:8.5 2023:3.5 2024:15.2 2025:1.6 2026:7.3 |
| altri mercati | **no** | 1/7 con net/DD ≥ 1 (serve meta'): @FDAX 0.72, @NQ 0.77, @ES 3.05, @FESX -0.05, @FCE 0.27, @Z -0.72, @NIY 0.11 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=23, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=-45, SkipDay=-1, BreakoutOffsetTicks=2`

## VBO Volatility breakout

656 simulazioni in 27.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.0, Direction=1 | ✔ | avg smussato 12.9, netto 992, 77 trade |
| finestra: inizio | StartHour | -1 | 17 | · | avg smussato 176.7, netto 11,159, 63 trade; annullato dall'ultimo terzo (7,476 -> 6,245) |
| finestra: fine | EndHour | -1 | 22 | ✔ | avg smussato 14.6, netto 1,796, 76 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 7,490 (migliore 7,490), 76 trade; annullato dall'ultimo terzo (7,476 -> 6,901) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 23.6, netto 1,796, 76 trade |
| YES | PtnNeutYes | 55 | 26 | · | batte lo spento: netto 5,829 contro 1,796, net/DD 2.05 contro 0.47; annullato dall'ultimo terzo (7,476 -> 4,399) |
| NO | PtnNeutNo | 56 | 9 | ✔ | batte lo spento: netto 5,945 contro 1,796, net/DD 1.20 contro 0.47 |
| direzionale YES | PtnDirYes | 52 | 5 | · | batte lo spento: netto 9,617 contro 5,945, net/DD 2.76 contro 1.20; annullato dall'ultimo terzo (8,715 -> 3,418) |
| direzionale NO | PtnDirNo | 53 | 10 | · | batte lo spento: netto 10,854 contro 5,945, net/DD 2.87 contro 1.20; annullato dall'ultimo terzo (8,715 -> 5,053) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 8,014 contro 5,945, net/DD 1.52 contro 1.20; annullato dall'ultimo terzo (8,715 -> 5,197) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 5,556 contro 5,945, net/DD 1.88 contro 1.20; annullato dall'ultimo terzo (8,715 -> 6,771) |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 6,743 (migliore 6,743), 67 trade |

**Storia di ricerca**: 121 trade, netto 15,925, DD 6,191, average trade 132, UngerFit 2.14.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 121 su almeno 50 |
| average trade | passa | 132 contro la soglia 61 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 31.7 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 9,010 |
| ultimo terzo | passa | netto 9,010, net/DD 2.57 (serve 1,3) |
| due terzi su tre | passa | -377 / 7,291 / 9,010 |
| outlier | passa | trade migliore 15% del netto sulla storia, 26% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 167 con i pattern, 146 senza |
| pattern casuali | **no** | 1 estrazioni su 2 fanno almeno altrettanto, p 0.67, Wilson 0.27 |
| plateau | passa | media 0.75 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 80 trade, netto 17,657, DD 6,498, net/DD 2.72, average trade 221 (soglia 77), UngerFit 3.12, finestre in utile 4/4 (6,170 / 3,863 / 2,684 / 4,941).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 2.72 (serve 1), average trade 221 (soglia 77), finestre 4/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 29% del netto (massimo 40%); average trade / soglia per anno: 2020:5.8 2021:-1.5 2022:4.1 2023:1.6 2024:4.9 2025:2.1 2026:2.8 |
| altri mercati | passa | 4/7 con net/DD ≥ 1 (serve meta'): @FDAX 1.08, @NQ 5.85, @ES 1.26, @FESX 0.62, @FCE -0.74, @Z -0.86, @NIY 4.68 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=22, PtnNeutYes=55, PtnNeutNo=9, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=1.0, Direction=1`

## MAC Incrocio di medie

127 simulazioni in 3.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=30, SlowPeriod=50, Direction=2 | ✔ | avg smussato 86.5, netto 13,234, 153 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 11,072 (migliore 11,072), 153 trade; annullato dall'ultimo terzo (-7,279 -> -10,579) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 86.5, netto 13,234, 153 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 16,737 contro 13,234, net/DD 1.78 contro 1.41; annullato dall'ultimo terzo (-7,279 -> -7,299) |
| affinamento stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 11,072 (migliore 11,072), 153 trade; annullato dall'ultimo terzo (-7,279 -> -10,579) |

**Storia di ricerca**: 228 trade, netto 5,954, DD 17,976, average trade 26, UngerFit 0.25.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 228 su almeno 50 |
| average trade | **no** | 26 contro la soglia 61 |
| anni | **no** | 5 anni, minimo 7 trade in un anno, 59.8 all'anno, 2 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -7,279 |
| ultimo terzo | **no** | netto -7,279, net/DD -0.72 (serve 1,3) |
| due terzi su tre | passa | 2,185 / 11,048 / -7,279 |
| outlier | **no** | trade migliore 90% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.04 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 127 trade, netto -25,183, DD 29,891, net/DD -0.84, average trade -198 (soglia 77), UngerFit , finestre in utile 0/4 (-3,254 / -6,137 / -8,327 / -7,466).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.84 (serve 1), average trade -198 (soglia 77), finestre 0/4 (servono 3) |
| anni | **no** | 2/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:-6.4 2021:0.3 2022:5.0 2023:-3.7 2024:-2.2 2025:-2.7 2026:-2.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=30, SlowPeriod=50, Direction=2`

## RBM Reversal Bollinger mirrored

676 simulazioni in 25.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=2.5 | ✔ | avg smussato 38.2, netto 14,599, 382 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 104.0, netto 14,599, 382 trade |
| finestra: fine | EndHour | -1 | 18 | · | avg smussato 47.5, netto 20,155, 330 trade; annullato dall'ultimo terzo (2,904 -> 2,418) |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 24,491 (migliore 24,491), 382 trade; annullato dall'ultimo terzo (2,904 -> -393) |
| target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 17,184 contro 14,599, net/DD 1.60 contro 1.13; annullato dall'ultimo terzo (2,904 -> -714) |
| YES | PtnNeutYes | 55 | 29 | · | batte lo spento: netto 20,280 contro 14,599, net/DD 6.20 contro 1.13; annullato dall'ultimo terzo (2,904 -> -6,537) |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 20,280 contro 14,599, net/DD 6.20 contro 1.13; annullato dall'ultimo terzo (2,904 -> -6,537) |
| direzionale YES | PtnDirYes | 52 | 25 | · | batte lo spento: netto 20,991 contro 14,599, net/DD 7.45 contro 1.13; annullato dall'ultimo terzo (2,904 -> 29) |
| direzionale NO | PtnDirNo | 53 | 46 | ✔ | batte lo spento: netto 25,458 contro 14,599, net/DD 3.27 contro 1.13 |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 29,202 contro 25,458, net/DD 5.87 contro 3.27; annullato dall'ultimo terzo (5,275 -> 4,407) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 102.2, netto 25,458, 249 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 25,933 (migliore 26,781), 249 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 0 | · | avg smussato 208.2, netto 49,763, 239 trade; annullato dall'ultimo terzo (5,275 -> 735) |

**Storia di ricerca**: 393 trade, netto 30,732, DD 7,778, average trade 78, UngerFit 1.14.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 393 su almeno 50 |
| average trade | passa | 78 contro la soglia 61 |
| anni | passa | 5 anni, minimo 6 trade in un anno, 103.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 5,275 |
| ultimo terzo | **no** | netto 5,275, net/DD 0.75 (serve 1,3) |
| due terzi su tre | passa | -7,392 / 32,850 / 5,275 |
| outlier | **no** | trade migliore 14% del netto sulla storia, 51% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 37 con i pattern, 14 senza |
| pattern casuali | **no** | 2 estrazioni su 3 fanno almeno altrettanto, p 0.75, Wilson 0.39 |
| plateau | passa | media 0.82 su 7 vicini, minimo 0.24 |

**Prova su FTMO**: 206 trade, netto 2,666, DD 18,862, net/DD 0.14, average trade 13 (soglia 77), UngerFit 0.11, finestre in utile 3/4 (1,371 / -9,414 / 288 / 10,421).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.14 (serve 1), average trade 13 (soglia 77), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 65% del netto (massimo 40%); average trade / soglia per anno: 2020:-4.3 2021:-0.8 2022:2.7 2023:2.6 2024:0.7 2025:-2.0 2026:2.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=46, SkipDay=-1, BbLength=50, BbNumDevs=2.5`

## RBU Reversal Bollinger unmirrored

775 simulazioni in 37.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=2.5 | ✔ | avg smussato 38.2, netto 14,599, 382 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 104.0, netto 14,599, 382 trade |
| finestra: fine | EndHour | -1 | 18 | · | avg smussato 47.5, netto 20,155, 330 trade; annullato dall'ultimo terzo (2,904 -> 2,418) |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 24,491 (migliore 24,491), 382 trade; annullato dall'ultimo terzo (2,904 -> -393) |
| target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 17,184 contro 14,599, net/DD 1.60 contro 1.13; annullato dall'ultimo terzo (2,904 -> -714) |
| YES long | FastYesLong | 152 | 139 | · | batte lo spento: netto 25,463 contro 14,599, net/DD 3.21 contro 1.13; annullato dall'ultimo terzo (2,904 -> 2,563) |
| YES short | FastYesShort | 152 | 29 | · | batte lo spento: netto 30,105 contro 14,599, net/DD 3.97 contro 1.13; annullato dall'ultimo terzo (2,904 -> -7,702) |
| NO long | FastNoLong | 153 | 143 | · | batte lo spento: netto 22,623 contro 14,599, net/DD 2.52 contro 1.13; annullato dall'ultimo terzo (2,904 -> 1,445) |
| NO short | FastNoShort | 153 | 25 | · | batte lo spento: netto 30,105 contro 14,599, net/DD 3.97 contro 1.13; annullato dall'ultimo terzo (2,904 -> -7,702) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 17,910 contro 14,599, net/DD 2.46 contro 1.13; annullato dall'ultimo terzo (2,904 -> 1,526) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 38.2, netto 14,599, 382 trade |
| uscita a ora fissa | ExitHour | -1 | 21 | ✔ | batte lo spento: netto 22,753 contro 14,599, net/DD 2.57 contro 1.13 |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 25,039 (migliore 25,865), 355 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 0 | · | avg smussato 180.4, netto 61,335, 340 trade; annullato dall'ultimo terzo (6,294 -> 2,023) |

**Storia di ricerca**: 546 trade, netto 31,548, DD 12,745, average trade 58, UngerFit 0.66.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 546 su almeno 50 |
| average trade | **no** | 58 contro la soglia 61 |
| anni | passa | 5 anni, minimo 8 trade in un anno, 143.3 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 6,294 |
| ultimo terzo | **no** | netto 6,294, net/DD 0.49 (serve 1,3) |
| due terzi su tre | passa | -3,465 / 28,719 / 6,294 |
| outlier | **no** | trade migliore 14% del netto sulla storia, 42% sull'ultimo terzo |
| plateau | passa | media 0.78 su 9 vicini, minimo 0.37 |

**Prova su FTMO**: 294 trade, netto -106, DD 11,902, net/DD -0.01, average trade 0 (soglia 77), UngerFit , finestre in utile 2/4 (-7,452 / -1,397 / 8,169 / 574).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.01 (serve 1), average trade 0 (soglia 77), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2020:0.7 2021:-0.3 2022:2.0 2023:1.7 2024:-0.7 2025:-0.4 2026:1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=21, StartHour=0, EndHour=-1, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1, BbLength=50, BbNumDevs=2.5`

## RHL Reversal sui livelli di ieri

474 simulazioni in 16.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=2 | ✔ | avg smussato 33.5, netto 9,936, 297 trade |
| finestra: inizio | StartHour | -1 | 6 | ✔ | avg smussato 36.3, netto 11,338, 292 trade |
| finestra: fine | EndHour | -1 | 11 | · | avg smussato 73.6, netto 12,583, 151 trade; annullato dall'ultimo terzo (15,773 -> 6,945) |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 13,022 (migliore 13,022), 292 trade; annullato dall'ultimo terzo (15,773 -> 14,188) |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 12,532 contro 11,338, net/DD 1.23 contro 1.11; annullato dall'ultimo terzo (15,773 -> 14,728) |
| YES | PtnNeutYes | 55 | 49 | · | batte lo spento: netto 13,206 contro 11,338, net/DD 11.32 contro 1.11; annullato dall'ultimo terzo (15,773 -> 4,400) |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 18,155 contro 11,338, net/DD 4.21 contro 1.11; annullato dall'ultimo terzo (15,773 -> 10,512) |
| direzionale YES | PtnDirYes | 52 | -13 | · | batte lo spento: netto 12,553 contro 11,338, net/DD 3.23 contro 1.11; annullato dall'ultimo terzo (15,773 -> 6,148) |
| direzionale NO | PtnDirNo | 53 | 13 | · | batte lo spento: netto 12,553 contro 11,338, net/DD 3.23 contro 1.11; annullato dall'ultimo terzo (15,773 -> 4,871) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 13,694 contro 11,338, net/DD 1.51 contro 1.11 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 54.1, netto 13,694, 253 trade |
| uscita a ora fissa | ExitHour | -1 | 20 | · | batte lo spento: netto 16,404 contro 13,694, net/DD 2.91 contro 1.51; annullato dall'ultimo terzo (18,722 -> 14,834) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 14,775 (migliore 14,775), 253 trade; annullato dall'ultimo terzo (18,722 -> 17,341) |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 14,888 contro 13,694, net/DD 1.64 contro 1.51; annullato dall'ultimo terzo (18,722 -> 17,677) |
| durata | MaxBars | 4 | 8 | · | avg smussato 69.6, netto 15,976, 253 trade; annullato dall'ultimo terzo (18,722 -> 6,595) |

**Storia di ricerca**: 398 trade, netto 32,416, DD 9,083, average trade 81, UngerFit 1.09.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 398 su almeno 50 |
| average trade | passa | 81 contro la soglia 61 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 104.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 18,722 |
| ultimo terzo | passa | netto 18,722, net/DD 3.33 (serve 1,3) |
| due terzi su tre | passa | 9,647 / 4,048 / 18,722 |
| outlier | passa | trade migliore 13% del netto sulla storia, 17% sull'ultimo terzo |
| plateau | passa | media 0.92 su 5 vicini, minimo 0.73 |

**Prova su FTMO**: 191 trade, netto -10,654, DD 24,103, net/DD -0.44, average trade -56 (soglia 77), UngerFit , finestre in utile 1/4 (-6,697 / -2,619 / -7,421 / 6,083).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.44 (serve 1), average trade -56 (soglia 77), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 61% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.2 2021:1.2 2022:0.7 2023:0.4 2024:2.3 2025:-2.0 2026:1.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=6, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0, LevelOffsetTicks=20, Direction=2`

## LF Level fader sul pivot di ieri

499 simulazioni in 11.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=0 | ✔ | avg smussato 44.4, netto 13,321, 300 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 19,782 (migliore 20,230), 309 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 2 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 1 | ✔ | batte lo spento: netto 15,580 contro 20,442, net/DD 7.26 contro 1.99 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 1 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 4 | · | batte lo spento: netto 15,953 contro 15,580, net/DD 7.43 contro 7.26; annullato dall'ultimo terzo (2,260 -> 1,673) |
| calendario short | NotEntryDayShort | -1 | 2 | · | batte lo spento: netto 15,803 contro 15,580, net/DD 7.36 contro 7.26; annullato dall'ultimo terzo (2,260 -> 510) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 283.3, netto 15,580, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 15,593 (migliore 15,593), 55 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 8 | ✔ | avg smussato 394.3, netto 18,468, 52 trade |

**Storia di ricerca**: 67 trade, netto 23,960, DD 2,411, average trade 358, UngerFit 9.33.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 67 su almeno 50 |
| average trade | passa | 358 contro la soglia 61 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 17.6 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 5,491 |
| ultimo terzo | passa | netto 5,491, net/DD 2.62 (serve 1,3) |
| due terzi su tre | passa | 5,917 / 12,551 / 5,491 |
| outlier | **no** | trade migliore 20% del netto sulla storia, 61% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 366 con i pattern, -12 senza |
| pattern casuali | passa | 1 estrazioni su 41 fanno almeno altrettanto, p 0.05, Wilson 0.02 |
| plateau | passa | media 0.98 su 4 vicini, minimo 0.94 |

**Prova su FTMO**: 28 trade, netto -2,101, DD 4,746, net/DD -0.44, average trade -75 (soglia 77), UngerFit , finestre in utile 2/4 (506 / -4,201 / 2,235 / -640).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.44 (serve 1), average trade -75 (soglia 77), finestre 2/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 53% del netto (massimo 40%); average trade / soglia per anno: 2020:22.1 2021:1.9 2022:8.5 2023:5.9 2024:6.8 2025:-3.9 2026:1.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=8, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=1, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=0`

## LFHL Level fader sugli estremi di ieri

505 simulazioni in 11.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | avg smussato 31.2, netto 14,038, 450 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 14,885 (migliore 14,885), 450 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 53 | ✔ | batte lo spento: netto 17,889 contro 14,038, net/DD 1.41 contro 1.06 |
| NO | PtnNeutNo | 56 | 18 | · | batte lo spento: netto 15,345 contro 17,889, net/DD 3.47 contro 1.41; annullato dall'ultimo terzo (-4,949 -> -7,403) |
| direzionale YES | PtnDirYes | 52 | -33 | ✔ | batte lo spento: netto 20,655 contro 17,889, net/DD 2.80 contro 1.41 |
| calendario long | NotEntryDayLong | -1 | 0 | · | batte lo spento: netto 20,198 contro 20,655, net/DD 3.67 contro 2.80; annullato dall'ultimo terzo (-623 -> -1,201) |
| calendario short | NotEntryDayShort | -1 | 4 | ✔ | batte lo spento: netto 23,135 contro 20,655, net/DD 3.76 contro 2.80 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 214.2, netto 23,135, 108 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 23,604 (migliore 24,703), 108 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 422.6, netto 36,763, 87 trade |

**Storia di ricerca**: 138 trade, netto 40,806, DD 6,935, average trade 296, UngerFit 4.55.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 138 su almeno 50 |
| average trade | passa | 296 contro la soglia 61 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 36.2 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,043 |
| ultimo terzo | **no** | netto 4,043, net/DD 0.58 (serve 1,3) |
| due terzi su tre | passa | 11,734 / 25,029 / 4,043 |
| outlier | **no** | trade migliore 17% del netto sulla storia, 96% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 79 con i pattern, -56 senza |
| pattern casuali | passa | 2 estrazioni su 29 fanno almeno altrettanto, p 0.10, Wilson 0.05 |
| plateau | passa | media 0.93 su 4 vicini, minimo 0.74 |

**Prova su FTMO**: 61 trade, netto 38,905, DD 4,003, net/DD 9.72, average trade 638 (soglia 77), UngerFit 11.48, finestre in utile 4/4 (2,740 / 22,800 / 4,043 / 9,322).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 9.72 (serve 1), average trade 638 (soglia 77), finestre 4/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 29% del netto (massimo 40%); average trade / soglia per anno: 2020:18.2 2021:7.6 2022:6.6 2023:-0.3 2024:4.6 2025:11.6 2026:7.1 |
| altri mercati | **no** | 1/7 con net/DD ≥ 1 (serve meta'): @FDAX 0.79, @NQ 0.14, @ES 2.86, @FESX -0.20, @FCE -0.27, @Z 0.80, @NIY 0.74 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=53, PtnNeutNo=56, PtnDirYes=-33, NotEntryDayLong=-1, NotEntryDayShort=4, LevelShift=10`

## Riepilogo

- PCH: scartata; ricerca 439 trade netto 13,607 avg 31; prova 209 trade netto 10,784 net/DD 0.60 avg 52 fin 3/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, anni
- TFM: scartata; ricerca 673 trade netto 12,008 avg 18; prova 331 trade netto 22,270 net/DD 2.50 avg 67 fin 3/4; falliti: average trade, anni, prova sul broker, anni
- TFU: scartata; ricerca 673 trade netto 12,008 avg 18; prova 331 trade netto 22,270 net/DD 2.50 avg 67 fin 3/4; falliti: average trade, anni, prova sul broker, anni
- BO: abbandonato (R9: la base non guadagna su tutta la storia (543 trade, netto -14,347))
- BOS: scartata; ricerca 115 trade netto 54,070 avg 470; prova 58 trade netto 31,199 net/DD 5.61 avg 538 fin 4/4; falliti: altri mercati
- VBO: scartata; ricerca 121 trade netto 15,925 avg 132; prova 80 trade netto 17,657 net/DD 2.72 avg 221 fin 4/4; falliti: anni, pattern casuali
- MAC: scartata; ricerca 228 trade netto 5,954 avg 26; prova 127 trade netto -25,183 net/DD -0.84 avg -198 fin 0/4; falliti: average trade, anni, utile recente, ultimo terzo, outlier, plateau, prova sul broker, anni
- RBM: scartata; ricerca 393 trade netto 30,732 avg 78; prova 206 trade netto 2,666 net/DD 0.14 avg 13 fin 3/4; falliti: ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- RBU: scartata; ricerca 546 trade netto 31,548 avg 58; prova 294 trade netto -106 net/DD -0.01 avg 0 fin 2/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, anni
- RHL: scartata; ricerca 398 trade netto 32,416 avg 81; prova 191 trade netto -10,654 net/DD -0.44 avg -56 fin 1/4; falliti: prova sul broker, anni
- LF: scartata; ricerca 67 trade netto 23,960 avg 358; prova 28 trade netto -2,101 net/DD -0.44 avg -75 fin 2/4; falliti: anni, outlier, prova sul broker, anni
- LFHL: scartata; ricerca 138 trade netto 40,806 avg 296; prova 61 trade netto 38,905 net/DD 9.72 avg 638 fin 4/4; falliti: anni, ultimo terzo, outlier, altri mercati
