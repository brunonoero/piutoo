# Portare una strategia da un report di sweep

I candidati della serie PTS nascono da un run di ottimizzazione esterno (cartelle
`D:\Piootoo\davide\run_YYYYMMDD_HHMM\`) che esplora i dodici motori Unger su un
mercato e produce una classifica. Questo documento descrive come si legge quel
run, come si traducono i suoi parametri in una sottoclasse C#, e quali trappole
hanno già fatto divergere un porting dalla sua fonte. La descrizione dei motori e
delle regole comuni sta in [`motori-strategie.md`](motori-strategie.md); come si
indaga una divergenza già avvenuta sta in
[`parita-riferimento-esterno.md`](parita-riferimento-esterno.md).

## Dove sta la verità del run

`report.html` è una vista: comoda da leggere, inutile da citare. La fonte
autorevole è **`top_final.json`**, che contiene un blocco `metadata` con mercato,
timeframe, numero di barre, filtri hard, criterio di score e split
in-sample/out-of-sample, e un array `top` con un elemento per candidato. Ogni
elemento porta tutte le metriche (IS, OOS, walk-forward, multisplit, plateau,
Monte Carlo sul drawdown) e **tutti i parametri del motore** nei campi `p_*`; i
parametri che non appartengono al motore di quel candidato valgono `NaN`.

`trades/topNN_<MOTORE>.csv` è la lista dei trade del candidato, con
`entry_time, exit_time, side, entry_price, exit_price, pnl, bars_held, exit_reason`.
L'ordine dei file segue quello dell'array `top`, ma **va verificato** invece di
assunto: il numero di righe deve essere `n_trades + oos_n_trades` dell'elemento
corrispondente. Nel run `20260730_0005` questo dà 925 righe per `top01` (574 IS +
351 OOS) e 691 per `top02` (431 + 260).

Il resto della cartella serve raramente al porting: `all_combinations.csv` e
`sweep/*.parquet` sono lo spazio esplorato, `partial_*.csv` le classifiche per
motore, `wfo_*.csv` le finestre walk-forward, `candidate_robuste.csv` e
`top10_gates.csv` l'esito dei gate di robustezza.

Il campo `engine` dell'elemento dice quale motore C# usare, con la mappa dei nomi
in [`motori-strategie.md`](motori-strategie.md) §"Catalogo dei motori".

## Tradurre i parametri

Ogni parametro `p_*` del report ha una controparte nel motore. Per il `PC`
(`PriceChannelEngine`) la corrispondenza completa è questa, e le altre famiglie
seguono lo stesso schema:

| Report | Campo C# | Note |
|---|---|---|
| `p_channel_len` | `ChannelBars` | canale **inclusa** la barra di segnale |
| `p_breakout_offset_ticks` | `OffsetTicks` + `TickSize` | il motore moltiplica, il report conta i tick |
| `p_direction` | `Direction` | 0 entrambi, 1 solo long, 2 solo short |
| `p_intraday_only` | `IntradayOnly` | `0` richiede `IntradayOnly = false` esplicito |
| `p_dvol_min` | `DvolMin` | 0 disattiva |
| `p_ptn_neut_yes` / `p_ptn_neut_no` | `NeutralYes` / `NeutralNo` | |
| `p_ptn_dir_yes` / `p_ptn_dir_no` | `DirectionalYes` / `DirectionalNo` | il segno lo applica il motore per verso |
| `p_start_hour` / `p_end_hour` | `StartTime` / `EndTime` | HHMM, quindi ora × 100 |
| `p_skip_day` | `SkipDay` | convenzione pandas, 0 = lunedì |
| `p_stop_loss`, `p_take_profit`, `p_trailing_stop`, `p_breakeven` | `StopMoney`, `ProfitMoney`, `TrailingStopMoney`, `BreakEvenMoney` | USD per contratto di riferimento |
| `p_max_bars` | `MaxBars` | 0 disattiva |
| (implicito) | `SessionStartTime` / `SessionEndTime` | sessione dei pattern, per NQ CME `1700`/`1600` |

Il timeframe e il simbolo vengono da `metadata.market`. `Id` (nome della classe) e
`Name` (codice di esecuzione) restano due cose diverse: vale l'invariante
descritta in [`../PROGETTO.md`](../PROGETTO.md) §3.

Le **sentinelle** sono la fonte di equivoci più frequente. `pattern_neutral(55)` e
`pattern_directional(52)` valgono sempre vero, e ogni numero non gestito dallo
`switch` cade nel `default => false`, quindi `pattern_directional(53)` vale sempre
falso. Un `p_ptn_dir_no = 53` non è un filtro con soglia altissima: è
**nessun filtro**. Scriverlo nel commento della classe come se filtrasse qualcosa
ha già fatto perdere tempo a chi confrontava due varianti.

## Trappole verificate

**`IntradayOnly` è vero per default** in `PriceChannelEngine`,
`SessionBreakoutEngine`, `TfEngines` e nei due `ReversalBollingerBand`. Un
candidato con `p_intraday_only: 0.0` che non lo disattiva emette `CloseAtUtc` a
fine sessione su ogni segnale e diventa una strategia di sessione, senza violare
nessun contratto: i test passano e la differenza si vede solo nei numeri. Su
`PTS_NQ_PCH_001_15` erano 848 `TimeExit` su 2.012 trade e $46.000 di utile in meno.
Il test parametrico in `Pts002PcTests` copre ora la regressione per le due PC:
va esteso a ogni nuova strategia multiday.

**Il canale include la barra di segnale.** Il motore Python usa `donchian(shift=0)`
e lo dichiara esplicitamente; `HighestChannelHigh` fa lo stesso. Non è
look-ahead, perché alla chiusura gli OHLC di quella barra sono noti e l'ordine
vale solo dalla barra successiva. Descriverlo come "esclusa la barra di segnale"
è un errore di documentazione che è già comparso due volte.

**Il percorso legacy e quello di parità Python convivono** in alcuni motori. Nel
`PC`, `UseLegacyVariant` seleziona il secondo, che usa `EnableLong`/`EnableShort`
e `NotEntryDayLong` invece di `Direction` e `SkipDay`. Assegnare i campi del ramo
sbagliato non produce errori, semplicemente non ha effetto: in
`PTS_NQ_PCH_001_15.Initialize` il parametro `SkipDay` viene ancora scritto su
`NotEntryDayLong`, che il ramo attivo non legge.

**La finestra operativa confronta HHMM, non le ore.** Il motore Python valuta
l'orario completo contro gli estremi `"HH:00"` con fine inclusa. `PriceChannelEngine`
è stato allineato il 2026-08-02; `VolatilityBreakoutEngine`, `LevelFaderEngine` e
`SessionBreakoutEngine` confrontano ancora le sole ore, quindi la loro finestra
si allarga fino a `HH:59` e prende barre che la fonte non prende.

**Le etichette delle barre non coincidono — questione aperta.** Il datafeed
Piootoo etichetta ogni barra sull'**apertura**, `EasyLib.OHLCMulti5` la assume
etichettata sulla **chiusura** (`isBarTimeEndTime = true`, confine
`t > sessionStartTime`). Conseguenze misurate su NQ 15m: i timestamp del
riferimento risultano 15 minuti avanti ai nostri; con sessione CME 17:00–16:00 la
barra 16:00 finisce dentro la sessione già chiusa e la barra 17:00, prima della
nuova, resta fuori da ogni sessione; su 1.684 sessioni del 2020–2025 l'`open` di
sessione differisce nell'82,6% dei casi e il `close` nell'82% con scarto medio di
13,7 punti. Sulle barre della pausa CME (16:15–17:00), che non appartengono a
nessuna sessione, i gate leggono un `d0` stantio — la sessione precedente
completa — e i pattern direzionali passano quasi sempre: per `PTS_NQ_PCH_001_15` sono
175 trade su 1.084 e $36.785 di utile. Chi porta una strategia la cui finestra
attraversa la pausa di sessione deve aspettarsi questo scarto finché la
convenzione non viene decisa (voce in [`../decisioni.md`](../decisioni.md)).

**Le uscite devono essere eseguibili anche live.** Una strategia portata correttamente vale quanto
il client che la esegue: se il cBot non applica una delle uscite dichiarate dall'intent, in
produzione gira un'altra strategia. Il caso limite è il trailing stop, che sulle PC del catalogo è
la causa di uscita di circa un trade su tre e da solo tutto il profitto. La lista di ciò che il
client deve applicare, e i tre campi non-uscita che deve rispettare (`Status`, `FinalQuantity`,
`ExpiresAtUtc`), sta in [`trading-sessions-api.md`](trading-sessions-api.md).

**Le assunzioni di costo.** Le commissioni coincidono già: il motore addebita
`commissionPerContract` all'ingresso e all'uscita, quindi `2` produce $4 per trade
completo come il riferimento. Lo **slippage no**: il riferimento applica 1 tick
sui fill stop, l'engine Piootoo zero, e nel motore di esecuzione non esiste un
parametro di slippage — quello in `TitanoRotationRequest` riguarda solo la
simulazione di equity di Titano. Su NQ la rettifica da applicare a mano è $10 per
trade (1 tick da $5 all'ingresso e 1 all'uscita).

## Verificare il porting contro il report

Serve un datafeed con la stessa storia del run: `NQ` 15m copre
`2006-01-03 → 2025-05-30`, quindi per i run recenti il confronto è possibile su
tutto il periodo. Con un feed più corto le metriche aggregate non dimostrano
niente.

Si crea un workspace dedicato con il masterfilter delle strategie da confrontare —
metterle **nello stesso run** garantisce barre e assunzioni identiche — e si
lancia il backtest sul periodo del report con `closeAllPositionsAtWeekEnd: false`
per non introdurre una chiusura che la fonte non ha.

Poi, in ordine:

1. **Allineare i timestamp.** Cercare l'offset che massimizza le corrispondenze
   fra gli `entry_time` del report e i nostri `entryTimeUtc`, provando passi di 15
   minuti e non solo di un'ora. Oggi il massimo è a −15 minuti (345 corrispondenze
   contro 78 a offset nullo): è la conferma dell'etichettatura, non un fuso orario.
2. **Ricalcolare il livello di trigger** sul nostro feed per ogni trade del
   report e confrontarlo con `entry_price`. L'atteso è `livello + 1 tick` per lo
   slippage della fonte: su 925 trade sono 490 casi esatti, 238 fill in gap sopra
   il livello, 169 con massimo di canale diverso, cioè residuo di dati.
3. **Confrontare gli aggregati** — trade, net profit, avg trade — applicando la
   rettifica di slippage. Se il nostro conteggio di trade è più alto, contare
   quanti nascono dalle barre fuori sessione prima di cercare cause più esotiche.
4. **Confrontare la distribuzione degli orari di ingresso.** È il controllo che
   rivela i disallineamenti di finestra e di sessione: un picco su uno slot
   orario che la fonte non ha è sempre un artefatto, non un'intuizione della
   nostra implementazione.

## Baseline del caso PTS_NQ_PCH_001 / PTS_NQ_PCH_002

Run di riferimento `20260730_0005`, mercato NQ 15m. I due candidati migliori sono
diventati `PTS_NQ_PCH_001_15` (top #1, `p_ptn_dir_no = 53`) e `PTS_NQ_PCH_002_15`
(top #2, `p_ptn_dir_no = 6`), identici in ogni altro parametro; `PTS_NQ_TFM_001_60`
viene da un altro run e usa `TF_M`. Poiché il filtro è l'unica differenza, i
trade delle due si sovrappongono quasi del tutto — 906 identici su 1.015 di
PTS_NQ_PCH_002, l'89% — e PTS_NQ_PCH_002 rende meno, perché `pattern_dir(6)` esclude le sessioni
già estese al rialzo, dove un breakout long corre. Tenerle entrambe nel
masterfilter non diversifica, raddoppia la size sullo stesso segnale.

Stato al 2026-08-02, periodo 2012–2025, un contratto, commissioni $4 round-turn:

| | Report | Piootoo | Piootoo con slippage 1 tick |
|---|---|---|---|
| PTS_NQ_PCH_001 trade | 925 | 1.084 | 1.084 |
| PTS_NQ_PCH_001 net | $204.200 | $165.909 | $155.069 |
| PTS_NQ_PCH_002 trade | 691 | 816 | 816 |
| PTS_NQ_PCH_002 net | $170.281 | $104.666 | $96.506 |

Il divario residuo è dominato dai trade generati sulle barre senza sessione, che
la fonte non fa. Un nuovo porting che parta da questa baseline dovrebbe
riprodurre lo stesso ordine di grandezza; uno scarto molto diverso segnala un
errore di traduzione dei parametri, non le convenzioni descritte qui.

## Riferimenti codice

- `Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs`
- `Piootoo.Strategies/Easy/Engines/PriceChannelEngine.cs`
- `Piootoo.Strategies/Easy/EasyLib.cs` (`OHLCMulti5`, `PatternNeutralFast`, `PatternDirectionalFast`)
- `Piootoo.Strategies/PiutooStrategies/PTS_NQ_PCH_001_15.cs`, `PTS_NQ_PCH_002_15.cs`
- `Piootoo.Strategies.Tests/Pts002PcTests.cs`
- `piootoo-repository/easy_engine_py/price_channel.py` (motore di riferimento)
