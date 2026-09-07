#!/usr/bin/env python3
"""Passo 0 del refactor: il metro.

Riaggrega un feed a 1 minuto secondo la regola del layer (bucket ancorati a
`session_start_hour` nel fuso dichiarato, etichetta = apertura, confine calcolato
in ora locale e riconvertito) e confronta barra per barra con l'aggregato che il
cBot ha gia' prodotto.

Zero differenze = il layer riproduce la griglia su cui le strategie girano oggi.
"""
from __future__ import annotations

import json
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path
from zoneinfo import ZoneInfo

UTC = timezone.utc
ROOT = Path("D:/Piootoo/PiootooApp/piootoo-repository/datafeed-external")

# Stessa tabella di InstrumentSpec.ResearchSessionStartHour / SessionStartHourOf.
SESSION_START_HOUR = {"FDAX": 1, "CC": 1, "CT": 1, "KC": 1, "SB": 1}
ANCHOR_TZ = ZoneInfo("Europe/Rome")


def read_candles(path: Path):
    """Le candele stanno una per riga: si legge in streaming invece di caricare 100 MB."""
    with path.open("r", encoding="utf-8") as source:
        for line in source:
            line = line.strip().rstrip(",")
            if not line.startswith('{"timestamp"'):
                continue
            row = json.loads(line)
            yield (
                datetime.fromisoformat(row["dateTime"].replace("Z", "+00:00")),
                row["open"], row["high"], row["low"], row["close"], row["volume"],
            )


def local_to_utc(local_naive: datetime) -> datetime:
    """Le convenzioni di SessionClock.ToUtc: avanti sull'ora inesistente, prima occorrenza sull'ambigua."""
    first = local_naive.replace(tzinfo=ANCHOR_TZ, fold=0)
    if first.astimezone(UTC).astimezone(ANCHOR_TZ).replace(tzinfo=None) != local_naive:
        # orario inesistente: sposta avanti dell'ampiezza del salto
        before = local_naive.replace(tzinfo=ANCHOR_TZ) - timedelta(days=1)
        after = local_naive.replace(tzinfo=ANCHOR_TZ) + timedelta(days=1)
        delta = after.utcoffset() - before.utcoffset()
        return (local_naive + delta).replace(tzinfo=ANCHOR_TZ).astimezone(UTC)
    return first.astimezone(UTC)


def bucket_start(open_utc: datetime, timeframe: int, anchor_hour: int) -> datetime:
    """Il confine si calcola in ora locale e si riconverte, mai sottraendo il resto a UTC."""
    local = open_utc.astimezone(ANCHOR_TZ).replace(tzinfo=None)
    minutes_from_anchor = (local.hour * 60 + local.minute) - anchor_hour * 60
    if minutes_from_anchor < 0:
        minutes_from_anchor += 1440
    return local_to_utc(local - timedelta(minutes=minutes_from_anchor % timeframe))


def aggregate(symbol: str, broker: str, timeframe: int) -> dict:
    anchor = SESSION_START_HOUR.get(symbol.lstrip("@").upper(), 0)
    source = ROOT / broker / f"{symbol}_1.json"
    buckets: dict[datetime, list] = {}
    order: list[datetime] = []
    for dt, o, h, l, c, v in read_candles(source):
        start = bucket_start(dt, timeframe, anchor)
        cur = buckets.get(start)
        if cur is None:
            buckets[start] = [o, h, l, c, v, 1]
            order.append(start)
        else:
            cur[1] = max(cur[1], h)
            cur[2] = min(cur[2], l)
            cur[3] = c
            cur[4] += v
            cur[5] += 1
    return buckets


def compare(symbol: str, broker: str, timeframe: int) -> None:
    mine = aggregate(symbol, broker, timeframe)
    theirs = {
        dt: (o, h, l, c, v)
        for dt, o, h, l, c, v in read_candles(ROOT / broker / f"{symbol}_{timeframe}.json")
    }

    common = sorted(set(mine) & set(theirs))
    only_mine = sorted(set(mine) - set(theirs))
    only_theirs = sorted(set(theirs) - set(mine))

    print(f"\n=== {broker} {symbol} {timeframe}m  (ancoraggio {SESSION_START_HOUR.get(symbol.lstrip('@').upper(), 0):02d}:00 Europe/Rome)")
    print(f"    bucket dal 1m : {len(mine)}")
    print(f"    file esistente: {len(theirs)}")
    print(f"    in comune     : {len(common)}")
    print(f"    solo dal 1m   : {len(only_mine)}")
    print(f"    solo nel file : {len(only_theirs)}")

    if only_mine[:3]:
        print(f"    esempi solo-1m   : {[d.isoformat() for d in only_mine[:3]]}")
    if only_theirs[:3]:
        print(f"    esempi solo-file : {[d.isoformat() for d in only_theirs[:3]]}")

    diffs = {"open": 0, "high": 0, "low": 0, "close": 0, "volume": 0}
    esempi = []
    for dt in common:
        m = mine[dt]
        t = theirs[dt]
        campi = []
        for idx, nome in enumerate(("open", "high", "low", "close")):
            if abs(m[idx] - t[idx]) > 1e-9:
                diffs[nome] += 1
                campi.append(f"{nome} {m[idx]} vs {t[idx]}")
        if m[4] != t[4]:
            diffs["volume"] += 1
            campi.append(f"volume {m[4]} vs {t[4]}")
        if campi and len(esempi) < 5:
            esempi.append(f"      {dt.isoformat()}  " + " | ".join(campi))

    ohlc_diversi = sum(diffs[k] for k in ("open", "high", "low", "close"))
    print(f"    OHLC diversi  : {ohlc_diversi}  {dict((k, v) for k, v in diffs.items() if v)}")
    for riga in esempi:
        print(riga)


if __name__ == "__main__":
    broker = sys.argv[1] if len(sys.argv) > 1 else "FTMOPLATFORM"
    symbol = sys.argv[2] if len(sys.argv) > 2 else "@FDAX"
    timeframe = int(sys.argv[3]) if len(sys.argv) > 3 else 240
    compare(symbol, broker, timeframe)
