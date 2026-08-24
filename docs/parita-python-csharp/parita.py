#!/usr/bin/env python3
"""
parita.py — confronta i trade di un backtest Piootoo (C#) con il run Python di riferimento.

Cosa confronta, e perche' solo quello
------------------------------------
I riferimenti Python girano su contratti continui BACK-ADJUSTED: NQ vale 4.588 nel 2012
e il datafeed Piootoo ne da' 18.000 sullo stesso strumento. Non e' un errore di nessuno
dei due, e' il retro-aggiustamento dei rollover — ma rende inutile qualunque confronto
sui livelli, e con esso su stop e target espressi in punti.

Restano confrontabili, e sono cio' che questo script guarda:
  * il TIMESTAMP di ingresso     -> quanti ingressi in comune, quanti solo da un lato
  * la DIREZIONE                 -> long/short sullo stesso ingresso
  * il P&L in DOLLARI            -> invariante al back-adjustment
  * la CAUSA di uscita           -> SL / TP / MAXBARS / ...
  * la PRIMA DIVERGENZA          -> il punto da cui i due sistemi smettono di concordare

Uso
---
  # confronto vero: backtest C# contro run Python
  python3 parita.py --python  <cartella consegna/>  \
                    --csharp  <trades.json o .jsonl> \
                    [--offset auto|<minuti>] [--tolleranza <minuti>]

  # autotest: due run Python fra loro (attesa: corrispondenza quasi totale)
  python3 parita.py --python <consegna A/> --python2 <consegna B/>

L'abbinamento fra le classi PTS_* e le famiglie del run Python e' automatico, per
(symbol, timeframe, motore, stop_loss_pt, take_profit_pt) letti da parametri.csv.
"""
import argparse, csv, json, glob, os, sys, collections, datetime as dt

MOTORE_DA_CODICE = {'PCH':'PC','TFM':'TF_M','TFU':'TF_U','SBO':'BO',
                    'RBM':'RBB_M','RBU':'RBB_U','RHL':'RHL','BSW':'BIASW',
                    'VBO':'VBO','LVF':'LF','MAC':'MA','BIA':'BIAS'}
TF_DA_MINUTI = {'1':'1m','5':'5m','15':'15m','30':'30m','60':'1h','240':'4h','1440':'1d'}

# ---------------------------------------------------------------- lettura

def trova_consegne(radice):
    """Tutte le cartelle che contengono un parametri.csv, sotto la radice indicata.
       Cosi' si puo' puntare l'intera run-engine/ e non una consegna alla volta:
       un backtest solo attinge a famiglie che stanno in run diversi."""
    if os.path.exists(os.path.join(radice, 'parametri.csv')):
        return [radice]
    fuori = [os.path.dirname(p) for p in
             glob.glob(os.path.join(radice, '**', 'parametri.csv'), recursive=True)
             if '__MACOSX' not in p]
    return sorted(fuori)

def leggi_python(radici):
    """{chiave: {'motore':..,'params':{..},'trades':[..], 'run':..}} da una o piu' cartelle.
       La chiave e' 'run#famiglia', perche' 'fam1' esiste in ogni run."""
    if isinstance(radici, str): radici = [radici]
    cartelle = []
    for r in radici: cartelle += trova_consegne(r)
    if not cartelle:
        sys.exit(f"nessun parametri.csv sotto {radici}")
    fam = {}
    for cartella in cartelle:
        run = cartella.rstrip('/').replace('/consegna', '')
        run = os.path.basename(os.path.dirname(cartella)) if cartella.endswith('consegna') else os.path.basename(cartella)
        run = '/'.join(cartella.rstrip('/').split('/')[-3:-1]) or cartella
        locali = _leggi_una(cartella)
        for n, v in locali.items():
            v['run'] = run
            fam[f'{run}#{n}'] = v
    return fam

def _leggi_una(cartella):
    par = os.path.join(cartella, 'parametri.csv')
    fam = {}
    for r in csv.DictReader(open(par)):
        fam[r['famiglia']] = {'motore': r['motore'], 'params': r, 'trades': []}
    for f in sorted(glob.glob(os.path.join(cartella, 'trades', 'fam*.csv'))):
        n = os.path.basename(f).split('_')[0].replace('fam', '').lstrip('0') or '0'
        if n not in fam:
            continue
        for r in csv.DictReader(open(f)):
            if not r.get('entry_time'):
                continue
            fam[n]['trades'].append({
                'entry': dt.datetime.fromisoformat(r['entry_time']),
                'exit':  dt.datetime.fromisoformat(r['exit_time']) if r.get('exit_time') else None,
                'side':  'L' if r['side'].upper().startswith('L') else 'S',
                'entry_price': float(r['entry_price']),
                'pnl':   float(r['pnl']),
                'exit_reason': r.get('exit_reason', ''),
                'periodo': r.get('periodo', ''),
            })
        fam[n]['trades'].sort(key=lambda t: t['entry'])
    return fam

def leggi_csharp(percorso):
    """{codice_strategia: [trade]} da un trades.json (array) o .jsonl (una riga per trade)."""
    testo = open(percorso).read().strip()
    righe = json.loads(testo) if testo.startswith('[') else [json.loads(l) for l in testo.splitlines() if l.strip()]
    out = collections.defaultdict(list)
    for t in righe:
        e = dt.datetime.fromisoformat(t['entryTimeUtc'].replace('Z', '+00:00')).replace(tzinfo=None)
        x = t.get('exitTimeUtc')
        out[t['strategyCode']].append({
            'entry': e,
            'exit':  dt.datetime.fromisoformat(x.replace('Z', '+00:00')).replace(tzinfo=None) if x else None,
            'side':  'L' if t['direction'] == 'Buy' else 'S',
            'entry_price': float(t['entryPrice']),
            'pnl':   float(t['netProfit']),
            'exit_reason': t.get('exitReason', ''),
            'qty':   float(t.get('quantity', 1)),
            'sl':    float(t['stopLoss']) if t.get('stopLoss') else 0.0,
            'tp':    float(t['takeProfit']) if t.get('takeProfit') else 0.0,
            'symbol': t['symbol'],
        })
    for k in out:
        out[k].sort(key=lambda t: t['entry'])
    return dict(out)

# ---------------------------------------------------------------- abbinamento

def abbina(cs, fam):
    """PTS_* -> famiglia, per (symbol, timeframe, motore, SL punti, TP punti)."""
    mappa, orfani = {}, []
    for codice, trades in cs.items():
        p = codice.split('_')
        sym, mot, tfm = p[1], MOTORE_DA_CODICE.get(p[2], '?'), p[-1]
        tf = TF_DA_MINUTI.get(tfm, tfm)
        sl, tp = trades[0]['sl'], trades[0]['tp']
        cand = [n for n, f in fam.items()
                if f['params'].get('simbolo') == sym
                and f['params'].get('timeframe') == tf
                and f['motore'] == mot
                and abs(float(f['params'].get('stop_loss_pt') or 0) - sl) < 0.01
                and abs(float(f['params'].get('take_profit_pt') or 0) - tp) < 0.01]
        if len(cand) == 1:
            mappa[codice] = cand[0]
        elif len(cand) > 1:
            mappa[codice] = cand[0]      # ambiguo: si prende il primo e lo si segnala
            orfani.append((codice, f'ambiguo, {len(cand)} famiglie: {cand}'))
        else:
            vicini = sum(1 for f in fam.values()
                         if f['params'].get('simbolo') == sym and f['motore'] == mot)
            orfani.append((codice, f'nessuna corrispondenza ({vicini} righe stesso symbol/motore)'))
    return mappa, orfani

# ---------------------------------------------------------------- confronto

def accoppia(a, b, offset_min, tolleranza_min):
    """Accoppia per timestamp di ingresso, greedy sul piu' vicino entro tolleranza.
       `offset_min` viene SOTTRATTO ai timestamp di b prima del confronto."""
    off = dt.timedelta(minutes=offset_min)
    tol = dt.timedelta(minutes=tolleranza_min)
    usati = set()
    coppie, solo_a = [], []
    j = 0
    for ta in a:
        migliore, dist = None, None
        k = j
        while k < len(b):
            tb = b[k]['entry'] - off
            if tb < ta['entry'] - tol:
                k += 1
                if k - j > 200: j = k - 200
                continue
            if tb > ta['entry'] + tol:
                break
            if k not in usati:
                d = abs(tb - ta['entry'])
                if dist is None or d < dist:
                    migliore, dist = k, d
            k += 1
        if migliore is None:
            solo_a.append(ta)
        else:
            usati.add(migliore)
            coppie.append((ta, b[migliore]))
    solo_b = [b[i] for i in range(len(b)) if i not in usati]
    return coppie, solo_a, solo_b

def cerca_offset(a, b, tolleranza_min):
    """Offset in minuti che massimizza le corrispondenze. Come da porting-da-report-sweep.md,
       dove il massimo si trova a -15 minuti (345 corrispondenze contro 78 a offset nullo)."""
    migliore, quante = 0, -1
    for off in range(-120, 121, 5):
        c, _, _ = accoppia(a, b, off, tolleranza_min)
        if len(c) > quante:
            migliore, quante = off, len(c)
    return migliore, quante

def finestra_comune(a, b):
    if not a or not b: return None
    i = max(a[0]['entry'], b[0]['entry'])
    f = min(a[-1]['entry'], b[-1]['entry'])
    return (i, f) if i <= f else None

def confronta(nome, rif, prova, offset, tolleranza, etichette):
    ea, eb = etichette
    print('=' * 100)
    print(f"{nome}")
    print(f"  {ea:10} {len(rif):5d} trade   {rif[0]['entry']:%Y-%m-%d} -> {rif[-1]['entry']:%Y-%m-%d}"
          if rif else f"  {ea:10} nessun trade")
    print(f"  {eb:10} {len(prova):5d} trade   {prova[0]['entry']:%Y-%m-%d} -> {prova[-1]['entry']:%Y-%m-%d}"
          if prova else f"  {eb:10} nessun trade")
    if not rif or not prova:
        print("  → impossibile confrontare: un lato e' vuoto\n"); return None

    fin = finestra_comune(rif, prova)
    if fin is None:
        print(f"  → NESSUNA SOVRAPPOSIZIONE TEMPORALE. I due run descrivono periodi diversi:")
        print(f"     non c'e' niente da confrontare finche' non si rifa' il backtest sul periodo del riferimento.\n")
        return None
    ini, fine = fin
    giorni = (fine - ini).days
    ra = [t for t in rif   if ini <= t['entry'] <= fine]
    rb = [t for t in prova if ini <= t['entry'] <= fine]
    print(f"  finestra comune: {ini:%Y-%m-%d} -> {fine:%Y-%m-%d}  ({giorni} giorni)"
          f"   {ea} {len(ra)} / {eb} {len(rb)}")

    if offset == 'auto':
        offset, _ = cerca_offset(ra, rb, tolleranza)
        print(f"  offset che massimizza le corrispondenze: {offset:+d} min")
    coppie, solo_a, solo_b = accoppia(ra, rb, offset, tolleranza)

    n = max(1, len(ra))
    print(f"\n  ingressi in comune  {len(coppie):5d}  ({len(coppie)/n*100:5.1f}% del riferimento)")
    print(f"  solo {ea:10}    {len(solo_a):5d}")
    print(f"  solo {eb:10}    {len(solo_b):5d}")

    if coppie:
        disc = [(x, y) for x, y in coppie if x['side'] != y['side']]
        print(f"  direzione discorde  {len(disc):5d}")
        pa = sum(x['pnl'] for x, _ in coppie); pb = sum(y['pnl'] for _, y in coppie)
        print(f"\n  P&L sugli ingressi in comune:  {ea} {pa:12,.0f}   {eb} {pb:12,.0f}"
              f"   scarto {pb-pa:+12,.0f}  ({(pb-pa)/abs(pa)*100 if pa else 0:+.1f}%)")
        pta = sum(t['pnl'] for t in ra); ptb = sum(t['pnl'] for t in rb)
        print(f"  P&L totale nella finestra:     {ea} {pta:12,.0f}   {eb} {ptb:12,.0f}"
              f"   scarto {ptb-pta:+12,.0f}")
        print(f"  P&L dei soli {ea:10}       {sum(t['pnl'] for t in solo_a):12,.0f}")
        print(f"  P&L dei soli {eb:10}       {sum(t['pnl'] for t in solo_b):12,.0f}")

        # prima divergenza: il primo ingresso, da una parte o dall'altra, non accoppiato
        primi = []
        if solo_a: primi.append((solo_a[0]['entry'], ea, solo_a[0]))
        if solo_b: primi.append((solo_b[0]['entry'] - dt.timedelta(minutes=offset), eb, solo_b[0]))
        if primi:
            q, lato, t = min(primi, key=lambda x: x[0])
            print(f"\n  prima divergenza: {q:%Y-%m-%d %H:%M}  ingresso {t['side']} presente solo in {lato}")
            comuni_prima = sum(1 for x, _ in coppie if x['entry'] < q)
            print(f"                    ({comuni_prima} ingressi concordi prima di questo punto)")
        else:
            print("\n  nessuna divergenza: gli insiemi di ingresso coincidono")

        ca = collections.Counter(t['exit_reason'] for t in ra)
        cb = collections.Counter(t['exit_reason'] for t in rb)
        if len(set(cb)) > 1 or len(set(ca)) > 1:
            print(f"\n  cause di uscita   {ea}: {dict(ca)}")
            print(f"                    {eb}: {dict(cb)}")
    print()
    return dict(comuni=len(coppie), solo_a=len(solo_a), solo_b=len(solo_b), offset=offset)

# ---------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--python',  required=True, nargs='+',
                    help="cartella consegna/, oppure una radice (es. run-engine/) da esplorare ricorsivamente")
    ap.add_argument('--python2', nargs='+', help="seconda cartella: autotest Python vs Python")
    ap.add_argument('--csharp',  nargs='+', help="uno o piu' trades.json / trades.jsonl del backtest Piootoo")
    ap.add_argument('--offset', default='auto', help="minuti da sottrarre ai timestamp C# ('auto' per cercarlo)")
    ap.add_argument('--tolleranza', type=int, default=1, help="minuti di tolleranza sull'accoppiamento (default 1)")
    a = ap.parse_args()
    off = a.offset if a.offset == 'auto' else int(a.offset)

    fam = leggi_python(a.python)
    runs = sorted(set(f['run'] for f in fam.values()))
    print(f"riferimento Python: {len(runs)} run — {', '.join(runs)}")
    print(f"  {len(fam)} famiglie, {sum(len(f['trades']) for f in fam.values())} trade\n")

    if a.python2:
        fam2 = leggi_python(a.python2)
        print(f"autotest contro: {', '.join(a.python2)}  ({len(fam2)} famiglie)\n")
        for n in sorted(fam):
            if n in fam2:
                confronta(f"{n} ({fam[n]['motore']})", fam[n]['trades'], fam2[n]['trades'],
                          0 if off == 'auto' else off, a.tolleranza, ('run A', 'run B'))
        return

    if not a.csharp:
        ap.error("serve --csharp oppure --python2")
    cs = {}
    for f in a.csharp:
        for k, v in leggi_csharp(f).items():
            cs.setdefault(k, []).extend(v)
    for k in cs: cs[k].sort(key=lambda t: t['entry'])
    print(f"backtest Piootoo: {', '.join(a.csharp)}")
    print(f"  {len(cs)} strategie, {sum(len(v) for v in cs.values())} trade\n")

    mappa, orfani = abbina(cs, fam)
    print(f"abbinate {len(mappa)} strategie su {len(cs)}")
    for c in sorted(mappa): print(f"  {c:24} -> {mappa[c]}")
    for c, perche in orfani:
        print(f"  non abbinata: {c:24} {perche}")
    print()
    tot = collections.Counter()
    for codice in sorted(mappa):
        r = confronta(f"{codice}  <->  famiglia {mappa[codice]} ({fam[mappa[codice]]['motore']})",
                      fam[mappa[codice]]['trades'], cs[codice], off, a.tolleranza,
                      ('Python', 'Piootoo'))
        if r:
            tot['comuni'] += r['comuni']; tot['solo_py'] += r['solo_a']; tot['solo_cs'] += r['solo_b']
    if tot:
        print('=' * 100)
        print(f"TOTALE  ingressi in comune {tot['comuni']}   solo Python {tot['solo_py']}   solo Piootoo {tot['solo_cs']}")

if __name__ == '__main__':
    main()
