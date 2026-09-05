# Piani di trading

Un piano è una configurazione operativa riutilizzabile salvata nel workspace. Il codice è
univoco tra tutti i workspace perché costituisce l'unico identificatore configurato nel cBot.
Nome e codice sono distinti.

Il piano dichiara il **broker** su cui opera (`BrokerCode`) e contiene solo conti di quel broker:
da lì vengono la tabella dei simboli e l'archivio del datafeed, e mescolare due broker in un piano
significherebbe eseguire lo stesso segnale su due serie di prezzi diverse. Vedi
[`account-e-conversione-symbol.md`](account-e-conversione-symbol.md).

Il piano contiene poi l'elenco dei **conti** che lo eseguono (`Accounts`), il tetto di posizioni
contemporanee **per conto** (`MaxConcurrentTrades`) con la sua modalità di conteggio, sizing e
commissioni. I file sono salvati in `<workspace>/plans/plans.json`.

**I gruppi non esistono più** (03/09/2026 per Titano, 05/09/2026 per la distribuzione): il piano è
una lista di conti, e ogni conto riceve *ogni* segnale della barra — una volta sola — con la size
del proprio capitale. Il gruppo era un livello in mezzo che nei piani reali conteneva un conto solo,
e il suo unico effetto operativo («un segnale è consumato una volta per gruppo») è ora «una volta
per conto». Un `plans.json` con le vecchie righe resta leggibile: `TradingPlanService` ne ricava i
conti e prende tetto e modalità di conteggio dalla **prima riga**, poi non riscrive più il campo.
Dove le righe dichiaravano tetti diversi la differenza si perde, ed è voluto: prendere il massimo
allargherebbe in silenzio un limite che una prop impone.

Il piano **non porta un capitale**. Le sessioni che apre sono sempre `ExternalBroker`, dove il
saldo è del broker e la size di ogni conto viene dal suo `InitialBalance` (vedi
[`account-e-conversione-symbol.md`](account-e-conversione-symbol.md)); il capitale iniziale è un
parametro del singolo run di backtest. I `plans.json` scritti prima contengono ancora la proprietà
`InitialCapital`: viene ignorata alla lettura, non serve migrarli.

I piani legacy a singola riga (solo `GroupId`/`AccountNumber`) restano leggibili: al
caricamento vengono normalizzati in una `Groups` da un elemento. In scrittura i campi
singoli restano popolati come mirror della riga primaria (prima con run Titano, altrimenti
la prima) per compatibilità.

Il cBot apre una sessione con `POST /api/v1/trading-sessions/open-plan`, indicando codice piano,
contesto `Backtest`/`Realtime`, account ed `ExecutionKey`. L'account deve appartenere alle
righe del piano; se omesso si usa il primo. La tripla piano, contesto ed execution key è
idempotente: richieste ripetute riprendono la stessa sessione; una execution key diversa
crea una sessione nuova. All'apertura i conti del piano diventano i conti della sessione
(`PUT /trading-sessions/{id}/accounts`).

## Quali strategie gira il piano

Il masterfilter del workspace dice **quali strategie esistono**; il piano ne spegne un
sottoinsieme. `TradingPlan.DisabledStrategies` elenca gli `Id` di catalogo spenti, e una sessione
aperta dal piano valuta `masterfilter - DisabledStrategies`. Si configura nel tab *Strategie* del
dettaglio piano, dove la spunta è in positivo (*Attiva*).

**Si elencano le spente, non le accese.** Il masterfilter cambia nel tempo: se il piano portasse
l'elenco delle attive, una strategia aggiunta al masterfilter resterebbe fuori da ogni piano già
scritto senza che nulla lo dicesse. Elencando le spente il default è «attiva», cioè il piano com'era
prima che il campo esistesse, e il file dichiara esattamente ciò che qualcuno ha scelto di togliere.

Un Id spento che il masterfilter non contiene (più) **non viene scartato** al salvataggio: se quella
strategia rientra nel masterfilter deve ritrovarsi spenta, non riaccesa di nascosto. Il tab lo mostra
in coda con la nota «fuori dal masterfilter».

Spegnerle tutte non apre una sessione muta: `TradingSessionService.CreateCore` rifiuta l'apertura,
perché una sessione senza strategie è indistinguibile da una che non produce segnali. La console
anticipa lo stesso rifiuto al salvataggio.

Due punti in cui lo spegnimento **non** arriva, di proposito:

- **Il datafeed.** `ResolveDatafeedInstruments` continua a raccogliere gli strumenti di tutto il
  masterfilter: spegnere una strategia è reversibile, e riaccenderla non deve trovare nel feed un
  buco lungo quanto lo spegnimento. È la stessa ragione per cui il raccoglitore segue il
  masterfilter e non le strategie attive.
- **Il masterfilter.** Spegnere una strategia in un piano non la toglie dal workspace: altri piani
  dello stesso workspace continuano a farla girare.

Il filtro vale in **ogni** `TradingRunProfile`, backtest sorgente compreso. Non è un vincolo
operativo che mutila il campione, è quali strategie il piano fa girare: se il sorgente ne contenesse
di più, il confronto con `BacktestStaticFilter` misurerebbe la differenza fra due insiemi di
strategie invece dell'effetto del tetto di concorrenza, che è l'unica cosa che quel confronto esiste
per misurare.

## Overnight e overweek

Il piano porta `Holding` (`AccountHoldingPolicy`): se il conto può tenere oltre la sessione, se può
attraversare il fine settimana, e a che ora taglia quando non può. È la **parola finale** sulla
tenuta di una posizione — motore e strategia decidono solo dentro ciò che il piano concede — e
scende nella sessione, nel descriptor e nei cBot, che su questo non hanno più parametri propri. Il
dettaglio del piano ha un tab dedicato con l'elenco delle strategie del masterfilter che quel piano
taglierebbe. Regole complete in [`overnight-e-overweek.md`](overnight-e-overweek.md).

## Distribuzione o esecuzione diretta

`DistributeToAccounts` (default `true`) decide come la sessione consegna i segnali.

Con la distribuzione attiva i conti del piano diventano i conti della sessione: `POST /bars`
restituisce template non assegnati e ogni conto li reclama da `GET /accounts/{n}/signals`, dove
vivono lo slot del conto e il limite di trade concorrenti. È il percorso di
`PiootooDistributedExecutionBot`, e la sessione è condivisa fra i conti del piano.

Con `DistributeToAccounts=false` il server non configura alcun gruppo: `POST /bars` restituisce
intent già assegnati, che il client esegue direttamente. Serve ai cBot che non implementano il
claim, come `PiootooDirectExecutionBot`. Il piano continua a fornire workspace, commissioni,
sizing, metadata strumenti e Titano; cambia soltanto il canale di consegna. La chiave
idempotente include in questo caso anche l'account e un marcatore di modalità, perché la sessione
non è condivisibile: due cBot sulla stessa sessione eseguirebbero gli stessi intent due volte.

`MaxConcurrentTrades` è applicato esclusivamente da `GetNextSignalForAccount`, cioè dal percorso di
claim. In esecuzione diretta non esiste un punto in cui applicarlo, quindi aprire un piano che lo
dichiara (con `EnforceConcurrencyLimits` attivo) restituisce `400`: meglio rifiutare che operare
senza il limite che il piano promette.

Il limite è **per account e trasversale ai simboli**: dieci significa dieci ingressi in volo, che
stiano su un simbolo solo o su dieci diversi. Un solo numero vale per tutti i conti del piano — un
conto che deve operare con un tetto diverso è un altro piano. Cosa venga contato lo dice
`ConcurrencyCountMode` del piano — `PositionsAndPendingOrders` (default) o `PositionsOnly`, dove gli ordini
pendenti non consumano budget e il tetto viene fatto valere dal cBot al primo fill. Le due modalità,
e perché la scelta dipende dal tipo di motore, in
[`distribuzione-multi-account.md`](distribuzione-multi-account.md) §2 e §4.6.

La sessione acquisisce uno snapshot del piano alla creazione. Modificare il piano non cambia
sessioni già aperte. Il server sceglie automaticamente la modalità Titano dalla riga primaria (in
esecuzione diretta, dalla riga dell'account che apre la sessione):

- piano senza filtro: `Disabled`;
- piano filtrato in backtest: `BacktestRotationFile`;
- piano filtrato live: `Realtime`.

I profili Titano delle altre righe restano applicati al claim degli intent per gruppo. Il cBot
non interpreta Titano e riceve soltanto intent già filtrati. Gli strumenti e i timeframe
sono derivati dal masterfilter del workspace e restituiti nel descriptor della sessione.

API CRUD: `GET/PUT/DELETE /api/v1/workspaces/{workspaceId}/trading-plans[/{code}]`. Freschezza
rotazione: `GET .../trading-plans/{code}/rotation-status` (vedi `TitanoRotationStatus`).

La ripresa idempotente sopravvive all'interruzione del cBot finché il processo server resta
attivo. La ricostruzione completa dello stato runtime dopo il riavvio del server resta un limite
delle sessioni, che sono ancora residenti in memoria.

In realtime il cBot salva inoltre
`%AppData%/PiootooLiveTradingBot/state-{planCode}-{accountNumber}.json` (la cartella conserva il
nome storico del bot: rinominarla orfanerebbe lo stato già scritto sulle macchine che operano). Per ogni posizione
registra `PositionId`, intent di ingresso, strategia, simbolo, `CloseAtUtc`,
`MaxBarsInPosition` e numero di barre già trascorse. La scrittura è atomica. Al riavvio il file
viene accettato soltanto se appartiene alla sessione risolta da `open-plan`; i record vengono poi
incrociati con le posizioni Piootoo ancora presenti su cTrader. I record non più presenti sul
broker sono eliminati.

Il cBot corrente invia esclusivamente ordini market sincroni, quindi non possiede condizioni di
uscita associate a ordini pending da persistere: il contesto viene salvato non appena il market
order produce una posizione. Se verranno introdotti ordini stop/limit asincroni, il medesimo file
dovrà contenere anche il contesto pending fino all'evento di apertura della posizione.

## Editing dalla shell

Un piano si modifica **solo da qui**. La console legacy (*File → Console legacy*, tab *Trading
Session*) aveva un proprio editor che mandava soltanto codice, nome e righe gruppo/account: siccome
il salvataggio riscrive il piano intero, ogni salvataggio da lì riportava ai default commissione,
moltiplicatore di size, limiti di concorrenza e `Holding` — cioè cambiava in silenzio la policy di
tenuta del conto. È stato rimosso (vedi [`../decisioni.md`](../decisioni.md) 2026-09-05); nel tab
legacy resta la griglia gruppi/account, che però appartiene alla **sessione** attiva, non al piano.

Il dettaglio di un piano (*Anagrafiche → Piani di trading*) espone tre riferimenti come
combo invece che come testo libero, perché sono tutti identificatori che il server risolve e
un refuso produce un errore soltanto all'apertura della sessione.

Il **workspace** è modificabile solo su un piano nuovo: sceglie dove il piano verrà scritto. Su
un piano esistente la combo è disabilitata, perché il piano vive in `<workspace>/plans/plans.json`
e spostarlo sarebbe una move, non una modifica di campo.

Le altre due sono indipendenti: la **cartella di backtest** viene dai backtest del workspace, il
**setup di rotazione** dalla lista globale dei setup — non dipende dal workspace, e non entra nel
percorso di filtro: resta come tracciamento della ricetta con cui il run è stato prodotto.

Il **run Titano non si sceglie più**: si usa sempre l'ultimo generato per la cartella scelta,
risolto al momento (`TitanoRotationService.ResolveLatestRun`), non congelato sul piano. Il
dettaglio del piano mostra invece, in sola lettura, lo **stato di freschezza** di quell'ultimo run
(`GET .../trading-plans/{code}/rotation-status`): pronto se copre ancora il periodo corrente,
da aggiornare se `now` ha già superato la fine dell'ultimo periodo calcolato — segno che serve un
nuovo backtest campione e una nuova rotazione. Una sessione già aperta recepisce un run più
recente dalla barra successiva, senza bisogno di riaprirla.

Un valore già persistito che non compare più nella lista corrente viene mostrato come voce
«non più presente» invece di essere scartato: il salvataggio riscrive il piano intero, quindi
perderlo in silenzio azzererebbe un riferimento che le sessioni già aperte usano ancora.

Nella griglia gruppi/account le colonne **Gruppo** e **Account cTrader** sono anch'esse combo,
alimentate dal registro globale (`GET /api/Accounts/groups` e `GET /api/Accounts`) e non dal
workspace. Sono in relazione: la lista account di una riga contiene solo gli account il cui
`GroupId` nel registro corrisponde al gruppo scelto sulla stessa riga, quindi la lista vive sulla
cella e non sulla colonna. Cambiare gruppo azzera un account che non gli appartiene. Senza gruppo
scelto non si filtra nulla, perché una lista vuota si leggerebbe come registro vuoto invece che
come scelta ancora da fare.

Il tab **Strumenti** non è l'elenco dei simboli del piano ma una lista di *override* del sizing:
all'apertura della sessione i simboli si derivano dalle strategie del masterfilter, e quelli assenti
dalla lista ricevono `DollarsPerPoint = 1`, `MinimumQuantity = 1`, `QuantityStep = 1` e
`FuturesContracts`. Poiché quei default sono quasi sempre sbagliati su un future e non producono
alcun errore, il tab ha un pulsante *Importa simboli dal masterfilter* che precarica una riga per
simbolo: sbagliare per default resta possibile, ma almeno diventa visibile. Non è il posto della
conversione simbolo verso il broker, che sta sull'account — vedi
[`account-e-conversione-symbol.md`](account-e-conversione-symbol.md).

Anche le colonne Titano della griglia sono combo: ogni riga può avere la propria **cartella di
backtest**, che eredita altrimenti quella del tab Generale. Il run resta implicito anche qui —
sempre l'ultimo della cartella della riga. Ricorda che `TradingPlanService` rifiuta un piano in cui
due righe con lo stesso `GroupId` dichiarano coppie setup/cartella diverse.

## Riferimenti codice

`Piootoo.Shared/Models/Trading/TradingPlanContracts.cs`,
`Piootoo.Core/Services/TradingPlanService.cs`,
`Piootoo.Core/Services/TradingSessionService.cs` (`ResolveRunIdForFolder`, risoluzione dinamica),
`Piootoo.Core/Services/TitanoRotationService.cs` (`ResolveLatestRun`, `GetFreshness`),
`PiootooApp.Server/Controllers/TradingPlansController.cs`,
`piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs` (distribuzione),
`piootoo-repository/ctrader/PiootooDirectExecutionBot.cs` (esecuzione diretta),
`piootooapp.clientform/Shell/Screens/PlanDetailScreen.cs`,
`piootooapp.clientform/Shell/Screens/PlanListScreen.cs` (badge di stato).
