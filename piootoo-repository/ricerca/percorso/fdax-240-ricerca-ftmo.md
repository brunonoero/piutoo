# Percorso v4 — @FDAX 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (5,843 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,151 barre), mai vista dal percorso
- costi FTMO: spread 1.33 punti (mediana), swap long 4.5288 short 0.0457 pt/notte; orologio al minuto
- soglia di average trade (15% del range medio della barra): 309 nella ricerca, 464 nella prova

## PCH Price Channel

558 simulazioni in 14.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=5, OffsetTicks=0, Direction=0 | ✔ | avg smussato 93.6, netto 60,932, 651 trade |
| finestra: inizio | StartHour | -1 | 3 | · | avg smussato 155.8, netto 97,826, 628 trade; annullato dall'ultimo terzo (77,901 -> 57,093) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 93.6, netto 60,932, 651 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 73,258 (migliore 73,258), 691 trade; annullato dall'ultimo terzo (77,901 -> 55,632) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 93.6, netto 60,932, 651 trade |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 72,390 contro 60,932, net/DD 6.80 contro 0.98; annullato dall'ultimo terzo (77,901 -> 14,374) |
| NO | PtnNeutNo | 56 | 2 | · | batte lo spento: netto 104,112 contro 60,932, net/DD 1.90 contro 0.98; annullato dall'ultimo terzo (77,901 -> 44,554) |
| direzionale YES | PtnDirYes | 52 | 39 | · | batte lo spento: netto 70,993 contro 60,932, net/DD 5.90 contro 0.98; annullato dall'ultimo terzo (77,901 -> 13,542) |
| direzionale NO | PtnDirNo | 53 | 45 | · | batte lo spento: netto 127,025 contro 60,932, net/DD 2.25 contro 0.98; annullato dall'ultimo terzo (77,901 -> 59,742) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 93,547 contro 60,932, net/DD 1.69 contro 0.98 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 92,817 contro 93,547, net/DD 1.80 contro 1.69 |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 119,251 (migliore 119,251), 592 trade; annullato dall'ultimo terzo (95,355 -> 56,009) |

**Storia di ricerca**: 821 trade, netto 188,173, DD 51,543, average trade 229, UngerFit 0.57.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 821 su almeno 50 |
| average trade | **no** | 229 contro la soglia 309 |
| anni | passa | 5 anni, minimo 17 trade in un anno, 215.4 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 95,355 |
| ultimo terzo | passa | netto 95,355, net/DD 4.75 (serve 1,3) |
| due terzi su tre | passa | 60,401 / 32,416 / 95,355 |
| outlier | passa | trade migliore 8% del netto sulla storia, 7% sull'ultimo terzo |
| plateau | passa | media 0.68 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 428 trade, netto -48,854, DD 137,989, net/DD -0.35, average trade -114 (soglia 464), UngerFit , finestre in utile 1/4 (43,762 / -51,933 / -36,918 / -3,765).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.35 (serve 1), average trade -114 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 51% del netto (massimo 40%); average trade / soglia per anno: 2020:2.0 2021:0.7 2022:0.4 2023:0.7 2024:1.1 2025:-0.5 2026:-0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0, ChannelBars=5, OffsetTicks=0, Direction=0`

## TFM Trend following mirrored

639 simulazioni in 12.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 2 | ✔ | nessuna in utile: netto massimo -97,308 |
| finestra: fine | EndHour | -1 | 14 | ✔ | avg smussato 11.4, netto 6,296, 550 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 53,241 (migliore 53,241), 565 trade; annullato dall'ultimo terzo (60,229 -> 42,876) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 11.4, netto 6,296, 550 trade |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 65,806 contro 6,296, net/DD 4.35 contro 0.12; annullato dall'ultimo terzo (60,229 -> 1,691) |
| NO | PtnNeutNo | 56 | 33 | ✔ | batte lo spento: netto 73,427 contro 6,296, net/DD 2.67 contro 0.12 |
| direzionale YES | PtnDirYes | 52 | -49 | · | batte lo spento: netto 57,422 contro 73,427, net/DD 3.58 contro 2.67; annullato dall'ultimo terzo (74,136 -> 14,978) |
| direzionale NO | PtnDirNo | 53 | 45 | · | batte lo spento: netto 90,094 contro 73,427, net/DD 4.51 contro 2.67; annullato dall'ultimo terzo (74,136 -> 62,135) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 82,877 contro 73,427, net/DD 3.79 contro 2.67; annullato dall'ultimo terzo (74,136 -> 45,024) |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 90,554 contro 73,427, net/DD 3.79 contro 2.67; annullato dall'ultimo terzo (74,136 -> 64,772) |
| target | TargetAtr | 0 | 0.5 | ✔ | batte lo spento: netto 93,344 contro 73,427, net/DD 4.70 contro 2.67 |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 99,603 (migliore 99,603), 422 trade; annullato dall'ultimo terzo (74,218 -> 49,546) |

**Storia di ricerca**: 691 trade, netto 167,563, DD 19,852, average trade 242, UngerFit 0.98.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 691 su almeno 50 |
| average trade | **no** | 242 contro la soglia 309 |
| anni | passa | 5 anni, minimo 12 trade in un anno, 181.3 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 74,218 |
| ultimo terzo | passa | netto 74,218, net/DD 5.76 (serve 1,3) |
| due terzi su tre | passa | 61,804 / 31,540 / 74,218 |
| outlier | passa | trade migliore 3% del netto sulla storia, 5% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 265 con i pattern, 234 senza |
| pattern casuali | **no** | 2 estrazioni su 2 fanno almeno altrettanto, p 1.00, Wilson 0.55 |
| plateau | passa | media 0.86 su 3 vicini, minimo 0.80 |

**Prova su FTMO**: 381 trade, netto 13,468, DD 85,141, net/DD 0.16, average trade 35 (soglia 464), UngerFit 0.06, finestre in utile 2/4 (43,034 / -11,758 / 4,731 / -22,539).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.16 (serve 1), average trade 35 (soglia 464), finestre 2/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 31% del netto (massimo 40%); average trade / soglia per anno: 2020:1.5 2021:0.7 2022:0.7 2023:0.9 2024:0.9 2025:0.1 2026:-0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=2, EndHour=14, PtnNeutYes=55, PtnNeutNo=33, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

932 simulazioni in 21.3 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 2 | ✔ | nessuna in utile: netto massimo -97,308 |
| finestra: fine | EndHour | -1 | 14 | ✔ | avg smussato 11.4, netto 6,296, 550 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 53,241 (migliore 53,241), 565 trade; annullato dall'ultimo terzo (60,229 -> 42,876) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 11.4, netto 6,296, 550 trade |
| YES long | FastYesLong | 152 | 138 | · | batte lo spento: netto 28,514 contro 6,296, net/DD 0.64 contro 0.12; annullato dall'ultimo terzo (60,229 -> 27,359) |
| YES short | FastYesShort | 152 | 117 | · | batte lo spento: netto 74,815 contro 6,296, net/DD 2.04 contro 0.12; annullato dall'ultimo terzo (60,229 -> 40,416) |
| NO long | FastNoLong | 153 | 30 | · | batte lo spento: netto 35,901 contro 6,296, net/DD 0.79 contro 0.12; annullato dall'ultimo terzo (60,229 -> 47,904) |
| NO short | FastNoShort | 153 | 55 | ✔ | batte lo spento: netto 80,053 contro 6,296, net/DD 2.81 contro 0.12 |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 79,713 contro 80,053, net/DD 3.33 contro 2.81 |
| uscita a ora fissa | ExitHour | -1 | 17 | · | batte lo spento: netto 77,535 contro 79,713, net/DD 3.78 contro 3.33; annullato dall'ultimo terzo (83,087 -> 73,677) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 100,748 contro 79,713, net/DD 5.85 contro 3.33; annullato dall'ultimo terzo (83,087 -> 79,554) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 94,456 (migliore 94,456), 406 trade; annullato dall'ultimo terzo (83,087 -> 62,630) |

**Storia di ricerca**: 628 trade, netto 162,799, DD 23,920, average trade 259, UngerFit 0.95.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 628 su almeno 50 |
| average trade | **no** | 259 contro la soglia 309 |
| anni | passa | 5 anni, minimo 13 trade in un anno, 164.8 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 83,087 |
| ultimo terzo | passa | netto 83,087, net/DD 4.86 (serve 1,3) |
| due terzi su tre | passa | 36,092 / 43,621 / 83,087 |
| outlier | passa | trade migliore 8% del netto sulla storia, 12% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 355 con i pattern, 339 senza |
| pattern casuali | **no** | 13 estrazioni su 28 fanno almeno altrettanto, p 0.48, Wilson 0.37 |
| plateau | passa | media 0.76 su 2 vicini, minimo 0.65 |

**Prova su FTMO**: 338 trade, netto 84,956, DD 63,820, net/DD 1.33, average trade 251 (soglia 464), UngerFit 0.46, finestre in utile 3/4 (42,877 / 40,864 / -20,109 / 21,323).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.33 (serve 1), average trade 251 (soglia 464), finestre 3/4 (servono 3) |
| anni | passa | 7/7 anni in utile (serve 60%), anno migliore 24% del netto (massimo 40%); average trade / soglia per anno: 2020:2.0 2021:0.3 2022:0.9 2023:1.0 2024:1.1 2025:0.7 2026:0.1 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=2, EndHour=14, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=55, SkipDay=0`

## BO Breakout di N sessioni

512 simulazioni in 17.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=2, IncludeCurrentSession=1 | ✔ | avg smussato 38.2, netto 22,484, 588 trade |
| finestra: inizio | StartHour | -1 | 3 | · | avg smussato 78.0, netto 44,247, 567 trade; annullato dall'ultimo terzo (62,409 -> 56,163) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 38.2, netto 22,484, 588 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 52,583 (migliore 52,583), 608 trade; annullato dall'ultimo terzo (62,409 -> 50,027) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 38.2, netto 22,484, 588 trade |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 76,994 contro 22,484, net/DD 5.64 contro 0.32; annullato dall'ultimo terzo (62,409 -> 17,232) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 80,802 contro 22,484, net/DD 4.90 contro 0.32; annullato dall'ultimo terzo (62,409 -> 9,201) |
| direzionale YES | PtnDirYes | 52 | 39 | · | batte lo spento: netto 72,029 contro 22,484, net/DD 5.70 contro 0.32; annullato dall'ultimo terzo (62,409 -> 18,840) |
| direzionale NO | PtnDirNo | 53 | 49 | · | batte lo spento: netto 52,212 contro 22,484, net/DD 1.57 contro 0.32; annullato dall'ultimo terzo (62,409 -> 10,344) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 56,545 contro 22,484, net/DD 1.14 contro 0.32; annullato dall'ultimo terzo (62,409 -> 41,997) |
| uscita a ora fissa | ExitHour | -1 | 17 | ✔ | batte lo spento: netto 49,125 contro 22,484, net/DD 0.74 contro 0.32 |
| target | TargetAtr | 0 | 0.75 | ✔ | batte lo spento: netto 77,266 contro 49,125, net/DD 1.24 contro 0.74 |
| affinamento stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 68,320 (migliore 70,017), 571 trade; annullato dall'ultimo terzo (73,164 -> 56,055) |

**Storia di ricerca**: 858 trade, netto 150,429, DD 62,305, average trade 175, UngerFit 0.40.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 858 su almeno 50 |
| average trade | **no** | 175 contro la soglia 309 |
| anni | passa | 5 anni, minimo 17 trade in un anno, 225.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 73,164 |
| ultimo terzo | passa | netto 73,164, net/DD 2.67 (serve 1,3) |
| due terzi su tre | passa | 104,175 / -26,910 / 73,164 |
| outlier | passa | trade migliore 8% del netto sulla storia, 8% sull'ultimo terzo |
| plateau | passa | media 0.75 su 9 vicini, minimo 0.62 |

**Prova su FTMO**: 458 trade, netto -61,901, DD 159,415, net/DD -0.39, average trade -135 (soglia 464), UngerFit , finestre in utile 2/4 (51,492 / -40,780 / 10,054 / -83,944).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.39 (serve 1), average trade -135 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 80% del netto (massimo 40%); average trade / soglia per anno: 2020:3.9 2021:0.9 2022:-0.2 2023:0.8 2024:1.0 2025:-0.1 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0.75, MaxBars=0, IntradayOnly=1, ExitHour=17, StartHour=-1, EndHour=-1, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=1, BreakoutOffsetTicks=2`

## BOS Breakout della sessione in corso

444 simulazioni in 15.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=2 | ✔ | avg smussato 19.0, netto 13,215, 697 trade |
| finestra: inizio | StartHour | -1 | 3 | · | avg smussato 135.1, netto 92,822, 687 trade; annullato dall'ultimo terzo (66,431 -> 19,047) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 19.0, netto 13,215, 697 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 25,764 (migliore 25,764), 746 trade; annullato dall'ultimo terzo (66,431 -> 55,219) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 19.0, netto 13,215, 697 trade |
| YES | PtnNeutYes | 55 | 49 | · | batte lo spento: netto 70,973 contro 13,215, net/DD 3.17 contro 0.17; annullato dall'ultimo terzo (66,431 -> -8,170) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 55,567 contro 13,215, net/DD 2.12 contro 0.17; annullato dall'ultimo terzo (66,431 -> 12,256) |
| direzionale YES | PtnDirYes | 52 | -15 | · | batte lo spento: netto 136,161 contro 13,215, net/DD 4.60 contro 0.17; annullato dall'ultimo terzo (66,431 -> -2,421) |
| direzionale NO | PtnDirNo | 53 | 28 | · | batte lo spento: netto 196,429 contro 13,215, net/DD 4.49 contro 0.17; annullato dall'ultimo terzo (66,431 -> 20,187) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 32,242 contro 13,215, net/DD 0.51 contro 0.17; annullato dall'ultimo terzo (66,431 -> 61,293) |
| uscita a ora fissa | ExitHour | -1 | 20 | · | batte lo spento: netto 62,776 contro 13,215, net/DD 1.11 contro 0.17; annullato dall'ultimo terzo (66,431 -> 60,920) |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 23,965 contro 13,215, net/DD 0.40 contro 0.17 |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 41,239 (migliore 41,239), 800 trade; annullato dall'ultimo terzo (77,891 -> 75,577) |

**Storia di ricerca**: 1062 trade, netto 101,856, DD 60,535, average trade 96, UngerFit 0.22.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 1062 su almeno 50 |
| average trade | **no** | 96 contro la soglia 309 |
| anni | passa | 5 anni, minimo 22 trade in un anno, 278.7 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 77,891 |
| ultimo terzo | passa | netto 77,891, net/DD 2.31 (serve 1,3) |
| due terzi su tre | passa | -4,604 / 28,569 / 77,891 |
| outlier | passa | trade migliore 15% del netto sulla storia, 10% sull'ultimo terzo |
| plateau | passa | media 0.72 su 6 vicini, minimo 0.31 |

**Prova su FTMO**: 566 trade, netto 27,175, DD 178,735, net/DD 0.15, average trade 48 (soglia 464), UngerFit 0.05, finestre in utile 2/4 (54,825 / 89,755 / -73,435 / -52,270).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.15 (serve 1), average trade 48 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 71% del netto (massimo 40%); average trade / soglia per anno: 2020:2.4 2021:-0.1 2022:-0.2 2023:0.8 2024:1.1 2025:0.4 2026:-0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=2`

## VBO Volatility breakout

648 simulazioni in 13.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=1.0, Direction=2 | ✔ | avg smussato 128.9, netto 15,343, 119 trade |
| finestra: inizio | StartHour | -1 | 7 | ✔ | avg smussato 422.2, netto 43,483, 103 trade |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 422.2, netto 43,483, 103 trade |
| stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 41,462 (migliore 43,007), 103 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 397.9, netto 40,984, 103 trade |
| YES | PtnNeutYes | 55 | 5 | ✔ | batte lo spento: netto 58,151 contro 40,984, net/DD 5.86 contro 3.49 |
| NO | PtnNeutNo | 56 | 24 | · | batte lo spento: netto 57,953 contro 58,151, net/DD 6.94 contro 5.86; annullato dall'ultimo terzo (1,889 -> -4,200) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessun valore sopra lo spento |
| direzionale NO | PtnDirNo | 53 | 25 | ✔ | batte lo spento: netto 64,639 contro 58,151, net/DD 8.49 contro 5.86 |
| calendario | SkipDay | -1 | -1 | · | nessun valore sopra lo spento |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.5 | 0.3 | · | D2: netto smussato 64,606 (migliore 65,029), 50 trade; annullato dall'ultimo terzo (8,070 -> 7,328) |

**Storia di ricerca**: 84 trade, netto 72,709, DD 9,056, average trade 866, UngerFit 5.18.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 84 su almeno 50 |
| average trade | passa | 866 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 22.0 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 8,070 |
| ultimo terzo | **no** | netto 8,070, net/DD 0.89 (serve 1,3) |
| due terzi su tre | passa | 18,891 / 45,748 / 8,070 |
| outlier | **no** | trade migliore 18% del netto sulla storia, 105% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 237 con i pattern, -160 senza |
| pattern casuali | passa | 1 estrazioni su 10 fanno almeno altrettanto, p 0.18, Wilson 0.08 |
| plateau | passa | media 0.71 su 4 vicini, minimo 0.21 |

**Prova su FTMO**: 44 trade, netto -53,202, DD 53,591, net/DD -0.99, average trade -1,209 (soglia 464), UngerFit , finestre in utile 0/4 (-12,824 / -10,522 / -10,742 / -19,114).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.99 (serve 1), average trade -1,209 (soglia 464), finestre 0/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 179% del netto (massimo 40%); average trade / soglia per anno: 2020:5.0 2021:2.9 2022:4.3 2023:2.1 2024:0.0 2025:-2.3 2026:-2.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=7, EndHour=-1, PtnNeutYes=5, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=25, SkipDay=-1, AtrMultiplierLong=1.0, Direction=2`

## MAC Incrocio di medie

123 simulazioni in 2.4 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=5, SlowPeriod=100, Direction=2 | ✔ | avg smussato 962.9, netto 51,996, 54 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 53,599 (migliore 55,745), 54 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 1,170.0, netto 63,180, 54 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 53,599 (migliore 55,745), 54 trade |

**Storia di ricerca**: 84 trade, netto 74,363, DD 27,224, average trade 885, UngerFit 3.05.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 84 su almeno 50 |
| average trade | passa | 885 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 22.0 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 11,183 |
| ultimo terzo | **no** | netto 13,515, net/DD 0.80 (serve 1,3) |
| due terzi su tre | passa | 28,652 / 34,528 / 13,515 |
| outlier | **no** | trade migliore 30% del netto sulla storia, 81% sull'ultimo terzo |
| plateau | **no** | media 0.48 su 5 vicini, minimo 0.05 |

**Prova su FTMO**: 38 trade, netto 29,226, DD 38,027, net/DD 0.77, average trade 769 (soglia 464), UngerFit 1.83, finestre in utile 1/4 (-4,329 / 59,079 / -16,091 / -38,027).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.77 (serve 1), average trade 769 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 61% del netto (massimo 40%); average trade / soglia per anno: 2020:-4.0 2021:0.6 2022:10.7 2023:1.8 2024:-0.7 2025:6.6 2026:-1.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=5, SlowPeriod=100, Direction=2`

## RBM Reversal Bollinger mirrored

477 simulazioni in 13.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 192.3, netto 81,522, 424 trade |
| finestra: inizio | StartHour | -1 | 3 | ✔ | avg smussato 209.4, netto 82,702, 395 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 267.6, netto 81,520, 306 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 72,496 (migliore 73,993), 306 trade; annullato dall'ultimo terzo (21,564 -> 17,208) |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 88,512 contro 81,520, net/DD 2.87 contro 2.65; annullato dall'ultimo terzo (21,564 -> 20,467) |
| YES | PtnNeutYes | 55 | 54 | · | batte lo spento: netto 81,734 contro 81,520, net/DD 5.50 contro 2.65; annullato dall'ultimo terzo (21,564 -> 4,648) |
| NO | PtnNeutNo | 56 | 53 | · | batte lo spento: netto 81,734 contro 81,520, net/DD 5.50 contro 2.65; annullato dall'ultimo terzo (21,564 -> 4,648) |
| direzionale YES | PtnDirYes | 52 | 16 | · | batte lo spento: netto 67,719 contro 81,520, net/DD 5.31 contro 2.65; annullato dall'ultimo terzo (21,564 -> 10,383) |
| direzionale NO | PtnDirNo | 53 | -14 | · | batte lo spento: netto 98,739 contro 81,520, net/DD 4.53 contro 2.65; annullato dall'ultimo terzo (21,564 -> 932) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 98,007 contro 81,520, net/DD 4.31 contro 2.65; annullato dall'ultimo terzo (21,564 -> 10,845) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 266.4, netto 81,520, 306 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 72,496 (migliore 73,993), 306 trade; annullato dall'ultimo terzo (21,564 -> 17,208) |
| affinamento target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 88,512 contro 81,520, net/DD 2.87 contro 2.65; annullato dall'ultimo terzo (21,564 -> 20,467) |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 266.4, netto 81,520, 306 trade |

**Storia di ricerca**: 462 trade, netto 103,084, DD 30,806, average trade 223, UngerFit 0.72.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 462 su almeno 50 |
| average trade | **no** | 223 contro la soglia 309 |
| anni | passa | 5 anni, minimo 8 trade in un anno, 121.2 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 21,564 |
| ultimo terzo | **no** | netto 21,564, net/DD 0.88 (serve 1,3) |
| due terzi su tre | passa | 25,017 / 56,503 / 21,564 |
| outlier | **no** | trade migliore 14% del netto sulla storia, 31% sull'ultimo terzo |
| plateau | **no** | media 0.40 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 216 trade, netto -28,070, DD 60,387, net/DD -0.46, average trade -130 (soglia 464), UngerFit , finestre in utile 1/4 (-9,170 / -2,669 / 4,443 / -20,674).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.46 (serve 1), average trade -130 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 64% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.8 2021:0.9 2022:1.0 2023:0.3 2024:0.6 2025:-0.7 2026:0.0 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=3, EndHour=9, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RBU Reversal Bollinger unmirrored

972 simulazioni in 21.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 192.3, netto 81,522, 424 trade |
| finestra: inizio | StartHour | -1 | 3 | ✔ | avg smussato 209.4, netto 82,702, 395 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 267.6, netto 81,520, 306 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 72,496 (migliore 73,993), 306 trade; annullato dall'ultimo terzo (21,564 -> 17,208) |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 88,512 contro 81,520, net/DD 2.87 contro 2.65; annullato dall'ultimo terzo (21,564 -> 20,467) |
| YES long | FastYesLong | 152 | 3 | ✔ | batte lo spento: netto 106,895 contro 81,520, net/DD 3.55 contro 2.65 |
| YES short | FastYesShort | 152 | 78 | · | batte lo spento: netto 129,298 contro 106,895, net/DD 11.33 contro 3.55; annullato dall'ultimo terzo (24,046 -> 6,648) |
| NO long | FastNoLong | 153 | 137 | ✔ | batte lo spento: netto 110,537 contro 106,895, net/DD 4.18 contro 3.55 |
| NO short | FastNoShort | 153 | 63 | · | batte lo spento: netto 106,303 contro 110,537, net/DD 10.31 contro 4.18; annullato dall'ultimo terzo (24,722 -> 3,099) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 116,690 contro 110,537, net/DD 8.12 contro 4.18; annullato dall'ultimo terzo (24,722 -> 15,651) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 491.3, netto 110,537, 225 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 99,599 (migliore 102,758), 225 trade |
| affinamento target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 105,354 contro 98,363, net/DD 4.08 contro 3.81; annullato dall'ultimo terzo (31,449 -> 30,353) |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 437.2, netto 98,363, 225 trade |

**Storia di ricerca**: 348 trade, netto 129,812, DD 25,793, average trade 373, UngerFit 1.32.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 348 su almeno 50 |
| average trade | passa | 373 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 91.3 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 31,449 |
| ultimo terzo | passa | netto 31,449, net/DD 2.46 (serve 1,3) |
| due terzi su tre | passa | 30,472 / 67,891 / 31,449 |
| outlier | passa | trade migliore 11% del netto sulla storia, 21% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 256 con i pattern, 110 senza |
| pattern casuali | **no** | 11 estrazioni su 37 fanno almeno altrettanto, p 0.32, Wilson 0.23 |
| plateau | **no** | media 0.48 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 167 trade, netto -18,765, DD 47,308, net/DD -0.40, average trade -112 (soglia 464), UngerFit , finestre in utile 1/4 (-8,900 / 1,266 / -6,178 / -4,953).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.40 (serve 1), average trade -112 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 64% del netto (massimo 40%); average trade / soglia per anno: 2020:3.7 2021:1.0 2022:2.0 2023:0.6 2024:0.9 2025:-1.0 2026:0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=3, EndHour=9, FastYesLong=3, FastYesShort=152, FastNoLong=137, FastNoShort=153, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RHL Reversal sui livelli di ieri

666 simulazioni in 12.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=1 | ✔ | avg smussato 183.9, netto 46,341, 252 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 183.9, netto 46,341, 252 trade |
| finestra: fine | EndHour | -1 | 10 | ✔ | avg smussato 276.8, netto 61,450, 222 trade |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 55,572 (migliore 55,572), 222 trade |
| target | TargetAtr | 0 | 0.75 | ✔ | batte lo spento: netto 67,962 contro 56,334, net/DD 1.44 contro 1.23 |
| YES | PtnNeutYes | 55 | 34 | ✔ | batte lo spento: netto 106,494 contro 67,962, net/DD 9.96 contro 1.44 |
| NO | PtnNeutNo | 56 | 45 | · | batte lo spento: netto 111,074 contro 106,494, net/DD 10.13 contro 9.96; annullato dall'ultimo terzo (14,656 -> 12,508) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| intraday o overnight | IntradayOnly | 1 | 0 | · | avg smussato 1,816.9, netto 110,834, 61 trade; annullato dall'ultimo terzo (14,656 -> 11,059) |
| uscita a ora fissa | ExitHour | -1 | 22 | · | batte lo spento: netto 110,842 contro 106,494, net/DD 11.23 contro 9.96; annullato dall'ultimo terzo (14,656 -> 13,997) |
| affinamento stop | StopAtr | 1.0 | 0.8 | · | D2: netto smussato 99,058 (migliore 102,694), 68 trade; annullato dall'ultimo terzo (14,656 -> 14,268) |
| affinamento target | TargetAtr | 0.75 | 1.0 | ✔ | batte lo spento: netto 107,700 contro 100,879, net/DD 10.80 contro 9.38 |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 1,615.5, netto 109,853, 68 trade |

**Storia di ricerca**: 79 trade, netto 125,006, DD 9,970, average trade 1,582, UngerFit 9.02.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 79 su almeno 50 |
| average trade | passa | 1,582 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 20.7 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 15,153 |
| ultimo terzo | passa | netto 15,153, net/DD 5.29 (serve 1,3) |
| due terzi su tre | passa | 14,333 / 95,520 / 15,153 |
| outlier | **no** | trade migliore 13% del netto sulla storia, 43% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 1,378 con i pattern, -190 senza |
| pattern casuali | passa | 0 estrazioni su 30 fanno almeno altrettanto, p 0.03, Wilson 0.01 |
| plateau | passa | media 0.93 su 5 vicini, minimo 0.88 |

**Prova su FTMO**: 36 trade, netto 62,519, DD 19,559, net/DD 3.20, average trade 1,737 (soglia 464), UngerFit 5.76, finestre in utile 4/4 (8,335 / 28,046 / 12,946 / 18,702).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 3.20 (serve 1), average trade 1,737 (soglia 464), finestre 4/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 43% del netto (massimo 40%); average trade / soglia per anno: 2020:-3.7 2021:2.2 2022:4.6 2023:7.7 2024:6.8 2025:5.9 2026:1.0 |
| altri mercati | passa | 6/7 con net/DD ≥ 1 (serve meta'): @NQ 2.66, @ES 4.69, @YM 5.90, @FESX 5.60, @FCE 1.45, @Z 1.65, @NIY 0.19 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=10, PtnNeutYes=34, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=20, Direction=1`

## LF Level fader sul pivot di ieri

505 simulazioni in 7.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=5 | ✔ | avg smussato 286.3, netto 34,933, 122 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 37,533 (migliore 37,533), 122 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 15 | ✔ | batte lo spento: netto 40,978 contro 39,038, net/DD 2.49 contro 2.37 |
| NO | PtnNeutNo | 56 | 45 | ✔ | batte lo spento: netto 55,059 contro 40,978, net/DD 5.17 contro 2.49 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 1 | ✔ | batte lo spento: netto 56,847 contro 55,059, net/DD 5.34 contro 5.17 |
| calendario short | NotEntryDayShort | -1 | 3 | · | batte lo spento: netto 58,052 contro 56,847, net/DD 5.45 contro 5.34; annullato dall'ultimo terzo (-877 -> -2,679) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 931.9, netto 56,847, 61 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 56,785 (migliore 56,785), 61 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| durata | MaxBars | 4 | 2 | · | avg smussato 974.8, netto 62,076, 61 trade; annullato dall'ultimo terzo (-877 -> -12,305) |

**Storia di ricerca**: 98 trade, netto 55,970, DD 13,022, average trade 571, UngerFit 2.85.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 98 su almeno 50 |
| average trade | passa | 571 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 25.7 all'anno, 5 in utile |
| utile recente | **no** | trade usciti nell'ultimo terzo: -877 |
| ultimo terzo | **no** | netto -877, net/DD -0.07 (serve 1,3) |
| due terzi su tre | passa | 35,720 / 21,127 / -877 |
| outlier | passa | trade migliore 22% del netto sulla storia,  sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade -24 con i pattern, -60 senza |
| pattern casuali | **no** | 5 estrazioni su 17 fanno almeno altrettanto, p 0.33, Wilson 0.21 |
| plateau | passa | media 0.91 su 6 vicini, minimo 0.73 |

**Prova su FTMO**: 44 trade, netto -35,124, DD 44,515, net/DD -0.79, average trade -798 (soglia 464), UngerFit , finestre in utile 1/4 (-22,756 / -20,557 / 14,641 / -6,027).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.79 (serve 1), average trade -798 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 113% del netto (massimo 40%); average trade / soglia per anno: 2020:10.7 2021:3.0 2022:1.2 2023:1.5 2024:-1.2 2025:-1.4 2026:-0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=15, PtnNeutNo=45, PtnDirYes=52, NotEntryDayLong=1, NotEntryDayShort=-1, LevelShift=5`

## LFHL Level fader sugli estremi di ieri

505 simulazioni in 8.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | avg smussato 526.9, netto 86,414, 164 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 84,209 (migliore 88,244), 164 trade |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 90,614 contro 86,414, net/DD 7.11 contro 6.78 |
| YES | PtnNeutYes | 55 | 33 | ✔ | batte lo spento: netto 87,653 contro 90,614, net/DD 12.26 contro 7.11 |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 69,177 contro 87,653, net/DD 17.13 contro 12.26; annullato dall'ultimo terzo (6,109 -> 4,197) |
| direzionale YES | PtnDirYes | 52 | -44 | · | batte lo spento: netto 61,516 contro 87,653, net/DD 13.78 contro 12.26; annullato dall'ultimo terzo (6,109 -> -1,161) |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| calendario short | NotEntryDayShort | -1 | 0 | ✔ | batte lo spento: netto 88,237 contro 87,653, net/DD 21.20 contro 12.26 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 1,038.1, netto 88,237, 85 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 85,394 (migliore 88,714), 85 trade |
| affinamento target | TargetAtr | 2.0 | 2.0 | · | batte lo spento: netto 88,237 contro 86,255, net/DD 21.20 contro 20.73 |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 1,003.7, netto 82,873, 84 trade |

**Storia di ricerca**: 112 trade, netto 93,098, DD 8,317, average trade 831, UngerFit 5.19.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 112 su almeno 50 |
| average trade | passa | 831 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 29.4 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 10,225 |
| ultimo terzo | passa | netto 10,225, net/DD 2.91 (serve 1,3) |
| due terzi su tre | passa | 30,746 / 52,126 / 10,225 |
| outlier | **no** | trade migliore 14% del netto sulla storia, 50% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 365 con i pattern, -61 senza |
| pattern casuali | passa | 3 estrazioni su 23 fanno almeno altrettanto, p 0.17, Wilson 0.09 |
| plateau | passa | media 0.90 su 8 vicini, minimo 0.52 |

**Prova su FTMO**: 71 trade, netto 36,991, DD 25,765, net/DD 1.44, average trade 521 (soglia 464), UngerFit 1.51, finestre in utile 2/4 (-2,329 / -16,456 / 14,684 / 41,092).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.44 (serve 1), average trade 521 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 46% del netto (massimo 40%); average trade / soglia per anno: 2020:-2.0 2021:2.9 2022:3.0 2023:1.0 2024:1.1 2025:-0.7 2026:2.7 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=33, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=0, LevelShift=10`

## Riepilogo

- PCH: scartata; ricerca 821 trade netto 188,173 avg 229; prova 428 trade netto -48,854 net/DD -0.35 avg -114 fin 1/4; falliti: average trade, prova sul broker, anni
- TFM: scartata; ricerca 691 trade netto 167,563 avg 242; prova 381 trade netto 13,468 net/DD 0.16 avg 35 fin 2/4; falliti: average trade, pattern casuali, prova sul broker
- TFU: scartata; ricerca 628 trade netto 162,799 avg 259; prova 338 trade netto 84,956 net/DD 1.33 avg 251 fin 3/4; falliti: average trade, pattern casuali, prova sul broker
- BO: scartata; ricerca 858 trade netto 150,429 avg 175; prova 458 trade netto -61,901 net/DD -0.39 avg -135 fin 2/4; falliti: average trade, prova sul broker, anni
- BOS: scartata; ricerca 1062 trade netto 101,856 avg 96; prova 566 trade netto 27,175 net/DD 0.15 avg 48 fin 2/4; falliti: average trade, prova sul broker, anni
- VBO: scartata; ricerca 84 trade netto 72,709 avg 866; prova 44 trade netto -53,202 net/DD -0.99 avg -1,209 fin 0/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- MAC: scartata; ricerca 84 trade netto 74,363 avg 885; prova 38 trade netto 29,226 net/DD 0.77 avg 769 fin 1/4; falliti: anni, ultimo terzo, outlier, plateau, prova sul broker, anni
- RBM: scartata; ricerca 462 trade netto 103,084 avg 223; prova 216 trade netto -28,070 net/DD -0.46 avg -130 fin 1/4; falliti: average trade, ultimo terzo, outlier, plateau, prova sul broker, anni
- RBU: scartata; ricerca 348 trade netto 129,812 avg 373; prova 167 trade netto -18,765 net/DD -0.40 avg -112 fin 1/4; falliti: anni, pattern casuali, plateau, prova sul broker, anni
- RHL: scartata; ricerca 79 trade netto 125,006 avg 1,582; prova 36 trade netto 62,519 net/DD 3.20 avg 1,737 fin 4/4; falliti: anni, outlier, anni
- LF: scartata; ricerca 98 trade netto 55,970 avg 571; prova 44 trade netto -35,124 net/DD -0.79 avg -798 fin 1/4; falliti: anni, utile recente, ultimo terzo, pattern casuali, prova sul broker, anni
- LFHL: scartata; ricerca 112 trade netto 93,098 avg 831; prova 71 trade netto 36,991 net/DD 1.44 avg 521 fin 2/4; falliti: anni, outlier, prova sul broker, anni
