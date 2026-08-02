# Motori di generazione delle strategie

Questo documento guida il porting in `Piootoo.Strategies/Easy` dei risultati
prodotti dai motori Unger. La fonte autorevole dei motori resta
`unger/core/engines/ENGINE_REGISTRY`: in caso di differenze prevale il codice
sorgente del motore.

Per verificare una strategia appena portata contro il suo riferimento esterno —
cosa è confrontabile, in che ordine cercare le cause di una divergenza — vedi
[`parita-riferimento-esterno.md`](parita-riferimento-esterno.md).

## Regole comuni

La condizione di ingresso è valutata alla chiusura della barra corrente e
l'ordine può essere eseguito soltanto dalla barra successiva. Gli ordini stop e
limit vengono riemessi a ogni barra valida, hanno durata di una barra e devono
rispettare i fill gap-aware: uno stop long è eseguito a
`max(open, livello)`, mentre un limit richiede la penetrazione stretta del
livello.

Con dati OHLC, l'ordine reale dei tick nella barra non è ricostruibile. Il
motore interno applica quindi una policy intrabar conservativa anche sulla barra
di fill: se sono raggiungibili sia stop loss/trailing/breakeven sia take profit,
chiude allo stop protettivo. Questa regola deve essere replicata da ogni
confronto cross-engine; per una sequenza reale serve un feed a granularità
inferiore o tick.

Per evitare di usare un estremo certamente precedente al fill, l'emulatore
percorre la barra di ingresso con una convenzione fissa: candela rialzista
`Open → Low → High → Close`, candela ribassista `Open → High → Low → Close`.
Un buy stop riempito nel tratto ascendente di una barra rialzista non può quindi
essere chiuso dal suo low; simmetricamente, un sell stop riempito nel tratto
discendente di una barra ribassista non può essere chiuso dal suo high.

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

Chi parte da un run di ottimizzazione esterno (le cartelle `run_*` con
`top_final.json`) trova in [`porting-da-report-sweep.md`](porting-da-report-sweep.md)
la mappa parametro per parametro, le trappole già viste e la procedura per
verificare il porting contro i trade del report.

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
- `offset_ticks = 2` = `0,50` punti NQ, **più un tick di penetrazione**: lo stop
  viene piazzato a `highest(high, N) + offset + 1 tick` perché il livello del
  canale va superato, non toccato. È la convenzione del motore Python, applicata
  dal solo percorso di parità di `PriceChannelEngine` (le `Easy_*` PC usano tutte
  il percorso legacy e non la ereditano);
- `channel_length = 100`, calcolato sulle ultime 100 barre, **inclusa** quella di
  segnale, come `highest(high, 100)` EasyLanguage: alla chiusura i suoi OHLC sono
  noti e l'ordine vale solo dalla barra successiva, quindi non c'è look-ahead;
- solo long;
- nessun filtro volatilità daily;
- `skip_day = -1`;
- `start_hour = 13`, `end_hour = 4` (finestra inclusiva che attraversa la
  mezzanotte);
- `intraday_only = 0`: nessuna chiusura di fine sessione. Il default di
  `PriceChannelEngine` è `IntradayOnly = true`, quindi la sottoclasse deve
  disattivarlo esplicitamente — la classe lo dichiara — perché dimenticarlo mette
  `CloseAtUtc` alle 16:00 su ogni segnale e trasforma il PC in una strategia di
  sessione;
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

## Contratto comune C#

Tutti i dodici motori derivano da `EasyEngineBase`:

- ingresso solo tramite `EntryStopNextBar` / `EntryLimitNextBar` /
  `EntryMarketNextBar` (validità = una sola barra successiva);
- SL, TP, BE e trailing dichiarati in **denaro per contratto di riferimento**;
  la conversione in punti avviene in `PiootooTradingService` via
  `InstrumentRegistry`;
- `MaxEntriesPerSession` + `EntrySessionStartUtc` sul segnale, applicati al
  **fill** (uno stop non eseguito può essere riemesso).

Il registro machine-checkable è
`Piootoo.Strategies.Tests/EngineCatalogMigrationTests.cs`.

## Mappa strategia → motore → sorgente EasyLanguage

Stato: `migrata` = sottoclasse dichiarativa; `ibrida` = motore + override o
gate custom; `esclusa` = `IsPositionCloseDependent` (fuori catalogo).

| Strategia | Motore | Sorgente EL | Stato |
|---|---|---|---|
| Easy_15_EC_5 | BIASW | `s_TOP_UA_15_EC_5__7.txt` | migrata |
| Easy_99_CL_5 | BIASW | `s_TOP_UA_99_CL_5__7.txt` | migrata |
| Easy_100_PL_5 | BIASW | `s_TOP_UA_100_PL_5__7.txt` | migrata |
| Easy_102_FDAX_5 | TrendDeveloper | `s_TOP_UA_102_FDAX_5__7.txt` | migrata |
| Easy_120_CL_15 | BO | `s_TOP_UA_120_CL_15__7.txt` | migrata |
| Easy_123_CL_5 | Aroon (ibrido) | `s_TOP_UA_123_CL_5____120__7.txt` | ibrida — richiede data2 120m |
| Easy_152_NQ_5 | TrendDeveloper | `s_TOP_UA_152_NQ_5__7.txt` | migrata |
| Easy_156_NQ_15 | TF_U | `s_TOP_UA_156_NQ_15__7.txt` | migrata |
| Easy_181_NQ_30 | RBB_U | `s_TOP_UA_181_NQ_30__7.txt` | migrata |
| Easy_195_CL_15 | TrendDeveloper | `s_TOP_UA_195_CL_15____1440__7.txt` | migrata |
| Easy_196_EC_5 | BIASW | `s_TOP_UA_196_EC_5__7.txt` | migrata |
| Easy_218_GC_60 | BIAS | `s_TOP_UA_218_GC_60__7.txt` | migrata |
| Easy_228_FDAX_30 | EasyEngineBase | `s_TOP_UA_228_FDAX_30__7.txt` | ibrida — gap + recross d0 |
| Easy_244_FDAX_15 | BIAS | `s_TOP_UA_244_FDAX_15__7.txt` | ibrida — hook custom |
| Easy_246_CL_5 | TrendDeveloper | `s_TOP_UA_246_CL_5__7.txt` | migrata |
| Easy_261_GC_60 | BIAS | `s_TOP_UA_261_GC_60__7.txt` | migrata |
| Easy_287_GC_5 | BO | `s_TOP_UA_287_GC_5__7.txt` | migrata |
| Easy_291_GC_15 | TrendDeveloper | `s_TOP_UA_291_GC_15__7.txt` | migrata |
| Easy_298_NQ_30 | BO | `s_TOP_UA_298_NQ_30__7.txt` | migrata |
| Easy_303_GC_15 | TrendDeveloper | `s_TOP_UA_303_GC_15____1440__7.txt` | ibrida — ADX/extra gates |
| Easy_32_FDAX_15 | TrendDeveloper | `s_TOP_UA_32_FDAX_15__7.txt` | esclusa — uscite strutturali runtime |
| Easy_336_GC_15 | PC | `s_TOP_UA_336_GC_15__7.txt` | esclusa — Donchian trailing |
| Easy_342_NQ_15 | VBO | `s_TOP_UA_342_NQ_15__7.txt` | migrata |
| Easy_361_FDAX_30 | PC | `s_TOP_UA_361_FDAX_30__7.txt` | migrata |
| Easy_416_GC_30 | RBB_M | `s_TOP_UA_416_GC_30__7.txt` | migrata — EL market vs limit engine |
| Easy_452_BP_15 | BIASW | `s_TOP_UA_452_BP_15__7.txt` | migrata |
| Easy_460_GC_30 | BIAS | `s_TOP_UA_460_GC_30__7.txt` | migrata |
| Easy_486_NQ_15 | VBO legacy | `s_TOP_UA_486_NQ_15__7.txt` | ibrida |
| Easy_506_GC_30 | EasyEngineBase | `s_TOP_UA_506_GC_30__7.txt` | ibrida — range + BB market |
| Easy_515_FDAX_15 | LF | `s_TOP_UA_515_FDAX_15__7.txt` | migrata |
| Easy_531_NQ_60 | EasyEngineBase | `s_TOP_UA_531_NQ_60__7.txt` | ibrida — ingresso a orario fisso |
| Easy_545_HG_15 | BIASW | `s_TOP_UA_545_HG_15__7.txt` | migrata |
| Easy_587_NQ_15 | VBO | `s_TOP_UA_587_NQ_15__7.txt` | ibrida — bande MA±ATR |
| Easy_643_FDAX_60 | VBO legacy | `s_TOP_UA_643_FDAX_60__7.txt` | migrata |
| Easy_653_GC_60 | TrendDeveloper | `s_TOP_UA_653_GC_60__7.txt` | migrata |
| Easy_661_GC_30 | — | `s_TOP_UA_661_GC_30____15__7.txt` | esclusa — uscite `dist` dinamiche |
| Easy_666_GC_5 | VBO legacy | `s_TOP_UA_666_GC_5__7.txt` | migrata |
| Easy_695_GC_5 | TrendDeveloper | `s_TOP_UA_695_GC_5__7.txt` | migrata |
| Easy_772_CL_60 | MAC | `s_TOP_UA_772_CL_60__7.txt` | migrata |
| Easy_796_NQ_15 | TrendDeveloper | `s_TOP_UA_796_NQ_15____1440__7.txt` | migrata |
| Easy_851_GC_5 | TrendDeveloper | `s_TOP_UA_851_GC_5__7.txt` | esclusa — uscite a orario su P&L |
| Easy_872_CL_15 | BIAS | `s_TOP_UA_872_CL_15__7.txt` | ibrida — hook custom |
| Easy_940_GC_15 | LF | `s_TOP_UA_940_GC_15__7.txt` | migrata |
| Easy_956_NQ_15 | EasyEngineBase | `s_TOP_UA_956_NQ_15__7.txt` | ibrida — long stop / short market |
| Easy_960_GC_60 | BIAS | `s_TOP_UA_960_GC_60__7.txt` | migrata |
| PTS_001_NQ_60 | TF_M | (spec PTS, senza `s_TOP_UA_*`) | migrata |
| PTS_002_NQ_15 | PC | (spec PTS) | migrata |
| PTS_003_NQ_15 | PC | (spec PTS) | migrata |

### Motori senza istanza Easy_* nel catalogo attuale

- `RHL`: implementato (`RhlEngine`) e coperto da test di parità; nessuna
  `Easy_*` corrispondente nel set TOP corrente.
- `TrendDeveloper`: famiglia residua per varianti non catalogate nei 12
  template Unger standard; resta disponibile ma non è uno dei dodici.

## Riferimenti codice

- `Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs`
- `Piootoo.Strategies/Easy/Engines/*.cs` (i 12 motori + TrendDeveloper + Aroon)
- `Piootoo.Strategies/Easy/StatelessEasyStrategyBase.cs`
- `Piootoo.Strategies/Easy/EasyLib.cs`
- `Piootoo.Shared/Models/TradeSignal.cs`
- `Piootoo.Core/Services/PiootooTradingService.cs`
- `Piootoo.Core/Services/StrategyFactory.cs`
- `Piootoo.Strategies.Tests/EngineCatalogMigrationTests.cs`
- `piootoo-repository/easy/s_TOP_UA_*`
