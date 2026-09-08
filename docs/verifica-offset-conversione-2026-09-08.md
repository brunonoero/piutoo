# Verifica degli offset di conversione EasyLanguage → Python → C# (08/09/2026)

Confronto a tre fra `piootoo-repository/easy/` (le sorgenti EasyLanguage
originali), `piootoo-repository/easy_engine_py/` (i dodici motori Python della
ricerca) e gli engine C# di `Piootoo.Strategies/Easy/`, per rispondere a una sola
domanda: **resta qualche offset di conversione non compensato?**

Sì, sei — di cui uno misurabile subito sul feed che gira oggi. Sono elencati al §3
in ordine di impatto. Il §2 dice cosa è invece già chiuso, e il §4 cosa *sembra* un
offset ma non lo è.

## 0. Cosa è confrontabile e cosa no

Del pacchetto Python il repository contiene **solo i motori**: `time_window`,
`day_filter`, il caricamento del dataframe, il `resample` e le colonne di sessione
(`sess_id`, `bar_num`, `H_d1`…) stanno nel package genitore, che **non è sul
disco**. Per quelle regole questa verifica si appoggia alle misure già registrate
in [`domini/porting-da-report-sweep.md`](domini/porting-da-report-sweep.md)
(l'allineamento a −15 minuti fra `entry_time` del report e i nostri
`entryTimeUtc`) e nel docstring di `aggregate_flat_feed.py` (le 201 righe alle
`00:01` contro 4 alle `00:00`). Tutto il resto è letto direttamente sul sorgente.

Le tre convenzioni, messe una accanto all'altra:

| | EasyLanguage | motore Python | Piootoo C# |
|---|---|---|---|
| fuso dei timestamp | ora di **borsa** dello strumento | ora della **ricerca**, CET per ogni simbolo | **UTC vero**, convertito al confronto da `SessionClock` |
| etichetta della barra | **chiusura** (`isBarTimeEndTime = true`) | **chiusura** (`inizio + Δ`) | **apertura** (`[t, t+Δ)`) |
| confine di sessione | finestra di borsa, le barre fuori orario non stanno in nessuna sessione | `(t − 1 min − h).normalize()`, giorno di calendario europeo, ogni barra in una sessione | `SessionGrid.SessionDayOf`, ancoraggio `h` in `researchTz` |
| finestra operativa | `t >= StartTrade and t < EndTrade` (fine **esclusa**) | `min >= start && min <= end` (fine **inclusa**) | `TimeWindowInclusive` per le PTS, `TimeWindow` per il percorso EL |
| giorno della settimana | `dayofweek()`, 0 = domenica | `index.dayofweek`, 0 = lunedì | `EasyDayOfWeek` / `PythonWeekday`, entrambi presenti |

**Il punto che genera tutti gli offset residui è la riga "etichetta".** Loro
etichettano alla chiusura, noi all'apertura. Finché si confrontano *barre fisiche*
non cambia nulla; cambia quando un **parametro con un orario di parete** — una
finestra, uno `skip_day`, un `le_time` — viene confrontato con l'etichetta.

## 1. Le tre regole EasyLanguage che contano, e dove sono finite

`f__OHLCMulti5` dichiara `isBarTimeEndTime = true` e ne deriva
`timeStarted = t > StartTime`, `timeNotEnded = t <= EndTime`: è il confronto
stretto che compensa l'etichetta di chiusura. `EasyLib.ClassifySessionBar` lo
riproduce per le sessioni di borsa e lo **sostituisce** con `>= ancoraggio` per le
sessioni a giornata piena, che è la forma corretta su etichette di apertura.

`f_tw` ha la fine **esclusa**; il motore Python la ha **inclusa**. Sono due regole
diverse e in C# convivono due funzioni distinte: le `PTS_*` passano da
`TimeWindowInclusive`, il percorso EL storico da `TimeWindow`. Nessuna PTS usa il
secondo.

`f_twBars` (finestra in barre, non in orari) non ha problemi di etichetta:
`BiasBarCountEngine` conta barre di sessione, e il conteggio coincide per
costruzione una volta che il confine di sessione coincide.

## 2. Gli offset già chiusi — da non riaprire

- **Il fuso dei timestamp.** `datafeed/feed-clocks.json` dichiara l'orologio di
  ogni feed (`Europe/Rome` per il vendor) e `DataSourceRepository` converte a UTC
  una volta sola al caricamento.
- **L'etichetta nella generazione del feed.** `aggregate_flat_feed.py` fa
  `floor(t − 1 min)` e scrive l'apertura del bucket.
- **L'etichetta nel confine di sessione.** `ClassifySessionBar` e
  `SessionGrid.SessionDayOf`, con la dimostrazione nel commento di `BucketStartUtc`
  sul perché lì il "meno un minuto" non compare.
- **L'ancoraggio per simbolo.** `market-calendars.json` è la sola tabella; i tre
  cBot e `aggregate_flat_feed.py` ne tengono copie che concordano. HK compare solo
  in un commento: è stato ritirato dal paniere il 07/09/2026, e `CLAUDE.md` lo cita
  ancora fra i simboli ad ancoraggio 1.
- **La finestra operativa non si converte a mano.** `ZonedWindow` porta
  l'orologio, e il sentinella `-1` è reso come `2359`, non come `2300`.
- **Lo spread per ora.** `PiootooSpreadDumpBot` gira con
  `[Robot(TimeZone = TimeZones.UTC)]` e verifica a runtime di essere davvero in
  UTC; `PiootooTradingService` indicizza con `entryTimeUtc.Hour`. Stessa unità.

## 3. Gli offset ancora aperti

### 3.1 `@FDAX_240.json` sta su un ancoraggio che il calendario non dichiara più *(misurato)*

Il calendario dà a FDAX `sessionStartHour: 1`, quindi i bucket 4h devono cadere
alle 01, 05, 09, 13, 17, 21 locali. Le barre sul disco cadono alle **00, 04, 08,
12, 16, 20**: tutte e 20.795, senza eccezioni.

Il file è stato generato il **25/08/2026**; `SESSION_START_HOUR` è entrato in
`aggregate_flat_feed.py` il **07/09/2026** (commit `31fab8e`) e il feed non è mai
stato rigenerato. Conseguenza: le quattro `PTS_FDAX_*_240` girano su barre spostate
di un'ora rispetto alla griglia su cui sono state trovate, e `SessionDayOf` taglia
la sessione alle 01:00 dentro un bucket che comincia alle 00:00 — quindi anche
`d0..d5` e l'uscita di fine sessione stanno su un confine che nessuna barra ha.

Non lo vede nessun test: `BarAggregatorMetroTests.CollectedAggregatesSitOnTheirOwnGrid`
guarda solo `datafeed-external/{BROKER}`, mai `datafeed/`. La misura del §7quater di
[`domini/layer-barre-e-calendario.md`](domini/layer-barre-e-calendario.md) — "solo i
giornalieri, 18-19 barre su ~5.000" — è stata fatta prima che il calendario
dichiarasse l'ancoraggio 1, e oggi non è più vera.

**Rimedio**: rigenerare i feed dei simboli ad ancoraggio ≠ 0 con lo script attuale,
ed estendere il metro a `datafeed/`. Fra i feed interni l'unico interessato è FDAX:
CC, CT, KC e SB non hanno feed interno.

### 3.2 `skip_day` è letto sull'apertura, mai compensato *(misurato)*

`day_filter` del Python confronta `index.dayofweek`, cioè il giorno dell'etichetta
di **chiusura**; `EasyEngineBase.PythonWeekday` legge il giorno dell'**apertura**.
`WindowInstant` non passa di qui — l'esclusione è deliberata e documentata, ma non
era mai stata misurata.

Barre classificate in modo diverso con `skip_day = 4`:

| feed | barre | nostro venerdì | venerdì del Python | discordanti |
|---|---|---|---|---|
| `@NQ_240` | 29.922 | 5.886 | 5.909 | 1.939 (6,5%) |
| `@FDAX_240` | 20.795 | 4.101 | 4.122 | 1.755 (8,4%) |
| `@NG_240` | 23.798 | 4.657 | 4.681 | 1.548 (6,5%) |
| `@NQ_60` | 116.424 | 22.647 | 23.030 | 649 (0,6%) |
| `@NQ_15` | 455.210 | 88.510 | 88.574 | 64 (0,0%) |

È un bucket su sei a 4h: non saltiamo l'ultimo bucket del giovedì (che la ricerca
salta) e saltiamo l'ultimo del venerdì (che la ricerca non salta). Tredici strategie
dichiarano `SkipDay = 4`, e **otto sono a 240 minuti**: `PTS_FDAX_PCH_001_240`,
`PTS_FDAX_SBO_001_240`, `PTS_FDAX_VBO_001_240`, `PTS_CC_PCH_001_240`,
`PTS_NG_TFU_001_240`, `PTS_NG_TFU_002_240`, `PTS_NQ_TFM_014_240`,
`PTS_NQ_VBO_002_240`. Le altre cinque sono a 15 e 60 minuti, dove l'effetto è
marginale ma non nullo.

Nessuna strategia a 1440 dichiara `skip_day`, ed è una fortuna: lì lo scarto
sarebbe un giorno intero.

### 3.3 La finestra operativa è compensata solo dietro un interruttore, e solo nel backtest

`BacktestingRequest.ResearchWindowOnBarClose` è **`false` per default**, quindi il
comportamento normale resta quello disallineato di una barra (345 corrispondenze
contro 78 a offset nullo su NQ 15m, §"Trappole verificate" del porting).

Il difetto in più rispetto a quanto già documentato: `WindowsOnBarClose` viene
assegnato **solo** in `PiootooBacktestingService`. Il percorso di sessione live
(`StrategyEvaluationService`) non lo tocca, quindi un run acceso e la sessione dello
stesso piano valutano la finestra su etichette diverse — cioè eseguono due strategie
diverse, che è esattamente ciò che `PlanCode` esiste per impedire.

### 3.4 BIASW: `le_time`/`lx_time` sono confrontati con l'apertura

`bias_weekly.py` fa `_time_match(df, le_time)` con `minutes == target` sull'etichetta
di chiusura e riempie a `df["open"]` della stessa barra: il fill cade quindi a
**`le_time − Δ`** di orologio. `BiasWeeklyEngine.IsInScheduledEntry` confronta
`Hhmm(barTime)` sull'apertura, senza `WindowInstant`, e riempie all'apertura di
*quella* barra: il fill cade a `le_time`.

Ingressi e uscite delle quattro `PTS_*_BSW_*` sono quindi in ritardo di una barra
(15 o 60 minuti). `ResolveScheduledExitUtc` ha lo stesso scarto sull'uscita, che è
un istante secco e non una finestra. E c'è un caso degenere da tenere presente per i
porting futuri: con `le_time = 0000` non scivola l'ora ma il **giorno**, perché la
barra che chiude a mezzanotte apre il giorno prima.

### 3.5 `MAC`: l'uscita del venerdì è irraggiungibile *(verificato con il test)*

`MovingAverageCrossoverEngine.IsFridaySessionEnd` chiede
`Hhmm(barEnd) == SessionEndTime`, e `SessionEndTime` di una sessione della ricerca
vale `2359` — che è una **sentinella**, non un orario, come `ResolveCloseAtUtc`
dichiara esplicitamente dieci righe più in là. Nessun `barEnd` di una griglia a 30 o
240 minuti vale `23:59`, quindi il ramo non scatta mai:

```
Failed! - Failed: 1, Passed: 3
MovingAverageCrossoverEngineTests.FridaySessionEnd_EmitsImmediateExitOnly [FAIL]
```

È uno dei rossi preesistenti. Il `friday_eod` del Python (`last_bar & dayofweek == 4`)
chiude le due `PTS_*_MAC_*`, che tengono overnight: senza, restano aperte nel fine
settimana e le chiude solo il flat di conto, con orari che il riferimento non ha.

Quando lo si ripara va riparato **con l'etichetta giusta**: `last_bar` del Python è
la barra la cui *chiusura* cade di venerdì, quindi comprende anche l'ultima barra
della sessione di giovedì (etichettata venerdì 00:00). La forma naturale è passare da
`SessionGrid` — `barEnd >= SessionOpenUtc(SessionDayOf(barTime) + 1)` — invece di
confrontare due `HHMM`.

### 3.6 Tre copie di "inizio sessione", e due non arretrano

La chiave di `MaxEntriesPerSession` è calcolata in quattro punti diversi, con due
comportamenti:

- `GetSessionStartUtc` (TF, RBB, BIASW) arretra sempre quando `timeUtc < sessionStart`: **corretto**.
- `SessionKey` (PCH, SBO, VBO) e `EasyEngineBase.ResolveEntrySessionStartUtc` (RHL)
  arretrano **solo se `SessionStartTime > SessionEndTime`**, che per una sessione
  della ricerca (`h*100` → `2359`) non è mai vero.

Su un simbolo ad ancoraggio 0 non cambia niente. Su uno ad ancoraggio 1 ogni istante
fra le 00:00 e le 01:00 locali viene attribuito alla sessione **successiva**:
l'ingresso delle 00:00 consuma l'unico fill della sessione che deve ancora
cominciare. Oggi tocca `PTS_CC_SBO_001_60` (a 240 minuti nessuna barra apre in quella
fascia); domani tocca qualunque PCH/SBO/VBO/RHL su CC, CT, KC, SB o FDAX sotto le 4h.
È lo stesso difetto che `ResolveCloseAtUtc` ha già chiuso passando da `SessionGrid`,
non ancora propagato alle altre tre copie.

### 3.7 Latenti — nessuna strategia li tocca oggi

- **`MaxDaysInTrade`** compone la deadline con `Clock.SessionInstantUtc(…, 0)`, cioè
  la **mezzanotte** locale invece dell'ancoraggio: su un simbolo ad ancoraggio 1 cade
  un'ora prima e nella sessione sbagliata. Nessuna `PTS_*` lo usa.
- **`ReversalBollingerBandEngines`** filtra il giorno con `EasyDayOfWeek`
  (0 = domenica) mentre `PTS_NQ_RBM_001_15` scrive in `DayToFilter` uno `skip_day`
  pandas (0 = lunedì). Vale `-1`, quindi è inerte; con un `4` sarebbe giovedì invece
  di venerdì. Sono gli unici motori derivati dal Python che non usano `PythonWeekday`.
- **19 barre giornaliere per simbolo** etichettate `01:00` invece di `00:00`
  (l'ultima domenica di ottobre): già noto, si chiude al Passo 8.

## 4. Cosa *non* è un offset

- **Gli interi `1700` delle sorgenti EasyLanguage.** Non sono un ancoraggio scritto
  in un altro fuso: sono un modello di sessione diverso. Nessuna aritmetica porta da
  `1700` a `sessionStartHour`, e nessuna `PTS_*` usa `InstrumentClock.Exchange`.
- **La fine esclusa di `f_tw` contro quella inclusa del Python.** Non è un errore di
  conversione: sono due motori diversi, e in C# ci sono due funzioni.
- **I bucket sotto l'ora.** Con fusi a ore piene la griglia locale e quella UTC sono
  lo stesso insieme di confini: `SessionGrid` calcola in UTC apposta, e sopra l'ora
  passa per l'orario locale.
- **Che i nostri `entryTimeUtc` siano 15 minuti "prima" di quelli del report.** È la
  stessa barra fisica letta con due etichette; l'offset da correggere sta nel
  confronto, non nell'esecuzione.

## 5. Riferimenti codice

- `piootoo-repository/easy/f__OHLCMulti5__0.txt`, `f_tw__0.txt`, `f_twBars__0.txt` — le tre regole originali.
- `piootoo-repository/easy_engine_py/bias_weekly.py`, `ma_crossover.py`, `breakout.py` — i punti in cui il Python legge un orario.
- `Piootoo.Strategies/Easy/EasyLib.cs` — `ClassifySessionBar`, `FullDaySessionDay`, `TimeWindow`, `TimeWindowInclusive`.
- `Piootoo.Shared/MarketData/SessionGrid.cs` — `SessionDayOf`, `BucketStartUtc`.
- `Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs` — `WindowInstant`, `PythonWeekday`, `ResolveEntrySessionStartUtc`, `ResolveCloseAtUtc`.
- `piootoo-repository/datafeed-future/aggregate_flat_feed.py` — `SESSION_START_HOUR`, `minutes_into_bucket`.
- [`domini/porting-da-report-sweep.md`](domini/porting-da-report-sweep.md), [`domini/orari-di-sessione-e-fusi.md`](domini/orari-di-sessione-e-fusi.md), [`domini/layer-barre-e-calendario.md`](domini/layer-barre-e-calendario.md).
