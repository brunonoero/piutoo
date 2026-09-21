# Sweep @NQ 30m — PC

- Strategia di partenza: `PT2_NQ_PCH_002_30`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana **per ora UTC** (NQ da 1.45 a 1.95 pt); costante di riserva: NQ 1.45 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 50 trade e 5 per tratto
- Beam: 2 · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)
- Durata: 140.7 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 124 | 3.6 | PT2_NQ_PCH_002_30: 792 trade, netto 180,025, DD 26,524, PF 1.24, 6,870 ms |
| volatilita' | 3 | 6 | 1.0 | PT2_NQ_PCH_002_30: 787 trade, netto 179,042, DD 22,024, PF 1.25, 5,146 ms |
| pattern neutrali: PtnNeutYes | 55 | 104 | 4.0 | PT2_NQ_PCH_002_30: 129 trade, netto 79,968, DD 6,152, PF 1.78, 6,411 ms |
| pattern neutrali: PtnNeutNo | 55 | 95 | 2.3 | PT2_NQ_PCH_002_30: 120 trade, netto 80,040, DD 6,080, PF 1.86, 6,942 ms |
| pattern direzionali: PtnDirYes | 103 | 65 | 3.8 | PT2_NQ_PCH_002_30: 103 trade, netto 69,676, DD 6,032, PF 1.87, 4,921 ms |
| pattern direzionali: PtnDirNo | 103 | 179 | 3.8 | PT2_NQ_PCH_002_30: 99 trade, netto 66,708, DD 6,032, PF 1.87, 6,608 ms |
| orari e giorni | 1,250 | 2,130 | 37.2 | PT2_NQ_PCH_002_30: 99 trade, netto 71,208, DD 6,032, PF 1.94, 6,925 ms |
| stop e target | 1,352 | 2,544 | 80.6 | PT2_NQ_PCH_002_30: 93 trade, netto 111,882, DD 9,048, PF 2.45, 14,216 ms |
| trailing e breakeven | 12 | 24 | 0.9 | PT2_NQ_PCH_002_30: 94 trade, netto 96,085, DD 9,048, PF 2.24, 14,204 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 94 | 96,085 | 10.62 | 64 | 29,488 | 1.39 | 13% | 3/4 |
| 2 | scartata | 103 | 68,354 | 8.97 | 65 | 57 | 0.01 | 0% | 2/4 |
| 3 | scartata | 104 | 60,043 | 7.88 | 65 | 11,496 | 1.44 | 18% | 2/4 |
| 4 | scartata | 109 | 27,548 | 2.95 | 65 | 9,574 | 1.13 | 38% | 2/4 |
| 5 | **passa** | 110 | 28,184 | 4.74 | 65 | 18,692 | 6.98 | 147% | 4/4 |

**Finalista 1** — tenuta 13%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 17
IntradayOnly = 0
MaxBars = 460
OffsetTicks = 5
PtnDirNo = -20
PtnDirYes = -49
PtnNeutNo = 1
PtnNeutYes = 45
SkipDay = -1
StartHour = 18
StopLoss = 1500
TakeProfit = 7500
TrailingStop = 0
```

- 2022-01-01 → 2023-03-07: 19 trade, 7,348
- 2023-03-07 → 2024-05-10: 12 trade, 17,904
- 2024-05-10 → 2025-07-14: 15 trade, -4,620
- 2025-07-14 → 2026-09-17: 17 trade, 1,364

**Finalista 2** — tenuta 0%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 17
IntradayOnly = 0
MaxBars = 460
OffsetTicks = 5
PtnDirNo = -20
PtnDirYes = -49
PtnNeutNo = 1
PtnNeutYes = 45
SkipDay = -1
StartHour = 18
StopLoss = 1500
TakeProfit = 10000
TrailingStop = 2000
```

- 2022-01-01 → 2023-03-07: 19 trade, -3,876
- 2023-03-07 → 2024-05-10: 13 trade, 3,788
- 2024-05-10 → 2025-07-14: 15 trade, 5,575
- 2025-07-14 → 2026-09-17: 18 trade, -5,430

**Finalista 3** — tenuta 18%, sotto il minimo 50%

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 17
IntradayOnly = 0
MaxBars = 460
OffsetTicks = 5
PtnDirNo = -20
PtnDirYes = -49
PtnNeutNo = 1
PtnNeutYes = 45
SkipDay = -1
StartHour = 18
StopLoss = 1500
TakeProfit = 7500
TrailingStop = 2000
```

- 2022-01-01 → 2023-03-07: 19 trade, -718
- 2023-03-07 → 2024-05-10: 13 trade, 7,321
- 2024-05-10 → 2025-07-14: 15 trade, 8,437
- 2025-07-14 → 2026-09-17: 18 trade, -3,544

**Finalista 4** — tenuta 38%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 17
IntradayOnly = 0
MaxBars = 460
OffsetTicks = 5
PtnDirNo = -20
PtnDirYes = -49
PtnNeutNo = 1
PtnNeutYes = 45
SkipDay = -1
StartHour = 18
StopLoss = 1500
TakeProfit = 7500
TrailingStop = 2000
```

- 2022-01-01 → 2023-03-07: 19 trade, -1,926
- 2023-03-07 → 2024-05-10: 13 trade, 8,237
- 2024-05-10 → 2025-07-14: 15 trade, 10,247
- 2025-07-14 → 2026-09-17: 18 trade, -6,984

**Finalista 5** — tenuta 147%, 4/4 finestre in utile

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 17
IntradayOnly = 0
MaxBars = 460
OffsetTicks = 5
PtnDirNo = -20
PtnDirYes = -49
PtnNeutNo = 1
PtnNeutYes = 45
SkipDay = -1
StartHour = 18
StopLoss = 1500
TakeProfit = 10000
TrailingStop = 1000
```

- 2022-01-01 → 2023-03-07: 19 trade, 2,180
- 2023-03-07 → 2024-05-10: 13 trade, 7,831
- 2024-05-10 → 2025-07-14: 15 trade, 5,528
- 2025-07-14 → 2026-09-17: 18 trade, 3,154

## Esito: la finalista evidenziata sopravvive. Resta da verificarla in sessione con il cBot.