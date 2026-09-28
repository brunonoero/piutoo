# Broker workspace: i piani in produzione

*Progetto, 28/09/2026. Niente di questo esiste ancora nel codice: i riferimenti di riga sono lo stato
di oggi e servono a chi implementa.*

## Cosa e' e perche'

I workspace di ricerca si moltiplicano, e i piani che girano sui conti stanno sparsi fra
`ftmo-pt5dav-41`, i workspace PT3B e altri. Il **broker workspace** e' il posto unico in cui stanno i
piani promossi alla produzione, uno per broker. I piani al suo interno sono gia' quelli da eseguire,
uno per conto, sullo stesso broker.

Non e' un workspace con un prefisso nel nome: e' un'entita' propria, come i best plan
(`best-plans.md`), fuori dai workspace in `[BasePath]\broker-workspaces\{BROKER}\`. Il motivo e' il
masterfilter. In un workspace le strategie di un piano sono `masterfilter − DisabledStrategies`
(`TradingPlanContracts.cs:130-150`), e in un workspace che raccoglie piani diversi ogni strategia
aggiunta per un piano nuovo si accenderebbe in silenzio in tutti i piani gia' in produzione.

Il percorso diventa lineare: ricerca nel workspace → backtest con piano → best plan → broker workspace.

## Le regole

1. **Uno per broker.** La chiave e' `BrokerCode`: un secondo broker workspace per lo stesso broker
   non si puo' creare. Nasce alla prima promozione su quel broker, non c'e' un "Nuovo".
2. **Si entra solo da un best plan**, e solo da un best plan con `PlanCode` (un run neutro sul
   masterfilter non e' un piano). L'unica eccezione e' il duplica con cambio di conto (vedi sotto).
3. **Il piano dichiara le strategie attive** (`EnabledStrategies`, per Id di catalogo). Nel broker
   workspace non c'e' masterfilter e `DisabledStrategies` e' vuoto.
4. **Il broker del piano e' quello del broker workspace**, e i suoi conti sono di quel broker (la
   seconda meta' la controlla gia' `ValidateBrokerAndAccounts`).
5. **Una strategia sta in un solo piano attivo** del broker workspace. E' la regola "una strategia su
   un conto solo per prop" che oggi vive nella tabella di `registro-piani.md`.
6. **Un conto sta in un solo piano attivo** del broker workspace: il piano e' gia' quello del conto.
7. **Nessun contenitore di ricerca** (`IsResearchContainer`).
8. **Immutabile.** Un piano promosso non si modifica: una modifica e' un piano nuovo, promosso da un
   best plan nuovo. Non si cancella: si **ritira** (`RetiredUtc`). Un piano ritirato resta leggibile,
   le sue sessioni restano, non apre sessioni nuove e libera strategie e conti per le regole 5 e 6.
9. **Codice unico a livello globale**, workspace e broker workspace insieme. E' quello che lascia i
   cBot come sono: il bot conosce solo `PlanCode` e il server lo cerca ovunque
   (`TradingPlanService.Resolve`, :239).
10. **Provenienza dichiarata**: best plan, workspace, piano e cartella di backtest di origine, istante
    della promozione.

Le violazioni sono un `409` che nomina il piano e la strategia (o il conto) in conflitto.

## Quando il broker cambia il numero di conto

FTMO cambia il numero di conto di tanto in tanto: passaggio di fase, reset, conto nuovo. Il modello
dei conti resta com'e', con i piani che elencano i numeri. Il cambio si fa cosi':

1. si crea il conto nuovo in anagrafica; il vecchio si **disattiva**, non si cancella, perche'
   sessioni e artefatti vecchi citano il suo numero;
2. si **duplica** il piano con il conto nuovo e un codice nuovo (per esempio `FTMO-EUROPA-2`);
3. a conto flat si mette l'istanza cTrader sul conto nuovo con il codice nuovo. Il cBot va comunque
   aggiunto sul conto nuovo, quindi cambiare `PlanCode` non costa niente di piu'.

Il numero nuovo e' un conto nuovo sul broker, senza le posizioni del vecchio: la sessione nuova e'
quella giusta, e la vecchia non va continuata.

Nel broker workspace il duplica e' **l'unica eccezione** alla regola 2, e fa una cosa sola:
- crea il piano nuovo identico al vecchio salvo i conti;
- **ritira** il vecchio nello stesso passaggio, cosi' le regole 5 e 6 restano vere;
- porta nella provenienza il piano precedente e lo stesso best plan.

Il legame fra i due piani resta quindi scritto. Endpoint:
`POST {broker}/plans/{code}/duplicate` con codice nuovo e conti nuovi.

## Da dove vengono le strategie alla promozione

Il best plan porta due fonti, e non dicono la stessa cosa:

- il **summary del run**: `backtest-summary.json` per un run interno, `session-summary.json` per un
  run del cBot. Entrambi elencano in `strategies` le strategie che il run ha eseguito, per codice di
  esecuzione. Solo il primo porta anche pesi e holding: il secondo no.
- `artifacts/plan.json` e' il piano **al momento della promozione a best plan**, non al momento del
  run (`BestPlanService.CopyArtifacts`).

Tutti i best plan di oggi vengono da run del cBot, quindi la regola deve reggere con il solo
`session-summary.json`:

- le **strategie attive** sono le `strategies` del summary, riportate a Id di catalogo. Un codice che
  il catalogo non conosce piu' o un contenitore di ricerca fermano la promozione;
- **tutto il resto** (pesi, holding, commissione, tetto di concorrenza, `SizeMultiplier`, sizing) viene
  da `plan.json`. I pesi si tengono solo per le strategie attive;
- `plan.json` vale **solo se il piano non e' stato salvato dopo l'inizio del run**
  (`UpdatedUtc` ≤ `CreatedUtc` di `origin.json`). Altrimenti non si sa quale configurazione il run
  abbia eseguito e la promozione si **rifiuta**: si rifa' il backtest e si ripromuove. Sui sette best
  plan del 28/09/2026 il controllo passa per sei e ferma `ftmo-combo-europa-o4`, il cui piano e'
  stato salvato tre minuti dopo l'avvio del run;
- una strategia eseguita dal run ma spenta nel `plan.json` e' una contraddizione e ferma la
  promozione.

**I conti si scelgono alla promozione.** `PromoteToProductionRequest.Accounts` sostituisce i conti del
piano di origine; vuota vuol dire tenerli. Serve perche' i piani di ricerca girano tutti sul conto di
prova comune (oggi 17202911), e per la regola 6 in produzione ognuno vuole il suo. I conti non cambiano
cio' che il run ha misurato: le size scalano con il capitale del conto. Un conto disattivato in
anagrafica non entra in un piano nuovo.

## Il risolutore unico

Un servizio solo risponde a "dammi il piano con questo codice":

```
ResolvedPlan PlanResolver.Resolve(string planCode)                     // globale: cBot, ripresa, datafeed
ResolvedPlan PlanResolver.Resolve(string workspaceId, string planCode) // backtest con piano
IReadOnlyList<string> PlanResolver.SessionDirectories()                // dove la ripresa cerca
IReadOnlyList<string> PlanResolver.PlanCodesForAccount(string number)  // presidio del conto

record ResolvedPlan(
    TradingPlan Plan,
    PlanHome Home,                              // dove vivono sessioni e backtest del piano
    IReadOnlyList<string> UniverseStrategyIds)  // da cui si parte; e' anche cio' che si raccoglie
{
    IReadOnlyList<string> ActiveStrategyIds;    // universo − spente: cosa gira
}

record PlanHome(PlanHomeKind Kind, string Id, string RootPath)   // Workspace | Broker
```

| | piano di workspace | piano di broker workspace |
|---|---|---|
| `UniverseStrategyIds` | masterfilter intero (come prima) | `EnabledStrategies` |
| `ActiveStrategyIds` | masterfilter − spente (come prima) | `EnabledStrategies` (nessuna spenta) |
| `Home.RootPath` | `workspaces\{ws}` | `broker-workspaces\{BROKER}` |

Per i piani di workspace il comportamento non cambia di una virgola. In particolare l'impronta di
ripresa (`ConfigurationFingerprint`, `TradingSessionService.cs:4470`) dipende dall'insieme risolto:
se cambiasse, le sessioni vive verrebbero rifiutate al primo riavvio.

I punti che oggi risolvono da soli e passano al risolutore:

| # | punto | oggi |
|---|---|---|
| 1 | `OpenFromPlan` + `CreateCore` | `Resolve` (:912) e `GetMasterFilter(request.WorkspaceId)` (:1253) |
| 2 | ripresa delle sessioni | scansione dei soli `workspaces\*\sessions` (:4504), `Resolve` (:4667) |
| 3 | cartelle della sessione | `ResolveSessionDirectory` (:1133), `PromoteToBacktest` (:2646) |
| 4 | strumenti del datafeed | `ResolveDatafeedInstruments` (`TradingPlanService.cs:55`) |
| 5 | backtest con piano | masterfilter in `BacktestingController` (:52), `ResolvePlan` (:200), cartella di uscita (:1879) |
| 6 | presidio del conto | `ResolvePlanCodesForAccount` (:734) |
| 7 | unicita' del codice | `Save` (:289), `Duplicate` (:382) |

Le sessioni manuali (`Create`, senza piano) restano sul masterfilter del workspace.

Un test di conformita', sul modello di `UtcOnlyConformanceTests`, impone che `GetMasterFilter` sia
chiamato solo dal risolutore, dal controller dei masterfilter e dal percorso delle sessioni manuali.
Chi legge `plan.WorkspaceId` per trovare le strategie di un piano di produzione non trova niente, ed
e' il test a dirlo, non il conto.

## Contratti

- **`BrokerWorkspace`** (`broker-workspace.json`): `BrokerCode`, `CreatedUtc`. I piani stanno in
  `plans\plans.json` accanto, con lo stesso formato dei workspace; sessioni e backtest in `sessions\`
  e `backtests\`.
- **`TradingPlan`**, campi nuovi:
  - `EnabledStrategies`: null nei piani di workspace;
  - `Provenance` (`PlanProvenance`: `BestPlanId`, `SourceWorkspaceId`, `SourcePlanCode`,
    `BacktestFolder`, `PromotedUtc`);
  - `RetiredUtc`.

  `WorkspaceId` e' vuoto per i piani di produzione: la casa del piano la dice `PlanHome`, e si
  assegna alla lettura dalla cartella, non dal JSON.
- **`PromoteToProductionRequest`**: `BestPlanId`, `PlanCode` (nuovo), `Name`, `Accounts`, `DryRun`.
  Con `DryRun` il server risponde con il piano che creerebbe, senza scrivere; un conflitto e' un
  `409` anche li'. La console lo mostra prima di confermare.
- **`DuplicateProductionPlanRequest`**: `NewCode`, `NewName`, `Accounts` (obbligatori), `DryRun`.
- **API** `api/v1/broker-workspaces`:

  | metodo e percorso | cosa fa |
  |---|---|
  | `GET` | elenco |
  | `GET {broker}` | dettaglio con i piani |
  | `GET {broker}/plans/{code}` | un piano |
  | `POST {broker}/plans` | promozione da best plan; il broker del piano deve essere `{broker}` |
  | `POST {broker}/plans/{code}/retire` | ritiro |
  | `POST {broker}/plans/{code}/duplicate` | duplica con cambio di conto; ritira l'originale |

  Nessun `PUT` e nessun `DELETE`.

## Console

- **Menu *Operativita'* → *Produzione*.** `BrokerWorkspaceListScreen` mostra una riga per broker:
  piani attivi, piani ritirati, strategie, conti. Il dettaglio e' la griglia dei piani, ordinabile.
  Il dettaglio del piano riusa `PlanDetailScreen` in sola lettura, secondo la regola "una schermata
  nuova che duplica assorbe l'esistente", con in piu' la provenienza e il pulsante *Ritira*. Come i
  best plan, non dipende dal workspace corrente.
- **`BestPlanDetailScreen`**: pulsante *Promuovi in produzione…*. Chiede codice e nome, chiama il
  dry-run, mostra broker, strategie, pesi, conti ed eventuali conflitti, poi conferma.
- **`TradingSessionsScreen`**: il workspace del piano (:358) diventa la casa del piano.

## Migrazione dei piani sui conti

Per ogni piano gia' su un conto (oggi `FTMO-PT3B-EUROPA`, `FTMO-PT3B-USA`, `PT5DAV-O4-X05`):

1. promuovere il suo best plan nel broker workspace FTMO con il codice nuovo;
2. **a mercato chiuso**: fermare l'istanza cTrader, cambiare `PlanCode`, riavviare.

Il codice nuovo apre una sessione nuova, e la vecchia resta nel workspace di origine. Il piano di
origine resta bloccato dove sta. Si aggiorna `registro-piani.md`, la cui tabella delle
incompatibilita' diventa una regola del server.

## Fasi

1. **Contratti e deposito.** *Fatta il 28/09/2026.* `BrokerWorkspaceStore` (lettura e scrittura) e
   `BrokerWorkspaceService` (le regole): promozione con la derivazione dal run, ritiro, duplica con
   cambio di conto, API `api/v1/broker-workspaces`. L'unicita' del codice vale anche dal lato dei
   workspace (`TradingPlanService.Save` e `Duplicate`). Test in `BrokerWorkspaceServiceTests`.
   Nessun effetto sul runtime: un piano di produzione non apre ancora sessioni.
2. **Risolutore unico, solo per i piani di workspace.** *Fatta il 28/09/2026.* `PlanResolver` in
   `Piootoo.Core/Services/Plans/`. Ci passano apertura da piano e ripresa (`CreateCore` riceve il
   piano risolto e non legge piu' il masterfilter per un piano), cartelle della sessione (la
   `Session` porta `HomePath`, usata anche da `PromoteToBacktest`), strumenti del datafeed, backtest
   con piano (controller e servizio, cartella di uscita compresa) e presidio del conto. L'unicita'
   del codice era gia' nella fase 1. Comportamento invariato: la suite passa senza toccare i test
   esistenti, ripresa compresa. `PlanResolverTests` fissa cosa il risolutore restituisce per un piano
   di workspace, `PlanResolutionConformanceTests` ammette `GetMasterFilter` solo nel risolutore,
   nella sessione manuale, nel backtest neutro, in `WorkspaceService` e nel suo controller.
3. **Piani di produzione nel runtime:** `open-plan`, ripresa, strumenti del datafeed, backtest con
   piano che scrive sotto il broker workspace. Test HTTP sul modello di `TradingSessionsHttpTests`.
4. **Console.**
5. **Documentazione:**
   - questo file diventa *Stabile*;
   - `trading-plans.md` e `best-plans.md`;
   - un invariante in `CLAUDE.md`;
   - `README.md`, `decisioni.md`, `registro-piani.md`.
6. **Deploy e migrazione** dei piani sui conti, insieme, a mercato chiuso.

Ogni fase e' un commit che si puo' rilasciare da solo: dopo la 2 il sistema e' identico a oggi,
dopo la 3 i piani di produzione funzionano da API.

## Fuori da questo lavoro

- Un best plan da un backtest fatto dentro il broker workspace: `BestPlanService` cerca il backtest
  nei workspace (:145). Servira' per rimisurare un piano in produzione. Si fa dopo, se serve.
- Due prop sullo stesso broker cTrader: la regola 5 vale per broker workspace, non per prop.

## Convenzioni decise

- **Codice dei piani di produzione**: `{BROKER}-{nome}` senza la serie, per esempio `FTMO-EUROPA`,
  diverso dal codice di origine. A ogni cambio di conto si aggiunge `-2`, `-3`.
- **Da demo a conto vero**: e' lo stesso caso del cambio di numero, con un duplica verso il conto vero.

## Riferimenti codice

- `Piootoo.Shared/Models/BrokerWorkspaces/BrokerWorkspaceContracts.cs`: `BrokerWorkspace`, richieste di
  promozione e duplica.
- `Piootoo.Core/Services/BrokerWorkspaces/`: `BrokerWorkspaceStore` (deposito), `BrokerWorkspaceService`
  (regole). `PiootooApp.Server/Controllers/BrokerWorkspacesController.cs`.
- `Piootoo.Core/Services/Plans/PlanResolver.cs`: `PlanResolver`, `ResolvedPlan`, `PlanHome`.
- `Piootoo.Shared/Configuration/PiootooSettings.cs`: `BrokerWorkspacesPath`, `GetBrokerWorkspacesPath`.
- `Piootoo.Shared/Models/Trading/TradingPlanContracts.cs`: `TradingPlan` (`EnabledStrategies`,
  `Provenance`, `RetiredUtc`), `PlanProvenance`, `DisabledStrategies`, `StrategyWeights`.
- `Piootoo.Shared/Models/BestPlans/BestPlanContracts.cs`, `Piootoo.Core/Services/BestPlans/BestPlanService.cs`.
- `Piootoo.Core/Services/TradingPlanService.cs`: `Resolve`, `Get`, `ResolveDatafeedInstruments`, `Save`, `Duplicate`.
- `Piootoo.Core/Services/TradingSessionService.cs`: `OpenFromPlan`, `CreateCore`,
  `ResolveSessionDirectory`, `RestoreSessions`, `RestoreSession`, `BuildSessionState`.
- `Piootoo.Core/Services/PiootooBacktestingService.cs`: `ResolvePlan`, `ApplyPlanUniverse`.
- `PiootooApp.Server/Controllers/BacktestingController.cs`, `TradingSessionsController.cs`,
  `DatafeedExternalController.cs`, `BestPlansController.cs`.
- `Piootoo.Shared/Models/Trading/BacktestDiagnosticsContracts.cs`: i campi del summary da cui nasce il piano.
- `piootooapp.clientform/Shell/NavigationRegistry.cs`, `Screens/BestPlanDetailScreen.cs`,
  `Screens/PlanDetailScreen.cs`, `Screens/TradingSessionsScreen.cs`.
