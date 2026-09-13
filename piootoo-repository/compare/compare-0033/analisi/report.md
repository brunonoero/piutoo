# compare-0033 — cBot cTrader contro backtest interno (feed FTMO)

Generato: 2026-09-11 08:29Z. Finestra di sovrapposizione usata per il confronto trade: `2025-08-31` → `2026-08-01` (ingressi).

## 0. Perimetro dei due run

| | interno | cBot |
|---|---|---|
| trade totali | 3815 | 3779 |
| primo ingresso | 2025-08-01 03:11 | 2025-08-31 06:33 |
| ultimo ingresso | 2026-07-31 19:22 | 2026-09-05 19:00 |
| trade nella sovrapposizione | 3504 | 3459 |
| contratti per trade | 1 valori: 1 | 3 valori: 0.1,0.0987,0.096 |
| netto totale | 483,644 | 29,523 |
| netto per contratto, sovrapposizione | 344,738 | 282,409 |
| lordo per contratto, sovrapposizione | 358,754 | 390,396 |
| commissioni per contratto, sovrapposizione | 14,016 | 99,626 |
| swap, sovrapposizione (grezzo) | 0 | -834 |

Log cBot: 921,196 righe; chiusure 3779, fill con spread 3789, ingressi scartati 6706, annullati 32, bracket riancorati 113, chiusure per flat weekend 0, righe non interpretate 0.
Chiusure del log senza trade corrispondente: 0; fill senza trade: 10.

### 0b. Commissioni per contratto e per trade (sovrapposizione)

| simbolo | int trade | int comm/trade/ctr | cBot trade | cBot comm/trade/ctr | cBot swap/trade/ctr | cBot lordo/ctr | cBot netto/ctr |
|---|---:|---:|---:|---:|---:|---:|---:|
| BP | 18 | 4.0 | 14 | 5.0 | -30.1 | 2,999 | 2,508 |
| BTC | 540 | 4.0 | 609 | 24.0 | -1.9 | 10,415 | -5,351 |
| CC | 50 | 4.0 | 44 | 3.6 | 0.0 | 10,078 | 9,920 |
| CL | 33 | 4.0 | 33 | 5.6 | 0.2 | -1,513 | -1,692 |
| CT | 7 | 4.0 | 5 | 2.6 | 0.0 | 6,035 | 6,022 |
| ES | 330 | 4.0 | 301 | 23.2 | -0.4 | -62,898 | -70,005 |
| FDAX | 291 | 4.0 | 312 | 45.4 | 0.0 | 91,834 | 77,662 |
| GC | 451 | 4.0 | 474 | 26.8 | -8.5 | 32,278 | 15,558 |
| KC | 40 | 4.0 | 42 | 6.9 | 0.0 | 8,280 | 7,991 |
| NQ | 1275 | 4.0 | 1280 | 35.4 | -1.9 | 384,269 | 336,531 |
| PL | 16 | 4.0 | 16 | 5.4 | 0.0 | -4,652 | -4,738 |
| SB | 40 | 4.0 | 19 | 1.2 | 0.0 | -7,381 | -7,404 |
| YM | 413 | 4.0 | 310 | 16.2 | -0.7 | -79,350 | -84,592 |

FDAX: rapporto lordo cBot / lordo interno sulle coppie con stessa uscita (n=203): mediana 1.196 — il conto cTrader è in USD e GER40 quota in EUR, l'interno usa 25 EUR/punto senza conversione.

## 1. Differenze di trade per strategia (finestra di sovrapposizione)

Match = stessa strategia, stesso verso, ingresso entro una barra della strategia. Netto per contratto future.

| strategia | tf | int | cbot | match | solo int | solo cbot | Δentry mediano (min) | netto/ctr int | netto/ctr cbot | Δ netto/ctr | int in match (netto) | cbot in match (netto) | int in match (lordo) | cbot in match (lordo) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 60 | 13 | 9 | 7 | 6 | 2 | 0.3 | 1,892 | 6,646 | 4,754 | 4,381 | 8,176 | 4,409 | 8,503 |
| PTS_BP_TFM_002_15 | 15 | 5 | 5 | 5 | 0 | 0 | 0.5 | -4,020 | -4,139 | -119 | -4,020 | -4,139 | -4,000 | -4,001 |
| PTS_BTC_BIA_001_60 | 60 | 243 | 300 | 135 | 108 | 165 | 0.3 | 12,580 | 2,018 | -10,562 | -288 | -25,190 | 252 | -21,950 |
| PTS_BTC_PCH_001_240 | 240 | 209 | 220 | 197 | 12 | 23 | 0.5 | -6,335 | -14,647 | -8,312 | -2,994 | -8,028 | -2,206 | -2,513 |
| PTS_BTC_TFU_001_60 | 60 | 37 | 37 | 36 | 1 | 1 | 0.6 | 6,102 | 4,518 | -1,584 | 6,356 | 4,797 | 6,500 | 5,661 |
| PTS_BTC_TFU_002_60 | 60 | 51 | 52 | 49 | 2 | 3 | 0.4 | 12,046 | 2,760 | -9,286 | 12,554 | 3,628 | 12,750 | 4,804 |
| PTS_CC_PCH_001_240 | 240 | 21 | 19 | 15 | 6 | 4 | 0.0 | 651 | 220 | -431 | 2,192 | 1,285 | 2,252 | 1,339 |
| PTS_CC_SBO_001_60 | 60 | 11 | 10 | 9 | 2 | 1 | 0.1 | 18,188 | 17,976 | -212 | 21,696 | 19,769 | 21,732 | 19,801 |
| PTS_CC_TFM_001_60 | 60 | 6 | 4 | 2 | 4 | 2 | 0.8 | -1,024 | -4,096 | -3,072 | -2,008 | -2,043 | -2,000 | -2,036 |
| PTS_CC_TFM_002_60 | 60 | 12 | 11 | 9 | 3 | 2 | 0.0 | -9,439 | -4,180 | 5,259 | -3,427 | -160 | -3,391 | -128 |
| PTS_CL_MAC_001_30 | 30 | 33 | 33 | 33 | 0 | 0 | 0.0 | 315 | -1,692 | -2,007 | 315 | -1,692 | 447 | -1,513 |
| PTS_CT_TFU_001_240 | 240 | 7 | 5 | 0 | 7 | 5 | - | -2,173 | 6,022 | 8,195 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_001_60 | 60 | 18 | 7 | 7 | 11 | 0 | 60.0 | 28,102 | 12,213 | -15,888 | 12,487 | 12,213 | 12,515 | 12,376 |
| PTS_ES_BSW_002_15 | 15 | 14 | 10 | 10 | 4 | 0 | 15.0 | 43,426 | 32,873 | -10,553 | 33,007 | 32,873 | 33,047 | 33,105 |
| PTS_ES_BSW_003_15 | 15 | 38 | 33 | 33 | 5 | 0 | 15.0 | 11,746 | 1,987 | -9,759 | 2,706 | 1,987 | 2,838 | 2,753 |
| PTS_ES_PCH_001_60 | 60 | 20 | 20 | 18 | 2 | 2 | 0.2 | 1,365 | -9 | -1,374 | 1,266 | 310 | 1,338 | 728 |
| PTS_ES_PCH_002_60 | 60 | 36 | 36 | 32 | 4 | 4 | -0.6 | -919 | -1,268 | -349 | -4,208 | -1,904 | -4,080 | -1,162 |
| PTS_ES_PCH_003_1440 | 1440 | 12 | 11 | 11 | 1 | 0 | 0.3 | -5,109 | -6,095 | -986 | -5,055 | -6,095 | -5,011 | -5,840 |
| PTS_ES_PCH_004_240 | 240 | 71 | 69 | 63 | 8 | 6 | -0.3 | -60,444 | -47,572 | 12,872 | -60,412 | -47,318 | -60,160 | -45,791 |
| PTS_ES_SBO_001_15 | 15 | 6 | 6 | 6 | 0 | 0 | 0.6 | 5,519 | 5,337 | -182 | 5,519 | 5,337 | 5,543 | 5,477 |
| PTS_ES_SBO_002_240 | 240 | 15 | 14 | 12 | 3 | 2 | 0.7 | 784 | -11,843 | -12,627 | -920 | -17,796 | -872 | -17,518 |
| PTS_ES_SBO_003_240 | 240 | 18 | 17 | 15 | 3 | 2 | 0.5 | -16,260 | -21,459 | -5,199 | -24,248 | -23,414 | -24,188 | -23,066 |
| PTS_ES_TFM_001_1440 | 1440 | 10 | 10 | 8 | 2 | 2 | 0.5 | -20,040 | 10,424 | 30,464 | -16,032 | -16,376 | -16,000 | -16,162 |
| PTS_ES_TFM_002_1440 | 1440 | 29 | 30 | 28 | 1 | 2 | 0.4 | -7,021 | -6,219 | 803 | -6,017 | -4,167 | -5,905 | -3,517 |
| PTS_ES_TFU_001_1440 | 1440 | 43 | 38 | 35 | 8 | 3 | 0.2 | -41,628 | -38,376 | 3,252 | -33,596 | -40,261 | -33,456 | -39,420 |
| PTS_FDAX_MAC_001_240 | 240 | 10 | 10 | 10 | 0 | 0 | 0.0 | -20,973 | -21,508 | -535 | -20,973 | -21,508 | -20,933 | -21,054 |
| PTS_FDAX_PCH_001_240 | 240 | 92 | 91 | 87 | 5 | 4 | 0.2 | 6,215 | 8,281 | 2,067 | 7,235 | 6,826 | 7,583 | 10,776 |
| PTS_FDAX_SBO_001_240 | 240 | 55 | 54 | 53 | 2 | 1 | 0.3 | 8,780 | -187 | -8,967 | 6,038 | 156 | 6,250 | 2,562 |
| PTS_FDAX_SBO_002_1440 | 1440 | 12 | 15 | 12 | 0 | 3 | 0.5 | 3,952 | 144 | -3,808 | 3,952 | 3,900 | 4,000 | 4,445 |
| PTS_FDAX_TFU_001_1440 | 1440 | 39 | 43 | 37 | 2 | 6 | 0.2 | 32,248 | 48,616 | 16,368 | 36,256 | 46,965 | 36,404 | 48,653 |
| PTS_FDAX_TFU_002_1440 | 1440 | 40 | 45 | 38 | 2 | 7 | 0.2 | 24,804 | 32,753 | 7,949 | 28,812 | 33,272 | 28,964 | 34,997 |
| PTS_FDAX_VBO_001_240 | 240 | 43 | 54 | 42 | 1 | 12 | 0.2 | 15,102 | 9,563 | -5,539 | 15,106 | 13,452 | 15,274 | 15,359 |
| PTS_GC_PCH_001_60 | 60 | 20 | 21 | 20 | 0 | 1 | 0.7 | -15,080 | -21,345 | -6,265 | -15,080 | -16,818 | -15,000 | -15,469 |
| PTS_GC_PCH_004_240 | 240 | 251 | 268 | 232 | 19 | 36 | 0.4 | 47,144 | 22,133 | -25,011 | 45,312 | 13,290 | 46,240 | 19,757 |
| PTS_GC_PCH_005_240 | 240 | 80 | 87 | 71 | 9 | 16 | 0.5 | 28,048 | 44,129 | 16,081 | 32,114 | 25,895 | 32,398 | 28,985 |
| PTS_GC_RHL_001_60 | 60 | 31 | 32 | 31 | 0 | 1 | 0.4 | -5,313 | -3,042 | 2,271 | -5,313 | -2,845 | -5,189 | -1,931 |
| PTS_GC_RHL_002_60 | 60 | 30 | 30 | 30 | 0 | 0 | 0.6 | 2,335 | 1,662 | -673 | 2,335 | 1,662 | 2,455 | 2,466 |
| PTS_GC_TFM_001_240 | 240 | 7 | 7 | 7 | 0 | 0 | 0.4 | -10,028 | -10,259 | -231 | -10,028 | -10,259 | -10,000 | -10,063 |
| PTS_GC_TFU_001_30 | 30 | 32 | 29 | 29 | 3 | 0 | 0.3 | -34,379 | -17,721 | 16,659 | -23,867 | -17,721 | -23,751 | -16,271 |
| PTS_KC_SBO_001_240 | 240 | 40 | 42 | 37 | 3 | 5 | 0.1 | 25,709 | 7,991 | -17,718 | 26,157 | 9,026 | 26,305 | 9,281 |
| PTS_NQ_PCH_001_15 | 15 | 90 | 90 | 87 | 3 | 3 | 0.4 | 9,160 | 3,602 | -5,558 | 6,952 | 5,227 | 7,300 | 8,307 |
| PTS_NQ_PCH_002_15 | 15 | 65 | 65 | 64 | 1 | 1 | 0.4 | 2,791 | -3,384 | -6,175 | 743 | -2,848 | 999 | -582 |
| PTS_NQ_PCH_003_30 | 30 | 64 | 64 | 59 | 5 | 5 | 0.2 | 16,787 | 10,609 | -6,178 | -870 | -8,176 | -634 | -6,087 |
| PTS_NQ_PCH_004_30 | 30 | 44 | 44 | 42 | 2 | 2 | 0.3 | 24,418 | 19,669 | -4,749 | 20,226 | 15,755 | 20,394 | 17,242 |
| PTS_NQ_PCH_007_240 | 240 | 18 | 18 | 17 | 1 | 1 | 0.3 | -16,976 | -19,075 | -2,098 | -18,782 | -21,031 | -18,714 | -20,430 |
| PTS_NQ_PCH_008_240 | 240 | 44 | 45 | 44 | 0 | 1 | 0.2 | 12,865 | 8,996 | -3,868 | 12,865 | 11,090 | 13,041 | 12,956 |
| PTS_NQ_RBM_001_15 | 15 | 150 | 136 | 114 | 36 | 22 | 0.7 | 23,201 | 40,761 | 17,560 | 40,002 | 39,931 | 40,458 | 43,987 |
| PTS_NQ_SBO_001_15 | 15 | 33 | 34 | 32 | 1 | 2 | 0.3 | -5,482 | -11,479 | -5,997 | -8,478 | -9,390 | -8,350 | -8,257 |
| PTS_NQ_SBO_002_15 | 15 | 21 | 22 | 21 | 0 | 1 | 0.4 | 416 | -4,217 | -4,633 | 416 | -176 | 500 | 568 |
| PTS_NQ_SBO_003_15 | 15 | 30 | 30 | 28 | 2 | 2 | 0.3 | 51,804 | 50,088 | -1,716 | 53,812 | 52,175 | 53,924 | 53,166 |
| PTS_NQ_SBO_004_60 | 60 | 18 | 18 | 18 | 0 | 0 | 0.2 | -9,072 | -10,142 | -1,070 | -9,072 | -10,142 | -9,000 | -9,505 |
| PTS_NQ_SBO_005_1440 | 1440 | 11 | 11 | 10 | 1 | 1 | 0.5 | 8,312 | 11,421 | 3,109 | 9,316 | 9,201 | 9,356 | 9,555 |
| PTS_NQ_SBO_006_240 | 240 | 30 | 31 | 30 | 0 | 1 | 0.2 | 19,992 | 18,565 | -1,427 | 19,992 | 18,854 | 20,112 | 19,916 |
| PTS_NQ_TFM_001_60 | 60 | 17 | 17 | 17 | 0 | 0 | 0.3 | 2,582 | 1,890 | -692 | 2,582 | 1,890 | 2,650 | 2,492 |
| PTS_NQ_TFM_002_15 | 15 | 34 | 34 | 33 | 1 | 1 | 0.4 | 3,548 | 1,167 | -2,381 | 6,552 | 4,219 | 6,684 | 5,387 |
| PTS_NQ_TFM_003_15 | 15 | 40 | 38 | 33 | 7 | 5 | 0.2 | 32,840 | 30,318 | -2,522 | 15,368 | 14,936 | 15,500 | 16,412 |
| PTS_NQ_TFM_005_15 | 15 | 33 | 33 | 32 | 1 | 1 | 0.4 | -6,847 | -7,542 | -696 | -12,843 | -13,507 | -12,715 | -12,271 |
| PTS_NQ_TFM_006_30 | 30 | 29 | 27 | 24 | 5 | 3 | 0.2 | 16,884 | -4,289 | -21,173 | 8,904 | -2,567 | 9,000 | -1,717 |
| PTS_NQ_TFM_007_30 | 30 | 20 | 20 | 20 | 0 | 0 | 0.3 | 12,814 | 12,393 | -421 | 12,814 | 12,393 | 12,894 | 13,101 |
| PTS_NQ_TFM_008_30 | 30 | 20 | 20 | 20 | 0 | 0 | 0.2 | 919 | -1,573 | -2,492 | 919 | -1,573 | 999 | -865 |
| PTS_NQ_TFM_009_60 | 60 | 21 | 21 | 21 | 0 | 0 | 0.3 | -4,084 | -5,108 | -1,024 | -4,084 | -5,108 | -4,000 | -4,364 |
| PTS_NQ_TFM_010_60 | 60 | 32 | 32 | 32 | 0 | 0 | 0.4 | -20,128 | -22,177 | -2,049 | -20,128 | -22,177 | -20,000 | -20,735 |
| PTS_NQ_TFM_011_60 | 60 | 30 | 30 | 30 | 0 | 0 | 0.4 | -5,111 | -6,421 | -1,310 | -5,111 | -6,421 | -4,991 | -5,359 |
| PTS_NQ_TFM_012_1440 | 1440 | 7 | 6 | 5 | 2 | 1 | 0.2 | -14,028 | 19,730 | 33,758 | -10,020 | -10,220 | -10,000 | -10,043 |
| PTS_NQ_TFM_013_1440 | 1440 | 9 | 9 | 9 | 0 | 0 | 0.5 | 10,320 | 10,250 | -70 | 10,320 | 10,250 | 10,356 | 10,568 |
| PTS_NQ_TFM_014_240 | 240 | 19 | 19 | 19 | 0 | 0 | 0.2 | 4,954 | 3,966 | -988 | 4,954 | 3,966 | 5,030 | 4,638 |
| PTS_NQ_TFM_015_240 | 240 | 17 | 19 | 17 | 0 | 2 | 0.7 | 12,432 | 10,614 | -1,818 | 12,432 | 11,720 | 12,500 | 12,322 |
| PTS_NQ_TFU_001_15 | 15 | 29 | 29 | 28 | 1 | 1 | 0.2 | 71,633 | 70,203 | -1,430 | 76,041 | 74,919 | 76,153 | 76,116 |
| PTS_NQ_TFU_002_15 | 15 | 40 | 42 | 38 | 2 | 4 | 0.1 | -5,029 | 15,515 | 20,544 | -14,680 | -16,777 | -14,528 | -15,329 |
| PTS_NQ_TFU_003_15 | 15 | 52 | 50 | 46 | 6 | 4 | 0.2 | 18,959 | 18,075 | -883 | 13,919 | 12,131 | 14,103 | 13,862 |
| PTS_NQ_TFU_004_60 | 60 | 34 | 34 | 32 | 2 | 2 | 0.2 | -9,377 | 4,479 | 13,856 | -2,628 | -4,703 | -2,500 | -3,158 |
| PTS_NQ_TFU_005_60 | 60 | 44 | 42 | 38 | 6 | 4 | 0.2 | 36,020 | 13,987 | -22,034 | 27,447 | 12,958 | 27,599 | 14,714 |
| PTS_NQ_TFU_006_1440 | 1440 | 36 | 42 | 35 | 1 | 7 | 0.2 | 23,041 | 31,175 | 8,134 | 24,045 | 22,465 | 24,185 | 23,704 |
| PTS_NQ_TFU_007_1440 | 1440 | 15 | 15 | 15 | 0 | 0 | 0.1 | 5,940 | 5,423 | -517 | 5,940 | 5,423 | 6,000 | 5,954 |
| PTS_NQ_TFU_008_240 | 240 | 53 | 56 | 53 | 0 | 3 | 0.2 | -6,712 | -10,968 | -4,256 | -6,712 | -9,246 | -6,500 | -7,370 |
| PTS_NQ_VBO_001_1440 | 1440 | 7 | 7 | 7 | 0 | 0 | 0.3 | -5,139 | -5,657 | -518 | -5,139 | -5,657 | -5,111 | -5,409 |
| PTS_NQ_VBO_002_240 | 240 | 19 | 30 | 19 | 0 | 11 | 0.5 | 19,285 | 35,666 | 16,381 | 19,285 | 27,911 | 19,361 | 28,583 |
| PTS_PL_TFM_001_240 | 240 | 16 | 16 | 12 | 4 | 4 | 0.0 | -3,833 | -4,738 | -905 | -2,804 | -9,579 | -2,756 | -9,515 |
| PTS_SB_TFM_001_240 | 240 | 40 | 19 | 10 | 30 | 9 | 0.6 | 1,766 | -7,404 | -9,170 | -1,810 | -4,884 | -1,770 | -4,872 |
| PTS_YM_BIA_001_240 | 240 | 120 | 63 | 62 | 58 | 1 | 0.0 | -11,900 | -11,787 | 114 | -6,601 | -11,443 | -6,353 | -10,382 |
| PTS_YM_SBO_001_240 | 240 | 34 | 33 | 32 | 2 | 1 | 0.2 | -7,848 | -8,911 | -1,063 | -7,340 | -8,643 | -7,212 | -8,124 |
| PTS_YM_SBO_002_240 | 240 | 30 | 30 | 30 | 0 | 0 | 0.3 | 3,525 | 2,913 | -612 | 3,525 | 2,913 | 3,645 | 3,399 |
| PTS_YM_TFM_001_240 | 240 | 6 | 6 | 6 | 0 | 0 | 0.3 | 9,655 | 9,503 | -152 | 9,655 | 9,503 | 9,679 | 9,600 |
| PTS_YM_TFM_002_240 | 240 | 20 | 16 | 15 | 5 | 1 | 0.3 | 3,379 | -15,853 | -19,233 | -9,019 | -16,445 | -8,959 | -16,204 |
| PTS_YM_TFM_003_240 | 240 | 57 | 31 | 16 | 41 | 15 | 0.5 | -16,710 | -15,031 | 1,679 | -11,194 | -8,596 | -11,130 | -8,340 |
| PTS_YM_TFU_001_60 | 60 | 55 | 46 | 42 | 13 | 4 | 0.3 | -14,803 | -17,819 | -3,015 | -16,943 | -14,245 | -16,775 | -13,509 |
| PTS_YM_TFU_002_60 | 60 | 33 | 29 | 28 | 5 | 1 | 0.3 | -27,393 | -22,909 | 4,485 | -22,373 | -21,874 | -22,261 | -21,421 |
| PTS_YM_TFU_003_60 | 60 | 58 | 56 | 56 | 2 | 0 | 0.3 | -1,898 | -4,698 | -2,800 | -1,390 | -4,698 | -1,166 | -3,791 |
| **TOTALE** | | 3504 | 3459 | 3009 | 495 | 450 | | 344,738 | 282,409 | -62,329 | 288,315 | 106,792 | 300,351 | 201,336 |

Trade abbinati: 3009. Scarto di ingresso: ≤1 min 2526, ≤5 min 2697, ≤15 min 2831, oltre 178.
Prezzo di ingresso cBot peggiore dell'interno (punti, positivo = cBot paga di più): long mediana 0.29 media 1.9438 (n=2095); short mediana 0.34 media 0.0131 (n=914).

### 1b. Perché un trade interno non esiste sul cBot

| causa | trade | netto/ctr interno di quei trade |
|---|---:|---:|
| cBot già in posizione (nessuna inversione) | 199 | -1,241 |
| nessun evento nel log (livello non toccato o intent non consegnato) | 194 | -10,459 |
| cBot scartato: lato sbagliato | 68 | 37,754 |
| cBot scartato: slippage market | 34 | 30,369 |

Uscite per segnale opposto nell'interno (inversione che il cBot non fa): 148 trade chiusi così, netto/ctr -29,697.

Trade solo cBot: 450, di cui 64 mentre l'interno era già in posizione sulla stessa strategia; netto/ctr dei solo-cBot 175,617.

## 2. Uscite

### 2a. Motivo di uscita dichiarato in trades.json

| interno | n | | cBot | n |
|---|---:|---|---|---:|
| StopLoss | 1938 | | LocalExit:StopLoss | 2691 |
| TrailingStop | 415 | | LocalExit:Closed | 342 |
| TakeProfit | 310 | | LocalExit:TakeProfit | 321 |
| MaxBars | 248 | | BrokerExit:TakeProfit | 84 |
| BreakEven | 230 | | BrokerExit:StopLoss | 21 |
| TimeExit | 215 | |  |  |
| OppositeSignal | 148 | |  |  |

### 2b. Esito reale dal log del cBot (riga `Chiuso`)

| esito log | n | netto/ctr | di cui trades.json=Closed | StopLoss | TakeProfit |
|---|---:|---:|---:|---:|---:|
| StopLoss | 2712 | -2,763,257 | 0 | 2712 | 0 |
| TakeProfit | 405 | 2,514,245 | 0 | 0 | 405 |
| Closed | 342 | 531,422 | 342 | 0 | 0 |

### 2c. Trade abbinati: motivo interno × esito cBot

| interno \ cBot | Closed | StopLoss | TakeProfit | tot |
|---|---:|---:|---:|---:|
| BreakEven | 0 | 212 | 1 | 213 |
| MaxBars | 89 | 19 | 64 | 172 |
| OppositeSignal | 24 | 46 | 17 | 87 |
| StopLoss | 14 | 1672 | 3 | 1689 |
| TakeProfit | 2 | 6 | 264 | 272 |
| TimeExit | 155 | 13 | 13 | 181 |
| TrailingStop | 0 | 394 | 1 | 395 |

Impatto sui trade abbinati (netto/ctr cBot − interno) per coppia di esiti, prime 12 per valore assoluto:

| interno | cBot | n | Δ netto/ctr totale | Δ medio | Δ uscita mediano (min) |
|---|---|---:|---:|---:|---:|
| OppositeSignal | StopLoss | 46 | -108,580 | -2,360 | 476 |
| StopLoss | StopLoss | 1672 | -107,466 | -64 | 0 |
| OppositeSignal | TakeProfit | 17 | 88,088 | 5,182 | 2743 |
| TakeProfit | StopLoss | 6 | -55,292 | -9,215 | -458 |
| TimeExit | StopLoss | 13 | -34,119 | -2,625 | -642 |
| TakeProfit | TakeProfit | 264 | 33,610 | 127 | 1 |
| MaxBars | StopLoss | 19 | -21,839 | -1,149 | 0 |
| OppositeSignal | Closed | 24 | 17,970 | 749 | 2795 |
| StopLoss | TakeProfit | 3 | 15,999 | 5,333 | 127 |
| BreakEven | TakeProfit | 1 | 14,981 | 14,981 | 13057 |
| BreakEven | StopLoss | 212 | -12,626 | -60 | 0 |
| TakeProfit | Closed | 2 | -10,537 | -5,268 | 9 |

### 2d. Stop loss: perdita realizzata contro distanza dichiarata (per contratto, lordo)

Solo trade usciti per stop (interno `StopLoss`; cBot esito `StopLoss` senza trailing). Rapporto = perdita lorda / stop dichiarato (allargato dove previsto). >1 = stop eseguito oltre il livello (gap, spread sugli short).

| simbolo | int n | int rapporto mediano | int oltre 1,1 | cBot n | cBot rapporto mediano | cBot oltre 1,1 | cBot short mediano | cBot long mediano |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| BP | 8 | 1.00 | 0 | 7 | 1.00 | 0 | 1.00 | 1.00 |
| BTC | 339 | 1.00 | 1 | 412 | 1.05 | 115 | 1.03 | 1.05 |
| CC | 37 | 1.00 | 0 | 34 | 1.02 | 7 | 1.01 | 1.02 |
| CL | 17 | 1.00 | 0 | 18 | 1.00 | 0 | 1.00 | 1.00 |
| CT | 0 | NaN | 0 | 1 | 1.00 | 0 | NaN | 1.00 |
| ES | 94 | 1.00 | 2 | 159 | 1.00 | 5 | 1.00 | 0.01 |
| FDAX | 213 | 1.00 | 0 | 265 | 1.20 | 243 | 1.20 | 1.20 |
| GC | 300 | 1.00 | 4 | 314 | 1.01 | 22 | 1.01 | 1.01 |
| KC | 27 | 1.00 | 2 | 33 | 1.04 | 7 | 1.04 | 1.04 |
| NQ | 665 | 1.00 | 8 | 699 | 1.01 | 42 | 1.01 | 1.01 |
| PL | 15 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| SB | 0 | NaN | 0 | 0 | NaN | 0 | NaN | NaN |
| YM | 207 | 1.00 | 1 | 209 | 1.00 | 3 | 1.00 | 1.00 |

### 2f. Trade abbinati: quanto distano le uscite

| |Δ uscita| | n | Σ Δ lordo/ctr (cBot − int) | Σ Δ netto/ctr | Δ netto medio |
|---|---:|---:|---:|---:|
| ≤ 1 min | 2254 | -54,897 | -119,152 | -53 |
| 1–5 min | 218 | -1,197 | -6,723 | -31 |
| 5–60 min | 294 | -14,822 | -22,405 | -76 |
| 1–24 h | 188 | -51,388 | -55,636 | -296 |
| > 24 h | 55 | 23,289 | 22,393 | 407 |

Nota: per le chiusure che il cBot scopre in ritardo (`BrokerExit`), l'esito nel log è dedotto dal segno del netto (`DeduceCloseReason`), quindi `MaxBars → TakeProfit` con Δ uscita 0 è la stessa chiusura etichettata in due modi.

### 2g. Riempimenti dell'interno su un minuto senza barra nel feed

Ingresso/uscita a un istante in cui `@SYM_1.json` non ha una barra: il prezzo viene dal mark-to-market, non dal mercato. Uscite tecniche (TimeExit, MaxBars, SessionFlat, WeekEnd, EndOfRun) escluse dal conteggio delle uscite.

| strategia | ingressi senza barra / tot | netto/ctr di quei trade | uscite SL/TP/trailing/BE senza barra | esempio |
|---|---:|---:|---:|---|
| PTS_BTC_BIA_001_60 | 1/269 | -254 | 3 | Buy 2026-03-08 08:00 @ 67458.9 → StopLoss |
| PTS_BTC_PCH_001_240 | 6/229 | -3,619 | 11 | Sell 2025-11-02 22:00 @ 109673.4 → BreakEven |
| PTS_BTC_TFU_001_60 | 5/38 | -1,270 | 5 | Buy 2025-08-11 21:00 @ 119093.77 → StopLoss |
| PTS_BTC_TFU_002_60 | 2/53 | -508 | 3 | Buy 2025-08-11 21:00 @ 119093.77 → StopLoss |
| PTS_CC_TFM_001_60 | 1/6 | -1,004 | 0 | Sell 2026-03-25 08:00 @ 3146.7 → StopLoss |
| PTS_CC_TFM_002_60 | 0/13 | 0 | 1 |  |
| PTS_ES_BSW_003_15 | 0/41 | 0 | 1 |  |
| PTS_ES_PCH_001_60 | 0/21 | 0 | 1 |  |
| PTS_ES_PCH_002_60 | 0/40 | 0 | 1 |  |
| PTS_ES_PCH_003_1440 | 0/12 | 0 | 1 |  |
| PTS_ES_PCH_004_240 | 0/75 | 0 | 2 |  |
| PTS_ES_SBO_002_240 | 1/17 | 2,975 | 1 | Buy 2025-08-07 22:00 @ 6354.01 → MaxBars |
| PTS_ES_TFU_001_1440 | 2/48 | 3,992 | 2 | Buy 2025-08-07 22:00 @ 6353.51 → TakeProfit |
| PTS_GC_PCH_001_60 | 2/21 | 3,492 | 1 | Buy 2025-10-05 22:00 @ 3891.79 → TakeProfit |
| PTS_GC_PCH_004_240 | 37/270 | -4,881 | 15 | Buy 2025-08-06 22:00 @ 3374.53 → StopLoss |
| PTS_GC_PCH_005_240 | 10/91 | -215 | 0 | Sell 2025-09-07 22:00 @ 3586.96 → TimeExit |
| PTS_GC_TFU_001_30 | 1/37 | -3,504 | 3 | Buy 2026-02-08 23:00 @ 5023.61 → StopLoss |
| PTS_KC_SBO_001_240 | 1/50 | 60 | 0 | Sell 2025-12-24 08:00 @ 357.27 → TimeExit |
| PTS_NQ_PCH_003_30 | 0/68 | 0 | 1 |  |
| PTS_NQ_PCH_004_30 | 0/47 | 0 | 1 |  |
| PTS_NQ_PCH_007_240 | 1/20 | 1,806 | 0 | Buy 2025-09-04 22:00 @ 23664.06 → TrailingStop |
| PTS_NQ_PCH_008_240 | 0/48 | 0 | 1 |  |
| PTS_NQ_SBO_001_15 | 0/36 | 0 | 1 |  |
| PTS_NQ_SBO_005_1440 | 1/13 | -1,004 | 0 | Buy 2025-11-28 13:00 @ 25328.45 → StopLoss |
| PTS_NQ_SBO_006_240 | 2/32 | -508 | 1 | Sell 2025-09-02 22:00 @ 23322.40 → StopLoss |
| PTS_NQ_TFM_001_60 | 0/18 | 0 | 2 |  |
| PTS_NQ_TFM_002_15 | 0/37 | 0 | 1 |  |
| PTS_NQ_TFM_007_30 | 0/21 | 0 | 1 |  |
| PTS_NQ_TFM_009_60 | 0/23 | 0 | 1 |  |
| PTS_NQ_TFM_011_60 | 0/33 | 0 | 1 |  |
| PTS_NQ_TFM_013_1440 | 1/10 | -1,004 | 0 | Buy 2025-11-28 13:00 @ 25328.45 → StopLoss |
| PTS_NQ_TFM_015_240 | 1/17 | -504 | 0 | Buy 2025-10-06 22:00 @ 24985.85 → StopLoss |
| PTS_NQ_TFU_001_15 | 0/31 | 0 | 1 |  |
| PTS_NQ_TFU_002_15 | 1/43 | -2,504 | 2 | Buy 2026-04-27 22:00 @ 27318.93 → StopLoss |
| PTS_NQ_TFU_004_60 | 0/37 | 0 | 2 |  |
| PTS_NQ_TFU_006_1440 | 2/39 | -2,008 | 3 | Buy 2025-11-28 13:00 @ 25328.45 → StopLoss |
| PTS_NQ_TFU_008_240 | 0/58 | 0 | 1 |  |
| PTS_NQ_VBO_001_1440 | 1/8 | 1,300 | 0 | Buy 2025-08-06 22:00 @ 23283.65 → TrailingStop |
| PTS_NQ_VBO_002_240 | 2/19 | -660 | 1 | Buy 2025-12-07 23:00 @ 25708.06074 → TimeExit |
| PTS_SB_TFM_001_240 | 7/43 | -252 | 2 | Sell 2025-10-30 08:00 @ 13.72 → OppositeSignal |
| PTS_YM_BIA_001_240 | 71/131 | -7,781 | 13 | Sell 2025-08-02 00:00 @ 43584.48 → MaxBars |
| PTS_YM_SBO_001_240 | 2/37 | -508 | 2 | Sell 2025-12-17 23:00 @ 47882.68 → StopLoss |
| PTS_YM_SBO_002_240 | 2/31 | -1,008 | 2 | Sell 2025-12-17 23:00 @ 47885.68 → StopLoss |
| PTS_YM_TFM_001_240 | 0/7 | 0 | 1 |  |
| PTS_YM_TFM_002_240 | 0/21 | 0 | 2 |  |
| PTS_YM_TFM_003_240 | 3/63 | -5,976 | 3 | Sell 2025-12-18 23:00 @ 47862.38 → OppositeSignal |
| PTS_YM_TFU_001_60 | 3/62 | -1,012 | 2 | Buy 2025-11-28 13:00 @ 47481.48 → TakeProfit |
| PTS_YM_TFU_002_60 | 1/36 | -1,004 | 1 | Buy 2026-05-13 22:00 @ 49829.6 → StopLoss |
| **TOTALE** | 170 | -27,364 | | |

Per confronto, ingressi cBot il cui minuto non ha una barra nel feed raccolto: 31 su 3779 (il cBot esegue sui tick di cTrader, il feed è la raccolta a barre dello stesso broker).

Bracket riancorato al fill (ingresso slittato rispetto al livello): 113 trade; slippage mediano 0.43 punti, costo totale per contratto 2,988.

### 2e. Verifica contro l'export eventi di cTrader

Posizioni nell'export: 3789, chiuse 3789; trade cBot abbinati a una posizione: 3779 su 3779; posizioni senza trade registrato: 10.
Le posizioni non registrate valgono lordo 16 (prime: #3550 Sell 2026-08-12 14:08 53749 → 53238.86 Position closed 255.07; #3608 Sell 2026-08-18 04:21 53396.36 → 53238.86 Position closed 78.75; #3695 Buy 2026-08-27 15:24 53708.95 → 53236.23 Position closed -236.36; #3716 Sell 2026-08-28 13:59 1.35692 → 1.3516 Position closed 31.92; #3725 Sell 2026-08-28 15:31 17.43 → 18.03 Position closed -67.2; #3740 Buy 2026-08-31 08:29 90.45 → 84.31 Position closed -307; #3771 Buy 2026-09-02 23:46 29163.18 → 29494.84 Position closed 663.32; #3785 Buy 2026-09-04 05:19 7756.25 → 7708.75 Position closed -237.5).

| evento di chiusura cTrader | n | trades.json StopLoss | TakeProfit | Closed | prezzo uscita diverso | lordo diverso |
|---|---:|---:|---:|---:|---:|---:|
| Stop Loss Hit | 2937 | 2937 | 0 | 0 | 182 | 282 |
| Position closed | 487 | 22 | 93 | 372 | 0 | 0 |
| Take Profit Hit | 355 | 0 | 355 | 0 | 16 | 24 |

Saldo finale cTrader: 129,436.54 (partenza 100.000); somma lordi export 41,291.25; somma netti trades.json 29,522.56.

## 3. Finestra operativa (TradingWindow) e giorno saltato

Per ogni trade si ricava la barra di segnale (la barra della strategia che precede quella del fill) e si etichetta come fa il motore (`BarLabelHhmm` sull'orologio della finestra). Fuori finestra = un ingresso che la strategia non avrebbe dovuto emettere.

| strategia | finestra | fuso | int fuori/tot | cBot fuori/tot | int giorno saltato | cBot giorno saltato | fill fuori griglia int/cBot |
|---|---|---|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 2000-1900 | Research | 0/13 | 0/9 | 0 | 0 | 0/0 |
| PTS_BP_TFM_002_15 | 2300-2200 | Research | 0/5 | 0/5 | 0 | 0 | 0/0 |
| PTS_BTC_BIA_001_60 | 0000-2359 | Research | 0/243 | 0/300 | 0 | 0 | 0/1 |
| PTS_BTC_PCH_001_240 | 0600-2359 | Research | 0/209 | 0/220 | 0 | 0 | 0/0 |
| PTS_BTC_TFU_001_60 | 0400-0300 | Research | 0/37 | 0/37 | 0 | 0 | 0/0 |
| PTS_BTC_TFU_002_60 | 0400-0300 | Research | 0/51 | 0/52 | 0 | 1 | 0/1 |
| PTS_CC_PCH_001_240 | 0000-1400 | Research | 0/21 | 0/19 | 0 | 0 | 0/0 |
| PTS_CC_SBO_001_60 | 1100-1900 | Research | 0/11 | 0/10 | 0 | 0 | 0/0 |
| PTS_CC_TFM_001_60 | 1700-1500 | Research | 0/6 | 0/4 | 0 | 0 | 1/0 |
| PTS_CC_TFM_002_60 | 1700-1400 | Research | 0/12 | 0/11 | 0 | 0 | 0/0 |
| PTS_CL_MAC_001_30 | 0000-2359 | Research | 0/33 | 0/33 | 0 | 0 | 0/0 |
| PTS_CT_TFU_001_240 | 0000-2359 | Research | 0/7 | 0/5 | 0 | 0 | 0/0 |
| PTS_ES_BSW_001_60 | 0000-2359 | Research | 0/18 | 0/7 | 0 | 0 | 0/0 |
| PTS_ES_BSW_002_15 | 0000-2359 | Research | 0/14 | 0/10 | 0 | 0 | 0/0 |
| PTS_ES_BSW_003_15 | 0000-2359 | Research | 0/38 | 0/33 | 0 | 0 | 0/0 |
| PTS_ES_PCH_001_60 | 0300-0200 | Research | 0/20 | 0/20 | 0 | 0 | 0/0 |
| PTS_ES_PCH_002_60 | 0800-1000 | Research | 0/36 | 0/36 | 0 | 0 | 0/0 |
| PTS_ES_PCH_003_1440 | 0000-2359 | Research | 0/12 | 0/11 | 0 | 0 | 0/0 |
| PTS_ES_PCH_004_240 | 0000-2359 | Research | 0/71 | 0/69 | 0 | 0 | 0/0 |
| PTS_ES_SBO_001_15 | 0300-0200 | Research | 0/6 | 0/6 | 0 | 0 | 0/0 |
| PTS_ES_SBO_002_240 | 1400-0900 | Research | 0/15 | 0/14 | 0 | 0 | 0/0 |
| PTS_ES_SBO_003_240 | 1400-0900 | Research | 0/18 | 0/17 | 0 | 0 | 0/0 |
| PTS_ES_TFM_001_1440 | 0000-2359 | Research | 0/10 | 0/10 | 0 | 0 | 0/0 |
| PTS_ES_TFM_002_1440 | 0000-2359 | Research | 0/29 | 0/30 | 0 | 0 | 0/0 |
| PTS_ES_TFU_001_1440 | 0000-2359 | Research | 0/43 | 0/38 | 0 | 0 | 0/0 |
| PTS_FDAX_MAC_001_240 | 0000-2359 | Research | 0/10 | 0/10 | 0 | 0 | 0/0 |
| PTS_FDAX_PCH_001_240 | 0700-1300 | Research | 0/92 | 0/91 | 0 | 0 | 0/0 |
| PTS_FDAX_SBO_001_240 | 0700-1000 | Research | 0/55 | 0/54 | 0 | 0 | 0/0 |
| PTS_FDAX_SBO_002_1440 | 0000-2359 | Research | 0/12 | 0/15 | 0 | 0 | 0/0 |
| PTS_FDAX_TFU_001_1440 | 0000-2359 | Research | 0/39 | 0/43 | 0 | 0 | 0/0 |
| PTS_FDAX_TFU_002_1440 | 0000-2359 | Research | 0/40 | 0/45 | 0 | 0 | 0/0 |
| PTS_FDAX_VBO_001_240 | 0700-1400 | Research | 0/43 | 0/54 | 0 | 0 | 0/0 |
| PTS_GC_PCH_001_60 | 0600-0500 | Research | 0/20 | 0/21 | 0 | 0 | 0/0 |
| PTS_GC_PCH_004_240 | 0000-1200 | Research | 0/251 | 0/268 | 0 | 0 | 0/0 |
| PTS_GC_PCH_005_240 | 0000-0900 | Research | 0/80 | 0/87 | 0 | 0 | 0/0 |
| PTS_GC_RHL_001_60 | 1300-1200 | Research | 0/31 | 0/32 | 0 | 0 | 0/0 |
| PTS_GC_RHL_002_60 | 1300-1200 | Research | 0/30 | 0/30 | 0 | 0 | 0/0 |
| PTS_GC_TFM_001_240 | 1000-1300 | Research | 0/7 | 0/7 | 0 | 0 | 0/0 |
| PTS_GC_TFU_001_30 | 1600-0800 | Research | 0/32 | 0/29 | 0 | 0 | 0/0 |
| PTS_KC_SBO_001_240 | 0000-2359 | Research | 0/40 | 0/42 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_001_15 | 1300-0400 | Research | 0/90 | 0/90 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_002_15 | 1300-0400 | Research | 0/65 | 0/65 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_003_30 | 1400-0400 | Research | 0/64 | 0/64 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_004_30 | 1100-1000 | Research | 0/44 | 0/44 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_007_240 | 1000-0000 | Research | 0/18 | 0/18 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_008_240 | 0600-2359 | Research | 0/44 | 0/45 | 0 | 0 | 0/0 |
| PTS_NQ_RBM_001_15 | 0700-0600 | Research | 0/150 | 0/136 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_001_15 | 0500-0400 | Research | 0/33 | 0/34 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_002_15 | 1000-0500 | Research | 0/21 | 0/22 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_003_15 | 1300-0600 | Research | 0/30 | 0/30 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_004_60 | 2200-2100 | Research | 0/18 | 0/18 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_005_1440 | 0000-2359 | Research | 0/11 | 0/11 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_006_240 | 1800-1300 | Research | 0/30 | 0/31 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_001_60 | 1600-0300 | Research | 0/17 | 0/17 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_002_15 | 0900-0500 | Research | 0/34 | 0/34 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_003_15 | 1300-0500 | Research | 0/40 | 0/38 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_005_15 | 1800-1700 | Research | 0/33 | 0/33 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_006_30 | 1400-0400 | Research | 0/29 | 0/27 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_007_30 | 0200-0100 | Research | 0/20 | 0/20 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_008_30 | 0900-1900 | Research | 0/20 | 0/20 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_009_60 | 1400-0400 | Research | 0/21 | 0/21 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_010_60 | 0000-1700 | Research | 0/32 | 0/32 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_011_60 | 2100-1400 | Research | 0/30 | 0/30 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_012_1440 | 0000-2359 | Research | 0/7 | 0/6 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_013_1440 | 0000-2359 | Research | 0/9 | 0/9 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_014_240 | 1400-0500 | Research | 0/19 | 0/19 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_015_240 | 1800-0500 | Research | 0/17 | 0/19 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_001_15 | 1700-1000 | Research | 0/29 | 0/29 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_002_15 | 1700-0700 | Research | 0/40 | 0/42 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_003_15 | 1700-0300 | Research | 0/52 | 0/50 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_004_60 | 1700-0300 | Research | 0/34 | 0/34 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_005_60 | 1600-0400 | Research | 0/44 | 0/42 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_006_1440 | 0000-2359 | Research | 0/36 | 0/42 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_007_1440 | 0000-2359 | Research | 0/15 | 0/15 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_008_240 | 0600-2359 | Research | 0/53 | 1/56 | 0 | 0 | 0/0 |
| PTS_NQ_VBO_001_1440 | 0000-2359 | Research | 0/7 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_VBO_002_240 | 0000-1700 | Research | 0/19 | 0/30 | 0 | 0 | 0/0 |
| PTS_PL_TFM_001_240 | 0000-0800 | Research | 0/16 | 0/16 | 0 | 0 | 0/0 |
| PTS_SB_TFM_001_240 | 1300-2359 | Research | 0/40 | 0/19 | 0 | 0 | 0/0 |
| PTS_YM_BIA_001_240 | 0000-2359 | Research | 0/120 | 0/63 | 0 | 0 | 44/0 |
| PTS_YM_SBO_001_240 | 1400-0500 | Research | 0/34 | 0/33 | 0 | 0 | 0/0 |
| PTS_YM_SBO_002_240 | 1400-0500 | Research | 0/30 | 0/30 | 0 | 0 | 0/0 |
| PTS_YM_TFM_001_240 | 0600-0900 | Research | 0/6 | 0/6 | 0 | 0 | 0/0 |
| PTS_YM_TFM_002_240 | 0600-2359 | Research | 0/20 | 0/16 | 0 | 0 | 0/0 |
| PTS_YM_TFM_003_240 | 0000-2359 | Research | 0/57 | 0/31 | 0 | 0 | 0/0 |
| PTS_YM_TFU_001_60 | 1900-1700 | Research | 0/55 | 0/46 | 0 | 0 | 0/0 |
| PTS_YM_TFU_002_60 | 1200-0300 | Research | 0/33 | 0/29 | 0 | 0 | 0/0 |
| PTS_YM_TFU_003_60 | 0100-2100 | Research | 0/58 | 0/56 | 0 | 0 | 0/0 |
| **TOTALE** | | | 0 | 1 | 0 | 1 | 45/2 |

Esempi fuori finestra (primi 25):

| lato | strategia | ingresso UTC | etichetta barra di segnale | finestra |
|---|---|---|---:|---|
| cBot | PTS_NQ_TFU_008_240 | 2026-02-01 23:05 | 0000 | 0600-2359 |

## 4. Sessione: ancoraggio e un fill per sessione per lato

| strategia | sessione (ancoraggio) | max ingressi/sessione | int sessioni con >max per lato | cBot sessioni con >max per lato | int ingressi weekend UTC | cBot ingressi weekend UTC |
|---|---|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 0000-2359 | 0 | 0 | 0 | 1 | 2 |
| PTS_BP_TFM_002_15 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_BTC_BIA_001_60 | 0000-2359 | 0 | 0 | 0 | 57 | 65 |
| PTS_BTC_PCH_001_240 | 0000-2359 | 1 | 0 | 0 | 86 | 91 |
| PTS_BTC_TFU_001_60 | 0000-2359 | 0 | 0 | 0 | 9 | 9 |
| PTS_BTC_TFU_002_60 | 0000-2359 | 0 | 0 | 0 | 17 | 18 |
| PTS_CC_PCH_001_240 | 0100-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_CC_SBO_001_60 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_CC_TFM_001_60 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_CC_TFM_002_60 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_CL_MAC_001_30 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_CT_TFU_001_240 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_001_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_002_15 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_003_15 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_001_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_002_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_003_1440 | 0000-2359 | 1 | 0 | 0 | 1 | 1 |
| PTS_ES_PCH_004_240 | 0000-2359 | 1 | 1 | 1 | 1 | 1 |
| PTS_ES_SBO_001_15 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_SBO_002_240 | 0000-2359 | 0 | 0 | 0 | 0 | 3 |
| PTS_ES_SBO_003_240 | 0000-2359 | 0 | 0 | 0 | 0 | 1 |
| PTS_ES_TFM_001_1440 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_TFM_002_1440 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_TFU_001_1440 | 0000-2359 | 0 | 0 | 0 | 2 | 1 |
| PTS_FDAX_MAC_001_240 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_FDAX_PCH_001_240 | 0100-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_FDAX_SBO_001_240 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_FDAX_SBO_002_1440 | 0100-2359 | 0 | 0 | 0 | 1 | 2 |
| PTS_FDAX_TFU_001_1440 | 0100-2359 | 0 | 0 | 0 | 0 | 2 |
| PTS_FDAX_TFU_002_1440 | 0100-2359 | 0 | 0 | 0 | 1 | 3 |
| PTS_FDAX_VBO_001_240 | 0100-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_GC_PCH_001_60 | 0000-2359 | 0 | 0 | 0 | 2 | 2 |
| PTS_GC_PCH_004_240 | 0000-2359 | 1 | 4 | 4 | 12 | 12 |
| PTS_GC_PCH_005_240 | 0000-2359 | 1 | 1 | 1 | 5 | 5 |
| PTS_GC_RHL_001_60 | 0000-2359 | 0 | 0 | 0 | 0 | 1 |
| PTS_GC_RHL_002_60 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_GC_TFM_001_240 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_GC_TFU_001_30 | 0000-2359 | 0 | 0 | 0 | 4 | 4 |
| PTS_KC_SBO_001_240 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_PCH_001_15 | 0000-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_PCH_002_15 | 0000-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_PCH_003_30 | 0000-2359 | 0 | 0 | 0 | 3 | 3 |
| PTS_NQ_PCH_004_30 | 0000-2359 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_PCH_007_240 | 0000-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_PCH_008_240 | 0000-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_RBM_001_15 | 0000-2359 | 0 | 0 | 0 | 2 | 2 |
| PTS_NQ_SBO_001_15 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_002_15 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_003_15 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_004_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_SBO_005_1440 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_006_240 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_001_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_002_15 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_003_15 | 0000-2359 | 0 | 0 | 0 | 2 | 1 |
| PTS_NQ_TFM_005_15 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_006_30 | 0000-2359 | 0 | 0 | 0 | 2 | 1 |
| PTS_NQ_TFM_007_30 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_008_30 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_009_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_010_60 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_011_60 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_012_1440 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_013_1440 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_014_240 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_015_240 | 0000-2359 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_001_15 | 0000-2359 | 0 | 0 | 0 | 2 | 2 |
| PTS_NQ_TFU_002_15 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFU_003_15 | 0000-2359 | 0 | 0 | 0 | 5 | 4 |
| PTS_NQ_TFU_004_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFU_005_60 | 0000-2359 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_006_1440 | 0000-2359 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_007_1440 | 0000-2359 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_008_240 | 0000-2359 | 0 | 0 | 0 | 0 | 1 |
| PTS_NQ_VBO_001_1440 | 0000-2359 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_VBO_002_240 | 0000-2359 | 1 | 0 | 0 | 2 | 2 |
| PTS_PL_TFM_001_240 | 0000-2359 | 0 | 0 | 0 | 2 | 2 |
| PTS_SB_TFM_001_240 | 0100-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_BIA_001_240 | 0000-2359 | 0 | 0 | 0 | 43 | 0 |
| PTS_YM_SBO_001_240 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_YM_SBO_002_240 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_001_240 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_002_240 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_003_240 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_YM_TFU_001_60 | 0000-2359 | 0 | 0 | 0 | 1 | 1 |
| PTS_YM_TFU_002_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFU_003_60 | 0000-2359 | 0 | 0 | 0 | 0 | 0 |
| **TOTALE** | | | 6 | 6 | | |

Nel log del cBot il server ha rifiutato template per `limite di ingressi per sessione raggiunto` (conteggio righe, per strategia e lato, prime 15):

- PTS_NQ_PCH_001_15 Buy: 6014
- PTS_NQ_RBM_001_15 Sell: 1367
- PTS_NQ_RBM_001_15 Buy: 791
- PTS_BTC_TFU_002_60 Sell: 783
- PTS_NQ_SBO_001_15 Sell: 743
- PTS_GC_PCH_004_240 Buy: 477
- PTS_GC_TFU_001_30 Sell: 462
- PTS_BTC_TFU_001_60 Buy: 404
- PTS_GC_PCH_004_240 Sell: 359
- PTS_NQ_TFM_003_15 Sell: 351
- PTS_NQ_TFM_002_15 Sell: 331
- PTS_NQ_TFU_003_15 Buy: 326
- PTS_BTC_PCH_001_240 Buy: 306
- PTS_NQ_PCH_003_30 Buy: 284
- PTS_BTC_PCH_001_240 Sell: 274

## 5. Spread

Il backtest interno gira senza spread (`spreadSource: nessuno`). Il cBot lo paga davvero: costo per trade ≈ spread al fill × valore punto × contratti (una volta per round trip). Lo stop è quello eseguito (allargato ×2 dove previsto).

| strategia | simbolo | fill con spread | spread medio (punti) | stop (punti) | spread/stop | costo spread tot (per contratto, $) | netto/ctr cBot | netto/ctr senza spread | costo/trade ($/ctr) |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | BP | 9/9 | 0 | 0.01 | 0.0% | 0 | 6,646 | 6,646 | 0 |
| PTS_BP_TFM_002_15 | BP | 5/5 | 0 | 0.03 | 0.0% | 0 | -4,139 | -4,139 | 0 |
| PTS_BTC_BIA_001_60 | BTC | 320/320 | 2.608 | 50 | 5.2% | 4,172 | -3,648 | 525 | 13 |
| PTS_BTC_PCH_001_240 | BTC | 248/248 | 2.072 | 350 | 0.6% | 2,570 | 2,973 | 5,543 | 10 |
| PTS_BTC_TFU_001_60 | BTC | 39/39 | 4.267 | 50 | 8.5% | 832 | 3,908 | 4,740 | 21 |
| PTS_BTC_TFU_002_60 | BTC | 56/56 | 4.03 | 50 | 8.1% | 1,128 | 1,582 | 2,710 | 20 |
| PTS_CC_PCH_001_240 | CC | 20/20 | 19.14 | 25 | 76.6% | 3,828 | -45 | 3,783 | 191 |
| PTS_CC_SBO_001_60 | CC | 11/11 | 19.064 | 175 | 10.9% | 2,097 | 16,210 | 18,307 | 191 |
| PTS_CC_TFM_001_60 | CC | 4/4 | 20.3 | 100 | 20.3% | 812 | -4,096 | -3,284 | 203 |
| PTS_CC_TFM_002_60 | CC | 12/12 | 19.858 | 200 | 9.9% | 2,383 | -131 | 2,252 | 199 |
| PTS_CL_MAC_001_30 | CL | 35/35 | 0.05 | 1 | 5.0% | 1,740 | -2,249 | -509 | 50 |
| PTS_CT_TFU_001_240 | CT | 5/5 | 0.442 | 12 | 3.7% | 1,105 | 6,022 | 7,127 | 221 |
| PTS_ES_BSW_001_60 | ES | 7/7 | 0.55 | 0 | 0.0% | 193 | 12,213 | 12,406 | 28 |
| PTS_ES_BSW_002_15 | ES | 11/11 | 0.529 | 0 | 0.0% | 291 | 36,492 | 36,783 | 26 |
| PTS_ES_BSW_003_15 | ES | 37/37 | 0.525 | 0 | 0.0% | 972 | 9,262 | 10,234 | 26 |
| PTS_ES_PCH_001_60 | ES | 22/22 | 0.478 | 80 | 0.6% | 526 | 6,743 | 7,268 | 24 |
| PTS_ES_PCH_002_60 | ES | 39/39 | 0.533 | 80 | 0.7% | 1,040 | -2,173 | -1,133 | 27 |
| PTS_ES_PCH_003_1440 | ES | 11/11 | 0.518 | 40 | 1.3% | 285 | -6,095 | -5,810 | 26 |
| PTS_ES_PCH_004_240 | ES | 74/74 | 0.505 | 200 | 0.3% | 1,869 | -47,708 | -45,839 | 25 |
| PTS_ES_SBO_001_15 | ES | 6/6 | 0.558 | 80 | 0.7% | 168 | 5,337 | 5,505 | 28 |
| PTS_ES_SBO_002_240 | ES | 15/15 | 0.523 | 80 | 0.7% | 392 | -15,872 | -15,480 | 26 |
| PTS_ES_SBO_003_240 | ES | 18/18 | 0.531 | 80 | 0.7% | 478 | -25,483 | -25,005 | 27 |
| PTS_ES_TFM_001_1440 | ES | 10/10 | 0.527 | 40 | 1.3% | 264 | 10,424 | 10,688 | 26 |
| PTS_ES_TFM_002_1440 | ES | 33/33 | 0.546 | 20 | 2.7% | 902 | -4,301 | -3,399 | 27 |
| PTS_ES_TFU_001_1440 | ES | 41/41 | 0.541 | 40 | 1.4% | 1,110 | -44,500 | -43,391 | 27 |
| PTS_FDAX_MAC_001_240 | FDAX | 10/10 | 2.224 | 120 | 1.9% | 556 | -21,508 | -20,952 | 56 |
| PTS_FDAX_PCH_001_240 | FDAX | 96/96 | 1.416 | 10 | 14.2% | 3,399 | 8,855 | 12,254 | 35 |
| PTS_FDAX_SBO_001_240 | FDAX | 60/60 | 1.289 | 10 | 12.9% | 1,934 | -2,263 | -330 | 32 |
| PTS_FDAX_SBO_002_1440 | FDAX | 15/15 | 2.889 | 40 | 7.2% | 1,083 | 144 | 1,227 | 72 |
| PTS_FDAX_TFU_001_1440 | FDAX | 48/48 | 2.812 | 80 | 3.5% | 3,374 | 36,413 | 39,787 | 70 |
| PTS_FDAX_TFU_002_1440 | FDAX | 48/48 | 2.644 | 80 | 3.3% | 3,173 | 25,148 | 28,321 | 66 |
| PTS_FDAX_VBO_001_240 | FDAX | 59/59 | 1.339 | 10 | 13.4% | 1,975 | 11,124 | 13,098 | 33 |
| PTS_GC_PCH_001_60 | GC | 25/25 | 0.711 | 45 | 1.6% | 1,778 | 10,574 | 12,352 | 71 |
| PTS_GC_PCH_004_240 | GC | 294/294 | 0.668 | 5 | 13.4% | 19,627 | 13,733 | 33,360 | 67 |
| PTS_GC_PCH_005_240 | GC | 101/101 | 0.542 | 50 | 1.1% | 5,474 | 50,058 | 55,532 | 54 |
| PTS_GC_RHL_001_60 | GC | 37/37 | 0.583 | 40 | 1.5% | 2,157 | -14,969 | -12,812 | 58 |
| PTS_GC_RHL_002_60 | GC | 34/34 | 0.564 | 40 | 1.4% | 1,919 | 567 | 2,486 | 56 |
| PTS_GC_TFM_001_240 | GC | 8/8 | 0.524 | 50 | 1.0% | 419 | -15,345 | -14,926 | 52 |
| PTS_GC_TFU_001_30 | GC | 32/32 | 0.507 | 35 | 1.4% | 1,621 | -9,886 | -8,265 | 51 |
| PTS_KC_SBO_001_240 | KC | 45/45 | 0.319 | 0.67 | 47.9% | 5,385 | 8,649 | 14,034 | 120 |
| PTS_NQ_PCH_001_15 | NQ | 96/96 | 1.501 | 25 | 6.0% | 2,881 | 5,286 | 8,167 | 30 |
| PTS_NQ_PCH_002_15 | NQ | 71/71 | 1.496 | 25 | 6.0% | 2,125 | -1,780 | 344 | 30 |
| PTS_NQ_PCH_003_30 | NQ | 73/73 | 1.528 | 125 | 1.2% | 2,231 | 12,605 | 14,836 | 31 |
| PTS_NQ_PCH_004_30 | NQ | 51/51 | 1.551 | 112.5 | 1.4% | 1,582 | 21,835 | 23,418 | 31 |
| PTS_NQ_PCH_007_240 | NQ | 22/22 | 1.554 | 150 | 1.0% | 684 | -25,268 | -24,584 | 31 |
| PTS_NQ_PCH_008_240 | NQ | 52/52 | 1.449 | 200 | 0.7% | 1,507 | -399 | 1,109 | 29 |
| PTS_NQ_RBM_001_15 | NQ | 147/147 | 1.545 | 100 | 1.5% | 4,544 | 50,345 | 54,888 | 31 |
| PTS_NQ_SBO_001_15 | NQ | 39/39 | 1.553 | 250 | 0.6% | 1,211 | -12,607 | -11,396 | 31 |
| PTS_NQ_SBO_002_15 | NQ | 26/26 | 1.564 | 200 | 0.8% | 813 | -7,362 | -6,549 | 31 |
| PTS_NQ_SBO_003_15 | NQ | 34/34 | 1.554 | 50 | 3.1% | 1,057 | 45,831 | 46,888 | 31 |
| PTS_NQ_SBO_004_60 | NQ | 19/19 | 1.462 | 25 | 5.8% | 556 | -10,682 | -10,127 | 29 |
| PTS_NQ_SBO_005_1440 | NQ | 11/11 | 1.639 | 50 | 3.3% | 361 | 11,421 | 11,782 | 33 |
| PTS_NQ_SBO_006_240 | NQ | 33/33 | 1.715 | 12.5 | 13.7% | 1,132 | 17,990 | 19,122 | 34 |
| PTS_NQ_TFM_001_60 | NQ | 19/19 | 1.337 | 100 | 1.3% | 508 | 5,819 | 6,327 | 27 |
| PTS_NQ_TFM_002_15 | NQ | 39/39 | 1.609 | 150 | 1.1% | 1,255 | 6,986 | 8,242 | 32 |
| PTS_NQ_TFM_003_15 | NQ | 44/44 | 1.522 | 125 | 1.2% | 1,339 | 29,087 | 30,426 | 30 |
| PTS_NQ_TFM_005_15 | NQ | 37/37 | 1.551 | 400 | 0.4% | 1,148 | -11,657 | -10,509 | 31 |
| PTS_NQ_TFM_006_30 | NQ | 31/31 | 1.596 | 25 | 6.4% | 990 | -6,447 | -5,458 | 32 |
| PTS_NQ_TFM_007_30 | NQ | 22/22 | 1.653 | 250 | 0.7% | 727 | 18,333 | 19,060 | 33 |
| PTS_NQ_TFM_008_30 | NQ | 23/23 | 1.882 | 12.5 | 15.1% | 866 | 1,627 | 2,492 | 38 |
| PTS_NQ_TFM_009_60 | NQ | 23/23 | 1.605 | 25 | 6.4% | 738 | 316 | 1,054 | 32 |
| PTS_NQ_TFM_010_60 | NQ | 34/34 | 1.55 | 125 | 1.2% | 1,054 | -19,754 | -18,700 | 31 |
| PTS_NQ_TFM_011_60 | NQ | 36/36 | 1.697 | 125 | 1.4% | 1,222 | -21,712 | -20,491 | 34 |
| PTS_NQ_TFM_012_1440 | NQ | 6/6 | 1.642 | 100 | 1.6% | 197 | 19,730 | 19,927 | 33 |
| PTS_NQ_TFM_013_1440 | NQ | 9/9 | 1.681 | 50 | 3.4% | 303 | 10,250 | 10,552 | 34 |
| PTS_NQ_TFM_014_240 | NQ | 21/21 | 1.47 | 25 | 5.9% | 618 | 33,332 | 33,950 | 29 |
| PTS_NQ_TFM_015_240 | NQ | 20/20 | 1.614 | 25 | 6.5% | 645 | 10,030 | 10,675 | 32 |
| PTS_NQ_TFU_001_15 | NQ | 31/31 | 1.546 | 400 | 0.4% | 959 | 54,125 | 55,084 | 31 |
| PTS_NQ_TFU_002_15 | NQ | 46/46 | 1.368 | 125 | 1.1% | 1,258 | 5,333 | 6,591 | 27 |
| PTS_NQ_TFU_003_15 | NQ | 56/56 | 1.383 | 87.5 | 1.6% | 1,549 | 9,937 | 11,485 | 28 |
| PTS_NQ_TFU_004_60 | NQ | 36/36 | 1.347 | 37.5 | 3.6% | 970 | 2,900 | 3,870 | 27 |
| PTS_NQ_TFU_005_60 | NQ | 47/47 | 1.329 | 400 | 0.3% | 1,250 | 8,553 | 9,802 | 27 |
| PTS_NQ_TFU_006_1440 | NQ | 47/47 | 1.65 | 50 | 3.3% | 1,551 | 41,972 | 43,523 | 33 |
| PTS_NQ_TFU_007_1440 | NQ | 16/16 | 1.624 | 50 | 3.2% | 520 | 4,386 | 4,906 | 32 |
| PTS_NQ_TFU_008_240 | NQ | 59/59 | 1.519 | 25 | 6.1% | 1,793 | -13,488 | -11,695 | 30 |
| PTS_NQ_VBO_001_1440 | NQ | 7/7 | 1.669 | 50 | 3.3% | 234 | -5,657 | -5,423 | 33 |
| PTS_NQ_VBO_002_240 | NQ | 33/33 | 1.675 | 112.5 | 1.5% | 1,105 | 41,857 | 42,962 | 33 |
| PTS_PL_TFM_001_240 | PL | 19/19 | 8.339 | 5 | 166.8% | 7,923 | -13,006 | -5,083 | 417 |
| PTS_SB_TFM_001_240 | SB | 19/19 | 0.129 | 2.01 | 6.4% | 2,755 | -7,404 | -4,648 | 145 |
| PTS_YM_BIA_001_240 | YM | 67/67 | 2.384 | 450 | 0.5% | 799 | -14,973 | -14,174 | 12 |
| PTS_YM_SBO_001_240 | YM | 35/35 | 2.089 | 50 | 4.2% | 366 | -9,445 | -9,080 | 10 |
| PTS_YM_SBO_002_240 | YM | 31/31 | 2.135 | 100 | 2.1% | 331 | 8,899 | 9,230 | 11 |
| PTS_YM_TFM_001_240 | YM | 7/7 | 2.297 | 1000 | 0.2% | 80 | 9,305 | 9,385 | 11 |
| PTS_YM_TFM_002_240 | YM | 16/16 | 2.448 | 1600 | 0.2% | 196 | -15,853 | -15,657 | 12 |
| PTS_YM_TFM_003_240 | YM | 31/31 | 2.42 | 500 | 0.5% | 375 | -15,031 | -14,656 | 12 |
| PTS_YM_TFU_001_60 | YM | 47/47 | 2.427 | 500 | 0.5% | 570 | -20,335 | -19,765 | 12 |
| PTS_YM_TFU_002_60 | YM | 31/31 | 2.165 | 200 | 1.1% | 336 | -17,937 | -17,601 | 11 |
| PTS_YM_TFU_003_60 | YM | 60/60 | 2.093 | 50 | 4.2% | 628 | -518 | 110 | 10 |
| **TOTALE** | | | | | | 140,866 | 295,441 | 436,307 | |

### 5b. Strategie con stop più stretto dello spread, o quasi

| strategia | spread/stop | stop (punti) | trade cBot | netto/ctr cBot | commento |
|---|---:|---:|---:|---:|---|
| PTS_PL_TFM_001_240 | 166.8% | 5 | 19 | -13,006 | **stop sotto lo spread: non eseguibile** |
| PTS_CC_PCH_001_240 | 76.6% | 25 | 20 | -45 | stop di poco sopra lo spread |
| PTS_KC_SBO_001_240 | 47.9% | 0.67 | 45 | 8,649 | oltre il tetto del 20% del cBot (filtro spento nel profilo sorgente) |
| PTS_CC_TFM_001_60 | 20.3% | 100 | 4 | -4,096 | oltre il tetto del 20% del cBot (filtro spento nel profilo sorgente) |
| PTS_NQ_TFM_008_30 | 15.1% | 12.5 | 23 | 1,627 | 10-20% |
| PTS_FDAX_PCH_001_240 | 14.2% | 10 | 96 | 8,855 | 10-20% |
| PTS_NQ_SBO_006_240 | 13.7% | 12.5 | 33 | 17,990 | 10-20% |
| PTS_FDAX_VBO_001_240 | 13.4% | 10 | 59 | 11,124 | 10-20% |
| PTS_GC_PCH_004_240 | 13.4% | 5 | 294 | 13,733 | 10-20% |
| PTS_FDAX_SBO_001_240 | 12.9% | 10 | 60 | -2,263 | 10-20% |
| PTS_CC_SBO_001_60 | 10.9% | 175 | 11 | 16,210 | 10-20% |

Sui trade abbinati: 690 volte il cBot esce per stop dove l'interno no; 17 il contrario.

## 6. Strategie con curva di equity crescente

Metriche su netto per contratto, trade ordinati per uscita. R² = fit lineare della curva cumulata sull'indice dei trade. Crescente = netto > 0, R² ≥ 0,6, chiusura ad almeno il 70% del picco, ≥ 8 trade. Il cBot è calcolato sull'intero run (31/08/2025 → 05/09/2026), l'interno sull'intero run (01/08/2025 → 01/08/2026).

| strategia | cBot n | cBot netto/ctr | PF | R² | DD max/ctr | fine/picco | **cBot** | int n | int netto/ctr | PF | R² | DD max/ctr | fine/picco | **int** |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| PTS_BP_TFM_001_60 | 9 | 6,646 | 3.08 | 0.54 | 1,667 | 0.80 | no | 14 | 1,138 | 1.22 | 0.62 | 2,370 | 0.32 | no |
| PTS_BP_TFM_002_15 | 5 | -4,139 | 0.32 | 0.82 | 5,090 | -4.35 | no | 6 | -3,024 | 0.50 | 0.79 | 5,016 | -1.52 | no |
| PTS_BTC_BIA_001_60 | 320 | -3,648 | 0.96 | 0.11 | 29,039 | -0.40 | no | 269 | 16,994 | 1.26 | 0.02 | 26,627 | 0.80 | no |
| PTS_BTC_PCH_001_240 | 248 | 2,973 | 1.02 | 0.33 | 25,714 | 0.65 | no | 229 | -8,497 | 0.93 | 0.09 | 20,619 | -4.09 | no |
| PTS_BTC_TFU_001_60 | 39 | 3,908 | 1.35 | 0.47 | 5,982 | 0.62 | no | 38 | 5,848 | 1.64 | 0.49 | 5,080 | 0.79 | no |
| PTS_BTC_TFU_002_60 | 56 | 1,582 | 1.10 | 0.06 | 9,151 | 0.52 | no | 53 | 11,538 | 1.93 | 0.45 | 7,620 | 0.98 | no |
| PTS_CC_PCH_001_240 | 20 | -45 | 0.99 | 0.17 | 2,065 | -0.02 | no | 24 | 2,139 | 1.44 | 0.04 | 2,343 | 0.61 | no |
| PTS_CC_SBO_001_60 | 11 | 16,210 | 2.31 | 0.39 | 8,836 | 0.65 | no | 13 | 16,920 | 2.22 | 0.60 | 8,594 | 0.66 | no |
| PTS_CC_TFM_001_60 | 4 | -4,096 | 0.00 | 1.00 | 4,096 | 0.00 | no | 6 | -1,024 | 0.80 | 0.03 | 4,016 | 0.00 | no |
| PTS_CC_TFM_002_60 | 12 | -131 | 0.99 | 0.01 | 6,169 | 0.00 | no | 13 | -5,443 | 0.70 | 0.76 | 11,431 | -1.36 | no |
| PTS_CL_MAC_001_30 | 35 | -2,249 | 0.90 | 0.01 | 9,069 | -2.23 | no | 34 | 720 | 1.04 | 0.02 | 9,036 | 0.26 | no |
| PTS_CT_TFU_001_240 | 5 | 6,022 | 2.00 | 0.04 | 6,008 | 0.67 | no | 8 | 823 | 1.13 | 0.41 | 6,290 | 0.12 | no |
| PTS_ES_BSW_001_60 | 7 | 12,213 | 1.63 | 0.41 | 10,031 | 0.96 | no | 20 | 36,329 | 2.04 | 0.58 | 20,542 | 0.99 | no |
| PTS_ES_BSW_002_15 | 11 | 36,492 | 4.02 | 0.89 | 6,047 | 0.94 | SÌ | 15 | 49,672 | 3.76 | 0.84 | 12,008 | 0.89 | SÌ |
| PTS_ES_BSW_003_15 | 37 | 9,262 | 1.11 | 0.05 | 27,866 | 0.31 | no | 41 | 17,389 | 1.22 | 0.39 | 27,944 | 0.38 | no |
| PTS_ES_PCH_001_60 | 22 | 6,743 | 1.78 | 0.10 | 4,336 | 0.90 | no | 21 | 2,405 | 1.36 | 0.10 | 3,384 | 0.43 | no |
| PTS_ES_PCH_002_60 | 39 | -2,173 | 0.85 | 0.07 | 6,957 | -0.72 | no | 40 | -945 | 0.93 | 0.06 | 6,320 | -0.32 | no |
| PTS_ES_PCH_003_1440 | 11 | -6,095 | 0.11 | 0.92 | 6,095 | 0.00 | no | 12 | -5,109 | 0.16 | 0.87 | 5,109 | 0.00 | no |
| PTS_ES_PCH_004_240 | 74 | -47,708 | 0.24 | 0.88 | 62,549 | 0.00 | no | 75 | -60,460 | 0.00 | 0.92 | 60,460 | 0.00 | no |
| PTS_ES_SBO_001_15 | 6 | 5,337 | 1.44 | 0.22 | 8,057 | 0.72 | no | 8 | 7,511 | 1.47 | 0.08 | 8,008 | 0.79 | no |
| PTS_ES_SBO_002_240 | 15 | -15,872 | 0.64 | 0.56 | 24,165 | -7.61 | no | 17 | -246 | 0.99 | 0.01 | 17,987 | -0.07 | no |
| PTS_ES_SBO_003_240 | 18 | -25,483 | 0.51 | 0.73 | 38,955 | 0.00 | no | 20 | -10,901 | 0.76 | 0.67 | 33,171 | -0.84 | no |
| PTS_ES_TFM_001_1440 | 10 | 10,424 | 1.64 | 0.07 | 10,191 | 0.63 | no | 11 | -8,044 | 0.60 | 1.00 | 20,040 | -0.67 | no |
| PTS_ES_TFM_002_1440 | 33 | -4,301 | 0.85 | 0.52 | 15,903 | -0.73 | no | 33 | -6,037 | 0.78 | 0.57 | 13,072 | -1.97 | no |
| PTS_ES_TFU_001_1440 | 41 | -44,500 | 0.40 | 0.79 | 48,087 | 0.00 | no | 48 | -42,978 | 0.46 | 0.93 | 48,958 | -10.77 | no |
| PTS_FDAX_MAC_001_240 | 10 | -21,508 | 0.13 | 0.88 | 21,508 | 0.00 | no | 12 | -18,984 | 0.26 | 0.98 | 23,977 | -3.80 | no |
| PTS_FDAX_PCH_001_240 | 96 | 8,855 | 1.33 | 0.08 | 6,540 | 0.84 | no | 100 | 6,845 | 1.37 | 0.23 | 4,273 | 0.95 | no |
| PTS_FDAX_SBO_001_240 | 60 | -2,263 | 0.88 | 0.57 | 10,135 | -0.35 | no | 61 | 10,506 | 1.78 | 0.63 | 5,334 | 0.82 | SÌ |
| PTS_FDAX_SBO_002_1440 | 15 | 144 | 1.01 | 0.04 | 12,278 | 0.01 | no | 12 | 3,952 | 1.36 | 0.28 | 7,028 | 0.36 | no |
| PTS_FDAX_TFU_001_1440 | 48 | 36,413 | 1.40 | 0.35 | 38,283 | 0.50 | no | 43 | 24,232 | 1.36 | 0.63 | 32,064 | 0.43 | no |
| PTS_FDAX_TFU_002_1440 | 48 | 25,148 | 1.27 | 0.29 | 31,070 | 0.52 | no | 42 | 34,796 | 1.53 | 0.59 | 22,044 | 0.66 | no |
| PTS_FDAX_VBO_001_240 | 59 | 11,124 | 1.70 | 0.53 | 7,534 | 0.94 | no | 43 | 15,102 | 3.56 | 0.78 | 2,306 | 0.94 | SÌ |
| PTS_GC_PCH_001_60 | 25 | 10,574 | 1.15 | 0.07 | 28,260 | 1.00 | no | 21 | -19,584 | 0.71 | 0.72 | 26,568 | -5.61 | no |
| PTS_GC_PCH_004_240 | 294 | 13,733 | 1.10 | 0.47 | 21,714 | 0.56 | no | 270 | 49,762 | 1.42 | 0.74 | 19,523 | 1.00 | SÌ |
| PTS_GC_PCH_005_240 | 101 | 50,058 | 1.36 | 0.74 | 34,280 | 0.75 | SÌ | 91 | 26,834 | 1.20 | 0.54 | 26,688 | 0.59 | no |
| PTS_GC_RHL_001_60 | 37 | -14,969 | 0.77 | 0.38 | 28,027 | -1.26 | no | 33 | -1,107 | 0.98 | 0.14 | 21,657 | -0.07 | no |
| PTS_GC_RHL_002_60 | 34 | 567 | 1.01 | 0.01 | 20,521 | 0.10 | no | 32 | 5,460 | 1.13 | 0.01 | 19,107 | 0.58 | no |
| PTS_GC_TFM_001_240 | 8 | -15,345 | 0.49 | 0.34 | 17,760 | 0.00 | no | 8 | -13,082 | 0.53 | 0.48 | 20,578 | 0.00 | no |
| PTS_GC_TFU_001_30 | 32 | -9,886 | 0.90 | 0.50 | 47,257 | -0.77 | no | 37 | -44,058 | 0.59 | 0.73 | 57,890 | 0.00 | no |
| PTS_KC_SBO_001_240 | 45 | 8,649 | 1.86 | 0.61 | 4,438 | 0.94 | SÌ | 50 | 23,152 | 3.40 | 0.93 | 2,556 | 0.98 | SÌ |
| PTS_NQ_PCH_001_15 | 96 | 5,286 | 1.17 | 0.32 | 8,435 | 0.72 | no | 97 | 11,510 | 1.46 | 0.51 | 6,425 | 0.91 | no |
| PTS_NQ_PCH_002_15 | 71 | -1,780 | 0.93 | 0.33 | 8,195 | -0.37 | no | 68 | 1,675 | 1.08 | 0.00 | 6,570 | 0.30 | no |
| PTS_NQ_PCH_003_30 | 73 | 12,605 | 1.26 | 0.06 | 20,525 | 0.56 | no | 68 | 16,368 | 1.38 | 0.17 | 19,015 | 0.74 | no |
| PTS_NQ_PCH_004_30 | 51 | 21,835 | 2.30 | 0.39 | 12,111 | 0.91 | no | 47 | 26,278 | 3.09 | 0.50 | 9,392 | 1.00 | no |
| PTS_NQ_PCH_007_240 | 22 | -25,268 | 0.13 | 0.97 | 25,268 | 0.00 | no | 20 | -11,998 | 0.42 | 0.89 | 18,402 | -1.87 | no |
| PTS_NQ_PCH_008_240 | 52 | -399 | 0.99 | 0.13 | 16,406 | -0.03 | no | 48 | 13,435 | 1.41 | 0.43 | 16,455 | 0.76 | no |
| PTS_NQ_RBM_001_15 | 147 | 50,345 | 1.30 | 0.71 | 22,671 | 1.00 | SÌ | 163 | 21,591 | 1.13 | 0.41 | 27,870 | 0.76 | no |
| PTS_NQ_SBO_001_15 | 39 | -12,607 | 0.84 | 0.68 | 31,921 | -0.72 | no | 36 | 3,506 | 1.05 | 0.13 | 26,422 | 0.13 | no |
| PTS_NQ_SBO_002_15 | 26 | -7,362 | 0.84 | 0.12 | 19,379 | -0.61 | no | 22 | 2,912 | 1.09 | 0.18 | 16,016 | 0.15 | no |
| PTS_NQ_SBO_003_15 | 34 | 45,831 | 2.40 | 0.61 | 17,987 | 0.81 | SÌ | 32 | 49,796 | 2.71 | 0.54 | 17,068 | 0.89 | no |
| PTS_NQ_SBO_004_60 | 19 | -10,682 | 0.00 | 1.00 | 10,682 | 0.00 | no | 21 | -9,594 | 0.05 | 1.00 | 9,594 | 0.00 | no |
| PTS_NQ_SBO_005_1440 | 11 | 11,421 | 2.21 | 0.88 | 8,399 | 0.58 | no | 13 | 10,284 | 1.93 | 0.01 | 10,040 | 0.51 | no |
| PTS_NQ_SBO_006_240 | 33 | 17,990 | 3.03 | 0.77 | 2,985 | 0.90 | SÌ | 32 | 19,484 | 3.63 | 0.75 | 3,048 | 0.94 | SÌ |
| PTS_NQ_TFM_001_60 | 19 | 5,819 | 1.19 | 0.21 | 13,965 | 1.00 | no | 18 | 8,578 | 1.31 | 0.01 | 13,402 | 0.81 | no |
| PTS_NQ_TFM_002_15 | 39 | 6,986 | 1.11 | 0.18 | 22,905 | 0.44 | no | 37 | 8,536 | 1.14 | 0.10 | 22,384 | 0.41 | no |
| PTS_NQ_TFM_003_15 | 44 | 29,087 | 1.47 | 0.62 | 23,218 | 0.79 | SÌ | 45 | 41,320 | 1.72 | 0.47 | 22,536 | 0.94 | no |
| PTS_NQ_TFM_005_15 | 37 | -11,657 | 0.91 | 0.00 | 39,995 | -0.97 | no | 36 | 11,141 | 1.10 | 0.01 | 39,038 | 0.37 | no |
| PTS_NQ_TFM_006_30 | 31 | -6,447 | 0.61 | 0.03 | 9,832 | -38.89 | no | 34 | 14,364 | 1.92 | 0.65 | 7,056 | 0.80 | SÌ |
| PTS_NQ_TFM_007_30 | 22 | 18,333 | 1.68 | 0.77 | 13,599 | 1.00 | SÌ | 21 | 14,622 | 1.55 | 0.60 | 13,514 | 1.00 | SÌ |
| PTS_NQ_TFM_008_30 | 23 | 1,627 | 1.26 | 0.03 | 3,206 | 1.00 | no | 21 | 3,567 | 1.78 | 0.34 | 2,541 | 0.67 | no |
| PTS_NQ_TFM_009_60 | 23 | 316 | 1.03 | 0.04 | 6,733 | 1.00 | no | 23 | -5,092 | 0.54 | 0.13 | 7,056 | 0.00 | no |
| PTS_NQ_TFM_010_60 | 34 | -19,754 | 0.69 | 0.54 | 31,668 | -3.98 | no | 36 | -22,644 | 0.67 | 0.60 | 30,096 | -9.09 | no |
| PTS_NQ_TFM_011_60 | 36 | -21,712 | 0.72 | 0.47 | 35,707 | -1.55 | no | 33 | -12,623 | 0.82 | 0.04 | 20,535 | -1.70 | no |
| PTS_NQ_TFM_012_1440 | 6 | 19,730 | 2.93 | 0.19 | 8,185 | 0.71 | no | 8 | -16,032 | 0.00 | 1.00 | 16,032 | 0.00 | no |
| PTS_NQ_TFM_013_1440 | 9 | 10,250 | 2.22 | 0.02 | 7,360 | 0.58 | no | 10 | 9,316 | 2.03 | 0.17 | 7,028 | 0.57 | no |
| PTS_NQ_TFM_014_240 | 21 | 33,332 | 4.15 | 0.08 | 9,478 | 0.98 | no | 21 | 3,946 | 1.39 | 0.00 | 8,568 | 0.32 | no |
| PTS_NQ_TFM_015_240 | 20 | 10,030 | 2.01 | 0.01 | 8,256 | 0.55 | no | 17 | 12,432 | 2.64 | 0.07 | 6,048 | 0.67 | no |
| PTS_NQ_TFU_001_15 | 31 | 54,125 | 1.55 | 0.60 | 44,129 | 0.77 | no | 31 | 79,543 | 1.97 | 0.55 | 43,412 | 1.00 | no |
| PTS_NQ_TFU_002_15 | 46 | 5,333 | 1.06 | 0.22 | 35,377 | 0.34 | no | 43 | 8,633 | 1.12 | 0.08 | 34,107 | 0.33 | no |
| PTS_NQ_TFU_003_15 | 56 | 9,937 | 1.24 | 0.57 | 11,739 | 0.46 | no | 57 | 22,608 | 1.63 | 0.79 | 6,706 | 0.87 | SÌ |
| PTS_NQ_TFU_004_60 | 36 | 2,900 | 1.11 | 0.08 | 14,520 | 0.17 | no | 37 | -889 | 0.97 | 0.26 | 19,373 | -0.05 | no |
| PTS_NQ_TFU_005_60 | 47 | 8,553 | 1.07 | 0.21 | 29,629 | 0.22 | no | 47 | 41,310 | 1.40 | 0.69 | 22,865 | 0.66 | no |
| PTS_NQ_TFU_006_1440 | 47 | 41,972 | 1.88 | 0.08 | 31,054 | 0.98 | no | 39 | 30,284 | 1.78 | 0.00 | 30,919 | 0.91 | no |
| PTS_NQ_TFU_007_1440 | 16 | 4,386 | 1.32 | 0.01 | 10,538 | 0.68 | no | 16 | 4,936 | 1.38 | 0.01 | 10,040 | 0.83 | no |
| PTS_NQ_TFU_008_240 | 59 | -13,488 | 0.57 | 0.74 | 17,823 | -4.84 | no | 58 | 768 | 1.03 | 0.53 | 15,120 | 0.07 | no |
| PTS_NQ_VBO_001_1440 | 7 | -5,657 | 0.10 | 0.92 | 5,657 | 0.00 | no | 8 | -3,839 | 0.36 | 0.91 | 5,139 | -2.95 | no |
| PTS_NQ_VBO_002_240 | 33 | 41,857 | 4.47 | 0.91 | 3,251 | 0.97 | SÌ | 19 | 19,285 | 3.73 | 0.72 | 3,962 | 1.00 | SÌ |
| PTS_PL_TFM_001_240 | 19 | -13,006 | 0.44 | 0.72 | 15,716 | -4.80 | no | 18 | -4,341 | 0.00 | 1.00 | 4,341 | 0.00 | no |
| PTS_SB_TFM_001_240 | 19 | -7,404 | 0.27 | 0.94 | 7,826 | -17.54 | no | 43 | 1,687 | 1.21 | 0.43 | 2,964 | 0.78 | no |
| PTS_YM_BIA_001_240 | 67 | -14,973 | 0.69 | 0.75 | 20,788 | -11.38 | no | 131 | -13,431 | 0.83 | 0.67 | 20,670 | -37.55 | no |
| PTS_YM_SBO_001_240 | 35 | -9,445 | 0.00 | 1.00 | 9,445 | 0.00 | no | 37 | -8,610 | 0.06 | 0.99 | 8,610 | 0.00 | no |
| PTS_YM_SBO_002_240 | 31 | 8,899 | 1.71 | 0.40 | 5,248 | 1.00 | no | 31 | 3,021 | 1.24 | 0.43 | 5,040 | 0.40 | no |
| PTS_YM_TFM_001_240 | 7 | 9,305 | 2.68 | 0.81 | 4,558 | 0.90 | no | 7 | 13,397 | 3.59 | 0.63 | 4,520 | 0.95 | no |
| PTS_YM_TFM_002_240 | 16 | -15,853 | 0.58 | 0.43 | 23,871 | 0.00 | no | 21 | -112 | 1.00 | 0.00 | 16,698 | 0.00 | no |
| PTS_YM_TFM_003_240 | 31 | -15,031 | 0.76 | 0.56 | 20,796 | -2.61 | no | 63 | -11,302 | 0.86 | 0.08 | 22,310 | -1.49 | no |
| PTS_YM_TFU_001_60 | 47 | -20,335 | 0.75 | 0.68 | 33,119 | -1.59 | no | 62 | -13,506 | 0.85 | 0.51 | 31,771 | -1.05 | no |
| PTS_YM_TFU_002_60 | 31 | -17,937 | 0.40 | 0.83 | 23,765 | 0.00 | no | 36 | -30,361 | 0.11 | 0.98 | 30,361 | 0.00 | no |
| PTS_YM_TFU_003_60 | 60 | -518 | 0.97 | 0.00 | 6,221 | -0.34 | no | 61 | -2,660 | 0.82 | 0.12 | 4,833 | -1.39 | no |

**Crescenti sul cBot:** PTS_ES_BSW_002_15, PTS_GC_PCH_005_240, PTS_KC_SBO_001_240, PTS_NQ_RBM_001_15, PTS_NQ_SBO_003_15, PTS_NQ_SBO_006_240, PTS_NQ_TFM_003_15, PTS_NQ_TFM_007_30, PTS_NQ_VBO_002_240

**Crescenti sull'interno:** PTS_ES_BSW_002_15, PTS_FDAX_SBO_001_240, PTS_FDAX_VBO_001_240, PTS_GC_PCH_004_240, PTS_KC_SBO_001_240, PTS_NQ_SBO_006_240, PTS_NQ_TFM_006_30, PTS_NQ_TFM_007_30, PTS_NQ_TFU_003_15, PTS_NQ_VBO_002_240

**Crescenti su entrambi:** PTS_ES_BSW_002_15, PTS_KC_SBO_001_240, PTS_NQ_SBO_006_240, PTS_NQ_TFM_007_30, PTS_NQ_VBO_002_240

## 7. Equity aggregata per mese (netto per contratto, sovrapposizione)

| mese (uscita) | interno | cBot | Δ |
|---|---:|---:|---:|
| 2025-08 | -1,758 (cum -1,758) | -1,804 (cum -1,804) | -46 |
| 2025-09 | 179,031 (cum 177,273) | 182,050 (cum 180,247) | 3,019 |
| 2025-10 | 23,852 (cum 201,125) | -13,590 (cum 166,657) | -37,442 |
| 2025-11 | 17,407 (cum 218,532) | 79,222 (cum 245,878) | 61,814 |
| 2025-12 | -104,860 (cum 113,672) | -88,938 (cum 156,940) | 15,922 |
| 2026-01 | -59,729 (cum 53,943) | -71,493 (cum 85,447) | -11,764 |
| 2026-02 | -97,836 (cum -43,892) | -133,718 (cum -48,271) | -35,883 |
| 2026-03 | -46,062 (cum -89,955) | -76,142 (cum -124,413) | -30,079 |
| 2026-04 | 306,632 (cum 216,678) | 263,726 (cum 139,313) | -42,907 |
| 2026-05 | 187,248 (cum 403,925) | 165,802 (cum 305,115) | -21,445 |
| 2026-06 | -61,293 (cum 342,632) | -86,982 (cum 218,133) | -25,689 |
| 2026-07 | 2,106 (cum 344,738) | -7,814 (cum 210,319) | -9,919 |
| 2026-08 | 0 (cum 344,738) | 72,090 (cum 282,409) | 72,090 |

## 9. Quando arrivano gli intent al cBot (righe `Intent ... attesa`)

`attesa` = ValidFrom − ora del server: negativo = l'intent arriva dopo l'inizio della barra su cui è valido. Per tipo di ordine e timeframe della strategia.

| tipo | tf | intent | in orario (≤ 10 s) | 10 s – 5 min | 5 min – 1 barra | ≥ 1 barra (in ritardo di una barra intera) | esempio strategie ≥ 1 barra |
|---|---:|---:|---:|---:|---:|---:|---|
| Limit | 15 | 2826 | 2669 | 4 | 1 | 152 | PTS_NQ_RBM_001_15 (152) |
| Limit | 60 | 1685 | 1669 | 0 | 0 | 16 | PTS_GC_RHL_001_60 (9), PTS_GC_RHL_002_60 (7) |
| Market | 15 | 57 | 0 | 0 | 0 | 57 | PTS_ES_BSW_003_15 (42), PTS_ES_BSW_002_15 (15) |
| Market | 30 | 35 | 33 | 2 | 0 | 0 |  |
| Market | 60 | 18 | 0 | 0 | 0 | 18 | PTS_ES_BSW_001_60 (18) |
| Market | 240 | 103 | 70 | 28 | 1 | 4 | PTS_YM_BIA_001_240 (4) |
| Stop | 15 | 50908 | 49911 | 41 | 8 | 948 | PTS_NQ_SBO_001_15 (208), PTS_NQ_TFM_003_15 (196), PTS_NQ_TFU_002_15 (112), PTS_NQ_TFU_003_15 (86) |
| Stop | 30 | 11889 | 11627 | 3 | 16 | 243 | PTS_NQ_PCH_003_30 (92), PTS_GC_TFU_001_30 (70), PTS_NQ_PCH_004_30 (43), PTS_NQ_TFM_006_30 (23) |
| Stop | 60 | 17186 | 16285 | 388 | 54 | 459 | PTS_NQ_TFU_004_60 (122), PTS_YM_TFU_001_60 (96), PTS_NQ_TFU_005_60 (82), PTS_NQ_TFM_011_60 (41) |
| Stop | 240 | 6821 | 5489 | 933 | 21 | 378 | PTS_GC_PCH_004_240 (66), PTS_SB_TFM_001_240 (54), PTS_YM_SBO_001_240 (48), PTS_GC_PCH_005_240 (24) |
| Stop | 1440 | 1170 | 460 | 479 | 5 | 226 | PTS_FDAX_TFU_002_1440 (34), PTS_ES_TFU_001_1440 (33), PTS_NQ_TFU_006_1440 (31), PTS_FDAX_TFU_001_1440 (29) |

### 9b. Trade cBot nati da un intent arrivato in ritardo di almeno una barra

Trade cBot con intent risolto: 3751 su 3779; nati da intent in ritardo ≥ 1 barra: 169, netto/ctr 145,769; di questi senza corrispondente interno: 18.

| strategia | trade da intent stantio | netto/ctr | non abbinati |
|---|---:|---:|---:|
| PTS_ES_BSW_003_15 | 37 | 9,262 | 4 |
| PTS_GC_PCH_004_240 | 19 | -1,764 | 4 |
| PTS_GC_PCH_005_240 | 11 | -1,047 | 4 |
| PTS_ES_BSW_002_15 | 11 | 36,492 | 1 |
| PTS_NQ_TFU_006_1440 | 9 | 22,604 | 2 |
| PTS_ES_TFU_001_1440 | 8 | -360 | 0 |
| PTS_ES_TFM_002_1440 | 8 | 1,652 | 1 |
| PTS_ES_BSW_001_60 | 7 | 12,213 | 0 |
| PTS_NQ_TFU_007_1440 | 6 | 7,882 | 0 |
| PTS_SB_TFM_001_240 | 6 | -4,364 | 5 |
| PTS_NQ_TFM_013_1440 | 4 | 15,475 | 0 |
| PTS_BTC_BIA_001_60 | 4 | -1,130 | 2 |
| PTS_ES_PCH_003_1440 | 4 | -2,277 | 0 |
| PTS_BTC_PCH_001_240 | 3 | -5,194 | 1 |
| PTS_NQ_SBO_005_1440 | 3 | 16,523 | 0 |

Trade abbinati in cui il cBot esce per stop più di 5 minuti PRIMA dell'uscita interna (che non era uno stop): 16, Δ netto/ctr -96,320. Per strategia: PTS_KC_SBO_001_240 5, PTS_GC_PCH_004_240 4, PTS_NQ_TFM_006_30 1, PTS_FDAX_SBO_001_240 1, PTS_BTC_BIA_001_60 1, PTS_ES_SBO_002_240 1, PTS_BTC_TFU_002_60 1, PTS_CC_PCH_001_240 1.

### 9c. Scostamenti fra trades.json e l'export cTrader (esempi)

Trade con prezzo di uscita diverso dall'export: 198; differenza mediana in punti 0.55; per simbolo: NQ 136, FDAX 34, BTC 12, GC 6, YM 6, ES 4.

| strategia | uscita trades.json | prezzo trades.json | prezzo export | lordo trades.json | lordo export | evento export |
|---|---|---:|---:|---:|---:|---|
| PTS_FDAX_PCH_001_240 | 2025-09-11 07:36:49 | 23585.75 | 23586.75 | -31.94 | -34.85 | Stop Loss Hit |
| PTS_FDAX_VBO_001_240 | 2025-09-11 07:36:53 | 23586.75 | 23585.75 | -34.85 | -31.94 | Stop Loss Hit |
| PTS_NQ_TFU_008_240 | 2025-09-23 17:30:27 | 24617.45 | 24617.05 | -51.2 | -52 | Stop Loss Hit |
| PTS_NQ_TFM_014_240 | 2025-09-23 17:30:27 | 24617.05 | 24617.45 | -52 | -51.2 | Stop Loss Hit |
| PTS_FDAX_TFU_001_1440 | 2025-09-25 07:03:40 | 23614.06 | 23613.06 | -233.06 | -235.97 | Stop Loss Hit |
| PTS_FDAX_TFU_002_1440 | 2025-09-25 07:03:41 | 23613.06 | 23614.06 | -235.97 | -233.06 | Stop Loss Hit |
| PTS_NQ_PCH_008_240 | 2025-10-02 13:40:02 | 24857.15 | 24857.65 | 335 | 336 | Stop Loss Hit |
| PTS_NQ_PCH_004_30 | 2025-10-02 13:40:02 | 24857.65 | 24857.15 | 336 | 335 | Stop Loss Hit |
| PTS_NQ_TFM_011_60 | 2025-10-03 17:03:14 | 24838.65 | 24837.75 | -251.4 | -253.2 | Stop Loss Hit |
| PTS_NQ_TFM_010_60 | 2025-10-03 17:03:14 | 24837.75 | 24838.65 | -253.2 | -251.4 | Stop Loss Hit |

Posizioni dell'export senza trade in trades.json: 10 — chiuse fra 2026-09-05 23:59 e 2026-09-05 23:59, lordo totale 16.04.

## 8. Avvisi ed errori nel log del cBot (messaggi normalizzati, prime 25)


Ingressi scartati per classe:
-    6660  lato sbagliato
-      46  slippage market

Ingressi annullati per classe:
-      32  posizione già aperta

`Nessun intent per l'account` per motivo:
-   20612  già reclamati dal conto '17188650'
-   20437  l'account ha già un ingresso in corso per quella strategia su quel simbolo e lato

Ingressi scartati `lato sbagliato` per strategia (prime 15):
-     940  PTS_NQ_TFU_002_15
-     682  PTS_BTC_TFU_001_60
-     466  PTS_GC_TFU_001_30
-     423  PTS_NQ_TFM_007_30
-     373  PTS_ES_SBO_001_15
-     339  PTS_NQ_TFU_003_15
-     339  PTS_BTC_TFU_002_60
-     306  PTS_NQ_TFU_001_15
-     269  PTS_NQ_TFU_004_60
-     209  PTS_NQ_TFM_009_60
-     206  PTS_NQ_TFM_008_30
-     182  PTS_NQ_TFM_001_60
-     157  PTS_YM_TFU_001_60
-     136  PTS_BTC_BIA_001_60
-      93  PTS_BP_TFM_002_15

