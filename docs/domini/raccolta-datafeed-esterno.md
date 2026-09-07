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

**L'unità è il blocco**, non il feed. Piccolo (default due giorni di
calendario, al massimo 5000 barre — a un minuto un giorno sono 1.440 barre), autonomo,
**idempotente**: la chiave di una
barra è il suo istante di apertura, quindi rimandare un periodo non produce
righe in più. Da qui discende tutto il resto — si può completare un feed in
cento invii, in ordine qualsiasi, su più sessioni, riprendendo dopo un crash.

```
cBot                                    server
 |  GET  status?broker&symbol&tf    -->  cosa ho già: primo/ultimo, buchi
 |  POST bars   (blocco 1)          -->  journal .jsonl (append, no fsync)
 |  POST bars   (blocco 2)          -->  journal
 |  ...                                  (a soglia: compattazione)
 |  POST compact                    -->  @NQ_1.json (scrittura atomica durabile)
 |  POST rebuild-from-minutes       -->  @NQ_15.json, @NQ_60.json, @NQ_240.json (derivati)
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

## Il bot raccoglie solo il minuto (dalla 2.0.0)

Il raccoglitore chiede alla piattaforma la serie da **un minuto**, la spedisce così com'è, e
non costruisce più nessun timeframe. Tutto ciò che sta sopra lo deriva il server dal minuto,
con il layer (`Piootoo.Shared/MarketData/BarAggregator`), sulla griglia che il calendario di
mercato dichiara per quel simbolo.

**Il minuto è il solo dato su cui non c'è niente da decidere**: nessun ancoraggio, nessun fuso,
nessuna convenzione sul cambio d'ora. È quindi il solo che un bot possa raccogliere senza
poter sbagliare.

### Perché è cambiato

La griglia oltre l'ora dipende dall'ora di inizio sessione dello strumento — la tabella §2.4
del dossier — che è un dato del calendario di mercato: una tabella che il server ha e che
cTrader non vede. Il bot ne teneva una **copia**, e il commento nel sorgente lo ammetteva già:
*«le copie restano identiche o la prossima lettura non sa più quale sia quella giusta»*.

Quando hanno smesso di essere d'accordo, il risultato non è stato un errore. La deduplica è
sull'istante di apertura della barra, quindi due raccolte con ancoraggi diversi producono
etichette diverse per lo stesso periodo e **non si sovrascrivono: si sommano**. Sull'archivio
FTMOPLATFORM erano `@KC_240` (880 barre fuori griglia su 1.760), `@KC_1440` e `@CT_1440` (295
su 590): metà file su una griglia che nessuna strategia ha mai visto, e indistinguibile
guardandolo. Vedi [`layer-barre-e-calendario.md`](layer-barre-e-calendario.md) §7bis.

### Cosa è sparito dal bot

I tre parametri della griglia — *Fuso dell'ancoraggio*, *Ora di inizio sessione*, *Timeframe
base in minuti* — e il codice che li usava: `BucketStartUtc`, `SessionLocalToUtc`,
`SessionStartHourOf`, `TryValidateSessionOffsets`, `TryResolveBase`, `FoldBackwards`. Con
loro sparisce anche il tetto dei sessanta minuti: se l'unico ingresso è il minuto, non c'è
nessuna serie della piattaforma di cui fidarsi a nessun timeframe.

*Timeframe in minuti* resta come parametro ma **cambia mestiere**: non dice più cosa
raccogliere, dice cosa far **derivare al server** a fine backfill. Con un codice piano viene
ignorato, perché quei timeframe li dichiara il masterfilter — la stessa fonte da cui vengono
gli strumenti.

### Gli aggregati li chiede il bot a fine backfill

`POST api/datafeed-external/rebuild-from-minutes`, automatico salvo spegnere il parametro. Non
è una comodità: senza, una raccolta su un archivio nuovo lascerebbe **solo** il minuto, e ogni
backtest a 15, 60 o 240 minuti troverebbe il datafeed mancante senza che nulla spieghi il
perché. Un fallimento della derivazione non è fatale — il minuto, che è il dato che conta, è
già salvato — ma viene detto a log, e la si può rifare a mano con la stessa chiamata.

### Due conseguenze pratiche

**La tolleranza sui buchi va dichiarata.** Il default del server è due volte il passo
dominante, cioè due minuti: su una serie a un minuto vera *ogni notte è un buco* — la pausa di
manutenzione CME, la chiusura serale degli europei, i festivi. Un anno ne produce centinaia,
l'elenco viene troncato a duecento, e un elenco troncato fa dire a `IsAlreadyCovered` «non so»
per ogni blocco: *Salta i periodi già presenti* non salterebbe mai niente e ogni run
rispedirebbe milioni di barre. Il bot chiede quindi la status con una tolleranza di **quattro
giorni** (parametro *Tolleranza buchi*), che copre un fine settimana lungo con un festivo
attaccato.

**La RAM è il vincolo vero.** La serie resta in memoria per intero mentre si cammina
all'indietro: un anno sono circa 370.000 barre per simbolo, contro le 1.500 di una 240. Il bot
stampa la stima all'avvio. Su molti simboli conviene spezzare per finestre di date — a cosa
servono *Data inizio* e *Data fine* — invece di chiedere tutta la storia in un run solo.

### I cBot operativi piegano ancora i bucket — e vanno rifatti

Il raccoglitore è cambiato, i due bot che eseguono no: `PiootooDirectExecutionBot` e
`PiootooDistributedExecutionBot` sottoscrivono ancora una serie base e costruiscono i bucket
con una copia del vecchio codice, ancoraggio compreso. **Restano quindi due delle quattro
copie della tabella**, ed è lavoro aperto (passo 6 del refactor, parte operativa).

Perché per loro conta: in sessione `ExternalBroker` il server non aggrega niente —
`TradingSessionService.PushBars` valida, deduplica sulla chiave di idempotenza, **accoda** a
`session.History[(simbolo, timeframe)]` e valuta. Non c'è ricampionamento da nessuna parte, e
le barre su cui girano le strategie in live sono *esattamente* quelle che il cBot spinge. Se il
cBot prende l'H4 della piattaforma, l'esecuzione gira su candele che il backtest non ha mai
visto e i numeri non lo dicono.

Due conseguenze che oggi vivono nei bot operativi e che, quando l'aggregazione passerà al
server, diventeranno proprietà del server:

**L'orologio a barre non è `Series.Count`.** La scadenza di un ordine "next bar" e
`MaxBarsInPosition` sono espressi in barre *della strategia*: contarli sulla serie base farebbe
scadere l'ordine di una 240 dopo un'ora e chiuderebbe la posizione quattro volte troppo presto.
Ogni stream tiene il proprio conteggio di bucket chiusi, e la marcatura del pending si scrive e
si confronta su quello.

**La barra base non è la barra della strategia.** Il battito della serie arriva quattro volte
per candela di una 240: la passata di barra — ritiro dell'ordine scaduto, push, claim del
segnale — si fa solo quando il bucket si chiude davvero. Break-even, trailing e uscite a tempo
girano già a ogni tick e restano tempestive.

E poiché più stream possono pendere dalla **stessa** serie base, il match fra evento e stream
non può fermarsi al primo che corrisponde.

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
| `POST /rebuild-from-minutes[?broker&symbol&timeframeMinutes]` | Riscrive un aggregato dalle barre da un minuto dello stesso stream, sulla griglia dichiarata dal calendario del simbolo. Serve quando un file è nato sull'ancoraggio sbagliato — o su **due** ancoraggi insieme. Vedi sotto. |

### Quando un aggregato nasce su due griglie

La chiave di deduplica è l'istante di apertura della barra. Due raccolte con
ancoraggi diversi producono etichette diverse per lo stesso periodo, quindi **non
si sovrascrivono: si sommano**. Il file che ne esce ha il doppio delle barre, tutte
plausibili, metà su una griglia che nessuna strategia ha mai visto — e guardando il
file non si distinguono.

È successo: il 07/09/2026 `@KC_240.json` di FTMOPLATFORM conteneva 1.760 barre,
l'unione *esatta* dell'ancoraggio 00:00 e di quello 01:00, 880 ciascuno e zero
sovrapposizioni; `@KC_1440` e `@CT_1440` 295 su 590. Raccolte fatte prima che il bot
imparasse la tabella §2.4.

`POST /rebuild-from-minutes` ripara riscrivendo dal minuto, che è il dato
autorevole. Scarta i bucket incompleti — il primo, troncato dall'inizio del journal,
e l'ultimo, in formazione — e **cancella il journal del bersaglio**, che altrimenti
alla prima compattazione rifonderebbe dentro i blocchi sulla vecchia griglia. Il
campo `source` del file dichiara la derivazione.

Il guardiano è `BarAggregatorMetroTests.CollectedAggregatesSitOnTheirOwnGrid`:
verifica che l'etichetta di ogni barra coincida con l'inizio del bucket che la
contiene, e nomina i file che non lo rispettano.

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

Dalla **2.0.0** raccoglie soltanto barre da un minuto. Il salto di major è dovuto: un archivio
raccolto con la 1.x contiene aggregati costruiti dal bot, uno raccolto con la 2.x contiene il
minuto più aggregati derivati dal server, e i due non sono la stessa cosa. Il campo `source`
del feed è il solo modo di distinguerli a posteriori.

Parametri che contano:

- **Codice piano** — se valorizzato, gli strumenti li dichiara il piano. Il codice piano è
  globale, quindi basta quello: niente workspace, niente account. Su un piano reale la
  risposta è `@NQ → USTEC` — il bot chiede `USTEC` al broker e il server salva `@NQ_1.json`,
  senza che nessuno mappi niente a mano. I timeframe del piano non servono alla raccolta ma
  alla **derivazione**: il bot li passa al server a fine backfill.
- **Simboli** — `NAS100=@NQ, XAUUSD=@GC`: nome del broker a sinistra, simbolo
  Piootoo a destra. Senza mappatura si usa il nome del broker con `@` davanti.
  **Con il codice piano cambia mestiere: non dichiara, filtra.** Gli strumenti
  restano quelli del masterfilter, con il loro nome sul conto, e si raccolgono solo quelli
  elencati; vuoto = tutto il piano. La voce si
  confronta sia con il nome del broker sia con il simbolo Piootoo, con o senza
  `@` — `@NQ`, `NQ` e `USTEC` selezionano lo stesso strumento — così rifare la
  storia di un simbolo solo non richiede di riscrivere a mano la sua mappatura. Una voce che
  non corrisponde a niente viene segnalata e ignorata; se non ne corrisponde
  nessuna il bot non parte, invece di raccogliere il nulla.
- **Timeframe da far derivare al server** — `15,60,240`. Non è cosa raccogliere: si raccoglie
  il minuto e basta. È cosa il server deve derivarne a fine backfill. Con un codice piano
  viene **ignorato**, perché quei timeframe li dichiara il masterfilter — l'unica fonte di
  verità su quali servono. Vuoto e senza piano: si rifanno gli aggregati già presenti.
- **Fai derivare gli aggregati a fine backfill** (acceso) — la chiamata a
  `rebuild-from-minutes`. Spegnendolo, sul disco resta il solo minuto.
- **Tolleranza buchi in minuti** (`0` = quattro giorni) — da quanto in su un vuoto fra due
  barre è un buco invece che mercato chiuso. A un minuto il default del server (due minuti)
  renderebbe ogni notte un buco. Vedi "Due conseguenze pratiche".
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
invece in serie dentro lo stesso battito (fino a 500): su una finestra lunga con blocchi da
due giorni sarebbero migliaia di battiti prima di arrivare al primo dato che manca davvero.

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
- `piootoo-repository/ctrader/syntax-check/` — compila i sorgenti dei cBot contro uno stub
  minimo dell'API cAlgo: verifica sintassi e tipi fuori da cTrader, non il comportamento.
- `Piootoo.Shared/MarketData/BarAggregator.cs` — la derivazione dal minuto, lato server.
- `piootoo-repository/ctrader/PiootooTickDownloaderBot.cs` — scarica i tick nella
  cache di cTrader, senza inviarli a nessuno.
- `Piootoo.Strategies.Tests/ExternalDatafeedStoreTests.cs` — cucitura,
  deduplica, buchi, separazione per broker.
