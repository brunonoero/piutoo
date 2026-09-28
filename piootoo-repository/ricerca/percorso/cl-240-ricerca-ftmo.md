# Percorso v4 — @CL 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (5,900 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,218 barre), mai vista dal percorso
- costi FTMO: spread 0.081 punti (mediana), swap long 0 short 0.2253 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 154 nella ricerca, 160 nella prova

## PCH Price Channel

500 simulazioni in 24.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=100, OffsetTicks=10, Direction=2 | ✔ | nessuna in utile: netto massimo -399 |
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -399 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -399 |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato -534 (migliore -534), 50 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -399 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (89 trade, netto -6,164).

## TFM Trend following mirrored

624 simulazioni in 23.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -51,374 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -51,374 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -39,550 (migliore -39,550), 389 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -32,441; annullato dall'ultimo terzo (-25,462 -> -25,870) |
| YES | PtnNeutYes | 55 | 49 | ✔ | batte lo spento: netto 10,006 contro -38,909, net/DD 1.46 contro -0.93 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 2 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 11,006 contro 10,006, net/DD 1.58 contro 1.46 |
| target | TargetAtr | 0 | 0 | · | nessuno dei 2 candidati batte lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 9,095 (migliore 9,095), 51 trade |

**Storia di ricerca**: 75 trade, netto 12,475, DD 6,975, average trade 166, UngerFit 1.60.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 75 su almeno 50 |
| average trade | passa | 166 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 19.7 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,468 |
| ultimo terzo | **no** | netto 1,468, net/DD 0.42 (serve 1,3) |
| due terzi su tre | passa | -1,408 / 12,415 / 1,468 |
| outlier | **no** | trade migliore 55% del netto sulla storia, 127% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 61 con i pattern, -108 senza |
| pattern casuali | passa | 3 estrazioni su 45 fanno almeno altrettanto, p 0.09, Wilson 0.05 |
| plateau | passa | media 0.78 su 2 vicini, minimo 0.58 |

**Prova su FTMO**: 63 trade, netto -9,046, DD 17,702, net/DD -0.51, average trade -144 (soglia 160), UngerFit , finestre in utile 1/4 (-5,377 / -4,078 / -683 / 1,092).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.51 (serve 1), average trade -144 (soglia 160), finestre 1/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 184% del netto (massimo 40%); average trade / soglia per anno: 2020:-4.8 2021:0.8 2022:1.2 2023:1.2 2024:-0.2 2025:-1.5 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=17, EndHour=-1, PtnNeutYes=49, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

921 simulazioni in 36.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -51,374 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -51,374 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -39,550 (migliore -39,550), 389 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -32,441; annullato dall'ultimo terzo (-25,462 -> -25,870) |
| YES long | FastYesLong | 152 | 104 | ✔ | batte lo spento: netto 3,405 contro -38,909, net/DD 0.16 contro -0.93 |
| YES short | FastYesShort | 152 | 130 | ✔ | batte lo spento: netto 36,579 contro 3,405, net/DD 5.50 contro 0.16 |
| NO long | FastNoLong | 153 | 80 | ✔ | batte lo spento: netto 41,929 contro 36,579, net/DD 13.37 contro 5.50 |
| NO short | FastNoShort | 153 | 124 | ✔ | batte lo spento: netto 42,877 contro 41,929, net/DD 16.57 contro 13.37 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 43,055 (migliore 43,055), 51 trade |

**Storia di ricerca**: 72 trade, netto 39,850, DD 5,654, average trade 553, UngerFit 5.93.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 72 su almeno 50 |
| average trade | passa | 553 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 18.9 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -3,027 |
| ultimo terzo | **no** | netto -3,027, net/DD -0.63 (serve 1,3) |
| due terzi su tre | passa | 670 / 42,208 / -3,027 |
| outlier | passa | trade migliore 25% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -144 con i pattern, -128 senza |
| pattern casuali | **no** | 95 estrazioni su 175 fanno almeno altrettanto, p 0.55, Wilson 0.50 |
| plateau | passa | media 0.81 su 1 vicini, minimo 0.81 |

**Prova su FTMO**: 50 trade, netto -7,113, DD 14,031, net/DD -0.51, average trade -142 (soglia 160), UngerFit , finestre in utile 2/4 (-5,494 / -6,715 / 1,130 / 3,967).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.51 (serve 1), average trade -142 (soglia 160), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 102% del netto (massimo 40%); average trade / soglia per anno: 2020:-4.9 2021:1.2 2022:5.8 2023:1.6 2024:-1.9 2025:-2.7 2026:0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=17, EndHour=-1, FastYesLong=104, FastYesShort=130, FastNoLong=80, FastNoShort=124, SkipDay=-1`

## BO Breakout di N sessioni

703 simulazioni in 31.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=20, IncludeCurrentSession=0 | ✔ | nessuna in utile: netto massimo -56,397 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -37,490 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -37,490 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -32,659 (migliore -32,659), 352 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -22,666; annullato dall'ultimo terzo (-17,741 -> -19,017) |
| YES | PtnNeutYes | 55 | 24 | ✔ | batte lo spento: netto 13,764 contro -23,763, net/DD 1.79 contro -0.81 |
| NO | PtnNeutNo | 56 | 48 | · | batte lo spento: netto 17,689 contro 13,764, net/DD 2.32 contro 1.79; annullato dall'ultimo terzo (1,914 -> -2,201) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 45 | · | batte lo spento: netto 19,811 contro 13,764, net/DD 4.64 contro 1.79; annullato dall'ultimo terzo (1,914 -> 1,110) |
| calendario | SkipDay | -1 | 4 | ✔ | batte lo spento: netto 16,084 contro 13,764, net/DD 2.97 contro 1.79 |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 18,039 contro 16,084, net/DD 4.32 contro 2.97; annullato dall'ultimo terzo (2,851 -> -646) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.5 | · | D2: netto smussato 22,131 (migliore 22,131), 68 trade; annullato dall'ultimo terzo (2,851 -> 2,548) |

**Storia di ricerca**: 105 trade, netto 18,935, DD 5,419, average trade 180, UngerFit 1.97.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 105 su almeno 50 |
| average trade | passa | 180 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 27.6 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,851 |
| ultimo terzo | **no** | netto 2,851, net/DD 1.23 (serve 1,3) |
| due terzi su tre | passa | 1,280 / 14,804 / 2,851 |
| outlier | **no** | trade migliore 51% del netto sulla storia, 112% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 77 con i pattern, -110 senza |
| pattern casuali | passa | 4 estrazioni su 29 fanno almeno altrettanto, p 0.17, Wilson 0.10 |
| plateau | passa | media 0.74 su 3 vicini, minimo 0.30 |

**Prova su FTMO**: 60 trade, netto -18,225, DD 21,722, net/DD -0.84, average trade -304 (soglia 160), UngerFit , finestre in utile 0/4 (-422 / -6,453 / -7,241 / -4,110).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.84 (serve 1), average trade -304 (soglia 160), finestre 0/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 2,156% del netto (massimo 40%); average trade / soglia per anno: 2020:-4.6 2021:0.6 2022:1.8 2023:0.7 2024:0.0 2025:-2.7 2026:-1.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=17, EndHour=-1, LevelSource=0, IncludeCurrentSession=0, PtnNeutYes=24, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=4, Sessions=1, BreakoutOffsetTicks=20`

## BOS Breakout della sessione in corso

391 simulazioni in 19.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=20 | ✔ | nessuna in utile: netto massimo -132,493 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -75,620 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -75,620 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -57,093 (migliore -57,093), 129 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -49,660 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (173 trade, netto -42,142).

## VBO Volatility breakout

551 simulazioni in 23.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.7, Direction=2 | ✔ | nessuna in utile: netto massimo -5,301 |
| finestra: inizio | StartHour | -1 | 18 | ✔ | avg smussato 134.5, netto 13,584, 101 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 134.5, netto 13,584, 101 trade |
| stop | StopAtr | 0.8 | 1.5 | · | D2: netto smussato 13,015 (migliore 13,329), 101 trade; annullato dall'ultimo terzo (-10,899 -> -13,421) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 134.5, netto 13,584, 101 trade |
| YES | PtnNeutYes | 55 | 5 | ✔ | batte lo spento: netto 17,882 contro 13,584, net/DD 0.88 contro 0.53 |
| NO | PtnNeutNo | 56 | 45 | ✔ | batte lo spento: netto 24,292 contro 17,882, net/DD 1.77 contro 0.88 |
| direzionale YES | PtnDirYes | 52 | 6 | ✔ | batte lo spento: netto 30,502 contro 24,292, net/DD 3.36 contro 1.77 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 33,398 contro 30,502, net/DD 3.70 contro 3.36 |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 37,475 contro 33,398, net/DD 4.16 contro 3.70 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 39,321 (migliore 40,245), 54 trade; annullato dall'ultimo terzo (-4,927 -> -5,911) |

**Storia di ricerca**: 79 trade, netto 31,632, DD 9,003, average trade 400, UngerFit 3.40.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 79 su almeno 50 |
| average trade | passa | 400 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 20.7 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -5,843 |
| ultimo terzo | **no** | netto -4,927, net/DD -0.63 (serve 1,3) |
| due terzi su tre | **no** | -3,283 / 42,956 / -4,927 |
| outlier | passa | trade migliore 22% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -205 con i pattern, -75 senza |
| pattern casuali | **no** | 16 estrazioni su 19 fanno almeno altrettanto, p 0.85, Wilson 0.72 |
| plateau | passa | media 0.93 su 7 vicini, minimo 0.65 |

**Prova su FTMO**: 50 trade, netto 2,216, DD 13,979, net/DD 0.16, average trade 44 (soglia 160), UngerFit 0.30, finestre in utile 1/4 (-2,706 / -7,173 / -703 / 12,797).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.16 (serve 1), average trade 44 (soglia 160), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 94% del netto (massimo 40%); average trade / soglia per anno: 2020:4.1 2021:-1.6 2022:6.1 2023:2.3 2024:-1.8 2025:-4.0 2026:2.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=18, EndHour=-1, PtnNeutYes=5, PtnNeutNo=45, PtnDirYes=6, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.7, Direction=2`

## MAC Incrocio di medie

128 simulazioni in 6.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=5, SlowPeriod=30, Direction=1 | ✔ | avg smussato 206.5, netto 20,240, 98 trade |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 30,037 (migliore 30,037), 98 trade; annullato dall'ultimo terzo (-12,787 -> -15,465) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 206.5, netto 20,240, 98 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 28,689 contro 20,240, net/DD 1.29 contro 0.63 |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 32,629 (migliore 32,629), 98 trade |

**Storia di ricerca**: 149 trade, netto 24,355, DD 19,284, average trade 163, UngerFit 0.95.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 149 su almeno 50 |
| average trade | passa | 163 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 39.1 all'anno, 2 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -8,637 |
| ultimo terzo | **no** | netto -8,637, net/DD -0.88 (serve 1,3) |
| due terzi su tre | passa | 13,682 / 19,310 / -8,637 |
| outlier | passa | trade migliore 25% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.46 su 6 vicini, minimo 0.04 |

**Prova su FTMO**: 70 trade, netto 18,327, DD 12,532, net/DD 1.46, average trade 262 (soglia 160), UngerFit 1.85, finestre in utile 3/4 (4,615 / -7,637 / 4,986 / 16,363).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 1.46 (serve 1), average trade 262 (soglia 160), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 58% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.3 2021:1.8 2022:2.5 2023:-1.0 2024:0.7 2025:-2.3 2026:3.7 |
| altri mercati | **no** | 0/1 con net/DD ≥ 1 (serve meta'): @BRN 0.69 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=5, SlowPeriod=30, Direction=1`

## RBM Reversal Bollinger mirrored

670 simulazioni in 29.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | avg smussato 17.4, netto 1,201, 69 trade |
| finestra: inizio | StartHour | -1 | 8 | ✔ | avg smussato 255.8, netto 13,251, 65 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 203.9, netto 13,251, 65 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 14,751 (migliore 14,751), 65 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 14,165 contro 13,251, net/DD 1.91 contro 1.78; annullato dall'ultimo terzo (-5,957 -> -5,971) |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 3 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 48 | ✔ | batte lo spento: netto 18,467 contro 13,251, net/DD 3.82 contro 1.78 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | -40 | · | batte lo spento: netto 19,139 contro 18,467, net/DD 4.16 contro 3.82; annullato dall'ultimo terzo (-3,440 -> -3,537) |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 324.0, netto 18,467, 57 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 20,296 contro 18,467, net/DD 4.17 contro 3.82 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 20,736 (migliore 20,736), 57 trade |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 20,344 contro 20,296, net/DD 4.18 contro 4.17; annullato dall'ultimo terzo (-2,576 -> -2,640) |
| durata | MaxBars | 4 | 6 | · | avg smussato 330.3, netto 18,090, 57 trade; annullato dall'ultimo terzo (-2,576 -> -2,681) |

**Storia di ricerca**: 85 trade, netto 17,720, DD 6,356, average trade 208, UngerFit 2.11.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 85 su almeno 50 |
| average trade | passa | 208 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 22.3 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -2,576 |
| ultimo terzo | **no** | netto -2,576, net/DD -0.62 (serve 1,3) |
| due terzi su tre | passa | 4,653 / 15,643 / -2,576 |
| outlier | **no** | trade migliore 45% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -92 con i pattern, -142 senza |
| pattern casuali | **no** | 5 estrazioni su 5 fanno almeno altrettanto, p 1.00, Wilson 0.75 |
| plateau | passa | media 0.67 su 7 vicini, minimo 0.12 |

**Prova su FTMO**: 47 trade, netto 5,359, DD 9,434, net/DD 0.57, average trade 114 (soglia 160), UngerFit 0.93, finestre in utile 2/4 (922 / 7,143 / -1,343 / -2,511).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.57 (serve 1), average trade 114 (soglia 160), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 80% del netto (massimo 40%); average trade / soglia per anno: 2020:0.6 2021:1.2 2022:3.7 2023:-1.8 2024:0.1 2025:2.7 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=22, StartHour=8, EndHour=-1, PtnNeutYes=55, PtnNeutNo=48, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

964 simulazioni in 43.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | avg smussato 17.4, netto 1,201, 69 trade |
| finestra: inizio | StartHour | -1 | 8 | ✔ | avg smussato 255.8, netto 13,251, 65 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 203.9, netto 13,251, 65 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 14,751 (migliore 14,751), 65 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 14,165 contro 13,251, net/DD 1.91 contro 1.78; annullato dall'ultimo terzo (-5,957 -> -5,971) |
| YES long | FastYesLong | 152 | 90 | ✔ | batte lo spento: netto 25,574 contro 13,251, net/DD 4.70 contro 1.78 |
| YES short | FastYesShort | 152 | 152 | · | nessuno dei 2 candidati batte lo spento |
| NO long | FastNoLong | 153 | 103 | ✔ | batte lo spento: netto 25,723 contro 25,574, net/DD 4.73 contro 4.70 |
| NO short | FastNoShort | 153 | 153 | · | nessuno dei 2 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 514.5, netto 25,723, 50 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 28,008 contro 25,723, net/DD 5.45 contro 4.73 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 27,635 (migliore 27,907), 50 trade |
| affinamento target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 31,272 contro 28,008, net/DD 6.09 contro 5.45 |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 660.1, netto 33,006, 50 trade |

**Storia di ricerca**: 72 trade, netto 32,775, DD 5,138, average trade 455, UngerFit 5.11.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 72 su almeno 50 |
| average trade | passa | 455 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 18.9 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -231 |
| ultimo terzo | **no** | netto -231, net/DD -0.07 (serve 1,3) |
| due terzi su tre | passa | 14,137 / 18,869 / -231 |
| outlier | passa | trade migliore 26% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -11 con i pattern, -145 senza |
| pattern casuali | passa | 15 estrazioni su 62 fanno almeno altrettanto, p 0.25, Wilson 0.19 |
| plateau | passa | media 0.67 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 41 trade, netto -4,029, DD 10,732, net/DD -0.38, average trade -98 (soglia 160), UngerFit , finestre in utile 2/4 (83 / 3,801 / -1,247 / -6,666).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.38 (serve 1), average trade -98 (soglia 160), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 71% del netto (massimo 40%); average trade / soglia per anno: 2020:0.6 2021:5.0 2022:4.1 2023:-0.7 2024:0.6 2025:1.2 2026:-1.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=8, EndHour=-1, FastYesLong=90, FastYesShort=152, FastNoLong=103, FastNoShort=153, SkipDay=-1, BbLength=50, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

670 simulazioni in 27.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=10, Direction=2 | ✔ | nessuna in utile: netto massimo -21,226 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -12,382 |
| finestra: fine | EndHour | -1 | 0 | ✔ | nessuna in utile: netto massimo -537 |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato 5,508 (migliore 5,508), 184 trade; annullato dall'ultimo terzo (4,550 -> 3,488) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 29 | · | batte lo spento: netto 23,856 contro -537, net/DD 3.68 contro -0.02; annullato dall'ultimo terzo (4,550 -> -3,225) |
| NO | PtnNeutNo | 56 | 25 | · | batte lo spento: netto 23,856 contro -537, net/DD 3.68 contro -0.02; annullato dall'ultimo terzo (4,550 -> -3,225) |
| direzionale YES | PtnDirYes | 52 | 31 | · | batte lo spento: netto 40,415 contro -537, net/DD 5.22 contro -0.02; annullato dall'ultimo terzo (4,550 -> 2,565) |
| direzionale NO | PtnDirNo | 53 | 2 | ✔ | batte lo spento: netto 36,297 contro -537, net/DD 6.69 contro -0.02 |
| calendario | SkipDay | -1 | 2 | ✔ | batte lo spento: netto 36,472 contro 36,297, net/DD 8.64 contro 6.69 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 473.7, netto 36,472, 77 trade |
| uscita a ora fissa | ExitHour | -1 | 21 | ✔ | batte lo spento: netto 36,923 contro 36,472, net/DD 8.75 contro 8.64 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 38,464 (migliore 39,956), 77 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 4 | · | avg smussato 297.2, netto 36,923, 77 trade |

**Storia di ricerca**: 113 trade, netto 42,982, DD 4,349, average trade 380, UngerFit 4.64.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 113 su almeno 50 |
| average trade | passa | 380 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 29.7 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 6,059 |
| ultimo terzo | passa | netto 6,059, net/DD 1.78 (serve 1,3) |
| due terzi su tre | passa | 13,809 / 23,113 / 6,059 |
| outlier | **no** | trade migliore 31% del netto sulla storia, 59% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 168 con i pattern, 13 senza |
| pattern casuali | passa | 3 estrazioni su 18 fanno almeno altrettanto, p 0.21, Wilson 0.11 |
| plateau | passa | media 0.81 su 8 vicini, minimo 0.13 |

**Prova su FTMO**: 45 trade, netto 5,025, DD 9,006, net/DD 0.56, average trade 112 (soglia 160), UngerFit 0.93, finestre in utile 1/4 (-1,484 / -763 / -4,140 / 11,412).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.56 (serve 1), average trade 112 (soglia 160), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 59% del netto (massimo 40%); average trade / soglia per anno: 2020:3.1 2021:2.5 2022:3.5 2023:1.8 2024:-0.5 2025:-2.3 2026:1.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=21, StartHour=17, EndHour=0, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=2, SkipDay=2, LevelOffsetTicks=10, Direction=2`

## LF Level fader sul pivot di ieri

500 simulazioni in 18.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -6,010 |
| stop | StopAtr | 0.8 | 2.0 | ✔ | D2: netto smussato -6,249 (migliore -6,249), 91 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 3 | ✔ | batte lo spento: netto 4,730 contro -6,249, net/DD 0.98 contro -0.49 |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 94.6, netto 4,730, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 2.0 | 1.25 | ✔ | D2: netto smussato 4,730 (migliore 4,730), 50 trade |
| affinamento target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 5,614 contro 4,730, net/DD 1.16 contro 0.98; annullato dall'ultimo terzo (-1,116 -> -1,764) |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 108.3, netto 5,415, 50 trade |

**Storia di ricerca**: 79 trade, netto 7,606, DD 4,027, average trade 96, UngerFit 1.22.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 79 su almeno 50 |
| average trade | **no** | 96 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 20.7 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,191 |
| ultimo terzo | **no** | netto 2,191, net/DD 0.57 (serve 1,3) |
| due terzi su tre | passa | -241 / 5,656 / 2,191 |
| outlier | **no** | trade migliore 55% del netto sulla storia, 122% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 76 con i pattern, 35 senza |
| pattern casuali | passa | 2 estrazioni su 12 fanno almeno altrettanto, p 0.23, Wilson 0.11 |
| plateau | passa | media 0.66 su 3 vicini, minimo 0.02 |

**Prova su FTMO**: 52 trade, netto 31,746, DD 10,458, net/DD 3.04, average trade 611 (soglia 160), UngerFit 4.72, finestre in utile 2/4 (-5,331 / 10,970 / -6,019 / 32,126).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 3.04 (serve 1), average trade 611 (soglia 160), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 76% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.9 2021:-0.1 2022:0.9 2023:0.4 2024:-1.8 2025:3.3 2026:5.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=3, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=20`

## LFHL Level fader sugli estremi di ieri

498 simulazioni in 16.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | avg smussato 10.5, netto 1,605, 153 trade |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 5,591 (migliore 5,820), 154 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 42 | ✔ | batte lo spento: netto 22,419 contro 10,858, net/DD 5.04 contro 1.11 |
| NO | PtnNeutNo | 56 | 27 | ✔ | batte lo spento: netto 23,138 contro 22,419, net/DD 5.20 contro 5.04 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 462.8, netto 23,138, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.5 | 0.5 | · | D2: netto smussato 20,854 (migliore 21,507), 50 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 501.5, netto 25,073, 50 trade |

**Storia di ricerca**: 95 trade, netto 17,005, DD 12,430, average trade 179, UngerFit 1.29.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 95 su almeno 50 |
| average trade | passa | 179 contro la soglia 154 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 24.9 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -8,068 |
| ultimo terzo | **no** | netto -8,068, net/DD -0.65 (serve 1,3) |
| due terzi su tre | passa | 13,675 / 11,398 / -8,068 |
| outlier | **no** | trade migliore 59% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -179 con i pattern, -93 senza |
| pattern casuali | **no** | 12 estrazioni su 18 fanno almeno altrettanto, p 0.68, Wilson 0.53 |
| plateau | passa | media 0.75 su 3 vicini, minimo 0.56 |

**Prova su FTMO**: 35 trade, netto -8,018, DD 8,855, net/DD -0.91, average trade -229 (soglia 160), UngerFit , finestre in utile 0/4 (-5,084 / -1,165 / -1,667 / -102).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.91 (serve 1), average trade -229 (soglia 160), finestre 0/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 106% del netto (massimo 40%); average trade / soglia per anno: 2020:17.7 2021:2.5 2022:3.2 2023:0.3 2024:-1.5 2025:-2.2 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=42, PtnNeutNo=27, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=20`

## Riepilogo

- PCH: abbandonato (R9: la base non guadagna su tutta la storia (89 trade, netto -6,164))
- TFM: scartata; ricerca 75 trade netto 12,475 avg 166; prova 63 trade netto -9,046 net/DD -0.51 avg -144 fin 1/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- TFU: scartata; ricerca 72 trade netto 39,850 avg 553; prova 50 trade netto -7,113 net/DD -0.51 avg -142 fin 2/4; falliti: anni, utile recente, ultimo terzo, pattern utile, pattern casuali, prova sul broker, anni
- BO: scartata; ricerca 105 trade netto 18,935 avg 180; prova 60 trade netto -18,225 net/DD -0.84 avg -304 fin 0/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- BOS: abbandonato (R9: la base non guadagna su tutta la storia (173 trade, netto -42,142))
- VBO: scartata; ricerca 79 trade netto 31,632 avg 400; prova 50 trade netto 2,216 net/DD 0.16 avg 44 fin 1/4; falliti: anni, utile recente, ultimo terzo, due terzi su tre, pattern utile, pattern casuali, prova sul broker, anni
- MAC: scartata; ricerca 149 trade netto 24,355 avg 163; prova 70 trade netto 18,327 net/DD 1.46 avg 262 fin 3/4; falliti: anni, utile recente, ultimo terzo, plateau, anni, altri mercati
- RBM: scartata; ricerca 85 trade netto 17,720 avg 208; prova 47 trade netto 5,359 net/DD 0.57 avg 114 fin 2/4; falliti: anni, utile recente, ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- RBU: scartata; ricerca 72 trade netto 32,775 avg 455; prova 41 trade netto -4,029 net/DD -0.38 avg -98 fin 2/4; falliti: anni, utile recente, ultimo terzo, prova sul broker, anni
- RHL: scartata; ricerca 113 trade netto 42,982 avg 380; prova 45 trade netto 5,025 net/DD 0.56 avg 112 fin 1/4; falliti: anni, outlier, prova sul broker, anni
- LF: scartata; ricerca 79 trade netto 7,606 avg 96; prova 52 trade netto 31,746 net/DD 3.04 avg 611 fin 2/4; falliti: average trade, anni, ultimo terzo, outlier, prova sul broker, anni
- LFHL: scartata; ricerca 95 trade netto 17,005 avg 179; prova 35 trade netto -8,018 net/DD -0.91 avg -229 fin 0/4; falliti: anni, utile recente, ultimo terzo, outlier, pattern utile, pattern casuali, prova sul broker, anni
