using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_851
/// Moving average filter strategy with ADX for Gold 5 min
/// </summary>
public class Easy_851_GC_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1800;
    private int _sessionEndTimeC = 1700;
    private int _ptnNeutYes = 4;
    private int _ptnNeutNo = 30;
    private int _ptnDirYes = 52;
    private int _ptnDirNo = -28;
    private int _myStartTrade = 2300;
    private int _myEndTrade = 1000;
    private int _myStartPause = 1200;
    private int _myEndPause = 1100;
    private int _myStop = 2000;
    private int _myProfit = 0;
    private int _lunghezza = 200;
    private int _myBreakeven = 3000;
    private int _numBarre = 3;
    private int _adxLen = 5;
    private int _adxTH = 60;
    private int _myContracts = 1;

    // VARIABLES
    private decimal _avg = 0;
    private int _countSessioneIntrade = 0;
    private decimal _guadagno = 0;
    private decimal _prevGuadagno = 0;
    private bool _vendita = false;
    private decimal _adxVal = 0;
    private decimal[] _adxCalcValues = new decimal[4];

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_851";
    private string _description = "Moving average filter strategy with ADX";
    private int _currentMP = 0;
    private int _prevMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 250;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("sessionStartTimeC", out var sst)) _sessionStartTimeC = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeC", out var set)) _sessionEndTimeC = Convert.ToInt32(set);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnDirNo", out var pdn)) _ptnDirNo = Convert.ToInt32(pdn);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("Lunghezza", out var l)) _lunghezza = Convert.ToInt32(l);
            if (parameters.TryGetValue("Numbarre", out var nb)) _numBarre = Convert.ToInt32(nb);
            if (parameters.TryGetValue("ADXLen", out var al)) _adxLen = Convert.ToInt32(al);
            if (parameters.TryGetValue("ADXTH", out var at)) _adxTH = Convert.ToInt32(at);
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
        var isStartOfSession = OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);

        var highd0 = ohlcValues[1];
        var lowd0 = ohlcValues[2];
        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];

        // ADX calculation
        if (isStartOfSession)
        {
            _adxVal = iADXOnArray(_adxLen, ohlcValues[5], ohlcValues[6], ohlcValues[7],
                ohlcValues[9], ohlcValues[10], ohlcValues[11], ref _adxCalcValues) * 100;
        }

        // Calculate average
        var closes = data.Skip(Math.Max(0, data.Length - _lunghezza)).Select(d => d.Close).ToArray();
        _avg = closes.Length > 0 ? closes.Average() : currentPrice;

        _prevMP = _currentMP;

        // Track sessions in trade
        if (isStartOfSession && _currentMP != 0)
            _countSessioneIntrade++;
        if (_currentMP != _prevMP && _currentMP != 0)
            _countSessioneIntrade = 1;
        if (_currentMP == 0)
            _countSessioneIntrade = 0;

        // Profit tracking
        if (currentTime == 1630 && _currentMP != 0)
        {
            _prevGuadagno = _guadagno;
            // Simplified profit tracking - in real implementation should track actual P&L
            _guadagno = 0; // Placeholder
        }
        _vendita = _guadagno < _prevGuadagno;

        // Exit on first session if losing
        if (currentTime == 1645 && _countSessioneIntrade == 1 && _guadagno < 0)
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
                    Reason = "Exit FirstSession Loss"
                };
            }
        }

        // Exit if profit decreasing
        if (_vendita && _countSessioneIntrade > 1 && _currentMP != 0)
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
                Reason = exitMP == 1 ? "LongDaGuadagno" : "ShortDaGuadagno"
            };
        }

        // Max sessions exit
        if (currentTime == 1645 && _numBarre > 0 && _countSessioneIntrade == _numBarre && _currentMP != 0)
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
                Reason = exitMP == 1 ? "LongMaxGG" : "ShortMaxGG"
            };
        }

        // Entry conditions
        bool inTimeWindow = TimeWindow(_myStartTrade, _myEndTrade, currentDate);
        bool notInPause = currentTime < _myStartPause || currentTime > _myEndPause;
        bool adxOk = _adxVal < _adxTH;

        if (inTimeWindow && notInPause && adxOk &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
        {
            // Long entry
            if (_currentMP == 0 && currentPrice >= _avg &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) && !PatternDirectionalFast(_ptnDirNo, ohlcValues))
            {
                if (currentHigh >= highd0)
                {
                    _currentMP = 1;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = highd0,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE Above Avg Breakout"
                    };
                }
            }

            // Short entry
            if (_currentMP == 0 && currentPrice < _avg &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) && !PatternDirectionalFast(-_ptnDirNo, ohlcValues))
            {
                if (currentLow <= lowd0)
                {
                    _currentMP = -1;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = lowd0,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE Below Avg Breakout"
                    };
                }
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
