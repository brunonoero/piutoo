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
