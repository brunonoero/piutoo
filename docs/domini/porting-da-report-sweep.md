# Portare una strategia da un report di sweep

I candidati della serie PTS nascono da un run di ottimizzazione esterno (cartelle
`D:\Piootoo\davide\run_YYYYMMDD_HHMM\`) che esplora i dodici motori Unger su un
mercato e produce una classifica. Questo documento descrive come si legge quel
run, come si traducono i suoi parametri in una sottoclasse C#, e quali trappole
hanno già fatto divergere un porting dalla sua fonte. La descrizione dei motori e
delle regole comuni sta in [`motori-strategie.md`](motori-strategie.md); come si
indaga una divergenza già avvenuta sta in
[`parita-riferimento-esterno.md`](parita-riferimento-esterno.md).

Quali run sono già stati tradotti, e quale classe `PTS_*` viene da quale riga, sta in
[`mappa-strategie-pts.md`](mappa-strategie-pts.md).

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

## La regola degli orari — leggila prima di scrivere una riga

Sbagliare qui non produce un errore, produce numeri plausibili. La fonte è il motore Python, e le
sue due regole sono **indipendenti fra loro**.

**1. La finestra operativa confronta l'orario della barra, e basta.** `filters.py` calcola
`minuti = index.hour * 60 + index.minute` e verifica `minuti >= start && minuti <= end` — oppure
l'OR, se la finestra attraversa la mezzanotte. Nessun riferimento a dove inizi la sessione. Quindi
`start_hour`/`end_hour` di `parametri.csv` si riportano **verbatim**:

```csharp
TradingWindow = ZonedWindow.ResearchHours(17, 10);   // start_hour 17, end_hour 10
```

Mai convertirli nell'ora di borsa del simbolo. La conversione a mano — meno sette ore per NQ, meno
sei per GC — è esatta solo fuori dalle settimane di disallineamento fra ora legale americana ed
europea, ed è stata la causa di una divergenza reale.

**2. La sessione NON si dichiara più: la governa il calendario.** Dal 07/09/2026 il confine di
sessione di una strategia viene da `MarketData/market-calendars.json`, per simbolo. Una classe
`PTS_*` non scrive nulla sulla sessione:

```csharp
// PRIMA: ogni classe dichiarava il proprio ancoraggio, e doveva indovinare quello giusto.
Session = ZonedWindow.ResearchSession(1);

// DOPO: niente. L'ancoraggio di FDAX e' 01:00 perche' lo dice il calendario, in un posto solo.
```

Il modello resta quello della ricerca — il motore Python taglia con
`(timestamp − 1 min − session_start_hour).normalize()`, cioè il **giorno di calendario europeo**,
non la sessione CME 17:00→16:00 di New York — ma l'ora non è più una scelta di chi porta: è un dato
dello strumento.

### 2.1 Gli interi `1700` delle sorgenti EasyLanguage non sono un ancoraggio

È la trappola che costa di più, perché il numero *sembra* pronto da copiare.

Una sorgente EasyLanguage dichiara `SessBegin = 1700`, `SessEnd = 1600`. Quello è **l'orario della
borsa** — le 17:00 di Chicago, la riapertura Globex — ed è un **modello di sessione diverso** da
quello della ricerca, non lo stesso orario scritto in un altro fuso. Nella sessione di borsa le
barre fuori orario non appartengono a nessuna sessione; nella sessione della ricerca ogni barra sta
in una sessione e il confine è l'ancoraggio.

**Non c'è nessuna aritmetica che porti da `1700` a un ancoraggio, e cercarne una è l'errore.** Le
due coincidono per gran parte dell'anno — mezzanotte a Roma sono le 17:00 a Chicago — e divergono
nelle circa quattro settimane in cui gli Stati Uniti sono già passati all'ora legale e l'Europa no.
Un port che "converte" il numero è esatto tranne lì, ed è esattamente il posto in cui non se ne
accorge.

**Cosa farne, allora.** L'intero della sorgente è una **verifica**, non un ingresso:

1. **Dice quale borsa la sorgente assumeva.** `1700`→`1600` è ora di Chicago, `1800`→`1700` è New
   York, `0800`→`2200` è Eurex. Si controlla che `exchangeTz` del simbolo nel calendario sia quella
   — il campo esiste per questo, e la coppia deve tornare *in quel fuso*.
2. **Dice se la sorgente usa davvero una sessione di borsa.** Se la sorgente si affida al fatto che
   le barre fuori orario non appartengano a nessuna sessione, il porting **si ferma e lo si
   segnala**: è il modello che oggi non supportiamo, e convertirlo in silenzio produrrebbe una
   strategia che opera su sessioni diverse da quelle su cui è stata trovata.
3. **Non dice l'ancoraggio.** Quello viene dal dossier della ricerca (tabella §2.4,
   `session_start_hour`), che è la stessa cosa che sta in `sessionStartHour` del calendario.

**Se il simbolo non è nel calendario**, va aggiunto *prima* di portare la strategia, dopo aver
verificato fuso di borsa, ancoraggio e giorni di sessione. Non esiste un default: un simbolo
sconosciuto è un errore esplicito, come per `PointValue`.

### 2.1bis La finestra operativa NON segue la stessa strada

Domanda naturale una volta che il calendario governa la sessione: allora anche
`ZonedWindow.ResearchHours(6, 5)` andrebbe convertito col calendario? **No**, e le ragioni sono
due, entrambe misurate.

**È una proprietà della strategia, non del simbolo.** Sul solo NQ il catalogo ha **24 finestre
diverse** — da `(0, 17)` a `(22, 21)` — su GC sei, su ES tre. Il calendario dichiara una cosa per
simbolo: una finestra per simbolo non esiste, e metterla lì è strutturalmente impossibile prima
ancora che sbagliato.

**Le ore non si convertono, mai.** Il filtro orario del motore Python confronta l'orario *della
barra stessa* (`filters.py`: `minuti = index.hour * 60 + index.minute`) senza alcun riferimento a
dove inizi la sessione: le due regole sono **indipendenti**. Convertire `start_hour`/`end_hour`
nell'ora di borsa del simbolo — meno sette ore per NQ, meno sei per GC — è esatto <i>tranne</i>
nelle settimane in cui l'ora legale americana ed europea non sono allineate, ed è già stato la
causa di una divergenza reale.

**E il fuso?** `ResearchHours` dichiara `Europe/Rome`, e il calendario lo dichiara di nuovo in
`researchTz`. È una duplicazione, ma **non può divergere**:
`MarketCalendarConformanceTests.ResearchTimeZoneIsTheOneDeclaredByZonedWindow` verifica che i due
coincidano per tutti e trenta i simboli. Non c'è niente da consolidare.

### 2.1ter Da dove viene la strategia decide in che orologio è la sua finestra

È la distinzione che conta quando arriva una strategia nuova, e si sbaglia in silenzio.

| Provenienza | La finestra è scritta in | Come si porta |
|---|---|---|
| Run di ricerca Python (`parametri.csv`, dossier) | **orologio della ricerca**, CET, per ogni simbolo | `ZonedWindow.ResearchHours(start, end)`, verbatim |
| Sorgente EasyLanguage (`StartTrade`/`EndTrade`) | **ora di borsa** dello strumento | `new ZonedWindow(start, end, fuso di borsa)` — il fuso è `exchangeTz` del calendario |

**Un numero da solo non dice da dove viene.** `1700` in un report di ricerca sono le 17:00 CET; in
una sorgente EasyLanguage su NQ sono le 17:00 di Chicago, cioè un istante diverso per sei o sette
ore. Prima di scrivere la finestra bisogna sapere quale delle due cose si ha in mano, e la risposta
sta nella provenienza del file, non nel numero.

Vale per **tutti** gli orari della sorgente, non solo per la finestra: le uscite a tempo e i filtri
sul giorno della settimana hanno lo stesso problema. Un `dayofweek()` letto nell'orologio sbagliato
cambia giorno nel mezzo della sessione serale americana.

### 2.2 L'override, quando serve davvero

Una strategia può avere un ancoraggio diverso da quello del suo simbolo. Deve dichiararlo, e
dichiarare **perché**:

```csharp
OverrideSessionAnchor(1, "run X: il report taglia le sessioni all'01:00 anche su NQ");
```

Due limiti voluti. Il motivo è **obbligatorio**: un override senza motivo scritto è
indistinguibile da una distrazione, e non compila. E si può cambiare **solo l'ora**: la sessione di
borsa — dove le barre fuori orario non stanno in nessuna sessione — non è un parametro diverso, è un
modello diverso, e non è supportata. Nessuna strategia la usa; costruirla ora significherebbe far
rinascere la seconda fonte di verità che il calendario esiste per eliminare.

Gli override sono elencati in `SessionAnchorOverrideTests`. Uno nuovo fa fallire il test finché non
viene messo in lista: così nasce da una decisione e non da un merge.

**3. L'orologio della ricerca è `Europe/Rome`, con le regole DST europee.** Misurato, non dedotto:
prendendo la barra a volume massimo di ogni giorno del 2024 — quella che marca l'apertura o la
chiusura del cash americano — il picco sta alle 22:00 a febbraio, alle **21:00 dall'11 al 28
marzo**, e di nuovo alle 22:00 ad aprile. Quelle tre settimane sono esattamente la finestra fra il
passaggio americano all'ora legale (10 marzo) e quello europeo (31 marzo): il mercato americano
compare un'ora prima. Stesso effetto nel 2013. Con un offset fisso da UTC quello scarto non
esisterebbe.

**4. Nessuna strategia legge l'ora di una barra.** Legge il suo istante e lo confronta con una
finestra che dichiara il proprio fuso. `StrategyClockConformanceTests` lo impone sul sorgente, e
impone anche che ogni `PTS_*` dichiari la propria `TradingWindow` con fuso esplicito. La sessione
non si dichiara più (vedi la regola 2). Il
feed dichiara il proprio orologio in `datafeed/feed-clocks.json` e viene convertito a UTC vero una
volta sola al caricamento: da lì in poi il port non dipende più da come sono stampate le barre.

Il dettaglio dei fusi, e la procedura per accertare l'orologio di un feed nuovo, stanno in
[`orari-di-sessione-e-fusi.md`](orari-di-sessione-e-fusi.md).

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
| `p_start_hour` / `p_end_hour` | `TradingWindow` | `ZonedWindow.ResearchHours(start, end)`, **verbatim**: vedi la regola sopra |
| `p_skip_day` | `SkipDay` | convenzione pandas, 0 = lunedì |
| `p_stop_loss`, `p_take_profit`, `p_trailing_stop`, `p_breakeven` | `StopMoney`, `ProfitMoney`, `TrailingStopMoney`, `BreakEvenMoney` | USD per contratto di riferimento |
| `p_max_bars` | `MaxBars` | 0 disattiva |
| `session_start_hour` | `Session` | `ZonedWindow.ResearchSession()`: giorno di calendario europeo, **non** la sessione del broker |

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

**L'offset del canale non ha tick impliciti — corretto il 17/08/2026.** `PriceChannelEngine`
sommava `(OffsetTicks + 1) * TickSize`, con un commento che attribuiva il tick in più alla
convenzione del motore Python. Il sorgente dice il contrario: `price_channel.py` calcola
`upper + offset * tick`, e `breakout.py` lo stesso — `SessionBreakoutEngine` era già corretto.
Con `breakout_offset_ticks = 2` su NQ il livello era 0,75 punti sopra il canale invece di 0,50, e
con offset 0 era 0,25 invece di zero. Il tick in più **non** è un modo valido di compensare lo
slippage che il riferimento applica sui fill stop e l'engine no (vedi *Le assunzioni di costo*):
alzare il livello cambia *se* il breakout scatta, non solo a che prezzo viene riempito. La
rettifica di slippage va applicata al confronto, non al livello.

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

> ⚠ **I numeri qui sotto sono anteriori a due correzioni del 17/08/2026** — l'offset del canale
> (trappola sopra) e la formula del `pattern_neutral(47)` in `EasyLib`, che divideva per 3 invece
> che per 2. Entrambe cambiano i trade delle PC, quindi la tabella non è più una baseline valida:
> va rimisurata prima di usarla per giudicare un porting nuovo.

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
