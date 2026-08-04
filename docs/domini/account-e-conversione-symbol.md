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

## Effetto sul backtest

`BacktestingRequest.AccountId` è opzionale. Quando è valorizzato, il run risolve **una volta sola**
l'account e — se ha un `SymbolConversionCode` — la tabella referenziata, in un
`AccountSymbolConversion` (dizionario per symbol normalizzato: niente lookup costosi nel loop
caldo). La applica in due punti diversi, per due ragioni diverse:

- **Size** — il moltiplicatore scala `signal.Quantity` *prima* che il motore veda il segnale, così
  trade, equity e drawdown riflettono i contratti realmente inviabili su quel conto. La quantità
  resta `decimal` fino a trade e P&L: conversioni come `1 × 0,01 = 0,01` non vengono arrotondate
  artificialmente a un contratto.
- **Symbol** — la traduzione finisce solo in `signals.json` (`AccountSymbol`, `AccountId`,
  `ContractMultiplier`). Il symbol interno **non** viene rinominato: il motore indicizza prezzi,
  barre e chiavi di posizione sul symbol Piootoo normalizzato, e rinominarlo a monte lo lascerebbe
  senza prezzi. Il symbol del broker serve a chi inoltra l'ordine, non a chi lo simula.

Un symbol mappato ma **disabilitato** non è operativo sull'account: i suoi segnali vengono scartati
e registrati come anomalia nel `backtest-log.jsonl`. Un symbol **assente** dalla tabella non è un
errore: nessuna conversione, moltiplicatore 1.

Un `AccountId` che non esiste, o un `SymbolConversionCode` valorizzato ma assente dal registro,
fanno fallire il run. Proseguire 1 a 1 falserebbe le size in silenzio, ed è esattamente la classe
di errore che il progetto tratta come inaccettabile (vedi `docs/PROGETTO.md` §7).

## Riferimenti codice

- `Piootoo.Shared/Models/Workspaces/WorkspaceModels.cs` — `WorkspaceAccount`, `SymbolConversion`,
  `AccountSymbolMapping`
- `Piootoo.Core/Services/AccountSymbolConversion.cs` — tabella risolta per il loop
- `Piootoo.Core/Services/WorkspaceService.cs` — CRUD account, CRUD tabelle di conversione,
  `ResolveSymbolConversionMappings`, account di default
- `Piootoo.Core/Services/PiootooBacktestingService.cs` — `ResolveAccountConversion`,
  `TryApplyAccountConversion`, `ToPersistedSignals`
- `Piootoo.Core/Services/TradingSessionService.cs` — `ResolveAccountConversion` per sessione
- `PiootooApp.Server/Controllers/AccountsController.cs`,
  `PiootooApp.Server/Controllers/SymbolConversionsController.cs` — endpoint
- `piootooapp.clientform/Shell/Screens/AccountDetailScreen.cs`,
  `piootooapp.clientform/Shell/Screens/SymbolConversionListScreen.cs`,
  `piootooapp.clientform/Shell/Screens/SymbolConversionDetailScreen.cs`
- `piootooapp.clientform/WorkspaceBacktestingForm.cs` — tab Accounts e selettore nel tab
  Backtesting della console legacy
