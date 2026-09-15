# compare-0042 — cBot cTrader contro backtest interno (feed FTMO)

Generato: 2026-09-13 10:29Z. Finestra di sovrapposizione usata per il confronto trade: `2025-08-31` → `2026-08-05` (ingressi).

## 0. Perimetro dei due run

| | interno | cBot |
|---|---|---|
| trade totali | 3557 | 3438 |
| primo ingresso | 2025-08-31 06:33 | 2025-08-31 06:33 |
| ultimo ingresso | 2026-08-30 23:45 | 2026-08-04 08:02 |
| trade nella sovrapposizione | 3338 | 3438 |
| contratti per trade | 1 valori: 1 | 2 valori: 0.1,0.096 |
| netto totale | -42,926 | -19,649 |
| netto per contratto, sovrapposizione | -10,298 | -196,183 |
| lordo per contratto, sovrapposizione | 3,054 | -104,392 |
| commissioni per contratto, sovrapposizione | 13,352 | 85,618 |
| swap, sovrapposizione (grezzo) | 0 | -616 |

Log cBot: 0 righe; chiusure 0, fill con spread 0, ingressi scartati 0, annullati 0, bracket riancorati 0, chiusure per flat weekend 0, righe non interpretate 0.
Chiusure del log senza trade corrispondente: 0; fill senza trade: 0.

### 0b. Commissioni per contratto e per trade (sovrapposizione)

| simbolo | int trade | int comm/trade/ctr | cBot trade | cBot comm/trade/ctr | cBot swap/trade/ctr | cBot lordo/ctr | cBot netto/ctr |
|---|---:|---:|---:|---:|---:|---:|---:|
| BP | 18 | 4.0 | 14 | 5.0 | -26.7 | 8,016 | 7,573 |
| BTC | 600 | 4.0 | 615 | 23.6 | -1.9 | 9,652 | -6,013 |
| CC | 10 | 4.0 | 10 | 4.0 | 0.0 | 18,012 | 17,972 |
| CL | 33 | 4.0 | 33 | 5.2 | 0.2 | -1,513 | -1,679 |
| CT | 12 | 4.0 | 10 | 2.8 | 0.0 | 6,130 | 6,102 |
| ES | 350 | 4.0 | 333 | 23.0 | 0.0 | -88,745 | -96,409 |
| FDAX | 127 | 4.0 | 123 | 45.6 | 0.0 | -49,921 | -55,529 |
| GC | 463 | 4.0 | 481 | 26.8 | -4.2 | -5,814 | -20,730 |
| NG | 235 | 4.0 | 352 | 1.8 | 0.0 | -151,520 | -152,154 |
| NQ | 1066 | 4.0 | 1072 | 35.4 | -2.3 | 231,020 | 190,658 |
| SB | 20 | 4.0 | 18 | 1.2 | 0.0 | -6,619 | -6,641 |
| YM | 404 | 4.0 | 377 | 16.0 | -0.6 | -73,090 | -79,334 |

FDAX: rapporto lordo cBot / lordo interno sulle coppie con stessa uscita (n=82): mediana 1.171 — il conto cTrader è in USD e GER40 quota in EUR, l'interno usa 25 EUR/punto senza conversione.

## 1. Differenze di trade per strategia (finestra di sovrapposizione)

Match = stessa strategia, stesso verso, ingresso entro una barra della strategia. Netto per contratto future.

| strategia | tf | int | cbot | match | solo int | solo cbot | Δentry mediano (min) | netto/ctr int | netto/ctr cbot | Δ netto/ctr | int in match (netto) | cbot in match (netto) | int in match (lordo) | cbot in match (lordo) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 60 | 13 | 9 | 7 | 6 | 2 | 0.3 | 1,879 | 6,641 | 4,763 | 4,372 | 8,172 | 4,400 | 8,503 |
| PTS_BP_TFM_002_15 | 15 | 5 | 5 | 5 | 0 | 0 | 0.5 | 980 | 931 | -49 | 980 | 931 | 1,000 | 1,016 |
| PTS_BTC_BIA_001_60 | 60 | 290 | 303 | 160 | 130 | 143 | 0.4 | 15,418 | 1,283 | -14,135 | 8,360 | -22,373 | 9,000 | -18,597 |
| PTS_BTC_PCH_001_240 | 240 | 223 | 223 | 221 | 2 | 2 | 0.4 | -4,151 | -14,610 | -10,458 | -643 | -10,936 | 241 | -4,933 |
| PTS_BTC_TFU_001_60 | 60 | 36 | 37 | 36 | 0 | 1 | 0.5 | 6,356 | 4,533 | -1,823 | 6,356 | 4,861 | 6,500 | 5,710 |
| PTS_BTC_TFU_002_60 | 60 | 51 | 52 | 50 | 1 | 2 | 0.4 | 12,046 | 2,781 | -9,265 | 12,300 | 3,368 | 12,500 | 4,548 |
| PTS_CC_SBO_001_60 | 60 | 10 | 10 | 9 | 1 | 1 | 0.1 | 17,862 | 17,972 | 110 | 19,616 | 19,765 | 19,652 | 19,801 |
| PTS_CL_MAC_001_30 | 30 | 33 | 33 | 33 | 0 | 0 | 0.0 | -950 | -1,679 | -729 | -950 | -1,679 | -818 | -1,513 |
| PTS_CT_TFU_001_240 | 240 | 12 | 10 | 6 | 6 | 4 | -25.1 | -6,868 | 6,102 | 12,970 | -5,269 | -2 | -5,245 | 15 |
| PTS_ES_BSW_001_60 | 60 | 18 | 18 | 18 | 0 | 0 | 0.0 | 22,825 | 22,506 | -320 | 22,825 | 22,506 | 22,897 | 22,920 |
| PTS_ES_BSW_002_15 | 15 | 14 | 14 | 14 | 0 | 0 | 0.0 | 2,944 | 10,197 | 7,253 | 2,944 | 10,197 | 3,000 | 10,519 |
| PTS_ES_BSW_003_15 | 15 | 39 | 38 | 38 | 1 | 0 | 0.0 | 16,678 | 8,753 | -7,925 | 9,182 | 8,753 | 9,334 | 9,627 |
| PTS_ES_PCH_001_60 | 60 | 21 | 20 | 19 | 2 | 1 | 0.2 | 7,747 | -5 | -7,751 | -158 | -554 | -82 | -117 |
| PTS_ES_PCH_002_60 | 60 | 35 | 36 | 34 | 1 | 2 | -0.7 | -1,520 | -1,261 | 259 | -1,516 | -1,200 | -1,380 | -418 |
| PTS_ES_PCH_003_1440 | 1440 | 12 | 11 | 11 | 1 | 0 | 0.3 | -7,549 | -7,966 | -417 | -7,465 | -7,966 | -7,421 | -7,714 |
| PTS_ES_PCH_004_240 | 240 | 82 | 79 | 73 | 9 | 6 | -0.2 | -30,328 | -57,705 | -27,377 | -37,792 | -52,473 | -37,500 | -50,794 |
| PTS_ES_SBO_001_15 | 15 | 7 | 6 | 6 | 1 | 0 | 0.6 | 11,485 | 5,339 | -6,147 | 5,489 | 5,339 | 5,513 | 5,477 |
| PTS_ES_SBO_002_240 | 240 | 15 | 14 | 12 | 3 | 2 | 0.7 | 604 | -11,841 | -12,444 | -1,040 | -17,794 | -992 | -17,518 |
| PTS_ES_SBO_003_240 | 240 | 18 | 17 | 15 | 3 | 2 | 0.5 | -16,385 | -21,456 | -5,071 | -24,373 | -23,411 | -24,313 | -23,066 |
| PTS_ES_TFM_001_1440 | 1440 | 13 | 12 | 12 | 1 | 0 | 0.4 | 948 | 1,649 | 701 | 1,952 | 1,649 | 2,000 | 1,928 |
| PTS_ES_TFM_002_1440 | 1440 | 32 | 30 | 30 | 2 | 0 | 0.3 | -10,063 | -6,268 | 3,796 | -8,055 | -6,268 | -7,935 | -5,578 |
| PTS_ES_TFU_001_1440 | 1440 | 44 | 38 | 38 | 6 | 0 | 0.2 | -42,444 | -38,351 | 4,093 | -38,420 | -38,351 | -38,268 | -37,475 |
| PTS_FDAX_MAC_001_240 | 240 | 10 | 10 | 10 | 0 | 0 | 0.0 | -21,325 | -21,493 | -168 | -21,325 | -21,493 | -21,285 | -21,037 |
| PTS_FDAX_SBO_002_1440 | 1440 | 14 | 15 | 14 | 0 | 1 | 0.5 | 1,944 | 140 | -1,804 | 1,944 | 1,359 | 2,000 | 1,997 |
| PTS_FDAX_TFU_001_1440 | 1440 | 49 | 47 | 43 | 6 | 4 | 0.2 | -7,374 | -10,316 | -2,942 | -1,350 | -4,053 | -1,178 | -2,092 |
| PTS_FDAX_TFU_002_1440 | 1440 | 54 | 51 | 45 | 9 | 6 | 0.2 | -19,394 | -23,860 | -4,466 | -17,358 | -15,090 | -17,178 | -13,038 |
| PTS_GC_PCH_001_60 | 60 | 22 | 23 | 22 | 0 | 1 | 0.6 | -5,838 | -9,094 | -3,256 | -5,838 | -6,804 | -5,750 | -6,214 |
| PTS_GC_PCH_004_240 | 240 | 261 | 269 | 256 | 5 | 13 | 0.4 | 65,711 | 21,606 | -44,105 | 54,815 | 13,034 | 55,839 | 20,144 |
| PTS_GC_PCH_005_240 | 240 | 79 | 87 | 74 | 5 | 13 | 0.4 | -6,979 | -24,791 | -17,812 | 1,596 | -14,128 | 1,892 | -11,132 |
| PTS_GC_RHL_001_60 | 60 | 31 | 32 | 31 | 0 | 1 | 0.4 | 4,411 | 3,341 | -1,070 | 4,411 | 3,538 | 4,535 | 4,452 |
| PTS_GC_RHL_002_60 | 60 | 30 | 30 | 30 | 0 | 0 | 0.6 | 7,383 | 6,040 | -1,343 | 7,383 | 6,040 | 7,503 | 6,844 |
| PTS_GC_TFM_001_240 | 240 | 8 | 8 | 8 | 0 | 0 | 0.4 | -15,032 | -15,345 | -313 | -15,032 | -15,345 | -15,000 | -15,122 |
| PTS_GC_TFU_001_30 | 30 | 32 | 32 | 32 | 0 | 0 | 0.3 | -628 | -2,488 | -1,860 | -628 | -2,488 | -500 | -1,298 |
| PTS_NG_BSW_001_30 | 30 | 15 | 15 | 15 | 0 | 0 | 0.2 | -5,560 | -1,397 | 4,163 | -5,560 | -1,397 | -5,500 | -1,370 |
| PTS_NG_TFM_001_240 | 240 | 33 | 36 | 19 | 14 | 17 | 0.2 | -32,732 | -15,835 | 16,897 | -27,256 | -14,424 | -27,180 | -14,390 |
| PTS_NG_TFM_002_240 | 240 | 32 | 41 | 25 | 7 | 16 | 0.3 | -8,128 | -7,604 | 524 | -6,350 | -7,915 | -6,250 | -7,870 |
| PTS_NG_TFM_003_30 | 30 | 16 | 44 | 14 | 2 | 30 | 0.6 | -12,064 | -22,099 | -10,035 | -10,556 | -6,855 | -10,500 | -6,830 |
| PTS_NG_TFM_004_60 | 60 | 6 | 15 | 5 | 1 | 10 | 0.3 | 2,086 | 973 | -1,113 | 3,590 | 4,031 | 3,610 | 4,040 |
| PTS_NG_TFM_005_30 | 30 | 39 | 59 | 28 | 11 | 31 | 0.3 | -20,706 | -17,076 | 3,630 | -12,582 | -4,370 | -12,470 | -4,320 |
| PTS_NG_TFM_006_60 | 60 | 37 | 60 | 26 | 11 | 34 | 0.4 | -15,218 | -35,538 | -20,320 | -15,564 | -16,587 | -15,460 | -16,540 |
| PTS_NG_TFU_001_240 | 240 | 16 | 20 | 16 | 0 | 4 | 0.2 | -12,274 | -10,626 | 1,648 | -12,274 | -12,089 | -12,210 | -12,060 |
| PTS_NG_TFU_002_240 | 240 | 23 | 27 | 23 | 0 | 4 | 0.2 | -23,112 | -16,919 | 6,193 | -23,112 | -17,871 | -23,020 | -17,830 |
| PTS_NG_TFU_003_60 | 60 | 18 | 35 | 8 | 10 | 27 | 0.2 | -19,082 | -26,033 | -6,951 | -12,032 | -8,034 | -12,000 | -8,020 |
| PTS_NQ_PCH_003_30 | 30 | 64 | 65 | 58 | 6 | 7 | 0.3 | 16,347 | 13,666 | -2,681 | 591 | -5,793 | 823 | -3,740 |
| PTS_NQ_PCH_004_30 | 30 | 45 | 45 | 43 | 2 | 2 | 0.3 | 25,237 | 22,677 | -2,559 | 21,082 | 18,758 | 21,254 | 20,280 |
| PTS_NQ_PCH_007_240 | 240 | 19 | 19 | 19 | 0 | 0 | 0.3 | -18,247 | -19,899 | -1,652 | -18,247 | -19,899 | -18,171 | -19,226 |
| PTS_NQ_PCH_008_240 | 240 | 46 | 46 | 45 | 1 | 1 | 0.2 | 20,479 | 7,992 | -12,487 | 10,483 | 10,085 | 10,663 | 12,061 |
| PTS_NQ_RBM_001_15 | 15 | 152 | 136 | 114 | 38 | 22 | 0.7 | 15,990 | 40,787 | 24,798 | 30,456 | 39,957 | 30,912 | 43,987 |
| PTS_NQ_SBO_001_15 | 15 | 33 | 35 | 32 | 1 | 3 | 0.3 | -5,511 | -8,478 | -2,967 | -8,507 | -9,360 | -8,379 | -8,227 |
| PTS_NQ_SBO_002_15 | 15 | 23 | 24 | 23 | 0 | 1 | 0.4 | 5,408 | 727 | -4,681 | 5,408 | 4,769 | 5,500 | 5,583 |
| PTS_NQ_SBO_003_15 | 15 | 34 | 35 | 33 | 1 | 2 | 0.3 | 15,185 | 12,824 | -2,361 | 15,689 | 13,955 | 15,821 | 15,123 |
| PTS_NQ_SBO_004_60 | 60 | 17 | 18 | 16 | 1 | 2 | 0.3 | -8,568 | -10,144 | -1,576 | -8,064 | -9,017 | -8,000 | -8,451 |
| PTS_NQ_SBO_005_1440 | 1440 | 12 | 11 | 11 | 1 | 0 | 0.2 | 10,832 | 11,447 | 615 | 11,836 | 11,447 | 11,880 | 11,831 |
| PTS_NQ_TFM_001_60 | 60 | 17 | 17 | 17 | 0 | 0 | 0.3 | -1,068 | -1,703 | -635 | -1,068 | -1,703 | -1,000 | -1,101 |
| PTS_NQ_TFM_002_15 | 15 | 35 | 35 | 34 | 1 | 1 | 0.4 | 7,515 | 5,148 | -2,367 | 3,519 | 1,171 | 3,655 | 2,375 |
| PTS_NQ_TFM_003_15 | 15 | 38 | 38 | 34 | 4 | 4 | 0.2 | 30,848 | 30,270 | -578 | 19,864 | 19,372 | 20,000 | 20,958 |
| PTS_NQ_TFM_005_15 | 15 | 41 | 41 | 39 | 2 | 2 | 0.4 | -6,573 | -7,835 | -1,262 | -12,565 | -13,794 | -12,409 | -12,413 |
| PTS_NQ_TFM_006_30 | 30 | 27 | 27 | 25 | 2 | 2 | 0.2 | 7,392 | -4,289 | -11,681 | 8,400 | -3,102 | 8,500 | -2,217 |
| PTS_NQ_TFM_007_30 | 30 | 20 | 20 | 20 | 0 | 0 | 0.3 | 12,690 | 12,389 | -301 | 12,690 | 12,389 | 12,770 | 13,097 |
| PTS_NQ_TFM_010_60 | 60 | 31 | 32 | 31 | 0 | 1 | 0.4 | -25,124 | -22,155 | 2,969 | -25,124 | -27,127 | -25,000 | -25,647 |
| PTS_NQ_TFM_011_60 | 60 | 33 | 34 | 32 | 1 | 2 | 0.4 | -14,662 | -6,884 | 7,779 | -13,408 | -14,815 | -13,280 | -13,682 |
| PTS_NQ_TFM_012_1440 | 1440 | 9 | 8 | 8 | 1 | 0 | 0.7 | 6,964 | 7,631 | 667 | 7,968 | 7,631 | 8,000 | 7,914 |
| PTS_NQ_TFM_013_1440 | 1440 | 9 | 9 | 9 | 0 | 0 | 0.2 | 10,513 | 10,250 | -264 | 10,513 | 10,250 | 10,549 | 10,568 |
| PTS_NQ_TFM_015_240 | 240 | 17 | 19 | 17 | 0 | 2 | 0.7 | 1,932 | 10,614 | 8,682 | 1,932 | 1,204 | 2,000 | 1,806 |
| PTS_NQ_TFU_001_15 | 15 | 32 | 32 | 31 | 1 | 1 | 0.2 | 35,527 | 34,441 | -1,086 | 30,531 | 29,462 | 30,655 | 30,687 |
| PTS_NQ_TFU_002_15 | 15 | 43 | 44 | 41 | 2 | 3 | 0.1 | -17,253 | -22,231 | -4,978 | -27,596 | -29,337 | -27,432 | -27,885 |
| PTS_NQ_TFU_003_15 | 15 | 49 | 50 | 47 | 2 | 3 | 0.2 | 16,082 | 18,051 | 1,969 | 13,552 | 13,020 | 13,740 | 14,812 |
| PTS_NQ_TFU_004_60 | 60 | 33 | 34 | 32 | 1 | 2 | 0.2 | -3,382 | 4,380 | 7,762 | -2,628 | -4,802 | -2,500 | -3,158 |
| PTS_NQ_TFU_005_60 | 60 | 49 | 48 | 44 | 5 | 4 | 0.3 | 13,658 | -4,574 | -18,232 | -2,322 | -6,388 | -2,146 | -4,320 |
| PTS_NQ_TFU_006_1440 | 1440 | 43 | 42 | 42 | 1 | 0 | 0.3 | 36,828 | 31,147 | -5,681 | 37,832 | 31,147 | 38,000 | 32,634 |
| PTS_NQ_TFU_007_1440 | 1440 | 15 | 15 | 15 | 0 | 0 | 0.1 | 5,940 | 5,429 | -511 | 5,940 | 5,429 | 6,000 | 5,960 |
| PTS_NQ_TFU_008_240 | 240 | 54 | 56 | 54 | 0 | 2 | 0.2 | -7,216 | -10,980 | -3,764 | -7,216 | -9,800 | -7,000 | -7,889 |
| PTS_NQ_VBO_001_1440 | 1440 | 6 | 7 | 6 | 0 | 1 | 0.5 | -6,024 | -5,657 | 367 | -6,024 | -6,287 | -6,000 | -6,074 |
| PTS_NQ_VBO_002_240 | 240 | 20 | 30 | 19 | 1 | 11 | 0.5 | 30,105 | 35,616 | 5,511 | 22,609 | 27,911 | 22,685 | 28,583 |
| PTS_SB_TFM_001_240 | 240 | 20 | 18 | 10 | 10 | 8 | 0.4 | -4,112 | -6,641 | -2,529 | -2,784 | -3,126 | -2,744 | -3,114 |
| PTS_YM_BIA_001_240 | 240 | 82 | 81 | 81 | 1 | 0 | 0.0 | 11 | -6,421 | -6,431 | -4,985 | -6,421 | -4,661 | -5,091 |
| PTS_YM_SBO_001_240 | 240 | 35 | 33 | 33 | 2 | 0 | 0.2 | -8,108 | -8,905 | -797 | -7,600 | -8,905 | -7,468 | -8,377 |
| PTS_YM_SBO_002_240 | 240 | 32 | 31 | 31 | 1 | 0 | 0.2 | 4,870 | 1,403 | -3,466 | 1,874 | 1,403 | 1,998 | 1,899 |
| PTS_YM_TFM_001_240 | 240 | 7 | 7 | 7 | 0 | 0 | 0.2 | -4,528 | -4,617 | -89 | -4,528 | -4,617 | -4,500 | -4,505 |
| PTS_YM_TFM_002_240 | 240 | 25 | 23 | 23 | 2 | 0 | 0.3 | -8,062 | -3,901 | 4,161 | -5,554 | -3,901 | -5,462 | -3,533 |
| PTS_YM_TFM_003_240 | 240 | 76 | 71 | 62 | 14 | 9 | 0.2 | -8,979 | -11,487 | -2,508 | -9,258 | -5,254 | -9,010 | -4,207 |
| PTS_YM_TFU_001_60 | 60 | 56 | 46 | 42 | 14 | 4 | 0.3 | -17,416 | -17,788 | -372 | -17,031 | -14,213 | -16,863 | -13,452 |
| PTS_YM_TFU_002_60 | 60 | 33 | 29 | 27 | 6 | 2 | 0.2 | -27,426 | -22,937 | 4,489 | -21,402 | -20,883 | -21,294 | -20,417 |
| PTS_YM_TFU_003_60 | 60 | 58 | 56 | 56 | 2 | 0 | 0.3 | -1,992 | -4,682 | -2,690 | -1,484 | -4,682 | -1,260 | -3,786 |
| **TOTALE** | | 3338 | 3438 | 2954 | 384 | 484 | | -10,298 | -196,183 | -185,885 | -79,009 | -212,269 | -67,193 | -129,696 |

Trade abbinati: 2954. Scarto di ingresso: ≤1 min 2632, ≤5 min 2787, ≤15 min 2862, oltre 92.
Prezzo di ingresso cBot peggiore dell'interno (punti, positivo = cBot paga di più): long mediana -0.54 media 0.7095 (n=1967); short mediana -0.3 media 0.2963 (n=987).

### 1b. Perché un trade interno non esiste sul cBot

| causa | trade | netto/ctr interno di quei trade |
|---|---:|---:|
| nessun evento nel log (livello non toccato o intent non consegnato) | 192 | 49,818 |
| cBot già in posizione (nessuna inversione) | 192 | 18,893 |

Uscite per segnale opposto nell'interno (inversione che il cBot non fa): 95 trade chiusi così, netto/ctr -10,920.

Trade solo cBot: 484, di cui 55 mentre l'interno era già in posizione sulla stessa strategia; netto/ctr dei solo-cBot 16,086.

## 2. Uscite

### 2a. Motivo di uscita dichiarato in trades.json

| interno | n | | cBot | n |
|---|---:|---|---|---:|
| StopLoss | 2014 | | LocalExit:StopLoss | 2634 |
| TakeProfit | 390 | | LocalExit:TakeProfit | 420 |
| TrailingStop | 320 | | LocalExit:Closed | 271 |
| BreakEven | 209 | | BrokerExit:TakeProfit | 75 |
| MaxBars | 160 | | BrokerExit:StopLoss | 38 |
| TimeExit | 150 | |  |  |
| OppositeSignal | 95 | |  |  |

### 2b. Esito reale dal log del cBot (riga `Chiuso`)

| esito log | n | netto/ctr | di cui trades.json=Closed | StopLoss | TakeProfit |
|---|---:|---:|---:|---:|---:|
| (senza riga Chiuso) | 3438 | -196,183 | 271 | 2672 | 495 |

### 2c. Trade abbinati: motivo interno × esito cBot

| interno \ cBot | BrokerExit:StopLoss | BrokerExit:TakeProfit | LocalExit:Closed | LocalExit:StopLoss | LocalExit:TakeProfit | tot |
|---|---:|---:|---:|---:|---:|---:|
| BreakEven | 0 | 0 | 0 | 197 | 0 | 197 |
| MaxBars | 9 | 35 | 89 | 6 | 2 | 141 |
| OppositeSignal | 0 | 3 | 9 | 47 | 18 | 77 |
| StopLoss | 13 | 12 | 2 | 1710 | 8 | 1745 |
| TakeProfit | 0 | 1 | 0 | 7 | 335 | 343 |
| TimeExit | 1 | 5 | 130 | 0 | 2 | 138 |
| TrailingStop | 0 | 0 | 0 | 312 | 1 | 313 |

Impatto sui trade abbinati (netto/ctr cBot − interno) per coppia di esiti, prime 12 per valore assoluto:

| interno | cBot | n | Δ netto/ctr totale | Δ medio | Δ uscita mediano (min) |
|---|---|---:|---:|---:|---:|
| StopLoss | LocalExit:StopLoss | 1710 | -99,654 | -58 | 0 |
| OppositeSignal | LocalExit:TakeProfit | 18 | 90,900 | 5,050 | 1965 |
| OppositeSignal | LocalExit:StopLoss | 47 | -84,814 | -1,805 | 301 |
| TakeProfit | LocalExit:StopLoss | 7 | -72,006 | -10,287 | -762 |
| StopLoss | LocalExit:TakeProfit | 8 | 46,386 | 5,798 | 2607 |
| MaxBars | LocalExit:StopLoss | 6 | -27,257 | -4,543 | 85 |
| StopLoss | BrokerExit:StopLoss | 13 | -19,131 | -1,472 | 2355 |
| StopLoss | BrokerExit:TakeProfit | 12 | 17,566 | 1,464 | 3719 |
| OppositeSignal | LocalExit:Closed | 9 | 15,156 | 1,684 | 1075 |
| TakeProfit | LocalExit:TakeProfit | 335 | 11,866 | 35 | 0 |
| BreakEven | LocalExit:StopLoss | 197 | -11,659 | -59 | 1 |
| TrailingStop | LocalExit:StopLoss | 312 | -11,323 | -36 | 1 |

### 2d. Stop loss: perdita realizzata contro distanza dichiarata (per contratto, lordo)

Solo trade usciti per stop (interno `StopLoss`; cBot esito `StopLoss` senza trailing). Rapporto = perdita lorda / stop dichiarato (allargato dove previsto). >1 = stop eseguito oltre il livello (gap, spread sugli short).

| simbolo | int n | int rapporto mediano | int oltre 1,1 | cBot n | cBot rapporto mediano | cBot oltre 1,1 | cBot short mediano | cBot long mediano |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| BP | 6 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| BTC | 383 | 1.00 | 1 | 0 | NaN | 0 | NaN | NaN |
| CC | 6 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| CL | 18 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| CT | 3 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| ES | 109 | 1.00 | 1 | 0 | NaN | 0 | NaN | NaN |
| FDAX | 109 | 1.00 | 2 | 0 | NaN | 0 | NaN | NaN |
| GC | 345 | 1.00 | 0 | 0 | NaN | 0 | NaN | NaN |
| NG | 185 | 1.00 | 6 | 0 | NaN | 0 | NaN | NaN |
| NQ | 561 | 1.00 | 6 | 0 | NaN | 0 | NaN | NaN |
| SB | 0 | NaN | 0 | 0 | NaN | 0 | NaN | NaN |
| YM | 255 | 1.00 | 1 | 0 | NaN | 0 | NaN | NaN |

### 2f. Trade abbinati: quanto distano le uscite

| |Δ uscita| | n | Σ Δ lordo/ctr (cBot − int) | Σ Δ netto/ctr | Δ netto medio |
|---|---:|---:|---:|---:|
| ≤ 1 min | 2208 | -45,109 | -101,316 | -46 |
| 1–5 min | 284 | -4,621 | -11,132 | -39 |
| 5–60 min | 185 | -14,140 | -17,690 | -96 |
| 1–24 h | 198 | -44,262 | -48,012 | -242 |
| > 24 h | 79 | 45,628 | 44,890 | 568 |

Nota: per le chiusure che il cBot scopre in ritardo (`BrokerExit`), l'esito nel log è dedotto dal segno del netto (`DeduceCloseReason`), quindi `MaxBars → TakeProfit` con Δ uscita 0 è la stessa chiusura etichettata in due modi.

### 2g. Riempimenti dell'interno su un minuto senza barra nel feed

Ingresso/uscita a un istante in cui `@SYM_1.json` non ha una barra: il prezzo viene dal mark-to-market, non dal mercato. Uscite tecniche (TimeExit, MaxBars, SessionFlat, WeekEnd, EndOfRun) escluse dal conteggio delle uscite.

| strategia | ingressi senza barra / tot | netto/ctr di quei trade | uscite SL/TP/trailing/BE senza barra | esempio |
|---|---:|---:|---:|---|
| **TOTALE** | 0 | 0 | | |

Per confronto, ingressi cBot il cui minuto non ha una barra nel feed raccolto: 2 su 3438 (il cBot esegue sui tick di cTrader, il feed è la raccolta a barre dello stesso broker).

Bracket riancorato al fill (ingresso slittato rispetto al livello): 0 trade; slippage mediano NaN punti, costo totale per contratto 0.

## 3. Finestra operativa (TradingWindow) e giorno saltato

Per ogni trade si ricava la barra di segnale (la barra della strategia che precede quella del fill) e si etichetta come fa il motore (`BarLabelTime` sull'orologio della finestra). Fuori finestra = un ingresso che la strategia non avrebbe dovuto emettere.

| strategia | finestra | fuso | int fuori/tot | cBot fuori/tot | int giorno saltato | cBot giorno saltato | fill fuori griglia int/cBot |
|---|---|---|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 20:00-19:00 | Research | 0/13 | 0/9 | 0 | 0 | 0/0 |
| PTS_BP_TFM_002_15 | 23:00-22:00 | Research | 0/5 | 0/5 | 0 | 0 | 0/0 |
| PTS_BTC_BIA_001_60 | 00:00-24:00 | Research | 0/290 | 0/303 | 0 | 0 | 0/1 |
| PTS_BTC_PCH_001_240 | 06:00-24:00 | Research | 0/223 | 0/223 | 0 | 0 | 0/0 |
| PTS_BTC_TFU_001_60 | 04:00-03:00 | Research | 0/36 | 0/37 | 0 | 0 | 0/0 |
| PTS_BTC_TFU_002_60 | 04:00-03:00 | Research | 0/51 | 0/52 | 0 | 1 | 0/1 |
| PTS_CC_SBO_001_60 | 11:00-19:00 | Research | 0/10 | 0/10 | 0 | 0 | 0/0 |
| PTS_CL_MAC_001_30 | 00:00-24:00 | Research | 0/33 | 0/33 | 0 | 0 | 0/0 |
| PTS_CT_TFU_001_240 | 00:00-24:00 | Research | 0/12 | 0/10 | 0 | 0 | 0/0 |
| PTS_ES_BSW_001_60 | 00:00-24:00 | Research | 0/18 | 0/18 | 0 | 0 | 0/0 |
| PTS_ES_BSW_002_15 | 00:00-24:00 | Research | 0/14 | 0/14 | 0 | 0 | 0/0 |
| PTS_ES_BSW_003_15 | 00:00-24:00 | Research | 0/39 | 0/38 | 0 | 0 | 0/0 |
| PTS_ES_PCH_001_60 | 03:00-02:00 | Research | 0/21 | 0/20 | 0 | 0 | 0/0 |
| PTS_ES_PCH_002_60 | 08:00-10:00 | Research | 0/35 | 0/36 | 0 | 0 | 0/0 |
| PTS_ES_PCH_003_1440 | 00:00-24:00 | Research | 0/12 | 0/11 | 0 | 0 | 0/0 |
| PTS_ES_PCH_004_240 | 00:00-24:00 | Research | 0/82 | 0/79 | 0 | 0 | 0/0 |
| PTS_ES_SBO_001_15 | 03:00-02:00 | Research | 0/7 | 0/6 | 0 | 0 | 0/0 |
| PTS_ES_SBO_002_240 | 14:00-09:00 | Research | 0/15 | 0/14 | 0 | 0 | 0/0 |
| PTS_ES_SBO_003_240 | 14:00-09:00 | Research | 0/18 | 0/17 | 0 | 0 | 0/0 |
| PTS_ES_TFM_001_1440 | 00:00-24:00 | Research | 0/13 | 0/12 | 0 | 0 | 0/0 |
| PTS_ES_TFM_002_1440 | 00:00-24:00 | Research | 0/32 | 0/30 | 0 | 0 | 0/0 |
| PTS_ES_TFU_001_1440 | 00:00-24:00 | Research | 0/44 | 0/38 | 0 | 0 | 0/0 |
| PTS_FDAX_MAC_001_240 | 00:00-24:00 | Research | 0/10 | 0/10 | 0 | 0 | 0/0 |
| PTS_FDAX_SBO_002_1440 | 00:00-24:00 | Research | 0/14 | 0/15 | 0 | 0 | 0/0 |
| PTS_FDAX_TFU_001_1440 | 00:00-24:00 | Research | 0/49 | 0/47 | 0 | 0 | 0/0 |
| PTS_FDAX_TFU_002_1440 | 00:00-24:00 | Research | 0/54 | 0/51 | 0 | 0 | 0/0 |
| PTS_GC_PCH_001_60 | 06:00-05:00 | Research | 0/22 | 0/23 | 0 | 0 | 0/0 |
| PTS_GC_PCH_004_240 | 00:00-12:00 | Research | 0/261 | 0/269 | 0 | 0 | 0/0 |
| PTS_GC_PCH_005_240 | 00:00-09:00 | Research | 0/79 | 0/87 | 0 | 0 | 0/0 |
| PTS_GC_RHL_001_60 | 13:00-12:00 | Research | 0/31 | 0/32 | 0 | 0 | 0/0 |
| PTS_GC_RHL_002_60 | 13:00-12:00 | Research | 0/30 | 0/30 | 0 | 0 | 0/0 |
| PTS_GC_TFM_001_240 | 10:00-13:00 | Research | 0/8 | 0/8 | 0 | 0 | 0/0 |
| PTS_GC_TFU_001_30 | 16:00-08:00 | Research | 0/32 | 0/32 | 0 | 0 | 0/0 |
| PTS_NG_BSW_001_30 | 00:00-24:00 | Research | 0/15 | 0/15 | 0 | 0 | 0/0 |
| PTS_NG_TFM_001_240 | 10:00-24:00 | Research | 0/33 | 1/36 | 0 | 0 | 0/0 |
| PTS_NG_TFM_002_240 | 00:00-17:00 | Research | 0/32 | 0/41 | 0 | 0 | 0/0 |
| PTS_NG_TFM_003_30 | 22:00-17:00 | Research | 0/16 | 0/44 | 0 | 0 | 0/0 |
| PTS_NG_TFM_004_60 | 02:00-14:00 | Research | 0/6 | 0/15 | 0 | 0 | 0/0 |
| PTS_NG_TFM_005_30 | 03:00-24:00 | Research | 0/39 | 0/59 | 0 | 0 | 0/0 |
| PTS_NG_TFM_006_60 | 00:00-22:00 | Research | 0/37 | 2/60 | 0 | 0 | 0/0 |
| PTS_NG_TFU_001_240 | 14:00-24:00 | Research | 0/16 | 0/20 | 0 | 0 | 0/0 |
| PTS_NG_TFU_002_240 | 14:00-24:00 | Research | 0/23 | 0/27 | 0 | 0 | 0/0 |
| PTS_NG_TFU_003_60 | 08:00-23:00 | Research | 0/18 | 0/35 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_003_30 | 14:00-04:00 | Research | 0/64 | 0/65 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_004_30 | 11:00-10:00 | Research | 0/45 | 0/45 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_007_240 | 10:00-00:00 | Research | 0/19 | 0/19 | 0 | 0 | 0/0 |
| PTS_NQ_PCH_008_240 | 06:00-24:00 | Research | 0/46 | 0/46 | 0 | 0 | 0/0 |
| PTS_NQ_RBM_001_15 | 07:00-06:00 | Research | 0/152 | 0/136 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_001_15 | 05:00-04:00 | Research | 0/33 | 0/35 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_002_15 | 10:00-05:00 | Research | 0/23 | 0/24 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_003_15 | 13:00-06:00 | Research | 0/34 | 0/35 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_004_60 | 22:00-21:00 | Research | 0/17 | 0/18 | 0 | 0 | 0/0 |
| PTS_NQ_SBO_005_1440 | 00:00-24:00 | Research | 0/12 | 0/11 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_001_60 | 16:00-03:00 | Research | 0/17 | 0/17 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_002_15 | 09:00-05:00 | Research | 0/35 | 0/35 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_003_15 | 13:00-05:00 | Research | 0/38 | 0/38 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_005_15 | 18:00-17:00 | Research | 0/41 | 0/41 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_006_30 | 14:00-04:00 | Research | 0/27 | 0/27 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_007_30 | 02:00-01:00 | Research | 0/20 | 0/20 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_010_60 | 00:00-17:00 | Research | 0/31 | 0/32 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_011_60 | 21:00-14:00 | Research | 0/33 | 0/34 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_012_1440 | 00:00-24:00 | Research | 0/9 | 0/8 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_013_1440 | 00:00-24:00 | Research | 0/9 | 0/9 | 0 | 0 | 0/0 |
| PTS_NQ_TFM_015_240 | 18:00-05:00 | Research | 0/17 | 0/19 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_001_15 | 17:00-10:00 | Research | 0/32 | 0/32 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_002_15 | 17:00-07:00 | Research | 0/43 | 0/44 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_003_15 | 17:00-03:00 | Research | 0/49 | 0/50 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_004_60 | 17:00-03:00 | Research | 0/33 | 0/34 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_005_60 | 16:00-04:00 | Research | 0/49 | 0/48 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | Research | 0/43 | 0/42 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_007_1440 | 00:00-24:00 | Research | 0/15 | 0/15 | 0 | 0 | 0/0 |
| PTS_NQ_TFU_008_240 | 06:00-24:00 | Research | 0/54 | 1/56 | 0 | 0 | 0/0 |
| PTS_NQ_VBO_001_1440 | 00:00-24:00 | Research | 0/6 | 0/7 | 0 | 0 | 0/0 |
| PTS_NQ_VBO_002_240 | 00:00-17:00 | Research | 0/20 | 0/30 | 0 | 0 | 0/0 |
| PTS_SB_TFM_001_240 | 13:00-24:00 | Research | 0/20 | 0/18 | 0 | 0 | 0/0 |
| PTS_YM_BIA_001_240 | 00:00-24:00 | Research | 0/82 | 0/81 | 0 | 0 | 0/0 |
| PTS_YM_SBO_001_240 | 14:00-05:00 | Research | 0/35 | 0/33 | 0 | 0 | 0/0 |
| PTS_YM_SBO_002_240 | 14:00-05:00 | Research | 0/32 | 0/31 | 0 | 0 | 0/0 |
| PTS_YM_TFM_001_240 | 06:00-09:00 | Research | 0/7 | 0/7 | 0 | 0 | 0/0 |
| PTS_YM_TFM_002_240 | 06:00-24:00 | Research | 0/25 | 0/23 | 0 | 0 | 0/0 |
| PTS_YM_TFM_003_240 | 00:00-24:00 | Research | 0/76 | 0/71 | 0 | 0 | 0/0 |
| PTS_YM_TFU_001_60 | 19:00-17:00 | Research | 0/56 | 0/46 | 0 | 0 | 0/0 |
| PTS_YM_TFU_002_60 | 12:00-03:00 | Research | 0/33 | 0/29 | 0 | 0 | 0/0 |
| PTS_YM_TFU_003_60 | 01:00-21:00 | Research | 0/58 | 0/56 | 0 | 0 | 0/0 |
| **TOTALE** | | | 0 | 4 | 0 | 1 | 0/2 |

Esempi fuori finestra (primi 25):

| lato | strategia | ingresso UTC | etichetta barra di segnale | finestra |
|---|---|---|---:|---|
| cBot | PTS_NG_TFM_006_60 | 2025-11-23 23:05 | 23:00 | 00:00-22:00 |
| cBot | PTS_NQ_TFU_008_240 | 2026-02-01 23:05 | 00:00 | 06:00-24:00 |
| cBot | PTS_NG_TFM_006_60 | 2026-02-08 23:05 | 23:00 | 00:00-22:00 |
| cBot | PTS_NG_TFM_001_240 | 2026-02-08 23:05 | 00:00 | 10:00-24:00 |

## 4. Sessione: ancoraggio e un fill per sessione per lato

| strategia | sessione (ancoraggio) | max ingressi/sessione | int sessioni con >max per lato | cBot sessioni con >max per lato | int ingressi weekend UTC | cBot ingressi weekend UTC |
|---|---|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_BP_TFM_002_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_BTC_BIA_001_60 | 00:00-24:00 | 0 | 0 | 0 | 71 | 68 |
| PTS_BTC_PCH_001_240 | 00:00-24:00 | 1 | 0 | 0 | 92 | 92 |
| PTS_BTC_TFU_001_60 | 00:00-24:00 | 0 | 0 | 0 | 8 | 9 |
| PTS_BTC_TFU_002_60 | 00:00-24:00 | 0 | 0 | 0 | 17 | 18 |
| PTS_CC_SBO_001_60 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_CL_MAC_001_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_CT_TFU_001_240 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_001_60 | 00:00-24:00 | 0 | 0 | 0 | 7 | 7 |
| PTS_ES_BSW_002_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_BSW_003_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_002_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_PCH_003_1440 | 00:00-24:00 | 1 | 0 | 0 | 1 | 1 |
| PTS_ES_PCH_004_240 | 00:00-24:00 | 1 | 1 | 1 | 0 | 1 |
| PTS_ES_SBO_001_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 0 |
| PTS_ES_SBO_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 3 |
| PTS_ES_SBO_003_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_ES_TFM_001_1440 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_TFM_002_1440 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_ES_TFU_001_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_FDAX_MAC_001_240 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_FDAX_SBO_002_1440 | 01:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_FDAX_TFU_001_1440 | 01:00-24:00 | 0 | 0 | 0 | 1 | 3 |
| PTS_FDAX_TFU_002_1440 | 01:00-24:00 | 0 | 0 | 0 | 1 | 3 |
| PTS_GC_PCH_001_60 | 00:00-24:00 | 0 | 0 | 0 | 2 | 2 |
| PTS_GC_PCH_004_240 | 00:00-24:00 | 1 | 4 | 4 | 9 | 12 |
| PTS_GC_PCH_005_240 | 00:00-24:00 | 1 | 1 | 1 | 1 | 5 |
| PTS_GC_RHL_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_GC_RHL_002_60 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_GC_TFM_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_GC_TFU_001_30 | 00:00-24:00 | 0 | 0 | 0 | 4 | 4 |
| PTS_NG_BSW_001_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_NG_TFM_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_NG_TFM_003_30 | 00:00-24:00 | 0 | 0 | 0 | 2 | 3 |
| PTS_NG_TFM_004_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFM_005_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 2 |
| PTS_NG_TFM_006_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 2 |
| PTS_NG_TFU_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFU_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NG_TFU_003_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_NQ_PCH_003_30 | 00:00-24:00 | 0 | 0 | 0 | 2 | 3 |
| PTS_NQ_PCH_004_30 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_PCH_007_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_PCH_008_240 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_RBM_001_15 | 00:00-24:00 | 0 | 0 | 0 | 2 | 2 |
| PTS_NQ_SBO_001_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_002_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_003_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_SBO_004_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_SBO_005_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_001_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_002_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_003_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_005_15 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_006_30 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_007_30 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_010_60 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_011_60 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_012_1440 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFM_013_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_NQ_TFM_015_240 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_001_15 | 00:00-24:00 | 0 | 0 | 0 | 2 | 2 |
| PTS_NQ_TFU_002_15 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFU_003_15 | 00:00-24:00 | 0 | 0 | 0 | 3 | 4 |
| PTS_NQ_TFU_004_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_NQ_TFU_005_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 2 |
| PTS_NQ_TFU_006_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_007_1440 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_NQ_TFU_008_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 1 |
| PTS_NQ_VBO_001_1440 | 00:00-24:00 | 1 | 0 | 0 | 0 | 0 |
| PTS_NQ_VBO_002_240 | 00:00-24:00 | 1 | 0 | 0 | 2 | 2 |
| PTS_SB_TFM_001_240 | 01:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_BIA_001_240 | 00:00-24:00 | 0 | 0 | 0 | 4 | 4 |
| PTS_YM_SBO_001_240 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_YM_SBO_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_001_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_002_240 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFM_003_240 | 00:00-24:00 | 0 | 0 | 0 | 1 | 2 |
| PTS_YM_TFU_001_60 | 00:00-24:00 | 0 | 0 | 0 | 1 | 1 |
| PTS_YM_TFU_002_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| PTS_YM_TFU_003_60 | 00:00-24:00 | 0 | 0 | 0 | 0 | 0 |
| **TOTALE** | | | 6 | 6 | | |

Nel log del cBot il server ha rifiutato template per `limite di ingressi per sessione raggiunto` (conteggio righe, per strategia e lato, prime 15):


## 5. Spread

Spread dell'interno, dal summary: `FTMOPLATFORM · p50Spread · per ora UTC · FTMOPLATFORM_spread-by-symbol_20260801-20260901.csv (2026-09-06)`. Il cBot lo paga sui tick: costo per trade ≈ spread al fill × valore punto × contratti (una volta per round trip). Lo stop è quello eseguito (allargato ×1 dove previsto).

| strategia | simbolo | fill con spread | spread medio (punti) | stop (punti) | spread/stop | costo spread tot (per contratto, $) | netto/ctr cBot | netto/ctr senza spread | costo/trade ($/ctr) |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| PTS_BP_TFM_001_60 | BP | 0/9 | 0 | 0.01 | 0.0% | 0 | 6,641 | 6,641 | 0 |
| PTS_BP_TFM_002_15 | BP | 0/5 | 0 | 0.02 | 0.0% | 0 | 931 | 931 | 0 |
| PTS_BTC_BIA_001_60 | BTC | 0/303 | 0 | 50 | 0.0% | 0 | 1,283 | 1,283 | 0 |
| PTS_BTC_PCH_001_240 | BTC | 0/223 | 0 | 350 | 0.0% | 0 | -14,610 | -14,610 | 0 |
| PTS_BTC_TFU_001_60 | BTC | 0/37 | 0 | 50 | 0.0% | 0 | 4,533 | 4,533 | 0 |
| PTS_BTC_TFU_002_60 | BTC | 0/52 | 0 | 50 | 0.0% | 0 | 2,781 | 2,781 | 0 |
| PTS_CC_SBO_001_60 | CC | 0/10 | 0 | 175 | 0.0% | 0 | 17,972 | 17,972 | 0 |
| PTS_CL_MAC_001_30 | CL | 0/33 | 0 | 1 | 0.0% | 0 | -1,679 | -1,679 | 0 |
| PTS_CT_TFU_001_240 | CT | 0/10 | 0 | 6 | 0.0% | 0 | 6,102 | 6,102 | 0 |
| PTS_ES_BSW_001_60 | ES | 0/18 | 0 | 0 | 0.0% | 0 | 22,506 | 22,506 | 0 |
| PTS_ES_BSW_002_15 | ES | 0/14 | 0 | 0 | 0.0% | 0 | 10,197 | 10,197 | 0 |
| PTS_ES_BSW_003_15 | ES | 0/38 | 0 | 0 | 0.0% | 0 | 8,753 | 8,753 | 0 |
| PTS_ES_PCH_001_60 | ES | 0/20 | 0 | 80 | 0.0% | 0 | -5 | -5 | 0 |
| PTS_ES_PCH_002_60 | ES | 0/36 | 0 | 80 | 0.0% | 0 | -1,261 | -1,261 | 0 |
| PTS_ES_PCH_003_1440 | ES | 0/11 | 0 | 20 | 0.0% | 0 | -7,966 | -7,966 | 0 |
| PTS_ES_PCH_004_240 | ES | 0/79 | 0 | 100 | 0.0% | 0 | -57,705 | -57,705 | 0 |
| PTS_ES_SBO_001_15 | ES | 0/6 | 0 | 80 | 0.0% | 0 | 5,339 | 5,339 | 0 |
| PTS_ES_SBO_002_240 | ES | 0/14 | 0 | 80 | 0.0% | 0 | -11,841 | -11,841 | 0 |
| PTS_ES_SBO_003_240 | ES | 0/17 | 0 | 80 | 0.0% | 0 | -21,456 | -21,456 | 0 |
| PTS_ES_TFM_001_1440 | ES | 0/12 | 0 | 20 | 0.0% | 0 | 1,649 | 1,649 | 0 |
| PTS_ES_TFM_002_1440 | ES | 0/30 | 0 | 20 | 0.0% | 0 | -6,268 | -6,268 | 0 |
| PTS_ES_TFU_001_1440 | ES | 0/38 | 0 | 40 | 0.0% | 0 | -38,351 | -38,351 | 0 |
| PTS_FDAX_MAC_001_240 | FDAX | 0/10 | 0 | 120 | 0.0% | 0 | -21,493 | -21,493 | 0 |
| PTS_FDAX_SBO_002_1440 | FDAX | 0/15 | 0 | 40 | 0.0% | 0 | 140 | 140 | 0 |
| PTS_FDAX_TFU_001_1440 | FDAX | 0/47 | 0 | 40 | 0.0% | 0 | -10,316 | -10,316 | 0 |
| PTS_FDAX_TFU_002_1440 | FDAX | 0/51 | 0 | 40 | 0.0% | 0 | -23,860 | -23,860 | 0 |
| PTS_GC_PCH_001_60 | GC | 0/23 | 0 | 22.5 | 0.0% | 0 | -9,094 | -9,094 | 0 |
| PTS_GC_PCH_004_240 | GC | 0/269 | 0 | 5 | 0.0% | 0 | 21,606 | 21,606 | 0 |
| PTS_GC_PCH_005_240 | GC | 0/87 | 0 | 25 | 0.0% | 0 | -24,791 | -24,791 | 0 |
| PTS_GC_RHL_001_60 | GC | 0/32 | 0 | 20 | 0.0% | 0 | 3,341 | 3,341 | 0 |
| PTS_GC_RHL_002_60 | GC | 0/30 | 0 | 20 | 0.0% | 0 | 6,040 | 6,040 | 0 |
| PTS_GC_TFM_001_240 | GC | 0/8 | 0 | 50 | 0.0% | 0 | -15,345 | -15,345 | 0 |
| PTS_GC_TFU_001_30 | GC | 0/32 | 0 | 17.5 | 0.0% | 0 | -2,488 | -2,488 | 0 |
| PTS_NG_BSW_001_30 | NG | 0/15 | 0 | 0 | 0.0% | 0 | -1,397 | -1,397 | 0 |
| PTS_NG_TFM_001_240 | NG | 0/36 | 0 | 0.18 | 0.0% | 0 | -15,835 | -15,835 | 0 |
| PTS_NG_TFM_002_240 | NG | 0/41 | 0 | 0.03 | 0.0% | 0 | -7,604 | -7,604 | 0 |
| PTS_NG_TFM_003_30 | NG | 0/44 | 0 | 0.08 | 0.0% | 0 | -22,099 | -22,099 | 0 |
| PTS_NG_TFM_004_60 | NG | 0/15 | 0 | 0.15 | 0.0% | 0 | 973 | 973 | 0 |
| PTS_NG_TFM_005_30 | NG | 0/59 | 0 | 0.1 | 0.0% | 0 | -17,076 | -17,076 | 0 |
| PTS_NG_TFM_006_60 | NG | 0/60 | 0 | 0.13 | 0.0% | 0 | -35,538 | -35,538 | 0 |
| PTS_NG_TFU_001_240 | NG | 0/20 | 0 | 0.1 | 0.0% | 0 | -10,626 | -10,626 | 0 |
| PTS_NG_TFU_002_240 | NG | 0/27 | 0 | 0.1 | 0.0% | 0 | -16,919 | -16,919 | 0 |
| PTS_NG_TFU_003_60 | NG | 0/35 | 0 | 0.15 | 0.0% | 0 | -26,033 | -26,033 | 0 |
| PTS_NQ_PCH_003_30 | NQ | 0/65 | 0 | 125 | 0.0% | 0 | 13,666 | 13,666 | 0 |
| PTS_NQ_PCH_004_30 | NQ | 0/45 | 0 | 112.5 | 0.0% | 0 | 22,677 | 22,677 | 0 |
| PTS_NQ_PCH_007_240 | NQ | 0/19 | 0 | 150 | 0.0% | 0 | -19,899 | -19,899 | 0 |
| PTS_NQ_PCH_008_240 | NQ | 0/46 | 0 | 200 | 0.0% | 0 | 7,992 | 7,992 | 0 |
| PTS_NQ_RBM_001_15 | NQ | 0/136 | 0 | 100 | 0.0% | 0 | 40,787 | 40,787 | 0 |
| PTS_NQ_SBO_001_15 | NQ | 0/35 | 0 | 250 | 0.0% | 0 | -8,478 | -8,478 | 0 |
| PTS_NQ_SBO_002_15 | NQ | 0/24 | 0 | 200 | 0.0% | 0 | 727 | 727 | 0 |
| PTS_NQ_SBO_003_15 | NQ | 0/35 | 0 | 25 | 0.0% | 0 | 12,824 | 12,824 | 0 |
| PTS_NQ_SBO_004_60 | NQ | 0/18 | 0 | 25 | 0.0% | 0 | -10,144 | -10,144 | 0 |
| PTS_NQ_SBO_005_1440 | NQ | 0/11 | 0 | 50 | 0.0% | 0 | 11,447 | 11,447 | 0 |
| PTS_NQ_TFM_001_60 | NQ | 0/17 | 0 | 50 | 0.0% | 0 | -1,703 | -1,703 | 0 |
| PTS_NQ_TFM_002_15 | NQ | 0/35 | 0 | 150 | 0.0% | 0 | 5,148 | 5,148 | 0 |
| PTS_NQ_TFM_003_15 | NQ | 0/38 | 0 | 125 | 0.0% | 0 | 30,270 | 30,270 | 0 |
| PTS_NQ_TFM_005_15 | NQ | 0/41 | 0 | 200 | 0.0% | 0 | -7,835 | -7,835 | 0 |
| PTS_NQ_TFM_006_30 | NQ | 0/27 | 0 | 25 | 0.0% | 0 | -4,289 | -4,289 | 0 |
| PTS_NQ_TFM_007_30 | NQ | 0/20 | 0 | 250 | 0.0% | 0 | 12,389 | 12,389 | 0 |
| PTS_NQ_TFM_010_60 | NQ | 0/32 | 0 | 125 | 0.0% | 0 | -22,155 | -22,155 | 0 |
| PTS_NQ_TFM_011_60 | NQ | 0/34 | 0 | 62.5 | 0.0% | 0 | -6,884 | -6,884 | 0 |
| PTS_NQ_TFM_012_1440 | NQ | 0/8 | 0 | 50 | 0.0% | 0 | 7,631 | 7,631 | 0 |
| PTS_NQ_TFM_013_1440 | NQ | 0/9 | 0 | 50 | 0.0% | 0 | 10,250 | 10,250 | 0 |
| PTS_NQ_TFM_015_240 | NQ | 0/19 | 0 | 25 | 0.0% | 0 | 10,614 | 10,614 | 0 |
| PTS_NQ_TFU_001_15 | NQ | 0/32 | 0 | 200 | 0.0% | 0 | 34,441 | 34,441 | 0 |
| PTS_NQ_TFU_002_15 | NQ | 0/44 | 0 | 62.5 | 0.0% | 0 | -22,231 | -22,231 | 0 |
| PTS_NQ_TFU_003_15 | NQ | 0/50 | 0 | 87.5 | 0.0% | 0 | 18,051 | 18,051 | 0 |
| PTS_NQ_TFU_004_60 | NQ | 0/34 | 0 | 37.5 | 0.0% | 0 | 4,380 | 4,380 | 0 |
| PTS_NQ_TFU_005_60 | NQ | 0/48 | 0 | 200 | 0.0% | 0 | -4,574 | -4,574 | 0 |
| PTS_NQ_TFU_006_1440 | NQ | 0/42 | 0 | 50 | 0.0% | 0 | 31,147 | 31,147 | 0 |
| PTS_NQ_TFU_007_1440 | NQ | 0/15 | 0 | 50 | 0.0% | 0 | 5,429 | 5,429 | 0 |
| PTS_NQ_TFU_008_240 | NQ | 0/56 | 0 | 25 | 0.0% | 0 | -10,980 | -10,980 | 0 |
| PTS_NQ_VBO_001_1440 | NQ | 0/7 | 0 | 50 | 0.0% | 0 | -5,657 | -5,657 | 0 |
| PTS_NQ_VBO_002_240 | NQ | 0/30 | 0 | 112.5 | 0.0% | 0 | 35,616 | 35,616 | 0 |
| PTS_SB_TFM_001_240 | SB | 0/18 | 0 | 2.01 | 0.0% | 0 | -6,641 | -6,641 | 0 |
| PTS_YM_BIA_001_240 | YM | 0/81 | 0 | 450 | 0.0% | 0 | -6,421 | -6,421 | 0 |
| PTS_YM_SBO_001_240 | YM | 0/33 | 0 | 50 | 0.0% | 0 | -8,905 | -8,905 | 0 |
| PTS_YM_SBO_002_240 | YM | 0/31 | 0 | 50 | 0.0% | 0 | 1,403 | 1,403 | 0 |
| PTS_YM_TFM_001_240 | YM | 0/7 | 0 | 500 | 0.0% | 0 | -4,617 | -4,617 | 0 |
| PTS_YM_TFM_002_240 | YM | 0/23 | 0 | 800 | 0.0% | 0 | -3,901 | -3,901 | 0 |
| PTS_YM_TFM_003_240 | YM | 0/71 | 0 | 250 | 0.0% | 0 | -11,487 | -11,487 | 0 |
| PTS_YM_TFU_001_60 | YM | 0/46 | 0 | 500 | 0.0% | 0 | -17,788 | -17,788 | 0 |
| PTS_YM_TFU_002_60 | YM | 0/29 | 0 | 200 | 0.0% | 0 | -22,937 | -22,937 | 0 |
| PTS_YM_TFU_003_60 | YM | 0/56 | 0 | 50 | 0.0% | 0 | -4,682 | -4,682 | 0 |
| **TOTALE** | | | | | | 0 | -196,183 | -196,183 | |

### 5b. Strategie con stop più stretto dello spread, o quasi

| strategia | spread/stop | stop (punti) | trade cBot | netto/ctr cBot | commento |
|---|---:|---:|---:|---:|---|

Sui trade abbinati: 0 volte il cBot esce per stop dove l'interno no; 1745 il contrario.

## 6. Strategie con curva di equity crescente

Metriche su netto per contratto, trade ordinati per uscita. R² = fit lineare della curva cumulata sull'indice dei trade. Crescente = netto > 0, R² ≥ 0,6, chiusura ad almeno il 70% del picco, ≥ 8 trade. Il cBot è calcolato sull'intero run (ingressi 31/08/2025 → 04/08/2026), l'interno sull'intero run (ingressi 31/08/2025 → 30/08/2026).

| strategia | cBot n | cBot netto/ctr | PF | R² | DD max/ctr | fine/picco | **cBot** | int n | int netto/ctr | PF | R² | DD max/ctr | fine/picco | **int** |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| PTS_BP_TFM_001_60 | 9 | 6,641 | 3.07 | 0.54 | 1,671 | 0.80 | no | 13 | 1,879 | 1.42 | 0.60 | 2,372 | 0.44 | no |
| PTS_BP_TFM_002_15 | 5 | 931 | 1.91 | 0.29 | 1,021 | 0.48 | no | 5 | 980 | 1.98 | 0.30 | 1,004 | 0.49 | no |
| PTS_BTC_BIA_001_60 | 303 | 1,283 | 1.01 | 0.17 | 28,957 | 0.14 | no | 299 | 15,142 | 1.21 | 0.10 | 24,002 | 0.72 | no |
| PTS_BTC_PCH_001_240 | 223 | -14,610 | 0.90 | 0.45 | 25,643 | -4.59 | no | 243 | 13,795 | 1.10 | 0.15 | 17,375 | 1.00 | no |
| PTS_BTC_TFU_001_60 | 37 | 4,533 | 1.43 | 0.43 | 5,974 | 0.71 | no | 38 | 5,848 | 1.64 | 0.60 | 4,572 | 0.74 | no |
| PTS_BTC_TFU_002_60 | 52 | 2,781 | 1.18 | 0.00 | 9,139 | 0.91 | no | 55 | 11,030 | 1.85 | 0.60 | 7,112 | 0.90 | no |
| PTS_CC_SBO_001_60 | 10 | 17,972 | 2.69 | 0.52 | 7,072 | 0.72 | no | 11 | 16,108 | 2.31 | 0.39 | 8,770 | 0.65 | no |
| PTS_CL_MAC_001_30 | 33 | -1,679 | 0.92 | 0.00 | 9,065 | -1.66 | no | 35 | -1,684 | 0.92 | 0.03 | 9,048 | -1.59 | no |
| PTS_CT_TFU_001_240 | 10 | 6,102 | 2.01 | 0.07 | 6,026 | 0.80 | no | 12 | -6,868 | 0.52 | 0.87 | 11,356 | -2.30 | no |
| PTS_ES_BSW_001_60 | 18 | 22,506 | 1.69 | 0.39 | 15,090 | 0.90 | no | 18 | 22,825 | 1.70 | 0.40 | 15,012 | 0.90 | no |
| PTS_ES_BSW_002_15 | 14 | 10,197 | 1.48 | 0.62 | 9,138 | 0.63 | no | 15 | 7,440 | 1.31 | 0.32 | 9,012 | 0.83 | no |
| PTS_ES_BSW_003_15 | 38 | 8,753 | 1.14 | 0.49 | 18,210 | 0.32 | no | 42 | 16,051 | 1.25 | 0.51 | 18,024 | 0.59 | no |
| PTS_ES_PCH_001_60 | 20 | -5 | 1.00 | 0.01 | 4,334 | -0.00 | no | 22 | 6,980 | 1.84 | 0.13 | 4,064 | 0.90 | no |
| PTS_ES_PCH_002_60 | 36 | -1,261 | 0.90 | 0.12 | 6,954 | -0.42 | no | 38 | -2,507 | 0.82 | 0.04 | 6,629 | -1.02 | no |
| PTS_ES_PCH_003_1440 | 11 | -7,966 | 0.08 | 0.93 | 7,966 | 0.00 | no | 12 | -7,549 | 0.11 | 0.88 | 7,549 | 0.00 | no |
| PTS_ES_PCH_004_240 | 79 | -57,705 | 0.00 | 0.94 | 57,705 | 0.00 | no | 86 | -30,344 | 0.33 | 0.86 | 45,256 | -4.08 | no |
| PTS_ES_SBO_001_15 | 6 | 5,339 | 1.44 | 0.22 | 8,057 | 0.72 | no | 7 | 11,485 | 1.96 | 0.48 | 8,008 | 1.00 | no |
| PTS_ES_SBO_002_240 | 14 | -11,841 | 0.71 | 0.54 | 24,164 | -5.67 | no | 15 | 604 | 1.02 | 0.03 | 14,829 | 0.13 | no |
| PTS_ES_SBO_003_240 | 17 | -21,456 | 0.56 | 0.77 | 38,953 | 0.00 | no | 18 | -16,385 | 0.64 | 0.70 | 33,206 | -2.17 | no |
| PTS_ES_TFM_001_1440 | 12 | 1,649 | 1.16 | 0.15 | 8,274 | 0.33 | no | 13 | 948 | 1.09 | 0.25 | 9,036 | 0.19 | no |
| PTS_ES_TFM_002_1440 | 30 | -6,268 | 0.76 | 0.61 | 15,954 | -1.06 | no | 34 | -7,071 | 0.75 | 0.64 | 16,084 | -3.46 | no |
| PTS_ES_TFU_001_1440 | 38 | -38,351 | 0.44 | 0.79 | 48,065 | 0.00 | no | 48 | -48,718 | 0.39 | 0.94 | 53,442 | -10.31 | no |
| PTS_FDAX_MAC_001_240 | 10 | -21,493 | 0.13 | 0.88 | 21,493 | 0.00 | no | 10 | -21,325 | 0.08 | 0.97 | 21,325 | 0.00 | no |
| PTS_FDAX_SBO_002_1440 | 15 | 140 | 1.01 | 0.04 | 12,270 | 0.01 | no | 14 | 1,944 | 1.15 | 0.12 | 9,036 | 0.18 | no |
| PTS_FDAX_TFU_001_1440 | 47 | -10,316 | 0.80 | 0.00 | 23,468 | -0.78 | no | 52 | -10,386 | 0.78 | 0.00 | 23,270 | -0.81 | no |
| PTS_FDAX_TFU_002_1440 | 51 | -23,860 | 0.59 | 0.59 | 23,860 | 0.00 | no | 58 | -23,410 | 0.56 | 0.62 | 23,410 | 0.00 | no |
| PTS_GC_PCH_001_60 | 23 | -9,094 | 0.75 | 0.59 | 18,770 | -5.40 | no | 25 | -100 | 1.00 | 0.22 | 17,826 | -0.03 | no |
| PTS_GC_PCH_004_240 | 269 | 21,606 | 1.16 | 0.34 | 21,714 | 0.89 | no | 281 | 61,785 | 1.51 | 0.88 | 11,917 | 0.90 | SÌ |
| PTS_GC_PCH_005_240 | 87 | -24,791 | 0.80 | 0.62 | 32,610 | -10.35 | no | 89 | 12,463 | 1.11 | 0.03 | 24,997 | 0.83 | no |
| PTS_GC_RHL_001_60 | 32 | 3,341 | 1.09 | 0.02 | 14,337 | 0.26 | no | 33 | 403 | 1.01 | 0.02 | 13,252 | 0.03 | no |
| PTS_GC_RHL_002_60 | 30 | 6,040 | 1.19 | 0.00 | 13,996 | 0.79 | no | 33 | 1,371 | 1.04 | 0.05 | 13,009 | 0.15 | no |
| PTS_GC_TFM_001_240 | 8 | -15,345 | 0.49 | 0.34 | 17,760 | 0.00 | no | 8 | -15,032 | 0.50 | 0.33 | 17,524 | 0.00 | no |
| PTS_GC_TFU_001_30 | 32 | -2,488 | 0.95 | 0.37 | 15,797 | -0.19 | no | 35 | 3,360 | 1.07 | 0.27 | 18,814 | 0.23 | no |
| PTS_NG_BSW_001_30 | 15 | -1,397 | 0.76 | 0.12 | 2,692 | 0.00 | no | 15 | -5,560 | 0.37 | 0.72 | 6,848 | 0.00 | no |
| PTS_NG_TFM_001_240 | 36 | -15,835 | 0.64 | 0.63 | 17,007 | -13.51 | no | 34 | -34,486 | 0.31 | 0.91 | 34,618 | 0.00 | no |
| PTS_NG_TFM_002_240 | 41 | -7,604 | 0.76 | 0.01 | 13,143 | -1.59 | no | 32 | -8,128 | 0.00 | 1.00 | 8,128 | 0.00 | no |
| PTS_NG_TFM_003_30 | 44 | -22,099 | 0.29 | 0.89 | 22,099 | 0.00 | no | 18 | -13,572 | 0.00 | 1.00 | 13,572 | 0.00 | no |
| PTS_NG_TFM_004_60 | 15 | 973 | 1.17 | 0.01 | 3,531 | 0.25 | no | 7 | 582 | 1.15 | 0.10 | 3,646 | 0.14 | no |
| PTS_NG_TFM_005_30 | 59 | -17,076 | 0.66 | 0.08 | 26,087 | -1.90 | no | 42 | -23,718 | 0.39 | 0.75 | 23,718 | 0.00 | no |
| PTS_NG_TFM_006_60 | 60 | -35,538 | 0.45 | 0.86 | 35,538 | 0.00 | no | 37 | -15,218 | 0.58 | 0.31 | 15,218 | 0.00 | no |
| PTS_NG_TFU_001_240 | 20 | -10,626 | 0.41 | 0.73 | 10,626 | 0.00 | no | 16 | -12,274 | 0.18 | 0.91 | 12,274 | 0.00 | no |
| PTS_NG_TFU_002_240 | 27 | -16,919 | 0.33 | 0.88 | 16,919 | 0.00 | no | 23 | -23,112 | 0.00 | 1.00 | 23,112 | 0.00 | no |
| PTS_NG_TFU_003_60 | 35 | -26,033 | 0.38 | 0.89 | 26,033 | 0.00 | no | 21 | -23,594 | 0.16 | 0.97 | 23,594 | 0.00 | no |
| PTS_NQ_PCH_003_30 | 65 | 13,666 | 1.33 | 0.02 | 20,507 | 0.61 | no | 69 | 16,842 | 1.41 | 0.10 | 19,621 | 0.81 | no |
| PTS_NQ_PCH_004_30 | 45 | 22,677 | 2.56 | 0.22 | 12,129 | 1.00 | no | 50 | 23,183 | 2.58 | 0.47 | 11,229 | 0.92 | no |
| PTS_NQ_PCH_007_240 | 19 | -19,899 | 0.16 | 0.95 | 19,899 | 0.00 | no | 21 | -21,779 | 0.15 | 0.95 | 21,779 | 0.00 | no |
| PTS_NQ_PCH_008_240 | 46 | 7,992 | 1.22 | 0.32 | 16,480 | 0.55 | no | 50 | 16,215 | 1.44 | 0.50 | 17,061 | 0.79 | no |
| PTS_NQ_RBM_001_15 | 136 | 40,787 | 1.26 | 0.69 | 22,645 | 0.81 | SÌ | 163 | 22,515 | 1.14 | 0.54 | 26,251 | 0.75 | no |
| PTS_NQ_SBO_001_15 | 35 | -8,478 | 0.88 | 0.65 | 31,921 | -0.49 | no | 36 | -4,523 | 0.94 | 0.62 | 26,451 | -0.30 | no |
| PTS_NQ_SBO_002_15 | 24 | 727 | 1.02 | 0.02 | 16,234 | 0.06 | no | 25 | -2,600 | 0.94 | 0.01 | 19,032 | -0.16 | no |
| PTS_NQ_SBO_003_15 | 35 | 12,824 | 1.68 | 0.17 | 14,973 | 0.77 | no | 38 | 13,169 | 1.71 | 0.35 | 13,104 | 0.70 | no |
| PTS_NQ_SBO_004_60 | 18 | -10,144 | 0.00 | 1.00 | 10,144 | 0.00 | no | 18 | -9,072 | 0.00 | 1.00 | 9,072 | 0.00 | no |
| PTS_NQ_SBO_005_1440 | 11 | 11,447 | 2.21 | 0.87 | 8,399 | 0.58 | no | 12 | 10,832 | 2.08 | 0.89 | 9,036 | 0.55 | no |
| PTS_NQ_TFM_001_60 | 17 | -1,703 | 0.88 | 0.00 | 5,579 | 0.00 | no | 18 | -2,072 | 0.85 | 0.04 | 5,052 | 0.00 | no |
| PTS_NQ_TFM_002_15 | 35 | 5,148 | 1.09 | 0.41 | 22,899 | 0.32 | no | 37 | 1,507 | 1.02 | 0.20 | 26,409 | 0.09 | no |
| PTS_NQ_TFM_003_15 | 38 | 30,270 | 1.59 | 0.43 | 23,306 | 0.92 | no | 42 | 34,832 | 1.63 | 0.60 | 22,536 | 0.93 | no |
| PTS_NQ_TFM_005_15 | 41 | -7,835 | 0.90 | 0.30 | 34,374 | -0.88 | no | 43 | -14,581 | 0.83 | 0.19 | 33,509 | -1.62 | no |
| PTS_NQ_TFM_006_30 | 27 | -4,289 | 0.70 | 0.01 | 9,832 | -25.87 | no | 30 | 18,303 | 2.40 | 0.72 | 4,536 | 0.97 | SÌ |
| PTS_NQ_TFM_007_30 | 20 | 12,389 | 1.46 | 0.69 | 13,599 | 1.00 | SÌ | 21 | 15,686 | 1.59 | 0.74 | 13,580 | 1.00 | SÌ |
| PTS_NQ_TFM_010_60 | 32 | -22,155 | 0.64 | 0.52 | 31,728 | -4.46 | no | 33 | -22,632 | 0.64 | 0.48 | 35,080 | -4.53 | no |
| PTS_NQ_TFM_011_60 | 34 | -6,884 | 0.82 | 0.69 | 16,436 | -1.63 | no | 38 | -20,932 | 0.53 | 0.84 | 20,932 | 0.00 | no |
| PTS_NQ_TFM_012_1440 | 8 | 7,631 | 2.04 | 0.02 | 6,306 | 0.55 | no | 9 | 6,964 | 1.87 | 0.00 | 7,028 | 0.50 | no |
| PTS_NQ_TFM_013_1440 | 9 | 10,250 | 2.22 | 0.02 | 7,360 | 0.58 | no | 9 | 10,513 | 2.31 | 0.02 | 7,028 | 0.60 | no |
| PTS_NQ_TFM_015_240 | 19 | 10,614 | 2.14 | 0.00 | 7,672 | 0.58 | no | 18 | 1,428 | 1.17 | 1.00 | 8,568 | 0.14 | no |
| PTS_NQ_TFU_001_15 | 32 | 34,441 | 1.62 | 0.75 | 14,401 | 1.00 | SÌ | 33 | 31,523 | 1.54 | 0.78 | 14,361 | 0.89 | SÌ |
| PTS_NQ_TFU_002_15 | 44 | -22,231 | 0.63 | 0.65 | 26,785 | -4.88 | no | 45 | -19,761 | 0.66 | 0.79 | 31,014 | -9.86 | no |
| PTS_NQ_TFU_003_15 | 50 | 18,051 | 1.56 | 0.67 | 6,998 | 0.83 | SÌ | 53 | 11,339 | 1.31 | 0.69 | 8,078 | 0.58 | no |
| PTS_NQ_TFU_004_60 | 34 | 4,380 | 1.17 | 0.02 | 12,941 | 0.25 | no | 35 | -4,890 | 0.80 | 0.55 | 14,886 | -0.49 | no |
| PTS_NQ_TFU_005_60 | 48 | -4,574 | 0.96 | 0.25 | 26,094 | -0.28 | no | 54 | 2,638 | 1.02 | 0.00 | 20,398 | 0.11 | no |
| PTS_NQ_TFU_006_1440 | 42 | 31,147 | 1.71 | 0.00 | 31,082 | 0.91 | no | 47 | 48,812 | 2.19 | 0.12 | 26,104 | 1.00 | no |
| PTS_NQ_TFU_007_1440 | 15 | 5,429 | 1.43 | 0.05 | 10,536 | 0.84 | no | 16 | 4,936 | 1.38 | 0.00 | 10,040 | 0.71 | no |
| PTS_NQ_TFU_008_240 | 56 | -10,980 | 0.62 | 0.75 | 17,841 | -3.94 | no | 56 | -8,224 | 0.69 | 0.59 | 15,624 | -2.76 | no |
| PTS_NQ_VBO_001_1440 | 7 | -5,657 | 0.10 | 0.92 | 5,657 | 0.00 | no | 6 | -6,024 | 0.00 | 1.00 | 6,024 | 0.00 | no |
| PTS_NQ_VBO_002_240 | 30 | 35,616 | 4.35 | 0.90 | 3,251 | 1.00 | SÌ | 21 | 30,410 | 5.56 | 0.79 | 4,131 | 1.00 | SÌ |
| PTS_SB_TFM_001_240 | 18 | -6,641 | 0.30 | 0.92 | 7,063 | -15.74 | no | 20 | -4,112 | 0.44 | 0.82 | 4,522 | -10.04 | no |
| PTS_YM_BIA_001_240 | 81 | -6,421 | 0.89 | 0.45 | 20,530 | -5.52 | no | 88 | -3,556 | 0.94 | 0.14 | 19,623 | -2.63 | no |
| PTS_YM_SBO_001_240 | 33 | -8,905 | 0.00 | 1.00 | 8,905 | 0.00 | no | 37 | -8,616 | 0.06 | 0.99 | 8,616 | 0.00 | no |
| PTS_YM_SBO_002_240 | 31 | 1,403 | 1.19 | 0.08 | 4,875 | 0.26 | no | 32 | 4,870 | 1.68 | 0.01 | 4,574 | 0.89 | no |
| PTS_YM_TFM_001_240 | 7 | -4,617 | 0.63 | 0.05 | 7,565 | -10.73 | no | 7 | -4,528 | 0.64 | 0.06 | 7,512 | -9.43 | no |
| PTS_YM_TFM_002_240 | 23 | -3,901 | 0.86 | 0.07 | 17,141 | -8.99 | no | 25 | -8,062 | 0.75 | 0.39 | 15,885 | -16.66 | no |
| PTS_YM_TFM_003_240 | 71 | -11,487 | 0.84 | 0.25 | 22,849 | -1.56 | no | 80 | -13,338 | 0.82 | 0.14 | 19,147 | -2.30 | no |
| PTS_YM_TFU_001_60 | 46 | -17,788 | 0.77 | 0.66 | 32,912 | -1.38 | no | 61 | -21,920 | 0.76 | 0.63 | 31,849 | -2.44 | no |
| PTS_YM_TFU_002_60 | 29 | -22,937 | 0.21 | 0.91 | 23,795 | 0.00 | no | 35 | -27,686 | 0.14 | 0.97 | 27,686 | 0.00 | no |
| PTS_YM_TFU_003_60 | 56 | -4,682 | 0.68 | 0.03 | 6,211 | -3.06 | no | 64 | -2,518 | 0.83 | 0.05 | 5,098 | -0.98 | no |

**Crescenti sul cBot:** PTS_NQ_RBM_001_15, PTS_NQ_TFM_007_30, PTS_NQ_TFU_001_15, PTS_NQ_TFU_003_15, PTS_NQ_VBO_002_240

**Crescenti sull'interno:** PTS_GC_PCH_004_240, PTS_NQ_TFM_006_30, PTS_NQ_TFM_007_30, PTS_NQ_TFU_001_15, PTS_NQ_VBO_002_240

**Crescenti su entrambi:** PTS_NQ_TFM_007_30, PTS_NQ_TFU_001_15, PTS_NQ_VBO_002_240

## 7. Equity aggregata per mese (netto per contratto, sovrapposizione)

| mese (uscita) | interno | cBot | Δ |
|---|---:|---:|---:|
| 2025-08 | -1,758 (cum -1,758) | -1,803 (cum -1,803) | -45 |
| 2025-09 | 69,142 (cum 67,384) | 57,233 (cum 55,430) | -11,909 |
| 2025-10 | -53,160 (cum 14,224) | -62,971 (cum -7,541) | -9,811 |
| 2025-11 | 44,276 (cum 58,500) | 60,053 (cum 52,512) | 15,777 |
| 2025-12 | -123,920 (cum -65,420) | -96,055 (cum -43,543) | 27,865 |
| 2026-01 | -126,244 (cum -191,664) | -117,089 (cum -160,631) | 9,155 |
| 2026-02 | -50,833 (cum -242,496) | -90,133 (cum -250,764) | -39,300 |
| 2026-03 | 26,205 (cum -216,291) | -7,185 (cum -257,949) | -33,390 |
| 2026-04 | 102,786 (cum -113,506) | 42,507 (cum -215,443) | -60,279 |
| 2026-05 | 103,772 (cum -9,733) | 98,499 (cum -116,944) | -5,274 |
| 2026-06 | -71,085 (cum -80,819) | -77,384 (cum -194,328) | -6,299 |
| 2026-07 | -16,320 (cum -97,139) | -15,389 (cum -209,717) | 931 |
| 2026-08 | 86,841 (cum -10,298) | 13,534 (cum -196,183) | -73,307 |

## 9. Quando arrivano gli intent al cBot (righe `Intent ... attesa`)

`attesa` = ValidFrom − ora del server: negativo = l'intent arriva dopo l'inizio della barra su cui è valido. Per tipo di ordine e timeframe della strategia.

| tipo | tf | intent | in orario (≤ 10 s) | 10 s – 5 min | 5 min – 1 barra | ≥ 1 barra (in ritardo di una barra intera) | esempio strategie ≥ 1 barra |
|---|---:|---:|---:|---:|---:|---:|---|

### 9b. Trade cBot nati da un intent arrivato in ritardo di almeno una barra

Trade cBot con intent risolto: 0 su 3438; nati da intent in ritardo ≥ 1 barra: 0, netto/ctr 0; di questi senza corrispondente interno: 0.

| strategia | trade da intent stantio | netto/ctr | non abbinati |
|---|---:|---:|---:|

Trade abbinati in cui il cBot esce per stop più di 5 minuti PRIMA dell'uscita interna (che non era uno stop): 0, Δ netto/ctr 0. Per strategia: .

## 8. Avvisi ed errori nel log del cBot (messaggi normalizzati, prime 25)


Ingressi scartati per classe:

Ingressi annullati per classe:

`Nessun intent per l'account` per motivo:

Ingressi scartati `lato sbagliato` per strategia (prime 15):

