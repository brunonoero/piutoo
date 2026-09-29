# Percorso v4 — @FDAX 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (20,691 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (11,102 barre), mai vista dal percorso
- costi FTMO: spread 1.33 punti (mediana), swap long 4.5288 short 0.0457 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 161 nella ricerca, 244 nella prova

## PCH Price Channel

555 simulazioni in 28.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=1, OffsetTicks=10, Direction=1 | ✔ | avg smussato 185.7, netto 116,800, 629 trade |
| finestra: inizio | StartHour | -1 | 2 | ✔ | avg smussato 204.6, netto 141,552, 629 trade |
| finestra: fine | EndHour | -1 | 10 | ✔ | avg smussato 249.7, netto 157,389, 583 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 141,281 (migliore 141,281), 583 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 270.0, netto 157,389, 583 trade |
| YES | PtnNeutYes | 55 | 3 | · | batte lo spento: netto 151,435 contro 157,389, net/DD 3.63 contro 3.57; annullato dall'ultimo terzo (69,870 -> 39,019) |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 151,435 contro 157,389, net/DD 3.63 contro 3.57; annullato dall'ultimo terzo (69,870 -> 39,019) |
| direzionale YES | PtnDirYes | 52 | -33 | · | batte lo spento: netto 164,262 contro 157,389, net/DD 6.34 contro 3.57; annullato dall'ultimo terzo (69,870 -> 26,375) |
| direzionale NO | PtnDirNo | 53 | -34 | · | batte lo spento: netto 164,134 contro 157,389, net/DD 6.34 contro 3.57; annullato dall'ultimo terzo (69,870 -> 26,375) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 152,852 contro 157,389, net/DD 4.55 contro 3.57; annullato dall'ultimo terzo (69,870 -> 44,330) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 168,613 contro 157,389, net/DD 4.75 contro 3.57 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 153,042 (migliore 153,042), 583 trade |

**Storia di ricerca**: 872 trade, netto 254,144, DD 36,087, average trade 291, UngerFit 1.21.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 872 su almeno 50 |
| average trade | passa | 291 contro la soglia 161 |
| anni | passa | 5 anni, minimo 16 trade in un anno, 228.8 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 85,531 |
| ultimo terzo | passa | netto 85,531, net/DD 2.37 (serve 1,3) |
| due terzi su tre | passa | 57,053 / 111,560 / 85,531 |
| outlier | passa | trade migliore 6% del netto sulla storia, 8% sull'ultimo terzo |
| plateau | passa | media 0.73 su 6 vicini, minimo 0.44 |

**Prova su FTMO**: 493 trade, netto 117,277, DD 72,476, net/DD 1.62, average trade 238 (soglia 244), UngerFit 0.57, finestre in utile 2/4 (64,598 / -14,057 / -10,610 / 77,345).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.62 (serve 1), average trade 238 (soglia 244), finestre 2/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 21% del netto (massimo 40%); average trade / soglia per anno: 2020:2.2 2021:1.7 2022:1.4 2023:1.7 2024:2.0 2025:0.5 2026:1.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=2, EndHour=10, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=1, OffsetTicks=10, Direction=1`

## TFM Trend following mirrored

437 simulazioni in 25.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 15 | · | avg smussato 61.9, netto 43,241, 451 trade; annullato dall'ultimo terzo (95,966 -> 21,608) |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 347.0, netto 48,965, 74 trade; annullato dall'ultimo terzo (95,966 -> 6,465) |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 39,321 (migliore 39,321), 608 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 49.9, netto 30,369, 608 trade |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 93,567 contro 30,369, net/DD 6.38 contro 0.60; annullato dall'ultimo terzo (96,978 -> 35,160) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 56,387 contro 30,369, net/DD 0.78 contro 0.60; annullato dall'ultimo terzo (96,978 -> 47,010) |
| direzionale YES | PtnDirYes | 52 | 39 | · | batte lo spento: netto 94,030 contro 30,369, net/DD 8.90 contro 0.60; annullato dall'ultimo terzo (96,978 -> 34,213) |
| direzionale NO | PtnDirNo | 53 | 49 | · | batte lo spento: netto 111,753 contro 30,369, net/DD 5.32 contro 0.60; annullato dall'ultimo terzo (96,978 -> 77,501) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 62,389 contro 30,369, net/DD 1.85 contro 0.60 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 74,653 contro 62,389, net/DD 2.09 contro 1.85; annullato dall'ultimo terzo (102,059 -> 81,194) |
| affinamento stop | StopAtr | 0.5 | 0.3 | · | D2: netto smussato 61,103 (migliore 63,718), 504 trade; annullato dall'ultimo terzo (102,059 -> 85,358) |

**Storia di ricerca**: 764 trade, netto 164,448, DD 33,739, average trade 215, UngerFit 0.92.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 764 su almeno 50 |
| average trade | passa | 215 contro la soglia 161 |
| anni | passa | 5 anni, minimo 13 trade in un anno, 200.5 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 102,059 |
| ultimo terzo | passa | netto 102,059, net/DD 4.80 (serve 1,3) |
| due terzi su tre | passa | 44,429 / 17,959 / 102,059 |
| outlier | passa | trade migliore 10% del netto sulla storia, 9% sull'ultimo terzo |
| plateau | passa | media 0.89 su 2 vicini, minimo 0.78 |

**Prova su FTMO**: 399 trade, netto 36,465, DD 54,859, net/DD 0.66, average trade 91 (soglia 244), UngerFit 0.25, finestre in utile 3/4 (20,811 / 14,992 / -13,249 / 13,912).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.66 (serve 1), average trade 91 (soglia 244), finestre 3/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 28% del netto (massimo 40%); average trade / soglia per anno: 2020:5.1 2021:0.9 2022:0.6 2023:1.6 2024:1.8 2025:0.6 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0`

## TFU Trend following unmirrored

731 simulazioni in 42.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 15 | · | avg smussato 61.9, netto 43,241, 451 trade; annullato dall'ultimo terzo (95,966 -> 21,608) |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 347.0, netto 48,965, 74 trade; annullato dall'ultimo terzo (95,966 -> 6,465) |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 39,321 (migliore 39,321), 608 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 49.9, netto 30,369, 608 trade |
| YES long | FastYesLong | 152 | 149 | · | batte lo spento: netto 46,108 contro 30,369, net/DD 0.83 contro 0.60; annullato dall'ultimo terzo (96,978 -> 85,709) |
| YES short | FastYesShort | 152 | 117 | · | batte lo spento: netto 121,734 contro 30,369, net/DD 5.08 contro 0.60; annullato dall'ultimo terzo (96,978 -> 83,532) |
| NO long | FastNoLong | 153 | 144 | · | batte lo spento: netto 46,108 contro 30,369, net/DD 0.83 contro 0.60; annullato dall'ultimo terzo (96,978 -> 85,709) |
| NO short | FastNoShort | 153 | 4 | · | batte lo spento: netto 84,648 contro 30,369, net/DD 2.92 contro 0.60; annullato dall'ultimo terzo (96,978 -> 70,162) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 62,389 contro 30,369, net/DD 1.85 contro 0.60 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 74,653 contro 62,389, net/DD 2.09 contro 1.85; annullato dall'ultimo terzo (102,059 -> 81,194) |
| affinamento stop | StopAtr | 0.5 | 0.3 | · | D2: netto smussato 61,103 (migliore 63,718), 504 trade; annullato dall'ultimo terzo (102,059 -> 85,358) |

**Storia di ricerca**: 764 trade, netto 164,448, DD 33,739, average trade 215, UngerFit 0.92.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 764 su almeno 50 |
| average trade | passa | 215 contro la soglia 161 |
| anni | passa | 5 anni, minimo 13 trade in un anno, 200.5 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 102,059 |
| ultimo terzo | passa | netto 102,059, net/DD 4.80 (serve 1,3) |
| due terzi su tre | passa | 44,429 / 17,959 / 102,059 |
| outlier | passa | trade migliore 10% del netto sulla storia, 9% sull'ultimo terzo |
| plateau | passa | media 0.89 su 2 vicini, minimo 0.78 |

**Prova su FTMO**: 399 trade, netto 36,465, DD 54,859, net/DD 0.66, average trade 91 (soglia 244), UngerFit 0.25, finestre in utile 3/4 (20,811 / 14,992 / -13,249 / 13,912).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.66 (serve 1), average trade 91 (soglia 244), finestre 3/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 28% del netto (massimo 40%); average trade / soglia per anno: 2020:5.1 2021:0.9 2022:0.6 2023:1.6 2024:1.8 2025:0.6 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=0`

## BO Breakout di N sessioni

510 simulazioni in 34.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=2, IncludeCurrentSession=0 | ✔ | avg smussato 16.0, netto 9,280, 581 trade |
| finestra: inizio | StartHour | -1 | 15 | · | avg smussato 59.7, netto 43,057, 443 trade; annullato dall'ultimo terzo (82,405 -> 19,587) |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 375.4, netto 50,924, 71 trade; annullato dall'ultimo terzo (82,405 -> 4,620) |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 30,924 (migliore 30,924), 599 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 39.0, netto 23,358, 599 trade |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 92,035 contro 23,358, net/DD 5.93 contro 0.45; annullato dall'ultimo terzo (87,843 -> 36,612) |
| NO | PtnNeutNo | 56 | 30 | · | batte lo spento: netto 75,988 contro 23,358, net/DD 1.52 contro 0.45; annullato dall'ultimo terzo (87,843 -> 52,267) |
| direzionale YES | PtnDirYes | 52 | 39 | · | batte lo spento: netto 92,798 contro 23,358, net/DD 8.21 contro 0.45; annullato dall'ultimo terzo (87,843 -> 35,865) |
| direzionale NO | PtnDirNo | 53 | 49 | · | batte lo spento: netto 97,736 contro 23,358, net/DD 3.69 contro 0.45; annullato dall'ultimo terzo (87,843 -> 65,760) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 56,057 contro 23,358, net/DD 1.54 contro 0.45 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.75 | ✔ | batte lo spento: netto 87,869 contro 56,057, net/DD 2.85 contro 1.54 |
| affinamento stop | StopAtr | 0.5 | 0.5 | · | D2: netto smussato 87,324 (migliore 87,324), 495 trade |

**Storia di ricerca**: 757 trade, netto 187,140, DD 30,880, average trade 247, UngerFit 1.11.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 757 su almeno 50 |
| average trade | passa | 247 contro la soglia 161 |
| anni | passa | 5 anni, minimo 13 trade in un anno, 198.6 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 99,272 |
| ultimo terzo | passa | netto 99,272, net/DD 4.56 (serve 1,3) |
| due terzi su tre | passa | 59,119 / 28,749 / 99,272 |
| outlier | passa | trade migliore 7% del netto sulla storia, 6% sull'ultimo terzo |
| plateau | passa | media 0.84 su 7 vicini, minimo 0.68 |

**Prova su FTMO**: 399 trade, netto 2,434, DD 54,917, net/DD 0.04, average trade 6 (soglia 244), UngerFit 0.02, finestre in utile 2/4 (23,039 / -26,229 / -2,139 / 7,764).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.04 (serve 1), average trade 6 (soglia 244), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 37% del netto (massimo 40%); average trade / soglia per anno: 2020:6.8 2021:1.3 2022:0.7 2023:1.5 2024:2.3 2025:0.1 2026:-0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0.75, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=0, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0, Sessions=1, BreakoutOffsetTicks=2`

## BOS Breakout della sessione in corso

537 simulazioni in 25.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=10 | ✔ | avg smussato 113.9, netto 79,955, 702 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 113.9, netto 79,955, 702 trade |
| finestra: fine | EndHour | -1 | 16 | ✔ | avg smussato 126.9, netto 88,253, 692 trade |
| stop | StopAtr | 0.8 | 1.5 | · | D2: netto smussato 83,651 (migliore 83,651), 637 trade; annullato dall'ultimo terzo (30,489 -> 26,070) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 127.5, netto 88,253, 692 trade |
| YES | PtnNeutYes | 55 | 5 | ✔ | batte lo spento: netto 133,486 contro 88,253, net/DD 2.76 contro 1.68 |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 105,703 contro 133,486, net/DD 3.45 contro 2.76; annullato dall'ultimo terzo (37,018 -> -7,992) |
| direzionale YES | PtnDirYes | 52 | 27 | ✔ | batte lo spento: netto 178,496 contro 133,486, net/DD 4.38 contro 2.76 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 176,347 contro 178,496, net/DD 5.73 contro 4.38 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 201,271 contro 176,347, net/DD 7.40 contro 5.73 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 209,787 (migliore 214,045), 348 trade; annullato dall'ultimo terzo (102,157 -> 98,778) |

**Storia di ricerca**: 554 trade, netto 303,429, DD 36,828, average trade 548, UngerFit 2.25.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 554 su almeno 50 |
| average trade | passa | 548 contro la soglia 161 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 145.4 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 102,157 |
| ultimo terzo | passa | netto 102,157, net/DD 2.77 (serve 1,3) |
| due terzi su tre | passa | 78,042 / 123,229 / 102,157 |
| outlier | passa | trade migliore 5% del netto sulla storia, 7% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 506 con i pattern, 238 senza |
| pattern casuali | passa | 2 estrazioni su 7 fanno almeno altrettanto, p 0.38, Wilson 0.19 |
| plateau | passa | media 0.78 su 6 vicini, minimo 0.42 |

**Prova su FTMO**: 308 trade, netto -110,027, DD 219,670, net/DD -0.50, average trade -357 (soglia 244), UngerFit , finestre in utile 2/4 (13,577 / 35,803 / -76,167 / -83,239).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.50 (serve 1), average trade -357 (soglia 244), finestre 2/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 72% del netto (massimo 40%); average trade / soglia per anno: 2020:1.2 2021:2.9 2022:4.3 2023:2.6 2024:2.0 2025:0.5 2026:-4.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=16, LevelSource=1, PtnNeutYes=5, PtnNeutNo=56, PtnDirYes=27, PtnDirNo=53, SkipDay=0, BreakoutOffsetTicks=10`

## VBO Volatility breakout

656 simulazioni in 25.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.5, Direction=2 | ✔ | avg smussato 71.6, netto 19,261, 269 trade |
| finestra: inizio | StartHour | -1 | 3 | ✔ | avg smussato 81.3, netto 27,664, 268 trade |
| finestra: fine | EndHour | -1 | 16 | · | avg smussato 128.0, netto 33,675, 255 trade; annullato dall'ultimo terzo (18,838 -> 14,102) |
| stop | StopAtr | 0.8 | 2.0 | · | D2: netto smussato 21,922 (migliore 22,817), 268 trade; annullato dall'ultimo terzo (18,838 -> 12,565) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 116.7, netto 31,267, 268 trade |
| YES | PtnNeutYes | 55 | 6 | · | batte lo spento: netto 78,307 contro 31,267, net/DD 6.69 contro 0.62; annullato dall'ultimo terzo (18,838 -> 6,800) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 78,307 contro 31,267, net/DD 6.69 contro 0.62; annullato dall'ultimo terzo (18,838 -> 6,800) |
| direzionale YES | PtnDirYes | 52 | -28 | · | batte lo spento: netto 97,640 contro 31,267, net/DD 12.61 contro 0.62; annullato dall'ultimo terzo (18,838 -> 18,247) |
| direzionale NO | PtnDirNo | 53 | 28 | ✔ | batte lo spento: netto 68,251 contro 31,267, net/DD 1.71 contro 0.62 |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 78,219 contro 68,251, net/DD 1.99 contro 1.71 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 71,737 (migliore 73,218), 150 trade; annullato dall'ultimo terzo (26,253 -> 22,148) |

**Storia di ricerca**: 224 trade, netto 104,472, DD 39,396, average trade 466, UngerFit 1.85.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 224 su almeno 50 |
| average trade | passa | 466 contro la soglia 161 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 58.8 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 26,253 |
| ultimo terzo | passa | netto 26,253, net/DD 1.47 (serve 1,3) |
| due terzi su tre | passa | 40,297 / 37,923 / 26,253 |
| outlier | **no** | trade migliore 17% del netto sulla storia, 37% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 355 con i pattern, 318 senza |
| pattern casuali | **no** | 2 estrazioni su 5 fanno almeno altrettanto, p 0.50, Wilson 0.25 |
| plateau | passa | media 0.73 su 4 vicini, minimo 0.51 |

**Prova su FTMO**: 108 trade, netto 7,599, DD 33,945, net/DD 0.22, average trade 70 (soglia 244), UngerFit 0.24, finestre in utile 2/4 (7,509 / -16,668 / 26,751 / -9,992).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.22 (serve 1), average trade 70 (soglia 244), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 53% del netto (massimo 40%); average trade / soglia per anno: 2020:7.2 2021:3.3 2022:5.1 2023:-1.8 2024:3.4 2025:0.5 2026:-0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=3, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=28, SkipDay=0, AtrMultiplierLong=0.5, Direction=2`

## MAC Incrocio di medie

126 simulazioni in 3.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=20, SlowPeriod=50, Direction=2 | ✔ | avg smussato 577.0, netto 85,393, 148 trade |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 87,490 (migliore 90,572), 148 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 612.0, netto 90,572, 148 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 95,310 contro 90,572, net/DD 6.58 contro 6.25; annullato dall'ultimo terzo (-5,189 -> -5,522) |
| affinamento stop | StopAtr | 1.25 | 1.25 | · | D2: netto smussato 87,490 (migliore 90,572), 148 trade |

**Storia di ricerca**: 217 trade, netto 85,383, DD 18,214, average trade 393, UngerFit 2.30.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 217 su almeno 50 |
| average trade | passa | 393 contro la soglia 161 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 56.9 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -5,189 |
| ultimo terzo | **no** | netto -5,189, net/DD -0.39 (serve 1,3) |
| due terzi su tre | passa | 55,013 / 35,560 / -5,189 |
| outlier | passa | trade migliore 21% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.40 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 115 trade, netto -83,221, DD 101,664, net/DD -0.82, average trade -724 (soglia 244), UngerFit , finestre in utile 0/4 (-24,067 / -37,112 / -27,758 / -1,505).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.82 (serve 1), average trade -724 (soglia 244), finestre 0/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 2,622% del netto (massimo 40%); average trade / soglia per anno: 2020:4.8 2021:3.6 2022:4.7 2023:-1.0 2024:-1.0 2025:-5.7 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=20, SlowPeriod=50, Direction=2`

## RBM Reversal Bollinger mirrored

669 simulazioni in 21.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=10, BbNumDevs=3.0 | ✔ | avg smussato 5.0, netto 3,971, 802 trade |
| finestra: inizio | StartHour | -1 | 15 | ✔ | avg smussato 118.0, netto 29,214, 226 trade |
| finestra: fine | EndHour | -1 | 6 | · | avg smussato 133.2, netto 78,263, 397 trade; annullato dall'ultimo terzo (-1,580 -> -30,981) |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 29,263 (migliore 29,263), 226 trade; annullato dall'ultimo terzo (-1,580 -> -3,605) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 46 | ✔ | batte lo spento: netto 28,856 contro 29,214, net/DD 5.84 contro 2.17 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 10 | · | batte lo spento: netto 30,246 contro 28,856, net/DD 7.32 contro 5.84; annullato dall'ultimo terzo (5,441 -> 5,260) |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 515.3, netto 28,856, 56 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 29,648 (migliore 29,648), 56 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 20 | · | avg smussato 727.1, netto 40,539, 55 trade; annullato dall'ultimo terzo (5,441 -> 3,003) |

**Storia di ricerca**: 76 trade, netto 35,209, DD 4,133, average trade 463, UngerFit 5.67.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 76 su almeno 50 |
| average trade | passa | 463 contro la soglia 161 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 19.9 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 5,441 |
| ultimo terzo | passa | netto 5,441, net/DD 1.89 (serve 1,3) |
| due terzi su tre | passa | 4,936 / 24,833 / 5,441 |
| outlier | **no** | trade migliore 16% del netto sulla storia, 59% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 272 con i pattern, -39 senza |
| pattern casuali | passa | 2 estrazioni su 29 fanno almeno altrettanto, p 0.10, Wilson 0.05 |
| plateau | passa | media 0.77 su 6 vicini, minimo 0.38 |

**Prova su FTMO**: 39 trade, netto -5,497, DD 22,730, net/DD -0.24, average trade -141 (soglia 244), UngerFit , finestre in utile 2/4 (1,026 / -507 / -8,496 / 2,480).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.24 (serve 1), average trade -141 (soglia 244), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 83% del netto (massimo 40%); average trade / soglia per anno: 2020:4.2 2021:0.9 2022:5.1 2023:1.9 2024:1.3 2025:-1.6 2026:-0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=15, EndHour=-1, PtnNeutYes=46, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=10, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

970 simulazioni in 40.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=10, BbNumDevs=3.0 | ✔ | avg smussato 5.0, netto 3,971, 802 trade |
| finestra: inizio | StartHour | -1 | 15 | ✔ | avg smussato 118.0, netto 29,214, 226 trade |
| finestra: fine | EndHour | -1 | 6 | · | avg smussato 133.2, netto 78,263, 397 trade; annullato dall'ultimo terzo (-1,580 -> -30,981) |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 29,263 (migliore 29,263), 226 trade; annullato dall'ultimo terzo (-1,580 -> -3,605) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 87 | ✔ | batte lo spento: netto 41,849 contro 29,214, net/DD 3.17 contro 2.17 |
| YES short | FastYesShort | 152 | 85 | ✔ | batte lo spento: netto 53,522 contro 41,849, net/DD 8.79 contro 3.17 |
| NO long | FastNoLong | 153 | 96 | · | batte lo spento: netto 46,556 contro 53,522, net/DD 17.61 contro 8.79; annullato dall'ultimo terzo (7,663 -> 6,896) |
| NO short | FastNoShort | 153 | 67 | ✔ | batte lo spento: netto 55,626 contro 53,522, net/DD 8.79 contro 8.79 |
| calendario | SkipDay | -1 | 3 | ✔ | batte lo spento: netto 50,842 contro 55,626, net/DD 9.83 contro 8.79 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 747.7, netto 50,842, 68 trade |
| uscita a ora fissa | ExitHour | -1 | 21 | · | batte lo spento: netto 44,552 contro 50,842, net/DD 11.88 contro 9.83; annullato dall'ultimo terzo (9,593 -> 7,808) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 51,447 (migliore 51,447), 68 trade; annullato dall'ultimo terzo (9,593 -> 7,496) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 20 | ✔ | avg smussato 956.2, netto 66,709, 68 trade |

**Storia di ricerca**: 93 trade, netto 81,783, DD 3,756, average trade 879, UngerFit 11.30.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 93 su almeno 50 |
| average trade | passa | 879 contro la soglia 161 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 24.4 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 15,074 |
| ultimo terzo | passa | netto 15,074, net/DD 5.64 (serve 1,3) |
| due terzi su tre | passa | 20,331 / 46,378 / 15,074 |
| outlier | passa | trade migliore 9% del netto sulla storia, 28% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 603 con i pattern, 76 senza |
| pattern casuali | passa | 8 estrazioni su 124 fanno almeno altrettanto, p 0.07, Wilson 0.05 |
| plateau | passa | media 0.73 su 5 vicini, minimo 0.40 |

**Prova su FTMO**: 41 trade, netto 9,758, DD 15,994, net/DD 0.61, average trade 238 (soglia 244), UngerFit 1.21, finestre in utile 3/4 (-3,870 / 4,774 / 6,901 / 1,954).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.61 (serve 1), average trade 238 (soglia 244), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 52% del netto (massimo 40%); average trade / soglia per anno: 2020:25.8 2021:2.7 2022:6.0 2023:3.4 2024:-0.1 2025:2.1 2026:1.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=20, IntradayOnly=1, ExitHour=-1, StartHour=15, EndHour=-1, FastYesLong=87, FastYesShort=85, FastNoLong=153, FastNoShort=67, SkipDay=3, BbLength=10, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

665 simulazioni in 23.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=1 | ✔ | avg smussato 59.6, netto 14,789, 248 trade |
| finestra: inizio | StartHour | -1 | 11 | ✔ | avg smussato 132.1, netto 44,065, 208 trade |
| finestra: fine | EndHour | -1 | 14 | · | avg smussato 292.3, netto 45,404, 164 trade; annullato dall'ultimo terzo (-3,243 -> -5,504) |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 40,677 (migliore 41,623), 208 trade; annullato dall'ultimo terzo (-3,243 -> -5,509) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 2 | ✔ | batte lo spento: netto 42,591 contro 44,065, net/DD 4.31 contro 1.25 |
| NO | PtnNeutNo | 56 | 40 | ✔ | batte lo spento: netto 52,278 contro 42,591, net/DD 8.85 contro 4.31 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 50,171 contro 52,278, net/DD 10.91 contro 8.85 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 895.9, netto 50,171, 56 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | · | batte lo spento: netto 51,101 contro 50,171, net/DD 11.11 contro 10.91; annullato dall'ultimo terzo (13,529 -> 13,304) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 50,656 (migliore 52,548), 56 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | · | avg smussato 765.9, netto 42,891, 56 trade; annullato dall'ultimo terzo (13,529 -> 3,412) |

**Storia di ricerca**: 75 trade, netto 63,699, DD 4,600, average trade 849, UngerFit 9.86.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 75 su almeno 50 |
| average trade | passa | 849 contro la soglia 161 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 19.7 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 13,529 |
| ultimo terzo | passa | netto 13,529, net/DD 2.95 (serve 1,3) |
| due terzi su tre | passa | 15,102 / 35,069 / 13,529 |
| outlier | **no** | trade migliore 14% del netto sulla storia, 32% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 712 con i pattern, 29 senza |
| pattern casuali | passa | 0 estrazioni su 29 fanno almeno altrettanto, p 0.03, Wilson 0.01 |
| plateau | passa | media 0.71 su 5 vicini, minimo 0.31 |

**Prova su FTMO**: 31 trade, netto -21,405, DD 24,918, net/DD -0.86, average trade -690 (soglia 244), UngerFit , finestre in utile 1/4 (-4,005 / 1,540 / -13,489 / -5,452).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.86 (serve 1), average trade -690 (soglia 244), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 70% del netto (massimo 40%); average trade / soglia per anno: 2020:7.0 2021:4.5 2022:4.9 2023:5.4 2024:3.2 2025:-3.9 2026:-1.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=11, EndHour=-1, PtnNeutYes=2, PtnNeutNo=40, PtnDirYes=52, PtnDirNo=53, SkipDay=0, LevelOffsetTicks=20, Direction=1`

## LF Level fader sul pivot di ieri

246 simulazioni in 8.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=5 | ✔ | nessuna in utile: netto massimo -7,346 |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato -621 (migliore -621), 283 trade; annullato dall'ultimo terzo (-533 -> -1,664) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 3 | · | batte lo spento: netto 27,269 contro -7,346, net/DD 2.50 contro -0.20; annullato dall'ultimo terzo (-533 -> -2,969) |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 27,269 contro -7,346, net/DD 2.50 contro -0.20; annullato dall'ultimo terzo (-533 -> -2,969) |
| direzionale YES | PtnDirYes | 52 | -8 | · | batte lo spento: netto 21,838 contro -7,346, net/DD 1.98 contro -0.20; annullato dall'ultimo terzo (-533 -> -3,199) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (455 trade, netto -7,879).

## LFHL Level fader sugli estremi di ieri

506 simulazioni in 15.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=5 | ✔ | avg smussato 77.3, netto 32,558, 421 trade |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 39,307 (migliore 40,293), 421 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 33 | ✔ | batte lo spento: netto 37,141 contro 38,416, net/DD 1.87 contro 1.86 |
| NO | PtnNeutNo | 56 | 18 | ✔ | batte lo spento: netto 48,778 contro 37,141, net/DD 3.68 contro 1.87 |
| direzionale YES | PtnDirYes | 52 | -15 | ✔ | batte lo spento: netto 41,588 contro 48,778, net/DD 8.52 contro 3.68 |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 815.4, netto 41,588, 51 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 1.25 | 0.8 | ✔ | D2: netto smussato 41,588 (migliore 41,588), 51 trade |
| affinamento target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 42,711 contro 41,588, net/DD 10.32 contro 8.52 |
| durata | MaxBars | 4 | 4 | · | avg smussato 757.8, netto 42,711, 51 trade |

**Storia di ricerca**: 59 trade, netto 41,849, DD 4,139, average trade 709, UngerFit 8.68.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 59 su almeno 50 |
| average trade | passa | 709 contro la soglia 161 |
| anni | passa | 4 anni, minimo 5 trade in un anno, 15.5 all'anno, 2 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -863 |
| ultimo terzo | **no** | netto -863, net/DD -0.27 (serve 1,3) |
| due terzi su tre | passa | 4,377 / 38,334 / -863 |
| outlier | passa | trade migliore 13% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -108 con i pattern, -171 senza |
| pattern casuali | **no** | 14 estrazioni su 52 fanno almeno altrettanto, p 0.28, Wilson 0.21 |
| plateau | passa | media 0.76 su 7 vicini, minimo 0.36 |

**Prova su FTMO**: 33 trade, netto 14,714, DD 13,191, net/DD 1.12, average trade 446 (soglia 244), UngerFit 2.49, finestre in utile 2/4 (9,062 / -4,571 / -7,037 / 17,260).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.12 (serve 1), average trade 446 (soglia 244), finestre 2/4 (servono 3) |
| anni | **no** | 4/6 anni in utile (serve 60%), anno migliore 72% del netto (massimo 40%); average trade / soglia per anno: 2021:-1.3 2022:4.8 2023:2.9 2024:5.0 2025:-5.5 2026:2.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0.5, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=33, PtnNeutNo=18, PtnDirYes=-15, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=5`

## Riepilogo

- PCH: scartata; ricerca 872 trade netto 254,144 avg 291; prova 493 trade netto 117,277 net/DD 1.62 avg 238 fin 2/4; falliti: prova sul broker
- TFM: scartata; ricerca 764 trade netto 164,448 avg 215; prova 399 trade netto 36,465 net/DD 0.66 avg 91 fin 3/4; falliti: prova sul broker
- TFU: scartata; ricerca 764 trade netto 164,448 avg 215; prova 399 trade netto 36,465 net/DD 0.66 avg 91 fin 3/4; falliti: prova sul broker
- BO: scartata; ricerca 757 trade netto 187,140 avg 247; prova 399 trade netto 2,434 net/DD 0.04 avg 6 fin 2/4; falliti: prova sul broker
- BOS: scartata; ricerca 554 trade netto 303,429 avg 548; prova 308 trade netto -110,027 net/DD -0.50 avg -357 fin 2/4; falliti: prova sul broker, anni
- VBO: scartata; ricerca 224 trade netto 104,472 avg 466; prova 108 trade netto 7,599 net/DD 0.22 avg 70 fin 2/4; falliti: anni, outlier, pattern casuali, prova sul broker, anni
- MAC: scartata; ricerca 217 trade netto 85,383 avg 393; prova 115 trade netto -83,221 net/DD -0.82 avg -724 fin 0/4; falliti: anni, utile recente, ultimo terzo, plateau, prova sul broker, anni
- RBM: scartata; ricerca 76 trade netto 35,209 avg 463; prova 39 trade netto -5,497 net/DD -0.24 avg -141 fin 2/4; falliti: anni, outlier, prova sul broker, anni
- RBU: scartata; ricerca 93 trade netto 81,783 avg 879; prova 41 trade netto 9,758 net/DD 0.61 avg 238 fin 3/4; falliti: anni, prova sul broker, anni
- RHL: scartata; ricerca 75 trade netto 63,699 avg 849; prova 31 trade netto -21,405 net/DD -0.86 avg -690 fin 1/4; falliti: anni, outlier, prova sul broker, anni
- LF: abbandonato (R9: la base non guadagna su tutta la storia (455 trade, netto -7,879))
- LFHL: scartata; ricerca 59 trade netto 41,849 avg 709; prova 33 trade netto 14,714 net/DD 1.12 avg 446 fin 2/4; falliti: utile recente, ultimo terzo, pattern casuali, prova sul broker, anni
