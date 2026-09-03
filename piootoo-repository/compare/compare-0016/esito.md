# compare-0016 — portafoglio nuovo (96 strategie), 15-31 marzo 2026, due gambe + ricerca

Analisi del 2026-09-03. Motore **5.1.0 su entrambe le gambe**.
Prima volta che il paniere delle nuove strategie (dossier `run-engine/run-08-settembre`) viene
confrontato con qualcosa.

## Le gambe

| | file | tipo | motore | prezzi | arco ingressi |
|---|---|---|---|---|---|
| interno CFD | `trades-interno-cfd-FTMOPLATFORM.json` | `interno-cfd-FTMOPLATFORM` | `PiootooTradingService` 5.1.0 | `datafeed-external/FTMOPLATFORM/` | 16/03 01:00 → 30/03 19:00 |
| cBot | `trades-cbot-cfd-FTMO.json` | `cbot-cfd-FTMO` | `PiootooDistributedExecutionBot` nel backtester cTrader, 5.1.0 | CFD **FTMO**, conto 17188650, piano `FTMO-ALL` | 15/03 21:37 → 31/03 23:36 |
| ricerca | `run-engine/run-08-settembre/DOSSIER_PANIERE.md` | — | motore Python | dati vendor | OOS 02/06/2021 → 30/05/2025 |

**Gli slug dei broker non coincidono: `FTMO` (conto) contro `FTMOPLATFORM` (archivio barre).**
Sotto `datafeed-external/` esiste solo `FTMOPLATFORM`. Per compare-0015 fu verificato che due
broker possono comunque essere la stessa serie di prezzi; qui non e' stato verificato, e senza
quella verifica la gamba «motore isolato» e' un'assunzione, non una misura. Da chiarire: se sono
lo stesso broker va uniformato lo slug, se no il confronto misura feed e motore insieme.

## Il periodo e' troppo corto per pronunciarsi sul rendimento

16 giorni, 146 trade interni, deviazione standard **3.159 USD per trade**. L'errore standard
sulla somma e' **+/-38.173 USD** contro un risultato di +48.215. Qualunque frase del tipo «rende
meno della ricerca» e' dentro il rumore e non va scritta.

| | trade | netto | banda 1 sigma |
|---|---|---|---|
| interno CFD | 146 | **+48.215 USD** | [10.042, 86.388] |
| cBot, normalizzato x10 | 189 | **+22.893 USD** | [-19.817, 65.603] |
| cBot, grezzo | 189 | +2.676 USD | — |
| ricerca, scalata linearmente sui 16 giorni | ~171 attesi | ~98.293 USD | — |

L'attesa della ricerca dista **1,31 sigma** dal risultato interno: non e' una divergenza.
Aspettativa per trade: ricerca 492 USD, interno 330, cBot normalizzato 121.

**Quello che il periodo corto permette di misurare sono le strutture** — quali strategie
esistono, se le entrate coincidono, se il feed e' integro. Quelle non hanno bisogno di campione.

## 1. Il cBot gira a 1/10 della taglia interna, su tutti i simboli

Misurato come `lordo / |uscita - ingresso| / quantita`, simbolo per simbolo:

| | BTC | CC | CL | ES | GC | KC | NG | NQ | PL | YM | FDAX |
|---|---|---|---|---|---|---|---|---|---|---|---|
| rapporto denaro EXT/INT | 0,100 | 0,100 | 0,100 | 0,100 | 0,100 | 0,099 | 0,100 | 0,100 | 0,100 | 0,100 | 0,116 |

Il valore-punto interno coincide con la tabella §2.4 del dossier su tutti e undici i simboli
(NQ 20, ES 50, FDAX 25, GC 100, CL 1000, NG 10000, KC 375, PL 50, YM 5, CC 10, BTC 5). FDAX a
0,116 invece di 0,100 e' il cambio EUR/USD (0,116 / 0,100 = 1,155), non un errore di taglia.

Su un conto FTMO da 100.000 con 96 strategie contemporanee 1/10 e' plausibilmente **voluto**, ma
non e' dichiarato da nessuna parte, e in compare-0007 era stato verificato l'opposto (1:1).
**Finche' non e' dichiarato in `run-*.json`, ogni confronto va normalizzato a mano.** Tutti i
numeri sotto sono normalizzati.

## 2. Le entrate coincidono — la cosa che il dossier chiede di verificare per prima

§5 del dossier: *«Il port e' corretto quando le entrate coincidono — timestamp e prezzo.»*

Appaiamento per (strategia, lato), tolleranza una barra del timeframe con minimo 2 ore:

- **119 coppie** su 146 interni (82%) e 189 esterni (63%).
- Scarto di prezzo d'ingresso: **mediana 0,13 punti base**, p90 3,74.
- Ritardo d'ingresso del cBot: mediana **+8,4 min a 15'**, +9,7 a 30', +17,3 a 60', +8,0 a 240',
  +6,1 a 1440' — cioe' **una frazione di barra**, non la barra intera di compare-0015. Il segno
  e' coerente: l'interno segna il fill sull'apertura della barra, il cBot quando il livello viene
  davvero attraversato.
- Famiglia d'uscita concorde su **110 coppie su 119 (92%)**.
- **Target contro target: 22 coppie, scarto complessivo -3 USD.** I target sono identici.
- Slittamento sugli stop, misurato contro la **distanza dichiarata** del cBot: mediana +0,26
  punti su NQ, +0,01 su ES, +0,00 su NG, +0,14 su GC, +0,78 su YM, +1,12 su BTC. Il cBot riempie
  onestamente. **Lo slippage non e' piu' un tema.**

Il porting regge. Il divario e' altrove.

## 3. Il divario, scomposto

Interno +48.215 contro cBot normalizzato +22.893: **25.321 USD**, che quadra esattamente cosi':

| voce | USD |
|---|---|
| 119 coppie | +24.639 |
| 27 trade solo interni | +3.442 |
| 70 trade solo cBot | -2.759 |
| **totale** | **+25.321** |

E dentro le coppie, per combinazione di uscita:

| interno → cBot | coppie | USD |
|---|---|---|
| protettiva → protettiva | 88 | **+11.637** |
| target → protettiva | 1 | **+6.267** |
| protettiva → Closed | 1 | +2.438 |
| TimeExit → protettiva | 1 | +1.930 |
| OppositeSignal → protettiva | 1 | +1.598 |
| SessionFlat → target | 1 | -1.214 |
| altre | 16 | +1.983 |

Per simbolo, il divario e' concentrato su NG (+14.579), NQ (+13.887) e BTC (+6.262), con segno
opposto su ES (-11.191) e YM (-6.431).

## 4. Il difetto vero: 29 trade interni (20%) aprono e chiudono nello stesso istante

Zero dal lato cBot. Valgono **+10.713 USD**, il **42% del divario**.

Non sono tutti uguali. Cinque sono `BreakEven` a prezzo d'ingresso identico (-4 USD di sola
commissione): innocui. Sette invece sono **vincite piene ricavate dentro la barra d'ingresso**:

| strategia | istante | ingresso | uscita | causa | netto |
|---|---|---|---|---|---|
| `PTS_NQ_TFU_008_240` | 23/03 11:00 | 23.883,76 | 24.108,76 | TakeProfit | **+4.496** |
| `PTS_YM_TFM_001_240` | 23/03 11:00 | 45.667,08 | 46.467,08 | TakeProfit | **+3.996** |
| `PTS_BTC_PCH_001_240` | 16/03 03:00 | 73.177,95 | 73.896,10 | TrailingStop | **+3.587** |
| `PTS_FDAX_PCH_001_240` | 30/03 09:00 | 22.356,31 | 22.453,31 | TrailingStop | **+2.421** |
| `PTS_YM_TFM_002_240` | 23/03 11:00 | 45.667,08 | 45.967,08 | TakeProfit | **+1.496** |
| `PTS_FDAX_PCH_001_240` | 17/03 13:00 | 23.705,01 | 23.741,51 | TrailingStop | +908 |
| `PTS_CL_MAC_001_30` | 23/03 13:30 | 91,30 | 90,77 | TrailingStop | +521 |

Diciotto dei 29 sono su timeframe **240**. L'engine entra all'apertura della barra a 4 ore e
raggiunge il target *della stessa barra* senza mai passare da un'altra barra: la distanza
realizzata e' esattamente quella dichiarata (+225 su NQ, +800 su YM). E' la
**risoluzione intrabarra ottimista** gia' vista in compare-0015 (la' 8% dei trade, -30.687 USD),
qui col segno opposto perche' stavolta l'engine sceglie il ramo favorevole.

Durata mediana: interno **300 min**, cBot **120 min**. Il cBot su dati tick non produce mai
questo caso — §5 del dossier lo dice esplicitamente («il backtest su barre non e' attendibile»).

Correlato: su FDAX la distanza mediana realizzata sulle uscite protettive interne e' **0,00
punti** su 6 coppie (3.466 USD di divario). Uno stop che si riempie a distanza zero non esiste.

## 5. Sei file del feed non hanno piu' le barre con cui il run e' stato fatto

`backtest-summary-interno-cfd-FTMOPLATFORM.json` dichiara `coversRequestedRange: true` su tutti
e 28 i datasource, con conteggi pieni (GC 30': 1.470 candele 13/02 → 31/03/2026; FDAX 240':
1.327). Oggi quegli stessi file **non contengono una sola barra fra febbraio e maggio 2026**:

| file | candele | mesi coperti | in finestra 15-31/03 |
|---|---|---|---|
| `@GC_30.json` | 2.257 | 2022-12, 2026-06 → 2026-09 | **0** |
| `@CL_30.json` | 2.257 | 2022-12, 2026-06 → 2026-09 | **0** |
| `@GC_60.json` | 1.082 | 2022-12, 2026-06 → 2026-09 | **0** |
| `@GC_240.json` | 280 | 2022-12, 2026-07 → 2026-08 | **0** |
| `@FDAX_240.json` | 282 | 2022-12, 2023-01, 2026-07 → 2026-08 | **0** |
| `@BP_60.json` | 1.134 | 2022-12, 2023-01, 2026-07 → 2026-09 | **0** |

Tutti e sei portano `lastUpdate` 2026-09-02, cioe' **prima** del run (2026-09-03T13:56Z), e tutti
e sei iniziano con uno stub isolato a 2022-12-28: sono stati **ricostruiti da zero** dal
`PiootooDatafeedSyncBot/1.1.0`, perdendo la storia. Non c'e' nessun journal `.jsonl` accanto, e
la cartella `ticks/` contiene solo il 02/09/2026, quindi le barre non sono recuperabili da li'.
Gli altri 30 file sono intatti (`@NQ_240`: 133 candele a marzo 2026, coerente col summary).

Conseguenza: **compare-0016 non e' riproducibile**, e i 13 trade GC, gli 11 FDAX e i 2 CL della
gamba interna girano su barre che l'archivio non ha piu'. Da chiarire prima di qualunque altra
misura, perche' tocca la raccolta, non l'analisi.

## 6. Registro delle strategie contro il dossier

96 strategie schedulate + 15 rifiutate dal conto (`strategiesNotSupportedByAccount`: tutta la
famiglia HK, HO, CT, SB) = **111**, contro **116** del dossier. Mappando per
(simbolo, timeframe, famiglia) i bucket combaciano ovunque tranne:

- **JY manca del tutto: 8 strategie** (2x 4h TF_U, 3x 30m TF_U, 3x 1h TF_U). Non c'e' nessun
  `@JY_*.json` sotto `datafeed-external/FTMOPLATFORM/`. E' il punto gia' annotato come «JY fuori
  registro» il 02/09.
- **3 strategie in piu' del dossier**: `PTS_NQ_PCH_001_15`, `PTS_NQ_PCH_002_15` e una quarta
  `NQ 60 TFM` (il dossier ne ha 3). Da capire se vengono da un run successivo o sono residui.
- `NQ 15 RBB_M` del dossier e' `PTS_NQ_RBM_001_15` nel catalogo: solo nomenclatura.

Le 15 rifiutate dal conto sono **tutte** simboli che FTMO non quota. Non e' un difetto del
porting, ma sono 15 strategie su 116 che su questo conto non esisteranno mai.

## 7. Overweek: il cBot tiene, il piano dice di no

`holding` del piano: `allowOvernight: true`, **`allowOverweek: false`**, flat di sessione 20:45,
weekend ven 20:45 → dom 23:00.

Il cBot ha **10 posizioni che attraversano il sabato** e una durata massima di **8 giorni e 12
ore** (interno: 3 giorni e 12 ore). Ha inoltre **8 ingressi nel fine settimana**, di cui due il
**sabato** su BTC (21/03 15:16 e 28/03 07:00). E' la stessa asimmetria di compare-0014, e il
divario e' piccolo qui solo perche' il periodo e' corto.

## 8. Il calendario di sessione del feed contro §2.1.1 del dossier

Il dossier vieta le sessioni domenicali su CC, CT, FDAX, KC, SB, e misura sul DAX un costo
dell'**11% del P&L** quando ci sono. Nel feed FTMOPLATFORM, finestra 15-31/03:

- **FDAX ha 3 barre di domenica** (su `@FDAX_1440`) — sessioni che la ricerca non ha mai avuto.
- BTC ha 55 barre di sabato e 90 di domenica; la ricerca da' BTC sab = 0, dom = 27.
- CC, CT, KC, SB sono puliti (0 domenica), come devono essere.

## Cose che tornano e non vanno riaperte

- Valore-punto interno = tabella §2.4 del dossier su tutti e 11 i simboli operati.
- Target: 22 coppie, -3 USD complessivi.
- Slippage sugli stop del cBot contro la distanza dichiarata: <=0,26 punti su NQ/ES/NG/GC.
- Nessuna strategia saltata per riscaldamento: `skippedNotEnoughCandles` = 0 su tutte e 96, e
  tutte hanno `evaluations == scheduled`. Il problema VBO di compare-0015 non si ripresenta —
  ma `PTS_FDAX_VBO_001_240` resta l'**unica strategia che opera solo dall'interno** (2 trade),
  e `PTS_NQ_VBO_001_1440` / `PTS_NQ_VBO_002_240` non operano da nessuna delle due parti.
- 730 `wrongSideLevelsRejected` sul run interno, contro 7.030 segnali d'ingresso: il filtro
  lavora, non e' spento.

## Da fare, in ordine

1. **Ricostruire i sei file di feed** e ricontrollare che l'archivio contenga il periodo del run.
   Senza questo, niente di quanto sopra e' riproducibile.
2. **Dichiarare la taglia** nel marcatore del run, o allinearla. 1/10 non deve essere una cosa
   che si scopre misurando.
3. **Chiudere la questione `FTMO` contro `FTMOPLATFORM`**: stesso broker o no.
4. **La risoluzione intrabarra** (§4): quando la barra d'ingresso copre sia lo stop sia il
   target, l'engine deve scegliere il ramo peggiore. Sono il 42% del divario e il segno cambia da
   un run all'altro, quindi il numero che si legge oggi non e' affidabile in nessuna direzione.
5. Rifare il confronto su **almeno sei mesi**. Con 16 giorni la sigma vale l'80% del risultato.
6. Recuperare JY (8 strategie) o dichiararle fuori paniere.
