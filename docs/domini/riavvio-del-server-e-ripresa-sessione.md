# Riavvio del server e ripresa di una sessione realtime

Cosa si rompe quando il processo `PiootooApp.Server` si riavvia mentre una
sessione `ExternalBroker` è viva su cTrader, e con quale procedura si torna allo
stato di prima senza lasciare posizioni scoperte.

Riguarda solo le sessioni **realtime** (`ClientRunMode = Realtime`,
`ExecutionMode = ExternalBroker`). Un backtest via cBot apre sempre una sessione
nuova per costruzione — vedi [`trading-sessions-api.md`](trading-sessions-api.md)
— e non ha niente da riprendere.

Prerequisiti: [`trading-sessions-api.md`](trading-sessions-api.md) per il ciclo
di vita, [`finestra-candele-e-riscaldamento.md`](finestra-candele-e-riscaldamento.md)
per la storia delle candele, [`distribuzione-multi-account.md`](distribuzione-multi-account.md)
per lo stato di gruppo.

> **Stato al 04/09/2026.** Implementate: la **fase 0** (dump), la **fase 1**
> (reidratazione con id e token stabili), il riscaldamento autoguarente del §4
> fase 2, e il presidio della §8. Restano da scrivere: l'epoca esplicita, la
> quarantena e la riconciliazione (fasi 2 e 3) — la §4 fase 1 spiega **perché la
> quarantena non è stata implementata insieme alla reidratazione**. La §1
> descrive il comportamento *prima* di questo lavoro, e vale ancora per il cBot
> diretto. Le voci aperte stanno in [`lavori-in-corso.md`](../lavori-in-corso.md).

---

## 1. Cosa si perde oggi

La mappa delle sessioni vive in RAM: `_sessions` e `_planExecutions` sono due
`ConcurrentDictionary` di `TradingSessionService`, e niente le ricostruisce
all'avvio. Al riavvio il primo `open-plan` del cBot non trova la sua execution
key e **crea una sessione nuova, con un `SessionId` nuovo**.

Da lì scendono tre danni, in ordine di gravità.

**Il cBot butta via le condizioni di uscita.** Il file di stato locale del bot è
ancorato al `SessionId`: `anchored` confronta l'id salvato con quello appena
risolto, e se non coincidono `RestoreExitConditions` non viene nemmeno chiamato.
Un riavvio del *server* fa quindi perdere al *client* break-even, trailing stop,
`CloseAtUtc`, `ProfitStallAfterUtc` e `MaxBarsInPosition` di ogni posizione
aperta. Le posizioni restano a mercato con i soli SL/TP nativi del broker, e
nessun errore lo dice. Su un catalogo dove il trailing è la causa di uscita di
circa un trade su tre — vedi [`trading-sessions-api.md`](trading-sessions-api.md)
§"Intent di ingresso e chiusure" — non è una degradazione, è un'altra strategia.

**La sessione nuova resta muta.** Il riscaldamento è governato da un flag del
client (`pair.WarmedUp`): se il bot resta vivo mentre il server si riavvia, non
rimanda mai la storia profonda, e la sessione nuova riceve soltanto le finestre
incrementali da venti barre. `StrategyEvaluationService` salta in silenzio finché
`history.Count < RequiredCandles`: per `PTS_NQ_VBO_002_240` sono 606 barre a 240
minuti, cioè **settimane** di silenzio senza un messaggio. È lo stesso silenzio
che descrive [`finestra-candele-e-riscaldamento.md`](finestra-candele-e-riscaldamento.md),
con la differenza che qui non lo causa un run corto ma un riavvio.

> **Chiuso sul percorso distribuito.** `PiootooDistributedExecutionBot` ora
> confronta le candele che il server dichiara di avere con quante il
> riscaldamento gliene aveva spedite: se ne ha meno, le ha perse, e il bot
> rimanda la storia profonda da sé. Il confronto è con `WarmUpBarsSent` e non con
> `RequiredCandles` perché su uno stream di cui il broker non ha tutta la storia i
> due numeri non coincidono, e usare il secondo rimanderebbe la finestra a ogni
> barra per sempre. Il cBot **diretto** non ha riscaldamento e resta come prima.

**Quello che succede durante il buco è perso.** `TrySendReport` e
`RegisterExternalCloseAndReport` sono fire-and-forget: se la POST fallisce, il
bot stampa e prosegue. Fill e chiusure avvenuti mentre il server era giù non
arrivano mai, e non arriveranno dopo: non c'è coda né ritentativo. Il dump da
solo non copre questo caso — serve la riconciliazione della fase 3, o un outbox
sul bot.

Un quarto effetto è cosmetico ma disorienta: la cartella della sessione ha nome
stabile (`sessions/{piano}-{executionKey}/`), quindi la sessione nuova **riusa la
cartella della vecchia** e continua a scriverci dentro. Gli artefatti non si
azzerano — `Initialize()` riazzera solo sul percorso di backtest — ma il
`session-summary.json` che ne esce descrive due esecuzioni cucite insieme.

---

## 2. Come si riconosce una posizione: la label

È la domanda da cui dipende tutta la riconciliazione, e la risposta non è la
stessa per i due cBot: **il percorso distribuito è già a posto, quello diretto
no.**

Ci sono tre identità in gioco, e non coincidono.

**Sul broker** una posizione è un `Position.Id` di cTrader.

**Sul server** una posizione è una chiave di `ExternalPositions`, costruita a
mano nei due punti che la scrivono: `symbol|strategyCode` senza gruppi,
`account|symbol|strategyCode` in multi-account — dove il prefisso serve proprio a
lasciare che più conti detengano la stessa strategia senza sovrascriversi. Ne
discende un vincolo strutturale che vale la pena rendere esplicito: **il server
può tenere una sola posizione per (conto, strategia, simbolo)**. Due ingressi
contemporanei della stessa strategia sullo stesso simbolo e sullo stesso conto
sono, per il server, una posizione sola.

**In volo** il legame è l'`IntentId` (`{sessionId}-{seq:D10}`): il cBot lo tiene
in RAM e lo rimanda in ogni `execution-report`. Finché il bot non si riavvia, la
corrispondenza è esatta in entrambi i percorsi. La differenza è cosa sopravvive
al riavvio.

### Il percorso distribuito porta già l'identità

`PiootooDistributedExecutionBot` scrive label
`PiootooLive:{StrategyCode}:{IntentId}` (`MakeLabel`, `ParseLabel`) e a **ogni
poll di claim** spedisce lo stato del broker dentro `AccountSignalPollRequest`:
`Positions`, `Orders` e `Trades`, ciascuno con il proprio id e l'`IntentId` letto
dalla label. Il server lo consuma in `ReconcileVanishedPositions`, che conferma
le posizioni comparse almeno una volta nello snapshot
(`BrokerConfirmedPositions`) e chiude quelle sparite, liberandone gli slot di
gruppo.

È già, in piccolo, la fase 3: identità esatta e riconciliazione continua. Quello
che le manca è il resto della tabella — i pending non vengono giudicati, e
`BrokerTradeSnapshot` porta solo `PositionId` e `ClosingTimeUtc`, quindi una
chiusura avvenuta mentre il server era giù si può rilevare ma non contabilizzare
al prezzo vero.

### Il percorso diretto non porta niente

`PiootooDirectExecutionBot` scrive `PiootooSession:{strategyCode}` (`MakeLabel`,
`IsOurs`, `ExtractStrategyCode`): **solo il codice strategia**, e non ha un poll
di claim, quindi non manda mai al server lo stato del broker. Il server sa di
quel conto soltanto ciò che gli execution report gli hanno detto, e non lo
verifica mai contro cTrader.

La label basta per il percorso normale di chiusura:
`RegisterExternalCloseAndReport` estrae la strategia e chiama `close-external`
con `(StrategyCode, Symbol)`, che è la chiave del server — per questo la chiusura
funziona anche su posizioni aperte prima di un riavvio del bot.

Non basta per il riaggancio all'intent. Dopo un riavvio,
`ReconcileExistingPositionsAndOrders` ricostruisce `_positionIntent` con
`FindLatestMatchingIntent`, cioè *"l'intent più recente, non-close, con quella
strategia e quel simbolo"*: sceglie per data, non per identità. Con un solo
ingresso per strategia indovina sempre; con due ingressi ravvicinati, o con gli
ordini riemessi a ogni barra dai motori Unger, può agganciare la posizione
all'intent sbagliato — e l'intent sbagliato porta SL/TP, `CloseAtUtc` e
`MaxBarsInPosition` sbagliati. Non dà errore: dà una posizione sorvegliata con i
parametri di un ordine che non è il suo.

**Correzione richiesta**: allineare il bot diretto al formato che il distribuito
già usa, `{prefisso}:{strategyCode}:{intentId}`, mantenendo
`ExtractStrategyCode` retrocompatibile con la label a due campi — altrimenti la
migrazione stessa orfanizza le posizioni già a mercato. Il prefisso resta diverso
fra i due bot ed è giusto così: distingue chi ha aperto la posizione.

Resta un caso che nessuna label copre: una posizione aperta **a mano**
sull'account. Va lasciata fuori da tutto — `IsOurs` già la esclude — e dichiarata
nel presidio della §8, perché altrimenti la si legge come una posizione del
sistema andata perduta.

---

## 3. Cosa si può ricostruire, e cosa no

In `ExternalBroker` la valutazione di una strategia è funzione di tre cose sole:
la **storia** dello stream, la **posizione** aperta e il contatore `Entries`.
`GetExecution`, nel ramo esterno, costruisce lo snapshot da `ExternalPositions` e
non legge il `RuntimeState` catturato in `SimulatedEngine` — quel percorso è solo
di `ServerSimulated`. Le strategie sono stateless.

Ne discende la regola di cosa dumpare.

**La storia delle candele non si dumpa.** È RAM-only per progetto e la verità sta
su cTrader: la rimanda il client. Dumpare 606 barre per quaranta stream
duplicherebbe un dato già disponibile, con il rischio peggiore — reidratare una
storia stantia e credere di essere caldi quando non lo si è.

**Si dumpa lo stato che il client non può ricostruire.** È compatto, poche decine
di record per sessione:

| Blocco | Campi |
|---|---|
| Identità | `Id`, `Token`, `PlanCode`, `ExecutionKey`, `ClientRunMode`, `Mode`, `RunProfile`, `WorkspaceId`, `DirectAccountNumber`, `JoinedAccounts`, `CreatedAtUtc`, `Status` |
| Ordini in volo | `LiveIntents` con la loro copia in `Intents`/`IntentsById`, `UnsettledIntents` |
| Posizioni | `ExternalPositions`, `ExternalPositionDetails`, `BrokerConfirmedPositions`, `CanonicalPositions`, `StrategyHolderCounts` |
| Contatori | `Entries`, `Fills`, `IntentSequence`, `ActivitySequence`, `PeakEquity`, `EntryFills`, `StrategyNetPnl`, `HistoryHighWater`, `FirstBarUtc`, `LastBarUtc`, `LastEvaluatedBarTimeUtc` |
| Deduplica | `LastSequence` per stream, `BarKeys` e `ReportIds` **potati a finestra** (24h): senza potatura crescono per tutta la vita della sessione |
| Multi-account | `EntryTemplates`, `TemplateClaimedGroups`, `GroupStrategySlots`, `AccountGroups`, `AccountMaxConcurrentTrades`, `AccountConcurrencyCountMode` |
| Impronta | hash di piano, masterfilter e codici strategia risolti |

`ExternalTrades` non entra nel dump: sta già in `trades.json` e si rilegge da lì,
con `CompactAll()` prima, per l'invariante del journal.

L'**impronta** copre un caso che altrimenti passa in silenzio: piano o
masterfilter modificati mentre il server era giù. Le strategie della sessione si
ricostruiscono dal piano, e riagganciare posizioni aperte a un catalogo diverso
significa sorvegliarle con regole che non sono quelle con cui sono nate. Impronta
diversa = **niente ripresa**: sessione nuova, e la ragione scritta nel monitor.

---

## 4. La procedura, in quattro fasi

### Fase 0 — Dump *(implementata)*

Accanto a `signals.json` e `trades.json` c'è `session-state.json`, scritto dal
`TradingJsonStore` con `AtomicFileWriter`. Lo scrive `PersistState`, chiamata
dall'inizio di `Persist` — cioè da ogni punto che già segnalava "qualcosa è
cambiato": barra valutata, execution report, chiusura esterna, claim.

**Il dump non passa dal throttle degli artefatti**, ed è la scelta che conta: quel
throttle esiste per non riscrivere decine di MB a ogni barra, mentre qui si
scrivono qualche decina di record, e la cadenza giusta è "ogni volta che qualcosa
è cambiato". Scrivere da un punto solo invece che dai cinque eventi che contano è
ciò che impedisce di dimenticarne uno.

Il dump vale **solo per le sessioni realtime aperte da piano**. Un backtest apre
sempre una sessione nuova per costruzione e non ha niente da riprendere; una
sessione creata a mano senza piano non ha una configurazione da cui ricostruirsi,
quindi il suo dump sarebbe un file che nessuno potrà mai rileggere.

Un errore di scrittura è silenzioso di proposito: il dump è una rete di
sicurezza, e far fallire un execution report perché il disco è pieno rovescia la
priorità. Che manchi si vede al riavvio, dove la sessione semplicemente non
riprende — ed è un evento rumoroso.

### Fase 1 — Reidratazione *(implementata)*

`ITradingSessionService.RestoreSessions()` scandisce
`<workspace>/sessions/*/session-state.json`, e per ogni dump valido ricostruisce
la sessione **con lo stesso `Id` e lo stesso `Token`**. La chiama `Program.cs`
subito dopo `builder.Build()` e **prima** di `app.Run()`: non un `IHostedService`
e non `ApplicationStarted`, perché entrambi possono girare quando il server ha
già iniziato ad accettare richieste, e un cBot che facesse `open-plan` in quella
finestra aprirebbe una sessione nuova accanto a quella che stava per essere
ripresa.

La ricostruzione **non duplica la configurazione**: risolve il piano e ripassa da
`CreateCore` con la stessa richiesta che costruisce `OpenFromPlan`
(`BuildPlanSessionRequest`, estratta apposta perché le due non possano divergere).
Quindi strategie, sizing, holding e moltiplicatore vengono dal piano, non dal
file. `store.Initialize()` viene saltato: azzererebbe proprio i due artefatti che
il dump non duplica perché sono già lì.

Ogni cartella viene tentata da sola e ognuna restituisce un
`SessionRestoreOutcome` che finisce nel log di avvio, ripreso o no. Una sessione
che non riprende è una posizione che resta senza sorveglianza lato server: deve
essere rumorosa, non un silenzio.

Si rifiuta di riprendere quando: l'impronta non torna (§3), il piano non è più
risolvibile, la sessione era `Stopped` — riprenderla in esecuzione la rimetterebbe
a mercato senza che nessuno l'abbia chiesto — o non è realtime. Un rifiuto non
lascia mezza sessione in memoria.

Sul percorso di scrittura resta una conseguenza da conoscere: su una sessione
ripresa `session.Intents` contiene i **soli ordini in volo** del dump, non la
storia. La scrittura autorevole degli artefatti diventa quindi un `Upsert` invece
di una sostituzione (`WriteArtifactsFull`), altrimenti la prima persona che apre
i segnali per leggerli cancellerebbe tutto ciò che la sessione ha prodotto prima
del riavvio. Per la stessa ragione i contatori di `session-summary.json` valgono
dalla ripresa in poi, e la scheda lo dichiara in `diagnostics`.

#### Perché la quarantena non è stata implementata qui

Il progetto prevedeva che la sessione ripresa nascesse in
`RestoredPendingReconcile` e non emettesse ingressi finché il client non avesse
riconciliato. **Non è stato fatto, di proposito**: l'uscita dalla quarantena è la
fase 3, che non esiste ancora, quindi una quarantena implementata oggi
congelerebbe il sistema dopo ogni riavvio — un danno certo per evitarne uno
possibile.

Senza quarantena, la reidratazione resta comunque **migliore del comportamento di
prima in entrambe le direzioni di errore**:

- *prima*: il server ripartiva credendo il conto piatto, quindi rientrava su una
  strategia che a mercato aveva già una posizione → **esposizione doppia**;
- *adesso*: il server sa della posizione e non rientra. Se nel frattempo quella
  posizione si è chiusa al broker, il server continua a crederla aperta e la
  strategia non entra più → **ingresso mancato**, silenzioso.

Il secondo errore costa meno del primo — un costo opportunità contro rischio non
dichiarato — ed è l'unico che resta scoperto. Non resta invisibile: il presidio
della §8 lo mostra come `SessioneRipresaSenzaFlusso` finché il cBot non ricomincia
a spingere barre, e come `PosizioneMaiConfermata` sul percorso distribuito, dove
`ReconcileVanishedPositions` peraltro lo ripara da sé al primo poll.

### Fase 2 — Epoca

Ogni risposta del server porta `SessionId` più un `SessionEpoch`, incrementato a
ogni reidratazione. Il client confronta con quello che ha in mano: epoca diversa
significa "il server è ripartito", e fa scattare il riaggancio completo —
`open-plan`, `WarmedUp = false` su tutti gli stream, snapshot della fase 3.

L'epoca copre anche il caso in cui il server riparta **senza** dump utilizzabile,
che è il caso in cui il client deve accorgersene di più.

Di scorta, e indipendente dall'epoca — **già implementato**: a ogni risposta di
`bars/window` il client confronta le candele che il server dichiara di avere con
quante gliene ha spedite col riscaldamento, e se ne ha meno rimette `WarmedUp =
false` per quello stream. È autoguarigione, non dipende da nessuna delle altre
fasi, e da sola chiude il secondo danno della §1 sul percorso distribuito. È
anche il prerequisito perché la fase 1 serva a qualcosa: una sessione reidratata
non ha storia candele, quindi senza questo resterebbe muta pur essendo viva.

### Fase 3 — Riconciliazione con cTrader

**cTrader è la verità, il dump è solo un'ipotesi.** Non si parte da zero: il
payload esiste già come `AccountSignalPollRequest` (§2) e va generalizzato — reso
disponibile anche alle sessioni dirette, che non hanno un poll di claim, ed
esteso ai pending e al prezzo di chiusura. Il client consegna in una sola
chiamata (`POST /{id}/reconcile`):

- le posizioni aperte, con label, `Position.Id`, prezzo e istante di ingresso,
  volume, SL/TP correnti;
- gli ordini pending vivi, con label e id;
- i **deal chiusi** con `ClosingTime` successivo a `LastKnownUtc`, cioè cosa è
  successo mentre il server non c'era.

Il server fa il diff contro lo stato reidratato, agganciando per label secondo la
§2:

| Caso | Azione |
|---|---|
| Posizione nel dump **e** sul broker | conferma e riaggancia all'intent |
| Posizione nel dump, **non** sul broker | chiusa nel buco: si genera il `Close` intent e il `PersistedTrade` dai dati del deal — prezzo e orario veri, non stimati |
| Posizione sul broker, **non** nel dump | adozione: intent sintetico marcato `Adopted`, così `MaxEntriesPerSession` e i lucchetti di concorrenza la contano |
| Pending nel dump, non sul broker | `Cancelled` |
| Pending sul broker, non nel dump | si **cancella l'ordine**: un ordine di cui il server non conosce la specifica di uscita, se si riempie, apre una posizione che nessuno sorveglia |
| Posizione senza label `PiootooSession:` | non è del sistema: si ignora e si dichiara |

Tutto idempotente per `Position.Id` e id del deal, quindi la chiamata si può
ripetere: una riconciliazione fallita a metà si rifà, non lascia residui.

Nella risposta il server **rispedisce al client la specifica di uscita completa**
degli intent riagganciati e adottati. Serve al terzo scenario della §6, dove il
file locale del bot non c'è più.

---

## 5. Perché la quarantena non è prudenza in eccesso

La sessione reidratata conosce le posizioni aperte del dump, ma non sa se sono
ancora aperte. Se accettasse barre prima della riconciliazione, la valutazione
girerebbe con uno snapshot di posizione potenzialmente falso in entrambe le
direzioni:

- posizione nel dump ma chiusa nel frattempo → la strategia si crede in posizione
  e non entra più, per sempre, senza dirlo;
- posizione chiusa nel dump ma aperta a mercato → la strategia entra di nuovo e
  raddoppia l'esposizione.

Il secondo è il motivo per cui il default è *non operare*: un ingresso mancato è
un costo opportunità, un doppione è rischio non dichiarato.

---

## 6. I tre scenari

**a) Server riavviato, cTrader invariato.** Fase 1 restituisce la stessa sessione
con lo stesso id e token; fase 2 fa rimandare il riscaldamento; fase 3 produce un
diff vuoto e toglie la quarantena. Il cBot conserva le condizioni di uscita
perché l'ancora ha retto. Continuità piena.

**b) Server riavviato, cTrader cambiato.** Il diff non è vuoto e la tabella della
fase 3 lo assorbe: le chiusure avvenute nel buco entrano come trade veri, presi
dai deal; i pending morti si chiudono; le posizioni sconosciute si adottano.
`MaxEntriesPerSession` e i lucchetti di concorrenza tornano coerenti con il conto
vero e non con il dump.

**c) Server e cBot riavviati insieme** — tipico se stanno sulla stessa macchina.
Con la fase 1 fatta questo è diventato lo scenario **migliore**, non il peggiore,
e vale la pena capire perché il verso si è rovesciato.

*Prima*: il server ripartiva con un `SessionId` nuovo, quindi
`RestoreLocalState` del cBot — che confronta l'id salvato con quello appena
risolto — scartava il proprio file e stampava «stato locale ignorato: appartiene
a una sessione diversa». Break-even, trailing, `CloseAtUtc` e
`MaxBarsInPosition` di ogni posizione aperta sparivano, **e non tornavano più**:
non per la durata del riavvio, per sempre.

*Adesso*: il server riprende lo stesso id, `open-plan` del bot rientra nella
stessa sessione, il file locale è di nuovo ancorato e le condizioni di uscita si
riapplicano — filtrate su ciò che è ancora sul broker, quindi le posizioni chiuse
a mano nel frattempo spariscono da sole.

In più il riavvio del bot ripercorre l'intera sequenza di avvio, che è la cosa
più vicina alla fase 3 che oggi esista: `LoadHistoryBackwards` +
`SendWarmUpWindow` incondizionati (nessuna dipendenza dall'euristica del §4 fase
2), `RestoreLocalState` filtrato sulle posizioni vere, e — sul bot diretto —
`ReconcileExistingPositionsAndOrders`, che rilegge `GET /intents?after=0` e
riaggancia posizioni e pending.

Il prezzo è la finestra in cui il bot è spento: lì **nessuno applica le uscite**
diverse da SL/TP nativi, perché in `ExternalBroker` le sorveglia il client e non
il server. È il motivo per cui il bot, quando il server cade, resta acceso
apposta.

Da cui la regola operativa: **riavvio breve → riavvia entrambi**, il conto e i
libri del server restano allineati e non resta nessun buco silenzioso;
**indisponibilità lunga → lascia acceso il bot**, che continua a gestire le
uscite, sapendo che le barre di quella finestra non verranno valutate e che i
fill di quella finestra il server non li saprà mai (fire-and-forget, §1). Il
secondo buco lo chiude solo l'outbox del §7 punto 5.

---

## 7. Ordine di implementazione

Le fasi hanno valore da sole, in quest'ordine:

1. ~~riscaldamento autoguarente~~ — **fatto** su `PiootooDistributedExecutionBot`;
   resta da portare sul bot diretto, che però non ha riscaldamento del tutto;
2. ~~`session-state.json` e reidratazione con id e token stabili~~ — **fatto**;
3. epoca e quarantena `RestoredPendingReconcile` — la quarantena ha senso solo
   insieme al punto 4, che è la sua unica uscita (vedi §4 fase 1);
4. label con il progressivo dell'intent e `POST /reconcile` con il diff completo;
5. outbox persistente sui cBot per gli execution report, che copre il buco anche
   fuori dal riavvio.

---

## 8. Il presidio dalla console (implementato)

Finché le fasi 1-4 non esistono, resta il problema operativo: **capire, dopo un
riavvio, se su cTrader c'è una posizione che nessuno sta più gestendo**. La
console lo mostra in *Operatività → Presidio realtime*, una schermata per account.

Cosa può dire, e cosa no. La console parla solo HTTP con l'API e **non vede
cTrader**: non può confermare né smentire una posizione. Quello che può fare è
dichiarare cosa il server crede, da quanto tempo non lo verifica, e trasformarlo
in un verdetto. Le regole, in ordine di gravità:

| Verdetto | Quando | Cosa fare |
|---|---|---|
| `SessioneAssente` | nessuna sessione realtime per l'account, ma il piano ne prevede una | aprire cTrader: ogni posizione con label `PiootooSession:` è fuori dal controllo del server |
| `SessioneNonInEsecuzione` | `Status != Running` con posizioni aperte | far ripartire la sessione, o chiudere a mano |
| `ChiusuraAttesaNonAvvenuta` | `CloseAtUtc` passato e posizione ancora aperta per il server | controllare su cTrader e chiudere a mano se aperta |
| `OltreIlFlatDiConto` | posizione aperta oltre il flat di sessione o di fine settimana permesso dal piano | idem, con la stessa urgenza |
| `FlussoFermo` | ultima barra più vecchia di un multiplo del timeframe più fitto, **fuori** dalla finestra di fine settimana | verificare che il cBot giri: il server è cieco, e con lui la sorveglianza lato server |
| `PendingScaduto` | intent `Pending` la cui barra di validità è passata senza report | l'ordine può essere ancora a mercato: cancellarlo a mano |
| `SessioneRipresaSenzaFlusso` | sessione reidratata dopo un riavvio, e da allora nessuna barra | verificare che il cBot sia acceso: posizioni e ordini elencati vengono dal dump, non da una lettura del conto |
| `Presidiata` | sessione viva, ultima barra recente, nessuna anomalia | niente |

I verdetti si calcolano sul server (`GET /api/v1/trading-sessions/accounts/{n}/watch`)
e non nella schermata: sono la stessa domanda che si porrà la riconciliazione
della fase 3, e vanno scritti una volta sola. La schermata li ordina per gravità
e mostra, per ogni riga, il dato grezzo su cui il verdetto poggia — chi deve
decidere se toccare cTrader a mano non può farlo su un semaforo senza numeri.

Nessuna riga afferma che una posizione **è** ancora aperta. Il testo dice sempre
"per il server", ed è la sola formulazione onesta finché la fase 3 non esiste.

**A mercato chiuso il presidio abbassa la voce, non la alza.** Dentro la finestra
di fine settimana `FlussoFermo` tace e `SessioneRipresaSenzaFlusso` scende ad
Attenzione: il silenzio delle barre è un fatto del calendario, e un cBot acceso e
uno spento lo producono identico, quindi nessun intervento è né possibile né
sensato prima della riapertura. La guardia dipende dalla finestra e **non** da
`AllowOverweek`: condizionarla alla policy — come faceva la prima stesura —
significava suonare l'allarme proprio al conto che il fine settimana lo tiene,
cioè all'unico che il sabato ha davvero posizioni aperte, per due giorni di fila
ogni settimana.

---

## Riferimenti codice

- `Piootoo.Core/Services/TradingSessionService.cs` — `Session`, `_sessions`,
  `_planExecutions`, `OpenFromPlan`, `Persist`, `WriteArtifactsFull`,
  `GetExecution`, `ReconcileVanishedPositions`, `BuildAccountWatch`
- `Piootoo.Core/Services/RealtimeWatchRules.cs` — le regole dei verdetti, pure e
  testabili a parte
- `Piootoo.Core/Services/TradingSessionService.cs` — `PersistState`,
  `BuildSessionState`, `BuildExecutionIndexKey`, `BuildConfigurationFingerprint`,
  `RestoreSessions`, `RestoreSession`, `ApplySessionState`, `RestoreContext`,
  `BuildPlanSessionRequest`
- `Piootoo.Shared/Models/Trading/SessionStateContracts.cs` — `SessionStateFile`,
  `SessionStatePosition`, `SessionStateEntryFill`, `SessionRestoreOutcome`
- `Piootoo.Core/Services/TradingJsonStore.cs` — `WriteSessionState`,
  `ReadSessionState`, `WriteSessionSummary`, `CompactAll`
- `PiootooApp.Server/Program.cs` — la chiamata a `RestoreSessions()` prima di `app.Run()`
- `Piootoo.Strategies.Tests/SessionRestoreTests.cs` — il giro completo dump →
  riavvio → ripresa → riaggancio del cBot
- `Piootoo.Shared/Models/Trading/TradingSessionContracts.cs` —
  `AccountSignalPollRequest`, `BrokerPositionSnapshot`, `BrokerOrderSnapshot`,
  `BrokerTradeSnapshot`
- `Piootoo.Shared/Models/Trading/AccountRealtimeWatch.cs` — contratti del presidio
- `PiootooApp.Server/Controllers/TradingSessionsController.cs` —
  `GET /accounts/{accountNumber}/watch`
- `piootoo-repository/ctrader/PiootooDirectExecutionBot.cs` — `MakeLabel`,
  `IsOurs`, `ExtractStrategyCode`, `LoadSessionState`, `SaveSessionState`,
  `RestoreExitConditions`, `ReconcileExistingPositionsAndOrders`,
  `FindLatestMatchingIntent`, `TrySendReport`
- `piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs` — `MakeLabel`,
  `ParseLabel`, lo stato broker spedito col poll di claim; `SendWarmUpWindow`,
  `SendWindow`, il flag `WarmedUp`
- `piootooapp.clientform/Shell/Screens/RealtimeWatchScreen.cs` — il presidio
