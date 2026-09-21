# Sweep @FDAX 240m — PC

- Strategia di partenza: `PT2_FDAX_PCH_001_240`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana, FDAX 1.23 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: netto/drawdown, almeno 50 trade
- Beam: 2
- Durata: 91.3 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 235 | 0.5 | PT2_FDAX_PCH_001_240: 2787 trade, netto 629,703, DD 22,565, PF 1.32, 702 ms |
| volatilita' | 3 | 6 | 0.0 | PT2_FDAX_PCH_001_240: 2787 trade, netto 629,703, DD 22,565, PF 1.32, 428 ms |
| pattern neutrali | 3,025 | 5,368 | 9.5 | PT2_FDAX_PCH_001_240: 2667 trade, netto 619,687, DD 19,850, PF 1.33, 602 ms |
| pattern direzionali | 10,609 | 14,800 | 30.6 | PT2_FDAX_PCH_001_240: 2565 trade, netto 632,297, DD 18,366, PF 1.35, 704 ms |
| orari e giorni | 1,250 | 1,996 | 3.6 | PT2_FDAX_PCH_001_240: 2350 trade, netto 671,424, DD 17,767, PF 1.45, 739 ms |
| stop e target | 1,352 | 2,160 | 45.0 | PT2_FDAX_PCH_001_240: 1844 trade, netto 205,539, DD 43,847, PF 1.12, 8,072 ms |
| trailing e breakeven | 12 | 18 | 0.5 | PT2_FDAX_PCH_001_240: 1844 trade, netto 205,539, DD 43,847, PF 1.12, 8,416 ms |

**Ablation**: PtnDirYes: -47 → 52 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | **passa** | 1,913 | 216,830 | 4.83 | 1,327 | 153,781 | 2.49 | 52% | 4/4 |
| 2 | scartata | 1,844 | 205,539 | 4.69 | 1,285 | 138,948 | 2.31 | 49% | 3/4 |
| 3 | scartata | 2,756 | 68,908 | 1.74 | 1,891 | -64,397 |  |  | 1/4 |

**Finalista 1** — tenuta 52%, 4/4 finestre in utile

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 18
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -18
PtnDirYes = 52
PtnNeutNo = 52
PtnNeutYes = 44
SkipDay = -1
StartHour = 3
StopLoss = 5000
TakeProfit = 4500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 327 trade, 31,194
- 2023-03-07 → 2024-05-10: 307 trade, 34,139
- 2024-05-10 → 2025-07-14: 340 trade, 66,043
- 2025-07-14 → 2026-09-17: 353 trade, 22,406

**Finalista 2** — tenuta 49%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 18
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -18
PtnDirYes = -47
PtnNeutNo = 52
PtnNeutYes = 44
SkipDay = -1
StartHour = 3
StopLoss = 5000
TakeProfit = 4500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 306 trade, 32,227
- 2023-03-07 → 2024-05-10: 303 trade, 37,206
- 2024-05-10 → 2025-07-14: 334 trade, 73,584
- 2025-07-14 → 2026-09-17: 342 trade, -4,070

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 0
DvolMin = 0
EndHour = 18
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -18
PtnDirYes = -47
PtnNeutNo = 52
PtnNeutYes = 44
SkipDay = -1
StartHour = 3
StopLoss = 5000
TakeProfit = 4500
TrailingStop = 1000
```

- 2022-01-01 → 2023-03-07: 445 trade, 15,623
- 2023-03-07 → 2024-05-10: 489 trade, -29,745
- 2024-05-10 → 2025-07-14: 483 trade, -6,279
- 2025-07-14 → 2026-09-17: 474 trade, -43,995

## Esito: la finalista evidenziata sopravvive. Resta da verificarla in sessione con il cBot.