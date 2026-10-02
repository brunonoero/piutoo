"""Rigenera web/data/piani.js, i dati della pagina "Piani per broker".

    python aggiorna-dati.py

Legge quello che il server gia' scrive su disco e non chiede il server acceso:

  accounts/brokers.json, accounts/accounts.json     broker e conti
  broker-workspaces/{BROKER}/plans/plans.json       piani in produzione (e ritirati)
  best-plans/*/best-plan.json, artifacts/plan.json  best plan: strategie e numeri del run, size e tenuta

e ci unisce data/compatibilita.json, la parte scritta a mano (trade gemelli misurati, note,
combinazioni). Le strategie in comune fra due piani le trova da solo: a mano si scrive solo cio'
che nessun file dichiara.

Il risultato e' un file .js e non .json perche' il sito si apre anche con un doppio clic
(file://), dove un fetch di JSON e' bloccato dal browser.
"""

from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path

WEB = Path(__file__).resolve().parent
REPOSITORY = WEB.parent
OUTPUT = WEB / "data" / "piani.js"
CURATED = WEB / "data" / "compatibilita.json"

# Sotto questa quota di trade gemelli due piani sono compatibili (registro-piani.md, 29/09/2026).
VERDICTS = ("ok", "limite", "no")


def read_json(path: Path):
    # utf-8-sig: i file scritti dal server possono avere il BOM.
    return json.loads(path.read_text(encoding="utf-8-sig"))


def load_brokers() -> dict[str, dict]:
    brokers = {}
    for broker in read_json(REPOSITORY / "accounts" / "brokers.json").get("Brokers", []):
        brokers[broker["Code"]] = {
            "code": broker["Code"],
            "name": broker.get("Name") or broker["Code"],
            "enabled": broker.get("Enabled", True),
            "accounts": [],
        }
    for account in read_json(REPOSITORY / "accounts" / "accounts.json").get("Accounts", []):
        broker = brokers.get(account.get("BrokerCode") or "")
        if broker is None:
            continue
        broker["accounts"].append({
            "number": account.get("AccountNumber"),
            "name": account.get("Name"),
            "currency": account.get("Currency"),
            "balance": account.get("InitialBalance"),
            "enabled": account.get("Enabled", True),
        })
    return brokers


def field(record: dict, name: str, default=None):
    """Legge un campo senza badare alla maiuscola iniziale.

    plans.json dei workspace e' in PascalCase, il plan.json copiato fra gli artefatti di un best
    plan e' in camelCase: stesso contratto, due serializzatori.
    """
    if name in record:
        return record[name]
    lowered = name.lower()
    for key, value in record.items():
        if key.lower() == lowered:
            return value
    return default


def plan_settings(plan: dict) -> dict:
    """I campi di un TradingPlan che la pagina mostra: conti, size, tenuta, commissione, pesi."""
    holding = field(plan, "Holding") or {}
    weights = {k: v for k, v in (field(plan, "StrategyWeights") or {}).items() if k and v != 1}
    single = field(plan, "AccountNumber")
    return {
        "accounts": field(plan, "Accounts") or ([single] if single else []),
        "size": field(plan, "SizeMultiplier", 1),
        "commission": field(plan, "CommissionPerContract"),
        "overnight": bool(field(holding, "AllowOvernight")),
        "overweek": bool(field(holding, "AllowOverweek")),
        "sessionFlatUtc": (field(holding, "SessionFlatUtc") or "")[:5],
        "weights": weights,
    }


def load_production_plans(warnings: list[str]) -> list[dict]:
    plans = []
    root = REPOSITORY / "broker-workspaces"
    if not root.is_dir():
        return plans
    for plans_file in sorted(root.glob("*/plans/plans.json")):
        broker_code = plans_file.parent.parent.name
        for plan in read_json(plans_file):
            provenance = plan.get("Provenance") or {}
            entry = {
                "code": plan["Code"],
                "name": plan.get("Name") or "",
                "broker": plan.get("BrokerCode") or broker_code,
                "state": "ritirato" if plan.get("RetiredUtc") else "produzione",
                "strategies": sorted(plan.get("EnabledStrategies") or []),
                "promotedUtc": provenance.get("PromotedUtc"),
                "retiredUtc": plan.get("RetiredUtc"),
                "origin": {
                    "planCode": provenance.get("SourcePlanCode"),
                    "workspace": provenance.get("SourceWorkspaceId"),
                    "bestPlanId": provenance.get("BestPlanId"),
                    "backtestFolder": provenance.get("BacktestFolder"),
                    "previousPlanCode": provenance.get("PreviousPlanCode"),
                },
                **plan_settings(plan),
            }
            if not entry["strategies"]:
                warnings.append(f"{entry['code']}: piano di produzione senza EnabledStrategies.")
            plans.append(entry)
    return plans


def load_best_plans(warnings: list[str]) -> list[dict]:
    """Un best plan per codice di piano: la scheda promossa per ultima. Le altre si contano."""
    by_code: dict[str, list[dict]] = {}
    root = REPOSITORY / "best-plans"
    if not root.is_dir():
        return []
    for card_file in sorted(root.glob("*/best-plan.json")):
        card = read_json(card_file)
        code = card.get("planCode")
        if not code:
            continue  # run neutro sul masterfilter: non e' un piano
        plan_file = card_file.parent / "artifacts" / "plan.json"
        plan = read_json(plan_file) if plan_file.is_file() else {}
        if not plan:
            warnings.append(f"{code}: best plan senza artifacts/plan.json, broker e size non noti.")
        entry = {
            "code": code,
            "name": card.get("planName") or field(plan, "Name") or "",
            "broker": field(plan, "BrokerCode") or "",
            "state": "best-plan",
            "strategies": sorted(s["strategyCode"] for s in card.get("strategies", []) if s.get("strategyCode")),
            "promotedUtc": card.get("promotedUtc"),
            "origin": {
                "planCode": code,
                "workspace": card.get("workspaceId"),
                "bestPlanId": card.get("id"),
                "backtestFolder": card.get("backtestFolder"),
            },
            "run": {
                "startUtc": card.get("startUtc"),
                "endUtc": card.get("endUtc"),
                "trades": card.get("totalTrades"),
                "netProfit": card.get("netProfit"),
                "maxDrawdown": card.get("maxDrawdown"),
                "priceSource": card.get("priceSource"),
                "equitySource": card.get("equitySource"),
            },
            **plan_settings(plan),
        }
        by_code.setdefault(code, []).append(entry)

    latest = []
    for code, cards in by_code.items():
        cards.sort(key=lambda c: c["promotedUtc"] or "")
        chosen = cards[-1]
        chosen["olderCards"] = len(cards) - 1
        latest.append(chosen)
    return latest


class TwinLookup:
    """Le misure di trade gemelli, ritrovate per INSIEME DI STRATEGIE e non per codice.

    Cosi' una copia, una versione a size diversa e il piano di produzione nato da un best plan
    prendono l'esito del piano misurato senza che nessuno lo riscriva.
    """

    def __init__(self, curated: dict, strategies_by_code: dict[str, frozenset], warnings: list[str]):
        self.equivalents = curated.get("equivalenti", {})
        self.strategies_by_code = strategies_by_code
        self.entries = []
        for row in curated.get("gemelli", []):
            if row.get("esito") not in VERDICTS:
                warnings.append(f"compatibilita.json: esito sconosciuto nella coppia {row.get('a')} x {row.get('b')}.")
                continue
            sets = [strategies_by_code.get(row[k]) for k in ("a", "b")]
            if any(s is None for s in sets):
                missing = [row[k] for k, s in zip(("a", "b"), sets) if s is None]
                warnings.append(f"compatibilita.json: {', '.join(missing)} non e' un piano promosso noto, coppia ignorata.")
                continue
            self.entries.append((sets[0], sets[1], row))

    def measured_set(self, code: str, own: frozenset) -> frozenset:
        equivalent = self.equivalents.get(code)
        return self.strategies_by_code.get(equivalent, own) if equivalent else own

    def find(self, plan_a: dict, plan_b: dict):
        set_a = self.measured_set(plan_a["measureCode"], frozenset(plan_a["strategies"]))
        set_b = self.measured_set(plan_b["measureCode"], frozenset(plan_b["strategies"]))
        for first, second, row in self.entries:
            if (first == set_a and second == set_b) or (first == set_b and second == set_a):
                return row
        return None


def compare(plan_a: dict, plan_b: dict, twins: TwinLookup) -> dict:
    shared = sorted(set(plan_a["strategies"]) & set(plan_b["strategies"]))
    if shared:
        same = set(plan_a["strategies"]) == set(plan_b["strategies"])
        return {
            "verdict": "stesso" if same else "no",
            "reason": "Stesse strategie: è lo stesso piano, mai su due conti." if same
                      else "Strategie in comune.",
            "shared": shared,
        }
    row = twins.find(plan_a, plan_b)
    if row is None:
        return {"verdict": "nd", "reason": "Nessuna strategia in comune, ma i trade gemelli non sono stati misurati."}
    reasons = {
        "ok": f"Nessuna strategia in comune, trade gemelli {row['quota']}.",
        "limite": f"Nessuna strategia in comune, ma trade gemelli {row['quota']}: al limite del 5%.",
        "no": f"Nessuna strategia in comune, ma trade gemelli {row['quota']}.",
    }
    result = {"verdict": row["esito"], "reason": reasons[row["esito"]], "twins": row["quota"], "measured": row.get("data")}
    if row.get("nota"):
        result["note"] = row["nota"]
    measured_on = sorted({row["a"], row["b"]} - {plan_a["code"], plan_b["code"]})
    if measured_on:
        result["measuredOn"] = f"{row['a']} × {row['b']}"
    return result


def build() -> dict:
    warnings: list[str] = []
    curated = read_json(CURATED)
    brokers = load_brokers()
    production = load_production_plans(warnings)
    best = load_best_plans(warnings)

    # Un best plan gia' promosso in produzione (e non ritirato) non fa riga a se': e' l'origine
    # della riga di produzione, che ne porta i numeri del run.
    best_by_code = {plan["code"]: plan for plan in best}
    promoted_sources = set()
    for plan in production:
        plan["measureCode"] = plan["origin"]["planCode"] or plan["code"]
        source = best_by_code.get(plan["origin"]["planCode"] or "")
        if source is not None:
            plan["run"] = source["run"]
            if plan["state"] == "produzione":
                promoted_sources.add(source["code"])
    for plan in best:
        plan["measureCode"] = plan["code"]

    rows = production + [plan for plan in best if plan["code"] not in promoted_sources]

    strategies_by_code = {plan["code"]: frozenset(plan["strategies"]) for plan in best + production}
    twins = TwinLookup(curated, strategies_by_code, warnings)
    plan_notes = curated.get("piani", {})

    state_order = {"produzione": 0, "best-plan": 1, "ritirato": 2}
    result_brokers = []
    for code, broker in brokers.items():
        plans = [plan for plan in rows if plan["broker"] == code]
        plans.sort(key=lambda p: (state_order[p["state"]], p["code"]))
        for plan in plans:
            note = plan_notes.get(plan["code"]) or plan_notes.get(plan["measureCode"])
            if note:
                plan["note"] = note

        active = [plan for plan in plans if plan["state"] != "ritirato"]
        pairs = {}
        for i, plan_a in enumerate(active):
            for plan_b in active[i + 1:]:
                pairs[f"{plan_a['code']}|{plan_b['code']}"] = compare(plan_a, plan_b, twins)

        extra = curated.get("broker", {}).get(code, {})
        result_brokers.append({
            **broker,
            "rules": extra.get("regole", ""),
            "notes": extra.get("note", []),
            "combinations": extra.get("combinazioni", []),
            "plans": plans,
            "pairs": pairs,
        })

    orphans = sorted({plan["code"] for plan in rows if plan["broker"] not in brokers})
    if orphans:
        warnings.append("Piani con un broker che l'anagrafica non conosce: " + ", ".join(orphans) + ".")

    # Prima i broker che hanno piani in produzione, poi quelli con soli best plan, poi gli altri.
    def in_production(broker: dict) -> int:
        return sum(1 for plan in broker["plans"] if plan["state"] == "produzione")

    result_brokers.sort(key=lambda b: (-in_production(b), -len(b["plans"]), b["code"]))
    return {
        "generatedUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "criterion": curated.get("criterio", ""),
        "brokers": result_brokers,
        "warnings": warnings,
    }


def main() -> int:
    data = build()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    payload = json.dumps(data, ensure_ascii=False, indent=1)
    OUTPUT.write_text(
        "// Generato da aggiorna-dati.py: non modificare a mano.\n"
        f"window.PIOOTOO_PIANI = {payload};\n",
        encoding="utf-8",
    )
    for broker in data["brokers"]:
        counts = {}
        for plan in broker["plans"]:
            counts[plan["state"]] = counts.get(plan["state"], 0) + 1
        summary = ", ".join(f"{n} {state}" for state, n in counts.items()) or "nessun piano promosso"
        unmeasured = sum(1 for pair in broker["pairs"].values() if pair["verdict"] == "nd")
        print(f"{broker['code']}: {summary}; {len(broker['pairs'])} coppie, {unmeasured} non misurate")
    for warning in data["warnings"]:
        print("AVVISO:", warning)
    print("scritto", OUTPUT)
    return 0


if __name__ == "__main__":
    sys.exit(main())
