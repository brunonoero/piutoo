# Codice Base — Implementazione Python del Metodo Unger

---

## Session OHLC (`session.py`)

```python
import pandas as pd
import numpy as np
from typing import Optional


def calc_session_ohlc(
    df: pd.DataFrame,
    session_start: str = "17:00",
    session_end: str = "16:59",
    n_sessions: int = 5,
) -> pd.DataFrame:
    """
    Equivalente Python di _OHLCMulti5 (Michael Bruns / Andrea Unger).

    Calcola OHLC delle ultime n_sessions sessioni complete + sessione corrente.

    Args:
        df: DataFrame con indice DatetimeIndex e colonne open/high/low/close
        session_start: orario inizio sessione (es. "17:00" per futures americani)
        session_end: orario fine sessione (es. "16:59")
        n_sessions: numero di sessioni storiche da conservare (default 5)

    Returns:
        DataFrame con colonne aggiuntive:
            O_d0, H_d0, L_d0, C_d0  → sessione corrente in costruzione
            O_d1, H_d1, L_d1, C_d1  → sessione precedente (completata)
            ...
            O_d5, H_d5, L_d5, C_d5  → 5 sessioni fa
            is_session_start         → True sulla prima barra della nuova sessione
    """
    df = df.copy()
    times = df.index.time

    # Determina se la sessione attraversa la mezzanotte
    h_start, m_start = map(int, session_start.split(":"))
    h_end, m_end = map(int, session_end.split(":"))
    t_start = h_start * 60 + m_start
    t_end = h_end * 60 + m_end
    crosses_midnight = t_start > t_end

    def in_session(t):
        minutes = t.hour * 60 + t.minute
        if crosses_midnight:
            return minutes >= t_start or minutes <= t_end
        else:
            return t_start <= minutes <= t_end

    in_sess = np.array([in_session(t) for t in times])

    # Identifica inizio sessione
    is_start = np.zeros(len(df), dtype=bool)
    for i in range(1, len(df)):
        if in_sess[i] and not in_sess[i - 1]:
            is_start[i] = True
        elif in_sess[i] and df.index[i].date() != df.index[i - 1].date() and crosses_midnight:
            # Gestione buco (giorno mancante in sessione cross-midnight)
            is_start[i] = True
    if in_sess[0]:
        is_start[0] = True

    df["is_session_start"] = is_start

    # Calcola OHLC sessione corrente (d0) in modo rolling
    sess_o = np.full(len(df), np.nan)
    sess_h = np.full(len(df), np.nan)
    sess_l = np.full(len(df), np.nan)
    sess_c = np.full(len(df), np.nan)

    cur_o = cur_h = cur_l = cur_c = np.nan
    for i in range(len(df)):
        if not in_sess[i]:
            continue
        if is_start[i]:
            cur_o = df["open"].iloc[i]
            cur_h = df["high"].iloc[i]
            cur_l = df["low"].iloc[i]
        cur_h = max(cur_h, df["high"].iloc[i])
        cur_l = min(cur_l, df["low"].iloc[i])
        cur_c = df["close"].iloc[i]
        sess_o[i] = cur_o
        sess_h[i] = cur_h
        sess_l[i] = cur_l
        sess_c[i] = cur_c

    df["O_d0"] = sess_o
    df["H_d0"] = sess_h
    df["L_d0"] = sess_l
    df["C_d0"] = sess_c

    # Calcola OHLC sessioni passate (d1..d5) shiftando i valori di fine sessione
    # Raccoglie OHLC di ogni sessione completata
    completed = []  # lista di (O, H, L, C) per ogni sessione completata

    cur_sess_idx = []
    for i in range(len(df)):
        if not in_sess[i]:
            if cur_sess_idx:
                idxs = cur_sess_idx
                o = df["open"].iloc[idxs[0]]
                h = df["high"].iloc[idxs].max()
                l = df["low"].iloc[idxs].min()
                c = df["close"].iloc[idxs[-1]]
                completed.append((o, h, l, c, idxs[-1]))
                cur_sess_idx = []
        else:
            if is_start[i] and cur_sess_idx:
                idxs = cur_sess_idx
                o = df["open"].iloc[idxs[0]]
                h = df["high"].iloc[idxs].max()
                l = df["low"].iloc[idxs].min()
                c = df["close"].iloc[idxs[-1]]
                completed.append((o, h, l, c, idxs[-1]))
                cur_sess_idx = []
            cur_sess_idx.append(i)

    # Per ogni barra, associa le N sessioni passate più recenti
    for d in range(1, n_sessions + 1):
        for col, idx in [("O", 0), ("H", 1), ("L", 2), ("C", 3)]:
            df[f"{col}_d{d}"] = np.nan

    # Mappa: per ogni barra, quante sessioni complete ci sono prima?
    completed_end_indices = [c[4] for c in completed]
    for i in range(len(df)):
        # Sessioni completate prima o alla barra i
        past = [c for c in completed if c[4] < i]
        for d in range(1, n_sessions + 1):
            if len(past) >= d:
                o, h, l, c_val, _ = past[-(d)]
                df.at[df.index[i], f"O_d{d}"] = o
                df.at[df.index[i], f"H_d{d}"] = h
                df.at[df.index[i], f"L_d{d}"] = l
                df.at[df.index[i], f"C_d{d}"] = c_val

    return df
```

---

## Pattern Library (`patterns.py`)

```python
import pandas as pd
import numpy as np


def _get_ohlc(df: pd.DataFrame, d: int):
    """Estrae O, H, L, C per la sessione d (0=corrente, 1=precedente, ...)"""
    return (df[f"O_d{d}"], df[f"H_d{d}"], df[f"L_d{d}"], df[f"C_d{d}"])


def pattern_neutral(df: pd.DataFrame, n: int) -> pd.Series:
    """
    PatternNeutralFast: 55 pattern senza direzione.
    n=55 → True, n=56 → False (valori sentinella).
    """
    O0, H0, L0, C0 = _get_ohlc(df, 0)
    O1, H1, L1, C1 = _get_ohlc(df, 1)
    O2, H2, L2, C2 = _get_ohlc(df, 2)
    O3, H3, L3, C3 = _get_ohlc(df, 3)
    O4, H4, L4, C4 = _get_ohlc(df, 4)
    O5, H5, L5, C5 = _get_ohlc(df, 5)

    body1d = (O1 - C1).abs()
    range1d = H1 - L1
    body5d = (O5 - C1).abs()
    h_max5 = pd.concat([H1, H2, H3, H4, H5], axis=1).max(axis=1)
    l_min5 = pd.concat([L1, L2, L3, L4, L5], axis=1).min(axis=1)
    range5d = h_max5 - l_min5
    range_d2 = H2 - L2
    range_d3 = H3 - L3

    cases = {
        1:  body1d < 0.10 * range1d,
        2:  body1d < 0.25 * range1d,
        3:  body1d < 0.50 * range1d,
        4:  body1d < 0.75 * range1d,
        5:  body1d > 0.25 * range1d,
        6:  body1d > 0.50 * range1d,
        7:  body1d > 0.75 * range1d,
        8:  body1d > 0.90 * range1d,
        9:  body5d < 0.10 * (H5 - L1),
        10: body5d < 0.25 * (H5 - L1),
        11: body5d < 0.50 * (H5 - L1),
        12: body5d < 0.75 * (H5 - L1),
        13: body5d < 1.00 * (H5 - L1),
        14: body5d < 1.50 * (H5 - L1),
        15: body5d < 2.00 * (H5 - L1),
        16: body5d > 0.25 * (H5 - L1),
        17: body5d > 0.50 * (H5 - L1),
        18: body5d > 0.75 * (H5 - L1),
        19: body5d > 1.00 * (H5 - L1),
        20: body5d > 1.50 * (H5 - L1),
        21: body5d > 2.00 * (H5 - L1),
        22: body5d > 2.50 * (H5 - L1),
        23: body5d < 0.10 * range5d,
        24: body5d < 0.25 * range5d,
        25: body5d < 0.50 * range5d,
        26: body5d < 0.75 * range5d,
        27: body5d > 0.90 * range5d,
        28: body5d > 0.25 * range5d,
        29: body5d > 0.50 * range5d,
        30: body5d > 0.75 * range5d,
        31: H0 > L0 + L0 * 0.005,
        32: H0 > L0 + L0 * 0.0075,
        33: H0 > L0 + L0 * 0.010,
        34: H0 > L0 + L0 * 0.015,
        35: H0 > L0 + L0 * 0.020,
        36: H0 > L0 + L0 * 0.025,
        37: H0 > L0 + L0 * 0.030,
        38: H0 < L0 + L0 * 0.005,
        39: H0 < L0 + L0 * 0.0075,
        40: H0 < L0 + L0 * 0.010,
        41: H0 < L0 + L0 * 0.015,
        42: H0 < L0 + L0 * 0.020,
        43: H0 < L0 + L0 * 0.025,
        44: H0 < L0 + L0 * 0.030,
        45: (O0 < L1) | (O0 > H1),
        46: (H0 < H1) & (L0 > L1),
        47: range1d < ((range_d2 + range_d3) / 3),
        48: (range1d < range_d2) & (range_d2 < range_d3),
        49: (H1 < H2) & (L1 > L2),
        50: (H1 < H2) | (L1 > L2),
        51: (H0 > H1) & (L0 < L1),
        52: (H0 > H1) & (L0 < L1),
        53: range1d < range_d2,
        54: range1d > range_d2,
        55: pd.Series(True, index=df.index),
    }

    if n in cases:
        result = cases[n]
        if isinstance(result, bool):
            return pd.Series(result, index=df.index)
        return result.fillna(False)
    else:
        return pd.Series(False, index=df.index)


def pattern_directional(df: pd.DataFrame, n: int) -> pd.Series:
    """
    PatternDirectionalFast: pattern ±n con direzione.
    +n per long, -n per short (logica speculare).
    +52/-52 → True, +53/-53 → False.
    """
    sign = 1 if n >= 0 else -1
    abs_n = abs(n)

    O0, H0, L0, C0 = _get_ohlc(df, 0)
    O1, H1, L1, C1 = _get_ohlc(df, 1)
    O2, H2, L2, C2 = _get_ohlc(df, 2)
    O3, H3, L3, C3 = _get_ohlc(df, 3)
    O4, H4, L4, C4 = _get_ohlc(df, 4)
    O5, H5, L5, C5 = _get_ohlc(df, 5)
    range1d = H1 - L1

    def pos(expr_pos, expr_neg):
        return expr_pos if sign > 0 else expr_neg

    cases = {
        1:  pos((H0-O0) > (H1-O1)*0.25,  (O0-L0) > (O1-L1)*0.25),
        2:  pos((H0-O0) > (H1-O1)*0.50,  (O0-L0) > (O1-L1)*0.50),
        3:  pos((H0-O0) > (H1-O1)*0.75,  (O0-L0) > (O1-L1)*0.75),
        4:  pos((H0-O0) > (H1-O1)*1.00,  (O0-L0) > (O1-L1)*1.00),
        5:  pos((H0-O0) > (H1-O1)*1.50,  (O0-L0) > (O1-L1)*1.50),
        6:  pos((H0-O0) > (H1-O1)*2.00,  (O0-L0) > (O1-L1)*2.00),
        7:  pos((H0-O0) > (H1-O1)*2.50,  (O0-L0) > (O1-L1)*2.50),
        8:  pos((H0-O0) > (H1-O1)*3.00,  (O0-L0) > (O1-L1)*3.00),
        9:  pos((H0-O0) < (H1-O1),       (O0-L0) < (O1-L1)),
        10: pos((C1>C2)&(C2>C3)&(C3>C4), (C1<C2)&(C2<C3)&(C3<C4)),
        11: pos((C1>C2)&(C2>C3)&(C3>C4)&(C4>C5), (C1<C2)&(C2<C3)&(C3<C4)&(C4<C5)),
        12: pos((H1>H2)&(L1>L2),          (H1<H2)&(L1<L2)),
        13: pos(C1>C2,  C1<C2),
        14: pos(C1>O1,  C1<O1),
        15: pos(C1>C2*(1+0.005), C1<C2*(1-0.005)),
        16: pos(C1>C2*(1+0.010), C1<C2*(1-0.010)),
        17: pos(C1>C2*(1+0.015), C1<C2*(1-0.015)),
        18: pos(C1>C2*(1+0.020), C1<C2*(1-0.020)),
        19: pos(C1>C2*(1+0.025), C1<C2*(1-0.025)),
        20: pos(C1>C2*(1+0.030), C1<C2*(1-0.030)),
        21: pos(H0>H1, L0<L1),
        22: pos(H0>H1*(1+0.0025), L0<L1*(1-0.0025)),
        23: pos(H0>H1*(1+0.005),  L0<L1*(1-0.005)),
        24: pos(H0>H1*(1+0.0075), L0<L1*(1-0.0075)),
        25: pos(H0>H1*(1+0.010),  L0<L1*(1-0.010)),
        26: pos(H0>H1*(1+0.015),  L0<L1*(1-0.015)),
        27: pos(L0>L1, H0<H1),
        28: pos(L0>L1*(1+0.005), H0<H1*(1-0.005)),
        29: pos(L0>L1*(1+0.010), H0<H1*(1-0.010)),
        30: pos(L0>L1*(1+0.015), H0<H1*(1-0.015)),
        31: pos(L0>L1*(1+0.020), H0<H1*(1-0.020)),
        32: pos(L0>L1*(1+0.025), H0<H1*(1-0.025)),
        33: pos(H1>H5, L1<L5),
        34: pos(H1<H5, L1>L5),
        35: pos((H1>H2)&(H1>H3)&(H1>H4), (L1<L2)&(L1<L3)&(L1<L4)),
        36: pos((L1>L2)&(L1>L3)&(L1>L4), (H1<H2)&(H1<H3)&(H1<H4)),
        37: pos((C1>C2)&(C2>C3)&(O0>C1), (C1<C2)&(C2<C3)&(O0<C1)),
        38: pos((H1-C1)<0.20*range1d,     (C1-L1)<0.20*range1d),
        39: pos(O0>H1,  O0<L1),
        40: pos(O0>C1*(1+0.0025), O0<C1*(1-0.0025)),
        41: pos(O0>C1*(1+0.005),  O0<C1*(1-0.005)),
        42: pos(O0>C1*(1+0.0075), O0<C1*(1-0.0075)),
        43: pos(O0>C1*(1+0.010),  O0<C1*(1-0.010)),
        44: pos(L1>L2,  H1<H2),
        45: pos((C1>O1)&(C2>O2), (C1<O1)&(C2<O2)),
        46: pos((C1>O1)&(C2<O2), (C1<O1)&(C2>O2)),
        47: pos(df["close"]>O0*0.99,   df["close"]<O0*1.01),
        48: pos(df["close"]>O0*0.995,  df["close"]<O0*1.005),
        49: pos(df["close"]>O0,        df["close"]<O0),
        50: pos(df["close"]>O0*1.005,  df["close"]<O0*0.995),
        51: pos(df["close"]>O0*1.010,  df["close"]<O0*0.990),
        52: pd.Series(True, index=df.index),
    }

    if abs_n in cases:
        result = cases[abs_n]
        if isinstance(result, bool):
            return pd.Series(result, index=df.index)
        return result.fillna(False)
    else:
        return pd.Series(False, index=df.index)


def pattern_fast(df: pd.DataFrame, n: int) -> pd.Series:
    """
    PatternFast: 152 pattern unmirrored (senza segno).
    n=152 → True, n>152 → False.
    Pattern 1–30 identici a pattern_neutral 1–30.
    """
    if n == 152:
        return pd.Series(True, index=df.index)
    if n > 152 or n <= 0:
        return pd.Series(False, index=df.index)

    # Pattern 1–30: stessa logica di pattern_neutral
    if 1 <= n <= 30:
        return pattern_neutral(df, n)

    O0, H0, L0, C0 = _get_ohlc(df, 0)
    O1, H1, L1, C1 = _get_ohlc(df, 1)
    O2, H2, L2, C2 = _get_ohlc(df, 2)
    O3, H3, L3, C3 = _get_ohlc(df, 3)
    O4, H4, L4, C4 = _get_ohlc(df, 4)
    O5, H5, L5, C5 = _get_ohlc(df, 5)
    range1d = H1 - L1

    cases = {
        # Wick superiore vs wick d1
        31: (H0-O0) > (H1-O1)*0.25,
        32: (H0-O0) > (H1-O1)*0.50,
        33: (H0-O0) > (H1-O1)*0.75,
        34: (H0-O0) > (H1-O1)*1.00,
        35: (H0-O0) > (H1-O1)*1.50,
        36: (H0-O0) > (H1-O1)*2.00,
        37: (H0-O0) > (H1-O1)*2.50,
        38: (H0-O0) > (H1-O1)*3.00,
        39: (H0-O0) < (H1-O1),
        40: (O0-L0) < (O1-L1),
        # Wick inferiore
        41: (O0-L0) > (O1-L1)*0.50,
        42: (O0-L0) > (O1-L1)*1.00,
        43: (O0-L0) > (O1-L1)*1.50,
        44: (O0-L0) > (O1-L1)*2.00,
        45: (O0-L0) > (O1-L1)*2.50,
        46: (O0-L0) > (O1-L1)*3.00,
        # Sequenze chiusure
        47: (C1>C2)&(C2>C3)&(C3>C4),
        48: (C1<C2)&(C2<C3)&(C3<C4),
        49: (C1>C2)&(C2>C3)&(C3>C4)&(C4>C5),
        50: (C1<C2)&(C2<C3)&(C3<C4)&(C4<C5),
        51: (H1>H2)&(L1>L2),
        52: (H1<H2)&(L1<L2),
        # Range corrente in %
        53: H0 > L0*(1+0.005),
        54: H0 > L0*(1+0.0075),
        55: H0 > L0*(1+0.010),
        56: H0 > L0*(1+0.015),
        57: H0 > L0*(1+0.020),
        58: H0 > L0*(1+0.025),
        59: H0 > L0*(1+0.030),
        60: H0 < L0*(1+0.005),
        61: H0 < L0*(1+0.0075),
        62: H0 < L0*(1+0.010),
        63: H0 < L0*(1+0.015),
        64: H0 < L0*(1+0.020),
        65: H0 < L0*(1+0.025),
        66: H0 < L0*(1+0.030),
        # Close vs close/open
        67: C1>C2, 68: C1<C2, 69: C1<O1, 70: C1>O1,
        71: C1<C2*(1-0.005), 72: C1<C2*(1-0.010), 73: C1<C2*(1-0.015),
        74: C1<C2*(1-0.020), 75: C1<C2*(1-0.025), 76: C1<C2*(1-0.030),
        77: C1>C2*(1+0.005), 78: C1>C2*(1+0.010), 79: C1>C2*(1+0.015),
        80: C1>C2*(1+0.020),
        # High/Low attuali vs precedenti
        81: H0>H1, 82: H0>H1*(1+0.0025), 83: H0>H1*(1+0.005),
        84: H0>H1*(1+0.0075), 85: H0>H1*(1+0.010), 86: H0>H1*(1+0.015),
        87: H0<H1, 88: H0<H1*(1-0.005), 89: H0<H1*(1-0.010),
        90: H0<H1*(1-0.015), 91: H0<H1*(1-0.020), 92: H0<H1*(1-0.025),
        93: H1>H5, 94: H1<H5,
        95: L0<L1, 96: L0<L1*(1-0.0025), 97: L0<L1*(1-0.005),
        98: L0<L1*(1-0.0075), 99: L0<L1*(1-0.010),
        100: L0>L1, 101: L0>L1*(1+0.005), 102: L0>L1*(1+0.010),
        103: L0>L1*(1+0.015), 104: L0>L1*(1+0.020), 105: L0>L1*(1+0.025),
        106: L1<L5, 107: L1>L5,
        108: (H1>H2)&(H1>H3)&(H1>H4),
        109: (H1<H2)&(H1<H3)&(H1<H4),
        110: (L1<L2)&(L1<L3)&(L1<L4),
        111: (L1>L2)&(L1>L3)&(L1>L4),
        112: (C1>C2)&(C2>C3)&(O0>C1),
        113: (C1<C2)&(C2<C3)&(O0<C1),
        114: (H1-C1)<0.20*range1d,
        115: (C1-L1)<0.20*range1d,
        116: (O0<L1)|(O0>H1),
        117: O0<L1, 118: O0>H1,
        119: O0<C1*(1-0.0025), 120: O0<C1*(1-0.005),
        121: O0<C1*(1-0.0075), 122: O0<C1*(1-0.010),
        123: O0>C1*(1+0.0025), 124: O0>C1*(1+0.005),
        125: O0>C1*(1+0.0075), 126: O0>C1*(1+0.010),
        127: (H0<H1)&(L0>L1),
        128: range1d < ((H2-L2+H3-L3)/3),
        129: (range1d<(H2-L2))&((H2-L2)<(H3-L3)),
        130: (H2>H1)&(L2<L1),
        131: H1<H2, 132: L1>L2,
        133: (H1<H2)|(L1>L2),
        134: (H2<H1)&(L2>L1),
        135: (H0>H1)&(L0<L1),
        136: (C1>O1)&(C2>O2), 137: (C1<O1)&(C2>O2),
        138: (C1>O1)&(C2<O2), 139: (C1<O1)&(C2<O2),
        140: (H1-L1)<(H2-L2), 141: (H1-L1)>(H2-L2),
        # Momentum intrabar (close vs open corrente)
        142: df["close"]>O0*0.99,  143: df["close"]>O0*0.995,
        144: df["close"]>O0,       145: df["close"]>O0*1.005,
        146: df["close"]>O0*1.010,
        147: df["close"]<O0*1.010, 148: df["close"]<O0*1.005,
        149: df["close"]<O0,       150: df["close"]<O0*0.995,
        151: df["close"]<O0*0.990,
        152: pd.Series(True, index=df.index),
    }

    if n in cases:
        result = cases[n]
        if isinstance(result, bool):
            return pd.Series(result, index=df.index)
        return result.fillna(False)
    return pd.Series(False, index=df.index)


def pattern_uaptnbase(df: pd.DataFrame, n: int) -> pd.Series:
    """UAPtnBase: 42 pattern. n=41 → True, n=42 → False."""
    if n == 41:
        return pd.Series(True, index=df.index)
    if n >= 42:
        return pd.Series(False, index=df.index)

    O0, H0, L0, C0 = _get_ohlc(df, 0)
    O1, H1, L1, C1 = _get_ohlc(df, 1)
    O2, H2, L2, C2 = _get_ohlc(df, 2)
    O3, H3, L3, C3 = _get_ohlc(df, 3)
    O4, H4, L4, C4 = _get_ohlc(df, 4)
    O5, H5, L5, C5 = _get_ohlc(df, 5)

    cases = {
        1:  (O1-C1).abs() < 0.5*(H1-L1),
        2:  (O1-C5).abs() < 0.5*(H5-C1),
        3:  (O5-C1).abs() < 0.5*(pd.concat([H1,H2,H3,H4,H5],axis=1).max(axis=1) - pd.concat([L1,L2,L3,L4,L5],axis=1).min(axis=1)),
        4:  (H0-O0) > (H1-O1)*1.0,
        5:  (H0-O0) > (H1-O1)*1.5,
        6:  (O0-L0) > (O1-L1)*1.0,
        7:  (O0-L0) > (O1-L1)*1.5,
        8:  (C1>C2)&(C2>C3)&(C3>C4),
        9:  (C1<C2)&(C2<C3)&(C3<C4),
        10: (H1>H2)&(L1>L2),
        11: (H1<H2)&(L1<L2),
        12: H0 > L0*(1+0.0075),
        13: H0 < L0*(1+0.0075),
        14: C1>C2, 15: C1<C2, 16: C1<O1, 17: C1>O1,
        18: C1<C2*(1-0.005), 19: C1>C2*(1+0.005),
        20: H0>H1, 21: H1>H5,
        22: L0<L1, 23: L1<L5,
        24: (H1>H2)&(H1>H3)&(H1>H4),
        25: (H1<H2)&(H1<H3)&(H1<H4),
        26: (L1<L2)&(L1<L3)&(L1<L4),
        27: (L1>L2)&(L1>L3)&(L1>L4),
        28: (C1>C2)&(C2>C3)&(O0>C1),
        29: (C1<C2)&(C2<C3)&(O0<C1),
        30: (H1-C1)<0.20*(H1-L1),
        31: (C1-L1)<0.20*(H1-L1),
        32: (O0<L1)|(O0>H1),
        33: O0<C1*(1-0.005),
        34: O0>C1*(1+0.005),
        35: (H0<H1)&(L0>L1),
        36: (H1-L1) < ((H2-L2+H3-L3)/3),
        37: (H1-L1<H2-L2)&(H2-L2<H3-L3),
        38: (H2>H1)&(L2<L1),
        39: (H1<H2)|(L1>L2),
        40: (H2<H1)&(L2>L1),
        41: pd.Series(True, index=df.index),
    }

    result = cases.get(n, pd.Series(False, index=df.index))
    if isinstance(result, bool):
        return pd.Series(result, index=df.index)
    return result.fillna(False)
```

---

## Motori (`engines.py`) — schema base

```python
import pandas as pd
import numpy as np
from .patterns import pattern_neutral, pattern_directional, pattern_fast, pattern_uaptnbase
from .filters import time_window, day_filter


def engine_trend_following(
    df: pd.DataFrame,
    mirrored: bool = True,
    ptn_neut_yes: int = 55,
    ptn_neut_no: int = 56,
    ptn_dir_yes: int = 52,
    ptn_dir_no: int = 53,
    ptn_ly: int = 152,   # solo se unmirrored
    ptn_ln: int = 153,
    ptn_sy: int = 152,
    ptn_sn: int = 153,
    start_time: str = "00:00",
    end_time: str = "23:59",
    skip_day: int = -1,
) -> tuple[pd.Series, pd.Series]:
    """
    Trend Following: entrata su rottura H/L della sessione precedente.
    Entry level LONG  = H_d1 (stop buy)
    Entry level SHORT = L_d1 (stop sell)
    """
    base_filter = (
        time_window(df, start_time, end_time) &
        day_filter(df, skip_day)
    )

    if mirrored:
        pattern_filter = (
            pattern_neutral(df, ptn_neut_yes) &
            ~pattern_neutral(df, ptn_neut_no)
        )
        entries_long = base_filter & pattern_filter & pattern_directional(df, +ptn_dir_yes) & ~pattern_directional(df, +ptn_dir_no)
        entries_short = base_filter & pattern_filter & pattern_directional(df, -ptn_dir_yes) & ~pattern_directional(df, -ptn_dir_no)
    else:
        entries_long  = base_filter & pattern_fast(df, ptn_ly) & ~pattern_fast(df, ptn_ln)
        entries_short = base_filter & pattern_fast(df, ptn_sy) & ~pattern_fast(df, ptn_sn)

    # Prezzi di entrata (stop order = H_d1 per long, L_d1 per short)
    # vectorbt gestisce gli stop order: restituiamo solo i segnali booleani
    # Il prezzo di entrata viene passato separatamente a vbt.Portfolio.from_signals()
    return entries_long, entries_short


def engine_bias(
    df: pd.DataFrame,
    le_bar: int = 3,
    se_bar: int = 3,
    lx_bar: int = 10,
    sx_bar: int = 10,
    ptn_ly: int = 152,
    ptn_ln: int = 153,
    ptn_sy: int = 152,
    ptn_sn: int = 153,
    not_le_day: int = -1,
    not_se_day: int = -1,
) -> tuple[pd.Series, pd.Series]:
    """
    BIAS: entrata alla N-esima barra della sessione.
    le_bar/se_bar: numero barra per entrata (1-indexed dall'inizio sessione)
    lx_bar/sx_bar: numero barra per uscita
    """
    # Calcola il numero di barra nella sessione corrente
    bar_num = df.groupby(df["is_session_start"].cumsum()).cumcount() + 1
    df = df.copy()
    df["bar_num"] = bar_num

    entries_long = (
        (df["bar_num"] == le_bar) &
        pattern_fast(df, ptn_ly) & ~pattern_fast(df, ptn_ln) &
        day_filter(df, not_le_day)
    )
    entries_short = (
        (df["bar_num"] == se_bar) &
        pattern_fast(df, ptn_sy) & ~pattern_fast(df, ptn_sn) &
        day_filter(df, not_se_day)
    )
    exits_long  = df["bar_num"] == lx_bar
    exits_short = df["bar_num"] == sx_bar

    return entries_long, entries_short, exits_long, exits_short
```

---

## Backtest con vectorbt (`backtest.py`) — schema base

```python
import vectorbt as vbt
import pandas as pd
from .session import calc_session_ohlc
from .engines import engine_trend_following


def run_strategy(
    df: pd.DataFrame,
    entries_long: pd.Series,
    entries_short: pd.Series,
    entry_price_long: pd.Series = None,   # es. df["H_d1"] per TF long
    entry_price_short: pd.Series = None,  # es. df["L_d1"] per TF short
    stop_loss_pct: float = None,
    take_profit_pct: float = None,
    size: int = 1,
    fees: float = 0.0,
) -> vbt.Portfolio:
    """
    Lancia il backtest con vectorbt.
    """
    price = df["close"]

    if entry_price_long is None:
        entry_price_long = price
    if entry_price_short is None:
        entry_price_short = price

    portfolio = vbt.Portfolio.from_signals(
        close=price,
        entries=entries_long,
        short_entries=entries_short,
        entry_price=entry_price_long,   # può essere Series o scalare
        short_entry_price=entry_price_short,
        sl_stop=stop_loss_pct,
        tp_stop=take_profit_pct,
        size=size,
        fees=fees,
        freq="15T",  # adattare al timeframe
    )
    return portfolio


def print_stats(portfolio: vbt.Portfolio):
    """Stampa le statistiche principali."""
    stats = portfolio.stats()
    print(f"Net Profit:     {stats['Total Return [%]']:.2f}%")
    print(f"Sharpe Ratio:   {stats['Sharpe Ratio']:.2f}")
    print(f"Max Drawdown:   {stats['Max Drawdown [%]']:.2f}%")
    print(f"Total Trades:   {stats['Total Trades']}")
    print(f"Win Rate:       {stats['Win Rate [%]']:.1f}%")
    print(f"Profit Factor:  {stats.get('Profit Factor', 'N/A')}")
    print(f"Avg Trade:      {stats.get('Avg Winning Trade [%]', 'N/A')}")
```

---

## Esempio completo: TF su GC (Gold Futures) 15min

```python
import pandas as pd
from unger.session import calc_session_ohlc
from unger.engines import engine_trend_following
from unger.backtest import run_strategy, print_stats

# 1. Carica dati
df = pd.read_csv("GC_15min.csv", parse_dates=["datetime"], index_col="datetime")

# 2. Calcola sessione OHLC (GC: sessione 18:00-17:00 Chicago)
df = calc_session_ohlc(df, session_start="18:00", session_end="17:00")

# 3. Genera segnali con motore TF mirrored
entries_long, entries_short = engine_trend_following(
    df,
    mirrored=True,
    ptn_neut_yes=3,    # body1d < 50% range1d
    ptn_neut_no=56,    # no filtro esclusione
    ptn_dir_yes=14,    # C_d1 > O_d1 (barra verde ieri → TF long)
    ptn_dir_no=53,     # no filtro esclusione
    start_time="18:00",
    end_time="16:30",
    skip_day=5,        # no venerdì
)

# 4. Backtest
portfolio = run_strategy(
    df,
    entries_long=entries_long,
    entries_short=entries_short,
    entry_price_long=df["H_d1"],   # stop su massimo di ieri
    entry_price_short=df["L_d1"],  # stop su minimo di ieri
    stop_loss_pct=0.015,           # 1.5% stop
    take_profit_pct=0.04,          # 4% target
    size=1,
)

print_stats(portfolio)
portfolio.plot().show()
```
