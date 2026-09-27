# Percorso v4 — @NQ 240m

- ricerca: feed interno 2008-01-01 → 2022-05-16 (22,260 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2022-05-16 → 2026-09-01 (1,808 barre), mai vista dal percorso
- costi FTMO: spread 1.45 punti (mediana), swap long 6.7098 short 0 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 109 nella ricerca, 509 nella prova

## PCH Price Channel

763 simulazioni in 51.0 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=30, OffsetTicks=0, Direction=2 | ✔ | nessuna in utile: netto massimo -23,859 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -4,471 |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 35.1, netto 4,316, 123 trade; annullato dall'ultimo terzo (24,310 -> 12,823) |
| stop | StopAtr | 0.8 | 1.5 | · | D2: netto smussato -3,946 (migliore -3,946), 94 trade; annullato dall'ultimo terzo (24,310 -> 16,187) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -4,471 |
| YES | PtnNeutYes | 55 | 28 | · | batte lo spento: netto 75 contro -4,471, net/DD 0.01 contro -0.40; annullato dall'ultimo terzo (24,310 -> -20,371) |
| NO | PtnNeutNo | 56 | 1 | ✔ | batte lo spento: netto 874 contro -4,471, net/DD 0.10 contro -0.40 |
| direzionale YES | PtnDirYes | 52 | -9 | · | batte lo spento: netto 3,817 contro 874, net/DD 1.06 contro 0.10; annullato dall'ultimo terzo (33,930 -> 3,565) |
| direzionale NO | PtnDirNo | 53 | 7 | · | batte lo spento: netto 5,026 contro 874, net/DD 1.06 contro 0.10; annullato dall'ultimo terzo (33,930 -> 19,742) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 5,950 contro 874, net/DD 1.04 contro 0.10; annullato dall'ultimo terzo (33,930 -> 33,504) |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 3,795 contro 874, net/DD 0.59 contro 0.10; annullato dall'ultimo terzo (33,930 -> 19,647) |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 2,988 contro 874, net/DD 0.34 contro 0.10 |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 1,839 (migliore 1,839), 88 trade |

**Storia di ricerca**: 139 trade, netto 42,154, DD 19,639, average trade 303, UngerFit 2.07.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 139 su almeno 50 |
| average trade | passa | 303 contro la soglia 109 |
| anni | **no** | 15 anni, minimo 6 trade in un anno, 9.7 all'anno, 9 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 41,575 |
| ultimo terzo | passa | netto 41,575, net/DD 3.37 (serve 1,3) |
| due terzi su tre | passa | 7,138 / -6,559 / 41,575 |
| outlier | **no** | trade migliore 30% del netto sulla storia, 30% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 815 con i pattern, 529 senza |
| pattern casuali | passa | 0 estrazioni su 3 fanno almeno altrettanto, p 0.25, Wilson 0.07 |
| plateau | passa | media 0.81 su 7 vicini, minimo 0.57 |

**Prova su FTMO**: 15 trade, netto -1,172, DD 37,790, net/DD -0.03, average trade -78 (soglia 509), UngerFit , finestre in utile 1/4 (0 / 0 / 8,825 / -9,997).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.03 (serve 1), average trade -78 (soglia 509), finestre 1/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 31 trade 22,556 net/DD 1.33, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 10/17 anni in utile (serve 60%), anno migliore 70% del netto (massimo 40%); average trade / soglia per anno: 2008:1.5 2009:-1.9 2010:2.5 2011:5.0 2012:0.2 2013:-10.3 2014:-0.8 2015:9.5 2016:-6.0 2017:-15.7 2018:3.6 2019:-7.3 2020:4.0 2021:2.5 2022:5.2 2025:-3.1 2026:1.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=17, EndHour=-1, PtnNeutYes=55, PtnNeutNo=1, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=30, OffsetTicks=0, Direction=2`

## TFM Trend following mirrored

530 simulazioni in 35.4 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -82,029 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -82,029 |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato -72,182 (migliore -72,182), 1496 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -19,290 |
| YES | PtnNeutYes | 55 | 21 | · | batte lo spento: netto 8,485 contro -19,290, net/DD 0.78 contro -0.80; annullato dall'ultimo terzo (-29,521 -> -67,885) |
| NO | PtnNeutNo | 56 | 15 | · | batte lo spento: netto 8,485 contro -19,290, net/DD 0.78 contro -0.80; annullato dall'ultimo terzo (-29,521 -> -67,885) |
| direzionale YES | PtnDirYes | 52 | -35 | ✔ | batte lo spento: netto 9,013 contro -19,290, net/DD 0.92 contro -0.80 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 2 | ✔ | batte lo spento: netto 11,801 contro 9,013, net/DD 1.50 contro 0.92 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 14,236 contro 11,801, net/DD 1.82 contro 1.50; annullato dall'ultimo terzo (17,843 -> 11,278) |
| affinamento stop | StopAtr | 0.5 | 0.6 | ✔ | D2: netto smussato 12,914 (migliore 13,471), 294 trade |

**Storia di ricerca**: 459 trade, netto 39,833, DD 20,689, average trade 87, UngerFit 0.58.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 459 su almeno 50 |
| average trade | **no** | 87 contro la soglia 109 |
| anni | passa | 15 anni, minimo 14 trade in un anno, 31.9 all'anno, 10 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 26,093 |
| ultimo terzo | **no** | netto 26,093, net/DD 1.26 (serve 1,3) |
| due terzi su tre | passa | -1,303 / 15,043 / 26,093 |
| outlier | **no** | trade migliore 26% del netto sulla storia, 40% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 158 con i pattern, 5 senza |
| pattern casuali | passa | 5 estrazioni su 25 fanno almeno altrettanto, p 0.23, Wilson 0.14 |
| plateau | passa | media 0.73 su 2 vicini, minimo 0.70 |

**Prova su FTMO**: 40 trade, netto 30,057, DD 18,425, net/DD 1.63, average trade 751 (soglia 509), UngerFit 2.45, finestre in utile 2/4 (0 / 0 / 6,645 / 23,412).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.63 (serve 1), average trade 751 (soglia 509), finestre 2/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 104 trade 51,708 net/DD 3.18, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 12/17 anni in utile (serve 60%), anno migliore 41% del netto (massimo 40%); average trade / soglia per anno: 2008:-1.0 2009:-0.7 2010:-1.5 2011:0.6 2012:4.5 2013:-0.9 2014:3.6 2015:1.7 2016:1.5 2017:1.2 2018:-0.7 2019:0.5 2020:0.5 2021:0.1 2022:2.9 2025:0.3 2026:1.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=5, IntradayOnly=0, ExitHour=-1, StartHour=17, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=-35, PtnDirNo=53, SkipDay=2`

## TFU Trend following unmirrored

928 simulazioni in 65.7 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -82,029 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -82,029 |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato -72,182 (migliore -72,182), 1496 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | nessuna in utile: netto massimo -19,290 |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 21 | ✔ | batte lo spento: netto 12,402 contro -19,290, net/DD 0.96 contro -0.80 |
| NO long | FastNoLong | 153 | 34 | · | batte lo spento: netto 19,446 contro 12,402, net/DD 2.46 contro 0.96; annullato dall'ultimo terzo (-17,657 -> -21,081) |
| NO short | FastNoShort | 153 | 143 | ✔ | batte lo spento: netto 18,183 contro 12,402, net/DD 1.83 contro 0.96 |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 21,188 contro 18,183, net/DD 3.80 contro 1.83; annullato dall'ultimo terzo (16,360 -> -4,505) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 18,935 contro 18,183, net/DD 1.91 contro 1.83; annullato dall'ultimo terzo (16,360 -> 16,351) |
| affinamento stop | StopAtr | 0.5 | 0.6 | ✔ | D2: netto smussato 19,740 (migliore 20,519), 973 trade |

**Storia di ricerca**: 1516 trade, netto 50,354, DD 43,051, average trade 33, UngerFit 0.15.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 1516 su almeno 50 |
| average trade | **no** | 33 contro la soglia 109 |
| anni | passa | 15 anni, minimo 28 trade in un anno, 105.5 all'anno, 10 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 26,562 |
| ultimo terzo | **no** | netto 26,562, net/DD 0.62 (serve 1,3) |
| due terzi su tre | passa | 1,581 / 22,211 / 26,562 |
| outlier | **no** | trade migliore 21% del netto sulla storia, 39% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 49 con i pattern, -34 senza |
| pattern casuali | passa | 11 estrazioni su 53 fanno almeno altrettanto, p 0.22, Wilson 0.16 |
| plateau | passa | media 0.87 su 2 vicini, minimo 0.75 |

**Prova su FTMO**: 133 trade, netto -12,131, DD 65,999, net/DD -0.18, average trade -91 (soglia 509), UngerFit , finestre in utile 1/4 (0 / 0 / 5,688 / -17,819).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.18 (serve 1), average trade -91 (soglia 509), finestre 1/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 331 trade 5,900 net/DD 0.10, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 11/17 anni in utile (serve 60%), anno migliore 55% del netto (massimo 40%); average trade / soglia per anno: 2008:-1.0 2009:0.8 2010:-0.9 2011:1.0 2012:0.5 2013:1.4 2014:1.4 2015:0.5 2016:-0.3 2017:1.2 2018:0.8 2019:1.2 2020:0.5 2021:-0.1 2022:-1.6 2025:-1.2 2026:0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=5, IntradayOnly=0, ExitHour=-1, StartHour=17, EndHour=-1, FastYesLong=152, FastYesShort=21, FastNoLong=153, FastNoShort=143, SkipDay=-1`

## BO Breakout di N sessioni

594 simulazioni in 40.0 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=20, IncludeCurrentSession=1 | ✔ | nessuna in utile: netto massimo -96,359 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -30,169 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -30,169 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -16,998 (migliore -16,998), 299 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -13,483; annullato dall'ultimo terzo (-8,484 -> -15,039) |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 6 | ✔ | batte lo spento: netto 238 contro -18,480, net/DD 0.03 contro -0.82 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 1 | ✔ | batte lo spento: netto 2,966 contro 238, net/DD 0.54 contro 0.03 |
| uscita a ora fissa | ExitHour | -1 | 22 | · | batte lo spento: netto 4,015 contro 2,966, net/DD 0.76 contro 0.54; annullato dall'ultimo terzo (1,300 -> -708) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 3,078 (migliore 3,078), 87 trade |

**Storia di ricerca**: 187 trade, netto 4,266, DD 25,547, average trade 23, UngerFit 0.14.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 187 su almeno 50 |
| average trade | **no** | 23 contro la soglia 109 |
| anni | **no** | 15 anni, minimo 4 trade in un anno, 13.0 all'anno, 6 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 1,300 |
| ultimo terzo | **no** | netto 1,300, net/DD 0.05 (serve 1,3) |
| due terzi su tre | passa | -5,224 / 8,190 / 1,300 |
| outlier | **no** | trade migliore 293% del netto sulla storia, 961% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 13 con i pattern, 9 senza |
| pattern casuali | passa | 1 estrazioni su 9 fanno almeno altrettanto, p 0.20, Wilson 0.08 |
| plateau | **no** | media 0.53 su 4 vicini, minimo 0.00 |

**Prova su FTMO**: 16 trade, netto -7,112, DD 8,713, net/DD -0.82, average trade -445 (soglia 509), UngerFit , finestre in utile 0/4 (0 / 0 / -4,006 / -3,106).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.82 (serve 1), average trade -445 (soglia 509), finestre 0/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 48 trade 2,218 net/DD 0.10, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 6/17 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2008:-3.4 2009:-4.9 2010:-5.4 2011:-1.4 2012:-1.0 2013:5.2 2014:0.5 2015:7.8 2016:0.3 2017:-0.7 2018:-1.3 2019:1.1 2020:-0.3 2021:-0.5 2022:2.4 2025:-2.4 2026:-0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=17, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=6, PtnDirNo=53, SkipDay=1, Sessions=5, BreakoutOffsetTicks=20`

## BOS Breakout della sessione in corso

424 simulazioni in 35.5 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=0 | ✔ | nessuna in utile: netto massimo -135,477 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -58,155 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -58,155 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -39,542 (migliore -39,542), 619 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -27,356; annullato dall'ultimo terzo (50,938 -> 38,591) |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato -39,542 (migliore -39,542), 619 trade |

**Storia di ricerca**: 1082 trade, netto 12,051, DD 48,333, average trade 11, UngerFit 0.05.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 1082 su almeno 50 |
| average trade | **no** | 11 contro la soglia 109 |
| anni | **no** | 15 anni, minimo 37 trade in un anno, 75.3 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 50,938 |
| ultimo terzo | passa | netto 50,938, net/DD 2.44 (serve 1,3) |
| due terzi su tre | **no** | -16,706 / -22,181 / 50,938 |
| outlier | **no** | trade migliore 118% del netto sulla storia, 28% sull'ultimo terzo |
| plateau | **no** | media 0.38 su 2 vicini, minimo 0.00 |

**Prova su FTMO**: 107 trade, netto 49,935, DD 29,178, net/DD 1.71, average trade 467 (soglia 509), UngerFit 1.21, finestre in utile 2/4 (0 / 0 / 2,732 / 47,203).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.71 (serve 1), average trade 467 (soglia 509), finestre 2/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 240 trade 21,119 net/DD 0.58, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 6/17 anni in utile (serve 60%), anno migliore 80% del netto (massimo 40%); average trade / soglia per anno: 2008:-1.3 2009:-0.9 2010:-2.4 2011:-0.3 2012:-1.5 2013:0.2 2014:-2.5 2015:0.0 2016:-1.7 2017:-1.6 2018:0.4 2019:0.0 2020:1.4 2021:0.5 2022:-0.6 2025:0.8 2026:0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=17, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=0`

## VBO Volatility breakout

406 simulazioni in 29.1 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=2.0, Direction=2 | ✔ | nessuna in utile: netto massimo -5,938 |
| finestra: inizio | StartHour | -1 | 9 | ✔ | nessuna in utile: netto massimo -2,716 |
| finestra: fine | EndHour | -1 | 12 | ✔ | avg smussato 0.0, netto 1, 53 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 363 (migliore 363), 53 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 33.1, netto 1,753, 53 trade; annullato dall'ultimo terzo (-15,847 -> -28,245) |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 1 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 1 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 2 candidati batte lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (89 trade, netto -15,391).

## MAC Incrocio di medie

119 simulazioni in 9.4 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=30, SlowPeriod=200, Direction=2 | ✔ | avg smussato 147.4, netto 7,810, 53 trade |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 10,922 (migliore 10,922), 53 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 233.6, netto 12,379, 53 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 10,922 (migliore 10,922), 53 trade |

**Storia di ricerca**: 74 trade, netto 34,866, DD 6,876, average trade 471, UngerFit 5.44.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 74 su almeno 50 |
| average trade | passa | 471 contro la soglia 109 |
| anni | **no** | 15 anni, minimo 2 trade in un anno, 5.1 all'anno, 9 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 22,487 |
| ultimo terzo | passa | netto 22,487, net/DD 3.27 (serve 1,3) |
| due terzi su tre | passa | 2,712 / 9,667 / 22,487 |
| outlier | **no** | trade migliore 55% del netto sulla storia, 85% sull'ultimo terzo |
| plateau | **no** | media 0.25 su 3 vicini, minimo 0.00 |

**Prova su FTMO**: 5 trade, netto -4,574, DD 9,941, net/DD -0.46, average trade -915 (soglia 509), UngerFit , finestre in utile 0/4 (0 / 0 / 0 / -4,574).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.46 (serve 1), average trade -915 (soglia 509), finestre 0/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 15 trade 10,124 net/DD 1.15, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 10/17 anni in utile (serve 60%), anno migliore 76% del netto (massimo 40%); average trade / soglia per anno: 2008:8.9 2009:-3.3 2010:7.3 2011:-0.5 2012:5.7 2013:-0.2 2014:-5.7 2015:5.5 2016:12.2 2017:0.5 2018:-5.1 2019:-0.7 2020:2.7 2021:1.9 2022:22.9 2025:-6.6 2026:0.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=30, SlowPeriod=200, Direction=2`

## RBM Reversal Bollinger mirrored

473 simulazioni in 39.1 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -15,973 |
| finestra: inizio | StartHour | -1 | 17 | · | nessuna in utile: netto massimo -2,210; annullato dall'ultimo terzo (8,183 -> -3,812) |
| finestra: fine | EndHour | -1 | 4 | ✔ | avg smussato 60.0, netto 698, 68 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -381 (migliore -381), 68 trade; annullato dall'ultimo terzo (23,829 -> 22,313) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 28 | · | batte lo spento: netto 6,258 contro 698, net/DD 1.81 contro 0.09; annullato dall'ultimo terzo (23,829 -> 19,981) |
| NO | PtnNeutNo | 56 | 24 | · | batte lo spento: netto 6,258 contro 698, net/DD 1.81 contro 0.09; annullato dall'ultimo terzo (23,829 -> 19,981) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | -38 | · | batte lo spento: netto 2,490 contro 698, net/DD 0.41 contro 0.09; annullato dall'ultimo terzo (23,829 -> 18,513) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 3,763 contro 698, net/DD 0.84 contro 0.09; annullato dall'ultimo terzo (23,829 -> 5,492) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 10.3, netto 698, 68 trade |
| uscita a ora fissa | ExitHour | -1 | 18 | · | batte lo spento: netto 1,538 contro 698, net/DD 0.24 contro 0.09; annullato dall'ultimo terzo (23,829 -> 23,420) |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -381 (migliore -381), 68 trade; annullato dall'ultimo terzo (23,829 -> 22,313) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 4 | · | avg smussato 6.4, netto 698, 68 trade |

**Storia di ricerca**: 114 trade, netto 24,527, DD 8,726, average trade 215, UngerFit 2.20.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 114 su almeno 50 |
| average trade | passa | 215 contro la soglia 109 |
| anni | **no** | 15 anni, minimo 2 trade in un anno, 7.9 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 23,829 |
| ultimo terzo | passa | netto 23,829, net/DD 2.73 (serve 1,3) |
| due terzi su tre | passa | -1,849 / 2,547 / 23,829 |
| outlier | **no** | trade migliore 47% del netto sulla storia, 49% sull'ultimo terzo |
| plateau | passa | media 0.66 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 12 trade, netto 21,922, DD 12,021, net/DD 1.82, average trade 1,827 (soglia 509), UngerFit 7.39, finestre in utile 2/4 (0 / 0 / 3,269 / 18,653).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.82 (serve 1), average trade 1,827 (soglia 509), finestre 2/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 21 trade -18,011 net/DD -0.61, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 9/17 anni in utile (serve 60%), anno migliore 50% del netto (massimo 40%); average trade / soglia per anno: 2008:1.8 2009:-1.4 2010:-5.1 2011:2.5 2012:-8.4 2013:-4.9 2014:1.2 2015:-2.9 2016:3.0 2017:7.2 2018:-5.2 2019:-1.3 2020:3.8 2021:2.0 2022:10.1 2025:-0.7 2026:5.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=4, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=30, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

970 simulazioni in 63.9 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -15,973 |
| finestra: inizio | StartHour | -1 | 17 | · | nessuna in utile: netto massimo -2,210; annullato dall'ultimo terzo (8,183 -> -3,812) |
| finestra: fine | EndHour | -1 | 4 | ✔ | avg smussato 60.0, netto 698, 68 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -381 (migliore -381), 68 trade; annullato dall'ultimo terzo (23,829 -> 22,313) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 28 | · | batte lo spento: netto 6,672 contro 698, net/DD 1.66 contro 0.09; annullato dall'ultimo terzo (23,829 -> 19,981) |
| YES short | FastYesShort | 152 | 25 | · | batte lo spento: netto 1,669 contro 698, net/DD 0.21 contro 0.09; annullato dall'ultimo terzo (23,829 -> 19,695) |
| NO long | FastNoLong | 153 | 11 | · | batte lo spento: netto 6,718 contro 698, net/DD 2.55 contro 0.09; annullato dall'ultimo terzo (23,829 -> 16,383) |
| NO short | FastNoShort | 153 | 22 | ✔ | batte lo spento: netto 2,052 contro 698, net/DD 0.26 contro 0.09 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 36.6, netto 2,052, 56 trade |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 3,017 contro 2,052, net/DD 0.50 contro 0.26; annullato dall'ultimo terzo (23,941 -> 13,302) |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 902 (migliore 902), 56 trade; annullato dall'ultimo terzo (23,941 -> 22,424) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 2 | · | avg smussato 32.1, netto 1,541, 56 trade; annullato dall'ultimo terzo (23,941 -> 7,367) |

**Storia di ricerca**: 92 trade, netto 25,992, DD 9,078, average trade 283, UngerFit 2.84.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 92 su almeno 50 |
| average trade | passa | 283 contro la soglia 109 |
| anni | **no** | 15 anni, minimo 2 trade in un anno, 6.4 all'anno, 8 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 23,941 |
| ultimo terzo | passa | netto 23,941, net/DD 2.64 (serve 1,3) |
| due terzi su tre | passa | -1,763 / 3,814 / 23,941 |
| outlier | **no** | trade migliore 45% del netto sulla storia, 49% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 665 con i pattern, 518 senza |
| pattern casuali | **no** | 20 estrazioni su 50 fanno almeno altrettanto, p 0.41, Wilson 0.33 |
| plateau | passa | media 0.63 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 11 trade, netto 14,741, DD 12,021, net/DD 1.23, average trade 1,340 (soglia 509), UngerFit 5.42, finestre in utile 1/4 (0 / 0 / -3,913 / 18,653).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.23 (serve 1), average trade 1,340 (soglia 509), finestre 1/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 17 trade -13,778 net/DD -0.54, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 9/17 anni in utile (serve 60%), anno migliore 57% del netto (massimo 40%); average trade / soglia per anno: 2008:1.8 2009:-1.4 2010:-6.4 2011:2.5 2012:-9.6 2013:-2.6 2014:0.6 2015:-4.4 2016:3.0 2017:12.0 2018:-5.2 2019:-1.5 2020:4.4 2021:2.5 2022:10.1 2025:-5.7 2026:5.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=4, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=22, SkipDay=-1, BbLength=30, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

559 simulazioni in 33.4 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=2 | ✔ | nessuna in utile: netto massimo -32,913 |
| finestra: inizio | StartHour | -1 | 17 | · | nessuna in utile: netto massimo -30,045; annullato dall'ultimo terzo (-21,782 -> -81,044) |
| finestra: fine | EndHour | -1 | 1 | · | avg smussato 20.8, netto 3,918, 188 trade; annullato dall'ultimo terzo (-21,782 -> -34,801) |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato -33,339 (migliore -33,339), 1120 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 8 | ✔ | batte lo spento: netto 3,607 contro -32,913, net/DD 1.04 contro -0.71 |
| NO | PtnNeutNo | 56 | 46 | · | batte lo spento: netto 4,421 contro 3,607, net/DD 1.37 contro 1.04; annullato dall'ultimo terzo (12,292 -> 8,319) |
| direzionale YES | PtnDirYes | 52 | -2 | ✔ | batte lo spento: netto 4,518 contro 3,607, net/DD 2.32 contro 1.04 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 106.6, netto 5,330, 50 trade; annullato dall'ultimo terzo (20,299 -> 16,369) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 4,747 (migliore 4,747), 52 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 92.3, netto 4,797, 52 trade |

**Storia di ricerca**: 83 trade, netto 27,473, DD 2,264, average trade 331, UngerFit 6.65.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 83 su almeno 50 |
| average trade | passa | 331 contro la soglia 109 |
| anni | **no** | 14 anni, minimo 4 trade in un anno, 5.8 all'anno, 11 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 22,676 |
| ultimo terzo | passa | netto 22,676, net/DD 10.70 (serve 1,3) |
| due terzi su tre | passa | 3,341 / 1,456 / 22,676 |
| outlier | passa | trade migliore 18% del netto sulla storia, 22% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 731 con i pattern, 17 senza |
| pattern casuali | passa | 2 estrazioni su 66 fanno almeno altrettanto, p 0.04, Wilson 0.02 |
| plateau | passa | media 0.92 su 3 vicini, minimo 0.83 |

**Prova su FTMO**: 7 trade, netto -1,326, DD 9,820, net/DD -0.14, average trade -189 (soglia 509), UngerFit , finestre in utile 0/4 (0 / 0 / 0 / -1,326).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.14 (serve 1), average trade -189 (soglia 509), finestre 0/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 22 trade 22,610 net/DD 2.25, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 12/16 anni in utile (serve 60%), anno migliore 53% del netto (massimo 40%); average trade / soglia per anno: 2008:7.6 2009:-2.2 2010:0.9 2011:0.6 2012:4.4 2013:1.7 2014:7.7 2015:0.3 2016:-2.2 2017:-0.4 2018:2.5 2019:3.7 2020:5.6 2021:1.6 2025:2.2 2026:-1.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=8, PtnNeutNo=56, PtnDirYes=-2, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=20, Direction=2`

## LF Level fader sul pivot di ieri

502 simulazioni in 26.0 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -16,671 |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato -16,400 (migliore -16,400), 329 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 30 | ✔ | batte lo spento: netto 1,772 contro -16,027, net/DD 0.47 contro -0.95 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 1 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| calendario long | NotEntryDayLong | -1 | 1 | ✔ | batte lo spento: netto 2,758 contro 1,772, net/DD 0.71 contro 0.47 |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 53.0, netto 2,758, 52 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.6 | 0.8 | · | D2: netto smussato 2,754 (migliore 2,754), 52 trade; annullato dall'ultimo terzo (28,034 -> 24,987) |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 20 | · | avg smussato 65.4, netto 3,337, 51 trade; annullato dall'ultimo terzo (28,034 -> 27,270) |

**Storia di ricerca**: 95 trade, netto 30,792, DD 9,037, average trade 324, UngerFit 3.26.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 95 su almeno 50 |
| average trade | passa | 324 contro la soglia 109 |
| anni | **no** | 15 anni, minimo 3 trade in un anno, 6.6 all'anno, 10 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 28,034 |
| ultimo terzo | passa | netto 28,034, net/DD 3.10 (serve 1,3) |
| due terzi su tre | passa | -557 / 3,315 / 28,034 |
| outlier | **no** | trade migliore 36% del netto sulla storia, 40% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 652 con i pattern, 93 senza |
| pattern casuali | passa | 2 estrazioni su 34 fanno almeno altrettanto, p 0.09, Wilson 0.04 |
| plateau | passa | media 0.79 su 5 vicini, minimo 0.57 |

**Prova su FTMO**: 11 trade, netto 10,295, DD 6,884, net/DD 1.50, average trade 936 (soglia 509), UngerFit 5.00, finestre in utile 1/4 (0 / 0 / 0 / 10,295).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.50 (serve 1), average trade 936 (soglia 509), finestre 1/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 22 trade -6,366 net/DD -0.36, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 12/17 anni in utile (serve 60%), anno migliore 50% del netto (massimo 40%); average trade / soglia per anno: 2008:-2.7 2009:-2.8 2010:-4.7 2011:-0.8 2012:6.7 2013:1.0 2014:3.3 2015:1.4 2016:2.8 2017:3.3 2018:0.0 2019:-2.8 2020:0.9 2021:2.9 2022:8.2 2025:0.8 2026:2.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=30, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=1, NotEntryDayShort=-1, LevelShift=20`

## LFHL Level fader sugli estremi di ieri

510 simulazioni in 27.6 minuti, conferma dal 2017-07-31.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | nessuna in utile: netto massimo -23,313 |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato -24,330 (migliore -24,330), 599 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 12 | ✔ | batte lo spento: netto 3,063 contro -25,380, net/DD 0.24 contro -0.77 |
| NO | PtnNeutNo | 56 | 11 | · | batte lo spento: netto 9,068 contro 3,063, net/DD 2.74 contro 0.24; annullato dall'ultimo terzo (23,344 -> 14,732) |
| direzionale YES | PtnDirYes | 52 | -45 | · | batte lo spento: netto 4,979 contro 3,063, net/DD 1.24 contro 0.24; annullato dall'ultimo terzo (23,344 -> -936) |
| calendario long | NotEntryDayLong | -1 | 0 | · | batte lo spento: netto 7,498 contro 3,063, net/DD 0.95 contro 0.24; annullato dall'ultimo terzo (23,344 -> 12,354) |
| calendario short | NotEntryDayShort | -1 | 1 | · | batte lo spento: netto 4,187 contro 3,063, net/DD 0.35 contro 0.24; annullato dall'ultimo terzo (23,344 -> 12,438) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 12.8, netto 3,063, 240 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 1.0 | 0.6 | · | D2: netto smussato 4,499 (migliore 4,499), 241 trade; annullato dall'ultimo terzo (23,344 -> 15,509) |
| affinamento target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 4,112 contro 3,063, net/DD 0.32 contro 0.24; annullato dall'ultimo terzo (23,344 -> 21,114) |
| durata | MaxBars | 4 | 2 | ✔ | avg smussato 15.5, netto 4,559, 251 trade |

**Storia di ricerca**: 428 trade, netto 34,132, DD 18,180, average trade 80, UngerFit 0.57.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 428 su almeno 50 |
| average trade | **no** | 80 contro la soglia 109 |
| anni | passa | 15 anni, minimo 16 trade in un anno, 29.8 all'anno, 9 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 29,573 |
| ultimo terzo | passa | netto 29,573, net/DD 1.63 (serve 1,3) |
| due terzi su tre | passa | -874 / 5,433 / 29,573 |
| outlier | **no** | trade migliore 32% del netto sulla storia, 37% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 167 con i pattern, 111 senza |
| pattern casuali | **no** | 9 estrazioni su 22 fanno almeno altrettanto, p 0.43, Wilson 0.31 |
| plateau | passa | media 0.82 su 5 vicini, minimo 0.57 |

**Prova su FTMO**: 54 trade, netto -38,310, DD 53,210, net/DD -0.72, average trade -709 (soglia 509), UngerFit , finestre in utile 0/4 (0 / 0 / 0 / -38,310).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.72 (serve 1), average trade -709 (soglia 509), finestre 0/4 (servono 3) |
| due feed | **no** | 2022-05 → 2025-05: interno 119 trade 9,903 net/DD 0.63, FTMO 0 trade 0 net/DD 0.00 |
| anni | **no** | 10/17 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2008:0.3 2009:0.8 2010:-0.8 2011:0.2 2012:-3.9 2013:-1.4 2014:-0.4 2015:0.4 2016:1.7 2017:1.9 2018:2.4 2019:-1.3 2020:0.8 2021:2.0 2022:-0.6 2025:1.9 2026:-2.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=2, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=12, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=10`

## Riepilogo

- PCH: scartata; ricerca 139 trade netto 42,154 avg 303; prova 15 trade netto -1,172 net/DD -0.03 avg -78 fin 1/4; falliti: anni, outlier, prova sul broker, due feed, anni
- TFM: scartata; ricerca 459 trade netto 39,833 avg 87; prova 40 trade netto 30,057 net/DD 1.63 avg 751 fin 2/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, due feed, anni
- TFU: scartata; ricerca 1516 trade netto 50,354 avg 33; prova 133 trade netto -12,131 net/DD -0.18 avg -91 fin 1/4; falliti: average trade, ultimo terzo, outlier, prova sul broker, due feed, anni
- BO: scartata; ricerca 187 trade netto 4,266 avg 23; prova 16 trade netto -7,112 net/DD -0.82 avg -445 fin 0/4; falliti: average trade, anni, ultimo terzo, outlier, plateau, prova sul broker, due feed, anni
- BOS: scartata; ricerca 1082 trade netto 12,051 avg 11; prova 107 trade netto 49,935 net/DD 1.71 avg 467 fin 2/4; falliti: average trade, anni, due terzi su tre, outlier, plateau, prova sul broker, due feed, anni
- VBO: abbandonato (R9: la base non guadagna su tutta la storia (89 trade, netto -15,391))
- MAC: scartata; ricerca 74 trade netto 34,866 avg 471; prova 5 trade netto -4,574 net/DD -0.46 avg -915 fin 0/4; falliti: anni, outlier, plateau, prova sul broker, due feed, anni
- RBM: scartata; ricerca 114 trade netto 24,527 avg 215; prova 12 trade netto 21,922 net/DD 1.82 avg 1,827 fin 2/4; falliti: anni, outlier, prova sul broker, due feed, anni
- RBU: scartata; ricerca 92 trade netto 25,992 avg 283; prova 11 trade netto 14,741 net/DD 1.23 avg 1,340 fin 1/4; falliti: anni, outlier, pattern casuali, prova sul broker, due feed, anni
- RHL: scartata; ricerca 83 trade netto 27,473 avg 331; prova 7 trade netto -1,326 net/DD -0.14 avg -189 fin 0/4; falliti: anni, prova sul broker, due feed, anni
- LF: scartata; ricerca 95 trade netto 30,792 avg 324; prova 11 trade netto 10,295 net/DD 1.50 avg 936 fin 1/4; falliti: anni, outlier, prova sul broker, due feed, anni
- LFHL: scartata; ricerca 428 trade netto 34,132 avg 80; prova 54 trade netto -38,310 net/DD -0.72 avg -709 fin 0/4; falliti: average trade, outlier, pattern casuali, prova sul broker, due feed, anni
