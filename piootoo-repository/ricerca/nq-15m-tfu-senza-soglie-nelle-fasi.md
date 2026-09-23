# Sweep @NQ 15m — TFU

- Strategia di partenza: `PT3B_NQ_TFU_001_15`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **per ora UTC** (NQ da 1.45 a 1.95 pt); costante di riserva: NQ 1.45 pt
- Swap: **NQ** long 6.739 pt/notte, short 0.355 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Tenuta: **overnight e overweek liberi** (parita' con il motore di ricerca)
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 250 trade, 25 per tratto e 10 in perdita
- Beam: 2 · **fasi pattern spezzate** (deviazione dal motore di ricerca: il pattern richiesto e quello vietato sono ottimizzati uno alla volta, quindi le coppie che rendono solo insieme non sono raggiungibili)
- Spazio: intero
- Durata: 166.0 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| uscita base | 2 | 2 | 0.2 | PT3B_NQ_TFU_001_15: 773 trade, netto -43,402, DD 68,531, PF 0.93, 14,698 ms |
| pattern long: PtnLyYes | 153 | 306 | 9.0 | PT3B_NQ_TFU_001_15: 644 trade, netto 73,597, DD 22,468, PF 1.17, 13,706 ms |
| pattern long: PtnLyNo | 152 | 304 | 8.7 | PT3B_NQ_TFU_001_15: 609 trade, netto 72,479, DD 20,391, PF 1.18, 13,665 ms |
| pattern short: PtnSyYes | 153 | 293 | 8.1 | PT3B_NQ_TFU_001_15: 439 trade, netto 94,643, DD 18,216, PF 1.34, 12,494 ms |
| pattern short: PtnSyNo | 152 | 293 | 7.9 | PT3B_NQ_TFU_001_15: 347 trade, netto 116,645, DD 12,234, PF 1.57, 12,493 ms |
| orari e giorni | 1,250 | 1,538 | 61.0 | PT3B_NQ_TFU_001_15: 346 trade, netto 119,647, DD 12,234, PF 1.58, 12,266 ms |
| uscita di sessione | 9 | 14 | 0.7 | PT3B_NQ_TFU_001_15: 346 trade, netto 119,647, DD 12,234, PF 1.58, 12,905 ms |
| stop e target | 1,352 | 2,704 | 67.6 | PT3B_NQ_TFU_001_15: 346 trade, netto 119,647, DD 12,234, PF 1.58, 11,708 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 346 | 119,647 | 9.78 | 220 | -31,155 |  |  | 1/4 |
| 2 | scartata | 346 | 119,647 | 9.78 | 220 | -31,155 |  |  | 1/4 |
| 3 | scartata | 342 | 116,706 | 9.93 | 207 | -30,531 |  |  | 1/4 |
| 4 | scartata | 342 | 116,706 | 9.93 | 207 | -30,531 |  |  | 1/4 |
| 5 | scartata | 346 | 119,647 | 9.78 | 220 | -31,155 |  |  | 1/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EndHour = 2
ExitHour = -1
IntradayOnly = 1
MaxBars = 368
PtnLyNo = 115
PtnLyYes = 33
PtnSyNo = 6
PtnSyYes = 25
SkipDay = -1
StartHour = 8
StopLoss = 1000
TakeProfit = 3000
```

- 2025-01-01 → 2025-06-02: 52 trade, -15,872
- 2025-06-02 → 2025-11-01: 59 trade, 6,521
- 2025-11-01 → 2026-04-02: 65 trade, -17,997
- 2026-04-02 → 2026-09-01: 44 trade, -3,808

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EndHour = 2
ExitHour = -1
IntradayOnly = 1
MaxBars = 644
PtnLyNo = 115
PtnLyYes = 33
PtnSyNo = 6
PtnSyYes = 25
SkipDay = -1
StartHour = 8
StopLoss = 1000
TakeProfit = 3000
```

- 2025-01-01 → 2025-06-02: 52 trade, -15,872
- 2025-06-02 → 2025-11-01: 59 trade, 6,521
- 2025-11-01 → 2026-04-02: 65 trade, -17,997
- 2026-04-02 → 2026-09-01: 44 trade, -3,808

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EndHour = 2
ExitHour = 22
IntradayOnly = 1
MaxBars = 920
PtnLyNo = 115
PtnLyYes = 33
PtnSyNo = 6
PtnSyYes = 25
SkipDay = -1
StartHour = 8
StopLoss = 1000
TakeProfit = 3000
```

- 2025-01-01 → 2025-06-02: 47 trade, -11,783
- 2025-06-02 → 2025-11-01: 56 trade, 3,266
- 2025-11-01 → 2026-04-02: 60 trade, -16,268
- 2026-04-02 → 2026-09-01: 44 trade, -5,747

**Finalista 4** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EndHour = 2
ExitHour = 22
IntradayOnly = 1
MaxBars = 0
PtnLyNo = 115
PtnLyYes = 33
PtnSyNo = 6
PtnSyYes = 25
SkipDay = -1
StartHour = 8
StopLoss = 1000
TakeProfit = 3000
```

- 2025-01-01 → 2025-06-02: 47 trade, -11,783
- 2025-06-02 → 2025-11-01: 56 trade, 3,266
- 2025-11-01 → 2026-04-02: 60 trade, -16,268
- 2026-04-02 → 2026-09-01: 44 trade, -5,747

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
EndHour = 2
ExitHour = -1
IntradayOnly = 1
MaxBars = 184
PtnLyNo = 115
PtnLyYes = 33
PtnSyNo = 6
PtnSyYes = 25
SkipDay = -1
StartHour = 8
StopLoss = 1000
TakeProfit = 3000
```

- 2025-01-01 → 2025-06-02: 52 trade, -15,872
- 2025-06-02 → 2025-11-01: 59 trade, 6,521
- 2025-11-01 → 2026-04-02: 65 trade, -17,997
- 2026-04-02 → 2026-09-01: 44 trade, -3,808

## Esito: nessuna finalista sopravvive alla validazione fuori campione.