# Mappa: da quale scheda viene ogni strategia PT2

La serie `PT2_*` e' il porting del **paniere rifatto** — `piootoo-repository/run-engine-v2/DOSSIER_PANIERE_001.md`,
generato il 14/09/2026 dalle celle `FDAX_1h, FDAX_4h, NQ_30m, NQ_4h` — nato perche' l'analisi che aveva
prodotto le `PTS_*` conteneva errori. Le due serie **convivono**: le `PTS_*` restano in
`Piootoo.Strategies/PiutooStrategies/` e continuano a descrivere le righe del dossier di settembre
([`mappa-strategie-pts.md`](mappa-strategie-pts.md)); le `PT2_*` stanno in
`Piootoo.Strategies/PT2Strategies/`, namespace `Piootoo.Strategies.PT2Strategies`, e numerano per conto
proprio: `PT2_NQ_PCH_001_240` e' la prima PC su NQ *di questa serie*, non la nona del catalogo.

Le regole di traduzione sono quelle di [`porting-da-report-sweep.md`](porting-da-report-sweep.md), con
l'aggiunta scritta il 16/09/2026: i run di `run-engine-v2` **etichettano le barre all'inizio** (§2.6
del dossier), come il feed e al contrario dei run delle `PTS_*`. Ogni PT2 lo dichiara con
`ResearchLabelsBarsOnOpen = true` e riporta gli orari **verbatim**: e' l'engine a confrontarli con
l'apertura. Le liste trade citate dal dossier non sono nel repository.

## La mappa: S-ID → classe C#

| Dossier | Classe C# | Mercato/TF/Motore | Finestra CET (etichetta = apertura) | Stop / target ($) | max_bars | Gate | Tenuta |
|---|---|---|---|---|---|---|---|
| `S01` | `PT2_FDAX_BSW_001_60` | FDAX 1h BIASW | ingresso lun barra 08:00-09:00 (`le_time` 08:00), uscita lun successivo stessa barra (`lx_time` 08:00) | 44.700 / — | — | nessuno (152/153) | multiday per costruzione |
| `S02` | `PT2_NQ_PCH_001_240` | NQ 4h PC | 12:00 → 16:00: barre 12:00-16:00 e 16:00-20:00 | 4.595 / — | 5 | nessuno (55/56, 52/53) | multiday |
| `S03` | `PT2_FDAX_PCH_001_240` | FDAX 4h PC | tutte le 24 ore | 3.575 / 5.800 | — | neutrale 7 vietato | intraday (fine sessione 01:00) |
| `S04` | `PT2_NQ_PCH_002_30` | NQ 30m PC | tutte le 24 ore | 1.045 / — | 46 | nessuno | multiday, solo long |

Parametri strutturali: `S02` e `S03` hanno canale a **1 barra** (rottura della barra appena chiusa),
`S03` con offset **10 tick = 10 punti**; `S04` canale a 10 barre e `direction = 1`. Nessuna delle
quattro usa trailing, breakeven o filtro di volatilita' (`dvol_min = 0`), e nessuna esclude un giorno
della settimana (`skip_day = -1`).

Le conversioni in punti del dossier tornano con il registro strumenti: FDAX 25 per punto
(44.700 → 1.788; 3.575 → 143; 5.800 → 232), NQ $20 (4.595 → 229,75; 1.045 → 52,25). Il registro
dichiara il FDAX in EUR e il dossier in dollari: cambia la valuta del P&L, non la distanza in punti.

## Il ciclo settimanale di S01, letto contro il motore

Il dossier dice «MARKET alle 08:00 di lunedì (apertura della barra da 60 minuti che chiude alle 09:00)»
e «uscita lunedì alle 09:00, market alla chiusura della barra che termina a quell'ora». Ingresso e uscita
cadono sulla **stessa barra** (la 08:00-09:00 di lunedì, etichetta 08:00): `BiasWeeklyEngine.ResolveScheduledExitUtc`
prende la prima occorrenza *dopo* la barra di ingresso, quindi la deadline e' la barra 08:00-09:00 del
lunedì **successivo** e la posizione dura una settimana. E' coerente con le metriche della scheda —
166 trade in tre anni e mezzo fuori campione, un drawdown di $99.782 — che una tenuta di un'ora non
produrrebbe, e con la frase «se quella barra non esiste (festivo) la posizione resta aperta fino alla
stessa barra della settimana successiva».

L'engine interno esegue la chiusura a tempo al **mark della barra della deadline**, cioe' la chiusura
della barra etichettata 09:00: e' quanto il dossier descrive. Il cBot invece chiude quando il proprio
orologio supera `CloseAtUtc`, cioe' all'apertura di quella barra, un'ora prima: e' una differenza fra
i due esecutori che vale per ogni BIASW, non una scelta di questo porting, ed e' da misurare al primo
confronto (vedi `compare/`).

## Cosa e' verificato e cosa no

**Le due FDAX sul feed interno, 24/01/2022 → 30/05/2025** (workspace `v02-fdax-interno`, run
`pt2-fdax-interno-2022-2025-v2`, senza spread, `RejectWrongSideLevels` spento, versione 7.4.4):

| Scheda | Trade scheda | Trade nostri | Netto scheda | Netto nostro |
|---|---|---|---|---|
| S01 BSW 1h | 166 | **166** | $191.311 | 180.711 |
| S03 PC 4h | 884 | 850 | $264.639 | 274.000 |

Il numero di trade e' il criterio del dossier (§5) in assenza delle liste; il primo run, prima delle
due correzioni del motore del 16/09 (rollover BIASW, deadline di sessione sul fill: `decisioni.md`),
dava 84 e 873. Il confronto entrata per entrata resta da fare.

- **Le liste trade del dossier non sono nel repository.** Il dossier le cita
  (`FDAX_1h/consegna/trades/fam01_BIASW.csv`, `NQ_4h/consegna/trades/fam01_PC.csv`,
  `FDAX_4h/consegna/trades/fam01_PC.csv`, `NQ_30m/consegna/trades/fam01_PC.csv`): **vanno chieste a
  chi lo ha prodotto** e messe accanto al dossier. Procedura in
  [`porting-da-report-sweep.md`](porting-da-report-sweep.md) §"Verificare il porting".
- **Datafeed.** `@FDAX_1`, `@FDAX_60` e `@FDAX_240` sono generati dal CSV del vendor in
  `datafeed-future/` (UTC, etichetta di apertura, ancoraggio 01:00). Per NQ il loop al minuto
  pretende `@NQ_1.json`, che non esiste: serve `@NQ-ASCII Mapping-CME-Futures-Minute-Trade.csv`
  nella stessa cartella, poi `aggregate_flat_feed.py --symbols NQ --timeframes 1`.
- **Il rollover della BSW in vivo** e' allineato dal 16/09/2026 (server e cBot 7.4.4): il claim
  consegna il template dello stesso verso quando la posizione scade al suo istante di validita', e
  il bot chiude e rientra. Non ancora osservato su un conto.
- **La finestra di S02.** La scheda dice «12:00 e 16:00» e subito dopo «ordini emessi sulle barre che
  *chiudono* fra le 12:00 e le 16:00»: la seconda frase e' quella dei dossier a etichetta di chiusura
  e contraddice §2.6. La classe segue §2.6 (barre che *aprono* alle 12:00 e alle 16:00); se la lista
  trade dira' il contrario, il numero resta e cambia la dichiarazione dell'etichetta, non l'orario.
- **Il calendario di sessione** (§2.1.1 del dossier: FDAX lun-ven, NQ lun-ven piu' 48 domeniche) e'
  quello che `market-calendars.json` dichiara per i due simboli. Il conteggio per giorno sul feed non e'
  stato rifatto per questa serie.
- **`StopMoneyPolicy`** non elenca nessuna PT2: girano con lo stop della ricerca, che dal 11/09/2026
  e' comunque la regola per tutte (fattore 1).

## Come si aggiunge una scheda alla serie

1. Classe in `Piootoo.Strategies/PT2Strategies/`, namespace `Piootoo.Strategies.PT2Strategies`, nome
   `PT2_[SIMBOLO]_[MOTORE]_[NNN]_[TF]` con il progressivo che prosegue quello della coppia
   (simbolo, motore) **dentro la serie**. `PtsNamingConventionTests` impone forma e contiguita'.
2. XMLdoc che apre con `Codice sorgente: SNN (run-engine-v2, DOSSIER_PANIERE_001)`: gli S-ID di questo
   dossier non sono confrontabili con quelli dei dossier precedenti.
3. Il catalogo la trova da solo (`StrategyFactory` enumera l'assembly); i controlli di conformita' la
   vedono perche' i due namespace di catalogo sono dichiarati in `PtsNamingConventionTests.Series` e in
   `StrategyClockConformanceTests.CatalogNamespaces`. Una serie nuova si dichiara li'.
4. `tools/dossier-diff.py --dossier <md> --classes <cartella> --prefix PT2` abbina schede e classi
   per impronta numerica, come per le PTS.
5. Aggiornare questa mappa.

## Riferimenti codice

- `Piootoo.Strategies/PT2Strategies/` — le classi `PT2_*`
- `Piootoo.Strategies/Easy/Engines/BiasWeeklyEngine.cs`, `PriceChannelEngine.cs` — i due motori usati
- `Piootoo.Strategies.Tests/PtsNamingConventionTests.cs`, `StrategyClockConformanceTests.cs`,
  `PtsStrategyRegistrationTests.cs` — convenzione di nome, orologi, registrazione
- `Piootoo.Core/Services/Compare/CompareRunner.cs` — enumera anche le `PT2_*`
- `tools/dossier-diff.py` — abbinamento schede ↔ classi
