using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_460
/// Bar-based entry strategy for Gold 30 min
/// </summary>
public class Easy_460_GC_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1800;
    private int _sessionEndTimeA = 1700;
    private int _myPtnLY = 18;
    private int _myPtnSY = 25;
    private int _myPtnLN = 10;
    private int _myPtnSN = 28;
    private int _myLEbar = 36;
    private int _myLXbar = 10;
    private int _mySEbar = 10;
    private int _mySXbar = 36;
    private int _myNotLEDay = 4;
    private int _myNotSEDay = 4;
    private int _myStop = 1700;
    private int _myProfit = 4000;
    private int _myContracts = 1;

    // VARIABLES
    private int _myCount = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_460";
    private string _description = "Bar-based entry strategy for Gold";
    private int _currentMP = 0;
    private int _prevTime = 0;

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
            if (parameters.TryGetValue("sessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("MyPtnLY", out var mply)) _myPtnLY = Convert.ToInt32(mply);
            if (parameters.TryGetValue("MyPtnSY", out var mpsy)) _myPtnSY = Convert.ToInt32(mpsy);
            if (parameters.TryGetValue("MyPtnLN", out var mpln)) _myPtnLN = Convert.ToInt32(mpln);
            if (parameters.TryGetValue("MyPtnSN", out var mpsn)) _myPtnSN = Convert.ToInt32(mpsn);
            if (parameters.TryGetValue("MyLEbar", out var mleb)) _myLEbar = Convert.ToInt32(mleb);
            if (parameters.TryGetValue("MyLXbar", out var mlxb)) _myLXbar = Convert.ToInt32(mlxb);
            if (parameters.TryGetValue("MySEbar", out var mseb)) _mySEbar = Convert.ToInt32(mseb);
            if (parameters.TryGetValue("MySXbar", out var msxb)) _mySXbar = Convert.ToInt32(msxb);
            if (parameters.TryGetValue("MyNotLEDay", out var mnled)) _myNotLEDay = Convert.ToInt32(mnled);
            if (parameters.TryGetValue("MyNotSEDay", out var mnsed)) _myNotSEDay = Convert.ToInt32(mnsed);
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
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        // Reset bar counter at session start
        if (_prevTime < _sessionStartTimeA && currentTime >= _sessionStartTimeA)
        {
            _myCount = 1;
        }
        _myCount++;
        _prevTime = currentTime;

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        OHLCMulti5(_sessionStartTimeA, _sessionEndTimeA, data, currentDate, out ohlcValues);

        // Long exit on bar count
        if (_myCount == _myLXbar && _currentMP == 1)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Sell,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "LX BarCount"
            };
        }

        // Short exit on bar count
        if (_myCount == _mySXbar && _currentMP == -1)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Buy,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "SX BarCount"
            };
        }

        // Long entry on bar count with pattern
        if (_myCount == _myLEbar && _currentMP <= 0 &&
            PtnBaseSA2(_myPtnLY, ohlcValues) && !PtnBaseSA2(_myPtnLN, ohlcValues) &&
            (int)currentDate.DayOfWeek != _myNotLEDay)
        {
            _currentMP = 1;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Buy,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "LE BarCount Pattern"
            };
        }

        // Short entry on bar count with pattern
        if (_myCount == _mySEbar && _currentMP >= 0 &&
            PtnBaseSA2(_myPtnSY, ohlcValues) && !PtnBaseSA2(_myPtnSN, ohlcValues) &&
            (int)currentDate.DayOfWeek != _myNotSEDay)
        {
            _currentMP = -1;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Sell,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "SE BarCount Pattern"
            };
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
