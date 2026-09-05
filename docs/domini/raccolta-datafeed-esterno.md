# Raccolta del datafeed esterno (cBot → server → `datafeed-external/`)

Come si costruisce un datafeed su disco a partire da un broker cTrader, un pezzo
alla volta, senza timeout e senza perdere il lavoro fatto se qualcosa si ferma a
metà.

Il feed del vendor (`datafeed/@NQ_60.json`, prodotto da
`datafeed-future/aggregate_flat_feed.py` — vedi
[`datafeed-generazione.md`](datafeed-generazione.md)) resta la sorgente di
riferimento. Questa è la seconda strada: i dati del broker su cui si opera
davvero, raccolti dal conto, tenuti separati.

## Il problema

In sessione `ExternalBroker` il server non ha datafeed proprio: la storia è solo
quella che il client gli spinge, e resta in RAM (vedi
[`finestra-candele-e-riscaldamento.md`](finestra-candele-e-riscaldamento.md)). Il
datafeed su disco è compito di un bot raccoglitore dedicato.

Il modo ovvio di scriverlo — un giro di `LoadMoreHistory` fino in fondo dentro
`OnStart`, poi un unico POST con tutto — non funziona per tre motivi che si
presentano insieme:

- il thread dell'algoritmo resta bloccato per minuti e la piattaforma lo tratta
  come un bot piantato;
- un invio da centomila barre supera qualunque timeout HTTP ragionevole;
- se muore a metà non resta niente di riutilizzabile, e il giro dopo ricomincia
  da zero.

## La forma della soluzione

**L'unità è il blocco**, non il feed. Piccolo (default cinque giorni di
calendario, al massimo 2000 barre), autonomo, **idempotente**: la chiave di una
barra è il suo istante di apertura, quindi rimandare un periodo non produce
righe in più. Da qui discende tutto il resto — si può completare un feed in
cento invii, in ordine qualsiasi, su più sessioni, riprendendo dopo un crash.

```
cBot                                    server
 |  GET  status?broker&symbol&tf    -->  cosa ho già: primo/ultimo, buchi
 |  POST bars   (blocco 1)          -->  journal .jsonl (append, no fsync)
 |  POST bars   (blocco 2)          -->  journal
 |  ...                                  (a soglia: compattazione)
 |  POST compact                    -->  @NQ_60.json (scrittura atomica durabile)
```

### Perché un journal e non il file

Riscrivere il file piatto a ogni blocco costa quanto tutto il feed già raccolto,
e rende quadratico un backfill che è lineare. È la stessa trappola dei
checkpoint di `TradingJsonStore` (CLAUDE.md, "I checkpoint non riscrivono
l'artefatto intero"). I blocchi si accodano a
`{BROKER}/.pending/@SYM_{minuti}.jsonl`; il file piatto viene materializzato
alla compattazione:

- a soglia (20 000 barre nel journal);
- su richiesta esplicita del bot a fine backfill di uno stream, e allo
  spegnimento se si è fermato a metà;
- **sempre prima di rispondere a una `status`** — una status che ignorasse il
  journal direbbe al bot che gli mancano barre appena spedite, e il bot le
  rispedirebbe all'infinito.

### Sovrapposizioni e buchi

Le sovrapposizioni **collassano da sole**: barra identica = duplicato, non si
scrive niente; barra diversa sullo stesso istante = vince l'ultima arrivata,
perché rimandare un periodo è il modo con cui si corregge una barra sbagliata.

I buchi **non si riempiono, si dichiarano** (`gaps` nella status). Inventare
barre mancanti è esattamente ciò che un datafeed non deve fare. Il passo con cui
si decide cos'è un buco è **dedotto dai dati** (`dominantStepMinutes`), non
assunto dal timeframe: un giornaliero di broker apre alle 22:00 o alle 23:00 UTC,
non a mezzanotte, e assumere l'allineamento all'epoch farebbe comparire un buco
per ogni giornata. Ogni buco porta `spansWeekend`: il mercato chiuso non è storia
mancante, e il bot non deve richiederlo al broker all'infinito.

`spansWeekend` è vero solo se fra le due barre c'è un sabato o una domenica **e
nessun giorno feriale intero**. La seconda metà della regola non è un dettaglio:
il bot *salta* i blocchi che cadono dentro un buco marcato weekend, e un buco di
tre anni di sabati ne contiene centocinquanta. Finché bastava contenerne uno, un
feed fatto di tre barre vecchie più due mesi recenti veniva dichiarato coperto
per tutto quello che c'era in mezzo e non si riempiva mai, per quanti run gli si
dessero — il bot concludeva "finestra coperta" in pochi secondi con ottanta
blocchi saltati, e il backtest continuava a trovare zero candele. Vedi
`docs/decisioni.md` 2026-09-03.

## La griglia oltre l'ora

Un timeframe alto **non si chiede alla piattaforma**. Il grafico H4 di cTrader — e
ogni serie sopra l'ora — è ancorato all'orologio del broker; il feed Piootoo e i
run di ricerca sono ancorati all'**inizio sessione del giorno di calendario
europeo** (`ZonedWindow.ResearchSession()`, `aggregate_flat_feed.py`, vedi
[`datafeed-generazione.md`](datafeed-generazione.md) §"Dove cadono i bucket"). Le
due griglie non coincidono, e la differenza non dà errore: dà barre diverse —
apertura, estremi e volume diversi — che guardando il file non si distinguono. Un
backtest su quel feed non corrisponde ad alcun run di ricerca, e nemmeno a
`datafeed/`.

Il bot sottoscrive quindi una **serie base fino a sessanta minuti** (l'ora, salvo
forzatura) e costruisce i bucket in codice:

- il bucket è `[inizio sessione + k·tf, +tf)` nell'orologio dichiarato, e una barra
  base appartiene al bucket che contiene la sua **apertura**;
- l'etichetta è l'**inizio** del bucket: è la chiave con cui il server deduplica ed
  è la convenzione di tutto il feed;
- fino a sessanta minuti la serie della piattaforma va bene com'è, perché lo scarto
  di un fuso è un numero intero di ore e i due allineamenti coincidono comunque. È
  il motivo per cui la soglia sta a 60 e non più in basso;
- un timeframe che non divide il giorno viene rifiutato, come nell'aggregatore: i
  bucket scivolerebbero di giorno in giorno rispetto alla sessione. Il settimanale
  non si raccoglie.

**Le formule della ricerca sono le stesse, scritte su etichette di chiusura.**
`resample_ohlcv` e `aggregate_flat_feed.py` lavorano su righe il cui timestamp è la
*fine* del periodo, e da lì vengono il `bin = (minuti_da_inizio_sessione − 1) / 240`
e l'etichetta a `inizio + (bin+1)·240`. Le barre di cTrader portano invece l'orario
di **apertura**: contando dall'apertura i confini dei bucket sono gli stessi e
l'etichetta è già quella che il feed vuole.

**Il confine si calcola sull'orologio locale, non sottraendo il resto all'istante
UTC.** La scorciatoia `openUtc − resto` — quella dell'aggregatore Python — conta
minuti locali su un istante UTC, e sul cambio d'ora scavalca l'ora che non esiste.
Misurata su due anni di barre orarie a `Europe/Rome` (35.088 righe, 4h e
giornaliero) coincide con il calcolo locale su **34.988** e diverge sui **quattro
giorni di transizione**: lì assegna minuti della stessa ora a bucket diversi e
produce barre che si scavalcano. Nel feed del vendor non si vede, perché il cambio
d'ora cade di domenica a mercato chiuso — nei tredici file 240 e 1440 di
`datafeed/` non c'è un solo passo più corto del timeframe — ma un broker che quoti
la domenica lo farebbe comparire, e un feed con due barre sovrapposte non è più
ordinato. Le due giornate all'anno in cui l'orario locale del confine non esiste o
esiste due volte si risolvono con le convenzioni di `SessionClock.ToUtc`: avanti
dell'ampiezza del salto, e prima delle due occorrenze.

**Il file dichiara la propria griglia.** Il campo `source` del feed raccolto porta
`... griglia(60m->240m, Europe/Rome 00:00)`. Due file identici nella forma ma nati
su ancoraggi diversi non sono confrontabili, e senza questo non si
distinguerebbero.

I blocchi del backfill si arrotondano ai confini dei bucket, verso il basso: un
bucket a cavallo della fine del blocco non si tocca — lo prende il blocco
successivo, che ha quel confine come inizio — e il tetto di barre per invio ferma
il giro sull'ultimo bucket **completo**. Un bucket a metà nel feed sarebbe un dato
falso che poi nessuno distingue da uno vero, esattamente come la barra in
formazione. A regime vale la stessa regola: un bucket si spedisce solo quando la
barra base in formazione ne ha già cominciato un altro.

### La stessa griglia nei cBot operativi

La regola non riguarda solo il feed su disco: **vale identica in esecuzione**, e per
un motivo preciso. In sessione `ExternalBroker` il server non aggrega niente —
`TradingSessionService.PushBars` valida, deduplica sulla chiave di idempotenza,
**accoda** a `session.History[(simbolo, timeframe)]` e valuta. Non c'è
ricampionamento da nessuna parte. Le barre su cui girano le strategie in live sono
quindi *esattamente* quelle che il cBot spinge: se il cBot prende l'H4 della
piattaforma, l'esecuzione gira su candele che il backtest non ha mai visto, e
nessuno se ne accorge dai numeri. Per questo `PiootooDirectExecutionBot` e
`PiootooDistributedExecutionBot` sottoscrivono la serie base e piegano i bucket con
lo stesso codice del raccoglitore, e hanno gli stessi tre parametri.

Da lì discendono due conseguenze che nei bot operativi non sono opzionali.

**L'orologio a barre non è più `Series.Count`.** La scadenza di un ordine "next
bar" e `MaxBarsInPosition` sono espressi in barre *della strategia*: contarli sulla
serie base farebbe scadere l'ordine di una 240 dopo un'ora e chiuderebbe la
posizione quattro volte troppo presto. Ogni stream tiene quindi il proprio
conteggio di bucket chiusi, e la marcatura del pending si scrive e si confronta su
quello. Nel bot distribuito il pending porta direttamente l'istante della barra su
cui è nato.

**La barra base non è la barra della strategia.** Il battito della serie arriva
quattro volte per candela di una 240: la passata di barra — ritiro dell'ordine
scaduto, push, claim del segnale — si fa solo quando il bucket si chiude davvero.
Break-even, trailing e uscite a tempo non passano di lì: girano già a ogni tick e a
ogni battito del timer, quindi restano tempestive come prima.

E poiché più stream possono ora pendere dalla **stessa** serie base — `@FDAX/60` e
`@FDAX/240` vengono entrambi dall'ora — il match fra evento e stream non può più
fermarsi al primo che corrisponde.

## Una cartella per broker

```
piootoo-repository/datafeed-external/
  ICMARKETS/
    feed-clocks.json                     (generato, tutti UTC)
    @NQ_60.json                          (stesso formato di datafeed/)
    @NQ_15.json
    .pending/@NQ_60.jsonl                (journal, sparisce alla compattazione)
    ticks/@NQ/@NQ_ticks_20260830.jsonl
    ticks/@NQ/@NQ_ticks_state.json       (ultimo tick: il punto di ripresa)
  PEPPERSTONE/
    ...
```

Le barre dello stesso simbolo prese da due broker diversi **non sono la stessa
serie**: cambiano l'orario di sessione, il bucket in cui cade la barra e il
volume (che è conteggio tick, non contratti). Mescolarle darebbe un feed che non
corrisponde a nessuno dei due conti, e nessuno se ne accorgerebbe. Separate,
invece, `datafeed-external/ICMARKETS` è una cartella di feed completa a sé
stante — ha il proprio `feed-clocks.json` — e ci si può puntare
`DataSourceRepository` direttamente per fare un backtest su quei dati.

Il codice broker è **obbligatorio** su ogni invio, e viene ridotto a un nome di
cartella sicuro (maiuscolo, lettere/cifre/`-`/`_`): arriva da un bot, quindi da
fuori, e un `..` in un percorso costruito con `Path.Combine` uscirebbe dal
repository.

### Il manifest degli orologi

`FeedClockRegistry` si rifiuta di leggere una cartella senza `feed-clocks.json`:
è la regola che impedisce di prendere per UTC dei timestamp che non lo sono
(vedi [`orari-di-sessione-e-fusi.md`](orari-di-sessione-e-fusi.md)). Qui il fuso
è noto e vale UTC davvero — cTrader espone gli orari delle barre in UTC e il bot
li spedisce così — quindi il manifest si scrive da solo, **ma non sovrascrive
mai una voce esistente**: chi l'ha corretta a mano ha più ragione del codice.

Il bot si rifiuta di partire se `Server.Time != Server.TimeInUtc`, cioè se
qualcuno ha cambiato l'attributo `[Robot(TimeZone = TimeZones.UTC)]`: senza quel
controllo, `SpecifyKind` trasformerebbe in silenzio un orario locale in "UTC" e
il feed nascerebbe sfalsato di un'ora per sempre.

## API

Tutto sotto `api/datafeed-external`. Non esiste una chiamata "importa tutto lo
storico", ed è voluto: sarebbe l'unica che può andare in timeout.

| Verbo e rotta | A cosa serve |
|---|---|
| `POST /bars` | Accoda uno o più blocchi. Risponde con nuove / aggiornate / duplicate / scartate (con la ragione) e lo stato del journal. `compact: true` materializza subito. |
| `POST /ticks` | Accoda tick ai journal giornalieri. Risponde con `lastTickUtc`, il punto di ripresa. |
| `GET /status?broker&symbol&timeframeMinutes[&gapToleranceMinutes]` | Copertura e buchi di uno stream. È la chiamata con cui il bot decide cosa chiedere al broker e cosa saltare. |
| `GET /index[?broker][&gapToleranceMinutes]` | Tutti i feed raccolti, di tutti i broker o di uno. |
| `GET /plan-instruments?planCode[&accountNumber]` | Le coppie (simbolo, timeframe) che un piano tocca, con il nome di ogni simbolo sul conto. Lettura pura: non apre sessioni. |
| `POST /compact[?broker&symbol&timeframeMinutes]` | Materializza i journal. Senza parametri, tutto: è la chiamata da fare a mano quando un bot è morto a metà backfill. |

Una barra rotta viene **scartata con la ragione**, non accettata: OHLC incoerente,
prezzo non positivo, istante non UTC, volume negativo. Non si pretende invece
l'allineamento all'epoch — i timeframe alti del broker aprono all'orario di
sessione, e rifiutarli svuoterebbe il feed giornaliero.

## Da dove arriva l'elenco degli strumenti

Due modi, e il piano vince sui parametri manuali: tenerli entrambi vivi
significherebbe due liste destinate a divergere in silenzio.

Con **Codice piano**, le coppie (simbolo, timeframe) vengono dal **masterfilter**
del workspace del piano, e ogni simbolo arriva già tradotto nel nome che ha sul
conto (tabella di conversione dell'account — vedi
[`account-e-conversione-symbol.md`](account-e-conversione-symbol.md)).

**Dal masterfilter, non dalla rotazione Titano**, ed è la differenza che conta:
Titano abilita e disabilita strategie ogni periodo, ma il datafeed di uno
strumento serve *sempre* — anche mentre è spento, perché quando torna attivo la
sua storia deve esserci già. Seguendo la rotazione, il feed si interromperebbe a
ogni disabilitazione e lascerebbe un buco lungo esattamente quanto la pausa,
scoperto mesi dopo al primo backtest su quel periodo.

E **non apre sessioni**, a differenza del cBot distribuito che ricava gli
strumenti dal descriptor: un raccoglitore è una lettura pura e non deve avere
alcun effetto sull'operatività.

## Il cBot

`piootoo-repository/ctrader/PiootooDatafeedSyncBot.cs`. Non apre posizioni, non
chiede segnali, non conosce piani né strategie: raccogliere dati e mandare ordini
restano due mestieri separati, così un raccoglitore può girare per giorni su
venti simboli senza toccare niente di operativo. Il simbolo e il timeframe del
grafico a cui è agganciato sono irrilevanti.

Parametri che contano:

- **Codice piano** — se valorizzato, le coppie (simbolo, timeframe) le dichiara
  il piano e **Timeframe in minuti** viene **ignorato**. Il codice piano è
  globale, quindi basta quello: niente workspace, niente account. Su un piano
  reale la risposta è `@NQ → USTEC, [15, 60]` — il bot chiede `USTEC` al broker e
  il server salva `@NQ_15.json`, senza che nessuno mappi niente a mano.
- **Simboli** — `NAS100=@NQ, XAUUSD=@GC`: nome del broker a sinistra, simbolo
  Piootoo a destra. Senza mappatura si usa il nome del broker con `@` davanti.
  **Con il codice piano cambia mestiere: non dichiara, filtra.** Gli strumenti
  restano quelli del masterfilter, con i loro timeframe e il loro nome sul conto,
  e si raccolgono solo quelli elencati; vuoto = tutto il piano. La voce si
  confronta sia con il nome del broker sia con il simbolo Piootoo, con o senza
  `@` — `@NQ`, `NQ` e `USTEC` selezionano lo stesso strumento — così rifare la
  storia di un simbolo solo non richiede di riscrivere a mano la sua mappatura e
  i suoi timeframe (che è il modo in cui le due liste divergevano). Una voce che
  non corrisponde a niente viene segnalata e ignorata; se non ne corrisponde
  nessuna il bot non parte, invece di raccogliere il nulla.
- **Timeframe in minuti** — `15,60,240`. Si espande per prodotto cartesiano con
  i simboli. Oltre i sessanta minuti la serie della piattaforma non si usa: vedi
  "La griglia oltre l'ora".
- **Fuso dell'ancoraggio** (`Europe/Rome`), **Ora di inizio sessione** (`0`) —
  dove cadono i bucket dei timeframe alti. Cambiarli rende il feed non
  confrontabile con `datafeed/`, ed è per questo che finiscono nel `source`.
- **Timeframe base in minuti** (`0` = automatico) — la serie da cui si costruiscono
  quei bucket: automatico prende la più larga che divide il timeframe, cioè l'ora.
  Si forza a `1` solo se si sospetta che siano le barre orarie del broker a essere
  mal allineate, e costa molte più barre da scaricare. Il bot non parte se il fuso
  non si risolve o se il suo scarto da UTC non è un multiplo del timeframe base —
  un fuso a mezz'ora taglierebbe a metà una barra base.
- **Codice broker** — vuoto = dedotto da `Account.BrokerName` ("IC Markets" →
  `ICMARKETS`) e stampato all'avvio. L'override esiste perché il nome dichiarato
  dal broker non è un identificatore stabile: cambia fra demo e reale e fra due
  server dello stesso broker, e se cambia da solo il backfill riparte da zero in
  una cartella nuova senza che niente lo segnali.
- **Data inizio / Data fine** (`yyyy-MM-dd`, UTC; fine inclusa nel giorno) — la
  finestra di *questo* run. È il modo previsto per spezzare un backfill lungo in
  più sessioni corte, un anno per volta: i pezzi non si pestano, perché quello
  che arriva due volte è un duplicato.
- **Giorni per blocco** / **Barre massime per invio** — la dimensione del pezzo.
- **Salta i periodi già presenti sul server** — usa `firstCandleUtc`,
  `lastCandleUtc` e l'elenco dei buchi della status. Se l'elenco era troncato non
  si salta niente: meglio rispedire dati che il server conterà come duplicati,
  che dare per coperto un periodo su un elenco incompleto.

Il ciclo è un timer: **ogni battito fa una cosa sola** — una status, un blocco,
o uno svuotamento del buffer tick — e poi restituisce il thread alla piattaforma.
Si cammina all'indietro dalla fine della finestra verso l'inizio, perché è il
verso in cui il broker consegna la storia. I blocchi già coperti si consumano
invece in serie dentro lo stesso battito (fino a 500): su vent'anni con blocchi
da cinque giorni sarebbero millequattrocento battiti prima di arrivare al primo
dato che manca davvero.

A regime (`Resta in ascolto dopo il backfill`) ogni barra chiusa viene spedita
insieme alle due precedenti: sono già note al server, che le conta come
duplicate, e ricuciono un invio perso senza lasciare un buco permanente.

**L'ultima barra della serie non si spedisce mai**: è quella in formazione, e una
barra a metà salvata nel feed è un dato falso che poi nessuno distingue più da
uno vero.

### Tick

Opzionali, spenti di default. Sono un flusso, non un artefatto: non si compattano
in niente. L'unica proprietà che serve è che due invii sovrapposti non li
duplichino, e per questo il server tiene `lastTickUtc` per simbolo e scarta tutto
ciò che non lo supera. Il buffer del bot si svuota *prima* dell'invio: se la
chiamata fallisce si perdono dei tick, ma tenerli accumulerebbe memoria senza
limite finché il server è giù. Le barre — che sono il dato che conta — non si
perdono mai, perché quelle si rileggono dal broker.

## Scaricare i tick prima di raccoglierli

`piootoo-repository/ctrader/PiootooTickDownloaderBot.cs` fa **una cosa sola**: chiede a cTrader
la storia dei tick, a piccoli passi, finché non copre la finestra di date. Non
parla con il server Piootoo, non salva file, non apre posizioni.

Serve perché cTrader consegna i tick a blocchi e li tiene in una cache locale:
chiederne un anno in una volta è una singola richiesta lunghissima che va in
timeout e che, morendo, non lascia niente. A passi corti — con il thread
dell'algoritmo restituito alla piattaforma fra un passo e l'altro — è più lento
in assoluto ma arriva in fondo. Finito, i tick sono in cache e chi li chiede
dopo li trova già lì; è la preparazione naturale prima di accendere
`Sincronizza i tick` sul raccoglitore.

Stessa tecnica dell'altro bot (finestra di date, passo in giorni, un simbolo per
battito, tetto ai caricamenti per battito), ma senza i parametri che non hanno
senso qui: niente server, niente codice piano, niente codice broker, niente
timeframe — un tick non ne ha uno.

**La RAM è il vincolo vero**: la serie tick resta in memoria per intero mentre la
si carica, e i tick di un simbolo liquido sono milioni al mese. C'è un tetto per
simbolo (`Tick massimi in memoria`, default 20 milioni): raggiunto, quel simbolo
si ferma dicendo fin dove è arrivato, invece di far esaurire la memoria alla
piattaforma e perdere anche ciò che aveva già preso. La finestra si allarga a
tappe.

## Riferimenti codice

- `Piootoo.Core/Services/ExternalDatafeedStore.cs` — journal, compattazione,
  deduplica, buchi, manifest degli orologi, tick.
- `PiootooApp.Server/Controllers/DatafeedExternalController.cs` — gli endpoint.
- `Piootoo.Shared/Models/Datafeed/ExternalDatafeedContracts.cs` — i contratti.
- `Piootoo.Shared/Configuration/PiootooSettings.cs` — `ExternalRepositoryPath`.
- `piootoo-repository/ctrader/PiootooDatafeedSyncBot.cs` — il cBot raccoglitore.
- `piootoo-repository/ctrader/PiootooTickDownloaderBot.cs` — scarica i tick nella
  cache di cTrader, senza inviarli a nessuno.
- `Piootoo.Strategies.Tests/ExternalDatafeedStoreTests.cs` — cucitura,
  deduplica, buchi, separazione per broker.
