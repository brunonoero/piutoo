# Account e tabella di conversione symbol

Un **account** è la configurazione di un conto operativo dentro un workspace: anagrafica, balance
iniziale, gruppo anti copy-trading e — soprattutto — la **tabella di conversione symbol** che
traduce il mondo Piootoo nel mondo del broker.

La tabella risponde a due domande, una per riga:

- con che nome questo simbolo esiste sull'account (`@NQ` → `USDTEC`);
- quanto vale un contratto dell'account rispetto a un contratto Piootoo
  (`ContractMultiplier`: 0,1 se il contratto del broker vale 100k contro 1M).

Il balance iniziale sta **sull'account**, non sulla riga: è il capitale del conto, non un'allocazione
per strumento.

## Dove vivono i dati

| File | Contenuto |
|---|---|
| `<workspace>/accounts.json` | Tutti gli account del workspace (`WorkspaceAccountsFile`). |
| `settings/default-symbol-conversion.json` | Preset condiviso della tabella di conversione. |

Ci sono due tabelle "di partenza" ed è una distinzione voluta:

- **Identità** — ricalcolata dal catalogo a ogni richiesta, mai persistita: `@GC` → `@GC`,
  moltiplicatore 1. È la base di ogni **account nuovo** e dell'account `Default`. Garantisce che un
  account appena creato sia idempotente: non converte niente finché non lo si decide.
- **Preset condiviso** — `settings/default-symbol-conversion.json`, generato la prima volta come
  identità ma da lì in poi un normale file editabile. Si carica nel tab con *Carica preset* e si
  risalva con *Salva come preset*.

Se i due coincidessero, modificare il preset cambierebbe in silenzio il punto di partenza di ogni
account futuro — esattamente ciò che l'idempotenza deve escludere.

L'account `Default` (creabile dal tab o via `POST .../accounts/default`) è l'identità materializzata:
mappatura 1 a 1, moltiplicatore 1, balance iniziale 1.000.000. È l'account da usare per un run senza
conversioni che resti comunque tracciabile.

## Interfaccia

Nel tab **Accounts** workspace e account sono due combo affiancate; il pannello sotto mostra
anagrafica e tabella di conversione dell'account selezionato. *Nuovo account…* apre una **modale**
che raccoglie solo l'anagrafica (nome obbligatorio, balance preimpostato a 1.000.000) e crea
l'account già popolato con l'identità: la tabella si rifinisce poi nel tab.

## API

Tutto passa da HTTP come il resto della console: il client WinForms non tocca il filesystem.

```
GET    api/Workspace/accounts/symbol-identity
GET    api/Workspace/accounts/symbol-preset
PUT    api/Workspace/accounts/symbol-preset
GET    api/Workspace/{workspaceId}/accounts
POST   api/Workspace/{workspaceId}/accounts
POST   api/Workspace/{workspaceId}/accounts/default
GET    api/Workspace/{workspaceId}/accounts/{accountId}
PUT    api/Workspace/{workspaceId}/accounts/{accountId}
DELETE api/Workspace/{workspaceId}/accounts/{accountId}
```

L'`Id` dell'account è lo slug del nome, con la stessa normalizzazione degli id di workspace.

## Effetto sul backtest

`BacktestingRequest.AccountId` è opzionale. Quando è valorizzato, il run risolve **una volta sola**
la tabella in un `AccountSymbolConversion` (dizionario per simbolo normalizzato: niente lookup
costosi nel loop caldo) e la applica in due punti diversi, per due ragioni diverse:

- **Size** — il moltiplicatore scala `signal.Quantity` *prima* che il motore veda il segnale, così
  trade, equity e drawdown riflettono i contratti realmente inviabili su quel conto. La quantità
  resta `decimal` fino a trade e P&L: conversioni come `1 × 0,01 = 0,01` non vengono arrotondate
  artificialmente a un contratto.
- **Symbol** — la traduzione finisce solo in `signals.json` (`AccountSymbol`, `AccountId`,
  `ContractMultiplier`). Il simbolo interno **non** viene rinominato: il motore indicizza prezzi,
  barre e chiavi di posizione sul simbolo Piootoo normalizzato, e rinominarlo a monte lo lascerebbe
  senza prezzi. Il simbolo del broker serve a chi inoltra l'ordine, non a chi lo simula.

Un simbolo mappato ma **disabilitato** non è operativo sull'account: i suoi segnali vengono scartati
e registrati come anomalia nel `backtest-log.jsonl`. Un simbolo **assente** dalla tabella non è un
errore: nessuna conversione, moltiplicatore 1.

Un `AccountId` che non esiste fa fallire il run. Proseguire 1 a 1 falserebbe le size in silenzio, ed
è esattamente la classe di errore che il progetto tratta come inaccettabile (vedi
`docs/PROGETTO.md` §7).

## Riferimenti codice

- `Piootoo.Shared/Models/Workspaces/WorkspaceModels.cs` — `WorkspaceAccount`, `AccountSymbolMapping`
- `Piootoo.Core/Services/AccountSymbolConversion.cs` — tabella risolta per il loop
- `Piootoo.Core/Services/WorkspaceService.cs` — CRUD account, preset, account di default
- `Piootoo.Core/Services/PiootooBacktestingService.cs` — `ResolveAccountConversion`,
  `TryApplyAccountConversion`, `ToPersistedSignals`
- `PiootooApp.Server/Controllers/WorkspaceController.cs` — endpoint
- `piootooapp.clientform/WorkspaceBacktestingForm.cs` — tab Accounts e selettore nel tab Backtesting
