# Piootoo — descrizione del progetto

> Documento di riferimento: cosa fa il sistema, com'è organizzato, quali sono le
> invarianti da non rompere. Scritto per essere riletto e migliorato nel tempo.
> Ultimo aggiornamento: 2026-07-27.

---

## 1. Cos'è

Piootoo è un **trading system per future** che copre tre attività:

1. **Backtesting locale** su datafeed su file, con motore di esecuzione interno.
2. **Trading live con engine esterno cTrader**: le strategie girano lato server,
   l'esecuzione è del broker.
3. **Titano**: filtro di rotazione che decide, periodo per periodo, quali strategie
   sono abilitate e con quale allocazione, sulla base dei trade realmente eseguiti.

A queste si aggiunge il **setup del workspace**, che è il contenitore di tutto:
selezione strategie (masterfilter), backtest prodotti, run Titano, sessioni.

L'interfaccia operativa è una console WinForms che parla solo via HTTP con l'API
ASP.NET Core. Ne esistono due: quella nuova (`Shell/MainShellForm`, menu a
sinistra e navigazione lista → dettaglio) e quella storica a tab
(Workspaces, Backtesting, Titano, Trading Session e altri), che resta operativa
finché la prima non copre tutte le funzioni. Vedi `decisioni.md` (2026-08-03).

---

## 2. Mappa dei progetti

| Progetto | Ruolo | Dipende da |
|---|---|---|
| `Piootoo.Shared` | Modelli e contratti. Nessuna logica. | — |
| `Piootoo.Domain` | Repository di base, in particolare `DataSourceRepository` (lettura feed). | Shared |
| `Piootoo.Core` | Tutti i servizi applicativi: workspace, backtesting, trading engine, sizing, Titano, sessioni. | Shared, Domain, Strategies |
| `Piootoo.Strategies` | Catalogo strategie (`ITradingStrategy`), incluse quelle generate da EasyLanguage. | Shared |
| `PiootooApp.Server` | API HTTP. Solo controller sottili + DI. | Core |
| `Piootoo.FeedWorker` | Worker che alimenta le sessioni live con barre chiuse. | Core |
| `piootooapp.clientform` | Console WinForms: shell nuovo con menu e navigazione lista → dettaglio, più la vecchia finestra a tab. Client HTTP puro. | Shared |
| `piootooapp.client` | SPA Angular, scollegata dal debug F5. | — |
| `piootoo-repository/` | Dati fuori dal codice: `datafeed/` (JSON OHLCV), `ctrader/` (sorgenti cBot), `easy/` (sorgenti EasyLanguage). | — |

Regola: `Piootoo.Shared` non deve mai dipendere dagli altri progetti. I controller
non contengono logica: traducono eccezioni in `ProblemDetails` e delegano.

---

## 3. Concetti chiave

### 3.1 Workspace e masterfilter

Un **workspace** è una cartella su disco che contiene un `masterfilter.json`
(l'elenco delle strategie abilitate), una cartella `backtests/`, una `plans/` e una `sessions/`.

Il masterfilter contiene **Id di classe** (es. `Easy_218_GC_60`): è la chiave di
*selezione* dal catalogo. È l'unica fonte autorevole di quali strategie girano —
sia in backtest sia in sessione.

#### Piani e sessioni

Un **piano di trading** è una configurazione riutilizzabile del workspace: nome e codice,
gruppo, account, massimo trade concorrenti, setup/run Titano, flag di applicazione, sizing e
metadata strumenti. Il codice è globale perché è l'unico parametro funzionale configurato nel
cBot. I piani sono persistiti in `<workspace>/plans/plans.json`.

Una **sessione** resta invece l'istanza mutabile di una singola esecuzione. Alla creazione
acquisisce uno snapshot del piano: modificare il piano non altera una sessione già aperta.
`PlanCode + ClientRunMode + ExecutionKey` è la chiave idempotente usata dal cBot per riprendere
la stessa sessione dopo un'interruzione. Dettaglio in
[`domini/trading-plans.md`](domini/trading-plans.md).

### 3.2 Identificazione delle strategie — invariante importante

Ogni strategia ha due identificatori e **non vanno confusi**:

| | Valore | Uso |
|---|---|---|
| **Id** | nome della classe, es. `Easy_218_GC_60` | *selezione*: masterfilter, catalogo, `StrategyFactory` |
| **Name** / **StrategyCode** | es. `TOP_UA_218` | *esecuzione*: segnali, trade, posizioni, Titano, report |

> **Invariante:** tutto ciò che è persistito nel dominio dell'esecuzione
> (`signals.json`, `trades.json`, `StrategyHourlyResult.StrategyCode`,
> `PersistedTrade.StrategyCode`, chiavi di posizione, stati Titano) usa **Name**.
> Tutto ciò che seleziona strategie dal catalogo usa **Id**.
> Ogni volta che si confronta il masterfilter con dati di esecuzione bisogna
> passare per la risoluzione Id → Name (`StrategyCatalog.ResolveCodes`).

I `Name` sono univoci nel catalogo (verificato). Un tempo questa distinzione non
era rispettata, e l'incoerenza rendeva vuoti sia il report di backtest sia le
rotazioni Titano — vedi `decisioni.md` 2026-07-27.

### 3.3 Chiave di posizione

`positionKey = "{SYMBOL}|{StrategyCode}"` con il simbolo normalizzato
(trim, `@` rimosso, maiuscolo). Isola le posizioni per strategia: due strategie
sullo stesso simbolo hanno posizioni indipendenti.
Sorgente: `PiootooTradingService.MakePositionKey`.

### 3.4 Segnale, intent, trade

* **`TradeSignal`** — cosa vuole la strategia. Prodotto da `ITradingStrategy.Evaluate`.
  Non conosce quantità finale né account. **Descrive per intero anche come si esce**:
  `StopLoss`, `TakeProfit`, `BreakEven`, `CloseAtUtc`, `MaxBarsInPosition`.
* **`OrderIntent`** — cosa il server ordina di fare, con quantità già dimensionata
  ed eventuale assegnazione a un account. Esiste solo nelle sessioni.
* **`PersistedTrade`** — cosa è realmente successo: entry, exit, P&L netto.
  È **l'unico input di Titano**.

Un intent rifiutato o mai eseguito non genera trade e quindi non entra nei calcoli
a valle. Questo è voluto.

### 3.5 Uscita autocontenuta — invariante

Un segnale di ingresso deve descrivere da solo come si esce. L'engine — interno o esterno —
chiude usando **soltanto** quelle informazioni: stop loss, take profit, break even, uscita a
tempo (`CloseAtUtc`) e numero massimo di barre (`MaxBarsInPosition`).

Non esiste più un segnale di sola chiusura: il meccanismo `CloseOnly` è stato rimosso da
`TradeSignal`, `OrderIntent`, engine, sessioni e cBot. Le strategie che decidevano l'uscita a
runtime — verificando un pattern barra per barra — dichiarano `IsPositionCloseDependent => true`
e sono **escluse dal catalogo**: `StrategyFactory.GetRegisteredStrategies` non le restituisce e
`CreateStrategy` le rifiuta con un errore esplicito, così un masterfilter salvato in passato non
può riportarle in esecuzione di nascosto. L'elenco degli esclusi è in
`StrategyFactory.GetCloseDependentStrategyIds`.

Le uscite a tempo **non** rendono close-dependent una strategia: vanno espresse come `CloseAtUtc`
o `MaxBarsInPosition` sul segnale di ingresso.

Un ingresso privo di qualsiasi condizione di uscita non è un errore bloccante, ma è un difetto:
la posizione resterebbe aperta fino alla chiusura tecnica di fine settimana o fine run. Viene
contato in `backtest-summary.json` (`signalsWithoutExitSpec`) e segnalato tra i `diagnostics`.

**Fine settimana: né posizioni né ordini.** Con `CloseAllPositionsAtWeekEnd` (default `true`) il
backtest, sull'ultima barra della settimana, chiude le posizioni *e* cancella gli ordini pendenti:
la sola chiusura non basterebbe, perché uno stop "next bar" emesso il venerdì scade sulla prima
barra del lunedì e riempirebbe sul gap. In live la stessa regola vive nei cBot
(`FlatAtWeekEnd`), non nel server, perché deve tenere anche a server irraggiungibile. Attenzione nei
confronti con il riferimento Python, che invece tiene le posizioni nel weekend: la parità richiede
il flag spento. Dettaglio e misure in `decisioni.md` (2026-08-02).

### 3.6 Le tre modalità dell'engine

`TitanoFilterMode` vale identica per backtest interno (`BacktestingRequest.TitanoMode`) e per le
sessioni, quindi anche per l'engine esterno cTrader (`CreateTradingSessionRequest.TitanoMode`):

| Modalità | Cosa fa | Richiede |
|---|---|---|
| `Disabled` | Nessun filtro: valuta tutte le strategie del masterfilter. È il run che produce il `trades.json` su cui Titano calcola offline le rotazioni. | — |
| `BacktestRotationFile` | Per ogni barra valuta solo le strategie abilitate dal periodo di rotazione che la contiene, letto dal manifest calcolato offline. | `TitanoRunId` + `TitanoBacktestFolder` |
| `Realtime` | Vale la decisione del periodo corrente dell'ultima analisi Titano. Oltre la fine del manifest resta in vigore l'ultimo periodo calcolato, dichiarato nel rotation-log (`UsedLatestPeriod`). | `TitanoRunId` + `TitanoBacktestFolder` |

Una modalità filtrata **non degrada mai in silenzio**: se manca il run la sessione non parte, e se
nessun periodo copre una barra il run si ferma con un errore che dice quale intervallo copre il
manifest. Prima si proseguiva senza filtri, ottenendo il contrario di quanto richiesto.
`Realtime` non è applicabile a un backtest e viene rifiutata.

#### Contesto dichiarato dal client

La modalità Titano da sola non basta: dice *cosa* si vuole fare, non *dove* si sta girando. Il
client dichiara quindi il proprio contesto in `CreateTradingSessionRequest.ClientRunMode`
(`Backtest`, `Realtime`, `Unknown`). Il cBot lo ricava da `Robot.IsBacktesting` — **non** è un
parametro, quindi non si può sbagliare per distrazione — e il server rifiuta le due combinazioni
impossibili:

| | `ClientRunMode.Backtest` | `ClientRunMode.Realtime` |
|---|---|---|
| `Disabled` | ok | ok |
| `BacktestRotationFile` | ok | **rifiutata**: il tempo live esce subito dall'intervallo del manifest |
| `Realtime` | **rifiutata**: rotazione "corrente" su barre storiche, cioè look-ahead | ok |

`Unknown` (console WinForms, test, integrazioni terze) non viene verificato: il client non ha
dichiarato nulla e inventare un contesto sarebbe peggio che lasciare la responsabilità a chi
configura.

La validazione è alla **creazione** della sessione, quindi una sessione ripresa la salterebbe: il
cBot confronta perciò il contesto del descriptor con il proprio e, se non coincide, scarta lo stato
salvato e ne crea una nuova. È il caso concreto in cui si rilancia in backtest un bot che aveva
lasciato su file una sessione live — il file di stato è per account/simbolo/timeframe, non per
contesto. Il `ClientRunMode` finisce anche nel `rotation-log`: rileggendolo mesi dopo è l'unica cosa
che dice se quelle decisioni sono state prese su dati storici o su mercato reale.

### 3.7 Strategie stateless

`ITradingStrategy.Evaluate(StrategyEvaluationRequest)` è l'API corretta.
`GenerateSignal(OhlcvData[], DateTime)` è legacy e marcata `[Obsolete]`.

Le strategie **non possiedono lo stato di posizione**: lo riceve l'engine e glielo
passa in `StrategyExecutionSnapshot`. La "memoria tecnica" della strategia (contatori
di sessione, flag di pattern) viaggia avanti e indietro come
`RuntimeState` — un dizionario di field catturato dall'engine.

`StatelessEasyStrategyBase` implementa questo contratto per le strategie generate da
EasyLanguage: a ogni valutazione clona l'istanza, ci inietta lo stato ricevuto,
esegue il codice legacy e restituisce lo stato aggiornato. Riflessione e metadati
di tipo sono cacheati per tipo — non toccare quella cache senza misurare.

---

## 4. Flussi

### 4.1 Backtest locale

```
BacktestingRequest (workspace + range + capitale)
  → risoluzione masterfilter Id → StrategyDefinition → istanze
  → prefill datafeed per ogni (Symbol, Timeframe) usato
  → LOOP sul minimo timeframe, tutto in-process
       per ogni strategia allineata a questa barra:
           finestra di candele (cursore incrementale, non LINQ)
           Evaluate → TradeSignal
       ProcessSignals + UpdateMarketPrices su TUTTI i simboli della barra
       log eventi (append-only)
  → trades.json, signals.json, backtest-log.jsonl, backtest-summary.json,
    backtest_<nome>_<ts>.json, report HTML
```

**Invarianti del loop:**

* Nessuna chiamata HTTP: il loop è interamente in-process.
* Nessuna scrittura sincrona su disco per barra: solo checkpoint periodici e
  scrittura finale.
* `UpdateMarketPrices` va chiamato **a ogni barra** con i prezzi di tutti i simboli
  della barra, indipendentemente da quali strategie sono state valutate — altrimenti
  stop loss, take profit e time exit vengono verificati in ritardo.
* Ogni job usa la **propria istanza** di `PiootooTradingService`: il motore è mutabile
  e non è thread-safe.
* L'orologio del loop è sintetico e non coincide con le barre del feed. Il prezzo di
  **mark-to-market** è sempre l'ultimo noto, anche stantio; la barra di **esecuzione**
  deve invece appartenere al tick corrente, altrimenti un ordine si riempie sui prezzi
  di un intervallo diverso da quello rappresentato. Un intent scaduto si **scarta**,
  non si esegue al proprio livello. Dettaglio e casistica in
  [`domini/orologio-barre-e-fill.md`](domini/orologio-barre-e-fill.md).

### 4.2 Sessione live con cTrader

```
cBot OnStart → POST /trading-sessions/open-plan
    server: risolve piano → riprende oppure crea sessione → restituisce strumenti/timeframe
cBot OnBar → POST /trading-sessions/{id}/bars   (solo la barra chiusa)
    server: [Titano?] → Evaluate → sizing → OrderIntent[]
cBot esegue → POST /trading-sessions/{id}/execution-reports
    server: aggiorna posizione, e sulla chiusura produce il PersistedTrade
```

Il server emette **solo intent di ingresso**, ciascuno con la specifica di uscita completa
(SL, TP, `CloseAtUtc`, `MaxBarsInPosition`). Il cBot la applica: SL/TP come livelli nativi del
broker, uscita a tempo e limite barre sorvegliati in locale a ogni barra.

Le uscite hanno quindi **un solo canale**: qualunque ne sia la causa, il cBot registra un intent
`OrderIntentKind.Close` con `POST /trading-sessions/{id}/intents/close-external` e poi lo
referenzia nel normale execution report. È così che nasce il `PersistedTrade` in `trades.json`.

Conseguenza pratica sul riavvio del cBot: la ricostruzione degli intent di apertura
(`GET /{id}/intents`) è **necessaria**, non un'ottimizzazione. Senza di essa il bot perde
`CloseAtUtc` e `MaxBarsInPosition` delle posizioni aperte e non sa più chiuderle a tempo.

**Autorità:** il server decide *cosa*, il broker decide *se e a che prezzo*.
Il server non assume mai un fill.

### 4.3 Gruppi account — anti copy-trading

Solo in `ExecutionMode.ExternalBroker`. Ogni account cTrader è mappato a un
**GroupId** (tipicamente una prop firm).

Regola: **dentro lo stesso gruppo una strategia va a un solo account**; gruppi
diversi ricevono copie indipendenti dello stesso segnale.

Implementazione: i segnali di ingresso non vengono assegnati subito, restano
**template** (`EntryTemplates`). Ogni account fa polling di
`POST /trading-sessions/{id}/accounts/{account}/signal` e reclama un template se:

* il proprio gruppo non l'ha già reclamato (`TemplateClaimedGroups`);
* lo slot `(gruppo, strategia, simbolo)` è libero (`GroupStrategySlots`);
* l'account non è già impegnato su quel simbolo (`AccountActiveIntent`).

Le chiusure fanno invece **fan-out**: un intent per ciascun gruppo che detiene la
posizione. Gli slot si liberano sia sul fill di chiusura sia sull'ingresso
rifiutato con zero riempito.

### 4.4 Titano

Un solo modello: `TitanoRotationService`. Legge `trades.json`, produce un manifest
di run con periodi e moltiplicatori di allocazione, consumato dal backtest interno
e dalle sessioni di trading.

Fino al 31/07/2026 esisteva un secondo filtro (`TitanoFilterService`, ON/OFF
settimanale binario su `BacktestingResult`, esposto su `POST /titano/apply-filter`
e usato dal client `piootoo.titanoclient`). È stato rimosso: mai eseguito, mai
adottato dalla console principale, e affetto da un lock-out permanente del
cooldown. Vedi `decisioni.md` (2026-07-31) e
`titano-analisi-parametri-e-audit-2026-07-31.md`.

```
trades.json → Run() → runId = sha(trades + masterfilter + config)
           → periodi → decisioni per strategia (voto, score, isteresi, cooldown,
             hard stop) → manifest.json + period-*.json
Resolve(runId, timestamp) → strategie effettive + moltiplicatore di allocazione
```

Il `runId` è deterministico: stessi input ⇒ stesso run, riutilizzato invece di
ricalcolato.

**Modalità:** vedi §3.6. Backtest interno ed engine esterno usano lo stesso `TitanoFilterMode`,
quindi filtrano allo stesso modo; il cBot non conosce Titano e riceve i segnali già filtrati.

**Attenzione:** un manifest costruito su un backtest storico copre solo l'intervallo di quel
backtest. Fuori dai periodi definiti non esiste una decisione. In `BacktestRotationFile` è un
errore esplicito; in `Realtime` resta in vigore l'ultimo periodo calcolato, dichiarandolo nel
rotation-log.

---

## 5. Datafeed

`piootoo-repository/datafeed/` contiene un file per combinazione simbolo+timeframe:

```
{tickerSenzaCaratteriSpeciali}_{timeframeMinuti}.json     es. GCF_15.json
```

Il mapping tra simbolo di strategia (`@GC`) e ticker del feed (`GC=F` → `GCF`) è in
`DataSourceRepository.RootSymbolToTicker`.

Il downloader è `piootoo-repository/datafeed-downloader/` (Python, Yahoo Finance).
**Limite noto:** Yahoo fornisce dati intraday solo per ~60 giorni. Un backtest su
finestre più lunghe con timeframe intraday non ha i dati e le strategie restano mute.

**Invariante:** se una coppia `(Symbol, Timeframe)` richiesta dal masterfilter non
ha dati, il backtest deve **fallire o segnalarlo esplicitamente**, mai proseguire in
silenzio.

---

## 6. Artefatti prodotti

Sotto `<workspace>/backtests/<nome>/`:

| File | Contenuto |
|---|---|
| `signals.json` | tutti i segnali emessi (schema `PersistedSignal` v2) |
| `trades.json` | trade realmente chiusi (schema `PersistedTrade` v2) — **input di Titano** |
| `backtest-log.jsonl` | log eventi append-only, una riga JSON per evento |
| `backtest-summary.json` | contatori per strategia + diagnosi automatica |
| `backtest_<nome>_<ts>.json` | `BacktestingResult` completo |
| `backtest_<nome>_<ts>.html` | report equity per strategia |
| `titano/<runId>/manifest.json` | manifest di rotazione |

Sotto `<workspace>/plans/`: `plans.json`.

Sotto `<workspace>/sessions/<sessionId>/`: `signals.json`, `trades.json`,
`rotation-log.json`.

### 6.1 Log di trading — `backtest-log.jsonl`

Una riga JSON per evento, append-only, mai riscritto. Tipi di evento
(`BacktestLogEventType`):

| Tipo | Quando | Campi salienti |
|---|---|---|
| `Run` | inizio/fine job | configurazione, strategie, esito |
| `DataSource` | dopo il prefill | simbolo, timeframe, candele, primo/ultimo timestamp |
| `Signal` | segnale non-Hold | strategia, side, prezzo, SL/TP, motivo |
| `Entry` | apertura posizione | prezzo di fill, contratti, SL/TP in punti |
| `Exit` | chiusura posizione | prezzo, motivo (`StopLoss`, `TakeProfit`, `TimeExit`, `MaxBars`, `OppositeSignal`, `WeekEnd`, `EndOfRun`), P&L |
| `Anomaly` | incoerenza rilevata | descrizione |

Gli **skip** (dati insufficienti, candela stale, timeframe non allineato) non
producono righe: sarebbero milioni. Vengono contati e riportati nel summary.

### 6.2 Riepilogo — `backtest-summary.json`

Per ogni strategia: valutazioni, skip per motivo, segnali per tipo, trade,
vincenti/perdenti, P&L, uscite per motivo. In testa un blocco `diagnostics` con i
problemi rilevati automaticamente (strategia mai valutata, mai un segnale, segnali
senza trade, datasource vuoto…). È il file da leggere per capire *perché* un
backtest non ha prodotto trade.

---

## 7. Convenzioni e trappole

**Tempo.** Tutto è UTC. `TradingDateTime.ToFeedUtc` normalizza. I contratti delle
sessioni **rifiutano** `DateTime` con `Kind != Utc`: è voluto, non "aggiustare" con
`SpecifyKind` a valle.

**Simboli.** Le strategie usano `@GC`, il motore normalizza a `GC`. Il prefisso `@`
esiste solo nel catalogo e nei metadati.

**Scritture su disco.** `AtomicFileWriter` scrive su temporaneo e rinomina, con
`WriteThrough` + fsync. È corretto per l'artefatto *finale* ed è **inaccettabile
dentro un loop**: un fsync per barra costa più di tutto il resto del backtest messo
insieme. Per i checkpoint intermedi usare la variante non sincronizzata.

**LINQ nei loop caldi.** `Where().OrderBy().Take()` su una serie già ordinata è
O(N) per chiamata. Nel loop di backtest si usa un cursore incrementale
(`CandleWindowCursor`): la data è monotona crescente, l'indice avanza e basta.

**Reflection.** `StatelessEasyStrategyBase` è il punto più caldo del sistema.
`FieldInfo[]`, `MethodInfo` e accessor di proprietà sono cacheati per tipo. Ogni
chiamata a `GetType().GetProperty(...)` aggiunta lì dentro si paga moltiplicata per
(barre × strategie).

**Servizi singleton.** `PiootooBacktestingService` e i suoi collaboratori sono
singleton per tenere i job in memoria, ma il **motore di trading dev'essere per job**.
Vale lo stesso per le sessioni, che infatti istanziano il proprio motore.

**Stato in memoria.** Le sessioni di trading vivono solo in RAM: un riavvio del
server le perde. È un limite noto, non un comportamento voluto.

---

## 8. Punti aperti

* Persistenza dello stato di sessione (oggi solo in RAM).
* Titano in tempo reale che **ricalcoli** dai trade live invece di rileggere un
  manifest precalcolato (oggi `Realtime` rilegge il manifest e tiene in vigore
  l'ultimo periodo calcolato).
* Conversione delle uscite residue: la maggior parte delle strategie del catalogo
  esce ancora con un segnale opposto deciso barra per barra (`LX`/`SX` su contatore
  di barre) invece che con `CloseAtUtc`/`MaxBarsInPosition` sul segnale di ingresso.
* Unificazione dei due modelli di configurazione Titano (filtro e rotazione).
* `RequiredCandles` hardcoded a 100 nelle strategie generate: va derivato dal
  lookback reale.
* Le strategie con timeframe superiore al minimo vengono valutate su un orologio
  sintetico e non sui confini reali della loro barra.
* TTL sui template di ingresso nelle sessioni multi-account.
* Copertura del datafeed limitata a @GC 5m/15m.

Dettaglio ed evidenze: [`verifica-codice-2026-07-27.md`](verifica-codice-2026-07-27.md).
