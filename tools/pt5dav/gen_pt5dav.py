"""Genera le classi PT5DAV dal CSV della consegna, parametri verbatim.

uso: python tools/pt5dav/gen_pt5dav.py SIMBOLO [SIMBOLO...]   (ALL = tutti)

keep135.txt e' il perimetro deciso il 24/09/2026: le strategie che non tengono piu' di una notte
(vedi docs/domini/mappa-strategie-pt5dav.md). Le classi si cambiano rigenerandole, non a mano.
"""
import csv, re, sys, os, html

BASE = r"C:\piootoo-dev\piootoo-repository\PT5DAV"
OUT = r"C:\piootoo-dev\Piootoo.Strategies\PT5DAVStrategies"
KEEP = [l.strip() for l in open(os.path.join(os.path.dirname(__file__), "keep135.txt")) if l.strip()]

ENGINES = {
    "TF_M": ("TFM", "Pt5DavTfMirroredEngine"),
    "TF_U": ("TFU", "Pt5DavTfUnmirroredEngine"),
    "PC": ("PCH", "Pt5DavPriceChannelEngine"),
    "BO": ("SBO", "Pt5DavSessionBreakoutEngine"),
    "BO_S": ("BOS", "Pt5DavCurrentSessionBreakoutEngine"),
    "VBO": ("VBO", "Pt5DavVolatilityBreakoutEngine"),
    "LF": ("LFD", "Pt5DavPivotFaderEngine"),
    "LF_HL": ("LFH", "Pt5DavHighLowFaderEngine"),
    "RHL": ("RHL", "Pt5DavRhlEngine"),
    "RBB_M": ("RBM", "Pt5DavBollingerMirroredEngine"),
    "RBB_U": ("RBU", "Pt5DavBollingerUnmirroredEngine"),
    "BIAS": ("BIA", "Pt5DavBiasMarketEngine"),
    "BIAS_RT": ("BRT", "Pt5DavBiasRetracementEngine"),
    "BIAS_BO": ("BBO", "Pt5DavBiasBreakoutEngine"),
    "MAC": ("MAC", "Pt5DavMovingAverageCrossoverEngine"),
}
TF = {"15m": 15, "30m": 30, "1h": 60, "4h": 240}

rows = {r["codice"]: r for r in csv.DictReader(open(os.path.join(BASE, "strategie_224.csv"), encoding="utf-8"))}
schede = open(os.path.join(BASE, "schede_224.md"), encoding="utf-8").read()


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


# Numerazione: per (simbolo, sigla), in ordine di timeframe e poi di codice, sulle 135.
order = sorted(KEEP, key=lambda c: (rows[c]["simbolo"], ENGINES[rows[c]["motore"]][0], TF[rows[c]["timeframe"]], c))
numbers, counters = {}, {}
for c in order:
    key = (rows[c]["simbolo"], ENGINES[rows[c]["motore"]][0])
    counters[key] = counters.get(key, 0) + 1
    numbers[c] = counters[key]


def class_name(c):
    r = rows[c]
    return f"PT5DAV_{r['simbolo']}_{ENGINES[r['motore']][0]}_{numbers[c]:03d}_{TF[r['timeframe']]}"


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


def gates_fast(r, prefix_long="FastYesLong", names=("FastYesLong", "FastNoLong", "FastYesShort", "FastNoShort")):
    cols = ["ptn_ly_yes", "ptn_ly_no", "ptn_sy_yes", "ptn_sy_no"]
    return [f"{n} = {i(r[c])};".ljust(40) + f"// {c}" for n, c in zip(names, cols)]


def body(r):
    m = r["motore"]
    w, wnote = window(r)
    lines = [f"TradingWindow = {w};".ljust(40) + f"// {wnote}"]
    if m in ("TF_M",):
        lines += gates_mirrored(r) + skip(r)
    elif m == "TF_U":
        lines += gates_fast(r) + skip(r)
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
        lines += [
            f"VolatilitySource = {i(r['vol_source'])};".ljust(40) + "// vol_source: 1 range d1, 3 ATR di barra",
            f"AtrLength = {i(r['atr_len'])};".ljust(40) + "// atr_len (letto solo con vol_source 3)",
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


def render(c):
    r = rows[c]
    name = class_name(c)
    sigla, base = ENGINES[r["motore"]]
    tf = TF[r["timeframe"]]
    s_n, b_n = r["s_n"], r["b_n"]
    doc = xml_doc(scheda(c))
    lines = "\n".join("        " + l for l in body(r))
    return f'''using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>{name}</b> — {r["motore"]} su {r["simbolo"]} {r["timeframe"]}, codice della ricerca <c>{c}</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia {s_n} trade, netto ${float(r["s_netto"]):,.0f}; broker (27/08/2025 → 09/09/2026)
/// {b_n} trade, netto ${float(r["b_netto"]):,.0f}. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/{c}.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
{doc}
/// </summary>
public sealed class {name} : {base}
{{
    public override string Name => "{name}";

    public override string Description => "{r["motore"]} {r["simbolo"]} {r["timeframe"]}, ricerca PT5DAV {c}";

    public override string Symbol => "@{r["simbolo"]}";

    public override int TimeframeMinutes => {tf};

    public override string ResearchCode => "{c}";

    public {name}()
    {{
{lines}
    }}
}}
'''


def main():
    wanted = sys.argv[1:]
    todo = [c for c in KEEP if "ALL" in wanted or rows[c]["simbolo"] in wanted]
    # Le FDAX a 4 ore restano fuori: la ricerca le ha su barre 08-12/12-16/16-20/20-22, che il feed
    # (ancorato all'01:00) e il cBot non costruiscono. La numerazione le conta comunque, cosi' quando
    # entreranno non sposteranno i numeri delle altre.
    skipped = [c for c in todo if rows[c]["simbolo"] == "FDAX" and rows[c]["timeframe"] == "4h"]
    todo = [c for c in todo if c not in skipped]
    for c in skipped:
        print(f"SALTATA {class_name(c):28} <- {c} (FDAX 4h: griglia 08-12-16-20-22 non disponibile)")
    os.makedirs(OUT, exist_ok=True)
    for c in sorted(todo, key=class_name):
        name = class_name(c)
        with open(os.path.join(OUT, name + ".cs"), "w", encoding="utf-8", newline="\n") as f:
            f.write(render(c))
        print(f"{name:28} <- {c}")


main()
