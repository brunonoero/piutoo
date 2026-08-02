using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

// CLOSE_DEPENDENT: uscite su pnt2/pnt3 (high/low 19:00 ± dist) rivalutate a ogni barra e gate
// su data2 (15m) per oklong1/okshort1. Non mappabile su un motore Unger senza perdere la
// semantica delle uscite runtime.

/// <summary>
/// TOP_UA_661 — reversal su incrocio open di sessione, GC 30 minuti + data2 15m.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_661_GC_30____15__7.txt</c>.
/// <b>Non eseguibile.</b> I livelli <c>dist</c> sono stop assoluti ricalcolati alle 19:00 e
/// valutati a ogni barra; non sono un'uscita dichiarabile al fill. Resta su
/// <see cref="StatelessEasyStrategyBase"/> ed è esclusa dal catalogo operativo.</para>
/// </summary>
public class Easy_661_GC_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1800;
    private int _sessionEndTimeC = 1700;
    private int _myContracts = 1;
    private int _ptnNeutYes = 55;
    private int _ptnNeutNo = 27;
    private int _ptnDirYes = 52;
    private int _ptnDirNo = 35;
    private int _myStartTime = 1830;
    private int _myEndTime = 1630;
    private int _dist = 30;
    private int _myStop = 0;
    private int _myProfit = 50;
    private int _myBreakeven = 30;
    private int _length = 20;
    private int _numDevs = 2;
    private int _maxDaysInTrade = 2;

    // VARIABLES
    private int _daysInTrade = 0;
    private bool _okLong = false;
    private bool _okLong2 = false;
    private bool _okLong1 = false;
    private bool _okShort = false;
    private bool _okShort2 = false;
    private bool _okShort1 = false;
    private decimal _pnt = 0;
    private decimal _pnt2 = 0;
    private decimal _pnt3 = 0;
    private bool _okLongShort = true;
    private bool _maxTradeLong = true;
    private bool _maxTradeShort = true;
    private decimal _prevClose = 0;
    private decimal _prevOpen = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_661";
    private string _description = "Reversal strategy based on session open cross";
    private int _currentMP = 0;
    private int _prevMP = 0;

    public override bool IsPositionCloseDependent => true;

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
            if (parameters.TryGetValue("dist", out var d)) _dist = Convert.ToInt32(d);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("maxdaysintrade", out var mdit)) _maxDaysInTrade = Convert.ToInt32(mdit);
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
        var currentOpen = data.Last().Open;
        var currentHigh = data.Last().High;
        var currentLow = data.Last().Low;
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        var isStartOfSession = OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);

        _prevMP = _currentMP;

        if (isStartOfSession && _currentMP != 0)
        {
            _daysInTrade++;
        }

        // Track last bar of session for okLong1/okShort1
        // Simplified: use previous bar close/open comparison
        if (data.Length > 1)
        {
            var prevBar = data[data.Length - 2];
            if (prevBar.Close > prevBar.Open)
            {
                _okLong1 = true;
                _okShort1 = false;
            }
            else if (prevBar.Close < prevBar.Open)
            {
                _okLong1 = false;
                _okShort1 = true;
            }
        }

        // Max trade per session reset
        if (_currentMP == 1) _maxTradeLong = false;
        if (currentTime == 1900) _maxTradeLong = true;
        if (_currentMP == -1) _maxTradeShort = false;
        if (currentTime == 1900) _maxTradeShort = true;

        // Set session open at 1900
        if (currentTime == 1900) _pnt = currentOpen;
        if (isStartOfSession) _pnt = 0;

        // Track price crossing below/above session open
        if (currentTime > 1900 && currentPrice < _pnt && _okLong1)
            _okLong = true;
        if (_okLong && currentPrice > _pnt)
            _okLong2 = true;

        if (currentTime > 1900 && currentPrice > _pnt && _okShort1)
            _okShort = true;
        if (_okShort && currentPrice < _pnt)
            _okShort2 = true;

        if (isStartOfSession)
        {
            _okLong = false;
            _okLong2 = false;
            _okShort = false;
            _okShort2 = false;
        }

        _okLongShort = _pnt > 0;

        // Set exit levels at 1900
        if (currentTime == 1900 && _currentMP != 1) _pnt3 = currentLow - _dist;
        if (currentTime == 1900 && _currentMP != -1) _pnt2 = currentHigh + _dist;
        if (isStartOfSession && _currentMP != 1) _pnt3 = 0;
        if (isStartOfSession && _currentMP != -1) _pnt2 = 0;

        // Exit if price exceeds exit levels
        if (_pnt2 > 0 && currentPrice > _pnt2 && _currentMP == -1)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Buy,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "SX Exit Level"
            };
        }
        if (_pnt3 > 0 && currentPrice < _pnt3 && _currentMP == 1)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Sell,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _myContracts,
                Reason = "LX Exit Level"
            };
        }

        // Max days exit
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
                    Reason = "MaxDays Exit"
                };
            }
        }

        if (_currentMP != _prevMP && _currentMP != 0) _daysInTrade = 1;

        // Entry conditions
        if (TimeWindow(_myStartTime, _myEndTime, currentDate) &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
        {
            // Short entry
            if (_okLongShort && _okShort2 && _maxTradeShort &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) && !PatternDirectionalFast(-_ptnDirNo, ohlcValues))
            {
                _currentMP = -1;
                _okShort2 = false;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SE Open Cross Reversal"
                };
            }

            // Long entry
            if (_okLongShort && _okLong2 && _maxTradeLong &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) && !PatternDirectionalFast(_ptnDirNo, ohlcValues))
            {
                _currentMP = 1;
                _okLong2 = false;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LE Open Cross Reversal"
                };
            }
        }

        _prevClose = currentPrice;
        _prevOpen = currentOpen;

        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = currentPrice,
            StrategyName = Name
        };
    }
}
