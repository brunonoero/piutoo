# Percorso v4 — @FDAX 240m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (5,843 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (3,151 barre), mai vista dal percorso
- costi FTMO: spread 1.33 punti (mediana), swap long 4.5288 short 0.0457 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 309 nella ricerca, 464 nella prova

## PCH Price Channel

556 simulazioni in 14.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=5, OffsetTicks=0, Direction=0 | ✔ | avg smussato 127.7, netto 84,274, 660 trade |
| finestra: inizio | StartHour | -1 | 7 | · | avg smussato 140.0, netto 78,399, 560 trade; annullato dall'ultimo terzo (85,518 -> 44,705) |
| finestra: fine | EndHour | -1 | -1 | · | avg smussato 127.7, netto 84,274, 660 trade |
| stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 113,066 (migliore 113,066), 715 trade; annullato dall'ultimo terzo (85,518 -> 83,340) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 127.7, netto 84,274, 660 trade |
| YES | PtnNeutYes | 55 | 6 | · | batte lo spento: netto 97,192 contro 84,274, net/DD 1.42 contro 1.38; annullato dall'ultimo terzo (85,518 -> 53,829) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 97,192 contro 84,274, net/DD 1.42 contro 1.38; annullato dall'ultimo terzo (85,518 -> 53,829) |
| direzionale YES | PtnDirYes | 52 | -38 | · | batte lo spento: netto 97,453 contro 84,274, net/DD 4.32 contro 1.38; annullato dall'ultimo terzo (85,518 -> 4,352) |
| direzionale NO | PtnDirNo | 53 | 28 | · | batte lo spento: netto 162,630 contro 84,274, net/DD 6.41 contro 1.38; annullato dall'ultimo terzo (85,518 -> 71,821) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 109,927 contro 84,274, net/DD 2.58 contro 1.38; annullato dall'ultimo terzo (85,518 -> 74,061) |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 88,309 contro 84,274, net/DD 1.40 contro 1.38; annullato dall'ultimo terzo (85,518 -> 80,922) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 122,350 (migliore 122,350), 715 trade; annullato dall'ultimo terzo (85,518 -> 83,340) |

**Storia di ricerca**: 997 trade, netto 169,792, DD 60,880, average trade 170, UngerFit 0.39.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 997 su almeno 50 |
| average trade | **no** | 170 contro la soglia 309 |
| anni | passa | 5 anni, minimo 20 trade in un anno, 261.6 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 85,518 |
| ultimo terzo | passa | netto 85,518, net/DD 3.68 (serve 1,3) |
| due terzi su tre | passa | 60,498 / 23,775 / 85,518 |
| outlier | passa | trade migliore 11% del netto sulla storia, 12% sull'ultimo terzo |
| plateau | passa | media 0.61 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 532 trade, netto -41,635, DD 149,465, net/DD -0.28, average trade -78 (soglia 464), UngerFit , finestre in utile 1/4 (56,929 / -14,751 / -45,784 / -42,863).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.28 (serve 1), average trade -78 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 61% del netto (massimo 40%); average trade / soglia per anno: 2020:1.4 2021:0.4 2022:0.3 2023:1.2 2024:0.7 2025:-0.2 2026:-0.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, ChannelBars=5, OffsetTicks=0, Direction=0`

## TFM Trend following mirrored

437 simulazioni in 13.2 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 2 | · | nessuna in utile: netto massimo -82,866; annullato dall'ultimo terzo (63,523 -> 60,220) |
| finestra: fine | EndHour | -1 | 14 | ✔ | avg smussato 15.9, netto 9,005, 568 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 60,924 (migliore 60,924), 592 trade; annullato dall'ultimo terzo (71,559 -> 64,601) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 15.9, netto 9,005, 568 trade |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 88,645 contro 9,005, net/DD 5.90 contro 0.14; annullato dall'ultimo terzo (71,559 -> 25,345) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 61,237 contro 9,005, net/DD 4.41 contro 0.14; annullato dall'ultimo terzo (71,559 -> 16,930) |
| direzionale YES | PtnDirYes | 52 | 39 | · | batte lo spento: netto 93,368 contro 9,005, net/DD 7.39 contro 0.14; annullato dall'ultimo terzo (71,559 -> 23,885) |
| direzionale NO | PtnDirNo | 53 | 49 | · | batte lo spento: netto 103,141 contro 9,005, net/DD 3.43 contro 0.14; annullato dall'ultimo terzo (71,559 -> 36,802) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 28,507 contro 9,005, net/DD 0.49 contro 0.14; annullato dall'ultimo terzo (71,559 -> 42,615) |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 27,082 contro 9,005, net/DD 0.62 contro 0.14; annullato dall'ultimo terzo (71,559 -> 41,210) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 27,841 contro 9,005, net/DD 0.43 contro 0.14; annullato dall'ultimo terzo (71,559 -> 48,699) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 47,613 (migliore 47,613), 590 trade; annullato dall'ultimo terzo (71,559 -> 63,393) |

**Storia di ricerca**: 873 trade, netto 80,563, DD 66,467, average trade 92, UngerFit 0.20.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 873 su almeno 50 |
| average trade | **no** | 92 contro la soglia 309 |
| anni | passa | 5 anni, minimo 16 trade in un anno, 229.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 71,559 |
| ultimo terzo | passa | netto 71,559, net/DD 1.98 (serve 1,3) |
| due terzi su tre | passa | 35,519 / -26,514 / 71,559 |
| outlier | passa | trade migliore 18% del netto sulla storia, 13% sull'ultimo terzo |
| plateau | passa | media 0.82 su 2 vicini, minimo 0.63 |

**Prova su FTMO**: 465 trade, netto -6,629, DD 143,535, net/DD -0.05, average trade -14 (soglia 464), UngerFit , finestre in utile 2/4 (40,892 / 27,288 / -6,764 / -76,137).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.05 (serve 1), average trade -14 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 75% del netto (massimo 40%); average trade / soglia per anno: 2020:3.2 2021:0.0 2022:0.0 2023:0.8 2024:0.5 2025:0.5 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=14, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1`

## TFU Trend following unmirrored

731 simulazioni in 18.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| finestra: inizio | StartHour | -1 | 2 | · | nessuna in utile: netto massimo -82,866; annullato dall'ultimo terzo (63,523 -> 60,220) |
| finestra: fine | EndHour | -1 | 14 | ✔ | avg smussato 15.9, netto 9,005, 568 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 60,924 (migliore 60,924), 592 trade; annullato dall'ultimo terzo (71,559 -> 64,601) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 15.9, netto 9,005, 568 trade |
| YES long | FastYesLong | 152 | 26 | · | batte lo spento: netto 29,397 contro 9,005, net/DD 0.51 contro 0.14; annullato dall'ultimo terzo (71,559 -> 58,736) |
| YES short | FastYesShort | 152 | 117 | · | batte lo spento: netto 115,558 contro 9,005, net/DD 2.81 contro 0.14; annullato dall'ultimo terzo (71,559 -> 63,942) |
| NO long | FastNoLong | 153 | 30 | · | batte lo spento: netto 29,397 contro 9,005, net/DD 0.51 contro 0.14; annullato dall'ultimo terzo (71,559 -> 58,736) |
| NO short | FastNoShort | 153 | 149 | · | batte lo spento: netto 103,311 contro 9,005, net/DD 2.56 contro 0.14; annullato dall'ultimo terzo (71,559 -> 37,696) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 28,507 contro 9,005, net/DD 0.49 contro 0.14; annullato dall'ultimo terzo (71,559 -> 42,615) |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 27,082 contro 9,005, net/DD 0.62 contro 0.14; annullato dall'ultimo terzo (71,559 -> 41,210) |
| target | TargetAtr | 0 | 0.5 | · | batte lo spento: netto 27,841 contro 9,005, net/DD 0.43 contro 0.14; annullato dall'ultimo terzo (71,559 -> 48,699) |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 47,613 (migliore 47,613), 590 trade; annullato dall'ultimo terzo (71,559 -> 63,393) |

**Storia di ricerca**: 873 trade, netto 80,563, DD 66,467, average trade 92, UngerFit 0.20.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 873 su almeno 50 |
| average trade | **no** | 92 contro la soglia 309 |
| anni | passa | 5 anni, minimo 16 trade in un anno, 229.1 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 71,559 |
| ultimo terzo | passa | netto 71,559, net/DD 1.98 (serve 1,3) |
| due terzi su tre | passa | 35,519 / -26,514 / 71,559 |
| outlier | passa | trade migliore 18% del netto sulla storia, 13% sull'ultimo terzo |
| plateau | passa | media 0.82 su 2 vicini, minimo 0.63 |

**Prova su FTMO**: 465 trade, netto -6,629, DD 143,535, net/DD -0.05, average trade -14 (soglia 464), UngerFit , finestre in utile 2/4 (40,892 / 27,288 / -6,764 / -76,137).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.05 (serve 1), average trade -14 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 75% del netto (massimo 40%); average trade / soglia per anno: 2020:3.2 2021:0.0 2022:0.0 2023:0.8 2024:0.5 2025:0.5 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=14, FastYesLong=152, FastYesShort=152, FastNoLong=153, FastNoShort=153, SkipDay=-1`

## BO Breakout di N sessioni

512 simulazioni in 13.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | Sessions+BreakoutOffsetTicks+IncludeCurrentSession | Sessions=-, BreakoutOffsetTicks=-, IncludeCurrentSession=0 | Sessions=1, BreakoutOffsetTicks=2, IncludeCurrentSession=1 | ✔ | avg smussato 48.0, netto 27,843, 580 trade |
| finestra: inizio | StartHour | -1 | 3 | · | avg smussato 114.9, netto 63,336, 551 trade; annullato dall'ultimo terzo (72,927 -> 59,098) |
| finestra: fine | EndHour | -1 | 22 | ✔ | avg smussato 48.0, netto 27,843, 580 trade |
| stop | StopAtr | 0.8 | 0.3 | · | D2: netto smussato 57,167 (migliore 57,167), 599 trade; annullato dall'ultimo terzo (72,927 -> 68,287) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 48.0, netto 27,843, 580 trade |
| YES | PtnNeutYes | 55 | 45 | · | batte lo spento: netto 70,529 contro 27,843, net/DD 4.65 contro 0.40; annullato dall'ultimo terzo (72,927 -> 22,129) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 75,594 contro 27,843, net/DD 5.20 contro 0.40; annullato dall'ultimo terzo (72,927 -> 15,141) |
| direzionale YES | PtnDirYes | 52 | 39 | · | batte lo spento: netto 65,725 contro 27,843, net/DD 5.20 contro 0.40; annullato dall'ultimo terzo (72,927 -> 23,693) |
| direzionale NO | PtnDirNo | 53 | 49 | · | batte lo spento: netto 71,763 contro 27,843, net/DD 2.17 contro 0.40; annullato dall'ultimo terzo (72,927 -> 12,702) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 45,694 contro 27,843, net/DD 0.90 contro 0.40; annullato dall'ultimo terzo (72,927 -> 49,743) |
| uscita a ora fissa | ExitHour | -1 | 15 | · | batte lo spento: netto 39,758 contro 27,843, net/DD 0.96 contro 0.40; annullato dall'ultimo terzo (72,927 -> 52,067) |
| target | TargetAtr | 0 | 0.75 | ✔ | batte lo spento: netto 55,760 contro 27,843, net/DD 0.88 contro 0.40 |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 58,693 (migliore 58,693), 597 trade |

**Storia di ricerca**: 917 trade, netto 135,426, DD 48,706, average trade 148, UngerFit 0.38.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 917 su almeno 50 |
| average trade | **no** | 148 contro la soglia 309 |
| anni | passa | 5 anni, minimo 17 trade in un anno, 240.6 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 88,823 |
| ultimo terzo | passa | netto 88,823, net/DD 3.23 (serve 1,3) |
| due terzi su tre | passa | 76,197 / -29,595 / 88,823 |
| outlier | passa | trade migliore 9% del netto sulla storia, 7% sull'ultimo terzo |
| plateau | passa | media 0.87 su 7 vicini, minimo 0.72 |

**Prova su FTMO**: 489 trade, netto -12,549, DD 84,064, net/DD -0.15, average trade -26 (soglia 464), UngerFit , finestre in utile 3/4 (23,542 / 2,122 / 13,366 / -52,037).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.15 (serve 1), average trade -26 (soglia 464), finestre 3/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 48% del netto (massimo 40%); average trade / soglia per anno: 2020:3.5 2021:0.6 2022:-0.2 2023:0.7 2024:0.8 2025:0.3 2026:-0.6 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0.75, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=22, LevelSource=0, IncludeCurrentSession=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, Sessions=1, BreakoutOffsetTicks=2`

## BOS Breakout della sessione in corso

444 simulazioni in 13.0 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BreakoutOffsetTicks | BreakoutOffsetTicks=- | BreakoutOffsetTicks=5 | ✔ | avg smussato 75.6, netto 52,152, 690 trade |
| finestra: inizio | StartHour | -1 | 3 | · | avg smussato 147.1, netto 99,604, 677 trade; annullato dall'ultimo terzo (85,529 -> -8,011) |
| finestra: fine | EndHour | -1 | 22 | ✔ | avg smussato 75.6, netto 52,152, 690 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 55,503 (migliore 55,503), 739 trade; annullato dall'ultimo terzo (85,529 -> 74,386) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 75.6, netto 52,152, 690 trade |
| YES | PtnNeutYes | 55 | 10 | · | batte lo spento: netto 72,691 contro 52,152, net/DD 4.31 contro 0.74; annullato dall'ultimo terzo (85,529 -> 22,417) |
| NO | PtnNeutNo | 56 | 16 | · | batte lo spento: netto 72,691 contro 52,152, net/DD 4.31 contro 0.74; annullato dall'ultimo terzo (85,529 -> 17,749) |
| direzionale YES | PtnDirYes | 52 | -15 | · | batte lo spento: netto 117,961 contro 52,152, net/DD 3.12 contro 0.74; annullato dall'ultimo terzo (85,529 -> 9,514) |
| direzionale NO | PtnDirNo | 53 | 28 | · | batte lo spento: netto 196,969 contro 52,152, net/DD 4.98 contro 0.74; annullato dall'ultimo terzo (85,529 -> 21,925) |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 59,954 contro 52,152, net/DD 1.44 contro 0.74; annullato dall'ultimo terzo (85,529 -> 79,667) |
| uscita a ora fissa | ExitHour | -1 | 17 | ✔ | batte lo spento: netto 56,565 contro 52,152, net/DD 0.97 contro 0.74 |
| target | TargetAtr | 0 | 2.0 | · | batte lo spento: netto 57,228 contro 56,565, net/DD 0.99 contro 0.97; annullato dall'ultimo terzo (88,462 -> 87,211) |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 54,769 (migliore 57,299), 654 trade |

**Storia di ricerca**: 989 trade, netto 145,027, DD 58,089, average trade 147, UngerFit 0.35.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 989 su almeno 50 |
| average trade | **no** | 147 contro la soglia 309 |
| anni | passa | 5 anni, minimo 20 trade in un anno, 259.5 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 88,462 |
| ultimo terzo | passa | netto 88,462, net/DD 2.23 (serve 1,3) |
| due terzi su tre | passa | 30,924 / 25,641 / 88,462 |
| outlier | passa | trade migliore 11% del netto sulla storia, 11% sull'ultimo terzo |
| plateau | passa | media 0.81 su 6 vicini, minimo 0.56 |

**Prova su FTMO**: 532 trade, netto 116,680, DD 75,391, net/DD 1.55, average trade 219 (soglia 464), UngerFit 0.37, finestre in utile 3/4 (39,462 / 72,523 / -43,364 / 42,763).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.55 (serve 1), average trade 219 (soglia 464), finestre 3/4 (servono 3) |
| anni | passa | 6/7 anni in utile (serve 60%), anno migliore 36% del netto (massimo 40%); average trade / soglia per anno: 2020:1.8 2021:0.3 2022:-0.1 2023:0.9 2024:1.2 2025:0.3 2026:0.5 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=17, StartHour=-1, EndHour=22, LevelSource=1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BreakoutOffsetTicks=5`

## VBO Volatility breakout

458 simulazioni in 11.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | AtrMultiplierLong+Direction | AtrMultiplierLong=-, Direction=- | AtrMultiplierLong=0.5, Direction=2 | ✔ | avg smussato 48.4, netto 13,130, 271 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 48.4, netto 13,130, 271 trade |
| finestra: fine | EndHour | -1 | 10 | · | avg smussato 81.2, netto 20,129, 248 trade; annullato dall'ultimo terzo (30,776 -> 15,562) |
| stop | StopAtr | 0.8 | 2.0 | · | D2: netto smussato 12,940 (migliore 13,173), 271 trade; annullato dall'ultimo terzo (30,776 -> 24,543) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 48.4, netto 13,130, 271 trade |
| YES | PtnNeutYes | 55 | 6 | · | batte lo spento: netto 58,649 contro 13,130, net/DD 2.34 contro 0.27; annullato dall'ultimo terzo (30,776 -> 13,135) |
| NO | PtnNeutNo | 56 | 3 | · | batte lo spento: netto 58,649 contro 13,130, net/DD 2.34 contro 0.27; annullato dall'ultimo terzo (30,776 -> 13,135) |
| direzionale YES | PtnDirYes | 52 | -28 | · | batte lo spento: netto 94,201 contro 13,130, net/DD 7.24 contro 0.27; annullato dall'ultimo terzo (30,776 -> 14,479) |
| direzionale NO | PtnDirNo | 53 | 27 | · | batte lo spento: netto 50,404 contro 13,130, net/DD 2.90 contro 0.27; annullato dall'ultimo terzo (30,776 -> 14,515) |
| calendario | SkipDay | -1 | 3 | · | batte lo spento: netto 34,061 contro 13,130, net/DD 0.67 contro 0.27; annullato dall'ultimo terzo (30,776 -> 7,512) |
| uscita a ora fissa | ExitHour | -1 | 15 | ✔ | batte lo spento: netto 49,081 contro 13,130, net/DD 1.61 contro 0.27 |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 49,519 contro 49,081, net/DD 1.62 contro 1.61 |
| affinamento stop | StopAtr | 0.8 | 0.5 | · | D2: netto smussato 50,445 (migliore 50,445), 260 trade; annullato dall'ultimo terzo (36,411 -> 25,284) |

**Storia di ricerca**: 386 trade, netto 85,930, DD 30,528, average trade 223, UngerFit 0.73.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 386 su almeno 50 |
| average trade | **no** | 223 contro la soglia 309 |
| anni | passa | 5 anni, minimo 7 trade in un anno, 101.3 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 36,411 |
| ultimo terzo | passa | netto 36,411, net/DD 3.10 (serve 1,3) |
| due terzi su tre | passa | 30,920 / 18,599 / 36,411 |
| outlier | passa | trade migliore 17% del netto sulla storia, 18% sull'ultimo terzo |
| plateau | passa | media 0.66 su 7 vicini, minimo 0.03 |

**Prova su FTMO**: 195 trade, netto 10,152, DD 55,146, net/DD 0.18, average trade 52 (soglia 464), UngerFit 0.10, finestre in utile 2/4 (-16,304 / 33,698 / 27,360 / -39,767).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.18 (serve 1), average trade 52 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 56% del netto (massimo 40%); average trade / soglia per anno: 2020:5.1 2021:0.6 2022:0.8 2023:0.1 2024:0.3 2025:1.2 2026:-0.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=15, StartHour=-1, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, AtrMultiplierLong=0.5, Direction=2`

## MAC Incrocio di medie

129 simulazioni in 2.5 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | FastPeriod+SlowPeriod+Direction | FastPeriod=-, SlowPeriod=-, Direction=- | FastPeriod=5, SlowPeriod=100, Direction=2 | ✔ | avg smussato 792.1, netto 43,565, 55 trade |
| stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 45,423 (migliore 46,352), 55 trade; annullato dall'ultimo terzo (2,310 -> 1,974) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 792.1, netto 43,565, 55 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 2.0 | ✔ | batte lo spento: netto 43,998 contro 43,565, net/DD 3.68 contro 3.64 |
| affinamento stop | StopAtr | 0.8 | 1.0 | · | D2: netto smussato 45,855 (migliore 46,784), 55 trade; annullato dall'ultimo terzo (2,310 -> 1,974) |

**Storia di ricerca**: 84 trade, netto 46,307, DD 14,927, average trade 551, UngerFit 2.57.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 84 su almeno 50 |
| average trade | passa | 551 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 22.0 all'anno, 3 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,310 |
| ultimo terzo | **no** | netto 2,310, net/DD 0.21 (serve 1,3) |
| due terzi su tre | passa | 14,846 / 29,151 / 2,310 |
| outlier | **no** | trade migliore 20% del netto sulla storia, 192% sull'ultimo terzo |
| plateau | **no** | media 0.59 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 37 trade, netto -15,882, DD 29,020, net/DD -0.55, average trade -429 (soglia 464), UngerFit , finestre in utile 1/4 (-497 / -11,684 / -8,926 / 1,522).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.55 (serve 1), average trade -429 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 3/7 anni in utile (serve 60%), anno migliore 146% del netto (massimo 40%); average trade / soglia per anno: 2020:-4.4 2021:0.4 2022:7.5 2023:-0.4 2024:0.3 2025:-1.8 2026:-0.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=2.0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, FastPeriod=5, SlowPeriod=100, Direction=2`

## RBM Reversal Bollinger mirrored

477 simulazioni in 10.9 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 170.9, netto 70,772, 414 trade |
| finestra: inizio | StartHour | -1 | 3 | ✔ | avg smussato 192.9, netto 73,704, 382 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 264.8, netto 79,803, 306 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 73,628 (migliore 73,628), 306 trade; annullato dall'ultimo terzo (23,038 -> 17,100) |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 86,893 contro 79,803, net/DD 3.10 contro 2.84; annullato dall'ultimo terzo (23,038 -> 21,992) |
| YES | PtnNeutYes | 55 | 54 | · | batte lo spento: netto 84,023 contro 79,803, net/DD 5.65 contro 2.84; annullato dall'ultimo terzo (23,038 -> 5,289) |
| NO | PtnNeutNo | 56 | 53 | · | batte lo spento: netto 84,023 contro 79,803, net/DD 5.65 contro 2.84; annullato dall'ultimo terzo (23,038 -> 5,289) |
| direzionale YES | PtnDirYes | 52 | 28 | · | batte lo spento: netto 97,400 contro 79,803, net/DD 5.13 contro 2.84; annullato dall'ultimo terzo (23,038 -> -2,184) |
| direzionale NO | PtnDirNo | 53 | -14 | · | batte lo spento: netto 99,466 contro 79,803, net/DD 4.66 contro 2.84; annullato dall'ultimo terzo (23,038 -> -514) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 99,050 contro 79,803, net/DD 4.63 contro 2.84; annullato dall'ultimo terzo (23,038 -> 12,585) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 260.8, netto 79,803, 306 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.5 | ✔ | D2: netto smussato 70,541 (migliore 73,628), 306 trade |
| affinamento target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 76,250 contro 69,161, net/DD 2.45 contro 2.23; annullato dall'ultimo terzo (27,031 -> 25,985) |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 226.0, netto 69,161, 306 trade |

**Storia di ricerca**: 462 trade, netto 96,192, DD 31,081, average trade 208, UngerFit 0.67.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 462 su almeno 50 |
| average trade | **no** | 208 contro la soglia 309 |
| anni | passa | 5 anni, minimo 8 trade in un anno, 121.2 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 27,031 |
| ultimo terzo | **no** | netto 27,031, net/DD 1.13 (serve 1,3) |
| due terzi su tre | passa | 18,742 / 50,418 / 27,031 |
| outlier | passa | trade migliore 14% del netto sulla storia, 24% sull'ultimo terzo |
| plateau | **no** | media 0.34 su 5 vicini, minimo 0.00 |

**Prova su FTMO**: 216 trade, netto -27,385, DD 67,438, net/DD -0.41, average trade -127 (soglia 464), UngerFit , finestre in utile 1/4 (-15,144 / -11,353 / 13,959 / -14,846).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.41 (serve 1), average trade -127 (soglia 464), finestre 1/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 88% del netto (massimo 40%); average trade / soglia per anno: 2020:1.1 2021:0.4 2022:1.2 2023:0.0 2024:0.5 2025:-0.7 2026:0.2 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.5, TargetAtr=0, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=3, EndHour=9, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RBU Reversal Bollinger unmirrored

974 simulazioni in 18.1 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | BbLength+BbNumDevs | BbLength=-, BbNumDevs=- | BbLength=30, BbNumDevs=1.5 | ✔ | avg smussato 170.9, netto 70,772, 414 trade |
| finestra: inizio | StartHour | -1 | 3 | ✔ | avg smussato 192.9, netto 73,704, 382 trade |
| finestra: fine | EndHour | -1 | 9 | ✔ | avg smussato 264.8, netto 79,803, 306 trade |
| stop | StopAtr | 0.8 | 0.6 | · | D2: netto smussato 73,628 (migliore 73,628), 306 trade; annullato dall'ultimo terzo (23,038 -> 17,100) |
| target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 86,893 contro 79,803, net/DD 3.10 contro 2.84; annullato dall'ultimo terzo (23,038 -> 21,992) |
| YES long | FastYesLong | 152 | 3 | ✔ | batte lo spento: netto 101,231 contro 79,803, net/DD 3.42 contro 2.84 |
| YES short | FastYesShort | 152 | 78 | · | batte lo spento: netto 130,200 contro 101,231, net/DD 11.91 contro 3.42; annullato dall'ultimo terzo (24,036 -> 6,024) |
| NO long | FastNoLong | 153 | 137 | ✔ | batte lo spento: netto 105,742 contro 101,231, net/DD 4.07 contro 3.42 |
| NO short | FastNoShort | 153 | 63 | · | batte lo spento: netto 108,279 contro 105,742, net/DD 11.44 contro 4.07; annullato dall'ultimo terzo (25,341 -> 5,523) |
| calendario | SkipDay | -1 | 1 | · | batte lo spento: netto 113,656 contro 105,742, net/DD 8.79 contro 4.07; annullato dall'ultimo terzo (25,341 -> 16,794) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 470.0, netto 105,742, 225 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 97,237 (migliore 98,560), 225 trade |
| affinamento target | TargetAtr | 0 | 1.5 | · | batte lo spento: netto 102,010 contro 94,920, net/DD 3.85 contro 3.58; annullato dall'ultimo terzo (31,581 -> 30,534) |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 421.9, netto 94,920, 225 trade |

**Storia di ricerca**: 348 trade, netto 126,501, DD 26,528, average trade 364, UngerFit 1.27.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 348 su almeno 50 |
| average trade | passa | 364 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 91.3 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 31,581 |
| ultimo terzo | passa | netto 31,581, net/DD 2.18 (serve 1,3) |
| due terzi su tre | passa | 35,996 / 58,925 / 31,581 |
| outlier | passa | trade migliore 11% del netto sulla storia, 20% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 257 con i pattern, 110 senza |
| pattern casuali | **no** | 11 estrazioni su 37 fanno almeno altrettanto, p 0.32, Wilson 0.23 |
| plateau | passa | media 0.61 su 7 vicini, minimo 0.00 |

**Prova su FTMO**: 167 trade, netto 3,999, DD 41,507, net/DD 0.10, average trade 24 (soglia 464), UngerFit 0.05, finestre in utile 2/4 (-6,278 / 1,646 / -1,208 / 9,839).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 0.10 (serve 1), average trade 24 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 54% del netto (massimo 40%); average trade / soglia per anno: 2020:5.3 2021:1.0 2022:2.0 2023:0.4 2024:0.9 2025:-0.9 2026:0.8 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=3, EndHour=9, FastYesLong=3, FastYesShort=152, FastNoLong=137, FastNoShort=153, SkipDay=-1, BbLength=30, BbNumDevs=1.5`

## RHL Reversal sui livelli di ieri

666 simulazioni in 10.6 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelOffsetTicks+Direction | LevelOffsetTicks=-, Direction=- | LevelOffsetTicks=20, Direction=1 | ✔ | avg smussato 272.4, netto 68,375, 251 trade |
| finestra: inizio | StartHour | -1 | -1 | · | avg smussato 272.4, netto 68,375, 251 trade |
| finestra: fine | EndHour | -1 | 10 | ✔ | avg smussato 357.6, netto 79,385, 222 trade |
| stop | StopAtr | 0.8 | 1.0 | ✔ | D2: netto smussato 73,636 (migliore 73,636), 222 trade |
| target | TargetAtr | 0 | 1.0 | ✔ | batte lo spento: netto 82,469 contro 74,177, net/DD 1.83 contro 1.72 |
| YES | PtnNeutYes | 55 | 34 | ✔ | batte lo spento: netto 109,257 contro 82,469, net/DD 10.90 contro 1.83 |
| NO | PtnNeutNo | 56 | 45 | · | batte lo spento: netto 126,182 contro 109,257, net/DD 11.42 contro 10.90; annullato dall'ultimo terzo (11,034 -> 10,403) |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 2 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 53 | · | nessuno dei 3 candidati batte lo spento |
| calendario | SkipDay | -1 | 0 | · | batte lo spento: netto 101,973 contro 109,257, net/DD 11.24 contro 10.90; annullato dall'ultimo terzo (11,034 -> 4,672) |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 1,606.7, netto 109,257, 68 trade |
| uscita a ora fissa | ExitHour | -1 | 22 | ✔ | batte lo spento: netto 111,397 contro 109,257, net/DD 11.62 contro 10.90 |
| affinamento stop | StopAtr | 1.0 | 0.8 | · | D2: netto smussato 103,147 (migliore 107,795), 68 trade; annullato dall'ultimo terzo (11,089 -> 11,021) |
| affinamento target | TargetAtr | 1.0 | 1.0 | · | batte lo spento: netto 111,397 contro 103,289, net/DD 11.62 contro 10.77 |
| durata | MaxBars | 4 | 0 | ✔ | avg smussato 1,708.2, netto 116,155, 68 trade |

**Storia di ricerca**: 79 trade, netto 127,244, DD 9,587, average trade 1,611, UngerFit 9.36.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 79 su almeno 50 |
| average trade | passa | 1,611 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 2 trade in un anno, 20.7 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 11,089 |
| ultimo terzo | passa | netto 11,089, net/DD 3.38 (serve 1,3) |
| due terzi su tre | passa | 14,805 / 101,350 / 11,089 |
| outlier | **no** | trade migliore 13% del netto sulla storia, 49% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 1,008 con i pattern, -161 senza |
| pattern casuali | passa | 0 estrazioni su 30 fanno almeno altrettanto, p 0.03, Wilson 0.01 |
| plateau | passa | media 0.94 su 6 vicini, minimo 0.87 |

**Prova su FTMO**: 36 trade, netto 68,757, DD 17,820, net/DD 3.86, average trade 1,910 (soglia 464), UngerFit 6.64, finestre in utile 4/4 (7,305 / 30,123 / 15,655 / 19,444).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | passa | net/DD 3.86 (serve 1), average trade 1,910 (soglia 464), finestre 4/4 (servono 3) |
| anni | **no** | 6/7 anni in utile (serve 60%), anno migliore 44% del netto (massimo 40%); average trade / soglia per anno: 2020:-3.6 2021:2.2 2022:4.8 2023:8.1 2024:5.5 2025:6.2 2026:1.3 |
| altri mercati | passa | 6/7 con net/DD ≥ 1 (serve meta'): @NQ 2.71, @ES 4.27, @YM 4.75, @FESX 6.16, @FCE 1.10, @Z 1.73, @NIY 0.87 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=1.0, TargetAtr=1.0, MaxBars=0, IntradayOnly=1, ExitHour=22, StartHour=-1, EndHour=10, PtnNeutYes=34, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=-1, LevelOffsetTicks=20, Direction=1`

## LF Level fader sul pivot di ieri

503 simulazioni in 6.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=5 | ✔ | avg smussato 267.2, netto 32,063, 120 trade |
| stop | StopAtr | 0.8 | 0.6 | ✔ | D2: netto smussato 34,464 (migliore 34,464), 120 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 3 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 45 | ✔ | batte lo spento: netto 45,684 contro 35,511, net/DD 3.73 contro 2.07 |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| calendario long | NotEntryDayLong | -1 | 1 | · | batte lo spento: netto 49,830 contro 45,684, net/DD 4.07 contro 3.73; annullato dall'ultimo terzo (-396 -> -545) |
| calendario short | NotEntryDayShort | -1 | 4 | ✔ | batte lo spento: netto 47,454 contro 45,684, net/DD 3.88 contro 3.73 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 499.5, netto 47,454, 95 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessuno dei 2 candidati batte lo spento |
| affinamento stop | StopAtr | 0.6 | 0.6 | · | D2: netto smussato 47,283 (migliore 47,283), 95 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessuno dei 2 candidati batte lo spento |
| durata | MaxBars | 4 | 2 | · | avg smussato 555.8, netto 58,765, 96 trade; annullato dall'ultimo terzo (2,870 -> -10,358) |

**Storia di ricerca**: 147 trade, netto 50,324, DD 13,796, average trade 342, UngerFit 1.66.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 147 su almeno 50 |
| average trade | passa | 342 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 38.6 all'anno, 5 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 2,870 |
| ultimo terzo | **no** | netto 2,870, net/DD 0.30 (serve 1,3) |
| due terzi su tre | passa | 39,851 / 7,604 / 2,870 |
| outlier | **no** | trade migliore 23% del netto sulla storia, 164% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 55 con i pattern, -25 senza |
| pattern casuali | passa | 1 estrazioni su 8 fanno almeno altrettanto, p 0.22, Wilson 0.09 |
| plateau | passa | media 0.95 su 6 vicini, minimo 0.84 |

**Prova su FTMO**: 81 trade, netto -8,353, DD 42,274, net/DD -0.20, average trade -103 (soglia 464), UngerFit , finestre in utile 2/4 (-19,366 / -18,413 / 25,782 / 3,644).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.20 (serve 1), average trade -103 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 73% del netto (massimo 40%); average trade / soglia per anno: 2020:8.8 2021:2.7 2022:0.1 2023:0.6 2024:-0.3 2025:-0.6 2026:1.4 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.6, TargetAtr=0, MaxBars=4, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=1, PtnNeutYes=55, PtnNeutNo=45, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=4, LevelShift=5`

## LFHL Level fader sugli estremi di ieri

503 simulazioni in 6.8 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | LevelShift | LevelShift=- | LevelShift=10 | ✔ | avg smussato 464.7, netto 74,344, 160 trade |
| stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 72,869 (migliore 75,603), 160 trade |
| target | TargetAtr | 0 | 0 | · | nessuno dei 1 candidati batte lo spento |
| YES | PtnNeutYes | 55 | 33 | ✔ | batte lo spento: netto 71,279 contro 74,344, net/DD 7.20 contro 4.66 |
| NO | PtnNeutNo | 56 | 6 | · | batte lo spento: netto 57,601 contro 71,279, net/DD 8.80 contro 7.20; annullato dall'ultimo terzo (3,726 -> 626) |
| direzionale YES | PtnDirYes | 52 | -44 | · | batte lo spento: netto 47,989 contro 71,279, net/DD 7.60 contro 7.20; annullato dall'ultimo terzo (3,726 -> -3,958) |
| calendario long | NotEntryDayLong | -1 | -1 | · | nessuno dei 1 candidati batte lo spento |
| calendario short | NotEntryDayShort | -1 | 0 | ✔ | batte lo spento: netto 72,327 contro 71,279, net/DD 9.28 contro 7.20 |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 882.0, netto 72,327, 82 trade |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| affinamento stop | StopAtr | 0.8 | 0.8 | · | D2: netto smussato 70,078 (migliore 72,431), 82 trade |
| affinamento target | TargetAtr | 0 | 0 | · | nessun valore sopra lo spento |
| durata | MaxBars | 4 | 6 | ✔ | avg smussato 853.4, netto 67,962, 81 trade |

**Storia di ricerca**: 106 trade, netto 77,572, DD 9,519, average trade 732, UngerFit 4.27.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 106 su almeno 50 |
| average trade | passa | 732 contro la soglia 309 |
| anni | **no** | 5 anni, minimo 1 trade in un anno, 27.8 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 9,610 |
| ultimo terzo | passa | netto 9,610, net/DD 2.79 (serve 1,3) |
| due terzi su tre | passa | 25,938 / 42,024 / 9,610 |
| outlier | **no** | trade migliore 17% del netto sulla storia, 54% sull'ultimo terzo |
| pattern utile | passa | ultimo terzo: average trade 384 con i pattern, -101 senza |
| pattern casuali | passa | 3 estrazioni su 24 fanno almeno altrettanto, p 0.16, Wilson 0.09 |
| plateau | passa | media 0.85 su 6 vicini, minimo 0.42 |

**Prova su FTMO**: 70 trade, netto 43,535, DD 23,211, net/DD 1.88, average trade 622 (soglia 464), UngerFit 1.90, finestre in utile 2/4 (-2,228 / -14,704 / 16,892 / 43,576).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD 1.88 (serve 1), average trade 622 (soglia 464), finestre 2/4 (servono 3) |
| anni | **no** | 5/7 anni in utile (serve 60%), anno migliore 46% del netto (massimo 40%); average trade / soglia per anno: 2020:-0.9 2021:2.2 2022:2.8 2023:0.4 2024:0.9 2025:-0.6 2026:2.9 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.8, TargetAtr=0, MaxBars=6, IntradayOnly=1, ExitHour=-1, StartHour=-1, EndHour=-1, LevelChoice=2, PtnNeutYes=33, PtnNeutNo=56, PtnDirYes=52, NotEntryDayLong=-1, NotEntryDayShort=0, LevelShift=10`

## Riepilogo

- PCH: scartata; ricerca 997 trade netto 169,792 avg 170; prova 532 trade netto -41,635 net/DD -0.28 avg -78 fin 1/4; falliti: average trade, prova sul broker, anni
- TFM: scartata; ricerca 873 trade netto 80,563 avg 92; prova 465 trade netto -6,629 net/DD -0.05 avg -14 fin 2/4; falliti: average trade, prova sul broker, anni
- TFU: scartata; ricerca 873 trade netto 80,563 avg 92; prova 465 trade netto -6,629 net/DD -0.05 avg -14 fin 2/4; falliti: average trade, prova sul broker, anni
- BO: scartata; ricerca 917 trade netto 135,426 avg 148; prova 489 trade netto -12,549 net/DD -0.15 avg -26 fin 3/4; falliti: average trade, prova sul broker, anni
- BOS: scartata; ricerca 989 trade netto 145,027 avg 147; prova 532 trade netto 116,680 net/DD 1.55 avg 219 fin 3/4; falliti: average trade, prova sul broker
- VBO: scartata; ricerca 386 trade netto 85,930 avg 223; prova 195 trade netto 10,152 net/DD 0.18 avg 52 fin 2/4; falliti: average trade, prova sul broker, anni
- MAC: scartata; ricerca 84 trade netto 46,307 avg 551; prova 37 trade netto -15,882 net/DD -0.55 avg -429 fin 1/4; falliti: anni, ultimo terzo, outlier, plateau, prova sul broker, anni
- RBM: scartata; ricerca 462 trade netto 96,192 avg 208; prova 216 trade netto -27,385 net/DD -0.41 avg -127 fin 1/4; falliti: average trade, ultimo terzo, plateau, prova sul broker, anni
- RBU: scartata; ricerca 348 trade netto 126,501 avg 364; prova 167 trade netto 3,999 net/DD 0.10 avg 24 fin 2/4; falliti: anni, pattern casuali, prova sul broker, anni
- RHL: scartata; ricerca 79 trade netto 127,244 avg 1,611; prova 36 trade netto 68,757 net/DD 3.86 avg 1,910 fin 4/4; falliti: anni, outlier, anni
- LF: scartata; ricerca 147 trade netto 50,324 avg 342; prova 81 trade netto -8,353 net/DD -0.20 avg -103 fin 2/4; falliti: anni, ultimo terzo, outlier, prova sul broker, anni
- LFHL: scartata; ricerca 106 trade netto 77,572 avg 732; prova 70 trade netto 43,535 net/DD 1.88 avg 622 fin 2/4; falliti: anni, outlier, prova sul broker, anni
