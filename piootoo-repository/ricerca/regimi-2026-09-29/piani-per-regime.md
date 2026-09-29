# Piani in produzione per regime di mercato

Generato da `piani.py`. Metodo delle etichette in `regimi.py` (etichette note all'ingresso).

## Vista FTMO 2022-2026: i tre piani sul conto

Fonti delle etichette: ES `datafeed-external\FTMO\@ES_1440.json`, FDAX `datafeed-external\FTMO\@FDAX_1440.json`, FESX `datafeed-external\FTMO\@FESX_240.json`, GC `datafeed-external\FTMO\@GC_240.json`, KC `datafeed-external\FTMO\@KC_240.json`, NQ `datafeed-external\FTMO\@NQ_1440.json`, YM `datafeed-external\FTMO\@YM_240.json`

Trade dal 2022-06-01 al 2026-09-24: 2749.

### Netto per regime del mercato di ogni trade

Tra parentesi: trade, e strategie in perdita in quel regime su quelle che ci hanno operato. `-` = regime in cui il piano perde.

| piano | trend-su | trend-giu | laterale | senza etichetta |
|---|---:|---:|---:|---:|
| FTMO-EUROPA | 47.514 (380; 0/3 in perdita) | 5.379 (78; 1/3 in perdita) | 202.531 (817; 0/3 in perdita) | 0 |
| CONTO | 80.152 (831; 2/11 in perdita) | 7.672 (177; 5/13 in perdita) | 567.604 (1732; 1/13 in perdita) | 9 |
| FTMO-USA | -721 (6; 1/1 in perdita) | -12.061 (19; 2/3 in perdita) | 92.245 (47; 0/3 in perdita) | 5 |
| FTMO-O4-X05 | 33.359 (445; 1/7 in perdita) | 14.353 (80; 2/7 in perdita) | 272.828 (868; 1/7 in perdita) | 4 |

| piano | calma | normale | agitata | senza etichetta |
|---|---:|---:|---:|---:|
| FTMO-EUROPA | -23.951 (433; 3/3 in perdita) | 108.319 (404; 0/3 in perdita) | 171.056 (438; 0/3 in perdita) | 0 |
| CONTO | 13.087 (938; 4/13 in perdita) | 216.723 (840; 1/13 in perdita) | 425.618 (962; 1/13 in perdita) | 9 |
| FTMO-USA | 8.264 (12; 0/3 in perdita) | 31.344 (12; 0/3 in perdita) | 39.855 (48; 0/3 in perdita) | 5 |
| FTMO-O4-X05 | 28.774 (493; 1/7 in perdita) | 77.060 (424; 1/7 in perdita) | 214.706 (476; 1/7 in perdita) | 4 |

### Giorni per regime dell'S&P (ES), P&L per giorno di uscita

| piano | regime ES | giorni | P&L medio/giorno | giorni in perdita | peggior giorno |
|---|---|---:|---:|---:|---:|
| CONTO | trend-su | 345 | 445 | 39% | -10.591 |
| CONTO | trend-giu | 49 | 245 | 41% | -24.036 |
| CONTO | laterale | 735 | 740 | 39% | -15.649 |
| CONTO | calma | 460 | 344 | 42% | -10.591 |
| CONTO | normale | 334 | 351 | 38% | -12.736 |
| CONTO | agitata | 335 | 1.294 | 37% | -24.036 |
| FTMO-EUROPA | trend-su | 345 | 81 | 37% | -7.372 |
| FTMO-EUROPA | trend-giu | 49 | -337 | 39% | -11.000 |
| FTMO-EUROPA | laterale | 735 | 302 | 36% | -10.008 |
| FTMO-EUROPA | calma | 460 | 152 | 39% | -7.372 |
| FTMO-EUROPA | normale | 334 | 116 | 36% | -10.008 |
| FTMO-EUROPA | agitata | 335 | 372 | 35% | -11.000 |
| FTMO-USA | trend-su | 345 | 31 | 0% | 0 |
| FTMO-USA | trend-giu | 49 | -488 | 12% | -9.252 |
| FTMO-USA | laterale | 735 | 113 | 1% | -14.292 |
| FTMO-USA | calma | 460 | 6 | 0% | -7.893 |
| FTMO-USA | normale | 334 | 80 | 1% | -4.385 |
| FTMO-USA | agitata | 335 | 119 | 3% | -14.292 |
| FTMO-O4-X05 | trend-su | 345 | 333 | 30% | -5.587 |
| FTMO-O4-X05 | trend-giu | 49 | 1.070 | 35% | -8.842 |
| FTMO-O4-X05 | laterale | 735 | 325 | 32% | -9.301 |
| FTMO-O4-X05 | calma | 460 | 186 | 30% | -6.898 |
| FTMO-O4-X05 | normale | 334 | 154 | 36% | -9.301 |
| FTMO-O4-X05 | agitata | 335 | 803 | 29% | -8.842 |

### I sei mesi peggiori del conto

| mese | netto | regime ES prevalente (direzione / volatilita') |
|---|---:|---|
| 2026-06 | -43.558 | trend-su / agitata |
| 2023-07 | -24.882 | trend-su / calma |
| 2023-09 | -24.779 | laterale / calma |
| 2023-04 | -22.715 | laterale / calma |
| 2024-10 | -15.035 | laterale / normale |
| 2023-01 | -14.354 | laterale / calma |


## Vista storica 2012-2025: le strategie di FTMO-O4-X05

Fonti delle etichette: ES `datafeed\@ES_1440.json`, FDAX `datafeed\@FDAX_240.json`, GC `datafeed\@GC_240.json`, KC `datafeed-external\FTMO\@KC_240.json`, NQ `datafeed\@NQ_1440.json`, YM `datafeed\@YM_240.json`

Trade dal 2012-03-20 al 2025-05-30: 3845.

### Netto per regime del mercato di ogni trade

Tra parentesi: trade, e strategie in perdita in quel regime su quelle che ci hanno operato. `-` = regime in cui il piano perde.

| piano | trend-su | trend-giu | laterale | senza etichetta |
|---|---:|---:|---:|---:|
| FTMO-O4-X05 | 308.736 (1032; 0/7 in perdita) | 173.214 (268; 0/6 in perdita) | 1.109.980 (2502; 0/7 in perdita) | 43 |

| piano | calma | normale | agitata | senza etichetta |
|---|---:|---:|---:|---:|
| FTMO-O4-X05 | 271.348 (1438; 0/7 in perdita) | 529.169 (1022; 1/7 in perdita) | 791.411 (1342; 0/7 in perdita) | 43 |

### Giorni per regime dell'S&P (ES), P&L per giorno di uscita

| piano | regime ES | giorni | P&L medio/giorno | giorni in perdita | peggior giorno |
|---|---|---:|---:|---:|---:|
| FTMO-O4-X05 | trend-su | 1057 | 420 | 27% | -19.074 |
| FTMO-O4-X05 | trend-giu | 155 | 471 | 29% | -26.144 |
| FTMO-O4-X05 | laterale | 2238 | 412 | 27% | -20.021 |
| FTMO-O4-X05 | calma | 1341 | 182 | 29% | -19.074 |
| FTMO-O4-X05 | normale | 1065 | 551 | 25% | -19.393 |
| FTMO-O4-X05 | agitata | 1044 | 582 | 27% | -26.144 |

### I sei mesi peggiori del conto

| mese | netto | regime ES prevalente (direzione / volatilita') |
|---|---:|---|
| 2018-10 | -36.128 | laterale / agitata |
| 2023-10 | -30.116 | trend-giu / normale |
| 2022-01 | -29.874 | laterale / agitata |
| 2019-05 | -29.521 | laterale / agitata |
| 2021-05 | -25.763 | trend-su / calma |
| 2018-12 | -23.143 | laterale / agitata |

