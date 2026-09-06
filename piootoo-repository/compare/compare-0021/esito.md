# compare-0021 — cBot FTMO contro backtest interno sul feed FTMOPLATFORM

Analisi del 2026-09-05. Nessun report HTML: i numeri qui sotto sono ricavati dai
quattro artefatti della cartella.

**In una riga:** il divario non e' spread. Lo spread pesa il **4%**; il resto e'
un **bug del server** (57%), due **serie di barre diverse** sugli stream oltre
l'ora (fino al 52% dei trade), e un **riscaldamento assente** sul feed interno.

## Le due gambe

| | sinistra | destra |
|---|---|---|
| file trade | `trades-cbot-cfd-FTMO.json` | `trades-interno-cfd-FTMOPLATFORM.json` |
| slug | `cbot-cfd-FTMO` | `interno-cfd-FTMOPLATFORM` |
| motore | `PiootooDistributedExecutionBot` in cTrader (backtester), server in `ExternalBroker` | `PiootooTradingService` |
| barre | serie del backtester cTrader, bucket oltre l'ora costruiti dal bot | `datafeed-external/FTMOPLATFORM/` |
| piano | `FTMO-ALL`, conto 17188650, capitale 100.000 | `FTMO-ALL`, capitale 1.000.000 |
| arco ingressi | 2025-07-02 03:12 → 2025-08-30 15:20 | 2025-07-02 09:00 → 2025-08-29 13:00 |
| trade | 101 | 97 |
| netto grezzo | +640,51 (valuta conto) | +46.136,82 |

Il broker e' lo stesso: `FTMO` e' il campo `Broker` del conto, `FTMOPLATFORM` la
cartella sotto `datafeed-external/`. Entrambe le gambe girano sul piano `FTMO-ALL`,
13 strategie abilitate su 111 di catalogo, stesso `holding` (overnight e overweek
permessi, flat 20:45, fine settimana 20:45→23:00), stesse convenzioni di fill.

## Esito

Le size **non** sono confrontabili grezze: `accountBalanceScale = 0,1` sul lato
cBot (conto 100k contro capitale 1M), leggibile in `signals.json` e confermato dal
valore per punto implicito — cBot 1 USD/pt per lotto su NQ e BTC, interno 20 e 5 per
contratto. Il fattore e' **esattamente 10** su tutti i simboli.

Riportando il cBot alla scala interna (× 10), sulla finestra comune:

| | trade | netto |
|---|---|---|
| interno | 97 | **+46.136,82** |
| cBot × 10 | 101 | **+6.405,10** |
| divario | | **39.731,72** |

Non e' deriva costante: il cBot resta sotto zero fino a W31, recupera con due
settimane sole (W31 +9.604, W34 +7.572) e chiude a +6.405. L'interno sale da W29 in
poi. Le due curve divergono da **W29** (interno +17.709, cBot −1.083) e non si
riavvicinano piu'.

## Scomposizione

| causa | quota del divario | valore |
|---|---|---|
| **1. `EntriesToday` sbagliato in `ExternalBroker`** — `PTS_FDAX_VBO_001_240` muta dal secondo giorno | **57,4%** | 22.824 |
| **2. Griglia H4/D1 diversa fra archivio e sessione** — 8 strategie su 13 | fino a **52%** dei trade | ±20.804 netti |
| **3. Riscaldamento assente sul feed interno** — NQ e BTC ciechi 8-10 giorni | ~10% | ~3.937 |
| **4. Spread e slippage** — costo diretto sugli stop | **4,2%** | 1.687 |
| 5. Coda: 3 strategie sopravvivono a tutto, saldo ±0,5% | — | ~500 |

Le cause 1-3 si sovrappongono (VBO sta anche sulla griglia sbagliata), quindi le
quote non sommano a 100: 1 e' certa e isolata, 2 e 3 delimitano il resto.

### 1. Il server mostra alle strategie il numero sbagliato di ingressi

`TradingSessionService.GetExecution` (riga ~4128) passa `EntriesToday = session.Entries`.
`session.Entries` e' un contatore **di sessione**: si incrementa a ogni riempimento di
**qualunque** strategia su **qualunque** simbolo (riga 1819) e **non si azzera mai**, ne'
per giorno ne' per strategia. Il backtest fa un'altra cosa —
`PiootooTradingService.GetExecutionSnapshot` conta per `positionKey` (simbolo|strategia)
**e per giorno**:

```csharp
var day = TradingDateTime.ToFeedUtc(barTimeUtc).Date;
if (_entriesByDay.TryGetValue(positionKey, out var tracked) && tracked.Day == day)
    entriesToday = tracked.Count;
```

Una sola strategia del piano ferma se stessa su quel numero:
`PTS_FDAX_VBO_001_240`, tramite `VolatilityBreakoutEngine:200`
(`MaxEntriesPerSession > 0 && EntriesTodayCount >= MaxEntriesPerSession`, con
`MaxEntriesPerSession = 1`). Il **primo riempimento della sessione** e'
`PTS_BTC_BIA_001_60` il 2025-07-02 alle 03:12: da quell'istante `session.Entries >= 1` e
VBO restituisce `Hold` per i restanti 60 giorni.

Riscontro: `session-summary.json` da' VBO `everEvaluable: true`, `intentsEmitted: 0`;
`backtest-summary-interno` da' 44 segnali e 12 trade, **+22.824,32**, il singolo
contributo positivo piu' grande dell'intero run interno.

Non tocca le altre dodici: `PTS_FDAX_PCH_001_240` dichiara `MaxEntriesPerSession = 1`
ma `PriceChannelEngine` lo **propaga** sul segnale (riga 280) senza mai leggere
`EntriesTodayCount`, e `PTS_FDAX_MAC_001_240` ha `MaxEntriesPerDay = 0`. Le altre
non dichiarano cap. Un piano con piu' strategie VBO — o con `MovingAverageCrossoverEngine`
/ `TrendDeveloperEngine` a cap acceso — perderebbe tutto in blocco allo stesso modo.

### 2. Le due gambe non leggono le stesse barre oltre l'ora

Timestamp delle barre, luglio-agosto 2025:

| stream | archivio `datafeed-external/FTMOPLATFORM` | sessione (bot operativo) |
|---|---|---|
| FDAX 240 | 01/05/09/13/17/21 UTC = **03,07,11,15,19,23 locali** | 02/06/10/14/18/22 UTC = **00,04,08,12,16,20 locali** |
| FDAX 1440 | 21:00 UTC = **23:00 locale** | 22:00 UTC = **00:00 locale** |
| NQ 1440 | 21:00 UTC = **23:00 locale** | 22:00 UTC = **00:00 locale** |
| NQ 15/30/60, BTC 60 | griglia UTC nativa | griglia UTC nativa — **identiche** |

**La gamba sbagliata e' l'archivio, non la sessione** (corretto il 2026-09-05 dopo
l'audit del feed: la prima stesura di questa scheda diceva il contrario). Il feed futures
del vendor — `datafeed/@FDAX_240.json`, `@NQ_240.json` — apre le barre a **00/04/08/12/16/20
locali**, cioe' l'ancoraggio a mezzanotte europea di `aggregate_flat_feed.py`. La sessione
del cBot operativo produce **la stessa griglia**. L'archivio no: e' ancorato all'orologio
del **broker**, ed e' l'unica delle tre a non essere confrontabile con la ricerca.

Il motivo e' la versione del raccoglitore. `PiootooDatafeedSyncBot` **1.1.0** prendeva le
serie oltre l'ora dalla piattaforma; la **1.2.0** (commit `aaec722`) le costruisce dalle
barre da 60m con `NativeCeilingMinutes = 60` e le dichiara nel campo `source`. Tutti i file
di questo archivio dicono `PiootooDatafeedSyncBot/1.1.0@FTMO Platform`, senza il suffisso
`griglia(60m->240m, Europe/Rome 00:00)` che la 1.2.0 aggiunge. **Il codice e' gia' corretto:
l'archivio e' vecchio.**

Quanto sono lontane le due griglie lo dice il controllo piu' severo: rifacendo i bucket a
240m dalle barre da 60m dello **stesso** archivio, sull'ancoraggio della ricerca, gli
istanti in comune con il 240m raccolto sono **zero** — su NQ, ES, GC, YM, NG, HO e BTC. Non
una barra su 1.812. Vedi `audit-feed-FTMOPLATFORM.md`.

E' la trappola descritta in `CLAUDE.md` §"Oltre l'ora la griglia la costruisce il codice",
qui realizzata **fra il raccoglitore vecchio e tutto il resto**. Nessun errore la segnala.

Otto strategie su tredici stanno su quegli stream (FDAX 240: MAC, PCH, SBO_001, VBO;
FDAX 1440: SBO_002, TFU_001, TFU_002; NQ 1440: TFU_007). Diviso per famiglia di griglia:

| | trade int | netto int | trade cBot | netto cBot ×10 | divario |
|---|---|---|---|---|---|
| griglia UTC nativa (≤60m) | 51 | 18.680 | 70 | −248 | 18.928 (48%) |
| griglia costruita (>60m) | 46 | 27.457 | 31 | 6.653 | 20.804 (52%) |

Su meta' del divario **le barre stesse sono diverse**: non e' un confronto fra motori.
Per VBO conta doppio — il livello e' `apertura di sessione ± k × ATR(500)`, e spostare
l'ancoraggio di un'ora cambia sia `O_d0` sia i pattern d0/d1/d2.

### 3. Il feed interno non ha riscaldamento per NQ e BTC

`datafeed-external/FTMOPLATFORM/` comincia **esattamente il 2025-07-01** per
`@NQ_15/30/60`, `@BTC_60` e `@NQ_1440` — zero barre prima. Il run parte lo stesso
giorno, quindi le strategie bruciano il proprio `RequiredCandles` dentro la finestra:

| stream | richieste | valutazioni saltate | primo segnale |
|---|---|---|---|
| NQ 15 | 576 | 630 | 2025-07-09 |
| NQ 30 | 288 | 314 | 2025-07-09 |
| NQ 60 | 144 | 156 | 2025-07-09 |
| BTC 60 | 144 | 109 | 2025-07-11 |

FDAX no: `@FDAX_240` ha 3.857 barre prima del 2025-07-01 e `@FDAX_1440` ne ha 601.

Il cBot, dentro cTrader, la storia ce l'ha e comincia il 2025-07-02. Nella finestra in
cui l'interno e' cieco ha fatto **13 trade per −3.937** (scala interna): 6 BTC, 3 FDAX_PCH,
2 NQ_TFU_003, 1 FDAX_SBO_001, 1 NQ_SBO_004. La prima settimana e mezza dei due run non
descrive lo stesso portafoglio.

### 4. Lo spread costa poco, ma su BTC decide il run

Gli **ingressi non slittano**: ogni riempimento del cBot avviene **esattamente** al
`triggerPrice` dell'intent (verificato su tutti i BTC). Il costo sta sull'uscita in stop —
eccesso oltre il livello nominale:

| simbolo | n | medio | mediano | max | stop nominale |
|---|---|---|---|---|---|
| BTC | 42 | 4,22 pt | 3,12 | 12,93 | 50 pt |
| FDAX | 22 | 1,14 pt | 0,79 | 5,32 | 10 / 40 pt |
| NQ | 9 | 0,37 pt | 0,20 | 1,20 | 25 / 50 pt |

In denaro, riportato alla scala interna: **1.687 USD**, il **4,2%** del divario.
Commissioni allineate (3,47 contro 4,00 per trade).

Ma su BTC l'8% di erosione su uno stop da 50 punti non e' il costo, e' il moltiplicatore:
la strategia ha rapporto 40:1 (TP 2000, SL 50), quindi il saldo dipende da **quali** take
profit prendi. L'interno ne prende due (2025-07-14 +2000 pt, 2025-08-15 +1312 pt in time
exit); il cBot ne prende uno, diverso (2025-08-22 +2001 pt). Il cBot fa **44 trade contro
24** — con exit mediana di **967 secondi** contro le 4 ore dell'interno — perche' esce
prima, torna flat dentro la stessa barra e riemette. Su un campione di due mesi le due
gambe pescano lotterie diverse: **il divario BTC (16.317, il 41%) non e' misura di nulla**.

## Aperto

- **Il feed va ricostruito** con il raccoglitore 1.2.0, cancellando prima i 17 stream
  oltre l'ora (tutti i `@*_240.json` e `@*_1440.json` di `datafeed-external/FTMOPLATFORM/`).
  Finche' non lo e', nessun confronto cBot/interno sopra l'ora e' leggibile. Non c'e' niente
  da decidere: la griglia giusta e' quella del vendor e del bot operativo, 00:00 europee.
- **Le sessioni false restano da filtrare** anche dopo la ricostruzione: sono una proprieta'
  del feed CFD, non della griglia. Vedi `audit-feed-FTMOPLATFORM.md` §3.
- **Rifare il confronto dopo il fix di `EntriesToday`.** Solo allora il residuo
  descrive il motore.
- **`run-interno-cfd-FTMOPLATFORM.json` dichiara `PlanCode: null`** mentre
  `backtest-summary` dello stesso run dichiara `FTMO-ALL`. Il marcatore mente per i run
  `Origin: Internal`: da correggere, o un confronto futuro concludera' che le gambe
  avevano piani diversi.

## Chiuso

- **"E' lo spread."** No: 4,2% del divario, e gli ingressi non slittano affatto.
  Misurato sopra, trade per trade.
- **"Il server non genera i segnali."** No, li genera: `everEvaluable: true` su tutti e
  sette gli stream, `diagnostics` vuoto, e i livelli degli intent coincidono con i prezzi
  di riempimento. L'unica strategia muta e' VBO, e la causa e' il contatore, non
  l'emissione.
- **"Il motore riempie male."** No: sui 42 trade accoppiati, 38 differiscono di meno di
  3 punti. I 4 fuori scala sono trailing/break-even che seguono percorsi intrabarra
  diversi (FDAX_PCH 07-23 e 07-31, BTC 08-22, FDAX_MAC 08-13), non convenzioni diverse.
- **"Le size sono sbagliate."** No: il fattore 10 e' `accountBalanceScale = 0,1`, voluto
  (conto 100k contro capitale neutro 1M).

## Cosa torna

- Piano, universo (16 simboli mappati e abilitati), strategie spente (98), `holding`,
  `fillConventions` (`ProtectiveBeforeTarget`, `trailingMinStepFraction` 0,10,
  `rejectWrongSideLevels` on), `engineVersion` 6.0.0: **identici sulle due gambe**.
- Barre di NQ 15/30/60 e BTC 60: **stessa griglia UTC**, stessi timestamp.
- Riempimento degli ingressi: **al livello, senza slittamento**.
- `PTS_NQ_SBO_004_60` (7 e 7 trade, divario 210), `PTS_NQ_TFU_007_1440` (2 e 2, divario
  73), `PTS_FDAX_SBO_002_1440` (1 e 1, divario 191): stesse barre, stesso riscaldamento,
  nessun cap. **Su queste i due motori coincidono entro l'1% del divario totale** — ed e'
  la prova che il motore, di per se', va.

## Trappole di misura di questa cartella

1. **Non sommare i saldi grezzi.** Fattore 10 di `accountBalanceScale`. Il valore per
   punto implicito (`grossProfit / punti / quantita`) lo smaschera in una riga.
2. **L'interno non converte EUR in USD.** FDAX vale 25,0000 esatti per punto sull'interno
   (nessuna varianza) contro 1,14-1,18 per lotto sul cBot: il cBot converte trade per
   trade, l'interno no. Il netto FDAX interno (29.464) e' in euro; in dollari sarebbe
   ~34.300. **Allarga il divario, non lo spiega.**
3. **Non accoppiare i trade per orario di ingresso.** L'interno timbra l'apertura della
   barra, il cBot il tick. Accoppiando entro una barra si trovano 42 coppie su 97/101, e
   le 55 spaiate per lato **sono il risultato**, non un difetto dell'accoppiamento.
4. **`signals.json` della sessione contiene solo gli intent riempiti** (207 su 2.317
   emessi): `PersistOnlyFilledIntents` e' `true`. Per contare emissioni e rifiuti si legge
   `session-summary.json`, non i segnali.
5. **`everEvaluable: true` non dice *da quando*.** E' `picco >= RequiredCandles`, e il
   picco puo' arrivare l'ultimo giorno. Per FDAX 240 il riscaldamento implicito e'
   765 − 271 = **494 barre**, sotto le 501 di VBO: la strategia e' diventata valutabile
   circa un giorno dopo l'avvio — poco prima di venire zittita dal contatore.
6. **Le famiglie d'uscita non si confrontano una a una.** `LocalExit:StopLoss` del cBot
   (87 casi) copre stop, trailing e break-even; l'interno li separa (48 + 11 + 16 = 75).
