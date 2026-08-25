"""
Core condiviso tra il form web (app.py) e lo script CLI (download_cli.py):
scarica lo storico OHLCV da Yahoo Finance e lo converte nello schema
OhlcvDto/ClosedBarDto usato dal cBot di trading/backtesting (vedi
ctrader/PiootooLiveTradingBot.cs, righe ~641-666).

    OhlcvDto    { dateTime, open, high, low, close, volume }
    ClosedBarDto{ symbol, timeframeMinutes, bar: OhlcvDto, ... }

Ogni file JSON prodotto rappresenta un ClosedBarDto[] "srotolato" per un
singolo symbol+timeframe:

    {
      "symbol": "...",
      "timeframeMinutes": 15,
      "source": "yahoo-finance",
      "generatedAtUtc": "...",
      "requestedStartUtc": "...",
      "effectiveStartUtc": "...",
      "note": "..." | null,
      "bars": [
        { "dateTime": "2024-01-01T00:00:00Z", "open": .., "high": .., "low": .., "close": .., "volume": .. },
        ...
      ]
    }
"""

import json
import os
from datetime import datetime, timedelta, timezone

import pandas as pd
import yfinance as yf

APP_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_DATAFEED_DIR = os.path.abspath(os.path.join(APP_DIR, "..", "datafeed"))

# Timeframe (minuti) -> intervallo nativo Yahoo Finance.
# I timeframe non presenti qui (es. 10, 120) vengono ottenuti scaricando
# l'intervallo nativo più fine che li divide esattamente e poi
# aggregando (resample) in pandas.
NATIVE_INTERVALS = {
    1: "1m",
    5: "5m",
    15: "15m",
    30: "30m",
    60: "60m",
    1440: "1d",
}

# Limite massimo di storico intraday concesso da Yahoo Finance per
# ciascun intervallo nativo (None = nessun limite pratico, es. daily).
MAX_LOOKBACK_DAYS = {
    "1m": 6,
    "5m": 58,
    "15m": 58,
    "30m": 58,
    "60m": 728,
    "1d": None,
}

DEFAULT_LOOKBACK_DAYS = 730  # ultimi 2 anni


def resolve_base_interval(timeframe_minutes: int):
    """Ritorna (interval_yahoo, resample_minutes|None).

    Se il timeframe richiesto è nativo su Yahoo, resample_minutes è None
    (nessuna aggregazione necessaria). Altrimenti indica in quanti minuti
    aggregare i dati scaricati con l'intervallo nativo più fine possibile.
    """
    if timeframe_minutes in NATIVE_INTERVALS:
        return NATIVE_INTERVALS[timeframe_minutes], None
    for base in (60, 30, 15, 5, 1):
        if base < timeframe_minutes and timeframe_minutes % base == 0:
            return NATIVE_INTERVALS[base], timeframe_minutes
    raise ValueError(f"Timeframe non supportato: {timeframe_minutes} minuti")


def _flatten_columns(df: pd.DataFrame) -> pd.DataFrame:
    # yfinance ritorna colonne MultiIndex (Price, Ticker) anche per un
    # singolo simbolo quando passato come lista/stringa in certe versioni.
    if isinstance(df.columns, pd.MultiIndex):
        df.columns = df.columns.get_level_values(0)
    return df


def download_symbol(symbol: str, timeframe_minutes: int, start: datetime, end: datetime):
    """Scarica le barre OHLCV per un symbol/timeframe. Ritorna (bars, note, effective_start)."""
    interval, resample_minutes = resolve_base_interval(timeframe_minutes)

    max_days = MAX_LOOKBACK_DAYS[interval]
    effective_start = start
    note = None
    if max_days is not None:
        earliest_allowed = end - timedelta(days=max_days)
        if effective_start < earliest_allowed:
            note = (
                f"Yahoo Finance limita l'intervallo '{interval}' a {max_days} giorni di storico; "
                f"la data di inizio richiesta ({start.date()}) è stata spostata a "
                f"{earliest_allowed.date()}."
            )
            effective_start = earliest_allowed

    df = yf.download(
        symbol,
        start=effective_start,
        end=end,
        interval=interval,
        progress=False,
        auto_adjust=False,
    )

    if df is None or df.empty:
        empty_note = (
            "Yahoo Finance non ha restituito barre: verifica che il ticker sia corretto "
            "e che la connessione internet verso Yahoo Finance funzioni."
        )
        return [], (note or empty_note), effective_start

    df = _flatten_columns(df)

    if resample_minutes:
        df = (
            df.resample(f"{resample_minutes}min")
            .agg(
                {
                    "Open": "first",
                    "High": "max",
                    "Low": "min",
                    "Close": "last",
                    "Volume": "sum",
                }
            )
            .dropna(how="any")
        )

    bars = []
    for ts, row in df.iterrows():
        ts = ts.tz_localize("UTC") if ts.tzinfo is None else ts.tz_convert("UTC")
        bars.append(
            {
                "dateTime": ts.strftime("%Y-%m-%dT%H:%M:%SZ"),
                "open": round(float(row["Open"]), 6),
                "high": round(float(row["High"]), 6),
                "low": round(float(row["Low"]), 6),
                "close": round(float(row["Close"]), 6),
                "volume": float(row["Volume"]) if not pd.isna(row["Volume"]) else 0.0,
            }
        )

    return bars, note, effective_start


def safe_filename(symbol: str, timeframe_minutes: int) -> str:
    safe_symbol = symbol.replace("=", "").replace("^", "").replace("/", "-")
    return f"{safe_symbol}_{timeframe_minutes}.json"


def download_and_save(symbol: str, timeframe_minutes: int, days_back: int, output_dir: str):
    """Scarica un symbol/timeframe e salva il JSON in output_dir. Ritorna un dict risultato."""
    end = datetime.now(timezone.utc)
    start = end - timedelta(days=days_back)

    os.makedirs(output_dir, exist_ok=True)

    try:
        bars, note, effective_start = download_symbol(symbol, timeframe_minutes, start, end)
    except Exception as exc:  # noqa: BLE001
        return {"symbol": symbol, "timeframeMinutes": timeframe_minutes, "ok": False, "error": str(exc)}

    filename = safe_filename(symbol, timeframe_minutes)
    filepath = os.path.join(output_dir, filename)

    output = {
        "symbol": symbol,
        "timeframeMinutes": timeframe_minutes,
        "source": "yahoo-finance",
        "generatedAtUtc": end.strftime("%Y-%m-%dT%H:%M:%SZ"),
        "requestedStartUtc": start.strftime("%Y-%m-%dT%H:%M:%SZ"),
        "effectiveStartUtc": effective_start.strftime("%Y-%m-%dT%H:%M:%SZ"),
        "note": note,
        "bars": bars,
    }

    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(output, f, indent=2)

    return {
        "symbol": symbol,
        "timeframeMinutes": timeframe_minutes,
        "ok": True,
        "bars": len(bars),
        "file": filename,
        "path": filepath,
        "note": note,
    }
