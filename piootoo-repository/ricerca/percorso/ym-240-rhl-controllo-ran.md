# RHL YM 4h contro 100 ingressi long casuali con le stesse uscite

Regola identica a PT3B_FDAX_RHL_001_240 (trovata sul DAX), nessun parametro ritoccato.

Costi FTMO (spread 2.10 punti, swap), orologio al minuto, semi 1001-1100. Percentile = quota di semi che fa peggio della strategia.

## FTMO dal 2020, fuori campione per la regola

RHL 24 trade, netto 23,633. RAN p = 0.00559, trade medi 24.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 23,632.77 | -13,363.41 | -812.93 | 10,602.92 | 100% |
| average trade | 984.70 | -558.89 | -38.71 | 453.19 | 100% |
| profit factor | 3.07 | 0.39 | 0.94 | 2.52 | 98% |
| netto / DD | 5.90 | -0.88 | -0.16 | 3.27 | 99% |

