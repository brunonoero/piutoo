#!/usr/bin/env python3
"""SUPERATO — usare `aggregate_flat_feed.py`.

Questo script resta solo come riferimento storico: produce la vecchia gerarchia
`datafeed/{tf}/{symbol}/`, e soprattutto sbaglia il bucket. Tratta il timestamp
del CSV come inizio del minuto (`floor(t)`), mentre e' la sua fine: i feed che
ha generato sono sfasati di un minuto per barra. Vedi `docs/decisioni.md`
2026-08-25.

Aggrega il CSV minute di NQ nei feed JSON gerarchici di Piootoo.

I timeframe generati per default sono quelli richiesti dalle strategie @NQ del
catalogo: 5m, 15m, 30m e 1h. Su richiesta si generano anche 4h e giornaliero. Il
CSV viene letto una volta sola e alimenta tutti i timeframe richiesti in
parallelo.

Esempio (rigenerazione completa, se gli orari del CSV sono già UTC):
  python aggregate_nq_ascii.py --source-timezone UTC --overwrite

Esempio (solo i timeframe che mancano, senza toccare 15m e 1h già presenti):
  python aggregate_nq_ascii.py --source-timezone UTC --timeframes 5 30

Il fuso del CSV è obbligatorio: il feed Piootoo deve contenere esclusivamente
timestamp UTC, ma il file ASCII non dichiara il proprio fuso. I feed 15m e 1h
già nel repository sono stati generati con `--source-timezone UTC`: usare un
fuso diverso per i timeframe nuovi produrrebbe serie non allineate fra loro.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
from dataclasses import dataclass
from datetime import datetime, timezone, tzinfo
from decimal import Decimal
from pathlib import Path
from typing import Iterator
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_INPUT = (
    REPOSITORY_ROOT
    / "datafeed-future"
    / "FUTURES_Historical_Data"
    / "@NQ-ASCII Mapping-CME-Futures-Minute-Trade.csv"
)
DEFAULT_OUTPUT_ROOT = REPOSITORY_ROOT / "datafeed"
SYMBOL = "@NQ"

# Cartella e barType per timeframe, come li attende `DataSourceRepository`
# (`TimeframeFolders` e `CanonicalBarTypes`): un nome inventato qui renderebbe il
# feed invisibile al server.
CANONICAL_TIMEFRAMES: dict[int, tuple[str, str]] = {
    1: ("1m", "OneMinute"),
    5: ("5m", "FiveMinute"),
    15: ("15m", "FifteenMinute"),
    30: ("30m", "ThirtyMinute"),
    60: ("1h", "OneHour"),
    240: ("4h", "FourHour"),
    1440: ("D", "Daily"),
}

# La barra giornaliera è il giorno di calendario del CSV, e per questo feed non è una
# scelta arbitraria: nell'orologio del sorgente le barre finiscono alle 23:00 e
# riprendono alle 00:00, cioè la pausa di manutenzione CME (16:00-17:00 di Chicago)
# cade esattamente a cavallo della mezzanotte. Il giorno di calendario contiene quindi
# la sessione completa, dalla riapertura serale alla chiusura del pomeriggio dopo, ed è
# lo stesso raggruppamento che il feed usa già per i file giornalieri.
#
# Resta un'imperfezione nelle due-tre settimane all'anno in cui l'ora legale europea e
# quella americana non sono allineate: lì la pausa slitta di un'ora oltre la mezzanotte
# e il confine taglia la sessione un'ora dopo. Riguarda circa l'8% dei giorni del
# campione. Per le strategie EasyLanguage questo non conta, perché le loro barre di
# sessione si costruiscono a runtime dal timeframe intraday con l'orario di sessione
# dichiarato dalla singola sorgente (`EasyLib.BuildSessionSeries`), non da questo feed.
#
# Il settimanale invece non si genera: `bucket_start` allinea dalla mezzanotte, quindi
# non sa dove comincia la settimana, e nessuna strategia lo chiede.
SESSION_BOUND_TIMEFRAMES = {10080: "W"}

# Timeframe usati dalle strategie @NQ eseguibili del catalogo: 5 (Easy_152),
# 15 (Easy_156/342/486/587/796/956, PTS_002/003), 30 (Easy_181/298) e 60
# (Easy_531, PTS_001).
DEFAULT_TIMEFRAMES = (5, 15, 30, 60)


@dataclass
class Candle:
    start_utc: datetime
    open: Decimal
    high: Decimal
    low: Decimal
    close: Decimal
    volume: Decimal

    def update(self, high: Decimal, low: Decimal, close: Decimal, volume: Decimal) -> None:
        self.high = max(self.high, high)
        self.low = min(self.low, low)
        self.close = close
        self.volume += volume


def parse_timeframes(values: list[int]) -> list[int]:
    timeframes: list[int] = []
    for minutes in values:
        if minutes in SESSION_BOUND_TIMEFRAMES:
            raise ValueError(
                f"Timeframe {minutes} ('{SESSION_BOUND_TIMEFRAMES[minutes]}') non generabile: "
                "l'allineamento delle barre parte dalla mezzanotte, quindi non sa dove comincia "
                "la settimana."
            )
        if minutes not in CANONICAL_TIMEFRAMES:
            supported = ", ".join(str(key) for key in sorted(CANONICAL_TIMEFRAMES))
            raise ValueError(f"Timeframe {minutes} non riconosciuto. Ammessi: {supported}.")
        # Vincolo su cui poggia `bucket_start`: se il timeframe non divide la giornata, le
        # barre scivolerebbero di giorno in giorno invece di ripartire dalla mezzanotte.
        if 1440 % minutes != 0:
            raise ValueError(f"Timeframe {minutes} non divide la giornata in parti uguali.")
        if minutes not in timeframes:
            timeframes.append(minutes)

    return sorted(timeframes)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, default=DEFAULT_INPUT, help="CSV minute sorgente.")
    parser.add_argument(
        "--output-root",
        type=Path,
        default=DEFAULT_OUTPUT_ROOT,
        help="Radice che conterrà, ad esempio, datafeed/5m/@NQ e datafeed/30m/@NQ.",
    )
    parser.add_argument(
        "--source-timezone",
        required=True,
        help="Fuso IANA del CSV, ad esempio UTC o America/Chicago.",
    )
    parser.add_argument(
        "--timeframes",
        type=int,
        nargs="+",
        default=list(DEFAULT_TIMEFRAMES),
        metavar="MINUTI",
        help=(
            "Timeframe in minuti da generare. Default: "
            f"{' '.join(str(minutes) for minutes in DEFAULT_TIMEFRAMES)}."
        ),
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Sostituisce i JSON giornalieri eventualmente già presenti.",
    )
    return parser.parse_args()


def parse_minute(row: dict[str, str], source_timezone: tzinfo) -> tuple[datetime, Decimal, Decimal, Decimal, Decimal, Decimal]:
    try:
        # Il file ASCII Mapping usa giorno/mese/anno, anche quando le prime righe sono
        # ambigue (es. 03/01/2006). Il 13/01/2006 presente nel file conferma il formato.
        local_time = datetime.strptime(
            f"{row['Date']} {row['Time']}", "%d/%m/%Y %H:%M:%S"
        ).replace(tzinfo=source_timezone)
        timestamp_utc = local_time.astimezone(timezone.utc)
        return (
            timestamp_utc,
            Decimal(row["Open"]),
            Decimal(row["High"]),
            Decimal(row["Low"]),
            Decimal(row["Close"]),
            Decimal(row["TotalVolume"]),
        )
    except (KeyError, ValueError, ArithmeticError) as error:
        raise ValueError(f"Riga CSV non valida: {row}") from error


def bucket_start(timestamp_utc: datetime, timeframe_minutes: int) -> datetime:
    # L'allineamento parte dalla mezzanotte e non dall'ora corrente, così vale anche per i
    # timeframe che superano i 60 minuti: il 4h cade su 00, 04, 08, 12, 16 e 20, e il
    # giornaliero collassa sull'inizio del giorno.
    minutes_since_midnight = timestamp_utc.hour * 60 + timestamp_utc.minute
    aligned = minutes_since_midnight - minutes_since_midnight % timeframe_minutes
    return timestamp_utc.replace(
        hour=aligned // 60, minute=aligned % 60, second=0, microsecond=0
    )


def read_minutes(
    csv_path: Path, source_timezone: tzinfo
) -> Iterator[tuple[datetime, Decimal, Decimal, Decimal, Decimal, Decimal]]:
    previous_timestamp: datetime | None = None

    with csv_path.open("r", encoding="utf-8-sig", newline="") as source:
        for row_number, row in enumerate(csv.DictReader(source), start=2):
            minute = parse_minute(row, source_timezone)
            timestamp = minute[0]
            if previous_timestamp is not None and timestamp < previous_timestamp:
                raise ValueError(
                    f"CSV non ordinato alla riga {row_number}: {timestamp.isoformat()} "
                    f"precede {previous_timestamp.isoformat()}."
                )
            previous_timestamp = timestamp
            yield minute


def json_number(value: Decimal) -> int | float:
    if value == value.to_integral_value():
        return int(value)
    return float(value)


def candle_json(candle: Candle) -> dict[str, object]:
    return {
        "timestamp": int(candle.start_utc.timestamp()),
        "dateTime": candle.start_utc.strftime("%Y-%m-%dT%H:%M:%SZ"),
        "dateTimeFormatted": candle.start_utc.strftime("%Y-%m-%d %H:%M:%S"),
        "open": json_number(candle.open),
        "high": json_number(candle.high),
        "low": json_number(candle.low),
        "close": json_number(candle.close),
        "volume": json_number(candle.volume),
        "volumeHigh": None,
        "volumeLow": None,
    }


def write_day(output_directory: Path, timeframe_name: str, candles: list[Candle]) -> Path:
    day = candles[0].start_utc.strftime("%Y%m%d")
    destination = output_directory / f"{SYMBOL}-{day}.json"

    payload = {
        "symbol": SYMBOL,
        "barType": timeframe_name,
        "barEnd": None,
        "lastUpdate": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%fZ"),
        "candleCount": len(candles),
        "candles": [candle_json(candle) for candle in candles],
    }

    temporary = destination.with_suffix(".json.tmp")
    with temporary.open("w", encoding="utf-8", newline="\n") as output:
        json.dump(payload, output, ensure_ascii=False, indent=2)
        output.write("\n")
        output.flush()
        os.fsync(output.fileno())
    temporary.replace(destination)
    return destination


class TimeframeFeed:
    """Accumula le barre di un timeframe e scrive un JSON per giorno UTC."""

    def __init__(self, timeframe_minutes: int, output_root: Path) -> None:
        self.timeframe_minutes = timeframe_minutes
        self.folder, self.bar_type = CANONICAL_TIMEFRAMES[timeframe_minutes]
        self.directory = output_root / self.folder / SYMBOL
        self.files = 0
        self.candles = 0
        self._current: Candle | None = None
        self._day_candles: list[Candle] = []
        self._current_day = None

    def ensure_writable(self) -> None:
        """
        Il controllo sui file già presenti sta prima della lettura del CSV, non dentro la
        scrittura del singolo giorno: il CSV alimenta tutti i timeframe nella stessa
        passata, e accorgersi di un file già presente a metà corsa lascerebbe i feed nuovi
        troncati a una data intermedia.
        """
        existing = next(self.directory.glob(f"{SYMBOL}-*.json"), None)
        if existing is not None:
            raise FileExistsError(
                f"{self.directory} contiene già dei feed (es. {existing.name}). Escludi "
                f"{self.timeframe_minutes} da --timeframes oppure usa --overwrite per "
                "rigenerarlo esplicitamente."
            )

    def create_directory(self) -> None:
        self.directory.mkdir(parents=True, exist_ok=True)

    def add(
        self,
        timestamp: datetime,
        open_: Decimal,
        high: Decimal,
        low: Decimal,
        close: Decimal,
        volume: Decimal,
    ) -> None:
        start = bucket_start(timestamp, self.timeframe_minutes)
        if self._current is None or self._current.start_utc != start:
            if self._current is not None:
                self._append(self._current)
            self._current = Candle(start, open_, high, low, close, volume)
        else:
            self._current.update(high, low, close, volume)

    def finish(self) -> None:
        if self._current is not None:
            self._append(self._current)
            self._current = None
        self._flush_day()

    def _append(self, candle: Candle) -> None:
        day = candle.start_utc.date()
        if self._current_day is not None and day != self._current_day:
            self._flush_day()

        self._current_day = day
        self._day_candles.append(candle)

    def _flush_day(self) -> None:
        if not self._day_candles:
            return

        write_day(self.directory, self.bar_type, self._day_candles)
        self.files += 1
        self.candles += len(self._day_candles)
        self._day_candles = []


def resolve_source_timezone(name: str) -> tzinfo:
    if name.upper() in {"UTC", "GMT"}:
        return timezone.utc

    try:
        return ZoneInfo(name)
    except ZoneInfoNotFoundError as error:
        raise ValueError(f"Fuso IANA non valido: {name}") from error


def main() -> int:
    args = parse_args()
    if not args.input.is_file():
        raise FileNotFoundError(f"CSV sorgente non trovato: {args.input}")

    source_timezone = resolve_source_timezone(args.source_timezone)
    timeframes = parse_timeframes(args.timeframes)

    feeds = [TimeframeFeed(minutes, args.output_root) for minutes in timeframes]
    if not args.overwrite:
        for feed in feeds:
            feed.ensure_writable()

    for feed in feeds:
        feed.create_directory()

    print(
        "Timeframe richiesti: "
        + ", ".join(f"{feed.folder} ({feed.bar_type})" for feed in feeds)
    )

    for minute in read_minutes(args.input, source_timezone):
        for feed in feeds:
            feed.add(*minute)

    for feed in feeds:
        feed.finish()
        print(
            f"{feed.folder}: scritti {feed.candles:,} candle in {feed.files:,} file sotto "
            f"{feed.directory}"
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
