# Percorso v4 — @FESX 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (19,892 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (7,972 barre), mai vista dal percorso
- costi FTMO: spread 1.46 punti (mediana), swap long 1.1327 short 0.0114 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 60 nella ricerca, 60 nella prova

## PCH Price Channel

649 simulazioni in 17.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=75, OffsetTicks=5, Direction=1 | ✔ | avg smussato 10.5, netto 1,661, 158 trade |
| finestra: inizio | StartHour | -1 | 4 | ✔ | avg smussato 19.0, netto 2,984, 153 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 30.7, netto 2,356, 83 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 3,587 (migliore 3,587), 83 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 46.2, netto 3,836, 83 trade |
| YES | PtnNeutYes | 55 | 31 | · | batte lo spento: netto 4,100 contro 3,836, net/DD 2.78 contro 2.52; annullato dall'ultimo terzo (-420 -> -678) |
| NO | PtnNeutNo | 56 | 38 | · | batte lo spento: netto 4,100 contro 3,836, net/DD 2.78 contro 2.52; annullato dall'ultimo terzo (-420 -> -678) |
| direzionale YES | PtnDirYes | 52 | 15 | ✔ | batte lo spento: netto 5,034 contro 3,836, net/DD 7.59 contro 2.52 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 5,368 contro 5,034, net/DD 8.10 contro 7.59 |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 5,263 (migliore 5,263), 56 trade |

**Storia di ricerca**: 81 trade, netto 5,978, DD 904, average trade 74, UngerFit 3.17.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 81 su almeno 50 |
| average trade | passa | 74 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 21.3 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 610 |
| ultimo terzo | **no** | netto 610, net/DD 0.67 (serve 1,3) |
| due terzi su tre | passa | 845 / 4,523 / 610 |
| outlier | **no** | trade migliore 14% del netto sulla storia, 86% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 24 con i pattern, -8 senza |
| pattern casuali | **no** | 3 estrazioni su 9 fanno almeno altrettanto, p 0.40, Wilson 0.22 |
| plateau | passa | media 0.83 su 7 vicini, minimo 0.64 |

**Prova su FTMO**: 36 trade, netto -446, DD 1,835, net/DD -0.24, average trade -12 (soglia 60), UngerFit , finestre in utile 1/4 (-378 / 1,418 / -1,260 / -226).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.24 (serve 1), average trade -12 (soglia 60), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 65% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.9 2021:0.9 2022:2.5 2023:0.8 2024:0.3 2025:0.6 2026:-1.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=4, EndHour=9, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=15, PtnDirNo=53, SkipDay=-1, ChannelBars=75, OffsetTicks=5, Direction=1`

## TFM Trend following mirrored

394 simulazioni in 16.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 15 | · | nessuna in utile: netto massimo -1,300; annullato dall'ultimo terzo (4,058 -> -2,105) |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 14.1, netto 2,358, 83 trade; annullato dall'ultimo terzo (4,058 -> -156) |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato -12,207 (migliore -12,207), 616 trade; annullato dall'ultimo terzo (4,058 -> 1,128) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -11,028 |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 8,143 contro -11,028, net/DD 4.43 contro -0.77; annullato dall'ultimo terzo (4,058 -> 2,875) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 1,694 contro -11,028, net/DD 0.74 contro -0.77; annullato dall'ultimo terzo (4,058 -> 1,678) |
| direzionale YES | PtnDirYes | 52 | 39 | · | batte lo spento: netto 6,163 contro -11,028, net/DD 3.35 contro -0.77; annullato dall'ultimo terzo (4,058 -> 2,665) |
| direzionale NO | PtnDirNo | 53 | 27 | · | batte lo spento: netto 2,968 contro -11,028, net/DD 1.85 contro -0.77; annullato dall'ultimo terzo (4,058 -> -174) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (919 trade, netto -6,971).

## TFU Trend following unmirrored

684 simulazioni in 28.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 15 | · | nessuna in utile: netto massimo -1,300; annullato dall'ultimo terzo (4,058 -> -2,105) |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 14.1, netto 2,358, 83 trade; annullato dall'ultimo terzo (4,058 -> -156) |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato -12,207 (migliore -12,207), 616 trade; annullato dall'ultimo terzo (4,058 -> 1,128) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -11,028 |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 116 | · | batte lo spento: netto 4,345 contro -11,028, net/DD 0.87 contro -0.77; annullato dall'ultimo terzo (4,058 -> 2,130) |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 149 | · | batte lo spento: netto 1,881 contro -11,028, net/DD 0.27 contro -0.77; annullato dall'ultimo terzo (4,058 -> 2,113) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (919 trade, netto -6,971).

## BO Breakout di N sessioni

703 simulazioni in 22.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=2, BreakoutOffsetTicks=20, IncludeCurrentSession=0 | ✔ | nessuna in utile: netto massimo -1,426 |
| finestra: inizio | StartHour | -1 | 14 | ✔ | avg smussato 3.9, netto 1,494, 188 trade |
| finestra: fine | EndHour | -1 | 16 | ✔ | avg smussato 8.7, netto 1,682, 160 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 1,620 (migliore 1,620), 160 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 10.5, netto 1,682, 160 trade |
| YES | PtnNeutYes | 55 | 41 | ✔ | batte lo spento: netto 2,693 contro 1,682, net/DD 4.61 contro 0.73 |
| NO | PtnNeutNo | 56 | 49 | · | batte lo spento: netto 2,679 contro 2,693, net/DD 4.92 contro 4.61; annullato dall'ultimo terzo (-1,223 -> -1,406) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 10 | ✔ | batte lo spento: netto 3,310 contro 2,693, net/DD 6.07 contro 4.61 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 3,327 contro 3,310, net/DD 6.10 contro 6.07 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 3,274 (migliore 3,327), 54 trade |

**Storia di ricerca**: 98 trade, netto 2,171, DD 1,790, average trade 22, UngerFit 0.68.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 98 su almeno 50 |
| average trade | **no** | 22 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 25.7 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -1,156 |
| ultimo terzo | **no** | netto -1,156, net/DD -0.65 (serve 1,3) |
| due terzi su tre | passa | 874 / 2,453 / -1,156 |
| outlier | passa | trade migliore 21% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -26 con i pattern, -23 senza |
| pattern casuali | **no** | 5 estrazioni su 7 fanno almeno altrettanto, p 0.75, Wilson 0.51 |
| plateau | passa | media 0.80 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 96 trade, netto -5,011, DD 5,622, net/DD -0.89, average trade -52 (soglia 60), UngerFit , finestre in utile 1/4 (-2,105 / -555 / 519 / -2,870).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.89 (serve 1), average trade -52 (soglia 60), finestre 1/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.9 2021:0.5 2022:1.5 2023:0.7 2024:-1.2 2025:-0.8 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=14, EndHour=16, LevelSource=0, IncludeCurrentSession=0, PtnNeutYes=41, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=10, SkipDay=-1, Sessions=2, BreakoutOffsetTicks=20`

## BOS Breakout della sessione in corso

537 simulazioni in 17.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=5 | ✔ | nessuna in utile: netto massimo -10,770 |
| finestra: inizio | StartHour | -1 | 19 | · | nessuna in utile: netto massimo -856; annullato dall'ultimo terzo (799 -> -372) |
| finestra: fine | EndHour | -1 | 1 | · | nessuna in utile: netto massimo -2,325; annullato dall'ultimo terzo (799 -> -1,813) |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato -5,797 (migliore -5,797), 645 trade; annullato dall'ultimo terzo (799 -> -1,260) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -10,770 |
| YES | PtnNeutYes | 55 | 10 | ✔ | batte lo spento: netto 4,975 contro -10,770, net/DD 1.92 contro -0.82 |
| NO | PtnNeutNo | 56 | 32 | · | batte lo spento: netto 7,813 contro 4,975, net/DD 4.19 contro 1.92; annullato dall'ultimo terzo (1,973 -> 1,715) |
| direzionale YES | PtnDirYes | 52 | 49 | ✔ | batte lo spento: netto 7,030 contro 4,975, net/DD 2.79 contro 1.92 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 8,181 contro 7,030, net/DD 3.24 contro 2.79; annullato dall'ultimo terzo (2,204 -> 1,401) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 7,317 contro 7,030, net/DD 2.90 contro 2.79; annullato dall'ultimo terzo (2,204 -> 2,103) |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 8,503 (migliore 8,503), 96 trade; annullato dall'ultimo terzo (2,204 -> 417) |

**Storia di ricerca**: 149 trade, netto 9,234, DD 2,523, average trade 62, UngerFit 1.59.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 149 su almeno 50 |
| average trade | passa | 62 contro la soglia 60 |
| anni | passa | 5 anni, minimo 5 trade in un anno, 39.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,204 |
| ultimo terzo | **no** | netto 2,204, net/DD 1.09 (serve 1,3) |
| due terzi su tre | passa | 4,424 / 2,606 / 2,204 |
| outlier | **no** | trade migliore 12% del netto sulla storia, 44% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 47 con i pattern, 2 senza |
| pattern casuali | passa | 7 estrazioni su 49 fanno almeno altrettanto, p 0.16, Wilson 0.10 |
| plateau | **no** | media 0.58 su 4 vicini, minimo 0.28 |

**Prova su FTMO**: 86 trade, netto 4,428, DD 2,268, net/DD 1.95, average trade 51 (soglia 60), UngerFit 1.40, finestre in utile 3/4 (208 / -1,115 / 2,192 / 3,144).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.95 (serve 1), average trade 51 (soglia 60), finestre 3/4 (servono 3) |
| anni | passa | 5/7 anni in utile (serve 60%), anno migliore 34% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.4 2021:1.7 2022:1.0 2023:0.9 2024:0.2 2025:-0.2 2026:2.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=10, PtnNeutNo=56, PtnDirYes=49, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=5`

## VBO Volatility breakout

449 simulazioni in 15.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.5, Direction=0 | ✔ | nessuna in utile: netto massimo -199 |
| finestra: inizio | StartHour | -1 | 12 | ✔ | avg smussato 21.0, netto 1,368, 58 trade |
| finestra: fine | EndHour | -1 | 3 | · | avg smussato 37.4, netto 2,171, 58 trade; annullato dall'ultimo terzo (-781 -> -1,320) |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 1,537 (migliore 1,609), 58 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 27.7, netto 1,609, 58 trade |
| YES | PtnNeutYes | 55 | 50 | · | batte lo spento: netto 2,560 contro 1,609, net/DD 3.30 contro 0.99; annullato dall'ultimo terzo (-519 -> -649) |
| NO | PtnNeutNo | 56 | 51 | · | batte lo spento: netto 2,560 contro 1,609, net/DD 3.30 contro 0.99; annullato dall'ultimo terzo (-519 -> -649) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 37 | · | batte lo spento: netto 3,210 contro 1,609, net/DD 3.67 contro 0.99; annullato dall'ultimo terzo (-519 -> -1,560) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 2,602 contro 1,609, net/DD 2.84 contro 0.99 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 1.25 | 1.25 | · | D2: netto smussato 2,533 (migliore 2,602), 51 trade |

**Storia di ricerca**: 90 trade, netto 2,083, DD 1,761, average trade 23, UngerFit 0.71.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 90 su almeno 50 |
| average trade | **no** | 23 contro la soglia 60 |
| anni | passa | 4 anni, minimo 19 trade in un anno, 23.6 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -519 |
| ultimo terzo | **no** | netto -519, net/DD -0.29 (serve 1,3) |
| due terzi su tre | passa | 487 / 2,115 / -519 |
| outlier | **no** | trade migliore 46% del netto sulla storia,  sull'ultimo terzo |
| plateau | passa | media 0.73 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 46 trade, netto 1,737, DD 1,526, net/DD 1.14, average trade 38 (soglia 60), UngerFit 1.25, finestre in utile 3/4 (-309 / 802 / 331 / 913).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.14 (serve 1), average trade 38 (soglia 60), finestre 3/4 (servono 3) |
| anni | **no** | 5/6 anni in utile (serve 60%), anno migliore 54% del netto (massimo 40%); average trade / soglia per anno: 2021:0.2 2022:1.8 2023:-0.4 2024:0.2 2025:0.3 2026:1.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=12, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0, AtrMultiplierLong=1.5, Direction=0`

## MAC Incrocio di medie

127 simulazioni in 2.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=30, SlowPeriod=100, Direction=1 | ✔ | avg smussato 35.4, netto 2,867, 81 trade |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 3,523 (migliore 3,523), 81 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 48.9, netto 3,961, 81 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 4,298 contro 3,961, net/DD 2.30 contro 2.12 |
| affinamento stop | StopAtr | 0.5 | 0.5 | · | D2: netto smussato 3,860 (migliore 3,860), 81 trade |

**Storia di ricerca**: 119 trade, netto 3,348, DD 2,143, average trade 28, UngerFit 0.78.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 119 su almeno 50 |
| average trade | **no** | 28 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 31.2 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -950 |
| ultimo terzo | **no** | netto -950, net/DD -0.44 (serve 1,3) |
| due terzi su tre | passa | 2,707 / 1,591 / -950 |
| outlier | **no** | trade migliore 36% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.49 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 44 trade, netto -1,780, DD 3,179, net/DD -0.56, average trade -40 (soglia 60), UngerFit , finestre in utile 2/4 (-2,112 / -717 / 665 / 384).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.56 (serve 1), average trade -40 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 190% del netto (massimo 40%); average trade / soglia per anno: 2020:1.6 2021:1.5 2022:0.4 2023:-0.1 2024:-1.1 2025:-0.4 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=30, SlowPeriod=100, Direction=1`

## RBM Reversal Bollinger mirrored

414 simulazioni in 13.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -3,711 |
| finestra: inizio | StartHour | -1 | 10 | · | nessuna in utile: netto massimo -76; annullato dall'ultimo terzo (-363 -> -1,261) |
| finestra: fine | EndHour | -1 | 7 | ✔ | nessuna in utile: netto massimo -1,133 |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -45 (migliore -45), 64 trade; annullato dall'ultimo terzo (-316 -> -814) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 23 | · | batte lo spento: netto 387 contro -1,133, net/DD 0.25 contro -0.36; annullato dall'ultimo terzo (-316 -> -361) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (103 trade, netto -1,449).

## RBU Reversal Bollinger unmirrored

962 simulazioni in 25.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -3,711 |
| finestra: inizio | StartHour | -1 | 10 | · | nessuna in utile: netto massimo -76; annullato dall'ultimo terzo (-363 -> -1,261) |
| finestra: fine | EndHour | -1 | 7 | ✔ | nessuna in utile: netto massimo -1,133 |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -45 (migliore -45), 64 trade; annullato dall'ultimo terzo (-316 -> -814) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 152 | · | nessun valore sopra lo spento |
| NO long | FastNoLong | 153 | 109 | ✔ | batte lo spento: netto 780 contro -1,133, net/DD 0.67 contro -0.36 |
| NO short | FastNoShort | 153 | 153 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 15.3, netto 780, 51 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 953 (migliore 953), 51 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 2 candidati batte lo spento |
| durata | MaxBars | 4 | 2 | · | avg smussato 19.3, netto 1,011, 51 trade; annullato dall'ultimo terzo (112 -> 87) |

**Storia di ricerca**: 83 trade, netto 1,065, DD 1,246, average trade 13, UngerFit 0.47.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 83 su almeno 50 |
| average trade | **no** | 13 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 21.8 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 112 |
| ultimo terzo | **no** | netto 112, net/DD 0.09 (serve 1,3) |
| due terzi su tre | passa | 411 / 543 / 112 |
| outlier | **no** | trade migliore 74% del netto sulla storia, 300% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 3 con i pattern, -1 senza |
| pattern casuali | **no** | 14 estrazioni su 41 fanno almeno altrettanto, p 0.36, Wilson 0.27 |
| plateau | **no** | media 0.46 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 5 trade, netto -463, DD 883, net/DD -0.52, average trade -93 (soglia 60), UngerFit , finestre in utile 0/4 (-463 / 0 / 0 / 0).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.52 (serve 1), average trade -93 (soglia 60), finestre 0/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 350% del netto (massimo 40%); average trade / soglia per anno: 2020:0.7 2021:0.5 2022:-0.9 2023:1.5 2024:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=7, FastYesLong=152, FastYesShort=152, FastNoLong=109, FastNoShort=153, SkipDay=-1, BbLength=30, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

473 simulazioni in 15.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=2, Direction=1 | ✔ | avg smussato 0.5, netto 146, 275 trade |
| finestra: inizio | StartHour | -1 | 11 | ✔ | avg smussato 1.9, netto 2,193, 231 trade |
| finestra: fine | EndHour | -1 | 12 | · | avg smussato 22.0, netto 3,038, 137 trade; annullato dall'ultimo terzo (-480 -> -1,044) |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 1,849 (migliore 1,849), 231 trade; annullato dall'ultimo terzo (-480 -> -789) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 3,040 contro 2,193, net/DD 0.79 contro 0.54; annullato dall'ultimo terzo (-480 -> -531) |
| YES | PtnNeutYes | 55 | 3 | · | batte lo spento: netto 5,558 contro 2,193, net/DD 1.59 contro 0.54; annullato dall'ultimo terzo (-480 -> -744) |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 5,623 contro 2,193, net/DD 1.69 contro 0.54; annullato dall'ultimo terzo (-480 -> -615) |
| direzionale YES | PtnDirYes | 52 | 28 | · | batte lo spento: netto 3,756 contro 2,193, net/DD 1.38 contro 0.54; annullato dall'ultimo terzo (-480 -> -1,379) |
| direzionale NO | PtnDirNo | 53 | -2 | · | batte lo spento: netto 4,444 contro 2,193, net/DD 2.87 contro 0.54; annullato dall'ultimo terzo (-480 -> -1,492) |
| calendario | SkipDay | -1 | 4 | · | batte lo spento: netto 3,522 contro 2,193, net/DD 1.27 contro 0.54; annullato dall'ultimo terzo (-480 -> -1,082) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 9.5, netto 2,193, 231 trade |
| uscita a ora fissa | ExitHour | -1 | 20 | · | batte lo spento: netto 2,476 contro 2,193, net/DD 0.64 contro 0.54; annullato dall'ultimo terzo (-480 -> -913) |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 1,849 (migliore 1,849), 231 trade; annullato dall'ultimo terzo (-480 -> -789) |
| affinamento target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 3,040 contro 2,193, net/DD 0.79 contro 0.54; annullato dall'ultimo terzo (-480 -> -531) |
| durata | MaxBars | 4 | 4 | · | avg smussato 0.0, netto 2,193, 231 trade |

**Storia di ricerca**: 350 trade, netto 1,713, DD 4,069, average trade 5, UngerFit 0.10.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 350 su almeno 50 |
| average trade | **no** | 5 contro la soglia 60 |
| anni | passa | 5 anni, minimo 6 trade in un anno, 91.8 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -480 |
| ultimo terzo | **no** | netto -480, net/DD -0.32 (serve 1,3) |
| due terzi su tre | **no** | -828 / 3,021 / -480 |
| outlier | **no** | trade migliore 58% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.17 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 199 trade, netto 248, DD 3,584, net/DD 0.07, average trade 1 (soglia 60), UngerFit 0.03, finestre in utile 2/4 (-922 / -591 / 583 / 1,557).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.07 (serve 1), average trade 1 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 108% del netto (massimo 40%); average trade / soglia per anno: 2020:1.1 2021:0.2 2022:0.0 2023:0.2 2024:-0.5 2025:-0.1 2026:0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=11, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=2, Direction=1`

## LF Level fader sul pivot di ieri

307 simulazioni in 8.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | avg smussato 30.6, netto 3,338, 109 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 3,415 (migliore 3,415), 113 trade; annullato dall'ultimo terzo (2,211 -> 777) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 3 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 34 | · | batte lo spento: netto 4,574 contro 3,338, net/DD 4.20 contro 2.21; annullato dall'ultimo terzo (2,211 -> 1,764) |
| NO | PtnNeutNo | 56 | 41 | · | batte lo spento: netto 4,574 contro 3,338, net/DD 4.20 contro 2.21; annullato dall'ultimo terzo (2,211 -> 1,764) |
| direzionale YES | PtnDirYes | 52 | -5 | · | batte lo spento: netto 5,822 contro 3,338, net/DD 8.58 contro 2.21; annullato dall'ultimo terzo (2,211 -> 1,485) |
| calendario long | NotEntryDayLong | -1 | 4 | · | batte lo spento: netto 4,300 contro 3,338, net/DD 2.84 contro 2.21; annullato dall'ultimo terzo (2,211 -> 1,599) |
| calendario short | NotEntryDayShort | -1 | 2 | · | batte lo spento: netto 3,476 contro 3,338, net/DD 3.11 contro 2.21; annullato dall'ultimo terzo (2,211 -> 1,793) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 30.6, netto 3,338, 109 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 3,266 (migliore 3,376), 110 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 2 | · | avg smussato 26.6, netto 3,010, 118 trade; annullato dall'ultimo terzo (2,412 -> 930) |

**Storia di ricerca**: 164 trade, netto 5,462, DD 1,721, average trade 33, UngerFit 1.04.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 164 su almeno 50 |
| average trade | **no** | 33 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 43.0 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,412 |
| ultimo terzo | passa | netto 2,412, net/DD 5.29 (serve 1,3) |
| due terzi su tre | passa | -274 / 3,323 / 2,412 |
| outlier | passa | trade migliore 18% del netto sulla storia, 22% sull'ultimo terzo |
| plateau | passa | media 0.66 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 120 trade, netto -2,048, DD 4,183, net/DD -0.49, average trade -17 (soglia 60), UngerFit , finestre in utile 2/4 (266 / -755 / 392 / -1,952).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.49 (serve 1), average trade -17 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 102% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.6 2021:-0.1 2022:1.0 2023:0.2 2024:0.8 2025:0.3 2026:-1.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=20`

## LFHL Level fader sugli estremi di ieri

502 simulazioni in 10.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=0 | ✔ | nessuna in utile: netto massimo -771 |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato 24 (migliore 24), 397 trade; annullato dall'ultimo terzo (-6,240 -> -6,696) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 49 | ✔ | batte lo spento: netto 3,243 contro -771, net/DD 5.82 contro -0.23 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 2 | · | batte lo spento: netto 3,249 contro 3,243, net/DD 5.83 contro 5.82; annullato dall'ultimo terzo (113 -> 38) |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 59.0, netto 3,243, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 3,237 (migliore 3,262), 55 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 8 | ✔ | avg smussato 61.5, netto 2,852, 50 trade |

**Storia di ricerca**: 73 trade, netto 3,156, DD 1,031, average trade 43, UngerFit 1.74.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 73 su almeno 50 |
| average trade | **no** | 43 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 19.2 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 304 |
| ultimo terzo | **no** | netto 304, net/DD 0.60 (serve 1,3) |
| due terzi su tre | passa | 92 / 2,760 / 304 |
| outlier | **no** | trade migliore 38% del netto sulla storia, 87% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 13 con i pattern, -31 senza |
| pattern casuali | passa | 5 estrazioni su 50 fanno almeno altrettanto, p 0.12, Wilson 0.07 |
| plateau | passa | media 0.86 su 5 vicini, minimo 0.74 |

**Prova su FTMO**: 43 trade, netto 290, DD 1,403, net/DD 0.21, average trade 7 (soglia 60), UngerFit 0.23, finestre in utile 3/4 (20 / 156 / -338 / 452).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.21 (serve 1), average trade 7 (soglia 60), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 66% del netto (massimo 40%); average trade / soglia per anno: 2020:0.8 2021:0.5 2022:1.8 2023:-0.5 2024:0.1 2025:0.4 2026:0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=8, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=49, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=0`

## Riepilogo

- PCH: scartata; ricerca 81 trade netto 5,978 avg 74; prova 36 trade netto -446 net/DD -0.24 avg -12 fin 1/4; falliti: anni, ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- TFM: abbandonato (R9: la base non guadagna su tutta la storia (919 trade, netto -6,971))
- TFU: abbandonato (R9: la base non guadagna su tutta la storia (919 trade, netto -6,971))
- BO: scartata; ricerca 98 trade netto 2,171 avg 22; prova 96 trade netto -5,011 net/DD -0.89 avg -52 fin 1/4; falliti: average trade, anni, utile recente, ultimo terzo, pattern utile, pattern casuali, prova sul broker, anni
- BOS: scartata; ricerca 149 trade netto 9,234 avg 62; prova 86 trade netto 4,428 net/DD 1.95 avg 51 fin 3/4; falliti: ultimo terzo, outlier, plateau, prova sul broker
- VBO: scartata; ricerca 90 trade netto 2,083 avg 23; prova 46 trade netto 1,737 net/DD 1.14 avg 38 fin 3/4; falliti: average trade, utile recente, ultimo terzo, outlier, prova sul broker, anni
- MAC: scartata; ricerca 119 trade netto 3,348 avg 28; prova 44 trade netto -1,780 net/DD -0.56 avg -40 fin 2/4; falliti: average trade, anni, utile recente, ultimo terzo, outlier, plateau, prova sul broker, anni
- RBM: abbandonato (R9: la base non guadagna su tutta la storia (103 trade, netto -1,449))
- RBU: scartata; ricerca 83 trade netto 1,065 avg 13; prova 5 trade netto -463 net/DD -0.52 avg -93 fin 0/4; falliti: average trade, anni, ultimo terzo, outlier, pattern casuali, plateau, prova sul broker, anni
- RHL: scartata; ricerca 350 trade netto 1,713 avg 5; prova 199 trade netto 248 net/DD 0.07 avg 1 fin 2/4; falliti: average trade, utile recente, ultimo terzo, due terzi su tre, outlier, plateau, prova sul broker, anni
- LF: scartata; ricerca 164 trade netto 5,462 avg 33; prova 120 trade netto -2,048 net/DD -0.49 avg -17 fin 2/4; falliti: average trade, anni, prova sul broker, anni
- LFHL: scartata; ricerca 73 trade netto 3,156 avg 43; prova 43 trade netto 290 net/DD 0.21 avg 7 fin 3/4; falliti: average trade, anni, ultimo terzo, outlier, prova sul broker, anni
