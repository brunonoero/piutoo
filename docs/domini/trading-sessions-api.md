# Trading sessions API v1

`FeedRunner` non è ospitato dall'API: legge i file nel backtest oppure gira come
cBot cTrader e invia esclusivamente barre chiuse. La sessione viene creata dal
`masterfilter.json` del workspace; workspace vuoti e ID non presenti nel
catalogo sono rifiutati senza fallback.

## Sequenza

1. `POST /api/v1/trading-sessions` crea la sessione e restituisce `SessionId`,
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
