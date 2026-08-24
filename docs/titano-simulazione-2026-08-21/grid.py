import itertools, json, math, os, random, sys, time, statistics
from multiprocessing import Pool
import titano as T

TRADES = T.load()

# variante rischio normalizzato per symbol
NQm = statistics.mean(abs(t['net']) for t in TRADES if t['symbol'] == 'NQ')
GCm = statistics.mean(abs(t['net']) for t in TRADES if t['symbol'] == 'GC')
K = NQm / GCm
TRADES_NORM = [dict(t, net=t['net'] * (K if t['symbol'] == 'GC' else 1.0)) for t in TRADES]

DATASETS = {'raw': TRADES, 'norm': TRADES_NORM}

GRID = dict(
    rotationPeriod=['Weekly', 'Biweekly', 'Monthly'],
    shortWindowDays=[14, 21, 30, 45, 60, 90],
    longWindowDays=[60, 90, 180, 365],
    movingAverageWindowDays=[14, 21, 45, 90],
    minimumTrades=[1, 2, 3, 5],
    maximumCurrentDrawdown=[0.03, 0.05, 0.08, 0.12, 0.15],
    reenableRatio=[0.3, 0.5, 0.67, 1.0],
    minimumPassingFilters=[3, 4, 5],
    cooldownPeriodsAfterOff=[0, 1, 2, 4],
    minimumOnPeriods=[0, 1, 2],
    maximumReturnVolatility=[0.03, 0.05, 0.10, 0.25],
    minimumShortReturn=[-0.02, 0.0, 0.01],
    requireEquityAboveMovingAverage=[True, False],
    crossSectionalSizing=[True, False],
    allocBand=[(0.25, 1.00), (0.10, 1.00), (0.50, 1.00), (0.25, 0.60), (0.0, 1.0)],
)


def sample(rng):
    while True:
        d = {k: rng.choice(v) for k, v in GRID.items()}
        if d['longWindowDays'] >= d['shortWindowDays']:
            return d


def build(d):
    lo, hi = d['allocBand']
    return T.Cfg(
        rotationPeriod=d['rotationPeriod'],
        shortWindowDays=d['shortWindowDays'],
        longWindowDays=d['longWindowDays'],
        movingAverageWindowDays=d['movingAverageWindowDays'],
        minimumTrades=d['minimumTrades'],
        minimumShortReturn=d['minimumShortReturn'],
        requireEquityAboveMovingAverage=d['requireEquityAboveMovingAverage'],
        maximumCurrentDrawdown=d['maximumCurrentDrawdown'],
        reenableMaximumCurrentDrawdown=d['maximumCurrentDrawdown'] * d['reenableRatio'],
        minimumPassingFilters=d['minimumPassingFilters'],
        cooldownPeriodsAfterOff=d['cooldownPeriodsAfterOff'],
        minimumOnPeriods=d['minimumOnPeriods'],
        maximumReturnVolatility=d['maximumReturnVolatility'],
        crossSectionalSizing=d['crossSectionalSizing'],
        minimumAllocationMultiplier=lo,
        maximumAllocationMultiplier=hi,
    )


def evaluate(args):
    d, ds = args
    cfg = build(d)
    tr = DATASETS[ds]
    try:
        dec, P = T.run(tr, cfg)
        rows, outside = T.apply(tr, dec, cfg)
        if not rows:
            return None
        s = T.curve_stats(rows)
    except Exception:
        return None
    if s['net'] <= 0:
        return None
    downFrac = s['dailyDownDays'] / max(1, s['dailyDays'])
    rec = dict(d)
    rec['allocBand'] = list(d['allocBand'])
    rec.update(dataset=ds, net=s['net'], maxDD=s['dailyMaxDDabs'], maxDDpct=s['dailyMaxDD'],
               downFrac=downFrac, monthsDown=s['monthsDown'], months=s['months'],
               worstMonth=s['worstMonth'], nTrades=s['n'], outside=outside,
               calmar=s['net'] / s['dailyMaxDDabs'] if s['dailyMaxDDabs'] > 0 else 999.0)
    return rec


if __name__ == '__main__':
    N = int(sys.argv[1]) if len(sys.argv) > 1 else 8000
    seed = int(sys.argv[2]) if len(sys.argv) > 2 else 20260821
    rng = random.Random(seed)
    seen = set()
    todo = []
    while len(todo) < N:
        d = sample(rng)
        for ds in ('raw', 'norm'):
            key = (json.dumps(d, sort_keys=True, default=str), ds)
            if key in seen:
                continue
            seen.add(key)
            todo.append((d, ds))
    print('combos', len(todo), flush=True)
    t0 = time.time()
    out = []
    with Pool(os.cpu_count()) as p:
        for i, r in enumerate(p.imap_unordered(evaluate, todo, chunksize=32)):
            if r:
                out.append(r)
            if i and i % 5000 == 0:
                print(i, f'{time.time()-t0:.0f}s valid={len(out)}', flush=True)
    print('valid', len(out), f'{time.time()-t0:.0f}s')
    json.dump(out, open('grid_results.json', 'w'))
