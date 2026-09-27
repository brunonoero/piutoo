# Percorso v4 — @FDAX 60m

- ricerca: feed interno 2008-01-01 → 2020-11-09 (50,767 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2020-11-09 → 2026-09-01 (31,357 barre), mai vista dal percorso
- costi FTMO: spread 1.33 punti (mediana), swap long 4.5288 short 0.0457 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 150 nella ricerca, 190 nella prova

## PCH Price Channel

757 simulazioni in 73.8 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=30, OffsetTicks=0, Direction=1 | ✔ | avg smussato 68.7, netto 63,313, 922 trade |
| finestra: inizio | StartHour | -1 | 8 | · | avg smussato 73.3, netto 63,313, 922 trade; annullato dall'ultimo terzo (-3,258 -> -5,407) |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 145.5, netto 68,400, 455 trade |
| stop | StopAtr | 0.8 | 0.4 | ✔ | D2: netto smussato 74,098 (migliore 77,845), 455 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 188.4, netto 85,705, 455 trade |
| YES | PtnNeutYes | 55 | 32 | ✔ | batte lo spento: netto 63,116 contro 85,705, net/DD 9.73 contro 5.21 |
| NO | PtnNeutNo | 56 | 7 | · | batte lo spento: netto 68,917 contro 63,116, net/DD 11.60 contro 9.73; annullato dall'ultimo terzo (63,830 -> 46,690) |
| direzionale YES | PtnDirYes | 52 | -47 | · | batte lo spento: netto 67,345 contro 63,116, net/DD 14.46 contro 9.73; annullato dall'ultimo terzo (63,830 -> 42,996) |
| direzionale NO | PtnDirNo | 53 | 17 | · | batte lo spento: netto 73,111 contro 63,116, net/DD 15.28 contro 9.73; annullato dall'ultimo terzo (63,830 -> 60,541) |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 67,392 contro 63,116, net/DD 10.51 contro 9.73 |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.4 | 0.5 | · | D2: netto smussato 71,689 (migliore 73,838), 84 trade; annullato dall'ultimo terzo (67,541 -> 66,589) |

**Storia di ricerca**: 162 trade, netto 134,934, DD 11,711, average trade 833, UngerFit 6.28.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 162 su almeno 50 |
| average trade | passa | 833 contro la soglia 150 |
| anni | **no** | 13 anni, minimo 2 trade in un anno, 12.6 all'anno, 11 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 67,541 |
| ultimo terzo | passa | netto 67,541, net/DD 5.77 (serve 1,3) |
| due terzi su tre | passa | 41,453 / 25,939 / 67,541 |
| outlier | passa | trade migliore 11% del netto sulla storia, 22% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 866 con i pattern, 75 senza |
| pattern casuali | passa | 0 estrazioni su 16 fanno almeno altrettanto, p 0.06, Wilson 0.02 |
| plateau | passa | media 0.89 su 6 vicini, minimo 0.64 |

**Prova su FTMO**: 138 trade, netto -14,155, DD 52,482, net/DD -0.27, average trade -103 (soglia 190), UngerFit , finestre in utile 1/4 (15,569 / -1,646 / -9,935 / -18,143).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.27 (serve 1), average trade -103 (soglia 190), finestre 1/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 90 trade 31,884 net/DD 2.34, FTMO 110 trade 15,381 net/DD 0.61 |
| anni | **no** | 15/19 anni in utile (serve 60%), anno migliore 50% del netto (massimo 40%); average trade / soglia per anno: 2008:12.0 2009:3.3 2010:11.9 2011:5.6 2012:6.0 2013:4.4 2014:12.0 2015:4.5 2016:-1.3 2017:14.6 2018:-6.4 2019:3.8 2020:4.9 2021:1.2 2022:1.2 2023:0.2 2024:-6.4 2025:1.1 2026:-4.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.4, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=9, PtnNeutYes=32, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=30, OffsetTicks=0, Direction=1`

## TFM Trend following mirrored

636 simulazioni in 63.6 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -19,084 |
| finestra: fine | EndHour | -1 | 11 | · | nessuna in utile: netto massimo -1,232; annullato dall'ultimo terzo (-24,076 -> -97,082) |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato 33,244 (migliore 33,244), 1430 trade; annullato dall'ultimo terzo (-24,076 -> -36,428) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -19,084 |
| YES | PtnNeutYes | 55 | 3 | · | batte lo spento: netto 73,310 contro -19,084, net/DD 3.00 contro -0.22; annullato dall'ultimo terzo (-24,076 -> -56,680) |
| NO | PtnNeutNo | 56 | 47 | ✔ | batte lo spento: netto 48,792 contro -19,084, net/DD 2.07 contro -0.22 |
| direzionale YES | PtnDirYes | 52 | -33 | · | batte lo spento: netto 49,520 contro 48,792, net/DD 3.40 contro 2.07; annullato dall'ultimo terzo (9,534 -> -7,267) |
| direzionale NO | PtnDirNo | 53 | 29 | · | batte lo spento: netto 74,245 contro 48,792, net/DD 4.19 contro 2.07; annullato dall'ultimo terzo (9,534 -> -10,466) |
| calendario | SkipDay | -1 | 1 | ✔ | batte lo spento: netto 64,367 contro 48,792, net/DD 3.11 contro 2.07 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 70,973 contro 64,367, net/DD 3.43 contro 3.11; annullato dall'ultimo terzo (29,090 -> 10,868) |
| affinamento stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 59,976 (migliore 61,258), 490 trade; annullato dall'ultimo terzo (29,090 -> 16,348) |

**Storia di ricerca**: 708 trade, netto 93,457, DD 49,238, average trade 132, UngerFit 0.49.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 708 su almeno 50 |
| average trade | **no** | 132 contro la soglia 150 |
| anni | passa | 13 anni, minimo 44 trade in un anno, 55.1 all'anno, 7 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 29,090 |
| ultimo terzo | **no** | netto 29,090, net/DD 0.63 (serve 1,3) |
| due terzi su tre | passa | 34,778 / 29,589 / 29,090 |
| outlier | **no** | trade migliore 34% del netto sulla storia, 109% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 133 con i pattern, -42 senza |
| pattern casuali | passa | 0 estrazioni su 12 fanno almeno altrettanto, p 0.08, Wilson 0.02 |
| plateau | passa | media 0.88 su 2 vicini, minimo 0.83 |

**Prova su FTMO**: 294 trade, netto -18,113, DD 87,316, net/DD -0.21, average trade -62 (soglia 190), UngerFit , finestre in utile 2/4 (3,417 / -29,508 / -27,944 / 35,923).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.21 (serve 1), average trade -62 (soglia 190), finestre 2/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 235 trade -13,286 net/DD -0.35, FTMO 233 trade -66,696 net/DD -0.84 |
| anni | **no** | 9/19 anni in utile (serve 60%), anno migliore 73% del netto (massimo 40%); average trade / soglia per anno: 2008:4.3 2009:1.9 2010:-1.4 2011:-0.2 2012:0.7 2013:-0.4 2014:1.9 2015:0.9 2016:-1.3 2017:-0.4 2018:-1.7 2019:0.7 2020:4.8 2021:0.6 2022:-1.4 2023:-1.9 2024:-2.6 2025:-1.6 2026:4.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=21, EndHour=-1, PtnNeutYes=55, PtnNeutNo=47, PtnDirYes=52, PtnDirNo=53, SkipDay=1`

## TFU Trend following unmirrored

928 simulazioni in 91.2 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -19,084 |
| finestra: fine | EndHour | -1 | 11 | · | nessuna in utile: netto massimo -1,232; annullato dall'ultimo terzo (-24,076 -> -97,082) |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato 33,244 (migliore 33,244), 1430 trade; annullato dall'ultimo terzo (-24,076 -> -36,428) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -19,084 |
| YES long | FastYesLong | 152 | 56 | ✔ | batte lo spento: netto 66,986 contro -19,084, net/DD 2.50 contro -0.22 |
| YES short | FastYesShort | 152 | 78 | ✔ | batte lo spento: netto 66,023 contro 66,986, net/DD 4.07 contro 2.50 |
| NO long | FastNoLong | 153 | 145 | ✔ | batte lo spento: netto 62,249 contro 66,023, net/DD 7.36 contro 4.07 |
| NO short | FastNoShort | 153 | 4 | · | batte lo spento: netto 64,490 contro 62,249, net/DD 9.76 contro 7.36; annullato dall'ultimo terzo (30,769 -> 12,557) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 69,835 contro 62,249, net/DD 9.58 contro 7.36 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 70,798 (migliore 70,798), 64 trade |

**Storia di ricerca**: 92 trade, netto 105,773, DD 8,959, average trade 1,150, UngerFit 9.92.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 92 su almeno 50 |
| average trade | passa | 1,150 contro la soglia 150 |
| anni | **no** | 13 anni, minimo 2 trade in un anno, 7.2 all'anno, 10 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 34,542 |
| ultimo terzo | passa | netto 34,542, net/DD 3.86 (serve 1,3) |
| due terzi su tre | passa | 53,525 / 17,706 / 34,542 |
| outlier | **no** | trade migliore 30% del netto sulla storia, 92% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 1,234 con i pattern, 37 senza |
| pattern casuali | passa | 5 estrazioni su 192 fanno almeno altrettanto, p 0.03, Wilson 0.02 |
| plateau | passa | media 0.97 su 2 vicini, minimo 0.94 |

**Prova su FTMO**: 32 trade, netto -25,221, DD 32,653, net/DD -0.77, average trade -788 (soglia 190), UngerFit , finestre in utile 0/4 (-2,665 / -15,847 / -1,825 / -4,884).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.77 (serve 1), average trade -788 (soglia 190), finestre 0/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 21 trade -14,421 net/DD -0.59, FTMO 27 trade -22,697 net/DD -0.80 |
| anni | **no** | 11/19 anni in utile (serve 60%), anno migliore 49% del netto (massimo 40%); average trade / soglia per anno: 2008:12.1 2009:7.9 2010:17.8 2011:6.9 2012:2.7 2013:-4.6 2014:0.4 2015:5.8 2016:-1.4 2017:0.1 2018:-0.5 2019:3.7 2020:10.8 2021:1.6 2022:-5.0 2023:-21.1 2024:-7.7 2025:-1.3 2026:-3.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=21, EndHour=-1, FastYesLong=56, FastYesShort=78, FastNoLong=145, FastNoShort=153, SkipDay=0`

## BO Breakout di N sessioni

708 simulazioni in 100.8 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=0, IncludeCurrentSession=0 | ✔ | nessuna in utile: netto massimo -18,853 |
| finestra: inizio | StartHour | -1 | 15 | · | avg smussato 1.1, netto 19,540, 984 trade; annullato dall'ultimo terzo (60,260 -> 13,852) |
| finestra: fine | EndHour | -1 | 8 | · | nessuna in utile: netto massimo -8,775; annullato dall'ultimo terzo (60,260 -> 50,009) |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 5,849 (migliore 5,849), 1171 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 16.5, netto 19,324, 1171 trade |
| YES | PtnNeutYes | 55 | 11 | · | batte lo spento: netto 96,071 contro 19,324, net/DD 4.97 contro 0.38; annullato dall'ultimo terzo (77,023 -> 58,854) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 93,197 contro 19,324, net/DD 4.82 contro 0.38; annullato dall'ultimo terzo (77,023 -> 58,854) |
| direzionale YES | PtnDirYes | 52 | -46 | · | batte lo spento: netto 54,218 contro 19,324, net/DD 3.17 contro 0.38; annullato dall'ultimo terzo (77,023 -> -11,057) |
| direzionale NO | PtnDirNo | 53 | 16 | ✔ | batte lo spento: netto 83,225 contro 19,324, net/DD 2.22 contro 0.38 |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 84,640 contro 83,225, net/DD 3.18 contro 2.22; annullato dall'ultimo terzo (77,349 -> 60,756) |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 101,573 contro 83,225, net/DD 3.49 contro 2.22 |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.5 | 0.5 | · | D2: netto smussato 92,628 (migliore 95,015), 779 trade |

**Storia di ricerca**: 1155 trade, netto 209,356, DD 29,143, average trade 181, UngerFit 0.87.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 1155 su almeno 50 |
| average trade | passa | 181 contro la soglia 150 |
| anni | passa | 13 anni, minimo 58 trade in un anno, 89.8 all'anno, 11 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 107,784 |
| ultimo terzo | passa | netto 108,109, net/DD 5.84 (serve 1,3) |
| due terzi su tre | passa | 48,021 / 53,552 / 108,109 |
| outlier | passa | trade migliore 6% del netto sulla storia, 12% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 288 con i pattern, 216 senza |
| pattern casuali | passa | 1 estrazioni su 4 fanno almeno altrettanto, p 0.40, Wilson 0.16 |
| plateau | passa | media 0.93 su 6 vicini, minimo 0.79 |

**Prova su FTMO**: 527 trade, netto 7,311, DD 66,026, net/DD 0.11, average trade 14 (soglia 190), UngerFit 0.04, finestre in utile 2/4 (32,023 / -16,220 / 20,364 / -28,856).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.11 (serve 1), average trade 14 (soglia 190), finestre 2/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 439 trade 22,686 net/DD 0.39, FTMO 416 trade 33,861 net/DD 0.51 |
| anni | passa | 14/19 anni in utile (serve 60%), anno migliore 24% del netto (massimo 40%); average trade / soglia per anno: 2008:1.0 2009:0.9 2010:-1.3 2011:2.7 2012:0.8 2013:0.2 2014:0.7 2015:2.3 2016:-0.7 2017:1.4 2018:1.4 2019:1.7 2020:3.6 2021:1.5 2022:2.9 2023:-1.3 2024:-2.0 2025:1.3 2026:-1.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=0, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=16, SkipDay=-1, Sessions=5, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

442 simulazioni in 71.9 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=0 | ✔ | nessuna in utile: netto massimo -120,328 |
| finestra: inizio | StartHour | -1 | 21 | · | avg smussato 2.2, netto 3,933, 812 trade; annullato dall'ultimo terzo (97,561 -> 35,573) |
| finestra: fine | EndHour | -1 | 10 | ✔ | nessuna in utile: netto massimo -112,606 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -17,151 (migliore -17,151), 2548 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -32,366; annullato dall'ultimo terzo (178,756 -> 169,947) |
| YES | PtnNeutYes | 55 | 11 | · | batte lo spento: netto 120,162 contro -59,187, net/DD 4.88 contro -0.49; annullato dall'ultimo terzo (178,756 -> 54,537) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 118,492 contro -59,187, net/DD 4.81 contro -0.49; annullato dall'ultimo terzo (178,756 -> 54,759) |
| direzionale YES | PtnDirYes | 52 | 23 | · | batte lo spento: netto 61,711 contro -59,187, net/DD 3.14 contro -0.49; annullato dall'ultimo terzo (178,756 -> 51,559) |
| direzionale NO | PtnDirNo | 53 | -27 | · | batte lo spento: netto 28,994 contro -59,187, net/DD 1.02 contro -0.49; annullato dall'ultimo terzo (178,756 -> 110,801) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 15,775 contro -59,187, net/DD 0.19 contro -0.49; annullato dall'ultimo terzo (178,756 -> 140,427) |
| uscita a ora fissa | ExitHour | -1 | 21 | ✔ | batte lo spento: netto 4,268 contro -59,187, net/DD 0.05 contro -0.49 |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 19,186 contro 4,268, net/DD 0.27 contro 0.05; annullato dall'ultimo terzo (181,677 -> 149,137) |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 42,332 (migliore 42,332), 2548 trade |

**Storia di ricerca**: 3867 trade, netto 185,511, DD 78,143, average trade 48, UngerFit 0.14.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 3867 su almeno 50 |
| average trade | **no** | 48 contro la soglia 150 |
| anni | **no** | 13 anni, minimo 251 trade in un anno, 300.8 all'anno, 6 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 181,243 |
| ultimo terzo | passa | netto 181,677, net/DD 4.23 (serve 1,3) |
| due terzi su tre | passa | -29,357 / 33,625 / 181,677 |
| outlier | passa | trade migliore 15% del netto sulla storia, 15% sull'ultimo terzo |
| plateau | passa | media 0.82 su 4 vicini, minimo 0.57 |

**Prova su FTMO**: 1946 trade, netto -24,603, DD 108,314, net/DD -0.23, average trade -13 (soglia 190), UngerFit , finestre in utile 3/4 (-62,322 / 20,762 / 7,563 / 7,379).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.23 (serve 1), average trade -13 (soglia 190), finestre 3/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 1550 trade -80,942 net/DD -0.43, FTMO 1529 trade -22,477 net/DD -0.21 |
| anni | **no** | 8/19 anni in utile (serve 60%), anno migliore 97% del netto (massimo 40%); average trade / soglia per anno: 2008:-0.6 2009:0.6 2010:-0.3 2011:-0.2 2012:-0.2 2013:-0.6 2014:0.9 2015:0.2 2016:0.6 2017:-0.2 2018:0.5 2019:-0.1 2020:2.3 2021:-0.1 2022:-0.6 2023:0.2 2024:-0.3 2025:0.8 2026:-0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=21, StartHour=-1, EndHour=10, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=0`

## VBO Volatility breakout

658 simulazioni in 63.9 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.3, Direction=1 | ✔ | avg smussato 11.9, netto 14,945, 1257 trade |
| finestra: inizio | StartHour | -1 | 10 | · | avg smussato 29.7, netto 37,313, 1155 trade; annullato dall'ultimo terzo (36,308 -> -31,554) |
| finestra: fine | EndHour | -1 | 10 | ✔ | avg smussato 33.5, netto 29,059, 826 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 39,023 (migliore 39,023), 826 trade; annullato dall'ultimo terzo (68,601 -> 30,510) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 35.2, netto 29,059, 826 trade |
| YES | PtnNeutYes | 55 | 33 | ✔ | batte lo spento: netto 55,625 contro 29,059, net/DD 2.09 contro 0.91 |
| NO | PtnNeutNo | 56 | 5 | · | batte lo spento: netto 55,373 contro 55,625, net/DD 5.21 contro 2.09; annullato dall'ultimo terzo (96,143 -> 28,800) |
| direzionale YES | PtnDirYes | 52 | 6 | · | batte lo spento: netto 58,396 contro 55,625, net/DD 4.11 contro 2.09; annullato dall'ultimo terzo (96,143 -> 53,001) |
| direzionale NO | PtnDirNo | 53 | -16 | · | batte lo spento: netto 78,845 contro 55,625, net/DD 6.99 contro 2.09; annullato dall'ultimo terzo (96,143 -> 72,282) |
| calendario | SkipDay | -1 | 3 | ✔ | batte lo spento: netto 56,762 contro 55,625, net/DD 3.31 contro 2.09 |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 67,709 contro 56,762, net/DD 4.24 contro 3.31 |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 86,778 contro 67,709, net/DD 7.95 contro 4.24; annullato dall'ultimo terzo (108,785 -> 104,437) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 64,435 (migliore 66,542), 183 trade; annullato dall'ultimo terzo (108,785 -> 78,676) |

**Storia di ricerca**: 274 trade, netto 176,494, DD 15,983, average trade 644, UngerFit 4.16.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 274 su almeno 50 |
| average trade | passa | 644 contro la soglia 150 |
| anni | **no** | 13 anni, minimo 3 trade in un anno, 21.3 all'anno, 11 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 108,785 |
| ultimo terzo | passa | netto 108,785, net/DD 8.06 (serve 1,3) |
| due terzi su tre | passa | 49,431 / 18,278 / 108,785 |
| outlier | passa | trade migliore 7% del netto sulla storia, 12% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 1,195 con i pattern, 297 senza |
| pattern casuali | passa | 1 estrazioni su 22 fanno almeno altrettanto, p 0.09, Wilson 0.04 |
| plateau | passa | media 0.69 su 4 vicini, minimo 0.34 |

**Prova su FTMO**: 161 trade, netto 40,113, DD 38,171, net/DD 1.05, average trade 249 (soglia 190), UngerFit 0.92, finestre in utile 2/4 (17,955 / -4,609 / -16,284 / 43,050).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.05 (serve 1), average trade 249 (soglia 190), finestre 2/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 99 trade 50,109 net/DD 1.79, FTMO 132 trade 8,005 net/DD 0.21 |
| anni | passa | 15/19 anni in utile (serve 60%), anno migliore 31% del netto (massimo 40%); average trade / soglia per anno: 2008:7.9 2009:5.2 2010:6.6 2011:-1.2 2012:3.0 2013:0.6 2014:8.9 2015:-0.2 2016:4.0 2017:12.9 2018:11.4 2019:4.8 2020:4.7 2021:4.4 2022:-1.1 2023:7.3 2024:-14.6 2025:3.0 2026:5.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=10, PtnNeutYes=33, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=3, AtrMultiplierLong=0.3, Direction=1`

## MAC Incrocio di medie

91 simulazioni in 9.4 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=15, SlowPeriod=30, Direction=2 | ✔ | avg smussato 116.7, netto 63,742, 546 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 78,433 (migliore 81,307), 546 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 98.6, netto 53,827, 546 trade |

**Abbandonato**: R9: la base non guadagna su tutta la storia (909 trade, netto -61,034).

## RBM Reversal Bollinger mirrored

673 simulazioni in 83.8 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | avg smussato 1.6, netto 874, 563 trade |
| finestra: inizio | StartHour | -1 | 9 | · | avg smussato 5.2, netto 10,985, 523 trade; annullato dall'ultimo terzo (25,031 -> 16,835) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 1.6, netto 874, 563 trade |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato 13,901 (migliore 14,261), 562 trade; annullato dall'ultimo terzo (25,031 -> 24,443) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 17 | ✔ | batte lo spento: netto 50,479 contro 874, net/DD 3.10 contro 0.04 |
| NO | PtnNeutNo | 56 | 22 | · | batte lo spento: netto 56,329 contro 50,479, net/DD 4.33 contro 3.10; annullato dall'ultimo terzo (31,624 -> 12,613) |
| direzionale YES | PtnDirYes | 52 | 44 | · | batte lo spento: netto 48,395 contro 50,479, net/DD 3.77 contro 3.10; annullato dall'ultimo terzo (31,624 -> 21,579) |
| direzionale NO | PtnDirNo | 53 | -5 | · | batte lo spento: netto 59,290 contro 50,479, net/DD 5.63 contro 3.10; annullato dall'ultimo terzo (31,624 -> -13,028) |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 143.5, netto 60,412, 421 trade; annullato dall'ultimo terzo (31,624 -> 24,992) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 53,390 (migliore 54,846), 421 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 120.3, netto 48,222, 412 trade; annullato dall'ultimo terzo (32,747 -> 15,103) |

**Storia di ricerca**: 654 trade, netto 82,971, DD 21,303, average trade 127, UngerFit 0.71.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 654 su almeno 50 |
| average trade | **no** | 127 contro la soglia 150 |
| anni | passa | 13 anni, minimo 35 trade in un anno, 50.9 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 32,747 |
| ultimo terzo | passa | netto 32,747, net/DD 1.59 (serve 1,3) |
| due terzi su tre | passa | 13,791 / 36,433 / 32,747 |
| outlier | **no** | trade migliore 14% del netto sulla storia, 33% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 141 con i pattern, 69 senza |
| pattern casuali | **no** | 1 estrazioni su 3 fanno almeno altrettanto, p 0.50, Wilson 0.20 |
| plateau | passa | media 0.62 su 6 vicini, minimo 0.16 |

**Prova su FTMO**: 348 trade, netto -131,176, DD 152,245, net/DD -0.86, average trade -377 (soglia 190), UngerFit , finestre in utile 0/4 (-62,239 / -34,016 / -24,938 / -9,984).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.86 (serve 1), average trade -377 (soglia 190), finestre 0/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 279 trade -126,659 net/DD -0.97, FTMO 272 trade -129,118 net/DD -0.98 |
| anni | **no** | 9/19 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2008:1.9 2009:0.5 2010:0.2 2011:-0.6 2012:1.1 2013:-0.7 2014:2.3 2015:3.4 2016:-0.6 2017:-1.3 2018:0.9 2019:-0.2 2020:1.7 2021:-2.2 2022:-4.7 2023:-1.1 2024:-0.3 2025:-2.7 2026:1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=17, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

969 simulazioni in 149.2 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | avg smussato 1.6, netto 874, 563 trade |
| finestra: inizio | StartHour | -1 | 9 | · | avg smussato 5.2, netto 10,985, 523 trade; annullato dall'ultimo terzo (25,031 -> 16,835) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 1.6, netto 874, 563 trade |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato 13,901 (migliore 14,261), 562 trade; annullato dall'ultimo terzo (25,031 -> 24,443) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES long | FastYesLong | 152 | 17 | · | batte lo spento: netto 44,587 contro 874, net/DD 2.91 contro 0.04; annullato dall'ultimo terzo (25,031 -> 14,787) |
| YES short | FastYesShort | 152 | 88 | ✔ | batte lo spento: netto 31,889 contro 874, net/DD 2.65 contro 0.04 |
| NO long | FastNoLong | 153 | 25 | · | batte lo spento: netto 59,556 contro 31,889, net/DD 7.48 contro 2.65; annullato dall'ultimo terzo (43,819 -> 25,501) |
| NO short | FastNoShort | 153 | 68 | · | batte lo spento: netto 33,552 contro 31,889, net/DD 2.79 contro 2.65; annullato dall'ultimo terzo (43,819 -> 43,677) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 34,392 contro 31,889, net/DD 3.94 contro 2.65 |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 127.9, netto 34,539, 270 trade; annullato dall'ultimo terzo (51,711 -> 48,971) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 37,827 (migliore 39,545), 270 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 144.7, netto 43,095, 267 trade; annullato dall'ultimo terzo (52,385 -> 44,152) |

**Storia di ricerca**: 430 trade, netto 87,112, DD 15,298, average trade 203, UngerFit 1.34.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 430 su almeno 50 |
| average trade | passa | 203 contro la soglia 150 |
| anni | passa | 13 anni, minimo 24 trade in un anno, 33.4 all'anno, 10 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 52,385 |
| ultimo terzo | passa | netto 52,385, net/DD 3.42 (serve 1,3) |
| due terzi su tre | passa | 20,968 / 13,760 / 52,385 |
| outlier | passa | trade migliore 12% del netto sulla storia, 20% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 327 con i pattern, 123 senza |
| pattern casuali | passa | 9 estrazioni su 78 fanno almeno altrettanto, p 0.13, Wilson 0.09 |
| plateau | passa | media 0.65 su 6 vicini, minimo 0.09 |

**Prova su FTMO**: 242 trade, netto -66,018, DD 91,342, net/DD -0.72, average trade -273 (soglia 190), UngerFit , finestre in utile 0/4 (-43,662 / -3,695 / -3,578 / -15,084).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.72 (serve 1), average trade -273 (soglia 190), finestre 0/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 196 trade -75,177 net/DD -0.98, FTMO 189 trade -74,916 net/DD -1.00 |
| anni | **no** | 12/19 anni in utile (serve 60%), anno migliore 139% del netto (massimo 40%); average trade / soglia per anno: 2008:1.3 2009:1.6 2010:0.4 2011:0.7 2012:-0.6 2013:1.6 2014:-0.5 2015:2.7 2016:1.8 2017:-2.3 2018:2.1 2019:0.9 2020:3.3 2021:-2.0 2022:-2.8 2023:0.3 2024:-0.7 2025:-3.8 2026:1.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastYesLong=152, FastYesShort=88, FastNoLong=153, FastNoShort=153, SkipDay=0, BbLength=50, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

673 simulazioni in 69.6 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=1 | ✔ | nessuna in utile: netto massimo -25,498 |
| finestra: inizio | StartHour | -1 | 22 | · | avg smussato -8.0, netto 17,824, 348 trade; annullato dall'ultimo terzo (54,769 -> 2,117) |
| finestra: fine | EndHour | -1 | 12 | · | nessuna in utile: netto massimo -1,968; annullato dall'ultimo terzo (54,769 -> 39,613) |
| stop | StopAtr | 0.8 | 3.0 | ✔ | D2: netto smussato -4,994 (migliore -4,994), 792 trade |
| target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 5,812 contro -4,994, net/DD 0.16 contro -0.13; annullato dall'ultimo terzo (63,705 -> 59,111) |
| YES | PtnNeutYes | 55 | 6 | · | batte lo spento: netto 67,773 contro -4,994, net/DD 4.71 contro -0.13; annullato dall'ultimo terzo (63,705 -> 26,810) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 59,399 contro -4,994, net/DD 3.37 contro -0.13; annullato dall'ultimo terzo (63,705 -> 29,139) |
| direzionale YES | PtnDirYes | 52 | -40 | · | batte lo spento: netto 45,517 contro -4,994, net/DD 4.13 contro -0.13; annullato dall'ultimo terzo (63,705 -> 20,861) |
| direzionale NO | PtnDirNo | 53 | 21 | ✔ | batte lo spento: netto 27,471 contro -4,994, net/DD 1.07 contro -0.13 |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 43,705 contro 27,471, net/DD 2.25 contro 1.07; annullato dall'ultimo terzo (70,682 -> 70,146) |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 90.9, netto 28,998, 319 trade; annullato dall'ultimo terzo (70,682 -> 68,467) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 3.0 | 2.0 | ✔ | D2: netto smussato 26,854 (migliore 27,471), 321 trade |
| affinamento target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 36,281 contro 26,236, net/DD 1.46 contro 1.02; annullato dall'ultimo terzo (70,682 -> 63,407) |
| durata | MaxBars | 4 | 6 | · | avg smussato 82.1, netto 26,873, 321 trade; annullato dall'ultimo terzo (70,682 -> 70,060) |

**Storia di ricerca**: 497 trade, netto 96,918, DD 25,611, average trade 195, UngerFit 0.99.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 497 su almeno 50 |
| average trade | passa | 195 contro la soglia 150 |
| anni | passa | 13 anni, minimo 26 trade in un anno, 38.7 all'anno, 9 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 70,682 |
| ultimo terzo | passa | netto 70,682, net/DD 5.88 (serve 1,3) |
| due terzi su tre | passa | 10,555 / 15,682 / 70,682 |
| outlier | passa | trade migliore 9% del netto sulla storia, 12% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 402 con i pattern, 160 senza |
| pattern casuali | passa | 2 estrazioni su 12 fanno almeno altrettanto, p 0.23, Wilson 0.11 |
| plateau | passa | media 0.74 su 5 vicini, minimo 0.08 |

**Prova su FTMO**: 302 trade, netto 36,060, DD 43,964, net/DD 0.82, average trade 119 (soglia 190), UngerFit 0.41, finestre in utile 3/4 (22,807 / 8,406 / 11,465 / -6,618).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.82 (serve 1), average trade 119 (soglia 190), finestre 3/4 (servono 3) |
| due feed | passa | 2020-11 → 2025-05: interno 220 trade 54,440 net/DD 3.35, FTMO 215 trade 63,180 net/DD 4.29 |
| anni | passa | 13/19 anni in utile (serve 60%), anno migliore 26% del netto (massimo 40%); average trade / soglia per anno: 2008:1.8 2009:-0.3 2010:-1.6 2011:2.6 2012:-0.7 2013:0.8 2014:0.3 2015:1.8 2016:-0.1 2017:0.4 2018:3.0 2019:0.7 2020:2.8 2021:1.7 2022:1.4 2023:1.1 2024:2.0 2025:-0.3 2026:-0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=2.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=21, SkipDay=-1, LevelOffsetTicks=20, Direction=1`

## LF Level fader sul pivot di ieri

509 simulazioni in 44.0 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=2 | ✔ | avg smussato 16.7, netto 14,552, 873 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 18,703 (migliore 18,703), 874 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 21 | · | batte lo spento: netto 38,295 contro 22,435, net/DD 4.39 contro 0.70; annullato dall'ultimo terzo (36,911 -> 3,334) |
| NO | PtnNeutNo | 56 | 15 | · | batte lo spento: netto 38,295 contro 22,435, net/DD 4.39 contro 0.70; annullato dall'ultimo terzo (36,911 -> 3,334) |
| direzionale YES | PtnDirYes | 52 | 3 | ✔ | batte lo spento: netto 56,674 contro 22,435, net/DD 6.45 contro 0.70 |
| calendario long | NotEntryDayLong | -1 | 2 | · | batte lo spento: netto 70,450 contro 56,674, net/DD 10.43 contro 6.45; annullato dall'ultimo terzo (52,873 -> 49,162) |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 171.7, netto 56,674, 330 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 53,635 (migliore 55,984), 330 trade |
| affinamento target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 58,100 contro 56,674, net/DD 6.62 contro 6.45 |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 188.5, netto 56,504, 313 trade |

**Storia di ricerca**: 482 trade, netto 114,372, DD 12,083, average trade 237, UngerFit 1.76.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 482 su almeno 50 |
| average trade | passa | 237 contro la soglia 150 |
| anni | passa | 13 anni, minimo 23 trade in un anno, 37.5 all'anno, 12 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 57,868 |
| ultimo terzo | passa | netto 57,868, net/DD 4.79 (serve 1,3) |
| due terzi su tre | passa | 7,179 / 49,325 / 57,868 |
| outlier | passa | trade migliore 6% del netto sulla storia, 13% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 342 con i pattern, 10 senza |
| pattern casuali | passa | 1 estrazioni su 24 fanno almeno altrettanto, p 0.08, Wilson 0.03 |
| plateau | passa | media 0.94 su 8 vicini, minimo 0.80 |

**Prova su FTMO**: 237 trade, netto -74,393, DD 81,259, net/DD -0.92, average trade -314 (soglia 190), UngerFit , finestre in utile 0/4 (-19,944 / -12,956 / -13,620 / -27,873).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.92 (serve 1), average trade -314 (soglia 190), finestre 0/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 160 trade -42,012 net/DD -0.76, FTMO 182 trade -68,216 net/DD -0.98 |
| anni | **no** | 13/19 anni in utile (serve 60%), anno migliore 97% del netto (massimo 40%); average trade / soglia per anno: 2008:-1.3 2009:0.8 2010:1.1 2011:0.8 2012:1.9 2013:0.0 2014:2.0 2015:1.9 2016:3.2 2017:0.1 2018:0.5 2019:0.9 2020:6.3 2021:-2.1 2022:-2.1 2023:-1.8 2024:-1.3 2025:-2.7 2026:0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=2.0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=3, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=2`

## LFHL Level fader sugli estremi di ieri

510 simulazioni in 43.4 minuti, conferma dal 2016-07-27.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=2 | ✔ | avg smussato 7.2, netto 7,422, 1038 trade |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 11,137 (migliore 11,137), 1038 trade; annullato dall'ultimo terzo (-31,768 -> -36,691) |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 8,848 contro 7,422, net/DD 0.20 contro 0.17 |
| YES | PtnNeutYes | 55 | 45 | ✔ | batte lo spento: netto 28,272 contro 8,848, net/DD 2.20 contro 0.20 |
| NO | PtnNeutNo | 56 | 29 | ✔ | batte lo spento: netto 30,779 contro 28,272, net/DD 3.84 contro 2.20 |
| direzionale YES | PtnDirYes | 52 | 6 | · | batte lo spento: netto 29,439 contro 30,779, net/DD 8.43 contro 3.84; annullato dall'ultimo terzo (6,062 -> 2,464) |
| calendario long | NotEntryDayLong | -1 | 2 | · | batte lo spento: netto 37,311 contro 30,779, net/DD 4.88 contro 3.84; annullato dall'ultimo terzo (6,062 -> 5,920) |
| calendario short | NotEntryDayShort | -1 | 4 | ✔ | batte lo spento: netto 29,307 contro 30,779, net/DD 4.01 contro 3.84 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 215.5, netto 29,307, 136 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 28,422 (migliore 29,249), 136 trade |
| affinamento target | TargetAtr | 2.0 | 2.0 | · | batte lo spento: netto 29,022 contro 27,596, net/DD 4.21 contro 4.00 |
| durata | MaxBars | 4 | 20 | ✔ | avg smussato 287.4, netto 34,153, 119 trade |

**Storia di ricerca**: 183 trade, netto 58,166, DD 17,080, average trade 318, UngerFit 1.99.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 183 su almeno 50 |
| average trade | passa | 318 contro la soglia 150 |
| anni | passa | 13 anni, minimo 7 trade in un anno, 14.2 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 24,014 |
| ultimo terzo | passa | netto 24,014, net/DD 1.41 (serve 1,3) |
| due terzi su tre | passa | 5,793 / 28,360 / 24,014 |
| outlier | **no** | trade migliore 13% del netto sulla storia, 32% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 375 con i pattern, 38 senza |
| pattern casuali | passa | 9 estrazioni su 51 fanno almeno altrettanto, p 0.19, Wilson 0.13 |
| plateau | passa | media 0.97 su 7 vicini, minimo 0.83 |

**Prova su FTMO**: 57 trade, netto -25,448, DD 44,184, net/DD -0.58, average trade -446 (soglia 190), UngerFit , finestre in utile 1/4 (-17,549 / -2,616 / -23,162 / 17,879).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.58 (serve 1), average trade -446 (soglia 190), finestre 1/4 (servono 3) |
| due feed | **no** | 2020-11 → 2025-05: interno 48 trade -34,512 net/DD -1.00, FTMO 42 trade -43,327 net/DD -0.98 |
| anni | **no** | 10/19 anni in utile (serve 60%), anno migliore 88% del netto (massimo 40%); average trade / soglia per anno: 2008:-0.2 2009:1.5 2010:-0.8 2011:1.3 2012:1.8 2013:-1.2 2014:-0.7 2015:7.6 2016:11.0 2017:0.3 2018:3.5 2019:6.6 2020:-1.6 2021:-1.9 2022:-4.3 2023:-8.3 2024:-9.4 2025:2.6 2026:4.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=2.0, MaxBars=20, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=45, PtnNeutNo=29, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=4, LevelShift=2`

## Riepilogo

- PCH: scartata; ricerca 162 trade netto 134,934 avg 833; prova 138 trade netto -14,155 net/DD -0.27 avg -103 fin 1/4; falliti: anni, prova sul broker, due feed, anni
- TFM: scartata; ricerca 708 trade netto 93,457 avg 132; prova 294 trade netto -18,113 net/DD -0.21 avg -62 fin 2/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, due feed, anni
- TFU: scartata; ricerca 92 trade netto 105,773 avg 1,150; prova 32 trade netto -25,221 net/DD -0.77 avg -788 fin 0/4; falliti: anni, outlier, prova sul broker, due feed, anni
- BO: scartata; ricerca 1155 trade netto 209,356 avg 181; prova 527 trade netto 7,311 net/DD 0.11 avg 14 fin 2/4; falliti: prova sul broker, due feed
- BOS: scartata; ricerca 3867 trade netto 185,511 avg 48; prova 1946 trade netto -24,603 net/DD -0.23 avg -13 fin 3/4; falliti: average trade, anni, prova sul broker, due feed, anni
- VBO: scartata; ricerca 274 trade netto 176,494 avg 644; prova 161 trade netto 40,113 net/DD 1.05 avg 249 fin 2/4; falliti: anni, prova sul broker, due feed
- MAC: abbandonato (R9: la base non guadagna su tutta la storia (909 trade, netto -61,034))
- RBM: scartata; ricerca 654 trade netto 82,971 avg 127; prova 348 trade netto -131,176 net/DD -0.86 avg -377 fin 0/4; falliti: average trade, outlier, pattern casuali, prova sul broker, due feed, anni
- RBU: scartata; ricerca 430 trade netto 87,112 avg 203; prova 242 trade netto -66,018 net/DD -0.72 avg -273 fin 0/4; falliti: prova sul broker, due feed, anni
- RHL: scartata; ricerca 497 trade netto 96,918 avg 195; prova 302 trade netto 36,060 net/DD 0.82 avg 119 fin 3/4; falliti: prova sul broker
- LF: scartata; ricerca 482 trade netto 114,372 avg 237; prova 237 trade netto -74,393 net/DD -0.92 avg -314 fin 0/4; falliti: prova sul broker, due feed, anni
- LFHL: scartata; ricerca 183 trade netto 58,166 avg 318; prova 57 trade netto -25,448 net/DD -0.58 avg -446 fin 1/4; falliti: outlier, prova sul broker, due feed, anni
