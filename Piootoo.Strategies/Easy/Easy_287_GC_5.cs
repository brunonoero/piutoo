using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_287
/// Breakout on N-sessions highest high / lowest low
/// </summary>
public class Easy_287_GC_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessBegin = 1800;
    private int _sessEnd = 1700;
    private int _mycontracts = 1;
    private int _nSess = 1;
    private int _levIncludeSess0 = 1;
    private int _adxLen = 5;
    private int _adxTH = 55;
    private int _myStartTime = 100;
    private int _myEndTime = 1700;
    private int _myStartPause = 600;
    private int _myEndPause = 800;
    private int _ptnNeutYes = 4;
    private int _ptnNeutYes2 = 55;
    private int _ptnNeutNo = 45;
    private int _ptnDirYes = 1;
    private int _ptnDirNo = 10;
    private int _skipSessL = 0;
    private int _skipSessS = 3;
    private int _maxDaysInTrade = 0;
    private int _flatTime = 1630;
    private int _myStop = 2500;
    private int _myBreakeven = 2250;
    private int _myProfit = 5500;

    // VARIABLES
    private decimal _hh = 0;
    private decimal _ll = 0;
    private bool _okL = true;
    private bool _okS = true;
    private int _daysInTrade = 0;
    private int _soW = 0;
    private bool _sessAcross2Days = false;
    private int _flatTimeFixed = 0;
    private decimal[] _adxCalcValues = new decimal[4];

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_287";
    private string _description = "Breakout on N-sessions highest high / lowest low";
    private int _currentMP = 0;
    private int _prevMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        // Initialize once variables
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
            if (parameters.TryGetValue("mycontracts", out var mc)) _mycontracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("nSess", out var ns)) _nSess = Convert.ToInt32(ns);
            if (parameters.TryGetValue("ADXLen", out var al)) _adxLen = Convert.ToInt32(al);
            if (parameters.TryGetValue("ADXTH", out var at)) _adxTH = Convert.ToInt32(at);
            if (parameters.TryGetValue("MyStartTime", out var mst)) _myStartTime = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTime", out var met)) _myEndTime = Convert.ToInt32(met);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutYes2", out var pny2)) _ptnNeutYes2 = Convert.ToInt32(pny2);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("ptnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("ptnDirNo", out var pdn)) _ptnDirNo = Convert.ToInt32(pdn);
            if (parameters.TryGetValue("SkipSessL", out var ssl)) _skipSessL = Convert.ToInt32(ssl);
            if (parameters.TryGetValue("SkipSessS", out var sss)) _skipSessS = Convert.ToInt32(sss);
            if (parameters.TryGetValue("maxdaysintrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
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

        // Calcola OHLC per sessioni
        decimal[] ohlcValues = new decimal[24];
        var isStartOfSession = OHLCMulti5(_sessBegin, _sessEnd, data, currentDate, out ohlcValues);

        if (isStartOfSession)
        {
            // ADX Calculation
            var adxVal = iADXOnArray(_adxLen, ohlcValues[5], ohlcValues[6], ohlcValues[7], 
                ohlcValues[9], ohlcValues[10], ohlcValues[11], ref _adxCalcValues) * 100;

            // N-sessions Highest High/Lowest Low calculation
            _hh = -99999999999m;
            _ll = 99999999999m;
            
            if (_nSess > 1)
            {
                for (int i = 1; i <= _nSess; i++)
                {
                    _hh = Math.Max(_hh, ohlcValues[1 + i * 4]);
                    _ll = Math.Min(_ll, ohlcValues[2 + i * 4]);
                }
            }
            else if (_nSess == 1)
            {
                _hh = ohlcValues[5]; // highd1
                _ll = ohlcValues[6]; // lowd1
            }

            // Reset permissions
            _okL = true;
            _okS = true;

            // Days in trade counter
            if (_currentMP != 0) _daysInTrade++;

            // Session of week
            if (_sessAcross2Days) _soW = (int)currentDate.DayOfWeek + 1;
            else _soW = (int)currentDate.DayOfWeek;
        }

        // Include current session in HH/LL if enabled
        if (_levIncludeSess0 == 1)
        {
            _hh = Math.Max(_hh, currentHigh);
            _ll = Math.Min(_ll, currentLow);
        }

        // Check ADX and patterns
        var adxCurrent = iADXOnArray(_adxLen, ohlcValues[5], ohlcValues[6], ohlcValues[7],
            ohlcValues[9], ohlcValues[10], ohlcValues[11], ref _adxCalcValues) * 100;

        bool inTimeWindow = TimeWindow(_myStartTime, _myEndTime, currentDate);
        bool notInPause = currentTime < _myStartPause || currentTime > _myEndPause;
        bool adxOk = adxCurrent < _adxTH;
        bool ptnNeutYesOk = PatternNeutralFast(_ptnNeutYes, ohlcValues);
        bool ptnNeutYes2Ok = PatternNeutralFast(_ptnNeutYes2, ohlcValues);
        bool ptnNeutNoOk = !PatternNeutralFast(_ptnNeutNo, ohlcValues);

        if (inTimeWindow && notInPause && adxOk && ptnNeutYesOk && ptnNeutYes2Ok && ptnNeutNoOk)
        {
            // Long entry
            if (_okL && PatternDirectionalFast(_ptnDirYes, ohlcValues) && 
                !PatternDirectionalFast(_ptnDirNo, ohlcValues) && _soW != _skipSessL)
            {
                if (currentHigh >= _hh)
                {
                    _prevMP = _currentMP;
                    _currentMP = 1;
                    if (_currentMP != _prevMP && _currentMP != 0) _daysInTrade = 1;
                    _okL = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _hh,
                        StrategyName = Name,
                        Quantity = _mycontracts,
                        Reason = "LE Breakout HH"
                    };
                }
            }

            // Short entry
            if (_okS && PatternDirectionalFast(-_ptnDirYes, ohlcValues) && 
                !PatternDirectionalFast(-_ptnDirNo, ohlcValues) && _soW != _skipSessS)
            {
                if (currentLow <= _ll)
                {
                    _prevMP = _currentMP;
                    _currentMP = -1;
                    if (_currentMP != _prevMP && _currentMP != 0) _daysInTrade = 1;
                    _okS = false;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _ll,
                        StrategyName = Name,
                        Quantity = _mycontracts,
                        Reason = "SE Breakout LL"
                    };
                }
            }
        }

        // Update MP state
        if (_currentMP == 1) _okL = false;
        if (_currentMP == -1) _okS = false;

        // Exit on max days in trade
        if (_maxDaysInTrade > 0 && _daysInTrade >= _maxDaysInTrade)
        {
            if (currentTime >= _flatTime && currentTime < _flatTimeFixed)
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
                        Quantity = _mycontracts,
                        Reason = "LX_MaxDays"
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
                        Quantity = _mycontracts,
                        Reason = "SX_MaxDays"
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
