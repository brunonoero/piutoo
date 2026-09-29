# Persistenza del rendimento delle strategie — esito (29/09/2026)

**Domanda.** Una strategia che è andata bene di recente continua ad andare bene? Se sì, un filtro
che accende le strategie "in forma" (una rotazione, come era Titano) aggiunge valore.

**Risposta: no.** In tre campioni, tre cadenze e quattro finestre, il rendimento recente di una
strategia non predice quello successivo in modo sfruttabile. La rotazione peggiora sempre il
portafoglio equipesato e quasi mai batte una selezione casuale dello stesso numero di strategie.

## Campioni

| file | trade | strategie | periodo | taglio | note |
|---|---:|---:|---|---|---|
| prova, non salvata (5 permutazioni) su `piani-onesti-2026-09-27/run-storia-ricerca` | 12.970 | 33 | 2012 → 2025-05 | 2019-01-01 | le PT5DAV dei piani del 27/09, scelte su questa storia |
| `storia-ricerca-132.md` | 75.984 | 132 | 2012 → 2025-05 | 2019-01-01 | ottimizzate su tutta la storia: il taglio non e' un vero fuori campione |
| `pt5dav-tutte-ftmo-2022-2026.md` | 54.621 | 218 | 2022-06 → 2026-09 | 2025-06-01 | **tutte** le PT5DAV, anche le scartate, feed FTMO; dopo il taglio e' fuori campione vero |

Script: `persistenza.py <trades.json> [permutazioni] [taglio]`, solo libreria standard.

## Cosa si vede

- **Persistenza per strategia (A)**: lo Spearman medio fra gli ultimi L periodi e il successivo sta fra
  -0,09 e +0,005, e la maggioranza delle strategie e' negativa. Sulle storie di ricerca e'
  leggermente **negativo** in modo sistematico: e' l'impronta della selezione (una strategia scelta
  su tutta la storia e' per costruzione una che e' uscita da ogni drawdown), non un'inversione
  sfruttabile.
- **Sezione trasversale (B)**: l'unico IC positivo significativo e' giornaliero a L=1 sulle 218 FTMO
  (+0,017, t 5,2). Vale 1,7 centesimi di correlazione, e il filtro che lo usa sta al 2° percentile
  della selezione casuale. E' con ogni probabilita' il **mercato** che tiene la direzione per un
  giorno, visto da tante strategie sullo stesso simbolo, non la strategia che "e' in forma".
- **Rotazione (C)**: su 72 combinazioni (3 campioni × 3 cadenze × 4 finestre × dentro/fuori) il
  filtro non batte mai l'equipesato su netto÷DD in modo stabile fra dentro e fuori campione. I
  percentili contro la selezione casuale vanno da 0% a 90% senza una cadenza o una finestra che
  tenga su tutti i campioni.
- **Per famiglia**: nessuna famiglia ha persistenza positiva coerente fra i campioni (RHL e SBO
  vicini a zero, TFM negativa).

Coerente con la simulazione Titano del 21/08 (`docs/titano-simulazione-2026-08-21/`, correlazione
IS→OOS del Calmar +0,017).

## A margine

Sulle 218 PT5DAV su FTMO il rendimento normalizzato mensile medio passa da circa 0,53 (giugno 2022 →
maggio 2025) a 0,24 (giugno 2025 → agosto 2026): **il fuori campione vale meta'**. Non e' una
questione di rotazione ma del catalogo, e va tenuto presente nei piani.

## Conseguenza

La strada "scegliere le strategie in forma" e' chiusa. Il trend temporaneo, se c'e', sta nel
**mercato**, non nella curva della strategia: va misurato dentro il motore, con una regola di regime
calcolabile sulla barra (seconda fase: motore con filtro di regime, sweep, confronto col motore
nudo, controllo RAN).
