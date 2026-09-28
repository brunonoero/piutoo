# Percorso v4 — @BP 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (24,236 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,913 barre), mai vista dal percorso
- costi FTMO: spread 0.00003 punti (mediana), swap long 0.000059 short 0.0000435 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 38 nella ricerca, 38 nella prova

## PCH Price Channel

759 simulazioni in 58.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=1, OffsetTicks=2, Direction=2 | ✔ | avg smussato 14.9, netto 9,833, 662 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 14.9, netto 9,833, 662 trade |
| finestra: fine | EndHour | -1 | 3 | · | avg smussato 16.4, netto 8,227, 539 trade; annullato dall'ultimo terzo (-4,715 -> -7,132) |
| stop | StopAtr | 0.8 | 2.0 | · | D2: netto smussato 10,628 (migliore 11,172), 662 trade; annullato dall'ultimo terzo (-4,715 -> -4,913) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 14.9, netto 9,833, 662 trade |
| YES | PtnNeutYes | 55 | 29 | ✔ | batte lo spento: netto 12,082 contro 9,833, net/DD 2.41 contro 1.16 |
| NO | PtnNeutNo | 56 | 49 | ✔ | batte lo spento: netto 12,908 contro 12,082, net/DD 4.14 contro 2.41 |
| direzionale YES | PtnDirYes | 52 | -9 | · | batte lo spento: netto 18,142 contro 12,908, net/DD 6.50 contro 4.14; annullato dall'ultimo terzo (318 -> -895) |
| direzionale NO | PtnDirNo | 53 | -2 | · | batte lo spento: netto 18,885 contro 12,908, net/DD 8.05 contro 4.14; annullato dall'ultimo terzo (318 -> 128) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 12,961 contro 12,908, net/DD 4.16 contro 4.14; annullato dall'ultimo terzo (318 -> -538) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 3.0 | ✔ | batte lo spento: netto 16,205 contro 12,908, net/DD 5.34 contro 4.14 |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 16,855 (migliore 16,855), 231 trade |

**Storia di ricerca**: 338 trade, netto 16,728, DD 3,672, average trade 49, UngerFit 1.33.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 338 su almeno 50 |
| average trade | passa | 49 contro la soglia 38 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 88.7 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,430 |
| ultimo terzo | **no** | netto 1,812, net/DD 0.49 (serve 1,3) |
| due terzi su tre | passa | 3,339 / 11,959 / 1,812 |
| outlier | **no** | trade migliore 15% del netto sulla storia, 47% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 17 con i pattern, -15 senza |
| pattern casuali | passa | 2 estrazioni su 20 fanno almeno altrettanto, p 0.14, Wilson 0.07 |
| plateau | passa | media 0.93 su 7 vicini, minimo 0.82 |

**Prova su FTMO**: 196 trade, netto -1,668, DD 3,873, net/DD -0.43, average trade -9 (soglia 38), UngerFit , finestre in utile 2/4 (-1,586 / 995 / 675 / -1,752).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.43 (serve 1), average trade -9 (soglia 38), finestre 2/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.9 2021:1.3 2022:2.8 2023:1.5 2024:-0.7 2025:-0.2 2026:-0.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=3.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=29, PtnNeutNo=49, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=1, OffsetTicks=2, Direction=2`

## TFM Trend following mirrored

437 simulazioni in 37.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 11 | · | nessuna in utile: netto massimo -14,189; annullato dall'ultimo terzo (-552 -> -3,019) |
| finestra: fine | EndHour | -1 | 0 | ✔ | avg smussato -4.3, netto 2,307, 183 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 2,719 (migliore 2,846), 183 trade; annullato dall'ultimo terzo (3,453 -> 2,660) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 14.6, netto 2,677, 183 trade |
| YES | PtnNeutYes | 55 | 29 | · | batte lo spento: netto 5,429 contro 2,677, net/DD 2.35 contro 0.52; annullato dall'ultimo terzo (3,973 -> 925) |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 5,429 contro 2,677, net/DD 2.35 contro 0.52; annullato dall'ultimo terzo (3,973 -> 925) |
| direzionale YES | PtnDirYes | 52 | -9 | · | batte lo spento: netto 7,549 contro 2,677, net/DD 3.11 contro 0.52; annullato dall'ultimo terzo (3,973 -> 3,634) |
| direzionale NO | PtnDirNo | 53 | -4 | · | batte lo spento: netto 7,549 contro 2,677, net/DD 3.11 contro 0.52; annullato dall'ultimo terzo (3,973 -> 3,956) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 3,762 contro 2,677, net/DD 0.85 contro 0.52; annullato dall'ultimo terzo (3,973 -> 3,059) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 3.0 | · | batte lo spento: netto 2,932 contro 2,677, net/DD 0.57 contro 0.52; annullato dall'ultimo terzo (3,973 -> 3,968) |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 3,254 (migliore 3,254), 183 trade; annullato dall'ultimo terzo (3,973 -> 3,506) |

**Storia di ricerca**: 283 trade, netto 6,650, DD 5,144, average trade 23, UngerFit 0.53.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 283 su almeno 50 |
| average trade | **no** | 23 contro la soglia 38 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 74.3 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 3,973 |
| ultimo terzo | passa | netto 3,973, net/DD 3.84 (serve 1,3) |
| due terzi su tre | passa | -1,025 / 3,702 / 3,973 |
| outlier | **no** | trade migliore 20% del netto sulla storia, 34% sull'ultimo terzo |
| plateau | passa | media 0.73 su 2 vicini, minimo 0.54 |

**Prova su FTMO**: 155 trade, netto -4,700, DD 6,927, net/DD -0.68, average trade -30 (soglia 38), UngerFit , finestre in utile 1/4 (-4,530 / -1,378 / -83 / 1,040).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.68 (serve 1), average trade -30 (soglia 38), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 236% del netto (massimo 40%); average trade / soglia per anno: 2020:-7.4 2021:0.3 2022:1.6 2023:0.8 2024:-0.9 2025:-0.6 2026:0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=0, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

932 simulazioni in 69.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 11 | · | nessuna in utile: netto massimo -14,189; annullato dall'ultimo terzo (-552 -> -3,019) |
| finestra: fine | EndHour | -1 | 0 | ✔ | avg smussato -4.3, netto 2,307, 183 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 2,719 (migliore 2,846), 183 trade; annullato dall'ultimo terzo (3,453 -> 2,660) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 14.6, netto 2,677, 183 trade |
| YES long | FastYesLong | 152 | 139 | · | batte lo spento: netto 7,467 contro 2,677, net/DD 2.69 contro 0.52; annullato dall'ultimo terzo (3,973 -> 25) |
| YES short | FastYesShort | 152 | 106 | ✔ | batte lo spento: netto 4,207 contro 2,677, net/DD 1.07 contro 0.52 |
| NO long | FastNoLong | 153 | 70 | · | batte lo spento: netto 8,333 contro 4,207, net/DD 5.47 contro 1.07; annullato dall'ultimo terzo (4,394 -> -1,283) |
| NO short | FastNoShort | 153 | 81 | ✔ | batte lo spento: netto 4,957 contro 4,207, net/DD 1.32 contro 1.07 |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 4,942 contro 4,957, net/DD 1.71 contro 1.32; annullato dall'ultimo terzo (4,394 -> 3,485) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 3.0 | · | batte lo spento: netto 5,211 contro 4,957, net/DD 1.39 contro 1.32; annullato dall'ultimo terzo (4,394 -> 4,388) |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 5,041 (migliore 5,083), 148 trade; annullato dall'ultimo terzo (4,394 -> 3,396) |

**Storia di ricerca**: 231 trade, netto 9,350, DD 3,757, average trade 40, UngerFit 1.08.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 231 su almeno 50 |
| average trade | passa | 40 contro la soglia 38 |
| anni | passa | 5 anni, minimo 5 trade in un anno, 60.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,394 |
| ultimo terzo | passa | netto 4,394, net/DD 3.95 (serve 1,3) |
| due terzi su tre | passa | 307 / 4,649 / 4,394 |
| outlier | **no** | trade migliore 15% del netto sulla storia, 31% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 53 con i pattern, 40 senza |
| pattern casuali | **no** | 30 estrazioni su 53 fanno almeno altrettanto, p 0.57, Wilson 0.49 |
| plateau | passa | media 0.81 su 2 vicini, minimo 0.68 |

**Prova su FTMO**: 126 trade, netto -180, DD 4,444, net/DD -0.04, average trade -1 (soglia 38), UngerFit , finestre in utile 3/4 (-3,783 / 754 / 794 / 1,788).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.04 (serve 1), average trade -1 (soglia 38), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 52% del netto (massimo 40%); average trade / soglia per anno: 2020:-6.9 2021:0.7 2022:2.2 2023:1.5 2024:-1.1 2025:0.8 2026:1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=0, FastYesLong=152, FastYesShort=106, FastNoLong=153, FastNoShort=81, SkipDay=-1`

## BO Breakout di N sessioni

708 simulazioni in 74.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=10, IncludeCurrentSession=1 | ✔ | avg smussato 13.4, netto 3,595, 268 trade |
| finestra: inizio | StartHour | -1 | 6 | ✔ | avg smussato 28.0, netto 6,633, 242 trade |
| finestra: fine | EndHour | -1 | 13 | ✔ | avg smussato 34.0, netto 6,276, 180 trade |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 7,829 (migliore 8,111), 180 trade; annullato dall'ultimo terzo (-3,363 -> -3,531) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 45.6, netto 7,661, 168 trade |
| YES | PtnNeutYes | 55 | 29 | ✔ | batte lo spento: netto 9,630 contro 7,661, net/DD 4.99 contro 1.81 |
| NO | PtnNeutNo | 56 | 47 | · | batte lo spento: netto 9,969 contro 9,630, net/DD 6.97 contro 4.99; annullato dall'ultimo terzo (1,293 -> 672) |
| direzionale YES | PtnDirYes | 52 | -9 | · | batte lo spento: netto 9,052 contro 9,630, net/DD 6.26 contro 4.99; annullato dall'ultimo terzo (1,293 -> 1,007) |
| direzionale NO | PtnDirNo | 53 | -4 | · | batte lo spento: netto 9,052 contro 9,630, net/DD 6.26 contro 4.99; annullato dall'ultimo terzo (1,293 -> 1,007) |
| calendario | SkipDay | -1 | 1 | ✔ | batte lo spento: netto 9,912 contro 9,630, net/DD 7.51 contro 4.99 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 8,836 (migliore 9,265), 64 trade |

**Storia di ricerca**: 102 trade, netto 11,306, DD 1,874, average trade 111, UngerFit 4.18.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 102 su almeno 50 |
| average trade | passa | 111 contro la soglia 38 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 26.8 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,394 |
| ultimo terzo | **no** | netto 1,394, net/DD 0.74 (serve 1,3) |
| due terzi su tre | passa | 2,903 / 7,009 / 1,394 |
| outlier | **no** | trade migliore 35% del netto sulla storia, 86% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 37 con i pattern, -37 senza |
| pattern casuali | passa | 0 estrazioni su 6 fanno almeno altrettanto, p 0.14, Wilson 0.04 |
| plateau | passa | media 0.84 su 6 vicini, minimo 0.58 |

**Prova su FTMO**: 49 trade, netto 901, DD 2,207, net/DD 0.41, average trade 18 (soglia 38), UngerFit 0.64, finestre in utile 3/4 (-688 / 516 / 626 / 447).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.41 (serve 1), average trade 18 (soglia 38), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 70% del netto (massimo 40%); average trade / soglia per anno: 2020:-12.7 2021:4.0 2022:6.7 2023:-0.1 2024:1.2 2025:-0.6 2026:2.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=6, EndHour=13, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=29, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=1, Sessions=5, BreakoutOffsetTicks=10`

## BOS Breakout della sessione in corso

444 simulazioni in 42.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=2 | ✔ | avg smussato 13.7, netto 10,952, 800 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 13.7, netto 10,952, 800 trade |
| finestra: fine | EndHour | -1 | 10 | ✔ | avg smussato 19.1, netto 13,477, 677 trade |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 11,840 (migliore 11,840), 663 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 17.5, netto 11,537, 661 trade; annullato dall'ultimo terzo (5,314 -> -239) |
| YES | PtnNeutYes | 55 | 24 | · | batte lo spento: netto 11,050 contro 11,218, net/DD 4.02 contro 1.10; annullato dall'ultimo terzo (5,314 -> 709) |
| NO | PtnNeutNo | 56 | 28 | · | batte lo spento: netto 11,050 contro 11,218, net/DD 4.02 contro 1.10; annullato dall'ultimo terzo (5,314 -> 709) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 19,483 contro 11,218, net/DD 6.92 contro 1.10; annullato dall'ultimo terzo (5,314 -> -1,947) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 18,490 contro 11,218, net/DD 2.68 contro 1.10; annullato dall'ultimo terzo (5,314 -> -684) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 17,347 contro 11,218, net/DD 3.12 contro 1.10; annullato dall'ultimo terzo (5,314 -> 5,193) |
| uscita a ora fissa | ExitHour | -1 | 18 | · | batte lo spento: netto 14,391 contro 11,218, net/DD 2.01 contro 1.10; annullato dall'ultimo terzo (5,314 -> 3,627) |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 13,229 contro 11,218, net/DD 1.62 contro 1.10 |
| affinamento stop | StopAtr | 1.0 | 1.0 | · | D2: netto smussato 13,386 (migliore 13,386), 663 trade |

**Storia di ricerca**: 995 trade, netto 18,460, DD 8,161, average trade 19, UngerFit 0.34.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 995 su almeno 50 |
| average trade | **no** | 19 contro la soglia 38 |
| anni | passa | 5 anni, minimo 38 trade in un anno, 261.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 5,231 |
| ultimo terzo | passa | netto 5,569, net/DD 1.41 (serve 1,3) |
| due terzi su tre | passa | -4,463 / 17,692 / 5,569 |
| outlier | passa | trade migliore 11% del netto sulla storia, 18% sull'ultimo terzo |
| plateau | passa | media 0.75 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 527 trade, netto 5,854, DD 4,173, net/DD 1.40, average trade 11 (soglia 38), UngerFit 0.28, finestre in utile 4/4 (1,915 / 2,698 / 193 / 975).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.40 (serve 1), average trade 11 (soglia 38), finestre 4/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 51% del netto (massimo 40%); average trade / soglia per anno: 2020:0.2 2021:-0.3 2022:1.3 2023:0.9 2024:0.3 2025:0.3 2026:0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=10, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=2`

## VBO Volatility breakout

648 simulazioni in 30.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.5, Direction=2 | ✔ | avg smussato 19.9, netto 2,462, 124 trade |
| finestra: inizio | StartHour | -1 | 13 | ✔ | avg smussato 39.1, netto 3,532, 101 trade |
| finestra: fine | EndHour | -1 | 15 | · | avg smussato 42.8, netto 4,344, 87 trade; annullato dall'ultimo terzo (-1,335 -> -1,654) |
| stop | StopAtr | 0.8 | 1.5 | ✔ | D2: netto smussato 4,606 (migliore 4,767), 101 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 43.1, netto 4,350, 101 trade |
| YES | PtnNeutYes | 55 | 13 | ✔ | batte lo spento: netto 4,450 contro 4,350, net/DD 6.70 contro 1.27 |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 4,929 contro 4,450, net/DD 7.42 contro 6.70 |
| affinamento stop | StopAtr | 1.5 | 1.5 | · | D2: netto smussato 4,929 (migliore 4,929), 57 trade |

**Storia di ricerca**: 87 trade, netto 4,536, DD 1,044, average trade 52, UngerFit 2.63.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 87 su almeno 50 |
| average trade | passa | 52 contro la soglia 38 |
| anni | passa | 4 anni, minimo 18 trade in un anno, 22.8 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -394 |
| ultimo terzo | **no** | netto -394, net/DD -0.38 (serve 1,3) |
| due terzi su tre | passa | 821 / 4,108 / -394 |
| outlier | passa | trade migliore 24% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -13 con i pattern, -20 senza |
| pattern casuali | passa | 3 estrazioni su 12 fanno almeno altrettanto, p 0.31, Wilson 0.17 |
| plateau | passa | media 0.74 su 6 vicini, minimo 0.02 |

**Prova su FTMO**: 50 trade, netto 201, DD 1,083, net/DD 0.19, average trade 4 (soglia 38), UngerFit 0.20, finestre in utile 2/4 (-328 / 781 / -938 / 686).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.19 (serve 1), average trade 4 (soglia 38), finestre 2/4 (servono 3) |
| anni | **no** | 5/6 anni in utile (serve 60%), anno migliore 69% del netto (massimo 40%); average trade / soglia per anno: 2021:1.1 2022:3.2 2023:1.0 2024:-0.2 2025:0.1 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.5, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=13, EndHour=-1, PtnNeutYes=13, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=1.5, Direction=2`

## MAC Incrocio di medie

128 simulazioni in 6.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=5, SlowPeriod=50, Direction=0 | ✔ | avg smussato 22.1, netto 6,376, 289 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 7,362 (migliore 7,487), 387 trade; annullato dall'ultimo terzo (4,429 -> -2,285) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 22.1, netto 6,376, 289 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 12,207 contro 6,376, net/DD 4.05 contro 1.54 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 11,864 (migliore 11,864), 371 trade; annullato dall'ultimo terzo (5,900 -> 3,857) |

**Storia di ricerca**: 569 trade, netto 18,107, DD 3,017, average trade 32, UngerFit 0.95.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 569 su almeno 50 |
| average trade | **no** | 32 contro la soglia 38 |
| anni | passa | 5 anni, minimo 21 trade in un anno, 149.3 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 5,900 |
| ultimo terzo | passa | netto 5,900, net/DD 3.76 (serve 1,3) |
| due terzi su tre | passa | 5,964 / 6,243 / 5,900 |
| outlier | passa | trade migliore 4% del netto sulla storia, 5% sull'ultimo terzo |
| plateau | **no** | media 0.44 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 285 trade, netto -6,657, DD 9,449, net/DD -0.70, average trade -23 (soglia 38), UngerFit , finestre in utile 1/4 (-2,930 / -2,947 / -1,709 / 725).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.70 (serve 1), average trade -23 (soglia 38), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2020:1.3 2021:1.0 2022:0.3 2023:1.6 2024:-0.4 2025:-0.7 2026:0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=5, SlowPeriod=50, Direction=0`

## RBM Reversal Bollinger mirrored

472 simulazioni in 33.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 7.6, netto 6,814, 899 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 8.5, netto 6,814, 899 trade |
| finestra: fine | EndHour | -1 | 12 | ✔ | avg smussato 12.8, netto 7,239, 656 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 8,924 (migliore 8,924), 657 trade |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 9,361 contro 8,638, net/DD 3.05 contro 2.81 |
| YES | PtnNeutYes | 55 | 25 | · | batte lo spento: netto 8,521 contro 9,361, net/DD 4.05 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,589) |
| NO | PtnNeutNo | 56 | 29 | · | batte lo spento: netto 8,521 contro 9,361, net/DD 4.05 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,589) |
| direzionale YES | PtnDirYes | 52 | 15 | · | batte lo spento: netto 7,863 contro 9,361, net/DD 12.60 contro 3.05; annullato dall'ultimo terzo (4,666 -> 460) |
| direzionale NO | PtnDirNo | 53 | 1 | · | batte lo spento: netto 8,963 contro 9,361, net/DD 10.00 contro 3.05; annullato dall'ultimo terzo (4,666 -> 827) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 8,050 contro 9,361, net/DD 3.27 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,660) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 14.2, netto 9,361, 657 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 9,647 (migliore 9,647), 657 trade |
| affinamento target | TargetAtr | 1.5 | 1.5 | · | batte lo spento: netto 9,361 contro 8,638, net/DD 3.05 contro 2.81 |
| durata | MaxBars | 4 | 0 | · | avg smussato 16.9, netto 9,910, 585 trade; annullato dall'ultimo terzo (4,666 -> -1,408) |

**Storia di ricerca**: 978 trade, netto 14,094, DD 3,069, average trade 14, UngerFit 0.42.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 978 su almeno 50 |
| average trade | **no** | 14 contro la soglia 38 |
| anni | passa | 5 anni, minimo 35 trade in un anno, 256.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,733 |
| ultimo terzo | passa | netto 4,666, net/DD 5.26 (serve 1,3) |
| due terzi su tre | passa | 4,312 / 5,049 / 4,666 |
| outlier | passa | trade migliore 12% del netto sulla storia, 13% sull'ultimo terzo |
| plateau | **no** | media 0.56 su 8 vicini, minimo 0.02 |

**Prova su FTMO**: 497 trade, netto 3,050, DD 3,123, net/DD 0.98, average trade 6 (soglia 38), UngerFit 0.18, finestre in utile 2/4 (3,975 / -915 / -347 / 338).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.98 (serve 1), average trade 6 (soglia 38), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 26% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.2 2021:0.5 2022:0.5 2023:0.4 2024:0.3 2025:0.1 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=1.5, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=12, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RBU Reversal Bollinger unmirrored

766 simulazioni in 52.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 7.6, netto 6,814, 899 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 8.5, netto 6,814, 899 trade |
| finestra: fine | EndHour | -1 | 12 | ✔ | avg smussato 12.8, netto 7,239, 656 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 8,924 (migliore 8,924), 657 trade |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 9,361 contro 8,638, net/DD 3.05 contro 2.81 |
| YES long | FastYesLong | 152 | 45 | · | batte lo spento: netto 9,491 contro 9,361, net/DD 3.44 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,239) |
| YES short | FastYesShort | 152 | 77 | · | batte lo spento: netto 8,573 contro 9,361, net/DD 4.23 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,519) |
| NO long | FastNoLong | 153 | 39 | · | batte lo spento: netto 8,953 contro 9,361, net/DD 4.52 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,654) |
| NO short | FastNoShort | 153 | 32 | · | batte lo spento: netto 11,102 contro 9,361, net/DD 4.70 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,471) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 8,050 contro 9,361, net/DD 3.27 contro 3.05; annullato dall'ultimo terzo (4,666 -> 2,660) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 14.2, netto 9,361, 657 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 9,647 (migliore 9,647), 657 trade |
| affinamento target | TargetAtr | 1.5 | 1.5 | · | batte lo spento: netto 9,361 contro 8,638, net/DD 3.05 contro 2.81 |
| durata | MaxBars | 4 | 0 | · | avg smussato 16.9, netto 9,910, 585 trade; annullato dall'ultimo terzo (4,666 -> -1,408) |

**Storia di ricerca**: 978 trade, netto 14,094, DD 3,069, average trade 14, UngerFit 0.42.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 978 su almeno 50 |
| average trade | **no** | 14 contro la soglia 38 |
| anni | passa | 5 anni, minimo 35 trade in un anno, 256.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,733 |
| ultimo terzo | passa | netto 4,666, net/DD 5.26 (serve 1,3) |
| due terzi su tre | passa | 4,312 / 5,049 / 4,666 |
| outlier | passa | trade migliore 12% del netto sulla storia, 13% sull'ultimo terzo |
| plateau | **no** | media 0.56 su 8 vicini, minimo 0.02 |

**Prova su FTMO**: 497 trade, netto 3,050, DD 3,123, net/DD 0.98, average trade 6 (soglia 38), UngerFit 0.18, finestre in utile 2/4 (3,975 / -915 / -347 / 338).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.98 (serve 1), average trade 6 (soglia 38), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 26% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.2 2021:0.5 2022:0.5 2023:0.4 2024:0.3 2025:0.1 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=1.5, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=12, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RHL Reversal sui livelli di ieri

466 simulazioni in 28.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=5, Direction=1 | ✔ | avg smussato 7.9, netto 2,764, 351 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 7.9, netto 2,764, 351 trade |
| finestra: fine | EndHour | -1 | 7 | · | avg smussato 22.2, netto 4,879, 173 trade; annullato dall'ultimo terzo (216 -> -72) |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 2,904 (migliore 2,904), 351 trade; annullato dall'ultimo terzo (216 -> -55) |
| target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 2,838 contro 2,764, net/DD 1.06 contro 1.03 |
| YES | PtnNeutYes | 55 | 49 | · | batte lo spento: netto 3,345 contro 2,838, net/DD 4.84 contro 1.06; annullato dall'ultimo terzo (629 -> 113) |
| NO | PtnNeutNo | 56 | 19 | · | batte lo spento: netto 4,074 contro 2,838, net/DD 1.91 contro 1.06; annullato dall'ultimo terzo (629 -> -926) |
| direzionale YES | PtnDirYes | 52 | -5 | · | batte lo spento: netto 4,580 contro 2,838, net/DD 3.59 contro 1.06; annullato dall'ultimo terzo (629 -> 56) |
| direzionale NO | PtnDirNo | 53 | -45 | · | batte lo spento: netto 5,001 contro 2,838, net/DD 2.11 contro 1.06; annullato dall'ultimo terzo (629 -> -298) |
| calendario | SkipDay | -1 | 4 | · | batte lo spento: netto 4,320 contro 2,838, net/DD 2.06 contro 1.06; annullato dall'ultimo terzo (629 -> -156) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 8.1, netto 2,838, 351 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 2,937 (migliore 2,937), 351 trade; annullato dall'ultimo terzo (629 -> 234) |
| affinamento target | TargetAtr | 0.5 | 0.5 | · | batte lo spento: netto 2,838 contro 2,764, net/DD 1.06 contro 1.03 |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 9.2, netto 4,037, 351 trade |

**Storia di ricerca**: 511 trade, netto 5,007, DD 2,472, average trade 10, UngerFit 0.32.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 511 su almeno 50 |
| average trade | **no** | 10 contro la soglia 38 |
| anni | passa | 5 anni, minimo 14 trade in un anno, 134.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 970 |
| ultimo terzo | **no** | netto 970, net/DD 0.51 (serve 1,3) |
| due terzi su tre | passa | 3,686 / 351 / 970 |
| outlier | passa | trade migliore 12% del netto sulla storia, 29% sull'ultimo terzo |
| plateau | passa | media 0.76 su 7 vicini, minimo 0.56 |

**Prova su FTMO**: 251 trade, netto 3,162, DD 3,286, net/DD 0.96, average trade 13 (soglia 38), UngerFit 0.36, finestre in utile 3/4 (-2,691 / 1,863 / 2,970 / 1,097).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.96 (serve 1), average trade 13 (soglia 38), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 42% del netto (massimo 40%); average trade / soglia per anno: 2020:0.2 2021:0.7 2022:0.1 2023:-0.1 2024:0.0 2025:0.3 2026:1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0.5, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=5, Direction=1`

## LF Level fader sul pivot di ieri

307 simulazioni in 15.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | avg smussato 1.5, netto 631, 426 trade |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 1,260 (migliore 1,260), 427 trade |
| target | TargetAtr | 0 | 3.0 | ✔ | batte lo spento: netto 2,800 contro 2,143, net/DD 0.88 contro 0.67 |
| YES | PtnNeutYes | 55 | 52 | · | batte lo spento: netto 4,060 contro 2,800, net/DD 3.33 contro 0.88; annullato dall'ultimo terzo (4,098 -> 2,157) |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 5,732 contro 2,800, net/DD 3.32 contro 0.88; annullato dall'ultimo terzo (4,098 -> 3,883) |
| direzionale YES | PtnDirYes | 52 | 6 | · | batte lo spento: netto 4,278 contro 2,800, net/DD 2.12 contro 0.88; annullato dall'ultimo terzo (4,098 -> 1,987) |
| calendario long | NotEntryDayLong | -1 | 3 | · | batte lo spento: netto 4,055 contro 2,800, net/DD 1.53 contro 0.88; annullato dall'ultimo terzo (4,098 -> 3,875) |
| calendario short | NotEntryDayShort | -1 | 3 | · | batte lo spento: netto 3,880 contro 2,800, net/DD 1.36 contro 0.88; annullato dall'ultimo terzo (4,098 -> 3,131) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 6.6, netto 2,800, 427 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.5 | 0.5 | · | D2: netto smussato 1,917 (migliore 1,917), 427 trade |
| affinamento target | TargetAtr | 3.0 | 3.0 | · | batte lo spento: netto 2,800 contro 2,143, net/DD 0.88 contro 0.67 |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 11.5, netto 6,406, 387 trade |

**Storia di ricerca**: 548 trade, netto 12,303, DD 3,037, average trade 22, UngerFit 0.67.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 548 su almeno 50 |
| average trade | **no** | 22 contro la soglia 38 |
| anni | passa | 5 anni, minimo 34 trade in un anno, 143.8 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 5,897 |
| ultimo terzo | passa | netto 5,897, net/DD 3.85 (serve 1,3) |
| due terzi su tre | passa | 1,416 / 4,990 / 5,897 |
| outlier | passa | trade migliore 20% del netto sulla storia, 11% sull'ultimo terzo |
| plateau | passa | media 0.67 su 8 vicini, minimo 0.00 |

**Prova su FTMO**: 249 trade, netto 593, DD 2,216, net/DD 0.27, average trade 2 (soglia 38), UngerFit 0.08, finestre in utile 2/4 (-329 / 897 / 620 / -595).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.27 (serve 1), average trade 2 (soglia 38), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 35% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.3 2021:0.4 2022:0.9 2023:0.7 2024:0.3 2025:0.3 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=3.0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=10`

## LFHL Level fader sugli estremi di ieri

309 simulazioni in 15.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=2 | ✔ | avg smussato 10.2, netto 5,942, 582 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 6,107 (migliore 6,107), 584 trade; annullato dall'ultimo terzo (3,723 -> 2,382) |
| target | TargetAtr | 0 | 3.0 | ✔ | batte lo spento: netto 6,599 contro 5,942, net/DD 1.64 contro 1.48 |
| YES | PtnNeutYes | 55 | 18 | · | batte lo spento: netto 7,339 contro 6,599, net/DD 2.11 contro 1.64; annullato dall'ultimo terzo (3,723 -> 649) |
| NO | PtnNeutNo | 56 | 12 | · | batte lo spento: netto 7,339 contro 6,599, net/DD 2.11 contro 1.64; annullato dall'ultimo terzo (3,723 -> 649) |
| direzionale YES | PtnDirYes | 52 | -46 | · | batte lo spento: netto 9,938 contro 6,599, net/DD 5.59 contro 1.64; annullato dall'ultimo terzo (3,723 -> 142) |
| calendario long | NotEntryDayLong | -1 | 0 | ✔ | batte lo spento: netto 7,322 contro 6,599, net/DD 2.91 contro 1.64 |
| calendario short | NotEntryDayShort | -1 | 3 | · | batte lo spento: netto 8,489 contro 7,322, net/DD 3.26 contro 2.91; annullato dall'ultimo terzo (3,772 -> 2,919) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 14.6, netto 7,322, 502 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 7,475 (migliore 7,475), 506 trade; annullato dall'ultimo terzo (3,772 -> 1,979) |
| affinamento target | TargetAtr | 3.0 | 3.0 | · | batte lo spento: netto 7,322 contro 6,664, net/DD 2.91 contro 2.65 |
| durata | MaxBars | 4 | 6 | · | avg smussato 11.9, netto 8,013, 461 trade; annullato dall'ultimo terzo (3,772 -> 1,353) |

**Storia di ricerca**: 763 trade, netto 11,094, DD 2,513, average trade 15, UngerFit 0.47.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 763 su almeno 50 |
| average trade | **no** | 15 contro la soglia 38 |
| anni | passa | 5 anni, minimo 29 trade in un anno, 200.2 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 3,772 |
| ultimo terzo | passa | netto 3,772, net/DD 1.89 (serve 1,3) |
| due terzi su tre | passa | 3,458 / 3,864 / 3,772 |
| outlier | passa | trade migliore 23% del netto sulla storia, 22% sull'ultimo terzo |
| plateau | passa | media 0.68 su 8 vicini, minimo 0.03 |

**Prova su FTMO**: 368 trade, netto -4,323, DD 5,666, net/DD -0.76, average trade -12 (soglia 38), UngerFit , finestre in utile 0/4 (-1,016 / -2,233 / -288 / -787).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.76 (serve 1), average trade -12 (soglia 38), finestre 0/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 59% del netto (massimo 40%); average trade / soglia per anno: 2020:0.3 2021:0.2 2022:0.5 2023:0.2 2024:0.2 2025:-0.3 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=3.0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=0, NotEntryDayShort=-1, LevelShift=2`

## Riepilogo

- PCH: scartata; ricerca 338 trade netto 16,728 avg 49; prova 196 trade netto -1,668 net/DD -0.43 avg -9 fin 2/4; falliti: ultimo terzo, outlier, prova sul broker, anni
- TFM: scartata; ricerca 283 trade netto 6,650 avg 23; prova 155 trade netto -4,700 net/DD -0.68 avg -30 fin 1/4; falliti: average trade, outlier, prova sul broker, anni
- TFU: scartata; ricerca 231 trade netto 9,350 avg 40; prova 126 trade netto -180 net/DD -0.04 avg -1 fin 3/4; falliti: outlier, pattern casuali, prova sul broker, anni
- BO: scartata; ricerca 102 trade netto 11,306 avg 111; prova 49 trade netto 901 net/DD 0.41 avg 18 fin 3/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- BOS: scartata; ricerca 995 trade netto 18,460 avg 19; prova 527 trade netto 5,854 net/DD 1.40 avg 11 fin 4/4; falliti: average trade, prova sul broker, anni
- VBO: scartata; ricerca 87 trade netto 4,536 avg 52; prova 50 trade netto 201 net/DD 0.19 avg 4 fin 2/4; falliti: utile recente, ultimo terzo, prova sul broker, anni
- MAC: scartata; ricerca 569 trade netto 18,107 avg 32; prova 285 trade netto -6,657 net/DD -0.70 avg -23 fin 1/4; falliti: average trade, plateau, prova sul broker, anni
- RBM: scartata; ricerca 978 trade netto 14,094 avg 14; prova 497 trade netto 3,050 net/DD 0.98 avg 6 fin 2/4; falliti: average trade, plateau, prova sul broker
- RBU: scartata; ricerca 978 trade netto 14,094 avg 14; prova 497 trade netto 3,050 net/DD 0.98 avg 6 fin 2/4; falliti: average trade, plateau, prova sul broker
- RHL: scartata; ricerca 511 trade netto 5,007 avg 10; prova 251 trade netto 3,162 net/DD 0.96 avg 13 fin 3/4; falliti: average trade, ultimo terzo, prova sul broker, anni
- LF: scartata; ricerca 548 trade netto 12,303 avg 22; prova 249 trade netto 593 net/DD 0.27 avg 2 fin 2/4; falliti: average trade, prova sul broker
- LFHL: scartata; ricerca 763 trade netto 11,094 avg 15; prova 368 trade netto -4,323 net/DD -0.76 avg -12 fin 0/4; falliti: average trade, prova sul broker, anni
