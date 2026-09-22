"""
Reversal BB Mirrored — Mean Reversion con Bollinger Bands.

Entrata LONG:  close crossover banda inferiore BB (limit buy a bb_dn)
Entrata SHORT: close crossunder banda superiore BB (limit sell a bb_up)

Pattern: NEUTRALE + DIRECTIONAL inverso (segno opposto perché è reversal):
  +PtnDirYes per long se direzionale ribassista (cattura il fondo)
  -PtnDirYes per short se direzionale rialzista (cattura il top)
"""
import pandas as pd
from typing import Dict, List, Any
from .base import (
    BaseEngine, EngineSignals,
    GRID_START_HOURS, GRID_END_HOURS, GRID_STOP_LOSS, GRID_TAKE_PROFIT,
    mirrored_dir_values, neutral_values, multiday_max_bars,
)
from ..patterns import pattern_neutral, pattern_directional
from ..filters import time_window, day_filter, bollinger_bands


class ReversalBBMirroredEngine(BaseEngine):
    name = "RBB_M"
    description = "Reversal Bollinger mirrored — entry limit su BB up/dn, pattern speculare"
    pattern_library = "neutral_directional"
    entry_type = "limit"

    def get_param_space(self) -> Dict[str, List[Any]]:
        neut_yes = neutral_values(55)
        neut_no  = neutral_values(56)
        dir_yes  = mirrored_dir_values(52)
        dir_no   = mirrored_dir_values(53)

        if self.is_daily:
            return {
                "bb_length":    [10, 14, 20, 30, 50],
                "bb_num_devs":  [1.5, 2.0, 2.5, 3.0],
                "ptn_neut_yes": neut_yes,
                "ptn_neut_no":  neut_no,
                "ptn_dir_yes":  dir_yes,
                "ptn_dir_no":   dir_no,
                "start_hour":   [-1],
                "end_hour":     [-1],
                "skip_day":     [-1, 4],
                "stop_loss":    [3000, 1000, 2000, 5000, 8000],
                "take_profit":  [0, 2000, 4000, 6000, 10000, 15000],
                "max_bars":     [0, 5, 10, 20],
            }

        return {
            # FASE 1: BB params + uscita base (multiday MR: GC_416 tiene 5 sessioni)
            "bb_length":   [10, 14, 20, 30, 50],
            "bb_num_devs": [1.5, 2.0, 2.5, 3.0],
            "intraday_only": [1, 0],
            # FASE 2: pattern neutrali
            "ptn_neut_yes": neut_yes,
            "ptn_neut_no":  neut_no,
            # FASE 3: pattern direzionale (segno INVERSO: long con dir negativa = reversal)
            "ptn_dir_yes":  dir_yes,
            "ptn_dir_no":   dir_no,
            # FASE 4: filtri
            "start_hour":   GRID_START_HOURS,
            "end_hour":     GRID_END_HOURS,
            "skip_day":     [-1, 4],
            # FASE 5: risk
            "stop_loss":    GRID_STOP_LOSS,
            "take_profit":  GRID_TAKE_PROFIT,
            "max_bars":     multiday_max_bars(self.mc.get("timeframe_minutes", 60)),
        }

    def get_optimization_phases(self) -> List[List[str]]:
        if self.is_daily:
            return [
                ["bb_length", "bb_num_devs"],
                ["ptn_neut_yes", "ptn_neut_no"],
                ["ptn_dir_yes", "ptn_dir_no"],
                ["start_hour", "end_hour", "skip_day"],
                ["stop_loss", "take_profit", "max_bars"],
            ]
        return [
            ["bb_length", "bb_num_devs", "intraday_only"],
            ["ptn_neut_yes", "ptn_neut_no"],
            ["ptn_dir_yes", "ptn_dir_no"],
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

        # Emissione CONTINUA stile EL: finche' la close sta sopra bb_dn (long) /
        # sotto bb_up (short), a ogni barra si ri-emette "next bar at banda limit".
        # Il fill (= il cross) avviene alla barra successiva se il prezzo penetra
        # il livello (simulatore next-bar). Prima il segnale ERA il cross stesso
        # con fill sulla stessa barra -> pattern d0 valutati sulla barra di fill
        # (look-ahead); ora ogni condizione e' valutata alla chiusura della barra
        # di emissione. Una-entrata-per-sessione via single_entry_per_session.
        armed_dn = df["close"] > bb_dn
        armed_up = df["close"] < bb_up

        ptn_n_y = params.get("ptn_neut_yes", 55)
        ptn_n_n = params.get("ptn_neut_no", 56)
        ptn_d_y = params.get("ptn_dir_yes", 52)
        ptn_d_n = params.get("ptn_dir_no", 53)

        neut_mask = pattern_neutral(df, ptn_n_y) & ~pattern_neutral(df, ptn_n_n)
        # Reversal: long usa segno NEGATIVO del directional (perché vogliamo la fase ribassista)
        dir_long  = pattern_directional(df, -ptn_d_y) & ~pattern_directional(df, -ptn_d_n)
        dir_short = pattern_directional(df, +ptn_d_y) & ~pattern_directional(df, +ptn_d_n)

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

        base = tw & df_mask & neut_mask
        entries_long  = base & armed_dn & dir_long
        entries_short = base & armed_up & dir_short

        return EngineSignals(
            entries_long=entries_long.fillna(False),
            entries_short=entries_short.fillna(False),
            entry_price_long=bb_dn,
            entry_price_short=bb_up,
            entry_type="limit",
            single_entry_per_session=True,
            exit_on_session_end=(bool(params.get("intraday_only", 1))
                                 and not self.is_daily),
            notes=f"RBB_M len={bb_len} dev={bb_dev} id={int(params.get('intraday_only', 1))} "
                  f"ptn_n={ptn_n_y}/{ptn_n_n} ptn_d=±{ptn_d_y}",
        )
