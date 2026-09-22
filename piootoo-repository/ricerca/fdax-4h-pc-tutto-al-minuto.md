# Sweep @FDAX 240m — PC

- Strategia di partenza: `PT3B_FDAX_PCH_001_240`
- Datafeed: ICS
- Spread: ICS,FTMOPLATFORM, mediana **costante** per simbolo: FDAX 1.23 pt
- Swap: **FDAX** long 5.054 pt/notte, short 1.215 pt/notte, rollover 20:59 UTC (ICS,FTMO)
- Commissione: $19.23 per contratto e per lato, cioe' $38.46 per trade
- Campione di ricerca: 2022-01-01 → 2025-01-01
- Validazione: 2025-01-01 → 2026-09-01
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 30 trade e 5 per tratto
- Beam: 2
- Durata: 177.4 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| trigger | 240 | 122 | 2.0 | PT3B_FDAX_PCH_001_240: 775 trade, netto 38,576, DD 49,331, PF 1.06, 3,080 ms |
| volatilita' | 3 | 6 | 0.1 | PT3B_FDAX_PCH_001_240: 782 trade, netto 41,505, DD 48,813, PF 1.06, 2,833 ms |
| pattern neutrali | 3,025 | 2,765 | 37.6 | PT3B_FDAX_PCH_001_240: 173 trade, netto 89,892, DD 8,099, PF 1.75, 2,523 ms |
| pattern direzionali | 10,609 | 7,412 | 108.5 | PT3B_FDAX_PCH_001_240: 56 trade, netto 50,798, DD 3,221, PF 2.85, 2,543 ms |
| orari e giorni | 1,250 | 1,456 | 13.2 | PT3B_FDAX_PCH_001_240: 52 trade, netto 53,407, DD 3,497, PF 3.37, 2,579 ms |
| uscita di sessione | 9 | 18 | 0.2 | PT3B_FDAX_PCH_001_240: 52 trade, netto 53,407, DD 3,497, PF 3.37, 2,700 ms |
| stop e target | 1,352 | 2,704 | 14.6 | PT3B_FDAX_PCH_001_240: 52 trade, netto 50,386, DD 3,077, PF 3.28, 2,589 ms |
| trailing e breakeven | 12 | 18 | 0.2 | PT3B_FDAX_PCH_001_240: 52 trade, netto 50,386, DD 3,077, PF 3.28, 2,705 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 52 | 50,386 | 16.38 | 32 | -20,300 |  |  | 0/4 |
| 2 | scartata | 52 | 26,424 | 8.59 | 32 | -7,231 |  |  | 0/4 |
| 3 | scartata | 52 | 32,398 | 9.35 | 32 | -11,731 |  |  | 0/4 |
| 4 | scartata | 52 | 17,146 | 3.58 | 32 | -5,469 |  |  | 1/4 |
| 5 | scartata | 52 | 39,293 | 10.50 | 32 | -10,466 |  |  | 0/4 |
| 6 | scartata | 52 | 27,488 | 8.93 | 32 | -6,427 |  |  | 0/4 |
| 7 | scartata | 52 | 32,791 | 9.83 | 32 | -9,638 |  |  | 0/4 |
| 8 | scartata | 52 | 12,689 | 2.84 | 32 | -6,550 |  |  | 0/4 |
| 9 | scartata | 52 | 17,146 | 3.58 | 32 | -5,469 |  |  | 1/4 |

**Finalista 1** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 12 trade, -6,862
- 2025-06-02 → 2025-11-01: 4 trade, -1,654
- 2025-11-01 → 2026-04-02: 10 trade, -7,054
- 2026-04-02 → 2026-09-01: 6 trade, -4,731

**Finalista 2** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 12 trade, -462
- 2025-06-02 → 2025-11-01: 4 trade, -154
- 2025-11-01 → 2026-04-02: 10 trade, -4,885
- 2026-04-02 → 2026-09-01: 6 trade, -1,731

**Finalista 3** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 0
```

- 2025-01-01 → 2025-06-02: 12 trade, -4,962
- 2025-06-02 → 2025-11-01: 4 trade, -1,654
- 2025-11-01 → 2026-04-02: 10 trade, -3,385
- 2026-04-02 → 2026-09-01: 6 trade, -1,731

**Finalista 4** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 12 trade, -1,471
- 2025-06-02 → 2025-11-01: 4 trade, -1,439
- 2025-11-01 → 2026-04-02: 10 trade, -3,475
- 2026-04-02 → 2026-09-01: 6 trade, 915

**Finalista 5** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 0
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 12 trade, -4,283
- 2025-06-02 → 2025-11-01: 4 trade, -840
- 2025-11-01 → 2026-04-02: 10 trade, -4,042
- 2026-04-02 → 2026-09-01: 6 trade, -1,301

**Finalista 6** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 12 trade, -462
- 2025-06-02 → 2025-11-01: 4 trade, -154
- 2025-11-01 → 2026-04-02: 10 trade, -4,081
- 2026-04-02 → 2026-09-01: 6 trade, -1,731

**Finalista 7** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 2000
```

- 2025-01-01 → 2025-06-02: 12 trade, -4,574
- 2025-06-02 → 2025-11-01: 4 trade, -840
- 2025-11-01 → 2026-04-02: 10 trade, -3,666
- 2026-04-02 → 2026-09-01: 6 trade, -558

**Finalista 8** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 500
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 12 trade, -858
- 2025-06-02 → 2025-11-01: 4 trade, -735
- 2025-11-01 → 2026-04-02: 10 trade, -2,953
- 2026-04-02 → 2026-09-01: 6 trade, -2,004

**Finalista 9** — fuori campione non ammissibile per l'obiettivo (in perdita o sotto le soglie)

```
BreakEven = 1000
ChannelBars = 1
Direction = 1
DvolMin = 3000
EndHour = 10
ExitHour = 20
IntradayOnly = 1
MaxBars = 12
OffsetTicks = 0
PtnDirNo = -28
PtnDirYes = 45
PtnNeutNo = 53
PtnNeutYes = 19
SkipDay = -1
StartHour = -1
StopLoss = 1500
TakeProfit = 3000
TrailingStop = 1000
```

- 2025-01-01 → 2025-06-02: 12 trade, -1,471
- 2025-06-02 → 2025-11-01: 4 trade, -1,439
- 2025-11-01 → 2026-04-02: 10 trade, -3,475
- 2026-04-02 → 2026-09-01: 6 trade, 915

## Esito: nessuna finalista sopravvive alla validazione fuori campione.

## Rilettura del 22/09

Lettura a freddo, senza codice. Il confronto con la 002 (`fdax-4h-pc-uscita-sessione.md` e
`lavori-in-corso.md`) è su **stesso feed, stesso split, stessi costi e stesso orologio**: le
intestazioni coincidono riga per riga, e la domanda «batte la 002?» ha risposta valida. Le
obiezioni riguardano cosa quella risposta dimostra.

**F1. Come è stato scelto `ExitHour = 21` per la 002?** La fase «uscita di sessione» della sweep
veloce aveva scelto 15; 21 viene da fuori. Se la ragione è a priori — chiudere prima del rollover
delle 20:59 UTC, quindi 21 di Roma è sempre prima — il +52.310 fuori campione è pulito. Se 21 è
stato scelto guardando più orari *sulla validazione*, il fuori campione della 002 non è più fuori
campione, e il confronto qui sopra è truccato a favore della 002. Il testo non lo dice.

**F2. Manca la 001 sullo stesso split.** In campione al minuto la base fa 775 trade e 38.576 (riga
«trigger»), la 002 787 e 51.951: `ExitHour = 21` vale ≈13.000 in campione. Fuori campione il
numero della 001 non è scritto da nessuna parte. «La 002 resta il candidato» richiede 001 contro
002 sullo stesso split, non solo sweep contro 002: «riproduce al dollaro la misura» è
riproducibilità, non superiorità.

**F3. Le nove finaliste sono una configurazione sola.** Condividono tutte le prime sei fasi
(stessi pattern, orari, direzione, `DvolMin`) e differiscono per break-even e trailing; la 4 e la
9 sono **identiche** nei risultati (break-even 0 e 1000 con trailing 1000 danno gli stessi trade,
quindi il break-even è inerte lì). Il beam a 2 si è richiuso su una discendenza sola.
«Nessuna delle nove sopravvive» va letto «una non sopravvive, provata in nove varianti di rischio».

**F4. Quattro cose sono cambiate insieme rispetto alla sweep che ha trovato la 001**, e il
fallimento viene attribuito a una sola. Orologio (veloce → minuto), obiettivo (netto/DD con 50
trade → peggior tratto), costi ($4 per lato → $19,23 più swap più broker peggiore) e periodo
(2014-2022 → 2022-2024). La 001, uscita dalla sweep «che ordinava rumore», valida 4/4 su
2022-2026 e torna sul numero di trade del run cTrader a tick: c'è una tensione fra «il veloce
ordinava rumore» (Spearman 0,021 su 22 configurazioni, una cella; 0,62 su NQ) e il fatto che il suo
prodotto regga. Per sapere quale delle quattro cose ha spento la ricerca serve un run controllato:
**vecchio obiettivo, orologio al minuto, stesso split**. Senza, «si tara a mano» è una preferenza.

**F5. `MinTrades = 300` cura il sintomo; il criterio premia la rarità per costruzione.** Il
pavimento del drawdown è la peggior perdita singola, cioè circa uno stop: con 52 trade il DD
peggiore è 3.077, due stop, e il punteggio schizza a 16. Alzare il minimo sposta la soglia ma non
il gradiente: la previsione verificabile è che la prossima sweep **converga su ~300 trade esatti**,
il minimo che passa. Se succede, il criterio sta scegliendo il conteggio, non la qualità, e va
cambiato il denominatore (scalato con il numero di trade) invece della soglia.

**F6. Il taglio non lo fa la fase di rischio, lo fanno i pattern, con uno stop che non è quello
della strategia.** 782 → 173 (pattern neutrali) → 56 (direzionali): l'80% dei trade sparisce
prima di orari e rischio. E quelle fasi girano con lo stop al seme (1500, 60 punti FDAX): i pattern
sono selezionati per funzionare con uno stop **tre volte più stretto** di quello della 001 (5000).
Il «secondo giro dopo il rischio» di `lavori-in-corso.md` riguarda gli orari; qui il danno è nei
pattern, e un secondo giro degli orari non lo tocca.

**F7. Incoerenze piccole ma da chiudere.** `lavori-in-corso.md` scrive «`MinTrades = 50`», qui
l'intestazione dice «almeno 30». «Correzione 1» dice che `MinProfitFactor` 1,25 vale in tutte le
fasi, ma la migliore ammissibile della fase trigger ha PF 1,06: o la soglia non è in vigore, o la
colonna «migliore in campione» mostra anche le non ammissibili. E «ammissibili» supera
«combinazioni» (9 → 18, 1.352 → 2.704): è combinazioni × semi del beam, ma il resoconto non lo dice.

**Cosa regge.** Il confronto è onesto nelle condizioni. La diagnosi «con l'orologio giusto ordina
fortuna» è corretta come descrizione. La 002 è un candidato legittimo *se* F1 ha la risposta
a priori.