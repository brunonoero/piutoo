"""Confronto finale delle varianti + robustezza + curve per il report."""
import datetime as dt, collections, json, statistics
import titano as T

TR = T.load(); CAP = 100_000.0
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


def daily_stop(trades, limit):
    realized = collections.defaultdict(float)
    events = []
    for t in trades:
        events.append((t['entry'], 0, t)); events.append((t['exit'], 1, t))
    events.sort(key=lambda e: (e[0], e[1], e[2]['tradeId']))
    open_ids = set(); kept = []
    for ts, kind, t in events:
        if kind == 0:
            if realized[ts.date()] > -limit:
                open_ids.add(t['tradeId'])
        else:
            if t['tradeId'] in open_ids:
                realized[t['exit'].date()] += t['net']
                kept.append(t)
    kept.sort(key=lambda r: (r['exit'], r['tradeId']))
    return kept


def titano_rows(cfg, trades=None, sizing=True):
    tr = trades if trades is not None else TR
    dec, _ = T.run(tr, cfg)
    rows, outside = T.apply(tr, dec, cfg)
    if not sizing:
        rows = [dict(r, adj=r['net']) for r in rows]
    return rows, dec


def report(name, rows):
    s = T.curve_stats(rows)
    a = T.curve_stats([r for r in rows if r['exit'] < SPLIT])
    b = T.curve_stats([r for r in rows if r['exit'] >= SPLIT])
    return dict(name=name, net=s['net'], dd=s['dailyMaxDDabs'],
                calmar=s['net']/s['dailyMaxDDabs'] if s['dailyMaxDDabs'] else 0,
                monthsDown=s['monthsDown'], months=s['months'], n=s['n'],
                worstMonth=s['worstMonth'],
                is_net=a['net'], is_dd=a['dailyMaxDDabs'],
                oos_net=b['net'], oos_dd=b['dailyMaxDDabs'],
                oos_calmar=b['net']/b['dailyMaxDDabs'] if b['dailyMaxDDabs'] else 0,
                daily=[(str(d), v) for d, v in s['daily']],
                monthly=s['monthly'])


VAR = []
VAR.append(report('A · Baseline (nessun filtro)', [dict(t, adj=t['net']) for t in TR]))

cfgB = T.Cfg()
rB, _ = titano_rows(cfgB)
VAR.append(report('B · Titano default (Weekly, sw90)', rB))

cfgB2 = T.Cfg(rotationPeriod='Monthly')
rB2, _ = titano_rows(cfgB2)
VAR.append(report('B2 · Titano default (Monthly, sw90)', rB2))

VAR.append(report('C · Limite perdita giornaliero 5.000',
                  [dict(t, adj=t['net']) for t in daily_stop(TR, 5000)]))
VAR.append(report('C2 · Limite perdita giornaliero 2.000',
                  [dict(t, adj=t['net']) for t in daily_stop(TR, 2000)]))

cfgD = T.Cfg(rotationPeriod='Daily', shortWindowDays=14, longWindowDays=90,
             movingAverageWindowDays=21, minimumTrades=1, minimumPassingFilters=5,
             maximumCurrentDrawdown=0.03, reenableMaximumCurrentDrawdown=0.02,
             cooldownPeriodsAfterOff=3, minimumOnPeriods=0, crossSectionalSizing=True)
rD, decD = titano_rows(cfgD)
VAR.append(report('D · Titano GIORNALIERO + sizing percentile', rD))

rD2, _ = titano_rows(cfgD, sizing=False)
VAR.append(report('E · Titano GIORNALIERO solo ON/OFF', rD2))

keep = set(r['tradeId'] for r in rD2)
sub = [t for t in TR if t['tradeId'] in keep]
VAR.append(report('F · Titano GIORNALIERO ON/OFF + limite 5.000',
                  [dict(t, adj=t['net']) for t in daily_stop(sub, 5000)]))

print(f"{'variante':46s} | {'net':>8s} {'maxDD':>8s} {'calmar':>6s} {'mesi-':>6s} {'n':>4s} "
      f"|| {'IS net':>8s} {'IS dd':>7s} | {'OOS net':>8s} {'OOS dd':>7s} {'OOScal':>6s}")
for v in VAR:
    print(f"{v['name']:46s} | {v['net']:8,.0f} {v['dd']:8,.0f} {v['calmar']:6.2f} "
          f"{v['monthsDown']:2d}/{v['months']:<3d} {v['n']:4d} || {v['is_net']:8,.0f} {v['is_dd']:7,.0f} | "
          f"{v['oos_net']:8,.0f} {v['oos_dd']:7,.0f} {v['oos_calmar']:6.2f}")

# ---- robustezza del vicinato di D ----
print("\n--- robustezza: vicinato della config GIORNALIERA (variando un parametro alla volta) ---")
base = dict(shortWindowDays=14, movingAverageWindowDays=21, minimumPassingFilters=5,
            maximumCurrentDrawdown=0.03, cooldownPeriodsAfterOff=3)
sweep = dict(shortWindowDays=[7, 10, 14, 21, 30, 45],
             movingAverageWindowDays=[7, 14, 21, 30, 45],
             minimumPassingFilters=[3, 4, 5],
             maximumCurrentDrawdown=[0.02, 0.03, 0.05, 0.08, 0.12],
             cooldownPeriodsAfterOff=[0, 1, 2, 3, 5, 10])
rob = {}
for k, vals in sweep.items():
    print(f"\n  {k}:")
    for v in vals:
        d = dict(base); d[k] = v
        cfg = T.Cfg(rotationPeriod='Daily', longWindowDays=90, minimumTrades=1,
                    minimumOnPeriods=0, crossSectionalSizing=True,
                    reenableMaximumCurrentDrawdown=d['maximumCurrentDrawdown']*0.67, **d)
        r, _ = titano_rows(cfg)
        if not r:
            print(f"    {v:>6}  (nessun trade)"); continue
        rp = report('x', r)
        rob.setdefault(k, []).append((v, rp['calmar'], rp['oos_calmar']))
        print(f"    {str(v):>6s}  net {rp['net']:8,.0f}  DD {rp['dd']:8,.0f}  calmar {rp['calmar']:5.2f}  "
              f"| OOS net {rp['oos_net']:8,.0f} DD {rp['oos_dd']:7,.0f} calmar {rp['oos_calmar']:5.2f}  n={rp['n']}")

# ---- quanti dei 10 trade migliori sopravvivono ----
top10 = set(t['tradeId'] for t in sorted(TR, key=lambda t: -t['net'])[:10])
print("\n--- quanti dei 10 trade PIU' PROFITTEVOLI sopravvivono al filtro ---")
for nm, rows in (('Titano default weekly', rB), ('Titano giornaliero', rD),
                 ('Titano giorn. ON/OFF', rD2), ('limite giornaliero 5k', [dict(t, adj=t['net']) for t in daily_stop(TR, 5000)])):
    ids = set(r['tradeId'] for r in rows)
    surv = len(top10 & ids)
    tot = sum(r['adj'] for r in rows if r['tradeId'] in top10)
    orig = sum(t['net'] for t in TR if t['tradeId'] in top10)
    print(f"  {nm:24s} {surv:2d}/10 sopravvissuti, valore incassato {tot:>9,.0f} su {orig:,.0f}")

json.dump(VAR, open('final_variants.json', 'w'))
print('\nsalvato final_variants.json')
