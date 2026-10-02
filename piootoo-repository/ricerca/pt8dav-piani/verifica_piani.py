"""Verifica dei piani composti sul tratto A: come vanno sul tratto B, che nessuna scelta ha visto.

uso: python verifica_piani.py [run-ftmo] [piani-ftmo-A]

Legge i piani da <piani>/plans.json e i trade da <run>/*/trades.json (tutte le strategie, non le sole
candidate: cosi' un piano si puo' verificare anche ai costi di un altro broker). Stampa, per piano:
i due tratti, i trimestri, il drawdown peggiore del tratto B con chi lo fa, la quota dell'oro, e la
correlazione giornaliera fra i piani nel tratto B.
"""
import json, glob, os, sys, math, collections, datetime as dt

HERE = os.path.dirname(os.path.abspath(__file__))
RUN = sys.argv[1] if len(sys.argv) > 1 else "run-ftmo"
PLANS = sys.argv[2] if len(sys.argv) > 2 else "piani-ftmo-A"
SPLIT, END = "2025-06-01", "2026-09-25"

trades = []
for path in sorted(glob.glob(os.path.join(HERE, RUN, "*", "trades.json"))):
    trades += json.load(open(path, encoding="utf-8-sig"))

raw = json.load(open(os.path.join(HERE, PLANS, "plans.json"), encoding="utf-8-sig"))
plans = raw if isinstance(raw, list) else (raw.get("plans") or raw.get("Plans"))


def members(plan):
    items = plan.get("strategies") or plan.get("Strategies") or plan.get("members") or plan.get("Members")
    return [x if isinstance(x, str) else (x.get("strategy") or x.get("Strategy") or x.get("code") or x.get("Code")) for x in items]


def daily(ts):
    d = collections.defaultdict(float)
    for t in ts:
        d[t["exitTimeUtc"][:10]] += t["netProfit"]
    return d


def drawdown(d):
    eq = peak = dd = 0.0
    start = peak_day = end = None
    for day in sorted(d):
        eq += d[day]
        if eq > peak:
            peak, peak_day = eq, day
        if peak - eq > dd:
            dd, start, end = peak - eq, peak_day, day
    return dd, start, end


def years(a, b):
    return (dt.date.fromisoformat(b) - dt.date.fromisoformat(a)).days / 365.25


series = {}
for number, plan in enumerate(plans, 1):
    names = members(plan)
    mine = [t for t in trades if t["strategyCode"] in names]
    print(f"\n=== Piano {number}: {len(names)} strategie ({RUN})")
    for label, a, b in (("A 06/2022-05/2025", "2022-06-01", SPLIT), ("B 06/2025-09/2026", SPLIT, END)):
        ts = [t for t in mine if a <= t["exitTimeUtc"] < b + "T99"]
        d = daily(ts)
        dd, _, _ = drawdown(d)
        net = sum(d.values())
        worst = min(d.values()) if d else 0.0
        print(f"  {label}: trade {len(ts):5}  netto {net:10,.0f}  ({net / years(a, b):9,.0f}/anno)  "
              f"DD giornaliero {dd:8,.0f}  giorno peggiore {worst:9,.0f}  netto/DD {net / dd if dd else 0:5.2f}")
        if label[0] == "B":
            series[number] = d

    quarters = collections.defaultdict(float)
    for t in mine:
        month = int(t["exitTimeUtc"][5:7])
        quarters[t["exitTimeUtc"][:4] + "Q" + str((month + 2) // 3)] += t["netProfit"]
    print("  per trimestre (migliaia): " + " ".join(f"{k}:{v / 1000:.0f}" for k, v in sorted(quarters.items())))

    b_trades = [t for t in mine if t["exitTimeUtc"] >= SPLIT]
    dd, start, end = drawdown(daily(b_trades))
    if start:
        print(f"  drawdown peggiore del tratto B: {dd:,.0f} dal {start} al {end}")
    total_b = sum(t["netProfit"] for t in b_trades)
    gold = sum(t["netProfit"] for t in b_trades if t["symbol"] == "GC")
    if total_b:
        print(f"  oro nel tratto B: {gold:,.0f} su {total_b:,.0f} ({100 * gold / total_b:.0f}%); senza oro {total_b - gold:,.0f}")
    for name in names:
        a_net = sum(t["netProfit"] for t in mine if t["strategyCode"] == name and t["exitTimeUtc"] < SPLIT)
        b_net = sum(t["netProfit"] for t in mine if t["strategyCode"] == name and t["exitTimeUtc"] >= SPLIT)
        in_dd = sum(t["netProfit"] for t in b_trades if t["strategyCode"] == name and start and start < t["exitTimeUtc"][:10] <= end)
        print(f"     {name:26} A {a_net:9,.0f}   B {b_net:9,.0f}   nel drawdown {in_dd:9,.0f}")

days = sorted(set().union(*[set(s) for s in series.values()]))


def corr(x, y):
    a = [x.get(d, 0.0) for d in days]
    b = [y.get(d, 0.0) for d in days]
    ma, mb = sum(a) / len(a), sum(b) / len(b)
    cov = sum((p - ma) * (q - mb) for p, q in zip(a, b))
    return cov / math.sqrt(sum((p - ma) ** 2 for p in a) * sum((q - mb) ** 2 for q in b))


print("\ncorrelazione giornaliera fra i piani nel tratto B: " +
      ", ".join(f"{i}-{j}: {corr(series[i], series[j]):.2f}" for i in series for j in series if i < j))
