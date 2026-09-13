# compare-0040 — cBot cTrader contro backtest interno (feed FTMO)

Generato: 2026-09-12 06:59Z. Finestra di sovrapposizione usata per il confronto trade: `2025-08-31` → `2025-11-13` (ingressi).

## 0. Perimetro dei due run

| | interno | cBot |
|---|---|---|
| trade totali | 3567 | 786 |
| primo ingresso | 2025-08-31 06:33 | 2025-08-31 06:33 |
| ultimo ingresso | 2026-08-31 10:08 | 2025-11-12 07:14 |
| trade nella sovrapposizione | 764 | 786 |
| contratti per trade | 1 valori: 1 | 2 valori: 0.1,0.096 |
| netto totale | -50,725 | 2,354 |
| netto per contratto, sovrapposizione | 11,230 | 23,632 |
| lordo per contratto, sovrapposizione | 14,286 | 44,134 |
| commissioni per contratto, sovrapposizione | 3,056 | 20,184 |
| swap, sovrapposizione (grezzo) | 0 | -31 |

Log cBot: 0 righe; chiusure 0, fill con spread 0, ingressi scartati 0, annullati 0, bracket riancorati 0, chiusure per flat weekend 0, righe non interpretate 0.
Chiusure del log senza trade corrispondente: 0; fill senza trade: 0.

### 0b. Commissioni per contratto e per trade (sovrapposizione)

| simbolo | int trade | int comm/trade/ctr | cBot trade | cBot comm/trade/ctr | cBot swap/trade/ctr | cBot lordo/ctr | cBot netto/ctr |
|---|---:|---:|---:|---:|---:|---:|---:|
| BP | 4 | 4.0 | 2 | 5.0 | -79.3 | 2,508 | 2,339 |
| BTC | 138 | 4.0 | 136 | 24.0 | 0.0 | -8,274 | -11,538 |
| CC | 3 | 4.0 | 2 | 3.6 | 0.0 | 3,575 | 3,568 |
| CL | 8 | 4.0 | 7 | 5.6 | 0.8 | -2,991 | -3,025 |
| ES | 83 | 4.0 | 73 | 23.2 | -0.1 | -7,261 | -8,959 |
| FDAX | 26 | 4.0 | 26 | 45.4 | 0.0 | -14,616 | -15,797 |
| GC | 97 | 4.0 | 103 | 26.8 | -1.6 | -9,078 | -12,004 |
| NG | 39 | 4.0 | 74 | 1.8 | 0.0 | -48,350 | -48,483 |
| NQ | 261 | 4.0 | 274 | 35.4 | 0.0 | 146,585 | 136,891 |
| SB | 7 | 4.0 | 3 | 1.2 | 0.0 | 426 | 422 |
| YM | 98 | 4.0 | 86 | 16.2 | 0.0 | -18,389 | -19,782 |

FDAX: rapporto lordo cBot / lordo interno sulle coppie con stessa uscita (n=21): mediana 1.174 — il conto cTrader è in USD e GER40 quota in EUR, l'interno usa 25 EUR/punto senza conversione.

## 1. Differenze di trade per strategia (finestra di sovrapposizione)

Match = stessa strategia, stesso verso, ingresso entro una barra della strategia. Netto per contratto future.

| strategia | tf | int | cbot | match | solo int | solo cbot | Δentry mediano (min) | netto/ctr int | netto/ctr cbot | Δ netto/ctr | int in match (netto) | cbot in match (netto) | int in match (lordo) | cbot in match (lordo) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 60 | 3 | 1 | 1 | 2 | 0 | 0.4 | -395 | 1,885 | 2,280 | 409 | 1,885 | 413 | 2,003 |
| PTS_BP_TFM_002_15 | 15 | 1 | 1 | 1 | 0 | 0 | 0.3 | 496 | 454 | -42 | 496 | 454 | 500 | 504 |
| PTS_BTC_BIA_001_60 | 60 | 73 | 72 | 44 | 29 | 28 | 0.3 | 6,261 | -1,384 | -7,645 | -3,774 | -3,118 | -3,598 | -2,062 |
| PTS_BTC_PCH_001_240 | 240 | 49 | 48 | 47 | 2 | 1 | 0.4 | -9,238 | -4,946 | 4,292 | -5,730 | -3,067 | -5,542 | -1,939 |
| PTS_BTC_TFU_001_60 | 60 | 5 | 5 | 5 | 0 | 0 | 0.2 | -1,270 | -1,667 | -397 | -1,270 | -1,667 | -1,250 | -1,547 |
| PTS_BTC_TFU_002_60 | 60 | 11 | 11 | 10 | 1 | 1 | 0.4 | -2,794 | -3,541 | -747 | -2,540 | -3,260 | -2,500 | -3,020 |
| PTS_CC_SBO_001_60 | 60 | 3 | 2 | 2 | 1 | 0 | 0.0 | 12,316 | 3,568 | -8,748 | 3,634 | 3,568 | 3,642 | 3,575 |
| PTS_CL_MAC_001_30 | 30 | 8 | 7 | 7 | 1 | 0 | 0.0 | -4,137 | -3,025 | 1,113 | -3,133 | -3,025 | -3,105 | -2,991 |
| PTS_ES_BSW_001_60 | 60 | 5 | 4 | 4 | 1 | 0 | 0.0 | 3,556 | 8,531 | 4,975 | 8,560 | 8,531 | 8,576 | 8,624 |
| PTS_ES_BSW_002_15 | 15 | 3 | 3 | 3 | 0 | 0 | 0.0 | -1,512 | -1,614 | -102 | -1,512 | -1,614 | -1,500 | -1,545 |
| PTS_ES_BSW_003_15 | 15 | 9 | 8 | 8 | 1 | 0 | 0.1 | 6,411 | 9,593 | 3,182 | 9,415 | 9,593 | 9,447 | 9,779 |
| PTS_ES_PCH_001_60 | 60 | 6 | 5 | 5 | 1 | 0 | 0.2 | 1,363 | 627 | -737 | 797 | 627 | 817 | 743 |
| PTS_ES_PCH_002_60 | 60 | 7 | 7 | 7 | 0 | 0 | -0.8 | -2,833 | -2,891 | -58 | -2,833 | -2,891 | -2,805 | -2,729 |
| PTS_ES_PCH_003_1440 | 1440 | 2 | 2 | 2 | 0 | 0 | 0.3 | -2,018 | -2,058 | -40 | -2,018 | -2,058 | -2,010 | -2,012 |
| PTS_ES_PCH_004_240 | 240 | 23 | 25 | 21 | 2 | 4 | 0.0 | -2,592 | -10,960 | -8,368 | 2,416 | -5,790 | 2,500 | -5,303 |
| PTS_ES_SBO_001_15 | 15 | 1 | 1 | 1 | 0 | 0 | 0.6 | -4,004 | -4,026 | -22 | -4,004 | -4,026 | -4,000 | -4,003 |
| PTS_ES_SBO_002_240 | 240 | 3 | 1 | 1 | 2 | 0 | 0.9 | -5,102 | -4,026 | 1,076 | 726 | -4,026 | 730 | -4,003 |
| PTS_ES_SBO_003_240 | 240 | 5 | 3 | 3 | 2 | 0 | 0.2 | -468 | -4,629 | -4,161 | -2,460 | -4,629 | -2,448 | -4,559 |
| PTS_ES_TFM_001_1440 | 1440 | 2 | 2 | 2 | 0 | 0 | 0.5 | 4,992 | 4,954 | -38 | 4,992 | 4,954 | 5,000 | 5,003 |
| PTS_ES_TFM_002_1440 | 1440 | 5 | 4 | 4 | 1 | 0 | 0.5 | 2,045 | 5,920 | 3,875 | 3,049 | 5,920 | 3,065 | 6,013 |
| PTS_ES_TFU_001_1440 | 1440 | 12 | 8 | 8 | 4 | 0 | 0.3 | -4,251 | -8,379 | -4,128 | -4,235 | -8,379 | -4,203 | -8,191 |
| PTS_FDAX_MAC_001_240 | 240 | 4 | 4 | 4 | 0 | 0 | 0.0 | -11,120 | -13,195 | -2,075 | -11,120 | -13,195 | -11,104 | -13,014 |
| PTS_FDAX_SBO_002_1440 | 1440 | 2 | 2 | 2 | 0 | 0 | 0.8 | -2,008 | -2,528 | -520 | -2,008 | -2,528 | -2,000 | -2,437 |
| PTS_FDAX_TFU_001_1440 | 1440 | 9 | 9 | 9 | 0 | 0 | 0.1 | 4,964 | 5,285 | 321 | 4,964 | 5,285 | 5,000 | 5,693 |
| PTS_FDAX_TFU_002_1440 | 1440 | 11 | 11 | 10 | 1 | 1 | 0.2 | -4,044 | -5,358 | -1,314 | -3,040 | -4,110 | -3,000 | -3,656 |
| PTS_GC_PCH_001_60 | 60 | 3 | 4 | 3 | 0 | 1 | 0.6 | -512 | -3,148 | -2,636 | -512 | -857 | -500 | -777 |
| PTS_GC_PCH_004_240 | 240 | 52 | 54 | 51 | 1 | 3 | 0.3 | -4,156 | -10,141 | -5,985 | -3,652 | -9,490 | -3,448 | -8,040 |
| PTS_GC_PCH_005_240 | 240 | 25 | 28 | 23 | 2 | 5 | 0.3 | -1,172 | 914 | 2,086 | 1,152 | 694 | 1,244 | 1,393 |
| PTS_GC_RHL_001_60 | 60 | 6 | 6 | 6 | 0 | 0 | 0.9 | 5,947 | 5,928 | -19 | 5,947 | 5,928 | 5,971 | 6,089 |
| PTS_GC_RHL_002_60 | 60 | 7 | 7 | 7 | 0 | 0 | 1.0 | 1,573 | 1,876 | 303 | 1,573 | 1,876 | 1,601 | 2,064 |
| PTS_GC_TFU_001_30 | 30 | 4 | 4 | 4 | 0 | 0 | 0.2 | -7,016 | -7,433 | -417 | -7,016 | -7,433 | -7,000 | -7,326 |
| PTS_NG_BSW_001_30 | 30 | 4 | 4 | 4 | 0 | 0 | 0.3 | -4,626 | -937 | 3,689 | -4,626 | -937 | -4,610 | -930 |
| PTS_NG_TFM_001_240 | 240 | 3 | 5 | 2 | 1 | 3 | 0.8 | -5,252 | -7,309 | -2,057 | -3,498 | -3,164 | -3,490 | -3,160 |
| PTS_NG_TFM_002_240 | 240 | 5 | 10 | 4 | 1 | 6 | 0.8 | -1,270 | -3,298 | -2,028 | -1,016 | -3,047 | -1,000 | -3,040 |
| PTS_NG_TFM_003_30 | 30 | 4 | 13 | 4 | 0 | 9 | 0.8 | -3,016 | -2,333 | 683 | -3,016 | -3,037 | -3,000 | -3,030 |
| PTS_NG_TFM_004_60 | 60 | 0 | 2 | 0 | 0 | 2 | - | 0 | 26 | 26 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_005_30 | 30 | 8 | 12 | 8 | 0 | 4 | 0.4 | -8,502 | -8,372 | 130 | -8,502 | -8,364 | -8,470 | -8,350 |
| PTS_NG_TFM_006_60 | 60 | 4 | 9 | 3 | 1 | 6 | 0.8 | -5,016 | -11,626 | -6,610 | -3,762 | -4,115 | -3,750 | -4,110 |
| PTS_NG_TFU_001_240 | 240 | 2 | 5 | 2 | 0 | 3 | 1.0 | -2,008 | -5,039 | -3,031 | -2,008 | -2,014 | -2,000 | -2,010 |
| PTS_NG_TFU_002_240 | 240 | 4 | 7 | 4 | 0 | 3 | 0.5 | -4,016 | -7,063 | -3,047 | -4,016 | -4,017 | -4,000 | -4,010 |
| PTS_NG_TFU_003_60 | 60 | 5 | 7 | 3 | 2 | 4 | 0.2 | -3,520 | -2,533 | 987 | -4,512 | -4,525 | -4,500 | -4,520 |
| PTS_NQ_PCH_003_30 | 30 | 21 | 20 | 15 | 6 | 5 | 0.3 | 17,318 | 20,949 | 3,631 | 1,562 | 2,170 | 1,622 | 2,701 |
| PTS_NQ_PCH_004_30 | 30 | 11 | 11 | 10 | 1 | 1 | 0.3 | 19,527 | 18,863 | -664 | 15,368 | 14,906 | 15,408 | 15,260 |
| PTS_NQ_PCH_007_240 | 240 | 4 | 4 | 4 | 0 | 0 | 0.2 | -1,114 | -1,630 | -516 | -1,114 | -1,630 | -1,098 | -1,488 |
| PTS_NQ_PCH_008_240 | 240 | 11 | 10 | 10 | 1 | 0 | 0.2 | 10,728 | 10,878 | 150 | 11,799 | 10,878 | 11,839 | 11,232 |
| PTS_NQ_RBM_001_15 | 15 | 38 | 32 | 25 | 13 | 7 | 0.6 | -1,085 | -6,235 | -5,151 | 3,611 | 208 | 3,711 | 1,087 |
| PTS_NQ_SBO_001_15 | 15 | 6 | 7 | 6 | 0 | 1 | 0.4 | 9,976 | 12,790 | 2,814 | 9,976 | 9,820 | 10,000 | 10,032 |
| PTS_NQ_SBO_002_15 | 15 | 6 | 7 | 6 | 0 | 1 | 0.4 | 1,976 | -2,360 | -4,336 | 1,976 | 1,682 | 2,000 | 1,894 |
| PTS_NQ_SBO_003_15 | 15 | 8 | 9 | 8 | 0 | 1 | 0.5 | -4,032 | -5,086 | -1,054 | -4,032 | -4,497 | -4,000 | -4,213 |
| PTS_NQ_SBO_004_60 | 60 | 10 | 11 | 10 | 0 | 1 | 0.4 | -5,040 | -6,311 | -1,271 | -5,040 | -5,720 | -5,000 | -5,366 |
| PTS_NQ_SBO_005_1440 | 1440 | 2 | 2 | 2 | 0 | 0 | 0.7 | 17,541 | 17,600 | 58 | 17,541 | 17,600 | 17,549 | 17,670 |
| PTS_NQ_TFM_001_60 | 60 | 3 | 3 | 3 | 0 | 0 | 0.3 | -3,012 | -3,150 | -138 | -3,012 | -3,150 | -3,000 | -3,044 |
| PTS_NQ_TFM_002_15 | 15 | 9 | 9 | 8 | 1 | 1 | 0.7 | 964 | 7,681 | 6,717 | 3,968 | 3,705 | 4,000 | 3,988 |
| PTS_NQ_TFM_003_15 | 15 | 13 | 13 | 12 | 1 | 1 | 0.4 | 9,448 | 8,930 | -518 | 4,952 | 4,458 | 5,000 | 4,882 |
| PTS_NQ_TFM_005_15 | 15 | 11 | 11 | 10 | 1 | 1 | 0.4 | -23,044 | -16,581 | 6,463 | -19,040 | -19,546 | -19,000 | -19,192 |
| PTS_NQ_TFM_006_30 | 30 | 8 | 8 | 8 | 0 | 0 | 0.4 | -4,032 | -4,371 | -339 | -4,032 | -4,371 | -4,000 | -4,087 |
| PTS_NQ_TFM_007_30 | 30 | 4 | 4 | 4 | 0 | 0 | 0.6 | -8,197 | -8,208 | -11 | -8,197 | -8,208 | -8,181 | -8,066 |
| PTS_NQ_TFM_010_60 | 60 | 6 | 7 | 6 | 0 | 1 | 0.3 | -7,524 | -2,812 | 4,712 | -7,524 | -7,784 | -7,500 | -7,572 |
| PTS_NQ_TFM_011_60 | 60 | 6 | 7 | 5 | 1 | 2 | 0.0 | -7,524 | 1,486 | 9,010 | -6,270 | -6,445 | -6,250 | -6,268 |
| PTS_NQ_TFM_012_1440 | 1440 | 1 | 1 | 1 | 0 | 0 | 0.7 | -1,004 | -1,035 | -31 | -1,004 | -1,035 | -1,000 | -1,000 |
| PTS_NQ_TFM_013_1440 | 1440 | 3 | 3 | 3 | 0 | 0 | 0.2 | 16,537 | 16,572 | 35 | 16,537 | 16,572 | 16,549 | 16,678 |
| PTS_NQ_TFM_015_240 | 240 | 4 | 5 | 4 | 0 | 1 | 0.4 | 8,484 | 18,286 | 9,802 | 8,484 | 8,313 | 8,500 | 8,455 |
| PTS_NQ_TFU_001_15 | 15 | 7 | 7 | 6 | 1 | 1 | 0.0 | 7,972 | 7,660 | -312 | 2,976 | 2,682 | 3,000 | 2,894 |
| PTS_NQ_TFU_002_15 | 15 | 11 | 13 | 10 | 1 | 3 | 0.0 | -8,027 | -48 | 7,979 | -6,773 | -7,154 | -6,733 | -6,800 |
| PTS_NQ_TFU_003_15 | 15 | 16 | 17 | 14 | 2 | 3 | 0.3 | 11,318 | 14,337 | 3,019 | 8,789 | 9,307 | 8,845 | 9,802 |
| PTS_NQ_TFU_004_60 | 60 | 8 | 10 | 8 | 0 | 2 | 0.1 | 4,718 | 13,610 | 8,892 | 4,718 | 4,429 | 4,750 | 4,712 |
| PTS_NQ_TFU_005_60 | 60 | 12 | 11 | 9 | 3 | 2 | 0.3 | 13,019 | 5,231 | -7,788 | -1,969 | -4,703 | -1,933 | -4,384 |
| PTS_NQ_TFU_006_1440 | 1440 | 6 | 6 | 6 | 0 | 0 | 0.5 | 9,976 | 9,674 | -302 | 9,976 | 9,674 | 10,000 | 9,886 |
| PTS_NQ_TFU_007_1440 | 1440 | 4 | 4 | 4 | 0 | 0 | 0.1 | 2,984 | 3,009 | 25 | 2,984 | 3,009 | 3,000 | 3,150 |
| PTS_NQ_TFU_008_240 | 240 | 12 | 12 | 12 | 0 | 0 | 0.2 | -1,048 | -1,735 | -687 | -1,048 | -1,735 | -1,000 | -1,310 |
| PTS_NQ_VBO_002_240 | 240 | 0 | 10 | 0 | 0 | 10 | - | 0 | 8,894 | 8,894 | 0 | 0 | 0 | 0 |
| PTS_SB_TFM_001_240 | 240 | 7 | 3 | 3 | 4 | 0 | 0.4 | -1,428 | 422 | 1,850 | 380 | 422 | 392 | 426 |
| PTS_YM_BIA_001_240 | 240 | 28 | 28 | 28 | 0 | 0 | 0.0 | -5,387 | -5,898 | -511 | -5,387 | -5,898 | -5,275 | -5,444 |
| PTS_YM_SBO_001_240 | 240 | 6 | 5 | 5 | 1 | 0 | 0.2 | -1,524 | -1,363 | 162 | -1,270 | -1,363 | -1,250 | -1,282 |
| PTS_YM_SBO_002_240 | 240 | 5 | 5 | 5 | 0 | 0 | 0.1 | 5,230 | 5,163 | -67 | 5,230 | 5,163 | 5,250 | 5,244 |
| PTS_YM_TFM_001_240 | 240 | 1 | 1 | 1 | 0 | 0 | 0.3 | -2,504 | -2,525 | -21 | -2,504 | -2,525 | -2,500 | -2,509 |
| PTS_YM_TFM_002_240 | 240 | 4 | 4 | 4 | 0 | 0 | 0.7 | 484 | 433 | -51 | 484 | 433 | 500 | 498 |
| PTS_YM_TFM_003_240 | 240 | 17 | 12 | 11 | 6 | 1 | 0.5 | -2,417 | -4,752 | -2,335 | -6,238 | -3,455 | -6,194 | -3,277 |
| PTS_YM_TFU_001_60 | 60 | 12 | 7 | 6 | 6 | 1 | 0.3 | 2,017 | 1,873 | -143 | -3,120 | -2,111 | -3,096 | -2,014 |
| PTS_YM_TFU_002_60 | 60 | 9 | 8 | 8 | 1 | 0 | 0.2 | -9,036 | -8,338 | 698 | -8,032 | -8,338 | -8,000 | -8,208 |
| PTS_YM_TFU_003_60 | 60 | 16 | 16 | 16 | 0 | 0 | 0.2 | -4,064 | -4,376 | -312 | -4,064 | -4,376 | -4,000 | -4,117 |
| **TOTALE** | | 764 | 786 | 655 | 109 | 131 | | 11,230 | 23,632 | 12,402 | -16,041 | -47,683 | -13,421 | -29,977 |

Trade abbinati: 655. Scarto di ingresso: ≤1 min 572, ≤5 min 624, ≤15 min 643, oltre 12.
Prezzo di ingresso cBot peggiore dell'interno (punti, positivo = cBot paga di più): long mediana -0.55 media 0.1937 (n=476); short mediana -0.118 media 0.8783 (n=179).

### 1b. Perché un trade interno non esiste sul cBot

| causa | trade | netto/ctr interno di quei trade |
|---|---:|---:|
| nessun evento nel log (livello non toccato o intent non consegnato) | 61 | -22,113 |
| cBot già in posizione (nessuna inversione) | 48 | 49,383 |

Uscite per segnale opposto nell'interno (inversione che il cBot non fa): 26 trade chiusi così, netto/ctr -4,202.

Trade solo cBot: 131, di cui 14 mentre l'interno era già in posizione sulla stessa strategia; netto/ctr dei solo-cBot 71,315.

## 2. Uscite

### 2a. Motivo di uscita dichiarato in trades.json

| interno | n | | cBot | n |
|---|---:|---|---|---:|
| StopLoss | 441 | | LocalExit:StopLoss | 582 |
| TakeProfit | 76 | | LocalExit:TakeProfit | 90 |
| TrailingStop | 69 | | LocalExit:Closed | 87 |
| MaxBars | 54 | | BrokerExit:TakeProfit | 16 |
| BreakEven | 53 | | BrokerExit:StopLoss | 11 |
| TimeExit | 45 | |  |  |
| OppositeSignal | 26 | |  |  |

### 2b. Esito reale dal log del cBot (riga `Chiuso`)

| esito log | n | netto/ctr | di cui trades.json=Closed | StopLoss | TakeProfit |
|---|---:|---:|---:|---:|---:|
| (senza riga Chiuso) | 786 | 23,632 | 87 | 593 | 106 |

### 2c. Trade abbinati: motivo interno × esito cBot

| interno \ cBot | BrokerExit:StopLoss | BrokerExit:TakeProfit | LocalExit:Closed | LocalExit:StopLoss | LocalExit:TakeProfit | tot |
|---|---:|---:|---:|---:|---:|---:|
| BreakEven | 0 | 0 | 0 | 50 | 0 | 50 |
| MaxBars | 3 | 9 | 33 | 1 | 2 | 48 |
| OppositeSignal | 0 | 0 | 3 | 13 | 4 | 20 |
| StopLoss | 3 | 1 | 1 | 369 | 0 | 374 |
| TakeProfit | 0 | 0 | 0 | 1 | 60 | 61 |
| TimeExit | 0 | 2 | 36 | 0 | 2 | 40 |
| TrailingStop | 0 | 0 | 0 | 62 | 0 | 62 |

Impatto sui trade abbinati (netto/ctr cBot − interno) per coppia di esiti, prime 12 per valore assoluto:

| interno | cBot | n | Δ netto/ctr totale | Δ medio | Δ uscita mediano (min) |
|---|---|---:|---:|---:|---:|
| OppositeSignal | LocalExit:StopLoss | 13 | -27,697 | -2,131 | 124 |
| StopLoss | LocalExit:StopLoss | 369 | -21,045 | -57 | 1 |
| OppositeSignal | LocalExit:TakeProfit | 4 | 11,788 | 2,947 | 2381 |
| OppositeSignal | LocalExit:Closed | 3 | 7,645 | 2,548 | 863 |
| MaxBars | LocalExit:TakeProfit | 2 | 7,546 | 3,773 | -142 |
| TakeProfit | LocalExit:StopLoss | 1 | -7,522 | -7,522 | -9004 |
| StopLoss | LocalExit:Closed | 1 | -4,041 | -4,041 | 758 |
| MaxBars | LocalExit:StopLoss | 1 | -3,822 | -3,822 | 101 |
| TimeExit | LocalExit:TakeProfit | 2 | 3,274 | 1,637 | -4699 |
| StopLoss | BrokerExit:StopLoss | 3 | -3,113 | -1,038 | 2332 |
| TakeProfit | LocalExit:TakeProfit | 60 | 2,624 | 44 | 0 |
| TrailingStop | LocalExit:StopLoss | 62 | 2,031 | 33 | 1 |

### 2d. Stop loss: perdita realizzata contro distanza dichiarata (per contratto, lordo)

Solo trade usciti per stop (interno `StopLoss`; cBot esito `StopLoss` senza trailing). Rapporto = perdita lorda / stop dichiarato (allargato dove previsto). >1 = stop eseguito oltre il livello (gap, spread sugli short).

| simbolo | int n | int rapporto mediano | int oltre 1,1 | cBot n | cBot rapporto mediano | cBot oltre 1,1 | cBot short mediano | cBot long mediano |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| BP | 1 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| BTC | 93 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| CC | 1 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| CL | 6 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| ES | 20 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| FDAX | 22 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| GC | 68 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| NG | 33 | 1.00 | 2 | 0 | NaN | 0 | NaN | NaN |
| NQ | 133 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| SB | 0 | NaN | 0 | 0 | NaN | 0 | NaN | NaN |
| YM | 56 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |

### 2f. Trade abbinati: quanto distano le uscite

| |Δ uscita| | n | Σ Δ lordo/ctr (cBot − int) | Σ Δ netto/ctr | Δ netto medio |
|---|---:|---:|---:|---:|
| ≤ 1 min | 459 | -8,676 | -19,717 | -43 |
| 1–5 min | 91 | 1,046 | -1,182 | -13 |
| 5–60 min | 44 | 399 | -388 | -9 |
| 1–24 h | 45 | -9,565 | -10,350 | -230 |
| > 24 h | 16 | 239 | -4 | 0 |

Nota: per le chiusure che il cBot scopre in ritardo (`BrokerExit`), l'esito nel log è dedotto dal segno del netto (`DeduceCloseReason`), quindi `MaxBars → TakeProfit` con Δ uscita 0 è la stessa chiusura etichettata in due modi.

### 2g. Riempimenti dell'interno su un minuto senza barra nel feed

Ingresso/uscita a un istante in cui `@SYM_1.json` non ha una barra: il prezzo viene dal mark-to-market, non dal mercato. Uscite tecniche (TimeExit, MaxBars, SessionFlat, WeekEnd, EndOfRun) escluse dal conteggio delle uscite.

| strategia | ingressi senza barra / tot | netto/ctr di quei trade | uscite SL/TP/trailing/BE senza barra | esempio |
|---|---:|---:|---:|---|
| **TOTALE** | 0 | 0 | | |

Per confronto, ingressi cBot il cui minuto non ha una barra nel feed raccolto: 2 su 786 (il cBot esegue sui tick di cTrader, il feed è la raccolta a barre dello stesso broker).

Bracket riancorato al fill (ingresso slittato rispetto al livello): 0 trade; slippage mediano NaN punti, costo totale per contratto 0.

## 3. Finestra operativa (TradingWindow) e giorno saltato

Per ogni trade si ricava la barra di segnale (la barra della strategia che precede quella del fill) e si etichetta come fa il motore (`BarLabelTime` sull'orologio della finestra). Fuori finestra = un ingresso che la strategia non avrebbe dovuto emettere.

| strategia | finestra | fuso | int fuori/tot | cBot fuori/tot | int giorno saltato | cBot giorno saltato | fill fuori griglia int/cBot |
|---|---|---|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 20:00-19:00 | Research | 0/3 | 0/1 | 0 | 0 | 0/0 |
| PTS_BP_TFM_002_15 | 23:00-22:00 | Research | 0/1 | 0/1 | 0 | 0 | 0/0 |
| PTS_BTC_BIA_001_60 | 00:00-24:00 | Research | 0/73 | 0/72 | 0 | 0 | 0/1 |
| PTS_BTC_PCH_001_240 | 06:00-24:00 | Research | 0/49 | 0/48 | 0 | 0 | 0/0 |
| PTS_BTC_TFU_001_60 | 04:00-03:00 | Research | 0/5 | 0/5 | 0 | 0 | 0/0 |
| PTS_BTC_TFU_002_60 | 04:00-03:00 | Research | 0/11 | 0/11 | 0 | 1 | 0/1 |
| PTS_CC_SBO_001_60 | 11:00-19:00 | Research | 0/3 | 0/2 | 0 | 0 | 0/0 |
| PTS_CL_MAC_001_30 | 00:00-24:00 | Research | 0/8 | 0/7 | 0 | 0 | 0/0 |
| PTS_ES_BSW_001_60 | 00:00-24:00 | Research | 0/5 | 0/4 | 0 | 0 | 0/0 |
| PTS_ES_BSW_002_15 | 00:00-24:00 | Research | 0/3 | 0/3 | 0 | 0 | 0/0 |
| PTS_ES_BSW_003_15 | 00:00-24:00 | Research | 0/9 | 0/8 | 0 | 0 | 0/0 |
| PTS_ES_PCH_001_60 | 03:00-02:00 | Research | 0/6 | 0/5 | 0 | 0 | 0/0 |
| PTS_ES_PCH_002_60 | 08:00-10:00 | Research | 0/7 | 0/7 | 0 | 0 | 0/0 |
| PTS_ES_PCH_003_1440 | 00:00-24:00 | Research | 0/2 | 0/2 | 0 | 0 | 0/0 |
| PTS_ES_PCH_004_240 | 00:00-24:00 | Research | 0/23 | 0/25 | 0 | 0 | 0/0 |
| PTS_ES_SBO_001_15 | 03:00-02:00 | Research | 0/1 | 0/1 | 0 | 0 | 0/0 |
| PTS_ES_SBO_002_240 | 14:00-09:00 | Research | 0/3 | 0/1 | 0 | 0 | 0/0 |
| PTS_ES_SBO_003_240 | 14:00-09:00 | Research | 0/5 | 0/3 | 0 | 0 | 0/0 |
| PTS_ES_TFM_001_1440 | 00:00-24:00 | Research | 0/2 | 0/2 | 0 | 0 | 0/0 |
| PTS_ES_TFM_002_1440 | 00:00-24:00 | Research | 0/5 | 0/4 | 0 | 0 | 0/0 |
| PTS_ES_TFU_001_1440 | 00:00-24:00 | Research | 0/12 | 0/8 | 0 | 0 | 0/0 |
| PTS_FDAX_MAC_001_240 | 00:00-24:00 | Research | 0/4 | 0/4 | 0 | 0 | 0/0 |
| PTS_FDAX_SBO_002_1440 | 00:00-24:00 | Research | 0/2 | 0/2 | 0 | 0 | 0/0 |
| PTS_FDAX_TFU_001_1440 | 00:00-24:00 | Research | 0/9 | 0/9 | 0 | 0 | 0/0 |
| PTS_FDAX_TFU_002_1440 | 00:00-24:00 | Research | 0/11 | 0/11 | 0 | 0 | 0/0 |
| PTS_GC_PCH_001_60 | 06:00-05:00 | Research | 0/3 | 0/4 | 0 | 0 | 0/0 |
| PTS_GC_PCH_004_240 | 00:00-12:00 | Research | 0/52 | 0/54 | 0 | 0 | 0/0 |
| PTS_GC_PCH_005_240 | 00:00-09:00 | Research | 0/25 | 0/28 | 0 | 0 | 0/0 |
| PTS_GC_RHL_001_60 | 13:00-12:00 | Research | 0/6 | 0/6 | 0 | 0 | 0/0 |
| PTS_GC_RHL_002_60 | 13:00-12:00 | Research | 0/7 | 0/7 | 0 | 0 | 0/0 |
| PTS_GC_TFU_001_30 | 16:00-08:00 | Research | 0/4 | 0/4 | 0 | 0 | 0/0 |
| PTS_NG_BSW_001_30 | 00:00-24:00 | Research | 0/4 | 0/4 | 0 | 0 | 0/0 |
| PTS_NG_TFM_001_240 | 10:00-24:00 | Research | 0/3 | 0/5 | 0 | 0 | 0/0 |
| PTS_NG_TFM_002_240 | 00:00-17:00 | Research | 0/5 | 0/10 | 0 | 0 | 0/0 |
| PTS_NG_TFM_003_30 | 22:00-17:00 | Research | 0/4 | 0/13 | 0 | 0 | 0/0 |
| PTS_NG_TFM_004_60 | 02:00-14:00 | Research | 0/0 | 0/2 | 0 | 0 | 0/0 |
| PTS_NG_TFM_005_30 | 03:00-24:00 | Research | 0/8 | 0/12 | 0 | 0 | 0/0 |
| PTS_NG_TFM_006_60 | 00:00-22:00 | Research | 0/4 | 0/9 | 0 | 0 | 0/0 |
| PTS_NG_TFU_001_240 | 14:00-24:00 | Research | 0/2 | 0/5 | 0 | 0 | 0/0 |
| PTS_NG_TFU_002_240 | 14:00-24:00 | Research | 0/4 | 0/7 | 0 | 0 | 0/0 |
| PTS_NG_TFU_003_60 | 08:00-23:00 | Research | 0/5 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_003_30 | 14:00-04:00 | Research | 0/21 | 0/20 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_004_30 | 11:00-10:00 | Research | 0/11 | 0/11 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_007_240 | 10:00-00:00 | Research | 0/4 | 0/4 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_008_240 | 06:00-24:00 | Research | 0/11 | 0/10 | 0 | 0 | 0/0 |
| PTS_NQ_RBM_001_15 | 07:00-06:00 | Research | 0/38 | 0/32 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_001_15 | 05:00-04:00 | Research | 0/6 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_002_15 | 10:00-05:00 | Research | 0/6 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_003_15 | 13:00-06:00 | Research | 0/8 | 0/9 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_004_60 | 22:00-21:00 | Research | 0/10 | 0/11 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_005_1440 | 00:00-24:00 | Research | 0/2 | 0/2 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_001_60 | 16:00-03:00 | Research | 0/3 | 0/3 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_002_15 | 09:00-05:00 | Research | 0/9 | 0/9 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_003_15 | 13:00-05:00 | Research | 0/13 | 0/13 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_005_15 | 18:00-17:00 | Research | 0/11 | 0/11 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_006_30 | 14:00-04:00 | Research | 0/8 | 0/8 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_007_30 | 02:00-01:00 | Research | 0/4 | 0/4 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_010_60 | 00:00-17:00 | Research | 0/6 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_011_60 | 21:00-14:00 | Research | 0/6 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_012_1440 | 00:00-24:00 | Research | 0/1 | 0/1 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_013_1440 | 00:00-24:00 | Research | 0/3 | 0/3 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_015_240 | 18:00-05:00 | Research | 0/4 | 0/5 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_001_15 | 17:00-10:00 | Research | 0/7 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_002_15 | 17:00-07:00 | Research | 0/11 | 0/13 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_003_15 | 17:00-03:00 | Research | 0/16 | 0/17 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_004_60 | 17:00-03:00 | Research | 0/8 | 0/10 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_005_60 | 16:00-04:00 | Research | 0/12 | 0/11 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | Research | 0/6 | 0/6 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_007_1440 | 00:00-24:00 | Research | 0/4 | 0/4 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_008_240 | 06:00-24:00 | Research | 0/12 | 0/12 | 0 | 0 | 0/0 |
| PTS_NQ_VBO_002_240 | 00:00-17:00 | Research | 0/0 | 0/10 | 0 | 0 | 0/0 |
| PTS_SB_TFM_001_240 | 13:00-24:00 | Research | 0/7 | 0/3 | 0 | 0 | 0/0 |
| PTS_YM_BIA_001_240 | 00:00-24:00 | Research | 0/28 | 0/28 | 0 | 0 | 0/0 |
| PTS_YM_SBO_001_240 | 14:00-05:00 | Research | 0/6 | 0/5 | 0 | 0 | 0/0 |
| PTS_YM_SBO_002_240 | 14:00-05:00 | Research | 0/5 | 0/5 | 0 | 0 | 0/0 |
| PTS_YM_TFM_001_240 | 06:00-09:00 | Research | 0/1 | 0/1 | 0 | 0 | 0/0 |
| PTS_YM_TFM_002_240 | 06:00-24:00 | Research | 0/4 | 0/4 | 0 | 0 | 0/0 |
| PTS_YM_TFM_003_240 | 00:00-24:00 | Research | 0/17 | 0/12 | 0 | 0 | 0/0 |
| PTS_YM_TFU_001_60 | 19:00-17:00 | Research | 0/12 | 0/7 | 0 | 0 | 0/0 |
| PTS_YM_TFU_002_60 | 12:00-03:00 | Research | 0/9 | 0/8 | 0 | 0 | 0/0 |
| PTS_YM_TFU_003_60 | 01:00-21:00 | Research | 0/16 | 0/16 | 0 | 0 | 0/0 |
| **TOTALE** | | | 0 | 0 | 0 | 1 | 0/2 |

## 4. Sessione: ancoraggio e un fill per sessione per lato

| strategia | sessione (ancoraggio) | max ingressi/sessione | int sessioni con >max per lato | cBot sessioni con >max per lato | int ingressi weekend UTC | cBot ingressi weekend UTC |
|---|---|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_BP_TFM_002_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_BTC_BIA_001_60 | 00:00-24:00 | 0 | 0 | 0 | 15 | 15 |
| PTS_BTC_PCH_001_240 | 00:00-24:00 | 1 | 0 | 0 | 23 | 22 |
| PTS_BTC_TFU_001_60 | 00:00-24:00 | 0 | 0 | 0 | 2 | 2 |
| PTS_BTC_TFU_002_60 | 00:00-24:00 | 0 | 0 | 0 | 5 | 5 |
| PTS_CC_SBO_001_60 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_CL_MAC_001_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_001_60 | 00:00-24:00 | 0 | 0 | 0 | 3 | 3 |
| PTS_ES_BSW_002_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_003_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_002_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_003_1440 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_004_240 | 00:00-24:00 | 1 | 0 | 1 | 0 | 1 |
| PTS_ES_SBO_001_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_SBO_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_SBO_003_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_TFM_001_1440 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_TFM_002_1440 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_TFU_001_1440 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_FDAX_MAC_001_240 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_FDAX_SBO_002_1440 | 01:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_FDAX_TFU_001_1440 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_FDAX_TFU_002_1440 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_GC_PCH_001_60 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_GC_PCH_004_240 | 00:00-24:00 | 1 | 2 | 2 | 4 | 5 |
| PTS_GC_PCH_005_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 3 |
| PTS_GC_RHL_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_GC_RHL_002_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_GC_TFU_001_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_BSW_001_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_NG_TFM_003_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_004_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_005_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_006_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFU_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFU_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFU_003_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_PCH_003_30 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_PCH_004_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_NQ_PCH_007_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_PCH_008_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_RBM_001_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_001_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_SBO_002_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_SBO_003_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_SBO_004_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_SBO_005_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_002_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_003_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_005_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_006_30 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_007_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_010_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_011_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_012_1440 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_013_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_015_240 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFU_001_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFU_002_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFU_003_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_004_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFU_005_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFU_007_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFU_008_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_VBO_002_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_SB_TFM_001_240 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_BIA_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_SBO_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_SBO_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_003_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFU_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFU_002_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFU_003_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| **TOTALE** | | | 2 | 3 | | |

Nel log del cBot il server ha rifiutato template per `limite di ingressi per sessione raggiunto` (conteggio righe, per strategia e lato, prime 15):


## 5. Spread

Spread dell'interno, dal summary: `FTMOPLATFORM · p50Spread · per ora UTC · FTMOPLATFORM_spread-by-symbol_20260801-20260901.csv (2026-09-06)`. Il cBot lo paga sui tick: costo per trade ≈ spread al fill × valore punto × contratti (una volta per round trip). Lo stop è quello eseguito (allargato ×1 dove previsto).

| strategia | simbolo | fill con spread | spread medio (punti) | stop (punti) | spread/stop | costo spread tot (per contratto, $) | netto/ctr cBot | netto/ctr senza spread | costo/trade ($/ctr) |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | BP | 0/1 | 0 | 0.01 | 0.0% | 0 | 1,885 | 1,885 | 0 |
| PTS_BP_TFM_002_15 | BP | 0/1 | 0 | 0.02 | 0.0% | 0 | 454 | 454 | 0 |
| PTS_BTC_BIA_001_60 | BTC | 0/72 | 0 | 50 | 0.0% | 0 | -1,384 | -1,384 | 0 |
| PTS_BTC_PCH_001_240 | BTC | 0/48 | 0 | 350 | 0.0% | 0 | -4,946 | -4,946 | 0 |
| PTS_BTC_TFU_001_60 | BTC | 0/5 | 0 | 50 | 0.0% | 0 | -1,667 | -1,667 | 0 |
| PTS_BTC_TFU_002_60 | BTC | 0/11 | 0 | 50 | 0.0% | 0 | -3,541 | -3,541 | 0 |
| PTS_CC_SBO_001_60 | CC | 0/2 | 0 | 175 | 0.0% | 0 | 3,568 | 3,568 | 0 |
| PTS_CL_MAC_001_30 | CL | 0/7 | 0 | 1 | 0.0% | 0 | -3,025 | -3,025 | 0 |
| PTS_ES_BSW_001_60 | ES | 0/4 | 0 | 0 | 0.0% | 0 | 8,531 | 8,531 | 0 |
| PTS_ES_BSW_002_15 | ES | 0/3 | 0 | 0 | 0.0% | 0 | -1,614 | -1,614 | 0 |
| PTS_ES_BSW_003_15 | ES | 0/8 | 0 | 0 | 0.0% | 0 | 9,593 | 9,593 | 0 |
| PTS_ES_PCH_001_60 | ES | 0/5 | 0 | 80 | 0.0% | 0 | 627 | 627 | 0 |
| PTS_ES_PCH_002_60 | ES | 0/7 | 0 | 80 | 0.0% | 0 | -2,891 | -2,891 | 0 |
| PTS_ES_PCH_003_1440 | ES | 0/2 | 0 | 20 | 0.0% | 0 | -2,058 | -2,058 | 0 |
| PTS_ES_PCH_004_240 | ES | 0/25 | 0 | 100 | 0.0% | 0 | -10,960 | -10,960 | 0 |
| PTS_ES_SBO_001_15 | ES | 0/1 | 0 | 80 | 0.0% | 0 | -4,026 | -4,026 | 0 |
| PTS_ES_SBO_002_240 | ES | 0/1 | 0 | 80 | 0.0% | 0 | -4,026 | -4,026 | 0 |
| PTS_ES_SBO_003_240 | ES | 0/3 | 0 | 80 | 0.0% | 0 | -4,629 | -4,629 | 0 |
| PTS_ES_TFM_001_1440 | ES | 0/2 | 0 | 20 | 0.0% | 0 | 4,954 | 4,954 | 0 |
| PTS_ES_TFM_002_1440 | ES | 0/4 | 0 | 20 | 0.0% | 0 | 5,920 | 5,920 | 0 |
| PTS_ES_TFU_001_1440 | ES | 0/8 | 0 | 40 | 0.0% | 0 | -8,379 | -8,379 | 0 |
| PTS_FDAX_MAC_001_240 | FDAX | 0/4 | 0 | 120 | 0.0% | 0 | -13,195 | -13,195 | 0 |
| PTS_FDAX_SBO_002_1440 | FDAX | 0/2 | 0 | 40 | 0.0% | 0 | -2,528 | -2,528 | 0 |
| PTS_FDAX_TFU_001_1440 | FDAX | 0/9 | 0 | 40 | 0.0% | 0 | 5,285 | 5,285 | 0 |
| PTS_FDAX_TFU_002_1440 | FDAX | 0/11 | 0 | 40 | 0.0% | 0 | -5,358 | -5,358 | 0 |
| PTS_GC_PCH_001_60 | GC | 0/4 | 0 | 22.5 | 0.0% | 0 | -3,148 | -3,148 | 0 |
| PTS_GC_PCH_004_240 | GC | 0/54 | 0 | 5 | 0.0% | 0 | -10,141 | -10,141 | 0 |
| PTS_GC_PCH_005_240 | GC | 0/28 | 0 | 25 | 0.0% | 0 | 914 | 914 | 0 |
| PTS_GC_RHL_001_60 | GC | 0/6 | 0 | 20 | 0.0% | 0 | 5,928 | 5,928 | 0 |
| PTS_GC_RHL_002_60 | GC | 0/7 | 0 | 20 | 0.0% | 0 | 1,876 | 1,876 | 0 |
| PTS_GC_TFU_001_30 | GC | 0/4 | 0 | 17.5 | 0.0% | 0 | -7,433 | -7,433 | 0 |
| PTS_NG_BSW_001_30 | NG | 0/4 | 0 | 0 | 0.0% | 0 | -937 | -937 | 0 |
| PTS_NG_TFM_001_240 | NG | 0/5 | 0 | 0.18 | 0.0% | 0 | -7,309 | -7,309 | 0 |
| PTS_NG_TFM_002_240 | NG | 0/10 | 0 | 0.03 | 0.0% | 0 | -3,298 | -3,298 | 0 |
| PTS_NG_TFM_003_30 | NG | 0/13 | 0 | 0.08 | 0.0% | 0 | -2,333 | -2,333 | 0 |
| PTS_NG_TFM_004_60 | NG | 0/2 | 0 | 0.15 | 0.0% | 0 | 26 | 26 | 0 |
| PTS_NG_TFM_005_30 | NG | 0/12 | 0 | 0.1 | 0.0% | 0 | -8,372 | -8,372 | 0 |
| PTS_NG_TFM_006_60 | NG | 0/9 | 0 | 0.13 | 0.0% | 0 | -11,626 | -11,626 | 0 |
| PTS_NG_TFU_001_240 | NG | 0/5 | 0 | 0.1 | 0.0% | 0 | -5,039 | -5,039 | 0 |
| PTS_NG_TFU_002_240 | NG | 0/7 | 0 | 0.1 | 0.0% | 0 | -7,063 | -7,063 | 0 |
| PTS_NG_TFU_003_60 | NG | 0/7 | 0 | 0.15 | 0.0% | 0 | -2,533 | -2,533 | 0 |
| PTS_NQ_PCH_003_30 | NQ | 0/20 | 0 | 125 | 0.0% | 0 | 20,949 | 20,949 | 0 |
| PTS_NQ_PCH_004_30 | NQ | 0/11 | 0 | 112.5 | 0.0% | 0 | 18,863 | 18,863 | 0 |
| PTS_NQ_PCH_007_240 | NQ | 0/4 | 0 | 150 | 0.0% | 0 | -1,630 | -1,630 | 0 |
| PTS_NQ_PCH_008_240 | NQ | 0/10 | 0 | 200 | 0.0% | 0 | 10,878 | 10,878 | 0 |
| PTS_NQ_RBM_001_15 | NQ | 0/32 | 0 | 100 | 0.0% | 0 | -6,235 | -6,235 | 0 |
| PTS_NQ_SBO_001_15 | NQ | 0/7 | 0 | 250 | 0.0% | 0 | 12,790 | 12,790 | 0 |
| PTS_NQ_SBO_002_15 | NQ | 0/7 | 0 | 200 | 0.0% | 0 | -2,360 | -2,360 | 0 |
| PTS_NQ_SBO_003_15 | NQ | 0/9 | 0 | 25 | 0.0% | 0 | -5,086 | -5,086 | 0 |
| PTS_NQ_SBO_004_60 | NQ | 0/11 | 0 | 25 | 0.0% | 0 | -6,311 | -6,311 | 0 |
| PTS_NQ_SBO_005_1440 | NQ | 0/2 | 0 | 50 | 0.0% | 0 | 17,600 | 17,600 | 0 |
| PTS_NQ_TFM_001_60 | NQ | 0/3 | 0 | 50 | 0.0% | 0 | -3,150 | -3,150 | 0 |
| PTS_NQ_TFM_002_15 | NQ | 0/9 | 0 | 150 | 0.0% | 0 | 7,681 | 7,681 | 0 |
| PTS_NQ_TFM_003_15 | NQ | 0/13 | 0 | 125 | 0.0% | 0 | 8,930 | 8,930 | 0 |
| PTS_NQ_TFM_005_15 | NQ | 0/11 | 0 | 200 | 0.0% | 0 | -16,581 | -16,581 | 0 |
| PTS_NQ_TFM_006_30 | NQ | 0/8 | 0 | 25 | 0.0% | 0 | -4,371 | -4,371 | 0 |
| PTS_NQ_TFM_007_30 | NQ | 0/4 | 0 | 250 | 0.0% | 0 | -8,208 | -8,208 | 0 |
| PTS_NQ_TFM_010_60 | NQ | 0/7 | 0 | 125 | 0.0% | 0 | -2,812 | -2,812 | 0 |
| PTS_NQ_TFM_011_60 | NQ | 0/7 | 0 | 62.5 | 0.0% | 0 | 1,486 | 1,486 | 0 |
| PTS_NQ_TFM_012_1440 | NQ | 0/1 | 0 | 50 | 0.0% | 0 | -1,035 | -1,035 | 0 |
| PTS_NQ_TFM_013_1440 | NQ | 0/3 | 0 | 50 | 0.0% | 0 | 16,572 | 16,572 | 0 |
| PTS_NQ_TFM_015_240 | NQ | 0/5 | 0 | 25 | 0.0% | 0 | 18,286 | 18,286 | 0 |
| PTS_NQ_TFU_001_15 | NQ | 0/7 | 0 | 200 | 0.0% | 0 | 7,660 | 7,660 | 0 |
| PTS_NQ_TFU_002_15 | NQ | 0/13 | 0 | 62.5 | 0.0% | 0 | -48 | -48 | 0 |
| PTS_NQ_TFU_003_15 | NQ | 0/17 | 0 | 87.5 | 0.0% | 0 | 14,337 | 14,337 | 0 |
| PTS_NQ_TFU_004_60 | NQ | 0/10 | 0 | 37.5 | 0.0% | 0 | 13,610 | 13,610 | 0 |
| PTS_NQ_TFU_005_60 | NQ | 0/11 | 0 | 200 | 0.0% | 0 | 5,231 | 5,231 | 0 |
| PTS_NQ_TFU_006_1440 | NQ | 0/6 | 0 | 50 | 0.0% | 0 | 9,674 | 9,674 | 0 |
| PTS_NQ_TFU_007_1440 | NQ | 0/4 | 0 | 50 | 0.0% | 0 | 3,009 | 3,009 | 0 |
| PTS_NQ_TFU_008_240 | NQ | 0/12 | 0 | 25 | 0.0% | 0 | -1,735 | -1,735 | 0 |
| PTS_NQ_VBO_002_240 | NQ | 0/10 | 0 | 112.5 | 0.0% | 0 | 8,894 | 8,894 | 0 |
| PTS_SB_TFM_001_240 | SB | 0/3 | 0 | 2.01 | 0.0% | 0 | 422 | 422 | 0 |
| PTS_YM_BIA_001_240 | YM | 0/28 | 0 | 450 | 0.0% | 0 | -5,898 | -5,898 | 0 |
| PTS_YM_SBO_001_240 | YM | 0/5 | 0 | 50 | 0.0% | 0 | -1,363 | -1,363 | 0 |
| PTS_YM_SBO_002_240 | YM | 0/5 | 0 | 50 | 0.0% | 0 | 5,163 | 5,163 | 0 |
| PTS_YM_TFM_001_240 | YM | 0/1 | 0 | 500 | 0.0% | 0 | -2,525 | -2,525 | 0 |
| PTS_YM_TFM_002_240 | YM | 0/4 | 0 | 800 | 0.0% | 0 | 433 | 433 | 0 |
| PTS_YM_TFM_003_240 | YM | 0/12 | 0 | 250 | 0.0% | 0 | -4,752 | -4,752 | 0 |
| PTS_YM_TFU_001_60 | YM | 0/7 | 0 | 500 | 0.0% | 0 | 1,873 | 1,873 | 0 |
| PTS_YM_TFU_002_60 | YM | 0/8 | 0 | 200 | 0.0% | 0 | -8,338 | -8,338 | 0 |
| PTS_YM_TFU_003_60 | YM | 0/16 | 0 | 50 | 0.0% | 0 | -4,376 | -4,376 | 0 |
| **TOTALE** | | | | | | 0 | 23,632 | 23,632 | |

### 5b. Strategie con stop più stretto dello spread, o quasi

| strategia | spread/stop | stop (punti) | trade cBot | netto/ctr cBot | commento |
|---|---:|---:|---:|---:|---|

Sui trade abbinati: 0 volte il cBot esce per stop dove l'interno no; 374 il contrario.

## 6. Strategie con curva di equity crescente

Metriche su netto per contratto, trade ordinati per uscita. R² = fit lineare della curva cumulata sull'indice dei trade. Crescente = netto > 0, R² ≥ 0,6, chiusura ad almeno il 70% del picco, ≥ 8 trade. Il cBot è calcolato sull'intero run (ingressi 31/08/2025 → 12/11/2025), l'interno sull'intero run (ingressi 31/08/2025 → 31/08/2026).

| strategia | cBot n | cBot netto/ctr | PF | R² | DD max/ctr | fine/picco | **cBot** | int n | int netto/ctr | PF | R² | DD max/ctr | fine/picco | **int** |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| PTS_BP_TFM_001_60 | 1 | 1,885 | 99.00 | 0.00 | 0 | 1.00 | no | 13 | 1,879 | 1.42 | 0.60 | 2,372 | 0.44 | no |
| PTS_BP_TFM_002_15 | 1 | 454 | 99.00 | 0.00 | 0 | 1.00 | no | 5 | 980 | 1.98 | 0.30 | 1,004 | 0.49 | no |
| PTS_BTC_BIA_001_60 | 72 | -1,384 | 0.94 | 0.58 | 18,120 | -0.15 | no | 299 | 15,142 | 1.21 | 0.10 | 24,002 | 0.72 | no |
| PTS_BTC_PCH_001_240 | 48 | -4,946 | 0.85 | 0.03 | 13,661 | -1.56 | no | 244 | 14,900 | 1.11 | 0.16 | 17,375 | 1.00 | no |
| PTS_BTC_TFU_001_60 | 5 | -1,667 | 0.00 | 1.00 | 1,667 | 0.00 | no | 38 | 5,848 | 1.64 | 0.60 | 4,572 | 0.74 | no |
| PTS_BTC_TFU_002_60 | 11 | -3,541 | 0.00 | 1.00 | 3,541 | 0.00 | no | 55 | 11,030 | 1.85 | 0.60 | 7,112 | 0.90 | no |
| PTS_CC_SBO_001_60 | 2 | 3,568 | 3.02 | 1.00 | 1,771 | 1.00 | no | 11 | 16,108 | 2.31 | 0.39 | 8,770 | 0.65 | no |
| PTS_CL_MAC_001_30 | 7 | -3,025 | 0.48 | 0.24 | 4,032 | -3.00 | no | 35 | -1,684 | 0.92 | 0.03 | 9,048 | -1.59 | no |
| PTS_CT_TFU_001_240 | 0 | 0 | 0.00 | 0.00 | 0 | 0.00 | no | 12 | -6,868 | 0.52 | 0.87 | 11,356 | -2.30 | no |
| PTS_ES_BSW_001_60 | 4 | 8,531 | 2.70 | 0.38 | 5,026 | 0.63 | no | 18 | 22,825 | 1.70 | 0.40 | 15,012 | 0.90 | no |
| PTS_ES_BSW_002_15 | 3 | -1,614 | 0.74 | 1.00 | 6,112 | -0.36 | no | 15 | 7,440 | 1.31 | 0.32 | 9,012 | 0.83 | no |
| PTS_ES_BSW_003_15 | 8 | 9,593 | 2.05 | 0.66 | 3,641 | 1.00 | SÌ | 42 | 16,051 | 1.25 | 0.51 | 18,024 | 0.59 | no |
| PTS_ES_PCH_001_60 | 5 | 627 | 1.32 | 0.00 | 1,060 | 0.91 | no | 22 | 6,980 | 1.84 | 0.13 | 4,064 | 0.90 | no |
| PTS_ES_PCH_002_60 | 7 | -2,891 | 0.29 | 0.67 | 2,891 | 0.00 | no | 38 | -2,507 | 0.82 | 0.04 | 6,629 | -1.02 | no |
| PTS_ES_PCH_003_1440 | 2 | -2,058 | 0.00 | 1.00 | 2,058 | 0.00 | no | 12 | -7,549 | 0.11 | 0.88 | 7,549 | 0.00 | no |
| PTS_ES_PCH_004_240 | 25 | -10,960 | 0.00 | 0.50 | 10,960 | 0.00 | no | 86 | -30,344 | 0.33 | 0.86 | 45,256 | -4.08 | no |
| PTS_ES_SBO_001_15 | 1 | -4,026 | 0.00 | 0.00 | 4,026 | 0.00 | no | 7 | 11,485 | 1.96 | 0.48 | 8,008 | 1.00 | no |
| PTS_ES_SBO_002_240 | 1 | -4,026 | 0.00 | 0.00 | 4,026 | 0.00 | no | 15 | 604 | 1.02 | 0.03 | 14,829 | 0.13 | no |
| PTS_ES_SBO_003_240 | 3 | -4,629 | 0.43 | 0.02 | 4,629 | 0.00 | no | 18 | -16,385 | 0.64 | 0.70 | 33,206 | -2.17 | no |
| PTS_ES_TFM_001_1440 | 2 | 4,954 | 5.82 | 1.00 | 1,027 | 1.00 | no | 13 | 948 | 1.09 | 0.25 | 9,036 | 0.19 | no |
| PTS_ES_TFM_002_1440 | 4 | 5,920 | 3.88 | 0.80 | 1,026 | 1.00 | no | 34 | -7,071 | 0.75 | 0.64 | 16,084 | -3.46 | no |
| PTS_ES_TFU_001_1440 | 8 | -8,379 | 0.42 | 0.68 | 14,360 | 0.00 | no | 48 | -48,718 | 0.39 | 0.94 | 53,442 | -10.31 | no |
| PTS_FDAX_MAC_001_240 | 4 | -13,195 | 0.00 | 1.00 | 13,195 | 0.00 | no | 10 | -21,325 | 0.08 | 0.97 | 21,325 | 0.00 | no |
| PTS_FDAX_SBO_002_1440 | 2 | -2,528 | 0.00 | 1.00 | 2,528 | 0.00 | no | 14 | 1,944 | 1.15 | 0.12 | 9,036 | 0.18 | no |
| PTS_FDAX_TFU_001_1440 | 9 | 5,285 | 1.62 | 0.03 | 8,587 | 1.00 | no | 53 | -11,390 | 0.76 | 0.01 | 24,274 | -0.88 | no |
| PTS_FDAX_TFU_002_1440 | 11 | -5,358 | 0.57 | 0.64 | 12,329 | 0.00 | no | 58 | -23,410 | 0.56 | 0.62 | 23,410 | 0.00 | no |
| PTS_GC_PCH_001_60 | 4 | -3,148 | 0.56 | 0.10 | 4,833 | -1.87 | no | 25 | -100 | 1.00 | 0.22 | 17,826 | -0.03 | no |
| PTS_GC_PCH_004_240 | 54 | -10,141 | 0.64 | 0.82 | 17,189 | -4.76 | no | 281 | 61,785 | 1.51 | 0.88 | 11,917 | 0.90 | SÌ |
| PTS_GC_PCH_005_240 | 28 | 914 | 1.03 | 0.09 | 14,388 | 0.63 | no | 90 | 12,182 | 1.10 | 0.04 | 24,997 | 0.81 | no |
| PTS_GC_RHL_001_60 | 6 | 5,928 | 2.40 | 0.24 | 4,150 | 1.00 | no | 34 | -1,601 | 0.96 | 0.00 | 14,714 | -0.12 | no |
| PTS_GC_RHL_002_60 | 7 | 1,876 | 1.45 | 0.00 | 4,089 | 0.31 | no | 33 | 1,371 | 1.04 | 0.05 | 13,009 | 0.15 | no |
| PTS_GC_TFM_001_240 | 0 | 0 | 0.00 | 0.00 | 0 | 0.00 | no | 8 | -15,032 | 0.50 | 0.33 | 17,524 | 0.00 | no |
| PTS_GC_TFU_001_30 | 4 | -7,433 | 0.00 | 1.00 | 7,433 | 0.00 | no | 35 | 3,360 | 1.07 | 0.27 | 18,814 | 0.23 | no |
| PTS_NG_BSW_001_30 | 4 | -937 | 0.52 | 0.01 | 1,944 | 0.00 | no | 16 | -6,404 | 0.34 | 0.74 | 6,848 | 0.00 | no |
| PTS_NG_TFM_001_240 | 5 | -7,309 | 0.00 | 0.99 | 7,309 | 0.00 | no | 34 | -34,486 | 0.31 | 0.91 | 34,618 | 0.00 | no |
| PTS_NG_TFM_002_240 | 10 | -3,298 | 0.58 | 0.67 | 7,317 | -2.15 | no | 32 | -8,128 | 0.00 | 1.00 | 8,128 | 0.00 | no |
| PTS_NG_TFM_003_30 | 13 | -2,333 | 0.72 | 0.00 | 4,541 | 0.00 | no | 18 | -13,572 | 0.00 | 1.00 | 13,572 | 0.00 | no |
| PTS_NG_TFM_004_60 | 2 | 26 | 1.05 | 1.00 | 552 | 1.00 | no | 7 | 582 | 1.15 | 0.10 | 3,646 | 0.14 | no |
| PTS_NG_TFM_005_30 | 12 | -8,372 | 0.26 | 0.76 | 8,372 | 0.00 | no | 43 | -24,722 | 0.38 | 0.77 | 24,722 | 0.00 | no |
| PTS_NG_TFM_006_60 | 9 | -11,626 | 0.00 | 1.00 | 11,626 | 0.00 | no | 37 | -15,218 | 0.58 | 0.31 | 15,218 | 0.00 | no |
| PTS_NG_TFU_001_240 | 5 | -5,039 | 0.00 | 1.00 | 5,039 | 0.00 | no | 16 | -12,274 | 0.18 | 0.91 | 12,274 | 0.00 | no |
| PTS_NG_TFU_002_240 | 7 | -7,063 | 0.00 | 1.00 | 7,063 | 0.00 | no | 23 | -23,112 | 0.00 | 1.00 | 23,112 | 0.00 | no |
| PTS_NG_TFU_003_60 | 7 | -2,533 | 0.66 | 0.09 | 6,027 | 0.00 | no | 21 | -23,594 | 0.16 | 0.97 | 23,594 | 0.00 | no |
| PTS_NQ_PCH_003_30 | 20 | 20,949 | 2.84 | 0.62 | 5,050 | 1.00 | SÌ | 69 | 16,842 | 1.41 | 0.10 | 19,621 | 0.81 | no |
| PTS_NQ_PCH_004_30 | 11 | 18,863 | 53.75 | 0.82 | 169 | 1.00 | SÌ | 50 | 23,183 | 2.58 | 0.47 | 11,229 | 0.92 | no |
| PTS_NQ_PCH_007_240 | 4 | -1,630 | 0.51 | 0.18 | 2,585 | 0.00 | no | 21 | -21,779 | 0.15 | 0.95 | 21,779 | 0.00 | no |
| PTS_NQ_PCH_008_240 | 10 | 10,878 | 3.10 | 0.44 | 3,074 | 1.00 | no | 50 | 16,215 | 1.44 | 0.50 | 17,061 | 0.79 | no |
| PTS_NQ_RBM_001_15 | 32 | -6,235 | 0.83 | 0.37 | 12,038 | -3.11 | no | 163 | 22,515 | 1.14 | 0.54 | 26,251 | 0.75 | no |
| PTS_NQ_SBO_001_15 | 7 | 12,790 | 3.54 | 0.62 | 5,041 | 1.00 | no | 36 | -4,523 | 0.94 | 0.62 | 26,451 | -0.30 | no |
| PTS_NQ_SBO_002_15 | 7 | -2,360 | 0.81 | 0.19 | 12,240 | -0.24 | no | 25 | -2,600 | 0.94 | 0.01 | 19,032 | -0.16 | no |
| PTS_NQ_SBO_003_15 | 9 | -5,086 | 0.00 | 1.00 | 5,086 | 0.00 | no | 38 | 13,169 | 1.71 | 0.35 | 13,104 | 0.70 | no |
| PTS_NQ_SBO_004_60 | 11 | -6,311 | 0.00 | 1.00 | 6,311 | 0.00 | no | 18 | -9,072 | 0.00 | 1.00 | 9,072 | 0.00 | no |
| PTS_NQ_SBO_005_1440 | 2 | 17,600 | 17.84 | 1.00 | 1,045 | 0.94 | no | 12 | 10,832 | 2.08 | 0.89 | 9,036 | 0.55 | no |
| PTS_NQ_TFM_001_60 | 3 | -3,150 | 0.00 | 1.00 | 3,150 | 0.00 | no | 18 | -2,072 | 0.85 | 0.04 | 5,052 | 0.00 | no |
| PTS_NQ_TFM_002_15 | 9 | 7,681 | 1.63 | 0.03 | 12,168 | 0.48 | no | 37 | 1,507 | 1.02 | 0.20 | 26,409 | 0.09 | no |
| PTS_NQ_TFM_003_15 | 13 | 8,930 | 1.50 | 0.50 | 7,616 | 1.00 | no | 42 | 34,832 | 1.63 | 0.60 | 22,536 | 0.93 | no |
| PTS_NQ_TFM_005_15 | 11 | -16,581 | 0.42 | 0.88 | 25,489 | -1.86 | no | 43 | -14,581 | 0.83 | 0.19 | 33,509 | -1.62 | no |
| PTS_NQ_TFM_006_30 | 8 | -4,371 | 0.00 | 1.00 | 4,371 | 0.00 | no | 31 | 17,799 | 2.31 | 0.74 | 4,536 | 0.95 | SÌ |
| PTS_NQ_TFM_007_30 | 4 | -8,208 | 0.28 | 0.63 | 11,334 | 0.00 | no | 21 | 15,686 | 1.59 | 0.74 | 13,580 | 1.00 | SÌ |
| PTS_NQ_TFM_010_60 | 7 | -2,812 | 0.78 | 0.74 | 12,749 | -0.57 | no | 33 | -22,632 | 0.64 | 0.48 | 35,080 | -4.53 | no |
| PTS_NQ_TFM_011_60 | 7 | 1,486 | 1.23 | 0.04 | 5,156 | 0.56 | no | 38 | -20,932 | 0.53 | 0.84 | 20,932 | 0.00 | no |
| PTS_NQ_TFM_012_1440 | 1 | -1,035 | 0.00 | 0.00 | 1,035 | 0.00 | no | 9 | 6,964 | 1.87 | 0.00 | 7,028 | 0.50 | no |
| PTS_NQ_TFM_013_1440 | 3 | 16,572 | 9.00 | 0.71 | 1,037 | 0.94 | no | 9 | 10,513 | 2.31 | 0.02 | 7,028 | 0.60 | no |
| PTS_NQ_TFM_015_240 | 5 | 18,286 | 12.06 | 0.35 | 1,654 | 1.00 | no | 18 | 1,428 | 1.17 | 1.00 | 8,568 | 0.14 | no |
| PTS_NQ_TFU_001_15 | 7 | 7,660 | 1.63 | 0.08 | 12,214 | 0.51 | no | 33 | 31,523 | 1.54 | 0.78 | 14,361 | 0.89 | SÌ |
| PTS_NQ_TFU_002_15 | 13 | -48 | 1.00 | 0.00 | 9,220 | -0.02 | no | 45 | -19,761 | 0.66 | 0.79 | 31,014 | -9.86 | no |
| PTS_NQ_TFU_003_15 | 17 | 14,337 | 3.21 | 0.89 | 2,069 | 1.00 | SÌ | 53 | 11,339 | 1.31 | 0.69 | 8,078 | 0.58 | no |
| PTS_NQ_TFU_004_60 | 10 | 13,610 | 3.11 | 0.23 | 4,742 | 0.89 | no | 35 | -4,890 | 0.80 | 0.55 | 14,886 | -0.49 | no |
| PTS_NQ_TFU_005_60 | 11 | 5,231 | 1.26 | 0.03 | 12,230 | 0.32 | no | 54 | 2,638 | 1.02 | 0.00 | 20,398 | 0.11 | no |
| PTS_NQ_TFU_006_1440 | 6 | 9,674 | 2.83 | 1.00 | 5,291 | 0.65 | no | 48 | 47,808 | 2.13 | 0.16 | 26,104 | 0.98 | no |
| PTS_NQ_TFU_007_1440 | 4 | 3,009 | 1.96 | 1.00 | 3,144 | 0.49 | no | 16 | 4,936 | 1.38 | 0.00 | 10,040 | 0.71 | no |
| PTS_NQ_TFU_008_240 | 12 | -1,735 | 0.72 | 0.04 | 4,523 | -0.62 | no | 56 | -8,224 | 0.69 | 0.59 | 15,624 | -2.76 | no |
| PTS_NQ_VBO_001_1440 | 0 | 0 | 0.00 | 0.00 | 0 | 0.00 | no | 6 | -6,024 | 0.00 | 1.00 | 6,024 | 0.00 | no |
| PTS_NQ_VBO_002_240 | 10 | 8,894 | 3.52 | 0.72 | 2,945 | 0.99 | SÌ | 21 | 30,410 | 5.56 | 0.79 | 4,131 | 1.00 | SÌ |
| PTS_SB_TFM_001_240 | 3 | 422 | 1.61 | 1.00 | 696 | 1.00 | no | 20 | -4,112 | 0.44 | 0.82 | 4,522 | -10.04 | no |
| PTS_YM_BIA_001_240 | 28 | -5,898 | 0.68 | 0.51 | 9,780 | -5.08 | no | 88 | -3,556 | 0.94 | 0.14 | 19,623 | -2.63 | no |
| PTS_YM_SBO_001_240 | 5 | -1,363 | 0.00 | 1.00 | 1,363 | 0.00 | no | 37 | -8,616 | 0.06 | 0.99 | 8,616 | 0.00 | no |
| PTS_YM_SBO_002_240 | 5 | 5,163 | 7.40 | 0.87 | 536 | 0.95 | no | 32 | 4,870 | 1.68 | 0.01 | 4,574 | 0.89 | no |
| PTS_YM_TFM_001_240 | 1 | -2,525 | 0.00 | 0.00 | 2,525 | 0.00 | no | 7 | -4,528 | 0.64 | 0.06 | 7,512 | -9.43 | no |
| PTS_YM_TFM_002_240 | 4 | 433 | 1.11 | 1.00 | 4,022 | 1.00 | no | 25 | -8,062 | 0.75 | 0.39 | 15,885 | -16.66 | no |
| PTS_YM_TFM_003_240 | 12 | -4,752 | 0.63 | 0.44 | 11,456 | 0.00 | no | 81 | -14,592 | 0.80 | 0.17 | 20,401 | -2.51 | no |
| PTS_YM_TFU_001_60 | 7 | 1,873 | 1.19 | 0.16 | 7,562 | 0.20 | no | 61 | -21,920 | 0.76 | 0.63 | 31,849 | -2.44 | no |
| PTS_YM_TFU_002_60 | 8 | -8,338 | 0.00 | 1.00 | 8,338 | 0.00 | no | 36 | -28,690 | 0.14 | 0.97 | 28,690 | 0.00 | no |
| PTS_YM_TFU_003_60 | 16 | -4,376 | 0.00 | 1.00 | 4,376 | 0.00 | no | 64 | -2,518 | 0.83 | 0.05 | 5,098 | -0.98 | no |

**Crescenti sul cBot:** PTS_ES_BSW_003_15, PTS_NQ_PCH_003_30, PTS_NQ_PCH_004_30, PTS_NQ_TFU_003_15, PTS_NQ_VBO_002_240

**Crescenti sull'interno:** PTS_GC_PCH_004_240, PTS_NQ_TFM_006_30, PTS_NQ_TFM_007_30, PTS_NQ_TFU_001_15, PTS_NQ_VBO_002_240

**Crescenti su entrambi:** PTS_NQ_VBO_002_240

## 7. Equity aggregata per mese (netto per contratto, sovrapposizione)

| mese (uscita) | interno | cBot | Δ |
|---|---:|---:|---:|
| 2025-08 | -1,758 (cum -1,758) | -1,804 (cum -1,804) | -46 |
| 2025-09 | 69,142 (cum 67,384) | 57,200 (cum 55,397) | -11,942 |
| 2025-10 | -53,160 (cum 14,224) | -63,026 (cum -7,629) | -9,866 |
| 2025-11 | -2,994 (cum 11,230) | 31,261 (cum 23,632) | 34,255 |

## 9. Quando arrivano gli intent al cBot (righe `Intent ... attesa`)

`attesa` = ValidFrom − ora del server: negativo = l'intent arriva dopo l'inizio della barra su cui è valido. Per tipo di ordine e timeframe della strategia.

| tipo | tf | intent | in orario (≤ 10 s) | 10 s – 5 min | 5 min – 1 barra | ≥ 1 barra (in ritardo di una barra intera) | esempio strategie ≥ 1 barra |
|---|---:|---:|---:|---:|---:|---:|---|

### 9b. Trade cBot nati da un intent arrivato in ritardo di almeno una barra

Trade cBot con intent risolto: 0 su 786; nati da intent in ritardo ≥ 1 barra: 0, netto/ctr 0; di questi senza corrispondente interno: 0.

| strategia | trade da intent stantio | netto/ctr | non abbinati |
|---|---:|---:|---:|

Trade abbinati in cui il cBot esce per stop più di 5 minuti PRIMA dell'uscita interna (che non era uno stop): 0, Δ netto/ctr 0. Per strategia: .

## 8. Avvisi ed errori nel log del cBot (messaggi normalizzati, prime 25)


Ingressi scartati per classe:

Ingressi annullati per classe:

`Nessun intent per l'account` per motivo:

Ingressi scartati `lato sbagliato` per strategia (prime 15):

