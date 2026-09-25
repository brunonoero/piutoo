# Catalogo di idee PT6EXO

La serie `PT6EXO_*` raccoglie motori di **famiglie nuove**, separate per concetto da tutto cio' che
esiste. Lo scopo non e' migliorare le strategie che ci sono ma trovarne di **scorrelate**, da
raggruppare in piani diversi, uno per conto e per istanza di cBot. Le idee possono essere classiche,
strane o assurde: non si chiede loro un perche', si chiede di passare il metodo. Stato al 25/09/2026:
**scritti tutti i motori del catalogo** (XMK per la sola sweep), ognuno con contenitore `RC_{SIGLA}` e test;
nessuno e' ancora stato misurato su una griglia. Le scelte di dettaglio di ciascun motore stanno nel
commento della sua classe in `PT6EXOStrategies/Engines/`. Questo file si aggiorna man mano che una famiglia diventa
codice; quando la serie avra' classi, l'elenco classe → cella andra' in una `mappa-strategie-pt6exo.md`.

## Perche' una serie a parte

Tutti i motori di oggi — `Easy/Engines` (PT3B) e `PT5DAVStrategies/Engines` (PT5DAV) — leggono la sola
**geometria del prezzo** e ricadono in quattro famiglie: trend (TF, MAC, Aroon, TrendDeveloper),
breakout (PCH, SBO, VBO), inversione (RBB, RHL, LevelFader), bias di calendario (BiasWeekly,
BiasBarCount). Variare i parametri dentro queste famiglie da' strategie che guadagnano e perdono negli
stessi giorni. Le PT6EXO cambiano **cio' che il motore guarda**: l'orologio, il volume, la forma della
barra, il regime di volatilita', la statistica dei ritorni, o niente di sensato.

Regole della serie:

- Cartella `Piootoo.Strategies/PT6EXOStrategies/`, **motori propri** in `PT6EXOStrategies/Engines/`.
  Come per le PT5DAV, i motori condivisi non si toccano; le formule comuni si prendono da `EasyLib`.
- Nome `PT6EXO_{SIMBOLO}_{SIGLA}_{NNN}_{TF}`, progressivo per (simbolo, sigla), mai rinumerato.
- Contenitori di ricerca `RC_{SIGLA}` in `ResearchContainers/` con `IsResearchContainer = true`, come
  gli altri: il server non li accetta in un masterfilter. Le sigle qui sotto non collidono con quelle
  esistenti (TFM, TFU, PCH, SBO, BOS, VBO, LFD, LFH, RHL, RBM, RBU, BIA, BRT, BBO, MAC, BSW).
- Valgono **tutti** gli invarianti di `CLAUDE.md`: UTC, orari `TimeOnly` dentro `ZonedWindow` con il
  loro fuso, etichetta della barra sull'apertura, sessione della ricerca ancorata al simbolo, un fill
  per sessione per lato, `CrossedLevelPolicy` dichiarata dal segnale, overnight deciso dal piano.

## Il filtro: senza perche', non senza metodo

Un'idea assurda non e' un problema; lo e' il numero di prove. Con 15 famiglie per 30 celle per mille
combinazioni qualcosa di bellissimo esce per caso. Per questo ogni famiglia passa dal percorso normale
— `griglia-grossa`, `sweep-cella`, `lettura-risultati` con le sue soglie, fuori campione, costi veri
del paniere — e in piu' si confronta con il **controllo a ingresso casuale** (RAN, sotto) con le
stesse uscite: se la cella non batte con chiarezza la distribuzione dei semi, il guadagno viene dalle
uscite e non dall'idea. Le soglie numeriche restano in `lettura-risultati` e non si ripetono qui.

## Famiglie che stanno nell'infrastruttura attuale

Un simbolo, dati OHLCV, un timeframe (piu' eventuali timeframe dello stesso simbolo).

### GAP — gap di apertura di sessione

L'apertura della sessione della ricerca dista dalla chiusura della precedente piu' di `k × ATR`. Due
varianti: **fill** (contro il gap, target la chiusura di ieri) e **go** (a favore, stop sotto
l'apertura). Leve: `k`, verso, uscita a orario o al livello del gap, finestra di ingresso. Scorrelata
perche' esiste solo nei giorni con gap e non guarda il trend.
*Trappole:* i CFD quotano quasi 24 ore e su NQ/ES il gap all'ancoraggio e' piccolo; e' una famiglia
naturale per FDAX (finestra `SessionMask`) e per le commodity con pausa. Sul feed interno, serie
continua aggiustata, i salti di rollover sono gia' tolti; sul feed del broker no, e un gap di
rollover e' un gap finto: va riconosciuto dal calendario, non dal prezzo.

### IBS — forza interna della barra

`IBS = (C − L) / (H − L)` della barra chiusa. Sotto la soglia bassa si compra, sopra quella alta si
vende, uscita alla prima chiusura con IBS opposto o dopo N barre. Leve: soglie, filtro di trend lungo
(media a N giorni), N, timeframe (giornaliero e 4h per primi). Anomalia nota sugli indici azionari:
e' ritorno alla media su **una barra**, non su una banda, e ha molti trade con win rate alto.
*Trappole:* barra con `H = L` → nessun segnale, non divisione per zero.

**Scritto il 25/09/2026**: `InternalBarStrengthEngine` in `PT6EXOStrategies/Engines/`, contenitore
`RC_IBS`. Ingresso a mercato sulla barra dopo; filtro di trend `TrendBars` come media semplice delle
chiusure sul timeframe della strategia (long solo sopra, short solo sotto; 0 = spento). L'uscita
classica e' `ExitIbs`: il long chiude alla prima barra con IBS almeno `ExitIbs`, lo short alla prima
con IBS al massimo `1 − ExitIbs`, come segnale `ExitOnly` a mercato sulla barra dopo — lo stesso
percorso dell'incrocio inverso del MAC, gia' eseguito da motore interno e cBot. Il contenitore parte
con `ExitIbs = 0` (motore nudo: solo uscite comuni) e la griglia la accende. Leve: `LowThreshold`,
`HighThreshold`, `TrendBars`, `ExitIbs`, `Direction`. Soglie incoerenti fermano la valutazione con un
errore. Test in `InternalBarStrengthEngineTests`. Celle naturali: indici a 1440 e 240.

### RUN — serie di chiusure consecutive

N chiusure consecutive nella stessa direzione, oppure RSI a 2 periodi oltre una soglia estrema: si
entra contro, uscita alla prima chiusura contraria o a N barre. Leve: N, periodo e soglia RSI, filtro
di trend, uscita. Profilo opposto al trend following: vince spesso e poco, perde di rado e tanto;
per questo compone bene con le TF.

### FBO — falso breakout ("turtle soup")

Il prezzo rompe il massimo (minimo) degli ultimi N giorni e rientra sotto (sopra) entro X barre: si
entra **contro** la rottura, stop oltre l'estremo della rottura. Leve: N, X, profondita' minima della
rottura in ATR, validita' dell'ordine. E' la famiglia piu' promettente per la scorrelazione perche'
guadagna esattamente quando PCH/SBO/VBO perdono: e' **anti-correlata** per costruzione.

**Scritto il 25/09/2026**: `FailedBreakoutEngine` in `PT6EXOStrategies/Engines/`, contenitore
`RC_FBO`. Scelte fatte scrivendolo:

- **Il canale finisce prima della finestra di rottura.** Livello = estremo di `ChannelBars` barre che
  precedono le ultime `ReentryBars`; se il canale includesse la finestra si alzerebbe con la rottura
  e il rientro non esisterebbe mai. Canale a barre, come PCH, non a giornate.
- **Solo il primo rientro.** La barra di segnale chiude dentro, e o ha bucato il livello lei stessa
  (con `ReentryBars = 1` e' il caso classico, rottura e rientro nella stessa barra) o la barra prima
  aveva chiuso fuori. Senza, una rottura darebbe un segnale per ogni barra rimasta dentro.
- **Ingresso a mercato sulla barra dopo.** Un ordine al livello sarebbe gia' scavalcato per
  costruzione: il rientro e' proprio il prezzo che torna dall'altra parte.
- **Stop:** quello comune della base (denaro o ATR), oppure con `StopAtExtreme = 1` oltre l'estremo
  della rottura di `ExtremeBufferTicks`, misurato dalla chiusura della barra di segnale. Profondita'
  minima della rottura `MinBreakAtr` in ATR delle sessioni chiuse (0 = basta superare il livello).
- Leve: `ChannelBars`, `ReentryBars`, `MinBreakAtr`, `StopAtExtreme`, `ExtremeBufferTicks`,
  `Direction`, piu' le uscite comuni. Test in `FailedBreakoutEngineTests`. **Manca la griglia grossa**,
  e la domanda da farle e' doppia: se la cella ha un edge, e se il suo P&L giornaliero e' davvero
  anti-correlato con quello del PCH sulla stessa cella.

### NRX — compressione

La barra piu' stretta delle ultime N (NR4, NR7), una inside bar, o le Bollinger dentro i Keltner
(squeeze): OCO sugli estremi della barra di compressione, valido per M barre. Leve: tipo di
compressione, N, M, stop all'estremo opposto o in ATR. Gli ingressi nascono nei momenti morti, non
dopo un'espansione come nei VBO.
*Trappole:* OCO = due gambe, e un fill per sessione vale **per lato**; se la seconda gamba deve
morire al fill della prima va dichiarato, non dedotto.

### HOD — deriva oraria pura

Entra all'ora H, esce all'ora K, eventualmente solo in certi giorni o solo se il ritorno della notte
ha un segno. Leve: H, K, giorni, condizione. Il motore non guarda il livello del prezzo, solo
l'orologio: e' la famiglia piu' lontana dal catalogo attuale.
*Trappole:* H e K sono `TimeOnly` in una `ZonedWindow` con il fuso del mercato (New York per gli
indici USA, Berlino per FDAX), **mai** ore UTC: con l'ora legale una deriva "delle 15:30" in UTC
si sposta di un'ora due volte l'anno. Come `BiasWeeklyEngine`, emette sulla barra **prima** di quella
pianificata.

**Scritto il 25/09/2026**: `HourOfDayEngine` in `PT6EXOStrategies/Engines/`, contenitore `RC_HOD`.

- **Orologio dichiarato**: `ScheduleClock` = ricerca (Roma, per ogni simbolo) o borsa (il fuso di
  borsa del calendario, Chicago per NQ). L'ingresso cade sulla barra la cui **apertura** e' `EntryTime`
  su quell'orologio; la tenuta `HoldHours` si conta in ora locale, quindi l'uscita resta alla stessa
  ora anche la notte del cambio d'ora. Un'uscita di sessione piu' stretta (`IntradayOnly`, `ExitHour`)
  vince. `EntryHour` e' la leva intera della griglia ed e' l'unico punto in cui l'ora entra come numero.
- **La barra pianificata deve esistere**: in un giorno di sessione e dentro la finestra di
  negoziazione del simbolo (`SessionMask`). Il giorno da solo non basta: la domenica e' una sessione
  di NQ ma solo dalla sera, e un ordine su una barra che non c'e' aprirebbe alla prima vera, a un
  orario che nessuno ha dichiarato.
- **Il verso**: fisso da `Direction` (1 o 2; 0 senza momentum e' un errore), oppure con
  `MomentumMode` 1/2 dal segno delle ultime `MomentumBars` barre, a favore o contro — la variante
  "entra alle H solo se la notte e' salita". `SkipDay` sull'orologio dell'orario.
- Un orario che non e' l'apertura di una barra del timeframe (le 09:30 su barre orarie, le 03:00 su
  barre a 4 ore ancorate all'01:00) non scatta mai: la griglia deve usare ore coerenti con la cella.
- Test in `HourOfDayEngineTests`: inverno ed estate a Roma, ora di Chicago, sabato e domenica
  mattina, uscita di sessione piu' stretta, momentum nei due versi, giorno escluso.

### CAL — anomalie di calendario

Fine e inizio mese (turn-of-month: ultimi `a` giorni di borsa e primi `b`), settimana della scadenza
trimestrale (terzo venerdi'), vigilia dei festivi del calendario di mercato, giorno della settimana.
Leve: offset in giorni di borsa, durata, verso. Parente di BiasWeekly ma con un ciclo diverso.
*Trappole:* "giorno di borsa" si conta sul calendario del simbolo (`market-calendars.json`), non sui
giorni di calendario; i festivi vengono dal calendario, mai da una lista nel motore.

### VLM — anomalia di volume (declassata)

Volume della barra oltre `k` volte la mediana delle ultime N: si segue la direzione della barra
oppure la si sfuma. **Sui CFD non porta informazione propria**, per tre motivi:

- Il volume di cTrader e' **tick volume**: quante volte il broker ha aggiornato la quotazione, non
  quanti contratti sono passati di mano. Dipende dal fornitore di liquidita' del broker, da come
  filtra e diluisce le quotazioni, e cambia quando il broker cambia infrastruttura.
- Quel poco che dice e' gia' detto dal prezzo: le quotazioni si aggiornano di piu' quando il
  prezzo si muove di piu', quindi il tick volume e' in gran parte un doppione dell'ampiezza della
  barra e della stagionalita' oraria. Un motore sul tick volume finirebbe correlato con NRX, REG e
  HOD, cioe' l'opposto dello scopo della serie.
- Il volume **vero** c'e' solo nel feed interno (future del vendor), che in live non abbiamo: una
  cella ricercata li' non e' eseguibile sul CFD.

Resta nel catalogo come idea **bizzarra**, a ultima priorita': solo con la soglia relativa (mai il
livello assoluto), solo sul feed del broker che la esegue, e con un solo criterio di ammissione —
deve battere la stessa cella con il volume sostituito dall'ampiezza della barra. Se non la batte, il
volume era un travestimento della volatilita'. Il volume vero diventa utilizzabile solo con XMK,
facendolo arrivare da un feed del future accanto al CFD.

### REG — regime di volatilita'

Percentile dell'ATR su una finestra lunga (un anno di barre). Sotto il percentile basso il motore
segue il trend (rottura del canale), sopra quello alto va contro (banda). E' un meta-motore: il
regime sceglie la logica. Leve: finestra, i due percentili, i due sotto-motori, cosa fare in mezzo.
*Trappole:* un anno di barre e' il `RequiredCandles`; in sessione `ExternalBroker` il riscaldamento
deve portarlo, altrimenti `everEvaluable` resta falso per sempre.

### CDL — candela di rifiuto su un livello

Pin bar (ombra ≥ r volte il corpo) o engulfing che toccano un livello — massimo o minimo di ieri,
apertura di sessione, estremo settimanale — e chiudono dalla parte opposta. Leve: r, tipo di candela,
livello, tolleranza del tocco in ATR. Il trigger e' la forma, non la distanza come nei LevelFader.

### MTF — timeframe in disaccordo

Trend del timeframe alto in una direzione, eccesso contrario sul timeframe basso (per esempio
giornaliero sopra la media a 50, 15 minuti con RSI ipervenduto): si compra il ritracciamento. Leve:
coppia di timeframe, definizione di trend, soglia dell'eccesso. `IMultiTimeframeTradingStrategy`
esiste gia' (la usa `AroonCrossoverEngine`).
*Da verificare prima:* che la sessione live e il descriptor del cBot servano davvero i timeframe
aggiuntivi come fa il backtest.

## Le bizzarre

Nessuna ipotesi economica, oppure una debole. Il controllo RAN per loro non e' facoltativo.

### RNM — magnete dei numeri tondi

Il prezzo e' entro `d` punti da un multiplo di `M` (100, 500, 1000 punti): si entra verso il livello
(magnete) oppure lo si sfuma al tocco (muro). Meno assurda di quanto sembri: su quei livelli si
accumulano ordini.
*Trappole:* ha senso solo sul **prezzo vero**. Il feed interno e' la serie continua aggiustata, dove
i tondi non sono tondi: RNM si ricerca **solo** sul feed di un broker.

### FIB — conte di Fibonacci nel tempo

Si individua l'ultimo swing (pivot di k barre per lato) e si entra contro il movimento esattamente 8,
13, 21 o 34 barre dopo. Leve: k, conta, verso, uscita.
*Trappole:* "barre dopo" si conta sulle barre stampate, non sui tick dell'orologio (stessa regola di
`MaxBarsInPosition`).

### LUN — fasi lunari

Long dalla luna nuova alla piena e short dalla piena alla nuova, o il contrario, o solo nei k giorni
attorno a una fase. La fase e' deterministica: mese sinodico di 29,530588853 giorni dalla luna nuova
di riferimento del 06/01/2000 18:14 UTC. Nessun legame con nulla del catalogo, quindi scorrelata per
definizione; se funziona fuori campione su piu' simboli e' una curiosita' da tenere, se funziona su
uno solo e' rumore.

### MOD — modulo del tempo

Entra solo sulle barre il cui indice temporale e' multiplo di N, nella direzione della barra
precedente. L'indice si calcola dal **tempo** (`minuti dall'epoca / timeframe`), non contando le barre
del feed: contare le barre darebbe risultati diversi fra feed interno, feed del broker e sessione
live, che hanno buchi diversi.

### DXH — griglia giorno × ora

Per ogni coppia (giorno della settimana, ora locale del mercato) la griglia sceglie long, short o
niente, con uscita a K ore. E' stagionalita' pura ed e' la famiglia con il **rischio di adattamento
piu' alto** del catalogo (centinaia di gradi di liberta'): la si prova per ultima, con uno split fuori
campione severo e solo sulle celle in cui HOD ha gia' mostrato qualcosa.

## Il controllo: RAN — ingresso casuale con seme fisso

Non e' una strategia da piano ma lo strumento di verifica di tutta la serie, e anche di PT3B e PT5DAV.
Stesse uscite (stop, target, trailing, tenuta) di una strategia vera, ingresso a caso con probabilita'
`p` per barra e verso a caso. Su 100 semi da' una distribuzione: una strategia vale per il suo
**segnale** solo se sta chiaramente sopra quella distribuzione.
*Trappole:* il caso deve essere **riproducibile senza stato**, mai `System.Random` con stato,
altrimenti backtest e sessione live divergono al primo riavvio. Si tara `p` in modo che il numero di
trade sia quello della strategia confrontata.

**Scritto il 25/09/2026**: `RandomEntryEngine` in `PT6EXOStrategies/Engines/`, contenitore `RC_RAN`.
L'estrazione e' SplitMix64 su (seme, simbolo con FNV-1a, timeframe, istante di apertura della barra):
il nome della strategia non entra, cosi' contenitore e classe con gli stessi parametri scelgono le
stesse barre, e il simbolo non passa da `string.GetHashCode`, che .NET randomizza per processo. Leve
proprie `Seed`, `EntryProbability` (per barra **flat**: in posizione non nasce nulla), `Direction`
(0 a caso, 1 long, 2 short); ingresso a mercato sulla barra dopo; le uscite sono quelle comuni della
base (stop e target in denaro o in ATR, `MaxBars`, `IntradayOnly`, `ExitHour`, finestra). Il valore
di un'estrazione e' fissato in `RandomEntryEngineTests.TheDrawIsStableAcrossProcesses`: se cambia,
i run di controllo archiviati non si ripetono piu'. **Manca lo studio** che, data una strategia, ne
copia le uscite, tara `p` sul suo numero di trade, gira i cento semi e dice dove cade la strategia
nella distribuzione.

## Serve lavoro sull'infrastruttura

### XMK — tra mercati diversi

NQ entra quando GC ha rotto il proprio canale; divergenza fra FDAX e NQ nella stessa ora; rapporto
oro/argento o petrolio/gas oltre una banda. E' la fonte di scorrelazione piu' vera, ma oggi
`IMultiTimeframeTradingStrategy` da' solo altri timeframe **dello stesso simbolo**. Serve un contratto
multi-simbolo in quattro punti — backtest (cursori per simbolo), sessione (history per simbolo),
descriptor e cBot (sottoscrizione ai simboli aggiuntivi) — e una regola esplicita per i buchi: se il
simbolo di riferimento non ha stampato la barra, la strategia non valuta, non usa la barra vecchia.
Si apre solo dopo che le famiglie sopra hanno dato qualcosa.

**Scritto il 25/09/2026, per la sola ricerca**: `CrossMarketEngine` in `PT6EXOStrategies/Engines/`,
contenitore `RC_XMK`, sul contratto nuovo `IMultiSymbolTradingStrategy` (`Piootoo.Shared/Interfaces`):
la strategia dichiara `ReferenceSymbols`, la richiesta di valutazione porta `ReferenceOhlcv` per
simbolo, sullo stesso timeframe. Tre modi: 0 anticipo (il riferimento chiude oltre il proprio canale →
stesso verso), 1 divergenza (il riferimento si muove di `MoveThresholdPct` per cento, il simbolo operato
al contrario → si segue il riferimento), 2 rapporto (z-score del log del rapporto oltre `ZEntry`). Le
barre si accoppiano per istante di apertura; se il riferimento non ha la barra di segnale la strategia
non valuta.

- **Dove arrivano le serie**: solo nella sweep, `SweepRunner(serie, riferimenti)`, che confronta i
  simboli normalizzati e ferma il run se un riferimento dichiarato non e' caricato. La matrice e la
  coda non le passano ancora: per una cella XMK serve uno studio che carichi le due serie.
- **Dove non arrivano**: backtest completo, sessione live, cBot. Li' una strategia XMK si ferma con un
  errore (`InvalidOperationException`) invece di tacere; portarcela e' il lavoro vero — cursori per
  simbolo nel backtest, storia per simbolo nella sessione, sottoscrizione ai simboli nel descriptor e
  nel cBot, con la versione minore del contratto — e si fa solo se una cella XMK risponde.
- Test in `CrossMarketEngineTests`, compresa la sweep con un feed in memoria.
- **Primo studio**: `NqGcCrossMarketStudy`, NQ operato guardando GC, i tre modi a 60 e 240 minuti,
  leva `LookbackBars` 10/20/50 e la griglia di rischio della matrice FTMO. Usa la griglia grossa con
  `CoarseGridSpec.ReferenceSymbols`, che carica le serie di riferimento dallo stesso feed e le passa ai
  runner. CSV in `ricerca/matrice/nq-{tf}-xmk-gc-{modo}.csv`. In coda (`xmk-nq-gc-griglie`) dopo il
  lotto PT6EXO.

## Dalle strategie ai piani

L'idea diversa non basta, la scorrelazione **si misura**:

1. Sul **P&L giornaliero** del run ai costi veri di ogni finalista, non sulla sovrapposizione dei
   trade.
2. Con piu' peso alla **correlazione nelle code**: strategie scorrelate in media perdono insieme nei
   giorni di crash. Si calcola anche sui soli giorni peggiori del paniere.
3. Il piano si costruisce **in modo goloso**: si parte dalla strategia con il punteggio migliore, si
   aggiunge quella con la correlazione massima piu' bassa verso le gia' scelte, con un tetto per
   simbolo e per famiglia, finche' il drawdown del piano rispetta le regole del conto (perdita
   giornaliera e massima del prop firm).
4. Un piano per conto, un'istanza di cBot per piano. Piani con famiglie diverse proteggono anche dal
   rischio di modello: se una famiglia smette di funzionare muore un conto, non tutti.

Lo strumento che lo fa non esiste ancora (nome di lavoro `piootoo-plan-builder`): legge i
`trades.json` di un run di catalogo e propone i piani con la loro matrice di correlazione.

## Ordine di lavoro proposto

**In coda dal 25/09/2026**: lo studio RAN sulla 002 (`Pt3b002RandomControlStudy`) e poi, davanti alla
matrice vecchia in `ricerca/coda.json`, un primo lotto di 18 celle FTMO nella matrice delle griglie
grosse (`CoarseGridMatrixTests`, split 2022-01 → 2025-01 → 2026-09): FBO, IBS, RUN, NRX su FDAX e NQ
a 240 e 60 minuti, HOD su FDAX e NQ a 60. Leve della matrice: FBO `ChannelBars` 5/10/20/50 con
rientro nella stessa barra; IBS soglia simmetrica 0,10/0,20/0,30 con uscita a IBS 0,5; RUN 2-5
chiusure con uscita alla prima contraria; NRX NR-4/7/10 con stop validi una barra; HOD ingresso alle
8, 9, 10, 14, 15, 16 di Roma, quattro ore, long e short separati (senza momentum non c'e' un
"entrambi"). FDAX e NQ perche' li' i motori vecchi sono gia' misurati e la correlazione si calcola.

1. **RAN**, perche' e' il metro di tutto il resto, anche delle serie esistenti.
2. **FBO** (anti-correlata con i breakout), **IBS** (profilo opposto), **HOD** (non guarda il prezzo).
3. RUN, NRX, CAL, GAP, CDL.
4. REG, MTF (dopo la verifica del percorso live).
5. Le bizzarre: RNM (feed del broker), LUN, FIB, MOD; VLM e DXH per ultime.
6. `piootoo-plan-builder`, appena ci sono finaliste di almeno tre famiglie.
7. XMK, se ne vale ancora la pena.

| sigla | famiglia | guarda | stato |
|---|---|---|---|
| GAP | gap di apertura | apertura vs chiusura precedente | `SessionGapEngine`, `RC_GAP`; manca la griglia |
| IBS | forza interna della barra | posizione della chiusura nel range | `InternalBarStrengthEngine`, `RC_IBS`; manca la griglia |
| RUN | serie di chiusure / RSI(2) | sequenza dei ritorni | `RunOfClosesEngine`, `RC_RUN`; manca la griglia |
| FBO | falso breakout | rottura fallita | `FailedBreakoutEngine`, `RC_FBO`; manca la griglia |
| NRX | compressione | ampiezza delle barre | `CompressionEngine`, `RC_NRX`; manca la griglia |
| HOD | deriva oraria | orologio | `HourOfDayEngine`, `RC_HOD`; manca la griglia |
| CAL | anomalie di calendario | calendario di borsa | `CalendarAnomalyEngine`, `RC_CAL`; il modo "vigilia di festivo" non scatta finche' i calendari non dichiarano festivi |
| VLM | anomalia di volume | tick volume del CFD | declassata; `VolumeSpikeEngine`, `RC_VLM`, con la leva di controllo `ActivitySource` (volume o ampiezza) |
| REG | regime di volatilita' | percentile dell'ATR | `VolatilityRegimeEngine`, `RC_REG`; manca la griglia |
| CDL | candela di rifiuto | forma della barra su un livello | `RejectionCandleEngine`, `RC_CDL`; manca la griglia |
| MTF | timeframe in disaccordo | due timeframe | `TimeframeDisagreementEngine`, `RC_MTF`; **solo backtest**: sessione live, sweep e cBot non passano la serie alta, e il backtest ne passa 8 barre per il giornaliero |
| RNM | numeri tondi | prezzo vero | `RoundNumberEngine`, `RC_RNM`; solo feed del broker |
| FIB | conte di Fibonacci | tempo dallo swing | `FibonacciTimeEngine`, `RC_FIB`; manca la griglia |
| LUN | fasi lunari | calendario lunare | `LunarPhaseEngine`, `RC_LUN`; manca la griglia |
| MOD | modulo del tempo | indice temporale | `TimeModuloEngine`, `RC_MOD`; manca la griglia |
| DXH | griglia giorno × ora | orologio | `DayHourGridEngine`, `RC_DXH`; per ultima |
| RAN | ingresso casuale (controllo) | niente | motore e contenitore `RC_RAN` scritti; manca lo studio dei semi |
| XMK | tra mercati | altri simboli | `CrossMarketEngine`, `RC_XMK`; **solo sweep**: backtest completo, live e cBot si fermano con un errore |

## Riferimenti codice

- `Piootoo.Strategies/PT6EXOStrategies/Engines/RandomEntryEngine.cs`, `ResearchContainers/ResearchContainers.cs` (`RC_RAN`)
- `Piootoo.Strategies/PT6EXOStrategies/Engines/FailedBreakoutEngine.cs` (`RC_FBO`), `InternalBarStrengthEngine.cs` (`RC_IBS`), `HourOfDayEngine.cs` (`RC_HOD`)
- `Piootoo.Strategies.Tests/RandomEntryEngineTests.cs`, `FailedBreakoutEngineTests.cs`, `InternalBarStrengthEngineTests.cs`, `HourOfDayEngineTests.cs`; serie e sigle in `PtsNamingConventionTests`
- `Piootoo.Strategies.Tests/Pt3b002RandomControlStudy.cs` (primo uso di RAN)
- `Piootoo.Strategies/ResearchContainers/ResearchContainers.cs` (convenzione `RC_{SIGLA}`)
- `Piootoo.Strategies/Easy/EasyLib.cs` (sessione, pattern, formule comuni)
- `Piootoo.Strategies/Easy/Engines/BiasWeeklyEngine.cs` (motore a mercato programmato: modello per HOD e CAL)
- `Piootoo.Strategies/Easy/Engines/AroonCrossoverEngine.cs`, `Piootoo.Shared/Interfaces/IMultiTimeframeTradingStrategy.cs` (MTF)
- `.claude/skills/griglia-grossa`, `sweep-cella`, `lettura-risultati`, `promuovi-finalista`
