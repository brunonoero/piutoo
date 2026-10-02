"""Backtest interno con il piano dei quattro piani PT8DAV, dal server.

uso: python backtest_piani.py [inizio AAAA-MM-GG] [fine AAAA-MM-GG] [etichetta] [http://localhost:5000]

Feed FTMO per tutti (quello Fintokei comincia a fine 2025), spread per ora UTC e swap del broker del
piano. Universo, tenuta, commissione e pesi li porta il piano. Un run alla volta.
"""
import json, sys, time, urllib.request

START = sys.argv[1] if len(sys.argv) > 1 else "2025-06-01"
END = sys.argv[2] if len(sys.argv) > 2 else "2026-09-25"
LABEL = sys.argv[3] if len(sys.argv) > 3 else "fuori-campione"
SERVER = sys.argv[4] if len(sys.argv) > 4 else "http://localhost:5000"

PLANS = [
    ("ftmo-pt8dav", "FTMO-PT8DAV-P1", "FTMO"),
    ("ftmo-pt8dav", "FTMO-PT8DAV-P2", "FTMO"),
    ("fintokei-pt8dav", "FINTOKEI-PT8DAV-P1", "FINTOKEI"),
    ("fintokei-pt8dav", "FINTOKEI-PT8DAV-P2", "FINTOKEI"),
]


def call(method, path, body=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    request = urllib.request.Request(SERVER + path, data=data, method=method,
                                     headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            text = response.read().decode("utf-8-sig")
            return response.status, (json.loads(text) if text else None)
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")


for workspace, code, costs in PLANS:
    name = f"bt-{code.lower()}-{LABEL}"
    status, answer = call("POST", "/api/Backtesting/start", {
        "workspaceId": workspace, "planCode": code, "name": name, "backtestFolderName": name,
        "overwriteExistingBacktest": True,
        "startDate": START + "T00:00:00Z", "endDate": END + "T00:00:00Z",
        "initialCapital": 100000,
        "datafeedBroker": "FTMO", "spreadBroker": costs, "swapBroker": costs,
        "spreadStatistic": "Median", "spreadResolution": "PerHour",
        "rejectWrongSideLevels": True,
    })
    if status >= 300:
        print(code, "ERRORE all'avvio:", status, answer)
        continue
    job = answer["jobId"]
    began = time.time()
    while True:
        time.sleep(10)
        _, state = call("GET", f"/api/Backtesting/status/{job}")
        text = str(state.get("status") if isinstance(state, dict) else state)
        if text not in ("Running", "Queued", "Pending", "0", "1"):
            break
    _, summary = call("GET", f"/api/Workspace/{workspace}/backtests/{name}/summary")
    if isinstance(summary, dict):
        print(f"{code}: {text} in {time.time() - began:.0f}s  trade {summary.get('totalTrades')}  "
              f"netto {summary.get('totalNetProfit'):,.0f}  DD {summary.get('maxDrawdown'):.1f}%  "
              f"aperte a fine run {summary.get('openPositionsAtEnd')}")
        for line in summary.get("diagnostics") or []:
            print("    ", line[:260])
    else:
        print(code, text, summary)
