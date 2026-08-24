# Trading sessions API v1

`FeedRunner` non è ospitato dall'API: legge i file nel backtest oppure gira come
cBot cTrader e invia esclusivamente barre chiuse. La sessione viene creata dal
`masterfilter.json` del workspace; workspace vuoti e ID non presenti nel
catalogo sono rifiutati senza fallback.

## Sequenza

1. `POST /api/v1/trading-sessions` crea direttamente una sessione, oppure
   `POST /api/v1/trading-sessions/open-plan` risolve il codice piano e crea/riprende
   idempotentemente la relativa esecuzione. Entrambi restituiscono `SessionId`,
   token e coppie simbolo/timeframe derivate dal masterfilter.
2. `POST /{id}/start`, `/stop`, `/resume` controllano il lifecycle. Il token è
   nel payload dove previsto o nell'header `X-Session-Token`.
3. `POST /{id}/bars` accetta batch di barre chiuse. Ogni stream
   simbolo/timeframe richiede sequence crescente e idempotency key stabile.
   Replay della stessa key è deduplicato; una sequence arretrata è rifiutata.
3-bis. `POST /{id}/bars/window` accetta, per stream, l'**intera finestra** di candele
   che le strategie richiedono: il server accoda quelle che non ha e valuta **solo
   l'ultima**. Sequence e idempotency key valgono su quella. Esiste perché in
   `ExternalBroker` il server non ha un datafeed proprio — la storia di uno stream è
   solo ciò che il client gli ha spinto — e con una barra per volta le prime
   `RequiredCandles` barre di ogni run venivano scartate in silenzio da
   `StrategyEvaluationService`: per una strategia a 15 minuti sono 576 barre (sei
   sessioni piene), quindi un backtest più corto non produceva un solo segnale.
   Le candele viaggiano in due tempi. All'avvio, una volta per stream, il client manda
   tutta la storia richiesta con `EvaluateLastCandle = false`: il server **accoda e
   basta**, senza valutare, senza consumare l'idempotency key e senza avanzare la
   sequence — sono barre già passate e valutarle produrrebbe intent sul passato. Poi,
   a ogni barra chiusa, manda una finestra corta (default 20 barre) con
   `EvaluateLastCandle = true`.
   La sovrapposizione non è banda sprecata: è ciò che impedisce i buchi. Il server
   **rifiuta una finestra che comincia dopo la sua ultima candela nota**, e il criterio
   è la sovrapposizione e non l'aritmetica sui timestamp perché fine settimana e
   festivi sono buchi legittimi. Con 20 barre si ricuciono da sole fino a 19 barre
   consecutive perse; oltre, l'errore è esplicito invece che silenzioso.
   Quanta storia serve lo dice il descriptor, in
   `TradingInstrument.RequiredCandlesByTimeframe`. La risposta porta, per stream,
   `HistoryBars`/`RequiredCandles`/`SkippedForInsufficientHistory`, così il silenzio
   di una sessione è leggibile invece che indistinguibile da "nessun segnale".
   **Le candele restano in RAM**: la sessione non scrive datafeed su disco
   (`TradingJsonStore` persiste signal, trade e rotation-log), raccogliere il feed da
   cTrader è compito di un cBot dedicato.
   Regole complete, con la procedura per diagnosticare una sessione muta, in
   [`finestra-candele-e-riscaldamento.md`](finestra-candele-e-riscaldamento.md).
4. Per ogni barra l'orchestratore processa prima exit e pending, valuta poi
   soltanto le strategie con lo stesso timeframe esplicito e infine pubblica
   gli intent. Non si aggregano barre e non si usa MCM: 5m/15m e 7m/15m sono
   stream di bar-close indipendenti.
5. `GET /{id}/intents?after=N` effettua polling; gli intent hanno ID stabili.
6. In `ExternalBroker`, `POST /{id}/execution-reports` applica report
   idempotenti Accepted/PartiallyFilled/Filled/Rejected/Cancelled.
7. `GET /{id}/snapshot` restituisce stato sessione; `DELETE
   /{id}/intents/{intentId}` cancella un intent ancora pendente.

## Apertura tramite piano

`open-plan` è il percorso usato da entrambi i cBot. Il payload contiene `PlanCode`,
`ClientRunMode`, `ExecutionKey` e il numero account rilevato da cTrader. Il codice piano viene
risolto globalmente; l'account deve coincidere con quello configurato nel piano.

`DistributeToAccounts` sceglie il canale di consegna dei segnali: `true` (default) attiva la
distribuzione multi-account di `PiootooDistributedExecutionBot`, `false` lascia la sessione senza gruppi e
fa restituire a `POST /bars` intent già assegnati, come richiede `PiootooDirectExecutionBot`.
Dettaglio e vincoli in `docs/domini/trading-plans.md`.

La chiave `(PlanCode, ClientRunMode, ExecutionKey)` — più l'account in esecuzione diretta, dove la
sessione non è condivisibile — separa le esecuzioni e rende il riavvio del
cBot idempotente. In backtest il cBot deriva l'execution key dall'istante iniziale della
simulazione; in realtime usa la chiave stabile `LIVE`.

Il piano decide automaticamente la modalità: senza filtro `Disabled`, con filtro
`BacktestRotationFile` nel backtest e `Realtime` sul mercato reale. La sessione riceve inoltre
uno snapshot della riga gruppo/account del piano.

La mappa delle sessioni resta in RAM: l'idempotenza copre il riavvio del cBot, non ancora il
riavvio del processo server.

Il resume del cBot comprende anche lo stato locale delle uscite: le condizioni
`CloseAtUtc`/`MaxBarsInPosition` delle posizioni vengono persistite per piano e account e
riconciliate con le posizioni presenti su cTrader all'avvio. Il file viene ignorato se il server
ha risolto una sessione diversa, evitando di riportare chiusure contro intent non appartenenti
alla sessione corrente.

## Intent di ingresso e chiusure

Il server emette **solo** intent `OrderIntentKind.Entry`. Ciascuno porta la specifica di
uscita completa — `StopLoss`, `TakeProfit`, `BreakEven`, `TrailingStop`, `CloseAtUtc`,
`ProfitStallAfterUtc`, `MaxBarsInPosition` — e il client la applica **per intero**: SL/TP come
livelli nativi del broker, breakeven e trailing come modifiche dello stop nativo sorvegliate a
ogni tick, uscita a tempo, stallo dell'utile e limite barre valutati a ogni barra. Le strategie
che deciderebbero l'uscita a runtime verificando un pattern sono `IsPositionCloseDependent` e
vengono escluse dal catalogo.

Applicarne solo una parte non produce una versione prudente della strategia, ne produce
un'altra: sulle PC del catalogo il trailing stop è la causa di uscita di circa un trade su tre
ed è da solo tutto il profitto. Il difetto è invisibile dai contratti — l'intent viene eseguito,
i trade nascono, i report tornano — e si vede solo confrontando i numeri col backtest.

Il client deve inoltre rispettare tre cose che non sono uscite. **`Status`**: il server consegna
anche intent che ha già scartato (sizing a zero, allocazione Titano nulla, limite di fill per
sessione raggiunto), per tracciabilità; solo `Pending` va eseguito. **`FinalQuantity`**: è la
quantità dopo Titano e sizing, e `Quantity` non è un ripiego quando è zero — ricadere su di essa
rimette a mercato esattamente i segnali rifiutati. **`ExpiresAtUtc`**: gli ordini dei motori
Unger sono `next bar at ... stop`, vivono la sola barra successiva al segnale e vengono riemessi
a ogni barra col livello ricalcolato, quindi l'ordine pending precedente va cancellato, non
lasciato a mercato accanto al nuovo.

`MaxEntriesPerSession` e `EntrySessionStartUtc` viaggiano sull'intent per diagnostica, ma il
limite è applicato dal server sui **fill confermati**: in `PushBars` per le sessioni a singolo
account, in `GetNextSignalForAccount` per account in multi-account. Uno stop non eseguito non
consuma il limite.

In multi-account `POST /bars` restituisce **template non assegnati**, che vanno reclamati da
`GET /accounts/{n}/signals`: eseguirli direttamente scavalca slot di gruppo, limite di trade
concorrenti ed eleggibilità, e lo stesso template finisce su più account. Vedi
[`distribuzione-multi-account.md`](distribuzione-multi-account.md).

Le chiusure hanno un canale unico, qualunque ne sia la causa:
`POST /{id}/intents/close-external` registra un intent `OrderIntentKind.Close` per la
posizione aperta, che il client referenzia nel normale `POST /{id}/execution-reports`. È così
che nasce il `PersistedTrade` che alimenta Titano.

## Dove persiste una sessione

Il percorso dipende dal contesto, e non è un dettaglio organizzativo:

| Sessione | Cartella |
|---|---|
| Aperta da piano, `ClientRunMode = Backtest` | `<workspace>/backtests/{piano}-{executionKey}/` |
| Aperta da piano, `Realtime` | `<workspace>/sessions/{piano}-{executionKey}/` |
| Creata direttamente (`POST /trading-sessions`) | `<workspace>/sessions/{guid}/` |

Le sessioni di backtest scrivono **dentro `backtests/`**, cioè dove `TitanoRotationService` cerca il
campione sorgente: un run dell'engine cTrader è utilizzabile da una rotazione senza passaggi
intermedi. Prima finivano in `sessions/<guid>/` e andavano copiate a mano; dimenticarlo non dava
errore, dava una rotazione calcolata su un campione vecchio.

Le sessioni realtime restano separate — non sono campioni — ma prendono anch'esse un nome parlante:
una cartella che non dichiara il piano non è né ispezionabile né ripulibile.

La pulizia delle cartelle vecchie resta manuale.

## Promozione a campione Titano

`POST /{id}/promote-to-backtest` copia `trades.json` e `signals.json` della sessione in
`<workspace>/backtests/{cartella}/`. Da quando le sessioni di backtest aperte da piano scrivono già
lì (vedi sopra), serve nei casi restanti: sessioni create direttamente senza piano, e sessioni
realtime di cui si voglia usare lo storico come campione.

Due rifiuti espliciti: una sessione **senza trade chiusi** non è promuovibile, perché una rotazione
su campione vuoto non fallisce ma disabilita tutte le strategie; una cartella già esistente non
viene sovrascritta senza `OverwriteExisting`, perché l'id di un run Titano contiene l'hash del
`trades.json` di origine e cambiarlo rende il run non riproducibile.

## Modalità Titano

`TitanoMode` (`Disabled`, `BacktestRotationFile`, `Realtime`) decide se e come la rotazione
filtra le strategie valutate; le ultime due richiedono `TitanoRunId` + `TitanoBacktestFolder`
e la creazione della sessione è rifiutata se mancano. Il client non conosce Titano: riceve i
segnali già filtrati e si comporta identico in tutte e tre le modalità.

`ClientRunMode` (`Backtest`, `Realtime`, `Unknown`) è il contesto dichiarato dal client — il cBot
lo legge da `Robot.IsBacktesting`, non da un parametro. Il server incrocia i due e rifiuta
`Realtime` in backtest (look-ahead) e `BacktestRotationFile` in tempo reale (il manifest copre solo
l'intervallo del backtest sorgente). Con `Unknown` non verifica nulla.

Dettaglio in `docs/PROGETTO.md` §3.6.

## Autorità di execution

- `ServerSimulated`: `PiootooTradingService` per-sessione simula ordini, fill,
  SL/TP e time exit. I report broker esterni sono rifiutati.
- `ExternalBroker`: il server non apre posizioni e non simula fill. Solo
  quantità effettivamente riportate dal broker aggiornano posizione, entries e
  fills; reject e non-fill non lo fanno.

Lo stato è isolato per `SessionId` e serializzato da un lock per sessione. Le
API usano `ProblemDetails`; token e validazioni di symbol/workspace creano un
boundary pronto per un provider di autenticazione futuro.

Riferimenti codice: `PiootooApp.Server/Controllers/TradingSessionsController.cs`,
`Piootoo.Core/Services/TradingSessionService.cs`.

Vedi anche: `position-sizing.md` per il calcolo di `FinalQuantity`.
