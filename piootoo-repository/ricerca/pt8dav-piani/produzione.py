"""Mette in produzione, nel broker workspace del loro broker, i quattro best plan PT8DAV.

uso: python produzione.py [--scrivi] [http://localhost:5000]

Senza --scrivi e' una prova (DryRun): il server verifica tutto e dice il piano che nascerebbe.
I conti sono quelli del piano di origine: il demo FTMO 17202911 e il demo Fintokei 4302718.
"""
import json, sys, urllib.request

WRITE = "--scrivi" in sys.argv
SERVER = next((a for a in sys.argv[1:] if a.startswith("http")), "http://localhost:5000")

PLANS = [
    # best plan (workspace__cartella del run), codice di produzione
    ("ftmo-pt8dav__ftmo-pt8dav-p1-bt-20250803-2105-v7.8.13-20261002-0600", "FTMO-PT8-P1"),
    ("ftmo-pt8dav__ftmo-pt8dav-p2-bt-20250803-2105-v7.8.13-20261002-0756", "FTMO-PT8-P2"),
    ("fintokei-pt8dav__fintokei-pt8dav-p1-bt-20251225-2206-v7.8.13-20261002-0604", "FINTOKEI-PT8-P1"),
    ("fintokei-pt8dav__fintokei-pt8dav-p2-bt-20251225-2206-v7.8.13-20261002-0756", "FINTOKEI-PT8-P2"),
]

for best_plan, code in PLANS:
    body = json.dumps({"bestPlanId": best_plan, "planCode": code, "accounts": [], "dryRun": not WRITE}).encode("utf-8")
    request = urllib.request.Request(SERVER + "/api/v1/broker-workspaces/plans", data=body, method="POST",
                                     headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            plan = json.loads(response.read().decode("utf-8-sig"))
        print(f"{'SCRITTO' if WRITE else 'prova'} {plan['code']}: broker {plan['brokerCode']}, conti {plan['accounts']}, "
              f"size {plan['sizeMultiplier']}, commissione {plan['commissionPerContract']}, "
              f"{len(plan.get('enabledStrategies') or [])} strategie, bloccato {plan.get('locked')}")
    except urllib.error.HTTPError as error:
        print(code, "ERRORE", error.code, error.read().decode("utf-8", "replace")[:600])
