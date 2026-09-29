"""Sintesi del profilo per regime e prova di persistenza fra due meta' della storia.

Uso: python analisi.py <trades.json> <anno di taglio>

La domanda che decide se il regime serve a spegnere una strategia: il regime che la danneggia
nella prima meta' la danneggia anche nella seconda? Per ogni strategia e regime si confronta
l'average trade del regime meno quello del resto (delta) prima e dopo il taglio.
"""
import json
import statistics
import sys
from collections import defaultdict
from datetime import datetime

from regimi import load_daily, classify, label_for, zscore

NAMES = ("trend-su", "trend-giu", "laterale", "calma", "normale", "agitata")


def main():
    trades_path, cut = sys.argv[1], int(sys.argv[2])
    with open(trades_path, encoding="utf-8-sig") as f:
        trades = json.load(f)
    regimes = {}
    for s in {t["symbol"] for t in trades}:
        order, bars, _ = load_daily(s)
        if order:
            labels = classify(order, bars)
            regimes[s] = (labels, sorted(labels))

    data = defaultdict(list)  # strategia -> (anno, etichette, pnl)
    for t in trades:
        if t["symbol"] not in regimes:
            continue
        d = datetime.fromisoformat(t["entryTimeUtc"].replace("Z", "+00:00")).date()
        lab = label_for(*regimes[t["symbol"]], d)
        if lab:
            data[t["strategyCode"]].append((d.year, lab, float(t["netProfit"]), t["direction"]))

    # 1) intero periodo: quante dipendenze "significative", contro le ~5% attese per caso
    tests = sig = 0
    worst = []
    for code, items in data.items():
        for name in NAMES:
            ins = [p for _, l, p, _ in items if name in l]
            out = [p for _, l, p, _ in items if name not in l]
            z = zscore(ins, out)
            if z is None:
                continue
            tests += 1
            if abs(z) > 2:
                sig += 1
            if len(ins) >= 30 and statistics.fmean(ins) < 0 < statistics.fmean([p for *_, p, _ in items]):
                worst.append((code, name, len(ins), round(sum(ins)), round(statistics.fmean(ins)), round(z, 1)))
    print(f"Intero periodo: {sig} confronti con |z|>2 su {tests} ({sig / tests:.0%}; il caso ne da' ~5%)")
    print(f"Strategie positive con un regime ad average trade negativo (>=30 trade): {len(worst)}")
    for w in sorted(worst, key=lambda x: x[5])[:25]:
        print("  ", w)

    # 2) persistenza: delta del regime prima e dopo il taglio
    pairs = []
    flips = keeps = 0
    for code, items in data.items():
        a = [x for x in items if x[0] < cut]
        b = [x for x in items if x[0] >= cut]
        for name in NAMES:
            def delta(xs):
                ins = [p for _, l, p, _ in xs if name in l]
                out = [p for _, l, p, _ in xs if name not in l]
                if len(ins) < 30 or len(out) < 30:
                    return None, None
                return statistics.fmean(ins) - statistics.fmean(out), zscore(ins, out)
            da, za = delta(a)
            db, zb = delta(b)
            if da is None or db is None:
                continue
            pairs.append((da, db))
            if za is not None and za < -2:  # nella prima meta' il regime la danneggia davvero
                if db < 0:
                    keeps += 1
                else:
                    flips += 1
    if pairs:
        xs, ys = [p[0] for p in pairs], [p[1] for p in pairs]
        rho = statistics.correlation(xs, ys)
        same = sum(1 for x, y in pairs if (x > 0) == (y > 0)) / len(pairs)
        print(f"\nPersistenza (taglio {cut}): {len(pairs)} coppie strategia-regime, "
              f"correlazione dei delta {rho:.2f}, stesso segno {same:.0%}")
        print(f"Regimi dannosi con z<-2 prima del taglio: {keeps + flips}; ancora dannosi dopo: {keeps}")

    # 3) quanto si sarebbe guadagnato spegnendo dopo il taglio i regimi dannosi (z<-2) di prima
    gain = 0.0
    off = 0
    for code, items in data.items():
        a = [x for x in items if x[0] < cut]
        bad = []
        for name in NAMES:
            ins = [p for _, l, p, _ in a if name in l]
            out = [p for _, l, p, _ in a if name not in l]
            z = zscore(ins, out)
            if z is not None and z < -2 and statistics.fmean(ins) < 0:
                bad.append(name)
        if not bad:
            continue
        removed = [p for y, l, p, _ in items if y >= cut and any(n in l for n in bad)]
        off += len(removed)
        gain -= sum(removed)
    print(f"Spegnendo dopo il {cut} i regimi dannosi (z<-2, avg<0) visti prima: {off} trade tolti, "
          f"effetto sul netto {gain:+,.0f}")


if __name__ == "__main__":
    main()
