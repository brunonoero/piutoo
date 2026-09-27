# RHL FESX 4h contro 100 ingressi long casuali con le stesse uscite

Regola identica a PT3B_FDAX_RHL_001_240 (trovata sul DAX), nessun parametro ritoccato.

Costi FTMO (spread 1.46 punti, swap), orologio al minuto, semi 1001-1100. Percentile = quota di semi che fa peggio della strategia.

## FTMO dal 2020, fuori campione per la regola

RHL 67 trade, netto 7,916. RAN p = 0.01873, trade medi 65.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 7,916.38 | -5,415.06 | -2,012.19 | 3,059.54 | 100% |
| average trade | 118.15 | -87.02 | -30.03 | 43.54 | 100% |
| profit factor | 1.74 | 0.53 | 0.81 | 1.39 | 98% |
| netto / DD | 5.60 | -0.96 | -0.40 | 1.56 | 100% |

