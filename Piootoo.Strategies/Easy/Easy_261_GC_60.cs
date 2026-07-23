using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_261
/// Works using 60-minute bars
/// </summary>
public class Easy_261_GC_60 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _myPtnLY = 5;
    private int _myPtnSY = 25;
    private int _myPtnLN = 1;
    private int _myPtnSN = 4;
    private int _myLEbar = 23;
    private int _myLXbar = 5;
    private int _mySEbar = 9;
    private int _mySXbar = 14;
    private int _myNotLEDay = 4;
    private int _myNotSEDay = 4;
    private int _myStop = 1500;
    private int _myProfit = 0;
    private int _mycounter = 0;
    private int _testphase = 0;
    private int _sessionStartTimeA = 800;
    private int _sessionEndTimeA = 2200;

    // VARIABLES
    private bool _wasSessionLastBar = false;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 60;
    private string _name = "TOP_UA_261";
    private string _description = "Works using 60-minute bars";

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
            if (parameters.TryGetValue("TitanExportMode", out var titanexportmode))
                _titanExportMode = Convert.ToInt32(titanexportmode);
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
        bool isStartOfSession = OHLCMulti5(_sessionStartTimeA, _sessionEndTimeA, data, currentDate, out ohlcValues);
        
        // Gestione sessionlastbar
        bool isSessionLastBar = IsSessionLastBar(data, currentDate, _sessionStartTimeA, _sessionEndTimeA);
        if (_wasSessionLastBar)
        {
            _myCount = 0;
        }
        _wasSessionLastBar = isSessionLastBar;
        _myCount++;
        
        // Test phase
        if (_testphase == 1)
        {
            if (_mycounter == _myCount)
            {
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = 1,
                    Reason = "LE_TEST"
                };
            }
            if (_currentMP == 1)
            {
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = 1,
                    Reason = "LX_TEST"
                };
            }
        }
        
        // Trading phase
        if (_testphase == 0)
        {
            // BUY condition
            if (_myCount == _myLEbar && PtnBaseSA2(_myPtnLY, ohlcValues) && 
                !PtnBaseSA2(_myPtnLN, ohlcValues) && 
                (int)currentDate.DayOfWeek != _myNotLEDay && _currentMP == 0)
            {
                _currentMP = 1;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = 1,
                    StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                    TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                    Reason = "LE"
                };
            }
            
            // EXIT LONG
            if (_myCount == _myLXbar && _currentMP == 1)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = 1,
                    Reason = "LX"
                };
            }
            
            // SELLSHORT condition
            if (_myCount == _mySEbar && PtnBaseSA2(_myPtnSY, ohlcValues) && 
                !PtnBaseSA2(_myPtnSN, ohlcValues) && 
                (int)currentDate.DayOfWeek != _myNotSEDay && _currentMP == 0)
            {
                _currentMP = -1;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = 1,
                    StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                    TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                    Reason = "SE"
                };
            }
            
            // EXIT SHORT
            if (_myCount == _mySXbar && _currentMP == -1)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = 1,
                    Reason = "SX"
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

