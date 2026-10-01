# Registro dei piani di trading

Scheda di ogni `TradingPlan` esistente: a cosa serve, cosa contiene, com'e' andato. **Si aggiorna a
ogni piano creato, modificato o abbandonato.** La fonte vera resta `plans/plans.json` del workspace:
se questa pagina e il file non concordano, ha ragione il file.

Stato al **28/09/2026**. Server acceso: `GET api/v1/workspaces/{ws}/trading-plans` elenca i piani
di un workspace.

## Come leggere i numeri

- **Backtest interno** = run con `PlanCode`, feed e spread FTMO (mediana per ora UTC), 27/08/2025 →
  10/09/2026 salvo dove indicato, capitale 100.000. Le size sono **neutre**: 1 contratto del future
  per ogni strategia, e il `SizeMultiplier` del piano **non** si applica.
- **Sul conto** FTMO una strategia apre **0,1 × `SizeMultiplier`** della size neutra, uguale per
  ogni simbolo (tabella `cfd-ctrader-ftmo`, verificato sul run cTrader di `PT5DAV-41`). Il netto
  interno si porta sul conto moltiplicando per quel fattore.
- **DD %** e' il `maxDrawdown` del summary, in percento sul capitale alla size neutra: confronta i
  piani fra loro, non dice il drawdown del conto.
- Regole FTMO su 100k: perdita giornaliera 5.000, perdita massima 10.000.

## Caratteristiche comuni

Salvo dove indicato, tutti i piani PT5DAV hanno: broker **FTMO**, conto **17202911** (conto di
prova, gli stessi piani girano tutti li'), commissione **2** per contratto, overnight e overweek
**ammessi**, flat di sessione **20:45 UTC** per 30 minuti, flat di fine settimana 20:45 → 23:00 UTC,
sizing di portafoglio spento.

## Come usare i best plan sui conti (28/09/2026)

| best plan | conto 100k: netto run cBot (periodo) | DD run cBot | DD periodo lungo | size | ruolo |
|---|---|---:|---:|---:|---|
| `FTMO-PT3B-EUROPA` | +10.073 (06/2025-09/2026) | 3.505 | ~5.400 (2022-2025) | 1 | conto 1 |
| `FTMO-PT3B-USA` | +5.064 (10/2025-09/2026, 19 trade) | 1.030 | ~2.100 | 1 | conto 2, pochi trade |
| `PT5DAV-O4-X05` | +12.118 (08/2025-09/2026) | 4.109 | n/d | 0,5 | conto 3, irregolare |
| `FTMO-PT3B-INDICI` | +14.604 (08/2025-09/2026) | 2.704 | ~5.400 | 1 | alternativa a EUROPA + USA su un conto solo |
| `PT5DAV-O4` | +24.329 | 8.382 | n/d | 1 | NON su conto vero a size 1: usare X05 |
| `PT5DAV-P1` | +81.699 | 3.965 | n/d | 1 | in campione: non per un conto vero |

- **Combinazioni su una prop**: vedi la sezione seguente, verificata anche sui trade il 29/09/2026.
  La combinazione EUROPA + USA + O4-X05 consigliata prima ha trade simultanei fra EUROPA e O4-X05.
- **Prima un conto demo** per 1-2 mesi per piano, stessi parametri; si passa al conto vero solo se il
  demo resta dentro i numeri del backtest.
- **Setup cTrader**: un'istanza del cBot operativo per conto, con server del piano, workspace e codice del
  piano; server installato su `http://localhost:5000`, senza console (vedi `lavori-in-corso.md`).
- **Controllo settimanale**: netto e drawdown del conto contro la colonna "DD periodo lungo". Allarme
  quando il drawdown del conto supera il DD del periodo lungo; **stop del piano** a 1,5 volte quel valore
  o a 7.000, quello che viene prima (limite FTMO 10.000, perdita giornaliera 5.000). Un piano fermato si
  rimisura prima di ripartire, non si ritocca.
- **Non cambiare parametri o pesi durante l'uso**: una modifica e' un piano nuovo, da duplicare e
  rimisurare.

## Combinazioni di best plan sulla stessa prop, verificate sui trade (29/09/2026)

Due controlli per ogni coppia di best plan: **strategie in comune** (masterfilter del workspace meno le
spente del piano) e **trade quasi identici** sui run cBot dei best plan (stesso simbolo, stesso lato,
ingresso entro 5 minuti). Il secondo controllo serve perche' strategie diverse possono aprire gli stessi
trade: un controllo di copy trading guarda i trade, non i nomi. Coppia **compatibile** = nessuna
strategia in comune e meno del 5% di trade simili.

**Trovato il 29/09: EUROPA e O4-X05 non hanno strategie in comune ma aprono trade simultanei.** Tre
breakout del DAX scattano sulle stesse rotture: `PT3B_FDAX_PCH_002_240` (EUROPA) con
`PT5DAV_FDAX_BOS_001_15` e `PT5DAV_FDAX_PCH_002_60` (O4). Sui 326 trade di EUROPA dal 12/08/2025:

| tolleranza sull'ingresso | trade EUROPA con un gemello in O4-X05 | di cui con BOS_001_15 | di cui con PCH_002_60 |
|---|---:|---:|---:|
| 30 secondi | 26 (8%) | 17 | 9 |
| 1 minuto | 34 (10%) | 23 | 11 |
| 5 minuti | 65 (20%) | 53 | 12 |

Lo stesso vale per INDICI e per EUROPA-O4 con O4 (16-20% entro 5 minuti). **Su conti diversi della stessa
prop questi piani non vanno insieme.** Sullo stesso conto non e' copy trading ma e' concentrazione: la
stessa rottura del DAX presa due o tre volte, ed e' gia' dentro `FTMO-EUROPA-O4`.

**Coppie** (tutte le altre fra best plan diversi sono incompatibili per strategie in comune):

| coppia | strategie in comune | trade simili entro 5 min | esito |
|---|---|---:|---|
| EUROPA × USA | - | 0% | ok |
| EUROPA × P1 | - | 0% | ok |
| INDICI × P1 | - | 0% | ok |
| USA × O4 / O4-X05 | - | 0% | ok |
| USA × P1 | - | 0% | ok |
| USA × EUROPA-O4 (e -B) | - | 0% | ok |
| EUROPA × O4-X05 | - | **18%** | **no** (breakout DAX) |
| INDICI × O4-X05 | - | **20%** | **no** (breakout DAX) |
| O4 / O4-X05 / EUROPA-O4 × P1 | NQ_BSW_001_240 | 13-15% | no |
| `FTMO-PIENO-B` × USA | - | 0% | ok |
| `FTMO-PIENO-B` × P1 | - | **12%** dei trade di P1 | **no**: `GC_PCH_001_15` (B) e `GC_PCH_003_60` (P1), 47 ingressi entro 30 secondi |

`FTMO-PIENO-B` non e' un best plan: misurato a parte il 29/09 sul suo run cTrader
(`ricerca/due-piani-pieni-2026-09-28/gemelli_piano_b.py`). **Dentro** il piano i gemelli sono il 3,5% dei
trade entro 30 secondi: la 002 con `FDAX_PCH_002_60` (13 ingressi entro 5 minuti, un quinto dei trade della
60) e `NQ_TFM_001_60` con `NQ_VBO_001_15` (9). E' concentrazione sullo stesso conto, non copy trading.

**Combinazioni massimali di best plan compatibili su una prop:**

| conti | combinazione | note |
|---:|---|---|
| **3** | **EUROPA + USA + P1** | l'unica a tre conti; **P1 e' in campione** (composto sullo stesso periodo del run): da demo prima |
| 2 | INDICI + P1 | INDICI = EUROPA + USA su un conto; stessa riserva su P1 |
| 2 | EUROPA-O4 + USA | EUROPA-O4 = EUROPA + O4 a meta' su un conto; niente P1 |
| 2 | O4-X05 + USA | senza EUROPA |

Senza P1 (per chi non vuole un piano in campione su un conto vero) i conti su una prop sono **due**:
EUROPA-O4 + USA, oppure O4-X05 + USA, oppure EUROPA + USA. Una variante di O4-X05 senza i due
breakout del DAX, per metterla accanto a EUROPA, e' stata **scartata** (29/09/2026): restano cinque
strategie e `NQ_BSW_001_240` fa il 74% del netto, cioe' un long Nasdaq tre giorni a settimana senza stop
con un contorno. Su prop diverse ogni best plan si puo' ripetere: le regole di copy trading della seconda prop
vanno lette prima (alcune guardano anche i trade identici fra firm diverse).

`FTMO-EUROPA-O4-B` e' la copia di `FTMO-EUROPA-O4` (stesse strategie, pesi, size e tenuta): conta come lo
stesso piano. I piani in produzione `FTMO-EUROPA`, `FTMO-USA`, `FTMO-O4-X05` sono sullo **stesso** conto
demo: li' la sovrapposizione EUROPA/O4-X05 e' concentrazione, non copy trading; su conti separati della
stessa prop EUROPA e O4-X05 non vanno.

Per rifare il controllo quando cambia un best plan: lo script legge l'elenco da `api/BestPlans`, le
strategie da masterfilter e piano, i trade dal run del best plan
(`ricerca/combinazioni-best-plan.ps1`).

## Piani Fintokei (30/09/2026)

Conto **4302718**, 100.000 USD, broker `FINTOKEI` (tabella `cfd-ctrader-fintokei`, simboli `_P`). Regole del conto
dette dall'utente: perdita giornaliera 5% **sull'equity**, ora del reset ignota; perdita massima 10%; overnight e
overweek ammessi; le stesse strategie in uso su FTMO si possono usare anche qui. Dalla scheda cTrader di US100_P
(01/10/2026): **commissione 0**, commissione di conversione del P&L 0%, swap alle **21:00 UTC** (piattaforma in UTC+0,
confermato), triplo il venerdi', niente swap nel fine settimana. Commissione portata da 2 a 0 su tutti i
piani Fintokei il 01/10/2026 (EUROPA-O4 dopo il suo backtest cBot); i numeri del periodo lungo qui sotto sono con
commissione 2, quindi prudenti.

**Cosa cambia rispetto a FTMO.** Manca il caffe' (`KC`), quindi O4 perde `PT5DAV_KC_LFH_001_60` (+322 nel run cBot).
Spread mediano migliore su DAX (1,16 contro 1,33), Nasdaq (1,38 / 1,45), S&P (0,50 / 0,60), peggiore su Dow (3,03 /
2,10) ed EuroStoxx (2,10 / 1,46). Swap in **percentuale annua**: tabella `swap/FINTOKEI/FINTOKEI_swap-by-symbol.csv`
scritta a mano convertendo in punti al prezzo del 21/09/2026 (Nasdaq long 4,65 punti a notte contro 6,71 di FTMO);
rollover messo alle 21:00 UTC, da verificare. Il feed Fintokei va da fine 2025 (DAX, S&P, Nasdaq, Dow, oro) o da un
mese (EuroStoxx e gli altri): troppo corto per scegliere o per il periodo lungo, serve solo al cBot.

**Piani**: `fintokei` (EUROPA, USA, USA-2, INDICI), `fintokei-pt5dav` (O4-X05 senza KC, P1 a 0,5),
`fintokei-europa-o4`. Stessa tenuta dei gemelli FTMO.

**Periodo lungo** (feed FTMO 06/2022 → 09/2026, spread orario e swap Fintokei, sul conto a 100k; *gg equity* = perdita
giornaliera peggiore dal maggiore fra saldo ed equity, sull'ora di reset peggiore delle 24,
`tools/dd-giornaliero-ftmo`):

| piano | size | trade | netto | DD chiuso | DD equity | gg equity | giorni > 2.500 | minimo sotto 100k | gemello FTMO |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| FINTOKEI-EUROPA | 1 | 1.275 | 25.674 | 5.389 | 5.714 | 1.723 | 0 | -3.229 | 25.542 / DD eq 5.732 |
| FINTOKEI-USA-2 | 1 | 215 | 11.916 | 1.887 | 4.008 | 2.017 | 0 | -1.720 | 11.852 / 4.018 |
| FINTOKEI-INDICI | 1 | 1.490 | 37.590 | 4.931 | 5.400 | 3.193 | 2 | -2.757 | (INDICI FTMO senza ES 1h: 33.823 / 5.732) |
| FINTOKEI-O4-X05 | 0,5 | 1.405 | 34.431 | 3.656 | 3.933 | 2.418 | 0 | -125 | nessun run lungo |
| FINTOKEI-USA | 1 | 77 | 8.292 | 2.083 | 4.949 | 2.017 | 0 | -1.787 | 8.281 / 4.951 |
| FINTOKEI-P1 a size 1 (prima) | 1 | 1.673 | 128.450 | 8.060 | **9.101** | **4.971** | 3 | **-8.731** | nessun run lungo |
| **FINTOKEI-P1** | **0,5** | 1.673 | 64.225 | 4.030 | 4.550 | 2.486 | 0 | -4.365 | nessun run lungo |
| FINTOKEI-EUROPA-O4 | 0,75 | 2.680 | 45.090 | 5.433 | 6.134 | 2.056 | 0 | -433 | 43.354 / 6.389 |

- I piani PT3B danno su Fintokei quasi esattamente i numeri di FTMO: lo spread piu' stretto sul DAX compensa quello
  piu' largo su Dow ed EuroStoxx.
- **P1 a size 1 non va**: il 09/04/2025 perde 4.971 in un giorno sull'equity, il limite e' 5.000, e il conto scende a
  -8.731 dal capitale. **Portato a size 0,5 il 30/09/2026**: DD equity 4.550, giorno peggiore 2.486, minimo -4.365.
  E' anche il solo piano composto sullo stesso periodo su cui e' stato misurato (in campione nel 2025-2026).
- **`FINTOKEI-USA`** (30/09/2026): le tre RHL a 4 ore senza `PT3B_ES_RHL_002_60`, per stare accanto a P1.
- Lo swap dei piani multiday e' in punti al prezzo di oggi: sul 2022-2023 e' sovrastimato, quindi O4 e P1 sono qui
  un po' pessimisti.

**Combinazioni su Fintokei** (le strategie sono quelle di FTMO, valgono le stesse compatibilita'):
- **2 conti**: EUROPA-O4 + USA-2.
- **3 conti con P1**: FINTOKEI-EUROPA + FINTOKEI-USA + FINTOKEI-P1 (0,5). Si usa USA e non USA-2, perche' USA-2 e P1
  hanno il 7% di ingressi gemelli (`PT3B_ES_RHL_002_60` con `PT5DAV_ES_RHL_001_30`).
- EUROPA e O4-X05 mai su due conti (18% di trade gemelli sulle rotture del DAX).
- Su FTMO e Fintokei si possono usare gli stessi piani.

**Backtest cBot.** Partire da **dicembre 2025**: prima cTrader non ha lo storico degli indici `_P`, e uno stream senza
storia all'avvio resta muto per tutto il run (il primo run di EUROPA-O4 dal 21/08/2024 ha operato solo sull'oro, 29
trade). I trade **non** dipendono dal fuso impostato in cTrader: il cBot lavora in UTC e costruisce le barre da se'.

- **`FINTOKEI-EUROPA-O4`, best plan dal 01/10/2026** (run `fintokei-europa-o4-bt-20251225-2206-v7.8.12-20261001-0555`,
  25/12/2025 → 31/08/2026): 454 trade, **+11.214**, DD chiuso 4.872, DD equity 5.674, peggior giorno sull'equity 2.304,
  nessun giorno oltre 2.500, minimo -642. FTMO-EUROPA-O4 sullo stesso periodo: 452 trade, +12.097, DD equity 4.456,
  peggior giorno 2.073; i trade per strategia coincidono quasi uno a uno. Swap pagato -226, commissioni 2,52. Il campo
  commissione del piano resta 2: nel cBot conta quella del broker (0), e cambiarlo dopo il run renderebbe il run non
  promuovibile in produzione.
  Contro il gemello interno sullo stesso feed (`compare-0050`): 454 contro 454, 444 abbinati; per contratto 260.652
  cBot contro 255.836, ma sul DAX il cBot e' gonfiato del 16,5% dal cambio (conto USD, GER40 in EUR): al netto il cBot
  sta circa il 5% sotto. Lo scarto e' quasi tutto in `PT3B_FDAX_PCH_002_240` (-22.598/ctr): 10 trade solo interni,
  fra cui tre take profit da 4.496 entrati all'apertura delle 07:00/08:00 che sul cBot non nascono (probabile livello
  scavalcato in apertura e scartato; senza `log.txt` del cBot non si conferma). `PT3B_FDAX_RHL_001_240` sta +14.551 sul
  cBot quasi per un trade solo: il take profit del 23/03/2026 riempito in cTrader 480 punti oltre il livello, da non
  contare nel live.
- **`FINTOKEI-USA`, run completo** `fintokei-usa-bt-20250801-0000-v7.8.12-20261001-0608` (01/08/2025 → 25/09/2026,
  primo trade 14/10/2025): 19 trade, +5.154. Contro il gemello interno (`compare-0049`): 19 su 19 abbinati, scarto 8
  per contratto su 51.529. Il cBot paga 61 di commissione (612/ctr) che il broker non ha: e' l'impostazione del
  backtest in cTrader di quell'istanza, da azzerare. I run brevi di prima erano gia' coerenti (0 e 0 dal 01/07 al
  14/09/2025, 1 e 1 dal 25/12/2025 al 25/02/2026).
- **`FINTOKEI-USA-2`, best plan dal 01/10/2026** (run `fintokei-usa-2-bt-20250801-0000-v7.8.12-20261001-1034`,
  01/08/2025 → 25/09/2026, **con l'overnight**, avviato 16 secondi dopo il salvataggio del piano): 49 trade (30 della
  RHL S&P a 1 ora, 19 a 4 ore, primo il 14/10/2025), **+6.500**, DD chiuso 885, peggior giorno -724, minimo -94, 30
  vincenti. Il gemello intraday delle 10:00 (`…-1000`) da' gli stessi 49 trade e +6.349: l'overnight sposta l'uscita
  dal flat delle 20:45 alla fine della sessione della strategia (21:59 / 22:59 UTC), restano al flat i soli venerdi'.
  Swap pagato -20 (le uscite oltre il rollover delle 21:00), commissioni 130 che il broker non ha (impostazione del
  backtest in cTrader, come per USA): numeri un po' prudenti.
  Contro l'interno sul feed FTMO dallo stesso giorno (stesso piano, per contratto e lordo di commissioni, il cBot per
  10): 43 trade abbinati su 49 / 50, **61.562 cBot contro 57.764 FTMO**. I trade a 4 ore coincidono quasi tutti (NQ
  40.705 / 37.832, ES 7.552 / 7.253; YM il 02/04 entra alle 06:00 su Fintokei e alle 10:00 su FTMO); le divergenze
  stanno nella RHL S&P a 1 ora (26 abbinati su 30 / 31, 10.088 / 8.465), dove i prezzi del feed spostano i livelli.

**Overnight su FINTOKEI-USA e USA-2 dal 01/10/2026** (overweek resta spento). Periodo lungo interno, feed FTMO, per
contratto: USA 82.924 → 84.683, DD 34,5% → 32,2%; USA-2 119.160 → 121.472, DD 25,0% → 22,9%. Stessi trade, cambia
solo l'uscita: il flat di sessione scende da 56 a 13 trade (restano i venerdi'), il resto esce per tempo della
strategia. Nei trade interni lo swap e' 0 e il summary non lo dichiara: il guadagno va riletto sul run cBot, dove
lo swap si paga. Il run cBot USA del mattino e' intraday e non e' piu' promuovibile.

**Da fare**: backtest cBot di USA con l'overnight, poi best plan (USA-2 fatto il 01/10/2026, vedi sopra); backtest
cBot di EUROPA + P1, poi best plan con la scheda; azzerare la commissione del backtest nell'istanza cTrader.

## Piani in produzione: il broker workspace FTMO (28/09/2026)

Dalla 28/09 i piani che vanno su un conto si **promuovono** da un best plan nel broker workspace del
broker (console: dettaglio del best plan, *Promuovi in produzione…*; elenco in *Operativita' →
Produzione*). Regole in [`domini/broker-workspace.md`](domini/broker-workspace.md):

- **Codice** `FTMO-NOME`, senza la serie (per esempio `FTMO-EUROPA`), diverso dal codice di ricerca.
  A ogni cambio di conto si duplica con `-2`, `-3`: il piano vecchio si ritira e il cBot del conto
  nuovo prende il codice nuovo. Da demo a conto vero e' lo stesso passaggio.
- **Una strategia, un piano** dentro FTMO: il server lo impone, e la tabella delle compatibilita' qui
  sotto diventa un controllo alla promozione invece di una regola da ricordare. Un conto invece puo'
  eseguire piu' piani (regola tolta il 28/09): i conti si scelgono alla promozione, e il demo 17202911
  puo' tenerli tutti.
- **Il piano e' quello misurato**: strategie dal run del best plan, il resto dalla copia del piano,
  valida solo se non salvata dopo l'avvio del run. Prova in anteprima del 28/09: sette best plan su
  otto sono promuovibili; `ftmo-combo-europa-o4` no (piano salvato tre minuti dopo l'avvio del run),
  va rifatto il backtest.
- **In produzione dal 28/09/2026, tutti sul demo 17202911** (server 7.8.9):

  | produzione | dal piano di ricerca | strategie | size | best plan |
  |---|---|---:|---:|---|
  | `FTMO-EUROPA` | `FTMO-PT3B-EUROPA` | 3 (FDAX PCH_002, FDAX RHL, FESX RHL, 240) | 1 | run cBot del 28/09 05:13 |
  | `FTMO-USA` | `FTMO-PT3B-USA` | 3 (ES, NQ, YM RHL, 240) | 1 | run cBot del 28/09 05:25 |
  | `FTMO-O4-X05` | `PT5DAV-O4-X05` | 7 PT5DAV | 0,5 | run cBot del 28/09 05:27 |

  Per farli girare davvero si mette sull'istanza cTrader il **codice di produzione** al posto di
  quello di ricerca, a mercato chiuso: la sessione nuova nasce in `broker-workspaces\FTMO\sessions\`.
  Fino ad allora le istanze restano sui codici di ricerca.
- **`FTMO-EUROPA-O4-B` resta di ricerca** (decisione del 29/09/2026): contiene le strategie di
  `FTMO-EUROPA` e di `FTMO-O4-X05`, quindi non puo' entrare in produzione accanto a loro. Si tengono i
  due piani separati sullo stesso conto; EUROPA-O4-B serve da termine di paragone. **Best plan** dal
  run cBot 7.8.8 del 28/09 19:37 (08/2025 → 09/2026): +15.679 (15,7%), DD max 4.008 (3,5%, curva
  realizzata), 748 trade, 10 strategie; 2025 +1.886, 2026 +13.793. Anche il run del 29/09 09:56
  (server 7.8.9) e' best plan, e da' gli stessi numeri: le versioni 7.8.8 e 7.8.9 non cambiano
  l'esecuzione.
- **`FTMO-PT3B-INDICI` resta di ricerca** (decisione del 29/09/2026): le sue 6 strategie sono quelle
  di `FTMO-EUROPA` piu' quelle di `FTMO-USA`, e la promozione viene rifiutata finche' quei due sono
  attivi. Si tengono EUROPA e USA separati.

## Perdita giornaliera alla FTMO sui run cBot (28/09/2026)

Saldo a mezzanotte di Praga contro l'equity piu' bassa della giornata, con le posizioni aperte valutate
minuto per minuto sul feed FTMO a un minuto (long al minimo del minuto, short al massimo: prudente).
Limiti FTMO su 100k: 5.000 al giorno, 10.000 in totale. "DD chiuso" e' il drawdown sui soli trade chiusi
usato finora; "DD equity" conta le posizioni aperte. Il saldo di mezzanotte non comprende le posizioni
aperte: la perdita non realizzata che una multiday porta dentro la giornata conta nel limite.

| piano (run cBot) | trade | netto | DD chiuso | DD equity | peggior giorno chiuso | **peggior giorno FTMO** | data | giorni > 2.500 |
|---|---:|---:|---:|---:|---:|---:|---|---:|
| INDICI | 372 | 14.604 | 2.704 | 3.416 | 1.651 | 1.960 | 20/03/2026 | 0 |
| INDICI-W1 | 372 | 11.786 | 2.579 | 3.372 | 1.478 | 1.815 | 20/03/2026 | 0 |
| INDICI-X15 | 382 | 22.698 | 4.882 | 5.720 | 2.527 | 3.002 | 20/03/2026 | 2 |
| EUROPA | 363 | 10.073 | 3.505 | 4.610 | 1.685 | 2.001 | 20/03/2026 | 0 |
| USA | 19 | 5.064 | 1.030 | 1.735 | 780 | 1.133 | 23/06/2026 | 0 |
| PT5DAV-O4 | 401 | 24.329 | 8.382 | 9.080 | 1.785 | **5.029** | 29/07/2026 | 3 |
| PT5DAV-O4-X05 | 386 | 12.118 | 4.109 | 4.456 | 882 | 2.502 | 29/07/2026 | 1 |
| PT5DAV-P1 | 433 | 81.699 | 3.965 | 5.373 | 2.309 | 3.026 | 19/08/2026 | 1 |
| EUROPA-O4 (run a size 1) | 748 | 21.266 | 4.821 | 5.866 | 1.738 | 2.502 | 29/07/2026 | 1 |
| EUROPA-O4 a 0,75 (stima) | 748 | 15.949 | 3.616 | 4.399 | 1.304 | 1.876 | 29/07/2026 | 0 |
| **EUROPA-O4 a 0,75 (run)** | 748 | 15.679 | 3.679 | 4.456 | 1.296 | 1.873 | 29/07/2026 | 0 |

**O4 a size 1 supera il limite giornaliero** il 29/07/2026: quattro long aperti in un ribasso, fra cui
`NQ_BSW_001_240` senza stop, aperto dal 27/07. Sul chiuso quel giorno perde 1.785: il numero vero e' tre
volte tanto. O4 si usa solo a 0,5 (O4-X05) o dentro EUROPA-O4. Per rifarlo su un run nuovo:
`dotnet run --project tools/dd-giornaliero-ftmo -c Release -- "<trades.json>|<etichetta>|<scala>"` (piu'
run di seguito; la scala stima un'altra size, il feed a un minuto e' letto dal 05/2025 al 09/2026).

## Rendimento in 4 mesi contro la soglia del 10% (28/09/2026)

La soglia per l'aumento di capitale e' **+10% in 4 mesi** (10.000 su 100k). Netto sul conto di ogni
finestra di 4 mesi, con l'inizio spostato di una settimana alla volta, sui run interni FTMO 06/2022 →
09/2026 (× 0,1, sui soli trade chiusi). EUROPA-O4 ha girato a size 1 ed e' scalato a mano a 0,75; O4-X05
e P1 sono le loro strategie filtrate dal run neutro `ftmo-pt5dav-congelato/pt5dav-tutte-2022-2026`, non
un run del piano.

| piano | finestre | mediana | migliore | peggiore | finestre >= 10k | DD chiuso |
|---|---:|---:|---:|---:|---:|---:|
| `FTMO-PT3B-INDICI` | 208 | 2.475 | 8.253 | −3.933 | 0 | 5.407 |
| `FTMO-PT3B-EUROPA` | 208 | 1.880 | 7.124 | −3.933 | 0 | 5.407 |
| `FTMO-PT3B-USA` | 197 | 607 | 4.866 | −1.724 | 0 | 3.088 |
| `FTMO-EUROPA-O4` a 0,75 | 208 | 3.131 | 11.004 | −4.403 | 3 (1%) | 5.903 |
| `PT5DAV-O4-X05` | 205 | 2.026 | 11.188 | −2.234 | 4 (2%) | 3.742 |
| `PT5DAV-C2` | 208 | 1.352 | 6.726 | −1.195 | 0 | 2.820 |
| `PT5DAV-P1` | 205 | 4.696 | 58.105 | −7.170 | 61 (30%) | 8.763 |

- **Nessun piano fa il 10% in 4 mesi con regolarita'.** I PT3B e C2 non ci arrivano mai; O4-X05 ed
  EUROPA-O4 solo nelle finestre gennaio-aprile 2025 e 2026. Il 2023 e il 2024 sono deboli per tutti.
- **P1**: le finestre sopra soglia sono tutte da fine 2024 in poi. Fuori dal periodo di composizione
  (finestre chiuse prima del 27/08/2025) sono 21 su 152, fra 12/2024 e 04/2025 (rally dell'oro), con
  mediana 2.982; nel 2022 perde. DD chiuso vicino al limite FTMO.
- Sui run cBot (08/2025 → 09/2026) il quadro e' lo stesso: INDICI mediana 4.336 e mai sopra soglia,
  O4-X05 12 finestre su 41, EUROPA-O4 a 0,75 7 su 43, P1 37 su 38 ma in campione.
- Il 10% in 4 mesi oggi lo danno solo oro e Nasdaq negli anni di tendenza forte: con questi piani
  dipende da quando si parte, non dal piano.

## Compatibilita' fra piani sulla stessa prop (aggiornata al 27/09/2026)

Dentro una stessa prop una strategia sta su **un conto solo** (copy trading); su prop diverse si puo'
ripetere. Piani correlati ma con strategie diverse si possono mettere su conti diversi della stessa prop.
I setup sulle prop si gestiscono a mano: qui c'e' solo chi esclude chi. Strategie attive = masterfilter
meno le spente del piano.

| piani incompatibili | strategia in comune |
|---|---|
| `FTMO-PT3B-INDICI` × `FTMO-PT3B-EUROPA` | FDAX_PCH_002_240, FDAX_RHL_001_240, FESX_RHL_001_240 |
| `FTMO-PT3B-INDICI` × `FTMO-PT3B-USA` | NQ_RHL_001_240, ES_RHL_001_240, YM_RHL_001_240 |
| `PT5DAV-O4` × `PT5DAV-P1` | NQ_BSW_001_240 |
| `PT5DAV-O4` × `PT5DAV-ORO` | GC_LFD_002_60 |
| `PT5DAV-P1` × `PT5DAV-ORO` | GC_BOS_002_240 |
| `PT5DAV-ORO` × `PT5DAV-O3` | GC_PCH_001_15 |
| `FTMO-EUROPA-O4` × `FTMO-PT3B-EUROPA`, `-INDICI`, `PT5DAV-O4`, `-O4-X05`, `-P1`, `-ORO` | le strategie di EUROPA e di O4 |
| `FTMO-PIENO-B` × `FTMO-PT3B-INDICI`, `-EUROPA`, `FTMO-EUROPA-O4` e i piani della sola 002 | FDAX_PCH_002_240 (e FESX_RHL_001_240 con INDICI ed EUROPA) |
| `FTMO-PIENO-B` × `PT5DAV-C2` | GC_VBO_002_240, GC_RHL_002_240, NQ_TFM_001_60, ES_MAC_002_60, BP_PCH_001_30, BP_LFD_001_60 |
| `FTMO-PIENO-B` × `PT5DAV-O1`, `-O2`, `-O3`, `-O4`, `-O4-X05`, `-ORO`, `-P2` | ES_MAC_002_60 (O1), GC_LFH_002_60 e NQ_VBO_001_15 (O2), BP_LFD_001_60 e GC_PCH_001_15 (O3), FDAX_PCH_002_60 (O4), GC_PCH_001_15 (ORO), NQ_TFM_001_60 (P2) |
| `FTMO-PIENO-A` × `FTMO-PT3B-INDICI`, `-EUROPA`, `FTMO-PT3B-002-RHL`, `FTMO-EUROPA-O4` | FDAX_RHL_001_240 (e FDAX_BOS, NQ_BSW, NQ_LFD_004 con EUROPA-O4) |
| `FTMO-PIENO-A` × `PT5DAV-O1`..`O4`, `-O4-X05`, `-P1`, `-C2` | BP_MAC_003_240 (O1), FDAX_LFD_002_30 (O2), BP_LFD_004_30, NQ_BOS_001_15, YM_RBM_002_30 (O3), FDAX_BOS, NQ_BSW, NQ_LFD_004 (O4), NQ_BSW (P1), ES_VBO_001_30 e YM_LFH_002_30 (C2) |

`FTMO-PIENO-A` e `FTMO-PIENO-B` sono disgiunti: stanno insieme sulla stessa prop. Fra i piani operativi B
e' compatibile **solo con USA**: con P1 non ha strategie in comune ma il 12% dei trade di P1 ha un gemello
in B (oro, vedi la tabella dei trade simili sopra). A con USA, P2 e ORO (non verificato sui trade). Elenco completo, piani storici compresi:
`python ricerca/due-piani-pieni-2026-09-28/incompatibili.py <CODICE>`.

Tutte le altre coppie fra INDICI, EUROPA, USA, O1-O4, P1, P2 e ORO sono compatibili. I duplicati
(`-W1`, `-X15`, `-X05`) hanno le stesse strategie dell'originale: valgono le stesse incompatibilita', e
originale e duplicato non stanno mai insieme. `NQ_RHL_001_30` (P1) e `NQ_RHL_001_240` (USA) sono strategie
diverse ma comprano gli stessi ritorni sul Nasdaq.

Combinazione consigliata su una prop: **EUROPA + USA + O4 a size 0,5** (tre conti); con due conti
**INDICI + O4 a size 0,5**, oppure **EUROPA-O4 + USA**.

## Piani da conto (composti per essere scorrelati)

Nati il 25/09/2026 da `piootoo-plan-builder` sul run neutro delle 41 PT5DAV
(`ftmo-pt5dav-41/backtests/pt5dav-41-a-mercato-770`); resoconto in
`piootoo-repository/ricerca/piani-2026-09-25-4conti/plan-builder.md`. Obiettivo: un piano per conto,
piani diversi su conti diversi della stessa prop senza fare copy trading. **Correlazione giornaliera
fra P1 e P2: 0,04.**

### `PT5DAV-P1` — workspace `ftmo-pt5dav-41`

> **DA TENERE** (25/09/2026). Decisione dell'utente dopo il primo backtest cTrader: la curva di
> equity e' la migliore vista finora. Candidato numero uno per un conto vero.

- **6 strategie** (dal 25/09 sera): `GC_BOS_002_240`, `GC_PCH_003_60`, `NQ_RHL_001_30`,
  `NQ_BSW_001_240`, `ES_RHL_001_30`, `ES_RBM_001_30`. Tolte `BP_RBM_001_15` e `BP_BIA_001_15`:
  in cTrader facevano 192 trade per +437. Backtest interno senza le due (`bt-pt5dav-p1-senza-bp`):
  413 trade, netto 828.825, DD 38.635, giorno peggiore −15.028, **sul conto ~83k con DD ~3,9k**,
  praticamente uguale a prima con 281 trade in meno. I numeri qui sotto sono della versione a 8.
- `SizeMultiplier` 1. Correlazione massima fra i membri 0,12.
- **Backtest interno** (`bt-pt5dav-p1`): 694 trade, netto **837.360**, DD giornaliero 39.293,
  giorno peggiore −14.888. **Sul conto** (× 0,1): netto ~84k, DD ~3,9k, giorno peggiore ~−1,5k.
- Per trimestre: Q3-25 +10k, Q4-25 +95k, Q1-26 +426k, Q2-26 +237k, Q3-26 +70k.
- **Attenzione**: l'oro fa il 62% del netto (anno del rally). NQ su FTMO ha l'archivio solo dal
  01/07/2025: nel run interno le due NQ operano da fine settembre/ottobre 2025.
- **Backtest cTrader** (`pt5dav-p1-bt-20260101-2205-v7.8.0-20260925-1704`, 01/01 → 24/09/2026,
  completo): 496 trade, **+75.717** sul conto, **DD 4.098** (21/08/2026), PF 2,39, 56% vincenti,
  giorno peggiore −2.279, i 5 trade migliori valgono il 29% del netto, long e short entrambi in
  utile. Coerente con il run interno trimestre per trimestre.
- **Il rendimento cala**: gennaio-aprile 12-17k al mese, maggio 7k, giugno-settembre 1-3k al mese
  (58,6k nei primi quattro mesi, 17k nei cinque dopo). Cala su tutte le strategie, di piu' su
  `GC_BOS_002_240` (26k → 6k) e `NQ_BSW_001_240` (11k → 1,6k). Il periodo e' lo stesso su cui il
  piano e' stato composto: e' tutto in campione.
- Le due BP sono in pari (+127 e +310 su 192 trade): portano trade, non denaro.
- I tre run cTrader precedenti (dal 01/09/2025) si sono interrotti dopo un minuto e mezzo perche' il
  server si bloccava scrivendo sulla console, non per il piano. Sono incompleti e non vanno letti.

### `PT5DAV-P2` — workspace `ftmo-pt5dav-41`

- **6 strategie**: `GC_BBO_002_60`, `NQ_RBU_002_60`, `NQ_TFM_001_60`, `ES_BSW_001_15`,
  `ES_LFD_002_60`, `BP_LFH_002_30`. Tolta a mano `GC_BSW_001_240`, che il costruttore aveva messo
  (netto/DD 0,65, da sola faceva il giorno peggiore del piano).
- `SizeMultiplier` 1. Correlazione massima fra i membri 0,23.
- **Backtest interno** (`bt-pt5dav-p2`): 489 trade, netto **359.373**, DD giornaliero 27.334,
  giorno peggiore −15.760. **Sul conto**: netto ~36k, DD ~2,7k, giorno peggiore ~−1,6k.
- Per trimestre: Q3-25 −11k, Q4-25 +59k, Q1-26 +152k, Q2-26 +122k, Q3-26 +36k.
- Oro 32% del netto. Stesso limite dell'archivio NQ del P1.
- Backtest cTrader: da fare.

### Piani 3 e 4: non creati

Il costruttore li ha composti con due sole strategie GC ciascuno (correlazione fra loro 0,42): non
sono conti indipendenti. Delle 41 PT5DAV solo 24 hanno almeno 30 trade nell'anno e 13 di queste sono
GC. Per un terzo e un quarto conto servono strategie buone fuori dall'oro (PT3B FDAX, PT6EXO).

## Piani per gruppo di mercato (PT5DAV)

Tre selezioni della stessa serie, in tre workspace: `ftmo-pt5dav-41` (le 41 con tenuta >= 1,5),
`ftmo-pt5dav-storia` (prefisso `S`) e `ftmo-pt5dav-trade` (prefisso `T`). *Il criterio con cui sono
state scelte le strategie di `storia` e `trade` non e' scritto da nessuna parte: da completare.*

| Piano | Workspace | Strategie | Size | Backtest interno: trade / netto / DD % | Note |
|---|---|---:|---:|---|---|
| `PT5DAV-41` | ftmo-pt5dav-41 | 41 (tutte) | 1 | 2.800 / 2.290.631 / 66 | run neutro di riferimento, base di P1 e P2. cTrader dal 01/08/2025: 2.226 trade, +173.564 |
| `PT5DAV-ORO` | ftmo-pt5dav-41 | 6 GC | 0,5 | 555 / 649.258 / 51 | `GC_BOS_002_240`, `LFD_002_60`, `MAC_002_240`, `PCH_001_15`, `RBU_001_15`, `TFU_001_15` |
| `PT5DAV-INDICI` | ftmo-pt5dav-41 | 8 | 1 | 244 / 329.085 / 21 | ES, NQ, YM |
| `PT5DAV-COMM-FX` | ftmo-pt5dav-41 | 8 | 1 | 140 / 104.138 / 30 | BP, CC, CL, KC |
| `PT5DAV-S-ORO` | ftmo-pt5dav-storia | 2 GC | 0,5 | 110 / 147.525 / 50 | il 25/09 spente `GC_LFD_001_15` e `GC_LFH_002_60`; restano `PCH_001_15`, `LFD_002_60`. Con 4 strategie: 162 / 173.697 / 83; cTrader +8.474 su 198 trade (06/2025 → 09/2026) |
| `PT5DAV-S-INDICI` | ftmo-pt5dav-storia | 17 | 1 | 802 / 253.815 / 69 | ES, FDAX, NQ, YM. Nel backtest cTrader smetteva di emettere dal 12/09/2025 (`Pt5DavSessionStallStudy`) |
| `PT5DAV-S-COMM-FX` | ftmo-pt5dav-storia | 12 | 1 | 158 / 1.711 / 36 | BP, CL, KC: praticamente pari |
| `PT5DAV-S-BTC` | ftmo-pt5dav-storia | 4 BTC | 1 | 98 / 1.782 / 55 | **intraday** (niente overnight), commissione 232 |
| `PT5DAV-T-ORO` | ftmo-pt5dav-trade | 4 GC | 0,5 | 465 / 224.653 / 127 | DD oltre il capitale |
| `PT5DAV-T-INDICI` | ftmo-pt5dav-trade | 21 | 1 | 1.491 / **−39.267** / 83 | in perdita |
| `PT5DAV-T-COMM-FX` | ftmo-pt5dav-trade | 8 | 1 | 615 / 234.573 / 77 | BP, BTC |
| `PT5DAV-NQ-INTRA` | ftmo-pt5dav | 16 NQ | 1 | 965 / 42.195 / 119 (dal 01/12/2025) | **intraday**, flat 21:50. cTrader dal 01/08/2025: −21.055 |

## Piani PT3B (DAX)

| Piano | Workspace | Broker / conto | Strategie | Note |
|---|---|---|---|---|
| `PT3B-FDAX-FTMO` | v03-pt3b | FTMO / 17202911 | `PT3B_FDAX_PCH_002_240` | commissione 19,23 |
| `PT3B-MIX` | v03-pt3b | ICS / 2094690 | `PT3B_FDAX_PCH_002_240` | overweek no. Nato come FDAX 002 + NQ 001, oggi resta la sola 002 |
| `FTMO-PT3-DAX` | ftmp-pt3 | FTMO (broker non impostato) / 17202911 | `PT3B_FDAX_PCH_002_240` | intraday. cTrader dal 01/08/2025: 1.147 trade, +12.524 |

Il piano `PT3B-FDAX` (ICS) ha backtest cTrader in archivio ma non esiste piu' in `plans.json`.

### `FTMO-PT3B-INDICI` — workspace `ftmo-pt3b-indici` (27/09/2026)

- **6 strategie**: `PT3B_FDAX_PCH_002_240` e la regola RHL long su cinque indici
  (`PT3B_{FDAX,NQ,ES,YM,FESX}_RHL_001_240`). Broker FTMO, conto 17202911, commissione 2, **niente
  overnight**, flat 20:45 UTC per 30 minuti, `SizeMultiplier` 1.
- **Campione e fuori campione**: la regola RHL e' stata trovata sul DAX con il feed FTMO 11/2020 →
  09/2024 (`ricerca/percorso/fdax-240-ricerca-ftmo.md`); sugli altri quattro indici e' applicata
  identica, quindi li' tutto il feed e' fuori campione. Controllo RAN superato su tutti e cinque
  (`ricerca/percorso/*-240-rhl-controllo-ran.md`). Il piano non e' stato composto guardando i risultati:
  sono le strategie validate, tutte.
- **Backtest interno** (`pt3b-indici-2022-2026`, FTMO 01/06/2022 → 26/09/2026, spread per ora, swap):
  1.352 trade, netto **338.233**, DD giornaliero 54.073, net/DD 6,26, giorno peggiore −19.670. Prima del
  01/09/2025 net/DD 3,87, **dopo 3,70** (+129.150, DD 34.928): non crolla. Tutti gli anni in utile
  (2022 +61k, 2023 +37k, 2024 +49k, 2025 +104k, 2026 +87k). **Sul conto** (× 0,1): dopo il 01/09/2025
  ~+12,9k con DD ~3,5k e giorno peggiore ~−1,4k.
- Per strategia: FDAX_RHL 75 trade +131k, 002 1.163 trade +119k, NQ_RHL 42 +54k, ES_RHL 22 +15k,
  YM_RHL 13 +14k, FESX_RHL 37 +5k. **Riserve**: sono tutti indici, e le RHL si muovono insieme nei giorni
  di crollo; la 002 ha il drawdown piu' largo (53k da sola).
- Backtest cTrader: da fare.

- **Best plan** dal 27/09/2026 (run cBot 01/08/2025 → 24/09/2026: 372 trade, +14.604, DD 2.704,
  giorno peggiore −1.651), con la scheda completa nel best plan. Il piano e' quindi bloccato.

### `FTMO-PT3B-INDICI-X15` — workspace `ftmo-pt3b-indici` (27/09/2026)

- Duplicato di `FTMO-PT3B-INDICI` con **`SizeMultiplier` 1,5**, tutto il resto identico. Atteso sul
  conto, dal run a × 1: DD ~4.300, giorno peggiore ~−2.500. × 2 e' stato scartato: con la regola
  prudente del drawdown futuro doppio di quello visto supererebbe il limite FTMO di 10.000.
- Backtest cTrader: da fare, poi promozione con la sua scheda.

- **Attenzione, corretto lo stesso giorno**: sul periodo 06/2022 → 09/2025 il piano a × 1 ha un DD sul
  conto di **5.407**, non i 2.700 dei 14 mesi del cBot; a × 1,5 arriva a **8.111**, troppo vicino al
  limite di 10.000. X15 resta solo come confronto, non per un conto vero.
- **Backtest cTrader** (`ftmo-pt3b-indici-x15-bt-20250623-2105-v7.8.6-20260928-0752`, dal 23/06/2025,
  ultimo trade ad agosto 2026): 382 trade, +22.698, DD 4.882, giorno peggiore −2.527. Sullo stesso
  periodo del run a × 1 (dal 01/08/2025): +19.293, DD 4.129 contro +14.604, DD 2.704 — le quantita'
  scalano di 1,5 e il net/DD scende (4,67 contro 5,40). Il run `...-0751` accanto e' un avvio interrotto
  (20 trade): non va letto. **Non promosso**: il limite resta il DD 8.111 del periodo lungo.

### `FTMO-PT3B-INDICI-W1` — workspace `ftmo-pt3b-indici` (27/09/2026)

- Duplicato di `FTMO-PT3B-INDICI` a `SizeMultiplier` 1 con **pesi per strategia** (7.8.6), calcolati
  sui soli trade del run interno **prima del 01/09/2025**: peso = rischio della mediana / rischio della
  strategia, rischio = deviazione del P&L giornaliero annualizzata, limitato 0,2-5. `FDAX_RHL` 0,61,
  `NQ_RHL` 0,56, `ES_RHL` 0,87, `YM_RHL` 1,19, `FESX_RHL` 5, la 002 1.
- Sul conto, 06/2022 → 09/2025: netto 18.109, DD 5.161, giorno peggiore −1.537 (a pesi uguali 20.908,
  5.407, −1.967). E' il candidato per un conto vero.
- Backtest cTrader: da fare.

### `PT5DAV-C2` — workspace `ftmo-pt5dav-congelato-p2` (27/09/2026)

- Il piano 2 dei piani congelati (`ricerca/piani-congelati-2025-09-01`): 8 PT5DAV scelte e composte
  sui soli trade fino al 01/09/2025. Tenuta dei piani PT5DAV (overnight e overweek ammessi, flat 20:45
  UTC), commissione 2, `SizeMultiplier` 1.
- **Backtest interno** (`pt5dav-c2-2022-2026`, FTMO 06/2022 → 26/09/2026): prima del 01/09/2025 net/DD
  14,50, **dopo 3,87** (514 trade, +90.884, DD 23.515, giorno peggiore −12.946; sul conto ~+9,1k,
  DD ~2,4k). **Riserva forte**: dopo il congelamento 4 strategie su 8 perdono (`BP_PCH_001_30`,
  `ES_VBO_001_30`, `YM_LFH_002_30`, e `GC_RHL_002_240` quasi pari con DD 30k); il netto viene quasi
  tutto da `NQ_TFM_001_60` (+57,6k) e `GC_VBO_002_240` (+32,1k). Non si toglie niente a posteriori:
  sarebbe di nuovo selezione sul periodo di prova.
- Backtest cTrader: da fare.

### `FTMO-EUROPA-O4` — workspace `ftmo-combo-europa-o4` (28/09/2026)

- EUROPA e O4 su **un conto solo**: le 3 strategie di `FTMO-PT3B-EUROPA` a peso 1 e le 7 di `PT5DAV-O4`
  a peso 0,5, `SizeMultiplier` **0,75**. Overnight e overweek ammessi (servono a O4; le strategie di
  EUROPA sono intraday e chiudono in giornata), flat 20:45 UTC per 30 minuti, commissione 2.
- Backtest interno 06/2022 → 09/2026 a size 1 (sul conto, × 0,1): 2.690 trade, +57.806, **DD 7.594**,
  giorno peggiore −1.698; dal 01/09/2025 +17.395, DD 5.342. Troppo vicino al limite: portato a 0,75
  (atteso DD ~5.700). La parte O4 prima del 06/2025 e' in campione.
- Incompatibile sulla stessa prop con `FTMO-PT3B-EUROPA`, `FTMO-PT3B-INDICI`, `PT5DAV-O4`,
  `PT5DAV-O4-X05`, `PT5DAV-P1` e `PT5DAV-ORO`. Compatibile con `FTMO-PT3B-USA`.
- **Backtest cTrader** (`ftmo-europa-o4-bt-20250801-0000-v7.8.6-20260928-0740`, 01/08/2025 →
  25/09/2026): **girato a size 1**, perche' la size e' passata a 0,75 tre minuti dopo l'avvio (le quantita'
  lo confermano). 748 trade, **+21.266, DD 4.821**, net/DD 4,41, giorno peggiore −1.738, mesi negativi 3
  su 14 (giugno 2026 −2.843). E' la somma dei due run separati EUROPA + O4-X05 (+20.271, DD 4.780): su
  un conto solo non si guadagna diversificazione, si risparmia un conto. Atteso a 0,75: ~+16k, DD ~3,6k.
  Riserve ereditate da O4: marzo-maggio 2026 fanno due terzi del netto, `NQ_BSW_001_240` il 38%.
- **Backtest cTrader a 0,75** (`ftmo-europa-o4-bt-20250801-0000-v7.8.6-20260928-0956`, stesso periodo):
  748 trade, **+15.679, DD 3.679**, net/DD 4,26, peggior giorno FTMO 1.873, DD con le posizioni aperte
  4.456. Conferma la stima dal run a size 1 (+15.949, DD 3.616): le quantita' scalano linearmente, salvo
  l'arrotondamento al passo di lotto su GC e KC.
- **Best plan** dal 28/09/2026 sul run a 0,75, con la scheda completa; il best plan sul run a size 1
  resta, con la scheda segnata "superata". Alternativa a EUROPA + O4-X05 su due conti, mai insieme a loro.

### `FTMO-PIENO-A` — workspace `ftmo-pieno-a` (28/09/2026) — **scartato**

> **SCARTATO** (28/09/2026, decisione dell'utente) dopo il backtest interno: dopo il taglio sei strategie su
> undici perdono, il netto viene quasi tutto da `NQ_BSW_001_240`, peggior giorno FTMO 3.739. Non va su
> nessun conto; resta nel workspace come storia. Il secondo conto accanto a B aspetta le PT6EXO.

- L'altra meta' della divisione di `ricerca/due-piani-pieni-2026-09-28/esito.md`, creata su richiesta
  dell'utente dopo B. **11 strategie, nessun peso**: `PT3B_FDAX_RHL_001_240`, `PT5DAV_{NQ_BSW_001_240,
  NQ_BOS_001_15, FDAX_BOS_001_15, NQ_LFD_004_240, YM_RBM_002_30, YM_LFH_002_30, FDAX_LFD_002_30,
  ES_VBO_001_30, BP_LFD_004_30, BP_MAC_003_240}`. Stesse regole di tenuta e costi di B.
- **`SizeMultiplier` 0,87**: porta a 7.000 il DD peggiore fra prima e dopo il taglio (qui e' quello dopo).
- **Backtest interno** (`ftmo-pieno-a-2022-2026`, stessi parametri di B): 2.815 trade, netto neutro
  752.496. **Sul conto a 0,87** (× 0,087): 2022 → 08/2025 +54.173, DD 5.401, mediana 4 mesi 4.555, ≥ 10k
  13%; **09/2025 → 09/2026 +9.989, DD 7.094**, mediana 5.362, ≥ 10k 33%, finestra peggiore −4.274. Per
  anno 12,2k / 4,2k / 13,7k / 21,4k / 13,9k. Script `run_piano_a.py`.
- **Dopo il taglio perdono sei strategie su undici** (`NQ_BOS_001_15` −2.519, `FDAX_BOS_001_15` −661,
  `YM_LFH_002_30`, `ES_VBO_001_30`, le due BP); `NQ_BSW_001_240` fa da sola piu' del netto (+12.276).
- Controllo FTMO 05/2025 → 09/2026: DD con le posizioni aperte **7.605**, **peggior giorno 3.739**
  (29/07/2026, lo stesso di O4: `NQ_BSW_001_240` senza stop), tre giorni oltre 2.500.
- Diagnostica: `NQ_BOS_001_15` chiede 5.760 candele di riscaldamento, `NQ_BSW` e `NQ_LFD_004` 360 su 4h;
  `BP_MAC_003_240` senza uscite protettive. Nel run cTrader controllare `everEvaluable`.
- **Riserva**: e' la meta' debole, piu' dipendente da una strategia sola e con il giorno peggiore piu'
  vicino al limite di B. Backtest cTrader: da fare.

### `FTMO-PIENO-B` — workspace `ftmo-pieno-b` (28/09/2026)

- Il «piano B» di `ricerca/due-piani-pieni-2026-09-28/esito.md`: dal bacino di 43 (6 PT3B di INDICI, 31
  PT5DAV «onesti», 8 di C2) le 23 candidate sui soli trade **prima del 01/09/2025**, divise in due piani
  per correlazione giornaliera. Questa e' la meta' migliore; l'altra (piano A, 11 strategie) non e' stata
  creata perche' dopo il taglio rende la meta' con un DD piu' alto.
- **12 strategie, nessun peso**: `PT3B_FDAX_PCH_002_240`, `PT3B_FESX_RHL_001_240`,
  `PT5DAV_{FDAX_PCH_002_60, GC_PCH_001_15, NQ_VBO_001_15, GC_LFH_002_60, GC_VBO_002_240, GC_RHL_002_240,
  NQ_TFM_001_60, ES_MAC_002_60, BP_PCH_001_30, BP_LFD_001_60}`. Broker FTMO, conto 17202911, commissione
  2, overnight e overweek ammessi, flat 20:45 UTC per 30 minuti.
- **`SizeMultiplier` 1,2**: il coefficiente che porta a 7.000 il DD peggiore fra prima e dopo il taglio
  (5.830 prima, 3.817 dopo, a × 1 sul conto). Il DD del periodo di composizione e' quello che comanda.
- Attese a 1,2, sui trade chiusi: 2022 → 08/2025 mediana di 4 mesi **5.757**, ≥ 10k nel **23%** delle
  finestre; 09/2025 → 09/2026 mediana **9.746**, ≥ 10k nel **49%**, finestra peggiore +82. Giorno
  peggiore ~2.300 (post) — da rifare con `tools/dd-giornaliero-ftmo` sul run vero.
- Dopo il taglio perdono `BP_PCH_001_30` (−712) e restano quasi pari `GC_RHL_002_240` e le BP: **non si
  tolgono**, sarebbe selezione sul periodo di prova.
- **Backtest interno** (`ftmo-pieno-b-2022-2026`, `PlanCode`, feed/spread per ora/swap FTMO, 01/06/2022 →
  26/09/2026, 7.8.6): 3.855 trade, netto neutro 809.696. **Sul conto a 1,2** (× 0,12): +97.295, DD chiuso
  7.001 (tutto nel 2022-2025), per anno 2022 +15,2k, 2023 +4,2k, 2024 +21,0k, 2025 +17,0k, 2026 +39,7k.
  Riproduce la ricombinazione dei run neutri (script `ricerca/due-piani-pieni-2026-09-28/run_piano_b.py`).
  Controllo FTMO 05/2025 → 09/2026 (`tools/dd-giornaliero-ftmo`, scala 0,12): DD con le posizioni aperte
  **6.126**, peggior giorno FTMO **2.403** (16/10/2025), nessun giorno oltre 2.500, equity mai sotto il
  capitale iniziale di piu' di 217.
- Diagnostica del run: `NQ_VBO_001_15` chiede 5.760 candele e `NQ_TFM_001_60` 1.440 di riscaldamento;
  nel run cTrader controllare `everEvaluable` in `session-summary.json`. `ES_MAC_002_60` non ha uscite
  protettive (esce solo con il segnale opposto e il flat).
- **Backtest cTrader** (`ftmo-pieno-b-bt-20250801-0000-v7.8.6-20260928-1100`, 01/08/2025 → 25/09/2026, size
  1,2): 1.097 trade, **+39.275**, DD chiuso **5.598** (09 → 25/03/2026), DD con le posizioni aperte 7.360,
  peggior giorno FTMO **2.562** (10/03/2026), un solo giorno oltre 2.500, equity al minimo 1.308 sotto il
  capitale iniziale. Tutte le 12 strategie `everEvaluable`, diagnostica vuota. Contro l'interno sullo
  stesso periodo (1.063 trade, +40.185, DD 4.580) il netto differisce del 2% e ogni strategia e' entro
  pochi trade; lo scarto maggiore e' `BP_PCH_001_30` (−1.394 contro −795) e marzo 2026 (−1.407 contro
  −211), che fa il DD piu' largo. Finestre di 4 mesi sul run cTrader: 43, mediana 9.371, **19 su 43
  sopra 10k (44%)**, peggiore −2.276. Script `ricerca/due-piani-pieni-2026-09-28/confronto_ctrader.py`.

### `FTMO-PT3B-USA-2` — workspace `ftmo-pt3b-usa2` (30/09/2026)

- Le tre RHL a 4 ore di `FTMO-PT3B-USA` (`PT3B_{ES,NQ,YM}_RHL_001_240`) piu' **`PT3B_ES_RHL_002_60`**, la RHL
  dell'S&P a 1 ora trovata dal percorso con il flat prima del rollover. Stessa tenuta, conto e commissione di USA:
  broker FTMO, conto 17202911, commissione 2, niente overnight, flat 20:45 UTC per 30 minuti, size 1, nessun peso.
  Workspace a parte perche' `ftmo-pt3b-indici` ha un masterfilter comune a INDICI, EUROPA e USA.
- **La strategia nuova** (`ricerca/percorso/es-60-ricerca-ftmo-flat.md`, `es-60-rhl-flat-verifiche.md`): long a
  limite 10 tick sotto il minimo di ieri, niente domenica ne' pattern direzionale 1, stop 0,8 ATR, uscita dopo 4
  barre o alle 21. Prova FTMO 09/2024-09/2026: 50 trade, +15.688, net/DD 5,42. S&P interno 2006-2020: 14 anni su 15
  in utile, net/DD 4,38. Contro il caso: 98-100° percentile. Riproduce il contenitore trade per trade. Con la RHL a
  4 ore dello stesso mercato nessun ingresso entro 5 minuti. La stessa regola sul Nasdaq (storia 2008-2022 net/DD
  0,36) e sul Dow (91° percentile) non regge: solo S&P.
- **Compatibilita'**: con `PT5DAV_ES_RHL_001_30` di P1 il 7% degli ingressi e' gemello entro 5 minuti: **USA-2
  non va accanto a P1** su conti diversi della stessa prop. Chi usa P1 tiene USA; chi non lo usa puo' passare a
  USA-2. USA-2 e USA hanno le stesse tre RHL a 4 ore: mai insieme.
- Server 7.8.12 (rilascio del 30/09 con la classe nuova). Backtest cTrader: da fare.

### `FTMO-PT3B-EUROPA` e `FTMO-PT3B-USA` — workspace `ftmo-pt3b-indici` (27/09/2026)

- Il piano indici diviso in due piani **disgiunti**, per due conti diversi (stesse regole di tenuta e
  costi di `FTMO-PT3B-INDICI`, size 1, nessun peso). Non si usano insieme a `FTMO-PT3B-INDICI`: hanno le
  stesse strategie.
- **EUROPA**: 002 DAX, RHL DAX, RHL EuroStoxx. Backtest interno 06/2022 → 09/2026 sul conto (× 0,1):
  1.275 trade, +25.542, DD 5.407, net/DD 4,72, giorno peggiore −1.449; dopo il 01/09/2025 +7.852, DD
  2.977. Tutti gli anni in utile.
- **USA**: RHL Nasdaq, S&P, Dow. Backtest interno sul conto: 77 trade in 4 anni, +8.281, DD 2.084,
  net/DD 3,97; dopo il 01/09/2025 +5.063, DD 1.054; il 2024 in perdita (−930). Opera pochissimo.
- Backtest cTrader: da fare, poi best plan.

### Piani `PT5DAV-O1`..`O4` (altra sessione), letti il 27/09/2026

- Composti sulla storia della ricerca fino al 30/05/2025 da un bacino di 33: il periodo del broker e'
  fuori campione per la composizione. Backtest cTrader (dal 06-08/2025): O1 +3.659 DD 3.468 (1,06), O2
  +4.981 DD 3.413 (1,46), O3 +13.165 DD 8.112 (1,62, cinque strategie su otto in perdita), **O4 +24.329
  DD 8.382 (2,90)**.
- **`PT5DAV-O4` promosso a best plan** con la scheda: DD vicino al limite FTMO a size 1, NQ_BSW_001_240
  fa il 70% del netto. Creato **`PT5DAV-O4-X05`** (size 0,5) come versione da conto; backtest cTrader da
  fare. Attenzione: O4 e `PT5DAV-P1` condividono `NQ_BSW_001_240`, non si usano su due conti insieme.

### `PT5DAV-C2-W1` — workspace `ftmo-pt5dav-congelato-p2` (27/09/2026) — **scartato**

- `PT5DAV-C2` con i pesi dai dati fino al 01/09/2025 (BP_LFD 3,28, BP_PCH 4,47, ES_VBO 1,36, ES_MAC
  0,98, NQ_TFM 0,96, GC_RHL 0,85, YM_LFH 0,59, GC_VBO 1).
- **Backtest cTrader** 01/08/2025 → 24/09/2026: 579 trade, **+4.275**, DD 4.229, net/DD 1,01, 6 mesi
  negativi su 14. `BP_PCH_001_30`, la meno volatile e quindi col peso piu' alto, perde −5.375: il peso
  ha moltiplicato proprio la strategia che dopo il congelamento non funziona. A pesi uguali il motore
  interno dava ~+9k con DD ~2,4k sul conto.
- Lezione sui pesi (da decidere come regola, non da applicare a posteriori qui): pesare per volatilita'
  da' molta size alle strategie tranquille; un tetto piu' basso di 5 (per esempio 2) limiterebbe il danno.

### `PT5DAV-ORO` fuori campione (analisi del 27/09/2026)

- Backtest cTrader 23/06/2025 → 24/09/2026 (size 0,5): +30.744, DD 3.314, ma l'82% del netto e' in
  gennaio-aprile 2026 (rally dell'oro) e `GC_BOS_002_240` ne fa il 54%.
- Con la data di congelamento al 01/09/2025 (run neutro delle 218, filtro pre-congelamento) solo
  `GC_PCH_001_15` e `GC_TFU_001_15` sarebbero state scelte; `GC_BOS_002_240` aveva net/DD 1,55 e un terzo
  su tre in utile, `GC_MAC_002_240` e `GC_RBU_001_15` erano in perdita. La versione onesta avrebbe reso
  circa un quarto (~+7k sul conto invece di ~+30k). Per anno, a size neutra: 2022 +0,6k, 2023 −13k,
  2024 +152k, 2025 +99k, 2026 +579k.

### `FTMO-PT3B-002-RHL` — workspace `ftmo-pt3b-paniere` (27/09/2026)

- `PT3B_FDAX_PCH_002_240` + `PT3B_FDAX_RHL_001_240`, stessa tenuta di `FTMO-PT3-DAX`.
- **Backtest cTrader** 12/08/2025 → 26/09/2026 (7.8.3): 342 trade, **+9.300** sul conto, DD 4.049;
  RHL 22 trade +4.361, identici al motore interno. Il primo run (7.8.2) aveva RHL a 2 trade: la classe
  scartava i livelli gia' superati, vedi `decisioni.md` e il commit 7.8.3.

## Piani PT5DAV congelati al 01/09/2025 (non creati come `TradingPlan`)

`ricerca/piani-congelati-2025-09-01/`: run neutro delle 218 PT5DAV su FTMO 06/2022 → 09/2026, candidate
filtrate e piani composti sui **soli trade fino al 01/09/2025**, poi misurati dopo senza ritocchi. Net/DD
da 14-30 prima a 1,20 / **2,87** / 1,41 / −0,22 dopo. Solo il piano 2 (`YM_LFH_002_30`, `ES_VBO_001_30`,
`GC_RHL_002_240`, `ES_MAC_002_60`, `BP_LFD_001_60`, `GC_VBO_002_240`, `BP_PCH_001_30`, `NQ_TFM_001_60`)
regge: +67.394 dopo il congelamento a size neutra, DD 23.515.

## Piani di servizio e storici

- `RACCOLTA-ICS` (raccolta-ics, ICS / 2094690): **non opera**, serve a `PiootooDatafeedSyncBot` per
  raccogliere GC, CL, BP al minuto.
- `V02-001`, `V2-ICS`, `V2-ICS-BEST2` (v02-001): contengono strategie `PT2_*`, **rimosse dal
  catalogo il 22/09/2026**. Non eseguibili, si tengono come storia.
