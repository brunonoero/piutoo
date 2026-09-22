# Ricerca dei parametri (sweep)

Dal 20/09/2026 il sistema **cerca** le strategie, non solo le esegue. Prima i parametri
arrivavano da un dossier di ricerca esterno e il porting li riportava verbatim
([porting-da-report-sweep.md](porting-da-report-sweep.md)); ora c'è un ottimizzatore interno, e le
strategie che ne escono sono la serie `PT3B_*`.

Il codice sta in `Piootoo.Core/Optimization/Sweep/`, l'eseguibile è `Piootoo.Sweep`
(`piootoo-sweep`), e i lanci del paniere passano da `tools/sweep-paniere.ps1`.

## Perché il runner non è un simulatore nuovo

`SweepRunner` è il **motore di esecuzione vero** (`PiootooTradingService`) dentro lo stesso loop del
backtest: stessa regola su quando una barra si valuta, stesso ordine dentro il tick, stesso flat di
fine settimana, stesse deadline. Le quattro regole del loop non sono duplicate — erano `private
static` nel servizio di backtest e ora sono `internal`.

Un ottimizzatore che gira su regole proprie consegna parametri che il backtest non riproduce: è il
difetto del porting spostato di un piano. La taratura è un test
(`SweepRunnerParityTests`): con l'orologio al minuto il runner deve dare **gli stessi numeri** del
backtest ufficiale. Ci è arrivato dopo una correzione vera — mancava `RejectWrongSideLevels`, e sette
trade su 861 erano diversi.

Quello che il runner non fa: artefatti su disco, diagnostica per evento, equity per ora. Sono le
ragioni per cui un run costa millisecondi invece di decine di secondi.

## Un orologio solo: il minuto

Dal 21/09/2026 **ogni fase** gira sul feed da un minuto. Fino a quel giorno le fasi che decidono
lo stop giravano al minuto e le altre — trigger, pattern, orari — sull'orologio al timeframe della
strategia, tredici volte più economico. La distinzione è stata tolta perché misurata falsa, e la
storia merita di restare scritta.

Senza il feed da un minuto lo stop è valutato sulla chiusura della barra della strategia invece che
dentro, e la barra che lo avrebbe colpito per poi recuperare non lo colpisce. **L'errore ha un
verso**: più lo stop è stretto, più il percorso veloce è ottimista. Misurato su
`PT2_NQ_PCH_001_240`, stesso set di parametri a parte lo stop:

| stop | veloce | al minuto | scarto |
|---:|---:|---:|---:|
| $1.000 | $733.145 | $186.543 | **+$546.602** |
| $2.500 | $417.198 | $173.165 | +$244.033 |
| $4.595 | $220.181 | $231.822 | −$11.641 |

Questo si sapeva, ed è il motivo per cui lo stop non si è mai scelto sul veloce. L'argomento per
tenerlo sulle altre fasi era che sbagliasse i *valori* ma conservasse l'*ordinamento*: dentro una
fase lo stop è fisso, quindi tutte le combinazioni sbaglierebbero nello stesso verso. Nessuno
l'aveva misurato. `SweepFastClockRankingTests` lo ha fatto su 22 configurazioni vere di `@FDAX
240m`, stop fisso a 1500:

| | Spearman fra i due orologi |
|---|---:|
| sul punteggio dell'obiettivo (quello che ordina) | **0,021** |
| sul netto | 0,694 |
| prime 3 in comune | 1 su 3 |

Il netto correla poco; il punteggio per niente. Il punteggio è netto **diviso** il drawdown del
peggior tratto, e il drawdown è esattamente ciò che il veloce non vede — e non lo vede in misura
diversa da configurazione a configurazione, a seconda di quante barre volatili ciascuna attraversa.
Al veloce i punteggi andavano da 1,7 a 3,6; al minuto erano quasi tutti negativi. Le fasi veloci
ordinavano rumore, e la sweep del 21/09 sera lo ha mostrato dal lato pratico: sei finaliste tutte in
perdita fuori campione, battute dalla configurazione di partenza con un solo parametro cambiato a
mano.

Il costo di girare tutto al minuto: su `@FDAX 240m` da 37 minuti a circa tre ore, sei core in
parallelo. `SweepOptimizerOptions.UseFastClockForOrderingPhases` riaccende il comportamento vecchio
per rimisurarlo, e il log di avvio dichiara l'orologio di ogni fase — perché fasi misurate su
orologi diversi non sono confrontabili, e il resoconto le stampava nella stessa colonna senza dirlo.
La riserva: 0,021 viene da una cella sola, e il test va ripetuto su NQ.

## Le fasi

L'ordine è il metodo Unger, tradotto da `get_optimization_phases()` dei motori Python: trigger,
filtri, orari, e il risk management per ultimo — **mai pattern e stop/target nella stessa fase**.

Ogni fase permuta i propri parametri tenendo fermi tutti gli altri al seme della precedente, e
consegna alla successiva le migliori `BeamWidth` (2 di default: con 1 la sweep sceglie il canale
prima di sapere che orari avrà).

Le griglie sono quelle di `easy_engine_py/`, riportate **verbatim** in `SweepSpaces` — comprese le
correzioni che le hanno prodotte: il passo orario a un'ora, i canali fino a 155, i 51 pattern
direzionali **negativi** del mirroring invertito senza cui i setup contrarian non esistono.

`SplitPatternPhases()` spezza le fasi a due pattern in due fasi da uno. Il prodotto diventa una
somma — sul BIAS settimanale da 23.256 combinazioni a 153 e 152, cioè da nove ore a pochi minuti —
al prezzo dichiarato: una coppia richiesto+vietato che rende solo insieme non è più raggiungibile.
È una **deviazione** dal metodo, non una variante equivalente, e il resoconto la stampa in testa.

### L'ora di uscita di sessione: la seconda deviazione dichiarata

`ExitHour` (PC intraday, dal 21/09/2026) sceglie **a che ora** l'uscita di sessione chiude la
posizione. `-1` — primo valore della griglia, quindi il punto di partenza della sweep — è la fine
della sessione, cioè il comportamento di sempre.

Esiste perché la sessione della ricerca è il **giorno di calendario europeo**: per FDAX finisce
alle 00:59, cioè *dopo* il rollover del broker (21:00 su ICS, 20:59 su FTMO). Una PC dichiarata
`intraday_only = 1` attraversa quindi il rollover ogni giorno e paga il finanziamento come una
multiday — su `PT3B_FDAX_PCH_001_240` sono 879 trade su 1.317 e il 28% del lordo. Non è un costo
che compra qualcosa: la posizione resta aperta nelle ore in cui il future è chiuso e quota solo il
CFD.

Come `SplitPatternPhases`, è una **deviazione** dal metodo e non una sua variante: `price_channel.py`
conosce il solo `exit_on_session_end` booleano, e una configurazione con `ExitHour != -1` non è
riproducibile dal motore Python. Il resoconto la dichiara.

Tre scelte che la rendono onesta:

- **`SessionEnd` non si tocca.** Quel valore definisce anche gli OHLC di sessione, i pattern e la
  chiave del limite di ingressi: spostarlo sarebbe un'altra strategia, non un altro orario di
  uscita. `SessionExitTime` è un campo suo e tocca la sola deadline.
- **Un ingresso che nascerebbe dopo la propria ora di uscita non nasce.** Il comportamento storico
  di `ResolveCloseAtUtc` rimanda alla sessione successiva, e qui trasformerebbe in overnight proprio
  la posizione che quell'ora esiste per chiudere prima della notte, smentendo la `Holding`
  dichiarata. L'overload con `rollToNextSession: false` restituisce `null` e il motore scarta.
- **La fase gira sull'orologio fitto.** `CloseAtUtc` lo applica `UpdateMarketPrices`, che sul
  percorso veloce gira una volta per barra della strategia: una chiusura alle 20:00 misurata a 240
  minuti cadrebbe sulla barra dopo, cioè fino a quattro ore di finanziamento *in più* di quelle che
  il parametro toglie. La misura direbbe il contrario del vero.

La fase sta **dopo gli orari e prima dello stop**: è una decisione di durata, parente di `MaxBars`,
e va scelta prima che stop e target vengano tarati addosso a una durata diversa.

### Stop e target in ATR: la terza deviazione dichiarata

`StopAtr` e `TargetAtr` (dal 22/09/2026, `EasyEngineBase.StopAtrMultiplier` /
`TargetAtrMultiplier`) esprimono stop e target come **multipli dell'ATR delle sessioni chiuse**
invece che in dollari per contratto. `0` è spento: vale `StopLoss`/`TakeProfit` in denaro, il
comportamento di sempre.

Esiste perché uno stop in denaro è tarato sul regime del campione. La griglia NQ 15 lo ha mostrato:
in campione vinceva lo stop a 4000, fuori quello a 1000, e la differenza era la volatilità, non la
strategia. Un multiplo dell'ATR è un solo parametro che vale in ogni fase, e con la size a %f sullo
stop dà un rischio in dollari costante per trade — il %Vol di Unger scritto dalla parte dello stop.

Tre scelte che la rendono onesta:

- **L'ATR è quello delle sessioni chiuse**, a 14 sessioni, lo stesso del filtro `DvolMin`
  (`session_atr(df, 14, shift=1)`): la sessione in corso non entra mai (Legge Zero). Il periodo è
  fisso e non in griglia — ogni parametro in più è selezione in più.
- **Il segnale esce in denaro.** La conversione — ATR in punti × valore del punto × multiplo —
  avviene al momento del segnale in `BuildEntry`, e `StopLossMoneyPerFutureContract` è numerico
  come oggi: backtest e cBot ricevono uno stop in dollari e non sanno da dove viene. Nessun cambio
  di contratto. Senza 15 sessioni di storia si ripiega sul denaro fisso, mai su "nessuno stop".
- **Non è riproducibile dal motore Python**, che conosce solo `stop_loss`/`take_profit` in dollari:
  una configurazione con `StopAtr != 0` è una deviazione dichiarata, come `ExitHour`.

Nella griglia grossa si accende con `CoarseGridSpec.AtrStops`: `Stops` e `Targets` diventano decimi
di ATR (10 = 1,0) e il denaro fisso va a zero, così un target a 0 è davvero "nessun target".

## Il criterio: il peggiore dei tratti, non il totale

Su decine di migliaia di combinazioni il massimo di `netto/drawdown` è quasi sempre una
configurazione che nel campione non ha mai incontrato un brutto tratto — non perché sia robusta, ma
perché fra tante qualcuna è fortunata. I numeri delle prime quattro celle lo dicono senza ambiguità:

| cella | punteggio in campione | tenuta fuori campione |
|---|---:|---:|
| NQ 4h | **26,2** | 4% |
| FDAX 1h | 16,1 | 7% |
| **FDAX 4h** | **4,8** | **52%** ✅ |

Le bocciate avevano i punteggi più alti. Un massimo troppo bello non è un risultato migliore: è un
avvertimento.

`WorstSubPeriodObjective` divide il campione in quattro tratti e tiene il **minimo**: una
configurazione che deve tutto a un anno buono viene giudicata sull'anno cattivo. Il denominatore ha
un pavimento sulla **peggiore perdita singola** del tratto, perché un drawdown minuscolo — di nuovo,
quasi sempre fortuna — produceva punteggi enormi dividendo per quasi zero.

**Ricerca e validazione usano criteri diversi**, ed è voluto: «quale preferisco fra diecimila» chiede
un criterio che punisca la fortuna, «questa regge fuori campione» chiede quanto ha reso rispetto a
quanto ha rischiato. Per la stessa ragione i controlli su «in perdita» guardano il netto e non il
punteggio, che cambia significato col criterio.

## La validazione

Campione e validazione sono due **finestre sugli stessi array** (`SweepSeries.Between`): costano un
caricamento solo, e le barre oltre la fine non sono look-ahead perché il cursore ne restituisce solo
fino all'istante corrente.

Tre criteri: il fuori campione dev'essere ammissibile, deve conservare una quota del punteggio
(50% di default) e non deve avere tutto l'utile in un tratto solo (walk-forward di **stabilità** —
non la Walk-Forward Optimization vera, che ri-ottimizza a ogni finestra ed è un piano sopra questo).

Si validano le **prime N finaliste** e non la sola vincitrice: la prima classificata in campione è
spesso quella che ha sfruttato meglio il rumore.

`Survivor` può essere `null`. È un esito legittimo, da riportare come tale e non da aggirare
abbassando le soglie finché qualcosa passa.

## I costi sono misure, non parametri

Vale la regola generale: finché un costo non è nel modello, la ricerca ci si infila. È successo tre
volte, e ogni volta il difetto si travestiva da scoperta.

| costo | dove | che cosa è successo senza |
|---|---|---|
| spread per ora | `spread/{BROKER}/` | due celle su due sceglievano le ore notturne, dove lo spread FDAX è 3 punti e la costante giornaliera ne dichiarava 1,23 |
| swap | `swap/{BROKER}/` | una strategia *intraday* pagava il rollover ogni giorno: 879 trade su 1.317, il 28% del lordo |
| commissione | richiesta | è **per lato** e il motore la addebita due volte; le schede stampano il round turn |

Vedi [spread-e-costo-di-transazione.md](spread-e-costo-di-transazione.md).

### Il costo peggiore fra più broker

`--spread-broker ICS,FTMOPLATFORM` e `--swap-broker ICS,FTMO` prendono il peggiore **voce per
voce**, non scegliendo il broker più caro — che non esiste:

| @FDAX | FTMO | ICS | peggiore |
|---|---:|---:|---|
| spread p50 | 1,23 pt | 0,50 pt | FTMO |
| swap long | 4,53 pt | 5,05 pt | ICS |
| swap short | 0,05 pt | 1,22 pt | ICS |
| rollover | 20:59 | 21:00 | FTMO |

Una strategia cercata sui costi di un broker vive dentro il listino di quel broker: se il listino
cambia, o si cambia prop firm, non è più la strategia che si era validata.

⚠ **Non modellato**: FTMO dichiara `P&L conversion fee rate 0,70%` dove ICS dichiara 0,00%. È un
costo proporzionale al P&L che il motore non applica.

### La tenuta con cui si opererà

Senza opzioni la sweep gira a overnight e overweek liberi, la parità con il motore Python. Con
`--flat-utc 20:45 [--flat-window 30]` gira con la stessa `AccountHoldingPolicy` del piano che
vieta l'overnight: deadline al flat, nessun ingresso nella finestra `[flat, flat + minuti)`,
pending cancellati al flat. È il modo di tenere una strategia lontana dal rollover **senza
riscriverla** — e il resoconto dichiara la tenuta e se la finestra copre il rollover degli swap
caricati. Cercare con una tenuta e operare con un'altra valida una strategia diversa da quella
che si opera. Vedi [overnight-e-overweek.md](overnight-e-overweek.md).

## Promuovere una finalista a classe

La classe di partenza della sweep **non è solo un contenitore di parametri**: porta con sé
l'etichetta della barra (`ResearchLabelsBarsOnOpen`) e l'ancoraggio di sessione, che dei parametri
non fanno parte e che nessun resoconto stampa.

Sulla prima `PT3B_*` questo è costato un errore: scritta con l'etichetta di chiusura invece che di
apertura, la finestra si spostava di quattro ore e il campione passava da **+$216.830 a −$2.527**.

Quindi la regola: dopo aver scritto la classe, **rieseguirla e verificare che riproduca i numeri
della ricerca**, prima del backtest e non dopo.

## Riferimenti codice

`Piootoo.Core/Optimization/Sweep/` — `SweepRunner`, `SweepSeries`, `SweepSpace`, `SweepOptimizer`,
`SweepObjective`, `SweepValidation`. `Piootoo.Core/Services/SwapTable.cs` e `SpreadTable.cs` per i
costi. `Piootoo.Sweep/Program.cs` per la riga di comando, `tools/sweep-paniere.ps1` per i lanci.
`Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs` per `SessionExitTime`, `StopAtrMultiplier` e
`ClosedSessionAtrPoints`; `Piootoo.Strategies.Tests/CoarseGridStudy.cs` per la griglia grossa.
Test: `SweepRunnerParityTests`, `SweepOptimizerTests`, `SweepValidationTests`, `SwapSpecTests`,
`SessionExitHourTests`, `AtrStopTests`.
Resoconti dei run in `piootoo-repository/ricerca/`.
