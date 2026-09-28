# Percorso v4 — @CL 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (22,539 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,277 barre), mai vista dal percorso
- costi FTMO: spread 0.081 punti (mediana), swap long 0 short 0.2253 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 75 nella ricerca, 79 nella prova

## PCH Price Channel

756 simulazioni in 33.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=20, OffsetTicks=2, Direction=1 | ✔ | nessuna in utile: netto massimo -27,831 |
| finestra: inizio | StartHour | -1 | 9 | ✔ | avg smussato -16.9, netto 1,749, 398 trade |
| finestra: fine | EndHour | -1 | 10 | ✔ | avg smussato 146.4, netto 18,422, 118 trade |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 19,323 (migliore 20,197), 118 trade; annullato dall'ultimo terzo (-540 -> -3,273) |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 189.2, netto 20,811, 110 trade; annullato dall'ultimo terzo (-540 -> -5,125) |
| YES | PtnNeutYes | 55 | 53 | ✔ | batte lo spento: netto 33,283 contro 18,422, net/DD 5.36 contro 1.49 |
| NO | PtnNeutNo | 56 | 40 | · | batte lo spento: netto 32,377 contro 33,283, net/DD 9.44 contro 5.36; annullato dall'ultimo terzo (2,230 -> -418) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 33,915 contro 33,283, net/DD 5.80 contro 5.36; annullato dall'ultimo terzo (2,230 -> -2,938) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 33,736 (migliore 33,736), 65 trade; annullato dall'ultimo terzo (2,230 -> 976) |

**Storia di ricerca**: 107 trade, netto 35,513, DD 11,422, average trade 332, UngerFit 3.58.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 107 su almeno 50 |
| average trade | passa | 332 contro la soglia 75 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 28.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,230 |
| ultimo terzo | **no** | netto 2,230, net/DD 0.33 (serve 1,3) |
| due terzi su tre | passa | 10,340 / 22,943 / 2,230 |
| outlier | **no** | trade migliore 20% del netto sulla storia, 108% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 53 con i pattern, -8 senza |
| pattern casuali | **no** | 3 estrazioni su 6 fanno almeno altrettanto, p 0.57, Wilson 0.33 |
| plateau | passa | media 0.93 su 6 vicini, minimo 0.76 |

**Prova su FTMO**: 60 trade, netto -1,331, DD 9,596, net/DD -0.14, average trade -22 (soglia 79), UngerFit , finestre in utile 2/4 (4,661 / -6,340 / -54 / 402).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.14 (serve 1), average trade -22 (soglia 79), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 96% del netto (massimo 40%); average trade / soglia per anno: 2020:-8.9 2021:4.2 2022:10.7 2023:-2.1 2024:1.1 2025:-2.2 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=9, EndHour=10, PtnNeutYes=53, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=20, OffsetTicks=2, Direction=1`

## TFM Trend following mirrored

522 simulazioni in 20.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -28,331 |
| finestra: fine | EndHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -25,831 |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato -25,831 (migliore -25,831), 344 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -25,831 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | -47 | ✔ | batte lo spento: netto 1,570 contro -25,831, net/DD 0.35 contro -0.98 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | 2 | ✔ | batte lo spento: netto 2,554 contro 1,570, net/DD 0.69 contro 0.35 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 1.0 | 0.6 | ✔ | D2: netto smussato 2,554 (migliore 2,554), 51 trade |

**Storia di ricerca**: 92 trade, netto -2,921, DD 6,569, average trade -32, UngerFit .

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 92 su almeno 50 |
| average trade | **no** | -32 contro la soglia 75 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 24.1 all'anno, 1 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -5,475 |
| ultimo terzo | **no** | netto -5,475, net/DD -1.00 (serve 1,3) |
| due terzi su tre | **no** | -3,404 / 5,958 / -5,475 |
| outlier | **no** | trade migliore  del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -134 con i pattern, -120 senza |
| pattern casuali | **no** | 16 estrazioni su 26 fanno almeno altrettanto, p 0.63, Wilson 0.50 |
| plateau | passa | UngerFit della candidata non positivo: nessun giudizio |

**Prova su FTMO**: 54 trade, netto -4,321, DD 6,495, net/DD -0.67, average trade -80 (soglia 79), UngerFit , finestre in utile 1/4 (671 / -3,049 / -1,776 / -167).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.67 (serve 1), average trade -80 (soglia 79), finestre 1/4 (servono 3) |
| anni | **no** | 1/7 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.5 2021:-1.7 2022:4.3 2023:-1.3 2024:-1.6 2025:-2.2 2026:-0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=21, EndHour=21, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=-47, PtnDirNo=53, SkipDay=2`

## TFU Trend following unmirrored

680 simulazioni in 28.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -28,331 |
| finestra: fine | EndHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -25,831 |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato -25,831 (migliore -25,831), 344 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -25,831 |
| YES long | FastYesLong | 152 | 152 | · | nessun valore sopra lo spento |
| YES short | FastYesShort | 152 | 152 | · | nessun valore sopra lo spento |
| NO long | FastNoLong | 153 | 153 | · | nessun valore sopra lo spento |
| NO short | FastNoShort | 153 | 153 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (526 trade, netto -47,755).

## BO Breakout di N sessioni

456 simulazioni in 29.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=5, BreakoutOffsetTicks=0, IncludeCurrentSession=1 | ✔ | nessuna in utile: netto massimo -70,303 |
| finestra: inizio | StartHour | -1 | 21 | ✔ | nessuna in utile: netto massimo -14,104 |
| finestra: fine | EndHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -12,023 |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato -10,163 (migliore -10,163), 59 trade; annullato dall'ultimo terzo (-7,176 -> -7,294) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -12,023 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (81 trade, netto -19,199).

## BOS Breakout della sessione in corso

391 simulazioni in 21.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=0 | ✔ | nessuna in utile: netto massimo -104,565 |
| finestra: inizio | StartHour | -1 | 22 | ✔ | nessuna in utile: netto massimo -25,404 |
| finestra: fine | EndHour | -1 | -1 | · | nessuna in utile: netto massimo -25,404 |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato -21,611 (migliore -21,611), 84 trade; annullato dall'ultimo terzo (-4,709 -> -4,748) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | nessuna in utile: netto massimo -25,404 |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | -3 | ✔ | batte lo spento: netto 282 contro -25,404, net/DD 0.05 contro -0.92 |

**Abbandonato**: R9: la base non guadagna su tutta la storia (72 trade, netto -564).

## VBO Volatility breakout

550 simulazioni in 20.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.7, Direction=2 | ✔ | nessuna in utile: netto massimo -750 |
| finestra: inizio | StartHour | -1 | 22 | ✔ | avg smussato -13.3, netto 2,445, 91 trade |
| finestra: fine | EndHour | -1 | 3 | · | avg smussato 134.4, netto 13,143, 98 trade; annullato dall'ultimo terzo (-4,817 -> -7,870) |
| stop | StopAtr | 0.8 | 2.5 | · | D2: netto smussato 1,118 (migliore 1,118), 91 trade; annullato dall'ultimo terzo (-4,817 -> -6,801) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 26.9, netto 2,445, 91 trade |
| YES | PtnNeutYes | 55 | 5 | · | batte lo spento: netto 14,308 contro 2,445, net/DD 0.69 contro 0.10; annullato dall'ultimo terzo (-4,817 -> -5,135) |
| NO | PtnNeutNo | 56 | 2 | · | batte lo spento: netto 14,308 contro 2,445, net/DD 0.69 contro 0.10; annullato dall'ultimo terzo (-4,817 -> -5,135) |
| direzionale YES | PtnDirYes | 52 | 8 | ✔ | batte lo spento: netto 20,551 contro 2,445, net/DD 1.40 contro 0.10 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 25,552 contro 20,551, net/DD 2.00 contro 1.40 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 23,549 (migliore 23,549), 53 trade; annullato dall'ultimo terzo (-2,979 -> -3,198) |

**Storia di ricerca**: 72 trade, netto 21,542, DD 12,763, average trade 299, UngerFit 3.05.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 72 su almeno 50 |
| average trade | passa | 299 contro la soglia 75 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 18.9 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -4,010 |
| ultimo terzo | **no** | netto -2,979, net/DD -0.36 (serve 1,3) |
| due terzi su tre | **no** | -7,080 / 32,632 / -2,979 |
| outlier | passa | trade migliore 30% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade -166 con i pattern, -121 senza |
| pattern casuali | **no** | 14 estrazioni su 21 fanno almeno altrettanto, p 0.68, Wilson 0.54 |
| plateau | passa | media 0.86 su 6 vicini, minimo 0.45 |

**Prova su FTMO**: 51 trade, netto 8,137, DD 12,496, net/DD 0.65, average trade 160 (soglia 79), UngerFit 1.61, finestre in utile 1/4 (-3,859 / -5,505 / -618 / 18,119).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.65 (serve 1), average trade 160 (soglia 79), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 98% del netto (massimo 40%); average trade / soglia per anno: 2020:13.4 2021:-5.0 2022:11.2 2023:3.9 2024:-5.8 2025:-6.2 2026:6.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=22, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=8, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.7, Direction=2`

## MAC Incrocio di medie

130 simulazioni in 6.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=15, SlowPeriod=50, Direction=1 | ✔ | avg smussato 173.9, netto 29,907, 172 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 31,291 (migliore 32,384), 172 trade; annullato dall'ultimo terzo (-7,180 -> -10,526) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 173.9, netto 29,907, 172 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 61,477 contro 29,907, net/DD 2.48 contro 1.03 |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 61,294 (migliore 61,355), 172 trade; annullato dall'ultimo terzo (-3,551 -> -8,293) |

**Storia di ricerca**: 255 trade, netto 57,926, DD 29,706, average trade 227, UngerFit 1.52.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 255 su almeno 50 |
| average trade | passa | 227 contro la soglia 75 |
| anni | passa | 5 anni, minimo 6 trade in un anno, 66.9 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -3,551 |
| ultimo terzo | **no** | netto -3,551, net/DD -0.16 (serve 1,3) |
| due terzi su tre | passa | 30,358 / 31,119 / -3,551 |
| outlier | passa | trade migliore 24% del netto sulla storia,  sull'ultimo terzo |
| plateau | **no** | media 0.57 su 8 vicini, minimo 0.03 |

**Prova su FTMO**: 136 trade, netto 9,548, DD 33,399, net/DD 0.29, average trade 70 (soglia 79), UngerFit 0.43, finestre in utile 2/4 (-18,264 / -6,541 / 1,542 / 33,606).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.29 (serve 1), average trade 70 (soglia 79), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 66% del netto (massimo 40%); average trade / soglia per anno: 2020:8.3 2021:3.6 2022:5.2 2023:-0.8 2024:-3.8 2025:-2.7 2026:6.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=15, SlowPeriod=50, Direction=1`

## RBM Reversal Bollinger mirrored

666 simulazioni in 33.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -7,925 |
| finestra: inizio | StartHour | -1 | 13 | ✔ | avg smussato 12.3, netto 6,040, 291 trade |
| finestra: fine | EndHour | -1 | 15 | ✔ | avg smussato 58.9, netto 9,619, 187 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 9,207 (migliore 9,207), 188 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 46 | ✔ | batte lo spento: netto 22,165 contro 10,652, net/DD 3.73 contro 0.95 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 403.0, netto 22,165, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| affinamento stop | StopAtr | 0.6 | 0.8 | ✔ | D2: netto smussato 23,595 (migliore 24,310), 55 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 3 candidati batte lo spento |
| durata | MaxBars | 4 | 4 | · | avg smussato 227.8, netto 24,743, 55 trade |

**Storia di ricerca**: 81 trade, netto 24,456, DD 5,942, average trade 302, UngerFit 4.51.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 81 su almeno 50 |
| average trade | passa | 302 contro la soglia 75 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 21.3 all'anno, 3 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -287 |
| ultimo terzo | **no** | netto -287, net/DD -0.09 (serve 1,3) |
| due terzi su tre | passa | 8,951 / 15,792 / -287 |
| outlier | passa | trade migliore 21% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -11 con i pattern, -212 senza |
| pattern casuali | passa | 1 estrazioni su 17 fanno almeno altrettanto, p 0.11, Wilson 0.05 |
| plateau | **no** | media 0.51 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 29 trade, netto 4,008, DD 3,300, net/DD 1.21, average trade 138 (soglia 79), UngerFit 2.71, finestre in utile 2/4 (-417 / 1,819 / -2,887 / 5,493).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.21 (serve 1), average trade 138 (soglia 79), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 41% del netto (massimo 40%); average trade / soglia per anno: 2020:-5.8 2021:5.1 2022:4.4 2023:5.7 2024:-1.2 2025:1.7 2026:2.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=13, EndHour=15, PtnNeutYes=46, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=30, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

966 simulazioni in 42.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=3.0 | ✔ | nessuna in utile: netto massimo -7,925 |
| finestra: inizio | StartHour | -1 | 13 | ✔ | avg smussato 12.3, netto 6,040, 291 trade |
| finestra: fine | EndHour | -1 | 15 | ✔ | avg smussato 58.9, netto 9,619, 187 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 9,207 (migliore 9,207), 188 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES long | FastYesLong | 152 | 142 | ✔ | batte lo spento: netto 15,897 contro 10,652, net/DD 1.71 contro 0.95 |
| YES short | FastYesShort | 152 | 88 | ✔ | batte lo spento: netto 24,822 contro 15,897, net/DD 6.98 contro 1.71 |
| NO long | FastNoLong | 153 | 153 | · | nessuno dei 3 candidati batte lo spento |
| NO short | FastNoShort | 153 | 72 | ✔ | batte lo spento: netto 26,825 contro 24,822, net/DD 7.55 contro 6.98 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 515.9, netto 26,825, 52 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.6 | 0.8 | · | D2: netto smussato 29,724 (migliore 31,173), 52 trade; annullato dall'ultimo terzo (-1,200 -> -1,683) |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 453.3, netto 21,823, 52 trade |

**Storia di ricerca**: 80 trade, netto 21,108, DD 7,084, average trade 264, UngerFit 3.61.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 80 su almeno 50 |
| average trade | passa | 264 contro la soglia 75 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 21.0 all'anno, 5 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -715 |
| ultimo terzo | **no** | netto -715, net/DD -0.10 (serve 1,3) |
| due terzi su tre | passa | 5,546 / 16,277 / -715 |
| outlier | passa | trade migliore 22% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -26 con i pattern, -197 senza |
| pattern casuali | passa | 13 estrazioni su 117 fanno almeno altrettanto, p 0.12, Wilson 0.09 |
| plateau | passa | media 0.78 su 7 vicini, minimo 0.18 |

**Prova su FTMO**: 35 trade, netto -2,436, DD 7,420, net/DD -0.33, average trade -70 (soglia 79), UngerFit , finestre in utile 2/4 (-2,872 / 271 / -1,524 / 1,689).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.33 (serve 1), average trade -70 (soglia 79), finestre 2/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 75% del netto (massimo 40%); average trade / soglia per anno: 2020:10.3 2021:2.1 2022:9.5 2023:0.6 2024:0.3 2025:-2.8 2026:2.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=13, EndHour=15, FastYesLong=142, FastYesShort=88, FastNoLong=153, FastNoShort=72, SkipDay=-1, BbLength=30, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

664 simulazioni in 24.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=5, Direction=1 | ✔ | nessuna in utile: netto massimo -3,851 |
| finestra: inizio | StartHour | -1 | 13 | ✔ | avg smussato -2.0, netto 9,173, 253 trade |
| finestra: fine | EndHour | -1 | 14 | ✔ | avg smussato 123.9, netto 21,694, 171 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 19,493 (migliore 19,493), 171 trade; annullato dall'ultimo terzo (-936 -> -2,669) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 42 | ✔ | batte lo spento: netto 23,344 contro 21,694, net/DD 9.41 contro 2.99 |
| NO | PtnNeutNo | 56 | 23 | ✔ | batte lo spento: netto 26,094 contro 23,344, net/DD 10.52 contro 9.41 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 1 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 466.0, netto 26,094, 56 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 24,721 (migliore 24,721), 56 trade; annullato dall'ultimo terzo (6,140 -> 4,862) |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 415.3, netto 23,182, 56 trade; annullato dall'ultimo terzo (6,140 -> 322) |

**Storia di ricerca**: 113 trade, netto 32,234, DD 7,132, average trade 285, UngerFit 3.89.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 113 su almeno 50 |
| average trade | passa | 285 contro la soglia 75 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 29.7 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 6,140 |
| ultimo terzo | **no** | netto 6,140, net/DD 0.86 (serve 1,3) |
| due terzi su tre | passa | 12,312 / 13,782 / 6,140 |
| outlier | **no** | trade migliore 9% del netto sulla storia, 41% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 108 con i pattern, -9 senza |
| pattern casuali | passa | 2 estrazioni su 7 fanno almeno altrettanto, p 0.38, Wilson 0.19 |
| plateau | passa | media 0.73 su 6 vicini, minimo 0.28 |

**Prova su FTMO**: 59 trade, netto -8,313, DD 15,606, net/DD -0.53, average trade -141 (soglia 79), UngerFit , finestre in utile 1/4 (-2,028 / -4,838 / -7,850 / 6,403).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.53 (serve 1), average trade -141 (soglia 79), finestre 1/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 44% del netto (massimo 40%); average trade / soglia per anno: 2020:5.6 2021:5.3 2022:6.1 2023:3.6 2024:1.8 2025:-4.8 2026:4.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=13, EndHour=14, PtnNeutYes=42, PtnNeutNo=23, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=5, Direction=1`

## LF Level fader sul pivot di ieri

507 simulazioni in 17.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=5 | ✔ | avg smussato 40.8, netto 11,271, 276 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 15,861 (migliore 15,861), 288 trade; annullato dall'ultimo terzo (-4,437 -> -11,693) |
| target | TargetAtr | 0 | 0 | · | nessuno dei 2 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 24 | ✔ | batte lo spento: netto 24,473 contro 11,271, net/DD 4.30 contro 0.66 |
| NO | PtnNeutNo | 56 | 23 | · | batte lo spento: netto 24,902 contro 24,473, net/DD 6.49 contro 4.30; annullato dall'ultimo terzo (5,607 -> 2,884) |
| direzionale YES | PtnDirYes | 52 | -3 | · | batte lo spento: netto 28,749 contro 24,473, net/DD 7.04 contro 4.30; annullato dall'ultimo terzo (5,607 -> 5,508) |
| calendario long | NotEntryDayLong | -1 | 4 | ✔ | batte lo spento: netto 24,239 contro 24,473, net/DD 4.65 contro 4.30 |
| calendario short | NotEntryDayShort | -1 | 0 | · | batte lo spento: netto 27,192 contro 24,239, net/DD 7.54 contro 4.65; annullato dall'ultimo terzo (5,783 -> 5,581) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 356.5, netto 24,239, 68 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 23,368 (migliore 24,194), 68 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 4 | · | avg smussato 221.0, netto 22,549, 68 trade |

**Storia di ricerca**: 108 trade, netto 28,331, DD 4,327, average trade 262, UngerFit 4.59.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 108 su almeno 50 |
| average trade | passa | 262 contro la soglia 75 |
| anni | passa | 5 anni, minimo 5 trade in un anno, 28.3 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 5,783 |
| ultimo terzo | passa | netto 5,783, net/DD 5.82 (serve 1,3) |
| due terzi su tre | passa | -435 / 22,984 / 5,783 |
| outlier | passa | trade migliore 14% del netto sulla storia, 29% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 145 con i pattern, -12 senza |
| pattern casuali | passa | 1 estrazioni su 35 fanno almeno altrettanto, p 0.06, Wilson 0.02 |
| plateau | passa | media 0.69 su 6 vicini, minimo 0.17 |

**Prova su FTMO**: 57 trade, netto -13,638, DD 19,201, net/DD -0.71, average trade -239 (soglia 79), UngerFit , finestre in utile 0/4 (-1,331 / -1,667 / -5,585 / -5,056).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.71 (serve 1), average trade -239 (soglia 79), finestre 0/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 118% del netto (massimo 40%); average trade / soglia per anno: 2020:2.6 2021:-1.3 2022:4.3 2023:4.4 2024:3.5 2025:-2.4 2026:-3.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=24, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=4, NotEntryDayShort=-1, LevelShift=5`

## LFHL Level fader sugli estremi di ieri

498 simulazioni in 19.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | nessuna in utile: netto massimo -20,847 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato -2,919 (migliore -2,919), 420 trade |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 3,664 contro 73, net/DD 0.18 contro 0.00 |
| YES | PtnNeutYes | 55 | 24 | ✔ | batte lo spento: netto 19,600 contro 3,664, net/DD 3.20 contro 0.18 |
| NO | PtnNeutNo | 56 | 42 | ✔ | batte lo spento: netto 20,711 contro 19,600, net/DD 9.04 contro 3.20 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 406.1, netto 20,711, 51 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 20,761 (migliore 20,761), 51 trade |
| affinamento target | TargetAtr | 1.0 | 1.0 | · | batte lo spento: netto 20,711 contro 19,815, net/DD 9.04 contro 8.65 |
| durata | MaxBars | 4 | 4 | · | avg smussato 318.2, netto 20,711, 51 trade |

**Storia di ricerca**: 75 trade, netto 19,631, DD 2,290, average trade 262, UngerFit 6.30.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 75 su almeno 50 |
| average trade | passa | 262 contro la soglia 75 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 19.7 all'anno, 4 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -1,080 |
| ultimo terzo | **no** | netto -1,080, net/DD -0.50 (serve 1,3) |
| due terzi su tre | **no** | -139 / 20,850 / -1,080 |
| outlier | passa | trade migliore 19% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -45 con i pattern, -59 senza |
| pattern casuali | **no** | 23 estrazioni su 64 fanno almeno altrettanto, p 0.37, Wilson 0.30 |
| plateau | passa | media 0.71 su 7 vicini, minimo 0.28 |

**Prova su FTMO**: 36 trade, netto 2,793, DD 6,564, net/DD 0.43, average trade 78 (soglia 79), UngerFit 1.08, finestre in utile 3/4 (2,467 / 1,378 / -3,554 / 2,502).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.43 (serve 1), average trade 78 (soglia 79), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 79% del netto (massimo 40%); average trade / soglia per anno: 2020:5.3 2021:-0.3 2022:3.8 2023:0.3 2024:4.2 2025:1.2 2026:-0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=1.0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=24, PtnNeutNo=42, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=10`

## Riepilogo

- PCH: scartata; ricerca 107 trade netto 35,513 avg 332; prova 60 trade netto -1,331 net/DD -0.14 avg -22 fin 2/4; falliti: anni, ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- TFM: scartata; ricerca 92 trade netto -2,921 avg -32; prova 54 trade netto -4,321 net/DD -0.67 avg -80 fin 1/4; falliti: average trade, anni, utile recente, ultimo terzo, due terzi su tre, outlier, pattern utile, pattern casuali, prova sul broker, anni
- TFU: abbandonato (R9: la base non guadagna su tutta la storia (526 trade, netto -47,755))
- BO: abbandonato (R9: la base non guadagna su tutta la storia (81 trade, netto -19,199))
- BOS: abbandonato (R9: la base non guadagna su tutta la storia (72 trade, netto -564))
- VBO: scartata; ricerca 72 trade netto 21,542 avg 299; prova 51 trade netto 8,137 net/DD 0.65 avg 160 fin 1/4; falliti: anni, utile recente, ultimo terzo, due terzi su tre, pattern utile, pattern casuali, prova sul broker, anni
- MAC: scartata; ricerca 255 trade netto 57,926 avg 227; prova 136 trade netto 9,548 net/DD 0.29 avg 70 fin 2/4; falliti: utile recente, ultimo terzo, plateau, prova sul broker, anni
- RBM: scartata; ricerca 81 trade netto 24,456 avg 302; prova 29 trade netto 4,008 net/DD 1.21 avg 138 fin 2/4; falliti: anni, utile recente, ultimo terzo, plateau, prova sul broker, anni
- RBU: scartata; ricerca 80 trade netto 21,108 avg 264; prova 35 trade netto -2,436 net/DD -0.33 avg -70 fin 2/4; falliti: anni, utile recente, ultimo terzo, prova sul broker, anni
- RHL: scartata; ricerca 113 trade netto 32,234 avg 285; prova 59 trade netto -8,313 net/DD -0.53 avg -141 fin 1/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- LF: scartata; ricerca 108 trade netto 28,331 avg 262; prova 57 trade netto -13,638 net/DD -0.71 avg -239 fin 0/4; falliti: prova sul broker, anni
- LFHL: scartata; ricerca 75 trade netto 19,631 avg 262; prova 36 trade netto 2,793 net/DD 0.43 avg 78 fin 3/4; falliti: anni, utile recente, ultimo terzo, due terzi su tre, pattern casuali, prova sul broker, anni
