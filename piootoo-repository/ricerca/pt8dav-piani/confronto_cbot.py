"""Confronto fra un run cBot (cTrader) e il backtest interno dello stesso piano.

uso: python confronto_cbot.py <cartella run cBot> <cartella backtest interno> <fattore conto>

<fattore conto> porta il netto interno (1 contratto per strategia) sul conto: 0,1 x size del piano su
FTMO e Fintokei (0.05 per un piano a size 0,5, 0.1 a size 1).

Stampa: i numeri del run cBot (netto, drawdown chiuso, giorno peggiore, costi), poi strategia per
strategia i trade dei due run sullo stesso periodo, quanti si abbinano (stesso lato, ingresso entro
la tolleranza) e il netto dei due. I trade interni si prendono dal primo giorno del run cBot.
"""
import json, sys, os, collections, datetime as dt

cbot_dir, internal_dir, factor = sys.argv[1], sys.argv[2], float(sys.argv[3])
TOLERANCE_MINUTES = 180


def load(folder):
    return json.load(open(os.path.join(folder, "trades.json"), encoding="utf-8-sig"))


def when(text):
    return dt.datetime.fromisoformat(text.replace("Z", "+00:00"))


cbot, internal = load(cbot_dir), load(internal_dir)
if not cbot:
    print("run cBot senza trade")
    sys.exit(0)

first = min(t["entryTimeUtc"] for t in cbot)
last = max(t["exitTimeUtc"] for t in cbot)
internal = [t for t in internal if first[:10] <= t["entryTimeUtc"][:10] <= last[:10]]

daily = collections.defaultdict(float)
for t in cbot:
    daily[t["exitTimeUtc"][:10]] += t["netProfit"]
equity = peak = dd = 0.0
for day in sorted(daily):
    equity += daily[day]
    peak = max(peak, equity)
    dd = max(dd, peak - equity)
net = sum(t["netProfit"] for t in cbot)
print(f"run cBot: {os.path.basename(cbot_dir)}")
print(f"  periodo {first[:10]} -> {last[:10]}   trade {len(cbot)}   netto {net:,.0f}   DD chiuso {dd:,.0f}   "
      f"giorno peggiore {min(daily.values()):,.0f}   vincenti {sum(1 for t in cbot if t['netProfit'] > 0)}")
print(f"  commissioni {sum(t.get('commission') or 0 for t in cbot):,.0f}   swap {sum(t.get('swap') or 0 for t in cbot):,.0f}")
months = collections.defaultdict(float)
for t in cbot:
    months[t["exitTimeUtc"][:7]] += t["netProfit"]
print("  per mese: " + "  ".join(f"{m[2:]}:{v:,.0f}" for m, v in sorted(months.items())))

inet = sum(t["netProfit"] for t in internal) * factor
print(f"interno sullo stesso periodo: trade {len(internal)}   netto sul conto {inet:,.0f}  (x {factor})")

print(f"\n{'strategia':26} {'cBot':>5} {'int':>5} {'abbin':>6} | {'netto cBot':>11} {'netto int':>10} | {'abbinati cBot':>13} {'abbinati int':>12}")
names = sorted({t["strategyCode"] for t in cbot} | {t["strategyCode"] for t in internal})
tot = [0, 0, 0, 0.0, 0.0, 0.0, 0.0]
for name in names:
    c = sorted((t for t in cbot if t["strategyCode"] == name), key=lambda t: t["entryTimeUtc"])
    i = sorted((t for t in internal if t["strategyCode"] == name), key=lambda t: t["entryTimeUtc"])
    used, matched_c, matched_i = set(), 0.0, 0.0
    pairs = 0
    for t in c:
        best = None
        for k, u in enumerate(i):
            if k in used or u["direction"] != t["direction"]:
                continue
            gap = abs((when(u["entryTimeUtc"]) - when(t["entryTimeUtc"])).total_seconds()) / 60
            if gap <= TOLERANCE_MINUTES and (best is None or gap < best[0]):
                best = (gap, k)
        if best:
            used.add(best[1])
            pairs += 1
            matched_c += t["netProfit"]
            matched_i += i[best[1]]["netProfit"] * factor
    nc, ni = sum(t["netProfit"] for t in c), sum(t["netProfit"] for t in i) * factor
    print(f"{name:26} {len(c):5} {len(i):5} {pairs:6} | {nc:11,.0f} {ni:10,.0f} | {matched_c:13,.0f} {matched_i:12,.0f}")
    for k, v in enumerate((len(c), len(i), pairs, nc, ni, matched_c, matched_i)):
        tot[k] += v
print(f"{'TOTALE':26} {tot[0]:5} {tot[1]:5} {tot[2]:6} | {tot[3]:11,.0f} {tot[4]:10,.0f} | {tot[5]:13,.0f} {tot[6]:12,.0f}")
print(f"abbinati: {100 * tot[2] / max(1, tot[0]):.0f}% dei trade cBot, {100 * tot[2] / max(1, tot[1]):.0f}% degli interni")
