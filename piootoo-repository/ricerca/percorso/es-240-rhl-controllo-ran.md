# RHL ES 4h contro 100 ingressi long casuali con le stesse uscite

Regola identica a PT3B_FDAX_RHL_001_240 (trovata sul DAX), nessun parametro ritoccato.

Costi FTMO (spread 0.60 punti, swap), orologio al minuto, semi 1001-1100. Percentile = quota di semi che fa peggio della strategia.

## FTMO dal 2020, fuori campione per la regola

RHL 35 trade, netto 28,363. RAN p = 0.00816, trade medi 36.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 28,362.69 | -15,834.70 | -1,369.33 | 23,653.26 | 96% |
| average trade | 810.36 | -491.52 | -44.30 | 652.02 | 98% |
| profit factor | 1.97 | 0.54 | 0.95 | 2.23 | 91% |
| netto / DD | 4.69 | -0.85 | -0.10 | 3.23 | 99% |

