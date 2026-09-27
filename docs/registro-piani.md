# Registro dei piani di trading

Scheda di ogni `TradingPlan` esistente: a cosa serve, cosa contiene, com'e' andato. **Si aggiorna a
ogni piano creato, modificato o abbandonato.** La fonte vera resta `plans/plans.json` del workspace:
se questa pagina e il file non concordano, ha ragione il file.

Stato al **25/09/2026**. Server acceso: `GET api/v1/workspaces/{ws}/trading-plans` elenca i piani
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
