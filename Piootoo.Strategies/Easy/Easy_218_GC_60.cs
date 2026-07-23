using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_218
/// Works using 30 minute bars (or other taking the number of daily bars into account)
/// </summary>
public class Easy_218_GC_60 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _mycontracts = 1;
    private int _testphase = 0;
    private int _mycounter = 0;
    private int _myLEBar = 16;
    private int _myLXBar = 8;
    private int _mySEBar = 8;
    private int _mySXBar = 16;
    private int _myPtnLY = 15;
    private int _myPtnSY = 70;
    private int _myPtnLN = 44;
    private int _myPtnSN = 114;
    private int _myNotLEDay = 3;
    private int _myNotSEDay = 3;
    private int _myStop = 2000;
    private int _myProfit = 4000;
    private int _entrytype = 2;
    private int _nHigh = 3;
    private int _nLow = 1;
    private int _endlong = 8;
    private int _endshort = 16;
    private int _sessionStartTimeA = 1800;
    private int _sessionEndTimeA = 1700;

    // VARIABLES
    private bool _isStartOfSession = false;
    private int _mycount = 0;
    private bool _entrywindowL = false;
    private bool _entrywindowS = false;
    private bool _okLong = false;
    private bool _okShort = false;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 60;
    private string _name = "TOP_UA_218";
    private string _description = "Works using 30 minute bars (or other taking the number of daily bars into account)";

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
            if (parameters.TryGetValue("mycontracts", out var mycontracts))
                _mycontracts = Convert.ToInt32(mycontracts);
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
        _isStartOfSession = OHLCMulti5(_sessionStartTimeA, _sessionEndTimeA, data, currentDate, out ohlcValues);
        
        // Reset all'inizio sessione
        if (_isStartOfSession)
        {
            _mycount = 0;
            _okLong = false;
            _okShort = false;
        }
        _mycount++;
        
        // Test phase
        if (_testphase == 1)
        {
            if (_mycounter == _mycount)
            {
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mycontracts,
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
                    Quantity = _mycontracts,
                    Reason = "LX_TEST"
                };
            }
        }
        
        // Trading phase
        if (_testphase == 0)
        {
            // Entry windows per entrytype > 1
            if (_entrytype > 1)
            {
                _entrywindowL = TwBars(_myLEBar, _endlong, _mycount);
                _entrywindowS = TwBars(_mySEBar, _endshort, _mycount);
                
                if (_mycount == _myLEBar && PatternFast(_myPtnLY, ohlcValues) && 
                    !PatternFast(_myPtnLN, ohlcValues) && 
                    (int)currentDate.DayOfWeek != _myNotLEDay)
                {
                    _okLong = true;
                }
                
                if (_mycount == _mySEBar && PatternFast(_myPtnSY, ohlcValues) && 
                    !PatternFast(_myPtnSN, ohlcValues) && 
                    (int)currentDate.DayOfWeek != _myNotSEDay)
                {
                    _okShort = true;
                }
                
                if (_entrywindowL && _currentMP == 1)
                {
                    _okLong = false;
                }
                if (_entrywindowS && _currentMP == -1)
                {
                    _okShort = false;
                }
            }
            
            // Entry logic basata su entrytype
            switch (_entrytype)
            {
                case 1: // Entry on time of BIAS
                    if (_mycount == _myLEBar && PatternFast(_myPtnLY, ohlcValues) && 
                        !PatternFast(_myPtnLN, ohlcValues) && 
                        (int)currentDate.DayOfWeek != _myNotLEDay && _currentMP == 0)
                    {
                        _currentMP = 1;
                        return new TradeSignal
                        {
                            Date = currentDate,
                            Type = SignalType.Buy,
                            Price = currentPrice,
                            StrategyName = Name,
                            Quantity = _mycontracts,
                            StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                            TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                            Reason = "LE_MKT"
                        };
                    }
                    if (_mycount == _mySEBar && PatternFast(_myPtnSY, ohlcValues) && 
                        !PatternFast(_myPtnSN, ohlcValues) && 
                        (int)currentDate.DayOfWeek != _myNotSEDay && _currentMP == 0)
                    {
                        _currentMP = -1;
                        return new TradeSignal
                        {
                            Date = currentDate,
                            Type = SignalType.Sell,
                            Price = currentPrice,
                            StrategyName = Name,
                            Quantity = _mycontracts,
                            StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                            TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                            Reason = "SE_MKT"
                        };
                    }
                    break;
                    
                case 2: // Breakout entry inside BIAS windows
                    if (_entrywindowL && _okLong && _currentMP == 0)
                    {
                        decimal highestH = Highest(data, _nHigh, d => d.High);
                        if (currentPrice >= highestH)
                        {
                            _currentMP = 1;
                            _okLong = false;
                            return new TradeSignal
                            {
                                Date = currentDate,
                                Type = SignalType.Buy,
                                Price = highestH,
                                StrategyName = Name,
                                Quantity = _mycontracts,
                                StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                                TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                                Reason = "LE_STP"
                            };
                        }
                    }
                    if (_entrywindowS && _okShort && _currentMP == 0)
                    {
                        decimal lowestL = Lowest(data, _nLow, d => d.Low);
                        if (currentPrice <= lowestL)
                        {
                            _currentMP = -1;
                            _okShort = false;
                            return new TradeSignal
                            {
                                Date = currentDate,
                                Type = SignalType.Sell,
                                Price = lowestL,
                                StrategyName = Name,
                                Quantity = _mycontracts,
                                StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                                TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                                Reason = "SE_STP"
                            };
                        }
                    }
                    break;
                    
                case 3: // Retracement entry inside BIAS windows
                    if (_entrywindowL && _okLong && _currentMP == 0)
                    {
                        decimal lowestL = Lowest(data, _nLow, d => d.Low);
                        if (currentPrice <= lowestL)
                        {
                            _currentMP = 1;
                            _okLong = false;
                            return new TradeSignal
                            {
                                Date = currentDate,
                                Type = SignalType.Buy,
                                Price = lowestL,
                                StrategyName = Name,
                                Quantity = _mycontracts,
                                StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                                TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                                Reason = "LE_LMT"
                            };
                        }
                    }
                    if (_entrywindowS && _okShort && _currentMP == 0)
                    {
                        decimal highestH = Highest(data, _nHigh, d => d.High);
                        if (currentPrice >= highestH)
                        {
                            _currentMP = -1;
                            _okShort = false;
                            return new TradeSignal
                            {
                                Date = currentDate,
                                Type = SignalType.Sell,
                                Price = highestH,
                                StrategyName = Name,
                                Quantity = _mycontracts,
                                StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                                TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                                Reason = "SE_LMT"
                            };
                        }
                    }
                    break;
            }
            
            // EXIT LONG
            if (_mycount == _myLXBar && _currentMP == 1)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mycontracts,
                    Reason = "LX"
                };
            }
            
            // EXIT SHORT
            if (_mycount == _mySXBar && _currentMP == -1)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mycontracts,
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

