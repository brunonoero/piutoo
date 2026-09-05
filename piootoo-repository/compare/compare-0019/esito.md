# compare-0019 — i difetti del lato cBot, separati da quelli del motore interno

Analisi del 2026-09-04. È **lo stesso run del cBot di compare-0018** (`SessionId
b974e64be16e4bdf9ce6efe45fa7b533`), proseguito: ora copre 01/07/2025 → **10/02/2026** con 2.597
trade invece di 548. La gamba interna è **byte per byte la stessa di compare-0018**.

## ⚠ La gamba interna è quella *prima* del fix del trailing

`run-interno-cfd-FTMOPLATFORM.json` dichiara **motore 5.1.1** e il riepilogo dichiara
`"trailingPeakIncludesCurrentBar": true`. È il run analizzato in compare-0018 §2, in cui **221
uscite in trailing dentro la prima barra valgono +183.003 USD**, cioè profitti su percorsi mai
esistiti. Il difetto è stato corretto il 04/09 (motore **5.1.2**), ma **non è in questi dati**.

Quindi il divario misurato qui — INT **+405.969** contro EXT **−473.488** nella finestra comune,
**879.457 USD** — è **sovrastimato di circa 183.000 USD**, e la parte di quel divario che passa
dalle uscite protettive è in buona misura il difetto interno, non il cBot. **Prima di decidere
qualunque intervento sul cBot va rifatto il run interno con 5.1.2.**

Detto questo, quello che segue è separabile: sono difetti che non dipendono da come l'engine
interno chiude le posizioni.

## 1. RISOLTO: **non è il cBot.** Il segnale si perde nel server, fra `Evaluate` e l'intent

Il `session-summary.json` e `signals.json` della sessione `b974e64b…` dicono la stessa cosa da due
strade indipendenti:

| strategia | `everEvaluable` | intent emessi | intent riempiti | record in `signals.json` |
|---|---|---|---|---|
| `PTS_FDAX_VBO_001_240` | **true** | **0** | 0 | **0** |
| `PTS_NQ_VBO_002_240` | **true** | **0** | 0 | **0** |
| `PTS_NQ_VBO_001_1440` | **true** | **0** | 0 | **0** |
| `PTS_HK_BIA_001_15` | **true** | **0** | 0 | **0** |
| `PTS_HO_BIA_003_60` | **true** | **0** | 0 | **0** |

**Il cBot non ha mai ricevuto niente da eseguire.** Zero intent emessi, non zero riempiti: nessuna
strategia del run è nello stato «intent emessi e zero riempiti» (`intentsEmitted > 0 &&
intentsFilled == 0` è vuoto su tutte e 111).

### Il riscaldamento è definitivamente escluso

Tutti e 36 gli stream chiudono con `historyBarsHighWater ≥ requiredCandles` e
`strategiesNeverEvaluated = 0`. In particolare **FDAX/240: 1.856 barre contro le 501 richieste** e
**NQ/240: 1.991 contro 606**. La pista aperta in compare-0017 §5.1 e compare-0018 §6 è chiusa: le
VBO erano valutabili per tutto il run.

### Le VBO emettono segnali se le si chiama come le chiama la sessione

Rigirate sul feed vero con **la stessa finestra della sessione** (`RequiredCandles × 1,2`, che è
identica a quella del backtest) e uno snapshot di esecuzione vuoto:

| strategia | segnali non-Hold |
|---|---|
| `PTS_FDAX_VBO_001_240` | **666** |
| `PTS_NQ_VBO_002_240` | **172** |
| `PTS_NQ_VBO_001_1440` | **40** |
| `PTS_FDAX_PCH_001_240` (controllo) | 611 |

Quindi il segnale **nasce**. Si perde fra `StrategyEvaluationService.Evaluate` e la creazione
dell'intent, cioè **dentro il server**, non sul bot e non nel motore della strategia.

### L'ipotesi dell'etichetta di chiusura è morta

Spostando tutte le barre di un timeframe — come se il feed le etichettasse in chiusura invece che in
apertura — i conteggi non crollano: 666 → 803, 172 → 188, 40 → 40. Non è quello.

### `PTS_HO_BIA_003_60` è un caso diverso

Zero segnali **anche fuori dalla sessione**, su 6.479 valutazioni. Non è infrastruttura: è la
strategia che non scatta mai su questo feed, e la domanda è di porting. Da confrontare con la
propria lista di trade di riferimento. Probabilmente vale lo stesso per `PTS_HK_BIA_001_15`.

### CAUSA TROVATA E CORRETTA (motore 5.1.3)

`TradingSessionService.GetExecution`, ramo `ExternalBroker`, passava:

```
EntriesToday = session.Entries
```

`session.Entries` è il contatore di **tutta la sessione su tutte le strategie**, incrementato a ogni
fill che apre una posizione esterna. Ma il campo che riempie —
`StrategyExecutionSnapshot.EntriesToday` — la strategia lo legge come *«quanti ingressi ho fatto io
oggi»*, e `VolatilityBreakoutEngine` ci mette sopra un gate diretto:

```
if (!InPythonTradingWindow(barTime) ||
    PythonDayOfWeek(barTime) == SkipDay ||
    (MaxEntriesPerSession > 0 && EntriesTodayCount >= MaxEntriesPerSession) || ...)
    return Hold(bar.Close, barTime);
```

Tutte e tre le VBO del paniere dichiarano `MaxEntriesPerSession = 1`. Quindi **al primo fill della
sessione — di qualunque strategia — le tre VBO passano in Hold e non ne escono più.** La sessione ha
avuto 3.947 fill; il primo è del 01/07/2025. Sono state mute dal primo giorno.

Il backtest non ne soffre perché `PiootooTradingService.GetExecutionSnapshot` conta per **chiave di
posizione** e per **giorno**. Era l'unica differenza fra i due percorsi, ed era sufficiente.

**Corretto**: la sessione tiene `EntriesByStrategyDay` con la stessa semantica del backtest,
incrementato dove si incrementava `Entries`. Riguarda anche
`MovingAverageCrossoverEngine.MaxEntriesPerDay` e `TrendDeveloperEngine.MaxTradesPerDay`, che
leggono lo stesso contatore. Build verde, 43 test falliti = baseline invariato; **il fix non ha
ancora un test dedicato** (servirebbe uno scenario a due strategie in `SessionEntryLimitTests`).

Restano fuori da questa causa `PTS_HK_BIA_001_15` e `PTS_HO_BIA_003_60`: il BIAS non legge
`EntriesTodayCount`, e `PTS_HO_BIA_003_60` non emette segnali nemmeno fuori dalla sessione. Sono una
domanda di porting.

## 2. Certamente asimmetrico — l'uscita per segnale opposto non esiste fuori dal backtest

`TradeExitReason.OppositeSignal` **non compare in `TradingSessionService`** e il cBot non ha nessun
concetto equivalente: il commento del suo DTO di uscita dice *«Specifica di uscita completa: è
l'unica informazione con cui il bot chiude la posizione»*, e quella specifica contiene stop, target,
`CloseAtUtc` e `MaxBarsInPosition` — non il segnale opposto.

Il backtest interno chiude **130 posizioni** così. Nelle coppie appaiate, **31 finiscono in una
protettiva sul cBot, per +46.138 USD** di divario.

Non è un difetto del bot: è una regola del motore interno **senza controparte nel contratto**. Va
deciso da che parte stare — o il server emette una chiusura, o il backtest smette di applicarla.

## 3. Da attribuire — le uscite a tempo e per numero di barre

`CloseAtUtc` e `MaxBarsInPosition` **sono implementati nel bot** (chiude quando `closeAt <= nowUtc`
o `BarsInPosition >= MaxBarsInPosition`), e infatti **152 coppie** `tempo → Closed` funzionano. Ma
restano **19 coppie `tempo → protettiva`** (+56.844) e **5 `maxbars → protettiva`** (+19.291), cioè
posizioni che l'engine chiude a scadenza e il bot lascia correre fino allo stop.

Il sospetto è la frequenza di valutazione: il bot controlla la scadenza sui propri tick, e su uno
stream lento la finestra fra la scadenza e il controllo può contenere lo stop. Da misurare sul
ritardo di uscita, non da assumere.

## 4. NON è un difetto del cBot — gli ingressi nel fine settimana

127 ingressi di sabato o domenica (124 BTC, 3 HO) per **−51.494 USD**, contro **zero** dell'interno.

Ma il piano `FTMO-ALL` dichiara `allowOverweek: true`, e il bot legge quel campo: `EnforceWeekEndFlat()`
esce subito quando `_allowOverweek` è vero, e la guardia sugli ingressi è `if (!_allowOverweek &&
IsWeekEndFlatWindow(...))`. **Il bot sta rispettando il piano.**

Il difetto vero è che **l'interno, con lo stesso piano e lo stesso feed, non entra mai nel
weekend**: i due motori non sono d'accordo su quali barre siano operabili. E nessuno dei due è
fedele alla ricerca, che sul BTC dà **sabato = 0 e domenica ≈ 7% delle settimane** (§2.1.1 del
dossier) mentre il feed CFD ne offre 35 e 34 su 35. È la voce C3 di compare-0017, ed è quasi
certamente anche la spiegazione del **1,90× di `PTS_BTC_BIA_001_60`**: il BIAS fa una entrata per
sessione e per direzione, e sta ricevendo sessioni in più.

## 5. Il rovescio — `PTS_SB_TFM_001_240` opera sul cBot e non sull'interno

5 trade sul cBot, **zero** sull'interno, ed è l'unica strategia in questa condizione. L'interno
emette 601 segnali e non riempie mai, perché `@SB_240` ha tre barre al giorno e la barra su cui
l'ordine dovrebbe vivere arriva il giorno dopo, oltre `ExpiresAtUtc + 240 min` (compare-0017 §3.4).
Sul conto vero quegli ordini si riempiono. **Qui è il backtest a perdere trade che esistono.**

## 6. Da rimisurare dopo il fix — le uscite protettive

Sulle coppie in cui entrambi escono su una protettiva, la distanza realizzata dall'ingresso:

| sym | n | INT | EXT | extra EXT | in USD |
|---|---|---|---|---|---|
| HO | 60 | −0,0143 | −0,0300 | −0,0157 | **−659** |
| GC | 133 | −5,000 | −6,820 | −1,820 | **−182** |
| NG | 39 | −0,100 | −0,103 | −0,003 | −30 |
| YM | 119 | −50,00 | −54,18 | −4,18 | −21 |
| BTC | 153 | −50,00 | −54,07 | −4,07 | −20 |
| FDAX | 83 | −10,00 | −10,69 | −0,69 | −17 |
| KC | 26 | −0,667 | −0,710 | −0,043 | −16 |
| NQ | 575 | −25,00 | −25,50 | −0,50 | −10 |

**Attenzione a come si legge.** La colonna INT contiene anche le uscite in trailing fabbricate del
§⚠, che escono *sopra* l'ingresso e abbassano la mediana: il confronto non è pulito finché il run
interno non viene rifatto con 5.1.2.

Quello che invece è pulito è il cBot contro **se stesso**: distanza realizzata contro distanza
**dichiarata** nell'intent, mediana della differenza per trade — NQ +0,30, YM +0,68, FDAX +0,52,
GC +0,13, BTC +2,21, NG 0,00, CL 0,00, HK +0,14. **Il cBot riempie onestamente**, quarta conferma
dopo 0016, 0017 e 0018. E i **23,5 punti di YM** annotati in compare-0018 §3 erano un artefatto del
campione corto: su 119 coppie invece di 17 il numero è 4,18.

## 7. Le taglie sono giuste

Rapporto EXT/INT del denaro per punto, per simbolo: 0,1000 esatto su BTC, CC, CL, ES, GC, HO, NG,
NQ, PL, YM; 0,0995 su HK; 0,0987 su KC; 0,0960 su BP; 0,1158 su FDAX (= il cambio EUR/USD).
Compreso **HO a 4.200 contro 42.000**, quindi il moltiplicatore corretto è arrivato. Nessun
problema di sizing.

## Ordine di lavoro

1. **Rifare il run interno con 5.1.2.** Senza, i §6 e metà del divario non sono interpretabili, e si
   rischia di correggere il cBot per un difetto che è dell'engine.
2. **Chiudere normalmente la prossima sessione del cBot** e leggere `session-summary.json`: è
   l'unica cosa che attribuisce il §1 (cinque strategie mai eseguite, +46.966 solo su FDAX_VBO).
3. **Decidere il segnale opposto** (§2): o entra nel contratto, o esce dal backtest. Sono 130 trade
   interni.
4. **Il calendario di sessione** (§4): finché il feed CFD offre sette sessioni a settimana dove la
   ricerca ne ha cinque, BTC diverge da entrambe le parti in modo diverso.
5. **Misurare il ritardo di uscita** sulle 24 coppie `tempo`/`maxbars → protettiva` (§3).
6. **`PTS_SB_TFM_001_240`** (§5): o si ripara `@SB_240`, o la strategia esce dal paniere. Oggi il
   backtest la dà a zero e il conto vero no.

## Trappole di misura di questa cartella

- **La gamba interna è la stessa di compare-0018** (stesso md5): non è un run nuovo, ed è
  precedente al fix del trailing.
- **Il run del cBot è ancora in corso**: copre 01/07/2025 → 10/02/2026 contro i dieci mesi
  dell'interno. Va tagliato.
- **La distanza realizzata dell'interno non è confrontabile** finché contiene le uscite in trailing
  fabbricate. Il confronto pulito è cBot contro la propria distanza dichiarata.
- **`LocalExit:StopLoss` del cBot copre stop, trailing e break-even**: confrontare la distanza
  realizzata con lo `stopLoss` dichiarato dà differenze negative sui trade usciti in break-even, e
  non è un errore di riempimento.
