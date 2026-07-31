"""
BIAS Weekly — ciclo settimanale: entra giorno X ora Y, esce giorno Z ora W.

Famiglia documentata dal metodo (§5: "Swing/BIAS: cicli intraday / SETTIMANALI
ricorrenti") ma assente fino al 2026-07-07: tutte le TOP_UA FX (EC_15 long
lun→mar, EC_196 short ven), RB_343 (long mer→ven, short dom→lun) e PL_100
erano irrappresentabili — il BIAS intraday esprime solo il ciclo di sessione.

Semantica (parity con i template EL "Bias Finder"):
- entrata MARKET alla barra con (giorno CET == le_day) e (orario barra == le_time);
  il fill avviene all'OPEN di quella barra (convenzione BIAS del simulatore).
  NB: le barre sono END-labeled, quindi per portare un EL "if time=T then buy
  next bar" va usato le_time = T + timeframe (label della barra successiva).
- uscita MARKET alla barra con (giorno == lx_day) e (orario == lx_time); se la
  barra non esiste (festivo) la posizione resta aperta fino alla settimana
  successiva, come in EL.
- long e short indipendenti (giorni/orari/pattern separati); day = -1 disattiva
  la direzione.
- tiene SEMPRE overnight (exit_on_session_end=False): il ciclo settimanale è
  il punto del motore. Stop/target dal risk management.

LEGGE ZERO: fill all'open della barra trigger -> la maschera pattern è
shiftata di 1 barra (valutata alla chiusura della barra PRECEDENTE, parity
EL "if pattern then buy next bar"). Con lo shift TUTTI i 151 pattern fast
sono ammessi, anche quelli che maturano intrabar (H_d0/L_d0/close).

Orari come interi HHMM in ora CET (es. 700 = 07:00, 1530 = 15:30). La griglia
usa le ore piene; un valore qualsiasi (es. 725) è accettato per i port esatti.
Giorni in convenzione Python: 0=Lun .. 4=Ven (-1 = direzione disattivata).
"""
import pandas as pd
from typing import Dict, List, Any
from .base import BaseEngine, EngineSignals, GRID_STOP_LOSS, GRID_TAKE_PROFIT, fast_values
from ..patterns import pattern_fast


def _time_match(df: pd.DataFrame, hhmm: int) -> pd.Series:
    minutes = df.index.hour * 60 + df.index.minute
    target = (int(hhmm) // 100) * 60 + int(hhmm) % 100
    return pd.Series(minutes == target, index=df.index)


class BiasWeeklyEngine(BaseEngine):
    name = "BIASW"
    description = "BIAS weekly — entra giorno X ora Y, esce giorno Z ora W (ciclo settimanale, hold overnight)"
    pattern_library = "fast"
    entry_type = "market"

    def get_param_space(self) -> Dict[str, List[Any]]:
        # Libreria fast COMPLETA: la maschera è shiftata di 1 barra in
        # generate_signals, quindi ogni pattern è causale (vedi docstring).
        fast_yes = fast_values(152)   # include 153 -> one-sided raggiungibile
        fast_no  = fast_values(153)
        hours = [h * 100 for h in range(0, 24)]
        days = [-1, 0, 1, 2, 3, 4]          # -1 = direzione off; 0=Lun..4=Ven

        return {
            # FASE 1-2: timing LONG (giorni, poi orari)
            "le_day":  days,
            "lx_day":  [0, 1, 2, 3, 4],
            "le_time": hours,
            "lx_time": hours,
            # FASE 3-4: timing SHORT
            "se_day":  days,
            "sx_day":  [0, 1, 2, 3, 4],
            "se_time": hours,
            "sx_time": hours,
            # FASE 5-6: pattern (libreria completa, maschera shiftata)
            "ptn_ly_yes": fast_yes,
            "ptn_ly_no":  fast_no,
            "ptn_sy_yes": fast_yes,
            "ptn_sy_no":  fast_no,
            # FASE 7: risk management
            "stop_loss":   GRID_STOP_LOSS,
            "take_profit": GRID_TAKE_PROFIT,
        }

    def get_default_params(self) -> Dict[str, Any]:
        defaults = {k: v[0] for k, v in self.get_param_space().items()}
        # Default: entrambe le direzioni OFF; le fasi timing accendono una
        # direzione alla volta (l'altra resta spenta -> sweep pulita).
        defaults["le_day"] = -1
        defaults["se_day"] = -1
        defaults["le_time"] = 100
        defaults["lx_time"] = 100
        defaults["se_time"] = 100
        defaults["sx_time"] = 100
        # Base senza SL/TP come da manuale (TSS2 pag. 227): il trade BIASW e'
        # delimitato dalla finestra giorno/ora. SL/TP si scavano nell'ultima fase.
        defaults["stop_loss"] = 0
        defaults["take_profit"] = 0
        return defaults

    def get_optimization_phases(self) -> List[List[str]]:
        return [
            ["le_day", "lx_day"],
            ["le_time", "lx_time"],
            ["se_day", "sx_day"],
            ["se_time", "sx_time"],
            ["ptn_ly_yes", "ptn_ly_no"],
            ["ptn_sy_yes", "ptn_sy_no"],
            ["stop_loss", "take_profit"],
        ]

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        le_day = int(params.get("le_day", -1))
        se_day = int(params.get("se_day", -1))
        dow = pd.Series(df.index.dayofweek, index=df.index)

        false_s = pd.Series(False, index=df.index)
        entries_long = false_s
        exits_long = false_s
        entries_short = false_s
        exits_short = false_s

        if le_day >= 0:
            # shift(1): pattern valutato alla chiusura della barra precedente,
            # fill all'open della barra trigger (parity EL).
            mask_l = (pattern_fast(df, params.get("ptn_ly_yes", 152))
                      & ~pattern_fast(df, params.get("ptn_ly_no", 153))
                      ).shift(1, fill_value=False).astype(bool)
            entries_long = ((dow == le_day)
                            & _time_match(df, params.get("le_time", 100))
                            & mask_l)
            exits_long = ((dow == int(params.get("lx_day", 0)))
                          & _time_match(df, params.get("lx_time", 100)))

        if se_day >= 0:
            mask_s = (pattern_fast(df, params.get("ptn_sy_yes", 152))
                      & ~pattern_fast(df, params.get("ptn_sy_no", 153))
                      ).shift(1, fill_value=False).astype(bool)
            entries_short = ((dow == se_day)
                             & _time_match(df, params.get("se_time", 100))
                             & mask_s)
            exits_short = ((dow == int(params.get("sx_day", 0)))
                           & _time_match(df, params.get("sx_time", 100)))

        return EngineSignals(
            entries_long=entries_long.fillna(False),
            entries_short=entries_short.fillna(False),
            entry_price_long=df["open"],
            entry_price_short=df["open"],
            entry_type="market",
            exits_long=exits_long.fillna(False),
            exits_short=exits_short.fillna(False),
            exit_on_session_end=False,
            notes="BIASW L d{}@{}->d{}@{} S d{}@{}->d{}@{}".format(
                le_day, params.get("le_time", 100),
                params.get("lx_day", 0), params.get("lx_time", 100),
                se_day, params.get("se_time", 100),
                params.get("sx_day", 0), params.get("sx_time", 100)),
        )
