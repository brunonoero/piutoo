# Percorso v4 — @NQ 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (13,595 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,260 barre), mai vista dal percorso
- costi FTMO: spread 1.45 punti (mediana), swap long 6.7098 short 0 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 145 nella ricerca, 234 nella prova

## PCH Price Channel

560 simulazioni in 11.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=40, OffsetTicks=0, Direction=0 | ✔ | avg smussato 268.5, netto 61,217, 228 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 2,052.6, netto 61,217, 228 trade |
| finestra: fine | EndHour | -1 | 17 | · | avg smussato 346.9, netto 68,016, 197 trade; annullato dall'ultimo terzo (63,657 -> 60,261) |
| stop | StopAtr | 0.8 | 1.25 | · | D2: netto smussato 72,883 (migliore 72,883), 215 trade; annullato dall'ultimo terzo (63,657 -> 44,526) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 268.5, netto 61,217, 228 trade |
| YES | PtnNeutYes | 55 | 5 | · | batte lo spento: netto 79,560 contro 61,217, net/DD 3.06 contro 1.48; annullato dall'ultimo terzo (63,657 -> 32,169) |
| NO | PtnNeutNo | 56 | 2 | · | batte lo spento: netto 79,560 contro 61,217, net/DD 3.06 contro 1.48; annullato dall'ultimo terzo (63,657 -> 32,169) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 72,705 contro 61,217, net/DD 9.93 contro 1.48; annullato dall'ultimo terzo (63,657 -> 21,896) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 65,011 contro 61,217, net/DD 4.29 contro 1.48; annullato dall'ultimo terzo (63,657 -> 6,888) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 72,949 contro 61,217, net/DD 2.80 contro 1.48; annullato dall'ultimo terzo (63,657 -> 12,060) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 66,005 contro 61,217, net/DD 1.60 contro 1.48 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 70,754 (migliore 73,128), 219 trade; annullato dall'ultimo terzo (72,974 -> 65,494) |

**Storia di ricerca**: 529 trade, netto 138,979, DD 41,232, average trade 263, UngerFit 1.08.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 529 su almeno 50 |
| average trade | passa | 263 contro la soglia 145 |
| anni | passa | 3 anni, minimo 136 trade in un anno, 138.8 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 72,974 |
| ultimo terzo | passa | netto 72,974, net/DD 3.16 (serve 1,3) |
| due terzi su tre | passa | 0 / 66,005 / 72,974 |
| outlier | passa | trade migliore 10% del netto sulla storia, 20% sull'ultimo terzo |
| plateau | passa | media 0.94 su 7 vicini, minimo 0.87 |

**Prova su FTMO**: 457 trade, netto 8,357, DD 123,586, net/DD 0.07, average trade 18 (soglia 234), UngerFit 0.03, finestre in utile 2/4 (61,017 / -4,105 / -66,896 / 25,006).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.07 (serve 1), average trade 18 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 70% del netto (massimo 40%); average trade / soglia per anno: 2022:2.3 2023:0.7 2024:2.8 2025:-0.3 2026:-0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=40, OffsetTicks=0, Direction=0`

## TFM Trend following mirrored

431 simulazioni in 9.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 421.8, netto 69,495, 244 trade |
| finestra: fine | EndHour | -1 | 23 | ✔ | avg smussato 384.4, netto 69,495, 244 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 69,551 (migliore 71,073), 244 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 284.8, netto 69,495, 244 trade |
| YES | PtnNeutYes | 55 | 5 | · | batte lo spento: netto 78,360 contro 69,495, net/DD 2.69 contro 1.34; annullato dall'ultimo terzo (68,651 -> 31,202) |
| NO | PtnNeutNo | 56 | 2 | · | batte lo spento: netto 78,360 contro 69,495, net/DD 2.69 contro 1.34; annullato dall'ultimo terzo (68,651 -> 31,202) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 70,077 contro 69,495, net/DD 7.40 contro 1.34; annullato dall'ultimo terzo (68,651 -> 34,280) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 64,531 contro 69,495, net/DD 2.84 contro 1.34; annullato dall'ultimo terzo (68,651 -> 4,962) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 80,567 contro 69,495, net/DD 3.06 contro 1.34; annullato dall'ultimo terzo (68,651 -> 12,542) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 74,220 contro 69,495, net/DD 1.76 contro 1.34; annullato dall'ultimo terzo (68,651 -> 22,803) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 69,551 (migliore 71,861), 244 trade |

**Storia di ricerca**: 564 trade, netto 138,146, DD 51,840, average trade 245, UngerFit 0.89.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 564 su almeno 50 |
| average trade | passa | 245 contro la soglia 145 |
| anni | passa | 3 anni, minimo 147 trade in un anno, 148.0 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 68,651 |
| ultimo terzo | passa | netto 68,651, net/DD 3.05 (serve 1,3) |
| due terzi su tre | passa | 0 / 69,495 / 68,651 |
| outlier | passa | trade migliore 8% del netto sulla storia, 16% sull'ultimo terzo |
| plateau | passa | media 1.00 su 2 vicini, minimo 1.00 |

**Prova su FTMO**: 495 trade, netto -21,285, DD 142,477, net/DD -0.15, average trade -43 (soglia 234), UngerFit , finestre in utile 2/4 (44,308 / -59,910 / -52,780 / 47,097).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.15 (serve 1), average trade -43 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 65% del netto (massimo 40%); average trade / soglia per anno: 2022:1.9 2023:1.5 2024:1.9 2025:-1.4 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=23, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

930 simulazioni in 18.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 421.8, netto 69,495, 244 trade |
| finestra: fine | EndHour | -1 | 23 | ✔ | avg smussato 384.4, netto 69,495, 244 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 69,551 (migliore 71,073), 244 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 284.8, netto 69,495, 244 trade |
| YES long | FastYesLong | 152 | 139 | · | batte lo spento: netto 75,521 contro 69,495, net/DD 3.66 contro 1.34; annullato dall'ultimo terzo (68,651 -> 12,374) |
| YES short | FastYesShort | 152 | 22 | ✔ | batte lo spento: netto 67,953 contro 69,495, net/DD 2.01 contro 1.34 |
| NO long | FastNoLong | 153 | 28 | · | batte lo spento: netto 74,765 contro 67,953, net/DD 11.77 contro 2.01; annullato dall'ultimo terzo (69,774 -> 55,760) |
| NO short | FastNoShort | 153 | 90 | ✔ | batte lo spento: netto 72,382 contro 67,953, net/DD 2.16 contro 2.01 |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 77,896 contro 72,382, net/DD 2.29 contro 2.16; annullato dall'ultimo terzo (74,247 -> 55,979) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 79,044 contro 72,382, net/DD 2.36 contro 2.16 |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 82,243 (migliore 83,133), 168 trade |

**Storia di ricerca**: 401 trade, netto 175,127, DD 19,317, average trade 437, UngerFit 2.61.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 401 su almeno 50 |
| average trade | passa | 437 contro la soglia 145 |
| anni | passa | 3 anni, minimo 99 trade in un anno, 105.2 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 85,121 |
| ultimo terzo | passa | netto 85,121, net/DD 4.41 (serve 1,3) |
| due terzi su tre | passa | 0 / 90,006 / 85,121 |
| outlier | passa | trade migliore 6% del netto sulla storia, 9% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 365 con i pattern, 237 senza |
| pattern casuali | passa | 12 estrazioni su 67 fanno almeno altrettanto, p 0.19, Wilson 0.14 |
| plateau | passa | media 0.94 su 4 vicini, minimo 0.83 |

**Prova su FTMO**: 369 trade, netto 18,390, DD 68,941, net/DD 0.27, average trade 50 (soglia 234), UngerFit 0.12, finestre in utile 2/4 (4,375 / -27,849 / -14,301 / 56,166).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.27 (serve 1), average trade 50 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 42% del netto (massimo 40%); average trade / soglia per anno: 2022:3.2 2023:3.7 2024:2.0 2025:-1.1 2026:1.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=23, FastYesLong=152, FastYesShort=22, FastNoLong=153, FastNoShort=90, SkipDay=-1`

## BO Breakout di N sessioni

503 simulazioni in 13.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=0, IncludeCurrentSession=0 | ✔ | avg smussato 284.8, netto 69,495, 244 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 421.8, netto 69,495, 244 trade |
| finestra: fine | EndHour | -1 | 23 | ✔ | avg smussato 384.4, netto 69,495, 244 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 69,551 (migliore 71,073), 244 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 284.8, netto 69,495, 244 trade |
| YES | PtnNeutYes | 55 | 5 | · | batte lo spento: netto 78,360 contro 69,495, net/DD 2.69 contro 1.34; annullato dall'ultimo terzo (68,651 -> 31,202) |
| NO | PtnNeutNo | 56 | 2 | · | batte lo spento: netto 78,360 contro 69,495, net/DD 2.69 contro 1.34; annullato dall'ultimo terzo (68,651 -> 31,202) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 70,077 contro 69,495, net/DD 7.40 contro 1.34; annullato dall'ultimo terzo (68,651 -> 34,280) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 64,531 contro 69,495, net/DD 2.84 contro 1.34; annullato dall'ultimo terzo (68,651 -> 4,962) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 80,567 contro 69,495, net/DD 3.06 contro 1.34; annullato dall'ultimo terzo (68,651 -> 12,542) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 74,220 contro 69,495, net/DD 1.76 contro 1.34; annullato dall'ultimo terzo (68,651 -> 22,803) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 69,551 (migliore 71,861), 244 trade |

**Storia di ricerca**: 564 trade, netto 138,146, DD 51,840, average trade 245, UngerFit 0.89.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 564 su almeno 50 |
| average trade | passa | 245 contro la soglia 145 |
| anni | passa | 3 anni, minimo 147 trade in un anno, 148.0 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 68,651 |
| ultimo terzo | passa | netto 68,651, net/DD 3.05 (serve 1,3) |
| due terzi su tre | passa | 0 / 69,495 / 68,651 |
| outlier | passa | trade migliore 8% del netto sulla storia, 16% sull'ultimo terzo |
| plateau | passa | media 0.99 su 4 vicini, minimo 0.95 |

**Prova su FTMO**: 495 trade, netto -21,285, DD 142,477, net/DD -0.15, average trade -43 (soglia 234), UngerFit , finestre in utile 2/4 (44,308 / -59,910 / -52,780 / 47,097).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.15 (serve 1), average trade -43 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 65% del netto (massimo 40%); average trade / soglia per anno: 2022:1.9 2023:1.5 2024:1.9 2025:-1.4 2026:0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=23, LevelSource=0, IncludeCurrentSession=0, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=1, BreakoutOffsetTicks=0`

## BOS Breakout della sessione in corso

641 simulazioni in 13.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=10 | ✔ | avg smussato 1.8, netto 506, 288 trade |
| finestra: inizio | StartHour | -1 | 10 | ✔ | avg smussato 365.4, netto 94,180, 266 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 354.1, netto 94,180, 266 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 78,796 (migliore 81,356), 292 trade; annullato dall'ultimo terzo (58,800 -> 50,789) |
| intraday o overnight | IntradayOnly | 1 | 0 | ✔ | avg smussato 354.9, netto 94,414, 266 trade |
| YES | PtnNeutYes | 55 | 39 | · | batte lo spento: netto 83,149 contro 94,414, net/DD 4.20 contro 3.21; annullato dall'ultimo terzo (59,400 -> 9,702) |
| NO | PtnNeutNo | 56 | 49 | · | batte lo spento: netto 131,002 contro 94,414, net/DD 6.56 contro 3.21; annullato dall'ultimo terzo (59,400 -> 38,061) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 84,908 contro 94,414, net/DD 4.97 contro 3.21; annullato dall'ultimo terzo (59,400 -> -10,894) |
| direzionale NO | PtnDirNo | 53 | 3 | ✔ | batte lo spento: netto 123,041 contro 94,414, net/DD 5.51 contro 3.21 |
| calendario | SkipDay | -1 | 4 | · | batte lo spento: netto 127,008 contro 123,041, net/DD 7.12 contro 5.51; annullato dall'ultimo terzo (60,371 -> 51,921) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 115,048 (migliore 117,712), 208 trade; annullato dall'ultimo terzo (60,371 -> 10,411) |

**Storia di ricerca**: 458 trade, netto 183,412, DD 33,491, average trade 400, UngerFit 1.82.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 458 su almeno 50 |
| average trade | passa | 400 contro la soglia 145 |
| anni | passa | 3 anni, minimo 119 trade in un anno, 120.2 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 60,371 |
| ultimo terzo | passa | netto 60,371, net/DD 1.80 (serve 1,3) |
| due terzi su tre | passa | 0 / 123,041 / 60,371 |
| outlier | passa | trade migliore 8% del netto sulla storia, 16% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 232 con i pattern, 162 senza |
| pattern casuali | passa | 0 estrazioni su 5 fanno almeno altrettanto, p 0.17, Wilson 0.05 |
| plateau | passa | media 0.92 su 4 vicini, minimo 0.75 |

**Prova su FTMO**: 354 trade, netto -14,722, DD 111,162, net/DD -0.13, average trade -42 (soglia 234), UngerFit , finestre in utile 2/4 (36,302 / 19,196 / -28,408 / -41,811).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.13 (serve 1), average trade -42 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 60% del netto (massimo 40%); average trade / soglia per anno: 2022:4.9 2023:2.8 2024:2.0 2025:-0.2 2026:-1.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=23, IntradayOnly=0, ExitHour=-1, StartHour=10, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=3, SkipDay=-1, BreakoutOffsetTicks=10`

## VBO Volatility breakout

453 simulazioni in 9.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.5, Direction=0 | ✔ | avg smussato 136.0, netto 24,761, 182 trade |
| finestra: inizio | StartHour | -1 | 18 | · | avg smussato 247.6, netto 48,809, 151 trade; annullato dall'ultimo terzo (100,964 -> 23,158) |
| finestra: fine | EndHour | -1 | 19 | ✔ | avg smussato 143.3, netto 24,830, 175 trade |
| stop | StopAtr | 0.8 | 2.0 | ✔ | D2: netto smussato 41,889 (migliore 41,889), 175 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 248.7, netto 43,528, 175 trade |
| YES | PtnNeutYes | 55 | 25 | · | batte lo spento: netto 57,639 contro 43,528, net/DD 2.76 contro 1.22; annullato dall'ultimo terzo (110,811 -> 39,386) |
| NO | PtnNeutNo | 56 | 29 | · | batte lo spento: netto 57,639 contro 43,528, net/DD 2.76 contro 1.22; annullato dall'ultimo terzo (110,811 -> 39,386) |
| direzionale YES | PtnDirYes | 52 | -12 | · | batte lo spento: netto 68,246 contro 43,528, net/DD 4.57 contro 1.22; annullato dall'ultimo terzo (110,811 -> 21,097) |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 60,785 contro 43,528, net/DD 4.85 contro 1.22; annullato dall'ultimo terzo (110,811 -> 36,010) |
| calendario | SkipDay | -1 | 2 | · | batte lo spento: netto 54,910 contro 43,528, net/DD 1.65 contro 1.22; annullato dall'ultimo terzo (110,811 -> 58,124) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 48,783 contro 43,528, net/DD 1.37 contro 1.22; annullato dall'ultimo terzo (110,811 -> 110,089) |
| affinamento stop | StopAtr | 2.0 | 2.0 | · | D2: netto smussato 41,889 (migliore 41,889), 175 trade |

**Storia di ricerca**: 408 trade, netto 154,339, DD 35,566, average trade 378, UngerFit 1.67.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 408 su almeno 50 |
| average trade | passa | 378 contro la soglia 145 |
| anni | passa | 3 anni, minimo 99 trade in un anno, 107.1 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 110,811 |
| ultimo terzo | passa | netto 110,811, net/DD 6.23 (serve 1,3) |
| due terzi su tre | passa | 0 / 43,528 / 110,811 |
| outlier | passa | trade migliore 7% del netto sulla storia, 8% sull'ultimo terzo |
| plateau | passa | media 0.74 su 4 vicini, minimo 0.15 |

**Prova su FTMO**: 382 trade, netto 48,943, DD 136,012, net/DD 0.36, average trade 128 (soglia 234), UngerFit 0.23, finestre in utile 3/4 (43,455 / 48,629 / -78,903 / 46,361).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.36 (serve 1), average trade 128 (soglia 234), finestre 3/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 53% del netto (massimo 40%); average trade / soglia per anno: 2022:2.9 2023:1.0 2024:3.8 2025:-0.3 2026:0.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=2.0, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=19, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.5, Direction=0`

## MAC Incrocio di medie

126 simulazioni in 1.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=20, SlowPeriod=50, Direction=0 | ✔ | avg smussato 743.8, netto 78,097, 105 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 69,604 (migliore 72,001), 105 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 743.8, netto 78,097, 105 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 3 candidati batte lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 84,401 contro 78,097, net/DD 6.18 contro 5.72 |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 75,908 (migliore 78,305), 105 trade |

**Storia di ricerca**: 239 trade, netto 123,245, DD 23,166, average trade 516, UngerFit 2.82.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 239 su almeno 50 |
| average trade | passa | 516 contro la soglia 145 |
| anni | passa | 3 anni, minimo 58 trade in un anno, 62.7 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 38,843 |
| ultimo terzo | passa | netto 38,843, net/DD 1.68 (serve 1,3) |
| due terzi su tre | passa | 0 / 84,401 / 38,843 |
| outlier | passa | trade migliore 8% del netto sulla storia, 23% sull'ultimo terzo |
| plateau | passa | media 0.63 su 8 vicini, minimo 0.00 |

**Prova su FTMO**: 220 trade, netto 6,947, DD 90,198, net/DD 0.08, average trade 32 (soglia 234), UngerFit 0.07, finestre in utile 2/4 (24,128 / -55,125 / -19,390 / 57,333).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.08 (serve 1), average trade 32 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 50% del netto (massimo 40%); average trade / soglia per anno: 2022:5.1 2023:2.3 2024:3.4 2025:-2.6 2026:2.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=20, SlowPeriod=50, Direction=0`

## RBM Reversal Bollinger mirrored

568 simulazioni in 9.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=20, BbNumDevs=3.0 | ✔ | avg smussato 147.7, netto 34,999, 237 trade |
| finestra: inizio | StartHour | -1 | 11 | · | avg smussato 215.9, netto 47,862, 225 trade; annullato dall'ultimo terzo (-26,123 -> -34,468) |
| finestra: fine | EndHour | -1 | 20 | ✔ | avg smussato 156.7, netto 37,717, 237 trade |
| stop | StopAtr | 0.8 | 0.4 | · | D2: netto smussato 40,990 (migliore 42,944), 238 trade; annullato dall'ultimo terzo (-21,789 -> -48,062) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES | PtnNeutYes | 55 | 29 | ✔ | batte lo spento: netto 42,950 contro 37,717, net/DD 2.87 contro 1.09 |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 38,446 contro 42,950, net/DD 3.90 contro 2.87; annullato dall'ultimo terzo (-2,720 -> -24,433) |
| direzionale YES | PtnDirYes | 52 | 44 | ✔ | batte lo spento: netto 38,102 contro 42,950, net/DD 5.35 contro 2.87 |
| direzionale NO | PtnDirNo | 53 | - | · | saltato: PtnDirYes e' acceso |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 656.9, netto 38,102, 58 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 36,657 (migliore 38,579), 58 trade; annullato dall'ultimo terzo (3,454 -> -3,567) |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 0 | · | avg smussato 768.9, netto 43,061, 56 trade; annullato dall'ultimo terzo (3,454 -> -4,184) |

**Storia di ricerca**: 142 trade, netto 41,556, DD 18,575, average trade 293, UngerFit 1.79.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 142 su almeno 50 |
| average trade | passa | 293 contro la soglia 145 |
| anni | passa | 3 anni, minimo 32 trade in un anno, 37.3 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 3,454 |
| ultimo terzo | **no** | netto 3,454, net/DD 0.20 (serve 1,3) |
| due terzi su tre | passa | 0 / 38,102 / 3,454 |
| outlier | **no** | trade migliore 33% del netto sulla storia, 271% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 41 con i pattern, -67 senza |
| pattern casuali | **no** | 10 estrazioni su 26 fanno almeno altrettanto, p 0.41, Wilson 0.29 |
| plateau | **no** | media 0.59 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 105 trade, netto -25,248, DD 44,717, net/DD -0.56, average trade -240 (soglia 234), UngerFit , finestre in utile 1/4 (3,565 / -12,232 / -9,859 / -6,723).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.56 (serve 1), average trade -240 (soglia 234), finestre 1/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 124% del netto (massimo 40%); average trade / soglia per anno: 2022:3.6 2023:1.0 2024:1.8 2025:-1.2 2026:-1.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=20, PtnNeutYes=29, PtnNeutNo=56, PtnDirYes=44, PtnDirNo=53, SkipDay=-1, BbLength=20, BbNumDevs=3.0`

## RBU Reversal Bollinger unmirrored

966 simulazioni in 15.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=20, BbNumDevs=3.0 | ✔ | avg smussato 147.7, netto 34,999, 237 trade |
| finestra: inizio | StartHour | -1 | 11 | · | avg smussato 215.9, netto 47,862, 225 trade; annullato dall'ultimo terzo (-26,123 -> -34,468) |
| finestra: fine | EndHour | -1 | 20 | ✔ | avg smussato 156.7, netto 37,717, 237 trade |
| stop | StopAtr | 0.8 | 0.4 | · | D2: netto smussato 40,990 (migliore 42,944), 238 trade; annullato dall'ultimo terzo (-21,789 -> -48,062) |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| YES long | FastYesLong | 152 | 103 | ✔ | batte lo spento: netto 53,879 contro 37,717, net/DD 4.04 contro 1.09 |
| YES short | FastYesShort | 152 | 78 | ✔ | batte lo spento: netto 61,824 contro 53,879, net/DD 10.25 contro 4.04 |
| NO long | FastNoLong | 153 | 94 | · | batte lo spento: netto 64,773 contro 61,824, net/DD 11.17 contro 10.25; annullato dall'ultimo terzo (2,488 -> -1,567) |
| NO short | FastNoShort | 153 | 24 | ✔ | batte lo spento: netto 63,996 contro 61,824, net/DD 16.59 contro 10.25 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 1,230.7, netto 63,996, 52 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 65,148 (migliore 65,148), 52 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 4 | · | avg smussato 1,116.9, netto 65,335, 52 trade |

**Storia di ricerca**: 87 trade, netto 77,952, DD 5,426, average trade 896, UngerFit 10.11.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 87 su almeno 50 |
| average trade | passa | 896 contro la soglia 145 |
| anni | passa | 3 anni, minimo 17 trade in un anno, 22.8 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 12,618 |
| ultimo terzo | passa | netto 12,618, net/DD 2.33 (serve 1,3) |
| due terzi su tre | passa | 0 / 65,335 / 12,618 |
| outlier | **no** | trade migliore 18% del netto sulla storia, 53% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 361 con i pattern, -122 senza |
| pattern casuali | passa | 5 estrazioni su 180 fanno almeno altrettanto, p 0.03, Wilson 0.02 |
| plateau | passa | media 0.64 su 7 vicini, minimo 0.37 |

**Prova su FTMO**: 53 trade, netto -28,168, DD 34,408, net/DD -0.82, average trade -531 (soglia 234), UngerFit , finestre in utile 0/4 (-2,189 / -5,952 / -8,716 / -11,311).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.82 (serve 1), average trade -531 (soglia 234), finestre 0/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 84% del netto (massimo 40%); average trade / soglia per anno: 2022:7.5 2023:5.7 2024:2.0 2025:-2.5 2026:-2.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=20, FastYesLong=103, FastYesShort=78, FastNoLong=153, FastNoShort=24, SkipDay=-1, BbLength=20, BbNumDevs=3.0`

## RHL Reversal sui livelli di ieri

467 simulazioni in 7.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=2 | ✔ | nessuna in utile: netto massimo -1,736 |
| finestra: inizio | StartHour | -1 | 7 | ✔ | avg smussato 97.0, netto 11,305, 121 trade |
| finestra: fine | EndHour | -1 | 12 | · | avg smussato 247.6, netto 17,868, 65 trade; annullato dall'ultimo terzo (14,982 -> 3,561) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 17,871 (migliore 17,871), 121 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 28,199 contro 21,284, net/DD 1.82 contro 1.38; annullato dall'ultimo terzo (27,205 -> 19,390) |
| YES | PtnNeutYes | 55 | 13 | · | batte lo spento: netto 24,515 contro 21,284, net/DD 2.15 contro 1.38; annullato dall'ultimo terzo (27,205 -> 17,623) |
| NO | PtnNeutNo | 56 | 47 | · | batte lo spento: netto 19,859 contro 21,284, net/DD 2.85 contro 1.38; annullato dall'ultimo terzo (27,205 -> 18,618) |
| direzionale YES | PtnDirYes | 52 | -33 | · | batte lo spento: netto 30,228 contro 21,284, net/DD 3.56 contro 1.38; annullato dall'ultimo terzo (27,205 -> 16,662) |
| direzionale NO | PtnDirNo | 53 | -34 | · | batte lo spento: netto 30,228 contro 21,284, net/DD 3.56 contro 1.38; annullato dall'ultimo terzo (27,205 -> 16,662) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 29,646 contro 21,284, net/DD 2.99 contro 1.38; annullato dall'ultimo terzo (27,205 -> 17,621) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 175.9, netto 21,284, 121 trade |
| uscita a ora fissa | ExitHour | -1 | 16 | · | batte lo spento: netto 19,106 contro 21,284, net/DD 2.28 contro 1.38; annullato dall'ultimo terzo (27,205 -> -95) |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 17,871 (migliore 17,871), 121 trade |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 28,199 contro 21,284, net/DD 1.82 contro 1.38; annullato dall'ultimo terzo (27,205 -> 19,390) |
| durata | MaxBars | 4 | 4 | · | avg smussato 131.9, netto 21,284, 121 trade |

**Storia di ricerca**: 290 trade, netto 48,490, DD 15,455, average trade 167, UngerFit 1.12.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 290 su almeno 50 |
| average trade | passa | 167 contro la soglia 145 |
| anni | passa | 3 anni, minimo 69 trade in un anno, 76.1 all'anno, 2 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 27,205 |
| ultimo terzo | passa | netto 27,205, net/DD 2.04 (serve 1,3) |
| due terzi su tre | passa | 0 / 21,284 / 27,205 |
| outlier | **no** | trade migliore 18% del netto sulla storia, 32% sull'ultimo terzo |
| plateau | **no** | media 0.53 su 4 vicini, minimo 0.10 |

**Prova su FTMO**: 267 trade, netto -16,682, DD 47,407, net/DD -0.35, average trade -62 (soglia 234), UngerFit , finestre in utile 2/4 (1,988 / -36,247 / 20,136 / -2,559).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.35 (serve 1), average trade -62 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 2/5 anni in utile (serve 60%), anno migliore 82% del netto (massimo 40%); average trade / soglia per anno: 2022:1.8 2023:-0.2 2024:1.1 2025:-0.4 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=7, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=20, Direction=2`

## LF Level fader sul pivot di ieri

504 simulazioni in 6.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=0 | ✔ | avg smussato 56.0, netto 6,210, 111 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 6,182 (migliore 6,415), 111 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 28 | · | batte lo spento: netto 15,909 contro 7,149, net/DD 0.76 contro 0.31; annullato dall'ultimo terzo (-2,893 -> -9,201) |
| NO | PtnNeutNo | 56 | 24 | · | batte lo spento: netto 15,909 contro 7,149, net/DD 0.76 contro 0.31; annullato dall'ultimo terzo (-2,893 -> -9,201) |
| direzionale YES | PtnDirYes | 52 | -5 | ✔ | batte lo spento: netto 18,409 contro 7,149, net/DD 2.54 contro 0.31 |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | -1 | · | nessun valore sopra lo spento |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 361.0, netto 18,409, 51 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.6 | 0.5 | · | D2: netto smussato 16,331 (migliore 16,946), 51 trade; annullato dall'ultimo terzo (9,275 -> 4,150) |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | · | avg smussato 285.3, netto 16,683, 50 trade; annullato dall'ultimo terzo (9,275 -> 8,237) |

**Storia di ricerca**: 119 trade, netto 27,684, DD 7,548, average trade 233, UngerFit 2.23.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 119 su almeno 50 |
| average trade | passa | 233 contro la soglia 145 |
| anni | passa | 3 anni, minimo 28 trade in un anno, 31.2 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 9,275 |
| ultimo terzo | **no** | netto 9,275, net/DD 1.23 (serve 1,3) |
| due terzi su tre | passa | 0 / 18,409 / 9,275 |
| outlier | **no** | trade migliore 28% del netto sulla storia, 82% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 136 con i pattern, -21 senza |
| pattern casuali | passa | 4 estrazioni su 20 fanno almeno altrettanto, p 0.24, Wilson 0.14 |
| plateau | passa | media 0.73 su 5 vicini, minimo 0.41 |

**Prova su FTMO**: 123 trade, netto 498, DD 22,718, net/DD 0.02, average trade 4 (soglia 234), UngerFit 0.02, finestre in utile 2/4 (13,904 / -10,682 / 3,110 / -5,834).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.02 (serve 1), average trade 4 (soglia 234), finestre 2/4 (servono 3) |
| anni | **no** | 3/5 anni in utile (serve 60%), anno migliore 84% del netto (massimo 40%); average trade / soglia per anno: 2022:3.8 2023:0.4 2024:2.9 2025:-0.1 2026:-1.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=-5, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=0`

## LFHL Level fader sugli estremi di ieri

507 simulazioni in 6.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | avg smussato 71.8, netto 13,356, 186 trade |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 12,977 (migliore 13,247), 186 trade |
| target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 13,468 contro 13,214, net/DD 0.64 contro 0.63; annullato dall'ultimo terzo (-11,505 -> -13,339) |
| YES | PtnNeutYes | 55 | 14 | ✔ | batte lo spento: netto 32,126 contro 13,214, net/DD 4.80 contro 0.63 |
| NO | PtnNeutNo | 56 | 6 | ✔ | batte lo spento: netto 32,573 contro 32,126, net/DD 8.50 contro 4.80 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessun valore sopra lo spento |
| calendario short | NotEntryDayShort | -1 | 3 | · | batte lo spento: netto 36,115 contro 32,573, net/DD 9.42 contro 8.50; annullato dall'ultimo terzo (23,998 -> 22,352) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 592.2, netto 32,573, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| affinamento stop | StopAtr | 1.0 | 0.6 | · | D2: netto smussato 32,626 (migliore 32,626), 55 trade; annullato dall'ultimo terzo (23,998 -> 20,349) |
| affinamento target | TargetAtr | 0 | 1.0 | · | batte lo spento: netto 33,040 contro 32,573, net/DD 8.62 contro 8.50; annullato dall'ultimo terzo (23,998 -> 22,499) |
| durata | MaxBars | 4 | 4 | · | avg smussato 477.9, netto 32,573, 55 trade |

**Storia di ricerca**: 114 trade, netto 56,571, DD 9,288, average trade 496, UngerFit 4.28.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 114 su almeno 50 |
| average trade | passa | 496 contro la soglia 145 |
| anni | passa | 3 anni, minimo 34 trade in un anno, 29.9 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 23,998 |
| ultimo terzo | passa | netto 23,998, net/DD 2.58 (serve 1,3) |
| due terzi su tre | passa | 0 / 32,573 / 23,998 |
| outlier | **no** | trade migliore 15% del netto sulla storia, 36% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 407 con i pattern, -48 senza |
| pattern casuali | passa | 2 estrazioni su 36 fanno almeno altrettanto, p 0.08, Wilson 0.04 |
| plateau | passa | media 0.88 su 6 vicini, minimo 0.43 |

**Prova su FTMO**: 117 trade, netto -27,449, DD 41,088, net/DD -0.67, average trade -235 (soglia 234), UngerFit , finestre in utile 1/4 (4,040 / -5,596 / -1,340 / -24,553).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.67 (serve 1), average trade -235 (soglia 234), finestre 1/4 (servono 3) |
| anni | **no** | 4/5 anni in utile (serve 60%), anno migliore 86% del netto (massimo 40%); average trade / soglia per anno: 2022:4.2 2023:2.1 2024:2.2 2025:1.0 2026:-3.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=14, PtnNeutNo=6, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=-1, LevelShift=10`

## Riepilogo

- PCH: scartata; ricerca 529 trade netto 138,979 avg 263; prova 457 trade netto 8,357 net/DD 0.07 avg 18 fin 2/4; falliti: prova sul broker, anni
- TFM: scartata; ricerca 564 trade netto 138,146 avg 245; prova 495 trade netto -21,285 net/DD -0.15 avg -43 fin 2/4; falliti: prova sul broker, anni
- TFU: scartata; ricerca 401 trade netto 175,127 avg 437; prova 369 trade netto 18,390 net/DD 0.27 avg 50 fin 2/4; falliti: prova sul broker, anni
- BO: scartata; ricerca 564 trade netto 138,146 avg 245; prova 495 trade netto -21,285 net/DD -0.15 avg -43 fin 2/4; falliti: prova sul broker, anni
- BOS: scartata; ricerca 458 trade netto 183,412 avg 400; prova 354 trade netto -14,722 net/DD -0.13 avg -42 fin 2/4; falliti: prova sul broker, anni
- VBO: scartata; ricerca 408 trade netto 154,339 avg 378; prova 382 trade netto 48,943 net/DD 0.36 avg 128 fin 3/4; falliti: prova sul broker, anni
- MAC: scartata; ricerca 239 trade netto 123,245 avg 516; prova 220 trade netto 6,947 net/DD 0.08 avg 32 fin 2/4; falliti: prova sul broker, anni
- RBM: scartata; ricerca 142 trade netto 41,556 avg 293; prova 105 trade netto -25,248 net/DD -0.56 avg -240 fin 1/4; falliti: ultimo terzo, outlier, pattern casuali, plateau, prova sul broker, anni
- RBU: scartata; ricerca 87 trade netto 77,952 avg 896; prova 53 trade netto -28,168 net/DD -0.82 avg -531 fin 0/4; falliti: outlier, prova sul broker, anni
- RHL: scartata; ricerca 290 trade netto 48,490 avg 167; prova 267 trade netto -16,682 net/DD -0.35 avg -62 fin 2/4; falliti: outlier, plateau, prova sul broker, anni
- LF: scartata; ricerca 119 trade netto 27,684 avg 233; prova 123 trade netto 498 net/DD 0.02 avg 4 fin 2/4; falliti: ultimo terzo, outlier, prova sul broker, anni
- LFHL: scartata; ricerca 114 trade netto 56,571 avg 496; prova 117 trade netto -27,449 net/DD -0.67 avg -235 fin 1/4; falliti: outlier, prova sul broker, anni
