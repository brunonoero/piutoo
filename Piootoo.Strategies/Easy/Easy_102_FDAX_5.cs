using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_102
/// Works using 60-minute bars but can also work down to 5 minutes bars
/// </summary>
public class Easy_102_FDAX_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _myStop = 2000;
    private int _myProfit = 3750;
    private int _myPtnLY = 4;
    private int _myPtnSY = 106;
    private int _myPtnLN = 73;
    private int _myPtnSN = 38;
    private int _sessionStartTimeC = 800;
    private int _sessionEndTimeC = 2200;
    private int _myStartTrade = 1100;
    private int _myEndTrade = 1700;
    private int _myStartPause = 1200;
    private int _myEndPause = 1100;
    private int _texit = 2145;

    // VARIABLES
    private bool _isStartOfSession = true;
    private int _mP = 0;
    private decimal _highd0 = 0;
    private decimal _lowd0 = 0;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_102";
    private string _description = "Works using 60-minute bars but can also work down to 5 minutes bars";

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
            if (parameters.TryGetValue("MyStop", out var mystop))
                _myStop = Convert.ToInt32(mystop);
            if (parameters.TryGetValue("MyPtnLY", out var myptnly))
                _myPtnLY = Convert.ToInt32(myptnly);
            if (parameters.TryGetValue("sessionStartTimeC", out var sessionstarttimec))
                _sessionStartTimeC = Convert.ToInt32(sessionstarttimec);
            if (parameters.TryGetValue("MyStartTrade", out var mystarttrade))
                _myStartTrade = Convert.ToInt32(mystarttrade);
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
        _isStartOfSession = OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);
        _highd0 = ohlcValues[1]; // High della sessione corrente
        _lowd0 = ohlcValues[2];  // Low della sessione corrente
        
        // Condizioni operative
        var inTimeWindow = currentTime > _myStartTrade && currentTime < _myEndTrade && 
                          (currentTime < _myStartPause || currentTime > _myEndPause);
        
        // ENTRY CONDITIONS
        if (inTimeWindow)
        {
            // BUY condition: breakout sopra highd0
            if (PatternFast(_myPtnLY, ohlcValues) && !PatternFast(_myPtnLN, ohlcValues))
            {
                if (currentPrice >= _highd0 && _currentMP == 0)
                {
                    _currentMP = 1;
                    _myCount = 1;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _highd0,
                        StrategyName = Name,
                        Quantity = 1,
                        StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                        TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                        Reason = "LE"
                    };
                }
            }
            
            // SELLSHORT condition: breakout sotto lowd0
            if (PatternFast(_myPtnSY, ohlcValues) && !PatternFast(_myPtnSN, ohlcValues))
            {
                if (currentPrice <= _lowd0 && _currentMP == 0)
                {
                    _currentMP = -1;
                    _myCount = 1;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _lowd0,
                        StrategyName = Name,
                        Quantity = 1,
                        StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                        TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                        Reason = "SE"
                    };
                }
            }
        }
        
        // EXIT all'ora di exit
        if (currentTime == _texit)
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
                    Quantity = 1,
                    Reason = "LX_ExitTime"
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
                    Quantity = 1,
                    Reason = "SX_ExitTime"
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

