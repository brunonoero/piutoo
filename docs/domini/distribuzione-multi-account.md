# Distribuzione multi-account: gruppi, slot e max trade concorrenti

Il secondo layer di filtro, quello che agisce **dopo** Titano. Titano decide *quali
strategie* possono operare e *con che size*; questo layer decide *quale account*
esegue *quale segnale*.

Vale solo per `ExecutionMode.ExternalBroker` con gruppi account configurati. Senza
gruppi la sessione resta nel comportamento storico a singolo destinatario.

Riferimenti: `TradingSessionService.PushBars` (template) e
`GetNextSignalForAccount` (claim). Titano: `titano-rotation.md`.

---

## 1. Il flusso per barra

Il cBot non riceve i segnali come risposta a `PushBars`. Sono due chiamate distinte,
e la separazione è il cuore del meccanismo.

```
cBot (account N) --OnBar--> POST /trading-sessions/{id}/bars
                            │
                            ├─ UpdateMarketPrices su tutti i simboli della barra
                            ├─ LAYER 1 — Titano: quali strategie valutare
                            │     evaluationStrategies = session.Strategies
                            │                          ∩ EffectiveStrategies(barTimeUtc)
                            ├─ StrategyEvaluationService.Evaluate → segnali
                            ├─ PositionSizing (allocazione Titano × ATR × portfolio)
                            └─ un EntryTemplate per segnale, NON assegnato

cBot (account N) --poll--> GET .../signals/next?accountNumber=N
                            │
                            └─ LAYER 2 — gruppi/slot/max: chi lo prende
```

I template restano `Pending` e senza destinatario finché un account non li reclama.
La distribuzione è quindi **pull**, non push: l'ordine di arrivo dei poll determina
chi prende cosa.

---

## 2. Le condizioni del claim

`GetNextSignalForAccount(account)` applica, nell'ordine:

### Passo 1 — le chiusure prima di tutto

```csharp
var pendingClose = pendingForAccount.FirstOrDefault(i => i.Kind == Close);
if (pendingClose != null) return pendingClose;
```

Un intent di **chiusura** assegnato all'account si ripropone sempre, in ogni
profilo, e non consuma budget: è un ordine da eseguire, non un segnale da
distribuire, e perderne uno lascia aperta una posizione che nessuno chiuderà più.

Gli **ingressi** non fermano più il poll. Fino all'11/08/2026 qualunque intent
pendente lo fermava, e la conseguenza era che *un account non poteva detenere più
di un intent pendente alla volta, qualunque fosse `MaxConcurrentTrades`*. Adesso
l'account drena finché ha budget; a budget esaurito l'ingresso pendente più vecchio
viene riproposto (vedi sotto).

### Passo 2 — budget di concorrenza (per account, trasversale ai simboli)

```csharp
var inFlight = CountInFlightForAccount(session, account, brokerState, countMode, …);
if (IsConcurrentTradeLimitActive(session) && max > 0 && inFlight >= max)
    return stalledEntry ?? "MaxConcurrentTradesExceeded";
```

**`MaxConcurrentTrades` conta sull'insieme delle strategie e non guarda il simbolo:
dieci significa dieci, che stiano su un simbolo solo o su dieci diversi.**

Cosa sia "in volo" lo decide il piano, con `ConcurrencyCountMode`:

| Modalità | Conta | Quando ha senso |
|---|---|---|
| `PositionsAndPendingOrders` (default) | posizioni riempite **+** ordini pendenti **+** claim non ancora piazzati | tetto rigido, nessuno sfondamento possibile |
| `PositionsOnly` | solo posizioni riempite | motori breakout: un ordine stop non è esposizione, è un'opzione |

Il conteggio è **deduplicato per IntentId**, e la ragione non è cosmetica: un ordine
già piazzato esiste contemporaneamente come intent `Pending` sul server e come
pending order nello snapshot del broker. La somma dei due conteggi grezzi — com'era
prima — contava due volte ogni ordine a mercato e dimezzava di fatto il tetto
configurato. L'esposizione che non porta un IntentId leggibile (label di formato
precedente, o fallback al conteggio server quando il claim arriva dalla vecchia GET
senza corpo) non è deduplicabile e viene sommata: meglio contare una volta di troppo
che consegnare un ingresso oltre il tetto.

I **claim consegnati e non ancora comparsi sul broker** entrano nel conto in
modalità `PositionsAndPendingOrders`. Senza di loro un drenaggio veloce reclamerebbe
tutti i template della barra prima che il broker registri il primo ordine, e il
tetto verrebbe sfondato dal ritardo di propagazione invece che da una decisione.

A tetto pieno, se l'account ha un ingresso ancora `Pending`, glielo si **ripropone**
invece di rispondere `MaxConcurrentTradesExceeded`: è l'unico modo di recuperare un
claim la cui risposta si è persa in rete, e il client lo riconosce come già inviato
(`_submittedIntentIds`) e smette di drenare. Senza budget residuo non ci sarebbe
comunque niente di nuovo da consegnargli.

### Passo 3 — guardia di identità, attiva in ogni profilo

```csharp
.Where(t => !AccountHasEntryInFlight(account, t.StrategyCode, t.Symbol))
```

Un account non riceve un template di una coppia (strategia, simbolo) su cui ha già
un ingresso `Pending` o una posizione aperta. **Non è un vincolo di concorrenza, è
l'identità della strategia**: quel segnale è già in mano al broker, e un secondo
ordine sarebbe rischio doppio sullo stesso motivo di ingresso. Vale anche a
lucchetti spenti. Il perché in §4.3.

### Passi 4-7 — selezione del template

```csharp
session.EntryTemplates
  .Where(t => t.Status == Pending)
  .Where(t => non scaduto)
  .Where(t => groupId ∉ TemplateClaimedGroups[t.IntentId])              // 3
  .Where(t => (groupId, t.StrategyCode, t.Symbol) ∉ GroupStrategySlots) // 4
  .Where(t => IsTemplateEligibleForGroup(groupId, t))                   // 6 — Titano di gruppo
  .OrderByDescending(priorità)                                          // 7
  .ThenBy(CreatedAtUtc)
  .FirstOrDefault();
```

| # | Chiave | Granularità | Significato |
|---|---|---|---|
| 3 | `TemplateClaimedGroups[IntentId]` → set di gruppi | template × gruppo | **Un segnale è consumato una volta per gruppo.** Gruppi diversi ricevono lo stesso segnale: è il fan-out. |
| 4 | `GroupStrategySlots[grp\|strategia\|simbolo]` | gruppo × strategia × simbolo | Dentro un gruppo, una sola posizione per coppia strategia/simbolo. |
| 6 | run Titano del gruppo | gruppo | Ogni gruppo può avere un proprio run, o disattivare il filtro. |

Le chiavi 3 e 4 sono **due lucchetti diversi**, non due modi di dire la stessa cosa.

> **Il lucchetto 5 non esiste più.** C'era una chiave `AccountActiveIntent[account|simbolo]`
> — «un account non apre due ingressi sullo stesso simbolo» — che non si liberava al
> fill ma solo alla chiusura. Su una sessione a simbolo singolo rendeva
> `MaxConcurrentTrades` inapplicabile: il tetto effettivo era 1 qualunque valore si
> impostasse, e la seconda strategia sullo stesso simbolo non arrivava mai a mercato.
> Rimosso l'11/08/2026, vedi `docs/decisioni.md`. Quanto un account operi in parallelo
> lo dice ora `MaxConcurrentTrades` e basta; che non ci siano doppioni della stessa
> strategia lo garantisce il passo 3.

### Quando i lucchetti si aprono

| Evento | 3 (`ClaimedGroups`) | 4 (`GroupStrategySlots`) |
|---|---|---|
| Ingresso **rifiutato / non riempito** | resta | **liberato** |
| Ingresso **riempito** | resta | resta |
| Posizione **chiusa** | resta | **liberato** |

`TemplateClaimedGroups` non si libera mai: un template consumato da un gruppo resta
consumato. È corretto — quel segnale di quella barra è stato servito.

Il budget dell'account non compare in questa tabella perché **non è un lucchetto da
liberare**: si ricalcola a ogni poll dallo stato del broker e dagli intent ancora
`Pending`. Una posizione chiusa esce dallo snapshot, un ordine cancellato pure, e il
budget torna disponibile senza che nessuno debba ricordarsi di rilasciarlo.

---

## 3. Gli esempi

Tutti verificati rieseguendo l'algoritmo di `GetNextSignalForAccount` fuori dal
progetto. `→` indica l'esito del poll.

### 3.1 I casi base

| # | Max | Gruppi | Segnali | Sequenza poll | Esito | Non reclamati |
|---|---|---|---|---|---|---|
| 1 | 1 | acct1·g1, acct2·g1 | 1 | a1, a2 | a1 → **s1** · a2 → `NoSignal` | — |
| 2 | 1 | acct1·g1, acct2·g1 | 2 | a1, a2 | a1 → **s1** · a2 → **s2** | — |
| 3 | 1 | acct1·g1, acct2·g1 | 3 | a1, a2, a1 | a1 → **s1** · a2 → **s2** · a1 → *ripropone s1* | s3 |
| 4 | 1 | acct1·g1, acct2·**g2** | 3 | a1, a2 | a1 → **s1** · a2 → **s1** | s2, s3 |

**Caso 1** — il lucchetto 3 blocca acct2: `g1` ha già consumato s1.

**Caso 2** — s2 è un template diverso, quindi il lucchetto 3 non si applica, e il 4
non scatta perché la strategia è diversa. Vale **anche se s1 e s2 sono sullo stesso
simbolo**.

**Caso 3** — s3 non è "scartato": resta `Pending` e reclamabile. Semplicemente i due
account di `g1` hanno esaurito il proprio budget (`max: 1`). Se s1 viene rifiutato,
al poll successivo acct1 può prendere s3.

**Caso 4** — è il fan-out. `g2` non ha ancora consumato s1, quindi acct2 riceve lo
**stesso** segnale. Due gruppi = due portafogli paralleli sullo stesso flusso.
s2 e s3 restano fermi perché entrambi gli account hanno già un intent pendente.

### 3.2 Il caso che non dipende più dal simbolo

Il caso 5 — max 2, stesso gruppo, 5 segnali, con acct1 che prende s1 e s3 e acct2 s2
e s4 — vale ora **indipendentemente dai simboli**.

| # | Max | Segnali | Sequenza | Esito |
|---|---|---|---|---|
| 5a | 2 | 5, **stesso simbolo**, strategie diverse | a1, a1, a2, a2 | a1 → **s1**, **s3** · a2 → **s2**, **s4** · poi `MaxConcurrentTradesExceeded` |
| 5b | 2 | 5, **simboli diversi** | a1, a1, a2, a2 | identico a 5a |

**5a era il caso che sorprendeva**, e non sorprende più. Con
`MaxConcurrentTrades = 2` si ottengono due ingressi per account anche su un simbolo
solo, purché siano di strategie diverse: il vincolo che li teneva a uno — il
lucchetto (account, simbolo) — non c'è più.

> `MaxConcurrentTrades` conta gli ingressi in volo **sull'insieme delle strategie**,
> senza guardare il simbolo. Il solo vincolo che resta legato al simbolo è la coppia
> (strategia, simbolo), che non è concorrenza ma unicità del segnale.

Nota sul **drenaggio**: acct1 può prendere s1 e s3 in due poll consecutivi *senza*
attendere il fill del primo, perché il passo 1 non fa più da tappo e il passo 2
conta anche i claim non ancora piazzati (in `PositionsAndPendingOrders`). In
`PositionsOnly` il tappo sono le posizioni riempite, quindi acct1 continua a
reclamare finché ci sono coppie (strategia, simbolo) libere, e a limitarlo è il
numero di strategie della sessione.

**L'alternanza s1/s3 e s2/s4 non è una regola.** È l'effetto dell'ordine dei poll in
quello scenario. Se acct1 pollasse quattro volte mentre acct2 tace, acct1
prenderebbe s1 e s2 e poi si fermerebbe al proprio tetto. L'unica garanzia è: ogni
gruppo consuma ogni template al massimo una volta.

### 3.3 Casi aggiuntivi

| # | Configurazione | Scenario | Esito | Perché |
|---|---|---|---|---|
| 6 | max 1, acct1·g1, acct2·g2 | 1 segnale | **entrambi** ricevono s1 | fan-out: lucchetto 3 è per gruppo |
| 7 | max 1, acct1·g1, acct2·g1, acct3·g1 | 3 segnali | a1→s1, a2→s2, a3→s3 | tre account liberi, tre template distinti |
| 8 | max 1, acct1·g1, acct2·g1 | 2 template **stessa strategia+simbolo** (barre successive) | a1→s1, a2→`NoSignal`, s2 fermo | lucchetto 4: `(g1, strat, sym)` già occupato |
| 9 | max 1, acct1·g1, acct2·g1 | 1 segnale, poi a1 **rifiutato** dal broker | a1→s1, *rifiuto*, a1→`NoSignal`, a2→`NoSignal` | s1 resta consumato da g1 (lucchetto 3 non si libera) |
| 15 | max 3, acct1·g1, `PositionsOnly` | 3 segnali di 3 strategie sullo **stesso simbolo** | a1×3 | a1→s1, s2, s3: tre stop a mercato insieme; al primo fill il cBot cancella gli altri due |
| 10 | max 1, acct1·g1 | 2 segnali, a1 riempie s1 poi **chiude** la posizione | a1→s1, fill, a1→`MaxConcurrentTradesExceeded`, chiusura, a1→s2 | il passo 2 blocca finché la posizione è aperta |
| 11 | max 0 (illimitato), acct1·g1 | 3 segnali simboli diversi | a1→s1, fill, a1→s2, fill, a1→s3 | `max > 0` è la condizione: 0 disattiva il limite |
| 12 | max 1, acct1·g1, acct2·g2 con **run Titano diversi** | 1 segnale su strategia OFF in g2 | a1→s1, a2→`NoSignal` | lucchetto 6: `IsTemplateEligibleForGroup` |
| 13 | max 1, acct **non configurato** | qualunque | eccezione `Account non configurato` | serve una riga in `SetAccountGroups` |
| 14 | max 1, sessione non `Running` | qualunque | `SessionNotRunning` | precede ogni altro controllo |

Il caso 9 merita attenzione operativa: **un rifiuto del broker non restituisce il
segnale al gruppo.** Lo slot di gruppo si libera e il budget dell'account torna
disponibile, quindi l'account può prendere un
*altro* template, ma quel template specifico è perso per quel gruppo. È una scelta
deliberata — riproporlo significherebbe inseguire un fill su una barra ormai vecchia
— ma va saputa quando si legge un `trades.json` con meno trade del previsto.

---

## 4. Disaccoppiamento da Titano

Richiesta esplicita: i due layer devono potersi attivare e disattivare in modo
indipendente, senza che l'uno produca bug nell'altro.

### 4.1 Cosa è già disaccoppiato

I due lucchetti che fanno la distribuzione — `TemplateClaimedGroups` e
`GroupStrategySlots` — **non leggono nulla di Titano**: sono dizionari su chiavi
`(template, gruppo)` e `(gruppo, strategia, simbolo)`, popolati al claim e svuotati
sui fill di chiusura e sui rifiuti. Nemmeno il budget per account lo legge: si
ricalcola a ogni poll dallo stato del broker. Una sessione con
`TitanoMode = Disabled` e nessun `TitanoRunId` li usa esattamente come una sessione
filtrata.

Simmetricamente, Titano non conosce gli account: `Resolve` restituisce strategie e
moltiplicatori, mai destinatari. Il layer 1 lavora per barra, il layer 2 per poll.

Il punto di contatto è uno solo ed è pulito: `IsTemplateEligibleForGroup` (lucchetto
6), che consulta il run Titano **del gruppo** e restituisce `true` quando il gruppo
non ha filtro, quando il run non è risolvibile o quando non c'è un periodo attivo.
Il fallimento è verso "passa", quindi Titano non può bloccare la distribuzione per
un problema proprio.

### 4.2 L'accoppiamento che c'era, e come è stato tolto

Fino al 31/07/2026:

```csharp
private static bool IsConcurrentTradeLimitActive(Session session)
    => !(session.ClientRunMode == ClientRunMode.Backtest &&
         session.TitanoMode == TitanoFilterMode.Disabled);
```

**Il limite di trade concorrenti leggeva `TitanoMode`.** La ragione era corretta
(decisione B4 del 29/07): il run in backtest senza filtro serve a produrre il
`trades.json` sorgente su cui Titano calcola le rotazioni, e applicare un limite di
concorrenza eliminerebbe segnali falsando la sorgente.

Ma **la ragione era espressa attraverso la variabile sbagliata**. Ciò che si vuole
dire è "questo run genera il campione sorgente, non simularlo con vincoli
operativi"; ciò che si scriveva è "Titano è spento". Due conseguenze:

1. Non era possibile eseguire un backtest **senza** Titano ma **con** il limite di
   concorrenza, per misurare quanto il limite costa in isolamento.
2. Confrontare un run `Disabled` con uno `BacktestRotationFile` significava muovere
   **due** variabili e attribuire alla rotazione una differenza che in parte veniva
   dal limite di trade. Ed è esattamente il confronto che si fa per valutare Titano.

**Correzione applicata.** Un flag esplicito, propagato da piano e sessione:

```csharp
// CreateTradingSessionRequest / TradingPlan
public bool? EnforceConcurrencyLimits { get; init; }   // null = default storico

// TradingSessionService
private static bool IsConcurrentTradeLimitActive(Session session)
    => session.EnforceConcurrencyLimits;

public static bool DefaultEnforceConcurrencyLimits(ClientRunMode runMode, TitanoFilterMode titanoMode)
    => !(runMode == ClientRunMode.Backtest && titanoMode == TitanoFilterMode.Disabled);
```

`null` conserva il comportamento storico, quindi nulla cambia per le configurazioni
esistenti; valorizzarlo permette di variare concorrenza e rotazione in modo
indipendente.

### 4.3 Il flag copriva solo il passo 2 — corretto il 2026-08-06

`EnforceConcurrencyLimits` governava **soltanto** `MaxConcurrentTradesExceeded`. I
il lucchetto 4, l'allora lucchetto 5 e il passo 1 restavano incondizionati, quindi un
backtest sorgente apriva comunque **una posizione per account per simbolo** e ne consegnava **una per
poll**. Su un piano a simbolo singolo questo riduce il campione a un trade alla
volta — che è esattamente ciò che il run sorgente non deve fare — e rende il
`trades.json` del cBot incomparabile con quello del backtest interno, che di
lucchetti non ne ha nessuno.

La distinzione giusta è fra vincoli **operativi** e struttura della distribuzione:

| Lucchetto | Cos'è | Segue il flag? |
|---|---|---|
| passo 2 (`MaxConcurrentTrades`) | operativo | sì (già prima) |
| passo 3 (`AccountHasEntryInFlight`) | *identità del segnale, non concorrenza* | **no, mai** |
| 3 (`TemplateClaimedGroups`) | *un template è già stato servito a quel gruppo* | **no, mai** |
| 4 (`GroupStrategySlots`) | operativo | **sì** |

*(Il passo 1 non è più un lucchetto: le chiusure si ripropongono sempre e gli
ingressi non fermano il poll. Il lucchetto 5 è stato rimosso — §2 e §4.5.)*

Il lucchetto 3 non si spegne in nessun profilo: non limita quanto si opera in
parallelo, registra che quel segnale è stato consegnato. Spento, il cBot che drena
la coda riceverebbe lo stesso template all'infinito.

Al passo 1 le **chiusure si ripropongono sempre**, anche a flag spento: sono ordini
da eseguire, non segnali da distribuire, e perderne una lascia aperta una posizione
che nessuno chiuderà più.

C'è però un filtro nuovo, **attivo in ogni profilo**: un account non riceve un
template di una coppia (strategia, simbolo) su cui ha già un ingresso `Pending` o una
posizione aperta (`AccountHasEntryInFlight`). Non è un vincolo di concorrenza, è
l'identità della strategia — quel segnale è già in mano al broker. Serve perché
`MaxEntriesPerSession` si applica al **fill** e non al claim: due template di barre
diverse reclamati prima che il primo riempia passano entrambi il controllo. Su un run
reale (`PTS_NQ_PCH_002_15`, 14/10/2024 13:15) questo ha prodotto due stop order
riempiti allo stesso prezzo e due posizioni da 20 lotti sullo stesso segnale. Con i
lucchetti attivi il 4 lo copriva già, ma è più largo — vale per tutto il gruppo — e
a lucchetti spenti non restava niente a fermare il doppione.

### 4.4 Il profilo del run

Il flag resta, ma non è più il modo di sceglierlo: `ApplyTitanoFilters` nel piano
più `EnforceConcurrencyLimits` nella sessione descrivono la stessa decisione in due
posti, e per cambiare backtest bisognava editare il piano. Il cBot dichiara invece
`TradingRunProfile` in `OpenTradingPlanSessionRequest`:

| Profilo | `TitanoMode` | Lucchetti operativi | Note |
|---|---|---|---|
| `DalPiano` | dal piano | default | comportamento storico, default |
| `BacktestSorgente` | `Disabled` | **off** | tutte le strategie del masterfilter, un intent per segnale |
| `BacktestTitano` | `BacktestRotationFile` | attivi | errore esplicito senza `TitanoBacktestFolder` |

Il profilo **prevale sul piano** — è il cBot a sapere che run sta aprendo — ed entra
nella chiave di esecuzione: rilanciare lo stesso bot con un profilo diverso apre una
sessione nuova invece di riprendere quella vecchia con i vincoli di prima. I profili
`Backtest*` sono rifiutati in `Realtime`.

Conseguenza sul client: con i lucchetti spenti il claim non ha più il tappo di un
intent per account, quindi il cBot **deve drenare la coda** (`PollNextSignal` cicla
finché il server risponde `Intent = null`). Fermarsi al primo intent significherebbe
eseguire una strategia per barra.

### 4.5 Il tetto era per simbolo, non per account — corretto l'11/08/2026

Il sintomo, da un run reale (`FTMO-TRIAL-01`, 10/08/2026): un account con
`MaxConcurrentTrades = 10`, tre strategie, due di esse — `PTS_NQ_PCH_001_15` e
`PTS_NQ_PCH_002_15` — sullo stesso `US100.cash`. Per undici ore il log mostra un
solo ordine per barra, sempre della 001, mentre gli IntentId saltano di due: il
template della 002 nasceva a ogni barra e non arrivava mai a mercato.

Erano due vincoli distinti, e nessuno dei due era `MaxConcurrentTrades`:

1. **Passo 1**, idempotenza: l'account non poteva detenere più di un intent
   pendente. Uno stop order vive per tutta la barra, quindi il conto era saturo per
   tutta la barra.
2. **Lucchetto 5**, `(account, simbolo)`: un solo ingresso per simbolo, liberato
   alla chiusura e non al fill.

Su una sessione a simbolo singolo il tetto effettivo era quindi **1**, e il valore
configurato non entrava mai in gioco. La risposta al poll era `NoSignal`, non
`MaxConcurrentTradesExceeded`, quindi nemmeno la diagnostica lo diceva.

**Cosa è cambiato**

- Il lucchetto 5 non esiste più. `MaxConcurrentTrades` conta sull'insieme delle
  strategie, trasversale ai simboli.
- Il passo 1 non fa più da tappo agli ingressi: l'account drena finché ha budget.
- Il budget si conta **deduplicato per IntentId** e include i claim non ancora
  piazzati. Prima `openPositions + pendingOrders` contava due volte ogni ordine a
  mercato, perché lo stesso ordine è insieme un intent `Pending` sul server e un
  pending order sul broker.
- `ConcurrencyCountMode` diventa un parametro del piano: cosa conti il tetto lo
  decide chi configura, non una convenzione del server.

**Cosa NON è cambiato**: la guardia `AccountHasEntryInFlight` (stessa strategia,
stesso simbolo) resta attiva in ogni profilo. È quella nata dall'incidente
`PTS_NQ_PCH_002_15` del 14/10/2024 — due stop riempiti allo stesso prezzo, due
posizioni da 20 lotti sullo stesso segnale — e non è un vincolo di concorrenza.

### 4.6 `PositionsOnly` e la metà client del limite

In `PositionsOnly` il server distribuisce tutti gli intent della barra senza contare
gli ordini a mercato. È deliberato: su un motore breakout non si sa quale livello
verrà toccato, e bloccarne uno per «occupazione di slot» significa perdere il solo
che sarebbe partito.

Il tetto viene allora fatto valere **a valle**, nel momento in cui si scopre quale
ordine è entrato davvero: `PiootooDistributedExecutionBot.CancelPendingOrdersAtCap`,
chiamato da `OnPositionOpened`, spegne gli ordini rimasti quando le posizioni
riempite raggiungono il tetto. È il comportamento di un OCO — il primo che entra
spegne gli altri.

**Questo non accoppia il cBot al server.** Il bot non riceve mai un comando: legge
dal descriptor un parametro di configurazione all'apertura, decide da solo guardando
la propria piattaforma, e comunica al server solo il fatto compiuto — un `Cancelled`
sullo stesso canale degli ordini scaduti, che libera lo slot di gruppo senza che il
server debba sapere perché. Resta vero l'invariante di sempre: **il server decide
*cosa*, il broker decide *se e a che prezzo***.

Il rischio residuo va conosciuto: fra il fill e la cancellazione c'è una finestra in
cui due stop possono riempirsi insieme, e in quella finestra l'esposizione supera il
tetto. È il prezzo del modello, ed è il motivo per cui la modalità è un parametro e
non il default. Su conti con regole di esposizione istantanea — FTMO e simili — resta
preferibile `PositionsAndPendingOrders`.

Come ultima barriera il bot ricontrolla il proprio conteggio di posizioni anche
*prima* di mandare un ordine: fra il claim e l'invio un altro stop può essersi
riempito, e il server non poteva saperlo.

### 4.7 `ClaimableIntents`: quando il poll si può saltare

`PushBarWindowResponse.ClaimableIntents` conta ciò che la sessione potrebbe consegnare
a un claim — template `Pending` non scaduti, più gli intent già assegnati e ancora
pendenti. **Zero significa che `GetNextSignalForAccount` non può restituire nulla per
nessun account**, quindi il cBot salta il poll immediato dopo il push.

Il costo che rimuove è reale: in backtest ogni barra e ogni stream valgono due chiamate
HTTP sincrone, e dai log la grande maggioranza delle barre non produce alcun segnale.

Due scelte da non invertire:

- **Conta il server, non il client.** Solo il server sa dei template di barre precedenti
  ancora vivi e degli intent assegnati in un giro anteriore. Dedurlo lato client da
  `Intents` — i soli segnali di *quella* barra — salterebbe poll che avevano qualcosa.
- **Il campo è nullable sul DTO del cBot.** Un server che non lo conosce lo omette, e su
  un `int` varrebbe 0, cioè "non pollare mai": il bot smetterebbe di reclamare per tutto
  il run, in silenzio. `null` vale "non so" e polla.

Il conteggio è deliberatamente **più largo** del claim: non applica lucchetti, Titano né
la conversione dell'account. Sbagliare per eccesso costa un poll a vuoto, per difetto
costa un segnale.

Regressioni in `RunProfileTests.cs`.

**Come misurare l'effetto di Titano in isolamento**: eseguire i due backtest — quello
`Disabled` e quello `BacktestRotationFile` — entrambi con `EnforceConcurrencyLimits`
forzato allo stesso valore. La differenza fra i due `trades.json` è allora attribuibile
alla sola rotazione.

### 4.8 Un secondo contatto, minore

`ComputeStrategyPriority(session, groupId)` ordina i template per
`AllocationMultiplier` Titano, con fallback sul PnL cumulato per strategia quando la
rotazione non è risolvibile o assente.

Non è un accoppiamento di ammissione — nessun template viene escluso — ma di
**ordinamento**: con Titano attivo un account contende prima le strategie meglio
allocate, senza Titano prima quelle storicamente più profittevoli. In scenari come
il caso 3, dove i template disponibili superano gli account liberi, l'ordine
determina *quale* segnale resta indietro.

Va documentato, non necessariamente cambiato: usare l'allocazione come priorità è
sensato. Ma spiega perché due run con Titano diverso possono distribuire in modo
diverso anche a parità di segnali.

---

## 5. Copertura a test

| Comportamento | Test |
|---|---|
| Account a capacità, il fratello riceve il template | `AccountAtCapacity_DoesNotClaimTemplate_SiblingAccountCanReceiveIt` |
| Gruppo senza filtro riceve ciò che il gruppo filtrato scarta | `GroupTitanoProfile_OpenGroupReceivesTemplateWhenFilteredGroupDoesNot` |
| Allocazione di gruppo applicata alla quantità del claim | `GroupTitanoProfile_ScalesClaimedQuantityUsingAllocationMultiplier` |
| Allocazione applicata una volta sola (sessione + gruppo) | `OpenPlan_WithTitano_AppliesAllocationOnce` |
| Limite non applicato in backtest senza Titano | `OpenPlan_BacktestWithoutTitano_EvaluatesAllWorkspaceStrategies` |
| Limite applicato in realtime senza Titano | `OpenPlan_RealtimeWithoutTitano_EnforcesMaxConcurrentTrades` |
| Persistenza mappatura account/gruppo e profilo Titano | `SetTradingGroups_PersistsTitanoProfileAndAccountMapping`, `SetAccountGroups_PreservesExistingGroupTitanoProfiles` |
| Il profilo prevale sul piano (§4.4) | `BacktestSorgente_SpegneTitanoEILucchettiDiConcorrenza`, `BacktestTitano_TieneLeRotazioniEIVincoliOperativi` |
| Run sbagliato rifiutato all'apertura | `UnProfiloDiBacktest_InRealtimeVieneRifiutato`, `BacktestTitano_SenzaRotazioniVieneRifiutato` |
| Il profilo entra nella chiave di esecuzione | `ProfiliDiversi_NonSiRiprendonoAVicenda` |
| Drenaggio completo a lucchetti spenti (§4.3) | `BacktestSorgente_UnAccountReclamaTuttiISegnaliDellaBarra`, `ConILucchettiAttivi_LAccountNeOttieneUnoSolo` |
| Il lucchetto 3 non si spegne mai | `BacktestSorgente_IlLucchettoDelGruppoRestaAttivo` |
| Niente due ingressi della stessa strategia in volo (§4.3) | `BacktestSorgente_NonConsegnaDueIngressiDellaStessaStrategia` |

Le lacune segnalate nella prima stesura sono state colmate in
`MultiAccountDistributionTests.cs`:

| Comportamento | Test |
|---|---|
| Fan-out fra gruppi diversi sullo stesso template (casi 4/6) | `DifferentGroups_BothClaimTheSameTemplate` |
| Stesso gruppo: il secondo account non riprende il template (caso 1) | `SameGroup_SecondAccountDoesNotGetTheSameTemplate` |
| L'account drena piu' intent finche' ha budget (passo 1) | `AccountDrainsUpToItsBudget_NotOneIntentAtATime` |
| A tetto pieno il claim ripropone l'intent pendente | `AtTheCap_ThePendingEntryIsRedelivered` |
| Il budget non e' piu' per simbolo, e sopravvive al fill | `BudgetIsPerAccountNotPerSymbol_AndSurvivesTheFill` |
| Conteggio deduplicato per IntentId | `InFlightCount_DeduplicatesTheSameIntentSeenTwice` |
| `PositionsOnly` non conta gli ordini pendenti | `PositionsOnly_PendingOrdersDoNotConsumeBudget` |
| `PositionsOnly` conta comunque le posizioni riempite | `PositionsOnly_AFilledPositionStillConsumesBudget` |
| Un claim non ancora piazzato conta nel budget | `AClaimNotYetPlacedStillCountsAgainstTheLimit` (`ConcurrencyLimitsMatrixTests`) |
| Template perso dopo un rifiuto del broker (caso 9) | `RejectedEntry_FreesTheAccount_ButTheTemplateStaysConsumedByTheGroup` |
| Default del limite di concorrenza | `ConcurrencyLimitDefault_IsOffOnlyForTheSourceBacktest` |
| Limite forzabile in entrambe le direzioni (§4.2) | `ConcurrencyLimitCanBeForcedOn_InABacktestWithoutTitano`, `ConcurrencyLimitCanBeForcedOff_InRealtime` |

La matrice del §3 — limite per account contro lucchetti di gruppo — è verificata in
`ConcurrencyLimitsMatrixTests.cs`, che si appoggia al fatto che i due meccanismi
rispondono in modo diverso: `MaxConcurrentTradesExceeded` viene dal passo 2 e non
consuma il template, `NoSignal` dai passi 3-5. Lo stesso file contiene i test di
concorrenza reale (poll paralleli di più account, poll simultanei dello stesso
account, push e poll sovrapposti).

---

## 6. Provarlo su dati reali

*Console WinForms → Operatività → Verifica concorrenza.*

La schermata apre una sessione usa e getta da un piano reale (chiave di esecuzione
con l'istante di avvio, quindi non riprende mai una sessione di un cBot), la
alimenta con le barre del datafeed del repository e polla al posto dei cBot,
registrando ogni decisione: log per poll con i numeri su cui il server ha deciso,
matrice per account, template con i gruppi che li hanno consumati, conteggio per
causa di scarto. I gruppi arrivano dal piano ma sono modificabili, così si prova
la stessa serie di barre con limiti diversi.

Il broker è simulato dalla console: riempie tutto e chiude dopo N barre passando
per `POST /intents/close-external` — cioè per lo stesso percorso che in produzione
libera lo slot di gruppo, senza scorciatoie. La console non simula però la
cancellazione OCO del cBot (§4.6): in `PositionsOnly` i suoi numeri sono quelli del
solo layer server.

Due cose da sapere prima di leggere i risultati:

- **La modalità client cambia il default del limite.** In `Backtest` con Titano
  spento il limite è disattivo (§4), e la schermata lo dice in rosso prima di
  partire: senza quell'avviso si finisce a chiedersi perché `MaxConcurrentTrades`
  «non funziona».
- **L'ordine dei poll è quello delle righe.** La distribuzione è pull: chi polla
  prima serve per primo, quindi riordinare le righe cambia chi prende cosa. È il
  motivo per cui l'alternanza del caso 5b non è una regola.

---

## Riferimenti codice

- `Piootoo.Core/Services/TradingSessionService.cs`
  - `PushBars` — layer 1 e creazione template (440-560)
  - `GetNextSignalForAccount` — layer 2 (896-972)
  - `IsConcurrentTradeLimitActive` (974-976)
  - `ComputeStrategyPriority` (1036-1062)
  - `ResolveGroupTitano` (1064-1078), `IsTemplateEligibleForGroup` (1093-1120)
  - `GetGroupStrategyAllocation` (1122-1140), `CloneForClaim` (1219-1262)
  - `CountInFlightForAccount` — budget per account, deduplicato per IntentId
  - `AccountHasEntryInFlight` — guardia (strategia, simbolo), attiva in ogni profilo
  - `SlotKey`
  - Rilascio dello slot di gruppo: rifiuto, chiusura
- `Piootoo.Shared/Models/Trading/TradingSessionContracts.cs` — `AccountGroupMapping`,
  `AccountSignalResponse`
- `piootoo-repository/ctrader/PiootooDirectExecutionBot.cs` — ciclo `OnBar` → push → poll
- `piootoo-repository/ctrader/PiootooDistributedExecutionBot.cs` — `CancelPendingOrdersAtCap`
  (metà client del limite, §4.6), `CountPiootooPositions`
- `Piootoo.Strategies.Tests/ConcurrencyLimitsMatrixTests.cs` — limite vs lucchetti, stress concorrente
- `piootooapp.clientform/Shell/Screens/ConcurrencyHarnessScreen.cs` — banco di prova su dati reali
