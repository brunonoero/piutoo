using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_916
/// Mean Reverting Level Fader strategy for Gold 15 min
/// </summary>
public class Easy_916_GC_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1800;
    private int _sessionEndTimeA = 1700;
    private int _myContracts = 1;
    private int _levelShift = 13;
    private decimal _myTick = 0.1m;
    private int _maxTradesPerDay = 2;
    private int _ptnLY = 41;
    private int _ptnLN = 42;
    private int _ptnSY = 23;
    private int _ptnSN = 42;
    private int _ptnNeutYes = 55;
    private int _ptnNeutYes2 = 55;
    private int _ptnNeutNo = 52;
    private int _ptnDirYes = 52;
    private int _ptnDirNo = 37;
    private int _ptnDirNo2 = 38;
    private int _myStartTime = 2000;
    private int _myEndTime = 1530;
    private int _myStartPause = 130;
    private int _myEndPause = 400;
    private int _maxDaysLong = 1;
    private int _maxDaysShort = 1;
    private int _flatTime = 1645;
    private int _skipSess = -1;
    private int _myStop = 1400;
    private int _myBreakeven = 0;
    private int _myProfit = 0;
    private decimal _multRng = 0.065m;
    private int _periodRng = 2;

    // VARIABLES
    private int _endSession = 0;
    private bool _okL = true;
    private bool _okS = true;
    private int _daysInTrade = 0;
    private int _soW = 0;
    private bool _sessAcross2Days = false;
    private int _flatTimeFixed = 0;
    private int _mySessionEntries = 0;
    private decimal _myLETrigger = 0;
    private decimal _mySETrigger = 99999;
    private decimal _prevClose = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_916";
    private string _description = "Mean Reverting Level Fader strategy";
    private int _currentMP = 0;
    private int _prevMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (_sessionEndTimeA >= 2400) _endSession = _sessionStartTimeA;
        else _endSession = _sessionEndTimeA;

        _sessAcross2Days = _sessionStartTimeA > _sessionEndTimeA;
        if (_sessAcross2Days) _flatTimeFixed = _sessionStartTimeA;
        else _flatTimeFixed = _sessionEndTimeA;
        if (_sessionStartTimeA == 0 && _sessionEndTimeA == 0) _flatTimeFixed = 2359;

        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("sessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("mycontracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("LevelShift", out var ls)) _levelShift = Convert.ToInt32(ls);
            if (parameters.TryGetValue("MyTick", out var mt)) _myTick = Convert.ToDecimal(mt);
            if (parameters.TryGetValue("MaxTradesPerDay", out var mtpd)) _maxTradesPerDay = Convert.ToInt32(mtpd);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("ptnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("ptnDirNo", out var pdn)) _ptnDirNo = Convert.ToInt32(pdn);
            if (parameters.TryGetValue("MyStartTime", out var mst)) _myStartTime = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTime", out var met)) _myEndTime = Convert.ToInt32(met);
            if (parameters.TryGetValue("MaxDaysLong", out var mdl)) _maxDaysLong = Convert.ToInt32(mdl);
            if (parameters.TryGetValue("MaxDaysShort", out var mds)) _maxDaysShort = Convert.ToInt32(mds);
            if (parameters.TryGetValue("flatTime", out var ft)) _flatTime = Convert.ToInt32(ft);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
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

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        var isStartOfSession = OHLCMulti5(_sessionStartTimeA, _endSession, data, currentDate, out ohlcValues);

        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];

        if (isStartOfSession)
        {
            _okL = true;
            _okS = true;
            if (_currentMP != 0) _daysInTrade++;
            _mySessionEntries = 0;

            if (_sessAcross2Days) _soW = (int)currentDate.DayOfWeek + 1;
            else _soW = (int)currentDate.DayOfWeek;

            _mySETrigger = highd1 + _levelShift * _myTick;
            _myLETrigger = lowd1 - _levelShift * _myTick;
        }

        if (_currentMP == 1) _okL = false;
        if (_currentMP == -1) _okS = false;

        _prevMP = _currentMP;
        if (_currentMP != _prevMP && _currentMP != 0)
        {
            _mySessionEntries++;
            _daysInTrade = 1;
        }

        // Volatility filter condition
        var openPeriodRng = data.Length > _periodRng ? data[data.Length - 1 - _periodRng].Open : currentPrice;
        var highestPeriod = Highest(data, _periodRng, d => d.High);
        var lowestPeriod = Lowest(data, _periodRng, d => d.Low);
        bool condition2 = Math.Abs(openPeriodRng - currentPrice) > (highestPeriod - lowestPeriod) * _multRng;

        // End of day exit
        int exitBeg = 1630;
        int exitEnd = 1645;
        bool myExitTime = currentTime >= exitBeg && currentTime <= exitEnd;

        if (myExitTime && _currentMP != 0)
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
                Reason = exitMP == 1 ? "Eod_Long" : "Eod_Short"
            };
        }

        // Max days exit
        if (_currentMP == 1 && _maxDaysLong > 0 && _daysInTrade >= _maxDaysLong)
        {
            if (currentTime >= _flatTime && currentTime < _flatTimeFixed)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LX_MaxDays"
                };
            }
        }

        if (_currentMP == -1 && _maxDaysShort > 0 && _daysInTrade >= _maxDaysShort)
        {
            if (currentTime >= _flatTime && currentTime < _flatTimeFixed)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SX_MaxDays"
                };
            }
        }

        // Entry conditions - Mean Reverting on false breakout
        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);
        bool notInPause = currentTime < _myStartPause || currentTime > _myEndPause;

        if (inTimeWindow && notInPause &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && PatternNeutralFast(_ptnNeutYes2, ohlcValues) &&
            !PatternNeutralFast(_ptnNeutNo, ohlcValues) &&
            _mySessionEntries < _maxTradesPerDay && condition2)
        {
            // Long entry - price crossed below trigger then back above
            if (_okL && _soW != _skipSess &&
                UAPtnBase(_ptnLY, ohlcValues) && !UAPtnBase(_ptnLN, ohlcValues) &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) && !PatternDirectionalFast(_ptnDirNo, ohlcValues) &&
                !PatternDirectionalFast(_ptnDirNo2, ohlcValues) &&
                _prevClose < _myLETrigger && currentPrice > _myLETrigger)
            {
                _currentMP = 1;
                _prevClose = currentPrice;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LE Level Fader"
                };
            }

            // Short entry - price crossed above trigger then back below
            if (_okS && _soW != _skipSess &&
                UAPtnBase(_ptnSY, ohlcValues) && !UAPtnBase(_ptnSN, ohlcValues) &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) && !PatternDirectionalFast(-_ptnDirNo, ohlcValues) &&
                !PatternDirectionalFast(-_ptnDirNo2, ohlcValues) &&
                _prevClose > _mySETrigger && currentPrice < _mySETrigger)
            {
                _currentMP = -1;
                _prevClose = currentPrice;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SE Level Fader"
                };
            }
        }

        _prevClose = currentPrice;

        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = currentPrice,
            StrategyName = Name
        };
    }
}
