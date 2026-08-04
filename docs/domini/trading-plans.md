# Piani di trading

Un piano è una configurazione operativa riutilizzabile salvata nel workspace. Il codice è
univoco tra tutti i workspace perché costituisce l'unico identificatore configurato nel cBot.
Nome e codice sono distinti.

Il piano contiene una o più righe gruppo/account (`Groups`): ciascuna con massimo trade
concorrenti, setup/run Titano e flag di applicazione. Contiene inoltre sizing e metadata
strumenti condivisi. I file sono salvati in `<workspace>/plans/plans.json`.

I piani legacy a singola riga (solo `GroupId`/`AccountNumber`) restano leggibili: al
caricamento vengono normalizzati in una `Groups` da un elemento. In scrittura i campi
singoli restano popolati come mirror della riga primaria (prima con run Titano, altrimenti
la prima) per compatibilità.

Il cBot apre una sessione con `POST /api/v1/trading-sessions/open-plan`, indicando codice piano,
contesto `Backtest`/`Realtime`, account ed `ExecutionKey`. L'account deve appartenere alle
righe del piano; se omesso si usa il primo. La tripla piano, contesto ed execution key è
idempotente: richieste ripetute riprendono la stessa sessione; una execution key diversa
crea una sessione nuova. All'apertura tutte le righe del piano sono applicate come gruppi
della sessione (anti copy-trading e profili Titano per gruppo).

## Distribuzione o esecuzione diretta

`DistributeToAccounts` (default `true`) decide come la sessione consegna i segnali.

Con la distribuzione attiva le righe del piano diventano i gruppi della sessione: `POST /bars`
restituisce template non assegnati e ogni account li reclama da `GET /accounts/{n}/signals`, dove
vivono slot di gruppo, limite di trade concorrenti ed eleggibilità Titano. È il percorso di
`PiootooDistributedExecutionBot`, e la sessione è condivisa fra gli account del piano.

Con `DistributeToAccounts=false` il server non configura alcun gruppo: `POST /bars` restituisce
intent già assegnati, che il client esegue direttamente. Serve ai cBot che non implementano il
claim, come `PiootooDirectExecutionBot`. Il piano continua a fornire workspace, capitale,
commissioni, sizing, metadata strumenti e Titano; cambia soltanto il canale di consegna. La chiave
idempotente include in questo caso anche l'account e un marcatore di modalità, perché la sessione
non è condivisibile: due cBot sulla stessa sessione eseguirebbero gli stessi intent due volte.

`MaxConcurrentTrades` è applicato esclusivamente da `GetNextSignalForAccount`, cioè dal percorso di
claim. In esecuzione diretta non esiste un punto in cui applicarlo, quindi aprire un piano che lo
dichiara (con `EnforceConcurrencyLimits` attivo) restituisce `400`: meglio rifiutare che operare
senza il limite che il piano promette.

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
