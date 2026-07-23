# Position sizing e confine autorevole

La quantità è determinata dal server una sola volta:

`FinalQuantity = BaseQuantity × StrategyEquityMultiplier × MarketVolatilityMultiplier × PortfolioRiskMultiplier`.

Titano produce il coefficiente strategy-equity lento, efficace per periodo. Il
coefficiente market-volatility usa ATR calcolato soltanto sulle barre con
timestamp non successivo alla barra corrente, `DollarsPerPoint` e rischio
monetario target. Il coefficiente portfolio usa equity, peak/drawdown,
esposizione e, opzionalmente, floor/cushion CPPI.

I coefficienti sono clampati in `[0,1]` di default e la quantità finale non può
superare la base. Moduli aggressivi/anti-martingale/optimal-f sono disabilitati;
il contratto richiede abilitazione esplicita, fractional factor e cap. Queste
tecniche possono amplificare perdite e non sono una garanzia finanziaria.

`CreateTradingSessionRequest.instruments` accetta metadata broker autorevoli:
`symbol`, `dollarsPerPoint`, `minimumQuantity`, `quantityStep`, `roundingMode`.
I futures arrotondano per difetto a contratti interi; CFD/cTrader arrotondano al
volume step. Sotto il minimo viene persistito un signal/intent cancellato con
`BelowMinimumQuantity` e non nasce alcun ordine o trade.

ServerSimulated ed ExternalBroker ricevono la stessa `FinalQuantity`. cTrader
non deve scalarla né convertirla nuovamente. `signals.json` schema v2 conserva
base, tre coefficienti, finale e motivo; `trades.json` conserva la quantità
effettivamente filled. L'idempotency key della barra impedisce un secondo sizing
in replay.

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
