# Strategie e catalogo

> Bozza — contenuto da scrivere, tranne la sezione sulla convenzione di nome.

## Convenzione di nome delle strategie PTS

`PTS_[SYMBOL]_[ENG]_[NNN]_[TF]` — per esempio `PTS_NQ_PCH_001_15`, la prima
PriceChannel su NQ a 15 minuti.

| Campo | Significato |
|---|---|
| `SYMBOL` | Simbolo senza il prefisso feed: `@NQ` diventa `NQ`. |
| `ENG` | Sigla di tre lettere del motore. `TFM` = `TfMirroredEngine`, `PCH` = `PriceChannelEngine`. |
| `NNN` | Progressivo a tre cifre, **che riparte da 001 per ogni coppia (symbol, motore)**. |
| `TF` | Timeframe in minuti, coerente con `TimeframeMinutes`. |

La sigla del motore precede il numero perché il numero da solo è ambiguo: senza
`PCH` non si distingue la prima PriceChannel su NQ dalla prima TfMirrored sullo
stesso simbolo.

Per le PTS `Id` (nome della classe, che è anche il nome del file) e `Name` /
`StrategyCode` coincidono — resta valida la distinzione generale descritta in
`../PROGETTO.md` §3, ma tenerli allineati evita di dover passare da
`StrategyCatalog.ResolveCodes` per risalire dal trade al sorgente.

`PtsNamingConventionTests` verifica formato, coerenza fra nome e proprietà,
presenza della sigla motore e contiguità dei progressivi. Aggiungendo un motore
va dichiarata la sua sigla nella tabella `EngineCodes` del test, altrimenti
fallisce.

**Le `Easy_*` non seguono questa convenzione**: mantengono
`Easy_[numero]_[symbol]_[tf]`, dove il numero è quello del sorgente
EasyLanguage in `piootoo-repository/easy/`. Rinominarle spezzerebbe la
tracciabilità verso l'origine.

## Esportare la scheda di una strategia

La voce *Strategie* della console elenca le strategie del **workspace corrente**, non il
catalogo: sono gli Id del suo `masterfilter.json` risolti sul catalogo del server. Senza
workspace selezionato non carica nulla e lo dice; con un masterfilter vuoto manda a
«Gestisci workspace…», che è dove si sceglie cosa il workspace contiene. Il conteggio a
fondo pagina e la riga di stato portano sempre il totale del catalogo accanto, così un
sottoinsieme non si legge come "tutto", e le voci del masterfilter che nessuna strategia
soddisfa — un Id scritto male, una strategia disabilitata dopo il salvataggio — vengono
elencate invece di sparire.

Da lì, *Esporta griglia…* salva un **array JSON con una scheda per ogni riga in griglia** —
filtro compreso, nello stesso ordine: con un filtro attivo escono solo le righe filtrate,
che è il modo per portarsi via un mercato o un motore alla volta senza scegliere gli id a
mano. Senza filtro escono tutte le strategie del workspace. Come ordine di grandezza, il
catalogo intero (111 strategie) sono circa 4,3 MB.

Lo costruisce il server — `POST api/strategies/export` con la lista di id,
`StrategyExportService` — perché le strategie sono classi compilate e i loro parametri sono
leggibili solo da chi le istanzia; la console riceve il testo e lo scrive, senza rileggerlo.
`GET api/strategies/{id}/export` resta per la singola strategia. **Un id sconosciuto fa
fallire tutta la richiesta**: un array più corto di quanto chiesto non si distingue da uno
completo.

Cosa c'è in ogni elemento:

| Blocco | Cosa porta |
|---|---|
| `identity` | I due identificatori, simbolo, timeframe, `requiredCandles`, tenuta dichiarata. |
| `instrument` | Valore del punto, tick e fuso di sessione: senza, `stopMoney: 4000` non è confrontabile con i «200.0 pt» del dossier. |
| `parameters` | I campi del motore **letti dall'istanza**, ognuno con la classe che lo dichiara. Sono i numeri della traduzione. |
| `conversion` | Sigla del motore, motore C#, file Python, scheda di dossier risolta e S-ID dichiarato. |
| `sources` | I testi integrali: sorgente C# della classe e del motore, motore Python, scheda del dossier. |
| `warnings` | Cosa non si è potuto raccogliere e perché. |

Due cose che l'export dichiara e conviene leggere.

**Cosa è autorevole.** I documenti con `fromAssembly: true` sono il sorgente compilato
*dentro il binario che ha risposto* — è per questo che i `.cs` delle strategie e dei
motori sono `EmbeddedResource` in `Piootoo.Strategies.csproj`, oltre che compilati. Un
checkout accanto al server descriverebbe codice che il server potrebbe non star
eseguendo. Motore Python e scheda del dossier vengono invece dal repository dati e sono
marcati `fromAssembly: false`: possono essere stati rigenerati dopo la traduzione.

**L'aggancio al dossier non passa dagli S-ID.** Sono ordinati per atteso/trade e scorrono
a ogni rigenerazione: `PTS_ES_PCH_001_60` dichiara `S43`, che nell'edizione corrente è una
NQ 15m TF_M. L'export usa l'**impronta numerica** — simbolo, timeframe, motore, stop e
target in denaro — la stessa chiave di `tools/dossier-diff.py`, e riporta a parte l'S-ID
dichiarato con un warning quando i due divergono (vedi
[`mappa-strategie-pts.md`](mappa-strategie-pts.md)). Quattro impronte del dossier corrente
sono condivise da due schede: lì l'export le allega **entrambe** e lo dice, invece di
sceglierne una. Sul catalogo attuale 108 strategie su 111 trovano la propria scheda; le
tre restanti vengono da un'edizione precedente e lo dichiarano nei `warnings`.

Il percorso del dossier è la costante `StrategyExportService.DossierRelativePath`: va
spostata con ogni edizione nuova, insieme ai due script in `tools/`, e
`StrategyExportTests` fallisce se resta indietro.

## Resto da coprire:

- Interfaccia `ITradingStrategy`: contratto minimo (`GenerateSignal`,
  `Initialize`) e cosa deve garantire un'implementazione.
- Catalogo strategie: come viene costruito e come si registra una nuova
  strategia.
- Generazione automatica da EasyLanguage (`Piootoo.Strategies/Easy/`): script
  `GenerateAllStrategies.ps1`, sorgenti in `piootoo-repository/easy/`, output
  `Easy_*.cs`. Vedi `Piootoo.Strategies/Easy/README.md` per i dettagli d'uso
  dello script.

Riferimenti codice: `Piootoo.Core/Services/StrategyFactory.cs`,
`Piootoo.Core/Services/StrategyExportService.cs`, `Piootoo.Strategies/`.
