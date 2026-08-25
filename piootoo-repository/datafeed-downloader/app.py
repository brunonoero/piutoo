"""
Piootoo Datafeed Downloader - form web
----------------------------------------
Interfaccia web (Flask) per scegliere symbol e timeframe e scaricare lo
storico OHLCV da Yahoo Finance. La logica di download/JSON è in core.py
(condivisa con lo script da terminale download_cli.py).

Uso:
    pip install -r requirements.txt
    python app.py
    apri http://localhost:5050 nel browser
"""

import os

from flask import Flask, jsonify, request, send_from_directory

import core

APP_DIR = os.path.dirname(os.path.abspath(__file__))
DATAFEED_DIR = core.DEFAULT_DATAFEED_DIR

app = Flask(__name__, static_folder=None)


@app.route("/")
def index():
    return send_from_directory(APP_DIR, "index.html")


@app.route("/api/download", methods=["POST"])
def api_download():
    payload = request.get_json(force=True) or {}
    symbols = [s.strip() for s in payload.get("symbols", []) if s.strip()]
    timeframes = [int(t) for t in payload.get("timeframes", [])]
    days_back = int(payload.get("daysBack") or core.DEFAULT_LOOKBACK_DAYS)

    if not symbols or not timeframes:
        return jsonify({"error": "Seleziona almeno un symbol e un timeframe."}), 400

    results = [
        core.download_and_save(symbol, tf, days_back, DATAFEED_DIR)
        for symbol in symbols
        for tf in timeframes
    ]

    return jsonify({"results": results, "datafeedDir": DATAFEED_DIR})


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5050, debug=False)
