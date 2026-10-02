// Generato da aggiorna-dati.py: non modificare a mano.
window.PIOOTOO_PIANI = {
 "generatedUtc": "2026-10-02T08:43:11Z",
 "criterion": "Due piani stanno su conti diversi dello stesso broker solo se non hanno strategie in comune e se meno del 5% dei trade ha un gemello nell'altro piano (stesso simbolo, stesso lato, ingresso entro 5 minuti, misurato sui run cBot).",
 "brokers": [
  {
   "code": "FTMO",
   "name": "FTMO",
   "enabled": true,
   "accounts": [
    {
     "number": "17202911",
     "name": "ftmo demo",
     "currency": "USD",
     "balance": 100000,
     "enabled": true
    }
   ],
   "rules": "Conto da 100.000: perdita giornaliera massima 5.000, perdita totale massima 10.000.",
   "notes": [
    "I tre piani in produzione girano tutti sul conto demo 17202911: lì la sovrapposizione fra EUROPA e O4-X05 è concentrazione sulla stessa rottura del DAX, non copy trading. Su due conti separati di FTMO, EUROPA e O4-X05 non vanno insieme.",
    "I quattro piani PT8DAV sono best plan dal 02/10/2026: il passaggio in produzione e il demo sono ancora da fare."
   ],
   "combinations": [
    {
     "conti": 3,
     "piani": [
      "FTMO-PT3B-EUROPA",
      "FTMO-PT3B-USA",
      "PT5DAV-P1"
     ],
     "nota": "L'unica a tre conti fra i piani PT3B e PT5DAV. P1 è in campione: da demo prima."
    },
    {
     "conti": 2,
     "piani": [
      "FTMO-PT3B-INDICI",
      "PT5DAV-P1"
     ],
     "nota": "INDICI = EUROPA + USA su un conto. Stessa riserva su P1."
    },
    {
     "conti": 2,
     "piani": [
      "FTMO-EUROPA-O4",
      "FTMO-PT3B-USA"
     ],
     "nota": "Senza P1, per chi non vuole un piano in campione su un conto vero."
    },
    {
     "conti": 2,
     "piani": [
      "PT5DAV-O4-X05",
      "FTMO-PT3B-USA"
     ],
     "nota": "Senza EUROPA."
    },
    {
     "conti": 1,
     "piani": [
      "FTMO-PT8DAV-P2"
     ],
     "nota": "Un conto in più accanto a qualsiasi combinazione: P2 è compatibile con tutti gli altri best plan."
    },
    {
     "conti": 1,
     "piani": [
      "FTMO-PT8DAV-P1"
     ],
     "nota": "Un conto in più solo accanto a EUROPA, USA, INDICI e PT8DAV-P2. Non con PT5DAV-P1; al limite con O4, O4-X05 ed EUROPA-O4."
    }
   ],
   "plans": [
    {
     "code": "FTMO-EUROPA",
     "name": "ftmo-pt3b-europa (002 + RHL DAX + RHL EuroStoxx)",
     "broker": "FTMO",
     "state": "produzione",
     "strategies": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240"
     ],
     "promotedUtc": "2026-09-28T20:10:53.0968258Z",
     "retiredUtc": null,
     "origin": {
      "planCode": "FTMO-PT3B-EUROPA",
      "workspace": "ftmo-pt3b-indici",
      "bestPlanId": "ftmo-pt3b-indici__ftmo-pt3b-europa-bt-20250623-2105-v7.8.6-20260928-0513",
      "backtestFolder": "ftmo-pt3b-europa-bt-20250623-2105-v7.8.6-20260928-0513",
      "previousPlanCode": null
     },
     "accounts": [
      "17202911"
     ],
     "size": 1,
     "commission": 2,
     "overnight": false,
     "overweek": false,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "measureCode": "FTMO-PT3B-EUROPA",
     "run": {
      "startUtc": "2025-06-23T21:05:00Z",
      "endUtc": "2026-08-31T19:00:00Z",
      "trades": 363,
      "netProfit": 10073.05,
      "maxDrawdown": 4094.27,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "note": "Indici europei, intraday (niente overnight)."
    },
    {
     "code": "FTMO-O4-X05",
     "name": "pt5dav-o4 size 0,5 (versione da conto)",
     "broker": "FTMO",
     "state": "produzione",
     "strategies": [
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ],
     "promotedUtc": "2026-09-28T20:10:53.1949161Z",
     "retiredUtc": null,
     "origin": {
      "planCode": "PT5DAV-O4-X05",
      "workspace": "ftmo-pt5dav-onesti",
      "bestPlanId": "ftmo-pt5dav-onesti__pt5dav-o4-x05-bt-20250812-0000-v7.8.6-20260928-0527",
      "backtestFolder": "pt5dav-o4-x05-bt-20250812-0000-v7.8.6-20260928-0527",
      "previousPlanCode": null
     },
     "accounts": [
      "17202911"
     ],
     "size": 0.5,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "measureCode": "PT5DAV-O4-X05",
     "run": {
      "startUtc": "2025-08-12T00:00:00Z",
      "endUtc": "2026-09-25T19:30:00Z",
      "trades": 386,
      "netProfit": 12117.79,
      "maxDrawdown": 4109.19,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "note": "O4 a size 0,5, la versione da conto. NQ_BSW_001_240 fa gran parte del netto."
    },
    {
     "code": "FTMO-USA",
     "name": "ftmo-pt3b-usa (RHL Nasdaq, S&P, Dow)",
     "broker": "FTMO",
     "state": "produzione",
     "strategies": [
      "PT3B_ES_RHL_001_240",
      "PT3B_NQ_RHL_001_240",
      "PT3B_YM_RHL_001_240"
     ],
     "promotedUtc": "2026-09-28T20:10:53.1511581Z",
     "retiredUtc": null,
     "origin": {
      "planCode": "FTMO-PT3B-USA",
      "workspace": "ftmo-pt3b-indici",
      "bestPlanId": "ftmo-pt3b-indici__ftmo-pt3b-usa-bt-20250623-2105-v7.8.6-20260928-0525",
      "backtestFolder": "ftmo-pt3b-usa-bt-20250623-2105-v7.8.6-20260928-0525",
      "previousPlanCode": null
     },
     "accounts": [
      "17202911"
     ],
     "size": 1,
     "commission": 2,
     "overnight": false,
     "overweek": false,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "measureCode": "FTMO-PT3B-USA",
     "run": {
      "startUtc": "2025-06-23T21:05:00Z",
      "endUtc": "2026-08-31T18:00:00Z",
      "trades": 19,
      "netProfit": 5063.98,
      "maxDrawdown": 1029.56,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "note": "Pochi trade (77 in quattro anni): piano di diversificazione, non di volume."
    },
    {
     "code": "FTMO-EUROPA-O4",
     "name": "ftmo-europa-o4 (EUROPA + O4 a meta', size 0,75, un conto)",
     "broker": "FTMO",
     "state": "best-plan",
     "strategies": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240",
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ],
     "promotedUtc": "2026-09-28T10:24:33.1141528Z",
     "origin": {
      "planCode": "FTMO-EUROPA-O4",
      "workspace": "ftmo-combo-europa-o4",
      "bestPlanId": "ftmo-combo-europa-o4__ftmo-europa-o4-bt-20250801-0000-v7.8.6-20260928-0956",
      "backtestFolder": "ftmo-europa-o4-bt-20250801-0000-v7.8.6-20260928-0956"
     },
     "run": {
      "startUtc": "2025-08-01T00:00:00Z",
      "endUtc": "2026-09-25T19:30:00Z",
      "trades": 748,
      "netProfit": 15679.19,
      "maxDrawdown": 4007.58,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "17202911"
     ],
     "size": 0.75,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {
      "PT5DAV_FDAX_BOS_001_15": 0.5,
      "PT5DAV_FDAX_PCH_002_60": 0.5,
      "PT5DAV_GC_LFD_002_60": 0.5,
      "PT5DAV_KC_LFH_001_60": 0.5,
      "PT5DAV_NQ_BSW_001_240": 0.5,
      "PT5DAV_NQ_LFD_004_240": 0.5,
      "PT5DAV_YM_LFH_003_240": 0.5
     },
     "olderCards": 1,
     "measureCode": "FTMO-EUROPA-O4",
     "note": "EUROPA + O4 a metà su un conto solo, size 0,75. Alternativa a EUROPA e O4-X05 separati, mai insieme a loro."
    },
    {
     "code": "FTMO-EUROPA-O4-B",
     "name": "ftmo-europa-o4 (EUROPA + O4 a meta', size 0,75, un conto) (copia)",
     "broker": "FTMO",
     "state": "best-plan",
     "strategies": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240",
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ],
     "promotedUtc": "2026-09-29T08:47:04.1370098Z",
     "origin": {
      "planCode": "FTMO-EUROPA-O4-B",
      "workspace": "ftmo-combo-europa-o4",
      "bestPlanId": "ftmo-combo-europa-o4__ftmo-europa-o4-b-bt-20250801-0000-v7.8.9-20260929-0756",
      "backtestFolder": "ftmo-europa-o4-b-bt-20250801-0000-v7.8.9-20260929-0756"
     },
     "run": {
      "startUtc": "2025-08-01T00:00:00Z",
      "endUtc": "2026-09-25T19:30:00Z",
      "trades": 748,
      "netProfit": 15679.19,
      "maxDrawdown": 4007.58,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "17202911"
     ],
     "size": 0.75,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {
      "PT5DAV_FDAX_BOS_001_15": 0.5,
      "PT5DAV_FDAX_PCH_002_60": 0.5,
      "PT5DAV_GC_LFD_002_60": 0.5,
      "PT5DAV_KC_LFH_001_60": 0.5,
      "PT5DAV_NQ_BSW_001_240": 0.5,
      "PT5DAV_NQ_LFD_004_240": 0.5,
      "PT5DAV_YM_LFH_003_240": 0.5
     },
     "olderCards": 1,
     "measureCode": "FTMO-EUROPA-O4-B",
     "note": "Copia di FTMO-EUROPA-O4: resta di ricerca, come termine di paragone."
    },
    {
     "code": "FTMO-PT3B-INDICI",
     "name": "ftmo-pt3b-indici (002 + RHL su 5 indici)",
     "broker": "FTMO",
     "state": "best-plan",
     "strategies": [
      "PT3B_ES_RHL_001_240",
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240",
      "PT3B_NQ_RHL_001_240",
      "PT3B_YM_RHL_001_240"
     ],
     "promotedUtc": "2026-09-27T11:29:05.1256644Z",
     "origin": {
      "planCode": "FTMO-PT3B-INDICI",
      "workspace": "ftmo-pt3b-indici",
      "bestPlanId": "ftmo-pt3b-indici__ftmo-pt3b-indici-bt-20250801-0000-v7.8.4-20260927-1052",
      "backtestFolder": "ftmo-pt3b-indici-bt-20250801-0000-v7.8.4-20260927-1052"
     },
     "run": {
      "startUtc": "2025-08-01T00:00:00Z",
      "endUtc": "2026-09-24T19:00:00Z",
      "trades": 372,
      "netProfit": 14603.77,
      "maxDrawdown": 2865.83,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "17202911"
     ],
     "size": 1,
     "commission": 2,
     "overnight": false,
     "overweek": false,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "olderCards": 0,
     "measureCode": "FTMO-PT3B-INDICI",
     "note": "EUROPA + USA su un conto solo. Resta di ricerca finché FTMO-EUROPA e FTMO-USA sono in produzione."
    },
    {
     "code": "FTMO-PT8DAV-P1",
     "name": "ftmo-pt8dav-p1 (piano 1, 8 strategie, oro all'83% nel fuori campione, size 0,5)",
     "broker": "FTMO",
     "state": "best-plan",
     "strategies": [
      "PT8DAV_BP_SBO_001_15",
      "PT8DAV_BP_VBO_001_15",
      "PT8DAV_ES_RHL_002_60",
      "PT8DAV_FDAX_BRT_001_15",
      "PT8DAV_GC_LFD_001_60",
      "PT8DAV_GC_PCH_002_60",
      "PT8DAV_NQ_SBO_003_60",
      "PT8DAV_NQ_VBO_001_15"
     ],
     "promotedUtc": "2026-10-02T08:24:55.4494089Z",
     "origin": {
      "planCode": "FTMO-PT8DAV-P1",
      "workspace": "ftmo-pt8dav",
      "bestPlanId": "ftmo-pt8dav__ftmo-pt8dav-p1-bt-20250803-2105-v7.8.13-20261002-0600",
      "backtestFolder": "ftmo-pt8dav-p1-bt-20250803-2105-v7.8.13-20261002-0600"
     },
     "run": {
      "startUtc": "2025-08-03T21:05:00Z",
      "endUtc": "2026-09-25T20:30:00Z",
      "trades": 531,
      "netProfit": 14606.58,
      "maxDrawdown": 3242.76,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "17202911"
     ],
     "size": 0.5,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "olderCards": 0,
     "measureCode": "FTMO-PT8DAV-P1",
     "note": "Piano sull'oro: le due GC fanno l'83% del netto nel fuori campione. Per questo va a size 0,5."
    },
    {
     "code": "FTMO-PT8DAV-P2",
     "name": "ftmo-pt8dav-p2 (piano 2, 8 strategie, size 1)",
     "broker": "FTMO",
     "state": "best-plan",
     "strategies": [
      "PT8DAV_BP_PCH_001_60",
      "PT8DAV_BP_RBM_001_30",
      "PT8DAV_ES_LFH_001_15",
      "PT8DAV_ES_MAC_001_30",
      "PT8DAV_FDAX_RHL_002_60",
      "PT8DAV_GC_VBO_001_15",
      "PT8DAV_NQ_VBO_003_240",
      "PT8DAV_YM_RBU_001_60"
     ],
     "promotedUtc": "2026-10-02T08:24:55.6393323Z",
     "origin": {
      "planCode": "FTMO-PT8DAV-P2",
      "workspace": "ftmo-pt8dav",
      "bestPlanId": "ftmo-pt8dav__ftmo-pt8dav-p2-bt-20250803-2105-v7.8.13-20261002-0756",
      "backtestFolder": "ftmo-pt8dav-p2-bt-20250803-2105-v7.8.13-20261002-0756"
     },
     "run": {
      "startUtc": "2025-08-03T21:05:00Z",
      "endUtc": "2026-09-25T20:30:00Z",
      "trades": 307,
      "netProfit": 26858.08,
      "maxDrawdown": 2409.69,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "17202911"
     ],
     "size": 1,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "olderCards": 0,
     "measureCode": "FTMO-PT8DAV-P2",
     "note": "Il più equilibrato dei PT8DAV: tutti i mesi del run cBot in utile."
    },
    {
     "code": "PT5DAV-O4",
     "name": "pt5dav-onesto-4",
     "broker": "FTMO",
     "state": "best-plan",
     "strategies": [
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ],
     "promotedUtc": "2026-09-27T15:09:27.8446761Z",
     "origin": {
      "planCode": "PT5DAV-O4",
      "workspace": "ftmo-pt5dav-onesti",
      "bestPlanId": "ftmo-pt5dav-onesti__pt5dav-o4-bt-20250623-2105-v7.8.3-20260927-0803",
      "backtestFolder": "pt5dav-o4-bt-20250623-2105-v7.8.3-20260927-0803"
     },
     "run": {
      "startUtc": "2025-06-23T21:05:00Z",
      "endUtc": "2026-08-31T22:00:00Z",
      "trades": 401,
      "netProfit": 24328.54,
      "maxDrawdown": 8382.36,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "17202911"
     ],
     "size": 1,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "olderCards": 0,
     "measureCode": "PT5DAV-O4",
     "note": "Non su un conto vero a size 1: peggior giorno FTMO 5.029 contro il limite di 5.000. Si usa O4-X05."
    },
    {
     "code": "PT5DAV-P1",
     "name": "pt5dav-piano-1-scorrelato",
     "broker": "FTMO",
     "state": "best-plan",
     "strategies": [
      "PT5DAV_ES_RBM_001_30",
      "PT5DAV_ES_RHL_001_30",
      "PT5DAV_GC_BOS_002_240",
      "PT5DAV_GC_PCH_003_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_RHL_001_30"
     ],
     "promotedUtc": "2026-09-26T07:23:11.5577061Z",
     "origin": {
      "planCode": "PT5DAV-P1",
      "workspace": "ftmo-pt5dav-41",
      "bestPlanId": "ftmo-pt5dav-41__pt5dav-p1-bt-20250901-0000-v7.8.0-20260926-0642",
      "backtestFolder": "pt5dav-p1-bt-20250901-0000-v7.8.0-20260926-0642"
     },
     "run": {
      "startUtc": "2025-09-01T00:00:00Z",
      "endUtc": "2026-09-24T23:00:00Z",
      "trades": 433,
      "netProfit": 81699.13,
      "maxDrawdown": 3965.29,
      "priceSource": "CFD FTMO (datafeed-external/FTMO)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "17202911"
     ],
     "size": 1,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "olderCards": 0,
     "measureCode": "PT5DAV-P1",
     "note": "In campione: composto sullo stesso periodo del run. Prima un demo, non un conto vero."
    }
   ],
   "pairs": {
    "FTMO-EUROPA|FTMO-O4-X05": {
     "verdict": "no",
     "reason": "Nessuna strategia in comune, ma trade gemelli 18%.",
     "twins": "18%",
     "measured": "2026-09-29",
     "note": "Breakout del DAX: PT3B_FDAX_PCH_002_240 scatta sulle stesse rotture di PT5DAV_FDAX_BOS_001_15 e PT5DAV_FDAX_PCH_002_60.",
     "measuredOn": "FTMO-PT3B-EUROPA × PT5DAV-O4"
    },
    "FTMO-EUROPA|FTMO-USA": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29",
     "measuredOn": "FTMO-PT3B-EUROPA × FTMO-PT3B-USA"
    },
    "FTMO-EUROPA|FTMO-EUROPA-O4": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240"
     ]
    },
    "FTMO-EUROPA|FTMO-EUROPA-O4-B": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240"
     ]
    },
    "FTMO-EUROPA|FTMO-PT3B-INDICI": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240"
     ]
    },
    "FTMO-EUROPA|FTMO-PT8DAV-P1": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P1 × FTMO-PT3B-EUROPA"
    },
    "FTMO-EUROPA|FTMO-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0-1%.",
     "twins": "0-1%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P2 × FTMO-PT3B-EUROPA"
    },
    "FTMO-EUROPA|PT5DAV-O4": {
     "verdict": "no",
     "reason": "Nessuna strategia in comune, ma trade gemelli 18%.",
     "twins": "18%",
     "measured": "2026-09-29",
     "note": "Breakout del DAX: PT3B_FDAX_PCH_002_240 scatta sulle stesse rotture di PT5DAV_FDAX_BOS_001_15 e PT5DAV_FDAX_PCH_002_60.",
     "measuredOn": "FTMO-PT3B-EUROPA × PT5DAV-O4"
    },
    "FTMO-EUROPA|PT5DAV-P1": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29",
     "measuredOn": "FTMO-PT3B-EUROPA × PT5DAV-P1"
    },
    "FTMO-O4-X05|FTMO-USA": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29",
     "measuredOn": "FTMO-PT3B-USA × PT5DAV-O4"
    },
    "FTMO-O4-X05|FTMO-EUROPA-O4": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ]
    },
    "FTMO-O4-X05|FTMO-EUROPA-O4-B": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ]
    },
    "FTMO-O4-X05|FTMO-PT3B-INDICI": {
     "verdict": "no",
     "reason": "Nessuna strategia in comune, ma trade gemelli 20%.",
     "twins": "20%",
     "measured": "2026-09-29",
     "note": "Breakout del DAX: PT3B_FDAX_PCH_002_240 scatta sulle stesse rotture di PT5DAV_FDAX_BOS_001_15 e PT5DAV_FDAX_PCH_002_60.",
     "measuredOn": "FTMO-PT3B-INDICI × PT5DAV-O4"
    },
    "FTMO-O4-X05|FTMO-PT8DAV-P1": {
     "verdict": "limite",
     "reason": "Nessuna strategia in comune, ma trade gemelli 4-7%: al limite del 5%.",
     "twins": "4-7%",
     "measured": "2026-10-02",
     "note": "PT8DAV_GC_LFD_001_60 apre gli stessi trade di PT5DAV_GC_LFD_002_60.",
     "measuredOn": "FTMO-PT8DAV-P1 × PT5DAV-O4"
    },
    "FTMO-O4-X05|FTMO-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 1-2%.",
     "twins": "1-2%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P2 × PT5DAV-O4"
    },
    "FTMO-O4-X05|PT5DAV-O4": {
     "verdict": "stesso",
     "reason": "Stesse strategie: è lo stesso piano, mai su due conti.",
     "shared": [
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ]
    },
    "FTMO-O4-X05|PT5DAV-P1": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_NQ_BSW_001_240"
     ]
    },
    "FTMO-USA|FTMO-EUROPA-O4": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29",
     "measuredOn": "FTMO-PT3B-USA × FTMO-EUROPA-O4"
    },
    "FTMO-USA|FTMO-EUROPA-O4-B": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29",
     "measuredOn": "FTMO-PT3B-USA × FTMO-EUROPA-O4"
    },
    "FTMO-USA|FTMO-PT3B-INDICI": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT3B_ES_RHL_001_240",
      "PT3B_NQ_RHL_001_240",
      "PT3B_YM_RHL_001_240"
     ]
    },
    "FTMO-USA|FTMO-PT8DAV-P1": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P1 × FTMO-PT3B-USA"
    },
    "FTMO-USA|FTMO-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0-1%.",
     "twins": "0-1%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P2 × FTMO-PT3B-USA"
    },
    "FTMO-USA|PT5DAV-O4": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29",
     "measuredOn": "FTMO-PT3B-USA × PT5DAV-O4"
    },
    "FTMO-USA|PT5DAV-P1": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29",
     "note": "NQ_RHL_001_30 (P1) e NQ_RHL_001_240 (USA) sono strategie diverse ma comprano gli stessi ritorni sul Nasdaq.",
     "measuredOn": "FTMO-PT3B-USA × PT5DAV-P1"
    },
    "FTMO-EUROPA-O4|FTMO-EUROPA-O4-B": {
     "verdict": "stesso",
     "reason": "Stesse strategie: è lo stesso piano, mai su due conti.",
     "shared": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240",
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ]
    },
    "FTMO-EUROPA-O4|FTMO-PT3B-INDICI": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240"
     ]
    },
    "FTMO-EUROPA-O4|FTMO-PT8DAV-P1": {
     "verdict": "limite",
     "reason": "Nessuna strategia in comune, ma trade gemelli 4-7%: al limite del 5%.",
     "twins": "4-7%",
     "measured": "2026-10-02",
     "note": "PT8DAV_GC_LFD_001_60 apre gli stessi trade di PT5DAV_GC_LFD_002_60."
    },
    "FTMO-EUROPA-O4|FTMO-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 1-2%.",
     "twins": "1-2%",
     "measured": "2026-10-02"
    },
    "FTMO-EUROPA-O4|PT5DAV-O4": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ]
    },
    "FTMO-EUROPA-O4|PT5DAV-P1": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_NQ_BSW_001_240"
     ]
    },
    "FTMO-EUROPA-O4-B|FTMO-PT3B-INDICI": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240"
     ]
    },
    "FTMO-EUROPA-O4-B|FTMO-PT8DAV-P1": {
     "verdict": "limite",
     "reason": "Nessuna strategia in comune, ma trade gemelli 4-7%: al limite del 5%.",
     "twins": "4-7%",
     "measured": "2026-10-02",
     "note": "PT8DAV_GC_LFD_001_60 apre gli stessi trade di PT5DAV_GC_LFD_002_60.",
     "measuredOn": "FTMO-PT8DAV-P1 × FTMO-EUROPA-O4"
    },
    "FTMO-EUROPA-O4-B|FTMO-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 1-2%.",
     "twins": "1-2%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P2 × FTMO-EUROPA-O4"
    },
    "FTMO-EUROPA-O4-B|PT5DAV-O4": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_KC_LFH_001_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ]
    },
    "FTMO-EUROPA-O4-B|PT5DAV-P1": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_NQ_BSW_001_240"
     ]
    },
    "FTMO-PT3B-INDICI|FTMO-PT8DAV-P1": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-10-02"
    },
    "FTMO-PT3B-INDICI|FTMO-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0-1%.",
     "twins": "0-1%",
     "measured": "2026-10-02"
    },
    "FTMO-PT3B-INDICI|PT5DAV-O4": {
     "verdict": "no",
     "reason": "Nessuna strategia in comune, ma trade gemelli 20%.",
     "twins": "20%",
     "measured": "2026-09-29",
     "note": "Breakout del DAX: PT3B_FDAX_PCH_002_240 scatta sulle stesse rotture di PT5DAV_FDAX_BOS_001_15 e PT5DAV_FDAX_PCH_002_60."
    },
    "FTMO-PT3B-INDICI|PT5DAV-P1": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 0%.",
     "twins": "0%",
     "measured": "2026-09-29"
    },
    "FTMO-PT8DAV-P1|FTMO-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 2-3%.",
     "twins": "2-3%",
     "measured": "2026-10-02"
    },
    "FTMO-PT8DAV-P1|PT5DAV-O4": {
     "verdict": "limite",
     "reason": "Nessuna strategia in comune, ma trade gemelli 4-7%: al limite del 5%.",
     "twins": "4-7%",
     "measured": "2026-10-02",
     "note": "PT8DAV_GC_LFD_001_60 apre gli stessi trade di PT5DAV_GC_LFD_002_60."
    },
    "FTMO-PT8DAV-P1|PT5DAV-P1": {
     "verdict": "no",
     "reason": "Nessuna strategia in comune, ma trade gemelli 13-17%.",
     "twins": "13-17%",
     "measured": "2026-10-02",
     "note": "Le due PCH dell'oro aprono gli stessi trade: PT8DAV_GC_PCH_002_60 e PT5DAV_GC_PCH_003_60."
    },
    "FTMO-PT8DAV-P2|PT5DAV-O4": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 1-2%.",
     "twins": "1-2%",
     "measured": "2026-10-02"
    },
    "FTMO-PT8DAV-P2|PT5DAV-P1": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 1-2%.",
     "twins": "1-2%",
     "measured": "2026-10-02"
    },
    "PT5DAV-O4|PT5DAV-P1": {
     "verdict": "no",
     "reason": "Strategie in comune.",
     "shared": [
      "PT5DAV_NQ_BSW_001_240"
     ]
    }
   }
  },
  {
   "code": "FINTOKEI",
   "name": "Fintokei",
   "enabled": true,
   "accounts": [
    {
     "number": "4302718",
     "name": "fintokei-4302718",
     "currency": "USD",
     "balance": 100000,
     "enabled": true
    }
   ],
   "rules": "Conto da 100.000 USD: perdita giornaliera 5% sull'equity, perdita massima 10%. Commissione 0.",
   "notes": [
    "Nessun piano è ancora in produzione su Fintokei: va scelta la combinazione da promuovere nel broker workspace (2 o 3 conti).",
    "Le strategie sono quelle di FTMO e valgono le stesse compatibilità: le quote di trade gemelli sono misurate sui run dei piani FTMO con le stesse strategie.",
    "Da chiarire (02/10/2026): il registro dei piani dà come best plan dal 01/10/2026 anche FINTOKEI-USA, FINTOKEI-USA-2 e FINTOKEI-P1, ma nella cartella best-plans non ci sono e i tre piani non risultano bloccati. Finché non tornano best plan, qui non compaiono."
   ],
   "combinations": [
    {
     "conti": 2,
     "piani": [
      "FINTOKEI-EUROPA-O4",
      "FINTOKEI-USA-2"
     ],
     "nota": "USA-2 non è (ancora) fra i best plan su disco."
    },
    {
     "conti": 3,
     "piani": [
      "FINTOKEI-EUROPA",
      "FINTOKEI-USA",
      "FINTOKEI-P1"
     ],
     "nota": "Si usa USA e non USA-2: USA-2 e P1 hanno il 7% di ingressi gemelli. Nessuno dei tre è fra i best plan su disco."
    },
    {
     "conti": 1,
     "piani": [
      "FINTOKEI-PT8DAV-P2"
     ],
     "nota": "Compatibile con tutti gli altri piani, come su FTMO."
    }
   ],
   "plans": [
    {
     "code": "FINTOKEI-EUROPA-O4",
     "name": "fintokei-europa-o4 (EUROPA + O4 a meta', senza KC_LFH, size 0,75)",
     "broker": "FINTOKEI",
     "state": "best-plan",
     "strategies": [
      "PT3B_FDAX_PCH_002_240",
      "PT3B_FDAX_RHL_001_240",
      "PT3B_FESX_RHL_001_240",
      "PT5DAV_FDAX_BOS_001_15",
      "PT5DAV_FDAX_PCH_002_60",
      "PT5DAV_GC_LFD_002_60",
      "PT5DAV_NQ_BSW_001_240",
      "PT5DAV_NQ_LFD_004_240",
      "PT5DAV_YM_LFH_003_240"
     ],
     "promotedUtc": "2026-10-01T06:16:51.2658215Z",
     "origin": {
      "planCode": "FINTOKEI-EUROPA-O4",
      "workspace": "fintokei-europa-o4",
      "bestPlanId": "fintokei-europa-o4__fintokei-europa-o4-bt-20251225-2206-v7.8.12-20261001-0555",
      "backtestFolder": "fintokei-europa-o4-bt-20251225-2206-v7.8.12-20261001-0555"
     },
     "run": {
      "startUtc": "2025-12-25T22:06:00Z",
      "endUtc": "2026-08-31T22:00:00Z",
      "trades": 454,
      "netProfit": 11214.18,
      "maxDrawdown": 5210.18,
      "priceSource": "CFD FINTOKEI (datafeed-external/FINTOKEI)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "4302718"
     ],
     "size": 0.75,
     "commission": 2,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {
      "PT5DAV_FDAX_BOS_001_15": 0.5,
      "PT5DAV_FDAX_PCH_002_60": 0.5,
      "PT5DAV_GC_LFD_002_60": 0.5,
      "PT5DAV_NQ_BSW_001_240": 0.5,
      "PT5DAV_NQ_LFD_004_240": 0.5,
      "PT5DAV_YM_LFH_003_240": 0.5
     },
     "olderCards": 0,
     "measureCode": "FINTOKEI-EUROPA-O4",
     "note": "Gemello di FTMO-EUROPA-O4 senza PT5DAV_KC_LFH_001_60: Fintokei non quota il caffè."
    },
    {
     "code": "FINTOKEI-PT8DAV-P1",
     "name": "fintokei-pt8dav-p1 (piano 1, 8 strategie, oro all'83% nel fuori campione, size 0,5)",
     "broker": "FINTOKEI",
     "state": "best-plan",
     "strategies": [
      "PT8DAV_BP_SBO_001_15",
      "PT8DAV_BP_VBO_001_15",
      "PT8DAV_ES_RHL_002_60",
      "PT8DAV_FDAX_BRT_001_15",
      "PT8DAV_GC_LFD_001_60",
      "PT8DAV_GC_PCH_002_60",
      "PT8DAV_NQ_SBO_003_60",
      "PT8DAV_NQ_VBO_001_15"
     ],
     "promotedUtc": "2026-10-02T08:24:55.8407448Z",
     "origin": {
      "planCode": "FINTOKEI-PT8DAV-P1",
      "workspace": "fintokei-pt8dav",
      "bestPlanId": "fintokei-pt8dav__fintokei-pt8dav-p1-bt-20251225-2206-v7.8.13-20261002-0604",
      "backtestFolder": "fintokei-pt8dav-p1-bt-20251225-2206-v7.8.13-20261002-0604"
     },
     "run": {
      "startUtc": "2025-12-25T22:06:00Z",
      "endUtc": "2026-10-01T23:30:00Z",
      "trades": 339,
      "netProfit": 10934.64,
      "maxDrawdown": 1577.26,
      "priceSource": "CFD FINTOKEI (datafeed-external/FINTOKEI)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "4302718"
     ],
     "size": 0.5,
     "commission": 0,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "olderCards": 0,
     "measureCode": "FINTOKEI-PT8DAV-P1",
     "note": "Piano sull'oro, size 0,5. Stesse strategie di FTMO-PT8DAV-P1."
    },
    {
     "code": "FINTOKEI-PT8DAV-P2",
     "name": "fintokei-pt8dav-p2 (piano 2, 8 strategie, size 1)",
     "broker": "FINTOKEI",
     "state": "best-plan",
     "strategies": [
      "PT8DAV_BP_PCH_001_60",
      "PT8DAV_BP_RBM_001_30",
      "PT8DAV_ES_LFH_001_15",
      "PT8DAV_ES_MAC_001_30",
      "PT8DAV_FDAX_RHL_002_60",
      "PT8DAV_GC_VBO_001_15",
      "PT8DAV_NQ_VBO_003_240",
      "PT8DAV_YM_RBU_001_60"
     ],
     "promotedUtc": "2026-10-02T08:24:56.0174605Z",
     "origin": {
      "planCode": "FINTOKEI-PT8DAV-P2",
      "workspace": "fintokei-pt8dav",
      "bestPlanId": "fintokei-pt8dav__fintokei-pt8dav-p2-bt-20251225-2206-v7.8.13-20261002-0756",
      "backtestFolder": "fintokei-pt8dav-p2-bt-20251225-2206-v7.8.13-20261002-0756"
     },
     "run": {
      "startUtc": "2025-12-25T22:06:00Z",
      "endUtc": "2026-10-01T23:30:00Z",
      "trades": 209,
      "netProfit": 15848.02,
      "maxDrawdown": 2458.03,
      "priceSource": "CFD FINTOKEI (datafeed-external/FINTOKEI)",
      "equitySource": "realizzata"
     },
     "accounts": [
      "4302718"
     ],
     "size": 1,
     "commission": 0,
     "overnight": true,
     "overweek": true,
     "sessionFlatUtc": "20:45",
     "weights": {},
     "olderCards": 0,
     "measureCode": "FINTOKEI-PT8DAV-P2",
     "note": "Stesse strategie di FTMO-PT8DAV-P2. Sul feed Fintokei i trade abbinati all'interno scendono all'80%."
    }
   ],
   "pairs": {
    "FINTOKEI-EUROPA-O4|FINTOKEI-PT8DAV-P1": {
     "verdict": "limite",
     "reason": "Nessuna strategia in comune, ma trade gemelli 4-7%: al limite del 5%.",
     "twins": "4-7%",
     "measured": "2026-10-02",
     "note": "PT8DAV_GC_LFD_001_60 apre gli stessi trade di PT5DAV_GC_LFD_002_60.",
     "measuredOn": "FTMO-PT8DAV-P1 × FTMO-EUROPA-O4"
    },
    "FINTOKEI-EUROPA-O4|FINTOKEI-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 1-2%.",
     "twins": "1-2%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P2 × FTMO-EUROPA-O4"
    },
    "FINTOKEI-PT8DAV-P1|FINTOKEI-PT8DAV-P2": {
     "verdict": "ok",
     "reason": "Nessuna strategia in comune, trade gemelli 2-3%.",
     "twins": "2-3%",
     "measured": "2026-10-02",
     "measuredOn": "FTMO-PT8DAV-P1 × FTMO-PT8DAV-P2"
    }
   }
  },
  {
   "code": "ICS",
   "name": "ICS",
   "enabled": true,
   "accounts": [
    {
     "number": "2094690",
     "name": "ICS-V2",
     "currency": "USD",
     "balance": 100000,
     "enabled": true
    }
   ],
   "rules": "",
   "notes": [],
   "combinations": [],
   "plans": [],
   "pairs": {}
  }
 ],
 "warnings": []
};
