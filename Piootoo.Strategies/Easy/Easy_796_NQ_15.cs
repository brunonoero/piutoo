using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_796
/// ATR-based breakout strategy for NQ 15 min (uses Data2 for ATR)
/// </summary>
public class Easy_796_NQ_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1700;
    private int _sessionEndTimeA = 1559;
    private int _maxTradesPerDay = 4;
    private int _ptnDirYes = 52;
    private int _ptnNeutYes = 33;
    private int _ptnNeutNo = 23;
    private int _ptnLY = 41;
    private int _ptnLN = 42;
    private int _ptnSY = 19;
    private int _ptnSN = 32;
    private int _myStop = 2120;
    private int _myProfit = 5160;
    private int _myBE = 3130;
    private int _id = 1;
    private int _myTrigger = 1;
    private int _myStartTrade = 730;
    private int _myEndTrade = 1530;
    private int _closeAtTime = 2500;
    private int _atrLength = 23;
    private int _atrLengthID = 9;
    private int _atrThreshold = 35;
    private decimal _atrMult = 0.35m;
    private int _maxDaysInTrade = 9;
    private int _exitModeDaysMax = 1;
    private int _myContracts = 1;

    // VARIABLES
    private int _endSession = 0;
    private decimal _myLE = 99999;
    private decimal _mySE = 0;
    private int _myEndTime = 0;
    private int _entriesToday = 0;
    private DateTime? _lastTradeDate = null;
    private DateTime? _entryDate = null;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_796";
    private string _description = "ATR-based breakout strategy for NQ";
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
            if (parameters.TryGetValue("sessionStartTimeAHour", out var ssth)) _sessionStartTimeA = Convert.ToInt32(ssth);
            if (parameters.TryGetValue("sessionEndTimeAHour", out var seth)) _sessionEndTimeA = Convert.ToInt32(seth);
            if (parameters.TryGetValue("MaxTradesPerDay", out var mtpd)) _maxTradesPerDay = Convert.ToInt32(mtpd);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("MyTrigger", out var mt)) _myTrigger = Convert.ToInt32(mt);
            if (parameters.TryGetValue("MyStartTradeHour", out var msth)) _myStartTrade = Convert.ToInt32(msth);
            if (parameters.TryGetValue("MyEndTradeHour", out var meth)) _myEndTrade = Convert.ToInt32(meth);
            if (parameters.TryGetValue("ATRLength", out var al)) _atrLength = Convert.ToInt32(al);
            if (parameters.TryGetValue("ATRThreshold", out var at)) _atrThreshold = Convert.ToInt32(at);
            if (parameters.TryGetValue("ATRMult", out var am)) _atrMult = Convert.ToDecimal(am);
            if (parameters.TryGetValue("MaxDaysInTrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
            if (parameters.TryGetValue("ExitModeDaysMax", out var emdm)) _exitModeDaysMax = Convert.ToInt32(emdm);
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

        var opend0 = ohlcValues[0];
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

        // Calculate ATR values
        var atrValue = AvgTrueRange(data, _atrLength);
        var atrValueID = AvgTrueRange(data, _atrLengthID);

        // Calculate days in trade
        int daysInTrade = 0;
        if (_entryDate.HasValue && _currentMP != 0)
        {
            daysInTrade = (currentDate.Date - _entryDate.Value.Date).Days;
        }

        // Time window calculation
        int calcStart = _myStartTrade;
        int calcEnd = _myEndTrade;
        bool timeWindow = TimeWindow(calcStart, calcEnd, currentDate);

        // Exit on max days in trade
        if (_exitModeDaysMax == 1 && daysInTrade >= _maxDaysInTrade && _currentMP != 0)
        {
            if (currentTime >= 1545 && currentTime < 1600)
            {
                var exitMP = _currentMP;
                _currentMP = 0;
                _entryDate = null;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = exitMP == 1 ? SignalType.Sell : SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "MaxDays Exit"
                };
            }
        }

        // Trading conditions
        if (timeWindow && _entriesToday < _maxTradesPerDay &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
        {
            // Long entry
            if (_currentMP <= 0 &&
                PtnBaseSA2(_ptnLY, ohlcValues) && !PtnBaseSA2(_ptnLN, ohlcValues) &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) &&
                currentPrice >= (opend0 + atrValue * _atrMult) &&
                atrValueID <= _atrThreshold)
            {
                _currentMP = 1;
                _entriesToday++;
                _entryDate = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LE ATR Breakout"
                };
            }

            // Short entry
            if (_currentMP >= 0 &&
                PtnBaseSA2(_ptnSY, ohlcValues) && !PtnBaseSA2(_ptnSN, ohlcValues) &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) &&
                currentPrice <= (opend0 - atrValue * _atrMult) &&
                atrValueID <= _atrThreshold)
            {
                _currentMP = -1;
                _entriesToday++;
                _entryDate = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SE ATR Breakout"
                };
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
