"""Walk-forward onesto: le config si scelgono SOLO sull'in-sample, si giudicano sull'out-of-sample."""
import json, datetime as dt, statistics, os
from multiprocessing import Pool
import titano as T
import grid as G

SPLIT = dt.datetime(2025, 11, 1, tzinfo=dt.timezone.utc)
R = json.load(open('grid_results.json'))


def segment_stats(rows, lo, hi, cap=100_000.0):
    sub = [r for r in rows if lo <= r['exit'] < hi]
    return T.curve_stats(sub, cap=cap)


def run_cfg(args):
    d, ds = args
    cfg = G.build(d)
    tr = G.DATASETS[ds]
    dec, _ = T.run(tr, cfg)
    rows, _ = T.apply(tr, dec, cfg)
    lo = min(t['exit'] for t in tr)
    hi = max(t['exit'] for t in tr) + dt.timedelta(days=1)
    IS = segment_stats(rows, lo, SPLIT)
    OOS = segment_stats(rows, SPLIT, hi)
    out = dict(d)
    out['allocBand'] = list(d['allocBand'])
    out['dataset'] = ds
    for tag, s in (('is', IS), ('oos', OOS)):
        out[tag + '_net'] = s['net']
        out[tag + '_dd'] = s['dailyMaxDDabs']
        out[tag + '_n'] = s['n']
        out[tag + '_monthsDown'] = s['monthsDown']
        out[tag + '_calmar'] = s['net'] / s['dailyMaxDDabs'] if s['dailyMaxDDabs'] > 0 else (999.0 if s['net'] > 0 else 0.0)
    return out


def keyify(r):
    d = {k: r[k] for k in G.GRID}
    d['allocBand'] = tuple(r['allocBand'])
    return d


if __name__ == '__main__':
    tr = T.load()
    lo = min(t['exit'] for t in tr)
    hi = max(t['exit'] for t in tr) + dt.timedelta(days=1)
    b = [dict(t, adj=t['net']) for t in tr]
    bIS = segment_stats(b, lo, SPLIT)
    bOOS = segment_stats(b, SPLIT, hi)
    print(f"BASELINE  IS  net {bIS['net']:>10,.0f}  maxDD {bIS['dailyMaxDDabs']:>9,.0f}  n {bIS['n']}")
    print(f"BASELINE  OOS net {bOOS['net']:>10,.0f}  maxDD {bOOS['dailyMaxDDabs']:>9,.0f}  n {bOOS['n']}")

    todo = [(keyify(r), r['dataset']) for r in R]
    print('\nvaluto', len(todo), 'config su IS/OOS...', flush=True)
    with Pool(os.cpu_count()) as p:
        res = p.map(run_cfg, todo, chunksize=32)
    json.dump(res, open('wf_results.json', 'w'), default=str)

    valid = [r for r in res if r['is_n'] >= 30]
    print('config con almeno 30 trade in-sample:', len(valid))

    def table(rs, title, n=15):
        print('\n=== ' + title + ' ===')
        print(f"{'ds':5s} {'cad':9s} {'sw':3s} {'lw':4s} {'ma':3s} {'pf':2s} {'mDD':5s} {'cd':2s} {'xs':3s} | "
              f"{'IS net':>9s} {'IS dd':>8s} {'IScal':>6s} {'ISn':>4s} || {'OOS net':>9s} {'OOS dd':>8s} {'OOScal':>6s} {'OOSn':>4s} {'m-':>3s}")
        for r in rs[:n]:
            print(f"{r['dataset']:5s} {r['rotationPeriod']:9s} {r['shortWindowDays']:3d} {r['longWindowDays']:4d} "
                  f"{r['movingAverageWindowDays']:3d} {r['minimumPassingFilters']:2d} {r['maximumCurrentDrawdown']:.3f} "
                  f"{r['cooldownPeriodsAfterOff']:2d} {str(r['crossSectionalSizing'])[0]:3s} | "
                  f"{r['is_net']:9,.0f} {r['is_dd']:8,.0f} {r['is_calmar']:6.2f} {r['is_n']:4d} || "
                  f"{r['oos_net']:9,.0f} {r['oos_dd']:8,.0f} {r['oos_calmar']:6.2f} {r['oos_n']:4d} {r['oos_monthsDown']:3d}")

    top = sorted(valid, key=lambda r: -r['is_calmar'])[:50]
    table(top, 'TOP 50 per calmar IN-SAMPLE -> cosa fanno OOS')
    print(f"\n  OOS net mediano dei top-50 IS: {statistics.median(r['oos_net'] for r in top):,.0f}"
          f"   (baseline OOS {bOOS['net']:,.0f})")
    print(f"  quanti dei top-50 IS hanno OOS net > 0: {sum(1 for r in top if r['oos_net']>0)}/50")
    print(f"  quanti hanno OOS calmar > baseline OOS: "
          f"{sum(1 for r in top if r['oos_dd']>0 and r['oos_net']/r['oos_dd'] > (bOOS['net']/bOOS['dailyMaxDDabs'] if bOOS['dailyMaxDDabs']>0 else 0))}/50")

    # correlazione IS->OOS
    xs = [r['is_calmar'] for r in valid if r['is_calmar'] < 900]
    ys = [r['oos_calmar'] for r in valid if r['is_calmar'] < 900]
    if len(xs) > 10:
        mx, my = statistics.mean(xs), statistics.mean(ys)
        num = sum((a - mx) * (b - my) for a, b in zip(xs, ys))
        den = (sum((a - mx) ** 2 for a in xs) * sum((b - my) ** 2 for b in ys)) ** 0.5
        print(f"\n  correlazione calmar IS vs OOS su {len(xs)} config: {num/den if den else 0:+.3f}")

    table(sorted(valid, key=lambda r: -r['oos_calmar']), 'TOP per calmar OUT-OF-SAMPLE (solo diagnostico, e look-ahead)')
