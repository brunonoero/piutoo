# BOS Dow 1 ora con il flat: le verifiche

Regola identica alla finalista del percorso (`ym-60-ricerca-ftmo-flat.md`): breakout della sessione in corso, LevelSource 1, offset 2 tick, pattern neutro YES 23, direzionale NO -45, stop 1 ATR, nessun target, al massimo 23 barre. Costi FTMO, niente overnight, flat alle 20:45 UTC. Nessun parametro ritoccato.

## 1. Sui periodi

| periodo | trade | netto | DD chiuso | net/DD | average trade | anni in utile | per anno (netto in migliaia) |
|---|---:|---:|---:|---:|---:|---:|---|
| Dow, ricerca FTMO 11/2020-09/2024 | 115 | 54,070 | 6,936 | 7.80 | 470 | 4/5 | 2020:-2.1 2021:22 2022:21.8 2023:5.5 2024:6.8 |
| Dow, prova FTMO 09/2024-09/2026 | 58 | 31,199 | 5,566 | 5.61 | 538 | 3/3 | 2024:13.3 2025:3.4 2026:14.5 |
| S&P, FTMO 11/2020-09/2026 | 160 | 45,578 | 14,952 | 3.05 | 285 | 5/7 | 2020:-0.3 2021:6.3 2022:13.8 2023:-3.9 2024:12.5 2025:2.9 2026:14.2 |
| S&P, interno 2006-11/2020, mai visto (orologio a 60 minuti) | 393 | -21,513 | 26,379 | -0.82 | -55 | 4/15 | 2006:-1.3 2007:-2.6 2008:1.3 2009:-3.9 2010:-0.5 2011:-1.9 2012:-1.1 2013:0.1 2014:-1.6 2015:-4.4 2016:-3.3 2017:0.8 2018:-1 2019:-4 2020:2 |

## 2. Contro 100 ingressi casuali con le stesse uscite

RC_RAN riceve stop 1 ATR, al massimo 23 barre, un ingresso per sessione, entrambi i lati, e sceglie a caso il quando e il lato. Percentile = quota di semi che fa peggio della strategia.

### Dow, prova FTMO 09/2024-09/2026

BOS 58 trade, netto 31,199. RAN p = 0.00549, trade medi 56.

| metrica | BOS | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 31,198.75 | -20,381.17 | 1,167.51 | 18,954.60 | 99% |
| average trade | 537.91 | -369.25 | 17.14 | 322.06 | 100% |
| netto / DD | 5.61 | -0.89 | 0.08 | 2.93 | 99% |

### S&P, interno 2006-11/2020, mai visto (orologio a 60 minuti)

BOS 393 trade, netto -21,513. RAN p = 0.00529, trade medi 405.

| metrica | BOS | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | -21,512.69 | -48,250.36 | -21,522.12 | 6,854.67 | 51% |
| average trade | -54.74 | -118.02 | -49.29 | 16.64 | 47% |
| netto / DD | -0.82 | -0.98 | -0.71 | 0.47 | 32% |


## Verdetto (30/09/2026)

**Sospesa, non si promuove.** Sul Dow batte il caso in modo netto, ma la storia lunga non c'e' e dove c'e' la regola
non regge.

- **Dow, prova FTMO**: 99-100° percentile contro il caso (mediana del caso +1.168, la BOS +31.199). Il vantaggio sul
  periodo recente e' vero.
- **Storia lunga**: il Dow interno ha solo barre a 4 ore, la regola a 1 ora non si puo' misurare. Sull'S&P, l'unico
  altro mercato dove regge dal 2020 (net/DD 3,05), il 2006-2020 perde: -21.513, 4 anni su 15 in utile, 51° percentile
  contro il caso. Sull'S&P la regola vale quanto il caso fuori dal 2020-2026.
- **Altri mercati**: 1 su 7 dal 2020.

Senza una storia lunga del Dow non si distingue un edge del Dow da un regime degli ultimi sei anni. Si riapre se
arriva il Dow orario o al minuto prima del 2020 (datafeed interno o vendor).
