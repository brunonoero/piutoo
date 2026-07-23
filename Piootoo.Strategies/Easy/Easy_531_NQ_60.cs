using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_531
/// works on 60 minutes bars fatto per il gold partendo dal codice jumper
/// </summary>
public class Easy_531_NQ_60 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _maxdaysIntrade = 10;
    private int _sessionStartTimeC = 1700;
    private int _sessionEndTimeC = 1600;
    private int _myTime = 800;
    private int _titanExportMode = 0;
    private int _mycontracts = 1;
    private int _myPtnLY = 142;
    private int _myPtnSY = 64;
    private int _myPtnLN = 92;
    private int _myPtnSN = 87;
    private int _myNotLEDay = -1;
    private int _myNotSEDay = -1;
    private int _myStop = 2500;
    private int _myProfit = 6000;
    private int _noMonthLE = -1;
    private int _noMonthSE = -1;

    // VARIABLES
    private int _markPos = 0;
    private bool _isStartOfSession = true;
    private int _daysInTr = 0;
    private int _previousMP = 0;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 60;
    private string _name = "TOP_UA_531";
    private string _description = "works on 60 minutes bars fatto per il gold partendo dal codice jumper";

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100; // TODO: Calcolare in base alla strategia

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym))
                _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf))
                _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("MaxdaysIntrade", out var maxdaysintrade))
                _maxdaysIntrade = Convert.ToInt32(maxdaysintrade);
            if (parameters.TryGetValue("sessionStartTimeC", out var sessionstarttimec))
                _sessionStartTimeC = Convert.ToInt32(sessionstarttimec);
            if (parameters.TryGetValue("MyTime", out var mytime))
                _myTime = Convert.ToInt32(mytime);
            if (parameters.TryGetValue("TitanExportMode", out var titanexportmode))
                _titanExportMode = Convert.ToInt32(titanexportmode);
            if (parameters.TryGetValue("Mycontracts", out var mycontracts))
                _mycontracts = Convert.ToInt32(mycontracts);
            if (parameters.TryGetValue("MyPtnLY", out var myptnly))
                _myPtnLY = Convert.ToInt32(myptnly);
        }
    }

    // Stato per tracciare la posizione corrente (MP = marketposition)
    private int _currentMP = 0; // 0 = nessuna posizione, +1 = long, -1 = short
    private int _myCount = 0;
    private DateTime? _lastEntryDate = null;

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
        
        // Calcola OHLC per pattern
        decimal[] ohlcValues = new decimal[24];
        _isStartOfSession = OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);
        
        // Gestione DaysInTrade
        _markPos = _currentMP;
        if (_markPos != _previousMP)
        {
            _daysInTr = 0;
        }
        if (_markPos != 0 && _lastEntryDate.HasValue && currentDate.Date > _lastEntryDate.Value.Date)
        {
            _daysInTr++;
        }
        _previousMP = _markPos;
        
        // ENTRY CONDITIONS all'ora specificata
        if (currentTime == _myTime)
        {
            // BUY condition
            if (PatternFast(_myPtnLY, ohlcValues) && !PatternFast(_myPtnLN, ohlcValues) && 
                (int)currentDate.DayOfWeek != _myNotLEDay && 
                (int)currentDate.Month != _noMonthLE && _currentMP == 0)
            {
                _currentMP = 1;
                _lastEntryDate = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = data.Last().High, // h stop
                    StrategyName = Name,
                    Quantity = _mycontracts,
                    StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                    TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                    Reason = "LE"
                };
            }
            
            // SELLSHORT condition
            if (PatternFast(_myPtnSY, ohlcValues) && !PatternFast(_myPtnSN, ohlcValues) && 
                (int)currentDate.DayOfWeek != _myNotSEDay && 
                (int)currentDate.Month != _noMonthSE && _currentMP == 0)
            {
                _currentMP = -1;
                _lastEntryDate = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = data.Last().Low, // l stop
                    StrategyName = Name,
                    Quantity = _mycontracts,
                    StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                    TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                    Reason = "SE"
                };
            }
        }
        
        // EXIT per MaxDays
        if (_maxdaysIntrade > 0 && _daysInTr >= _maxdaysIntrade)
        {
            int exitTime = _sessionEndTimeC - 200;
            if (currentTime == exitTime)
            {
                if (_currentMP == 1)
                {
                    _currentMP = 0;
                    _daysInTr = 0;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = currentPrice,
                        StrategyName = Name,
                        Quantity = _mycontracts,
                        Reason = "LX_EndDay"
                    };
                }
                if (_currentMP == -1)
                {
                    _currentMP = 0;
                    _daysInTr = 0;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = currentPrice,
                        StrategyName = Name,
                        Quantity = _mycontracts,
                        Reason = "SX_EndDay"
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

