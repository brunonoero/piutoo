using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_181
/// Bollinger Band reversal strategy for NQ 30 min
/// </summary>
public class Easy_181_NQ_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1700;
    private int _sessionEndTimeC = 1600;
    private int _myContracts = 1;
    private int _myPtnLY = 152;
    private int _myPtnSY = 100;
    private int _myPtnLN = 8;
    private int _myPtnSN = 109;
    private int _myStartTime = 1800;
    private int _myEndTime = 400;
    private int _dayToFilter = -1;
    private int _myStop = 3000;
    private int _myProfit = 6000;
    private int _length = 20;
    private int _numDevs = 2;

    // VARIABLES
    private decimal _upperBand = 0;
    private decimal _lowerBand = 0;
    private decimal _prevClose = 0;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_181";
    private string _description = "Bollinger Band reversal strategy for NQ";
    private int _currentMP = 0;

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
            if (parameters.TryGetValue("MyContracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("MyPtnLY", out var mply)) _myPtnLY = Convert.ToInt32(mply);
            if (parameters.TryGetValue("MyPtnSY", out var mpsy)) _myPtnSY = Convert.ToInt32(mpsy);
            if (parameters.TryGetValue("MyPtnLN", out var mpln)) _myPtnLN = Convert.ToInt32(mpln);
            if (parameters.TryGetValue("MyPtnSN", out var mpsn)) _myPtnSN = Convert.ToInt32(mpsn);
            if (parameters.TryGetValue("MyStartTime", out var mst)) _myStartTime = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTime", out var met)) _myEndTime = Convert.ToInt32(met);
            if (parameters.TryGetValue("DaytoFilter", out var dtf)) _dayToFilter = Convert.ToInt32(dtf);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("Length", out var l)) _length = Convert.ToInt32(l);
            if (parameters.TryGetValue("NumDevs", out var nd)) _numDevs = Convert.ToInt32(nd);
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

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);

        // Calculate Bollinger Bands
        var closes = data.Skip(Math.Max(0, data.Length - _length)).Select(d => d.Close).ToArray();
        if (closes.Length >= _length)
        {
            var sma = closes.Average();
            var stdDev = (decimal)Math.Sqrt((double)closes.Select(c => (c - sma) * (c - sma)).Average());
            _upperBand = sma + (_numDevs * stdDev);
            _lowerBand = sma - (_numDevs * stdDev);
        }

        bool dayOk = (int)currentDate.DayOfWeek != _dayToFilter;
        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);

        if (dayOk && inTimeWindow)
        {
            // Short entry on cross under upper band (mean reversion)
            if (_prevClose > _upperBand && currentPrice <= _upperBand &&
                PatternFast(_myPtnSY, ohlcValues) && !PatternFast(_myPtnSN, ohlcValues))
            {
                if (_currentMP != -1)
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
                        Reason = "SE BB Upper Cross"
                    };
                }
            }

            // Long entry on cross over lower band (mean reversion)
            if (_prevClose < _lowerBand && currentPrice >= _lowerBand &&
                PatternFast(_myPtnLY, ohlcValues) && !PatternFast(_myPtnLN, ohlcValues))
            {
                if (_currentMP != 1)
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
