using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_172
/// Dax 15 min breakout Donchian Channel
/// </summary>
public class Easy_172_FDAX_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _myPtnLY = 26;
    private int _mySize = 1;
    private int _myPtnSY = 64;
    private int _myPtnSx = 34;
    private int _myPtnLN = 23;
    private int _pP = 2400;
    private int _myPtnSN = 61;
    private int _beginTime = 1600;
    private int _sessionStartTimeC = 800;
    private int _stopLoss = 3500;
    private string _myPtnLx = "-46";

    // VARIABLES
    private int _myDonchianUpper = 0;
    private bool _isStartOfSession = true;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_172";
    private string _description = "Dax 15 min breakout Donchian Channel";

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
            if (parameters.TryGetValue("MySize", out var mysize))
                _mySize = Convert.ToInt32(mysize);
            if (parameters.TryGetValue("MyPtnSY", out var myptnsy))
                _myPtnSY = Convert.ToInt32(myptnsy);
            if (parameters.TryGetValue("MyPtnSx", out var myptnsx))
                _myPtnSx = Convert.ToInt32(myptnsx);
            if (parameters.TryGetValue("MyPtnLN", out var myptnln))
                _myPtnLN = Convert.ToInt32(myptnln);
            if (parameters.TryGetValue("PP", out var pp))
                _pP = Convert.ToInt32(pp);
            if (parameters.TryGetValue("MyPtnSN", out var myptnsn))
                _myPtnSN = Convert.ToInt32(myptnsn);
            if (parameters.TryGetValue("BeginTime", out var begintime))
                _beginTime = Convert.ToInt32(begintime);
            if (parameters.TryGetValue("sessionStartTimeC", out var sessionstarttimec))
                _sessionStartTimeC = Convert.ToInt32(sessionstarttimec);
            if (parameters.TryGetValue("StopLoss", out var stoploss))
                _stopLoss = Convert.ToInt32(stoploss);
            if (parameters.TryGetValue("MyPtnLx", out var myptnlx))
                _myPtnLx = Convert.ToString(myptnlx);
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
        _isStartOfSession = OHLCMulti5(_sessionStartTimeC, 2200, data, currentDate, out ohlcValues);
        
        // DEFINITIONS - Calcola Donchian Channel
        var donchianPeriod = 20; // Default
        _myDonchianUpper = (int)HighestFC(data, donchianPeriod, d => d.High);
        var myDonchianLower = (int)LowestFC(data, donchianPeriod, d => d.Low);
        
        // CONDITIONS - Entry solo nel periodo BeginTime-EndTime
        if (currentTime >= _beginTime && currentTime < 1700) // EndTime = 1700
        {
            // BUY condition: breakout sopra Donchian Upper
            // Nota: sessionlastbar Ã¨ semplificato - dovrebbe verificare se Ã¨ l'ultima barra della sessione
            if (PatternFast(_myPtnLY, ohlcValues) && !PatternFast(_myPtnLN, ohlcValues))
            {
                if (currentPrice >= _myDonchianUpper && _currentMP == 0)
                {
                    _currentMP = 1;
                    _myCount = 1;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = (decimal)_myDonchianUpper,
                        StrategyName = Name,
                        Quantity = _mySize,
                        StopLoss = _stopLoss > 0 ? (decimal?)_stopLoss : null,
                        Reason = "LE"
                    };
                }
            }
            
            // SELLSHORT condition: breakout sotto Donchian Lower
            if (PatternFast(_myPtnSY, ohlcValues) && !PatternFast(_myPtnSN, ohlcValues))
            {
                if (currentPrice <= myDonchianLower && _currentMP == 0)
                {
                    _currentMP = -1;
                    _myCount = 1;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = myDonchianLower,
                        StrategyName = Name,
                        Quantity = _mySize,
                        StopLoss = _stopLoss > 0 ? (decimal?)_stopLoss : null,
                        Reason = "SE"
                    };
                }
            }
        }
        
        // Exit su profit target con pattern
        if (_currentMP == 1)
        {
            // Calcola openPositionProfit semplificato
            var entryPrice = _myDonchianUpper;
            var openProfit = (currentPrice - entryPrice) * _mySize;
            
            if (openProfit > _pP && _pP > 0 && PatternDirectionalFast(int.Parse(_myPtnLx), ohlcValues))
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "LX_Profit"
                };
            }
        }
        
        if (_currentMP == -1)
        {
            var entryPrice = myDonchianLower;
            var openProfit = (entryPrice - currentPrice) * _mySize;
            
            if (openProfit > _pP && _pP > 0 && PatternDirectionalFast(_myPtnSx, ohlcValues))
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "SX_Profit"
                };
            }
        }
        
        // Exit dopo max bars (280 barre = 5 giorni a 15 min)
        if (_myCount >= 280)
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
                    Reason = "LX_MaxBars"
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
                    Reason = "SX_MaxBars"
                };
            }
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

