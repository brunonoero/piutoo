# Sweep @FDAX 60m — BIASW

- Strategia di partenza: `PT2_FDAX_BSW_001_60`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana, FDAX 1.23 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: netto/drawdown, almeno 50 trade
- Beam: 2 · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)
- Durata: 25.0 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| timing long: giorni | 30 | 17 | 0.2 | PT2_FDAX_BSW_001_60: 373 trade, netto 154,132, DD 36,138, PF 1.27, 2,386 ms |
| timing long: orari | 576 | 1,056 | 5.6 | PT2_FDAX_BSW_001_60: 371 trade, netto 221,523, DD 33,946, PF 1.36, 2,272 ms |
| timing short: giorni | 30 | 34 | 0.4 | PT2_FDAX_BSW_001_60: 384 trade, netto 230,804, DD 33,946, PF 1.37, 2,374 ms |
| timing short: orari | 576 | 1,152 | 5.6 | PT2_FDAX_BSW_001_60: 385 trade, netto 242,256, DD 33,946, PF 1.39, 2,261 ms |
| pattern long: PtnLyYes | 153 | 129 | 1.5 | PT2_FDAX_BSW_001_60: 385 trade, netto 325,318, DD 30,229, PF 1.59, 2,342 ms |
| pattern long: PtnLyNo | 152 | 262 | 1.5 | PT2_FDAX_BSW_001_60: 384 trade, netto 339,984, DD 16,155, PF 1.79, 2,294 ms |
| pattern short: PtnSyYes | 153 | 306 | 1.5 | PT2_FDAX_BSW_001_60: 289 trade, netto 368,495, DD 13,854, PF 2.10, 2,355 ms |
| pattern short: PtnSyNo | 152 | 304 | 1.5 | PT2_FDAX_BSW_001_60: 285 trade, netto 378,582, DD 13,854, PF 2.17, 2,281 ms |
| stop e target | 169 | 325 | 5.8 | PT2_FDAX_BSW_001_60: 314 trade, netto 262,016, DD 16,277, PF 1.87, 8,149 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 314 | 262,016 | 16.10 | 217 | 40,522 | 1.14 | 7% | 2/4 |
| 2 | scartata | 310 | 272,146 | 15.46 | 215 | 28,537 | 0.71 | 5% | 2/4 |

**Finalista 1** — tenuta 7%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 400
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 19
PtnLyYes = 143
PtnSyNo = 88
PtnSyYes = 100
StopLoss = 2000
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 49 trade, 41,291
- 2023-03-07 → 2024-05-10: 52 trade, 17,521
- 2024-05-10 → 2025-07-14: 55 trade, -4,328
- 2025-07-14 → 2026-09-17: 61 trade, -13,963

**Finalista 2** — tenuta 5%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 1
EntryTimeLong = 400
EntryTimeShort = 2200
ExitDayLong = 2
ExitDayShort = 2
ExitTimeLong = 1400
ExitTimeShort = 1400
PtnLyNo = 19
PtnLyYes = 143
PtnSyNo = 88
PtnSyYes = 100
StopLoss = 2250
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 49 trade, 44,734
- 2023-03-07 → 2024-05-10: 50 trade, 17,402
- 2024-05-10 → 2025-07-14: 55 trade, -13,828
- 2025-07-14 → 2026-09-17: 61 trade, -19,771

## Esito: nessuna finalista sopravvive alla validazione fuori campione.