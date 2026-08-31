# Confronti fra run

Questa cartella tiene gli artefatti dei confronti (`compare-NNNN/`), esportati a
mano dai run e dagli eventi cTrader. Nessun codice li scrive e nessun codice li
legge: l'unico strumento è `confronta-trades.py`, che prende i due percorsi come
argomenti.

## I tre tipi di backtest

Non sono varianti dello stesso run: cambiano **il motore che esegue** e
**l'archivio di barre da cui legge**, e ogni combinazione ha prezzi propri.

| tipo | slug | motore | barre |
|---|---|---|---|
| backtest del cBot | `cbot-cfd-{BROKER}` | `PiootooDistributedExecutionBot` dentro cTrader | CFD del broker, dal backtester di cTrader |
| backtest interno futures | `interno-futures` | `PiootooTradingService` | `piootoo-repository/datafeed/` (CSV del vendor, `@SYM` retro-aggiustati) |
| backtest interno CFD | `interno-cfd-{BROKER}` | `PiootooTradingService` | `piootoo-repository/datafeed-external/{BROKER}/` (barre raccolte dal bot) |

Le due gambe *interne* isolano il feed a parità di motore; `cbot-cfd` contro
`interno-cfd` isola il motore a parità di feed — nei limiti in cui l'archivio del
raccoglitore e le barre del backtester cTrader coincidono. Confrontare
`interno-futures` con `cbot-cfd`, come in `compare-0012`, cambia **entrambe** le
cose insieme: si misura la somma, non i due addendi.

## Come si chiamano i file

    <artefatto>-<slug>.json

cioè `trades-interno-futures.json`, `signals-cbot-cfd-ICS.json`,
`backtest-summary-interno-cfd-RAWTRADINGLTD.json`. Gli eventi esportati da
cTrader restano con il nome che dà cTrader: contiene già i parametri del bot.

**Il broker fa parte del nome ogni volta che il feed è CFD.** Due broker
chiudono le stesse candele su prezzi diversi: `ICS` e `RAWTRADINGLTD` non sono
la stessa serie e i loro run non sono confrontabili fra loro. È la stessa regola
per cui `backtest-summary.json` dichiara `datafeedBroker` (null = interno). Lo
slug del broker è quello della cartella sotto `datafeed-external/`, o il campo
`Broker` di `workspaces/accounts/accounts.json` in maiuscolo, per un run del
cBot che gira su un conto.

Il nome dice il *tipo*, mai il verdetto: niente `-buono`, `-vecchio`, `-fix`. La
finestra temporale e la versione del motore stanno dentro gli artefatti
(`backtest-summary.json` porta `engineVersion` e `datafeedBroker`).

## Lo slug non si digita: lo scrive il run

`origin.json`, che ogni run lascia nella propria cartella, dichiara il motore e
la serie di prezzi, e da quei due campi calcola lo slug:

```json
{
  "Origin": "ExternalBroker",
  "PriceSource": { "Kind": "BrokerCfd", "Broker": "ICS" },
  "EngineVersion": "3.11.0",
  "AccountNumber": "1075035",
  "RunSlug": "cbot-cfd-ICS"
}
```

Il nome serve a te che guardi la cartella, il file serve quando il nome è stato
scritto male. **Il contenuto è l'autorità.**

Non serve copiarli a mano: nel dettaglio del backtest, in console, il pulsante
**Esporta per confronto** chiede una cartella e ci scrive `trades-<slug>.json`,
`backtest-summary-<slug>.json` e `run-<slug>.json` — il marcatore rinominato, che
viaggia con i trade. Punta due run alla stessa cartella e hai il confronto
pronto. Il server compatta il journal `.jsonl` prima di leggere, quindi anche un
run appena finito esce completo, e `signals.json` resta fuori: nei portafogli
arriva a centinaia di megabyte e il confronto lavora sui trade.

Un run che non sa dire su quali prezzi è girato **non si esporta** (`409`).
Vale anche per un CFD di cui non si conosce il broker: `cbot-cfd` non identifica
una serie di prezzi, e un artefatto rinominato a mano è un'affermazione che
nessuno ha verificato.

Non c'è un campo "tipo di run": i tre tipi *sono* il prodotto di `Origin` e
`PriceSource`, e un quarto campo sarebbe solo una cosa in più che può
contraddire le altre. Le cartelle prodotte prima di questo campo danno
`interno-feed-sconosciuto` — di un run interno vecchio il feed non è
ricostruibile, e tirare a indovinare è peggio che dirlo.

## Ogni confronto lascia un `esito.md`

Gli artefatti dicono *cosa è successo in un run*, mai *cosa ha detto il confronto*:
`backtest-summary.json` è la scheda di **una gamba sola** — finestra, `holding`,
`datafeedBroker`, `engineVersion`, diagnostica — e non sa nemmeno di essere stato
confrontato con qualcosa. Quindi ogni `compare-NNNN/` tiene un `esito.md` scritto a mano
a fine analisi, e **quello è l'unico file che risponde alla domanda "com'è andata"**.

Scheletro, nell'ordine (salta le sezioni vuote, non inventarle):

1. **Titolo e data** dell'analisi, più il link al report se ce n'è uno.
2. **Le due gambe**: una riga per lato con file, slug del tipo, motore, serie di prezzi,
   arco. Segnala qui se un campo è stato *dedotto* invece che letto da `origin.json`.
3. **Esito**: finestra comune, trade e saldo per lato **convertiti**, divario in valore e
   in percentuale, e se il divario è deriva o cambia segno.
4. **Scomposizione**: da cosa viene il divario, in tabella, con le quote che sommano.
5. **Aperto** / **Chiuso**: le ipotesi, una riga l'una col perché. Una pista chiusa si
   scrive *chiusa*, con la prova: è ciò che impedisce di riaprirla fra un mese.
6. **Cosa torna**: quello che è stato verificato uguale e non va più guardato.
7. **Trappole di misura di questa cartella**: gli errori in cui si è già cascati.

Numeri sempre con la loro unità e la loro valuta, e mai un saldo esterno grezzo
presentato come confrontabile.

## Cosa finisce in git

Solo il testo e le schede: `esito.md`, `README.md`, `backtest-summary*.json`,
`origin.json`, `run-*.json`, `confronta-trades.py`. Trades, signals, rotation-log ed
export cTrader restano **fuori** (regole in fondo al `.gitignore` della solution): sono
centinaia di MB e si riottengono riesportando il run. Conseguenza da tenere a mente:
**chi clona il repo trova gli esiti ma non i dati** — se un numero dev'essere
riverificabile, mettilo nell'`esito.md`, non lasciarlo implicito in un JSON che quella
persona non avrà.

## Materiale sciolto alla radice

`signals-backtest.json` / `signals-external.json` e l'`.xlsx` degli eventi sono
dell'indagine sulla divergenza GC, non di un `compare-NNNN/`. `last-backtest/` è lo
scarico dell'ultimo run del cBot e viene sovrascritto: per analizzare, copia prima in un
`compare-NNNN/`.

## Prima di confrontare due file

Tre controlli che hanno già invalidato un'analisi ciascuno:

- **Arco temporale.** Un run del cBot si ferma dove si è fermato: restringere
  entrambi i lati alla finestra comune prima di sommare qualsiasi cosa.
- **Valuta.** I trade del cBot sono nella valuta del conto e il cambio si muove
  *dentro* il run — vanno convertiti trade per trade, non con una costante. Il
  cambio implicito di un trade è `grossProfit / punti / valore-punto-in-USD`.
- **Famiglie d'uscita.** `LocalExit:StopLoss` di cAlgo copre stop, trailing e
  break-even insieme, `LocalExit:Closed` copre time exit, max bars, flat e
  segnale opposto. Confrontarle una a una fabbrica trade "senza controparte" che
  non esistono.
