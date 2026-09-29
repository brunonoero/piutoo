# Catalogo delle figure classiche (PT7CLP)

La serie `PT7CLP_*` (classic pattern) raccoglie i motori che riconoscono le **figure dell'analisi tecnica
classica** — bandiere, doppi massimi e minimi, testa e spalle, triangoli — tradotte in regole misurabili.
Nasce il 29/09/2026. Il metodo non cambia rispetto alle altre serie: griglia grossa in campione, fuori
campione, costi veri del paniere, controllo RAN (`catalogo-idee-pt6exo.md` §RAN), correlazione con il
catalogo prima di entrare in un piano. Le soglie stanno in `lettura-risultati` e non si ripetono qui.

## Regole della serie

- Cartella `Piootoo.Strategies/PT7CLPStrategies/`, motori propri in `PT7CLPStrategies/Engines/`, sulla
  base comune `EasyEngineBase`. Nome `PT7CLP_{SIMBOLO}_{SIGLA}_{NNN}_{TF}`, una sigla per figura,
  progressivo per (simbolo, sigla). Contenitori `RC_{SIGLA}` in `ResearchContainers/`.
- **La figura non e' il segnale.** Il segnale e' la rottura del livello di conferma (retta della bandiera,
  neckline, lato del triangolo): un ordine stop su un livello noto, lo stesso percorso dei motori di
  breakout, con `CrossedLevelPolicy` del segnale.
- **Tolleranze in ATR**, mai in punti: la stessa figura su un indice da 25.000 punti e sull'argento.
- **Niente look-ahead.** Uno swing con k barre dopo si conosce k barre piu' tardi: sulla barra `t` si
  usano solo estremi e rette calcolati su barre chiuse fino a `t`.
- **Senza stato**: a ogni barra la figura si ricerca dalla finestra, e l'ordine si riemette finche' la
  figura resta valida.
- **Il rischio atteso e' la correlazione.** Una figura di continuazione rotta nel verso del trend e' un
  breakout: se una cella risponde, la prima domanda e' se guadagna negli stessi giorni di PCH e VBO sulla
  stessa cella (`piootoo-plan-builder`). Se si', e' un doppione con un altro nome.

## FLG — bandiera

Un movimento forte (il palo) seguito da un breve canale a rette parallele inclinato contro il palo; si
entra sulla rottura del canale nel verso del palo.

**Scritto il 29/09/2026**: `FlagEngine` in `PT7CLPStrategies/Engines/`, contenitore `RC_FLG`. Per una
lunghezza F fra `FlagMinBars` e `FlagMaxBars`, la barra ultima − F e' la cima e le F barre dopo sono la
bandiera (rialzista; la ribassista e' lo specchio):

- **palo**: la cima e' l'estremo delle `PoleBars` barre che finiscono li', e dista dalla loro base almeno
  `PoleAtr` volte l'ATR delle barre;
- **ATR delle barre**: media semplice del true range delle `AtrBars` barre *prima* del palo, sul timeframe
  della strategia. Non l'ATR di sessione della base, che su una cella oraria vale una giornata, e non
  gonfiato dal palo stesso;
- **bandiera**: nessun massimo oltre la cima; minimo non sotto la cima meno `MaxRetrace` del palo; rette
  di regressione su massimi e minimi con pendenza media nulla o contraria (il rettangolo orizzontale vale);
  pendenze che differiscono al piu' di `ParallelTolerance` ATR per barra — larga, ammette il pennant;
  l'ultima chiusura ancora sotto la retta dei massimi;
- le lunghezze si provano dalla piu' corta, la prima valida e' la bandiera in corso;
- **ordine**: buy stop sulla retta dei massimi proiettata sulla barra dopo, piu' `OffsetTicks`, arrotondato
  al tick verso l'esterno; valido una barra e riemesso finche' la figura regge, al piu' `FlagMaxBars` barre
  dopo la cima;
- **uscite**: quelle comuni della base, oppure `StopAtFlag` (oltre l'estremo opposto della bandiera di
  `ExtremeBufferTicks`) e `TargetPole` (multiplo dell'altezza del palo, il "measured move"), entrambi
  misurati dal livello d'ingresso.

Leve: `PoleBars`, `PoleAtr`, `AtrBars`, `FlagMinBars`, `FlagMaxBars`, `MaxRetrace`, `ParallelTolerance`,
`OffsetTicks`, `StopAtFlag`, `ExtremeBufferTicks`, `TargetPole`, `Direction`. Test in `FlagEngineTests`.

*Trappole:* lo stop sotto una bandiera stretta e' vicino, e il numero che conta e' `spread / distanza di
stop` (`spread-e-costo-di-transazione.md`): sui timeframe bassi, dove le bandiere sono di piu', lo spread
se lo mangia. Le leve di forma sono molte: la griglia grossa ne varia una sola.

**In matrice** (`CoarseGridMatrixTests`, motore `FLG`): leva `PoleAtr` 2 / 3 / 5, forma ferma ai valori da
manuale (palo 5 barre, ATR 20, bandiera 3-10 barre, ritracciamento 50%, parallelismo 0,1), stop e target
della matrice. In coda sul feed lungo del vendor (in campione 2008-2021, fuori 2022-2025), FDAX e NQ a
60 e 240 minuti: `matrice-{sim}-{tf}-flg-vendor`. Il secondo passo — stop sotto la bandiera e target al
palo — si fa solo su una cella che risponde.

## Figure in attesa

| sigla | figura | idea di regola |
|---|---|---|
| DTB | doppio massimo / minimo | due swing entro una tolleranza in ATR, avvallamento profondo almeno d ATR, stop sulla rottura della neckline; confronto obbligato con FBO sulla stessa cella |
| HSH | testa e spalle | cinque swing, testa oltre le spalle, spalle simili, neckline per i due minimi, stop sulla rottura |
| TRI | triangoli e cunei | regressione su massimi e minimi di una finestra, rette convergenti, stop sulla rottura di un lato |
| PBK | pullback in trend | trend confermato da swing crescenti o dalla media, ritorno sul livello rotto, ingresso alla ripresa |

Le figure armoniche (X-A-B-C-D) hanno una serie propria, `PTARM`: `catalogo-armonici.md`. Il loro
riconoscimento degli swing (pivot confermati, X chiuso dallo swing precedente) e' il modello per DTB e HSH.

Gli swing (pivot a k barre o ZigZag in ATR) servono a DTB e HSH: il pivot oggi vive solo dentro
`FibonacciTimeEngine`, e andra' portato in una funzione comune della serie quando la seconda figura lo usa.

| sigla | figura | stato |
|---|---|---|
| FLG | bandiera | `FlagEngine`, `RC_FLG`; quattro celle vendor in coda |

## Riferimenti codice

- `Piootoo.Strategies/PT7CLPStrategies/Engines/FlagEngine.cs`, `ResearchContainers/RC_FLG.cs`
- `Piootoo.Strategies.Tests/FlagEngineTests.cs`; serie e sigla in `PtsNamingConventionTests`
- `Piootoo.Strategies.Tests/CoarseGridMatrixTests.cs` (motore `FLG`), `piootoo-repository/ricerca/coda.json`
- `docs/domini/catalogo-idee-pt6exo.md` (controllo RAN, dai motori ai piani)
