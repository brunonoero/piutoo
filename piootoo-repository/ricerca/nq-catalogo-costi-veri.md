# Le 39 strategie NQ del catalogo ai costi veri, sullo split — 22/09/2026

**Domanda:** fra le strategie NQ già in catalogo (PTS e PT2), quali reggono ai costi veri su un
fuori campione mai visto?

**Come:** ogni strategia com'è, nessuna ricerca (`--params` con un parametro neutro). Feed
**ICS** (aggregati 15/60/1440 derivati dal minuto il 22/09), orologio al minuto, campione
2022-01-01 → 2025-01-01, validazione 2025-01-01 → 2026-09-01, spread e swap **peggiori** fra ICS e
FTMO, commissione 19,23 per lato. Criterio di validazione: `net-over-dd`, 30 trade minimi, tenuta
≥ 50%, 3 finestre su 4 in utile. Dati in `nq-catalogo-costi-veri-ics.csv`; la versione sul feed
del vendor (`nq-catalogo-costi-veri.csv`) ha il fuori campione monco al 30/05/2025 e non vale.

## Il risultato

Sei passano la validazione, nessuna con il profilo della 002 del DAX (centinaia di trade da
entrambe le parti, utile da entrambe le parti):

| strategia | IS trade | IS netto | OOS trade | OOS netto | finestre | lettura |
|---|---:|---:|---:|---:|---:|---|
| `PT2_NQ_PCH_002_30` | 748 | **+2.281** | 413 | +64.037 | 3/4 | in campione è zero: il fuori campione è vento a favore, non edge |
| `PTS_NQ_TFU_003_15` | 195 | +16.578 | 128 | +41.872 | **4/4** | la più coerente, ma 195 trade in tre anni |
| `PTS_NQ_TFM_002_15` | 107 | +45.905 | 74 | +30.494 | 3/4 | pochi trade |
| `PTS_NQ_TFM_005_15` | 117 | +28.862 | 75 | +30.247 | 3/4 | pochi trade |
| `PTS_NQ_PCH_001_15` | 269 | +3.192 | 156 | +19.035 | 3/4 | in campione è zero |
| `PTS_NQ_PCH_002_15` | 205 | +125 | 112 | +7.513 | 3/4 | in campione è zero |

E le due con più trade, che sono le sole su cui una statistica dica qualcosa:

| strategia | IS trade | IS netto | OOS trade | OOS netto |
|---|---:|---:|---:|---:|
| `PT2_NQ_PCH_001_240` | 767 | +63.200 | 443 | **−47.114** |
| `PT2_NQ_PCH_002_30` | 748 | +2.281 | 413 | +64.037 |

La 4h Price Channel su NQ — la cella che la sweep al minuto aveva bocciato — **perde 47.114 fuori
campione** con 443 trade: la sweep aveva ragione, e la base che ieri sembrava la migliore (+218k sul
vendor senza costi) ai costi veri non regge. La 30 minuti fa il contrario, +64.037 fuori dopo un
campione a zero: due segni opposti sulla stessa famiglia sono la firma della varianza, non di due
strategie.

## Cosa se ne ricava

1. **Nessuna NQ del catalogo è un candidato oggi.** Le tre TF a 15 minuti (`TFU_003`, `TFM_002`,
   `TFM_005`) sono le uniche coerenti fra dentro e fuori, e sono le uniche da cui partire per uno
   step a mano — ma con 100-200 trade in tre anni un passo a mano rischia di tarare rumore. Prima
   di toccarle: le tre insieme, come mini-paniere, sullo stesso split.
2. **Il feed conta.** `PT2_NQ_PCH_001_240` in campione fa +54.324 sul vendor e +63.200 su ICS a
   parità di tutto: il CFD e il future non sono la stessa serie, e la ricerca va fatta sul feed su
   cui si opera.
3. **La rilettura N3 aveva ragione**: la cella NQ 4h PC è sottile ai costi veri. Non è un problema
   di filtri, è un «se».
4. Le PTS NQ sono state cercate sull'orologio veloce, sul feed del vendor, senza swap: che 33 su 39
   non reggano non dice niente sui motori, dice che quel processo di selezione non selezionava.

## Riferimenti

`nq-catalogo-costi-veri-ics.csv`, `nq-catalogo-costi-veri.csv` (vendor, fuori campione monco),
`docs/lavori-in-corso.md` (22/09).
