"""Anatomia del drawdown: e' un declino lento (spegnibile) o un colpo secco (non spegnibile)?"""
import datetime as dt, collections, statistics
import titano as T

TR = T.load(); CAP = 100_000.0
e = CAP; pts = []
for t in TR:
    e += t['net']; pts.append((t['exit'], e, t))
peak = CAP; peakT = TR[0]['exit']; worst = 0; span = None
for ts, v, t in pts:
    if v > peak: peak, peakT = v, ts
    if peak - v > worst: worst = peak - v; span = (peakT, ts)
print(f"max drawdown {worst:,.0f} da {span[0]:%Y-%m-%d} a {span[1]:%Y-%m-%d} "
      f"({(span[1]-span[0]).days} giorni)")
inside = [t for ts, v, t in pts if span[0] < ts <= span[1]]
print(f"trade dentro il drawdown: {len(inside)}   P&L {sum(t['net'] for t in inside):,.0f}")
losses = sorted((t for t in inside if t['net'] < 0), key=lambda t: t['net'])
print(f"\ni 10 trade peggiori dentro il drawdown (su {len(losses)} perdenti):")
tot = sum(t['net'] for t in losses)
for t in losses[:10]:
    print(f"  {t['exit']:%Y-%m-%d} {t['strategyCode']:22s} {t['net']:>10,.0f}  "
          f"({t['net']/tot*100:4.1f}% delle perdite del periodo)")
print(f"\ni 5 trade peggiori valgono {sum(t['net'] for t in losses[:5]):,.0f} "
      f"su {tot:,.0f} di perdite totali nel drawdown ({sum(t['net'] for t in losses[:5])/tot*100:.0f}%)")
print(f"strategie coinvolte nelle perdite del drawdown: "
      f"{len(set(t['strategyCode'] for t in losses))} diverse")
c = collections.Counter()
for t in losses: c[t['strategyCode']] += t['net']
for k, v in c.most_common()[::-1][:8]:
    print(f"    {k:22s} {v:>10,.0f}")

print("\n--- concentrazione per giorno di uscita ---")
d = collections.Counter()
for t in inside: d[t['exit'].date()] += t['net']
worstdays = sorted(d.items(), key=lambda kv: kv[1])[:8]
for day, v in worstdays:
    print(f"  {day}  {v:>10,.0f}")
print(f"  i 3 giorni peggiori valgono {sum(v for _,v in worstdays[:3]):,.0f} "
      f"su {worst:,.0f} di drawdown")

print("\n--- distribuzione dei trade: quanto pesa la coda ---")
allsorted = sorted(TR, key=lambda t: t['net'])
print(f"  10 peggiori: {sum(t['net'] for t in allsorted[:10]):,.0f}")
print(f"  10 migliori: {sum(t['net'] for t in allsorted[-10:]):,.0f}")
print(f"  net totale : {sum(t['net'] for t in TR):,.0f}")
print(f"  net senza i 10 migliori: {sum(t['net'] for t in allsorted[:-10]):,.0f}")
pos = [t['net'] for t in TR if t['net'] > 0]; neg = [t['net'] for t in TR if t['net'] < 0]
print(f"  vincenti {len(pos)} ({len(pos)/len(TR)*100:.0f}%) media {statistics.mean(pos):,.0f}; "
      f"perdenti {len(neg)} media {statistics.mean(neg):,.0f}")

print("\n--- durata dei trade (quanto Titano puo' anticipare) ---")
hrs = sorted((t['exit']-t['entry']).total_seconds()/3600 for t in TR)
print(f"  mediana {statistics.median(hrs):.1f} h, p90 {hrs[int(len(hrs)*0.9)]:.1f} h, max {hrs[-1]:.1f} h")

print("\n--- P&L per mese, per strategia (quante scendono insieme) ---")
months = sorted(set(t['exit'].strftime('%Y-%m') for t in TR))
codes = sorted(set(t['strategyCode'] for t in TR))
m = {c: collections.defaultdict(float) for c in codes}
for t in TR: m[t['strategyCode']][t['exit'].strftime('%Y-%m')] += t['net']
hdr = 'strategia               ' + ' '.join(f"{x[2:]:>8s}" for x in months)
print(hdr)
for c in codes:
    row = ' '.join(f"{m[c][x]:8,.0f}" if m[c][x] else '       .' for x in months)
    print(f"{c:22s} {row}")
