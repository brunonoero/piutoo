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

## Riepilogo

- PCH: scartata; ricerca 835 trade netto 84,933 avg 102; prova 430 trade netto 4,075 net/DD 0.13 avg 9 fin 2/4; falliti: average trade, pattern casuali, prova sul broker
- TFM: scartata; ricerca 879 trade netto 11,667 avg 13; prova 459 trade netto 3,389 net/DD 0.11 avg 7 fin 2/4; falliti: average trade, anni, due terzi su tre, outlier, prova sul broker, anni
- TFU: scartata; ricerca 879 trade netto 11,667 avg 13; prova 459 trade netto 3,389 net/DD 0.11 avg 7 fin 2/4; falliti: average trade, anni, due terzi su tre, outlier, prova sul broker, anni
