using Piootoo.Core.Services.Interfaces;
using Piootoo.Shared.Configuration;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Shared.Models.Backtesting;
using Piootoo.Shared.Models.Trading;
using Piootoo.Shared.Utilities;

namespace Piootoo.Core.Services;

/// <summary>
/// Servizio per l'emulazione del trading
/// </summary>
public class PiootooTradingService : IPiootooTradingService
{
    private TradingState _state = new();
    private decimal _commissionPerContract = 2.0m; // Commissione per contratto
    private decimal _initialCapital = 0m; // Capitale iniziale per calcolare il profit totale
    private readonly List<TradingResult> _closedTrades = new();
    private readonly Dictionary<string, decimal> _strategyCashAdjustments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _lastPrices = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyDictionary<string, object?>> _strategyRuntimeStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PendingOrder> _pendingOrders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (DateTime Day, int Count)> _entriesByDay = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _entriesBySession = new(StringComparer.OrdinalIgnoreCase);

    private sealed class PendingOrder
    {
        public required string PositionKey { get; init; }
        public required TradeSignal Signal { get; set; }
    }

    /// <summary>
    /// Notifica di apertura posizione. L'engine non conosce la diagnostica: espone un hook e chi
    /// esegue il backtest ci collega il logger. Null = nessun osservatore, costo zero.
    /// </summary>
    public Action<PositionOpenedEvent>? PositionOpened { get; set; }

    /// <summary>Notifica di chiusura posizione, con il motivo dell'uscita.</summary>
    public Action<PositionClosedEvent>? PositionClosed { get; set; }

    /// <summary>
    /// Se il picco favorevole include l'estremo della barra in corso.
    ///
    /// <para>Con <c>true</c> — comportamento storico dell'engine — il picco viene aggiornato
    /// <i>prima</i> del controllo dello stop protettivo, quindi lo stop trascinato può scattare
    /// sulla stessa barra che ha segnato il nuovo massimo: è la politica intrabar conservativa,
    /// che assume il percorso avverso. Il motore di riferimento Python non esce mai in trailing
    /// sulla barra del nuovo estremo. Il campo esiste per <b>misurare</b> quanto vale quella
    /// convenzione a parità di ingressi, non per cambiarla di default.</para>
    /// </summary>
    public bool TrailingPeakIncludesCurrentBar { get; set; } = true;

    public void Initialize(decimal initialCapital, decimal commissionPerContract = 2.0m)
    {
        _commissionPerContract = commissionPerContract;
        _initialCapital = initialCapital;
        _state = new TradingState
        {
            Equity = initialCapital,
            Balance = initialCapital,
            MaxEquity = initialCapital,
            Drawdown = 0,
            OpenPositions = new Dictionary<string, OpenPosition>()
        };
        _closedTrades.Clear();
        _strategyCashAdjustments.Clear();
        _lastPrices.Clear();
        _strategyRuntimeStates.Clear();
        _pendingOrders.Clear();
        _entriesByDay.Clear();
        _entriesBySession.Clear();
    }

    public TradingSnapshot ProcessSignals(List<TradeSignal> signals, decimal currentPrice, DateTime currentTime)
    {
        var currentPrices = BuildCurrentPrices(signals, currentPrice);
        return ProcessSignals(signals, currentPrices, currentTime);
    }

    public StrategyExecutionSnapshot GetExecutionSnapshot(string strategyCode, string symbol, DateTime barTimeUtc)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var positionKey = MakePositionKey(normalizedSymbol, strategyCode);
        _state.OpenPositions.TryGetValue(positionKey, out var position);
        var day = TradingDateTime.ToFeedUtc(barTimeUtc).Date;
        var entriesToday = 0;
        if (_entriesByDay.TryGetValue(positionKey, out var tracked) && tracked.Day == day)
        {
            entriesToday = tracked.Count;
        }

        return new StrategyExecutionSnapshot
        {
            StrategyCode = strategyCode,
            Symbol = normalizedSymbol,
            BarTimeUtc = TradingDateTime.ToFeedUtc(barTimeUtc),
            DollarsPerPoint = GetContractPointValue(normalizedSymbol),
            EntriesToday = entriesToday,
            Position = position is null
                ? null
                : new StrategyPositionSnapshot
                {
                    Direction = position.Direction,
                    EntryPrice = position.EntryPrice,
                    EntryTimeUtc = position.EntryTime,
                    Contracts = position.Contracts,
                    BarsInPosition = position.BarsInPosition
                },
            RuntimeState = _strategyRuntimeStates.TryGetValue(positionKey, out var state)
                ? state
                : new Dictionary<string, object?>(StringComparer.Ordinal)
        };
    }

    public void CaptureStrategyRuntimeState(string strategyCode, string symbol, IReadOnlyDictionary<string, object?> runtimeState)
    {
        var key = MakePositionKey(NormalizeSymbol(symbol), strategyCode);
        _strategyRuntimeStates[key] = new Dictionary<string, object?>(runtimeState, StringComparer.Ordinal);
    }

    public TradingSnapshot ProcessSignals(List<TradeSignal> signals, Dictionary<string, decimal> currentPrices, DateTime currentTime)
    {
        return ProcessSignals(signals, currentPrices, new Dictionary<string, OhlcvData>(StringComparer.OrdinalIgnoreCase), currentTime);
    }

    public TradingSnapshot ProcessSignals(List<TradeSignal> signals, Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, DateTime currentTime)
    {
        currentTime = TradingDateTime.ToFeedUtc(currentTime);
        foreach (var signal in signals)
        {
            TradingDateTime.NormalizeSignalToUtc(signal);
        }

        currentPrices = NormalizeCurrentPrices(currentPrices);
        currentBars = NormalizeCurrentBars(currentBars);
        CheckStopLossAndTakeProfit(currentPrices, currentBars, 0m, currentTime);
        CheckTimeExits(currentPrices, currentBars, 0m, currentTime);
        TryFillPendingOrders(currentPrices, currentBars, currentTime);

        // Chiudi posizioni se necessario (es. segnale opposto)
        foreach (var signal in signals)
        {
            if (!IsSignalActive(signal, currentTime))
            {
                continue;
            }

            var signalSymbol = ResolveSignalSymbol(signal, currentPrices);
            var positionKey = MakePositionKey(signalSymbol, GetSignalStrategyCode(signal));
            if (_state.OpenPositions.TryGetValue(positionKey, out var position))
            {
                if (signal.ExitOnly &&
                    ((position.Direction == SignalType.Buy && signal.Type == SignalType.Sell) ||
                     (position.Direction == SignalType.Sell && signal.Type == SignalType.Buy)))
                {
                    ClosePosition(positionKey, ResolveSignalPrice(signal, currentPrices), currentTime,
                        TradeExitReason.OppositeSignal);
                    CancelPendingOrders(positionKey);
                    continue;
                }

                // Un segnale opposto chiude la posizione esistente e viene consumato.
                if ((position.Direction == SignalType.Buy && signal.Type == SignalType.Sell) ||
                    (position.Direction == SignalType.Sell && signal.Type == SignalType.Buy))
                {
                    if (signal.OrderType is TradeOrderType.Stop or TradeOrderType.Limit)
                    {
                        // Gli ordini condizionati di entry/reversal restano pendenti e non chiudono
                        // a mercato subito.
                        continue;
                    }

                    ClosePosition(positionKey, ResolveSignalPrice(signal, currentPrices), currentTime,
                        TradeExitReason.OppositeSignal);
                    CancelPendingOrders(positionKey);
                }
            }
        }

        // Apri nuove posizioni o accoda intent stop/next-bar
        foreach (var signal in signals)
        {
            if (signal.Type != SignalType.Buy && signal.Type != SignalType.Sell)
            {
                continue;
            }

            var signalSymbol = ResolveSignalSymbol(signal, currentPrices);
            if (string.IsNullOrEmpty(signalSymbol))
            {
                continue;
            }

            var positionKey = MakePositionKey(signalSymbol, GetSignalStrategyCode(signal));

            // Un intent scaduto non è eseguibile: la barra su cui era valido è passata e non
            // sappiamo a che prezzo il mercato l'avrebbe riempito. Il caso non è teorico —
            // sui periodi senza barre nel feed la strategia rivaluta l'ultima barra chiusa e
            // riemette un ordine con ValidFromUtc/ExpiresAtUtc già nel passato: eseguirlo
            // significa aprire al livello dello stop, un prezzo a cui nessuno ha scambiato.
            if (signal.ExpiresAtUtc.HasValue && currentTime > signal.ExpiresAtUtc.Value)
            {
                continue;
            }

            if (RequiresDeferredExecution(signal, currentTime))
            {
                EnqueuePendingOrder(positionKey, signal);
                continue;
            }

            if (signal.ExitOnly)
            {
                continue;
            }

            // Se non c'è già una posizione aperta per questa strategia, aprine una
            if (!_state.OpenPositions.ContainsKey(positionKey) &&
                CanFillEntry(positionKey, signal))
            {
                OpenFromSignal(positionKey, signal, signalSymbol, currentPrices, currentBars, currentTime);
            }
            else if (signal.OrderType == TradeOrderType.Stop)
            {
                // Posizione già aperta: mantieni lo stop come pending per un eventuale reverse fill.
                EnqueuePendingOrder(positionKey, signal);
            }
        }

        TryFillPendingOrders(currentPrices, currentBars, currentTime);

        // Verifica stop loss e take profit per posizioni aperte
        CheckStopLossAndTakeProfit(currentPrices, currentBars, 0m, currentTime);

        // Aggiorna equity con unrealized P&L
        UpdateEquity(currentPrices, 0m);

        return GetSnapshot();
    }

    private static bool IsSignalActive(TradeSignal signal, DateTime currentTime)
    {
        if (signal.ExpiresAtUtc.HasValue && currentTime > signal.ExpiresAtUtc.Value)
        {
            return false;
        }

        if (signal.ValidFromUtc.HasValue && currentTime < signal.ValidFromUtc.Value)
        {
            return false;
        }

        return true;
    }

    private static bool RequiresDeferredExecution(TradeSignal signal, DateTime currentTime)
    {
        if (signal.ValidFromUtc.HasValue && currentTime < signal.ValidFromUtc.Value)
        {
            return true;
        }

        return signal.OrderType == TradeOrderType.Stop;
    }

    private void EnqueuePendingOrder(string positionKey, TradeSignal signal)
    {
        // Chiave per direzione: long e short stop possono coesistere (OCO logico).
        var pendingKey = $"{positionKey}|{(int)signal.Type}|{(int)signal.OrderType}";
        _pendingOrders[pendingKey] = new PendingOrder
        {
            PositionKey = positionKey,
            Signal = signal
        };
    }

    private void CancelPendingOrders(string positionKey)
    {
        foreach (var key in _pendingOrders.Keys.Where(k => k.StartsWith(positionKey + "|", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            _pendingOrders.Remove(key);
        }
    }

    private void TryFillPendingOrders(
        Dictionary<string, decimal> currentPrices,
        Dictionary<string, OhlcvData> currentBars,
        DateTime currentTime)
    {
        foreach (var pendingKey in _pendingOrders.Keys.ToList())
        {
            if (!_pendingOrders.TryGetValue(pendingKey, out var pending))
            {
                continue;
            }

            var signal = pending.Signal;
            if (signal.ExpiresAtUtc.HasValue && currentTime > signal.ExpiresAtUtc.Value)
            {
                _pendingOrders.Remove(pendingKey);
                continue;
            }

            if (signal.ValidFromUtc.HasValue && currentTime < signal.ValidFromUtc.Value)
            {
                continue;
            }

            var signalSymbol = ResolveSignalSymbol(signal, currentPrices);
            if (string.IsNullOrEmpty(signalSymbol))
            {
                continue;
            }

            currentBars.TryGetValue(NormalizeSymbol(signalSymbol), out var bar);
            currentPrices.TryGetValue(NormalizeSymbol(signalSymbol), out var markPrice);

            if (signal.OrderType == TradeOrderType.Stop)
            {
                if (bar is null || !CanExecuteOnBar(signal, bar))
                {
                    continue;
                }

                var touched = signal.Type == SignalType.Buy
                    ? bar.High >= signal.Price
                    : bar.Low <= signal.Price;

                if (!touched)
                {
                    continue;
                }

                if (!CanFillEntry(pending.PositionKey, signal))
                {
                    _pendingOrders.Remove(pendingKey);
                    continue;
                }

                var fillPrice = signal.Type == SignalType.Buy
                    ? Math.Max(bar.Open, signal.Price)
                    : Math.Min(bar.Open, signal.Price);

                if (_state.OpenPositions.TryGetValue(pending.PositionKey, out var existing))
                {
                    if (existing.Direction == signal.Type)
                    {
                        _pendingOrders.Remove(pendingKey);
                        continue;
                    }

                    ClosePosition(pending.PositionKey, fillPrice, currentTime, TradeExitReason.OppositeSignal);
                }

                OpenFromSignal(pending.PositionKey, signal, signalSymbol, currentPrices, currentBars, currentTime, fillPrice);
                // Dopo un fill cancelliamo entrambi gli stop della stessa strategia (comportamento OCO).
                CancelPendingOrders(pending.PositionKey);
                continue;
            }

            if (signal.OrderType == TradeOrderType.Limit)
            {
                if (bar is null || !CanExecuteOnBar(signal, bar))
                {
                    continue;
                }

                // Un limit deve essere penetrato: il solo contatto non basta, come nel
                // simulatore Python degli engine RBB. In caso di gap il prezzo di fill è
                // migliorativo rispetto al limite.
                var penetrated = signal.Type == SignalType.Buy
                    ? bar.Low < signal.Price
                    : bar.High > signal.Price;
                if (!penetrated)
                {
                    continue;
                }

                if (!CanFillEntry(pending.PositionKey, signal))
                {
                    _pendingOrders.Remove(pendingKey);
                    continue;
                }

                var fillPrice = signal.Type == SignalType.Buy
                    ? Math.Min(bar.Open, signal.Price)
                    : Math.Max(bar.Open, signal.Price);

                if (_state.OpenPositions.TryGetValue(pending.PositionKey, out var existing))
                {
                    if (existing.Direction == signal.Type)
                    {
                        _pendingOrders.Remove(pendingKey);
                        continue;
                    }

                    ClosePosition(pending.PositionKey, fillPrice, currentTime, TradeExitReason.OppositeSignal);
                }

                OpenFromSignal(pending.PositionKey, signal, signalSymbol, currentPrices, currentBars, currentTime, fillPrice);
                // I due limit RBB emessi sulla stessa barra sono OCO.
                CancelPendingOrders(pending.PositionKey);
                continue;
            }

            // Market deferred (ValidFromUtc raggiunto)
            if (signal.OrderType == TradeOrderType.Market)
            {
                if (signal.ExitOnly)
                {
                    if (_state.OpenPositions.TryGetValue(pending.PositionKey, out var existing) &&
                        existing.Direction != signal.Type)
                    {
                        ClosePosition(pending.PositionKey, ResolveFillPrice(signal, bar, markPrice), currentTime,
                            TradeExitReason.OppositeSignal);
                        CancelPendingOrders(pending.PositionKey);
                    }
                    else
                    {
                        _pendingOrders.Remove(pendingKey);
                    }

                    continue;
                }

                if (_state.OpenPositions.ContainsKey(pending.PositionKey))
                {
                    _pendingOrders.Remove(pendingKey);
                    continue;
                }

                if (bar is null || !CanExecuteOnBar(signal, bar))
                {
                    continue;
                }

                if (!CanFillEntry(pending.PositionKey, signal))
                {
                    _pendingOrders.Remove(pendingKey);
                    continue;
                }

                var fillPrice = ResolveFillPrice(signal, bar, markPrice);
                OpenFromSignal(pending.PositionKey, signal, signalSymbol, currentPrices, currentBars, currentTime, fillPrice);
                _pendingOrders.Remove(pendingKey);
            }
        }
    }

    /// <summary>
    /// Una barra può eseguire un intent "next bar" solo se è chiusa dopo l'inizio della sua
    /// validità. Sui periodi in cui il feed non ha barre il cursore del backtest restituisce
    /// l'ultima barra disponibile, che è la stessa che ha generato il segnale: riempirla
    /// vorrebbe dire eseguire su un intervallo di cui non conosciamo i prezzi.
    /// </summary>
    private static bool CanExecuteOnBar(TradeSignal signal, OhlcvData bar) =>
        !signal.ValidFromUtc.HasValue ||
        TradingDateTime.ToFeedUtc(bar.DateTime) >= signal.ValidFromUtc.Value;

    private static decimal ResolveFillPrice(TradeSignal signal, OhlcvData? bar, decimal markPrice)
    {
        if (signal.OrderType == TradeOrderType.Market)
        {
            return bar?.Open ?? (signal.Price != 0 ? signal.Price : markPrice);
        }

        return signal.Price != 0 ? signal.Price : (bar?.Close ?? markPrice);
    }

    private void OpenFromSignal(
        string positionKey,
        TradeSignal signal,
        string signalSymbol,
        Dictionary<string, decimal> currentPrices,
        Dictionary<string, OhlcvData> currentBars,
        DateTime currentTime,
        decimal? explicitFillPrice = null)
    {
        var dollarsPerPoint = GetContractPointValue(signalSymbol);
        var stopLossPoints = signal.StopLoss
            ?? (signal.StopLossMoneyPerFutureContract.HasValue
                ? signal.StopLossMoneyPerFutureContract.Value / dollarsPerPoint
                : null);
        var takeProfitPoints = signal.TakeProfit
            ?? (signal.TakeProfitMoneyPerFutureContract.HasValue
                ? signal.TakeProfitMoneyPerFutureContract.Value / dollarsPerPoint
                : null);
        var trailingStopPoints = signal.TrailingStopMoneyPerFutureContract.HasValue
            ? (decimal?)(signal.TrailingStopMoneyPerFutureContract.Value / dollarsPerPoint)
            : null;
        var breakEvenPoints = signal.BreakEvenMoneyPerFutureContract.HasValue
            ? (decimal?)(signal.BreakEvenMoneyPerFutureContract.Value / dollarsPerPoint)
            : signal.BreakEven;

        currentBars.TryGetValue(NormalizeSymbol(signalSymbol), out var bar);
        currentPrices.TryGetValue(NormalizeSymbol(signalSymbol), out var markPrice);
        var entryPrice = explicitFillPrice ?? ResolveFillPrice(signal, bar, markPrice);

        OpenPosition(
            positionKey,
            signal.StrategyName,
            GetSignalStrategyCode(signal),
            signalSymbol,
            signal.Type,
            entryPrice,
            currentTime,
            signal.Quantity,
            stopLossPoints,
            takeProfitPoints,
            breakEvenPoints,
            trailingStopPoints,
            signal.MaxBarsInPosition,
            signal.CloseAtUtc,
            signal.Reason,
            signal.TimeExitOnlyIfProfitBelowMoneyPerContract,
            signal.ProfitStallAfterUtc);

        RecordEntry(positionKey, currentTime, signal);
    }

    private bool CanFillEntry(string positionKey, TradeSignal signal)
    {
        if (!signal.MaxEntriesPerSession.HasValue ||
            signal.MaxEntriesPerSession.Value <= 0 ||
            !signal.EntrySessionStartUtc.HasValue)
        {
            return true;
        }

        var sessionKey = MakeEntrySessionKey(positionKey, signal.EntrySessionStartUtc.Value);
        return !_entriesBySession.TryGetValue(sessionKey, out var entries) ||
               entries < signal.MaxEntriesPerSession.Value;
    }

    private void RecordEntry(string positionKey, DateTime entryTime, TradeSignal signal)
    {
        var day = entryTime.Date;
        if (_entriesByDay.TryGetValue(positionKey, out var tracked) && tracked.Day == day)
        {
            _entriesByDay[positionKey] = (day, tracked.Count + 1);
        }
        else
        {
            _entriesByDay[positionKey] = (day, 1);
        }

        if (signal.MaxEntriesPerSession.HasValue &&
            signal.MaxEntriesPerSession.Value > 0 &&
            signal.EntrySessionStartUtc.HasValue)
        {
            var sessionKey = MakeEntrySessionKey(positionKey, signal.EntrySessionStartUtc.Value);
            _entriesBySession.TryGetValue(sessionKey, out var entries);
            _entriesBySession[sessionKey] = entries + 1;
        }
    }

    private static string MakeEntrySessionKey(string positionKey, DateTime sessionStartUtc) =>
        $"{positionKey}|{TradingDateTime.ToFeedUtc(sessionStartUtc):O}";

    public TradingSnapshot UpdateMarketPrices(Dictionary<string, decimal> currentPrices, DateTime currentTime)
    {
        return UpdateMarketPrices(currentPrices, new Dictionary<string, OhlcvData>(StringComparer.OrdinalIgnoreCase), currentTime);
    }

    public TradingSnapshot UpdateMarketPrices(Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, DateTime currentTime)
    {
        currentTime = TradingDateTime.ToFeedUtc(currentTime);
        currentPrices = NormalizeCurrentPrices(currentPrices);
        currentBars = NormalizeCurrentBars(currentBars);
        CheckStopLossAndTakeProfit(currentPrices, currentBars, 0m, currentTime);
        CheckTimeExits(currentPrices, currentBars, 0m, currentTime);
        TryFillPendingOrders(currentPrices, currentBars, currentTime);
        // Un ingresso riempito in questa stessa barra deve poter uscire subito (SL/TP
        // same-bar). Senza questo secondo passaggio, le uscite sulla barra di fill
        // resterebbero in sospeso fino alla barra successiva.
        CheckStopLossAndTakeProfit(currentPrices, currentBars, 0m, currentTime);
        CheckTimeExits(currentPrices, currentBars, 0m, currentTime);
        UpdateEquity(currentPrices, 0m);

        return GetSnapshot();
    }

    public TradingSnapshot CloseAllOpenPositions(Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, DateTime currentTime)
        => CloseAllOpenPositions(currentPrices, currentBars, currentTime, TradeExitReason.EndOfRun);

    /// <summary>
    /// Chiude tutte le posizioni aperte al prezzo corrente attribuendo un motivo esplicito
    /// (fine settimana, fine run): senza di esso in analisi non si distingue una chiusura
    /// tecnica da un'uscita decisa dalla strategia.
    /// </summary>
    public TradingSnapshot CloseAllOpenPositions(
        Dictionary<string, decimal> currentPrices,
        Dictionary<string, OhlcvData> currentBars,
        DateTime currentTime,
        TradeExitReason reason)
    {
        currentTime = TradingDateTime.ToFeedUtc(currentTime);
        currentPrices = NormalizeCurrentPrices(currentPrices);
        currentBars = NormalizeCurrentBars(currentBars);
        RecordCurrentPrices(currentPrices);

        foreach (var positionKey in _state.OpenPositions.Keys.ToList())
        {
            if (!_state.OpenPositions.TryGetValue(positionKey, out var position))
            {
                continue;
            }

            var exitPrice = GetCurrentBar(position, currentBars)?.Close ?? GetCurrentPrice(position, currentPrices, 0m);
            if (exitPrice.HasValue)
            {
                ClosePosition(positionKey, exitPrice.Value, currentTime, reason);
            }
        }

        UpdateEquity(currentPrices, 0m);
        return GetSnapshot();
    }

    /// <summary>
    /// Cancella tutti gli ordini pendenti, restituendo quanti ne sono stati rimossi.
    /// Serve alla regola di flat settimanale: chiudere le posizioni non basta, perché uno stop
    /// emesso sull'ultima barra della settimana scade sulla barra successiva, che è la prima
    /// della settimana dopo, e resterebbe eseguibile sul gap di riapertura.
    /// </summary>
    public int CancelAllPendingOrders()
    {
        var cancelled = _pendingOrders.Count;
        _pendingOrders.Clear();
        return cancelled;
    }

    /// <summary>Diagnostica: quanti ordini pendenti sono in attesa di riempimento.</summary>
    public int PendingOrdersCount => _pendingOrders.Count;

    public TradingSnapshot GetSnapshot()
    {
        // Il profit totale è la differenza tra equity corrente e capitale iniziale
        // Non Equity - Balance (che è solo unrealized P&L)
        return new TradingSnapshot
        {
            DateTime = DateTime.UtcNow,
            Equity = _state.Equity,
            Balance = _state.Balance,
            Drawdown = _state.Drawdown,
            Profit = _state.Equity - _initialCapital,
            OpenPositionsCount = _state.OpenPositions.Count,
            StrategyEquities = GetStrategyEquities()
        };
    }

    public IReadOnlyList<TradingResult> GetClosedTrades() => _closedTrades.ToArray();

    /// <summary>Alza il picco favorevole di un long. Vedi <see cref="TrailingPeakIncludesCurrentBar"/>.</summary>
    private static void RaisePeak(OpenPosition position, decimal price)
    {
        if (!position.PeakFavorablePrice.HasValue || price > position.PeakFavorablePrice.Value)
            position.PeakFavorablePrice = price;
    }

    /// <summary>Abbassa il picco favorevole di uno short. Vedi <see cref="TrailingPeakIncludesCurrentBar"/>.</summary>
    private static void LowerPeak(OpenPosition position, decimal price)
    {
        if (!position.PeakFavorablePrice.HasValue || price < position.PeakFavorablePrice.Value)
            position.PeakFavorablePrice = price;
    }

    public BacktestingResult ApplyStrategyFilter(BacktestingResult result, List<string> enabledStrategies, Dictionary<string, decimal> multipliers)
    {
        // Filtra i risultati per strategia
        var filteredStrategyResults = result.StrategyResults
            .Where(sr => enabledStrategies.Contains(sr.StrategyName))
            .ToList();

        // Applica i moltiplicatori
        foreach (var strategyResult in filteredStrategyResults)
        {
            if (multipliers.TryGetValue(strategyResult.StrategyName, out var multiplier))
            {
                strategyResult.Profit *= multiplier;
                strategyResult.Contracts *= multiplier;
            }
        }

        // Ricalcola i risultati orari aggregati
        var filteredHourlyResults = filteredStrategyResults
            .GroupBy(sr => sr.DateTime)
            .Select(g => new HourlyResult
            {
                DateTime = g.Key,
                Profit = g.Sum(sr => sr.Profit),
                Equity = result.InitialCapital + g.Sum(sr => sr.Profit),
                Balance = result.InitialCapital + g.Sum(sr => sr.Profit),
                Drawdown = 0 // Ricalcolato dopo
            })
            .OrderBy(hr => hr.DateTime)
            .ToList();

        // Calcola drawdown
        decimal maxEquity = result.InitialCapital;
        foreach (var hr in filteredHourlyResults)
        {
            if (hr.Equity > maxEquity)
                maxEquity = hr.Equity;
            
            hr.Drawdown = maxEquity > 0 ? ((maxEquity - hr.Equity) / maxEquity) * 100m : 0;
        }

        // Crea nuovo risultato filtrato
        var filteredResult = new BacktestingResult
        {
            JobId = result.JobId,
            SetupName = result.SetupName,
            SetupId = result.SetupId,
            StartDate = result.StartDate,
            EndDate = result.EndDate,
            InitialCapital = result.InitialCapital,
            HourlyResults = filteredHourlyResults,
            StrategyResults = filteredStrategyResults,
            FinalEquity = filteredHourlyResults.LastOrDefault()?.Equity ?? result.InitialCapital,
            TotalProfit = filteredHourlyResults.Sum(hr => hr.Profit),
            MaxDrawdown = filteredHourlyResults.Max(hr => hr.Drawdown),
            StrategiesUsed = enabledStrategies
        };

        return filteredResult;
    }

    public void Reset()
    {
        _state = new TradingState
        {
            Equity = 0,
            Balance = 0,
            MaxEquity = 0,
            Drawdown = 0,
            OpenPositions = new Dictionary<string, OpenPosition>()
        };
        _closedTrades.Clear();
        _strategyCashAdjustments.Clear();
        _lastPrices.Clear();
        _strategyRuntimeStates.Clear();
        _pendingOrders.Clear();
        _entriesByDay.Clear();
        _entriesBySession.Clear();
    }

    private void OpenPosition(string positionKey, string strategyName, string strategyCode, string symbol, SignalType direction, decimal entryPrice, DateTime entryTime, decimal quantity, decimal? stopLoss, decimal? takeProfit, decimal? breakEven = null, decimal? trailingStop = null, int? maxBarsInPosition = null, DateTime? closeAtUtc = null, string? reason = null, decimal? timeExitOnlyIfProfitBelow = null, DateTime? profitStallAfterUtc = null)
    {
        if (quantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La quantità di ingresso deve essere positiva.");

        var position = new OpenPosition
        {
            StrategyName = strategyName,
            StrategyCode = strategyCode,
            Symbol = NormalizeSymbol(symbol),
            Direction = direction,
            EntryPrice = entryPrice,
            EntryTime = entryTime,
            Contracts = quantity,
            ContractPointValue = GetContractPointValue(symbol),
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            BreakEven = breakEven,
            TrailingStop = trailingStop,
            MaxBarsInPosition = maxBarsInPosition,
            CloseAtUtc = closeAtUtc,
            BarsInPosition = 0,
            LastProcessedBarTime = entryTime,
            BreakEvenActivated = false,
            TimeExitOnlyIfProfitBelowMoneyPerContract = timeExitOnlyIfProfitBelow,
            ProfitStallAfterUtc = profitStallAfterUtc
        };

        _state.OpenPositions[positionKey] = position;

        // Sottrai commissione dal balance
        var entryCommission = _commissionPerContract * position.Contracts;
        _state.Balance -= entryCommission;
        AddStrategyCashAdjustment(positionKey, -entryCommission);

        PositionOpened?.Invoke(new PositionOpenedEvent
        {
            StrategyCode = position.StrategyCode,
            StrategyName = position.StrategyName,
            Symbol = position.Symbol,
            Direction = position.Direction,
            EntryTimeUtc = position.EntryTime,
            EntryPrice = position.EntryPrice,
            Contracts = position.Contracts,
            StopLossPoints = position.StopLoss,
            TakeProfitPoints = position.TakeProfit,
            Reason = reason
        });
    }
    
    private void CheckStopLossAndTakeProfit(Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, decimal fallbackPrice, DateTime currentTime)
    {
        var positionsToClose = new List<(string PositionKey, decimal ExitPrice, TradeExitReason Reason)>();

        foreach (var (positionKey, position) in _state.OpenPositions)
        {
            var currentPrice = GetCurrentPrice(position, currentPrices, fallbackPrice);
            if (!currentPrice.HasValue)
            {
                continue;
            }

            decimal favorableMove = 0;
            var currentBar = GetCurrentBar(position, currentBars);
            var postFillLow = currentBar?.Low ?? currentPrice.Value;
            var postFillHigh = currentBar?.High ?? currentPrice.Value;

            // Convenzione OHLC deterministica per la barra che ha eseguito un ingresso:
            // candela rialzista O→L→H→C, candela ribassista O→H→L→C.
            // L'estremo opposto al verso della candela può quindi precedere il fill stop e
            // non deve essere usato per simulare uno stop successivo all'ingresso.
            if (position.EntryTime == currentTime && currentBar is not null)
            {
                var isBullishOrDoji = currentBar.Close >= currentBar.Open;
                if (position.Direction == SignalType.Buy &&
                    isBullishOrDoji &&
                    currentBar.Open < position.EntryPrice)
                {
                    // Buy stop raggiunto nella tratta L→H: il low è pre-fill, dopo il fill
                    // il solo minimo osservabile è il close della tratta H→C.
                    postFillLow = currentBar.Close;
                }
                else if (position.Direction == SignalType.Sell &&
                         !isBullishOrDoji &&
                         currentBar.Open > position.EntryPrice)
                {
                    // Sell stop raggiunto nella tratta H→L: l'high è pre-fill, dopo il fill
                    // il solo massimo osservabile è il close della tratta L→C.
                    postFillHigh = currentBar.Close;
                }
            }
            
            if (position.Direction == SignalType.Buy)
            {
                favorableMove = currentPrice.Value - position.EntryPrice;
                var favorableHighMove = (currentBar?.High ?? currentPrice.Value) - position.EntryPrice;
                var favorableHighPrice = currentBar?.High ?? currentPrice.Value;
                if (TrailingPeakIncludesCurrentBar)
                    RaisePeak(position, favorableHighPrice);
                
                // Gestione Break Even per Long
                if (!position.BreakEvenActivated && position.BreakEven.HasValue && 
                    favorableHighMove >= position.BreakEven.Value)
                {
                    // Sposta stop loss al prezzo di entry (break even)
                    position.StopLoss = 0;
                    position.BreakEvenActivated = true;
                }
                
                decimal? protectiveStopPrice = position.StopLoss.HasValue
                    ? position.EntryPrice - position.StopLoss.Value
                    : null;
                var protectiveExitReason = TradeExitReason.StopLoss;
                if (position.BreakEvenActivated)
                {
                    protectiveStopPrice = Math.Max(protectiveStopPrice ?? decimal.MinValue, position.EntryPrice);
                    protectiveExitReason = TradeExitReason.BreakEven;
                }
                if (position.TrailingStop.HasValue && position.PeakFavorablePrice.HasValue)
                {
                    var trailingStopPrice = position.PeakFavorablePrice.Value - position.TrailingStop.Value;
                    if (!protectiveStopPrice.HasValue || trailingStopPrice > protectiveStopPrice.Value)
                    {
                        protectiveStopPrice = trailingStopPrice;
                        protectiveExitReason = TradeExitReason.TrailingStop;
                    }
                }

                // Anche nella barra di fill, la policy intrabar conservativa fa precedere lo stop
                // protettivo al target. Con sole OHLC non è possibile ricostruire l'ordine reale
                // dei tick.
                if (protectiveStopPrice.HasValue && postFillLow <= protectiveStopPrice.Value)
                {
                    positionsToClose.Add((positionKey, protectiveStopPrice.Value, protectiveExitReason));
                    continue;
                }

                // Verifica Take Profit (profitto)
                if (position.TakeProfit.HasValue && favorableHighMove >= position.TakeProfit.Value)
                {
                    positionsToClose.Add((positionKey, position.EntryPrice + position.TakeProfit.Value, TradeExitReason.TakeProfit));
                    continue;
                }

                if (!TrailingPeakIncludesCurrentBar)
                    RaisePeak(position, favorableHighPrice);
            }
            else if (position.Direction == SignalType.Sell)
            {
                favorableMove = position.EntryPrice - currentPrice.Value;
                var favorableLowMove = position.EntryPrice - (currentBar?.Low ?? currentPrice.Value);
                var favorableLowPrice = currentBar?.Low ?? currentPrice.Value;
                if (TrailingPeakIncludesCurrentBar)
                    LowerPeak(position, favorableLowPrice);
                
                // Gestione Break Even per Short
                if (!position.BreakEvenActivated && position.BreakEven.HasValue && 
                    favorableLowMove >= position.BreakEven.Value)
                {
                    // Sposta stop loss al prezzo di entry (break even)
                    position.StopLoss = 0;
                    position.BreakEvenActivated = true;
                }
                
                decimal? protectiveStopPrice = position.StopLoss.HasValue
                    ? position.EntryPrice + position.StopLoss.Value
                    : null;
                var protectiveExitReason = TradeExitReason.StopLoss;
                if (position.BreakEvenActivated)
                {
                    protectiveStopPrice = Math.Min(protectiveStopPrice ?? decimal.MaxValue, position.EntryPrice);
                    protectiveExitReason = TradeExitReason.BreakEven;
                }
                if (position.TrailingStop.HasValue && position.PeakFavorablePrice.HasValue)
                {
                    var trailingStopPrice = position.PeakFavorablePrice.Value + position.TrailingStop.Value;
                    if (!protectiveStopPrice.HasValue || trailingStopPrice < protectiveStopPrice.Value)
                    {
                        protectiveStopPrice = trailingStopPrice;
                        protectiveExitReason = TradeExitReason.TrailingStop;
                    }
                }

                if (protectiveStopPrice.HasValue && postFillHigh >= protectiveStopPrice.Value)
                {
                    positionsToClose.Add((positionKey, protectiveStopPrice.Value, protectiveExitReason));
                    continue;
                }

                // Verifica Take Profit (profitto)
                if (position.TakeProfit.HasValue && favorableLowMove >= position.TakeProfit.Value)
                {
                    positionsToClose.Add((positionKey, position.EntryPrice - position.TakeProfit.Value, TradeExitReason.TakeProfit));
                    continue;
                }

                if (!TrailingPeakIncludesCurrentBar)
                {
                    LowerPeak(position, favorableLowPrice);
                }
            }
        }
        
        // Chiudi posizioni che hanno raggiunto stop loss o take profit
        foreach (var (positionKey, exitPrice, reason) in positionsToClose)
        {
            if (_state.OpenPositions.ContainsKey(positionKey))
            {
                ClosePosition(positionKey, exitPrice, currentTime, reason);
            }
        }
    }

    private void CheckTimeExits(Dictionary<string, decimal> currentPrices, Dictionary<string, OhlcvData> currentBars, decimal fallbackPrice, DateTime currentTime)
    {
        var positionsToClose = new List<(string PositionKey, decimal ExitPrice, TradeExitReason Reason)>();

        foreach (var (positionKey, position) in _state.OpenPositions)
        {
            var markPrice = GetCurrentBar(position, currentBars)?.Close
                ?? GetCurrentPrice(position, currentPrices, fallbackPrice);

            if (position.CloseAtUtc.HasValue && currentTime >= position.CloseAtUtc.Value)
            {
                // La chiusura a tempo può essere condizionata all'utile aperto: alcune strategie
                // escono all'ora prevista solo se sono sotto, altre lasciano correre il vincente
                // che ha già raggiunto una soglia. È la stessa regola con soglie diverse.
                var executeTimeExit = true;
                if (position.TimeExitOnlyIfProfitBelowMoneyPerContract is { } threshold &&
                    markPrice.HasValue)
                {
                    executeTimeExit = OpenProfitPerContract(position, markPrice.Value) < threshold;
                }

                if (executeTimeExit)
                {
                    if (markPrice.HasValue)
                    {
                        positionsToClose.Add((positionKey, markPrice.Value, TradeExitReason.TimeExit));
                    }
                    continue;
                }
            }

            // Uscita per stallo dell'utile: dopo la deadline si tiene il massimo osservato e si
            // chiude alla prima barra che non lo supera.
            if (position.ProfitStallAfterUtc is { } stallAfter &&
                currentTime >= stallAfter &&
                markPrice.HasValue)
            {
                var profit = OpenProfitPerContract(position, markPrice.Value);
                if (position.PeakProfitAfterStallDeadline is not { } peak)
                {
                    position.PeakProfitAfterStallDeadline = profit;
                }
                else if (profit > peak)
                {
                    position.PeakProfitAfterStallDeadline = profit;
                }
                else
                {
                    positionsToClose.Add((positionKey, markPrice.Value, TradeExitReason.TimeExit));
                    continue;
                }
            }

            if (!position.MaxBarsInPosition.HasValue || position.MaxBarsInPosition.Value <= 0)
            {
                continue;
            }

            if (position.EntryTime == currentTime || position.LastProcessedBarTime == currentTime)
            {
                continue;
            }

            position.BarsInPosition++;
            position.LastProcessedBarTime = currentTime;

            if (position.BarsInPosition < position.MaxBarsInPosition.Value)
            {
                continue;
            }

            var currentBar = GetCurrentBar(position, currentBars);
            var currentPrice = GetCurrentPrice(position, currentPrices, fallbackPrice);
            var exitPrice = currentBar?.Close ?? currentPrice;
            if (exitPrice.HasValue)
            {
                positionsToClose.Add((positionKey, exitPrice.Value, TradeExitReason.MaxBars));
            }
        }

        foreach (var (positionKey, exitPrice, reason) in positionsToClose)
        {
            if (_state.OpenPositions.ContainsKey(positionKey))
            {
                ClosePosition(positionKey, exitPrice, currentTime, reason);
            }
        }
    }

    /// <summary>
    /// Utile aperto per singolo contratto, in denaro. È la grandezza con cui le strategie
    /// EasyLanguage esprimono <c>openpositionprofit</c> quando è attivo <c>setstopcontract</c>,
    /// quindi confrontabile direttamente con le soglie dichiarate nel segnale.
    /// </summary>
    private static decimal OpenProfitPerContract(OpenPosition position, decimal markPrice)
    {
        var move = position.Direction == SignalType.Buy
            ? markPrice - position.EntryPrice
            : position.EntryPrice - markPrice;

        return move * position.ContractPointValue;
    }

    private void ClosePosition(string positionKey, decimal exitPrice, DateTime exitTime, TradeExitReason exitReason)
    {
        if (!_state.OpenPositions.TryGetValue(positionKey, out var position))
            return;

        var trade = new TradingResult
        {
            StrategyName = position.StrategyName,
            StrategyCode = position.StrategyCode,
            Symbol = position.Symbol,
            EntryDate = position.EntryTime,
            ExitDate = exitTime,
            EntryPrice = position.EntryPrice,
            ExitPrice = exitPrice,
            Quantity = position.Contracts,
            Direction = position.Direction,
            ContractPointValue = position.ContractPointValue,
            ExitReason = exitReason,
            BarsInPosition = position.BarsInPosition,
            Commission = _commissionPerContract * position.Contracts * 2 // Entry + Exit
        };

        // Calcola profit
        var grossProfit = trade.GrossProfit;
        var exitCommission = _commissionPerContract * position.Contracts;
        _state.Balance += grossProfit - exitCommission;
        AddStrategyCashAdjustment(positionKey, grossProfit - exitCommission);
        
        // Aggiorna equity: balance + unrealized P&L di eventuali altre posizioni aperte
        // Non impostare semplicemente Equity = Balance perché potrebbero esserci altre posizioni aperte
        // L'equity verrà aggiornata dalla chiamata a UpdateEquity dopo la chiusura
        
        // Aggiorna max equity e drawdown
        // Nota: l'equity verrà aggiornata correttamente da UpdateEquity chiamato dopo
        // ma dobbiamo aggiornare subito per il calcolo del drawdown
        _state.Equity = _state.Balance; // Temporaneo, verrà aggiornato da UpdateEquity
        
        if (_state.Equity > _state.MaxEquity)
            _state.MaxEquity = _state.Equity;
        
        _state.UpdateDrawdown();

        _closedTrades.Add(trade);
        _state.OpenPositions.Remove(positionKey);

        PositionClosed?.Invoke(new PositionClosedEvent
        {
            StrategyCode = position.StrategyCode,
            StrategyName = position.StrategyName,
            Symbol = position.Symbol,
            Direction = position.Direction,
            EntryTimeUtc = position.EntryTime,
            ExitTimeUtc = exitTime,
            EntryPrice = position.EntryPrice,
            ExitPrice = exitPrice,
            Contracts = position.Contracts,
            ExitReason = exitReason,
            GrossProfit = trade.GrossProfit,
            NetProfit = trade.NetProfit,
            Commission = trade.Commission,
            BarsInPosition = position.BarsInPosition,
            BalanceAfter = _state.Balance
        });
    }

    private void UpdateEquity(Dictionary<string, decimal> currentPrices, decimal fallbackPrice)
    {
        RecordCurrentPrices(currentPrices);
        decimal unrealizedPnL = 0;

        foreach (var position in _state.OpenPositions.Values)
        {
            var currentPrice = GetCurrentPrice(position, currentPrices, fallbackPrice);
            if (!currentPrice.HasValue)
            {
                continue;
            }

            if (position.Direction == SignalType.Buy)
            {
                unrealizedPnL += (currentPrice.Value - position.EntryPrice) * position.Contracts * position.ContractPointValue;
            }
            else if (position.Direction == SignalType.Sell)
            {
                unrealizedPnL += (position.EntryPrice - currentPrice.Value) * position.Contracts * position.ContractPointValue;
            }
        }

        _state.Equity = _state.Balance + unrealizedPnL;

        if (_state.Equity > _state.MaxEquity)
            _state.MaxEquity = _state.Equity;

        _state.UpdateDrawdown();
    }

    private Dictionary<string, decimal> GetStrategyEquities()
    {
        var strategyEquities = new Dictionary<string, decimal>(_strategyCashAdjustments, StringComparer.OrdinalIgnoreCase);

        foreach (var (positionKey, position) in _state.OpenPositions)
        {
            if (!strategyEquities.ContainsKey(positionKey))
            {
                strategyEquities[positionKey] = 0m;
            }

            strategyEquities[positionKey] += CalculateUnrealizedProfit(position);
        }

        return strategyEquities.ToDictionary(
            item => item.Key,
            item => _initialCapital + item.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private decimal CalculateUnrealizedProfit(OpenPosition position)
    {
        var currentPrice = GetCurrentPrice(position, _lastPrices, 0m);
        if (!currentPrice.HasValue)
        {
            return 0m;
        }

        return position.Direction == SignalType.Buy
            ? (currentPrice.Value - position.EntryPrice) * position.Contracts * position.ContractPointValue
            : (position.EntryPrice - currentPrice.Value) * position.Contracts * position.ContractPointValue;
    }

    private static Dictionary<string, decimal> BuildCurrentPrices(IEnumerable<TradeSignal> signals, decimal fallbackPrice)
    {
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var signal in signals)
        {
            var symbol = NormalizeSymbol(signal.Symbol);
            if (!string.IsNullOrEmpty(symbol) && signal.Price > 0)
            {
                prices[symbol] = signal.Price;
            }
        }

        if (!prices.Any() && fallbackPrice > 0)
        {
            prices[string.Empty] = fallbackPrice;
        }

        return prices;
    }

    private static decimal ResolveSignalPrice(TradeSignal signal, Dictionary<string, decimal> currentPrices)
    {
        if (signal.Price > 0)
        {
            return signal.Price;
        }

        var symbol = NormalizeSymbol(signal.Symbol);
        if (!string.IsNullOrEmpty(symbol) && currentPrices.TryGetValue(symbol, out var symbolPrice))
        {
            return symbolPrice;
        }

        return currentPrices.Count == 1 ? currentPrices.Values.First() : 0m;
    }

    private static decimal? GetCurrentPrice(OpenPosition position, Dictionary<string, decimal> currentPrices, decimal fallbackPrice)
    {
        if (!string.IsNullOrEmpty(position.Symbol) && currentPrices.TryGetValue(position.Symbol, out var symbolPrice))
        {
            return symbolPrice;
        }

        if (!string.IsNullOrEmpty(position.Symbol))
        {
            return null;
        }

        if (currentPrices.TryGetValue(string.Empty, out var defaultPrice))
        {
            return defaultPrice;
        }

        return fallbackPrice > 0 ? fallbackPrice : null;
    }

    private static OhlcvData? GetCurrentBar(OpenPosition position, Dictionary<string, OhlcvData> currentBars)
    {
        if (!string.IsNullOrEmpty(position.Symbol) && currentBars.TryGetValue(position.Symbol, out var symbolBar))
        {
            return symbolBar;
        }

        if (!string.IsNullOrEmpty(position.Symbol))
        {
            return null;
        }

        if (currentBars.TryGetValue(string.Empty, out var defaultBar))
        {
            return defaultBar;
        }

        return currentBars.Count == 1 ? currentBars.Values.First() : null;
    }

    private void AddStrategyCashAdjustment(string positionKey, decimal amount)
    {
        if (!_strategyCashAdjustments.ContainsKey(positionKey))
        {
            _strategyCashAdjustments[positionKey] = 0m;
        }

        _strategyCashAdjustments[positionKey] += amount;
    }

    private void RecordCurrentPrices(Dictionary<string, decimal> currentPrices)
    {
        foreach (var (symbol, price) in currentPrices)
        {
            if (!string.IsNullOrEmpty(symbol) && price > 0)
            {
                _lastPrices[NormalizeSymbol(symbol)] = price;
            }
        }
    }

    private string ResolveSignalSymbol(TradeSignal signal, Dictionary<string, decimal> currentPrices)
    {
        var symbol = NormalizeSymbol(signal.Symbol);
        if (!string.IsNullOrEmpty(symbol))
        {
            return symbol;
        }

        var strategyCode = GetSignalStrategyCode(signal);
        var matchingPosition = _state.OpenPositions.Values.FirstOrDefault(position =>
            position.StrategyCode.Equals(strategyCode, StringComparison.OrdinalIgnoreCase) ||
            position.StrategyName.Equals(signal.StrategyName, StringComparison.OrdinalIgnoreCase));
        if (matchingPosition != null)
        {
            return matchingPosition.Symbol;
        }

        var symbolKeys = currentPrices.Keys.Where(key => !string.IsNullOrEmpty(key)).ToList();
        return symbolKeys.Count == 1 ? symbolKeys[0] : string.Empty;
    }

    private static Dictionary<string, decimal> NormalizeCurrentPrices(Dictionary<string, decimal> currentPrices)
    {
        var normalized = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (symbol, price) in currentPrices)
        {
            if (price <= 0)
            {
                continue;
            }

            var normalizedSymbol = NormalizeSymbol(symbol);
            if (!string.IsNullOrEmpty(normalizedSymbol))
            {
                normalized[normalizedSymbol] = price;
            }
            else if (!normalized.Any())
            {
                normalized[string.Empty] = price;
            }
        }

        if (normalized.Keys.Any(key => !string.IsNullOrEmpty(key)))
        {
            normalized.Remove(string.Empty);
        }

        return normalized;
    }

    private static Dictionary<string, OhlcvData> NormalizeCurrentBars(Dictionary<string, OhlcvData> currentBars)
    {
        var normalized = new Dictionary<string, OhlcvData>(StringComparer.OrdinalIgnoreCase);
        foreach (var (symbol, bar) in currentBars)
        {
            if (bar == null)
            {
                continue;
            }

            var normalizedSymbol = NormalizeSymbol(symbol);
            if (!string.IsNullOrEmpty(normalizedSymbol))
            {
                normalized[normalizedSymbol] = bar;
            }
            else if (!normalized.Any())
            {
                normalized[string.Empty] = bar;
            }
        }

        if (normalized.Keys.Any(key => !string.IsNullOrEmpty(key)))
        {
            normalized.Remove(string.Empty);
        }

        return normalized;
    }

    private static string MakePositionKey(string symbol, string strategyCode)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var normalizedStrategyCode = strategyCode.Trim();

        if (string.IsNullOrEmpty(normalizedSymbol))
        {
            return normalizedStrategyCode;
        }

        if (string.IsNullOrEmpty(normalizedStrategyCode))
        {
            return normalizedSymbol;
        }

        return $"{normalizedSymbol}|{normalizedStrategyCode}";
    }

    private static string GetSignalStrategyCode(TradeSignal signal)
    {
        return !string.IsNullOrWhiteSpace(signal.StrategyCode)
            ? signal.StrategyCode
            : signal.StrategyName;
    }

    private static string NormalizeSymbol(string symbol)
    {
        return symbol.Trim().TrimStart('@').ToUpperInvariant();
    }

    /// <summary>
    /// Denaro per punto dello strumento. Delega alla sorgente unica
    /// <see cref="InstrumentRegistry"/>, che lancia sui simboli non verificati: la vecchia
    /// implementazione locale restituiva 1 in silenzio, falsando stop, target e P&amp;L senza
    /// alcun segnale.
    /// </summary>
    private static decimal GetContractPointValue(string symbol) =>
        InstrumentRegistry.PointValue(symbol);
}
