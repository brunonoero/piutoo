# Finestra di candele e riscaldamento della sessione

Come un client esterno (cBot cTrader) consegna al server le candele su cui far
girare le strategie. Riguarda solo le sessioni `ExternalBroker`: nel backtest
interno il server legge il datafeed da sé e niente di quanto segue si applica.

Per il ciclo di vita della sessione vedi
[`trading-sessions-api.md`](trading-sessions-api.md); per i cBot vedi
[`cbot-realtime-backtest-titano.md`](cbot-realtime-backtest-titano.md).

---

## 1. Il problema

In una sessione `ExternalBroker` **il server non ha un datafeed proprio**: la
storia di uno stream `(simbolo, timeframe)` è soltanto ciò che il client gli ha
spinto, e vive in RAM per la durata della sessione.

Su quella storia c'è una soglia dura, in `StrategyEvaluationService.Evaluate`:

```csharp
if (history.Count < strategy.RequiredCandles)
    continue;   // salta, senza dire niente
```

`RequiredCandles` non è piccolo. `StrategyContractConformanceTests` impone a ogni
strategia di coprire almeno sei sessioni piene, perché `EasyLib.OHLCMulti5`
ricostruisce `d0..d5` dalla sola finestra ricevuta e riparte da zero a ogni
valutazione: con meno storia i pattern che leggono `d4`/`d5` lavorerebbero su
sessioni troncate. Per una strategia a 15 minuti sono `6 × 96 = 576` barre.

Finché il client mandava una barra per volta, ogni run partiva da storia vuota e
il server scartava in silenzio le prime 576 barre — circa sei giorni di borsa. Un
backtest più corto di quella soglia non produceva **un solo segnale**, senza un
messaggio. E siccome in backtest `ExecutionKey = BT-{istante di avvio}`, ogni run
apre una sessione nuova: il riscaldamento non si eredita mai dal run precedente.

---

## 2. Le regole

### R1 — Quanta storia serve lo dice il server, non il client

Il descriptor di sessione porta
`TradingInstrument.RequiredCandlesByTimeframe`: per ogni timeframe della coppia,
il massimo `RequiredCandles` fra le strategie del masterfilter su quello stream.

Non esiste un parametro locale del cBot che dichiari la stessa cifra, ed è
voluto: sarebbe una seconda verità, e divergerebbe dal masterfilter in silenzio
il giorno in cui si aggiunge una strategia più esigente.

### R2 — Il client carica la storia dal broker prima di partire

cTrader tiene in serie solo le barre che gli servono per il grafico. Il cBot
chiama `Bars.LoadMoreHistory()` su ogni stream finché la serie non copre
`RequiredCandles`, e se non ci arriva lo dice a log invece di procedere in
silenzio.

### R3 — Le candele viaggiano in due tempi

| Momento | Profondità | `EvaluateLastCandle` | Cosa fa il server |
|---|---|---|---|
| Avvio, una volta per stream | `RequiredCandles` (576) | `false` | accoda e basta |
| Ogni barra chiusa | `IncrementalWindowBars` (default 20) | `true` | accoda e **valuta l'ultima candela** |

Il riscaldamento non fa valutare nulla perché le sue barre sono già passate:
valutarle produrrebbe intent sul passato, che il bot eseguirebbe al prezzo di
adesso. Per lo stesso motivo non consuma l'idempotency key e non avanza la
sequence — la stessa barra può tornare più tardi come barra da valutare, e in
quel momento non deve sembrare un replay.

Se il riscaldamento fallisce (server ancora irraggiungibile all'avvio) il client
lo ritenta alla prima barra: senza storia profonda il server non valuterebbe
comunque, quindi procedere sarebbe lavoro a vuoto.

### R4 — Il server accoda solo le candele che non ha

`PushBarWindow` confronta ogni candela con l'ultima già in storia e scarta quelle
non più recenti. È questo che permette al client di rispedire una finestra
sovrapposta a ogni barra senza duplicare niente, e alla prima finestra di un run
di entrare tutta.

### R5 — Si valuta **una sola** candela per finestra: l'ultima

Le precedenti sono contesto. Idempotency key e sequence si riferiscono a
quell'unica barra.

### R6 — La sovrapposizione non è banda sprecata: è ciò che impedisce i buchi

Mandare la sola barra chiusa significherebbe che ogni giro perso — chiamata
fallita, server irraggiungibile per qualche minuto — lascia nella serie del
server un vuoto **permanente**, che nessuno colmerà più, e le strategie
girerebbero su dati bucati senza accorgersene.

Con 20 barre di margine il buco si ricuce da solo fino a 19 barre consecutive
perse. Oltre, si applica R7.

### R7 — Una finestra che non si sovrappone viene rifiutata

Se la storia dello stream non è vuota e la finestra comincia **dopo** l'ultima
candela nota, il server solleva un errore esplicito invece di accodare una serie
discontinua. È lo stesso invariante di "datafeed mancante = errore esplicito".

Il criterio è la sovrapposizione, **non** l'aritmetica sui timestamp: gli stream
hanno buchi legittimi — fine settimana, festivi, mercati chiusi — che un
confronto "ultima barra + timeframe" scambierebbe per barre perse. Vedi
[`orologio-barre-e-fill.md`](orologio-barre-e-fill.md) per cosa costa confondere
un buco di calendario con un buco di feed.

Corollario: una finestra rifiutata non deve lasciare tracce. Tutte le validazioni
girano **prima** di toccare lo stato della sessione, altrimenti il rinvio
corretto verrebbe scambiato per un replay.

### R8 — Le candele non finiscono su disco

`session.History` vive in RAM. `TradingJsonStore` persiste signal, trade e
rotation-log, e nient'altro. Raccogliere il datafeed da cTrader e salvarlo per i
backtest locali è compito di un **cBot raccoglitore dedicato**, non della strada
di esecuzione: mescolare le due cose farebbe dipendere la qualità del datafeed
storico dagli orari in cui è girato un bot di trading.

### R9 — Il silenzio va spiegato

La risposta porta, per stream, `HistoryBars`, `RequiredCandles`,
`EvaluatedStrategies` e `SkippedForInsufficientHistory`; il cBot lo stampa una
volta per stream, con quante barre mancano ancora.

Senza questo "nessuna strategia ha prodotto un segnale" e "il server non ha
abbastanza storia per valutare" sono lo stesso identico silenzio — che è
esattamente il modo in cui il problema di §1 è passato inosservato. È la stessa
ragione per cui il backtest interno ha il blocco `diagnostics` in testa a
`backtest-summary.json` (vedi [`backtesting.md`](backtesting.md)).

---

## 3. Diagnosticare una sessione muta

1. Log del cBot: se compare `il server ha N candele su M richieste`, è
   riscaldamento incompleto. Manca storia sul broker (R2) o il riscaldamento non
   è mai passato (R3).
2. Se compare `Buco nella storia`, il client ha perso più di
   `IncrementalWindowBars` barre di fila (R7). Alzare il parametro non è la cura:
   va capito perché il bot ha smesso di spedire.
3. Se `HistoryBars >= RequiredCandles` e `SkippedForInsufficientHistory = 0`, la
   storia è a posto: il silenzio viene dalle strategie o da Titano. Da lì si
   passa al rotation-log (`GET /{id}/rotation-log`, solo per sessioni collegate a
   un run Titano) e a [`titano-rotation.md`](titano-rotation.md).

---

## Riferimenti codice

- `Piootoo.Core/Services/TradingSessionService.cs` — `PushBarWindow`, `Backfill`,
  `BuildStreamStatus`, `EvaluateClosedBar`; `Describe` per
  `RequiredCandlesByTimeframe`
- `Piootoo.Core/Services/TradingSessionService.cs` —
  `StrategyEvaluationService.Evaluate`, la soglia `RequiredCandles`
- `Piootoo.Shared/Models/Trading/TradingSessionContracts.cs` —
  `ClosedBarWindow`, `PushBarWindowRequest/Response`, `StreamHistoryStatus`,
  `TradingInstrument`
- `PiootooApp.Server/Controllers/TradingSessionsController.cs` —
  `POST /{sessionId}/bars/window`
- `piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs` —
  `LoadHistoryBackwards`, `SendWarmUpWindow`, `SendWindow`, `TryPushClosedBar`,
  `ReportWindowStatus`
- `Piootoo.Strategies.Tests/StrategyContractConformanceTests.cs` — il vincolo
  "almeno sei sessioni" su `RequiredCandles`
