"""Genera le classi PT8DAV dal CSV della consegna v5.1 (run-engine-v3, 111 strategie), parametri verbatim.

uso: python tools/pt8dav/gen_pt8dav.py SIMBOLO [SIMBOLO...]   (ALL = tutti)

La serie PT8DAV (01/10/2026) porta la consegna v5.1 del 25/09/2026: stessa ricerca e stessi motori
della v5.0 gia' portata in PT5DAV (tools/pt5dav/gen_pt5dav.py), ma parametri scelti sui dati fino al
31/05/2022 e filtro sui tre anni dopo. Le classi derivano dai motori PT5DAV, che non si copiano.

Differenze dal generatore PT5DAV:
- una fase sola di numerazione, per (simbolo, sigla) in ordine di timeframe e codice;
- le FDAX a 4 ore SI generano. La ricerca le ha su barre 08-12/12-16/16-20/20-22, che il feed
  (griglia a 4 ore ancorata all'01:00, quella delle PT3B) non ha: la classe riceve la serie a 60
  minuti e dichiara BarMinutes = 240, e il motore piega le barre. Sulla griglia 01:00 con i
  parametri verbatim tre su cinque non aprivano mai un trade (misurato il 01/10/2026);
- le colonne nuove `ordine` ed `entrate` non sono parametri: si verificano contro cio' che il motore
  fa, e una riga che non torna ferma la generazione.

Vedi docs/domini/mappa-strategie-pt8dav.md. Le classi si cambiano rigenerandole, non a mano.
"""
import csv, re, sys, os, html

BASE = r"C:\piootoo-dev\piootoo-repository\run-engine-v3"
OUT = r"C:\piootoo-dev\Piootoo.Strategies\PT8DAVStrategies"

# motore della ricerca -> (sigla, motore PT5DAV, tipo d'ordine, entrate): le ultime due sono cio' che
# il motore C# fa (EntryStopNextBar/EntryLimitNextBar/EntryMarketNextBar e oneEntryPerSessionPerSide),
# e devono coincidere con le colonne `ordine` ed `entrate` della consegna.
PER_SESSION = "una per sessione e per direzione"
NO_LIMIT = "nessun limite"
PER_WEEK = "una per settimana e per direzione"
ENGINES = {
    "TF_M": ("TFM", "Pt5DavTfMirroredEngine", "stop", PER_SESSION),
    "PC": ("PCH", "Pt5DavPriceChannelEngine", "stop", PER_SESSION),
    "BO": ("SBO", "Pt5DavSessionBreakoutEngine", "stop", PER_SESSION),
    "BO_S": ("BOS", "Pt5DavCurrentSessionBreakoutEngine", "stop", PER_SESSION),
    "VBO": ("VBO", "Pt5DavVolatilityBreakoutEngine", "stop", PER_SESSION),
    "LF": ("LFD", "Pt5DavPivotFaderEngine", "market", NO_LIMIT),
    "LF_HL": ("LFH", "Pt5DavHighLowFaderEngine", "market", NO_LIMIT),
    "RHL": ("RHL", "Pt5DavRhlEngine", "limit", PER_SESSION),
    "RBB_M": ("RBM", "Pt5DavBollingerMirroredEngine", "limit", PER_SESSION),
    "RBB_U": ("RBU", "Pt5DavBollingerUnmirroredEngine", "limit", PER_SESSION),
    "BIAS": ("BIA", "Pt5DavBiasMarketEngine", "market", PER_SESSION),
    "BIAS_RT": ("BRT", "Pt5DavBiasRetracementEngine", "limit", PER_SESSION),
    "BIAS_BO": ("BBO", "Pt5DavBiasBreakoutEngine", "stop", PER_SESSION),
    "MAC": ("MAC", "Pt5DavMovingAverageCrossoverEngine", "market", NO_LIMIT),
    "BIASW": ("BSW", "Pt5DavBiasWeeklyEngine", "market", PER_WEEK),
}
TF = {"15m": 15, "30m": 30, "1h": 60, "4h": 240}

rows = {r["codice"]: r for r in csv.DictReader(open(os.path.join(BASE, "strategie_111.csv"), encoding="utf-8"))}
schede = open(os.path.join(BASE, "schede_111.md"), encoding="utf-8").read()


def num(v):
    return float(v) if v not in ("", None) else None


def i(v):
    f = num(v)
    assert f is not None and f == int(f), v
    return int(f)


def d(v):
    f = num(v)
    assert f is not None, v
    s = repr(f)
    if s.endswith(".0"):
        s = s[:-2]
    return s + "m"


def is_fdax_4h(c):
    return rows[c]["simbolo"] == "FDAX" and rows[c]["timeframe"] == "4h"


def sort_key(c):
    return (rows[c]["simbolo"], ENGINES[rows[c]["motore"]][0], TF[rows[c]["timeframe"]], c)


ALL = list(rows)
numbers, counters = {}, {}
for c in sorted(ALL, key=sort_key):
    key = (rows[c]["simbolo"], ENGINES[rows[c]["motore"]][0])
    counters[key] = counters.get(key, 0) + 1
    numbers[c] = counters[key]


def class_name(c):
    r = rows[c]
    return f"PT8DAV_{r['simbolo']}_{ENGINES[r['motore']][0]}_{numbers[c]:03d}_{TF[r['timeframe']]}"


def scheda(code):
    m = re.search(r"^## Famiglia .*`" + re.escape(code) + r"`.*$", schede, re.M)
    assert m, code
    rest = schede[m.end():]
    end = rest.find("\n---")
    text = rest[:end] if end >= 0 else rest
    body = text.split("### Come e' stata scelta")[0]
    return body.strip()


def xml_doc(md):
    out = []
    for line in md.splitlines():
        line = line.rstrip()
        if not line:
            continue
        line = line.replace("**", "").replace("`", "'")
        line = html.escape(line, quote=False)
        if line.startswith("### "):
            out.append(f"/// <para><b>{line[4:]}</b></para>")
        elif line.startswith("- "):
            out.append(f"/// <para>· {line[2:]}</para>")
        else:
            out.append(f"/// <para>{line}</para>")
    return "\n".join(out)


def window(r):
    s, e = r["start_hour"], r["end_hour"]
    if s == "" and e == "":
        return "ResearchWindow(-1, -1)", "nessun filtro orario (-1/-1)"
    return f"ResearchWindow({i(s)}, {i(e)})", f"start_hour {i(s)}, end_hour {i(e)}, verbatim"


def holding_lines(r):
    lines = []
    if r["intraday_only"] != "":
        intraday = i(r["intraday_only"]) == 1
        lines.append(f"IntradayOnly = {'true' if intraday else 'false'};".ljust(40) + f"// intraday_only {i(r['intraday_only'])} ({r['tenuta']})")
        mb = i(r["max_bars"]) if r["max_bars"] != "" else 0
        lines.append(f"MaxBars = {mb};".ljust(40) + "// max_bars" + (" (0 = nessun limite)" if mb == 0 else " (la giornata della ricerca in barre)"))
    return lines


def stops(r):
    return [
        f"StopAtr = {d(r['stop_atr'])};".ljust(40) + "// stop_atr: stop in ATR50 dal prezzo d'ingresso",
        f"TargetAtr = {d(r['take_profit_atr'])};".ljust(40) + "// take_profit_atr" + (" (0 = nessun target)" if num(r['take_profit_atr']) == 0 else ""),
    ]


def skip(r, field="skip_day"):
    return [f"SkipDay = {i(r[field])};".ljust(40) + "// skip_day, pandas 0 = lunedi' (-1 = nessuno)"]


def gates_mirrored(r, with_no=True):
    out = [
        f"NeutralYes = {i(r['ptn_neut_yes'])};".ljust(40) + "// ptn_neut_yes",
        f"NeutralNo = {i(r['ptn_neut_no'])};".ljust(40) + "// ptn_neut_no",
        f"DirectionalYes = {i(r['ptn_dir_yes'])};".ljust(40) + "// ptn_dir_yes",
    ]
    if with_no:
        out.append(f"DirectionalNo = {i(r['ptn_dir_no'])};".ljust(40) + "// ptn_dir_no")
    return out


def gates_fast(r, names=("FastYesLong", "FastNoLong", "FastYesShort", "FastNoShort")):
    cols = ["ptn_ly_yes", "ptn_ly_no", "ptn_sy_yes", "ptn_sy_no"]
    return [f"{n} = {i(r[c])};".ljust(40) + f"// {c}" for n, c in zip(names, cols)]


# Le colonne di parametro che ogni motore legge. Tutte le altre devono essere vuote o al valore
# spento: una colonna valorizzata che il motore non legge e' un pezzo di strategia perso in silenzio.
COMMON = {"intraday_only", "start_hour", "end_hour", "max_bars", "stop_atr", "take_profit_atr"}
MIRRORED = {"ptn_neut_yes", "ptn_neut_no", "ptn_dir_yes", "ptn_dir_no"}
FAST = {"ptn_ly_yes", "ptn_ly_no", "ptn_sy_yes", "ptn_sy_no"}
READS = {
    "TF_M": COMMON | MIRRORED | {"skip_day"},
    "PC": COMMON | MIRRORED | {"skip_day", "channel_len", "breakout_offset_atr", "direction", "trailing_stop", "breakeven", "dvol_min"},
    "BO": COMMON | MIRRORED | {"skip_day", "level_source", "n_sess", "lev_include_sess0", "breakout_offset_atr"},
    "BO_S": COMMON | MIRRORED | {"skip_day", "level_source", "breakout_offset_atr"},
    "VBO": COMMON | MIRRORED | {"skip_day", "vol_source", "atr_len", "vol_mult", "vol_mult_short", "momentum", "direction", "trailing_stop", "breakeven"},
    "LF": COMMON | (MIRRORED - {"ptn_dir_no"}) | FAST | {"level_choice", "level_shift_atr", "not_le_day", "not_se_day"},
    "LF_HL": COMMON | (MIRRORED - {"ptn_dir_no"}) | FAST | {"level_choice", "level_shift_atr", "not_le_day", "not_se_day"},
    "RHL": COMMON | MIRRORED | {"skip_day", "long_offset_atr", "short_offset_atr", "direction"},
    "RBB_M": COMMON | MIRRORED | {"skip_day", "bb_length", "bb_num_devs"},
    "RBB_U": COMMON | FAST | {"skip_day", "bb_length", "bb_num_devs"},
    "BIAS": COMMON | FAST | {"entry_type", "le_bar", "lx_bar", "se_bar", "sx_bar", "end_long", "end_short", "nhigh", "nlow", "not_le_day", "not_se_day"},
    "BIASW": COMMON | FAST | {"le_day", "lx_day", "le_time", "lx_time", "se_day", "sx_day", "se_time", "sx_time", "entrata_inizio"},
    "MAC": COMMON | {"fast", "slow", "gradient_length", "gradient_factor", "daily_factor", "direction", "trailing_stop"},
}
READS["BIAS_RT"] = READS["BIAS_BO"] = READS["BIAS"]
HEADER = list(next(iter(rows.values())).keys())
# Da intraday_only alla prima metrica: nella v5.1 le leve della MAC stanno dopo entrata_inizio.
PARAMETER_COLUMNS = HEADER[HEADER.index("intraday_only"):HEADER.index("s_n")]
# Valori che vogliono dire "spento" per una colonna che il motore non legge.
OFF = {"skip_day": (-1,), "not_le_day": (-1,), "not_se_day": (-1,), "trailing_stop": (0,), "breakeven": (0,),
       "dvol_min": (0,), "direction": (0,), "ptn_dir_no": (0,),
       # BO_S (level_source 1) prende il massimo/minimo della sessione in corso: n_sess e
       # lev_include_sess0 restano ai default del motore Python e non entrano nella regola
       # (scheda: "massimo in costruzione della sessione corrente"). Per BO sono letti.
       "n_sess": (1,), "lev_include_sess0": (0,)}


def check_unread(r):
    m = r["motore"]
    for col in PARAMETER_COLUMNS:
        if col in READS[m] or r[col] == "":
            continue
        assert num(r[col]) in OFF.get(col, ()), f"{r['codice']}: la colonna {col} = {r[col]} non e' letta dal motore {m}"


def body(r):
    m = r["motore"]
    check_unread(r)
    if r["direction"] != "":
        assert i(r["direction"]) in (0, 1, 2), (r["codice"], r["direction"])
    w, wnote = window(r)
    lines = [f"TradingWindow = {w};".ljust(40) + f"// {wnote}"]
    if m in ("TF_M",):
        lines += gates_mirrored(r) + skip(r)
    elif m == "PC":
        for k in ("trailing_stop", "breakeven", "dvol_min"):
            assert num(r[k]) in (None, 0), (r["codice"], k)
        lines += [
            f"ChannelBars = {i(r['channel_len'])};".ljust(40) + "// channel_len, barra di segnale inclusa",
            f"OffsetAtr = {d(r['breakout_offset_atr'])};".ljust(40) + "// breakout_offset_atr",
            f"Direction = {i(r['direction'])};".ljust(40) + "// direction: 0 entrambe, 1 long, 2 short",
        ] + gates_mirrored(r) + skip(r)
    elif m == "BO":
        assert i(r["level_source"]) == 0
        lines += [
            f"Sessions = {i(r['n_sess'])};".ljust(40) + "// n_sess",
            f"IncludeCurrentSession = {'true' if i(r['lev_include_sess0']) == 1 else 'false'};".ljust(40) + f"// lev_include_sess0 {i(r['lev_include_sess0'])}",
            f"OffsetAtr = {d(r['breakout_offset_atr'])};".ljust(40) + "// breakout_offset_atr",
        ] + gates_mirrored(r) + skip(r)
    elif m == "BO_S":
        assert i(r["level_source"]) == 1
        lines += [f"OffsetAtr = {d(r['breakout_offset_atr'])};".ljust(40) + "// breakout_offset_atr"] + gates_mirrored(r) + skip(r)
    elif m == "VBO":
        for k in ("trailing_stop", "breakeven"):
            assert num(r[k]) in (None, 0), (r["codice"], k)
        # I valori che il motore C# conosce. vol_source 2 (ATR di Wilder sulle sessioni) e' nato con
        # la v5.1: il generatore lo scriveva verbatim e il motore, che conosceva solo 1 e 3, non
        # apriva mai un trade. Un valore nuovo deve fermare qui.
        assert i(r["vol_source"]) in (1, 2, 3), (r["codice"], r["vol_source"])
        assert i(r["momentum"]) in (0, 1, 2), (r["codice"], r["momentum"])
        vol_note = {1: "range della sessione d1", 2: "ATR di Wilder su atr_len sessioni", 3: "ATR su atr_len barre"}[i(r["vol_source"])]
        lines += [
            f"VolatilitySource = {i(r['vol_source'])};".ljust(40) + f"// vol_source: {vol_note}",
            f"AtrLength = {i(r['atr_len'])};".ljust(40) + "// atr_len (sessioni con vol_source 2, barre con 3; non letto con 1)",
            f"MultiplierLong = {d(r['vol_mult'])};".ljust(40) + "// vol_mult",
            f"MultiplierShort = {d(r['vol_mult_short'])};".ljust(40) + "// vol_mult_short (-1 = come il long)",
            f"Momentum = {i(r['momentum'])};".ljust(40) + "// momentum",
            f"Direction = {i(r['direction'])};".ljust(40) + "// direction: 0 entrambe, 1 long, 2 short",
        ] + gates_mirrored(r) + skip(r)
    elif m in ("LF", "LF_HL"):
        assert i(r["level_choice"]) == (1 if m == "LF" else 2)
        lines += [f"LevelShiftAtr = {d(r['level_shift_atr'])};".ljust(40) + "// level_shift_atr"]
        lines += gates_mirrored(r, with_no=False)
        lines += gates_fast(r, names=("BaseYesLong", "BaseNoLong", "BaseYesShort", "BaseNoShort"))
        lines += [
            f"NotEntryDayLong = {i(r['not_le_day'])};".ljust(40) + "// not_le_day, pandas",
            f"NotEntryDayShort = {i(r['not_se_day'])};".ljust(40) + "// not_se_day, pandas",
        ]
    elif m == "RHL":
        lines += [
            f"LongOffsetAtr = {d(r['long_offset_atr'])};".ljust(40) + "// long_offset_atr",
            f"ShortOffsetAtr = {d(r['short_offset_atr'])};".ljust(40) + "// short_offset_atr",
            f"Direction = {i(r['direction'])};".ljust(40) + "// direction: 0 entrambe, 1 long, 2 short",
        ] + gates_mirrored(r) + skip(r)
    elif m in ("RBB_M", "RBB_U"):
        lines += [
            f"BollingerLength = {i(r['bb_length'])};".ljust(40) + "// bb_length",
            f"BollingerNumDevs = {d(r['bb_num_devs'])};".ljust(40) + "// bb_num_devs",
        ]
        lines += gates_mirrored(r) if m == "RBB_M" else gates_fast(r)
        lines += skip(r)
    elif m in ("BIAS", "BIAS_RT", "BIAS_BO"):
        expected = {"BIAS": 1, "BIAS_BO": 2, "BIAS_RT": 3}[m]
        assert i(r["entry_type"]) == expected, r["codice"]
        lines += [
            f"ArmBarLong = {i(r['le_bar'])};".ljust(40) + "// le_bar",
            f"ExitBarLong = {i(r['lx_bar'])};".ljust(40) + "// lx_bar",
            f"EndLong = {i(r['end_long'])};".ljust(40) + "// end_long",
            f"ArmBarShort = {i(r['se_bar'])};".ljust(40) + "// se_bar",
            f"ExitBarShort = {i(r['sx_bar'])};".ljust(40) + "// sx_bar",
            f"EndShort = {i(r['end_short'])};".ljust(40) + "// end_short",
            f"BreakoutBarsHigh = {i(r['nhigh'])};".ljust(40) + "// nhigh",
            f"BreakoutBarsLow = {i(r['nlow'])};".ljust(40) + "// nlow",
        ]
        lines += gates_fast(r, names=("PatternLongYes", "PatternLongNo", "PatternShortYes", "PatternShortNo"))
        lines += [
            f"NotEntryDayLong = {i(r['not_le_day'])};".ljust(40) + "// not_le_day, pandas",
            f"NotEntryDayShort = {i(r['not_se_day'])};".ljust(40) + "// not_se_day, pandas",
        ]
    elif m == "BIASW":
        assert i(r["entrata_inizio"]) == 1, r["codice"]
        def hhmm(v):
            n = i(v)
            return f"new TimeOnly({n // 100}, {n % 100})"
        lines += [
            f"EntryDayLong = {i(r['le_day'])};".ljust(40) + "// le_day, pandas (-1 = long spento)",
            f"EntryTimeLong = {hhmm(r['le_time'])};".ljust(40) + f"// le_time {i(r['le_time'])}: apre la barra d'ingresso",
            f"ExitDayLong = {i(r['lx_day'])};".ljust(40) + "// lx_day, pandas",
            f"ExitTimeLong = {hhmm(r['lx_time'])};".ljust(40) + f"// lx_time {i(r['lx_time'])}: chiude la barra d'uscita",
            f"EntryDayShort = {i(r['se_day'])};".ljust(40) + "// se_day, pandas (-1 = short spento)",
            f"EntryTimeShort = {hhmm(r['se_time'])};".ljust(40) + f"// se_time {i(r['se_time'])}",
            f"ExitDayShort = {i(r['sx_day'])};".ljust(40) + "// sx_day, pandas",
            f"ExitTimeShort = {hhmm(r['sx_time'])};".ljust(40) + f"// sx_time {i(r['sx_time'])}",
        ]
        lines += gates_fast(r, names=("PatternLongYes", "PatternLongNo", "PatternShortYes", "PatternShortNo"))
    elif m == "MAC":
        assert num(r["trailing_stop"]) in (None, 0), r["codice"]
        lines += [
            f"FastPeriod = {i(r['fast'])};".ljust(40) + "// fast",
            f"SlowPeriod = {i(r['slow'])};".ljust(40) + "// slow",
            f"GradientPeriod = {i(r['gradient_length'])};".ljust(40) + "// gradient_length",
            f"GradientFactor = {d(r['gradient_factor'])};".ljust(40) + "// gradient_factor",
            f"DailyFactor = {d(r['daily_factor'])};".ljust(40) + "// daily_factor",
            f"Direction = {i(r['direction'])};".ljust(40) + "// direction: 0 entrambe, 1 long, 2 short",
        ]
    else:
        raise NotImplementedError(m)
    lines += holding_lines(r) + stops(r)
    return lines


FDAX_4H_NOTE = '''///
/// <para><b>Barre a 4 ore piegate dal motore.</b> La ricerca ha costruito questa strategia su barre
/// 08-12, 12-16, 16-20, 20-22 (ora di Roma), che nel feed non esistono: la griglia a 4 ore del DAX
/// e' ancorata all'01:00, quella delle PT3B. La classe riceve quindi la serie <b>a 60 minuti</b>
/// (<c>TimeframeMinutes</c>) e ragiona su barre da 240 (<c>BarMinutes</c>) che il motore si
/// costruisce: vedi <see cref="Pt5DavEngineBase.BarMinutes"/>. Il nome dice la barra della strategia,
/// non la serie.</para>
'''


def render(c):
    r = rows[c]
    name = class_name(c)
    sigla, base, order, entries = ENGINES[r["motore"]]
    assert r["ordine"] == order, f"{c}: ordine {r['ordine']} ma il motore {base} emette {order}"
    assert r["entrate"] == entries, f"{c}: entrate '{r['entrate']}' ma il motore {base} fa '{entries}'"
    tf = TF[r["timeframe"]]
    s_n, b_n = r["s_n"], r["b_n"]
    doc = xml_doc(scheda(c))
    lines = "\n".join("        " + l for l in body(r))
    note = FDAX_4H_NOTE if is_fdax_4h(c) else ""
    # Le FDAX a 4 ore ricevono la serie oraria e piegano le barre in motore (Pt5DavEngineBase.BarMinutes).
    stream = 60 if is_fdax_4h(c) else tf
    bar_minutes = f"\n    public override int BarMinutes => {tf};\n" if is_fdax_4h(c) else ""
    return f'''using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>{name}</b> — {r["motore"]} su {r["simbolo"]} {r["timeframe"]}, codice della ricerca <c>{c}</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>{r["ordine"]}</b>, entrate: <b>{r["entrate"]}</b>, tenuta: <b>{r["tenuta"]}</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) {s_n} trade, netto {float(r["s_netto"]):,.0f}; fuori campione 06/2022 → 05/2025 netto
/// {float(r["f_netto"]):,.0f}; broker (16/09/2025 → 09/09/2026) {b_n} trade, netto {float(r["b_netto"]):,.0f}.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/{c}.csv</c>.</para>
{note}///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
{doc}
/// </summary>
public sealed class {name} : {base}
{{
    public override string Name => "{name}";

    public override string Description => "{r["motore"]} {r["simbolo"]} {r["timeframe"]}, ricerca v5.1 {c}";

    public override string Symbol => "@{r["simbolo"]}";

    public override int TimeframeMinutes => {stream};
{bar_minutes}
    public override string ResearchCode => "{c}";

    public {name}()
    {{
{lines}
    }}
}}
'''


def main():
    wanted = sys.argv[1:]
    todo = [c for c in ALL if "ALL" in wanted or rows[c]["simbolo"] in wanted]
    os.makedirs(OUT, exist_ok=True)
    for c in sorted(todo, key=class_name):
        name = class_name(c)
        with open(os.path.join(OUT, name + ".cs"), "w", encoding="utf-8", newline="\n") as f:
            f.write(render(c))
        print(f"{name:28} <- {c}")


main()
