# Sweep @FDAX 240m — PC

- Strategia di partenza: `PT2_FDAX_PCH_001_240`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana **per ora UTC** (FDAX da 1.13 a 3.69 pt); costante di riserva: FDAX 1.23 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 50 trade e 5 per tratto
- Beam: 2
- Durata: 110.9 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 235 | 0.5 | PT2_FDAX_PCH_001_240: 2787 trade, netto 629,703, DD 22,565, PF 1.32, 580 ms |
| volatilita' | 3 | 6 | 0.0 | PT2_FDAX_PCH_001_240: 2787 trade, netto 629,703, DD 22,565, PF 1.32, 456 ms |
| pattern neutrali | 3,025 | 5,368 | 8.5 | PT2_FDAX_PCH_001_240: 2565 trade, netto 591,621, DD 22,565, PF 1.32, 1,009 ms |
| pattern direzionali | 10,609 | 15,159 | 42.1 | PT2_FDAX_PCH_001_240: 2318 trade, netto 550,600, DD 21,014, PF 1.33, 917 ms |
| orari e giorni | 1,250 | 2,060 | 4.6 | PT2_FDAX_PCH_001_240: 2318 trade, netto 550,600, DD 21,014, PF 1.33, 962 ms |
| stop e target | 1,352 | 1,520 | 52.4 | PT2_FDAX_PCH_001_240: 2062 trade, netto 108,087, DD 44,939, PF 1.06, 9,033 ms |
| trailing e breakeven | 12 | 8 | 0.6 | PT2_FDAX_PCH_001_240: 2062 trade, netto 108,087, DD 44,939, PF 1.06, 9,487 ms |

**Ablation**: PtnDirNo: 42 → 53 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 2,098 | 101,582 | 1.84 | 1,685 | -111,065 |  |  | 1/4 |
| 2 | scartata | 2,062 | 108,087 | 2.41 | 1,668 | -85,061 |  |  | 1/4 |
| 3 | scartata | 2,511 | 3,478 | 0.04 | 1,965 | -59,284 |  |  | 2/4 |
| 4 | scartata | 2,611 | 13,315 | 0.24 | 2,008 | -83,750 |  |  | 1/4 |
| 5 | scartata | 2,366 | 89,464 | 0.84 | 1,872 | -44,262 |  |  | 2/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 3000
EndHour = 23
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 53
PtnDirYes = 48
PtnNeutNo = 56
PtnNeutYes = 55
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 3000
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 439 trade, -31,810
- 2023-03-07 → 2024-05-10: 359 trade, -43,968
- 2024-05-10 → 2025-07-14: 421 trade, 77,724
- 2025-07-14 → 2026-09-17: 465 trade, -116,003

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 3000
EndHour = 23
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 42
PtnDirYes = 48
PtnNeutNo = 56
PtnNeutYes = 55
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 3000
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 432 trade, -13,517
- 2023-03-07 → 2024-05-10: 358 trade, -43,839
- 2024-05-10 → 2025-07-14: 418 trade, 90,816
- 2025-07-14 → 2026-09-17: 459 trade, -121,512

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 3000
EndHour = 23
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 42
PtnDirYes = 48
PtnNeutNo = 56
PtnNeutYes = 55
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 3000
TrailingStop = 2000
```

- 2022-01-01 → 2023-03-07: 499 trade, 41,470
- 2023-03-07 → 2024-05-10: 447 trade, -53,897
- 2024-05-10 → 2025-07-14: 480 trade, 44,984
- 2025-07-14 → 2026-09-17: 538 trade, -88,522

**Finalista 4** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 0
DvolMin = 3000
EndHour = 23
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 42
PtnDirYes = 48
PtnNeutNo = 56
PtnNeutYes = 55
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 3000
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 507 trade, 32,932
- 2023-03-07 → 2024-05-10: 466 trade, -27,363
- 2024-05-10 → 2025-07-14: 492 trade, -51,892
- 2025-07-14 → 2026-09-17: 543 trade, -42,378

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 0
DvolMin = 3000
EndHour = 23
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 42
PtnDirYes = 48
PtnNeutNo = 56
PtnNeutYes = 55
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 3000
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 481 trade, 18,978
- 2023-03-07 → 2024-05-10: 416 trade, -38,553
- 2024-05-10 → 2025-07-14: 456 trade, 34,612
- 2025-07-14 → 2026-09-17: 519 trade, -56,300

## Esito: nessuna finalista sopravvive alla validazione fuori campione.