# Sweep @NQ 15m — P5-BOS

- Strategia di partenza: `RC5_BOS`
- Datafeed: interno (vendor)
- Spread: FTMO, mediana **per ora UTC** (NQ da 1.45 a 1.75 pt); costante di riserva: NQ 1.45 pt
- Swap: **NQ** long 6.7098 pt/notte, short 0 pt/notte, rollover 20:59 UTC (FTMO)
- Commissione: $2 per contratto e per lato, cioe' $4 per trade
- Tenuta: **overnight e overweek liberi** (parita' con il motore di ricerca)
- Campione di ricerca: 2008-01-01 → 2017-01-01
- Validazione: 2017-01-01 → 2025-05-30
- Obiettivo: peggiore di 4 sotto-periodi (netto/drawdown con pavimento sulla perdita massima), almeno 250 trade, 25 per tratto e 10 in perdita
- Beam: 2
- Spazio: intero
- Durata: 413.1 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| 1 trigger | 9 | 7 | 2.3 | RC5_BOS: 1133 trade, netto -43,178, DD 52,550, PF 0.84, 87,481 ms |
| 2 finestra | 625 | 702 | 207.6 | RC5_BOS: 377 trade, netto 2,193, DD 10,530, PF 1.03, 66,455 ms |
| 3 stop | 16 | 32 | 4.7 | RC5_BOS: 271 trade, netto -5,579, DD 11,078, PF 0.93, 71,774 ms |
| 4 tenuta | 12 | 12 | 4.7 | RC5_BOS: 271 trade, netto -5,579, DD 11,078, PF 0.93, 73,076 ms |
| 5 YES neutro | 55 | 12 | 27.2 | RC5_BOS: 271 trade, netto -5,579, DD 11,078, PF 0.93, 103,235 ms |
| 6 NO neutro | 55 | 19 | 24.7 | RC5_BOS: 250 trade, netto -4, DD 8,236, PF 1.00, 73,246 ms |
| 7 YES direzionale | 103 | 7 | 67.2 | RC5_BOS: 250 trade, netto -4, DD 8,236, PF 1.00, 80,744 ms |
| 7 NO direzionale | 103 | 12 | 51.2 | RC5_BOS: 250 trade, netto -4, DD 8,236, PF 1.00, 106,697 ms |
| 8 calendario | 6 | 2 | 2.4 | RC5_BOS: 250 trade, netto -4, DD 8,236, PF 1.00, 76,700 ms |
| 9 target | 13 | 13 | 2.2 | RC5_BOS: 250 trade, netto 1,227, DD 8,236, PF 1.02, 60,392 ms |
| 10 stop finale | 16 | 32 | 5.0 | RC5_BOS: 250 trade, netto 3,910, DD 6,384, PF 1.06, 72,924 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 250 | 3,910 | 0.61 | 214 | 104,743 | 1.99 | 325% | 2/4 |
| 2 | scartata | 250 | -295 |  | 214 | 104,743 | 1.99 |  | 2/4 |
| 3 | scartata | 250 | 1,227 | 0.15 | 214 | 96,439 | 1.77 | 1,186% | 2/4 |
| 4 | scartata | 250 | 2,537 | 0.37 | 214 | 81,466 | 1.55 | 416% | 2/4 |
| 5 | scartata | 250 | 1,779 | 0.30 | 214 | 22,938 | 0.37 | 121% | 1/4 |

**Finalista 1** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
breakout_offset_atr = 0.2
end_hour = 15
intraday_only = 1
max_bars = 0
ptn_dir_no = 53
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 55
skip_day = -1
start_hour = 7
stop_atr = 0
take_profit_atr = 4
```

- 2017-01-01 → 2019-02-07: 38 trade, -4,014
- 2019-02-07 → 2021-03-16: 49 trade, 33,040
- 2021-03-16 → 2023-04-23: 53 trade, -2,761
- 2023-04-23 → 2025-05-30: 73 trade, 77,120

**Finalista 2** — in campione in perdita (-295)

```
TimeframeMinutes = 15
breakout_offset_atr = 0.2
end_hour = 15
intraday_only = 1
max_bars = 0
ptn_dir_no = 53
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 55
skip_day = -1
start_hour = 7
stop_atr = 3
take_profit_atr = 4
```

- 2017-01-01 → 2019-02-07: 38 trade, -4,014
- 2019-02-07 → 2021-03-16: 49 trade, 33,040
- 2021-03-16 → 2023-04-23: 53 trade, -2,761
- 2023-04-23 → 2025-05-30: 73 trade, 77,120

**Finalista 3** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
breakout_offset_atr = 0.2
end_hour = 15
intraday_only = 1
max_bars = 0
ptn_dir_no = 53
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 55
skip_day = -1
start_hour = 7
stop_atr = 2.2
take_profit_atr = 4
```

- 2017-01-01 → 2019-02-07: 38 trade, -9,685
- 2019-02-07 → 2021-03-16: 49 trade, 33,040
- 2021-03-16 → 2023-04-23: 53 trade, -4,662
- 2023-04-23 → 2025-05-30: 73 trade, 76,405

**Finalista 4** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
breakout_offset_atr = 0.2
end_hour = 15
intraday_only = 1
max_bars = 0
ptn_dir_no = 53
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 55
skip_day = -1
start_hour = 7
stop_atr = 1.6
take_profit_atr = 4
```

- 2017-01-01 → 2019-02-07: 38 trade, -19,066
- 2019-02-07 → 2021-03-16: 49 trade, 28,741
- 2021-03-16 → 2023-04-23: 53 trade, -1,901
- 2023-04-23 → 2025-05-30: 73 trade, 72,201

**Finalista 5** — solo 1 finestre su 4 in utile

```
TimeframeMinutes = 15
breakout_offset_atr = 0.2
end_hour = 15
intraday_only = 1
max_bars = 0
ptn_dir_no = 53
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 55
skip_day = -1
start_hour = 7
stop_atr = 1.25
take_profit_atr = 4
```

- 2017-01-01 → 2019-02-07: 38 trade, -22,033
- 2019-02-07 → 2021-03-16: 49 trade, -1,007
- 2021-03-16 → 2023-04-23: 53 trade, -10,994
- 2023-04-23 → 2025-05-30: 73 trade, 55,535

## Esito: nessuna finalista sopravvive alla validazione fuori campione.

## Verdetto (27/09/2026, lettura con `lettura-risultati`)

**Confrontabilita'.** Stesso feed interno e stesso split (2008-2016 ricerca, 2017 → 30/05/2025
verifica) della replica della configurazione della consegna `NQ-15M-BOS`, costi FTMO veri. Griglie
ricostruite dai valori delle 224 (deviazione dichiarata in `Pt5DavSweepSpaces`).

**Cosa regge.** Poco. La finalista 1 (finestra 07-15, target 4 ATR, stop 0) in campione fa +3.910,
meglio della consegna (-22.587). Finaliste 1 e 2 (stop 0 e stop 3) sono identiche trade per trade
fuori campione: lo stop a 3 ATR probabilmente non scatta mai, da verificare.

**Cosa non regge** (finalista 1):

| criterio | valore | soglia |
|---|---:|---:|
| trade in campione | 250 | >= 250, sul minimo |
| average trade in campione | ~16 | 120 (NQ 15m) |
| netto/DD in campione | 0,61 | >= 1 |
| UngerFit | ~0,18 | >= 1 |
| guadagno annuo / DD | ~0,07 | >= 2 |
| finestre fuori campione in utile | 2/4 | >= 3/4 |

- Il motore nudo perde (fase 1: -43.178 su 1.133 trade, PF 0,84): tutto il margine sta nella
  finestra e nei filtri, che lo portano appena in pareggio.
- **Segnale di regime.** I +104.743 fuori campione vengono da 2019-2021 (+33.040) e 2023-2025
  (+77.120); le altre due finestre perdono. Stessa forma per tutte e cinque le finaliste e, piu'
  accentuata, per la consegna (-22.587 dentro, +208.460 fuori). Average trade fuori (~490) trenta
  volte quello dentro: e' il Nasdaq 2019-2025, non una configurazione.

**Decisione.** Nessuna classe dalla sweep. `PT5DAV_NQ_BOS_001_15` (nel piano 3 di
`piani-onesti-2026-09-27`) va tolta dal bacino dei piani bilanciati: sugli anni in cui non e' stata
scelta non ha vantaggio. `BOS_002_30` e `BOS_003_60` non sono misurate qui e non vanno date per buone.
Si aspetta la sweep P5-VBO sullo stesso NQ 15m prima di toccare O3: se ripete la forma, il problema
e' di tutta la serie PT5DAV su NQ, non del solo motore BOS.

### La classe `PT5DAV_NQ_BOS_001_15`, misurata verbatim (28/09/2026)

`piootoo-sweep --strategy PT5DAV_NQ_BOS_001_15 --params "TimeframeMinutes=15"`, stessi feed, split e
costi: **1.595 trade e -22.587 in campione, 1.410 trade e +208.460 fuori**, cioe' esattamente la
replica della configurazione della consegna. Fuori campione 4 finestre su 4 in utile (+14.209,
+70.717, +28.546, +96.916), ma sui nove anni in cui non e' stata scelta perde su 1.595 trade: non e'
un campione piccolo, e' l'assenza di vantaggio. **Confermato: fuori dal bacino dei piani bilanciati.**
Log in `pt5dav-nq-bos-001-15-classe.log`.