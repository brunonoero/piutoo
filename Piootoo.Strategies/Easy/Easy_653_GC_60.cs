using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_653
/// Trend following breakout strategy for Gold 60 min
/// </summary>
public class Easy_653_GC_60 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1800;
    private int _sessionEndTimeC = 1700;
    private int _myStartTime = 1000;
    private int _myEndTime = 1600;
    private int _dayToFilterLng = 2;
    private int _dayToFilterSht = 5;
    private int _teDayClose = 5;
    private int _targetMyProfit = 2300;
    private int _stopMyStop = 2200;
    private int _myContracts = 1;

    // VARIABLES
    private int _daysInTrade = 0;
    private int _tradeToday = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 60;
    private string _name = "TOP_UA_653";
    private string _description = "Trend following breakout strategy";
    private int _currentMP = 0;
    private int _prevMP = 0;
    private int _prevDayOfWeek = -1;

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
            if (parameters.TryGetValue("MyStartTime", out var mst)) _myStartTime = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTime", out var met)) _myEndTime = Convert.ToInt32(met);
            if (parameters.TryGetValue("DaytoFilterLng", out var dtfl)) _dayToFilterLng = Convert.ToInt32(dtfl);
            if (parameters.TryGetValue("DaytoFilterSht", out var dtfs)) _dayToFilterSht = Convert.ToInt32(dtfs);
            if (parameters.TryGetValue("TE_Day_close", out var tedc)) _teDayClose = Convert.ToInt32(tedc);
            if (parameters.TryGetValue("Target_MyProfit", out var tmp)) _targetMyProfit = Convert.ToInt32(tmp);
            if (parameters.TryGetValue("STOP_MyStop", out var sms)) _stopMyStop = Convert.ToInt32(sms);
            if (parameters.TryGetValue("mycontracts", out var mc)) _myContracts = Convert.ToInt32(mc);
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
        OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);

        var highD0 = ohlcValues[1];
        var lowD0 = ohlcValues[2];
        var highD1 = ohlcValues[5];
        var lowD1 = ohlcValues[6];

        // Time window check
        bool finestraOraria;
        if (_myStartTime > _myEndTime)
            finestraOraria = currentTime > _myStartTime || currentTime < _myEndTime;
        else
            finestraOraria = currentTime > _myStartTime && currentTime < _myEndTime;

        // Conditions
        bool condition1 = highD0 > (lowD1 + (highD1 - lowD1) * 0.5m);
        bool condition2 = lowD0 < (highD1 - (highD1 - lowD1) * 0.5m);

        // Track trades and days
        int currentDayOfWeek = (int)currentDate.DayOfWeek;
        if (currentDayOfWeek != _prevDayOfWeek && _currentMP == 0)
            _tradeToday = 0;
        if (currentDayOfWeek != _prevDayOfWeek && _currentMP != 0)
            _tradeToday = 1;

        _prevMP = _currentMP;
        if (currentDayOfWeek == _prevDayOfWeek && _currentMP != _prevMP && _currentMP != 0)
            _tradeToday++;

        // Track days in trade
        if (_currentMP != _prevMP && _currentMP != 0)
            _daysInTrade = 1;

        if (currentTime >= 1600 && data.Length > 1)
        {
            var prevTime = data[data.Length - 2].DateTime.Hour * 100 + data[data.Length - 2].DateTime.Minute;
            if (prevTime < 1600 && _currentMP != 0)
                _daysInTrade++;
        }

        // Exit based on days
        if (_currentMP != 0 && _daysInTrade >= _teDayClose && _teDayClose > 0)
        {
            if (currentTime >= 1600 && currentTime < 1700)
            {
                var exitMP = _currentMP;
                _currentMP = 0;
                _prevDayOfWeek = currentDayOfWeek;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = exitMP == 1 ? SignalType.Sell : SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "Exit DayClose"
                };
            }
        }

        // Entry signals
        if (finestraOraria)
        {
            // Long entry
            if (condition1 && _currentMP <= 0 && currentHigh >= highD0)
            {
                _currentMP = 1;
                _daysInTrade = 1;
                _tradeToday++;
                _prevDayOfWeek = currentDayOfWeek;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = highD0,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LE HighD0 Breakout"
                };
            }

            // Short entry
            if (condition2 && _currentMP >= 0 && currentLow <= lowD0)
            {
                _currentMP = -1;
                _daysInTrade = 1;
                _tradeToday++;
                _prevDayOfWeek = currentDayOfWeek;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = lowD0,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SE LowD0 Breakout"
                };
            }
        }

        _prevDayOfWeek = currentDayOfWeek;

        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = currentPrice,
            StrategyName = Name
        };
    }
}
