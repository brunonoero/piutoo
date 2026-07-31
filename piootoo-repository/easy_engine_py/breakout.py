"""
Breakout N-sessioni -- rottura del massimo/minimo delle ultime N sessioni COMPLETE.

Generalizzazione del Trend Following: invece di rompere solo H_d1/L_d1 (1 sessione),
il livello di breakout e':
- LONG:  stop buy a max(H_d1 .. H_dN)  (+ offset opzionale)
- SHORT: stop sell a min(L_d1 .. L_dN) (- offset opzionale)

Con n_sess=1 degenera esattamente nel TF_M. Con n_sess>1 diventa un canale tipo
Donchian su sessioni complete (channel breakout).

Pattern: stesso schema mirrored di TF_M (neutrale + direzionale +-).
Filtri: time_window, day_filter, una sola entrata per sessione.

--- LEGGE ZERO (look-ahead) ---
- Il livello usa SOLO sessioni complete d1..dN  -> sempre sicuro.
- entry_type="stop": il simulatore riempie a max(open, livello) -> niente fill a
  livello "stantio" gia' superato (Tipo 1 prevenuto).
- lev_include_sess0=True: include il running high/low della sessione CORRENTE ma
  ESCLUDENDO la barra in corso (shift(1) dentro la sessione). Cosi' il livello a
  ogni barra i non contiene mai high[i]/low[i] -> niente look-ahead. Su daily,
  ogni sessione = 1 barra, quindi il running shiftato e' NaN e l'opzione e' inerte.
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


class BreakoutEngine(BaseEngine):
    name = "BO"
    description = "Breakout su N sessioni complete -- stop su max(H_d1..H_dN)/min(L_d1..L_dN), pattern mirrored"
    pattern_library = "neutral_directional"
    entry_type = "stop"

    def get_param_space(self) -> Dict[str, List[Any]]:
        neut_yes = neutral_values(55)
        neut_no  = neutral_values(56)
        dir_yes  = mirrored_dir_values(52)
        dir_no   = mirrored_dir_values(53)

        if self.is_daily:
            return {
                # FASE 0: struttura del canale di breakout
                # level_source=1 (H_d0 running) inerte su daily: 1 sessione = 1 barra
                "level_source":         [0],
                "n_sess":               [2, 1, 3, 4, 5],
                "lev_include_sess0":    [0],          # inerte su daily (vedi docstring)
                "breakout_offset_ticks": [0, 5, 10, 20],
                "intraday_only":        [0],          # su daily l'EOS chiuderebbe ogni barra
                # FASE 1-2: pattern
                "ptn_neut_yes": neut_yes,
                "ptn_neut_no":  neut_no,
                "ptn_dir_yes":  dir_yes,
                "ptn_dir_no":   dir_no,
                # FASE 3: filtri (orari inerti su daily)
                "start_hour":   [-1],
                "end_hour":     [-1],
                "skip_day":     [-1, 4],
                # FASE 4: risk management ($ / contratto)
                "stop_loss":    [3000, 1000, 2000, 5000, 8000],
                "take_profit":  [0, 2000, 4000, 6000, 10000, 15000],
                "max_bars":     [0, 5, 10, 20],   # giorni in posizione
            }

        return {
            # FASE 0: struttura del canale di breakout + uscita base
            # level_source: 0 = canale N sessioni complete (comportamento storico)
            #               1 = running H/L della sessione CORRENTE (EL MyTrigger=1:
            #                   nuovo massimo/minimo di sessione, anche sotto H_d1)
            "level_source":         [0, 1],
            "n_sess":               [2, 1, 3, 4, 5],
            "lev_include_sess0":    [0, 1],
            "breakout_offset_ticks": [0, 2, 5, 10],
            # intraday_only: 1 = flat a fine sessione (storico), 0 = tiene overnight
            # (famiglia TF multiday delle TOP_UA: S_674, FC_420, FGBL_736, JY_758)
            "intraday_only":        [1, 0],
            # FASE 1: pattern neutrale
            "ptn_neut_yes": neut_yes,
            "ptn_neut_no":  neut_no,
            # FASE 2: pattern direzionale
            "ptn_dir_yes":  dir_yes,
            "ptn_dir_no":   dir_no,
            # FASE 3: filtri orari
            "start_hour":   GRID_START_HOURS,
            "end_hour":     GRID_END_HOURS,
            "skip_day":     [-1, 4],
            # FASE 4: risk management
            "stop_loss":    GRID_STOP_LOSS,
            "take_profit":  GRID_TAKE_PROFIT,
            "max_bars":     multiday_max_bars(self.mc.get("timeframe_minutes", 60)),
        }

    def get_default_params(self) -> Dict[str, Any]:
        """
        Come TF: durante la sweep dei pattern serve un risk management di base non-zero,
        altrimenti su intraday il sistema esce sempre a fine sessione in perdita e tutti
        i pattern ottengono UngerFit=0 (vince il sentinel). Vedi SKILL.md sezione D.
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
        return [
            ["level_source", "n_sess", "lev_include_sess0", "breakout_offset_ticks",
             "intraday_only"],
            ["ptn_neut_yes", "ptn_neut_no"],
            ["ptn_dir_yes", "ptn_dir_no"],
            ["start_hour", "end_hour", "skip_day"],
            ["stop_loss", "take_profit", "max_bars"],
        ]

    def _breakout_levels(self, df: pd.DataFrame, n_sess: int,
                         include_sess0: bool, offset_ticks: int,
                         level_source: int = 0):
        """Livello stop LONG/SHORT. Solo sessioni complete (+ running sess0 sicuro)."""
        tick = self.mc.get("tick_size", 0.1)

        if int(level_source) == 1:
            # Trigger EL "MyTrigger=1" (TOP_UA 291/102/420/758): running H/L della
            # sessione corrente INCLUSA la barra in corso — il segnale emesso alla
            # barra i produce un ordine valido SOLO alla barra i+1 (vita-1-barra),
            # quindi high[i]/low[i] sono noti al momento dell'emissione: nessun
            # look-ahead. n_sess/lev_include_sess0 sono ignorati.
            level_long  = df.groupby("sess_id")["high"].transform("cummax")
            level_short = df.groupby("sess_id")["low"].transform("cummin")
            return (level_long + offset_ticks * tick,
                    level_short - offset_ticks * tick)

        # Limita n_sess alle colonne di sessione effettivamente disponibili nel df
        avail = max((k for k in range(1, 6) if f"H_d{k}" in df.columns), default=1)
        n_use = max(1, min(int(n_sess), avail))

        high_cols = [f"H_d{k}" for k in range(1, n_use + 1)]
        low_cols  = [f"L_d{k}" for k in range(1, n_use + 1)]
        level_long  = df[high_cols].max(axis=1)
        level_short = df[low_cols].min(axis=1)

        if include_sess0:
            # running high/low della sessione CORRENTE escludendo la barra in corso
            run_high = df.groupby("sess_id")["high"].transform(lambda s: s.cummax().shift(1))
            run_low  = df.groupby("sess_id")["low"].transform(lambda s: s.cummin().shift(1))
            level_long  = pd.concat([level_long, run_high], axis=1).max(axis=1)
            level_short = pd.concat([level_short, run_low], axis=1).min(axis=1)

        level_long  = level_long  + offset_ticks * tick
        level_short = level_short - offset_ticks * tick
        return level_long, level_short

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        # Pattern mask (uguale per L e S -- mirrored)
        ptn_n_yes = params.get("ptn_neut_yes", 55)
        ptn_n_no  = params.get("ptn_neut_no", 56)
        ptn_d_yes = params.get("ptn_dir_yes", 52)
        ptn_d_no  = params.get("ptn_dir_no", 53)

        neut_mask = pattern_neutral(df, ptn_n_yes) & ~pattern_neutral(df, ptn_n_no)
        dir_long  = pattern_directional(df, +ptn_d_yes) & ~pattern_directional(df, +ptn_d_no)
        dir_short = pattern_directional(df, -ptn_d_yes) & ~pattern_directional(df, -ptn_d_no)

        # Livelli di breakout: canale N sessioni complete o running H/L sess0
        n_sess        = params.get("n_sess", 2)
        include_sess0 = bool(params.get("lev_include_sess0", 0))
        offset_ticks  = params.get("breakout_offset_ticks", 0)
        level_source  = int(params.get("level_source", 0))
        level_long, level_short = self.memo(
            df, ("bo_levels", n_sess, include_sess0, offset_ticks, level_source),
            lambda: self._breakout_levels(
                df, n_sess, include_sess0, offset_ticks, level_source))

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

        # Emissione CONTINUA stile EL (segnale valido = livello non-NaN): fill
        # next-bar dal simulatore, 1 entrata/sessione/direzione via flag.
        base = tw & df_mask & neut_mask & level_long.notna() & level_short.notna()
        entries_long  = base & dir_long
        entries_short = base & dir_short

        return EngineSignals(
            entries_long=entries_long.fillna(False),
            entries_short=entries_short.fillna(False),
            entry_price_long=level_long,
            entry_price_short=level_short,
            entry_type="stop",
            exits_long=None,
            exits_short=None,
            single_entry_per_session=True,
            exit_on_session_end=(bool(params.get("intraday_only", 1))
                                 and not self.is_daily),
            notes="BO src={} n_sess={} sess0={} off={}t id={} ptn_n={}/{} ptn_d=+-{}/{}".format(
                level_source, n_sess, int(include_sess0), offset_ticks,
                int(params.get("intraday_only", 1)),
                ptn_n_yes, ptn_n_no, ptn_d_yes, ptn_d_no),
        )
