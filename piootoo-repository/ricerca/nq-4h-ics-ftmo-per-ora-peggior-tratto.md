# Sweep @NQ 240m — PC

- Strategia di partenza: `PT2_NQ_PCH_001_240`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana **per ora UTC** (NQ da 1.45 a 1.95 pt); costante di riserva: NQ 1.45 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 50 trade e 5 per tratto
- Beam: 2
- Durata: 87.2 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 159 | 0.5 | PT2_NQ_PCH_001_240: 346 trade, netto 143,703, DD 16,826, PF 1.48, 660 ms |
| volatilita' | 3 | 4 | 0.0 | PT2_NQ_PCH_001_240: 345 trade, netto 127,161, DD 16,851, PF 1.42, 401 ms |
| pattern neutrali | 3,025 | 3,857 | 10.5 | PT2_NQ_PCH_001_240: 160 trade, netto 91,720, DD 9,072, PF 1.71, 734 ms |
| pattern direzionali | 10,609 | 7,547 | 29.4 | PT2_NQ_PCH_001_240: 91 trade, netto 69,772, DD 4,524, PF 2.03, 700 ms |
| orari e giorni | 1,250 | 1,584 | 3.5 | PT2_NQ_PCH_001_240: 68 trade, netto 68,456, DD 3,040, PF 2.51, 674 ms |
| stop e target | 1,352 | 2,632 | 41.1 | PT2_NQ_PCH_001_240: 65 trade, netto 74,661, DD 4,040, PF 3.18, 7,153 ms |
| trailing e breakeven | 12 | 24 | 0.4 | PT2_NQ_PCH_001_240: 65 trade, netto 74,661, DD 4,040, PF 3.18, 6,976 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 65 | 74,661 | 18.48 | 52 | -46,416 |  |  | 0/4 |
| 2 | scartata | 74 | 45,760 | 7.56 | 55 | 560 | 0.09 | 1% | 1/4 |
| 3 | scartata | 71 | 53,978 | 11.73 | 55 | -26,940 |  |  | 0/4 |
| 4 | scartata | 72 | 54,157 | 10.94 | 54 | 3,607 | 0.55 | 5% | 3/4 |
| 5 | scartata | 75 | 34,660 | 5.03 | 55 | 6,412 | 1.10 | 22% | 3/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 15
Direction = 1
DvolMin = 0
EndHour = 13
IntradayOnly = 0
MaxBars = 50
OffsetTicks = 10
PtnDirNo = 11
PtnDirYes = -2
PtnNeutNo = 7
PtnNeutYes = 22
SkipDay = -1
StartHour = 22
StopLoss = 3000
TakeProfit = 2500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 10 trade, -19,080
- 2023-03-07 → 2024-05-10: 13 trade, -11,604
- 2024-05-10 → 2025-07-14: 15 trade, -1,120
- 2025-07-14 → 2026-09-17: 14 trade, -14,612

**Finalista 2** — tenuta 1%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 15
Direction = 1
DvolMin = 0
EndHour = 13
IntradayOnly = 0
MaxBars = 48
OffsetTicks = 10
PtnDirNo = 11
PtnDirYes = -2
PtnNeutNo = 7
PtnNeutYes = 22
SkipDay = -1
StartHour = 22
StopLoss = 3000
TakeProfit = 2500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 10 trade, -580
- 2023-03-07 → 2024-05-10: 13 trade, -1,104
- 2024-05-10 → 2025-07-14: 17 trade, 3,364
- 2025-07-14 → 2026-09-17: 15 trade, -1,120

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 15
Direction = 1
DvolMin = 0
EndHour = 13
IntradayOnly = 0
MaxBars = 48
OffsetTicks = 10
PtnDirNo = 11
PtnDirYes = -2
PtnNeutNo = 7
PtnNeutYes = 22
SkipDay = -1
StartHour = 22
StopLoss = 3000
TakeProfit = 2500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 10 trade, -12,580
- 2023-03-07 → 2024-05-10: 13 trade, -7,104
- 2024-05-10 → 2025-07-14: 17 trade, -136
- 2025-07-14 → 2026-09-17: 15 trade, -7,120

**Finalista 4** — tenuta 5%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 15
Direction = 1
DvolMin = 0
EndHour = 13
IntradayOnly = 0
MaxBars = 48
OffsetTicks = 10
PtnDirNo = 11
PtnDirYes = -2
PtnNeutNo = 7
PtnNeutYes = 22
SkipDay = -1
StartHour = 22
StopLoss = 3000
TakeProfit = 2500
TrailingStop = 2000
```

- 2022-01-01 → 2023-03-07: 10 trade, -2,408
- 2023-03-07 → 2024-05-10: 13 trade, 2,244
- 2024-05-10 → 2025-07-14: 17 trade, 3,302
- 2025-07-14 → 2026-09-17: 14 trade, 469

**Finalista 5** — tenuta 22%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 15
Direction = 1
DvolMin = 0
EndHour = 13
IntradayOnly = 0
MaxBars = 48
OffsetTicks = 10
PtnDirNo = 11
PtnDirYes = -2
PtnNeutNo = 7
PtnNeutYes = 22
SkipDay = -1
StartHour = 22
StopLoss = 3000
TakeProfit = 2500
TrailingStop = 2000
```

- 2022-01-01 → 2023-03-07: 10 trade, 1,096
- 2023-03-07 → 2024-05-10: 13 trade, 1,242
- 2024-05-10 → 2025-07-14: 17 trade, 4,685
- 2025-07-14 → 2026-09-17: 15 trade, -611

## Esito: nessuna finalista sopravvive alla validazione fuori campione.