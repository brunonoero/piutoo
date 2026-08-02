# Generare i feed JSON dai CSV storici

I dati di backtest stanno fuori dal codice, in `piootoo-repository/datafeed/`,
un file JSON per simbolo, timeframe e giorno. Quando una strategia nuova chiede
una coppia `(simbolo, timeframe)` che non c'è, il backtest deve fallire con un
errore esplicito: non esiste fallback su un timeframe vicino, ed è voluto. Il
feed va generato.

Questo documento descrive come si genera un timeframe partendo dai CSV minute
in `piootoo-repository/datafeed-future/FUTURES_Historical_Data/`, e come si
verifica che il risultato sia corretto.

## Struttura attesa dal server

`DataSourceRepository` cerca i file in `datafeed/<timeframe>/<simbolo>/` con
nome `<simbolo>-<AAAAMMGG>.json`, e i nomi di cartella e di `barType` sono
chiusi: `1m`/`OneMinute`, `5m`/`FiveMinute`, `15m`/`FifteenMinute`,
`30m`/`ThirtyMinute`, `1h`/`OneHour`, `4h`/`FourHour`, `D`/`Daily`. Un nome
inventato rende il feed invisibile al server senza alcun messaggio, quindi lo
script tiene la tabella in una costante sola, `CANONICAL_TIMEFRAMES`, invece di
comporre le stringhe al volo.

Oggi il repository contiene @NQ nei sei timeframe da 5m a giornaliero, 5.081
file per ciascuno.

## Lo script

`piootoo-repository/datafeed-future/aggregate_nq_ascii.py` legge il CSV una
volta sola e alimenta tutti i timeframe richiesti nella stessa passata. Non è
un dettaglio di prestazioni: il CSV ha milioni di righe, e leggerlo una volta
per timeframe significava anche poter produrre serie non allineate fra loro se
qualcuno cambiava un parametro fra una corsa e l'altra.

```bash
# tutti i timeframe di default (5, 15, 30, 60 minuti)
python aggregate_nq_ascii.py --source-timezone UTC

# solo quelli che mancano, senza toccare gli altri
python aggregate_nq_ascii.py --source-timezone UTC --timeframes 240 1440

# rigenerazione completa, sovrascrivendo
python aggregate_nq_ascii.py --source-timezone UTC --timeframes 5 15 30 60 240 1440 --overwrite
```

`--source-timezone` è **obbligatorio** e senza default, perché il CSV non
dichiara il proprio fuso e indovinarlo è esattamente l'errore che si vuole
impedire. Vedi più sotto.

Senza `--overwrite` lo script si rifiuta di scrivere in una cartella che
contiene già dei feed, e il controllo avviene **prima** di leggere il CSV, su
tutti i timeframe richiesti. Se avvenisse durante la scrittura del singolo
giorno, accorgersi di un file già presente a metà corsa lascerebbe i feed nuovi
troncati a una data intermedia — e un feed troncato non si distingue da un feed
i cui dati finiscono lì.

Ogni file viene scritto su un temporaneo e poi rinominato, con `fsync`: un
interruzione a metà non lascia JSON parziali che il server proverebbe a
leggere.

## Il fuso del CSV, e perché è la cosa che si sbaglia

L'etichetta `Z` nei JSON generati non certifica nulla: dice solo che qualcuno
ha passato `UTC` a `--source-timezone`. È successo, e per @NQ è ancora così: i
feed attualmente nel repository sono stati generati con `--source-timezone
UTC`, ma il CSV sorgente **è in ora europea** (CET/CEST). Le barre sono quindi
etichettate UTC pur non essendolo, con sette ore di scarto rispetto all'ora di
Chicago con cui le strategie @NQ dichiarano la propria sessione.

Prima di generare un feed nuovo, accerta il fuso del CSV dai dati, con le due
misure descritte in
[`orari-di-sessione-e-fusi.md`](orari-di-sessione-e-fusi.md): dove cade il
picco di volume e dove cade la pausa di manutenzione. Devono concordare.

Su Windows serve il pacchetto `tzdata` perché `zoneinfo` risolva gli
identificatori IANA (`pip install tzdata`), altrimenti anche
`Europe/Rome` fallisce con `ZoneInfoNotFoundError`.

Nota che cambiare il fuso di generazione **non sposta solo le etichette**:
sposta i confini dei bucket, quindi le barre a 4h e giornaliere aggregano
minuti diversi. Se rigeneri un timeframe con un fuso diverso devi rigenerarli
tutti, altrimenti le serie non sono più confrontabili fra loro.

## Allineamento dei bucket

`bucket_start` allinea sempre a partire dalla mezzanotte, non dall'ora
corrente: così il 4h cade su 00, 04, 08, 12, 16 e 20, e la regola vale
identica per tutti i timeframe. Da qui il vincolo che lo script verifica in
ingresso, cioè che il timeframe divida i 1440 minuti del giorno; un timeframe
che non lo fa produrrebbe barre che scivolano di giorno in giorno.

Il **settimanale non si genera**: l'allineamento dalla mezzanotte non sa dove
comincia la settimana, e nessuna strategia lo chiede. Lo script lo rifiuta
esplicitamente invece di produrre barre plausibili e sbagliate.

Il **giornaliero** è il giorno di calendario del CSV, e per questo feed non è
una scelta arbitraria: nell'orologio del sorgente la pausa di manutenzione cade
a cavallo della mezzanotte, quindi il giorno di calendario contiene la sessione
completa. Resta imperfetto nelle due o tre settimane all'anno in cui l'ora
legale europea e quella americana non sono allineate, circa l'8% dei giorni del
campione. Per le strategie EasyLanguage questo non conta, perché le loro barre
di sessione si costruiscono a runtime dal timeframe intraday con l'orario
dichiarato dalla singola sorgente (`EasyLib.BuildSessionSeries`), non da questo
feed: il giornaliero serve alle analisi, non alla segmentazione.

## Verificare un feed appena generato

Tre controlli, in ordine di quanto spesso hanno trovato qualcosa.

**Coerenza fra timeframe.** Le barre di un timeframe grosso devono essere
esattamente l'aggregazione di quelle del timeframe fine: `open` della prima,
`close` dell'ultima, `high`/`low` estremi, `volume` somma. È il controllo più
forte perché indipendente dal fuso, e va fatto su tutte le coppie generate.

**Conteggio dei file.** Tutti i timeframe generati dallo stesso CSV devono
avere lo stesso numero di file, uno per giorno di calendario con almeno una
barra. Una differenza significa che uno dei due contiene giorni che l'altro non
ha, ed è così che è emerso l'unico errore reale trovato finora: il 15m aveva
due file in più, `@NQ-20060401.json` e `@NQ-20061001.json`, entrambi con barre
fantasma su date di weekend. Erano il residuo di una generazione precedente che
leggeva il CSV come mese/giorno invece che giorno/mese: il 4 gennaio e il 10
gennaio erano finiti in aprile e in ottobre. Sono stati cancellati.

**Formato della data del CSV.** Il file ASCII Mapping usa giorno/mese/anno.
Le prime righe sono ambigue (`03/01/2006` è valido in entrambe le letture), e
la conferma sta più avanti nel file, dove compare `13/01/2006`. Su un CSV nuovo
questa verifica va rifatta: leggere il formato al contrario non produce errori,
produce date sbagliate solo per i giorni oltre il dodici.

## Riferimenti codice

- `piootoo-repository/datafeed-future/aggregate_nq_ascii.py` — lo script, con
  `CANONICAL_TIMEFRAMES`, `bucket_start`, `TimeframeFeed`.
- `Piootoo.Domain/Repositories/DataSourceRepository.cs` — `TimeframeFolders` e
  `CanonicalBarTypes`, cioè i nomi che lo script deve rispettare.
- [`orari-di-sessione-e-fusi.md`](orari-di-sessione-e-fusi.md) — come si
  accerta l'orologio di un feed e perché conta.
- [`orologio-barre-e-fill.md`](orologio-barre-e-fill.md) — cosa succede nel
  backtest quando il feed ha buchi.
