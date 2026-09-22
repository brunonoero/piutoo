# Griglia grossa NQ 4 ore sul periodo lungo — 450 combinazioni su dodici anni, 22/09/2026

**Cella:** Price Channel su `@NQ` a 4 ore, contenitore `PT3B_NQ_PCH_002_240` con il motore nudo.
Leve: canale {1, 20, 50} × stop {500…10000} × target {0…12000} × uscita {fine sessione, 21} ×
direzione {0, 1, 2}. Feed ICS, spread **ICS** 1,00, swap ICS, **commissione zero** (la scheda USTEC
la dichiara zero), orologio al minuto. **Campione 2014-07-17 → 2021-01-01, validazione →
2026-09-01.** 40 minuti su 8 core. Dati in `nq-4h-griglia-grossa-lunga.csv`, generati da
`Nq4hCoarseGridTests`.

## Perché, dopo due bocciature sulla stessa cella

NQ a 4 ore era già stato cercato due volte con la sweep — il 21/09 col criterio vecchio e il 22/09
con quello nuovo — e in entrambi i casi nessuna finalista era sopravvissuta. Ma tutte e due giravano
su **2022-2026**: quattro anni, di cui il fuori campione è il rialzo del 2025-26. Su dodici anni la
domanda cambia: non «questa configurazione regge?» ma «la cella ha un edge?». È la stessa differenza
che su GC ha portato da zero celle equilibrate a trentaquattro.

## Il risultato

- **127 celle su 450** ammissibili (≥ 250 trade in campione e campione in utile).
- **31 equilibrate** (netto/DD ≥ 1 dentro e fuori, ≥ 3 finestre su 4).
- Ma con i criteri del metodo: **zero** celle con UngerFit ≥ 1 (massimo 0,45), **zero** con guadagno
  annuo su drawdown ≥ 2, e **zero con l'average trade sopra soglia**.

Il numero che chiude la questione è l'ultimo. Il range medio della barra da 4 ore è 42 punti nel
campione, cioè 834 dollari per contratto, quindi la soglia del 15% è **125 dollari**. Il miglior
average trade delle 127 ammissibili è **44**: il 35% della soglia.

Le migliori equilibrate:

| can | stop | targ | exit | dir | IS n | IS netto | IS DD | IS PF | OOS n | OOS netto | OOS PF | fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 20 | 500 | 1.500 | 21 | short | 423 | 17.711 | 13.011 | 1,14 | 444 | 53.620 | 1,36 | 4/4 |
| 20 | 500 | 3.000 | 21 | entrambe | 1.158 | 38.693 | 12.385 | 1,13 | 1.058 | 85.818 | 1,21 | 4/4 |
| 20 | 500 | 1.500 | 21 | entrambe | 1.168 | 51.416 | 10.614 | 1,18 | 1.063 | 65.006 | 1,18 | 4/4 |
| 20 | 3.000 | 1.500 | 21 | entrambe | 1.131 | 44.110 | 24.626 | 1,11 | 1.039 | 109.894 | 1,14 | 4/4 |

### Le leve

| leva | IS medio | OOS medio | ammissibili | equilibrate |
|---|---:|---:|---:|---:|
| **uscita alle 21** | 24.419 | 65.204 | **118** | **31** |
| uscita a fine sessione | 5.181 | 64.260 | 9 | 0 |
| direzione entrambe | 19.183 | 65.383 | 46 | 5 |
| direzione solo long | 26.916 | 66.008 | 73 | 24 |
| direzione solo short | 10.095 | 55.779 | 8 | 2 |
| canale 1 | 37.974 | 99.619 | 43 | 13 |
| canale 20 | 18.977 | 55.051 | 55 | 15 |
| canale 50 | 8.670 | 33.138 | 29 | 3 |

**L'uscita alle 21 domina come su nessun'altra cella**: 118 ammissibili su 127 e tutte e 31 le
equilibrate. A fine sessione la cella quasi non produce configurazioni ammissibili. È la quarta
conferma indipendente della stessa leva, dopo FDAX (40 equilibrate su 60), GC (26 su 34) e la
validazione della 002.

## Conclusione

**Su NQ a 4 ore il Price Channel nudo non ha un average trade che valga il costo, nemmeno su dodici
anni.** Trentuno celle equilibrate sembrano un risultato, ma sono equilibrate in senso relativo —
netto sopra il drawdown — mentre in assoluto guadagnano 44 dollari a trade dove la volatilità della
barra ne chiederebbe 125.

Le due sweep del 21 e 22 settembre avevano ragione, e questa griglia dice **perché**: non è che le
loro finaliste fossero sfortunate, è che la cella non ha margine per trade da nessuna parte dello
spazio dei parametri. Alzare il pavimento dei trade o cambiare criterio non poteva cambiarlo.

## Riferimenti

`nq-4h-griglia-grossa-lunga.csv`, `nq-4h-pc-tutto-al-minuto.md` e
`nq-4h-pc-tutto-al-minuto-criterio-2.md` (le due sweep), `nq-15m-griglia-grossa.md`,
`fdax-4h-griglia-grossa-lunga.md` (la cella che invece supera le soglie),
`Piootoo.Strategies.Tests/Nq4hCoarseGridTests.cs`, `metodo/metodo-unger/SKILL.md` §8.2 e §13.6.
