# BOS Nasdaq 4 ore con il flat: le tre verifiche

Regola identica alla finalista del percorso (`nq-240-ricerca-ftmo-flat.md`): breakout della sessione in corso con LevelSource 1, offset 10 tick, finestra fino all'01, pattern neutro NO 6, stop 1 ATR, nessun target, intraday. Costi FTMO, orologio al minuto, niente overnight, flat alle 20:45 UTC. Nessun parametro ritoccato.

## 1. Nasdaq: ricerca, prova e storia interna mai vista

| periodo | trade | netto | DD chiuso | net/DD | average trade | anni in utile | per anno (netto in migliaia) |
|---|---:|---:|---:|---:|---:|---:|---|
| ricerca FTMO 05/2022-09/2024 | 205 | 148,137 | 24,344 | 6.09 | 723 | 3/3 | 2022:43 2023:49 2024:56 |
| prova FTMO 09/2024-09/2026 | 208 | 93,607 | 63,353 | 1.48 | 450 | 2/3 | 2024:13 2025:-5 2026:85 |
| interno 2008-05/2022, mai visto | 939 | 2,077 | 58,390 | 0.04 | 2 | 9/15 | 2008:-2 2009:2 2010:3 2011:7 2012:0 2013:-1 2014:-2 2015:7 2016:1 2017:7 2018:-6 2019:1 2020:3 2021:-13 2022:-5 |

## 2. La regola identica sugli altri indici (FTMO 11/2020-09/2026)

| indice | trade | netto | DD chiuso | net/DD | average trade |
|---|---:|---:|---:|---:|---:|
| @FDAX | 606 | 103,110 | 90,049 | 1.15 | 170 |
| @ES | 390 | 2,921 | 55,866 | 0.05 | 7 |
| @YM | 494 | -54,859 | 68,081 | -0.81 | -111 |
| @FESX | 120 | -7,255 | 7,873 | -0.92 | -60 |
| @FCE | 0 | 0 | 0 | 0.00 | 0 |
| @Z | 442 | -6,820 | 16,158 | -0.42 | -15 |
| @NIY | 371 | 10,583,812 | 2,589,960 | 4.09 | 28,528 |

2 su 7 con net/DD ≥ 1 (il percorso chiede almeno la meta').

## 3. Contro 100 ingressi casuali con le stesse uscite

RC_RAN riceve finestra fino all'01, un ingresso per sessione, entrambi i lati, stop 1 ATR, intraday, e sceglie a caso il quando e il lato. Percentile = quota di semi che fa peggio della strategia.

### prova FTMO 09/2024-09/2026

BOS 208 trade, netto 93,607. RAN p = 0.39445, trade medi 203.

| metrica | BOS | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 93,606.51 | -127,957.83 | -4,541.47 | 134,129.65 | 89% |
| average trade | 450.03 | -653.45 | -21.73 | 647.97 | 88% |
| netto / DD | 1.48 | -0.89 | -0.08 | 2.97 | 87% |

### interno 2008-05/2022, mai visto

BOS 939 trade, netto 2,077. RAN p = 0.25611, trade medi 947.

| metrica | BOS | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 2,076.58 | -94,507.58 | -23,008.75 | 57,749.30 | 72% |
| average trade | 2.21 | -101.02 | -23.99 | 63.87 | 72% |
| netto / DD | 0.04 | -0.97 | -0.39 | 1.50 | 71% |


## Verdetto (30/09/2026)

**Non si tiene.** Fallisce tutte e tre le verifiche.

- **Storia interna 2008-05/2022, mai vista**: 939 trade, +2.077, net/DD 0,04, average trade 2. Nessun edge.
- **Contro il caso**: 89° percentile sulla prova, 72° sulla storia interna (serve almeno il 96°, come le RHL).
- **Altri indici**: 2 su 7 con net/DD ≥ 1 (DAX 1,15, Nikkei 4,09); S&P, Dow, EuroStoxx e FTSE in perdita o a zero.

Il 2022-2024 della ricerca (+148.137, 3 anni su 3) e il 2026 della prova (+85.000) sono il Nasdaq in rialzo.