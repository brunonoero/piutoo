# Mappa: da quale run viene ogni strategia PTS

Ogni classe `PTS_*` è la traduzione di **una riga approvata** di un run di ricerca Python.
Questo file dice quale riga, in quale run, e con quale motore — così fra sei mesi si può
risalire dalla classe alla sua fonte senza riaprire i CSV a tentativi. La procedura di
traduzione sta in [`porting-da-report-sweep.md`](porting-da-report-sweep.md); i motori sono
descritti in [`motori-strategie.md`](motori-strategie.md).

## I run tradotti finora

| Run | Mercato | Righe approvate | Strategie | Univoche | Tradotte |
|---|---|---|---|---|---|
| `run_20260730_0005` | NQ 15m | — | — | — | 2 (`PCH_001`, `PCH_002`) |
| `run_20260730_2127` | NQ 30m | — | — | — | nessuna |
| `run_20260814_1453` | NQ 15m | 133 | 11 | 10 | 11 |
| `run_20260815_1021` | NQ 30m | 24 | 7 | 5 | 7 |

Le **righe approvate** includono la stessa strategia con stop e target diversi: sono tarature
del rischio, non sistemi distinti, e non vengono tradotte. Le **univoche** restano dopo aver
confrontato le entrate anche *fra* run diversi: è il numero che conta per l'operatività, perché
due strategie che mandano gli stessi ordini su conti separati sono copy trading.

I due run di agosto stanno in `piootoo-repository/run-engine/run-01-agosto/<run>/`; i due di
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

## Il confine di sessione: una discrepanza aperta

Tutte le classi tradotte ad agosto usano `SessionStartTime = 0` / `SessionEndTime = 2359`, cioe'
la sessione coincide col giorno di calendario del feed. Non e' un'assunzione: si misura
raggruppando gli ingressi del file di trade di riferimento e contando le violazioni di "al massimo
un ingresso per sessione e per direzione". Su sei famiglie delle due run di agosto il confine di
mezzanotte e' l'unico con zero violazioni ovunque, mentre il confine CME 17:00 e' escluso da
cinque su sei.

Fra le tre classi anteriori, pero', **due dichiarano la sessione CME**:

| Classe | `SessionStartTime`/`EndTime` | Misura sui suoi trade di riferimento |
|---|---|---|
| `PTS_NQ_PCH_001_15` | `0` / `2359` | mezzanotte: 0 violazioni &#183; CME 17:00: 48 &#183; **coerente** |
| `PTS_NQ_PCH_002_15` | `1700` / `1600` | mezzanotte: 0 violazioni &#183; CME 17:00: 35 &#183; **incoerente** |
| `PTS_NQ_TFM_001_60` | `1700` / `1600` | non misurabile: manca il run |

`PCH_001` e `PCH_002` vengono dallo stesso run, dallo stesso motore e dallo stesso mercato, e
differiscono solo per un filtro: il confine di sessione non puo' essere diverso fra le due. La
misura su `trades/top02_PC.csv` dice che per `PCH_002` il confine giusto e' mezzanotte, non le
17:00. La correzione non e' stata applicata: va decisa, perche' cambia i trade di una strategia
gia' in catalogo.

## Le tre disabilitate

Sono righe approvate dalla ricerca, tradotte correttamente, che però emettono **gli stessi ordini
di entrata** di un'altra classe. Portano `[StrategiaDisabilitata]`, quindi non compaiono nel
catalogo e non sono selezionabili nel masterfilter, ma restano istanziabili per nome perché
servono ai confronti storici.

| Classe | Equivalente a | Perché coincidono |
|---|---|---|
| `PTS_NQ_TFM_004_15` | `PTS_NQ_TFM_003_15` (S05) | `ptn_dir_yes = 52` è la sentinella sempre-vera: resta solo il filtro `ptn_dir_no = 17`, identico |
| `PTS_NQ_PCH_005_30` | `PTS_NQ_PCH_004_30` (S12) | stessa famiglia, entrate coincidenti |
| `PTS_NQ_PCH_006_30` | `PTS_NQ_PCH_004_30` (S12) | come sopra; inoltre `ptn_dir_no = 53` cade nel `default => false`, quindi non filtra nulla |

## Cosa non è stato tradotto

I run esplorano **dodici** motori — MAC, RHL, LF, PC, VBO, BO, TF_M, RBB_M, RBB_U, TF_U, BIAS,
BIASW — ma le strategie approvate usano solo cinque. Dopo la rimozione del catalogo `Easy_*`
(17/08/2026) questi motori esistono in C# e **non hanno alcuna sottoclasse concreta**:

`MovingAverageCrossoverEngine`, `RhlEngine`, `LevelFaderEngine`, `VolatilityBreakoutEngine`,
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

## Riferimenti codice

- `Piootoo.Strategies/PiutooStrategies/` — le classi `PTS_*`
- `Piootoo.Strategies/Easy/Engines/` — i motori; `EasyLib.cs` — la libreria dei pattern
- `Piootoo.Shared/Interfaces/StrategiaDisabilitataAttribute.cs` — esclusione dal catalogo
- `Piootoo.Core/Services/StrategyFactory.cs` — `GetRegisteredStrategies`, che applica l'esclusione
- `Piootoo.Strategies.Tests/PtsNamingConventionTests.cs` — convenzione di nome e sigle dei motori
