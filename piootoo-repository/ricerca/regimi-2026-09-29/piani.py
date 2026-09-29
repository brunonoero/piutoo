"""I piani in produzione misurati per regime di mercato.

Uso: python piani.py

Due viste:
- FTMO 2022-2026: i tre piani del demo 17202911 dai run 2022-2026 sul feed FTMO (PT3B dai run con
  il PlanCode del piano, O4 dal run neutro PT5DAV-TUTTE con la stessa holding, a size 0,5).
  Etichette di regime dal feed FTMO, perche' e' il mercato che il conto vede.
- storia 2012-2025: le sette strategie di O4 dal run storico sul feed interno (le PT3B non hanno un
  run lungo).

Per ogni piano e per il conto: P&L per regime del PROPRIO simbolo di ogni trade, e P&L giornaliero
per regime di un mercato comune (ES) con quante strategie perdono in quel regime.
"""
import json
import os
import statistics
from collections import defaultdict
from datetime import datetime

import regimi
from regimi import load_daily, classify, label_for

REPO = regimi.REPO
WS = os.path.join(REPO, "workspaces")
NAMES = ("trend-su", "trend-giu", "laterale", "calma", "normale", "agitata")

PLANS = {
    "FTMO-EUROPA": ["PT3B_FDAX_PCH_002_240", "PT3B_FDAX_RHL_001_240", "PT3B_FESX_RHL_001_240"],
    "FTMO-USA": ["PT3B_ES_RHL_001_240", "PT3B_NQ_RHL_001_240", "PT3B_YM_RHL_001_240"],
    "FTMO-O4-X05": ["PT5DAV_FDAX_BOS_001_15", "PT5DAV_FDAX_PCH_002_60", "PT5DAV_GC_LFD_002_60",
                    "PT5DAV_KC_LFH_001_60", "PT5DAV_NQ_BSW_001_240", "PT5DAV_NQ_LFD_004_240",
                    "PT5DAV_YM_LFH_003_240"],
}
SIZE = {"FTMO-O4-X05": 0.5}

FTMO_RUNS = [
    (os.path.join(WS, r"ftmo-pt3b-indici\backtests\ftmo-pt3b-europa-2022-2026\trades.json"), "FTMO-EUROPA"),
    (os.path.join(WS, r"ftmo-pt3b-indici\backtests\ftmo-pt3b-usa-2022-2026\trades.json"), "FTMO-USA"),
    (os.path.join(WS, r"ftmo-pt5dav-congelato\backtests\pt5dav-tutte-2022-2026\trades.json"), "FTMO-O4-X05"),
]
HISTORY_RUN = os.path.join(REPO, r"ricerca\piani-pesati-2026-09-27\run-storia-senza-pesi\trades.json")


def load_trades(runs):
    out = []
    for path, plan in runs:
        wanted = set(PLANS[plan])
        with open(path, encoding="utf-8-sig") as f:
            for t in json.load(f):
                if t["strategyCode"] in wanted:
                    t["plan"] = plan
                    t["pnl"] = float(t["netProfit"]) * SIZE.get(plan, 1)
                    out.append(t)
    return out


def labels_for(symbols, sources):
    regimi.SOURCES = sources
    res = {}
    for s in symbols:
        order, bars, src = load_daily(s)
        if order:
            lab = classify(order, bars)
            res[s] = (lab, sorted(lab), src)
    return res


def day(ts):
    return datetime.fromisoformat(ts.replace("Z", "+00:00")).date()


def fmt(x):
    return f"{x:,.0f}".replace(",", ".")


def report(title, trades, labels, lines):
    lines.append(f"\n## {title}\n")
    lines.append("Fonti delle etichette: " + ", ".join(f"{s} `{v[2]}`" for s, v in sorted(labels.items())))
    first = min(day(t["entryTimeUtc"]) for t in trades)
    last = max(day(t["entryTimeUtc"]) for t in trades)
    lines.append(f"\nTrade dal {first} al {last}: {len(trades)}.\n")

    # A) regime del proprio simbolo al giorno di ingresso
    groups = defaultdict(list)
    for t in trades:
        lab = label_for(*labels[t["symbol"]][:2], day(t["entryTimeUtc"])) if t["symbol"] in labels else None
        groups[t["plan"]].append((lab, t["pnl"], t["strategyCode"]))
        if len({x["plan"] for x in trades}) > 1:
            groups["CONTO"].append((lab, t["pnl"], t["strategyCode"]))
    lines.append("### Netto per regime del mercato di ogni trade\n")
    lines.append("Tra parentesi: trade, e strategie in perdita in quel regime su quelle che ci hanno "
                 "operato. `-` = regime in cui il piano perde.\n")
    for axis in (NAMES[:3], NAMES[3:]):
        lines.append("| piano | " + " | ".join(axis) + " | senza etichetta |")
        lines.append("|---|" + "---:|" * (len(axis) + 1))
        for plan, items in groups.items():
            cells = []
            for n in axis:
                ins = [(p, c) for l, p, c in items if l and n in l]
                per = defaultdict(float)
                for p, c in ins:
                    per[c] += p
                neg = sum(1 for v in per.values() if v < 0)
                net = sum(p for p, _ in ins)
                cells.append(f"{'-' if net < 0 else ''}{fmt(abs(net))} ({len(ins)}; {neg}/{len(per)} in perdita)")
            none = [p for l, p, _ in items if not l]
            lines.append(f"| {plan} | " + " | ".join(cells) + f" | {len(none)} |")
        lines.append("")

    # B) giorni per regime di un mercato comune (ES): P&L giornaliero del conto e dei piani
    es = labels.get("ES")
    if es:
        lines.append("### Giorni per regime dell'S&P (ES), P&L per giorno di uscita\n")
        daily = defaultdict(lambda: defaultdict(float))
        for t in trades:
            d = day(t["exitTimeUtc"])
            daily["CONTO"][d] += t["pnl"]
            daily[t["plan"]][d] += t["pnl"]
        # giorni di calendario del periodo in cui ES ha un'etichetta
        es_days = [d for d in es[1] if first <= d <= last]
        lines.append("| piano | regime ES | giorni | P&L medio/giorno | giorni in perdita | peggior giorno |")
        lines.append("|---|---|---:|---:|---:|---:|")
        present = [p for p in PLANS if p in daily]
        for plan in (["CONTO"] if len(present) > 1 else []) + present:
            for n in NAMES:
                ds = [d for d in es_days if n in es[0][d]]
                if not ds:
                    continue
                vals = [daily[plan].get(d, 0.0) for d in ds]
                lines.append(f"| {plan} | {n} | {len(ds)} | {fmt(statistics.fmean(vals))} | "
                             f"{sum(1 for v in vals if v < 0) / len(ds):.0%} | {fmt(min(vals))} |")
        lines.append("")

    # C) mesi peggiori del conto e regime prevalente di ES in quei mesi
    monthly = defaultdict(float)
    for t in trades:
        d = day(t["exitTimeUtc"])
        monthly[(d.year, d.month)] += t["pnl"]
    worst = sorted(monthly.items(), key=lambda kv: kv[1])[:6]
    if es:
        lines.append("### I sei mesi peggiori del conto\n")
        lines.append("| mese | netto | regime ES prevalente (direzione / volatilita') |")
        lines.append("|---|---:|---|")
        for (y, m), v in worst:
            ds = [es[0][d] for d in es[1] if d.year == y and d.month == m]
            dirs = defaultdict(int)
            vols = defaultdict(int)
            for a, b in ds:
                dirs[a] += 1
                vols[b] += 1
            dd = max(dirs, key=dirs.get) if dirs else "?"
            vv = max(vols, key=vols.get) if vols else "?"
            lines.append(f"| {y}-{m:02d} | {fmt(v)} | {dd} / {vv} |")
        lines.append("")


def main():
    lines = ["# Piani in produzione per regime di mercato", "",
             "Generato da `piani.py`. Metodo delle etichette in `regimi.py` (etichette note all'ingresso)."]
    ftmo = load_trades(FTMO_RUNS)
    syms = {t["symbol"] for t in ftmo} | {"ES"}
    lab = labels_for(syms, [os.path.join(REPO, "datafeed-external", "FTMO"), os.path.join(REPO, "datafeed")])
    report("Vista FTMO 2022-2026: i tre piani sul conto", ftmo, lab, lines)

    hist = load_trades([(HISTORY_RUN, "FTMO-O4-X05")])
    syms = {t["symbol"] for t in hist} | {"ES"}
    lab = labels_for(syms, [os.path.join(REPO, "datafeed"), os.path.join(REPO, "datafeed-external", "FTMO")])
    report("Vista storica 2012-2025: le strategie di FTMO-O4-X05", hist, lab, lines)

    with open(os.path.join(os.path.dirname(__file__), "piani-per-regime.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print("\n".join(lines))


if __name__ == "__main__":
    main()
