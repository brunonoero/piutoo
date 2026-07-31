"""
TF Mirrored -- Trend Following con pattern simmetrici (neutrale + direzionale +-).

Entrata LONG:  stop buy a H_d1 (massimo sessione precedente)
Entrata SHORT: stop sell a L_d1 (minimo sessione precedente)

Pattern logica:
- pattern_neutral(ptn_neut_yes) deve essere True (stesso per long e short)
- pattern_neutral(ptn_neut_no) deve essere False
- pattern_directional(+ptn_dir_yes) True per long, -ptn_dir_yes True per short
- pattern_directional(+ptn_dir_no) False per long, -ptn_dir_no False per short

Filtri: time_window, day_filter, una sola entrata per sessione.
"""
import pandas as pd
import numpy as np
from typing import Dict, List, Any
from .base import (
    BaseEngine, EngineSignals,
    GRID_START_HOURS, GRID_END_HOURS, GRID_STOP_LOSS, GRID_TAKE_PROFIT,
    mirrored_dir_values, neutral_values, multiday_max_bars,
)
from ..patterns import pattern_neutral, pattern_directional
from ..filters import time_window, day_filter


class TFMirroredEngine(BaseEngine):
    name = "TF_M"
    description = "Trend Following mirrored -- entry stop su H_d1/L_d1, pattern simmetrico"
    pattern_library = "neutral_directional"
    entry_type = "stop"

    def get_param_space(self) -> Dict[str, List[Any]]:
        neut_yes = neutral_values(55)
        neut_no  = neutral_values(56)
        dir_yes  = mirrored_dir_values(52)
        dir_no   = mirrored_dir_values(53)

        if self.is_daily:
            # Su D1: nessun filtro orario (le barre sono tutte a mezzanotte),
            # stop/target calibrati su ATR giornaliero, max_bars in giorni.
            return {
                "ptn_neut_yes": neut_yes,
                "ptn_neut_no":  neut_no,
                "ptn_dir_yes":  dir_yes,
                "ptn_dir_no":   dir_no,
                "start_hour":   [-1],          # inutile su daily -- fisso a no-filter
                "end_hour":     [-1],
                "skip_day":     [-1, 4],
                "stop_loss":    [3000, 1000, 2000, 5000, 8000],    # $ / contratto
                "take_profit":  [0, 2000, 4000, 6000, 10000, 15000],
                "max_bars":     [0, 5, 10, 20],   # giorni in posizione
            }

        return {
            # FASE 0: uscita base -- 1 = flat a fine sessione, 0 = multiday
            "intraday_only": [1, 0],
            # FASE 1: pattern neutrale (corpo, range)
            "ptn_neut_yes": neut_yes,
            "ptn_neut_no":  neut_no,
            # FASE 2: pattern direzionale (segno della tendenza)
            "ptn_dir_yes":  dir_yes,
            "ptn_dir_no":   dir_no,
            # FASE 3: filtri orari (window di trading)
            "start_hour":   GRID_START_HOURS,   # -1 = no filtro
            "end_hour":     GRID_END_HOURS,
            "skip_day":     [-1, 4],   # -1=no filtro, 4=venerdi escluso
            # FASE 4: risk management ($ per contratto)
            "stop_loss":    GRID_STOP_LOSS,
            "take_profit":  GRID_TAKE_PROFIT,
            "max_bars":     multiday_max_bars(self.mc.get("timeframe_minutes", 60)),
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
                ["ptn_neut_yes", "ptn_neut_no"],
                ["ptn_dir_yes", "ptn_dir_no"],
                ["start_hour", "end_hour", "skip_day"],
                ["stop_loss", "take_profit", "max_bars"],
            ]
        return [
            ["intraday_only"],
            ["ptn_neut_yes", "ptn_neut_no"],
            ["ptn_dir_yes", "ptn_dir_no"],
            ["start_hour", "end_hour", "skip_day"],
            ["stop_loss", "take_profit", "max_bars"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        # Pattern mask (uguale per L e S)
        ptn_n_yes = params.get("ptn_neut_yes", 55)
        ptn_n_no  = params.get("ptn_neut_no", 56)
        ptn_d_yes = params.get("ptn_dir_yes", 52)
        ptn_d_no  = params.get("ptn_dir_no", 53)

        neut_mask = pattern_neutral(df, ptn_n_yes) & ~pattern_neutral(df, ptn_n_no)
        dir_long  = pattern_directional(df, +ptn_d_yes) & ~pattern_directional(df, +ptn_d_no)
        dir_short = pattern_directional(df, -ptn_d_yes) & ~pattern_directional(df, -ptn_d_no)

        # Filtri orari
        sh = int(params.get("start_hour", -1))
        eh = int(params.get("end_hour", -1))
        if sh < 0 and eh < 0:
            tw = pd.Series(True, index=df.index)
        else:
            sh_str = "00:00" if sh < 0 else "{:02d}:00".format(sh)
            eh_str = "23:59" if eh < 0 else "{:02d}:00".format(eh)
            tw = time_window(df, sh_str, eh_str)

        # Filtro giorno
        df_mask = day_filter(df, params.get("skip_day", -1))

        # Emissione CONTINUA stile EL: ordine ri-emesso a ogni barra valida
        # (condizioni valutate alla chiusura della barra, fill next-bar dal
        # simulatore); 1 entrata/sessione/direzione via single_entry_per_session.
        base = tw & df_mask & neut_mask
        entries_long  = base & dir_long
        entries_short = base & dir_short

        return EngineSignals(
            entries_long=entries_long.fillna(False),
            entries_short=entries_short.fillna(False),
            entry_price_long=df["H_d1"],
            entry_price_short=df["L_d1"],
            entry_type="stop",
            exits_long=None,
            exits_short=None,
            single_entry_per_session=True,
            exit_on_session_end=(bool(params.get("intraday_only", 1))
                                 and not self.is_daily),
            notes="TF_M id={} ptn_n={}/{} ptn_d=+-{}/{}".format(
                int(params.get("intraday_only", 1)),
                ptn_n_yes, ptn_n_no, ptn_d_yes, ptn_d_no),
        )
