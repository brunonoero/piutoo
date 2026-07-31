"""
TF Unmirrored -- Trend Following con pattern indipendenti L/S (PatternFast).

Stesse entry di TF_M (stop su H_d1/L_d1) ma con pattern_fast separati per long e short.
"""
import pandas as pd
from typing import Dict, List, Any
from .base import (
    BaseEngine, EngineSignals,
    GRID_START_HOURS, GRID_END_HOURS, GRID_STOP_LOSS, GRID_TAKE_PROFIT,
    multiday_max_bars, fast_values,
)
from ..patterns import pattern_fast
from ..filters import time_window, day_filter


class TFUnmirroredEngine(BaseEngine):
    name = "TF_U"
    description = "Trend Following unmirrored -- entry stop H_d1/L_d1, pattern fast L/S indipendenti"
    pattern_library = "fast"
    entry_type = "stop"

    def get_param_space(self) -> Dict[str, List[Any]]:
        # Sweep COMPLETA della libreria fast (152 pattern; era un subset di 17 —
        # correzione 5, 2026-07-07: TOP_UA_156 usa 54/75/111/31, tutti fuori dal
        # vecchio subset, e batteva il nostro pick su stesso motore+dati).
        # LEGGE ZERO: entry stop con ordine vita-1-barra e fill gap-aware
        # max(open, livello) -> i pattern d0 (valutati alla chiusura della barra
        # segnale) non producono fill impossibili.
        fast_yes = fast_values(152)   # include 153 -> one-sided raggiungibile
        fast_no  = fast_values(153)

        if self.is_daily:
            return {
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
            # FASE 0: uscita base -- 1 = flat a fine sessione, 0 = multiday
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

    def get_default_params(self) -> Dict[str, Any]:
        """
        Override dei default: durante la sweep dei pattern (Fasi 1-2) il risk management
        usa valori di base non-zero. Senza stop/target il motore TF su intraday e
        sistematicamente in perdita (exit a fine sessione) -- tutti i pattern ottengono
        UngerScore=0 -- vince sempre il sentinel (primo elemento della lista).
        Con un risk management di base ragionevole i pattern possono emergere correttamente,
        come da schema Unger (SKILL.md sezione D).
        """
        defaults = {k: v[0] for k, v in self.get_param_space().items()}
        if self.is_daily:
            defaults["stop_loss"]   = 3000
            defaults["take_profit"] = 6000
        else:
            defaults["stop_loss"]   = 1000
            defaults["take_profit"] = 3000
        defaults["max_bars"] = 0
        return defaults

    def get_optimization_phases(self) -> List[List[str]]:
        if self.is_daily:
            return [
                ["ptn_ly_yes", "ptn_ly_no"],
                ["ptn_sy_yes", "ptn_sy_no"],
                ["start_hour", "end_hour", "skip_day"],
                ["stop_loss", "take_profit", "max_bars"],
            ]
        return [
            ["intraday_only"],
            ["ptn_ly_yes", "ptn_ly_no"],
            ["ptn_sy_yes", "ptn_sy_no"],
            ["start_hour", "end_hour", "skip_day"],
            ["stop_loss", "take_profit", "max_bars"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
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
            sh_str = "00:00" if sh < 0 else "{:02d}:00".format(sh)
            eh_str = "23:59" if eh < 0 else "{:02d}:00".format(eh)
            tw = time_window(df, sh_str, eh_str)
        df_mask = day_filter(df, params.get("skip_day", -1))

        # Emissione CONTINUA stile EL (vedi TF_M): fill next-bar dal simulatore,
        # 1 entrata/sessione/direzione via single_entry_per_session.
        base_long  = tw & df_mask & mask_long
        base_short = tw & df_mask & mask_short

        return EngineSignals(
            entries_long=base_long.fillna(False),
            entries_short=base_short.fillna(False),
            entry_price_long=df["H_d1"],
            entry_price_short=df["L_d1"],
            entry_type="stop",
            single_entry_per_session=True,
            exit_on_session_end=(bool(params.get("intraday_only", 1))
                                 and not self.is_daily),
            notes="TF_U id={} ly={}/{} sy={}/{}".format(
                int(params.get("intraday_only", 1)), ly_y, ly_n, sy_y, sy_n),
        )
