# Account e conversione symbol

Un **account** è la configurazione di un conto operativo: anagrafica, balance iniziale, gruppo
anti copy-trading e un riferimento opzionale a una **tabella di conversione symbol** dal registro
globale, che traduce il mondo Piootoo nel mondo del broker.

La tabella di conversione **non vive sull'account**: è un'entità globale a sé, fuori sia dal
workspace sia dall'account, con un nome e un codice propri (`SymbolConversion`). Più account
possono referenziare la stessa tabella; l'account porta solo il codice
(`WorkspaceAccount.SymbolConversionCode`), non una copia dei dati. Separare i due significa che
correggere un moltiplicatore in un posto si applica a tutti gli account che lo usano, invece di
dover ripetere la modifica account per account.

Ogni riga della tabella risponde a due domande:

- con che nome questo symbol esiste sul broker (`@NQ` → `USDTEC`);
- quanto vale un contratto del broker rispetto a un contratto Piootoo
  (`ContractMultiplier`: 0,1 se il contratto del broker vale 100k contro 1M).

Il balance iniziale sta **sull'account**, non sulla tabella: è il capitale del conto, non
un'allocazione per strumento.

## Da dove esce il moltiplicatore

Il numero non si stima: si calcola dalle specifiche che il broker espone.

```
ContractMultiplier = PointValue(future) / PointValue(1 lotto CFD)
```

Entrambi i valori vanno presi nella **valuta di quotazione** dello strumento, non in quella del
conto: il valore di un punto per un lotto CFD è la dimensione del lotto in unità del sottostante
(`Symbol.LotSize` in cAlgo), perché il controvalore è *unità × prezzo*. Usare il valore in valuta
conto (`TickValue`) ci infila dentro un cambio, e il moltiplicatore andrebbe rivisto a ogni
movimento FX.

`PointValue(future)` è quello di `InstrumentRegistry`, l'unica fonte verificata dei contratti.

Il cBot `piootoo-repository/ctrader/PiootooSymbolMultiplierBot.cs` fa questo conto su un conto
cTrader collegato: legge le specifiche dei symbol elencati in parametro, calcola moltiplicatore,
quantità minima e passo, e scrive un `Mappings` pronto da incollare qui dentro. Segnala i casi in
cui il future e il CFD non sono quotati nella stessa valuta, che sono quelli in cui il rapporto va
verificato a mano. `PiootooSymbolInfoDumpBot.cs` fa invece il dump grezzo di *tutti* i symbol del
conto, senza calcoli.

`piootoo-repository/symbol-convertion/PiootooSymbolMultiplierBot-FTMO.cs` è la variante per un
conto **cTrader FTMO** (classe `PiootooSymbolMultiplierBotFtmo`, codice tabella
`cfd-ctrader-ftmo`). Cambia solo il modo di trovare i nomi, non il conto: ogni future porta un
elenco di alias separati da `|` — primo il nome verificato sul conto (`US100.cash`, `GER40.cash`,
`COCOA.c`, `NATGAS.cash`), poi le varianti degli altri broker — e vince il primo che esiste; se
nessuno esiste adotta l'unico symbol simile, marcandolo (`ResolvedBy: "somiglianza"`) invece di
sceglierlo in silenzio, e scrive comunque il listino completo del conto in `AccountSymbols`. I
symbol dismessi che FTMO tiene nel listino (`COCOA.c_removed`) sono esclusi dalla ricerca: erano
loro a rendere ambiguo ogni candidato. La mappa copre **solo i simboli con almeno una strategia in
catalogo**, che è anche il criterio della tabella. Un quarto campo opzionale nella mappa
(`@NQ=US100.cash:20:USD:23400`) è un prezzo di riferimento del future: se il CFD quota una potenza
di dieci più in là, il bot deduce `PriceScale` invece di lasciarlo a 1. L'output `.mappings.json`
esce già come voce intera (`Code`, `Name`, `RoundingMode`, `Mappings`, date), da incollare
nell'array `Conversions`.

⚠ La tabella FTMO cTrader è **un'altra voce** rispetto a `cfd-mt4-ftmo`: stesso broker, listino
diverso. Nomi, minimi e passi non coincidono, e un conto cTrader a lotti frazionari non ha lo
stesso `RoundingMode` di un conto MT4 a contratti interi.

## Dove vivono i dati

| File | Contenuto |
|---|---|
| `accounts/accounts.json` | Tutti gli account globali (`WorkspaceAccountsFile`), condivisi da tutti i workspace. |
| `accounts/symbol-conversions.json` | Registro globale delle tabelle di conversione nominate (`SymbolConversionsFile`). |

Un `SymbolConversionCode` vuoto o assente dal registro segue la stessa regola della tabella:
**nessun codice = nessuna conversione**, l'account opera 1 a 1. Un codice valorizzato ma assente
dal registro è invece un errore esplicito quando la conversione viene risolta per un run (vedi
sotto) — non un 1 a 1 silenzioso.

## Interfaccia

Le due entità hanno schermate separate nella Shell:

- **Anagrafiche → Account** (`AccountListScreen` / `AccountDetailScreen`) — anagrafica del conto
  e una sola combo per scegliere quale tabella di conversione referenziare. Non c'è editing della
  tabella qui.
- **Anagrafiche → Conversioni simbolo** (`SymbolConversionListScreen` /
  `SymbolConversionDetailScreen`) — CRUD del registro globale: nome, codice (immutabile dopo il
  primo salvataggio, è l'identificativo con cui gli account la referenziano) e la griglia delle
  righe symbol/symbol account/moltiplicatore/abilitato. Un pulsante "Riempi dal catalogo
  (identità)" precarica ogni symbol del catalogo strategie su se stesso, moltiplicatore 1.

La console legacy (`WorkspaceBacktestingForm`, tab Accounts) segue lo stesso schema: anagrafica
account con una combo di sola selezione, nessuna griglia di conversione inline.

Un codice già persistito su un account ma non più presente nel registro compare in combo come
«non più presente» invece di essere scartato al caricamento: salvare l'account riscrive il record
intero, quindi perderlo in silenzio azzererebbe un riferimento che un run potrebbe usare ancora.

## API

```
GET    api/SymbolConversions
GET    api/SymbolConversions/identity
GET    api/SymbolConversions/{code}
POST   api/SymbolConversions
PUT    api/SymbolConversions/{code}
DELETE api/SymbolConversions/{code}
GET    api/Accounts
POST   api/Accounts
POST   api/Accounts/default
GET    api/Accounts/{accountId}
PUT    api/Accounts/{accountId}
DELETE api/Accounts/{accountId}
```

`DELETE /api/SymbolConversions/{code}` rifiuta la cancellazione se un account la referenzia ancora
(stesso principio di `RemoveAccountGroup` per i gruppi): altrimenti l'account resterebbe con un
codice orfano.

L'`Id` dell'account è lo slug del nome. Il `Code` di una tabella di conversione è invece scelto
liberamente da chi la crea (come `TradingPlan.Code`): è l'identificativo stabile, non derivato.

L'account `Default` (creabile da *Crea account Default* o via `POST api/Accounts/default`) non ha
alcun `SymbolConversionCode`: nessun codice è già 1 a 1, quindi resta l'account neutro anche se il
registro delle conversioni cambia. Il preset identità del catalogo strategie è comunque
disponibile come tabella nominata riutilizzabile: vedi `default-futures` nella migrazione.

## Nessun effetto sul backtest interno

**Il backtest interno non conosce gli account.** Un run è *capitale iniziale + strategie del
masterfilter + datafeed*: nessuna conversione di simbolo, `ContractMultiplier` e `BalanceScale`
fissi a 1, quantità identica a quella dichiarata dalla strategia. `BacktestingRequest` non ha più
un `AccountId` e `BacktestingScreen` non ha più la combo di selezione.

Il motivo è che quel run è il **campione sorgente di Titano**: con la size legata al conto, due
backtest identici su account diversi produrrebbero rotazioni diverse, e la rotazione misurerebbe il
capitale invece delle strategie. È lo stesso principio per cui `EnforceConcurrencyLimits` è già
disattivo nel backtest sorgente. Vedi `docs/decisioni.md` (2026-08-05).

Conseguenza da conoscere: un symbol **disabilitato** su un account non filtra più nulla in
backtest. La disabilitazione è una proprietà operativa del conto e agisce dove si opera.

I campi `AccountId`, `AccountSymbol`, `ContractMultiplier` e `AccountBalanceScale` restano nel
`PersistedSignal` — il formato di `signals.json` è unico con quello prodotto dalle sessioni — e il
backtest li scrive all'identità.

## Effetto sulle sessioni

È qui che la conversione conta, perché il segnale deve diventare un ordine eseguibile su un conto
reale. La sessione risolve la tabella per account e la applica in `CloneForClaim`, cioè **quando
il destinatario è noto**: i template prodotti da `PushBars` sono ancora all'identità.

La size dell'account è il prodotto di due fattori indipendenti:

- **`BalanceScale`** = `InitialBalance / 1.000.000`, proprietà del **conto**. Le strategie
  dichiarano le quantità rispetto a un milione di riferimento, quindi un conto da 100.000 opera
  `0,1` volte la size dichiarata.
- **`ContractMultiplier`**, proprietà dello **strumento**: rapporta il lotto del broker (CFD) al
  contratto Piootoo (future), che hanno taglie diverse.

`GetSizeFactor` è il loro prodotto. Tenerli separati è ciò che permette di cambiare il capitale di
un conto senza ricalcolare a mano tutte le righe simbolo.

Ogni riga porta anche la granularità di volume del broker: `MinimumQuantity`, `QuantityStep`,
`RoundingMode` (contratto intero o passo esplicito, per i CFD frazionari). È una proprietà della
coppia broker/strumento, non del piano di trading — `TradingPlan` non ha più un elenco strumenti.
`AccountSymbolConversion.RoundQuantity` arrotonda **dopo** la conversione (quando la quantità è già
nei contratti del broker) e vale zero sotto la quantità minima; per un simbolo senza riga in
tabella applica comunque il default a contratto intero invece di lasciar passare una quantità
frazionaria. Per non arrotondare due volte, `PositionSizingService` e l'allocazione di gruppo non
arrotondano più sulle sessioni `ExternalBroker` (`QuantityRoundingMode.Deferred`): l'unico
arrotondamento è quello del conto, applicato una volta sola in `CloneForClaim` (percorso
multi-account) o direttamente in `AddIntent` (esecuzione diretta senza claim). Vedi
`docs/decisioni.md` (2026-08-05).

Il **symbol** tradotto finisce solo nell'intent e in `signals.json` (`AccountSymbol`, `AccountId`,
`ContractMultiplier`). Il symbol interno **non** viene rinominato: il motore indicizza prezzi, barre
e chiavi di posizione sul symbol Piootoo normalizzato, e rinominarlo a monte lo lascerebbe senza
prezzi. Il symbol del broker serve a chi inoltra l'ordine, non a chi lo valuta.

Un symbol mappato ma **disabilitato** non è operativo su quell'account: il template resta
disponibile per gli altri account invece di essere consumato. Un symbol **assente** dalla tabella
non è un errore: nessuna conversione, moltiplicatore 1.

Un account senza anagrafica, o un `SymbolConversionCode` valorizzato ma assente dal registro, fanno
fallire l'apertura della sessione. Proseguire 1 a 1 falserebbe le size in silenzio, ed è esattamente
la classe di errore che il progetto tratta come inaccettabile (vedi `docs/PROGETTO.md` §7).

## Riferimenti codice

- `Piootoo.Shared/Models/Workspaces/WorkspaceModels.cs` — `WorkspaceAccount`, `SymbolConversion`,
  `AccountSymbolMapping`
- `Piootoo.Core/Services/AccountSymbolConversion.cs` — tabella risolta per il loop
- `Piootoo.Core/Services/WorkspaceService.cs` — CRUD account, CRUD tabelle di conversione,
  `ResolveSymbolConversionMappings`, account di default
- `Piootoo.Core/Services/TradingSessionService.cs` — `ResolveAccountConversion` per sessione,
  `CloneForClaim` (applicazione dei due fattori al claim)
- `PiootooApp.Server/Controllers/AccountsController.cs`,
  `PiootooApp.Server/Controllers/SymbolConversionsController.cs` — endpoint
- `piootooapp.clientform/Shell/Screens/AccountDetailScreen.cs`,
  `piootooapp.clientform/Shell/Screens/SymbolConversionListScreen.cs`,
  `piootooapp.clientform/Shell/Screens/SymbolConversionDetailScreen.cs`
- `piootooapp.clientform/WorkspaceBacktestingForm.cs` — tab Accounts della console legacy
