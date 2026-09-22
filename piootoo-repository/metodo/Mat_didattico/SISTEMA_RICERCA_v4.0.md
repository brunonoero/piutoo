# Il sistema di ricerca delle strategie — definizione v4.0

**Documento unico di ricostruzione.** Descrive, senza rimandi obbligatori ad altri file, come
il sistema trova e giudica le strategie: dati, fusi orari, sessioni, barre, simulatore, motori,
pattern, percorso di ricerca, cancelli, walk-forward, costi, consegna e controllo sul broker.
Con questo documento il sistema si può riscrivere da zero e deve dare gli stessi trade.

- **Versione della definizione:** v4.0, battezzata il 2026-09-17 (storico dal 2012 + walk-forward
  su tre livelli). È quella delle ricerche nuove.
- **Allineato al codice** della copia congelata `backup/battesimo_v40_20260917/` (cartella `unger/`).
  Dove questo documento e il codice divergono, fa fede il codice e il documento va corretto.
- **Documenti più vecchi** (`MOTORI.md`, `SPECIFICA_RICERCA_v4.html`, `Ing/REPORT_PER_INGEGNERE.md`)
  possono contenere regole superate: in caso di dubbio vale questo.
- Le regole nascono dal materiale formativo di Andrea Unger (TSS2 slide 123-288, TSE, MPS); il
  riferimento alla slide è indicato dove una regola viene da lì.

---

## Indice

1. [Il flusso in una pagina](#1-il-flusso-in-una-pagina)
2. [Dati](#2-dati)
3. [Tempo: fuso, etichette delle barre, sessioni](#3-tempo-fuso-etichette-delle-barre-sessioni)
4. [Il simulatore](#4-il-simulatore)
5. [Metriche e UngerFit](#5-metriche-e-ungerfit)
6. [Soglie e griglie calcolate per cella](#6-soglie-e-griglie-calcolate-per-cella)
7. [Le librerie di pattern (formule complete)](#7-le-librerie-di-pattern-formule-complete)
8. [I 16 motori](#8-i-16-motori)
9. [Il percorso di ricerca v4 (un motore)](#9-il-percorso-di-ricerca-v4-un-motore)
10. [Dalla strategia completa al giudizio (una cella)](#10-dalla-strategia-completa-al-giudizio-una-cella)
11. [La walk-forward](#11-la-walk-forward)
12. [Il modello dei costi CFD](#12-il-modello-dei-costi-cfd)
13. [Consegna, registro, paniere e prova sul broker](#13-consegna-registro-paniere-e-prova-sul-broker)
14. [Configurazione in vigore e riga di comando](#14-configurazione-in-vigore-e-riga-di-comando)
15. [Riproducibilità: metadata e run di prima](#15-riproducibilità-metadata-e-run-di-prima)
16. [Mappa dei file sorgente](#16-mappa-dei-file-sorgente)
17. [Glossario](#17-glossario)

---

## 1. Il flusso in una pagina

Una **cella** è un mercato su un timeframe (es. NQ 15 minuti). Per ogni cella:

1. **Dati.** Minuti del future continuo (TradeStation), dal 2012-01-01 al 2025-05-30, in ora di Roma.
2. **Barre.** I minuti si aggregano nel timeframe della cella dentro la sessione del mercato; ogni
   barra porta le OHLC della sessione in corso e delle 5 precedenti.
3. **Soglie della cella.** ATR giornaliero e di barra, soglia minima del guadagno medio per trade,
   griglie di stop e target in multipli dell'ATR giornaliero.
4. **Per ognuno dei 16 motori, il percorso v4** sulla **storia intera**: un passo alla volta
   (trigger, finestra oraria, stop, target, pattern YES, NO, direzionale, calendario, affinamenti),
   scegliendo sui primi 2/3 degli anni e confermando ogni passo sull'ultimo terzo. Il simulatore
   risolve stop e target **sul minuto**. Esce **una strategia completa** per motore, o nessuna.
5. **Filtri minimi** sulla storia intera: ≥ 50 trade, ≥ 12 all'anno in media, ≥ 5 in ogni anno,
   guadagno medio sopra soglia, almeno metà degli anni in utile.
6. **Ultimo terzo** (≈ dicembre 2020 → maggio 2025): in utile e NetProfit/MaxDD ≥ 1,3.
7. **Doppioni fuori**: stesse metriche, stessa entrata con rischio diverso, stesse entrate (≥ 99%).
8. **Batteria dei cancelli** sulla strategia: guadagno in almeno 2 terzi della storia su 3, plateau
   dei parametri, costi CFD veri con lo swap, pattern che battono i pattern casuali e il senza-filtro,
   nessun trade che fa da solo più del 30% del profitto.
9. **Walk-forward** per motore e mercato (5 finestre ancorate, la ricerca rifatta in ognuna):
   **fuori** (prove in perdita), **seconda scelta** o **prima scelta**.
10. **Famiglie di entrate** (≥ 70% di entrate in comune = stessa idea), **consegna** di prima e
    seconda scelta, **controllo del paniere** sul minuto del broker (luglio 2025 → oggi), una
    strategia per famiglia, 1 contratto, costi CFD: in utile e con drawdown non oltre il peggiore
    della sua storia riportata alla volatilità di oggi → **demo sì**.

---

## 2. Dati

### 2.1 Sorgenti

| sorgente | uso | formato |
|---|---|---|
| **CSV TradeStation** `@<SIMBOLO>-*.csv`, future continuo retro-aggiustato, 1 minuto | ricerca e simulatore | `Date,Time,Open,High,Low,Close,TotalVolume`, data `GG/MM/AAAA`, ora `HH:MM:SS`, **ora di Roma** (naive, con ora legale), **minuto etichettato alla FINE** |
| **CSV Dukascopy** convertiti (solo NK, HK) | ricerca | stesso formato e stessa convenzione dei CSV TradeStation |
| **Feed a 1 minuto del broker FTMO** (14 simboli) | prova in avanti del paniere | UTC vero, barre etichettate all'**inizio**; cache in `forward/feed_m1_cache/` |

- Il file si trova per glob `@<SIMBOLO>-*.csv` nella cartella dati (preferito il nome più corto), o
  col nome esplicito `file:` nel registro. Cartella dati: variabile `UNGER_DATA_DIR`, poi `data_dir`
  del registro.
- **Conversione all'ingresso:** l'indice dei CSV TradeStation/Dukascopy si sposta di **−1 minuto**
  (`csv_etichetta: fine`): il minuto `00:01` del file diventa `00:00` (copre 00:00-00:01).
- **Prezzi retro-aggiustati:** i livelli di prezzo storici non sono prezzi di mercato. Per questo
  ogni costo in percentuale del nozionale usa l'**ultimo close** della serie (§12).

### 2.2 Periodo

- **Floor globale 2012-01-01** (`start_date` del registro): i minuti prima si scartano **prima** di
  aggregare. Fine dati 2025-05-30. Circa 13,4 anni.
- **Pavimenti per simbolo** (vincono sul floor globale e su `--start-date`): HK e NK dal
  **2018-05-01** (prima la serie Dukascopy ha un'altra struttura di sedute). Storie più corte per
  natura: NE1 dal 2013-10, BTC dal 2017-12.
- **Orario di mercato** (oggi solo **FDAX**): si tengono solo i minuti che iniziano in
  **[08:00, 22:00)** ora di Roma, in ogni anno e in ogni sorgente (CSV e broker), prima di aggregare.
  Motivo: il future tratta 08-22 fino al 2018 e 01:10-22 dal 2019, il CFD 00-23; una sola fascia
  rende omogenee le tre serie.

### 2.3 Registro dei simboli (`unger/configs/symbols.yaml`)

`bpv` = big point value (valuta per punto), `commissione` = costo per trade andata e ritorno nel
backtest dei future, `slippage` = tick per lato. **Sessione** = ora d'inizio della giornata di
sessione (§3.3). Valuta del P&L = valuta del contratto (FDAX, FESX, FGBL in EUR: il sistema non
converte).

| simbolo | contratto | bpv | tick | commissione | slippage | sessione |
|---|---|---|---|---|---|---|
| GC | Gold COMEX | 100 | 0,1 | 6 | 1 | 00:00 Roma |
| SI | Silver COMEX | 5000 | 0,005 | 6 | 1 | 00:00 |
| HG | Copper COMEX | 25000 | 0,0005 | 6 | 1 | 00:00 |
| PL | Platinum NYMEX | 50 | 0,1 | 6 | 1 | 00:00 |
| CL | Crude WTI NYMEX | 1000 | 0,01 | 6 | 1 | 00:00 |
| NG | Natural Gas NYMEX | 10000 | 0,001 | 6 | 1 | 00:00 |
| HO | Heating Oil NYMEX | 42000 | 0,0001 | 6 | 1 | 00:00 |
| RB | RBOB NYMEX | 42000 | 0,0001 | 6 | 1 | 00:00 |
| ES | E-mini S&P 500 | 50 | 0,25 | 4 | 1 | 00:00 |
| NQ | E-mini Nasdaq 100 | 20 | 0,25 | 4 | 1 | 00:00 |
| RTY | E-mini Russell 2000 | 50 | 0,1 | 4 | 1 | 00:00 |
| YM | E-mini Dow | 5 | 1 | 4 | 1 | 00:00 |
| NK | Nikkei 225 (CME NKD, da Dukascopy) | 5 | 5 | 6 | 1 | **07:00 ora di Tokyo**, dati dal 2018-05-01 |
| HK | Hang Seng (in USD, da Dukascopy) | 6,41 | 1 | 6 | 1 | **08:00 ora di Hong Kong**, dati dal 2018-05-01 |
| EC | Euro FX | 125000 | 0,00005 | 5 | 1 | 00:00 |
| BP | British Pound | 62500 | 0,0001 | 5 | 1 | 00:00 |
| JY | Japanese Yen | 125000 | 0,00005 | 5 | 1 | 00:00 |
| AD | Australian Dollar | 100000 | 0,00005 | 5 | 1 | 00:00 |
| CD | Canadian Dollar | 100000 | 0,00005 | 5 | 1 | 00:00 |
| SF | Swiss Franc | 125000 | 0,0001 | 5 | 1 | 00:00 |
| NE1 | New Zealand Dollar | 100000 | 0,0001 | 5 | 1 | 00:00 |
| US | T-Bond 30Y | 1000 | 0,03125 | 5 | 1 | 00:00 |
| TY | T-Note 10Y | 1000 | 0,015625 | 5 | 1 | 00:00 |
| BTC | Bitcoin CME | 5 | 5 | 15 | 1 | 00:00 |
| VX | VIX CFE | 1000 | 0,05 | 5 | 1 | 00:00 |
| C | Corn CBOT | 50 | 0,25 | 6 | 1 | 01:00 |
| S | Soybeans CBOT | 50 | 0,25 | 6 | 1 | 01:00 |
| W | Wheat CBOT | 50 | 0,25 | 6 | 1 | 01:00 |
| KW | KC Wheat CBOT | 50 | 0,25 | 6 | 1 | 01:00 |
| SM | Soybean Meal CBOT | 100 | 0,1 | 6 | 1 | 01:00 |
| RR | Rough Rice CBOT | 2000 | 0,005 | 6 | 1 | 01:00 |
| CC | Cocoa ICE | 10 | 1 | 8 | 1 | 01:00 |
| KC | Coffee ICE | 375 | 0,05 | 8 | 1 | 01:00 |
| SB | Sugar #11 ICE | 1120 | 0,01 | 8 | 1 | 01:00 |
| CT | Cotton #2 ICE | 500 | 0,01 | 8 | 1 | 01:00 |
| OJ | Orange Juice ICE | 150 | 0,05 | 8 | 1 | 01:00 |
| LC | Live Cattle CME | 400 | 0,025 | 6 | 1 | 01:00 |
| FC | Feeder Cattle CME | 500 | 0,025 | 6 | 1 | 01:00 |
| LH | Lean Hogs CME | 400 | 0,025 | 6 | 1 | 01:00 |
| FDAX | DAX EUREX (EUR) | 25 | 1 | 4 | 1 | **08:00**, orario di mercato 08:00-22:00 |
| FESX | Euro Stoxx 50 (EUR) | 10 | 1 | 4 | 1 | 01:00 |
| FGBL | Euro Bund (EUR) | 1000 | 0,01 | 4 | 1 | 01:00 |

**Universo di produzione della v4** (i mercati che hanno anche il minuto del broker): BP, BTC, CC, CL,
CT, ES, FDAX, GC, KC, NG, NQ, PL, SB, YM × timeframe 15m, 30m, 1h, 4h, day = 70 celle.

---

## 3. Tempo: fuso, etichette delle barre, sessioni

### 3.1 Fuso

- **Tutti i timestamp sono in ora di Roma (Europe/Rome), naive, con ora legale.**
- Il feed del broker è in UTC: si converte in ora di Roma e basta.
- **Orologio delle regole.** Per quasi tutti i simboli è Roma. **HK e NK** usano l'orologio della
  loro borsa (Asia/Hong_Kong, Asia/Tokyo), che non ha ora legale: sessioni, barre, finestre orarie
  e giorni si calcolano su quell'orologio, mentre i timestamp restano in ora di Roma.
- Il rollover dello swap del broker è alle **17:00 di New York** (§12.3).

### 3.2 Etichetta delle barre: all'INIZIO

**Ogni barra è etichettata al suo inizio.** La barra 15m "09:00" copre [09:00, 09:15); la barra
`day` porta l'inizio della sessione; la barra `week` la sua prima sessione. Vale per minuti,
barre, trade, feed del broker.

**Le regole orarie leggono invece l'istante di CHIUSURA della barra** su cui sono valutate, cioè il
momento in cui l'ordine emesso su quella barra comincia a lavorare (il `Time` di TradeStation):

```
istante_regola(barra) = inizio_barra + passo      (intraday)
istante_regola(barra) = data della sessione        (day, week)
```

Esempio 15m: la barra 13:45-14:00 ha istante-regola 14:00. Una finestra che "comincia alle 14:00"
include quella barra; l'ordine che emette lavora nella barra 14:00-14:15.

### 3.3 Sessione

La **data-sessione** di un minuto (o di una barra) è:

```
data_sessione = floor_al_giorno( t_orologio_sessione − ora_inizio_sessione )
```

Una nuova sessione comincia quando la data-sessione cambia. Ore d'inizio: 00:00 Roma per Globex US
(metalli, energia, indici US, valute, tassi, BTC, VX); 01:00 per grani, soft, carni ed EUREX; 08:00
per FDAX; 08:00 Hong Kong per HK; 07:00 Tokyo per NK.

**Regola causale delle sessioni tardive (180 minuti).** Se il **primo minuto** di una data-sessione
sta a **meno di 180 minuti** dal confine della sessione successiva, quella data-sessione appartiene
alla sessione **dopo**. Esempio: nelle settimane in cui l'ora legale USA è già cambiata e quella
europea no, il CME riapre la domenica alle 23:00 di Roma; quell'ora diventa parte del lunedì, non
una sessione a sé. La regola non va mai a cascata (una sola sessione avanti). È causale: si decide
guardando l'ora del primo prezzo.

### 3.4 Aggregazione in barre

- `open` = primo open, `high` = massimo, `low` = minimo, `close` = ultimo close, `volume` = somma.
- **Intraday (5m-4h):** le barre si formano **dentro la sessione**, a partire dal suo inizio:
  ```
  minuti_dall_inizio = (t − inizio_sessione) in minuti
  indice_bin = floor(minuti_dall_inizio / passo)
  etichetta  = inizio_sessione + indice_bin × passo
  ```
  Una barra non attraversa mai il confine di sessione. Con sessione alle 00:00 le barre 4h sono
  00-04, 04-08, …, 20-24. Su FDAX (08:00, orario 08-22) sono 08-12, 12-16, 16-20, 20-22 (l'ultima
  dura 2 ore).
- **day:** una barra per sessione, etichettata all'inizio sessione (con la regola dei 180 minuti,
  la domenica sera tardiva entra nel lunedì).
- **week:** barre `day` raggruppate per settimana che termina venerdì, etichettate alla prima sessione.
- Barre senza minuti non esistono (le assenze non si riempiono).

### 3.5 Colonne di sessione su ogni barra

Per ogni barra si calcolano:

| colonna | significato |
|---|---|
| `sess_id` | numero progressivo della sessione |
| `bar_num` | posizione della barra nella sessione, **da 0** |
| `O_d0` | open della prima barra della sessione in corso |
| `H_d0`, `L_d0` | massimo e minimo della sessione in corso **fino a questa barra inclusa** (cumulati) |
| `C_d0` | close di questa barra |
| `O_dk`, `H_dk`, `L_dk`, `C_dk` (k = 1..5) | open, massimo, minimo, close della sessione **completa** k sessioni fa |

La prima barra del dataset è sempre inizio sessione. Si **scartano** le barre per cui non esistono
ancora 5 sessioni complete (`H_d5`, `L_d5` mancanti). Sul `day` ogni barra è una sessione.

### 3.6 Filtri orari e di calendario (comuni ai motori)

- **Finestra oraria** `[start, end]` con ore intere: vera sulle barre il cui istante-regola (§3.2), in
  minuti dalla mezzanotte, soddisfa `start ≤ m ≤ end`; se `start > end` la finestra scavalca la
  mezzanotte (`m ≥ start` oppure `m ≤ end`). Valori dei motori: `start_hour = -1` → 00:00,
  `end_hour = -1` → 23:59, altrimenti `HH:00`. Entrambi −1 = nessun filtro.
- **Giorno escluso** `skip_day` (e `not_le_day`, `not_se_day`): vero se il giorno della settimana
  dell'istante-regola è diverso; 0 = lunedì … 4 = venerdì, −1 = nessun filtro.

---

## 4. Il simulatore

### 4.1 Cosa riceve

Ogni motore produce, barra per barra:

- `entries_long`, `entries_short`: condizioni di ingresso **valutate alla chiusura della barra**;
- `entry_price_long`, `entry_price_short`: livello dell'ordine (per market è l'open, ignorato);
- `entry_type`: `stop`, `limit` o `market`;
- opzionali: `exits_long`, `exits_short` (uscita forzata alla chiusura della barra),
  `single_entry_per_session` (al massimo un ingresso per direzione per sessione),
  `exit_on_session_end` (chiusura a fine sessione; se il motore non lo dice vale: **sì** sugli
  intraday, **no** su day e week).

Parametri di rischio in **valuta per contratto**: `stop_loss`, `take_profit`, `max_bars`,
`trailing_stop`, `breakeven` (0 = spento). Una posizione alla volta, 1 contratto, niente piramidi.

Il simulatore riceve anche il **feed a 1 minuto** del simbolo (indice, low, high, open), da cui
stop e target si risolvono al minuto (§4.4). Il passo della barra si deduce dall'indice (mediana
delle differenze).

### 4.2 Ordine delle operazioni per ogni barra i

1. **Scadenza degli ordini pendenti** (stop/limit): un ordine emesso sulla barra j vale **solo nella
   barra j+1**. Scade anche al cambio di sessione (solo se una sessione dura più di una barra, cioè
   non su day/week).
2. **Uscita, se in posizione**, in quest'ordine di precedenza:
   1. stop o target (al minuto, §4.4; oppure sulla barra se la barra non ha minuti);
   2. uscita forzata del motore (`exits_*` sulla barra i) → al **close** della barra;
   3. `max_bars`: se le barre in posizione (contate da 1 alla prima barra dopo quella d'ingresso)
      raggiungono `max_bars` → al close;
   4. **fine sessione** (se attiva): se la barra i è l'ultima della sessione → al close;
   5. fine dei dati → al close.
   Se non esce, aggiorna breakeven e trailing (§4.7), validi dalla barra successiva.
3. **Esecuzione degli ordini pendenti** emessi alla barra i−1 (solo se flat). Regole di fill in §4.3,
   doppio tocco in §4.5, uscita sulla stessa barra d'ingresso in §4.6.
4. **Registrazione dei nuovi ordini** della barra i (solo se flat, e con `single_entry_per_session`
   solo se in quella sessione non c'è già stato un ingresso in quella direzione):
   - stop/limit: diventano pendenti per la barra i+1;
   - **market: si entra subito all'open della barra i** (i motori market emettono il segnale già
     spostato di una barra, quindi la decisione è stata presa alla chiusura della barra i−1).
     Se long e short sono veri insieme, entra il long.
5. **Drawdown intraday** (§4.8).

Una barra può quindi chiudere un trade al punto 2 e aprirne un altro ai punti 3-4.

### 4.3 Fill d'ingresso e prezzi

Con `s` = slippage in punti = `slippage_ticks × tick`:

| ordine | condizione di esecuzione | prezzo |
|---|---|---|
| stop long | massimo ≥ livello | `max(open, livello) + s` |
| stop short | minimo ≤ livello | `min(open, livello) − s` |
| limit long | minimo **<** livello (penetrazione stretta, il solo tocco non basta) | `min(open, livello) + s` |
| limit short | massimo **>** livello | `max(open, livello) − s` |
| market long / short | sempre | `open ± s` |

Con il minuto disponibile, le condizioni si verificano **minuto per minuto** dentro la barra: il fill
avviene nel **primo minuto** che le soddisfa, con open/high/low di quel minuto (il gap all'apertura
del minuto viene rispettato).

Stop e target in punti dal prezzo d'ingresso (slippage incluso):

```
long:  stop = entrata − stop_loss/bpv      target = entrata + take_profit/bpv
short: stop = entrata + stop_loss/bpv      target = entrata − take_profit/bpv
```

P&L del trade: `long (uscita − entrata) × bpv − commissione`, `short (entrata − uscita) × bpv −
commissione`. Il prezzo d'uscita include lo slippage (−s sulle uscite long, +s sulle short).

### 4.4 Stop e target al minuto (regola dal 2026-09-12)

Le barre servono alle **regole** (segnali, finestre, pattern, fine sessione). Stop e target sono
soglie di prezzo: si leggono sul feed a 1 minuto.

- Si camminano i minuti della barra in ordine. Per un long: tocca lo stop se `low_minuto ≤ stop`,
  il target se `high_minuto ≥ target` (short speculare). **Il primo minuto che tocca decide.**
- **Prezzo:** se il minuto apre già oltre la soglia si esce all'open del minuto, altrimenti alla
  soglia; poi lo slippage. Long: stop `min(open_min, stop) − s`, target `max(open_min, target) − s`.
- **Stop e target nello stesso minuto → vince lo stop.**
- **Barra d'ingresso:** si parte **dal minuto del fill, incluso** (dentro quel minuto l'ordine fra fill
  e tocco non si sa: il tocco conta). Poi i minuti successivi della barra. Se nessuno tocca e la barra
  è l'ultima della sessione con fine sessione attiva, si esce al close della barra.
- Barra senza minuti: si legge la barra (§4.6 per la barra d'ingresso; dalla seconda barra in poi
  long: `low ≤ stop` → `min(open, stop) − s`, altrimenti `high ≥ target` → `max(open, target) − s`).
- Le uscite di **regola** (forzata, `max_bars`, fine sessione) restano al close della barra.

Motivi d'uscita registrati: `SL`, `TP`, `SL_SAMEBAR`, `TP_SAMEBAR` (sulla barra d'ingresso), `FORCE`,
`MAXBARS`, `EOD`, `BE`, `TRAIL`.

### 4.5 Doppio tocco: ordini pendenti long e short eseguibili nella stessa barra

- **Con il minuto:** entra l'ordine eseguito nel **minuto più presto**; l'altro si cancella (OCO).
  Stesso minuto → entra il long.
- **Senza minuto** (regola "percorso di TradeStation"): un livello già superato all'open parte per
  primo; altrimenti se `high − open ≤ open − low` la barra sale prima (open → massimo → minimo →
  close): per ordini stop entra il long, per ordini limit entra lo short; se no il contrario.
- Dopo un ingresso entrambi gli ordini pendenti si cancellano.

### 4.6 Uscita sulla barra d'ingresso senza minuti (lettura "ottimista")

Solo dove la barra non ha minuti:

- **market** (fill all'open, tutta la barra è dopo il fill): stop se `low ≤ stop` (prezzo = stop − s),
  target se `high ≥ target` (prezzo = target − s);
- **limit**: stop se `low ≤ stop` (prezzo `min(open, stop) − s`), target solo se `close ≥ target`;
- **stop**: stop solo se `close ≤ stop`, target solo se `close ≥ target` (il minimo potrebbe essere
  arrivato prima del fill);
- poi fine sessione se è l'ultima barra della sessione.

(Short speculare.) Esiste una lettura "pessimista" (`same_bar_pessimistic`) usata solo per misure.

### 4.7 Breakeven e trailing

Aggiornati **dopo** i controlli d'uscita della barra, con il picco favorevole fino alla barra
(massimo per i long, minimo per gli short), quindi validi dalla barra successiva:

- breakeven: se `(picco − entrata) × bpv ≥ breakeven`, lo stop sale all'entrata (se più alto);
- trailing: `stop = picco − trailing/bpv` se più alto dello stop attuale.

Si controllano sulla barra, non al minuto. **Nel percorso v4 sono sempre spenti** (§9.2).

### 4.8 Drawdown

`max_dd` = massimo drawdown **intraday**: dopo ogni trade chiuso si aggiorna l'equity chiusa; con
posizione aperta, per ogni barra l'equity peggiore usa il minimo (long) o il massimo (short) della
barra, prima dell'aggiornamento del picco. `max_dd_closed` = drawdown sulla sola equity dei trade
chiusi. Il drawdown usato dalle metriche è il maggiore dei due.

---

## 5. Metriche e UngerFit

Da una lista di trade (`compute_metrics`):

| metrica | definizione |
|---|---|
| `n_trades`, `net_profit` | numero, somma dei P&L |
| `avg_trade` | net_profit / n_trades |
| `profit_factor`, `win_rate` | profitti lordi / perdite lorde; quota di trade > 0 |
| `max_dd` | §4.8 |
| anni | **anno di uscita** del trade; solo gli anni con almeno un trade |
| `profitable_years_ratio` | anni con somma > 0 / anni |
| `yearly_trades_mean`, `min_yearly_trades` | media e minimo dei trade per anno |
| `top_trade_pct` | miglior trade / net_profit (0 se net ≤ 0) |
| `np_dd` | net_profit / max_dd (0 se uno dei due ≤ 0) |
| `recovery_trades` | max_dd / avg_trade |

**UngerFit** (colonna `unger_score`), usato per ordinare e per il plateau:

```
E = avg_trade / min_avg_trade_della_cella        (§6.2)
R = max_dd / avg_trade                           (trade per recuperare il drawdown)
S = 100 / R
UngerFit = sqrt(E × S)        (0 se avg_trade ≤ 0 o max_dd ≤ 0)
```

---

## 6. Soglie e griglie calcolate per cella

### 6.1 ATR

- **ATR di barra** (Wilder, 14): `TR = max(H−L, |H−C_prec|, |L−C_prec|)`, media esponenziale con
  α = 1/14 (dopo 14 valori). `$ATR_barra` = **mediana** dell'ATR di barra × bpv sulla cella.
- **ATR giornaliero** (`session_atr`): lo stesso ATR(14) calcolato sulle **sessioni complete**
  (massimo, minimo, close di sessione), **spostato di una sessione** e riportato su ogni barra.
  `$ATR_giorno` = **mediana** su un valore per sessione × bpv. Sul `day` coincide con l'ATR di barra.

### 6.2 Soglia del guadagno medio (`min_avg_trade`)

```
min_avg_trade = max( 20 $,
                     2 tick × valore_tick,
                     0,15 × $ATR_barra,
                     frizione_CFD )            frizione_CFD = spread×bpv + comm% × ultimo_close × bpv
```

La frizione CFD (§12) c'è solo per i simboli con tariffa. Serve come filtro minimo e come scala
dell'UngerFit.

`min_expected_avg_trade = max(20 $, 6 tick × valore_tick, frizione_CFD)`: pavimento "6 tick" del
metodo, usato come base dei costi veri (§10.8) e del pavimento della walk-forward.

### 6.3 Griglie di rischio in multipli dell'ATR giornaliero

Ogni valore `m` diventa dollari: `punti = m × $ATR_giorno / bpv`, arrotondati a un numero intero di
tick (minimo 1), poi `× tick × bpv`. `m = 0` resta 0 (spento). Valori duplicati si tolgono tenendo
l'ordine. **Il primo valore della lista è la base.**

| griglia | intraday | day |
|---|---|---|
| stop | 0,80 · 0,10 · 0,15 · 0,20 · 0,30 · 0,40 · 0,50 · 0,65 · 1,00 · 1,25 · 1,60 · 2,20 · 10,00 | 0,80 · 0,10 · 0,15 · 0,25 · 0,40 · 0,60 · 1,20 · 2,00 |
| target | 0 · 0,20 · 0,35 · 0,50 · 0,75 · 1,00 · 1,30 · 1,70 · 2,20 · 3,00 · 4,00 · 5,50 · 8,00 | 0 · 0,40 · 0,75 · 1,20 · 1,70 · 2,50 · 4,00 · 6,00 |
| trailing | 0 · 0,30 · 0,60 · 1,20 | uguale |
| breakeven | 0 · 0,30 · 0,60 | uguale |

**Pavimento dello stop:** dalla griglia di stop si tolgono i valori sotto `0,50 × $ATR_barra`. Se
non ne resta nessuno, la griglia diventa il solo pavimento arrotondato al tick. (La calibrazione
0,80× viene dagli stop scelti a mano da Unger in TSS2: DAX 0,61×, miniSP 0,85×, CL 1,27×, Live
Cattle 0,78×.)

**Vicini di griglia:** si prendono sempre sulla griglia **ordinata per valore**, mai per posizione.

---

## 7. Le librerie di pattern (formule complete)

### 7.1 Notazione e tempi

`Ok, Hk, Lk, Ck` = colonne `O_dk, H_dk, L_dk, C_dk` (§3.5): k = 0 sessione in corso fino alla barra,
k = 1..5 sessioni complete. `close` = close della barra (= C0). Ogni pattern restituisce vero/falso
per barra; un confronto con un valore mancante è **falso**. I pattern si valutano alla **chiusura
della barra**. Per i motori con ordini stop/limit l'ordine lavora dalla barra successiva; per i
motori market la maschera del pattern è spostata di una barra (fill all'open della barra dopo).
Ogni numero di ogni libreria è ammesso: la causalità la garantisce il momento del fill.

Regola d'uso: **filtro = pattern(YES) AND NOT pattern(NO)**. Ogni libreria ha un valore
"sempre vero" (YES spento) e uno "sempre falso" (NO spento, oppure, messo come YES, spegne un lato).

### 7.2 Pattern neutrali (`pattern_neutral`, 1-54; 55 = sempre vero, 56 = sempre falso)

Definizioni: `b1 = |O1−C1|`, `r1 = H1−L1` (0 → mancante), `b5 = |O5−C1|`, `d95 = H5−L1` (può essere
piccolo o negativo, è voluto), `Hm = max(H1..H5)`, `Lm = min(L1..L5)`, `r5 = Hm−Lm` (0 → mancante),
`r2 = H2−L2`, `r3 = H3−L3` (0 → mancante), `rd = H0−L0`.

| n | condizione | n | condizione |
|---|---|---|---|
| 1 | b1 < 0,10·r1 | 28 | b5 > 0,25·r5 |
| 2 | b1 < 0,25·r1 | 29 | b5 > 0,50·r5 |
| 3 | b1 < 0,50·r1 | 30 | b5 > 0,75·r5 |
| 4 | b1 < 0,75·r1 | 31 | rd > 0,005·L0 |
| 5 | b1 > 0,25·r1 | 32 | rd > 0,0075·L0 |
| 6 | b1 > 0,50·r1 | 33 | rd > 0,010·L0 |
| 7 | b1 > 0,75·r1 | 34 | rd > 0,015·L0 |
| 8 | b1 > 0,90·r1 | 35 | rd > 0,020·L0 |
| 9 | b5 < 0,10·d95 | 36 | rd > 0,025·L0 |
| 10 | b5 < 0,25·d95 | 37 | rd > 0,030·L0 |
| 11 | b5 < 0,50·d95 | 38 | rd < 0,005·L0 |
| 12 | b5 < 0,75·d95 | 39 | rd < 0,0075·L0 |
| 13 | b5 < 1,00·d95 | 40 | rd < 0,010·L0 |
| 14 | b5 < 1,50·d95 | 41 | rd < 0,015·L0 |
| 15 | b5 < 2,00·d95 | 42 | rd < 0,020·L0 |
| 16 | b5 > 0,25·d95 | 43 | rd < 0,025·L0 |
| 17 | b5 > 0,50·d95 | 44 | rd < 0,030·L0 |
| 18 | b5 > 0,75·d95 | 45 | O0 < L1 oppure O0 > H1 |
| 19 | b5 > 1,00·d95 | 46 | H0 < H1 e L0 > L1 |
| 20 | b5 > 1,50·d95 | 47 | r1 < (r2 + r3)/2 |
| 21 | b5 > 2,00·d95 | 48 | r1 < r2 e r2 < r3 |
| 22 | b5 > 2,50·d95 | 49 | H1 < H2 e L1 > L2 |
| 23 | b5 < 0,10·r5 | 50 | H1 < H2 oppure L1 > L2 |
| 24 | b5 < 0,25·r5 | 51 | H1 > H2 e L1 < L2 |
| 25 | b5 < 0,50·r5 | 52 | H0 > H1 e L0 < L1 |
| 26 | b5 < 0,75·r5 | 53 | r1 < r2 |
| 27 | b5 > 0,90·r5 | 54 | r1 > r2 |

(Nella 31-44 `L0` = 0 è trattato come mancante.)

### 7.3 Pattern direzionali (`pattern_directional`, ±1..±51; ±52 = sempre vero, ±53 = sempre falso)

`+n` = versione long, `−n` = versione short (espressione speculare, non il negato). `range1 = H1−L1`.

| n | long (+n) | short (−n) |
|---|---|---|
| 1 | (H0−O0) > 0,25·(H1−O1) | (O0−L0) > 0,25·(O1−L1) |
| 2 | (H0−O0) > 0,50·(H1−O1) | (O0−L0) > 0,50·(O1−L1) |
| 3 | (H0−O0) > 0,75·(H1−O1) | (O0−L0) > 0,75·(O1−L1) |
| 4 | (H0−O0) > 1,00·(H1−O1) | (O0−L0) > 1,00·(O1−L1) |
| 5 | (H0−O0) > 1,50·(H1−O1) | (O0−L0) > 1,50·(O1−L1) |
| 6 | (H0−O0) > 2,00·(H1−O1) | (O0−L0) > 2,00·(O1−L1) |
| 7 | (H0−O0) > 2,50·(H1−O1) | (O0−L0) > 2,50·(O1−L1) |
| 8 | (H0−O0) > 3,00·(H1−O1) | (O0−L0) > 3,00·(O1−L1) |
| 9 | (H0−O0) < (H1−O1) | (O0−L0) < (O1−L1) |
| 10 | C1>C2, C2>C3, C3>C4 | C1<C2, C2<C3, C3<C4 |
| 11 | C1>C2>C3>C4>C5 | C1<C2<C3<C4<C5 |
| 12 | H1>H2 e L1>L2 | H1<H2 e L1<L2 |
| 13 | C1 > C2 | C1 < C2 |
| 14 | C1 > O1 | C1 < O1 |
| 15 | C1 > 1,005·C2 | C1 < 0,995·C2 |
| 16 | C1 > 1,010·C2 | C1 < 0,990·C2 |
| 17 | C1 > 1,015·C2 | C1 < 0,985·C2 |
| 18 | C1 > 1,020·C2 | C1 < 0,980·C2 |
| 19 | C1 > 1,025·C2 | C1 < 0,975·C2 |
| 20 | C1 > 1,030·C2 | C1 < 0,970·C2 |
| 21 | H0 > H1 | L0 < L1 |
| 22 | H0 > 1,0025·H1 | L0 < 0,9975·L1 |
| 23 | H0 > 1,005·H1 | L0 < 0,995·L1 |
| 24 | H0 > 1,0075·H1 | L0 < 0,9925·L1 |
| 25 | H0 > 1,010·H1 | L0 < 0,990·L1 |
| 26 | H0 > 1,015·H1 | L0 < 0,985·L1 |
| 27 | L0 > L1 | H0 < H1 |
| 28 | L0 > 1,005·L1 | H0 < 0,995·H1 |
| 29 | L0 > 1,010·L1 | H0 < 0,990·H1 |
| 30 | L0 > 1,015·L1 | H0 < 0,985·H1 |
| 31 | L0 > 1,020·L1 | H0 < 0,980·H1 |
| 32 | L0 > 1,025·L1 | H0 < 0,975·H1 |
| 33 | H1 > H5 | L1 < L5 |
| 34 | H1 < H5 | L1 > L5 |
| 35 | H1>H2 e H1>H3 e H1>H4 | L1<L2 e L1<L3 e L1<L4 |
| 36 | L1>L2 e L1>L3 e L1>L4 | H1<H2 e H1<H3 e H1<H4 |
| 37 | C1>C2 e C2>C3 e O0>C1 | C1<C2 e C2<C3 e O0<C1 |
| 38 | (H1−C1) < 0,20·range1 | (C1−L1) < 0,20·range1 |
| 39 | O0 > H1 | O0 < L1 |
| 40 | O0 > 1,0025·C1 | O0 < 0,9975·C1 |
| 41 | O0 > 1,005·C1 | O0 < 0,995·C1 |
| 42 | O0 > 1,0075·C1 | O0 < 0,9925·C1 |
| 43 | O0 > 1,010·C1 | O0 < 0,990·C1 |
| 44 | L1 > L2 | H1 < H2 |
| 45 | C1>O1 e C2>O2 | C1<O1 e C2<O2 |
| 46 | C1>O1 e C2<O2 | C1<O1 e C2>O2 |
| 47 | close > 0,99·O0 | close < 1,01·O0 |
| 48 | close > 0,995·O0 | close < 1,005·O0 |
| 49 | close > O0 | close < O0 |
| 50 | close > 1,005·O0 | close < 0,995·O0 |
| 51 | close > 1,010·O0 | close < 0,990·O0 |

**Uso "mirrored" nei motori:** con parametro `d` (può essere negativo):
- trend (TF_M, BO, BO_S, PC, VBO): `long = dir(+d) AND NOT dir(+dno)`, `short = dir(−d) AND NOT dir(−dno)`;
- reversal (RBB_M, RHL): segno **invertito**: `long = dir(−d) AND NOT dir(−dno)`, `short = dir(+d) AND NOT dir(+dno)`.

Un `d` negativo nel trend significa quindi "long col pattern ribassista" (setup contrarian).

### 7.4 Pattern fast (`pattern_fast`, 1-151; 152 = sempre vero, 153 = sempre falso)

**1-30 = pattern neutrali 1-30** (§7.2). Poi, `range1 = H1−L1`:

| n | condizione | n | condizione | n | condizione |
|---|---|---|---|---|---|
| 31 | (H0−O0) > 0,25·(H1−O1) | 71 | C1 < 0,995·C2 | 111 | L1>L2, L1>L3, L1>L4 |
| 32 | (H0−O0) > 0,50·(H1−O1) | 72 | C1 < 0,990·C2 | 112 | C1>C2, C2>C3, O0>C1 |
| 33 | (H0−O0) > 0,75·(H1−O1) | 73 | C1 < 0,985·C2 | 113 | C1<C2, C2<C3, O0<C1 |
| 34 | (H0−O0) > 1,00·(H1−O1) | 74 | C1 < 0,980·C2 | 114 | (H1−C1) < 0,20·range1 |
| 35 | (H0−O0) > 1,50·(H1−O1) | 75 | C1 < 0,975·C2 | 115 | (C1−L1) < 0,20·range1 |
| 36 | (H0−O0) > 2,00·(H1−O1) | 76 | C1 < 0,970·C2 | 116 | O0 < L1 oppure O0 > H1 |
| 37 | (H0−O0) > 2,50·(H1−O1) | 77 | C1 > 1,005·C2 | 117 | O0 < L1 |
| 38 | (H0−O0) > 3,00·(H1−O1) | 78 | C1 > 1,010·C2 | 118 | O0 > H1 |
| 39 | (H0−O0) < (H1−O1) | 79 | C1 > 1,015·C2 | 119 | O0 < 0,9975·C1 |
| 40 | (O0−L0) < (O1−L1) | 80 | C1 > 1,020·C2 | 120 | O0 < 0,995·C1 |
| 41 | (O0−L0) > 0,50·(O1−L1) | 81 | H0 > H1 | 121 | O0 < 0,9925·C1 |
| 42 | (O0−L0) > 1,00·(O1−L1) | 82 | H0 > 1,0025·H1 | 122 | O0 < 0,990·C1 |
| 43 | (O0−L0) > 1,50·(O1−L1) | 83 | H0 > 1,005·H1 | 123 | O0 > 1,0025·C1 |
| 44 | (O0−L0) > 2,00·(O1−L1) | 84 | H0 > 1,0075·H1 | 124 | O0 > 1,005·C1 |
| 45 | (O0−L0) > 2,50·(O1−L1) | 85 | H0 > 1,010·H1 | 125 | O0 > 1,0075·C1 |
| 46 | (O0−L0) > 3,00·(O1−L1) | 86 | H0 > 1,015·H1 | 126 | O0 > 1,010·C1 |
| 47 | C1>C2, C2>C3, C3>C4 | 87 | H0 < H1 | 127 | H0 < H1 e L0 > L1 |
| 48 | C1<C2, C2<C3, C3<C4 | 88 | H0 < 0,995·H1 | 128 | range1 < ((H2−L2)+(H3−L3))/3 |
| 49 | C1>C2>C3>C4>C5 | 89 | H0 < 0,990·H1 | 129 | range1 < (H2−L2) e (H2−L2) < (H3−L3) |
| 50 | C1<C2<C3<C4<C5 | 90 | H0 < 0,985·H1 | 130 | H2 > H1 e L2 < L1 |
| 51 | H1>H2 e L1>L2 | 91 | H0 < 0,980·H1 | 131 | H1 < H2 |
| 52 | H1<H2 e L1<L2 | 92 | H0 < 0,975·H1 | 132 | L1 > L2 |
| 53 | H0 > 1,005·L0 | 93 | H1 > H5 | 133 | H1 < H2 oppure L1 > L2 |
| 54 | H0 > 1,0075·L0 | 94 | H1 < H5 | 134 | H2 < H1 e L2 > L1 |
| 55 | H0 > 1,010·L0 | 95 | L0 < L1 | 135 | H0 > H1 e L0 < L1 |
| 56 | H0 > 1,015·L0 | 96 | L0 < 0,9975·L1 | 136 | C1>O1 e C2>O2 |
| 57 | H0 > 1,020·L0 | 97 | L0 < 0,995·L1 | 137 | C1<O1 e C2>O2 |
| 58 | H0 > 1,025·L0 | 98 | L0 < 0,9925·L1 | 138 | C1>O1 e C2<O2 |
| 59 | H0 > 1,030·L0 | 99 | L0 < 0,990·L1 | 139 | C1<O1 e C2<O2 |
| 60 | H0 < 1,005·L0 | 100 | L0 > L1 | 140 | (H1−L1) < (H2−L2) |
| 61 | H0 < 1,0075·L0 | 101 | L0 > 1,005·L1 | 141 | (H1−L1) > (H2−L2) |
| 62 | H0 < 1,010·L0 | 102 | L0 > 1,010·L1 | 142 | close > 0,99·O0 |
| 63 | H0 < 1,015·L0 | 103 | L0 > 1,015·L1 | 143 | close > 0,995·O0 |
| 64 | H0 < 1,020·L0 | 104 | L0 > 1,020·L1 | 144 | close > O0 |
| 65 | H0 < 1,025·L0 | 105 | L0 > 1,025·L1 | 145 | close > 1,005·O0 |
| 66 | H0 < 1,030·L0 | 106 | L1 < L5 | 146 | close > 1,010·O0 |
| 67 | C1 > C2 | 107 | L1 > L5 | 147 | close < 1,010·O0 |
| 68 | C1 < C2 | 108 | H1>H2, H1>H3, H1>H4 | 148 | close < 1,005·O0 |
| 69 | C1 < O1 | 109 | H1<H2, H1<H3, H1<H4 | 149 | close < O0 |
| 70 | C1 > O1 | 110 | L1<L2, L1<L3, L1<L4 | 150 | close < 0,995·O0 |
| | | | | 151 | close < 0,990·O0 |

**Uso "unmirrored":** long e short hanno pattern indipendenti:
`long = fast(ly_yes) AND NOT fast(ly_no)`, `short = fast(sy_yes) AND NOT fast(sy_no)`.
Un YES = 153 **spegne il lato** (strategia solo long o solo short).

### 7.5 Pattern UAPtnBase (`pattern_uaptnbase`, 1-40; 41 = sempre vero, 42 = sempre falso)

Usata dai Level Fader, per lato come la fast. `range1 = H1−L1`, `Hm = max(H1..H5)`, `Lm = min(L1..L5)`.

| n | condizione | n | condizione |
|---|---|---|---|
| 1 | \|O1−C1\| < 0,5·(H1−L1) | 21 | H1 > H5 |
| 2 | \|O1−C5\| < 0,5·(H5−C1) | 22 | L0 < L1 |
| 3 | \|O5−C1\| < 0,5·(Hm−Lm) | 23 | L1 < L5 |
| 4 | (H0−O0) > 1,0·(H1−O1) | 24 | H1>H2, H1>H3, H1>H4 |
| 5 | (H0−O0) > 1,5·(H1−O1) | 25 | H1<H2, H1<H3, H1<H4 |
| 6 | (O0−L0) > 1,0·(O1−L1) | 26 | L1<L2, L1<L3, L1<L4 |
| 7 | (O0−L0) > 1,5·(O1−L1) | 27 | L1>L2, L1>L3, L1>L4 |
| 8 | C1>C2, C2>C3, C3>C4 | 28 | C1>C2, C2>C3, O0>C1 |
| 9 | C1<C2, C2<C3, C3<C4 | 29 | C1<C2, C2<C3, O0<C1 |
| 10 | H1>H2 e L1>L2 | 30 | (H1−C1) < 0,20·range1 |
| 11 | H1<H2 e L1<L2 | 31 | (C1−L1) < 0,20·range1 |
| 12 | H0 > 1,0075·L0 | 32 | O0 < L1 oppure O0 > H1 |
| 13 | H0 < 1,0075·L0 | 33 | O0 < 0,995·C1 |
| 14 | C1 > C2 | 34 | O0 > 1,005·C1 |
| 15 | C1 < C2 | 35 | H0 < H1 e L0 > L1 |
| 16 | C1 < O1 | 36 | range1 < ((H2−L2)+(H3−L3))/3 |
| 17 | C1 > O1 | 37 | range1 < (H2−L2) e (H2−L2) < (H3−L3) |
| 18 | C1 < 0,995·C2 | 38 | H2 > H1 e L2 < L1 |
| 19 | C1 > 1,005·C2 | 39 | H1 < H2 oppure L1 > L2 |
| 20 | H0 > H1 | 40 | H2 < H1 e L2 > L1 |

### 7.6 Libreria PtnSpecchio

Esiste nel codice (`pattern_specchio`, ±1..±61) ma **nessun motore la usa**: non serve a ricostruire
il sistema.

---

## 8. I 16 motori

### 8.1 Principio: un motore = un modo di entrare

Un parametro che cambia **dove o come** si entra (quale livello, che tipo di ordine) è un motore a sé;
quello che cambia **quanto** (lunghezze, tick, sessioni) resta dentro. BO_S deriva da BO, LF_HL da LF,
BIAS_BO e BIAS_RT da BIAS (stesso codice, spazio diverso).

### 8.2 Tabella riassuntiva

| motore | idea | ordine | famiglia v4 | pattern | fine sessione (intraday) | daily |
|---|---|---|---|---|---|---|
| TF_M | rottura del massimo/minimo di ieri | stop | TF | neutrale + direzionale | parametro `intraday_only` | sì |
| TF_U | come TF_M | stop | TF | fast per lato | `intraday_only` | sì |
| BO | rottura del canale di N sessioni | stop | TF | neutrale + direzionale | `intraday_only` | sì |
| BO_S | rottura del massimo/minimo della sessione in corso | stop | TF | neutrale + direzionale | `intraday_only` | **no** |
| PC | rottura del canale delle ultime N barre (Donchian) | stop | TF | neutrale + direzionale | `intraday_only` | sì |
| VBO | apertura di sessione ± k × volatilità | stop | TF | neutrale + direzionale | sempre | sì |
| MAC | incrocio di due medie mobili | market | TF | nessuno | **mai** (overnight) | sì |
| RBB_M | ritorno sulle bande di Bollinger | limit | CT | neutrale + direzionale inverso | `intraday_only` | sì |
| RBB_U | come RBB_M | limit | CT | fast per lato | `intraday_only` | sì |
| RHL | limit sotto il minimo / sopra il massimo di ieri | limit | CT | neutrale + direzionale inverso | sempre | sì |
| LF | rientro sul pivot S1/R1 | market | CT | UAPtnBase per lato + direzionale (+ neutrale) | sempre | sì |
| LF_HL | rientro sul massimo/minimo di ieri | market | CT | come LF | sempre | sì |
| BIAS | entrata a mercato alla barra N della sessione | market | BIAS | fast per lato | sempre | sì |
| BIAS_BO | dopo la barra N, stop sul massimo/minimo delle ultime barre | stop | BIAS | fast per lato | sempre | **no** |
| BIAS_RT | dopo la barra N, limit sul minimo/massimo delle ultime barre | limit | BIAS | fast per lato | sempre | **no** |
| BIASW | ciclo settimanale: giorno/ora d'entrata e d'uscita | market | BIAS | fast per lato | **mai** | sì |

"sempre" = il motore esce a fine sessione su ogni timeframe intraday; "`intraday_only`" = 1 esce a
fine sessione, 0 tiene la posizione (sul daily mai fine sessione).

Ordine di esecuzione nella cella: MAC, RHL, LF, LF_HL, PC, VBO, BO, BO_S, TF_M, RBB_M, RBB_U, TF_U,
BIAS, BIAS_BO, BIAS_RT, BIASW.

Griglie comuni: `start_hour`, `end_hour` ∈ {−1, 0..23}; `skip_day` ∈ {−1, 4}; stop, target,
trailing, breakeven da §6.3. `max_bars` "multiday" = {0, 12, 24, 48, 2·bpd, 4·bpd, 7·bpd, 10·bpd} con
`bpd = floor(1380 / passo_minuti)` (valori doppi tolti). "Neutrale" = {55, 1..54} per YES, {56, 1..54}
per NO; "direzionale" = {52, 1..51, −1..−51} per YES, {53, ±1..±51} per NO; "fast" = {152, 1..151, 153}
per YES, {153, 1..151} per NO; "UAPtnBase" = {41, 1..40, 42} per YES, {42, 1..40} per NO. Il primo
valore di ogni lista è quello spento.

**Emissione continua:** i motori stop e limit ripetono l'ordine a ogni barra in cui le condizioni sono
vere (come un "next bar" ri-emesso in EasyLanguage); con `single_entry_per_session` si entra al
massimo una volta per sessione per direzione.

### 8.3 TF_M — Trend Following mirrored

- **Livelli:** long stop a `H_d1`, short stop a `L_d1`.
- **Condizione:** `finestra AND giorno AND neutrale(yes) AND NOT neutrale(no) AND direzionale_long/short` (§7.3, uso trend).
- **Parametri intraday:** `intraday_only` {1, 0}, `ptn_neut_yes`, `ptn_neut_no`, `ptn_dir_yes`, `ptn_dir_no`, `start_hour`, `end_hour`, `skip_day`, `stop_loss`, `take_profit`, `max_bars` multiday.
- **Daily:** niente `intraday_only` né finestra; `max_bars` {0, 5, 10, 20}.
- 1 ingresso per sessione per direzione. Fine sessione se `intraday_only = 1`.
- **Percorso v4:** famiglia TF, trigger fisso (primo passo = valutazione della base), pattern mirrored, calendario `skip_day`.

### 8.4 TF_U — Trend Following unmirrored

Come TF_M con pattern fast per lato: `long = fast(ly_yes) AND NOT fast(ly_no)`, `short = fast(sy_yes) AND NOT fast(sy_no)`.
Parametri: `intraday_only`, `ptn_ly_yes`, `ptn_ly_no`, `ptn_sy_yes`, `ptn_sy_no`, finestra, `skip_day`, stop, target, `max_bars`.
Percorso: TF, trigger fisso, pattern unmirrored, calendario `skip_day`.

### 8.5 BO — Breakout di N sessioni

- **Livelli:** `long = max(H_d1..H_dN) + off × tick`, `short = min(L_d1..L_dN) − off × tick` (N limitato a 5).
  Con `lev_include_sess0 = 1` il livello è il massimo (minimo) fra il canale e il **massimo (minimo)
  della sessione in corso fino alla barra precedente** (esclusa la barra corrente).
- **Parametri intraday:** `level_source` {0}, `n_sess` {2, 1, 3, 4, 5}, `lev_include_sess0` {0, 1},
  `breakout_offset_ticks` {0, 2, 5, 10}, `intraday_only` {1, 0}, pattern mirrored, finestra, `skip_day`, stop, target, `max_bars` multiday.
- **Daily:** `n_sess` {2,1,3,4,5}, `lev_include_sess0` {0}, offset {0, 5, 10, 20}, `intraday_only` {0}, `max_bars` {0,5,10,20}.
- Condizione: finestra, giorno, neutrale, livelli validi, direzionale. 1 ingresso per sessione.
- **Percorso v4:** TF, trigger = `n_sess`, `lev_include_sess0`, `breakout_offset_ticks` (quelli con più di un valore), pattern mirrored, calendario `skip_day`.

### 8.6 BO_S — Breakout della sessione in corso

- **Livelli:** long stop sul **massimo della sessione in corso inclusa la barra corrente** (cumulato),
  short sul minimo, ± offset. L'ordine emesso alla barra i lavora nella barra i+1, quindi il massimo
  fino a i è noto.
- Spazio di BO con `level_source` {1}, `n_sess` {1}, `lev_include_sess0` {0}. Non esiste sul daily.
- Percorso: TF, trigger = `breakout_offset_ticks`, pattern mirrored, calendario `skip_day`.

### 8.7 PC — Price Channel (Donchian)

- **Livelli:** `long = max(high delle ultime N barre, barra corrente inclusa) + off × tick`,
  `short = min(low delle ultime N barre, inclusa) − off × tick`. Il canale si aggiorna a ogni barra.
- **Filtro di volatilità** `dvol_min`: vero se `ATR giornaliero(14, spostato di una sessione) × bpv ≥ dvol_min` (0 = spento).
- **Direzione** `direction`: 0 entrambe, 1 solo long, 2 solo short.
- **Parametri intraday:** `channel_len` {20, 1, 10, 15, 30, 40, 50, 75, 100, 155}, `breakout_offset_ticks` {0, 2, 5, 10}, `intraday_only` {1, 0}, `direction` {0, 1, 2}, `dvol_min` {0, 3000, 6000}, pattern mirrored, finestra, `skip_day`, stop, target, `max_bars` multiday, `trailing_stop`, `breakeven`.
- **Daily:** `channel_len` {20, 1, 10, 15, 30, 40, 55}, offset {0, 5, 10, 20}, `dvol_min` {0}, `max_bars` {0,5,10,20}.
- Percorso: TF, trigger = `channel_len`, `breakout_offset_ticks`, `direction`; filtro `dvol_min`; calendario `skip_day`.

### 8.8 VBO — Volatility Breakout

- **Livelli:** `long = O_d0 + k × VOL`, `short = O_d0 − k_short × VOL`, con `k_short = k` se `vol_mult_short = −1`.
  - `vol_source = 1`: `VOL = H_d1 − L_d1`;
  - `vol_source = 2`: ATR giornaliero di lunghezza `atr_len`, spostato di una sessione;
  - `vol_source = 3`: ATR di barra di lunghezza `atr_len`, spostato di una barra.
- **Momentum:** 0 spento; 1 long se `C_d1 > C_d2` (short `<`); 2 long se `O_d0 > C_d1` (short `<`).
- **Parametri intraday:** `vol_source` {1, 2, 3}, `vol_mult` {0,5; 0,3; 0,7; 1; 1,5; 2; 3; 4; 6; 8,5; 9,5}, `vol_mult_short` {−1; 0,3; 0,5; 0,7; 1; 1,5; 2; 3; 4; 6; 8,5; 9,5}, `atr_len` {14, 5, 10, 20, 100, 200, 500}, `momentum` {0, 1, 2}, `direction` {0, 1, 2}, pattern mirrored, finestra, `skip_day`, stop, target, `max_bars` {0, 12, 24, 48}, trailing, breakeven.
- **Daily:** `vol_mult` {0,5; 0,3; 0,7; 1}, `vol_mult_short` {−1; 0,3; 0,5; 0,7; 1}, `atr_len` {14, 5, 10}, `max_bars` {0,5,10,20}.
- Esce sempre a fine sessione sugli intraday. 1 ingresso per sessione.
- Percorso: TF, trigger = `vol_source`, `vol_mult`, `vol_mult_short`, `atr_len`, `direction`; filtro `momentum`; calendario `skip_day`.

### 8.9 MAC — Incrocio di medie mobili

- `fast_sma`, `slow_sma` = medie semplici del close (lunghezze `fast` < `slow`; se `fast ≥ slow` nessun segnale).
- `gap = fast_sma − slow_sma`, `eps = tick × 10⁻⁶`. Incrocio sopra: `gap > eps` e `gap_prec ≤ eps`; sotto: `gap < −eps` e `gap_prec ≥ −eps`.
- **Gradiente:** `|fast_sma − fast_sma[g barre fa]| ≥ gfac × |slow_sma − slow_sma[g]| − tolleranza`
  (tolleranza = `10⁻⁹ × max(dei due) + eps`); `gfac = 0` spegne il filtro.
- **Setup di ieri:** `|C_d1 − O_d1| / (H_d1 − L_d1) ≤ daily_factor`; long se anche `C_d1 > O_d1`, short se `C_d1 < O_d1`.
- **Entrata:** market all'open della **barra dopo** l'incrocio (segnale spostato di una barra).
- **Uscita:** incrocio inverso, spostato di una barra, al close; sugli intraday anche **all'ultima barra
  della sessione del venerdì** (giorno dell'istante-regola). Mai uscita di fine sessione normale.
- **Parametri:** `fast` {12, 5, 8, 16, 20}, `slow` {24, 20, 30, 40, 50}, `gradient_length` {2, 1, 3, 5}, `gradient_factor` {1,6; 0; 0,8; 1,2; 2}, `daily_factor` {0,5; 0,3; 0,7; 1}, `direction` {0, 1, 2}, `stop_loss`, `take_profit`, `trailing_stop` (nel percorso v4 il trailing resta 0).
- Percorso: TF, trigger = `fast`, `slow`, `gradient_length`, `gradient_factor`, `daily_factor`, `direction`; niente pattern, finestra o calendario.

### 8.10 RBB_M — Reversal sulle bande di Bollinger, mirrored

- Bande: `media = SMA(close, N)`, `dev = deviazione standard (popolazione, ddof 0) su N`,
  `bb_up = media + devs × dev`, `bb_dn = media − devs × dev`.
- **Armato** se la banda è larga almeno un tick (`bb_up − bb_dn ≥ tick`) e: long `close > bb_dn`, short `close < bb_up`.
- **Ordine:** long **limit** a `bb_dn`, short limit a `bb_up` (fill con penetrazione stretta, §4.3).
- Pattern: neutrale + direzionale con segno **invertito** (§7.3).
- **Parametri intraday:** `bb_length` {10, 14, 20, 30, 50}, `bb_num_devs` {1,5; 2; 2,5; 3}, `intraday_only` {1, 0}, pattern mirrored, finestra, `skip_day`, stop, target, `max_bars` multiday. Daily: niente `intraday_only`, `max_bars` {0,5,10,20}.
- Percorso: CT, trigger = `bb_length`, `bb_num_devs`, calendario `skip_day`, durata.

### 8.11 RBB_U — Reversal Bollinger unmirrored

Come RBB_M con pattern fast per lato. `bb_length` {20, 10, 14, 30, 50}, `bb_num_devs` {2; 1,5; 2,5; 3}.
Percorso: CT, trigger = `bb_length`, `bb_num_devs`, pattern unmirrored, calendario `skip_day`, durata.

### 8.12 RHL — Reversal sul massimo/minimo di ieri

- Long **limit** a `L_d1 − long_offset × tick`, short limit a `H_d1 + short_offset × tick`.
- Pattern neutrale + direzionale **invertito**; `direction` {0, 1, 2}.
- **Parametri intraday:** `long_offset_ticks`, `short_offset_ticks` {0, 5, 10, 20, 40, 80}, `direction`, pattern mirrored, finestra, `skip_day`, stop, target, `max_bars` {0, 12, 24, 48}. Daily: `max_bars` {0,3,5,10,20}.
- Esce sempre a fine sessione sugli intraday. 1 ingresso per sessione.
- Percorso: CT, trigger = i due offset e `direction`, calendario `skip_day`, durata.

### 8.13 LF — Level Fader sul pivot

- Livelli dalla sessione di ieri: `pivot = (H1+L1+C1)/3`, `R1 = 2·pivot − L1`, `S1 = 2·pivot − H1`;
  `LE = S1 − shift × tick`, `SE = R1 + shift × tick`.
- **Segnale alla chiusura della barra i:** long se `close[i−1] < LE` e `close[i] > LE`; short se
  `close[i−1] > SE` e `close[i] < SE`.
- Filtri alla barra del segnale: `neutrale(yes) AND NOT neutrale(no)`; direzionale **solo YES**
  (long `dir(+d)`, short `dir(−d)`); `UAPtnBase(ly_yes) AND NOT UAPtnBase(ly_no)` per il long,
  `sy` per lo short; finestra; `not_le_day` per il long, `not_se_day` per lo short.
- **Entrata market all'open della barra i+1.** Esce sempre a fine sessione sugli intraday.
- **Parametri intraday:** `level_choice` {1}, `level_shift` {0, 2, 5, 10}, `ptn_neut_yes`, `ptn_neut_no`, `ptn_dir_yes`, `ptn_ly_yes`, `ptn_ly_no`, `ptn_sy_yes`, `ptn_sy_no`, finestra, `not_le_day`, `not_se_day` {−1, 0..4}, stop, target, `max_bars` {0, 12, 24, 48}. Daily: `level_shift` {0, 5, 10, 20}, `max_bars` {0,5,10,20}.
- **Percorso v4:** CT, trigger = `level_shift`, pattern "lf" = UAPtnBase per lato come YES/NO più il direzionale (il neutrale resta spento); calendario per lato; durata.

### 8.14 LF_HL — Level Fader sui livelli di ieri

Come LF con `level_choice = 2`: `LE = L_d1 − shift × tick`, `SE = H_d1 + shift × tick`.

### 8.15 BIAS — entrata a mercato a una barra della sessione

- `bars_sess = max(8, round(23 × 60 / passo))` (sessione da ~23 ore).
- Griglie in barre (0 = prima barra della sessione), ciascuna `= min(bars_sess−1, max(minimo, round(f × bars_sess)))`, valori unici ordinati:
  - entrata `le_bar`, `se_bar`: f ∈ {0,05; 0,10; 0,20; 0,30; 0,45; 0,60; 0,75; 0,90; 0,97}, minimo 1;
  - uscita `lx_bar`, `sx_bar`: f ∈ {0,15; 0,30; 0,45; 0,60; 0,75; 0,90; 0,99}, minimo 2;
  - fine finestra `end_long`, `end_short` (solo BIAS_BO/RT): f ∈ {0,30; 0,50; 0,70; 0,90; 0,99}, minimo 2.
- **Intraday (entry_type 1):** long market **all'open della barra con `bar_num = le_bar`** se il pattern
  long era vero alla chiusura della barra precedente e il giorno non è `not_le_day`; short con
  `se_bar` e `not_se_day`. **Uscita forzata al close della barra `lx_bar`** (short `sx_bar`); comunque
  fine sessione.
- Pattern fast per lato. **Parametri:** `entry_type` {1}, `le_bar`, `lx_bar`, `se_bar`, `sx_bar`, `ptn_ly_yes`, `ptn_ly_no`, `ptn_sy_yes`, `ptn_sy_no`, `not_le_day`, `not_se_day` {−1, 0..4}, `stop_loss`, `take_profit`.
- **Daily:** long market all'open se il pattern di ieri (barra precedente) era vero e il giorno è ammesso; primo ingresso della sessione; uscita con `max_bars` {1, 3, 5, 10, 20}.
- **Percorso v4:** famiglia BIAS. Intraday: trigger per lato `le_bar`, `lx_bar` (long, con lo short spento) poi `se_bar`, `sx_bar` (short, col long spento); lato spento = `ptn_ly_yes` / `ptn_sy_yes` = 153. Calendario `not_le_day` (L), `not_se_day` (S); affinamento orari su `le_bar`, `lx_bar`, `se_bar`, `sx_bar`. Daily: trigger `max_bars`, durata.

### 8.16 BIAS_BO — BIAS con rottura

- **Armamento:** alla barra `bar_num = le_bar` il pattern long **di quella barra** e il giorno sono veri;
  resta armato per il resto della sessione.
- **Finestra in barre** `[le_bar, end_long)` (se `le_bar > end_long`: `bar_num ≥ le_bar` oppure `bar_num < end_long`).
- **Ordine stop:** long al **massimo delle `nhigh` barre precedenti** (esclusa la corrente), short al
  **minimo delle `nlow` barre precedenti**. Emesso a ogni barra armata nella finestra, lavora nella
  barra successiva. 1 ingresso per sessione.
- Uscita forzata al close di `lx_bar` / `sx_bar`; fine sessione.
- Parametri come BIAS con `entry_type` {2}, `end_long`, `end_short` dalla griglia, `nhigh` {3, 1, 2, 5}, `nlow` {1, 2, 3, 5}. Solo intraday.
- Percorso: BIAS; trigger long `le_bar`, `lx_bar`, `end_long`, `nhigh`; short `se_bar`, `sx_bar`, `end_short`, `nlow`.

### 8.17 BIAS_RT — BIAS con ritracciamento

Come BIAS_BO con ordine **limit**: long al **minimo delle `nlow` barre precedenti**, short al
**massimo delle `nhigh` barre precedenti**. `entry_type` {3}. Solo intraday.

### 8.18 BIASW — ciclo settimanale

- **Entrata market all'open della barra che COMINCIA** (sull'orologio del simbolo) il giorno `le_day`
  all'ora `le_time` (HHMM), se il pattern long era vero alla chiusura della barra precedente.
  Short con `se_day`, `se_time`. Giorno −1 = lato spento.
- **Uscita forzata al close della barra la cui CHIUSURA** cade il giorno `lx_day` all'ora `lx_time`
  (short `sx_day`, `sx_time`). Se quella barra non esiste (festivo) la posizione resta aperta fino
  alla settimana dopo.
- **Mai uscita di fine sessione** (tiene la notte). Pattern fast per lato.
- **Parametri:** `le_day`, `se_day` {−1, 0..4}; `lx_day`, `sx_day` {0..4}; `le_time`, `lx_time`, `se_time`, `sx_time` {0, 100, …, 2300}; pattern fast per lato; `stop_loss`, `take_profit`; `entrata_inizio` {1} (dichiarazione: entra all'inizio della barra).
- **Percorso v4:** BIAS; base con gli orari all'ora d'inizio sessione × 100 e i due lati spenti; trigger long `le_day`, `le_time`, `lx_day`, `lx_time` poi short; lato spento = giorno −1; affinamento orari; nessun calendario.

---

## 9. Il percorso di ricerca v4 (un motore)

Il percorso sceglie i parametri **un passo alla volta**, come negli sviluppi guidati di Unger (TSS2
123-130; DAX, miniSP, Crude Oil, Live Cattle). Ogni passo muove **un parametro**, tutto il resto
fermo. Ogni simulazione è al minuto (§4.4).

### 9.1 Costanti

| nome | valore | significato |
|---|---|---|
| `phase_np_keep_frac` | **0,75** | un valore è eleggibile se il suo net profit è almeno il 75% del migliore del passo |
| `QUOTA_NP_PATTERN` | **2/3** | per i pattern la soglia è 2/3 |
| `N_TENTATIVI_FILTRO` | **3** | un filtro prova i primi 3 candidati sopra lo spento |
| `QUOTA_STOP_D2` | **0,95** | lo stop è il più piccolo col net profit ≥ 95% del migliore |
| `N_CANDIDATI_NO` | **10** | i candidati NO sono i 10 peggiori della classifica YES |
| `DURATA_BASE_BARRE` | **4** | durata di base dei reversal |
| `GRADINI_AFFINAMENTO` | **2** | l'affinamento prova 2 gradini sopra e sotto |
| `GRIGLIA_GIORNI` | {−1, 0, 1, 2, 3, 4} | calendario: un giorno escluso alla volta |
| `QUOTA_TRADE_LATO` | **0,5** | un lato acceso deve avere almeno 0,5 × `min_trades` trade |
| `min_trades` | **50** | trade minimi per l'eleggibilità (dai filtri minimi) |
| `conferma_terzo` | **1/3** | fetta di conferma |
| `rami_trigger` | **1** | una sola partenza per motore |
| test dei pattern casuali | p ≤ **0,20**, estrazioni con ≥ max(10, ⌈0,5 × trade⌉) trade | §9.10 |

### 9.2 Strategia di base

Dai valori di default del motore (primo di ogni lista), poi:
- target 0, trailing 0, breakeven 0 (trailing e breakeven restano spenti per tutto il percorso);
- stop: **0 nella famiglia BIAS**, altrimenti il primo valore della griglia di stop (0,80 × ATR giornaliero, o il primo sopra il pavimento);
- `max_bars`: 0 nella famiglia TF, **4 nella famiglia CT**, invariato nella BIAS;
- ritocchi del motore (BIASW: orari = ora d'inizio sessione).

Tutte le ore, pattern spenti, filtri spenti.

### 9.3 Ordine dei passi per famiglia

| famiglia | passi |
|---|---|
| **TF** | trigger → finestra (inizio, fine) → stop → intraday/overnight → YES → NO → direzionale → **[R9]** → filtri → calendario → target → affinamento stop |
| **CT** | trigger → finestra → stop → target → YES → NO → direzionale → **[R9]** → filtri → calendario → chiusura intraday → affinamento stop → affinamento target → durata |
| **BIAS** | trigger (entrata e uscita, per lato) → stop → target → YES → NO → **[R9]** → calendario → affinamento stop → affinamento target → affinamento orari → durata |

Un passo che il motore non ha (parametro assente o con un solo valore, finestra sul daily) si salta
senza cambiare l'ordine degli altri. **R9** si esegue dopo l'ultimo passo prima del primo "filtri" o
"calendario"; se il motore non ne ha, alla fine del percorso.

Passi per tipo di pattern:
- mirrored: YES = `ptn_neut_yes`, NO = `ptn_neut_no`, direzionale = `ptn_dir_yes` poi `ptn_dir_no`;
- unmirrored: YES = `ptn_ly_yes` (lato L) e `ptn_sy_yes` (lato S), NO = `ptn_ly_no`, `ptn_sy_no`;
- lf (Level Fader): come unmirrored (UAPtnBase), più direzionale `ptn_dir_yes`.

### 9.4 Fetta di scelta e fetta di conferma

La ricerca usa **tutta la storia** della cella. Dentro il percorso:
- **fetta di scelta** = primi 2/3 delle barre; tutti i passi scelgono qui;
- **fetta di conferma** = ultimo terzo delle barre;
- **conferma:** dopo ogni passo che cambia i parametri (tutti tranne il trigger), si simula prima e
  dopo sulla fetta di conferma: se il net profit **dopo < prima**, il passo si annulla e il parametro
  torna com'era;
- R9, controlli finali e metriche della strategia completa usano **tutta la storia**.

### 9.5 La classifica del passo (`_ordina`) e lo smussamento

Dato un insieme di configurazioni valutate:
1. **eleggibili** = net profit > 0 e trade ≥ `min_trades`;
2. il **pool** = eleggibili con net profit ≥ quota × il net profit migliore (quota 0,75, o 2/3 per i pattern);
3. ogni valore si ordina per **avg trade smussato**: se il parametro è ordinale, la media del suo avg
   trade e di quello dei due vicini **in ordine di valore** (il valore spento non è vicino di nessuno;
   ore circolari: le 23 confinano con le 0; sotto 3 valori niente smussamento);
4. a parità, la finestra più stretta; poi l'ordine della griglia.

**Criterio del passo** = il primo della classifica; nessuno eleggibile → il passo non cambia niente.

### 9.6 Trigger

- Motori con trigger fisso (TF_M, TF_U): una sola valutazione della base.
- Per ogni parametro del trigger (per lato nella famiglia BIAS, **con l'altro lato spento**), in
  ordine: si provano tutti i valori della griglia; pool = net > 0, trade ≥ 50, net ≥ 75% del migliore;
  vince l'avg trade (smussato se il parametro è ordinale con più di 2 valori).
- **Se nessun valore è in utile:** si sceglie il net profit massimo (smussato) **solo fra i valori che
  fanno almeno 50 trade**, o almeno un trade se nessuno arriva a 50. Il giudizio lo dà R9 più avanti.
- Due valori con metriche identiche (stessa firma: UngerFit, net, trade, DD, avg) non si tengono entrambi.

### 9.7 Finestra oraria

- Ore candidate: 0..23 su 15m, 30m, 1h; su 4h solo le ore in cui almeno l'1% delle barre ha
  l'istante-regola esattamente all'ora piena; nessuna sul daily.
- **Inizio:** valori {−1} ∪ ore. Nella famiglia TF la fine resta aperta (−1); nella famiglia CT la fine
  è `inizio + max(1, round(4 × passo/60))` ore (modulo 24).
- **Fine:** valori {−1} ∪ ore, con l'inizio scelto.
- Criterio del passo, ordinale circolare, spento = −1, a parità la più stretta.
- Nessuna finestra in utile → la finestra col net profit smussato massimo fra quelle con ≥ 50 trade.

### 9.8 Stop (regola D2, al minuto)

Tutti i valori della griglia di stop, ordinati. Fra quelli con ≥ 50 trade si smussa il **net profit
al minuto** coi vicini; se il migliore è > 0 si sceglie **il più piccolo stop con net smussato ≥ 95%
del migliore**, altrimenti quello col net smussato massimo. Lo stop non si spegne mai.

### 9.9 Intraday / overnight (e chiusura intraday dei CT)

Valori `intraday_only` {1, 0}, criterio del passo non ordinale. Nella famiglia TF: il valore 0
(overnight) porta `max_bars = floor(1380/passo)` se non c'era una durata (una sessione di tenuta);
tornando a 1 la durata torna 0.

### 9.10 Filtro: la regola di accettazione (YES, NO, direzionale, target, calendario, filtri)

1. Classifica del passo (§9.5) sulle configurazioni provate.
2. Candidati = i valori **sopra al valore spento** nella classifica, esclusi spento e "sempre falso",
   primi 3.
3. Si prende il **primo candidato che**:
   - ha net profit > 0 e ≥ `net_spento − (1 − quota) × |net_spento|` (quota 0,75; 2/3 per i pattern);
   - **migliora il rapporto net profit / max drawdown** rispetto allo spento (DD = 0 e net > 0 = infinito);
   - nel **direzionale**: anche il drawdown deve scendere;
   - per i **pattern**: supera il test dei pattern casuali (sotto).
4. Nessuno (o metriche dello spento non disponibili) → il filtro resta spento.

**Test dei pattern casuali dentro il passo.** Si confronta l'avg trade del candidato con quello degli
**altri pattern della stessa libreria** già valutati nel passo (esclusi spento e "sempre falso"), solo
quelli con almeno `max(10, ⌈0,5 × trade del candidato⌉)` trade:
```
p = (1 + numero di pattern con avg trade ≥ candidato) / (1 + numero di pattern confrontati)
```
Si accetta se `p ≤ 0,20` (o se non c'è nessun pattern confrontabile).

**YES** (per i pattern per lato — unmirrored, BIAS, Level Fader — metriche del solo lato): si provano
tutti i valori della libreria. Dopo un passo YES per lato, se il lato non guadagna (net ≤ 0) si
**spegne** (YES = sempre falso: 153, o 42 per UAPtnBase); se è già spento anche l'altro, il ramo muore.

**NO:** candidati = i 10 pattern con **avg trade più basso** (e ≥ 50 trade) della classifica YES dello
stesso lato, escluso lo YES scelto. Il test dei pattern casuali usa l'intera libreria NO.

**Direzionale** (un solo direzionale): prima `ptn_dir_yes` (regola YES, con DD in discesa); `ptn_dir_no`
solo se lo YES è rimasto spento (regola NO sulla classifica del direzionale YES).

**Target:** valori della griglia ordinata con 0; metriche **al minuto**; ordinale, spento = 0; quota 0,75.

**Calendario:** per ogni parametro giorno (`skip_day`, oppure `not_le_day` sul lato L e `not_se_day`
sul lato S) si provano {−1, 0, 1, 2, 3, 4}; regola del filtro (non ordinale), spento = −1.

**Filtri del motore** (`dvol_min` di PC, `momentum` di VBO): griglia del motore, spento = primo valore.

### 9.11 Affinamenti e durata

- **Affinamento stop:** i due valori sopra e i due sotto lo stop scelto (griglia ordinata), regola D2.
- **Affinamento target:** i due sopra e i due sotto più lo 0, regola del filtro al minuto.
- **Affinamento orari** (BIAS, BIASW): due valori sopra e due sotto nella griglia del parametro,
  criterio del passo ordinale, metriche del lato.
- **Durata** (`max_bars`): griglia del motore (più 4 per i CT), criterio ordinale con spento 0, metriche al minuto.

### 9.12 R9: la base deve guadagnare

A quel punto del percorso, **su tutta la storia**:
- motori con lati separati (BIAS, BIASW): ogni lato acceso si simula da solo; se non guadagna si spegne;
- la strategia intera: net profit ≤ 0 o zero trade → **motore abbandonato** sulla cella.

### 9.13 Controlli finali (miniSP, TSS2 196-197)

A percorso finito, su tutta la storia:
- un lato acceso con meno di 25 trade (0,5 × 50) → scartata ("pochissimi trade");
- il **net profit dei trade usciti nell'ultimo terzo del tempo** (dal 2/3 della durata fra prima e ultima
  barra) ≤ 0 → scartata ("profitti nei primi anni").

### 9.14 Uscita del percorso

Una **strategia completa** per motore (o nessuna), con metriche su tutta la storia. Il file
`percorso_v4.csv` registra ogni decisione: passo, valore prima/dopo, accettato, motivo, prove, avg e
net prima/dopo, p del test, metro (barre o minuto).

Per i motori BIAS/BIASW sugli intraday si scrive anche `profilo_orario.csv` (guadagno di una posizione
di un'ora, ora per ora): informativo, non sceglie niente.

---

## 10. Dalla strategia completa al giudizio (una cella)

### 10.1 Filtri minimi (storia intera)

- trade ≥ **50**;
- avg trade ≥ `min_avg_trade` (§6.2);
- **≥ 5 trade in ogni anno** con trade;
- **media ≥ 12 trade all'anno**;
- anni in utile ≥ **50%**;
- (tetto al drawdown in dollari: spento).

Chi non passa non arriva oltre. Con una partenza per motore arriva al massimo una strategia per
motore; restano attivi anche il de-dup per metriche, la copertura strutturale e la quota diversità
(pensati per più candidate per motore).

### 10.2 Ultimo terzo (`overall_pass`)

La strategia si **ri-simula sulla sola fetta finale** (dalla barra `2/3 × n` alla fine: gli indicatori
ripartono dall'inizio della fetta). Colonne `oos_*`. Passa se:

```
oos_net_profit > 0    e    oos_net_profit / oos_max_dd ≥ 1,3
```

### 10.3 Doppioni

Sulle candidate di tutti i motori che passano l'ultimo terzo:
1. stessi parametri → una;
2. stesse metriche (motore, trade, net, DD, avg, e le stesse sull'ultimo terzo, arrotondate a 4 decimali) → una;
3. **stessa entrata, rischio diverso** (uguali in tutti i parametri tranne stop, target, trailing,
   breakeven, `max_bars`) → la migliore per UngerFit dell'ultimo terzo;
4. **stesse entrate**: sulle entrate dell'ultimo terzo, sovrapposizione ≥ **0,99** → la migliore.

**Sovrapposizione di entrate** fra A e B = quota di ingressi di A con un ingresso di B **della stessa
direzione** entro una tolleranza pari al passo della barra (la barra più lunga fra le due), divisa per
il numero di trade della strategia con meno trade.

### 10.4 Preselezione e ordine di merito

- **Multi-split** (§10.8) su fino a 2.000 candidate (per UngerFit dell'ultimo terzo, più 50 per
  diversità): serve a mettere prima le coerenti.
- **Walk-forward di stabilità** (non ri-ottimizza): fino a 500 candidate (se sono di più, un terzo dei
  posti va alla migliore di ogni famiglia strutturale non ancora presente) si simulano a parametri
  fissi su 5 fette consecutive uguali della storia; la media degli UngerFit delle fette
  (`wf_mean_unger`) dà l'ordine. **Non boccia niente.**

### 10.5 Plateau (TSS2 265)

Per la candidata e per ogni **vicino** si calcola l'UngerFit su tutta la storia:
- per ogni parametro **ordinale** con griglia di almeno 3 valori (i pattern, le direzioni, i giorni,
  `vol_source`, `momentum`, `level_choice`, `level_source`, `lev_include_sess0`, `intraday_only`,
  `entry_type`, `entrata_inizio` non sono ordinali), i due valori adiacenti **in ordine di valore**;
  se la candidata ha il parametro spento (`start_hour`/`end_hour` −1, `take_profit`, `trailing_stop`,
  `breakeven`, `max_bars`, `gradient_factor`, `dvol_min` 0, `vol_mult_short` −1) quel parametro non
  si guarda; il valore spento non è mai un vicino; fra due valori entrambi positivi non è vicino uno
  più lontano di **3×**; un valore negativo e uno non negativo non sono vicini;
- **finestra oraria**: se inizio e fine sono entrambi accesi, i vicini sono la finestra **intera spostata
  di un'ora** avanti e indietro (non inizio o fine da soli).

```
plateau_ratio = media( min(UngerFit_vicino / UngerFit_candidata, 1) )    (tetto 1 per vicino)
plateau_min   = min(UngerFit_vicino / UngerFit_candidata)                (colonna)
```

Senza vicini valutabili, o con UngerFit della candidata ≤ 0 → nessun giudizio (passa).
Un vicino che non si riesce a simulare conta 0.

### 10.6 Costi veri con lo swap

Sui trade della candidata su tutta la storia (§12):

```
costi_veri   = spread × bpv + comm% × nozionale + swap_medio_per_trade
soglia_costi = max( min_expected_avg_trade, costi_veri, cintura × costi_veri )     cintura: BTC 2, altri 1
```

Nozionale al prezzo di **oggi** (ultimo close della serie). Simbolo senza tariffa → nessun giudizio.

### 10.7 Colonne diagnostiche (non bocciano)

- **Verità al minuto:** con la strategia simulata apposta a barre, quota di trade con lo stop toccato
  dopo il fill dentro la barra d'ingresso (`dopo_il_fill`, soglia storica 0,35) e P&L risolto al minuto.
- **Due letture** (ottimista/pessimista a barre) e il loro divario.
- **Aspettativa:** `avg trade ultimo terzo × efficienza della walk-forward` (limitata a [0, 1];
  0,25 se non misurata).
- Monte Carlo sull'ordine dei trade (1.000 permutazioni): drawdown mediano, al 95°, peggiore, sulla
  storia e sull'ultimo terzo.

### 10.8 La batteria dei cancelli (definizione v4.0)

| cancello | colonna | regola |
|---|---|---|
| ultimo terzo | `overall_pass` | §10.2 |
| **2 terzi su 3** | `passes_multisplit` | la **storia intera** divisa in 3 fette uguali di barre (le fette sotto 100 barre non contano; storia sotto 200 barre: nessuna fetta); la candidata simulata su ognuna; almeno una fetta valutata e fette in utile ≥ `min(2, max(1, fette − 1))` |
| **plateau** | `passes_plateau` | `plateau_ratio ≥ 0,60` |
| **pattern casuali** | `passes_null` | sull'ultimo terzo: 200 estrazioni (seme 0) in cui **ogni** parametro pattern prende un valore a caso della sua griglia; contano solo le estrazioni con trade ≥ max(10, ⌈0,5 × trade della candidata⌉); `p = (1 + estrazioni con avg ≥ candidata) / (1 + estrazioni valide)`; boccia se l'**estremo inferiore di Wilson al 90%** (z = 1,2816) di p su n estrazioni valide è **> 0,20**. Senza pattern accesi o senza estrazioni valide: passa |
| **pattern utile** | `passes_pattern_useful` | sull'ultimo terzo: avg trade con i pattern > avg trade della stessa strategia con tutti i pattern spenti (senza pattern: passa) |
| **outlier** | `passes_outlier` | trade migliore / net profit ≤ **0,30** su tutta la storia **e** sull'ultimo terzo |
| **costi veri** | `passes_costi` | `avg_trade` (storia) ≥ `soglia_costi` |
| **walk-forward** | `passes_wfo`, `wfo_livello` | §11: "fuori" boccia |

Wilson inferiore: `(p + z²/2n)/(1 + z²/n) − (z/(1 + z²/n)) × sqrt(p(1−p)/n + z²/4n²)`.

**Esito:**
- `good` (**prima scelta**) = tutti i cancelli e walk-forward in **prima** fascia;
- `seconda_scelta` = tutti i cancelli e walk-forward in **seconda** fascia;
- walk-forward "fuori" o "non misurata" → né l'una né l'altra.

`gate_reason` scrive in chiaro perché una candidata non è buona.

### 10.9 Dopo i cancelli

1. Trade finali (al minuto) delle candidate; Monte Carlo; Z-score di dipendenza sui finalisti
   (`Z = (N(R − 0,5) − X) / sqrt(X(X − N)/(N − 1))`, R = numero di strisce, X = 2WL).
2. Ultimo de-dup sulle entrate (≥ 0,99, entrate dell'ultimo terzo).
3. **Famiglie di entrate:** prima + seconda scelta insieme, in ordine (prime davanti, poi avg trade
   atteso decrescente); una strategia entra nella famiglia del primo capofila con cui ha
   **sovrapposizione > 0,70** sulle entrate dell'ultimo terzo, altrimenti apre una famiglia nuova ed è
   **capofila**.
4. Finalisti decorrelati (fino a 3, sovrapposizione ≤ 0,70) e correlazioni dei P&L giornalieri: solo report.

### 10.10 File della cella

| file | contenuto |
|---|---|
| `strategie_buone.csv` | le prime scelte |
| `bacino_portafoglio.csv` | tutte le candidate arrivate ai cancelli, con esiti, `seconda_scelta`, famiglie |
| `top10_gates.csv` | le stesse con tutte le colonne dei cancelli |
| `candidate_robuste.csv` | sopravvissute all'ultimo terzo con multi-split |
| `percorso_v4.csv`, `ipotesi_v4.csv` | le decisioni del percorso e le prove per passo |
| `wfo_<MOTORE>.csv` | le finestre della walk-forward |
| `funnel.csv` | l'imbuto per motore |
| `multisplit_oos.csv`, `entry_overlap.csv`, `correlation_*.csv` | dettagli |
| `all_combinations.csv`, `partial_*.csv`, `sweep/*.parquet` | tutte le valutazioni |
| `top_final.json` | **metadata della run** (§15) e finalisti |
| `report.html`, `*.png`, `trades/` | report |

---

## 11. La walk-forward

Giudica la **procedura** di ricerca su quel motore e mercato, non una strategia: la ricerca si rifà da
zero su ogni finestra e si guarda cosa succede dopo.

### 11.1 Finestre

- `K = 5`, `n` = barre della cella, `finestra = floor(n / 5)`.
- Finestra k (0..4): `inizio = k × finestra`, `fine = (k+1) × finestra` (l'ultima arriva alla fine),
  `taglio = inizio + floor((fine − inizio) × 0,667)`.
- **Ancorate:** ricerca sulle barre `[0, taglio)`, prova su `[taglio, fine)`. Si salta se la ricerca ha
  meno di 200 barre o la prova meno di 100.
- Su 13,4 anni: ricerche da circa 1,8 / 4,5 / 7,2 / 9,8 / 12,5 anni, prove di circa 11 mesi l'una.

### 11.2 Ricerca in ogni finestra

- **Percorso v4 identico** (§9, con la sua conferma sull'ultimo terzo della finestra di ricerca).
- **Soglie al taglio:** ATR giornaliero (griglie di stop e target), ATR di barra (pavimento dello stop) e
  `min_avg_trade` calcolati **sui soli dati fino al taglio**; i costi CFD restano ai prezzi di oggi.
  Anche la scala dell'UngerFit usa la soglia della finestra.
- **Minimo di trade proporzionale:** `min_trades_finestra = max(1, ⌈50 × barre_ricerca / n⌉)`
  (circa 7, 17, 27, 37, 47); vale anche per il minimo per lato dei controlli finali.
- Esce la strategia completa (o nessuna). **Finestra valida** se la strategia ha almeno **20 trade**
  sulla sua ricerca.

### 11.3 Aggregati

Sulle sole finestre valide:
- prove concatenate: `wfo_net_profit`, `wfo_n_trades`, `wfo_avg_trade`, `wfo_max_dd`;
- efficienza = (net prove / trade prove) / (net ricerche / trade ricerche);
- **errore standard fra finestre** (almeno 3 finestre con trade):
  ```
  avg = Σ net_i / Σ n_i
  var = Σ n_i × (net_i/n_i − avg)² / Σ n_i
  ES  = sqrt(var / K_finestre_con_trade)
  ```
  sotto 3 finestre: non stimato.
- `wfo_anni_prova` = somma delle durate delle prove di **tutte** le finestre (anche non valide).

### 11.4 Livelli (definizione v4.0)

```
wfo_min_trades = ⌈12 × wfo_anni_prova⌉
wfo_pavimento  = max( min_expected_avg_trade, costi veri CFD con lo swap dei trade delle prove )

non misurata   se la walk-forward non ha girato o nessuna finestra è valida
fuori          se wfo_net_profit ≤ 0
seconda        se wfo_n_trades < wfo_min_trades
prima          se wfo_avg_trade − ES ≥ wfo_pavimento      (senza ES: wfo_avg_trade ≥ pavimento)
seconda        altrimenti
```

Il livello vale per **motore e mercato**: tutte le candidate di quel motore sulla cella lo ereditano.

La walk-forward gira (`--wfo final`) sui motori presenti fra le candidate arrivate ai cancelli
(`--wfo all`: su tutti i motori con almeno una candidata oltre l'ultimo terzo; usata in produzione).

---

## 12. Il modello dei costi CFD

### 12.1 Voci

Tariffe in `unger/configs/cfd_costs.yaml`, nelle unità del **nostro future**:

- `spread`: in unità di prezzo → costo `spread × bpv` per trade (giro completo);
- `comm_pct`: frazione del nozionale andata e ritorno → `comm_pct × prezzo × bpv`;
- `swap_l`, `swap_s`: costo **annuo** in frazione del nozionale per notte (positivo = costo, negativo = accredito) →
  `swap_trade = prezzo × bpv × tasso(lato) / 365 × notti`.

Prezzo = **ultimo close della serie** (continui retro-aggiustati). Le coppie CD, SF, JY hanno swap
long/short **scambiati** e spread riportato nelle unità del future (il CFD è USD/XXX).

### 12.2 Tariffe (lettura FTMO del 2026-09-03, src M = misurata, V = ticker, A = assunta)

| simbolo | CFD | spread | comm% | swap long | swap short | src |
|---|---|---|---|---|---|---|
| AD | AUDUSD | 0,00003 | 0,000069831 | 0,015038198 | 0,031809612 | V |
| BP | GBPUSD | 0,00003 | 0,000036921 | 0,012910289 | 0,015039543 | M |
| EC | EURUSD | 0,0000215387 | 0,000043112 | 0,02986644 | −0,001793875 | M |
| NE1 | NZDUSD | 0,0000790029 | 0,000084525 | 0,02505156 | 0,002776651 | M |
| CD | USDCAD | 0,00003440099857 | 0,00005 | 0,030345073 | −0,001707731 | M |
| SF | USDCHF | 0,0001095444545 | 0,00005 | 0,060166819 | −0,008575916 | M |
| JY | USDJPY | 0,00002968198636 | 0,00005 | 0,040765527 | −0,006101119 | M |
| SI | XAGUSD | 0,0560918223 | 0,000028 | 0,084318032 | −0,003272369 | M |
| GC | XAUUSD | 0,4481286012 | 0,000028 | 0,069858504 | 0,01099655 | M |
| PL | XPTUSD | 6,839606074 | 0,000028 | 0,120728054 | 0,043170778 | M |
| YM | US30.cash | 2,262940149 | 0 | 0,080053723 | −0,003454275 | M |
| FESX | EU50.cash | 0,86 | 0 | 0,061677782 | 0,003935678 | V |
| FDAX | GER40.cash | 1,23 | 0 | 0,061913929 | 0,003952453 | M |
| HK | HK50.cash | 6,631284026 | 0 | 0,058946073 | 0,008905161 | M |
| NK | JP225.cash | 10,51219325 | 0 | 0,046053654 | 0,019737517 | M |
| NQ | US100.cash | 1,588593969 | 0 | 0,079008114 | −0,00207256 | M |
| ES | US500.cash | 0,6002539872 | 0 | 0,074190828 | 0,002532466 | M |
| CL | USOIL.cash | 0,0752921949 | 0 | −0,024925722 | 0,145771339 | M |
| RTY | US2000.cash | 1,0215082 | 0 | 0,066926112 | 0,01059827 | M |
| CC | COCOA.c | 19,343063 | 0 | 0,102911961 | −0,015283402 | M |
| KC | COFFEE.c | 0,3204022177 | 0 | −0,050993968 | 0,259687346 | M |
| S | SOYBEAN.c | 2,136976411 | 0 | 0,075310514 | −0,00852356 | M |
| W | WHEAT.c | 1,782086065 | 0 | 0,111627953 | −0,016956749 | M |
| BTC | BTCUSD | 1,147363242 | 0,0013 | 0,3 | 0,3 | M |
| NG | NATGAS.cash | 0,0596205622 | 0,000028 | 0,130668257 | −0,021155813 | M |
| C | CORN.c | 1 | 0 | 0,310105975 | −0,061231116 | A |
| HO | HEATOIL.c | 0,0339843214 | 0,000028 | −0,09017248 | 0,426871558 | M |
| CT | COTTON.c | 0,4261710035 | 0 | 0,178132736 | −0,031409879 | M |
| SB | SUGAR.c | 0,1383177199 | 0 | 0,181432749 | −0,032017544 | M |

Senza tariffa (niente costi CFD, i cancelli sui costi non giudicano): HG e gli altri simboli non in tabella.

### 12.3 Notti di swap: rollover attraversati

- Il broker addebita lo swap al **rollover delle 17:00 di New York** (23:00 di Roma, 22:00 nelle
  settimane con l'ora legale sfasata), una volta al giorno, alle posizioni aperte in quell'istante.
- **Peso per giorno della settimana** (data di New York del rollover, lunedì..domenica):
  - `triplo_ven` [1,1,1,1,3,0,0]: NQ, ES, YM, RTY, FDAX, FESX, HK, NK;
  - `triplo_mer` [1,1,3,1,1,0,0]: GC, SI, PL, BP, AD, EC, NE1, CD, SF, JY, CL, NG, HO, CC, KC, S, W, C, CT, SB;
  - `ogni_giorno` [1,1,1,1,1,1,1]: BTC.
- **Notti di un trade** = somma dei pesi dei rollover `r` con `istante_ingresso < r ≤ istante_uscita`:
  - ingresso: dentro la barra d'ingresso (se un rollover cade dentro la barra e l'istante vero non è
    noto, si conta come pagato: dopo il rollover il mercato è in pausa, quindi il fill è avvenuto prima);
  - uscita **a chiusura** (`EOD`, `MAXBARS`, `FORCE`): esattamente la fine della barra d'uscita (un
    rollover che coincide con la fine barra si **paga**);
  - uscita **dentro la barra** (stop, target, trailing, breakeven): un rollover dentro la barra d'uscita
    senza istante vero si conta come **non** pagato.
- Conseguenza: sulle sessioni che finiscono a mezzanotte di Roma ogni uscita di fine sessione paga una notte.

---

## 13. Consegna, registro, paniere e prova sul broker

### 13.1 Codice univoco della strategia

```
impronta = "SIMBOLO|TF|MOTORE|" + "|".join( p=valore  per ogni parametro di ENTRATA, in ordine alfabetico )
codice   = SIMBOLO-TF-MOTORE_senza_underscore-<primi 6 caratteri esadecimali di sha256(impronta)>
```

Parametri di entrata = tutti tranne `stop_loss`, `take_profit`, `trailing_stop`, `breakeven`,
`max_bars`. Valori interi scritti senza decimali, gli altri col formato `%g`; valori mancanti esclusi.
TF: `15M`, `30M`, `1H`, `4H`, `1D`, `1W`. Forma: `NQ-15M-TFM-` + 6 caratteri esadecimali. Stessa
entrata con rischio diverso = stesso codice = stessa strategia.

### 13.2 Consegna

Si consegnano **prima e seconda scelta** (colonna `livello`), con parametri, schede, trade al minuto
sull'ultimo terzo, famiglie ("fuori dal paniere" per le non capofila). Registro di produzione:
`results/registro_v4.csv` (solo run v4 con consegna dichiarata).

### 13.3 Controllo del paniere sul broker

Regola scritta prima di guardare:
- **paniere** = prima + seconda scelta di tutte le celle, **una per famiglia** (il capofila), 1 contratto;
- **storia**: ogni strategia ri-simulata sui future al minuto con lo storico della sua run, costi CFD al
  prezzo di oggi (se il net si scosta di oltre il 2% da quello della run, il verdetto lo segnala);
- **broker**: la strategia sulle **barre ricostruite dal feed a 1 minuto del broker** (stesse regole di
  sessione e orario), simulatore al minuto sullo stesso feed, costi CFD;
- **drawdown** su equity dei trade chiusi sommati per giorno d'uscita;
- **storia alla volatilità di oggi**: il lordo di ogni trade storico si moltiplica per
  `ATR giornaliero medio del broker / ATR giornaliero medio del suo anno` (stesso mercato); i costi no.

**Verdetto:** broker in utile **e** drawdown del broker ≤ drawdown massimo della storia alla volatilità
di oggi → **demo sì**; altrimenti niente demo finché non si trova la causa. **Nessun veto per singola
strategia** (14 mesi non giudicano una strategia). Il drawdown in dollari resta colonna.

### 13.4 Il broker rispetto ai future (fatti misurati da tenere presenti)

- Sessione del CFD: chiude alle 16:49 e riapre alle 18:05 di New York (il future CME 17:00-18:01).
- FDAX: il CFD tratta quasi 24 ore, il future no → orario di mercato 08-22 in ogni sorgente (§2.2).
- Il feed del broker non ha HK, HO, JY.

---

## 14. Configurazione in vigore e riga di comando

### 14.1 `validation.yaml` (valori effettivi per la v4.0)

| chiave | valore |
|---|---|
| `definizione.versione` | `v4.0` (battezzata 2026-09-17) |
| `definizione.v4.ultimo_terzo` | 1/3 |
| `definizione.v4.np_dd_ultimo_terzo` | 1,3 |
| `definizione.v4.min_trades_anno_medio` | 12 |
| `definizione.v4.wfo_trades_per_anno_prova` | 12 |
| `definizione.v4.plateau_min_worst` | 0 (il peggior vicino non boccia) |
| `definizione.v4.cancello_aspettativa`, `cancello_verita_minuto` | false (colonne) |
| `definizione.v4.swap_nel_pavimento` | true |
| `definizione.v4.cintura_costi` | BTC: 2 |
| `definizione.v4.wfo_min_trades_proporzionale` | true |
| `definizione.v4.plateau_tetto_vicino` | 1 |
| `is_oos.top_n_for_oos` | 0 (nessun tetto) |
| `is_oos.oos_diversity_quota` | 50 |
| `is_oos.dedup_before_oos` | true |
| `walkforward.top_m` | 500 |
| `walkforward.n_windows` | 5 |
| `walkforward.is_pct_per_window` | 0,667 |
| `walkforward.min_window_trades` | 20 |
| `walkforward.anchored` | true |
| `final.top`, `final.max_entry_overlap` | 3, 0,70 |
| `robustness.n_oos_blocks`, `min_oos_blocks_positive` | 3, 2 |
| `robustness.plateau_min_ratio` | 0,60 |
| `robustness.null_test_draws`, `null_max_p` | 200, 0,20 |
| `robustness.null_min_draw_trades`, `null_min_draw_trades_frac` | 10, 0,5 |
| `hard_filters.min_avg_trade`, `min_avg_trade_ticks`, `min_avg_trade_atr_frac` | 20, 2, 0,15 |
| `hard_filters.min_avg_trade_cfd_friction` | true |
| `hard_filters.min_expected_avg_trade_ticks` | 6 |
| `hard_filters.min_trades`, `min_trades_per_year`, `min_profitable_years_ratio` | 50, 5, 0,5 |
| `hard_filters.max_dd_pct_of_capital` | 0 (spento) |

(Le chiavi `is_oos.is_pct` 0,70, `oos_degradation_threshold`, `oos_min_profit_dd` 1,5 e
`plateau_min_worst` 0,40 valgono solo per la definizione v3.8.)

### 14.2 Comandi

```bash
.venv/bin/python -m unger.run --symbol NQ --timeframe 15m
```

Default rilevanti: `--metodo v4`, `--conferma-terzo 1/3`, `--controlli-finali tutti_gli_anni`,
`--wfo final`, `--lettura ottimista`, `--doppio-tocco primo_tocco`, `--doppio-tocco-barre percorso`,
`--anticipo-sessione 180`, `--dump-sweep all`, `--jobs` = tutti i core. Storia dal registro (2012).
`--no-consegna` per le prove (niente registro). `--definizione v4.1` / `v3.8` riproduce altre definizioni.

```bash
.venv/bin/python -m unger.matrix --symbols "BP,BTC,CC,CL,CT,ES,FDAX,GC,KC,NG,NQ,PL,SB,YM" --timeframes 15m,30m,1h,4h,day --wfo all --jobs 6 --out results/<cartella>
```

```bash
.venv/bin/python forward/paniere_broker.py results/<cartella con le celle>
```

---

## 15. Riproducibilità: metadata e run di prima

Ogni run scrive in `top_final.json` → `metadata` le regole con cui è stata fatta; chi ri-simula una run
**legge queste chiavi**, non i default di oggi.

| chiave | significato | assente = |
|---|---|---|
| `definizione` | blocco della definizione (`versione`, soglie) | v3.8 |
| `metodo` | `v4` percorso passo per passo | v3.5 |
| `start_date` | floor della storia | registro (2012) |
| `etichetta_barre` | `inizio` | orari intraday una barra avanti (run prima del 2026-09-11) |
| `anticipo_sessione` | 180 | regola di calendario |
| `orario` | fascia di mercato (FDAX) | nessun taglio |
| `doppio_tocco`, `doppio_tocco_barre` | `primo_tocco`, `percorso` | `long_prima` |
| `conferma_terzo` | 1/3 | 0 |
| `controlli_finali_tutti_gli_anni` | true | sulla sola fetta di scelta |
| `wfo_soglie_al_taglio` | true | soglie della storia intera |
| `wfo`, `rami_trigger`, `lettura`, `hard_filters`, `validation`, `consegna` | come da CLI | — |

Guardiani: `tests/verify_definizione_buona.py` (la definizione congelata), `tests/regress_trades.py`
(le run di prima riprodotte trade per trade), `tests/verify_percorso_v4.py`, `tests/test_notti_swap.py`,
`tests/verify_controlli_finali.py`, `tests/verify_sessioni_anticipate.py`, `tests/verify_doppio_tocco_barre.py`.

Versioni della definizione:
- **v3.8** (fino al 2026-09-14): ricerca sul 70%, fuori campione sul 30%, otto cancelli;
- **v4.0** (battezzata): questo documento;
- **v4.1** (provata il 2026-09-17, non adottata): v4.0 senza walk-forward e con storia dal 2015.

---

## 16. Mappa dei file sorgente

| area | file |
|---|---|
| dati, registro, orario | `unger/data/futures.py`, `unger/data/csv_loader.py`, `unger/configs/symbols.yaml` |
| etichette, orologi, sessioni, aggregazione | `unger/data/resample.py` |
| colonne di sessione | `unger/core/session.py` |
| filtri orari, Bollinger | `unger/core/filters.py` |
| ATR, Donchian, ATR di sessione | `unger/core/indicators.py`, `unger/atr_ref.py` |
| pattern | `unger/core/patterns.py` |
| motori, griglie | `unger/core/engines/*.py`, `unger/core/engines/base.py` |
| simulatore | `unger/backtest/simulator.py`, `unger/backtest/_simcore.py` |
| metriche, UngerFit, Monte Carlo | `unger/backtest/metrics.py`, `unger/backtest/evaluation.py` |
| percorso v4 | `unger/optimize/percorso_v4.py` (configurazione in `unger/optimize/optimizer.py`) |
| ultimo terzo, walk-forward | `unger/optimize/validator.py` |
| cancelli, plateau, pattern casuali, livelli WFO | `unger/optimize/robustness.py` |
| doppioni, famiglie, sovrapposizione | `unger/optimize/selection.py` |
| pipeline della cella, soglie, costi veri | `unger/run.py` |
| definizioni | `unger/definizione.py`, `unger/configs/validation.yaml` |
| costi CFD, notti | `unger/cfd_costi.py`, `unger/configs/cfd_costs.yaml` |
| codice strategia, registro | `unger/registro.py` |
| consegna | `unger/consegna.py`, `unger/dossier.py` |
| broker | `forward/feed_m1.py`, `forward/prova_m1.py`, `forward/paniere_broker.py` |

---

## 17. Glossario

| termine | significato |
|---|---|
| **cella** | un mercato su un timeframe |
| **sessione** | la giornata di contrattazione del mercato, con l'ora d'inizio del registro |
| **d0, d1…d5** | la sessione in corso e le 5 sessioni complete precedenti |
| **istante-regola** | fine della barra (intraday) o data della sessione (day): l'istante che leggono finestre e giorni |
| **strategia completa** | l'uscita del percorso v4 di un motore |
| **ultimo terzo** | le ultime 1/3 delle barre della cella (≈ dic 2020 → mag 2025): dentro la ricerca, giudicato a parte |
| **fetta di scelta / di conferma** | primi 2/3 e ultimo 1/3 degli anni di ricerca dentro il percorso |
| **R9** | una base che non guadagna su tutta la storia si abbandona |
| **D2** | regola dello stop: il più piccolo col 95% del miglior net profit al minuto |
| **plateau** | la strategia regge spostando ogni parametro di una tacca |
| **pattern casuali** | il pattern scelto contro pattern estratti a caso dalla stessa libreria |
| **prima / seconda scelta** | livello della walk-forward del motore su quel mercato |
| **famiglia** | strategie con più del 70% di entrate in comune; il capofila rappresenta la famiglia |
| **paniere** | i capofila di prima e seconda scelta, 1 contratto ciascuno |
| **UngerFit** | `sqrt((avg/soglia) × (100 × avg/DD))` |
| **costi veri** | spread + commissione + swap medio per trade del CFD, ai prezzi di oggi |
| **mirrored / unmirrored** | pattern speculari per long e short / pattern indipendenti per lato |
