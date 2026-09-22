# Overnight e overweek: chi decide se una posizione resta aperta

Tre soggetti hanno un'opinione su quanto a lungo una posizione può restare
aperta: il **motore** che genera la strategia, la **strategia** che ne dichiara
i parametri, e il **piano** che la esegue su un conto vero. Questo documento
dice in che ordine parlano e dove ciascuna opinione vive nel codice.

## 1. La gerarchia

> **Decide prima il piano.** Se il piano concede di tenere, allora decidono
> motore e strategia. In una riga: `tiene = pianoPermette && strategiaVuole`.

Due corollari, che sono invarianti e non conseguenze da ricavare:

- **Il piano non può forzare un overnight che la strategia non vuole.** Un
  permesso non è un obbligo: è la strategia a sapere quando la sua edge muore.
  Una intraday su un piano permissivo resta intraday.
- **La strategia non può prendersi un overnight che il piano vieta.** Un conto
  prop che impone il flat taglia a prescindere, e il trade risultante non è più
  quello che la ricerca ha misurato — ma è quello che il conto avrebbe fatto.

Operativamente il taglio è **meccanico: vince la scadenza più stretta**. La
strategia porta la propria (`CloseAtUtc`, che può non esserci), il piano porta
la sua quando vieta l'overnight, e la posizione muore alla prima delle due.
La dichiarazione della strategia non entra nel calcolo del taglio: serve a
*mostrare in anticipo* chi verrà tagliato (vedi §5).

## 2. Cosa dichiara la strategia

`ITradingStrategy.Holding` restituisce uno `StrategyHolding(Overnight,
Overweek)`. Il default dell'interfaccia è `Multiday`, perché una strategia che
non dichiara nulla non emette alcuna uscita a tempo e quindi *di fatto* tiene.

I motori Easy lo derivano da `AppliesSessionExit`, che a sua volta i sei motori
con `intraday_only` (TF, PC, RHL, SBO, VBO, RBB) inoltrano su
`SessionExitFromIntradayOnly`. BIAS, BIASW e MAC dichiarano l'uscita in altro
modo e restano `Multiday`.

`IntradayOnly` è dichiarato **una volta sola**, in `EasyEngineBase`. Prima
viveva in sei copie, e le sei copie non dicevano la stessa cosa: cinque motori
scrivevano `IntradayOnly && TimeframeMinutes < 1440` nel proprio corpo, RBB no.
La stessa dichiarazione valeva o non valeva a seconda di chi la leggeva.

### La regola di parità daily

`SessionExitFromIntradayOnly` **disattiva l'uscita di sessione su D1**, anche
quando la classe dichiara `intraday_only = 1`. Non è una deduzione dal
timeframe: è il comportamento del motore di ricerca, che su daily quell'uscita
non la applica. Applicarla renderebbe il porting non confrontabile con il
report da cui nasce.

Il ramo è **inerte sul catalogo attuale**: tutte e dieci le strategie a 1440
dichiarano già `IntradayOnly = false`.
`HoldingPolicyTests.LeStrategieDailyDelCatalogoNonDipendonoDallEsenzioneD1`
tiene ferma quella condizione — se comparisse una daily che si affida
all'esenzione, la sua tenuta sarebbe decisa dal timeframe invece che dal report
della ricerca, ed è il tipo di cosa che si scopre altrimenti solo confrontando
liste di trade.

`StrategyHolding` non entra mai nel segnale: viaggia nel catalogo
(`StrategyCatalogItem.Overnight/Overweek/HoldingLabel`) e nel descriptor di
sessione (`TradingSessionStrategyInfo.Holding`).

## 3. Cosa dichiara il piano

`TradingPlan.Holding` è un `AccountHoldingPolicy` con cinque campi:

| Campo | Significato |
|---|---|
| `AllowOvernight` | Il conto può restare in posizione oltre la fine sessione. |
| `SessionFlatUtc` | Orario UTC del flat giornaliero (`TimeOnly`, sul filo `HH:mm:ss`), usato solo se l'overnight è vietato. |
| `SessionFlatWindowMinutes` | Durata in minuti della finestra di flat `[SessionFlatUtc, SessionFlatUtc + minuti)`, default 30. Dentro non nascono ingressi e i pending si cancellano. `SessionFlatUntilUtc` è derivata e non si salva. |
| `AllowOverweek` | Il conto può attraversare il fine settimana. |
| `WeekEnd` | La finestra di flat del weekend (`WeekEndFlatPolicy`), usata solo se l'overweek è vietato. |

Il default — `AllowOvernight = true`, `AllowOverweek = false` — riproduce
esattamente il comportamento storico del sistema, quindi nessun piano già
scritto cambia comportamento. I `plans.json` anteriori portano `WeekEndFlat` a
primo livello: `TradingPlanService.ResolveLoadedHolding` lo travasa dentro
`Holding` alla lettura e non lo riscrive più.

`Validate()` rifiuta **overweek senza overnight**: tenere il fine settimana è un
caso particolare di tenere oltre la sessione, e la combinazione contraria non
descrive alcun conto reale. Rifiuta anche una finestra di flat **fuori da 1–720
minuti**: vuota riaprirebbe in silenzio il buco fra flat e rollover, di mezza
giornata terrebbe il conto fermo per ore. Il rifiuto è al salvataggio del piano e
nella richiesta di backtest, non risolto in silenzio a valle.

### Perché l'ora del taglio è del piano

Per la stessa ragione per cui lo è già il flat del fine settimana (vedi la nota
di tipo su `WeekEndFlatPolicy`): la prop dice «piatto alle 20:45 UTC», non
«piatto alla fine della sessione del motore TF». Se ogni strategia tagliasse
alla propria fine sessione, il conto non sarebbe piatto in nessun istante — che
è esattamente ciò che il vincolo chiede di garantire.

### Perché il flat di sessione è una finestra e non un istante

Un ordine che nasce dopo il flat riceve la deadline del giorno *successivo*, non
una già passata: per convenzione CME la giornata di trading va dalla riapertura
al settlement successivo, quindi «flat alle 20:45 ogni giorno» significa non
portare mai una posizione attraverso un settlement. Fino al 22/09/2026 il flat
era il solo istante, e la promessa aveva un buco: un ordine valido **fra il flat
e il rollover del broker** — la barra delle 20:45 di una strategia a 15 minuti,
o uno stop ancora vivo che si riempie alle 20:50 — riceveva la deadline del
giorno dopo e attraversava proprio il rollover che il flat esiste per evitare.

Da allora il flat è una finestra `[SessionFlatUtc, SessionFlatUtc +
SessionFlatWindowMinutes)`, con la stessa forma di quella del fine settimana:
dentro, il conto è piatto, i pending si cancellano e **gli ingressi non
nascono**. Il fine della finestra va messo **oltre il rollover**: 30 minuti dalle
20:45 sono le 21:15, contro un rollover alle 20:59 (FTMO) o 21:00 (ICS).

La durata è un numero e non un'ora di fine perché un'ora di fine può cadere
prima dell'inizio — un `plans.json` con il flat alle 21:30 e un default alle
21:15 — e la finestra si rovescerebbe in silenzio. Una durata non può.

**È questo il parametro che tiene le strategie lontane dal rollover senza
riscriverle.** `PT3B_FDAX_PCH_002_240` fa la stessa cosa con `SessionExitTime`
dentro la classe, alle 21:00 di Roma; il piano lo fa per tutte le strategie che
esegue, a un'ora UTC fissa, e lascia rientrare dopo la finestra fino a fine
sessione. Stesso costo evitato, trade in parte diversi: un piano che vieta
l'overnight non misura la 002, misura un'altra tenuta, e va rimisurato. Le
strategie **non guardano il listino dello swap**: il flat è del conto, il
rollover è del broker, e a tenerli d'accordo c'è il controllo del §4.

## 4. Dove si risolve la gerarchia

In un punto solo: `HoldingResolver.Resolve(strategyCloseAtUtc, referenceUtc,
policy)`, in `Piootoo.Shared`. Restituisce la deadline effettiva e **chi l'ha
imposta**.

Lo chiamano tre motori:

- il **backtest interno**, in `PiootooBacktestingService.ApplyAccountHolding`,
  sul segnale appena prodotto e *prima* di persisterlo — così `signals.json`
  riporta la deadline che verrà davvero eseguita;
- la **sweep** (`SweepRunner.Evaluate`), con la policy che `piootoo-sweep`
  riceve da `--flat-utc HH:mm [--flat-window minuti]`; senza l'opzione la
  ricerca gira a overnight e overweek liberi, la parità con il motore Python.
  Cercare con una tenuta e operare con un'altra valida una strategia diversa da
  quella che si opera, ed è per questo che l'opzione esiste;
- la **sessione**, dove costruisce l'`OrderIntent`: il cBot riceve
  `TimeExitUtc` già composto e non sa nulla della gerarchia.

Accanto alla deadline c'è lo **scarto**: `HoldingResolver.BlocksEntry` dice se
un ingresso valido a quell'istante nasce dentro una finestra di flat del conto
— quella giornaliera se l'overnight è vietato, quella del fine settimana se lo è
l'overweek — e in quel caso il segnale non diventa né ordine né intent. Lo
chiedono gli stessi tre motori tramite
`PiootooBacktestingService.IsBlockedByAccountFlat`, che lascia sempre passare le
uscite (`ExitOnly`). Il cBot ripete il controllo in `HandleEntryIntent` come
ultima barriera, com'era già per il fine settimana.

Le **finestre** stesse — cancellare i pending e chiudere ciò che resta sul primo
tick dentro — le applicano il loop di backtest e la sweep (`IsFlatTrigger`,
`IsSessionFlatTrigger`) e il cBot (`EnforceWeekEndFlat`, `EnforceSessionFlat`,
insieme in `EnforceAccountFlat`). Sul flat giornaliero le posizioni portano già
la deadline, quindi la finestra chiude di norma solo i pending: è la rete di
sicurezza a server muto.

Una regola di composizione implementata due volte è una regola che prima o poi
diverge: è l'errore già pagato il 26/08/2026 sull'orario del flat del venerdì,
quando il backtest chiudeva alle 23:30 e il conto vero alle 20:45.

### Il controllo sul rollover

Il flat promette di non attraversare il rollover, e la promessa regge solo se il
rollover del broker cade **dentro** la finestra. All'avvio di un backtest con
`SwapBroker`, e di una sweep con `--swap-broker`, `HoldingResolver.
RolloversOutsideSessionFlatWindow` confronta `SwapSpec.RolloverUtc` di ogni
simbolo con la finestra del piano; un simbolo fuori produce una riga
`[rollover]` in testa a `diagnostics` del summary, nel log di avvio e nel
resoconto della sweep. È un avviso e non un errore, perché misurare un flat
*dopo* il rollover può essere proprio l'esperimento — ma il run deve dirlo.

## 5. Dove si vede

**Catalogo strategie** (console, *Strategie*): colonna **Tenuta**, con
`intraday` / `overnight` / `overnight+overweek`, filtrabile. La scheda della
singola strategia riporta la stessa cosa per esteso. Il masterfilter del
workspace mostra la tenuta accanto a ogni voce spuntabile, perché è lì che si
sceglie.

**Dettaglio piano**, tab *Chiusure forzate*: i cinque campi della policy — il
flat con l'ora e la durata della finestra — e, sotto, l'**avviso** con l'elenco
delle strategie del masterfilter che quel piano taglierebbe — quante, quali, e
con che effetto. L'elenco lo calcola `HoldingResolver.FindConflicts`, cioè la
stessa regola che poi esegue il motore.

**Pannello a chart del cBot**, riga `Tenuta:` **subito sotto il nome del piano**,
perché è una proprietà del piano ed è la riga che spiega uscite che altrimenti
sembrano della strategia. Ogni strategia elencata porta il proprio marcatore:
`[intraday]`, `[multiday]`, `[multiday, flat weekend]`, `[multiday TRONCATA]`.

**Summary di backtest**: `holding` in testa, e tre righe di diagnostica
distinte — `[fine settimana]` per il flat del weekend, `[fine sessione]` per il
troncamento imposto dal piano, `[rollover]` quando la finestra non copre il
rollover di un simbolo. Il log di avvio dichiara `sessionFlatFromUtc` e
`sessionFlatUntilUtc`, quello di fine run quanti ingressi la finestra ha
impedito (`flatWindowBlockedSignals`) e quanti pending ha cancellato. Due run
con permessi, orari o finestre diverse non sono confrontabili, e questa è
l'unica traccia che lo dice a chi li rilegge.

**Trade**: l'uscita imposta dal conto è `TradeExitReason.SessionFlat`, distinta
da `TimeExit`. Sommarle renderebbe invisibile la differenza fra ciò che la
strategia misura e ciò che il conto le concede.

## 6. Il cBot non ha più parametri su questo

`FlatAtWeekEnd`, `WeekEndFlatFromUtc` e `WeekEndFlatUntilUtc` sono stati
**rimossi** da entrambi gli execution bot. Finché sono vissuti lì, il bot poteva
contraddire il piano che diceva di eseguire: un parametro spento a mano operava
il venerdì sera contro un backtest che quei trade li tagliava, e la differenza
non compariva da nessuna parte.

Il flat resta comunque una regola di **sicurezza del bot** e non un ordine
impartito barra per barra: la policy ricevuta all'apertura vive in campi locali
(`_allowOvernight`, `_allowOverweek`, `_sessionFlatUtc`,
`_sessionFlatWindowMinutes`, `_weekEndFlatFromUtc/UntilUtc`, gli orari `TimeSpan`
come ora del giorno UTC) e continua a valere anche a server muto. Un descriptor
senza policy, o con orari implausibili, lascia i default storici invece di
spegnere il flat: un campo mancante non è un permesso, e un server che non manda
la durata della finestra lascia i 30 minuti di default.

La **deadline** dell'overnight non ha logica nel bot: il piano la esegue
stampandola nell'intent, che il bot già rispetta in `CloseExpiredPositions`. La
**finestra** invece sì, dal 7.6.0 (campo nuovo nel descriptor, quindi contratto nuovo): `EnforceSessionFlat` cancella i pending e
chiude ciò che resta, `HandleEntryIntent` rifiuta un ingresso reclamato un
attimo prima del flat e arrivato dentro.

## Riferimenti codice

- `Piootoo.Shared/Models/Trading/HoldingPolicy.cs` — `StrategyHolding`,
  `AccountHoldingPolicy`, `HoldingResolver`, `HoldingConflict`.
- `Piootoo.Shared/Models/Trading/TradingConventions.cs` —
  `WeekEndFlatPolicy`, i default (`TimeOnly`: 20:45, 23:00).
- `Piootoo.Core/Services/TradingPlanService.cs` — `MigrateLegacyHoldingTimes`, che traduce i
  `plans.json` scritti fino alla 7.2 con gli orari come interi `HHMM`.
- `Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs` — `IntradayOnly`,
  `AppliesSessionExit`, `SessionExitFromIntradayOnly`, `Holding`.
- `Piootoo.Core/Services/TradingPlanService.cs` — validazione e migrazione dei
  `plans.json` anteriori.
- `Piootoo.Core/Services/PiootooBacktestingService.cs` — `ApplyAccountHolding`,
  `IsBlockedByAccountFlat`, i trigger dei due flat e l'avviso `[rollover]`.
- `Piootoo.Core/Optimization/Sweep/SweepRunner.cs` e `Piootoo.Sweep/Program.cs`
  — la stessa tenuta nella ricerca (`--flat-utc`, `--flat-window`).
- `Piootoo.Core/Services/TradingSessionService.cs` — composizione della
  deadline nell'intent e `Holding` nel descriptor.
- `piootooapp.clientform/Shell/Screens/PlanDetailScreen.cs` — tab e avviso.
- `piootoo-repository/ctrader/*ExecutionBot.cs` — `ApplyHolding`,
  `DescribeHolding`, `EnforceWeekEndFlat`, `EnforceSessionFlat`.
- `tools/sweep-paniere.ps1` — `-FlatUtc`, `-FinestraFlat`.
- `Piootoo.Strategies.Tests/HoldingPolicyTests.cs`,
  `WeekEndFlatDiagnosticsTests.cs`.
