using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_342
/// Rif. BBBB (&quot;EB4B&quot;) - UNMIRRORED TEMPLATE (works using 5 or 15 minute bars, can be MD or ID).
/// </summary>
public class Easy_342_NQ_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _myBreakEven = 1000;
    private int _iD = 1;
    private int _myStartTrade = 1800;
    private int _myStop = 1600;
    private int _myProfit = 0;
    private int _myContracts = 1;
    private int _myStartPause = 1200;
    private int _maxEntriesPerDay = 1;
    private int _myPtnSY = 39;
    private int _myPtnLY = 26;
    private int _sessionStartTimeC = 1700;
    private int _pP = 0;
    private int _maxDaysInTrade = 7;
    private int _portATRpiu = 4;
    private decimal _portATRMeno = 9.5m;
    private int _titanExportMode = 0;
    private int _myPtnSN = 88;
    private int _myPtnLN = 134;

    // VARIABLES
    private decimal _opend0 = 0;
    private decimal _highd1 = 0;
    private decimal _lowd1 = 0;
    private int _daysInTrade = 0;
    private int _entriesToday = 0;
    private DateTime? _lastEntryDay = null;
    private int _previousMP = 0;
    private int _sessionEndTimeC = 1600;
    private int _myEndTrade = 1500;
    private int _myEndPause = 1100;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_342";
    private string _description = "Rif. BBBB (\"EB4B\") - UNMIRRORED TEMPLATE (works using 5 or 15 minute bars, can be MD or ID).";

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
            if (parameters.TryGetValue("MyBreakEven", out var mybreakeven))
                _myBreakEven = Convert.ToInt32(mybreakeven);
            if (parameters.TryGetValue("ID", out var id))
                _iD = Convert.ToInt32(id);
            if (parameters.TryGetValue("MyStartTrade", out var mystarttrade))
                _myStartTrade = Convert.ToInt32(mystarttrade);
            if (parameters.TryGetValue("MyStop", out var mystop))
                _myStop = Convert.ToInt32(mystop);
            if (parameters.TryGetValue("MyProfit", out var myprofit))
                _myProfit = Convert.ToInt32(myprofit);
            if (parameters.TryGetValue("MyContracts", out var mycontracts))
                _myContracts = Convert.ToInt32(mycontracts);
            if (parameters.TryGetValue("MyStartPause", out var mystartpause))
                _myStartPause = Convert.ToInt32(mystartpause);
            if (parameters.TryGetValue("MaxEntriesPerDay", out var maxentriesperday))
                _maxEntriesPerDay = Convert.ToInt32(maxentriesperday);
            if (parameters.TryGetValue("MyPtnSY", out var myptnsy))
                _myPtnSY = Convert.ToInt32(myptnsy);
            if (parameters.TryGetValue("MyPtnLY", out var myptnly))
                _myPtnLY = Convert.ToInt32(myptnly);
            if (parameters.TryGetValue("sessionStartTimeC", out var sessionstarttimec))
                _sessionStartTimeC = Convert.ToInt32(sessionstarttimec);
            if (parameters.TryGetValue("PP", out var pp))
                _pP = Convert.ToInt32(pp);
            if (parameters.TryGetValue("MaxDaysInTrade", out var maxdaysintrade))
                _maxDaysInTrade = Convert.ToInt32(maxdaysintrade);
            if (parameters.TryGetValue("PortATRpiu", out var portatrpiu))
                _portATRpiu = Convert.ToInt32(portatrpiu);
            if (parameters.TryGetValue("PortATRMeno", out var portatrmeno))
                _portATRMeno = Convert.ToDecimal(portatrmeno);
            if (parameters.TryGetValue("TitanExportMode", out var titanexportmode))
                _titanExportMode = Convert.ToInt32(titanexportmode);
            if (parameters.TryGetValue("MyPtnSN", out var myptnsn))
                _myPtnSN = Convert.ToInt32(myptnsn);
            if (parameters.TryGetValue("MyPtnLN", out var myptnln))
                _myPtnLN = Convert.ToInt32(myptnln);
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
        bool isStartOfSession = OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);
        _opend0 = ohlcValues[0];
        _highd1 = ohlcValues[5];
        _lowd1 = ohlcValues[6];
        
        // Reset entriesToday all'inizio giornata
        if (_lastEntryDay.HasValue && _lastEntryDay.Value.Date != currentDate.Date)
        {
            _entriesToday = 0;
        }
        
        // Gestione DaysInTrade
        if (_currentMP != _previousMP)
        {
            _daysInTrade = 0;
        }
        if (_currentMP != 0 && _lastEntryDate.HasValue && currentDate.Date > _lastEntryDate.Value.Date)
        {
            _daysInTrade++;
        }
        _previousMP = _currentMP;
        
        // Calcola ATR
        decimal atr = AvgTrueRange(data, 200);
        
        // Time Window check
        bool inTimeWindow = TimeWindow(_myStartTrade, _myEndTrade, currentDate) && 
                           (currentTime < _myStartPause || currentTime > _myEndPause);
        
        // ENTRY CONDITIONS
        if (inTimeWindow && _entriesToday < _maxEntriesPerDay && _currentMP == 0)
        {
            // BUY condition: breakout sopra opend0 + ATR
            if (PatternFast(_myPtnLY, ohlcValues) && !PatternFast(_myPtnLN, ohlcValues) && 
                currentPrice > _opend0 + _portATRpiu * atr)
            {
                _currentMP = 1;
                _entriesToday++;
                _lastEntryDate = currentDate;
                _lastEntryDay = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                    TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                    BreakEven = _myBreakEven > 0 ? (decimal?)_myBreakEven : null,
                    Reason = "LE"
                };
            }
            
            // SELLSHORT condition: breakout sotto opend0 - ATR
            if (PatternFast(_myPtnSY, ohlcValues) && !PatternFast(_myPtnSN, ohlcValues) && 
                currentPrice < _opend0 - _portATRMeno * atr)
            {
                _currentMP = -1;
                _entriesToday++;
                _lastEntryDate = currentDate;
                _lastEntryDay = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                    TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                    BreakEven = _myBreakEven > 0 ? (decimal?)_myBreakEven : null,
                    Reason = "SE"
                };
            }
        }
        
        // EXIT per MaxDays
        if (_daysInTrade >= _maxDaysInTrade && _maxDaysInTrade > 0)
        {
            if (_currentMP == 1)
            {
                _currentMP = 0;
                _daysInTrade = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "LX_MaxDays"
                };
            }
            if (_currentMP == -1)
            {
                _currentMP = 0;
                _daysInTrade = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    Reason = "SX_MaxDays"
                };
            }
        }
        
        // EXIT per ID=0 (setexitonclose)
        if (_iD == 0 && currentTime >= _sessionEndTimeC - 100)
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
                    Quantity = _myContracts,
                    Reason = "LX_ExitOnClose"
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
                    Quantity = _myContracts,
                    Reason = "SX_ExitOnClose"
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

