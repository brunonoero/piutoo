"""Profilo per regime di mercato di ogni strategia di un run.

Uso: python regimi.py <trades.json> <cartella di uscita>

Ogni giorno di ogni simbolo riceve due etichette, calcolate SOLO con le barre giornaliere
chiuse fino al giorno prima (quello che si sarebbe saputo all'ingresso):

- direzione: efficiency ratio a 50 giorni. Terzile alto del simbolo = trend (su o giu' secondo
  il segno della variazione), il resto = laterale;
- volatilita': ATR(20)/prezzo, percentile sugli ultimi 250 giorni: < 33 calma, > 67 agitata.

Ogni trade prende le etichette del giorno di ingresso (UTC). Per strategia e regime: trade,
netto, average trade, e uno z del confronto fra l'average trade del regime e quello del resto.
Solo stdlib.
"""
import csv
import json
import math
import os
import statistics
import sys
from collections import defaultdict
from datetime import datetime, timezone, timedelta

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCES = [os.path.join(REPO, "datafeed"), os.path.join(REPO, "datafeed-external", "FTMO")]
TIMEFRAMES = [1440, 240, 60, 30, 15]
ER_N, ATR_N, VOL_WINDOW = 50, 20, 250
MIN_TRADES_Z = 30


def load_daily(symbol):
    """Barre giornaliere per data UTC, dalla prima fonte che ha il simbolo (interno prima)."""
    for folder in SOURCES:
        for tf in TIMEFRAMES:
            path = os.path.join(folder, f"@{symbol}_{tf}.json")
            if not os.path.exists(path):
                continue
            with open(path, encoding="utf-8-sig") as f:
                candles = json.load(f)["candles"]
            days = {}
            for c in candles:
                d = datetime.fromtimestamp(c["timestamp"], tz=timezone.utc).date()
                b = days.get(d)
                if b is None:
                    days[d] = [c["open"], c["high"], c["low"], c["close"]]
                else:
                    b[1] = max(b[1], c["high"]); b[2] = min(b[2], c["low"]); b[3] = c["close"]
            order = sorted(days)
            return order, [days[d] for d in order], f"{os.path.relpath(path, REPO)}"
    return None, None, None


def classify(order, bars):
    """Etichette del giorno d costruite con le barre fino a d-1 compreso."""
    closes = [b[3] for b in bars]
    tr = [bars[0][1] - bars[0][2]] + [
        max(bars[i][1], bars[i - 1][3]) - min(bars[i][2], bars[i - 1][3]) for i in range(1, len(bars))]
    er, sign, atrp = [None] * len(bars), [0] * len(bars), [None] * len(bars)
    for i in range(len(bars)):
        if i >= ER_N:
            path = sum(abs(closes[k] - closes[k - 1]) for k in range(i - ER_N + 1, i + 1))
            move = closes[i] - closes[i - ER_N]
            er[i] = abs(move) / path if path > 0 else 0.0
            sign[i] = 1 if move > 0 else -1
        if i >= ATR_N:
            atrp[i] = (sum(tr[i - ATR_N + 1:i + 1]) / ATR_N) / closes[i]
    known = sorted(x for x in er if x is not None)
    hi = known[int(len(known) * 2 / 3)] if known else 1.0
    labels = {}
    for i in range(1, len(bars)):
        j = i - 1  # ultimo giorno chiuso prima di order[i]
        if er[j] is None or atrp[j] is None:
            continue
        direction = ("trend-su" if sign[j] > 0 else "trend-giu") if er[j] >= hi else "laterale"
        window = [x for x in atrp[max(0, j - VOL_WINDOW + 1):j + 1] if x is not None]
        if len(window) < 60:
            continue
        rank = sum(1 for x in window if x < atrp[j]) / len(window)
        vol = "calma" if rank < 1 / 3 else ("agitata" if rank > 2 / 3 else "normale")
        labels[order[i]] = (direction, vol)
    # un giorno senza barra (festivo, fine settimana) eredita l'etichetta dell'ultimo giorno noto
    return labels


def label_for(labels, sorted_days, day):
    if day in labels:
        return labels[day]
    lo, hi = 0, len(sorted_days)
    while lo < hi:
        mid = (lo + hi) // 2
        if sorted_days[mid] <= day:
            lo = mid + 1
        else:
            hi = mid
    if lo == 0:
        return None
    prev = sorted_days[lo - 1]
    return labels[prev] if day - prev <= timedelta(days=4) else None


def zscore(inside, outside):
    if len(inside) < MIN_TRADES_Z or len(outside) < MIN_TRADES_Z:
        return None
    se = math.sqrt(statistics.pvariance(inside) / len(inside) + statistics.pvariance(outside) / len(outside))
    return (statistics.fmean(inside) - statistics.fmean(outside)) / se if se > 0 else None


def main():
    trades_path, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    with open(trades_path, encoding="utf-8-sig") as f:
        trades = json.load(f)

    symbols = sorted({t["symbol"] for t in trades})
    regimes, sources = {}, {}
    for s in symbols:
        order, bars, src = load_daily(s)
        if order is None:
            sources[s] = "nessun datafeed"
            continue
        labels = classify(order, bars)
        regimes[s] = (labels, sorted(labels))
        sources[s] = f"{src} ({order[0]} - {order[-1]})"

    by_strategy = defaultdict(list)
    uncovered = defaultdict(int)
    for t in trades:
        s = t["symbol"]
        day = datetime.fromisoformat(t["entryTimeUtc"].replace("Z", "+00:00")).date()
        lab = label_for(*regimes[s], day) if s in regimes else None
        if lab is None:
            uncovered[t["strategyCode"]] += 1
            continue
        by_strategy[(t["strategyCode"], s)].append((lab[0], lab[1], t["direction"], float(t["netProfit"])))

    rows = []
    for (code, sym), items in sorted(by_strategy.items()):
        pnl = [x[3] for x in items]
        longs = sum(1 for x in items if x[2] == "Buy") / len(items)
        base = dict(strategy=code, symbol=sym, trades=len(items), net=round(sum(pnl)),
                    avg=round(statistics.fmean(pnl), 1), long_share=round(longs, 2),
                    uncovered=uncovered.get(code, 0))
        for axis, idx, names in (("direzione", 0, ("trend-su", "trend-giu", "laterale")),
                                 ("volatilita", 1, ("calma", "normale", "agitata"))):
            for name in names:
                inside = [x[3] for x in items if x[idx] == name]
                outside = [x[3] for x in items if x[idx] != name]
                z = zscore(inside, outside)
                rows.append(dict(base, axis=axis, regime=name, r_trades=len(inside),
                                 r_net=round(sum(inside)),
                                 r_avg=round(statistics.fmean(inside), 1) if inside else "",
                                 z=round(z, 2) if z is not None else ""))

    with open(os.path.join(out, "profilo-regimi.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()), delimiter=";")
        w.writeheader(); w.writerows(rows)

    # quota di giorni per regime e simbolo, per leggere i numeri sapendo quanto e' raro ogni regime
    shares = {}
    for s, (labels, _) in regimes.items():
        n = len(labels)
        shares[s] = {k: sum(1 for v in labels.values() if k in v) / n
                     for k in ("trend-su", "trend-giu", "laterale", "calma", "normale", "agitata")}
    with open(os.path.join(out, "fonti-e-quote.json"), "w", encoding="utf-8") as f:
        json.dump({"fonti": sources, "quote_giorni": shares,
                   "trade_senza_etichetta": dict(uncovered)}, f, indent=1, default=str)
    print(f"{len(by_strategy)} strategie, {len(rows)} righe; fonti: {json.dumps(sources, default=str, indent=1)}")
    print("trade senza etichetta:", sum(uncovered.values()))


if __name__ == "__main__":
    main()
