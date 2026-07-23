using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_303
/// Breakout strategy with ADX filter for Gold 15 min (uses Data2 for ADX)
/// </summary>
public class Easy_303_GC_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1800;
    private int _sessionEndTimeA = 1700;
    private int _maxTradesPerDay = 1;
    private int _ptnDirYes = -47;
    private int _ptnNeutYes = 26;
    private int _ptnNeutNo = 1;
    private int _ptnLY = 41;
    private int _ptnLN = 42;
    private int _ptnSY = 41;
    private int _ptnSN = 42;
    private int _myStop = 2400;
    private int _myProfit = 4500;
    private int _myBE = 2650;
    private int _id = 1;
    private int _myTrigger = 2;
    private int _myStartTrade = 0;
    private int _myEndTrade = 1600;
    private int _closeAtTime = 2500;
    private int _myADXLength = 5;
    private int _myADXThreshold = 60;
    private int _exitModeDaysMax = 0;
    private int _maxDaysInTrade = 9;
    private int _notDayLE = 1;
    private int _notDaySE = 0;
    private int _notMonthLE = 11;
    private int _notMonthSE = 8;
    private int _myContracts = 1;

    // VARIABLES
    private int _endSession = 0;
    private decimal _myLE = 99999;
    private decimal _mySE = 0;
    private int _myEndTime = 0;
    private int _entriesToday = 0;
    private DateTime? _lastTradeDate = null;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_303";
    private string _description = "Breakout strategy with ADX filter";
    private int _currentMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (_sessionEndTimeA >= 2400) _endSession = _sessionStartTimeA;
        else _endSession = _sessionEndTimeA;

        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("sessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("MaxTradesPerDay", out var mtpd)) _maxTradesPerDay = Convert.ToInt32(mtpd);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("MyTrigger", out var mt)) _myTrigger = Convert.ToInt32(mt);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("MyADXThreshold", out var mat)) _myADXThreshold = Convert.ToInt32(mat);
            if (parameters.TryGetValue("MaxDaysInTrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
            if (parameters.TryGetValue("MyContracts", out var mc)) _myContracts = Convert.ToInt32(mc);
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

        // Reset entries on new day
        if (_lastTradeDate == null || _lastTradeDate.Value.Date != currentDate.Date)
        {
            _entriesToday = 0;
            _lastTradeDate = currentDate;
        }

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        OHLCMulti5(_sessionStartTimeA, _endSession, data, currentDate, out ohlcValues);

        var highd0 = ohlcValues[1];
        var lowd0 = ohlcValues[2];
        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];

        // Levels choice based on trigger
        if (_myTrigger == 0)
        {
            _myLE = Highest(data, 24, d => d.High);
            _mySE = Lowest(data, 24, d => d.Low);
        }
        else if (_myTrigger == 1)
        {
            _myLE = highd0;
            _mySE = lowd0;
        }
        else if (_myTrigger == 2)
        {
            _myLE = highd1;
            _mySE = lowd1;
        }

        // Calculate ADX (simplified - using ATR as proxy)
        var atr = AvgTrueRange(data, _myADXLength);
        var priceRange = currentHigh - currentLow;
        var adxValue = priceRange > 0 ? (atr / priceRange) * 100 : 50;

        // Time window calculation
        int calcStart = _myStartTrade;
        int calcEnd = _myEndTrade;
        bool timeWindow = TimeWindow(calcStart, calcEnd, currentDate);

        // Trading conditions
        if (timeWindow && _entriesToday < _maxTradesPerDay &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && PatternNeutralFast(43, ohlcValues) &&
            (!PatternNeutralFast(_ptnNeutNo, ohlcValues) || !PatternNeutralFast(23, ohlcValues)) &&
            adxValue <= _myADXThreshold)
        {
            // Long entry
            if (_currentMP <= 0 && (int)currentDate.DayOfWeek != _notDayLE && currentDate.Month != _notMonthLE &&
                PtnBaseSA2(_ptnLY, ohlcValues) && !PtnBaseSA2(_ptnLN, ohlcValues) &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) && PatternDirectionalFast(-9, ohlcValues))
            {
                if (currentHigh >= _myLE)
                {
                    _currentMP = 1;
                    _entriesToday++;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _myLE,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE ADX Breakout"
                    };
                }
            }

            // Short entry
            if (_currentMP >= 0 && (int)currentDate.DayOfWeek != _notDaySE && currentDate.Month != _notMonthSE &&
                PtnBaseSA2(_ptnSY, ohlcValues) && !PtnBaseSA2(_ptnSN, ohlcValues) &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) && PatternDirectionalFast(9, ohlcValues))
            {
                if (currentLow <= _mySE)
                {
                    _currentMP = -1;
                    _entriesToday++;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _mySE,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE ADX Breakout"
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
