# Griglia grossa NQ 4 ore, Trend Following mirrored — 250 combinazioni, 22/09/2026

**Cella:** Trend Following mirrored su `@NQ` a 4 ore, contenitore `PT3B_NQ_TFM_001_240` con il motore
nudo (pattern alle sentinelle, nessun filtro orario). Leve: maxBars {0, 6, 12, 30, 60} × stop
{500…10000} × target {0…12000} × uscita {fine sessione, 21}. Feed ICS, spread **ICS** 1,00, swap ICS,
commissione zero, orologio al minuto. **Campione 2014-07-17 → 2021-01-01, validazione → 2026-09-01.**
29 minuti su 8 core. Dati in `nq-4h-tf-griglia-grossa.csv`, generati da `NqTfCoarseGridTests`.

**È il primo confronto fra motori a parità di tutto il resto**: stessa cella, stesso periodo, stesso
split, stessi costi della griglia Price Channel (`nq-4h-griglia-grossa-lunga.md`). L'unica cosa che
cambia è il motore.

## Il risultato: zero celle ammissibili, e non per pochi trade

- **0 celle su 250** ammissibili. Non perché facciano pochi trade — ne fanno da **1.617 a 1.983** in
  campione, più del Price Channel sulla stessa cella — ma perché **perdono tutte**: il netto in
  campione va da −283.909 a **−14.135**, e la cella migliore ha profit factor 0,97.
- Fuori campione, invece, guadagnano molto: la migliore fa **+205.498** con profit factor 1,27.

È la firma già vista su GC e CL, qui nella forma più estrema: il motore **perde su sei anni e mezzo
e guadagna sui cinque successivi**. Quando un motore nudo perde ovunque dentro e guadagna ovunque
fuori, quello che si sta misurando è il regime del 2021-2026, non il motore.

### Il confronto con il Price Channel sulla stessa cella

| | Price Channel | Trend Following mirrored |
|---|---:|---:|
| combinazioni | 450 | 250 |
| ammissibili | 127 | **0** |
| trade in campione | 250-2.500 | 1.617-1.983 |
| miglior netto in campione | +51.416 | **−14.135** |
| miglior average trade in campione | 44 | — (nessuna in utile) |

**L'ipotesi «un motore diverso farà meglio» non è confermata su questa cella.** Il Price Channel,
che pure non supera le soglie del metodo, almeno guadagna in campione su 127 configurazioni; il
trend following mirrored nudo non ne ha una.

## ⚠ Due leve su quattro erano inerti, e una è un difetto del codice

Le celle con parametri diversi danno risultati **identici**: `maxBars` 6, 12 e 30 con lo stesso stop
e target producono esattamente gli stessi 1.983 trade e lo stesso netto, e così le due uscite. Le
ragioni sono due e diverse.

**`MaxBars` è inerte per costruzione, ed è colpa della griglia.** Con `IntradayOnly = 1` la posizione
muore a fine sessione, cioè entro sei barre da quattro ore: un limite di 6, 12, 30 o 60 barre non
può mai scattare. La leva avrebbe senso solo con `IntradayOnly = 0`, e la griglia andava scritta
così.

**`ExitHour` è inerte perché il motore non la legge, ed è un difetto vero.** `SessionExitTime` è
dichiarata su `EasyEngineBase` — quindi la ereditano tutti e dieci i motori — ma **solo
`PriceChannelEngine` la usa**: `TfEngineBase.WithPythonSettings` chiude su `SessionEnd` e basta.
Impostare `ExitHour` su una strategia trend following **non fa niente, in silenzio**.

Non è un dettaglio da poco: l'uscita prima del rollover è la leva che ha prodotto
`PT3B_FDAX_PCH_002_240` e che quattro griglie indipendenti hanno confermato. Chiunque provi ad
applicarla a una TF otterrebbe gli stessi numeri di prima e concluderebbe che «su questa strategia
non serve». Va deciso se portarla in `EasyEngineBase` per tutti i motori o se vincolarla al solo
Price Channel con un controllo che la renda non dichiarabile altrove.

**Deciso e fatto il 23/09/2026: portata su tutti i motori.** La deadline di fine sessione la
risolve un punto solo, `EasyEngineBase.WithSessionExit`, e i sette motori che applicano l'uscita di
sessione ci passano; un test sul sorgente (`EveryEngineResolvesTheSessionExitInOnePlace`) impedisce
a un motore di risolverla da sé. Il contenitore `PT3B_NQ_TFM_001_240` legge di nuovo `ExitHour`.
Questa griglia **non è stata rilanciata**: la conclusione qui sotto dice perché la prossima prova sul
trend following non è un altro giro della stessa griglia, ed è quella che è partita
(`ricerca/nq-15m-tfu-*.md`, sweep con `SweepSpaces.TrendFollowingUnmirrored`).

## Conclusione

**Su NQ a 4 ore il trend following mirrored nudo è peggio del Price Channel**, che già non bastava.
Il risultato è valido per quello che ha misurato — venticinque combinazioni distinte di stop e
target — e quelle venticinque perdono tutte in campione.

**Ma è un punto, non una famiglia**, e prima di chiudere con il trend following vanno detti i limiti
di questa misura. Il motore è nudo, senza i gate di pattern che sulle TF del catalogo sono il filtro
principale; è la variante *mirrored*, mentre la sola TF coerente su NQ era `PTS_NQ_TFU_003_15`, cioè
*unmirrored*; ed è a 4 ore, mentre quella lavorava a 15 minuti. La prossima prova sul trend
following dovrebbe cambiare tutte e tre le cose insieme, e non è quindi un altro giro di questa
griglia.

## Riferimenti

`nq-4h-tf-griglia-grossa.csv`, `nq-4h-griglia-grossa-lunga.md` (il Price Channel sulla stessa cella),
`nq-catalogo-costi-veri.md` (le TF del catalogo), `Piootoo.Strategies.Tests/NqTfCoarseGridTests.cs`,
`Piootoo.Strategies/Easy/Engines/TfEngines.cs`, `metodo/metodo-unger/SKILL.md` §8.2.
