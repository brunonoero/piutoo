# Percorso v4 — @FESX 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (5,845 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (2,295 barre), mai vista dal percorso
- costi FTMO: spread 1.46 punti (mediana), swap long 1.1327 short 0.0114 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 60 nella ricerca, 60 nella prova

## PCH Price Channel

544 simulazioni in 14.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=20, OffsetTicks=10, Direction=1 | ✔ | avg smussato 16.1, netto 2,145, 133 trade |
| finestra: inizio | StartHour | -1 | 10 | ✔ | avg smussato 32.4, netto 1,653, 51 trade |
| finestra: fine | EndHour | -1 | 20 | ✔ | avg smussato 38.4, netto 1,653, 51 trade |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 2,150 (migliore 2,150), 51 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 38.8, netto 1,979, 51 trade |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 2 candidati batte lo spento |
| affinamento stop | StopAtr | 0.5 | 0.5 | · | D2: netto smussato 2,150 (migliore 2,150), 51 trade |

**Storia di ricerca**: 92 trade, netto 170, DD 2,998, average trade 2, UngerFit 0.04.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 92 su almeno 50 |
| average trade | **no** | 2 contro la soglia 60 |
| anni | passa | 4 anni, minimo 20 trade in un anno, 24.1 all'anno, 2 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -1,809 |
| ultimo terzo | **no** | netto -1,809, net/DD -0.75 (serve 1,3) |
| due terzi su tre | passa | 181 / 1,798 / -1,809 |
| outlier | **no** | trade migliore 546% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.20 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 80 trade, netto -983, DD 3,249, net/DD -0.30, average trade -12 (soglia 60), UngerFit , finestre in utile 2/4 (-617 / 1,428 / -2,999 / 1,205).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.30 (serve 1), average trade -12 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 2/6 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2021:0.2 2022:1.4 2023:-1.1 2024:-0.5 2025:0.0 2026:-0.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=10, EndHour=20, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=20, OffsetTicks=10, Direction=1`

## TFM Trend following mirrored

394 simulazioni in 12.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -13,164 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -13,164 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -8,291 (migliore -8,291), 311 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -8,712; annullato dall'ultimo terzo (4,828 -> 2,878) |
| YES | PtnNeutYes | 55 | 10 | · | batte lo spento: netto 941 contro -9,088, net/DD 0.76 contro -0.69; annullato dall'ultimo terzo (4,828 -> 2,776) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 941 contro -9,088, net/DD 0.76 contro -0.69; annullato dall'ultimo terzo (4,828 -> 2,776) |
| direzionale YES | PtnDirYes | 52 | 9 | · | batte lo spento: netto 1,352 contro -9,088, net/DD 0.57 contro -0.69; annullato dall'ultimo terzo (4,828 -> 1,185) |
| direzionale NO | PtnDirNo | 53 | 22 | · | batte lo spento: netto 3,063 contro -9,088, net/DD 1.24 contro -0.69; annullato dall'ultimo terzo (4,828 -> -1,356) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (487 trade, netto -4,260).

## TFU Trend following unmirrored

684 simulazioni in 15.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -13,164 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -13,164 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -8,291 (migliore -8,291), 311 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -8,712; annullato dall'ultimo terzo (4,828 -> 2,878) |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 61 | · | batte lo spento: netto 2,710 contro -9,088, net/DD 0.59 contro -0.69; annullato dall'ultimo terzo (4,828 -> 1,740) |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 96 | · | batte lo spento: netto 3,286 contro -9,088, net/DD 0.65 contro -0.69; annullato dall'ultimo terzo (4,828 -> 1,232) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (487 trade, netto -4,260).

## BO Breakout di N sessioni

495 simulazioni in 13.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=2, BreakoutOffsetTicks=20, IncludeCurrentSession=1 | ✔ | nessuna in utile: netto massimo -3,729 |
| finestra: inizio | StartHour | -1 | 14 | ✔ | avg smussato 18.9, netto 1,266, 67 trade |
| finestra: fine | EndHour | -1 | 16 | · | avg smussato 30.4, netto 1,519, 50 trade; annullato dall'ultimo terzo (1,050 -> 34) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 3,392 (migliore 3,392), 67 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 46.2, netto 3,095, 67 trade |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 1 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | -35 | · | batte lo spento: netto 3,580 contro 3,095, net/DD 4.02 contro 3.40; annullato dall'ultimo terzo (1,153 -> 341) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 4,063 contro 3,095, net/DD 5.90 contro 3.40; annullato dall'ultimo terzo (1,153 -> 249) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 3,392 (migliore 3,392), 67 trade |

**Storia di ricerca**: 89 trade, netto 4,248, DD 910, average trade 48, UngerFit 2.04.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 89 su almeno 50 |
| average trade | **no** | 48 contro la soglia 60 |
| anni | passa | 4 anni, minimo 14 trade in un anno, 23.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,153 |
| ultimo terzo | passa | netto 1,153, net/DD 1.92 (serve 1,3) |
| due terzi su tre | passa | 1,173 / 1,922 / 1,153 |
| outlier | **no** | trade migliore 39% del netto sulla storia, 65% sull'ultimo terzo |
| plateau | passa | media 0.79 su 4 vicini, minimo 0.46 |

**Prova su FTMO**: 111 trade, netto 1,667, DD 2,830, net/DD 0.59, average trade 15 (soglia 60), UngerFit 0.36, finestre in utile 2/4 (964 / 2,325 / -1,481 / -142).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.59 (serve 1), average trade 15 (soglia 60), finestre 2/4 (servono 3) |
| anni | passa | 5/6 anni in utile (serve 60%), anno migliore 37% del netto (massimo 40%); average trade / soglia per anno: 2021:1.0 2022:0.8 2023:0.4 2024:0.8 2025:0.4 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=14, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=2, BreakoutOffsetTicks=20`

## BOS Breakout della sessione in corso

440 simulazioni in 10.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=2 | ✔ | nessuna in utile: netto massimo -9,496 |
| finestra: inizio | StartHour | -1 | 18 | ✔ | avg smussato 9.2, netto 1,428, 155 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 9.2, netto 1,428, 155 trade |
| stop | StopAtr | 0.8 | 0.4 | · | D2: netto smussato 2,848 (migliore 2,848), 155 trade; annullato dall'ultimo terzo (4,787 -> 3,627) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 9.2, netto 1,428, 155 trade |
| YES | PtnNeutYes | 55 | 6 | · | batte lo spento: netto 5,688 contro 1,428, net/DD 2.73 contro 0.19; annullato dall'ultimo terzo (4,787 -> 4,049) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 5,519 contro 1,428, net/DD 2.47 contro 0.19; annullato dall'ultimo terzo (4,787 -> 4,089) |
| direzionale YES | PtnDirYes | 52 | 34 | · | batte lo spento: netto 6,267 contro 1,428, net/DD 3.34 contro 0.19; annullato dall'ultimo terzo (4,787 -> 2,542) |
| direzionale NO | PtnDirNo | 53 | 22 | · | batte lo spento: netto 8,326 contro 1,428, net/DD 2.30 contro 0.19; annullato dall'ultimo terzo (4,787 -> -324) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 4,587 contro 1,428, net/DD 0.82 contro 0.19; annullato dall'ultimo terzo (4,787 -> 2,070) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 2,102 contro 1,428, net/DD 0.28 contro 0.19; annullato dall'ultimo terzo (4,787 -> 4,244) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 2,537 (migliore 2,537), 155 trade; annullato dall'ultimo terzo (4,787 -> 4,120) |

**Storia di ricerca**: 226 trade, netto 6,215, DD 7,401, average trade 28, UngerFit 0.41.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 226 su almeno 50 |
| average trade | **no** | 28 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 4 trade in un anno, 59.3 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 4,787 |
| ultimo terzo | passa | netto 4,787, net/DD 1.82 (serve 1,3) |
| due terzi su tre | passa | 5,356 / -3,928 / 4,787 |
| outlier | **no** | trade migliore 35% del netto sulla storia, 24% sull'ultimo terzo |
| plateau | passa | media 0.83 su 4 vicini, minimo 0.65 |

**Prova su FTMO**: 228 trade, netto -2,982, DD 11,383, net/DD -0.26, average trade -13 (soglia 60), UngerFit , finestre in utile 2/4 (148 / 5,718 / -6,613 / -2,234).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.26 (serve 1), average trade -13 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 238% del netto (massimo 40%); average trade / soglia per anno: 2020:-2.5 2021:1.1 2022:-1.0 2023:2.7 2024:-0.2 2025:0.2 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=18, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=2`

## VBO Volatility breakout

652 simulazioni in 11.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.0, Direction=2 | ✔ | avg smussato 1.7, netto 203, 117 trade |
| finestra: inizio | StartHour | -1 | 2 | ✔ | avg smussato 5.9, netto 679, 116 trade |
| finestra: fine | EndHour | -1 | 9 | · | avg smussato 7.4, netto 566, 76 trade; annullato dall'ultimo terzo (-2,163 -> -2,694) |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 506 (migliore 506), 116 trade; annullato dall'ultimo terzo (-2,163 -> -2,514) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 5.9, netto 679, 116 trade |
| YES | PtnNeutYes | 55 | 12 | ✔ | batte lo spento: netto 1,573 contro 679, net/DD 0.98 contro 0.28 |
| NO | PtnNeutNo | 56 | 52 | · | batte lo spento: netto 2,288 contro 1,573, net/DD 1.99 contro 0.98; annullato dall'ultimo terzo (-876 -> -936) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | 19 | · | batte lo spento: netto 2,114 contro 1,573, net/DD 1.25 contro 0.98; annullato dall'ultimo terzo (-876 -> -2,496) |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 2,504 contro 1,573, net/DD 1.60 contro 0.98; annullato dall'ultimo terzo (-876 -> -1,126) |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 1,526 (migliore 1,526), 54 trade; annullato dall'ultimo terzo (-876 -> -962) |

**Storia di ricerca**: 84 trade, netto 698, DD 1,939, average trade 8, UngerFit 0.24.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 84 su almeno 50 |
| average trade | **no** | 8 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 22.0 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -876 |
| ultimo terzo | **no** | netto -876, net/DD -0.45 (serve 1,3) |
| due terzi su tre | passa | 195 / 1,378 / -876 |
| outlier | **no** | trade migliore 243% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -29 con i pattern, -37 senza |
| pattern casuali | **no** | 5 estrazioni su 15 fanno almeno altrettanto, p 0.38, Wilson 0.23 |
| plateau | **no** | media 0.23 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 41 trade, netto 3,355, DD 1,192, net/DD 2.81, average trade 82 (soglia 60), UngerFit 3.06, finestre in utile 2/4 (-244 / 2,028 / -172 / 1,743).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 2.81 (serve 1), average trade 82 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 43% del netto (massimo 40%); average trade / soglia per anno: 2020:-2.4 2021:0.1 2022:1.2 2023:0.7 2024:-1.2 2025:1.1 2026:2.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=2, EndHour=-1, PtnNeutYes=12, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=1.0, Direction=2`

## MAC Incrocio di medie

125 simulazioni in 2.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=5, SlowPeriod=100, Direction=0 | ✔ | avg smussato 34.4, netto 2,989, 87 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 2,783 (migliore 2,896), 87 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 39.5, netto 3,440, 87 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 4,056 contro 3,440, net/DD 1.93 contro 1.64; annullato dall'ultimo terzo (-1,202 -> -1,799) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 3,258 (migliore 3,395), 87 trade |

**Storia di ricerca**: 129 trade, netto 2,239, DD 3,151, average trade 17, UngerFit 0.40.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 129 su almeno 50 |
| average trade | **no** | 17 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 33.8 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -1,202 |
| ultimo terzo | **no** | netto -1,202, net/DD -0.38 (serve 1,3) |
| due terzi su tre | passa | 1,088 / 2,352 / -1,202 |
| outlier | **no** | trade migliore 42% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.47 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 56 trade, netto 3,530, DD 2,232, net/DD 1.58, average trade 63 (soglia 60), UngerFit 1.72, finestre in utile 3/4 (1,924 / 2,103 / 401 / -898).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 1.58 (serve 1), average trade 63 (soglia 60), finestre 3/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 55% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.1 2021:0.1 2022:1.9 2023:0.6 2024:-0.2 2025:2.0 2026:-0.7 |
| altri mercati | **no** | 2/7 con net/DD ≥ 1 (serve meta'): @FDAX 0.01, @NQ 0.92, @ES 1.32, @YM 2.26, @FCE -0.14, @Z -0.69, @NIY -0.34 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=5, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=5, SlowPeriod=100, Direction=0`

## RBM Reversal Bollinger mirrored

671 simulazioni in 13.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=15, BbNumDevs=2.0 | ✔ | avg smussato 9.1, netto 4,335, 474 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 9.1, netto 4,335, 474 trade |
| finestra: fine | EndHour | -1 | 23 | ✔ | avg smussato 11.8, netto 4,335, 474 trade |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 4,472 (migliore 4,472), 474 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 46 | ✔ | batte lo spento: netto 8,091 contro 5,209, net/DD 1.47 contro 0.66 |
| NO | PtnNeutNo | 56 | 54 | ✔ | batte lo spento: netto 8,008 contro 8,091, net/DD 4.74 contro 1.47 |
| direzionale YES | PtnDirYes | 52 | -1 | · | batte lo spento: netto 7,758 contro 8,008, net/DD 5.04 contro 4.74; annullato dall'ultimo terzo (2,713 -> 1,552) |
| direzionale NO | PtnDirNo | 53 | 3 | · | batte lo spento: netto 7,869 contro 8,008, net/DD 8.33 contro 4.74; annullato dall'ultimo terzo (2,713 -> 1,006) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 8,000 contro 8,008, net/DD 9.21 contro 4.74; annullato dall'ultimo terzo (2,713 -> 1,889) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 92.0, netto 8,008, 87 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 1.0 | 1.0 | · | D2: netto smussato 7,775 (migliore 7,965), 87 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 94.0, netto 8,174, 87 trade |

**Storia di ricerca**: 133 trade, netto 11,178, DD 1,565, average trade 84, UngerFit 2.74.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 133 su almeno 50 |
| average trade | passa | 84 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 34.9 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 3,003 |
| ultimo terzo | passa | netto 3,003, net/DD 2.90 (serve 1,3) |
| due terzi su tre | passa | 543 / 7,631 / 3,003 |
| outlier | passa | trade migliore 12% del netto sulla storia, 29% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 65 con i pattern, -12 senza |
| pattern casuali | passa | 0 estrazioni su 29 fanno almeno altrettanto, p 0.03, Wilson 0.01 |
| plateau | passa | media 0.70 su 6 vicini, minimo 0.10 |

**Prova su FTMO**: 24 trade, netto 643, DD 1,342, net/DD 0.48, average trade 27 (soglia 60), UngerFit 0.94, finestre in utile 2/4 (1,913 / -575 / -753 / 58).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.48 (serve 1), average trade 27 (soglia 60), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 37% del netto (massimo 40%); average trade / soglia per anno: 2020:0.0 2021:0.5 2022:2.1 2023:1.4 2024:2.2 2025:-3.1 2026:0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=23, PtnNeutYes=46, PtnNeutNo=54, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=15, BbNumDevs=2.0`

## RBU Reversal Bollinger unmirrored

969 simulazioni in 22.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=15, BbNumDevs=2.0 | ✔ | avg smussato 9.1, netto 4,335, 474 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 9.1, netto 4,335, 474 trade |
| finestra: fine | EndHour | -1 | 23 | ✔ | avg smussato 11.8, netto 4,335, 474 trade |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 4,472 (migliore 4,472), 474 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 127 | ✔ | batte lo spento: netto 8,385 contro 5,209, net/DD 1.66 contro 0.66 |
| YES short | FastYesShort | 152 | 37 | ✔ | batte lo spento: netto 13,814 contro 8,385, net/DD 9.24 contro 1.66 |
| NO long | FastNoLong | 153 | 131 | · | batte lo spento: netto 10,676 contro 13,814, net/DD 11.45 contro 9.24; annullato dall'ultimo terzo (1,392 -> -1,293) |
| NO short | FastNoShort | 153 | 63 | · | batte lo spento: netto 13,270 contro 13,814, net/DD 9.44 contro 9.24; annullato dall'ultimo terzo (1,392 -> 1,296) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 10,752 contro 13,814, net/DD 9.81 contro 9.24; annullato dall'ultimo terzo (1,392 -> 628) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 81.3, netto 13,814, 170 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | · | batte lo spento: netto 14,163 contro 13,814, net/DD 11.21 contro 9.24; annullato dall'ultimo terzo (1,392 -> 1,078) |
| affinamento stop | StopAtr | 1.0 | 1.0 | · | D2: netto smussato 13,564 (migliore 13,684), 170 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 0 | · | avg smussato 93.7, netto 15,832, 169 trade; annullato dall'ultimo terzo (1,392 -> 1,303) |

**Storia di ricerca**: 250 trade, netto 15,206, DD 1,559, average trade 61, UngerFit 1.99.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 250 su almeno 50 |
| average trade | passa | 61 contro la soglia 60 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 65.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,392 |
| ultimo terzo | **no** | netto 1,392, net/DD 0.89 (serve 1,3) |
| due terzi su tre | passa | 2,184 / 11,629 / 1,392 |
| outlier | **no** | trade migliore 9% del netto sulla storia, 66% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 17 con i pattern, -16 senza |
| pattern casuali | passa | 7 estrazioni su 121 fanno almeno altrettanto, p 0.07, Wilson 0.04 |
| plateau | passa | media 0.62 su 8 vicini, minimo 0.00 |

**Prova su FTMO**: 96 trade, netto -263, DD 3,258, net/DD -0.08, average trade -3 (soglia 60), UngerFit , finestre in utile 3/4 (440 / -1,671 / 382 / 586).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.08 (serve 1), average trade -3 (soglia 60), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 46% del netto (massimo 40%); average trade / soglia per anno: 2020:0.1 2021:0.6 2022:1.8 2023:1.5 2024:0.0 2025:-0.4 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=23, FastYesLong=127, FastYesShort=37, FastNoLong=153, FastNoShort=153, SkipDay=-1, BbLength=15, BbNumDevs=2.0`

## RHL Reversal sui livelli di ieri

661 simulazioni in 11.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=1 | ✔ | avg smussato 22.8, netto 3,712, 163 trade |
| finestra: inizio | StartHour | -1 | 18 | ✔ | avg smussato 78.6, netto 5,660, 72 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 78.6, netto 5,660, 72 trade |
| stop | StopAtr | 0.8 | 1.25 | ✔ | D2: netto smussato 6,223 (migliore 6,462), 71 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 7,133 contro 6,142, net/DD 5.13 contro 4.42; annullato dall'ultimo terzo (-1,893 -> -2,277) |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 3 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 41 | ✔ | batte lo spento: netto 7,421 contro 6,142, net/DD 5.88 contro 4.42 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 132.5, netto 7,421, 56 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 1.25 | 1.25 | · | D2: netto smussato 7,156 (migliore 7,330), 56 trade |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 8,413 contro 7,421, net/DD 6.67 contro 5.88; annullato dall'ultimo terzo (453 -> 70) |
| durata | MaxBars | 4 | 0 | · | avg smussato 145.1, netto 8,124, 56 trade; annullato dall'ultimo terzo (453 -> -111) |

**Storia di ricerca**: 79 trade, netto 7,874, DD 1,262, average trade 100, UngerFit 3.62.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 79 su almeno 50 |
| average trade | passa | 100 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 20.7 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 453 |
| ultimo terzo | **no** | netto 453, net/DD 0.39 (serve 1,3) |
| due terzi su tre | passa | 4,059 / 3,362 / 453 |
| outlier | **no** | trade migliore 12% del netto sulla storia, 200% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 20 con i pattern, -63 senza |
| pattern casuali | passa | 2 estrazioni su 9 fanno almeno altrettanto, p 0.30, Wilson 0.15 |
| plateau | passa | media 0.70 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 34 trade, netto -2,283, DD 5,075, net/DD -0.45, average trade -67 (soglia 60), UngerFit , finestre in utile 2/4 (-680 / -4,171 / 1,426 / 1,142).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.45 (serve 1), average trade -67 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 83% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.4 2021:4.5 2022:1.4 2023:0.3 2024:-0.9 2025:-3.6 2026:2.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.25, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=18, EndHour=-1, PtnNeutYes=55, PtnNeutNo=41, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=20, Direction=1`

## LF Level fader sul pivot di ieri

296 simulazioni in 7.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -1,952 |
| stop | StopAtr | 0.8 | 1.5 | ✔ | D2: netto smussato -902 (migliore -902), 53 trade |
| target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 1,073 contro -902, net/DD 0.43 contro -0.30 |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 2 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 2 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | 3 | ✔ | batte lo spento: netto 1,055 contro 1,073, net/DD 0.47 contro 0.43 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 21.1, netto 1,055, 50 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 1.5 | 1.5 | · | D2: netto smussato 1,055 (migliore 1,055), 50 trade |
| affinamento target | TargetAtr | 0.5 | 0.5 | · | batte lo spento: netto 1,055 contro -929, net/DD 0.47 contro -0.33 |
| durata | MaxBars | 4 | 0 | · | avg smussato 30.1, netto 1,507, 50 trade; annullato dall'ultimo terzo (1,167 -> 1,087) |

**Storia di ricerca**: 71 trade, netto 2,222, DD 2,243, average trade 31, UngerFit 0.85.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 71 su almeno 50 |
| average trade | **no** | 31 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 18.6 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,167 |
| ultimo terzo | passa | netto 1,167, net/DD 3.62 (serve 1,3) |
| due terzi su tre | passa | 917 / 139 / 1,167 |
| outlier | **no** | trade migliore 26% del netto sulla storia, 31% sull'ultimo terzo |
| plateau | passa | media 0.69 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 50 trade, netto -3,068, DD 4,721, net/DD -0.65, average trade -61 (soglia 60), UngerFit , finestre in utile 1/4 (565 / -1,456 / -397 / -1,780).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.65 (serve 1), average trade -61 (soglia 60), finestre 1/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.9 2021:0.7 2022:-0.1 2023:0.9 2024:1.2 2025:-1.0 2026:-1.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.5, TargetAtr=0.5, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=3, LevelShift=20`

## LFHL Level fader sugli estremi di ieri

502 simulazioni in 8.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=5 | ✔ | nessuna in utile: netto massimo -2,477 |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato -721 (migliore -721), 142 trade; annullato dall'ultimo terzo (-3,452 -> -3,936) |
| target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 504 contro -2,477, net/DD 0.13 contro -0.42 |
| YES | PtnNeutYes | 55 | 19 | ✔ | batte lo spento: netto 1,752 contro 504, net/DD 1.52 contro 0.13 |
| NO | PtnNeutNo | 56 | 23 | ✔ | batte lo spento: netto 1,852 contro 1,752, net/DD 1.61 contro 1.52 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 36.3, netto 1,852, 51 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 1,716 (migliore 1,716), 51 trade; annullato dall'ultimo terzo (-867 -> -1,221) |
| affinamento target | TargetAtr | 0.5 | 1.0 | · | batte lo spento: netto 2,037 contro 1,813, net/DD 1.77 contro 1.57; annullato dall'ultimo terzo (-867 -> -1,436) |
| durata | MaxBars | 4 | 4 | · | avg smussato 28.6, netto 1,852, 51 trade |

**Storia di ricerca**: 89 trade, netto 985, DD 1,221, average trade 11, UngerFit 0.41.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 89 su almeno 50 |
| average trade | **no** | 11 contro la soglia 60 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 23.4 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -867 |
| ultimo terzo | **no** | netto -867, net/DD -0.75 (serve 1,3) |
| due terzi su tre | passa | 1,099 / 752 / -867 |
| outlier | **no** | trade migliore 57% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -23 con i pattern, -30 senza |
| pattern casuali | passa | 5 estrazioni su 20 fanno almeno altrettanto, p 0.29, Wilson 0.18 |
| plateau | **no** | media 0.40 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 43 trade, netto -189, DD 1,176, net/DD -0.16, average trade -4 (soglia 60), UngerFit , finestre in utile 2/4 (541 / 86 / -243 / -574).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.16 (serve 1), average trade -4 (soglia 60), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 157% del netto (massimo 40%); average trade / soglia per anno: 2020:0.3 2021:0.8 2022:0.7 2023:-0.2 2024:-0.4 2025:0.2 2026:-0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0.5, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=19, PtnNeutNo=23, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=5`

## Riepilogo

- PCH: scartata; ricerca 92 trade netto 170 avg 2; prova 80 trade netto -983 net/DD -0.30 avg -12 fin 2/4; falliti: average trade, utile recente, ultimo terzo, outlier, plateau, prova sul broker, anni
- TFM: abbandonato (R9: la base non guadagna su tutta la storia (487 trade, netto -4,260))
- TFU: abbandonato (R9: la base non guadagna su tutta la storia (487 trade, netto -4,260))
- BO: scartata; ricerca 89 trade netto 4,248 avg 48; prova 111 trade netto 1,667 net/DD 0.59 avg 15 fin 2/4; falliti: average trade, outlier, prova sul broker
- BOS: scartata; ricerca 226 trade netto 6,215 avg 28; prova 228 trade netto -2,982 net/DD -0.26 avg -13 fin 2/4; falliti: average trade, anni, outlier, prova sul broker, anni
- VBO: scartata; ricerca 84 trade netto 698 avg 8; prova 41 trade netto 3,355 net/DD 2.81 avg 82 fin 2/4; falliti: average trade, anni, utile recente, ultimo terzo, outlier, pattern casuali, plateau, prova sul broker, anni
- MAC: scartata; ricerca 129 trade netto 2,239 avg 17; prova 56 trade netto 3,530 net/DD 1.58 avg 63 fin 3/4; falliti: average trade, anni, utile recente, ultimo terzo, outlier, plateau, anni, altri mercati
- RBM: scartata; ricerca 133 trade netto 11,178 avg 84; prova 24 trade netto 643 net/DD 0.48 avg 27 fin 2/4; falliti: anni, prova sul broker
- RBU: scartata; ricerca 250 trade netto 15,206 avg 61; prova 96 trade netto -263 net/DD -0.08 avg -3 fin 3/4; falliti: ultimo terzo, outlier, prova sul broker, anni
- RHL: scartata; ricerca 79 trade netto 7,874 avg 100; prova 34 trade netto -2,283 net/DD -0.45 avg -67 fin 2/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- LF: scartata; ricerca 71 trade netto 2,222 avg 31; prova 50 trade netto -3,068 net/DD -0.65 avg -61 fin 1/4; falliti: average trade, anni, outlier, prova sul broker, anni
- LFHL: scartata; ricerca 89 trade netto 985 avg 11; prova 43 trade netto -189 net/DD -0.16 avg -4 fin 2/4; falliti: average trade, anni, utile recente, ultimo terzo, outlier, plateau, prova sul broker, anni
