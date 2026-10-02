"""Promuove a best plan i quattro run cBot dei piani PT8DAV (02/10/2026).

uso: python promuovi.py [http://localhost:5000]
"""
import json, sys, urllib.request

SERVER = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5000"

COMMON = ("Serie PT8DAV (consegna v5.1 della ricerca esterna). Candidate scelte e piano composto sul "
          "06/2022-05/2025, verificato sul 06/2025-09/2026 che nessuna scelta ha visto "
          "(ricerca/pt8dav-piani/esito.md). Tenuta: overnight e overweek ammessi, flat 20:45 UTC. ")
P1 = ("P1, 8 strategie, size 0,5: FDAX_BRT_001_15, NQ_VBO_001_15, NQ_SBO_003_60, GC_LFD_001_60, "
      "GC_PCH_002_60, ES_RHL_002_60, BP_VBO_001_15, BP_SBO_001_15. E' un piano sull'oro (oltre l'80% del "
      "netto): per questo a mezza size. NON accanto a PT5DAV-P1 su un altro conto della stessa prop "
      "(13-17% di trade gemelli, GC_PCH_002_60 con PT5DAV_GC_PCH_003_60); al limite con O4 / EUROPA-O4 "
      "(4-7%). Compatibile con EUROPA, USA, INDICI e con PT8DAV-P2. ")
P2 = ("P2, 8 strategie, size 1: NQ_VBO_003_240, GC_VBO_001_15, ES_LFH_001_15, ES_MAC_001_30, "
      "FDAX_RHL_002_60, YM_RBU_001_60, BP_PCH_001_60, BP_RBM_001_30. Oro al 42% nel fuori campione. "
      "Compatibile con tutti i best plan esistenti e con PT8DAV-P1 (trade gemelli 0-3%). ")

RUNS = [
    ("ftmo-pt8dav", "ftmo-pt8dav-p1-bt-20250803-2105-v7.8.13-20261002-0600", P1 +
     "Run cBot FTMO 04/08/2025-25/09/2026: 531 trade, +14.607, DD chiuso 3.214, giorno peggiore -540; "
     "interno sullo stesso periodo 524 trade e +14.538, 98% dei trade abbinati."),
    ("ftmo-pt8dav", "ftmo-pt8dav-p2-bt-20250803-2105-v7.8.13-20261002-0756", P2 +
     "Run cBot FTMO 04/08/2025-25/09/2026: 307 trade, +26.858, DD chiuso 2.410, giorno peggiore -1.330, "
     "tutti i mesi in utile; interno 299 trade e +25.545, 96% abbinati."),
    ("fintokei-pt8dav", "fintokei-pt8dav-p1-bt-20251225-2206-v7.8.13-20261002-0604", P1 +
     "Run cBot Fintokei 26/12/2025-01/10/2026: 339 trade, +10.935, DD chiuso 1.222, giorno peggiore -530; "
     "sui trade abbinati con l'interno (feed FTMO) 10.499 contro 11.036."),
    ("fintokei-pt8dav", "fintokei-pt8dav-p2-bt-20251225-2206-v7.8.13-20261002-0756", P2 +
     "Run cBot Fintokei 26/12/2025-29/09/2026: 209 trade, +15.848, DD chiuso 2.458, giorno peggiore -1.641; "
     "sui trade abbinati con l'interno (feed FTMO) 17.712 contro 16.690."),
]

for workspace, folder, text in RUNS:
    body = json.dumps({"workspaceId": workspace, "backtestFolder": folder, "overwrite": False,
                       "description": COMMON + text}).encode("utf-8")
    request = urllib.request.Request(SERVER + "/api/BestPlans", data=body, method="POST",
                                     headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            plan = json.loads(response.read().decode("utf-8-sig"))
            print(f"{plan['planCode']}: promosso  trade {plan['totalTrades']}  netto {plan['netProfit']:,.0f}  "
                  f"DD {plan['maxDrawdown']:,.0f}  ({plan['startUtc'][:10]} -> {plan['endUtc'][:10]})")
    except urllib.error.HTTPError as error:
        print(folder, "ERRORE", error.code, error.read().decode("utf-8", "replace")[:400])
