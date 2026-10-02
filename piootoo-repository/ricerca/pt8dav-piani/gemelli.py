"""Trade gemelli fra un run cBot PT8DAV e i best plan in produzione.

uso: python gemelli.py <cartella run cBot> [minuti di tolleranza, default 5]

Gemello = stesso simbolo, stesso lato, ingresso entro la tolleranza. Si guardano i soli trade nel
periodo in cui i due run si sovrappongono. Sopra il 5% due piani non vanno su conti diversi della
stessa prop (registro-piani.md); sullo stesso conto e' concentrazione.
"""
import json, os, sys, bisect, datetime as dt, urllib.request

run = sys.argv[1]
tolerance = float(sys.argv[2]) if len(sys.argv) > 2 else 5.0
WS = r"C:\piootoo-dev\piootoo-repository\workspaces"


def when(text):
    return dt.datetime.fromisoformat(text.replace("Z", "+00:00"))


def load(folder):
    return json.load(open(os.path.join(folder, "trades.json"), encoding="utf-8-sig"))


def twins(a, b):
    index = {}
    for t in b:
        index.setdefault((t["symbol"], t["direction"]), []).append((when(t["entryTimeUtc"]), t["strategyCode"]))
    for key in index:
        index[key].sort()
    hits, by = 0, {}
    for t in a:
        entries = index.get((t["symbol"], t["direction"]))
        if not entries:
            continue
        e = when(t["entryTimeUtc"])
        k = bisect.bisect_left(entries, (e - dt.timedelta(minutes=tolerance), ""))
        if k < len(entries) and abs((entries[k][0] - e).total_seconds()) <= tolerance * 60:
            hits += 1
            pair = (t["strategyCode"].replace("PT8DAV_", ""), entries[k][1])
            by[pair] = by.get(pair, 0) + 1
    return hits, by


mine = load(run)
with urllib.request.urlopen("http://localhost:5000/api/BestPlans", timeout=30) as response:
    best = json.loads(response.read().decode("utf-8-sig"))
latest = {}
for plan in best:
    if plan["planCode"] not in latest or plan["backtestFolder"] > latest[plan["planCode"]]["backtestFolder"]:
        latest[plan["planCode"]] = plan

print(f"{os.path.basename(run)}: {len(mine)} trade, tolleranza {tolerance:g} minuti")
for code in sorted(latest):
    plan = latest[code]
    other = load(os.path.join(WS, plan["workspaceId"], "backtests", plan["backtestFolder"]))
    if not other:
        continue
    start = max(min(t["entryTimeUtc"] for t in mine), min(t["entryTimeUtc"] for t in other))
    end = min(max(t["entryTimeUtc"] for t in mine), max(t["entryTimeUtc"] for t in other))
    a = [t for t in mine if start <= t["entryTimeUtc"] <= end]
    b = [t for t in other if start <= t["entryTimeUtc"] <= end]
    if not a or not b:
        print(f"  {code:22} nessun periodo in comune")
        continue
    h1, by = twins(a, b)
    h2, _ = twins(b, a)
    share = max(h1 / len(a), h2 / len(b))
    note = "  <-- oltre il 5%" if share > 0.05 else ""
    print(f"  {code:22} {start[:10]} -> {end[:10]}: {h1}/{len(a)} dei nostri ({100 * h1 / len(a):.0f}%), "
          f"{h2}/{len(b)} dei suoi ({100 * h2 / len(b):.0f}%){note}")
    for (ours, theirs), n in sorted(by.items(), key=lambda kv: -kv[1])[:4]:
        print(f"       {ours} con {theirs}: {n}")
