using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_506
/// Bollinger Band cross with range filter for Gold 30 min
/// </summary>
public class Easy_506_GC_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1800;
    private int _sessionEndTimeC = 1700;
    private int _myContracts = 1;
    private int _periodL = 38;
    private int _periodS = 32;
    private int _highestPeriodL = 61;
    private int _lowestPeriodL = 61;
    private int _highestPeriodS = 48;
    private int _lowestPeriodS = 64;
    private int _myPtnLY = 152;
    private int _myPtnSY = 4;
    private int _myPtnLN = 8;
    private int _myPtnSN = 98;
    private int _myStartTime = 0;
    private int _myEndTime = 2300;
    private int _maxDaysInTrade = 5;
    private int _myStopL = 1100;
    private int _myStopS = 1100;
    private int _myProfitL = 3800;
    private int _myProfitS = 3500;
    private int _myBreakeven = 2000;
    private int _length = 20;
    private int _numDevs = 2;
    private decimal _multL = 0.8m;
    private decimal _multS = 0.8m;

    // VARIABLES
    private decimal _upperBand = 0;
    private decimal _lowerBand = 0;
    private bool _okL = false;
    private bool _okS = false;
    private decimal _rangL = 0;
    private decimal _rangS = 0;
    private int _daysInTrade = 0;
    private decimal _prevClose = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_506";
    private string _description = "Bollinger Band cross with range filter";
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
            if (parameters.TryGetValue("periodol", out var pl)) _periodL = Convert.ToInt32(pl);
            if (parameters.TryGetValue("periodos", out var ps)) _periodS = Convert.ToInt32(ps);
            if (parameters.TryGetValue("MyPtnLY", out var mply)) _myPtnLY = Convert.ToInt32(mply);
            if (parameters.TryGetValue("MyPtnSY", out var mpsy)) _myPtnSY = Convert.ToInt32(mpsy);
            if (parameters.TryGetValue("MyPtnLN", out var mpln)) _myPtnLN = Convert.ToInt32(mpln);
            if (parameters.TryGetValue("MyPtnSN", out var mpsn)) _myPtnSN = Convert.ToInt32(mpsn);
            if (parameters.TryGetValue("maxdaysintrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
            if (parameters.TryGetValue("MyStopl", out var msl)) _myStopL = Convert.ToInt32(msl);
            if (parameters.TryGetValue("MyStops", out var mss)) _myStopS = Convert.ToInt32(mss);
            if (parameters.TryGetValue("MyProfitl", out var mpl)) _myProfitL = Convert.ToInt32(mpl);
            if (parameters.TryGetValue("MyProfits", out var mps)) _myProfitS = Convert.ToInt32(mps);
            if (parameters.TryGetValue("Length", out var l)) _length = Convert.ToInt32(l);
            if (parameters.TryGetValue("NumDevs", out var nd)) _numDevs = Convert.ToInt32(nd);
            if (parameters.TryGetValue("multl", out var ml)) _multL = Convert.ToDecimal(ml);
            if (parameters.TryGetValue("mults", out var ms)) _multS = Convert.ToDecimal(ms);
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
        var prevLow = data.Length > 1 ? data[data.Length - 2].Low : currentLow;
        var prevHigh = data.Length > 1 ? data[data.Length - 2].High : currentHigh;
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        var isStartOfSession = OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);

        _prevMP = _currentMP;

        if (isStartOfSession && _currentMP != 0)
        {
            _daysInTrade++;
        }

        // Calculate ranges
        var highestL = Highest(data, _highestPeriodL, d => d.High);
        var lowestL = Lowest(data, _lowestPeriodL, d => d.Low);
        var highestS = Highest(data, _highestPeriodS, d => d.High);
        var lowestS = Lowest(data, _lowestPeriodS, d => d.Low);
        var highestPeriodL = Highest(data, _periodL, d => d.High);
        var lowestPeriodS = Lowest(data, _periodS, d => d.Low);

        _rangL = highestL - lowestL;
        _rangS = highestS - lowestS;

        // Check OK signals based on price crossing levels
        if (prevLow > highestPeriodL - _rangL * _multL && currentLow < highestPeriodL - _rangL * _multL)
            _okL = true;
        if (prevHigh < lowestPeriodS + _rangS * _multS && currentHigh > lowestPeriodS + _rangS * _multS)
            _okS = true;

        if (_currentMP == 1) _okL = false;
        if (_currentMP == -1) _okS = false;

        if (_currentMP != 0)
        {
            _okL = false;
            _okS = false;
            _rangL = 0;
            _rangS = 0;
        }

        // Calculate Bollinger Bands
        var closes = data.Skip(Math.Max(0, data.Length - _length)).Select(d => d.Close).ToArray();
        if (closes.Length >= _length)
        {
            var sma = closes.Average();
            var stdDev = (decimal)Math.Sqrt((double)closes.Select(c => (c - sma) * (c - sma)).Average());
            _upperBand = sma + (_numDevs * stdDev);
            _lowerBand = sma - (_numDevs * stdDev);
        }

        // Track MP changes
        if (_currentMP != _prevMP && _currentMP != 0)
        {
            _daysInTrade = 1;
        }

        // Max days in trade exit
        if (_maxDaysInTrade > 0 && _daysInTrade >= _maxDaysInTrade && currentTime >= 1630 && currentTime < 1700)
        {
            if (_currentMP != 0)
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
                    Reason = "Exit MaxDays"
                };
            }
        }

        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);

        if (inTimeWindow)
        {
            // Short entry
            if (_okS && _prevClose > _upperBand && currentPrice <= _upperBand &&
                PatternFast(_myPtnSY, ohlcValues) && !PatternFast(_myPtnSN, ohlcValues))
            {
                _currentMP = -1;
                _okS = false;
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

            // Long entry
            if (_okL && _prevClose < _lowerBand && currentPrice >= _lowerBand &&
                PatternFast(_myPtnLY, ohlcValues) && !PatternFast(_myPtnLN, ohlcValues))
            {
                _currentMP = 1;
                _okL = false;
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
