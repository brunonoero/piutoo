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
| Piccola e seria | `Entries`, `Fills`, `IntentSequence`, `ExternalPositions`, `AccountActiveIntent`, `GroupStrategySlots`, `PeakEquity` | poche centinaia di byte; perderla azzera `MaxEntriesPerSession` e libera slot di concorrenza già occupati, in silenzio |
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

## Riferimenti codice

`PiootooApp.Server/Controllers/WorkspaceController.cs`,
`Piootoo.Core/Services/WorkspaceService.cs`,
`Piootoo.Core/Services/TradingSessionService.cs`,
`piootooapp.clientform/Shell/NavigationRegistry.cs`,
`piootooapp.clientform/Shell/Screens/BacktestListScreen.Designer.cs`,
`piootoo-repository/ctrader/PiootooDirectExecutionBot.cs`.
