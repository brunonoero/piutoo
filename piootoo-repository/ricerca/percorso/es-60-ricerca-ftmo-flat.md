# Percorso v4 — @ES 60m

- ricerca: feed FTMO 2020-11-09 → 2024-09-01 (22,540 barre); scelta sui primi 2/3, conferma sull'ultimo terzo
- prova sul broker: feed FTMO 2024-09-01 → 2026-09-01 (12,260 barre), mai vista dal percorso
- costi FTMO: spread 0.60 punti (mediana), swap long 1.6673 short 0.002 pt/notte; orologio al minuto
- tenuta: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover FTMO), come i piani PT3B
- soglia di average trade (15% del range medio della barra): 84 nella ricerca, 114 nella prova

## PCH Price Channel

550 simulazioni in 35.7 minuti, conferma dal 2023-05-26.

| passo | chiave | prima | dopo | esito | motivo |
|---|---|---|---|---|---|
| trigger | ChannelBars+OffsetTicks+Direction | ChannelBars=-, OffsetTicks=-, Direction=- | ChannelBars=40, OffsetTicks=2, Direction=1 | ✔ | avg smussato 182.2, netto 59,391, 326 trade |
| finestra: inizio | StartHour | -1 | 0 | ✔ | avg smussato 315.9, netto 59,391, 326 trade |
| finestra: fine | EndHour | -1 | 19 | · | avg smussato 189.4, netto 57,808, 308 trade; annullato dall'ultimo terzo (-3,043 -> -3,592) |
| stop | StopAtr | 0.8 | 0.3 | ✔ | D2: netto smussato 56,187 (migliore 56,187), 326 trade |
| intraday o overnight | IntradayOnly | 1 | 1 | · | avg smussato 172.4, netto 56,217, 326 trade |
| YES | PtnNeutYes | 55 | 55 | · | nessuno dei 3 candidati batte lo spento |
| NO | PtnNeutNo | 56 | 56 | · | nessuno dei 3 candidati batte lo spento |
| direzionale YES | PtnDirYes | 52 | 52 | · | nessuno dei 3 candidati batte lo spento |
| direzionale NO | PtnDirNo | 53 | 44 | · | batte lo spento: netto 50,555 contro 56,217, net/DD 14.94 contro 8.87; annullato dall'ultimo terzo (18,800 -> 2,672) |
| calendario | SkipDay | -1 | 0 | ✔ | batte lo spento: netto 60,471 contro 56,217, net/DD 10.56 contro 8.87 |
| uscita a ora fissa | ExitHour | -1 | -1 | · | nessun valore sopra lo spento |
| target | TargetAtr | 0 | 1.5 | ✔ | batte lo spento: netto 63,415 contro 60,471, net/DD 11.07 contro 10.56 |
| affinamento stop | StopAtr | 0.3 | 0.3 | · | D2: netto smussato 62,270 (migliore 62,270), 264 trade |

**Storia di ricerca**: 415 trade, netto 85,027, DD 6,959, average trade 205, UngerFit 2.68.

| cancello | esito | dettaglio |
|---|---|---|
| trade | passa | 415 su almeno 50 |
| average trade | passa | 205 contro la soglia 84 |
| anni | passa | 5 anni, minimo 9 trade in un anno, 108.9 all'anno, 4 in utile |
| utile recente | passa | trade usciti nell'ultimo terzo: 21,612 |
| ultimo terzo | passa | netto 21,612, net/DD 3.11 (serve 1,3) |
| due terzi su tre | passa | 14,792 / 48,624 / 21,612 |
| outlier | passa | trade migliore 6% del netto sulla storia, 18% sull'ultimo terzo |
| plateau | passa | media 0.85 su 7 vicini, minimo 0.60 |

**Prova su FTMO**: 228 trade, netto -33,554, DD 39,385, net/DD -0.85, average trade -147 (soglia 114), UngerFit , finestre in utile 0/4 (-5,517 / -5,932 / -13,343 / -8,761).

| controllo | esito | dettaglio |
|---|---|---|
| prova sul broker | **no** | net/DD -0.85 (serve 1), average trade -147 (soglia 114), finestre 0/4 (servono 3) |
| anni | **no** | 4/7 anni in utile (serve 60%), anno migliore 78% del netto (massimo 40%); average trade / soglia per anno: 2020:-1.2 2021:1.8 2022:3.4 2023:2.4 2024:1.0 2025:-1.5 2026:-1.3 |

Parametri: `StopLoss=0, TakeProfit=0, TrailingStop=0, BreakEven=0, StopAtr=0.3, TargetAtr=1.5, MaxBars=0, IntradayOnly=1, ExitHour=-1, StartHour=0, EndHour=-1, PtnNeutYes=55, PtnNeutNo=56, PtnDirYes=52, PtnDirNo=53, SkipDay=0, ChannelBars=40, OffsetTicks=2, Direction=1`

## Riepilogo

- PCH: scartata; ricerca 415 trade netto 85,027 avg 205; prova 228 trade netto -33,554 net/DD -0.85 avg -147 fin 0/4; falliti: prova sul broker, anni
