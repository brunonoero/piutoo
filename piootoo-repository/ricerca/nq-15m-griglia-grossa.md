# Griglia grossa NQ 15 minuti — motore nudo, 450 combinazioni, 22/09/2026

**Cella:** Price Channel su @NQ a 15 minuti, contenitore `PT3B_NQ_PCH_001_15` (motore nudo:
pattern alle sentinelle, nessun filtro orario, niente trailing). Leve: canale {1, 20, 50} × stop
{500…4000} × target {0…8000} × uscita {fine sessione, 21} × direzione {0, 1, 2}. Feed ICS, spread
peggiore ICS/FTMO 1,45, swap peggiore (long 6,74 pt/notte), commissione 19,23 per lato, orologio al
minuto. Campione 2022-01 → 2025-01, validazione → 2026-09. 220 minuti su 8 core. Dati in
`nq-15m-griglia-grossa.csv`, generati da `NqCoarseGridTests`.

## Il risultato

- **92 celle su 450** ammissibili (≥ 250 trade in campione e campione in utile).
- **30** in utile fuori campione con ≥ 3 finestre su 4.
- **Una sola equilibrata** (netto/DD ≥ 1 sia dentro sia fuori):

| can | stop | targ | exit | dir | IS n | IS netto | IS DD | OOS n | OOS netto | OOS DD | fin |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 20 | 4000 | 8000 | −1 | 0 | 947 | 112.289 | 95.839 | 590 | 82.265 | 72.010 | 4/4 |

Le altre "migliori fuori campione" hanno il **campione a zero** (3-15k su 600-1.300 trade) e
40-148k fuori: è lo stesso profilo della `PT2_NQ_PCH_002_30` del catalogo — vento del 2025-26,
non edge. Le migliori *in* campione (stop 4000, 75-112k) sono per lo più negative fuori.

Per leva, media sulle ammissibili:

| leva | IS medio | OOS medio |
|---|---:|---:|
| uscita a fine sessione | 21.973 | 9.188 |
| **uscita alle 21** | 21.768 | **16.586** |
| direzione entrambe | 36.517 | 13.340 |
| solo long | 15.365 | 17.337 |
| **solo short** | 11.567 | **−2.189** |
| stop 500 | 18.847 | −6.839 |
| stop 1000 | 11.473 | 53.134 |
| stop 4000 | 36.478 | 7.496 |

## Letta con i criteri del metodo (skill `metodo-unger`, §8.2 e §13.6)

Sull'unica cella equilibrata, a un contratto:

- **Average trade**: IS $119, OOS $139. Soglie NQ: 6-7 tick = ~$32 (passa); 15% del $ATR
  ($800) = ~$120 (**al limite**). Con lo sconto walk-forward ×0,2-0,5 l'atteso live è $25-60:
  **sotto soglia**.
- **Guadagno annuo / max DD** (filtro Titan, soglia 2): IS ~37k/anno contro 96k = **0,39**; OOS
  ~49k/anno contro 72k = **0,69**. Lontano da 2.
- **UngerFit** = √(E × 100/R) con E = 119/120 ≈ 1,0 e R = 95.839/119 ≈ 805 trade per
  recuperare il DD peggiore: √(1,0 × 0,124) = **0,35**. Sotto 1 = non accettabile.

## Conclusione

**Su NQ 15 minuti il Price Channel nudo non ha un edge che regga gli standard del metodo.** La
cella non è "sbagliata" — 947 trade con +112k in campione e +82k fuori, quattro finestre su
quattro, sono numeri veri — ma il drawdown per unità di guadagno è troppo grande: servono 800
trade per recuperare il peggior drawdown, e l'average trade netto sta sul filo della soglia
prima ancora dello sconto. Coerente con le 39 del catalogo (le sole coerenti erano TF a 15
minuti con avg trade da $250-400, non PC) e con la sweep NQ 4h.

Due leve però parlano chiaro anche qui: **l'uscita alle 21 aiuta fuori campione** (16,6k contro
9,2k di media) e **lo short su NQ perde** (media OOS negativa). Se si torna su NQ, è con un
motore diverso dal PC — le TF a 15 minuti del catalogo indicano dove — e non con più filtri sul PC.

## Riferimenti

`nq-15m-griglia-grossa.csv`, `nq-catalogo-costi-veri.md`, `nq-4h-pc-tutto-al-minuto.md`,
`Piootoo.Strategies.Tests/NqCoarseGridTests.cs`, `metodo/metodo-unger/SKILL.md` §13.6.
