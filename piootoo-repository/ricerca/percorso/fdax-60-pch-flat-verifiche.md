# PCH DAX 1 ora con il flat: le tre verifiche

Regola identica alla finalista del percorso (`fdax-60-ricerca-ftmo-flat.md`): long, buy stop 10 punti sopra il massimo della barra oraria, barre dalle 02 alle 10, stop 0,8 ATR, target 1 ATR, intraday, nessun pattern. Costi FTMO, orologio al minuto, niente overnight, flat alle 20:45 UTC per 30 minuti. Nessun parametro ritoccato.

## 1. DAX: ricerca, prova e storia interna mai vista

| periodo | trade | netto | DD chiuso | net/DD | average trade | anni in utile | per anno (netto in migliaia) |
|---|---:|---:|---:|---:|---:|---:|---|
| ricerca FTMO 11/2020-09/2024 | 872 | 254,144 | 36,087 | 7.04 | 291 | 5/5 | 2020:5 2021:61 2022:76 2023:58 2024:53 |
| prova FTMO 09/2024-09/2026 | 493 | 117,277 | 72,476 | 1.62 | 238 | 3/3 | 2024:16 2025:31 2026:70 |
| interno 2008-11/2020, mai visto | 2175 | 19,514 | 70,432 | 0.28 | 9 | 7/13 | 2008:-3 2009:3 2010:-8 2011:-14 2012:5 2013:1 2014:-11 2015:29 2016:8 2017:8 2018:-42 2019:48 2020:-5 |

## 2. La regola identica sugli altri indici (FTMO 11/2020-09/2026)

| indice | trade | netto | DD chiuso | net/DD | average trade |
|---|---:|---:|---:|---:|---:|
| @NQ | 1085 | 209,984 | 78,957 | 2.66 | 194 |
| @ES | 1310 | 94,008 | 48,978 | 1.92 | 72 |
| @YM | 1416 | 89,736 | 33,578 | 2.67 | 63 |
| @FESX | 725 | 15,397 | 9,032 | 1.70 | 21 |
| @FCE | 1062 | 19,299 | 14,056 | 1.37 | 18 |
| @Z | 1334 | 32,956 | 12,630 | 2.61 | 25 |
| @NIY | 1090 | 12,251,210 | 4,538,139 | 2.70 | 11,240 |

7 su 7 con net/DD ≥ 1 (il percorso chiede almeno la meta').

## 3. Contro 100 ingressi long casuali con le stesse uscite

RC_RAN riceve finestra 02-10, un ingresso per sessione, solo long, stop 0,8 ATR, target 1 ATR, intraday, e sceglie a caso il quando. Percentile = quota di semi che fa peggio della strategia.

### prova FTMO 09/2024-09/2026

PCH 493 trade, netto 117,277. RAN p = 0.2865, trade medi 481.

| metrica | PCH | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 117,277.04 | 95,683.24 | 144,278.01 | 198,638.55 | 22% |
| average trade | 237.88 | 202.46 | 297.00 | 410.41 | 19% |
| netto / DD | 1.62 | 1.29 | 2.04 | 2.95 | 24% |

### interno 2008-11/2020, mai visto

PCH 2175 trade, netto 19,514. RAN p = 0.27781, trade medi 2,140.

| metrica | PCH | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 19,514.29 | -129,848.60 | -19,135.71 | 75,268.71 | 79% |
| average trade | 8.97 | -60.20 | -9.05 | 35.25 | 79% |
| netto / DD | 0.28 | -0.75 | -0.17 | 1.00 | 81% |


## Verdetto (29/09/2026)

**Non si tiene.** Il guadagno non viene dalla rottura del massimo ma dall'essere long sul DAX la mattina,
in anni in cui il DAX e' salito.

- **Contro il caso perde sul periodo mai visto.** Cento serie di ingressi long casuali nella stessa finestra
  e con le stesse uscite fanno in mediana +144.278 contro i +117.277 della PCH: la PCH sta al 22° percentile.
  La RHL, sullo stesso controllo, stava sopra il 96%.
- **Sul DAX 2008-2020 non c'e' edge.** 2.175 trade, +19.514, net/DD 0,28, average trade 9 punti contro una
  soglia di 161, 7 anni su 13 in utile, 2018 a -42.000. Il caso, li', perde in mediana: la PCH fa un po'
  meglio (79° percentile), ma non abbastanza da essere una strategia.
- **Gli altri indici, 7 su 7 in utile, non contano come prova**: sul feed FTMO dal 2020 tutti gli indici sono
  saliti, e un long mattutino con stop e target stretti guadagna ovunque. Lo dice il controllo sul caso.

Da ricordare: il flat prima del rollover e' stato utile — ha fatto emergere la configurazione — ma la
ricerca l'ha scelta sul 2020-2024, un periodo rialzista, e i cancelli della ricerca non separano un segnale
dalla deriva del mercato. Il controllo sul caso e la storia lunga si': vanno fatti prima di ogni classe.