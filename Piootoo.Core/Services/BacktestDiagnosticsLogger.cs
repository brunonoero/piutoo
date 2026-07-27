using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Trading;

namespace Piootoo.Core.Services;

/// <summary>
/// Raccoglie la traccia diagnostica di un backtest e la materializza in due file nella cartella
/// del backtest: un JSONL append-only con gli eventi rilevanti e un riepilogo con i contatori
/// aggregati per strategia.
///
/// Regola di progetto: gli eventi ad alta frequenza (valutazioni che restituiscono Hold, skip per
/// dati insufficienti, disallineamenti di timeframe) NON producono righe di log — verrebbero
/// milioni di righe e il segnale utile sparirebbe. Vengono contati e riportati nel riepilogo.
/// Nel log finiscono solo segnali, ingressi, uscite, esiti dei datasource e anomalie.
///
/// Il writer è bufferizzato e non forza il flush su disco a ogni riga: il costo per barra deve
/// restare trascurabile rispetto alla valutazione delle strategie.
/// </summary>
public sealed class BacktestDiagnosticsLogger : IDisposable
{
    private static readonly JsonSerializerOptions EventJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions SummaryJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _gate = new();
    private readonly Dictionary<string, BacktestStrategySummary> _strategies = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<BacktestDataSourceSummary> _dataSources = [];
    private readonly string _jobId;
    private readonly string _logPath;
    private readonly string _summaryPath;
    private readonly StreamWriter? _writer;
    private long _sequence;
    private bool _disposed;

    public BacktestDiagnosticsLogger(string directory, string jobId)
    {
        _jobId = jobId;
        Directory.CreateDirectory(directory);
        _logPath = Path.Combine(directory, BacktestDiagnosticsSchema.LogFileName);
        _summaryPath = Path.Combine(directory, BacktestDiagnosticsSchema.SummaryFileName);

        try
        {
            var stream = new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.Read, 128 * 1024);
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = false };
        }
        catch (IOException)
        {
            // La diagnostica non deve mai far fallire un backtest: se il file non è apribile
            // si continua a raccogliere i contatori in memoria per il riepilogo.
            _writer = null;
        }
    }

    public string LogPath => _logPath;
    public string SummaryPath => _summaryPath;

    public static string MakeKey(string symbol, string strategyCode) =>
        $"{NormalizeSymbol(symbol)}|{strategyCode.Trim()}";

    private static string NormalizeSymbol(string symbol) =>
        (symbol ?? string.Empty).Trim().TrimStart('@').ToUpperInvariant();

    /// <summary>Dichiara una strategia al riepilogo. Va chiamata anche per le strategie che non produrranno nulla.</summary>
    public void RegisterStrategy(string strategyCode, string strategyName, string symbol, int timeframeMinutes)
    {
        lock (_gate)
        {
            var key = MakeKey(symbol, strategyCode);
            if (_strategies.ContainsKey(key)) return;
            _strategies[key] = new BacktestStrategySummary
            {
                StrategyCode = strategyCode,
                StrategyName = strategyName,
                Symbol = NormalizeSymbol(symbol),
                TimeframeMinutes = timeframeMinutes
            };
        }
    }

    private BacktestStrategySummary? Find(string symbol, string strategyCode) =>
        _strategies.TryGetValue(MakeKey(symbol, strategyCode), out var summary) ? summary : null;

    // ---------------------------------------------------------------- contatori ad alta frequenza

    public void CountScheduled(string symbol, string strategyCode)
    {
        lock (_gate) { var s = Find(symbol, strategyCode); if (s != null) s.Scheduled++; }
    }

    public void CountEvaluation(string symbol, string strategyCode)
    {
        lock (_gate) { var s = Find(symbol, strategyCode); if (s != null) s.Evaluations++; }
    }

    public void CountSkipNoData(string symbol, string strategyCode)
    {
        lock (_gate) { var s = Find(symbol, strategyCode); if (s != null) s.SkippedNoData++; }
    }

    public void CountSkipNotEnoughCandles(string symbol, string strategyCode)
    {
        lock (_gate) { var s = Find(symbol, strategyCode); if (s != null) s.SkippedNotEnoughCandles++; }
    }

    public void CountSkipStaleCandle(string symbol, string strategyCode)
    {
        lock (_gate) { var s = Find(symbol, strategyCode); if (s != null) s.SkippedStaleCandle++; }
    }

    public void CountHold(string symbol, string strategyCode)
    {
        lock (_gate) { var s = Find(symbol, strategyCode); if (s != null) s.HoldSignals++; }
    }

    public void CountError(string symbol, string strategyCode, DateTime barTimeUtc, Exception exception)
    {
        lock (_gate)
        {
            var s = Find(symbol, strategyCode);
            if (s != null) s.Errors++;
        }

        Write(new BacktestLogEvent
        {
            Type = BacktestLogEventType.Anomaly,
            JobId = _jobId,
            BarTimeUtc = barTimeUtc,
            StrategyCode = strategyCode,
            Symbol = NormalizeSymbol(symbol),
            Message = $"{exception.GetType().Name}: {exception.Message}"
        });
    }

    // ---------------------------------------------------------------- eventi

    public void LogRun(string message, IReadOnlyDictionary<string, string>? data = null) =>
        Write(new BacktestLogEvent
        {
            Type = BacktestLogEventType.Run,
            JobId = _jobId,
            Message = message,
            Data = data
        });

    public void LogDataSource(BacktestDataSourceSummary summary)
    {
        lock (_gate) _dataSources.Add(summary);

        Write(new BacktestLogEvent
        {
            Type = BacktestLogEventType.DataSource,
            JobId = _jobId,
            Symbol = summary.Symbol,
            TimeframeMinutes = summary.TimeframeMinutes,
            Message = summary.Warning ?? "ok",
            Data = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["candles"] = summary.CandleCount.ToString(),
                ["firstBarUtc"] = summary.FirstBarUtc?.ToString("O") ?? "",
                ["lastBarUtc"] = summary.LastBarUtc?.ToString("O") ?? "",
                ["coversRequestedRange"] = summary.CoversRequestedRange ? "true" : "false"
            }
        });
    }

    public void LogSignal(TradeSignal signal, string strategyCode, string symbol, int timeframeMinutes, DateTime barTimeUtc)
    {
        lock (_gate)
        {
            var s = Find(symbol, strategyCode);
            if (s != null)
            {
                if (signal.CloseOnly) s.CloseOnlySignals++;
                else if (signal.Type == SignalType.Buy) s.BuySignals++;
                else if (signal.Type == SignalType.Sell) s.SellSignals++;

                s.FirstSignalUtc ??= barTimeUtc;
                s.LastSignalUtc = barTimeUtc;
            }
        }

        Write(new BacktestLogEvent
        {
            Type = BacktestLogEventType.Signal,
            JobId = _jobId,
            BarTimeUtc = barTimeUtc,
            StrategyCode = strategyCode,
            StrategyName = signal.StrategyName,
            Symbol = NormalizeSymbol(symbol),
            TimeframeMinutes = timeframeMinutes,
            Side = signal.Type,
            OrderType = signal.OrderType,
            Price = signal.Price,
            Quantity = signal.Quantity,
            StopLossPoints = signal.StopLoss,
            TakeProfitPoints = signal.TakeProfit,
            Message = signal.Reason,
            Data = signal.CloseOnly
                ? new Dictionary<string, string>(StringComparer.Ordinal) { ["closeOnly"] = "true" }
                : null
        });
    }

    public void LogEntry(PositionOpenedEvent opened) =>
        Write(new BacktestLogEvent
        {
            Type = BacktestLogEventType.Entry,
            JobId = _jobId,
            BarTimeUtc = opened.EntryTimeUtc,
            StrategyCode = opened.StrategyCode,
            StrategyName = opened.StrategyName,
            Symbol = NormalizeSymbol(opened.Symbol),
            Side = opened.Direction,
            Price = opened.EntryPrice,
            Quantity = opened.Contracts,
            StopLossPoints = opened.StopLossPoints,
            TakeProfitPoints = opened.TakeProfitPoints,
            Message = opened.Reason
        });

    public void LogExit(PositionClosedEvent closed)
    {
        lock (_gate)
        {
            var s = Find(closed.Symbol, closed.StrategyCode);
            if (s != null)
            {
                s.Trades++;
                if (closed.NetProfit > 0) s.WinningTrades++;
                else if (closed.NetProfit < 0) s.LosingTrades++;
                s.GrossProfit += closed.GrossProfit;
                s.NetProfit += closed.NetProfit;
                s.Commission += closed.Commission;

                var reason = closed.ExitReason.ToString();
                s.ExitReasons[reason] = s.ExitReasons.GetValueOrDefault(reason) + 1;
            }
        }

        Write(new BacktestLogEvent
        {
            Type = BacktestLogEventType.Exit,
            JobId = _jobId,
            BarTimeUtc = closed.ExitTimeUtc,
            StrategyCode = closed.StrategyCode,
            StrategyName = closed.StrategyName,
            Symbol = NormalizeSymbol(closed.Symbol),
            Side = closed.Direction,
            Price = closed.ExitPrice,
            Quantity = closed.Contracts,
            EntryTimeUtc = closed.EntryTimeUtc,
            EntryPrice = closed.EntryPrice,
            BarsInPosition = closed.BarsInPosition,
            ExitReason = closed.ExitReason,
            GrossProfit = closed.GrossProfit,
            NetProfit = closed.NetProfit,
            Commission = closed.Commission,
            Balance = closed.BalanceAfter
        });
    }

    public void LogAnomaly(string message, DateTime? barTimeUtc = null, string? strategyCode = null, string? symbol = null) =>
        Write(new BacktestLogEvent
        {
            Type = BacktestLogEventType.Anomaly,
            JobId = _jobId,
            BarTimeUtc = barTimeUtc,
            StrategyCode = strategyCode,
            Symbol = symbol is null ? null : NormalizeSymbol(symbol),
            Message = message
        });

    private void Write(BacktestLogEvent logEvent)
    {
        if (_writer is null) return;
        lock (_gate)
        {
            if (_disposed) return;
            logEvent.Sequence = ++_sequence;
            try
            {
                _writer.WriteLine(JsonSerializer.Serialize(logEvent, EventJsonOptions));
            }
            catch (IOException)
            {
                // Vedi nota nel costruttore: la diagnostica non interrompe mai il backtest.
            }
        }
    }

    /// <summary>Svuota il buffer del writer. Da chiamare ai checkpoint, non a ogni barra.</summary>
    public void Flush()
    {
        if (_writer is null) return;
        lock (_gate)
        {
            if (_disposed) return;
            try { _writer.Flush(); }
            catch (IOException) { }
        }
    }

    /// <summary>
    /// Chiude la traccia, calcola le diagnosi automatiche e scrive il riepilogo.
    /// Restituisce il riepilogo completo così il chiamante può allegarlo al risultato.
    /// </summary>
    public BacktestRunSummary Complete(BacktestRunSummary summary)
    {
        lock (_gate)
        {
            var strategies = _strategies.Values
                .OrderBy(x => x.Symbol, StringComparer.Ordinal)
                .ThenBy(x => x.StrategyCode, StringComparer.Ordinal)
                .ToList();

            foreach (var strategy in strategies)
                strategy.Diagnosis = Diagnose(strategy);

            summary.DataSources = _dataSources
                .OrderBy(x => x.Symbol, StringComparer.Ordinal)
                .ThenBy(x => x.TimeframeMinutes)
                .ToList();
            summary.Strategies = strategies;
            summary.TotalTrades = strategies.Sum(x => x.Trades);
            summary.WinningTrades = strategies.Sum(x => x.WinningTrades);
            summary.LosingTrades = strategies.Sum(x => x.LosingTrades);
            summary.Diagnostics = BuildRunDiagnostics(summary, strategies);

            Write(new BacktestLogEvent
            {
                Type = BacktestLogEventType.Run,
                JobId = _jobId,
                Message = $"fine job: {summary.Outcome}, {summary.TotalTrades} trade, " +
                          $"{summary.Diagnostics.Count} diagnosi"
            });

            try
            {
                _writer?.Flush();
                AtomicFileWriter.WriteAllText(_summaryPath, JsonSerializer.Serialize(summary, SummaryJsonOptions));
            }
            catch (IOException)
            {
                // idem
            }

            return summary;
        }
    }

    private static string? Diagnose(BacktestStrategySummary s)
    {
        if (s.Scheduled == 0)
            return "mai allineata a una barra del loop: verifica che il timeframe della strategia sia " +
                   "multiplo del timeframe minimo del portafoglio.";

        if (s.Evaluations == 0)
        {
            if (s.SkippedNoData == s.Scheduled)
                return "mai valutata: datasource assente o vuoto per questa coppia simbolo/timeframe.";
            if (s.SkippedNotEnoughCandles > 0 && s.SkippedNotEnoughCandles >= s.Scheduled - s.SkippedStaleCandle)
                return "mai valutata: candele disponibili sempre inferiori a RequiredCandles. " +
                       "Il feed non copre l'intervallo richiesto, oppure RequiredCandles è sovradimensionato.";
            if (s.SkippedStaleCandle > 0)
                return "mai valutata: ultima candela sempre troppo vecchia rispetto alla barra corrente.";
            return "mai valutata, motivo non classificato.";
        }

        if (s.Errors > 0 && s.Errors >= s.Evaluations)
            return "ogni valutazione ha sollevato un'eccezione: vedi gli eventi Anomaly nel log.";

        var signals = s.BuySignals + s.SellSignals + s.CloseOnlySignals;
        if (signals == 0)
            return $"valutata {s.Evaluations} volte senza mai emettere un segnale: le condizioni di " +
                   "ingresso non si verificano mai su questi dati.";

        if (s.BuySignals + s.SellSignals > 0 && s.Trades == 0)
            return $"{s.BuySignals + s.SellSignals} segnali di ingresso ma nessun trade chiuso: " +
                   "gli ordini non vengono riempiti (ordini stop mai toccati) oppure le posizioni " +
                   "restano aperte fino alla fine del run.";

        if (s.SkippedNotEnoughCandles > s.Evaluations)
            return $"valutata solo {s.Evaluations} volte su {s.Scheduled} occasioni: per la maggior " +
                   "parte del run mancavano candele a sufficienza.";

        return null;
    }

    private static List<string> BuildRunDiagnostics(
        BacktestRunSummary summary, IReadOnlyList<BacktestStrategySummary> strategies)
    {
        var diagnostics = new List<string>();

        foreach (var ds in summary.DataSources.Where(x => x.CandleCount == 0))
            diagnostics.Add($"[datafeed] nessuna candela per {ds.Symbol}/{ds.TimeframeMinutes}m: " +
                            "le strategie su questa coppia non potranno mai essere valutate.");

        foreach (var ds in summary.DataSources.Where(x => x.CandleCount > 0 && !x.CoversRequestedRange))
            diagnostics.Add($"[datafeed] {ds.Symbol}/{ds.TimeframeMinutes}m copre " +
                            $"{ds.FirstBarUtc:yyyy-MM-dd} → {ds.LastBarUtc:yyyy-MM-dd}, " +
                            "meno dell'intervallo richiesto dal backtest.");

        var mute = strategies.Where(x => x.Evaluations == 0).ToList();
        if (mute.Count == strategies.Count && strategies.Count > 0)
            diagnostics.Add("[strategie] nessuna strategia è mai stata valutata: il backtest non poteva " +
                            "produrre alcun risultato.");
        else if (mute.Count > 0)
            diagnostics.Add($"[strategie] {mute.Count} strategie su {strategies.Count} non sono mai state " +
                            $"valutate: {string.Join(", ", mute.Take(10).Select(x => x.StrategyCode))}" +
                            (mute.Count > 10 ? ", …" : ""));

        var silent = strategies
            .Where(x => x.Evaluations > 0 && x.BuySignals + x.SellSignals + x.CloseOnlySignals == 0)
            .ToList();
        if (silent.Count > 0)
            diagnostics.Add($"[segnali] {silent.Count} strategie valutate non hanno mai emesso un segnale: " +
                            $"{string.Join(", ", silent.Take(10).Select(x => x.StrategyCode))}" +
                            (silent.Count > 10 ? ", …" : ""));

        var unfilled = strategies.Where(x => x.BuySignals + x.SellSignals > 0 && x.Trades == 0).ToList();
        if (unfilled.Count > 0)
            diagnostics.Add($"[esecuzione] {unfilled.Count} strategie hanno prodotto segnali di ingresso " +
                            $"senza alcun trade chiuso: {string.Join(", ", unfilled.Take(10).Select(x => x.StrategyCode))}" +
                            (unfilled.Count > 10 ? ", …" : ""));

        var faulty = strategies.Where(x => x.Errors > 0).ToList();
        if (faulty.Count > 0)
            diagnostics.Add($"[errori] {faulty.Sum(x => x.Errors)} eccezioni durante la valutazione, " +
                            $"su {faulty.Count} strategie: {string.Join(", ", faulty.Take(10).Select(x => x.StrategyCode))}" +
                            (faulty.Count > 10 ? ", …" : ""));

        if (summary.OpenPositionsAtEnd > 0)
            diagnostics.Add($"[posizioni] {summary.OpenPositionsAtEnd} posizioni ancora aperte a fine run: " +
                            "il loro P&L non compare in trades.json e quindi non entra in Titano.");

        if (summary.ProcessedIterations == 0)
            diagnostics.Add("[loop] nessuna iterazione elaborata: intervallo di date vuoto o interamente " +
                            "coperto da weekend.");

        if (summary.MarkedToMarketBars == 0 && summary.ProcessedIterations > 0)
            diagnostics.Add("[loop] nessuna barra ha prodotto un prezzo utilizzabile: stop loss, take profit " +
                            "e time exit non sono mai stati verificati.");

        if (diagnostics.Count == 0)
            diagnostics.Add("Nessuna anomalia rilevata dai contatori.");

        return diagnostics;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _writer?.Flush(); } catch (IOException) { }
            _writer?.Dispose();
        }
    }
}

/// <summary>Notifica di apertura posizione emessa dall'engine di trading verso la diagnostica.</summary>
public sealed class PositionOpenedEvent
{
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public required SignalType Direction { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required decimal EntryPrice { get; init; }
    public required int Contracts { get; init; }
    public decimal? StopLossPoints { get; init; }
    public decimal? TakeProfitPoints { get; init; }
    public string? Reason { get; init; }
}

/// <summary>Notifica di chiusura posizione emessa dall'engine di trading verso la diagnostica.</summary>
public sealed class PositionClosedEvent
{
    public required string StrategyCode { get; init; }
    public required string StrategyName { get; init; }
    public required string Symbol { get; init; }
    public required SignalType Direction { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required DateTime ExitTimeUtc { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal ExitPrice { get; init; }
    public required int Contracts { get; init; }
    public required TradeExitReason ExitReason { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal NetProfit { get; init; }
    public decimal Commission { get; init; }
    public int BarsInPosition { get; init; }
    public decimal BalanceAfter { get; init; }
}
