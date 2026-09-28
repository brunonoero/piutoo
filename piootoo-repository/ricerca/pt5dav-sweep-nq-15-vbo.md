# Sweep @NQ 15m — P5-VBO

- Strategia di partenza: `RC5_VBO`
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
- Durata: 1,013.0 minuti

## Fasi

| fase | combinazioni | ammissibili | minuti | migliore in campione |
|---|---:|---:|---:|---|
| 1 trigger | 1,512 | 1,184 | 383.4 | RC5_VBO: 927 trade, netto -38,526, DD 51,374, PF 0.83, 117,876 ms |
| 2 finestra | 625 | 1,090 | 347.1 | RC5_VBO: 453 trade, netto -11,499, DD 17,030, PF 0.89, 137,059 ms |
| 3 stop | 16 | 32 | 7.8 | RC5_VBO: 453 trade, netto -5,452, DD 11,492, PF 0.91, 109,919 ms |
| 4 tenuta | 12 | 20 | 7.8 | RC5_VBO: 453 trade, netto -11,499, DD 17,030, PF 0.89, 138,419 ms |
| 5 YES neutro | 55 | 44 | 40.1 | RC5_VBO: 437 trade, netto -7,359, DD 14,274, PF 0.92, 145,229 ms |
| 6 NO neutro | 55 | 63 | 39.8 | RC5_VBO: 408 trade, netto -3,714, DD 11,288, PF 0.96, 164,096 ms |
| 7 YES direzionale | 103 | 24 | 71.5 | RC5_VBO: 408 trade, netto -3,714, DD 11,288, PF 0.96, 137,115 ms |
| 7 NO direzionale | 103 | 156 | 72.3 | RC5_VBO: 299 trade, netto 2,603, DD 9,204, PF 1.04, 103,897 ms |
| 8 calendario | 6 | 7 | 3.7 | RC5_VBO: 299 trade, netto 2,603, DD 9,204, PF 1.04, 108,272 ms |
| 9 target | 13 | 26 | 8.2 | RC5_VBO: 299 trade, netto 2,603, DD 9,204, PF 1.04, 131,010 ms |
| 10 stop finale | 16 | 32 | 9.9 | RC5_VBO: 299 trade, netto 2,603, DD 9,204, PF 1.04, 152,203 ms |

## Finaliste

| # | esito | IS trade | IS netto | IS punteggio | OOS trade | OOS netto | OOS punteggio | tenuta | finestre |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | scartata | 299 | 2,603 | 0.28 | 250 | 44,920 | 1.99 | 703% | 2/4 |
| 2 | scartata | 299 | 2,603 | 0.28 | 250 | 44,920 | 1.99 | 703% | 2/4 |
| 3 | scartata | 299 | 2,603 | 0.28 | 250 | 44,920 | 1.99 | 703% | 2/4 |
| 4 | scartata | 299 | 2,578 | 0.28 | 250 | 38,117 | 1.69 | 602% | 2/4 |
| 5 | scartata | 299 | 1,875 | 0.20 | 250 | 40,237 | 1.78 | 874% | 2/4 |

**Finalista 1** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
atr_len = 10
direction = 2
end_hour = 17
intraday_only = 1
max_bars = 0
ptn_dir_no = -45
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 41
skip_day = -1
start_hour = 14
stop_atr = 0
take_profit_atr = 0
vol_mult = 0.5
vol_mult_short = 6
vol_source = 3
```

- 2017-01-01 → 2019-02-07: 78 trade, -6,321
- 2019-02-07 → 2021-03-16: 42 trade, -8,938
- 2021-03-16 → 2023-04-23: 56 trade, 43,425
- 2023-04-23 → 2025-05-30: 73 trade, 16,094

**Finalista 2** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
atr_len = 10
direction = 2
end_hour = 17
intraday_only = 1
max_bars = 0
ptn_dir_no = -45
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 41
skip_day = -1
start_hour = 14
stop_atr = 2.2
take_profit_atr = 0
vol_mult = 0.5
vol_mult_short = 6
vol_source = 3
```

- 2017-01-01 → 2019-02-07: 78 trade, -6,321
- 2019-02-07 → 2021-03-16: 42 trade, -8,938
- 2021-03-16 → 2023-04-23: 56 trade, 43,425
- 2023-04-23 → 2025-05-30: 73 trade, 16,094

**Finalista 3** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
atr_len = 10
direction = 2
end_hour = 17
intraday_only = 1
max_bars = 0
ptn_dir_no = -45
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 41
skip_day = -1
start_hour = 14
stop_atr = 3
take_profit_atr = 0
vol_mult = 0.5
vol_mult_short = 6
vol_source = 3
```

- 2017-01-01 → 2019-02-07: 78 trade, -6,321
- 2019-02-07 → 2021-03-16: 42 trade, -8,938
- 2021-03-16 → 2023-04-23: 56 trade, 43,425
- 2023-04-23 → 2025-05-30: 73 trade, 16,094

**Finalista 4** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
atr_len = 10
direction = 2
end_hour = 17
intraday_only = 1
max_bars = 0
ptn_dir_no = -45
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 41
skip_day = -1
start_hour = 14
stop_atr = 1.6
take_profit_atr = 0
vol_mult = 0.5
vol_mult_short = 6
vol_source = 3
```

- 2017-01-01 → 2019-02-07: 78 trade, -6,321
- 2019-02-07 → 2021-03-16: 42 trade, -8,377
- 2021-03-16 → 2023-04-23: 56 trade, 43,425
- 2023-04-23 → 2025-05-30: 73 trade, 8,727

**Finalista 5** — solo 2 finestre su 4 in utile

```
TimeframeMinutes = 15
atr_len = 10
direction = 2
end_hour = 17
intraday_only = 1
max_bars = 0
ptn_dir_no = -45
ptn_dir_yes = 52
ptn_neut_no = 9
ptn_neut_yes = 41
skip_day = -1
start_hour = 14
stop_atr = 1.15
take_profit_atr = 0
vol_mult = 0.5
vol_mult_short = 6
vol_source = 3
```

- 2017-01-01 → 2019-02-07: 78 trade, -5,147
- 2019-02-07 → 2021-03-16: 42 trade, -6,998
- 2021-03-16 → 2023-04-23: 56 trade, 43,425
- 2023-04-23 → 2025-05-30: 73 trade, 8,280

## Esito: nessuna finalista sopravvive alla validazione fuori campione.

## Verdetto (28/09/2026, lettura con `lettura-risultati`)

**Confrontabilita'.** Stessi feed, split, costi e criterio della sweep P5-BOS
(`pt5dav-sweep-nq-15-bos.md`). Manca la replica della configurazione della consegna `NQ-15M-VBO`
sugli stessi anni: il confronto con la consegna qui non c'e', va fatto a parte.

**Cosa regge.** Niente di operativo. La ricerca converge su una sola regione: **solo short**
(`direction = 2`), finestra 14-17, `vol_mult_short = 6`, senza target. Le finaliste 1, 2 e 3 (stop 0,
2,2 e 3 ATR) sono identiche trade per trade, dentro e fuori: lo stop non scatta mai.

**Cosa non regge** (finalista 1):

| criterio | valore | soglia |
|---|---:|---:|
| trade in campione | 299 | >= 250 |
| average trade in campione | ~9 | 120 (NQ 15m) |
| netto/DD in campione | 0,28 | >= 1 |
| UngerFit | ~0,08 | >= 1 |
| guadagno annuo / DD | ~0,03 | >= 2 |
| finestre fuori campione in utile | 2/4 | >= 3/4 |

- Il motore nudo perde (fase 1: -38.526 su 927 trade, PF 0,83), e dopo la finestra ancora -11.499:
  il pareggio arriva solo con i pattern direzionali della fase 7.
- **Segnale di regime, di segno opposto alla BOS.** Dei +44.920 fuori campione, +43.425 (97%) vengono
  dalla finestra 03/2021 → 04/2023, cioe' dal ribasso del Nasdaq del 2022, e la finalista e' solo
  short. Le finestre 2017-2019 e 2019-2021 perdono. La BOS guadagnava nei rialzi (2019-2021,
  2023-2025), la VBO nel ribasso: due scommesse sul regime, nessuna delle due con un vantaggio sugli
  anni in cui e' stata cercata.

**Decisione.** Nessuna classe dalla sweep. Due motori PT5DAV su due a NQ 15m senza vantaggio in
2008-2016: il sospetto si allarga a tutta la serie PT5DAV su NQ. `PT5DAV_NQ_VBO_001_15` sta nel piano 2
di `piani-onesti-2026-09-27` e in `piani-congelati-2025-09-01`: prima di toglierla va misurata la sua
configurazione, verbatim, sul 2008-2016 del feed interno (la sweep misura il motore, non la classe).
La sweep P5-RHL decide il secondo membro NQ di O3 (`PT5DAV_NQ_RHL_002_15`).

### La classe `PT5DAV_NQ_VBO_001_15`, misurata verbatim (28/09/2026)

Stessa misura della BOS: **85 trade e +4.333 in campione, 248 trade e +106.289 fuori**, 4 finestre su
4 in utile (+7.210, +28.115, +36.190, +35.137). La classe sta in una regione diversa da quella che la
sweep trova, e **non e' smentita**: in campione e' in utile. Ma 85 trade in nove anni sono sotto i 250
con cui si giudica, e fuori campione i trade triplicano (28 → 81 per finestra) mentre l'utile medio
resta fra 250 e 480: la frequenza segue la volatilita' del Nasdaq, e il campione in cui la strategia
non e' stata scelta e' troppo piccolo per dire se il vantaggio c'era gia'. **Proposta (da confermare):
resta nel piano 2, a peso ridotto quando si pesano i piani, non come membro portante.** Log in
`pt5dav-nq-vbo-001-15-classe.log`.