# Lavori in corso

Stato al **2026-09-07**. Questo file è volutamente deperibile: quando una voce è chiusa si
cancella da qui, e la motivazione della scelta resta in [`decisioni.md`](decisioni.md). Se una
sezione qui contraddice il codice, ha ragione il codice.

---

# ⇦ RIPRENDERE DA QUI: refactor del layer barre, passo 5

Piano completo e stato passo per passo in
[`domini/layer-barre-e-calendario.md`](domini/layer-barre-e-calendario.md) §7. Il metodo è sempre
lo stesso e va tenuto: **si misura prima**, si cambia, si rimisura sul paniere completo.

## Dove siamo

Chiusi il 07/09/2026: **passo 0** (il metro), **1** (calendario come dato), **2** (il layer),
**3** (il backtest ci passa), **4a** (il calendario governa la sessione), la parte *raccoglitore*
del **6**, e la **prima voce del 5** (la deadline di fine sessione). Il progetto è alla **7.0.0**.

Suite: 1.107 test, **42 rossi preesistenti** (erano 43 a inizio sessione — uno si è risolto da
solo togliendo una finta sessione di borsa dai test dei motori). Nessun rosso nuovo introdotto.

## Le due voci che restano del passo 5

Sono quelle che realizzano la **logica future sul fine settimana**: il conteggio delle barre deve
seguire il calendario, non il feed.

### 5b — `MaxBarsInPosition` conta le barre del calendario

Oggi `PiootooTradingService` incrementa `BarsInPosition` quando il feed ha consegnato una barra per
quel simbolo. Conseguenza diretta e misurata al passo 3: le **972 barre di sabato su BTC/60m** e le
**53 domeniche su FDAX** fanno avanzare il contatore, chiudendo posizioni in anticipo rispetto al
future. Il contatore deve avanzare solo dove `BarContext.IsSessionDay` non è falso.

Nella stessa passata va eliminato **`ScaleSignalMaxBarsInPosition`**
(`PiootooBacktestingService`): moltiplica N per il rapporto fra timeframe della strategia e
orologio del loop *prima* di sapere quante barre esisteranno davvero. Con il conteggio sul
calendario non serve più — il numero è già in barre della strategia.

Riguarda **62 strategie su 124** (quelle con `MaxBars > 0`), quindi è la voce che sposterà di più.

### 5c — `SessionBarToUtc` diventa un conteggio di barre vere

`EasyEngineBase.SessionBarToUtc` proietta la N-esima barra di sessione sull'orologio, e il suo
stesso commento dichiara il limite: *«su una sessione con barre mancanti la chiusura cade
sull'orario atteso, non sulla N-esima barra ricevuta»*. Lo usa `BiasBarCountEngine`. Va sostituito
con `BarContext.BarIndexInSession`.

Da guardare nella stessa passata, perché è aritmetica a giorni di calendario della stessa famiglia:
`MaxDaysInTrade` / `MaxDaysFlatTime` in `PriceChannelEngine` (`AddDays()` su giorni di calendario).

## Le altre voci aperte del piano

- **4b — gli engine consumano `BarContext`.** Eliminare `EasyLib.ClassifySessionBar`,
  `FullDaySessionDay`, `ResolveEntrySessionStartUtc` e le sue tre copie nei motori. Include il ramo
  "sessione di borsa" di `ClassifySessionBar`, ora **provatamente irraggiungibile** (tutte e 124 le
  PTS usano la forma a giornata piena). Tocca i motori, non il catalogo.
  Guadagno collaterale: `OHLCMulti5` fa `Where().OrderBy().ToArray()` a **ogni valutazione** — LINQ
  nel punto più caldo del sistema, che CLAUDE.md vieta.
- **6 (parte operativa) — i due cBot di esecuzione.** `PiootooDirectExecutionBot` e
  `PiootooDistributedExecutionBot` piegano ancora i bucket con una copia del vecchio codice,
  ancoraggio compreso: sono le ultime **due copie della tabella** su quattro.
- **7 — riscaldamento dal disco.** Il server legge `datafeed-external/{BROKER}/@SYM_1.json` e il
  client manda solo la coda. Fa sparire R2 e R3 di
  [`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md).
- **8 — `aggregate_flat_feed.py` produce solo il minuto.** I CSV del vendor sono sul disco
  (`datafeed-future/FUTURES_Historical_Data/`, 200-360 MB per simbolo), quindi non serve nessuna
  raccolta. Chiude da sé il difetto §7quater: una barra giornaliera all'anno per simbolo è fuori
  griglia, sempre l'ultima domenica di ottobre.

## Da fare fuori dal codice

- **Ricompilare i cBot in cTrader.** Il salto a 7.0.0 è un cambio di contratto: i due bot operativi
  stampano un disallineamento finché non vengono ricompilati e ridistribuiti. Il raccoglitore
  (2.0.0, numerazione propria) va ricompilato perché è cambiato del tutto.
- **La raccolta a un minuto** può partire quando vuoi: il server nuovo è il prerequisito, perché
  `rebuild-from-minutes` non esiste su quello vecchio e senza aggregati i backtest sopra il minuto
  non trovano il datafeed.
- **Ventuno classi PTS sono su simboli fuori dal calendario** — HK (5), HO (8), JY (8) — e non sono
  eseguibili: manca loro il `PointValue` prima ancora della sessione. HK e HO sono usciti dal
  paniere il 07/09/2026 ma le classi sono rimaste sul disco. Decisione non presa: aggiungerli al
  calendario, o cancellarli.
  `StrategyClockConformanceTests.StrategiesOnSymbolsWithoutACalendarAreDeclared` fissa il numero.
- **`ResearchSessionStartConformanceTests` è diventato tautologico** dopo il 4a: legge `Session`
  dalla strategia, che ora viene dal calendario, e la confronta col calendario. Il controllo utile
  (i mercati che aprono all'01:00 sono quelli del dossier) è già in
  `MarketCalendarConformanceTests.OnlyTheDossierMarketsOpenAtOneCet`. Da potare.

## Niente di tutto questo è stato committato

Il lavoro è nel working tree. `git status` per vederlo; `docs/decisioni.md` ha una voce per ogni
scelta, in ordine.

---

## Nulla di quanto segue è stato compilato

Le modifiche del 04/08 sono state scritte in un ambiente senza `dotnet` e senza l'API cAlgo. Prima
di fidarsi di qualunque riga:

```bash
dotnet build PiootooApp.sln
dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj
```

I due cBot vanno ricompilati dentro cTrader. Sono stati rinominati, quindi per la piattaforma sono
robot nuovi e vanno riaggiunti ai grafici.

## Refactor della lista backtest — scritto, mai eseguito

Il refactor è completo lato codice: *Backtesting* è lista → dettaglio come le altre voci, con filtro
workspace e origine, ordinamento per data discendente, cancellazione che avvisa dei run Titano
contenuti, e *Nuovo backtest* che porta al form di avvio.

Server (invariato rispetto al 04/08): `GET {ws}/backtests/{cartella}/summary`, `GET .../titano-runs`,
`DELETE {ws}/backtests/{cartella}`; marcatore `origin.json` scritto da `PiootooBacktestingService` e
`TradingSessionService`, propagato in `WorkspaceBacktestInfo.Origin`.

Client: `BacktestListScreen` (+ designer, con colonna *Origine* e combo di filtro),
`BacktestDetailScreen` (+ designer, tab *Riepilogo* e *Operazioni*), `TradingResultsScreen`
cancellata e la sua voce di menu rimossa, `BacktestingScreen` raggiungibile solo da *Nuovo backtest*.
Le motivazioni sono in [`decisioni.md`](decisioni.md).

Lo stesso trattamento è stato applicato a Titano: *Operatività → Run Titano* è la lista
(`TitanoRunListScreen` → `TitanoRunDetailScreen`), `TitanoScreen` è la destinazione di *Nuova
rotazione*, `RotationsScreen` è cancellata e la sezione *Analisi* non esiste più. Lato server:
`GET /api/Titano/rotations` senza `backtestFolder` elenca tutti i run del workspace, e
`DELETE /api/Titano/rotations/{runId}` è nuovo.

Trasversali, sempre non compilati: griglie ordinabili (`SortableBindingList<T>` +
`EnableColumnSorting()`), busy visibile sull'intera schermata, e la lettura leggera di
`backtest-summary.json` e dei manifest Titano negli elenchi. Le regole sono ora scritte in
[`.cursor/rules/piutoo-console-screens.mdc`](../.cursor/rules/piutoo-console-screens.mdc).

**Da verificare al primo avvio su Windows** — niente di tutto questo è mai stato compilato né
aperto nel designer:

- Il layout dei due tab dipende dall'ordine di `Controls.Add` (Fill per primo, Top in ordine
  inverso). Se il riepilogo appare capovolto è quello.
- `BacktestingScreen` ora vive dentro uno stack di navigazione: a fine job non torna alla lista, e
  la lista non si aggiorna da sola. Se dà fastidio, `GoBack()` al completamento.
- Il tab *Operazioni* mostra `trades.json` anche per le cartelle di origine esterna, dove il
  summary non c'è: verificare che l'assenza sia segnalata e non passi per un errore di rete.

## Questioni aperte

### Ripresa di una sessione dopo il riavvio del server — fasi 0 e 1 fatte

Procedura completa in [`domini/riavvio-del-server-e-ripresa-sessione.md`](domini/riavvio-del-server-e-ripresa-sessione.md),
motivazioni in [`decisioni.md`](decisioni.md) 2026-09-04.

**Fatto il 04/09/2026:** il dump (`session-state.json`), la reidratazione all'avvio con id e token
conservati, il riscaldamento autoguarente sul cBot distribuito, il presidio dalla console.

**Aperto**, in ordine:

1. **Epoca e quarantena** (fase 2 e 3 del documento). La quarantena non è stata implementata
   insieme alla reidratazione perché la sua unica uscita è la riconciliazione, che non c'è: oggi
   congelerebbe il sistema dopo ogni riavvio. Le due vanno fatte insieme.
2. **`POST /reconcile`** (fase 3). Sul percorso distribuito buona parte esiste già —
   `AccountSignalPollRequest` porta posizioni, ordini e trade a ogni poll, e
   `ReconcileVanishedPositions` li consuma — e va generalizzata ai pending, al prezzo di chiusura
   vero e alle sessioni dirette, che non hanno un poll di claim.
3. **Label del cBot diretto** con l'IntentId, come già fa il distribuito.
4. **Outbox** sui cBot per gli execution report: oggi sono fire-and-forget, quindi quello che
   accade mentre il server è giù è perso per lui e nessun dump lo recupera.

**Da verificare al primo run vero:** i cBot non sono compilati qui dentro. `PiootooDistributedExecutionBot`
è cambiato (campo `WarmUpBarsSent`, confronto in `ReportWindowStatus`) e va ricompilato in cTrader.

### La distribuzione multi-account non è backtestabile

`MaxConcurrentTrades` e il fan-out fra gruppi sono applicati solo nel percorso di claim, che vale per
`ExecutionMode.ExternalBroker` con gruppi configurati. Il backtest interno non distribuisce, e il
backtesting cTrader esegue una istanza su un conto simulato: due backtest separati non sono
sincronizzabili, e la distribuzione è **pull**, quindi l'ordine dei poll — e con esso il risultato —
sarebbe casuale. Oggi l'unica verifica deterministica sono i test
(`MultiAccountDistributionTests.cs`); l'unica misura operativa è il live su conti demo.

Per chiudere il buco servirebbe un driver server-side che alimenti una sessione con barre storiche
(sul modello di `Piootoo.FeedWorker`) e faccia pollare N account simulati in un ordine deterministico.

### Storico barre persistito sul server, per non rimandare il riscaldamento a ogni run

Idea da valutare, **non** implementata. Oggi la storia di una sessione `ExternalBroker` vive in RAM
e muore con la sessione: siccome in backtest `ExecutionKey = BT-{istante di avvio}`, ogni run apre
una sessione nuova e il cBot deve rimandare da capo le `RequiredCandles` barre di riscaldamento
(576 a 15 minuti). Le regole attuali sono in
[`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md).

La variante: il server tiene su disco lo storico per `(simbolo, timeframe)`. Il cBot, al boot,
**chiede lo stato delle barre** (che intervallo il server ha già per i suoi stream), invia solo la
finestra che manca, e il server salva le candele che non ha. Da lì in poi il cBot continua a
mandare sempre le ultime N, ma sapendo che lo storico profondo è già dalla parte del server.

Cosa risolve: riscaldamento pagato una volta sola invece che a ogni run; e lo stesso storico
diventa datafeed riutilizzabile per i backtest locali, che è il lavoro previsto per il cBot
raccoglitore dedicato — le due cose confluiscono.

Da decidere prima di scriverlo:

- **Chi possiede il feed.** Oggi R8 dice esplicitamente che la strada di esecuzione non scrive
  datafeed, perché la qualità dello storico non deve dipendere dagli orari in cui è girato un bot
  di trading. Questa variante rompe quella separazione: o si accetta, o il salvataggio resta al
  solo cBot raccoglitore e la sessione si limita a *leggere* ciò che trova.
- **Fiducia nel feed salvato.** Barre arrivate da un conto demo, da un broker diverso o da una
  sessione interrotta a metà non sono equivalenti. Serve almeno la provenienza per riga, altrimenti
  un backtest locale gira su un miscuglio senza saperlo.
- **Il nuovo endpoint di stato.** `GET /{sessionId}/streams` (o per workspace, se lo storico è
  condiviso fra sessioni) che restituisca per stream primo/ultimo timestamp e numero di barre.
  Attenzione: "ho 600 barre" non basta, servono gli estremi, altrimenti il client non sa se la sua
  finestra si sovrappone e R7 non è verificabile dal suo lato.
- **I buchi interni.** Uno storico su disco può avere vuoti in mezzo, non solo in coda. La regola
  della sovrapposizione (R7) copre la coda; per i buchi interni serve un controllo suo, altrimenti
  si torna esattamente al problema che R6 e R7 esistono per evitare.

### Minori

- Le cartelle sotto `sessions/` non vengono mai ripulite. Ora hanno un nome parlante
  (`{piano}-{executionKey}`), quindi la pulizia è fattibile ma resta manuale.
- Nel bot diretto **non** sono persistiti gli ordini pending né il picco per lo stallo dell'utile:
  ripartono rispettivamente dalla riconciliazione col server e dall'utile corrente.
- `promote-to-backtest` è rimasto utile solo per le sessioni senza piano e per lo storico realtime:
  le sessioni di backtest da piano scrivono già dove Titano legge.
- Il dettaglio del setup Titano usa un `PropertyGrid`. Se si vuole un layout a gruppi come nella
  schermata operativa, le annotazioni `Category`/`DisplayName` sul modello sono già a posto.

## Da verificare al primo build: granularità di volume sulla riga di conversione (2026-08-05)

Chiuso lato codice (motivazioni in [`decisioni.md`](decisioni.md)): la granularità di volume vive
ora sulla riga della tabella di conversione dell'account, non sul piano. Nessuna voce di questa
sessione è stata compilata (vedi avviso in cima al file — file `.exe`/`.dll` bloccati da
un'istanza in esecuzione in Visual Studio): prima di fidarsene, `dotnet build` e
`dotnet test Piootoo.Strategies.Tests`.

Punti da controllare al primo run verde:

- I test toccati (`TradingSessionsHttpTests`, `TitanoSizingAuditTests`, `TradingGroupTitanoTests`,
  `TitanoRotationTests`) sono stati aggiornati a mano per compilare e ragionati a tavolino, non
  eseguiti. `TradingGroupTitanoTests.GroupTitanoProfile_ScalesClaimedQuantityUsingAllocationMultiplier`
  in particolare ha un `return` anticipato quando il manifest sintetico non produce
  un'allocazione parziale (dipende da `trades.json` vuoto): se nel run la formula attesa non torna,
  parti da lì prima di sospettare `RoundQuantity`.
- `SymbolConversionDetailScreen`: le colonne nuove non sono mai state aperte nel designer.
- Nessuna colonna aggiunta alla console legacy: `WorkspaceBacktestingForm` (tab Accounts) non ha
  mai avuto una griglia di conversione inline (solo una combo di selezione, vedi
  `docs/domini/account-e-conversione-symbol.md`), quindi il punto 6 originale non si applicava lì.

## Da fare dopo il prossimo backtest + run cTrader: `MaxEntriesPerSession` per direzione (2026-08-31)

Secondo tempo della correzione del 31/08 sul bracket (voce in [`decisioni.md`](decisioni.md)). Il
primo tempo — il lato dentro i lucchetti del claim — e' fatto e rilasciato come **3.13.0**. Questo
no, ed e' volutamente rimandato al **dopo**: cambia i trade dei backtest gia' fatti, quindi non deve
entrare nel run che serve a misurare il primo tempo.

**Cosa resta rotto.** `MaxEntriesPerSession` conta gli ingressi per (strategia, simbolo, sessione)
senza il verso, in **tutti e due** i motori:

- server: `EntryFillKey(strategyCode, symbol)` in `TradingSessionService`;
- engine interno: `MakeEntrySessionKey(positionKey, ...)` in `PiootooTradingService`, dove
  `positionKey` e' `simbolo|strategia`.

I docstring delle strategie dicono invece «una entrata per sessione **e per direzione**» — e' cosi'
che il limite e' scritto nei run di ricerca da cui sono portate. Con il conteggio attuale, la prima
gamba che si riempie consuma la sessione anche per la gamba opposta.

**Perche' i due vanno mossi insieme.** Sono cechi allo stesso modo, quindi oggi concordano.
Correggerne uno solo li fa divergere: il server diventerebbe piu' permissivo del backtest, e il
confronto interno/esterno ricomincerebbe a misurare due regole diverse invece dello stesso motore.

**Perche' dopo il run.** Cambiare `MakeEntrySessionKey` cambia quali trade produce un backtest: i
run precedenti non sarebbero piu' confrontabili, esattamente come per le 3.10/3.11/3.12. Il run che
verifica il bracket deve girare con l'engine di adesso, altrimenti non si sa quale delle due
modifiche ha prodotto la differenza.

**Ordine dei lavori:**

1. ~~Backtest interno + run cTrader con 3.13.0 da entrambe le parti.~~ **Fatto**, e' `compare-0010`
   (31/08, lug-dic 2024). L'atteso si e' verificato: `PTS_GC_PCH_004_240` fa 28 short contro i 29 del
   backtest, le strategie con zero short esterni scendono da otto a due, e i numeri stanno nella voce
   di `decisioni.md`. Il run e' quindi il riferimento da cui misurare il punto 2.
2. Il verso nelle due chiavi di `MaxEntriesPerSession`, con la propria regressione e la propria voce
   in `decisioni.md`, come release a se'. **Non ancora sbloccato**: vedi la nota sulla suite rossa
   qui sotto.

**Aperto a parte, e piu' grosso di quanto sembrasse:** la suite su `main` e' rossa **da prima** di
questa modifica. Misurato su un worktree di `f56f147`, cioe' il commit precedente: **55 falliti,
551 passati, 606 totali**. Dopo la correzione del bracket: **55 falliti, 554 passati, 609 totali** —
stessi identici fallimenti, i tre in piu' che passano sono `BracketClaimSideTests`.

**Aggiornamento 2026-09-05, su `36b6896`: 43 falliti, 717 passati, 760 totali.** Dodici sono
rientrati e 154 test sono nati da allora; le suite Titano non esistono piu'. Il censimento per
**causa** — piu' utile di quello per suite, perche' dice quali si correggono insieme:

| famiglia | n. | sintomo | lettura |
|---|---|---|---|
| orario di sessione | 11 | `Expected 2024-01-07T17:00:00Z` → `Actual 23:00Z` | il test scrive l'HHMM dichiarato **come se fosse UTC**, il codice lo converte dal fuso dichiarato |
| segnale non emesso | 13 | `Expected Buy` → `Actual Hold` | stessa causa a valle: con la finestra letta nel fuso giusto la barra del test cade fuori |
| valore numerico | 9 | livelli e prezzi (`110.25`→`110.00`, `158`→`168`) | da guardare uno per uno: qui ci sono anche i due di `SourceBacktestSampleTests`, che sono la questione aperta qui sotto |
| sessione HTTP | 8 | `409 Conflict` all'apertura, `0.25`→`1` sul sizing | isolamento fra classi di test e conversione dell'account |
| buco di storia | 2 | `ArgumentException: Buco nella storia di NQ\|15` | la finestra del push non si sovrappone a cio' che il server ha |

**Gli scarti orari non sono casuali**, ed e' l'indizio che tiene insieme le prime due famiglie:
`+6h` e' `America/Chicago` a gennaio (sessione 1700 di ES/NQ), `+5h` e' `America/New_York`, `-1h` e'
`Europe/Rome` (la finestra di ricerca). Sono esattamente le conversioni che `SessionClock` fa e che
i test, scritti prima, non facevano. Se la lettura regge, in quelle 24 righe **il codice e' quello
giusto** — e' l'invariante di `CLAUDE.md`, "gli orari dichiarano il proprio fuso e non si convertono
mai a mano" — e sono i test a descrivere il comportamento di prima di `a4d2d71` («fix varie time di
sessione forse tutto da rivedere»). Va verificata caso per caso prima di riscrivere: aggiornare un
test perche' torni verde e' il modo tipico di cementare un bug.

E' il punto interrogativo del messaggio di commit «refactor vari forse regression?», e va sciolto
**prima** del run del punto 1: con i test di parita' dei motori rossi non si sa se una differenza nel
confronto interno/esterno venga dal bracket o da li'.

## Da decidere: `AccountHasEntryInFlight` segue i lucchetti operativi, o no? (2026-08-31)

Due test si contraddicono, e non e' una svista di uno dei due: descrivono due progetti diversi dello
stesso filtro. Finche' non e' deciso, il codice tiene il comportamento **di produzione** — filtro
incondizionato — e due test restano rossi.

**Tesi A — deve seguire `EnforceConcurrencyLimits`.** La sostengono
`docs/domini/distribuzione-multi-account.md` §4.3 (che porta anche la misura: backtest sorgente NQ del
17/03/2026, nove template per barra, **un solo** claim servito, otto strategie su nove fuori dal
campione) e i due test ancora rossi
`SourceBacktestSampleTests.WithoutOperationalLocks_TheStrategyIsServedAgainOnEveryBar` e
`TheStrategyLimitCountsFills_NotUnexecutedOrders`. L'argomento e' che il campione sorgente deve
contenere tutte le strategie del masterfilter, perche' e' il `trades.json` su cui Titano calcola le
rotazioni: applicargli un vincolo operativo lo falsa.

**Tesi B — deve valere sempre.** La sostiene
`RunProfileTests.BacktestSorgente_NonConsegnaDueIngressiDellaStessaStrategia`, che oggi passa.
L'argomento e' il doppione reale del 14/10/2024 (PTS_NQ_PCH_002_15, due stop riempiti allo stesso
prezzo): a lucchetti spenti niente lo fermerebbe.

**Perche' la scadenza non le concilia.** §4.3 sostiene che `PurgeExpiredEntryIntents` toglie la
condizione che genera il doppione. Non basta: sulla barra N+1 l'intent della barra N e' ancora
**dentro** la propria finestra — `ExpiresAtUtc` e' l'apertura dell'ultima barra valida e il confronto
e' conservativo, come dice la nota in `SourceBacktestSampleTests` — quindi muore solo su N+2. Nella
barra N+1 o lo blocca il filtro, o il claim consegna il secondo ordine. Non c'e' una terza strada che
non sia cambiare la convenzione di scadenza, che a sua volta romperebbe
`AnExpiredEntry_ReleasesTheStrategyOnceItsWindowCloses`.

**Cosa serve per decidere**, ed e' una domanda di dominio, non di codice: nel run sorgente, due ordini
della stessa strategia e dello stesso lato vivi insieme su barre diverse sono un campione **piu'**
fedele (il motore quel livello lo riemette davvero) o **meno** fedele (il conto vero non li avrebbe
mai entrambi, perche' in produzione i lucchetti sono accesi)? Se vale la prima, tesi A e si aggiorna
`RunProfileTests`; se vale la seconda, tesi B e si aggiornano §4.3 e i due test di
`SourceBacktestSampleTests`. In entrambi i casi va corretto il documento di dominio, che oggi descrive
un codice che non esiste.

## Riferimenti codice

`PiootooApp.Server/Controllers/WorkspaceController.cs`,
`Piootoo.Core/Services/WorkspaceService.cs`,
`Piootoo.Core/Services/TradingSessionService.cs`,
`piootooapp.clientform/Shell/NavigationRegistry.cs`,
`piootooapp.clientform/Shell/Screens/BacktestListScreen.Designer.cs`,
`piootoo-repository/ctrader/PiootooDirectExecutionBot.cs`.
