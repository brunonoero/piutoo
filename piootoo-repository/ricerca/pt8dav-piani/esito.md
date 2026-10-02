# Piani PT8DAV: composizione e verifica fuori campione (01/10/2026)

Da 111 strategie a due piani da conto. Il metodo tiene separate le due cose che di solito si
mescolano: **si sceglie e si compone su un tratto, si verifica su un altro che nessuna scelta ha
visto**.

## Il run neutro

`Pt8DavNeutralRunStudy` (studio, `PIOOTOO_STUDI=1`): le 111 a un contratto, motore vero, orologio al
minuto, feed FTMO, tenuta dei piani PT5DAV (overnight e overweek ammessi), livelli gia' scavalcati
come sul conto. Due versioni dei costi:

- `run-ftmo`: spread FTMO per ora UTC, swap FTMO, commissione 2;
- `run-fintokei`: spread e swap Fintokei, commissione 0, **sempre sul feed FTMO** (quello Fintokei
  comincia a fine 2025). Senza il caffe', che Fintokei non tratta.

28.567 trade (FTMO). Non e' passato dal server: quello installato non ha ancora le PT8DAV, e un
secondo server sullo stesso repository riprenderebbe le sessioni live.

## I due tratti

| tratto | periodo | cosa e' | in utile | netto (1 contratto) |
|---|---|---|---:|---:|
| A | 01/06/2022 → 31/05/2025 | quello su cui la ricerca ha filtrato, ma con il suo simulatore e i suoi costi | 85 / 111 | 1.679.709 |
| B | 01/06/2025 → 24/09/2026 | **mai visto** da nessuna scelta, ne' della ricerca ne' nostra | 63 / 111 | 1.113.544 |

Sul tratto A il nostro motore ai costi FTMO fa il 45% di quanto la ricerca dichiara sullo stesso
periodo (3,75 M): feed del CFD e costi veri.

## Le candidate (`giudizio.py`, `giudizio-run-ftmo.csv`)

Criterio, **sul solo tratto A**: almeno 30 trade, in utile, netto/drawdown ≥ 1. Passano **29**
strategie: BP 4, ES 5, FDAX 7, GC 4, NQ 7, YM 2; nessuna su CL e KC.

Il criterio predice: nel tratto B le 29 candidate fanno 821.012 (21 in utile, 28 k a strategia), le 82
scartate 292.532 (42 in utile, 3,6 k a strategia).

## I piani (`piani-ftmo-A/plan-builder.md`)

`piootoo-plan-builder` sulle 29 candidate, **composizione sul solo tratto A** (`--to 2025-06-01`),
vincoli di default: 3 piani disgiunti da 6 a 8 strategie, al massimo 2 per simbolo e 2 per famiglia,
correlazione giornaliera e nelle code ≤ 0,30. Nessun regime scoperto in nessuno dei tre.

## La verifica sul tratto B (`verifica_piani.py`)

Numeri a un contratto per strategia. **Sul conto da 100 k una strategia apre 0,1 × size del piano**:
i numeri si dividono per dieci a size 1.

| piano | tratto A: netto / DD / giorno peggiore | tratto B: netto / DD / giorno peggiore | netto/DD in B | oro nel netto B |
|---|---|---|---:|---:|
| **1** (8) | 314.565 / 20.027 / −7.744 | **286.598** / 62.886 / −10.226 | 4,56 | 83% |
| **2** (8) | 249.040 / 23.778 / −14.433 | **274.724** / 22.843 / −13.217 | 12,03 | 42% |
| 3 (6) | 290.796 / 44.660 / −22.215 | 138.935 / **118.223** / −27.037 | 1,18 | 7% |

Correlazione giornaliera fra i piani nel tratto B: 1-2 0,08; 1-3 0,04; 2-3 −0,08.

Ai costi Fintokei (stessi trade, cambia il costo): piano 1 tratto B 289.417 / DD 62.386, piano 2
280.103 / DD 22.533. Praticamente uguali.

### Piano 2 — tiene, ed e' il piu' equilibrato

`NQ_VBO_003_240`, `GC_VBO_001_15`, `ES_LFH_001_15`, `FDAX_RHL_002_60`, `ES_MAC_001_30`,
`YM_RBU_001_60`, `BP_PCH_001_60`, `BP_RBM_001_30`.

Fuori campione rende piu' che in campione (209 k l'anno contro 83 k) con lo stesso drawdown. Sei
strategie su otto guadagnano nel tratto B; le due sterline sono in pari (−2 k e +0,2 k: portano
trade, non denaro).
Tutti i trimestri del tratto B in utile. Senza l'oro il tratto B fa 158 k.
**Sul conto a size 1**: circa +27 k in 16 mesi, drawdown chiuso 2,3 k, giorno peggiore 1,3 k.

### Piano 1 — tiene, ma e' un piano sull'oro e va a meta' size

`FDAX_BRT_001_15`, `NQ_VBO_001_15`, `GC_LFD_001_60`, `ES_RHL_002_60`, `BP_VBO_001_15`,
`BP_SBO_001_15`, `GC_PCH_002_60`, `NQ_SBO_003_60`.

Il netto del tratto B viene per l'83% dalle due strategie sull'oro, nell'anno del rally; senza l'oro
sono 50 k. Il drawdown triplica rispetto al tratto A (62,9 k contro 20 k) ed e' quasi tutto di
`GC_PCH_002_60` (−48,8 k fra il 14/11/2025 e il 02/01/2026). Un trimestre in perdita (Q4 2025, −14 k).
**Sul conto a size 1** il drawdown chiuso sarebbe 6,3 k su un limite di 10 k: **size 0,5** — circa
+14 k in 16 mesi, drawdown 3,1 k, giorno peggiore 0,5 k.

### Piano 3 — non tiene, non si crea

Nel tratto B il drawdown (118 k) e' quasi il netto (139 k), che viene tutto da `NQ_BSW_001_240`
(+129 k: il long sul Nasdaq). `NQ_LFD_002_60` perde 32,6 k. Come per i piani 3 e 4 delle PT5DAV, un
terzo conto chiede strategie che qui non ci sono.

## Riserve

- **Il tratto B e' uno solo**, 16 mesi, con oro e Nasdaq in rally: «tiene» vuol dire che non si e'
  rotto fuori campione, non che rendera' cosi'.
- **Niente e' stato ritoccato guardando il tratto B**: ne' le candidate, ne' i membri dei piani. La
  size del piano 1 e' l'unica cosa decisa li', ed e' una regola del conto.
- I drawdown qui sono sul **chiuso giornaliero**. La perdita giornaliera sull'equity (la regola delle
  prop) si misura sul backtest con il piano (`tools/dd-giornaliero-ftmo`), dopo che i piani esistono.
- **Gemelli con i piani in produzione**: 72 celle delle PT8DAV esistono anche fra le PT5DAV con altri
  parametri. Su conti diversi della stessa prop va fatto il controllo dei trade simili prima
  (`ricerca/combinazioni-best-plan.ps1`); candidati ovvi: `GC_PCH_002_60` con `PT5DAV_GC_PCH_003_60`
  (P1), `NQ_VBO_003_240`, `FDAX_RHL_002_60` con le RHL del DAX.
- Nessuna FDAX a 4 ore sta nei piani 1 e 2: la verifica del cBot su quelle resta a parte.
- `PT8DAV_ES_MAC_001_30` (piano 2): la MAC della ricerca inverte, la nostra no. Qui si misura la
  nostra.

## Prossimi passi

1. Rilascio del server con le PT8DAV (cambia anche il comportamento delle PT5DAV nei piani attivi).
2. Workspace `ftmo-pt8dav` e `fintokei-pt8dav`, piani `…-P1` (size 0,5) e `…-P2` (size 1), tenuta e
   commissione dei gemelli PT5DAV.
3. Backtest interno con il piano, perdita giornaliera sull'equity.
4. Backtest cBot in cTrader: FTMO dal 01/06/2025, Fintokei dal 25/12/2025 (prima cTrader non ha lo
   storico degli indici `_P`).
5. Confronto cBot contro interno, e solo dopo il demo.
