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
  i simboli.
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
