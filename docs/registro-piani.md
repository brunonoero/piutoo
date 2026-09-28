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

- **Combinazioni su una prop**: EUROPA + USA + O4-X05 (tre conti), oppure INDICI + O4-X05 (due conti).
  Mai INDICI con EUROPA o USA, mai O4/O4-X05 con P1 o ORO (vedi la tabella sotto).
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
