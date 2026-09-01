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
- **2026-08-19** — **Chiuso il problema aperto del 2026-08-02: il feed dichiara il proprio
  orologio, e la sessione diventa una proprieta' della strategia.** Un commit solo, perche' le due
  meta' non sono separabili.

  *Cosa c'era.* Gli orari di sessione erano due interi `HHMM` il cui fuso veniva dedotto dal
  **simbolo** (`InstrumentRegistry.CreateSessionClock`), e i timestamp del feed venivano
  ri-etichettati UTC da `TradingDateTime.ToFeedUtc` senza essere convertiti. Due conseguenze. La
  prima: una strategia non poteva dichiarare una sessione diversa da quella che il registro
  attribuiva al suo simbolo — semplificazione legittima per una sorgente EasyLanguage, che gira su
  un simbolo solo, insostenibile per una piattaforma che ne ospita molte. La seconda: siccome
  l'istante della barra era una bugia, il confine di sessione cadeva **un'ora prima d'inverno e due
  d'estate** rispetto a quello che la ricerca aveva usato.

  *Cosa c'e' adesso.* `ZonedWindow` — `(HHMM inizio, HHMM fine, fuso IANA)` — e su `EasyEngineBase`
  due proprieta' distinte: `Session`, il confine di sessione, e `TradingWindow`, la finestra
  operativa. **Entrambe in ora della ricerca**, e per una ragione che vale la pena scrivere: il
  motore Python confronta la finestra con l'orario **della barra stessa** (`filters.py`:
  `minuti = index.hour * 60 + index.minute`), senza riferimento alla sessione, e taglia le sessioni
  con `(timestamp − 1 min − session_start_hour).normalize()`, che con `session_start_hour = 0` da'
  il **giorno di calendario europeo**, non la sessione CME. Sono due meccanismi indipendenti, ed
  entrambi vivono nell'orologio della ricerca.

  *Una correzione del 19/08 stesso.* La prima stesura dichiarava la sessione come quella di borsa
  (`1700` Chicago, `1800` New York), sul ragionamento che mezzanotte europea e la riapertura CME
  fossero lo stesso istante. Lo sono, tranne nelle settimane in cui l'ora legale americana ed
  europea non sono allineate — ed e' esattamente li' che il port divergerebbe dalla fonte.
  Confermato da chi ha scritto il motore Python: la sessione del modello e' il giorno europeo, e il
  port deve riprodurre quella, non il broker. `ZonedWindow.ResearchSession()` e
  `ZonedWindow.ResearchHours(...)` sono le due forme da usare. Sono due perche' i due fusi
  non coincidono e non devono piu' essere riconciliati a mano. `FeedClockRegistry` legge
  `datafeed/feed-clocks.json`, dove ogni feed dichiara il proprio orologio, e
  `DataSourceRepository` converte a UTC vero **una volta sola** al caricamento; un feed non
  dichiarato e' un errore esplicito, mai un'assunzione.

  *Le misure che lo dimostrano.* Sul feed a 15 minuti di @NQ, dopo la conversione, la pausa di
  manutenzione CME cade sugli slot **16:15-16:45 di Chicago** e il picco di volume sulle **08:30 di
  Chicago** — l'apertura del cash di New York — **in entrambe le stagioni**. Prima non era cosi',
  ed e' il controllo che rivela un orologio sbagliato: se il fuso non torna, le due misure si
  spostano fra gennaio e luglio.

  *E che l'ora legale e' quella europea.* Prendendo la barra a volume massimo di ogni giorno del
  2024, il picco sta alle 22:00 a febbraio, alle **21:00 dall'11 al 28 marzo**, e di nuovo alle
  22:00 ad aprile. Quelle tre settimane sono la finestra fra il passaggio americano all'ora legale
  (10 marzo) e quello europeo (31 marzo): il mercato americano compare un'ora prima. Stesso effetto
  nel 2013, con l'apertura cash che passa da 15:45 a 14:45. Con un offset fisso da UTC quello
  scarto non esisterebbe. Misura di chi ha scritto il motore Python, coerente con le due nostre.

  *Quanto e' cambiato.* Su `PTS_NQ_TFU_001_15`, finestra 17:00-10:00 CET, il numero di barre
  accettate resta lo stesso ma **l'insieme cambia**: 48 barre diverse in un mese d'inverno, 88 in
  un mese d'estate. E' la finestra che prima era spostata di un'ora e di due. Ne segue che **i
  backtest archiviati non sono confrontabili** con quelli successivi a questa voce.

  *Le cinque `ClaudioUnger/` sono state rimosse.* Non passavano da `EasyEngineBase`: leggevano
  l'ora grezza della barra e avevano l'inizio sessione cablato a `23` UTC, un'approssimazione
  tarata sul feed etichettato in ora europea. Portarle avrebbe voluto dire riscriverle; nessun
  codice le referenziava e non erano nel masterfilter, quindi sono state spostate in
  `_to_delete/ClaudioUnger-20260819/` invece che adattate.

  *Il vincolo e' ora imposto dal sorgente.* `StrategyClockConformanceTests` fallisce se qualcuno
  rilegge `.Hour`, `.Minute`, `.DayOfWeek` o `.TimeOfDay` di una barra dentro `Piootoo.Strategies`
  — con l'unica eccezione dei valori gia' passati dall'orologio — e se una `PTS_*` non dichiara
  sessione e finestra con il proprio fuso. Nello stesso giro sono sparite da `EasyLib` le due
  funzioni che quell'errore lo rendevano comodo, `GetHhmm` e `CombineDateAndHhmm`: non le chiamava
  piu' nessuno.

  *Cosa resta fuori.* Il feed giornaliero `D` va ricostruito sulla sessione invece che sul
  calendario, come questa stessa voce prevedeva gia' il 2026-08-02, e le serie sintetiche dei test
  costruiscono ancora barre a orari UTC coincidenti per costruzione con gli orari di sessione.

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

- **2026-08-04** — Le griglie delle liste sono ordinabili per colonna. La collezione passa da
  `BindingList<T>` a `SortableBindingList<T>` (`Shell/Controls/SortableBindingList.cs`) e le
  colonne da `SortMode.NotSortable` a `Automatic` via `DataGridView.EnableColumnSorting()`.

  Il `NotSortable` sparso in nove schermate non era una scelta di interfaccia: `BindingList<T>`
  dichiara `SupportsSortingCore == false`, e una colonna `Automatic` su quella sorgente solleva
  `InvalidOperationException` al primo click sull'intestazione. Il problema era la collezione.

  L'ordinamento riordina **la lista sottostante**, non una vista sopra di essa: cosi' la
  corrispondenza 1 a 1 fra indice di riga e indice nella lista — che e' come tutte le schermate
  leggono la selezione — resta valida, e non c'e' da riscrivere ogni `SelectedRow`. Le liste
  svuotano e riempiono la collezione a ogni cambio di filtro, quindi `ApplyFilter` chiama
  `ReapplySort()`: senza, la griglia mostrerebbe la freccetta sull'intestazione con le righe
  tornate nell'ordine della sorgente, che e' il modo peggiore di sbagliare.

  Le quattro griglie *editabili* (mappature simbolo negli account e nei preset, gruppi e strumenti
  del piano) restano `BindingList<T>`: li' l'ordine e' quello in cui si sta scrivendo, e riordinare
  sotto il cursore mentre si compila una riga non aiuta nessuno.

- **2026-08-04** — Il cursore di attesa passa da `Cursor` sulla toolbar a `UseWaitCursor` sulla
  schermata che la contiene. `Cursor` valeva per il controllo e i suoi figli, cioe' per i
  quaranta pixel della barra dei comandi: mentre si aspettava il server il mouse era sopra la
  griglia, dove il cursore restava una freccia normale. `UseWaitCursor` si propaga a tutti i figli.

  In piu' `MainShellForm.ActivateAsync` scrive «Caricamento…» **prima** della await e mette in
  attesa il pannello dei contenuti: le schermate scrivono il proprio stato solo alla fine del
  caricamento, quindi fino a quel momento la barra mostrava il risultato precedente. E il cambio
  di workspace nelle liste backtest e piani, che rilegge l'intero elenco ed e' il percorso piu'
  lento, non chiamava `SetBusy` affatto.

- **2026-08-04** — *Operativita' → Run Titano* apre la lista dei run del workspace
  (`TitanoRunListScreen` → `TitanoRunDetailScreen`); `TitanoScreen` diventa la destinazione di
  *Nuova rotazione*. La sezione *Analisi* sparisce: conteneva *Rotazioni Titano*, che era il
  dettaglio di un run raggiunto per altra strada.

  La voce **non** si chiama «Backtesting Titano». Un run non e' un backtest: e' una rotazione
  calcolata *sui trade* di un backtest. Metterli sotto la stessa parola e' la stessa famiglia di
  confusione di `Id` ≠ `Name`, che qui ha gia' svuotato report una volta.

  La lista e' **piatta per workspace**, con la cartella di provenienza in colonna. I run vivono
  dentro il backtest che li ha prodotti (`<backtest>/titano/<runId>/`), ma per chi ne referenzia
  uno quella gerarchia e' un dettaglio di archiviazione: `GET /api/Titano/rotations` accetta ora
  `backtestFolder` come parametro facoltativo e senza di esso scandisce tutte le cartelle.

  `TitanoRotationService.ScanRunInfo` legge dal manifest solo i quattro campi dell'elenco con
  `Utf8JsonReader` e `Skip()` sui sottoalberi grossi (config, le due equity, walk-forward), invece
  di deserializzarlo intero come faceva `ListRuns`. Stessa lezione di `ListBacktests`: oggi i
  manifest sono ~1 MB e i run pochi, ma crescono entrambi.

  Nuovo `DELETE /api/Titano/rotations/{runId}`, che rimuove anche la voce dalla cache statica dei
  manifest — altrimenti un run cancellato resterebbe risolvibile fino allo sfratto LRU, cioe' si
  continuerebbe a ruotare su un file che non c'e' piu'. La conferma lato client **nomina i piani**
  che referenziano il run invece di dare l'avviso generico usato per i backtest: qui il
  riferimento e' puntuale (`TradingPlan.TitanoRunId`, sul piano o sul singolo gruppo), quindi si
  puo' dire *cosa* si sta rompendo. Il controllo guarda anche i gruppi: fermarsi al mirror legacy
  della prima riga lascerebbe fuori i piani multi-gruppo, che sono quelli in cui la rotazione
  conta di piu'.

- **2026-08-04** — Le regole delle schermate della console stanno in
  `.cursor/rules/piutoo-console-screens.mdc`, non sparse nei commenti. La prima e' che **ogni
  griglia deve essere ordinabile**: era una regola implicita nata sbagliata (nove schermate con
  `NotSortable` copiato) e finche' non era scritta si e' propagata per imitazione.
- **2026-08-04** — La tabella di conversione symbol non vive più embedded dentro l'account
  (`WorkspaceAccount.SymbolMappings`, una tabella non nominata per account): è diventata un
  registro globale di `SymbolConversion` (Nome + Codice + righe), fuori sia dal workspace sia
  dall'account, così più account possono condividere la stessa tabella e la si gestisce in un
  posto solo (Anagrafiche → Conversioni simbolo). L'account referenzia una tabella per codice
  (`WorkspaceAccount.SymbolConversionCode`, vuoto = nessuna conversione, 1 a 1) invece di editarla:
  `AccountDetailScreen` e il tab Accounts della console legacy espongono solo la combo di
  selezione. Il vecchio preset condiviso (`settings/default-symbol-conversion.json`, un unico
  documento senza identità) è sostituito dallo stesso registro: non serve più un concetto
  separato di "punto di partenza dei nuovi account" perché un account nuovo semplicemente non
  referenzia nessuna tabella. Migrazione: le due tabelle già embedded nei due account di test sono
  diventate `primo-ftmo` e `default-futures` in `piootoo-repository/accounts/symbol-conversions.json`,
  quest'ultima è anche il preset identità (ogni symbol del catalogo su se stesso, moltiplicatore 1)
  materializzato come voce nominata riutilizzabile invece che ricalcolato ogni volta.
- **2026-08-04** — Un piano di trading non indica più `TitanoRunId`: referenzia solo una cartella
  di backtest, e la sessione risolve **sempre l'ultimo run generato per quella cartella**
  (`TitanoRotationService.ResolveLatestRun`), ricalcolato a ogni barra invece che congelato allo
  snapshot del piano. Motivo: rifare una rotazione a fine periodo richiedeva di riaprire ogni piano
  e ripuntare a mano il nuovo `TitanoRunId` — un passaggio facile da dimenticare, e dimenticarlo
  non dava errore, dava una sessione che continuava silenziosamente sulla decisione vecchia. Con la
  risoluzione dinamica una sessione già aperta applica un run più recente dalla barra successiva
  alla sua generazione, senza riavviare il cBot. Aggiunto `TitanoRotationStatus`
  (`Fresh`/`Stale`/`NoRun`, da `TitanoRotationService.GetFreshness`): stale appena `now` supera
  `EffectiveToUtc` dell'ultima decisione, senza tolleranza — è il segnale che manca una rotazione
  per il periodo corrente. Esposto da `GET .../trading-plans/{code}/rotation-status` e mostrato in
  lista e dettaglio piani. Il percorso non-piano (`CreateTradingSessionRequest.TitanoRunId`)
  resta un pin esplicito opzionale per test e integrazioni. Fuori scope:
  `BacktestingRequest.TitanoRunId` del backtest ad-hoc della console resta manuale, per restare
  riproducibile.

- **2026-08-05** — La schermata **Setup Titano** distingue i parametri **Base** dagli **Avanzati**,
  invece di presentarne trenta tutti sullo stesso piano. Il livello è un attributo sul modello
  (`TitanoLevelAttribute` su ogni proprietà di `TitanoRotationSetup`) e non un elenco nel client:
  il `PropertyGrid` lo usa via `BrowsableAttributes`, che confronta gli attributi per valore — da
  cui l'`Equals` ridefinito. Tenerlo sul modello è la stessa scelta già fatta per `Category` e
  `Description`: un elenco nel client sarebbe una seconda dichiarazione del modello, che al primo
  parametro aggiunto resta indietro in silenzio. Un test verifica che ogni proprietà visibile abbia
  il livello, perché una dimenticanza farebbe sparire il parametro dalla vista Base senza errori,
  facendolo salvare al proprio default.
  Sono Base i dieci parametri su cui si decide davvero: cadenza, finestra breve, voti richiesti, le
  tre soglie di drawdown (spegnimento, rientro, blocco definitivo), il fermo dopo un OFF, la scelta
  del sizing e i due estremi di allocazione. Tutto il resto è calibrazione fine.
- **2026-08-05** — Le frazioni di `TitanoRotationSetup` si **inseriscono e si leggono come
  percentuali** (`PercentTypeConverter`). Il modello e il contratto verso il server restano in
  frazioni — la serializzazione JSON non passa dai `TypeConverter` — ma sparisce il campo in cui si
  doveva indovinare se `15` volesse dire 15% o 1500%. Era un errore di fattore 100 che non produce
  eccezioni: produce un manifest con tutte le strategie accese, o tutte spente, e nessun messaggio.
  Il converter accetta sia la virgola sia il punto.
- **2026-08-05** — Aggiunto sotto il grid un **riepilogo in prosa della configurazione**
  (`TitanoSetupSummary`), con gli avvisi di coerenza. Motivo: i tooltip spiegano un parametro alla
  volta, ma il comportamento nasce dalla loro combinazione — la soglia di rientro ha senso solo
  relativamente a quella di uscita, la finestra di misura solo relativamente alla cadenza. Il
  riquadro segnala isteresi assente, blocco definitivo non oltre la soglia di spegnimento, finestre
  invertite, allocazione degenere, e le due trappole di calibrazione già documentate nell'audit del
  31/07: finestra breve molto più lunga della cadenza (si ruota spesso e si decide piano) e
  parametri della categoria 6 inerti quando il sizing è per classifica — la coda di **B3**, che nel
  `PropertyGrid` non si può disabilitare per valore come si era fatto nella vecchia form a
  `NumericUpDown`, quindi si dichiara.
- **2026-08-05** — La schermata setup può **partire da un preset**: la combo elenca i setup
  esistenti (compresi i tre professionali seminati dal server) e ne copia i parametri lasciando
  intatti id, nome e descrizione. Chi applica un preset ne vuole la calibrazione, non l'identità.
- **2026-08-05** — **Il backtest interno è neutro rispetto agli account.** Un run è
  *balance iniziale + strategie del masterfilter del workspace + datafeed*, e nient'altro:
  niente conversione di simbolo, `ContractMultiplier` e `BalanceScale` fissi a 1, nessuna
  size scalata sul capitale di un conto. Gruppi, slot e limiti di concorrenza non c'erano
  già (vivono in `TradingSessionService`); l'unica contaminazione era
  `BacktestingRequest.AccountId`, che tirava dentro `AccountSymbolConversion`. Rimosso:
  cadono `ResolveAccountConversion` e `TryApplyAccountConversion` da
  `PiootooBacktestingService`, il parametro di conversione di `ToPersistedSignals`, la
  dipendenza `WorkspaceService` del servizio e la combo account di `BacktestingScreen` e
  della console legacy.
  Il motivo non è la semplicità: **quel run è il campione sorgente di Titano**. Con la size
  legata al conto, due backtest identici su account diversi producono rotazioni diverse, e
  la rotazione starebbe misurando il capitale invece delle strategie. È lo stesso principio
  per cui `EnforceConcurrencyLimits` è già off nel backtest sorgente (B4 del 29/07): il
  campione misura le strategie, non l'operatività.
  Conversione di simbolo e scala per conto **restano intatte sulle sessioni**
  (`ExternalBroker`), dove sono essenziali: lì il segnale deve diventare un ordine
  eseguibile su un conto reale. I campi `AccountId`, `AccountSymbol`, `ContractMultiplier`
  e `AccountBalanceScale` restano nel `PersistedSignal` — il formato di `signals.json` resta
  unico, il backtest li scrive all'identità.
  Effetto collaterale accettato: un simbolo **disabilitato** su un account non filtra più
  nulla in backtest. Il backtest interno valuta tutto ciò che il masterfilter dichiara; la
  disabilitazione è una proprietà operativa del conto e agisce dove si opera.
- **2026-08-05** — **Rimosso l'overlay CPPI** dal position sizing (`EnableCppi`,
  `CppiFloorFraction`, `CppiMultiplier`). Entrava solo come `Math.Min` sul moltiplicatore
  già calcolato, quindi toglierlo non può ridurre alcuna size: i run con CPPI spento sono
  identici, quelli con CPPI acceso perdono un taglio che con i default (floor 0,80,
  moltiplicatore 1) valeva l'80% della size fin dalla prima barra. Restano i due freni di
  `PortfolioMultiplier` che arrivano comunque a zero: drawdown dal picco ed esposizione
  lorda. Attenzione alla console legacy, dove la checkbox CPPI accendeva anche
  `PortfolioRisk.Enabled`: chi voleva il solo freno di drawdown doveva attivare il CPPI.
- **2026-08-05** — **`InitialCapital` esce dal piano di trading** e resta un parametro del
  singolo run di backtest (`BacktestingRequest.InitialCapital`, già esposto da
  `BacktestingScreen`). Sulle sessioni cBot — sempre `ExternalBroker` — non aveva
  consumatori reali: `Snapshot()` ritorna `Balance = Equity = InitialCapital`, quindi
  l'equity non si muove mai, il drawdown è identicamente zero e il motore simulato non viene
  interrogato. Era una costante travestita da capitale.
  Soprattutto, **non era lui a scalare le size**: in `ExternalBroker` ogni account porta il
  proprio `InitialBalance`, che diventa `BalanceScale = InitialBalance / 1.000.000` e viene
  applicato — insieme al `ContractMultiplier` dello strumento — in `CloneForClaim`, cioè
  quando il destinatario è noto. Il dimensionamento per conto resta quindi intatto: quello
  che sparisce è solo un secondo capitale, di sessione, che non corrispondeva a nessun saldo.
  Di conseguenza **il freno di esposizione lorda è disattivato in `ExternalBroker`**: il suo
  denominatore era quel capitale fittizio, e sommava le posizioni di tutti gli account della
  sessione contro un unico numero che non era il saldo di nessuno. Il rischio di portafoglio
  live è governato dal broker — `PiootooRiskGuardianBot` lo fa sul balance vero — coerente
  con l'invariante "il server decide *cosa*, il broker decide *se e a che prezzo*".
  `CreateTradingSessionRequest.InitialCapital` resta con il proprio default: serve alle sessioni
  `ServerSimulated` create dall'API diretta e dal form manuale di `TradingSessionsScreen`.
  I `plans.json` esistenti non si rompono, la proprietà resta nel file e viene ignorata.
- **2026-08-05** — **Le strategie dichiarano l'ingresso, non la size.** Tutte espongono
  `Contracts = 1` (o `_contracts = 1m`) nel costruttore: il segnale dice *entra*, e la quantità
  nasce dai layer a valle. La catena completa è
  `1 × allocazione Titano × volatilità di mercato × rischio di portafoglio → arrotondamento →
  (solo ExternalBroker, al claim) BalanceScale × ContractMultiplier`.
  Resta l'override `"Contracts"` letto da `Initialize` e inoltrato da `StrategyFactory`: è una
  seconda leva di sizing fuori dai layer, **tenuta di proposito** per i casi in cui una strategia
  vada calibrata su una base diversa. Chi la valorizza deve sapere che sposta la base da cui tutti
  i moltiplicatori partono.
- **2026-08-05** — **Il backtest interno applica solo l'allocazione Titano.**
  `PiootooBacktestingService` fa `signal.Quantity *= titanoAllocation` e non chiama mai
  `PositionSizingService`: volatilità di mercato, freni di portafoglio, arrotondamento allo step e
  quantità minima agiscono soltanto nelle sessioni. Non è una dimenticanza ed è la stessa ragione
  dell'account-neutralità e di `EnforceConcurrencyLimits` off: il run è il campione sorgente di
  Titano e deve misurare le strategie a un contratto, non l'operatività. Va saputo prima di
  confrontare le quantità di un `trades.json` interno con quelle di una sessione: non sono
  omogenee per costruzione.
- **2026-08-05** — Il capitale di riferimento delle strategie vive in **una sola costante**,
  `TradingConventions.StrategyReferenceBalance` (1.000.000). Prima erano due letterali distinti —
  `AccountSymbolConversion.ReferenceBalance` e il default del capitale nella schermata di backtest —
  e non coincidevano: la shell proponeva 100.000 contro il milione della console legacy e del
  denominatore di `BalanceScale`. Un disallineamento che non produce errori, solo percentuali e
  scale che non parlano della stessa cosa. Il campo resta modificabile: cambiarlo sposta il
  denominatore di equity e drawdown del run, non le quantità, che nel backtest interno restano
  quelle dichiarate dalle strategie.
- **2026-08-05** — **La granularità di volume (minimo/passo/arrotondamento) è passata dal piano
  alla riga della tabella di conversione dell'account.** È una proprietà della coppia
  broker/strumento, non del piano: `TradingPlan.Instruments` e `SaveTradingPlanRequest.Instruments`
  sono stati rimossi insieme al tab *Strumenti* di `PlanDetailScreen`; `AccountSymbolMapping` porta
  ora `MinimumQuantity`, `QuantityStep`, `RoundingMode` (colonne nuove in
  `SymbolConversionDetailScreen`). `DollarsPerPoint` non è più configurabile: viene da
  `InstrumentRegistry.PointValue`, che lancia sui simboli non verificati (stesso invariante dei
  datafeed mancanti).
  Per non arrotondare due volte — una sui contratti Piootoo, una sui contratti del broker dopo la
  conversione — `QuantityRoundingMode` ha un terzo valore, `Deferred`: le sessioni
  `ExternalBroker` lo usano di default e `PositionSizingService`/`ApplyGroupAllocation` lo
  rispettano non arrotondando. L'arrotondamento vero avviene una sola volta, con la granularità
  reale del conto, in `AccountSymbolConversion.RoundQuantity` — chiamato sia da `CloneForClaim`
  (percorso multi-account) sia da `AddIntent` (esecuzione diretta senza claim, dove altrimenti la
  quantità non verrebbe mai arrotondata). Un simbolo senza riga in tabella non ha una granularità
  dichiarata dal broker: `RoundQuantity` applica comunque il default a contratto intero (passo 1,
  minimo 1) invece di lasciar passare una quantità frazionaria — "nessuna conversione" vale per
  simbolo e moltiplicatore, non per la granularità.
- **2026-08-06** — **`PiootooDistributedExecutionBot` non usa più il simbolo e il timeframe del
  grafico a cui è agganciato.** Prima il grafico era l'orologio comune: il bot pretendeva un chart
  al timeframe base (parametro `BaseTimeframeMinutes`, rimosso) e a ogni sua barra scorreva tutte
  le coppie del piano per vedere quali avessero chiuso. Il timeframe del chart quindi decideva la
  latenza di tutti gli stream, e un chart più lento delle coppie del piano ne perdeva le barre.
  Ora ogni coppia (simbolo, timeframe) del descriptor apre la propria serie nativa e si sottoscrive
  al proprio `Bars.BarOpened`: push della barra, conteggio di `MaxBarsInPosition` e claim del
  segnale avvengono per stream, ciascuno col proprio orologio. Conseguenze: i simboli del piano non
  disponibili sull'account fanno fallire l'avvio invece di essere scoperti al primo intent
  (stesso invariante dei datafeed mancanti); break-even e trailing si sottoscrivono a `Symbol.Tick`
  di ogni simbolo, perché `OnTick` del robot riporta solo i tick del chart; `CloseExpiredPositions`
  gira anche su `OnTimer`, perché senza la barra del grafico un piano di soli stream lenti
  valuterebbe `CloseAtUtc` con ore di ritardo.
- **2026-08-06** — **Le label di posizioni e ordini portano l'IntentId**:
  `PiootooLive:{StrategyCode}:{IntentId}` invece di `PiootooLive:{StrategyCode}`. Dal solo stato
  della piattaforma si risale al segnale che ha creato ciascun ordine o posizione, anche dopo un
  riavvio del cBot e senza lo stato locale; `BrokerPositionSnapshot`/`BrokerOrderSnapshot` portano
  il campo `IntentId` (vuoto per le label di formato precedente, ancora a mercato). Poiché la label
  cambia a ogni segnale, i match che riguardano la strategia e non il singolo intent — sostituzione
  dell'ordine pending della barra precedente, ricerca della posizione da chiudere per un intent
  `Close` — passano dal prefisso `PiootooLive:{StrategyCode}:` e non più dalla label esatta.
- **2026-08-06** — **Il client invia al server la finestra di candele, non la singola barra chiusa**
  (`POST /{id}/bars/window`, `PiootooDistributedExecutionBot`). Nelle sessioni `ExternalBroker` il
  server non ha un datafeed proprio: la storia di uno stream è soltanto ciò che gli è stato spinto,
  e `StrategyEvaluationService` salta la valutazione finché `history.Count < RequiredCandles`. Con
  una barra per volta ogni run partiva quindi da storia vuota e scartava in silenzio le prime
  `RequiredCandles` barre — per `PTS_NQ_PCH_001_15`, `PriceChannelEngine` a 15 minuti, sono
  `max(6 sessioni × 96, ChannelBars+1) = 576` barre, circa sei sessioni — e un backtest più corto
  di quella soglia non produceva un solo segnale, senza un messaggio. Peggio: in backtest
  `ExecutionKey = BT-{istante di avvio}`, quindi ogni run apre una sessione nuova e il
  riscaldamento non si eredita mai.
  Ora il bot carica la storia all'indietro con `Bars.LoadMoreHistory()` fino a coprire
  `RequiredCandles` e a ogni barra chiusa spedisce le ultime N candele; il server accoda quelle che
  non ha e valuta **solo l'ultima**, così la prima finestra fa da riscaldamento senza generare
  intent sul passato. La profondità la dichiara il server in
  `TradingInstrument.RequiredCandlesByTimeframe` (massimo fra le strategie del masterfilter su
  quello stream): un parametro locale del cBot sarebbe una seconda verità destinata a divergere dal
  masterfilter.
  **Le candele viaggiano in due tempi.** All'avvio, una volta per stream, parte tutta la storia
  richiesta con `EvaluateLastCandle = false`: il server accoda e basta, senza valutare, senza
  consumare l'idempotency key e senza avanzare la sequence — sono barre già passate, e valutarle
  produrrebbe intent sul passato che il bot eseguirebbe al prezzo di adesso. Poi, a ogni barra
  chiusa, una finestra corta (`IncrementalWindowBars`, default 20) di cui il server valuta l'ultima
  candela. Rispedire ogni volta l'intera finestra da 576 candele sarebbe stato più semplice ma
  costava ~50 KB di JSON per barra; mandare la sola barra chiusa era la versione rotta di partenza.
  Le 20 barre sono il margine: **la sovrapposizione non è banda sprecata, è ciò che impedisce i
  buchi.** Ogni giro perso — chiamata fallita, server irraggiungibile — lascerebbe altrimenti nella
  serie del server un vuoto permanente, e le strategie girerebbero su dati bucati senza
  accorgersene. Con 20 barre si ricuce da solo fino a 19 barre consecutive perse. E il buco non
  resta affidato alla buona volontà del client: la finestra deve **sovrapporsi** alla storia già
  presente, e il server rifiuta quella che comincia dopo la sua ultima candela nota. Il criterio è
  la sovrapposizione e non l'aritmetica sui timestamp perché gli stream hanno buchi legittimi —
  fine settimana, festivi, mercati chiusi — che una differenza in minuti scambierebbe per barre
  perse.
  `POST /{id}/bars` resta invariato per `PiootooDirectExecutionBot`, che non è stato migrato.
- **2026-08-06** — **La sessione non persiste le candele ricevute.** `session.History` vive in RAM;
  `TradingJsonStore` scrive signal, trade e rotation-log e nient'altro. Raccogliere il datafeed da
  cTrader e salvarlo su disco per i backtest locali sarà compito di un cBot raccoglitore dedicato,
  non della strada di esecuzione: mescolare le due cose farebbe dipendere la qualità del datafeed
  storico dagli orari in cui è girato un bot di trading.
- **2026-08-06** — **La risposta di `bars/window` porta la diagnostica per stream**
  (`HistoryBars`, `RequiredCandles`, `EvaluatedStrategies`, `SkippedForInsufficientHistory`) e il
  cBot la stampa una volta per stream, con quante barre mancano. Prima "nessuna strategia ha
  prodotto un segnale" e "il server non ha abbastanza storia per valutare" erano lo stesso identico
  silenzio: è la stessa ragione per cui il backtest interno ha il blocco `diagnostics` in testa a
  `backtest-summary.json`.
- **2026-08-06** — **In modalità `Disabled` un run Titano mancante non blocca più la sessione.**
  `CreateCore` accetta da sempre `TitanoMode = Disabled` con `TitanoBacktestFolder` valorizzato e
  nessun run per quella cartella — è lo scenario A di
  `domini/cbot-realtime-backtest-titano.md`, dove il piano dichiara la cartella in cui i trade
  verranno promossi ma la rotazione non esiste ancora, perché è proprio quel run a doverla
  alimentare. `EvaluateClosedBar` però risolveva il run appena la cartella era valorizzata, senza
  guardare la modalità, e lanciava: la sessione si apriva e poi falliva identica a ogni barra
  (409 "esegui prima una rotazione" per l'intero backtest, zero valutazioni). Ora in `Disabled` il
  run si risolve solo se esiste; se manca si annota nel rotation-log e si prosegue senza filtri,
  che è già la semantica della modalità. Le modalità filtrate continuano a fallire in modo
  esplicito: senza rotazione eseguirebbero tutto il masterfilter, cioè l'opposto di quanto chiesto.
- **2026-08-06** — **Il cBot non ripete l'errore di invio e si ferma se non passa più nulla.** Un
  messaggio identico allo scorso, sullo stesso stream, non viene ristampato; dopo
  `MaxConsecutivePushFailures` (20) invii falliti di fila senza uno riuscito in mezzo il bot chiama
  `Stop()`. Gli errori che bloccano l'invio sono di configurazione — piano che punta a una rotazione
  inesistente, sessione fermata, token scaduto — e non si risolvono da soli: prima un backtest
  arrivava in fondo producendo centinaia di righe identiche e nessuna valutazione.
- **2026-08-06** — **Gli intent generati si vedono sulla console del server**, una riga per intent in
  `TradingSessionsController` (`PushBars` e `PushBarWindow`), a livello Information: strategia,
  simbolo, lato, tipo ordine, prezzo, quantità finale con la base quando differiscono, stato ed
  eventuale `SizingReason`. Serviva un punto in cui il segnale è visibile *nel momento in cui nasce*:
  `signals.json` si legge a run finito, e il cBot vede solo ciò che gli viene consegnato, non un
  intent annullato dal sizing o dal limite di ingressi per sessione — che è proprio il caso da capire
  quando "non arriva niente". Il riempimento della storia barra per barra è invece a Debug: a
  Information sarebbero 576 righe di riscaldamento a soffocare i segnali, e la stessa informazione il
  cBot la stampa già una volta per stream.
- **2026-08-06** — **Nel claim "adesso" è l'ultima barra valutata, non `DateTime.UtcNow`.**
  `GetNextSignalForAccount` scartava i template scaduti confrontando `ExpiresAtUtc` con l'ora di
  sistema. In un replay storico le due date distano mesi, quindi **ogni** ordine "next bar" dei
  motori Unger nasceva già scaduto: il server generava e loggava i segnali come template `Pending`,
  il claim rispondeva sempre `NoSignal`, e sul broker non arrivava mai un ordine — un backtest
  perfettamente muto pur avendo prodotto i segnali. Stessa correzione in
  `CreateExternalCloseIntent`, dove l'ora di sistema datava la chiusura fuori dall'intervallo del
  run e quindi fuori da qualunque periodo di rotazione Titano. Il fallback a `DateTime.UtcNow` resta
  solo prima della prima barra, quando `LastEvaluatedBarTimeUtc` è ancora null.
  Non contraddice l'invariante "adesso è `DateTime.UtcNow`" di `CLAUDE.md`: quello vale per il tempo
  reale: dentro un replay l'orologio autorevole è la barra, come già in
  `docs/domini/orologio-barre-e-fill.md`. Regressioni in
  `MultiAccountDistributionTests.TemplateWithExpiry_OnHistoricalBars_IsStillClaimable` e
  `TemplateExpiredBeforeTheCurrentBar_IsNotClaimable`.
- **2026-08-06** — **Il claim dice perché non consegna niente.** `AccountSignalResponse` porta
  `ReasonDetail` accanto a `Reason` (che resta il codice stabile, "NoSignal"/"SessionNotRunning",
  su cui i test fanno match): i filtri di `GetNextSignalForAccount` sono applicati a stadi invece che
  in un'unica catena LINQ, così si sa **quale** ha svuotato la lista e quanti template ha scartato —
  simbolo non abilitato sulla tabella di conversione, template scaduti rispetto alla barra corrente,
  già reclamati dal gruppo, slot occupato, account già impegnato su quel simbolo, limite di ingressi,
  esclusione Titano. Caso a parte e il più insidioso: template idoneo ma con quantità azzerata dalla
  conversione dell'account (BalanceScale × moltiplicatore contratto, poi arrotondamento del broker),
  che dal client è identico a "nessun segnale". Il server logga l'esito del claim una volta per
  motivo, il cBot stampa il motivo senza bisogno di `VerboseLogging`. Fra "template generato" e
  "ordine sul broker" c'è tutto il secondo layer di filtro: è lì che un run resta muto pur avendo
  prodotto i segnali, ed era l'unico tratto senza diagnostica.
- **2026-08-06** — **Il controllo di slippage del cBot vale solo per gli ordini a mercato.** Uno Stop
  o un Limit sta per definizione lontano dal prezzo corrente — è il livello a cui si vuole entrare,
  non quello a cui si è — quindi misurarne la distanza come slippage scartava sistematicamente gli
  ordini che i motori Unger emettono sempre: con `MaxEntrySlippagePips` a 5, un breakout di Donchian
  a decine di punti dal prezzo veniva rifiutato a ogni barra. Lo slippage di un pending lo governa il
  broker al fill, non il bot al piazzamento.
- **2026-08-06** — **Quando il cBot cancella un ordine pending lo riporta al server come
  `Cancelled`.** Prima lo cancellava solo sul broker: l'intent restava `Pending` lato sessione,
  e `GetNextSignalForAccount` restituisce per primo proprio l'intent già assegnato e ancora
  pendente. Risultato osservato su un run reale: un solo ordine piazzato all'avvio, cancellato dopo
  la sua barra, e poi lo **stesso** intent riproposto a ogni poll per il resto del backtest — il bot
  lo scartava perché già gestito (`_submittedIntentIds`), i lucchetti (account, simbolo) e
  (gruppo, strategia, simbolo) restavano chiusi, e non arrivava più nessun segnale nuovo. Il report
  vale per tutti i punti di cancellazione: scadenza "next bar", sostituzione da parte del signal
  successivo, flat di fine settimana. L'IntentId si legge dalla label, che è il motivo per cui ce
  l'ha. Stesso trattamento all'ingresso scartato perché il simbolo ha già una posizione: annullato e
  riportato, non ignorato in silenzio — e comunque un segnale Unger vale la sua barra, non quella in
  cui il simbolo tornerà libero.
- **2026-08-06** — **I template scaduti vengono rimossi dalla sessione, non solo filtrati al claim.**
  `EntryTemplates` cresceva per tutta la durata del run — un template per segnale, mai rimosso — e
  ogni claim li riscorreva tutti per scartare quelli fuori finestra. Non era un rischio di
  esecuzione (il filtro di scadenza c'era e funzionava), ma costava tre cose: una lista che cresce
  senza limite con un costo per poll proporzionale, una diagnostica che continuava a parlare di
  template di barre vecchie invece di dire che per la barra corrente non c'era alcun segnale, e
  soprattutto l'impossibilità di convincersi leggendo il codice che un segnale di una barra passata
  non potesse più essere eseguito. Ora `PushBarWindow`/`PushBars`, all'arrivo di ogni barra,
  eliminano i template con `ExpiresAtUtc` già passato e la traccia dei gruppi che li avevano
  reclamati. Si rimuovono solo quelli con una scadenza dichiarata: senza `ExpiresAtUtc` non c'è una
  finestra da far scadere, e su una sessione multi-timeframe un template del 60m deve sopravvivere
  alle barre del 15m che gli passano accanto.
  Corollario sulla diagnostica: i motivi del claim non contengono più l'orario della barra. Client e
  server li deduplicano per stringa, quindi un valore che cambia a ogni barra mandava a vuoto la
  deduplica e riempiva entrambi i log di righe identiche nella sostanza.
- **2026-08-06** — **Il cBot dichiara il profilo del run, e i lucchetti di concorrenza lo seguono.**
  `EnforceConcurrencyLimits` governava solo `MaxConcurrentTrades`: il passo 1 del claim (un intent
  pendente per account) e i lucchetti (gruppo, strategia, simbolo) e (account, simbolo) restavano
  incondizionati. Un backtest "sorgente" fatto col cBot distribuito produceva quindi **un trade alla
  volta per simbolo e un intent per poll**, cioè un `trades.json` mutilato proprio nel run che deve
  contenere tutti i segnali perché Titano ci calcoli sopra le rotazioni — e incomparabile col
  backtest interno, che di lucchetti non ne ha. Su un run reale (piano a simbolo singolo, USTEC su
  15m e 60m) l'effetto era una posizione aperta e cinque giorni di `l'account ha già un intent attivo
  su quel simbolo`. Ora i lucchetti operativi seguono il flag; restano fuori `TemplateClaimedGroups`,
  che non è un vincolo di concorrenza ma la memoria di cosa è già stato servito a un gruppo, e le
  chiusure al passo 1, che vanno consegnate sempre.
  Il flag però non è il modo giusto di scegliere: descriveva la stessa decisione di
  `ApplyTitanoFilters` in un secondo posto, e per passare da un backtest all'altro si doveva editare
  il piano. Il cBot dichiara invece `TradingRunProfile` — `DalPiano` (default, storico),
  `BacktestSorgente` (Titano off, lucchetti off), `BacktestTitano` (rotazioni storiche, lucchetti
  attivi) — che prevale sul piano, entra nella chiave di esecuzione perché due profili non si
  riprendano a vicenda, ed è rifiutato in realtime. Conseguenza sul client: senza il tappo di un
  intent per account il cBot deve **drenare** la coda dei segnali invece di fermarsi al primo.
- **2026-08-06** — **L'orologio dei cBot è la serie di ogni stream, non il grafico.**
  `PiootooDirectExecutionBot` usava `OnBar()`, quindi il grafico doveva essere al timeframe più fine
  del piano e, su un piano misto (indice + forex), le barre del simbolo che stava scambiando non
  venivano pubblicate finché quello del grafico era chiuso. Ora ogni `PlanStream` sottoscrive
  `Series.BarOpened` e il grafico non è più l'orologio di niente: né come timeframe né come simbolo.
  Cade con questo il vincolo sul timeframe del chart, e con lui la lettura di `TimeFrame` all'avvio
  che fermava il bot su un grafico Renko o a tick.
- **2026-08-06** — **Il pannello a chart mostra la configurazione risolta dal server, non i
  parametri del cBot.** Piano, run mode, profilo, stato del filtro Titano, lucchetti e limite di
  trade, ed elenco delle strategie con il loro timeframe, letti tutti dal descriptor di sessione. Un
  bot che dichiara un piano e ne esegue un altro, o un parametro che il piano contraddice, sono
  altrimenti invisibili finché non si leggono i trade. Il descriptor espone per questo `RunProfile`,
  `EnforceConcurrencyLimits`, `MaxConcurrentTrades` e `Strategies`.
- **2026-08-06** — **Il push dichiara se c'è qualcosa da reclamare, e il cBot salta il poll quando non
  c'è.** In backtest ogni barra di ogni stream costava due chiamate HTTP sincrone — push e poll — e
  dai log reali la grande maggioranza delle barre non produce alcun segnale: metà del traffico di un
  run serviva a farsi dire "niente". `PushBarWindowResponse.ClaimableIntents` conta ora i template
  `Pending` non scaduti più gli intent già assegnati e ancora pendenti; a zero,
  `GetNextSignalForAccount` non può restituire nulla per nessun account e il poll immediato si salta.
  Il conteggio lo fa il server perché solo lui sa dei template di barre precedenti ancora vivi:
  dedurlo dagli `Intents` di quella barra salterebbe poll che avevano qualcosa. Sul DTO del cBot il
  campo è `int?` di proposito — un server che non lo conosce lo omette, e su un `int` varrebbe 0,
  cioè "non pollare mai", spegnendo il bot per tutto il run senza una riga di log. Il conteggio è
  volutamente più largo del claim (niente lucchetti, Titano, conversione account): sbagliare per
  eccesso costa un poll a vuoto, per difetto costa un segnale.
- **2026-08-06** — **Break-even e trailing escono subito quando non c'è nulla da proteggere.**
  Restano valutati a ogni tick — il prezzo può raggiungere e perdere la soglia dentro la stessa barra,
  ed è il motivo per cui quel lavoro non sta sul bar-close — ma senza posizioni aperte i due metodi
  ora tornano prima di scorrere `Positions` e di allocare. In un backtest tick-based i tick sono
  ordini di grandezza più delle barre, e la stragrande maggioranza cade a portafoglio vuoto. Nel bot
  distribuito il tick handler filtra anche per simbolo: il tick di EURUSD non può muovere lo stop di
  una posizione su NQ.
  Correlato: il pannello a chart del bot diretto si ridisegna solo quando cambia ciò che si legge
  (profit e drawdown a due decimali). `UpdateChartDisplay` è chiamato a ogni tick, e da quando il
  pannello include l'elenco delle strategie ricostruirne il testo ogni volta sarebbe stato più caro
  del pannello stesso.
- **2026-08-06** — **Lo spread al fill viene misurato e registrato.** Su un CFD long si entra
  sull'**Ask** e lo stop è valutato sul **Bid**: la perdita in denaro quando lo stop salta resta
  quella dichiarata dalla strategia, ma il Bid deve scendere solo di `(distanza stop − spread)` per
  farlo saltare. Stessa perdita per stop, più stop. Il costo non è quindi nel singolo trade — sui
  fill osservati lo slippage d'ingresso è sotto il punto, il 6% di una perdita — ma nel margine
  operativo che lo strumento si prende, e il numero che lo misura è **spread / distanza stop**: su
  `PTS_NQ_PCH_002_15` (stop 12,5 punti) uno spread di 2 vale il 16%, su `PTS_NQ_TFM_001_60`
  (stop 50) il 4%. Non era misurabile da nessuna parte del sistema: `ExternalExecutionReport` porta
  ora `SpreadAtFill`, il cBot lo legge al fill (non dopo: fra due minuti vale un altro numero), lo
  stampa per fill e ne fa un riepilogo per strategia a `OnStop`, e il controller lo logga sui soli
  report `Filled`. Non influenza nessuna decisione: serve a scegliere quali strategie ha senso far
  girare su quale strumento, perché uno stop stretto su uno spread largo non è un difetto del
  sistema ma una coppia strategia/strumento sbagliata.
  Corollario di analisi: gli ordini "doppi" allo stesso millisecondo e allo stesso prezzo nei log di
  cTrader **non sono doppioni**. Sono `PTS_NQ_PCH_001_15` e `PTS_NQ_PCH_002_15`, entrambe Donchian-100
  long-only su NQ 15m, che a gate passati producono lo stesso livello di canale. Il codice strategia
  è nella riga di cancellazione dell'ordine ed è l'unico modo per distinguerli: codici diversi =
  due strategie, codice uguale = doppione vero.
- **2026-08-11** — **`MaxConcurrentTrades` conta ora sull'insieme delle strategie, trasversale ai
  simboli**: dieci significa dieci, che stiano su un simbolo solo o su dieci diversi. Prima non era
  così, e su una sessione a simbolo singolo il valore configurato non entrava mai in gioco. Il
  sintomo, da un run reale (`FTMO-TRIAL-01`, 10/08/2026, `MaxConcurrentTrades = 10`): per undici ore
  un solo ordine per barra, sempre di `PTS_NQ_PCH_001_15`, con gli IntentId che saltano di due
  perché il template di `PTS_NQ_PCH_002_15` — stesso `US100.cash` — nasceva a ogni barra e non
  arrivava mai a mercato. A bloccarlo erano due vincoli, nessuno dei quali era il tetto: il **passo 1**
  di idempotenza (un solo intent pendente per account, e uno stop order vive l'intera barra) e il
  **lucchetto (account, simbolo)**, che si liberava alla chiusura e non al fill. Il tetto effettivo
  era 1, e la risposta al poll era `NoSignal`, quindi nemmeno la diagnostica lo diceva.
  Rimosso il lucchetto (`AccountActiveIntent`, `ActiveIntentKey`); il passo 1 ripropone ora le sole
  chiusure e lascia drenare gli ingressi finché c'è budget; a tetto pieno ripropone l'ingresso
  pendente, che è come si recupera un claim la cui risposta si è persa in rete. Resta invariata
  `AccountHasEntryInFlight` (stessa strategia, stesso simbolo, attiva in ogni profilo): è la guardia
  nata dall'incidente `PTS_NQ_PCH_002_15` del 14/10/2024, e non è concorrenza ma unicità del segnale.
- **2026-08-11** — Il budget di concorrenza si conta **deduplicato per IntentId**
  (`CountInFlightForAccount`). `openPositions + pendingOrders` contava due volte ogni ordine a
  mercato — lo stesso ordine è insieme un intent `Pending` sul server e un pending order nello
  snapshot del broker — e dimezzava di fatto il tetto configurato. Entrano nel conto anche i claim
  consegnati e non ancora comparsi sul broker: senza, un drenaggio veloce sfonderebbe il tetto per
  ritardo di propagazione invece che per una decisione. L'esposizione senza IntentId leggibile
  (label vecchie, fallback al conteggio server) non è deduplicabile e si somma: meglio contare una
  volta di troppo che consegnare un ingresso oltre il tetto.
- **2026-08-11** — **Cosa conti `MaxConcurrentTrades` è un parametro del piano**, non una convenzione
  del server: `ConcurrencyCountMode` vale `PositionsAndPendingOrders` (default, comportamento
  storico) o `PositionsOnly`. La risposta giusta dipende dal motore: chi entra a mercato non ha
  ordini in attesa da contare, chi entra in breakout ne ha uno per strategia per tutta la barra, e su
  un breakout non si sa a priori quale livello verrà toccato — bloccarne uno per «occupazione di
  slot» significa perdere il solo che sarebbe partito. In `PositionsOnly` il tetto si fa valere a
  valle: `PiootooDistributedExecutionBot.CancelPendingOrdersAtCap`, chiamato da `OnPositionOpened`,
  spegne gli ordini rimasti quando i fill raggiungono il tetto — un OCO, il primo che entra spegne
  gli altri. Il cBot resta disaccoppiato dal server: legge un parametro dal descriptor all'apertura,
  decide guardando la propria piattaforma e comunica solo il fatto compiuto, un `Cancelled` sullo
  stesso canale degli ordini scaduti. Il rischio residuo è dichiarato: fra il fill e la cancellazione
  due stop possono riempirsi insieme, ed è la ragione per cui la modalità è un parametro e non il
  default — su conti con regole di esposizione istantanea resta preferibile contare anche i pendenti.
- **2026-08-11** — Il dettaglio di un backtest ha un pulsante **Report HTML**, servito da
  `GET /api/Workspace/{id}/backtests/{cartella}/report`. Il report esisteva già — lo scrive
  `GenerateStrategyEquityHtmlReport` nella cartella del run — ma era raggiungibile solo per `jobId`,
  cioè finché il job era vivo in memoria: riaprendo un backtest archiviato non c'era modo di vederlo
  se non aprendo il file a mano. Il nome non è fisso (dipende dal prefisso del run), quindi il
  servizio cerca per estensione e sceglie il più recente invece di indovinarlo. Il pulsante non
  verifica che il file esista — costerebbe una chiamata HTTP a ogni apertura della schermata per un
  file che si apre di rado: l'assenza è un `404` con un messaggio che spiega quando è normale (run
  interrotti, run dell'engine esterno, che archiviano i trade ma non generano il report).
- **2026-08-12** — All'arrivo di ogni intent di ingresso il cBot distribuito registra la
  **fotografia del mercato**: Bid, Ask, spread, distanza fra il prezzo dell'intent e il lato su cui
  si entra (Ask per i long, Bid per gli short), età dell'intent rispetto a `ValidFromUtc` e coerenza
  del livello per i pending. Prima l'unica misura di esecuzione era lo spread al *fill*
  (`MeasureSpreadAtFill`), che arriva troppo tardi e solo per gli ordini che sono entrati: di un
  ingresso scartato per slippage restava la sola distanza in pips, con cui non si distingue un server
  che prezza su una barra vecchia da un mercato che si è mosso da uno spread anomalo. Le tre anomalie
  che la riga rende visibili sono il ritardo del giro poll/valutazione (`eta` che cresce), il prezzo
  del server fuori mercato (distanza sistematica sugli ordini a mercato) e il livello pending dalla
  parte sbagliata — uno Stop long sotto l'Ask si riempie subito invece di attendere il breakout, ed è
  un bug, non un evento di mercato, quindi esce su una riga a parte.
- **2026-08-12** — Il flag booleano "Log dettagliato" del cBot distribuito diventa il parametro a
  scala **`LivelloLog`** (`Minimo` / `Operativo` / `Diagnostico`), e **le righe dei segnali stanno
  fuori dalla scala**: intent ricevuto, scarti, anomalie, fill ed errori si stampano sempre, a
  qualunque livello. Sono proporzionali ai trade e non alle barre, e sono l'unica traccia del
  *perché* di un trade: spegnerle significa scoprire il problema senza avere più i dati per
  spiegarlo. Il livello governa il contorno — riscaldamento, finestre, poll — che invece è per barra.
  In backtest il livello effettivo è tagliato a `Minimo` con un avviso all'avvio: il buffer della
  piattaforma, quando si riempie, scarta le righe più *vecchie*, cioè proprio quelle dell'avvio dove
  stanno le cause.
- **2026-08-13** — *Visualizza → Stato server (sessioni)* (F9) apre una schermata diagnostica che
  riversa in una text area copiabile tutto ciò che il server espone sulle sessioni vive: riepilogo,
  `snapshot`, `groups`, `intents`, `signals`, `trades`, `rotation-log`. Nasce dal caso "il cBot non
  apre posizioni": il log del bot dice solo *nessun intent per la barra corrente*, che è la stessa
  riga sia quando nessuna strategia ha un setup sia quando le strategie non hanno abbastanza barre
  perché il riscaldamento non è stato accodato — due cause opposte, un solo sintomo. Il riepilogo
  mette in testa `LastBarTimeUtc`, che è il campo che le separa. Tre scelte non ovvie: il dump è
  **JSON grezzo non deserializzato** (`TradingSessionApiClient.GetRawJsonAsync`), perché una
  schermata che filtrasse i campi attraverso i contratti del client nasconderebbe proprio i campi
  nuovi che si sta cercando di leggere; ogni risorsa che fallisce stampa `!! errore:` e il dump
  prosegue, perché il buco è spesso l'informazione (`rotation-log` su sessione senza Titano);
  la voce sta nel **menu** e non in `NavigationRegistry` perché non è un'entità da elencare ma una
  lente sul processo, utile da qualunque schermata. Nessun polling: l'istantanea è quella del momento
  in cui si preme Aggiorna.
- **2026-08-13** — Server e `PiootooDistributedExecutionBot` condividono **un solo numero di
  versione** (2.2.0 alla decisione), da muovere sempre insieme: `Piootoo.Shared.PiootooVersion.Current`
  e la costante `BotVersion` del cBot. Sono i due lati dello stesso contratto HTTP ma non condividono
  una build — il cBot lo compila cTrader, che non referenzia le assembly della solution — quindi non
  esiste un punto unico leggibile a compile time e la sincronia è manuale, dichiarata nei commenti di
  entrambi i file. Gli altri cBot (`Direct` 1.4.0, `BarCycleTest` 1.0.0) restano su versioni proprie:
  non parlano col server, non c'è contratto comune di cui il numero sia la sintesi. Il server stampa
  la versione all'avvio due volte, su `Console` prima che l'host parta e su `ILogger` in
  `ApplicationStarted`, perché la prima riga si vede a schermo e la seconda è quella che finisce nel
  log strutturato allegato a un ticket. La console WinForms confronta la propria versione compilata
  con quella dichiarata da `GET /api/v1/version` e alza un alert se differiscono: il confronto non è
  tautologico anche se leggono la stessa costante, perché la console si ricompila dalla solution
  mentre il server gira spesso da `publish_run`, che può essere una build precedente — ed è il caso in
  cui i contratti divergono e i sintomi non parlano mai di versioni. Nessun blocco, né lato server né
  lato client: un server aggiornato che rifiutasse i bot in esecuzione farebbe più danni del
  disallineamento che segnala. L'alert compare una volta per versione, non a ogni gesto.
- **2026-08-13** — Il cBot distribuito passa a **2.3.0** e i tre avvisi diagnostici
  introdotti in 2.2.0 diventano **scarti**. Il caso che ha forzato la scelta è nei log di
  backtest del 26/06 e 07/07: `PTS_NQ_TFM_001_60` riceve uno Stop dal lato sbagliato del
  mercato, il bot stampa l'ATTENZIONE e piazza l'ordine lo stesso, che si riempie entro il
  millisecondo. Un avviso che non ferma nulla documenta il difetto invece di evitarlo.
  Ora si scartano: livello dal lato sbagliato, livello oltre 8x lo stop di distanza
  (lo stesso prezzo veniva riproposto per due giorni, fino a 323 pip dal mercato) e
  spread oltre il 20% dello stop. Tutti e tre disattivabili da parametro.
- **2026-08-13** — L'`eta` dell'intent era calcolata su `ValidFromUtc`, che è il bordo
  della barra **successiva** e quindi un istante futuro per costruzione: ne uscivano
  `3900s`, `-3599s`, `-188100s`, numeri che non misuravano né il ritardo né l'attesa.
  Sostituita da due grandezze separate: **ritardo** (da `CreatedAtUtc`, cioè dalla barra
  che ha prodotto il segnale — il campo era già sul filo, il bot non lo leggeva) e
  **attesa** (a `ValidFromUtc`). Nessuna modifica al contratto server.
- **2026-08-13** — Il trailing stop ha un **passo minimo** (10% della distanza di
  trailing) e un **intervallo minimo** fra modifiche (5s). Senza, il bot inseguiva il Bid
  tick per tick: nei log una singola posizione produce ~50 `ModifyPosition`, alcune da 0,1
  punti e più d'una nello stesso secondo. In backtest è rumore che satura il buffer, in
  live è rate limit del broker e reject della modifica successiva — che potrebbe essere
  quella utile.
- **2026-08-13** — Un ordine pending identico a quello già a mercato (verso, tipo,
  livello, volume, SL/TP) viene **esteso** invece che cancellato e ripiazzato, e il ritiro
  degli ordini scaduti si sposta **dopo** il poll della barra. Costo del compromesso: fra
  l'apertura della barra e il ritiro passa il tempo di push e poll, e in quella finestra
  un ordine non rinnovato è ancora a mercato. Accettato perché il livello è quello che la
  strategia aveva attivo fino a un istante prima; `ExtendIdenticalPendingOrders=false`
  ripristina il comportamento precedente.
- **2026-08-13** — Il cBot stampa il **consuntivo di ogni trade alla chiusura**
  (entry/exit, motivo, lordo/commissioni/swap/netto, netto per contratto Piootoo, MFE,
  MAE, durata, modifiche di trailing) e la **tabella per strategia a fine run** (trade,
  win rate, netto, media, profit factor, migliore/peggiore). Fino alla 2.2.0 il log
  raccontava per intero come nascevano gli ordini e taceva su come finivano: si poteva
  dire se il bot aveva eseguito, non se aveva guadagnato. Profit factor è `null` e non
  infinito quando non ci sono perdite: in tabella un numero enorme si legge come risultato
  eccezionale mentre significa solo campione piccolo.
- **2026-08-15** — Il cBot distribuito passa a **2.3.1**: il ritiro dei pending scaduti
  diventa **differito** di `PendingRetirementGraceSeconds` (default 5s) invece che
  immediato. L'estensione dei pending identici introdotta il 13/08 non ha mai lavorato, e
  i log lo dicono in negativo: la riga `non riemesso: ordine gia' a mercato` non compare
  **nemmeno una volta** in un mese di backtest, mentre la coppia cancella/ripiazza sullo
  stesso identico prezzo c'è a ogni barra — sul canale Donchian, che tiene il livello
  fermo finché non si muove, per giorni interi. Il motivo: il ritiro sta dopo il poll
  post-push, ma quel poll è saltato da `ShouldPollAfterPush()` quando il push dichiara la
  sessione senza nulla di reclamabile, e l'intent arriva col polling periodico qualche
  centinaio di ms dopo — con l'ordine da riconoscere già cancellato. La grazia gli dà il
  tempo di arrivare; chi non arriva lo ritira `SweepRetiredPendingOrders`, chiamato dal
  timer subito **dopo** `PollNextSignal()`. La scadenza non si rinnova: se alla barra
  successiva il ritiro era già programmato l'ordine muore lì, così la vita extra resta
  limitata a una barra qualunque sia la grazia — ed è la stessa riga che chiude il caso
  del backtest a barre, dove fra due barre l'orologio non avanza e lo sweep a tempo non
  scatterebbe mai. A 0 il comportamento torna quello della 2.3.0.
- **2026-08-15** — La **ripresa dell'intent bloccato** (`stalledEntry`, in
  `GetNextSignalForAccount` quando il budget di concorrenza è pieno) riverifica
  **scadenza** e **limite di ingressi per sessione** prima di riproporre l'intent. Quella
  strada non passa da `NarrowTemplates`, quindi li scavalcava entrambi: nei log del 06/08,
  08/08 e 11/08 si vedono ordini piazzati dopo che il limite di ingressi per sessione era
  già stato dichiarato raggiunto per gli altri template della stessa barra. Un intent che
  non supera i controlli passa a `Cancelled` e non viene solo saltato: lasciarlo `Pending`
  significa riproporlo a ogni claim per sempre e tenere occupati i lucchetti che lo
  riguardano. Cambia quali ordini arrivano a mercato, quindi cambia i backtest — nel
  campione di riferimento nessuno dei tre si era riempito e il P&L non si muove.
- **2026-08-15** — Nuovo profilo **`BacktestStaticFilter`**: strategie del masterfilter del
  workspace come in `BacktestSorgente`, lucchetti di concorrenza e distribuzione attivi come in
  `BacktestTitano`. È il termine di paragone che mancava. `BacktestSorgente` risponde a "quanto
  rende ogni strategia da sola", `BacktestTitano` a "quanto rende il sistema con il filtro
  dinamico": senza un run intermedio la differenza fra i due mescola due effetti — il merito della
  rotazione e quello del tetto di concorrenza — e non c'è modo di dire quanto pesa ciascuno.
  Condividendo i lucchetti con `BacktestTitano`, l'unica variabile fra i due run resta il filtro:
  statico (il masterfilter, fisso per tutto il run) contro dinamico (le rotazioni, che cambiano nel
  tempo). Il nome dice quello, non i lucchetti, che sono uguali nei due e quindi non
  distinguerebbero niente. Non richiede `TitanoBacktestFolder`: non ne legge nessuna.
- **2026-08-15** — I profili espliciti **dichiarano** i lucchetti e il piano non li contraddice
  più. Fino a oggi solo `BacktestSorgente` era blindato contro `plan.EnforceConcurrencyLimits`,
  mentre gli altri ci ricadevano: un piano con `EnforceConcurrencyLimits=false` rendeva
  `BacktestTitano` un run senza vincoli operativi che continuava a chiamarsi Titano, e la differenza
  si sarebbe vista solo confrontando due `trades.json` — esattamente lo scenario che la nota del
  sorgente dice di voler evitare. Ora `BacktestSorgente` forza `false`, `BacktestStaticFilter` e
  `BacktestTitano` forzano `true`, `DalPiano` resta governato dal piano. Il piano continua a
  decidere tutto il resto: workspace, sizing, strumenti, cartella del run Titano.
- **2026-08-15** — Rimossi **l'estensione dei pending identici** (2.3.0) e **l'attesa prima del
  ritiro** (2.3.1), con i due parametri `ExtendIdenticalPendingOrders` e
  `PendingRetirementGraceSeconds`. Il ritiro degli ordini scaduti torna a essere la prima cosa della
  barra: chiusura, push, richiesta dei segnali nuovi.

  Non è un ripensamento di stile, è l'unico ordine possibile. Il server non rilascia il template
  finché l'intent vecchio è `Pending` — lo tiene il lucchetto "l'account ha già un ingresso in corso
  per quella strategia su quel simbolo" — e l'intent vecchio resta `Pending` finché il cBot non ne
  riporta la cancellazione con `ReportExecution(Cancelled)`. Il cBot aspettava il segnale nuovo per
  riconoscere l'ordine da estendere, il server aspettava il report per consegnare il segnale:
  nessuno dei due poteva muoversi per primo. La prova è nei log, in negativo — la riga `non
  riemesso: ordine gia' a mercato` non compare **nemmeno una volta** in un mese di backtest 2.3.0 —
  e la 2.3.1 che provava a sbloccare il caso con una grazia di 5 secondi ha ottenuto solo di
  spostare il piazzamento da ~0,3s a ~8,3s dall'apertura della barra, lasciando intatta ogni coppia
  cancella/ripiazza. P&L identico al centesimo sui trade confrontabili: nessun danno e nessun
  beneficio, solo latenza.

  La coppia cancella/ripiazza per barra sullo stesso prezzo resta, ed è il costo accettato: due
  ordini al broker per barra finché il livello del canale non si muove. Toglierla davvero richiede
  di separare "riporto l'intent cancellato" da "cancello l'ordine dal broker" — il report libera il
  server, l'ordine fisico resta a mercato e viene esteso o modificato con `ModifyPendingOrder`
  quando il segnale arriva. È un cambio della semantica del reporting e va deciso a parte.
- **2026-08-18** — **I due livelli di filtro sugli ingressi sono separati, e solo uno vale sempre.**
  Livello 1, la *strategia*: `MaxEntriesPerSession` conta i fill per account ed è attivo in ogni
  profilo — è una regola del motore, non della piattaforma, e un campione che la ignorasse
  conterrebbe trade che la strategia non avrebbe mai fatto. Livello 2, la *piattaforma*:
  `MaxConcurrentTrades`, lo slot di gruppo e il lucchetto `AccountHasEntryInFlight` sono i vincoli
  operativi del setup del server e seguono `EnforceConcurrencyLimits`, quindi si spengono nel run
  sorgente, il cui scopo è eseguire *tutte* le strategie e misurarne il risultato complessivo.

  `AccountHasEntryInFlight` era finora incondizionato, classificato come "identità del segnale" e
  non come concorrenza. Il costo si legge in un backtest sorgente NQ del 17/03/2026: nove template
  di ingresso per barra, **un solo** claim servito, e per tutti gli altri `l'account ha già un
  ingresso in corso per quella strategia su quel simbolo`, con il conto dei template scartati che
  sale barra dopo barra (19, 27, …). Il campione sorgente usciva mutilato di otto strategie su nove.
- **2026-08-18** — **Gli intent di ingresso scaduti si annullano all'arrivo della barra**
  (`PurgeExpiredEntryIntents`), gemello di `PurgeExpiredTemplates` sugli intent già reclamati.
  Il template scaduto veniva buttato, il claim che ne era nato no: restava `Pending` per sempre, e
  con lui ogni lucchetto che legge "ingresso in volo". Un buy stop *next bar* mai toccato dal prezzo
  bloccava così la propria coppia (strategia, simbolo) per il resto del run, e l'unico punto che
  ripuliva un pendente scaduto era il ramo "tetto pieno" di `MaxConcurrentTrades` — che in un run
  sorgente, dove il tetto è spento, non viene mai eseguito.

  È anche ciò che rende sicuro spegnere il lucchetto di identità: due template della stessa
  strategia nati su barre diverse non possono più essere vivi insieme, cioè la condizione da cui
  nascevano i doppioni del 14/10/2024 (`PTS_NQ_PCH_002_15`, due stop riempiti allo stesso prezzo).
  Resta scoperto solo il motore che non dichiara `ExpiresAtUtc`: lì due ordini coesistono per
  costruzione, e nel run sorgente è il comportamento voluto.

  Lo sblocco è ora **del server e basta**: non dipende più dal fatto che il cBot riporti
  `ReportExecution(Cancelled)`, che è la metà mancante dello stallo descritto nella voce del
  15/08. Annullare non perde un fill reale — `ApplyReport` accetta il report anche su un intent
  annullato, quindi un ordine che il broker riempie comunque resta contato nel `trades.json` e nel
  tetto di livello 1. Nessuna riga di attività per la scadenza: è l'esito normale di un ordine
  *next bar*, e registrarla riempirebbe il buffer del monitor.
- **2026-08-18** — **In sessione una strategia riceve una finestra, non tutta la storia**, ed è la
  stessa del backtest locale (`RequiredCandles * 1.2`, `StrategyEvaluationService.EvaluationWindow`).
  `session.History` viene potato allo stesso numero più un margine (`TrimHistory`).

  Era `history.ToArray()` su una lista che cresce di una candela per barra: una copia intera per
  strategia per barra, con gli array oltre 85 KB sulla Large Object Heap, e i motori che percorrono
  la serie dall'inizio (`PriceChannelEngine.CalculateBarAdx`, `EasyLib.WindowUpTo`) che rifacevano
  ogni volta un lavoro più lungo. Il costo per barra cresceva con i giorni di run: è la ragione per
  cui un backtest via cBot rallentava fino quasi a fermarsi. Misura su @NQ 15m 2024 (23.922 barre,
  `PTS_NQ_PCH_001_15`): 36,3 s → 1,9 s, 12,2 GB → 0,87 GB allocati, **segnali identici**; su tutte
  le 13 strategie 15m @NQ del catalogo, primo semestre 2024, segnali identici a coppie.

  Era anche una divergenza dal backtest locale, che la finestra fissa l'ha sempre avuta: gli
  indicatori a smoothing ricorsivo dipendono da quante barre hanno visto, quindi la stessa strategia
  sullo stesso feed poteva dare valori diversi nei due percorsi. Resta un caso latente non toccato:
  `MovingAverageCrossoverEngine.MovingAverage` semina l'EMA su `data[0]`, cioè sul primo elemento
  della finestra ricevuta, invece che su una SMA dei primi `period` valori. Nessuna strategia del
  catalogo eredita oggi da quel motore; il giorno in cui succede va seminata su finestra fissa,
  altrimenti il valore torna a dipendere dalla profondità di storia.
- **2026-08-18** — **`Persist` di una sessione è un checkpoint, non una scrittura per barra.**
  Al massimo una scrittura ogni 2 s di orologio; l'fsync (`AtomicFileWriter` durabile) resta solo
  per le scritture forzate e per quelle che arrivano dopo più di 30 s dalla precedente — cioè per
  il live, dove fra due barre passano minuti, dove non cambia nulla. Il `rotation-log.json`, che è
  append-only, si riscrive solo se è cresciuto.

  Prima ogni push di barra riscriveva per intero `signals.json`, `trades.json` e
  `rotation-log.json` in JSON indentato e con fsync: file che crescono per tutto il run, riscritti
  a ogni barra, cioè costo quadratico e decine di GB scritti in un run lungo. Chi legge gli
  artefatti da fuori (`GetPersistedSignals`, `GetPersistedTrades`, `GetRotationLog`,
  `PromoteToBacktest`, il cambio di stato della sessione) forza prima il flush, quindi nessun
  lettore vede dati più vecchi della propria chiamata; il rischio residuo è perdere gli ultimi due
  secondi di scritture se il processo muore, che è la definizione di checkpoint.
- **2026-08-20** — **I checkpoint intermedi di signal, trade e rotation-log sono
  incrementali.** Prima ogni checkpoint riscriveva l'array JSON per intero:
  riproiettava, validava, deduplicava e serializzava *tutti* i record dall'inizio del
  run per salvarne due nuovi. Su un backtest lungo un anno sono ~24.000 intent e un
  `signals.json` da 40-80 MB, riscritti ogni due secondi — il costo per barra cresceva
  con le barre già fatte, quindi il run era quadratico e rallentava visibilmente verso
  la fine. Ora `TradingJsonStore` affianca a ogni array un journal `.jsonl`
  append-only (`AppendSignals`/`AppendTrades`/`AppendRotationLog`) e l'array viene
  materializzato solo alla lettura o alla scrittura autorevole di fine run, fondendo
  il journal con "ultima versione di ogni id, ordine preservato". Il journal è stato
  transitorio: ogni scrittura completa lo cancella, e chi legge i file senza passare
  dallo store (`TitanoRotationService`, che fa l'hash di `trades.json`) chiama prima
  `CompactAll()`. La completezza non dipende dall'intercettare i punti che mutano un
  intent — dimenticarne uno è il modo in cui una scrittura incrementale perde dati in
  silenzio: un intent viene riscritto a ogni checkpoint finché non è in uno stato
  definitivo (`Filled`/`Rejected`/`Cancelled`), quindi il passaggio a quello stato è
  sempre catturato dal checkpoint successivo.

- **2026-08-25** — **La barra successiva di un ordine `next bar` si calcola sul timeframe
  dichiarato dalla strategia, non sulla distanza fra le ultime due barre.**
  `EasyLib.EstimateNextBarUtc` deduceva il timeframe da `data[^1] - data[^2]`: esatto solo su
  una serie senza buchi. Sulla prima barra dopo una chiusura — fine settimana, festività,
  pausa di sessione — quella distanza *è* il buco: per l'oro alla riapertura della domenica
  vale circa 49 ore, e una strategia a 30 minuti emetteva un ordine con `ValidFromUtc` ed
  `ExpiresAtUtc` due giorni avanti. Le due conseguenze erano indipendenti e nessuna delle due
  si presentava come un errore: sul cBot l'ordine veniva piazzato con un'attesa di 174.600s
  (backtest GC dell'11/08/2013, solo un avviso stampato), e sul server il template restava
  vivo due giorni invece che una barra, quindi veniva riproposto a ogni claim tenendo occupato
  il lucchetto "ingresso in corso" della strategia. Ora `BuildEntry` e `BiasBarCountEngine`
  passano `TimeframeMinutes`, che il motore già dichiara; `GetTimeframeMinutes` resta per chi
  un timeframe dichiarato non ce l'ha, ma deduce dalla **minima distanza positiva** delle
  ultime 32 barre invece che dall'ultima coppia — un buco allunga una distanza, non ne
  accorcia nessuna. Rete sotto lato cBot: `RejectUnsoundIntent` scarta un intent la cui
  attivazione cade oltre la barra corrente **in avanti**, insieme al livello dal lato
  sbagliato e come quello anche nel profilo `BacktestSorgente` — non è un filtro
  discrezionale, è un difetto di sistema. Solo in avanti: un'attesa negativa è il caso
  normale, il pending è già attivo quando l'intent arriva.


## 2026-08-25 — Scala di prezzo dell'account e rischio in denaro fino al client (3.6.0)

Due modifiche indipendenti sullo stesso confine, quello fra il segnale e l'account che lo
esegue.

**`PriceScale` sulla riga di conversione.** La tabella di conversione dell'account toccava la
sola quantità. Le distanze di uscita — stop, target, trailing, break even — arrivavano al
client identiche a come la strategia le aveva dichiarate, ed è giusto così quasi sempre: i
punti sono la grandezza invariante del contratto, 20 punti restano 20 punti su future, mini,
micro e CFD dello stesso sottostante. Non lo sono quando il broker quota lo stesso sottostante
in un'altra unità, che è un cambio di unità di misura del **prezzo** e non del contratto. Da
qui il fattore separato: `ContractMultiplier` scala quanto si compra, `PriceScale` in che unità
è quotato, e tenerli distinti permette di cambiarne uno senza toccare l'altro. Si applica in
`CloneForClaim`, cioè quando l'account è noto, insieme alla conversione della quantità e per la
stessa ragione. **Il default è 1 ovunque**, quindi nessun run esistente cambia comportamento; una
scala assente o non positiva si rilegge come 1, come già faceva il moltiplicatore — azzerare gli
stop in silenzio è il modo peggiore in cui un errore di configurazione possa manifestarsi.

**Il denaro sopravvive fino al client.** Le strategie portate da EasyLanguage dichiarano il
rischio in denaro per contratto future di riferimento; il server lo divide **una volta sola**
per il valore punto di quel contratto e spedisce punti. Il denaro moriva lì. Ora viaggia
sull'intent accanto ai punti, insieme al divisore usato (`ReferenceDollarsPerPoint`), e non
viene scalato da niente: il rischio dichiarato è del contratto di riferimento e resta quello
qualunque strumento lo esegua. Non è da eseguire — i punti lo sono — serve a permettere al cBot
di rifare il conto nell'altro verso, con il valore pip del suo strumento e il volume che sta
davvero inviando, e di stampare il rapporto fra i due (`intent/rischio`). È la verifica che
prima non era possibile fare da nessuna parte: un conto che eseguiva un decimo del contratto di
riferimento rischiava un decimo del denaro dichiarato, e né il server né il bot avevano in mano
i numeri per accorgersene.

Perché non percentuali sul prezzo, che pure erano state chieste: le percentuali sono invarianti
per riscalamento moltiplicativo, i punti lo sono per traslazione additiva e per cambio di taglia
del contratto. Fra una serie continua back-adjusted e lo spot la differenza è **additiva** — per
GC nel 2012 vale ~580 punti — quindi la stessa distanza percentuale vale distanze diverse sulle
due serie, mentre in punti resta la stessa. La percentuale risolverebbe il problema che non
abbiamo e romperebbe quello che oggi funziona.

- **2026-08-25** — **La versione del progetto passa a 3.7.0** e i punti da tenere allineati
  diventano tre invece di due: `PiootooVersion.Current` (server e console WinForms, che compila
  contro la stessa assembly), `BotVersion` del cBot distribuito, e ora `VersionPrefix` in
  `Directory.Build.props`. Quest'ultimo non esisteva: senza nessun `<Version>` dichiarato MSBuild
  stampava il proprio default `1.0.0.0` su ogni assembly di ogni build, quindi un binario non
  diceva a quale release appartenesse né era confrontabile con il numero che il server dichiara a
  runtime. `VersioneDelProgettoTests` verifica che i tre coincidano e fallisce se se ne muove uno
  solo — per il cBot leggendo il sorgente, che è il massimo possibile visto che lo compila cTrader.
  Voce aperta: `PiootooDirectExecutionBot` ha un client HTTP verso il server (parametro
  `API Base Url`) ma porta una versione propria (1.4.0), mentre l'XMLdoc di `PiootooVersion`
  affermava che non parla HTTP. O segue anche lui il numero del progetto, o va detto perché no.

- **2026-08-25** — **Il datafeed locale passa a un file per (simbolo, timeframe)**,
  `datafeed/@NQ_15.json`, generato dai CSV minute del vendor da
  `datafeed-future/aggregate_flat_feed.py`. Sostituisce la gerarchia
  `datafeed/{tf}/{symbol}/{symbol}-{yyyyMMdd}.json`, che per il solo @NQ erano 30.000 file aperti
  e deserializzati uno a uno a ogni caricamento di datasource. Il layout piatto non è solo più
  ordinato: il backtest pre-carica un datasource per coppia unica, quindi il costo passa da
  migliaia di `File.ReadAllText` a uno.

  Nel farlo sono emerse due proprietà del sorgente che nessuno aveva accertato, e che il vecchio
  aggregatore sbagliava entrambe:

  **Il timestamp del CSV è la fine del minuto, non l'inizio.** Su 200.000 righe di @NQ ci sono
  201 righe alle `00:01` e 4 alle `00:00`: la riapertura Globex è la mezzanotte europea e la
  prima barra della sessione — quella che copre `00:00`→`00:01` — è stampata `00:01`. Con
  timestamp di inizio le due frequenze sarebbero simili. Il bucket giusto è quindi
  `floor(t - 1 minuto)`; `aggregate_nq_ascii.py` usava `floor(t)`, e il feed gerarchico che ha
  prodotto è sfasato di un minuto per barra (sul primo bucket 15m del 03/01/2006: 574 di volume
  contro 554). È il motivo per cui il file piatto ha la precedenza invece di affiancarsi.

  **Gli istanti scritti nel feed ora sono UTC vero.** Il CSV è in ora dell'Europa continentale —
  già stabilito in `domini/orari-di-sessione-e-fusi.md` — ma finora veniva copiato verbatim con
  l'etichetta `Z` e raddrizzato a valle da `FeedClockRegistry`. Ora l'ora legale europea si
  risolve una volta sola, in conversione, e `feed-clocks.json` dichiara `UTC`: la sorgente della
  bugia sparisce invece di essere compensata a ogni lettura. I bucket restano però allineati
  sulla **mezzanotte locale del feed**, non su quella UTC — per 15/30/60 minuti è indifferente,
  per 4h e giornaliero no: è l'allineamento dei run di ricerca da cui le PTS sono portate, e per
  il giornaliero è l'unico che non spezza la sessione, perché su questo feed la pausa di
  manutenzione CME cade a cavallo della mezzanotte europea.

  Convertiti gli 11 simboli con strategie a catalogo più @EC, e solo i timeframe che le strategie
  dichiarano. Nel repository resta il ramo di lettura gerarchico come fallback; è sparito invece
  quello dei file flat Yahoo (`GCF_15.json`), che nessuno produceva più da quando
  `datafeed-downloader/` non c'è.

- **2026-08-25** — **`Microsoft.Web.WebView2` non è più `PrivateAssets="all"`.** Marcare privato
  l'intero pacchetto rendeva privato anche il suo asset **nativo**, `WebView2Loader.dll`, e gli
  asset privati vengono esclusi dal `deps.json` del *publish*. Il file finiva comunque in
  `runtimes/win-x64/native/`, ma l'host .NET non aggiungeva mai quella cartella ai percorsi di
  ricerca dei nativi: il P/Invoke falliva con `Unable to load DLL 'WebView2Loader.dll'`
  (0x8007007E) e il tab *Report* del backtest restava vuoto nella console pubblicata. Il build
  normale non lo mostrava — lì il `deps.json` ha ancora `runtimeTargets` con tutti e tre i RID —
  quindi il bug si vedeva solo sull'app pubblicata, che è quella che si usa davvero.

  `compile` resta privato: serve a non far arrivare a `Piootoo.Strategies.Tests` (che referenzia
  la console) il wrapper WPF del pacchetto, cioè l'MSB3277 di cui parla il target
  `RimuoviWebView2Wpf`. Privati anche `build`, `buildtransitive`, `analyzers` e `contentfiles`;
  `runtime` e `native` no.

  La diagnosi è stata confronto fra i tre `deps.json` prodotti (Debug: `runtimeTargets`; build
  RID-specifico: `native`; publish: **oggetto vuoto**), e la causa è stata dimostrata riproducendo
  lo stesso identico messaggio d'errore in un progetto probe pubblicato nei due modi.

- **2026-08-26** — **Gli ordini pending si cancellano per strategia *e lato*, non per sola
  strategia.** `CancelStrategyPendingOrders` nel cBot cancellava tutti i pending con il prefisso
  di label della strategia prima di piazzare il nuovo ordine, sul presupposto — vero solo per una
  strategia a un lato solo — che "il motore riemette lo stesso ordine a ogni barra col livello
  ricalcolato, quindi non è un secondo ordine". Le strategie non simmetriche (`TfUnmirroredEngine`,
  `PTS_GC_TFU_001_30` in testa) emettono invece sulla **stessa barra** un bracket a due gambe —
  stop buy su `H_d1`, stop sell su `L_d1` — che sono due motivi d'ingresso indipendenti: la seconda
  gamba uccideva la prima, e il bracket non poteva mai stare a mercato intero.

  Misurato sul confronto Feb–Mag 2014 GC in `piootoo-repository/compare/`: sui 1501 ordini
  piazzati dal bot, TFU Buy e TFU Sell non risultano pendenti insieme **nemmeno una volta**, mentre
  il backtest locale le tiene entrambe su cinque sessioni (14/03, 21/03, 24/03, 21/04, 30/05, ~291
  segnali). Sulle sessioni accoppiate i livelli dei due lati coincidono entro ~1 punto — l'offset
  @GC/XAUUSD — quindi il segnale nasceva giusto e la perdita era tutta nel cBot.

  Il lato entra anche in `PendingOrderMark` (la label non lo porta) e nella potatura di
  `_lastOpenIntentByLabel`: potarla per sola strategia toglieva l'intent anche alla gamba opposta,
  e il suo fill sarebbe arrivato a `OnPositionOpened` senza intent associato, cioè senza report al
  server.

- **2026-08-26** — **L'OCO fra le gambe di un bracket lo simula il bot.** Conseguenza diretta della
  voce precedente: con entrambe le gambe finalmente a mercato, un prezzo che attraversa tutti e due i
  livelli lascerebbe la strategia long e short insieme. cTrader non lega fra loro ordini piazzati
  separatamente — non esiste un OCO nativo da chiedere al broker — quindi il legame lo tiene il cBot:
  `EnforceBracketOco`, chiamata da `OnPositionOpened`, ritira la gamba opposta della stessa strategia
  nel solo istante in cui l'esito della prima è noto, e ne riporta al server l'annullamento (senza
  quel report l'intent resterebbe `Pending`, assegnato al conto e riproposto a ogni poll).

  Il ritiro va **prima** della risoluzione dell'intent locale: se l'intent è perduto — riavvio del bot
  con un pending già a mercato — il report del fill salta, ma la gamba opposta va tolta lo stesso,
  perché è a mercato e si riempirebbe.

  Sulle barre successive l'OCO era già di fatto imposto da `alreadyOpenOnStrategy`, che annulla ogni
  intent della strategia mentre una sua posizione è aperta; quello che mancava era lo scioglimento del
  bracket già piazzato.

  **Il caso che resta scoperto**, e che per questo viene riconosciuto e stampato invece di passare in
  silenzio: una barra che spazza entrambi i livelli **prima** che l'evento della prima apertura sia
  servito riempie tutte e due le gambe comunque. Quando il codice gira, il secondo fill è già avvenuto.
  `EnforceBracketOco` lo rileva, stampa `OCO CEDUTO` e scrive l'evento `oco/ceduto` nel log JSON: un
  OCO che ha ceduto in silenzio sarebbe indistinguibile da uno che ha tenuto, ed è esattamente il tipo
  di silenzio che ha lasciato passare inosservata la divergenza di TFU per mesi.

- **2026-08-26** — **Il poll a timer non gira più in backtest, se non c'è un motivo locale.**
  `OnTimer` reclamava un segnale a ogni battito (`PollingSeconds`, default 2). In live è una
  chiamata ogni 2 secondi di tempo reale; in backtest il timer batte sull'orologio **simulato**,
  quindi su un run di un anno scatta nell'ordine del milione di volte contro le ~35.000 push di
  barre. È il grosso del traffico HTTP di un backtest esterno, e quasi tutto per sentirsi dire
  "nessun intent".

  La guardia (`ShouldPollOnTimer`) non è un'euristica di risparmio: in backtest l'esito di quel
  claim è **deducibile**. I template nascono solo dalla valutazione di una barra; l'orologio del
  server è l'ultima barra valutata e `PurgeExpiredTemplates` gira sulla stessa valutazione, quindi
  fra due push nessun template nasce e nessuno scade; e in backtest il bot è l'unico attore della
  sessione. L'unica cosa che può far passare un claim da "no" a "sì" senza barre nuove è lo stato
  del broker che il claim stesso trasporta — un tetto di concorrenza che si libera, un ingresso
  della stessa strategia che smette di essere "in volo" — cioè un evento locale:
  `Positions.Closed` e ogni execution report alzano `_claimRetryPending`, e solo allora si polla.
  Il numero di claim passa da "quanti secondi simulati dura il run" a "quante barre e quante
  esecuzioni ha il run".

  **In live non cambia nulla**, di proposito: lì il timer è l'unico canale che scopre i template
  nati dalla push del bot di un altro account, ed è la prima chiamata che si accorge del server
  tornato su (`TryReopenSession` vive nel percorso del poll). Su uno stream a 60 minuti toglierlo
  vorrebbe dire riagganciarsi un'ora dopo.

  Il timer **resta acceso anche in backtest**: `EnforceWeekEndFlat` e `CloseExpiredPositions` sono
  lavoro locale senza rete, e girano sulla stessa cadenza di prima. Spostarli su barra o su tick
  avrebbe cambiato quando scattano — il flat di venerdì con soli ordini pendenti e nessuna
  posizione aperta non ha un tick che lo svegli, perché `OnSymbolTick` esce subito se non c'è
  niente da proteggere — ed è un cambio di comportamento che questa voce non vuole fare.

  Verifica: `OnStop` stampa push e claim del run, e il parametro *Poll a timer anche in backtest*
  rimette il comportamento della 3.8.0 — così l'A/B si fa con lo stesso binario, stesso run due
  volte, un solo parametro diverso. Devono risultare lo **stesso numero di push** e lo **stesso
  `trades.json`**, con i soli claim a crollare; se cambiano anche le push, non è la guardia ad aver
  agito, è il run a essere diverso. Lo stesso parametro è la via di ritorno se un giorno saltasse
  fuori un caso non previsto, senza ricompilare.

- **2026-08-26** — **Backtest interno e conto vero cTrader facevano trade diversi.** Rilasciato
  come **3.10.0** (cBot diretto 1.5.0): la correzione tocca l'engine di esecuzione, i contratti di
  sessione e di piano e tutti e due i cBot, e cambia quali trade un backtest produce — i run
  precedenti non sono confrontabili con i successivi. Sul
  confronto in `piootoo-repository/compare/compare-0002` (GC/XAUUSD, 2022–2023, quattro
  strategie) i due sistemi coincidevano solo sul **40%** dei trade: 97 dei 166 interni non
  esistevano nell'esterno, 107 dei 176 esterni non esistevano nell'interno, e il risultato
  era +507 punti contro −5,5. La differenza di feed — future back-adjusted contro CFD spot,
  basis da 377 a 325 punti in due anni — **non c'entrava**: sui trade che i due sistemi
  fanno davvero uguali lo scarto mediano fra i livelli di trigger è 0,53 punti. Erano tre
  difetti di esecuzione, corretti insieme.

  1. **Gli ordini di una strategia a 60 minuti vivevano mezz'ora.** L'orologio del backtest
     è il timeframe minimo del portafoglio (30, per via di una strategia a 30) e
     `currentBars` tiene una sola barra per simbolo, la più fitta; `ExpiresAtUtc` veniva
     confrontato con il tick corrente, quindi il pending moriva dopo il primo tick da 30 e
     metà del suo range non veniva mai guardata. Il segnale dichiara ora `TimeframeMinutes`
     e la scadenza è `ExpiresAtUtc + TimeframeMinutes`. Misura: le due strategie a limite a
     60 minuti avevano 29 fill interni contro 69 del broker, e il 54% dei fill del broker
     cade nella seconda mezz'ora dell'ora. Dettagli in
     [`domini/orologio-barre-e-fill.md`](domini/orologio-barre-e-fill.md).

  2. **Il backtest riempiva livelli che il cBot non piazza.** Uno stop buy già sotto il
     prezzo si riempiva a `Math.Max(bar.Open, livello)`, mentre il bot lo scarta da sempre
     (`RejectWrongSideLevels`): nel log erano **644 scarti su 53 giornate**, tutti di una
     sola strategia, e corrispondevano esattamente ai 53 trade TFU che l'interno aveva e
     l'esterno no — 270 punti sui 507 dell'intero run. `PiootooTradingService` ha ora lo
     stesso filtro, acceso di default, con verifica una volta sola alla nascita dell'ordine
     e disuguaglianza stretta sull'apertura. Spegnerlo (`RejectWrongSideLevels` nella
     `BacktestingRequest`) riporta la semantica TradeStation e serve solo alla misura di
     fedeltà del porting.

  3. **L'orario di flat del fine settimana è del server.** Il cBot flatta venerdì alle
     20:45 UTC; il backtest non aveva alcun orario e chiudeva sull'ultimo slot del proprio
     orologio sintetico prima di sabato — **venerdì 23:30**, tutto l'anno. Due ore e tre
     quarti di venerdì in più su 74 trade su 166. Il numero vive ora in `WeekEndFlatPolicy`
     (`TradingConventions`), sta sul `TradingPlan`, scende nella sessione, viene pubblicato
     nel descriptor e i due cBot lo usano al posto del proprio parametro, che resta come
     rete per server irraggiungibile o versione vecchia. La regola di sicurezza resta nel
     bot — deve tenere anche a server muto — ma il *numero* no: una regola che uno solo dei
     due motori conosce garantisce divergenza.

  Restano fuori, di proposito, le differenze di contabilità che l'engine interno non
  pretende di riprodurre: valuta di conto (l'interno lavora a 100 USD per punto, il conto è
  in euro e il valore punto oscilla fra 87 e 90 con l'EURUSD), commissione (4 contro 9,65
  medi) e swap. Valgono circa il 13% dello scarto e non spiegano il segno. Resta anche lo
  slippage sugli stop, che il conto vero paga e il backtest no: −20,01 punti medi contro
  −18,42, circa 1,5 punti per stop.

- **2026-08-27** — **Il flat di fine settimana ora si dichiara nel summary del backtest.** Non è una
  regola della strategia e non lo diventa: né `CloseAllPositionsAtWeekEnd` (una checkbox della
  console) né `FlatAtWeekEnd` del cBot guardano cosa la strategia ha dichiarato con `IntradayOnly` o
  `MaxDaysInTrade`. È corretto che sia così — è una regola di rischio del **conto**, e una regola di
  rischio con eccezioni per strategia non protegge più niente — ma finora si applicava in silenzio
  anche alle strategie che dichiarano di restare aperte oltre la sessione, e l'uscita che ne
  risultava aveva un prezzo e un motivo perfettamente plausibili.

  Quanto costa: confrontando `PTS_GC_TFU_001_30` con la propria lista di trade della ricerca
  (`run-engine/run-02-agosto/consegna/trades/fam01_TF_U.csv`, che combacia con il docstring — 150
  trade OOS, $176.500, PF 2,04), sui 54 trade con lo stesso ingresso lo scarto è di 495 punti, e
  **479 vengono dalle 25 uscite di fine settimana**: il 97%. Stop e target sono portati bene, 22
  stop e 1 target coincidono entro qualche decimo. Sul run 2022-2023 il flat chiude il 62% dei trade
  di `PTS_GC_PCH_001_60` (multiday, `MaxBars = 0`) e il 43% di `PTS_GC_TFU_001_30` (multiday, 460
  barre); sulle due RHL, che sono `IntradayOnly` e chiudono da sé a fine sessione, il 12% e il 6%.

  Il `backtest-summary.json` porta ora `weekEndFlatFromUtcHhmm` (null a flat spento) e una diagnosi
  `[fine settimana]` che elenca le strategie a cui il flat ha chiuso almeno il 25% dei trade, con i
  numeri. È **misurata sui trade**, non dedotta da un flag dichiarato: `IntradayOnly` non è
  attendibile allo scopo, perché i motori che non lo espongono — `BiasWeeklyEngine` tiene la
  posizione dal venerdì al lunedì — risulterebbero intraday. La soglia esiste per non trasformare la
  riga in rumore.

  Resta aperta la decisione operativa, che il codice non può prendere: se il conto deve essere
  piatto nel fine settimana, TFU e PCH rendono una frazione di quello per cui sono state validate.

- **2026-08-27** — **Overnight e overweek hanno una gerarchia dichiarata: decide prima il piano.**
  Prima la catena era spezzata in tre punti che non si parlavano. Il livello strategia esisteva ma
  era invisibile (`IntradayOnly`, un campo `protected` ripetuto in sei motori, leggibile solo
  aprendo il `.cs`) e non uniforme (cinque motori applicavano `IntradayOnly && TimeframeMinutes <
  1440`, RBB no). Il livello piano aveva solo mezzo asse: `WeekEndFlat` portava *l'orario* del
  taglio del venerdì, ma l'*interruttore* viveva altrove e in due copie —
  `CloseAllPositionsAtWeekEnd` sulla richiesta di backtest e `FlatAtWeekEnd`, parametro locale del
  cBot. L'overnight a livello di piano non esisteva affatto: per una prop che impone il flat di
  sessione bisognava modificare le classi delle strategie.

  Ora: `StrategyHolding` dice cosa la strategia *vuole* (`Overnight`, `Overweek`), esposto nel
  catalogo, nella griglia, nel masterfilter e nel descriptor; `AccountHoldingPolicy` sul piano dice
  cosa il conto *permette* e a che ora taglia; `HoldingResolver` compone i due in un punto solo,
  chiamato sia dal backtest sia dalla sessione. La regola è `tiene = pianoPermette &&
  strategiaVuole`: il piano taglia a prescindere, ma non può forzare un overnight su una strategia
  intraday, e `AllowOverweek` senza `AllowOvernight` viene rifiutato invece che risolto in silenzio.

  Il default (`AllowOvernight = true`, `AllowOverweek = false`) riproduce esattamente il
  comportamento precedente: nessun piano già scritto cambia comportamento, e il vecchio
  `WeekEndFlat` di primo livello viene travasato alla lettura.

  Rimossi da entrambi gli execution bot i parametri `FlatAtWeekEnd`, `WeekEndFlatFromUtc` e
  `WeekEndFlatUntilUtc`: finché sono vissuti lì, il bot poteva contraddire il piano che diceva di
  eseguire, e la differenza non compariva da nessuna parte. Il flat resta una regola di sicurezza
  locale — la policy ricevuta all'apertura vale anche a server muto — ma il permesso non è più suo.

  `IntradayOnly` è ora dichiarato una volta sola in `EasyEngineBase`. La regola daily
  (`intraday_only` non si applica su D1) **resta**, perché è parità col motore di ricerca e non una
  deduzione dal timeframe: è scritta una volta in `SessionExitFromIntradayOnly`, vale ora anche per
  RBB che ne era esente, ed è inerte sul catalogo attuale — un test di conformità impedisce che
  compaia una daily che vi si affida senza saperlo.

  Il summary di backtest porta `holding` al posto di `weekEndFlatFromUtcHhmm`, e una diagnosi
  `[fine sessione]` gemella di `[fine settimana]`. L'uscita imposta dal conto è
  `TradeExitReason.SessionFlat`, distinta da `TimeExit`: sommarle renderebbe invisibile la
  differenza fra ciò che la strategia misura e ciò che il conto le concede. Dettagli in
  `domini/overnight-e-overweek.md`.

- **2026-08-28** — **Due convenzioni di uscita, dal confronto `compare-0005`.** Rilasciate come
  **3.11.0**: cambiano il prezzo e l'istante a cui l'engine chiude, non quali ingressi fa, e i run
  precedenti non sono confrontabili con i successivi.

  1. **Il trailing segue il picco a scatti** (`TrailingMinStepFraction`, 0,10). Il cBot ha sempre
     avuto un passo minimo — muove lo stop nativo solo se il miglioramento vale almeno un decimo
     della distanza di trailing, più un intervallo minimo fra due modifiche, altrimenti insegue il
     Bid tick per tick e il broker gli rifiuta anche le modifiche utili. L'engine invece ricalcolava
     il livello dal picco a ogni barra e si faceva togliere dal primo ritracciamento. Misura: le
     uscite in trailing valevano **2.605 $/trade sul conto vero contro 522 nel backtest**, su 85 e
     126 osservazioni. Il modello era pessimista, e il pezzo da correggere era il proprio. Nell'engine
     il passo è applicato al **picco** invece che allo stop: stesso risultato — il livello si muove a
     scatti di un decimo — senza dover ricordare l'ultimo livello inviato.

  2. **Lo stop protettivo si riempie all'apertura sulla barra in gap.** L'ingresso applicava già la
     convenzione (`Math.Max(bar.Open, livello)`), l'uscita no: chiudeva al livello, dove in quel
     momento non c'era nessuno. Vale per lo **stop originale** e basta: un trailing o un break-even
     possono essere nati dall'estremo della barra in corso, e confrontarli con la propria apertura li
     farebbe riempire a un prezzo che precede il livello stesso. Non è un modello di slippage — lo
     spike dentro la barra resta invisibile alle sole OHLC, ed è lì che sta la maggior parte delle
     perdite oltre lo stop del confronto.

  Il log di avvio del job porta ora anche `engineVersion`: un backtest lanciato contro un server non
  ricompilato era indistinguibile da uno aggiornato, e il confronto misurava un motore che non era
  più quello del sorgente.

- **2026-08-29** — **Il passo minimo del trailing è un parametro del run, non una costante del
  binario.** Nasceva come proprietà del solo engine, con l'XMLdoc che diceva "a 0 si torna al
  comportamento precedente, serve a misurare": ma senza un campo sulla `BacktestingRequest` quel
  ritorno richiedeva di ricompilare, e nessun artefatto diceva con quale convenzione un run fosse
  nato. Ora `TrailingMinStepFraction` sta sulla richiesta accanto a `RejectWrongSideLevels`, è
  rifiutato fuori da `[0, 1]` — lo stesso intervallo che il cBot dichiara sul parametro omonimo — e
  finisce nel log di avvio del job. Due cartelle di backtest con trailing diverso, altrimenti, sono
  indistinguibili, ed è esattamente il tipo di silenzio che ha reso lungo il confronto con l'esterno.

  Le due convenzioni del 28/08 hanno ora le proprie regressioni
  (`TrailingStepAndGapFillTests`): il passo misurato sulle stesse barre con un solo numero diverso
  (uscita a 95 col passo, a 95,5 senza), il fill sul gap sui due lati, e il caso di controllo che
  il trailing **non** si riempie all'apertura. Versione **3.12.0**.
- **2026-08-30** — Il datafeed raccolto da un broker vive in
  `piootoo-repository/datafeed-external/{BROKER}/`, **separato** da `datafeed/` e con una
  cartella per broker. Le barre dello stesso simbolo prese da due broker non sono la stessa
  serie — cambiano orario di sessione, bucket e volume (che e' conteggio tick) — e mescolarle
  darebbe un feed che non corrisponde a nessuno dei due conti, senza che niente lo dica.
  Ogni cartella ha il proprio `feed-clocks.json` generato, quindi si puo' passare direttamente
  a `DataSourceRepository`. Vedi `domini/raccolta-datafeed-esterno.md`.
- **2026-08-30** — La raccolta avviene **a blocchi**, mai in un'unica chiamata: l'unita' e' il
  blocco (default cinque giorni, max 2000 barre), idempotente perche' la chiave e' l'istante di
  apertura della barra. Un `POST` unico da centomila barre andrebbe in timeout e, morendo a
  meta', non lascerebbe niente di riutilizzabile; il giro di `LoadMoreHistory` fatto tutto in
  `OnStart` bloccherebbe il thread dell'algoritmo per minuti. I blocchi si accodano a un journal
  `.jsonl` e il file piatto si materializza alla compattazione, per lo stesso motivo per cui i
  checkpoint del backtest non riscrivono l'artefatto intero.
- **2026-08-30** — I buchi del feed esterno si **dichiarano**, non si riempiono, e il passo con
  cui si decide cos'e' un buco e' dedotto dai dati (`dominantStepMinutes`), non assunto dal
  timeframe: un giornaliero di broker apre alle 22:00 o alle 23:00 UTC, e assumere
  l'allineamento all'epoch farebbe comparire un buco per ogni giornata. Ogni buco porta
  `spansWeekend`, perche' il mercato chiuso non e' storia mancante.
- **2026-08-30** — Il cBot raccoglitore prende gli strumenti da un **codice piano**, e li prende
  dal **masterfilter**, non dalla rotazione Titano corrente: il datafeed di uno strumento serve
  anche mentre le sue strategie sono spente, altrimenti alla riaccensione mancherebbe la storia
  esattamente lunga quanto la pausa. `GET api/datafeed-external/plan-instruments` e' una lettura
  pura e non apre sessioni — a differenza del cBot distribuito, che gli strumenti li ricava dal
  descriptor di sessione: un raccoglitore non deve avere effetti sull'operativita'. Restituisce
  anche l'`accountSymbol` dalla tabella di conversione del conto, cosi' il bot non mappa niente a
  mano e non esiste una seconda lista di simboli che possa divergere dal masterfilter.
- **2026-08-30** — `PiootooDatafeedSyncBot` porta una versione PROPRIA (1.x) e non segue
  `PiootooVersion`, che e' vincolata dai test a coincidere con il bot distribuito e
  `Directory.Build.props`. Quel numero e' la sintesi del contratto di esecuzione, che il
  raccoglitore non tocca: legarlo li' significherebbe far comparire un finto disallineamento nel
  log di un bot non cambiato a ogni release del server, o ri-deployarlo per niente.

- **2026-08-31** — **Un ingresso reclamato che scade senza report non blocca piu' la propria coppia,
  e la spazzata sta sul percorso che realtime e backtest condividono.** Rilasciato come **3.14.0**.

  `PurgeExpiredEntryIntents` esisteva nel progetto — `domini/distribuzione-multi-account.md` §4.3 la
  descrive e la tabella dei lucchetti la cita — ma nel codice non c'era piu'. Senza di lei un
  ingresso gia' reclamato che superava la propria finestra **senza mai ricevere un execution report**
  restava `Pending` per sempre, e con lui restavano chiusi i due lucchetti che lo riguardano:
  `AccountHasEntryInFlight` sul ramo pendente e lo slot di gruppo. Misura su una sonda a sei barre
  con un segnale per barra: **un intent in tutto il run**. A lucchetti spenti il claim rispondeva
  «l'account ha gia' un ingresso in corso per quella strategia su quel simbolo e lato»; a lucchetti
  accesi l'intent veniva annullato dal ramo "tetto pieno" ma lo slot no, e il blocco restava con
  l'altro messaggio. In entrambi i casi quella coppia era morta fino a fine sessione.

  Serve il caso in cui il report non arriva: risposta al claim persa, bot riavviato con un pending che
  non traccia piu', bot caduto. Nel funzionamento normale il cBot riporta le cancellazioni e l'intent
  si assesta da se' — ed e' per questo che il difetto non si vedeva nei run.

  **Dove sta la spazzata e' parte della correzione, non un dettaglio.** L'unico codice che ripuliva un
  pendente scaduto viveva dentro il ramo `inFlight >= maxConcurrentTrades` di
  `GetNextSignalForAccount`, cioe' sul percorso del **claim**. Il claim ha cadenze diverse nei due
  mondi — in realtime il timer batte ogni due secondi, in backtest `ShouldPollOnTimer` lo sopprime e
  il claim parte solo su evento locale (voce del 26/08) — quindi la stessa sessione si comportava in
  due modi a seconda di chi la eseguiva, ed e' esattamente la classe di divergenza che il confronto
  con il conto vero esiste per eliminare. Ora vive in `EvaluateClosedBar`, il corpo comune di
  `PushBars` e `PushBarWindow`, accanto a `PurgeExpiredTemplates`: realtime e backtest cTrader ci
  passano identici, sulla stessa barra e con lo stesso orologio. Per lo stesso motivo il confronto e'
  su `barTimeUtc` e non su `DateTime.UtcNow`, e usa la stessa convenzione conservativa dei template.
  Il controllo di scadenza e' stato tolto dal ramo del claim: lasciarlo in due posti avrebbe
  reintrodotto la dipendenza dal poll.

  L'annullamento libera anche lo slot di gruppo. Liberarlo solo sui percorsi che richiedono un report
  del broker — rifiuto, chiusura, posizione sparita — significava non liberarlo mai proprio nel caso
  per cui la spazzata esiste. L'evento ha un tipo suo, `SessionActivityKind.IntentScaduto`, e non
  ricade in `EsitoEsecuzione` perche' l'esito NON e' arrivato: confonderlo con un report vero
  renderebbe invisibile l'unica traccia di un client che ha smesso di riportare.

  Regressione in `ExpiredIntentSweepTests`, nelle due configurazioni di `EnforceConcurrencyLimits` —
  erano due difetti diversi con lo stesso sintomo — piu' il caso di controllo che un ingresso
  **riempito** non viene toccato, e il caso che dimostra che la spazzata non dipende dal claim.
  Rientrano tre dei cinque test rossi di `SourceBacktestSampleTests`.

  **Resta aperta una contraddizione fra due test, e non e' stata sciolta qui.** §4.3 del documento di
  dominio dice che `AccountHasEntryInFlight` deve seguire `EnforceConcurrencyLimits` — e' concorrenza,
  non identita' del segnale — e misura cosa costa lasciarlo incondizionato: nove template per barra e
  un solo claim servito, otto strategie su nove fuori dal campione sorgente. Nella stessa direzione
  vanno i due test di `SourceBacktestSampleTests` ancora rossi. In direzione opposta va
  `RunProfileTests.BacktestSorgente_NonConsegnaDueIngressiDellaStessaStrategia`, che oggi passa e
  pretende il filtro attivo anche a lucchetti spenti. Le due aspettative non sono conciliabili con la
  sola scadenza — sulla barra N+1 l'intent della barra N e' ancora dentro la propria finestra — quindi
  il filtro resta incondizionato, cioe' com'e' in produzione, finche' non e' deciso quale delle due
  valga. Vedi `lavori-in-corso.md`.

- **2026-08-31** — **Le barre a 4 ore del broker e quelle della ricerca non condividono UN SOLO
  istante di apertura.** Non e' una sfumatura di feed: e' la causa dominante di quel che resta dello
  scarto fra backtest e conto vero, e vale anche in live, non solo nel confronto.

  La griglia del vendor e' ancorata alla **mezzanotte dell'Europa continentale** — 22:00 UTC d'estate,
  23:00 d'inverno — perche' `datafeed-future/aggregate_flat_feed.py` costruisce i bucket a partire da
  li', ed e' la griglia su cui girano i run di ricerca da cui le strategie sono portate. La griglia
  del broker e' ancorata alle **17:00 di New York**, l'apertura di sessione CME: 21:00 UTC d'estate,
  22:00 d'inverno. Un'ora di scarto, sempre, in entrambi i regimi di ora legale. Misurato su
  lug-dic 2024: `@GC_240` **0 istanti in comune su 785**, `@NQ_240` **0 su 782/780**. Il quindici
  minuti invece coincide al 100% (11.942 istanti su 11.942), perche' un bucket da 15 minuti cade
  sulla stessa griglia comunque lo si ancori.

  Conseguenza sui segnali, misurata rieseguendo `Evaluate` sulle due serie: sulle strategie a 240
  minuti la coincidenza dei segnali (istante di barra + verso) e' **0%** — nessun segnale nasce sulla
  stessa barra, perche' non esiste la stessa barra. Sulle strategie a 15 minuti e' **78-96%**.

  Che sia la griglia e non i dati e' dimostrato ricostruendo le barre a 4 ore dal quindici minuti del
  broker **sui bucket del vendor**: la coincidenza dei segnali passa da 0% a **90-98%**
  (`PCH_007` 90%, `PCH_008` 92%, `TFU_008` 97%, `TFM_014` 98%), cioe' nella stessa fascia delle
  strategie a 15 minuti. Il residuo e' la differenza di prezzo fra CFD e future retro-aggiustato, che
  muove i livelli di canale e l'esito dei gate — non l'allineamento.

  **Perche' conta oltre al confronto.** Il cBot spinge al server le barre che cTrader gli da', e
  `MarketSeries(TimeFrame.Hour4)` e' ancorata al broker. Quindi ogni strategia a 240 minuti sul conto
  vero sta valutando un canale di Donchian e una sessione **diversi da quelli su cui e' stata
  validata**, e nessuno se ne accorgeva perche' i trade prodotti sono plausibili. Le coppie a 240
  minuti sono circa il 55% dei trade che i due sistemi non si scambiano.

  La direzione della correzione e' la stessa che il vendor usa gia': si raccoglie e si spinge il
  **timeframe piu' fitto**, e i bucket li fa Piootoo con la propria convenzione. Vale sia per il cBot
  di esecuzione sia per `PiootooDatafeedSyncBot`, che oggi raccoglie il timeframe nominale e quindi
  scrive in `datafeed-external/` una serie non confrontabile con `datafeed/`. Non e' stato fatto qui:
  tocca i due bot e il contratto di push, ed e' una modifica che va misurata da sola.

  Verificato su **entrambi i simboli e con due sorgenti di aggregazione diverse** — NQ dal quindici
  minuti, GC dal trenta:

  | strategia | serie nativa del broker | serie riallineata |
  |---|---|---|
  | `PTS_GC_PCH_004_240` | 0% | **96%** |
  | `PTS_GC_PCH_005_240` | 0% | **89%** |
  | `PTS_GC_TFM_001_240` | 0% | **90%** |
  | `PTS_NQ_PCH_007_240` | 0% | **90%** |
  | `PTS_NQ_PCH_008_240` | 0% | **92%** |
  | `PTS_NQ_TFU_008_240` | 0% | **97%** |
  | `PTS_NQ_TFM_014_240` | 0% | **98%** |
  | `PTS_GC_TFU_001_30` (controllo, 30m) | **92%** gia' nativo | — |

  Il controllo a 30 minuti e' la conferma per assurdo: non ha bisogno di essere riallineato perche'
  la sua griglia gia' coincide, e sta esattamente nella fascia in cui le 240 arrivano *dopo* la
  ricostruzione. Il tetto del 90-98% e' la differenza di prezzo fra CFD e future retro-aggiustato,
  non l'allineamento: quella non si toglie riaggregando.

- **2026-08-31** — **Le due gambe di un bracket non sono un doppione: il lato entra nei lucchetti
  del claim.** Rilasciato come **3.13.0**. Non cambia nessun backtest — il percorso corretto esiste
  solo nella sessione `ExternalBroker` — ma cambia quali trade fa un run esterno, quindi i run
  precedenti non sono confrontabili con i successivi.

  I filtri del claim erano chiavati su (strategia, simbolo) senza il verso:
  `AccountHasEntryInFlight` e `SlotKey`. Un motore non simmetrico emette le due gambe sulla
  **stessa barra** — `EasyEngineBase.Combine` mette il Buy in `entries[0]` e il Sell in
  `CompanionSignals` — e il claim ne serve una alla volta, ordinata per `CreatedAtUtc` a parità di
  priorità, cioè in ordine di lista. Servito il Buy, il Sell veniva rifiutato finché il primo era
  `Pending`: e il Buy è `Pending` per sempre, perché viene riemesso e rireclamato a ogni barra.
  Il bracket non poteva mai stare a mercato intero, e quale gamba passasse non lo decideva il
  mercato ma la posizione nella lista.

  Misura, dal confronto `compare-0009` (GC/XAUUSD, 1/7 → 13/11 2024): su `PTS_GC_PCH_004_240` il
  conto vero ha **47 long e zero short**, il backtest interno 25 e 20 sugli **stessi identici
  segnali**. La strategia emette 127 buy e 127 sell nella finestra, sempre in coppia — i gate
  direzionali sono alle sentinelle 52/53, quindi le due gambe non hanno mai una barra tutta loro. Nel
  log eventi del cBot non esiste **nessun** sell stop con la firma SL 5,0 / TP 100,0 in cinque mesi;
  il controllo `PTS_GC_PCH_005_240` — stesso motore, stesso simbolo, stesso `Direction = 0`, ma gambe
  spesso indipendenti — ne ha 30 su 67. La corrispondenza è monotona su tutto il paniere: zero barre
  con solo short → zero ordini short piazzati, poche → pochi, molte → molti.

  Il feed non c'entra, ed è stato escluso misurandolo invece di assumerlo: rieseguendo `Evaluate`
  sulle barre del broker (`datafeed-external`, @GC 240m) la strategia emette 244 buy e **244 sell**,
  esattamente come sul feed vendor: il verso non dipende dalla serie.

  **Il ramo delle posizioni resta cieco al verso, ed è voluto**: lì la cecità *è* la regola. Una
  gamba riempita deve impedire l'altra, altrimenti la strategia sta long e short insieme — è l'OCO,
  lo stesso che il cBot impone con `EnforceBracketOco` e `alreadyOpenOnStrategy`. La distinzione fra
  i due rami è l'unica cosa che questa voce aggiunge al ragionamento del 26/08: allora fu corretto
  `CancelStrategyPendingOrders` nel cBot, con le stesse identiche parole, e il gemello lato server
  non fu fatto. Il sintomo è rimasto per quello.

  Il motore interno faceva già la cosa giusta, con il commento che lo dice:
  `PiootooTradingService.EnqueuePendingOrder` mette il verso nella chiave del pending — «long e
  short stop possono coesistere (OCO logico)». È per questo che il backtest non ha mai mostrato il
  difetto: non passa dal claim, che è il meccanismo di distribuzione multi-account e non esiste
  altrove in `Piootoo.Core`.

  Regressione in `BracketClaimSideTests`: le due gambe arrivano allo stesso account, il doppione
  dello stesso lato resta rifiutato (il caso `PTS_NQ_PCH_002_15` del 14/10/2024), e la gamba opposta
  resta rifiutata dopo il fill dell'altra. Il primo test è stato verificato togliendo la guardia:
  senza, fallisce.

  **Verificato su `compare-0010`** (stesso interno bit per bit, run esterno nuovo con 3.13.0). Nel
  log eventi del cBot gli ordini short di `PTS_GC_PCH_004_240` passano da **0 a 84 creati e 28
  riempiti**, contro 84 long creati: le due gambe risultano ora emesse in numero **esattamente
  uguale**, che e' la firma prevista dalla sonda — i gate direzionali alle sentinelle le fanno
  nascere sempre in coppia. Le firme delle altre cinque strategie colpite si muovono nello stesso
  verso (TFM_003_15 613 -> 1.877 short creati e 0 -> 9 riempiti; TFU_005_60 156 -> 288 e 0 -> 4;
  TFM_003_240 10 -> 111 e 0 -> 15).

  Sulla finestra comune (1/7 -> 13/11 2024, stesso interno) il conto dei trade per verso passa da
  737 long / **155** short a 723 / **256**, contro i 738 / 244 del backtest: la popolazione dei due
  motori ora concorda su entrambi i lati, e non piu' solo sui long. Le strategie con short interni e
  zero short esterni scendono da otto a due, ed entrambe le rimaste ne hanno uno o due in tutto.
  Il tasso di appaiamento e' identico fra i lati (46% sui long, 42% sui short, contro il 45%
  complessivo): l'asimmetria per verso e' sparita. Lo scarto medio per trade appaiato scende da
  +1,93 a +0,74 punti.

  **Cio' che questa correzione non tocca**, e che resta il residuo dominante: il 55% dei trade non
  si appaia dentro la barra e il 35% nemmeno nella giornata, sulle stesse strategie di sempre
  (`PTS_GC_PCH_004_240`, `PTS_GC_PCH_005_240`, `PTS_ES_PCH_004_240`, `PTS_FDAX_PCH_001_240`,
  `PTS_NQ_TFU_008_240`). E' la divergenza di serie fra feed vendor e barre del broker, che il lavoro
  su `datafeed-external` sta andando a chiudere.

  **Resta aperto, di proposito**, `MaxEntriesPerSession`, che conta gli ingressi per sessione senza
  il verso in **tutti e due** i motori (`EntryFillKey` sul server, `MakeEntrySessionKey`
  nell'engine), mentre le strategie dichiarano «una entrata per sessione *e per direzione*».
  Correggerne uno solo li farebbe divergere, e correggerli entrambi cambia i trade dei backtest già
  fatti: è una release a sé, dopo il run che misura questa. Vedi `lavori-in-corso.md`.

- **2026-08-30** — **Il claim di una sessione non scorre piu' la storia degli intent.** Un backtest
  esterno rallentava giorno dopo giorno anche dopo le tre correzioni di agosto (finestra di
  valutazione, checkpoint, journal incrementale): quelle avevano reso piatto il percorso della
  barra, ma non il claim. Misura su una sonda a 9 strategie @NQ 15m con drenaggio a ogni barra, ms
  ogni 500 barre: push 135 → 68 (piatto), **claim 5.460 → 17.816 → 32.153 → 33.139**. Il costo per
  barra era proporzionale agli intent gia' accumulati — a fine anno decine di migliaia — quindi il
  run era quadratico.

  Il profilo interno al claim (1.500 barre, 13.500 intent) dice dove: **`DescribeSessionLimit`
  42.020 ms su 52 totali**. E' pura diagnostica, ma `NarrowTemplates` prendeva il motivo come
  `string`: veniva quindi costruito a **ogni** claim, anche quando quel filtro non scartava niente,
  e costruirlo costava due scansioni di tutti gli intent del run *per candidato*. Ora esiste la
  variante con `Func<string>` e il motivo si costruisce solo se e' quel filtro a svuotare la lista;
  chi passa una costante resta sull'overload di prima, per non allocare una closure per niente.

  Sotto c'erano altre cinque scansioni della stessa lista, tutte per leggere il sottoinsieme degli
  ordini davvero in volo: chiusure pendenti dell'account, budget di concorrenza, ingresso gia' in
  volo, template reclamabili e `PendingIntents` dello snapshot (che ogni execution report
  restituisce). `Session.LiveIntents` tiene quel sottoinsieme — qualche decina di elementi — e si
  pota **pigramente** (`Live()`, `RemoveAll(IsSettled)`): gli stati cambiano in troppi punti perche'
  valga la pena intercettarli tutti, ed e' cosi' che un indice incrementale perde un elemento in
  silenzio. Un intent assestato non torna indietro; l'unico caso in cui potrebbe — un report tardivo
  del broker — lo rimette in lista `ApplyReport`. I cinque filtri hanno lo stesso significato di
  prima perche' `Pending`/`Accepted`/`PartiallyFilled` **e'** la definizione di non assestato.

  `MaxEntriesPerSessionReached` invece conta i *fill*, che sono assestati per definizione: quello
  passa da `Session.EntryFills`, indice `strategia|simbolo` → (secchio, account) → quanti ingressi
  hanno almeno un riempimento, aggiornato nell'unico punto in cui un intent passa da zero a
  riempito. Cresce di una voce per (giorno, account), non per intent, e serve anche i due numeri
  della diagnostica senza rileggere niente. Stesso trattamento per la priorita' di consegna dei
  template senza rotazione Titano: `Session.StrategyNetPnl` si accumula alla chiusura del trade
  invece di risommare tutti gli `ExternalTrades` a ogni poll.

  Dopo, sulla stessa sonda: **8.000 barre e 72.000 intent in 5,5 secondi**, claim fra 76 e 168 ms
  ogni 500 barre senza tendenza (prima le stesse 8.000 barre non finivano in dieci minuti), report
  da 254→845 ms a ~50 ms piatti. Nessun cambiamento nella suite: gli stessi 55 test rossi prima e
  dopo, uno per uno.

  Verificato nello stesso giro che la sessione tiene in RAM **solo le candele necessarie**:
  `TrimHistory` la mantiene a `EvaluationWindowSize` + 512 e pota a blocchi di 1.024, misurato a
  dente di sega fra 1.500 e 2.100 candele su 4.000 barre con `RequiredCandles` 576, con il push
  costante a ~2 ms per barra. E che il datafeed resta disaccoppiato: `ExternalDatafeedStore` e'
  raggiungibile solo da `DatafeedExternalController`, il cBot distribuito non chiama mai
  `api/datafeed-external` e il raccoglitore non chiama mai `trading-sessions`.

## 2026-08-31 — Report HTML anche per i backtest dell'engine esterno

Il dettaglio di un backtest ha un secondo pulsante, **Genera report**: chiede al server di
costruire il report HTML dai JSON che il run ha lasciato nella cartella. Serve ai run
dell'engine esterno, che archiviano `trades.json` e `signals.json` ma il report no — fino a
ieri l'unico modo di guardarli era la griglia dei trade — e ai run interni interrotti prima
della scrittura degli artefatti.

Il report è **lo stesso** dei run interni, non una seconda versione: la generazione è uscita
da `PiootooBacktestingService` ed è ora `BacktestHtmlReport`, che ha due chiamanti (il motore
a fine run, e `ExternalBacktestReportService` a posteriori). Due generatori avrebbero prodotto
due report diversi per gli stessi trade, e il confronto interno/esterno è esattamente ciò a cui
il report serve. Per lo stesso motivo le chiavi di strategia stanno in `StrategyKeys`: il
raggruppamento `SYMBOL|StrategyCode` è uno solo, per il loop e per i report.

Cosa cambia nella ricostruzione, ed è dichiarato in testa al report invece che lasciato dedurre
dalla forma della curva: **l'equity è quella realizzata**, un gradino alla chiusura di ogni
trade. Il server non ha le barre di quel run — in sessione `ExternalBroker` la storia è solo
quella che il client spinge, e resta in RAM — quindi le posizioni aperte non sono valorizzate a
mercato e il drawdown è misurato fra chiusure, non fra massimi intraday. L'equity per strategia
parte comunque dal capitale iniziale e ci somma il proprio netto cumulato, la convenzione di
`GetStrategyEquities`, altrimenti le due curve non sarebbero sovrapponibili.

Il capitale iniziale non stava da nessuna parte: il piano non ne ha uno (vedi 2026-08-05) e la
sessione non ne lasciava traccia. Ora `origin.json` lo registra all'apertura
(`BacktestOriginInfo.InitialCapital`); per le cartelle più vecchie il report assume 100.000 e lo
scrive fra le note, perché profit e drawdown in valuta non ne dipendono ma le percentuali sì.
L'endpoint accetta un `initialCapital` per rileggere lo stesso run su un'altra base.

Rifiuta (409) quando la cartella **ha già** un report del proprio run. Il criterio è la presenza
del file, non l'origine dichiarata: le cartelle scritte prima del marcatore hanno origine ignota
pur essendo run interni completi, e lì una ricostruzione avrebbe affiancato al report del motore
una curva diversa che, essendo più recente, lo avrebbe scavalcato all'apertura del dettaglio
(`GetBacktestHtmlReportPath` serve l'HTML più recente). Il nome del file ricostruito è fisso,
così rigenerare sostituisce invece di accumulare.

`POST api/Workspace/{ws}/backtests/{cartella}/report` genera, il `GET` di sempre serve il file.
Verificato su `pts-02/ftmo-trial-01-bt-20250716000000`: 280 trade, 18 strategie, profit -13.590,60
e max drawdown 115.469,00 (61,88%), identici al ricalcolo indipendente sullo stesso `trades.json`.


- **2026-08-31** — Il backtest sceglie **da quale archivio di barre legge**: il datafeed interno
  (`piootoo-repository/datafeed/`) oppure quello di un broker, sotto
  `piootoo-repository/datafeed-external/{BROKER}/`. Prima la radice era una sola, fissata in
  `PiootooSettings.RepositoryPath`, e le barre raccolte dal bot cTrader non erano confrontabili con
  quelle del vendor senza scambiare le cartelle a mano fra un run e l'altro.

  Le due strutture hanno lo stesso formato — file piatti `@SYM_{minuti}.json` più `feed-clocks.json` —
  quindi `DataSourceRepository` non sa quale delle due sta leggendo: cambia solo la radice, e a
  deciderla è `DatafeedCatalog`, unico punto che traduce un nome di broker in un path (e che quindi
  può rifiutare quello che path non è). `PiootooDataFeedService` tiene un repository per radice,
  perché ricrearlo significherebbe rileggere `feed-clocks.json` per ogni datasource del run.

  Un run legge da **una sola** radice: due broker chiudono le stesse candele su prezzi diversi, e un
  backtest a cavallo delle due non corrisponderebbe ad alcun conto. Per lo stesso motivo
  `BacktestingRequest.DatafeedBroker` finisce in `backtest-summary.json` accanto a `holding`: è una
  scelta che cambia i risultati senza comparire nei trade. Null = interno, sempre, così i run e le
  richieste già scritte non cambiano comportamento.

  Un broker inesistente **fa fallire l'avvio** prima ancora che la cartella di output venga creata:
  è la stessa regola del datafeed mancante, mai proseguire in silenzio ripiegando sull'interno.
- **2026-08-31** — **Il workspace è contesto della console, non un filtro di ogni schermata.** È
  scelto una volta nella barra in alto (`WorkspaceSelection` in `piootooapp.clientform/Shell/`) e
  vale per tutte: liste backtest, piani, run Titano, avvio backtest, rotazione, sessioni e verifica
  concorrenza non hanno più una combo propria. Prima ognuna rileggeva l'elenco dal server e chiedeva
  di nuovo la stessa scelta, e due schermate potevano puntare a workspace diversi senza che nulla lo
  dicesse.

  Cambiare workspace ricarica la schermata aperta e chiude gli eventuali dettagli: erano stati
  aperti per un'entità di un altro workspace. Dove il workspace resta rilevante da leggere — avvio
  di un backtest, nuova rotazione, dettaglio piano, sessione diretta — resta a video come etichetta
  in sola lettura: dice dove finiranno gli artefatti senza tornare a essere una scelta.

  **L'anagrafica dei workspace esce dal menu di sinistra** e diventa "Gestisci workspace…" accanto
  al selettore: quel menu elenca cose che stanno *dentro* un workspace, e l'unica voce che non può
  essere filtrata per il workspace corrente è proprio la radice. Creare o eliminare un workspace
  aggiorna il selettore, e la schermata di anagrafica è l'unica che un cambio di selezione non
  azzera — è il posto da cui quel cambio arriva.

- **2026-08-31** — **La versione è `major.minor.patch`, il contratto è `major.minor`** (3.11.1).
  Console e cBot confrontano con il server solo il contratto: una patch diversa non è un
  disallineamento, si legge nella barra di stato e non apre alcun popup. Serve a portare una fix su
  una delle tre parti senza obbligare le altre a muoversi — in particolare senza ricompilare e
  ridistribuire i cBot su ogni macchina per un numero che per loro non cambia nulla.
  `VersioneDelProgettoTests` verifica di conseguenza: costante e `VersionPrefix` devono coincidere
  esattamente (stessa build), il sorgente del cBot deve dichiarare almeno lo stesso contratto. Un
  salto di minor resta un disallineamento vero, e continua ad avvisare.

- **2026-08-31** — I confronti fra run hanno **tre tipi di backtest** con un nome ciascuno, e il
  nome sta nel file: `cbot-cfd-{BROKER}` (bot cTrader, barre CFD del broker), `interno-futures`
  (engine interno, `datafeed/`), `interno-cfd-{BROKER}` (engine interno,
  `datafeed-external/{BROKER}/`). Gli artefatti si chiamano `<artefatto>-<slug>.json`.

  Serviva perché motore e feed cambiano insieme senza che si veda: `compare-0012` metteva a
  confronto `tradesinternal.json` e `trades-rxternal.json`, e da quei nomi non si capiva né che il
  secondo era il bot cTrader (non un run interno su feed di broker) né su quale broker girava —
  ICS, mentre l'unico archivio esterno su disco è RAWTRADINGLTD. Un run del cBot su un broker e un
  run interno sul feed di un altro non sono confrontabili, e il nome ora lo dice prima che
  qualcuno sommi i due totali.

  Il broker entra nel nome ogni volta che il feed è CFD, per la stessa ragione per cui
  `backtest-summary.json` dichiara `datafeedBroker`. Convenzione e trappole di misura in
  `piootoo-repository/compare/README.md`.

  **Lo slug lo calcola il run, non chi rinomina.** `origin.json` — che già distingueva motore
  interno ed engine esterno — porta ora anche `PriceSource` (`Futures` | `BrokerCfd` + nome del
  broker), `EngineVersion` e, per l'origine esterna, l'`AccountNumber` da cui il broker si
  ricava; `RunSlug` è calcolato da `Origin` × `PriceSource` e si copia nel nome dei file
  esportati. Niente campo "tipo di run" a sé: sarebbe una quarta cosa che può contraddire le
  altre tre. Sta in `origin.json` e non solo nel summary perché **due run interni su feed diversi
  producono trade indistinguibili** — stesse size, stesse commissioni, livelli abbastanza simili
  da non poterci scommettere — e il summary manca appena il run si interrompe. Le cartelle
  scritte prima danno `interno-feed-sconosciuto`: di un run interno vecchio il feed non è
  ricostruibile, e dedurlo sarebbe peggio che dichiararlo ignoto.

  **Esporta per confronto** (dettaglio backtest in console) chiude il giro: il server impacchetta
  `trades.json`, il summary e `origin.json` rinominati con lo slug, il client scompatta nella
  cartella scelta. È uno zip su HTTP e non una copia di file perché la console è un client HTTP
  puro e non condivide il filesystem col server. Tre regole nel servizio: `CompactAll()` prima di
  leggere — l'array è indietro finché il journal `.jsonl` non è compattato, e si esporterebbe un
  file che sembra completo; `signals.json` fuori, perché nei portafogli sono centinaia di MB;
  e un run che non identifica sé stesso è un `409`, non un export. Quest'ultima è più stretta di
  "lo slug è noto": `BacktestOriginInfo.IdentifiesRun` rifiuta anche un CFD di cui manca il
  broker, perché `cbot-cfd` sta ugualmente per due serie di prezzi diverse.

- **2026-08-31** — **L'elenco dei run Titano di una cartella non si rilegge da disco a ogni
  barra.** `ResolveLatestRun` passa da `ListRuns`, che apriva **ogni** manifest della cartella con
  un `File.ReadAllBytes` — nel repository quei file stanno fra i 672 KB e 1 MB. Quel percorso lo
  percorre `EvaluateClosedBar` una volta per barra e, sul claim multi-account, lo percorreva **una
  volta per template candidato per account**: la `ManifestCache` non lo copriva, perché il listing
  non passa da `Get`. Misurato sul manifest vero di `pts-02/backtest-20260803-0441` (984 KB, 104
  periodi): la catena di una barra — runId più `Resolve` — costava **13,8 ms** e ora costa
  **0,46 ms**. Su un run di un anno a 15 minuti sono ore contro minuti, ed è la ragione per cui il
  backtest esterno era tornato lento dopo il 31/08: la fix di `PurgeExpiredEntryIntents` (3.14.0) ha
  smesso di tenere vuota la lista dei template, e il percorso costoso ha ricominciato a girare
  davvero.

  Cache invalidata sull'ultima modifica della cartella `titano/`: un run nuovo crea
  `titano/{runId}/` e una cancellazione la rimuove, quindi l'invariante "una rotazione nuova si
  applica dalla barra successiva senza riaprire la sessione" resta intatto. Il contenuto di un run
  già elencato non cambia — il runId è l'hash dei suoi input — e i file di hard-stop reset, che
  invece si aggiungono a caldo, non entrano in `TitanoRunInfo`: li legge `Get`, che ha la propria
  invalidazione. Tetto LRU come per i manifest, perché il numero di cartelle cresce a ogni backtest.
  `TitanoStateMachineTests.ResolveLatestRun_ReadsTheFolderOnce_AndSeesANewRotationImmediately` fissa
  le due metà insieme: nessuna rilettura quando non cambia niente, rotazione nuova vista subito
  quando cambia.

  Stessa cura per `masterfilter.json`, che `Resolve` rileggeva e deserializzava a ogni chiamata, e
  per la rotazione del gruppo nel claim: si risolve **una volta per claim** invece che una per
  template. Quest'ultima non è solo costo — è coerenza: i due usi (chi è ammesso, con che
  allocazione) sono lo stesso giudizio, e risolverli separatamente li lasciava disallineabili da una
  rotazione che atterra fra l'uno e l'altro. La risoluzione resta differita perché i chiamanti hanno
  cortocircuiti che oggi evitano di risolvere del tutto, e anticiparla farebbe fallire un claim che
  oggi passa.

  Resta sul tavolo il residuo: dei 0,46 ms per barra, **0,20 sono le due stat di filesystem** con
  cui `Get` verifica la propria cache. Toglierle vuol dire memoizzare `Resolve` per periodo e
  rivalidare a tempo, che è una semantica diversa per gli hard-stop reset: da fare con un numero in
  mano, non per simmetria.
