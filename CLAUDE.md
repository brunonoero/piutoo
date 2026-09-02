# CLAUDE.md

Guida per Claude Code su questo repository.

## Documentazione esistente — leggila prima

Questo progetto ha già documentazione curata in `docs/`. **Prima di modificare
codice, leggi `docs/PROGETTO.md`**: contiene mappa dei moduli, flussi end-to-end,
invarianti e trappole note. `docs/README.md` è l'indice (segnala quali file sono
*stabili* e quali *bozza*). `docs/verifica-codice-2026-07-27.md` è l'audit del
codice con riferimenti puntuali. `docs/decisioni.md` è il log delle scelte fatte.

Non duplicare qui contenuto che sta in `docs/`: linkalo.

## Cos'è

Piootoo è un trading system per future in .NET: backtesting locale su datafeed
JSON, trading live via engine esterno cTrader, e "Titano" (filtro di rotazione
che decide periodo per periodo quali strategie sono abilitate e con che
allocazione). L'interfaccia operativa è una console WinForms che parla solo HTTP
con l'API ASP.NET Core.

## Build e run

```bash
dotnet build PiootooApp.sln
dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj
dotnet run --project PiootooApp.Server            # http://localhost:5142, swagger su /swagger
dotnet run --project piootooapp.clientform        # console WinForms (richiede il server attivo)
```

Il profilo di avvio della solution lancia insieme `PiootooApp.Server` e
`piootooapp.clientform`.

Target framework: `net8.0` per le librerie e il server, `net8.0-windows` per i
client WinForms, `net9.0-windows` per il progetto di test. Test con xUnit +
`Microsoft.AspNetCore.Mvc.Testing`. Buona parte della build richiede Windows
(WinForms); da Linux si compilano solo i progetti non-Windows.

## Struttura

| Progetto | Ruolo |
|---|---|
| `Piootoo.Shared` | Modelli e contratti. **Nessuna logica, nessuna dipendenza** verso gli altri progetti. |
| `Piootoo.Domain` | Repository di base, in particolare `DataSourceRepository` (lettura feed). |
| `Piootoo.Core` | Tutti i servizi applicativi: `Services/` (workspace, backtesting, trading, sizing, Titano, sessioni) e `Optimization/`. |
| `Piootoo.Strategies` | Catalogo strategie (`ITradingStrategy`), incluse quelle generate da EasyLanguage in `Easy/`. |
| `PiootooApp.Server` | API HTTP. Solo controller sottili + DI. |
| `Piootoo.FeedWorker` | Worker che alimenta le sessioni live con barre chiuse. |
| `piootooapp.clientform` | Console WinForms. Client HTTP puro. Due interfacce: la nuova `Shell/MainShellForm` (menu a sinistra, lista → dettaglio, schermate designer-first) e la storica `WorkspaceBacktestingForm` a tab, raggiungibile da *File → Console legacy*. |
| `piootooapp.client` | SPA Angular, scollegata dal debug F5. |
| `piootoo-repository/` | Dati fuori dal codice: `datafeed/` (JSON OHLCV), `datafeed-external/{BROKER}/` (le stesse barre raccolte dai bot cTrader, un archivio per broker), `ctrader/` (sorgenti cBot), `easy/` (sorgenti EasyLanguage), `datafeed-downloader/` (Python). |

I controller non contengono logica: traducono eccezioni in `ProblemDetails` e
delegano ai servizi di `Piootoo.Core`.

## Invarianti da non rompere

Elenco completo con motivazioni in `docs/PROGETTO.md` §3 e §7. I punti su cui si
sbaglia più spesso:

- **Id ≠ Name.** `Id` è il nome della classe (`PTS_NQ_TFM_001_60`) e serve solo a
  *selezionare* dal catalogo (masterfilter, `StrategyFactory`). `Name` /
  `StrategyCode` (`PTS_NQ_TFM_001_60`) è ciò che finisce in tutto il dominio di
  *esecuzione*: `signals.json`, `trades.json`, chiavi di posizione, stati Titano.
  Per confrontare masterfilter e dati di esecuzione passa da
  `StrategyCatalog.ResolveCodes`. Confondere i due ha già svuotato report e
  rotazioni una volta.
- **Tutto è UTC.** I contratti delle sessioni rifiutano `DateTime` con
 `Kind != Utc`: è voluto, non "aggiustarlo" con `SpecifyKind` a valle. E
 "adesso" è `DateTime.UtcNow`: `DateTime.Now`, `ToLocalTime` e affini sono
 vietati fuori dalla console WinForms, e `UtcOnlyConformanceTests` lo verifica.
- **Gli orari di una strategia dichiarano il proprio fuso, e non si convertono mai a mano.**
 `Session` e `TradingWindow` sono due `ZonedWindow` distinte — orario locale più fuso IANA — e il
 confronto passa da `SessionClock`, mai dall'ora grezza della barra. Per le strategie portate dai
 run di ricerca valgono `ZonedWindow.ResearchSession()` e `ZonedWindow.ResearchHours(start, end)`,
 con `start_hour`/`end_hour` riportati **verbatim**: la sessione della ricerca è il giorno di
 calendario europeo, non quella del broker. La regola completa è in
 `docs/domini/porting-da-report-sweep.md` §"La regola degli orari"; i fusi e le misure in
 `docs/domini/orari-di-sessione-e-fusi.md`. `StrategyClockConformanceTests` la impone sul sorgente.
- **`UpdateMarketPrices` a ogni barra**, su tutti i simboli della barra, anche
  se nessuna strategia è stata valutata — altrimenti SL/TP/time exit scattano in
  ritardo.
- **Un `PiootooTradingService` per job.** Il motore è mutabile e non thread-safe,
  anche se i servizi che lo ospitano sono singleton.
- **`AtomicFileWriter` mai dentro un loop.** Fa fsync: va bene per l'artefatto
  finale, per i checkpoint intermedi usa la variante non sincronizzata.
- **I checkpoint non riscrivono l'artefatto intero.** `signals.json`,
  `trades.json` e `rotation-log.json` crescono per tutto il run: riscriverli a
  ogni checkpoint costa quanto il run già fatto, e rende il backtest quadratico.
  I checkpoint accodano al journal `.jsonl` affiancato
  (`TradingJsonStore.Append*`); l'array viene materializzato alla lettura o alla
  scrittura autorevole di fine run. Chi legge quei file senza passare dallo
  store deve chiamare prima `CompactAll()`. Vedi `docs/decisioni.md` 2026-08-20.
- **Niente LINQ nei loop caldi.** Nel backtest si usa `CandleWindowCursor`
  (indice incrementale su serie già ordinata).
- **Reflection cacheata.** `StatelessEasyStrategyBase` è il punto più caldo del
  sistema: ogni `GetType().GetProperty(...)` aggiunto lì si paga
  moltiplicato per (barre × strategie).
- **Datafeed mancante = errore esplicito.** Se una coppia `(Symbol, Timeframe)`
 del masterfilter non ha dati, il backtest deve fallire o segnalarlo, mai
 proseguire in silenzio.
- **Un run legge da un archivio di barre solo.** Il backtest sceglie il datafeed interno
 (`datafeed/`) oppure quello di un broker (`datafeed-external/{BROKER}/`): stessa struttura,
 prezzi diversi, e un run a cavallo delle due non corrisponde a nessun conto. `DatafeedCatalog`
 e' l'unico punto che traduce un broker in un path, `BacktestingRequest.DatafeedBroker` lo
 sceglie (null = interno) e `backtest-summary.json` lo dichiara: due run su feed diversi non
 sono confrontabili. Un broker inesistente fa fallire l'avvio, non ripiega sull'interno.
 Vedi `docs/domini/datafeed-generazione.md`.
- **In sessione `ExternalBroker` la storia è solo quella che il client spinge.**
 Il server non ha datafeed proprio e `StrategyEvaluationService` salta in
 silenzio finché `history.Count < RequiredCandles` (per una strategia a 15
 minuti sono 576 barre). Il client manda quindi il riscaldamento all'avvio, poi
 finestre corte e **sovrapposte** a ogni barra; il server accoda solo le candele
 che non ha, ne valuta una sola, e rifiuta la finestra che non si sovrappone
 invece di accodare una serie bucata. Le candele restano in RAM: il datafeed su
 disco è compito di un cBot raccoglitore dedicato (`PiootooDatafeedSyncBot` →
 `api/datafeed-external`, vedi `docs/domini/raccolta-datafeed-esterno.md`).
 Regole complete in `docs/domini/finestra-candele-e-riscaldamento.md`.
- **Il feed di un broker non si mescola a quello di un altro, né a quello del
 vendor.** Il datafeed raccolto vive in `datafeed-external/{BROKER}/`, con la
 stessa convenzione `@SYM_{minuti}.json` e un `feed-clocks.json` proprio. Per lo
 stesso simbolo due broker non producono la stessa serie — cambiano orario di
 sessione, bucket e volume — e il feed del vendor ha un bucket diverso da
 entrambi. E si raccoglie **a blocchi**, mai in un'unica chiamata: l'unità è
 idempotente perché la chiave è l'istante di apertura della barra, i blocchi si
 accodano a un journal e il file piatto si materializza alla compattazione.
- **Barra di esecuzione ≠ prezzo di mark.** L'orologio del loop è sintetico e sui
 tick senza barre il cursore restituisce l'ultima barra chiusa. Quel prezzo va
 usato per il mark-to-market (altrimenti stop e time exit non sono valutabili) ma
 **non** per riempire un ordine, e un intent scaduto si scarta invece di eseguirsi
 al proprio livello: è così che nascono i fill fantasma. Vedi
 `docs/domini/orologio-barre-e-fill.md`.
- **Un pending vive la PROPRIA barra, non un tick dell'orologio.** `ExpiresAtUtc`
 e' l'inizio dell'ultima barra su cui l'ordine e' valido: la scadenza si misura su
 `ExpiresAtUtc + TimeframeMinutes` del segnale. Il loop gira al timeframe *minimo* del
 portafoglio e `currentBars` tiene una sola barra per simbolo — la piu' fitta — quindi
 confrontare i due istanti fa morire l'ordine di una strategia a 60 minuti dopo mezz'ora,
 e gli fa vedere meta' del proprio range. Vedi
 `docs/domini/orologio-barre-e-fill.md`.
- **Un livello gia' scavalcato non e' un ordine.** Uno stop buy sotto il prezzo si
 riempirebbe all'apertura, ma il cBot lo scarta al piazzamento
 (`RejectWrongSideLevels`) e quel trade nel conto vero non esiste. L'engine interno ha
 lo stesso filtro, acceso di default: spegnerlo serve solo a misurare la fedelta' del
 porting rispetto al motore di ricerca.
- **Le uscite protettive hanno le stesse convenzioni degli ingressi.** Uno stop
 originale su una barra che *apre* oltre il livello si riempie all'apertura, come fa da
 sempre l'ingresso; un trailing o un break-even no, perche' possono essere nati
 dall'estremo della barra in corso. E il trailing segue il picco **a scatti** di
 `TrailingMinStepFraction` (0,10, lo stesso passo minimo del cBot), non a ogni barra:
 senza, il primo ritracciamento lo toglieva e l'engine era pessimista di un fattore
 cinque sulle uscite in trailing. Entrambi i numeri stanno sulla `BacktestingRequest` e
 nel log di avvio del job. Vedi `docs/domini/orologio-barre-e-fill.md`.
- **Overnight e overweek: decide prima il piano, poi motore e strategia.**
 `tiene = pianoPermette && strategiaVuole`. `AccountHoldingPolicy` sta sul `TradingPlan`,
 scende nella sessione, esce nel descriptor e la eseguono i cBot; la stessa
 `BacktestingRequest` la riceve. La composizione avviene in **un punto solo**
 (`HoldingResolver`), chiamato da backtest e sessione: vince la scadenza piu' stretta, il
 piano non puo' forzare un overnight che la strategia non vuole, e `AllowOverweek` senza
 `AllowOvernight` viene rifiutato. Cosa la strategia vuole lo dichiara `ITradingStrategy.Holding`
 e si vede nel catalogo, nella griglia e a chart. I cBot **non hanno piu' parametri** di
 chiusura forzata: il flat *come regola di sicurezza* resta nel bot — deve tenere a server
 muto — ma il permesso e gli orari no. Quando vivevano solo li', il backtest ne usava altri
 (l'ultimo slot prima di sabato, le 23:30 contro le 20:45) e i due run non erano confrontabili.
 Vedi `docs/domini/overnight-e-overweek.md`.
- **Il server decide *cosa*, il broker decide *se e a che prezzo*.** Non
  assumere mai un fill.

## Diagnosticare un backtest

Quando un backtest non produce trade, il file da leggere è
`<workspace>/backtests/<nome>/backtest-summary.json`: il blocco `diagnostics` in
testa elenca i problemi rilevati automaticamente (strategia mai valutata, mai un
segnale, segnali senza trade, datasource vuoto). Il dettaglio evento per evento
è in `backtest-log.jsonl` (append-only, una riga JSON per evento).

Causa frequente: il timeframe chiesto dalla strategia non ha il proprio
`datafeed/@{simbolo}_{minuti}.json` — il datafeed locale è un file per coppia
(simbolo, timeframe) e si genera con `datafeed-future/aggregate_flat_feed.py`
(vedi `docs/PROGETTO.md` §5). Per il motivo opposto — un backtest che produce *troppi* trade, o trade a orari in cui il
feed non ha barre — parti da `coversRequestedRange` nel summary e dai controlli in
`docs/domini/orologio-barre-e-fill.md`.

Quando invece i trade ci sono ma l'equity non torna con la ricerca, la prima riga
da cercare e' `[fine settimana]`: dice a quali strategie il flat ha chiuso una
quota rilevante dei trade. Il flat e' una regola del **conto**, non della
strategia, e su una multiday (`IntradayOnly = false`) le taglia i trade a meta'
senza che l'uscita sembri anomala. Il summary porta anche
`weekEndFlatFromUtcHhmm`: due run con orari diversi non sono confrontabili.

## Diagnosticare una sessione (cBot, `ExternalBroker`)

L'equivalente del `backtest-summary.json` e' **`session-summary.json`**, scritto nella cartella
della sessione accanto a `signals.json` e `trades.json`. Serve perche' quei due non bastano:
`PersistOnlyFilledIntents` e' `true` di default e negli artefatti finiscono i soli intent
**riempiti**, quindi *"mai valutata"*, *"intent mai emesso"* e *"ordine rifiutato"* hanno tutti e
tre lo stesso aspetto — il nulla. La scheda conta gli intent per stato senza tenerli.

Quando una strategia non opera, l'ordine delle domande e' questo:

1. **`everEvaluable`** nella riga della strategia. Se e' `false`, lo stream non ha **mai** avuto
 abbastanza barre e il server ha saltato in silenzio (`history.Count < RequiredCandles`, in
 `StrategyEvaluationService.Evaluate`): non e' un problema di segnale. Il confronto sta nel blocco
 `streams`, dove `historyBarsHighWater` e' il **massimo storico** — non il conteggio finale, che
 `TrimHistory` falsa. Le soglie variano di venti volte fra strategie sullo stesso stream:
 `PTS_NQ_VBO_002_240` ne chiede 606 e le altre sei di `@NQ_240` ne chiedono 36.
2. **`intentsEmitted` contro `intentsFilled`**. Emessi e mai riempiti = il segnale nasce e
 l'ordine no: guarda `RejectWrongSideLevels` e la scadenza dell'intent. Zero emessi con
 `everEvaluable: true` = i gate della strategia non sono mai scattati, ed e' una domanda per il
 porting, non per l'infrastruttura.
3. Il blocco `diagnostics` in testa dice gia' 1 e 2 a parole.

Solo se la scheda non basta si rifa' il run con `PIOOTOO_PERSIST_ALL_INTENTS=1`, che spegne il
filtro e tiene ogni intent: e' molto piu' caro e serve solo per guardare i singoli record.
Vedi `docs/decisioni.md` 2026-09-01.

## Convenzioni

- **Console WinForms**: le regole delle schermate stanno in
  `.cursor/rules/piutoo-console-screens.mdc` — griglie sempre ordinabili
  (`SortableBindingList<T>` + `EnableColumnSorting()`), busy su ogni chiamata al server,
  lista → dettaglio con gli artefatti in sola lettura, elenchi che non deserializzano ciò
  che elencano.
- Documentazione e commenti in italiano, come il resto del progetto.
- `docs/`: un file per concetto, nome in kebab-case, prosa tecnica compatta;
  ogni file di dominio chiude con "Riferimenti codice" invece di ripetere firme.
- Messaggi di commit in italiano, descrittivi.
- Nullable reference types e implicit usings sono abilitati ovunque.
