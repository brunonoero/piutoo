# compare-0015 — portafoglio intero, primo semestre 2024, tre gambe

Analisi del 2026-09-01. Motore **4.0.0 su tutte e tre le gambe**.
Report: https://claude.ai/code/artifact/a92f10e6-0fdb-492b-8394-f101f8ed6f6d

È il primo confronto con **tutti e tre i tipi** insieme, quindi il primo in cui feed e motore
si misurano separati invece che sommati.

## Le tre gambe

| | file | tipo | motore | prezzi | arco |
|---|---|---|---|---|---|
| interno futures | `trades-interno-futures.json` | `interno-futures` | `PiootooTradingService` 4.0.0 | `datafeed/` (vendor, `@SYM` retro-aggiustati) | 02/01 → 31/07/2024 |
| interno CFD | `trades-interno-cfd-RAWTRADINGLTD.json` | `interno-cfd-RAWTRADINGLTD` | `PiootooTradingService` 4.0.0 | `datafeed-external/RAWTRADINGLTD/` | 02/01 → 31/07/2024 |
| cBot | `trades-cbot-cfd-ICS.json` | `cbot-cfd-ICS` | `PiootooDistributedExecutionBot` nel backtester cTrader, 4.0.0 | CFD del broker **ICS**, conto 1075035, piano `ALL-76` | 02/01 → 02/07/2024 |

Niente è dedotto: motore, origine e serie prezzi vengono da `run-*.json`. Le due gambe interne
hanno il proprio `backtest-summary` (`holding`: overnight sì, **overweek no**, flat di sessione
20:45, weekend ven 20:45 → dom 23:00; `wrongSideLevelsRejected` 6.349 futures / 6.536 CFD;
`coversRequestedRange` true su tutti e 19 i datasource; zero `skippedNoData`, zero errori).

75 strategie, 9 simboli. Il piano del cBot si chiama `ALL-76` perché **il catalogo eseguibile ha
esattamente 76 strategie**: il run interno ne ha 75 perché `PTS_BTC_PCH_001_240` non ha datafeed,
e nemmeno la sessione esterna ha uno stream BTC. Non manca niente.

## Esito

Finestra comune sugli **ingressi**: **2024-01-02 → 2024-07-01**. Il cBot si ferma a metà
giornata il 2 luglio e quella giornata monca è esclusa da tutti e tre i lati (2 trade esterni).

I trade del cBot sono riespressi in USD **punto per punto** (`punti × valore-punto interno`),
non con un cambio: il cambio implicito si muove nel run (0,900 → 0,950, mediana 0,9247) e il
metodo per punti è insensibile alla deriva. FDAX resta in EUR da entrambi i lati, come sempre.

| | trade | lordo (USD-equiv) | oneri | netto |
|---|---|---|---|---|
| interno futures | 1.180 | **+282.411** | commissioni −4.720 | +277.691 |
| interno CFD | 1.234 | **+218.339** | commissioni −4.936 | +213.403 |
| cBot ICS | 1.179 | **+148.192** | swap −6.126, commissioni −1.865 | +140.200 |

- **Divario di feed** (futures → CFD, stesso motore): **64.072 USD, il 23%.**
- **Divario di motore** (interno CFD → cBot, stesso tipo di prezzi): **70.147 USD, il 32%.**

Nessuno dei due è deriva. Mensile, feed: +18.991, −14, +12.012, +10.108, +36.576, −11.814.
Mensile, motore: +28.939, +7.786, +11.460, **−1.942**, **−2.434**, +15.046.

**Il confronto di motore è legittimo, e questa volta si può dimostrare.** Sui trade appaiati
entro mezz'ora, lo scarto di prezzo d'ingresso fra RAWTRADINGLTD e ICS ha mediana **0,00** su
ogni simbolo (NQ −0,11 punti su 18.138, ES −0,05 su 5.180, GC 0,00, FDAX 0,00, NG 0,00,
CL 0,00, YM −0,21). I due archivi CFD sono la stessa serie: il timore del README — "due broker
non sono confrontabili" — in questo periodo e su questi simboli **non si materializza**. Lo
scarto futures/CFD invece è quello noto e deriva: NQ 1.331 → 1.020 punti, ES 338 → 268,
GC 221 → 184.

## Scomposizione

Appaiamento per strategia + lato, ingresso entro **una barra della strategia** (minimo 2 ore).

### Divario di motore — 70.147 USD

| voce | n | USD | quota |
|---|---|---|---|
| coppie (stesso trade da entrambi i lati) | 1.019 | **+40.520** | 58% |
| trade che prende **solo l'interno** | 215 | +62.896 | 90% |
| trade che prende **solo il cBot** | 160 | −33.269 | −47% |

Dentro le 1.019 coppie, per esito interno:

| esito interno | n | INT | EXT | delta | per trade |
|---|---|---|---|---|---|
| TakeProfit | 81 | +303.500 | +283.290 | **+20.210** | +250 |
| TimeExit | 72 | +68.679 | +55.954 | +12.725 | +177 |
| WeekEnd | 170 | +217.041 | +209.983 | +7.057 | +42 |
| StopLoss | 479 | −495.549 | −501.326 | **+5.777** | **+12** |
| BreakEven | 64 | 0 | −2.716 | +2.716 | +42 |
| MaxBars | 23 | +17.773 | +17.604 | +169 | +7 |
| OppositeSignal | 13 | −10.178 | −9.539 | −639 | −49 |
| TrailingStop | 117 | +54.177 | +61.673 | **−7.496** | **−64** |

### Divario di feed — 64.072 USD

| voce | n | USD |
|---|---|---|
| coppie | 784 | **−8.314** |
| solo futures | 396 | +55.614 |
| solo CFD | 450 | −16.773 |

**Il feed non cambia l'esito del singolo trade: cambia quali trade esistono.** Su 784 coppie il
saldo è −8.314 (−11 USD a trade), mentre 846 trade su ~1.200 non hanno controparte affatto.

## Anomalie di motore

### 1. Quattro strategie che il cBot non esegue mai — VBO al completo

| codice | motore | trade interni (fut / cfd) | trade cBot | USD interni (cfd) |
|---|---|---|---|---|
| `PTS_FDAX_VBO_001_240` | Volatility Breakout | 17 / 14 | **0** | +6.642 |
| `PTS_NQ_VBO_002_240` | Volatility Breakout | 11 / 9 | **0** | +5 |
| `PTS_NQ_VBO_001_1440` | Volatility Breakout | 2 / 1 | **0** | −1.000 |
| `PTS_FDAX_MAC_001_240` | MA Crossover | 2 / 6 | **0** | −285 |

Sono esattamente le uniche quattro: le altre 71 strategie hanno almeno un fill da entrambi i
lati. **La famiglia VBO è morta al 100%** — 24 trade interni, zero esterni, su due simboli e
tre timeframe diversi. FDAX e NQ funzionano in generale (FDAX ha 53 fill esterni fra SBO e PCH),
quindi non è il simbolo.

Cosa condividono le tre VBO: `EntryOrderType = Stop` con
`EntryLevel = SessionOpenAtrBand`, cioè **uno stop armato a `O_d0 ± k × VOL`, livello fissato
all'apertura della sessione e immobile per tutta la sessione**. È la stessa forma che su GC fa
scartare sessioni intere dal `RejectWrongSideLevels` del cBot: quando il prezzo ha già
oltrepassato la banda, lo stop è dalla parte sbagliata e il bot lo rifiuta al piazzamento.
L'interno ha lo stesso filtro acceso (6.536 rifiuti nel run) ma decide **una volta per barra su
`bar.Open`**, il bot **a ogni piazzamento sul Bid/Ask corrente**: su una barra a 4 ore la
differenza è enorme.

**Aggiornamento del 2026-09-01, con gli intent alla mano: c'è un'ipotesi migliore di questa, ed è
il riscaldamento** — `NQ_VBO_002_240` chiede **606** barre a 4 ore e `FDAX_VBO_001_240` ne chiede
**501**, contro le 36 di ogni altra strategia sugli stessi due stream. Sono le due soglie più alte
del portafoglio, e il server salta in silenzio finché la storia non le raggiunge. Il dettaglio è
nella sezione «Gli intent», più sotto.

Entrambe restano **ipotesi**: dai soli trade, e dagli intent riempiti, non si distingue "mai
valutata" da "intent mai emesso" da "ordine rifiutato". Le separa il fix 1 del piano.

`PTS_FDAX_MAC_001_240` sta nella lista ma con 2 e 6 trade interni **non è misurabile**: zero
esterni è compatibile col caso.

In soldi queste quattro pesano poco (~5.400 USD nella finestra). Il valore della segnalazione è
il meccanismo, non la cifra.

### 2. BSW — il bot esegue 9 entrate su 34, e sono le peggiori: 35.481 USD

È **la voce singola più grande del divario di motore, il 50%**.

`PTS_ES_BSW_003_15`, `PTS_ES_BSW_001_60`, `PTS_ES_BSW_002_15` (Bias settimanale su ES) emettono
**un ingresso market a un istante fisso della settimana** — lunedì 11:00 CET, venerdì 03:00 CET,
lunedì 02:00 CET — e l'uscita viaggia col segnale (`CloseAtUtc`). Nella finestra:

- interno CFD: **34 trade, +36.568 USD**; interno futures 35, +21.712
- cBot: **9 trade, +844 USD**
- i 25 mancati valgono **+35.481 USD**, media +1.419 l'uno; 16 dei 25 escono al flat del venerdì
  (sono le settimane portate intere). I 9 presi valgono in media +121.

Due difetti distinti, entrambi verificati:

**a) Il cBot entra una barra dopo, sempre.** Ritardo d'ingresso sulle coppie BSW: mediana
**+15,0 minuti** sulle strategie a 15 minuti (p10 15,0, p90 15,3) e **+60,0 minuti** su quella a
60. Non è dispersione, è esattamente una barra. Per confronto, sulle altre famiglie la mediana
sta fra 0 e 7 minuti e p10 è spesso negativa. L'interno entra all'**apertura** della barra il cui
timestamp è l'orario programmato; il bot vede il segnale quando quella barra **chiude**, e apre
alla successiva. Un ingresso "market all'apertura della barra X" non è replicabile live: o la
strategia dichiara la barra X−1 come barra di decisione, o le due gambe non misurano la stessa
cosa. Su BSW_001_60 il disallineamento è di un'ora piena.

**b) Le altre 25 settimane non entrano affatto**, e i lunedì saltati non seguono un ordine
(prese 26/02, 04/03, 15/04, 29/04, 20/05, 17/06; saltate tutte le altre). **Non è la
concorrenza**: 7 delle 25 mancate hanno **zero** posizioni esterne aperte in quell'istante (10
su 82 contando anche BIA, VBO e RHL), e `MaxConcurrentTrades` non morde da nessuna delle due
parti. Causa non determinabile dai soli
trade.

`PTS_YM_BIA_001_240` (Bias su conteggio barre, altro ingresso programmato) ha la stessa forma:
**30 trade interni contro 5**, e i 5 presi sono all'orario esatto, ritardo mediano 0. Costo
−5.899 USD (qui il bot ha saltato dei perdenti). `GC_RHL_001/002_60` (livelli `H_d1`/`L_d1`,
la famiglia dell'indagine GC): 17 interni contro 10.

### 3. Risoluzione intrabarra ottimista: 4 trade, 21.844 USD, il 31% del divario di motore

Quattro coppie in cui **l'interno registra il target e il cBot lo stop, sullo stesso trade**:

| codice | ingresso | interno | cBot | delta |
|---|---|---|---|---|
| `PTS_NQ_PCH_003_30` | 21/02 21:30 | +10.000 TakeProfit | −2.094 stop | +12.094 |
| `PTS_FDAX_SBO_001_240` | 08/01 10:00 | +3.000 TakeProfit | −250 stop | +3.250 |
| `PTS_FDAX_SBO_001_240` | 22/04 09:00 | +3.000 TakeProfit | −250 stop | +3.250 |
| `PTS_FDAX_SBO_001_240` | 23/04 09:00 | +3.000 TakeProfit | −250 stop | +3.250 |

**Causa individuata il 2026-09-01, dopo l'arrivo degli intent — e non è quella scritta qui
sopra nella prima stesura.** L'engine *già* fa precedere lo stop protettivo al target
(`CheckStopLossAndTakeProfit`). Il difetto è più preciso: **sulla barra che esegue l'ingresso lo
stop legge il close e il target legge l'high.** La convenzione O→L→H→C restringe l'estremo
sfavorevole al close — su un buy stop riempito nella tratta L→H il minimo è pre-fill e non può
stoppare una posizione che non esisteva ancora, ed è giusto così — ma il target continua a
leggere il massimo **pieno**. Su una barra che copre entrambi i livelli vince quindi sempre il
target.

I tick dell'esterno lo confermano: in tutti e tre i casi il cBot **entra ed esce dentro la
stessa barra da 4 ore** dell'interno — 8/1 ingresso 12:10 e stop 12:16, 22/4 09:42 e 09:46, 23/4
09:18 e 09:20 — a prezzi d'ingresso identici a quelli interni al decimo di punto. Su FDAX_SBO lo
stop è 10 punti e il target 120: la barra li copre entrambi molto spesso.

La cella opposta esiste ma è più piccola: 5 coppie in cui l'interno esce a tempo e il cBot a
target, −5.655.

Stesso capitolo, altro sintomo: **20 coppie in cui l'interno arriva all'uscita a tempo e il cBot
viene stoppato prima, +22.473 USD**. In 9 di queste il trade esterno dura meno di 2 ore contro
13-40 ore dell'interno — il cBot è già fuori quando l'interno è ancora dentro.

### 4. Trade a durata nulla: 96 sull'interno, 1 sul cBot

96 trade interni CFD (78 sul futures) aprono e chiudono **allo stesso identico timestamp**, l'8%
del totale: 90 sono stop loss pieni, valgono −30.687 USD, e 64 su 96 stanno su barre a 4 ore.
Concentrati su `FDAX_PCH_001_240` (16), `YM_SBO_001_240` (10), `NQ_PCH_002_15` (9),
`NQ_PCH_001_15` (9). Il cBot ne ha **uno**.

È lo stesso problema del punto 3 visto da un'altra faccia: ingresso stop e stop di protezione
dentro la stessa barra, e l'engine li risolve entrambi sul timestamp d'apertura. Non è
necessariamente sbagliato — su quelle barre la perdita è plausibile — ma **su quei trade
qualunque misura di orario o di durata è priva di significato**, e vanno esclusi prima di
misurare ritardi di ingresso o tempi di tenuta.

### 5. Il trailing stop interno esce troppo presto — e il segno è cambiato rispetto a compare-0014

117 coppie con uscita interna in trailing: interno +54.177, cBot +61.673, **−7.496 USD, −64 a
trade**. È l'**unica** famiglia d'uscita in cui l'interno sta sistematicamente sotto. Casi tipici:
`NQ_PCH_008_240` 14/05 → interno +2.232 contro +6.451; `NQ_PCH_003_30` 11/06 → +4.452 contro
+7.223; `NQ_PCH_008_240` 11/06 → +4.429 contro +7.019.

**Attenzione: in compare-0014 il segno era opposto** (interno 26.045 contro esterno 13.759 su 35
coppie). Stesso motore 4.0.0, stesso `TrailingMinStepFraction` 0,10. O il campione di 0014 era
troppo piccolo, o il comportamento dipende dal simbolo. Da rimisurare prima di toccare il codice.

## Perché le due curve di equity non si somigliano — e non è il motore

Domanda nata guardando i grafici: su cTrader l'equity a maggio torna quasi al capitale iniziale,
nei run interni scende ma continua a salire. **La discesa è la stessa, il denominatore no.**

| | INT-FUT | INT-CFD | cBot |
|---|---|---|---|
| capitale iniziale dichiarato | 1.000.000 | 1.000.000 | **100.000** |
| P&L cumulato a fine marzo | +171.622 | +140.545 | +95.760 |
| aprile | **−73.442** | **−89.467** | **−99.741** |
| P&L cumulato a fine aprile | +98.181 | +51.078 | **−3.981** |
| massimo drawdown **in denaro** | 105.795 | 135.229 | 140.582 |
| massimo drawdown **in % dell'equity** | 8,9% | 11,6% | **64,3%** |
| giorno del minimo | 2024-05-03 | 2024-05-03 | **2024-05-03** |

Tutte e tre toccano il fondo **lo stesso giorno**, e in denaro la perdita è dello stesso ordine
(106k / 135k / 141k). È aprile su **NQ** da tutte e tre le parti: −68.661 sul futures, −86.546
sul CFD interno, −88.825 sul cBot, con gli altri otto simboli quasi in pari. Non è un evento
diverso visto da due motori: è lo stesso evento.

Due cause si sommano, in quest'ordine di importanza:

1. **Il capitale è dieci volte diverso.** I due run interni girano con `initialCapital`
   1.000.000, il conto del cBot con 100.000. La quantità è **fissa a 1 contratto** su tutti i
   1.378 trade interni, quindi il capitale non cambia *un solo trade*: cambia solo il divisore.
   Rifacendo il conto interno su 100.000 a parità di trade, il drawdown passa da 11,6% a
   **51,4%** (e da 8,9% a 35,9% sul futures). Il grosso dell'effetto ottico è tutto qui.
2. **Il cBot arriva ad aprile con 45.000 USD di cuscino in meno** (95.760 contro 140.545 a fine
   marzo), che è il divario di motore già scomposto sopra. Con lo stesso colpo da ~90-100k, chi
   ha meno cuscino finisce **sotto** la linea del capitale iniziale e chi ne ha di più resta
   sopra. Rimane un residuo reale — 66,8% contro 51,4% a parità di capitale — ma è un sesto
   dell'apparenza, non il tutto.

**Conseguenza operativa: il `maxDrawdown` del `backtest-summary` interno non è confrontabile con
quello di cTrader finché i due capitali sono diversi.** L'11% del summary è una proprietà del
capitale scelto, non del portafoglio. O si gira l'interno con `initialCapital` 100.000, o si
legge il drawdown **in denaro** (le due colonne qui sopra) e si ignorano le percentuali.

Una cautela sul confronto: questa curva è a **trade chiusi**. L'equity che disegna cTrader
include il flottante delle posizioni aperte, quindi il fondo reale del grafico è più profondo di
quello calcolato qui, e i due run interni chiudono con 5 e 2 posizioni ancora aperte il cui P&L
non è in `trades.json`.

## La taglia non ha errori di conversione — verificato simbolo per simbolo

Seconda domanda: se il divario venga da una taglia sbagliata. **No.** Test: per ogni trade
esterno, `denaro per punto ÷ (valore-punto interno × cambio del mese)`; 1,000 significa taglia
identica. Il cambio del mese è stimato sui trade in dollari (0,918 gen → 0,932 giu), FDAX escluso
perché è già in euro.

| simbolo | n | mediana | scarto di taglia |
|---|---|---|---|
| NQ | 642 | 1,0000 | −0,00% |
| ES | 149 | 1,0003 | +0,03% |
| GC | 147 | 0,9998 | −0,02% |
| YM | 73 | 1,0013 | +0,13% |
| NG | 61 | 1,0004 | +0,04% |
| FDAX | 53 | 1,0000 | 0,00% |
| CL | 18 | 1,0020 | +0,20% |
| PL | 17 | 1,0000 | 0,00% |
| **BP** | 10 | **0,9923** | **−0,77%** |

La dispersione p10–p90 di ±0,5% è il movimento del cambio *dentro* il mese, non taglia. L'unico
scostamento vero è **BP: il bot gira 0,62 lotti dove ne servirebbero 0,625** — 0,8% di taglia in
meno su 10 trade, cioè qualche decina di dollari. Non spiega niente del divario.

**C'è però un problema di valuta, e va nella direzione opposta a quella sospettata.** FDAX vale
25 **EUR** per punto e il sistema non ha un layer FX (è dichiarato in `InstrumentRegistry`): il
run interno somma quegli euro dentro un totale in dollari. Nella finestra sono 36.514 EUR contati
come dollari, cioè **il totale interno sottostima FDAX di circa 3.000 USD**, non lo gonfia.

Quindi l'impressione che l'interno "guadagni molto di più" è corretta come fatto — +218.339
contro +148.192 — ma la causa non è la taglia né il cambio: sono i **70.147 USD** già scomposti
sopra, e per metà sono trade che il cBot non prende affatto.

## Fragilità di feed — non è motore, ma cambia i risultati quanto il motore

- **Le stesse 75 strategie sullo stesso motore producono 846 trade senza controparte su ~1.200
  quando cambia solo l'archivio di barre.** Il singolo trade appaiato torna (−11 USD in media);
  è la popolazione che si riscrive.
- **Le strategie a barra giornaliera sono le più esposte**: 1440 fa −725 USD sul futures e
  **+23.914** sul CFD, con 69 e 72 trade. Il feed CFD ha anche **5 barre giornaliere in meno**
  su NQ (169 contro 174) e 3 su ES (171 contro 174), e produce 6 `skippedStaleCandle` — una per
  ciascuna delle sei strategie NQ a 1440. È l'unica anomalia di datasource dei due run.
- Strategie con i gate più instabili fra i due feed (scarto sul numero di segnali):
  `NG_TFU_001_240` 47%, `BP_TFM_002_15` 46%, `BP_TFM_001_60` 43%, `NG_TFU_002_240` 43%,
  `NQ_TFM_014_240` 41%, `NQ_TFU_008_240` 33%. Su queste il risultato di un run dice più
  sull'archivio che sulla strategia.

## Chiuso

- **Lo slippage sugli stop non esiste più.** 479 coppie stop→stop: **+12 USD a stop** in favore
  dell'interno, saldo +5.777 su −500.000 di stop pagati. Contro 180 USD/stop di compare-0007,
  74 di compare-0013 e −3.914 totali di compare-0014. Non è più una voce del divario.
- **Overweek: allineato.** Zero posizioni che attraversano il sabato, su tutte e tre le gambe.
  Il problema di compare-0014 (33 posizioni oltre il sabato lato cBot, 36.248 USD) **non si
  ripresenta**: `AllowOverweek = false` scende ora fino al bot.
- **Il flat del venerdì è allineato**: 226 chiusure interne e 205 esterne dopo le 20:00 di
  venerdì, e nessuna posizione oltre.
- **Il conteggio degli ingressi torna**: 1.234 interni contro 1.179 esterni, e 1.019 coppie
  (83%). Tolte le quattro strategie morte e BSW, lo scarto è di poche unità per strategia.
- **La taglia è 1:1 su tutti e nove i simboli**, verificata dal moltiplicatore implicito:
  ES 50×0,925, NQ 20×0,925, YM 5×0,923, GC 1×92,49, CL 10×92,48, NG 1×9.243, PL 0,5×92,39,
  FDAX 25×1,00. **Unica eccezione: BP gira a 0,62 lotti invece di 0,625** — 0,8% di taglia in
  meno, 10 trade, irrilevante nei numeri ma è un arrotondamento da correggere nel bot.
- **Il break-even non è più una voce**: 64 coppie, +2.716 in favore dell'interno. In
  compare-0012 valeva −22.226.
- **Nessuna posizione sovrapposta** su `(strategia, simbolo)` in nessuna delle tre gambe.
- **`ALL-76` è il catalogo intero, e la 76ª è `PTS_BTC_PCH_001_240`.** Il catalogo eseguibile ha
  esattamente 76 strategie (`StrategyFactory.GetRegisteredStrategies`); il run interno ne ha 75
  perché BTC non ha datafeed, e nemmeno la sessione esterna ha uno stream BTC. Non manca niente.
- **I due run interni sono puliti**: zero `skippedNoData`, zero `skippedNotEnoughCandles`, zero
  errori, `coversRequestedRange` true su tutti i 19 datasource di entrambi.

## Gli intent (aggiunti il 2026-09-01) — cosa dicono e cosa no

`all-76-bt-20240101-2302/` porta `signals.json` + `signals.jsonl` della sessione esterna, 2.845
record fusi. **Non possono chiudere i punti 1 e 2, e il codice lo dice:**
`TradingSessionService.PersistOnlyFilledIntents` è `true` di default, quindi l'artefatto contiene
i soli intent **riempiti**. La verifica: tutti e 2.845 hanno `status: "Filled"`, e i 1.179 di
apertura sono esattamente i 1.179 trade della finestra. Il commento sul campo documenta la stessa
trappola: nel confronto del 2026-08-28 aveva fatto sembrare che il server non emettesse 675
segnali. Si spegne con `PIOOTOO_PERSIST_ALL_INTENTS=1` all'avvio del server.

Quel che comunque si ricava:

- **Il ritardo di una barra di BSW è ora provato dal contratto dell'intent**, non dedotto dai
  trade: `orderType: Market` con `validFromUtc == expiresAtUtc == il timestamp della barra
  stessa`. Il segnale della barra 2024-02-26T10:00 è valido **sulla barra che parte alle 10:00**,
  cioè si riempie alle 10:15. Il server vede la barra quando chiude, e non c'è modo di entrare
  alla sua apertura.
- **Sono 13 gli intent BSW riempiti in tutto il run** (gen → set), 9 dei quali su
  `ES_BSW_003_15`. Contro i 34 trade interni della sola finestra. Ma quanti ne siano stati
  *emessi* resta invisibile.

**Ipotesi VBO, ora quantificata.** Il fabbisogno di storia dichiarato dalle strategie, letto dal
catalogo (`ITradingStrategy.RequiredCandles`):

| strategia | barre richieste | ≈ giorni di calendario | resto dello stream |
|---|---|---|---|
| `PTS_NQ_VBO_002_240` | **606** (ATR 100 su sessioni chiuse) | ~142 | le altre 6 su NQ 240 ne chiedono 36 |
| `PTS_FDAX_VBO_001_240` | **501** (ATR 500 sulle barre) | ~117 | le altre 3 su FDAX 240 ne chiedono 27-90 |
| `PTS_NQ_VBO_001_1440` | 6 | 9 | — |
| `PTS_FDAX_MAC_001_240` | 27 | 7 | — |

Sono **le due soglie più alte dell'intero portafoglio**, e non è un caso che i due datafeed
interni corrispondenti siano gli unici estesi all'indietro: NQ 240 parte dal 2023-01-02 (2.425
barre) e FDAX 240 dal 2023-03-06 (2.148), mentre ogni altro feed parte dal 2023-12-03. Lato
sessione il salto è **silenzioso** (`if (history.Count < strategy.RequiredCandles) continue;`) e
il bot deve caricare 606 barre H4 all'indietro dentro il backtester cTrader, con un tetto di 50
giri di `LoadMoreHistory`.

Copre 23 dei 24 trade VBO mancanti. **Non copre** `NQ_VBO_001_1440` (6 barre) né
`FDAX_MAC_001_240` (27): quelle due però hanno 1 e 6 trade interni, e zero esterni è compatibile
col caso. Resta un'ipotesi finché non c'è la misura — vedi il piano.

## Aperto

1. **Perché il cBot non riempie mai la famiglia VBO.** Ipotesi in testa: la storia non raggiunge
   mai `RequiredCandles` e il server salta in silenzio. Seconda ipotesi:
   `RejectWrongSideLevels` su un livello ancorato all'apertura di sessione. Le due si separano
   con la scheda di sessione (fix 1), non con altri run.
2. **Perché il cBot prende 9 BSW su 34.** La concorrenza è esclusa, e gli intent riempiti non
   bastano. Serve la scheda di sessione (fix 1) o un run con `PIOOTOO_PERSIST_ALL_INTENTS=1`.
3. **L'ingresso market "all'apertura della barra X" non è replicabile**: il bot arriva sempre una
   barra dopo. Decisione di modello, non bug — ma finché resta così, BSW e le altre famiglie a
   ingresso programmato non sono confrontabili.
4. **La risoluzione intrabarra dell'engine è ottimista** quando una barra copre stop e target.
   Il fix è una regola, non un parametro: sceglie il peggiore.
5. **Il trailing ha cambiato segno fra 0014 e 0015.** Non toccare il codice prima di aver capito
   quale dei due campioni mente.
6. **Rigirare l'interno con `initialCapital` 100.000**, o smettere di leggere il `maxDrawdown`
   in percentuale: finché i capitali sono 1.000.000 contro 100.000, l'11% del summary e il 64%
   di cTrader descrivono lo stesso drawdown.
7. **`stopLoss` e `takeProfit` restano `null` in tutti i trade interni** (segnalato in
   compare-0014, non ancora corretto): non si può misurare quanto un fill si discosti dal livello
   dichiarato senza ricostruirlo dal codice della strategia. Lato cBot sono presenti, ma sono
   **distanze in punti**, non livelli.

## Trappole di misura di questa cartella

- **Le tre gambe hanno archi diversi** (interni fino al 31/07, cBot fino al 02/07): senza tagliare
  alla finestra comune il divario si gonfia di ~100.000 USD di trade che una gamba sola ha visto.
- **La giornata del 02/07 è monca lato cBot** (2 trade contro una media di 9): esclusa.
- **Il cambio si muove nel run** (0,900 → 0,950). Qui la conversione è per punti
  (`punti × valore-punto interno`), che è insensibile: verificata contro il metodo per cambio
  implicito, 148.192 USD contro 136.687 EUR = 0,9224 medio.
- **La tolleranza di appaiamento deve scalare col timeframe.** A 2 ore fisse si perdono ~230
  coppie sulle strategie a 240 e 1440 minuti e il divario di motore sembra 27.871 invece di
  40.520. Qui: una barra della strategia, minimo 2 ore.
- **`LocalExit:StopLoss` copre stop + trailing + break-even insieme**, `LocalExit:Closed` copre
  time exit + max bars + flat + segnale opposto. Le tabelle qui sopra sono costruite su
  quell'aggregazione, mai uno a uno.
- **I 96 trade a durata nulla vanno esclusi da qualunque statistica di orario**, altrimenti
  inventano ritardi che non esistono.

## Piano di correzione (proposto il 2026-09-01, niente ancora applicato)

**Il fix 1 e' applicato** (2026-09-01, non ancora committato). I fix 2 e 3 restano proposte: il
patch e' in `proposte-fix/` (fuori da git, come gli altri artefatti pesanti) e si applica con
`git apply piootoo-repository/compare/compare-0015/proposte-fix/01-03-fix.patch --include=...`
piu' la copia del file di test.

### Fix 1 — `session-summary.json`, la scheda del run di sessione — **FATTO**

Accanto a `signals.json` e `trades.json`, un file piccolo che il filtro `PersistOnlyFilledIntents`
non puo' cancellare:

- per strategia: `requiredCandles`, **`everEvaluable`**, intent emessi / riempiti / rifiutati /
  annullati, primo e ultimo intent;
- per stream: **massimo storico** di barre accumulate (non il conteggio finale, che la potatura
  falsa) contro `requiredCandles`, e quante strategie non sono mai state valutate;
- `diagnostics` in testa, come nel `backtest-summary.json`: *"[storia] NQ 240m ha raggiunto al
  massimo N barre: 1 strategia non e' MAI stata valutata"*, *"[nessun fill] X: N intent emessi,
  zero riempiti"*.

Separa le tre cause che oggi sono indistinguibili: **mai valutata** / **emessa e rifiutata** /
**i gate non sono mai scattati**. E' esattamente la domanda VBO, ed e' anche la domanda BSW.
Costa quanto la scheda, si scrive solo sulla scrittura autorevole, e non richiede di rifare il run
con `PIOOTOO_PERSIST_ALL_INTENTS=1`.

Tocca: `SessionRunSummaryContracts.cs` (nuovo), `TradingJsonStore.WriteSessionSummary`,
`TradingSessionService` (`HistoryHighWater`, `NoteHistoryCoverage`, `BuildRunSummary`), piu'
`docs/decisioni.md` e la sezione «Diagnosticare una sessione» di `CLAUDE.md`.

**Costo per barra: zero.** La scheda vive solo su `WriteArtifactsFull`, cioe' sui cinque percorsi
fuori banda (cambio di stato, letture esplicite degli artefatti, promozione a backtest) che
**gia'** riscrivono `signals.json` e `trades.json` per intero. Non e' mai sul percorso per barra,
quindi l'invariante "i checkpoint non riscrivono l'artefatto intero" resta intatta.

Test: `TradingSessionsHttpTests.LaSchedaDelRunDenunciaLaStrategiaMaiValutabile` (uno stream con
una barra sola contro una strategia che ne chiede 576: `everEvaluable: false`,
`strategiesNeverEvaluated: 1`, diagnostica `[storia]`) e `LaSchedaContaGliIntentEmessiAnchePrimaDiUnFill`
(un intent emesso e non riempito e' contato, mentre `signals.json` sarebbe vuoto). **Verdi.** Gli 8
test rossi della stessa classe sono rossi anche su HEAD pulito: verificato con stash.

**Prossimo passo, ed e' quello che decide**: rifare il backtest del cBot sullo stesso arco e
leggere `session-summary.json`. Su `NQ 240` la riga dello stream dice se
`historyBarsHighWater` ha mai raggiunto 606; la riga di `PTS_NQ_VBO_002_240` dice se e' mai stata
valutabile e, se si', quanti intent ha emesso e quanti ne sono stati rifiutati. Le due ipotesi si
separano li'.

### Fix 2 — La barra d'ingresso non nasconde il minimo allo stop — *l'unica che cambia i numeri*

Sulla barra che esegue l'ingresso, lo **stop originale** si misura sull'estremo pieno della barra;
trailing e break-even restano su `postFill`, perche' nascono dall'estremo della barra in corso. E'
la stessa linea che `ProtectiveFillPrice` traccia gia' per il gap all'apertura, e la stessa che
CLAUDE.md dichiara fra stop originale e uscite dinamiche.

**Conseguenza da mettere in conto: il backtest interno rende di meno.** Di quanto non e'
misurabile su questa macchina — il workspace `all-in` non c'e' — ma sui soli tre casi noti di
`FDAX_SBO_001_240` sono 9.750 USD, e i 96 trade a durata nulla dicono che la barra d'ingresso
decide spesso.

Tocca: `PiootooTradingService.OriginalStopBreachedOnEntryBar` + due chiamate. ~30 righe, piu'
`EntryBarStopBeatsTargetTests` (4 test: long, short, il caso in cui la barra **non** copre lo stop
e il target deve restare valido, e le distanze dichiarate). **I 4 test passano.** I 3 test rossi
vicini (`RhlEngineParityTests` x2, `PtsPriceChannelTests` x1) sono rossi anche su HEAD pulito:
verificato con stash, non sono una regressione.

**Verifica vera**: A/B sullo stesso run, contando quante uscite cambiano e di quanto — come per il
passo del trailing nel 2026-08-28.

### Fix 3 — `stopLoss` / `takeProfit` sui trade interni

`OpenPosition.DeclaredStopLoss` (immutabile: `StopLoss` viene azzerato dal break-even),
`TradingResult.StopLossPoints` / `TakeProfitPoints`, e `ToPersistedTrade` che li scrive. Stessa
convenzione del cBot: **distanze in punti**, non livelli. Senza, lo scarto fra fill e livello
dichiarato non e' calcolabile dal lato interno. ~15 righe.

### Fix 4 — La barra di decisione degli ingressi market programmati — *decisione, non bug*

Un ingresso "market all'apertura della barra X" non e' replicabile live: il server vede la barra
X quando chiude. Due strade, e la scelta e' tua:

**(a)** la strategia dichiara **X−1** come barra di decisione, cosi' l'ingresso cade
sull'apertura di X da entrambi i motori. Cambia il porting di BSW e BIA rispetto alla ricerca, e
va misurato contro le liste di trade di riferimento.
**(b)** si accetta il ritardo e si scrive che BSW e BIA **non sono confrontabili** fra i due
motori, escludendole dalle scomposizioni.

Nessun codice proposto finche' non e' scelta.

### Fix 5 — Rendere rumoroso il silenzio del riscaldamento — *dipende dall'esito del fix 1*

Se la scheda conferma che NQ 240 non ha mai raggiunto 606 barre, tre leve, in ordine di invasivita':
alzare `MaxHistoryLoadAttempts` (oggi 50) nel cBot; far **fallire** l'avvio di uno stream con
storia insufficiente invece di stamparlo e proseguire; oppure ridurre `AtrLength` su
`NQ_VBO_002_240` — che pero' e' una decisione di ricerca, non di infrastruttura, perche' cambia la
strategia.

### Fuori piano, deliberatamente

- **Il trailing**: ha cambiato segno fra 0014 e 0015 a parita' di motore. Prima si misura quale
  campione mente, poi eventualmente si tocca.
- **BP a 0,62 lotti invece di 0,625**: vale qualche decina di dollari su 10 trade.
