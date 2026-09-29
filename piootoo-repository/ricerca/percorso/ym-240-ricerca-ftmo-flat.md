# Percorso v4 — @YM 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (5,890 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,206 barre), mai vista dal percorso
- costi FTMO: spread 2.10 punti (mediana), swap long 11.7286 short 0 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 126 nella ricerca, 160 nella prova

## PCH Price Channel

649 simulazioni in 11.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=1, OffsetTicks=5, Direction=1 | ✔ | avg smussato 17.4, netto 10,769, 620 trade |
| finestra: inizio | StartHour | -1 | 2 | · | avg smussato 26.9, netto 16,396, 610 trade; annullato dall'ultimo terzo (28,628 -> 26,687) |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 29.5, netto 17,752, 602 trade; annullato dall'ultimo terzo (28,628 -> 26,683) |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 19,754 (migliore 19,834), 620 trade; annullato dall'ultimo terzo (28,628 -> 21,413) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 17.4, netto 10,769, 620 trade |
| YES | PtnNeutYes | 55 | 34 | · | batte lo spento: netto 22,950 contro 10,769, net/DD 2.26 contro 0.34; annullato dall'ultimo terzo (28,628 -> 5,159) |
| NO | PtnNeutNo | 56 | 41 | · | batte lo spento: netto 22,950 contro 10,769, net/DD 2.26 contro 0.34; annullato dall'ultimo terzo (28,628 -> 5,159) |
| direzionale YES | PtnDirYes | 52 | 9 | ✔ | batte lo spento: netto 51,442 contro 10,769, net/DD 3.33 contro 0.34 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 55,258 contro 51,442, net/DD 3.58 contro 3.33; annullato dall'ultimo terzo (33,491 -> 31,745) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 51,245 (migliore 53,938), 544 trade |

**Storia di ricerca**: 835 trade, netto 84,933, DD 18,827, average trade 102, UngerFit 0.66.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 835 su almeno 50 |
| average trade | **no** | 102 contro la soglia 126 |
| anni | passa | 5 anni, minimo 17 trade in un anno, 219.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 33,491 |
| ultimo terzo | passa | netto 33,491, net/DD 1.78 (serve 1,3) |
| due terzi su tre | passa | 13,565 / 37,876 / 33,491 |
| outlier | passa | trade migliore 6% del netto sulla storia, 11% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 115 con i pattern, 89 senza |
| pattern casuali | **no** | 1 estrazioni su 2 fanno almeno altrettanto, p 0.67, Wilson 0.27 |
| plateau | passa | media 0.97 su 5 vicini, minimo 0.91 |

**Prova su FTMO**: 430 trade, netto 4,075, DD 30,863, net/DD 0.13, average trade 9 (soglia 160), UngerFit 0.04, finestre in utile 2/4 (-6,608 / 5,719 / 6,988 / -2,024).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.13 (serve 1), average trade 9 (soglia 160), finestre 2/4 (servono 3) |
| anni | passa | 5/7 anni in utile (serve 60%), anno migliore 28% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.5 2021:1.0 2022:0.7 2023:0.9 2024:0.3 2025:0.4 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=9, PtnDirNo=53, SkipDay=-1, ChannelBars=1, OffsetTicks=5, Direction=1`

## TFM Trend following mirrored

430 simulazioni in 12.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 13 | ✔ | nessuna in utile: netto massimo -31,125 |
| finestra: fine | EndHour | -1 | 16 | · | avg smussato 31.7, netto 13,784, 435 trade; annullato dall'ultimo terzo (30,962 -> 5,609) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -23,786 (migliore -23,786), 590 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -20,232 |
| YES | PtnNeutYes | 55 | 48 | · | batte lo spento: netto 17,606 contro -20,232, net/DD 2.61 contro -0.81; annullato dall'ultimo terzo (32,483 -> 5,033) |
| NO | PtnNeutNo | 56 | 5 | · | batte lo spento: netto 8,278 contro -20,232, net/DD 1.37 contro -0.81; annullato dall'ultimo terzo (32,483 -> -3,305) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 15,647 contro -20,232, net/DD 3.08 contro -0.81; annullato dall'ultimo terzo (32,483 -> 18,899) |
| direzionale NO | PtnDirNo | 53 | 24 | · | batte lo spento: netto 4,428 contro -20,232, net/DD 0.41 contro -0.81; annullato dall'ultimo terzo (32,483 -> 23,728) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 1,872 contro -20,232, net/DD 0.18 contro -0.81; annullato dall'ultimo terzo (32,483 -> 31,877) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 9,912 contro -20,232, net/DD 0.96 contro -0.81; annullato dall'ultimo terzo (32,483 -> 20,349) |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato -23,786 (migliore -23,786), 590 trade |

**Storia di ricerca**: 879 trade, netto 11,667, DD 24,943, average trade 13, UngerFit 0.07.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 879 su almeno 50 |
| average trade | **no** | 13 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 9 trade in un anno, 230.6 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 31,899 |
| ultimo terzo | passa | netto 32,483, net/DD 5.70 (serve 1,3) |
| due terzi su tre | **no** | -8,633 / -10,608 / 32,483 |
| outlier | **no** | trade migliore 45% del netto sulla storia, 10% sull'ultimo terzo |
| plateau | passa | media 0.60 su 1 vicini, minimo 0.60 |

**Prova su FTMO**: 459 trade, netto 3,389, DD 29,834, net/DD 0.11, average trade 7 (soglia 160), UngerFit 0.03, finestre in utile 2/4 (14,175 / 11,361 / -20,624 / -1,524).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.11 (serve 1), average trade 7 (soglia 160), finestre 2/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 159% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.7 2021:-0.2 2022:-0.2 2023:0.6 2024:0.9 2025:0.2 2026:-0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=13, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

720 simulazioni in 17.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 13 | ✔ | nessuna in utile: netto massimo -31,125 |
| finestra: fine | EndHour | -1 | 16 | · | avg smussato 31.7, netto 13,784, 435 trade; annullato dall'ultimo terzo (30,962 -> 5,609) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -23,786 (migliore -23,786), 590 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -20,232 |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 47 | · | batte lo spento: netto 22,667 contro -20,232, net/DD 3.42 contro -0.81; annullato dall'ultimo terzo (32,483 -> 28,965) |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 16 | · | batte lo spento: netto 16,319 contro -20,232, net/DD 2.43 contro -0.81; annullato dall'ultimo terzo (32,483 -> 25,973) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 1,872 contro -20,232, net/DD 0.18 contro -0.81; annullato dall'ultimo terzo (32,483 -> 31,877) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 9,912 contro -20,232, net/DD 0.96 contro -0.81; annullato dall'ultimo terzo (32,483 -> 20,349) |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato -23,786 (migliore -23,786), 590 trade |

**Storia di ricerca**: 879 trade, netto 11,667, DD 24,943, average trade 13, UngerFit 0.07.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 879 su almeno 50 |
| average trade | **no** | 13 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 9 trade in un anno, 230.6 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 31,899 |
| ultimo terzo | passa | netto 32,483, net/DD 5.70 (serve 1,3) |
| due terzi su tre | **no** | -8,633 / -10,608 / 32,483 |
| outlier | **no** | trade migliore 45% del netto sulla storia, 10% sull'ultimo terzo |
| plateau | passa | media 0.60 su 1 vicini, minimo 0.60 |

**Prova su FTMO**: 459 trade, netto 3,389, DD 29,834, net/DD 0.11, average trade 7 (soglia 160), UngerFit 0.03, finestre in utile 2/4 (14,175 / 11,361 / -20,624 / -1,524).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.11 (serve 1), average trade 7 (soglia 160), finestre 2/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 159% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.7 2021:-0.2 2022:-0.2 2023:0.6 2024:0.9 2025:0.2 2026:-0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=13, EndHour=-1, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1`

## BO Breakout di N sessioni

712 simulazioni in 15.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=2, IncludeCurrentSession=1 | ✔ | nessuna in utile: netto massimo -34,982 |
| finestra: inizio | StartHour | -1 | 17 | · | nessuna in utile: netto massimo -13,259; annullato dall'ultimo terzo (18,971 -> 15,057) |
| finestra: fine | EndHour | -1 | 0 | · | nessuna in utile: netto massimo -10,051; annullato dall'ultimo terzo (18,971 -> 8,919) |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -22,427 (migliore -22,427), 354 trade; annullato dall'ultimo terzo (18,971 -> 17,757) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -34,982 |
| YES | PtnNeutYes | 55 | 23 | · | batte lo spento: netto 4,410 contro -34,982, net/DD 0.43 contro -0.97; annullato dall'ultimo terzo (18,971 -> 4,793) |
| NO | PtnNeutNo | 56 | 47 | ✔ | batte lo spento: netto 5,412 contro -34,982, net/DD 0.35 contro -0.97 |
| direzionale YES | PtnDirYes | 52 | 36 | · | batte lo spento: netto 11,991 contro 5,412, net/DD 1.47 contro 0.35; annullato dall'ultimo terzo (25,646 -> 16,448) |
| direzionale NO | PtnDirNo | 53 | 21 | · | batte lo spento: netto 9,266 contro 5,412, net/DD 1.92 contro 0.35; annullato dall'ultimo terzo (25,646 -> 1,459) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 9,679 contro 5,412, net/DD 1.12 contro 0.35; annullato dall'ultimo terzo (25,646 -> 25,450) |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 8,132 contro 5,412, net/DD 0.85 contro 0.35; annullato dall'ultimo terzo (25,646 -> 6,727) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 17,954 contro 5,412, net/DD 1.75 contro 0.35; annullato dall'ultimo terzo (25,646 -> 16,688) |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 11,460 (migliore 11,460), 179 trade; annullato dall'ultimo terzo (25,646 -> 23,831) |

**Storia di ricerca**: 288 trade, netto 31,057, DD 15,285, average trade 108, UngerFit 0.78.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 288 su almeno 50 |
| average trade | **no** | 108 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 75.6 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 25,646 |
| ultimo terzo | passa | netto 25,646, net/DD 4.91 (serve 1,3) |
| due terzi su tre | passa | -9,304 / 14,715 / 25,646 |
| outlier | passa | trade migliore 13% del netto sulla storia, 11% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 235 con i pattern, 101 senza |
| pattern casuali | passa | 0 estrazioni su 7 fanno almeno altrettanto, p 0.12, Wilson 0.04 |
| plateau | passa | media 0.90 su 6 vicini, minimo 0.76 |

**Prova su FTMO**: 144 trade, netto -3,274, DD 38,016, net/DD -0.09, average trade -23 (soglia 160), UngerFit , finestre in utile 1/4 (-1,650 / 28,507 / -8,848 / -19,739).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.09 (serve 1), average trade -23 (soglia 160), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 95% del netto (massimo 40%); average trade / soglia per anno: 2020:-10.3 2021:0.0 2022:0.5 2023:2.1 2024:1.6 2025:2.3 2026:-3.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=47, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=5, BreakoutOffsetTicks=2`

## BOS Breakout della sessione in corso

399 simulazioni in 10.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=5 | ✔ | nessuna in utile: netto massimo -60,156 |
| finestra: inizio | StartHour | -1 | 14 | · | avg smussato 6.3, netto 2,531, 405 trade; annullato dall'ultimo terzo (23,564 -> 10,058) |
| finestra: fine | EndHour | -1 | 0 | · | nessuna in utile: netto massimo -43,998; annullato dall'ultimo terzo (23,564 -> 11,154) |
| stop | StopAtr | 0.8 | 0.4 | ✔ | D2: netto smussato -40,798 (migliore -40,798), 856 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -44,402 |
| YES | PtnNeutYes | 55 | 34 | · | batte lo spento: netto 21,252 contro -44,402, net/DD 1.61 contro -0.94; annullato dall'ultimo terzo (33,668 -> -2,286) |
| NO | PtnNeutNo | 56 | 41 | · | batte lo spento: netto 21,252 contro -44,402, net/DD 1.61 contro -0.94; annullato dall'ultimo terzo (33,668 -> -2,286) |
| direzionale YES | PtnDirYes | 52 | -11 | · | batte lo spento: netto 14,087 contro -44,402, net/DD 1.72 contro -0.94; annullato dall'ultimo terzo (33,668 -> 1,316) |
| direzionale NO | PtnDirNo | 53 | -47 | · | batte lo spento: netto 7,901 contro -44,402, net/DD 0.48 contro -0.94; annullato dall'ultimo terzo (33,668 -> -789) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (1266 trade, netto -10,733).

## VBO Volatility breakout

459 simulazioni in 10.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.0, Direction=1 | ✔ | avg smussato 0.8, netto 60, 78 trade |
| finestra: inizio | StartHour | -1 | 16 | · | avg smussato 167.6, netto 7,776, 61 trade; annullato dall'ultimo terzo (10,068 -> 4,994) |
| finestra: fine | EndHour | -1 | 17 | · | avg smussato 1.4, netto 102, 72 trade; annullato dall'ultimo terzo (10,068 -> 8,934) |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 5,666 (migliore 5,666), 78 trade; annullato dall'ultimo terzo (10,068 -> 6,256) |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 5.5, netto 426, 78 trade; annullato dall'ultimo terzo (10,068 -> 8,281) |
| YES | PtnNeutYes | 55 | 32 | · | batte lo spento: netto 5,046 contro 60, net/DD 1.74 contro 0.01; annullato dall'ultimo terzo (10,068 -> 2,434) |
| NO | PtnNeutNo | 56 | 39 | · | batte lo spento: netto 5,046 contro 60, net/DD 1.74 contro 0.01; annullato dall'ultimo terzo (10,068 -> 2,434) |
| direzionale YES | PtnDirYes | 52 | 44 | · | batte lo spento: netto 4,905 contro 60, net/DD 1.69 contro 0.01; annullato dall'ultimo terzo (10,068 -> 7,897) |
| direzionale NO | PtnDirNo | 53 | 38 | · | batte lo spento: netto 5,953 contro 60, net/DD 2.03 contro 0.01; annullato dall'ultimo terzo (10,068 -> 5,548) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 4,502 contro 60, net/DD 1.02 contro 0.01; annullato dall'ultimo terzo (10,068 -> 5,415) |
| uscita a ora fissa | ExitHour | -1 | 16 | · | batte lo spento: netto 8,642 contro 60, net/DD 2.60 contro 0.01; annullato dall'ultimo terzo (10,068 -> 5,421) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 4,434 contro 60, net/DD 1.53 contro 0.01; annullato dall'ultimo terzo (10,068 -> 6,992) |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 2,580 (migliore 2,580), 78 trade |

**Storia di ricerca**: 135 trade, netto 13,649, DD 5,377, average trade 101, UngerFit 1.23.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 135 su almeno 50 |
| average trade | **no** | 101 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 35.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 10,429 |
| ultimo terzo | passa | netto 10,429, net/DD 2.97 (serve 1,3) |
| due terzi su tre | passa | -1,823 / 5,043 / 10,429 |
| outlier | passa | trade migliore 17% del netto sulla storia, 22% sull'ultimo terzo |
| plateau | passa | media 0.61 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 89 trade, netto 21,479, DD 7,107, net/DD 3.02, average trade 241 (soglia 160), UngerFit 2.26, finestre in utile 4/4 (6,620 / 4,518 / 3,645 / 6,696).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 3.02 (serve 1), average trade 241 (soglia 160), finestre 4/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 30% del netto (massimo 40%); average trade / soglia per anno: 2020:1.1 2021:-0.8 2022:1.0 2023:1.3 2024:2.2 2025:1.2 2026:1.5 |
| altri mercati | passa | 4/7 con net/DD ≥ 1 (serve meta'): @FDAX 3.19, @NQ 4.56, @ES 2.71, @FESX -0.29, @FCE 0.20, @Z -0.50, @NIY 2.65 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=1.0, Direction=1`

## MAC Incrocio di medie

129 simulazioni in 2.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=5, SlowPeriod=100, Direction=0 | ✔ | avg smussato 408.2, netto 30,614, 75 trade |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 32,748 (migliore 33,792), 73 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 464.7, netto 33,923, 73 trade; annullato dall'ultimo terzo (13,315 -> 13,114) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 38,958 contro 33,792, net/DD 10.89 contro 9.44 |
| affinamento stop | StopAtr | 1.25 | 1.25 | · | D2: netto smussato 37,914 (migliore 38,958), 73 trade |

**Storia di ricerca**: 112 trade, netto 52,273, DD 3,578, average trade 467, UngerFit 6.95.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 112 su almeno 50 |
| average trade | passa | 467 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 29.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 13,315 |
| ultimo terzo | passa | netto 13,315, net/DD 4.47 (serve 1,3) |
| due terzi su tre | passa | 21,235 / 17,723 / 13,315 |
| outlier | passa | trade migliore 7% del netto sulla storia, 21% sull'ultimo terzo |
| plateau | passa | media 0.61 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 64 trade, netto 4,140, DD 22,285, net/DD 0.19, average trade 65 (soglia 160), UngerFit 0.34, finestre in utile 3/4 (1,391 / 14,267 / 1,568 / -13,086).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.19 (serve 1), average trade 65 (soglia 160), finestre 3/4 (servono 3) |
| anni | passa | 5/7 anni in utile (serve 60%), anno migliore 35% del netto (massimo 40%); average trade / soglia per anno: 2020:-2.3 2021:6.0 2022:4.4 2023:2.1 2024:3.4 2025:3.2 2026:-3.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=5, SlowPeriod=100, Direction=0`

## RBM Reversal Bollinger mirrored

675 simulazioni in 12.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=2.0 | ✔ | avg smussato 150.7, netto 50,189, 333 trade |
| finestra: inizio | StartHour | -1 | 6 | ✔ | avg smussato 180.9, netto 53,546, 296 trade |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 190.1, netto 59,705, 314 trade; annullato dall'ultimo terzo (-19,773 -> -20,375) |
| stop | StopAtr | 0.8 | 1.5 | · | D2: netto smussato 57,795 (migliore 59,978), 296 trade; annullato dall'ultimo terzo (-19,773 -> -25,373) |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 56,991 contro 53,546, net/DD 5.53 contro 5.51 |
| YES | PtnNeutYes | 55 | 13 | ✔ | batte lo spento: netto 59,117 contro 56,991, net/DD 11.99 contro 5.53 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | -9 | · | batte lo spento: netto 57,439 contro 59,117, net/DD 12.53 contro 11.99; annullato dall'ultimo terzo (-4,599 -> -7,893) |
| direzionale NO | PtnDirNo | 53 | -4 | · | batte lo spento: netto 56,024 contro 59,117, net/DD 12.23 contro 11.99; annullato dall'ultimo terzo (-4,599 -> -7,893) |
| calendario | SkipDay | -1 | 2 | ✔ | batte lo spento: netto 58,214 contro 59,117, net/DD 12.06 contro 11.99 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 328.9, netto 58,214, 177 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 58,765 (migliore 59,041), 177 trade; annullato dall'ultimo terzo (-2,869 -> -5,438) |
| affinamento target | TargetAtr | 1.0 | 1.0 | · | batte lo spento: netto 58,214 contro 53,563, net/DD 12.06 contro 11.10 |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 355.6, netto 62,944, 177 trade |

**Storia di ricerca**: 253 trade, netto 60,608, DD 6,610, average trade 240, UngerFit 2.62.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 253 su almeno 50 |
| average trade | passa | 240 contro la soglia 126 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 66.4 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -2,336 |
| ultimo terzo | **no** | netto -2,336, net/DD -0.35 (serve 1,3) |
| due terzi su tre | passa | 22,142 / 40,802 / -2,336 |
| outlier | passa | trade migliore 6% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -31 con i pattern, -111 senza |
| pattern casuali | passa | 1 estrazioni su 6 fanno almeno altrettanto, p 0.29, Wilson 0.12 |
| plateau | passa | media 0.61 su 8 vicini, minimo 0.12 |

**Prova su FTMO**: 122 trade, netto -21,314, DD 27,970, net/DD -0.76, average trade -175 (soglia 160), UngerFit , finestre in utile 1/4 (-6,090 / -13,598 / 473 / -2,100).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.76 (serve 1), average trade -175 (soglia 160), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2020:7.5 2021:2.0 2022:2.2 2023:1.4 2024:-0.9 2025:-2.5 2026:0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=6, EndHour=-1, PtnNeutYes=13, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=2, BbLength=30, BbNumDevs=2.0`

## RBU Reversal Bollinger unmirrored

973 simulazioni in 27.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=2.0 | ✔ | avg smussato 150.7, netto 50,189, 333 trade |
| finestra: inizio | StartHour | -1 | 6 | ✔ | avg smussato 180.9, netto 53,546, 296 trade |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 190.1, netto 59,705, 314 trade; annullato dall'ultimo terzo (-19,773 -> -20,375) |
| stop | StopAtr | 0.8 | 1.5 | · | D2: netto smussato 57,795 (migliore 59,978), 296 trade; annullato dall'ultimo terzo (-19,773 -> -25,373) |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 56,991 contro 53,546, net/DD 5.53 contro 5.51 |
| YES long | FastYesLong | 152 | 100 | ✔ | batte lo spento: netto 54,912 contro 56,991, net/DD 6.67 contro 5.53 |
| YES short | FastYesShort | 152 | 128 | ✔ | batte lo spento: netto 58,104 contro 54,912, net/DD 12.29 contro 6.67 |
| NO long | FastNoLong | 153 | 41 | ✔ | batte lo spento: netto 50,676 contro 58,104, net/DD 14.15 contro 12.29 |
| NO short | FastNoShort | 153 | 61 | ✔ | batte lo spento: netto 51,041 contro 50,676, net/DD 15.69 contro 14.15 |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 49,261 contro 51,041, net/DD 16.39 contro 15.69 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 834.9, netto 49,261, 59 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 46,997 (migliore 48,695), 59 trade |
| affinamento target | TargetAtr | 1.0 | 0 | · | nessun valore sopra lo spento; annullato dall'ultimo terzo (2,444 -> 649) |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 792.5, netto 46,759, 59 trade |

**Storia di ricerca**: 85 trade, netto 49,305, DD 3,758, average trade 580, UngerFit 8.42.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 85 su almeno 50 |
| average trade | passa | 580 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 22.3 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,546 |
| ultimo terzo | **no** | netto 2,546, net/DD 0.68 (serve 1,3) |
| due terzi su tre | passa | 14,167 / 32,592 / 2,546 |
| outlier | **no** | trade migliore 7% del netto sulla storia, 97% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 98 con i pattern, -93 senza |
| pattern casuali | passa | 8 estrazioni su 139 fanno almeno altrettanto, p 0.06, Wilson 0.04 |
| plateau | passa | media 0.73 su 8 vicini, minimo 0.16 |

**Prova su FTMO**: 31 trade, netto -7,752, DD 11,854, net/DD -0.65, average trade -250 (soglia 160), UngerFit , finestre in utile 1/4 (-5,269 / -1,408 / 1,028 / -2,103).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.65 (serve 1), average trade -250 (soglia 160), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 67% del netto (massimo 40%); average trade / soglia per anno: 2020:8.8 2021:2.0 2022:6.6 2023:5.7 2024:-1.4 2025:-2.4 2026:3.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=6, EndHour=-1, FastYesLong=100, FastYesShort=128, FastNoLong=41, FastNoShort=61, SkipDay=0, BbLength=30, BbNumDevs=2.0`

## RHL Reversal sui livelli di ieri

669 simulazioni in 22.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=0 | ✔ | avg smussato 75.9, netto 42,946, 566 trade |
| finestra: inizio | StartHour | -1 | 2 | ✔ | avg smussato 82.7, netto 45,595, 551 trade |
| finestra: fine | EndHour | -1 | 4 | · | avg smussato 150.6, netto 38,995, 259 trade; annullato dall'ultimo terzo (5,330 -> -7,374) |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato 46,803 (migliore 49,211), 549 trade; annullato dall'ultimo terzo (5,330 -> 1,287) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 29 | · | batte lo spento: netto 49,423 contro 45,595, net/DD 4.35 contro 2.15; annullato dall'ultimo terzo (5,330 -> 1,335) |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 49,423 contro 45,595, net/DD 4.35 contro 2.15; annullato dall'ultimo terzo (5,330 -> 1,335) |
| direzionale YES | PtnDirYes | 52 | 36 | · | batte lo spento: netto 40,991 contro 45,595, net/DD 3.55 contro 2.15; annullato dall'ultimo terzo (5,330 -> -1,215) |
| direzionale NO | PtnDirNo | 53 | 46 | ✔ | batte lo spento: netto 61,121 contro 45,595, net/DD 4.45 contro 2.15 |
| calendario | SkipDay | -1 | 4 | ✔ | batte lo spento: netto 68,758 contro 61,121, net/DD 6.03 contro 4.45 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 206.5, netto 68,758, 333 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 66,665 (migliore 66,665), 328 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 183.8, netto 57,336, 326 trade; annullato dall'ultimo terzo (9,972 -> 6,553) |

**Storia di ricerca**: 501 trade, netto 75,467, DD 14,274, average trade 151, UngerFit 1.12.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 501 su almeno 50 |
| average trade | passa | 151 contro la soglia 126 |
| anni | passa | 5 anni, minimo 8 trade in un anno, 131.5 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 9,972 |
| ultimo terzo | **no** | netto 9,972, net/DD 0.93 (serve 1,3) |
| due terzi su tre | passa | 18,478 / 47,018 / 9,972 |
| outlier | **no** | trade migliore 9% del netto sulla storia, 38% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 58 con i pattern, 35 senza |
| pattern casuali | passa | 1 estrazioni su 4 fanno almeno altrettanto, p 0.40, Wilson 0.16 |
| plateau | passa | media 0.87 su 5 vicini, minimo 0.72 |

**Prova su FTMO**: 258 trade, netto 25,888, DD 13,888, net/DD 1.86, average trade 100 (soglia 160), UngerFit 0.67, finestre in utile 3/4 (7,047 / 9,666 / -3,037 / 10,764).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.86 (serve 1), average trade 100 (soglia 160), finestre 3/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 27% del netto (massimo 40%); average trade / soglia per anno: 2020:3.1 2021:1.6 2022:1.1 2023:0.3 2024:1.3 2025:0.2 2026:1.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=2, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=46, SkipDay=4, LevelOffsetTicks=20, Direction=0`

## LF Level fader sul pivot di ieri

510 simulazioni in 9.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=2 | ✔ | avg smussato 142.3, netto 18,777, 132 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 18,035 (migliore 18,035), 132 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 20,825 contro 18,777, net/DD 2.16 contro 1.95; annullato dall'ultimo terzo (-1,828 -> -2,278) |
| YES | PtnNeutYes | 55 | 13 | ✔ | batte lo spento: netto 20,290 contro 18,777, net/DD 2.25 contro 1.95 |
| NO | PtnNeutNo | 56 | 35 | ✔ | batte lo spento: netto 21,506 contro 20,290, net/DD 3.45 contro 2.25 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 2 | · | batte lo spento: netto 21,024 contro 21,506, net/DD 3.75 contro 3.45; annullato dall'ultimo terzo (-1,020 -> -1,223) |
| calendario short | NotEntryDayShort | -1 | 4 | · | batte lo spento: netto 23,317 contro 21,506, net/DD 5.18 contro 3.45; annullato dall'ultimo terzo (-1,020 -> -1,275) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 364.5, netto 21,506, 59 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 20,767 (migliore 21,251), 59 trade |
| affinamento target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 21,909 contro 20,730, net/DD 3.34 contro 3.16 |
| durata | MaxBars | 4 | 8 | · | avg smussato 374.9, netto 22,119, 59 trade; annullato dall'ultimo terzo (-352 -> -1,445) |

**Storia di ricerca**: 92 trade, netto 21,556, DD 6,553, average trade 234, UngerFit 2.58.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 92 su almeno 50 |
| average trade | passa | 234 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 24.1 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -352 |
| ultimo terzo | **no** | netto -352, net/DD -0.09 (serve 1,3) |
| due terzi su tre | passa | 11,970 / 9,939 / -352 |
| outlier | passa | trade migliore 9% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -11 con i pattern, -36 senza |
| pattern casuali | passa | 4 estrazioni su 14 fanno almeno altrettanto, p 0.33, Wilson 0.20 |
| plateau | passa | media 0.93 su 7 vicini, minimo 0.82 |

**Prova su FTMO**: 37 trade, netto 6,792, DD 5,758, net/DD 1.18, average trade 184 (soglia 160), UngerFit 1.91, finestre in utile 3/4 (896 / 3,272 / 3,959 / -1,334).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 1.18 (serve 1), average trade 184 (soglia 160), finestre 3/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 41% del netto (massimo 40%); average trade / soglia per anno: 2020:6.3 2021:2.1 2022:3.3 2023:-0.3 2024:0.2 2025:2.2 2026:0.7 |
| altri mercati | **no** | 2/7 con net/DD ≥ 1 (serve meta'): @FDAX -0.31, @NQ 1.24, @ES 2.43, @FESX -0.96, @FCE -0.24, @Z 0.14, @NIY 0.27 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0.5, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=13, PtnNeutNo=35, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=2`

## LFHL Level fader sugli estremi di ieri

508 simulazioni in 11.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=0 | ✔ | avg smussato 86.8, netto 19,259, 222 trade |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 18,783 (migliore 19,419), 224 trade; annullato dall'ultimo terzo (-8,321 -> -12,336) |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 21,854 contro 19,259, net/DD 1.66 contro 1.21 |
| YES | PtnNeutYes | 55 | 53 | ✔ | batte lo spento: netto 27,357 contro 21,854, net/DD 4.63 contro 1.66 |
| NO | PtnNeutNo | 56 | 48 | ✔ | batte lo spento: netto 28,140 contro 27,357, net/DD 8.51 contro 4.63 |
| direzionale YES | PtnDirYes | 52 | -44 | ✔ | batte lo spento: netto 29,377 contro 28,140, net/DD 12.22 contro 8.51 |
| calendario long | NotEntryDayLong | -1 | 3 | ✔ | batte lo spento: netto 32,475 contro 29,377, net/DD 13.51 contro 12.22 |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 601.4, netto 32,475, 54 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 31,153 (migliore 31,919), 54 trade; annullato dall'ultimo terzo (3,783 -> 188) |
| affinamento target | TargetAtr | 1.0 | 1.0 | · | batte lo spento: netto 32,475 contro 30,772, net/DD 13.51 contro 12.80 |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 567.1, netto 29,697, 54 trade |

**Storia di ricerca**: 72 trade, netto 34,530, DD 2,404, average trade 480, UngerFit 8.71.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 72 su almeno 50 |
| average trade | passa | 480 contro la soglia 126 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 18.9 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,833 |
| ultimo terzo | passa | netto 4,833, net/DD 2.15 (serve 1,3) |
| due terzi su tre | passa | 7,568 / 22,129 / 4,833 |
| outlier | **no** | trade migliore 9% del netto sulla storia, 41% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 269 con i pattern, 5 senza |
| pattern casuali | passa | 3 estrazioni su 33 fanno almeno altrettanto, p 0.12, Wilson 0.06 |
| plateau | passa | media 0.92 su 7 vicini, minimo 0.68 |

**Prova su FTMO**: 35 trade, netto 2,721, DD 6,558, net/DD 0.41, average trade 78 (soglia 160), UngerFit 0.76, finestre in utile 3/4 (1,955 / 6,429 / 633 / -6,297).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.41 (serve 1), average trade 78 (soglia 160), finestre 3/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 40% del netto (massimo 40%); average trade / soglia per anno: 2020:4.8 2021:3.4 2022:3.6 2023:3.7 2024:3.5 2025:0.7 2026:-0.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=53, PtnNeutNo=48, PtnDirYes=-44, NotEntryDayLong=3, NotEntryDayShort=-1, LevelShift=0`

## Riepilogo

- PCH: scartata; ricerca 835 trade netto 84,933 avg 102; prova 430 trade netto 4,075 net/DD 0.13 avg 9 fin 2/4; falliti: average trade, pattern casuali, prova sul broker
- TFM: scartata; ricerca 879 trade netto 11,667 avg 13; prova 459 trade netto 3,389 net/DD 0.11 avg 7 fin 2/4; falliti: average trade, anni, due terzi su tre, outlier, prova sul broker, anni
- TFU: scartata; ricerca 879 trade netto 11,667 avg 13; prova 459 trade netto 3,389 net/DD 0.11 avg 7 fin 2/4; falliti: average trade, anni, due terzi su tre, outlier, prova sul broker, anni
- BO: scartata; ricerca 288 trade netto 31,057 avg 108; prova 144 trade netto -3,274 net/DD -0.09 avg -23 fin 1/4; falliti: average trade, anni, prova sul broker, anni
- BOS: abbandonato (R9: la base non guadagna su tutta la storia (1266 trade, netto -10,733))
- VBO: scartata; ricerca 135 trade netto 13,649 avg 101; prova 89 trade netto 21,479 net/DD 3.02 avg 241 fin 4/4; falliti: average trade, anni
- MAC: scartata; ricerca 112 trade netto 52,273 avg 467; prova 64 trade netto 4,140 net/DD 0.19 avg 65 fin 3/4; falliti: anni, prova sul broker
- RBM: scartata; ricerca 253 trade netto 60,608 avg 240; prova 122 trade netto -21,314 net/DD -0.76 avg -175 fin 1/4; falliti: utile recente, ultimo terzo, prova sul broker, anni
- RBU: scartata; ricerca 85 trade netto 49,305 avg 580; prova 31 trade netto -7,752 net/DD -0.65 avg -250 fin 1/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- RHL: scartata; ricerca 501 trade netto 75,467 avg 151; prova 258 trade netto 25,888 net/DD 1.86 avg 100 fin 3/4; falliti: ultimo terzo, outlier, prova sul broker
- LF: scartata; ricerca 92 trade netto 21,556 avg 234; prova 37 trade netto 6,792 net/DD 1.18 avg 184 fin 3/4; falliti: anni, utile recente, ultimo terzo, anni, altri mercati
- LFHL: scartata; ricerca 72 trade netto 34,530 avg 480; prova 35 trade netto 2,721 net/DD 0.41 avg 78 fin 3/4; falliti: anni, outlier, prova sul broker
