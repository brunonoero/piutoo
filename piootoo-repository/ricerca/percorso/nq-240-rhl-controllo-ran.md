# RHL NQ 4h contro 100 ingressi long casuali con le stesse uscite

Regola identica a PT3B_FDAX_RHL_001_240 (trovata sul DAX), nessun parametro ritoccato.

Costi FTMO (spread 1.45 punti, swap), orologio al minuto, semi 1001-1100. Percentile = quota di semi che fa peggio della strategia.

## FTMO dal 2020, fuori campione per la regola

RHL 42 trade, netto 53,555. RAN p = 0.01327, trade medi 43.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 53,554.67 | -35,857.83 | 988.04 | 47,750.16 | 99% |
| average trade | 1,275.11 | -880.70 | 17.04 | 975.07 | 100% |
| profit factor | 1.76 | 0.58 | 1.01 | 2.03 | 90% |
| netto / DD | 2.66 | -0.90 | 0.04 | 4.07 | 90% |

## interno 2008-2020, mai visto

RHL 49 trade, netto 34,729. RAN p = 0.00486, trade medi 48.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 34,729.16 | -17,519.34 | -3,553.79 | 9,338.93 | 100% |
| average trade | 708.76 | -430.10 | -71.84 | 195.97 | 100% |
| profit factor | 1.78 | 0.25 | 0.77 | 1.81 | 93% |
| netto / DD | 3.12 | -1.00 | -0.48 | 2.55 | 97% |

