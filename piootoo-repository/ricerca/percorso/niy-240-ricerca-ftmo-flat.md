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

## VBO Volatility breakout

447 simulazioni in 15.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.7, Direction=1 | ✔ | avg smussato 13,403.0, netto 2,506,364, 187 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 13,403.0, netto 2,506,364, 187 trade |
| finestra: fine | EndHour | -1 | 5 | · | avg smussato 21,458.4, netto 2,188,761, 102 trade; annullato dall'ultimo terzo (1,187,965 -> 354,259) |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 2,348,301 (migliore 2,348,301), 187 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 13,403.0, netto 2,506,364, 187 trade |
| YES | PtnNeutYes | 55 | 12 | · | batte lo spento: netto 2,489,489 contro 2,506,364, net/DD 5.56 contro 2.84; annullato dall'ultimo terzo (1,187,965 -> 50,954) |
| NO | PtnNeutNo | 56 | 18 | · | batte lo spento: netto 2,489,489 contro 2,506,364, net/DD 5.56 contro 2.84; annullato dall'ultimo terzo (1,187,965 -> 50,954) |
| direzionale YES | PtnDirYes | 52 | -27 | · | batte lo spento: netto 2,970,468 contro 2,506,364, net/DD 11.16 contro 2.84; annullato dall'ultimo terzo (1,187,965 -> 451,419) |
| direzionale NO | PtnDirNo | 53 | 21 | · | batte lo spento: netto 2,923,418 contro 2,506,364, net/DD 10.98 contro 2.84; annullato dall'ultimo terzo (1,187,965 -> 451,419) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 3,431,414 contro 2,506,364, net/DD 6.13 contro 2.84; annullato dall'ultimo terzo (1,187,965 -> 563,825) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 2,348,301 (migliore 2,348,301), 187 trade |

**Storia di ricerca**: 289 trade, netto 3,694,329, DD 1,086,371, average trade 12,783, UngerFit 1.00.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 289 su almeno 50 |
| average trade | **no** | 12,783 contro la soglia 15,109 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 75.8 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,187,965 |
| ultimo terzo | **no** | netto 1,187,965, net/DD 1.09 (serve 1,3) |
| due terzi su tre | passa | 232,882 / 2,273,482 / 1,187,965 |
| outlier | **no** | trade migliore 11% del netto sulla storia, 34% sull'ultimo terzo |
| plateau | **no** | media 0.57 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 144 trade, netto 8,407,464, DD 1,328,913, net/DD 6.33, average trade 58,385 (soglia 32,362), UngerFit 2.82, finestre in utile 4/4 (1,152,205 / 1,537,841 / 1,651,373 / 4,066,045).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 6.33 (serve 1), average trade 58,385 (soglia 32,362), finestre 4/4 (servono 3) |
| anni | **no** | 7/7 anni in utile (serve 60%), anno migliore 48% del netto (massimo 40%); average trade / soglia per anno: 2020:0.2 2021:0.3 2022:1.5 2023:1.1 2024:0.9 2025:0.8 2026:2.4 |
| altri mercati | **no** | 2/7 con net/DD ≥ 1 (serve meta'): @FDAX 2.17, @NQ 4.60, @ES 0.98, @YM -0.31, @FESX 0.40, @FCE 0.17, @Z -0.32 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.7, Direction=1`

## MAC Incrocio di medie

124 simulazioni in 2.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=20, SlowPeriod=50, Direction=0 | ✔ | avg smussato 21,357.9, netto 1,858,136, 87 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 1,841,780 (migliore 1,846,896), 87 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 22,226.4, netto 1,933,696, 87 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 1,841,780 (migliore 1,846,896), 87 trade |

**Storia di ricerca**: 122 trade, netto 3,610,346, DD 587,704, average trade 29,593, UngerFit 3.14.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 122 su almeno 50 |
| average trade | passa | 29,593 contro la soglia 15,109 |
| anni | passa | 5 anni, minimo 5 trade in un anno, 32.0 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,676,650 |
| ultimo terzo | passa | netto 1,676,650, net/DD 3.74 (serve 1,3) |
| due terzi su tre | passa | 409,311 / 1,524,386 / 1,676,650 |
| outlier | passa | trade migliore 12% del netto sulla storia, 25% sull'ultimo terzo |
| plateau | **no** | media 0.35 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 64 trade, netto 1,800,904, DD 832,385, net/DD 2.16, average trade 28,139 (soglia 32,362), UngerFit 1.71, finestre in utile 3/4 (-106,432 / 210,678 / 1,168,950 / 527,708).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 2.16 (serve 1), average trade 28,139 (soglia 32,362), finestre 3/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 27% del netto (massimo 40%); average trade / soglia per anno: 2020:4.2 2021:0.4 2022:3.6 2023:1.0 2024:1.7 2025:0.9 2026:1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=20, SlowPeriod=50, Direction=0`

## RBM Reversal Bollinger mirrored

476 simulazioni in 12.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | avg smussato 3,136.7, netto 241,528, 77 trade |
| finestra: inizio | StartHour | -1 | 6 | · | avg smussato 12,035.6, netto 710,102, 59 trade; annullato dall'ultimo terzo (1,104,135 -> 355,595) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 3,136.7, netto 241,528, 77 trade |
| stop | StopAtr | 0.8 | 2.0 | ✔ | D2: netto smussato 920,701 (migliore 954,814), 77 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 1,212,103 contro 954,814, net/DD 1.75 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 982,866) |
| YES | PtnNeutYes | 55 | 17 | · | batte lo spento: netto 1,340,901 contro 954,814, net/DD 2.28 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 563,248) |
| NO | PtnNeutNo | 56 | 11 | · | batte lo spento: netto 1,340,901 contro 954,814, net/DD 2.28 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 563,248) |
| direzionale YES | PtnDirYes | 52 | 24 | · | batte lo spento: netto 1,344,104 contro 954,814, net/DD 2.45 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 418,727) |
| direzionale NO | PtnDirNo | 53 | 34 | · | batte lo spento: netto 1,426,848 contro 954,814, net/DD 2.12 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 129,260) |
| calendario | SkipDay | -1 | 4 | · | batte lo spento: netto 1,420,226 contro 954,814, net/DD 3.11 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 669,110) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 12,400.2, netto 954,814, 77 trade |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 1,331,691 contro 954,814, net/DD 3.60 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 634,783) |
| affinamento stop | StopAtr | 2.0 | 2.0 | · | D2: netto smussato 920,701 (migliore 954,814), 77 trade |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 1,212,103 contro 954,814, net/DD 1.75 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 982,866) |
| durata | MaxBars | 4 | 6 | · | avg smussato 11,069.0, netto 801,064, 77 trade; annullato dall'ultimo terzo (1,234,199 -> 649,143) |

**Storia di ricerca**: 115 trade, netto 2,189,014, DD 693,640, average trade 19,035, UngerFit 1.86.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 115 su almeno 50 |
| average trade | passa | 19,035 contro la soglia 15,109 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 30.2 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,234,199 |
| ultimo terzo | passa | netto 1,234,199, net/DD 1.89 (serve 1,3) |
| due terzi su tre | passa | 1,167,373 / -212,559 / 1,234,199 |
| outlier | **no** | trade migliore 24% del netto sulla storia, 42% sull'ultimo terzo |
| plateau | **no** | media 0.39 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 60 trade, netto 1,322,897, DD 1,686,697, net/DD 0.78, average trade 22,048 (soglia 32,362), UngerFit 0.94, finestre in utile 3/4 (-234,415 / 190,659 / 482,602 / 884,051).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.78 (serve 1), average trade 22,048 (soglia 32,362), finestre 3/4 (servono 3) |
| anni | passa | 5/7 anni in utile (serve 60%), anno migliore 39% del netto (massimo 40%); average trade / soglia per anno: 2020:-5.4 2021:0.9 2022:2.0 2023:1.4 2024:-0.1 2025:2.1 2026:0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=2.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

970 simulazioni in 18.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | avg smussato 3,136.7, netto 241,528, 77 trade |
| finestra: inizio | StartHour | -1 | 6 | · | avg smussato 12,035.6, netto 710,102, 59 trade; annullato dall'ultimo terzo (1,104,135 -> 355,595) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 3,136.7, netto 241,528, 77 trade |
| stop | StopAtr | 0.8 | 2.0 | ✔ | D2: netto smussato 920,701 (migliore 954,814), 77 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 1,212,103 contro 954,814, net/DD 1.75 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> 982,866) |
| YES long | FastYesLong | 152 | 99 | · | batte lo spento: netto 1,496,009 contro 954,814, net/DD 2.47 contro 1.38; annullato dall'ultimo terzo (1,234,199 -> -362,861) |
| YES short | FastYesShort | 152 | 46 | ✔ | batte lo spento: netto 1,672,528 contro 954,814, net/DD 3.96 contro 1.38 |
| NO long | FastNoLong | 153 | 153 | · | nessuno dei 3 candidati batte lo spento |
| NO short | FastNoShort | 153 | 153 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | 4 | · | batte lo spento: netto 1,779,411 contro 1,672,528, net/DD 4.21 contro 3.96; annullato dall'ultimo terzo (1,480,516 -> 915,427) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 30,409.6, netto 1,672,528, 55 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 1,714,575 contro 1,672,528, net/DD 4.00 contro 3.96 |
| affinamento stop | StopAtr | 2.0 | 1.5 | · | D2: netto smussato 1,656,066 (migliore 1,714,575), 55 trade; annullato dall'ultimo terzo (1,568,516 -> 1,499,183) |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 1,794,935 contro 1,714,575, net/DD 4.19 contro 4.00; annullato dall'ultimo terzo (1,568,516 -> 1,292,505) |
| durata | MaxBars | 4 | 0 | · | avg smussato 35,674.1, netto 1,962,075, 55 trade; annullato dall'ultimo terzo (1,568,516 -> 1,159,960) |

**Storia di ricerca**: 78 trade, netto 3,283,091, DD 534,337, average trade 42,091, UngerFit 4.68.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 78 su almeno 50 |
| average trade | passa | 42,091 contro la soglia 15,109 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 20.5 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,568,516 |
| ultimo terzo | passa | netto 1,568,516, net/DD 2.94 (serve 1,3) |
| due terzi su tre | passa | 1,620,330 / 94,245 / 1,568,516 |
| outlier | **no** | trade migliore 16% del netto sulla storia, 33% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 68,196 con i pattern, 35,703 senza |
| pattern casuali | passa | 15 estrazioni su 82 fanno almeno altrettanto, p 0.19, Wilson 0.14 |
| plateau | passa | media 0.64 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 38 trade, netto 2,977,119, DD 1,053,893, net/DD 2.82, average trade 78,345 (soglia 32,362), UngerFit 4.24, finestre in utile 3/4 (-141,018 / 608,927 / 767,191 / 1,742,019).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 2.82 (serve 1), average trade 78,345 (soglia 32,362), finestre 3/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 28% del netto (massimo 40%); average trade / soglia per anno: 2020:1.7 2021:1.8 2022:2.7 2023:3.4 2024:1.2 2025:3.6 2026:3.2 |
| altri mercati | **no** | 3/7 con net/DD ≥ 1 (serve meta'): @FDAX 0.34, @NQ 3.96, @ES 1.96, @YM 1.49, @FESX 0.04, @FCE 0.01, @Z -0.15 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=2.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=-1, FastYesLong=152, FastYesShort=46, FastNoLong=153, FastNoShort=153, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

658 simulazioni in 10.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=2, Direction=1 | ✔ | avg smussato 5,235.9, netto 1,350,871, 258 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 5,235.9, netto 1,350,871, 258 trade |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 11,949.2, netto 1,840,179, 154 trade; annullato dall'ultimo terzo (-1,864,119 -> -1,932,740) |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 624,142 (migliore 624,142), 258 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 54 | ✔ | batte lo spento: netto 2,129,557 contro 1,350,871, net/DD 2.17 contro 1.25 |
| NO | PtnNeutNo | 56 | 25 | ✔ | batte lo spento: netto 1,716,564 contro 2,129,557, net/DD 4.08 contro 2.17 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 28,609.4, netto 1,716,564, 60 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 1,770,993 (migliore 1,770,993), 60 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 27,735.4, netto 1,637,907, 60 trade |

**Storia di ricerca**: 91 trade, netto 1,554,753, DD 880,171, average trade 17,085, UngerFit 1.48.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 91 su almeno 50 |
| average trade | passa | 17,085 contro la soglia 15,109 |
| anni | passa | 4 anni, minimo 16 trade in un anno, 23.9 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -83,154 |
| ultimo terzo | **no** | netto -83,154, net/DD -0.09 (serve 1,3) |
| due terzi su tre | passa | 1,200,150 / 437,757 / -83,154 |
| outlier | **no** | trade migliore 33% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -2,682 con i pattern, -15,748 senza |
| pattern casuali | passa | 4 estrazioni su 31 fanno almeno altrettanto, p 0.16, Wilson 0.09 |
| plateau | passa | media 0.96 su 6 vicini, minimo 0.84 |

**Prova su FTMO**: 45 trade, netto 3,425,130, DD 1,436,412, net/DD 2.38, average trade 76,114 (soglia 32,362), UngerFit 3.53, finestre in utile 3/4 (187,248 / -1,099,637 / 1,497,920 / 2,839,600).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 2.38 (serve 1), average trade 76,114 (soglia 32,362), finestre 3/4 (servono 3) |
| anni | **no** | 5/6 anni in utile (serve 60%), anno migliore 57% del netto (massimo 40%); average trade / soglia per anno: 2021:4.6 2022:1.5 2023:0.2 2024:-2.0 2025:1.5 2026:7.2 |
| altri mercati | **no** | 3/7 con net/DD ≥ 1 (serve meta'): @FDAX 0.01, @NQ -0.36, @ES 0.44, @YM 1.04, @FESX 1.50, @FCE 3.20, @Z -0.72 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=54, PtnNeutNo=25, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=2, Direction=1`

## LF Level fader sul pivot di ieri

508 simulazioni in 6.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=2 | ✔ | avg smussato 10,767.3, netto 1,808,900, 168 trade |
| stop | StopAtr | 0.8 | 1.5 | · | D2: netto smussato 2,677,327 (migliore 2,720,750), 165 trade; annullato dall'ultimo terzo (-2,833,352 -> -3,254,193) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 6 | ✔ | batte lo spento: netto 1,871,864 contro 1,808,900, net/DD 3.51 contro 2.16 |
| NO | PtnNeutNo | 56 | 51 | · | batte lo spento: netto 1,829,043 contro 1,871,864, net/DD 6.01 contro 3.51; annullato dall'ultimo terzo (-495,705 -> -538,205) |
| direzionale YES | PtnDirYes | 52 | 47 | ✔ | batte lo spento: netto 2,049,043 contro 1,871,864, net/DD 5.55 contro 3.51 |
| calendario long | NotEntryDayLong | -1 | 2 | · | batte lo spento: netto 2,230,293 contro 2,049,043, net/DD 8.09 contro 5.55; annullato dall'ultimo terzo (-459,205 -> -511,705) |
| calendario short | NotEntryDayShort | -1 | 3 | ✔ | batte lo spento: netto 1,915,250 contro 2,049,043, net/DD 9.70 contro 5.55 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 37,553.9, netto 1,915,250, 51 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 1,844,804 (migliore 1,915,250), 52 trade; annullato dall'ultimo terzo (-346,705 -> -392,412) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 20 | ✔ | avg smussato 41,303.9, netto 2,106,500, 51 trade |

**Storia di ricerca**: 70 trade, netto 1,905,052, DD 470,743, average trade 27,215, UngerFit 3.23.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 70 su almeno 50 |
| average trade | passa | 27,215 contro la soglia 15,109 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 18.4 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -201,448 |
| ultimo terzo | **no** | netto -201,448, net/DD -0.43 (serve 1,3) |
| due terzi su tre | passa | 609,000 / 1,497,500 / -201,448 |
| outlier | passa | trade migliore 19% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -10,603 con i pattern, -28,164 senza |
| pattern casuali | passa | 7 estrazioni su 29 fanno almeno altrettanto, p 0.27, Wilson 0.18 |
| plateau | passa | media 0.86 su 5 vicini, minimo 0.45 |

**Prova su FTMO**: 38 trade, netto 677,872, DD 853,976, net/DD 0.79, average trade 17,839 (soglia 32,362), UngerFit 1.07, finestre in utile 3/4 (-499,514 / 382,120 / 553,500 / 241,767).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.79 (serve 1), average trade 17,839 (soglia 32,362), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 41% del netto (massimo 40%); average trade / soglia per anno: 2020:-5.0 2021:2.1 2022:2.7 2023:1.2 2024:-0.3 2025:0.8 2026:1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=20, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=6, PtnNeutNo=56, PtnDirYes=47, NotEntryDayLong=-1, NotEntryDayShort=3, LevelShift=2`

## LFHL Level fader sugli estremi di ieri

506 simulazioni in 6.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=0 | ✔ | avg smussato 7,203.6, netto 1,311,064, 182 trade |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 1,706,147 (migliore 1,740,028), 181 trade; annullato dall'ultimo terzo (-2,029,949 -> -2,262,513) |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 1,404,421 contro 1,311,064, net/DD 2.18 contro 2.04 |
| YES | PtnNeutYes | 55 | 29 | ✔ | batte lo spento: netto 1,597,307 contro 1,404,421, net/DD 2.39 contro 2.18 |
| NO | PtnNeutNo | 56 | 40 | ✔ | batte lo spento: netto 1,623,643 contro 1,597,307, net/DD 3.36 contro 2.39 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 1 | · | batte lo spento: netto 1,634,886 contro 1,623,643, net/DD 3.38 contro 3.36; annullato dall'ultimo terzo (-192,000 -> -344,500) |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 29,520.8, netto 1,623,643, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 1,698,707 (migliore 1,698,707), 55 trade; annullato dall'ultimo terzo (-192,000 -> -500,475) |
| affinamento target | TargetAtr | 2.0 | 2.0 | · | batte lo spento: netto 1,623,643 contro 1,530,286, net/DD 3.36 contro 3.17 |
| durata | MaxBars | 4 | 6 | · | avg smussato 28,938.2, netto 1,575,579, 55 trade; annullato dall'ultimo terzo (-192,000 -> -271,000) |

**Storia di ricerca**: 97 trade, netto 1,431,643, DD 1,088,000, average trade 14,759, UngerFit 1.15.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 97 su almeno 50 |
| average trade | **no** | 14,759 contro la soglia 15,109 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 25.5 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -192,000 |
| ultimo terzo | **no** | netto -192,000, net/DD -0.18 (serve 1,3) |
| due terzi su tre | passa | 1,270,636 / 353,007 / -192,000 |
| outlier | **no** | trade migliore 31% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -4,571 con i pattern, -17,807 senza |
| pattern casuali | **no** | 7 estrazioni su 25 fanno almeno altrettanto, p 0.31, Wilson 0.20 |
| plateau | passa | media 0.86 su 7 vicini, minimo 0.63 |

**Prova su FTMO**: 70 trade, netto 1,280,290, DD 1,726,352, net/DD 0.74, average trade 18,290 (soglia 32,362), UngerFit 0.77, finestre in utile 2/4 (738,775 / 1,773,951 / -455,483 / -776,953).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.74 (serve 1), average trade 18,290 (soglia 32,362), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 59% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.1 2021:4.5 2022:0.3 2023:0.9 2024:0.4 2025:1.8 2026:-0.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=29, PtnNeutNo=40, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=0`

## Riepilogo

- PCH: scartata; ricerca 393 trade netto 5,241,986 avg 13,338; prova 215 trade netto 2,744,423 net/DD 1.14 avg 12,765 fin 3/4; falliti: average trade, prova sul broker
- TFM: scartata; ricerca 228 trade netto 3,660,693 avg 16,056; prova 126 trade netto 1,959,184 net/DD 0.44 avg 15,549 fin 2/4; falliti: ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- TFU: scartata; ricerca 561 trade netto 7,041,170 avg 12,551; prova 309 trade netto 4,139,714 net/DD 1.09 avg 13,397 fin 4/4; falliti: average trade, ultimo terzo, pattern casuali, prova sul broker
- BO: scartata; ricerca 885 trade netto 1,813,244 avg 2,049; prova 463 trade netto 308,524 net/DD 0.06 avg 666 fin 1/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, anni
- BOS: scartata; ricerca 671 trade netto 1,799,644 avg 2,682; prova 469 trade netto -1,075,601 net/DD -0.19 avg -2,293 fin 2/4; falliti: average trade, outlier, plateau, prova sul broker, anni
- VBO: scartata; ricerca 289 trade netto 3,694,329 avg 12,783; prova 144 trade netto 8,407,464 net/DD 6.33 avg 58,385 fin 4/4; falliti: average trade, ultimo terzo, outlier, plateau, anni, altri mercati
- MAC: scartata; ricerca 122 trade netto 3,610,346 avg 29,593; prova 64 trade netto 1,800,904 net/DD 2.16 avg 28,139 fin 3/4; falliti: plateau, prova sul broker
- RBM: scartata; ricerca 115 trade netto 2,189,014 avg 19,035; prova 60 trade netto 1,322,897 net/DD 0.78 avg 22,048 fin 3/4; falliti: anni, outlier, plateau, prova sul broker
- RBU: scartata; ricerca 78 trade netto 3,283,091 avg 42,091; prova 38 trade netto 2,977,119 net/DD 2.82 avg 78,345 fin 3/4; falliti: anni, outlier, altri mercati
- RHL: scartata; ricerca 91 trade netto 1,554,753 avg 17,085; prova 45 trade netto 3,425,130 net/DD 2.38 avg 76,114 fin 3/4; falliti: utile recente, ultimo terzo, outlier, anni, altri mercati
- LF: scartata; ricerca 70 trade netto 1,905,052 avg 27,215; prova 38 trade netto 677,872 net/DD 0.79 avg 17,839 fin 3/4; falliti: anni, utile recente, ultimo terzo, prova sul broker, anni
- LFHL: scartata; ricerca 97 trade netto 1,431,643 avg 14,759; prova 70 trade netto 1,280,290 net/DD 0.74 avg 18,290 fin 2/4; falliti: average trade, anni, utile recente, ultimo terzo, outlier, pattern casuali, prova sul broker, anni
