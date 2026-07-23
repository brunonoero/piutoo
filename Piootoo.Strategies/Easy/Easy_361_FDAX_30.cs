using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_361
/// MultiCharts/TradeStation code generator by Unger Academy. All rights reserved.
/// </summary>
public class Easy_361_FDAX_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _myContracts = 1;
    private int _titanExportMode = 0;
    private int _sessionStartTimeA = 800;
    private int _sessionEndTimeA = 2200;
    private int _myStartTime = 1400;
    private int _myEndTime = 2100;
    private int _myStartPause = 1200;
    private int _myEndPause = 1200;
    private int _ptnNeutYes = 3;
    private int _ptnNeutNo = 56;
    private int _ptnDirYes = 52;
    private int _ptnDirNo = 8;
    private int _maxEntriesPerDay = 2;
    private int _adx_TH = 90;
    private int _nBars = 20;
    private int _maxDaysInTrade = 3;
    private int _flatTime = 2130;
    private int _myStop = 1800;
    private int _myBreakEven = 0;
    private int _myProfit = 4800;

    // VARIABLES
    private int _aDXval = 0;
    private bool _isStartOfSession = false;
    private decimal _upperchannel = 0;
    private decimal _lowerchannel = 0;
    private int _daysInTrade = 0;
    private decimal[] _adxCalcValues = new decimal[4];
    private int _entriesToday = 0;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_361";
    private string _description = "MultiCharts/TradeStation code generator by Unger Academy. All rights reserved.";

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
            if (parameters.TryGetValue("MyContracts", out var mycontracts))
                _myContracts = Convert.ToInt32(mycontracts);
            if (parameters.TryGetValue("TitanExportMode", out var titanexportmode))
                _titanExportMode = Convert.ToInt32(titanexportmode);
            if (parameters.TryGetValue("SessionStartTimeA", out var sessionstarttimea))
                _sessionStartTimeA = Convert.ToInt32(sessionstarttimea);
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
        _isStartOfSession = OHLCMulti5(_sessionStartTimeA, _sessionEndTimeA, data, currentDate, out ohlcValues);
        
        // Calcola ADX all'inizio sessione
        if (_isStartOfSession)
        {
            var highd1 = ohlcValues[5];
            var lowd1 = ohlcValues[6];
            var closed1 = ohlcValues[7];
            var highd2 = ohlcValues[9];
            var lowd2 = ohlcValues[10];
            var closed2 = ohlcValues[11];
            
            _aDXval = (int)(iADXOnArray(5, highd1, lowd1, closed1, highd2, lowd2, closed2, ref _adxCalcValues) * 100);
            
            if (_currentMP != 0)
            {
                _daysInTrade++;
            }
        }
        
        // Calcola channel
        _upperchannel = HighestFC(data, _nBars, d => d.High);
        _lowerchannel = LowestFC(data, _nBars, d => d.Low);
        
        // Reset entriesToday all'inizio giornata
        if (_lastEntryDate.HasValue && _lastEntryDate.Value.Date != currentDate.Date)
        {
            _entriesToday = 0;
        }
        
        // Reset DaysInTrade quando MP cambia
        if (_lastEntryDate.HasValue && _lastEntryDate.Value.Date != currentDate.Date && _currentMP != 0)
        {
            _daysInTrade = 1;
        }
        
        // Condizioni operative
        var inTimeWindow = currentTime >= _myStartTime && currentTime <= _myEndTime && 
                          (currentTime < _myStartPause || currentTime > _myEndPause);
        
        // ENTRY CONDITIONS
        if (inTimeWindow && PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues) &&
            _entriesToday < _maxEntriesPerDay && _aDXval < _adx_TH)
        {
            // BUY condition: breakout sopra upper channel
            if (PatternDirectionalFast(_ptnDirYes, ohlcValues) && !PatternDirectionalFast(_ptnDirNo, ohlcValues))
            {
                if (currentPrice >= _upperchannel && _currentMP == 0)
                {
                    _currentMP = 1;
                    _daysInTrade = 1;
                    _entriesToday++;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = _upperchannel,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                        TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                        BreakEven = _myBreakEven > 0 ? (decimal?)_myBreakEven : null,
                        Reason = "LE"
                    };
                }
            }
            
            // SELLSHORT condition: breakout sotto lower channel
            if (PatternDirectionalFast(-_ptnDirYes, ohlcValues) && !PatternDirectionalFast(-_ptnDirNo, ohlcValues))
            {
                if (currentPrice <= _lowerchannel && _currentMP == 0)
                {
                    _currentMP = -1;
                    _daysInTrade = 1;
                    _entriesToday++;
                    _lastEntryDate = currentDate;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = _lowerchannel,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        StopLoss = _myStop > 0 ? (decimal?)_myStop : null,
                        TakeProfit = _myProfit > 0 ? (decimal?)_myProfit : null,
                        BreakEven = _myBreakEven > 0 ? (decimal?)_myBreakEven : null,
                        Reason = "SE"
                    };
                }
            }
        }
        
        // EXIT per MaxDays
        if (_daysInTrade >= _maxDaysInTrade && _maxDaysInTrade > 0)
        {
            if (currentTime == _flatTime)
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

