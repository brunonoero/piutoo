# Sweep @NQ 240m — PC

- Strategia di partenza: `PT2_NQ_PCH_001_240`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **costante** per simbolo: NQ 1.45 pt
- Swap: **NQ** long 6.739 pt/notte, short 0.355 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 30 trade e 5 per tratto
- Beam: 2
- Durata: 177.1 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 169 | 2.2 | PT2_NQ_PCH_001_240: 179 trade, netto 23,833, DD 19,448, PF 1.14, 3,768 ms |
| volatilita' | 3 | 4 | 0.1 | PT2_NQ_PCH_001_240: 179 trade, netto 23,833, DD 19,448, PF 1.14, 3,293 ms |
| pattern neutrali | 3,025 | 2,294 | 36.3 | PT2_NQ_PCH_001_240: 48 trade, netto 46,224, DD 4,615, PF 2.38, 2,771 ms |
| pattern direzionali | 10,609 | 3,282 | 108.2 | PT2_NQ_PCH_001_240: 33 trade, netto 40,227, DD 2,212, PF 3.07, 2,658 ms |
| orari e giorni | 1,250 | 524 | 13.3 | PT2_NQ_PCH_001_240: 33 trade, netto 40,227, DD 2,212, PF 3.07, 2,691 ms |
| uscita di sessione | 9 | 18 | 0.2 | PT2_NQ_PCH_001_240: 33 trade, netto 40,227, DD 2,212, PF 3.07, 2,630 ms |
| stop e target | 1,352 | 2,474 | 15.6 | PT2_NQ_PCH_001_240: 42 trade, netto 19,115, DD 0, PF -, 2,542 ms |
| trailing e breakeven | 12 | 24 | 0.2 | PT2_NQ_PCH_001_240: 42 trade, netto 19,115, DD 0, PF -, 2,820 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 42 | 19,115 | 19,115.12 | 33 | -2,808 |  |  | 2/4 |
| 2 | scartata | 42 | 18,615 | 484.01 | 33 | -3,808 |  |  | 2/4 |
| 3 | scartata | 42 | 19,115 | 19,115.12 | 33 | -2,808 |  |  | 2/4 |
| 4 | scartata | 42 | 16,567 | 9.62 | 33 | -4,383 |  |  | 1/4 |
| 5 | scartata | 42 | 17,067 | 9.91 | 33 | -3,883 |  |  | 1/4 |
| 6 | scartata | 42 | 17,067 | 9.91 | 33 | -3,883 |  |  | 1/4 |
| 7 | scartata | 42 | 5,633 | 1.82 | 33 | -1,160 |  |  | 2/4 |
| 8 | scartata | 42 | 5,633 | 1.82 | 33 | -1,157 |  |  | 2/4 |
| 9 | scartata | 42 | 5,633 | 1.82 | 33 | -1,160 |  |  | 2/4 |
| 10 | scartata | 43 | 2,870 | 1.07 | 35 | -3,752 |  |  | 1/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 7 trade, -3,904
- 2025-06-02 → 2025-11-01: 10 trade, 846
- 2025-11-01 → 2026-04-02: 4 trade, -1,654
- 2026-04-02 → 2026-09-01: 12 trade, 1,904

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 7 trade, -4,404
- 2025-06-02 → 2025-11-01: 10 trade, 846
- 2025-11-01 → 2026-04-02: 4 trade, -1,654
- 2026-04-02 → 2026-09-01: 12 trade, 1,404

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 7 trade, -3,904
- 2025-06-02 → 2025-11-01: 10 trade, 846
- 2025-11-01 → 2026-04-02: 4 trade, -1,654
- 2026-04-02 → 2026-09-01: 12 trade, 1,904

**Finalista 4** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 7 trade, -3,754
- 2025-06-02 → 2025-11-01: 10 trade, -2,644
- 2025-11-01 → 2026-04-02: 4 trade, -675
- 2026-04-02 → 2026-09-01: 12 trade, 2,691

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 7 trade, -3,754
- 2025-06-02 → 2025-11-01: 10 trade, -2,644
- 2025-11-01 → 2026-04-02: 4 trade, -675
- 2026-04-02 → 2026-09-01: 12 trade, 3,191

**Finalista 6** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 7 trade, -3,754
- 2025-06-02 → 2025-11-01: 10 trade, -2,644
- 2025-11-01 → 2026-04-02: 4 trade, -675
- 2026-04-02 → 2026-09-01: 12 trade, 3,191

**Finalista 7** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 7 trade, -1,859
- 2025-06-02 → 2025-11-01: 10 trade, -777
- 2025-11-01 → 2026-04-02: 4 trade, 433
- 2026-04-02 → 2026-09-01: 12 trade, 1,044

**Finalista 8** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 7 trade, -1,859
- 2025-06-02 → 2025-11-01: 10 trade, -777
- 2025-11-01 → 2026-04-02: 4 trade, 433
- 2026-04-02 → 2026-09-01: 12 trade, 1,047

**Finalista 9** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 7 trade, -1,859
- 2025-06-02 → 2025-11-01: 10 trade, -777
- 2025-11-01 → 2026-04-02: 4 trade, 433
- 2026-04-02 → 2026-09-01: 12 trade, 1,044

**Finalista 10** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 50
Direction = 1
DvolMin = 0
EndHour = 21
ExitHour = 15
IntradayOnly = 0
MaxBars = 12
OffsetTicks = 2
PtnDirNo = -3
PtnDirYes = 52
PtnNeutNo = 47
PtnNeutYes = 32
SkipDay = -1
StartHour = -1
StopLoss = 3000
TakeProfit = 500
TrailingStop = 500
```

- 2025-01-01 → 2025-06-02: 7 trade, -2,405
- 2025-06-02 → 2025-11-01: 11 trade, -1,355
- 2025-11-01 → 2026-04-02: 4 trade, -62
- 2026-04-02 → 2026-09-01: 13 trade, 70

## Esito: nessuna finalista sopravvive alla validazione fuori campione.

## Rilettura del 22/09

Lettura a freddo, senza codice. Stesso split e stessi costi della sweep FDAX, quindi le due si
leggono insieme; le obiezioni F3-F7 della rilettura di `fdax-4h-pc-tutto-al-minuto.md` valgono
anche qui. In più:

**N1. «Drawdown zero non ammissibile» è necessario, non sufficiente.** La finalista ha
`StopLoss = 3000` e `TakeProfit = 500`: sei a uno *al contrario*, 42 trade e nessuna perdita in
campione. Basta una perdita minuscola perché la regola proposta la lasci passare con un punteggio
ancora assurdo. Quello che il criterio non vede è la coda: un rapporto stop/target così è una
strategia che vince quasi sempre poco e perde raramente molto, e tre anni possono non contenere il
«raramente». O il rapporto si vincola in griglia, o il pavimento del drawdown si lega alla
distanza di stop e non alla peggior perdita *osservata*.

**N2. Dieci finaliste, sei distinte, una discendenza.** 1 ≡ 3, 7 ≡ 9, 5 ≡ 6 nei risultati: il
break-even è inerte con quel trailing. E `ExitHour = 15` con `IntradayOnly = 0` è inerte per
definizione — la fase l'ha «scelto» su una configurazione dove non fa niente. Parametri inerti
gonfiano la lista delle finaliste e il conteggio delle combinazioni.

**N3. La cella era sottile prima di qualunque filtro.** La base al minuto fa 179 trade in tre
anni con PF 1,14 e netto 23.833: un PC a 4 ore su NQ con $38 di commissione e 6,7 punti di swap a
notte è marginale in partenza. Il criterio ha poi stretto 179 → 48 → 33 → 42. Su una cella così la
domanda non è «quale filtro», è «se» — e la risposta della sweep è coerente con un «no».

**Cosa regge.** La diagnosi dei due difetti del criterio in `lavori-in-corso.md` è giusta;
N1 dice che la seconda correzione va pensata sulla coda e non sul solo zero.