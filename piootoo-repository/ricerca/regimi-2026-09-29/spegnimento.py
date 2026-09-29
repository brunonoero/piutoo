import json,statistics,sys
from collections import defaultdict
from datetime import datetime
from regimi import load_daily,classify,label_for,zscore
t=json.load(open(r'..\piani-pesati-2026-09-27\run-storia-senza-pesi\trades.json',encoding='utf-8-sig'))
R={}
for s in {x['symbol'] for x in t}:
    o,b,_=load_daily(s); L=classify(o,b); R[s]=(L,sorted(L))
D=defaultdict(list)
for x in t:
    d=datetime.fromisoformat(x['entryTimeUtc'].replace('Z','+00:00')).date(); l=label_for(*R[x['symbol']],d)
    if l: D[x['strategyCode']].append((d.year,l,float(x['netProfit'])))
N=("trend-su","trend-giu","laterale","calma","normale","agitata")
tot_after=sum(p for v in D.values() for y,l,p in v if y>=2019)
print('netto totale dal 2019', round(tot_after))
for cut in (2016,2019,2022):
  g=0;n=0;rows=[]
  for c,it in D.items():
    a=[x for x in it if x[0]<cut]
    for nm in N:
      ins=[p for y,l,p in a if nm in l]; out=[p for y,l,p in a if nm not in l]
      z=zscore(ins,out)
      if z is not None and z<-2 and statistics.fmean(ins)<0:
        rem=[p for y,l,p in it if y>=cut and nm in l]
        rows.append((c,nm,len(ins),round(sum(ins)),len(rem),round(sum(rem))))
        g-=sum(rem); n+=len(rem)
  print(f'\ntaglio {cut}: {len(rows)} coppie, {n} trade tolti dopo, effetto {g:+,.0f}')
  for r in rows: print('  ',r)
