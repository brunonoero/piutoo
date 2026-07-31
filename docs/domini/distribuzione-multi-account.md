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

## 2. Le cinque condizioni del claim

`GetNextSignalForAccount(account)` applica, nell'ordine:

### Passo 1 — idempotenza

```csharp
var assigned = session.Intents.FirstOrDefault(i =>
    i.AssignedAccountNumber == accountNumber && i.Status == Pending);
if (assigned != null) return assigned;
```

Se l'account ha già un intent **pendente**, il poll lo ripropone e si ferma lì.

Conseguenza che non è ovvia e che governa metà degli esempi: **un account non può
detenere più di un intent pendente alla volta**, qualunque sia
`MaxConcurrentTrades`. Il secondo segnale arriva solo dopo che il primo è stato
riempito, rifiutato o annullato.

### Passo 2 — max trade concorrenti (per account)

```csharp
if (IsConcurrentTradeLimitActive(session) && max > 0 &&
    openPositions + pendingOrders >= max)
    return "MaxConcurrentTradesExceeded";
```

`openPositions` è quello **dichiarato dal broker** nel poll, o in mancanza il conteggio
server delle posizioni aperte dell'account. `pendingOrders` viene solo dal broker.

Nota importante: si contano **posizioni riempite e ordini pendenti presso il broker**,
non i claim non ancora eseguiti. È il passo 1 a impedire l'accumulo di claim.

### Passi 3-7 — selezione del template

```csharp
session.EntryTemplates
  .Where(t => t.Status == Pending)
  .Where(t => non scaduto)
  .Where(t => groupId ∉ TemplateClaimedGroups[t.IntentId])        // 3
  .Where(t => (groupId, t.StrategyCode, t.Symbol) ∉ GroupStrategySlots)  // 4
  .Where(t => (accountNumber, t.Symbol) ∉ AccountActiveIntent)    // 5
  .Where(t => IsTemplateEligibleForGroup(groupId, t))             // 6 — Titano di gruppo
  .OrderByDescending(priorità)                                    // 7
  .ThenBy(CreatedAtUtc)
  .FirstOrDefault();
```

| # | Chiave | Granularità | Significato |
|---|---|---|---|
| 3 | `TemplateClaimedGroups[IntentId]` → set di gruppi | template × gruppo | **Un segnale è consumato una volta per gruppo.** Gruppi diversi ricevono lo stesso segnale: è il fan-out. |
| 4 | `GroupStrategySlots[grp\|strategia\|simbolo]` | gruppo × strategia × simbolo | Dentro un gruppo, una sola posizione per coppia strategia/simbolo. |
| 5 | `AccountActiveIntent[account\|simbolo]` | account × simbolo | Un account non apre due ingressi sullo stesso simbolo. Può operare in parallelo su simboli diversi. |
| 6 | run Titano del gruppo | gruppo | Ogni gruppo può avere un proprio run, o disattivare il filtro. |

Le chiavi 3, 4, 5 sono **tre lucchetti diversi**, non tre modi di dire la stessa
cosa. Confonderli è la fonte più comune di aspettative sbagliate.

### Quando i lucchetti si aprono

| Evento | 3 (`ClaimedGroups`) | 4 (`GroupStrategySlots`) | 5 (`AccountActiveIntent`) |
|---|---|---|---|
| Ingresso **rifiutato / non riempito** | resta | **liberato** | **liberato** |
| Ingresso **riempito** | resta | resta | resta |
| Posizione **chiusa** | resta | **liberato** | **liberato** |

`TemplateClaimedGroups` non si libera mai: un template consumato da un gruppo resta
consumato. È corretto — quel segnale di quella barra è stato servito.

Il punto da tenere a mente è la seconda riga: **al fill l'account non libera il
proprio lucchetto sul simbolo.** Lo tiene fino alla chiusura della posizione.

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

**Caso 2** — s2 è un template diverso, quindi il lucchetto 3 non si applica; il
lucchetto 4 non scatta perché la strategia è diversa; il 5 nemmeno perché acct2 non
ha nulla in corso. Vale **anche se s1 e s2 sono sullo stesso simbolo**.

**Caso 3** — s3 non è "scartato": resta `Pending` e reclamabile. Semplicemente non
c'è un terzo account in `g1`, e i due esistenti sono occupati. Se s1 viene rifiutato,
al poll successivo acct1 può prendere s3.

**Caso 4** — è il fan-out. `g2` non ha ancora consumato s1, quindi acct2 riceve lo
**stesso** segnale. Due gruppi = due portafogli paralleli sullo stesso flusso.
s2 e s3 restano fermi perché entrambi gli account hanno già un intent pendente.

### 3.2 Il caso che dipende dal simbolo

Il caso 5 dell'ipotesi iniziale — max 2, stesso gruppo, 5 segnali, con acct1 che
prende s1 e s3 e acct2 s2 e s4 — **è corretto solo se i segnali sono su simboli
diversi**.

| # | Max | Segnali | Sequenza | Esito |
|---|---|---|---|---|
| 5a | 2 | 5, **stesso simbolo** | a1, a2, fill a1, fill a2, a1, a2 | a1 → **s1** · a2 → **s2** · a1 → `NoSignal` · a2 → `NoSignal` |
| 5b | 2 | 5, **simboli diversi** | a1, a2, a1, fill×2, a1, a2, fill×2, a1, a2 | a1 → **s1**, *ripropone s1*, **s3** · a2 → **s2**, **s4** · poi entrambi `MaxConcurrentTradesExceeded` |

**5a** è il caso che sorprende. Con `MaxConcurrentTrades = 2` ci si aspetta due
posizioni per account, ma sullo stesso simbolo il lucchetto 5 non si apre al fill —
si apre alla chiusura. Il limite effettivo è **1 posizione per account per simbolo**,
e `MaxConcurrentTrades` non entra mai in gioco: la risposta è `NoSignal`, non
`MaxConcurrentTradesExceeded`.

> `MaxConcurrentTrades` conta **posizioni su simboli diversi**. Su un singolo
> simbolo il vincolo binding è sempre il lucchetto 5, qualunque valore si imposti.

**5b** mostra il ciclo completo. Il terzo poll di acct1 ripropone s1 invece di dare
s3: è il passo 1. Solo dopo il fill l'account può reclamare il secondo. Raggiunte 2
posizioni riempite, il passo 2 risponde `MaxConcurrentTradesExceeded` e s5 resta
non reclamato.

**L'alternanza s1/s3 e s2/s4 non è una regola.** È l'effetto dell'ordine dei poll in
quello scenario. Se acct1 pollasse due volte dopo il proprio fill mentre acct2 tace,
acct1 prenderebbe s1 e s2. L'unica garanzia è: ogni gruppo consuma ogni template al
massimo una volta.

### 3.3 Casi aggiuntivi

| # | Configurazione | Scenario | Esito | Perché |
|---|---|---|---|---|
| 6 | max 1, acct1·g1, acct2·g2 | 1 segnale | **entrambi** ricevono s1 | fan-out: lucchetto 3 è per gruppo |
| 7 | max 1, acct1·g1, acct2·g1, acct3·g1 | 3 segnali | a1→s1, a2→s2, a3→s3 | tre account liberi, tre template distinti |
| 8 | max 1, acct1·g1, acct2·g1 | 2 template **stessa strategia+simbolo** (barre successive) | a1→s1, a2→`NoSignal`, s2 fermo | lucchetto 4: `(g1, strat, sym)` già occupato |
| 9 | max 1, acct1·g1, acct2·g1 | 1 segnale, poi a1 **rifiutato** dal broker | a1→s1, *rifiuto*, a1→`NoSignal`, a2→`NoSignal` | s1 resta consumato da g1 (lucchetto 3 non si libera) |
| 10 | max 1, acct1·g1 | 2 segnali, a1 riempie s1 poi **chiude** la posizione | a1→s1, fill, a1→`MaxConcurrentTradesExceeded`, chiusura, a1→s2 | il passo 2 blocca finché la posizione è aperta |
| 11 | max 0 (illimitato), acct1·g1 | 3 segnali simboli diversi | a1→s1, fill, a1→s2, fill, a1→s3 | `max > 0` è la condizione: 0 disattiva il limite |
| 12 | max 1, acct1·g1, acct2·g2 con **run Titano diversi** | 1 segnale su strategia OFF in g2 | a1→s1, a2→`NoSignal` | lucchetto 6: `IsTemplateEligibleForGroup` |
| 13 | max 1, acct **non configurato** | qualunque | eccezione `Account non configurato` | serve una riga in `SetAccountGroups` |
| 14 | max 1, sessione non `Running` | qualunque | `SessionNotRunning` | precede ogni altro controllo |

Il caso 9 merita attenzione operativa: **un rifiuto del broker non restituisce il
segnale al gruppo.** I lucchetti 4 e 5 si liberano, quindi l'account può prendere un
*altro* template, ma quel template specifico è perso per quel gruppo. È una scelta
deliberata — riproporlo significherebbe inseguire un fill su una barra ormai vecchia
— ma va saputa quando si legge un `trades.json` con meno trade del previsto.

---

## 4. Disaccoppiamento da Titano

Richiesta esplicita: i due layer devono potersi attivare e disattivare in modo
indipendente, senza che l'uno produca bug nell'altro.

### 4.1 Cosa è già disaccoppiato

I tre lucchetti che fanno la distribuzione — `TemplateClaimedGroups`,
`GroupStrategySlots`, `AccountActiveIntent` — **non leggono nulla di Titano**. Sono
dizionari su chiavi `(gruppo, strategia, simbolo)` e `(account, simbolo)`, popolati
al claim e svuotati sui fill di chiusura e sui rifiuti. Una sessione con
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
indipendente. Il cBot lo espone come parametro a tre stati
(`Limite Trade Concorrenti`: `Default` / `On` / `Off`).

**Come misurare l'effetto di Titano in isolamento**: eseguire i due backtest — quello
`Disabled` e quello `BacktestRotationFile` — entrambi con `EnforceConcurrencyLimits`
forzato allo stesso valore. La differenza fra i due `trades.json` è allora attribuibile
alla sola rotazione.

### 4.4 Un secondo contatto, minore

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

Le lacune segnalate nella prima stesura sono state colmate in
`MultiAccountDistributionTests.cs`:

| Comportamento | Test |
|---|---|
| Fan-out fra gruppi diversi sullo stesso template (casi 4/6) | `DifferentGroups_BothClaimTheSameTemplate` |
| Stesso gruppo: il secondo account non riprende il template (caso 1) | `SameGroup_SecondAccountDoesNotGetTheSameTemplate` |
| Un account tiene un solo intent pendente (passo 1, casi 3/5b) | `PollIsIdempotent_AnAccountHoldsOnlyOnePendingIntent` |
| Il lucchetto account/simbolo sopravvive al fill (caso 5a) | `SymbolLockSurvivesTheFill_AndIsReleasedOnlyOnClose` |
| Template perso dopo un rifiuto del broker (caso 9) | `RejectedEntry_FreesTheAccount_ButTheTemplateStaysConsumedByTheGroup` |
| Default del limite di concorrenza | `ConcurrencyLimitDefault_IsOffOnlyForTheSourceBacktest` |
| Limite forzabile in entrambe le direzioni (§4.2) | `ConcurrencyLimitCanBeForcedOn_InABacktestWithoutTitano`, `ConcurrencyLimitCanBeForcedOff_InRealtime` |

---

## Riferimenti codice

- `Piootoo.Core/Services/TradingSessionService.cs`
  - `PushBars` — layer 1 e creazione template (440-560)
  - `GetNextSignalForAccount` — layer 2 (896-972)
  - `IsConcurrentTradeLimitActive` (974-976)
  - `ComputeStrategyPriority` (1036-1062)
  - `ResolveGroupTitano` (1064-1078), `IsTemplateEligibleForGroup` (1093-1120)
  - `GetGroupStrategyAllocation` (1122-1140), `CloneForClaim` (1219-1262)
  - `SlotKey` / `ActiveIntentKey` (1281-1286)
  - Rilascio lucchetti: rifiuto (615-625), chiusura (676-695)
- `Piootoo.Shared/Models/Trading/TradingSessionContracts.cs` — `AccountGroupMapping`,
  `AccountSignalResponse`
- `piootoo-repository/ctrader/PiootooTradingSessionBot.cs` — ciclo `OnBar` → push → poll
