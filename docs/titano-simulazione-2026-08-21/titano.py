"""Simulatore Titano v2 offline, fedele a docs/domini/titano-rotation.md
   e docs/titano-analisi-parametri-e-audit-2026-07-31.md (sezioni 1-3)."""
import json, math, datetime as dt
from dataclasses import dataclass

UTC = dt.timezone.utc
SRC = '/mnt/user-data/uploads/piutoo/piootoo-repository/trades.jsonl'


# ---------------- dati ----------------
def load(path=SRC):
    rows = []
    for l in open(path):
        l = l.strip()
        if not l:
            continue
        r = json.loads(l)
        r['entry'] = dt.datetime.fromisoformat(r['entryTimeUtc'].replace('Z', '+00:00'))
        r['exit'] = dt.datetime.fromisoformat(r['exitTimeUtc'].replace('Z', '+00:00'))
        r['net'] = float(r['netProfit'])
        r['qty'] = float(r['quantity'])
        rows.append(r)
    rows.sort(key=lambda r: (r['exit'], r['tradeId']))
    return rows


# ---------------- calendario ----------------
def periods(start, end, mode, anchor=None):
    out = []
    if mode == 'Weekly':
        c = start - dt.timedelta(days=start.weekday())
        c = c.replace(hour=0, minute=0, second=0, microsecond=0)
        while c < end:
            out.append((c, c + dt.timedelta(days=7)))
            c += dt.timedelta(days=7)
    elif mode == 'Biweekly':
        a = anchor or start.replace(hour=0, minute=0, second=0, microsecond=0)
        c = a
        while c < end:
            out.append((c, c + dt.timedelta(days=14)))
            c += dt.timedelta(days=14)
    elif mode == 'Monthly':
        c = start.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
        while c < end:
            n = (c.replace(day=28) + dt.timedelta(days=4)).replace(day=1)
            out.append((c, n))
            c = n
    else:
        raise ValueError(mode)
    return out


# ---------------- configurazione ----------------
@dataclass
class Cfg:
    rotationPeriod: str = 'Weekly'
    initialCapital: float = 100_000.0
    shortWindowDays: int = 90
    longWindowDays: int = 365
    movingAverageWindowDays: int = 90
    minimumTrades: int = 5
    minimumShortReturn: float = 0.0
    minimumLongReturn: float = 0.0
    requireEquityAboveMovingAverage: bool = True
    minimumZScore: float = -1.5
    maximumZScore: float = 2.5
    maximumCurrentDrawdown: float = 0.15
    maximumObservedDrawdown: float = 0.25
    maximumReturnVolatility: float = 0.10
    minimumPassingFilters: int = 4
    reenableMaximumCurrentDrawdown: float = 0.10
    cooldownPeriodsAfterOff: int = 2
    minimumOnPeriods: int = 1
    hardStopDrawdown: float = 0.35
    crossSectionalSizing: bool = True
    minimumAllocationMultiplier: float = 0.25
    maximumAllocationMultiplier: float = 1.00
    allocationStep: float = 0.05
    disableCompositeScore: float = 0.40
    reenableCompositeScore: float = 0.60
    sizingTiers: tuple = ((0.80, 1.0), (0.60, 0.5), (0.40, 0.25), (0.0, 0.0))
    commissionPerUnit: float = 0.0
    slippagePerUnit: float = 0.0


# ---------------- metriche per strategia ----------------
def equity_series(trades, cap):
    """[(exitTime, equity, tradeReturn)] su trade chiusi ordinati; E(0)=cap."""
    e = cap
    out = []
    for t in trades:
        prev = e
        e += t['net']
        r = (t['net'] / abs(prev)) if prev != 0 else 0.0
        out.append((t['exit'], e, r))
    return out


def equity_at(series, when):
    """equity all'istante `when` (ultimo punto con exit<=when); None se nessun dato."""
    v = None
    for (ts, e, _) in series:
        if ts <= when:
            v = e
        else:
            break
    return v


def metrics(series, cap, now, cfg):
    if not series:
        return None
    E = series[-1][1]

    def ret(days):
        base = equity_at(series, now - dt.timedelta(days=days))
        if base is None:
            base = cap  # nessun dato prima -> capitale iniziale
        if base == 0:
            return 0.0
        return (E - base) / abs(base)

    shortRet = ret(cfg.shortWindowDays)
    longRet = ret(cfg.longWindowDays)
    maW = [e for (ts, e, _) in series if ts > now - dt.timedelta(days=cfg.movingAverageWindowDays)]
    if maW:
        ma = sum(maW) / len(maW)
        sd = math.sqrt(sum((x - ma) ** 2 for x in maW) / len(maW))
    else:
        ma = E
        sd = 0.0
    z = (E - ma) / sd if sd > 0 else 0.0
    peak = cap
    mdd = 0.0
    for (_, e, _) in series:
        peak = max(peak, e)
        if peak != 0:
            mdd = max(mdd, (peak - e) / abs(peak))
    cur = (peak - E) / abs(peak) if peak != 0 else 0.0
    rs = [r for (ts, e, r) in series if ts > now - dt.timedelta(days=cfg.shortWindowDays)]
    if len(rs) > 1:
        m = sum(rs) / len(rs)
        vol = math.sqrt(sum((x - m) ** 2 for x in rs) / len(rs))
    else:
        vol = 0.0
    nShort = len([1 for (ts, _, _) in series if ts > now - dt.timedelta(days=cfg.shortWindowDays)])
    return dict(E=E, shortRet=shortRet, longRet=longRet, ma=ma, z=z, curDD=cur, maxDD=mdd,
                vol=vol, nShort=nShort, aboveMa=E >= ma)


def votes(m, cfg):
    """5 voti: (passed, score 0..1)"""
    v = {}
    ok = (m['nShort'] >= cfg.minimumTrades and m['shortRet'] >= cfg.minimumShortReturn
          and (m['aboveMa'] if cfg.requireEquityAboveMovingAverage else True))
    v['short'] = (ok, 0.5 + max(-0.5, min(0.5, m['shortRet'] - cfg.minimumShortReturn)))
    ok = m['longRet'] >= cfg.minimumLongReturn
    v['long'] = (ok, 0.5 + max(-0.5, min(0.5, m['longRet'] - cfg.minimumLongReturn)))
    lo, hi = cfg.minimumZScore, cfg.maximumZScore
    c = (lo + hi) / 2
    half = (hi - lo) / 2
    inb = lo <= m['z'] <= hi
    v['z'] = (inb, max(0.0, 1 - abs(m['z'] - c) / half) if half > 0 and inb else 0.0)
    ok = m['curDD'] <= cfg.maximumCurrentDrawdown and m['maxDD'] <= cfg.maximumObservedDrawdown
    v['dd'] = (ok, max(0.0, 1 - m['curDD'] / cfg.maximumCurrentDrawdown) if cfg.maximumCurrentDrawdown > 0 else 0.0)
    ok = m['vol'] <= cfg.maximumReturnVolatility
    v['vol'] = (ok, max(0.0, 1 - m['vol'] / cfg.maximumReturnVolatility) if cfg.maximumReturnVolatility > 0 else 0.0)
    return v


VKEYS = ['short', 'long', 'z', 'dd', 'vol']


def percentiles(scores_by_strategy):
    codes = list(scores_by_strategy)
    N = len(codes)
    if N == 1:
        return {codes[0]: 1.0}
    out = {c: 0.0 for c in codes}
    for k in VKEYS:
        vals = [scores_by_strategy[c][k] for c in codes]
        for c in codes:
            s = scores_by_strategy[c][k]
            below = sum(1 for x in vals if x < s)
            eq = sum(1 for x in vals if x == s)
            out[c] += (below + (eq - 1) / 2) / (N - 1)
    return {c: out[c] / len(VKEYS) for c in codes}


@dataclass
class State:
    on: bool = True
    consecOn: int = 0
    cooldown: int = 0
    hardStopped: bool = False
    alloc: float = 1.0
    first: bool = True


def run(trades, cfg, start=None, end=None):
    if start is None:
        start = min(t['exit'] for t in trades)
    if end is None:
        end = max(t['exit'] for t in trades) + dt.timedelta(days=1)
    P = periods(start, end, cfg.rotationPeriod)
    by = {}
    for t in trades:
        by.setdefault(t['strategyCode'], []).append(t)
    for c in by:
        by[c].sort(key=lambda t: (t['exit'], t['tradeId']))
    codes = sorted(by)
    states = {c: State() for c in codes}
    decisions = []
    for i in range(len(P) - 1):
        pend = P[i][1]
        effFrom, effTo = P[i + 1]
        cand = {}
        for c in codes:
            hist = [t for t in by[c] if t['exit'] < pend]
            if not hist:
                continue
            m = metrics(equity_series(hist, cfg.initialCapital), cfg.initialCapital, pend, cfg)
            cand[c] = (m, votes(m, cfg))
        if not cand:
            decisions.append((effFrom, effTo, {}))
            continue
        onset = {}
        for c, (m, v) in cand.items():
            s = states[c]
            npass = sum(1 for k in VKEYS if v[k][0])
            eligible = npass >= cfg.minimumPassingFilters
            raw = sum(v[k][1] for k in VKEYS) / 5
            if m['maxDD'] >= cfg.hardStopDrawdown or m['curDD'] >= cfg.hardStopDrawdown:
                s.hardStopped = True
            mayDisable = s.first or s.consecOn >= cfg.minimumOnPeriods
            disable = ((m['curDD'] > cfg.maximumCurrentDrawdown) or (not eligible)
                       or ((not cfg.crossSectionalSizing) and raw < cfg.disableCompositeScore))
            reenable = ((m['curDD'] <= cfg.reenableMaximumCurrentDrawdown) and eligible
                        and s.cooldown == 0
                        and ((raw >= cfg.reenableCompositeScore) if not cfg.crossSectionalSizing else True))
            if s.first:
                on = not disable
            elif s.on:
                on = (not disable) or (not mayDisable)
            else:
                on = reenable
            if s.hardStopped:
                on = False
            onset[c] = (on, raw, v, npass)
        alloc = {}
        onc = [c for c in onset if onset[c][0]]
        if cfg.crossSectionalSizing:
            if onc:
                sc = {c: {k: onset[c][2][k][1] for k in VKEYS} for c in onc}
                pc = percentiles(sc)
                for c in onc:
                    a = (cfg.minimumAllocationMultiplier
                         + (cfg.maximumAllocationMultiplier - cfg.minimumAllocationMultiplier) * pc[c])
                    a = round(a / cfg.allocationStep) * cfg.allocationStep
                    alloc[c] = min(cfg.maximumAllocationMultiplier,
                                   max(cfg.minimumAllocationMultiplier, a))
        else:
            for c in onc:
                raw = onset[c][1]
                a = 0.0
                for thr, mult in sorted(cfg.sizingTiers, key=lambda x: -x[0]):
                    if raw >= thr:
                        a = mult
                        break
                if a > 0:
                    alloc[c] = a
        for c in codes:
            if c not in onset:
                continue
            s = states[c]
            was = s.on
            s.first = False
            s.on = c in alloc and alloc[c] > 0
            if s.on:
                s.consecOn += 1
                s.cooldown = max(0, s.cooldown - 1)
            else:
                s.consecOn = 0
                s.cooldown = cfg.cooldownPeriodsAfterOff if was else max(0, s.cooldown - 1)
            s.alloc = alloc.get(c, 0.0)
        decisions.append((effFrom, effTo, dict(alloc)))
    return decisions, P


def apply(trades, decisions, cfg):
    """Ricalcolo offline: trade incluso con la decisione efficace al suo entryTimeUtc,
       netProfit contabilizzato a exitTimeUtc e scalato dall'allocazione."""
    out = []
    outside = 0
    for t in trades:
        a = None
        for (s, e, al) in decisions:
            if s <= t['entry'] < e:
                a = al
                break
        if a is None:
            outside += 1
            continue
        mult = a.get(t['strategyCode'], 0.0)
        if mult <= 0:
            continue
        cost = (cfg.commissionPerUnit + cfg.slippagePerUnit) * t['qty'] * mult
        out.append(dict(t, adj=t['net'] * mult - cost, mult=mult))
    out.sort(key=lambda r: (r['exit'], r['tradeId']))
    return out, outside


def curve_stats(rows, key='adj', cap=100_000.0):
    e = cap
    peak = cap
    mdd = 0.0
    mddabs = 0.0
    pts = []
    negsteps = 0
    prev = cap
    for r in rows:
        e += r[key]
        pts.append((r['exit'], e))
        if e < prev:
            negsteps += 1
        prev = e
        peak = max(peak, e)
        mddabs = max(mddabs, peak - e)
        mdd = max(mdd, (peak - e) / peak)
    daily = {}
    for ts, v in pts:
        daily[ts.date()] = v
    dd = sorted(daily)
    dpk = cap
    dmdd = 0.0
    dmddabs = 0.0
    dneg = 0
    dprev = cap
    for d in dd:
        v = daily[d]
        if v < dprev:
            dneg += 1
        dprev = v
        dpk = max(dpk, v)
        dmdd = max(dmdd, (dpk - v) / dpk)
        dmddabs = max(dmddabs, dpk - v)
    mon = {}
    for ts, v in pts:
        mon[ts.strftime('%Y-%m')] = v
    mk = sorted(mon)
    mneg = 0
    mprev = cap
    worstm = 0.0
    for k in mk:
        if mon[k] < mprev:
            mneg += 1
        worstm = min(worstm, mon[k] - mprev)
        mprev = mon[k]
    return dict(net=e - cap, n=len(rows), maxDD=mdd, maxDDabs=mddabs,
                dailyMaxDD=dmdd, dailyMaxDDabs=dmddabs, dailyDownDays=dneg, dailyDays=len(dd),
                monthsDown=mneg, months=len(mk), worstMonth=worstm, tradeDown=negsteps,
                pts=pts, monthly=[(k, mon[k]) for k in mk], daily=[(d, daily[d]) for d in dd])
