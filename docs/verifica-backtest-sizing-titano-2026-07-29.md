# Verifica backtest, sizing e Titano — 29/07/2026

Ambito: strategia `PTS_001_NQ_60`, motore di backtest interno, conversione
account, filtro `TitanoFilterMode.BacktestRotationFile` e piano/sessione
`ExternalBroker`.

Stato: **stabile**. La verifica è basata su fixture OHLCV e manifest Titano
deterministici, senza modificare i dati del workspace operativo.

---

## Esito

La strategia PTS è risultata coerente con la propria specifica:

- emette stop order attivo dalla barra successiva;
- dichiara `$1.000` di stop e `$3.000` di target per contratto;
- su NQ (`$20/punto`) l'engine li traduce rispettivamente in `50` e `150`
  punti;
- il fill stop è gap-aware (`max(open, stop)` per un long);
- i companion long/short sono OCO: il fill di uno cancella l'altro.

Il controllo ha individuato tre difetti nel confine di esecuzione; sono stati
corretti nello stesso intervento. Il comportamento relativo al massimo di trade
concorrenti in backtest senza Titano non è un difetto: quel run deve mantenere
tutte le strategie del masterfilter per generare i trade sorgente della
rotazione.

## Formule verificate

| Grandezza | Formula | Caso PTS NQ |
|---|---|---|
| Stop in punti | `StopLossMoneyPerFutureContract / DollarsPerPoint` | `1000 / 20 = 50` |
| Target in punti | `TakeProfitMoneyPerFutureContract / DollarsPerPoint` | `3000 / 20 = 150` |
| P&L long | `(exit - entry) × quantity × DollarsPerPoint` | `150 × 1 × 20 = $3.000` |
| Conversione account | `quantity × ContractMultiplier` | `1 × 0,01 = 0,01` |
| Titano sessione | `base × strategyEquity × volatilità × portfolio` | `10 × 0,5 = 5` |

## Correzioni applicate

### B1 — Quantità frazionarie

**Problema.** `PiootooTradingService.OpenPosition` convertiva la quantità in
`int` e imponeva un minimo di uno:

```csharp
Math.Max(1, (int)quantity)
```

Una conversione account `1 × 0,01` diventava quindi un contratto intero. Il
rischio, le commissioni, il P&L realizzato e l'equity venivano sovrastimati di
100 volte.

**Correzione.** `OpenPosition.Contracts`, `StrategyPositionSnapshot.Contracts`,
gli eventi di diagnostica e `StrategyHourlyResult.Contracts` sono ora
`decimal`. L'engine rifiuta soltanto quantità non positive e conserva `0,01`
fino al `TradingResult`.

**Evidenza.** `AccountConversion_FractionalMultiplier_PreservesDecimalQuantity`
apre `0,01` e verifica un TP lordo di `$30`.

### B2 — AllocationMultiplier Titano nel backtest interno

**Problema.** Il filtro `BacktestRotationFile` escludeva correttamente le
strategie OFF, ma una strategia `Reduced` con allocation `0,5` continuava a
inviare la quantità piena. Il `trades.json` del backtest filtrato non era quindi
economicamente confrontabile con `FilteredEquity` del manifest.

**Correzione.** `TitanoBacktestFilter` mantiene, con la stessa cache di periodo
usata per l'insieme ON/OFF, la mappa `StrategyCode → AllocationMultiplier`.
Nel loop il moltiplicatore viene applicato al segnale e ai companion prima della
conversione account e prima di `PiootooTradingService.ProcessSignals`.

**Evidenza.** `TitanoResolve_ReducedStrategy_ScalesBacktestQuantity` verifica
che `10 × 0,5` apra `5` contratti.

### B3 — Doppia applicazione Titano nel piano

**Problema.** `OpenFromPlan` mette lo stesso run Titano sulla sessione e sul suo
gruppo. `PushBars` applicava già l'allocation nel `PositionSizingService`, poi
`CloneForClaim` applicava di nuovo l'allocation del gruppo: `10 × 0,5 × 0,5`,
arrotondato a `2`, anziché `5`.

**Correzione.** `GetGroupStrategyAllocation` restituisce `1` quando il gruppo
usa lo stesso `TitanoRunId` e la stessa cartella della sessione già filtrata.
Un gruppo con run distinto mantiene invece il proprio scaling indipendente.

**Evidenza.** `OpenPlan_WithTitano_AppliesAllocationOnce` verifica template e
intent reclamato entrambi a quantità `5` e moltiplicatore strategia `0,5`.

### B4 — MaxConcurrentTrades in backtest senza Titano

**Decisione.** Non modificato. In `ClientRunMode.Backtest` con
`TitanoFilterMode.Disabled`, il piano deve produrre il master completo; applicare
il limite operativo di concorrenza eliminerebbe segnali e altererebbe i trade
usati per calcolare la rotazione Titano.

**Evidenza.** `OpenPlan_BacktestWithoutTitano_EvaluatesAllWorkspaceStrategies`
conferma che il poll non restituisce `MaxConcurrentTradesExceeded`. In realtime
il limite resta applicato, verificato da
`OpenPlan_RealtimeWithoutTitano_EnforcesMaxConcurrentTrades`.

## Test eseguiti

I test deterministici sono in:

- `Piootoo.Strategies.Tests/PtsEngineAuditTests.cs`;
- `Piootoo.Strategies.Tests/TitanoSizingAuditTests.cs`.

Risultato mirato: **15/15 pass**.

La build della solution è riuscita. La suite seriale completa ha prodotto
**100 pass e 2 failure** non trattate in questo intervento:

1. `TitanoRotationTests.RunPersistsOriginalEquityAndReportIncludesComparisonChart`;
2. `TradingSessionsHttpTests.TitanoRunFiltersSignalsThroughHttpBoundary`.

## Riferimenti codice

- `Piootoo.Strategies/Easy/PTS_001_NQ_60.cs`
- `Piootoo.Core/Services/PiootooTradingService.cs`
- `Piootoo.Core/Services/PiootooBacktestingService.cs`
- `Piootoo.Core/Services/TradingSessionService.cs`
- `Piootoo.Core/Services/PositionSizingService.cs`
- `Piootoo.Core/Services/TitanoRotationService.cs`
- `Piootoo.Shared/Models/Trading/OpenPosition.cs`
- `Piootoo.Shared/Models/Trading/StrategyEvaluationRequest.cs`
- `Piootoo.Shared/Models/Backtesting/StrategyHourlyResult.cs`
