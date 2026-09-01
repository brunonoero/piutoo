# compare-0014 — NQ 15 minuti, secondo semestre 2024

Analisi del 2026-09-01. Motore **4.0.0 da entrambi i lati**, per la prima volta.

## Le due gambe

| | file | tipo | motore | prezzi | arco |
|---|---|---|---|---|---|
| interno | `trades-interno-futures.json` | `interno-futures` | `PiootooTradingService` 4.0.0 | `datafeed/@NQ_15.json` (vendor, retro-aggiustato) | 01/07 → 30/12/2024 |
| esterno | `trades-cbot-cfd-ICS.json` | `cbot-cfd-ICS` | `PiootooDistributedExecutionBot` nel backtester cTrader, 4.0.0 | CFD **USTEC** del broker ICS, conto 1075035, piano `NQ-15` | 01/07 → 31/12/2024 |

Tutti i campi letti da `run-interno-futures.json` e `run-cbot-cfd-ICS.json`: niente è dedotto.
Il run interno ha `backtest-summary-interno-futures.json` (`holding`: overnight sì, overweek **no**,
flat di sessione 20:45, weekend ven 20:45 → dom 23:00; `wrongSideLevelsRejected` 3.276;
`coversRequestedRange` true; nessuna anomalia in `diagnostics`).

Confronta **motore e feed insieme** — la gamba `interno-cfd-ICS` continua a non esistere, perché
sotto `datafeed-external/` c'è solo RAWTRADINGLTD.

## Esito

Finestra comune **2024-07-01 13:46 → 2024-12-30 15:00**.

| | trade | valuta di conto | USD |
|---|---|---|---|
| interno | 327 | +85.829 USD (netti, 4 $ di commissione a trade) | **+85.829** |
| esterno | 337 | +25.240 EUR | +50.672 lordi **−23.415 di swap** = **+27.258** |

**Divario 58.571 USD, il 68% dell'interno.** Non è deriva: il gap mensile è +12.036, +14.207,
+16.151, **−5.748**, +15.068, +6.857 — cambia segno a ottobre.

Il cambio implicito si muove nel run (0,8929 → 0,9652, mediana 0,9166): convertito trade per trade.

## Scomposizione

Appaiamento: stessa strategia, stesso lato, ingresso entro 2 ore → **294 coppie**, 33 solo-interni,
43 solo-esterni. Segno positivo = l'interno sta sopra.

| causa | USD | quota |
|---|---|---|
| **swap del CFD** (93 trade con almeno una notte, −252 a notte-trade) | 23.415 | 40% |
| **43 trade presi solo dal bot**, di cui: | 27.001 | 46% |
| — 9 entrati dentro la finestra di flat interna (domenica 22:41–22:59) | 8.875 | 15% |
| — 8 su cui l'interno aveva già una posizione aperta sulla stessa strategia | 15.540 | 27% |
| — i restanti 26 | 2.585 | 4% |
| **trailing**: 35 coppie in cui l'interno esce in trailing | 12.286 | 21% |
| 2 coppie in cui l'interno prende il target e il bot resta dentro fino allo stop | 15.015 | 26% |
| **flat del venerdì**: 25 coppie con uscita `WeekEnd` interna | −13.598 | −23% |
| stop → stop, 147 coppie | −3.914 | −7% |
| target → target, 53 coppie | −1.874 | −3% |
| `MaxBars`, 12 coppie | −1.470 | −3% |
| `TimeExit`, 20 coppie | +351 | 1% |
| 33 trade presi solo dall'interno | +1.361 | 2% |
| **totale** | **58.573** | 100% |

Nelle 294 coppie il divario complessivo è solo **6.795 USD**. Separandole per allineamento
dell'uscita: 235 coppie che escono entro un'ora l'una dall'altra valgono +10.603 (45 a coppia),
le 59 disallineate −3.809 — e dentro queste ultime stanno tutte le voci grosse di segno opposto.

## Aperto

- **Le due gambe non hanno la stessa politica di tenuta, ed è l'imputato principale.** L'interno
  gira con `AllowOverweek = false`; l'esterno porta **33 posizioni oltre il sabato** (la più lunga
  6 giorni) ed **entra 9 volte di domenica fra le 22:41 e le 22:59**, cioè dentro la finestra
  ven 20:45 → dom 23:00. Entrambe le cose sono impossibili con `_allowOverweek = false`:
  `EnforceWeekEndFlat` (`PiootooDistributedExecutionBot.cs:1790`) cancella i pending, chiude le
  posizioni e non reclama intent dentro la finestra. Quindi in quel backtest il bot ha ricevuto
  `AllowOverweek = true` dal descriptor — la logica c'è, il permesso no. Da verificare nel piano
  `NQ-15` e nella riga `Tenuta:` che il bot stampa all'avvio. Vale, direttamente o
  indirettamente, 8.875 (i 9 ingressi domenicali) + 13.598 (le 25 coppie che l'interno tronca al
  venerdì) + 13.775 (lo swap dei 33 trade che attraversano il sabato) = **36.248 USD, il 62% del
  divario**. Finché non è allineata, il resto si misura male.
- **Il trailing resta la divergenza sistematica di motore.** 35 coppie: l'interno chiude in utile
  33 volte su 35 per 26.045 USD, il bot sulle stesse posizioni 21 su 35 per 13.759, e tiene in
  mediana 6,1 punti in più. `TrailingMinStepFraction` a 0,10 c'è già in 4.0.0, quindi *non* è il
  passo minimo. Ipotesi da testare: l'interno riempie il trailing esattamente al livello anche
  quando la barra ci passa sopra in gap — è voluto (`docs/domini/orologio-barre-e-fill.md`: il
  fill sull'apertura è ristretto allo stop originale) ma il bot quel gap lo subisce sul tick.
  Stesso segno e stesso ordine di grandezza di compare-0012 (+13.451).
- **`wrongSideLevelsRejected` = 3.276 dal lato interno, il numero del bot non è in questa
  cartella.** Resta la misura che manca da compare-0007.
- I 26 solo-EXT che non hanno spiegazione valgono **−2.585 USD in tutto**: non sono la priorità.

## Chiuso

- **Il numero di ingressi non spiega più niente.** 327 contro 337, e strategia per strategia lo
  scarto massimo è 4 trade su 12 strategie. Era il sospetto principale di compare-0013 (51 trade
  di troppo al bot): con il motore allineato non c'è.
- **L'asimmetria domenicale @NQ/USTEC non esiste.** In compare-0013 dieci ingressi esterni di
  domenica sembravano barre che al feed interno mancavano: il feed `@NQ_15` ha barre dalle 22:00
  in 17 delle 26 domeniche del periodo (172 barre domenicali in tutto) e i 9 ingressi domenicali
  del bot sono tutti alle 22:41 o dopo. Anche dove le barre ci sono l'interno non entra, perché la
  finestra di flat si riapre alle 23:00: non è un buco di dati, è la politica di tenuta.
- **Lo slippage sugli stop non è più un imputato.** 147 coppie stop → stop: la perdita esterna
  supera l'interna di 0,40 punti in mediana (8 USD), e il saldo delle 147 è **−3.914 USD, cioè in
  favore dell'esterno**. Contro i 180 USD a stop di compare-0007, i 74 di compare-0013 e i 26 di
  compare-0012. Misurato anche dal solo lato esterno contro la distanza dichiarata: mediana
  +0,35 punti, p90 +2,00, 77% dei 221 stop peggio del livello.

## Cosa torna

Size 1:1 (20 unità × 1 USD/punto = 1 contratto × 20 USD/punto, invariante su tutti e 665 i trade);
lo scarto di ingresso nelle coppie è 5 minuti in mediana, 28 al p90, e 257 coppie su 294 stanno
entro il quarto d'ora; target realizzati a 0,40 punti dal dichiarato; nessuna inversione di segno;
`net = gross + swap` esatto su tutti i 338 trade esterni (commissione 0 lato broker, 4 $ lato
interno, 1.312 USD in tutto); offset USTEC − @NQ da −969 a luglio a −481 a dicembre, coerente col
carry di un CFD contro un future retro-aggiustato.

## Trappole di misura di questa cartella

- **Lo swap è dentro `netProfit` dell'esterno e non c'è dal lato interno.** Confrontare i due
  `netProfit` regala 23.415 USD di divario che non è né motore né feed: è il costo di
  finanziamento di un CFD, e un future non ce l'ha per costruzione (sta nel roll). La
  scomposizione va fatta sul **lordo**, e lo swap contato una volta sola come voce a sé.
- **`stopLoss` e `takeProfit` sono `null` in tutti i 328 trade interni** e sono **distanze in
  punti** (non livelli di prezzo) nei 338 esterni. Quindi lo slippage interno contro il livello
  dichiarato **non è misurabile da questa cartella**, e chi legge i due file con lo stesso codice
  ottiene numeri senza senso (la prima misura di questa analisi dava una mediana di −19.832 punti).
  Da riempire nell'export interno.
- Le famiglie d'uscita vanno aggregate come sempre: `LocalExit:StopLoss` copre stop, trailing e
  break-even insieme, `LocalExit:Closed` copre `TimeExit`, `MaxBars` e `WeekEnd`.
- La finestra comune finisce il **30/12 alle 15:00**, non a fine anno: il run interno si ferma lì.
