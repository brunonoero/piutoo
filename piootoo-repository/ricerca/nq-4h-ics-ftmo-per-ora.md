# Sweep @NQ 240m — PC

- Strategia di partenza: `PT2_NQ_PCH_001_240`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana, NQ 1.45 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: netto/drawdown, almeno 50 trade
- Beam: 2
- Durata: 92.4 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 159 | 0.5 | PT2_NQ_PCH_001_240: 223 trade, netto 117,712, DD 10,580, PF 1.64, 521 ms |
| volatilita' | 3 | 4 | 0.0 | PT2_NQ_PCH_001_240: 223 trade, netto 117,712, DD 10,580, PF 1.64, 429 ms |
| pattern neutrali | 3,025 | 2,959 | 9.5 | PT2_NQ_PCH_001_240: 177 trade, netto 119,871, DD 7,564, PF 1.87, 690 ms |
| pattern direzionali | 10,609 | 8,633 | 30.9 | PT2_NQ_PCH_001_240: 152 trade, netto 126,071, DD 6,056, PF 2.14, 738 ms |
| orari e giorni | 1,250 | 1,960 | 3.6 | PT2_NQ_PCH_001_240: 150 trade, netto 133,587, DD 6,056, PF 2.26, 707 ms |
| stop e target | 1,352 | 2,678 | 45.9 | PT2_NQ_PCH_001_240: 222 trade, netto 100,249, DD 5,265, PF 2.16, 7,555 ms |
| trailing e breakeven | 12 | 24 | 0.5 | PT2_NQ_PCH_001_240: 238 trade, netto 82,820, DD 3,262, PF 2.93, 7,825 ms |

**Ablation**: PtnNeutNo: 46 → 56 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 255 | 85,446 | 26.19 | 199 | 26,983 | 1.04 | 4% | 3/4 |
| 2 | scartata | 238 | 82,820 | 25.39 | 188 | 24,071 | 0.99 | 4% | 3/4 |
| 3 | scartata | 225 | 90,375 | 17.61 | 182 | 21,389 | 0.74 | 4% | 4/4 |

**Finalista 1** — tenuta 4%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 0
IntradayOnly = 0
MaxBars = 10
OffsetTicks = 0
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

- 2022-01-01 → 2023-03-07: 30 trade, 7,260
- 2023-03-07 → 2024-05-10: 47 trade, 19,124
- 2024-05-10 → 2025-07-14: 65 trade, 5,480
- 2025-07-14 → 2026-09-17: 57 trade, -4,881

**Finalista 2** — tenuta 4%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 0
IntradayOnly = 0
MaxBars = 10
OffsetTicks = 0
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

- 2022-01-01 → 2023-03-07: 25 trade, 2,800
- 2023-03-07 → 2024-05-10: 47 trade, 19,124
- 2024-05-10 → 2025-07-14: 61 trade, 8,512
- 2025-07-14 → 2026-09-17: 55 trade, -6,365

**Finalista 3** — tenuta 4%, sotto il minimo 50%

```
BreakEven = 1000
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 0
IntradayOnly = 0
MaxBars = 10
OffsetTicks = 0
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

- 2022-01-01 → 2023-03-07: 24 trade, 10,308
- 2023-03-07 → 2024-05-10: 46 trade, 7,409
- 2024-05-10 → 2025-07-14: 60 trade, 2,520
- 2025-07-14 → 2026-09-17: 52 trade, 1,152

## Esito: nessuna finalista sopravvive alla validazione fuori campione.