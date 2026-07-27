# Verifica codice Piootoo — 27/07/2026

Ambito: loop di backtesting locale, TradingEngine, generazione segnali, trading session con
engine esterno cTrader, filtri Titano, gruppi account anti copy-trading.

---

## 0. Sintesi esecutiva

| # | Area | Problema | Gravità |
|---|------|----------|---------|
| **1** | Trasversale | **Id classe ≠ Name strategia**: `Easy_218_GC_60` vs `TOP_UA_218`. Rompe il join del report di backtest, Titano e la valutazione strategie in sessione | 🔴 Bloccante |
| **2** | Backtest perf | `WriteTrades()` con **fsync su disco a ogni candela** | 🔴 Bloccante |
| **3** | Backtest perf | Slicing candele **O(N) per strategia per barra** (LINQ `Where`+`OrderBy`+`Take`) | 🔴 Bloccante |
| **4** | Datafeed | Repository contiene **solo @GC 5m e 15m**, 29/05→24/07/2026. Tutto il resto = 0 candele, skip silenzioso | 🔴 Bloccante |
| **5** | Titano live | Fuori dal range del manifest ⇒ **tutte le strategie disabilitate, senza errore** | 🔴 Bloccante |
| 6 | Titano live | Il "real time" **non ricalcola** i filtri: rilegge un manifest precalcolato da backtest | 🟠 Gap funzionale |
| 7 | Trading Session | Manca **selezione setup Titano salvato** e **flag applica sì/no** | 🟠 Gap funzionale |
| 8 | Strategie perf | Reflection non cacheata a ogni `Evaluate` (5 walk sui field + Activator + Invoke) | 🟠 Alta |
| 9 | Backtest | `UpdateMarketPrices` saltato quando nessuna strategia è valutata ⇒ SL/TP/time-exit non verificati | 🟠 Alta |
| 10 | Backtest | `IPiootooTradingService` **Singleton** condiviso tra job concorrenti | 🟠 Alta |
| 11 | Sessioni | Stato sessione **solo in RAM**, perso al riavvio del server | 🟠 Alta |
| 12 | Sessioni | `EntryTemplates` senza TTL: un account può reclamare un segnale di giorni prima | 🟡 Media |
| 13 | Sessioni | `Equity`/`Balance` fissi a `InitialCapital` in ExternalBroker ⇒ risk overlay inerte | 🟡 Media |
| 14 | Datafeed | Finestra di prefetch sottostimata (assume densità 24/7) | 🟡 Media |
| — | Gruppi | Meccanismo anti copy-trading **corretto** ✅ | — |
| — | Loop | **Nessuna chiamata HTTP nel loop** ✅ | — |
| — | cTrader | Architettura push-bar / intent / execution-report **corretta** ✅ | — |

---

## 1. 🔴 Il difetto centrale: `Id` ≠ `Name`

Ogni strategia EasyLanguage ha due identificatori diversi:

| Sorgente | Valore | Dove nasce |
|---|---|---|
| `StrategyDefinition.Id` | `Easy_218_GC_60` | nome della classe — `StrategyFactory.cs:54` |
| `ITradingStrategy.Name` | `TOP_UA_218` | campo `_name` della strategia |

Verificato su tutte le 47 strategie: **non coincidono mai**.

Il masterfilter del workspace contiene **Id** (validato in `TradingSessionService.cs:200-203`),
mentre i segnali e i trade persistiti portano **Name** (`StatelessEasyStrategyBase.EnrichSignal`,
riga 74 → `signal.StrategyCode = Name`).

### 1.a — Il report di backtest mostra 0 trade anche quando i segnali ci sono

`PiootooBacktestingService.cs:519`
```csharp
StrategiesInfo = createdStrategies.Select(item => new StrategyInfo
{
    Name       = item.Definition.Name,   // TOP_UA_218
    StrategyCode = item.Definition.Id,   // Easy_218_GC_60  ← incoerente
    ...
```

In `AppendStrategyEquityResults` (riga 987) si costruiscono due chiavi:

* dai segnali → `MakeStrategyKey(signal.Symbol, signal.StrategyCode)` = `GC|TOP_UA_218`
* da `StrategiesInfo` → `MakeStrategyKey(info.Symbol, info.StrategyCode)` = `GC|Easy_218_GC_60`

**Non combaciano mai.** Conseguenze a cascata:

* `signalsByKey.TryGetValue(...)` fallisce sempre → `Signal = null`, `Contracts = 0`, `EntryPrice = null` su **ogni** riga di `StrategyResults`;
* `snapshot.StrategyEquities.TryGetValue(strategyKey)` fallisce sempre (le chiavi lì sono `positionKey` = `GC|TOP_UA_218`) → l'equity per strategia resta **piatta a `InitialCapital`**, `Profit = 0`;
* riga 818: `TotalTrades = StrategyResults.Count(sr => sr.Signal.HasValue && ...)` → **sempre 0**;
* `CalculateWeeklyResults`: `TotalTrades = 0`, `WinRate = 0`;
* il report HTML mostra linee piatte e nessun marker di segnale.

> **È esattamente questo il "sembra che le strategie non diano segnali".**
> **Verifica immediata:** apri `signals.json` e `trades.json` nella cartella del backtest.
> Se sono popolati ma il report dice 0 trade, è confermato.

### 1.b — Titano disabilita sempre tutto

`TitanoRotationService.BuildDecisions` (riga 183):
```csharp
var rows = trades.Where(t => t.StrategyCode.Equals(code, ...))   // code = Id, t.StrategyCode = Name
```
→ 0 trade per ogni strategia → `metrics.Trades = 0` → il voto `short-performance` fallisce
(`metrics.Trades >= MinimumTrades` falso) → `passing < MinimumPassingFilters` → `eligible = false`
→ `disable = true` → `on = false`. **Tutte le strategie risultano permanentemente disabilitate in ogni run Titano.**

### 1.c — La sessione con Titano non valuta nessuna strategia

`TradingSessionService.cs:316`:
```csharp
evaluationStrategies = session.Strategies
    .Where(x => effective.EffectiveStrategies.Contains(x.Name, ...));   // Name vs lista di Id
```
→ insieme vuoto → **zero segnali in tutte le sessioni collegate a un run Titano**.

### Fix consigliato

Un solo identificatore canonico. La strada più pulita è usare **l'Id della classe come
`StrategyCode` ovunque**, lasciando `Name` come sola etichetta di visualizzazione:

1. `StatelessEasyStrategyBase.EnrichSignal` → `signal.StrategyCode = <Id>` (esporre l'Id sull'interfaccia, es. `string Code => GetType().Name`).
2. `StrategyEvaluationService.Prepare` (`TradingSessionService.cs:63`) → idem.
3. `PiootooBacktestingService` → `GetExecutionSnapshot`/`CaptureStrategyRuntimeState` con l'Id.
4. `PushBars` riga 316 → confrontare sull'Id.
5. Migrazione: i `trades.json` esistenti hanno `StrategyCode = Name` — serve un mapping Name→Id una tantum, altrimenti i run Titano storici restano rotti.

Fix minimo tampone (solo per sbloccare il report di backtest): riga 519 →
`StrategyCode = item.Instance.Name`. **Non risolve** Titano né le sessioni.

---

## 2. Backtest locale — il loop

### ✅ Confermato: nessuna chiamata HTTP per candela

`ExecuteBacktesting` (`PiootooBacktestingService.cs:403`) gira in `Task.Run`, il `while` di riga 572
è interamente in-process. `PiootooDataFeedService` legge file JSON locali tramite
`DataSourceRepository` — nessun HTTP. Il client fa solo polling dello stato del job. Requisito rispettato.

L'unica `await` nel loop è il fallback di riga 630 (cache miss), che comunque è I/O su file.

### 🔴 2.a — fsync su disco a ogni candela

`PiootooBacktestingService.cs:761-763`
```csharp
if (signals.Count != 0)
    tradingJsonStore.WriteSignals(ToPersistedSignals(job.JobId, emittedTradeSignals));
tradingJsonStore.WriteTrades(ToPersistedTrades(job.JobId, _tradingService.GetClosedTrades()));
```

`WriteTrades` è eseguito **incondizionatamente a ogni iterazione**, e la catena è:
`ToArray` → validazione → `DistinctBy` → `JsonSerializer` con `WriteIndented = true` sull'intero array →
`AtomicFileWriter.Write` con `FileOptions.WriteThrough` + `stream.Flush(flushToDisk: true)` + `File.Replace`.

**Una sincronizzazione forzata su disco per ogni barra.** Su 100.000 barre sono 100.000 fsync:
a 5-15 ms l'uno (tipico su Windows con WriteThrough) sono **8-25 minuti di sola I/O**, più la
riserializzazione completa che cresce linearmente → costo complessivo O(n²) sui byte scritti.
`WriteSignals` ha lo stesso problema ogni volta che c'è un segnale.

**Fix:** scrivere solo a fine job, più eventuali checkpoint ogni N barre o ogni X secondi; per i
checkpoint intermedi togliere `WriteThrough`/`flushToDisk` e `WriteIndented`.
Da solo questo intervento vale probabilmente un ordine di grandezza.

### 🔴 2.b — Slicing candele O(N) per strategia per barra

`PiootooBacktestingService.cs:619-624`
```csharp
candles = cachedData
    .Where(d => d.DateTime <= currentDate)
    .OrderByDescending(d => d.DateTime)
    .Take(requiredCandles)
    .OrderBy(d => d.DateTime)
    .ToArray();
```

`cachedData` è l'intera serie precaricata. Ogni chiamata scansiona tutto l'array, bufferizza per
l'ordinamento e alloca un nuovo array. Con 100k candele, 20 strategie e 100k iterazioni si arriva a
~10¹¹ operazioni elementari e ~2M allocazioni di array.

I dati sono **già ordinati** e `currentDate` è **monotona crescente**: basta un cursore per
`(Symbol, Timeframe)` che avanza e restituire una `ArraySegment<OhlcvData>` / `ReadOnlySpan`.
Costo ammortizzato O(1) per barra.

### 🔴 2.c — `GetAdditionalTimeframeData`

`PiootooBacktestingService.cs:1620-1642` — stesso pattern, ma peggiore: restituisce **tutto il
prefisso** della serie (non gli ultimi N), riallocandolo a ogni barra. Per le strategie
multi-timeframe è il collo di bottiglia dominante.

### 🟠 2.d — Reflection non cacheata in `StatelessEasyStrategyBase`

`StatelessEasyStrategyBase.Evaluate` (riga 34), **per ogni valutazione**:

* `Activator.CreateInstance(GetType())`
* 5 percorrenze complete di `GetInstanceFields` (`CopyFields`, `RestoreRuntimeState`,
  `SetMarketPositionFields`, `SetEntriesTodayFields`, `CaptureRuntimeState`) — nessuna cacheata
* `GetType().GetMethod(...)` + `MethodInfo.Invoke`
* boxing di ~40 field in un `Dictionary<string, object?>`

In più, `Name`, `Symbol`, `TimeframeMinutes`, `RequiredCandles` sono implementazioni esplicite che
fanno `GetType().GetProperty(...)` + `GetValue()` **a ogni lettura** (`ReadProperty`, riga 81), e nel
loop vengono lette più volte per barra.

**Fix:** `static ConcurrentDictionary<Type, FieldInfo[]>`, `MethodInfo` cacheato, delegate compilati
(`Delegate.CreateDelegate` / espressioni) per le property. Atteso 5-20× su questa componente.

### 🟡 2.e — `OHLCMulti5` ricalcola tutto a ogni barra

`EasyLib.cs:48-55`: `Where` + `OrderBy` + `ToArray` sulla finestra, poi ciclo completo sui bar, più
5 array da 20 e uno da 24 allocati ogni volta. La finestra arriva già filtrata e ordinata dal chiamante:
`Where`/`OrderBy` sono ridondanti.

### 🟡 2.f — Log e accumulo in memoria

* `Console.WriteLine` sincrono per ogni segnale generato (riga 691) + log periodici.
* `AppendStrategyEquityResults` fa `GroupBy` + `ToDictionary` + `OrderBy` su `StrategiesInfo`
  **a ogni barra** e aggiunge una riga per strategia: 20 strategie × 100k barre = 2M oggetti in RAM,
  poi serializzati in JSON e **inlinati nell'HTML**.

### 🟠 2.g — BUG: mark-to-market saltato

`PiootooBacktestingService.cs:748-759`: se in quella iterazione nessuna strategia è stata valutata,
`currentPrices` è vuoto e il codice fa `continue` **senza chiamare `UpdateMarketPrices`**.
Stop loss, take profit e time exit vengono quindi verificati **solo sulle barre in cui una strategia
gira**, e solo per i simboli valutati in quella barra. In un portafoglio multi-simbolo /
multi-timeframe le uscite sono sistematicamente ritardate.

### 🟠 2.h — BUG: `IPiootooTradingService` è Singleton condiviso

`Program.cs:25` registra `PiootooTradingService` come singleton, e `PiootooBacktestingService`
(anch'esso singleton) chiama `_tradingService.Initialize(...)` all'avvio di ogni job (riga 505).
`StartBacktesting` blocca solo la stessa cartella di output (`_activeOutputPaths`), quindi **due
backtest su cartelle diverse girano in parallelo sullo stesso motore mutabile** → posizioni e trade
si mescolano.
`Initialize` pulisce correttamente lo stato, quindi le esecuzioni **sequenziali** sono a posto: il
problema è solo la concorrenza. **Fix:** istanziare un `new PiootooTradingService()` per job, come già
fa correttamente `TradingSessionService.Create` (riga 222).

### 🟡 2.i — `TradingEngine` legacy

`TradingEngine.RunBacktestWithWeeklyRotation` (riga 78) contiene
`data.Where(d => d.DateTime <= date).ToArray()` **dentro il loop per barra** — quadratico.
La classe non risulta usata dal percorso API (il backtest reale è `PiootooBacktestingService`):
va rimossa o marcata `[Obsolete]` per evitare che venga scambiata per il motore attivo.

---

## 3. 🔴 Perché le strategie non producono segnali — cause aggiuntive

Oltre al difetto §1 (che nasconde i segnali *dopo* che sono stati generati), ci sono cause che li
impediscono *a monte*.

### 3.a — Il datafeed copre solo @GC 5m e 15m

`piootoo-repository/datafeed/` contiene esclusivamente:

| File | Symbol | TF | Barre | Da | A |
|---|---|---|---|---|---|
| `GCF_5.json` | GC=F | 5 | 10.782 | 2026-05-29 | 2026-07-24 |
| `GCF_15.json` | GC=F | 15 | 3.599 | 2026-05-29 | 2026-07-24 |

`DataSourceRepository.BuildFeedFileName` risolve `{TICKER}_{minuti}.json`. Quindi:

* tutte le strategie **@NQ, @CL, @FDAX** → file inesistente → `Array.Empty` → `candles.Length < RequiredCandles` → **skip silenzioso a ogni barra**;
* **@GC su 30m / 60m / Daily** → stessa cosa (nessun `GCF_30.json`, `GCF_60.json`, `GCF_1440.json`);
* funzionano solo `Easy_*_GC_5` e `Easy_*_GC_15`, e solo nella finestra ~8 settimane.

Il limite di ~57 giorni è il tetto di Yahoo Finance sui dati intraday.

Nel log compare `Dati insufficienti per {strategia} (richiesti: 100, disponibili: 0)`, ma solo
ogni 100 iterazioni — passa inosservato.

**Fix:** far fallire il job (o riportarlo esplicitamente nel risultato) se il prefill restituisce
0 candele per una coppia `(Symbol, Timeframe)` presente nel masterfilter.

### 3.b — `RequiredCandles = 100` hardcoded ovunque

Tutte le strategie generate hanno `public int RequiredCandles => 100; // TODO: Calcolare in base alla strategia`.

Per una strategia GC a 5 minuti, 100 barre ≈ 8 ore, **meno di una sessione**. Ma `OHLCMulti5`
serve a ricostruire l'OHLC delle **5 sessioni precedenti**: con una finestra da 100 barre a 5m,
`ohlcValues[4..23]` restano a 0 e `PatternFast` confronta contro zero → le condizioni non si
verificano mai (o si verificano sempre).

Conseguenza correlata: `_mycount` viene azzerato solo quando `OHLCMulti5` rileva l'inizio sessione.
Se la finestra non contiene il confine di sessione, `_mycount` cresce all'infinito e la condizione
`_mycount == _myLEBar` (16) si verifica **una sola volta nell'intero backtest** → al massimo 1 trade.

**Fix:** derivare `RequiredCandles` dal lookback reale (`sessioni × barre_per_sessione`), o almeno
imporre ≥ 6 sessioni per la famiglia `OHLCMulti5`.

### 3.c — Finestra di prefetch sottostimata

`PiootooDataFeedService.cs:45`
```csharp
var daysBack = Math.Max(30, (numberOfCandles * timeframeMinutes) / (24 * 60) + 7);
```
Assume densità **24/7**. I future hanno ~5 giorni su 7 e sessioni non continue: lo span di calendario
realmente necessario è ~1,4× quello calcolato. Il `startDate` risulta troppo recente e
`LoadDataRangeAsync` **taglia l'inizio del periodo richiesto** — nella pratica il primo ~30% della
finestra di backtest può restare senza dati.

**Fix:** per il prefill usare direttamente `LoadAllDataAsync`, oppure passare
`request.StartDate - buffer` esplicito invece di derivare i giorni dal numero di candele.

### 3.d — Disallineamento della barra per strategie con TF > minTimeframe

`roundedStartDate` è arrotondato **solo** a `minTimeframeMinutes` (riga 531) e
`ShouldEvaluateStrategy` usa `iterationCount % multiplier == 0` (riga 1593), contando anche le
iterazioni di weekend saltate.

Una strategia a 60m in un portafoglio con minimo 5m viene quindi valutata a 09:05, 10:05, 11:05…
non sull'ora. Il `currentDate` passato a `GenerateSignal` **non è un confine di barra reale**.
Le strategie che ragionano su `currentDate.Hour*100 + Minute` (finestre di sessione, `TwBars`,
`_mycount`) lavorano su orari sfasati.

**Fix strutturale:** guidare il loop dai timestamp reali delle candele (merge event-driven degli
stream per simbolo/timeframe) invece che da un orologio sintetico.

### Diagnostica da aggiungere

Contatori per strategia — valutazioni, skip per dati insufficienti, skip per candela stale, Hold,
Buy/Sell — esposti nel `BacktestingResult`. Oggi l'unica evidenza sono `Console.WriteLine`
campionati ogni 100/500 iterazioni, che nascondono il problema invece di mostrarlo.

---

## 4. Trading session con engine esterno cTrader

### ✅ Architettura corretta

`PiootooTradingSessionBot.OnBar()` → `PushClosedBar()` → `POST /api/v1/trading-sessions/{id}/bars`
con la **sola barra chiusa**; il server valuta le strategie e restituisce gli `Intents`.

Le uscite sono gestite su due canali, entrambi corretti:

1. **Uscite dal server** — intent `CloseOnly` emessi dalle strategie.
2. **Uscite decise dal client** — SL/TP nativi del broker o limite barre: `OnPositionClosed` →
   `RegisterExternalCloseAndReport` → `POST /intents/close-external` → `POST /execution-reports`.

In entrambi i casi `ApplyReport` (riga 476-509) genera il `PersistedTrade` che finisce in
`trades.json`, che è la sorgente per Titano. Il requisito è soddisfatto.

### 🟠 4.a — Le sessioni vivono solo in RAM

`TradingSessionService._sessions` (riga 173) è un `ConcurrentDictionary` **mai ricaricato da disco**.
Un riavvio del server perde sessioni, posizioni, intent, gruppi account e `StrategyHolderCounts`.
Il cBot salva `SessionId`+`Token` per riprendere, ma il server risponde 404 e la riconciliazione
fallisce. `Persist()` scrive solo signals/trades/rotation-log, **non lo stato di sessione**.

### 🟡 4.b — `EntryTemplates` senza scadenza

`session.EntryTemplates` (riga 155) cresce senza limite e i template restano `Pending` per sempre,
a meno che la strategia non abbia valorizzato `ExpiresAtUtc` — cosa che nessuna strategia fa.
Un account che si libera ore dopo può reclamare in `GetNextSignalForAccount` (riga 648) un ingresso
generato giorni prima, a un prezzo ormai irrilevante.
**Fix:** TTL di default (N barre o N minuti) e purge periodico.

### 🟡 4.c — `Equity` e `Balance` bloccati su `InitialCapital`

`TradingSessionService.cs:1003-1004`: in modalità `ExternalBroker` lo snapshot restituisce sempre
`InitialCapital`, pur avendo il P&L realizzato in `session.ExternalTrades`.
Di conseguenza `session.PeakEquity` non si muove mai e il position sizing per rischio di portafoglio
(drawdown cap, CPPI) lavora su equity costante: **l'overlay di rischio è di fatto inerte in live**.

### 🟡 4.d — `GrossExposureFraction`

Riga 340: usa sempre `session.ExternalPositions`, anche in `ServerSimulated` dove è sempre vuoto → 0.

### 🟡 4.e — `EntriesToday` non è "today" né per strategia

`GetExecution` (riga 954) passa `session.Entries`, un contatore **globale di sessione**, come
`EntriesToday` a ogni strategia. Nel backtest invece è corretto (`_entriesByDay` per positionKey e
per giorno). Le strategie che limitano gli ingressi giornalieri si comportano in modo diverso
tra backtest e live.

### 🟡 4.f — Validazione barre

`ValidateBar` non verifica che `BarTimeUtc` sia allineato a `TimeframeMinutes`, e `PushBars` accetta
simboli/timeframe non presenti nella sessione: vengono semplicemente ignorati in `Evaluate`,
in silenzio.

---

## 5. Titano

Esistono **due implementazioni distinte e scollegate** di "Titano":

| | `TitanoFilterService` + `TitanoSetupService` | `TitanoRotationService` |
|---|---|---|
| Input | `BacktestingResult` (equity settimanale) | `trades.json` di una cartella backtest |
| Config | `TitanoFilterRequest` | `TitanoRotationRequest` |
| Setup salvati | ✅ `GET/POST /titano/setups` | ❌ nessuna persistenza |
| Usato da | `POST /titano/apply-filter` | Trading session (`TitanoRunId`) |

**È questa la ragione per cui il tab Trading Session non può selezionare un "setup Titano salvato":
i setup persistiti appartengono al primo modello, le sessioni usano il secondo.**

### ✅ 5.a — Titano su backtest locale funziona

`Run()` legge `trades.json`, costruisce i periodi, calcola decisioni, equity filtrata e
walk-forward con warning `InSampleOnlyImprovementWarning`. Il `runId` è un hash di
trades+masterfilter+config, quindi riproducibile e idempotente. Buon design.

Nota: `BuildDecisions` produce `periods.Count - 1` decisioni e il **primo periodo non è mai coperto**
(non ha storia su cui calibrare). `Resolve` su un timestamp nel primo periodo restituisce `null`
→ tutto disabilitato. È intenzionale, ma andrebbe reso esplicito invece che silenzioso.

### 🔴 5.b — Fuori dal range del manifest, tutto disabilitato senza errore

`TitanoRotationService.cs:125`
```csharp
var period = manifest.Periods.SingleOrDefault(x => timestampUtc >= x.EffectiveFromUtc && timestampUtc < x.EffectiveToUtc);
var enabled = period?.Strategies.Where(x => x.Enabled)... ?? [];
```

Un manifest costruito su un backtest storico **termina nel passato**. In live il `barTimeUtc` è oltre
l'ultimo periodo → `period == null` → `enabled = []` → `EffectiveStrategies = []` →
`evaluationStrategies` vuoto → **la sessione non valuta mai nessuna strategia e non produce alcun
segnale**, senza sollevare errori.

**Fix:** stato esplicito "nessun periodo attivo" (errore o warning nel `RotationLog`), oppure
estensione automatica dell'ultimo periodo, oppure — meglio — ricalcolo (vedi 5.c).

### 🟠 5.c — Il "calcolo in tempo reale" non c'è

`TradingSessionService.cs:311`
```csharp
effective = service.Resolve(session.WorkspaceId, session.TitanoBacktestFolder!, session.TitanoRunId, bar.BarTimeUtc);
```

`Resolve` è una **pura lettura** di un manifest precalcolato. I trade live accumulati in
`session.ExternalTrades` **non rientrano mai** nella rotazione. In modalità external engine Titano
viene quindi *riprodotto*, non *ricalcolato*.

**Per ottenere il comportamento richiesto:** al confine di ogni periodo di rotazione, ricalcolare le
metriche da `session.ExternalTrades` (eventualmente con lo storico del backtest come seed) usando
`TitanoRotationService.CalculateMetrics` / `EvaluateVotes` / `SelectMultiplier` — sono già
`public static`, quindi riutilizzabili così come sono. Manca solo l'orchestrazione lato sessione
e la persistenza dello stato precedente (`TitanoStrategyState`) tra i periodi.

### 🔴 5.d — `Resolve` rilegge il manifest da disco a ogni barra

`Resolve` → `Get` → `ReadManifest` (deserializzazione JSON completa) + `ReadResets`
(enumerazione directory) + `_workspaces.GetMasterFilter` (altra lettura file).
**Per ogni barra, per ogni sessione.** Nessuna cache.
`ComputeStrategyPriority` (riga 734) fa la stessa cosa a ogni poll di ogni account.

**Fix:** cache in memoria per `(workspaceId, backtestFolder, runId)` invalidata sul timestamp del file.

### 🟠 5.e — Manca la UI: setup salvato + flag applica sì/no

`WorkspaceBacktestingForm.cs:1717-1722`
```csharp
TitanoRunId          = string.IsNullOrWhiteSpace(_sessionTitanoRunId.Text) ? null : _sessionTitanoRunId.Text.Trim(),
TitanoBacktestFolder = string.IsNullOrWhiteSpace(_sessionTitanoRunId.Text) ? null : _sessionTitanoBacktest.Text.Trim(),
```

Il tab espone due sole textbox libere. Titano si applica **implicitamente** se `TitanoRunId` non è
vuoto: non c'è un interruttore esplicito. Da notare anche che la seconda riga testa
`_sessionTitanoRunId` per decidere su `_sessionTitanoBacktest` — copia-incolla, funziona per caso.

**Da aggiungere:**

1. `bool ApplyTitanoFilters` in `CreateTradingSessionRequest`; lato server in `PushBars`:
   `if (!session.ApplyTitanoFilters) evaluationStrategies = session.Strategies;`
   (mantenendo comunque il `RotationLog` per diagnostica).
2. Una `ComboBox` popolata da `GET /api/v1/titano/rotations?workspaceId=…&backtestFolder=…`
   (l'endpoint esiste già, `TitanoController` riga 84) al posto della textbox libera per il RunId.
3. Se si vuole davvero "selezionare un setup Titano salvato": estendere `TitanoSetupService` a
   persistere anche i `TitanoRotationRequest`, oppure unificare i due modelli di configurazione (§5).

### 🟡 5.f — `TitanoEnabledStrategies` semanticamente ambiguo

`Resolve` riga 146 sovrascrive `enabled` con `states.Where(AllocationMultiplier > 0)`, dove `states`
è **già** filtrato per masterfilter. Quindi `TitanoEnabledStrategies` (che il nome suggerisce essere
pre-masterfilter) coincide con `EffectiveStrategies`. Ininfluente sul comportamento, fuorviante in
diagnostica.

---

## 6. ✅ Gruppi account — anti copy-trading

Il meccanismo è **implementato correttamente** e rispetta il requisito.

Strutture in `TradingSessionService.Session` (righe 149-170):

| Struttura | Ruolo |
|---|---|
| `AccountGroups` | `AccountNumber → GroupId` |
| `EntryTemplates` | segnali di ingresso non ancora assegnati |
| `TemplateClaimedGroups` | `IntentId → {gruppi che l'hanno già preso}` |
| `GroupStrategySlots` | `(gruppo, strategia, simbolo) → account che lo detiene` |
| `AccountActiveIntent` | `(account, simbolo) → intent attivo` |
| `CanonicalPositions` | posizione di riferimento per la valutazione strategie |

Flusso in `GetNextSignalForAccount` (riga 618):

1. se l'account ha già un intent `Pending` assegnato, lo ripropone (poll idempotente);
2. filtra i template scartando quelli **già reclamati dal proprio gruppo** (`TemplateClaimedGroups`) e
   quelli il cui **slot (gruppo, strategia, simbolo) è già occupato** (`GroupStrategySlots`);
3. auto-limitazione: un solo ingresso per account **per simbolo** (`AccountActiveIntent`), che
   permette correttamente posizioni parallele su simboli diversi;
4. ordina per priorità Titano (o P&L live come fallback) e clona il template in un intent concreto
   assegnato a `(account, gruppo)`.

**Risultato: dentro lo stesso gruppo una strategia va a un solo account; gruppi diversi ricevono
copie indipendenti.** Esattamente la protezione anti copy-trading richiesta per le prop.

Anche il rilascio degli slot è gestito bene:

* fill di chiusura → `ApplyReport` righe 513-531 libera slot, auto-limitazione e decrementa `StrategyHolderCounts`;
* ingresso rifiutato/annullato con `FilledQuantity == 0` → righe 452-462, stessa liberazione (evita slot bloccati per sempre);
* le chiusure fanno **fan-out** su tutti i gruppi che detengono la posizione (righe 354-369).

Osservazioni minori:

* `SetAccountGroups` **azzera e riscrive** l'intera mappa (riga 602) senza verificare che gli account rimossi non abbiano posizioni aperte o slot occupati → slot orfani in `GroupStrategySlots`/`AccountActiveIntent`.
* La mappa gruppi è in RAM come tutto il resto della sessione (§4.a): si perde al riavvio.
* Nessun vincolo di capienza per gruppo: se un gruppo ha 3 account e arrivano 3 template su simboli diversi, li prende un account solo? No — l'auto-limitazione è per simbolo, quindi lo stesso account può prenderli tutti e tre. Se l'intento è distribuire il carico tra gli account di un gruppo, serve un criterio aggiuntivo (round-robin o cap di posizioni per account).

---

## 7. Ordine di intervento consigliato

**Sblocco funzionale (nell'ordine)**

1. **§1** — unificare `StrategyCode` su un identificatore unico (Id classe). Sblocca report di backtest, Titano e sessioni Titano in un colpo solo. Prevedere la migrazione dei `trades.json` esistenti.
2. **§3.a** — fail-fast sul datafeed mancante + scaricare i timeframe/simboli effettivamente usati dal masterfilter.
3. **§5.b** — gestione esplicita del "nessun periodo attivo" in `Resolve`.
4. **§3.b/3.c** — `RequiredCandles` reale e finestra di prefetch corretta.

**Performance (nell'ordine di impatto)**

5. **§2.a** — togliere `WriteTrades`/`WriteSignals` dal loop per barra.
6. **§2.b/2.c** — cursore incrementale al posto dello slicing LINQ.
7. **§2.d** — cache della reflection in `StatelessEasyStrategyBase`.
8. **§2.f** — ridurre logging e accumulo in RAM.
9. **§5.d** — cache del manifest Titano.

**Correttezza**

10. **§2.g** — `UpdateMarketPrices` sempre, con i prezzi di tutti i simboli della barra.
11. **§2.h** — motore di trading per-job invece che singleton.
12. **§4.a** — persistenza dello stato di sessione.
13. **§4.c** — equity reale in `ExternalBroker`.

**Funzionalità richieste**

14. **§5.e** — flag "applica filtri Titano" + combo dei run/setup nel tab Trading Session.
15. **§5.c** — ricalcolo Titano in tempo reale dai trade live.
