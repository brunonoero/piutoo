"""
Price Channel (Donchian) -- breakout del massimo/minimo delle ultime N BARRE rolling.

Differenza da BO (Breakout N-sessioni): BO rompe il canale delle ultime N SESSIONI
complete; qui il canale e' sulle ultime N BARRE del timeframe corrente (Donchian/Price
Channel classico, es. 20 barre a 30 min). E' l'archetipo "Trend Following Price Channel"
descritto da Unger Academy (NQ 30m + filtro volatilita' daily).

- LONG:  stop buy a max(high, ultime N barre complete) + offset
- SHORT: stop sell a min(low,  ultime N barre complete) - offset
Filtro volatilita' "data2": opzionale floor sull'ATR giornaliero (session_atr).
Direzione selezionabile (NQ e' long-biased -> spesso solo long).

--- LEGGE ZERO ---
- emissione alla chiusura della barra i (canale include la barra i, come
  highest(high,N) EL); il FILL avviene solo dalla barra i+1 (simulatore
  next-bar) -> nessun Tipo 1.
- entry stop -> il simulatore riempie a max(open, livello) -> niente fill "stantio".
- `session_atr(shift=1)` usa solo sessioni chiuse -> filtro daily sicuro.
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
from ..indicators import donchian, session_atr


class PriceChannelEngine(BaseEngine):
    name = "PC"
    description = "Price Channel/Donchian -- stop su max/min ultime N barre, filtro vol daily, direzione selezionabile"
    pattern_library = "neutral_directional"
    entry_type = "stop"

    def get_param_space(self) -> Dict[str, List[Any]]:
        neut_yes = neutral_values(55)
        neut_no  = neutral_values(56)
        dir_yes  = mirrored_dir_values(52)
        dir_no   = mirrored_dir_values(53)

        if self.is_daily:
            return {
                "channel_len":           [20, 1, 10, 15, 30, 40, 55],
                "breakout_offset_ticks": [0, 5, 10, 20],
                "direction":             [0, 1, 2],
                "dvol_min":              [0],            # ATR daily gia' implicito su daily
                "ptn_neut_yes": neut_yes, "ptn_neut_no": neut_no,
                "ptn_dir_yes":  dir_yes,  "ptn_dir_no":  dir_no,
                "start_hour":   [-1], "end_hour": [-1], "skip_day": [-1, 4],
                "stop_loss":    [3000, 1000, 2000, 5000, 8000],
                "take_profit":  [0, 2000, 4000, 6000, 10000, 15000],
                "max_bars":     [0, 5, 10, 20],
                "trailing_stop": [0, 2000, 4000, 8000],
                "breakeven":     [0, 1000, 2000],
            }

        return {
            # FASE 0: struttura del canale + uscita base.
            # channel_len esteso a 75/100/155: TOP_UA_336 usa Donchian 155 —
            # con max 50 il suo canale lungo era irraggiungibile (backlog 06-26).
            # channel_len=1 aggiunto 2026-07-11: NQ_531 = breakout della SINGOLA
            # barra precedente a ora fissa (start_hour == end_hour).
            "channel_len":           [20, 1, 10, 15, 30, 40, 50, 75, 100, 155],
            "breakout_offset_ticks": [0, 2, 5, 10],
            # intraday_only: 1 = flat a fine sessione (storico), 0 = multiday
            "intraday_only":         [1, 0],
            # FASE 1: direzione + filtro volatilita' daily ($ ATR giornaliero minimo)
            "direction":             [0, 1, 2],
            "dvol_min":              [0, 3000, 6000],
            # FASE 2-3: pattern
            "ptn_neut_yes": neut_yes, "ptn_neut_no": neut_no,
            "ptn_dir_yes":  dir_yes,  "ptn_dir_no":  dir_no,
            # FASE 4: filtri orari/giorno
            "start_hour":   GRID_START_HOURS, "end_hour": GRID_END_HOURS,
            "skip_day":     [-1, 4],
            # FASE 5: risk management
            "stop_loss":    GRID_STOP_LOSS,
            "take_profit":  GRID_TAKE_PROFIT,
            "max_bars":     multiday_max_bars(self.mc.get("timeframe_minutes", 60)),
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
        # direction in FASE 0 (2026-07-14, diagnosi Donchian-155 su NQ): la
        # direzione fa parte del trigger sui canali. Giudicare channel_len con
        # direction=0 forzata dimezzava il punteggio del ramo long-only sugli
        # indici long-biased (155 long-only q=4.5 vs 2.6 both) e la fase 0
        # sceglieva il canale sbagliato.
        if self.is_daily:
            return [
                ["channel_len", "breakout_offset_ticks", "direction"],
                ["dvol_min"],
                ["ptn_neut_yes", "ptn_neut_no"],
                ["ptn_dir_yes", "ptn_dir_no"],
                ["start_hour", "end_hour", "skip_day"],
                # stop/target separati da trailing/breakeven: vedi VBO — 16.224
                # combo in una fase sola sono troppa selezione per §14.B.4.
                ["stop_loss", "take_profit", "max_bars"],
                ["trailing_stop", "breakeven"],
            ]
        return [
            ["channel_len", "breakout_offset_ticks", "intraday_only", "direction"],
            ["dvol_min"],
            ["ptn_neut_yes", "ptn_neut_no"],
            ["ptn_dir_yes", "ptn_dir_no"],
            ["start_hour", "end_hour", "skip_day"],
            ["stop_loss", "take_profit", "max_bars"],
            ["trailing_stop", "breakeven"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        ptn_n_yes = params.get("ptn_neut_yes", 55)
        ptn_n_no  = params.get("ptn_neut_no", 56)
        ptn_d_yes = params.get("ptn_dir_yes", 52)
        ptn_d_no  = params.get("ptn_dir_no", 53)
        neut_mask = pattern_neutral(df, ptn_n_yes) & ~pattern_neutral(df, ptn_n_no)
        dir_long  = pattern_directional(df, +ptn_d_yes) & ~pattern_directional(df, +ptn_d_no)
        dir_short = pattern_directional(df, -ptn_d_yes) & ~pattern_directional(df, -ptn_d_no)

        n = int(params.get("channel_len", 20))
        offset = params.get("breakout_offset_ticks", 0)
        tick = self.mc.get("tick_size", 0.1)
        # shift=0: il canale alla barra di emissione include la barra stessa
        # (completa alla close), come highest(high,N) in EL. Il fill avviene
        # comunque alla barra successiva (simulatore next-bar) -> causale.
        upper, lower = self.memo(df, ("donchian", n), lambda: donchian(df, n, shift=0))
        level_long  = upper + offset * tick
        level_short = lower - offset * tick

        # Filtro volatilita' "data2": ATR giornaliero (in $) sopra una soglia
        dvol_min = params.get("dvol_min", 0)
        if dvol_min and dvol_min > 0:
            bpv = self.mc.get("big_point_value", 1.0)
            atr_d_dollar = self.memo(
                df, ("session_atr", 14), lambda: session_atr(df, 14, shift=1)) * bpv
            vol_gate = atr_d_dollar >= dvol_min
        else:
            vol_gate = pd.Series(True, index=df.index)

        sh = int(params.get("start_hour", -1))
        eh = int(params.get("end_hour", -1))
        if sh < 0 and eh < 0:
            tw = pd.Series(True, index=df.index)
        else:
            sh_str = "00:00" if sh < 0 else "{:02d}:00".format(sh)
            eh_str = "23:59" if eh < 0 else "{:02d}:00".format(eh)
            tw = time_window(df, sh_str, eh_str)
        df_mask = day_filter(df, params.get("skip_day", -1))

        # Emissione CONTINUA stile EL: il canale Donchian si AGGIORNA barra per
        # barra (prima il livello era congelato alla prima barra valida della
        # sessione); fill next-bar, 1 entrata/sessione/direzione via flag.
        base = tw & df_mask & neut_mask & vol_gate & level_long.notna() & level_short.notna()
        entries_long  = (base & dir_long).fillna(False)
        entries_short = (base & dir_short).fillna(False)
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
            exit_on_session_end=(bool(params.get("intraday_only", 1))
                                 and not self.is_daily),
            notes="PC N={} off={}t id={} dir={} dvol={} ptn_n={}/{} ptn_d=+-{}/{}".format(
                n, offset, int(params.get("intraday_only", 1)),
                params.get("direction", 0), dvol_min,
                ptn_n_yes, ptn_n_no, ptn_d_yes, ptn_d_no),
        )
