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
- **2026-07-29** — `PiootooLiveTradingBot` gira su grafico 5m (configurabile come timeframe base)
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
