# Mappa delle strategie PT8DAV

La serie `PT8DAV_*` porta in C# la **consegna v5.1** della ricerca Python fatta su un altro server
(25/09/2026, `piootoo-repository/run-engine-v3/`): 111 strategie su 8 mercati. E' la stessa ricerca
e sono gli stessi quindici motori della v5.0, gia' portata nella serie `PT5DAV_*`
([`mappa-strategie-pt5dav.md`](mappa-strategie-pt5dav.md)), con un metodo di selezione diverso.
Stato al 01/10/2026: **111 classi** in `Piootoo.Strategies/PT8DAVStrategies/`, riconciliate trade
per trade con il simulatore della ricerca sul minuto FTMO: **97,3%** dei suoi 6.882 trade
ritrovati, mediana per strategia 100%, 98 strategie su 111 sopra il 90% che la consegna chiede
(`VERIFICA.md`).

Analisi fatta prima del porting: `piootoo-repository/run-engine-v3/analisi-2026-10-01.md`.

## Da dove vengono, e cosa promette la selezione

| | v5.0 (`PT5DAV_*`, 224) | v5.1 (`PT8DAV_*`, 111) |
|---|---|---|
| parametri scelti su | tutta la storia 2012-2025 | i dati fino al **31/05/2022** |
| filtro | l'anno broker | in utile sul CFD nei 3 anni 06/2022 → 05/2025 |
| doppioni | — | di ogni gruppo che entra insieme oltre il 70% delle volte ne resta una |
| anno broker (16/09/2025 → 09/09/2026) | usato per scegliere | **non usato**: 57 su 111 in utile |

Misurato sui 94.347 trade di `trade_111.csv` (netto CFD, 1 future, per anno): in campione 2,79 M,
fuori campione 1,25 M (**0,45**), anno broker 0,87 M (**0,31**; mediana per strategia 0,15). Il
tratto 2022-2025 non e' un fuori campione pulito: le 111 sono quelle in utile li', per costruzione.
L'unico tratto che nessuna scelta ha visto e' l'anno broker. I 32,8 M di storia e il drawdown di
410 k di `SIZE.md` hanno l'ottimizzazione dentro per dieci anni su tredici: la size non si
dimensiona li'.

Il codice della ricerca (`NQ-15M-BOS-…`) **non e' un'impronta dei parametri**: otto codici
compaiono in entrambe le consegne, tre con `stop_atr` diverso. 72 celle (simbolo, timeframe, motore)
delle 111 esistono anche fra le PT5DAV, con parametri diversi: nei piani vanno trattate come
cugine, con ingressi gemelli attesi.

## Cosa e' entrato e come

Tutte le 111. Nome `PT8DAV_{SIMBOLO}_{SIGLA}_{NNN}_{TF}`, progressivo per (simbolo, sigla) in ordine
di timeframe e codice; sigle come le PT5DAV. Le classi **derivano dai motori PT5DAV**
(`PT5DAVStrategies/Engines/`), che non si copiano: la serie ha solo classi.

Le classi si **generano** dal CSV, parametri verbatim, con l'estratto della scheda nel commento:
`python tools/pt8dav/gen_pt8dav.py ALL`. Rigenerarle e' il modo di cambiarle. Il generatore ferma su
cio' che non sa tradurre:

- una colonna valorizzata che il motore non legge (salvo al suo valore spento);
- un valore enumerato che il motore non conosce (`vol_source`, `momentum`, `direction`,
  `entry_type`, `level_source`, `level_choice`);
- `ordine` ed `entrate`, le due colonne nuove della v5.1, che non sono parametri: si confrontano con
  cio' che il motore fa (tipo d'ordine e una-entrata-per-sessione) e devono coincidere.

Tre controlli indipendenti dal generatore:

- `Pt8DavConversionTests`: ogni riga ha esattamente una classe, e la classe ha la configurazione che
  quella riga da' al contenitore di ricerca dello stesso motore (`RC5_*`), campo per campo;
- le **formule dei pattern**: i 49 pattern neutri e direzionali che le 111 schede usano sono stati
  confrontati uno per uno con `EasyLib` (i 14 che la v5.0 non usava mai compresi: neutri 21, 41,
  44; direzionali 4, 5, 7, 11, 15, 30, 38, 48, 50), e il segno dei direzionali negativi con le schede;
- le **frasi-regola**: le 133 forme distinte di regola delle 111 schede (numeri normalizzati) sono
  state lette contro i motori. Quelle che il motore non fa sono le «differenze note» qui sotto.

## Cosa e' cambiato nei motori (01/10/2026)

La riconciliazione ha trovato regole che i motori PT5DAV non avevano o avevano sbagliate. **Sono
correzioni del motore condiviso: cambiano anche le PT5DAV.** Ognuna e' nel codice con la misura.

| regola | dove | misura |
|---|---|---|
| `vol_source = 2`: VOL e' l'ATR di **Wilder** su `atr_len` **sessioni** | VBO, `DailySessionsAtr` | nuovo nella v5.1; prima tre VBO non aprivano un trade. VOL implicito nei prezzi d'ingresso / Wilder = 1,0000 (mediana e primo decile); media semplice 0,93-0,99 |
| `vol_source = 3`: ATR di **Wilder** sulle barre fino a quella prima del segnale, non media semplice | VBO, `BarAtrBeforeCurrentBar` | ES-1H-VBO: 104 livelli su 120 entro il 2‰ con Wilder, 1 su 120 con la media semplice; trade ritrovati 81% → 96% |
| un ordine **stop o limit non attraversa il cambio di sessione** ne' il fine settimana; uno **a mercato** si' | `Finish` | sulla sterlina (24 ore) 0 ingressi alle 00:00 su 6.572 trade stop/limit, 70 su 183 per LF a mercato; BP-15M-RHL 53% → 100% |
| DAX: l'ordine a mercato nato sull'ultima barra della fascia si esegue alle 08:00 del giorno dopo | `Finish` | 52 ingressi su 475 di FDAX-1H-LFHL, tutti quelli delle due FDAX 4h a mercato; prima non nasceva |
| DAX: `max_bars` conta le sole barre 08-22 che esistono (non le notturne del feed, non i festivi) | `CountsMarketBarsInPosition`, uscita `ExitOnly` | stessa barra d'uscita 4-27% → 81-100% sulle FDAX overnight |
| BIASW: l'uscita vale solo se la barra d'uscita **esiste** negli orari del CFD | `WithScheduledExit` | YM-1H-BIASW: 101 trade nostri contro 22, poi 21 |
| MAC: l'uscita del venerdi' e' alla **chiusura del CFD**, sull'ultima barra anche se tronca | `WeekendExitUtc` | GC-30M-MAC: stessa uscita 72% → 91% |
| un `vol_source` sconosciuto ferma la strategia invece di tacere | VBO | — |

**Effetto sulle PT5DAV**, misurato lo stesso giorno rigirando la loro riconciliazione FTMO
(27/08/2025 → 09/09/2026, 12.864 trade della ricerca) prima sul codice non modificato e poi con le
correzioni: trade ritrovati **92,1% → 95,6%**, stessa barra d'uscita 91,4% → 93,1%. Per simbolo: BP
87,9 → 96,2, BTC 80,8 → 95,0 (la regola sul cambio di sessione: il Bitcoin tratta a mezzanotte),
GC 93,6 → 94,6, NQ 96,5 → 97,3, ES 93,5 → 94,6, FDAX 95,0 → 95,9 con le uscite delle overnight dal
10-30% all'85-100%; CL, YM, KC e CC invariati entro un trade. Tre righe peggiorano, tutte di un
trade su campioni piccoli (`BP_BOS_002_30`, `FDAX_LFD_003_60`) o nelle sole uscite a fronte di piu'
ingressi ritrovati (`BTC_SBO_001_30`, 74% → 98%). Resoconti in `piootoo-repository/PT5DAV/verifica/`.
Le PT5DAV nei piani attivi operano quindi in modo diverso da prima del 01/10: piu' vicino alla
ricerca, ma diverso — va detto nel registro dei piani al prossimo rilascio.

### Il DAX a 4 ore: barre piegate dal motore

La ricerca taglia il DAX alla fascia 08-22 e conta le barre a 4 ore dalle 08:00 (08-12, 12-16,
16-20, 20-22). La griglia del feed oltre l'ora e' una per simbolo ed e' ancorata all'01:00, quella
su cui girano le PT3B su FDAX: le due danno barre diverse. La prova con le cinque FDAX a 4 ore sulla
griglia 01:00 e i parametri verbatim: **tre non aprono mai un trade** (la finestra chiede una barra
che chiuda entro le 12:00; li' chiudono alle 13, 17 e 21), le altre due ritrovano il 25% e il 67%.

Le cinque classi ricevono allora la serie **a 60 minuti** (`TimeframeMinutes = 60`), che e'
allineata all'ora su ogni griglia, e dichiarano `BarMinutes = 240`: il motore piega le orarie nella
barra della ricerca (`Pt5DavEngineBase.FoldToResearchBars`), decide solo quando una si chiude, e
l'ordine nasce valido per una barra della ricerca (`RetimeToResearchBar`). Il nome porta la barra
(`_240`), non la serie. Riconciliate: BO_S 56/56, BO 20/20, VBO 30/30, LF 3/4, LF_HL 5/6.

**Da verificare prima del live**: il cBot riceve uno stream a 60 minuti e un intent che vive 240.
Che conti la vita del pending sui 240 dell'intent e non sui 60 dello stream non e' stato provato.
Riguarda le tre con ordine stop (BO, BO_S, VBO); le due a mercato non hanno pending.

I motori che lasciano contare un'uscita a chi esegue (BIAS) o emettono uscite con i tempi della
serie (MAC, BIASW) non possono piegare le barre, e si rifiutano (`SupportsFoldedBars`).

## Riconciliazione

`Pt5DavParityStudy.Pt8DavMatchesThePythonTradesOnFtmo` (studio, `PIOOTOO_STUDI=1`, circa 14 minuti
per gli 8 simboli): motore vero, orologio al minuto, senza costi, `RejectWrongSideLevels` spento,
feed FTMO, 16/09/2025 → 09/09/2026. Lo studio parte 300 giorni prima con le strategie valutate ma
ferme (`SweepJob.EntriesFromUtc`), per scaldare l'ATR50. Resoconti e trade a confronto in
`piootoo-repository/run-engine-v3/verifica/`.

Totale 6.698 trade su 6.882 (97,3%), ingresso al tick. Per simbolo, mediana e minimo: BP 99/75, CL
99/76, ES 100/87, FDAX 100/75, GC 100/95, KC 100/92, NQ 100/86, YM 99/0 (una strategia con due
trade).

### Quello che resta, e perche'

**L'ATR50 della ricerca nel periodo broker non e' rodato.** Ricavato dalle distanze di stop dei suoi
trade: a settembre 2025 vale il 75% di quello vero, e converge a 1,000 solo ad agosto 2026, con il
decadimento esatto di una media di Wilder partita da zero a meta' maggio 2025 (NQ: scarto mediano
0,08% con quel modello). La prova sul broker della ricerca ha quindi stop e livelli piu' stretti del
dovuto per mesi. I nostri sono quelli della definizione. Conseguenza: le 12 strategie sotto il 90%
(oltre alla MAC qui sotto) hanno tutte un livello d'ingresso spostato in ATR50, e la corrispondenza
sale col tempo — 95,4% fra settembre e dicembre 2025, 97,7% fra gennaio e aprile, **98,3%** da
maggio; NQ-15M-LF da 28/41 a 45/45, NQ-30M-BOS da 17/22 a 15/15. Non e' un errore di conversione, ed
e' un motivo in piu' per non leggere i numeri dell'anno broker della consegna come quelli della
strategia.

**Differenze note, non corrette:**

- **La MAC della ricerca inverte**, il sistema no: sull'incrocio contrario apre subito la posizione
  opposta. Engine, server e cBot non invertono (`PiootooTradingService.ProcessSignals`,
  compare-0041), quindi quel trade manca: 9 ingressi su 48 di YM-4H-MAC, i soli che mancano.
- **Limit a penetrazione stretta**: per RHL, RBB e BIAS_RT (28 strategie) la ricerca riempie solo
  se il prezzo *passa* il livello; il motore riempie al tocco. Scarto non visibile nei numeri
  (queste strategie stanno fra il 93% e il 100%).
- **Festivi del DAX per le uscite a orario**; gli stop notturni del DAX sul gap di apertura (la
  ricerca esce al livello, noi al primo prezzo).

## Avvertenze per i piani

- `PT8DAV_YM_BSW_001_60`: l'uscita long «giovedi' alle 00:00» cade nella pausa giornaliera del CFD
  e non esiste quasi mai. Nei fatti e' un long del lunedi' tenuto fino allo stop: nell'anno broker
  due posizioni di 108 e 163 notti. E' cio' che la ricerca ha selezionato, ed e' portata cosi'; in
  un piano con il flat di fine settimana diventa un'altra strategia.
- Quattro strategie tengono fino a 10 giornate (`BP_RHL_001_15`, `CL_LFD_001_30`, `YM_LFH_002_60`,
  `YM_RHL_001_30`): la tenuta la decide il piano (`HoldingResolver`).
- 111 strategie insieme sono circa 23 trade al giorno e fino a 35 posizioni aperte: i piani si
  compongono con `piootoo-plan-builder`, non con il paniere intero.

## Le classi

Trade della ricerca e nostri nel periodo broker; «ritrovati» = trade della ricerca con la stessa
barra d'ingresso e lo stesso lato.

| classe | codice della ricerca | motore | ordine | tenuta | trade ricerca | nostri | ritrovati | stessa uscita |
|---|---|---|---|---|---:|---:|---:|---:|
| `PT8DAV_BP_BBO_001_15` | `BP-15M-BIASBO-708c70` | BIAS_BO | stop | del motore | 195 | 198 | 99% | 98% |
| `PT8DAV_BP_BBO_002_240` | `BP-4H-BIASBO-66a0de` | BIAS_BO | stop | del motore | 122 | 124 | 98% | 99% |
| `PT8DAV_BP_BIA_001_15` | `BP-15M-BIAS-05a93a` | BIAS | market | del motore | 203 | 205 | 99% | 99% |
| `PT8DAV_BP_LFD_001_30` | `BP-30M-LF-4cdc5b` | LF | market | intraday | 8 | 8 | 75% | 100% |
| `PT8DAV_BP_LFD_002_240` | `BP-4H-LF-f437ae` | LF | market | overnight 1 giornate | 5 | 6 | 100% | 100% |
| `PT8DAV_BP_PCH_001_60` | `BP-1H-PC-35e04a` | PC | stop | overnight 1 giornate | 24 | 24 | 100% | 92% |
| `PT8DAV_BP_RBM_001_30` | `BP-30M-RBBM-94e71f` | RBB_M | limit | intraday | 16 | 16 | 94% | 87% |
| `PT8DAV_BP_RHL_001_15` | `BP-15M-RHL-579220` | RHL | limit | overnight 10 giornate | 36 | 36 | 100% | 81% |
| `PT8DAV_BP_SBO_001_15` | `BP-15M-BO-58a5fd` | BO | stop | intraday | 5 | 5 | 100% | 80% |
| `PT8DAV_BP_VBO_001_15` | `BP-15M-VBO-366341` | VBO | stop | overnight 5 giornate | 38 | 40 | 97% | 97% |
| `PT8DAV_CL_BOS_001_60` | `CL-1H-BOS-3616b7` | BO_S | stop | overnight 1 giornate | 21 | 16 | 76% | 94% |
| `PT8DAV_CL_BOS_002_240` | `CL-4H-BOS-266dca` | BO_S | stop | overnight 5 giornate | 14 | 13 | 93% | 100% |
| `PT8DAV_CL_BSW_001_15` | `CL-15M-BIASW-37bccc` | BIASW | market | del motore | 45 | 46 | 100% | 98% |
| `PT8DAV_CL_LFD_001_30` | `CL-30M-LF-09db78` | LF | market | overnight 10 giornate | 8 | 8 | 100% | 100% |
| `PT8DAV_CL_LFH_001_30` | `CL-30M-LFHL-e1d51a` | LF_HL | market | overnight 1 giornate | 14 | 13 | 86% | 100% |
| `PT8DAV_CL_RHL_001_15` | `CL-15M-RHL-b6a766` | RHL | limit | intraday | 41 | 41 | 98% | 93% |
| `PT8DAV_CL_RHL_002_30` | `CL-30M-RHL-8b2671` | RHL | limit | overnight 5 giornate | 14 | 14 | 100% | 100% |
| `PT8DAV_CL_RHL_003_240` | `CL-4H-RHL-0b2514` | RHL | limit | intraday | 15 | 16 | 100% | 100% |
| `PT8DAV_ES_BOS_001_15` | `ES-15M-BOS-e59326` | BO_S | stop | intraday | 12 | 11 | 92% | 91% |
| `PT8DAV_ES_BOS_002_30` | `ES-30M-BOS-de468f` | BO_S | stop | intraday | 38 | 36 | 89% | 82% |
| `PT8DAV_ES_BSW_001_15` | `ES-15M-BIASW-4870eb` | BIASW | market | del motore | 48 | 51 | 100% | 96% |
| `PT8DAV_ES_LFD_001_60` | `ES-1H-LF-96f483` | LF | market | overnight 5 giornate | 30 | 30 | 87% | 88% |
| `PT8DAV_ES_LFH_001_15` | `ES-15M-LFHL-06b892` | LF_HL | market | intraday | 19 | 19 | 95% | 94% |
| `PT8DAV_ES_LFH_002_60` | `ES-1H-LFHL-8d5600` | LF_HL | market | intraday | 3 | 3 | 100% | 100% |
| `PT8DAV_ES_MAC_001_30` | `ES-30M-MAC-1dbff7` | MAC | market | del motore | 41 | 42 | 100% | 95% |
| `PT8DAV_ES_PCH_001_15` | `ES-15M-PC-56b7e3` | PC | stop | intraday | 80 | 81 | 100% | 89% |
| `PT8DAV_ES_RBU_001_30` | `ES-30M-RBBU-101fb7` | RBB_U | limit | overnight 1 giornate | 135 | 137 | 92% | 88% |
| `PT8DAV_ES_RBU_002_60` | `ES-1H-RBBU-a7f570` | RBB_U | limit | intraday | 85 | 87 | 100% | 91% |
| `PT8DAV_ES_RHL_001_15` | `ES-15M-RHL-63f185` | RHL | limit | overnight 3 giornate | 11 | 11 | 91% | 100% |
| `PT8DAV_ES_RHL_002_60` | `ES-1H-RHL-41e912` | RHL | limit | overnight 1 giornate | 42 | 41 | 95% | 85% |
| `PT8DAV_ES_TFM_001_240` | `ES-4H-TFM-77198b` | TF_M | stop | intraday | 13 | 13 | 100% | 100% |
| `PT8DAV_ES_VBO_001_15` | `ES-15M-VBO-338cca` | VBO | stop | intraday | 57 | 58 | 100% | 96% |
| `PT8DAV_ES_VBO_002_30` | `ES-30M-VBO-a33891` | VBO | stop | intraday | 21 | 21 | 100% | 100% |
| `PT8DAV_ES_VBO_003_60` | `ES-1H-VBO-0b900e` | VBO | stop | intraday | 133 | 137 | 100% | 86% |
| `PT8DAV_FDAX_BOS_001_30` | `FDAX-30M-BOS-5483ed` | BO_S | stop | intraday | 124 | 121 | 94% | 89% |
| `PT8DAV_FDAX_BOS_002_240` | `FDAX-4H-BOS-c185c5` | BO_S | stop | intraday | 56 | 57 | 100% | 96% |
| `PT8DAV_FDAX_BRT_001_15` | `FDAX-15M-BIASRT-15fb09` | BIAS_RT | limit | del motore | 193 | 198 | 100% | 96% |
| `PT8DAV_FDAX_BRT_002_30` | `FDAX-30M-BIASRT-8144b9` | BIAS_RT | limit | del motore | 173 | 168 | 94% | 100% |
| `PT8DAV_FDAX_LFD_001_30` | `FDAX-30M-LF-b84c31` | LF | market | overnight 1 giornate | 30 | 29 | 93% | 93% |
| `PT8DAV_FDAX_LFD_002_240` | `FDAX-4H-LF-cc1098` | LF | market | intraday | 4 | 3 | 75% | 100% |
| `PT8DAV_FDAX_LFH_001_15` | `FDAX-15M-LFHL-a2a479` | LF_HL | market | overnight 2 giornate | 16 | 16 | 100% | 81% |
| `PT8DAV_FDAX_LFH_002_60` | `FDAX-1H-LFHL-1b7ca6` | LF_HL | market | overnight 1 giornate | 28 | 28 | 96% | 100% |
| `PT8DAV_FDAX_LFH_003_240` | `FDAX-4H-LFHL-3605bf` | LF_HL | market | overnight 3 giornate | 6 | 5 | 83% | 100% |
| `PT8DAV_FDAX_PCH_001_15` | `FDAX-15M-PC-7dad11` | PC | stop | intraday | 52 | 52 | 100% | 92% |
| `PT8DAV_FDAX_RHL_001_30` | `FDAX-30M-RHL-383827` | RHL | limit | overnight 1 giornate | 40 | 42 | 98% | 87% |
| `PT8DAV_FDAX_RHL_002_60` | `FDAX-1H-RHL-e6b251` | RHL | limit | overnight 1 giornate | 21 | 22 | 100% | 90% |
| `PT8DAV_FDAX_SBO_001_15` | `FDAX-15M-BO-0dd638` | BO | stop | overnight 1 giornate | 52 | 51 | 92% | 81% |
| `PT8DAV_FDAX_SBO_002_30` | `FDAX-30M-BO-66b8eb` | BO | stop | overnight 1 giornate | 45 | 46 | 98% | 86% |
| `PT8DAV_FDAX_SBO_003_60` | `FDAX-1H-BO-30ac18` | BO | stop | intraday | 25 | 26 | 100% | 100% |
| `PT8DAV_FDAX_SBO_004_240` | `FDAX-4H-BO-e72a1e` | BO | stop | intraday | 20 | 20 | 100% | 100% |
| `PT8DAV_FDAX_TFM_001_15` | `FDAX-15M-TFM-24e36c` | TF_M | stop | intraday | 59 | 61 | 100% | 88% |
| `PT8DAV_FDAX_VBO_001_15` | `FDAX-15M-VBO-e3797c` | VBO | stop | intraday | 101 | 104 | 100% | 82% |
| `PT8DAV_FDAX_VBO_002_30` | `FDAX-30M-VBO-5c3d7d` | VBO | stop | intraday | 26 | 26 | 100% | 100% |
| `PT8DAV_FDAX_VBO_003_240` | `FDAX-4H-VBO-8b2691` | VBO | stop | overnight 1 giornate | 30 | 30 | 100% | 90% |
| `PT8DAV_GC_BOS_001_60` | `GC-1H-BOS-fb52a8` | BO_S | stop | intraday | 8 | 8 | 100% | 100% |
| `PT8DAV_GC_LFD_001_60` | `GC-1H-LF-7d0430` | LF | market | overnight 3 giornate | 31 | 32 | 100% | 97% |
| `PT8DAV_GC_LFH_001_30` | `GC-30M-LFHL-2ff331` | LF_HL | market | intraday | 43 | 44 | 100% | 98% |
| `PT8DAV_GC_LFH_002_240` | `GC-4H-LFHL-8e771a` | LF_HL | market | overnight 2 giornate | 8 | 9 | 100% | 100% |
| `PT8DAV_GC_MAC_001_30` | `GC-30M-MAC-0992dc` | MAC | market | del motore | 73 | 70 | 95% | 91% |
| `PT8DAV_GC_PCH_001_30` | `GC-30M-PC-6f9fd4` | PC | stop | intraday | 66 | 67 | 100% | 98% |
| `PT8DAV_GC_PCH_002_60` | `GC-1H-PC-c38862` | PC | stop | intraday | 92 | 94 | 100% | 99% |
| `PT8DAV_GC_PCH_003_240` | `GC-4H-PC-65a002` | PC | stop | intraday | 180 | 184 | 100% | 100% |
| `PT8DAV_GC_RBM_001_30` | `GC-30M-RBBM-634e19` | RBB_M | limit | intraday | 204 | 207 | 100% | 96% |
| `PT8DAV_GC_RBM_002_60` | `GC-1H-RBBM-c99262` | RBB_M | limit | intraday | 148 | 151 | 100% | 96% |
| `PT8DAV_GC_RBU_001_15` | `GC-15M-RBBU-f5a594` | RBB_U | limit | intraday | 236 | 239 | 99% | 94% |
| `PT8DAV_GC_RHL_001_30` | `GC-30M-RHL-20b00f` | RHL | limit | intraday | 60 | 60 | 98% | 95% |
| `PT8DAV_GC_SBO_001_15` | `GC-15M-BO-b6f52d` | BO | stop | intraday | 5 | 5 | 100% | 100% |
| `PT8DAV_GC_TFM_001_15` | `GC-15M-TFM-acafd8` | TF_M | stop | intraday | 77 | 78 | 100% | 92% |
| `PT8DAV_GC_VBO_001_15` | `GC-15M-VBO-3d44fd` | VBO | stop | overnight 1 giornate | 27 | 28 | 100% | 100% |
| `PT8DAV_KC_BOS_001_15` | `KC-15M-BOS-819341` | BO_S | stop | intraday | 5 | 6 | 100% | 100% |
| `PT8DAV_KC_RHL_001_30` | `KC-30M-RHL-568d96` | RHL | limit | intraday | 17 | 19 | 100% | 100% |
| `PT8DAV_KC_TFM_001_30` | `KC-30M-TFM-bfb5f1` | TF_M | stop | intraday | 30 | 32 | 100% | 100% |
| `PT8DAV_KC_VBO_001_60` | `KC-1H-VBO-eb33b9` | VBO | stop | overnight 1 giornate | 26 | 26 | 92% | 96% |
| `PT8DAV_NQ_BOS_001_30` | `NQ-30M-BOS-d6bd00` | BO_S | stop | intraday | 64 | 59 | 86% | 87% |
| `PT8DAV_NQ_BSW_001_240` | `NQ-4H-BIASW-2aa291` | BIASW | market | del motore | 48 | 50 | 100% | 96% |
| `PT8DAV_NQ_LFD_001_15` | `NQ-15M-LF-3ac566` | LF | market | intraday | 137 | 137 | 88% | 93% |
| `PT8DAV_NQ_LFD_002_60` | `NQ-1H-LF-25505f` | LF | market | overnight 1 giornate | 26 | 27 | 96% | 96% |
| `PT8DAV_NQ_LFH_001_15` | `NQ-15M-LFHL-f70a71` | LF_HL | market | intraday | 93 | 94 | 95% | 90% |
| `PT8DAV_NQ_LFH_002_30` | `NQ-30M-LFHL-968c28` | LF_HL | market | overnight 1 giornate | 94 | 90 | 87% | 85% |
| `PT8DAV_NQ_MAC_001_15` | `NQ-15M-MAC-26880b` | MAC | market | del motore | 76 | 83 | 100% | 100% |
| `PT8DAV_NQ_PCH_001_15` | `NQ-15M-PC-1113e7` | PC | stop | intraday | 56 | 58 | 100% | 82% |
| `PT8DAV_NQ_PCH_002_60` | `NQ-1H-PC-dd1e5f` | PC | stop | intraday | 186 | 187 | 97% | 92% |
| `PT8DAV_NQ_RBU_001_15` | `NQ-15M-RBBU-5f0507` | RBB_U | limit | overnight 3 giornate | 129 | 125 | 90% | 72% |
| `PT8DAV_NQ_RBU_002_240` | `NQ-4H-RBBU-2d5d5a` | RBB_U | limit | intraday | 14 | 15 | 100% | 100% |
| `PT8DAV_NQ_RHL_001_60` | `NQ-1H-RHL-8f783f` | RHL | limit | overnight 2 giornate | 17 | 17 | 100% | 100% |
| `PT8DAV_NQ_SBO_001_15` | `NQ-15M-BO-8fc2b8` | BO | stop | intraday | 69 | 69 | 100% | 91% |
| `PT8DAV_NQ_SBO_002_30` | `NQ-30M-BO-82f356` | BO | stop | intraday | 42 | 42 | 100% | 98% |
| `PT8DAV_NQ_SBO_003_60` | `NQ-1H-BO-8d09a9` | BO | stop | intraday | 23 | 23 | 100% | 91% |
| `PT8DAV_NQ_VBO_001_15` | `NQ-15M-VBO-806050` | VBO | stop | intraday | 20 | 20 | 100% | 80% |
| `PT8DAV_NQ_VBO_002_30` | `NQ-30M-VBO-beb2a3` | VBO | stop | intraday | 24 | 25 | 100% | 100% |
| `PT8DAV_NQ_VBO_003_240` | `NQ-4H-VBO-cc305f` | VBO | stop | intraday | 52 | 52 | 100% | 100% |
| `PT8DAV_YM_BBO_001_15` | `YM-15M-BIASBO-c69d60` | BIAS_BO | stop | del motore | 251 | 255 | 100% | 82% |
| `PT8DAV_YM_BIA_001_30` | `YM-30M-BIAS-1187a8` | BIAS | market | del motore | 251 | 255 | 100% | 96% |
| `PT8DAV_YM_BRT_001_30` | `YM-30M-BIASRT-fb5c99` | BIAS_RT | limit | del motore | 224 | 228 | 100% | 96% |
| `PT8DAV_YM_BSW_001_60` | `YM-1H-BIASW-6e55f4` | BIASW | market | del motore | 22 | 21 | 95% | 71% |
| `PT8DAV_YM_LFD_001_30` | `YM-30M-LF-1ce7ca` | LF | market | intraday | 58 | 57 | 93% | 98% |
| `PT8DAV_YM_LFH_001_30` | `YM-30M-LFHL-fe246f` | LF_HL | market | overnight 1 giornate | 114 | 111 | 94% | 88% |
| `PT8DAV_YM_LFH_002_60` | `YM-1H-LFHL-1bef3a` | LF_HL | market | overnight 10 giornate | 2 | 3 | 0% | – |
| `PT8DAV_YM_LFH_003_240` | `YM-4H-LFHL-addb33` | LF_HL | market | overnight 3 giornate | 5 | 6 | 100% | 100% |
| `PT8DAV_YM_MAC_001_30` | `YM-30M-MAC-e55038` | MAC | market | del motore | 80 | 75 | 94% | 100% |
| `PT8DAV_YM_MAC_002_240` | `YM-4H-MAC-0f5aea` | MAC | market | del motore | 48 | 40 | 81% | 85% |
| `PT8DAV_YM_RBM_001_30` | `YM-30M-RBBM-31a116` | RBB_M | limit | intraday | 245 | 250 | 100% | 89% |
| `PT8DAV_YM_RBM_002_60` | `YM-1H-RBBM-bb13c2` | RBB_M | limit | intraday | 46 | 46 | 98% | 93% |
| `PT8DAV_YM_RBM_003_240` | `YM-4H-RBBM-9917ea` | RBB_M | limit | overnight 1 giornate | 23 | 23 | 100% | 91% |
| `PT8DAV_YM_RBU_001_60` | `YM-1H-RBBU-c35313` | RBB_U | limit | overnight 1 giornate | 48 | 50 | 100% | 94% |
| `PT8DAV_YM_RHL_001_30` | `YM-30M-RHL-80e5ab` | RHL | limit | overnight 10 giornate | 41 | 43 | 78% | 91% |
| `PT8DAV_YM_RHL_002_60` | `YM-1H-RHL-3e321d` | RHL | limit | overnight 2 giornate | 10 | 11 | 100% | 90% |
| `PT8DAV_YM_SBO_001_15` | `YM-15M-BO-2ef80f` | BO | stop | intraday | 23 | 23 | 96% | 95% |
| `PT8DAV_YM_TFM_001_30` | `YM-30M-TFM-7f684d` | TF_M | stop | intraday | 54 | 57 | 100% | 94% |
| `PT8DAV_YM_VBO_001_15` | `YM-15M-VBO-b3210f` | VBO | stop | intraday | 92 | 94 | 100% | 85% |
| `PT8DAV_YM_VBO_002_30` | `YM-30M-VBO-fac034` | VBO | stop | overnight 1 giornate | 46 | 46 | 98% | 87% |

## Riferimenti codice

- `Piootoo.Strategies/PT8DAVStrategies/` — le 111 classi; `tools/pt8dav/gen_pt8dav.py`
- `Piootoo.Strategies/PT5DAVStrategies/Engines/` — i motori: `Pt5DavEngineBase` (`Finish`,
  `BarMinutes`, `FoldToResearchBars`, `CountsMarketBarsInPosition`, `DailySessionsAtr`,
  `BarAtrBeforeCurrentBar`), `Pt5DavBreakoutEngines` (VBO), `Pt5DavBiasWeeklyEngine`,
  `Pt5DavMovingAverageCrossoverEngine`, `Pt5DavSessionState`
- `Piootoo.Strategies.Tests/Pt8DavConversionTests.cs`, `Pt5DavParityStudy.cs`
- `Piootoo.Core/Optimization/Sweep/` — `SweepJob.EntriesFromUtc`
- `piootoo-repository/run-engine-v3/` — consegna, analisi, `verifica/`
