import os
from collections import defaultdict
import piani as P
tr=P.load_trades(P.FTMO_RUNS)
lab=P.labels_for({t['symbol'] for t in tr}|{'ES'},[os.path.join(P.REPO,'datafeed-external','FTMO'),os.path.join(P.REPO,'datafeed')])
for s,v in sorted(lab.items()): print(s,'etichette dal',v[1][0])
y=defaultdict(lambda: defaultdict(float)); n=defaultdict(lambda: defaultdict(int))
for t in tr:
    if t['plan']!='FTMO-EUROPA': continue
    l=P.label_for(*lab[t['symbol']][:2],P.day(t['entryTimeUtc']))
    k=(t['strategyCode'][5:],l[1]); yy=t['entryTimeUtc'][:4]; y[k][yy]+=t['pnl']; n[k][yy]+=1
for k in sorted(y): print(k,{a:(round(b),n[k][a]) for a,b in sorted(y[k].items())})
m=defaultdict(float)
for t in tr:
    if P.day(t['exitTimeUtc']).strftime('%Y-%m')=='2026-06': m[t['plan']]+=t['pnl']
print('2026-06',dict(m))
