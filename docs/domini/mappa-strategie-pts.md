# Mappa: da quale run viene ogni strategia PTS

Ogni classe `PTS_*` è la traduzione di **una riga approvata** di un run di ricerca Python.
Questo file dice quale riga, in quale run, e con quale motore — così fra sei mesi si può
risalire dalla classe alla sua fonte senza riaprire i CSV a tentativi. La procedura di
traduzione sta in [`porting-da-report-sweep.md`](porting-da-report-sweep.md); i motori sono
descritti in [`motori-strategie.md`](motori-strategie.md).

## Il paniere del 25/08/2026: `DOSSIER_PANIERE.md`

La fonte autorevole per il porting non è più la consegna di un singolo run, ma il dossier
consolidato `piootoo-repository/run-engine/run-07-agosto/DOSSIER_PANIERE.md` (rigenerato il
25/08/2026). Somma ventuno run — da `run_20260814_1453` a `run_20260824_2232` — e ne estrae
**75 strategie univoche** su 11 mercati e 10 motori, deduplicate anche *fra* run e timeframe
diversi: nessuna coppia condivide più del 70% degli ordini di entrata. Ogni scheda è
autosufficiente (condizioni, filtri in formula, finestra oraria, uscite in dollari e in punti,
lista trade di riferimento), quindi per tradurre non serve più risalire ai `parametri.csv`.

**Il codice sorgente sta nel commento della classe.** Ogni `PTS_*` apre il proprio XMLdoc con un
paragrafo `Codice sorgente: SNN`, dove `SNN` è l'identificativo della scheda nel dossier. Serve a
una cosa sola e importante: da una classe si arriva alla sua sorgente per un controllo, senza
cercare a tentativi in ventuno cartelle di run. Le classi che una sorgente nel paniere non ce
l'hanno — i due PC di luglio, `PTS_NQ_TFM_001_60`, e le sei disabilitate perché doppioni —
dichiarano `Codice sorgente: nessuno` con il motivo.

> ⚠ **Gli `SNN` non sono stabili fra rigenerazioni del dossier.** Sono ordinati per atteso/trade
> decrescente, quindi una rigenerazione che aggiunga o tolga una strategia li fa scorrere tutti.
> Per questo il paragrafo `Codice sorgente` porta **anche** la coordinata del run
> (`run_YYYYMMDD_HHMM` + famiglia), che è stabile per costruzione: se un giorno gli `SNN` non
> tornano più, è quella la chiave con cui riallinearli. Le classi ES di `run-05-agosto` e le NQ 1h
> di `run-06-gosto` erano già state annotate con gli `SNN` dei *loro* dossier, numerazioni diverse
> e non confrontabili: quei riferimenti sono stati marcati come "vecchio dossier" per non lasciare
> due codici in conflitto nello stesso commento.

### La mappa completa: S-ID → classe C#

Tutte e 75 le strategie del paniere sono tradotte: 31 lo erano già dai run precedenti (il
reverse-mapping è passato dalla coppia run+famiglia, che dossier e classi dichiarano entrambi),
44 sono state create il 25/08/2026.

| Dossier | Classe C# | Stato |
|---|---|---|
| `S01` | `PTS_NQ_TFM_012_1440` | nuova |
| `S02` | `PTS_NQ_SBO_005_1440` | nuova |
| `S03` | `PTS_NQ_TFM_013_1440` | nuova |
| `S04` | `PTS_FDAX_MAC_001_240` | nuova |
| `S05` | `PTS_NQ_TFU_006_1440` | nuova |
| `S06` | `PTS_NQ_TFM_014_240` | nuova |
| `S07` | `PTS_NQ_TFU_007_1440` | nuova |
| `S08` | `PTS_FDAX_PCH_001_240` | nuova |
| `S09` | `PTS_GC_TFU_001_30` | gia' presente |
| `S10` | `PTS_FDAX_VBO_001_240` | nuova |
| `S11` | `PTS_NQ_VBO_001_1440` | nuova |
| `S12` | `PTS_ES_SBO_002_240` | nuova |
| `S13` | `PTS_NQ_TFM_015_240` | nuova |
| `S14` | `PTS_NQ_TFM_006_30` | gia' presente |
| `S15` | `PTS_GC_TFM_001_240` | nuova |
| `S16` | `PTS_BTC_PCH_001_240` | nuova |
| `S17` | `PTS_NQ_TFM_009_60` | gia' presente |
| `S18` | `PTS_ES_SBO_003_240` | nuova |
| `S19` | `PTS_FDAX_SBO_001_240` | nuova |
| `S20` | `PTS_NQ_TFM_007_30` | gia' presente |
| `S21` | `PTS_NQ_TFM_002_15` | gia' presente |
| `S22` | `PTS_NQ_TFM_010_60` | gia' presente |
| `S23` | `PTS_ES_BSW_001_60` | gia' presente |
| `S24` | `PTS_NQ_TFM_008_30` | gia' presente |
| `S25` | `PTS_NQ_TFM_003_15` | gia' presente |
| `S26` | `PTS_ES_PCH_003_1440` | nuova |
| `S27` | `PTS_BP_TFM_001_60` | nuova |
| `S28` | `PTS_NQ_TFM_005_15` | gia' presente |
| `S29` | `PTS_NQ_SBO_004_60` | gia' presente |
| `S30` | `PTS_NQ_SBO_006_240` | nuova |
| `S31` | `PTS_ES_SBO_001_15` | gia' presente |
| `S32` | `PTS_NQ_SBO_001_15` | gia' presente |
| `S33` | `PTS_NQ_SBO_002_15` | gia' presente |
| `S34` | `PTS_NQ_TFU_004_60` | gia' presente |
| `S35` | `PTS_NQ_TFM_011_60` | gia' presente |
| `S36` | `PTS_YM_TFM_001_240` | nuova |
| `S37` | `PTS_ES_TFM_001_1440` | nuova |
| `S38` | `PTS_GC_PCH_001_60` | gia' presente |
| `S39` | `PTS_NQ_PCH_003_30` | gia' presente |
| `S40` | `PTS_CL_MAC_001_30` | nuova |
| `S41` | `PTS_NQ_SBO_003_15` | gia' presente |
| `S42` | `PTS_ES_BSW_002_15` | gia' presente |
| `S43` | `PTS_ES_PCH_001_60` | gia' presente |
| `S44` | `PTS_NG_TFM_001_240` | nuova |
| `S45` | `PTS_ES_TFU_001_1440` | nuova |
| `S46` | `PTS_YM_TFM_002_240` | nuova |
| `S47` | `PTS_NQ_TFU_008_240` | nuova |
| `S48` | `PTS_NQ_TFU_001_15` | gia' presente |
| `S49` | `PTS_ES_BSW_003_15` | gia' presente |
| `S50` | `PTS_NG_TFU_001_240` | nuova |
| `S51` | `PTS_GC_PCH_004_240` | nuova |
| `S52` | `PTS_GC_RHL_001_60` | gia' presente |
| `S53` | `PTS_NQ_PCH_004_30` | gia' presente |
| `S54` | `PTS_ES_TFM_002_1440` | nuova |
| `S55` | `PTS_NQ_VBO_002_240` | nuova |
| `S56` | `PTS_JY_TFU_001_240` | nuova |
| `S57` | `PTS_ES_PCH_004_240` | nuova |
| `S58` | `PTS_YM_BIA_001_240` | nuova |
| `S59` | `PTS_NQ_PCH_007_240` | nuova |
| `S60` | `PTS_NQ_RBM_001_15` | gia' presente |
| `S61` | `PTS_BP_TFM_002_15` | nuova |
| `S62` | `PTS_GC_PCH_005_240` | nuova |
| `S63` | `PTS_JY_TFU_002_240` | nuova |
| `S64` | `PTS_NG_TFU_002_240` | nuova |
| `S65` | `PTS_GC_RHL_002_60` | gia' presente |
| `S66` | `PTS_YM_TFM_003_240` | nuova |
| `S67` | `PTS_NQ_TFU_005_60` | gia' presente |
| `S68` | `PTS_NG_TFM_002_240` | nuova |
| `S69` | `PTS_NQ_TFU_002_15` | gia' presente |
| `S70` | `PTS_NQ_PCH_008_240` | nuova |
| `S71` | `PTS_ES_PCH_002_60` | gia' presente |
| `S72` | `PTS_YM_SBO_001_240` | nuova |
| `S73` | `PTS_YM_SBO_002_240` | nuova |
| `S74` | `PTS_PL_TFM_001_240` | nuova |
| `S75` | `PTS_NQ_TFU_003_15` | gia' presente |

### Quello che il paniere ha portato di nuovo

- **Sei mercati nuovi**: FDAX, YM, CL, NG, PL, BTC — oltre a NQ, ES, GC già coperti. `BTC` è stato
  aggiunto a `InstrumentRegistry` (5 BTC, $5 per punto, tick 5 punti = $25).
- **Tre motori nuovi in catalogo**: `VBO` (`VolatilityBreakoutEngine`), `MAC`
  (`MovingAverageCrossoverEngine`), `BIAS` (`BiasBarCountEngine`). Le sigle `VBO`, `MAC` e `BIA`
  sono state dichiarate in `PtsNamingConventionTests`.
- **Due timeframe nuovi**: 4 ore (`_240`) e giornaliero (`_1440`).

### Cosa resta da fare

- **`PTS_JY_TFU_001_240` e `PTS_JY_TFU_002_240` nascono disabilitate.** Il dossier dichiara per JY
  $125.000 per punto con tick 0,00005, cioè la quotazione scalata ×100 rispetto al 6J CME
  (12.500.000 per punto, tick 0,0000005). Le due convenzioni danno lo stesso valore di tick
  ($6,25) ma convertono in modo diverso gli stop in denaro: finché non esiste un feed `@JY` su cui
  accertare la scala, mettere un `PointValue` a caso falserebbe stop e target senza produrre alcun
  errore. I parametri sono comunque tradotti e verificabili.
- **Nessun porting è verificato sui trade.** Il datafeed su disco è solo `@NQ`: per gli altri dieci
  mercati manca la storia su cui confrontare le entrate. La procedura resta quella di
  [`porting-da-report-sweep.md`](porting-da-report-sweep.md) §"Verificare il porting".
- **`S50` e `S64` citano lo stesso file di trade** (`run_20260824_1908/.../fam02_TF_U.csv`) pur
  avendo gate short e uscite diverse. È quello che dice il dossier; in verifica va chiarito quale
  delle due quella lista descriva.

---

## I run tradotti finora

| Run | Mercato | Righe approvate | Strategie | Univoche | Tradotte |
|---|---|---|---|---|---|
| `run_20260730_0005` | NQ 15m | — | — | — | 2 (`PCH_001`, `PCH_002`) |
| `run_20260730_2127` | NQ 30m | — | — | — | nessuna |
| `run_20260814_1453` | NQ 15m | 133 | 11 | 10 | 11 |
| `run_20260815_1021` | NQ 30m | 24 | 7 | 5 | 7 |
| `run_20260819_0201` | GC 30m | 1 | 1 | 1 | 1 (`TFU_001_30`) |
| `run_20260819_0659` | GC 1h | 5 | 5 | 3 | 5 (3 PC di cui 2 disabilitate, 2 RHL) |
| `run_20260819_1008` | ES 15m | 4 | 4 | 3 | 3 (`SBO_001`, `BSW_002`, `BSW_003`) |
| `run_20260820_0012` | ES 1h | 4 | 4 | 3 | 3 (`BSW_001`, `PCH_001`, `PCH_002`) |
| `run_20260820_0856` | NQ 1h | 6 | 6 | 6 | 6 (3 TF_M, 2 TF_U, 1 BO) |

Le **righe approvate** includono la stessa strategia con stop e target diversi: sono tarature
del rischio, non sistemi distinti, e non vengono tradotte. Le **univoche** restano dopo aver
confrontato le entrate anche *fra* run diversi: è il numero che conta per l'operatività, perché
due strategie che mandano gli stessi ordini su conti separati sono copy trading.

I due run di agosto stanno in `piootoo-repository/run-engine/run-01-agosto/<run>/`; i due run
del 19 agosto — i primi su **GC** — stanno in `run-engine/run-02-agosto/` (che contiene
`run_20260819_0201`) e `run-engine/run-03-agosto/` (`run_20260819_0659`), dove la consegna sta
direttamente in `consegna/` senza la cartella `run_*` intermedia e c'e' in piu'
`IMPLEMENTAZIONE.md`, che porta le formule dei pattern (§3) e la lista delle strategie univoche
(§1) — cioe' quello che per i run di NQ stava nel dossier fuori repository. I due di
luglio stanno **fuori dal repository**, in `C:\coo+project\davide\` (e in copia dentro
`Run_Test.zip`, stessa cartella). Dentro un run:

| File | A cosa serve |
|---|---|
| `consegna/parametri.csv` | **la fonte dei parametri.** Una riga per strategia approvata; `e_strategia = True` marca quelle da tradurre |
| `consegna/schede.md` | le condizioni in prosa, famiglia per famiglia |
| `consegna/trades/famNN_MOTORE.csv` | la lista trade di riferimento della capofila: è con questa che si verifica il porting |
| `top_final.json` | metadata del run e i primi 3 candidati della classifica generale con tutti i `p_*`. **Non** contiene le 18 righe approvate |
| `report.html`, `all_combinations.csv`, `sweep/*.parquet` | vista e spazio esplorato: non servono al porting |

> ⚠ **Il dossier vive fuori dal repository.** `dossier_ctrader_NQ.md` — che è l'unico documento
> con le **formule** dei pattern (§3) e la deduplicazione a 15 strategie univoche — sta in
> `C:\coo+project\run-engine\run-01-agosto\`, cioè fuori da `piutoo/`. Se serve conservarlo, va
> copiato dentro il repository: da lì può sparire senza che nessuno se ne accorga.

## Convenzione di nome

`PTS_[SIMBOLO]_[MOTORE]_[NNN]_[TIMEFRAME]` — per esempio `PTS_NQ_TFM_006_30`. Il numero è
progressivo per motore e continuo fra timeframe e run: `PTS_NQ_TFM_006_30` è la sesta TF_M su NQ,
indipendentemente da quando è stata tradotta. `PtsNamingConventionTests` impone la forma e
l'elenco delle sigle ammesse: aggiungendo un motore il test fallisce finché la sigla non è
dichiarata lì.

| Sigla | Motore ricerca | Classe base C# |
|---|---|---|
| `TFM` | TF_M | `TfMirroredEngine` |
| `TFU` | TF_U | `TfUnmirroredEngine` |
| `SBO` | BO | `SessionBreakoutEngine` |
| `PCH` | PC | `PriceChannelEngine` |
| `RBM` | RBB_M | `RbbMirroredEngine` |
| `RHL` | RHL | `RhlEngine` |
| `BSW` | BIASW | `BiasWeeklyEngine` |

## La mappa

`S01`…`S15` sono gli identificativi del dossier, ordinati per atteso/trade decrescente.
`fam/str` sono le colonne `famiglia` e `strategia` di `parametri.csv`.

| Classe C# | Run | fam/str | Dossier | Motore | Trade di riferimento |
|---|---|---|---|---|---|
| `PTS_NQ_TFM_002_15` | `20260814_1453` | 1/1 | S03 | TF_M | `consegna/trades/fam01_TF_M.csv` |
| `PTS_NQ_TFM_003_15` | `20260814_1453` | 2/2 | S05 | TF_M | `consegna/trades/fam02_TF_M.csv` |
| `PTS_NQ_TFM_004_15` | `20260814_1453` | 2/8 | — | TF_M | *disabilitata, vedi sotto* |
| `PTS_NQ_TFM_005_15` | `20260814_1453` | 3/3 | S06 | TF_M | `consegna/trades/fam03_TF_M.csv` |
| `PTS_NQ_SBO_001_15` | `20260814_1453` | 4/4 | S07 | BO | `consegna/trades/fam04_BO.csv` |
| `PTS_NQ_SBO_002_15` | `20260814_1453` | 5/5 | S08 | BO | `consegna/trades/fam05_BO.csv` |
| `PTS_NQ_SBO_003_15` | `20260814_1453` | 6/6 | S10 | BO | `consegna/trades/fam06_BO.csv` |
| `PTS_NQ_TFU_001_15` | `20260814_1453` | 7/7 | S11 | TF_U | `consegna/trades/fam07_TF_U.csv` |
| `PTS_NQ_RBM_001_15` | `20260814_1453` | 8/9 | S13 | RBB_M | `consegna/trades/fam08_RBB_M.csv` |
| `PTS_NQ_TFU_002_15` | `20260814_1453` | 9/10 | S14 | TF_U | `consegna/trades/fam09_TF_U.csv` |
| `PTS_NQ_TFU_003_15` | `20260814_1453` | 10/11 | S15 | TF_U | `consegna/trades/fam10_TF_U.csv` |
| `PTS_NQ_TFM_006_30` | `20260815_1021` | 1/1 | S01 | TF_M | `consegna/trades/fam01_TF_M.csv` |
| `PTS_NQ_TFM_007_30` | `20260815_1021` | 2/2 | S02 | TF_M | `consegna/trades/fam02_TF_M.csv` |
| `PTS_NQ_TFM_008_30` | `20260815_1021` | 3/3 | S04 | TF_M | `consegna/trades/fam03_TF_M.csv` |
| `PTS_NQ_PCH_003_30` | `20260815_1021` | 4/4 | S09 | PC | `consegna/trades/fam04_PC.csv` |
| `PTS_NQ_PCH_004_30` | `20260815_1021` | 5/5 | S12 | PC | `consegna/trades/fam05_PC.csv` |
| `PTS_NQ_PCH_005_30` | `20260815_1021` | 5/6 | — | PC | *disabilitata, vedi sotto* |
| `PTS_NQ_PCH_006_30` | `20260815_1021` | 5/7 | — | PC | *disabilitata, vedi sotto* |
| `PTS_GC_TFU_001_30` | `20260819_0201` | 1/1 | S01 | TF_U | `consegna/trades/fam01_TF_U.csv` |
| `PTS_GC_PCH_001_60` | `20260819_0659` | 1/1 | S01 | PC | `consegna/trades/fam01_PC.csv` |
| `PTS_GC_PCH_002_60` | `20260819_0659` | 1/2 | — | PC | *disabilitata, doppione della 001* |
| `PTS_GC_PCH_003_60` | `20260819_0659` | 1/3 | — | PC | *disabilitata, doppione della 001* |
| `PTS_GC_RHL_001_60` | `20260819_0659` | 2/4 | S02 | RHL | `consegna/trades/fam02_RHL.csv` |
| `PTS_GC_RHL_002_60` | `20260819_0659` | 3/5 | S03 | RHL | `consegna/trades/fam03_RHL.csv` |
| `PTS_ES_SBO_001_15` | `20260819_1008` | 1/1 | S02 | BO | `run-05-agosto/trades/S02_15m_BO.csv` |
| `PTS_ES_BSW_002_15` | `20260819_1008` | 3/3 | S03 | BIASW | `run-05-agosto/trades/S03_15m_BIASW.csv` |
| `PTS_ES_BSW_003_15` | `20260819_1008` | 4/4 | S05 | BIASW | `run-05-agosto/trades/S05_15m_BIASW.csv` |
| — (2/2 di `20260819_1008`) | `20260819_1008` | 2/2 | — | BIASW | *non tradotta: stesse entrate di `PTS_ES_BSW_001_60`* |
| `PTS_ES_BSW_001_60` | `20260820_0012` | 1/1 | S01 | BIASW | `run-05-agosto/trades/S01_1h_BIASW.csv` |
| `PTS_ES_PCH_001_60` | `20260820_0012` | 2/— | S04 | PC | `run-05-agosto/trades/S04_1h_PC.csv` |
| `PTS_ES_PCH_002_60` | `20260820_0012` | 3/— | S06 | PC | `run-05-agosto/trades/S06_1h_PC.csv` |
| `PTS_NQ_TFM_009_60` | `20260820_0856` | 1/1 | S01 | TF_M | `run-06-gosto/consegna/trades/fam01_TF_M.csv` |
| `PTS_NQ_TFM_010_60` | `20260820_0856` | 2/2 | S02 | TF_M | `run-06-gosto/consegna/trades/fam02_TF_M.csv` |
| `PTS_NQ_SBO_004_60` | `20260820_0856` | 3/3 | S03 | BO | `run-06-gosto/consegna/trades/fam03_BO.csv` |
| `PTS_NQ_TFU_004_60` | `20260820_0856` | 4/4 | S04 | TF_U | `run-06-gosto/consegna/trades/fam04_TF_U.csv` |
| `PTS_NQ_TFM_011_60` | `20260820_0856` | 5/5 | S05 | TF_M | `run-06-gosto/consegna/trades/fam05_TF_M.csv` |
| `PTS_NQ_TFU_005_60` | `20260820_0856` | 6/6 | S06 | TF_U | `run-06-gosto/consegna/trades/fam06_TF_U.csv` |

### Le due run ES di agosto (`run-04-agosto`, `run-05-agosto`)

Le due cartelle **si sovrappongono**: `run-04-agosto` e' la consegna di `run_20260819_1008`
(solo 15m, con `parametri.csv` e `schede.md`), `run-05-agosto` e' il dossier consolidato che
somma quel run e `run_20260820_0012` (1h) e **deduplica fra i due timeframe**. La fonte
autorevole per il porting e' `run-05-agosto/dossier_ctrader_ES.md`; `run-04-agosto/parametri.csv`
resta l'unica fonte numerica riga-per-riga delle quattro strategie a 15 minuti, e le due
concordano.

Delle 8 righe approvate complessive **sei sono univoche** e sono state tradotte. Le due escluse
emettono gli stessi ordini di entrata di una tradotta e metterle su conti separati sarebbe copy
trading:

| Riga esclusa | Duplicato di | Perche' |
|---|---|---|
| `20260819_1008` fam 02 (BIASW 15m, $195/trade) | `PTS_ES_BSW_001_60` | stesse entrate, il dossier tiene la 1h |
| `20260820_0012` fam 02-2 (PC 1h) | `PTS_ES_PCH_001_60` | stesse entrate |

⚠ **Etichetta della barra, per le tre BIASW.** Il dossier dichiara che l'orario di ingresso e' la
**etichetta di chiusura** della barra; il datafeed Piootoo etichetta ogni barra sull'**apertura**.
Gli orari sono stati riportati **verbatim**, coerentemente con la regola di
[`porting-da-report-sweep.md`](porting-da-report-sweep.md): la convenzione di etichettatura e' una
questione aperta di progetto e non va compensata strategia per strategia. In verifica del porting
attendersi lo scarto di una barra gia' misurato su NQ.

⚠ **Nessun datafeed `@ES`.** Le sei classi non sono ancora state verificate contro le liste trade
di riferimento in `run-05-agosto/trades/`: serve prima un feed `@ES` a 15 e 60 minuti che copra
almeno `01/06/2021 → 30/05/2025`, il periodo fuori campione del dossier. Il confronto si fa sulle
**entrate** (timestamp e prezzo), rettificando 1 tick di slippage per lato che il riferimento
applica e l'engine no.

### Il run NQ 1h di agosto (`run-06-gosto`)

La consegna sta in `piootoo-repository/run-engine/run-06-gosto/consegna/` — stessa forma dei due
run GC: `parametri.csv`, `schede.md` e `IMPLEMENTAZIONE.md`, che porta le formule dei pattern (§3)
e la lista delle strategie univoche (§1). E' il primo run **NQ a 60 minuti** con la sorgente sul
disco: le sei righe approvate sono anche sei famiglie e sei strategie univoche — la colonna
*Equivalenti* del dossier e' vuota per tutte — quindi sono state tradotte tutte e sei e nessuna
nasce disabilitata. Il vincolo dichiarato dal run e' che nessuna coppia condivide piu' del 70%
degli ordini di entrata.

Tutti i parametri sono riportati verbatim da `parametri.csv`: finestre operative con
`ZonedWindow.ResearchHours(start_hour, end_hour)` senza conversione, sessione
`ZonedWindow.ResearchSession()` (giorno di calendario europeo), `intraday_only = 0` tradotto in
`IntradayOnly = false` esplicito su tutte e sei — sono tutte multiday. `max_bars` vale 0 solo per
la famiglia 01, che non ha uscita a tempo; la 03 non ha take profit (`ProfitMoney = 0`).

| Classe | finestra CET | stop / target ($) | max_bars | gate |
|---|---|---|---|---|
| `PTS_NQ_TFM_009_60` | 14:00 → 04:00 | 250 / 3.000 | — | neut 47/1, dir 50/8 |
| `PTS_NQ_TFM_010_60` | 00:00 → 17:00 | 2.500 / 5.000 | 48 | neut 47/11, dir -34/28 |
| `PTS_NQ_SBO_004_60` | 22:00 → 21:00 | 500 / — | 230 | neut 4/32, dir 44/28 |
| `PTS_NQ_TFU_004_60` | 17:00 → 03:00 | 750 / 10.000 | 230 | fast L 32/2, S 38/137 |
| `PTS_NQ_TFM_011_60` | 21:00 → 14:00 | 1.250 / 4.000 | 230 | neut 47/24, dir -34/16 |
| `PTS_NQ_TFU_005_60` | 16:00 → 04:00 | 4.000 / 5.000 | 46 | fast L 107/83, S 21/39 |

Per la BO: `level_source = 0` — canale delle **N sessioni complete**, il ramo storico che
`SessionBreakoutEngine` implementa — con `n_sess = 4`, `lev_include_sess0 = 0` e
`breakout_offset_ticks = 0`. Il ramo `level_source = 1` (massimo/minimo running della sessione
corrente) non e' stato selezionato dal run e non va attivato.

⚠ **Porting dichiarato, non ancora verificato sui trade.** A differenza dei run GC ed ES qui il
datafeed c'e': `piootoo-repository/datafeed/1h/@NQ` copre dal 2006, quindi tutto il periodo delle
liste di riferimento (2012-01-10 → 2025-05-29). Il confronto va fatto sulle **entrate** — timestamp
e prezzo — rettificando 1 tick di slippage per lato che il riferimento applica e l'engine no, e
tenendo presente lo scarto di etichettatura della barra gia' misurato su NQ. Procedura in
[`porting-da-report-sweep.md`](porting-da-report-sweep.md) §"Verificare il porting".

### Anteriori a questa mappa

| Classe C# | Run | Note |
|---|---|---|
| `PTS_NQ_PCH_001_15` | `20260730_0005` | top #1. Parametri confrontati con `top_final.json`: coincidono. Trade: `trades/top01_PC.csv` |
| `PTS_NQ_PCH_002_15` | `20260730_0005` | top #2, identica alla 001 tranne `p_ptn_dir_no` (53 → 6). Trade: `trades/top02_PC.csv` |
| `PTS_NQ_TFM_001_60` | **non presente su questa macchina** | Vedi sotto |

`run_20260730_2127` esiste in `C:\coo+project\davide\` ma non ha prodotto nessuna classe: e'
un run **NQ 30m** e i suoi tre candidati sono tutti TF_M con parametri che non corrispondono a
`PTS_NQ_TFM_001_60`.

Il run di `PTS_NQ_TFM_001_60` non e' ne' `0005` (NQ 15m) ne' `2127` (NQ 30m): la strategia e' a
**60 minuti**, quindi nessuno dei due poteva generarla, e non ci sono altri run sul disco. Chi lo
ritrova aggiorni questa riga — i suoi parametri sono `start_hour 16`, `end_hour 3`,
`ptn_neut 47/1`, `ptn_dir 50/8`, `stop_loss 1000`, `take_profit 3000`, `intraday_only 0`.

> **Indizio dal run del 20/08/2026.** `run_20260820_0856` e' NQ **1h** e la sua famiglia 01 —
> tradotta in `PTS_NQ_TFM_009_60` — ha la stessa firma di gate (`ptn_neut 47/1`,
> `ptn_dir 50/8`), lo stesso `take_profit 3000` e lo stesso `intraday_only 0`. Differiscono la
> finestra (14:00–04:00 invece di 16:00–03:00) e lo stop (250 invece di 1000), quindi **non e'
> la stessa riga**: e' pero' la prova che quella combinazione di gate nasce da un run NQ a 60
> minuti con orari gia' in ora europea, il che rende la finestra 16:00–03:00 della `001`
> plausibile cosi' com'e' e non da spostare di sette ore. Resta un indizio, non una misura.

## Il confine di sessione

> ⚠ **Aggiornato il 19/08/2026.** La frase storica di questo paragrafo — "tutte le classi `PTS_*`
> dichiarano `SessionStartTime = 0` / `SessionEndTime = 2359`" — **non descrive piu' il codice**:
> le classi dichiarano oggi gli orari **in ora di borsa** (`1700`/`1600` per NQ, `1800`/`1700` per
> GC), perche' `EasyEngineBase.Clock` converte l'istante UTC della barra nel fuso di
> `InstrumentSpec.SessionTimeZone` prima di confrontare. Il ragionamento sotto resta utile per
> capire *perche'* i due numeri descrivono lo stesso istante, ma i valori da scrivere in una
> classe nuova sono quelli di borsa.

### GC: mezzanotte CET sono le 18:00 di New York

La ricerca Python ricostruisce le sessioni con confine a **00:00 CET**. Per NQ quell'istante e' la
riapertura CME delle 17:00 di Chicago; per GC e' la riapertura COMEX delle **18:00 di New York**
(`InstrumentSpec.SessionTimeZone` = `America/New_York`, spec `1800`→`1700`). Le sei classi GC
dichiarano quindi `SessionStartTime = 1800` / `SessionEndTime = 1700`.

Con lo stesso ragionamento si convertono le **finestre operative**, che la ricerca scrive in CET:
per GC vale **CET − 6**, non − 7 come per gli indici CME.

| Run | finestra dichiarata (CET) | scritta nella classe (New York) |
|---|---|---|
| `20260819_0201` TF_U | 16:00 → 08:00 | `StartHour = 10`, `EndHour = 2` |
| `20260819_0659` PC | 06:00 → 05:00 | `StartTime = 0`, `EndTime = 2300` |
| `20260819_0659` RHL | 13:00 → 12:00 | `StartHour = 7`, `EndHour = 6` |

Il residuo delle settimane in cui l'ora legale europea e americana non sono allineate vale per GC
come per NQ, ed e' descritto in fondo a questa sezione.

### La codifica storica, e perche' 0 e 1700 erano la stessa cosa

La riapertura Globex delle **17:00 di Chicago e' mezzanotte in Italia** — lo stesso istante, in due
orologi. E il feed `@NQ` di questo progetto e' stampato in **ora locale europea**, non in UTC,
nonostante la `Z` nel campo `dateTime`: quella `Z` dice solo che qualcuno ha passato `UTC` allo
script di aggregazione.

Misurato sul feed a 15 minuti, con le due prove indipendenti descritte in
[`orari-di-sessione-e-fusi.md`](orari-di-sessione-e-fusi.md):

| misura | gennaio | luglio | se il feed fosse UTC vero |
|---|---|---|---|
| picco di volume (apertura cash NY, 09:30 locali) | 15:30 | 15:30 | 14:30 → 13:30, **si sposterebbe** |
| pausa di manutenzione CME | 23:15–23:45 | 23:15–23:45 | 22:00–22:45 → 21:00–21:45 |

Il picco non si sposta fra le stagioni perche' Europa e New York cambiano ora insieme. Verificato
su 2013, 2024 e 2025: il feed non e' cambiato.

Finche' `EasyLib` confronta l'orario grezzo della barra — la migrazione a `SessionClock` **non e'
completa**, vedi lo *Stato della migrazione* in `orari-di-sessione-e-fusi.md` — il numero corretto
e' `0`. Anche le finestre operative (`StartHour`/`EndHour`, `StartTime`/`EndTime`,
`StartTrade`/`EndTrade`) sono nell'orologio europeo, che e' quello in cui la ricerca le ha
scritte: nessuna conversione.

### ⚠ Cosa succede quando la migrazione sara' completa

`SessionStartTime` verra' letto in **ora di borsa**, cioe' `America/Chicago` per NQ secondo
`InstrumentSpec.SessionTimeZone`. A quel punto `0` diventera' mezzanotte di Chicago, le 07:00
italiane, e **tutte e 21 le strategie si sposterebbero di sette ore**.

| | oggi (feed europeo, orario grezzo) | dopo la migrazione (ora di borsa) |
|---|---|---|
| sessione | `0` / `2359` | `1700` / `1600` |
| finestre operative | ore CET, come la ricerca | da riconvertire in ora di Chicago |

Le due codifiche descrivono la stessa sessione: **vanno ribaltate tutte insieme**, mai una alla
volta. Chi completa la migrazione tenga presente anche che `SessionTimeZone` e' uno **per
simbolo**, e non basta: le sorgenti EasyLanguage scrivevano orari di Chicago, i run Python
scrivono orari europei, e per lo stesso simbolo servono entrambe le letture.

### Il residuo che nessuna delle due codifiche elimina

Stati Uniti ed Europa cambiano ora in date diverse — circa tre settimane a marzo e una a fine
ottobre. In quelle settimane le 17:00 di Chicago non sono mezzanotte in Italia ma le 23:00.
Misurato: il **20 marzo 2025** la pausa CME cade alle 22:15–22:45 invece che alle 23:15–23:45.
In quelle giornate il confine `0` taglia la sessione un'ora tardi.

Non e' un difetto della traduzione: la ricerca ha fatto la stessa semplificazione, quindi
riprodurla fedelmente vuol dire tenersi anche questo. Diventa un problema solo il giorno in cui si
vuole essere corretti rispetto alla borsa invece che fedeli alla ricerca — e quel giorno la strada
e' la finestra dichiarata come `(fuso IANA, orario locale)`, non un altro numero fisso.

### Le due classi corrette il 17/08/2026

`PTS_NQ_PCH_002_15` e `PTS_NQ_TFM_001_60` dichiaravano `1700`/`1600`: i numeri di Chicago
confrontati con un feed europeo, cioe' un taglio alle 17:00 italiane, in mezzo alla giornata.

Per `PCH_002` la correzione e' **misurata** sui suoi stessi trade,
`run_20260730_0005/trades/top02_PC.csv`: su 691 ingressi, il confine di mezzanotte da' zero
violazioni della regola "al massimo un ingresso per sessione e per direzione", quello delle 17:00
ne da' 35. `PCH_001`, che gia' dichiarava `0`, ne da' zero contro 48.

Per `TFM_001_60` la correzione **non e' misurabile**, perche' il suo run non e' sul disco. Regge
comunque per entrambe le provenienze possibili: `1700` come riapertura di Chicago vale `0` su
questo feed, e `1700` come mezzanotte europea di un run Python vale ugualmente `0`. Resta invece
**non verificata la sua finestra 16:00–03:00**: se fosse in ora di Chicago andrebbe spostata di
sette ore. Da decidere quando il run salta fuori.

## Cosa non è stato tradotto

I run esplorano **dodici** motori — MAC, RHL, LF, PC, VBO, BO, TF_M, RBB_M, RBB_U, TF_U, BIAS,
BIASW — ma le strategie approvate usano solo sei (`RHL` si e' aggiunto il 19/08/2026 con le due
classi GC). Dopo la rimozione del catalogo `Easy_*`
(17/08/2026) questi motori esistono in C# e **non hanno alcuna sottoclasse concreta**:

`MovingAverageCrossoverEngine`, `LevelFaderEngine`, `VolatilityBreakoutEngine`,
`RbbUnmirroredEngine`, `BiasBarCountEngine`, `BiasWeeklyEngine` — più `AroonCrossoverEngine` e
`TrendDeveloperEngine`, che non hanno neppure una controparte fra i dodici motori Python perché
nascono da sorgenti EasyLanguage.

Non sono codice morto da cancellare: il primo run che approva una riga su uno di questi motori li
riusa così come sono. Vanno però considerati **non verificati** finché una strategia non ci gira
sopra — nessun test di parità li copre più.

## Aggiungere il prossimo run

1. Prendere le righe con `e_strategia = True` da `consegna/parametri.csv`; il campo `motore` dà la
   classe base.
2. Controllare nel dossier del run quali sono **univoche**: le altre si traducono lo stesso, ma
   nascono disabilitate con `[StrategiaDisabilitata]` e la ragione scritta.
3. Numerare proseguendo il contatore del motore, senza ripartire da 1 per il nuovo timeframe.
4. Se il motore è nuovo, dichiarare la sigla in `PtsNamingConventionTests.EngineCodes`.
5. Aggiornare questa mappa e la tabella dei run in testa.
6. Verificare il porting contro `consegna/trades/famNN_*.csv` seguendo
   [`porting-da-report-sweep.md`](porting-da-report-sweep.md) §"Verificare il porting".

## Da fare sui due run GC

1. **Manca il datafeed `@GC`.** `piootoo-repository/datafeed/{30m,1h}/` contiene solo `@NQ`:
   finche' non c'e' la storia di GC, le sei classi non possono essere confrontate con le liste
   trade di riferimento e restano un porting *dichiarato ma non verificato*.
2. **Il motore `RhlEngine` non e' mai stato verificato contro il Python.** Copre ora due classi
   attive; `RhlEngineParityTests` prova il contratto del segnale, non la parita' dei trade.
   Il 19/08/2026 il motore ha ricevuto `IntradayOnly` e la dichiarazione di
   `MaxEntriesPerSession` sul segnale, che prima mancavano del tutto — quindi anche gli
   ingressi cambiano rispetto a com'era il motore prima.
3. **Estendere ai nuovi multiday il test parametrico** di `Pts002PcTests` sul mancato
   `IntradayOnly`, come chiede `porting-da-report-sweep.md`.

## Riferimenti codice

- `Piootoo.Strategies/PiutooStrategies/` — le classi `PTS_*`
- `Piootoo.Strategies/Easy/Engines/` — i motori; `EasyLib.cs` — la libreria dei pattern
- `Piootoo.Shared/Interfaces/StrategiaDisabilitataAttribute.cs` — esclusione dal catalogo
- `Piootoo.Core/Services/StrategyFactory.cs` — `GetRegisteredStrategies`, che applica l'esclusione
- `Piootoo.Strategies.Tests/PtsNamingConventionTests.cs` — convenzione di nome e sigle dei motori
