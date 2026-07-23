using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_666
/// Intraday breakout strategy for Gold 5 min
/// </summary>
public class Easy_666_GC_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1800;
    private int _sessionEndTimeA = 1715;
    private int _myContracts = 1;
    private decimal _stopLoss = 7.5m;
    private decimal _breakEven = 15m;
    private decimal _takeProfit = 45m;
    private int _monthUno = 7;
    private int _monthDue = 8;

    // VARIABLES
    private bool _okL = true;
    private bool _okS = true;
    private int _myDOW = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_666";
    private string _description = "Intraday breakout strategy for Gold";
    private int _currentMP = 0;
    private decimal _entryPrice = 0;
    private bool _prevIsStartOfSession = false;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("MyContracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("StopLoss", out var sl)) _stopLoss = Convert.ToDecimal(sl);
            if (parameters.TryGetValue("BreakEven", out var be)) _breakEven = Convert.ToDecimal(be);
            if (parameters.TryGetValue("TakeProfit", out var tp)) _takeProfit = Convert.ToDecimal(tp);
            if (parameters.TryGetValue("MonthUno", out var mu)) _monthUno = Convert.ToInt32(mu);
            if (parameters.TryGetValue("MonthDue", out var md)) _monthDue = Convert.ToInt32(md);
        }
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data == null || data.Length < RequiredCandles)
        {
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Hold,
                Price = data?.LastOrDefault()?.Close ?? 0,
                StrategyName = Name,
                Reason = "Dati insufficienti"
            };
        }

        var currentPrice = data.Last().Close;
        var currentHigh = data.Last().High;
        var currentLow = data.Last().Low;
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        var isStartOfSession = OHLCMulti5(_sessionStartTimeA, _sessionEndTimeA, data, currentDate, out ohlcValues);

        var opend0 = ohlcValues[0];
        var highd0 = ohlcValues[1];
        var lowd0 = ohlcValues[2];
        var closed0 = ohlcValues[3];
        var opend1 = ohlcValues[4];
        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];
        var closed1 = ohlcValues[7];
        var closed2 = ohlcValues[11];

        // Reset at session start
        if (_prevIsStartOfSession)
        {
            _okL = true;
            _okS = true;
            _myDOW = (int)currentDate.DayOfWeek + 1;
        }
        _prevIsStartOfSession = isStartOfSession;

        // Exit at session end or on session start
        if ((currentTime >= 1654 && currentTime < 1800) || isStartOfSession)
        {
            if (_currentMP == 1)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LX Session End"
                };
            }
            if (_currentMP == -1)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SX Session End"
                };
            }
        }

        // Stop loss exits
        if (_currentMP == 1)
        {
            _okL = false;
            if (currentLow <= _entryPrice - _stopLoss)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = _entryPrice - _stopLoss,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LX Stop"
                };
            }
        }

        if (_currentMP == -1)
        {
            _okS = false;
            if (currentHigh >= _entryPrice + _stopLoss)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = _entryPrice + _stopLoss,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SX Stop"
                };
            }
        }

        // Entry conditions
        int currentMonth = currentDate.Month;
        bool monthFilter = currentMonth != _monthUno && currentMonth != _monthDue;
        bool timeFilter = currentTime < 1530 || currentTime >= 2100;
        bool closedFilter = !((closed1 < (closed2 - closed2 * 0.5m / 100m)) || (closed1 > (closed2 + closed2 * 0.5m / 100m)));

        if (monthFilter && timeFilter && closedFilter)
        {
            // Long entry
            if ((highd0 - opend0) > ((highd1 - opend1) * 0.75m) && _okL && _myDOW != 1)
            {
                if (currentHigh >= highd1)
                {
                    _currentMP = 1;
                    _entryPrice = highd1;
                    _okL = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = highd1,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE Breakout HighD1"
                    };
                }
            }

            // Short entry
            if ((opend0 - lowd0) > ((opend1 - lowd1) * 0.75m) && _okS && _myDOW != 4)
            {
                if (currentLow <= lowd1)
                {
                    _currentMP = -1;
                    _entryPrice = lowd1;
                    _okS = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = lowd1,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE Breakout LowD1"
                    };
                }
            }
        }

        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = currentPrice,
            StrategyName = Name
        };
    }
}
