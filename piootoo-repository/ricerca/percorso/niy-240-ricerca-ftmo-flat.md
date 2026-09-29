# Percorso v4 — @NIY 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (5,890 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,208 barre), mai vista dal percorso
- costi FTMO: spread 10.00 punti (mediana), swap long 8.4766 short 3.6329 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 15,109 nella ricerca, 32,362 nella prova

## PCH Price Channel

552 simulazioni in 16.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=10, OffsetTicks=0, Direction=1 | ✔ | avg smussato 11,553.6, netto 3,454,521, 299 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 11,553.6, netto 3,454,521, 299 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 11,553.6, netto 3,454,521, 299 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 3,355,735 (migliore 3,481,292), 299 trade; annullato dall'ultimo terzo (1,581,639 -> 1,569,004) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 11,553.6, netto 3,454,521, 299 trade |
| YES | PtnNeutYes | 55 | 3 | · | batte lo spento: netto 3,067,207 contro 3,454,521, net/DD 5.11 contro 4.35; annullato dall'ultimo terzo (1,581,639 -> -1,948,529) |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 3,067,207 contro 3,454,521, net/DD 5.11 contro 4.35; annullato dall'ultimo terzo (1,581,639 -> -1,948,529) |
| direzionale YES | PtnDirYes | 52 | -44 | · | batte lo spento: netto 3,438,679 contro 3,454,521, net/DD 8.26 contro 4.35; annullato dall'ultimo terzo (1,581,639 -> -980,876) |
| direzionale NO | PtnDirNo | 53 | 12 | · | batte lo spento: netto 4,181,264 contro 3,454,521, net/DD 5.93 contro 4.35; annullato dall'ultimo terzo (1,581,639 -> 772,991) |
| calendario | SkipDay | -1 | 1 | ✔ | batte lo spento: netto 3,164,150 contro 3,454,521, net/DD 4.48 contro 4.35 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 2,856,060 (migliore 2,900,839), 261 trade; annullato dall'ultimo terzo (2,077,836 -> 1,548,726) |

**Storia di ricerca**: 393 trade, netto 5,241,986, DD 1,366,296, average trade 13,338, UngerFit 0.93.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 393 su almeno 50 |
| average trade | **no** | 13,338 contro la soglia 15,109 |
| anni | passa | 5 anni, minimo 6 trade in un anno, 103.1 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,077,836 |
| ultimo terzo | passa | netto 2,077,836, net/DD 1.52 (serve 1,3) |
| due terzi su tre | passa | 1,336,664 / 1,827,486 / 2,077,836 |
| outlier | passa | trade migliore 9% del netto sulla storia, 24% sull'ultimo terzo |
| plateau | passa | media 0.74 su 5 vicini, minimo 0.38 |

**Prova su FTMO**: 215 trade, netto 2,744,423, DD 2,404,620, net/DD 1.14, average trade 12,765 (soglia 32,362), UngerFit 0.46, finestre in utile 3/4 (-429,273 / 1,059,437 / 1,630,995 / 483,265).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.14 (serve 1), average trade 12,765 (soglia 32,362), finestre 3/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 27% del netto (massimo 40%); average trade / soglia per anno: 2020:2.0 2021:0.9 2022:0.5 2023:1.2 2024:0.5 2025:0.1 2026:0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=1, ChannelBars=10, OffsetTicks=0, Direction=1`

## TFM Trend following mirrored

526 simulazioni in 12.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -134,800 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -134,800 |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 694,478 (migliore 694,478), 660 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 2,250.1, netto 1,485,045, 660 trade |
| YES | PtnNeutYes | 55 | 52 | · | batte lo spento: netto 2,223,411 contro 1,485,045, net/DD 5.03 contro 0.95; annullato dall'ultimo terzo (586,773 -> 138,888) |
| NO | PtnNeutNo | 56 | 37 | · | batte lo spento: netto 2,908,429 contro 1,485,045, net/DD 1.69 contro 0.95; annullato dall'ultimo terzo (586,773 -> 181,743) |
| direzionale YES | PtnDirYes | 52 | -21 | ✔ | batte lo spento: netto 2,699,161 contro 1,485,045, net/DD 3.69 contro 0.95 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 3,320,446 contro 2,699,161, net/DD 7.94 contro 3.69; annullato dall'ultimo terzo (961,532 -> -437,091) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 2,851,080 contro 2,699,161, net/DD 3.90 contro 3.69; annullato dall'ultimo terzo (961,532 -> 853,991) |
| affinamento stop | StopAtr | 0.5 | 0.6 | · | D2: netto smussato 2,961,758 (migliore 3,093,057), 152 trade; annullato dall'ultimo terzo (961,532 -> 829,336) |

**Storia di ricerca**: 228 trade, netto 3,660,693, DD 1,320,188, average trade 16,056, UngerFit 1.14.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 228 su almeno 50 |
| average trade | passa | 16,056 contro la soglia 15,109 |
| anni | passa | 5 anni, minimo 8 trade in un anno, 59.8 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 961,532 |
| ultimo terzo | **no** | netto 961,532, net/DD 0.73 (serve 1,3) |
| due terzi su tre | passa | 1,233,455 / 1,428,955 / 961,532 |
| outlier | **no** | trade migliore 17% del netto sulla storia, 64% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 12,652 con i pattern, 1,639 senza |
| pattern casuali | **no** | 15 estrazioni su 39 fanno almeno altrettanto, p 0.40, Wilson 0.31 |
| plateau | passa | media 0.96 su 2 vicini, minimo 0.92 |

**Prova su FTMO**: 126 trade, netto 1,959,184, DD 4,440,335, net/DD 0.44, average trade 15,549 (soglia 32,362), UngerFit 0.41, finestre in utile 2/4 (1,671,633 / -1,980,608 / -975,528 / 3,243,686).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.44 (serve 1), average trade 15,549 (soglia 32,362), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 67% del netto (massimo 40%); average trade / soglia per anno: 2020:2.4 2021:0.3 2022:2.3 2023:-0.2 2024:1.8 2025:-2.4 2026:2.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=-21, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

928 simulazioni in 24.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -134,800 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -134,800 |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 694,478 (migliore 694,478), 660 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 2,250.1, netto 1,485,045, 660 trade |
| YES long | FastYesLong | 152 | 131 | · | batte lo spento: netto 1,399,982 contro 1,485,045, net/DD 1.08 contro 0.95; annullato dall'ultimo terzo (586,773 -> -2,204,861) |
| YES short | FastYesShort | 152 | 84 | ✔ | batte lo spento: netto 4,806,598 contro 1,485,045, net/DD 4.66 contro 0.95 |
| NO long | FastNoLong | 153 | 132 | · | batte lo spento: netto 3,947,580 contro 4,806,598, net/DD 5.81 contro 4.66; annullato dall'ultimo terzo (1,995,241 -> 394,739) |
| NO short | FastNoShort | 153 | 59 | ✔ | batte lo spento: netto 4,927,214 contro 4,806,598, net/DD 4.78 contro 4.66 |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 4,767,429 contro 4,927,214, net/DD 6.20 contro 4.78; annullato dall'ultimo terzo (1,995,241 -> 1,135,502) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 5,001,036 contro 4,927,214, net/DD 4.85 contro 4.78 |
| affinamento stop | StopAtr | 0.5 | 0.6 | · | D2: netto smussato 5,019,467 (migliore 5,028,682), 365 trade; annullato dall'ultimo terzo (2,020,634 -> 1,906,331) |

**Storia di ricerca**: 561 trade, netto 7,041,170, DD 2,404,654, average trade 12,551, UngerFit 0.66.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 561 su almeno 50 |
| average trade | **no** | 12,551 contro la soglia 15,109 |
| anni | passa | 5 anni, minimo 12 trade in un anno, 147.2 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,040,134 |
| ultimo terzo | **no** | netto 2,020,634, net/DD 0.84 (serve 1,3) |
| due terzi su tre | passa | 1,564,518 / 3,411,018 / 2,020,634 |
| outlier | passa | trade migliore 8% del netto sulla storia, 29% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 10,309 con i pattern, 3,382 senza |
| pattern casuali | **no** | 34 estrazioni su 100 fanno almeno altrettanto, p 0.35, Wilson 0.29 |
| plateau | passa | media 0.88 su 4 vicini, minimo 0.59 |

**Prova su FTMO**: 309 trade, netto 4,139,714, DD 3,781,764, net/DD 1.09, average trade 13,397 (soglia 32,362), UngerFit 0.38, finestre in utile 4/4 (258,496 / 528,621 / 3,318,876 / 33,721).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.09 (serve 1), average trade 13,397 (soglia 32,362), finestre 4/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 29% del netto (massimo 40%); average trade / soglia per anno: 2020:0.1 2021:0.6 2022:1.2 2023:1.3 2024:0.2 2025:0.4 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastYesLong=152, FastYesShort=84, FastNoLong=153, FastNoShort=59, SkipDay=-1`

## BO Breakout di N sessioni

505 simulazioni in 18.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=0, IncludeCurrentSession=1 | ✔ | avg smussato 880.1, netto 506,929, 576 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 880.1, netto 506,929, 576 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 880.1, netto 506,929, 576 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 691,462 (migliore 691,462), 587 trade; annullato dall'ultimo terzo (1,286,816 -> -1,650,171) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 880.1, netto 506,929, 576 trade |
| YES | PtnNeutYes | 55 | 52 | · | batte lo spento: netto 1,892,800 contro 506,929, net/DD 3.36 contro 0.20; annullato dall'ultimo terzo (1,286,816 -> -144,926) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 1,199,250 contro 506,929, net/DD 1.86 contro 0.20; annullato dall'ultimo terzo (1,286,816 -> 130,504) |
| direzionale YES | PtnDirYes | 52 | -21 | · | batte lo spento: netto 2,724,214 contro 506,929, net/DD 4.24 contro 0.20; annullato dall'ultimo terzo (1,286,816 -> 282,344) |
| direzionale NO | PtnDirNo | 53 | 27 | · | batte lo spento: netto 2,859,964 contro 506,929, net/DD 4.74 contro 0.20; annullato dall'ultimo terzo (1,286,816 -> 341,844) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 1,424,000 contro 506,929, net/DD 0.60 contro 0.20; annullato dall'ultimo terzo (1,286,816 -> 901,010) |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 1,837,538 contro 506,929, net/DD 0.76 contro 0.20; annullato dall'ultimo terzo (1,286,816 -> -180,256) |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 1,716,893 contro 506,929, net/DD 0.66 contro 0.20; annullato dall'ultimo terzo (1,286,816 -> 716,841) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 484,461 (migliore 491,950), 586 trade; annullato dall'ultimo terzo (1,286,816 -> 1,080,364) |

**Storia di ricerca**: 885 trade, netto 1,813,244, DD 2,572,821, average trade 2,049, UngerFit 0.10.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 885 su almeno 50 |
| average trade | **no** | 2,049 contro la soglia 15,109 |
| anni | passa | 5 anni, minimo 13 trade in un anno, 232.2 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,306,316 |
| ultimo terzo | **no** | netto 1,286,816, net/DD 0.64 (serve 1,3) |
| due terzi su tre | passa | 871,114 / -389,186 / 1,286,816 |
| outlier | **no** | trade migliore 43% del netto sulla storia, 60% sull'ultimo terzo |
| plateau | passa | media 0.60 su 4 vicini, minimo 0.14 |

**Prova su FTMO**: 463 trade, netto 308,524, DD 5,555,625, net/DD 0.06, average trade 666 (soglia 32,362), UngerFit 0.02, finestre in utile 1/4 (-108,453 / -1,299,158 / -911,456 / 2,495,091).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.06 (serve 1), average trade 666 (soglia 32,362), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 182% del netto (massimo 40%); average trade / soglia per anno: 2020:0.0 2021:0.3 2022:-0.4 2023:0.7 2024:0.0 2025:-0.7 2026:0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=1, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

437 simulazioni in 15.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=20 | ✔ | nessuna in utile: netto massimo -1,176,250 |
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -1,176,250 |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 8,724.0, netto 1,011,979, 116 trade; annullato dall'ultimo terzo (2,975,894 -> 850,426) |
| stop | StopAtr | 0.8 | 0.4 | · | D2: netto smussato -942,338 (migliore -942,338), 424 trade; annullato dall'ultimo terzo (2,975,894 -> -2,696,439) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -1,176,250 |
| YES | PtnNeutYes | 55 | 48 | · | batte lo spento: netto 1,856,071 contro -1,176,250, net/DD 3.30 contro -0.41; annullato dall'ultimo terzo (2,975,894 -> -1,226,294) |
| NO | PtnNeutNo | 56 | 32 | · | batte lo spento: netto 1,894,236 contro -1,176,250, net/DD 3.37 contro -0.41; annullato dall'ultimo terzo (2,975,894 -> 98,439) |
| direzionale YES | PtnDirYes | 52 | -7 | · | batte lo spento: netto 978,393 contro -1,176,250, net/DD 1.31 contro -0.41; annullato dall'ultimo terzo (2,975,894 -> 2,975,257) |
| direzionale NO | PtnDirNo | 53 | 1 | · | batte lo spento: netto 972,021 contro -1,176,250, net/DD 2.11 contro -0.41; annullato dall'ultimo terzo (2,975,894 -> 736,903) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 1,457,836 contro -1,176,250, net/DD 0.83 contro -0.41; annullato dall'ultimo terzo (2,975,894 -> 1,118,174) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato -865,994 (migliore -865,994), 423 trade; annullato dall'ultimo terzo (2,975,894 -> 1,277,987) |

**Storia di ricerca**: 671 trade, netto 1,799,644, DD 2,860,029, average trade 2,682, UngerFit 0.13.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 671 su almeno 50 |
| average trade | **no** | 2,682 contro la soglia 15,109 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 176.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,975,894 |
| ultimo terzo | passa | netto 2,975,894, net/DD 1.78 (serve 1,3) |
| due terzi su tre | passa | -1,666,386 / 490,136 / 2,975,894 |
| outlier | **no** | trade migliore 40% del netto sulla storia, 24% sull'ultimo terzo |
| plateau | **no** | media 0.25 su 3 vicini, minimo 0.00 |

**Prova su FTMO**: 469 trade, netto -1,075,601, DD 5,761,722, net/DD -0.19, average trade -2,293 (soglia 32,362), UngerFit , finestre in utile 2/4 (1,490,926 / 1,250,125 / -543,226 / -3,015,965).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.19 (serve 1), average trade -2,293 (soglia 32,362), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 464% del netto (massimo 40%); average trade / soglia per anno: 2020:0.6 2021:-0.3 2022:-0.6 2023:1.1 2024:0.7 2025:0.1 2026:-0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=20`

## Riepilogo

- PCH: scartata; ricerca 393 trade netto 5,241,986 avg 13,338; prova 215 trade netto 2,744,423 net/DD 1.14 avg 12,765 fin 3/4; falliti: average trade, prova sul broker
- TFM: scartata; ricerca 228 trade netto 3,660,693 avg 16,056; prova 126 trade netto 1,959,184 net/DD 0.44 avg 15,549 fin 2/4; falliti: ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- TFU: scartata; ricerca 561 trade netto 7,041,170 avg 12,551; prova 309 trade netto 4,139,714 net/DD 1.09 avg 13,397 fin 4/4; falliti: average trade, ultimo terzo, pattern casuali, prova sul broker
- BO: scartata; ricerca 885 trade netto 1,813,244 avg 2,049; prova 463 trade netto 308,524 net/DD 0.06 avg 666 fin 1/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, anni
- BOS: scartata; ricerca 671 trade netto 1,799,644 avg 2,682; prova 469 trade netto -1,075,601 net/DD -0.19 avg -2,293 fin 2/4; falliti: average trade, outlier, plateau, prova sul broker, anni
