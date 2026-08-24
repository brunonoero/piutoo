import json, statistics, collections
R = json.load(open('grid_results.json'))
print('risultati validi', len(R))

BASE_NET, BASE_DD = 74005.4, 31021.87

def show(rs, title, n=12):
    print('\n=== ' + title + ' ===')
    print(f"{'ds':5s} {'cad':9s} {'sw':3s} {'lw':4s} {'ma':3s} {'mt':2s} {'mDD':5s} {'re':5s} {'pf':2s} {'cd':2s} {'mo':2s} {'vol':5s} {'msr':5s} {'ma>':3s} {'xs':3s} {'band':11s} | {'net':>9s} {'maxDD':>8s} {'calmar':>6s} {'down%':>6s} {'m-':>3s} {'worst':>8s} {'n':>4s}")
    for r in rs[:n]:
        print(f"{r['dataset']:5s} {r['rotationPeriod']:9s} {r['shortWindowDays']:3d} {r['longWindowDays']:4d} "
              f"{r['movingAverageWindowDays']:3d} {r['minimumTrades']:2d} {r['maximumCurrentDrawdown']:.3f} "
              f"{r['reenableRatio']:.2f} {r['minimumPassingFilters']:2d} {r['cooldownPeriodsAfterOff']:2d} "
              f"{r['minimumOnPeriods']:2d} {r['maximumReturnVolatility']:.3f} {r['minimumShortReturn']:+.2f} "
              f"{str(r['requireEquityAboveMovingAverage'])[0]:3s} {str(r['crossSectionalSizing'])[0]:3s} "
              f"{str(tuple(r['allocBand'])):11s} | {r['net']:9,.0f} {r['maxDD']:8,.0f} {r['calmar']:6.2f} "
              f"{r['downFrac']*100:5.1f}% {r['monthsDown']:3d} {r['worstMonth']:8,.0f} {r['nTrades']:4d}")

print(f"\nBASELINE: net {BASE_NET:,.0f}  maxDD {BASE_DD:,.0f}  calmar {BASE_NET/BASE_DD:.2f}  mesi negativi 4/9")

show(sorted(R, key=lambda r: -r['calmar']), 'MIGLIOR CALMAR (net/maxDD)')
show(sorted(R, key=lambda r: r['maxDD']), 'MINOR DRAWDOWN ASSOLUTO')
show(sorted([r for r in R if r['net'] >= 40000], key=lambda r: r['maxDD']), 'MINOR DD con net >= 40k')
show(sorted([r for r in R if r['net'] >= 60000], key=lambda r: r['maxDD']), 'MINOR DD con net >= 60k')
show(sorted(R, key=lambda r: (r['monthsDown'], -r['calmar'])), 'MENO MESI NEGATIVI')
show(sorted(R, key=lambda r: (r['downFrac'], -r['net'])), 'MINOR FRAZIONE GIORNI IN DISCESA')

# quante config battono baseline su ENTRAMBI gli assi
better = [r for r in R if r['net'] > BASE_NET and r['maxDD'] < BASE_DD]
print(f"\nconfig che battono la baseline su net E maxDD: {len(better)} / {len(R)}")
show(sorted(better, key=lambda r: -r['calmar']), 'DOMINANTI sulla baseline', 15)

# nessuna config a zero mesi negativi?
z = [r for r in R if r['monthsDown'] == 0]
print(f"\nconfig con ZERO mesi in discesa: {len(z)}")
show(sorted(z, key=lambda r: -r['net']), 'ZERO MESI NEGATIVI', 15)

print('\n=== sensibilita: calmar mediano per valore di parametro ===')
for k in ['rotationPeriod', 'shortWindowDays', 'longWindowDays', 'movingAverageWindowDays',
          'minimumTrades', 'maximumCurrentDrawdown', 'reenableRatio', 'minimumPassingFilters',
          'cooldownPeriodsAfterOff', 'minimumOnPeriods', 'maximumReturnVolatility',
          'minimumShortReturn', 'requireEquityAboveMovingAverage', 'crossSectionalSizing',
          'dataset']:
    g = collections.defaultdict(list)
    for r in R:
        g[str(r[k])].append(r)
    print(f"\n  {k}:")
    for v, rs in sorted(g.items(), key=lambda kv: -statistics.median(x['calmar'] for x in kv[1])):
        print(f"    {v:>10s}  n={len(rs):5d}  calmar med={statistics.median(x['calmar'] for x in rs):6.2f}  "
              f"net med={statistics.median(x['net'] for x in rs):9,.0f}  DD med={statistics.median(x['maxDD'] for x in rs):9,.0f}")
g = collections.defaultdict(list)
for r in R:
    g[str(tuple(r['allocBand']))].append(r)
print('\n  allocBand:')
for v, rs in sorted(g.items(), key=lambda kv: -statistics.median(x['calmar'] for x in kv[1])):
    print(f"    {v:>12s}  n={len(rs):5d}  calmar med={statistics.median(x['calmar'] for x in rs):6.2f}  "
          f"net med={statistics.median(x['net'] for x in rs):9,.0f}  DD med={statistics.median(x['maxDD'] for x in rs):9,.0f}")
