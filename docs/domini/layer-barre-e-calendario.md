# Il layer delle barre e il calendario di mercato

> **Stato: migrazione in corso.** Il §7 è il piano e dice, passo per passo, cosa è
> già fatto e cosa no. Al 07/09/2026 sono chiusi il **passo 0** (il metro),
> il **1** (il calendario come dato), il **2** (il layer), il **3** (il backtest interno), il **4a**
> (il calendario governa la sessione) e la parte *raccoglitore* del **6**; restano
> il 4b, i passi 5, i due cBot operativi, il 7 e l'8. Fuori da lì il codice descrive ancora il modello vecchio: in caso di
> contraddizione ha ragione il codice, e questo file dice dove si sta andando.
> Origine della richiesta:
> `piootoo-repository/timeframe-analisys/timeframe-refactor.txt`.

---

## 1. Il problema: il calendario è scritto in sette posti

Oggi la domanda «a quale sessione appartiene questa barra, e da dove comincia il
suo bucket» ha **sette** risposte indipendenti nel repository. Nessuna è
sbagliata da sola; tutte sono libere di divergere, e quando divergono non
producono un errore — producono barre *diverse*, tutte plausibili.

| Dove | Cosa decide | Come |
|---|---|---|
| `aggregate_flat_feed.py` (`minutes_into_bucket`, `SESSION_START_HOUR`) | i bucket del feed del vendor | tabella hardcoded a cinque simboli, `floor` su etichette di **chiusura** |
| `PiootooDatafeedSyncBot.BucketStartUtc` + `SessionStartHourOf` | i bucket del feed raccolto | copia della stessa tabella, su etichette di **apertura** |
| `PiootooDirectExecutionBot` | i bucket dell'esecuzione diretta | terza copia |
| `PiootooDistributedExecutionBot` | i bucket dell'esecuzione distribuita | quarta copia |
| `EasyLib.ClassifySessionBar` + `FullDaySessionDay` | il confine di `d0..d5`, il secchio degli ingressi, l'uscita di fine sessione | ricalcolato dai timestamp a ogni valutazione |
| `EasyEngineBase.ResolveEntrySessionStartUtc` / `SessionBarToUtc` / `ResolveCloseAtUtc`, più le copie in `PriceChannelEngine`, `BiasWeeklyEngine`, `ReversalBollingerBandEngines` | l'inizio della sessione di appartenenza di un segnale | aritmetica a **giorni di calendario** (`AddDays(±1)`) |
| `InstrumentRegistry` (`SessionTimeZone`, `ResearchSessionStartHour`, `SessionDays`) | il fuso e i giorni di sessione | tabella C# hardcoded |

Il commento su `SessionStartHourOf` nel cBot lo dice già senza giri di parole:
«È una copia della tabella — il bot gira dentro cTrader e non vede
`InstrumentRegistry` — e vale la stessa regola: le copie restano identiche o la
prossima lettura non sa più quale sia quella giusta.» Chiedere alle copie di
restare d'accordo non è una proprietà che si ottiene chiedendola.

### 1.1 I due difetti che ne discendono

**L'aritmetica a giorni di calendario non conosce i buchi.**
`ResolveEntrySessionStartUtc` risolve «l'inizio della sessione che contiene
questo istante» come «lo stesso orario di ieri». Su un lunedì mattina *ieri* è
domenica, e su un mercato che la domenica non apre quella sessione non esiste.
`SessionBarToUtc` documenta il difetto gemello e lo dichiara voluto: «l'originale
EasyLanguage conta *barre*, non orologio […] su una sessione con barre mancanti
la chiusura cade sull'orario atteso, non sulla N-esima barra ricevuta».

**L'aggregazione è scritta due volte in due linguaggi.** La formula di
`aggregate_flat_feed.py` lavora su etichette di chiusura (`bin = (minuti − 1) /
240`), quella dei cBot su etichette di apertura. Il commento in `BucketStartUtc`
dimostra che le due sono la stessa cosa scritta diversamente — ed è esattamente
il tipo di dimostrazione che va rifatta a mano ogni volta che una delle due
cambia. Peggio: il Python prende la scorciatoia `openUtc − resto`, che sul salto
dell'ora legale scavalca l'ora che non esiste, e il cBot no. Due implementazioni,
due comportamenti diversi su quattro giorni all'anno.

---

## 2. Il modello

Un **unico layer puro**, condiviso da backtest interno, backtest su feed di
broker e sessione realtime. Non tre percorsi che si somigliano: lo stesso tipo,
la stessa funzione, lo stesso file di configurazione.

```
                       ┌──────────────────────────────┐
   datafeed/@SYM_1     │                              │
   (vendor, 1m UTC)  ──┤                              │
                       │        MarketDataLayer       │
   datafeed-external/  │                              │   ┌─ decision feed ─→ Evaluate
   {BROKER}/@SYM_1   ──┤  calendario → classify →     ├───┤
   (broker, 1m UTC)    │  mask → aggregate → emit     │   └─ risk feed ─────→ UpdateMarketPrices
                       │                              │
   cBot live (1m UTC) ─┤                              │
                       └──────────────────────────────┘
```

**Il 1 minuto UTC è l'unico dato che entra.** Tutto il resto è derivato, e la
derivazione avviene in un punto solo, in C#, lato server.

### 2.1 Il calendario è un dato, non codice

Una spec versionata per simbolo, in JSON. Il formato supporta fin da subito fasi,
festivi ed early close — perché aggiungerli dopo vorrebbe dire rileggere ogni
consumatore — ma si popola con ciò che oggi il sistema sa davvero e usa. Un campo
che non abbiamo resta **dichiarato e vuoto**: inventare un calendario festivi non
verificato è precisamente l'errore che `InstrumentRegistry` esiste per impedire.

```jsonc
{
  "specVersion": "2026-09-07.1",
  "symbols": {
    "FDAX": {
      "exchangeTz": "Europe/Berlin",       // dove tornano gli orari delle sorgenti EasyLanguage
      "researchTz": "Europe/Rome",         // l'orologio in cui la ricerca ha scritto le finestre
      "sessionStartHour": 1,               // ancoraggio della sessione, in researchTz
      "sessionDays": ["Mon","Tue","Wed","Thu","Fri"],
      "phases": [                          // dichiarate, oggi vuote per tutti
        // {"name":"asian","start":"00:15","startAnchor":"utc","end":"08:00","endAnchor":"local"}
      ],
      "holidays": [],                      // da popolare quando esiste una fonte verificata
      "earlyClose": {}
    }
  }
}
```

Tre punti su cui il formato è deliberatamente più ricco dei dati che abbiamo:

- **`startAnchor` per fase.** La trappola che la spec di origine segnala per prima:
  sul FDAX l'apertura notturna è agganciata a Singapore ed è fissa a 00:15 UTC,
  mentre chiusura e cash seguono Berlino. Modellare tutto in locale sbaglia
  l'apertura per metà anno; modellare tutto in UTC sbaglia tutto il resto. Ogni
  fase dichiara quindi il proprio riferimento.
- **`exchangeTz` è separato da `researchTz`.** Non è ridondanza: sono due orologi
  diversi e il sistema lo sa già (`ZonedWindow.CmeChicago` contro
  `ZonedWindow.ResearchTimeZone`). Il primo è quello in cui tornano i numeri delle
  sorgenti EasyLanguage, il secondo quello in cui i run Python hanno scritto
  `start_hour`/`end_hour` — **per ogni simbolo**, CET.
- **`sessionDays` è già in produzione** e non va perso: è ciò che impedisce a un
  feed CFD di fabbricare la sessione domenicale che il future non ha. Il dossier
  la misura sul DAX all'11% del P&L. E non è «togliere la domenica»: su CME la
  domenica sera è una sessione vera nelle settimane in cui l'ora legale europea e
  americana sono sfasate.

**Dove vive il file.** Risorsa incorporata in `Piootoo.Shared`, così test, server
e client hanno sempre un calendario senza dipendere da un percorso su disco; con
override facoltativo in `piootoo-repository/settings/market-calendars.json`, che
deve dichiarare la stessa `specVersion` o l'avvio fallisce. Un simbolo assente è
un errore esplicito, come oggi per `PointValue`.

`InstrumentRegistry` **non sparisce**: resta la sorgente della parte economica
(`PointValue`, `TickSize`, `Currency`), che con il calendario non c'entra. Cede
al calendario `SessionTimeZone`, `ResearchSessionStartHour` e `SessionDays`, e per
un periodo continua a esporne i getter inoltrando al layer, così le 124 classi
`PTS_*` non vanno toccate tutte insieme.

### 2.2 La pipeline, a stadi ispezionabili

```
raw 1m UTC (immutabile)
  → validate    dedup, monotonicità, OHLC coerente, rilevamento buchi
  → classify    ogni barra riceve sessionId, phase, indice, flag
  → mask        qualità: spread, tick count, staleness — su finestra rolling
  → aggregate   resample ancorato agli eventi di sessione del calendario
  → emit        BarContext alla strategia
```

**`mask` e `filter` non cancellano niente dal raw.** Il 1m resta immutabile su
disco, la normalizzazione è una funzione pura e versionata: si rigenera tutto
quando si scopre un errore nella spec, e si fa audit su un trade dubbio. Le barre
spazzatura si **marcano**, non si eliminano: la strategia decide se ignorarle,
l'analisi vuole vederle.

**Zero look-ahead nel classificatore.** Fase e flag di una barra dipendono solo da
informazioni disponibili alla sua chiusura. Un `qualityFlag` calcolato sui
quantili dell'intero dataset è look-ahead: va calcolato su finestra rolling. È la
stessa disciplina che il sistema già applica altrove — vedi
[`orologio-barre-e-fill.md`](orologio-barre-e-fill.md).

### 2.3 Le convenzioni sui timestamp, scritte una volta

Sono già quelle del sistema; il layer le rende non aggirabili.

- Barra etichettata sull'**apertura**, intervallo semiaperto `[t, t+Δ)`.
- Tutto UTC. Nessun `DateTime` naive, nessuna conversione a locale se non per il
  display.
- Ogni barra porta un **`SessionId` esplicito** — la data della *sessione*, non
  del calendario. Su FDAX quasi sempre coincidono; su NQ la sessione comincia il
  giorno prima, e senza `SessionId` ogni logica giornaliera si rompe in silenzio.
- Il confine si calcola **sull'orologio locale e si riconverte**, mai sottraendo
  il resto all'istante UTC: la scorciatoia scavalca l'ora inesistente del salto in
  avanti e produce due barre che si accavallano. Misurato su due anni di barre
  orarie `Europe/Rome`: coincide su 34.988 righe su 35.088 e diverge sui quattro
  giorni di transizione. Le convenzioni sui due giorni all'anno restano quelle di
  `SessionClock.ToUtc` — avanti dell'ampiezza del salto, e prima delle due
  occorrenze.

### 2.4 Aggregazione ancorata

Il resample non è mai al default di mezzanotte: è ancorato a `sessionStartHour`
nel fuso dichiarato. Ogni barra aggregata porta due flag:

- **`Complete`** — ha visto tutti i minuti attesi per quel bucket;
- **`SpansBoundary`** — attraversa un confine di sessione.

Una 4h a cavallo del gap notturno non arriva alla strategia senza essere
segnalata. Oggi non è distinguibile da una piena.

Il tetto dei 60 minuti che i cBot applicano oggi (*«fino all'ora la serie della
piattaforma va bene com'è»*) **sparisce**: se l'unico ingresso è il minuto, non
c'è nessuna serie della piattaforma di cui fidarsi a nessun timeframe.

### 2.5 Cosa arriva alla strategia

Non la barra: la barra **più il contesto già calcolato**.

```csharp
public readonly record struct BarContext(
    OhlcvData Bar,
    DateTime SessionId,              // la data della sessione, non del calendario
    string? Phase,                   // null finché le fasi non sono popolate
    int BarIndexInSession,           // 1-based
    DateTime SessionOpenUtc,
    DateTime? PreviousSessionCloseUtc,
    int MinutesSinceSessionOpen,
    bool IsFirstBarAfterGap,
    bool Complete,
    bool SpansBoundary,
    BarQualityFlags Quality);
```

**La strategia consuma, non ricalcola.** È il punto che chiude il difetto §1.1:
se la strategia ricostruisce la sessione dai timestamp, il calendario è duplicato
in n posti e prima o poi divergono — che è la situazione attuale, misurata.

### 2.6 Due canali distinti: decision feed e risk feed

Sono già due percorsi nel motore (`currentBars` contro `currentPrices` nel loop di
backtest) ma non sono nominati, e nulla impedisce di confonderli.

- **Decision feed** — aggregato, mascherato, emula il future: alimenta
  `Evaluate` e i livelli operativi. La strategia non deve vedere le ore morte.
- **Risk feed** — il 1m grezzo, sempre attivo, non si ferma mai: alimenta il
  mark-to-market, gli stop e le uscite a tempo. Il risk manager deve vedere
  *tutto*, comprese le ore che il decision feed scarta.

Nominarli separatamente è ciò che rende impossibile la classe di errore già
documentata in [`orologio-barre-e-fill.md`](orologio-barre-e-fill.md): usare per
riempire un ordine un prezzo che serviva solo al mark.

---

## 3. L'orologio a barre e la logica future sul fine settimana

È il punto della richiesta: *«le strategie di chiusura si dovranno basare sul
numero di barre, che in caso di weekend vengono avvicinate sempre con logica
future»*.

**La regola.** `MaxBarsInPosition`, la scadenza di un pending e ogni conteggio
`TwBars` si misurano sulle barre che il **calendario prevede**, non sui tick
dell'orologio sintetico e non sulle barre che il feed ha consegnato.

Ne discende esattamente il comportamento chiesto:

- fra la chiusura del venerdì e la riapertura prevista dal calendario **non passa
  nessuna barra**: il contatore non avanza, la posizione arriva al lunedì con le
  stesse N barre residue. È la logica del future, dove il weekend semplicemente
  non esiste sul grafico;
- un CFD che quota la domenica **non fa avanzare il contatore** dove
  `sessionDays` dice che il future non ha sessione: quelle barre esistono nel raw,
  sono marcate, e non contano;
- dove invece la sessione domenicale è vera — CME nelle settimane sfasate — conta,
  perché è il calendario a dirlo e non un `if` sul giorno.

Oggi la stessa domanda ha tre risposte: `PiootooTradingService` conta le barre
consegnate e salta i tick a vuoto; `ScaleSignalMaxBarsInPosition` moltiplica per
il rapporto dei timeframe *prima* di sapere quante barre esisteranno davvero; i
cBot contano i bucket chiusi. Con il calendario le tre diventano una, e
`ScaleSignalMaxBarsInPosition` sparisce: non serve più convertire barre della
strategia in tick dell'orologio, perché il conteggio non passa più dall'orologio.

**Effetto atteso, da misurare e non da assumere.** Le posizioni multiday
sopravvivono più a lungo di oggi attraverso i weekend e i festivi. È un
cambiamento di risultato, non solo di forma: i backtest archiviati prima della
migrazione non sono confrontabili con quelli dopo, e va scritto nel summary.

---

## 4. Cosa cambia nei cBot

**Il cBot non sa più cos'è un fuso orario.** Sparisce da tutti e tre:
`BucketStartUtc`, `SessionLocalToUtc`, `SessionStartHourOf`,
`TryValidateSessionOffsets`, `FoldBackwards`, e i parametri *Fuso
dell'ancoraggio*, *Ora di inizio sessione*, *Timeframe base in minuti*.

| Bot | Prima | Dopo |
|---|---|---|
| `PiootooDatafeedSyncBot` | sottoscrive fino a 60m, piega i bucket, spedisce `@SYM_{tf}` | sottoscrive **solo 1m**, spedisce `@SYM_1` e basta |
| `PiootooDirectExecutionBot` | idem, più il conteggio dei bucket per l'orologio a barre | manda 1m, riceve dal server la barra aggregata e il suo indice di sessione |
| `PiootooDistributedExecutionBot` | idem | idem |

I due bot operativi restano responsabili di **eseguire**, non di interpretare: SL
e TP come livelli nativi del broker, il flat di sicurezza di fine settimana come
regola locale che deve tenere a server muto. Ma *quando* una barra è chiusa,
*a quale sessione* appartiene e *quante barre* mancano a `MaxBarsInPosition` lo
dice il server.

Questo chiude anche l'invariante di CLAUDE.md «l'orologio a barre si conta sui
bucket, non su `Series.Count`»: con il bot che non costruisce più bucket, la
regola non è più imponibile al bot — diventa una proprietà del server, dove esiste
un solo contatore.

**Costo da mettere in conto.** Un anno di 1m per simbolo sono circa 370.000 barre
contro 1.500 a 240 minuti. Sul backfill non cambia niente — è già a blocchi
idempotenti, e i file `@SYM_1.json` da 70–100 MB esistono già per 14 simboli su
`FTMOPLATFORM`. A regime cambia la cadenza: un invio al minuto invece che uno per
bucket. È la ragione per cui il riscaldamento non può più passare dal client (§5).

---

## 5. Il riscaldamento nel nuovo modello

Il problema è aritmetico: `PTS_NQ_VBO_002_240` chiede 606 barre a 240 minuti,
cioè **145.440 barre da un minuto**. Spedirle dal client all'avvio ricrea
esattamente il problema che il journal a blocchi del raccoglitore ha già risolto.

**Modello ibrido: disco per la storia, client per la coda.**

1. All'apertura della sessione il server legge
   `datafeed-external/{BROKER}/@SYM_1.json`, aggrega col layer e riempie la storia
   di ogni stream fino a `RequiredCandles`.
2. Il client manda **solo il 1m da lì in avanti**: la coda che il disco non ha.
3. Se il feed su disco manca, o è vecchio oltre una soglia dichiarata, l'apertura
   della sessione **lo dice esplicitamente** invece di partire muta. È lo stesso
   invariante di «datafeed mancante = errore esplicito», applicato al
   riscaldamento — e chiude la classe di errore descritta in
   [`finestra-candele-e-riscaldamento.md`](finestra-candele-e-riscaldamento.md) §1,
   dove il server scartava in silenzio le prime 576 barre di ogni run.

Ne discende una dipendenza operativa nuova e va detta chiaramente: **il bot
raccoglitore diventa un prerequisito della sessione**, non più un accessorio
indipendente. In cambio, R2 e R3 di quel documento — il client che carica la
storia dal broker con `LoadMoreHistory` e la spedisce in due tempi — spariscono
del tutto.

**Il riscaldamento viene da dati filtrati.** È la terza trappola della spec di
origine: se si inizializzano le medie con lo storico grezzo e poi si gira sul
filtrato, i primi giorni di live divergono dal backtest senza motivo apparente.
Disco e stream passano dallo stesso `BarAggregator` e dallo stesso mask, quindi la
proprietà è per costruzione e non per disciplina.

---

## 6. Gli aggregati diventano una cache

Scelta: il **1m è l'unico artefatto autorevole**; `@SYM_{tf}.json` per `tf > 1` è
cache derivata, rigenerabile.

Ogni file di cache dichiara in testa la `specVersion` del calendario che l'ha
prodotto e l'ancoraggio usato. Il layer lo usa se la versione combacia, altrimenti
lo rigenera dal 1m. È la forma corretta del campo `source` che il feed raccolto già
porta (`... griglia(60m->240m, Europe/Rome 00:00)`): la stessa informazione, resa
verificabile dal codice invece che leggibile da un umano.

**Conseguenza su `aggregate_flat_feed.py`.** Smette di essere una seconda
implementazione della regola dei bucket e diventa un convertitore: dai CSV minute
del vendor a `datafeed/@SYM_1.json`, in UTC vero, con le due convenzioni del
sorgente che restano sue (l'ora CET e il timestamp a fine minuto). Tutto ciò che
sta sopra il minuto lo produce il layer C#. `SESSION_START_HOUR` sparisce dal
Python.

**Costo da mettere in conto.** Il 1m del vendor per `@NQ` sono circa 7 milioni di
righe, ~1 GB in JSON. Il colpo si paga una volta alla rigenerazione della cache,
non a ogni run. Se diventasse un problema, la risposta è un formato compatto per
il 1m — non un secondo aggregatore.

---

## 7. Piano di migrazione

Ogni passo è verificabile da solo e nessuno rompe il precedente. La parità si
**misura**, non si assume.

**Passo 0 — il metro. Fatto, e il risultato è quello sperato.**
`piootoo-repository/timeframe-analisys/metro_aggregazione.py` rigenera un
aggregato dal 1m con la regola del layer e lo confronta barra per barra con il
file che il cBot ha già prodotto. Su `FTMOPLATFORM`, dove il 1m e i suoi aggregati
coesistono per 14 simboli:

| stream | bucket in comune | OHLC diversi | dove |
|---|---|---|---|
| `@FDAX_240` (ancoraggio 01:00) | 1.847 | 2 | primo e ultimo |
| `@NQ_240` (ancoraggio 00:00) | 1.816 | 2 | primo e ultimo |
| `@NQ_15` | 27.752 | 1 | ultimo |

**Tutte le differenze stanno ai bordi**, e sono spiegate: il primo bucket è
troncato perché il journal 1m comincia più tardi del 60m da cui l'aggregato è
nato, e l'ultimo è ancora in formazione. Nel mezzo la corrispondenza è **esatta**
su OHLC e volume, per entrambi gli ancoraggi e sopra e sotto l'ora.

È la prova che serviva prima di toccare qualunque altra cosa: aggregare dal minuto
in C# riproduce la griglia su cui le strategie sono state trovate, quindi il
refactor sposta *dove* si aggrega senza spostare *cosa* esce. Il metro resta come
test di regressione dei passi 2 e 8.

**Passo 1 — il calendario come dato. Fatto il 07/09/2026.**
`Piootoo.Shared/MarketData/market-calendars.json` (risorsa incorporata, spec
`2026-09-07.1`, 30 simboli) è ora la sorgente di fuso di borsa, fuso della
ricerca, ora di inizio sessione e giorni di sessione. `InstrumentRegistry` tiene
la sola parte economica — `PointValue`, `Currency`, `TickSize` — e unisce le due
in `BuildSpecs`: un contratto senza calendario è un errore esplicito, non un
default.

Il formato porta già `phases`, `holidays` ed `earlyClose`, **dichiarati e vuoti**
per tutti. `sessionDays` distingue l'assenza dalla lista vuota: assente è "non
dichiarato", e un array vuoto viene *rifiutato* dal lettore, perché uno strumento
senza alcun giorno di sessione non esiste e trattare l'assenza come vuoto lo
spegnerebbe invece di lasciarlo passare.

Nessun cambiamento di comportamento, e la prova sta in
`MarketCalendarConformanceTests`: la tabella com'era prima del file è trascritta a
mano nel test come copia indipendente, e vale come attesa per tutti e 30 i
simboli. La suite passa da 954 a 1.032 test; i **43 rossi preesistenti restano
esattamente gli stessi 41 gruppi**, verificati confrontando l'elenco dei falliti
prima e dopo la modifica.

L'override su disco è collegato all'avvio del server (`Program.cs`, prima di
qualunque servizio) e stampa quale calendario è in vigore:
`[Piootoo] Calendario di mercato: incorporato, spec 2026-09-07.1, 30 simboli`.
Collegarlo era necessario, non un extra: un file di override che non venisse letto
sarebbe una configurazione che non fa nulla in silenzio.

**Passo 2 — classificatore e aggregatore. Fatto il 07/09/2026.**
`SessionGrid`, `BarAggregator`, `SessionSegmenter` e `BarContext` in
`Piootoo.Shared/MarketData/`. Il metro è portato in C#
(`BarAggregatorMetroTests`) e gira su `@CC` a 240/60/15, `@CT`, `@SB` e `@FDAX`:
nell'interno la corrispondenza con gli aggregati raccolti è **esatta** su OHLC e
volume. Comportamento del sistema invariato — nessun consumatore ci passa ancora —
e i 43 rossi preesistenti restano gli stessi, su una suite che va da 1.032 a 1.087
test.

Due correzioni a quanto questo documento diceva prima, entrambe emerse misurando.

**Sotto l'ora il bucket si calcola in UTC, non in ora locale.** §2.3 prescriveva il
giro attraverso l'orologio locale per tutti i timeframe. È giusto solo sopra
l'ora: per un timeframe che divide l'ora le due griglie sono lo stesso insieme di
confini — i fusi del calendario hanno scarti a ore piene — tranne nell'ora che
esiste **due volte** al ritorno dell'ora solare, dove il giro locale manda
entrambe le occorrenze sulla stessa etichetta e le **fonde in una barra sola**.
Sui future non si vede, perché il cambio d'ora cade di domenica a mercato chiuso;
su BTC, che quota 24/7, sono le barre del 26/10/2025 dall'01:00 all'01:45 UTC —
quattro sul 15 minuti, due sul 30, una sull'ora. È anche il comportamento che i
file raccolti già hanno, perché fino a sessanta minuti il cBot spedisce la serie
nativa senza piegarla. `SessionGrid` impone inoltre che il fuso di ancoraggio
abbia scarti a ore piene, che è ciò che rende la regola dimostrabile invece che
sperata.

**`SpansBoundary` non esiste; al suo posto `IsSessionDay` e `MinuteCount`.**
§2.5 lo prevedeva, ma finché i timeframe devono dividere il giorno e le fasi sono
vuote un bucket non può attraversare un confine di sessione: il campo sarebbe
sempre falso, e un flag costante è peggio di nessun flag. Al suo posto ci sono i
due che portano informazione vera: `MinuteCount` — quanti minuti hanno composto la
barra, che è come si vede una 4h contenente mezz'ora di nulla — e `IsSessionDay`,
cioè se il calendario dichiara che il simbolo ha una sessione quel giorno. Il
secondo è il campo che abilita la logica future del §3.

`Complete` ha una definizione precisa e ristretta: l'input copriva **tutto l'arco**
del bucket. Un buco interno — il mercato chiuso — **non** rende incompleto un
bucket, perché il mercato chiuso non è storia mancante. Restano incompleti solo i
due bordi: il primo bucket, troncato dall'inizio del feed, e l'ultimo, in
formazione.

**Passo 3 — il backtest interno passa dal layer. Fatto il 07/09/2026.** Al
prefill ogni serie caricata attraversa `SessionSegmenter` e produce un
`FeedCalendarReport`, che finisce nel `backtest-summary.json` sotto
`dataSources[].calendar` e, quando trova qualcosa, fra i `diagnostics`.

**Descrive, non decide.** Il run non si ferma. È deliberato: anche il feed del
vendor ha barre fuori griglia (§7ter), e trasformare il controllo in un blocco
fermerebbe run che oggi girano. Il blocco si mette quando i feed saranno
rigenerati dal minuto, non prima.

**Cosa aggiunge a quello che il summary già diceva.** `CandleCount` e
`CoversRequestedRange` non vedono l'errore che conta: un feed nato su un ancoraggio
diverso ha il numero di barre giusto e copre l'intervallo giusto — sono
semplicemente barre *altre*. È il difetto che su `@KC_240` è passato inosservato
finché non l'ha trovato il metro.

**Parità dimostrata.** Paniere completo `all-in` su `FTMOPLATFORM`, un anno
(2025-08 → 2026-08), 30 stream e 3.973 trade. Prima e dopo: `totalTrades`,
`winningTrades`, `losingTrades`, `totalNetProfit`, `finalEquity`, `maxDrawdown`,
`markedToMarketBars`, `processedIterations` e `wrongSideLevelsRejected` **identici**.
In `trades.json` gli unici campi che differiscono su tutti e 3.973 i trade sono
`tradeId` e `correlationId`, che contengono il GUID del job e sono diversi per
costruzione a ogni run.

**E ha trovato qualcosa al primo giro** — vedi §7ter.

**Passo 4a — il calendario governa la sessione. Fatto il 07/09/2026.**
`Session` non si dichiara più: `EasyEngineBase` la deriva da
`MarketCalendarRegistry` tramite il simbolo, ed è **sola lettura**. Le 124 classi
`PTS_*` hanno perso la riga che la dichiarava (617 righe in meno, tutte
cancellazioni), i tre motori `RhlEngine`, `TfEngines` e `ReversalBollingerBandEngines`
hanno perso i loro `SessionStartTime = 1700` — **codice morto**: il costruttore base
gira prima di quello della sottoclasse, e tutte e 124 lo sovrascrivevano — e
`PTS_NQ_PCH_001_15` il gancio `Initialize` che accettava la sessione da parametri
esterni, che nessuno passava.

L'override esiste ed è `OverrideSessionAnchor(hour, reason)`: **solo l'ora**, e il
motivo è obbligatorio (stringa vuota = eccezione). La sessione *di borsa* — dove le
barre fuori orario non appartengono a nessuna sessione — non è supportata: è un
modello diverso, non un parametro diverso, e nessuna strategia la usa.

**Parità dimostrata** con lo stesso metro del passo 3: `all-in` su `FTMOPLATFORM`,
un anno, 3.973 trade, tutti gli aggregati identici e in `trades.json` solo
`tradeId` e `correlationId` diversi.

Due cose emerse dal fatto stesso di farlo:

- **I test dei motori giravano su un modello che la produzione non usa.** Nove file
  di test costruivano i propri doppioni con `SessionStartTime = 1700` — la sessione
  di borsa — cioè esercitavano il ramo di `ClassifySessionBar` che **nessuna**
  strategia esegue. Tolta la finta sessione, `TfM_UsesPythonHourWindowAndMondayBasedDayFilter`
  è tornato verde: era rosso da prima per quella ragione.
- **Ventuno classi sono su simboli che il calendario non conosce** — HK (5), HO (8),
  JY (8) — e ora la loro sessione non risolve. Non è una regressione: erano già non
  eseguibili, perché manca loro il `PointValue` prima ancora della sessione. HK e HO
  sono usciti dal paniere il 07/09/2026 ma le classi sono rimaste sul disco.
  `StrategyClockConformanceTests.StrategiesOnSymbolsWithoutACalendarAreDeclared` fissa
  il numero perché cambi solo di proposito.

**Passo 4b — gli engine consumano `BarContext`.** Resta da fare: eliminare
`ClassifySessionBar`, `FullDaySessionDay`, `ResolveEntrySessionStartUtc`,
`SessionBarToUtc` e le loro copie nei motori, sostituendoli con letture del
contesto. Include il ramo "sessione di borsa" di `EasyLib`, ora provatamente
irraggiungibile. Tocca i motori, non il catalogo.

**Passo 5 — l'orologio a barre.** `MaxBarsInPosition` e la scadenza dei pending sul
contatore del calendario; `ScaleSignalMaxBarsInPosition` eliminato. È il primo
passo che **cambia i risultati** di proposito (§3): va misurato e dichiarato, non
nascosto in mezzo agli altri.

**Passo 6 — i cBot. Raccoglitore fatto il 07/09/2026; i due operativi restano.**
`PiootooDatafeedSyncBot` **2.0.0** raccoglie soltanto barre da un minuto UTC.
Spariti i tre parametri della griglia — *Fuso dell'ancoraggio*, *Ora di inizio
sessione*, *Timeframe base* — e il codice che li usava: `BucketStartUtc`,
`SessionLocalToUtc`, `SessionStartHourOf`, `TryValidateSessionOffsets`,
`TryResolveBase`, `FoldBackwards`. Con loro sparisce il tetto dei sessanta minuti:
se l'unico ingresso è il minuto, non c'è nessuna serie della piattaforma di cui
fidarsi a nessun timeframe. **Due delle quattro copie della tabella sono chiuse**;
restano quelle dei due bot operativi.

*Timeframe in minuti* resta come parametro ma cambia mestiere: dice cosa far
**derivare al server** a fine backfill, non cosa raccogliere. Con un codice piano è
ignorato — quei timeframe li dichiara il masterfilter.

Il bot chiude il cerchio da solo: a fine backfill chiama
`POST rebuild-from-minutes`, perché altrimenti una raccolta su un archivio nuovo
lascerebbe **solo** il minuto e ogni backtest sopra il minuto troverebbe il
datafeed mancante. Per questo l'endpoint accetta ora anche `planCode`, e costruisce
i timeframe dichiarati **anche quando il file non esiste ancora**: filtrando su ciò
che esiste, una ricostruzione su un archivio nuovo non avrebbe fatto niente
dicendolo come "zero stream", che è indistinguibile da "è andato tutto bene".

Due conseguenze del passaggio al minuto, entrambe da gestire e nessuna ovvia. La
**tolleranza sui buchi** va dichiarata: il default del server è due volte il passo
dominante, cioè due minuti, e su una serie a un minuto ogni notte è un buco — un
anno ne produce centinaia, l'elenco si tronca a duecento, e un elenco troncato fa
saltare il salto dei periodi già coperti, quindi ogni run rispedirebbe milioni di
barre. Il bot chiede la status con quattro giorni di tolleranza. E la **RAM**: la
serie resta in memoria mentre si cammina all'indietro, un anno sono ~370.000 barre
per simbolo contro le 1.500 di una 240; il bot ne stampa la stima all'avvio.

**Il sorgente è stato compilato**, cosa che per un cBot non era mai successa prima
di essere spedito: `piootoo-repository/ctrader/syntax-check/` compila i bot contro
uno stub minimo dell'API cAlgo. Verifica sintassi e tipi, non il comportamento — e
la sua prova di fedeltà è che compila **anche la versione precedente** del bot: uno
stub che riuscisse a compilare solo quella nuova sarebbe stato piegato su di essa.

**Passo 7 — riscaldamento e `PushBars`.** Il server aggrega ciò che il client
spinge, il riscaldamento viene dal disco, R2/R3 di
[`finestra-candele-e-riscaldamento.md`](finestra-candele-e-riscaldamento.md)
spariscono.

**Passo 8 — `aggregate_flat_feed.py` a solo 1m** e rigenerazione della cache del
vendor.

---

## 7bis. Tre file dell'archivio erano da rifare — rifatti

Il metro del passo 2 ha trovato un difetto che nessuno stava cercando, e vale la
pena isolarlo perché è la conferma sperimentale del motivo per cui questo refactor
esiste.

**`CollectedAggregatesSitOnTheirOwnGrid`** verifica una cosa banale: l'etichetta di
ogni barra di un aggregato raccolto deve coincidere con l'inizio del bucket che la
contiene, sulla griglia dichiarata dal suo simbolo. Tre file non la rispettano:

| File | Barre fuori griglia | Su |
|---|---|---|
| `@KC_240.json` | 880 | 1.760 |
| `@KC_1440.json` | 295 | 590 |
| `@CT_1440.json` | 295 | 590 |

**Esattamente la metà, in tutti e tre.** Su `@KC_240` la verifica è conclusiva: le
1.760 barre sono l'**unione esatta** della griglia ancorata alle 00:00 e di quella
ancorata alle 01:00, 880 ciascuna, con zero sovrapposizioni.

Il meccanismo è quello che la deduplica rende inevitabile: la chiave di una barra
è il suo istante di apertura, quindi due raccolte con ancoraggi diversi producono
etichette diverse per lo stesso periodo e **non si sovrascrivono — si sommano**. Il
file che ne esce ha il doppio delle barre, tutte plausibili, metà su una griglia
che nessuna strategia ha mai visto. Guardando il file non si distinguono, ed è
precisamente il caso che `raccolta-datafeed-esterno.md` prevedeva («due file
identici nella forma ma nati su ancoraggi diversi non sono confrontabili») — con
l'aggravante che qui le due griglie stanno *dentro lo stesso file*.

CC, SB e FDAX, che hanno lo stesso ancoraggio di KC e CT, sono puliti: era una
raccolta fatta prima che il cBot imparasse la tabella §2.4, non un difetto dei
simboli.

### La riparazione, e perché è diventata una funzione

I tre file sono stati **rigenerati dal minuto** lo stesso giorno:

| File | Prima | Fuori griglia | Dopo |
|---|---|---|---|
| `@KC_240.json` | 1.760 | 880 | 878 |
| `@KC_1440.json` | 590 | 295 | 293 |
| `@CT_1440.json` | 590 | 295 | 293 |

I due bucket che mancano oltre a quelli fuori griglia sono i bordi incompleti — il
primo, troncato dall'inizio del journal a un minuto, e l'ultimo, in formazione — che
la ricostruzione scarta. Dopo, ogni file sta su **una griglia sola** in entrambe le
stagioni, come CC.

Non è stato uno script usa-e-getta ma
**`POST api/datafeed-external/rebuild-from-minutes`**
(`ExternalDatafeedStore.RebuildFromMinutesAsync`), e per due ragioni. La prima è che
usa il layer: una conversione scritta a parte sarebbe stata una *quarta*
implementazione della regola dei bucket, cioè il problema che questo refactor
esiste per chiudere. La seconda è che serve di nuovo — ogni volta che si rifà la
raccolta a un minuto, gli aggregati vanno rigenerati, ed è il §6 ("gli aggregati
diventano una cache derivata") che arriva in anticipo.

Due dettagli che non sono facoltativi. Il **journal del bersaglio si cancella**:
contiene blocchi arrivati sulla vecchia griglia, e lasciarlo lì significherebbe che
la prima compattazione successiva li rifonde dentro e il file torna misto senza che
nulla lo segnali. E il `source` del file dichiara la propria derivazione
(`rebuild-from-1m/6.0.0@FTMOPLATFORM griglia(1m->240m, Europe/Rome 01:00)`), perché
due file identici nella forma e nati su griglie diverse devono restare
distinguibili.

`BarAggregatorMetroTests.KnownMixedGridFeeds` è ora **vuoto**: se torna a
popolarsi, un file è stato raccolto con l'ancoraggio sbagliato e le sue barre non
corrispondono a nessun run.

---

## 7ter. Cosa il layer ha trovato al primo run

Zero barre fuori griglia su tutti e trenta gli stream: dopo la conversione §7bis
l'archivio è pulito. Ma due simboli portano **sessioni che il future non ha**, ed è
esattamente la classe di difetto per cui `SessionDays` esiste.

| stream | barre in giorni senza sessione | su | quota |
|---|---|---|---|
| `BTC/60m` | 972 | 9.051 | 10,7% |
| `BTC/240m` | 265 | 2.299 | 11,5% |
| `FDAX/240m` | 53 | 1.708 | 3,1% |
| `FDAX/1440m` | 53 | 327 | **16,2%** |

Su **BTC** sono i sabati: il CFD quota 24/7, il future CME no. Su **FDAX** sono le
53 domeniche dell'anno — precisamente le sessioni domenicali che il dossier misura
sul DAX all'**11% del P&L**, e su cui girano le sette `PTS_FDAX_*`.

Quelle barre non generano trade da sole, ma **spezzano la sessione**: aprono un
`SessionId` che nella ricerca non esiste, e con l'uscita di fine sessione chiudono
posizioni ancora valide. Finora nessuno strumento le vedeva.

**Il rimedio non è in questo passo.** Filtrarle cambia i risultati, e cambiare i
risultati è lavoro dei passi 4 e 5, dove va misurato da solo invece che mescolato
al resto. Qui si è ottenuto ciò che serviva: il difetto ha un numero.

Nota di lettura sui **gap**: `CC/60m` ha 271 discontinuità su 272 sessioni, cioè
una per sessione. Non è un difetto — è la pausa notturna di un soft, che quota nove
ore al giorno. A 240 minuti lo stesso stream ne ha 271 e `ES/240m` solo 64, perché a
bucket più larghi la pausa cade spesso *dentro* un bucket invece che fra due.

---

## 7quater. Anche il feed del vendor ha barre fuori griglia

Misurato prima di scrivere il Passo 3, su tutti i 26 file di `datafeed/`: **solo i
giornalieri**, e sempre 18-19 barre su ~5.000.

Sono **una all'anno, l'ultima domenica di ottobre** — il giorno in cui l'orologio
torna indietro e la giornata locale dura 25 ore. La causa è la scorciatoia di
`aggregate_flat_feed.py`, che calcola l'inizio del bucket come
`row_utc − minuti_dall'ancoraggio` contando minuti *locali* su un istante *UTC*: su
una giornata di 25 ore l'etichetta esce a 01:00 locali invece che a mezzanotte.

È precisamente il difetto che §2.3 attribuisce alla scorciatoia. I documenti lo
davano per invisibile nel feed del vendor, ma la verifica fatta allora era un'altra
— «nessun passo più corto del timeframe», che una barra mal etichettata non viola.

Costa una barra giornaliera all'anno per simbolo, e si chiude da sé al **Passo 8**:
il layer calcola il confine in ora locale e riconverte, e non ha quel caso. Fino ad
allora è una segnalazione nel summary, non un blocco — ed è il motivo per cui il
Passo 3 descrive invece di decidere.

---

## 8. Validazione: emulato contro reale

L'unico modo per sapere se funziona, ed è già il mestiere della cartella
`piootoo-repository/compare/`. Tre metriche, dalla spec di origine:

1. **correlazione dei rendimenti** barra per barra, in sessione, fra serie emulata
   dal CFD e serie del future;
2. **distribuzione della differenza di prezzo**, che dà il profilo del basis e
   dello skew del broker;
3. **conteggio delle barre in cui il CFD si è mosso oltre soglia mentre il future
   era piatto** — la misura diretta di quanto rumore il layer sta eliminando.

Con l'avvertenza che la spec mette per prima fra le trappole residue: **i livelli
assoluti non sono confrontabili**. Il CFD accumula gli aggiustamenti di rollover e
diverge progressivamente dal future. Qualunque logica su livelli assoluti o su
serie lunghe va letta in termini relativi o ricalibrata sul feed che si usa
davvero in esecuzione.

---

## 9. Cosa NON cambia

Vale la pena dirlo, perché un refactor di questa ampiezza attira modifiche che non
gli appartengono.

- **Il server decide *cosa*, il broker decide *se e a che prezzo*.** Nessun fill
  assunto.
- **Un run legge da un archivio di barre solo.** `DatafeedCatalog` resta l'unico
  punto che traduce un broker in un percorso, e due run su feed diversi restano non
  confrontabili.
- **Lo spread resta una misura sul solo prezzo di ingresso**, e resta una scelta
  separata dal datafeed e dal piano.
- **Le uscite restano autocontenute nel segnale di ingresso.** Il layer cambia
  *come si contano* le barre, non chi decide l'uscita.
- **Gli orari delle strategie restano verbatim dalla ricerca**, con il proprio
  fuso. Il layer toglie la ricostruzione della sessione, non la dichiarazione della
  finestra operativa.

---

## Riferimenti codice

Stato attuale, da leggere prima di toccare qualunque passo del §7.

- `Piootoo.Shared/Configuration/SessionClock.cs` — conversione, cache dell'offset,
  convenzioni sui due giorni di cambio d'ora. Il layer lo usa, non lo sostituisce.
- `Piootoo.Shared/Configuration/ZonedWindow.cs` — `ResearchSession`,
  `ResearchHours`, i fusi.
- `Piootoo.Shared/Configuration/InstrumentRegistry.cs` — la tabella che diventa un
  file.
- `Piootoo.Shared/Models/Trading/InstrumentSpec.cs` — `ResearchSessionStartHour`,
  `SessionDays`.
- `Piootoo.Strategies/Easy/EasyLib.cs` — `ClassifySessionBar`,
  `FullDaySessionDay`, `OHLCMulti5`, `BuildSessionSeries`, `InSessionBars`.
- `Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs` —
  `ResolveEntrySessionStartUtc`, `SessionBarToUtc`, `ResolveCloseAtUtc`.
- `Piootoo.Core/Services/PiootooBacktestingService.cs` — il loop,
  `ShouldEvaluateStrategy`, `ScaleSignalMaxBarsInPosition`,
  `IterationIsSkippedByWeekEndFlat`.
- `Piootoo.Core/Services/PiootooTradingService.cs` — il conteggio di
  `BarsInPosition`.
- `Piootoo.Core/Services/TradingSessionService.cs` — `PushBars`,
  `PushBarWindow`, `TrimHistory`, `StrategyEvaluationService.Evaluate`.
- `Piootoo.Core/Services/ExternalDatafeedStore.cs` — journal, compattazione,
  buchi.
- `Piootoo.Domain/Repositories/DataSourceRepository.cs` — lettura del feed piatto.
- `piootoo-repository/ctrader/PiootooDatafeedSyncBot.cs` — `BucketStartUtc`,
  `SessionStartHourOf`, `FoldBackwards`.
- `piootoo-repository/datafeed-future/aggregate_flat_feed.py` —
  `minutes_into_bucket`, `SESSION_START_HOUR`.

Documenti che questo refactor tocca e che vanno aggiornati alla fine:
[`orari-di-sessione-e-fusi.md`](orari-di-sessione-e-fusi.md),
[`datafeed-generazione.md`](datafeed-generazione.md),
[`raccolta-datafeed-esterno.md`](raccolta-datafeed-esterno.md),
[`finestra-candele-e-riscaldamento.md`](finestra-candele-e-riscaldamento.md),
[`orologio-barre-e-fill.md`](orologio-barre-e-fill.md).
