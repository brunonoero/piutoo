# Sweep @GC 240m — PC

- Strategia di partenza: `PTS_GC_PCH_004_240`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana **costante** per simbolo: GC 0.45 pt
- Swap: **GC** long 0.5949 pt/notte, short 0 pt/notte, rollover 21:00 UTC (ICS)
- Commissione: $10.8 per contratto e per lato, cioe' $21.6 per trade
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 250 trade e 5 per tratto
- Beam: 2
- Durata: 214.0 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 30 | 1.7 | PTS_GC_PCH_004_240: 276 trade, netto 61,195, DD 27,472, PF 1.23, 2,793 ms |
| volatilita' | 3 | 2 | 0.1 | PTS_GC_PCH_004_240: 276 trade, netto 61,195, DD 27,472, PF 1.23, 2,599 ms |
| pattern neutrali | 3,025 | 100 | 14.1 | PTS_GC_PCH_004_240: 253 trade, netto 44,806, DD 25,651, PF 1.18, 2,874 ms |
| pattern direzionali | 10,609 | 43 | 133.9 | PTS_GC_PCH_004_240: 251 trade, netto 59,866, DD 26,326, PF 1.25, 3,727 ms |
| orari e giorni | 1,250 | 164 | 26.9 | PTS_GC_PCH_004_240: 251 trade, netto 59,866, DD 26,326, PF 1.25, 5,216 ms |
| uscita di sessione | 9 | 18 | 0.3 | PTS_GC_PCH_004_240: 251 trade, netto 59,866, DD 26,326, PF 1.25, 4,872 ms |
| stop e target | 1,352 | 972 | 35.0 | PTS_GC_PCH_004_240: 251 trade, netto 53,104, DD 24,449, PF 1.25, 5,592 ms |
| trailing e breakeven | 12 | 9 | 0.4 | PTS_GC_PCH_004_240: 250 trade, netto 53,026, DD 23,949, PF 1.25, 6,166 ms |

**Ablation**: PtnDirNo: -40 → 53 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 254 | 47,811 | 2.07 | 344 | 82,722 | 0.89 | 43% | 3/4 |
| 2 | scartata | 250 | 53,026 | 2.21 | 329 | 83,165 | 0.90 | 41% | 3/4 |
| 3 | **passa** | 285 | 51,866 | 2.20 | 386 | 115,029 | 1.94 | 88% | 4/4 |
| 4 | **passa** | 322 | 33,303 | 1.65 | 405 | 90,175 | 2.38 | 144% | 3/4 |
| 5 | **passa** | 353 | 8,701 | 0.25 | 404 | 96,609 | 2.37 | 966% | 4/4 |
| 6 | **passa** | 304 | 36,988 | 1.54 | 397 | 93,266 | 2.30 | 150% | 3/4 |

**Finalista 1** — tenuta 43%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = -1
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 10
PtnDirNo = 53
PtnDirYes = 52
PtnNeutNo = 52
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 2500
TakeProfit = 5000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 67 trade, 33,191
- 2025-06-02 → 2025-11-01: 72 trade, 33,630
- 2025-11-01 → 2026-04-02: 107 trade, 35,595
- 2026-04-02 → 2026-09-01: 97 trade, -16,328

**Finalista 2** — tenuta 41%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = -1
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 52
PtnNeutNo = 52
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 2500
TakeProfit = 5000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 67 trade, 25,750
- 2025-06-02 → 2025-11-01: 70 trade, 38,732
- 2025-11-01 → 2026-04-02: 102 trade, 33,203
- 2026-04-02 → 2026-09-01: 89 trade, -11,156

**Finalista 3** — tenuta 88%, 4/4 finestre in utile

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = -1
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 52
PtnNeutNo = 52
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 2500
TakeProfit = 5000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 84 trade, 44,658
- 2025-06-02 → 2025-11-01: 90 trade, 35,605
- 2025-11-01 → 2026-04-02: 113 trade, 22,321
- 2026-04-02 → 2026-09-01: 98 trade, 12,467

**Finalista 4** — tenuta 144%, 3/4 finestre in utile

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = -1
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 52
PtnNeutNo = 52
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 2500
TakeProfit = 5000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 95 trade, 38,315
- 2025-06-02 → 2025-11-01: 96 trade, 31,284
- 2025-11-01 → 2026-04-02: 113 trade, 22,756
- 2026-04-02 → 2026-09-01: 100 trade, -2,159

**Finalista 5** — tenuta 966%, 4/4 finestre in utile

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = -1
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 52
PtnNeutNo = 52
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 2500
TakeProfit = 4500
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 96 trade, 35,841
- 2025-06-02 → 2025-11-01: 93 trade, 29,590
- 2025-11-01 → 2026-04-02: 114 trade, 23,978
- 2026-04-02 → 2026-09-01: 100 trade, 7,221

**Finalista 6** — tenuta 150%, 3/4 finestre in utile

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = -1
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 52
PtnNeutNo = 52
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 2500
TakeProfit = 5000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 92 trade, 33,397
- 2025-06-02 → 2025-11-01: 92 trade, 37,948
- 2025-11-01 → 2026-04-02: 113 trade, 30,445
- 2026-04-02 → 2026-09-01: 100 trade, -10,921

## Esito: la finalista evidenziata sopravvive. Resta da verificarla in sessione con il cBot.