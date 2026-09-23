# Sweep @FDAX 240m — PC

- Strategia di partenza: `PT3B_FDAX_PCH_001_240`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **per ora UTC** (FDAX da 1.13 a 4.00 pt); costante di riserva: FDAX 1.23 pt
- Swap: **FDAX** long 5.054 pt/notte, short 1.215 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Tenuta: **overnight e overweek liberi** (parita' con il motore di ricerca)
- Campione di ricerca: 2014-07-17 → 2021-01-01
- Validazione: 2021-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 50 trade, 25 per tratto e 10 in perdita, utile medio ≥ 150, profit factor ≥ 1.25
- Beam: 2
- Spazio: **regione fissata** — `ChannelBars = 20`, `Direction = 1`, `ExitHour = 21` (scelti da una misura precedente, non cercati)
- Durata: 334.3 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 8 | 0 | 0.2 | nessuna ammissibile |
| volatilita' | 3 | 0 | 0.1 | nessuna ammissibile |
| pattern neutrali | 3,025 | 377 | 42.8 | PT3B_FDAX_PCH_001_240: 177 trade, netto 40,772, DD 10,803, PF 1.39, 6,941 ms |
| pattern direzionali | 10,609 | 4,156 | 243.6 | PT3B_FDAX_PCH_001_240: 130 trade, netto 41,383, DD 6,386, PF 1.54, 5,593 ms |
| orari e giorni | 1,250 | 1,368 | 29.7 | PT3B_FDAX_PCH_001_240: 130 trade, netto 41,383, DD 6,386, PF 1.54, 7,430 ms |
| uscita di sessione | 1 | 2 | 0.2 | PT3B_FDAX_PCH_001_240: 130 trade, netto 41,383, DD 6,386, PF 1.54, 4,664 ms |
| stop e target | 1,352 | 1,000 | 15.8 | PT3B_FDAX_PCH_001_240: 130 trade, netto 58,254, DD 8,986, PF 1.86, 5,615 ms |
| trailing e breakeven | 12 | 18 | 0.3 | PT3B_FDAX_PCH_001_240: 130 trade, netto 58,254, DD 8,986, PF 1.86, 5,913 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 130 | 58,254 | 6.48 | 142 | 18,841 | 0.88 | 14% | 2/4 |
| 2 | scartata | 130 | 35,099 | 6.68 | 142 | -6,560 |  |  | 1/4 |
| 3 | **passa** | 130 | 47,113 | 5.71 | 142 | 26,938 | 2.90 | 51% | 3/4 |
| 4 | scartata | 130 | 35,127 | 6.69 | 142 | -6,560 |  |  | 1/4 |
| 5 | scartata | 130 | 33,411 | 6.69 | 142 | -4,919 |  |  | 1/4 |

**Finalista 1** — tenuta 14%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 20
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = 21
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -5
PtnDirYes = 52
PtnNeutNo = 32
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 1000
TakeProfit = 10000
TrailingStop = 0
```

- 2021-01-01 → 2022-06-02: 36 trade, -3,345
- 2022-06-02 → 2023-11-01: 30 trade, -10,643
- 2023-11-01 → 2025-04-01: 41 trade, 21,066
- 2025-04-01 → 2026-09-01: 35 trade, 11,764

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 20
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = 21
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -5
PtnDirYes = 52
PtnNeutNo = 32
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 1000
TakeProfit = 10000
TrailingStop = 1000
```

- 2021-01-01 → 2022-06-02: 36 trade, -965
- 2022-06-02 → 2023-11-01: 30 trade, 1,001
- 2023-11-01 → 2025-04-01: 41 trade, -930
- 2025-04-01 → 2026-09-01: 35 trade, -5,666

**Finalista 3** — tenuta 51%, 3/4 finestre in utile

```
BreakEven = 0
ChannelBars = 20
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = 21
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -5
PtnDirYes = 52
PtnNeutNo = 32
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 1000
TakeProfit = 10000
TrailingStop = 2000
```

- 2021-01-01 → 2022-06-02: 36 trade, -2,408
- 2022-06-02 → 2023-11-01: 30 trade, 177
- 2023-11-01 → 2025-04-01: 41 trade, 14,160
- 2025-04-01 → 2026-09-01: 35 trade, 15,009

**Finalista 4** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 20
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = 21
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -5
PtnDirYes = 52
PtnNeutNo = 32
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 1000
TakeProfit = 10000
TrailingStop = 1000
```

- 2021-01-01 → 2022-06-02: 36 trade, -965
- 2022-06-02 → 2023-11-01: 30 trade, 1,001
- 2023-11-01 → 2025-04-01: 41 trade, -930
- 2025-04-01 → 2026-09-01: 35 trade, -5,666

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 20
Direction = 1
DvolMin = 0
EndHour = -1
ExitHour = 21
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -5
PtnDirYes = 52
PtnNeutNo = 32
PtnNeutYes = 31
SkipDay = -1
StartHour = -1
StopLoss = 1000
TakeProfit = 10000
TrailingStop = 1000
```

- 2021-01-01 → 2022-06-02: 36 trade, -972
- 2022-06-02 → 2023-11-01: 30 trade, 1,019
- 2023-11-01 → 2025-04-01: 41 trade, -1,337
- 2025-04-01 → 2026-09-01: 35 trade, -3,629

## Esito: la finalista evidenziata sopravvive. Resta da verificarla in sessione con il cBot.