#!/usr/bin/env python3
"""Aggrega i CSV minute di `FUTURES_Historical_Data` nel datafeed piatto di Piootoo.

Layout prodotto — un file per coppia (simbolo, timeframe), tutti nella stessa
cartella, senza gerarchia:

    datafeed/@NQ_15.json
    datafeed/@NQ_1440.json
    datafeed/@GC_240.json

Il timeframe nel nome file e' in **minuti**, la stessa unita' che
`DataSourceRepository` usa per parlare di timeframe. Il contenuto e' lo schema
gia' noto al server (`symbol` / `barType` / `candles[]`).

Esempio (tutti i simboli del catalogo, con i soli timeframe che le strategie
usano davvero):

    python aggregate_flat_feed.py --overwrite

Esempio (un simbolo solo, timeframe scelti a mano):

    python aggregate_flat_feed.py --symbols @NQ --timeframes 15 60 --overwrite


## Le due convenzioni del sorgente, e perche' non sono opinabili

**1. I timestamp del CSV sono ora CET/CEST, non UTC.** Il file non lo dichiara,
ma due misure indipendenti lo mostrano e si controllano a vicenda: sul feed @NQ
il picco di volume dell'apertura cash di New York cade sullo slot 15:30 e la
pausa di manutenzione CME cade fra le 23:00 e le 00:00 — in *entrambe* le
stagioni. Se le etichette fossero UTC vero, entrambe si sposterebbero di un'ora
fra inverno ed estate. Vedi `docs/domini/orari-di-sessione-e-fusi.md`.

Questo script converte quindi ogni istante in UTC **vero**, rispettando l'ora
legale europea anno per anno (`zoneinfo`). Il feed prodotto contiene solo UTC, e
`feed-clocks.json` lo dichiara come tale: nessuna conversione residua a valle.

**2. Il timestamp di una riga e' la FINE del minuto, non l'inizio.** La prova sta
nella distribuzione degli orari: su 200.000 righe di @NQ ci sono 201 righe alle
`00:01` e solo 4 alle `00:00`. La riapertura Globex e' esattamente la mezzanotte
europea, e la prima barra della sessione — quella che copre 00:00→00:01 — e'
stampata `00:01`. Con timestamp di inizio le due frequenze sarebbero simili.

Di conseguenza il bucket di una riga e' `floor(t - 1 minuto)`, non `floor(t)`:
la riga `00:15` chiude la barra 15m che comincia alle `00:00`, non ne apre una
nuova. **Il vecchio `aggregate_nq_ascii.py` usava `floor(t)`**, quindi il feed
gerarchico che sta ancora in `datafeed/{tf}/{symbol}/` e' sfasato di un minuto
per bucket. E' uno dei motivi per cui questo script lo sostituisce invece di
affiancarlo.


## Dove cadono i bucket

L'allineamento e' sulla **mezzanotte locale del feed** (CET), non su quella UTC,
e poi l'inizio del bucket viene tradotto in UTC. Non e' un dettaglio estetico:

- e' l'allineamento su cui girano i run di ricerca da cui le strategie PTS sono
  state portate (`ZonedWindow.ResearchSession()`, il giorno di calendario
  europeo);
- per il giornaliero e' l'unico che tiene insieme la sessione. Su questo feed la
  pausa di manutenzione CME cade a cavallo della mezzanotte europea, quindi il
  giorno di calendario locale contiene la sessione intera, dalla riapertura
  serale alla chiusura del pomeriggio dopo. Tagliare a mezzanotte UTC la
  spezzerebbe in due.

Per i timeframe che dividono l'ora (5, 15, 30, 60) i due allineamenti
coincidono comunque, perche' l'offset CET e' un numero intero di ore. Divergono
da 4h in su — ed e' li' che la scelta conta.

Il settimanale non si genera: l'allineamento parte dalla mezzanotte, quindi non
sa dove comincia la settimana, e nessuna strategia lo chiede.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import sys
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from decimal import Decimal
from pathlib import Path
from typing import Iterator, TextIO
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE_DIRECTORY = REPOSITORY_ROOT / "datafeed-future" / "FUTURES_Historical_Data"
DEFAULT_OUTPUT_ROOT = REPOSITORY_ROOT / "datafeed"

# I CSV del vendor sono stampati in ora dell'Europa continentale (CET d'inverno,
# CEST d'estate). Vedi il docstring in testa per le due misure che lo stabiliscono.
DEFAULT_SOURCE_TIMEZONE = "Europe/Rome"

# Cartella e barType per timeframe, come li attende `DataSourceRepository`
# (`CanonicalBarTypes`): un nome inventato qui renderebbe il feed illeggibile.
CANONICAL_BAR_TYPES: dict[int, str] = {
    1: "OneMinute",
    5: "FiveMinute",
    15: "FifteenMinute",
    30: "ThirtyMinute",
    60: "OneHour",
    240: "FourHour",
    1440: "Daily",
}

# Nome del CSV sorgente per simbolo. Il vendor non ha un solo schema di nome —
# alcuni file portano la borsa nel nome, altri no — quindi la mappa e' esplicita
# invece che ricostruita a pattern.
SYMBOL_SOURCES: dict[str, str] = {
    "@BP": "@BP-Minute-Trade.csv",
    "@BTC": "@BTC-Minute-Trade.csv",
    "@CL": "@CL-ASCII Mapping-NYMEX-Futures-Minute-Trade.csv",
    "@EC": "@EC-Minute-Trade.csv",
    "@ES": "@ES-Minute-Trade.csv",
    "@FDAX": "@FDAX-Minute-Trade.csv",
    "@GC": "@GC-Minute-Trade.csv",
    "@JY": "@JY-Minute-Trade.csv",
    "@NG": "@NG-Minute-Trade.csv",
    "@NQ": "@NQ-ASCII Mapping-CME-Futures-Minute-Trade.csv",
    "@PL": "@PL-ASCII Mapping-NYMEX-Futures-Minute-Trade.csv",
    "@YM": "@YM-ASCII Mapping-CBOT-Futures-Minute-Trade.csv",
}

# Timeframe dichiarati dalle strategie del catalogo (`Symbol` + `TimeframeMinutes`
# di ogni PTS_*). Generare solo questi tiene il feed alla dimensione minima utile:
# ogni timeframe in piu' e' un file che nessuno legge ma che pesa quanto gli altri.
# @EC non ha ancora strategie: gli si da' il set completo degli altri indici,
# perche' e' il simbolo su cui si sta lavorando.
SYMBOL_TIMEFRAMES: dict[str, tuple[int, ...]] = {
    "@BP": (15, 60),
    "@BTC": (240,),
    "@CL": (30,),
    "@EC": (15, 30, 60, 240, 1440),
    "@ES": (15, 60, 240, 1440),
    "@FDAX": (240,),
    "@GC": (30, 60, 240),
    "@JY": (240,),
    "@NG": (240,),
    "@NQ": (15, 30, 60, 240, 1440),
    "@PL": (240,),
    "@YM": (240,),
}

UTC = timezone.utc


class SourceClock:
    """
    Traduce l'orario di parete del CSV nell'istante UTC che gli corrisponde.

    Due casi vanno decisi invece che subiti, perche' `zoneinfo` da solo li
    risolve in silenzio e in modo arbitrario:

    - **ora ambigua** (il ritorno all'ora solare: le 02:30 esistono due volte).
      Si sceglie l'occorrenza che tiene il tempo monotono, cioe' la prima finche'
      il CSV non torna indietro, poi la seconda.
    - **ora inesistente** (il passaggio all'ora legale: le 02:30 non esistono).
      Non dovrebbe capitare — i mercati sono chiusi la domenica mattina — ma se
      capita si conta e si segnala, invece di far finta di niente.
    """

    def __init__(self, zone: ZoneInfo) -> None:
        self._zone = zone
        self.ambiguous = 0
        self.nonexistent = 0

    def to_utc(self, local_naive: datetime, previous_utc: datetime | None) -> datetime:
        first = local_naive.replace(tzinfo=self._zone).astimezone(UTC)

        # Round-trip: se ritornando nel fuso locale l'orario cambia, l'orario di
        # partenza non esiste (buco dell'ora legale).
        if first.astimezone(self._zone).replace(tzinfo=None) != local_naive:
            self.nonexistent += 1
            return first

        if previous_utc is not None and first <= previous_utc:
            second = local_naive.replace(tzinfo=self._zone, fold=1).astimezone(UTC)
            if second > previous_utc:
                self.ambiguous += 1
                return second

        return first


@dataclass
class Candle:
    """
    Barra in costruzione. Prezzi tenuti come coppia (numero, testo di origine):
    il numero serve a confrontare massimi e minimi senza pagare `Decimal` su
    decine di milioni di righe, il testo a riscrivere in uscita esattamente la
    cifra del sorgente, senza il rumore di un giro attraverso `float`.
    """

    start_utc: datetime
    open_text: str
    high: float
    high_text: str
    low: float
    low_text: str
    close_text: str
    volume: int

    def update(
        self,
        high: float,
        high_text: str,
        low: float,
        low_text: str,
        close_text: str,
        volume: int,
    ) -> None:
        if high > self.high:
            self.high, self.high_text = high, high_text
        if low < self.low:
            self.low, self.low_text = low, low_text
        self.close_text = close_text
        self.volume += volume


def json_number(text: str) -> str:
    """Normalizza la cifra del CSV (`1.64800` -> `1.648`) restando in testo."""
    value = Decimal(text)
    normalized = value.normalize()
    # `normalize()` porta gli interi in notazione esponenziale (`4050` -> `4.05E+3`).
    if normalized == normalized.to_integral_value():
        return str(normalized.quantize(Decimal(1)))
    return format(normalized, "f")


@dataclass
class TimeframeFeed:
    """Accumula le barre di un timeframe e le scrive man mano su un unico file.

    La scrittura e' incrementale di proposito: il 15m di @NQ sono quasi mezzo
    milione di barre, e tenerle tutte in memoria per poi serializzarle in blocco
    costerebbe qualche gigabyte per niente. L'unico campo che si conosce solo
    alla fine e' `candleCount`, quindi va in coda all'oggetto invece che in testa
    — l'ordine delle chiavi in JSON non significa nulla.
    """

    symbol: str
    timeframe_minutes: int
    destination: Path
    candles: int = 0
    _current: Candle | None = field(default=None, init=False)
    _output: TextIO | None = field(default=None, init=False)
    _temporary: Path | None = field(default=None, init=False)

    @property
    def bar_type(self) -> str:
        return CANONICAL_BAR_TYPES[self.timeframe_minutes]

    def open(self) -> None:
        self.destination.parent.mkdir(parents=True, exist_ok=True)
        self._temporary = self.destination.with_suffix(".json.tmp")
        self._output = self._temporary.open("w", encoding="utf-8", newline="\n")
        self._output.write(
            "{"
            f'"symbol":{json.dumps(self.symbol)},'
            f'"barType":"{self.bar_type}",'
            f'"timeframeMinutes":{self.timeframe_minutes},'
            '"barEnd":null,'
            f'"lastUpdate":"{datetime.now(UTC).strftime("%Y-%m-%dT%H:%M:%SZ")}",'
            '"candles":['
        )

    def add(
        self,
        row_utc: datetime,
        minutes_into_bucket: int,
        open_text: str,
        high: float,
        high_text: str,
        low: float,
        low_text: str,
        close_text: str,
        volume: int,
    ) -> None:
        # L'inizio del bucket si ricava dall'istante UTC della riga, non
        # riconvertendo l'orario locale: cosi' l'ora legale e' gia' stata risolta
        # una volta sola, sulla riga, e non puo' risolversi in modo diverso qui.
        start_utc = row_utc - timedelta(minutes=minutes_into_bucket + 1)

        if self._current is not None and self._current.start_utc == start_utc:
            self._current.update(high, high_text, low, low_text, close_text, volume)
            return

        if self._current is not None:
            self._write(self._current)
        self._current = Candle(
            start_utc, open_text, high, high_text, low, low_text, close_text, volume
        )

    def finish(self) -> None:
        if self._current is not None:
            self._write(self._current)
            self._current = None

        assert self._output is not None and self._temporary is not None
        self._output.write(f'],"candleCount":{self.candles}}}\n')
        self._output.flush()
        os.fsync(self._output.fileno())
        self._output.close()
        self._output = None
        self._temporary.replace(self.destination)

    def _write(self, candle: Candle) -> None:
        assert self._output is not None
        separator = "" if self.candles == 0 else ","
        stamp = candle.start_utc.strftime("%Y-%m-%dT%H:%M:%SZ")
        self._output.write(
            f'{separator}\n{{"timestamp":{int(candle.start_utc.timestamp())},'
            f'"dateTime":"{stamp}",'
            f'"dateTimeFormatted":"{candle.start_utc.strftime("%Y-%m-%d %H:%M:%S")}",'
            f'"open":{json_number(candle.open_text)},'
            f'"high":{json_number(candle.high_text)},'
            f'"low":{json_number(candle.low_text)},'
            f'"close":{json_number(candle.close_text)},'
            f'"volume":{candle.volume},'
            '"volumeHigh":null,"volumeLow":null}'
        )
        self.candles += 1

    def abort(self) -> None:
        if self._output is not None:
            self._output.close()
            self._output = None
        if self._temporary is not None and self._temporary.exists():
            self._temporary.unlink()


def read_minutes(csv_path: Path, clock: SourceClock) -> Iterator[tuple[datetime, datetime, str, float, str, float, str, str, int]]:
    """
    Restituisce, riga per riga: (istante UTC di FINE minuto, orario locale di fine
    minuto, open, high, testo high, low, testo low, close, volume).

    `csv.reader` invece di `DictReader`: sono decine di milioni di righe per
    simbolo e la costruzione di un dict per riga si sente.
    """
    previous_utc: datetime | None = None

    with csv_path.open("r", encoding="utf-8-sig", newline="") as source:
        reader = csv.reader(source)
        header = next(reader, None)
        if header is None:
            return
        expected = ["Date", "Time", "Open", "High", "Low", "Close", "TotalVolume"]
        if [column.strip().strip('"') for column in header] != expected:
            raise ValueError(f"Intestazione inattesa in {csv_path.name}: {header}")

        for row_number, row in enumerate(reader, start=2):
            if not row or len(row) < 7:
                continue
            try:
                # Il vendor scrive giorno/mese/anno: le righe `13/01/2006` presenti
                # nel file escludono il formato americano.
                local_naive = datetime.strptime(f"{row[0]} {row[1]}", "%d/%m/%Y %H:%M:%S")
                high, low = float(row[3]), float(row[4])
                volume = int(row[6])
            except ValueError as error:
                raise ValueError(f"Riga {row_number} non valida in {csv_path.name}: {row}") from error

            row_utc = clock.to_utc(local_naive, previous_utc)
            if previous_utc is not None and row_utc < previous_utc:
                raise ValueError(
                    f"CSV non ordinato alla riga {row_number} di {csv_path.name}: "
                    f"{row_utc.isoformat()} precede {previous_utc.isoformat()}."
                )
            previous_utc = row_utc

            yield row_utc, local_naive, row[2], high, row[3], low, row[4], row[5], volume


def minutes_into_bucket(local_minute_end: datetime, timeframe_minutes: int) -> int:
    """
    Quanti minuti separano l'inizio del bucket dall'inizio del minuto che sta
    arrivando. Il conto e' su `t - 1 minuto` perche' il timestamp del CSV e' la
    fine del minuto (vedi il docstring del modulo).
    """
    start = local_minute_end - timedelta(minutes=1)
    minutes_since_midnight = start.hour * 60 + start.minute
    return minutes_since_midnight % timeframe_minutes


def convert_symbol(
    symbol: str,
    timeframes: list[int],
    source_directory: Path,
    output_root: Path,
    zone: ZoneInfo,
    overwrite: bool,
) -> None:
    csv_name = SYMBOL_SOURCES.get(symbol)
    if csv_name is None:
        raise ValueError(f"Nessun CSV mappato per {symbol}. Aggiungerlo a SYMBOL_SOURCES.")

    csv_path = source_directory / csv_name
    if not csv_path.is_file():
        raise FileNotFoundError(f"CSV sorgente non trovato: {csv_path}")

    feeds = [
        TimeframeFeed(symbol, minutes, output_root / f"{symbol}_{minutes}.json")
        for minutes in timeframes
    ]

    # Il controllo sui file gia' presenti sta prima della lettura del CSV: la
    # stessa passata alimenta tutti i timeframe, e accorgersi a meta' corsa che
    # un file c'era gia' lascerebbe gli altri troncati a una data intermedia.
    if not overwrite:
        for feed in feeds:
            if feed.destination.exists():
                raise FileExistsError(
                    f"{feed.destination} esiste gia'. Usa --overwrite per rigenerarlo."
                )

    clock = SourceClock(zone)
    print(
        f"{symbol}: {csv_path.name} -> "
        + ", ".join(f"{feed.destination.name}" for feed in feeds),
        flush=True,
    )

    for feed in feeds:
        feed.open()

    try:
        rows = 0
        for row_utc, local_naive, open_text, high, high_text, low, low_text, close_text, volume in read_minutes(csv_path, clock):
            rows += 1
            for feed in feeds:
                feed.add(
                    row_utc,
                    minutes_into_bucket(local_naive, feed.timeframe_minutes),
                    open_text,
                    high,
                    high_text,
                    low,
                    low_text,
                    close_text,
                    volume,
                )

        for feed in feeds:
            feed.finish()
    except BaseException:
        for feed in feeds:
            feed.abort()
        raise

    print(f"  {rows:,} minuti letti", flush=True)
    if clock.ambiguous or clock.nonexistent:
        print(
            f"  ora legale: {clock.ambiguous:,} minuti nell'ora ripetuta, "
            f"{clock.nonexistent:,} in quella inesistente",
            flush=True,
        )
    for feed in feeds:
        size_mb = feed.destination.stat().st_size / (1024 * 1024)
        print(f"  {feed.destination.name}: {feed.candles:,} barre, {size_mb:,.1f} MB", flush=True)


def resolve_timezone(name: str) -> ZoneInfo:
    try:
        return ZoneInfo(name)
    except ZoneInfoNotFoundError as error:
        raise ValueError(f"Fuso IANA non valido: {name}") from error


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "--source-directory",
        type=Path,
        default=DEFAULT_SOURCE_DIRECTORY,
        help="Cartella dei CSV minute del vendor.",
    )
    parser.add_argument(
        "--output-root",
        type=Path,
        default=DEFAULT_OUTPUT_ROOT,
        help="Cartella di destinazione, quella che il server configura come RepositoryPath.",
    )
    parser.add_argument(
        "--source-timezone",
        default=DEFAULT_SOURCE_TIMEZONE,
        help=f"Fuso IANA in cui e' stampato il CSV. Default: {DEFAULT_SOURCE_TIMEZONE}.",
    )
    parser.add_argument(
        "--symbols",
        nargs="+",
        metavar="SIMBOLO",
        help="Simboli da convertire. Default: tutti quelli usati dal catalogo strategie.",
    )
    parser.add_argument(
        "--timeframes",
        type=int,
        nargs="+",
        metavar="MINUTI",
        help="Forza i timeframe per tutti i simboli richiesti, invece di quelli del catalogo.",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Sostituisce i file gia' presenti.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    zone = resolve_timezone(args.source_timezone)

    symbols = args.symbols or sorted(SYMBOL_TIMEFRAMES)
    symbols = [symbol if symbol.startswith("@") else "@" + symbol for symbol in symbols]

    for symbol in symbols:
        if symbol not in SYMBOL_TIMEFRAMES and not args.timeframes:
            raise ValueError(
                f"{symbol} non e' nel catalogo: indica i timeframe con --timeframes."
            )

    for minutes in args.timeframes or []:
        if minutes not in CANONICAL_BAR_TYPES:
            supported = ", ".join(str(key) for key in sorted(CANONICAL_BAR_TYPES))
            raise ValueError(f"Timeframe {minutes} non riconosciuto. Ammessi: {supported}.")
        # Vincolo su cui poggia l'allineamento: se il timeframe non divide la
        # giornata, le barre scivolerebbero di giorno in giorno invece di
        # ripartire dalla mezzanotte.
        if 1440 % minutes != 0:
            raise ValueError(f"Timeframe {minutes} non divide la giornata in parti uguali.")

    started = datetime.now(UTC)
    for symbol in symbols:
        timeframes = sorted(args.timeframes or SYMBOL_TIMEFRAMES[symbol])
        convert_symbol(
            symbol, timeframes, args.source_directory, args.output_root, zone, args.overwrite
        )

    elapsed = datetime.now(UTC) - started
    print(f"Fatto in {elapsed}.", flush=True)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, FileNotFoundError, FileExistsError) as error:
        print(f"Errore: {error}", file=sys.stderr)
        raise SystemExit(1)
