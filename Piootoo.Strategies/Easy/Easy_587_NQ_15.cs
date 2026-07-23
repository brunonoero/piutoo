using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_587
/// ATR Band Breakout with ADX filter for NQ 15 min
/// </summary>
public class Easy_587_NQ_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessBegin = 1700;
    private int _sessEnd = 1600;
    private int _myContracts = 1;
    private int _myStartTime = 800;
    private int _myEndTime = 1300;
    private int _myStartPause = 1200;
    private int _myEndPause = 1100;
    private int _adxLen = 5;
    private int _adxTH = 50;
    private int _adxTL = 20;
    private int _avgMoltip = 17;
    private int _ptnNeutYes = 32;
    private int _ptnNeutNo = 8;
    private int _ptnDirYes = 49;
    private int _ptnDirNo = 16;
    private int _maxDaysInTrade = 2;
    private int _flatTime = 1530;
    private int _myStop = 2500;
    private int _myBreakeven = 2000;
    private int _myProfit = 0;

    // VARIABLES
    private decimal _hh = 0;
    private decimal _ll = 0;
    private bool _okL = true;
    private bool _okS = true;
    private int _daysInTrade = 0;
    private int _soW = 0;
    private bool _sessAcross2Days = false;
    private int _flatTimeFixed = 0;
    private decimal _adxVal = 0;
    private decimal[] _adxCalcValues = new decimal[4];

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_587";
    private string _description = "ATR Band Breakout with ADX filter";
    private int _currentMP = 0;
    private int _prevMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        _sessAcross2Days = _sessBegin > _sessEnd;
        if (_sessAcross2Days) _flatTimeFixed = _sessBegin;
        else _flatTimeFixed = _sessEnd;
        if (_sessBegin == 0 && _sessEnd == 0) _flatTimeFixed = 2359;

        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("SessBegin", out var sb)) _sessBegin = Convert.ToInt32(sb);
            if (parameters.TryGetValue("SessEnd", out var se)) _sessEnd = Convert.ToInt32(se);
            if (parameters.TryGetValue("mycontracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("MyStartTime", out var mst)) _myStartTime = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTime", out var met)) _myEndTime = Convert.ToInt32(met);
            if (parameters.TryGetValue("ADXLen", out var al)) _adxLen = Convert.ToInt32(al);
            if (parameters.TryGetValue("ADXTH", out var at)) _adxTH = Convert.ToInt32(at);
            if (parameters.TryGetValue("ADXTL", out var atl)) _adxTL = Convert.ToInt32(atl);
            if (parameters.TryGetValue("AVGMoltip", out var am)) _avgMoltip = Convert.ToInt32(am);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("ptnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("ptnDirNo", out var pdn)) _ptnDirNo = Convert.ToInt32(pdn);
            if (parameters.TryGetValue("maxdaysintrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
            if (parameters.TryGetValue("flatTime", out var ft)) _flatTime = Convert.ToInt32(ft);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
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
        var isStartOfSession = OHLCMulti5(_sessBegin, _sessEnd, data, currentDate, out ohlcValues);

        if (isStartOfSession)
        {
            // ADX calculation
            if (_adxLen > 1)
                _adxVal = iADXOnArray(_adxLen, ohlcValues[5], ohlcValues[6], ohlcValues[7],
                    ohlcValues[9], ohlcValues[10], ohlcValues[11], ref _adxCalcValues) * 100;
            else
                _adxVal = 50;

            _okL = true;
            _okS = true;

            if (_currentMP != 0) _daysInTrade++;

            if (_sessAcross2Days) _soW = (int)currentDate.DayOfWeek + 1;
            else _soW = (int)currentDate.DayOfWeek;
        }

        // Calculate ATR bands
        var avgClose = data.Skip(Math.Max(0, data.Length - 5)).Select(d => d.Close).Average();
        var atr = AvgTrueRange(data, 5);
        _hh = avgClose + (_avgMoltip / 10m) * atr;
        _ll = avgClose - (_avgMoltip / 10m) * atr;

        _prevMP = _currentMP;
        if (_currentMP != _prevMP && _currentMP != 0) _daysInTrade = 1;
        if (_currentMP == 1) _okL = false;
        if (_currentMP == -1) _okS = false;

        // Max days exit
        if (_maxDaysInTrade > 0 && _daysInTrade >= _maxDaysInTrade)
        {
            if (currentTime >= _flatTime && currentTime < _flatTimeFixed && _currentMP != 0)
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

        // Entry conditions
        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);
        bool notInPause = currentTime < _myStartPause || currentTime > _myEndPause;
        bool adxOk = _adxVal < _adxTH && _adxVal > _adxTL;

        if (inTimeWindow && notInPause && adxOk &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
        {
            // Long entry
            if (_okL && PatternDirectionalFast(_ptnDirYes, ohlcValues) && !PatternDirectionalFast(_ptnDirNo, ohlcValues))
            {
                if (currentHigh >= _hh)
                {
                    _currentMP = 1;
                    _okL = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _hh,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE ATR Band Breakout"
                    };
                }
            }

            // Short entry
            if (_okS && PatternDirectionalFast(-_ptnDirYes, ohlcValues) && !PatternDirectionalFast(-_ptnDirNo, ohlcValues))
            {
                if (currentLow <= _ll)
                {
                    _currentMP = -1;
                    _okS = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _ll,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE ATR Band Breakout"
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
