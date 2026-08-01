# Motori di generazione delle strategie

Questo documento guida il porting in `Piootoo.Strategies/Easy` dei risultati
prodotti dai motori Unger. La fonte autorevole dei motori resta
`unger/core/engines/ENGINE_REGISTRY`: in caso di differenze prevale il codice
sorgente del motore.

## Regole comuni

La condizione di ingresso è valutata alla chiusura della barra corrente e
l'ordine può essere eseguito soltanto dalla barra successiva. Gli ordini stop e
limit vengono riemessi a ogni barra valida, hanno durata di una barra e devono
rispettare i fill gap-aware: uno stop long è eseguito a
`max(open, livello)`, mentre un limit richiede la penetrazione stretta del
livello.

Con dati OHLC, l'ordine reale dei tick nella barra non è ricostruibile. Perciò
SL, TP, breakeven e trailing non sono controllati sulla barra di fill: il suo
minimo o massimo potrebbe essere avvenuto prima dell'ingresso. Dalla barra
successiva il motore applica una policy intrabar conservativa: se sono
raggiungibili sia stop loss/trailing/breakeven sia take profit, chiude allo stop
protettivo. Questa regola deve essere replicata da ogni confronto cross-engine;
per una sequenza reale serve un feed a granularità inferiore o tick.

`start_hour` e `end_hour` delimitano la finestra operativa, anche quando
attraversa la mezzanotte; `-1` disabilita il filtro. I giorni usano
`0 = lunedì ... 4 = venerdì` e `-1` significa nessuna esclusione. Stop, target,
trailing e breakeven sono dollari per contratto.

Ogni segnale di ingresso Piootoo deve contenere tutte le uscite note:
`StopLossMoneyPerFutureContract`, `TakeProfitMoneyPerFutureContract`,
`MaxBarsInPosition` e, quando richiesto, `CloseAtUtc`. Il valore `0` disabilita
target e limite barre. Non si devono generare segnali di chiusura barra per barra
per uscite già dichiarabili all'ingresso.

Le sentinelle che disabilitano un filtro sono:

- neutral: `55`/`56`;
- directional: `52`/`53`;
- fast: `152`/`153`;
- uaptnbase: `41`/`42`.

## Catalogo dei motori

- `TF_M`: trend following, stop su estremi d1, neutral + directional mirrored.
- `TF_U`: stesso trigger di `TF_M`, pattern fast indipendenti long/short.
- `BIAS`: ingresso market, stop o limit governato da barre di sessione.
- `BIASW`: ciclo settimanale market, sempre multiday.
- `RBB_M`: reversal su bande di Bollinger, limit e pattern mirrored invertiti.
- `RBB_U`: stesso trigger di `RBB_M`, pattern fast indipendenti.
- `BO`: breakout stop del canale delle ultime N sessioni.
- `LF`: rientro oltre un livello d1, market next-bar, tre librerie pattern.
- `PC`: breakout stop del canale delle ultime N barre.
- `VBO`: breakout stop dall'apertura di sessione in funzione della volatilità.
- `RHL`: reversal limit sugli estremi d1, pattern mirrored invertiti.
- `MAC`: incrocio di medie, market next-bar, senza libreria pattern.

Prima di implementare una strategia bisogna registrare nella specifica almeno:
motore, numero/codice, simbolo, timeframe, parametri completi del motore,
timezone degli orari e convenzione temporale delle barre.

## Porting del motore `TF_M`

`TF_M` usa gli estremi della sessione completa precedente:

```text
neutral = pattern_neutral(neut_yes)
          AND NOT pattern_neutral(neut_no)

long = neutral
       AND pattern_directional(+dir_yes)
       AND NOT pattern_directional(+dir_no)

short = neutral
        AND pattern_directional(-dir_yes)
        AND NOT pattern_directional(-dir_no)
```

Quando la condizione è valida, il long riemette uno stop buy a `H_d1` e lo short
uno stop sell a `L_d1`. L'ordine nasce alla chiusura della barra di valutazione,
vale esclusivamente sulla barra successiva e deve essere rappresentato con
`OrderType.Stop` e `ValidFromUtc` coerente. È ammessa una sola entrata per
sessione e per direzione.

`intraday_only = 1` aggiunge l'uscita a fine sessione; con
`intraday_only = 0` la posizione può restare overnight. Stop, target e limite
barre sono sempre proprietà del segnale d'ingresso.

### Specifica corrente NQ 60

- Id/classe: `PTS_001_NQ_60`;
- codice di esecuzione (`Name`): `PTS_001_NQ_60` (coincide con l'Id: deviazione
  intenzionale dalla convenzione di serie, per un `Name` più leggibile nei
  report; vedi nota sotto);
- motore: `TF_M`;
- simbolo: `@NQ`;
- timeframe: 60 minuti;
- timezone del datafeed e degli orari: GMT/UTC;
- `ptn_neut_yes = 47`;
- `ptn_neut_no = 1`;
- `ptn_dir_yes = 50`;
- `ptn_dir_no = 8`;
- `intraday_only = 0`;
- `skip_day = -1`;
- `start_hour = 16`;
- `end_hour = 3`;
- `stop_loss = 1000`;
- `take_profit = 3000`;
- `max_bars = 0`.

Questa configurazione determina completamente la logica operativa del motore.
Le strategie della serie seguono di norma il formato Id
`PTS_NNN_{SYMBOL}_{TIMEFRAME}` e usano `PTS_NNN` come `Name`, mantenendo così
distinti l'identificatore di catalogo e il codice di esecuzione. `PTS_001_NQ_60`
è l'eccezione: `Name` è stato allineato all'Id per avere un codice di esecuzione
più parlante in `signals.json`/`trades.json`/stati Titano. Chi aggiunge una
nuova variante della serie (es. `PTS_001_GC_60`) deve scegliere esplicitamente
se seguire il `PTS_NNN` condiviso (per aggregare le varianti sotto lo stesso
`Name`) o un `Name` dedicato come qui — non c'è più un default univoco per la
serie. GMT coincide con UTC, quindi la finestra `16:00–03:00` non richiede
conversioni. Va ancora verificato che il calendario di sessione usato per
costruire `H_d1` e `L_d1` coincida con quello del feed Piootoo.

### Specifica corrente NQ 15 — PC

- Id/classe e codice di esecuzione (`Name`): `PTS_002_NQ_15`;
- motore: `PC`;
- simbolo: `@NQ`;
- timeframe: 15 minuti;
- timezone del datafeed e degli orari: GMT/UTC;
- sessione usata dai pattern: CME `17:00–16:00`;
- `ptn_neut_yes = 55` (sentinella, sempre attivo);
- `ptn_neut_no = 24`;
- `ptn_dir_yes = 2` (long);
- `ptn_dir_no = 53` (sentinella, mai escluso);
- `offset_ticks = 2` = `0,50` punti NQ;
- `channel_length = 100`, calcolato sulle 100 barre precedenti ed esclusa la
  barra di segnale;
- solo long;
- nessun filtro volatilità daily;
- `skip_day = -1`;
- `start_hour = 13`, `end_hour = 4` (finestra inclusiva che attraversa la
  mezzanotte);
- `intraday_only = 0`: nessuna chiusura di fine sessione;
- `stop_loss = 250`, `take_profit = 5000`, `trailing_stop = 1000`,
  `breakeven = 1000` USD per contratto;
- `max_entries_per_session = 1`, applicato dall'engine al fill usando l'inizio
  della sessione CME, non al semplice invio dell'ordine;
- `max_bars = 0`.

Il PC riemette lo stop buy sulla barra successiva finché resta flat. Per NQ,
con `$20` per punto, stop, target, trailing e breakeven valgono rispettivamente
`12,5`, `250`, `50` e `50` punti. Il trailing viene dichiarato in denaro dal
segnale e mantenuto come stop protettivo dall'engine interno e dal client live.
Il massimo di un fill per sessione non blocca la ripubblicazione di stop non
eseguiti, ma impedisce un nuovo ingresso dopo una chiusura, inclusa quella
same-bar.

### Specifica corrente NQ 15 — PC variante PTS_003

`PTS_003_NQ_15` usa gli stessi parametri PC di `PTS_002_NQ_15` (canale 100,
offset 2 tick, solo long, 13:00–04:00 UTC, multiday, SL $250, TP $5.000,
BE/trailing $1.000 e un fill per sessione), con una sola variazione intenzionale:
`ptn_dir_no = 6`, quindi l'ingresso long è inibito da `pattern_dir(6)` anziché
dalla sentinella 53. La classe vive in `Piootoo.Strategies/PiutooStrategies/`.

## Riferimenti codice

- `Piootoo.Strategies/Easy/StatelessEasyStrategyBase.cs`
- `Piootoo.Strategies/Easy/EasyLib.cs`
- `Piootoo.Shared/Models/TradeSignal.cs`
- `Piootoo.Core/Services/PiootooTradingService.cs`
- `Piootoo.Core/Services/StrategyFactory.cs`
