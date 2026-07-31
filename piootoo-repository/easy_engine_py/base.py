"""
Interfaccia base per i motori di strategia Unger.

Ogni motore espone:
- name (es. "TF_M")
- get_param_space() → dict dei parametri con range di sweep
- generate_signals(df, params) → EngineSignals

Il simulator riceve EngineSignals e esegue la simulazione barra-per-barra.
"""
from dataclasses import dataclass, field
from typing import Dict, List, Any, Optional
import pandas as pd


# Parametri CATEGORICI: id/scelte discrete dove "valore adiacente" NON significa
# "strategia simile" (pattern id, sorgenti, direzioni, giorni, tipi di entrata).
# Esclusi da plateau analysis e smoothing di fase (vedi get_ordinal_params).
# NB: start_hour/end_hour/skip_day sono ordinali ma con sentinella -1 ("off"):
# la coppia (-1 <-> primo valore) non è un vicinato valido, gestita a parte.
CATEGORICAL_PARAMS = frozenset({
    "ptn_neut_yes", "ptn_neut_no", "ptn_dir_yes", "ptn_dir_no",
    "ptn_ly_yes", "ptn_ly_no", "ptn_sy_yes", "ptn_sy_no",
    "direction", "vol_source", "momentum",
    "level_choice", "level_source", "lev_include_sess0", "intraday_only",
    "entry_type",
    "skip_day", "le_day", "lx_day", "se_day", "sx_day",
    "not_le_day", "not_se_day",
})

# Valore "OFF" dei parametri ordinali che ce l'hanno: il salto off <-> primo
# valore attivo NON è un vicinato di griglia (strategie molto diverse), quindi
# plateau/smoothing non attraversano mai questo confine.
OFF_SENTINELS = {
    "start_hour": -1, "end_hour": -1,
    "take_profit": 0, "trailing_stop": 0, "breakeven": 0, "max_bars": 0,
    "gradient_factor": 0, "dvol_min": 0, "vol_mult_short": -1,
    "level_shift": None,  # 0 = livello esatto, e' un valore pieno: nessun off
}


# ─────────────────────────────────────────────────────────────────────────────
# Griglie condivise tra motori (correzioni 2026-07-07, evidenza head-to-head
# TOP_UA: 0/11 config esatte raggiungibili dalla vecchia griglia).
# ─────────────────────────────────────────────────────────────────────────────

# Orari a passo 1h: le finestre operative delle TOP_UA cadono su ore qualsiasi
# (1, 7, 11, 17...) — la griglia rada [-1,2,6,9,14] non poteva atterrarci.
GRID_START_HOURS = [-1] + list(range(0, 24))
GRID_END_HOURS   = [-1] + list(range(0, 24))

# Stop/target a passo $250-500 su range 250-5000 / 0-10000 (workflow §14.B
# fase 4 "affinare stop/target"). Primo valore = default della sweep.
# 4500/6000 aggiunti 2026-07-11 (audit NQ): NQ_156 usa TP=4500, NQ_181/531
# TP=6000 — erano gli unici valori TOP_UA fuori griglia sul risk management.
GRID_STOP_LOSS   = [1500, 250, 500, 750, 1000, 1250, 1750, 2000,
                    2250, 2500, 3000, 4000, 5000]
GRID_TAKE_PROFIT = [0, 500, 1000, 1500, 2000, 2500, 3000, 4000,
                    4500, 5000, 6000, 7500, 10000]


def mirrored_dir_values(sentinel: int) -> List[int]:
    """
    Sweep COMPLETA della libreria pattern_directional (±1..51), negativi inclusi.
    n>0: pattern rialzista abilita il long (speculare per lo short).
    n<0: mirroring INVERTITO — il long richiede il pattern RIBASSISTA, lo short
    quello rialzista. Setup contrarian usato dalle TOP_UA counter-trend
    (GC_416 dir_yes=-1, CL_120 dir_yes=-9, FC_420 dir_yes=-33).
    TUTTI i pattern sono ammessi purché calcolati al momento opportuno: i motori
    a ordini pendenti (stop/limit/market next-bar) valutano alla chiusura della
    barra segnale; quelli con fill same-bar-open shiftano la maschera di 1 barra.
    """
    pos = list(range(1, 52))
    return [sentinel] + pos + [-v for v in pos]


def neutral_values(sentinel: int) -> List[int]:
    """Sweep COMPLETA della libreria pattern_neutral (1..54). Stessa regola dei
    direzionali: nessuna esclusione — la correttezza temporale è garantita dal
    timing di valutazione del motore (fill next-bar, o shift della maschera)."""
    return [sentinel] + list(range(1, 55))


def fast_values(sentinel: int) -> List[int]:
    """
    Sweep COMPLETA della libreria pattern_fast (1..151).
    Liste *_yes (sentinel=152, sempre True): includono anche 153 (sempre False)
    così la sweep può SPEGNERE una direzione — le strategie one-sided diventano
    raggiungibili (NQ_181 è long-only con ptn_sy_yes=153; audit 2026-07-11).
    Liste *_no (sentinel=153): il 153 in testa significa "nessuna esclusione".
    """
    vals = [sentinel] + list(range(1, 152))
    if sentinel == 152:
        vals.append(153)
    return vals


def uaptnbase_values(sentinel: int) -> List[int]:
    """
    Sweep COMPLETA della libreria pattern_uaptnbase (1..40).
    Liste *_yes (sentinel=41, sempre True): includono anche 42 (sempre False)
    per rendere raggiungibili i setup one-sided (stessa logica di fast_values).
    Liste *_no (sentinel=42): nessuna esclusione di default.
    """
    vals = [sentinel] + list(range(1, 41))
    if sentinel == 41:
        vals.append(42)
    return vals


def multiday_max_bars(timeframe_minutes: int) -> List[int]:
    """
    Griglia max_bars con valori multiday scalati sul timeframe (sessione
    futures ~23h = 1380 min): 2/4/7/10 sessioni in posizione, come le TOP_UA
    multiday (S_674=4gg, FC_420=7gg, GC_291=~4.7gg; 10 sessioni copre
    NQ_531=230 barre 60m, NQ_956=8gg, NQ_796=9gg — audit 2026-07-11).
    I valori grandi hanno effetto pieno con intraday_only=0; con
    intraday_only=1 il flat di fine sessione li rende inerti (la sweep li
    scarta da sola).
    """
    bpd = max(1, int(1380 / max(1, int(timeframe_minutes))))
    vals = [0, 12, 24, 48, 2 * bpd, 4 * bpd, 7 * bpd, 10 * bpd]
    out: List[int] = []
    for v in vals:
        if v not in out:
            out.append(v)
    return out


@dataclass
class EngineSignals:
    """Output di un motore: segnali e prezzi di ingresso/uscita."""
    # Segnali booleani (True = genera ordine alla barra)
    entries_long: pd.Series
    entries_short: pd.Series

    # Prezzo di ingresso per barra (NaN se non rilevante)
    entry_price_long: pd.Series
    entry_price_short: pd.Series

    # Tipo di ordine: "stop" (TF, breakout), "market" (BIAS), "limit" (reversal)
    entry_type: str = "stop"

    # Uscite forzate per segnale (es. BIAS: esci alla barra N-esima)
    exits_long: Optional[pd.Series] = None
    exits_short: Optional[pd.Series] = None

    # Cancellazione esplicita di un ordine stop/limit PENDENTE prima del cambio
    # sessione: True alla barra in cui l'ordine deve SCADERE. Usato dal BIAS
    # entry_type 2/3 per far scadere l'ordine a fine finestra (in EL gli ordini
    # "next bar" durano una sola barra e si ri-emettono solo dentro la finestra).
    # Se None il simulatore non cancella nulla (comportamento storico invariato).
    cancel_long: Optional[pd.Series] = None
    cancel_short: Optional[pd.Series] = None

    # Se True: al massimo UNA entrata per sessione per direzione (stop/limit).
    # BIAS 2/3: in EL OKLong/OKShort vengono disarmati dopo il fill -> niente
    # ri-entrata nella stessa sessione. I motori che già emettono il segnale una
    # sola volta per sessione (RHL/VBO/TF...) non sono toccati (default False).
    single_entry_per_session: bool = False

    # Override di SimConfig.exit_on_session_end per questo motore (None = usa il
    # default del SimConfig). MAC (MA-crossover) tiene le posizioni OVERNIGHT e
    # esce solo su incrocio inverso / stop / Friday-EOD -> imposta False, anche su
    # timeframe intraday dove il default e' True.
    exit_on_session_end: Optional[bool] = None

    # Metadata
    notes: str = ""


class BaseEngine:
    """Interfaccia comune. Ogni motore concreto eredita e implementa i metodi."""

    name: str = "BASE"
    description: str = ""

    # Pattern library usata dal motore: "neutral_directional" (mirrored) o "fast" (unmirrored)
    pattern_library: str = "fast"

    # Tipo di entry: "stop", "market", "limit"
    entry_type: str = "stop"

    def __init__(self, market_config: Dict[str, Any]):
        """market_config contiene: session_start_hour, big_point_value, tick_size, timeframe_minutes, etc."""
        self.mc = market_config
        self._memo_cache: Dict[Any, Any] = {}

    # ─── Memo dei calcoli STRUTTURALI (canali, bande, medie) ──────────────────
    # Dentro una fase di sweep la struttura del motore e' fissa e cambiano solo i
    # pattern/filtri: ricalcolare Donchian/Bollinger/SMA a ogni combo era, dopo il
    # passaggio del simulatore a numba, la voce di costo dominante di
    # generate_signals (audit efficienza 2026-07-27). Il memo e' per-istanza, quindi
    # per-processo worker: nessuna condivisione tra processi, nessun pickling.
    @staticmethod
    def df_key(df: pd.DataFrame):
        """Impronta del DataFrame: lunghezza + estremi dell'indice. Distingue IS,
        OOS, fold di walk-forward e mercati diversi senza dipendere da id()."""
        idx = df.index
        if len(idx) == 0:
            return (0, 0, 0)
        return (len(idx), idx[0].value, idx[-1].value)

    def memo(self, df: pd.DataFrame, key, fn):
        """Ritorna fn() con cache su (impronta df, key). Cache piccola: la si
        svuota quando supera 16 voci (le fasi cambiano struttura di rado)."""
        k = (self.df_key(df),) + tuple(key)
        cache = self._memo_cache
        if k in cache:
            return cache[k]
        val = fn()
        if len(cache) >= 16:
            cache.clear()
        cache[k] = val
        return val

    @property
    def is_daily(self) -> bool:
        """True se il timeframe è daily (1440 min) o superiore."""
        return self.mc.get("timeframe_minutes", 60) >= 1440

    @staticmethod
    def apply_direction(entries_long, entries_short, direction: int):
        """
        Filtra i segnali per direzione consentita.
        direction: 0 = entrambi, 1 = solo long, 2 = solo short.
        (utile su mercati strutturalmente direzionali, es. NQ long-biased)
        """
        d = int(direction)
        if d == 1:
            entries_short = entries_short & False
        elif d == 2:
            entries_long = entries_long & False
        return entries_long, entries_short

    def get_param_space(self) -> Dict[str, List[Any]]:
        """
        Ritorna lo spazio dei parametri per lo sweep sequenziale.
        Chiave: nome parametro. Valore: lista di valori da testare.
        Lo sweep sequenziale tiene gli altri parametri al primo valore (default).
        """
        raise NotImplementedError

    def get_ordinal_params(self) -> set:
        """
        Parametri ORDINALI: valori di griglia adiacenti = strategie 'vicine'
        (stop, target, orari, lunghezze, moltiplicatori). Su questi ha senso la
        plateau analysis (TSS2 slide 265, skill §13.2): un picco isolato tra
        vicini scarsi e' overfitting. I pattern e le scelte strutturali
        (vol_source, direction, entry_type, giorni...) sono CATEGORICI: id
        adiacenti non sono strategie simili -> mai smoothing/vicini su quelli.
        """
        return {p for p in self.get_param_space() if p not in CATEGORICAL_PARAMS}

    def get_default_params(self) -> Dict[str, Any]:
        """Default = primo valore di ogni lista in get_param_space()."""
        return {k: v[0] for k, v in self.get_param_space().items()}

    def get_pattern_sentinels(self) -> Dict[str, Any]:
        """
        Mappa param-pattern -> valore sentinella ("nessun filtro") per questo motore.
        Usata dall'optimizer per l'ablation finale: ogni pattern scelto deve battere
        la sentinella A PARITA' di parametri finali, altrimenti si torna alla
        sentinella (metodo Unger: un pattern si tiene solo se i numeri lo supportano).
        """
        yes_sent = {"fast": 152, "uaptnbase": 41}.get(self.pattern_library)
        no_sent  = {"fast": 153, "uaptnbase": 42}.get(self.pattern_library)
        table = {
            "ptn_neut_yes": 55, "ptn_neut_no": 56,
            "ptn_dir_yes": 52,  "ptn_dir_no": 53,
            "ptn_ly_yes": yes_sent, "ptn_sy_yes": yes_sent,
            "ptn_ly_no":  no_sent,  "ptn_sy_no":  no_sent,
        }
        return {p: s for p, s in table.items()
                if s is not None and p in self.get_param_space()}

    def generate_signals(self, df: pd.DataFrame, params: Dict[str, Any]) -> EngineSignals:
        """Genera segnali long/short su tutto il df dato i parametri."""
        raise NotImplementedError

    # ─── Ordine logico delle fasi di ottimizzazione (per sweep sequenziale) ───
    def get_optimization_phases(self) -> List[List[str]]:
        """
        Ordine sequenziale dei gruppi di parametri da ottimizzare.
        Ogni fase ottimizza un sottoinsieme; gli altri restano fissi ai semi
        della fase precedente (beam search, vedi optimizer.py).

        Mappatura sul processo Unger (SKILL metodo-unger §14.B + schema §D):
          §14.B.1 trigger        -> fase 0: struttura del motore (intraday/multiday,
                                   bb_length, channel_len, vol_source, offsets, ...)
          §14.B.2 uscita base    -> NON e' una fase: get_default_params() impone
                                   stop/target di base non-zero durante le sweep
                                   (senza, su intraday vince sempre la sentinella)
          §14.B.3 filtri         -> fasi pattern (neutrali POI direzionali per i
                                   mirrored, long POI short per gli unmirrored,
                                   con le controparti alle sentinelle — schema §D)
                                   e poi fase orari/giorno
          §14.B.4 affina st/tgt  -> ULTIMA fase: stop/target/max_bars(/trailing/BE)
          §14.B.5 stabilita'     -> plateau analysis: smoothing coi vicini di griglia
                                   dentro ogni fase (optimizer._plateau_smooth) +
                                   plateau_ratio/min sulla top 10 (run.plateau_for_params)
          §14.B.6 validazione    -> fuori dal motore: validator IS/OOS + walk-forward di
                                   stabilita' + Walk-Forward Optimization vera
                                   (WalkForwardOptimizer, ri-ottimizza per finestra)
        Vincolo §14 "NON fare": pattern e stop/target MAI nella stessa fase.
        Nota: lo schema §D accorpa orari+stop/target in un'unica chiamata; qui sono
        due fasi sequenziali (fedeli ai passi 3-4 di §14.B) perche' il prodotto
        congiunto orari x risk (~1.5M combo/seme) e' infattibile; il beam K>=2
        recupera parte dell'interazione persa.
        """
        raise NotImplementedError
