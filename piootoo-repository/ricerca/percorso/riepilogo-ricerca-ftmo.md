# Ricerca solo sul feed FTMO — riepilogo (28/09/2026)

Percorso v4 con la ricerca sul feed FTMO 11/2020 → 09/2024 e la prova sul broker 09/2024 → 09/2026, mai
vista dal percorso; controlli automatici (anni, due feed dove c'e' il feed interno, altri mercati). Dodici
motori per cella: PCH, TFM, TFU, BO, BOS, VBO, MAC, RBM, RBU, RHL, LF, LFHL.

| cella | resoconto | esito |
|---|---|---|
| FDAX 4h | `fdax-240-ricerca-ftmo.md` | **RHL** → `PT3B_FDAX_RHL_001_240` e la stessa regola su NQ, ES, YM, FESX (controllo RAN superato) |
| GC 4h, GC 1h | `gc-240.md`, `gc-60.md` | nessuna |
| CL 4h | `cl-240-ricerca-ftmo.md` | nessuna |
| CL 1h | `cl-60-ricerca-ftmo.md` | nessuna: sulla prova il migliore e' RBM con net/DD 1,21 su 29 trade e quattro cancelli falliti |
| BP 4h | `bp-240-ricerca-ftmo.md` | nessuna |
| BP 1h | `bp-60-ricerca-ftmo.md` | nessuna: average trade 6-20 contro soglia 38; BOS in utile 4/4 finestre ma 11 a trade |
| SI 4h | `si-240-ricerca-ftmo.md` | nessuna: MAC net/DD 0,47 sulla prova, LFHL in perdita |
| SI 1h | `si-60-ricerca-ftmo.md` | nessuna: TFM (+70.909 su 28 trade) e MAC (+162.593 su 52) sulla prova, ma falliscono anni, outlier e altri mercati; segnale di regime (rally dell'argento 2025-26), non sistema |

Conclusione: fuori dagli indici la ricerca solo FTMO non ha prodotto candidate. Le celle con molti
trade (BP) hanno un vantaggio per trade che i costi si mangiano; quelle con pochi trade (SI, CL) passano
la prova su campioni troppo piccoli o per effetto del regime.

## Secondo giro: indici con il flat prima del rollover (29-30/09/2026)

Stesso percorso, con la tenuta dei piani PT3B: niente overnight, flat alle 20:45 UTC per 30 minuti (copre il rollover
FTMO). Resoconti con suffisso `-flat`. Ogni candidata passa poi tre verifiche prima di una classe: il caso (100 ingressi
casuali con le stesse uscite, serve il 96° percentile), la storia lunga sul feed interno, la regola identica sugli altri
indici.

| cella | candidata | verifiche | esito |
|---|---|---|---|
| FDAX 4h | RHL, stessi parametri del giro senza flat | prova net/DD 3,86 (era 3,20), 6/7 indici | conferma di `PT3B_FDAX_RHL_001_240` |
| FDAX 1h | PCH long mattutino | caso 22°, 2008-2020 net/DD 0,28 | chiusa: e' la mattina rialzista |
| ES 4h, YM 4h, FESX 4h, NIY 4h | - | - | nessuna |
| NQ 4h | BOS (average trade 450 contro 483) | caso 72-89°, 2008-2022 net/DD 0,04 | chiusa |
| ES 1h | **RHL** | caso 98-100°, 2006-2020 14/15 anni, riproduzione esatta | **`PT3B_ES_RHL_002_60`, piano `FTMO-PT3B-USA-2`** |
| YM 1h | BOS (fallisce solo altri mercati) | caso 99-100° sul Dow; S&P 2006-2020 in perdita, 51° | sospesa: manca il Dow orario prima del 2020 |
| FESX 1h, NIY 1h, NQ 1h | - | - | nessuna |

La regola RHL dell'S&P a 1 ora non regge su Nasdaq (2008-2022 net/DD 0,36) e Dow (caso 91°).

Conclusioni. Il flat cambia i numeri nella direzione attesa (la BOS del DAX 4h passa da net/DD 0,15 a 1,55 sulla prova,
la PCH del DAX 1h da -14.155 a +117.277), ma tre candidate su quattro cadono sul caso o sulla storia lunga: i cancelli
della ricerca, calcolati su un 2020-2024 rialzista, non separano un segnale dalla deriva degli indici. Il caso e la storia
lunga si fanno prima di proporre una classe. Il Nikkei, a 4 ore e a 1 ora, da' prove fortissime (net/DD 6-9) che
falliscono anni e altri mercati: e' il rally giapponese 2024-2026.
