# Lavori in corso

Questo file è volutamente deperibile: quando una voce è chiusa si cancella da qui, e la motivazione
della scelta resta in [`decisioni.md`](decisioni.md). Se una sezione qui contraddice il codice, ha
ragione il codice.

---

# ⇦ RIPRENDERE DA QUI: le tre decisioni del 23/09, tutte prese e in corsa

Le tre cose lasciate aperte la sera del 22/09 (in fondo alla sezione «sei griglie grosse») sono
state fatte tutte e tre la mattina del 23/09. Voci in `decisioni.md` 2026-09-23.

1. **`SessionExitTime` vale per ogni motore.** Portata su `EasyEngineBase.WithSessionExit`, un
   punto solo che risolve la deadline di fine sessione per i sette motori con uscita di sessione, con
   lo scarto dell'ingresso che nascerebbe dopo la propria ora. Test sul sorgente
   (`EveryEngineResolvesTheSessionExitInOnePlace`) e comportamentali sul TF, gli stessi del PC. Il
   default non cambia. La griglia TF 4h del 22/09 **non** è stata rilanciata: il suo stesso rapporto
   diceva che il prossimo giro non doveva essere quello.
2. **La 003 su FDAX, cercata dentro la regione.** `piootoo-sweep --fix "ChannelBars=20;Direction=1;
   ExitHour=21"` (`SweepSpace.Fix`): la sweep sceglie pattern, orari e stop **dentro** la regione
   che la griglia lunga ha indicato, con lo stesso split della griglia (2021-01-01) e i costi del
   paniere (spread per ora peggiore ICS/FTMO, swap peggiore, 19,23 per lato). Riferimento nudo della
   regione: stop 2.500, target 9.000 fa **+96.324 IS / +102.480 OOS** su 577/528 trade, PF 1,22/1,20,
   avg trade 167/194 contro soglie 266/365. Una finalista deve battere **questo**, non lo zero. Cella
   `fdax-4h-003` in `tools/sweep-paniere.ps1`; resoconto in
   `ricerca/fdax-4h-003-ics-costo-peggiore-per-ora-peggior-tratto-regione.md`.
   **Esito, 13:25** (`ricerca/fdax-4h-regione-sweep.md`): una finalista passa (130 trade, +47.113 IS,
   +26.938 OOS, tenuta 51%) ma **non batte la regione nuda** sugli stessi costi (+75.815 / +87.567):
   i pattern alzano il netto su drawdown in campione (3,37 → 5,71) e lo abbassano fuori (3,51 →
   2,90). Average trade 362 IS (sopra soglia) e 190 OOS (metà). Passa solo la variante con trailing
   2.000, terza in campione. **Nessuna classe 004**: si aspetta la misura A.
3. **Il trend following, seconda prova, tre cose cambiate insieme**: unmirrored, 15 minuti, pattern
   cercati dalla sweep (`SweepSpaces.TrendFollowingUnmirrored`, `--engine TFU`, fasi pattern
   spezzate). Contenitore `PT3B_NQ_TFU_001_15`. 2022-01 → 2025-01 → 2026-09, feed ICS, utile medio
   minimo 120. Il riferimento è `PTS_NQ_TFU_003_15`, la sola TF coerente del catalogo ai costi veri
   (IS +16.578 su 195 trade, OOS +41.872 su 128, 4/4). Cella `nq-15m-tfu`; resoconto in
   `ricerca/nq-15m-tfu-ics-costo-peggiore-per-ora-peggior-tratto.md`.
   **Esito, 15:09** (`ricerca/nq-15m-tfu-sweep.md`): **zero ammissibili in ogni fase**, la sweep non è
   mai partita. La configurazione iniziale fa −21.923 in campione e −67.346 fuori, e nessun passo
   singolo arriva a PF 1,25 e 120 per trade. Non è un verdetto sul motore. In coda dopo le griglie la
   stessa cella senza quelle due soglie nelle fasi (`tools/coda-nq-tfu-senza-soglie-2026-09-23.ps1`).

Le due ricerche girano **in sequenza** da `tools/lancia-ricerche-2026-09-23.ps1`, log in
`ricerca/lancio-2026-09-23.log`. Quando finiscono: leggere i due resoconti con i criteri di
`metodo-unger` §8.2 (avg trade ≥ 266/365 su FDAX, ≥ 120 su NQ 15; UngerFit ≥ 1; guadagno annuo su DD
≥ 2), confrontarli con i riferimenti qui sopra, scrivere il rapporto e — solo se una finalista
batte il riferimento e regge — la classe accanto al contenitore.

## La coda di griglie che segue (`tools/coda-griglie-2026-09-23.ps1`, log `ricerca/coda-griglie-2026-09-23.log`)

Parte da sola quando le due sweep hanno finito, in sequenza. L'ordine è dal segnale più forte al
più debole, e **gli altri simboli non ci sono**: GC, NQ 4h, CL e BP con il Price Channel nudo hanno
già detto no su dodici anni, e rifarli con un motore che su FDAX non ha edge è tempo perso. Ci
tornano solo se B o C dicono qualcosa.

- **A. I filtri della 002 sulla regione FDAX 4h nuda** — cinque misure con `--params`
  (`ricerca/fdax-4h-regione-filtri-002.log`): nuda, pattern 44, finestra 03-18, i due insieme, tutti
  i gate della 002 con la finestra. È il lavoro «a mano» come sul DAX, con ipotesi a priori: i filtri
  sono validati su dodici anni su un'altra cella, non scelti guardando questa. Da confrontare con la
  regione nuda (+96.324 / +102.480) e con la finalista della sweep sulla stessa regione.
- **B. Altri motori sulla cella FDAX 4h** (`FdaxEnginesCoarseGridTests`, tre contenitori nuovi
  `PT3B_FDAX_SBO_001_240`, `PT3B_FDAX_RBM_001_240`, `PT3B_FDAX_VBO_001_240`): breakout di sessione
  (leva `Sessions`, 200 combinazioni), reversal Bollinger mirrored (leva `BbLength`, 150), volatility
  breakout dal range d1 (leva k in decimi, con direzione, 600). Stessi stop, target, uscite, periodo e
  costi della griglia Price Channel: cambia solo il motore. La domanda: l'edge è del mercato o del
  Price Channel? CSV `fdax-4h-{bo,rbm,vbo}-griglia-grossa.csv`.
- **C. Il Price Channel su FDAX a 1 ora** (`Fdax1hCoarseGridTests`, contenitore
  `PT3B_FDAX_PCH_003_60`, 450 combinazioni): l'edge sopravvive a una barra quattro volte più fitta?
  CSV `fdax-1h-griglia-grossa-lunga.csv`. **Il 15 minuti non si può ancora fare**: l'archivio ICS
  ha `@FDAX_1`, `_60` e `_240`, il 15 va derivato dal minuto con `rebuild-from-minutes` (server
  acceso, un piano che dichiari FDAX 15).

**Sul numero 003.** Il contenitore a 1 ora porta `003` perché la convenzione vuole i progressivi
contigui per (serie, simbolo, motore) senza distinguere il timeframe. La finalista della regione FDAX
4h, quella chiamata «la 003» nei documenti del 22/09, nascerà quindi come **`004`**.

`CoarseGridSpec.FirstLeverDivisor` (nuovo) permette una prima leva decimale: il k del volatility
breakout viaggia in decimi.

---

# La ricerca PT3B, stato al 21/09/2026

Due giorni di lavoro hanno prodotto un **ottimizzatore interno** funzionante e una prima strategia
(`PT3B_FDAX_PCH_001_240`). Il codice è committato e verde: 1.035 test. Quello che manca sono due
correzioni note e una decisione sul periodo di ricerca.

Come funziona il tutto sta in [`domini/ricerca-parametri.md`](domini/ricerca-parametri.md); i costi
in [`domini/spread-e-costo-di-transazione.md`](domini/spread-e-costo-di-transazione.md); il perché
delle scelte in `decisioni.md` alle voci del 20 e 21 settembre.

## Mattina del 22/09: prima di toccare qualunque cosa, una rilettura senza codice

Il primo lavoro della mattina non è produrre conclusioni, è **rileggere quelle di ieri**, in una
sessione separata e **senza aprire il codice**. Un lettore fresco non ha i bias di chi ha passato
la giornata dentro i run: se una strada è stata presa per abitudine, la vede lui. Due documenti:

1. **`compare/compare-0048/esito.md`** — il nodo dei tre run cTrader. Domande da fargli, non da
   dargli per scontate: la partizione 0%/100% sul minuto tondo basta davvero a dire "tick contro
   barre", o c'è un'altra spiegazione compatibile? I 13 stop confrontati col feed ICS sono scelti
   come i più slittati: il meccanismo vale anche sugli altri 107? Il test t in §6 bis usa i netti
   per trade come indipendenti: lo sono? E la conclusione "più celle, non più storia" è una
   deduzione o una preferenza?
2. **Il resoconto della ricerca FDAX 4h al minuto** (`ricerca/fdax-4h-pc-tutto-al-minuto.md`,
   partita la sera del 21/09). La domanda che le era stata assegnata è "batte la 002 fuori
   campione?": prima di rispondere, controllare che il confronto sia sullo stesso split, gli stessi
   costi (ICS+FTMO, il peggiore) e lo stesso orologio, altrimenti la risposta non vale.

Il lettore scrive le obiezioni in fondo ai due file, sotto un titolo «Rilettura del 22/09», e
**non corregge niente**: ciò che regge resta, ciò che non regge torna qui come voce aperta. Solo
dopo si riprende dalla sezione seguente.

**Fatta.** Le obiezioni stanno in coda a `compare-0048/esito.md` (R1-R8),
`ricerca/fdax-4h-pc-tutto-al-minuto.md` (F1-F7) e `ricerca/nq-4h-pc-tutto-al-minuto.md` (N1-N3).
Quello che non regge, in ordine di peso:

1. **I numeri di §6 di compare-0048 mescolano due run a tick** (R1): +34% in euro è il rilancio,
   +57% in dollari e il drawdown $62.614 sono il run `...0935`. Il paragrafo «resta aperto il
   numero» qui sotto ha ereditato la cucitura. E due run a tick dello stesso piano che distano
   ≈$18.000 sono un fatto da spiegare prima di dire che la modalità dati spiega tutto.
2. **Il motore interno è più pessimista del run a tick di ≈$48.000 sul lordo** su 2022-2026 a
   parità di trade (R4): mai confrontato, ed è il confronto che conta.
3. **Il fallimento della sweep al minuto è attribuito a una causa fra quattro cambiate insieme**
   (F4): serve il run controllato vecchio obiettivo + minuto + stesso split prima di «si tara a
   mano».
4. **`MinTrades = 300` non cambia il gradiente del criterio** (F5, N1): previsione da verificare
   sulla sweep GC, convergenza su ~300 trade esatti. E il taglio lo fanno i pattern con lo stop al
   seme, non la fase di rischio (F6).
5. **`ExitHour = 21` della 002: a priori o guardando la validazione?** (F1) Decide se +52.310 è
   fuori campione. E manca la 001 sullo stesso split (F2).
6. Minori: il salto ICS del 23/03 chiamato artefatto senza confronto col vendor (R7); tick dal
   2014 su cTrader da verificare prima del run (R8); `MinTrades` 30 o 50 e `MinProfitFactor` in
   fase trigger (F7); finaliste duplicate per parametri inerti (F3, N2).

## 22/09, dopo la rilettura: stop e target in ATR — scritto, da misurare

`StopAtr`/`TargetAtr` sui motori (`EasyEngineBase.StopAtrMultiplier`, 0 = denaro fisso di sempre;
`decisioni.md` 2026-09-22, `ricerca-parametri.md` §"Stop e target in ATR"). Compilato, tre test
verdi (`AtrStopTests`), **nessuna griglia ancora lanciata**. Il primo uso è la griglia grossa NQ 15
con `CoarseGridSpec.AtrStops = true` — stop {1, 1.5, 2, 3} × target {0, 2, 3, 5} ATR — a confronto
con la stessa griglia in dollari: la domanda è se la cella in ATR regge fuori campione dove quella
in dollari cambiava stop fra campione e validazione. Poi la 002 con lo stesso stop espresso in
ATR (5000 $ ≈ 200 punti: quanti ATR erano nel 2022 e quanti nel 2026?). Non entra in nessun piano
finché non è misurata.

**NQ 4h al minuto con il criterio 2 (250 trade, 25 per tratto, 10 perdite), consegnata alle 13:20
del 22/09** (`ricerca/nq-4h-pc-tutto-al-minuto-criterio-2.md`, 344 minuti): **nessuna delle dieci
sopravvive**, e la previsione F5/N1 è confermata alla lettera. Le fasi convergono sul pavimento
nuovo — 260, 257, 252, **250, 250, 250, 250** trade — cioè il criterio ha cambiato *dove* stringe,
non *se* stringe. La migliore in campione (97.255 su 250 trade, punteggio 6,31, overnight con
finestra 10→05 e pattern 16/1/47/−40) fa **−23.667** fuori campione, 1 finestra su 4; le altre nove
sono la stessa configurazione con trailing e breakeven diversi (tenuta 2-12%). Le fasi *stop e
target* e *uscita di sessione* non hanno spostato nulla: 1.352 combinazioni e lo stesso seme.
Conclusione che chiude la questione sweep su NQ 4h: il PC a 4 ore su NQ non regge fuori campione
con nessun criterio provato (vecchio, 2, griglia grossa), e alzare il pavimento sposta solo il
punto di convergenza. Il prossimo tentativo su NQ è la griglia in ATR a 15 minuti, non un'altra
sweep a 4 ore.

## Sera del 21/09: l'orologio veloce era rotto, la 002 è il candidato, la ricerca gira al minuto

- **L'orologio veloce della sweep ordinava rumore** — Spearman **0,021** sul punteggio fra veloce e
  minuto, su 22 configurazioni vere (`SweepFastClockRankingTests`). Spento: ogni fase gira al
  minuto, ~3 ore per FDAX 4h. Voce in `decisioni.md` 2026-09-21 e §"Un orologio solo" di
  `domini/ricerca-parametri.md`. **Ripetuto su NQ**: 240m vs 1m = 0,623 (non basta), **15m vs
  1m = 0,980** (basta). Quindi ES, BP ed EC — che il vendor ha a 15 minuti dal 2006-08 — si
  cercano con l'orologio a 15m; GC e CL (solo 30m) e gli altri restano fermi finche' non si
  compra il minuto.
- **La ricerca FDAX 4h interamente al minuto ha consegnato alle 2:30 del 22/09** — 177 minuti —
  e **non batte la 002. Nessuna delle nove finaliste sopravvive.** Stesso split, stessi costi
  (ICS+FTMO, il peggiore), stesso orologio del confronto con la 002:

  | | campione | fuori campione | finestre |
  |---|---:|---:|---:|
  | 002 (uscita alle 21, scelta a mano) | 51.951 su 787 trade | **+52.310** su 475 | 3/4 |
  | migliore finalista al minuto | 50.386 su **52** trade | **−20.300** su 32 | 0/4 |
  | le altre otto | 12.689 … 39.293 su 52 | da −5.469 a −11.731 | 0-1/4 |

  Il modo in cui ha fallito è più istruttivo dell'esito. Il criterio ha stretto la configurazione
  fino a **52 trade in tre anni** (pattern neutrale 19 + direzionale 45 con DvolMin 3000, solo
  long, finestra fino alle 10): punteggio in campione **16,38**, il più alto mai visto, e fuori
  campione zero finestre in utile su quattro. È lo stesso segnale delle 173 trade dopo i pattern
  neutrali, portato all'estremo: con `MinTrades = 50` e 5 per tratto, il "peggiore dei quattro
  tratti" premia chi fa così pochi trade da non avere un tratto brutto. Il pavimento sulla perdita
  singola non basta. **Il minimo di trade va alzato di molto** (ordine di 300 su tre anni a 4 ore,
  cioè due a settimana) e vale come vincolo anche per NQ e GC che girano stanotte con il vecchio.
  Con l'orologio giusto la sweep ha smesso di ordinare rumore e ha cominciato a ordinare fortuna:
  è un progresso, ma non ancora una ricerca. Conclusione di ieri confermata: su questa cella si
  tara a mano un parametro alla volta, e la 002 resta il candidato.
- **NQ 4h al minuto, consegnata alle 5:40 del 22/09** (`ricerca/nq-4h-pc-tutto-al-minuto.md`, 177
  minuti): **nessuna delle dieci finaliste sopravvive**, e la patologia è la stessa di FDAX portata
  al limite. Il criterio ha stretto a **42 trade** in tre anni con **drawdown zero e nessun trade in
  perdita** in campione — punteggio **19.115**, cioè un numero senza senso prodotto dalla divisione
  per un drawdown nullo che il pavimento sulla perdita singola non ha fermato, perché di perdite
  non ce n'erano — e fuori campione −2.808 su 33 trade. Due difetti del criterio, entrambi da
  correggere prima di qualunque altra sweep: (1) **`MinTrades` troppo basso** (30-50 su tre anni):
  una configurazione che fa un trade ogni tre settimane non ha un tratto brutto per costruzione;
  (2) **un drawdown zero o un profit factor infinito devono rendere la configurazione NON
  ammissibile**, non vincente — sono la firma di un campione troppo piccolo, non della qualità.
  Anche qui la fase nuova ha scelto `ExitHour = 15`, su una configurazione `IntradayOnly = 0` dove
  è inerte. La ricerca GC che segue parte già con `--min-trades 250`.
- **GC 4h al minuto partita alle 5:45 del 22/09** (`ricerca/gc-4h-pc-tutto-al-minuto.md`, fine
  prevista ~9:00): XAUUSD da ICS — l'archivio raccolto stanotte parte dal **2022-09-21**, quindi il
  campione è 2,3 anni e non 3 come FDAX e NQ, stesso split al 2025-01-01 — spread **FTMO** (lo
  `SpreadDumpBot` ICS non ha ancora scritto), swap ICS dalla scheda, commissione 10,8,
  `--min-trades 250`. ~~Il caricamento dice «scartate 0 fuori sessione, 0 fuori finestra», quindi
  il calendario di GC non dichiara la finestra~~ — **sbagliato, corretto la mattina del 22/09**: il
  calendario di `GC` dichiara giorni (dom-ven) e finestra COMEX (18:00→17:00 New York) come tutti
  gli altri, e lo zero è **giusto**: XAUUSD su ICS ha esattamente la pausa del future — nel feed a
  un minuto del 2025 l'ora 22 UTC ha **zero barre** d'inverno e l'ora 21 zero d'estate, con il
  passaggio a marzo — quindi non c'era niente da mascherare. A differenza del DAX su FTMO, il CFD
  dell'oro non quota quando il COMEX è chiuso. Nessun lavoro sul calendario prima degli step a mano
  su GC.
- **`PT3B_FDAX_PCH_002_240`** = la 001 con `SessionExitTime = 21:00`. Verificata: riproduce al
  dollaro la misura col parametro. Nel catalogo del server 7.5.5, nel masterfilter, attiva nel piano
  `PT3B-FDAX`, disattivata in `PT3B-FDAX-FTMO`. **È il candidato per il backtest cTrader** —
  a tick — e il confronto pulito è con la 001 nello stesso run.
- **Secondo giro delle fasi orari e uscita dopo il rischio** (da fare sulla prossima ricerca): la
  fase orari sceglie con lo stop al default 1500 (60 punti FDAX), non con quello finale. Vedi
  `decisioni.md`.
- **Il paniere è l'unità di misura**, non la singola cella: selezione per cella con soglie
  permissive, validazione del paniere fuori campione **con lo stesso split per tutte le celle**.
  Ordine: FDAX (in corso) → NQ (feed ICS) → i dieci simboli del vendor con costi CFD peggiori →
  paniere. La diversificazione compensa la varianza, non l'assenza di edge.
- **Il feed NQ del vendor finisce il 2025-05-30** (`datafeed/@NQ_1`, `_15`, `_240`: ultima barra
  30/05/2025). Scoperto la mattina del 22/09 misurando le 39 strategie NQ del catalogo ai costi
  veri sul vendor: fuori campione 2025-01 → 2026-09 avevano 5 mesi invece di 20 (108 trade per la
  `PT2_NQ_PCH_001_240` contro i ~430 attesi), quindi **quella misura non vale per il fuori
  campione** e va rifatta sull'archivio ICS, che arriva a settembre 2026 (aggregati 15/60/1440 in
  derivazione dal minuto). Vale anche per la correlazione fra motori: i ranghi restano validi, i
  netti coprono 2022-01 → 2025-05.
- **FDAX 4h al minuto con il criterio 2 (250 trade, 25 per tratto, 10 perdite), consegnata alle
  13:30 del 22/09** (`ricerca/fdax-4h-pc-tutto-al-minuto-criterio-2.md`, 361 minuti):
  **nessuna delle nove finaliste sopravvive, e F5 è confermata per intero** — tutte e nove stanno
  fra **260 e 264 trade**, cioè esattamente sul pavimento di 250, come le 52 con il pavimento a 50.
  Alzare il minimo ha spostato la convergenza, non l'ha tolta: il criterio sceglie il conteggio.
  E la configurazione scelta è la trappola già documentata nel progetto: finestra **19:00 → 05:00**
  di Roma, cioè la notte, con lo spread **costante** a 1,23 che rende le ore care uguali alle
  altre (la sweep era senza `--spread-per-hour`). Campione +113.074 su 260 trade, fuori campione
  +9.234 su 155, una finestra su quattro; le altre otto varianti in perdita. **Conclusione: la
  sweep a fasi con questo criterio non è uno strumento di ricerca**, con nessuno dei due pavimenti.
  O si cambia il denominatore del punteggio (scalato con il numero di trade, F5), o la si tiene
  come controllo e si cerca con la griglia grossa — che nel frattempo ha già risposto su GC in
  due ore. Da oggi la griglia è lo strumento; la sweep NQ criterio 2 in corso è l'ultima.
- **La sweep GC 4h al minuto (criterio vecchio, `--min-trades 250`) ha consegnato alle 9:20 del
  22/09** (`ricerca/gc-4h-pc-tutto-al-minuto.md`, 214 minuti): **quattro finaliste su sei passano**,
  ed è la prima volta che una sweep consegna qualcosa con il profilo della 002 — centinaia di trade
  da entrambe le parti, utile da entrambe le parti:

  | # | IS trade | IS netto | OOS trade | OOS netto | finestre | tenuta |
  |---:|---:|---:|---:|---:|---:|---:|
  | **3** | 285 | +51.866 | 386 | **+115.029** | **4/4** | 88% |
  | 4 | 322 | +33.303 | 405 | +90.175 | 3/4 | 144% |
  | 5 | 353 | +8.701 | 404 | +96.609 | 4/4 | — |
  | 6 | 304 | +36.988 | 397 | +93.266 | 3/4 | 150% |

  Configurazione (una sola, in quattro varianti di break-even/trailing — F3 di nuovo): canale 1,
  offset 10 tick, **solo long**, neutrale 31 richiesto / 52 vietato, direzionale −40 vietato,
  **multiday** (`IntradayOnly = 0`, `ExitHour` inerte), `MaxBars = 12`, stop 2.500 (25 punti),
  target 5.000, tutto il giorno. Le quattro finestre della 3: 44.658 → 35.605 → 22.321 → 12.467.

  **Le riserve, prima di innamorarsene.** (a) È **solo long sull'oro nel 2025-26**, l'anno del
  rally: il fuori campione batte il campione di 2-3 volte e **decresce finestra dopo finestra** man
  mano che il rally rallenta — regime, non necessariamente edge. (b) Multiday con swap long a
  $59 a notte: regge lo stesso, ma la leva dell'uscita non è stata provata perché con
  `IntradayOnly = 0` la fase non fa nulla. (c) Spread FTMO, non ICS (non ancora misurato).
  (d) F5 mezza confermata: le finaliste 1-2 sono a 250 e 254 trade, esattamente il pavimento.
  **Il confronto con la griglia grossa è arrivato alle 11:00 ed è netto**
  (`ricerca/gc-4h-griglia-grossa.md`): il motore **nudo**, senza un solo pattern, è in utile fuori
  campione in **146 celle su 148** ammissibili, media ~110k, qualunque uscita e qualunque
  direzione (entrambe 119k > solo long 92k). La finalista 3 (+115k) sta nella media del nudo: **il
  fuori campione GC è il rally dell'oro 2025-26, non i pattern.** In campione il nudo fa 14-17k su
  tre anni. **Nessuna classe GC oggi.** Il test che decide: raccogliere XAUUSD ICS indietro fino al
  2014 (il bot, un anno per run) e rifare la griglia con campione 2014-2021 e validazione
  2022-2024 — sette anni con rialzi, ribassi e laterali. L'ora di uscita sull'oro è inerte
  (103-113k in ogni caso).
- **Griglia grossa NQ 15 minuti, consegnata alle 15:40 del 22/09** (`ricerca/nq-15m-griglia-grossa.md`,
  220 minuti): 92 ammissibili su 450, 30 in utile fuori con ≥ 3 finestre, **una sola equilibrata**
  (canale 20, stop 4000, target 8000, fine sessione, entrambe: IS 947 trade +112k / DD 96k, OOS
  590 trade +82k / DD 72k, 4/4). Letta con i criteri del metodo: avg trade $119 al limite della
  soglia del 15% del $ATR, guadagno annuo/DD **0,39** contro il 2 di Titan, **UngerFit 0,35**.
  Su NQ 15 il PC nudo non regge gli standard. Leve che parlano: uscita alle 21 aiuta fuori
  campione (16,6k contro 9,2k), lo short perde. Se si torna su NQ è con un altro motore (le TF 15
  del catalogo), non con filtri sul PC.
- **Criteri: letto il metodo (slide TSS2/TSE/PS/MPS e skill `metodo-unger`) il 22/09.** Il nostro
  `netto/DD del peggior tratto` è `N/R` — ordina sul numero di trade (skill §13.6, F5 della
  rilettura, e i pavimenti 250/260/253 delle sweep). Da adottare: **UngerFit** = √(avg_trade/soglia
  × 100/R) come ordinamento, con **cancelli binari** prima (campione ≥ 50 IS / 20 OOS con il floor
  intraday più alto, avg trade ≥ max(6-7 tick, 15% $ATR), anni positivi, outlier < 30% del netto,
  segnali di look-ahead), il DD come cancello e non come denominatore, e l'accettazione con lo
  **sconto walk-forward** (avg trade IS × 0,2-0,5 deve restare sopra soglia). Il 20% di DD è un
  vincolo di **sizing** (%f 1,25% sul worst day), non di ricerca: in ricerca il DD si giudica in
  rapporto (guadagno annuo ≥ 2 × max DD). Con questi la 002 (avg trade $66 IS, $110 OOS;
  guadagno/DD ~0,3-0,7) **non passerebbe il filtro di Titan da sola**: è una componente di
  paniere, non una strategia da conto.
- **Le 39 NQ del catalogo ai costi veri su ICS, fuori campione intero** (`ricerca/nq-catalogo-costi-veri.md`):
  **sei passano, nessuna con il profilo della 002** — tre in campione a zero (`PT2_NQ_PCH_002_30`
  +2.281 su 748 trade e poi +64.037 fuori: vento, non edge), tre TF a 15 minuti coerenti ma con
  100-200 trade (`TFU_003_15` 4/4 finestre è la migliore). La 4h Price Channel `PT2_NQ_PCH_001_240`,
  767 trade, **perde 47.114 fuori campione**: la sweep al minuto aveva ragione a bocciare la cella.
  Nessuna NQ è un candidato oggi; l'unico passo sensato è il mini-paniere delle tre TF 15 sullo
  stesso split, prima di qualunque taratura a mano.
- **Correlazione fra motori su NQ, misurata la notte del 22/09** (`ricerca/nq-correlazione-motori.md`
  e `.csv`, 37 strategie, P&L giornaliero al minuto 2022-2026): **media 0,056**, e fra famiglie
  di motori tutto fra 0,00 e 0,08 — TFM contro TFU 0,06, TFM contro PCH 0,04, dentro TFM 0,08.
  **La regola del pollice della sera prima era sbagliata**: sullo stesso simbolo i trend following
  non perdono negli stessi giorni, se timeframe e filtri sono diversi. Le uniche coppie sopra 0,45
  sono gemelle (`PCH_001_15`~`PCH_002_15` a 0,86, `TFM_001_60`~`TFM_009_60` a 0,50): nel paniere
  se ne tiene una per coppia. Limite dichiarato: serie sparse, misura sui giorni di chiusura e non
  sull'esposizione. Per la scaletta dei motori vale l'ordine pratico (classi di partenza e griglie
  pronte), non la famiglia.
- **⇦ SEI GRIGLIE GROSSE, 22/09 sera: la 002 regge su dodici anni, e FDAX e' l'unica cella con un
  edge.** Tutte con costi ICS veri (spread misurato, swap dalle schede, commissione per simbolo) e
  orologio al minuto. Rapporti in `ricerca/*-griglia-grossa*.md`.

  | cella | ammissibili | equilibrate | miglior avg trade IS | soglia | % | miglior UngerFit IS |
  |---|---:|---:|---:|---:|---:|---:|
  | **FDAX 4h** (2014-2026) | **332/450** | **60** | **404** | 266 | **152%** | **1,95** |
  | GC 4h (2014-2026) | 203/450 | 34 | 77 | 100 | 77% | 0,76 |
  | NQ 15m (2022-2026) | 92/450 | 1 | 119 | 120 | 99% | 0,35 |
  | NQ 4h (2014-2026) | 127/450 | 31 | 44 | 125 | 35% | 0,45 |
  | CL 30m (2016-2026) | 87/450 | 0 | 39 | 60 | 65% | - |
  | BP 60m (2014-2026) | 229/450 | 0 | 12 | 40 | 30% | 0,18 |

  **FDAX e' l'unica cella in cui qualcosa supera le soglie del metodo dove conta, cioe' in
  campione**: 33 celle con UngerFit sopra 1 e 27 con l'average trade sopra soglia. Sulle altre
  cinque il massimo e' 0,76. Su tutte e sei resta zero il criterio guadagno annuo su drawdown >= 2.

  **La 002 e' stata riverificata su dodici anni** (`Pt3b002LongPeriodTests`) perche' la griglia
  aveva mostrato che i suoi parametri strutturali - canale 1, entrambe le direzioni - stanno nella
  regione peggiore: il motore nudo li' fa **-121.891** fuori campione, e tutte e 25 le combinazioni
  di stop e target in quella regione hanno il fuori campione negativo. Esito: **la 002 regge**.
  1.609 trade e +157.820 in campione, 1.527 e +106.254 in validazione, PF 1,11 e 1,06. I suoi due
  filtri - pattern 44 (sessione stretta) e finestra 03-18 - erano stati scelti su tre anni e
  tengono su dodici: valgono 228.145 sul fuori campione. **Ma l'average trade e' 98 e 70 contro
  soglie di 266 e 365**, il 37% e il 19%: edge reale e stabile, troppo piccolo per unita' di
  rischio. Da tenere con una size proporzionata, non da far portare da sola un conto.

  **L'uscita alle 21 e' confermata quattro volte indipendenti**: FDAX (40 equilibrate su 60), GC
  (26 su 34), NQ 4h (118 ammissibili su 127 e tutte e 31 le equilibrate), e la 002 contro la 001 sul
  lungo (la 001 in validazione scende a 15 dollari di average trade contro 70). **Lo short perde** su
  FDAX (0 equilibrate su 103 celle) e su GC; su CL e' il contrario.

  **Dove cercare la 003**: canale 20, solo long, uscita alle 21, su FDAX. E' la regione che la
  griglia lunga indica e che la 002 non occupa.

- **Le tre griglie grosse del 22/09 dicono tutte no, e ora sui costi veri.** GC 4h sul periodo lungo
  (icerca/gc-4h-griglia-grossa-lunga.md): 203 ammissibili su 450 e **34 equilibrate**, ma con i
  criteri del metodo **zero** passano — il miglior UngerFit in campione è 0,76 contro 1, il miglior
  guadagno annuo su drawdown è 0,45 contro 2. Fuori campione tutto migliora di tre o quattro volte
  mentre la volatilità dell'oro cresce di due volte e mezzo: il sistema non migliora, segue il
  mercato. CL 30m su dieci anni (icerca/cl-30m-griglia-grossa.md): **zero equilibrate**, profit
  factor 1,0-1,09, average trade 4,77 dollari contro un tick da 10. NQ 15m: già scritto stamattina.
  **Tre celle su tre: il Price Channel nudo non regge le soglie.** Il prossimo test non è un'altra
  griglia sullo stesso motore.
  Due leve misurate che restano: sull'oro lo **short perde** e l'**uscita prima del rollover** batte
  quella a fine sessione (26 equilibrate su 34 contro 8) — la stessa leva che su FDAX ha prodotto la
  002, e il giro corto l'aveva dichiarata irrilevante perché in quel periodo guadagnava tutto. Su CL
  è il contrario: lo short è l'unica direzione con media fuori campione positiva.

- **Raccolta ICS di GC, CL, BP avviata la notte del 21/09**: piano di sola raccolta `RACCOLTA-ICS`
  (workspace `raccolta-ics`), `PiootooDatafeedSyncBot` un anno per run, `PiootooSpreadDumpBot` un
  mese con broker `ICS` esplicito. Lo **swap ICS** dei tre è già in
  `swap/ICS/ICS_swap-by-symbol.csv` dalle schede del 22/09 — pip position 2 (oro, petrolio) e 4
  (sterlina), crediti azzerati, triplo di mercoledì su oro e sterlina. **Commissione per simbolo**,
  da passare a mano alla sweep: GC ~10,8 per lato, CL 0, BP ~4, contro il 19,23 del DAX. EC non è
  raccoglibile finché non esiste una classe `@EC` nel catalogo.
- **Flat prima del rollover come parametro del piano, 22/09**: non si riscrivono le strategie
  (la via di `PT3B_FDAX_PCH_002_240`), si vieta l'overnight nel piano. Tre cose chiuse insieme,
  vedi `domini/overnight-e-overweek.md` §3–4 e `decisioni.md`: (1) la sweep onora la tenuta con
  `--flat-utc HH:mm [--flat-window N]` (`sweep-paniere.ps1 -FlatUtc 20:45`), prima girava sempre a
  overnight libero e validava una strategia diversa da quella operata; (2) il flat di sessione è
  una **finestra** `[flat, flat + minuti)`, default 30, dentro la quale non nascono ingressi e i
  pending si cancellano — con il solo istante la barra delle 20:45 di una 15 minuti riceveva la
  deadline del giorno dopo e attraversava il rollover; (3) l'avviso `[rollover]` in testa al
  summary e nel resoconto della sweep quando `SwapSpec.RolloverUtc` non cade dentro la finestra.
  Versione **7.6.0** (campo nuovo nel descriptor = contratto nuovo): server, console e cBot
  (`EnforceSessionFlat`, barriera in `HandleEntryIntent`) vanno distribuiti insieme. **Non
  misurato**: un piano con flat alle 20:45 su GC, CL, BP; la 002 di FDAX resta la misura di
  riferimento e la variante «overnight solo sul lato in credito» è stata scartata perché
  legherebbe le strategie al listino dello swap.
- **Non ancora fatto**: la modalità dati del backtest cTrader in `origin.json` (compare-0048 §7);
  l'errore esplicito della sweep su archivio di broker senza il simbolo (oggi
  `IndexOutOfRangeException`).

## Le due correzioni da fare PRIMA di rilanciare la ricerca

**1. Le soglie di qualità sono applicate nella fase sbagliata.** `MinProfitFactor` (1,25) e
`MinAverageTrade` (150) valgono oggi per *tutte* le fasi, ma le prime ottimizzano il trigger quando
stop e target sono ancora a zero: la configurazione è grezza per costruzione e non può avere un buon
profit factor. Sul BIAS settimanale la prima fase dà **zero ammissibili con 381 trade osservati**, e
la ricerca muore prima di cominciare. Vanno applicate solo dalle fasi di rischio in poi
(`SweepPhase.RequiresAccurateClock` è già il flag che le distingue) o in validazione.

**2. Il periodo di ricerca usa costi che in quel periodo non esistevano.** Il campione 2014-2022
gira con lo spread misurato ad **agosto 2026**. Nel 2014 i CFD retail avevano spread di 4-5 punti
sul DAX; oggi ICS ne quota 0,50. **Cercare dal 2014 significa cercare in un mondo con costi
finti.** La proposta sul tavolo è campione 2021-01 → 2024-06 e validazione 2024-06 → 2026-09:
meno storia, ma costi veri.

⚠ **La misura che sosteneva questo punto non è valida e va rifatta.** La tabella di confronto
2014-2018 (scarto −$123 per trade, ≈5 punti FDAX) era presa contro il run cTrader
`...20140801-0000-...1449`, che gira a **barre al minuto** e non a tick: quello scarto contiene
slittamento di simulazione in quantità ignota. Vedi `compare-0048/esito.md`. La tesi resta
plausibile, il numero no: serve un run a tick sul 2014-2018 prima di decidere il periodo.

## Cosa c'è già e funziona

- **`piootoo-sweep`** — ricerca a fasi, validazione fuori campione, costo peggiore fra più broker.
  Lanci dal paniere con `tools/sweep-paniere.ps1` (`-SoloStima` per vedere cosa farebbe).
- **Swap nel motore** (`SwapSpec`, `SwapTable`) e spread per ora: misure in
  `piootoo-repository/swap/{BROKER}/` e `spread/{BROKER}/`, con ICS e FTMO già compilati.
- **`PT3B_FDAX_PCH_001_240`** — validata, scheda in `piootoo-repository/ricerca/`.
- **Workspace `v03-pt3b`** con due piani: `PT3B-FDAX` (ICS) e `PT3B-FDAX-FTMO`.

## Il nodo del backtest in cTrader — chiuso il 21/09/2026

I tre run `ExternalBroker` inconciliabili si spiegano per intero con la **modalità dati scelta
nella finestra di backtest di cTrader**: tick contro barre al minuto. Il cBot e il server non
c'entrano. Analisi completa in `compare/compare-0048/esito.md`; in breve:

- `quantity = 25` su **tutti** i trade di tutti i run e **zero sovrapposizioni**: le due ipotesi
  (size doppia, posizioni concorrenti) erano sbagliate, e il run incriminato girava col profilo
  `DalPiano`, quindi `BacktestSorgente` non era nemmeno in gioco;
- la percentuale di ingressi sul minuto tondo esatto partiziona i run **0% / 100%** senza
  sfumature: i due "coerenti" sono a tick, i tre negativi a barre;
- rilanciato a tick, il run che faceva −$98.613 chiude a **+33.535 € (+34%)**;
- causa: a barre m1 cTrader sintetizza pochi tick per barra dai soli OHLC, e lo stop si riempie
  **all'estremo della barra** anziché al livello — verificato con scarto 0,0-0,1 su 12 casi su 13.
  Lo slittamento è l'intera escursione della barra oltre il livello: fino a 333 punti su uno stop
  da 200.

**Resta aperto il numero, che non è buono.** Il run sano fa +34% in 3 anni e 8 mesi con un
**drawdown del 62,6%** ($62.614, minimo a $37.386 nell'ottobre 2023) — non il ~$47.000 stimato
finora. Win rate 49,4%, vincita media $2.691 contro perdita media $2.515, peggior singolo trade
5,3% del conto, fino a 8 perdite consecutive (13 sul run dal 2022). Un contratto FDAX su $100.000
è troppo, ma ridurre la size non migliora il rapporto: lo scala. Il margine per trade è troppo
sottile rispetto alla varianza, ed è quello il bersaglio delle direzioni 1 e 2 qui sotto.

## Le direzioni che valgono di più, in ordine

0. **Una cella sola non ha la potenza statistica per decidere niente.** Sui 1.035 netti per trade
   del run sano il test t dà **t = 0,57** (media $55,3, deviazione standard $3.142): l'intervallo
   di confidenza al 95% sull'utile annuo va da **−$38.000 a +$69.000**. Il +34% è compatibile con
   un'aspettativa nulla, e servirebbe `t ≈ 2` — di più, visto che la configurazione è stata scelta
   fra decine di migliaia. Non significa che la strategia sia cattiva: significa che **non lo
   sappiamo**, e che nessuna taratura del criterio su questa cella può dirlo. Il campione si
   allarga con più **celle** (simbolo × timeframe × motore), non con più storia sullo stesso
   mercato: quella cresce piano e porta costi d'epoca che non esistono più.

0 bis. **Il criterio di selezione deve guardare il rapporto, non il netto.** 34% di utile con 62%
   di drawdown non è una strategia da mettere in produzione, ed è uscita da una ricerca che quel
   rapporto non lo vincola: `WorstSubPeriodObjective` lo usa per *ordinare* le combinazioni, ma
   nessuna soglia di ammissibilità lo impone, né in campione né in validazione. Un tetto esplicito
   al drawdown — e un pavimento al rapporto netto/DD — appartiene alle soglie insieme a
   `MinProfitFactor` e `MinAverageTrade`, e come loro va applicato dalle fasi di rischio in poi.

1. **Meno trade, più margine ciascuno.** 1.317 trade da $78 netti è il profilo peggiore: i costi
   sono proporzionali ai trade, il margine no.
2. **Non attraversare il rollover.** $58.564 di swap su $212.344 di lordo, pagati per tenere
   posizioni poche ore oltre le 21:00. Oggi la chiusura è legata alla fine sessione: **renderla un
   parametro della ricerca** è una modifica piccola che vale un quarto del lordo.
3. **Futures invece di CFD** — niente swap, commissioni minori, spread di un tick.
4. **Gli altri otto motori.** Provati solo Price Channel e BIAS settimanale.
5. **Altri simboli.** Solo FDAX e NQ, perché il feed ICS ha solo quelli.

## Fuori dal modello, da non dimenticare

- **`P&L conversion fee 0,70%`** che FTMO dichiara e ICS no. Il motore non la applica. Si misura
  confrontando lo stesso backtest `ExternalBroker` sui due broker: la differenza che spread e swap
  non spiegano è lei.
- Lo **short perde in ogni periodo misurato** su FDAX. `Direction` è già nella prima fase della
  ricerca: va lasciato decidere a lei, non forzato a mano.

---

# ⇦ RIPRENDERE DA QUI: due misure sul paniere

Sessione del 08/09/2026 interrotta qui, di proposito. Il **codice è finito e verde**; quello che
manca sono due run, e nessuno dei due è stato lanciato.

Piano completo e stato passo per passo in
[`domini/layer-barre-e-calendario.md`](domini/layer-barre-e-calendario.md) §7. Il metodo è sempre
lo stesso e va tenuto: **si misura prima**, si cambia, si rimisura sul paniere completo.

## Le due misure che mancano

Servono due run `all-in` su `FTMOPLATFORM`, un anno, prima/dopo. **Indipendenti**: tenerli separati
o non si sa quale numero venga da cosa.

1. **Passo 5** — `MaxBarsInPosition` sul calendario e l'uscita a indice di barra come conteggio di
   barre vere. Riguarda 62 strategie su 124.
2. **Etichetta della barra** — la finestra, il filtro del giorno e le pause leggono ora la chiusura.
   Riguarda 83 strategie su 124 per la finestra, tutte e 13 le giornaliere per il giorno. Il
   confronto si fa con `BacktestingRequest.LegacyBarOpenLabels = true` contro il default.

Servono a **dichiarare** di quanto si spostano i risultati, non a decidere: le due modifiche sono
correzioni di difetti, non opzioni. E dicono quali cartelle archiviate non sono più confrontabili.

## Fatto l'08/09/2026

- **Passo 5 completo** — `MaxBarsInPosition` conta le barre della strategia sul calendario,
  `ScaleSignalMaxBarsInPosition` eliminato, `SessionBarToUtc` sostituito da un conteggio di barre
  vere in `BiasBarCountEngine.WithExit`. `MaxDaysInTrade`/`MaxDaysFlatTime` verificati **codice
  morto**: zero PTS su 124 li valorizzano.
- **Passo 7** — il riscaldamento di una sessione lo legge il server dal disco
  (`datafeed-external/{BROKER}/@SYM_1.json`). R2 e R3 di
  [`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md)
  **restano**: chiudono col passo 6-operativo.
- **L'etichetta della barra in un punto solo** — `SessionClock.BarLabelTime`/`BarLabelDay`, e
  `EasyLib.TimeWindow` prende un `TimeOnly` e non più una barra, così nessun chiamante può rispondere
  per conto proprio. Il flag è rovesciato (`LegacyBarOpenLabels`, default `false`): **il default è
  quello corretto**, quindi la sessione live non deve impostare niente. La guida di conversione,
  per le strategie che arriveranno, è in
  [`domini/porting-da-report-sweep.md`](domini/porting-da-report-sweep.md) §"La regola degli orari".
- **Cancellati**: le 21 classi PTS su simboli fuori calendario (HK, HO, JY),
  `ResearchSessionStartConformanceTests`, il motore a **rotazione settimanale** (`TradingEngine`,
  `StrategyRotationManager`, `WeeklyRotationScheduler` e i tre endpoint che lo esponevano — era un
  terzo percorso di valutazione senza niente dell'ultimo mese), e la **SPA Angular**.

Suite: **931 test, 40 rossi preesistenti**, nessuno nuovo in nessuno dei cinque commit. Tutto su
`main` (`origin/main` allineato l'08/09/2026).

## La serie PT2 (16/09/2026): tradotta, non verificata

Le quattro schede di `run-engine-v2/DOSSIER_PANIERE_001.md` sono tradotte in
`Piootoo.Strategies/PT2Strategies/` e registrate ([`domini/mappa-strategie-pt2.md`](domini/mappa-strategie-pt2.md)).
Le due FDAX sono girate sul feed interno 2022-2025 e tornano sul numero di trade (166 su 166, 850
su 884). Per chiuderle manca, in ordine:

1. **Il run NQ** per `S02` e `S04`: il CSV del vendor e' in `datafeed-future/FUTURES_Historical_Data/`
   e `@NQ_1.json` e' in generazione (16/09 pomeriggio). Stesso run delle FDAX.
2. **Le liste trade di riferimento.** Il dossier le cita (`*/consegna/trades/fam01_*.csv`) ma in
   `run-engine-v2/` non ci sono: senza, il confronto entrata per entrata non e' possibile.
3. **Il rollover della BSW in vivo** e' allineato (server e cBot 7.4.4, `decisioni.md`
   2026-09-16) ma non ancora osservato su un conto: al primo lunedì di `S01` controllare nel log del
   bot la riga `Rollover ...` e nel server che la chiusura e il nuovo ingresso portino lo stesso istante.

## Il resto, in ordine

- ~~**Il feed `@FDAX_240.json` è ancorato a 00:00 e il calendario dice 01:00.**~~ **Rigenerato il
  16/09/2026** dal CSV del vendor insieme a `@FDAX_1` e `@FDAX_60` (ancoraggio 01:00, UTC, etichetta
  di apertura; la copia vecchia e' fuori dal repository). CC, CT, KC e SB non hanno né `_240` né
  `_1440`, che è un buco diverso.
- **L'estrazione del passo di valutazione.** Backtest e sessione chiamano `strategy.Evaluate`
  ognuno per conto proprio: `RequiredCandles * 1.2` è un letterale in **sei punti**, la regola sulla
  storia insufficiente ha due comportamenti (contata da una parte, silenziosa dall'altra), e le
  convenzioni del motore le imposta solo il backtest. Coincidono oggi solo perché i default
  coincidono. Un punto solo che possieda finestra, regola sulla storia, convenzioni e rifinitura del
  segnale; da lì cade da sé la dichiarazione delle convenzioni nel `session-summary.json`.
- **I sei punti di `verifica-offset-conversione-2026-09-08.md`.** Quattro sono chiusi (finestra,
  filtro del giorno, dall'11/09 BIASW `le_time`/`lx_time`, che leggono l'etichetta di chiusura e
  nascono sulla barra prima, e dall'11/09 l'uscita del venerdì di MAC, che ora chiede alla griglia
  di sessione `IsLastBarOfSession` invece di confrontare l'orario di chiusura della barra con una
  fine sessione che era la sentinella `2359`). Restano le tre copie di "inizio sessione"
  (`SessionKey`, `ResolveEntrySessionStartUtc`) che arretrano solo se `Start > End`, mai vero per
  una sessione della ricerca.

## Le altre voci aperte del piano

- **4b — gli engine consumano `BarContext`.** Eliminare `EasyLib.ClassifySessionBar`,
  `FullDaySessionDay`, `ResolveEntrySessionStartUtc` e le sue tre copie nei motori. Include il ramo
  "sessione di borsa" di `ClassifySessionBar`, ora **provatamente irraggiungibile** (tutte e 124 le
  PTS usano la forma a giornata piena). Tocca i motori, non il catalogo.
  Guadagno collaterale: `OHLCMulti5` fa `Where().OrderBy().ToArray()` a **ogni valutazione** — LINQ
  nel punto più caldo del sistema, che CLAUDE.md vieta.
- ~~**6 (parte operativa) — i due cBot di esecuzione.**~~ **Chiuso il 08/09/2026 (7.2.0).** Il bot
  diretto e' stato **tolto** — un distribuito con un simbolo solo e' nelle stesse condizioni, e un
  terzo percorso di esecuzione da tenere allineato non dava niente in cambio — e il distribuito
  legge ora ancoraggio e fuso dal descriptor (`TradingInstrument.BarGrid`, dal calendario di
  mercato). Spariti `SessionStartHourOf` e i parametri *Fuso dell'ancoraggio* e *Ora di inizio
  sessione*: la tabella §2.4 vive in un punto solo. Uno strumento fuori dal calendario **non fa
  piu' aprire la sessione**.
- **7 — riscaldamento dal disco. Fatto per la meta' che non dipende dal 6.** Il server riempie la
  storia all'apertura leggendo `datafeed-external/{BROKER}/@SYM_1.json`. R2 e R3 di
  [`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md)
  **restano**: finche' i bot operativi mandano barre gia' aggregate il client deve poter
  caricare la propria storia dal broker, ed e' comunque l'unica strada dopo un riavvio del
  server. Chiude col passo 6-operativo.
- **8 — `aggregate_flat_feed.py` produce solo il minuto.** I CSV del vendor sono sul disco
  (`datafeed-future/FUTURES_Historical_Data/`, 200-360 MB per simbolo), quindi non serve nessuna
  raccolta. Chiude da sé il difetto §7quater: una barra giornaliera all'anno per simbolo è fuori
  griglia, sempre l'ultima domenica di ottobre.

## Da fare fuori dal codice

- **Rifare compare-0033 con la 7.2.1** su entrambe le gambe (backtest interno e run cBot
  ricompilato): le cinque correzioni dell'11/09 (`decisioni.md`) cambiano i trade di YM_BIA,
  ES_BSW, dei market su tick senza barra e dei livelli dentro lo spread. Lo strumento di confronto
  è in `piootoo-repository/compare/compare-0033/analisi/strumento/`. Le decisioni ancora aperte
  dal confronto: inversione sul segnale opposto, commissioni per simbolo e conversione EUR→USD nel
  backtest.
- **Ricompilare i cBot in cTrader.** Il salto a 7.0.0 è un cambio di contratto: i due bot operativi
  stampano un disallineamento finché non vengono ricompilati e ridistribuiti. Il raccoglitore
  (2.0.0, numerazione propria) va ricompilato perché è cambiato del tutto.
- **La raccolta a un minuto** può partire quando vuoi: il server nuovo è il prerequisito, perché
  `rebuild-from-minutes` non esiste su quello vecchio e senza aggregati i backtest sopra il minuto
  non trovano il datafeed.

---

## Nulla di quanto segue è stato compilato

Le modifiche del 04/08 sono state scritte in un ambiente senza `dotnet` e senza l'API cAlgo. Prima
di fidarsi di qualunque riga:

```bash
dotnet build PiootooApp.sln
dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj
```

I due cBot vanno ricompilati dentro cTrader. Sono stati rinominati, quindi per la piattaforma sono
robot nuovi e vanno riaggiunti ai grafici.

## Refactor della lista backtest — scritto, mai eseguito

Il refactor è completo lato codice: *Backtesting* è lista → dettaglio come le altre voci, con filtro
workspace e origine, ordinamento per data discendente, cancellazione che avvisa dei run Titano
contenuti, e *Nuovo backtest* che porta al form di avvio.

Server (invariato rispetto al 04/08): `GET {ws}/backtests/{cartella}/summary`, `GET .../titano-runs`,
`DELETE {ws}/backtests/{cartella}`; marcatore `origin.json` scritto da `PiootooBacktestingService` e
`TradingSessionService`, propagato in `WorkspaceBacktestInfo.Origin`.

Client: `BacktestListScreen` (+ designer, con colonna *Origine* e combo di filtro),
`BacktestDetailScreen` (+ designer, tab *Riepilogo* e *Operazioni*), `TradingResultsScreen`
cancellata e la sua voce di menu rimossa, `BacktestingScreen` raggiungibile solo da *Nuovo backtest*.
Le motivazioni sono in [`decisioni.md`](decisioni.md).

Lo stesso trattamento è stato applicato a Titano: *Operatività → Run Titano* è la lista
(`TitanoRunListScreen` → `TitanoRunDetailScreen`), `TitanoScreen` è la destinazione di *Nuova
rotazione*, `RotationsScreen` è cancellata e la sezione *Analisi* non esiste più. Lato server:
`GET /api/Titano/rotations` senza `backtestFolder` elenca tutti i run del workspace, e
`DELETE /api/Titano/rotations/{runId}` è nuovo.

Trasversali, sempre non compilati: griglie ordinabili (`SortableBindingList<T>` +
`EnableColumnSorting()`), busy visibile sull'intera schermata, e la lettura leggera di
`backtest-summary.json` e dei manifest Titano negli elenchi. Le regole sono ora scritte in
[`.cursor/rules/piutoo-console-screens.mdc`](../.cursor/rules/piutoo-console-screens.mdc).

**Da verificare al primo avvio su Windows** — niente di tutto questo è mai stato compilato né
aperto nel designer:

- Il layout dei due tab dipende dall'ordine di `Controls.Add` (Fill per primo, Top in ordine
  inverso). Se il riepilogo appare capovolto è quello.
- `BacktestingScreen` ora vive dentro uno stack di navigazione: a fine job non torna alla lista, e
  la lista non si aggiorna da sola. Se dà fastidio, `GoBack()` al completamento.
- Il tab *Operazioni* mostra `trades.json` anche per le cartelle di origine esterna, dove il
  summary non c'è: verificare che l'assenza sia segnalata e non passi per un errore di rete.

## Questioni aperte

### Ripresa di una sessione dopo il riavvio del server — fasi 0 e 1 fatte

Procedura completa in [`domini/riavvio-del-server-e-ripresa-sessione.md`](domini/riavvio-del-server-e-ripresa-sessione.md),
motivazioni in [`decisioni.md`](decisioni.md) 2026-09-04.

**Fatto il 04/09/2026:** il dump (`session-state.json`), la reidratazione all'avvio con id e token
conservati, il riscaldamento autoguarente sul cBot distribuito, il presidio dalla console.

**Aperto**, in ordine:

1. **Epoca e quarantena** (fase 2 e 3 del documento). La quarantena non è stata implementata
   insieme alla reidratazione perché la sua unica uscita è la riconciliazione, che non c'è: oggi
   congelerebbe il sistema dopo ogni riavvio. Le due vanno fatte insieme.
2. **`POST /reconcile`** (fase 3). Sul percorso distribuito buona parte esiste già —
   `AccountSignalPollRequest` porta posizioni, ordini e trade a ogni poll, e
   `ReconcileVanishedPositions` li consuma — e va generalizzata ai pending, al prezzo di chiusura
   vero e alle sessioni dirette, che non hanno un poll di claim.
3. **Label del cBot diretto** con l'IntentId, come già fa il distribuito.
4. **Outbox** sui cBot per gli execution report: oggi sono fire-and-forget, quindi quello che
   accade mentre il server è giù è perso per lui e nessun dump lo recupera.

**Da verificare al primo run vero:** i cBot non sono compilati qui dentro. `PiootooDistributedExecutionBot`
è cambiato (campo `WarmUpBarsSent`, confronto in `ReportWindowStatus`) e va ricompilato in cTrader.

### La distribuzione multi-account non è backtestabile

`MaxConcurrentTrades` e il fan-out fra gruppi sono applicati solo nel percorso di claim, che vale per
`ExecutionMode.ExternalBroker` con gruppi configurati. Il backtest interno non distribuisce, e il
backtesting cTrader esegue una istanza su un conto simulato: due backtest separati non sono
sincronizzabili, e la distribuzione è **pull**, quindi l'ordine dei poll — e con esso il risultato —
sarebbe casuale. Oggi l'unica verifica deterministica sono i test
(`MultiAccountDistributionTests.cs`); l'unica misura operativa è il live su conti demo.

Per chiudere il buco servirebbe un driver server-side che alimenti una sessione con barre storiche
(sul modello di `Piootoo.FeedWorker`) e faccia pollare N account simulati in un ordine deterministico.

### Storico barre persistito sul server, per non rimandare il riscaldamento a ogni run

Idea da valutare, **non** implementata. Oggi la storia di una sessione `ExternalBroker` vive in RAM
e muore con la sessione: siccome in backtest `ExecutionKey = BT-{istante di avvio}`, ogni run apre
una sessione nuova e il cBot deve rimandare da capo le `RequiredCandles` barre di riscaldamento
(576 a 15 minuti). Le regole attuali sono in
[`domini/finestra-candele-e-riscaldamento.md`](domini/finestra-candele-e-riscaldamento.md).

La variante: il server tiene su disco lo storico per `(simbolo, timeframe)`. Il cBot, al boot,
**chiede lo stato delle barre** (che intervallo il server ha già per i suoi stream), invia solo la
finestra che manca, e il server salva le candele che non ha. Da lì in poi il cBot continua a
mandare sempre le ultime N, ma sapendo che lo storico profondo è già dalla parte del server.

Cosa risolve: riscaldamento pagato una volta sola invece che a ogni run; e lo stesso storico
diventa datafeed riutilizzabile per i backtest locali, che è il lavoro previsto per il cBot
raccoglitore dedicato — le due cose confluiscono.

Da decidere prima di scriverlo:

- **Chi possiede il feed.** Oggi R8 dice esplicitamente che la strada di esecuzione non scrive
  datafeed, perché la qualità dello storico non deve dipendere dagli orari in cui è girato un bot
  di trading. Questa variante rompe quella separazione: o si accetta, o il salvataggio resta al
  solo cBot raccoglitore e la sessione si limita a *leggere* ciò che trova.
- **Fiducia nel feed salvato.** Barre arrivate da un conto demo, da un broker diverso o da una
  sessione interrotta a metà non sono equivalenti. Serve almeno la provenienza per riga, altrimenti
  un backtest locale gira su un miscuglio senza saperlo.
- **Il nuovo endpoint di stato.** `GET /{sessionId}/streams` (o per workspace, se lo storico è
  condiviso fra sessioni) che restituisca per stream primo/ultimo timestamp e numero di barre.
  Attenzione: "ho 600 barre" non basta, servono gli estremi, altrimenti il client non sa se la sua
  finestra si sovrappone e R7 non è verificabile dal suo lato.
- **I buchi interni.** Uno storico su disco può avere vuoti in mezzo, non solo in coda. La regola
  della sovrapposizione (R7) copre la coda; per i buchi interni serve un controllo suo, altrimenti
  si torna esattamente al problema che R6 e R7 esistono per evitare.

### Minori

- Le cartelle sotto `sessions/` non vengono mai ripulite. Ora hanno un nome parlante
  (`{piano}-{executionKey}`), quindi la pulizia è fattibile ma resta manuale.
- Nel bot diretto **non** sono persistiti gli ordini pending né il picco per lo stallo dell'utile:
  ripartono rispettivamente dalla riconciliazione col server e dall'utile corrente.
- `promote-to-backtest` è rimasto utile solo per le sessioni senza piano e per lo storico realtime:
  le sessioni di backtest da piano scrivono già dove Titano legge.
- Il dettaglio del setup Titano usa un `PropertyGrid`. Se si vuole un layout a gruppi come nella
  schermata operativa, le annotazioni `Category`/`DisplayName` sul modello sono già a posto.

## Da verificare al primo build: granularità di volume sulla riga di conversione (2026-08-05)

Chiuso lato codice (motivazioni in [`decisioni.md`](decisioni.md)): la granularità di volume vive
ora sulla riga della tabella di conversione dell'account, non sul piano. Nessuna voce di questa
sessione è stata compilata (vedi avviso in cima al file — file `.exe`/`.dll` bloccati da
un'istanza in esecuzione in Visual Studio): prima di fidarsene, `dotnet build` e
`dotnet test Piootoo.Strategies.Tests`.

Punti da controllare al primo run verde:

- I test toccati (`TradingSessionsHttpTests`, `TitanoSizingAuditTests`, `TradingGroupTitanoTests`,
  `TitanoRotationTests`) sono stati aggiornati a mano per compilare e ragionati a tavolino, non
  eseguiti. `TradingGroupTitanoTests.GroupTitanoProfile_ScalesClaimedQuantityUsingAllocationMultiplier`
  in particolare ha un `return` anticipato quando il manifest sintetico non produce
  un'allocazione parziale (dipende da `trades.json` vuoto): se nel run la formula attesa non torna,
  parti da lì prima di sospettare `RoundQuantity`.
- `SymbolConversionDetailScreen`: le colonne nuove non sono mai state aperte nel designer.
- Nessuna colonna aggiunta alla console legacy: `WorkspaceBacktestingForm` (tab Accounts) non ha
  mai avuto una griglia di conversione inline (solo una combo di selezione, vedi
  `docs/domini/account-e-conversione-symbol.md`), quindi il punto 6 originale non si applicava lì.

## `MaxEntriesPerSession` per direzione — chiuso il 07/09/2026

Il verso sta in entrambe le chiavi (`EntryFillKey` sul server, `MakeEntrySessionKey` nell'engine),
con regressioni in `EntryLimitPerSideTests` e `SessionEntryLimitTests`; voce in
[`decisioni.md`](decisioni.md) 2026-09-07. Questa sezione restava qui per errore e ha fatto
riproporre il punto come aperto l'11/09.

## La suite: verde dal 15/09/2026

**0 falliti** (erano 13 l'11/09, 43 il 05/09, 55 il 31/08). I 13 erano tutti test rimasti indietro
rispetto a regole introdotte dopo, nessuno un difetto del codice: dettaglio in
[`decisioni.md`](decisioni.md) 2026-09-15.

Regola invariata: un test che torna verde perche' riscritto e' il modo tipico di cementare un
bug; ogni caso va letto contro l'invariante di `CLAUDE.md` prima di toccarlo.

### Gli studi di ricerca non girano con la suite

Quattro classi del progetto di test non sono test: **misurano**, girano per ore sul feed vero e
riscrivono file in `piootoo-repository/ricerca/`. Sono `NqCoarseGridTests` e `GcCoarseGridTests`
(entrambe via `CoarseGridStudy`), `NqEngineCorrelationTests` e `SweepFastClockRankingTests`.

Il 22/09/2026 un `dotnet test` senza argomenti non e' finito in quaranta minuti, e lo studio
interrotto a meta' aveva gia' riscritto `nq-correlazione-motori.csv`, che e' tracciato da git.

Da allora gli studi sono **spenti di default**: escono subito, stampano il motivo e il test resta
verde, come gia' succede quando il feed non c'e'. L'interruttore e' la variabile d'ambiente
`PIOOTOO_STUDI` (vedi `ResearchStudy`), la stessa convenzione di `PIOOTOO_PERSIST_ALL_INTENTS`.
Un filtro di default andrebbe ricordato; una variabile spenta no.

```bash
dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj
```

gira i test veri in poco piu' di tre minuti. Per far girare gli studi, da PowerShell:

```bash
$env:PIOOTOO_STUDI='1'; dotnet test Piootoo.Strategies.Tests/Piootoo.Strategies.Tests.csproj --filter "Category=Studio"
```

Il filtro non accende nulla: serve a non trascinare gli altri mille test in un giro che ne vuole
quattro. Uno studio nuovo porta **entrambi**, la guardia e il trait `Category=Studio`; se nasce
come cella di `CoarseGridStudy` la guardia ce l'ha gia'. Dopo un giro interrotto conviene
controllare `git status -- piootoo-repository/ricerca`.

## Riferimenti codice

`PiootooApp.Server/Controllers/WorkspaceController.cs`,
`Piootoo.Core/Services/WorkspaceService.cs`,
`Piootoo.Core/Services/TradingSessionService.cs`,
`piootooapp.clientform/Shell/NavigationRegistry.cs`,
`piootooapp.clientform/Shell/Screens/BacktestListScreen.Designer.cs`,
`piootoo-repository/ctrader/PiootooDirectExecutionBot.cs`.
