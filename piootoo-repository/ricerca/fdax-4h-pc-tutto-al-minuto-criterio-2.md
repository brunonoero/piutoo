# Sweep @FDAX 240m — PC

- Strategia di partenza: `PT3B_FDAX_PCH_001_240`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **costante** per simbolo: FDAX 1.23 pt
- Swap: **FDAX** long 5.054 pt/notte, short 1.215 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 250 trade, 25 per tratto e 10 in perdita
- Beam: 2
- Durata: 361.5 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 52 | 3.7 | PT3B_FDAX_PCH_001_240: 775 trade, netto 38,576, DD 49,331, PF 1.06, 6,072 ms |
| volatilita' | 3 | 6 | 0.1 | PT3B_FDAX_PCH_001_240: 782 trade, netto 41,505, DD 48,813, PF 1.06, 3,918 ms |
| pattern neutrali | 3,025 | 1,736 | 75.4 | PT3B_FDAX_PCH_001_240: 281 trade, netto 103,378, DD 11,976, PF 1.51, 5,454 ms |
| pattern direzionali | 10,609 | 816 | 214.0 | PT3B_FDAX_PCH_001_240: 265 trade, netto 116,492, DD 11,976, PF 1.63, 3,313 ms |
| orari e giorni | 1,250 | 480 | 30.2 | PT3B_FDAX_PCH_001_240: 252 trade, netto 117,095, DD 11,058, PF 1.66, 5,724 ms |
| uscita di sessione | 9 | 18 | 0.4 | PT3B_FDAX_PCH_001_240: 260 trade, netto 115,474, DD 11,129, PF 1.62, 5,624 ms |
| stop e target | 1,352 | 2,456 | 34.7 | PT3B_FDAX_PCH_001_240: 260 trade, netto 113,074, DD 11,956, PF 1.53, 5,978 ms |
| trailing e breakeven | 12 | 18 | 0.4 | PT3B_FDAX_PCH_001_240: 260 trade, netto 113,074, DD 11,956, PF 1.53, 5,620 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 260 | 113,074 | 9.46 | 155 | 9,234 | 0.24 | 3% | 1/4 |
| 2 | scartata | 261 | 31,771 | 1.67 | 158 | -27,951 |  |  | 0/4 |
| 3 | scartata | 260 | 90,402 | 5.38 | 155 | -39,326 |  |  | 1/4 |
| 4 | scartata | 260 | 74,048 | 5.20 | 155 | -25,342 |  |  | 1/4 |
| 5 | scartata | 264 | 17,504 | 1.39 | 158 | -15,997 |  |  | 1/4 |
| 6 | scartata | 260 | 78,113 | 5.34 | 155 | -1,313 |  |  | 2/4 |
| 7 | scartata | 264 | 17,504 | 1.39 | 158 | -15,943 |  |  | 1/4 |
| 8 | scartata | 261 | 29,164 | 1.70 | 158 | -25,020 |  |  | 1/4 |
| 9 | scartata | 264 | 2,726 | 0.11 | 159 | -19,003 |  |  | 1/4 |

**Finalista 1** — tenuta 3%, sotto il minimo 50%

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 48 trade, 27,978
- 2025-06-02 → 2025-11-01: 30 trade, -10,676
- 2025-11-01 → 2026-04-02: 33 trade, -6,222
- 2026-04-02 → 2026-09-01: 44 trade, -1,846

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 50 trade, -479
- 2025-06-02 → 2025-11-01: 30 trade, -7,972
- 2025-11-01 → 2026-04-02: 33 trade, -8,019
- 2026-04-02 → 2026-09-01: 45 trade, -11,481

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 48 trade, 5,847
- 2025-06-02 → 2025-11-01: 30 trade, -20,900
- 2025-11-01 → 2026-04-02: 33 trade, -10,581
- 2026-04-02 → 2026-09-01: 44 trade, -13,692

**Finalista 4** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 48 trade, 3,197
- 2025-06-02 → 2025-11-01: 30 trade, -16,653
- 2025-11-01 → 2026-04-02: 33 trade, -10,555
- 2026-04-02 → 2026-09-01: 44 trade, -1,331

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 50 trade, 6,083
- 2025-06-02 → 2025-11-01: 30 trade, -5,111
- 2025-11-01 → 2026-04-02: 34 trade, -13,720
- 2026-04-02 → 2026-09-01: 44 trade, -3,249

**Finalista 6** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 48 trade, 20,100
- 2025-06-02 → 2025-11-01: 30 trade, -16,270
- 2025-11-01 → 2026-04-02: 33 trade, -5,172
- 2026-04-02 → 2026-09-01: 44 trade, 30

**Finalista 7** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 50 trade, 6,083
- 2025-06-02 → 2025-11-01: 30 trade, -5,111
- 2025-11-01 → 2026-04-02: 34 trade, -13,667
- 2026-04-02 → 2026-09-01: 44 trade, -3,249

**Finalista 8** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 50 trade, 1,227
- 2025-06-02 → 2025-11-01: 30 trade, -6,756
- 2025-11-01 → 2026-04-02: 33 trade, -8,931
- 2026-04-02 → 2026-09-01: 45 trade, -10,561

**Finalista 9** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 0
EndHour = 5
ExitHour = 22
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -22
PtnDirYes = 33
PtnNeutNo = 33
PtnNeutYes = 19
SkipDay = -1
StartHour = 19
StopLoss = 2250
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 50 trade, 3,319
- 2025-06-02 → 2025-11-01: 30 trade, -2,783
- 2025-11-01 → 2026-04-02: 34 trade, -11,264
- 2026-04-02 → 2026-09-01: 45 trade, -8,274

## Esito: nessuna finalista sopravvive alla validazione fuori campione.