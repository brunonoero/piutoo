"""Crea sul server i workspace e i piani PT8DAV composti in piani-ftmo-A (piani 1 e 2).

uso: python crea_piani.py [http://localhost:5000]

Un workspace per broker con le 16 strategie dei due piani nel masterfilter; ogni piano spegne le 8
dell'altro. Tenuta e flat dei piani PT5DAV. Il piano 3 non si crea: non tiene fuori campione
(esito.md). Rilanciarlo riscrive masterfilter e piani con gli stessi valori.
"""
import json, os, sys, urllib.request

SERVER = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5000"
HERE = os.path.dirname(os.path.abspath(__file__))

raw = json.load(open(os.path.join(HERE, "piani-ftmo-A", "plans.json"), encoding="utf-8-sig"))
plans = raw if isinstance(raw, list) else (raw.get("plans") or raw.get("Plans"))


def members(plan):
    items = plan.get("strategies") or plan.get("Strategies") or plan.get("members") or plan.get("Members")
    return [x if isinstance(x, str) else (x.get("strategy") or x.get("Strategy") or x.get("code") or x.get("Code")) for x in items]


P1, P2 = members(plans[0]), members(plans[1])
ALL = sorted(P1 + P2)
assert len(P1) == 8 and len(P2) == 8 and len(set(ALL)) == 16


def call(method, path, body=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    request = urllib.request.Request(SERVER + path, data=data, method=method,
                                     headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            text = response.read().decode("utf-8-sig")
            return response.status, (json.loads(text) if text else None)
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")


HOLDING = {"allowOvernight": True, "allowOverweek": True, "sessionFlatUtc": "20:45:00",
           "sessionFlatWindowMinutes": 30, "weekEnd": {"fromUtc": "20:45:00", "untilUtc": "23:00:00"}}

BROKERS = [
    # workspace, prefisso dei codici, broker, conto, commissione
    ("ftmo-pt8dav", "FTMO-PT8DAV", "FTMO", "17202911", 2),
    ("fintokei-pt8dav", "FINTOKEI-PT8DAV", "FINTOKEI", "4302718", 0),
]

existing = {w.get("id") or w.get("Id") for w in call("GET", "/api/Workspace")[1]}
for workspace, prefix, broker, account, commission in BROKERS:
    if workspace not in existing:
        status, answer = call("POST", "/api/Workspace", {"name": workspace, "strategiesFilter": ALL})
        print(workspace, "creato:", status, answer if status >= 300 else "")
    status, answer = call("PUT", f"/api/Workspace/{workspace}/masterfilter", {"name": workspace, "strategiesFilter": ALL})
    print(workspace, "masterfilter:", status, len(answer["strategiesFilter"]) if status < 300 else answer)

    for code, label, mine, others, size in (
            (f"{prefix}-P1", "piano 1, 8 strategie, oro all'83% nel fuori campione, size 0,5", P1, P2, 0.5),
            (f"{prefix}-P2", "piano 2, 8 strategie, size 1", P2, P1, 1)):
        plan = {
            "workspaceId": workspace, "code": code, "name": f"{code.lower()} ({label})",
            "brokerCode": broker, "accounts": [account], "accountNumber": account,
            "maxConcurrentTrades": 0, "sizeMultiplier": size, "commissionPerContract": commission,
            "holding": HOLDING, "disabledStrategies": sorted(others), "strategyWeights": {},
        }
        status, answer = call("PUT", f"/api/v1/workspaces/{workspace}/trading-plans/{code}", plan)
        if status < 300:
            print(" ", code, "salvato: size", answer["sizeMultiplier"], "commissione", answer["commissionPerContract"],
                  "spente", len(answer["disabledStrategies"]), "attive", ", ".join(s.replace("PT8DAV_", "") for s in sorted(mine)))
        else:
            print(" ", code, "ERRORE", status, answer)
