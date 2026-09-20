# Sweep @NQ 240m — PC

- Strategia di partenza: `PT2_NQ_PCH_001_240`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana, NQ 1.45 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: netto/drawdown, almeno 50 trade
- Durata: 92.9 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 160 | 0.5 | PT2_NQ_PCH_001_240: 222 trade, netto 119,208, DD 10,580, PF 1.65, 698 ms |
| volatilita' | 3 | 4 | 0.0 | PT2_NQ_PCH_001_240: 222 trade, netto 119,208, DD 10,580, PF 1.65, 467 ms |
| pattern neutrali | 3,025 | 2,942 | 9.8 | PT2_NQ_PCH_001_240: 190 trade, netto 131,757, DD 7,588, PF 1.90, 799 ms |
| pattern direzionali | 10,609 | 8,750 | 31.5 | PT2_NQ_PCH_001_240: 150 trade, netto 133,577, DD 6,056, PF 2.26, 740 ms |
| orari e giorni | 1,250 | 1,960 | 3.7 | PT2_NQ_PCH_001_240: 148 trade, netto 136,593, DD 6,056, PF 2.33, 701 ms |
| stop e target | 1,352 | 2,678 | 45.5 | PT2_NQ_PCH_001_240: 217 trade, netto 102,045, DD 5,317, PF 2.23, 7,519 ms |
| trailing e breakeven | 12 | 24 | 0.5 | PT2_NQ_PCH_001_240: 232 trade, netto 82,523, DD 3,281, PF 2.96, 7,703 ms |

**Ablation**: PtnNeutNo: 46 → 56 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 249 | 85,120 | 25.94 | 199 | 20,973 | 0.81 | 3% | 3/4 |
| 2 | scartata | 232 | 82,523 | 25.15 | 188 | 18,061 | 0.74 | 3% | 2/4 |
| 3 | scartata | 220 | 89,673 | 17.44 | 181 | 16,873 | 0.58 | 3% | 3/4 |

**Finalista 1** — tenuta 3%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 0
IntradayOnly = 0
MaxBars = 10
OffsetTicks = 2
PtnDirNo = 30
PtnDirYes = 52
PtnNeutNo = 56
PtnNeutYes = 32
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 30 trade, 2,760
- 2023-03-07 → 2024-05-10: 47 trade, 19,124
- 2024-05-10 → 2025-07-14: 65 trade, 3,980
- 2025-07-14 → 2026-09-17: 57 trade, -4,891

**Finalista 2** — tenuta 3%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 0
IntradayOnly = 0
MaxBars = 10
OffsetTicks = 2
PtnDirNo = 30
PtnDirYes = 52
PtnNeutNo = 46
PtnNeutYes = 32
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 25 trade, -1,700
- 2023-03-07 → 2024-05-10: 47 trade, 19,124
- 2024-05-10 → 2025-07-14: 61 trade, 7,012
- 2025-07-14 → 2026-09-17: 55 trade, -6,375

**Finalista 3** — tenuta 3%, sotto il minimo 50%

```
BreakEven = 1000
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 0
IntradayOnly = 0
MaxBars = 10
OffsetTicks = 2
PtnDirNo = 30
PtnDirYes = 52
PtnNeutNo = 46
PtnNeutYes = 32
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 24 trade, 7,308
- 2023-03-07 → 2024-05-10: 46 trade, 7,399
- 2024-05-10 → 2025-07-14: 60 trade, 2,520
- 2025-07-14 → 2026-09-17: 51 trade, -354

## Esito: nessuna finalista sopravvive alla validazione fuori campione.