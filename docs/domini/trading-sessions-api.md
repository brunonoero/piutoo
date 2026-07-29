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

`open-plan` è il percorso usato da `PiootooLiveTradingBot`. Il payload contiene `PlanCode`,
`ClientRunMode`, `ExecutionKey` e il numero account rilevato da cTrader. Il codice piano viene
risolto globalmente; l'account deve coincidere con quello configurato nel piano.

La chiave `(PlanCode, ClientRunMode, ExecutionKey)` separa le esecuzioni e rende il riavvio del
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
uscita completa — `StopLoss`, `TakeProfit`, `BreakEven`, `CloseAtUtc`, `MaxBarsInPosition` —
e il client la applica: SL/TP come livelli nativi del broker, uscita a tempo e limite barre
sorvegliati in locale. Le strategie che deciderebbero l'uscita a runtime verificando un
pattern sono `IsPositionCloseDependent` e vengono escluse dal catalogo.

Le chiusure hanno un canale unico, qualunque ne sia la causa:
`POST /{id}/intents/close-external` registra un intent `OrderIntentKind.Close` per la
posizione aperta, che il client referenzia nel normale `POST /{id}/execution-reports`. È così
che nasce il `PersistedTrade` che alimenta Titano.

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
