"""Studio di persistenza: il rendimento recente di una strategia predice quello successivo?

Ingresso: un trades.json (run neutro, stesse size). Nessuna dipendenza oltre la libreria standard.
Uso: python persistenza.py <trades.json> [permutazioni] [taglio AAAA-MM-GG]

Tre misure per ogni cadenza (giorno, settimana, mese) e ogni finestra di osservazione L:
  A. serie storica: per strategia, Spearman fra la somma degli ultimi L periodi e il periodo dopo;
     media sulle strategie, con p-value da permutazione (ordine dei periodi rimescolato);
  B. sezione trasversale: a ogni periodo, Spearman fra le strategie (IC) sui valori normalizzati;
  C. rotazione: portafoglio equipesato di tutte contro le sole con finestra positiva, e contro
     una selezione casuale dello stesso numero; dentro (< SPLIT) e fuori campione.
"""
import json, sys, random, math, datetime as dt
from collections import defaultdict

SPLIT = "2019-01-01"
SEED = 20260929


def rank(v):
    idx = sorted(range(len(v)), key=v.__getitem__)
    r = [0.0] * len(v)
    i = 0
    while i < len(idx):
        j = i
        while j + 1 < len(idx) and v[idx[j + 1]] == v[idx[i]]:
            j += 1
        avg = (i + j) / 2.0
        for k in range(i, j + 1):
            r[idx[k]] = avg
        i = j + 1
    return r


def pearson(x, y):
    n = len(x)
    if n < 3:
        return None
    mx, my = sum(x) / n, sum(y) / n
    sxy = sum((a - mx) * (b - my) for a, b in zip(x, y))
    sxx = sum((a - mx) ** 2 for a in x)
    syy = sum((b - my) ** 2 for b in y)
    if sxx == 0 or syy == 0:
        return None
    return sxy / math.sqrt(sxx * syy)


def spearman(x, y):
    return pearson(rank(x), rank(y))


def period_key(ts, cadence):
    d = dt.datetime.fromisoformat(ts.replace("Z", "+00:00")).date()
    if cadence == "giorno":
        return d
    if cadence == "settimana":
        return d - dt.timedelta(days=d.weekday())
    return d.replace(day=1)


def next_period(p, cadence):
    if cadence == "giorno":
        p = p + dt.timedelta(days=1)
        while p.weekday() >= 5:  # i giorni senza sessione non sono periodi
            p = p + dt.timedelta(days=1)
        return p
    if cadence == "settimana":
        return p + dt.timedelta(days=7)
    return (p.replace(day=28) + dt.timedelta(days=4)).replace(day=1)


def build_series(trades, cadence):
    """strategia -> (periodi, P&L) sul proprio intervallo attivo, periodi vuoti a zero."""
    pnl = defaultdict(lambda: defaultdict(float))
    for t in trades:
        pnl[t["strategyCode"]][period_key(t["exitTimeUtc"], cadence)] += t["netProfit"]
    out = {}
    for s, m in pnl.items():
        p, last = min(m), max(m)
        if cadence == "giorno" and p.weekday() >= 5:
            p = next_period(p, cadence)
        periods, vals = [], []
        while p <= last:
            periods.append(p)
            vals.append(m.get(p, 0.0))
            p = next_period(p, cadence)
        # un'uscita di domenica sera UTC e' gia' la sessione europea del lunedi'
        if cadence == "giorno":
            for k, v in m.items():
                if k.weekday() >= 5:
                    monday = k + dt.timedelta(days=7 - k.weekday())
                    if monday in periods:
                        vals[periods.index(monday)] += v
        out[s] = (periods, vals)
    return out


def lookback_pairs(vals, L):
    x, y = [], []
    run = sum(vals[:L])
    for t in range(L, len(vals)):
        x.append(run)
        y.append(vals[t])
        run += vals[t] - vals[t - L]
    return x, y


def ts_persistence(series, L, perms, rng):
    def mean_corr(get_vals):
        cs = []
        for s, (_, v) in series.items():
            vv = get_vals(v)
            x, y = lookback_pairs(vv, L)
            c = spearman(x, y)
            if c is not None:
                cs.append((s, c))
        return cs

    obs = mean_corr(lambda v: v)
    m_obs = sum(c for _, c in obs) / len(obs)
    null = []
    for _ in range(perms):
        def shuffled(v):
            v = list(v)
            rng.shuffle(v)
            return v
        cs = mean_corr(shuffled)
        null.append(sum(c for _, c in cs) / len(cs))
    p = (1 + sum(1 for n in null if n >= m_obs)) / (perms + 1)
    positive = sum(1 for _, c in obs if c > 0)
    return m_obs, p, positive, len(obs), dict(obs)


def normalized(series):
    """P&L diviso per la deviazione standard della strategia: GC e BP diventano confrontabili."""
    out = {}
    for s, (per, v) in series.items():
        n = len(v)
        mu = sum(v) / n
        sd = math.sqrt(sum((a - mu) ** 2 for a in v) / max(1, n - 1)) or 1.0
        out[s] = {p: a / sd for p, a in zip(per, v)}
    return out


def cross_section(series, L, min_active=10):
    norm = normalized(series)
    look = {}
    for s, (per, _) in series.items():
        v = [norm[s][p] for p in per]
        look[s] = {}
        run = sum(v[:L])
        for t in range(L, len(v)):
            look[s][per[t]] = (run, v[t])
            run += v[t] - v[t - L]
    by_period = defaultdict(list)
    for s, m in look.items():
        for p, xy in m.items():
            by_period[p].append((s, xy[0], xy[1]))
    return by_period


def ic_stats(by_period, min_active=10):
    ics = []
    for p in sorted(by_period):
        rows = by_period[p]
        if len(rows) < min_active:
            continue
        c = spearman([r[1] for r in rows], [r[2] for r in rows])
        if c is not None:
            ics.append((p, c))
    if not ics:
        return None
    v = [c for _, c in ics]
    m = sum(v) / len(v)
    sd = math.sqrt(sum((a - m) ** 2 for a in v) / max(1, len(v) - 1))
    return m, (m / (sd / math.sqrt(len(v))) if sd else 0.0), len(v)


def equity_stats(rets):
    eq = peak = dd = 0.0
    for r in rets:
        eq += r
        peak = max(peak, eq)
        dd = max(dd, peak - eq)
    return eq, dd, (eq / dd if dd else float("inf"))


def rotation(by_period, rng, perms, lo=None, hi=None, min_active=10):
    base, filt = [], []
    rnd = [[] for _ in range(perms)]
    kept = total = 0
    for p in sorted(by_period):
        if lo and str(p) < lo or hi and str(p) >= hi:
            continue
        rows = by_period[p]
        if len(rows) < min_active:
            continue
        n = len(rows)
        base.append(sum(r[2] for r in rows) / n)
        on = [r for r in rows if r[1] > 0]
        k = len(on)
        kept += k
        total += n
        # stessa scala dell'equipesato: una strategia spenta vale zero, non ridistribuisce
        filt.append(sum(r[2] for r in on) / n)
        for i in range(perms):
            pick = rng.sample(rows, k) if k else []
            rnd[i].append(sum(r[2] for r in pick) / n)
    b = equity_stats(base)
    f = equity_stats(filt)
    rs = sorted(equity_stats(r)[2] for r in rnd)
    pct = sum(1 for r in rs if r < f[2]) / len(rs) if rs else float("nan")
    med = rs[len(rs) // 2] if rs else float("nan")
    return b, f, med, pct, (kept / total if total else 0)


def family(code):
    parts = code.split("_")
    return parts[2] if len(parts) > 2 else "?"


def main():
    global SPLIT
    src = sys.argv[1]
    perms = int(sys.argv[2]) if len(sys.argv) > 2 else 200
    if len(sys.argv) > 3:
        SPLIT = sys.argv[3]
    rng = random.Random(SEED)
    trades = json.load(open(src, encoding="utf-8"))
    codes = sorted({t["strategyCode"] for t in trades})
    first = min(t["exitTimeUtc"] for t in trades)[:10]
    last = max(t["exitTimeUtc"] for t in trades)[:10]
    out = []
    w = out.append
    w("# Persistenza del rendimento delle strategie\n")
    w(f"Sorgente: `{src}` — {len(trades)} trade, {len(codes)} strategie, {first} → {last} "
      f"(giorno di uscita, UTC). Permutazioni: {perms}, seme {SEED}. Taglio dentro/fuori campione: {SPLIT}.\n")

    grids = {"giorno": [1, 5, 20, 60], "settimana": [1, 4, 13, 26], "mese": [1, 3, 6, 12]}
    fam_rows = {}
    for cadence, Ls in grids.items():
        series = build_series(trades, cadence)
        w(f"\n## Cadenza: {cadence}\n")
        w("| L | A: Spearman medio serie storica | p (perm.) | strategie > 0 | B: IC medio | t IC | periodi IC |")
        w("|---:|---:|---:|---:|---:|---:|---:|")
        rot_lines = []
        for L in Ls:
            m, p, pos, n, per_strat = ts_persistence(series, L, perms, rng)
            bp = cross_section(series, L)
            ic = ic_stats(bp)
            ic_s = f"{ic[0]:+.3f} | {ic[1]:+.2f} | {ic[2]}" if ic else "– | – | 0"
            w(f"| {L} | {m:+.3f} | {p:.3f} | {pos}/{n} | {ic_s} |")
            for lo, hi, lab in ((None, None, "tutto"), (None, SPLIT, "dentro"), (SPLIT, None, "fuori")):
                b, f, med, pct, share = rotation(bp, rng, min(perms, 100), lo, hi)
                rot_lines.append(
                    f"| {L} | {lab} | {b[0]:.1f} / {b[1]:.1f} / {b[2]:.2f} | {f[0]:.1f} / {f[1]:.1f} / {f[2]:.2f} "
                    f"| {med:.2f} | {pct:.0%} | {share:.0%} |")
            if (cadence, L) in (("settimana", 4), ("mese", 3)):
                fam = defaultdict(list)
                for s, c in per_strat.items():
                    fam[family(s)].append(c)
                fam_rows[(cadence, L)] = fam
        w("\n**C. Rotazione** (P&L normalizzato per strategia; netto / drawdown / netto÷DD; il filtro tiene "
          "le strategie con finestra positiva, la casuale ne tiene lo stesso numero):\n")
        w("| L | campione | tutte | filtro | casuale (mediana netto÷DD) | percentile del filtro | quota accesa |")
        w("|---:|---|---|---|---:|---:|---:|")
        out.extend(rot_lines)

    w("\n## Per famiglia (A, Spearman medio per strategia)\n")
    w("| famiglia | strategie | settimana L=4 | mese L=3 |")
    w("|---|---:|---:|---:|")
    fams = sorted(set(fam_rows[("settimana", 4)]) | set(fam_rows[("mese", 3)]))
    for f in fams:
        a = fam_rows[("settimana", 4)].get(f, [])
        b = fam_rows[("mese", 3)].get(f, [])
        fa = f"{sum(a)/len(a):+.3f}" if a else "–"
        fb = f"{sum(b)/len(b):+.3f}" if b else "–"
        w(f"| {f} | {max(len(a), len(b))} | {fa} | {fb} |")

    text = "\n".join(out) + "\n"
    print(text)
    return text


if __name__ == "__main__":
    main()
