# Allineare il simulatore Python e il motore Piootoo (C#)

**A chi serve.** A chi mantiene il simulatore Python da cui le strategie `PTS_*` sono state
trovate, e a chi mantiene il motore C# che le esegue in backtest e in live. Le strategie nascono
di là e vengono eseguite di qua: ogni convenzione su cui i due non sono d'accordo non produce un
errore, produce **numeri plausibili e diversi**, e la differenza si scopre mesi dopo guardando
un'equity che non torna.

**Cosa contiene.** Le regole che i due sistemi devono condividere perché una conversione sia
verificabile: come sono fatti i motori, come si calcolano sessioni e fasce orarie, come si legge
l'etichetta di una barra, come si riempie un ordine, quali costi si applicano dove. Per ogni
regola c'è il punto del codice C# che la implementa, così una divergenza si può leggere invece
che dedurre.

**Cosa non contiene.** L'infrastruttura Piootoo (workspace, piani, sessioni live, cBot cTrader):
non serve a un simulatore e non va replicata.

**Convenzione di lettura.** «Ricerca» = il mondo Python. «Motore» o «engine» senza altra
qualifica = il motore di esecuzione C# (`PiootooTradingService`). «Motore di strategia» = una
delle dodici famiglie Unger, che esiste in entrambi i mondi.

---

## 1. La mappa dei due mondi

| Ruolo | Python (ricerca) | C# (Piootoo) |
|---|---|---|
| Motore di strategia | `easy_engine_py/*.py`, uno per famiglia | `Piootoo.Strategies/Easy/Engines/*.cs` |
| Interfaccia del motore | `BaseEngine.generate_signals(df, params) -> EngineSignals` | `EasyEngineBase` + `GenerateSignal(data, currentDate)` |
| Istanza di strategia | una riga di `parametri.csv` / `top_final.json` | una classe `PTS_[SYM]_[ENG]_[NNN]_[TF]` |
| Libreria pattern | `patterns.py` | `EasyLib.PatternNeutralFast` / `PatternDirectionalFast` |
| Filtri orari e giorno | `filters.py` (`time_window`, `day_filter`) | `EasyLib.TimeWindowInclusive` + `EasyEngineBase.InDeclaredWindow` / `PythonWeekday` |
| Indicatori | `indicators.py` (`donchian`, `session_atr`) | metodi privati dei motori + `EasyLib` |
| Simulatore di esecuzione | il simulatore (numba) del run | `PiootooTradingService` |
| Calendario dei simboli | `market_config` per run | `piootoo-repository/marketdata/market-calendars.json` |

Una differenza strutturale che va tenuta a mente leggendo tutto il resto: in Python il motore
produce **serie booleane su tutto il dataframe** e il simulatore le percorre; in C# il motore è
**stateless e valutato barra per barra**, e restituisce un `TradeSignal` che dichiara tutto —
livello, tipo d'ordine, validità, stop, target, breakeven, trailing, limite barre, eventuale
deadline. Le uscite non vengono mai emesse come segnali successivi: sono proprietà dell'ingresso.
È un vincolo dell'esecuzione live, dove il server manda solo intent di ingresso e chi esegue è un
bot esterno.

---

## 2. Il catalogo dei motori

Dodici famiglie, con le stesse sigle nei due mondi.

| Sigla | Nome | Trigger | Tipo ordine |
|---|---|---|---|
| `TF_M` | trend following mirrored | stop sugli estremi della sessione precedente (`highd1`/`lowd1`), pattern neutral + directional speculari | stop |
| `TF_U` | trend following unmirrored | stesso trigger, pattern `fast` indipendenti long/short | stop |
| `BIAS` | bias a conteggio barre | ingresso market/stop/limit governato dalle barre di sessione | market/stop/limit |
| `BIASW` | bias settimanale | ciclo settimanale, sempre multiday | market |
| `RBB_M` | reversal Bollinger mirrored | limit sulle bande, pattern speculari invertiti | limit |
| `RBB_U` | reversal Bollinger unmirrored | stesso trigger, pattern `fast` indipendenti | limit |
| `BO` (`SBO`) | session breakout | breakout del canale delle ultime N **sessioni** | stop |
| `LF` | level fader | rientro oltre un livello d1, market next-bar | market |
| `PC` (`PCH`) | price channel / Donchian | breakout del canale delle ultime N **barre** | stop |
| `VBO` (`VB`) | volatility breakout | breakout dall'apertura di sessione in funzione della volatilità | stop |
| `RHL` | reversal high/low | limit sugli estremi d1, pattern speculari invertiti | limit |
| `MAC` | moving average crossover | incrocio di medie, market next-bar, nessuna libreria pattern | market |

**Attenzione alle sigle.** Il dossier del paniere scrive `VB` dove il catalogo C# scrive `VBO`, e
`BO` dove il catalogo scrive `SBO`. Non sono motori diversi.

**Distribuzione del catalogo C# oggi** (103 classi `PTS_*`): 33 `TF_M`, 21 `TF_U`, 20 `PC`,
15 `SBO`, 4 `BIASW`, 3 `VBO`, 2 `RHL`, 2 `MAC`, 2 `BIAS`, 1 `RBB_M`. Nessuna `LF`, nessuna
`RBB_U`.

### 2.1 Le sentinelle dei pattern

Sono la fonte di equivoci più frequente in entrambe le direzioni. Un numero di sentinella **non è
un filtro con soglia estrema: è nessun filtro**, oppure è un filtro sempre falso.

| Libreria | «sempre vero» | «sempre falso» |
|---|---|---|
| neutral | `55` | `56` |
| directional | `52` | `53` |
| fast | `152` | `153` |
| uaptnbase | `41` | `42` |

Ogni numero non gestito dallo `switch` cade nel `default => false`. Quindi `ptn_dir_no = 53`
significa «nessuna esclusione direzionale», e scriverlo nel commento della classe come se
filtrasse qualcosa fa perdere tempo a chi confronta due varianti.

Sui pattern direzionali il **segno conta**: `n > 0` è il pattern rialzista che abilita il long, e
lo short usa lo stesso pattern specchiato; `n < 0` è il mirroring **invertito** — il long richiede
il pattern ribassista. In C# il segno lo applica il motore (`direction * DirectionalYes`), non la
classe.

### 2.2 La tabella di conversione dei parametri

Esempio completo sul `PC`; le altre famiglie seguono lo stesso schema.

| `parametri.csv` / `p_*` | Campo C# | Note |
|---|---|---|
| `channel_len` | `ChannelBars` | canale **inclusa** la barra di segnale (`donchian(shift=0)`) |
| `breakout_offset_ticks` | `OffsetTicks` + `TickSize` | livello = `canale + offset * tick`, **senza tick impliciti** |
| `direction` | `Direction` | 0 entrambi, 1 solo long, 2 solo short |
| `intraday_only` | `IntradayOnly` | `0` richiede `IntradayOnly = false` **esplicito** |
| `dvol_min` | `DvolMin` | `session_atr(14, shift=1)` in dollari; 0 disattiva |
| `ptn_neut_yes` / `ptn_neut_no` | `NeutralYes` / `NeutralNo` | |
| `ptn_dir_yes` / `ptn_dir_no` | `DirectionalYes` / `DirectionalNo` | il segno lo applica il motore |
| `start_hour` / `end_hour` | `TradingWindow` | `ZonedWindow.ResearchHours(start, end)`, **verbatim** |
| `skip_day` | `SkipDay` | convenzione pandas, 0 = lunedì |
| `stop_loss`, `take_profit`, `trailing_stop`, `breakeven` | `StopMoney`, `ProfitMoney`, `TrailingStopMoney`, `BreakEvenMoney` | USD per contratto di riferimento |
| `max_bars` | `MaxBars` | 0 disattiva |
| `session_start_hour` | (niente) | lo dichiara il calendario del simbolo, non la classe |

**Il default del motore non è neutro.** `IntradayOnly` vale `true` per default in
`PriceChannelEngine`, `SessionBreakoutEngine`, i due `TfEngines` e i due `ReversalBollingerBand`.
Una riga con `intraday_only = 0` che non lo disattiva diventa una strategia di sessione senza
violare nessun contratto: i test passano e la differenza si vede solo nei numeri. Su
`PTS_NQ_PCH_001_15` valeva 848 uscite a tempo su 2.012 trade.

---

## 3. Il modello temporale — cinque piani indipendenti

È la parte che costa di più sbagliare, ed è quella su cui i due sistemi devono essere identici
riga per riga. **Cinque domande diverse, cinque risposte diverse**, e nessuna si deduce dalle
altre.

| # | Domanda | Risposta |
|---|---|---|
| 1 | In che orologio sono stampati i timestamp del feed? | `feed-clocks.json`, per feed |
| 2 | Come si chiama una barra quando la confronto con un parametro? | la sua **chiusura** |
| 3 | A quale sessione appartiene una barra? | ancoraggio del simbolo, nell'orologio della ricerca |
| 4 | La barra è dentro la finestra operativa? | `start_hour`/`end_hour` del run, nell'orologio della ricerca |
| 5 | Che giorno della settimana è? | il giorno dell'**etichetta**, convenzione pandas |

### 3.1 L'orologio del feed

I CSV del vendor sono in **ora dell'Europa continentale** (CET d'inverno, CEST d'estate), e non lo
dichiarano. Due misure indipendenti lo stabiliscono, e conviene farle entrambe perché si
controllano a vicenda:

- **picco di volume** — su un indice americano il volume ha un massimo netto all'apertura del cash
  di New York (09:30 locali). Sul feed `@NQ` cade sullo slot 15:30, con l'11,4% del volume su 400
  giorni: sei ore avanti rispetto a New York.
- **pausa di manutenzione** — i future CME hanno un'interruzione giornaliera di un'ora,
  16:00–17:00 di Chicago per gli indici. Sul feed `@NQ` le barre finiscono alle 23:00 e riprendono
  alle 00:00: sette ore avanti rispetto a Chicago.

Se le etichette fossero UTC vero, entrambe si sposterebbero di un'ora fra inverno ed estate. Non
lo fanno.

**Conseguenza per il confronto.** `aggregate_flat_feed.py` converte a UTC vero una volta sola, e i
file `datafeed/@SYM_{minuti}.json` contengono UTC. Il dataframe della ricerca invece è indicizzato
in **ora locale europea**. Quindi:

```python
# stesso istante, due etichette
label_ricerca = utc_piootoo.astimezone(ZoneInfo("Europe/Rome")).replace(tzinfo=None) \
                + timedelta(minutes=timeframe_minutes)
```

Assumere UTC su un feed non dichiarato è precisamente l'errore che il registro esiste per
impedire: `FeedClockRegistry` si rifiuta di caricare un feed che non dichiara il proprio orologio.

### 3.2 Il nome della barra — la trappola che non si vede

**Una barra copre un intervallo ma porta un timestamp solo.**

| | etichetta della barra 16:00→17:00 |
|---|---|
| feed Piootoo | `16:00` (apertura) |
| TradeStation, CSV del vendor, motore di ricerca | `17:00` (chiusura) |

Stessi prezzi, nome diverso. Finché si confrontano barre con barre non cambia niente. Cambia
quando si confronta il **nome** della barra con un orario di parete preso dai parametri —
`start_hour`, `end_hour`, `skip_day`, un orario di uscita — perché quei numeri sono tarati contro
nomi di chiusura. Il confronto si sposta di **esattamente una barra**.

La prova sta nella distribuzione degli orari del CSV del vendor: su 200.000 righe di `@NQ` ce ne
sono **201 alle `00:01` e 4 alle `00:00`**, perché la prima barra della sessione — quella che
copre 00:00→00:01 — è stampata `00:01`. Con timestamp di inizio le due frequenze sarebbero simili.

**In C# la conversione vive in un punto solo**, `SessionClock.BarLabelHhmm`, raggiunto dai motori
via `EasyEngineBase.ParamHhmm` / `WindowParamHhmm`:

```csharp
BarLabelUtc(barOpenUtc, tf) => barOpenUtc.AddMinutes(tf);
```

I minuti si sommano **in UTC** e la conversione al fuso viene dopo: sommarli sull'orario locale
sbaglierebbe nei due giorni all'anno del cambio d'ora, che è il posto in cui l'errore non si
vedrebbe mai.

**Cosa deve fare il simulatore Python: niente.** È già la convenzione della ricerca. Quello che
deve fare è **non cambiarla**, e sapere che gli artefatti Piootoo (`signals.json`, `trades.json`,
`backtest-summary.json`) portano etichette di **apertura**. Per accoppiare i trade:

```python
entry_time_ricerca == entry_time_piootoo + timedelta(minutes=timeframe_minutes)
```

Misura storica, quando la conversione in C# non esisteva ancora: allineando gli `entry_time` del
report ai nostri `entryTimeUtc` su NQ 15m, il massimo di corrispondenze cadeva a **±15 minuti**,
345 contro 78 a scarto nullo — esattamente una barra.

`SessionClock.Hhmm` **resta** ed è l'orario di **apertura**: serve a dire *a quale sessione*
appartiene una barra, che è la domanda del §3.3 e ha una risposta diversa. Confondere i due è il
difetto ricorrente.

### 3.3 Il confine di sessione

Il motore di ricerca taglia le sessioni con

```python
session_day = (timestamp - pd.Timedelta(minutes=1) - pd.Timedelta(hours=session_start_hour)).normalize()
```

cioè, per `session_start_hour = 0`, il **giorno di calendario europeo**, 00:00 → 00:00. **Non è la
sessione CME 17:00→16:00 di New York**, ed è una scelta di modello della ricerca, non
un'approssimazione. Il `−1 minuto` compensa l'etichetta di chiusura: la barra delle `00:00`
appartiene alla sessione **precedente**.

L'equivalente C#, che lavora su etichette di apertura, è in `SessionGrid.SessionDayOf`:

```csharp
var day = clock.SessionDay(instantUtc);          // data locale in researchTz
return clock.Hhmm(instantUtc) >= SessionStartHour * 100 ? day : day.AddDays(-1);
```

Le due formule sono la stessa cosa scritta sulle due etichette. Una sessione a giornata piena va
da `h:00` del giorno `D` a `h:00` del giorno `D+1` escluso, e **ogni barra appartiene a una
sessione**: non esiste la barra «fuori sessione» della sessione di borsa.

**L'ora di inizio è dello strumento, non della classe.** Sta in `market-calendars.json` come
`sessionStartHour`, nel fuso `researchTz`:

| Simboli | Inizio sessione (ora della ricerca) |
|---|---|
| FDAX, CC, CT, KC, SB, HK | **01:00** |
| tutti gli altri | 00:00 |

Un ancoraggio sbagliato **non dà barre sbagliate: dà barre diverse**, e nessun controllo a valle se
ne accorge. Segmentando le stesse 462.692 barre a 15 minuti di `@NQ` nei due modi — ancoraggio
della ricerca contro sessione di borsa letta nell'orologio sbagliato — le sessioni riconosciute
diventano 6.035 invece di 5.013, e sul livello che gli engine trend-following tradano più spesso
(`highd1`/`lowd1`) i due modi coincidono solo nel **2,8%** delle barre, con scostamento mediano di
20,8 punti.

**Gli interi `1700` delle sorgenti EasyLanguage non sono un ancoraggio.** `SessBegin = 1700` è la
riapertura Globex in ora di Chicago, un **modello di sessione diverso**. Non c'è nessuna aritmetica
che porti da `1700` a un ancoraggio, e cercarne una è l'errore: le due coincidono per gran parte
dell'anno — mezzanotte a Roma sono le 17:00 a Chicago — e divergono nelle circa quattro settimane
in cui gli Stati Uniti sono già passati all'ora legale e l'Europa no.

### 3.4 La finestra operativa

`filters.py` calcola

```python
minuti = index.hour * 60 + index.minute
dentro = (minuti >= start) & (minuti <= end)      # OR se attraversa la mezzanotte
```

**Nessun riferimento a dove inizi la sessione: le due regole sono indipendenti.** Estremi
**inclusi**, `-1` disabilita il filtro.

Da qui tre regole per il porting, tutte già costate una divergenza reale:

1. **`start_hour`/`end_hour` si riportano verbatim.** Mai convertirli nell'ora di borsa del
   simbolo. La conversione a mano — meno sette ore per NQ, meno sei per GC — è esatta solo fuori
   dalle settimane in cui l'ora legale americana ed europea non sono allineate.
2. **L'orologio della ricerca è `Europe/Rome`, per ogni simbolo**, qualunque sia la borsa dello
   strumento. Misurato, non dedotto: prendendo la barra a volume massimo di ogni giorno del 2024 —
   quella che marca l'apertura del cash americano — il picco sta alle 22:00 a febbraio, alle
   **21:00 dall'11 al 28 marzo**, e di nuovo alle 22:00 ad aprile. Quelle tre settimane sono
   esattamente la finestra fra il passaggio americano all'ora legale (10 marzo) e quello europeo
   (31 marzo). Con un offset fisso da UTC quello scarto non esisterebbe.
3. **Il confronto è su HHMM pieni, non sulle sole ore.** Confrontare `hour` allarga la finestra
   fino a `HH:59`: con `end_hour = 4` entrano anche le barre 04:15–04:45, che la fonte non prende.

**Stato in C#.** `EasyEngineBase.InDeclaredWindow` valuta la finestra dichiarata con
`EasyLib.TimeWindowInclusive` sull'orologio della finestra, ed è il percorso primario di `PC`,
`SBO`, `TF_M`, `TF_U`, `RBB_M`, `RBB_U`, `RHL`, `VBO`. **`LevelFaderEngine` confronta ancora le
sole ore** (`WindowParamHhmm(barTime) / 100`): è l'ultimo residuo, e non è attivo perché nessuna
`PTS_*` del catalogo usa `LF`. Se ne portate una, va allineato prima.

Una finestra `0000-2359` **non è un filtro**: è la forma in cui un run che non ha filtrato per ora
scrive i propri orari.

### 3.5 Il giorno della settimana

Due convenzioni, sfasate di un giorno, e nessuna delle due sbaglia: sbaglia chi legge un numero
con l'accessore dell'altra.

| Convenzione | 0 vale | Da dove arriva il numero | Accessore C# |
|---|---|---|---|
| pandas / motore di ricerca | **lunedì** | `skip_day`, `not_entry_day` di `parametri.csv` | `PythonWeekday` |
| `dayofweek()` di EasyLanguage (= .NET) | **domenica** | sorgenti EasyLanguage | `EasyDayOfWeek` |

La regola è che **la convenzione segue la provenienza del numero, non il motore che lo legge**.

E vale il §3.2: il giorno si legge sull'**etichetta** della barra, non sulla sua apertura. Su una
4h cambia su un bucket su sei; su una **giornaliera cambia sempre**, perché apre lunedì a
mezzanotte e chiude martedì a mezzanotte, quindi per la ricerca è martedì.

> **Difetto latente noto in C#.** `DayToFilter` dei motori RBB è letto con `EasyDayOfWeek`. L'unica
> classe che lo valorizza (`PTS_NQ_RBM_001_15`) ci scrive `-1`, quindi oggi è inerte. La prima RBB
> che porti un giorno escluso davvero lo attiva: uno `skip_day = 4` (venerdì per pandas) scritto lì
> escluderebbe il **giovedì**.

### 3.6 I bucket oltre l'ora

Sopra i 60 minuti la griglia la costruisce il codice, mai la piattaforma. Il grafico H4 di cTrader
è ancorato all'orologio del broker; il feed e i run di ricerca all'inizio sessione del giorno di
calendario europeo. Le barre che ne escono sono diverse e nessun errore lo segnala.

`SessionGrid.BucketStartUtc`:

- **fino a 60 minuti** (`60 % tf == 0`) il bucket si calcola **in UTC**, e non è una scorciatoia:
  i fusi del calendario hanno scarti a ore piene, quindi la griglia locale e quella UTC sono lo
  stesso insieme di confini. L'eccezione è l'ora che esiste **due volte** al ritorno dell'ora
  solare, dove il giro attraverso l'orario locale fonderebbe due barre in una. Sui future non si
  vede (il cambio d'ora cade di domenica a mercato chiuso); su BTC, che quota 24/7, sì.
- **oltre l'ora** il confine si calcola sull'orologio locale a partire dall'ancoraggio e si
  riconverte, **mai** sottraendo il resto all'istante UTC. La scorciatoia conta minuti locali su
  un istante UTC e sul salto in avanti dell'ora legale scavalca l'ora che non esiste: la barra che
  apre alle 03:00 locali della domenica di marzo finisce in un bucket etichettato *prima* di
  quello della barra precedente. Misurata su due anni di barre orarie `Europe/Rome`: coincide su
  34.988 righe su 35.088 e diverge sui quattro giorni di transizione.

**Un timeframe è utilizzabile solo se divide il giorno** (`1440 % tf == 0`). Uno che non lo divide
farebbe scivolare i bucket di giorno in giorno rispetto alla sessione, e due run sullo stesso feed
darebbero barre diverse a seconda di dove hanno cominciato.

**I secondi si buttano prima di contare.** Un confine di finestra si porta dietro secondi e
frazioni, e la sottrazione toglie minuti interi: quei secondi sopravviverebbero e il confine
uscirebbe a `inizio bucket + qualche secondo`. Nel cBot questo difetto ha prodotto un bucket monco
ogni blocco di backfill — il 19,7% dei giornalieri e il 3% dei 4h di un archivio raccolto.

---

## 4. Regole di verifica oraria — la checklist

Da eseguire ogni volta che si aggiunge un simbolo, un feed o una strategia. Costa dieci minuti e
copre tutte le divergenze storiche di questo capitolo.

**Sul feed nuovo.**

1. Sommare i volumi per slot orario su qualche centinaio di giorni: il massimo deve cadere
   sull'apertura del cash del mercato di riferimento, letta nell'orologio dichiarato.
2. Cercare il buco fra due barre consecutive: deve cadere sulla pausa di manutenzione nota
   (16:00–17:00 Chicago per gli indici CME).
3. Le due misure devono **concordare**. Se non concordano, il feed non è nell'orologio che si
   crede.
4. **Dopo la conversione a UTC**, ripetere le due misure lette in ora di borsa: non devono
   spostarsi fra gennaio e luglio. Sul feed `@NQ` a 15 minuti la pausa CME cade sugli slot
   16:15–16:45 di Chicago e il picco di volume sulle 08:30, in entrambe le stagioni.

**Sul simbolo nuovo.**

5. Verificare che la coppia start/end dichiarata dalla sorgente EasyLanguage coincida con l'orario
   reale della sua borsa **in quel fuso** — è così che si determina `exchangeTz`, e la coppia deve
   tornare esatta, non approssimata.
6. Prendere `sessionStartHour` dalla tabella §2.4 del dossier, **non** dedurlo dall'intero della
   sorgente EasyLanguage (§3.3).
7. Verificare che `researchTz` abbia scarti da UTC a **ore piene**. Un fuso a mezz'ora (India,
   Nepal, Lord Howe) taglierebbe a metà le barre orarie.

**Sulla strategia nuova.**

8. `start_hour`/`end_hour` copiati verbatim dal `parametri.csv`, nessuna conversione.
9. `skip_day` letto con la convenzione pandas.
10. `intraday_only = 0` dichiarato **esplicitamente** se il motore ha `IntradayOnly = true` per
    default.
11. Se il run ha tagliato le sessioni a un'ora diversa da quella del calendario, non si corregge il
    numero: si dichiara `OverrideSessionAnchor(ora, motivo)` — il motivo è obbligatorio.
12. Rendere ogni finestra **anche in UTC in due stagioni**, gennaio e luglio, e guardare le due.
    Un orario locale è fisso e il suo istante UTC no: una sessione ancorata all'01:00 di Roma
    comincia a mezzanotte UTC d'inverno e alle 23:00 del **giorno prima** d'estate.

**Sul run appena finito.**

13. Confrontare la **distribuzione degli orari di ingresso** con quella del riferimento. Un picco
    su uno slot orario che la fonte non ha è sempre un artefatto, mai un'intuizione.
14. Un `TimeExit` fra le cause di uscita di una strategia dichiarata multiday significa
    `intraday_only` sbagliato.
15. `sellSignals > 0` su una strategia solo long significa che il ramo direzionale in uso non è
    quello che si crede.

---

## 5. Il contratto di esecuzione

Le regole che il simulatore Python e `PiootooTradingService` devono condividere perché due run
sugli stessi dati diano gli stessi trade.

### 5.1 Emissione e validità di un ordine

- La condizione di ingresso è valutata **alla chiusura della barra corrente**.
- L'ordine può essere eseguito **soltanto dalla barra successiva** (`next bar` di EasyLanguage).
- L'ordine vive **una sola barra** e viene **riemesso a ogni barra valida** finché la condizione
  regge e la strategia resta flat. Un rapporto segnali/trade molto alto è quindi normale: 9.969
  segnali per 151 trade è il comportamento corretto, non un difetto.
- In C#: `ValidFromUtc == ExpiresAtUtc == barTime + TimeframeMinutes`, e il segnale dichiara
  `TimeframeMinutes` perché il loop può girare a un timeframe più fitto.

**Attraverso un buco della serie, la barra è quella che arriva, non quella proiettata.**
`ValidFromUtc` nasce come `barTime + timeframe` perché al momento del segnale la barra dopo non
esiste ancora; attraverso un fine settimana, un festivo o una pausa quella proiezione cade nel
vuoto. Il conteggio della vita dell'ordine parte quindi dalla **prima barra vera** su cui l'ordine
è risultato attivo. Senza buchi i due coincidono; con un buco l'ordine attraversa il vuoto e vive
la barra seguente della serie — che è la riga successiva del dataframe.

Quanto pesa: le barre seguite da un buco sono il **20,2%** su `@NQ_1440`, il **15,0%** su
`@FDAX_240`, il 3,9% sui 4h CME, circa il 2% sugli intraday. Sul giornaliero non è una coda: è
sistematicamente il segnale della sessione di **venerdì**, cioè quello che deve operare sul lunedì.

### 5.2 Il fill

| Tipo | Condizione | Prezzo |
|---|---|---|
| stop buy | `bar.High >= livello` | `max(bar.Open, livello)` |
| stop sell | `bar.Low <= livello` | `min(bar.Open, livello)` |
| limit buy | `bar.Low < livello` (**penetrazione stretta**) | `min(bar.Open, livello)` |
| limit sell | `bar.High > livello` (**penetrazione stretta**) | `max(bar.Open, livello)` |
| market | — | `bar.Open` della barra successiva |

Il limit richiede la penetrazione, non il solo contatto. Lo stop si accontenta del contatto.

**Un livello già scavalcato non è un ordine.** Uno stop buy sotto il prezzo corrente non è più il
breakout che la strategia aspettava: è un market al peggiore dei due prezzi. Il cBot lo scarta al
piazzamento; `PiootooTradingService.RejectWrongSideLevels` fa lo stesso, **acceso di default**. La
verifica avviene una volta sola, sulla prima barra su cui l'ordine è attivo, con disuguaglianza
**stretta** (un livello esattamente sull'apertura è il breakout che comincia lì).

> **Per un run di parità con la ricerca questo va spento.** Il motore di ricerca ha la semantica
> TradeStation, dove un ordine `next bar` a un livello già superato si riempie all'apertura.
> `RejectWrongSideLevels = false` riporta i due a coincidere. Il contatore finisce nel log come
> `wrongSideLevelsRejected`: non è un difetto, è la misura di quanti ingressi il backtest
> prenderebbe e il broker no.

### 5.3 Un fill per sessione, **per lato**

`single_entry_per_session` della ricerca è «una entrata per sessione **per direzione**»
(`EngineSignals` in `easy_engine_py/base.py`). Il lato fa parte della chiave:

```
chiave = (strategia, simbolo, LATO, inizio sessione)
```

Sui motori mirrored — `TF_M`, `PC`, `BO`, `VBO`, `RBB_M`, `RHL`, cioè quasi tutto il paniere — le
due gambe nascono sulla stessa barra e sono **due segnali indipendenti**, non un doppione. Senza il
lato nella chiave, il primo fill della sessione spegneva anche la gamba opposta.

**Il limite si applica al fill, non all'invio.** Uno stop non eseguito può essere ripubblicato
nella stessa sessione. Un riferimento che conta gli ordini inviati produce meno ingressi.

L'OCO — un solo ingresso in volo per strategia e simbolo — è un vincolo **diverso**: dopo un fill
si cancellano entrambi gli stop della stessa strategia.

### 5.4 La barra di ingresso: percorso intrabar

Con dati OHLC l'ordine reale dei tick non è ricostruibile. Due convenzioni fisse, da replicare
identiche:

**Percorso della candela.** Rialzista `Open → Low → High → Close`, ribassista
`Open → High → Low → Close`. Un buy stop riempito nel tratto ascendente di una candela rialzista
non può quindi essere chiuso dal suo low: dopo il fill il solo minimo osservabile è il close.
Simmetrico per lo short. In C# sono `postFillLow` e `postFillHigh`.

**Priorità intrabar conservativa.** Se sulla stessa barra sono raggiungibili sia lo stop protettivo
(stop loss, trailing o breakeven) sia il take profit, **chiude allo stop protettivo**. Vale anche
sulla barra di fill.

### 5.5 Le uscite

Tutte dichiarate **all'ingresso**, mai emesse come segnali successivi.

- **Stop loss** — distanza in denaro per contratto. Se la barra **apre** già oltre il livello, si
  riempie all'apertura (gap-aware), come fa da sempre l'ingresso. Vale per lo **stop originale** e
  per nessun altro livello, e **non** sulla barra che ha eseguito l'ingresso.
- **Take profit** — distanza in denaro; il fill è al livello.
- **Breakeven** — quando il movimento favorevole raggiunge la soglia, lo stop si sposta al prezzo
  di ingresso. Da lì il motivo di uscita diventa `BreakEven`, e il gap-aware **non** si applica.
- **Trailing stop** — segue il picco favorevole, ma **a scatti**: il livello si muove solo quando
  il miglioramento vale almeno `TrailingMinStepFraction` della distanza di trailing, default
  **0,10**. È lo stesso passo minimo del cBot. Senza, il primo ritracciamento lo toglieva e
  l'engine era pessimista di un fattore cinque sulle uscite in trailing. Il gap-aware non si
  applica: un trailing può essere nato dall'estremo della barra in corso.
- **`max_bars`** — **conta barre, non tick dell'orologio**. Il contatore avanza solo dove il
  simbolo ha davvero stampato una barra: contare i tick a vuoto (pausa notturna, festivi, fine
  settimana) fa morire la posizione prima delle N barre dichiarate.
- **Uscita di fine sessione** (`intraday_only = 1`) — diventa una deadline `CloseAtUtc` risolta
  **dentro la sessione**, non sul giorno di calendario. Su **D1 non si applica**: è parità col
  motore di ricerca, non una deduzione dal timeframe.

**Sul trailing e le PC.** Sulle strategie a canale del catalogo il trailing è la causa di uscita di
circa un trade su tre e da solo tutto il profitto. Un simulatore che lo modella diversamente non
sta misurando la stessa strategia.

### 5.6 Il fine settimana

**La ricerca non chiude a fine settimana.** Un run di parità va lanciato con
`closeAllPositionsAtWeekEnd: false` (in Piootoo: `AllowOverweek` sul piano) per non introdurre una
chiusura che la fonte non ha.

E il fine settimana **non si salta per calendario**: la sessione della ricerca è il giorno di
calendario europeo, quindi la riapertura del lunedì cade alle 22:00 (ora legale) o 23:00 (ora
solare) UTC di **domenica**. Saltare sabato e domenica UTC toglie l'apertura di ogni lunedì — il
**21,0%** delle barre di `@NQ_1440` sono lunedì timbrati domenica — e su BTC due giorni pieni a
settimana.

---

## 6. I costi, e dove si applicano

| Voce | Ricerca | Piootoo | Nota |
|---|---|---|---|
| Commissioni | $2 per contratto per lato | `commissionPerContract = 2`, addebitata a ingresso **e** uscita | $4 round-turn: **coincidono** |
| Slippage sui fill stop | **1 tick** | **zero**, e non esiste un parametro nel motore di esecuzione | rettifica manuale: su NQ $10 per trade (1 tick da $5 per lato) |
| Spread | non modellato | misurato per simbolo e per ora UTC, applicato al **solo prezzo di ingresso** | va spento per un confronto |
| Allargamento dello stop | non esiste | `StopMoneyPolicy`, selettivo per strategia | va spento per un confronto |

### 6.1 Lo spread è una misura, non un parametro

Piootoo carica la distribuzione misurata da `piootoo-repository/spread/{BROKER}/`, sceglie fra
mediana/media/p90, e sceglie fra la costante per simbolo e il valore dell'ora UTC dell'ingresso.
`ApplySpread` peggiora l'ingresso (long `+s`, short `-s`) e **non tocca trigger, livelli né
uscite**: il feed di `datafeed-external/` è la serie **Bid** di cTrader, quindi un long entra
sull'Ask e esce sul Bid.

L'effetto non è un costo per trade ma *stessa perdita, più stop*: stop e target si spostano insieme
all'ingresso. Il numero che lo misura è `spread / distanza di stop`.

**Il trigger dei pending resta sul feed.** Muoverlo cambierebbe quali trade nascono e i run non
sarebbero più confrontabili con il porting dalla ricerca.

### 6.2 L'allargamento dello stop — la divergenza voluta

`StopMoneyPolicy` moltiplica la **sola** distanza di stop di alcune strategie, applicato in un
punto solo (`StatelessEasyStrategyBase.EnrichSignal`, l'unico passaggio che ogni ingresso
attraversa). Stato attuale nel codice: interruttore **acceso**, fattore sullo stop **2**, fattore
sul target **1**, elenco di **33 codici** `PTS_*`.

Perché esiste: lo spread misurato è più largo della distanza di stop dichiarata su parecchie
strategie portate, e uno stop così stretto non è eseguibile su un conto vero. Perché è selettivo:
applicato a tutto il catalogo peggiora l'equity complessiva e raddoppia il drawdown — su 96
strategie con trade, 56 peggiorano e 31 migliorano.

**Per il simulatore Python questa è una deviazione consapevole dal riferimento.** Le classi `PTS_*`
continuano a dichiarare i numeri della ricerca **verbatim**; l'allargamento avviene a valle. Un run
di parità va fatto con `StopMoneyPolicy.Enabled = false`, altrimenti si confrontano due strategie
diverse.

Chi dichiara il fattore negli artefatti usa `EffectiveMultiplier`, **mai** `Multiplier`, e ci mette
**accanto l'elenco**: il solo fattore non dice a quali strategie è stato applicato, e due run che
ne allargano di diverse sembrerebbero identici.

> **Nota di manutenzione.** Il `CLAUDE.md` del repository di codice dichiara ancora
> `Multiplier = 3`; il sorgente dice `2`. Vale il sorgente.

### 6.3 La conversione denaro → punti

Stop, target, breakeven e trailing sono dichiarati dalla strategia in **dollari per contratto di
riferimento**, esattamente come `setstopcontract` + `setstoploss(N)` dell'originale. La conversione
in punti avviene una volta sola nel confine di esecuzione:

```
punti = denaro / PointValue(simbolo)
```

La strategia non sa, e non deve sapere, se sta girando su future, mini, micro o CFD. Su NQ, con $20
per punto, `stop_loss = 250` sono 12,5 punti e `take_profit = 5000` sono 250 punti.

---

## 7. Cosa è confrontabile, e cosa no

**I prezzi assoluti no.** I run Python girano su contratti continui **back-adjusted**: NQ vale
4.588 nel 2012 e il datafeed Piootoo ne dà 18.000 sullo stesso strumento. Non è un errore di
nessuno dei due — è il retro-aggiustamento dei rollover — ma rende inutile qualunque confronto sui
livelli, e con esso su stop e target espressi in **punti**.

**Restano confrontabili:**

| | |
|---|---|
| timestamp di ingresso | quanti in comune, quanti solo da un lato |
| direzione | long/short sullo stesso ingresso |
| P&L in dollari | invariante al back-adjustment |
| causa di uscita | SL / TP / MAXBARS / trailing / breakeven / … |
| distanze | SL, TP, trailing sono in dollari per contratto, quindi invarianti |
| prima divergenza | il punto da cui i due sistemi smettono di concordare |

**Come si legge una divergenza.** Una divergenza che parte da un punto preciso e poi non recupera
più è un problema di **stato** (una posizione aperta da uno e non dall'altro). Una divergenza
diffusa e simmetrica è un problema di **dati** o di **condizione d'ingresso**.

**Il vincolo che decide se il confronto ha senso** è la copertura del feed. Il datafeed `NQ` 15m
copre `2006-01-03 → 2025-05-30`. Il primo controllo è sempre `firstBarUtc`/`lastBarUtc` del
datasource contro il primo e l'ultimo trade del riferimento: su periodi che non si sovrappongono,
una differenza di net profit non dimostra niente.

### 7.1 La procedura

1. **Verificare quale codice ha prodotto il run.** È l'errore che costa più tempo, perché non
   lascia tracce: `generatedAtUtc` nel `backtest-summary.json` contro la data di modifica del
   sorgente della strategia. Un run di stamattina su un file toccato dieci minuti dopo mostra il
   comportamento di una versione che non esiste più.
2. **Allineare l'intervallo richiesto alla copertura del datasource**, e trattare
   `coversRequestedRange: false` come un blocco. Chiedere un intervallo più lungo del feed non fa
   fallire il run ma genera tick vuoti.
3. **Leggere il `backtest-summary.json` prima dei trade**: valutazioni, segnali per tipo, trade,
   cause di uscita. Tre incoerenze si vedono solo lì (§4, punti 14–15).
4. **Validare i fill contro il feed.** Ogni timestamp di ingresso deve avere una barra nel feed, e
   i prezzi di ingresso e uscita devono cadere dentro il `[low, high]` di quella barra, con
   l'eccezione delle chiusure tecniche. Un backtest con fill fantasma non è confrontabile con
   niente.
5. **Allineare i timestamp** cercando l'offset che massimizza le corrispondenze, provando passi di
   un timeframe e non solo di un'ora. Un massimo a un timeframe esatto è la conferma
   dell'etichettatura, non un fuso orario.
6. **Ricalcolare il livello di trigger** sul feed Piootoo per ogni trade del riferimento e
   confrontarlo con `entry_price`. L'atteso è `livello + 1 tick` per lo slippage della fonte.
7. **Confrontare gli aggregati** applicando la rettifica di slippage.
8. **Confrontare la distribuzione degli orari di ingresso** (§4, punto 13).

Lo strumento esiste: `docs/parita-python-csharp/parita.py` nel repository di codice fa i passi 5–8,
con `--offset auto` e `--tolleranza`. Ha un autotest: due run Python identici devono dare
corrispondenza totale.

### 7.2 Configurazione di un run di parità

Un backtest lanciato per confrontarsi con la ricerca deve spegnere tutto ciò che Piootoo aggiunge
e la ricerca non ha:

| Impostazione | Valore per la parità | Perché |
|---|---|---|
| `closeAllPositionsAtWeekEnd` | `false` | la ricerca non chiude a fine settimana |
| `RejectWrongSideLevels` | `false` | la ricerca ha la semantica TradeStation |
| `StopMoneyPolicy.Enabled` | `false` | l'allargamento non esiste nella ricerca |
| `SpreadBroker` | non impostato | lo spread non è modellato nella ricerca |
| `DatafeedBroker` | non impostato (feed interno) | il feed del broker ha prezzi diversi |
| `PlanCode` | non impostato (run neutro) | un piano sovrascrive holding e commissioni |
| `commissionPerContract` | `2` | coincide già con la ricerca |
| `TrailingMinStepFraction` | da verificare contro il simulatore | se il simulatore non ha passo minimo, `0` |
| `LegacyBarOpenLabels` | `false` | è il comportamento corretto |

E le strategie da confrontare vanno messe **nello stesso run**: così barre e assunzioni sono
identiche per costruzione.

---

## 8. Divergenze note e ancora aperte

Uno scarto non è di per sé un bug del porting. Ma le cause note sono finite, e servono a decidere
se lo scarto è spiegato da quelle o da qualcos'altro.

1. **Slippage.** Il riferimento applica 1 tick sui fill stop, l'engine zero, e non esiste un
   parametro. La rettifica è manuale: ~$10 per trade su NQ. **Aperta per scelta**: aggiungerlo al
   livello cambierebbe *se* il breakout scatta, non solo a che prezzo viene riempito.
2. **`LevelFaderEngine` confronta le sole ore.** Nessuna `PTS_*` usa `LF` oggi, quindi è inerte.
3. **`DayToFilter` degli RBB letto con la convenzione EasyLanguage** (§3.5): latente, non attivo.
4. **Granularità del feed a parità di timeframe nominale.** Due feed «15 minuti» possono avere un
   numero di barre al giorno diverso per sessioni, festivi o buchi. Per una strategia a canale la
   conseguenza è diretta: un Donchian di 100 barre copre una finestra temporale diversa nei due
   sistemi, quindi i livelli di trigger non sono gli stessi. Sul caso `PTS_NQ_PCH_001` la serie è
   la stessa e i livelli ricalcolati coincidono, ma resta un **18%** di trade in cui il massimo di
   canale differisce.
5. **Etichetta di chiusura sulla finestra operativa.**
   `BacktestingRequest.ResearchWindowOnBarClose` esiste per confrontare `apertura + timeframe`, ma
   quanto valga sul paniere **non è ancora stato misurato**.
6. **Strategie a timeframe superiore al minimo del run** sono valutate sull'orologio sintetico e
   non sui confini reali della loro barra. `ClockTimeframeMinutes` a un minuto riduce l'errore al
   minuto, non lo chiude.

### 8.1 Baseline storica — da rimisurare

Caso `PTS_NQ_PCH_001` / `PTS_NQ_PCH_002`, periodo 2012–2025, un contratto, commissioni $4
round-turn:

| | Report | Piootoo | Piootoo con slippage 1 tick |
|---|---|---|---|
| PCH_001 trade | 925 | 1.084 | 1.084 |
| PCH_001 net | $204.200 | $165.909 | $155.069 |
| PCH_002 trade | 691 | 816 | 816 |
| PCH_002 net | $170.281 | $104.666 | $96.506 |

> ⚠ **Questi numeri sono anteriori a correzioni successive** — l'offset del canale (il tick
> implicito, rimosso), la formula di `pattern_neutral(47)` (divideva per 3 invece che per 2), i
> fill fantasma, l'etichettatura delle barre. Non è più una baseline valida: va rimisurata prima di
> usarla per giudicare un porting nuovo. Resta utile come ordine di grandezza.

---

## 9. Riferimenti codice

**Tempo e calendario**

- `Piootoo.Shared/Configuration/SessionClock.cs` — conversione di fuso, cache dell'offset, giorni
  di cambio d'ora, `BarLabelHhmm` / `BarLabelDay` / `BarLabelUtc`
- `Piootoo.Shared/Configuration/ZonedWindow.cs` — `ResearchSession`, `ResearchHours`, `Exchange`,
  e perché un simbolo ha due orologi
- `Piootoo.Shared/MarketData/MarketCalendar.cs`, `SessionGrid.cs` — `SessionDayOf`,
  `SessionOpenUtc`, `BucketStartUtc`
- `piootoo-repository/marketdata/market-calendars.json` — il dato, per simbolo

**Motori di strategia**

- `Piootoo.Strategies/Easy/Engines/EasyEngineBase.cs` — `ParamHhmm`, `WindowParamHhmm`,
  `InDeclaredWindow`, `PythonWeekday`, `EasyDayOfWeek`, `EntryStopNextBar`, `ResolveCloseAtUtc`
- `Piootoo.Strategies/Easy/EasyLib.cs` — `ClassifySessionBar`, `OHLCMulti5`, `BuildSessionSeries`,
  `TimeWindow`, `TimeWindowInclusive`, `EstimateNextBarUtc`, le librerie pattern
- `Piootoo.Strategies/Easy/StatelessEasyStrategyBase.cs` — `Evaluate`, `EnrichSignal`
- `Piootoo.Strategies/Easy/Engines/*.cs` — i dodici motori
- `Piootoo.Strategies/PiutooStrategies/PTS_*.cs` — il catalogo

**Esecuzione**

- `Piootoo.Core/Services/PiootooTradingService.cs` — `TryFillPendingOrders`, `CanExecuteOnBar`,
  `IsExpired` / `IsPendingExpired`, `IsWrongSideLevel`, `ResolveFillPrice`, `ApplySpread`,
  `CheckStopLossAndTakeProfit`, `ProtectiveFillPrice`, `MakeEntrySessionKey`
- `Piootoo.Core/Services/PiootooBacktestingService.cs` — loop, `BelongsToCurrentTick`,
  `IterationIsSkippedByWeekEndFlat`, `IsStrategyCandleStale`
- `Piootoo.Shared/Configuration/StopMoneyPolicy.cs` — l'allargamento e il suo elenco
- `Piootoo.Shared/Models/TradeSignal.cs` — il contratto del segnale

**Ricerca**

- `piootoo-repository/easy_engine_py/*.py` — i motori di riferimento
- `piootoo-repository/datafeed-future/aggregate_flat_feed.py` — generazione del feed piatto,
  bucket e conversione di fuso
- `piootoo-repository/run-engine/` — i run, `parametri.csv`, `top_final.json`, `trades/*.csv`

**Documenti di dettaglio** (nel repository di codice, `docs/domini/`)

- `orari-di-sessione-e-fusi.md`, `porting-da-report-sweep.md`, `motori-strategie.md`,
  `orologio-barre-e-fill.md`, `parita-riferimento-esterno.md`, `layer-barre-e-calendario.md`,
  `spread-e-costo-di-transazione.md`, `datafeed-generazione.md`
