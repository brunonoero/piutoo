# Percorso v4 — @ES 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (22,540 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,260 barre), mai vista dal percorso
- costi FTMO: spread 0.60 punti (mediana), swap long 1.6673 short 0.002 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 84 nella ricerca, 114 nella prova

## PCH Price Channel

550 simulazioni in 35.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=40, OffsetTicks=2, Direction=1 | ✔ | avg smussato 182.2, netto 59,391, 326 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 315.9, netto 59,391, 326 trade |
| finestra: fine | EndHour | -1 | 19 | · | avg smussato 189.4, netto 57,808, 308 trade; annullato dall'ultimo terzo (-3,043 -> -3,592) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 56,187 (migliore 56,187), 326 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 172.4, netto 56,217, 326 trade |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 3 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 50,555 contro 56,217, net/DD 14.94 contro 8.87; annullato dall'ultimo terzo (18,800 -> 2,672) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 60,471 contro 56,217, net/DD 10.56 contro 8.87 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 63,415 contro 60,471, net/DD 11.07 contro 10.56 |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 62,270 (migliore 62,270), 264 trade |

**Storia di ricerca**: 415 trade, netto 85,027, DD 6,959, average trade 205, UngerFit 2.68.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 415 su almeno 50 |
| average trade | passa | 205 contro la soglia 84 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 108.9 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 21,612 |
| ultimo terzo | passa | netto 21,612, net/DD 3.11 (serve 1,3) |
| due terzi su tre | passa | 14,792 / 48,624 / 21,612 |
| outlier | passa | trade migliore 6% del netto sulla storia, 18% sull'ultimo terzo |
| plateau | passa | media 0.85 su 7 vicini, minimo 0.60 |

**Prova su FTMO**: 228 trade, netto -33,554, DD 39,385, net/DD -0.85, average trade -147 (soglia 114), UngerFit , finestre in utile 0/4 (-5,517 / -5,932 / -13,343 / -8,761).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.85 (serve 1), average trade -147 (soglia 114), finestre 0/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 78% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.2 2021:1.8 2022:3.4 2023:2.4 2024:1.0 2025:-1.5 2026:-1.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0, ChannelBars=40, OffsetTicks=2, Direction=1`

## TFM Trend following mirrored

437 simulazioni in 20.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 2 | ✔ | avg smussato 57.3, netto 38,845, 621 trade |
| finestra: fine | EndHour | -1 | 5 | ✔ | avg smussato 177.0, netto 44,667, 212 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 35,395 (migliore 35,395), 212 trade; annullato dall'ultimo terzo (32,395 -> 29,067) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 210.7, netto 44,667, 212 trade |
| YES | PtnNeutYes | 55 | 6 | · | batte lo spento: netto 45,954 contro 44,667, net/DD 5.56 contro 3.30; annullato dall'ultimo terzo (32,395 -> 15,355) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 45,954 contro 44,667, net/DD 5.56 contro 3.30; annullato dall'ultimo terzo (32,395 -> 15,355) |
| direzionale YES | PtnDirYes | 52 | 46 | · | batte lo spento: netto 43,262 contro 44,667, net/DD 6.11 contro 3.30; annullato dall'ultimo terzo (32,395 -> 16,963) |
| direzionale NO | PtnDirNo | 53 | 1 | · | batte lo spento: netto 45,070 contro 44,667, net/DD 5.07 contro 3.30; annullato dall'ultimo terzo (32,395 -> 21,239) |
| calendario | SkipDay | -1 | 3 | ✔ | batte lo spento: netto 56,304 contro 44,667, net/DD 4.89 contro 3.30 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 60,861 contro 56,304, net/DD 5.28 contro 4.89 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 55,420 (migliore 55,420), 169 trade; annullato dall'ultimo terzo (34,054 -> 33,625) |

**Storia di ricerca**: 236 trade, netto 94,915, DD 11,519, average trade 402, UngerFit 4.09.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 236 su almeno 50 |
| average trade | passa | 402 contro la soglia 84 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 61.9 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 34,054 |
| ultimo terzo | passa | netto 34,054, net/DD 7.50 (serve 1,3) |
| due terzi su tre | passa | 26,433 / 34,428 / 34,054 |
| outlier | passa | trade migliore 7% del netto sulla storia, 16% sull'ultimo terzo |
| plateau | passa | media 0.93 su 4 vicini, minimo 0.82 |

**Prova su FTMO**: 141 trade, netto -31,859, DD 52,445, net/DD -0.61, average trade -226 (soglia 114), UngerFit , finestre in utile 1/4 (-18,908 / -999 / -10,833 / 2,231).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.61 (serve 1), average trade -226 (soglia 114), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2020:-11.0 2021:3.1 2022:5.4 2023:2.5 2024:4.8 2025:-1.4 2026:-2.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=2, EndHour=5, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=3`

## TFU Trend following unmirrored

731 simulazioni in 29.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 2 | ✔ | avg smussato 57.3, netto 38,845, 621 trade |
| finestra: fine | EndHour | -1 | 5 | ✔ | avg smussato 177.0, netto 44,667, 212 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 35,395 (migliore 35,395), 212 trade; annullato dall'ultimo terzo (32,395 -> 29,067) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 210.7, netto 44,667, 212 trade |
| YES long | FastYesLong | 152 | 10 | · | batte lo spento: netto 45,763 contro 44,667, net/DD 3.54 contro 3.30; annullato dall'ultimo terzo (32,395 -> 30,273) |
| YES short | FastYesShort | 152 | 107 | · | batte lo spento: netto 60,923 contro 44,667, net/DD 5.12 contro 3.30; annullato dall'ultimo terzo (32,395 -> 14,300) |
| NO long | FastNoLong | 153 | 16 | · | batte lo spento: netto 45,763 contro 44,667, net/DD 3.54 contro 3.30; annullato dall'ultimo terzo (32,395 -> 30,273) |
| NO short | FastNoShort | 153 | 106 | · | batte lo spento: netto 60,923 contro 44,667, net/DD 5.12 contro 3.30; annullato dall'ultimo terzo (32,395 -> 14,300) |
| calendario | SkipDay | -1 | 3 | ✔ | batte lo spento: netto 56,304 contro 44,667, net/DD 4.89 contro 3.30 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 60,861 contro 56,304, net/DD 5.28 contro 4.89 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 55,420 (migliore 55,420), 169 trade; annullato dall'ultimo terzo (34,054 -> 33,625) |

**Storia di ricerca**: 236 trade, netto 94,915, DD 11,519, average trade 402, UngerFit 4.09.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 236 su almeno 50 |
| average trade | passa | 402 contro la soglia 84 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 61.9 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 34,054 |
| ultimo terzo | passa | netto 34,054, net/DD 7.50 (serve 1,3) |
| due terzi su tre | passa | 26,433 / 34,428 / 34,054 |
| outlier | passa | trade migliore 7% del netto sulla storia, 16% sull'ultimo terzo |
| plateau | passa | media 0.93 su 4 vicini, minimo 0.82 |

**Prova su FTMO**: 141 trade, netto -31,859, DD 52,445, net/DD -0.61, average trade -226 (soglia 114), UngerFit , finestre in utile 1/4 (-18,908 / -999 / -10,833 / 2,231).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.61 (serve 1), average trade -226 (soglia 114), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2020:-11.0 2021:3.1 2022:5.4 2023:2.5 2024:4.8 2025:-1.4 2026:-2.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=2, EndHour=5, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=3`

## BO Breakout di N sessioni

512 simulazioni in 56.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=5, IncludeCurrentSession=1 | ✔ | avg smussato 61.2, netto 36,746, 600 trade |
| finestra: inizio | StartHour | -1 | 16 | ✔ | avg smussato 103.6, netto 44,727, 389 trade |
| finestra: fine | EndHour | -1 | 2 | · | avg smussato 139.5, netto 69,396, 470 trade; annullato dall'ultimo terzo (20,696 -> 19,647) |
| stop | StopAtr | 0.8 | 1.5 | · | D2: netto smussato 49,863 (migliore 51,013), 386 trade; annullato dall'ultimo terzo (20,696 -> 19,314) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 116.3, netto 45,234, 389 trade |
| YES | PtnNeutYes | 55 | 11 | · | batte lo spento: netto 42,057 contro 45,234, net/DD 5.60 contro 3.87; annullato dall'ultimo terzo (22,338 -> 12,090) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 42,057 contro 45,234, net/DD 5.60 contro 3.87; annullato dall'ultimo terzo (22,338 -> 11,827) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 53,356 contro 45,234, net/DD 11.28 contro 3.87; annullato dall'ultimo terzo (22,338 -> 15,779) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 56,234 contro 45,234, net/DD 8.56 contro 3.87; annullato dall'ultimo terzo (22,338 -> 10,054) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 61,897 contro 45,234, net/DD 6.65 contro 3.87; annullato dall'ultimo terzo (22,338 -> 21,887) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 48,552 contro 45,234, net/DD 4.15 contro 3.87 |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 48,494 (migliore 48,494), 389 trade |

**Storia di ricerca**: 597 trade, netto 71,664, DD 14,941, average trade 120, UngerFit 1.07.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 597 su almeno 50 |
| average trade | passa | 120 contro la soglia 84 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 156.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 25,192 |
| ultimo terzo | passa | netto 25,192, net/DD 3.85 (serve 1,3) |
| due terzi su tre | passa | 785 / 45,688 / 25,192 |
| outlier | passa | trade migliore 7% del netto sulla storia, 12% sull'ultimo terzo |
| plateau | passa | media 0.95 su 7 vicini, minimo 0.79 |

**Prova su FTMO**: 318 trade, netto -21,076, DD 38,424, net/DD -0.55, average trade -66 (soglia 114), UngerFit , finestre in utile 1/4 (-9,130 / -471 / -20,882 / 9,407).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.55 (serve 1), average trade -66 (soglia 114), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2020:-2.2 2021:0.4 2022:1.8 2023:1.1 2024:1.6 2025:-1.4 2026:0.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=1.0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=16, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=1, BreakoutOffsetTicks=5`

## BOS Breakout della sessione in corso

642 simulazioni in 58.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=0 | ✔ | avg smussato 43.8, netto 32,474, 741 trade |
| finestra: inizio | StartHour | -1 | 11 | · | avg smussato 91.4, netto 74,808, 687 trade; annullato dall'ultimo terzo (13,387 -> 8,439) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 43.8, netto 32,474, 741 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 26,719 (migliore 26,719), 702 trade; annullato dall'ultimo terzo (13,387 -> -4,640) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 48.7, netto 36,114, 741 trade |
| YES | PtnNeutYes | 55 | 11 | · | batte lo spento: netto 104,440 contro 36,114, net/DD 6.44 contro 0.89; annullato dall'ultimo terzo (14,596 -> -2,667) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 104,440 contro 36,114, net/DD 6.44 contro 0.89; annullato dall'ultimo terzo (14,596 -> -1,532) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 4 | ✔ | batte lo spento: netto 72,271 contro 36,114, net/DD 1.80 contro 0.89 |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 73,524 contro 72,271, net/DD 2.17 contro 1.80; annullato dall'ultimo terzo (25,015 -> 15,640) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 91,390 contro 72,271, net/DD 2.47 contro 1.80 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 76,645 (migliore 76,645), 644 trade; annullato dall'ultimo terzo (31,443 -> 15,143) |

**Storia di ricerca**: 997 trade, netto 122,833, DD 36,979, average trade 123, UngerFit 0.70.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 997 su almeno 50 |
| average trade | passa | 123 contro la soglia 84 |
| anni | passa | 5 anni, minimo 20 trade in un anno, 261.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 31,443 |
| ultimo terzo | passa | netto 31,443, net/DD 1.48 (serve 1,3) |
| due terzi su tre | passa | 11,923 / 79,417 / 31,443 |
| outlier | passa | trade migliore 8% del netto sulla storia, 29% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 92 con i pattern, 52 senza |
| pattern casuali | passa | 1 estrazioni su 4 fanno almeno altrettanto, p 0.40, Wilson 0.16 |
| plateau | passa | media 0.77 su 5 vicini, minimo 0.69 |

**Prova su FTMO**: 516 trade, netto 6,667, DD 53,942, net/DD 0.12, average trade 13 (soglia 114), UngerFit 0.05, finestre in utile 1/4 (31,135 / -8,095 / -7,544 / -5,479).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.12 (serve 1), average trade 13 (soglia 114), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 59% del netto (massimo 40%); average trade / soglia per anno: 2020:5.1 2021:-0.7 2022:2.4 2023:1.4 2024:2.2 2025:-0.2 2026:-0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=4, SkipDay=-1, BreakoutOffsetTicks=0`

## VBO Volatility breakout

547 simulazioni in 27.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.7, Direction=2 | ✔ | avg smussato 61.1, netto 12,462, 204 trade |
| finestra: inizio | StartHour | -1 | 2 | · | avg smussato 70.3, netto 14,577, 202 trade; annullato dall'ultimo terzo (6,087 -> 5,440) |
| finestra: fine | EndHour | -1 | 23 | ✔ | avg smussato 157.7, netto 12,462, 204 trade |
| stop | StopAtr | 0.8 | 0.4 | ✔ | D2: netto smussato 12,309 (migliore 12,309), 204 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 59.0, netto 12,043, 204 trade |
| YES | PtnNeutYes | 55 | 5 | · | batte lo spento: netto 20,537 contro 12,043, net/DD 1.44 contro 0.68; annullato dall'ultimo terzo (6,799 -> 4,564) |
| NO | PtnNeutNo | 56 | 2 | · | batte lo spento: netto 20,537 contro 12,043, net/DD 1.44 contro 0.68; annullato dall'ultimo terzo (6,799 -> 4,564) |
| direzionale YES | PtnDirYes | 52 | -12 | ✔ | batte lo spento: netto 24,468 contro 12,043, net/DD 4.04 contro 0.68 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 35,110 contro 24,468, net/DD 8.46 contro 4.04 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.4 | 0.3 | · | D2: netto smussato 35,531 (migliore 37,190), 64 trade; annullato dall'ultimo terzo (15,502 -> 13,359) |

**Storia di ricerca**: 98 trade, netto 50,612, DD 4,679, average trade 516, UngerFit 8.25.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 98 su almeno 50 |
| average trade | passa | 516 contro la soglia 84 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 25.7 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 15,502 |
| ultimo terzo | passa | netto 15,502, net/DD 4.52 (serve 1,3) |
| due terzi su tre | passa | 12,630 / 22,480 / 15,502 |
| outlier | passa | trade migliore 14% del netto sulla storia, 21% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 456 con i pattern, 133 senza |
| pattern casuali | passa | 0 estrazioni su 16 fanno almeno altrettanto, p 0.06, Wilson 0.02 |
| plateau | passa | media 0.78 su 4 vicini, minimo 0.51 |

**Prova su FTMO**: 54 trade, netto 3,397, DD 13,401, net/DD 0.25, average trade 63 (soglia 114), UngerFit 0.51, finestre in utile 3/4 (5,774 / -7,942 / 4,440 / 1,125).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.25 (serve 1), average trade 63 (soglia 114), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 44% del netto (massimo 40%); average trade / soglia per anno: 2020:4.2 2021:5.1 2022:6.4 2023:3.6 2024:6.9 2025:0.5 2026:-0.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.4, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=23, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=-12, PtnDirNo=53, SkipDay=0, AtrMultiplierLong=0.7, Direction=2`

## MAC Incrocio di medie

125 simulazioni in 5.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=10, SlowPeriod=150, Direction=0 | ✔ | avg smussato 282.7, netto 41,280, 146 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 48,775 (migliore 48,775), 155 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 301.6, netto 46,754, 155 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 50,841 contro 46,754, net/DD 8.41 contro 7.74 |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 52,862 (migliore 52,862), 155 trade |

**Storia di ricerca**: 234 trade, netto 57,860, DD 9,837, average trade 247, UngerFit 2.72.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 234 su almeno 50 |
| average trade | passa | 247 contro la soglia 84 |
| anni | passa | 5 anni, minimo 6 trade in un anno, 61.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 7,019 |
| ultimo terzo | **no** | netto 7,019, net/DD 0.71 (serve 1,3) |
| due terzi su tre | passa | 23,463 / 27,378 / 7,019 |
| outlier | **no** | trade migliore 11% del netto sulla storia, 61% sull'ultimo terzo |
| plateau | **no** | media 0.54 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 126 trade, netto 3,528, DD 14,335, net/DD 0.25, average trade 28 (soglia 114), UngerFit 0.22, finestre in utile 2/4 (9,772 / -6,080 / 3,528 / -3,691).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.25 (serve 1), average trade 28 (soglia 114), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 57% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.8 2021:4.1 2022:4.7 2023:1.2 2024:2.4 2025:0.4 2026:-2.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=10, SlowPeriod=150, Direction=0`

## RBM Reversal Bollinger mirrored

668 simulazioni in 36.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -5,997 |
| finestra: inizio | StartHour | -1 | 15 | · | avg smussato -37.1, netto 3,714, 162 trade; annullato dall'ultimo terzo (-12,602 -> -14,347) |
| finestra: fine | EndHour | -1 | 17 | ✔ | avg smussato 113.1, netto 20,880, 145 trade |
| stop | StopAtr | 0.8 | 2.5 | ✔ | D2: netto smussato 23,203 (migliore 23,331), 145 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 26,867 contro 23,331, net/DD 3.73 contro 3.24; annullato dall'ultimo terzo (-3,648 -> -5,710) |
| YES | PtnNeutYes | 55 | 32 | ✔ | batte lo spento: netto 18,124 contro 23,331, net/DD 5.00 contro 3.24 |
| NO | PtnNeutNo | 56 | 48 | ✔ | batte lo spento: netto 19,159 contro 18,124, net/DD 6.35 contro 5.00 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 309.0, netto 19,159, 62 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 2.5 | 2.5 | · | D2: netto smussato 18,547 (migliore 19,159), 62 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | · | avg smussato 408.4, netto 25,319, 62 trade; annullato dall'ultimo terzo (8,196 -> 7,199) |

**Storia di ricerca**: 95 trade, netto 27,355, DD 5,178, average trade 288, UngerFit 4.37.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 95 su almeno 50 |
| average trade | passa | 288 contro la soglia 84 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 24.9 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 8,196 |
| ultimo terzo | passa | netto 8,196, net/DD 1.58 (serve 1,3) |
| due terzi su tre | passa | 8,469 / 10,690 / 8,196 |
| outlier | **no** | trade migliore 17% del netto sulla storia, 55% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 248 con i pattern, -41 senza |
| pattern casuali | passa | 0 estrazioni su 13 fanno almeno altrettanto, p 0.07, Wilson 0.02 |
| plateau | **no** | media 0.44 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 63 trade, netto 17,954, DD 9,424, net/DD 1.91, average trade 285 (soglia 114), UngerFit 2.75, finestre in utile 3/4 (-3,325 / 15,317 / 2,025 / 3,936).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 1.91 (serve 1), average trade 285 (soglia 114), finestre 3/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 27% del netto (massimo 40%); average trade / soglia per anno: 2020:-28.6 2021:2.8 2022:3.3 2023:5.2 2024:2.4 2025:3.1 2026:2.0 |
| altri mercati | **no** | 2/7 con net/DD ≥ 1 (serve meta'): @FDAX -0.64, @NQ -0.42, @YM 1.85, @FESX -0.72, @FCE -0.87, @Z 1.71, @NIY -0.61 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=2.5, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=17, PtnNeutYes=32, PtnNeutNo=48, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

964 simulazioni in 53.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -5,997 |
| finestra: inizio | StartHour | -1 | 15 | · | avg smussato -37.1, netto 3,714, 162 trade; annullato dall'ultimo terzo (-12,602 -> -14,347) |
| finestra: fine | EndHour | -1 | 17 | ✔ | avg smussato 113.1, netto 20,880, 145 trade |
| stop | StopAtr | 0.8 | 2.5 | ✔ | D2: netto smussato 23,203 (migliore 23,331), 145 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 26,867 contro 23,331, net/DD 3.73 contro 3.24; annullato dall'ultimo terzo (-3,648 -> -5,710) |
| YES long | FastYesLong | 152 | 33 | ✔ | batte lo spento: netto 33,192 contro 23,331, net/DD 6.71 contro 3.24 |
| YES short | FastYesShort | 152 | 152 | · | nessuno dei 3 candidati batte lo spento |
| NO long | FastNoLong | 153 | 129 | · | batte lo spento: netto 33,681 contro 33,192, net/DD 20.69 contro 6.71; annullato dall'ultimo terzo (-1,830 -> -8,654) |
| NO short | FastNoShort | 153 | 153 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 29,443 contro 33,192, net/DD 7.41 contro 6.71; annullato dall'ultimo terzo (-1,830 -> -4,528) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 377.2, netto 33,192, 88 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 2.5 | 2.5 | · | D2: netto smussato 33,192 (migliore 33,192), 88 trade |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 36,728 contro 33,192, net/DD 7.43 contro 6.71; annullato dall'ultimo terzo (-1,830 -> -7,488) |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 470.1, netto 40,426, 86 trade |

**Storia di ricerca**: 143 trade, netto 39,910, DD 10,941, average trade 279, UngerFit 2.91.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 143 su almeno 50 |
| average trade | passa | 279 contro la soglia 84 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 37.5 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -517 |
| ultimo terzo | **no** | netto -517, net/DD -0.05 (serve 1,3) |
| due terzi su tre | passa | 22,421 / 18,006 / -517 |
| outlier | passa | trade migliore 17% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -9 con i pattern, -147 senza |
| pattern casuali | **no** | 17 estrazioni su 64 fanno almeno altrettanto, p 0.28, Wilson 0.21 |
| plateau | **no** | media 0.55 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 83 trade, netto -11,010, DD 21,654, net/DD -0.51, average trade -133 (soglia 114), UngerFit , finestre in utile 2/4 (-20,915 / 8,790 / 1,508 / -394).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.51 (serve 1), average trade -133 (soglia 114), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 57% del netto (massimo 40%); average trade / soglia per anno: 2020:-3.9 2021:4.9 2022:3.5 2023:3.1 2024:-1.9 2025:-0.3 2026:0.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=2.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=17, FastYesLong=33, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

670 simulazioni in 32.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=10, Direction=1 | ✔ | nessuna in utile: netto massimo -24,878 |
| finestra: inizio | StartHour | -1 | 16 | · | avg smussato -1.6, netto 9,042, 228 trade; annullato dall'ultimo terzo (4,613 -> 1,082) |
| finestra: fine | EndHour | -1 | 2 | · | avg smussato 71.0, netto 5,312, 76 trade; annullato dall'ultimo terzo (4,613 -> -2,747) |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -18,638 (migliore -18,638), 267 trade; annullato dall'ultimo terzo (4,613 -> -266) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 24 | · | batte lo spento: netto 7,059 contro -24,878, net/DD 0.56 contro -0.74; annullato dall'ultimo terzo (4,613 -> -1,231) |
| NO | PtnNeutNo | 56 | 28 | · | batte lo spento: netto 7,059 contro -24,878, net/DD 0.56 contro -0.74; annullato dall'ultimo terzo (4,613 -> -1,231) |
| direzionale YES | PtnDirYes | 52 | 30 | · | batte lo spento: netto 11,227 contro -24,878, net/DD 2.25 contro -0.74; annullato dall'ultimo terzo (4,613 -> 3,025) |
| direzionale NO | PtnDirNo | 53 | 1 | ✔ | batte lo spento: netto 13,001 contro -24,878, net/DD 1.20 contro -0.74 |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 14,401 contro 13,001, net/DD 1.51 contro 1.20 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 165.5, netto 14,401, 87 trade |
| uscita a ora fissa | ExitHour | -1 | 21 | ✔ | batte lo spento: netto 15,610 contro 14,401, net/DD 1.91 contro 1.51 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 15,231 (migliore 15,743), 85 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 4 | · | avg smussato 126.5, netto 15,610, 85 trade |

**Storia di ricerca**: 128 trade, netto 26,177, DD 8,192, average trade 205, UngerFit 2.47.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 128 su almeno 50 |
| average trade | passa | 205 contro la soglia 84 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 33.6 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 10,567 |
| ultimo terzo | passa | netto 10,567, net/DD 3.18 (serve 1,3) |
| due terzi su tre | passa | 2,238 / 13,372 / 10,567 |
| outlier | **no** | trade migliore 15% del netto sulla storia, 38% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 246 con i pattern, 7 senza |
| pattern casuali | passa | 1 estrazioni su 16 fanno almeno altrettanto, p 0.12, Wilson 0.05 |
| plateau | passa | media 0.83 su 8 vicini, minimo 0.51 |

**Prova su FTMO**: 50 trade, netto 15,688, DD 2,895, net/DD 5.42, average trade 314 (soglia 114), UngerFit 5.46, finestre in utile 3/4 (1,515 / 6,764 / 9,440 / -2,031).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 5.42 (serve 1), average trade 314 (soglia 114), finestre 3/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 38% del netto (massimo 40%); average trade / soglia per anno: 2020:5.1 2021:1.0 2022:1.1 2023:5.4 2024:0.6 2025:4.0 2026:2.4 |
| altri mercati | passa | 5/7 con net/DD ≥ 1 (serve meta'): @FDAX 0.44, @NQ 2.28, @YM 1.51, @FESX 1.94, @FCE 1.03, @Z 1.24, @NIY 0.38 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=21, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=1, SkipDay=0, LevelOffsetTicks=10, Direction=1`

## LF Level fader sul pivot di ieri

305 simulazioni in 14.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | avg smussato 67.2, netto 15,122, 225 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 15,539 (migliore 15,640), 226 trade; annullato dall'ultimo terzo (14,517 -> 13,735) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 3 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 49 | · | batte lo spento: netto 24,448 contro 15,122, net/DD 7.51 contro 1.84; annullato dall'ultimo terzo (14,517 -> 10,870) |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 18,388 contro 15,122, net/DD 2.11 contro 1.84; annullato dall'ultimo terzo (14,517 -> 7) |
| direzionale YES | PtnDirYes | 52 | 44 | · | batte lo spento: netto 24,621 contro 15,122, net/DD 4.90 contro 1.84; annullato dall'ultimo terzo (14,517 -> 13,744) |
| calendario long | NotEntryDayLong | -1 | 1 | ✔ | batte lo spento: netto 23,129 contro 15,122, net/DD 2.80 contro 1.84 |
| calendario short | NotEntryDayShort | -1 | 1 | ✔ | batte lo spento: netto 31,239 contro 23,129, net/DD 5.44 contro 2.80 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 184.8, netto 31,239, 169 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 30,668 (migliore 31,239), 169 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 20 | · | avg smussato 213.0, netto 30,918, 146 trade; annullato dall'ultimo terzo (15,624 -> 13,471) |

**Storia di ricerca**: 247 trade, netto 46,863, DD 6,091, average trade 190, UngerFit 2.65.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 247 su almeno 50 |
| average trade | passa | 190 contro la soglia 84 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 64.8 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 15,624 |
| ultimo terzo | passa | netto 15,624, net/DD 2.57 (serve 1,3) |
| due terzi su tre | passa | 12,672 / 18,567 / 15,624 |
| outlier | passa | trade migliore 9% del netto sulla storia, 28% sull'ultimo terzo |
| plateau | passa | media 0.70 su 5 vicini, minimo 0.36 |

**Prova su FTMO**: 176 trade, netto -13,651, DD 26,489, net/DD -0.52, average trade -78 (soglia 114), UngerFit , finestre in utile 1/4 (10,609 / -7,067 / -2,190 / -15,003).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.52 (serve 1), average trade -78 (soglia 114), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 49% del netto (massimo 40%); average trade / soglia per anno: 2020:0.1 2021:1.8 2022:1.4 2023:3.7 2024:2.2 2025:0.0 2026:-2.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=1, NotEntryDayShort=1, LevelShift=20`

## LFHL Level fader sugli estremi di ieri

305 simulazioni in 14.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -10,306 |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato -5,182 (migliore -5,182), 368 trade; annullato dall'ultimo terzo (24,607 -> 22,311) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 2,433 contro -10,306, net/DD 0.09 contro -0.30; annullato dall'ultimo terzo (24,607 -> 16,580) |
| YES | PtnNeutYes | 55 | 49 | · | batte lo spento: netto 22,548 contro -10,306, net/DD 5.84 contro -0.30; annullato dall'ultimo terzo (24,607 -> 11,724) |
| NO | PtnNeutNo | 56 | 5 | · | batte lo spento: netto 11,770 contro -10,306, net/DD 2.93 contro -0.30; annullato dall'ultimo terzo (24,607 -> 15,036) |
| direzionale YES | PtnDirYes | 52 | -8 | · | batte lo spento: netto 19,951 contro -10,306, net/DD 5.45 contro -0.30; annullato dall'ultimo terzo (24,607 -> 7,051) |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -10,306 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato -5,540 (migliore -5,540), 368 trade; annullato dall'ultimo terzo (24,607 -> 22,311) |
| affinamento target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 2,433 contro -10,306, net/DD 0.09 contro -0.30; annullato dall'ultimo terzo (24,607 -> 16,580) |
| durata | MaxBars | 4 | 4 | · | nessuna in utile |

**Storia di ricerca**: 524 trade, netto 14,301, DD 34,750, average trade 27, UngerFit 0.16.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 524 su almeno 50 |
| average trade | **no** | 27 contro la soglia 84 |
| anni | passa | 5 anni, minimo 12 trade in un anno, 137.5 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 24,607 |
| ultimo terzo | passa | netto 24,607, net/DD 5.63 (serve 1,3) |
| due terzi su tre | passa | 8,923 / -19,229 / 24,607 |
| outlier | **no** | trade migliore 33% del netto sulla storia, 19% sull'ultimo terzo |
| plateau | passa | media 0.70 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 311 trade, netto -21,677, DD 26,363, net/DD -0.82, average trade -70 (soglia 114), UngerFit , finestre in utile 0/4 (-479 / -4,717 / -2,354 / -14,128).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.82 (serve 1), average trade -70 (soglia 114), finestre 0/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:2.6 2021:1.3 2022:-1.2 2023:1.8 2024:1.2 2025:-0.1 2026:-1.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=20`

## Riepilogo

- PCH: scartata; ricerca 415 trade netto 85,027 avg 205; prova 228 trade netto -33,554 net/DD -0.85 avg -147 fin 0/4; falliti: prova sul broker, anni
- TFM: scartata; ricerca 236 trade netto 94,915 avg 402; prova 141 trade netto -31,859 net/DD -0.61 avg -226 fin 1/4; falliti: prova sul broker, anni
- TFU: scartata; ricerca 236 trade netto 94,915 avg 402; prova 141 trade netto -31,859 net/DD -0.61 avg -226 fin 1/4; falliti: prova sul broker, anni
- BO: scartata; ricerca 597 trade netto 71,664 avg 120; prova 318 trade netto -21,076 net/DD -0.55 avg -66 fin 1/4; falliti: prova sul broker, anni
- BOS: scartata; ricerca 997 trade netto 122,833 avg 123; prova 516 trade netto 6,667 net/DD 0.12 avg 13 fin 1/4; falliti: prova sul broker, anni
- VBO: scartata; ricerca 98 trade netto 50,612 avg 516; prova 54 trade netto 3,397 net/DD 0.25 avg 63 fin 3/4; falliti: anni, prova sul broker, anni
- MAC: scartata; ricerca 234 trade netto 57,860 avg 247; prova 126 trade netto 3,528 net/DD 0.25 avg 28 fin 2/4; falliti: ultimo terzo, outlier, plateau, prova sul broker, anni
- RBM: scartata; ricerca 95 trade netto 27,355 avg 288; prova 63 trade netto 17,954 net/DD 1.91 avg 285 fin 3/4; falliti: anni, outlier, plateau, altri mercati
- RBU: scartata; ricerca 143 trade netto 39,910 avg 279; prova 83 trade netto -11,010 net/DD -0.51 avg -133 fin 2/4; falliti: anni, utile recente, ultimo terzo, pattern casuali, plateau, prova sul broker, anni
- RHL: scartata; ricerca 128 trade netto 26,177 avg 205; prova 50 trade netto 15,688 net/DD 5.42 avg 314 fin 3/4; falliti: anni, outlier
- LF: scartata; ricerca 247 trade netto 46,863 avg 190; prova 176 trade netto -13,651 net/DD -0.52 avg -78 fin 1/4; falliti: prova sul broker, anni
- LFHL: scartata; ricerca 524 trade netto 14,301 avg 27; prova 311 trade netto -21,677 net/DD -0.82 avg -70 fin 0/4; falliti: average trade, outlier, prova sul broker, anni
