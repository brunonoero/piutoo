using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_956
/// Average and pattern based strategy for NQ 15 min
/// </summary>
public class Easy_956_NQ_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1700;
    private int _sessionEndTimeA = 1600;
    private int _myContracts = 1;
    private int _myStartTime = 1100;
    private int _myEndTime = 1500;
    private int _maxDaysInTrade = 8;
    private int _flatTime = 1500;
    private int _maxEntriesPerDay = 1;
    private int _myStop = 1300;
    private int _myProfit = 3000;
    private int _ptnNeutYes = 32;
    private int _ptnNeutNo = 45;
    private int _myPtnSY = 25;
    private int _myPtnSN = 61;

    // VARIABLES
    private int _daysInTrade = 0;
    private int _entriesToday = 0;
    private DateTime? _lastTradeDate = null;
    private decimal _prevClose = 0;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_956";
    private string _description = "Average and pattern based strategy for NQ";
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
            if (parameters.TryGetValue("MaxDaysInTrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
            if (parameters.TryGetValue("FlatTime", out var ft)) _flatTime = Convert.ToInt32(ft);
            if (parameters.TryGetValue("MaxEntriesPerDay", out var mepd)) _maxEntriesPerDay = Convert.ToInt32(mepd);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("MyPtnSY", out var mpsy)) _myPtnSY = Convert.ToInt32(mpsy);
            if (parameters.TryGetValue("MyPtnSN", out var mpsn)) _myPtnSN = Convert.ToInt32(mpsn);
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
        OHLCMulti5(_sessionStartTimeA, _sessionEndTimeA, data, currentDate, out ohlcValues);

        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];

        // Days in trade tracking
        if (currentTime == 1800 && _currentMP != 0)
            _daysInTrade++;
        
        _prevMP = _currentMP;
        if (_currentMP == 0 || _currentMP != _prevMP)
            _daysInTrade = 0;

        // Max days exit
        if (_daysInTrade >= _maxDaysInTrade && _maxDaysInTrade > 0)
        {
            if (currentTime >= _flatTime && currentTime <= _sessionEndTimeA && _currentMP != 0)
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
                    Reason = exitMP == 1 ? "LX_MaxDays" : "SX_MaxDays"
                };
            }
        }

        // Time window
        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);

        // Calculate averages
        var avg65 = data.Skip(Math.Max(0, data.Length - 65)).Select(d => d.Close).Average();
        var avg5 = data.Skip(Math.Max(0, data.Length - 5)).Select(d => d.Close).Average();

        // Entry conditions
        if (inTimeWindow && _entriesToday < _maxEntriesPerDay)
        {
            // Long entry
            if (_currentMP <= 0 && currentPrice > avg65 &&
                PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
            {
                if (currentHigh >= highd1 - 10)
                {
                    _currentMP = 1;
                    _entriesToday++;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = highd1 - 10,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE Above Avg65"
                    };
                }
            }

            // Short entry
            if (_currentMP >= 0 && currentPrice < avg5 &&
                _prevClose > lowd1 + 35 && currentPrice <= lowd1 + 35 &&
                PatternFast(_myPtnSY, ohlcValues) && !PatternFast(_myPtnSN, ohlcValues))
            {
                _currentMP = -1;
                _entriesToday++;
                _prevClose = currentPrice;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SE Below Avg5 Cross"
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
