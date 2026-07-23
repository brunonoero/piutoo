using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_416
/// Bollinger Band reversal strategy for Gold 30 min
/// </summary>
public class Easy_416_GC_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1800;
    private int _sessionEndTimeC = 1700;
    private int _myContracts = 1;
    private int _ptnNeutYes = 16;
    private int _ptnNeutNo = 48;
    private int _ptnDirYes = -1;
    private int _ptnDirNo = 37;
    private int _myStartTime = 1900;
    private int _myEndTime = 200;
    private int _dayToFilter = -1;
    private int _myStop = 1800;
    private int _myProfit = 0;
    private int _length = 20;
    private int _numDevs = 2;
    private int _maxDaysInTrade = 5;

    // VARIABLES
    private decimal _upperBand = 0;
    private decimal _lowerBand = 0;
    private int _daysInTrade = 0;
    private decimal _prevClose = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_416";
    private string _description = "Bollinger Band reversal strategy";
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
            if (parameters.TryGetValue("sessionStartTimeC", out var sst)) _sessionStartTimeC = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeC", out var set)) _sessionEndTimeC = Convert.ToInt32(set);
            if (parameters.TryGetValue("Mycontracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnDirNo", out var pdn)) _ptnDirNo = Convert.ToInt32(pdn);
            if (parameters.TryGetValue("MyStartTime", out var mst)) _myStartTime = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTime", out var met)) _myEndTime = Convert.ToInt32(met);
            if (parameters.TryGetValue("DaytoFilter", out var dtf)) _dayToFilter = Convert.ToInt32(dtf);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("Length", out var l)) _length = Convert.ToInt32(l);
            if (parameters.TryGetValue("NumDevs", out var nd)) _numDevs = Convert.ToInt32(nd);
            if (parameters.TryGetValue("MaxDaysInTrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
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
        var isStartOfSession = OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);

        // Calculate Bollinger Bands
        var closes = data.Skip(Math.Max(0, data.Length - _length)).Select(d => d.Close).ToArray();
        if (closes.Length < _length)
        {
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Hold,
                Price = currentPrice,
                StrategyName = Name,
                Reason = "Dati insufficienti per Bollinger"
            };
        }

        var sma = closes.Average();
        var stdDev = (decimal)Math.Sqrt((double)closes.Select(c => (c - sma) * (c - sma)).Average());
        _upperBand = sma + (_numDevs * stdDev);
        _lowerBand = sma - (_numDevs * stdDev);

        // Track days in trade
        if (isStartOfSession && _currentMP != 0)
        {
            _daysInTrade++;
        }

        _prevMP = _currentMP;
        if (_currentMP != _prevMP && _currentMP != 0)
        {
            _daysInTrade = 1;
        }

        // Max days in trade exit
        if (_daysInTrade >= _maxDaysInTrade && _maxDaysInTrade > 0)
        {
            if (currentTime >= 1630 && currentTime <= 1700)
            {
                if (_currentMP == 1)
                {
                    _currentMP = 0;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = currentPrice,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LX MaxDays"
                    };
                }
                if (_currentMP == -1)
                {
                    _currentMP = 0;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = currentPrice,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SX MaxDays"
                    };
                }
            }
        }

        // Entry conditions
        bool dayOk = (int)currentDate.DayOfWeek != _dayToFilter;
        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);
        bool ptnNeutOk = PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues);

        if (dayOk && inTimeWindow && ptnNeutOk)
        {
            // Short on cross under upper band (mean reversion)
            if (_prevClose > _upperBand && currentPrice <= _upperBand &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) && !PatternDirectionalFast(-_ptnDirNo, ohlcValues))
            {
                if (_currentMP != -1)
                {
                    _currentMP = -1;
                    _daysInTrade = 1;
                    _prevClose = currentPrice;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = currentPrice,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE BB Upper Cross"
                    };
                }
            }

            // Long on cross over lower band (mean reversion)
            if (_prevClose < _lowerBand && currentPrice >= _lowerBand &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) && !PatternDirectionalFast(_ptnDirNo, ohlcValues))
            {
                if (_currentMP != 1)
                {
                    _currentMP = 1;
                    _daysInTrade = 1;
                    _prevClose = currentPrice;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = currentPrice,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE BB Lower Cross"
                    };
                }
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
