# Sweep @FDAX 60m — BIASW

- Strategia di partenza: `PT2_FDAX_BSW_001_60`
- Datafeed: ICS
- Spread: ICS, mediana **per ora UTC** (FDAX da 0.50 a 4.00 pt); costante di riserva: FDAX 0.50 pt
- Swap: **FDAX** long 5.054 pt/notte, short 1.215 pt/notte, rollover 21:00 UTC (ICS)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 50 trade e 5 per tratto
- Beam: 2 · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)
- Durata: 26.8 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| timing long: giorni | 30 | 3 | 0.2 | PT2_FDAX_BSW_001_60: 373 trade, netto 20,581, DD 119,188, PF 1.02, 2,377 ms |
| timing long: orari | 576 | 634 | 5.6 | PT2_FDAX_BSW_001_60: 372 trade, netto 120,947, DD 59,907, PF 1.17, 2,467 ms |
| timing short: giorni | 30 | 32 | 0.4 | PT2_FDAX_BSW_001_60: 384 trade, netto 116,642, DD 47,296, PF 1.17, 2,366 ms |
| timing short: orari | 576 | 1,152 | 5.6 | PT2_FDAX_BSW_001_60: 385 trade, netto 127,297, DD 43,336, PF 1.19, 2,296 ms |
| pattern long: PtnLyYes | 153 | 91 | 1.6 | PT2_FDAX_BSW_001_60: 383 trade, netto 144,351, DD 34,181, PF 1.30, 2,371 ms |
| pattern long: PtnLyNo | 152 | 213 | 1.5 | PT2_FDAX_BSW_001_60: 383 trade, netto 150,046, DD 34,338, PF 1.31, 2,396 ms |
| pattern short: PtnSyYes | 153 | 306 | 1.5 | PT2_FDAX_BSW_001_60: 221 trade, netto 183,277, DD 32,538, PF 1.55, 2,396 ms |
| pattern short: PtnSyNo | 152 | 304 | 1.5 | PT2_FDAX_BSW_001_60: 219 trade, netto 208,364, DD 32,652, PF 1.64, 2,275 ms |
| stop e target | 169 | 247 | 6.5 | PT2_FDAX_BSW_001_60: 237 trade, netto 101,539, DD 20,820, PF 1.41, 8,536 ms |

**Ablation**: PtnSyNo: 8 → 153 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 242 | 97,088 | 4.48 | 165 | -16,697 |  |  | 2/4 |
| 2 | scartata | 237 | 101,539 | 4.88 | 160 | -10,426 |  |  | 2/4 |
| 3 | scartata | 229 | 110,325 | 3.99 | 152 | -21,167 |  |  | 3/4 |
| 4 | scartata | 235 | 103,820 | 4.75 | 158 | 139 | 0.00 | 0% | 2/4 |
| 5 | scartata | 237 | 82,605 | 3.94 | 153 | -14,110 |  |  | 2/4 |
| 6 | scartata | 243 | 103,224 | 4.96 | 165 | -8,646 |  |  | 2/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 107
PtnLyYes = 152
PtnSyNo = 153
PtnSyYes = 77
StopLoss = 1750
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 39 trade, 11,452
- 2023-03-07 → 2024-05-10: 33 trade, 7,472
- 2024-05-10 → 2025-07-14: 46 trade, -2,701
- 2025-07-14 → 2026-09-17: 47 trade, -32,920

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 107
PtnLyYes = 152
PtnSyNo = 8
PtnSyYes = 77
StopLoss = 1750
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 39 trade, 11,452
- 2023-03-07 → 2024-05-10: 33 trade, 7,472
- 2024-05-10 → 2025-07-14: 44 trade, -1,856
- 2025-07-14 → 2026-09-17: 44 trade, -27,494

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 107
PtnLyYes = 152
PtnSyNo = 8
PtnSyYes = 77
StopLoss = 3000
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 38 trade, 7,831
- 2023-03-07 → 2024-05-10: 31 trade, 7,725
- 2024-05-10 → 2025-07-14: 41 trade, -37,147
- 2025-07-14 → 2026-09-17: 42 trade, 424

**Finalista 4** — tenuta 0%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 107
PtnLyYes = 152
PtnSyNo = 8
PtnSyYes = 77
StopLoss = 2000
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 39 trade, 11,554
- 2023-03-07 → 2024-05-10: 32 trade, 12,572
- 2024-05-10 → 2025-07-14: 43 trade, -8,052
- 2025-07-14 → 2026-09-17: 44 trade, -15,935

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 107
PtnLyYes = 152
PtnSyNo = 7
PtnSyYes = 77
StopLoss = 2500
TakeProfit = 4500
```

- 2022-01-01 → 2023-03-07: 39 trade, 18,777
- 2023-03-07 → 2024-05-10: 34 trade, 8,207
- 2024-05-10 → 2025-07-14: 38 trade, -29,187
- 2025-07-14 → 2026-09-17: 42 trade, -11,907

**Finalista 6** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 700
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 107
PtnLyYes = 152
PtnSyNo = 8
PtnSyYes = 77
StopLoss = 1750
TakeProfit = 10000
```

- 2022-01-01 → 2023-03-07: 41 trade, 14,941
- 2023-03-07 → 2024-05-10: 33 trade, 6,807
- 2024-05-10 → 2025-07-14: 46 trade, -2,668
- 2025-07-14 → 2026-09-17: 45 trade, -27,726

## Esito: nessuna finalista sopravvive alla validazione fuori campione.