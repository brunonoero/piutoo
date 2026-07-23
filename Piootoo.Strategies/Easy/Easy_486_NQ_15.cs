using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_486
/// ATR Breakout strategy for NQ 15 min
/// </summary>
public class Easy_486_NQ_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1700;
    private int _sessionEndTimeA = 1600;
    private int _mySize = 1;
    private int _twOn = 1;
    private int _myStartTrade = 900;
    private int _myEndTrade = 1300;
    private decimal _atrFactorL = 6;
    private decimal _atrFactorS = 8.5m;
    private int _myAtrLen = 500;
    private int _exitBeg = 1530;
    private int _exitEnd = 1545;
    private int _stopLoss = 1300;
    private int _takeProfit = 4000;
    private int _ptnFastLongYes = 26;
    private int _ptnFastShortYes = 57;
    private int _ptnFastLongNo = 62;
    private int _ptnFastShortNo = 23;

    // VARIABLES
    private decimal _myATR = 0;
    private decimal _lEntryLevel = 0;
    private decimal _sEntryLevel = 0;
    private int _entriesToday = 0;
    private DateTime? _lastTradeDate = null;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_486";
    private string _description = "ATR Breakout strategy for NQ";
    private int _currentMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 600;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("sessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("MySize", out var ms)) _mySize = Convert.ToInt32(ms);
            if (parameters.TryGetValue("TW_ON", out var two)) _twOn = Convert.ToInt32(two);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("ATRFactorL", out var afl)) _atrFactorL = Convert.ToDecimal(afl);
            if (parameters.TryGetValue("ATRFactorS", out var afs)) _atrFactorS = Convert.ToDecimal(afs);
            if (parameters.TryGetValue("MyAtrLen", out var mal)) _myAtrLen = Convert.ToInt32(mal);
            if (parameters.TryGetValue("ExitBeg", out var eb)) _exitBeg = Convert.ToInt32(eb);
            if (parameters.TryGetValue("ExitEnd", out var ee)) _exitEnd = Convert.ToInt32(ee);
            if (parameters.TryGetValue("StopLoss", out var sl)) _stopLoss = Convert.ToInt32(sl);
            if (parameters.TryGetValue("TakeProfit", out var tp)) _takeProfit = Convert.ToInt32(tp);
            if (parameters.TryGetValue("PtnFastLongYes", out var pfly)) _ptnFastLongYes = Convert.ToInt32(pfly);
            if (parameters.TryGetValue("PtnFastShortYes", out var pfsy)) _ptnFastShortYes = Convert.ToInt32(pfsy);
            if (parameters.TryGetValue("PtnFastLongNo", out var pfln)) _ptnFastLongNo = Convert.ToInt32(pfln);
            if (parameters.TryGetValue("PtnFastShortNo", out var pfsn)) _ptnFastShortNo = Convert.ToInt32(pfsn);
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
        OHLCMulti5(_sessionStartTimeA, _sessionEndTimeA, data, currentDate, out ohlcValues);

        var openS0 = ohlcValues[0];

        // Calculate ATR
        _myATR = AvgTrueRange(data, Math.Min(_myAtrLen, data.Length - 1));

        // Calculate entry levels
        _lEntryLevel = openS0 + (_myATR * _atrFactorL);
        _sEntryLevel = openS0 - (_myATR * _atrFactorS);

        // Time window
        bool timeWindow = _twOn == 0 ? true : TimeWindow(_myStartTrade, _myEndTrade, currentDate);

        // Exit time
        bool myExitTime = currentTime >= _exitBeg && currentTime <= _exitEnd;

        // End of day exit
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
                Quantity = _mySize,
                Reason = exitMP == 1 ? "EodLXx_ATR_BO-TF" : "EodSXx_ATR_BO-TF"
            };
        }

        // Entry conditions
        if (_entriesToday == 0 && timeWindow)
        {
            // Long entry
            if (currentPrice > _lEntryLevel &&
                PatternFast(_ptnFastLongYes, ohlcValues) && !PatternFast(_ptnFastLongNo, ohlcValues))
            {
                _currentMP = 1;
                _entriesToday++;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "LE_ATR_BO-TF"
                };
            }

            // Short entry
            if (currentPrice < _sEntryLevel &&
                PatternFast(_ptnFastShortYes, ohlcValues) && !PatternFast(_ptnFastShortNo, ohlcValues))
            {
                _currentMP = -1;
                _entriesToday++;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "SE_ATR_BO-TF"
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
