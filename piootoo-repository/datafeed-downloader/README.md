# Piootoo Datafeed Downloader

Progetto separato per scaricare lo storico OHLCV da Yahoo Finance e salvarlo
come JSON nella cartella `../datafeed`, con uno schema allineato a
`OhlcvDto`/`ClosedBarDto` usati nel cBot di trading/backtesting
(`ctrader/PiootooLiveTradingBot.cs`, righe ~641-666).

## Avvio - form web

```bash
cd datafeed-downloader
python -m venv .venv          # opzionale ma consigliato
.venv\Scripts\activate        # Windows
pip install -r requirements.txt
python app.py
```

Poi apri **http://localhost:5050** nel browser: form per scegliere symbol
(ticker Yahoo Finance), timeframe (minuti) e range di storico.

## Avvio - script da terminale

Alternativa senza browser, stessa logica e stesso output JSON (`core.py` è
condiviso tra i due):

```bash
cd datafeed-downloader
pip install -r requirements.txt

# non interattivo
python download_cli.py --symbols GC=F,CL=F,NQ=F --timeframes 15,60,1440
python download_cli.py --symbols GC=F --timeframes 15 --days-back 365
python download_cli.py --symbols GC=F --timeframes 15 --output-dir /altro/percorso

# interattivo (chiede symbol/timeframe a prompt se omessi)
python download_cli.py
```

## Default

- Range storico: **730 giorni (~2 anni)**, modificabile nel form.
- Timeframe pre-selezionati: 5, 15, 30, 60, 1440 minuti (ricavati dalle
  strategie in `/easy`, es. `s_TOP_UA_..._GC_15__7.txt` → GC, 15 minuti).
- Timeframe non nativi su Yahoo (es. 10, 120 minuti) vengono ricavati
  aggregando (resample) il dato nativo più fine disponibile (5m, 60m).

## Limiti di Yahoo Finance sullo storico intraday

| Intervallo nativo | Storico massimo |
|---|---|
| 1m  | 7 giorni |
| 5m / 15m / 30m | 60 giorni |
| 60m | 730 giorni |
| 1d  | nessun limite pratico |

Se il range richiesto supera il limite, viene accorciato automaticamente;
la cosa viene segnalata sia nella tabella risultati del form sia nel campo
`note` del JSON prodotto.

## Symbol: root futures → ticker Yahoo

Le strategie in `/easy` usano root futures CME/ICE (es. `GC`, `CL`, `NQ`)
che **non** sono ticker Yahoo diretti. Mappatura indicativa (verificare
prima dell'uso):

| Root strategia | Ticker Yahoo |
|---|---|
| GC (Gold) | `GC=F` |
| CL (Crude Oil) | `CL=F` |
| NQ (Nasdaq) | `NQ=F` |
| ES (S&P 500) | `ES=F` |
| HG (Copper) | `HG=F` |
| PL (Platinum) | `PL=F` |
| NG (Natural Gas) | `NG=F` |
| RB (RBOB Gasoline) | `RB=F` |
| HO (Heating Oil) | `HO=F` |
| S (Soybeans) | `ZS=F` |
| US (30y T-Bond) | `ZB=F` |
| EC (Euro FX) | `6E=F` |
| BP (British Pound) | `6B=F` |
| JY (Japanese Yen) | `6J=F` |
| LC (Live Cattle) | `LE=F` |
| FC (Feeder Cattle) | `GF=F` |
| FDAX (DAX futures) | `^GDAXI` (indice, non il future) |
| FGBL (Euro-Bund) | nessun ticker Yahoo affidabile |
| BTCUSDT / ETHUSDT | `BTC-USD` / `ETH-USD` |
| TSLA | `TSLA` |

## Struttura JSON prodotta

Un file per ogni combinazione symbol+timeframe, in `../datafeed/<symbol>_<timeframeMinutes>.json`:

```json
{
  "symbol": "GC=F",
  "timeframeMinutes": 15,
  "source": "yahoo-finance",
  "generatedAtUtc": "2026-07-26T10:00:00Z",
  "requestedStartUtc": "2024-07-27T10:00:00Z",
  "effectiveStartUtc": "2024-07-27T10:00:00Z",
  "note": null,
  "bars": [
    { "dateTime": "2024-07-27T10:00:00Z", "open": 2380.5, "high": 2382.1, "low": 2379.8, "close": 2381.0, "volume": 1234.0 }
  ]
}
```

Il campo `bars[]` rispecchia `OhlcvDto` (dateTime, open, high, low, close,
volume); `symbol`/`timeframeMinutes` rispecchiano i campi corrispondenti di
`ClosedBarDto`.
