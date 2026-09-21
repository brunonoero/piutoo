# Sweep @FDAX 60m — BIASW

- Strategia di partenza: `PT2_FDAX_BSW_001_60`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana **per ora UTC** (FDAX da 1.13 a 3.69 pt); costante di riserva: FDAX 1.23 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 50 trade e 5 per tratto
- Beam: 2 · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)
- Durata: 25.2 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| timing long: giorni | 30 | 17 | 0.2 | PT2_FDAX_BSW_001_60: 373 trade, netto 154,519, DD 36,103, PF 1.27, 2,314 ms |
| timing long: orari | 576 | 1,053 | 5.3 | PT2_FDAX_BSW_001_60: 371 trade, netto 204,184, DD 34,019, PF 1.33, 2,205 ms |
| timing short: giorni | 30 | 34 | 0.4 | PT2_FDAX_BSW_001_60: 384 trade, netto 199,403, DD 37,251, PF 1.32, 2,279 ms |
| timing short: orari | 576 | 1,152 | 5.2 | PT2_FDAX_BSW_001_60: 385 trade, netto 203,879, DD 37,251, PF 1.33, 2,055 ms |
| pattern long: PtnLyYes | 153 | 127 | 1.4 | PT2_FDAX_BSW_001_60: 385 trade, netto 203,879, DD 37,251, PF 1.33, 2,209 ms |
| pattern long: PtnLyNo | 152 | 252 | 1.4 | PT2_FDAX_BSW_001_60: 385 trade, netto 203,879, DD 37,251, PF 1.33, 2,313 ms |
| pattern short: PtnSyYes | 153 | 306 | 1.4 | PT2_FDAX_BSW_001_60: 382 trade, netto 205,241, DD 37,251, PF 1.33, 2,133 ms |
| pattern short: PtnSyNo | 152 | 304 | 1.4 | PT2_FDAX_BSW_001_60: 381 trade, netto 205,497, DD 37,251, PF 1.33, 2,148 ms |
| stop e target | 169 | 93 | 6.2 | PT2_FDAX_BSW_001_60: 563 trade, netto 55,728, DD 37,008, PF 1.09, 8,640 ms |

**Ablation**: PtnSyNo: 60 → 153 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 566 | 54,614 | 1.48 | 404 | 12,761 | 0.23 | 16% | 1/4 |
| 2 | scartata | 563 | 55,728 | 1.51 | 404 | 12,761 | 0.23 | 16% | 1/4 |
| 3 | scartata | 574 | 8,813 | 0.22 | 400 | 5,522 | 0.12 | 52% | 1/4 |
| 4 | scartata | 564 | 29,714 | 0.84 | 393 | 15,293 | 0.36 | 42% | 2/4 |
| 5 | scartata | 551 | 62,020 | 1.50 | 394 | 4,381 | 0.08 | 5% | 1/4 |
| 6 | scartata | 562 | 8,482 | 0.23 | 392 | 4,081 | 0.06 | 27% | 2/4 |

**Finalista 1** — tenuta 16%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1300
ExitTimeShort = 1000
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 153
PtnSyYes = 4
StopLoss = 2000
TakeProfit = 10000
```

- 2022-01-01 → 2023-03-07: 107 trade, -26,658
- 2023-03-07 → 2024-05-10: 85 trade, 60,821
- 2024-05-10 → 2025-07-14: 104 trade, -8,303
- 2025-07-14 → 2026-09-17: 107 trade, -11,092

**Finalista 2** — tenuta 16%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1300
ExitTimeShort = 1000
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 60
PtnSyYes = 4
StopLoss = 2000
TakeProfit = 10000
```

- 2022-01-01 → 2023-03-07: 107 trade, -26,658
- 2023-03-07 → 2024-05-10: 85 trade, 60,821
- 2024-05-10 → 2025-07-14: 104 trade, -8,303
- 2025-07-14 → 2026-09-17: 107 trade, -11,092

**Finalista 3** — solo 1 finestre su 4 in utile

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1300
ExitTimeShort = 1000
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 61
PtnSyYes = 4
StopLoss = 2000
TakeProfit = 6000
```

- 2022-01-01 → 2023-03-07: 111 trade, -1,892
- 2023-03-07 → 2024-05-10: 83 trade, 48,709
- 2024-05-10 → 2025-07-14: 102 trade, -7,001
- 2025-07-14 → 2026-09-17: 103 trade, -32,286

**Finalista 4** — tenuta 42%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1300
ExitTimeShort = 1000
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 61
PtnSyYes = 4
StopLoss = 2000
TakeProfit = 7500
```

- 2022-01-01 → 2023-03-07: 109 trade, 47
- 2023-03-07 → 2024-05-10: 80 trade, 46,357
- 2024-05-10 → 2025-07-14: 100 trade, -7,796
- 2025-07-14 → 2026-09-17: 103 trade, -21,307

**Finalista 5** — tenuta 5%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1300
ExitTimeShort = 1000
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 60
PtnSyYes = 4
StopLoss = 2250
TakeProfit = 10000
```

- 2022-01-01 → 2023-03-07: 101 trade, -4,411
- 2023-03-07 → 2024-05-10: 83 trade, 51,136
- 2024-05-10 → 2025-07-14: 102 trade, -17,473
- 2025-07-14 → 2026-09-17: 107 trade, -22,614

**Finalista 6** — tenuta 27%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1300
ExitTimeShort = 1000
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 61
PtnSyYes = 4
StopLoss = 2250
TakeProfit = 6000
```

- 2022-01-01 → 2023-03-07: 105 trade, 24,871
- 2023-03-07 → 2024-05-10: 83 trade, 41,783
- 2024-05-10 → 2025-07-14: 100 trade, -15,331
- 2025-07-14 → 2026-09-17: 103 trade, -44,985

## Esito: nessuna finalista sopravvive alla validazione fuori campione.