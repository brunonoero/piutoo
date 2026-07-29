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
- codice di esecuzione (`Name`): `PTS_001`;
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
Le strategie della serie seguono il formato Id
`PTS_NNN_{SYMBOL}_{TIMEFRAME}` e usano `PTS_NNN` come `Name`, mantenendo così
distinti l'identificatore di catalogo e il codice di esecuzione. GMT coincide
con UTC, quindi la finestra `16:00–03:00` non richiede conversioni. Va ancora
verificato che il calendario di sessione usato per costruire `H_d1` e `L_d1`
coincida con quello del feed Piootoo.

## Riferimenti codice

- `Piootoo.Strategies/Easy/StatelessEasyStrategyBase.cs`
- `Piootoo.Strategies/Easy/EasyLib.cs`
- `Piootoo.Shared/Models/TradeSignal.cs`
- `Piootoo.Core/Services/PiootooTradingService.cs`
- `Piootoo.Core/Services/StrategyFactory.cs`
