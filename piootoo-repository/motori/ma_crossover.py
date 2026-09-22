"""
MA Crossover -- trend following su incrocio di due medie mobili (SMA) della close.

- LONG:  FastSMA incrocia SOPRA SlowSMA  + filtro gradiente + daily-setup verde
- SHORT: FastSMA incrocia SOTTO SlowSMA  + filtro gradiente + daily-setup rosso
- Uscita: incrocio inverso (reverse), Friday-EOD (intraday), stop.

Tiene le posizioni OVERNIGHT (esce solo su reverse/stop/Friday) -> il motore forza
exit_on_session_end=False anche su timeframe intraday.

Fonte EL: TOP_UA 772 (CL 60m).

--- LEGGE ZERO ---
L'incrocio usa la close CORRENTE (FastSMA/SlowSMA includono close[i]); l'entrata EL e'
"next bar at market". Quindi il segnale (entrata E uscita) viene SHIFTATO di 1 barra:
deciso alla chiusura di bar k, eseguito su bar k+1. Niente look-ahead.
- entrata market -> simulatore entra a open[k+1].
- uscita reverse -> forced exit a close[k+1] (il simulatore esce in close; ~1 barra
  conservativa vs l'open[k+1] di EL, ma LEGGE ZERO pulita).
- daily-setup usa C_d1/O_d1/H_d1/L_d1 (sessione di IERI, completa) -> sicuro.
"""
import pandas as pd
import numpy as np
from typing import Dict, List, Any
from .base import BaseEngine, EngineSignals
from ..filters import day_filter


class MACrossoverEngine(BaseEngine):
    name = "MAC"
    description = "MA Crossover -- incrocio FastSMA/SlowSMA + filtro gradiente + daily-setup, hold overnight"
    pattern_library = "none"
    entry_type = "market"

    def get_param_space(self) -> Dict[str, List[Any]]:
        base = {
            # FASE 1: periodi medie (Fast < Slow)
            "fast": [12, 5, 8, 16, 20],
            "slow": [24, 20, 30, 40, 50],
            # FASE 2: filtro gradiente (0 = off; evita incroci piatti)
            "gradient_length": [2, 1, 3, 5],
            "gradient_factor": [1.6, 0.0, 0.8, 1.2, 2.0],
            # FASE 3: daily-setup (|C_d1-O_d1|/(H_d1-L_d1) <= dfactor; 1.0 = solo verde/rosso)
            "daily_factor": [0.5, 0.3, 0.7, 1.0],
            # FASE 4: direzione
            "direction": [0, 1, 2],
            # FASE 5: risk (l'uscita primaria e' il reverse cross; stop = sicurezza)
            "stop_loss":     [1500, 1000, 2000, 3000],
            "take_profit":   [0, 2000, 4000, 6000],
            "trailing_stop": [0, 1000, 2000, 4000],
        }
        return base

    def get_optimization_phases(self) -> List[List[str]]:
        return [
            ["fast", "slow"],
            ["gradient_length", "gradient_factor"],
            ["daily_factor"],
            ["direction"],
            ["stop_loss", "take_profit", "trailing_stop"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        fast = int(params.get("fast", 12))
        slow = int(params.get("slow", 24))
        g    = max(1, int(params.get("gradient_length", 2)))
        gfac = float(params.get("gradient_factor", 1.6))
        dfac = float(params.get("daily_factor", 0.5))
        direction = int(params.get("direction", 0))

        false_s = pd.Series(False, index=df.index)
        open_s  = df["open"]

        # Fast >= Slow non ha senso per un crossover -> nessun segnale.
        if fast >= slow:
            return EngineSignals(
                entries_long=false_s, entries_short=false_s,
                entry_price_long=open_s, entry_price_short=open_s,
                entry_type="market", exit_on_session_end=False,
                notes=f"MAC degenerate fast>={slow}",
            )

        close = df["close"]
        # memo: le SMA dipendono solo dalla lunghezza (audit efficienza 2026-07-27)
        fast_sma = self.memo(df, ("sma", fast),
                             lambda: close.rolling(fast, min_periods=fast).mean())
        slow_sma = self.memo(df, ("sma", slow),
                             lambda: close.rolling(slow, min_periods=slow).mean())

        # Incroci (calcolati sulla close corrente)
        prev_up = fast_sma.shift(1) <= slow_sma.shift(1)
        prev_dn = fast_sma.shift(1) >= slow_sma.shift(1)
        cross_above = (fast_sma > slow_sma) & prev_up
        cross_below = (fast_sma < slow_sma) & prev_dn

        # Filtro gradiente: il Fast deve muoversi >= gfac volte lo Slow su g barre.
        grad = (fast_sma - fast_sma.shift(g)).abs() >= gfac * (slow_sma - slow_sma.shift(g)).abs()

        # Daily-setup sulla sessione di IERI (d1). Denominatore 0 (giorno piatto) -> NaN -> fail.
        denom = (df["H_d1"] - df["L_d1"]).replace(0, np.nan)
        body_ratio = (df["C_d1"] - df["O_d1"]).abs() / denom
        indecision = body_ratio <= dfac
        daily_long  = indecision & (df["C_d1"] > df["O_d1"])   # giorno di indecisione ma verde
        daily_short = indecision & (df["C_d1"] < df["O_d1"])   # ... ma rosso

        raw_long  = (cross_above & grad & daily_long).fillna(False)
        raw_short = (cross_below & grad & daily_short).fillna(False)

        # LEGGE ZERO: shift(1) -- deciso a close[k], eseguito a bar k+1.
        entries_long  = raw_long.shift(1, fill_value=False)
        entries_short = raw_short.shift(1, fill_value=False)

        # Uscite: reverse cross (shiftato come l'entrata) + Friday-EOD (intraday).
        exits_long  = cross_below.fillna(False).shift(1, fill_value=False)
        exits_short = cross_above.fillna(False).shift(1, fill_value=False)
        if not self.is_daily:
            last_bar = df["sess_id"] != df["sess_id"].shift(-1)
            friday_eod = last_bar & (df.index.dayofweek == 4)
            exits_long  = exits_long  | friday_eod
            exits_short = exits_short | friday_eod

        entries_long, entries_short = self.apply_direction(entries_long, entries_short, direction)

        return EngineSignals(
            entries_long=entries_long,
            entries_short=entries_short,
            entry_price_long=open_s,
            entry_price_short=open_s,
            entry_type="market",
            exits_long=exits_long,
            exits_short=exits_short,
            exit_on_session_end=False,   # MAC tiene le posizioni overnight
            notes="MAC f={} s={} gL={} gF={} dF={} dir={}".format(
                fast, slow, g, gfac, dfac, direction),
        )
