# Sweep @FDAX 240m — PC

- Strategia di partenza: `PT3B_FDAX_PCH_001_240`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **costante** per simbolo: FDAX 1.23 pt
- Swap: **FDAX** long 5.054 pt/notte, short 1.215 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 30 trade e 5 per tratto
- Beam: 2
- Durata: 36.9 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 201 | 0.2 | PT3B_FDAX_PCH_001_240: 1179 trade, netto 279,603, DD 27,986, PF 1.30, 1,339 ms |
| volatilita' | 3 | 6 | 0.0 | PT3B_FDAX_PCH_001_240: 1179 trade, netto 279,603, DD 27,986, PF 1.30, 198 ms |
| pattern neutrali | 3,025 | 4,653 | 4.4 | PT3B_FDAX_PCH_001_240: 783 trade, netto 230,677, DD 16,797, PF 1.38, 282 ms |
| pattern direzionali | 10,609 | 10,752 | 13.7 | PT3B_FDAX_PCH_001_240: 783 trade, netto 230,677, DD 16,797, PF 1.38, 290 ms |
| orari e giorni | 1,250 | 2,264 | 1.8 | PT3B_FDAX_PCH_001_240: 570 trade, netto 293,501, DD 14,498, PF 1.76, 280 ms |
| uscita di sessione | 9 | 18 | 0.2 | PT3B_FDAX_PCH_001_240: 544 trade, netto 45,691, DD 34,897, PF 1.12, 3,784 ms |
| stop e target | 1,352 | 2,336 | 15.8 | PT3B_FDAX_PCH_001_240: 523 trade, netto 95,372, DD 23,591, PF 1.26, 3,802 ms |
| trailing e breakeven | 12 | 12 | 0.2 | PT3B_FDAX_PCH_001_240: 582 trade, netto 55,873, DD 19,410, PF 1.27, 2,614 ms |

**Ablation**: PtnNeutNo: 11 → 56 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 847 | 54,837 | 2.62 | 495 | -60,902 |  |  | 1/4 |
| 2 | scartata | 582 | 55,873 | 2.88 | 346 | -55,725 |  |  | 0/4 |
| 3 | scartata | 552 | 65,786 | 3.12 | 337 | -25,563 |  |  | 2/4 |
| 4 | scartata | 523 | 95,372 | 4.04 | 307 | -60,260 |  |  | 1/4 |
| 5 | scartata | 551 | 71,516 | 2.59 | 339 | -40,552 |  |  | 0/4 |
| 6 | scartata | 584 | 41,083 | 2.29 | 348 | -46,629 |  |  | 0/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 10
ExitHour = 15
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 2
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 56
PtnNeutYes = 4
SkipDay = -1
StartHour = 3
StopLoss = 1750
TakeProfit = 5000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 112 trade, 5,969
- 2025-06-02 → 2025-11-01: 127 trade, -19,259
- 2025-11-01 → 2026-04-02: 131 trade, -32,254
- 2026-04-02 → 2026-09-01: 125 trade, -15,358

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 10
ExitHour = 15
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 2
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 11
PtnNeutYes = 4
SkipDay = -1
StartHour = 3
StopLoss = 1750
TakeProfit = 5000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 93 trade, -8,432
- 2025-06-02 → 2025-11-01: 76 trade, -17,889
- 2025-11-01 → 2026-04-02: 86 trade, -13,391
- 2026-04-02 → 2026-09-01: 91 trade, -16,013

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 10
ExitHour = 15
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 2
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 11
PtnNeutYes = 4
SkipDay = -1
StartHour = 3
StopLoss = 1750
TakeProfit = 5000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 92 trade, 993
- 2025-06-02 → 2025-11-01: 73 trade, -3,890
- 2025-11-01 → 2026-04-02: 83 trade, 1,768
- 2026-04-02 → 2026-09-01: 89 trade, -24,434

**Finalista 4** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 10
ExitHour = 15
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 2
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 11
PtnNeutYes = 4
SkipDay = -1
StartHour = 3
StopLoss = 1750
TakeProfit = 5000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 84 trade, -11,564
- 2025-06-02 → 2025-11-01: 67 trade, -5,328
- 2025-11-01 → 2026-04-02: 74 trade, 3,345
- 2026-04-02 → 2026-09-01: 82 trade, -46,712

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 10
ExitHour = 15
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 2
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 11
PtnNeutYes = 4
SkipDay = -1
StartHour = 3
StopLoss = 1750
TakeProfit = 5000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 92 trade, -20,029
- 2025-06-02 → 2025-11-01: 74 trade, -3,435
- 2025-11-01 → 2026-04-02: 83 trade, -374
- 2026-04-02 → 2026-09-01: 90 trade, -16,714

**Finalista 6** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 10
ExitHour = 15
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 2
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 11
PtnNeutYes = 4
SkipDay = -1
StartHour = 3
StopLoss = 1750
TakeProfit = 5000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 93 trade, -9,338
- 2025-06-02 → 2025-11-01: 76 trade, -20,096
- 2025-11-01 → 2026-04-02: 87 trade, -7,265
- 2026-04-02 → 2026-09-01: 92 trade, -9,931

## Esito: nessuna finalista sopravvive alla validazione fuori campione.