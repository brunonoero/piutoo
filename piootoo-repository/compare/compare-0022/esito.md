# compare-0022 — cBot FTMO contro backtest interno con orologio a 1 minuto

Analisi del 2026-09-07. Nessun report HTML: i numeri vengono dai quattro artefatti
della cartella, dal `signals.json` del run interno
(`workspaces/all-in/backtests/backtest-20260906-1909-2/`) e dalle barre da 1 minuto
di `datafeed-external/FTMOPLATFORM/@BTC_1.json`.

**Aggiornata il 2026-09-07**: la causa 2 e' stata corretta e il run rifatto — vedi
"Verifica dopo la correzione". Le tabelle qui sotto restano quelle del run rotto.

**In una riga:** dove i due motori prendono lo stesso trade sono davvero quasi
identici — l'orologio a 1 minuto ha fatto il suo lavoro sul *prezzo* di
riempimento —, ma prendono trade diversi: il cBot ne fa 688 e l'interno 500, e
solo 308 si appaiano. Il divario non e' spread ne' griglia: sono **un universo
operativo sbagliato** (piano senza broker) e **pending che l'interno non riempie**.

## Le due gambe

| | sinistra | destra |
|---|---|---|
| file trade | `trades-cbot-cfd-FTMO.json` | `trades-interno-cfd-FTMOPLATFORM.json` |
| slug | `cbot-cfd-FTMO` | `interno-cfd-FTMOPLATFORM` |
| motore | `PiootooDistributedExecutionBot` nel backtester cTrader (**tick**), server in `ExternalBroker` | `PiootooTradingService`, orologio del loop a **1 minuto** (`clockTimeframeMinutes: 1`, strategia piu' corta 15m) |
| barre | serie del backtester cTrader | `datafeed-external/FTMOPLATFORM/`, `PiootooDatafeedSyncBot/1.2.1` (griglia `60m->240m, Europe/Rome 00:00`) |
| piano | `FTMO-55`, conto 17188650, capitale 100.000 | `FTMO-55`, capitale 1.000.000, spread `FTMOPLATFORM p50 per simbolo` |
| arco ingressi | 2026-01-02 02:15 → 2026-03-31 23:36 | 2026-01-01 11:15 → 2026-03-30 22:05 |
| trade | 688 | 500 |
| netto grezzo | −14.833,96 (valuta conto, lotti del piano) | −75.714,09 (1 contratto) |

Stesso `holding` (overnight e overweek permessi, flat 20:45, fine settimana
20:45→23:00), stesse 23 strategie spente dal piano, 88 strategie nel run.

**La griglia oltre l'ora non e' piu' un problema.** Tutti i file dell'archivio
dichiarano `PiootooDatafeedSyncBot/1.2.1 … griglia(60m->240m, Europe/Rome 00:00)`:
la causa n. 2 di `compare-0021` e' chiusa.

## Esito

I netti grezzi non sono confrontabili: il cBot lavora in lotti CFD (0,1 · 0,5 · 1 ·
2 · 2,5 · 5 · 50 · 112 secondo il simbolo), l'interno a 1 contratto neutro, e il
valore per punto implicito cambia per simbolo (GC 100 contro 100, NQ 1 contro 20,
CT 1 contro 500). Riportando **entrambe** le gambe a punti × valore-punto interno:

| | trade | netto normalizzato |
|---|---|---|
| cBot | 688 | **−125.739** |
| interno | 500 | **−73.714** |

Stesso segno, stesso trimestre perdente, ma il cBot perde ~1,7 volte tanto. Il
divario **non** e' deriva di prezzo: e' composizione — i due motori non fanno gli
stessi trade.

## Scomposizione

| causa | effetto misurato |
|---|---|
| **1. Piano `FTMO-55` senza `BrokerCode`** — universo neutro sull'interno | 79 trade interni su **NG, KC, PL**, che il conto non opera: **−37.590**, meta' della perdita interna |
| **2. Pending non riempiti dall'interno** — l'ordine muore prima del proprio range | su `PTS_BTC_BIA_001_60`: 72 occasioni, cBot 72 trade, interno **9** |
| **3. Convenzioni di prezzo di fill** — tick contro minuto | sui 308 trade appaiati: **+119,71 punti in totale** sui 196 usciti in stop |

La 3 e' la parte che l'orologio a 1 minuto doveva sistemare, ed e' sistemata.

### 1. Il piano non porta il proprio broker, e l'interno opera simboli che il conto non ha

`plans.json` da' a `FTMO-55` un `BrokerCode` **vuoto**. `ApplyPlanUniverse`
(`Piootoo.Core/Services/PiootooBacktestingService.cs:1938`) tratta un piano senza
broker come universo neutro — comportamento voluto per i piani scritti prima
dell'anagrafica — e il summary lo dichiara (`planUniverse.mappedSymbols: 0`,
`appliedAsNeutralUniverse: true`). Il run interno ha quindi operato **tutto il
masterfilter meno le 23 spente**, NG, KC e PL compresi.

Il conto no: 79 trade interni contro **1** del cBot su quei tre simboli.

Effetto: −37.590 sull'interno, contro un totale di −75.714. Tolti NG/KC/PL
l'interno chiude a **−38.124 su 421 trade**.

Non e' un bug del motore, e' una configurazione: al piano manca il broker. Finche'
manca, `interno-cfd` e `cbot-cfd` **non descrivono lo stesso conto** e il confronto
parte gia' sbilanciato.

### 2. L'interno non riempie i pending che il cBot riempie

Il caso piu' netto e' `PTS_BTC_BIA_001_60`: 424 segnali di ingresso, 336 buy e 88
sell, tutti `Stop` con `validFrom = expires = apertura della barra successiva`.

Ricostruzione meccanica sulle **stesse** barre da 1 minuto dell'archivio interno —
livello non gia' superato all'apertura, e toccato dentro la propria ora:

| | |
|---|---|
| segnali BIA | 424 |
| livello gia' superato all'apertura (scarto legittimo) | 45 |
| **occasioni buone** (toccate dentro la propria barra) | **72** |
| trade del cBot | **72** |
| trade dell'interno | **9** |

Il cBot prende esattamente le 72 che la ricostruzione prevede. L'interno ne prende 9.

Le 63 mancate si dividono su un criterio solo — **la strategia riemette il segnale
sulla barra dopo?**

| | riempito | non riempito |
|---|---|---|
| segnale anche all'ora successiva | 6 | **63** |
| nessun segnale all'ora successiva | **3** | 0 |

Cioe': un pending si riempie solo se la strategia **tace** sulla barra dopo (3 su 3),
oppure se il livello viene toccato nel **primo minuto** (i 6). Appena arriva la
rivalutazione successiva, l'ordine vivo sparisce.

Il meccanismo, letto nel codice:

`EnqueuePendingOrder` (`Piootoo.Core/Services/PiootooTradingService.cs:1069`)
indicizza su `positionKey|side|orderType` e **sovrascrive senza condizioni**. Le
strategie EasyLanguage riemettono l'ordine a ogni barra finche' la condizione
regge (`EasyEngineBase.cs:354`, `ValidFromUtc = ExpiresAtUtc = nextBar`): il
segnale nuovo, valido **dalla barra successiva**, prende il posto di quello che era
valido *adesso*.

L'unica finestra di riempimento che resta al vecchio ordine e' la
`TryFillPendingOrders` in testa a `ProcessSignals`
(`PiootooTradingService.cs:273`), che gira **prima** che il segnale nuovo lo
rimpiazzi. E quella finestra e' larga quanto la barra dell'orologio:

| orologio | cosa vede il vecchio pending prima di essere sostituito |
|---|---|
| 60 minuti (com'era) | la barra da 60 minuti **intera** — il massimo dell'ora, cioe' tutto il suo range |
| 1 minuto (questo run) | **un minuto su sessanta** |

Il difetto quindi non e' nuovo: era mascherato dal fatto che l'orologio coincideva
con il timeframe della strategia. L'orologio fine non l'ha creato, l'ha reso
visibile — ed e' anche la ragione per cui i 6 riempimenti "con segnale all'ora
dopo" cadono tutti nel primo minuto della barra.

**Corretto il 2026-09-07.** La generazione nuova non sostituisce piu' l'incumbent:
aspetta in `PendingOrder.Next` e subentra quando quello ha finito la propria barra
(`ResolveLivePending`), con diritto alla barra corrente. Un ordine per lato,
ripiazzato a ogni barra, come fa il broker. Si cede il posto solo a un incumbent
gia' attivo (`ActivatedAtUtc` valorizzato): una prenotazione che la propria barra
non l'ha ancora vista viene rimpiazzata dalla versione piu' recente, come prima.
`PendingOrderReemissionTests` copre i due casi rotti, il fill fantasma del livello
vecchio nell'ora della generazione dopo, e la regressione a orologio uguale al
timeframe. Vedi `docs/decisioni.md` 2026-09-07 e
`docs/domini/orologio-barre-e-fill.md`. **Le cifre di questa scheda restano quelle
del run rotto**: vanno rifatte su un run nuovo.

Nota sull'A/B che era stato proposto: non serviva piu' a diagnosticare. L'orologio
non puo' superare il timeframe piu' corto del portafoglio (`BacktestClock.Resolve`),
qui **15** minuti, quindi il default di prima non era immune — su BIA a 60 minuti
dava 33 riempimenti su 72, non 72. Il difetto mordeva gia', in silenzio.

**Non e' la scadenza dell'ordine.** `IsExpired` usa
`ExpiresAtUtc + TimeframeMinutes` e a runtime quel campo c'e'
(`EasyEngineBase.cs:359`): il pending vivrebbe fino alle 12:00. Muore prima
perche' viene sostituito, non perche' scade. Vedi anche la nota su
`CloneTradeSignal` sotto.

Il fenomeno non e' solo di BIA. Sulle 58 strategie che riemettono lo stop su barre
consecutive in oltre meta' dei casi: 491 trade cBot contro 330 interni (**0,67**).
Sulle 21 che non riemettono: 140 contro 109 (**0,78**), e i casi limite tornano —
`PTS_FDAX_SBO_001_240`, che non riemette mai, fa 10 contro 9.

### 3. Tick contro minuto: la parte che torna

Appaiando per `strategyCode + direzione + ingresso entro max(timeframe, 60 min)`:
**308 coppie**, di cui **241 escono entro 5 minuti l'una dall'altra**.

| famiglia di uscita interna | coppie | delta punti (cBot − interno) |
|---|---|---|
| StopLoss | 196 | **+119,71** |
| TrailingStop | 34 | +82,29 |
| MaxBars | 3 | +12,86 |
| TimeExit | 9 | −299,33 |
| TakeProfit | 36 | −681,37 |
| BreakEven | 30 | +1.910,12 |

Su 196 stop, +119,71 punti **in totale**: e' la misura del riempimento a tick
contro riempimento a minuto, ed e' rumore. Le due righe grosse — BreakEven e
TakeProfit — non sono prezzo: sono **uscite diverse sullo stesso trade**, e sono
quasi tutte BTC, dove un punto vale molto. Il delta complessivo sulle 308 coppie
e' +1.144 punti e i primi 20 scarti ne fanno +1.387: e' una coda, non una deriva.

Nota di misura: `LocalExit:StopLoss` di cAlgo comprende stop, trailing e break-even
insieme, quindi le famiglie qui sopra sono etichettate con l'uscita **interna**.

## Chi guadagna

Le due gambe concordano su un nocciolo di 16 strategie in attivo su **entrambe**
(valori nella valuta di ciascuna gamba; il rapporto e' l'ordine di 10 per il
fattore di scala del conto, ma non e' uniforme):

| strategia | cBot | interno |
|---|---|---|
| PTS_FDAX_SBO_002_1440 | +1.486,93 | +13.992,00 |
| PTS_NQ_TFM_007_30 | +1.739,22 | +10.470,40 |
| PTS_NQ_TFM_006_30 | +675,70 | +8.484,00 |
| PTS_NQ_TFM_015_240 | +728,60 | +7.476,00 |
| PTS_NQ_RBM_001_15 | +406,54 | +6.829,26 |
| PTS_ES_BSW_002_15 | +263,13 | +4.637,00 |
| PTS_FDAX_SBO_001_240 | +410,39 | +4.205,75 |
| PTS_YM_TFU_003_60 | +203,67 | +3.472,00 |
| PTS_ES_BSW_001_60 | +549,72 | +2.690,00 |
| PTS_NQ_SBO_002_15 | +603,34 | +2.496,00 |
| PTS_NQ_PCH_002_15 | +43,44 | +2.311,20 |
| PTS_GC_PCH_001_60 | +333,20 | +1.742,00 |
| PTS_CT_TFU_001_240 | +210,60 | +1.496,00 |
| PTS_NQ_TFU_001_15 | +765,24 | +992,00 |
| PTS_NG_BSW_001_30 | +49,82 | +496,00 |
| PTS_CL_MAC_001_30 | +39,68 | +471,00 |

Le prime dell'interno che il cBot **non** conferma: `PTS_GC_PCH_004_240` (+11.965
interno, −224 cBot) e `PTS_NQ_TFM_011_60` (+10.734 interno, −317 cBot). Le prime
del cBot che l'interno non conferma: `PTS_GC_TFU_001_30` (+1.959 cBot, −785
interno, 10 trade contro 5) e `PTS_BTC_PCH_001_240` (+1.830 cBot, −7.562 interno,
45 contro 28).

**Nessuna di queste liste e' una classifica di merito**: con conteggi di trade cosi'
diversi fra le gambe, e su un solo trimestre, danno l'ordine di grandezza, non il
verdetto. La classifica si rifa' dopo la causa 1 e la causa 2.

## Verifica dopo la correzione (2026-09-07)

Rifatto il run interno con gli **stessi identici parametri** — `all-in`, `FTMO-55`,
feed `FTMOPLATFORM`, spread `p50` per simbolo, `clockTimeframeMinutes: 1`,
2026-01-01 → 2026-03-31 — e la sola correzione della causa 2 in mezzo. Cartella:
`workspaces/all-in/backtests/backtest-20260907-fix-pending`. La causa 1 e' rimasta
**apposta** dentro, per cambiare una cosa sola.

La previsione era `PTS_BTC_BIA_001_60` da 9 a 72:

| | prima | dopo | cBot |
|---|---|---|---|
| `PTS_BTC_BIA_001_60` | 9 | **63** | 72 |

63 su 72, non 72: la previsione era un **limite superiore**, perche' contava le 72
occasioni come indipendenti mentre una posizione aperta blocca quelle che seguono
finche' non esce. Il grosso del recupero c'e'.

Sull'universo che il conto opera davvero (senza NG/KC/PL, cioe' la causa 1
neutralizzata a posteriori):

| | trade | netto normalizzato |
|---|---|---|
| interno **prima** | 421 | −36.440 |
| interno **dopo** | **712** | **−93.717** |
| cBot | 687 | −129.296 |

E l'appaiamento con il cBot — la misura che conta, perche' dice quanti dei suoi
trade il motore interno vede davvero:

| | appaiati |
|---|---|
| prima | **308 / 688** (45%) |
| dopo | **565 / 688** (82%) |

Tutto si muove nella stessa direzione: conteggio dei trade da −39% a **+4%** del
cBot, netto normalizzato da 3,5 volte piu' piccolo a 1,4, appaiamento da 45% a 82%.
`wrongSideLevelsRejected` resta praticamente fermo (3.694 → 3.713), come deve: la
correzione non tocca il giudizio sul lato del livello.

**Il residuo non e' piu' la causa 2.** I 123 trade del cBot ancora senza
controparte, e il netto che resta piu' negativo di 35.000, vanno cercati altrove —
la causa 1 in testa.

## Aperto

- **La causa 1.** Al piano `FTMO-55` manca il `BrokerCode`, e finche' manca il run
  interno opera simboli che il conto non ha. Va messo, e poi il confronto va
  **riesportato in un `compare-0023`**: la gamba cBot e' la stessa, ma quella interna
  no, e le tabelle di questa scheda — a parte la sezione di verifica — restano quelle
  del run rotto.
- **I 123 trade del cBot ancora senza controparte** dopo la correzione. Prima erano
  380 e la causa era nota; questi no. Da guardare dopo la causa 1, che ne spiega una
  parte da sola.
- **`PTS_BTC_BIA_001_60` a 63 contro 72.** Il divario residuo e' spiegabile con lo
  stato di posizione (una posizione aperta salta le occasioni successive) ma non e'
  stato verificato trade per trade.
- **`CloneTradeSignal` perde `TimeframeMinutes`.**
  `PiootooBacktestingService.CloneTradeSignal` copia `ValidFromUtc`, `ExpiresAtUtc`
  e `MaxBarsInPosition` ma **non** `TimeframeMinutes`, e da quel clone nasce
  `signals.json`: tutti i 35.067 segnali del run lo riportano a `0`
  (`PersistedSignalMapper` scrive `?? 0`). **L'esecuzione e' corretta** — il campo
  c'e' sul segnale vero — ma l'artefatto dice il falso proprio sul numero che
  spiega la vita di un pending, e chi lo legge per diagnosticare conclude che
  l'ordine scade dopo un tick. Difetto di artefatto, non di motore, ma da chiudere:
  e' costato mezza indagine qui dentro.
- **NG sul cBot**: 1 trade contro 61 interni, e l'archivio *ha* `@NG_*.json`. Va
  separato quanto e' universo del piano e quanto e' il simbolo NG sul conto.

## Chiuso

- **Griglia oltre l'ora fra archivio e sessione** — chiusa: l'archivio e' 1.2.1 e
  dichiara `griglia(60m->240m, Europe/Rome 00:00)`. Era la causa 2 di `compare-0021`.
- **Il prezzo di riempimento tick contro minuto** — chiusa: +119,71 punti in totale
  su 196 stop appaiati. Non e' li' che sta il divario.
- **`EntriesToday` di sessione** (causa 1 di `compare-0021`) — non si manifesta qui:
  `PTS_FDAX_VBO_001_240` fa 13 trade sul cBot contro 11 interni.
- **Le uscite non sono sbagliate dall'orologio fine** — chiusa. `TimeExit` e
  `CloseAtUtc` sono istanti UTC assoluti e non dipendono dal tick (le uscite del run
  cadono su 22:59 e 00:00, gli istanti di sessione). `MaxBarsInPosition` viene
  riscalato da `ScaleSignalMaxBarsInPosition` per il rapporto
  `timeframe / orologio`, e la verifica sui 12 trade usciti in `MaxBars` da' durate
  che sono multipli **esatti** del timeframe della strategia: 12 barre su
  `PTS_GC_RHL_002_60`, 48 su `PTS_NQ_TFU_003_15`, 60 e 72 su `PTS_ES_SBO_002_240`.
  Il problema sta sull'**ingresso**, non sull'uscita.

## Cosa torna

- Finestra, `holding`, strategie spente dal piano, commissioni e convenzioni di fill
  sono le stesse sulle due gambe.
- L'appaiamento dei 308 trade comuni: prezzi di ingresso a mediana 1,07 punti di
  scarto (l'ordine di grandezza dello spread applicato), 241 su 308 escono entro
  5 minuti.
- Le famiglie di uscita: 196 stop appaiati su 308, nessun trade "senza controparte"
  fabbricato dall'etichetta.

## Trappole di misura di questa cartella

- **I netti grezzi non si sommano.** Il fattore di scala non e' uniforme: il valore
  per punto implicito cambia per simbolo (rapporto interno/cBot da 1 su GC a 500 su
  CT), perche' il cBot lavora in lotti CFD e l'interno a contratti. L'unica
  normalizzazione onesta e' **punti × valore-punto interno**.
- **Il modulo sul timeframe non misura l'offset degli ingressi oltre l'ora.** Le
  serie 240m e 1440m sono ancorate a mezzanotte europea, non a mezzanotte UTC:
  `(ora·60+minuti) % 240` da' 225 ingressi su 227 "fuori barra" che fuori barra non
  sono.
- **Contare i trade per strategia non basta a dire chi guadagna.** Con 688 contro
  500 trade, una strategia in attivo su una gamba e in passivo sull'altra sta quasi
  sempre dicendo che le due gambe hanno preso trade diversi, non che la strategia
  e' fragile.
