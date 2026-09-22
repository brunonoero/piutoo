"""
BIAS — time-based intraday.

entry_type 1 (default): entrata MARKET alla N-esima barra della sessione (le_bar/se_bar).
entry_type 2: BREAKOUT dentro la finestra BIAS [le_bar..end] -- stop a highest(high,NHigh)
              (long) / lowest(low,NLow) (short). Livello da barre COMPLETE (shift) -> LEGGE ZERO.
entry_type 3: RITRACCIAMENTO dentro la finestra -- limit a lowest(low,NLow) (long) /
              highest(high,NHigh) (short). Livello da barre COMPLETE (shift).

Uscita forzata alla M-esima barra (lx_bar/sx_bar). Pattern fast indipendenti L/S.
Fonti EL: TOP_UA 218 (GC), 468 (HG) -- switch(entrytype), twBars(le_bar,end,mycount).
"""
import pandas as pd
from typing import Dict, List, Any
from .base import BaseEngine, EngineSignals, GRID_STOP_LOSS, GRID_TAKE_PROFIT, fast_values
from ..patterns import pattern_fast
from ..filters import day_filter


def _twbars(bar_num: pd.Series, start: int, end: int) -> pd.Series:
    """Porta f_twBars: finestra in barre. start>end -> wrap (>=start OR <end);
    altrimenti [start, end). bar_num e' la colonna di sessione (0-indexed)."""
    if start > end:
        return (bar_num >= start) | (bar_num < end)
    return (bar_num >= start) & (bar_num < end)


class BiasEngine(BaseEngine):
    name = "BIAS"
    description = "BIAS intraday — entry market a barra N (type 1), breakout-stop (2) o ritracciamento-limit (3) in finestra"
    pattern_library = "fast"
    entry_type = "market"

    def get_param_space(self) -> Dict[str, List[Any]]:
        # Sweep COMPLETA della libreria fast: TUTTI i pattern sono ammessi
        # purché calcolati al momento opportuno. Il type 1 (fill all'open della
        # barra trigger) shifta la maschera di 1 barra in generate_signals ->
        # il pattern è valutato alla chiusura della barra PRECEDENTE, come in
        # EL ("if pattern then buy next bar market"). I type 2/3 valutano alla
        # barra trigger con fill dalle barre successive: già causali.
        fast_yes = fast_values(152)   # include 153 -> one-sided raggiungibile
        fast_no  = fast_values(153)

        # Alias per chiarezza nel ramo daily.
        fast_yes_daily = fast_yes
        fast_no_daily  = fast_no

        if self.is_daily:
            return {
                "ptn_ly_yes": fast_yes_daily,
                "ptn_ly_no":  fast_no_daily,
                "ptn_sy_yes": fast_yes_daily,
                "ptn_sy_no":  fast_no_daily,
                "not_le_day": [-1, 0, 1, 2, 3, 4],
                "not_se_day": [-1, 0, 1, 2, 3, 4],
                "stop_loss":   [3000, 1000, 2000, 5000, 8000],
                "take_profit": [0, 2000, 4000, 6000, 10000, 15000],
                "max_bars":    [1, 3, 5, 10, 20],
            }

        # Griglia barre SCALATA al timeframe (era una lista fissa [1..16]/[5..20],
        # tarata su sessioni ~16-20 barre = solo 60m; su 30m/15m sondava solo
        # l'apertura e NON raggiungeva le entrate tardo-sessione di Unger:
        # 261=barra 23 (60m), 460=36 / 960=21 (30m) erano IRRAGGIUNGIBILI -> BIAS 0.
        # bars_sess = barre per sessione (~23h futures 24h). Sovrastimare e' innocuo:
        # un le_bar oltre la sessione reale non trova barre -> 0 trade -> filtrato.
        tf = int(self.mc.get("timeframe_minutes", 60))
        bars_sess = max(8, int(round(23 * 60 / tf)))   # GC/CL/ES...: sessione ~23h

        def _spread(fracs, lo):
            return sorted({min(bars_sess - 1, max(lo, int(round(f * bars_sess))))
                           for f in fracs})

        # entrata: distribuita sull'intera sessione (no barra 0 = apertura grezza)
        bars_entry = _spread([0.05, 0.10, 0.20, 0.30, 0.45, 0.60, 0.75, 0.90, 0.97], lo=1)
        # uscita: da inizio sessione fino all'ultima barra (overnight se lx < le)
        bars_exit  = _spread([0.15, 0.30, 0.45, 0.60, 0.75, 0.90, 0.99], lo=2)
        # fine finestra entry_type 2/3: griglia piu' rada per non esplodere i combo
        bars_end   = _spread([0.30, 0.50, 0.70, 0.90, 0.99], lo=2)
        return {
            "entry_type": [1, 2, 3],
            "le_bar": bars_entry,
            "lx_bar": bars_exit,
            "se_bar": bars_entry,
            "sx_bar": bars_exit,
            # entry_type 2/3: finestra [le_bar..end] + lookback breakout/ritracciamento
            "end_long":  bars_end,
            "end_short": bars_end,
            "nhigh": [3, 1, 2, 5],
            "nlow":  [1, 2, 3, 5],
            "ptn_ly_yes": fast_yes,
            "ptn_ly_no":  fast_no,
            "ptn_sy_yes": fast_yes,
            "ptn_sy_no":  fast_no,
            "not_le_day": [-1, 0, 1, 2, 3, 4],
            "not_se_day": [-1, 0, 1, 2, 3, 4],
            "stop_loss":   GRID_STOP_LOSS,
            "take_profit": GRID_TAKE_PROFIT,
        }

    def get_default_params(self) -> Dict[str, Any]:
        """
        Base SENZA stop e senza target durante le fasi struttura/pattern/giorni,
        come da manuale (TSS2 pag. 227, "Hourly bias": "Finora la valutazione era
        senza Stop Loss e senza Profit Target, scaviamo in questi valori"): nel
        BIAS il trade e' gia' delimitato dal time exit (le_bar->lx_bar / finestra),
        quindi lo stop base non serve a definire l'unita' di misura. SL/TP si
        ottimizzano nell'ultima fase (la griglia finale impone comunque stop>0,
        policy anti fat-tail: 68/74 TOP_UA hanno stop).

        Evidenza A/B (2026-07-14, base 1500/0 vs 0/0, sentinelle):
        - NQ 15m (le5/lx91): stessa classifica top-2, NP 236k vs 252k -> ok.
        - GC 1h (fase struttura): STESSA struttura vincente (et2/le1/lx3) ma
          livelli piu' bassi (avg 9.9$ vs 26.6$, q 0.3 vs 1.2) -> discriminazione
          preservata, margini ridotti. WATCH: se su una run piena il BIAS di un
          mercato mean-reverting crolla a zero candidate, valutare revert a 1500.
        """
        defaults = {k: v[0] for k, v in self.get_param_space().items()}
        defaults["stop_loss"] = 0
        defaults["take_profit"] = 0
        return defaults

    def get_optimization_phases(self) -> List[List[str]]:
        if self.is_daily:
            return [
                ["ptn_ly_yes", "ptn_ly_no"],
                ["ptn_sy_yes", "ptn_sy_no"],
                ["not_le_day", "not_se_day"],
                ["stop_loss", "take_profit", "max_bars"],
            ]
        return [
            ["entry_type", "le_bar", "lx_bar"],
            ["se_bar", "sx_bar"],
            ["end_long", "end_short", "nhigh", "nlow"],
            ["ptn_ly_yes", "ptn_ly_no"],
            ["ptn_sy_yes", "ptn_sy_no"],
            ["not_le_day", "not_se_day"],
            ["stop_loss", "take_profit"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        ly_y = params.get("ptn_ly_yes", 152)
        ly_n = params.get("ptn_ly_no", 153)
        sy_y = params.get("ptn_sy_yes", 152)
        sy_n = params.get("ptn_sy_no", 153)

        mask_long  = pattern_fast(df, ly_y) & ~pattern_fast(df, ly_n)
        mask_short = pattern_fast(df, sy_y) & ~pattern_fast(df, sy_n)

        day_l = day_filter(df, params.get("not_le_day", -1))
        day_s = day_filter(df, params.get("not_se_day", -1))

        if self.is_daily:
            # LEGGE ZERO: shift(1) obbligatorio su daily.
            # bar[i-1] (ieri completato) decide l'entrata a bar[i] (oggi open).
            mask_long  = mask_long.shift(1, fill_value=False).astype(bool)
            mask_short = mask_short.shift(1, fill_value=False).astype(bool)

            entries_long  = mask_long  & day_l
            entries_short = mask_short & day_s

            first_long  = entries_long.groupby(df["sess_id"]).transform(
                lambda s: (s.cumsum() == 1) & s
            )
            first_short = entries_short.groupby(df["sess_id"]).transform(
                lambda s: (s.cumsum() == 1) & s
            )
            return EngineSignals(
                entries_long=first_long.fillna(False),
                entries_short=first_short.fillna(False),
                entry_price_long=df["open"],
                entry_price_short=df["open"],
                entry_type="market",
                exits_long=None,
                exits_short=None,
                notes="BIAS_D1 ptn_l={}/{} ptn_s={}/{} [shift1]".format(ly_y, ly_n, sy_y, sy_n),
            )

        # ─── INTRADAY ───
        entry_type = int(params.get("entry_type", 1))
        le_bar = int(params.get("le_bar", 3))
        se_bar = int(params.get("se_bar", 3))
        lx_bar = int(params.get("lx_bar", 10))
        sx_bar = int(params.get("sx_bar", 10))
        bar_num = df["bar_num"]

        if entry_type == 1:
            # entrata MARKET alla barra trigger (fill = open della barra) ->
            # il pattern va valutato alla chiusura della barra PRECEDENTE:
            # shift(1) della maschera = parity EL "if pattern then buy next bar".
            # Con lo shift TUTTI i pattern (anche quelli che maturano intrabar:
            # H_d0/L_d0/close) sono causali.
            mask_long_t1  = mask_long.shift(1, fill_value=False).astype(bool)
            mask_short_t1 = mask_short.shift(1, fill_value=False).astype(bool)
            entries_long  = (bar_num == le_bar) & mask_long_t1  & day_l
            entries_short = (bar_num == se_bar) & mask_short_t1 & day_s
            exits_long    = bar_num == lx_bar
            exits_short   = bar_num == sx_bar
            return EngineSignals(
                entries_long=entries_long.fillna(False),
                entries_short=entries_short.fillna(False),
                entry_price_long=df["open"],
                entry_price_short=df["open"],
                entry_type="market",
                exits_long=exits_long.fillna(False),
                exits_short=exits_short.fillna(False),
                notes="BIAS T1 le/lx={}/{} se/sx={}/{}".format(le_bar, lx_bar, se_bar, sx_bar),
            )

        # ─── entry_type 2 (breakout stop) / 3 (ritracciamento limit) ───
        end_long  = int(params.get("end_long", lx_bar))
        end_short = int(params.get("end_short", sx_bar))
        nhigh = max(1, int(params.get("nhigh", 3)))
        nlow  = max(1, int(params.get("nlow", 1)))

        # ARMING CAUSALE: pattern + day filter valutati SOLO alla barra trigger
        # (le_bar/se_bar). cumsum>=1 propaga l'arming in avanti dentro la sessione
        # (NON transform('max'), che leakerebbe l'arming alle barre precedenti).
        trig_long  = (bar_num == le_bar) & mask_long  & day_l
        trig_short = (bar_num == se_bar) & mask_short & day_s
        armed_long  = trig_long.groupby(df["sess_id"]).cumsum() >= 1
        armed_short = trig_short.groupby(df["sess_id"]).cumsum() >= 1

        in_win_long  = _twbars(bar_num, le_bar, end_long)
        in_win_short = _twbars(bar_num, se_bar, end_short)

        entries_long  = (armed_long  & in_win_long).fillna(False)
        entries_short = (armed_short & in_win_short).fillna(False)

        # LIVELLI -- LEGGE ZERO: rolling su barre COMPLETE -> shift(1).
        # Lo stop/limit alla barra i usa il max/min delle NHigh/NLow barre PRECEDENTI:
        # un fill richiede che la barra i rompa quell'estremo (breakout/penetrazione).
        roll_hi = df["high"].rolling(nhigh, min_periods=1).max().shift(1)
        roll_lo = df["low"].rolling(nlow,  min_periods=1).min().shift(1)

        if entry_type == 2:   # breakout
            level_long  = roll_hi   # buy highest(high, NHigh) stop
            level_short = roll_lo   # sellshort lowest(low, NLow) stop
            et = "stop"
        else:                 # entry_type == 3: ritracciamento
            level_long  = roll_lo   # buy lowest(low, NLow) limit
            level_short = roll_hi   # sellshort highest(high, NHigh) limit
            et = "limit"

        # uscita forzata; la scadenza dell'ordine a fine finestra e' implicita
        # nella vita a 1 barra degli ordini del simulatore (EL: l'ordine emesso
        # all'ultima barra della finestra resta valido UNA barra oltre).
        exits_long  = bar_num == lx_bar
        exits_short = bar_num == sx_bar

        return EngineSignals(
            entries_long=entries_long,
            entries_short=entries_short,
            entry_price_long=level_long,
            entry_price_short=level_short,
            entry_type=et,
            exits_long=exits_long.fillna(False),
            exits_short=exits_short.fillna(False),
            single_entry_per_session=True,
            notes="BIAS T{} le={} end={} lx={} Nh={} Nl={} se={} ends={} sx={}".format(
                entry_type, le_bar, end_long, lx_bar, nhigh, nlow, se_bar, end_short, sx_bar),
        )
