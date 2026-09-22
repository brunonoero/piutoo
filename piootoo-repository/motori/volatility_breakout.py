"""
Volatility Breakout -- breakout dall'APERTURA di sessione di k volte una misura di volatilita'.

Archetipo unico che copre 3 strategie Unger Academy su NQ:
- S3 "range sessione precedente": vol_source=1 -> livello = O_d0 +- k*(H_d1-L_d1)
- S4 "breakout con ATR":          vol_source=3 -> livello = O_d0 +- k*ATR(barre)
- S1 "canale di volatilita'":     vol_source=2 -> livello = O_d0 +- k*ATR_giornaliero(data2)

- LONG:  stop buy a O_d0 + k*VOL
- SHORT: stop sell a O_d0 - k*VOL
Filtro momentum opzionale, pattern, direzione selezionabile, breakeven/trailing.

--- LEGGE ZERO ---
- O_d0 (apertura sessione) e' noto dalla prima barra -> sicuro.
- VOL viene SEMPRE da dati completi: (H_d1-L_d1) sessione chiusa; session_atr(shift=1)
  sessioni chiuse; atr(barre).shift(1) barre chiuse -> nessun look-ahead.
- entry stop -> fill a max(open, livello) nel simulatore.
"""
import pandas as pd
import numpy as np
from typing import Dict, List, Any
from .base import (
    BaseEngine, EngineSignals,
    GRID_START_HOURS, GRID_END_HOURS, GRID_STOP_LOSS, GRID_TAKE_PROFIT,
    mirrored_dir_values, neutral_values,
)
from ..patterns import pattern_neutral, pattern_directional
from ..filters import time_window, day_filter
from ..indicators import atr, session_atr


class VolatilityBreakoutEngine(BaseEngine):
    name = "VBO"
    description = "Volatility Breakout dall'open di sessione -- O_d0 +- k*VOL (range/ATR/ATR daily), momentum, direzione"
    pattern_library = "neutral_directional"
    entry_type = "stop"

    def get_param_space(self) -> Dict[str, List[Any]]:
        neut_yes = neutral_values(55)
        neut_no  = neutral_values(56)
        dir_yes  = mirrored_dir_values(52)
        dir_no   = mirrored_dir_values(53)

        if self.is_daily:
            # su daily ha poco senso (open=barra), ma lo lasciamo coerente
            return {
                "vol_source":  [1, 2, 3],
                "vol_mult":    [0.5, 0.3, 0.7, 1.0],
                "vol_mult_short": [-1.0, 0.3, 0.5, 0.7, 1.0],
                "atr_len":     [14, 5, 10],
                "momentum":    [0, 1, 2],
                "direction":   [0, 1, 2],
                "ptn_neut_yes": neut_yes, "ptn_neut_no": neut_no,
                "ptn_dir_yes":  dir_yes,  "ptn_dir_no":  dir_no,
                "start_hour":  [-1], "end_hour": [-1], "skip_day": [-1, 4],
                "stop_loss":   [3000, 1000, 2000, 5000],
                "take_profit": [0, 2000, 4000, 6000, 10000],
                "max_bars":    [0, 5, 10, 20],
                "trailing_stop": [0, 2000, 4000],
                "breakeven":     [0, 1000, 2000],
            }

        return {
            # FASE 0: definizione del livello di breakout.
            # Estensioni 2026-07-11 (audit NQ): le TOP_UA VBO usano ATR lunghi
            # (342: ATR(200), 486: ATR(500)) con moltiplicatori grandi e
            # ASIMMETRICI long/short (342: 4/9.5, 486: 6/8.5, 796: 0.35 daily).
            # vol_mult_short = -1 -> stesso moltiplicatore del long (simmetrico).
            "vol_source":  [1, 2, 3],          # 1=range d1, 2=ATR daily, 3=ATR barre
            "vol_mult":    [0.5, 0.3, 0.7, 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.5, 9.5],
            "vol_mult_short": [-1.0, 0.3, 0.5, 0.7, 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.5, 9.5],
            "atr_len":     [14, 5, 10, 20, 100, 200, 500],
            # FASE 1: momentum + direzione
            "momentum":    [0, 1, 2],          # 0=off, 1=C_d1 vs C_d2, 2=O_d0 vs C_d1
            "direction":   [0, 1, 2],
            # FASE 2-3: pattern
            "ptn_neut_yes": neut_yes, "ptn_neut_no": neut_no,
            "ptn_dir_yes":  dir_yes,  "ptn_dir_no":  dir_no,
            # FASE 4: filtri orari/giorno
            "start_hour":  GRID_START_HOURS, "end_hour": GRID_END_HOURS, "skip_day": [-1, 4],
            # FASE 5: risk management
            "stop_loss":   GRID_STOP_LOSS,
            "take_profit": GRID_TAKE_PROFIT,
            "max_bars":    [0, 12, 24, 48],
            "trailing_stop": [0, 500, 1000, 2000],
            "breakeven":     [0, 500, 1000],
        }

    def get_default_params(self) -> Dict[str, Any]:
        defaults = {k: v[0] for k, v in self.get_param_space().items()}
        if self.is_daily:
            defaults["stop_loss"], defaults["take_profit"] = 3000, 6000
        else:
            defaults["stop_loss"], defaults["take_profit"] = 1500, 3000
        defaults["max_bars"] = 0
        defaults["trailing_stop"] = 0
        defaults["breakeven"] = 0
        return defaults

    def get_optimization_phases(self) -> List[List[str]]:
        return [
            ["vol_source", "vol_mult", "vol_mult_short", "atr_len"],
            ["momentum", "direction"],
            ["ptn_neut_yes", "ptn_neut_no"],
            ["ptn_dir_yes", "ptn_dir_no"],
            ["start_hour", "end_hour", "skip_day"],
            # §14.B.4 dice "affinare stop e target": trailing e breakeven sono
            # una NOSTRA aggiunta e nella stessa fase facevano 8.112 combo, cioe'
            # un massimo pescato su un campione enorme (fortuna che entra dalla
            # porta principale). Separati: 416 + 12 = un ordine di grandezza in
            # meno di selezione, e piu' fedeli al metodo.
            ["stop_loss", "take_profit", "max_bars"],
            ["trailing_stop", "breakeven"],
        ]

    def _vol(self, df: pd.DataFrame, vol_source: int, atr_len: int) -> pd.Series:
        if vol_source == 2:
            return session_atr(df, atr_len, shift=1)
        if vol_source == 3:
            return atr(df, atr_len).shift(1)
        return (df["H_d1"] - df["L_d1"])   # 1 = range sessione precedente

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        ptn_n_yes = params.get("ptn_neut_yes", 55)
        ptn_n_no  = params.get("ptn_neut_no", 56)
        ptn_d_yes = params.get("ptn_dir_yes", 52)
        ptn_d_no  = params.get("ptn_dir_no", 53)
        neut_mask = pattern_neutral(df, ptn_n_yes) & ~pattern_neutral(df, ptn_n_no)
        dir_long  = pattern_directional(df, +ptn_d_yes) & ~pattern_directional(df, +ptn_d_no)
        dir_short = pattern_directional(df, -ptn_d_yes) & ~pattern_directional(df, -ptn_d_no)

        vol_source = int(params.get("vol_source", 1))
        vol_mult   = float(params.get("vol_mult", 0.5))
        # Moltiplicatore short separato (TOP_UA 342/486 sono asimmetrici);
        # -1 (default) = simmetrico, stesso moltiplicatore del long.
        vol_mult_s = float(params.get("vol_mult_short", -1.0))
        if vol_mult_s < 0:
            vol_mult_s = vol_mult
        atr_len    = int(params.get("atr_len", 14))
        vol = self._vol(df, vol_source, atr_len)
        level_long  = df["O_d0"] + vol_mult * vol
        level_short = df["O_d0"] - vol_mult_s * vol

        # Momentum (conferma direzionale dalla sessione precedente / gap di apertura)
        momentum = int(params.get("momentum", 0))
        if momentum == 1:
            mom_long  = df["C_d1"] > df["C_d2"]
            mom_short = df["C_d1"] < df["C_d2"]
        elif momentum == 2:
            mom_long  = df["O_d0"] > df["C_d1"]
            mom_short = df["O_d0"] < df["C_d1"]
        else:
            mom_long  = pd.Series(True, index=df.index)
            mom_short = pd.Series(True, index=df.index)

        sh = int(params.get("start_hour", -1))
        eh = int(params.get("end_hour", -1))
        if sh < 0 and eh < 0:
            tw = pd.Series(True, index=df.index)
        else:
            sh_str = "00:00" if sh < 0 else "{:02d}:00".format(sh)
            eh_str = "23:59" if eh < 0 else "{:02d}:00".format(eh)
            tw = time_window(df, sh_str, eh_str)
        df_mask = day_filter(df, params.get("skip_day", -1))

        # Emissione CONTINUA stile EL: fill next-bar, 1 entrata/sessione/direzione.
        base = tw & df_mask & neut_mask & level_long.notna() & level_short.notna()
        entries_long  = (base & dir_long  & mom_long).fillna(False)
        entries_short = (base & dir_short & mom_short).fillna(False)
        entries_long, entries_short = self.apply_direction(
            entries_long, entries_short, params.get("direction", 0))

        return EngineSignals(
            entries_long=entries_long,
            entries_short=entries_short,
            entry_price_long=level_long,
            entry_price_short=level_short,
            entry_type="stop",
            exits_long=None, exits_short=None,
            single_entry_per_session=True,
            notes="VBO src={} k={}/{} atr={} mom={} dir={} ptn_n={}/{} ptn_d=+-{}/{}".format(
                vol_source, vol_mult, vol_mult_s, atr_len, momentum,
                params.get("direction", 0),
                ptn_n_yes, ptn_n_no, ptn_d_yes, ptn_d_no),
        )
