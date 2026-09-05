# compare-0020 — cBot FTMO contro interno CFD FTMOPLATFORM

Analisi del 05/09/2026. Domanda di partenza: l'equity della gamba esterna scende
per tutto il run, quella interna sale molto. Perche'.

**Risposta in una riga: l'engine interno non modella lo spread, e su HO, NG, PL e
CC lo spread del broker e' piu' grande dello stop delle strategie.**

## Le due gambe

| | file | slug | motore | prezzi | arco |
|---|---|---|---|---|---|
| esterna | `trades-cbot-cfd-FTMO.json` | `cbot-cfd-FTMO` | `PiootooDistributedExecutionBot` in cTrader, engine 5.1.3 | CFD FTMO dal backtester cTrader | **2025-07-01 → 2026-08-30** |
| interna | `trades-interno-cfd-FTMOPLATFORM.json` | `interno-cfd-FTMOPLATFORM` | `PiootooTradingService`, engine 5.1.3 | `datafeed-external/FTMOPLATFORM/` | **2025-07-01 → 2025-08-29** |

Stesso `accountNumber` (17188650), stesso engine. Il broker e' **dedotto**: il
lato cBot dichiara `FTMO` (il conto), il lato interno `FTMOPLATFORM` (la cartella
del raccoglitore); `datafeed-external/FTMO/` non esiste. Sono lo stesso broker —
i prezzi di ingresso lo confermano (vedi *Cosa torna*).

Il lato cBot porta anche `log.txt` (260 MB, 1.133.678 righe, 5.147 chiusure): e'
il file che ha risolto il confronto, e senza le sue righe `Bid/Ask/spread` la
causa non era raggiungibile dai soli trade.

## Esito

**Il confronto come si presenta non e' valido, per due ragioni indipendenti da
sistemare prima di qualsiasi lettura.**

1. **Arco.** Il cBot copre **14 mesi**, l'interno **2**. Il rapporto fra i trade
   (4.969 contro 638, 7,8x) e' il rapporto delle finestre. La curva discendente
   e' reale — il cBot perde 87.512 su 100.000, con calo costante su tutti i 14
   mesi — ma e' misurata su 12 mesi che la gamba interna non ha mai visto.
2. **Capitale.** `InitialCapital` e' 100.000 sul cBot e 1.000.000 sull'interno.
   Il valore-punto interno risulta **esattamente 10x** quello del cBot su *tutti
   e 15* i simboli: il sizing e' proporzionalmente identico, ma i saldi grezzi
   non sono confrontabili.

Ristretto alla finestra comune (2025-07-01 → 2025-08-31) e normalizzato a 100.000:

| | trade | punti | saldo |
|---|---|---|---|
| cBot | 682 | **+8.497** | **−3.692** |
| interno | 638 | **+17.866** | **+23.318** |
| divario | | 9.369 | **27.010** (27,0% del capitale) |

**In punti il cBot e' in profitto.** Il segno si ribalta nella monetizzazione, e
il responsabile e' HO.

## La causa: lo spread, che l'interno non paga

Le righe `Intent` del log portano Bid, Ask e spread al momento di ogni
piazzamento — 122.846 osservazioni. Confrontate con la distanza di stop della
strategia che sta piazzando l'ordine:

| simbolo | strategia | spread mediano | distanza stop | spread / stop |
|---|---|---|---|---|
| HO | `PTS_HO_BIA_001_240` | 0,0350 | 0,0060 | **588%** |
| HO | `PTS_HO_BIA_002_240` | 0,0350 | 0,0060 | **588%** |
| NG | `PTS_NG_TFM_002_240` | 0,0410 | 0,0250 | **164%** |
| PL | `PTS_PL_TFM_001_240` | 6,7250 | 5,0000 | **134%** |
| CC | `PTS_CC_PCH_001_240` | 21,10 | 25,00 | 84% |
| NG | `PTS_NG_TFM_003_30` | 0,0570 | 0,0750 | 76% |
| KC | `PTS_KC_SBO_001_240` | 0,3100 | 0,6667 | 46% |
| HO | `PTS_HO_PCH_001_30` | 0,0330 | 0,1190 | 28% |

Sei strategie su 96 hanno **spread piu' grande dello stop**: la posizione nasce
gia' oltre il proprio livello di uscita e non esiste percorso di prezzo che la
salvi. L'engine interno riempie e chiude sulla stessa serie OHLC di
`datafeed-external/`, senza denaro/lettera: quello stesso stop e' interamente
disponibile.

Per simbolo il rapporto spread/stop ordina il divario quasi perfettamente:

| simbolo | spread/stop | divario normalizzato |
|---|---|---|
| **HO** | **311%** | **+11.661 (43%)** |
| PL | 134% | (fuori finestra comune) |
| KC | 46% | +884 |
| NG | 41% | +1.726 |
| CC | 19% | +984 |
| HK | 18% | +1.571 |
| FDAX | 11% | +2.729 |
| NQ | 3% | +4.201 (36 strategie, molti trade) |
| **GC** | **2%** | **+92** |

GC ha il rapporto piu' basso ed e' l'unico simbolo su cui i due motori
coincidono (225,30 punti contro 224,22). **Non e' una coincidenza: e' la stessa
causa letta dall'altro capo.**

### Secondo effetto dello stesso spread: gli ingressi che non avvengono

Un livello di breakout che cade dentro lo spread e' "dal lato sbagliato" e il
cBot lo scarta (`RejectWrongSideLevels`). Nel log:

    14.731 ingressi scartati — NG 4.646, NQ 4.529, HO 1.962, BTC 1.336

cioe' i simboli in cima alla tabella dello spread. L'interno ne conta 3.286
(`wrongSideLevelsRejected`), **4,5 volte meno**, perche' senza spread il livello
e' quasi sempre dalla parte giusta. Sono trade che esistono su una gamba sola.

## Cosa escludere

Numeri sul **run completo di 14 mesi** (4.969 trade, netto −87.512 su 100.000),
non sulla finestra comune: e' la base di evidenza piu' larga. `costoSpread` e'
`spread mediano × $/punto × n trade`, **una gamba sola**, quindi e' una stima
per difetto.

| escludere | spread/stop | strategie | trade | netto | costoSpread |
|---|---|---|---|---|---|
| **HO** | 311% | 4 | 243 | **−30.432** | ~33.898 |
| **NG** | 41% | 9 | 354 | **−19.851** | ~15.547 |
| **KC** | 46% | 1 | 71 | −1.778 | ~814 |
| **PL** | 134% | 1 | 12 | −1.072 | ~404 |
| `PTS_CC_PCH_001_240` | 84% | — | 32 | −632 | ~675 |

Su HO e NG **il costoSpread e' pari o superiore all'intera perdita**: non
perdono per il segnale, pagano il broker. HO e KC hanno una sola famiglia utile
e PL una sola strategia, quindi escludere il simbolo o la strategia coincide. Su
CC no: solo `PCH_001_240` ha lo stop (25,00) sotto lo spread (21,10), le altre
tre restano.

Il caso limite sono `PTS_HO_BIA_001_240` e `_002_240`, **spread al 588% dello
stop** (0,0350 contro 0,0060): ineseguibili in senso letterale, la posizione
nasce sei volte oltre il proprio livello di uscita.

**Da non escludere, benche' sembrino candidati:** HK (18%) e FDAX (11%) perdono
−3.537 e −7.299 contro un costoSpread di 1.272 e 1.869 — li' il problema e' il
segnale. Tutto il resto sta a ≤3%; su **NQ**, l'unico simbolo in profitto
(+2.677), lo spread e' il 3% dello stop e non c'entra.

**Effetto:** 786 trade (16%) portano 61.724 degli 87.512 di perdita, il **71%**.
Restano −25.788, cioe' 3,4x meno, **ma non un profitto**:

| mese | tutto | senza HO/NG/PL/KC |
|---|---|---|
| 2025-09 | −8.261 | **+12.314** (massimo) |
| 2025-12 | −27.656 | −2.497 |
| 2026-02 | −61.589 | −28.437 |
| 2026-08 | −87.512 | −25.788 |

## Scomposizione

Divario di 27.010 sulla finestra comune, normalizzato:

| blocco | interno | cBot | divario | quota |
|---|---|---|---|---|
| 402 coppie appaiate | 19.415 | 7.397 | 12.018 | 44% |
| 280 trade solo cBot | — | −11.089 | 11.089 | 41% |
| 236 trade solo interno | 3.903 | — | 3.903 | 14% |

Solo 402 trade su 638 interni e 682 cBot hanno controparte: il 37% e il 41% dei
due insiemi non si appaiano, ed e' l'effetto degli ingressi scartati qui sopra.

## Aperto

- **Quanto costa davvero lo spread, trade per trade.** Qui e' misurato al
  *piazzamento* dell'intent, non al fill. Serve la somma dello spread pagato
  all'andata e al ritorno su ogni trade per dire quanta parte dei 27.010 e'
  solo quello; il cBot ha gia' `SpreadStats` e registra lo spread di ingresso
  (`PiootooDistributedExecutionBot.cs`, in `OnPositionOpened`).
- **Cosa resta dopo le esclusioni.** La lista di *Cosa escludere* vale il 71%
  della perdita, ma i −25.788 residui sono un secondo problema intatto: la
  curva ripulita **sale fino a settembre 2025 e gira a dicembre**. Lo spread non
  lo spiega.
- **`PTS_HO_PCH_001_30` merita comunque un'occhiata a se':** 153 chiusure su 219
  con MFE sotto il centesimo di punto. Anche al netto dello spread, un breakout
  di Donchian che non vede mai un tick di profitto e' un sospetto di porting.
  Attenzione: l'MFE nel log e' stampato a 2 decimali e su HO l'intero range e'
  ~0,1, quindi "MFE 0" vuol dire "< 0,005", non zero.
- **La sessione del cBot gira a concorrenza spenta** (`concorrenza=OFF
  maxTrade=illimitati`, prima riga del log) eppure entrambe le gambe toccano
  esattamente 30 posizioni contemporanee. Coincidenza da verificare: se
  l'interno invece un tappo a 30 ce l'ha, i due non hanno lo stesso vincolo.

## Chiuso

- **Non e' il trailing del cBot.** Prima ipotesi, **sbagliata**, e vale la pena
  scriverlo perche' i trade da soli la sostenevano: `PTS_HO_PCH_001_30` esce 14
  volte su 18 allo stop pieno 0,119 mentre l'interno chiude a 0,0238, e la
  tentazione era dire che il cBot non traila. Il contatore `trailing Nx` del log
  lo smentisce: sulle strategie in cui `trailing < stop` — dove l'armamento deve
  essere immediato — il trailing scatta in **354 casi su 365 (97%)**. Dove non
  scatta (`trailing > stop`, 75% dei casi) e' il comportamento corretto: il
  livello di trailing e' piu' largo dello stop finche' il trade non avanza.
  Il trailing di HO_PCH non si arma perche' lo **spread (0,0330) e' 1,4x la
  distanza di trailing (0,0238)**, non per un difetto del meccanismo.
- **Non e' il feed.** Sui 402 trade appaiati la mediana dello scarto sul prezzo
  di ingresso e' **0,0000** su GC, HO, NG, HK e CL, +0,06 su ES, +0,19 su NQ. Le
  due serie di prezzi sono la stessa cosa: nonostante i nomi diversi delle
  cartelle, FTMO e FTMOPLATFORM sono lo stesso broker.
- **Non e' il sizing.** Il valore-punto interno e' 10,0x quello del cBot su tutti
  e 15 i simboli (BTC 5/0,5 — ES 50/5 — GC 100/10 — HO 42.000/4.200 — NQ 20/2),
  esattamente il rapporto dei capitali. Il peso relativo fra simboli e' identico.
- **Non e' la generazione dei segnali.** 682 contro 638 trade sulla finestra
  comune, e sulle coppie appaiate le famiglie d'uscita concordano nel **95%** dei
  casi (382 su 402).
- **Non e' la commissione.** cBot 1.710 su −3.692 di saldo netto: il lordo e' gia'
  negativo (−1.974).
- **Non e' la conversione denaro → punti.** Gli stop dichiarati coincidono dove si
  leggono su entrambe le gambe: `PTS_HO_BIA_001_240` 0,005952 pt ($250) e
  `PTS_HO_BSW_002_60` 0,029762 pt ($1.250), identici.

## Cosa torna

- **GC e' il controllo pulito**: 225,30 punti cBot contro 224,22 interni, 2.131 $
  contro 2.223 normalizzati, spread al 2% dello stop. Su GC i due motori fanno la
  stessa cosa. Se una modifica futura muove GC, ha rotto qualcosa.
- Il trailing interno e' verificabile al tick: Sell @2,333, minimo di barra
  2,332, uscita a 2,332 + 0,0238095 = **2,3558095**, esattamente il prezzo
  registrato. `PeakFavorablePrice` inizializzato all'estremo della barra
  d'ingresso, come da codice.
- I TakeProfit interni si riempiono al livello esatto (67 su 67) e i BreakEven a
  0,00 punti esatti (43 su 43): coerente col modello a sole OHLC, ma e' un fill
  senza slippage ne' spread — la stessa assenza che questo confronto misura.

## Trappole di misura di questa cartella

- **Sommare i due file interi.** E' il primo errore e falsa tutto di 7,8x: gli
  archi sono 14 mesi contro 2. Tagliare a 2025-08-31 prima di qualunque somma.
- **Confrontare i saldi grezzi.** 100.000 contro 1.000.000 di capitale. Dividere
  la gamba interna per 10, oppure ragionare in punti.
- **Leggere `LocalExit:StopLoss` come "stop".** Copre stop, trailing e break-even
  insieme: dei 512 del cBot solo 366 sono stop pieni e 52 sono uscite in
  profitto. Il criterio usato qui e' `|punti| / stopLoss > 0,9`. Diverse uscite
  ES/NQ con MFE 17 punti e perdita 0,06 sono **break-even**, non trailing.
- **Concludere sul motore guardando solo i trade.** L'ipotesi "il cBot non
  traila" era coerente con `trades-*.json` ed e' falsa. Il contatore
  `trailing Nx` e le righe Bid/Ask stanno solo nel log.
- **Leggere "MFE 0" come "mai in profitto".** E' stampato a 2 decimali; su HO
  significa "< 0,005".
- **Fidarsi del nome della cartella.** Si chiama `compsre-0020`, non
  `compare-0020`.
