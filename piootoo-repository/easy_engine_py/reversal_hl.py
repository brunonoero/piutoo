"""
Reversal H/L -- mean reversion sul livello (estremo) della sessione di IERI.

- LONG:  limit BUY  a  L_d1 - long_offset_ticks * tick_size   (compra sotto il minimo di ieri)
- SHORT: limit SELL a  H_d1 + short_offset_ticks * tick_size  (vende sopra il massimo di ieri)

Famiglia counter-trend / mean-reversion (skill metodo-unger §5 engine_reversal_hl).
Porta lo spirito delle TOP_UA ES 333/556/746 (limit attorno agli estremi di sessione):
- le sorgenti EL usano LowS(0)/HighS(0) o close +- frazione di range; QUI usiamo
  L_d1/H_d1 (sessione COMPLETA di ieri) -- la forma canonica e look-ahead-safe della
  skill. Il segno direzionale e' INVERTITO come nei reversal (long usa directional
  ribassista: si compra nella debolezza), identico a RBB_M e coerente con 333
  (PtnDirYes=-1: long usa +PtnDirYes = ribassista).

--- LEGGE ZERO ---
- L_d1 / H_d1 sono estremi della sessione di IERI, completata -> noti dall'apertura
  della sessione corrente -> sicuri su ogni timeframe (anche daily).
- Pattern d1-safe (stesse griglie neutral/directional di RBB_M/VBO: niente 31-44 /
  21-32 che leggono H_d0/L_d0 running).
- entry_type="limit": il simulatore esegue solo con PENETRAZIONE STRETTA del livello
  (lo[i] < L per long) + stop-out stessa barra -> nessun fake-fill al tocco
  ("trappola limit-order" chiusa nel simulator).
"""
import pandas as pd
from typing import Dict, List, Any
from .base import (
    BaseEngine, EngineSignals,
    GRID_START_HOURS, GRID_END_HOURS, GRID_STOP_LOSS, GRID_TAKE_PROFIT,
    mirrored_dir_values, neutral_values,
)
from ..patterns import pattern_neutral, pattern_directional
from ..filters import time_window, day_filter


class ReversalHLEngine(BaseEngine):
    name = "RHL"
    description = ("Reversal H/L -- limit a L_d1-offset (long) / H_d1+offset (short), "
                   "mean reversion sull'estremo di ieri")
    pattern_library = "neutral_directional"
    entry_type = "limit"

    def get_param_space(self) -> Dict[str, List[Any]]:
        # Griglie pattern d1-safe (identiche a RBB_M/VBO: escludono i pattern che
        # leggono H_d0/L_d0 running -> 31-44 neutral, 21-32 directional).
        neut_yes = neutral_values(55)
        neut_no  = neutral_values(56)
        dir_yes  = mirrored_dir_values(52)
        dir_no   = mirrored_dir_values(53)

        # offset in tick: 0 = limit esatto sull'estremo di ieri; valori >0 richiedono
        # una penetrazione piu' profonda prima di farsi prendere (fade piu' selettivo).
        offsets = [0, 5, 10, 20, 40, 80]

        if self.is_daily:
            return {
                "long_offset_ticks":  offsets,
                "short_offset_ticks": offsets,
                "direction":   [0, 1, 2],
                "ptn_neut_yes": neut_yes, "ptn_neut_no": neut_no,
                "ptn_dir_yes":  dir_yes,  "ptn_dir_no":  dir_no,
                "start_hour":  [-1], "end_hour": [-1], "skip_day": [-1, 4],
                "stop_loss":   [3000, 1000, 2000, 5000],
                "take_profit": [0, 2000, 4000, 6000, 10000],
                "max_bars":    [0, 3, 5, 10, 20],
            }

        return {
            # FASE 0: livello di entrata (offset dagli estremi di ieri)
            "long_offset_ticks":  offsets,
            "short_offset_ticks": offsets,
            # FASE 1: direzione consentita
            "direction":   [0, 1, 2],
            # FASE 2-3: pattern (neutrale -> direzionale, segno reversal)
            "ptn_neut_yes": neut_yes, "ptn_neut_no": neut_no,
            "ptn_dir_yes":  dir_yes,  "ptn_dir_no":  dir_no,
            # FASE 4: filtri orari/giorno
            "start_hour":  GRID_START_HOURS, "end_hour": GRID_END_HOURS, "skip_day": [-1, 4],
            # FASE 5: risk management
            "stop_loss":   GRID_STOP_LOSS,
            "take_profit": GRID_TAKE_PROFIT,
            "max_bars":    [0, 12, 24, 48],
        }

    def get_default_params(self) -> Dict[str, Any]:
        # Stop/target di default non nulli: una mean-reversion senza stop nelle fasi
        # iniziali genera trade strutturalmente diversi -> default coerente con VBO.
        defaults = {k: v[0] for k, v in self.get_param_space().items()}
        if self.is_daily:
            defaults["stop_loss"], defaults["take_profit"] = 3000, 6000
        else:
            defaults["stop_loss"], defaults["take_profit"] = 1500, 3000
        defaults["max_bars"] = 0
        return defaults

    def get_optimization_phases(self) -> List[List[str]]:
        if self.is_daily:
            return [
                ["long_offset_ticks", "short_offset_ticks"],
                ["direction"],
                ["ptn_neut_yes", "ptn_neut_no"],
                ["ptn_dir_yes", "ptn_dir_no"],
                ["skip_day"],
                ["stop_loss", "take_profit", "max_bars"],
            ]
        return [
            ["long_offset_ticks", "short_offset_ticks"],
            ["direction"],
            ["ptn_neut_yes", "ptn_neut_no"],
            ["ptn_dir_yes", "ptn_dir_no"],
            ["start_hour", "end_hour", "skip_day"],
            ["stop_loss", "take_profit", "max_bars"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        tick = float(self.mc.get("tick_size", 0.1))
        long_off  = int(params.get("long_offset_ticks", 0))
        short_off = int(params.get("short_offset_ticks", 0))

        # Livelli limit dagli estremi di IERI (costanti dentro la sessione).
        level_long  = df["L_d1"] - long_off  * tick
        level_short = df["H_d1"] + short_off * tick

        # Pattern: neutrale + direzionale con segno INVERTITO (reversal, come RBB_M).
        ptn_n_yes = params.get("ptn_neut_yes", 55)
        ptn_n_no  = params.get("ptn_neut_no", 56)
        ptn_d_yes = params.get("ptn_dir_yes", 52)
        ptn_d_no  = params.get("ptn_dir_no", 53)
        neut_mask = pattern_neutral(df, ptn_n_yes) & ~pattern_neutral(df, ptn_n_no)
        # long compra nella debolezza -> directional ribassista (segno -)
        dir_long  = pattern_directional(df, -ptn_d_yes) & ~pattern_directional(df, -ptn_d_no)
        dir_short = pattern_directional(df, +ptn_d_yes) & ~pattern_directional(df, +ptn_d_no)

        sh = int(params.get("start_hour", -1))
        eh = int(params.get("end_hour", -1))
        if sh < 0 and eh < 0:
            tw = pd.Series(True, index=df.index)
        else:
            sh_str = "00:00" if sh < 0 else "{:02d}:00".format(sh)
            eh_str = "23:59" if eh < 0 else "{:02d}:00".format(eh)
            tw = time_window(df, sh_str, eh_str)
        df_mask = day_filter(df, params.get("skip_day", -1))

        # Emissione CONTINUA stile EL: ordine limit ri-emesso a ogni barra valida
        # (livello L_d1/H_d1 costante in sessione), fill next-bar dal simulatore,
        # 1 entrata/sessione/direzione via single_entry_per_session.
        base = tw & df_mask & neut_mask & level_long.notna() & level_short.notna()
        entries_long  = (base & dir_long).fillna(False)
        entries_short = (base & dir_short).fillna(False)
        entries_long, entries_short = self.apply_direction(
            entries_long, entries_short, params.get("direction", 0))

        return EngineSignals(
            entries_long=entries_long,
            entries_short=entries_short,
            entry_price_long=level_long,
            entry_price_short=level_short,
            entry_type="limit",
            exits_long=None, exits_short=None,
            single_entry_per_session=True,
            notes="RHL Loff={} Soff={} dir={} ptn_n={}/{} ptn_d=-+{}/{}".format(
                long_off, short_off, params.get("direction", 0),
                ptn_n_yes, ptn_n_no, ptn_d_yes, ptn_d_no),
        )
