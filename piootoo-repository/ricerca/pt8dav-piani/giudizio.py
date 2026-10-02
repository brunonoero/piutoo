"""Giudizio delle PT8DAV sul run neutro ai costi veri (Pt8DavNeutralRunStudy).

uso: python giudizio.py [run-ftmo]

Legge i trades.json per simbolo del run, divide ogni strategia in due tratti e scrive:
  giudizio-<run>.csv      una riga per strategia, con i numeri dei due tratti
  candidate-<run>/trades.json   i trade delle sole candidate, per piootoo-plan-builder

I due tratti:
  A  01/06/2022 -> 31/05/2025  il tratto su cui la ricerca ha filtrato (con il suo simulatore e i suoi
     costi): qui si giudica e si compone, sapendo che la selezione l'ha gia' visto.
  B  01/06/2025 -> fine archivio  il tratto che nessuna scelta ha visto: qui NON si sceglie niente, si
     guarda soltanto se i piani composti su A tengono.

Candidata = nel tratto A, sul nostro motore e ai costi del broker: almeno 30 trade, in utile,
netto/drawdown >= 1. Il tratto B non entra nel criterio, di proposito.
"""
import csv, json, glob, os, sys, datetime as dt, collections

HERE = os.path.dirname(os.path.abspath(__file__))
RUN = sys.argv[1] if len(sys.argv) > 1 else "run-ftmo"
SPLIT = "2025-06-01"
MIN_TRADES, MIN_NET_DD = 30, 1.0

research = {r["codice"]: r for r in csv.DictReader(
    open(os.path.join(HERE, "..", "..", "run-engine-v3", "strategie_111.csv"), encoding="utf-8"))}

trades = []
for path in sorted(glob.glob(os.path.join(HERE, RUN, "*", "trades.json"))):
    trades += json.load(open(path, encoding="utf-8-sig"))
by = collections.defaultdict(list)
for t in trades:
    by[t["strategyCode"]].append(t)


def stats(ts):
    ts = sorted(ts, key=lambda t: t["exitTimeUtc"])
    net = sum(t["netProfit"] for t in ts)
    peak = eq = dd = 0.0
    for t in ts:
        eq += t["netProfit"]
        peak = max(peak, eq)
        dd = max(dd, peak - eq)
    return len(ts), net, dd, (net / len(ts) if ts else 0.0), (net / dd if dd > 0 else (9.99 if net > 0 else 0.0))


# codice della ricerca per classe, dal commento ResearchCode delle classi
codes = {}
for f in glob.glob(os.path.join(HERE, "..", "..", "..", "Piootoo.Strategies", "PT8DAVStrategies", "*.cs")):
    text = open(f, encoding="utf-8").read()
    codes[os.path.basename(f)[:-3]] = text.split('ResearchCode => "')[1].split('"')[0]

rows, candidates = [], []
for name in sorted(codes):
    ts = by.get(name, [])
    a = stats([t for t in ts if t["exitTimeUtc"] < SPLIT])
    b = stats([t for t in ts if t["exitTimeUtc"] >= SPLIT])
    r = research[codes[name]]
    ok = a[0] >= MIN_TRADES and a[1] > 0 and a[4] >= MIN_NET_DD
    if ok:
        candidates.append(name)
    rows.append([name, codes[name], r["motore"], r["tenuta"], "si" if ok else "no",
                 a[0], round(a[1]), round(a[2]), round(a[3]), round(a[4], 2),
                 b[0], round(b[1]), round(b[2]), round(b[3]), round(b[4], 2),
                 round(float(r["f_netto"])), round(float(r["b_netto"]))])

out = os.path.join(HERE, f"giudizio-{RUN}.csv")
with open(out, "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f)
    w.writerow(["classe", "codice", "motore", "tenuta", "candidata",
                "A_trade", "A_netto", "A_dd", "A_avg", "A_net_dd",
                "B_trade", "B_netto", "B_dd", "B_avg", "B_net_dd",
                "ricerca_f_netto_2022_2025", "ricerca_b_netto_broker"])
    w.writerows(rows)

folder = os.path.join(HERE, f"candidate-{RUN}")
os.makedirs(folder, exist_ok=True)
keep = set(candidates)
json.dump([t for t in trades if t["strategyCode"] in keep],
          open(os.path.join(folder, "trades.json"), "w", encoding="utf-8"), indent=1)

n = len(rows)
posA = sum(1 for r in rows if r[6] > 0)
posB = sum(1 for r in rows if r[11] > 0)
print(f"{n} strategie, {len(trades)} trade")
print(f"tratto A in utile: {posA}/{n}   netto {sum(r[6] for r in rows):,.0f}")
print(f"tratto B in utile: {posB}/{n}   netto {sum(r[11] for r in rows):,.0f}")
print(f"candidate (A: >= {MIN_TRADES} trade, in utile, netto/DD >= {MIN_NET_DD}): {len(candidates)}")
cb = [r for r in rows if r[4] == "si"]
print(f"  delle candidate, in utile nel tratto B: {sum(1 for r in cb if r[11] > 0)}/{len(cb)}   netto B {sum(r[11] for r in cb):,.0f}")
nb = [r for r in rows if r[4] == "no"]
print(f"  delle scartate, in utile nel tratto B: {sum(1 for r in nb if r[11] > 0)}/{len(nb)}   netto B {sum(r[11] for r in nb):,.0f}")
by_sym = collections.Counter(r[0].split("_")[1] for r in cb)
print("  candidate per simbolo:", dict(by_sym))
print("scritto", out, "e", folder)
