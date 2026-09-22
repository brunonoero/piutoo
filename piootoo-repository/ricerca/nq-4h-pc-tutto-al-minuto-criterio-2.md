# Sweep @NQ 240m — PC

- Strategia di partenza: `PT2_NQ_PCH_001_240`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **costante** per simbolo: NQ 1.45 pt
- Swap: **NQ** long 6.739 pt/notte, short 0.355 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 250 trade, 25 per tratto e 10 in perdita
- Beam: 2
- Durata: 343.9 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 88 | 4.0 | PT2_NQ_PCH_001_240: 260 trade, netto 27,047, DD 25,163, PF 1.11, 8,160 ms |
| volatilita' | 3 | 4 | 0.2 | PT2_NQ_PCH_001_240: 307 trade, netto 65,197, DD 28,044, PF 1.22, 5,915 ms |
| pattern neutrali | 3,025 | 262 | 86.5 | PT2_NQ_PCH_001_240: 257 trade, netto 85,557, DD 17,500, PF 1.37, 9,429 ms |
| pattern direzionali | 10,609 | 171 | 202.9 | PT2_NQ_PCH_001_240: 252 trade, netto 89,288, DD 15,423, PF 1.40, 10,196 ms |
| orari e giorni | 1,250 | 148 | 17.3 | PT2_NQ_PCH_001_240: 250 trade, netto 97,255, DD 15,423, PF 1.44, 3,772 ms |
| uscita di sessione | 9 | 18 | 0.2 | PT2_NQ_PCH_001_240: 250 trade, netto 97,255, DD 15,423, PF 1.44, 3,820 ms |
| stop e target | 1,352 | 1,078 | 30.6 | PT2_NQ_PCH_001_240: 250 trade, netto 97,255, DD 15,423, PF 1.44, 4,040 ms |
| trailing e breakeven | 12 | 24 | 0.3 | PT2_NQ_PCH_001_240: 250 trade, netto 97,255, DD 15,423, PF 1.44, 4,101 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 250 | 97,255 | 6.31 | 158 | -23,667 |  |  | 1/4 |
| 2 | scartata | 275 | 55,983 | 5.86 | 167 | 7,109 | 0.44 | 7% | 2/4 |
| 3 | scartata | 265 | 101,863 | 8.74 | 163 | 4,549 | 0.15 | 2% | 2/4 |
| 4 | scartata | 276 | 53,111 | 5.56 | 167 | 8,573 | 0.53 | 10% | 2/4 |
| 5 | scartata | 262 | 79,976 | 6.14 | 161 | 3,024 | 0.11 | 2% | 2/4 |
| 6 | scartata | 267 | 91,335 | 7.45 | 163 | 6,597 | 0.21 | 3% | 1/4 |
| 7 | scartata | 282 | 25,315 | 2.10 | 166 | 3,189 | 0.26 | 12% | 1/4 |
| 8 | scartata | 282 | 25,382 | 2.11 | 166 | 3,241 | 0.26 | 12% | 1/4 |
| 9 | scartata | 282 | 21,275 | 1.78 | 168 | 1,758 | 0.20 | 11% | 2/4 |
| 10 | scartata | 291 | 1,548 | 0.20 | 170 | 1,974 | 0.38 | 190% | 2/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 36 trade, -10,924
- 2025-06-02 → 2025-11-01: 41 trade, -16,545
- 2025-11-01 → 2026-04-02: 34 trade, -8,671
- 2026-04-02 → 2026-09-01: 47 trade, 12,473

**Finalista 2** — tenuta 7%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 37 trade, 4,577
- 2025-06-02 → 2025-11-01: 47 trade, -6,833
- 2025-11-01 → 2026-04-02: 35 trade, -2,155
- 2026-04-02 → 2026-09-01: 48 trade, 11,519

**Finalista 3** — tenuta 2%, sotto il minimo 50%

```
BreakEven = 1000
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 37 trade, 77
- 2025-06-02 → 2025-11-01: 44 trade, -12,987
- 2025-11-01 → 2026-04-02: 35 trade, -2,964
- 2026-04-02 → 2026-09-01: 47 trade, 20,423

**Finalista 4** — tenuta 10%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 37 trade, 4,174
- 2025-06-02 → 2025-11-01: 47 trade, -7,289
- 2025-11-01 → 2026-04-02: 35 trade, -556
- 2026-04-02 → 2026-09-01: 48 trade, 12,243

**Finalista 5** — tenuta 2%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 36 trade, 5,012
- 2025-06-02 → 2025-11-01: 43 trade, -12,907
- 2025-11-01 → 2026-04-02: 35 trade, -7,178
- 2026-04-02 → 2026-09-01: 47 trade, 18,097

**Finalista 6** — tenuta 3%, sotto il minimo 50%

```
BreakEven = 1000
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 37 trade, -648
- 2025-06-02 → 2025-11-01: 44 trade, -13,801
- 2025-11-01 → 2026-04-02: 35 trade, -2,539
- 2026-04-02 → 2026-09-01: 47 trade, 23,585

**Finalista 7** — tenuta 12%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 37 trade, -932
- 2025-06-02 → 2025-11-01: 46 trade, -675
- 2025-11-01 → 2026-04-02: 36 trade, -6,909
- 2026-04-02 → 2026-09-01: 47 trade, 11,705

**Finalista 8** — tenuta 12%, sotto il minimo 50%

```
BreakEven = 1000
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 37 trade, -932
- 2025-06-02 → 2025-11-01: 46 trade, -623
- 2025-11-01 → 2026-04-02: 36 trade, -6,909
- 2026-04-02 → 2026-09-01: 47 trade, 11,705

**Finalista 9** — tenuta 11%, sotto il minimo 50%

```
BreakEven = 500
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 37 trade, -562
- 2025-06-02 → 2025-11-01: 47 trade, 354
- 2025-11-01 → 2026-04-02: 36 trade, -5,098
- 2026-04-02 → 2026-09-01: 48 trade, 7,064

**Finalista 10** — solo 2 finestre su 4 in utile

```
BreakEven = 500
ChannelBars = 50
Direction = 0
DvolMin = 0
EndHour = 5
ExitHour = -1
IntradayOnly = 0
MaxBars = 24
OffsetTicks = 10
PtnDirNo = -40
PtnDirYes = 47
PtnNeutNo = 1
PtnNeutYes = 16
SkipDay = -1
StartHour = 10
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 500
```

- 2025-01-01 → 2025-06-02: 38 trade, 307
- 2025-06-02 → 2025-11-01: 48 trade, -536
- 2025-11-01 → 2026-04-02: 36 trade, -1,881
- 2026-04-02 → 2026-09-01: 48 trade, 4,084

## Esito: nessuna finalista sopravvive alla validazione fuori campione.