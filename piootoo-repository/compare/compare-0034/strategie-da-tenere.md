# Piano FTMO-NONG: strategie da tenere (spread / stop della ricerca <= 10%)

Criterio deciso l'11/09/2026: resta nel piano la strategia il cui spread mediano ai fill (tick FTMO, run cBot di compare-0033, 13 mesi) e' al massimo un decimo dello stop della ricerca, piu' quelle fuori soglia che sulla gamba cBot dei compare dal 0030 in poi mostrano un utile in crescita costante. Lo stop eseguito e' quello della ricerca per tutte (`StopMoneyPolicy.Multiplier = 1`). Misura dello spread in `spread-su-stop-ricerca.md`, curve delle escluse in `curve-cbot-escluse.csv`.

## Da tenere: 74

| strategia | spread/stop ricerca | p90 |
|---|---:|---:|
| `PTS_BP_TFM_001_60` | 0% | 0% |
| `PTS_BP_TFM_002_15` | 0% | 0% |
| `PTS_BTC_BIA_001_60` | 2% | 13.6% |
| `PTS_BTC_PCH_001_240` | 0.3% | 1.2% |
| `PTS_BTC_TFU_001_60` | 2% | 21.1% |
| `PTS_BTC_TFU_002_60` | 2.1% | 20.8% |
| `PTS_CC_SBO_001_60` | 11.1% | 12.3% |
| `PTS_CL_MAC_001_30` | 7% | 9% |
| `PTS_CT_TFU_001_240` | 7.5% | 7.5% |
| `PTS_ES_BSW_001_60` | 0.6% | 0.6% |
| `PTS_ES_BSW_002_15` | 0.9% | 1% |
| `PTS_ES_BSW_003_15` | 0.9% | 1% |
| `PTS_ES_PCH_001_60` | 0.7% | 0.7% |
| `PTS_ES_PCH_002_60` | 0.7% | 0.8% |
| `PTS_ES_PCH_003_1440` | 2.8% | 2.8% |
| `PTS_ES_PCH_004_240` | 0.6% | 0.6% |
| `PTS_ES_SBO_001_15` | 0.7% | 0.7% |
| `PTS_ES_SBO_002_240` | 0.7% | 0.8% |
| `PTS_ES_SBO_003_240` | 0.7% | 0.8% |
| `PTS_ES_TFM_001_1440` | 2.8% | 2.8% |
| `PTS_ES_TFM_002_1440` | 2.8% | 3% |
| `PTS_ES_TFU_001_1440` | 1.4% | 1.6% |
| `PTS_FDAX_MAC_001_240` | 1.7% | 2.8% |
| `PTS_FDAX_SBO_002_1440` | 7.3% | 9.5% |
| `PTS_FDAX_TFU_001_1440` | 8% | 9.5% |
| `PTS_FDAX_TFU_002_1440` | 7.4% | 9.5% |
| `PTS_GC_PCH_001_60` | 2.2% | 2.8% |
| `PTS_GC_PCH_004_240` | 9.5% | 13.6% |
| `PTS_GC_PCH_005_240` | 1.6% | 2.4% |
| `PTS_GC_RHL_001_60` | 2.5% | 3.6% |
| `PTS_GC_RHL_002_60` | 2.5% | 3.9% |
| `PTS_GC_TFM_001_240` | 1% | 1.4% |
| `PTS_GC_TFU_001_30` | 2.9% | 3.9% |
| `PTS_NQ_PCH_003_30` | 1.2% | 1.5% |
| `PTS_NQ_PCH_004_30` | 1.3% | 1.7% |
| `PTS_NQ_PCH_007_240` | 1% | 1.3% |
| `PTS_NQ_PCH_008_240` | 0.7% | 0.9% |
| `PTS_NQ_RBM_001_15` | 1.6% | 1.9% |
| `PTS_NQ_SBO_001_15` | 0.6% | 0.8% |
| `PTS_NQ_SBO_002_15` | 0.8% | 1% |
| `PTS_NQ_SBO_003_15` | 5.8% | 7.8% |
| `PTS_NQ_SBO_004_60` | 5.8% | 7.6% |
| `PTS_NQ_SBO_005_1440` | 3.2% | 3.8% |
| `PTS_NQ_TFM_001_60` | 2.9% | 2.9% |
| `PTS_NQ_TFM_002_15` | 1.1% | 1.3% |
| `PTS_NQ_TFM_003_15` | 1.2% | 1.5% |
| `PTS_NQ_TFM_005_15` | 0.8% | 1% |
| `PTS_NQ_TFM_006_30` | 6.3% | 7.6% |
| `PTS_NQ_TFM_007_30` | 0.6% | 0.8% |
| `PTS_NQ_TFM_010_60` | 1.2% | 1.5% |
| `PTS_NQ_TFM_011_60` | 2.8% | 3.1% |
| `PTS_NQ_TFM_012_1440` | 3.3% | 3.8% |
| `PTS_NQ_TFM_013_1440` | 3.4% | 4% |
| `PTS_NQ_TFM_015_240` | 6% | 7.9% |
| `PTS_NQ_TFU_001_15` | 0.7% | 1% |
| `PTS_NQ_TFU_002_15` | 2.3% | 2.7% |
| `PTS_NQ_TFU_003_15` | 1.7% | 2.2% |
| `PTS_NQ_TFU_004_60` | 3.9% | 4% |
| `PTS_NQ_TFU_005_60` | 0.7% | 0.8% |
| `PTS_NQ_TFU_006_1440` | 3.2% | 3.8% |
| `PTS_NQ_TFU_007_1440` | 3.2% | 3.8% |
| `PTS_NQ_TFU_008_240` | 5.8% | 7.8% |
| `PTS_NQ_VBO_001_1440` | 3.4% | 3.8% |
| `PTS_NQ_VBO_002_240` | 1.4% | 1.7% |
| `PTS_SB_TFM_001_240` | 6.5% | 7.5% |
| `PTS_YM_BIA_001_240` | 0.5% | 0.6% |
| `PTS_YM_SBO_001_240` | 4% | 5.2% |
| `PTS_YM_SBO_002_240` | 4.2% | 5.2% |
| `PTS_YM_TFM_001_240` | 0.5% | 0.5% |
| `PTS_YM_TFM_002_240` | 0.2% | 0.3% |
| `PTS_YM_TFM_003_240` | 0.8% | 1% |
| `PTS_YM_TFU_001_60` | 0.4% | 0.5% |
| `PTS_YM_TFU_002_60` | 1% | 1.3% |
| `PTS_YM_TFU_003_60` | 4% | 5% |

Le tre BTC a 60 minuti (`PTS_BTC_TFU_001_60`, `PTS_BTC_TFU_002_60`, `PTS_BTC_BIA_001_60`) passano in mediana (2%) ma il p90 sta fra il 14% e il 21%: lo spread si allarga a scatti in tutte le fasce orarie, non in una finestra, quindi restano nel piano e il controllo e' `MaxSpreadPercentOfStop` del cBot al momento del fill.

## Fuori soglia: la curva sulla gamba cBot dei compare 0030, 0032, 0033, 0034

0030 (01/09/25 -> 11/03/26) e 0032 (31/08/25 -> 05/09/26) hanno girato con lo stop della ricerca per tutte; 0033 con stop x2 e target x2 su 33 strategie; 0034 (fino al 29/01/26, in corso) con stop x3. Nessuna delle strategie sotto e' toccata dall'allargamento, salvo `CC_TFM_002_60` e le quattro NQ con stop da 12,5 punti. Netto in USD sulle size del piano. Crescente = netto > 0, R2 >= 0,6, chiusura ad almeno il 70% del picco, >= 8 trade (lo stesso criterio di compare-0033 par. 7).

| strategia | run | trade | netto | PF | R2 | fine/picco | crescente |
|---|---|---:|---:|---:|---:|---:|---|
| `PTS_CC_PCH_001_240` | 0030 | 10 | 66 | 1.28 | -0.73 | 0.31 |  |
| `PTS_CC_PCH_001_240` | 0032 | 20 | -5 | 0.99 | -0.17 | -0.02 |  |
| `PTS_CC_PCH_001_240` | 0033 | 20 | -5 | 0.99 | -0.17 | -0.02 |  |
| `PTS_CC_PCH_001_240` | 0034 | 8 | 107 | 1.63 | -0.73 | 0.53 |  |
| `PTS_CC_SBO_001_60` | 0030 | 7 | 2359 | 5.33 | 0.87 | 0.93 |  |
| `PTS_CC_SBO_001_60` | 0032 | 11 | 1621 | 2.31 | 0.39 | 0.65 |  |
| `PTS_CC_SBO_001_60` | 0033 | 11 | 1621 | 2.31 | 0.39 | 0.65 |  |
| `PTS_CC_SBO_001_60` | 0034 | 5 | 1940 | 6.5 | 0.91 | 0.92 |  |
| `PTS_CC_TFM_001_60` | 0030 | 3 | -314 | 0 | -1 | 0 |  |
| `PTS_CC_TFM_001_60` | 0032 | 4 | -410 | 0 | -1 | 0 |  |
| `PTS_CC_TFM_001_60` | 0033 | 4 | -410 | 0 | -1 | 0 |  |
| `PTS_CC_TFM_001_60` | 0034 | 2 | -203 | 0 | -1 | 0 |  |
| `PTS_CC_TFM_002_60` | 0030 | 8 | -557 | 0.27 | -0.67 | 0 |  |
| `PTS_CC_TFM_002_60` | 0032 | 14 | -522 | 0.53 | -0.21 | 0 |  |
| `PTS_CC_TFM_002_60` | 0033 | 12 | -13 | 0.99 | -0.01 | 0 |  |
| `PTS_CC_TFM_002_60` | 0034 | 6 | -308 | 0.66 | 0 | 0 |  |
| `PTS_FDAX_PCH_001_240` | 0030 | 43 | -1644 | 0.31 | -1 | -2.23 |  |
| `PTS_FDAX_PCH_001_240` | 0032 | 96 | 885 | 1.33 | 0.08 | 0.84 |  |
| `PTS_FDAX_PCH_001_240` | 0033 | 96 | 885 | 1.33 | 0.08 | 0.84 |  |
| `PTS_FDAX_PCH_001_240` | 0034 | 35 | 229 | 1.22 | -0.83 | 0.26 |  |
| `PTS_FDAX_SBO_001_240` | 0030 | 27 | -1104 | 0.26 | -0.96 | -3.26 |  |
| `PTS_FDAX_SBO_001_240` | 0032 | 60 | -226 | 0.88 | -0.57 | -0.35 |  |
| `PTS_FDAX_SBO_001_240` | 0033 | 60 | -226 | 0.88 | -0.57 | -0.35 |  |
| `PTS_FDAX_SBO_001_240` | 0034 | 20 | 436 | 1.73 | 0.56 | 0.67 |  |
| `PTS_FDAX_VBO_001_240` | 0030 | 27 | -1569 | 0 | -0.99 | 0 |  |
| `PTS_FDAX_VBO_001_240` | 0032 | 59 | 1112 | 1.7 | 0.53 | 0.94 |  |
| `PTS_FDAX_VBO_001_240` | 0033 | 59 | 1112 | 1.7 | 0.53 | 0.94 |  |
| `PTS_FDAX_VBO_001_240` | 0034 | 22 | -650 | 0 | -1 | 0 |  |
| `PTS_KC_SBO_001_240` | 0030 | 20 | -585 | 0.72 | -0.11 | 0 |  |
| `PTS_KC_SBO_001_240` | 0032 | 45 | 853 | 1.86 | 0.61 | 0.94 | si |
| `PTS_KC_SBO_001_240` | 0033 | 45 | 853 | 1.86 | 0.61 | 0.94 | si |
| `PTS_KC_SBO_001_240` | 0034 | 20 | -373 | 0.28 | -0.83 | 0 |  |
| `PTS_NQ_PCH_001_15` | 0030 | 48 | -1152 | 0.39 | -0.8 | -4.39 |  |
| `PTS_NQ_PCH_001_15` | 0032 | 96 | 453 | 1.2 | 0.17 | 0.75 |  |
| `PTS_NQ_PCH_001_15` | 0033 | 96 | 529 | 1.17 | 0.32 | 0.72 |  |
| `PTS_NQ_PCH_001_15` | 0034 | 43 | 597 | 1.4 | 0.69 | 0.64 |  |
| `PTS_NQ_PCH_002_15` | 0030 | 33 | -577 | 0.51 | -0.71 | -2.52 |  |
| `PTS_NQ_PCH_002_15` | 0032 | 71 | -177 | 0.9 | -0.46 | -0.53 |  |
| `PTS_NQ_PCH_002_15` | 0033 | 71 | -178 | 0.93 | -0.33 | -0.37 |  |
| `PTS_NQ_PCH_002_15` | 0034 | 31 | 541 | 1.54 | 0.66 | 0.68 |  |
| `PTS_NQ_SBO_006_240` | 0030 | 15 | -496 | 0 | -1 | 0 |  |
| `PTS_NQ_SBO_006_240` | 0032 | 30 | -884 | 0 | -1 | 0 |  |
| `PTS_NQ_SBO_006_240` | 0033 | 33 | 1799 | 3.03 | 0.77 | 0.9 | si |
| `PTS_NQ_SBO_006_240` | 0034 | 18 | 690 | 2.45 | 0.64 | 0.89 | si |
| `PTS_NQ_TFM_008_30` | 0030 | 9 | -433 | 0 | -0.96 | 0 |  |
| `PTS_NQ_TFM_008_30` | 0032 | 23 | 162 | 1.26 | 0.03 | 1 |  |
| `PTS_NQ_TFM_008_30` | 0033 | 23 | 163 | 1.26 | 0.03 | 1 |  |
| `PTS_NQ_TFM_008_30` | 0034 | 9 | -275 | 0 | -1 | 0 |  |
| `PTS_NQ_TFM_009_60` | 0030 | 6 | -309 | 0 | -0.97 | 0 |  |
| `PTS_NQ_TFM_009_60` | 0032 | 23 | -15 | 0.98 | -0.09 | 0 |  |
| `PTS_NQ_TFM_009_60` | 0033 | 23 | 32 | 1.03 | -0.04 | 1 |  |
| `PTS_NQ_TFM_009_60` | 0034 | 7 | -185 | 0.62 | -0.36 | 0 |  |
| `PTS_NQ_TFM_014_240` | 0030 | 10 | -427 | 0 | -0.99 | 0 |  |
| `PTS_NQ_TFM_014_240` | 0032 | 20 | -588 | 0 | -1 | 0 |  |
| `PTS_NQ_TFM_014_240` | 0033 | 21 | 3333 | 4.15 | 0.08 | 0.98 |  |
| `PTS_NQ_TFM_014_240` | 0034 | 11 | 608 | 1.77 | -0.01 | 0.46 |  |
| `PTS_PL_TFM_001_240` | 0030 | 7 | -247 | 0 | -0.99 | 0 |  |
| `PTS_PL_TFM_001_240` | 0032 | 19 | -1301 | 0.44 | -0.72 | -4.8 |  |
| `PTS_PL_TFM_001_240` | 0033 | 19 | -1301 | 0.44 | -0.72 | -4.8 |  |
| `PTS_PL_TFM_001_240` | 0034 | 6 | 22 | 1.05 | -0.56 | 0.09 |  |

Regge su tutti i run solo `PTS_CC_SBO_001_60` (spread/stop 11%): netto positivo in 0030, 0032, 0033 e 0034, PF fra 2,3 e 6,5, R2 0,87 / 0,39 / 0,91. Resta nel piano. `NQ_SBO_006_240` e `NQ_PCH_001_15` sono in utile nei run piu' recenti ma in perdita in 0030 e (la prima) in 0032 con lo stesso stop: non e' crescita costante. Le altre sono in perdita o intorno allo zero.

## Da spegnere: 14

| strategia | spread/stop ricerca | p90 |
|---|---:|---:|
| `PTS_PL_TFM_001_240` | 161.6% | 194.6% |
| `PTS_CC_PCH_001_240` | 80.8% | 84.8% |
| `PTS_KC_SBO_001_240` | 46.3% | 50.7% |
| `PTS_CC_TFM_001_60` | 21.4% | 21.6% |
| `PTS_CC_TFM_002_60` | 19.6% | 21.6% |
| `PTS_NQ_TFM_008_30` | 15.2% | 15.6% |
| `PTS_NQ_SBO_006_240` | 15.2% | 16.6% |
| `PTS_FDAX_VBO_001_240` | 12.9% | 14.9% |
| `PTS_FDAX_PCH_001_240` | 12.9% | 14.9% |
| `PTS_FDAX_SBO_001_240` | 12.9% | 13.9% |
| `PTS_NQ_PCH_001_15` | 11.6% | 15.2% |
| `PTS_NQ_PCH_002_15` | 11.6% | 15.2% |
| `PTS_NQ_TFM_009_60` | 11.6% | 15.2% |
| `PTS_NQ_TFM_014_240` | 11.6% | 15.2% |

Per `DisabledStrategies` del piano (`plans.json`):

```json
"DisabledStrategies": [
  "PTS_PL_TFM_001_240",
  "PTS_CC_PCH_001_240",
  "PTS_KC_SBO_001_240",
  "PTS_CC_TFM_001_60",
  "PTS_CC_TFM_002_60",
  "PTS_NQ_TFM_008_30",
  "PTS_NQ_SBO_006_240",
  "PTS_FDAX_VBO_001_240",
  "PTS_FDAX_PCH_001_240",
  "PTS_FDAX_SBO_001_240",
  "PTS_NQ_PCH_001_15",
  "PTS_NQ_PCH_002_15",
  "PTS_NQ_TFM_009_60",
  "PTS_NQ_TFM_014_240"
]
```
