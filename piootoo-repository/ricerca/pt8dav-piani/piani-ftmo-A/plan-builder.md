# Piani di strategie scorrelate

Run letti: `piootoo-repository\ricerca\pt8dav-piani\candidate-run-ftmo`.
Asse dei giorni: 787 giorni con almeno un trade, dal 2022-06-01 al 2025-05-30 (UTC, giorno di uscita).

Vincoli: fino a 3 piani disgiunti da 6 a 8 strategie; al massimo 2 per simbolo e 2 per famiglia; correlazione <= 0,30, nelle code (peggior 10% dei giorni) <= 0,30; drawdown del piano libero, perdita giornaliera libera; candidate da 30 trade in su.

Le candidate arrivano gia' giudicate dal metodo: qui si misura solo come stanno insieme. I run devono avere le stesse size.

## Piani

### Piano 1

8 strategie · netto 314.565 · drawdown giornaliero 20.027 · netto/DD 15,71 · giorno peggiore -7.744 · correlazione massima fra membri 0,18, nelle code -0,24

| strategia | simbolo | famiglia | trade | netto | DD | netto/DD | giorno peggiore |
|---|---|---|---:|---:|---:|---:|---:|
| `PT8DAV_FDAX_BRT_001_15` | FDAX | BRT | 605 | 75.799 | 12.212 | 6,21 | -2.254 |
| `PT8DAV_NQ_VBO_001_15` | NQ | VBO | 57 | 49.325 | 10.904 | 4,52 | -3.912 |
| `PT8DAV_GC_LFD_001_60` | GC | LFD | 58 | 53.203 | 10.863 | 4,90 | -2.458 |
| `PT8DAV_ES_RHL_002_60` | ES | RHL | 115 | 24.905 | 12.630 | 1,97 | -2.523 |
| `PT8DAV_BP_VBO_001_15` | BP | VBO | 121 | 9.119 | 4.214 | 2,16 | -1.650 |
| `PT8DAV_BP_SBO_001_15` | BP | SBO | 34 | 1.750 | 729 | 2,40 | -271 |
| `PT8DAV_GC_PCH_002_60` | GC | PCH | 354 | 72.171 | 18.491 | 3,90 | -3.290 |
| `PT8DAV_NQ_SBO_003_60` | NQ | SBO | 67 | 28.293 | 6.956 | 4,07 | -4.881 |

### Piano 2

8 strategie · netto 249.040 · drawdown giornaliero 23.778 · netto/DD 10,47 · giorno peggiore -14.433 · correlazione massima fra membri 0,07, nelle code -0,18

| strategia | simbolo | famiglia | trade | netto | DD | netto/DD | giorno peggiore |
|---|---|---|---:|---:|---:|---:|---:|
| `PT8DAV_NQ_VBO_003_240` | NQ | VBO | 112 | 102.029 | 17.850 | 5,72 | -7.886 |
| `PT8DAV_GC_VBO_001_15` | GC | VBO | 130 | 27.894 | 19.277 | 1,45 | -8.082 |
| `PT8DAV_ES_LFH_001_15` | ES | LFH | 43 | 13.299 | 5.299 | 2,51 | -1.370 |
| `PT8DAV_FDAX_RHL_002_60` | FDAX | RHL | 79 | 79.904 | 33.352 | 2,40 | -12.136 |
| `PT8DAV_ES_MAC_001_30` | ES | MAC | 129 | 11.464 | 9.310 | 1,23 | -4.535 |
| `PT8DAV_YM_RBU_001_60` | YM | RBU | 119 | 7.836 | 5.352 | 1,46 | -858 |
| `PT8DAV_BP_PCH_001_60` | BP | PCH | 78 | 3.921 | 2.581 | 1,52 | -890 |
| `PT8DAV_BP_RBM_001_30` | BP | RBM | 79 | 2.693 | 2.249 | 1,20 | -780 |

### Piano 3

6 strategie · netto 290.796 · drawdown giornaliero 44.660 · netto/DD 6,51 · giorno peggiore -22.215 · correlazione massima fra membri 0,25, nelle code -0,22

| strategia | simbolo | famiglia | trade | netto | DD | netto/DD | giorno peggiore |
|---|---|---|---:|---:|---:|---:|---:|
| `PT8DAV_YM_VBO_001_15` | YM | VBO | 259 | 31.668 | 7.140 | 4,44 | -1.286 |
| `PT8DAV_FDAX_BOS_002_240` | FDAX | BOS | 143 | 19.881 | 16.569 | 1,20 | -3.958 |
| `PT8DAV_FDAX_LFH_001_15` | FDAX | LFH | 36 | 42.326 | 14.836 | 2,85 | -9.067 |
| `PT8DAV_NQ_LFD_002_60` | NQ | LFD | 44 | 39.459 | 19.181 | 2,06 | -7.411 |
| `PT8DAV_NQ_BSW_001_240` | NQ | BSW | 142 | 136.617 | 49.149 | 2,78 | -15.764 |
| `PT8DAV_GC_TFM_001_15` | GC | TFM | 206 | 20.845 | 15.503 | 1,34 | -2.449 |

## Piani per regime di mercato

Ogni trade prende il regime del proprio simbolo nel giorno di sessione in cui entra, calcolato con le sole barre chiuse prima (`MarketRegimeClassifier`). Datafeed: `piootoo-repository\datafeed-external\FTMO`. Un regime e' segnalato quando il piano ci perde, o quando ci perdono tutte le sue strategie con almeno 10 trade in quel regime (almeno due). E' una verifica, non un vincolo: il regime non entra nella costruzione.

Etichette: BP `@BP_240` (2020-07-05 - 2026-09-25), ES `@ES_1440` (2021-03-04 - 2026-09-24), FDAX `@FDAX_1440` (2021-03-05 - 2026-09-21), GC `@GC_240` (2020-08-26 - 2026-09-22), NQ `@NQ_1440` (2022-09-06 - 2026-09-21), YM `@YM_240` (2021-03-03 - 2026-09-25).

### Piano 1: nessun regime scoperto

| regime | trade | netto | netto per trade | strategie in perdita |
|---|---:|---:|---:|---:|
| trend-su | 452 | 104.879 | 232 | 1 su 6 |
| trend-giu | 123 | 8.399 | 68 | 2 su 4 |
| laterale | 832 | 214.490 | 258 | 0 su 8 |
| calma | 556 | 47.272 | 85 | 2 su 8 |
| normale | 371 | 70.305 | 190 | 1 su 8 |
| agitata | 480 | 210.191 | 438 | 1 su 8 |

4 trade senza etichetta (simbolo senza feed o prima dell'inizio delle etichette).

### Piano 2: nessun regime scoperto

| regime | trade | netto | netto per trade | strategie in perdita |
|---|---:|---:|---:|---:|
| trend-su | 210 | 57.371 | 273 | 0 su 8 |
| trend-giu | 68 | 53.883 | 792 | 0 su 4 |
| laterale | 489 | 138.611 | 283 | 2 su 8 |
| calma | 329 | 68.992 | 210 | 1 su 8 |
| normale | 187 | 14.972 | 80 | 3 su 8 |
| agitata | 251 | 165.901 | 661 | 0 su 8 |

2 trade senza etichetta (simbolo senza feed o prima dell'inizio delle etichette).

### Piano 3: nessun regime scoperto

| regime | trade | netto | netto per trade | strategie in perdita |
|---|---:|---:|---:|---:|
| trend-su | 219 | 18.039 | 82 | 1 su 6 |
| trend-giu | 68 | 37.103 | 546 | 1 su 3 |
| laterale | 537 | 242.726 | 452 | 0 su 6 |
| calma | 305 | 14.482 | 47 | 1 su 5 |
| normale | 204 | 100.686 | 494 | 0 su 4 |
| agitata | 315 | 182.700 | 580 | 0 su 6 |

6 trade senza etichetta (simbolo senza feed o prima dell'inizio delle etichette).

Profilo di ogni candidata per regime in `regimi.csv`.

## Correlazione fra i piani

|  | piano 1 | piano 2 | piano 3 |
|---|---:|---:|---:|
| piano 1 | 1,00 | 0,30 | 0,26 |
| piano 2 | 0,30 | 1,00 | 0,29 |
| piano 3 | 0,26 | 0,29 | 1,00 |

## Coppie piu' correlate fra le candidate

Le prime venti per correlazione nelle code: sono le strategie che perdono insieme nei giorni peggiori. Matrici complete in `correlazioni.csv` e `correlazioni-code.csv`.

| strategia | strategia | correlazione | nelle code |
|---|---|---:|---:|
| `PT8DAV_FDAX_RHL_002_60` | `PT8DAV_FDAX_RHL_001_30` | 0,77 | -0,17 |
| `PT8DAV_ES_LFH_001_15` | `PT8DAV_GC_VBO_001_15` | -0,01 | -0,18 |
| `PT8DAV_YM_VBO_001_15` | `PT8DAV_NQ_VBO_002_30` | 0,50 | -0,19 |
| `PT8DAV_BP_PCH_001_60` | `PT8DAV_FDAX_VBO_003_240` | -0,04 | -0,21 |
| `PT8DAV_FDAX_BRT_001_15` | `PT8DAV_ES_BSW_001_15` | 0,58 | -0,22 |
| `PT8DAV_NQ_BSW_001_240` | `PT8DAV_FDAX_BOS_002_240` | 0,15 | -0,22 |
| `PT8DAV_NQ_LFD_002_60` | `PT8DAV_GC_VBO_001_15` | 0,03 | -0,23 |
| `PT8DAV_NQ_VBO_002_30` | `PT8DAV_ES_MAC_001_30` | 0,06 | -0,24 |
| `PT8DAV_NQ_VBO_003_240` | `PT8DAV_FDAX_VBO_003_240` | 0,07 | -0,24 |
| `PT8DAV_FDAX_BRT_001_15` | `PT8DAV_ES_RHL_002_60` | -0,04 | -0,24 |
| `PT8DAV_BP_PCH_001_60` | `PT8DAV_GC_VBO_001_15` | 0,05 | -0,25 |
| `PT8DAV_NQ_SBO_003_60` | `PT8DAV_BP_RBM_001_30` | 0,06 | -0,25 |
| `PT8DAV_ES_RHL_002_60` | `PT8DAV_ES_RBU_002_60` | 0,19 | -0,25 |
| `PT8DAV_NQ_LFD_002_60` | `PT8DAV_ES_MAC_001_30` | 0,03 | -0,26 |
| `PT8DAV_NQ_VBO_002_30` | `PT8DAV_NQ_PCH_002_60` | 0,50 | -0,26 |
| `PT8DAV_GC_LFD_001_60` | `PT8DAV_BP_PCH_001_60` | -0,04 | -0,26 |
| `PT8DAV_NQ_VBO_002_30` | `PT8DAV_FDAX_BOS_002_240` | 0,09 | -0,26 |
| `PT8DAV_BP_RBM_001_30` | `PT8DAV_FDAX_SBO_002_30` | 0,05 | -0,27 |
| `PT8DAV_NQ_LFD_002_60` | `PT8DAV_FDAX_BOS_002_240` | 0,04 | -0,27 |
| `PT8DAV_NQ_LFD_002_60` | `PT8DAV_ES_RBU_002_60` | -0,01 | -0,27 |

## Candidate rimaste fuori dai piani

`PT8DAV_ES_RBU_002_60`, `PT8DAV_NQ_VBO_002_30`, `PT8DAV_NQ_PCH_002_60`, `PT8DAV_ES_BSW_001_15`, `PT8DAV_FDAX_RHL_001_30`, `PT8DAV_FDAX_VBO_003_240`, `PT8DAV_FDAX_SBO_002_30`.

