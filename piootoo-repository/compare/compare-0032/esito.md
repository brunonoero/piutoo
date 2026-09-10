# compare-0032 — cBot FTMO contro backtest interno, un anno intero

Analisi del 2026-09-10. Nessun report HTML: i numeri vengono dagli artefatti della cartella,
dal **log del cBot** (`log.txt`, 1.023.552 righe, 31/08/2025 → 05/09/2026), dal minuto
dell'archivio `datafeed-external/FTMOPLATFORM/` — la stessa cartella che il run ha letto,
rinominata `FTMO` per l'occasione — e dalla misura di spread in
`piootoo-repository/spread/FTMOPLATFORM/`.

**In una riga:** i due motori fanno gli stessi trade e li chiudono quasi sempre allo stesso
modo. Il divario ha tre cause e **nessuna e' un bug di apertura o di chiusura del motore
interno**: un terzo e' un **difetto di persistenza sulla gamba cBot** che ha buttato via 123
chiusure, quasi tutte vincenti; il resto e' **spread mai addebitato** al backtest interno e
**commissione a un sesto** di quella vera. Tolte quelle tre cose le due gambe stanno dentro
il 4%.

## Le due gambe

| | sinistra | destra |
|---|---|---|
| file trade | `trades-cbot-cfd-FTMO.json` + `log.txt` | `trades-interno-cfd-FTMO.json` |
| slug | `cbot-cfd-FTMO` | `interno-cfd-FTMO` |
| motore | `PiootooDistributedExecutionBot` nel backtester cTrader (**tick**, Bid/Ask veri), server in `ExternalBroker` | `PiootooTradingService`, orologio del loop a **1 minuto** |
| barre | serie del backtester cTrader | `datafeed-external/FTMOPLATFORM/` (letta come `FTMO`) |
| piano | `FTMO-44`, conto 17188650, capitale 100.000, profilo `BacktestSorgente` | `FTMO-44`, capitale 1.000.000, 16 simboli mappati, nessuna strategia spenta |
| spread | quello vero del broker, su ogni tick | **nessuno** (`spreadSource: "nessuno"`) |
| commissione | tariffa del broker | 4,00 per contratto |
| arco | 2025-08-31 → 2026-09-05 | 2025-08-31 → 2026-08-30 |
| versione motore | 7.2.0 | 7.2.0 |

Stesso `holding` (overnight e overweek liberi, flat 20:45, fine settimana 20:45→23:00),
stesse 98 strategie, stessi 14 simboli. `intrabarPriority: ProtectiveBeforeTarget`,
`trailingMinStepFraction: 0,10`, `rejectWrongSideLevels: true`,
`wrongSideLevelsRejected: 18.510`.

Il fattore di scala fra le gambe e' **uniforme, ×10** (capitale 1.000.000 contro 100.000): il
valore per punto implicito sta a 10,000 su tredici simboli su quattordici, e le due eccezioni
sono cambio, non taglia — FDAX 8,602 (EUR→USD) e BP 10,417. Tutti i numeri qui sotto sono
**punti × valore-punto interno**, con la commissione del cBot riportata alla stessa taglia.

## Esito

| | trade | lordo normalizzato | netto normalizzato |
|---|---|---|---|
| interno | 4.326 | +241.617 | **+224.313** |
| cBot, **artefatto** | 4.090 | −341.267 | **−448.472** |
| cBot, **log** (completo) | 4.213 | −113.880 | **−224.309** |

La riga che conta e' la terza. Il cBot chiude il proprio run dicendo, di suo pugno:
`TOTALE: 4268 trade, netto -22868.60` (valuta conto, arco intero). L'artefatto ne ha 4.145 e
dichiara −45.415,67. **Il divario vero e' −448.622, non −672.785**: un terzo di quello che
sembrava non e' mai stato una differenza fra i due motori.

## Scomposizione

| causa | effetto sul netto normalizzato |
|---|---|
| **0. Chiusure perse dal server (HTTP 404)** | **−224.163** che non esistono |
| **1. Spread mai addebitato alla gamba interna** | **−396.217** |
| **2. Commissione: 4,00 contro 26,21 per contratto** | **−93.124** |
| **3. Residuo: trade diversi o esiti diversi** | **+40.720** (a favore del cBot) |

Le cause 1 e 2 insieme valgono −489.341 contro un divario vero di −448.622: **lo spiegano per
intero e lo superano**. Quello che resta, +40.720 su un anno e 4.300 trade, e' il 4% del lordo
interno. I due motori sono d'accordo.

### 0. Il server ha buttato via 123 trade del cBot, quasi tutti vincenti

Il log lo dice a chiare lettere, 123 volte:

```
21/09/2025 22:05:00.005 | Chiuso PTS_NQ_TFM_013_1440 NQ Buy: 23665.04 -> 24599.06 qty 2
  | esito TakeProfit | lordo 1868.04 commissioni -3.54 swap 0.00 netto 1864.50
21/09/2025 22:05:00.005 | Registrazione chiusura esterna fallita per NQ/PTS_NQ_TFM_013_1440:
  404 {"detail":"Nessuna posizione aperta per '17188650|NQ|PTS_NQ_TFM_013_1440'"}
```

Il trade e' avvenuto — il cBot lo ha aperto, tenuto due settimane e chiuso in take profit —
ma per il server quella posizione non esisteva piu', la chiusura e' stata rifiutata e il
`PersistedTrade` non e' mai nato. Quei 123 trade **non sono in `trades-cbot-cfd-FTMO.json`**.

| | n | netto (valuta conto) |
|---|---|---|
| chiusure registrate | 4.145 | −45.415,67 |
| **chiusure perse col 404** | **123** | **+22.547,07** |
| totale dichiarato dal cBot | 4.268 | −22.868,60 |

**La perdita non e' casuale, e' orientata.** Fra le perse ci sono 85 take profit e 38 stop;
fra le registrate 460 take profit e 3.388 stop. E la vita della posizione lo spiega:

| | n | p10 | mediana | p90 |
|---|---|---|---|---|
| registrate | 4.145 | 1,1 min | 130 min | 2.847 min |
| **perse** | 123 | **698 min** | **2.399 min** | **15.018 min** |

Piu' a lungo una posizione vive, piu' poll deve sopravvivere, e basta un poll in cui non
compare nello snapshot perche' `ReconcileVanishedPositions`
(`TradingSessionService.cs:2863`) la tolga da `ExternalPositions` e sblocchi la strategia.
Da quel momento la chiusura vera trovera' un 404. Il meccanismo e' quello che il commento di
quel metodo descrive per il caso opposto — la posizione fantasma che non si chiude mai —
applicato al rovescio: **qui il rimedio cancella una posizione viva**. La chiave della
riconciliazione e' `simbolo|strategyCode`, senza lato e senza id di posizione.

Conseguenza per chi legge gli artefatti: `trades-cbot-*.json` **non e' il run del cBot**,
e' il run meno i suoi trade lunghi vincenti. Il riepilogo di `OnStop` nel log e' l'unico
totale giusto.

Ricostruendo i 123 dal log (Fill e Chiuso appaiati in ordine per strategia) e rifacendo
l'appaiamento fra le gambe:

| | coppie | solo interno | solo cBot |
|---|---|---|---|
| con l'artefatto | 3.475 | 851 (+288.405) | 615 |
| **col log completo** | **3.564** | **762 (+66.126)** | 649 |

89 dei 123 recuperati trovano subito la loro controparte interna, e il lordo "che il cBot non
ha" scende del 77%.

### 1. Lo spread non e' un costo per trade, e' una convenzione di uscita

L'archivio raccolto e' la serie **Bid** di cTrader. In piattaforma:

| | apertura | chiusura | dove morde |
|---|---|---|---|
| long | Ask | Bid | **ingresso** (e il trigger d'ingresso scatta uno spread prima) |
| short | Bid | Ask | **uscita** (e lo stop protettivo scatta uno spread prima) |

Il motore interno applica lo spread **solo al prezzo d'ingresso** e mai a trigger, livelli o
uscite (`PiootooTradingService.ApplySpread`, ed e' l'invariante dichiarata). In questo run non
lo applica nemmeno li', perche' `SpreadPoints` e' vuota. Conseguenza: **lo stop di uno short
non scatta mai in anticipo sulla gamba interna, e sul cBot si'.**

Un caso, per intero. `PTS_NQ_TFM_006_30`, 13/11/2025, stop dichiarato 25 punti:

| | |
|---|---|
| log del cBot alle 14:00:00 | `Bid 25396.95 Ask 25398.85 spread 1.9` |
| ingresso short (Bid) | interno 25385,05 · cBot 25384,95 |
| stop del cBot, sull'Ask | 25384,95 + 25 = **25409,95** |
| uscita del cBot | 14:01, **25410,15** |
| massimo Bid della barra 14:01 nell'archivio | **25408,25** |
| 25408,25 + 1,9 di spread | **25410,15** — al centesimo |
| stop dell'interno, sul Bid | 25385,05 + 25 = 25410,05, **mai toccato** |
| uscita dell'interno | 14/11 alle 05:16, take profit a 500 punti |

Lo stesso conto su tutti gli stop veri del cBot, confrontando il prezzo di uscita col range
della barra da un minuto dell'archivio nello stesso minuto — quota di uscite che cade
**fuori** dalla barra:

| simbolo | long | short |
|---|---|---|
| NG | 0% | **100%** |
| CC | 0% | **94%** |
| KC | 0% | **92%** |
| CL | 0% | **88%** |
| ES | 0% | 46% |
| FDAX | 0% | 36% |
| GC | 0% | 33% |
| YM | 0% | 30% |
| NQ | 0% | 24% |
| BTC | 0% | 9% |

Zero per cento sui long su tutti e quattordici i simboli: l'archivio **e'** il Bid, e la
chiusura di un long ci sta dentro sempre. Sugli short il cBot esegue a prezzi che il Bid non
ha mai stampato, e la distanza e' lo spread.

Lo spread misurato dal log su tutto il run, contro la misura di agosto 2026 del
`PiootooSpreadDumpBot`: coincidono ovunque tranne **FDAX, 2,39 contro 1,23** — la misura di un
mese solo sottostima quel simbolo di un fattore due.

### 2. La commissione

La gamba interna addebita `CommissionPerContract = 4,00`. Il cBot paga la tariffa del broker,
che riportata alla taglia interna vale **26,21** per contratto. Su 4.213 trade sono −93.124.
Non e' un difetto del motore: e' un parametro della richiesta che non e' stato allineato al
conto.

### 3. Il residuo: dove i due motori chiudono davvero in modo diverso

Sulle 3.564 coppie del confronto completo:

| | coppie | da ingresso | da uscita | totale |
|---|---|---|---|---|
| **stessa famiglia di uscita** | 3.294 | −44.358 | −28.767 | **−73.125** |
| **famiglia diversa** | 270 | | | **−179.317** |

Dove i due motori chiudono allo stesso modo la differenza e' **−22 per trade**: lo spread
d'ingresso e la granularita' tick contro minuto, cioe' rumore. Tutto il resto sta nelle 270
coppie che finiscono diversamente, e in particolare nelle **139 in cui il cBot esce sul
protettivo e l'interno no** (−316.098). **Novantuno su 139 sono short**: e' la firma
dell'asimmetria Bid/Ask della causa 1, non un difetto in piu'.

### Il numero che decide: spread / distanza di stop

Non serve calcolarlo: **il cBot lo stampa da solo** nel riepilogo di `OnStop`.

| strategia | fill | spread medio | stop | quota |
|---|---|---|---|---|
| `PTS_NG_TFM_002_240` | 43 | 0,049 | 0,03 | **196,7%** |
| `PTS_PL_TFM_001_240` | 19 | 8,339 | 5 | **166,8%** |
| `PTS_CC_PCH_001_240` | 20 | 19,14 | 25 | 76,6% |
| `PTS_NG_TFM_003_30` | 49 | 0,051 | 0,08 | 67,7% |
| `PTS_NG_TFM_005_30` | 67 | 0,050 | 0,10 | 49,9% |
| `PTS_KC_SBO_001_240` | 45 | 0,319 | 0,67 | 47,9% |
| `PTS_NG_TFU_001_240` | 20 | 0,047 | 0,10 | 46,9% |
| `PTS_NG_TFU_002_240` | 27 | 0,046 | 0,10 | 46,5% |
| `PTS_NG_TFM_006_60` | 62 | 0,048 | 0,13 | 38,1% |
| `PTS_NG_TFU_003_60` | 41 | 0,048 | 0,15 | 31,8% |

`PTS_NG_TFM_002_240` e `PTS_PL_TFM_001_240` chiedono uno stop **piu' stretto dello spread**:
per il conto vero il rischio dichiarato non esiste. Entrambe operano lo stesso (43 e 19 fill)
e perdono (−729,56 e −1.300,56), e la prima e' anche quella che il 404 ha colpito piu' di
tutte, 42 chiusure su 43.

NG resta il simbolo peggiore del paniere: con un valore-punto di 10.000 e uno spread di
0,057, **ogni trade NG costa 570** prima di qualsiasi altra cosa. Su 461 trade sono −262.770,
i due terzi della causa 1 da solo.

## Chiuso

- **Gli stop del motore interno.** Ricostruiti dal minuto dell'archivio, con la distanza di
  stop letta dai trade stessi: sulle coppie in cui il cBot va in stop e l'interno no, **solo
  3** hanno il livello interno davvero toccato, e su 124 il prezzo Bid non ci arriva mai, con
  mediana 39% della distanza di stop di margine. Il motore non manca gli stop.
- **Le distanze di stop.** Lette dai trade interni e dal campo `stopLoss` del cBot,
  **coincidono esattamente** su ogni strategia controllata.
- **L'aggregazione oltre l'ora.** Riaggregando il minuto dell'archivio con l'ancoraggio
  dichiarato (ora di inizio sessione in ora di Roma) e confrontando col file: NQ/240m 1.815 su
  1.816 identiche, NG/60m 6.938 su 6.941, FDAX/240m 1.845 su 1.847, GC/30m 13.892 su 13.895.
  Le uniche differenze sono i bucket di bordo. **La griglia nuova e' corretta.**
- **La maschera degli orari future non e' una causa.** Gli ingressi per ora UTC e per giorno
  della settimana hanno la stessa forma sulle due gambe.
- **«Un fill per sessione per lato» regge da entrambe le parti.** Contando gli ingressi per
  (strategia, lato, sessione di ricerca): 105 eccedenze sull'interno su 4.326 trade, 192 sul
  cBot su 4.090, e quasi tutte su `PTS_BTC_BIA_001_60`, che la regola non la dichiara. Il cBot
  ha rifiutato 23.120 tentativi per quel limite e 23.726 per «ingresso gia' in corso».
- **Posizioni sovrapposte.** Zero su entrambe le gambe.
- **La vita di un pending.** 89.660 cancellazioni «scaduto (valido una barra sola)» contro
  appena 100 «sostituito dal signal successivo»: il difetto dei pending sostituiti di
  `compare-0022` e' chiuso, e i due motori contano la barra allo stesso modo.
- **I trade dopo un buco di quotazione.** L'ipotesi che l'interno riempisse ordini che il cBot
  aveva gia' cancellato attraverso il fine settimana e' falsa: lo 0,1% dei trade solo-interno
  nasce dopo un buco superiore all'ora, contro il 5,6% di quelli solo-cBot.

## Aperto

- **Il 404 su `close-external` e' il difetto da chiudere per primo.** Vale 224.163
  normalizzati, colpisce le posizioni lunghe e vincenti, e gli artefatti non lo segnalano in
  nessun modo: `session-summary.json` conta gli intent, non le chiusure rifiutate. Due cose
  servono, e sono diverse fra loro: (a) `ReconcileVanishedPositions` non deve poter togliere
  una posizione che il broker ha davvero aperto — la chiave `simbolo|strategyCode` non ha
  lato ne' id di posizione, e lo snapshot del cBot va confrontato su qualcosa che identifichi
  la posizione; (b) una chiusura rifiutata **non puo' finire in un `Print`**: il P&L esiste, e
  o si registra lo stesso o il run va dichiarato incompleto.
- **Rifare il run interno con lo spread e la commissione veri.** `SpreadBroker` sul broker
  misurato, statistica p50, `CommissionPerContract` alla tariffa del conto. Finche' mancano,
  ogni confronto rimisura gli stessi 489.000.
- **Lo spread interno non tocca le uscite, e questa e' la cosa da decidere.** Anche col
  `SpreadBroker` acceso il motore lo applica solo all'ingresso: lo stop di uno short
  continuerebbe a scattare uno spread dopo quello del cBot. Le 139 coppie sopra dicono quanto
  vale la scelta — **−316.098**. La regola di cambio minimo e' «stop e target di uno short si
  valutano sull'Ask»: un confronto in piu' in `PiootooTradingService`, non una revisione del
  fill.
- **Rimisurare lo spread su piu' di un mese.** FDAX sta a 2,39 sul run e a 1,23 nella misura
  di agosto 2026. Un `SpreadBroker` tarato su quel file sottostima FDAX di un fattore due.
- **Due strategie con lo stop piu' stretto dello spread.** `PTS_NG_TFM_002_240` (196,7%) e
  `PTS_PL_TFM_001_240` (166,8%). Vanno spente o riportate; dietro di loro le sei NG sopra il
  30% e `PTS_CC_PCH_001_240` al 76,6%.
- **BTC/240m ai cambi di ora legale.** Otto barre non coincidono con l'aggregazione dal
  minuto, tutte nei fine settimana del 2025-11-01/02 e del 2026-03-07/08 — proprio i giorni in
  cui l'ancoraggio si sposta.
- **`@KC_240` e' ancora il file a due griglie** descritto in
  `ExternalDatafeedStore.RebuildFromMinutesAsync`: 1.622 barre nella finestra del run, di cui
  meta' fuori griglia. Il run non ci e' cascato — il caricamento le scarta e il summary
  dichiara 809 barre con `barsOffGrid: 0` — ma il file va ricostruito, perche' il filtro e'
  silenzioso e chiunque legga il file direttamente prende per buone il doppio delle barre.
- **18 posizioni ancora aperte a fine run interno** e 9 sul cBot: il loro P&L non e' in nessun
  numero di questa scheda.

## Trappole di misura di questa cartella

- **`trades-cbot-*.json` non e' il run del cBot.** Mancano 123 chiusure su 4.268, per il 404
  della causa 0, e sono orientate verso i vincitori: l'artefatto da solo fa sembrare il cBot
  peggiore di quanto sia stato del doppio. Il totale giusto e' nel riepilogo di `OnStop` in
  fondo al log. **Questa e' la trappola che ha ribaltato l'analisi**: la prima versione di
  questa scheda attribuiva a differenze fra i motori 224.163 che erano un difetto di
  registrazione.
- **`LocalExit:StopLoss` non e' «stop».** Copre stop, trailing e break-even. Le coppie
  «il cBot va in stop e l'interno no» vanno filtrate tenendo solo le perdite oltre il 60% della
  distanza di stop dichiarata: senza quel filtro ci finiscono dentro 385 trailing e 236
  break-even, che sono d'accordo fra le gambe e sposterebbero il conto di 200.000.
- **I netti grezzi non si sommano** e **le commissioni nemmeno**: il cBot lavora a un decimo
  della taglia interna, quindi la sua commissione va moltiplicata per 10 prima di stare nella
  stessa tabella. Confrontata cosi' com'e' sembra meta' di quella interna, mentre e' sei volte
  tanto.
- **Appaiare per prezzo d'ingresso invece che per tempo fabbrica coppie false.** Su
  `PTS_GC_PCH_004_240` il cBot entra alle 02:09 e l'interno alle 02:25 quasi allo stesso
  prezzo: sono due occasioni diverse dello stesso livello, e l'uscita del cBot cade **prima**
  dell'ingresso interno.
- **`@BP_1.json` non c'e' piu' nell'archivio** mentre il summary del run dichiara 396.948
  barre BP/1m: il file e' stato tolto dopo il run. Riguarda 18 trade su 4.326 e non tocca
  nessuna conclusione, ma un controllo su BP oggi non e' rifacibile.
