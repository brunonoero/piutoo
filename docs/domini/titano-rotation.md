# Titano rotation

Dal 31/07/2026 esiste un solo Titano: questo. Il vecchio filtro settimanale binario
(`TitanoFilterService`) è stato rimosso — vedi `../decisioni.md`. Analisi completa dei
parametri, con esempi numerici e audit, in
`../titano-analisi-parametri-e-audit-2026-07-31.md`.

Titano è un filtro **Titan-like** sulle equity line delle singole strategie. Non
è, e non viene dichiarato, una riproduzione dell'algoritmo proprietario Unger.
Legge esclusivamente `trades.json`, non invoca `ExecutionEngine`, non rigenera
segnali e non modifica `masterfilter.json`.

## Equity line e formule

Per ogni `StrategyCode`, ordinando i trade chiusi per `(ExitTimeUtc, TradeId)`:

- `E(0) = InitialCapital`;
- `E(i) = E(i-1) + NetProfit(i)`;
- `r(i) = NetProfit(i) / abs(E(i-1))` (zero se il denominatore è zero);
- performance finestra `W`: `(E(now) - E(now-W)) / abs(E(now-W))`;
- media mobile: media aritmetica dei punti equity nella finestra configurata;
- deviazione standard equity: `sqrt(mean((E(i)-mean(E))^2))`;
- z-score: `(E(now)-mediaMobile)/deviazioneStandard` (zero se deviazione zero); il
  relativo voto vale 1 al centro della banda ammessa e degrada a 0 ai suoi estremi —
  il centro è il punto migliore, perché un z troppo alto è surriscaldamento;
- drawdown corrente: `(peak-E(now))/abs(peak)`;
- drawdown massimo: massimo drawdown osservato sull'intera storia disponibile;
- volatilità: deviazione standard popolazione dei rendimenti trade nella finestra breve.

Default: finestra breve 90 giorni, lunga 365, media mobile 90; return breve
e lungo minimi 0%; z-score tra -1,5 e +2,5 (anche l'eccesso positivo disabilita);
drawdown corrente massimo 15%, storico 25%, volatilità massima 10%. L'equity
deve essere sopra la media mobile e deve esserci almeno un trade breve. Tutte le
regole sono configurabili e ogni decisione contiene metriche e motivi completi.

## Anti-whipsaw, voto e sizing

La decisione è stateful. OFF usa `disableCompositeScore` e il limite drawdown;
ON richiede le soglie più severe `reenableCompositeScore` e
`reenableMaximumCurrentDrawdown`, oltre al cooldown. `minimumOnPeriods` impedisce
un OFF precoce, ma non prevale sul hard stop. I cinque voti indipendenti sono
performance breve, performance lunga, z-score, drawdown e volatilità; servono
almeno `minimumPassingFilters` voti. Score e dettaglio di ogni voto sono nel
manifest.

## Sizing per percentile

Il sizing di default (`crossSectionalSizing`) usa il **rango** della strategia fra
quelle dello stesso periodo, non un giudizio assoluto: per ciascun voto si calcola
il percentile (rango medio per i pari merito) e lo score è la media dei percentili.
L'allocazione è poi una curva continua fra `minimumAllocationMultiplier` (0,25 di
default) e `maximumAllocationMultiplier` (1), arrotondata ad `allocationStep` (0,05).
Lo stato `Enabled` significa "al tetto configurato", non "moltiplicatore uguale a 1":
con `maximumAllocationMultiplier = 0,80` il pieno regime è 0,80.

Il motivo è la scala dei voti assoluti. Misurati su un run reale a 52 periodi, i
voti stanno quasi fermi: performance lunga fra 0,499 e 0,556, drawdown fra 0,913 e
1, volatilità fra 0,962 e 1, z-score binario. Lo score composito ne risultava
compresso fra 0,595 e 0,808 — e con soglie tier a 0,80/0,60/0,40 l'81% delle
assegnazioni finiva sullo stesso scaglione al 50%. Il percentile restituisce a
ciascun voto l'intera scala 0..1 a prescindere da quanto vari in assoluto.

**L'accensione e lo spegnimento non passano dal percentile.** Restano governati dai
cancelli assoluti — filtri minimi superati, drawdown corrente, hard stop, isteresi,
cooldown — perché un rango dice "è la peggiore del gruppo", non "va male": usarlo
per spegnere significherebbe spegnere sempre qualcuno, anche in un periodo in cui
vanno tutte bene. Il percentile decide solo *quanto* allocare a una strategia già
ritenuta eleggibile. `rawScore` conserva la media dei voti assoluti, così un
percentile basso resta distinguibile da un peggioramento reale.

Con `crossSectionalSizing = false` si torna al comportamento storico: media dei voti
assoluti mappata sui `sizingTiers`. Lo score seleziona deterministicamente il primo
tier ordinato per soglia decrescente; i default sono 100%, 50%, 25%, 0%. In quella
modalità `disableCompositeScore` e `reenableCompositeScore` tornano a partecipare
all'accensione. `Enabled` resta equivalente a `AllocationMultiplier > 0`. In offline `NetProfit` è moltiplicato per
l'allocazione e si sottraggono
`(CommissionPerUnit + SlippagePerUnit) * Quantity * AllocationMultiplier`.
Non vengono inventati fill. Live/server arrotondano la quantità per difetto a
`QuantityStep`; sotto `MinimumIntentQuantity` l'intent di apertura è scartato.
Gli intent di chiusura non sono ridotti, per evitare posizioni residue.

Il hard stop usa una soglia drawdown assoluta, è latched e non si azzera da
solo; non arma il cooldown, che per una strategia bloccata non avrebbe significato.
Il reset è un evento audit separato e immutabile, efficace soltanto dal
periodo successivo; il manifest storico non viene riscritto.

**Il reset toglie il latch, non riabilita.** `Resolve` riapplica alla strategia la
stessa condizione `reenable` della rotazione, rivalutata sui dati già persistiti nello
stato — voti superati, drawdown corrente, cooldown residuo: se non è soddisfatta la
strategia resta a zero e rientrerà dalla prossima rotazione calcolata. Ricalcolare
l'allocazione dal solo score significherebbe riammetterla al pavimento (0,25) anche con
zero voti superati e drawdown al 60%.

## Calendario e no-look-ahead

Tutti gli intervalli sono UTC, con estremi `[inizio, fine)`.

- `Weekly`: settimana ISO, lunedì 00:00 UTC.
- `Biweekly`: blocchi di 14 giorni ancorati a `biweeklyAnchorUtc`.
- `Monthly`: mese di calendario, giorno 1 alle 00:00 UTC.

I trade chiusi prima della fine del periodo N producono metriche e decisioni applicabili solo nel
periodo N+1. Per il ricalcolo offline, un trade viene incluso usando la decisione
efficace al suo `entryTimeUtc`; il relativo `netProfit` viene contabilizzato a
`exitTimeUtc`. Signal rejected/cancelled/non-filled non entrano mai nel calcolo.

## Artefatti

Ogni run è scritto in:

`<workspace>/backtests/<backtest>/titano/<run-id>/`

Il `run-id` deriva deterministicamente da config, SHA-256 di `trades.json` e
master filter. Una richiesta identica restituisce il manifest esistente. I file
sono creati atomicamente e non sovrascritti:

- `manifest.json`: schema versionato, config, hash sorgenti, tutti i periodi,
  decisioni complete e curva balance/equity filtrata. Contiene anche
  `tradesOutsideCoverage` — i trade entrati fuori dai periodi efficaci, presenti nella
  curva originale e assenti da quella filtrata, senza i quali il confronto fra le due
  sembra a parità di campione e non lo è — e `walkForwardNote`, che spiega un
  walk-forward vuoto o parziale invece di lasciare una tabella muta;
- `period-<effective-start>-<effective-end>.json`: snapshot immutabile ON/OFF.

Ogni strategia contiene `strategyCode`, `enabled`, score, motivo e metriche.
Record legacy senza `StrategyCode` vengono rifiutati esplicitamente.
Contiene inoltre stato, multiplier, cooldown, contatori, hard stop, voti,
hash della configurazione, costi e risultati walk-forward IS/OOS.

La configurazione efficace è sempre:

`WorkspaceMasterFilter ∩ TitanoEnabledStrategies(timestampUtc)`

Pertanto Titano non può abilitare strategie esterne al master filter.

## Risoluzione automatica dell'ultimo run e freschezza

Un piano di trading non indica più un `TitanoRunId`: referenzia solo una cartella di backtest
(`TradingGroupRow.TitanoBacktestFolder`), e ogni sessione risolve **al momento** l'ultimo run
generato per quella cartella (`TitanoRotationService.ResolveLatestRun`, basato su
`ListRuns(...).FirstOrDefault()`, già ordinato per `GeneratedAtUtc` decrescente). La risoluzione
avviene a ogni barra, non una volta sola all'apertura: una sessione già aperta applica un run più
recente dal bar successivo alla sua generazione, senza bisogno di riaprirla.

`GetFreshness(workspaceId, backtestFolder)` classifica l'ultimo run in tre stati
(`TitanoRotationFreshness`):

- **Fresh** — `DateTime.UtcNow` è ancora dentro il periodo coperto dall'ultima decisione
  (`EffectiveToUtc` dell'ultimo `TitanoRotationDecision`).
- **Stale** — `now` ha già superato `EffectiveToUtc`: il sistema sta congelando un periodo per cui
  Titano non ha mai deciso nulla (vedi `Resolve`, `UsedLatestPeriod`). Non è un errore — il run
  resta applicabile in `TitanoFilterMode.Realtime` — ma segnala che serve un nuovo backtest
  campione e una nuova rotazione.
- **NoRun** — la cartella non ha ancora un run.

La soglia è rigida: appena si entra nel periodo successivo all'ultimo calcolato, lo stato passa a
Stale, senza tolleranza di un periodo extra. Esposto da
`GET .../trading-plans/{code}/rotation-status` (`TitanoRotationStatus`) e mostrato in sola lettura
nella lista e nel dettaglio dei piani.

Il percorso non-piano (`CreateTradingSessionRequest.TitanoRunId`, sessioni create senza
`open-plan`) resta un pin esplicito opzionale, per test e integrazioni che vogliono un run fisso.

## API e cTrader

- `POST /api/Titano/rotations`
- `GET /api/Titano/rotations?workspaceId=...&backtestFolder=...`
- `GET /api/Titano/rotations/{runId}?workspaceId=...&backtestFolder=...`
- `GET /api/Titano/rotations/{runId}/manifest?...`
- `GET /api/Titano/rotations/{runId}/effective-strategies?workspaceId=...&backtestFolder=...&timestampUtc=...`
- `POST /api/Titano/rotations/{runId}/hard-stop-reset?...`

FeedRunner può risolvere l'ultimo endpoint per il timestamp barra. In alternativa
una sessione `ServerSimulated` o `ExternalBroker` può specificare `titanoRunId` e
`titanoBacktestFolder`: il server applica l'intersezione prima di
`StrategyEvaluationService`. Se non esiste ancora una decisione efficace (warmup
o timestamp fuori run), l'insieme Titano è vuoto e non viene valutata alcuna
strategia.

## Contratto comune cross-engine

Titano non conosce l'esecutore. Backtest interno, backtest cTrader e live cTrader
producono gli stessi `signals.json` e `trades.json`; Titano usa solo trade chiusi
con `StrategyCode` non vuoto. In `ExternalBroker` un trade nasce esclusivamente
dai fill di chiusura autorevoli; la commissione dell'execution report viene
sottratta dal profitto lordo per ottenere `NetProfit`. L'applicazione prima della
valutazione strategia evita una seconda esecuzione o replay dei trade.

## Walk-forward e limiti

Le finestre di calibrazione e valutazione sono rolling o expanding. Le metriche
IS e OOS sono salvate separatamente; viene segnalato il caso in cui il filtro
migliora solo IS e quello in cui l'ultima finestra OOS è troncata perché il run
finisce prima (`evaluationTruncated`). L'OOS non è usato per scegliere soglie; eventuali sweep
parametrici devono restare processi separati.

Isteresi e cooldown riducono il flip-flop ma aumentano il ritardo; tier graduali
riducono gli shock di allocazione ma non eliminano rischio, slippage o gap.
I default sono conservativi, non costituiscono garanzia finanziaria e Titano non
è presentato come algoritmo proprietario Unger.

Titano controlla esclusivamente il coefficiente lento
`StrategyEquityMultiplier`. Nel backtest interno `BacktestRotationFile` il
moltiplicatore scala la `Quantity` del segnale prima dell'engine, con la stessa
semantica economica delle sessioni. ATR/target volatility e rischio di portafoglio sono
servizi veloci separati, applicati per barra al confine di esecuzione. Separare
questi orizzonti evita di ottimizzare Titano sulle oscillazioni di mercato
intra-periodo, ma non elimina overfitting: soglie, finestre e target devono
essere validati fuori campione senza usare OOS per la selezione.

## Come i parametri arrivano all'utente

La schermata *Setup Titano* della console non presenta i trenta parametri sullo
stesso piano. Il livello di ciascuno è un attributo sul modello
(`TitanoLevelAttribute` in `Piootoo.Shared/Models/Optimization/TitanoParameterMetadata.cs`),
e il `PropertyGrid` filtra su quello: la vista predefinita mostra i dieci
parametri **Base** — cadenza, finestra breve, voti richiesti, le tre soglie di
drawdown, il fermo dopo un OFF, la scelta del sizing e i due estremi di
allocazione — mentre gli **Avanzati** restano dietro una spunta.

Le frazioni si inseriscono e si leggono come percentuali
(`PercentTypeConverter`): il modello e il contratto verso il server restano in
frazioni, la serializzazione JSON non passa dai `TypeConverter`.

Sotto il grid, `TitanoSetupSummary` riscrive la configurazione corrente in prosa
e ne elenca le incoerenze. Serve perché i parametri di Titano non si giudicano
uno alla volta: la soglia di rientro ha senso solo relativamente a quella di
uscita, e la finestra di misura solo relativamente alla cadenza — la trappola
descritta in `../titano-analisi-parametri-e-audit-2026-07-31.md` §1.2. Lo stesso
riquadro dichiara i parametri che nella configurazione scelta non hanno alcun
effetto, che è la coda di **B3**: nel `PropertyGrid` non si possono disabilitare
per valore come si faceva nella vecchia form a `NumericUpDown`.

Riferimenti codice: `piootooapp.clientform/Shell/Screens/TitanoSetupDetailScreen.cs`,
`piootooapp.clientform/Shell/Screens/TitanoSetupSummary.cs`,
`Piootoo.Strategies.Tests/TitanoSetupUiTests.cs`.
