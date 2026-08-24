"""La stessa prova di onesta' fatta sulla griglia settimanale, ripetuta sulla famiglia GIORNALIERA."""
import datetime as dt, statistics, os, json, itertools
from multiprocessing import Pool
import titano as T

TR = T.load()
SPLIT = dt.datetime(2025, 11, 1, tzinfo=dt.timezone.utc)
_orig = T.periods
def patched(start, end, mode, anchor=None):
    if mode == 'Daily':
        c = start.replace(hour=0, minute=0, second=0, microsecond=0); out = []
        while c < end:
            out.append((c, c + dt.timedelta(days=1))); c += dt.timedelta(days=1)
        return out
    return _orig(start, end, mode, anchor)
T.periods = patched

SW = [7, 10, 14, 21, 30, 45]
MA = [7, 14, 21, 30, 45]
PF = [3, 4, 5]
MD = [0.02, 0.03, 0.05, 0.08, 0.12]
CD = [0, 1, 2, 3, 5]
XS = [True, False]
MT = [1, 2]


def ev(a):
    sw, ma, pf, md, cd, xs, mt = a
    cfg = T.Cfg(rotationPeriod='Daily', shortWindowDays=sw, longWindowDays=90,
                movingAverageWindowDays=ma, minimumTrades=mt, minimumPassingFilters=pf,
                maximumCurrentDrawdown=md, reenableMaximumCurrentDrawdown=md*0.67,
                cooldownPeriodsAfterOff=cd, minimumOnPeriods=0, crossSectionalSizing=xs)
    try:
        dec, _ = T.run(TR, cfg)
        rows, _ = T.apply(TR, dec, cfg)
    except Exception:
        return None
    if not rows: return None
    A = T.curve_stats([r for r in rows if r['exit'] < SPLIT])
    B = T.curve_stats([r for r in rows if r['exit'] >= SPLIT])
    if A['n'] < 20 or B['n'] < 10: return None
    ic = A['net']/A['dailyMaxDDabs'] if A['dailyMaxDDabs'] > 0 else None
    oc = B['net']/B['dailyMaxDDabs'] if B['dailyMaxDDabs'] > 0 else None
    if ic is None or oc is None: return None
    return dict(sw=sw, ma=ma, pf=pf, md=md, cd=cd, xs=xs, mt=mt,
                is_calmar=ic, oos_calmar=oc, is_net=A['net'], oos_net=B['net'],
                is_dd=A['dailyMaxDDabs'], oos_dd=B['dailyMaxDDabs'])


if __name__ == '__main__':
    todo = list(itertools.product(SW, MA, PF, MD, CD, XS, MT))
    print('config giornaliere', len(todo), flush=True)
    with Pool(os.cpu_count()) as p:
        res = [r for r in p.map(ev, todo, chunksize=16) if r]
    print('valide', len(res))
    xs_ = [r['is_calmar'] for r in res]; ys = [r['oos_calmar'] for r in res]
    mx, my = statistics.mean(xs_), statistics.mean(ys)
    num = sum((a-mx)*(b-my) for a, b in zip(xs_, ys))
    den = (sum((a-mx)**2 for a in xs_)*sum((b-my)**2 for b in ys))**0.5
    print(f"correlazione calmar IS vs OOS (famiglia GIORNALIERA): {num/den:+.3f}")
    top = sorted(res, key=lambda r: -r['is_calmar'])[:50]
    print(f"top-50 per calmar IS -> OOS net mediano {statistics.median(r['oos_net'] for r in top):,.0f}, "
          f"OOS calmar mediano {statistics.median(r['oos_calmar'] for r in top):.2f}")
    print(f"quanti dei top-50 IS hanno OOS calmar > 0.46 (baseline OOS): "
          f"{sum(1 for r in top if r['oos_calmar']>0.46)}/50")
    print(f"\nmediana OOS calmar per minimumPassingFilters:")
    for pf in PF:
        s = [r for r in res if r['pf'] == pf]
        print(f"  pf={pf}  n={len(s):4d}  IS calmar med {statistics.median(r['is_calmar'] for r in s):5.2f}  "
              f"OOS calmar med {statistics.median(r['oos_calmar'] for r in s):5.2f}  "
              f"OOS net med {statistics.median(r['oos_net'] for r in s):8,.0f}")
    print(f"\nmediana OOS calmar per crossSectionalSizing:")
    for x in XS:
        s = [r for r in res if r['xs'] == x]
        print(f"  xs={x}  n={len(s):4d}  IS calmar med {statistics.median(r['is_calmar'] for r in s):5.2f}  "
              f"OOS calmar med {statistics.median(r['oos_calmar'] for r in s):5.2f}  "
              f"OOS net med {statistics.median(r['oos_net'] for r in s):8,.0f}")
    # solo pf=5
    s5 = [r for r in res if r['pf'] == 5]
    xs5 = [r['is_calmar'] for r in s5]; ys5 = [r['oos_calmar'] for r in s5]
    mx, my = statistics.mean(xs5), statistics.mean(ys5)
    num = sum((a-mx)*(b-my) for a, b in zip(xs5, ys5))
    den = (sum((a-mx)**2 for a in xs5)*sum((b-my)**2 for b in ys5))**0.5
    print(f"\ncorrelazione IS/OOS nella sola regione pf=5: {num/den:+.3f} (n={len(s5)})")
    json.dump(res, open('wf_daily.json', 'w'))
