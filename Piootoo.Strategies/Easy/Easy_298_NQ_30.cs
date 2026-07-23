using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_298
/// N-Session Breakout strategy for NQ 30 min
/// </summary>
public class Easy_298_NQ_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1700;
    private int _sessionEndTimeA = 1600;
    private int _myContracts = 1;
    private int _myStartTime = 1200;
    private int _myEndTime = 1600;
    private int _myStartPause = 1200;
    private int _myEndPause = 1100;
    private int _id = 1;
    private int _myPtnLY = 41;
    private int _myPtnSY = 19;
    private int _myPtnLN = 36;
    private int _myPtnSN = 35;
    private int _myDayNoShort = -1;
    private int _myDayNoLong = -1;
    private int _maxDaysInTrade = 4;
    private int _noTradingMonthLong = 6;
    private int _noTradingMonthShort = 4;
    private int _nSessions = 1;
    private int _myStop = 1800;
    private int _myBreakeven = 0;
    private int _myProfit = 2500;

    // VARIABLES
    private decimal _longLevel = 0;
    private decimal _shortLevel = 0;
    private int _daysInTrade = 0;
    private bool _okL = true;
    private bool _okS = true;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_298";
    private string _description = "N-Session Breakout strategy for NQ";
    private int _currentMP = 0;
    private int _prevMP = 0;

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
            if (parameters.TryGetValue("SessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("MyContracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("MyStartTime", out var mst)) _myStartTime = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTime", out var met)) _myEndTime = Convert.ToInt32(met);
            if (parameters.TryGetValue("ID", out var id)) _id = Convert.ToInt32(id);
            if (parameters.TryGetValue("MyPtnLY", out var mply)) _myPtnLY = Convert.ToInt32(mply);
            if (parameters.TryGetValue("MyPtnSY", out var mpsy)) _myPtnSY = Convert.ToInt32(mpsy);
            if (parameters.TryGetValue("MyPtnLN", out var mpln)) _myPtnLN = Convert.ToInt32(mpln);
            if (parameters.TryGetValue("MyPtnSN", out var mpsn)) _myPtnSN = Convert.ToInt32(mpsn);
            if (parameters.TryGetValue("maxdaysintrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
            if (parameters.TryGetValue("NSessions", out var ns)) _nSessions = Convert.ToInt32(ns);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
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

        // Calculate levels based on N sessions
        _longLevel = ohlcValues[1 + 4 * _nSessions];
        _shortLevel = ohlcValues[2 + 4 * _nSessions];

        if (isStartOfSession)
        {
            _okL = true;
            _okS = true;
            if (_currentMP != 0) _daysInTrade++;
        }

        // Position management
        _prevMP = _currentMP;
        if (_currentMP != _prevMP && _currentMP != 0) _daysInTrade = 1;
        if (_currentMP == 1) _okL = false;
        if (_currentMP == -1) _okS = false;

        // Max days exit
        if (_maxDaysInTrade > 0 && _daysInTrade >= _maxDaysInTrade)
        {
            // Simplified - would need setexitonclose equivalent
        }

        // Time window and pause check
        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);
        bool notInPause = currentTime < _myStartPause || currentTime > _myEndPause;

        // Entry conditions
        if (inTimeWindow && notInPause)
        {
            // Long entry
            if (_okL && (int)currentDate.DayOfWeek != _myDayNoLong && currentDate.Month != _noTradingMonthLong &&
                PtnBaseSA2(_myPtnLY, ohlcValues) && !PtnBaseSA2(_myPtnLN, ohlcValues))
            {
                if (currentHigh >= _longLevel)
                {
                    _currentMP = 1;
                    _okL = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _longLevel,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE N-Session Breakout"
                    };
                }
            }

            // Short entry
            if (_okS && (int)currentDate.DayOfWeek != _myDayNoShort && currentDate.Month != _noTradingMonthShort &&
                PtnBaseSA2(_myPtnSY, ohlcValues) && !PtnBaseSA2(_myPtnSN, ohlcValues))
            {
                if (currentLow <= _shortLevel)
                {
                    _currentMP = -1;
                    _okS = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _shortLevel,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE N-Session Breakout"
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
