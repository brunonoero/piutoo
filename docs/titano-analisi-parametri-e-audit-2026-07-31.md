# Titano — analisi parametri, audit codice e prontezza JSON per il cBot

Data: 31/07/2026. Ambito: reverse del codice, non solo lettura della documentazione
esistente. Le affermazioni numeriche sono state verificate rieseguendo gli algoritmi
fuori dal progetto.

> **Stato al 31/07/2026 — tutti i difetti elencati sono stati corretti.**
> Ogni sezione riporta in coda la correzione applicata. Il documento resta come
> analisi di riferimento: spiega *perché* il codice è fatto così, non solo cosa fa.
>
> **La build non è stata eseguita in questa sessione** (l'ambiente non ha `dotnet`).
> Prima di considerare chiuso l'intervento serve
> `dotnet build PiootooApp.sln` e
> `dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj` da Windows.

Riferimenti: `docs/domini/titano-rotation.md` (stabile),
`docs/domini/distribuzione-multi-account.md` (il layer di filtro successivo),
`docs/verifica-backtest-sizing-titano-2026-07-29.md`, `docs/decisioni.md`.

---

## 0. Premessa: esisteva un secondo Titano, è stato rimosso

Fino al 31/07/2026 convivevano due sistemi con lo stesso nome. Il secondo —
`TitanoFilterService` + `TitanoSetupService` + client `piootoo.titanoclient`,
un ON/OFF settimanale binario calcolato su `BacktestingResult` invece che sui trade
chiusi — **è stato rimosso in questo stesso intervento**.

Non era mai stato eseguito: `settings/titano-setups/` e `settings/results/titano/`
erano vuote, create solo dal costruttore del servizio. La console principale non lo
chiamava (usa esclusivamente `rotations` / `rotation-setups`), il suo unico client
era fuori dal profilo di avvio della solution, ed era fermo al commit iniziale
mentre la v2 evolveva. Portava inoltre un lock-out permanente del cooldown.

Dettaglio della rimozione e motivazioni in `decisioni.md` (2026-07-31).
**Da qui in avanti "Titano" significa una cosa sola: `TitanoRotationService`.**

---

## 0-bis. I due flussi di lavoro

`trades.json` può essere prodotto in due modi, e **in entrambi i casi Titano si
applica allo stesso identico modo**. Non ci sono due Titano né due semantiche: c'è
un solo servizio, un solo `TitanoFilterMode`, un solo manifest.

Ciò che cambia è solo **chi esegue gli ordini**: il motore interno o cTrader.

### Flusso interno (motore `PiootooBacktestingService`)

```
1. backtest con TitanoMode = Disabled          → trades.json (tutte le strategie)
2. POST /api/Titano/rotations                  → manifest.json + period-*.json
3. backtest con TitanoMode = BacktestRotationFile  → trades.json filtrato
                                                     + confronto nel report
```

### Flusso esterno (cBot `PiootooTradingSessionBot`)

```
1. sessione con TitanoMode = Disabled          → trades.json (tutte le strategie)
2. POST /api/Titano/rotations                  → manifest.json + period-*.json
3. sessione con TitanoMode = BacktestRotationFile  → backtest cTrader filtrato
4. sessione con TitanoMode = Realtime          → live, segnali filtrati dalla
                                                  rotazione in corso
```

**I due flussi sono simmetrici fino al passo 3.** `BacktestingRequest.TitanoMode` e
`CreateTradingSessionRequest.TitanoMode` sono lo **stesso enum**
`Piootoo.Shared.Models.Trading.TitanoFilterMode`, e il commento nel contratto lo
dichiara esplicitamente: *"Identica a quella delle sessioni, così backtest interno
ed engine esterno cTrader si comportano allo stesso modo"*.

Anche il rifiuto delle combinazioni impossibili è simmetrico:
`PiootooBacktestingService.CreateTitanoFilter` rifiuta `Realtime` in backtest
interno esattamente come `TradingSessionService.RequireCoherentRunMode` lo rifiuta
per il cBot.

Il passo 4 esiste solo nel flusso esterno, per un motivo che non è arbitrario: il
motore interno replica dati storici, quindi non ha un "adesso" a cui applicare la
rotazione corrente.

**Sul passo 4 e sulla rotazione N+1.** La formulazione "Titano produce la rotazione
N+1 nel futuro" è corretta come intenzione ma va precisata su un punto:
`EffectiveToUtc` dell'ultimo periodo non si estende nel futuro, coincide con la fine
dell'intervallo su cui il manifest è stato calcolato. Le barre live cadono quindi
**oltre** la fine del manifest, e ci arrivano solo grazie al fallback di `Resolve`:
in `TitanoFilterMode.Realtime` una barra oltre l'ultimo periodo ricade sull'ultimo
periodo calcolato — che è appunto la rotazione in vigore finché non se ne produce
una nuova. Il caso è tracciato (`UsedLatestPeriod = true`) e finisce nel rotation-log
con l'avviso *"Rigenera l'analisi Titano"*.

Operativamente: **il manifest va rigenerato con la stessa cadenza di
`RotationPeriod`.** Con `Weekly`, un manifest non rigenerato per un mese fa girare
il live su una rotazione vecchia di quattro settimane, senza errori e senza che
nulla si fermi — solo una riga di avviso nel log.

---

## 1. I parametri

### 1.1 Calendario

| Parametro | Default | Effetto |
|---|---|---|
| `RotationPeriod` | `Weekly` | Granularità della decisione. Weekly = lunedì 00:00 UTC, blocchi `[inizio, fine)`. |
| `StartUtc` / `EndUtc` | — | Intervallo su cui costruire i periodi. Il primo periodo è **solo osservazione** (nessuna decisione efficace), l'ultimo **non produce mai una decisione**: con N periodi si ottengono N−1 decisioni. |
| `BiweeklyAnchorUtc` | `StartUtc` | Ancoraggio dei blocchi da 14 giorni. |
| `InitialCapital` | 100.000 | Denominatore di **tutte** le percentuali (return, drawdown, z-score). |

Il no-look-ahead è implementato in un punto solo e correttamente: `BuildDecisions`
misura sui trade con `ExitTimeUtc < periods[i].End` e applica il risultato a
`periods[i+1]`. Non c'è modo per una decisione di vedere il proprio periodo.

### 1.2 Finestre di misura

| Parametro | Default | Nota |
|---|---|---|
| `ShortWindowDays` | 90 | Finestra "breve": performance recente **e** volatilità dei rendimenti trade. |
| `LongWindowDays` | 365 | Trend di fondo. Vincolo: `≥ ShortWindowDays`. |
| `MovingAverageWindowDays` | 90 | Media mobile dell'equity e deviazione standard usata nello z-score. |

Osservazione che vale più di un bug: con `RotationPeriod = Weekly` e finestra breve
a 90 giorni, **la decisione settimanale è guidata da una misura che si muove su
~13 settimane**. Il periodo di rotazione è settimanale, la reattività no. Chi si
aspetta che Titano reagisca a una settimana pessima resterà deluso: quella settimana
pesa 1/13 della finestra breve e ~1/52 di quella lunga. Se si vuole reattività
settimanale, `ShortWindowDays` va portato nell'ordine di 21–35 giorni, e va
verificato che restino abbastanza trade per `MinimumTrades`.

### 1.3 I cinque cancelli (voti)

Ogni strategia riceve 5 voti indipendenti. Ogni voto ha un esito booleano
(`Passed`) **e** un punteggio continuo 0..1 (`Score`). I due servono a cose diverse:
l'esito governa l'ON/OFF, il punteggio governa la size.

| Voto | Passa se | Punteggio |
|---|---|---|
| `short-performance` | `Trades ≥ MinimumTrades` **e** `ShortReturn ≥ MinimumShortReturn` **e** (se richiesto) `equity ≥ media mobile` | `0.5 + clamp(ShortReturn − min, ±0.5)` |
| `long-performance` | `LongReturn ≥ MinimumLongReturn` | `0.5 + clamp(LongReturn − min, ±0.5)` |
| `z-score` | `MinimumZScore ≤ z ≤ MaximumZScore` | `1 − \|z − centro\| / semiampiezza`, 0 fuori banda |
| `drawdown` | `CurrentDD ≤ MaximumCurrentDrawdown` **e** `MaxDD ≤ MaximumObservedDrawdown` | `1 − CurrentDD / MaximumCurrentDrawdown` |
| `volatilità` | `ReturnVolatility ≤ MaximumReturnVolatility` | `1 − Vol / MaximumReturnVolatility` |

Default: `MinimumShortReturn = MinimumLongReturn = 0`, `z ∈ [−1.5, +2.5]`,
`MaxCurrentDD = 15%`, `MaxObservedDD = 25%`, `MaxVol = 10%`,
`RequireEquityAboveMovingAverage = true`.

Due cose vanno dette sulla scala dei punteggi, perché condizionano tutto il sizing:

- `short-performance` e `long-performance` sono normalizzati su ±50%. Un rendimento
  reale di +3% produce 0.53; uno di −2% produce 0.48. **Su una scala 0..1, questi
  voti stanno di fatto fermi a 0.5.** È esattamente il motivo per cui esiste il
  sizing per percentile.
- `z-score` **era** binario, ed era un difetto: senza informazione di grado tutte le
  strategie dentro banda risultavano pari merito, prendevano percentile 0,5 e nessuna
  poteva più raggiungere l'allocazione massima. Ora vale 1 al centro della banda e
  degrada verso 0 ai suoi estremi. Il centro è il punto migliore e non il massimo,
  perché un z troppo alto è surriscaldamento — è esattamente il motivo per cui esiste
  `MaximumZScore`. Vedi **B4** (§4).

`MinimumPassingFilters` (default 4 su 5) è il numero di voti che devono passare
perché la strategia sia **eleggibile**. Sotto quella soglia non è una questione di
size: la strategia è fuori.

### 1.4 Isteresi, sizing e costi

| Parametro | Default | Effetto reale |
|---|---|---|
| `MinimumPassingFilters` | 4 | Cancello di eleggibilità. |
| `MaximumCurrentDrawdown` | 15% | Cancello di spegnimento (hard). |
| `ReenableMaximumCurrentDrawdown` | 10% | Soglia **più severa** per riaccendere. È l'isteresi. |
| `CooldownPeriodsAfterOff` | 2 | Periodi di fermo obbligato dopo uno spegnimento. |
| `MinimumOnPeriods` | 1 | Periodi minimi di ON prima di poter spegnere. |
| `HardStopDrawdown` | 35% | Blocco latched, reset solo manuale. Vincolo: `> MaximumCurrentDrawdown`. |
| `CrossSectionalSizing` | **true** | Se true il sizing è per rango. Check *Sizing per percentile* nella form. |
| `MinimumAllocationMultiplier` | 0.25 | Allocazione della peggiore eleggibile. Solo se percentile. |
| `MaximumAllocationMultiplier` | 1.00 | Allocazione della migliore, ed è il tetto rispetto a cui si decide `Enabled`. Solo se percentile. |
| `AllocationStep` | 0.05 | Arrotondamento della curva. Solo se percentile. |
| `DisableCompositeScore` | 0.40 | Letto **solo** con `CrossSectionalSizing = false`. |
| `ReenableCompositeScore` | 0.60 | Letto **solo** con `CrossSectionalSizing = false`. |
| `SizingTiers` | 0.80/0.60/0.40/0 | Letto **solo** con `CrossSectionalSizing = false`. |
| `CommissionPerUnit`, `SlippagePerUnit` | 0 | Costi simulati sulla sola equity offline. |
| `CalibrationPeriods` / `EvaluationPeriods` | 8 / 4 | Walk-forward. |

Le due modalità di sizing sono mutuamente esclusive e leggono parametri diversi: la
form disabilita i controlli che non hanno effetto in quella scelta, così non è più
possibile modificare un campo, ottenere un `runId` nuovo e un manifest identico —
che era il difetto **B3**.

---

## 2. Come si compone il fattore di riduzione / sospensione

La domanda ha in realtà due risposte, perché **accensione e size sono due decisioni
separate che non si parlano**. Questo è il punto architetturale di Titano v2, ed è
una scelta corretta.

### 2.1 Prima decisione: la strategia è accesa? (cancelli assoluti)

Nessun percentile entra qui. Da `TitanoRotationService.BuildDecisions`:

```
eligible   = (voti passati ≥ MinimumPassingFilters)
mayDisable = (primo periodo) oppure (ConsecutiveOnPeriods ≥ MinimumOnPeriods)

disable  = CurrentDD > MaximumCurrentDrawdown
           OR NOT eligible
           OR [solo se CrossSectionalSizing=false] rawScore < DisableCompositeScore

reenable = CurrentDD ≤ ReenableMaximumCurrentDrawdown
           AND eligible
           AND cooldown == 0
           AND [solo se CrossSectionalSizing=false] rawScore ≥ ReenableCompositeScore

on = se non c'è storia   → NOT disable
     se era ON           → NOT disable OPPURE NOT mayDisable
     se era OFF          → reenable

se hardStopped → on = false   (prevale su tutto, anche su MinimumOnPeriods)
```

Tre asimmetrie volute, tutte anti-whipsaw:

1. **Spegnere è più facile che riaccendere.** Per spegnere basta violare
   `MaximumCurrentDrawdown` (15%); per riaccendere serve rientrare sotto
   `ReenableMaximumCurrentDrawdown` (10%). La banda 10–15% è la zona morta.
2. **Riaccendere richiede anche il cooldown scaduto**, spegnere no.
3. **`MinimumOnPeriods` può tenere accesa una strategia che ha violato i cancelli**,
   per evitare un OFF al primo inciampo. Non protegge dallo hard stop.

### 2.2 Seconda decisione: quanta size? (percentile cross-sezionale)

Solo per le strategie già dichiarate ON. Due passate su tutte le strategie del
periodo, perché il rango di una dipende dalle altre:

```
passata 1  →  per ogni strategia: metriche  →  5 voti (esito + punteggio 0..1)
passata 2  →  per ogni VOTO k, ordina le N strategie per punteggio:
                percentile_i,k = (quanti sotto + (quanti pari − 1)/2) / (N − 1)
              score_i = media dei 5 percentili
              allocazione = MinAlloc + (MaxAlloc − MinAlloc) × score_i
              arrotondata ad AllocationStep, clampata in [MinAlloc, MaxAlloc]
```

Il percentile è calcolato **voto per voto e poi mediato**, non sulla media dei voti.
Non è un dettaglio: un voto che varia pochissimo in assoluto (drawdown, volatilità
— stanno sopra 0.9) verrebbe schiacciato dalla media, mentre il percentile gli
restituisce l'intera scala 0..1 a prescindere da quanto vari.

I pari merito prendono il rango medio, quindi due strategie identiche prendono la
stessa allocazione. Con una sola strategia il rango è privo di significato e si
restituisce 1.

`RawScore` (media dei punteggi assoluti) è conservato nel manifest accanto a
`Score`. Serve a distinguere "è la peggiore del gruppo" da "va male": un percentile
basso in un periodo in cui vanno tutte bene non è un allarme, un `RawScore` basso sì.

### 2.3 Dallo stato all'ordine

`AllocationMultiplier` è il coefficiente **lento** `StrategyEquityMultiplier`. Si
combina moltiplicativamente con i coefficienti veloci, che Titano non conosce:

```
quantità finale = BaseQuantity
                × StrategyEquityMultiplier   ← Titano, per periodo
                × volatilityTarget           ← ATR, per barra
                × portfolioRisk              ← esposizione, per barra
                → arrotondata per difetto a QuantityStep
                → scartata se < MinimumIntentQuantity
```

Gli intent di **chiusura** non sono mai ridotti, per non lasciare posizioni residue.
Nel backtest interno il moltiplicatore scala la `Quantity` del segnale prima
dell'engine; offline, sull'equity del manifest, scala il `NetProfit` e sottrae
`(Commission + Slippage) × Quantity × AllocationMultiplier`.

Il difetto **B2** della verifica del 29/07 (allocation non applicata nel backtest
interno) è corretto e coperto da test. Il difetto **B3** (doppia applicazione
sessione + gruppo) idem.

### 2.4 In sintesi

> Il fattore di riduzione **non** è una funzione della bontà assoluta della
> strategia. È la sua **posizione in classifica** dentro il periodo, mappata
> linearmente su `[0.25, 1.00]`. La sospensione, al contrario, è **solo** funzione
> di soglie assolute. Usare il rango per spegnere significherebbe spegnere sempre
> qualcuno, anche in un periodo in cui vanno tutte bene.

---

## 3. Come si calcolano le settimane di fermo

### 3.1 Titano v2 — il conteggio corretto

Il contatore vive in `TitanoStrategyState.CooldownRemaining` ed è ricalcolato a
ogni periodo:

```
cooldown = (periodo precedente era OFF) ? max(0, precedente.CooldownRemaining − 1) : 0
...
se (era ON) e (ora è OFF) → cooldown = CooldownPeriodsAfterOff
```

Traccia con `CooldownPeriodsAfterOff = 2` e `RotationPeriod = Weekly`:

| Periodo | Stato | `CooldownRemaining` | Può riaccendersi? |
|---|---|---|---|
| N−1 | ON | 0 | — |
| **N** | **OFF** (violazione) | 2 | no |
| N+1 | OFF | 1 | no — cooldown ≠ 0 |
| N+2 | OFF | 0 | **sì**, se `reenable` è vero |

**`CooldownPeriodsAfterOff = 2` con periodo settimanale = la strategia sta ferma
2 settimane piene, e può rientrare dalla terza.** La riaccensione al periodo N+2
non è automatica: il cooldown è condizione necessaria, non sufficiente. Servono
anche `CurrentDD ≤ 10%` e `voti ≥ 4/5`. Se non ci sono, resta OFF con
`CooldownRemaining = 0` — cioè libera ma non idonea.

Il fermo minimo garantito, in settimane, è quindi `CooldownPeriodsAfterOff`; il
fermo effettivo è illimitato e dipende dai cancelli.

Attenzione a due punti che il manifest non rende evidenti:

- Il periodo N è già la decisione **efficace** per `[periods[N].Start, End)`. Lo
  spegnimento è stato *calcolato* sui trade fino a `periods[N−1].End`. Il ritardo
  di reazione è quindi 1 periodo **oltre** la latenza della finestra di misura
  (§1.2). Con finestra breve a 90 giorni la latenza totale è dominata da quella,
  non dal cooldown.
- Una strategia in **hard stop** mostra `CooldownRemaining` che scende a 0 e poi ci
  resta, mentre la strategia è ferma per sempre. Il campo è fuorviante ma non
  produce comportamento sbagliato (vedi **B7**).

### 3.2 Titano legacy — il conteggio è rotto

`TitanoFilterService` usava un meccanismo diverso, `cooldownUntil[strategia] =
indice settimana + CooldownWeeksAfterOff`, che veniva riarmato **anche nelle
settimane in cui la strategia era OFF proprio a causa del cooldown** — un lock-out
permanente al primo spegnimento. È una delle ragioni della rimozione (§5).

---

## 4. Bug — Titano v2 (`TitanoRotationService`)

Ordinati per impatto. La numerazione B* è nuova e non si sovrappone a quella della
verifica del 29/07.

### B3 — [ALTO] Tre campi della form non hanno alcun effetto

`WorkspaceBacktestingForm.cs:3660-3697` costruisce la `TitanoRotationRequest` ma
**non valorizza** `CrossSectionalSizing`, `MinimumAllocationMultiplier`,
`MaximumAllocationMultiplier`, `AllocationStep`. Restano ai default, quindi
`CrossSectionalSizing = true`. Ma in quella modalità:

- `SizingTiers` è ignorato da `ComputeAllocation` (ritorna alla riga 738 solo se
  `CrossSectionalSizing == false`);
- `DisableCompositeScore` è escluso da `disable` (riga 460);
- `ReenableCompositeScore` è escluso da `reenable` (riga 463).

Quindi i controlli **"Tier sizing"**, **"Score OFF"** e **"Score ON"** — con tanto
di tooltip che promettono un comportamento — sono **decorativi**. L'utente li
modifica, il `configHash` cambia, viene generato un `runId` nuovo, e il manifest è
**identico**. È il difetto più insidioso dell'insieme, perché non produce errori:
produce fiducia mal riposta.

Simmetricamente, i quattro parametri che *governano davvero* il sizing
(`MinimumAllocationMultiplier`, `MaximumAllocationMultiplier`, `AllocationStep`,
`CrossSectionalSizing`) non sono raggiungibili dalla form.

*Correzione:* esporre un check "Sizing per percentile" che abiliti/disabiliti i
gruppi di controlli corrispondenti, e aggiungere i tre numerici di allocazione.


**✅ Corretto.** La form espone ora un check *"Sizing per percentile"* e i tre
numerici `Alloc. min %`, `Alloc. max %`, `Passo alloc. %`. `ApplySizingModeAvailability`
disabilita i controlli che non hanno effetto nella modalità scelta: con il percentile
attivo si spengono `Tier sizing`, `Score OFF` e `Score ON`; con il percentile spento si
spengono i tre moltiplicatori. I quattro parametri sono anche persistiti nel
`TitanoRotationSetup`, e i setup predefiniti li usano per differenziarsi davvero
(*Conservativo* ha tetto 0,80, *Dinamico* pavimento 0,35).

### B4 — [ALTO] `State = Enabled` è di fatto irraggiungibile

`newStatus = multiplier == 1 ? Enabled : Reduced`. Con i default,
`ComputeAllocation` restituisce 1 solo se il percentile è esattamente 1, cioè se la
strategia è **strettamente la migliore su tutti e cinque i voti**. Ma il voto
`z-score` è binario: se due strategie lo superano entrambe, sono pari merito e
prendono percentile 0.5 su quel voto. Nessuna delle due può più arrivare a 1.

Verificato numericamente su 4 strategie plausibili, tutte con z-score dentro banda:

| Strategia | percentile | allocazione |
|---|---|---|
| A (migliore su 4 voti su 5) | 0.900 | **0.95** |
| B | 0.633 | 0.75 |
| C | 0.367 | 0.55 |
| D (peggiore) | 0.100 | 0.35 |

**Conseguenza: con ≥2 strategie che passano lo z-score, nessuna raggiunge mai il
100% di allocazione e lo stato `Enabled` non compare mai nel manifest — tutto è
`Reduced`.** Il portafoglio è sistematicamente sotto-investito di un ~5% strutturale
che non corrisponde ad alcun giudizio di rischio, e il campo `State` perde potere
informativo (`Enabled` diventa codice morto, `Reduced` non distingue più nulla).

*Correzione:* separare `State` dall'uguaglianza esatta con 1 — per esempio
`Enabled` quando `multiplier ≥ MaximumAllocationMultiplier`, oppure sopra una soglia
configurabile. In alternativa, dare allo z-score un punteggio continuo (distanza
normalizzata dal centro banda) così che i pari merito smettano di essere sistematici.


**✅ Corretto, su due fronti.**

1. **Il voto z-score ha ora un punteggio continuo** (`ZScoreVoteScore`): 1 al centro
   della banda ammessa, 0 ai suoi estremi, 0 fuori banda. Il centro è il punto migliore
   e non il massimo, perché un z troppo alto è surriscaldamento — è la ragione per cui
   `MaximumZScore` esiste. L'esito booleano del voto resta la semplice appartenenza alla
   banda: cambia solo il punteggio che entra nel percentile, che ora discrimina invece
   di produrre pari merito sistematici.
2. **`State` si confronta col tetto configurato, non con 1** (`ClassifyStatus` +
   `MaximumAllocation`). `Enabled` significa "a pieno regime", cioè al massimo che la
   configurazione permette — che con `MaximumAllocationMultiplier = 0,80` è 0,80.

Coperto da `ZScoreVoteScore_IsContinuous_SoTiesDoNotCollapseThePercentile`,
`MaximumAllocation_FollowsConfiguredCap_NotTheConstantOne` e
`AllocationCapIsReachable_WhenAStrategyLeadsEveryVote`.

### B5 — [ALTO] Il reset dello hard stop riapre la strategia scavalcando ogni cancello

`Resolve`, righe 365-372:

```csharp
AllocationMultiplier = x.HardStopped && resets.ContainsKey(x.StrategyCode)
    ? ComputeAllocation(x.Score, manifest.Config) : x.AllocationMultiplier,
State = ... ? TitanoStrategyStatus.Reduced : x.State,
```

Con `CrossSectionalSizing = true`, `ComputeAllocation` restituisce **almeno
`MinimumAllocationMultiplier` (0.25) per qualunque score, incluso 0**. Quindi una
strategia con 0/5 voti passati, drawdown corrente al 60% e cooldown attivo, dopo un
reset manuale torna operativa al 25% — immediatamente, senza ripassare da
`eligible`, `reenable` o `cooldown`.

Il reset è correttamente differito al periodo successivo (`EffectiveFromUtc = next.
EffectiveFromUtc`) e correttamente immutabile. Il problema non è *quando* ha
effetto, è *che cosa* riabilita. Un reset dovrebbe togliere il latch e lasciare che
sia la macchina a stati a decidere, non sostituirsi a essa.

*Correzione:* il reset azzera `HardStopped` e nient'altro; la strategia rientra dal
ramo `reenable` come qualsiasi altra, e `AllocationMultiplier` resta 0 finché i
cancelli non la riammettono.


**✅ Corretto.** Il reset toglie il latch e nient'altro. Il manifest è immutabile e
non si può rieseguire `BuildDecisions` dentro `Resolve`, ma tutto ciò che serve a
rivalutare i cancelli è già persistito nello stato — voti superati, drawdown corrente,
cooldown residuo, `RawScore`. `IsReenableSatisfied` riapplica quindi la **stessa**
condizione `reenable` della rotazione: se è soddisfatta la strategia rientra con
l'allocazione dello score, altrimenti resta a zero e rientrerà dalla prossima rotazione
calcolata. Il `Reason` dichiara quale dei due casi si è verificato.

Coperto da `HardStopReset_DoesNotReadmitAStrategyThatStillFailsTheGates`.

### B6 — [MEDIO] Il confronto "Backtesting vs Titano" nel report non è a parità di campione

`BuildOriginalEquity` include **tutti** i trade master. `BuildEquity` scarta i trade
la cui `EntryTimeUtc` non cade in alcun periodo efficace (riga 536,
`period is null → continue`). Il primo periodo di `BuildPeriods` non è mai efficace
e l'ultimo non produce decisione: **i trade entrati in quelle finestre entrano in
una curva e non nell'altra**.

La tabella "Confronto equity" del report mette le due curve fianco a fianco come se
fossero comparabili. Su un run breve — poche decine di periodi — l'esclusione del
primo e dell'ultimo può spostare il verdetto da sola. È un errore di misura, non di
esecuzione, ma è quello su cui si prendono le decisioni.

*Correzione:* o si escludono anche da `OriginalEquity` i trade fuori copertura, o si
dichiara esplicitamente nel report il numero di trade scartati per warmup (il dato
c'è già: `originalEquity.Count` vs `filteredEquity.Count`, ma è presentato come
"trade eliminati da Titano", che è un'altra cosa).


**✅ Corretto.** `BuildEquity` conta i trade la cui `EntryTimeUtc` cade fuori dai
periodi efficaci e li espone in `TitanoRotationManifest.TradesOutsideCoverage`. Il report
HTML, quando il numero è maggiore di zero, stampa un avviso sotto la tabella di confronto
che distingue esplicitamente "trade che Titano non poteva governare" da "trade eliminati
da Titano". Coperto da `ManifestDeclaresTradesOutsideCoverage_...`.

### B7 — [MEDIO] `EquityAt` confonde "equity pari a zero" con "nessun dato"

`CalculateMetrics`, righe 671-672:

```csharp
decimal EquityAt(DateTime time) => points.LastOrDefault(x => x.Time < time).Equity is var value && value != 0
    ? value : request.InitialCapital;
```

Il `value != 0` serve a intercettare il `default` di `LastOrDefault` su una tupla.
Ma se l'equity reale in quell'istante è **esattamente 0** — strategia che ha bruciato
tutto il capitale — la funzione restituisce `InitialCapital`. Il `ShortReturn`
diventa `(0 − 100000)/100000 = −100%` invece del corretto `0/0 → 0`, e la strategia
viene giudicata su un numero inventato.

È un caso limite, ma è raggiungibile e silenzioso.

*Correzione:* usare `points.LastOrDefault(...)` su una lista di tuple nullable, o
tenere un flag esplicito di "trovato".


**✅ Corretto.** `EquityAt` scandisce all'indietro `points` — che parte sempre da
`(DateTime.MinValue, InitialCapital)` — e restituisce il primo punto precedente
all'istante richiesto, qualunque sia il suo valore. Un'equity realmente azzerata non
viene più letta come capitale pieno.

### B8 — [MEDIO] `Get()` non è thread-safe e restituisce un manifest mutabile condiviso

`Get` è invocato **una volta per barra da ogni sessione live e a ogni polling di
ogni account** (lo dice il commento della cache stessa). Due problemi:

1. `manifest.HardStopResets.AddRange(...)` (riga 138) muta una `List<>` non
   sincronizzata. Due thread che entrano insieme sul cache-miss producono una lista
   duplicata o corrotta.
2. L'istanza in cache è restituita per riferimento. Qualunque chiamante che tocchi
   `manifest.Periods` o `HardStopResets` corrompe la cache per tutti gli altri.

*Correzione:* popolare i reset **prima** di inserire in cache, costruendo l'oggetto
una volta sola dentro un `lock` o con `GetOrAdd` su una factory; e restituire una
vista in sola lettura.


**✅ Corretto per la parte che conta.** Il manifest viene ora composto — lettura del
file più merge dei reset — interamente dentro il lock della cartella, con doppio
controllo della cache, e inserito in cache già completo. Il merge dei reset è
idempotente sul `ResetId`, quindi un manifest riletto non li duplica.

*Resta aperto*: l'istanza in cache è ancora restituita per riferimento. Nessun chiamante
attuale la muta, ma renderla immutabile richiede di cambiare i tipi del manifest da
`List<>` a `IReadOnlyList<>`, che tocca serializzazione e contratti. Non è stato fatto
qui per non allargare l'intervento.

### B9 — [BASSO] `CooldownRemaining` mente per le strategie in hard stop

Una strategia hard-stopped ha `on = false` sempre. Al primo hard stop il ramo
`prior?.Enabled == true && !on` arma il cooldown; nei periodi successivi decresce
fino a 0 e ci resta, mentre la strategia è bloccata a tempo indefinito. Chi legge il
manifest vede `CooldownRemaining = 0` e conclude che la strategia è libera di
rientrare. Non lo è. `HardStopped = true` c'è ed è autorevole, ma i due campi si
contraddicono a colpo d'occhio.


**✅ Corretto.** Il cooldown si arma solo per uno spegnimento da regole:
`if (prior?.Enabled == true && !on && !hardStopped)`. Una strategia in hard stop mostra
`CooldownRemaining = 0` in modo coerente con il fatto che il campo non ha significato
quando `HardStopped` è true. Coperto da `HardStopIsLatched_AndDoesNotArmTheCooldown`.

### B10 — [BASSO] `AnomalyFlags` produce falsi positivi strutturali

`DetectAnomalies` segnala `"Enabled=true con soli X/Y filtri minimi superati"`
ogni volta che `MinimumOnPeriods` tiene legittimamente accesa una strategia non
eleggibile. È esattamente il comportamento voluto, segnalato come anomalia. Un
campo diagnostico che grida al lupo perde la sua funzione.

Inoltre il messaggio usa `MinimumPassingFilters` come denominatore ("2/4") dove il
lettore si aspetta `TotalFilters` ("2/5").


**✅ Corretto.** `DetectAnomalies` riceve `mayDisable` e non segnala più il caso in cui
`MinimumOnPeriods` trattiene legittimamente una strategia sotto soglia. Il denominatore
del messaggio è `TotalFilters` e non più `MinimumPassingFilters`. Aggiunto un controllo
sullo stato `Reduced` fuori intervallo. Coperto da
`AnomalyFlags_AreEmptyOnAHealthyRun`.

### B11 — [BASSO] Il walk-forward tace invece di segnalare

`BuildWalkForward` parte da `i = CalibrationPeriods` (8). Se il run ha ≤ 8 periodi,
il ciclo non esegue mai e `manifest.WalkForward` resta vuoto — senza alcuna
indicazione che la validazione non è stata fatta. Il report mostra una tabella
vuota, indistinguibile da "nessun problema rilevato".

Analogamente, l'ultima finestra di valutazione viene troncata
(`Math.Min(periods.Count − 1, ...)`) senza che il risultato dichiari di essere
parziale.


**✅ Corretto.** `TitanoWalkForwardResult.EvaluationTruncated` marca le finestre OOS
accorciate, e `TitanoRotationManifest.WalkForwardNote` spiega una tabella vuota indicando
quanti periodi servirebbero. Entrambi finiscono nel report HTML. Coperto da
`WalkForwardNoteExplainsAnEmptyTable`.

### B12 — [BASSO] Perdite di prestazione e memoria note

- `BuildEquity` fa `decisions.SingleOrDefault(...)` per ogni trade: O(trade × periodi).
- `BuildWalkForward` fa `decisions.Any(...)` dentro un `Where` su tutti i trade,
  per ogni finestra: O(finestre × trade × periodi × strategie).
- `Gates` e `ManifestCache` (`ConcurrentDictionary` statici) crescono senza limite
  né eviction, chiavati su path. Processo lungo con molti workspace = crescita
  monotona.


**✅ Corretto per le due scansioni quadratiche.** `BuildEquity` costruisce un indice
periodo → (strategia → stato) una volta sola; `BuildWalkForward` costruisce un indice
strategia → intervalli abilitati e sostituisce il `decisions.Any(...)` annidato.

**Anche le cache statiche sono ora limitate.** `ManifestCache` ha un tetto di
`ManifestCacheCapacity` (32) voci con politica LRU: l'ordinamento usa un contatore
monotono, non l'orologio di sistema, e sbagliare vittima costa al massimo una rilettura
da disco. `Gates` non è più un dizionario che cresce a ogni run, ma un array di 64 lock
indicizzato sull'hash del percorso: memoria costante e lo stesso percorso mappa sempre
sullo stesso lock. Un dizionario con eviction sarebbe stato *pericoloso* — rimuovere un
lock mentre qualcuno lo detiene farebbe ottenere a due thread oggetti diversi per lo
stesso percorso, cioè nessuna mutua esclusione. Il prezzo dell'array è che due percorsi
diversi possono condividere un lock (1 su 64) e serializzarsi inutilmente: su
un'operazione rara e pesante come `Run` è irrilevante.

Coperto da `ManifestCacheIsBounded_AndKeepsTheMostRecentlyUsedEntry`.

### B13 — [INFO] La macchina a stati non è coperta da test

`TitanoRotationTests` copre calendario, rifiuto dei record legacy, metriche
pre-cutoff, persistenza dell'equity originale e sizing. `TitanoSizingAuditTests`
copre l'applicazione dell'allocazione ai vari confini di esecuzione.

**Nessun test tocca isteresi, cooldown, `MinimumOnPeriods`, il latch dello hard
stop o `ComputePercentileScores`.** È l'unica parte veramente stateful del sistema
ed è anche l'unica non verificata. B4 e B5 sarebbero stati intercettati da due test
di poche righe.

Restano inoltre aperti i due fallimenti già annotati il 29/07:
`RunPersistsOriginalEquityAndReportIncludesComparisonChart` e
`TitanoRunFiltersSignalsThroughHttpBoundary`.


**✅ Corretto.** Aggiunti `Piootoo.Strategies.Tests/TitanoStateMachineTests.cs`
(percentile e allocazione, cooldown, `MinimumOnPeriods`, latch dello hard stop, reset,
copertura, walk-forward) e `Piootoo.Strategies.Tests/MultiAccountDistributionTests.cs`
(fan-out fra gruppi, lucchetto simbolo che sopravvive al fill, template perso dopo un
rifiuto, disaccoppiamento del limite di concorrenza).

`TitanoRotationRequest` è diventato un `record` per poter derivare varianti con `with`
nei test senza ripetere venti parametri.

---

## 5. Titano legacy — rimosso

La versione precedente di questo documento elencava sei difetti di
`TitanoFilterService`, fra cui un lock-out permanente del cooldown (una strategia
spenta una volta non rientrava mai più) e un `MaxRollingDrawdown` da inserire
negativo senza che l'interfaccia lo dichiarasse.

Il servizio, i suoi modelli, i due endpoint e il client `piootoo.titanoclient`
**sono stati rimossi** nello stesso intervento che ha prodotto questo documento.
I difetti restano registrati in `decisioni.md` (2026-07-31) come motivazione della
rimozione, non come lavoro da fare.


## 6. Il JSON è pronto per il backtest del cBot?

### 6.1 Come il cBot usa davvero Titano

Chiarimento importante, perché cambia la natura della verifica: **il cBot non legge
il `manifest.json`.** `PiootooTradingSessionBot` passa tre stringhe alla creazione
della sessione —

```csharp
TitanoRunId          = "weekly-a1b2c3d4e5f6-1a2b3c4d-9f8e7d6c5b4a"
TitanoBacktestFolder = "<nome cartella backtest>"
TitanoMode           = "Disabled" | "BacktestRotationFile" | "Realtime"
ClientRunMode        = IsBacktesting ? "Backtest" : "Realtime"   // non è un parametro
```

— e riceve dal server segnali **già filtrati e già dimensionati**. Il manifest resta
sul filesystem del server e non attraversa mai la rete verso cTrader. Il contratto
da verificare è quindi quello della sessione, non lo schema del manifest.

### 6.2 Cosa funziona

- **Contratto allineato.** `TitanoFilterModeParam` nel cBot replica
  `TitanoFilterMode` lato server con gli stessi tre membri nello stesso ordine, ed è
  serializzato per nome (`JsonStringEnumConverter` senza policy camelCase). Il
  commento nel cBot dichiara esplicitamente il vincolo di allineamento.
- **Validazioni server corrette e per fallimento esplicito.**
  `TradingSessionService` rifiuta: `TitanoRunId` senza `TitanoBacktestFolder`; una
  modalità ≠ `Disabled` senza `TitanoRunId`; `Realtime` con `ClientRunMode.Backtest`
  (sarebbe look-ahead); `BacktestRotationFile` con `ClientRunMode.Realtime`.
  `ClientRunMode` è letto da `IsBacktesting`, non da un parametro — quindi il server
  può fidarsene.
- **Copertura incompleta = eccezione, non silenzio.** Se nessun periodo copre la
  barra e la modalità non è `Disabled`, `PushBars` lancia con un messaggio che
  riporta l'intervallo effettivo del manifest. È la scelta giusta: meglio fermarsi
  che eseguire non filtrato una sessione che l'utente ha chiesto filtrata.
- **Determinismo.** Il `runId` è derivato da `SHA-256(trades.json) + hash
  masterfilter + hash config`. Una richiesta identica restituisce il manifest
  esistente; i file sono scritti con `FileMode.CreateNew` e mai sovrascritti.
- **Applicazione singola.** `GetGroupStrategyAllocation` restituisce 1 quando gruppo
  e sessione condividono lo stesso run — il difetto B3 del 29/07 è chiuso e testato.
- **Sicurezza dei path.** `SafeSegment` blocca `..`, `/` e `\` nel `runId`.

### 6.3 Cosa blocca il test

**Nessun difetto di formato.** Il JSON è schema-versionato (`SchemaVersion = 2`),
completo di config, hash sorgenti, decisioni per periodo, voti, metriche, motivi,
transizioni e curve, più i campi diagnostici aggiunti in questo intervento
(`WalkForwardNote`, `TradesOutsideCoverage`, `EvaluationTruncated`).

I tre blocchi semantici individuati dall'analisi — B3 (parametri fantasma della
form), B4 (nessuna allocazione piena) e B5 (reset che scavalcava i cancelli) —
**sono stati corretti**. Resta un solo vincolo, che non è un bug:

**Copertura temporale.** Il manifest copre `[periods[1].Start, periods[N−1].End)`,
cioè **un periodo in meno all'inizio e uno alla fine** rispetto a
`[StartUtc, EndUtc)`. È la conseguenza diretta del no-look-ahead: il primo periodo
serve solo a misurare, e l'ultimo non ha un periodo successivo su cui applicarsi.

Due implicazioni pratiche:

- Un backtest cTrader che parta da `StartUtc` fallisce sulla prima barra con
  "Nessun periodo Titano copre la barra". **Il backtest deve partire dopo il secondo
  confine di periodo.**
- `EndUtc` va messo alla fine del periodo che si vuole governare, **non alla data
  dell'ultima candela disponibile**. Con dati fino a venerdì 15 maggio e
  `RotationPeriod = Weekly`, per ottenere la rotazione della settimana 18–25 serve
  `EndUtc = 25 maggio`:

  | `EndUtc` | Ultimo periodo generato | Ultima decisione efficace |
  |---|---|---|
  | 15 mag | (11 → 18) | 4 → 11 ✗ |
  | 18 mag | (11 → 18) | 4 → 11 ✗ |
  | 25 mag | (18 → 25) | **18 → 25 ✓** |

  La misura resta corretta in ogni caso: la decisione per 18–25 nasce dai trade con
  `ExitTimeUtc < lunedì 18 00:00 UTC`, cioè tutto fino a venerdì 15 incluso.

### 6.4 Verdetto

> **Formato e contenuto sono pronti**, a condizione di rispettare la copertura
> temporale sopra.
>
> La pipeline `trades.json → manifest → sessione → cBot` è corretta, validata e
> difensiva nei punti giusti. I difetti che avrebbero prodotto "numeri plausibili e
> sbagliati" sono chiusi e coperti da test.
>
> **Resta da eseguire la build.** Le correzioni sono state scritte in un ambiente
> senza `dotnet`: `dotnet build PiootooApp.sln` e la suite di test vanno lanciati da
> Windows prima di generare un manifest destinato a decisioni operative.

## 7. Cosa resta aperto

Nessuno dei difetti B3–B13 è ancora da correggere. Restano due voci, entrambe
consapevolmente fuori scope di questo intervento:

1. **Immutabilità del manifest in cache** (coda di B8). L'istanza è ancora
   restituita per riferimento. Nessun chiamante attuale la muta, ma renderla
   davvero immutabile richiede di passare i tipi del manifest da `List<>` a
   `IReadOnlyList<>`, il che tocca serializzazione e contratti pubblici.
2. **Rigenerazione periodica del manifest** (§0-bis). Serve una procedura — o un
   task schedulato — allineata a `RotationPeriod`. Senza, il live gira su rotazioni
   sempre più vecchie senza fermarsi e senza segnalare altro che una riga di log.

E due questioni di calibrazione, che non sono difetti ma scelte da validare:

3. **`ShortWindowDays = 90` con rotazione settimanale** (§1.2): il periodo di
   decisione è settimanale, la reattività della misura no.
4. **I due fallimenti già annotati il 29/07** —
   `RunPersistsOriginalEquityAndReportIncludesComparisonChart` e
   `TitanoRunFiltersSignalsThroughHttpBoundary` — vanno riverificati dopo la build:
   il primo tocca proprio il report modificato in questo intervento.

## Riferimenti codice

- `Piootoo.Core/Services/TitanoRotationService.cs` — `BuildDecisions` (410-505),
  `ComputeAllocation` (736-749), `ComputePercentileScores` (767-793),
  `Resolve` (336-392), `Get` (118-141), `CalculateMetrics` (650-695)
- `Piootoo.Core/Services/PiootooBacktestingService.cs` — `TitanoBacktestFilter` (1240-1292),
  `CreateTitanoFilter` (1294-1311)
- `Piootoo.Core/Services/TradingSessionService.cs` — `RequireCoherentRunMode` (379-395),
  `PushBars` (440-530)
- `Piootoo.Shared/Models/Optimization/TitanoRotationModels.cs`
- `Piootoo.Shared/Models/Trading/TradingSessionContracts.cs` — `TitanoFilterMode`, `ClientRunMode`
- `Piootoo.Shared/Models/Backtesting/BacktestingRequest.cs` — `TitanoMode` (20-46)
- `piootooapp.clientform/WorkspaceBacktestingForm.cs` — `BuildTitanoTab` (1625),
  costruzione richiesta (3660-3697)
- `piootoo-repository/ctrader/PiootooTradingSessionBot.cs` — parametri (86-110),
  `CreateSession` (551-580), DTO (996-1045)
