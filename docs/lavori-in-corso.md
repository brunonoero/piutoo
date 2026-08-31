# Lavori in corso

Stato al **2026-08-04**. Questo file è volutamente deperibile: quando una voce è chiusa si
cancella da qui, e la motivazione della scelta resta in [`decisioni.md`](decisioni.md). Se una
sezione qui contraddice il codice, ha ragione il codice.

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

### Ripresa di una sessione dopo il riavvio del server

`TradingSessionService` tiene le sessioni in RAM. L'idempotenza di `open-plan` copre il riavvio del
cBot, non quello del processo server. Persistere la sola identità non basta e sarebbe peggio del
comportamento attuale: la `Session` contiene l'istanza mutabile di `PiootooTradingService`, la
`History` per stream e i lucchetti di claim, quindi una sessione "ripresa" senza storia rivaluterebbe
le strategie con gli indicatori vuoti, producendo segnali plausibili e sbagliati fino a riempire le
finestre. Il fallimento attuale è almeno visibile.

Tre semantiche possibili, da scegliere prima di scrivere codice:

1. **Ricostruzione dalla storia** — si persiste `History` e si rigioca la sessione fino a "adesso"
   prima di riaprirla. Corretta, richiede un replay deterministico.
2. **Ripresa dichiarata degradata** — si riprende identità e cartella, il descriptor dichiara che lo
   stato runtime è perso e il cBot riconcilia dal broker; limite barre e stallo utile ripartono da
   zero.
3. **Rifiuto esplicito** — la vecchia execution key non è riutilizzabile e il cBot apre una sessione
   nuova, con un errore che lo dice.
4. **Warm-up replay dal client** — variante della 1 che non persiste `History`. Si salva il solo
   stato piccolo (vedi sotto); al resume la sessione riparte in **warm-up**: accetta barre e non
   emette intent finché ogni stream non ha storia sufficiente, e a riempirla è il cBot che ripusha
   la propria finestra storica. Non è lavoro nuovo lato client — `PiootooDistributedExecutionBot`
   ha già il parametro *finestra storica* e cTrader tiene le barre in memoria — e il replay è
   deterministico perché le barre chiuse sono immutabili e `POST /bars` è già deduplicato per
   idempotency key e sequence. Il fallimento resta visibile: se il client non ripusha, la sessione
   dichiara warm-up e non opera, invece di operare male.

Lo stato in RAM non è omogeneo, e la scelta cambia a seconda della fetta:

| Fetta | Contenuto | Nota |
|---|---|---|
| Già su disco | `trades.json`, `signals.json`, condizioni di uscita del bot diretto (04/08) | niente da fare |
| Piccola e seria | `Entries`, `Fills`, `IntentSequence`, `ExternalPositions`, `GroupStrategySlots`, `PeakEquity` | poche centinaia di byte; perderla azzera `MaxEntriesPerSession` e libera slot di concorrenza già occupati, in silenzio |
| Grossa e velenosa | `History` per stream, istanza mutabile di `PiootooTradingService` | è il buffer di barre che il **client ha già**: persisterlo è duplicare |

Se e quando si interviene, l'ordine per costo crescente è: rendere esplicito il fallimento (opzione
3, `409` con messaggio che dichiara lo stato runtime non ricostruibile, invece di una sessione nuova
che sembra la vecchia); poi persistere i soli contatori di rischio, che sono l'unica perdita che fa
*aprire* trade che non si dovrebbero aprire; il warm-up replay per ultimo. La scrittura va fatta con
`AtomicFileWriter` in `<workspace>/sessions/{piano}-{key}/session-state.json`, **fuori dal loop
barre** (§ invarianti).

La persistenza locale delle condizioni di uscita nel bot diretto (04/08) copre già buona parte del
rischio live a costo molto minore, quindi la scelta non è urgente. L'architettura è già "il client
sopravvive al server" — uscite persistite lato bot, riconciliazione col broker all'avvio,
`FlatAtWeekEnd` che vale anche a server irraggiungibile — ed è la postura giusta per un server che
gira su macchina locale.

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

Dove sono concentrati:

| suite | falliti |
|---|---|
| `TradingSessionsHttpTests` | 8 |
| `RunProfileTests` | 8 |
| `SourceBacktestSampleTests` | 5 |
| `PriceChannelEngineTests` | 5 |
| `BiasWeeklyEngineParityTests` | 5 |
| `BiasBarCountEngineTests` | 4 |
| parita' motori (VBO, TFM/TFU, SBO, RHL, LFD, TDV, RBB, MAC, PCH) | 13 |
| Titano e concorrenza (`TradingGroupTitano`, `TitanoSizingAudit`, `TitanoRotation`, `ConcurrencyLimitsMatrix`) | 4 |
| altri (`EasyEngineContract`, `BiasWeeklyVariants`, `PtsPriceChannel`) | 3 |

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
