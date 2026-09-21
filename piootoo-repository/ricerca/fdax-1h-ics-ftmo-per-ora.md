# Sweep @FDAX 60m — BIASW

- Strategia di partenza: `PT2_FDAX_BSW_001_60`
- Datafeed: ICS
- Spread: FTMOPLATFORM, mediana, FDAX 1.23 pt
- Commissione: $4 per contratto e per lato
- Campione di ricerca: 2014-07-17 → 2022-01-01
- Validazione: 2022-01-01 → 2026-09-17
- Obiettivo: netto/drawdown, almeno 50 trade
- Beam: 2 · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)
- Durata: 25.5 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| timing long: giorni | 30 | 17 | 0.2 | PT2_FDAX_BSW_001_60: 373 trade, netto 154,519, DD 36,103, PF 1.27, 2,348 ms |
| timing long: orari | 576 | 1,053 | 5.7 | PT2_FDAX_BSW_001_60: 372 trade, netto 210,695, DD 34,306, PF 1.34, 2,317 ms |
| timing short: giorni | 30 | 35 | 0.4 | PT2_FDAX_BSW_001_60: 373 trade, netto 236,304, DD 34,306, PF 1.39, 2,329 ms |
| timing short: orari | 576 | 1,028 | 5.7 | PT2_FDAX_BSW_001_60: 373 trade, netto 237,249, DD 34,306, PF 1.39, 2,339 ms |
| pattern long: PtnLyYes | 153 | 10 | 1.6 | PT2_FDAX_BSW_001_60: 373 trade, netto 237,406, DD 34,306, PF 1.39, 2,264 ms |
| pattern long: PtnLyNo | 152 | 32 | 1.5 | PT2_FDAX_BSW_001_60: 373 trade, netto 237,406, DD 34,306, PF 1.39, 2,491 ms |
| pattern short: PtnSyYes | 153 | 306 | 1.6 | PT2_FDAX_BSW_001_60: 372 trade, netto 243,168, DD 34,306, PF 1.40, 2,327 ms |
| pattern short: PtnSyNo | 152 | 304 | 1.5 | PT2_FDAX_BSW_001_60: 372 trade, netto 243,168, DD 34,306, PF 1.40, 2,445 ms |
| stop e target | 169 | 157 | 6.0 | PT2_FDAX_BSW_001_60: 516 trade, netto 155,338, DD 45,269, PF 1.27, 8,741 ms |

**Ablation**: PtnSyYes: 16 → 152 — pattern che non battono la propria sentinella, quindi tolti.

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 535 | 155,139 | 3.46 | 367 | 27,886 | 0.26 | 7% | 1/4 |
| 2 | scartata | 516 | 155,338 | 3.43 | 353 | -2,572 |  |  | 1/4 |
| 3 | scartata | 545 | 148,432 | 3.20 | 368 | -64,678 |  |  | 1/4 |

**Finalista 1** — tenuta 7%, sotto il minimo 50%

```
EntryDayLong = 0
EntryDayShort = 0
EntryTimeLong = 800
EntryTimeShort = 1800
ExitDayLong = 2
ExitDayShort = 0
ExitTimeLong = 1400
ExitTimeShort = 1500
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 153
PtnSyYes = 152
StopLoss = 1500
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 93 trade, -58,842
- 2023-03-07 → 2024-05-10: 82 trade, -3,049
- 2024-05-10 → 2025-07-14: 98 trade, -7,461
- 2025-07-14 → 2026-09-17: 93 trade, 98,745

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EntryDayLong = 0
EntryDayShort = 0
EntryTimeLong = 800
EntryTimeShort = 1800
ExitDayLong = 2
ExitDayShort = 0
ExitTimeLong = 1400
ExitTimeShort = 1500
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 153
PtnSyYes = 16
StopLoss = 1500
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 89 trade, -52,810
- 2023-03-07 → 2024-05-10: 80 trade, -32
- 2024-05-10 → 2025-07-14: 95 trade, -2,937
- 2025-07-14 → 2026-09-17: 88 trade, 54,715

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EntryDayLong = 0
EntryDayShort = 0
EntryTimeLong = 800
EntryTimeShort = 1800
ExitDayLong = 2
ExitDayShort = 0
ExitTimeLong = 1400
ExitTimeShort = 1500
PtnLyNo = 153
PtnLyYes = 152
PtnSyNo = 153
PtnSyYes = 16
StopLoss = 1250
TakeProfit = 0
```

- 2022-01-01 → 2023-03-07: 91 trade, -51,263
- 2023-03-07 → 2024-05-10: 85 trade, -23,327
- 2024-05-10 → 2025-07-14: 97 trade, -25,119
- 2025-07-14 → 2026-09-17: 94 trade, 36,289

## Esito: nessuna finalista sopravvive alla validazione fuori campione.