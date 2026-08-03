# cBot cTrader: realtime, backtest e Titano

Guida operativa ai tre scenari in cui i cBot Piootoo dialogano con
`PiootooApp.Server` via `POST /api/v1/trading-sessions`. Per il modello generale
del sistema vedi [`../PROGETTO.md`](../PROGETTO.md) (§3.6 modalità Titano, §4.2
sessione live); per piani e `open-plan` vedi [`trading-plans.md`](trading-plans.md);
per la distribuzione multi-account vedi
[`distribuzione-multi-account.md`](distribuzione-multi-account.md).

---

## 1. Ruoli: cBot vs server

Il **server** possiede strategie, masterfilter, Titano, sizing e stato di sessione.
Valuta le strategie a ogni barra, applica il filtro rotazione quando configurato,
dimensiona gli intent e li persiste. Non assume mai un fill: decide *cosa* ordinare.

Il **cBot** è l'engine di esecuzione esterno su cTrader. Apre o riprende la sessione
con `open-plan`, invia barre chiuse con `POST /{sessionId}/bars`, esegue gli intent
restituiti (o li reclama, nel bot multi-account) e riporta i fill con execution
report. Applica in locale le uscite (SL/TP nativi, trailing, breakeven, uscita a
tempo, limite barre) e registra le chiusure con `intents/close-external`. Decide
*se e a che prezzo* il broker esegue.

Esistono due cBot:

| cBot | Modello | Quando usarlo |
|---|---|---|
| `PiootooTradingSessionBot` | Esecuzione diretta: `POST /bars` restituisce intent già assegnati (`DistributeToAccounts=false`) | Un account, un grafico (simbolo + timeframe), backtest o live su quello stream |
| `PiootooLiveTradingBot` | Distribuzione: template da `POST /bars`, claim con `POST /accounts/{n}/signal` (`DistributeToAccounts=true`, default del piano) | Più account/gruppi sullo stesso piano, concorrenza e anti copy-trading |

Il cBot **non interpreta Titano**: riceve solo intent già filtrati e dimensionati.
L'universo di strategie valutate è sempre il **masterfilter del workspace** del
piano — il piano non restringe quali strategie girano, ma decide Titano, gruppi,
sizing e capitale (vedi [`trading-plans.md`](trading-plans.md)).

---

## 2. Prerequisiti comuni

1. **Server attivo** — `dotnet run --project PiootooApp.Server` (default
   `http://localhost:5142`). Il parametro `API Base Url` del cBot deve puntare qui.
2. **Workspace** con `masterfilter.json` non vuoto e strategie presenti nel
   catalogo. ID non validi o masterfilter vuoto → `400` alla creazione sessione.
3. **Piano di trading** in `<workspace>/plans/plans.json` con codice globale,
   capitale, commissioni, sizing, metadata strumenti e — se serve Titano — run e
   cartella backtest sulla riga gruppo/account. CRUD da shell
   *Anagrafiche → Piani di trading* o API
   `GET/PUT /api/v1/workspaces/{id}/trading-plans`.
4. **Account cTrader** registrati nel workspace (`WorkspaceAccount`) con eventuale
   tabella di conversione symbol (vedi
   [`account-e-conversione-symbol.md`](account-e-conversione-symbol.md)).
5. **Grafico coerente** — il simbolo e il timeframe del grafico cTrader devono
   corrispondere a una coppia coperta dal masterfilter; altrimenti il server
   accetta barre ma non valuta strategie. `PiootooTradingSessionBot` verifica
   all'avvio e si ferma; `PiootooLiveTradingBot` legge tutte le coppie dal
   descriptor e invia barre da serie native cTrader.
6. **Fuso UTC** — tutti i cBot dichiarano `[Robot(TimeZone = TimeZones.UTC)]`.
   Gli orari di barra inviati al server devono essere UTC (`Z`); vedi
   [`decisioni.md`](../decisioni.md) (2026-08-02) sul rifiuto delle barre senza
   suffisso.

---

## 3. Scenario A — Backtesting puro (campione Titano)

**Obiettivo:** eseguire tutte le strategie del masterfilter su dati storici cTrader
e produrre `trades.json` utilizzabile da Titano offline. Nessun filtro rotazione.

| Parametro | Valore |
|---|---|
| `ClientRunMode` | `Backtest` (da `Robot.IsBacktesting` nel cBot) |
| `TitanoFilterMode` | `Disabled` |
| Piano | `ApplyTitanoFilters = false`; run Titano non richiesti |
| `EnforceConcurrencyLimits` | default `false` — il run campione non va limitato |

### Passi

1. Configura il piano con workspace, capitale, sizing e metadata; lascia Titano
   disattivato.
2. In cTrader: abilita **backtesting** sul cBot, imposta `Codice piano`, lascia
   `Execution Key` vuota (viene derivata dall'istante di avvio:
   `BT-yyyyMMddHHmmss`) o fissala per riprendere lo stesso run.
3. Attacca il cBot a un grafico con simbolo e timeframe presenti nel masterfilter.
   Per più coppie servono più istanze del bot (una per stream) oppure
   `PiootooLiveTradingBot` con backtesting multi-simbolo abilitato in cTrader.
4. All'avvio: `POST /trading-sessions/open-plan` → il server crea o riprende la
   sessione, restituisce strumenti e token.
5. A ogni barra chiusa: push barra → intent di ingresso → esecuzione → report.
   Alla chiusura posizione nasce un `PersistedTrade` in
   `GET /{sessionId}/trades`.

### Quando usarlo

- Produrre il **campione sorgente** (`trades.json`) su cui Titano calcola il
  manifest offline (passo successivo dello scenario B).
- Confrontare l'engine esterno con il backtest interno sui trade realmente
  eseguiti dal broker simulato cTrader.
- **Non** simula ancora l'effetto del filtro rotazione: per quello serve lo
  scenario B.

Alternativa senza cBot: backtest interno da console (*Backtesting* → run con
`TitanoMode = Disabled`). Produce gli stessi artefatti sotto
`<workspace>/backtests/<nome>/` (vedi [`backtesting.md`](backtesting.md)).

---

## 4. Scenario B — Backtest con Titano offline

**Obiettivo:** replay storico cTrader in cui, **per ogni barra**, valgono solo le
strategie abilitate dal periodo di rotazione del manifest calcolato offline.
Simula come avrebbe operato il portafoglio filtrato, inclusi sizing Titano e —
con `PiootooLiveTradingBot` — concorrenza per account/gruppo.

| Parametro | Valore |
|---|---|
| `ClientRunMode` | `Backtest` |
| `TitanoFilterMode` | `BacktestRotationFile` |
| Piano | `ApplyTitanoFilters = true`, `TitanoRunId` + `TitanoBacktestFolder` sulla riga |
| `EnforceConcurrencyLimits` | default `true` (tranne run campione Disabled) |

### Catena preparatoria (console)

1. **Backtest campione** (scenario A o backtest interno `Disabled`) →
   `<workspace>/backtests/<nome>/trades.json`.
2. **Titano rotazioni** — esegui una rotazione sul backtest (`POST
   /api/Titano/rotations` o tab *Titano* della console legacy, *Genera e applica
   Titano*). Output in
   `<workspace>/backtests/<nome>/titano/<run-id>/manifest.json`. Dettaglio in
   [`titano-rotation.md`](titano-rotation.md).
3. **Piano di trading** — imposta `ApplyTitanoFilters`, collega `TitanoRunId` e
   `TitanoBacktestFolder`, configura righe gruppo/account con
   `MaxConcurrentTrades` se usi la distribuzione. Con
   `PiootooTradingSessionBot` azzera `MaxConcurrentTrades` o disattiva
   `EnforceConcurrencyLimits`: in esecuzione diretta il limite non è applicabile e
   l'apertura viene rifiutata (vedi [`trading-plans.md`](trading-plans.md)).
4. **Sessioni** — shell *Sessioni di trading* → *Apri da piano*, contesto
   `Backtest`, verifica l'hint di scenario. Oppure avvia direttamente il cBot con
   lo stesso piano in backtesting cTrader.

### Comportamento runtime

- Il server risolve `EffectiveStrategies(barTimeUtc)` dal manifest; una barra fuori
  da ogni periodo **ferma il run** con errore esplicito (non degrada a "nessun
  filtro").
- Gli intent scartati (allocazione zero, sizing sotto minimo) arrivano comunque
  con `Status ≠ Pending`: il cBot non deve eseguirli.
- Con **`PiootooLiveTradingBot`**: `MaxConcurrentTrades` e ordine di claim per
  gruppo/account seguono [`distribuzione-multi-account.md`](distribuzione-multi-account.md).
- **`rotation-log`** (`GET /{sessionId}/rotation-log`) documenta per barra quali
  strategie erano incluse e perché.

### Quando usarlo

- Validare end-to-end il portafoglio Titano sull'engine cTrader prima del live.
- Misurare l'impatto di concorrenza e distribuzione multi-account in backtest
  esterno (solo con `PiootooLiveTradingBot`).

---

## 5. Scenario C — Realtime (trading live)

**Obiettivo:** operare sul mercato reale applicando il **periodo corrente**
dell'ultima analisi Titano. Oltre la fine del manifest resta in vigore l'ultimo
periodo calcolato (`UsedLatestPeriod` nel rotation-log).

| Parametro | Valore |
|---|---|
| `ClientRunMode` | `Realtime` (`Robot.IsBacktesting = false`) |
| `TitanoFilterMode` | `Realtime` (se `ApplyTitanoFilters = true`) oppure `Disabled` |
| Piano | run Titano aggiornato periodicamente; righe gruppo/account per multi-account |
| `Execution Key` | vuota → `"LIVE"` (riavvio idempotente della stessa esecuzione live) |

### Passi

1. Mantieni un manifest Titano aggiornato (nuovo backtest campione + rotazione
   quando serve ricalibrare).
2. Piano con `ApplyTitanoFilters = true`, `TitanoRunId`, cartella backtest e
   gruppi/account configurati.
3. **Un account, un grafico:** `PiootooTradingSessionBot` su cTrader live,
   `Codice piano`, grafico allineato al masterfilter.
4. **Più account:** un'istanza di `PiootooLiveTradingBot` per account cTrader,
   stesso `Codice piano`; il server condivide la sessione e distribuisce i
   template con anti copy-trading per gruppo.
5. Il cBot invia barre chiuse; in live cTrader le fornisce il mercato. In
   alternativa un **feed worker** (`Piootoo.FeedWorker` o cBot dedicato) può
   alimentare sessioni `ServerSimulated` — vedi [`feed-worker.md`](feed-worker.md)
   (bozza).

### Note operative

- **`FlatAtWeekEnd`** (default attivo): regola di sicurezza nel cBot, non nel
  server — chiude posizioni e ordini nel weekend anche se il server è
  irraggiungibile (vedi [`../PROGETTO.md`](../PROGETTO.md) §3.5).
- **Ripresa:** stessa tripla `(PlanCode, ClientRunMode, ExecutionKey)` (+ account
  in esecuzione diretta) riprende la sessione in RAM del server. Riavvio processo
  server = perdita stato runtime (limite noto in [`trading-plans.md`](trading-plans.md)).
- **`PiootooLiveTradingBot`** persiste lo stato uscite in
  `%AppData%/PiootooLiveTradingBot/state-{planCode}-{account}.json`.

---

## 6. Tabella `ClientRunMode` × `TitanoFilterMode`

Validazione in `TradingSessionService.RequireCoherentRunMode` alla **creazione**
sessione. `ClientRunMode.Unknown` (console manuale, test) salta il controllo.

| | `Disabled` | `BacktestRotationFile` | `Realtime` |
|---|---|---|---|
| **`Backtest`** | ✅ Ammesso. Tutte le strategie del masterfilter. Run campione Titano. | ✅ Ammesso. Filtro per barra dal manifest offline. | ❌ **Rifiutato** — look-ahead: il periodo "corrente" su barre storiche userebbe informazione futura; oltre la fine manifest resterebbe congelato sull'ultimo periodo. |
| **`Realtime`** | ✅ Ammesso. Tutte le strategie, nessun filtro rotazione. | ❌ **Rifiutato** — il manifest copre l'intervallo del backtest sorgente; il tempo live ne esce subito e la sessione si fermerebbe alla prima barra scoperta. | ✅ Ammesso. Periodo corrente dell'ultima analisi; oltre manifest → ultimo periodo. |
| **`Unknown`** | ✅ (nessuna verifica incrociata) | ✅ | ✅ |

Il cBot legge il contesto da **`Robot.IsBacktesting`**, non da un parametro
manuale: `Backtest` ↔ backtesting cTrader, `Realtime` ↔ mercato reale. Se riprende
uno stato locale con contesto diverso, scarta la sessione salvata e ne crea una
nuova (caso tipico: rilanciare in backtest un bot che aveva lasciato sessione live).

Con **`open-plan`**, la modalità Titano deriva automaticamente dal piano e dal
contesto:

- `ApplyTitanoFilters = false` → `Disabled`;
- `ApplyTitanoFilters = true` + `Backtest` → `BacktestRotationFile`;
- `ApplyTitanoFilters = true` + `Realtime` → `Realtime`.

---

## 7. Parametri cBot (`PiootooTradingSessionBot`)

Parametri rimasti dopo la migrazione al piano (vedi [`decisioni.md`](../decisioni.md)
2026-08-03):

| Parametro | Ruolo |
|---|---|
| **API Base Url** | Radice HTTP del server (es. `http://localhost:5142`) |
| **Codice piano** | Unico identificatore operativo; risolve workspace, Titano, sizing, capitale |
| **Account Number Override** | `0` = account del grafico cTrader; altrimenti forza l'account (deve essere nel piano) |
| **Execution Key** | Distingue esecuzioni dello stesso piano. Vuota: `LIVE` in realtime, `BT-{timestamp}` in backtest |
| **Http Timeout (s)** | Timeout chiamate HTTP |
| **Persist Stato Locale** | Salva ancora sessione ed equity del pannello su disco |
| **Volume Per Quantity Unit** | Conversione quantità dominio Piootoo → volume broker cTrader |
| **Flat nel fine settimana** / **Flat da venerdì** / **Operativo da domenica** | Regola weekend lato bot |

**Non parametri del cBot** (vivono nel piano o nella piattaforma):

- Workspace, capitale, commissioni, sizing, metadata strumenti
- `TitanoRunId`, cartella backtest, `ApplyTitanoFilters`
- `ClientRunMode` — derivato da **`IsBacktesting`** (flag backtesting cTrader)
- Strategie valutate — dal **masterfilter** del workspace del piano

`PiootooLiveTradingBot` aggiunge timeframe base del grafico, intervallo polling
segnali, slippage e finestra storica; stesso `Codice piano` e stesso modello
`open-plan` con `DistributeToAccounts = true` (default).

---

## 8. Troubleshooting

| Sintomo | Causa probabile | Cosa fare |
|---|---|---|
| `400` masterfilter vuoto | Nessuna strategia nel workspace del piano | Popola `masterfilter.json` (shell *Workspace* o API) |
| `400` ID strategia non validi | Id di classe assenti dal catalogo | Allinea masterfilter al catalogo (`StrategyFactory`) |
| `400` TitanoRunId mancante con filtro attivo | Piano con `ApplyTitanoFilters` senza run | Esegui rotazione Titano e collega run + cartella backtest |
| `400` combinazione Backtest + Realtime Titano | Matrice §6 violata | Allinea modalità: backtest filtrato → `BacktestRotationFile`; live → `Realtime` |
| `400` barra senza `Z` / Kind non UTC | Timestamp barra mal serializzato | Verifica `[Robot(TimeZone = UTC)]` e `SpecifyKind` prima del push |
| `401` / token sessione | `X-Session-Token` errato o sessione inesistente | Riapri con `open-plan`; non riusare token di sessione diversa |
| Bot attivo, zero segnali | Grafico su simbolo/timeframe non nel masterfilter | Cambia grafico o estendi masterfilter; controlla conversione symbol account |
| `400` MaxConcurrentTrades con session bot | Piano con limite in esecuzione diretta | Azzera limite, disattiva `EnforceConcurrencyLimits`, o usa `PiootooLiveTradingBot` |
| Run Titano si ferma a metà backtest filtrato | Barra fuori periodi manifest | Estendi il backtest sorgente o accorcia l'intervallo cTrader |
| Numeri diversi dal backtest interno | Trailing/breakeven non applicati, intent scartati eseguiti, ordini pending accumulati | Verifica versione cBot ≥ 1.3.0; controlla `Status` e `FinalQuantity`; vedi [`decisioni.md`](../decisioni.md) 2026-08-02 |
| Riavvio server, sessione persa | Sessioni residenti in RAM | Riapri cBot (idempotente se server ancora vivo); persistenza post-riavvio server è lavoro aperto |

Per diagnosi fine sessione: `rotation-log`, `GET /trades`, confronto con
`backtest-summary.json` del run interno analogo.

---

## Riferimenti codice

- `piootoo-repository/ctrader/PiootooTradingSessionBot.cs` — esecuzione diretta,
  `open-plan`, push barre, gestione uscite
- `piootoo-repository/ctrader/PiootooLiveTradingBot.cs` — distribuzione
  multi-account, polling segnali
- `PiootooApp.Server/Controllers/TradingSessionsController.cs` — endpoint sessioni
  e `open-plan`
- `Piootoo.Core/Services/TradingSessionService.cs` — `OpenFromPlan`,
  `RequireCoherentRunMode`, `PushBars`, claim multi-account
- `Piootoo.Core/Services/TradingPlanService.cs` — persistenza piani
- `Piootoo.Shared/Models/Trading/TradingPlanContracts.cs` — modelli piano e
  `OpenTradingPlanSessionRequest`
- `piootooapp.clientform/Shell/Screens/TradingSessionsScreen.cs` — UI tre scenari,
  validazione incrociata, apertura da piano
- `piootooapp.clientform/Shell/Api/TradingSessionApiClient.cs` — client HTTP shell
