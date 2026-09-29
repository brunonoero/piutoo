# RHL S&P 1 ora con il flat: le verifiche

Regola identica alla finalista del percorso (`es-60-ricerca-ftmo-flat.md`): long a limite 10 tick sotto il minimo di ieri, pattern direzionale 1 vietato, domenica esclusa, stop 0,8 ATR, nessun target, uscita dopo 4 barre o alle 21, intraday. Costi FTMO, niente overnight, flat alle 20:45 UTC.

## 1. Sui periodi

| periodo | trade | netto | DD chiuso | net/DD | average trade | anni in utile | per anno (netto in migliaia) |
|---|---:|---:|---:|---:|---:|---:|---|
| ricerca FTMO 11/2020-09/2024 | 128 | 26,177 | 8,192 | 3.20 | 205 | 5/5 | 2020:0.8 2021:1.7 2022:5.5 2023:15.9 2024:2.3 |
| prova FTMO 09/2024-09/2026 | 50 | 15,688 | 2,895 | 5.42 | 314 | 2/3 | 2024:-0.8 2025:10.7 2026:5.8 |
| interno 2006-11/2020, mai visto (orologio a 60 minuti) | 309 | 32,481 | 7,423 | 4.38 | 105 | 14/15 | 2006:0.7 2007:1.8 2008:2.1 2009:2.9 2010:2 2011:4.5 2012:1 2013:4.3 2014:0.6 2015:-5.4 2016:2.8 2017:1.2 2018:2.2 2019:4.9 2020:6.9 |

## 2. Sovrapposizione con PT3B_ES_RHL_001_240 (prova FTMO)

RHL 1 ora 50 trade, RHL 4 ore 9 trade. Trade della 1 ora con un trade della 4 ore lo stesso giorno: 1 (2%); ingresso entro 5 minuti: 0 (0%). Correlazione del P&L giornaliero: 0.11.

## 3. Contro 100 ingressi long casuali con le stesse uscite

RC_RAN riceve stop 0,8 ATR, uscita dopo 4 barre o alle 21, un ingresso per sessione, solo long, e sceglie a caso il quando. Percentile = quota di semi che fa peggio della strategia.

### prova FTMO 09/2024-09/2026

RHL 50 trade, netto 15,688. RAN p = 0.00554, trade medi 54.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 15,688.03 | -15,408.65 | 1,529.83 | 13,795.00 | 98% |
| average trade | 313.76 | -293.88 | 26.38 | 252.19 | 98% |
| netto / DD | 5.42 | -0.92 | 0.16 | 2.99 | 99% |

### interno 2006-11/2020, mai visto (orologio a 60 minuti)

RHL 309 trade, netto 32,481. RAN p = 0.00443, trade medi 318.

| metrica | RHL | RAN p5 | mediana | p95 | percentile |
|---|---:|---:|---:|---:|---:|
| netto | 32,481.44 | -20,618.21 | -6,524.99 | 9,457.14 | 100% |
| average trade | 105.12 | -65.20 | -20.95 | 28.92 | 100% |
| netto / DD | 4.38 | -0.97 | -0.51 | 1.34 | 100% |


## Verdetto (29/09/2026)

**Passa le tre verifiche.** E' la prima candidata dopo la RHL del DAX a 4 ore che regge sul caso e sulla storia lunga.

- **Contro il caso**: 98-99° percentile sulla prova, 100° sul 2006-2020. Gli ingressi casuali con le stesse uscite
  fanno in mediana +1.530 sulla prova e -6.525 sulla storia interna: il vantaggio sta nel punto d'ingresso.
- **Storia interna 2006-11/2020, mai vista**: 309 trade, +32.481, net/DD 4,38, **14 anni su 15 in utile** (2015 -5.400).
  Riserva: orologio a 60 minuti, perche' l'S&P interno non ha il minuto.
- **Non duplica PT3B_ES_RHL_001_240**: sulla prova 0 ingressi entro 5 minuti, 1 giorno in comune su 50, correlazione
  giornaliera 0,11.

**Riserve.** Pochi trade (25-35 l'anno); la regola nuda perde e il vantaggio sta nel pattern direzionale 1 e nella
domenica esclusa, anche se il caso e la storia lunga dicono che non e' rumore; importi piccoli: +314 a contratto a
trade sulla prova, circa 800 l'anno su un conto FTMO a size 1.

**Da fare prima di un piano**: la classe (`PT3B_ES_RHL_002_60`) con il test di riproduzione, la sovrapposizione con
`PT5DAV_ES_RHL_001_30` di P1 (stesso mercato, stessa idea), la regola identica al controllo sul caso su Nasdaq e Dow a
1 ora (altri mercati: NQ 2,28, YM 1,51), poi il backtest cBot del piano che la riceve.