# RHL Z 4h contro 100 ingressi long casuali con le stesse uscite

Regola identica a PT3B_FDAX_RHL_001_240 (trovata sul DAX), nessun parametro ritoccato.

Costi FTMO (spread 0.85 punti, swap), orologio al minuto, semi 1001-1100. Percentile = quota di semi che fa peggio della strategia.

## FTMO dal 2020, fuori campione per la regola

RHL 52 trade, netto 5,349. RAN p = 0.01136, trade medi 50.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 5,348.59 | -6,562.12 | -129.32 | 6,032.16 | 92% |
| average trade | 102.86 | -123.69 | -2.54 | 125.25 | 89% |
| profit factor | 1.42 | 0.51 | 0.99 | 1.88 | 83% |
| netto / DD | 1.65 | -0.88 | -0.02 | 2.84 | 84% |

