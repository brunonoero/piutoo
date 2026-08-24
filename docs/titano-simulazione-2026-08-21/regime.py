"""Il drawdown e' idiosincratico (spegnibile per strategia) o comune (regime)?
   E: un interruttore di PORTAFOGLIO farebbe meglio di Titano per-strategia?"""
import datetime as dt, statistics, itertools, json
import titano as T

TR = T.load()
CAP = 100_000.0
codes = sorted(set(t['strategyCode'] for t in TR))

# ---- 1. correlazione fra strategie sul P&L mensile ----
months = sorted(set(t['exit'].strftime('%Y-%m') for t in TR))
pnl = {c: {m: 0.0 for m in months} for c in codes}
for t in TR:
    pnl[t['strategyCode']][t['exit'].strftime('%Y-%m')] += t['net']

def corr(a, b):
    if len(a) < 3: return None
    ma, mb = statistics.mean(a), statistics.mean(b)
    num = sum((x-ma)*(y-mb) for x, y in zip(a, b))
    den = (sum((x-ma)**2 for x in a)*sum((y-mb)**2 for y in b))**0.5
    return num/den if den else None

active = [c for c in codes if sum(1 for m in months if pnl[c][m] != 0) >= 5]
cs = []
for a, b in itertools.combinations(active, 2):
    r = corr([pnl[a][m] for m in months], [pnl[b][m] for m in months])
    if r is not None: cs.append(r)
print(f"strategie con >=5 mesi attivi: {len(active)}")
print(f"correlazione P&L mensile fra coppie: mediana {statistics.median(cs):+.3f}, "
      f"media {statistics.mean(cs):+.3f}, quota positive {sum(1 for x in cs if x>0)/len(cs)*100:.0f}%")

print("\n--- quante strategie sono in perdita ogni mese (su quelle attive nel mese) ---")
for m in months:
    act = [c for c in active if pnl[c][m] != 0]
    neg = [c for c in act if pnl[c][m] < 0]
    tot = sum(pnl[c][m] for c in codes)
    print(f"  {m}  attive {len(act):2d}  in perdita {len(neg):2d} ({len(neg)/max(1,len(act))*100:3.0f}%)  P&L {tot:>10,.0f}")

# ---- 2. interruttore di portafoglio ----
TR.sort(key=lambda t: (t['exit'], t['tradeId']))

def equity_curve(rows, key='net'):
    e = CAP; out = []
    for r in rows:
        e += r[key]; out.append((r['exit'], e))
    return out

def portfolio_switch(maDays, ddStop, ddResume, mode='Weekly'):
    """OFF totale se equity di portafoglio sotto la sua media mobile (maDays)
       o se il DD corrente supera ddStop; ON quando DD <= ddResume e sopra MA.
       Decisione presa a fine periodo, efficace dal periodo successivo (no look-ahead)."""
    start = min(t['exit'] for t in TR); end = max(t['exit'] for t in TR)+dt.timedelta(days=1)
    P = T.periods(start, end, mode)
    on = True; dec = []
    for i in range(len(P)-1):
        pend = P[i][1]
        hist = [t for t in TR if t['exit'] < pend]
        if len(hist) < 5:
            dec.append((P[i+1][0], P[i+1][1], on)); continue
        cur = equity_curve(hist)
        E = cur[-1][1]
        win = [v for (ts, v) in cur if ts > pend - dt.timedelta(days=maDays)]
        ma = sum(win)/len(win) if win else E
        peak = CAP;
        for (_, v) in cur: peak = max(peak, v)
        dd = (peak-E)/peak
        if on:
            if dd > ddStop or (maDays > 0 and E < ma): on = False
        else:
            if dd <= ddResume and (maDays == 0 or E >= ma): on = True
        dec.append((P[i+1][0], P[i+1][1], on))
    rows = []
    for t in TR:
        st = None
        for (a, b, s) in dec:
            if a <= t['entry'] < b: st = s; break
        if st: rows.append(dict(t, adj=t['net']))
    return rows

base = T.curve_stats([dict(t, adj=t['net']) for t in TR])
print(f"\nBASELINE                      net {base['net']:>9,.0f}  maxDD {base['dailyMaxDDabs']:>8,.0f}  "
      f"calmar {base['net']/base['dailyMaxDDabs']:5.2f}  mesi- {base['monthsDown']}/{base['months']}  n {base['n']}")

print("\n--- interruttore di PORTAFOGLIO (spegne tutto insieme) ---")
best = []
for mode in ('Weekly', 'Biweekly', 'Monthly'):
    for maDays in (0, 21, 45, 90):
        for ddStop in (0.05, 0.08, 0.10, 0.15, 0.20):
            for ddResume in (0.02, 0.05, 0.08, ddStop):
                if ddResume > ddStop: continue
                rows = portfolio_switch(maDays, ddStop, ddResume, mode)
                if not rows: continue
                s = T.curve_stats(rows)
                if s['dailyMaxDDabs'] <= 0: continue
                best.append((s['net']/s['dailyMaxDDabs'], mode, maDays, ddStop, ddResume, s))
best.sort(key=lambda x: -x[0])
print(f"{'cad':9s} {'MA':3s} {'stop':5s} {'resume':6s} | {'net':>9s} {'maxDD':>8s} {'calmar':>6s} {'mesi-':>6s} {'n':>4s}")
for cal, mode, ma, ds_, dr, s in best[:15]:
    print(f"{mode:9s} {ma:3d} {ds_:.3f} {dr:6.3f} | {s['net']:9,.0f} {s['dailyMaxDDabs']:8,.0f} {cal:6.2f} "
          f"{s['monthsDown']:2d}/{s['months']:<3d} {s['n']:4d}")

# walk-forward dell'interruttore di portafoglio
SPLIT = dt.datetime(2025, 11, 1, tzinfo=dt.timezone.utc)
print("\n--- gli stessi interruttori, spezzati IS / OOS ---")
print(f"{'cad':9s} {'MA':3s} {'stop':5s} {'res':5s} | {'IS net':>9s} {'IS dd':>7s} || {'OOS net':>9s} {'OOS dd':>7s}")
for cal, mode, ma, ds_, dr, s in best[:10]:
    rows = portfolio_switch(ma, ds_, dr, mode)
    a = T.curve_stats([r for r in rows if r['exit'] < SPLIT])
    b = T.curve_stats([r for r in rows if r['exit'] >= SPLIT])
    print(f"{mode:9s} {ma:3d} {ds_:.3f} {dr:5.3f} | {a['net']:9,.0f} {a['dailyMaxDDabs']:7,.0f} || "
          f"{b['net']:9,.0f} {b['dailyMaxDDabs']:7,.0f}")
bIS = T.curve_stats([dict(t, adj=t['net']) for t in TR if t['exit'] < SPLIT])
bOOS = T.curve_stats([dict(t, adj=t['net']) for t in TR if t['exit'] >= SPLIT])
print(f"{'BASELINE':9s} {'':3s} {'':5s} {'':5s} | {bIS['net']:9,.0f} {bIS['dailyMaxDDabs']:7,.0f} || "
      f"{bOOS['net']:9,.0f} {bOOS['dailyMaxDDabs']:7,.0f}")
