# compare-0041 — cbot-cfd-FTMO contro interno-cfd-FTMO

Cartella creata dalla console il 13/09/2026 10:00 UTC. Analisi con log ed Events del cBot, aggiornata il
13/09/2026 dopo la correzione dell'engine interno (commit `7c49e99`, niente inversione sul segnale opposto)
e il nuovo backtest interno a 7.4.0. Report dello strumento in `analisi/report.md`, scheletro e regole in
`compare/README.md`. Il run interno precedente (7.3.0, con l'inversione) sta in `interno-motore-precedente/`.

Questa coppia isola **il motore a parità di feed** (entrambe le gambe leggono CFD FTMO). Serve, in quest'ordine:

1. accertare se cBot e server **rispettano le strategie** allo stesso modo, o se ci sono bug di implementazione fra i due;
2. accertato quello, quantificare **quanto pesano i costi reali** — commissioni, swap, spread ai fill;
3. verificare che l'architettura tenga **strategie e trade disaccoppiati**: tutte le condizioni di ingresso e uscita nel segnale, l'engine solo esecutore.

**Esito in una riga: cBot, server ed engine interno ora chiudono le posizioni con lo stesso criterio.**
L'unica divergenza strutturale trovata — l'inversione sul segnale opposto dell'interno — è corretta e
verificata sul run: `OppositeSignal` è passato da 95 uscite a **0**. L'asimmetria di BiasBarCount aveva
una causa diversa, nel server: le sessioni cBot perdevano la memoria delle strategie fra una barra e
l'altra (§1b), corretto con `a764ef5`. Quello che resta del divario è esecuzione (spread, commissioni,
swap) e granularità barra/tick.

## Le due gambe

| lato | slug | motore | serie di prezzi | arco (ingressi) | versione | piano |
|---|---|---|---|---|---|---|
| cBot | `cbot-cfd-FTMO` | `PiootooDistributedExecutionBot` in cTrader | CFD FTMO | 31/08/2025 → 04/08/2026 | 7.4.0 | COMP-004X |
| interno | `interno-cfd-FTMO` | `PiootooTradingService` | CFD FTMO | 31/08/2025 → 30/08/2026 | 7.4.0 | COMP-004X |

Nessun campo dedotto: entrambi gli `origin.json` hanno `IdentifiesRun: true`, `Broker = FTMO`. Il backtest
interno è `all-003/backtests/backtest-20260913-senza-inversione`: piano COMP-004X, feed FTMO, spread
FTMOPLATFORM p50 per ora UTC, stop x1, capitale 1.000.000, gli stessi parametri del run precedente
`backtest-20260913-0748`. Conto cBot: FTMO 17188650, 100.000 USD, 0,1 contratti (0,096 su BP). Finestra comune
(ingressi) **31/08/2025 → 05/08/2026**. Tutto ciò che segue è **per contratto, in USD, sulla finestra comune**.

| | interno | cBot |
|---|---:|---:|
| trade totali | 3.474 | 3.438 |
| trade nella finestra comune | 3.267 | 3.438 |
| lordo | +39.496 | −104.392 |
| commissioni | 13.068 | 85.618 |
| netto | **+26.428** | **−196.183** |

## 1. Le strategie sono rispettate? Sì, e ora anche la gestione della posizione coincide

Il livello dei segnali torna:

- **2.975 coppie abbinate** su 3.267 trade interni nella finestra comune (91,1%); ingresso ≤ 1 min su 2.656, ≤ 5 min su 2.812;
- famiglie d'uscita mappate come previsto (BreakEven e TrailingStop → `LocalExit:StopLoss`, TimeExit e MaxBars → `LocalExit:Closed`);
- finestra operativa rispettata: interno 0 ingressi fuori, cBot 4 (le 23:05 UTC, §3 del report);
- limite ingressi per sessione: 6 sessioni oltre il massimo per lato, **identico** sui due lati;
- intent sani: 3.413 trade cBot su 3.438 hanno l'intent risolto nel log, 108 nascono da un intent in ritardo di almeno una barra;
- **2.076 trade identici** sui 3.730 distinti (55,7%): stessa strategia, verso, famiglia d'uscita, ingresso e uscita entro un minuto.

**L'inversione non c'è più.** Con il motore precedente l'interno chiudeva 95 trade nella finestra (109 sul
run) con `exitReason = OppositeSignal`; ora **zero**, e il report lo conferma: «Uscite per segnale opposto
nell'interno: 0». Le due MAC — le sole che possono uscire su incrocio inverso, via `ExitOnly` — non ne
avevano neanche prima: escono per stop e trailing (CL 19+16, FDAX 4+6), identiche prima e dopo la
correzione. Trade interni sul run intero: da 3.557 a 3.474.

**Chi aveva ragione: il cBot e il server**, come già argomentato. I trade di riferimento della ricerca (7
famiglie Python, 2.717 trade) non hanno una sola inversione; il dossier (§2.2) vuole *«al massimo una
entrata per sessione e per direzione»* e mentre si è in posizione la gamba opposta è bloccata. Il cBot
annulla l'intent (`HandleEntryIntent`, `alreadyOpenOnStrategy`), il server serializza con
`AccountHasEntryInFlight`; l'engine interno ora fa lo stesso (`PiootooTradingService.ProcessSignals` e
`TryFillPendingOrders`, regressione in `OppositeSignalDoesNotReverseTests`).

I trade non abbinati che restano, per causa (netto/ctr):

| lato | causa | trade | netto/ctr |
|---|---|---:|---:|
| solo interno | nessun evento nel log (livello non toccato sui tick, o intent non consegnato) | 182 | +34.188 |
| solo interno | cBot ancora in posizione (uscite sfasate, non inversione) | 90 | +29.932 |
| solo interno | cBot scartato: lato sbagliato | 20 | +21.664 |
| solo cBot | interno flat: livello non toccato sulle barre o scartato | 424 | +29.455 |
| solo cBot | interno ancora in posizione | 39 | −12.534 |

La voce «cBot già in posizione» non misura più un'inversione: con l'interno che non inverte, sono trade in
cui il cBot è uscito **dopo** l'interno (tipicamente uno stop più tardi o un target) e l'interno è rientrato
nel frattempo.

### 1b. BiasBarCount: il server perdeva la memoria della strategia (chiuso)

L'asimmetria di lato **non** era l'inversione, e non era un conteggio diverso delle barre fra i due engine.
Su `PTS_BTC_BIA_001_60` il **cBot** apre 143 Buy che l'interno non ha (+23.656), l'**interno** apre 101 Sell e
29 Buy che il cBot non ha (+5.730). Sull'intero run il cBot ha 303 trade BTC_BIA, **tutti Buy**; l'interno
197 Buy e 102 Sell.

**Cosa succedeva.** `BiasBarCountEngine` conta le barre della sessione in `_mycount`: il long si arma alla
barra 1 e vive fino alla 12, lo short si arma alla 10 e vive fino alla 21. In sessione cBot il contatore
ripartiva da zero a ogni valutazione e valeva sempre 1, cioè la barra di armamento del long. Il long si
riarmava e riemetteva lo stop a ogni barra, lo short non scattava mai. Le prove:

- nel `log.txt` gli intent BTC_BIA sono **solo `Buy Stop`**, da 46 a 124 per ciascuna delle 24 ore UTC; il
  `session-summary.json` conta 2.088 intent emessi, 303 riempiti e 1.659 cancellati per scadenza;
- tutti i 303 ingressi del cBot hanno `MaxBarsInPosition = 12`, cioè `14 − (_mycount + 1)` con
  `_mycount = 1`; nell'interno quel numero cambia con la barra di ingresso;
- gli abbinati sono tutti Buy entrati fra le 00 e le 11 UTC (la finestra long vera); i Buy solo cBot cadono
  fra le 11 e le 23, le stesse ore degli short solo interno.

**Dove si perdeva.** Due difetti sommati in `TradingSessionService`, mentre il backtest salvava la memoria
dopo ogni valutazione (`PiootooBacktestingService`):

1. `StrategyEvaluationService.Evaluate` scartava i `Hold` prima che qualcuno ne salvasse il `RuntimeState`, e
   `EvaluateClosedBar` lo salvava solo dai segnali restituiti: le barre senza ingresso, che sono quelle su cui
   i motori contano le barre e armano le direzioni, non aggiornavano la memoria;
2. in `ExternalBroker` `GetExecution` costruiva lo snapshot della strategia **senza** `RuntimeState`: anche lo
   stato salvato non tornava mai alla strategia.

I test del motore non l'avevano visto perché chiamano `GenerateSignal` sempre sulla stessa istanza, dove i
campi restano in memoria; nessuno passava da `Evaluate` dentro una sessione.

**Chi altro ne soffriva.** Tengono stato in campi privati anche `PriceChannelEngine`, `SessionBreakoutEngine`,
`VolatilityBreakoutEngine`, `RhlEngine` e `LevelFaderEngine`, ma in questa cartella abbinano fra il 96% e il
100%: ricalcolano quasi tutto dai dati a ogni barra, e il rientro che non bloccano più lo blocca già il
limite di ingressi per sessione. `PTS_YM_BIA_001_240` abbina il 98,8%: il suo long si arma sulla prima barra
di sessione, un percorso che non usa il contatore, e lo short non esce nemmeno nell'interno. Solo BTC_BIA,
dove il contatore **è** la regola di ingresso, cambiava strategia.

**Correzione** (`a764ef5`, installata il 13/09): `IStrategyEvaluationService` consegna la memoria di ogni
strategia valutata, `Hold` compresi; la sessione la salva per nome e simbolo della strategia e
`GetExecution` la rilegge anche in `ExternalBroker`. Regressione in `SessionStrategyRuntimeStateTests` e
`BiasBarCountEngineTests.ThroughTheEvaluationService_TheBarCountReachesTheTriggerBar`.

**Conseguenze per questa cartella.** I trade cBot di BTC_BIA vengono dal server con il difetto e non
descrivono la strategia: il confronto su BTC_BIA va rifatto con un nuovo run del cBot. Sui numeri corretti
dell'interno la strategia resta comunque fragile — 299 trade, netto con commissioni vere +7.954 $/ctr, profit
factor 1,10, drawdown 27.196, tutto il guadagno da 4 target — ed è stata **disabilitata** nei piani FTMO-44,
FTMO-55, FTMO-ALL e COMP-004X in attesa della verifica di parità con i trade della ricerca.

## 2. Architettura: i segnali sono autocontenuti, l'engine è solo esecutore

**La regola è quella dichiarata del sistema, e ora la rispettano tutti e tre i percorsi.** `TradeSignal` porta in
testa l'invariante (`Piootoo.Shared/Models/TradeSignal.cs:9-16`): *"un segnale di ingresso deve descrivere
per intero come si esce"* — `StopLoss`, `TakeProfit`, `TrailingStopMoneyPerFutureContract`, `BreakEven`,
`CloseAtUtc`, `MaxBarsInPosition`, `ProfitStallAfterUtc`, `TimeExitOnlyIfProfitBelow…`. Il cBot lo conferma
nel proprio header e sorveglia trailing, breakeven, CloseAt, ProfitStall e MaxBars leggendo **solo** i campi
dell'intent. Le 103 classi `PTS_*` dichiarano i numeri, l'esecutore li applica.

**`ExitOnly` è l'unica scappatoia prevista, ed è quella giusta** (`TradeSignal.cs:39-44`): per un'uscita
osservabile solo a runtime (l'incrocio inverso di MAC) il segnale porta `ExitOnly = true` e chiude la
posizione senza aprirne una nuova. La condizione resta nella strategia, l'engine la riceve nel segnale.

**La violazione trovata è corretta.** L'inversione in `PiootooTradingService` era una regola d'uscita cablata
nell'esecutore, che non passava da nessun campo del segnale. Con `7c49e99`:

1. in `ProcessSignals` un ingresso opposto a mercato mentre si è in posizione non chiude più;
2. uno stop opposto non resta pending "per un eventuale reverse fill";
3. nei due rami di fill dei pending, un pending toccato con una posizione aperta sulla stessa strategia viene cancellato, non usato per invertire;
4. resta solo il ramo `ExitOnly`.

## 3. Scomposizione del divario

Finestra comune 31/08/2025 → 05/08/2026. Netto/ctr interno +26.428, cBot −196.183, divario **−222.611**. Si
divide in:

| voce | Δ $/ctr | quota | natura |
|---|---:|---:|---|
| lordo (prezzi ed esiti) | −143.888 | 64,6% | esecuzione + logica |
| commissioni | −72.550 | 32,6% | modello (l'interno usa 4,0 $/trade piatti) |
| swap | −6.173 | 2,8% | costo reale non modellato dall'interno |

Il lordo, per blocco:

| blocco | n | Δ lordo $/ctr | Δ netto $/ctr | natura |
|---|---:|---:|---:|---|
| A — abbinati, stessa uscita (≤ 1 min) | 2.305 | −44.328 | −102.679 | **esecuzione (spread)** |
| B — abbinati, uscita diversa | 670 | −38.094 | −51.069 | granularità barra/tick, esiti opposti |
| C — solo cBot | 463 | +25.486 | +16.921 | livelli toccati sui tick e non sulle barre, BiasBarCount |
| D — solo interno (contributo) | 292 | −86.953 | −85.785 | livelli non toccati sui tick, BiasBarCount |

Le somme quadrano: −44.328 − 38.094 + 25.486 − 86.953 = **−143.888** di lordo; −102.679 − 51.069 + 16.921 −
85.785 = **−222.612** di netto. Il calcolo è in `script/scomposizione.ps1` (fuori da git, come i CSV).

Nel blocco B pesano poche coppie: 21 in cui l'interno esce per stop e il cBot va a target (+75.930) e 7 in cui
l'interno va a target e il cBot prende lo stop (−72.006); più 532 coppie in cui il cBot esce per stop dove
l'interno no, contro 23 al contrario.

## 4. I costi reali (domanda 2)

**Costo esplicito — commissioni + swap.** Sul conto vero il saldo finale è 81.777,98 su 100.000 (netto dei
trade −19.648,60). Per contratto sulla finestra comune le commissioni sono 85.618 contro le 13.068 dell'interno,
e lo swap è un costo che l'interno non modella affatto. Commissioni vere per trade e contratto (§0b del report):

| simbolo | cBot comm/trade/ctr | interno |
|---|---:|---:|
| FDAX | 45,6 | 4,0 |
| NQ | 35,4 | 4,0 |
| GC | 26,8 | 4,0 |
| BTC | 23,6 | 4,0 |
| ES | 23,0 | 4,0 |
| YM | 16,0 | 4,0 |
| CL | 5,2 | 4,0 |
| BP | 5,0 | 4,0 |
| CC | 4,0 | 4,0 |
| CT | 2,8 | 4,0 |
| NG | 1,8 | 4,0 |
| SB | 1,2 | 4,0 |

Su BTC le commissioni rovesciano il segno: lordo cBot +9.652, netto −6.013.

**Costo implicito — spread ai fill, ora misurato dal log.** Il costo dello spread pagato dal cBot vale
**270.921 $/ctr** sul run (§5 del report): senza spread il netto cBot sarebbe +74.738 invece di −196.183. Sui
2.305 abbinati con la stessa uscita il cBot perde **−19,2 $/trade** di lordo, **−35,9** sulle uscite per stop;
per simbolo BTC −44,4, FDAX −71,6 (qui pesa anche il cambio), GC −33,3, NQ −11,1, ES −15,2, YM +0,2. Lo stop
dell'interno si riempie esattamente al livello (rapporto perdita/stop 1,00 su ogni simbolo, §2d); quello del
cBot oltre il livello su BTC (mediano 1,05, 116 stop oltre 1,1), GC e NQ (1,01), FDAX 1,18 per il cambio.
L'interno addebita lo spread una volta, all'ingresso; il conto lo paga anche in uscita, più la coda dei gap.
Bracket riancorati al fill: 140, slittamento mediano 0,125 punti, 1.908 $/ctr in tutto.

**NG** è il caso limite dell'altro costo, lo spread contro lo stop: 352 trade cBot, −152.154 di netto, e nove
strategie NG con spread/stop fra il 27% e il 197% (`PTS_NG_TFM_002_240` ha lo stop sotto lo spread).

## Aperto

1. **BTC_BIA da rimisurare e da validare** (§1b): la causa dell'asimmetria è chiusa, ma il confronto va rifatto con un nuovo run del cBot sul server corretto (la strategia è disabilitata in COMP-004X: va riaccesa per quel run). E va verificata la parità degli ingressi con i trade della ricerca (`BTC_1h/consegna/trades/fam03_BIAS.csv`, non nel repository): 27 $/trade realizzati contro 741 attesi sono troppo lontani per escludere un errore di porting.
2. **Le 28 coppie con esito opposto sullo stesso ingresso** (7 TakeProfit→Stop, 21 Stop→TakeProfit, ±72-76 k): risoluzione intrabarra dell'interno (`ProtectiveBeforeTarget` su barre da 1 min) contro i tick del cBot. Decidere se l'approssimazione a barre è accettabile.
3. **I 4 ingressi cBot fuori finestra alle 23:05 UTC** (interno 0): confine di giornata, etichettatura della barra.
4. **Commissioni per simbolo e swap nel modello dell'interno**: con 4,0 $ piatti il netto per strategia dell'interno non è confrontabile con il conto.
5. **Spread in uscita**: l'interno lo paga solo all'ingresso e riempie lo stop al livello; il conto no.
6. **FDAX interno non convertito**: 25 EUR/punto come dollari, rapporto lordo mediano 1,171 su 82 coppie. Difetto dell'**interno**.
7. **NG**: spread vicino o sopra lo stop su nove strategie; va deciso se tenerle nel piano.

## Chiuso (con prova)

- **L'inversione era un difetto dell'engine interno, ed è corretta** — 0 inversioni su 2.717 trade di riferimento della ricerca; cBot e server bloccano; con `7c49e99` l'interno passa da 95 uscite `OppositeSignal` nella finestra (109 sul run) a 0, MAC comprese, che non ne avevano; `OppositeSignalDoesNotReverseTests`.
- **L'architettura del disaccoppiamento regge** per SL/TP/trailing/breakeven/tempo: sono tutti nel `TradeSignal` e l'esecutore li applica soltanto. L'unica violazione trovata, l'inversione cablata nell'interno, non c'è più.
- **`ExitOnly` è la scappatoia giusta e sufficiente** per le uscite runtime (MAC): la condizione resta nella strategia.
- **L'asimmetria di BiasBarCount era il server che perdeva la memoria delle strategie in sessione** — intent BTC_BIA solo `Buy Stop` a ogni ora, `MaxBarsInPosition` sempre 12, zero short; corretto con `a764ef5`, `SessionStrategyRuntimeStateTests` (§1b).
- **Le famiglie d'uscita corrispondono**; i segnali sparano sulle stesse barre (89% ≤ 1 min sugli abbinati); il limite per sessione è identico; l'interno non riempie su minuti senza barra (0).
- **Lo spread modellato dall'interno è corretto per l'ingresso**: il costo che manca è quello pagato in uscita e sui gap, ora misurato dal log (270.921 $/ctr sul run).
- Le somme quadrano: −143.888 − 72.550 − 6.173 = **−222.611**.

## Trappole di misura di questa cartella

1. **Due run interni.** I file `*-interno-cfd-FTMO.json` in radice sono il run 7.4.0 senza inversione; quelli del run 7.3.0 stanno in `interno-motore-precedente/` (in git solo summary e `run-*.json`, non i trade). I numeri del primo esito, calcolati sul run 7.3.0 e sull'export cBot corto, non sono confrontabili con questi.
2. **Finestra comune fino al 05/08/2026**: il run cBot si ferma al 04/08, l'interno al 30/08. Ogni totale va preso sulla finestra comune; §6 del report (curve crescenti) usa invece i run interi e le due colonne non sono confrontabili.
3. **Commissioni interne 4,0 piatte** su ogni simbolo (reali 1,2–45,6): ogni confronto di *netto* per strategia è viziato in proporzione alla frequenza. Confrontare il lordo.
4. **Swap in §0 del report grezzo (−616), in questo esito per contratto (−6.173)**: non sommarli com'è.
5. **Saldi grezzi non confrontabili** (100k/0,1 ctr contro 1M/1 ctr): solo i valori per contratto sulla finestra comune.
6. **«cBot già in posizione (nessuna inversione)»** nel report è l'etichetta storica della causa: con l'interno corretto indica uscite sfasate, non inversioni.
7. **I trade cBot di `PTS_BTC_BIA_001_60` non descrivono la strategia**: vengono dal server che perdeva la memoria delle strategie (§1b). Contano nel totale e nei blocchi C e D della §3, ma non vanno letti come il comportamento di BTC_BIA; gli altri motori con stato non ne risultano toccati.
