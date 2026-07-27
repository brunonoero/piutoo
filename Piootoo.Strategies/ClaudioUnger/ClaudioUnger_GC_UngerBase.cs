using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;

namespace Piootoo.Strategies.ClaudioUnger;

public abstract class ClaudioUngerBiasDailyBase : ITradingStrategy
{
    private readonly string _name;
    private readonly int _longYes;
    private readonly int _longNo;
    private readonly int _shortYes;
    private readonly int _shortNo;
    private DayOfWeek? _excludedLongDay;
    private DayOfWeek? _excludedShortDay;
    private decimal _stopLossDollars;
    private decimal? _takeProfitDollars;
    private int _maxBarsInPosition;

    private string _symbol;
    private decimal _contracts = 1m;
    private int _marketPosition;
    private int _barsInPosition;
    private DateTime? _entryDate;
    private DateTime? _lastProcessedBarDate;
    private DateTime? _lastTradedBarDate;

    protected ClaudioUngerBiasDailyBase(
        string name,
        string symbol,
        int longYes,
        int longNo,
        int shortYes,
        int shortNo,
        DayOfWeek? excludedLongDay,
        DayOfWeek? excludedShortDay,
        decimal stopLossDollars,
        decimal? takeProfitDollars,
        int maxBarsInPosition)
    {
        _name = name;
        _symbol = symbol;
        _longYes = longYes;
        _longNo = longNo;
        _shortYes = shortYes;
        _shortNo = shortNo;
        _excludedLongDay = excludedLongDay;
        _excludedShortDay = excludedShortDay;
        _stopLossDollars = stopLossDollars;
        _takeProfitDollars = takeProfitDollars;
        _maxBarsInPosition = maxBarsInPosition;
    }

    public string Name => _name;
    public string Description => $"Unger BIAS daily {_symbol}: ptn L {_longYes}/{_longNo}, S {_shortYes}/{_shortNo}";
    public string Symbol => _symbol;
    public int TimeframeMinutes => 1440;
    public int RequiredCandles => 8;

    /// <summary>Uscita decisa a runtime (pattern di uscita): strategia esclusa dal catalogo.</summary>
    public bool IsPositionCloseDependent => true;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters == null) return;

        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("Contracts", out var contracts)) _contracts = Convert.ToDecimal(contracts);
        if (parameters.TryGetValue("StopLossDollars", out var stopLoss)) _stopLossDollars = Convert.ToDecimal(stopLoss);
        if (parameters.TryGetValue("TakeProfitDollars", out var takeProfit)) _takeProfitDollars = ClaudioUngerPatterns.ToNullableDecimal(takeProfit);
        if (parameters.TryGetValue("MaxBarsInPosition", out var maxBars)) _maxBarsInPosition = Convert.ToInt32(maxBars);
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var currentPrice = data?.LastOrDefault()?.Close ?? 0m;
        if (data == null || data.Length < RequiredCandles)
        {
            return Hold(currentDate, currentPrice, "Dati insufficienti");
        }

        var current = data[^1];
        var barDate = current.DateTime.Date != default ? current.DateTime.Date : currentDate.Date;

        UpdateBarsInPosition(barDate);

        if (_marketPosition != 0 && _maxBarsInPosition > 0 && _barsInPosition >= _maxBarsInPosition)
        {
            ResetPositionState();
        }

        var shiftedPatternRow = data.Length - 2;
        var longAllowed = (!_excludedLongDay.HasValue || barDate.DayOfWeek != _excludedLongDay.Value)
            && ClaudioUngerPatterns.PatternFast(data, shiftedPatternRow, _longYes)
            && !ClaudioUngerPatterns.PatternFast(data, shiftedPatternRow, _longNo);
        var shortAllowed = (!_excludedShortDay.HasValue || barDate.DayOfWeek != _excludedShortDay.Value)
            && ClaudioUngerPatterns.PatternFast(data, shiftedPatternRow, _shortYes)
            && !ClaudioUngerPatterns.PatternFast(data, shiftedPatternRow, _shortNo);

        if (_marketPosition <= 0 && longAllowed && _lastTradedBarDate != barDate)
        {
            _marketPosition = 1;
            _barsInPosition = 0;
            _entryDate = barDate;
            _lastTradedBarDate = barDate;

            return Entry(currentDate, SignalType.Buy, current.Open, "BIAS daily long at open");
        }

        if (_marketPosition >= 0 && shortAllowed && _lastTradedBarDate != barDate)
        {
            _marketPosition = -1;
            _barsInPosition = 0;
            _entryDate = barDate;
            _lastTradedBarDate = barDate;

            return Entry(currentDate, SignalType.Sell, current.Open, "BIAS daily short at open");
        }

        return Hold(currentDate, current.Close, "Nessun setup BIAS");
    }

    private void UpdateBarsInPosition(DateTime barDate)
    {
        if (_lastProcessedBarDate == barDate)
        {
            return;
        }

        if (_marketPosition != 0 && _entryDate.HasValue && barDate > _entryDate.Value)
        {
            _barsInPosition++;
        }

        _lastProcessedBarDate = barDate;
    }

    private TradeSignal Exit(DateTime date, decimal price, string reason)
    {
        var exitPosition = _marketPosition;
        ResetPositionState();

        return Signal(date, exitPosition > 0 ? SignalType.Sell : SignalType.Buy, price, reason, closeOnly: true);
    }

    private void ResetPositionState()
    {
        _marketPosition = 0;
        _barsInPosition = 0;
        _entryDate = null;
    }

    private TradeSignal Entry(DateTime date, SignalType type, decimal price, string reason)
    {
        var signal = Signal(date, type, price, reason, closeOnly: false);
        signal.StopLoss = ClaudioUngerPatterns.DollarsToPoints(_symbol, _stopLossDollars, _contracts);
        signal.TakeProfit = _takeProfitDollars.HasValue
            ? ClaudioUngerPatterns.DollarsToPoints(_symbol, _takeProfitDollars.Value, _contracts)
            : null;
        signal.MaxBarsInPosition = _maxBarsInPosition;
        return signal;
    }

    private TradeSignal Hold(DateTime date, decimal price, string reason) => Signal(date, SignalType.Hold, price, reason, closeOnly: false);

    private TradeSignal Signal(DateTime date, SignalType type, decimal price, string reason, bool closeOnly)
    {
        return new TradeSignal
        {
            Date = date,
            Type = type,
            Price = price,
            Symbol = _symbol,
            StrategyCode = Name,
            StrategyName = Name,
            Quantity = _contracts,

            Reason = reason
        };
    }
}

public abstract class ClaudioUngerTfUnmirroredBase : ITradingStrategy
{
    private readonly string _name;
    private readonly int _timeframeMinutes;
    private readonly int _longYes;
    private readonly int _longNo;
    private readonly int _shortYes;
    private readonly int _shortNo;
    private readonly int? _startHour;
    private readonly int? _endHour;
    private readonly DayOfWeek? _excludedDay;
    private readonly decimal _stopLossDollars;
    private readonly decimal? _takeProfitDollars;
    private readonly int _maxBarsInPosition;

    private string _symbol;
    private decimal _contracts = 1m;
    private int _marketPosition;
    private int _barsInPosition;
    private DateTime? _entryBarTime;
    private DateTime? _lastProcessedBarTime;
    private DateTime? _lastLongSession;
    private DateTime? _lastShortSession;

    protected ClaudioUngerTfUnmirroredBase(string name, string symbol, int timeframeMinutes, int longYes, int longNo, int shortYes, int shortNo, int? startHour, int? endHour, DayOfWeek? excludedDay, decimal stopLossDollars, decimal? takeProfitDollars, int maxBarsInPosition)
    {
        _name = name;
        _symbol = symbol;
        _timeframeMinutes = timeframeMinutes;
        _longYes = longYes;
        _longNo = longNo;
        _shortYes = shortYes;
        _shortNo = shortNo;
        _startHour = startHour;
        _endHour = endHour;
        _excludedDay = excludedDay;
        _stopLossDollars = stopLossDollars;
        _takeProfitDollars = takeProfitDollars;
        _maxBarsInPosition = maxBarsInPosition;
    }

    public string Name => _name;
    public string Description => $"Unger TF_U {_symbol} {_timeframeMinutes}m: stop H_d1/L_d1";
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 8;

    /// <summary>Uscita decisa a runtime (pattern di uscita): strategia esclusa dal catalogo.</summary>
    public bool IsPositionCloseDependent => true;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters == null) return;
        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("Contracts", out var contracts)) _contracts = Convert.ToDecimal(contracts);
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var currentPrice = data?.LastOrDefault()?.Close ?? 0m;
        if (data == null || data.Length < RequiredCandles)
        {
            return Hold(currentDate, currentPrice, "Dati insufficienti");
        }

        var current = data[^1];
        var barTime = current.DateTime != default ? current.DateTime : currentDate;
        var session = barTime.Date;
        UpdateBarsInPosition(barTime);

        if (_marketPosition != 0 && _maxBarsInPosition > 0 && _barsInPosition >= _maxBarsInPosition)
        {
            return Exit(currentDate, current.Close, $"Max bars {_maxBarsInPosition}");
        }

        if (!ClaudioUngerPatterns.TimeWindow(barTime, _startHour, _endHour) ||
            (_excludedDay.HasValue && barTime.DayOfWeek == _excludedDay.Value))
        {
            return Hold(currentDate, current.Close, "Fuori finestra");
        }

        var row = data.Length - 1;
        var previous = data[^2];
        var longSetup = _lastLongSession != session
            && ClaudioUngerPatterns.PatternFast(data, row, _longYes)
            && !ClaudioUngerPatterns.PatternFast(data, row, _longNo)
            && current.High >= previous.High;
        var shortSetup = _lastShortSession != session
            && ClaudioUngerPatterns.PatternFast(data, row, _shortYes)
            && !ClaudioUngerPatterns.PatternFast(data, row, _shortNo)
            && current.Low <= previous.Low;

        if (_marketPosition <= 0 && longSetup)
        {
            _marketPosition = 1;
            _barsInPosition = 0;
            _entryBarTime = barTime;
            _lastLongSession = session;
            return Entry(currentDate, SignalType.Buy, Math.Max(current.Open, previous.High), "TF_U long stop H_d1");
        }

        if (_marketPosition >= 0 && shortSetup)
        {
            _marketPosition = -1;
            _barsInPosition = 0;
            _entryBarTime = barTime;
            _lastShortSession = session;
            return Entry(currentDate, SignalType.Sell, Math.Min(current.Open, previous.Low), "TF_U short stop L_d1");
        }

        return Hold(currentDate, current.Close, "Nessun setup TF_U");
    }

    private void UpdateBarsInPosition(DateTime barTime)
    {
        if (_lastProcessedBarTime == barTime) return;
        if (_marketPosition != 0 && _entryBarTime.HasValue && barTime > _entryBarTime.Value) _barsInPosition++;
        _lastProcessedBarTime = barTime;
    }

    private TradeSignal Entry(DateTime date, SignalType type, decimal price, string reason)
    {
        var signal = Signal(date, type, price, reason, closeOnly: false);
        signal.StopLoss = ClaudioUngerPatterns.DollarsToPoints(_symbol, _stopLossDollars, _contracts);
        signal.TakeProfit = _takeProfitDollars.HasValue ? ClaudioUngerPatterns.DollarsToPoints(_symbol, _takeProfitDollars.Value, _contracts) : null;
        return signal;
    }

    private TradeSignal Exit(DateTime date, decimal price, string reason)
    {
        var exitPosition = _marketPosition;
        _marketPosition = 0;
        _barsInPosition = 0;
        _entryBarTime = null;
        return Signal(date, exitPosition > 0 ? SignalType.Sell : SignalType.Buy, price, reason, closeOnly: true);
    }

    private TradeSignal Hold(DateTime date, decimal price, string reason) => Signal(date, SignalType.Hold, price, reason, closeOnly: false);

    private TradeSignal Signal(DateTime date, SignalType type, decimal price, string reason, bool closeOnly)
    {
        return new TradeSignal { Date = date, Type = type, Price = price, Symbol = _symbol, StrategyCode = Name, StrategyName = Name, Quantity = _contracts, Reason = reason };
    }
}

public abstract class ClaudioUngerRbbMirroredBase : ITradingStrategy
{
    private readonly string _name;
    private readonly int _timeframeMinutes;
    private readonly int _bbLength;
    private readonly decimal _bbNumDevs;
    private readonly int _neutralYes;
    private readonly int _neutralNo;
    private readonly int _directionalYes;
    private readonly int _directionalNo;
    private readonly int? _startHour;
    private readonly int? _endHour;
    private readonly DayOfWeek? _excludedDay;
    private readonly decimal _stopLossDollars;
    private readonly decimal? _takeProfitDollars;
    private readonly int _maxBarsInPosition;

    private string _symbol;
    private decimal _contracts = 1m;
    private int _marketPosition;
    private int _barsInPosition;
    private DateTime? _entryBarTime;
    private DateTime? _lastProcessedBarTime;
    private DateTime? _lastLongSession;
    private DateTime? _lastShortSession;

    protected ClaudioUngerRbbMirroredBase(string name, string symbol, int timeframeMinutes, int bbLength, decimal bbNumDevs, int neutralYes, int neutralNo, int directionalYes, int directionalNo, int? startHour, int? endHour, DayOfWeek? excludedDay, decimal stopLossDollars, decimal? takeProfitDollars, int maxBarsInPosition)
    {
        _name = name;
        _symbol = symbol;
        _timeframeMinutes = timeframeMinutes;
        _bbLength = bbLength;
        _bbNumDevs = bbNumDevs;
        _neutralYes = neutralYes;
        _neutralNo = neutralNo;
        _directionalYes = directionalYes;
        _directionalNo = directionalNo;
        _startHour = startHour;
        _endHour = endHour;
        _excludedDay = excludedDay;
        _stopLossDollars = stopLossDollars;
        _takeProfitDollars = takeProfitDollars;
        _maxBarsInPosition = maxBarsInPosition;
    }

    public string Name => _name;
    public string Description => $"Unger RBB_M {_symbol} {_timeframeMinutes}m: BB {_bbLength}/{_bbNumDevs}";
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => Math.Max(60, _bbLength + 8);

    /// <summary>Uscita decisa a runtime (pattern di uscita): strategia esclusa dal catalogo.</summary>
    public bool IsPositionCloseDependent => true;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters == null) return;
        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("Contracts", out var contracts)) _contracts = Convert.ToDecimal(contracts);
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        var currentPrice = data?.LastOrDefault()?.Close ?? 0m;
        if (data == null || data.Length < RequiredCandles)
        {
            return Hold(currentDate, currentPrice, "Dati insufficienti");
        }

        var current = data[^1];
        var previous = data[^2];
        var barTime = current.DateTime != default ? current.DateTime : currentDate;
        var session = barTime.Date;
        UpdateBarsInPosition(barTime);

        if (_marketPosition != 0 && _maxBarsInPosition > 0 && _barsInPosition >= _maxBarsInPosition)
        {
            return Exit(currentDate, current.Close, $"Max bars {_maxBarsInPosition}");
        }

        if (!ClaudioUngerPatterns.TimeWindow(barTime, _startHour, _endHour) ||
            (_excludedDay.HasValue && barTime.DayOfWeek == _excludedDay.Value))
        {
            return Hold(currentDate, current.Close, "Fuori finestra");
        }

        var (middle, stdDev) = Bollinger(data, data.Length - 1);
        var (previousMiddle, previousStdDev) = Bollinger(data, data.Length - 2);
        var bbUp = middle + _bbNumDevs * stdDev;
        var bbDown = middle - _bbNumDevs * stdDev;
        var previousBbUp = previousMiddle + _bbNumDevs * previousStdDev;
        var previousBbDown = previousMiddle - _bbNumDevs * previousStdDev;

        var row = data.Length - 1;
        var neutral = ClaudioUngerPatterns.PatternNeutral(data, row, _neutralYes) &&
            !ClaudioUngerPatterns.PatternNeutral(data, row, _neutralNo);
        var longDirectional = ClaudioUngerPatterns.PatternDirectional(data, row, -_directionalYes) &&
            !ClaudioUngerPatterns.PatternDirectional(data, row, -_directionalNo);
        var shortDirectional = ClaudioUngerPatterns.PatternDirectional(data, row, _directionalYes) &&
            !ClaudioUngerPatterns.PatternDirectional(data, row, _directionalNo);

        var crossDown = current.Low <= bbDown && previous.Close > previousBbDown;
        var crossUp = current.High >= bbUp && previous.Close < previousBbUp;

        if (_marketPosition <= 0 && _lastLongSession != session && neutral && longDirectional && crossDown)
        {
            _marketPosition = 1;
            _barsInPosition = 0;
            _entryBarTime = barTime;
            _lastLongSession = session;
            return Entry(currentDate, SignalType.Buy, Math.Min(current.Open, bbDown), "RBB_M long limit BB lower");
        }

        if (_marketPosition >= 0 && _lastShortSession != session && neutral && shortDirectional && crossUp)
        {
            _marketPosition = -1;
            _barsInPosition = 0;
            _entryBarTime = barTime;
            _lastShortSession = session;
            return Entry(currentDate, SignalType.Sell, Math.Max(current.Open, bbUp), "RBB_M short limit BB upper");
        }

        return Hold(currentDate, current.Close, "Nessun setup RBB_M");
    }

    private (decimal Middle, decimal StdDev) Bollinger(OhlcvData[] data, int rowIndex)
    {
        var values = data.Skip(rowIndex - _bbLength + 1).Take(_bbLength).Select(x => x.Close).ToArray();
        var mean = values.Average();
        var variance = values.Select(value => (value - mean) * (value - mean)).Average();
        return (mean, (decimal)Math.Sqrt((double)variance));
    }

    private void UpdateBarsInPosition(DateTime barTime)
    {
        if (_lastProcessedBarTime == barTime) return;
        if (_marketPosition != 0 && _entryBarTime.HasValue && barTime > _entryBarTime.Value) _barsInPosition++;
        _lastProcessedBarTime = barTime;
    }

    private TradeSignal Entry(DateTime date, SignalType type, decimal price, string reason)
    {
        var signal = Signal(date, type, price, reason, closeOnly: false);
        signal.StopLoss = ClaudioUngerPatterns.DollarsToPoints(_symbol, _stopLossDollars, _contracts);
        signal.TakeProfit = _takeProfitDollars.HasValue ? ClaudioUngerPatterns.DollarsToPoints(_symbol, _takeProfitDollars.Value, _contracts) : null;
        return signal;
    }

    private TradeSignal Exit(DateTime date, decimal price, string reason)
    {
        var exitPosition = _marketPosition;
        _marketPosition = 0;
        _barsInPosition = 0;
        _entryBarTime = null;
        return Signal(date, exitPosition > 0 ? SignalType.Sell : SignalType.Buy, price, reason, closeOnly: true);
    }

    private TradeSignal Hold(DateTime date, decimal price, string reason) => Signal(date, SignalType.Hold, price, reason, closeOnly: false);

    private TradeSignal Signal(DateTime date, SignalType type, decimal price, string reason, bool closeOnly)
    {
        return new TradeSignal { Date = date, Type = type, Price = price, Symbol = _symbol, StrategyCode = Name, StrategyName = Name, Quantity = _contracts, Reason = reason };
    }
}

public abstract class ClaudioUngerTfMirroredBase : IMultiTimeframeTradingStrategy
{
    private readonly string _name;
    private readonly int _timeframeMinutes;
    private readonly int _neutralYes;
    private readonly int _neutralNo;
    private readonly int _directionalYes;
    private readonly int _directionalNo;
    private readonly int? _startHour;
    private readonly int? _endHour;
    private readonly DayOfWeek? _excludedDay;
    private readonly decimal _stopLossDollars;
    private readonly decimal? _takeProfitDollars;
    private readonly int _maxBarsInPosition;

    private string _symbol;
    private decimal _contracts = 1m;
    private int _marketPosition;
    private int _barsInPosition;
    private DateTime? _entryBarTime;
    private DateTime? _lastProcessedBarTime;
    private DateTime? _lastSignalSession;
    private DateTime? _pendingOrderSession;
    private SignalType? _pendingOrderType;
    private decimal _pendingOrderPrice;
    private int _sessionStartHour = 23;
    private int _sessionStartMinute;

    protected ClaudioUngerTfMirroredBase(string name, string symbol, int timeframeMinutes, int neutralYes, int neutralNo, int directionalYes, int directionalNo, int? startHour, int? endHour, DayOfWeek? excludedDay, decimal stopLossDollars, decimal? takeProfitDollars, int maxBarsInPosition)
    {
        _name = name;
        _symbol = symbol;
        _timeframeMinutes = timeframeMinutes;
        _neutralYes = neutralYes;
        _neutralNo = neutralNo;
        _directionalYes = directionalYes;
        _directionalNo = directionalNo;
        _startHour = startHour;
        _endHour = endHour;
        _excludedDay = excludedDay;
        _stopLossDollars = stopLossDollars;
        _takeProfitDollars = takeProfitDollars;
        _maxBarsInPosition = maxBarsInPosition;
    }

    public string Name => _name;
    public string Description => $"Unger TF_M {_symbol} {_timeframeMinutes}m: ptn N {_neutralYes}/{_neutralNo}, D +/-{_directionalYes}/{_directionalNo}";
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => _timeframeMinutes >= 1440 ? 8 : Math.Max(8, 6 * 24 * 60 / _timeframeMinutes);

    /// <summary>Uscita decisa a runtime (pattern di uscita): strategia esclusa dal catalogo.</summary>
    public bool IsPositionCloseDependent => true;
    public IReadOnlyCollection<int> AdditionalTimeframes => _timeframeMinutes >= 1440 ? Array.Empty<int>() : new[] { 1440 };

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters == null) return;
        if (parameters.TryGetValue("Symbol", out var symbol)) _symbol = symbol?.ToString() ?? _symbol;
        if (parameters.TryGetValue("Contracts", out var contracts)) _contracts = Convert.ToDecimal(contracts);
        if (parameters.TryGetValue("SessionStartHour", out var sessionStartHour)) _sessionStartHour = Convert.ToInt32(sessionStartHour);
        if (parameters.TryGetValue("SessionStartMinute", out var sessionStartMinute)) _sessionStartMinute = Convert.ToInt32(sessionStartMinute);
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        GenerateSignal(data, new Dictionary<int, OhlcvData[]>(), currentDate);

    public TradeSignal GenerateSignal(OhlcvData[] data, IReadOnlyDictionary<int, OhlcvData[]> additionalData, DateTime currentDate)
    {
        var currentPrice = data?.LastOrDefault()?.Close ?? 0m;
        if (data == null || data.Length < RequiredCandles)
        {
            return Hold(currentDate, currentPrice, "Dati insufficienti");
        }

        var current = data[^1];
        if (!IsValidBar(current))
        {
            return Hold(currentDate, currentPrice, "Barra non valida");
        }

        var barTime = current.DateTime != default ? current.DateTime : currentDate;
        var session = GetSessionStart(barTime);
        UpdateBarsInPosition(barTime);

        if (_marketPosition != 0 && _maxBarsInPosition > 0 && _barsInPosition >= _maxBarsInPosition)
        {
            return Exit(currentDate, current.Close, $"Max bars {_maxBarsInPosition}");
        }

        if (!ClaudioUngerPatterns.TimeWindow(barTime, _startHour, _endHour) ||
            (_excludedDay.HasValue && barTime.DayOfWeek == _excludedDay.Value))
        {
            return Hold(currentDate, current.Close, "Fuori finestra");
        }

        var sessionData = BuildSessionOhlc(data, barTime);
        if (sessionData.Length < 6)
        {
            return Hold(currentDate, current.Close, "Dati sessione insufficienti");
        }

        var row = sessionData.Length - 1;
        var previousSession = sessionData[^2];
        if (!IsValidBar(previousSession))
        {
            ClearPendingOrder();
            return Hold(currentDate, current.Close, "Sessione precedente non valida");
        }

        if (_pendingOrderSession == session && _pendingOrderType.HasValue)
        {
            if (_pendingOrderType.Value == SignalType.Buy && current.High >= _pendingOrderPrice && _marketPosition <= 0)
            {
                _marketPosition = 1;
                _barsInPosition = 0;
                _entryBarTime = barTime;
                ClearPendingOrder();
                return Entry(currentDate, SignalType.Buy, Math.Max(current.Open, _pendingOrderPrice), "TF_M long stop H_d1");
            }

            if (_pendingOrderType.Value == SignalType.Sell && current.Low <= _pendingOrderPrice && _marketPosition >= 0)
            {
                _marketPosition = -1;
                _barsInPosition = 0;
                _entryBarTime = barTime;
                ClearPendingOrder();
                return Entry(currentDate, SignalType.Sell, Math.Min(current.Open, _pendingOrderPrice), "TF_M short stop L_d1");
            }

            return Hold(currentDate, current.Close, "TF_M stop pending");
        }

        ClearExpiredPendingOrder(session);

        var neutral = ClaudioUngerPatterns.PatternNeutral(sessionData, row, _neutralYes) &&
            !ClaudioUngerPatterns.PatternNeutral(sessionData, row, _neutralNo);

        if (!neutral || _lastSignalSession == session)
        {
            return Hold(currentDate, current.Close, "Nessun setup TF_M");
        }

        var longSetup = ClaudioUngerPatterns.PatternDirectional(sessionData, row, _directionalYes) &&
            !ClaudioUngerPatterns.PatternDirectional(sessionData, row, _directionalNo);
        var shortSetup = ClaudioUngerPatterns.PatternDirectional(sessionData, row, -_directionalYes) &&
            !ClaudioUngerPatterns.PatternDirectional(sessionData, row, -_directionalNo);

        if (longSetup)
        {
            _lastSignalSession = session;
            _pendingOrderSession = session;
            _pendingOrderType = SignalType.Buy;
            _pendingOrderPrice = previousSession.High;

            if (current.High >= _pendingOrderPrice && _marketPosition <= 0)
            {
                _marketPosition = 1;
                _barsInPosition = 0;
                _entryBarTime = barTime;
                ClearPendingOrder();
                return Entry(currentDate, SignalType.Buy, Math.Max(current.Open, previousSession.High), "TF_M long stop H_d1");
            }

            return Hold(currentDate, current.Close, "TF_M long stop pending");
        }

        if (shortSetup)
        {
            _lastSignalSession = session;
            _pendingOrderSession = session;
            _pendingOrderType = SignalType.Sell;
            _pendingOrderPrice = previousSession.Low;

            if (current.Low <= _pendingOrderPrice && _marketPosition >= 0)
            {
                _marketPosition = -1;
                _barsInPosition = 0;
                _entryBarTime = barTime;
                ClearPendingOrder();
                return Entry(currentDate, SignalType.Sell, Math.Min(current.Open, previousSession.Low), "TF_M short stop L_d1");
            }

            return Hold(currentDate, current.Close, "TF_M short stop pending");
        }

        return Hold(currentDate, current.Close, "Nessun setup TF_M");
    }

    private void ClearExpiredPendingOrder(DateTime session)
    {
        if (_pendingOrderSession.HasValue && _pendingOrderSession.Value != session)
        {
            ClearPendingOrder();
        }
    }

    private void ClearPendingOrder()
    {
        _pendingOrderSession = null;
        _pendingOrderType = null;
        _pendingOrderPrice = 0m;
    }

    private OhlcvData[] BuildSessionOhlc(OhlcvData[] intradayData, DateTime barTime)
    {
        var currentSessionStart = GetSessionStart(barTime);

        return intradayData
            .Where(bar => bar.DateTime <= barTime && IsValidBar(bar))
            .GroupBy(bar => GetSessionStart(bar.DateTime))
            .Where(group => group.Key <= currentSessionStart)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var bars = group.OrderBy(bar => bar.DateTime).ToArray();
                return new OhlcvData
                {
                    DateTime = group.Key,
                    Open = bars.First().Open,
                    High = bars.Max(bar => bar.High),
                    Low = bars.Min(bar => bar.Low),
                    Close = bars.Last().Close,
                    Volume = bars.Sum(bar => bar.Volume)
                };
            })
            .ToArray();
    }

    private static bool IsValidBar(OhlcvData bar)
    {
        return bar.Open > 0m && bar.High > 0m && bar.Low > 0m && bar.Close > 0m && bar.High >= bar.Low;
    }

    private DateTime GetSessionStart(DateTime dateTime)
    {
        var sessionStart = dateTime.Date.AddHours(_sessionStartHour).AddMinutes(_sessionStartMinute);
        return dateTime < sessionStart ? sessionStart.AddDays(-1) : sessionStart;
    }

    private void UpdateBarsInPosition(DateTime barTime)
    {
        if (_lastProcessedBarTime == barTime) return;
        if (_marketPosition != 0 && _entryBarTime.HasValue && barTime > _entryBarTime.Value) _barsInPosition++;
        _lastProcessedBarTime = barTime;
    }

    private TradeSignal Entry(DateTime date, SignalType type, decimal price, string reason)
    {
        var signal = Signal(date, type, price, reason, closeOnly: false);
        signal.StopLoss = _stopLossDollars > 0m ? ClaudioUngerPatterns.DollarsToPoints(_symbol, _stopLossDollars, _contracts) : null;
        signal.TakeProfit = _takeProfitDollars.HasValue && _takeProfitDollars.Value > 0m
            ? ClaudioUngerPatterns.DollarsToPoints(_symbol, _takeProfitDollars.Value, _contracts)
            : null;
        signal.MaxBarsInPosition = _maxBarsInPosition;
        return signal;
    }

    private TradeSignal Exit(DateTime date, decimal price, string reason)
    {
        var exitPosition = _marketPosition;
        _marketPosition = 0;
        _barsInPosition = 0;
        _entryBarTime = null;
        return Signal(date, exitPosition > 0 ? SignalType.Sell : SignalType.Buy, price, reason, closeOnly: true);
    }

    private TradeSignal Hold(DateTime date, decimal price, string reason) => Signal(date, SignalType.Hold, price, reason, closeOnly: false);

    private TradeSignal Signal(DateTime date, SignalType type, decimal price, string reason, bool closeOnly)
    {
        return new TradeSignal { Date = date, Type = type, Price = price, Symbol = _symbol, StrategyCode = Name, StrategyName = Name, Quantity = _contracts, Reason = reason };
    }
}

internal static class ClaudioUngerPatterns
{
    public static decimal DollarsToPoints(string symbol, decimal dollars, decimal contracts)
    {
        var pointValue = NormalizeSymbol(symbol) switch
        {
            "GC" => 100m,
            "MGC" => 10m,
            "NQ" => 20m,
            _ => 1m
        };

        return dollars / (pointValue * Math.Max(contracts, 1m));
    }

    public static bool TimeWindow(DateTime dateTime, int? startHour, int? endHour)
    {
        if (!startHour.HasValue && !endHour.HasValue) return true;

        var hour = dateTime.Hour;
        var start = startHour ?? 0;
        var end = endHour ?? 23;

        return start <= end ? hour >= start && hour <= end : hour >= start || hour <= end;
    }

    public static bool PatternFast(OhlcvData[] data, int rowIndex, int pattern)
    {
        if (pattern == 152) return true;
        if (pattern == 153 || pattern <= 0 || rowIndex < 0) return false;
        if (pattern is >= 1 and <= 30) return PatternNeutral(data, rowIndex, pattern);

        var o0 = Get(data, rowIndex, 0)?.Open ?? 0m;
        var h0 = Get(data, rowIndex, 0)?.High ?? 0m;
        var l0 = Get(data, rowIndex, 0)?.Low ?? 0m;
        var c0 = Get(data, rowIndex, 0)?.Close ?? 0m;
        var o1 = Get(data, rowIndex, 1)?.Open ?? 0m;
        var h1 = Get(data, rowIndex, 1)?.High ?? 0m;
        var l1 = Get(data, rowIndex, 1)?.Low ?? 0m;
        var c1 = Get(data, rowIndex, 1)?.Close ?? 0m;
        var c2 = Get(data, rowIndex, 2)?.Close ?? 0m;

        return pattern switch
        {
            14 => Math.Abs(o1 - c1) < 1.50m * FiveDayRange(data, rowIndex),
            51 => h1 > (Get(data, rowIndex, 2)?.High ?? 0m) && l1 > (Get(data, rowIndex, 2)?.Low ?? 0m),
            70 => c1 > o1,
            116 => o0 < l1 || o0 > h1,
            144 => c0 > o0,
            _ => false
        };
    }

    public static bool PatternNeutral(OhlcvData[] data, int rowIndex, int pattern)
    {
        if (pattern == 55) return true;
        if (pattern == 56 || pattern <= 0 || rowIndex < 0) return false;

        var bar1 = Get(data, rowIndex, 1);
        if (bar1 == null) return false;

        var range1 = bar1.High - bar1.Low;
        var body1 = Math.Abs(bar1.Open - bar1.Close);
        var h2 = Get(data, rowIndex, 2)?.High ?? 0m;
        var l2 = Get(data, rowIndex, 2)?.Low ?? 0m;
        var open5 = Get(data, rowIndex, 5)?.Open;
        var range5 = FiveDayRange(data, rowIndex);

        return pattern switch
        {
            1 => range1 > 0m && body1 < 0.10m * range1,
            6 => range1 > 0m && body1 > 0.50m * range1,
            17 => open5.HasValue && range5 > 0m && Math.Abs(open5.Value - bar1.Close) > 0.50m * range5,
            14 => open5.HasValue && range5 > 0m && Math.Abs(open5.Value - bar1.Close) < 1.50m * range5,
            47 => range1 < (Range(data, rowIndex, 2) + Range(data, rowIndex, 3)) / 2m,
            49 => bar1.High < h2 && bar1.Low > l2,
            _ => false
        };
    }

    public static bool PatternDirectional(OhlcvData[] data, int rowIndex, int signedPattern)
    {
        if (signedPattern is 52 or -52) return true;
        if (signedPattern == 0 || Math.Abs(signedPattern) >= 53 || rowIndex < 0) return false;

        var sign = Math.Sign(signedPattern);
        var pattern = Math.Abs(signedPattern);
        var o0 = Get(data, rowIndex, 0)?.Open ?? 0m;
        var h1 = Get(data, rowIndex, 1)?.High ?? 0m;
        var l1 = Get(data, rowIndex, 1)?.Low ?? 0m;
        var c1 = Get(data, rowIndex, 1)?.Close ?? 0m;
        var h2 = Get(data, rowIndex, 2)?.High ?? 0m;
        var l2 = Get(data, rowIndex, 2)?.Low ?? 0m;
        var c2 = Get(data, rowIndex, 2)?.Close ?? 0m;
        var c3 = Get(data, rowIndex, 3)?.Close ?? 0m;
        var c4 = Get(data, rowIndex, 4)?.Close ?? 0m;
        var c5 = Get(data, rowIndex, 5)?.Close ?? 0m;

        return pattern switch
        {
            11 => sign > 0 ? c1 > c2 && c2 > c3 && c3 > c4 && c4 > c5 : c1 < c2 && c2 < c3 && c3 < c4 && c4 < c5,
            41 => sign > 0 ? o0 > c1 * 1.005m : o0 < c1 * 0.995m,
            44 => sign > 0 ? l1 > l2 : h1 < h2,
            _ => false
        };
    }

    public static decimal? ToNullableDecimal(object value)
    {
        if (value == null) return null;
        if (value is string text && string.IsNullOrWhiteSpace(text)) return null;
        return Convert.ToDecimal(value);
    }

    private static decimal FiveDayRange(OhlcvData[] data, int rowIndex)
    {
        var bars = Enumerable.Range(1, 5)
            .Select(daysAgo => Get(data, rowIndex, daysAgo))
            .Where(bar => bar != null)
            .Cast<OhlcvData>()
            .ToArray();

        return bars.Length == 0 ? 0m : bars.Max(x => x.High) - bars.Min(x => x.Low);
    }

    private static decimal Range(OhlcvData[] data, int rowIndex, int daysAgo)
    {
        var bar = Get(data, rowIndex, daysAgo);
        return bar == null ? 0m : bar.High - bar.Low;
    }

    private static OhlcvData? Get(OhlcvData[] data, int rowIndex, int daysAgo)
    {
        var index = rowIndex - daysAgo;
        return index >= 0 && index < data.Length ? data[index] : null;
    }

    private static string NormalizeSymbol(string symbol)
    {
        return symbol.Trim().TrimStart('@').ToUpperInvariant();
    }
}
