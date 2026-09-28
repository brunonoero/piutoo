# Percorso v4 — @SI 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (22,992 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,284 barre), mai vista dal percorso
- costi FTMO: spread 0.056 punti (mediana), swap long 0.0165 short 0 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 150 nella ricerca, 324 nella prova

## PCH Price Channel

506 simulazioni in 27.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=100, OffsetTicks=0, Direction=2 | ✔ | nessuna in utile: netto massimo -31,034 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -16,824 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -16,824 |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -17,368 (migliore -17,368), 56 trade; annullato dall'ultimo terzo (-11,220 -> -12,186) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -9,358 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (74 trade, netto -22,534).

## TFM Trend following mirrored

632 simulazioni in 26.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -109,792 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -99,931 |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato -95,064 (migliore -95,064), 313 trade; annullato dall'ultimo terzo (-39,296 -> -44,628) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -94,913 |
| YES | PtnNeutYes | 55 | 2 | ✔ | batte lo spento: netto 6,871 contro -94,913, net/DD 0.38 contro -0.90 |
| NO | PtnNeutNo | 56 | 22 | ✔ | batte lo spento: netto 19,292 contro 6,871, net/DD 1.41 contro 0.38 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 37 | ✔ | batte lo spento: netto 25,180 contro 19,292, net/DD 3.26 contro 1.41 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 25,258 (migliore 25,297), 50 trade; annullato dall'ultimo terzo (11,744 -> 10,529) |

**Storia di ricerca**: 76 trade, netto 36,924, DD 7,719, average trade 486, UngerFit 4.52.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 76 su almeno 50 |
| average trade | passa | 486 contro la soglia 150 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 19.9 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 11,744 |
| ultimo terzo | passa | netto 11,744, net/DD 1.78 (serve 1,3) |
| due terzi su tre | passa | 19,798 / 5,382 / 11,744 |
| outlier | **no** | trade migliore 30% del netto sulla storia, 44% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 452 con i pattern, -235 senza |
| pattern casuali | passa | 0 estrazioni su 37 fanno almeno altrettanto, p 0.03, Wilson 0.01 |
| plateau | passa | media 0.85 su 2 vicini, minimo 0.73 |

**Prova su FTMO**: 28 trade, netto 70,909, DD 10,568, net/DD 6.71, average trade 2,532 (soglia 324), UngerFit 13.69, finestre in utile 3/4 (-8,284 / 1,238 / 33,950 / 43,625).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 6.71 (serve 1), average trade 2,532 (soglia 324), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 63% del netto (massimo 40%); average trade / soglia per anno: 2020:5.0 2021:6.2 2022:2.1 2023:3.0 2024:-1.5 2025:3.3 2026:9.1 |
| altri mercati | **no** | 0/2 con net/DD ≥ 1 (serve meta'): @GC -0.24, @PL -1.00 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=22, EndHour=22, PtnNeutYes=2, PtnNeutNo=22, PtnDirYes=52, PtnDirNo=37, SkipDay=-1`

## TFU Trend following unmirrored

682 simulazioni in 34.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -109,792 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -99,931 |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato -95,064 (migliore -95,064), 313 trade; annullato dall'ultimo terzo (-39,296 -> -44,628) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -94,913 |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 152 | · | nessun valore sopra lo spento |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 153 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (474 trade, netto -134,679).

## BO Breakout di N sessioni

702 simulazioni in 43.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=0, IncludeCurrentSession=0 | ✔ | nessuna in utile: netto massimo -103,535 |
| finestra: inizio | StartHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -48,279 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -46,009 |
| stop | StopAtr | 0.8 | 2.5 | ✔ | D2: netto smussato -39,048 (migliore -39,048), 156 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -39,907 |
| YES | PtnNeutYes | 55 | 12 | ✔ | batte lo spento: netto 6,808 contro -39,907, net/DD 0.71 contro -0.91 |
| NO | PtnNeutNo | 56 | 23 | ✔ | batte lo spento: netto 15,133 contro 6,808, net/DD 1.89 contro 0.71 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 16,185 contro 15,133, net/DD 2.02 contro 1.89; annullato dall'ultimo terzo (2,218 -> 1,546) |
| affinamento stop | StopAtr | 2.5 | 1.5 | ✔ | D2: netto smussato 15,133 (migliore 15,133), 50 trade |

**Storia di ricerca**: 79 trade, netto 17,350, DD 15,638, average trade 220, UngerFit 1.43.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 79 su almeno 50 |
| average trade | passa | 220 contro la soglia 150 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 20.7 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,218 |
| ultimo terzo | **no** | netto 2,218, net/DD 0.14 (serve 1,3) |
| due terzi su tre | passa | 11,663 / 3,470 / 2,218 |
| outlier | **no** | trade migliore 30% del netto sulla storia, 233% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 76 con i pattern, -172 senza |
| pattern casuali | **no** | 7 estrazioni su 22 fanno almeno altrettanto, p 0.35, Wilson 0.23 |
| plateau | passa | media 0.92 su 5 vicini, minimo 0.66 |

**Prova su FTMO**: 34 trade, netto -17,205, DD 78,708, net/DD -0.22, average trade -506 (soglia 324), UngerFit , finestre in utile 3/4 (3,598 / 1,838 / -30,438 / 7,798).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.22 (serve 1), average trade -506 (soglia 324), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 11,159% del netto (massimo 40%); average trade / soglia per anno: 2020:4.9 2021:2.9 2022:1.9 2023:5.4 2024:-3.2 2025:2.5 2026:-3.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=22, EndHour=22, LevelSource=0, IncludeCurrentSession=0, PtnNeutYes=12, PtnNeutNo=23, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=5, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

391 simulazioni in 23.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=5 | ✔ | nessuna in utile: netto massimo -266,190 |
| finestra: inizio | StartHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -15,852 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -15,477 |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato -15,754 (migliore -15,754), 53 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -5,255 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (90 trade, netto -29,301).

## VBO Volatility breakout

406 simulazioni in 20.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.5, Direction=0 | ✔ | nessuna in utile: netto massimo -51,262 |
| finestra: inizio | StartHour | -1 | 19 | ✔ | nessuna in utile: netto massimo -38,052 |
| finestra: fine | EndHour | -1 | 21 | · | nessuna in utile: netto massimo -22,423; annullato dall'ultimo terzo (-1,457 -> -5,310) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -31,295 (migliore -31,295), 56 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -22,533; annullato dall'ultimo terzo (-1,186 -> -1,908) |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (97 trade, netto -30,881).

## MAC Incrocio di medie

126 simulazioni in 5.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=10, SlowPeriod=200, Direction=2 | ✔ | avg smussato 109.8, netto 9,880, 90 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 10,795 (migliore 10,795), 90 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 102.3, netto 9,205, 90 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 4.0 | ✔ | batte lo spento: netto 20,503 contro 9,205, net/DD 0.82 contro 0.37 |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 23,033 (migliore 23,341), 91 trade |

**Storia di ricerca**: 134 trade, netto 15,625, DD 24,978, average trade 117, UngerFit 0.60.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 134 su almeno 50 |
| average trade | **no** | 117 contro la soglia 150 |
| anni | passa | 5 anni, minimo 5 trade in un anno, 35.2 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -4,878 |
| ultimo terzo | **no** | netto -4,878, net/DD -0.23 (serve 1,3) |
| due terzi su tre | passa | 1,886 / 18,617 / -4,878 |
| outlier | **no** | trade migliore 79% del netto sulla storia,  sull'ultimo terzo |
| plateau | passa | media 0.67 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 52 trade, netto 162,593, DD 49,727, net/DD 3.27, average trade 3,127 (soglia 324), UngerFit 7.79, finestre in utile 3/4 (8,493 / -3,539 / 110,651 / 47,039).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 3.27 (serve 1), average trade 3,127 (soglia 324), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 92% del netto (massimo 40%); average trade / soglia per anno: 2020:-8.4 2021:1.0 2022:3.9 2023:1.5 2024:0.0 2025:-2.4 2026:12.9 |
| altri mercati | **no** | 0/2 con net/DD ≥ 1 (serve meta'): @GC 0.10, @PL -0.69 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=4.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=10, SlowPeriod=200, Direction=2`

## RBM Reversal Bollinger mirrored

412 simulazioni in 22.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -63,925 |
| finestra: inizio | StartHour | -1 | 19 | ✔ | nessuna in utile: netto massimo -13,289 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -12,821 |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -13,969 (migliore -13,969), 51 trade; annullato dall'ultimo terzo (-9,506 -> -10,093) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (73 trade, netto -22,328).

## RBU Reversal Bollinger unmirrored

706 simulazioni in 32.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -63,925 |
| finestra: inizio | StartHour | -1 | 19 | ✔ | nessuna in utile: netto massimo -13,289 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -12,821 |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -13,969 (migliore -13,969), 51 trade; annullato dall'ultimo terzo (-9,506 -> -10,093) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 152 | · | nessun valore sopra lo spento |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 153 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (73 trade, netto -22,328).

## RHL Reversal sui livelli di ieri

407 simulazioni in 16.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=2 | ✔ | nessuna in utile: netto massimo -63,628 |
| finestra: inizio | StartHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -27,650 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -25,180 |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato -24,395 (migliore -24,395), 86 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (134 trade, netto -36,645).

## LF Level fader sul pivot di ieri

240 simulazioni in 9.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -43,716 |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato -43,437 (migliore -43,437), 209 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (313 trade, netto -76,172).

## LFHL Level fader sugli estremi di ieri

240 simulazioni in 9.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -69,017 |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -69,428 (migliore -69,428), 299 trade; annullato dall'ultimo terzo (-37,221 -> -38,246) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (431 trade, netto -106,238).

## Riepilogo

- PCH: abbandonato (R9: la base non guadagna su tutta la storia (74 trade, netto -22,534))
- TFM: scartata; ricerca 76 trade netto 36,924 avg 486; prova 28 trade netto 70,909 net/DD 6.71 avg 2,532 fin 3/4; falliti: anni, outlier, anni, altri mercati
- TFU: abbandonato (R9: la base non guadagna su tutta la storia (474 trade, netto -134,679))
- BO: scartata; ricerca 79 trade netto 17,350 avg 220; prova 34 trade netto -17,205 net/DD -0.22 avg -506 fin 3/4; falliti: anni, ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- BOS: abbandonato (R9: la base non guadagna su tutta la storia (90 trade, netto -29,301))
- VBO: abbandonato (R9: la base non guadagna su tutta la storia (97 trade, netto -30,881))
- MAC: scartata; ricerca 134 trade netto 15,625 avg 117; prova 52 trade netto 162,593 net/DD 3.27 avg 3,127 fin 3/4; falliti: average trade, utile recente, ultimo terzo, outlier, anni, altri mercati
- RBM: abbandonato (R9: la base non guadagna su tutta la storia (73 trade, netto -22,328))
- RBU: abbandonato (R9: la base non guadagna su tutta la storia (73 trade, netto -22,328))
- RHL: abbandonato (R9: la base non guadagna su tutta la storia (134 trade, netto -36,645))
- LF: abbandonato (R9: la base non guadagna su tutta la storia (313 trade, netto -76,172))
- LFHL: abbandonato (R9: la base non guadagna su tutta la storia (431 trade, netto -106,238))
