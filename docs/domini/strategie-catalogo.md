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
`Piootoo.Strategies/`.
