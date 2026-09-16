# FDAX · NQ su cTrader — specifica di implementazione

*Generato il 14 settembre 2026 da `FDAX_1h, FDAX_4h, NQ_30m, NQ_4h`.*

**4 strategie univoche** da implementare come cBot, ricavate da 3 timeframe (1h, 30m, 4h). Ogni strategia è definita qui per intero: condizioni di entrata, filtri, uscite, e la lista trade con cui verificare il port. Non serve conoscere il trading per implementarle — serve rispettare le regole della sezione 2 alla lettera.

> **Le tre cose che fanno fallire un port.** In ordine di frequenza: le sessioni ricostruite male (§2.1), l'ordine lasciato vivo più di una barra (§2.2), e il backtest fatto su barre invece che su tick (§5).

---

## 1. Cosa si costruisce

4 cBot indipendenti. Ognuno opera su un solo strumento e un solo timeframe, con un contratto per posizione. Non comunicano fra loro.

| ID | Mercato | TF | Motore | Atteso/trade | P&L OOS | Drawdown | Trade | Equivalenti |
|---|---|---|---|---|---|---|---|---|
| [S01](#s01) | FDAX | 1h | BIASW | $1,152 | $191,311 | $99,782 | 166 | — |
| [S02](#s02) | NQ | 4h | PC | $496 | $507,868 | $92,842 | 1023 | — |
| [S03](#s03) | FDAX | 4h | PC | $299 | $264,639 | $42,218 | 884 | — |
| [S04](#s04) | NQ | 30m | PC | $70 | $109,565 | $48,609 | 995 | — |

La colonna **Equivalenti** elenca le strategie che emettono gli stessi ordini di entrata: trovate separatamente, ma sono lo stesso sistema. Se ne implementa una sola.

### Da dove vengono i numeri

| Cella | Righe approvate | Strategie | Univoche |
|---|---|---|---|
| FDAX 1h | 1 | 1 | 1 |
| FDAX 4h | 1 | 1 | 1 |
| NQ 30m | 1 | 1 | 1 |
| NQ 4h | 1 | 1 | 1 |

Le **righe approvate** includono la stessa strategia con stop e target diversi: non sono sistemi distinti e non compaiono qui. Le **univoche** restano dopo aver confrontato le entrate anche *fra* timeframe diversi.

---

## 2. Fondamenta comuni

Questa parte si scrive una volta e si riusa in tutti i 4 cBot. Ogni regola è vincolante: cambiarne una fa divergere il port dalla ricerca.

### 2.1 Sessioni

Le condizioni usano massimi e minimi di **sessione**, non di barra. Le sessioni si ricostruiscono dalle barre intraday, **non** si leggono dalle candele giornaliere del broker.

- Una sessione inizia all'ora indicata per quello strumento in §2.4 e dura fino all'inizio della successiva.
- `H_d1`, `L_d1`, `O_d1`, `C_d1` = massimo, minimo, apertura e chiusura della sessione **precedente**.
- `H_d2` … `H_d5` = le quattro sessioni ancora prima.
- `H_d0`, `L_d0`, `O_d0` = massimo, minimo e apertura della sessione **corrente**, sulle sole barre già **chiuse**.
- `HH5` = massimo di `H_d1..H_d5`; `LL5` = minimo di `L_d1..L_d5`.
- `close` (minuscolo) = chiusura della **barra** corrente, non della sessione.

#### 2.1.1 Il calendario di sessione è vincolante

**Il bot deve produrre le stesse sessioni della ricerca, e nessuna in più.** Un feed CFD quota anche quando il future è chiuso — tipicamente la **domenica sera**. Dove la ricerca non ha quella sessione, il feed ne crea una che non è mai esistita: non genera trade, ma spezza la sessione e con l'uscita a fine sessione chiude posizioni ancora valide. Misurato sul DAX: **11% del P&L**.

⚠ La regola **non** è «ignorare la domenica». Sui mercati CME le sessioni domenicali della tabella sono **vere** (nelle settimane in cui l'ora legale europea e americana sono sfasate il future apre davvero domenica sera): toglierle sarebbe un secondo errore. La regola è **riprodurre questa tabella**.

Questi sono i conteggi misurati sui dati con cui le strategie sono state trovate. Il port è corretto quando, sullo stesso periodo, li riproduce.

| Strumento | Inizio sessione | lun | mar | mer | gio | ven | sab | dom |
|---|---|---|---|---|---|---|---|---|
| **FDAX** | 01:00 CET | 664 | 682 | 685 | 684 | 668 | 0 | 0 |
| **NQ** | 00:00 CET | 690 | 695 | 693 | 697 | 683 | 0 | **48** |

Una colonna **dom** a zero significa che su quello strumento *qualunque* sessione domenicale è un difetto del feed, da scartare.

#### 2.1.2 Verifica sul feed del broker, prima di scrivere codice

La tabella sopra dice cosa deve uscire. Questa è la prova che il feed lo faccia davvero, e va fatta **prima** di implementare: se il calendario non torna, ogni confronto successivo è inquinato.

1. Dal terminale del broker esporta **un mese** di barre **M1** per ogni strumento, in CSV. Su MT5 l'export delle barre porta anche la colonna `<SPREAD>`; se puoi esportare i **tick** con bid/ask, meglio ancora.
2. **Non convertire gli orari e non rinominare le colonne.** Il fuso del file va accertato, non assunto: si ricava confrontando i movimenti con una fonte indipendente. Un terminale su fuso diverso da Europe/Rome sposta ogni filtro orario delle strategie senza dare errore.
3. Ricostruisci le sessioni con l'ora di inizio di §2.4 e conta quante ne cadono su ogni giorno della settimana.
4. Confronta con la tabella di §2.1.1. Le proporzioni fra i giorni devono corrispondere; in particolare **la colonna `dom`**.

Se `dom` risulta popolata dove la tabella dà zero, il feed sta creando la sessione fantasma: vanno scartate le barre che cadono in un giorno di sessione che la ricerca non ha. ⚠ **Non** correggere cambiando l'ora di inizio sessione: quella è un parametro delle strategie, cambiarla le trasforma in strategie diverse da quelle testate.

> Mandando il CSV grezzo dell'export a chi ha prodotto questo dossier, la verifica è automatica (`forward/verifica_feed.py`: fuso, apertura settimanale, sessioni per giorno, spread reale contro quello assunto).

### 2.2 Ciclo di vita dell'ordine

- Le condizioni si valutano **alla chiusura della barra**.
- L'ordine emesso vive **una sola barra**: alla barra successiva va cancellato e, se le condizioni reggono, ri-emesso. In cTrader: *cancel & replace* ad ogni `OnBar`.
- Nessuna condizione può usare il prezzo della barra su cui si entra.
- Al massimo **una entrata per sessione e per direzione**. Se la posizione si chiude dentro la stessa sessione, non si rientra sullo stesso lato.

### 2.3 Riempimento

- **Ordine stop** (rottura di un livello): riempie a `max(apertura, livello)` per il long, `min(apertura, livello)` per lo short. Se il prezzo apre già oltre il livello, il fill è all'apertura — mai al livello superato.
- **Ordine limite** (ritorno alla media): serve una penetrazione **stretta** del livello (`minimo < livello` per il long). Il semplice tocco non riempie.

### 2.4 Costi e unità per strumento

La ricerca esprime stop e target in **dollari per contratto**; cTrader li vuole in **punti**. Nelle schede i valori sono già convertiti — questa tabella serve per verificarli.

| Strumento | Sessione | 1 punto | 1 tick | Commissione | Slippage |
|---|---|---|---|---|---|
| **FDAX** | 01:00 CET | $25 | 1 punti | $4.00 | 1 tick/lato |
| **NQ** | 00:00 CET | $20 | 0.25 punti | $4.00 | 1 tick/lato |

⚠ Un solo valore sbagliato in questa tabella e gli stop del cBot sono sbagliati di conseguenza: sono i numeri che convertono i dollari della ricerca nei punti del broker.

### 2.6 Orari

Le candele sono etichettate al loro **inizio**. Le finestre orarie sono in **ora dei dati (CET)** — tranne gli strumenti che in tabella hanno l'ora della loro borsa (Hong Kong, Tokyo) — e si valutano sulla chiusura della barra. Una finestra il cui orario di fine è minore di quello di inizio attraversa la mezzanotte e va gestita come tale.

---

## 3. Le condizioni di pattern

I filtri sono condizioni booleane sulle grandezze di sessione della §2.1. Qui tutte quelle usate dalle 4 strategie, già in forma di formula.

| Riferimento | Condizione |
|---|---|
| `` | `il motore entra su ogni segnale strutturale` |
| `neutrale 7` | `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)` |

**deve essere VERO** = condizione requisito per entrare. **deve essere FALSO** = l'entrata è vietata quando la condizione si verifica. Le voci marcate *nessun filtro* sono assenti e non vanno implementate.

---

## 4. Le 4 strategie

### S01 · FDAX 1h · Bias settimanale  <a id='s01'></a>

**LONG + SHORT** — Entra e esce a giorni/orari fissi della settimana.

| | |
|---|---|
| Timeframe | 1h |
| Motore | BIASW |
| Atteso/trade | $1,152 |
| P&L fuori campione | $191,311 |
| Drawdown | $99,782 |
| Trade | 166 |
| Stop loss | 1,788.0 pt |
| Take profit | — |

**Ciclo settimanale a giorno e ora fissi**

- LONG: **MARKET** alle **08:00 di lunedì** (apertura della barra da 60 minuti che chiude alle 09:00 di lunedì)
- SHORT: **spento** — questa strategia non apre mai al ribasso
- Orari in CET. Le candele sono etichettate al loro **inizio**: il fill è all'apertura della barra che inizia all'orario scritto sopra.
- I filtri pattern si valutano alla chiusura della barra precedente.
- Se quella barra non esiste (festivo, mercato chiuso) la settimana salta.

**Filtri pattern**

*Nessun filtro pattern*

- —: `il motore entra su ogni segnale strutturale`

**Quando può operare**

- Nessun filtro orario a parte il giorno e l'ora di entrata, che fanno già parte della regola di entrata
- Tiene la posizione **oltre la fine della sessione**: questo motore non chiude mai per fine sessione, e non c'è un parametro che lo cambi
- Al massimo **una entrata per settimana e per direzione**

**Uscite**

- Uscita LONG: **lunedì alle 09:00**, market alla chiusura della barra che termina a quell'ora.
- Se quella barra non esiste (festivo) la posizione resta aperta fino alla stessa barra della settimana successiva.
- È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.
- Stop loss: **$44,700** per contratto = **1,788.00 pt**
- Take profit: **nessuno**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `FDAX_1h/consegna/trades/fam01_BIASW.csv`

---

### S02 · NQ 4h · Price channel (Donchian)  <a id='s02'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $496 |
| P&L fuori campione | $507,868 |
| Drawdown | $92,842 |
| Trade | 1023 |
| Stop loss | 229.8 pt |
| Take profit | — |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre**
- SHORT: stop sell sul **minimo delle ultime 1 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Nessun filtro pattern*

- —: `il motore entra su ogni segnale strutturale`

**Quando può operare**

- Opera solo fra **12:00 e 16:00**, CET: ordini emessi sulle barre che **chiudono** fra le 12:00 e le 16:00 (estremi inclusi), cioè attivi da quell'ora in poi
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$4,595** per contratto = **229.75 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **5 barre** (20 ore)

**Verifica** — lista trade di riferimento: `NQ_4h/consegna/trades/fam01_PC.csv`

---

### S03 · FDAX 4h · Price channel (Donchian)  <a id='s03'></a>

**LONG + SHORT** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 4h |
| Motore | PC |
| Atteso/trade | $299 |
| P&L fuori campione | $264,639 |
| Drawdown | $42,218 |
| Trade | 884 |
| Stop loss | 143.0 pt |
| Take profit | 232.0 pt |

**Ordine STOP sul canale di Donchian a 1 barre**

- LONG: stop buy sul **massimo delle ultime 1 barre** + 10 tick (10 pt)
- SHORT: stop sell sul **minimo delle ultime 1 barre** − 10 tick (10 pt)
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).

**Filtri pattern**

*Filtro comune a long e short*

- deve essere FALSO — neutrale 7: `|O_d1-C_d1| > 0.75 * (H_d1-L_d1)`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Chiude tutto a **fine sessione** (nessun overnight)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$3,575** per contratto = **143.00 pt**
- Take profit: **$5,800** = **232.00 pt**
- Nessuna uscita a tempo

**Verifica** — lista trade di riferimento: `FDAX_4h/consegna/trades/fam01_PC.csv`

---

### S04 · NQ 30m · Price channel (Donchian)  <a id='s04'></a>

**SOLO LONG** — Rottura del canale di Donchian calcolato sulle barre, non sulle sessioni.

| | |
|---|---|
| Timeframe | 30m |
| Motore | PC |
| Atteso/trade | $70 |
| P&L fuori campione | $109,565 |
| Drawdown | $48,609 |
| Trade | 995 |
| Stop loss | 52.2 pt |
| Take profit | — |

**Ordine STOP sul canale di Donchian a 10 barre**

- LONG: stop buy sul **massimo delle ultime 10 barre**
- SHORT: stop sell sul **minimo delle ultime 10 barre**
- Il canale è calcolato sulle **barre del timeframe**, non sulle sessioni, e la barra di emissione è inclusa (è chiusa quando si valuta).
- **Solo long**: il lato short non opera mai.

**Filtri pattern**

*Nessun filtro pattern*

- —: `il motore entra su ogni segnale strutturale`

**Quando può operare**

- Nessun filtro orario: opera su tutte le 24 ore
- Può restare aperta **oltre la sessione** (multiday)
- Al massimo **una entrata per sessione e per direzione**

**Uscite**

- Stop loss: **$1,045** per contratto = **52.25 pt**
- Take profit: **nessuno**
- Uscita a tempo dopo **46 barre** (23 ore)

**Verifica** — lista trade di riferimento: `NQ_30m/consegna/trades/fam01_PC.csv`

---

## 5. Come si verifica un port

Il port è corretto quando le **entrate** coincidono — timestamp e prezzo. I P&L sono una conseguenza.

1. Backtest sullo stesso periodo della lista di riferimento, **su dati tick**.
2. Confronta le entrate: devono coincidere al minuto e al prezzo.
3. Se le **entrate** non coincidono, il problema è nelle condizioni o nella ricostruzione delle sessioni (§2.1). Isola stampando `H_d1` e `L_d1` per qualche giorno.
4. Se le entrate coincidono ma i **P&L** no, il problema è nelle uscite o nei costi (§2.3, §2.4).

> ⚠ **Il backtest su barre non è attendibile per queste strategie.** Su dati a barre il simulatore di cTrader valuta lo stop anche contro la barra d'ingresso, percorso pre-entrata incluso, e chiude a prezzi mai esistiti. Su un port già fatto, 201 trade su 359 uscivano nello stesso minuto del fill, con slippage medio di 23 punti oltre lo stop. Usare **Tick data (accurate)**.

### 5.1 Su QUALE serie si verifica — leggere prima di dichiarare un bug

Le liste trade di riferimento sono generate su **future** (la serie con cui le strategie sono state trovate). Il broker quota **CFD**, che è un'altra serie: niente costo del denaro, niente dividendi attesi, orari diversi. Le due non coincidono, e **non devono**.

Misurato su 45 strategie di NQ, ES, YM e FDAX, tre anni, stessi costi sui due lati (quindi è solo effetto della serie):

| | atteso passando al CFD |
|---|---|
| entrate che coincidono | **~84%** (81-87% per mercato) |
| P&L | **−13%** (NQ −14, YM −16, FDAX −16, ES 0) |
| strategie che restano in utile | **45 su 45** |

⚠ **Questi scarti non sono un difetto del port.** Il criterio del punto 2 — entrate identiche al minuto e al prezzo — vale **solo** confrontando sulla stessa serie. Su dati CFD un port perfetto mostra comunque ~16% di trade diversi. Cercarne la causa nel codice è tempo perso.

Come distinguere un port rotto da un port corretto su serie diversa: un port rotto sbaglia **in modo sistematico** (tutte le entrate spostate della stessa quantità, o un'intera fascia oraria assente). L'effetto serie è **sparso**: trade che compaiono e spariscono senza schema, e quelli che restano hanno prezzi vicini.

---

## 6. Vincolo operativo

Le 4 strategie sono univoche: nessuna coppia condivide più del 70% degli ordini di entrata. È questo che rende lecito farle girare su conti separati — due sistemi che mandano gli stessi ordini sono copy trading, e presso una prop firm costano il conto.

Il vincolo si misura sulle **entrate**, non sulla correlazione dei risultati: due strategie possono avere P&L molto diversi e mandare gli stessi ordini.

*Dati fuori campione dal 24/01/2022 al 30/05/2025. Criteri di selezione in `METODO_RICERCA.md`.*