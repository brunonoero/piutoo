# Simulazione Titano sul backtest a due symbol — 21/08/2026

Sorgente: `piootoo-repository/trades.jsonl` (432 trade, 19 strategie, NQ+GC,
25/06/2025 → 03/02/2026). Il report completo è `titano-analisi-2026-08-21.html`.

## Cosa c'è dentro

| File | Cosa fa |
|---|---|
| `titano.py` | Simulatore della logica Titano v2 fedele a `docs/domini/titano-rotation.md` e all'audit del 31/07 §1–§3: equity line per strategia, i 5 voti (esito + punteggio), i cancelli assoluti ON/OFF con isteresi/cooldown/minOnPeriods/hard stop, il sizing per percentile cross-sezionale, il no-look-ahead con decisione del periodo N applicata a N+1. |
| `baseline` (in `analyze.py`) | Statistiche del backtest senza filtro, per symbol e per strategia. |
| `grid.py` | Campionamento casuale di 12.000 configurazioni su 15 dimensioni, su variante raw e a rischio normalizzato. Scrive `grid_results.json`. |
| `analyze.py` | Frontiera, classifiche, sensibilità per parametro. |
| `wf.py` | Walk-forward onesto: taglio IS/OOS al 01/11/2025, correlazione fra Calmar IS e OOS. |
| `dd.py` | Anatomia del drawdown massimo (concentrazione per giorno, per strategia, coda della distribuzione). |
| `regime.py` | Correlazione fra strategie e interruttore di portafoglio (240 combinazioni). |
| `fast.py` | Limite di perdita giornaliero e Titano a cadenza giornaliera. |
| `wf_daily.py` | Walk-forward sulle 9.000 configurazioni a cadenza giornaliera. |
| `final.py` | Confronto finale delle varianti e sweep di robustezza. |

## Come si esegue

```
python3 grid.py 12000      # ~2 min su 2 core
python3 analyze.py
python3 wf.py              # ~7 min
python3 wf_daily.py        # ~16 min
python3 final.py
```

Il percorso del `trades.jsonl` è in `titano.SRC`.

## I tre risultati

1. **Nessuna delle 10.515 configurazioni valide batte la baseline su netto e drawdown
   insieme.** Correlazione fra Calmar in-sample e out-of-sample: **+0,017**. Il criterio
   di selezione non trasporta informazione.
2. **Il drawdown massimo (34.031) si forma in tre sedute** — 3, 10 e 22 ottobre 2025,
   l'86% del totale — dentro un mese chiuso a +12.622. Durata mediana di un trade:
   4,1 ore. Una rotazione settimanale non può vederlo.
3. **A cadenza giornaliera con `MinimumPassingFilters = 5`** la correlazione IS→OOS sale
   a **+0,282** e 49 config su 50 battono la baseline fuori campione. Il limite di perdita
   giornaliero (livello veloce, non Titano) è l'unico intervento che migliora
   contemporaneamente netto e drawdown.
