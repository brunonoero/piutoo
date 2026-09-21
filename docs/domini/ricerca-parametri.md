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

## I due orologi, e perché non sono intercambiabili

| orologio | uso | costo |
|---|---|---|
| timeframe della strategia | ricerca: **ordina** le configurazioni | ~250 ms |
| un minuto | verifica e fasi di rischio: **misura** una configurazione | ~8,5 s |

Senza il feed da un minuto lo stop è valutato sulla chiusura della barra della strategia invece che
dentro, e la barra che lo avrebbe colpito per poi recuperare non lo colpisce. **L'errore ha un
verso**: più lo stop è stretto, più il percorso veloce è ottimista. Misurato su
`PT2_NQ_PCH_001_240`, stesso set di parametri a parte lo stop:

| stop | veloce | al minuto | scarto |
|---:|---:|---:|---:|
| $1.000 | $733.145 | $186.543 | **+$546.602** |
| $2.500 | $417.198 | $173.165 | +$244.033 |
| $4.595 | $220.181 | $231.822 | −$11.641 |

Una fase di risk management girata sul percorso veloce sceglierebbe **sempre** lo stop più stretto
della griglia, che nel conto vero è il peggiore. Per questo le fasi che decidono lo stop dichiarano
`SweepPhase.RequiresAccurateClock`.

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
Test: `SweepRunnerParityTests`, `SweepOptimizerTests`, `SweepValidationTests`, `SwapSpecTests`.
Resoconti dei run in `piootoo-repository/ricerca/`.
