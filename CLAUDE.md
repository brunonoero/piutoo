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
| `piootooapp.clientform` | Console WinForms a quattro tab (Workspaces, Backtesting, Titano, Trading Session). Client HTTP puro. |
| `piootooapp.client` | SPA Angular, scollegata dal debug F5. |
| `piootoo-repository/` | Dati fuori dal codice: `datafeed/` (JSON OHLCV), `ctrader/` (sorgenti cBot), `easy/` (sorgenti EasyLanguage), `datafeed-downloader/` (Python). |

I controller non contengono logica: traducono eccezioni in `ProblemDetails` e
delegano ai servizi di `Piootoo.Core`.

## Invarianti da non rompere

Elenco completo con motivazioni in `docs/PROGETTO.md` §3 e §7. I punti su cui si
sbaglia più spesso:

- **Id ≠ Name.** `Id` è il nome della classe (`Easy_218_GC_60`) e serve solo a
  *selezionare* dal catalogo (masterfilter, `StrategyFactory`). `Name` /
  `StrategyCode` (`TOP_UA_218`) è ciò che finisce in tutto il dominio di
  *esecuzione*: `signals.json`, `trades.json`, chiavi di posizione, stati Titano.
  Per confrontare masterfilter e dati di esecuzione passa da
  `StrategyCatalog.ResolveCodes`. Confondere i due ha già svuotato report e
  rotazioni una volta.
- **Tutto è UTC.** I contratti delle sessioni rifiutano `DateTime` con
  `Kind != Utc`: è voluto, non "aggiustarlo" con `SpecifyKind` a valle.
- **`UpdateMarketPrices` a ogni barra**, su tutti i simboli della barra, anche
  se nessuna strategia è stata valutata — altrimenti SL/TP/time exit scattano in
  ritardo.
- **Un `PiootooTradingService` per job.** Il motore è mutabile e non thread-safe,
  anche se i servizi che lo ospitano sono singleton.
- **`AtomicFileWriter` mai dentro un loop.** Fa fsync: va bene per l'artefatto
  finale, per i checkpoint intermedi usa la variante non sincronizzata.
- **Niente LINQ nei loop caldi.** Nel backtest si usa `CandleWindowCursor`
  (indice incrementale su serie già ordinata).
- **Reflection cacheata.** `StatelessEasyStrategyBase` è il punto più caldo del
  sistema: ogni `GetType().GetProperty(...)` aggiunto lì si paga
  moltiplicato per (barre × strategie).
- **Datafeed mancante = errore esplicito.** Se una coppia `(Symbol, Timeframe)`
  del masterfilter non ha dati, il backtest deve fallire o segnalarlo, mai
  proseguire in silenzio.
- **Il server decide *cosa*, il broker decide *se e a che prezzo*.** Non
  assumere mai un fill.

## Diagnosticare un backtest

Quando un backtest non produce trade, il file da leggere è
`<workspace>/backtests/<nome>/backtest-summary.json`: il blocco `diagnostics` in
testa elenca i problemi rilevati automaticamente (strategia mai valutata, mai un
segnale, segnali senza trade, datasource vuoto). Il dettaglio evento per evento
è in `backtest-log.jsonl` (append-only, una riga JSON per evento).

Causa frequente: Yahoo Finance fornisce dati intraday solo per ~60 giorni, quindi
backtest intraday su finestre lunghe restano muti per mancanza di dati.

## Convenzioni

- Documentazione e commenti in italiano, come il resto del progetto.
- `docs/`: un file per concetto, nome in kebab-case, prosa tecnica compatta;
  ogni file di dominio chiude con "Riferimenti codice" invece di ripetere firme.
- Messaggi di commit in italiano, descrittivi.
- Nullable reference types e implicit usings sono abilitati ovunque.
