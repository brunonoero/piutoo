using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_291
/// UA_Trend Developer x Gold 15 min
/// </summary>
public class Easy_291_GC_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _myContracts = 1;
    private int _sessionStartTimeA = 1800;
    private int _sessionEndTimeA = 1700;
    private int _maxTradesPerDay = 4;
    private int _ptnDirYes = 48;
    private int _ptnDirNo = 13;
    private int _ptnNeutYes = 16;
    private int _ptnNeutNo = 30;
    private int _myStop = 2300;
    private int _myProfit = 5000;
    private int _myTrigger = 1;
    private int _myStartTrade = 0;
    private int _myEndTrade = 400;
    private int _closeAtTime = 2500;
    private int _sessTest = 0;
    private int _dStop = 432;

    // VARIABLES
    private decimal _highd0 = 0;
    private decimal _lowd0 = 0;
    private decimal _myLE = 99999;
    private decimal _mySE = 0;
    private decimal _highd1 = 0;
    private decimal _lowd1 = 0;
    private int _myEndTime = 0;
    private int _endSession = 0;
    private int _calcStart = 0;
    private int _calcEnd = 0;
    private int _barsSinceEntry = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_291";
    private string _description = "UA_Trend Developer x Gold 15 min";
    private int _currentMP = 0;
    private int _entriesToday = 0;
    private DateTime? _lastTradeDate = null;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        // End session setting
        if (_sessionEndTimeA >= 2400) _endSession = _sessionStartTimeA;
        else _endSession = _sessionEndTimeA;

        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("MyContracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("sessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("MaxTradesPerDay", out var mtpd)) _maxTradesPerDay = Convert.ToInt32(mtpd);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnDirNo", out var pdn)) _ptnDirNo = Convert.ToInt32(pdn);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("MyTrigger", out var mt)) _myTrigger = Convert.ToInt32(mt);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("DStop", out var ds)) _dStop = Convert.ToInt32(ds);
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

        // Reset entries counter on new day
        if (_lastTradeDate == null || _lastTradeDate.Value.Date != currentDate.Date)
        {
            _entriesToday = 0;
            _lastTradeDate = currentDate;
        }

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        var isStartOfSession = OHLCMulti5(_sessionStartTimeA, _endSession, data, currentDate, out ohlcValues);

        _highd0 = ohlcValues[1];
        _lowd0 = ohlcValues[2];
        _highd1 = ohlcValues[5];
        _lowd1 = ohlcValues[6];

        // Setting of time to close positions
        if (isStartOfSession)
        {
            _myEndTime = UACalcEndTime(_sessionStartTimeA, _endSession, currentDate);
        }
        if (_closeAtTime != 2500) _myEndTime = _closeAtTime;

        // Levels choice based on trigger
        if (_myTrigger == 0)
        {
            _myLE = (decimal)Highest(data, 24, d => d.High); // Current session high approximation
            _mySE = (decimal)Lowest(data, 24, d => d.Low);   // Current session low approximation
        }
        else if (_myTrigger == 1)
        {
            _myLE = _highd0;
            _mySE = _lowd0;
        }
        else if (_myTrigger == 2)
        {
            _myLE = _highd1;
            _mySE = _lowd1;
        }

        // Time window calculation
        if (_sessTest == 0)
        {
            _calcStart = _myStartTrade;
            _calcEnd = _myEndTrade;
        }
        else
        {
            _calcStart = _sessionStartTimeA + 200;
            if (_calcStart > 2359) _calcStart -= 2400;
            _calcEnd = _endSession - 200;
            if (_calcEnd < 0) _calcEnd += 2400;
        }

        bool timeWindow = TimeWindow(_calcStart, _calcEnd, currentDate);

        // Track bars since entry
        if (_currentMP != 0) _barsSinceEntry++;
        else _barsSinceEntry = 0;

        // Exit on bars since entry
        if (_barsSinceEntry > _dStop && _currentMP != 0)
        {
            var exitMP = _currentMP;
            _currentMP = 0;
            _barsSinceEntry = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = exitMP == 1 ? SignalType.Sell : SignalType.Buy,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "Exit DStop"
            };
        }

        // Trading conditions
        if (timeWindow && _entriesToday < _maxTradesPerDay &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
        {
            // Long entry
            if (_currentMP <= 0 && PatternDirectionalFast(_ptnDirYes, ohlcValues) && 
                !PatternDirectionalFast(_ptnDirNo, ohlcValues))
            {
                if (currentHigh >= _myLE)
                {
                    _currentMP = 1;
                    _entriesToday++;
                    _barsSinceEntry = 1;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _myLE,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE Breakout"
                    };
                }
            }

            // Short entry
            if (_currentMP >= 0 && PatternDirectionalFast(-_ptnDirYes, ohlcValues) && 
                !PatternDirectionalFast(-_ptnDirNo, ohlcValues))
            {
                if (currentLow <= _mySE)
                {
                    _currentMP = -1;
                    _entriesToday++;
                    _barsSinceEntry = 1;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _mySE,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE Breakout"
                    };
                }
            }
        }

        // End of session exit
        if (_myTrigger >= 1 && currentTime == _myEndTime && _currentMP != 0)
        {
            var exitMP = _currentMP;
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = exitMP == 1 ? SignalType.Sell : SignalType.Buy,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "EOSess"
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

    private static int UACalcEndTime(int startTime, int endTime, DateTime currentDate)
    {
        // Simplified calculation - returns end time
        return endTime;
    }
}
