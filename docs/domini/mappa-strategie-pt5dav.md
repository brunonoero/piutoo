# Mappa delle strategie PT5DAV

La serie `PT5DAV_*` porta in C# le strategie della ricerca v5.0 fatta su un altro server con un
motore Python (consegna del 23/09/2026, `piootoo-repository/PT5DAV/`). Stato al 24/09/2026:
**218 classi** in `Piootoo.Strategies/PT5DAVStrategies/`, riconciliate trade per trade sul periodo
`broker` con il minuto FTMO, e su NQ anche sul feed interno 2022-2025.

## Da dove vengono e cosa e' entrato

La consegna ha 224 strategie (`strategie_224.csv`, schede in `schede_224.md`, trade del simulatore in
`trades_per_strategia/`). Sono entrate tutte tranne le **6 FDAX a 4 ore**: la ricerca le ha su barre
08-12/12-16/16-20/20-22, che il feed ancorato all'01:00 e il cBot non costruiscono.

Il primo perimetro erano le **135** che non tengono piu' di una notte (notti **vere**, rollover delle
17:00 NY attraversati: la colonna `notti` dei CSV conta gli swap e il triplo ne fa tre); lo stesso
giorno la serie e' stata estesa alle altre 89 (overnight da 2 a 10 giornate, MAC, BIASW). Quante notti
un conto possa tenere lo decide il piano (`AllowOvernight`/`AllowOverweek`), non la strategia.

Nome: `PT5DAV_{SIMBOLO}_{SIGLA}_{NNN}_{TF}`. Il progressivo e' per (simbolo, sigla), in ordine di
timeframe e codice, **a fasi**: prima le 135, poi le altre 89, infine le FDAX a 4 ore (numerate ma non
generate) — cosi' un'estensione non rinomina mai una classe esistente. Sigle: TFM, TFU, PCH, SBO (BO),
BOS (BO_S), VBO, LFD (LF), LFH (LF_HL), RHL, RBM, RBU, BIA (BIAS), BRT (BIAS_RT), BBO (BIAS_BO), MAC,
BSW (BIASW). Ogni classe dichiara `ResearchCode`, il codice della consegna.

Le classi si **generano** dal CSV, parametri verbatim, con l'estratto della scheda nel commento:
`python tools/pt5dav/gen_pt5dav.py ALL` (primo perimetro in `tools/pt5dav/keep135.txt`). Rigenerarle
e' il modo di cambiarle: a mano si perde la garanzia del verbatim.

## Motori propri, sulla base comune

I motori condivisi con le PT3B (`Easy/Engines`) non si toccano. Le PT5DAV hanno motori propri in
`PT5DAVStrategies/Engines/` sulla base `Pt5DavEngineBase`; formule dei livelli e pattern sono gli
stessi (i pattern vengono da `EasyLib`). La base aggiunge cio' che la ricerca fa e i motori condivisi
non sanno dichiarare:

- **ATR50 di Wilder** sulle sessioni chiuse, per stop, target e offset dei livelli, tenuto come stato
  incrementale (`Pt5DavSessionState`, viaggia come `RuntimeState`). Misurato: sui 6.728 trade SL di
  NQ la distanza di stop / (stop_atr × ATR50) ha mediana 1,0045; la media semplice non torna.
- **Rodaggio**: nessun ingresso senza ATR50 (nessun ripiego sul denaro fisso, che varrebbe zero).
- **Sessione della ricerca**: giorno di Roma dall'ancoraggio, con le barre della domenica sera nella
  sessione del lunedi' (BTC, aperto 7 giorni su 7, ha sabato e domenica come sessioni proprie); DAX
  solo fra le 08:00 e le 22:00 con sessione dalle 08:00 (`OverrideSessionAnchor`). Il fuso e' **Roma**
  per sessione, finestra e giorni; **New York** solo per gli orari del CFD.
- **Uscita intraday** alla chiusura dell'ultima barra che finisce entro il limite del CFD (16:50 NY;
  17:00 BP e BTC; 13:30 CC e KC; il DAX alle 22:00).
- **Overnight**: `max_bars` della ricerca e le uscite proprie del motore, nient'altro.

**La giornata di New York come sessione e' stata provata e scartata** (24/09/2026): con la sessione
dalle 17:00 NY BP-15M-BIAS, che conta le barre della sessione, passava dal 93% al 3% dei trade
ritrovati. Lo scarto di BP che l'aveva suggerita era il calendario (sotto).

## Il calendario di BP e BTC segue il CFD

Dal 24/09/2026 `market-calendars.json` dichiara BP con una finestra continua dalla domenica alle
17:00 NY al venerdi' alle 17:00 NY (senza la pausa CME) e BTC 24/7 senza giorni dichiarati, come ETH,
SOL e XRP. E' una scelta di simbolo e non di strategia, perche' il calendario si applica per simbolo
in sei punti (feed, archivio del broker, backtest, sweep, sessione, descrittore del cBot); oggi nessuna
altra strategia eseguibile gira su BP o BTC. Motivo: la ricerca ha verificato le strategie sul broker
con il CFD sempre aperto (CONVENZIONI §6), e BP-30M-RHL opera solo nell'ora 17-18 NY — con la pausa
CME ritrovava lo 0% dei trade. Gli aggregati FTMO dei due simboli sono stati ricostruiti con le
finestre nuove.

## Regole misurate

Ognuna e' nel codice con il numero che la giustifica.

| regola | dove | misura |
|---|---|---|
| etichetta: finestra e giorni letti sulla **chiusura** della barra (il default del sistema, non `ResearchLabelsBarsOnOpen`) | base | BP-15M-BOS: ingressi dalle 11:00 alle 13:00 incluse con finestra 11-13 |
| deviazione delle Bollinger **di popolazione** (divisa per N) | RBB | 1.313/1.319, 1.539/1.544, 539/539, 741/742 ingressi al tick; con N−1 quasi nessuno |
| `max_bars` **non** conta la barra d'ingresso | base | 105/116 uscite una barra prima senza |
| uscita BIAS alla chiusura della barra `lx_bar`, mai sulla barra d'ingresso | BIAS | 249 uscite anticipate su NQ-4H-BIASBO senza |
| estremi BIAS_BO/RT su barre chiuse **prima** della barra di segnale | BIAS | 2.506/2.511 ingressi |
| la finestra BIAS si disarma a ogni inizio sessione, anche se "attraversa il cambio di sessione" | BIAS | FDAX-15M-BIASRT: 188 ingressi la mattina dopo senza |
| nessun ingresso emesso mentre si e' in posizione (niente rientro sulla barra dell'uscita) | base | NQ-15M-BOS da 91% a 98% |
| livello gia' superato quando l'ordine nasce: ingresso **a mercato** all'apertura della barra (`CrossedLevelPolicy.Market`, 7.7.0), non scarto | base | 41 con tenuta >= 1,5: 2.571 trade scartando, 2.800 a mercato, Python 2.695; GC-15M-TFU 10 contro 81 |
| CFD aperto all'**apertura** della barra d'ingresso; per le intraday anche alla **chiusura** | base | NQ 4h: prima barra mai (salvo ora legale sfasata); 20:00 vietata alle intraday, permessa a TF_M 4h (47 ingressi) |
| MAC: uscita per incrocio inverso alla **chiusura** della barra dopo il segnale (l'ingresso e' all'apertura) | MAC | NQ-15M-MAC: 74/74 uscite |
| MAC: uscita di fine settimana al limite del CFD del venerdi'; a 4 ore alla chiusura della barra di giovedi' 20:00 | MAC | tutte le 23 MAC; a 4 ore BP 140/266, GC 106/122 (regola misurata, la scheda non la spiega) |
| BIASW: ingresso all'apertura della barra che **apre** a `le_time`, uscita alla chiusura di quella che **finisce** a `lx_time` | BIASW | 676/676 su NQ-4H; la scheda scrive l'ingresso una barra prima |

## Riconciliazione

`Pt5DavParityStudy` (studio, `PIOOTOO_STUDI=1`) esegue le strategie con il motore vero, orologio al
minuto, senza costi e con `RejectWrongSideLevels` spento, e le confronta con i trade Python. Resoconti
in `piootoo-repository/PT5DAV/verifica/`.

**Periodo `broker`** (27/08/2025 → 09/09/2026, minuto FTMO, prezzi veri), mediana per strategia dei
trade Python ritrovati: CL 100%, YM 100%, GC 99%, ES 98%, FDAX 98%, BP 97%, NQ 96%, BTC 92%, KC 78%
(poche strategie con pochi trade). Ingresso al tick.

**Feed interno 2022-2025, solo NQ**: mediana 96%. Il feed interno e' la serie continua **aggiustata**,
e le soglie in percentuale dei pattern (neutri 31-40, direzionali 15/28/29/31/47/51/-24...) la ricerca
le calcola sul prezzo vero: li' scattano una barra dopo. In live non succede.

Scarti noti: a parita' di ingressi stop e target scattano a volte in un minuto diverso (granularita'
del minuto fra i due feed; su NQ-15M-BOS in FTMO +878 punti Python contro −67 nostri); e le strategie
che tengono a lungo senza filtri si separano a catena da una sola uscita diversa (GC-4H-BO_S 45%).

## Le classi

| classe | codice della ricerca | motore | tenuta | trade storia | trade broker |
|---|---|---|---|---:|---:|
| `PT5DAV_BP_BIA_001_15` | `BP-15M-BIAS-d2e4b2` | BIAS | del motore | 2727 | 206 |
| `PT5DAV_BP_BOS_001_15` | `BP-15M-BOS-974b9e` | BO_S | intraday | 2296 | 143 |
| `PT5DAV_BP_BOS_002_30` | `BP-30M-BOS-feff2b` | BO_S | intraday | 103 | 14 |
| `PT5DAV_BP_BRT_001_60` | `BP-1H-BIASRT-8577be` | BIAS_RT | del motore | 2051 | 159 |
| `PT5DAV_BP_LFD_001_60` | `BP-1H-LF-65aab4` | LF | overnight 1 giornate | 130 | 10 |
| `PT5DAV_BP_LFD_002_240` | `BP-4H-LF-f437ae` | LF | overnight 1 giornate | 66 | 5 |
| `PT5DAV_BP_LFD_003_15` | `BP-15M-LF-609b39` | LF | overnight 5 giornate | 52 | 3 |
| `PT5DAV_BP_LFD_004_30` | `BP-30M-LF-68837b` | LF | overnight 2 giornate | 400 | 39 |
| `PT5DAV_BP_LFH_001_15` | `BP-15M-LFHL-c6a4d3` | LF_HL | overnight 1 giornate | 420 | 17 |
| `PT5DAV_BP_LFH_002_30` | `BP-30M-LFHL-561a48` | LF_HL | intraday | 827 | 67 |
| `PT5DAV_BP_LFH_003_60` | `BP-1H-LFHL-a6a2ea` | LF_HL | overnight 1 giornate | 346 | 30 |
| `PT5DAV_BP_MAC_001_15` | `BP-15M-MAC-992e74` | MAC | del motore | 506 | 52 |
| `PT5DAV_BP_MAC_002_30` | `BP-30M-MAC-7816d1` | MAC | del motore | 341 | 31 |
| `PT5DAV_BP_MAC_003_240` | `BP-4H-MAC-6e4345` | MAC | del motore | 267 | 19 |
| `PT5DAV_BP_PCH_001_30` | `BP-30M-PC-1a7d28` | PC | intraday | 3496 | 269 |
| `PT5DAV_BP_PCH_002_60` | `BP-1H-PC-8146e0` | PC | overnight 1 giornate | 239 | 12 |
| `PT5DAV_BP_PCH_003_240` | `BP-4H-PC-e9a443` | PC | intraday | 265 | 14 |
| `PT5DAV_BP_RBM_001_15` | `BP-15M-RBBM-8c9611` | RBB_M | intraday | 588 | 38 |
| `PT5DAV_BP_RBM_002_30` | `BP-30M-RBBM-014ead` | RBB_M | overnight 5 giornate | 1539 | 109 |
| `PT5DAV_BP_RBU_001_240` | `BP-4H-RBBU-ed6ea7` | RBB_U | overnight 1 giornate | 356 | 31 |
| `PT5DAV_BP_RBU_002_15` | `BP-15M-RBBU-75b198` | RBB_U | overnight 10 giornate | 1258 | 90 |
| `PT5DAV_BP_RHL_001_30` | `BP-30M-RHL-9434f4` | RHL | overnight 5 giornate | 50 | 57 |
| `PT5DAV_BP_SBO_001_30` | `BP-30M-BO-d0b40f` | BO | intraday | 516 | 18 |
| `PT5DAV_BP_TFM_001_30` | `BP-30M-TFM-ca5773` | TF_M | intraday | 160 | 4 |
| `PT5DAV_BP_TFM_002_60` | `BP-1H-TFM-056220` | TF_M | intraday | 2045 | 162 |
| `PT5DAV_BTC_BBO_001_60` | `BTC-1H-BIASBO-13c5d9` | BIAS_BO | del motore | 683 | 138 |
| `PT5DAV_BTC_BOS_001_240` | `BTC-4H-BOS-a0b818` | BO_S | overnight 3 giornate | 485 | 103 |
| `PT5DAV_BTC_LFD_001_15` | `BTC-15M-LF-e49442` | LF | overnight 1 giornate | 165 | 36 |
| `PT5DAV_BTC_LFD_002_30` | `BTC-30M-LF-fc1ecc` | LF | overnight 1 giornate | 128 | 26 |
| `PT5DAV_BTC_LFD_003_60` | `BTC-1H-LF-a5bd10` | LF | intraday | 575 | 139 |
| `PT5DAV_BTC_LFD_004_240` | `BTC-4H-LF-8508b9` | LF | intraday | 56 | 15 |
| `PT5DAV_BTC_LFH_001_15` | `BTC-15M-LFHL-f87511` | LF_HL | intraday | 169 | 14 |
| `PT5DAV_BTC_LFH_002_30` | `BTC-30M-LFHL-336fad` | LF_HL | intraday | 151 | 23 |
| `PT5DAV_BTC_LFH_003_240` | `BTC-4H-LFHL-eeb933` | LF_HL | intraday | 246 | 45 |
| `PT5DAV_BTC_LFH_004_60` | `BTC-1H-LFHL-66e894` | LF_HL | overnight 5 giornate | 57 | 17 |
| `PT5DAV_BTC_MAC_001_30` | `BTC-30M-MAC-8c84eb` | MAC | del motore | 336 | 72 |
| `PT5DAV_BTC_MAC_002_60` | `BTC-1H-MAC-59418e` | MAC | del motore | 326 | 72 |
| `PT5DAV_BTC_MAC_003_240` | `BTC-4H-MAC-f77a19` | MAC | del motore | 67 | 9 |
| `PT5DAV_BTC_PCH_001_60` | `BTC-1H-PC-cf9d4f` | PC | intraday | 183 | 35 |
| `PT5DAV_BTC_PCH_002_30` | `BTC-30M-PC-c8276f` | PC | overnight 10 giornate | 213 | 37 |
| `PT5DAV_BTC_PCH_003_240` | `BTC-4H-PC-c81cdb` | PC | overnight 10 giornate | 83 | 12 |
| `PT5DAV_BTC_RBM_001_15` | `BTC-15M-RBBM-8b34eb` | RBB_M | overnight 1 giornate | 72 | 287 |
| `PT5DAV_BTC_SBO_001_30` | `BTC-30M-BO-284f81` | BO | overnight 2 giornate | 416 | 65 |
| `PT5DAV_BTC_SBO_002_60` | `BTC-1H-BO-4e41e2` | BO | overnight 2 giornate | 262 | 36 |
| `PT5DAV_BTC_SBO_003_240` | `BTC-4H-BO-b0c1fe` | BO | overnight 10 giornate | 140 | 45 |
| `PT5DAV_BTC_TFM_001_15` | `BTC-15M-TFM-a0a3a2` | TF_M | overnight 10 giornate | 486 | 101 |
| `PT5DAV_BTC_TFM_002_30` | `BTC-30M-TFM-9efa9d` | TF_M | overnight 10 giornate | 434 | 114 |
| `PT5DAV_BTC_TFM_003_60` | `BTC-1H-TFM-613144` | TF_M | overnight 5 giornate | 476 | 71 |
| `PT5DAV_BTC_TFU_001_240` | `BTC-4H-TFU-82f8b9` | TF_U | overnight 10 giornate | 612 | 138 |
| `PT5DAV_BTC_VBO_001_30` | `BTC-30M-VBO-987c5a` | VBO | overnight 10 giornate | 51 | 12 |
| `PT5DAV_BTC_VBO_002_240` | `BTC-4H-VBO-6983da` | VBO | overnight 5 giornate | 110 | 26 |
| `PT5DAV_CC_RBM_001_15` | `CC-15M-RBBM-c14423` | RBB_M | overnight 10 giornate | 116 | 8 |
| `PT5DAV_CL_BOS_001_30` | `CL-30M-BOS-a2d18a` | BO_S | overnight 1 giornate | 52 | 9 |
| `PT5DAV_CL_BSW_001_30` | `CL-30M-BIASW-b1dbc4` | BIASW | del motore | 632 | 43 |
| `PT5DAV_CL_LFD_001_240` | `CL-4H-LF-1010b4` | LF | intraday | 53 | 6 |
| `PT5DAV_CL_LFD_002_60` | `CL-1H-LF-5c157f` | LF | overnight 5 giornate | 52 | 13 |
| `PT5DAV_CL_LFH_001_30` | `CL-30M-LFHL-238a0d` | LF_HL | overnight 1 giornate | 57 | 13 |
| `PT5DAV_CL_LFH_002_60` | `CL-1H-LFHL-c58d20` | LF_HL | intraday | 50 | 7 |
| `PT5DAV_CL_LFH_003_240` | `CL-4H-LFHL-1670d8` | LF_HL | intraday | 60 | 7 |
| `PT5DAV_CL_LFH_004_15` | `CL-15M-LFHL-dfb899` | LF_HL | overnight 10 giornate | 54 | 12 |
| `PT5DAV_CL_MAC_001_30` | `CL-30M-MAC-dc3581` | MAC | del motore | 243 | 20 |
| `PT5DAV_CL_MAC_002_240` | `CL-4H-MAC-766ad5` | MAC | del motore | 107 | 5 |
| `PT5DAV_CL_PCH_001_30` | `CL-30M-PC-65acec` | PC | overnight 1 giornate | 114 | 10 |
| `PT5DAV_CL_RBM_001_15` | `CL-15M-RBBM-357d53` | RBB_M | intraday | 130 | 52 |
| `PT5DAV_CL_RHL_001_30` | `CL-30M-RHL-8b2671` | RHL | overnight 5 giornate | 80 | 15 |
| `PT5DAV_CL_TFM_001_60` | `CL-1H-TFM-3865b4` | TF_M | intraday | 87 | 14 |
| `PT5DAV_CL_VBO_001_30` | `CL-30M-VBO-ac297d` | VBO | overnight 1 giornate | 237 | 29 |
| `PT5DAV_CL_VBO_002_60` | `CL-1H-VBO-d7d890` | VBO | overnight 1 giornate | 51 | 9 |
| `PT5DAV_ES_BOS_001_15` | `ES-15M-BOS-a44bd3` | BO_S | intraday | 1014 | 78 |
| `PT5DAV_ES_BOS_002_30` | `ES-30M-BOS-0629bd` | BO_S | intraday | 537 | 54 |
| `PT5DAV_ES_BOS_003_60` | `ES-1H-BOS-a4bd8a` | BO_S | intraday | 3053 | 237 |
| `PT5DAV_ES_BSW_001_15` | `ES-15M-BIASW-4870eb` | BIASW | del motore | 674 | 48 |
| `PT5DAV_ES_LFD_001_30` | `ES-30M-LF-44d4f3` | LF | overnight 1 giornate | 302 | 41 |
| `PT5DAV_ES_LFD_002_60` | `ES-1H-LF-065db7` | LF | overnight 1 giornate | 569 | 44 |
| `PT5DAV_ES_LFH_001_60` | `ES-1H-LFHL-28652f` | LF_HL | intraday | 54 | 1 |
| `PT5DAV_ES_LFH_002_15` | `ES-15M-LFHL-712658` | LF_HL | overnight 2 giornate | 62 | 3 |
| `PT5DAV_ES_LFH_003_30` | `ES-30M-LFHL-a1480e` | LF_HL | overnight 2 giornate | 121 | 14 |
| `PT5DAV_ES_MAC_001_30` | `ES-30M-MAC-1dbff7` | MAC | del motore | 648 | 41 |
| `PT5DAV_ES_MAC_002_60` | `ES-1H-MAC-8e0899` | MAC | del motore | 297 | 14 |
| `PT5DAV_ES_MAC_003_240` | `ES-4H-MAC-ff6723` | MAC | del motore | 89 | 7 |
| `PT5DAV_ES_PCH_001_15` | `ES-15M-PC-2a3e7d` | PC | intraday | 878 | 72 |
| `PT5DAV_ES_PCH_002_30` | `ES-30M-PC-4cb3dc` | PC | intraday | 1162 | 74 |
| `PT5DAV_ES_PCH_003_60` | `ES-1H-PC-a19ab5` | PC | intraday | 692 | 66 |
| `PT5DAV_ES_PCH_004_240` | `ES-4H-PC-3671de` | PC | overnight 5 giornate | 327 | 29 |
| `PT5DAV_ES_RBM_001_30` | `ES-30M-RBBM-2bd92b` | RBB_M | overnight 1 giornate | 1017 | 90 |
| `PT5DAV_ES_RBU_001_30` | `ES-30M-RBBU-b45540` | RBB_U | intraday | 2665 | 200 |
| `PT5DAV_ES_RBU_002_60` | `ES-1H-RBBU-80478d` | RBB_U | intraday | 1168 | 85 |
| `PT5DAV_ES_RHL_001_30` | `ES-30M-RHL-a8743f` | RHL | overnight 3 giornate | 413 | 34 |
| `PT5DAV_ES_RHL_002_60` | `ES-1H-RHL-e0bf51` | RHL | overnight 3 giornate | 173 | 7 |
| `PT5DAV_ES_SBO_001_15` | `ES-15M-BO-289086` | BO | intraday | 100 | 8 |
| `PT5DAV_ES_SBO_002_240` | `ES-4H-BO-d4c2e5` | BO | intraday | 71 | 11 |
| `PT5DAV_ES_TFM_001_15` | `ES-15M-TFM-8a67d7` | TF_M | overnight 2 giornate | 2240 | 167 |
| `PT5DAV_ES_VBO_001_30` | `ES-30M-VBO-b11457` | VBO | intraday | 346 | 27 |
| `PT5DAV_ES_VBO_002_60` | `ES-1H-VBO-195d04` | VBO | intraday | 791 | 61 |
| `PT5DAV_FDAX_BOS_001_15` | `FDAX-15M-BOS-3fefca` | BO_S | overnight 1 giornate | 1968 | 140 |
| `PT5DAV_FDAX_BOS_002_30` | `FDAX-30M-BOS-955403` | BO_S | intraday | 1837 | 151 |
| `PT5DAV_FDAX_BOS_003_60` | `FDAX-1H-BOS-541680` | BO_S | intraday | 1943 | 143 |
| `PT5DAV_FDAX_BRT_001_15` | `FDAX-15M-BIASRT-c2029f` | BIAS_RT | del motore | 3286 | 240 |
| `PT5DAV_FDAX_LFD_001_15` | `FDAX-15M-LF-b449a3` | LF | overnight 5 giornate | 488 | 41 |
| `PT5DAV_FDAX_LFD_002_30` | `FDAX-30M-LF-3bd3fa` | LF | overnight 2 giornate | 132 | 7 |
| `PT5DAV_FDAX_LFD_003_60` | `FDAX-1H-LF-03a541` | LF | overnight 10 giornate | 194 | 9 |
| `PT5DAV_FDAX_LFD_004_240` *(esclusa: FDAX 4h)* | `FDAX-4H-LF-cc1098` | LF | intraday | 94 | 4 |
| `PT5DAV_FDAX_LFH_001_15` | `FDAX-15M-LFHL-7aed56` | LF_HL | intraday | 486 | 44 |
| `PT5DAV_FDAX_LFH_002_60` | `FDAX-1H-LFHL-e4baa9` | LF_HL | overnight 1 giornate | 195 | 11 |
| `PT5DAV_FDAX_LFH_003_240` *(esclusa: FDAX 4h)* | `FDAX-4H-LFHL-3605bf` | LF_HL | overnight 3 giornate | 114 | 6 |
| `PT5DAV_FDAX_MAC_001_15` | `FDAX-15M-MAC-bb1459` | MAC | del motore | 614 | 48 |
| `PT5DAV_FDAX_MAC_002_30` | `FDAX-30M-MAC-9c292b` | MAC | del motore | 928 | 65 |
| `PT5DAV_FDAX_MAC_003_60` | `FDAX-1H-MAC-ba5e30` | MAC | del motore | 313 | 28 |
| `PT5DAV_FDAX_PCH_001_30` | `FDAX-30M-PC-aa3ddd` | PC | intraday | 682 | 50 |
| `PT5DAV_FDAX_PCH_002_60` | `FDAX-1H-PC-22b63c` | PC | intraday | 718 | 58 |
| `PT5DAV_FDAX_PCH_003_240` *(esclusa: FDAX 4h)* | `FDAX-4H-PC-3a3d21` | PC | overnight 3 giornate | 785 | 61 |
| `PT5DAV_FDAX_RBM_001_240` *(esclusa: FDAX 4h)* | `FDAX-4H-RBBM-7c347c` | RBB_M | overnight 1 giornate | 209 | 10 |
| `PT5DAV_FDAX_RHL_001_15` | `FDAX-15M-RHL-d6f71d` | RHL | overnight 2 giornate | 315 | 21 |
| `PT5DAV_FDAX_RHL_002_60` | `FDAX-1H-RHL-38fb85` | RHL | overnight 3 giornate | 464 | 23 |
| `PT5DAV_FDAX_RHL_003_240` *(esclusa: FDAX 4h)* | `FDAX-4H-RHL-9fb44d` | RHL | overnight 10 giornate | 120 | 9 |
| `PT5DAV_FDAX_SBO_001_30` | `FDAX-30M-BO-804e20` | BO | overnight 1 giornate | 380 | 33 |
| `PT5DAV_FDAX_SBO_002_60` | `FDAX-1H-BO-234d33` | BO | intraday | 422 | 29 |
| `PT5DAV_FDAX_SBO_003_240` *(esclusa: FDAX 4h)* | `FDAX-4H-BO-299d38` | BO | intraday | 443 | 32 |
| `PT5DAV_FDAX_VBO_001_30` | `FDAX-30M-VBO-e5b952` | VBO | intraday | 769 | 67 |
| `PT5DAV_FDAX_VBO_002_60` | `FDAX-1H-VBO-6acd7e` | VBO | overnight 3 giornate | 467 | 33 |
| `PT5DAV_GC_BBO_001_30` | `GC-30M-BIASBO-cf5abd` | BIAS_BO | del motore | 1728 | 126 |
| `PT5DAV_GC_BBO_002_60` | `GC-1H-BIASBO-ee209b` | BIAS_BO | del motore | 2563 | 186 |
| `PT5DAV_GC_BIA_001_30` | `GC-30M-BIAS-9d8a5e` | BIAS | del motore | 3404 | 250 |
| `PT5DAV_GC_BOS_001_15` | `GC-15M-BOS-b79c91` | BO_S | intraday | 83 | 7 |
| `PT5DAV_GC_BOS_002_240` | `GC-4H-BOS-c4077a` | BO_S | overnight 2 giornate | 1703 | 130 |
| `PT5DAV_GC_BSW_001_240` | `GC-4H-BIASW-c4d948` | BIASW | del motore | 678 | 48 |
| `PT5DAV_GC_LFD_001_15` | `GC-15M-LF-30fffd` | LF | overnight 10 giornate | 173 | 23 |
| `PT5DAV_GC_LFD_002_60` | `GC-1H-LF-ad1a29` | LF | overnight 2 giornate | 85 | 23 |
| `PT5DAV_GC_LFH_001_30` | `GC-30M-LFHL-3bcfa0` | LF_HL | intraday | 266 | 40 |
| `PT5DAV_GC_LFH_002_60` | `GC-1H-LFHL-0d91f0` | LF_HL | intraday | 211 | 24 |
| `PT5DAV_GC_LFH_003_240` | `GC-4H-LFHL-8e771a` | LF_HL | overnight 2 giornate | 107 | 8 |
| `PT5DAV_GC_MAC_001_30` | `GC-30M-MAC-6f82f8` | MAC | del motore | 955 | 67 |
| `PT5DAV_GC_MAC_002_240` | `GC-4H-MAC-a1637f` | MAC | del motore | 276 | 19 |
| `PT5DAV_GC_PCH_001_15` | `GC-15M-PC-0e839f` | PC | intraday | 1378 | 78 |
| `PT5DAV_GC_PCH_002_30` | `GC-30M-PC-c6a9b9` | PC | intraday | 1881 | 109 |
| `PT5DAV_GC_PCH_003_60` | `GC-1H-PC-8a30d6` | PC | intraday | 982 | 73 |
| `PT5DAV_GC_PCH_004_240` | `GC-4H-PC-6248c0` | PC | intraday | 2000 | 155 |
| `PT5DAV_GC_RBM_001_15` | `GC-15M-RBBM-541cb4` | RBB_M | overnight 1 giornate | 1825 | 152 |
| `PT5DAV_GC_RBM_002_30` | `GC-30M-RBBM-c7fd58` | RBB_M | intraday | 2335 | 176 |
| `PT5DAV_GC_RBM_003_240` | `GC-4H-RBBM-3732c3` | RBB_M | overnight 10 giornate | 531 | 46 |
| `PT5DAV_GC_RBU_001_15` | `GC-15M-RBBU-7db95b` | RBB_U | intraday | 2490 | 204 |
| `PT5DAV_GC_RHL_001_30` | `GC-30M-RHL-20b00f` | RHL | intraday | 958 | 60 |
| `PT5DAV_GC_RHL_002_240` | `GC-4H-RHL-04c20f` | RHL | overnight 1 giornate | 243 | 35 |
| `PT5DAV_GC_SBO_001_15` | `GC-15M-BO-b7d6e3` | BO | intraday | 518 | 32 |
| `PT5DAV_GC_SBO_002_60` | `GC-1H-BO-21f231` | BO | intraday | 452 | 23 |
| `PT5DAV_GC_TFU_001_15` | `GC-15M-TFU-441e41` | TF_U | intraday | 882 | 77 |
| `PT5DAV_GC_TFU_002_240` | `GC-4H-TFU-55963c` | TF_U | overnight 1 giornate | 1327 | 101 |
| `PT5DAV_GC_VBO_001_60` | `GC-1H-VBO-0c7ac2` | VBO | intraday | 422 | 23 |
| `PT5DAV_GC_VBO_002_240` | `GC-4H-VBO-94b8f1` | VBO | intraday | 276 | 25 |
| `PT5DAV_KC_LFD_001_15` | `KC-15M-LF-dc9d32` | LF | overnight 2 giornate | 67 | 7 |
| `PT5DAV_KC_LFH_001_60` | `KC-1H-LFHL-742947` | LF_HL | intraday | 50 | 4 |
| `PT5DAV_KC_PCH_001_15` | `KC-15M-PC-73bc0d` | PC | intraday | 57 | 2 |
| `PT5DAV_KC_RBM_001_15` | `KC-15M-RBBM-603b3b` | RBB_M | intraday | 134 | 7 |
| `PT5DAV_KC_RBU_001_240` | `KC-4H-RBBU-ca3151` | RBB_U | overnight 10 giornate | 403 | 27 |
| `PT5DAV_KC_SBO_001_30` | `KC-30M-BO-68452f` | BO | overnight 5 giornate | 131 | 7 |
| `PT5DAV_KC_SBO_002_60` | `KC-1H-BO-9e1716` | BO | overnight 10 giornate | 409 | 47 |
| `PT5DAV_KC_TFM_001_30` | `KC-30M-TFM-0c0045` | TF_M | intraday | 84 | 16 |
| `PT5DAV_KC_VBO_001_15` | `KC-15M-VBO-eb0cb9` | VBO | intraday | 87 | 4 |
| `PT5DAV_KC_VBO_002_60` | `KC-1H-VBO-72d23e` | VBO | overnight 3 giornate | 351 | 38 |
| `PT5DAV_NQ_BBO_001_240` | `NQ-4H-BIASBO-860a60` | BIAS_BO | del motore | 2511 | 183 |
| `PT5DAV_NQ_BOS_001_15` | `NQ-15M-BOS-f7179d` | BO_S | intraday | 2250 | 188 |
| `PT5DAV_NQ_BOS_002_30` | `NQ-30M-BOS-be2e02` | BO_S | intraday | 408 | 38 |
| `PT5DAV_NQ_BOS_003_60` | `NQ-1H-BOS-979677` | BO_S | intraday | 2442 | 197 |
| `PT5DAV_NQ_BSW_001_240` | `NQ-4H-BIASW-cff083` | BIASW | del motore | 676 | 48 |
| `PT5DAV_NQ_LFD_001_15` | `NQ-15M-LF-9b2a65` | LF | intraday | 458 | 52 |
| `PT5DAV_NQ_LFD_002_30` | `NQ-30M-LF-82d4e0` | LF | overnight 1 giornate | 358 | 61 |
| `PT5DAV_NQ_LFD_003_60` | `NQ-1H-LF-e44570` | LF | overnight 5 giornate | 83 | 14 |
| `PT5DAV_NQ_LFD_004_240` | `NQ-4H-LF-89bec8` | LF | overnight 3 giornate | 262 | 33 |
| `PT5DAV_NQ_LFH_001_15` | `NQ-15M-LFHL-a3eb22` | LF_HL | intraday | 57 | 7 |
| `PT5DAV_NQ_LFH_002_30` | `NQ-30M-LFHL-c2b71a` | LF_HL | overnight 1 giornate | 107 | 14 |
| `PT5DAV_NQ_LFH_003_240` | `NQ-4H-LFHL-16fd5d` | LF_HL | overnight 1 giornate | 73 | 8 |
| `PT5DAV_NQ_MAC_001_15` | `NQ-15M-MAC-5368bc` | MAC | del motore | 1243 | 79 |
| `PT5DAV_NQ_MAC_002_60` | `NQ-1H-MAC-7d3a53` | MAC | del motore | 580 | 39 |
| `PT5DAV_NQ_MAC_003_240` | `NQ-4H-MAC-75a060` | MAC | del motore | 243 | 22 |
| `PT5DAV_NQ_PCH_001_30` | `NQ-30M-PC-b611a0` | PC | intraday | 1826 | 162 |
| `PT5DAV_NQ_RBM_001_15` | `NQ-15M-RBBM-9e6ab4` | RBB_M | intraday | 116 | 12 |
| `PT5DAV_NQ_RBM_002_60` | `NQ-1H-RBBM-31a938` | RBB_M | overnight 1 giornate | 1319 | 91 |
| `PT5DAV_NQ_RBM_003_240` | `NQ-4H-RBBM-92900f` | RBB_M | intraday | 1544 | 112 |
| `PT5DAV_NQ_RBU_001_15` | `NQ-15M-RBBU-316499` | RBB_U | intraday | 539 | 56 |
| `PT5DAV_NQ_RBU_002_60` | `NQ-1H-RBBU-0d6ea5` | RBB_U | overnight 1 giornate | 451 | 48 |
| `PT5DAV_NQ_RBU_003_240` | `NQ-4H-RBBU-ece922` | RBB_U | intraday | 742 | 55 |
| `PT5DAV_NQ_RBU_004_30` | `NQ-30M-RBBU-eb1d0a` | RBB_U | overnight 5 giornate | 359 | 30 |
| `PT5DAV_NQ_RHL_001_30` | `NQ-30M-RHL-df1434` | RHL | intraday | 507 | 37 |
| `PT5DAV_NQ_RHL_002_15` | `NQ-15M-RHL-873f51` | RHL | overnight 3 giornate | 525 | 44 |
| `PT5DAV_NQ_RHL_003_60` | `NQ-1H-RHL-0d0b7b` | RHL | overnight 2 giornate | 67 | 1 |
| `PT5DAV_NQ_RHL_004_240` | `NQ-4H-RHL-4cd2c3` | RHL | overnight 3 giornate | 513 | 44 |
| `PT5DAV_NQ_SBO_001_15` | `NQ-15M-BO-b752d0` | BO | intraday | 702 | 58 |
| `PT5DAV_NQ_SBO_002_60` | `NQ-1H-BO-33a555` | BO | overnight 3 giornate | 404 | 26 |
| `PT5DAV_NQ_TFM_001_60` | `NQ-1H-TFM-669f18` | TF_M | intraday | 703 | 67 |
| `PT5DAV_NQ_TFM_002_240` | `NQ-4H-TFM-c66f9e` | TF_M | overnight 1 giornate | 1372 | 105 |
| `PT5DAV_NQ_VBO_001_15` | `NQ-15M-VBO-557b5b` | VBO | intraday | 427 | 39 |
| `PT5DAV_NQ_VBO_002_240` | `NQ-4H-VBO-5aaf95` | VBO | intraday | 1578 | 141 |
| `PT5DAV_YM_BBO_001_60` | `YM-1H-BIASBO-e5a983` | BIAS_BO | del motore | 1536 | 128 |
| `PT5DAV_YM_BOS_001_60` | `YM-1H-BOS-523718` | BO_S | intraday | 2816 | 220 |
| `PT5DAV_YM_BSW_001_240` | `YM-4H-BIASW-8adfb4` | BIASW | del motore | 676 | 50 |
| `PT5DAV_YM_LFD_001_15` | `YM-15M-LF-8a2596` | LF | intraday | 1221 | 103 |
| `PT5DAV_YM_LFD_002_30` | `YM-30M-LF-ce0dc2` | LF | overnight 1 giornate | 59 | 6 |
| `PT5DAV_YM_LFD_003_60` | `YM-1H-LF-db5892` | LF | intraday | 585 | 62 |
| `PT5DAV_YM_LFH_001_15` | `YM-15M-LFHL-920960` | LF_HL | intraday | 365 | 24 |
| `PT5DAV_YM_LFH_002_30` | `YM-30M-LFHL-d5d55f` | LF_HL | intraday | 240 | 26 |
| `PT5DAV_YM_LFH_003_240` | `YM-4H-LFHL-14aa4a` | LF_HL | overnight 5 giornate | 86 | 4 |
| `PT5DAV_YM_MAC_001_15` | `YM-15M-MAC-87cbcb` | MAC | del motore | 989 | 84 |
| `PT5DAV_YM_MAC_002_30` | `YM-30M-MAC-5cdc3b` | MAC | del motore | 421 | 33 |
| `PT5DAV_YM_MAC_003_60` | `YM-1H-MAC-1d01cc` | MAC | del motore | 472 | 31 |
| `PT5DAV_YM_MAC_004_240` | `YM-4H-MAC-922e04` | MAC | del motore | 133 | 16 |
| `PT5DAV_YM_PCH_001_15` | `YM-15M-PC-33088c` | PC | intraday | 1872 | 154 |
| `PT5DAV_YM_PCH_002_30` | `YM-30M-PC-fd12e7` | PC | intraday | 807 | 56 |
| `PT5DAV_YM_PCH_003_60` | `YM-1H-PC-70ea3b` | PC | intraday | 1529 | 110 |
| `PT5DAV_YM_PCH_004_240` | `YM-4H-PC-ae2a80` | PC | overnight 10 giornate | 340 | 28 |
| `PT5DAV_YM_RBM_001_15` | `YM-15M-RBBM-cb8dc7` | RBB_M | intraday | 1092 | 86 |
| `PT5DAV_YM_RBM_002_30` | `YM-30M-RBBM-7d1b56` | RBB_M | intraday | 1363 | 93 |
| `PT5DAV_YM_RBM_003_60` | `YM-1H-RBBM-324e1f` | RBB_M | intraday | 2243 | 160 |
| `PT5DAV_YM_RBU_001_60` | `YM-1H-RBBU-b5b54f` | RBB_U | intraday | 1378 | 116 |
| `PT5DAV_YM_RBU_002_240` | `YM-4H-RBBU-ab5182` | RBB_U | overnight 5 giornate | 468 | 35 |
| `PT5DAV_YM_RHL_001_30` | `YM-30M-RHL-76a351` | RHL | intraday | 189 | 15 |
| `PT5DAV_YM_RHL_002_60` | `YM-1H-RHL-be711d` | RHL | overnight 2 giornate | 100 | 6 |
| `PT5DAV_YM_RHL_003_240` | `YM-4H-RHL-e90851` | RHL | overnight 10 giornate | 69 | 4 |
| `PT5DAV_YM_SBO_001_15` | `YM-15M-BO-2f48d5` | BO | overnight 5 giornate | 663 | 59 |
| `PT5DAV_YM_TFM_001_15` | `YM-15M-TFM-a7b654` | TF_M | intraday | 1072 | 86 |
| `PT5DAV_YM_TFM_002_240` | `YM-4H-TFM-49c3d4` | TF_M | intraday | 814 | 69 |
| `PT5DAV_YM_VBO_001_15` | `YM-15M-VBO-01f0dd` | VBO | intraday | 741 | 32 |
| `PT5DAV_YM_VBO_002_30` | `YM-30M-VBO-dc4c81` | VBO | intraday | 693 | 58 |
| `PT5DAV_YM_VBO_003_60` | `YM-1H-VBO-7f8ee0` | VBO | overnight 10 giornate | 84 | 6 |
| `PT5DAV_YM_VBO_004_240` | `YM-4H-VBO-06a4c2` | VBO | overnight 3 giornate | 72 | 3 |

## Riferimenti codice

- `Piootoo.Strategies/PT5DAVStrategies/Engines/Pt5DavEngineBase.cs`, `Pt5DavSessionState.cs`, `Pt5DavMarket.cs`, i motori nella stessa cartella
- `Piootoo.Strategies.Tests/Pt5DavParityStudy.cs`
- `tools/pt5dav/gen_pt5dav.py`
- `piootoo-repository/marketdata/market-calendars.json` (BP, BTC)
- `piootoo-repository/PT5DAV/` (consegna) e `PT5DAV/verifica/` (riconciliazioni)
