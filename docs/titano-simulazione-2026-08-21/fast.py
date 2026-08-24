"""Se Titano non puo' funzionare a cadenza settimanale, cosa serve?
   1) un limite di perdita giornaliero (rischio veloce, non Titano)
   2) Titano a cadenza GIORNALIERA (rotazione veloce)"""
import datetime as dt, collections, statistics, json
import titano as T

TR = T.load(); CAP = 100_000.0
BASE = T.curve_stats([dict(t, adj=t['net']) for t in TR])
print(f"BASELINE                net {BASE['net']:>9,.0f}  maxDD {BASE['dailyMaxDDabs']:>8,.0f}  "
      f"calmar {BASE['net']/BASE['dailyMaxDDabs']:5.2f}  mesi- {BASE['monthsDown']}/{BASE['months']}  n {BASE['n']}")

SPLIT = dt.datetime(2025, 11, 1, tzinfo=dt.timezone.utc)

def seg(rows):
    a = T.curve_stats([r for r in rows if r['exit'] < SPLIT])
    b = T.curve_stats([r for r in rows if r['exit'] >= SPLIT])
    return a, b

# ---------- 1) limite di perdita giornaliero ----------
# I trade sono ordinati per uscita; un trade che ENTRA dopo che la perdita
# realizzata del giorno ha superato la soglia viene saltato.
def daily_stop(limit):
    byexit = sorted(TR, key=lambda t: (t['exit'], t['tradeId']))
    realized = collections.defaultdict(float)
    kept = []
    order = sorted(TR, key=lambda t: (t['entry'], t['tradeId']))
    # simulo in ordine temporale reale mescolando entry e exit
    events = []
    for t in TR:
        events.append((t['entry'], 0, t))
        events.append((t['exit'], 1, t))
    events.sort(key=lambda e: (e[0], e[1], e[2]['tradeId']))
    open_ids = set()
    for ts, kind, t in events:
        day = ts.date()
        if kind == 0:
            if realized[day] > -limit:
                open_ids.add(t['tradeId'])
        else:
            if t['tradeId'] in open_ids:
                realized[t['exit'].date()] += t['net']
                kept.append(dict(t, adj=t['net']))
    kept.sort(key=lambda r: (r['exit'], r['tradeId']))
    return kept

print("\n--- 1) LIMITE DI PERDITA GIORNALIERO (blocca le APERTURE per il resto del giorno) ---")
print(f"{'limite':>7s} | {'net':>9s} {'maxDD':>8s} {'calmar':>6s} {'mesi-':>6s} {'n':>4s} || "
      f"{'IS net':>8s} {'IS dd':>7s} | {'OOS net':>8s} {'OOS dd':>7s}")
for lim in (2000, 3000, 4000, 5000, 6000, 8000, 10000, 15000):
    rows = daily_stop(lim)
    if not rows: continue
    s = T.curve_stats(rows)
    a, b = seg(rows)
    cal = s['net']/s['dailyMaxDDabs'] if s['dailyMaxDDabs'] else 0
    print(f"{lim:7,d} | {s['net']:9,.0f} {s['dailyMaxDDabs']:8,.0f} {cal:6.2f} "
          f"{s['monthsDown']:2d}/{s['months']:<3d} {s['n']:4d} || {a['net']:8,.0f} {a['dailyMaxDDabs']:7,.0f} | "
          f"{b['net']:8,.0f} {b['dailyMaxDDabs']:7,.0f}")

# ---------- 2) Titano a cadenza giornaliera ----------
def daily_periods(start, end):
    c = start.replace(hour=0, minute=0, second=0, microsecond=0)
    out = []
    while c < end:
        out.append((c, c + dt.timedelta(days=1))); c += dt.timedelta(days=1)
    return out

_orig = T.periods
def patched(start, end, mode, anchor=None):
    if mode == 'Daily':
        return daily_periods(start, end)
    return _orig(start, end, mode, anchor)
T.periods = patched

print("\n--- 2) TITANO a cadenza GIORNALIERA ---")
print(f"{'sw':>3s} {'ma':>3s} {'pf':>2s} {'mDD':>5s} {'cd':>2s} {'xs':>3s} | {'net':>9s} {'maxDD':>8s} "
      f"{'calmar':>6s} {'mesi-':>6s} {'n':>4s} || {'IS net':>8s} {'IS dd':>7s} | {'OOS net':>8s} {'OOS dd':>7s}")
res = []
for sw in (7, 14, 21, 30):
    for ma in (7, 14, 21):
        for pf in (3, 4, 5):
            for mdd in (0.03, 0.05, 0.08, 0.12):
                for cd in (0, 1, 3):
                    for xs in (True, False):
                        cfg = T.Cfg(rotationPeriod='Daily', shortWindowDays=sw,
                                    longWindowDays=max(90, sw), movingAverageWindowDays=ma,
                                    minimumTrades=1, minimumPassingFilters=pf,
                                    maximumCurrentDrawdown=mdd,
                                    reenableMaximumCurrentDrawdown=mdd*0.67,
                                    cooldownPeriodsAfterOff=cd, minimumOnPeriods=0,
                                    crossSectionalSizing=xs)
                        try:
                            dec, _ = T.run(TR, cfg)
                            rows, _ = T.apply(TR, dec, cfg)
                        except Exception:
                            continue
                        if not rows: continue
                        s = T.curve_stats(rows)
                        if s['net'] <= 0 or s['dailyMaxDDabs'] <= 0: continue
                        res.append((s['net']/s['dailyMaxDDabs'], sw, ma, pf, mdd, cd, xs, s, rows))
res.sort(key=lambda x: -x[0])
for cal, sw, ma, pf, mdd, cd, xs, s, rows in res[:15]:
    a, b = seg(rows)
    print(f"{sw:3d} {ma:3d} {pf:2d} {mdd:.3f} {cd:2d} {str(xs)[0]:3s} | {s['net']:9,.0f} "
          f"{s['dailyMaxDDabs']:8,.0f} {cal:6.2f} {s['monthsDown']:2d}/{s['months']:<3d} {s['n']:4d} || "
          f"{a['net']:8,.0f} {a['dailyMaxDDabs']:7,.0f} | {b['net']:8,.0f} {b['dailyMaxDDabs']:7,.0f}")
print(f"config giornaliere valutate: {len(res)}")
best = res[0]
json.dump(dict(shortWindowDays=best[1], movingAverageWindowDays=best[2], minimumPassingFilters=best[3],
               maximumCurrentDrawdown=best[4], cooldownPeriodsAfterOff=best[5],
               crossSectionalSizing=best[6]), open('best_daily.json', 'w'))

# ---------- 3) combinazione: Titano giornaliero + limite di perdita ----------
print("\n--- 3) TITANO GIORNALIERO + LIMITE DI PERDITA GIORNALIERO ---")
cal, sw, ma, pf, mdd, cd, xs, s, rows = best
keep = set(r['tradeId'] for r in rows)
sub = [t for t in TR if t['tradeId'] in keep]
_TR = TR
print(f"{'limite':>7s} | {'net':>9s} {'maxDD':>8s} {'calmar':>6s} {'mesi-':>6s} {'n':>4s} || "
      f"{'IS net':>8s} {'IS dd':>7s} | {'OOS net':>8s} {'OOS dd':>7s}")
for lim in (3000, 5000, 8000, 10000):
    globals()['TR'] = sub
    r2 = daily_stop(lim)
    globals()['TR'] = _TR
    if not r2: continue
    s2 = T.curve_stats(r2); a, b = seg(r2)
    c2 = s2['net']/s2['dailyMaxDDabs'] if s2['dailyMaxDDabs'] else 0
    print(f"{lim:7,d} | {s2['net']:9,.0f} {s2['dailyMaxDDabs']:8,.0f} {c2:6.2f} "
          f"{s2['monthsDown']:2d}/{s2['months']:<3d} {s2['n']:4d} || {a['net']:8,.0f} {a['dailyMaxDDabs']:7,.0f} | "
          f"{b['net']:8,.0f} {b['dailyMaxDDabs']:7,.0f}")
