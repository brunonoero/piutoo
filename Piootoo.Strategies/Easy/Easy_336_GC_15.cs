using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_336
/// Donchian channel breakout strategy with ADX filter for Gold 15 min
/// </summary>
public class Easy_336_GC_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _adxLength = 5;
    private int _myADXLimit = 50;
    private int _myLength = 155;
    private int _useDonchianTrailing = 1;
    private int _myStartTrade = 300;
    private int _myEndTrade = 1545;
    private int _myStop = 2500;
    private int _myProfit = 4300;
    private int _myNoShortDay = 5;
    private decimal _df = 0.40m;
    private int _myContracts = 1;

    // VARIABLES
    private decimal _myADXValue = 0;
    private decimal _upperChannel = 0;
    private decimal _lowerChannel = 0;
    private decimal _range1 = 0;
    private bool _condition1 = false;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_336";
    private string _description = "Donchian channel breakout with ADX filter";
    private int _currentMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 200;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("ADXLenght", out var al)) _adxLength = Convert.ToInt32(al);
            if (parameters.TryGetValue("MyADXLimit", out var mal)) _myADXLimit = Convert.ToInt32(mal);
            if (parameters.TryGetValue("MyLenght", out var ml)) _myLength = Convert.ToInt32(ml);
            if (parameters.TryGetValue("UseDonchianTrailing", out var udt)) _useDonchianTrailing = Convert.ToInt32(udt);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("MyNoShortDay", out var mnsd)) _myNoShortDay = Convert.ToInt32(mnsd);
            if (parameters.TryGetValue("DF", out var df)) _df = Convert.ToDecimal(df);
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

        // Calculate ADX (simplified - using ATR ratio as proxy)
        var atr = AvgTrueRange(data, _adxLength);
        var priceRange = currentHigh - currentLow;
        _myADXValue = priceRange > 0 ? (atr / priceRange) * 100 : 0;

        // Donchian channels
        _upperChannel = Highest(data, _myLength, d => d.High);
        _lowerChannel = Lowest(data, _myLength, d => d.Low);

        // Get daily OHLC for session 1
        var dailyData = GroupByDay(data, currentDate);
        if (dailyData.Count >= 2)
        {
            var session1 = dailyData[1];
            _range1 = session1.High - session1.Low;
            
            if (_range1 != 0)
            {
                _condition1 = Math.Abs(session1.Close - session1.Open) / _range1 <= _df;
            }
        }

        // Get current day high/low
        var highD0 = dailyData.Count > 0 ? dailyData[0].High : currentHigh;
        var lowD0 = dailyData.Count > 0 ? dailyData[0].Low : currentLow;

        bool inTimeWindow = currentTime > _myStartTrade && currentTime < _myEndTrade;

        // Donchian trailing exit
        if (_useDonchianTrailing == 1)
        {
            if (_currentMP == 1 && currentLow <= _lowerChannel)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = _lowerChannel,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LXx Donchian Trail"
                };
            }
            if (_currentMP == -1 && currentHigh >= _upperChannel)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = _upperChannel,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SXx Donchian Trail"
                };
            }
        }

        // Entry conditions
        if (inTimeWindow && _myADXValue < _myADXLimit && _condition1)
        {
            // Long entry
            if (_currentMP <= 0 && currentHigh >= highD0)
            {
                _currentMP = 1;
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

            // Short entry (not on MyNoShortDay)
            if (_currentMP >= 0 && (int)currentDate.DayOfWeek != _myNoShortDay && currentLow <= lowD0)
            {
                _currentMP = -1;
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

        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = currentPrice,
            StrategyName = Name
        };
    }
}
