"""Reversal BB Unmirrored — come RBB_M ma con pattern_fast indipendenti L/S."""
import pandas as pd
from typing import Dict, List, Any
from .base import (
    BaseEngine, EngineSignals,
    GRID_START_HOURS, GRID_END_HOURS, GRID_STOP_LOSS, GRID_TAKE_PROFIT,
    mirrored_dir_values, multiday_max_bars, fast_values,
)
from ..patterns import pattern_fast
from ..filters import time_window, day_filter, bollinger_bands


class ReversalBBUnmirroredEngine(BaseEngine):
    name = "RBB_U"
    description = "Reversal BB unmirrored — entry limit su BB, pattern fast L/S indipendenti"
    pattern_library = "fast"
    entry_type = "limit"

    def get_param_space(self) -> Dict[str, List[Any]]:
        # Sweep COMPLETA della libreria fast (era un subset di 16 — correzione 5).
        # LEGGE ZERO: entry limit con ordine vita-1-barra -> pattern valutati
        # alla chiusura della barra segnale, fill dalla barra dopo.
        fast_yes = fast_values(152)   # include 153 -> long-only NQ_181 raggiungibile
        fast_no  = fast_values(153)

        if self.is_daily:
            return {
                "bb_length":   [20, 10, 14, 30, 50],
                "bb_num_devs": [2.0, 1.5, 2.5, 3.0],
                "ptn_ly_yes": fast_yes,
                "ptn_ly_no":  fast_no,
                "ptn_sy_yes": fast_yes,
                "ptn_sy_no":  fast_no,
                "start_hour": [-1],
                "end_hour":   [-1],
                "skip_day":   [-1, 4],
                "stop_loss":   [3000, 1000, 2000, 5000, 8000],
                "take_profit": [0, 2000, 4000, 6000, 10000, 15000],
                "max_bars":    [0, 5, 10, 20],
            }

        return {
            "bb_length":   [20, 10, 14, 30, 50],
            "bb_num_devs": [2.0, 1.5, 2.5, 3.0],
            # intraday_only: 1 = flat a fine sessione (storico), 0 = multiday
            # (mean reversion multiday: GC_416 tiene fino a 5 sessioni)
            "intraday_only": [1, 0],
            "ptn_ly_yes": fast_yes,
            "ptn_ly_no":  fast_no,
            "ptn_sy_yes": fast_yes,
            "ptn_sy_no":  fast_no,
            "start_hour": GRID_START_HOURS,
            "end_hour":   GRID_END_HOURS,
            "skip_day":   [-1, 4],
            "stop_loss":   GRID_STOP_LOSS,
            "take_profit": GRID_TAKE_PROFIT,
            "max_bars":    multiday_max_bars(self.mc.get("timeframe_minutes", 60)),
        }

    def get_optimization_phases(self) -> List[List[str]]:
        if self.is_daily:
            return [
                ["bb_length", "bb_num_devs"],
                ["ptn_ly_yes", "ptn_ly_no"],
                ["ptn_sy_yes", "ptn_sy_no"],
                ["start_hour", "end_hour", "skip_day"],
                ["stop_loss", "take_profit", "max_bars"],
            ]
        return [
            ["bb_length", "bb_num_devs", "intraday_only"],
            ["ptn_ly_yes", "ptn_ly_no"],
            ["ptn_sy_yes", "ptn_sy_no"],
            ["start_hour", "end_hour", "skip_day"],
            ["stop_loss", "take_profit", "max_bars"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        bb_len = params.get("bb_length", 20)
        bb_dev = params.get("bb_num_devs", 2.0)
        # memo: le bande dipendono solo da (bb_len, bb_dev) -> nelle fasi pattern
        # si ricalcolavano identiche a ogni combo. Niente df.assign: copiava
        # l'intero DataFrame a ogni chiamata (i pattern non usano le colonne bb).
        bb = self.memo(df, ("bb", bb_len, bb_dev),
                       lambda: bollinger_bands(df, length=bb_len, num_devs=bb_dev,
                                               src="close"))
        bb_dn, bb_up = bb["bb_dn"], bb["bb_up"]

        # Emissione CONTINUA stile EL (vedi RBB_M): condizioni valutate alla barra
        # di emissione, fill next-bar dal simulatore; 1 entrata/sessione via flag.
        armed_dn = df["close"] > bb_dn
        armed_up = df["close"] < bb_up

        ly_y = params.get("ptn_ly_yes", 152)
        ly_n = params.get("ptn_ly_no", 153)
        sy_y = params.get("ptn_sy_yes", 152)
        sy_n = params.get("ptn_sy_no", 153)

        mask_long  = pattern_fast(df, ly_y) & ~pattern_fast(df, ly_n)
        mask_short = pattern_fast(df, sy_y) & ~pattern_fast(df, sy_n)

        sh = int(params.get("start_hour", -1))
        eh = int(params.get("end_hour", -1))
        if sh < 0 and eh < 0:
            tw = pd.Series(True, index=df.index)
        else:
            tw = time_window(
                df,
                "00:00" if sh < 0 else f"{sh:02d}:00",
                "23:59" if eh < 0 else f"{eh:02d}:00",
            )
        df_mask = day_filter(df, params.get("skip_day", -1))

        base = tw & df_mask
        entries_long  = base & armed_dn & mask_long
        entries_short = base & armed_up & mask_short

        return EngineSignals(
            entries_long=entries_long.fillna(False),
            entries_short=entries_short.fillna(False),
            entry_price_long=bb_dn,
            entry_price_short=bb_up,
            entry_type="limit",
            single_entry_per_session=True,
            exit_on_session_end=(bool(params.get("intraday_only", 1))
                                 and not self.is_daily),
            notes="RBB_U bb_len={} dev={} id={}".format(
                bb_len, bb_dev, int(params.get("intraday_only", 1))),
        )
