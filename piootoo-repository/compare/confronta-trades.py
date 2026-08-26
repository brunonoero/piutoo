"""Confronta due trades.json dello stesso run, ignorando cio' che cambia per forza.

Nato per l'A/B della guardia sul poll a timer (docs/decisioni.md, 2026-08-26): due backtest
identici, un solo parametro diverso, e la domanda e' una sola — e' lo stesso identico insieme di
trade? Id e correlazioni sono GUID nuovi a ogni run e non vanno confrontati; tutto il resto si'.

    python confronta-trades.py A/trades.json B/trades.json

Esce con codice 0 se i due run coincidono, 1 se differiscono, 2 se non ha potuto confrontarli.
"""

import json
import os
import sys
from decimal import Decimal

# Cio' che identifica un trade fra due run diversi. Non l'IntentId: e' un GUID nuovo ogni volta.
CHIAVE = ("strategyCode", "symbol", "entryTimeUtc", "direction")

# Cio' che deve coincidere. Un solo campo diverso qui e' una regressione da capire.
CONFRONTATI = (
    "quantity", "entryPrice", "exitTimeUtc", "exitPrice", "exitReason",
    "grossProfit", "netProfit", "commission", "swap",
)


def carica(percorso):
    if not os.path.exists(percorso):
        print("File assente: " + percorso)
        sys.exit(2)

    journal = percorso + "l"  # trades.jsonl affiancato
    if os.path.exists(journal):
        print("ATTENZIONE: c'e' ancora " + os.path.basename(journal) + " accanto a " +
              os.path.basename(percorso) + ": il run non si e' chiuso normalmente e l'array non e' "
              "aggiornato. Rileggi i trade dall'API (GET /trading-sessions/{id}/trades) prima di "
              "confrontarli.")

    with open(percorso, "r", encoding="utf-8-sig") as handle:
        dati = json.load(handle)
    if not isinstance(dati, list):
        print("Non e' un array di trade: " + percorso)
        sys.exit(2)
    return dati


def normalizza(valore):
    # I prezzi arrivano come numeri JSON: il confronto passa da Decimal sulla stringa per non
    # inventare differenze che sono solo binary floating point.
    if isinstance(valore, float):
        return Decimal(repr(valore))
    return valore


def indicizza(trade_list, etichetta):
    indice = {}
    for trade in trade_list:
        chiave = tuple(str(trade.get(campo)) for campo in CHIAVE)
        if chiave in indice:
            print("Chiave duplicata in " + etichetta + ": " + " | ".join(chiave) +
                  " — il confronto la tratta come un trade solo.")
        indice[chiave] = trade
    return indice


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(2)

    a = carica(sys.argv[1])
    b = carica(sys.argv[2])
    print("Trade: A={0}  B={1}".format(len(a), len(b)))

    indice_a = indicizza(a, "A")
    indice_b = indicizza(b, "B")

    solo_a = sorted(set(indice_a) - set(indice_b))
    solo_b = sorted(set(indice_b) - set(indice_a))
    comuni = sorted(set(indice_a) & set(indice_b))

    differenze = []
    for chiave in comuni:
        for campo in CONFRONTATI:
            va = normalizza(indice_a[chiave].get(campo))
            vb = normalizza(indice_b[chiave].get(campo))
            if va != vb:
                differenze.append((chiave, campo, va, vb))

    if not solo_a and not solo_b and not differenze:
        print("IDENTICI: stesso insieme di trade, stessi prezzi, stessi risultati.")
        return 0

    # Il primo trade che diverge, in ordine di tempo, e' quello da cui parte l'indagine: tutto
    # cio' che viene dopo puo' essere una conseguenza, non una causa.
    if solo_a:
        print("\nPresenti solo in A ({0}). I primi:".format(len(solo_a)))
        for chiave in solo_a[:10]:
            print("  " + " | ".join(chiave))
    if solo_b:
        print("\nPresenti solo in B ({0}). I primi:".format(len(solo_b)))
        for chiave in solo_b[:10]:
            print("  " + " | ".join(chiave))
    if differenze:
        print("\nStesso trade, valori diversi ({0}). I primi:".format(len(differenze)))
        for chiave, campo, va, vb in differenze[:20]:
            print("  " + " | ".join(chiave) + "  ->  " + campo + ": A=" + str(va) + "  B=" + str(vb))

    return 1


if __name__ == "__main__":
    sys.exit(main())
