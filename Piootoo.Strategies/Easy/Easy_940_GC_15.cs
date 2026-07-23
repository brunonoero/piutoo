using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_940
/// Pivot point reversal strategy for Gold 15 min
/// </summary>
public class Easy_940_GC_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1800;
    private int _sessionEndTimeA = 1700;
    private int _myContracts = 1;
    private int _levelChoice = 2;
    private decimal _levelShift = 0;
    private decimal _myTick = 0.1m;
    private int _ptnDirYes = -49;
    private int _ptnNeutYes = 4;
    private int _ptnNeutNo = 1;
    private int _ptnLY = 41;
    private int _ptnLN = 42;
    private int _ptnSY = 41;
    private int _ptnSN = 42;
    private int _myStop = 1100;
    private int _myProfit = 5000;
    private int _id = 1;
    private int _myDayNoLong = 1;
    private int _myDayNoShort = 5;
    private int _myStartTrade = 2300;
    private int _myEndTrade = 1600;
    private int _closeAtTime = 2500;
    private int _sessTest = 0;

    // VARIABLES
    private int _endSession = 0;
    private int _calcStart = 0;
    private int _calcEnd = 0;
    private decimal _myPivot = 0;
    private decimal _myR1 = 0;
    private decimal _myS1 = 0;
    private decimal _myR2 = 0;
    private decimal _myS2 = 0;
    private decimal _myLETrigger = 0;
    private decimal _mySETrigger = 99999;
    private int _myEndTime = 0;
    private decimal _prevClose = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_940";
    private string _description = "Pivot point reversal strategy";
    private int _currentMP = 0;

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
            if (parameters.TryGetValue("sessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("MyContracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("LevelChoice", out var lc)) _levelChoice = Convert.ToInt32(lc);
            if (parameters.TryGetValue("LevelShift", out var ls)) _levelShift = Convert.ToDecimal(ls);
            if (parameters.TryGetValue("MyTick", out var mt)) _myTick = Convert.ToDecimal(mt);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("PtnLY", out var ply)) _ptnLY = Convert.ToInt32(ply);
            if (parameters.TryGetValue("PtnLN", out var pln)) _ptnLN = Convert.ToInt32(pln);
            if (parameters.TryGetValue("PtnSY", out var psy)) _ptnSY = Convert.ToInt32(psy);
            if (parameters.TryGetValue("PtnSN", out var psn)) _ptnSN = Convert.ToInt32(psn);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("ID", out var id)) _id = Convert.ToInt32(id);
            if (parameters.TryGetValue("mydayNolong", out var mdnl)) _myDayNoLong = Convert.ToInt32(mdnl);
            if (parameters.TryGetValue("mydayNoshort", out var mdns)) _myDayNoShort = Convert.ToInt32(mdns);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("CloseAtTime", out var cat)) _closeAtTime = Convert.ToInt32(cat);
            if (parameters.TryGetValue("sesstest", out var st)) _sessTest = Convert.ToInt32(st);
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
        var closed1 = ohlcValues[7];

        // Time window settings
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

        // Calculate end time and pivot levels at session start
        if (isStartOfSession)
        {
            _myEndTime = _endSession;
            if (_closeAtTime != 2500) _myEndTime = _closeAtTime;

            _myPivot = (highd1 + lowd1 + closed1) / 3;
            _myR1 = 2 * _myPivot - lowd1;
            _myS1 = 2 * _myPivot - highd1;
            _myR2 = _myPivot + highd1 - lowd1;
            _myS2 = _myPivot - highd1 + lowd1;

            if (_levelChoice == 1)
            {
                _mySETrigger = _myR1 + _levelShift * _myTick;
                _myLETrigger = _myS1 - _levelShift * _myTick;
            }
            else if (_levelChoice == 2)
            {
                _mySETrigger = highd1 + _levelShift * _myTick;
                _myLETrigger = lowd1 - _levelShift * _myTick;
            }
        }

        // End of session exit
        if (_id == 0 && currentTime == _myEndTime && _currentMP != 0)
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
                Reason = exitMP == 1 ? "LX_EOSess" : "SX_EOSess"
            };
        }

        // Entry conditions
        if (PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues) && timeWindow)
        {
            // Long entry on cross over trigger level
            if ((int)currentDate.DayOfWeek != _myDayNoLong && _currentMP <= 0 &&
                UAPtnBase(_ptnLY, ohlcValues) && !UAPtnBase(_ptnLN, ohlcValues) &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) &&
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
                    Reason = "LE Pivot Cross"
                };
            }

            // Short entry on cross under trigger level
            if ((int)currentDate.DayOfWeek != _myDayNoShort && _currentMP >= 0 &&
                UAPtnBase(_ptnSY, ohlcValues) && !UAPtnBase(_ptnSN, ohlcValues) &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) &&
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
                    Reason = "SE Pivot Cross"
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
