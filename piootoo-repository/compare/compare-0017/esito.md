# compare-0017 — 111 strategie, 1/07/2025 → 25/02/2026, interno CFD contro cBot

Analisi del 2026-09-04. Motore **5.1.0 su entrambe le gambe**. Primo confronto su un arco lungo
(8 mesi, 2.845 + 2.446 trade): le strutture sono misurabili con il campione, non solo intuibili.

Domanda posta: *trovare gli errori di conversione delle strategie, gli errori sugli orari di
sessione e gli errori del cBot.* Stanno nei §3, §4 e §5; l'elenco operativo dei fix è in fondo.

**Due passaggi.** Il primo run interno (`backtest-20260904-0748-new-map`) girava con
`allowOverweek: false` contro il `FTMO-ALL` del cBot che ha `true`: non era confrontabile. È stato
rifatto (`backtest-20260904-0832`) con la stessa politica. Tutti i numeri qui sono del **secondo**;
del primo resta solo il §1.1, che è una misura utile ottenuta per sbaglio.

## Le gambe

| | file | tipo | motore | prezzi | arco ingressi |
|---|---|---|---|---|---|
| interno CFD | `trades-interno-cfd-FTMOPLATFORM.json` | `interno-cfd-FTMOPLATFORM` | `PiootooTradingService` 5.1.0 | `datafeed-external/FTMOPLATFORM/` | 01/07/2025 12:00 → 25/02/2026 13:00 |
| cBot | `trades-cbot-cfd-FTMO.json` | `cbot-cfd-FTMO` | `PiootooDistributedExecutionBot` nel backtester cTrader, 5.1.0 | CFD **FTMO**, conto 17188650, piano `FTMO-ALL` | 01/07/2025 05:00 → 08/03/2026 09:11 |
| ricerca | `run-engine/run-08-settembre/DOSSIER_PANIERE (1).md` | — | motore Python | dati vendor | OOS ~4 anni |

`Holding` ora allineato su entrambe: `allowOvernight: true`, `allowOverweek: true`, flat 20:45 UTC,
weekend ven 20:45 → dom 23:00. Resta aperto da compare-0016 che `FTMO` (conto) e `FTMOPLATFORM`
(archivio barre) siano lo stesso broker.

Il cBot gira ancora a **1/10 della taglia interna** su tutti i simboli (rapporto denaro-per-punto
misurato: 0,100 su BTC CC CL ES GC NG NQ PL YM, 0,096 su BP, 0,0987 su KC, 0,1158 su FDAX = il
cambio EUR/USD). Non è dichiarato da nessuna parte: tutti i numeri sotto sono **normalizzati ×10**.

## 1. Il divario, con la politica allineata

| | trade | netto |
|---|---|---|
| interno CFD | 2.845 | **+313.979 USD** |
| cBot, normalizzato ×10, stessa finestra | 2.446 | **−375.156 USD** |

Divario **+689.135 USD** contro un errore standard sulla somma di ±115.381: **6 σ**. Non è rumore,
e non è più il piano.

**Va contro l'attesa dichiarata.** La convenzione «lo stop prevale sul target dentro la barra»
rende il backtest interno *pessimista*, quindi ci si aspetta che l'interno renda **meno** del cBot
su tick. Qui rende **689 mila dollari in più**, e la convenzione pessimista è già attiva e già
misurata (§6: 311 trade a −106.697 USD). Il divario quindi non è il costo della risoluzione
intrabarra: è qualcosa che il cBot vede e l'interno no, o viceversa.

Scomposizione, appaiando per (strategia, lato) con tolleranza di una barra e minimo 2 ore:

| blocco | trade | USD |
|---|---|---|
| 1.795 coppie | — | **+369.898** |
| solo interni | 1.050 | **+151.839** |
| solo cBot | 651 | **−167.399** |
| **totale** | | **+689.135** |

E dentro le coppie, per combinazione di uscita:

| interno → cBot | coppie | USD |
|---|---|---|
| protettiva → protettiva | 1.357 | **+148.918** |
| target → protettiva | 25 | **+136.564** |
| uscita a tempo → protettiva | 19 | +53.508 |
| segnale opposto → protettiva | 34 | +48.508 |
| max bars → protettiva | 5 | +19.291 |
| uscita a tempo → Closed | 113 | +16.529 |
| max bars → Closed | 26 | −15.585 |
| protettiva → target | 5 | −28.536 |
| **target → target** | **184** | **−646** |

Le due righe da guardare per prime:

- **1.357 coppie in cui entrambi escono su una protettiva, +148.918 USD a favore dell'interno.**
  Stessa entrata, stessa uscita di famiglia, e l'interno perde meno: è la convenzione di
  riempimento di stop e trailing, non un segnale diverso.
- **25 coppie `target → protettiva` per +136.564 USD**, cioè 5.460 USD di divario a trade. L'interno
  prende il target dove il cBot viene stoppato. Su venticinque trade è la firma della barra che
  contiene entrambi i livelli.

I **target contro target sono identici** (184 coppie, −646 USD complessivi): quando la barra non è
ambigua, i due motori coincidono. Il problema è tutto nelle barre ambigue e nelle uscite protettive.

Per simbolo (delta = interno − cBot, ×10):

| sym | nI | nE | nettoI | nettoE | delta |
|---|---|---|---|---|---|
| BTC | 244 | 428 | +172.418 | −83.122 | **+255.541** |
| FDAX | 147 | 128 | +107.064 | −29.609 | **+136.673** |
| NQ | 888 | 920 | +40.423 | −62.524 | +102.946 |
| NG | 228 | 205 | +17.958 | −84.959 | +102.917 |
| ES | 211 | 210 | +29.312 | −13.802 | +43.114 |
| KC | 28 | 31 | +24.098 | −8.422 | +32.520 |
| HO | 232 | **0** | −24.896 | 0 | −24.896 |
| HK | 344 | **0** | +20.186 | 0 | +20.186 |
| GC | 223 | 228 | −47.947 | −28.897 | −19.050 |

### 1.1 Correzione: il flat del venerdì non era la fonte del risultato

Nel primo passaggio avevo misurato che il flat del venerdì chiudeva 364 trade interni per
+345.469 USD, e ne avevo dedotto che senza quelli il run interno valesse −31.637. **Il re-run lo
smentisce**: tolto il flat, l'interno fa **+313.979**, praticamente identico a prima (+313.833).

Il motivo è che quei 364 trade non sparivano, cambiavano solo uscita: senza il taglio del venerdì
le posizioni proseguono e **326 di esse attraversano il sabato, valendo +316.514 USD** — quasi
esattamente i +345.469 che prima incassava il flat. La sottrazione era un controfattuale sbagliato,
perché assumeva che gli altri 2.239 trade restassero identici.

Vale come metodo: **su un portafoglio multiday non si stima l'effetto di una regola di tenuta
sottraendo i trade che porta il suo nome. Si rifà il run.**

## 2. Il porting rende un quinto della ricerca, e non per il numero di trade

Scalando le schede del dossier (P&L e trade OOS) sui 240 giorni del run, su 103 strategie appaiate
per (simbolo, timeframe, motore):

| | trade | P&L |
|---|---|---|
| atteso dalla ricerca, scalato | 3.051 | **+1.590.859 USD** |
| run interno | 2.489 | **+315.672 USD** |

Il rapporto mediano dei trade è **0,81**: la cadenza è quasi giusta. Il denaro no: **il port
realizza il 20% dell'atteso**. Il difetto non è «entra poco», è «entra su barre diverse». Le cause
strutturali sono nel §3.

Gli estremi di cadenza, che sono i punti da guardare per primi:

| scheda | classe | attesi | reali | rapporto |
|---|---|---|---|---|
| S19 | `PTS_BTC_BIA_001_60` | 40 | **123** | 3,07 |
| S89 | `PTS_SB_TFM_001_240` | 17 | **0** | 0,00 |
| S95 | `PTS_NG_BSW_001_30` | 12 | **0** | 0,00 |
| S38 | `PTS_HO_BIA_002_240` | 65 | 7 | 0,11 |
| S113 | `PTS_PL_TFM_001_240` | 19 | 3 | 0,16 |

altre 18 strategie stanno fra 0,19 e 0,42, e sono quasi tutte a 240 e 1440 minuti.

## 3. Errori sugli orari di sessione

### 3.1 La griglia delle barre del feed CFD non è quella della ricerca

Misurata sui file di `datafeed-external/FTMOPLATFORM/`, in ora di Roma:

| timeframe | apertura barre nel feed | apertura barre nella ricerca |
|---|---|---|
| 240 m | **03:00 07:00 11:00 15:00 19:00 23:00** | 00:00 04:00 08:00 12:00 16:00 20:00 |
| 1440 m | **23:00** | 00:00 |
| 15/30/60 m | :00 / :30 (coincide) | :00 / :30 |

La griglia è ancorata a un'ora locale fissa (01:00Z d'estate, 02:00Z d'inverno), quindi è stabile
in ora di Roma ma **sfasata di tre ore** rispetto al giorno di calendario europeo su cui la ricerca
taglia le sessioni. Ogni barra a 4 ore copre quattro ore diverse da quelle della ricerca: i
massimi/minimi di canale, gli `H_d0`/`L_d0` in costruzione e le rotture cambiano.

Sono **40 strategie a 4 ore + 13 daily su 116**, ed è lo stesso gruppo che nel §2 trada troppo poco.

Sul daily c'è in più uno **slittamento di etichetta di un giorno**: la barra stampata alle 23:00 di
domenica contiene la sessione di lunedì. Su `@NQ_1440`, `@ES_1440`, `@FDAX_1440` il giorno di
sessione risulta *domenica–giovedì*, con **venerdì a zero**. Oggi nessuna delle 12 strategie daily
usa `skip_day`, quindi non morde; il giorno in cui una lo farà, `skip_day = 4` non scatterà mai.

### 3.2 Il calendario di sessione della §2.1.1 del dossier non è riprodotto

Il dossier misura sul DAX un costo dell'**11% del P&L** per le sessioni che il CFD inventa e la
ricerca non ha. Contate sulla finestra del run (35 settimane):

| simbolo | sessioni sab (feed) | sessioni dom (feed) | dom secondo il dossier |
|---|---|---|---|
| BTC | **35** | **34** | 27 su ~1.900 (≈7% delle settimane) |
| NQ, ES, GC, NG, HO, YM, PL, BP, CL | 0 | **34** | 48 su ~3.450 (≈7%) |
| FDAX (daily) | 0 | **34** | **0** |
| CC, CT, KC, SB | 0 | 0 | 0 ✔ |

Cioè: **su BTC il feed apre 7 sessioni a settimana dove la ricerca ne ha 5,07**, e su tutti i
simboli CME apre una sessione domenicale ogni settimana dove la ricerca ne ha una ogni quattordici.
FDAX viola direttamente la riga «dom = 0», che è quella su cui l'11% è stato misurato.

`PTS_BTC_BIA_001_60` a 3,07× la cadenza attesa è la firma: il BIAS fa una entrata per sessione e
per direzione, e sta ricevendo sessioni in più.

### 3.3 Orari della ricerca su cui il feed non ha barre — leghe di strategia morte in silenzio

`@HO_60` **non ha nessuna barra nell'ora 22:00–23:00 di Roma** (0 su 3.762 barre nel periodo).
`@NG_30` ha una sola barra alle 23:00 di giovedì in 35 settimane.

| classe | leg | orario dichiarato | barre disponibili in 35 settimane | effetto nel run |
|---|---|---|---|---|
| `PTS_HO_BSW_001_60` | ingresso LONG | mar 23:00 | **1** | **0 segnali buy** |
| `PTS_HO_BSW_002_60` | ingresso LONG | mar 23:00 | **1** | **0 segnali buy** |
| `PTS_HO_BSW_001/002_60` | uscita SHORT | mar 23:00 | **1** | uscita mai eseguita |
| `PTS_HO_BSW_003_60` | uscita LONG | ven 23:00 | **0** | uscita mai eseguita |
| `PTS_NG_BSW_001_30` | ingresso SHORT | gio 23:00 | **1** | **0 segnali sell** |

Il BIASW entra ed esce a giorno e ora fissi: se quella barra non c'è, la gamba non esiste. Il motore
non lo segnala — non è un errore, non è uno skip, è niente. **`PTS_NG_BSW_001_30` chiude gli 8 mesi
con zero segnali su 8.045 valutazioni** ed è l'unica strategia del run in quella condizione; metà
della spiegazione è qui, l'altra metà (il long del lunedì alle 20:00, dove la barra c'è 34 volte su
35) resta da capire nei gate.

Da verificare come conseguenza: la scheda S95 dice che *«l'orario è l'etichetta di **chiusura** della
barra: su 30m la barra delle 14:00 copre 13:30–14:00»*, mentre il datafeed Piootoo etichetta
all'apertura. Se `BiasWeeklyEngine` confronta `Hhmm(barTime)` con `EntryTime` senza compensare, i
sette BIASW entrano ed escono **una barra dopo** la ricerca. Non è dimostrato: va misurato sui
trade di riferimento `*/consegna/trades/fam03_BIASW.csv`.

### 3.4 Feed a 4 ore con meno di sei barre al giorno, e ordini che muoiono di conseguenza

| feed | barre nel periodo | ore di Roma presenti | barre al giorno |
|---|---|---|---|
| `@SB_240` | 499 | 06/07, 10/11, 14/15 | **3** |
| `@KC_240` | 661 | 07/08, 11/12, 15/16, 19 | 4 |
| `@CT_240` | 819 | 02/03 … 18/19 | 5 |
| `@CC_60` | 1.659 | 10 → 19 | 10 (su 24) |

`PTS_SB_TFM_001_240` dichiara una finestra 13:00–23:59: le uniche barre di segnale ammesse sono
quelle delle 14:00/15:00, e la barra successiva — quella su cui l'ordine vive — è quella del giorno
dopo alle 06:00, ben oltre `ExpiresAtUtc + 240 min`. Risultato: **478 segnali di ingresso, zero
trade**, per tutto il run. È l'invariante di `docs/domini/orologio-barre-e-fill.md` applicata a un
feed che non ha la barra successiva: corretta come regola, ma qui produce una strategia che non può
operare e nessun errore. Nel secondo run ci finisce anche `PTS_CT_TFU_001_240`, con segnali e zero
trade chiusi.

## 4. Errori di conversione delle strategie

### 4.1 Quello che è giusto (verificato meccanicamente, non va rifatto)

Confronto automatico delle **116 schede** del dossier `run-08-settembre` contro le **124 classi**
`PTS_*`, appaiate per (simbolo, timeframe, motore):

- **Registro**: i bucket coincidono, tranne 8 classi in più che vengono dai dossier precedenti
  (`GC_PCH_002/003_60`, `NQ_PCH_001/002_15`, `NQ_PCH_005/006_30`, `NQ_TFM_001_60`, `NQ_TFM_004_15`).
- **Stop loss, take profit, trailing, breakeven, `max_bars`, finestra oraria, `skip_day`,
  `not_entry_day` long e short: 116 schede su 116 coincidono.** Zero differenze.
- **Gate pattern**: il direzionale della ricerca è sempre simmetrico fra long e short (116 su 116),
  quindi il campo unico `DirectionalYes`/`DirectionalNo` della classe è la codifica giusta; il
  neutrale compare solo nel blocco comune e il fast solo nei blocchi per lato, come le classi li
  dichiarano.
- **Valore punto**: quello implicito nei trade interni coincide con la tabella §2.4 del dossier su
  tutti i simboli operati (BP 62.500, BTC 5, CC 10, CL 1.000, CT 500, ES 50, FDAX 25, GC 100, HO
  42.000, KC 375, NG 10.000, NQ 20, PL 50, YM 5).

Confermo quindi il controllo del 02/09: **i numeri del porting sono giusti**. Gli errori che restano
sono di *tempo* e di *infrastruttura*, non di parametro.

### 4.2 `PTS_KC_SBO_001_240` traduce ancora il livello sbagliato

Punto già aperto il 02/09 e non chiuso: la scheda S26 descrive `level_source = 1` di `breakout.py`
(cummax della sessione corrente **inclusa** la barra in corso, con `n_sess`/`lev_include_sess0`
ignorati); la classe scrive `Sessions = 1; IncludeCurrentSession = true`, che è `level_source = 0`.
`SessionBreakoutEngine` non ha il percorso `level_source = 1`. Nel run la strategia opera (28 trade,
+24.098 USD) — quindi non è visibile come anomalia, ed è il motivo per cui è sopravvissuta a due
confronti.

### 4.3 Le sei BIAS dichiarano l'uscita di fine sessione e non la ottengono

Le schede S19, S34, S38, S88, S108, S116 dicono tutte *«Chiude tutto a fine sessione (nessun
overnight)»*. `BiasBarCountEngine` lascia `AppliesSessionExit = false` (per scelta documentata in
`EasyEngineBase`: l'uscita è l'indice di barra), quindi `Holding => Multiday` e per il piano quelle
strategie tengono la notte e il fine settimana.

Finché l'indice di barra cade dentro la sessione le due cose coincidono. Quando la sessione ha meno
barre dell'indice (festivo, sessione corta, e sui feed del §3.4 è la norma) la posizione resta
aperta e la ricerca l'avrebbe chiusa.

### 4.4 `PTS_NQ_PCH_001_15` è scritta in un'altra forma

È l'unica classe del catalogo che espone `Symbol`/`TimeframeMinutes` da campi mutabili (`_symbol`,
`_timeframeMinutes`) e accetta `SessionStartTime`/`SessionEndTime` da `Initialize`, invece di
dichiarare `Session`/`TradingWindow` come le altre 123. Non produce un errore oggi, ma è
un'eccezione al modello che i test di conformità sull'orologio non coprono allo stesso modo.

### 4.5 Tredici classi del catalogo non girano, e nessuno lo dice

| classi | perché |
|---|---|
| 8 × `PTS_JY_*` | nessun feed `@JY` sotto FTMOPLATFORM; `@JY` non è nella tabella di conversione del conto; il simbolo è `KnownButUnverified` nel registro strumenti |
| `PTS_GC_PCH_002_60`, `PTS_GC_PCH_003_60`, `PTS_NQ_PCH_005_30`, `PTS_NQ_PCH_006_30`, `PTS_NQ_TFM_004_15` | **non sono nel masterfilter** di `all-in` |

Le 5 non-JY hanno la stessa finestra e lo stesso motore delle sorelle che invece girano: o sono
residui da cancellare, o è il masterfilter a essere incompleto. Oggi la differenza non si vede da
nessuna parte, perché un run riporta solo ciò che ha schedulato.

## 5. Errori del cBot cTrader

### 5.1 Il cBot non esegue mai le VBO — terzo confronto di fila

| classe | trade interni | trade cBot | netto interno |
|---|---|---|---|
| `PTS_FDAX_VBO_001_240` | 36 | **0** | +48.404 USD |
| `PTS_NQ_VBO_002_240` | 4 | **0** | +1.759 USD |
| `PTS_NQ_VBO_001_1440` | 6 | **0** | −3.720 USD |

**46 trade, +46.443 USD, zero controparte.** Non è riscaldamento: `FDAX_VBO_001_240` ha
`skippedNotEnoughCandles = 0` e 160 segnali di ingresso. È l'unica famiglia di motori che il cBot
non esegue in nessun confronto (0015, 0016, 0017).

`PTS_NQ_VBO_002_240` ha in più **607 barre saltate per riscaldamento contro 436 valutazioni**:
chiede 606 candele e per la maggior parte del run non le ha. Anche fosse eseguita, opererebbe su
metà periodo.

### 5.2 Il cBot ignora la finestra di fine settimana del piano

Con lo stesso `Holding` dichiarato su entrambi i lati (`weekEnd` ven 20:45 → dom 23:00):

| | ingressi sabato/domenica | posizioni oltre il sabato | durata massima |
|---|---|---|---|
| interno | **0** | 326 | 27,4 giorni |
| cBot | **128** (tutti BTC) | 243 | 45,1 giorni |

L'overweek ora coincide (326 contro 243, entrambi autorizzati). **Gli ingressi nel fine settimana
no**: il cBot ne apre 128 dentro la finestra che il piano dichiara chiusa, l'interno zero. È il
grosso della differenza di cadenza su BTC (244 trade interni contro 445 del cBot) e il primo
contributo al +255.541 USD di divario su quel simbolo.

Nota: l'interno non entra nel weekend nemmeno con `allowOverweek: true`, e il flat settimanale in
quel ramo è disattivato — quindi il blocco viene da altro (il ciclo di iterazione o le sessioni).
**Da determinare quale dei due comportamenti è quello voluto**: la ricerca dà BTC sab = 0 e dom
≈ 7% delle settimane, cioè nessuno dei due è giusto.

### 5.3 HO, CT e HK: 576 trade interni su simboli che il conto ha disabilitati

`accounts/symbol-conversions.json`, tabella `cfd-ctrader-ftmo`, dichiara `"Enabled": false` per
**@CT, @HK, @HO, @SB**. Il file non è cambiato fra i run, ma l'esclusione sì:

| run | `strategiesNotSupportedByAccount` |
|---|---|
| compare-0016 (03/09) | 15 strategie (HK, HO, CT, SB) |
| compare-0017, primo passaggio | 5 (solo HK) |
| compare-0017, secondo passaggio | **nessuna** |

Nel secondo run girano **344 trade HK e 232 HO** che il cBot, correttamente, non esegue.
`AccountSymbolConversion.SupportsSymbol` è `mappato && Enabled`, quindi i quattro simboli sono nella
stessa condizione: l'unico ramo che spiega «nessuna esclusione» è
`conversion.HasSymbolTable == false`, cioè **la tabella non si è risolta e il run ha ammesso tutto
in silenzio** invece di fallire.

Legato: i moltiplicatori corretti di quei quattro simboli (CT 500, HK 50, HO 420, SB 1120) esistono
in `piootoo-repository/symbol-convertion/symbol-conversions.json` — **file modificato, non
committato, e non è quello che il server legge**. In `accounts/` valgono ancora tutti 1.

### 5.4 Le uscite non protettive del motore, sul cBot arrivano tarde o non arrivano

Dal cross-tab del §1: `uscita a tempo → protettiva` 19 coppie (+53.508), `segnale opposto →
protettiva` 34 (+48.508), `max bars → protettiva` 5 (+19.291). In totale il cBot chiude per ragioni
non protettive molto meno dell'interno, e quando lo fa arriva tardi.

Cosa invece torna: `IntradayOnly` è rispettato su tutte e 10 le strategie che lo dichiarano (zero
posizioni a cavallo del giorno, durate massime coincidenti entro un'ora).

## 6. Il difetto del motore interno: 311 trade che aprono e chiudono nello stesso istante

10,9% dei trade interni, **−106.697 USD**, zero dal lato cBot. Sono 259 `StopLoss`, 26
`TrailingStop`, 20 `BreakEven`, 4 `TakeProfit`, 2 `TimeExit`.

È la **risoluzione intrabarra**, e il segno cambia a ogni run: compare-0015 −30.687 (8% dei trade),
compare-0016 **+10.713** (20%), compare-0017 **−106.697** (10,9%). In questo run la scelta è
pessimista, ed è coerente con la convenzione «lo stop prevale sul target» — ma in compare-0016
l'engine sceglieva il ramo favorevole sugli stessi motori. **Finché non è una regola scritta e
testata, il ±10% del risultato interno non è affidabile in nessuna direzione**, e in particolare non
si può usare questo run per dire che l'interno è pessimista: qui è pessimista di 106 mila dollari e
resta comunque 689 mila sopra il cBot.

## Cose che tornano e non vanno riaperte

- **Le finestre operative sono rispettate.** Ricostruendo la barra di segnale sulla griglia reale
  del feed (ancorata in ora di Roma, §3.1): **3 ingressi fuori finestra su 1.950 dal lato interno,
  6 su 1.870 dal lato cBot**. Non c'è nessun bug di fuso sulla `TradingWindow`.
- **Tutti i parametri numerici del dossier** coincidono con le classi, 116 schede su 116 (§4.1).
- **Valore punto**: coincide con la §2.4 del dossier su tutti i simboli operati.
- **Prezzi d'ingresso**: su 1.795 coppie lo scarto mediano è 0,00 su BP e NG, 0,01 su CL, 0,045 su
  KC, 0,12 su GC, 0,19 su ES, 0,30 su NQ, 0,50 su FDAX. Le due gambe vedono gli stessi prezzi.
- **Target contro target**: 184 coppie, −646 USD complessivi. Identici.
- **Nessun errore di valutazione**: `errors = 0`, `skippedStaleCandle = 0`,
  `signalsWithoutExitSpec = 0`.
- **`wrongSideLevelsRejected` = 12.540**: il filtro dei livelli già scavalcati lavora.
- **`IntradayOnly` è rispettato dal cBot** su tutte e 10 le strategie che lo dichiarano.
- **L'overweek ora coincide** fra le due gambe: 326 contro 243 posizioni oltre il sabato, entrambe
  autorizzate dal piano. Non è più un difetto.

---

# Elenco fix

**Stato al 2026-09-04, motore 5.1.1.** ✅ = fatto e in build; ⚠ = fatto a metà, la parte aperta
è nella cella. Chiuse in questo giro: **A1** (era una diagnosi sbagliata, vedi la riga), **A2**,
**B5**, **C4a**, **D1**, **D3b**. Il codice è in `PiootooBacktestingService`,
`BacktestDiagnosticsLogger`, `SessionBreakoutEngine`, `BiasWeeklyEngine` e nei contratti del
riepilogo; le motivazioni in `docs/decisioni.md` alla data.

**Un run fatto con 5.1.0 e uno fatto con 5.1.1 non hanno lo stesso `backtest-summary.json`**: il
secondo dichiara `accountUniverse`, `fillConventions`, `requiredCandles` per strategia e il
conteggio del catalogo. Il confronto dei numeri resta valido — l'unico cambio di comportamento del
motore è `level_source` sul BO, che tocca solo `PTS_KC_SBO_001_240`.


Priorità = quanto denaro spiega, o quanto invalida le misure future. Ogni voce dice **dove**
intervenire e **come si verifica** che è chiusa.

## A — bloccanti per qualunque confronto successivo

| # | anomalia | dove | fix | verifica |
|---|---|---|---|---|
| **A1** ✅ | ~~La risoluzione intrabarra sceglie un ramo arbitrario~~. **Falso: la convenzione conservativa c'è già.** `CheckStopLossAndTakeProfit` valuta lo stop protettivo *prima* del target su ogni barra, inclusa quella d'ingresso, e `TrailingPeakIncludesCurrentBar = true` è deliberatamente il ramo conservativo. Il difetto vero è che **nessuna delle due compariva in un artefatto**, quindi due run con convenzioni diverse erano indistinguibili a posteriori. | `PiootooTradingService` (già corretto), `backtest-summary.json` | **Fatto**: il summary porta `fillConventions` (`intrabarPriority`, `trailingPeakIncludesCurrentBar`, `trailingMinStepFraction`, `rejectWrongSideLevels`, simboli con slippage sugli stop) e le stesse chiavi finiscono nel log di avvio. | Il summary del prossimo run dichiara `"intrabarPriority": "ProtectiveBeforeTarget"`. Resta da capire perché in compare-0016 lo stesso motore risultava ottimista. |
| **A2** ✅ | La tabella di conversione non si risolve e il run ammette **tutti** i simboli in silenzio: tre run, tre insiemi di esclusioni diversi con lo stesso file su disco. 576 trade HK+HO che sul conto non possono esistere. | `PiootooBacktestingService.ApplyAccountUniverse` | **Fatto**: un conto che *dichiara* un codice tabella e ne risolve zero righe fa fallire l'avvio con messaggio esplicito; il conto senza codice resta il conto neutro. Il summary porta `accountUniverse` con codice tabella, simboli mappati e abilitati, e `appliedAsNeutralAccount`. | Rilanciando lo stesso run si riottengono sempre le stesse esclusioni; con `@HO` disabilitato, zero trade HO. |
| **A3** | Due file di conversione divergenti: `accounts/symbol-conversions.json` (letto dal server, moltiplicatori 1 su CT/HK/HO/SB) e `symbol-convertion/symbol-conversions.json` (corretti: 500 / 50 / 420 / 1120), modificato e non committato. | `piootoo-repository/` | Portare i moltiplicatori corretti in `accounts/symbol-conversions.json`, decidere se i quattro simboli sono abilitati o no, e **cancellare la seconda copia** o dichiararla esplicitamente come sorgente. | Un solo file contiene i moltiplicatori; `git status` pulito. |
| **A4** | I sei file di feed segnalati in compare-0016 §5 non sono stati ricostruiti: `@GC_30/60/240`, `@CL_30`, `@BP_60`, `@FDAX_240` hanno 280–2.257 candele e **zero** barre nel periodo del run. | `datafeed-external/FTMOPLATFORM/`, `PiootooDatafeedSyncBot` | Riraccogliere quei sei a blocchi, e capire perché il bot li ha ricostruiti da zero perdendo la storia (tutti e sei con lo stesso stub a 2022-12-28). | Il `candleCount` di `backtest-summary.json` si ritrova nei file su disco. Oggi non è così e **compare-0017 non è riproducibile**. |

## B — spiegano il divario di 689 mila dollari

| # | anomalia | dove | fix | verifica |
|---|---|---|---|---|
| **B1** | 1.357 coppie in cui entrambi escono su una protettiva, **+148.918 USD** a favore dell'interno, con entrate identiche. È la convenzione di riempimento di stop e trailing. | `PiootooTradingService`, uscite protettive; `docs/domini/orologio-barre-e-fill.md` | Misurare la distanza realizzata dell'uscita interna contro la distanza **dichiarata**, come fatto in compare-0016 per gli stop del cBot, e allineare la convenzione (stop originale sull'apertura se la barra apre oltre; trailing e break-even mai all'estremo della barra in corso). | La mediana della differenza «distanza realizzata − distanza dichiarata» va a zero, e il blocco `protettiva → protettiva` scende sotto le decine di migliaia. |
| **B2** | 25 coppie `target → protettiva` per **+136.564 USD** (5.460 a trade): l'interno prende il target dove il cBot viene stoppato. | `PiootooTradingService`, correzione `postFillLow`/`postFillHigh` sulla barra d'ingresso | **Non ricade su A1**: la priorità conservativa è già attiva, quindi in quelle 25 barre lo stop non è mai stato toccato *secondo il modello*. Il sospetto è la convenzione OHLC deterministica della barra d'ingresso (rialzista O→L→H→C), che su un fill stop sostituisce il minimo col close e quindi nasconde lo stop. Vanno guardate una per una. | Le 25 coppie diventano `protettiva → protettiva`, oppure si dimostra che il cBot le stoppa per un motivo suo. |
| **B3** | Il cBot apre **128 posizioni nel fine settimana** (tutte BTC) dentro la finestra che il piano dichiara chiusa (ven 20:45 → dom 23:00). L'interno zero. | `PiootooDistributedExecutionBot`, gestione della `WeekEnd` del descriptor | Il bot deve rifiutare gli ingressi dentro la finestra di fine settimana, non solo evitare di tenere posizioni. **E va deciso quale dei due comportamenti è quello giusto**: la ricerca dà BTC sab = 0 e dom ≈ 7%, quindi nemmeno l'interno (che scarta tutto) è fedele. | Ingressi sabato/domenica: zero da entrambe le parti, e il conteggio delle sessioni BTC del feed si avvicina alla riga §2.1.1 del dossier. |
| **B4** ⚠ | Il cBot non esegue **mai** le VBO: 46 trade interni, +46.443 USD, zero controparte, terzo confronto di fila. **La formulazione era sbagliata**: non e' detto che il cBot non sappia eseguirle. Le tre VBO chiedono un riscaldamento fuori scala — `PTS_FDAX_VBO_001_240` **501 candele** a 4 ore (ATR a 500 barre), `PTS_NQ_VBO_002_240` **606** (ATR a 100 sessioni), contro le 36 delle altre strategie sullo stesso stream — e il server salta in silenzio finche' non le ha. Il terzo (`PTS_NQ_VBO_001_1440`) ne chiede 6 e ha solo 6 trade interni: campione troppo piccolo per dire qualcosa. | `VolatilityBreakoutEngine.RequiredCandles`, `PiootooDistributedExecutionBot.LoadHistoryBackwards` | **Fatto a meta'**: il riscaldamento adesso e' visibile (`requiredCandles` per strategia nel summary + diagnosi `[riscaldamento]`). Per separare le due cause serve il `session-summary.json` del run del cBot con `everEvaluable` e `historyBarsHighWater` — quel run non l'ha prodotto, quindi **la prossima sessione del cBot va chiusa normalmente**. | `PTS_FDAX_VBO_001_240` produce trade da entrambe le parti, oppure `everEvaluable: false` chiude la questione. |
| **B5** ✅ | `PTS_NQ_VBO_002_240` salta più barre per riscaldamento (607) di quante ne valuti (436). | `VolatilityBreakoutEngine.RequiredCandles`, o il periodo di riscaldamento del run | **Reso visibile**: il summary porta `requiredCandles` per strategia e una diagnosi `[riscaldamento]` per chi salta almeno quante barre valuta. Resta la decisione: allungare il pre-caricamento o dichiarare la strategia inutilizzabile su questo archivio. | `skippedNotEnoughCandles` ≪ `evaluations`. |

## C — errori sugli orari, che spiegano il rapporto 1:5 con la ricerca

| # | anomalia | dove | fix | verifica |
|---|---|---|---|---|
| **C1** | La griglia a 4 ore del feed CFD apre a Roma 03/07/11/15/19/23, la ricerca a 00/04/08/12/16/20. **53 strategie su 116** leggono barre che coprono ore diverse. | raccolta (`PiootooDatafeedSyncBot`) o aggregazione a valle | Decidere: (a) riaggregare il feed sul confine della ricerca partendo dai 15 minuti, oppure (b) accettare lo sfasamento e dichiararlo nel `backtest-summary.json` come si fa con `datafeedBroker`. Non lasciarlo implicito. | Le strategie a 240 con rapporto di cadenza 0,2–0,4 risalgono verso 1, oppure il summary dichiara la griglia e il confronto con la ricerca smette di essere presentato come confrontabile. |
| **C2** | La barra daily apre a Roma 23:00 e porta l'etichetta del giorno prima: il giorno di sessione risulta domenica–giovedì, **venerdì mai**. Latente finché nessuna daily usa `skip_day`. | stesso punto di C1, più `EasyLib.OHLCMulti5` | Riallineare l'etichetta al giorno di calendario europeo, o rifiutare esplicitamente una daily con `skip_day` su questo feed. | Il giorno di sessione dei daily copre lunedì–venerdì; un test di conformità impedisce a una daily con `skip_day` di girare su un feed sfasato. |
| **C3** | Nessuno dei due motori scarta le sessioni domenicali che il CFD inventa: BTC 35 sabati e 34 domeniche in 35 settimane, FDAX daily 34 domeniche dove il dossier dichiara **0** e misura un costo dell'11% del P&L. | `EasyLib.OHLCMulti5` / costruzione delle sessioni | Implementare il calendario §2.1.1 come **filtro dichiarato per strumento**, non come «ignora il weekend»: sui CME le sessioni domenicali vere esistono nelle settimane di disallineamento e vanno tenute. | I conteggi per giorno del feed riproducono la tabella §2.1.1 sullo stesso periodo. |
| **C4** ✅⚠ | `@HO_60` non ha barre fra le 22:00 e le 23:00 di Roma; `@NG_30` ne ha una alle 23:00 di giovedì in 35 settimane. Quattro leg di BIASW non esistono e due uscite programmate non scattano mai. Nessun segnale, nessun errore. | `BiasWeeklyEngine`, `PiootooBacktestingService` | (a) **Fatto**: `UnreachableScheduleLegs` conta le barre a ogni istante programmato sulla serie appena caricata e il run emette una diagnosi `[calendario]` prima di partire, con la leg e il suo (giorno, ora). Due test. (b) **Aperto**: la convenzione di etichettatura — la scheda dice orario di *chiusura* barra, il feed etichetta all'*apertura* — va misurata contro `fam03_BIASW.csv`. | La diagnostica del run elenca le leg mai eseguibili ✅; gli istanti di ingresso coincidono con `fam03_BIASW.csv` ⚠. |
| **C5** | `PTS_SB_TFM_001_240`: 478 segnali, **zero trade** — `@SB_240` ha 3 barre al giorno e la barra su cui l'ordine dovrebbe vivere arriva il giorno dopo, oltre la scadenza. Nel secondo run ci finisce anche `PTS_CT_TFU_001_240`. | raccolta di `@SB_240`/`@CT_240`, oppure il masterfilter | O si ripara il feed (3 barre al giorno su un 4 ore non è un feed), o si tolgono le due strategie dal paniere. La diagnostica già lo dice: va ascoltata. | `coversRequestedRange: true` e ~6 barre al giorno, oppure le due classi fuori dal masterfilter. |

## D — conversione e catalogo

| # | anomalia | dove | fix | verifica |
|---|---|---|---|---|
| **D1** ✅ | `PTS_KC_SBO_001_240` traduce `level_source = 0` dove la scheda S26 descrive `level_source = 1`. Unico errore di parametro trovato, aperto dal 02/09. | `SessionBreakoutEngine` + la classe | **Fatto**: `SessionBreakoutEngine.LevelSource` con il ramo `1` (running massimo/minimo della sessione corrente, barra in corso **inclusa**, `n_sess` e `lev_include_sess0` ignorati, come `easy_engine_py/breakout.py`); la classe dichiara `LevelSource = 1` e non piu' `Sessions`/`IncludeCurrentSession`. Test `PythonBo_LevelSource1_UsesRunningSessionExtremeIncludingCurrentBar`. | Confronto con `KC_4h/consegna/trades/fam01_BO.csv`: stessi istanti di ingresso. Nel prossimo run i trade KC devono cambiare (il livello scende, quindi ingressi prima o in piu'). |
| **D2** | Le sei BIAS dichiarano nella scheda «chiude tutto a fine sessione» ma sono `Holding => Multiday`: quando la sessione ha meno barre dell'indice di uscita, la posizione resta aperta. | `BiasBarCountEngine`, `EasyEngineBase.AppliesSessionExit` | Decidere se il BIAS deve avere anche l'uscita di sessione come rete di sicurezza quando l'indice di barra non è raggiungibile, e dichiararlo. | Nessuna posizione BIAS sopravvive alla propria sessione. |
| **D3** | 12 classi su 124 non girano e nessun artefatto lo dice: 8 JY (nessun feed, simbolo `KnownButUnverified`, fuori tabella di conversione) e 5 non nel masterfilter (`GC_PCH_002/003_60`, `NQ_PCH_005/006_30`, `NQ_TFM_004_15`). | `workspaces/all-in/masterfilter.json`, `InstrumentRegistry`, catalogo | Decidere per ciascuna: dentro il paniere (e allora serve il feed e la riga di conversione) o cancellata. La seconda meta' (**far riportare al summary le classi del catalogo non schedulate**) e' **fatta**: `catalogStrategies`, `masterfilterStrategies` e `strategiesNotInMasterfilter`. | Il summary elenca catalogo, schedulate ed escluse, e i tre numeri tornano. Resta da decidere il destino delle 13 classi. |
| **D4** | `PTS_NQ_PCH_001_15` è l'unica classe con `Symbol`/`TimeframeMinutes` mutabili e `SessionStartTime` da `Initialize`, fuori dal modello `Session`/`TradingWindow` delle altre 123. | `Piootoo.Strategies/PiutooStrategies/` | Riportarla alla forma delle altre. | `StrategyClockConformanceTests` la copre come le altre. |
| **D5** | La taglia del cBot è 1/10 di quella interna su tutti i simboli e non è dichiarata da nessuna parte: va rimisurata a ogni confronto. | `run-*.json` / marcatore di origine | Scrivere la taglia (o il moltiplicatore di conto) nel marcatore del run. | Il confronto non richiede più di dedurre il fattore dai trade. |

## Trappole di misura di questa cartella

- **Non si stima l'effetto di una regola di tenuta sottraendo i trade che portano il suo nome**: il
  primo passaggio dava −31.637 senza il flat del venerdì, il re-run ha dato +313.979. Le posizioni
  non spariscono, cambiano uscita (§1.1).
- **La griglia delle barre non si deduce dall'epoch UTC.** I feed a 240 minuti sono ancorati a
  un'ora locale fissa (Roma 03:00), i daily a Roma 23:00. Con l'ancoraggio sbagliato risultano 49
  ingressi FDAX «fuori finestra» che non esistono. La griglia va letta dal file, o dal `firstBarUtc`
  del `backtest-summary.json`.
- **`@FDAX_240` e `@GC_240` su disco oggi non contengono il periodo del run** (A4): da lì non si
  ricostruisce la griglia. Usare il summary.
- **Il cBot riempie intrabarra**, l'interno sulla griglia del suo loop (15 minuti). Confrontare gli
  orari di ingresso grezzi non dice niente: va ricostruita la barra di segnale su entrambi i lati.
- **La normalizzazione ×10 va rimisurata** (rapporto `lordo / punti / quantità` per simbolo). Su
  FDAX vale 0,1158, non 0,100: la differenza è il cambio EUR/USD, non un errore di taglia.
- **Le famiglie d'uscita non si confrontano una a una**: `LocalExit:StopLoss` del cBot copre stop,
  trailing e breakeven; `LocalExit:Closed` copre time exit, max bars, flat e segnale opposto.
- **Il cBot arriva fino all'08/03/2026**, l'interno si ferma al 25/02: i trade oltre vanno tagliati
  prima di sommare.
