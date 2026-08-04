# Decisioni

Log breve delle scelte fatte e del perché. Una riga (o poche righe) per voce,
in ordine cronologico. Non è un changelog di codice: quello resta nei commit.

- **2026-07-25** — La SPA Angular (`piootooapp.client`) è stata scollegata dal
  debug F5 di Visual Studio: rimossi `ProjectReference` al `.esproj`,
  `Microsoft.AspNetCore.SpaProxy` e `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` da
  `PiootooApp.Server`. F5 avvia solo il backend.
- **2026-07-25** — Tab Titano e Trading Session della workspace console
  riorganizzati in gruppi tematici con tooltip per ogni campo, per rendere
  esplicite le regole (già documentate in `domini/titano-rotation.md` e
  `domini/trading-sessions-api.md`) anche a chi non legge il codice.
- **2026-07-27** — **Il codice di esecuzione di una strategia è `ITradingStrategy.Name`**
  (es. `TOP_UA_218`), non l'Id di classe (`Easy_218_GC_60`). L'Id resta la chiave di
  selezione del masterfilter e del catalogo; ogni confronto tra masterfilter e dati di
  esecuzione passa da `StrategyCatalog.ResolveExecutionCodes`. Prima i due venivano
  confrontati direttamente e non combaciavano mai: il report di backtest mostrava zero
  trade ed equity piatte, Titano non trovava un solo trade e disabilitava tutto, le
  sessioni collegate a una rotazione non valutavano alcuna strategia.
- **2026-07-27** — Il backtest **non scrive più `signals.json`/`trades.json` a ogni barra**.
  Erano una riserializzazione completa più un fsync per barra: da sola la voce di costo
  dominante del run. Ora c'è un checkpoint non-durabile ogni 5.000 barre e una scrittura
  durabile finale.
- **2026-07-27** — Le finestre di candele nel loop si ottengono da `CandleWindowCursor`
  invece che da `Where().OrderBy().Take()`: la serie è ordinata e l'orologio del loop è
  monotono, quindi il costo passa da O(candele totali) a O(RequiredCandles) per chiamata.
- **2026-07-27** — `UpdateMarketPrices` viene chiamato a **ogni** barra con i prezzi di
  tutti i simboli, non solo di quelli con una strategia allineata: stop loss, take profit
  e time exit venivano altrimenti verificati in ritardo.
- **2026-07-27** — Ogni job di backtest istanzia il **proprio** `PiootooTradingService`.
  Il singleton condiviso faceva mescolare posizioni e trade tra backtest concorrenti.
- **2026-07-27** — Un datasource vuoto **fa fallire** il backtest invece di far saltare in
  silenzio le strategie interessate.
- **2026-07-27** — Aggiunti `backtest-log.jsonl` (eventi) e `backtest-summary.json`
  (contatori per strategia + diagnosi automatiche). Gli skip ad alta frequenza sono
  contati, non loggati riga per riga.
- **2026-07-27** — Le sessioni distinguono "nessun periodo attivo" da "tutte le strategie
  disabilitate": un manifest storico usato in live azzerava il portafoglio senza dirlo.
- **2026-07-27** — Rimosso il meccanismo `CloseOnly`. Un segnale di ingresso descrive per
  intero la propria uscita (`StopLoss`, `TakeProfit`, `BreakEven`, `CloseAtUtc`,
  `MaxBarsInPosition`) e l'engine chiude in autonomia. Le strategie che decidevano
  l'uscita a runtime verificando un pattern (`IsPositionCloseDependent`) sono escluse dal
  catalogo: `CreateStrategy` le rifiuta, così un masterfilter salvato in passato non può
  riportarle in esecuzione. In ExternalBroker le chiusure hanno un canale unico
  (`intents/close-external` → `OrderIntentKind.Close`), qualunque ne sia la causa.
  Motivo: due percorsi di uscita — server e client — significavano due semantiche da
  tenere allineate a mano e un cBot che doveva indovinare quale delle due stava vivendo.
- **2026-07-27** — Il flag booleano `ApplyTitanoFilters` è diventato `TitanoFilterMode` a
  tre valori (`Disabled`, `BacktestRotationFile`, `Realtime`), condiviso da backtest
  interno e sessioni. Una modalità filtrata non degrada più in silenzio a "nessun filtro":
  senza run la sessione non parte, e una barra fuori dai periodi del manifest ferma il run
  con un errore. Prima si proseguiva senza filtri, cioè l'opposto di quanto richiesto.
- **2026-07-27** — Titano assegna le allocazioni per **percentile** fra le strategie del periodo,
  con curva continua fra 25% e 100%, invece che per soglie assolute sui tier. Su un run reale a 52
  periodi l'81% delle assegnazioni finiva al 50%: quattro voti su cinque erano quasi costanti
  (performance lunga 0,499–0,556, drawdown 0,913–1, volatilità 0,962–1, z-score binario) perché
  normalizzati su intervalli molto più larghi della variazione reale, e lo score composito viveva
  tutto fra 0,595 e 0,808. Nessuna taratura dei tier poteva risolverlo: con varianza quasi nulla
  ogni soglia produce un unico scaglione. L'ON/OFF resta ai cancelli assoluti — un rango dice "è la
  peggiore del gruppo", non "va male" — e `rawScore` conserva il giudizio assoluto per diagnosi.
- **2026-07-27** — `EasyLib.PatternDirectionalFast` faceva il dispatch dello `switch` sul valore
  **con segno** mentre tutti i `case` sono range positivi (`>= 1 and <= 8`, `9`, `>= 10 and <= 12`,
  …). Un pattern negativo non entrava in nessun ramo e cadeva nel default `false`: i rami short,
  scritti come `numeroPattern > 0 ? long : short`, erano irraggiungibili. L'unico negativo gestito
  era `-52`, a mano. Dispatch spostato su `Math.Abs(numeroPattern)`. Effetto: le strategie che
  usano pattern direzionali negativi non emettevano **mai** un segnale — TOP_UA_303 (`-47`, `-9`),
  TOP_UA_416, TOP_UA_695, TOP_UA_851, TOP_UA_940, più il lato short di TOP_UA_291 (`-48`, `-13`).
  Trovato indagando un backtest a zero trade sul workspace gold-one.
- **2026-07-27** — Il client dichiara il proprio contesto di esecuzione (`ClientRunMode`) alla
  creazione della sessione, e il cBot lo legge da `Robot.IsBacktesting` invece di esporlo come
  parametro. Il server incrocia contesto e modalità Titano e rifiuta `Realtime` in backtest e
  `BacktestRotationFile` in live. Motivo: erano le uniche due misconfigurazioni che non davano
  nessun errore — producevano risultati plausibili ma sbagliati, visibili solo dai numeri. Un
  parametro manuale avrebbe spostato il problema, non risolto: il contesto lo conosce la
  piattaforma, non l'operatore.
- **2026-07-28** — La conversione symbol di un account si applica alla **size** dentro il motore ma
  al **simbolo** solo in uscita, su `signals.json`. Rinominare `@NQ` in `USDTEC` a monte sembrava la
  cosa ovvia, ma il motore indicizza prezzi correnti, barre e chiavi di posizione sul simbolo
  Piootoo normalizzato: un segnale con il simbolo del broker resterebbe senza prezzo e non
  verrebbe mai eseguito. Il simbolo del broker serve a chi inoltra l'ordine, non a chi lo simula,
  quindi vive in `PersistedSignal.AccountSymbol` accanto al simbolo interno. Il moltiplicatore
  contratto invece scala `signal.Quantity` prima di `ProcessSignals`, altrimenti equity e drawdown
  del backtest non sarebbero quelli del conto reale. Un `AccountId` inesistente fa fallire il run:
  proseguire 1 a 1 sarebbe un errore silenzioso sulle size.
- **2026-07-29** — La configurazione operativa riutilizzabile è un **piano di trading** del
  workspace, distinto dalla sessione mutabile. Il piano ha un codice globale e contiene account,
  gruppo, limite trade, setup/run Titano, sizing e metadata; il cBot configura soltanto quel
  codice. La chiave `(PlanCode, ClientRunMode, ExecutionKey)` crea o riprende idempotentemente una
  sessione e impedisce di confondere backtest e realtime. La sessione acquisisce uno snapshot del
  piano: successive modifiche non cambiano un'esecuzione già avviata. L'idempotenza è per ora in
  memoria server; la ricostruzione dopo il riavvio del processo resta un lavoro separato.
- **2026-07-29** — `signals.json` conserva **tutte** le condizioni di uscita del `TradeSignal`:
  stop/target in punti, stop/target in USD per contratto, break even, uscita a tempo e massimo
  barre. Prima i limiti monetari (usati da PTS e dalle Easy NQ recenti) andavano persi in
  persistenza: l'emulatore li convertiva in punti al fill, ma il file non li riportava. Non si
  convertono in percentuale: restano USD/contratto sul segnale e punti solo dopo la divisione per
  `DollarsPerPoint` (NQ = 20). Nelle sessioni, `AddIntent` fa la stessa conversione verso
  `OrderIntent.StopLoss`/`TakeProfit` così il client esterno riceve livelli utilizzabili.
- **2026-07-29** — `PiootooDistributedExecutionBot` gira su grafico 5m (configurabile come timeframe base)
  ma non aggrega barre per simulare timeframe superiori: a ogni `OnBar` legge la serie cTrader
  nativa di ogni coppia `(simbolo, timeframe)` restituita dal piano e invia al server soltanto la
  sua ultima barra chiusa non ancora trasmessa. L'intent porta anche il timeframe della strategia:
  `MaxBarsInPosition` viene quindi contato sulle sole barre di quello stream. Il cBot applica ora
  anche `BreakEven` spostando lo stop nativo all'entry quando raggiunta la distanza dichiarata.
- **2026-07-31** — Rimosso **Titano legacy** (`TitanoFilterService`,
  `TitanoSetupService`, i modelli `TitanoFilter*`, gli endpoint
  `GET/POST /api/Titano/setups` e `POST /api/Titano/apply-filter`, il progetto
  `piootoo.titanoclient`). Era un secondo filtro ON/OFF settimanale binario che
  calcolava su `BacktestingResult` invece che sui trade chiusi. Motivi: mai
  eseguito (`settings/titano-setups/` e `settings/results/titano/` vuote, create
  solo dal costruttore del servizio); mai richiamato dalla console principale,
  che usa esclusivamente gli endpoint `rotations`/`rotation-setups`; il suo unico
  client era fuori dal profilo di avvio della solution; fermo al commit iniziale
  mentre la v2 evolveva. Portava inoltre due difetti gravi — il cooldown veniva
  riarmato anche nelle settimane in cui la strategia era OFF *per il cooldown
  stesso*, cioè un lock-out permanente al primo spegnimento, e
  `MaxRollingDrawdown` andava inserito negativo senza che l'interfaccia lo
  dichiarasse. `TitanoSetupInfo` era l'unico tipo condiviso con la v2 ed è stato
  spostato in `TitanoRotationModels.cs`. Ora esiste un solo Titano: la rotazione.
- **2026-07-31** — Correzioni all'audit Titano (dettaglio in
  `titano-analisi-parametri-e-audit-2026-07-31.md`). Le tre che cambiano i numeri:
  (a) il **voto z-score** era binario, quindi tutte le strategie dentro banda erano pari
  merito, prendevano percentile 0,5 e **nessuna poteva raggiungere l'allocazione
  massima** — il portafoglio restava sotto-investito di un ~5% strutturale e lo stato
  `Enabled` non compariva mai. Ora vale 1 al centro della banda e degrada a 0 agli
  estremi: il centro è il punto migliore, perché un z alto è surriscaldamento ed è la
  ragione per cui esiste `MaximumZScore`. (b) `State = Enabled` si confronta con il
  **tetto configurato** e non con la costante 1. (c) Il **reset dello hard stop** toglie
  il latch e nient'altro: prima ricalcolava l'allocazione dal solo score, e col sizing
  per percentile `ComputeAllocation` restituisce almeno il pavimento per qualunque
  punteggio, quindi una strategia con zero voti superati e drawdown al 60% tornava
  operativa al 25%. Ora `Resolve` riapplica la stessa condizione `reenable` della
  rotazione sui dati già persistiti nello stato.
  Inoltre: `EquityAt` non confonde più equity zero con dato assente; il cooldown non si
  arma sull'hard stop; `AnomalyFlags` non segnala più l'isteresi legittima; il manifest
  dichiara `tradesOutsideCoverage` (le due curve del report non erano a parità di
  campione) e `walkForwardNote`; `Get()` compone il manifest dentro il lock; due
  scansioni quadratiche sostituite da indici. `TitanoRotationRequest` è diventato un
  `record` per poter derivare varianti nei test.
- **2026-07-31** — Il **limite di trade concorrenti è disaccoppiato da Titano**. Era
  cablato su `TitanoMode` (`Backtest && Disabled` ⇒ nessun limite): la regola era
  giusta — quel run produce il campione sorgente delle rotazioni e non va limitato — ma
  espressa dalla variabile sbagliata. Conseguenza pratica: confrontare un run
  `Disabled` con uno `BacktestRotationFile` muoveva due variabili, e si attribuiva a
  Titano una differenza che veniva in parte dal limite. Ora è un flag esplicito
  `EnforceConcurrencyLimits` su piano e sessione (`null` = default storico, quindi
  nessuna configurazione esistente cambia comportamento), esposto dal cBot come
  parametro a tre stati. I tre lucchetti della distribuzione — template per gruppo,
  slot gruppo/strategia/simbolo, account/simbolo — non leggevano e non leggono nulla di
  Titano: l'unico punto di contatto resta `IsTemplateEligibleForGroup`, che fallisce
  verso "passa".
- **2026-08-02** — Completata la migrazione dei **12 motori Unger** in
  `Piootoo.Strategies/Easy/Engines`: contratto comune in `EasyEngineBase` (ingresso next-bar,
  rischio in denaro/contratto, `MaxEntriesPerSession` al fill), catalogo `Easy_*`/`PTS_*` su
  sottoclassi dichiarative, ibridi classificati, close-dependent esclusi dal factory. Mappa in
  `docs/domini/motori-strategie.md` e registro testabile in
  `EngineCatalogMigrationTests`.
- **2026-07-31** — Le due cache statiche di `TitanoRotationService` sono ora limitate.
  `ManifestCache` ha un tetto di 32 voci con politica LRU: l'ordinamento usa un contatore
  monotono e non l'orologio di sistema, e sbagliare vittima sotto concorrenza costa una
  rilettura da disco, non un errore. `Gates` non è più un `ConcurrentDictionary` che
  cresce a ogni run: è un array di 64 lock indicizzato sull'hash del percorso. Un
  dizionario con eviction sarebbe stato **pericoloso** — rimuovere un lock mentre un
  thread lo detiene farebbe ottenere a due thread oggetti diversi per lo stesso percorso,
  cioè nessuna mutua esclusione. L'array ha memoria costante e mappa sempre lo stesso
  percorso sullo stesso lock; il prezzo è che due percorsi diversi possono condividerne
  uno (1 su 64) e serializzarsi inutilmente, irrilevante su un'operazione rara e pesante
  come `Run`.
- **2026-08-02** — **`MinimumTrades` è una precondizione di eleggibilità Titano, non uno dei
  cinque voti.** Era conteggiato solo dentro `short-performance`: con zero trade nella finestra
  breve quel voto era l'unico a fallire, mentre rendimento lungo, z-score, drawdown e
  volatilità passavano **a vuoto** — valgono zero proprio perché non c'è nulla da misurare — e
  con `MinimumPassingFilters = 4` il conteggio arrivava esattamente a soglia. Risultato: una
  strategia che non aveva mai operato veniva accesa ad **allocazione piena**. Ora
  `BuildDecisions` e `IsReenableSatisfied` richiedono entrambi `Metrics.Trades >=
  MinimumTrades` prima di guardare i voti: su un campione insufficiente Titano non ha un
  giudizio, e "nessun giudizio" non può valere "promossa". L'isteresi di `MinimumOnPeriods`
  resta invariata, quindi una strategia già accesa che attraversa una fase senza trade non
  viene spenta di colpo.
- **2026-08-02** — **`data2` giornaliero si ricostruisce aggregando le sessioni, non si pretende
  un datafeed a 1440.** Le sorgenti `..._1440_...` sono grafici intraday con un `data2`
  giornaliero, e su un grafico intraday le barre giornaliere di EasyLanguage *sono* le sessioni
  aggregate dello stesso strumento: `EasyLib.BuildSessionSeries` le costruisce con gli stessi
  confini di `OHLCMulti5`, estendendo da d0..d5 a tutto lo storico. L'ultima barra è la sessione
  in formazione, come `c data2` su una barra intraday in TradeStation e come d0. Pretendere un
  feed separato avrebbe reso `303`, `195` e `796` non eseguibili (il datafeed ha solo `@NQ` a 15m
  e 1h) in cambio di nessun guadagno di fedeltà. Un test confronta la coda della serie con
  d0/d1/d2 di `OHLCMulti5`: se le due segmentassero diversamente parlerebbero di sessioni
  diverse, ed è l'invariante che tiene insieme la traduzione.
- **2026-08-02** — Tre indicatori tornano sulla serie che l'originale usa davvero. La `303`
  calcolava `ADX(5) data2` sulle barre da 15 minuti e contava `ADXPastvalue` in barre invece che
  in sessioni; la `796` moltiplicava per `ATRMult` un ATR(23) di barre da 15 minuti dove
  l'originale usa un ATR(23) di *giorni* — grandezze di ordine diverso, che tenevano il gate
  praticamente sempre aperto — mentre il suo secondo ATR(9), che nell'originale è senza `data2`,
  resta correttamente sul grafico. La `195` non aveva affatto i due gate `Compression_condition`
  e `Volatility_condition`: ora ci sono, e l'`[1]` di `AvgTrueRange(5)[1] of data2` è
  sostanziale, perché esclude dalla media la sessione con cui la si confronta (`EasyLib` ha un
  overload `AvgTrueRange(periods, barsAgo)` per questo). Di conseguenza `RequiredCandles` di
  queste strategie cresce: deve coprire le sessioni che l'indicatore consuma più una di margine,
  perché la prima sessione della finestra è quasi sempre troncata.
- **2026-08-02** — La `661` è l'unico caso in cui `data2` è **più fine** del grafico (15 minuti
  su un grafico a 30) e quindi non è derivabile: da una barra da 30 minuti non si sa come si è
  mossa la seconda metà. Dichiara ora il timeframe aggiuntivo via
  `IMultiTimeframeTradingStrategy` e senza quella serie si ferma con un motivo esplicito, invece
  di ripiegare sulla barra a 30 minuti precedente. Il flag `oklong1`/`okshort1` è un **latch**:
  l'originale lo aggiorna solo su `sessionlastbar data2` e lo tiene per tutta la sessione
  seguente, mentre la traduzione lo ricalcolava a ogni barra, cambiandolo più volte al giorno.
  `EasyLib.LastBarOfPreviousSession` fornisce la barra giusta.
- **2026-08-02** — **`PTS_002_NQ_15` e `PTS_003_NQ_15` dichiarano `IntradayOnly = false`.**
  La specifica PC di entrambe prevede `intraday_only = 0`, ma `PriceChannelEngine` ha
  `IntradayOnly = true` per default e le due classi non lo disattivavano: ogni segnale
  usciva con `CloseAtUtc` alle 16:00 UTC e il PC multiday si comportava da strategia di
  sessione. Non violava nessun contratto, quindi i test passavano e il difetto si vedeva
  solo nei risultati: sul feed NQ 15 dal 2006 al 2025 il `TimeExit` era la causa di uscita
  di 848 trade su 2012 (PTS_002) e 596 su 1448 (PTS_003). Corretto, il net profit di
  PTS_002 passa da $128.762 a $175.172 con avg trade da $64 a $130 e max drawdown dal
  15,2% al 9,3%. La regressione è ora coperta da `Pts002PcTests`.
- **2026-08-02** — **La finestra operativa del `PriceChannelEngine` confronta HHMM, non le sole
  ore.** Il motore Python valuta l'orario completo contro gli estremi `"HH:00"` con fine
  inclusa; confrontando `barTime.Hour` la finestra si allargava fino a `HH:59`, quindi con
  `end_hour = 4` entravano anche le barre 04:15–04:45 che nella fonte non producono segnali.
  Lo stesso confronto per ore vive ancora in `VolatilityBreakoutEngine`,
  `LevelFaderEngine` e `SessionBreakoutEngine`: allinearli cambierebbe i risultati di quelle
  strategie, quindi resta da decidere.
- **2026-08-02** — **Aperto: il datafeed etichetta le barre sull'apertura, `OHLCMulti5` le
  assume etichettate sulla chiusura** (`isBarTimeEndTime = true`, confine `t > sessionStartTime`).
  Con sessione CME 17:00–16:00 la barra 16:00 finisce dentro la sessione che si è già chiusa e
  la barra 17:00, che è la prima della nuova, resta fuori da ogni sessione. Misure su NQ 15m
  2020–2025 (1.684 sessioni): `open` di sessione diverso nell'82,6% dei casi, `close` nell'82%
  con scarto medio 13,7 punti, `high` e `low` nel ~14,6%. Le barre della pausa CME
  (16:15–17:00) non appartengono a nessuna sessione e su di esse i gate leggono un `d0`
  stantio, cioè la sessione precedente completa: per PTS_002 sono 175 trade su 1.084 (16%) e
  $36.785 di utile, per PTS_003 98 su 816 (12%) e $21.983. Le altre sette strategie NQ del
  catalogo non hanno trade in quella fascia, perché le loro finestre operative la escludono.
  Correggerlo cambia i risultati di tutto il catalogo: decisione rinviata.
- **2026-08-02** — **Una barra stantia non può più riempire un ordine, e un intent scaduto si
  scarta invece di eseguirsi al proprio livello.** L'orologio del loop è sintetico: sui tick in
  cui il feed non ha barre il cursore restituisce l'ultima barra chiusa, la strategia la
  rivaluta, riemette lo stesso stop con `ValidFromUtc`/`ExpiresAtUtc` già passati, e
  `RequiresDeferredExecution` lo classificava come eseguibile subito — senza una barra da cui
  leggere i prezzi il fill ripiegava sul prezzo del segnale, cioè esattamente il livello dello
  stop. Risultato: ingressi a un timestamp senza barra, a un prezzo che nessuno ha scambiato, e
  sistematicamente in perdita perché il breakout non era mai avvenuto. Tre guardie:
  `ProcessSignals` abbandona gli intent scaduti (la verifica di scadenza esce da
  `RequiresDeferredExecution`, che ora risponde solo a "è per il futuro?"), `CanExecuteOnBar`
  pretende una barra chiusa a partire da `ValidFromUtc` per stop/limit/market non-`ExitOnly`, e
  `BelongsToCurrentTick` fa entrare in `currentBars` solo le barre del tick corrente.
  `currentPrices` continua a ricevere anche i prezzi stantii: senza di essi stop, target e time
  exit non sarebbero valutabili affatto, ed è la distinzione che rende le due cose non
  intercambiabili. Le uscite restano eseguibili su dati stantii e le chiusure tecniche
  (`WeekEnd`, `EndOfRun`) restano legittimamente senza barra corrispondente. Misura a parità di
  richiesta su `PTS_002_NQ_15` (2024-01-01 → 2025-12-31, cioè sette mesi oltre l'ultima barra
  del feed): **101 dei 226 trade erano fill fantasma**, il net profit passa da $56 a $33.270 e i
  vincenti dal 19% al 36%. Ne ricompaiono 25 nuovi, perché i fill fantasma consumavano
  `MaxEntriesPerSession` e bloccavano ingressi legittimi nella stessa sessione. Le guardie
  agiscono al fill e non alla valutazione, quindi i segnali inutili restano: `signals.json`
  conserva 14.625 copie identiche del segnale dell'ultima barra, ed è la ragione pratica per
  allineare l'intervallo richiesto alla copertura del feed.
  Documento: `docs/domini/orologio-barre-e-fill.md`, procedura di confronto con un
  riferimento esterno in `docs/domini/parita-riferimento-esterno.md`; regressioni
  `GapInFeed_DoesNotFillStopWithoutABar` e
  `ExpiredStopIntent_IsDiscardedInsteadOfFilledAtItsLevel` in `PendingStopOrderTests`.
- **2026-08-02** — Il percorso di parità Python di `PriceChannelEngine` piazza lo stop **un tick
  oltre** `estremo del canale + offset`: il livello va penetrato, non toccato. Il difetto si
  vedeva solo confrontando i trade con il riferimento `top01_PC`: a parità di canale l'ingresso
  Python cadeva sistematicamente un tick sopra il nostro (43 coppie appaiate su 52 con scarto
  esatto di 0,25 punti), e le 9 eccezioni erano barre che aprivano oltre il livello, dove il
  fill è `max(open, livello)` e non il livello. Corretto, sulle stesse coppie lo scarto di
  prezzo diventa 0,00 in 38 casi su 42 e il net profit della finestra 2024-01 → 2025-05 passa
  da $33.270 a **$37.084 contro i $37.200 di Python**, cioè lo 0,3%. Il tocco riguarda solo
  `PTS_002_NQ_15` e `PTS_003_NQ_15`: tutte le `Easy_*` su PC impostano `UseLegacyVariant`, il cui
  offset resta invariato. Regressione in
  `PriceChannelEngineTests.PythonParity_PlacesStopOneTickBeyondTheChannel`.
  Resta aperta la **selezione** dei trade, che il tick non spiega: 149 trade contro 120, con
  ingresso sulla stessa barra fisica solo nel 35% dei casi. La differenza non è nel canale (dove
  entrambi entrano il prezzo ora coincide) né nel limite per sessione (il C# ne rispetta uno per
  sessione su tutte e 149; Python ne fa due in 7 sessioni su 113, quindi quel vincolo o non
  esiste nel motore originale o usa un confine diverso), ma nei **gate**: il C# opera in 63
  sessioni in cui Python resta piatto. Il sospetto è la costruzione delle sessioni su cui girano
  i pattern (`neut_no = 24`, `dir_yes = 2`), non il Price Channel. Confermato e risolto nella voce
  successiva: era il confine di sessione.
- **2026-08-02** — **La sessione di `PTS_002_NQ_15` è il giorno di calendario UTC, non la sessione
  CME 17:00–16:00.** Il confine è stato *misurato* sul riferimento, non scelto: raggruppando i 120
  ingressi di `top01_PC` per ora di taglio, con un confine fra le 00:00 e le 02:00 si ottiene
  esattamente un ingresso per sessione, zero collisioni. Non è un caso: su ~350 giorni di borsa,
  120 ingressi liberi darebbero una ventina di giorni doppi per paradosso dei compleanni, quindi
  zero collisioni dimostra che il vincolo *è* uno per giorno. Un confine nella zona morta della
  finestra operativa (05:00–12:00) ne lascia due, quindi la sessione è il giorno e non
  l'occorrenza della finestra 13:00–04:00, che infatti viene tagliata a metà.
  Il parametro governa **due cose insieme**: il secchio di `MaxEntriesPerSession` via `SessionKey`
  e gli OHLC d0..d5 di `OHLCMulti5` su cui girano i pattern. Ecco perché le due divergenze che
  sembravano separate — 29 ingressi bloccati dal limite e 44 negati dai gate — erano lo stesso
  parametro, e perché nessuno shift di ±1 o ±2 sessioni recuperava l'accordo: un confine sbagliato
  non sfasa i pattern di un indice, li calcola su aggregati diversi delle stesse barre.
  Applicare il confine ha richiesto una guardia in `EasyLib`: `timeStarted = t > sessionStartTime`
  è un confronto stretto, quindi con inizio a `0` la barra delle 00:00 sarebbe stata scartata da
  ogni sessione, perdendone una al giorno da d0..d5 senza che nulla protestasse. La guardia vale
  solo per il confine di mezzanotte e `OHLCMulti5`/`InSessionBars` la applicano allo stesso modo,
  perché devono segmentare identicamente. Nessuna `Easy_*` usa `0` come inizio, quindi il catalogo
  non è toccato.
  Effetto sulla finestra 2024-01 → 2025-05: trade da 149 a **118 contro i 120 di Python**,
  ingressi appaiati sulla stessa barra da 42 a **101**, Jaccard degli ingressi da 0,185 a **0,737**,
  prezzo identico in 92 dei 101 appaiati, net profit $37.663 contro $37.200 (1,2%).
  Residuo: 15 ingressi che Python fa e noi no con gate negativo, e 17 nostri non appaiati di cui
  **12 cadono a una sola barra** da un ingresso Python — cioè sfasamento di barra, non gate.
  Regressioni `SessionSeriesTests.CalendarDaySession_KeepsTheMidnightBarAndSplitsOnTheDate` e
  l'asserzione su `EntrySessionStartUtc` in `Pts002PcTests`.
- **2026-08-02** — **Il feed @NQ copre 5m, 15m, 30m, 1h, 4h e giornaliero; il settimanale no.**
  `aggregate_nq_ascii.py` produceva solo 15m e 1h, mentre le strategie @NQ del catalogo chiedono
  anche 5 minuti (`Easy_152`) e 30 (`Easy_181`, `Easy_298`): quei quattro sono i timeframe di
  default, 4h e giornaliero si chiedono con `--timeframes`. Le cartelle e i `barType` restano
  quelli di `DataSourceRepository.TimeframeFolders`/`CanonicalBarTypes` — un nome inventato
  renderebbe il feed invisibile al server. Il CSV si legge in una passata sola alimentando tutti i
  timeframe insieme, e il controllo sui file già presenti sta **prima** della lettura:
  accorgersene a metà corsa lascerebbe i feed nuovi troncati a una data intermedia. Il fuso del
  sorgente resta obbligatorio e per @NQ è `UTC`, lo stesso con cui erano stati generati 15m e 1h:
  cambiarlo per i timeframe nuovi produrrebbe serie non allineate fra loro. Il settimanale resta
  rifiutato perché l'allineamento parte dalla mezzanotte e non sa dove comincia la settimana.
- **2026-08-02** — **La barra giornaliera del feed @NQ è il giorno di calendario, e per questo
  sorgente non è una scelta arbitraria.** Il dubbio era che il confine di una giornaliera su un
  future dipendesse dall'orario di sessione, che cambia da sorgente EasyLanguage a sorgente
  (17:00 per alcune, 18:00 per altre). Misurando la copertura oraria del feed si vede però che le
  barre finiscono alle 23:00 e riprendono alle 00:00: nell'orologio del CSV la pausa di
  manutenzione CME (16:00-17:00 di Chicago) cade a cavallo della mezzanotte, quindi il giorno di
  calendario contiene esattamente una sessione completa, dalla riapertura serale alla chiusura del
  pomeriggio successivo. Ne restano fuori le due-tre settimane all'anno in cui l'ora legale
  europea e quella americana non sono allineate, dove il confine taglia la sessione un'ora dopo:
  circa l'8% dei giorni. Questo **non** cambia nulla per le strategie EasyLanguage, che
  continuano a costruire le barre di sessione a runtime dal timeframe intraday con l'orario
  dichiarato dalla singola sorgente (`EasyLib.BuildSessionSeries`) e non da questo feed.
- **2026-08-02** — **I timestamp del feed @NQ sono marcati `Z` ma non sono UTC: sono ora europea,
  e questo disallinea le sessioni di tutte le strategie NQ.** Problema aperto, nessun codice
  ancora cambiato.

  *Che orologio ha il feed.* Due misure indipendenti concordano. Il picco di volume, che su NQ
  sta all'apertura del cash americano (09:30 di New York), cade sullo slot **15:30** del feed:
  11,4% del volume totale su 400 giorni, quasi il doppio del secondo slot. E la pausa di
  manutenzione CME (16:00-17:00 di Chicago) cade sulle **23:00-00:00**: le barre finiscono alle
  23:00 e riprendono alle 00:00. Entrambe collocano l'orologio del sorgente sette ore avanti
  rispetto a Chicago, cioè su CET/CEST — sei durante le settimane in cui l'ora legale europea e
  quella americana non sono allineate. Il CSV ASCII non dichiara il proprio fuso e
  `aggregate_nq_ascii.py` è stato lanciato con `--source-timezone UTC`, quindi gli orari sono
  passati inalterati.

  *In che fuso ragionano le sorgenti.* Non in UTC, e nemmeno in un fuso unico: ciascuna usa
  l'ora di borsa del proprio strumento. NQ dichiara `1700`/`1559`, la sessione CME indici in ora
  di Chicago; GC `1800`/`1700`, la stessa in ora di New York; FDAX `0800`/`2200`, Eurex in ora di
  Francoforte; TSLA `0930`/`1600`, Nasdaq in ora di New York. La conferma decisiva è Feeder
  Cattle, `0830`/`1305`: le 13:05 sono l'orario di chiusura del bestiame al CME in ora di
  Chicago, un valore troppo specifico per essere una coincidenza. Nessun fuso unico spiega
  insieme le 08:00 di FDAX e le 09:30 di TSLA.

  *Quanto costa.* Segmentando le stesse 462.692 barre a 15 minuti nei due modi — `1700`/`1559`
  sull'orologio del feed, come fa il codice oggi, e `1700`/`1559` sull'ora di Chicago, come
  intende la sorgente — le sessioni diventano 6.035 invece di 5.013, cioè il 20% in più: il
  taglio alle 17:00 dell'orologio sbagliato spezza sessioni che non esistono. I confini stanno
  17:15→15:45 invece di 00:15→22:45. Sul livello che gli engine Unger tradano più spesso,
  `highd1`/`lowd1` di `OHLCMulti5`, l'esito è che coincidono solo nel **2,8%** delle barre; lo
  scostamento mediano è di **20,8 punti**, il 34,7% dell'ampiezza della sessione precedente, e
  nel 66,5% delle barre supera un quarto di quell'ampiezza. Non è un bias di secondo ordine: i
  livelli di breakout sono un'altra cosa. Ne è investito tutto ciò che conta in sessioni —
  `OHLCMulti5`, `BuildSessionSeries`, ADX e ATR su serie di sessione, `MaxDaysInTrade`, i time
  exit e i filtri sul giorno della settimana.

  *Perché non basta rigenerare in UTC.* Portare il feed a UTC vero (`--source-timezone
  Europe/Rome`) è comunque giusto — l'etichetta `Z` oggi mente — ma non basta a far combaciare gli
  orari: `1700` resterebbe confrontato con un `hhmm` UTC mentre significa 17:00 di Chicago.

  *E non basta nemmeno riscrivere gli orari in UTC,* che sarebbe la soluzione più desiderabile
  perché non lascerebbe equivoci. Misurando sul feed riportato a UTC vero dove cade la pausa
  giornaliera nei due regimi di ora legale, le finestre ammesse per il confine di sessione
  risultano **adiacenti ma disgiunte**: da 21:00 a 22:00 quando a Chicago è ora legale (734 giorni
  su 740 osservati) e da 22:00 a 23:00 quando è ora solare (401 su 402). L'intersezione è vuota,
  quindi nessun valore UTC fisso descrive la sessione tutto l'anno: quello giusto in un regime è
  spostato di un'ora nell'altro. La sessione è ancorata all'ora di borsa e l'ora legale la muove
  rispetto a UTC; l'unica forma esatta è dichiarare la finestra come (fuso IANA del simbolo, orario
  locale) e convertire al confronto, tenendo UTC dappertutto nel resto del dominio.

  *Conseguenza sul feed giornaliero.* Il `D` di @NQ vale finché il feed è etichettato in ora
  europea, perché è lì che il giorno di calendario coincide con la sessione. Riportando il feed a
  UTC vero la pausa si sposta a metà giornata UTC e il giornaliero va ricostruito sulla sessione,
  non sul calendario.

  *Dove sta scritto per l'uso quotidiano.* La regola operativa — cosa controllare quando si
  aggiunge una strategia o un simbolo, come si accerta l'orologio di un feed, cosa costa
  sbagliarlo — è in [`domini/orari-di-sessione-e-fusi.md`](domini/orari-di-sessione-e-fusi.md),
  insieme allo stato della migrazione ancora aperta. La generazione dei timeframe mancanti è in
  [`domini/datafeed-generazione.md`](domini/datafeed-generazione.md).
- **2026-08-02** — **Il fuso dell'host non entra da nessuna parte: "adesso" è `DateTime.UtcNow`.**
  Sostituiti i quindici `DateTime.Now` rimasti nei progetti non-UI. Tre cambiavano davvero un
  risultato: la cron del `DataFeedWorker` (l'orario di polling seguiva il fuso della macchina, ora
  legale inclusa), `WeeklyRotationScheduler.IsSetupValid` e `GetCurrentSetup` (i confini del setup
  sono UTC, confrontarli con l'ora locale faceva scadere la settimana con ore di scarto) e
  `OptimizationRequest.GetDateRange` (la finestra delimita barre di feed, che sono UTC). Gli altri
  erano timestamp persistiti, ambigui una volta serializzati senza offset. Corretto anche
  `DataSourceRepositoryHierarchyTests`, che costruiva l'istante con `new DateTimeOffset(dateTime)`
  su un `Kind` non specificato: il feed sintetico cambiava timestamp secondo la macchina.
  Restano locali solo le tre righe di presentazione della console WinForms, che è il posto giusto.
  Il vincolo è verificato da `UtcOnlyConformanceTests`, che lo controlla sul **sorgente**: su una
  macchina configurata su UTC, cioè quella dove gira la CI, il codice sbagliato si comporta come
  quello giusto e nessun test sul comportamento lo vedrebbe. L'unico fuso diverso da UTC ammesso
  nel sistema resta quello di borsa, che si attraversa da `SessionClock`.
- **2026-08-02** — Rimossi da `datafeed/15m/@NQ` i due file `@NQ-20060401.json` e
  `@NQ-20061001.json`: erano copie esatte del 4 e del 10 gennaio 2006 pubblicate sotto date di
  aprile e ottobre, residui di una corsa che leggeva le date del CSV come mese/giorno invece di
  giorno/mese. Il sorgente non ha righe su quelle due date (sabato e domenica), quindi la corsa
  corretta non le sovrascriveva e sopravvivevano come barre fantasma per qualunque backtest che
  attraversasse quei giorni. Sintomo da cui si riconoscono: una giornata piena di candele nel
  weekend, e un `lastUpdate` più vecchio di quello dei file vicini. Dopo la pulizia i quattro
  timeframe coprono esattamente gli stessi 5.081 giorni.
- **2026-08-02** — **`PiootooDirectExecutionBot` non era in grado di eseguire i segnali del
  catalogo.** L'audit del cBot contro il contratto `OrderIntent` ha trovato sei difetti, tutti
  invisibili dai contratti: gli intent venivano eseguiti, i trade nascevano, i report tornavano.

  *Trailing stop e breakeven non applicati.* Il DTO locale dichiarava `BreakEven` senza usarlo e
  non dichiarava `TrailingStop`, quindi il campo arrivava dal server e veniva scartato in silenzio
  dal deserializzatore. `PTS_002_NQ_15` e `PTS_003_NQ_15` dichiarano entrambe $1.000 di trailing e
  $1.000 di breakeven: sul feed NQ 15 dal 2012 il trailing è la causa di uscita di 395 trade su
  1.084 per PTS_002 e vale +$303.415, cioè tutto il profitto, contro −$172.466 di stop loss e
  +$34.972 di take profit; per PTS_003 sono 284 trade e +$213.044. Eseguite senza trailing quelle
  posizioni corrono fino al take profit da $5.000 o allo stop da $250: non è la stessa strategia.
  Portati da `PiootooDistributedExecutionBot`, che li implementava già, con sorveglianza a ogni tick.

  *Ordini pending accumulati.* `EasyEngineBase` mette `ValidFromUtc = ExpiresAtUtc = barra
  successiva`, la semantica `next bar` di EasyLanguage. Il bot non passava la scadenza e
  controllava il flat solo su `Positions`, mai su `PendingOrders`: il Price Channel riemette a ogni
  barra finché è flat, quindi nella finestra 13:00–04:00 di PTS_002 restavano a mercato fino a una
  sessantina di stop a livelli diversi, tutti eseguibili. Ora l'ordine della barra precedente viene
  cancellato prima di piazzare il nuovo, e comunque alla barra dopo la scadenza.

  *Intent già scartati eseguiti comunque.* `PushBars` consegna anche gli intent annullati
  (`Status = Cancelled`, `FinalQuantity = 0`); il bot ignorava `Status` e con
  `FinalQuantity > 0 ? FinalQuantity : Quantity` ricadeva sulla quantità base, così un'allocazione
  Titano nulla, un blocco per drawdown di portafoglio o un `BelowMinimumQuantity` diventavano trade
  a size piena. Ora si esegue solo `Pending` e solo `FinalQuantity`.

  *Multi-account.* Con gruppi account configurati `POST /bars` restituisce template non assegnati,
  da reclamare via `GET /accounts/{n}/signals`: eseguirli scavalca slot, `MaxConcurrentTrades` ed
  eleggibilità, e lo stesso template finisce su più account. Il bot ora si ferma all'avvio e rimanda
  a `PiootooDistributedExecutionBot`.

  *Timeframe.* `MaxBarsInPosition` era contato sulle barre del grafico; ora l'intent porta
  `TimeframeMinutes` e un intent di timeframe diverso viene rifiutato con un messaggio esplicito.
- **2026-08-02** — **`MaxEntriesPerSession` è applicato anche in `ExternalBroker`, dal server.**
  Il campo esisteva su `TradeSignal` ma non su `OrderIntent` ed era verificato solo da
  `PiootooTradingService`, cioè nel backtest e in `ServerSimulated`: con un broker esterno nessuno
  lo applicava e una PC che dichiara un solo fill per sessione CME poteva entrare più volte. Ora
  viaggia sull'intent (con `EntrySessionStartUtc`) e viene applicato sui **fill confermati** —
  globalmente in `PushBars` per le sessioni a singolo account, per account in
  `GetNextSignalForAccount` — così uno stop non eseguito continua a essere riemesso e in
  multi-account il limite di un account non blocca gli altri. Regressione coperta da
  `SessionEntryLimitTests`.
- **2026-08-02** — **Il feed dei cBot è UTC per l'attributo `[Robot]`, non per l'impostazione di
  cTrader — ma `PiootooDirectExecutionBot` non riusciva a pubblicare nemmeno una barra.**
  Tutti e quattro i cBot dichiarano `[Robot(TimeZone = TimeZones.UTC)]`, che è un attributo di
  compilazione: fissa il fuso in cui il robot vede `Server.Time` e `Bars.OpenTimes` e **non**
  segue il fuso di visualizzazione scelto dall'utente nella piattaforma. La garanzia però veniva
  soltanto da lì, e il session bot serializzava `Bars.Last(1).OpenTime` senza `SpecifyKind`:
  cTrader restituisce `Kind` non impostato, `System.Text.Json` scrive quindi
  `"2026-01-05T00:00:00"` senza suffisso `Z`, il server rilegge `Kind = Unspecified` e
  `ValidateBar` → `RequireUtc` rifiuta la barra. Il push falliva sempre.
  `PiootooDistributedExecutionBot` il `SpecifyKind` lo faceva già.

  La conversione è ora in un unico punto (`BarOpenTimeUtc`), usato sia dal push sia da
  `ResolveBarIndexForTime`, che prima confrontava un orario senza `Kind` con un UTC del server —
  corretto solo perché il confronto tra `DateTime` ignora il `Kind` e l'attributo è UTC. Dove
  serve l'orologio e non una barra, i bot passano a `Server.TimeInUtc`, che è UTC per definizione
  e resta corretto anche se l'attributo cambiasse: `SpecifyKind(Server.Time, Utc)` in quel caso
  etichetterebbe un orario locale come UTC, e `TradingDateTime.ToFeedUtc` lo accetterebbe
  reinterpretando il wall-clock senza spostarlo, cioè spostando tutto il feed in silenzio.
  Regressione coperta da `TradingSessionsHttpTests.BarTimeWithoutTheUtcSuffixIsRejectedAtTheHttpBoundary`,
  che pubblica il JSON grezzo nelle due forme e verifica il 400 senza `Z`.

  *Trailing: non è quello nativo di cTrader.* `hasTrailingStop`/`ModifyTrailingStop` non sono mai
  usati; `MoveTrailingStops` sposta lo **stop loss nativo** via `ModifyPosition`, a ogni tick e a
  ogni barra. L'ordine protettivo resta quindi sul broker — se il bot muore la protezione tiene
  l'ultimo livello — ma il trascinamento no: a bot spento lo stop si congela, mentre quello nativo
  continuerebbe lato server. Il commento parla di distanza «dal massimo/minimo favorevole» mentre
  il codice usa il `Bid`/`Ask` corrente: coincidono perché l'aggiornamento è monotono e gira su
  ogni tick, quindi il massimo nel tempo di `Bid − distanza` è `(Bid massimo) − distanza`. È
  corretto per monotonia, non per costruzione. Restano da valutare: passo minimo prima di muovere
  lo stop (oggi una `ModifyPosition` sincrona per ogni nuovo estremo), rispetto della distanza
  minima di stop del broker, e `ModifyPositionAsync` per non bloccare `OnTick`.

- **2026-08-03** — **Nuova convenzione di nome per le strategie PTS**:
  `PTS_[SYMBOL]_[ENG]_[NNN]_[TF]` al posto di `PTS_[NNN]_[SYMBOL]_[TF]`. Le tre
  strategie esistenti diventano `PTS_001_NQ_60 → PTS_NQ_TFM_001_60`,
  `PTS_002_NQ_15 → PTS_NQ_PCH_001_15`, `PTS_003_NQ_15 → PTS_NQ_PCH_002_15`.
  Il motivo è che il numero da solo non diceva nulla: leggendo un report non si
  capiva con che logica operasse una strategia senza aprire il sorgente. La
  sigla motore sta prima del numero perché il progressivo riparte per coppia
  (symbol, motore), quindi `001` è ambiguo senza di essa. Formato e tabella
  delle sigle in `domini/strategie-catalogo.md`, imposti da
  `PtsNamingConventionTests`.

  *Rottura netta, voluta.* `Name` è lo `StrategyCode` che finisce in
  `signals.json`, `trades.json`, nelle chiavi di posizione e negli stati Titano:
  i run prodotti prima di questa data contengono i codici vecchi e **non sono
  più confrontabili** con quelli nuovi. Niente tabella di alias e nessuna
  riscrittura degli artefatti — quelli sono dichiarati immutabili. In pratica il
  workspace `pts-02` riparte da zero: i backtest esistenti restano leggibili
  come storia, gli stati Titano vanno ricalcolati. Alternativa scartata: un
  livello di alias nel catalogo, che avrebbe salvato la continuità al prezzo di
  due nomi vivi per la stessa strategia a tempo indeterminato.

- **2026-08-03** — **Nuova console WinForms accanto a quella a tab.** La finestra
  storica (`WorkspaceBacktestingForm`, ~4.200 righe, sette tab costruiti a mano
  con un `.Designer.cs` vuoto) resta intatta e raggiungibile da *File → Console
  legacy*, ma l'avvio è ora `MainShellForm`: menu ad albero a sinistra, area
  contenuti a destra, navigazione lista → dettaglio con breadcrumb. Motivo: nei
  tab le azioni di add/remove delle diverse entità erano mescolate nella stessa
  superficie e non si capiva su cosa stessero agendo.

  Due vincoli hanno deciso la forma. Primo, la nuova console deve essere
  **apribile nel designer**: quindi ogni schermata è un `UserControl` con il suo
  `.Designer.cs`, il riuso passa per **composizione** (`EntityToolbar`,
  `DetailToolbar`) e non per una classe base astratta o generica, che il
  designer rifiuta di renderizzare; le dipendenze arrivano da
  `IShellScreen.Initialize` e non dal costruttore, che deve restare senza
  parametri; il caricamento dati è sotto guardia `DesignMode`. Secondo, il menu
  deve essere estensibile: le voci stanno in `NavigationRegistry`, quindi
  aggiungerne una costa una riga più la coppia lista/dettaglio.

  Portate al nuovo modello le anagrafiche **Account**, **Gruppi** e
  **Workspace**; le altre voci sono già nel menu ma disabilitate. Finché la
  copertura non è completa le due console convivono e scrivono sulla stessa API.
  I Gruppi non hanno un dettaglio: l'API li tratta come semplici identificativi,
  quindi la creazione passa da un dialog a campo singolo e la lista mostra gli
  account associati, che è l'unica informazione utile a decidere se eliminarli.
  Il nome di un workspace è la cartella su disco e l'API non espone una
  rinomina: nel dettaglio è in sola lettura, modificabile solo alla creazione.
- **2026-08-03** — `PiootooDirectExecutionBot` si configura con il **codice piano** al posto del
  workspace id, e con esso perdono senso i parametri che il piano già contiene: modalità di
  esecuzione, capitale, commissioni, metadata dello strumento, run/cartella/modalità Titano e
  limite trade concorrenti. Restano solo base url, timeout, execution key, override account,
  `VolumePerQuantityUnit` e il flat di fine settimana, cioè ciò che è del broker o del bot. Il
  motivo è che due configurazioni della stessa esecuzione, una nel piano e una nel cBot, prima o
  poi divergono in silenzio.

  Il piano però attivava sempre la distribuzione multi-account, incompatibile con questo cBot che
  esegue gli intent di `POST /bars` invece di reclamarli: aperto da piano si sarebbe fermato ogni
  volta. `OpenTradingPlanSessionRequest.DistributeToAccounts` (default `true`, quindi nulla cambia
  per `PiootooDistributedExecutionBot`) permette di aprire il piano **senza** gruppi. In quel caso la chiave
  idempotente include anche l'account, perché la sessione non è più condivisibile, e la modalità
  Titano si legge dalla riga dell'account invece che dalla riga primaria del piano.

  `MaxConcurrentTrades` vive solo nel percorso di claim: un piano che lo dichiara e viene aperto in
  esecuzione diretta è rifiutato con `400` invece di girare senza limite. Il cBot verifica inoltre
  all'avvio che la coppia (simbolo, timeframe) del grafico sia coperta dal masterfilter del piano:
  il server accetta qualunque barra e la archivia, quindi un grafico sbagliato produceva un bot che
  lavorava per sempre senza un solo segnale e senza un errore.
- **2026-08-03** — **Shell Sessioni di trading e i tre scenari operativi.** La schermata
  `TradingSessionsScreen` distingue tre modi coerenti con cBot e server: (1) backtest con Titano
  `Disabled` — tutte le strategie del masterfilter del workspace (il piano fornisce solo workspace,
  sizing e capitale, non limita l'universo); (2) backtest con `BacktestRotationFile` — filtro Titano
  dal manifest offline per barra, più `MaxConcurrentTrades` e ordine di claim per gruppo/account in
  `ExternalBroker`; (3) realtime con `Realtime` — periodo corrente dell'ultima analisi, stesse regole
  di concorrenza. La combo `ClientRunMode` + `TitanoMode` replica la validazione
  `RequireCoherentRunMode` quando il contesto è dichiarato; l'apertura da piano usa
  `POST /trading-sessions/open-plan` e mostra il workspace derivato dal piano.
- **2026-08-03** — Completato il menu della shell WinForms con **Sessioni di trading**
  (`TradingSessionsScreen`): creazione manuale, apertura da piano (`open-plan`), snapshot e
  gruppi account via `TradingSessionApiClient`. L'editing completo dei piani resta in
  *Anagrafiche → Piani di trading*; «Genera e applica Titano» solo nella console legacy.
- **2026-08-03** — Nel dettaglio piano (`PlanDetailScreen`) workspace, cartella di backtest, run
  Titano e setup di rotazione sono **combo** invece che campi di testo. Erano quattro identificatori
  che il server risolve: scritti a mano, un refuso non dava alcun errore al salvataggio e si
  manifestava molto più tardi, come `400` all'apertura della sessione o come piano che non filtra.

  Il workspace è editabile solo su un piano nuovo. Su uno esistente resta visibile ma disabilitato:
  il piano vive dentro `<workspace>/plans/plans.json`, quindi cambiarlo sarebbe una move con
  cancellazione dall'origine, non una modifica di campo — e il salvataggio riscrive il piano intero,
  quindi sarebbe successo alla prima modifica di qualsiasi altro campo.

  Cartella e run sono in catena (un run vive dentro una cartella di backtest, e la coppia è ciò che
  identifica il manifest), quindi nel layout la cartella precede il run e cambiarla azzera il run.
  Un valore persistito che non compare più nella lista viene mostrato come voce «non più presente»
  anziché scartato: scartarlo avrebbe azzerato in silenzio un riferimento ancora in uso dalle
  sessioni aperte, che del piano hanno uno snapshot.

  Le colonne omonime della griglia gruppi/account restano testo libero: lì i valori sono per gruppo
  e `TradingPlanService` già rifiuta terne incoerenti sullo stesso `GroupId`.
- **2026-08-03** — Anche **Gruppo** e **Account cTrader** nella griglia del dettaglio piano sono
  combo, dal registro globale `api/Accounts`. Il gruppo **filtra** gli account: la lista di una riga
  contiene solo gli account che nel registro dichiarano quel `GroupId`, e cambiare gruppo azzera un
  account che non gli appartiene.

  L'alternativa era ricavare il gruppo dall'account (che il suo `GroupId` ce l'ha già) e mostrarlo in
  sola lettura. Scartata perché toglie la possibilità di assegnare un account a un gruppo diverso
  all'interno di un singolo piano, che è il motivo per cui `TradingGroupRow` porta entrambi i campi
  invece di puntare all'account. Restano quindi due campi distinti, ma non più combinabili a caso.

  Conseguenza tecnica: la lista account dipende dalla riga, quindi la `DataSource` sta sulla cella e
  non sulla colonna, e la griglia committa l'edit su `CurrentCellDirtyStateChanged` — altrimenti una
  combo notifica il cambio solo quando si esce dalla cella e il filtro scatterebbe in ritardo. Il
  pulsante «aggiungi riga» non scrive più il placeholder `"gruppo"`: era un gruppo inesistente che
  finiva nel piano se non lo si sostituiva.
- **2026-08-03** — Il tab *Strumenti* del dettaglio piano ha un pulsante **Importa simboli dal
  masterfilter**. La lista strumenti è per costruzione una lista di override — `TradingSessionService`
  deriva i simboli dalle strategie del masterfilter e per quelli assenti usa `DollarsPerPoint = 1`,
  passo 1 e `FuturesContracts` — quindi partire vuota è corretto ma pericoloso: su NQ il dollaro per
  punto è 20, e con 1 il sizing per volatilità sbaglia di venti volte senza che nulla fallisca. Il
  pulsante precarica una riga per simbolo con i default, così l'errore resta possibile ma visibile.
  Il confronto con le righe esistenti usa la stessa normalizzazione del server (trim, `@` iniziale
  rimossa, maiuscolo), altrimenti `@NQ` e `NQ` sembrerebbero due simboli diversi.
- **2026-08-03** — Setup di rotazione, cartella di backtest e run Titano sono combo anche nella
  griglia gruppi/account, non solo nel tab Generale. Setup e cartella stanno sulla colonna (le liste
  sono globali o del workspace); il **run sta sulla cella**, perché è filtrato sulla cartella della
  riga: un run appartiene alla cartella da cui è stato prodotto e la coppia è ciò che localizza il
  manifest, quindi righe con cartelle diverse hanno liste di run diverse. Cambiare cartella azzera il
  run della riga invece di lasciare una coppia mista, che darebbe un manifest introvabile solo
  all'apertura della sessione.

  I run sono in cache per cartella (`_runsByFolder`): le celle si popolano in modo sincrono e senza
  cache servirebbe una chiamata HTTP per riga a ogni refresh. La cache si svuota al cambio di
  workspace, perché le cartelle non sono più le stesse.
- **2026-08-04** — I due cBot sono stati rinominati: `PiootooTradingSessionBot` →
  **`PiootooDirectExecutionBot`**, `PiootooLiveTradingBot` → **`PiootooDistributedExecutionBot`**.
  I nomi vecchi suggerivano una divisione per contesto — uno da backtest, uno da live — che non
  esiste: entrambi girano in backtesting cTrader e in realtime, perché `ClientRunMode` è derivato da
  `Robot.IsBacktesting` e non è un parametro. La divisione reale è il canale di consegna dei segnali
  (intent già assegnati contro template da reclamare), ed è quella che ora si legge nel nome.

  Le **cartelle di stato locale** restano `%AppData%/PiootooLiveTradingBot` e
  `%AppData%/PiootooTradingSessionBot`: rinominarle orfanerebbe i file già scritti sulle macchine
  che operano, e il file del bot distribuito contiene il contesto di uscita delle posizioni aperte.
  Chi ricarica i sorgenti in cTrader deve però riaggiungere il bot ai grafici: per cTrader è un
  robot nuovo.

  I due documenti datati (`verifica-codice-2026-07-27.md`,
  `titano-analisi-parametri-e-audit-2026-07-31.md`) conservano i nomi vecchi: sono fotografie a una
  data, riscriverle falserebbe cosa era vero allora.
- **2026-08-04** — `PiootooDirectExecutionBot` e' **multi-coppia**: risolve tutti gli stream
  (simbolo, timeframe) dal descriptor di `open-plan` e non piu' dal grafico a cui e' agganciato. Una
  istanza copre l'intero piano, in backtest come in realtime. Non e' stato aggiunto un parametro di
  configurazione degli strumenti di proposito: sarebbe una seconda dichiarazione di cosa gira,
  accanto al masterfilter, e due dichiarazioni della stessa cosa divergono in silenzio.

  Il grafico resta solo come orologio e deve essere al timeframe **piu' fine** del piano, altrimenti
  gli stream rapidi verrebbero controllati troppo di rado. Le conseguenze meno ovvie, tutte dovute
  al fatto che prima esisteva un solo simbolo: volumi normalizzati con i passi dello strumento
  dell'intent e non del grafico; `StopLoss`/`TakeProfit` convertiti col `PipSize` di quello
  strumento; break-even e trailing letti da `Symbols.GetSymbol(position.SymbolName)`;
  `MaxBarsInPosition` e la scadenza degli ordini pending contate sulla serie dello stream che ha
  aperto, perche' "una barra" e' quella della strategia. Sbagliare uno di questi non produce un
  errore, produce un'uscita al prezzo o alla barra sbagliata.

  I bot non piu' in uso (`PiootooRiskGuardianBot`, `PiootooSignalReplayBot`) sono prefissati con
  `__` nel nome del file: nessun documento li referenziava.

- **2026-08-04** — Aggiunto `POST /api/v1/trading-sessions/{id}/promote-to-backtest`. Le due meta'
  della catena Titano scrivevano e leggevano in alberi diversi: una sessione persiste in
  `<workspace>/sessions/<id>/`, mentre `TitanoRotationService` legge
  `<workspace>/backtests/<cartella>/trades.json`. Un backtest eseguito dall'engine cTrader produceva
  quindi i trade ma non poteva alimentare le rotazioni, benche' la documentazione lo dichiarasse
  come "campione sorgente". L'endpoint copia trades e signals nella cartella attesa.

  Due rifiuti espliciti invece di comportamenti plausibili: promuovere una sessione **senza trade**
  e' un errore, perche' una rotazione su campione vuoto non fallisce ma disabilita tutte le
  strategie; sovrascrivere una cartella esistente richiede conferma, perche' i run Titano gia'
  calcolati portano nel proprio id l'hash del `trades.json` di origine e cambiarlo sotto di loro li
  rende non riproducibili.
- **2026-08-04** — Le sessioni di **backtest aperte da piano** persistono direttamente in
  `<workspace>/backtests/{piano}-{executionKey}/`, non piu' in `sessions/<guid>/`. Il campione
  prodotto dall'engine cTrader e' cosi' gia' dove `TitanoRotationService` lo cerca. La copia
  esplicita restava necessaria a ogni run e dimenticarla non dava errore: dava una rotazione
  calcolata sul campione precedente, cioe' un risultato plausibile e sbagliato.
  `promote-to-backtest` resta per i casi non coperti (sessioni senza piano, storico realtime).

  Le sessioni realtime restano sotto `sessions/` — non sono campioni e confonderle con i backtest
  renderebbe ambiguo cosa Titano puo' leggere — ma prendono anch'esse il nome `{piano}-{executionKey}`
  al posto del GUID. Il nome passa da una sanitizzazione: finisce in un path, e il separatore `|`
  della chiave di esecuzione lo romperebbe. La pulizia delle cartelle vecchie resta manuale.
- **2026-08-04** — `PiootooDirectExecutionBot` persiste su file le **condizioni di uscita** delle
  posizioni aperte (`%AppData%/PiootooTradingSessionBot/session-{piano}-{account}.json`), come gia'
  faceva il bot distribuito. Prima le ricostruiva solo da `GET /intents`, e le sessioni del server
  vivono in RAM: dopo un riavvio del **server** quella chiamata torna vuota e le posizioni aperte
  restavano senza uscita a tempo, senza limite di barre, senza trailing — aperte fino a un segnale
  opposto. Il file e' quindi l'unica copia che sopravvive a quel riavvio, non un doppione.

  Si salvano solo le condizioni che il broker non conosce: stop loss e take profit sono livelli
  nativi e sopravvivono da soli. Si persiste il **conteggio** delle barre trascorse e non l'indice
  di barra, che al riavvio si riferirebbe a una serie caricata da un altro punto. Scrittura atomica
  su file temporaneo: il file viene riscritto a ogni apertura e chiusura, e un'interruzione a meta'
  lascerebbe le posizioni senza uscite.

  Al riavvio un record viene accettato solo se la posizione **esiste ancora sul broker** — quelle
  chiuse a mano mentre il bot era fermo spariscono da sole — e solo se il file appartiene alla
  sessione appena risolta. Il file ha precedenza sul server sulle posizioni che copre: sul conteggio
  barre la sua fonte e' la storia effettiva del bot.

  Resta scoperto: gli ordini **pending** e il picco per lo stallo dell'utile non sono persistiti, e
  ripartono rispettivamente dalla riconciliazione col server e dall'utile corrente.
- **2026-08-04** — I **setup di rotazione Titano** diventano un'anagrafica della shell
  (*Anagrafiche -> Setup Titano*, lista + dettaglio) invece di una combo con «carica/salva» dentro la
  schermata operativa. Stavano nel posto sbagliato: un setup e' globale — vive in
  `settings/titano-rotation-setups/`, non appartiene ad alcun workspace e si applica a quanti se ne
  vuole — quindi appartiene alle anagrafiche accanto ad account e gruppi. Quello che e' per workspace
  e' il **run**, che nasce dai `trades.json` di uno specifico backtest. La schermata *Titano* resta
  operativa: sceglie workspace, cartella e setup, ed esegue.

  Nel dettaglio i circa trenta parametri numerici stanno in un `PropertyGrid` legato a
  `TitanoRotationSetup`, non in controlli replicati a mano. Replicarli sarebbe una seconda
  dichiarazione dello stesso modello, e al primo parametro aggiunto la UI resterebbe indietro
  salvandolo al proprio default senza segnalare nulla. Nome e descrizione restano campi propri
  perche' sono cio' con cui il setup si sceglie altrove.

  Aggiunta `DELETE /api/Titano/rotation-setups/{id}`. I tre setup predefiniti
  (`conservativo`, `bilanciato`, `dinamico`) non sono eliminabili: il servizio li ricrea a ogni
  avvio, quindi cancellarli darebbe una eliminazione riuscita e un setup che riappare al riavvio.
  Il server rifiuta e la lista tiene il pulsante spento.

  La schermata *Titano* perde i pulsanti «Carica setup» e «Salva setup»: la selezione dalla combo
  applica direttamente i parametri, e il salvataggio vive solo nell'anagrafica. Poterlo salvare da
  due punti avrebbe prodotto due versioni della stessa ricetta, con run che portano l'id di una e i
  parametri dell'altra. I parametri restano modificabili sulla schermata, perche' una rotazione una
  tantum e' un caso legittimo, ma la modifica vale per quella sola esecuzione.
- **2026-08-04** — I parametri di `TitanoRotationSetup` portano annotazioni `System.ComponentModel`
  (`Category`, `DisplayName`, `Description`, `Browsable`): il PropertyGrid del dettaglio setup li
  raggruppa in sette sezioni numerate — calendario, finestre di misura, soglie di ammissione,
  anti-whipsaw, allocazione, costi, walk-forward — con etichette in italiano e una riga di aiuto.

  Le annotazioni stanno sul modello in `Piootoo.Shared` e non in una classe adattatrice del client.
  Un adattatore sarebbe una seconda dichiarazione dello stesso modello e al primo parametro aggiunto
  resterebbe indietro, salvandolo al proprio default senza segnalarlo. Sono metadati, non logica:
  l'invariante di `Piootoo.Shared` (nessuna dipendenza verso gli altri progetti) resta intatta.

  `TitanoSizingTier` passa da `init` a `set`: l'editor di collezioni del PropertyGrid costruisce la
  voce e poi ne assegna le proprietà, quindi con `init` gli scaglioni sarebbero stati visibili ma
  non modificabili.
- **2026-08-04** — Ogni cartella di backtest porta un marcatore `origin.json` (`Internal` /
  `ExternalBroker`, piu' piano ed execution key per l'origine esterna). Da quando le sessioni di
  backtest scrivono anch'esse sotto `backtests/`, i due tipi convivono nello stesso albero e
  sceglierne uno per l'altro come campione Titano non da' alcun errore: da' numeri diversi.

  L'origine e' **dichiarata alla creazione** e non dedotta a posteriori. L'alternativa era
  desumerla dalla presenza di `backtest-summary.json`, che scrive solo il motore interno: ma un run
  interno interrotto prima della fine non ce l'ha e verrebbe etichettato come esterno. La scrittura
  del marcatore non puo' far fallire un backtest ne' l'apertura di una sessione: in caso di errore
  si tace e chi legge tratta l'assenza come `Unknown`, che e' anche lo stato delle cartelle
  preesistenti.

  L'origine compare nelle combo di scelta della cartella (Titano e dettaglio piano) e diventera' una
  colonna con filtro nella nuova lista dei backtest.

- **2026-08-04** — Nuovi endpoint sui backtest: `GET {ws}/backtests/{cartella}/summary` (restituisce
  `backtest-summary.json` grezzo, non deserializzato — un contratto tipizzato mostrerebbe un summary
  incompleto a ogni campo aggiunto senza segnalarlo), `GET .../titano-runs` e
  `DELETE {ws}/backtests/{cartella}`. La cancellazione porta via anche i run Titano contenuti: il
  server non lo impedisce, ma il client deve elencarli e avvisare, perche' i piani che li
  referenziano falliranno all'apertura della sessione e non prima.

- **2026-08-04** — *Operativita' → Backtesting* apre la lista dei backtest, non piu' il form di
  avvio: quest'ultimo (`BacktestingScreen`) e' diventato la destinazione di *Nuovo backtest*, come
  per tutte le altre voci lista → dettaglio. Il dettaglio (`BacktestDetailScreen`) e' **di sola
  lettura**: un backtest e' un artefatto prodotto da un run, non un'anagrafica, e renderlo
  editabile lo renderebbe incoerente con `backtest-log.jsonl`, che e' append-only.

  Il tab *Riepilogo* mostra il blocco `diagnostics` di `backtest-summary.json` **prima** del JSON
  grezzo, in una lista dedicata: e' la prima cosa da leggere quando un backtest non produce trade,
  e annegato in duecento righe di JSON non lo legge nessuno. Il summary resta comunque visibile per
  intero, perche' il contratto non e' tipizzato e i campi nuovi devono comparire da soli.
  L'assenza del file non e' un errore della schermata: manca in un run interrotto e nei run
  dell'engine esterno, e le operazioni restano leggibili lo stesso.

  La voce *Analisi → Risultati trading* e' stata **rimossa** e `TradingResultsScreen` cancellata:
  il tab *Operazioni* del dettaglio la assorbe. Tenerle entrambe avrebbe significato due griglie
  degli stessi `trades.json` da mantenere allineate, e soprattutto separava i trade dalla
  diagnostica che spiega perche' sono quelli e non altri.

- **2026-08-04** — `WorkspaceService.ReadBacktestPeriod` legge i primi 64 KB del file di risultato
  con `Utf8JsonReader` invece di fare `JsonDocument.Parse(File.ReadAllText(path))`.

  Il codice precedente deserializzava il risultato **intero** — equity ora per ora di ogni
  strategia, decine o centinaia di MB — per ricavarne due date che stanno nei primi duecento byte,
  e lo faceva una volta per cartella a ogni `ListBacktests`. Sul workspace `pts-02` sono 423 MB
  letti (e il doppio allocati, perche' `ReadAllText` materializza una stringa UTF-16) ogni volta
  che si apre la lista dei backtest: era quello il ritardo di cui ci si lamentava. Con la lettura
  della sola testa diventano 448 KB.

  `StartDate` ed `EndDate` sono la quarta e la quinta proprieta' di `BacktestingResult`, quindi la
  finestra e' scelta con tre ordini di grandezza di margine, non stimata. Il reader lavora con
  `isFinalBlock: false` perche' il buffer taglia il JSON a meta' per costruzione, e considera solo
  le proprieta' di primo livello: una omonima dentro `HourlyResults` darebbe la data sbagliata.
  Il file si apre con `FileShare.ReadWrite`, cosi' l'elenco non contende il lock con un backtest
  che sta scrivendo nella stessa cartella.

