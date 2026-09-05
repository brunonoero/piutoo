# compare-0018 — il trailing intrabarra vale il 30% del risultato interno

Analisi del 2026-09-04. Motore **5.1.1 su entrambe le gambe** (primo run con i campi nuovi del
riepilogo). Il run del cBot era **ancora in corso** e qui c'è solo la prima parte — e proprio per
questo il confronto è il più pulito che abbiamo avuto: nella finestra che copre, le due gambe hanno
lo stesso numero di trade.

## Le gambe

| | file | motore | prezzi | arco ingressi |
|---|---|---|---|---|
| interno CFD | `trades-interno-cfd-FTMOPLATFORM.json` | `PiootooTradingService` 5.1.1 | `datafeed-external/FTMOPLATFORM/` | 01/07/2025 → 29/04/2026 |
| cBot | `trades-cbot-cfd-FTMO.json` + `log-cbot.txt` + export eventi cTrader | `PiootooDistributedExecutionBot` **v5.1.0** nel backtester, server 5.1.1 | CFD FTMO, conto 17188650, piano `FTMO-ALL` | 01/07/2025 → **22/08/2025** |

`Holding` allineato (`allowOverweek: true` da entrambe le parti). Taglia del cBot ancora 1/10, ma
il **capitale è 1/10** (100.000 contro 1.000.000): **la leva è identica**, e le percentuali sono
confrontabili direttamente.

**Il run del cBot era ancora in corso quando è stato esportato**: gli artefatti qui sono la prima
parte, fino al 24/08/2025 (ultimo evento nell'export cTrader: `Create Stop Order` alle 04:00,
equity 102.682). Non è un'interruzione, è uno scarico parziale — ma **va tagliato lo stesso** prima
di sommare qualunque cosa. Il `log-cbot.txt` è troncato dalla piattaforma a 300 KB e copre solo le
prime **11 ore** (01/07 00:00 → 11:00): serve per le righe di avvio, non per il resto.

## 1. Il drawdown: 3,6% contro 12,8%, a parità di leva e sugli stessi trade

Finestra comune 01/07 → 24/08/2025, equity **realizzata** (stessa definizione da entrambe le parti):

| | trade | P&L | drawdown massimo | quando |
|---|---|---|---|---|
| interno | 556 | **+221.280** (+22,1%) | **3,63%** | 31/07/2025 |
| cBot | 548 | **−5.420** (−5,4%) | **12,79%** | 01/08/2025 |

Sull'equity **mark-to-market** del cBot (foglio eventi cTrader, che è la misura vera) il drawdown
massimo è **13,84%**, toccato il **22/07/2025**, con equity minima a 87.278.

Quindi sì: **a luglio il drawdown esterno è più di tre volte quello interno**, e non è un effetto
di taglia né di selezione — i due lati hanno praticamente lo stesso numero di trade nello stesso
periodo. È una differenza di *esito per trade*.

## 2. Da dove viene: il trailing stop nasce e scatta dentro la stessa barra

**221 uscite `TrailingStop` interne avvengono entro la prima barra della posizione, e valgono
+183.003 USD — il 30% del risultato dell'intero run** (+612.047). Di queste, 139 sono in guadagno
per +274.300 complessivi.

Le coppie appaiate mostrano il meccanismo senza ambiguità:

| strategia | interno | cBot |
|---|---|---|
| `PTS_BTC_PCH_001_240` Buy | 11/08 01:00 → **02:00**, `TrailingStop` **+9.958** | 11/08 01:32 → 01:37, `StopLoss` **−50** |
| `PTS_BTC_PCH_001_240` Sell | 25/07 01:00 → 02:00, `TrailingStop` **+4.496** | 25/07 01:09 → 01:36, `StopLoss` **−98** |
| `PTS_FDAX_PCH_001_240` Sell | 31/07 09:00 → 13:00, `TrailingStop` **+4.114** | 31/07 09:30 → 09:36, `StopLoss` **−405** |

`TrailingPeakIncludesCurrentBar` è `true`: il picco favorevole viene alzato al **massimo della barra
in corso** *prima* del controllo dello stop protettivo. Su una barra a 4 ore il livello di trailing
diventa `massimo − distanza`, resta sopra l'ingresso, e il minimo della stessa barra lo attraversa:
l'engine registra un'uscita in trailing con un profitto pieno. Su tick quel percorso non è mai
esistito — il prezzo ha toccato lo stop cinque minuti dopo l'ingresso.

Il commento del campo lo dice già: *«Il motore di riferimento Python non esce mai in trailing sulla
barra del nuovo estremo. Il campo esiste per **misurare** quanto vale quella convenzione, non per
cambiarla di default.»* La misura ora c'è: **vale il 30% del risultato**, e con il segno che
gonfia l'equity e comprime il drawdown.

La priorità intrabarra (stop prima del target) è già conservativa e non c'entra: qui il problema è
che il **picco** del trailing è ottimista, e la protettiva che ne deriva non è una protettiva — è
un target camuffato.

### 2.1 Corretto il 04/09/2026 (motore 5.1.2)

`TrailingPeakIncludesCurrentBar` passa a **`false`** di default, e il picco porta con sé la propria
barra (`OpenPosition.PeakBarUtc`): il trailing non decide sulla barra che ha segnato il picco.

**Il flag da solo non bastava.** `CheckStopLossAndTakeProfit` gira più di una volta sulla stessa
barra, quindi spostare `RaisePeak` dopo i controlli lasciava il secondo passaggio con il picco già
alzato — la posizione si chiudeva lo stesso. Serviva ricordare *quale* barra ha prodotto il picco.

Per un trailing **in perdita** non cambia niente: lì il picco viene già da barre precedenti. La
correzione toglie solo i casi fabbricati.

Il test `PtsPriceChannelTests.Engine_ClosesLongAtTrailingStopFromFavorableHigh` codificava il
difetto — terza barra `O 101 · H 170 · L 119 · C 120`, uscita attesa a 120 su quella stessa barra —
ed è stato riscritto: picco sulla terza barra, ritracciamento sulla quarta, stessa aritmetica. Nuovo
test `IlTrailingNonEsceSullaBarraDelNuovoEstremo` con la barra vera in scala. Build verde, 43 test
falliti = baseline invariato.

**Il §1 e il §2 sono quindi la misura del run *prima* della correzione.** Vanno rifatti sul run
nuovo: è l'unico modo di sapere quanto resta del divario.

## 3. Le uscite protettive vere invece coincidono

Distanza realizzata dall'ingresso sulle coppie in cui entrambi escono su una protettiva, mediana
per simbolo:

| sym | n | interno | cBot | differenza |
|---|---|---|---|---|
| NQ | 120 | −25,000 | −25,200 | −0,200 |
| BTC | 25 | −50,000 | −51,320 | −1,320 |
| FDAX | 21 | −10,000 | −10,790 | −0,790 |
| GC | 17 | −5,000 | −5,150 | −0,150 |
| HK | 19 | −39,002 | −39,140 | −0,138 |
| ES | 20 | 0,000 | −0,160 | −0,160 |
| **YM** | 17 | −50,000 | **−73,500** | **−23,500** |
| **HO** | 9 | −0,015 | **−0,119** | **−0,104** |

L'interno riempie esattamente al livello dichiarato, il cBot qualche decimo oltre: è slippage
onesto, e **conferma compare-0016**. Due eccezioni vere:

- **YM: 23,5 punti oltre il livello** — a $5 per punto sono ~$117 a trade sulla taglia di
  riferimento.
- **HO: 0,104 punti oltre** — sembra poco, ma HO vale $42.000 per punto: sono **~4.400 USD a
  trade**, e infatti HO da solo pesa +38.817 USD sulle 9 coppie protettiva/protettiva.

## 4. I campi nuovi del riepilogo funzionano

`backtest-summary.json` di questo run dichiara:

```
accountUniverse   { "symbolConversionCode": "cfd-ctrader-ftmo",
                    "mappedSymbols": 16, "enabledSymbols": 16,
                    "appliedAsNeutralAccount": false }
fillConventions   { "intrabarPriority": "ProtectiveBeforeTarget",
                    "trailingPeakIncludesCurrentBar": true,
                    "trailingMinStepFraction": 0.1, "rejectWrongSideLevels": true }
catalogStrategies 111   masterfilterStrategies 111   strategiesNotInMasterfilter []
```

La tabella di conversione ora si risolve (16 simboli, 16 abilitati), il catalogo è sceso da 124 a
111 classi e coincide col masterfilter. La diagnosi `[calendario]` ha funzionato al primo colpo:

> `[calendario] PTS_HO_BSW_003_60: il feed non ha nessuna barra all'istante programmato di 1 leg su
> HO/60m — uscita LONG (ven 2300).`

**Ma il controllo è troppo stretto.** `PTS_HO_BSW_001_60` e `PTS_HO_BSW_002_60` chiudono il run con
**zero segnali buy** (l'ingresso LONG è martedì 23:00) e `PTS_NG_BSW_001_30` con **un solo** segnale
sell in dieci mesi: quelle leg hanno una o due barre disponibili, non zero, quindi
`UnreachableScheduleLegs` non le segnala. Va allargato a «leg *rara*», non solo «leg assente».

Stessa cosa per `[riscaldamento]`: `PTS_NQ_VBO_002_240` salta 607 barre contro 695 valutazioni e
non scatta, perché la soglia è `saltate >= valutate`. Con la finestra allungata è passata sotto per
un soffio.

## 5. `PTS_KC_SBO_001_240`: il fix `level_source = 1` ha effetto

Da 28 trade interni (0017, finestra più corta) a **51**, con il cBot a 14. Il livello è sceso e
gli ingressi sono aumentati, come previsto. Il confronto con
`KC_4h/consegna/trades/fam01_BO.csv` non è ancora stato fatto: è quello che chiude la voce.

## 6. Le VBO: ancora zero dal lato cBot, e il log non lo spiega

`PTS_FDAX_VBO_001_240` fa **44 trade interni con `skippedNotEnoughCandles = 0`** (quindi
riscaldamento pieno) e **zero** dal cBot; `PTS_NQ_VBO_001_1440` 6 e zero; `PTS_NQ_VBO_002_240` 6 e
zero. Nel `log-cbot.txt` **non compare nessuna riga «Storia insufficiente»**, il che — se il livello
di log non l'ha filtrata — significa che tutti gli stream hanno raggiunto la propria finestra.

Il log però copre solo le prime 11 ore e il primo segnale VBO su FDAX è del **02/07 08:00**: fuori
finestra. Serve ancora il `session-summary.json` con `everEvaluable`.

## Cosa torna e non va riaperto

- **Le uscite protettive «normali» coincidono** entro qualche decimo di punto su NQ, BTC, FDAX, GC,
  HK, ES, KC, CL. Lo slippage del cBot non è un tema (terza conferma dopo 0016 e 0017).
- **Le taglie sono coerenti** su tutti i simboli: rapporto 1/10 esatto, incluso HO (qty 42, valore
  punto implicito 100 → 4.200 contro 42.000) e HK. I moltiplicatori corretti sono arrivati in
  `accounts/symbol-conversions.json`.
- **`accountUniverse` e `fillConventions` sono nel riepilogo** e si leggono.

## Da fare, in ordine

1. **`TrailingPeakIncludesCurrentBar = false` come default**, o meglio: il picco non può includere
   l'estremo della barra su cui si esce. Vale il 30% del risultato interno e falsa il drawdown di
   un fattore tre. È il numero più grande trovato in tre confronti.
2. **Rifare le misure a run del cBot finito.** Quanto sopra è la prima parte (otto settimane su
   dieci mesi); il difetto del §2 è strutturale e non cambierà, i totali sì.
3. **HO e YM: le protettive del cBot escono molto oltre il livello.** Su HO sono ~4.400 USD a trade
   per il valore punto.
4. Allargare `UnreachableScheduleLegs` alle leg **rare**, e abbassare la soglia di
   `[riscaldamento]`.
5. VBO: `session-summary.json` con `everEvaluable`, e la sessione va chiusa normalmente.
6. `PTS_KC_SBO_001_240` contro `fam01_BO.csv`.

## Trappole di misura di questa cartella

- **L'export del cBot è parziale — il run era ancora in corso**: copre 01/07 → 22/08/2025 contro i
  dieci mesi dell'interno. Sommare i totali senza tagliare dà 3.598 trade contro 548 e non
  significa niente.
- **Il capitale non è lo stesso** (1.000.000 contro 100.000) ma la taglia sì (1/10): la leva
  coincide, quindi le **percentuali** si confrontano e i valori assoluti no.
- **Il `log-cbot.txt` è troncato a 300 KB dalla piattaforma** e contiene solo l'inizio del run.
  L'assenza di una riga non prova che l'evento non sia successo, tranne per le righe di avvio.
- L'export eventi cTrader **non ha la colonna del simbolo**: serve per l'equity e i tempi, non per
  attribuire un evento a una strategia.
