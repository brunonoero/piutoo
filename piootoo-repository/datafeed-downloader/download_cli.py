#!/usr/bin/env python3
"""
Piootoo Datafeed Downloader - script da terminale
----------------------------------------------------
Scarica lo storico OHLCV da Yahoo Finance e salva i JSON in ../datafeed
(stesso schema OhlcvDto/ClosedBarDto del form web, vedi core.py).

Uso non interattivo:
    python download_cli.py --symbols GC=F,CL=F,NQ=F --timeframes 15,60,1440
    python download_cli.py --symbols GC=F --timeframes 15 --days-back 365
    python download_cli.py --symbols GC=F --timeframes 15 --output-dir /altro/percorso

Uso interattivo (nessun argomento, o solo alcuni):
    python download_cli.py
    -> chiede a prompt i symbol e i timeframe mancanti

Default: ultimi 730 giorni (~2 anni), output in ../datafeed.
"""

import argparse
import sys

import core


def parse_args(argv=None):
    parser = argparse.ArgumentParser(
        description="Scarica storico OHLCV da Yahoo Finance e salva JSON in datafeed/."
    )
    parser.add_argument(
        "--symbols",
        help="Ticker Yahoo Finance separati da virgola (es. GC=F,CL=F,NQ=F). "
        "Se omesso, viene chiesto a prompt.",
    )
    parser.add_argument(
        "--timeframes",
        help="Timeframe in minuti separati da virgola (es. 5,15,60,1440). "
        "Se omesso, viene chiesto a prompt.",
    )
    parser.add_argument(
        "--days-back",
        type=int,
        default=core.DEFAULT_LOOKBACK_DAYS,
        help=f"Giorni di storico da scaricare (default {core.DEFAULT_LOOKBACK_DAYS}, ~2 anni).",
    )
    parser.add_argument(
        "--output-dir",
        default=core.DEFAULT_DATAFEED_DIR,
        help=f"Cartella di output dei JSON (default {core.DEFAULT_DATAFEED_DIR}).",
    )
    return parser.parse_args(argv)


def prompt_list(label: str, example: str) -> list:
    raw = input(f"{label} (separati da virgola, es. {example}): ").strip()
    return [item.strip() for item in raw.split(",") if item.strip()]


def main(argv=None):
    args = parse_args(argv)

    symbols = [s.strip() for s in args.symbols.split(",")] if args.symbols else None
    if not symbols:
        symbols = prompt_list("Symbol Yahoo Finance", "GC=F,CL=F,NQ=F")

    if args.timeframes:
        timeframes = [int(t.strip()) for t in args.timeframes.split(",")]
    else:
        timeframes = [int(t) for t in prompt_list("Timeframe in minuti", "5,15,60,1440")]

    if not symbols or not timeframes:
        print("Serve almeno un symbol e un timeframe.", file=sys.stderr)
        return 1

    print(f"Storico: ultimi {args.days_back} giorni")
    print(f"Output: {args.output_dir}")
    print(f"Symbol: {', '.join(symbols)}")
    print(f"Timeframe (minuti): {', '.join(str(t) for t in timeframes)}")
    print()

    had_errors = False
    for symbol in symbols:
        for tf in timeframes:
            result = core.download_and_save(symbol, tf, args.days_back, args.output_dir)
            if result["ok"]:
                note = f"  [{result['note']}]" if result.get("note") else ""
                status = "WARN" if result["bars"] == 0 else "OK  "
                print(f"{status} {symbol:<12} {tf:>5}m  {result['bars']:>5} barre  -> {result['file']}{note}")
            else:
                had_errors = True
                print(f"ERR  {symbol:<12} {tf:>5}m  {result['error']}")

    return 1 if had_errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
