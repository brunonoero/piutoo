# compare-0043 — cbot-cfd-FTMO contro interno-cfd-FTMO, piano COMP-005B

Analisi del 13/09/2026. Report dello strumento in `analisi/report.md` (rigenerato con `log.txt` del cBot);
metriche per strategia dell'interno in `interno-005a-per-strategia.csv` e `interno-005b-per-strategia.csv`
(script `script/per-strategia.ps1`, fuori da git come i CSV).

Workspace `all-005x`: due piani sullo stesso masterfilter (98 strategie). **COMP-005A** le tiene tutte e ha
solo il backtest interno; **COMP-005B** ne spegne 89 e ne lascia 9, ed e' l'unico con il run del cBot.
Questa cartella confronta le due gambe di COMP-005B; 005A serve da paniere per scegliere le prossime.

## Le due gambe

| lato | cartella | slug | motore | serie di prezzi | arco (ingressi) | versione |
|---|---|---|---|---|---|---|
| cBot | `all-005x/backtests/comp-005b-bt-20250831-0000` | `cbot-cfd-FTMO` | `PiootooDistributedExecutionBot` | CFD FTMO | 01/09/2025 → 30/08/2026 | 7.4.0 |
| interno | `all-005x/backtests/backtest-20260913-comp-005b` | `interno-cfd-FTMO` | `PiootooTradingService` | CFD FTMO | 01/09/2025 → 30/08/2026 | 7.4.0 |

Nessun campo dedotto: `origin.json` di entrambi con `IdentifiesRun: true`, `Broker = FTMO`. Conto cBot 17188650,
100.000 USD, lotti 0,1 su GC, 2 su NQ, 5 su ES. Interno a 1.000.000, 1 contratto, commissione piatta 4 $,
spread FTMOPLATFORM p50 per ora UTC, stop ×1. Stesse 33 strategie nell'elenco di allargamento su entrambi
(nessuna delle 9 e' allargata tranne ES_BSW_003 e NQ_TFU_001, con fattore 1). `log.txt` copiato da
`cAlgo/Data/cBots/PiootooDistributedExecutionBot/3757098f-…/Backtesting/` (stesso istante del
`session-summary.json`). **Controprova:** i trade interni delle 9 strategie sono identici in 005A e 005B —
nell'interno le strategie non si influenzano.

## Esito

Finestra comune = run intero. Per contratto, USD.

| | interno | cBot |
|---|---:|---:|
| trade | 667 | 682 |
| lordo | 263.249 | 206.782 |
| commissioni | 2.668 | 21.111 |
| swap | 0 | −8.410 |
| netto | **260.581** | **177.260** |

Divario **−83.321** (−32%). Nessun cambio di segno: e' deriva, concentrata su GC_PCH_004 (−47.074) e
NQ_PCH_008 (−16.141). Abbinati 646 su 667 interni (96,9%); ingresso ≤ 1 min su 576.

## Scomposizione

| voce | Δ $/ctr | quota | natura |
|---|---:|---:|---|
| lordo | −56.467 | 67,8% | spread ai fill (31.178 misurati dal log), trigger su Ask, esiti |
| commissioni | −18.443 | 22,1% | modello (interno 4 $ piatti, cBot 23–35 $) |
| swap | −8.410 | 10,1% | non modellato dall'interno; ES da solo −6.040 (143,8 $/trade) |

−56.467 − 18.443 − 8.410 = **−83.320**.

Per strategia (netto/ctr):

| strategia | interno | cBot | Δ | da dove |
|---|---:|---:|---:|---|
| PTS_GC_PCH_004_240 | 61.785 | 14.711 | −47.074 | spread/stop 13,4% (19.527 di spread), 14 ingressi solo cBot alla riapertura, 19 da intent stantii, 7 chiusi a 0 barre |
| PTS_NQ_PCH_008_240 | 16.215 | 74 | −16.141 | un TP da +9.996 solo interno (04/08/2026): il cBot era entrato prima sull'Ask ed era stato stoppato |
| PTS_NQ_TFU_006_1440 | 48.812 | 41.960 | −6.852 | 9 trade da intent stantii (+22.592) |
| PTS_ES_BSW_003_15 | 16.051 | 9.361 | −6.690 | lordo quasi uguale (16.219 / 16.365): lo swap |
| PTS_NQ_PCH_003_30 | 16.842 | 14.839 | −2.003 | ingressi anticipati sull'Ask |
| PTS_NQ_PCH_004_30 | 23.183 | 21.683 | −1.500 | commissioni e spread |
| PTS_NQ_TFU_001_15 | 31.523 | 30.151 | −1.372 | commissioni e spread |
| PTS_NQ_TFU_003_15 | 11.339 | 10.460 | −879 | 1 ingresso fuori finestra da intent stantio |
| PTS_NQ_TFM_003_15 | 34.832 | 34.022 | −810 | commissioni e spread |

## Chiuso (con prova)

- **Il claim consegnava il template della barra prima** (difetto del server, corretto in 7.4.1). Mentre il conto
  e' in posizione i template restano non reclamati; il filtro `ExpiresAtUtc >= LastEvaluatedBarTimeUtc` li
  teneva vivi una barra oltre la loro e la selezione prendeva il piu' vecchio. Prova nel log: NQ_TFU_003
  chiude in TP il 03/12/2025 01:34:29 e riceve subito l'intent con `ValidFrom 01:15` («attesa -1170s»), poi a
  ogni barra quello della barra prima («attesa -900s») fino al fill delle 02:26, fuori finestra. Nel report
  §9: 447 intent da 15 min, 134 da 30, 68 da 240, 31 giornalieri in ritardo di ≥ 1 barra; 29 trade nati cosi'.
  Correzione: `IsTemplateBarOver` sull'ultima barra dello stream del template; `StaleTemplateClaimTests`.
- **Ingressi con la deadline gia' passata** (difetto del cBot, corretto in 7.4.1). GC_PCH_004 e' intraday: il
  template del venerdi' vale sulla prima barra dopo il fine settimana, ma il suo `CloseAtUtc` e' la fine della
  sessione del venerdi' (`ResolveCloseAtUtc(ValidFromUtc, SessionEnd)`). Il bot piazzava alla riapertura e
  `CloseExpiredPositions` chiudeva 1–1,5 s dopo: 7 trade `Closed` a 0 barre (01/09/2025 01:08, 07/09 22:15,
  30/11 23:08, 28/06/2026 22:06, …), −6 $ l'uno sul conto. `RejectUnsoundIntent` ora li scarta.
- **Le strategie sono rispettate.** Finestra: interno 0 fuori, cBot 1 (quello stantio). Limite per sessione: 4
  sessioni oltre il massimo su GC, identiche sui due lati. Famiglie d'uscita mappate come previsto. Nessun fill
  interno su minuti senza barra. Zero uscite per segnale opposto.

## Aperto

1. **Rifare il run cBot di COMP-005B con server e bot 7.4.1**: le due correzioni tolgono i 29 trade da intent
   stantii e i 7 a 0 barre; NQ_TFU_006 (+22.592 da intent stantii) e' la strategia che si muove di piu'.
2. **Trigger dei pending sul Bid contro Ask.** Il cBot fa scattare uno stop buy sull'Ask, l'interno sulla
   `High` della serie Bid: dentro lo spread il cBot entra e l'interno no, o entra prima (NQ: 12:00:00 contro
   13:03, 23:15 contro 23:55). E' la convenzione dichiarata in CLAUDE.md («il trigger dei pending resta sul
   feed»); alla riapertura di GC, con spread a 1,96 punti su uno stop da 5 (39%), pesa. Da decidere se misurarla.
3. **Swap e commissioni per simbolo nell'interno** — gia' aperto da compare-0041, qui vale il 32% del divario.
4. **La riga `limite di ingressi per sessione raggiunto`** stampa l'intero storico dei secchi riempiti
   (`fill in 2025-08-29 … 2026-04-16`): cresce per tutto il run, riga dopo riga. Solo log, ma sul server e'
   una lista che non viene mai potata.

## Quali strategie tenere (sui numeri del cBot, per contratto, costi veri)

| strategia | netto cBot | PF | R² | DD | verdetto |
|---|---:|---:|---:|---:|---|
| PTS_NQ_TFU_001_15 | 30.151 | 1,51 | 0,75 | 14.657 | **tenere** — crescente su entrambe le gambe |
| PTS_NQ_TFM_003_15 | 34.022 | 1,60 | 0,57 | 23.422 | **tenere** |
| PTS_NQ_PCH_004_30 | 21.683 | 2,29 | 0,36 | 12.348 | **tenere** |
| PTS_NQ_TFU_006_1440 | 41.960 | 1,88 | 0,08 | 31.066 | tenere con riserva: metà dell'utile da intent stantii, da rimisurare |
| PTS_NQ_TFU_003_15 | 10.460 | 1,26 | 0,55 | 9.934 | marginale |
| PTS_NQ_PCH_003_30 | 14.839 | 1,32 | 0,04 | 20.882 | marginale: curva piatta, DD > utile |
| PTS_GC_PCH_004_240 | 14.711 | 1,10 | 0,46 | 21.797 | **togliere** — lo spread vale 13,4% dello stop e si mangia tre quarti dell'interno |
| PTS_ES_BSW_003_15 | 9.361 | 1,14 | 0,34 | 18.680 | **togliere** — lo swap vale il 40% dell'utile |
| PTS_NQ_PCH_008_240 | 74 | 1,00 | 0,16 | 16.836 | **togliere** |

Candidate dal paniere 005A (solo interno, commissione 4 $ piatta, da portare in un run cBot): NQ_VBO_002_240
(+30.410, PF 5,56, DD 4.131), NQ_SBO_006_240 (+19.452, PF 3,64, DD 2.540), NQ_TFM_007_30 (+15.686, R² 0,74),
FDAX_VBO_001_240 (+16.690, PF 3,50 — FDAX nell'interno non e' convertito da EUR e il conto paga 45,6 $ a trade),
KC_SBO_001_240 (+15.688, PF 2,94), NQ_PCH_001_15 (+11.137, R² 0,61, ma stop 12,5 punti contro 1,45 di spread).

## Cosa torna

- Interno 005A = interno 005B sulle 9 strategie, trade per trade.
- Stop interni riempiti al livello (rapporto 1,00); cBot 1,02 su GC, 1,01 su NQ.
- Bracket riancorati al fill: 31, slittamento mediano 0,16 punti, 1.250 $/ctr in tutto.

## Trappole di misura di questa cartella

1. **Quantita' del cBot in lotti, non in contratti** (0,1 GC, 2 NQ, 5 ES): `script/per-strategia.ps1` divide per
   `quantity` ed e' corretto solo sull'interno. Per il cBot usare le cifre per contratto del report.
2. **Swap nel report §0 grezzo (−841), qui per contratto (−8.410).**
3. **Il primo lancio dello strumento senza `log.txt`** ha dato §2b, §5, §8 e §9 vuoti: il report in `analisi/` e'
   quello del secondo lancio.
