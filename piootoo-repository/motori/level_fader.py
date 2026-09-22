"""
Level Fader -- reversal su rientro del prezzo dentro un livello (fade del falso breakout).

Port fedele di s__UA_LevelFader (Andrea Unger). NON usa ordini limit: il segnale e' un
CROSSOVER della close su un livello di sessione, l'entrata e' MARKET sulla barra
SUCCESSIVA. Questo evita del tutto la "trappola limit-order" del simulatore.

Livelli (calcolati a inizio sessione dalla sessione precedente d1):
- LevelChoice=1 (Pivot):  pivot=(H_d1+L_d1+C_d1)/3,  R1=2*pivot-L_d1,  S1=2*pivot-H_d1
                          LE(long) = S1 - shift*tick   SE(short) = R1 + shift*tick
- LevelChoice=2 (H/L):    LE(long) = L_d1 - shift*tick SE(short) = H_d1 + shift*tick

Segnale (al close della barra i, "fade" del falso breakout):
- LONG:  close[i-1] < LE  AND  close[i] > LE   (prezzo rientra SOPRA il livello basso)
- SHORT: close[i-1] > SE  AND  close[i] < SE   (prezzo rientra SOTTO il livello alto)
Entrata: MARKET alla barra i+1 (segnale shiftato di 1).

Pattern (come nel sorgente EL):
- PatternNeutralFast(neut_yes) & not PatternNeutralFast(neut_no)   -- condivisi L/S
- PatternDirectionalFast(+dir_yes) per LONG, (-dir_yes) per SHORT  -- mirrored
- UAPtnBase(ly_yes) & not UAPtnBase(ly_no)   per LONG
- UAPtnBase(sy_yes) & not UAPtnBase(sy_no)   per SHORT

--- LEGGE ZERO (look-ahead) ---
Il segnale usa C_d0 (close corrente) e puo' usare pattern su d0, MA l'entrata e' sulla
barra SUCCESSIVA (shift(1) sempre, intraday e daily). Tutto cio' che si usa alla barra i
e' noto al close di i; si entra all'open di i+1 -> nessun look-ahead. I livelli vengono
solo da d1 (sessione completa) -> sicuri.
"""
import pandas as pd
import numpy as np
from typing import Dict, List, Any
from .base import (
    BaseEngine, EngineSignals,
    GRID_START_HOURS, GRID_END_HOURS, GRID_STOP_LOSS, GRID_TAKE_PROFIT,
    mirrored_dir_values, neutral_values,
    uaptnbase_values,
)
from ..patterns import pattern_neutral, pattern_directional, pattern_uaptnbase
from ..filters import time_window, day_filter


class LevelFaderEngine(BaseEngine):
    name = "LF"
    description = "Level Fader -- fade del falso breakout, crossover close su livello + entry market next bar"
    pattern_library = "uaptnbase"
    entry_type = "market"

    def get_param_space(self) -> Dict[str, List[Any]]:
        neut_yes = neutral_values(55)
        neut_no  = neutral_values(56)
        dir_yes  = mirrored_dir_values(52)
        # Sweep COMPLETA uaptnbase (era un subset 14/8 — completata 2026-07-11,
        # stesso principio della correzione 5 su fast: griglie piene, il 42
        # nelle *_yes rende raggiungibili i setup one-sided).
        uap_yes  = uaptnbase_values(41)
        uap_no   = uaptnbase_values(42)

        if self.is_daily:
            return {
                "level_choice": [1, 2],
                "level_shift":  [0, 5, 10, 20],
                "ptn_neut_yes": neut_yes,
                "ptn_neut_no":  neut_no,
                "ptn_dir_yes":  dir_yes,
                "ptn_ly_yes":   uap_yes,
                "ptn_ly_no":    uap_no,
                "ptn_sy_yes":   uap_yes,
                "ptn_sy_no":    uap_no,
                "start_hour":   [-1],
                "end_hour":     [-1],
                "not_le_day":   [-1, 0, 1, 2, 3, 4],
                "not_se_day":   [-1, 0, 1, 2, 3, 4],
                "stop_loss":    [3000, 1000, 2000, 5000, 8000],
                "take_profit":  [0, 2000, 4000, 6000, 10000, 15000],
                "max_bars":     [0, 5, 10, 20],
            }

        return {
            # FASE 1: livelli
            "level_choice": [1, 2],
            "level_shift":  [0, 2, 5, 10],
            # FASE 2: pattern neutrale
            "ptn_neut_yes": neut_yes,
            "ptn_neut_no":  neut_no,
            # FASE 3: pattern direzionale (mirrored)
            "ptn_dir_yes":  dir_yes,
            # FASE 4: uaptnbase long
            "ptn_ly_yes":   uap_yes,
            "ptn_ly_no":    uap_no,
            # FASE 5: uaptnbase short
            "ptn_sy_yes":   uap_yes,
            "ptn_sy_no":    uap_no,
            # FASE 6: filtri
            "start_hour":   GRID_START_HOURS,
            "end_hour":     GRID_END_HOURS,
            "not_le_day":   [-1, 0, 1, 2, 3, 4],
            "not_se_day":   [-1, 0, 1, 2, 3, 4],
            # FASE 7: risk management
            "stop_loss":    GRID_STOP_LOSS,
            "take_profit":  GRID_TAKE_PROFIT,
            "max_bars":     [0, 12, 24, 48],
        }

    def get_default_params(self) -> Dict[str, Any]:
        defaults = {k: v[0] for k, v in self.get_param_space().items()}
        # Un risk management di base evita che durante la sweep pattern tutto esca
        # solo a fine sessione (vedi nota in TF/BIAS).
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
                ["level_choice", "level_shift"],
                ["ptn_neut_yes", "ptn_neut_no"],
                ["ptn_dir_yes"],
                ["ptn_ly_yes", "ptn_ly_no"],
                ["ptn_sy_yes", "ptn_sy_no"],
                ["not_le_day", "not_se_day"],
                ["stop_loss", "take_profit", "max_bars"],
            ]
        return [
            ["level_choice", "level_shift"],
            ["ptn_neut_yes", "ptn_neut_no"],
            ["ptn_dir_yes"],
            ["ptn_ly_yes", "ptn_ly_no"],
            ["ptn_sy_yes", "ptn_sy_no"],
            # orari e giorni separati: insieme facevano 22.500 combo in una fase
            # sola (stessa ragione dello split stop/target su PC e VBO).
            ["start_hour", "end_hour"],
            ["not_le_day", "not_se_day"],
            ["stop_loss", "take_profit", "max_bars"],
        ]

    def _levels(self, df: pd.DataFrame, level_choice: int, level_shift: int):
        """Trigger LONG (LE) e SHORT (SE), costanti entro la sessione (da d1)."""
        tick = self.mc.get("tick_size", 0.1)
        H1, L1, C1 = df["H_d1"], df["L_d1"], df["C_d1"]
        if level_choice == 1:
            pivot = (H1 + L1 + C1) / 3.0
            r1 = 2 * pivot - L1
            s1 = 2 * pivot - H1
            le_trig = s1 - level_shift * tick
            se_trig = r1 + level_shift * tick
        else:  # level_choice == 2
            le_trig = L1 - level_shift * tick
            se_trig = H1 + level_shift * tick
        return le_trig, se_trig

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        # Pattern
        ptn_n_yes = params.get("ptn_neut_yes", 55)
        ptn_n_no  = params.get("ptn_neut_no", 56)
        ptn_d_yes = params.get("ptn_dir_yes", 52)
        ly_y = params.get("ptn_ly_yes", 41)
        ly_n = params.get("ptn_ly_no", 42)
        sy_y = params.get("ptn_sy_yes", 41)
        sy_n = params.get("ptn_sy_no", 42)

        neut_mask = pattern_neutral(df, ptn_n_yes) & ~pattern_neutral(df, ptn_n_no)
        dir_long  = pattern_directional(df, +ptn_d_yes)
        dir_short = pattern_directional(df, -ptn_d_yes)
        uap_long  = pattern_uaptnbase(df, ly_y) & ~pattern_uaptnbase(df, ly_n)
        uap_short = pattern_uaptnbase(df, sy_y) & ~pattern_uaptnbase(df, sy_n)

        # Livelli + crossover (fade)
        level_choice = params.get("level_choice", 1)
        level_shift  = params.get("level_shift", 0)
        le_trig, se_trig = self._levels(df, level_choice, level_shift)

        prev_close = df["close"].shift(1)
        cross_up   = (prev_close < le_trig) & (df["close"] > le_trig)   # rientro sopra livello basso
        cross_down = (prev_close > se_trig) & (df["close"] < se_trig)   # rientro sotto livello alto

        # Filtri (valutati alla barra di segnale)
        sh = int(params.get("start_hour", -1))
        eh = int(params.get("end_hour", -1))
        if sh < 0 and eh < 0:
            tw = pd.Series(True, index=df.index)
        else:
            sh_str = "00:00" if sh < 0 else "{:02d}:00".format(sh)
            eh_str = "23:59" if eh < 0 else "{:02d}:00".format(eh)
            tw = time_window(df, sh_str, eh_str)
        day_l = day_filter(df, params.get("not_le_day", -1))
        day_s = day_filter(df, params.get("not_se_day", -1))

        sig_long  = neut_mask & dir_long  & uap_long  & cross_up   & tw & day_l
        sig_short = neut_mask & dir_short & uap_short & cross_down & tw & day_s

        # LEGGE ZERO: entrata market alla barra SUCCESSIVA -> shift(1) sempre.
        # fill_value=False mantiene il dtype bool (niente NaN intermedi).
        entries_long  = sig_long.shift(1, fill_value=False)
        entries_short = sig_short.shift(1, fill_value=False)

        return EngineSignals(
            entries_long=entries_long,
            entries_short=entries_short,
            entry_price_long=df["open"],
            entry_price_short=df["open"],
            entry_type="market",
            exits_long=None,
            exits_short=None,
            notes="LF lvl={} shift={} ptn_n={}/{} dir=+-{} uap_l={}/{} uap_s={}/{} [shift1]".format(
                level_choice, level_shift, ptn_n_yes, ptn_n_no, ptn_d_yes,
                ly_y, ly_n, sy_y, sy_n),
        )
