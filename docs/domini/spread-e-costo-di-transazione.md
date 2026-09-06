# Spread e costo di transazione

Lo spread denaro/lettera non è un parametro del backtest: è una **misura**, e cambia per
broker, per simbolo e per ora del giorno. Le barre non lo contengono — il feed è di un lato
solo — quindi l'unico modo di conoscerlo è guardare i tick di un conto vero. Questo file
copre le tre cose che ne discendono: come si misura, come entra nel motore, e quale numero
si sceglie.

## Perché non basta la commissione

`CommissionPerContract` è un costo per trade e si sottrae al risultato. Lo spread no: non
rende più cara la singola perdita, rende **più facile subirla**.

Su un CFD long si entra sull'**Ask** e lo stop è valutato sul **Bid**. La perdita in denaro
quando lo stop salta resta quella dichiarata dalla strategia — stop e target si spostano
insieme all'ingresso — ma il Bid deve scendere solo di `distanza − spread` per farlo
saltare. *Stessa perdita, più stop.*

Il numero che misura il danno è quindi **`spread / distanza di stop`**, e non lo spread da
solo:

| Strategia | Stop | Spread 2 punti vale |
|---|---:|---:|
| `PTS_NQ_PCH_002_15` | 12,5 punti | **16 %** |
| `PTS_NQ_TFM_001_60` | 50 punti | **4 %** |

Due strategie sullo stesso strumento, due esiti diversi. Uno stop stretto su uno spread
largo non è un difetto del sistema: è una coppia strategia/strumento sbagliata, ed è per
scegliere quella coppia che la misura esiste. Vedi `decisioni.md` 2026-08-06.

## La misura: `PiootooSpreadDumpBot`

Il cBot prende gli strumenti di un piano (o un elenco a mano), scorre i tick di una finestra
— un mese di default — e ne scrive la **distribuzione**, non il dato grezzo:

| File | Cosa contiene |
|---|---|
| `{BROKER}_spread-by-symbol_{da}-{a}.csv` | una riga per simbolo |
| `{BROKER}_spread-by-hour_{da}-{a}.csv` | 24 righe per simbolo, una per ora UTC |
| `{BROKER}_{SIM}_ticks_{da}-{a}.csv` | dump tick per tick, **spento di default** |

Ogni riga porta `min`, `p50`, `avg`, `p90`, `p99`, `max`, due volte: in **prezzo** (l'unità
in cui le strategie dichiarano gli stop, quindi quella con cui si fa `spread / stop`) e in
**tick di strumento** (l'unità in cui il broker quota, quindi quella in cui la misura è
esatta e in cui due simboli si confrontano).

**Perché non solo min e max.** Su una finestra di un mese sono i due numeri meno
rappresentativi che si possano produrre: il minimo è il fondo dell'ora più liquida — spesso
un tick, a volte zero — e il massimo è una news o la riapertura della domenica sera, cioè un
istante in cui nessuna strategia sta entrando.

**Perché per ora.** Una strategia opera dentro la propria `TradingWindow`, e lo spread che
paga è quello di quelle ore. Le ore del file sono **UTC e non convertite**: il fuso di una
strategia sta nella sua `ZonedWindow` e la conversione si fa dove quella si conosce
(vedi [`orari-di-sessione-e-fusi.md`](orari-di-sessione-e-fusi.md)), non in un bot che le
strategie non le vede. Un'ora con `ticks=0` e celle vuote è un'ora in cui il mercato è
chiuso — ed è un dato, quando la finestra di una strategia ci cade dentro.

Le percentili sono **esatte**: lo spread si conta in tick interi, l'istogramma tiene ogni
valore osservato e la percentile è un conteggio, non un'interpolazione. Uno spread di 1,5
tick non esiste, e inventarlo renderebbe la misura meno vera invece che più precisa.

Prima di lanciarlo conviene far girare `PiootooTickDownloaderBot` sulla stessa finestra: se
la cache di cTrader non ha ancora quei tick, il primo giro li scarica dal broker e ci mette
molto.

## Dove vivono i file

```
piootoo-repository/spread/{BROKER}/{BROKER}_spread-by-symbol_....csv
```

Una cartella per broker, e un run ne legge una sola: due broker sullo stesso simbolo non
hanno lo stesso spread — è il confronto per cui la misura esiste. È la stessa regola del
datafeed, con la stessa ragione.

**Non in `datafeed-external/`**: quella è il feed di barre e tick che scrive il server, e
mescolarci file di misura fa sembrare dati di feed quello che non lo è. Il bot scrive nella
propria cartella di output (`%AppData%\PiootooSpreadDump` di default) e i CSV si copiano
qui a mano.

La radice è `PiootooSettings.SpreadPath`, con default `[BasePath]\spread`.

## Come entra nel backtest

Sulla `BacktestingRequest`:

| Campo | Cosa fa |
|---|---|
| `SpreadBroker` | broker di cui caricare la tabella. Null = nessuna tabella. |
| `SpreadStatistic` | `Median` (default), `Mean` o `P90`: quale **colonna** del file diventa il numero. |
| `SpreadResolution` | `PerSymbol` (default) o `PerHour`: quale **riga** — la costante del simbolo, o l'ora UTC dell'ingresso. |
| `SpreadPoints` | valori per simbolo scritti a mano; **scavalcano** la tabella (ore comprese), un simbolo alla volta. |

Statistica e risoluzione sono **ortogonali** e si combinano: i due file hanno le stesse colonne,
quindi «p90 dell'ora» è una domanda legittima quanto «p50 dell'ora». Infilare la risoluzione
nella statistica avrebbe raddoppiato l'enum per dire una cosa sola.

`SpreadTable.Load` sceglie il CSV `spread-by-symbol` **più recente** della cartella (il nome
porta la finestra, quindi non è fisso e non si indovina), salta le righe `#`, normalizza i
simboli come il resto del motore (`StrategyKeys.NormalizeSymbol`: niente `@`) e rifiuta un
file senza la colonna attesa. Un broker senza misura **fa fallire l'avvio**: vale la regola
del datafeed mancante, perché un run che ripiegasse in silenzio su «nessuno spread» è
indistinguibile da uno con lo spread e vale un'altra cosa.

Un simbolo con `truncated=true`, o senza tick nella finestra, produce un **avviso** e non
un'eccezione: la misura c'è, è solo meno solida di quanto sembri.

### Cosa tocca, e cosa no

`PiootooTradingService.ApplySpread` peggiora il **solo prezzo di ingresso** — long
`fill + spread`, short `fill − spread` — e lascia dove sono trigger, livelli e uscite. È
l'unico punto del motore in cui lo spread entra, e ci passano tutti e quattro gli ingressi
(market immediato, market differito, stop, limit): una riga sola, niente da ricordarsi
aggiungendone un quinto.

Non è un'approssimazione comoda, è il modello giusto per il feed che abbiamo. Le barre di
`datafeed-external/` le raccoglie un cBot da `MarketData.GetBars`, che in cTrader è la serie
**Bid**. Su un long si entra sull'Ask (`Bid + spread`) e si esce sul Bid, che è esattamente
ciò che quella riga produce; su uno short il conto vero entra sul Bid e valuta l'uscita
sull'Ask, e spostare l'ingresso di `−spread` dà lo stesso P&L e fa scattare stop e target
sugli stessi istanti.

**Il trigger dei pending resta sul feed.** Sul broker un buy stop scatta sull'Ask, cioè
quando il Bid arriva a `livello − spread`; qui no. Muovere anche il trigger cambierebbe
*quali* trade nascono, e i run non sarebbero più confrontabili con il porting dal motore di
ricerca — che è il metro con cui le strategie sono state scelte. Lo spread qui è un costo,
non un'altra regola d'ingresso.

Lo spread si applica **dopo** il calcolo del prezzo di fill e non dentro: `Math.Max(bar.Open,
livello)` e il gap all'apertura sono convenzioni sul *prezzo del feed*
(vedi [`orologio-barre-e-fill.md`](orologio-barre-e-fill.md)), e mescolarci il lato del book
renderebbe illeggibili entrambe.

### Dove si vede

- **Log di avvio del job**: `entrySpreadSource` (broker, statistica, file, data) e
  `entrySpreadPoints` (simbolo=valore).
- **`backtest-summary.json`**, in `fillConventions`: `spreadPoints` con i **valori** e
  `spreadSource`. I valori e non i soli simboli come per `stopFillSlippageSymbols`: due run
  che dichiarano lo stesso simbolo con spread 2 e con spread 8 danno risultati che non si
  somigliano.
- **Report HTML**, scheda «Spread applicato all'ingresso»: una riga per simbolo, con la
  misura in testa. Si elencano **tutti i simboli del run** e non i soli misurati — un
  simbolo senza spread è un'assenza di misura, e una riga mancante si leggerebbe come «non
  c'era niente da dire». Un run senza spread lo dichiara invece di tacere.

## Quale numero scegliere

`Median` (`p50Spread`) è il default: è lo spread che si paga *di solito*, e risponde alla
domanda «queste strategie reggono il costo normale di questo broker?».

`Mean` (`avgSpread`) comprende la riapertura della domenica sera e le news, cioè istanti in
cui nessuna strategia sta entrando: sovrastima il costo di un ingresso tipico, ma è la
statistica giusta se si vuole che l'*equity* complessiva torni — per la stessa ragione per
cui `StopFillSlippagePoints` usa medie e non mediane.

`P90` è il caso brutto: quanto resta di una strategia che entra nel momento sbagliato.

Il numero davvero corretto è la mediana **delle ore in cui la strategia opera**, e da lì viene
`SpreadResolution.PerHour`.

## La risoluzione per ora

Con `SpreadResolution.PerHour` il motore non applica più una costante ma il valore dell'**ora
UTC dell'istante di ingresso**, dal file `spread-by-hour` della stessa misura. Serve perché lo
spread di uno strumento varia dentro la giornata più di quanto vari fra un mese e l'altro:
`PTS_NQ_PCH_002_15` e `PTS_NQ_TFM_001_60` stanno sullo stesso strumento e pagano costi diversi
solo perché entrano in ore diverse, e una costante sola è il compromesso fra le due.

**Il file è quello gemello, non il più recente.** Il nome si ricava da quello per simbolo
sostituendo `spread-by-symbol` con `spread-by-hour`. I due escono dallo stesso run del bot e
devono restare accoppiati: prendere anche qui «il più recente» avrebbe potuto mettere insieme la
costante di agosto e le ore di luglio, e un run che mescola due finestre non è confrontabile con
nessuna delle due.

**Le ore vuote ripiegano sulla costante.** Un'ora con `ticks=0` è un'ora in cui il broker non ha
quotato, e la cella è vuota. Lì si applica la costante per simbolo e lo si dice fra gli avvisi:
zero sarebbe uno spread *misurato e nullo*, e un ingresso gratis in un'ora vuota non è un dato ma
un buco che somiglia a un regalo. Il ripiego rende anche l'array sempre pieno — 24 caselle — così
la lettura sul percorso di ogni ingresso è una ricerca più un indice, senza rami.

Stessa regola per un simbolo che nel file per ora non compare affatto: paga la costante, e
l'avviso lo dice. E un `SpreadPoints` scritto a mano **toglie** anche le ore di quel simbolo,
altrimenti la tabella oraria vincerebbe e la correzione sarebbe ignorata in silenzio.

**Chiedere le ore senza il file delle ore fa fallire l'avvio**, come un datafeed mancante. È il
caso peggiore da lasciar passare: il run dichiarerebbe nel summary una risoluzione che non ha
applicato. Il file esce di suo — il parametro `Scrivi la distribuzione per ora UTC` del bot è
acceso di default — basta copiare entrambi i CSV.

L'ora è **UTC e non convertita**, come nel file: il fuso di una strategia sta nella sua
`ZonedWindow` e la conversione si fa dove quella si conosce.

### Dove si vede la risoluzione

- **Log di avvio**: `entrySpreadResolution` e `entrySpreadHourRange` (`NQ=1,5..12`), che è il
  numero da cui si capisce se la scelta ha cambiato qualcosa.
- **`backtest-summary.json`**: `spreadResolution` e `spreadPointsByHour` con i **24 valori** per
  simbolo. Per intero e non come minimo/massimo: l'escursione non dice quale dei due estremi
  incontra una strategia che opera solo il pomeriggio. `spreadPoints` resta la costante, che con
  le ore è il *ripiego*, non ciò che il run ha pagato.
- **Report HTML**: una colonna in più, `min → max` con le ore in cui cadono, e la nota che la
  colonna «Spread» è il ripiego.

## Nella console

La schermata di backtesting ha tre combo — **misura**, **statistica**, **risoluzione** — e un
pulsante *Vedi gli spread…* che apre la griglia di tutto ciò che il run applicherebbe: un simbolo
per riga, con la costante, l'ora del minimo e del massimo, il rapporto `max/min` e quante delle 24
ore portano il ripiego. Sotto le combo una riga sola dice se la scelta cambia qualcosa
(`16 simboli · colonna p50Spread · escursione oraria massima 28,3x su BP (0,00003–0,00085)`).

L'anteprima **non ricalcola niente**: passa da `ISpreadCatalog.GetTable`, che chiama lo stesso
`SpreadTable.Load` del backtest. Un secondo lettore avrebbe dato fiducia a un numero che poi non è
quello applicato, ed è l'errore peggiore che un'anteprima possa fare.

### Quando la misura non c'è

| Situazione | Cosa succede |
|---|---|
| Nessuna cartella sotto `spread/` | La combo ha la sola voce «nessuno» e lo dice: il run gira agli ingressi del feed, che è quello che il backtest ha sempre fatto. Non è un errore. |
| Broker scelto ma cartella sparita | Il preventivo mostra l'errore del server (`Disponibili: …`) e la scelta **resta**: è chi lancia a doverla correggere sapendo cosa manca. |
| Broker senza `spread-by-hour` | La combo lo etichetta *senza le ore*, e scegliere «per ora UTC» riporta la risoluzione sulla costante spiegando perché. Il run non parte più per fallire all'avvio. |
| Simbolo senza misura nel file | Nessun errore: quel simbolo entra al prezzo del feed. Compare nella griglia dell'anteprima e nella scheda del report, perché una riga mancante si leggerebbe come «non c'era niente da dire». |
| File senza la colonna della statistica | Errore esplicito con il nome della colonna: è un file di una versione vecchia del bot. |

La regola sotto è sempre la stessa: **un broker chiesto e non trovato ferma l'avvio**, un simbolo
non misurato no. Il primo è una richiesta che non si può soddisfare; il secondo è un'assenza di
misura, e dichiararla vale più che rifiutarsi di partire.

## La scelta è indipendente dal datafeed e dal piano

`SpreadBroker` non segue né `DatafeedBroker` né il broker del `PlanCode`. Girare sul feed
interno con lo spread di un broker vero è esattamente il confronto che dice quanto costa
quel broker, e legare le due scelte lo renderebbe impossibile — è lo stesso argomento con
cui datasource e piano sono già separati (vedi [`backtesting.md`](backtesting.md)).

## Cosa resta fuori

- **Le uscite.** Lo spread non tocca il prezzo a cui si chiude: sul feed Bid è corretto per
  i long, e per gli short la compensazione all'ingresso dà lo stesso P&L e gli stessi
  istanti. Non dà però lo stesso *prezzo di uscita scritto nel trade*, che su uno short
  risulta migliore del vero di uno spread.
- **La variabilità dentro l'ora.** `PerHour` copre la variazione fra le ore, che è la parte
  sistematica; resta fuori la dispersione dentro un'ora, che per riprodurre il drawdown
  andrebbe estratta dalla distribuzione — come già osservato per `StopFillSlippagePoints`.
  Estrarre **uniforme fra min e max** non è quella cosa: sono i due numeri meno rappresentativi
  del file (vedi sopra), e la loro media sta sopra la p90 — si simulerebbe un broker molto
  peggiore di quello vero, e per giunta due run della stessa richiesta smetterebbero di
  coincidere. Se si farà, sarà per inversione della CDF empirica e con il seme dichiarato nel
  summary, come run di sensitività e non come default.
- **Lo spread sui trigger**, per la ragione detta sopra.
- **Il live.** In sessione `ExternalBroker` lo spread è quello vero del momento, e il cBot
  lo misura al fill (`ExternalExecutionReport.SpreadAtFill`): lì non c'è niente da modellare.

## Riferimenti codice

- `piootoo-repository/ctrader/PiootooSpreadDumpBot.cs` — la misura.
- `Piootoo.Core/Services/SpreadTable.cs` — il caricamento del CSV.
- `Piootoo.Core/Services/PiootooTradingService.cs` — `SpreadPoints`, `ApplySpread`.
- `Piootoo.Core/Services/PiootooBacktestingService.cs` — il cablaggio, il log, il summary.
- `Piootoo.Core/Services/BacktestHtmlReport.cs` — `AppendSpreadHtml`.
- `Piootoo.Shared/Models/Backtesting/BacktestingRequest.cs`, `SpreadStatistic.cs`,
  `SpreadResolution.cs`.
- `Piootoo.Core/Services/SpreadCatalog.cs`, `PiootooApp.Server/Controllers/SpreadController.cs` —
  l'anagrafica e l'anteprima.
- `piootooapp.clientform/Shell/Screens/BacktestingScreen.cs`,
  `Shell/Controls/SpreadPreviewDialog.cs` — le combo e la griglia.
- `Piootoo.Strategies.Tests/EntrySpreadTests.cs`.
