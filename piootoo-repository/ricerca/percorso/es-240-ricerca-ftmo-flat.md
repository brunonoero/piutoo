# Percorso v4 — @ES 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (5,890 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,207 barre), mai vista dal percorso
- costi FTMO: spread 0.60 punti (mediana), swap long 1.6673 short 0.002 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 174 nella ricerca, 234 nella prova

## PCH Price Channel

548 simulazioni in 13.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=10, OffsetTicks=2, Direction=1 | ✔ | avg smussato 159.7, netto 52,394, 328 trade |
| finestra: inizio | StartHour | -1 | 10 | ✔ | avg smussato 252.1, netto 67,309, 267 trade |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 261.0, netto 74,126, 284 trade; annullato dall'ultimo terzo (9,979 -> 5,409) |
| stop | StopAtr | 0.8 | 0.4 | ✔ | D2: netto smussato 67,909 (migliore 70,637), 267 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 252.1, netto 67,305, 267 trade |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 3 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 36 | · | batte lo spento: netto 60,719 contro 67,305, net/DD 13.16 contro 11.87; annullato dall'ultimo terzo (21,595 -> 3,317) |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.4 | 0.5 | · | D2: netto smussato 70,636 (migliore 72,301), 267 trade; annullato dall'ultimo terzo (21,595 -> 17,377) |

**Storia di ricerca**: 417 trade, netto 88,900, DD 8,963, average trade 213, UngerFit 1.71.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 417 su almeno 50 |
| average trade | passa | 213 contro la soglia 174 |
| anni | passa | 5 anni, minimo 11 trade in un anno, 109.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 21,595 |
| ultimo terzo | passa | netto 21,595, net/DD 2.41 (serve 1,3) |
| due terzi su tre | passa | 18,157 / 49,148 / 21,595 |
| outlier | passa | trade migliore 6% del netto sulla storia, 19% sull'ultimo terzo |
| plateau | passa | media 0.91 su 6 vicini, minimo 0.72 |

**Prova su FTMO**: 236 trade, netto 2,176, DD 14,922, net/DD 0.15, average trade 9 (soglia 234), UngerFit 0.05, finestre in utile 3/4 (1,340 / 379 / -543 / 1,001).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.15 (serve 1), average trade 9 (soglia 234), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 45% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.9 2021:0.9 2022:1.8 2023:1.6 2024:0.9 2025:-0.1 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.4, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=10, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=10, OffsetTicks=2, Direction=1`

## TFM Trend following mirrored

426 simulazioni in 11.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -32,505 |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 90.4, netto 50,359, 557 trade; annullato dall'ultimo terzo (18,746 -> -3,521) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 17,913 (migliore 17,913), 731 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 29.9, netto 21,823, 731 trade |
| YES | PtnNeutYes | 55 | 25 | · | batte lo spento: netto 62,069 contro 21,823, net/DD 4.49 contro 1.09; annullato dall'ultimo terzo (36,839 -> 13,465) |
| NO | PtnNeutNo | 56 | 29 | · | batte lo spento: netto 62,069 contro 21,823, net/DD 4.49 contro 1.09; annullato dall'ultimo terzo (36,839 -> 12,737) |
| direzionale YES | PtnDirYes | 52 | -15 | · | batte lo spento: netto 49,206 contro 21,823, net/DD 5.32 contro 1.09; annullato dall'ultimo terzo (36,839 -> 10,400) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 51,069 contro 21,823, net/DD 4.28 contro 1.09; annullato dall'ultimo terzo (36,839 -> 14,605) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 32,437 contro 21,823, net/DD 2.44 contro 1.09 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 26,050 (migliore 26,050), 592 trade |

**Storia di ricerca**: 897 trade, netto 73,703, DD 13,318, average trade 82, UngerFit 0.54.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 897 su almeno 50 |
| average trade | **no** | 82 contro la soglia 174 |
| anni | passa | 5 anni, minimo 20 trade in un anno, 235.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 41,266 |
| ultimo terzo | passa | netto 41,266, net/DD 4.14 (serve 1,3) |
| due terzi su tre | passa | 12,789 / 19,717 / 41,266 |
| outlier | passa | trade migliore 9% del netto sulla storia, 12% sull'ultimo terzo |
| plateau | passa | media 0.73 su 1 vicini, minimo 0.73 |

**Prova su FTMO**: 453 trade, netto -39,174, DD 63,851, net/DD -0.61, average trade -86 (soglia 234), UngerFit , finestre in utile 2/4 (2,333 / 14,398 / -41,886 / -14,020).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.61 (serve 1), average trade -86 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 77% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.9 2021:0.3 2022:0.4 2023:0.6 2024:0.7 2025:-0.2 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0`

## TFU Trend following unmirrored

720 simulazioni in 18.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -32,505 |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 90.4, netto 50,359, 557 trade; annullato dall'ultimo terzo (18,746 -> -3,521) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 17,913 (migliore 17,913), 731 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 29.9, netto 21,823, 731 trade |
| YES long | FastYesLong | 152 | 12 | · | batte lo spento: netto 33,153 contro 21,823, net/DD 1.98 contro 1.09; annullato dall'ultimo terzo (36,839 -> 28,001) |
| YES short | FastYesShort | 152 | 77 | · | batte lo spento: netto 56,396 contro 21,823, net/DD 4.84 contro 1.09; annullato dall'ultimo terzo (36,839 -> 35,414) |
| NO long | FastNoLong | 153 | 128 | · | batte lo spento: netto 45,497 contro 21,823, net/DD 2.70 contro 1.09; annullato dall'ultimo terzo (36,839 -> 30,643) |
| NO short | FastNoShort | 153 | 4 | · | batte lo spento: netto 44,582 contro 21,823, net/DD 3.88 contro 1.09; annullato dall'ultimo terzo (36,839 -> 33,970) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 32,437 contro 21,823, net/DD 2.44 contro 1.09 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 26,050 (migliore 26,050), 592 trade |

**Storia di ricerca**: 897 trade, netto 73,703, DD 13,318, average trade 82, UngerFit 0.54.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 897 su almeno 50 |
| average trade | **no** | 82 contro la soglia 174 |
| anni | passa | 5 anni, minimo 20 trade in un anno, 235.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 41,266 |
| ultimo terzo | passa | netto 41,266, net/DD 4.14 (serve 1,3) |
| due terzi su tre | passa | 12,789 / 19,717 / 41,266 |
| outlier | passa | trade migliore 9% del netto sulla storia, 12% sull'ultimo terzo |
| plateau | passa | media 0.73 su 1 vicini, minimo 0.73 |

**Prova su FTMO**: 453 trade, netto -39,174, DD 63,851, net/DD -0.61, average trade -86 (soglia 234), UngerFit , finestre in utile 2/4 (2,333 / 14,398 / -41,886 / -14,020).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.61 (serve 1), average trade -86 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 77% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.9 2021:0.3 2022:0.4 2023:0.6 2024:0.7 2025:-0.2 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=0`

## BO Breakout di N sessioni

504 simulazioni in 13.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=0, IncludeCurrentSession=1 | ✔ | avg smussato 40.7, netto 25,468, 626 trade |
| finestra: inizio | StartHour | -1 | 10 | · | avg smussato 69.4, netto 38,373, 553 trade; annullato dall'ultimo terzo (14,892 -> 14,179) |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 85.7, netto 47,453, 554 trade; annullato dall'ultimo terzo (14,892 -> 3,663) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 31,223 (migliore 31,223), 662 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 58.2, netto 38,511, 662 trade |
| YES | PtnNeutYes | 55 | 25 | · | batte lo spento: netto 63,549 contro 38,511, net/DD 4.61 contro 2.34; annullato dall'ultimo terzo (38,676 -> 12,009) |
| NO | PtnNeutNo | 56 | 29 | · | batte lo spento: netto 63,549 contro 38,511, net/DD 4.61 contro 2.34; annullato dall'ultimo terzo (38,676 -> 11,281) |
| direzionale YES | PtnDirYes | 52 | -15 | · | batte lo spento: netto 54,557 contro 38,511, net/DD 7.49 contro 2.34; annullato dall'ultimo terzo (38,676 -> 11,384) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 53,042 contro 38,511, net/DD 4.57 contro 2.34; annullato dall'ultimo terzo (38,676 -> 19,114) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 46,150 contro 38,511, net/DD 2.85 contro 2.34; annullato dall'ultimo terzo (38,676 -> 25,865) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 49,888 contro 38,511, net/DD 3.03 contro 2.34 |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 42,699 (migliore 42,699), 662 trade |

**Storia di ricerca**: 1006 trade, netto 89,997, DD 16,463, average trade 89, UngerFit 0.53.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 1006 su almeno 50 |
| average trade | **no** | 89 contro la soglia 174 |
| anni | passa | 5 anni, minimo 22 trade in un anno, 264.0 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 40,108 |
| ultimo terzo | passa | netto 40,108, net/DD 3.27 (serve 1,3) |
| due terzi su tre | passa | 36,761 / 13,127 / 40,108 |
| outlier | passa | trade migliore 8% del netto sulla storia, 17% sull'ultimo terzo |
| plateau | passa | media 0.88 su 5 vicini, minimo 0.69 |

**Prova su FTMO**: 523 trade, netto -33,122, DD 61,162, net/DD -0.54, average trade -63 (soglia 234), UngerFit , finestre in utile 1/4 (241 / -4,460 / -22,960 / -4,687).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.54 (serve 1), average trade -63 (soglia 234), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 54% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.8 2021:0.7 2022:0.4 2023:0.2 2024:0.7 2025:-0.2 2026:-0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=1, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

534 simulazioni in 10.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=0 | ✔ | avg smussato 22.1, netto 15,466, 701 trade |
| finestra: inizio | StartHour | -1 | 6 | · | avg smussato 114.7, netto 77,685, 677 trade; annullato dall'ultimo terzo (36,255 -> 9,862) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 22.1, netto 15,466, 701 trade |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 51,471 (migliore 51,471), 794 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 70.7, netto 56,109, 794 trade |
| YES | PtnNeutYes | 55 | 11 | · | batte lo spento: netto 78,275 contro 56,109, net/DD 4.75 contro 2.02; annullato dall'ultimo terzo (36,783 -> 17,000) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 78,275 contro 56,109, net/DD 4.75 contro 2.02; annullato dall'ultimo terzo (36,783 -> 18,935) |
| direzionale YES | PtnDirYes | 52 | 49 | ✔ | batte lo spento: netto 59,975 contro 56,109, net/DD 2.36 contro 2.02 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 83,932 contro 59,975, net/DD 3.12 contro 2.36; annullato dall'ultimo terzo (45,107 -> 27,828) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 62,682 contro 59,975, net/DD 2.66 contro 2.36; annullato dall'ultimo terzo (45,107 -> 44,575) |
| affinamento stop | StopAtr | 0.5 | 0.6 | ✔ | D2: netto smussato 51,511 (migliore 51,511), 726 trade |

**Storia di ricerca**: 1095 trade, netto 95,095, DD 29,693, average trade 87, UngerFit 0.38.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 1095 su almeno 50 |
| average trade | **no** | 87 contro la soglia 174 |
| anni | passa | 5 anni, minimo 26 trade in un anno, 287.3 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 48,109 |
| ultimo terzo | passa | netto 48,109, net/DD 3.60 (serve 1,3) |
| due terzi su tre | passa | -18,036 / 65,021 / 48,109 |
| outlier | passa | trade migliore 10% del netto sulla storia, 12% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 130 con i pattern, 98 senza |
| pattern casuali | **no** | 1 estrazioni su 3 fanno almeno altrettanto, p 0.50, Wilson 0.20 |
| plateau | passa | media 0.98 su 3 vicini, minimo 0.93 |

**Prova su FTMO**: 598 trade, netto -11,465, DD 71,330, net/DD -0.16, average trade -19 (soglia 234), UngerFit , finestre in utile 2/4 (-23,183 / 4,178 / -40,027 / 50,080).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.16 (serve 1), average trade -19 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 70% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.8 2021:-0.1 2022:0.8 2023:0.5 2024:0.4 2025:-0.7 2026:0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=49, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=0`

## VBO Volatility breakout

457 simulazioni in 11.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.7, Direction=0 | ✔ | avg smussato 50.5, netto 17,743, 351 trade |
| finestra: inizio | StartHour | -1 | 14 | · | avg smussato 93.8, netto 27,568, 294 trade; annullato dall'ultimo terzo (14,223 -> -1,824) |
| finestra: fine | EndHour | -1 | 23 | ✔ | avg smussato 62.0, netto 17,743, 351 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 27,641 (migliore 27,641), 352 trade; annullato dall'ultimo terzo (14,223 -> 14,135) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 50.5, netto 17,743, 351 trade |
| YES | PtnNeutYes | 55 | 11 | · | batte lo spento: netto 38,546 contro 17,743, net/DD 3.47 contro 0.66; annullato dall'ultimo terzo (14,223 -> 86) |
| NO | PtnNeutNo | 56 | 17 | · | batte lo spento: netto 38,546 contro 17,743, net/DD 3.47 contro 0.66; annullato dall'ultimo terzo (14,223 -> 86) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 56,867 contro 17,743, net/DD 7.79 contro 0.66; annullato dall'ultimo terzo (14,223 -> 6,671) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 64,563 contro 17,743, net/DD 6.47 contro 0.66; annullato dall'ultimo terzo (14,223 -> 5,397) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 26,105 contro 17,743, net/DD 1.04 contro 0.66; annullato dall'ultimo terzo (14,223 -> 3,304) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 27,494 contro 17,743, net/DD 1.51 contro 0.66 |
| affinamento stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 24,756 (migliore 24,756), 352 trade |

**Storia di ricerca**: 534 trade, netto 38,972, DD 21,527, average trade 73, UngerFit 0.38.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 534 su almeno 50 |
| average trade | **no** | 73 contro la soglia 174 |
| anni | passa | 5 anni, minimo 12 trade in un anno, 140.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 19,235 |
| ultimo terzo | **no** | netto 19,235, net/DD 1.30 (serve 1,3) |
| due terzi su tre | passa | -2,277 / 22,015 / 19,235 |
| outlier | passa | trade migliore 15% del netto sulla storia, 24% sull'ultimo terzo |
| plateau | passa | media 0.81 su 6 vicini, minimo 0.37 |

**Prova su FTMO**: 290 trade, netto -7,613, DD 38,893, net/DD -0.20, average trade -26 (soglia 234), UngerFit , finestre in utile 3/4 (3,791 / 3,466 / -16,583 / 1,713).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.20 (serve 1), average trade -26 (soglia 234), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 77% del netto (massimo 40%); average trade / soglia per anno: 2020:0.9 2021:-0.3 2022:0.6 2023:0.3 2024:0.7 2025:-0.3 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=23, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.7, Direction=0`

## MAC Incrocio di medie

126 simulazioni in 2.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=20, SlowPeriod=30, Direction=0 | ✔ | avg smussato 146.7, netto 19,513, 133 trade |
| stop | StopAtr | 0.8 | 0.4 | ✔ | D2: netto smussato 31,321 (migliore 31,321), 133 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 199.8, netto 26,569, 133 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 29,656 contro 26,569, net/DD 2.53 contro 2.27 |
| affinamento stop | StopAtr | 0.4 | 0.4 | · | D2: netto smussato 34,409 (migliore 35,971), 133 trade |

**Storia di ricerca**: 196 trade, netto 41,149, DD 11,718, average trade 210, UngerFit 1.47.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 196 su almeno 50 |
| average trade | passa | 210 contro la soglia 174 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 51.4 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 11,492 |
| ultimo terzo | passa | netto 11,492, net/DD 2.20 (serve 1,3) |
| due terzi su tre | passa | 1,457 / 28,199 / 11,492 |
| outlier | **no** | trade migliore 15% del netto sulla storia, 33% sull'ultimo terzo |
| plateau | passa | media 0.60 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 103 trade, netto -3,870, DD 20,238, net/DD -0.19, average trade -38 (soglia 234), UngerFit , finestre in utile 2/4 (9,556 / -5,375 / -8,669 / 617).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.19 (serve 1), average trade -38 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 67% del netto (massimo 40%); average trade / soglia per anno: 2020:0.4 2021:0.3 2022:1.9 2023:0.4 2024:2.1 2025:-0.2 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.4, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=20, SlowPeriod=30, Direction=0`

## RBM Reversal Bollinger mirrored

467 simulazioni in 15.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=15, BbNumDevs=2.0 | ✔ | avg smussato 119.0, netto 55,675, 468 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 119.0, netto 55,675, 468 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 165.6, netto 48,343, 292 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 57,344 (migliore 58,396), 294 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 46 | · | batte lo spento: netto 54,800 contro 53,499, net/DD 8.16 contro 3.37; annullato dall'ultimo terzo (31,105 -> 11,660) |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 49,682 contro 53,499, net/DD 5.50 contro 3.37; annullato dall'ultimo terzo (31,105 -> 19,264) |
| direzionale YES | PtnDirYes | 52 | -27 | · | batte lo spento: netto 61,145 contro 53,499, net/DD 9.11 contro 3.37; annullato dall'ultimo terzo (31,105 -> 11,237) |
| direzionale NO | PtnDirNo | 53 | 21 | · | batte lo spento: netto 61,145 contro 53,499, net/DD 9.11 contro 3.37; annullato dall'ultimo terzo (31,105 -> 10,597) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 64,541 contro 53,499, net/DD 5.73 contro 3.37; annullato dall'ultimo terzo (31,105 -> 23,212) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 182.0, netto 53,499, 294 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 57,344 (migliore 59,895), 294 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 210.4, netto 61,857, 294 trade |

**Storia di ricerca**: 442 trade, netto 94,586, DD 15,057, average trade 214, UngerFit 1.32.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 442 su almeno 50 |
| average trade | passa | 214 contro la soglia 174 |
| anni | passa | 5 anni, minimo 11 trade in un anno, 116.0 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 32,729 |
| ultimo terzo | passa | netto 32,729, net/DD 3.77 (serve 1,3) |
| due terzi su tre | passa | 1,389 / 60,468 / 32,729 |
| outlier | passa | trade migliore 11% del netto sulla storia, 20% sull'ultimo terzo |
| plateau | **no** | media 0.33 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 219 trade, netto 27,165, DD 20,560, net/DD 1.32, average trade 124 (soglia 234), UngerFit 0.57, finestre in utile 2/4 (-10,018 / 27,223 / 16,905 / -6,945).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.32 (serve 1), average trade 124 (soglia 234), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 35% del netto (massimo 40%); average trade / soglia per anno: 2020:4.8 2021:-0.3 2022:1.5 2023:1.7 2024:0.7 2025:0.9 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=9, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=15, BbNumDevs=2.0`

## RBU Reversal Bollinger unmirrored

761 simulazioni in 23.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=15, BbNumDevs=2.0 | ✔ | avg smussato 119.0, netto 55,675, 468 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 119.0, netto 55,675, 468 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 165.6, netto 48,343, 292 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 57,344 (migliore 58,396), 294 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 150 | · | batte lo spento: netto 59,453 contro 53,499, net/DD 4.61 contro 3.37; annullato dall'ultimo terzo (31,105 -> 6,943) |
| YES short | FastYesShort | 152 | 29 | · | batte lo spento: netto 57,880 contro 53,499, net/DD 4.53 contro 3.37; annullato dall'ultimo terzo (31,105 -> 23,696) |
| NO long | FastNoLong | 153 | 143 | · | batte lo spento: netto 59,453 contro 53,499, net/DD 4.61 contro 3.37; annullato dall'ultimo terzo (31,105 -> 6,943) |
| NO short | FastNoShort | 153 | 25 | · | batte lo spento: netto 57,880 contro 53,499, net/DD 4.53 contro 3.37; annullato dall'ultimo terzo (31,105 -> 23,696) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 64,541 contro 53,499, net/DD 5.73 contro 3.37; annullato dall'ultimo terzo (31,105 -> 23,212) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 182.0, netto 53,499, 294 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 57,344 (migliore 59,895), 294 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 210.4, netto 61,857, 294 trade |

**Storia di ricerca**: 442 trade, netto 94,586, DD 15,057, average trade 214, UngerFit 1.32.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 442 su almeno 50 |
| average trade | passa | 214 contro la soglia 174 |
| anni | passa | 5 anni, minimo 11 trade in un anno, 116.0 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 32,729 |
| ultimo terzo | passa | netto 32,729, net/DD 3.77 (serve 1,3) |
| due terzi su tre | passa | 1,389 / 60,468 / 32,729 |
| outlier | passa | trade migliore 11% del netto sulla storia, 20% sull'ultimo terzo |
| plateau | **no** | media 0.33 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 219 trade, netto 27,165, DD 20,560, net/DD 1.32, average trade 124 (soglia 234), UngerFit 0.57, finestre in utile 2/4 (-10,018 / 27,223 / 16,905 / -6,945).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.32 (serve 1), average trade 124 (soglia 234), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 35% del netto (massimo 40%); average trade / soglia per anno: 2020:4.8 2021:-0.3 2022:1.5 2023:1.7 2024:0.7 2025:0.9 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=9, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1, BbLength=15, BbNumDevs=2.0`

## RHL Reversal sui livelli di ieri

415 simulazioni in 8.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=2 | ✔ | nessuna in utile: netto massimo -13,196 |
| finestra: inizio | StartHour | -1 | 17 | · | nessuna in utile: netto massimo -5,857; annullato dall'ultimo terzo (10,055 -> -24,464) |
| finestra: fine | EndHour | -1 | 5 | · | avg smussato 134.4, netto 18,008, 134 trade; annullato dall'ultimo terzo (10,055 -> 3,425) |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato -2,854 (migliore -2,854), 309 trade; annullato dall'ultimo terzo (10,055 -> 7,516) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 38 | · | batte lo spento: netto 19,766 contro -13,196, net/DD 1.04 contro -0.52; annullato dall'ultimo terzo (10,055 -> 2,117) |
| NO | PtnNeutNo | 56 | 31 | · | batte lo spento: netto 19,766 contro -13,196, net/DD 1.04 contro -0.52; annullato dall'ultimo terzo (10,055 -> 2,117) |
| direzionale YES | PtnDirYes | 52 | 36 | · | batte lo spento: netto 20,944 contro -13,196, net/DD 1.33 contro -0.52; annullato dall'ultimo terzo (10,055 -> -9,638) |
| direzionale NO | PtnDirNo | 53 | 1 | · | batte lo spento: netto 28,257 contro -13,196, net/DD 1.40 contro -0.52; annullato dall'ultimo terzo (10,055 -> 5,733) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (481 trade, netto -3,142).

## LF Level fader sul pivot di ieri

500 simulazioni in 6.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | avg smussato 36.5, netto 4,128, 113 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 1,784 (migliore 1,824), 115 trade |
| target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 13,823 contro -418, net/DD 0.97 contro -0.02 |
| YES | PtnNeutYes | 55 | 29 | ✔ | batte lo spento: netto 21,540 contro 13,823, net/DD 5.36 contro 0.97 |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 430.8, netto 21,540, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.6 | 0.4 | · | D2: netto smussato 20,341 (migliore 21,089), 50 trade; annullato dall'ultimo terzo (-5,183 -> -5,743) |
| affinamento target | TargetAtr | 0.5 | 0.5 | · | batte lo spento: netto 21,540 contro 11,447, net/DD 5.36 contro 1.68 |
| durata | MaxBars | 4 | 6 | · | avg smussato 408.7, netto 19,883, 50 trade; annullato dall'ultimo terzo (-5,183 -> -7,639) |

**Storia di ricerca**: 70 trade, netto 16,357, DD 7,886, average trade 234, UngerFit 2.00.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 70 su almeno 50 |
| average trade | passa | 234 contro la soglia 174 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 18.4 all'anno, 2 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -5,183 |
| ultimo terzo | **no** | netto -5,183, net/DD -0.79 (serve 1,3) |
| due terzi su tre | passa | 4,237 / 17,303 / -5,183 |
| outlier | passa | trade migliore 20% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -259 con i pattern, -141 senza |
| pattern casuali | **no** | 16 estrazioni su 20 fanno almeno altrettanto, p 0.81, Wilson 0.68 |
| plateau | passa | media 0.73 su 7 vicini, minimo 0.41 |

**Prova su FTMO**: 56 trade, netto 29,789, DD 3,925, net/DD 7.59, average trade 532 (soglia 234), UngerFit 5.55, finestre in utile 4/4 (3,296 / 17,317 / 1,631 / 7,545).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 7.59 (serve 1), average trade 532 (soglia 234), finestre 4/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 44% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.4 2021:2.5 2022:2.8 2023:-0.7 2024:-0.9 2025:2.7 2026:2.1 |
| altri mercati | passa | 4/7 con net/DD ≥ 1 (serve meta'): @FDAX -0.64, @NQ 1.84, @YM 1.82, @FESX -0.48, @FCE 1.18, @Z 1.05, @NIY 0.03 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0.5, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=29, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=10`

## LFHL Level fader sugli estremi di ieri

500 simulazioni in 6.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -35,961 |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato -36,520 (migliore -36,520), 174 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 2 | ✔ | batte lo spento: netto 11,089 contro -35,961, net/DD 3.06 contro -0.71 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | 2 | · | batte lo spento: netto 11,275 contro 11,089, net/DD 4.33 contro 3.06; annullato dall'ultimo terzo (4,997 -> 1,802) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 213.2, netto 11,089, 52 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 10,271 (migliore 10,679), 52 trade |
| affinamento target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 14,011 contro 11,089, net/DD 4.96 contro 3.06; annullato dall'ultimo terzo (4,997 -> 4,450) |
| durata | MaxBars | 4 | 6 | · | avg smussato 209.6, netto 10,807, 52 trade; annullato dall'ultimo terzo (4,997 -> 4,460) |

**Storia di ricerca**: 63 trade, netto 16,086, DD 3,624, average trade 255, UngerFit 3.22.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 63 su almeno 50 |
| average trade | passa | 255 contro la soglia 174 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 16.5 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,997 |
| ultimo terzo | passa | netto 4,997, net/DD 4.26 (serve 1,3) |
| due terzi su tre | passa | 4,236 / 6,853 / 4,997 |
| outlier | **no** | trade migliore 20% del netto sulla storia, 49% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 454 con i pattern, 49 senza |
| pattern casuali | passa | 3 estrazioni su 30 fanno almeno altrettanto, p 0.13, Wilson 0.07 |
| plateau | passa | media 0.61 su 5 vicini, minimo 0.05 |

**Prova su FTMO**: 35 trade, netto 7,120, DD 6,994, net/DD 1.02, average trade 203 (soglia 234), UngerFit 1.59, finestre in utile 2/4 (-3,109 / 537 / -3,076 / 12,768).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.02 (serve 1), average trade 203 (soglia 234), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 40% del netto (massimo 40%); average trade / soglia per anno: 2020:2.1 2021:1.7 2022:0.3 2023:2.7 2024:-1.1 2025:0.4 2026:3.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=2, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=20`

## Riepilogo

- PCH: scartata; ricerca 417 trade netto 88,900 avg 213; prova 236 trade netto 2,176 net/DD 0.15 avg 9 fin 3/4; falliti: prova sul broker, anni
- TFM: scartata; ricerca 897 trade netto 73,703 avg 82; prova 453 trade netto -39,174 net/DD -0.61 avg -86 fin 2/4; falliti: average trade, prova sul broker, anni
- TFU: scartata; ricerca 897 trade netto 73,703 avg 82; prova 453 trade netto -39,174 net/DD -0.61 avg -86 fin 2/4; falliti: average trade, prova sul broker, anni
- BO: scartata; ricerca 1006 trade netto 89,997 avg 89; prova 523 trade netto -33,122 net/DD -0.54 avg -63 fin 1/4; falliti: average trade, prova sul broker, anni
- BOS: scartata; ricerca 1095 trade netto 95,095 avg 87; prova 598 trade netto -11,465 net/DD -0.16 avg -19 fin 2/4; falliti: average trade, pattern casuali, prova sul broker, anni
- VBO: scartata; ricerca 534 trade netto 38,972 avg 73; prova 290 trade netto -7,613 net/DD -0.20 avg -26 fin 3/4; falliti: average trade, ultimo terzo, prova sul broker, anni
- MAC: scartata; ricerca 196 trade netto 41,149 avg 210; prova 103 trade netto -3,870 net/DD -0.19 avg -38 fin 2/4; falliti: anni, outlier, prova sul broker, anni
- RBM: scartata; ricerca 442 trade netto 94,586 avg 214; prova 219 trade netto 27,165 net/DD 1.32 avg 124 fin 2/4; falliti: plateau, prova sul broker
- RBU: scartata; ricerca 442 trade netto 94,586 avg 214; prova 219 trade netto 27,165 net/DD 1.32 avg 124 fin 2/4; falliti: plateau, prova sul broker
- RHL: abbandonato (R9: la base non guadagna su tutta la storia (481 trade, netto -3,142))
- LF: scartata; ricerca 70 trade netto 16,357 avg 234; prova 56 trade netto 29,789 net/DD 7.59 avg 532 fin 4/4; falliti: anni, utile recente, ultimo terzo, pattern utile, pattern casuali, anni
- LFHL: scartata; ricerca 63 trade netto 16,086 avg 255; prova 35 trade netto 7,120 net/DD 1.02 avg 203 fin 2/4; falliti: anni, outlier, prova sul broker
