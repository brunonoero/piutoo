# Percorso v4 — @SI 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (6,023 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,218 barre), mai vista dal percorso
- costi FTMO: spread 0.056 punti (mediana), swap long 0.0165 short 0 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 187 nella ricerca, 654 nella prova

## PCH Price Channel

506 simulazioni in 13.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=100, OffsetTicks=5, Direction=2 | ✔ | nessuna in utile: netto massimo -15,556 |
| finestra: inizio | StartHour | -1 | 1 | · | nessuna in utile: netto massimo -13,145; annullato dall'ultimo terzo (-20,089 -> -20,284) |
| finestra: fine | EndHour | -1 | 8 | ✔ | nessuna in utile: netto massimo -12,990 |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato -13,480 (migliore -13,480), 59 trade; annullato dall'ultimo terzo (-15,229 -> -16,976) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -12,990 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (86 trade, netto -28,219).

## TFM Trend following mirrored

386 simulazioni in 10.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -164,555 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -164,555 |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato -159,872 (migliore -159,872), 402 trade; annullato dall'ultimo terzo (-53,743 -> -66,845) |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -155,456; annullato dall'ultimo terzo (-53,743 -> -53,907) |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (615 trade, netto -220,483).

## TFU Trend following unmirrored

680 simulazioni in 16.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -164,555 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -164,555 |
| stop | StopAtr | 0.8 | 3.0 | · | D2: netto smussato -159,872 (migliore -159,872), 402 trade; annullato dall'ultimo terzo (-53,743 -> -66,845) |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -155,456; annullato dall'ultimo terzo (-53,743 -> -53,907) |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 152 | · | nessun valore sopra lo spento |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 153 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (615 trade, netto -220,483).

## BO Breakout di N sessioni

454 simulazioni in 11.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=0, IncludeCurrentSession=1 | ✔ | nessuna in utile: netto massimo -98,989 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -40,774 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -40,774 |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato -28,910 (migliore -28,910), 90 trade; annullato dall'ultimo terzo (-22,132 -> -25,325) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -40,774 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (147 trade, netto -65,090).

## BOS Breakout della sessione in corso

391 simulazioni in 9.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=20 | ✔ | nessuna in utile: netto massimo -248,558 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -55,758 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -55,758 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -48,625 (migliore -48,625), 90 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -44,758; annullato dall'ultimo terzo (-20,790 -> -23,828) |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (143 trade, netto -65,823).

## VBO Volatility breakout

402 simulazioni in 9.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.5, Direction=2 | ✔ | nessuna in utile: netto massimo -23,757 |
| finestra: inizio | StartHour | -1 | -1 | · | nessuna in utile: netto massimo -23,757 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -23,757 |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -22,222 (migliore -22,222), 50 trade; annullato dall'ultimo terzo (-4,010 -> -5,520) |
| intraday o overnight | IntradayOnly | 1 | 0 | · | nessuna in utile: netto massimo -5,067; annullato dall'ultimo terzo (-4,010 -> -8,610) |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (72 trade, netto -27,767).

## MAC Incrocio di medie

128 simulazioni in 3.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=10, SlowPeriod=100, Direction=0 | ✔ | avg smussato 98.7, netto 6,417, 65 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 7,851 (migliore 7,851), 63 trade; annullato dall'ultimo terzo (37,073 -> 35,156) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 98.7, netto 6,417, 65 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 11,106 contro 6,417, net/DD 0.48 contro 0.28; annullato dall'ultimo terzo (37,073 -> 26,094) |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 8,568 (migliore 8,568), 60 trade; annullato dall'ultimo terzo (37,073 -> 35,587) |

**Storia di ricerca**: 98 trade, netto 43,490, DD 22,594, average trade 444, UngerFit 2.16.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 98 su almeno 50 |
| average trade | passa | 444 contro la soglia 187 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 25.7 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 37,073 |
| ultimo terzo | passa | netto 37,073, net/DD 5.53 (serve 1,3) |
| due terzi su tre | passa | 21,870 / -15,453 / 37,073 |
| outlier | passa | trade migliore 25% del netto sulla storia, 30% sull'ultimo terzo |
| plateau | **no** | media 0.43 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 46 trade, netto 21,568, DD 45,937, net/DD 0.47, average trade 469 (soglia 654), UngerFit 0.86, finestre in utile 3/4 (8,419 / -4,860 / 10,535 / 7,473).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.47 (serve 1), average trade 469 (soglia 654), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 47% del netto (massimo 40%); average trade / soglia per anno: 2020:-2.0 2021:4.8 2022:0.5 2023:-0.3 2024:5.4 2025:1.1 2026:0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=10, SlowPeriod=100, Direction=0`

## RBM Reversal Bollinger mirrored

414 simulazioni in 10.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -26,492 |
| finestra: inizio | StartHour | -1 | 9 | ✔ | nessuna in utile: netto massimo -11,116 |
| finestra: fine | EndHour | -1 | 0 | · | nessuna in utile: netto massimo -9,978; annullato dall'ultimo terzo (-15,687 -> -16,643) |
| stop | StopAtr | 0.8 | 3.0 | ✔ | D2: netto smussato -11,515 (migliore -11,515), 69 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | -36 | ✔ | batte lo spento: netto 918 contro -12,150, net/DD 0.06 contro -0.60 |

**Abbandonato**: R9: la base non guadagna su tutta la storia (82 trade, netto -11,555).

## RBU Reversal Bollinger unmirrored

710 simulazioni in 17.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=50, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -26,492 |
| finestra: inizio | StartHour | -1 | 9 | ✔ | nessuna in utile: netto massimo -11,116 |
| finestra: fine | EndHour | -1 | 0 | · | nessuna in utile: netto massimo -9,978; annullato dall'ultimo terzo (-15,687 -> -16,643) |
| stop | StopAtr | 0.8 | 3.0 | ✔ | D2: netto smussato -11,515 (migliore -11,515), 69 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 93 | ✔ | batte lo spento: netto 858 contro -12,150, net/DD 0.09 contro -0.60 |
| YES short | FastYesShort | 152 | 152 | · | nessuno dei 2 candidati batte lo spento |
| NO long | FastNoLong | 153 | 51 | ✔ | batte lo spento: netto 2,135 contro 858, net/DD 0.23 contro 0.09 |
| NO short | FastNoShort | 153 | 153 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (68 trade, netto -6,252).

## RHL Reversal sui livelli di ieri

411 simulazioni in 9.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=2 | ✔ | nessuna in utile: netto massimo -49,892 |
| finestra: inizio | StartHour | -1 | 17 | ✔ | nessuna in utile: netto massimo -9,417 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -9,417 |
| stop | StopAtr | 0.8 | 3.0 | ✔ | D2: netto smussato -970 (migliore -970), 117 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 18 | · | batte lo spento: netto 16,325 contro -970, net/DD 2.54 contro -0.10; annullato dall'ultimo terzo (-7,040 -> -12,635) |
| NO | PtnNeutNo | 56 | 12 | · | batte lo spento: netto 16,325 contro -970, net/DD 2.54 contro -0.10; annullato dall'ultimo terzo (-7,040 -> -12,635) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 26 | · | batte lo spento: netto 7,735 contro -970, net/DD 1.10 contro -0.10; annullato dall'ultimo terzo (-7,040 -> -13,880) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (181 trade, netto -8,010).

## LF Level fader sul pivot di ieri

242 simulazioni in 5.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | avg smussato 12.4, netto 1,032, 83 trade |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 140 (migliore 140), 83 trade |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 13 | ✔ | batte lo spento: netto 13,216 contro -620, net/DD 2.45 contro -0.06 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (83 trade, netto -894).

## LFHL Level fader sugli estremi di ieri

500 simulazioni in 8.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=20 | ✔ | nessuna in utile: netto massimo -13,758 |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato -10,481 (migliore -10,481), 130 trade; annullato dall'ultimo terzo (-14,482 -> -16,088) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 54 | ✔ | batte lo spento: netto 6,839 contro -13,758, net/DD 0.78 contro -0.68 |
| NO | PtnNeutNo | 56 | 8 | ✔ | batte lo spento: netto 13,355 contro 6,839, net/DD 2.64 contro 0.78 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 256.8, netto 13,355, 52 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 12,842 (migliore 12,842), 52 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 256.5, netto 13,333, 52 trade; annullato dall'ultimo terzo (-10,040 -> -11,065) |

**Storia di ricerca**: 81 trade, netto 3,315, DD 15,112, average trade 41, UngerFit 0.24.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 81 su almeno 50 |
| average trade | **no** | 41 contro la soglia 187 |
| anni | **no** | 5 anni, minimo 3 trade in un anno, 21.3 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -10,040 |
| ultimo terzo | **no** | netto -10,040, net/DD -0.75 (serve 1,3) |
| due terzi su tre | passa | 6,975 / 6,380 / -10,040 |
| outlier | **no** | trade migliore 160% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -346 con i pattern, -234 senza |
| pattern casuali | **no** | 12 estrazioni su 18 fanno almeno altrettanto, p 0.68, Wilson 0.53 |
| plateau | **no** | media 0.46 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 45 trade, netto -28,009, DD 54,454, net/DD -0.51, average trade -622 (soglia 654), UngerFit , finestre in utile 1/4 (-7,994 / 625 / -15,957 / -6,469).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.51 (serve 1), average trade -622 (soglia 654), finestre 1/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:0.6 2021:2.0 2022:2.1 2023:-0.9 2024:-3.2 2025:-0.7 2026:-0.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=54, PtnNeutNo=8, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=20`

## Riepilogo

- PCH: abbandonato (R9: la base non guadagna su tutta la storia (86 trade, netto -28,219))
- TFM: abbandonato (R9: la base non guadagna su tutta la storia (615 trade, netto -220,483))
- TFU: abbandonato (R9: la base non guadagna su tutta la storia (615 trade, netto -220,483))
- BO: abbandonato (R9: la base non guadagna su tutta la storia (147 trade, netto -65,090))
- BOS: abbandonato (R9: la base non guadagna su tutta la storia (143 trade, netto -65,823))
- VBO: abbandonato (R9: la base non guadagna su tutta la storia (72 trade, netto -27,767))
- MAC: scartata; ricerca 98 trade netto 43,490 avg 444; prova 46 trade netto 21,568 net/DD 0.47 avg 469 fin 3/4; falliti: anni, plateau, prova sul broker, anni
- RBM: abbandonato (R9: la base non guadagna su tutta la storia (82 trade, netto -11,555))
- RBU: abbandonato (R9: la base non guadagna su tutta la storia (68 trade, netto -6,252))
- RHL: abbandonato (R9: la base non guadagna su tutta la storia (181 trade, netto -8,010))
- LF: abbandonato (R9: la base non guadagna su tutta la storia (83 trade, netto -894))
- LFHL: scartata; ricerca 81 trade netto 3,315 avg 41; prova 45 trade netto -28,009 net/DD -0.51 avg -622 fin 1/4; falliti: average trade, anni, utile recente, ultimo terzo, outlier, pattern utile, pattern casuali, plateau, prova sul broker, anni
