# Percorso v4 — @NQ 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (3,553 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,206 barre), mai vista dal percorso
- costi FTMO: spread 1.45 punti (mediana), swap long 6.7098 short 0 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 301 nella ricerca, 483 nella prova

## PCH Price Channel

755 simulazioni in 6.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=1, OffsetTicks=2, Direction=0 | ✔ | avg smussato 318.1, netto 85,258, 268 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 318.1, netto 85,258, 268 trade |
| finestra: fine | EndHour | -1 | 5 | ✔ | avg smussato 402.8, netto 101,093, 251 trade |
| stop | StopAtr | 0.8 | 2.0 | · | D2: netto smussato 115,373 (migliore 120,255), 251 trade; annullato dall'ultimo terzo (81,026 -> 66,320) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 402.8, netto 101,093, 251 trade |
| YES | PtnNeutYes | 55 | 12 | · | batte lo spento: netto 107,619 contro 101,093, net/DD 5.13 contro 3.28; annullato dall'ultimo terzo (81,026 -> 9,402) |
| NO | PtnNeutNo | 56 | 18 | · | batte lo spento: netto 107,619 contro 101,093, net/DD 5.13 contro 3.28; annullato dall'ultimo terzo (81,026 -> 9,402) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 107,546 contro 101,093, net/DD 5.76 contro 3.28; annullato dall'ultimo terzo (81,026 -> 11,693) |
| direzionale NO | PtnDirNo | 53 | 3 | ✔ | batte lo spento: netto 127,284 contro 101,093, net/DD 4.60 contro 3.28 |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 155,604 contro 127,284, net/DD 8.77 contro 4.60; annullato dall'ultimo terzo (97,645 -> 14,079) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 127,297 (migliore 127,304), 221 trade; annullato dall'ultimo terzo (97,645 -> 86,705) |

**Storia di ricerca**: 520 trade, netto 224,928, DD 37,266, average trade 433, UngerFit 1.29.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 520 su almeno 50 |
| average trade | passa | 433 contro la soglia 301 |
| anni | passa | 3 anni, minimo 130 trade in un anno, 136.4 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 97,645 |
| ultimo terzo | passa | netto 97,645, net/DD 2.62 (serve 1,3) |
| due terzi su tre | passa | 0 / 127,284 / 97,645 |
| outlier | passa | trade migliore 6% del netto sulla storia, 12% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 327 con i pattern, 247 senza |
| pattern casuali | passa | 0 estrazioni su 3 fanno almeno altrettanto, p 0.25, Wilson 0.07 |
| plateau | passa | media 0.95 su 5 vicini, minimo 0.83 |

**Prova su FTMO**: 419 trade, netto -9,139, DD 80,197, net/DD -0.11, average trade -22 (soglia 483), UngerFit , finestre in utile 2/4 (4,881 / -10,690 / -21,176 / 24,511).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.11 (serve 1), average trade -22 (soglia 483), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 54% del netto (massimo 40%); average trade / soglia per anno: 2022:2.5 2023:0.8 2024:0.8 2025:0.3 2026:-0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=5, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=3, SkipDay=-1, ChannelBars=1, OffsetTicks=2, Direction=0`

## TFM Trend following mirrored

433 simulazioni in 7.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 93.0, netto 24,184, 260 trade |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 277.6, netto 59,681, 215 trade; annullato dall'ultimo terzo (109,037 -> 51,778) |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 27,313 (migliore 27,313), 260 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 95.1, netto 26,063, 274 trade; annullato dall'ultimo terzo (109,037 -> 91,436) |
| YES | PtnNeutYes | 55 | 40 | · | batte lo spento: netto 90,210 contro 24,184, net/DD 3.60 contro 0.74; annullato dall'ultimo terzo (109,037 -> 57,541) |
| NO | PtnNeutNo | 56 | 33 | · | batte lo spento: netto 90,210 contro 24,184, net/DD 3.60 contro 0.74; annullato dall'ultimo terzo (109,037 -> 57,541) |
| direzionale YES | PtnDirYes | 52 | 18 | · | batte lo spento: netto 48,894 contro 24,184, net/DD 7.34 contro 0.74; annullato dall'ultimo terzo (109,037 -> 8,176) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 88,580 contro 24,184, net/DD 4.38 contro 0.74; annullato dall'ultimo terzo (109,037 -> 13,764) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 54,610 contro 24,184, net/DD 1.70 contro 0.74; annullato dall'ultimo terzo (109,037 -> 76,899) |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 41,724 contro 24,184, net/DD 1.94 contro 0.74; annullato dall'ultimo terzo (109,037 -> 69,717) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 51,541 contro 24,184, net/DD 1.59 contro 0.74; annullato dall'ultimo terzo (109,037 -> 52,958) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 27,313 (migliore 27,313), 260 trade |

**Storia di ricerca**: 591 trade, netto 134,189, DD 32,864, average trade 227, UngerFit 0.72.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 591 su almeno 50 |
| average trade | **no** | 227 contro la soglia 301 |
| anni | passa | 3 anni, minimo 159 trade in un anno, 155.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 110,005 |
| ultimo terzo | passa | netto 109,037, net/DD 4.11 (serve 1,3) |
| due terzi su tre | passa | 0 / 24,184 / 109,037 |
| outlier | passa | trade migliore 8% del netto sulla storia, 10% sull'ultimo terzo |
| plateau | passa | media 1.00 su 2 vicini, minimo 1.00 |

**Prova su FTMO**: 538 trade, netto 17,214, DD 100,586, net/DD 0.17, average trade 32 (soglia 483), UngerFit 0.05, finestre in utile 2/4 (40,760 / -21,989 / -43,587 / 44,982).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.17 (serve 1), average trade 32 (soglia 483), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 81% del netto (massimo 40%); average trade / soglia per anno: 2022:0.4 2023:0.6 2024:1.4 2025:-0.3 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

727 simulazioni in 10.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 93.0, netto 24,184, 260 trade |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 277.6, netto 59,681, 215 trade; annullato dall'ultimo terzo (109,037 -> 51,778) |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 27,313 (migliore 27,313), 260 trade |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 95.1, netto 26,063, 274 trade; annullato dall'ultimo terzo (109,037 -> 91,436) |
| YES long | FastYesLong | 152 | 25 | · | batte lo spento: netto 63,907 contro 24,184, net/DD 2.08 contro 0.74; annullato dall'ultimo terzo (109,037 -> 70,180) |
| YES short | FastYesShort | 152 | 144 | · | batte lo spento: netto 70,209 contro 24,184, net/DD 2.01 contro 0.74; annullato dall'ultimo terzo (109,037 -> 54,171) |
| NO long | FastNoLong | 153 | 29 | · | batte lo spento: netto 63,907 contro 24,184, net/DD 2.08 contro 0.74; annullato dall'ultimo terzo (109,037 -> 70,180) |
| NO short | FastNoShort | 153 | 149 | · | batte lo spento: netto 70,209 contro 24,184, net/DD 2.01 contro 0.74; annullato dall'ultimo terzo (109,037 -> 54,171) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 54,610 contro 24,184, net/DD 1.70 contro 0.74; annullato dall'ultimo terzo (109,037 -> 76,899) |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 41,724 contro 24,184, net/DD 1.94 contro 0.74; annullato dall'ultimo terzo (109,037 -> 69,717) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 51,541 contro 24,184, net/DD 1.59 contro 0.74; annullato dall'ultimo terzo (109,037 -> 52,958) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 27,313 (migliore 27,313), 260 trade |

**Storia di ricerca**: 591 trade, netto 134,189, DD 32,864, average trade 227, UngerFit 0.72.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 591 su almeno 50 |
| average trade | **no** | 227 contro la soglia 301 |
| anni | passa | 3 anni, minimo 159 trade in un anno, 155.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 110,005 |
| ultimo terzo | passa | netto 109,037, net/DD 4.11 (serve 1,3) |
| due terzi su tre | passa | 0 / 24,184 / 109,037 |
| outlier | passa | trade migliore 8% del netto sulla storia, 10% sull'ultimo terzo |
| plateau | passa | media 1.00 su 2 vicini, minimo 1.00 |

**Prova su FTMO**: 538 trade, netto 17,214, DD 100,586, net/DD 0.17, average trade 32 (soglia 483), UngerFit 0.05, finestre in utile 2/4 (40,760 / -21,989 / -43,587 / 44,982).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.17 (serve 1), average trade 32 (soglia 483), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 81% del netto (massimo 40%); average trade / soglia per anno: 2022:0.4 2023:0.6 2024:1.4 2025:-0.3 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1`

## BO Breakout di N sessioni

503 simulazioni in 7.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=0, IncludeCurrentSession=1 | ✔ | avg smussato 198.2, netto 48,372, 244 trade |
| finestra: inizio | StartHour | -1 | 2 | · | avg smussato 222.4, netto 53,155, 239 trade; annullato dall'ultimo terzo (76,914 -> 66,836) |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 264.7, netto 56,638, 214 trade; annullato dall'ultimo terzo (76,914 -> 51,945) |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 59,792 (migliore 61,452), 253 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 258.0, netto 65,272, 253 trade |
| YES | PtnNeutYes | 55 | 40 | · | batte lo spento: netto 85,429 contro 65,272, net/DD 3.66 contro 2.34; annullato dall'ultimo terzo (79,901 -> 60,997) |
| NO | PtnNeutNo | 56 | 33 | · | batte lo spento: netto 85,429 contro 65,272, net/DD 3.66 contro 2.34; annullato dall'ultimo terzo (79,901 -> 60,997) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 84,796 contro 65,272, net/DD 6.82 contro 2.34; annullato dall'ultimo terzo (79,901 -> 48,159) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 93,791 contro 65,272, net/DD 5.18 contro 2.34; annullato dall'ultimo terzo (79,901 -> 41,915) |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 65,944 contro 65,272, net/DD 2.79 contro 2.34; annullato dall'ultimo terzo (79,901 -> 25,034) |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 59,792 (migliore 61,452), 253 trade |

**Storia di ricerca**: 576 trade, netto 145,173, DD 27,894, average trade 252, UngerFit 0.87.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 576 su almeno 50 |
| average trade | **no** | 252 contro la soglia 301 |
| anni | passa | 3 anni, minimo 152 trade in un anno, 151.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 79,901 |
| ultimo terzo | passa | netto 79,901, net/DD 4.35 (serve 1,3) |
| due terzi su tre | passa | 0 / 65,272 / 79,901 |
| outlier | passa | trade migliore 7% del netto sulla storia, 13% sull'ultimo terzo |
| plateau | passa | media 0.91 su 4 vicini, minimo 0.67 |

**Prova su FTMO**: 504 trade, netto 87,377, DD 79,123, net/DD 1.10, average trade 173 (soglia 483), UngerFit 0.28, finestre in utile 2/4 (55,990 / -16,585 / -35,586 / 88,556).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.10 (serve 1), average trade 173 (soglia 483), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 43% del netto (massimo 40%); average trade / soglia per anno: 2022:0.9 2023:0.7 2024:1.2 2025:-0.2 2026:0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=1, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

639 simulazioni in 6.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=10 | ✔ | avg smussato 295.4, netto 80,041, 271 trade |
| finestra: inizio | StartHour | -1 | 6 | · | avg smussato 350.2, netto 91,059, 260 trade; annullato dall'ultimo terzo (48,645 -> 41,912) |
| finestra: fine | EndHour | -1 | 1 | ✔ | avg smussato 430.1, netto 67,096, 156 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 59,272 (migliore 60,879), 156 trade; annullato dall'ultimo terzo (83,468 -> 78,847) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 430.1, netto 67,096, 156 trade |
| YES | PtnNeutYes | 55 | 12 | · | batte lo spento: netto 59,091 contro 67,096, net/DD 2.70 contro 2.19; annullato dall'ultimo terzo (83,468 -> 27,565) |
| NO | PtnNeutNo | 56 | 6 | ✔ | batte lo spento: netto 59,586 contro 67,096, net/DD 3.52 contro 2.19 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 36 | · | batte lo spento: netto 58,909 contro 59,586, net/DD 4.29 contro 3.52; annullato dall'ultimo terzo (93,634 -> 57,436) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 61,592 contro 59,586, net/DD 3.97 contro 3.52; annullato dall'ultimo terzo (93,634 -> 33,224) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 53,956 (migliore 53,956), 84 trade |

**Storia di ricerca**: 205 trade, netto 148,137, DD 24,344, average trade 723, UngerFit 2.67.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 205 su almeno 50 |
| average trade | passa | 723 contro la soglia 301 |
| anni | passa | 3 anni, minimo 50 trade in un anno, 53.8 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 98,581 |
| ultimo terzo | passa | netto 98,581, net/DD 4.05 (serve 1,3) |
| due terzi su tre | passa | 0 / 49,556 / 98,581 |
| outlier | passa | trade migliore 9% del netto sulla storia, 12% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 815 con i pattern, 352 senza |
| pattern casuali | passa | 0 estrazioni su 4 fanno almeno altrettanto, p 0.20, Wilson 0.05 |
| plateau | passa | media 0.95 su 4 vicini, minimo 0.91 |

**Prova su FTMO**: 208 trade, netto 93,607, DD 63,353, net/DD 1.48, average trade 450 (soglia 483), UngerFit 0.81, finestre in utile 3/4 (15,435 / 10,946 / -1,409 / 74,668).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.48 (serve 1), average trade 450 (soglia 483), finestre 3/4 (servono 3) |
| anni | passa | 4/5 anni in utile (serve 60%), anno migliore 35% del netto (massimo 40%); average trade / soglia per anno: 2022:2.4 2023:2.1 2024:2.2 2025:-0.1 2026:1.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=6, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=10`

## VBO Volatility breakout

455 simulazioni in 9.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.0, Direction=0 | ✔ | avg smussato 176.0, netto 14,082, 80 trade |
| finestra: inizio | StartHour | -1 | 16 | · | avg smussato 261.9, netto 14,152, 66 trade; annullato dall'ultimo terzo (46,068 -> 22,132) |
| finestra: fine | EndHour | -1 | 13 | · | avg smussato 311.2, netto 18,050, 58 trade; annullato dall'ultimo terzo (46,068 -> 35,476) |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 12,639 (migliore 12,639), 80 trade; annullato dall'ultimo terzo (46,068 -> 24,869) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 206.3, netto 16,503, 80 trade |
| YES | PtnNeutYes | 55 | 5 | · | batte lo spento: netto 32,139 contro 16,503, net/DD 3.44 contro 1.49; annullato dall'ultimo terzo (47,463 -> 24,265) |
| NO | PtnNeutNo | 56 | 2 | · | batte lo spento: netto 32,139 contro 16,503, net/DD 3.44 contro 1.49; annullato dall'ultimo terzo (47,463 -> 24,265) |
| direzionale YES | PtnDirYes | 52 | 7 | · | batte lo spento: netto 32,490 contro 16,503, net/DD 3.22 contro 1.49; annullato dall'ultimo terzo (47,463 -> 21,652) |
| direzionale NO | PtnDirNo | 53 | 36 | · | batte lo spento: netto 46,149 contro 16,503, net/DD 6.55 contro 1.49; annullato dall'ultimo terzo (47,463 -> 33,984) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 28,615 contro 16,503, net/DD 2.66 contro 1.49 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 24,860 (migliore 24,860), 73 trade; annullato dall'ultimo terzo (50,629 -> 49,380) |

**Storia di ricerca**: 172 trade, netto 79,244, DD 14,569, average trade 461, UngerFit 2.20.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 172 su almeno 50 |
| average trade | passa | 461 contro la soglia 301 |
| anni | passa | 3 anni, minimo 45 trade in un anno, 45.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 50,629 |
| ultimo terzo | passa | netto 50,629, net/DD 3.48 (serve 1,3) |
| due terzi su tre | passa | 0 / 28,615 / 50,629 |
| outlier | passa | trade migliore 13% del netto sulla storia, 14% sull'ultimo terzo |
| plateau | passa | media 0.71 su 4 vicini, minimo 0.47 |

**Prova su FTMO**: 137 trade, netto 26,100, DD 86,729, net/DD 0.30, average trade 191 (soglia 483), UngerFit 0.29, finestre in utile 2/4 (31,359 / -3,861 / -40,624 / 39,226).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.30 (serve 1), average trade 191 (soglia 483), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 74% del netto (massimo 40%); average trade / soglia per anno: 2022:1.2 2023:1.1 2024:2.8 2025:-0.8 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=5, IntradayOnly=0, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0, AtrMultiplierLong=1.0, Direction=0`

## MAC Incrocio di medie

126 simulazioni in 1.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=5, SlowPeriod=30, Direction=0 | ✔ | avg smussato 189.6, netto 13,270, 70 trade |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 17,356 (migliore 17,371), 70 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 288.7, netto 20,211, 70 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 26,768 contro 20,211, net/DD 1.10 contro 0.83 |
| affinamento stop | StopAtr | 0.5 | 0.5 | · | D2: netto smussato 23,913 (migliore 23,928), 70 trade |

**Storia di ricerca**: 154 trade, netto 39,438, DD 27,794, average trade 256, UngerFit 0.89.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 154 su almeno 50 |
| average trade | **no** | 256 contro la soglia 301 |
| anni | passa | 3 anni, minimo 39 trade in un anno, 40.4 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 12,670 |
| ultimo terzo | **no** | netto 12,670, net/DD 0.46 (serve 1,3) |
| due terzi su tre | passa | 0 / 26,768 / 12,670 |
| outlier | **no** | trade migliore 25% del netto sulla storia, 58% sull'ultimo terzo |
| plateau | **no** | media 0.54 su 6 vicini, minimo 0.00 |

**Prova su FTMO**: 118 trade, netto -5,652, DD 36,474, net/DD -0.15, average trade -48 (soglia 483), UngerFit , finestre in utile 2/4 (19,862 / -6,462 / -19,060 / 7).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.15 (serve 1), average trade -48 (soglia 483), finestre 2/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 142% del netto (massimo 40%); average trade / soglia per anno: 2022:1.1 2023:-0.6 2024:2.1 2025:-0.8 2026:0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=5, SlowPeriod=30, Direction=0`

## RBM Reversal Bollinger mirrored

670 simulazioni in 6.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 154.5, netto 25,346, 164 trade |
| finestra: inizio | StartHour | -1 | 6 | ✔ | avg smussato 185.7, netto 27,853, 150 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 607.9, netto 49,845, 82 trade |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 65,848 (migliore 65,848), 82 trade; annullato dall'ultimo terzo (1,857 -> -3,964) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 28 | ✔ | batte lo spento: netto 62,159 contro 49,845, net/DD 2.44 contro 1.70 |
| NO | PtnNeutNo | 56 | 38 | · | batte lo spento: netto 68,886 contro 62,159, net/DD 3.60 contro 2.44; annullato dall'ultimo terzo (5,768 -> -10,956) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | -12 | ✔ | batte lo spento: netto 79,057 contro 62,159, net/DD 6.38 contro 2.44 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 1,437.4, netto 79,057, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 93,458 (migliore 93,458), 55 trade; annullato dall'ultimo terzo (12,968 -> 7,921) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 1,437.4, netto 79,057, 55 trade |

**Storia di ricerca**: 123 trade, netto 92,025, DD 20,544, average trade 748, UngerFit 3.01.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 123 su almeno 50 |
| average trade | passa | 748 contro la soglia 301 |
| anni | passa | 3 anni, minimo 34 trade in un anno, 32.3 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 12,968 |
| ultimo terzo | **no** | netto 12,968, net/DD 0.74 (serve 1,3) |
| due terzi su tre | passa | 0 / 79,057 / 12,968 |
| outlier | **no** | trade migliore 16% del netto sulla storia, 60% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 191 con i pattern, 18 senza |
| pattern casuali | **no** | 3 estrazioni su 4 fanno almeno altrettanto, p 0.80, Wilson 0.48 |
| plateau | passa | media 0.68 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 102 trade, netto -40,242, DD 70,712, net/DD -0.57, average trade -395 (soglia 483), UngerFit , finestre in utile 2/4 (15,983 / -27,328 / 5,017 / -33,915).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.57 (serve 1), average trade -395 (soglia 483), finestre 2/4 (servono 3) |
| anni | **no** | 2/5 anni in utile (serve 60%), anno migliore 148% del netto (massimo 40%); average trade / soglia per anno: 2022:6.1 2023:-0.4 2024:1.6 2025:-0.7 2026:-1.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=6, EndHour=9, PtnNeutYes=28, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=-12, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RBU Reversal Bollinger unmirrored

966 simulazioni in 9.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 154.5, netto 25,346, 164 trade |
| finestra: inizio | StartHour | -1 | 6 | ✔ | avg smussato 185.7, netto 27,853, 150 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 607.9, netto 49,845, 82 trade |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 65,848 (migliore 65,848), 82 trade; annullato dall'ultimo terzo (1,857 -> -3,964) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 48 | · | batte lo spento: netto 48,512 contro 49,845, net/DD 2.70 contro 1.70; annullato dall'ultimo terzo (1,857 -> -18,957) |
| YES short | FastYesShort | 152 | 3 | ✔ | batte lo spento: netto 69,647 contro 49,845, net/DD 5.43 contro 1.70 |
| NO long | FastNoLong | 153 | 101 | · | batte lo spento: netto 86,608 contro 69,647, net/DD 9.70 contro 5.43; annullato dall'ultimo terzo (7,786 -> -11,549) |
| NO short | FastNoShort | 153 | 52 | ✔ | batte lo spento: netto 78,727 contro 69,647, net/DD 6.14 contro 5.43 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 1,431.4, netto 78,727, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 87,609 (migliore 92,050), 55 trade; annullato dall'ultimo terzo (7,924 -> 3,633) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 1,431.4, netto 78,727, 55 trade |

**Storia di ricerca**: 127 trade, netto 86,651, DD 23,301, average trade 682, UngerFit 2.58.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 127 su almeno 50 |
| average trade | passa | 682 contro la soglia 301 |
| anni | passa | 3 anni, minimo 33 trade in un anno, 33.3 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 7,924 |
| ultimo terzo | **no** | netto 7,924, net/DD 0.36 (serve 1,3) |
| due terzi su tre | passa | 0 / 78,727 / 7,924 |
| outlier | **no** | trade migliore 17% del netto sulla storia, 120% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 110 con i pattern, 18 senza |
| pattern casuali | **no** | 32 estrazioni su 57 fanno almeno altrettanto, p 0.57, Wilson 0.48 |
| plateau | passa | media 0.61 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 105 trade, netto -87,903, DD 92,588, net/DD -0.95, average trade -837 (soglia 483), UngerFit , finestre in utile 0/4 (-5,141 / -48,945 / -1,176 / -32,640).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.95 (serve 1), average trade -837 (soglia 483), finestre 0/4 (servono 3) |
| anni | **no** | 2/5 anni in utile (serve 60%), anno migliore 100% del netto (massimo 40%); average trade / soglia per anno: 2022:5.3 2023:0.6 2024:-0.2 2025:-1.7 2026:-1.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=6, EndHour=9, FastYesLong=152, FastYesShort=3, FastNoLong=153, FastNoShort=52, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RHL Reversal sui livelli di ieri

662 simulazioni in 6.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=2 | ✔ | nessuna in utile: netto massimo -9,576 |
| finestra: inizio | StartHour | -1 | 13 | · | nessuna in utile: netto massimo -2,484; annullato dall'ultimo terzo (-27,253 -> -44,447) |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 40.9, netto 4,169, 102 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 494 (migliore 494), 102 trade |
| target | TargetAtr | 0 | 0.75 | · | batte lo spento: netto 25,439 contro 4,169, net/DD 0.99 contro 0.12; annullato dall'ultimo terzo (-23,326 -> -33,476) |
| YES | PtnNeutYes | 55 | 29 | ✔ | batte lo spento: netto 25,346 contro 4,169, net/DD 1.30 contro 0.12 |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 1 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 478.2, netto 25,346, 53 trade |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 30,377 contro 25,346, net/DD 2.71 contro 1.30; annullato dall'ultimo terzo (-9,065 -> -12,757) |
| affinamento stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 22,839 (migliore 22,839), 53 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 2 | ✔ | avg smussato 507.1, netto 31,074, 53 trade |

**Storia di ricerca**: 122 trade, netto 31,960, DD 26,796, average trade 262, UngerFit 0.92.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 122 su almeno 50 |
| average trade | **no** | 262 contro la soglia 301 |
| anni | passa | 3 anni, minimo 30 trade in un anno, 32.0 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 886 |
| ultimo terzo | **no** | netto 886, net/DD 0.03 (serve 1,3) |
| due terzi su tre | passa | 0 / 31,074 / 886 |
| outlier | **no** | trade migliore 32% del netto sulla storia, 1,149% sull'ultimo terzo |
| pattern utile | **no** | ultimo terzo: average trade 13 con i pattern, 112 senza |
| pattern casuali | **no** | 8 estrazioni su 10 fanno almeno altrettanto, p 0.82, Wilson 0.62 |
| plateau | passa | media 0.83 su 4 vicini, minimo 0.39 |

**Prova su FTMO**: 102 trade, netto -27,168, DD 53,634, net/DD -0.51, average trade -266 (soglia 483), UngerFit , finestre in utile 0/4 (-5,305 / -718 / -2,784 / -18,360).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.51 (serve 1), average trade -266 (soglia 483), finestre 0/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 546% del netto (massimo 40%); average trade / soglia per anno: 2022:2.4 2023:0.3 2024:0.2 2025:-0.4 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=2, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=9, PtnNeutYes=29, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=20, Direction=2`

## LF Level fader sul pivot di ieri

240 simulazioni in 2.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | nessuna in utile: netto massimo -12,469 |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato -4,712 (migliore -4,712), 54 trade; annullato dall'ultimo terzo (433 -> -250) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessun valore sopra lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessun valore sopra lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |

**Abbandonato**: R9: la base non guadagna su tutta la storia (109 trade, netto -12,036).

## LFHL Level fader sugli estremi di ieri

248 simulazioni in 2.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | nessuna in utile: netto massimo -3,119 |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 4,529 (migliore 4,529), 93 trade |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 13,780 contro 2,038, net/DD 0.70 contro 0.09; annullato dall'ultimo terzo (-6,695 -> -18,001) |
| YES | PtnNeutYes | 55 | 53 | · | batte lo spento: netto 29,255 contro 2,038, net/DD 3.77 contro 0.09; annullato dall'ultimo terzo (-6,695 -> -24,344) |
| NO | PtnNeutNo | 56 | 54 | · | batte lo spento: netto 29,255 contro 2,038, net/DD 3.77 contro 0.09; annullato dall'ultimo terzo (-6,695 -> -24,344) |
| direzionale YES | PtnDirYes | 52 | -2 | · | batte lo spento: netto 14,558 contro 2,038, net/DD 1.67 contro 0.09; annullato dall'ultimo terzo (-6,695 -> -20,462) |

**Abbandonato**: R9: la base non guadagna su tutta la storia (192 trade, netto -4,657).

## Riepilogo

- PCH: scartata; ricerca 520 trade netto 224,928 avg 433; prova 419 trade netto -9,139 net/DD -0.11 avg -22 fin 2/4; falliti: prova sul broker, anni
- TFM: scartata; ricerca 591 trade netto 134,189 avg 227; prova 538 trade netto 17,214 net/DD 0.17 avg 32 fin 2/4; falliti: average trade, prova sul broker, anni
- TFU: scartata; ricerca 591 trade netto 134,189 avg 227; prova 538 trade netto 17,214 net/DD 0.17 avg 32 fin 2/4; falliti: average trade, prova sul broker, anni
- BO: scartata; ricerca 576 trade netto 145,173 avg 252; prova 504 trade netto 87,377 net/DD 1.10 avg 173 fin 2/4; falliti: average trade, prova sul broker, anni
- BOS: scartata; ricerca 205 trade netto 148,137 avg 723; prova 208 trade netto 93,607 net/DD 1.48 avg 450 fin 3/4; falliti: prova sul broker
- VBO: scartata; ricerca 172 trade netto 79,244 avg 461; prova 137 trade netto 26,100 net/DD 0.30 avg 191 fin 2/4; falliti: prova sul broker, anni
- MAC: scartata; ricerca 154 trade netto 39,438 avg 256; prova 118 trade netto -5,652 net/DD -0.15 avg -48 fin 2/4; falliti: average trade, ultimo terzo, outlier, plateau, prova sul broker, anni
- RBM: scartata; ricerca 123 trade netto 92,025 avg 748; prova 102 trade netto -40,242 net/DD -0.57 avg -395 fin 2/4; falliti: ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- RBU: scartata; ricerca 127 trade netto 86,651 avg 682; prova 105 trade netto -87,903 net/DD -0.95 avg -837 fin 0/4; falliti: ultimo terzo, outlier, pattern casuali, prova sul broker, anni
- RHL: scartata; ricerca 122 trade netto 31,960 avg 262; prova 102 trade netto -27,168 net/DD -0.51 avg -266 fin 0/4; falliti: average trade, ultimo terzo, outlier, pattern utile, pattern casuali, prova sul broker, anni
- LF: abbandonato (R9: la base non guadagna su tutta la storia (109 trade, netto -12,036))
- LFHL: abbandonato (R9: la base non guadagna su tutta la storia (192 trade, netto -4,657))
