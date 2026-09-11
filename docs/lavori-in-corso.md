# Lavori in corso

Stato al **2026-09-08**. Questo file è volutamente deperibile: quando una voce è chiusa si
cancella da qui, e la motivazione della scelta resta in [`decisioni.md`](decisioni.md). Se una
sezione qui contraddice il codice, ha ragione il codice.

---

# ⇦ RIPRENDERE DA QUI: due misure sul paniere

Sessione del 08/09/2026 interrotta qui, di proposito. Il **codice è finito e verde**; quello che
manca sono due run, e nessuno dei due è stato lanciato.

Piano completo e stato passo per passo in
[`domini/layer-barre-e-calendario.md`](domini/layer-barre-e-calendario.md) §7. Il metodo è sempre
lo stesso e va tenuto: **si misura prima**, si cambia, si rimisura sul paniere completo.

## Le due misure che mancano

Servono due run `all-in` su `FTMOPLATFORM`, un anno, prima/dopo. **Indipendenti**: tenerli separati
o non si sa quale numero venga da cosa.

1. **Passo 5** — `MaxBarsInPosition` sul calendario e l'uscita a indice di barra come conteggio di
   barre vere. Riguarda 62 strategie su 124.
2. **Etichetta della barra** — la finestra, il filtro del giorno e le pause leggono ora la chiusura.
   Riguarda 83 strategie su 124 per la finestra, tutte e 13 le giornaliere per il giorno. Il
   confronto si fa con `BacktestingRequest.LegacyBarOpenLabels = true` contro il default.

Servono a **dichiarare** di quanto si spostano i risultati, non a decidere: le due modifiche sono
correzioni di difetti, non opzioni. E dicono quali cartelle archiviate non sono più confrontabili.

## Fatto l'08/09/2026

- **Passo 5 completo** — `MaxBarsInPosition` conta le barre della strategia sul calendario,
  `ScaleSignalMaxBarsInPosition` eliminato, `SessionBarToUtc` sostituito da un conteggio di barre
  vere in `BiasBarCountEngine.WithExit`. `MaxDaysInTrade`/`MaxDaysFlatTime` verificati **codice
  morto**: zero PTS su 124 li valorizzano.
- **Passo 7** — il riscaldamento di una sessione lo legge il server dal disco
  (`datafeed-external/{BROKER}/@SYM_1.json`). R2 e R3 di
  [`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md)
  **restano**: chiudono col passo 6-operativo.
- **L'etichetta della barra in un punto solo** — `SessionClock.BarLabelHhmm`/`BarLabelDay`, e
  `EasyLib.TimeWindow` prende un `HHMM` e non più una barra, così nessun chiamante può rispondere
  per conto proprio. Il flag è rovesciato (`LegacyBarOpenLabels`, default `false`): **il default è
  quello corretto**, quindi la sessione live non deve impostare niente. La guida di conversione,
  per le strategie che arriveranno, è in
  [`domini/porting-da-report-sweep.md`](domini/porting-da-report-sweep.md) §"La regola degli orari".
- **Cancellati**: le 21 classi PTS su simboli fuori calendario (HK, HO, JY),
  `ResearchSessionStartConformanceTests`, il motore a **rotazione settimanale** (`TradingEngine`,
  `StrategyRotationManager`, `WeeklyRotationScheduler` e i tre endpoint che lo esponevano — era un
  terzo percorso di valutazione senza niente dell'ultimo mese), e la **SPA Angular**.

Suite: **931 test, 40 rossi preesistenti**, nessuno nuovo in nessuno dei cinque commit. Tutto su
`main` (`origin/main` allineato l'08/09/2026).

## Il resto, in ordine

- **Il feed `@FDAX_240.json` è ancorato a 00:00 e il calendario dice 01:00.** Misurato: 20.795
  barre, tutte su ore locali `{0,4,8,12,16,20}`. Il file è del 25/08, `SESSION_START_HOUR` è entrato
  nell'aggregatore il 07/09 e il feed non è stato rigenerato. Si ripara **rigenerando un file**,
  senza toccare codice — `@FDAX: 1` e `@FDAX: (240,)` sono già nelle tabelle di
  `aggregate_flat_feed.py`. FDAX è l'unico ancoraggio-1 con un feed sopra l'ora nell'archivio
  interno: CC, CT, KC e SB non hanno né `_240` né `_1440`, che è un buco diverso.
- **L'estrazione del passo di valutazione.** Backtest e sessione chiamano `strategy.Evaluate`
  ognuno per conto proprio: `RequiredCandles * 1.2` è un letterale in **sei punti**, la regola sulla
  storia insufficiente ha due comportamenti (contata da una parte, silenziosa dall'altra), e le
  convenzioni del motore le imposta solo il backtest. Coincidono oggi solo perché i default
  coincidono. Un punto solo che possieda finestra, regola sulla storia, convenzioni e rifinitura del
  segnale; da lì cade da sé la dichiarazione delle convenzioni nel `session-summary.json`.
- **I sei punti di `verifica-offset-conversione-2026-09-08.md`.** Tre sono chiusi (finestra,
  filtro del giorno, e dall'11/09 BIASW `le_time`/`lx_time`, che leggono l'etichetta di chiusura e
  nascono sulla barra prima). Restano: l'uscita del venerdì di MAC
  (`Hhmm(barEnd) == SessionEndTime` con `SessionEndTime = 2359`, che è una sentinella — il test è
  rosso), e le tre copie di "inizio sessione" (`SessionKey`, `ResolveEntrySessionStartUtc`) che
  arretrano solo se `Start > End`, mai vero per una sessione della ricerca.

## Le altre voci aperte del piano

- **4b — gli engine consumano `BarContext`.** Eliminare `EasyLib.ClassifySessionBar`,
  `FullDaySessionDay`, `ResolveEntrySessionStartUtc` e le sue tre copie nei motori. Include il ramo
  "sessione di borsa" di `ClassifySessionBar`, ora **provatamente irraggiungibile** (tutte e 124 le
  PTS usano la forma a giornata piena). Tocca i motori, non il catalogo.
  Guadagno collaterale: `OHLCMulti5` fa `Where().OrderBy().ToArray()` a **ogni valutazione** — LINQ
  nel punto più caldo del sistema, che CLAUDE.md vieta.
- ~~**6 (parte operativa) — i due cBot di esecuzione.**~~ **Chiuso il 08/09/2026 (7.2.0).** Il bot
  diretto e' stato **tolto** — un distribuito con un simbolo solo e' nelle stesse condizioni, e un
  terzo percorso di esecuzione da tenere allineato non dava niente in cambio — e il distribuito
  legge ora ancoraggio e fuso dal descriptor (`TradingInstrument.BarGrid`, dal calendario di
  mercato). Spariti `SessionStartHourOf` e i parametri *Fuso dell'ancoraggio* e *Ora di inizio
  sessione*: la tabella §2.4 vive in un punto solo. Uno strumento fuori dal calendario **non fa
  piu' aprire la sessione**.
- **7 — riscaldamento dal disco. Fatto per la meta' che non dipende dal 6.** Il server riempie la
  storia all'apertura leggendo `datafeed-external/{BROKER}/@SYM_1.json`. R2 e R3 di
  [`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md)
  **restano**: finche' i bot operativi mandano barre gia' aggregate il client deve poter
  caricare la propria storia dal broker, ed e' comunque l'unica strada dopo un riavvio del
  server. Chiude col passo 6-operativo.
- **8 — `aggregate_flat_feed.py` produce solo il minuto.** I CSV del vendor sono sul disco
  (`datafeed-future/FUTURES_Historical_Data/`, 200-360 MB per simbolo), quindi non serve nessuna
  raccolta. Chiude da sé il difetto §7quater: una barra giornaliera all'anno per simbolo è fuori
  griglia, sempre l'ultima domenica di ottobre.

## Da fare fuori dal codice

- **Rifare compare-0033 con la 7.2.1** su entrambe le gambe (backtest interno e run cBot
  ricompilato): le cinque correzioni dell'11/09 (`decisioni.md`) cambiano i trade di YM_BIA,
  ES_BSW, dei market su tick senza barra e dei livelli dentro lo spread. Lo strumento di confronto
  è in `piootoo-repository/compare/compare-0033/analisi/strumento/`. Le decisioni ancora aperte
  dal confronto: inversione sul segnale opposto, commissioni per simbolo e conversione EUR→USD nel
  backtest.
- **Ricompilare i cBot in cTrader.** Il salto a 7.0.0 è un cambio di contratto: i due bot operativi
  stampano un disallineamento finché non vengono ricompilati e ridistribuiti. Il raccoglitore
  (2.0.0, numerazione propria) va ricompilato perché è cambiato del tutto.
- **La raccolta a un minuto** può partire quando vuoi: il server nuovo è il prerequisito, perché
  `rebuild-from-minutes` non esiste su quello vecchio e senza aggregati i backtest sopra il minuto
  non trovano il datafeed.

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

## `MaxEntriesPerSession` per direzione — chiuso il 07/09/2026

Il verso sta in entrambe le chiavi (`EntryFillKey` sul server, `MakeEntrySessionKey` nell'engine),
con regressioni in `EntryLimitPerSideTests` e `SessionEntryLimitTests`; voce in
[`decisioni.md`](decisioni.md) 2026-09-07. Questa sezione restava qui per errore e ha fatto
riproporre il punto come aperto l'11/09.

## La suite rossa: stato all'11/09/2026

**13 falliti, 927 passati, 940 totali** (erano 43 su 760 il 05/09, 55 su 606 il 31/08). Le
famiglie «orario di sessione» e «segnale non emesso» del censimento del 05/09 sono rientrate.
Restano:

| famiglia | test | lettura |
|---|---|---|
| sessione HTTP | 8 di `TradingSessionsHttpTests` (`409 Conflict` all'apertura, sizing `0.25`→`1`) | isolamento fra classi di test e conversione dell'account |
| campione sorgente | i 2 di `SourceBacktestSampleTests` | la questione `AccountHasEntryInFlight` qui sotto |
| profilo di run | `RunProfileTests.IlPiano_NonPuoDisarmareILucchetti…`, `IlPushDichiaraQuantoCEDaReclamare` | da guardare uno per uno |
| concorrenza | `ConcurrencyLimitsMatrixTests.ParallelPollsOfTheSameAccount_ProduceExactlyOneClaim` | da guardare |

Regola invariata: un test che torna verde perche' riscritto e' il modo tipico di cementare un
bug; ogni caso va letto contro l'invariante di `CLAUDE.md` prima di toccarlo.

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
