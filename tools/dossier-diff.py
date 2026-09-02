"""Diff fra le schede del dossier del paniere e le classi PTS_* presenti nel catalogo.

Non usa gli S-ID: la numerazione cambia da un'edizione del dossier all'altra e le classi
tradotte prima citano quella vecchia. L'impronta e' invece l'insieme dei numeri che
identificano un run: simbolo, timeframe, motore, stop, target, trailing, uscita a tempo.
"""
import re
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOSSIER = ROOT / "piootoo-repository/run-engine/run-08-settembre/DOSSIER_PANIERE (1).md"
CLASSES = ROOT / "Piootoo.Strategies/PiutooStrategies"

TF_MIN = {"15m": 15, "30m": 30, "1h": 60, "4h": 240, "day": 1440}
ENGINE_BY_BASE = {
    "TfMirroredEngine": "TF_M",
    "TfUnmirroredEngine": "TF_U",
    "PriceChannelEngine": "PC",
    "SessionBreakoutEngine": "BO",
    "BiasBarCountEngine": "BIAS",
    "BiasWeeklyEngine": "BIASW",
    "VolatilityBreakoutEngine": "VBO",
    "RbbMirroredEngine": "RBB_M",
    "RhlEngine": "RHL",
    "MovingAverageCrossoverEngine": "MAC",
}


def money(text):
    return int(text.replace(",", "").replace(".", ""))


def parse_dossier():
    raw = DOSSIER.read_text(encoding="utf-8")
    blocks = re.split(r"^### ", raw, flags=re.M)[1:]
    out = []
    for block in blocks:
        head = re.match(r"(S\d+) · ([A-Z]+) (\S+) · (.+?)  <a", block)
        if not head:
            continue
        sid, sym, tf, title = head.groups()
        engine = re.search(r"\| Motore \| (\S+) \|", block)
        stop = re.search(r"Stop loss: \*\*\$([\d,]+)\*\*", block)
        profit = re.search(r"Take profit: \*\*\$([\d,]+)\*\*", block)
        trail = re.search(r"Trailing stop: \*\*\$([\d,]+)\*\*", block)
        maxbars = re.search(r"Uscita a tempo dopo \*\*(\d+) barre\*\*", block)
        out.append({
            "sid": sid,
            "sym": sym,
            "tf": TF_MIN[tf],
            "engine": engine.group(1) if engine else "?",
            "stop": money(stop.group(1)) if stop else 0,
            "profit": money(profit.group(1)) if profit else 0,
            "trail": money(trail.group(1)) if trail else 0,
            "maxbars": int(maxbars.group(1)) if maxbars else 0,
            "title": title,
        })
    return out


def parse_classes():
    out = []
    for path in sorted(CLASSES.glob("PTS_*.cs")):
        text = path.read_text(encoding="utf-8")
        base = re.search(r"class (PTS_\w+)\s*:\s*(\w+)", text)
        sym = (re.search(r'Symbol => "@?(\w+)"', text)
               or re.search(r'_symbol = "@?(\w+)"', text))
        tf = (re.search(r"TimeframeMinutes => (\d+)", text)
              or re.search(r"_timeframeMinutes = (\d+)", text))
        if not (base and sym and tf):
            print(f"  !! non interpretabile: {path.name}", file=sys.stderr)
            continue

        def num(name):
            m = re.search(rf"\b{name}\s*=\s*(-?[\d_]+)", text)
            return int(m.group(1).replace("_", "")) if m else 0

        out.append({
            "name": base.group(1),
            "sym": sym.group(1).upper(),
            "tf": int(tf.group(1)),
            "engine": ENGINE_BY_BASE.get(base.group(2), base.group(2)),
            "stop": num("StopMoney") or num("StopMoneyLong"),
            "profit": num("ProfitMoney") or num("ProfitMoneyLong"),
            "trail": num("TrailingStopMoney") or num("TrailingMoneyLong"),
            "maxbars": num("MaxBars"),
        })
    return out


def key(item):
    return (item["sym"], item["tf"], item["engine"], item["stop"], item["profit"])


def main():
    dossier = parse_dossier()
    classes = parse_classes()
    print(f"schede dossier: {len(dossier)}   classi PTS: {len(classes)}")

    have = defaultdict(list)
    for c in classes:
        have[key(c)].append(c["name"])

    missing, matched = [], []
    for d in dossier:
        k = key(d)
        if have[k]:
            name = have[k].pop(0)
            cls = next(c for c in classes if c["name"] == name)
            note = []
            if cls["trail"] != d["trail"]:
                note.append(f"trailing dossier={d['trail']} classe={cls['trail']}")
            if cls["maxbars"] != d["maxbars"]:
                note.append(f"max_bars dossier={d['maxbars']} classe={cls['maxbars']}")
            matched.append((d, name, note))
        else:
            missing.append(d)

    print(f"\nabbinate: {len(matched)}   mancanti: {len(missing)}")

    divergenti = [(d, n, note) for d, n, note in matched if note]
    if divergenti:
        print("\n--- ABBINATE MA CON NUMERI DIVERSI ---")
        for d, n, note in divergenti:
            print(f"  {d['sid']:>5}  {n:<24} {'; '.join(note)}")
    if "--abbinate" in sys.argv:
        print("\n--- ABBINATE ---")
        for d, n, _ in sorted(matched, key=lambda x: int(x[0]["sid"][1:])):
            print(f"| `{d['sid']}` | `{n}` | {d['sym']} {d['tf']}m {d['engine']} |")
    print("\n--- MANCANTI ---")
    for d in sorted(missing, key=lambda x: int(x["sid"][1:])):
        print(f"{d['sid']:>5}  {d['sym']:<5} {d['tf']:>5}m {d['engine']:<6} "
              f"stop={d['stop']:<6} tp={d['profit']:<6} tr={d['trail']:<6} mb={d['maxbars']:<3} {d['title']}")

    leftovers = [n for names in have.values() for n in names]
    if leftovers:
        print("\n--- CLASSI SENZA SCHEDA NEL DOSSIER ---")
        for n in sorted(leftovers):
            print(f"  {n}")


if __name__ == "__main__":
    main()
