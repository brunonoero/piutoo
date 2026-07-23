using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_32
/// NG_Trend following strategy DAX30 Fut - 2 hours breackout
/// </summary>
public class Easy_32_FDAX_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _maxBarsinTrade_Long = 100;
    private int _maxBarsinTrade_Short = 100;
    private int _mySize = 1;
    private int _mycheckTime = 1700;
    private int _mycheckGain = 1500;
    private int _myPtnLY = 1;
    private int _myPtnSY = 20;
    private int _myPtnLN = 6;
    private int _myPtnSN = 4;
    private int _myPauseDay = 5;
    private int _titanExportMode = 0;
    private int _myStartTime = 1000;
    private int _myEndTime = 1300;
    private int _myStop = 1450;
    private int _myProfit = 0;

    // VARIABLES
    private int _myExitLevelShort = 0;
    private int _myExitLevelLong = 0;
    private int _myEntryLevelLong = 0;
    private int _exitID = 0;
    private int _myEntryLevelShort = 0;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_32";
    private string _description = "NG_Trend following strategy DAX30 Fut - 2 hours breackout";

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
            if (parameters.TryGetValue("MaxBarsinTrade_Long", out var maxbarsintrade_long))
                _maxBarsinTrade_Long = Convert.ToInt32(maxbarsintrade_long);
            if (parameters.TryGetValue("MySize", out var mysize))
                _mySize = Convert.ToInt32(mysize);
            if (parameters.TryGetValue("MycheckTime", out var mychecktime))
                _mycheckTime = Convert.ToInt32(mychecktime);
            if (parameters.TryGetValue("MyPtnLY", out var myptnly))
                _myPtnLY = Convert.ToInt32(myptnly);
            if (parameters.TryGetValue("MyPauseDay", out var mypauseday))
                _myPauseDay = Convert.ToInt32(mypauseday);
            if (parameters.TryGetValue("TitanExportMode", out var titanexportmode))
                _titanExportMode = Convert.ToInt32(titanexportmode);
            if (parameters.TryGetValue("MyStartTime", out var mystarttime))
                _myStartTime = Convert.ToInt32(mystarttime);
            if (parameters.TryGetValue("MyStop", out var mystop))
                _myStop = Convert.ToInt32(mystop);
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
        var currentTime = currentDate.Hour * 100 + currentDate.Minute; // Formato HHMM
        
        // Calcola OHLC per pattern
        decimal[] ohlcValues = new decimal[24];
        bool isStartOfSession = OHLCMulti5(800, 2200, data, currentDate, out ohlcValues);
        
        // DEFINITIONS - Calcola entry/exit levels all'inizio del periodo
        if (currentTime <= _myStartTime)
        {
            _myEntryLevelLong = (int)GetDailyHigh(data, currentDate, 0);
            _myEntryLevelShort = (int)GetDailyLow(data, currentDate, 0);
            _myExitLevelLong = (int)GetDailyLow(data, currentDate, 0);
            _myExitLevelShort = (int)GetDailyHigh(data, currentDate, 0);
        }
        
        // Reset ExitID all'end time
        if (currentTime == _myEndTime)
        {
            _exitID = 0;
        }
        
        // CONDITIONS - Entry solo nel periodo specificato
        if ((int)currentDate.DayOfWeek != _myPauseDay && currentTime > _myStartTime && currentTime <= _myEndTime)
        {
            // BUY condition: breakout sopra high del giorno
            if (PtnBaseSA2(_myPtnLY, ohlcValues) && !PtnBaseSA2(_myPtnLN, ohlcValues))
            {
                if (currentPrice >= _myEntryLevelLong && _currentMP == 0)
                {
                    _currentMP = 1;
                    _myCount = 1;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _myEntryLevelLong,
                        StrategyName = Name,
                        Quantity = _mySize,
                        StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                        TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                        Reason = "LE"
                    };
                }
            }
            
            // SELLSHORT condition: breakout sotto low del giorno
            if (PtnBaseSA2(_myPtnSY, ohlcValues) && !PtnBaseSA2(_myPtnSN, ohlcValues))
            {
                if (currentPrice <= _myEntryLevelShort && _currentMP == 0)
                {
                    _currentMP = -1;
                    _myCount = 1;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _myEntryLevelShort,
                        StrategyName = Name,
                        Quantity = _mySize,
                        StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                        TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                        Reason = "SE"
                    };
                }
            }
        }
        
        // Check gain at check time
        if (currentTime == _mycheckTime)
        {
            // Se profit >= checkGain, set exitID = 1 (semplificato)
            // In realtÃ  dovremmo calcolare openpositionprofit
            _exitID = 1; // Semplificato
        }
        
        // EXIT CONDITIONS
        if (_exitID == 0)
        {
            // Exit alla fine della giornata
            if (currentTime >= 2145 && currentTime < 2200)
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
                        Quantity = _mySize,
                        Reason = "LX_EndDay"
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
                        Quantity = _mySize,
                        Reason = "SX_EndDay"
                    };
                }
            }
        }
        
        // Exit su stop levels
        if (_currentMP == 1 && currentPrice <= _myExitLevelLong)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Sell,
                Price = _myExitLevelLong,
                StrategyName = Name,
                Quantity = _mySize,
                Reason = "LX_Stop"
            };
        }
        
        if (_currentMP == -1 && currentPrice >= _myExitLevelShort)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Buy,
                Price = _myExitLevelShort,
                StrategyName = Name,
                Quantity = _mySize,
                Reason = "SX_Stop"
            };
        }
        
        // Exit dopo max bars
        if (_currentMP == 1 && _myCount >= _maxBarsinTrade_Long)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Sell,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _mySize,
                Reason = "LX_MaxBars"
            };
        }
        
        if (_currentMP == -1 && _myCount >= _maxBarsinTrade_Short)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Buy,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = _mySize,
                Reason = "SX_MaxBars"
            };
        }
        
        // Incrementa counter
        if (_currentMP != 0)
        {
            _myCount++;
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

