# Sweep @FDAX 240m — PC

- Strategia di partenza: `PT2_FDAX_PCH_001_240`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana, FDAX 1.23 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: netto/drawdown, almeno 50 trade
- Beam: 2
- Durata: 82.6 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 238 | 0.5 | PT2_FDAX_PCH_001_240: 2778 trade, netto 674,369, DD 20,923, PF 1.34, 694 ms |
| volatilita' | 3 | 6 | 0.0 | PT2_FDAX_PCH_001_240: 2778 trade, netto 674,369, DD 20,923, PF 1.34, 443 ms |
| pattern neutrali | 3,025 | 5,408 | 9.5 | PT2_FDAX_PCH_001_240: 1713 trade, netto 537,059, DD 15,388, PF 1.44, 767 ms |
| pattern direzionali | 10,609 | 16,278 | 29.9 | PT2_FDAX_PCH_001_240: 1713 trade, netto 537,059, DD 15,388, PF 1.44, 682 ms |
| orari e giorni | 1,250 | 2,280 | 3.5 | PT2_FDAX_PCH_001_240: 1123 trade, netto 485,105, DD 12,981, PF 1.60, 684 ms |
| stop e target | 1,352 | 2,656 | 37.7 | PT2_FDAX_PCH_001_240: 1057 trade, netto 127,375, DD 18,915, PF 1.23, 6,327 ms |
| trailing e breakeven | 12 | 24 | 0.4 | PT2_FDAX_PCH_001_240: 1118 trade, netto 90,801, DD 11,041, PF 1.27, 6,172 ms |

**Ablation**: PtnNeutNo: 37 → 56 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | **passa** | 1,135 | 91,149 | 8.26 | 812 | 65,092 | 5.08 | 61% | 4/4 |
| 2 | **passa** | 1,118 | 90,801 | 8.22 | 810 | 66,608 | 5.19 | 63% | 4/4 |
| 3 | **passa** | 1,063 | 126,531 | 8.33 | 767 | 90,296 | 5.67 | 68% | 4/4 |

**Finalista 1** — tenuta 61%, 4/4 finestre in utile

```
BreakEven = 500
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 6
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 56
PtnNeutYes = 13
SkipDay = 4
StartHour = 0
StopLoss = 750
TakeProfit = 2500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 214 trade, 8,197
- 2023-03-07 → 2024-05-10: 184 trade, 11,763
- 2024-05-10 → 2025-07-14: 176 trade, 19,797
- 2025-07-14 → 2026-09-17: 238 trade, 25,335

**Finalista 2** — tenuta 63%, 4/4 finestre in utile

```
BreakEven = 500
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 6
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 37
PtnNeutYes = 13
SkipDay = 4
StartHour = 0
StopLoss = 750
TakeProfit = 2500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 213 trade, 8,955
- 2023-03-07 → 2024-05-10: 183 trade, 12,521
- 2024-05-10 → 2025-07-14: 176 trade, 19,797
- 2025-07-14 → 2026-09-17: 238 trade, 25,335

**Finalista 3** — tenuta 68%, 4/4 finestre in utile

```
BreakEven = 1000
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 6
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 37
PtnNeutYes = 13
SkipDay = 4
StartHour = 0
StopLoss = 750
TakeProfit = 2500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 201 trade, 16,793
- 2023-03-07 → 2024-05-10: 172 trade, 9,256
- 2024-05-10 → 2025-07-14: 165 trade, 44,860
- 2025-07-14 → 2026-09-17: 229 trade, 19,388

## Esito: la finalista evidenziata sopravvive. Resta da verificarla in sessione con il cBot.