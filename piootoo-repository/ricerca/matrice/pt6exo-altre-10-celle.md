# PT6EXO, le altre 10 celle della matrice (lette il 29/09/2026)

Seguito di `pt6exo-prime-7-celle.md`, stesse condizioni: griglie grosse, motore nudo, feed FTMO, spread e
swap peggiori FTMO, commissione 0, orologio al minuto, campione 2022-01-01 → 2025-01-01, fuori campione →
2026-09-01. CSV `{sim}-{tf}-{motore}.csv`, riepiloghi in `log/matrice-*.log`. Direction: 0 entrambe, 1 solo
long, 2 solo short. Stop e target in decimi di ATR delle sessioni chiuse. La stessa avvertenza: il fuori
campione e' un rialzo forte su DAX e Nasdaq.

| cella | combinazioni | ammissibili | OOS in utile ≥ 3/4 | equilibrate | robuste | sopra soglia | verdetto |
|---|---:|---:|---:|---:|---:|---:|---|
| IBS NQ 4h | 243 | 33 | 3 | 0 | 0 | 0 | no, sovradattamento |
| IBS NQ 1h | 243 | 15 | 6 | 0 | 0 | 0 | no |
| RUN NQ 4h | 324 | 6 | 3 | 0 | 0 | 0 | no |
| RUN NQ 1h | 324 | 0 | 0 | 0 | 0 | 0 | no |
| NRX FDAX 4h | 243 | 124 | 7 | 0 | 0 | 0 | no, sovradattamento |
| NRX FDAX 1h | 243 | 78 | 9 | 0 | 0 | 0 | no |
| **NRX NQ 4h** | 243 | 168 | 71 | 29 | 22 | **10** | **la prima che passa la griglia** |
| NRX NQ 1h | 243 | 80 | 66 | 27 | 6 | 0 | vicina, sotto soglia |
| HOD FDAX 1h | 324 | 90 | 27 | 0 | 0 | 0 | no |
| HOD NQ 1h | 324 | 108 | 54 | 9 | 3 | 0 | no, average trade troppo piccolo |

Soglie di average trade misurate dalla griglia: NQ 4h 385, NQ 1h 186, FDAX 4h 385, FDAX 1h 201.

## Cella per cella

- **IBS NQ 4h**: le ammissibili sono tutte solo long; in campione +22,6k medi, fuori −27,3k su ogni leva. Le
  migliori in campione (soglia 10, uscita alle 21, +39k) perdono −46k fuori.
- **IBS NQ 1h**: 15 ammissibili, nessuna equilibrata. Il target non conta (0, 1,5 e 3 ATR danno lo stesso
  risultato): l'uscita a IBS 0,5 arriva sempre prima.
- **RUN NQ 4h e 1h**: 6 e 0 ammissibili. Le sei di 4h (due chiusure, solo long) fanno +1,5-8k in campione
  su 540-570 trade: e' zero.
- **NRX FDAX 4h**: il caso da manuale del campione che premia il rumore. Le 10 migliori in campione
  (NR-4, entrambi i lati, +140-191k) perdono **da −117k a −174k** fuori; per lookback l'OOS medio e'
  negativo ovunque (da −36k a −102k).
- **NRX FDAX 1h**: nessuna equilibrata. Il solo long con uscita alle 21 fa +87k dentro e +57k fuori in
  media, cioe' di nuovo la leva nota del DAX (long, 21), non la compressione; entrambi i lati −94k fuori.
- **HOD FDAX 1h**: si salva solo l'ingresso long alle 8 di Roma (OOS medio +69,5k contro +29k dentro): il
  rialzo del DAX preso alla prima ora. Nessuna equilibrata.
- **HOD NQ 1h**: long alle 16 di Roma (le 10 di New York, mezz'ora dopo l'apertura del cash): 9
  equilibrate, 3 robuste, IS +43,9k su 664 trade, OOS +81,7k su 428. Ma **average trade 66 contro soglia
  186**, UngerFit 0,25: una deriva vera ma troppo sottile per pagare lo scivolamento reale. Le 15 di Roma,
  migliori in campione (+72k), fuori fanno +2k.
- **NRX NQ 1h**: 27 equilibrate, 6 robuste, tutte solo long con stop 0,8 ATR. La migliore: NR-10, stop
  0,8, target 1,5, un giorno, IS +111,6k su 452 trade, OOS +77k su 282, 3/4, **average trade 247 contro
  186 ma UngerFit 0,84**. Fuori campione per anno vale piu' che dentro: regime.
- **NRX NQ 4h**: vedi sotto.

## NRX NQ 4h

29 equilibrate, 22 robuste (7 bocciate sull'outlier), **10 sopra soglia**: tutte **solo long** e NR-7, sei
su dieci con **stop 0,8 ATR** (il valore piu' stretto della griglia), sette su dieci con tenuta di un giorno.

| NR | stop | target | uscita | tenuta | IS trade | IS netto | IS DD | OOS trade | OOS netto | OOS DD | fin | avg IS | UngerFit |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|
| 7 | 0,8 | — | — | 1g | 267 | 126.110 | 31.490 | 182 | 121.104 | 27.926 | 3/4 | 472 | 1,36 |
| 7 | 2,5 | 1,5 | — | 1g | 265 | 121.892 | 29.957 | 181 | 70.269 | 50.818 | 3/4 | 460 | 1,35 |
| 7 | 0,8 | 1,5 | — | 1g | 267 | 122.013 | 31.490 | 183 | 126.243 | 27.926 | 3/4 | 457 | 1,31 |
| 7 | 0,8 | — | — | intraday | 283 | 121.648 | 34.330 | 197 | 100.691 | 37.307 | 3/4 | 430 | 1,18 |
| 7 | 0,8 | — | 21 | intraday | 284 | 108.391 | 29.601 | 204 | 55.371 | 52.888 | 3/4 | 382 | 1,13 |

**Cosa regge.** Average trade 382-472 contro soglia 385, UngerFit fino a 1,36; la configurazione migliore
fa +126k dentro e +121k fuori con drawdown sotto un quarto del netto in entrambe le meta'. Il risultato
non dipende da un valore solo delle uscite: fra le dieci ci sono stop 0,8, 1,5 e 2,5, con e senza target,
intraday e un giorno; fra le equilibrate c'e' anche NR-10 (+85,6k dentro, +84,3k fuori).

**Cosa non regge o non si e' visto.**

- **Solo long, su un Nasdaq che sale.** Direction=1 fa OOS medio +35k, entrambi i lati +10k, solo short
  −6k. Per anno il fuori campione (+72k/anno) vale piu' del campione (+42k/anno): e' il segnale di regime
  del punto 6. La domanda e' la stessa di FBO: quanto e' compressione e quanto e' "comprare il Nasdaq".
- **Guadagno annuo su drawdown in campione 1,33** (serve 2).
- **Sconto walk-forward**: 472 × 0,2-0,5 = 94-236, sotto la soglia di 385. Come la 002: componente di
  paniere, non strategia da conto.
- **Stop sul bordo della griglia**: 0,8 ATR e' il valore piu' stretto provato; il massimo potrebbe stare
  fuori.
- **Un periodo solo**: tre anni di campione e 267 trade, appena sopra i 250. FBO NQ 1h era all'88°/96°
  percentile contro il RAN e ha perso −245k sul 2008-2021.

## Decisione

- **Nessuna classe** da nessuna delle 10 celle, per ora.
- **NRX NQ 4h passa alle due misure che hanno chiuso FBO**, nell'ordine:
  1. **controllo RAN** solo long con le stesse uscite (stop 0,8 ATR, nessun target, un giorno), 100 semi;
  2. se sta sopra il 90° percentile in campione **e** fuori, la configurazione fissa sul **periodo lungo**
     del vendor (NQ dal 2008), come `NqFboLongPeriodStudy`.
  Solo se regge a entrambe: classe `PT6EXO_NQ_NRX_001_240` e correlazione giornaliera con le NQ dei piani.
- **NRX NQ 1h** e **HOD NQ 1h**: nessun passo ora. NRX NQ 1h si riapre solo se la 4h regge al periodo
  lungo (stessa idea su un altro timeframe); HOD NQ alle 16 ha un average trade che non paga i costi veri.
- IBS e RUN sono chiusi anche su NQ; NRX e HOD chiusi su FDAX.
