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

## Il confine di sessione

Tutte e 21 le classi `PTS_*` dichiarano `SessionStartTime = 0` / `SessionEndTime = 2359`. Non
significa "sessione = giorno di calendario invece che sessione CME": significa **la sessione CME,
scritta nell'orologio del feed**.

### Perche' 0 e 1700 sono la stessa cosa

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
