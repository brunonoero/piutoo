using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_515
/// Questo codice e' di tipo mean reverting e sfrutta i falsi breakout del max/min di ieri.
/// </summary>
public class Easy_515_FDAX_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _mycontracts = 1;
    private int _sessionStartTimeA = 800;
    private int _sessionEndTimeA = 2200;
    private int _levelShift = 7;
    private int _myTick = 1;
    private int _maxTradesPerDay = 2;
    private int _ptnLY = 41;
    private int _ptnLN = 42;
    private int _ptnSY = 41;
    private int _ptnSN = 42;
    private int _ptnLY_ext = 26;
    private int _ptnLN_ext = 74;
    private int _ptnSY_ext = 40;
    private int _ptnSN_ext = 9;
    private int _myStartTime = 800;
    private int _myEndTime = 1445;
    private int _myStartPause = 1200;
    private int _myEndPause = 1100;
    private int _maxDaysLong = 6;
    private int _maxDaysShort = 6;
    private int _flatTime = 2145;
    private int _skipSessL = 5;
    private int _skipSessS = 5;
    private int _myStopL = 3000;
    private int _myStopS = 4000;
    private int _myBreakevenL = 0;
    private int _myBreakevenS = 0;
    private int _myProfitL = 0;
    private int _myProfitS = 5000;

    // VARIABLES
    private bool _oKL = true;
    private bool _okShort = true;
    private bool _isStartOfSession = false;
    private int _daysInTrade = 0;
    private int _mySessionEntries = 0;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_515";
    private string _description = "Questo codice e' di tipo mean reverting e sfrutta i falsi breakout del max/min di ieri.";

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
        var currentTime = currentDate.Hour * 100 + currentDate.Minute; // Formato HHMM
        var endsession = _sessionEndTimeA >= 2400 ? _sessionStartTimeA : _sessionEndTimeA;
        
        // Calcola OHLC per le ultime sessioni
        decimal[] ohlcValues = new decimal[24];
        _isStartOfSession = OHLCMulti5(_sessionStartTimeA, endsession, data, currentDate, out ohlcValues);
        
        var highd1 = ohlcValues[5]; // High del giorno precedente
        var lowd1 = ohlcValues[6];  // Low del giorno precedente
        
        // Reset OKL/OKS all'inizio sessione
        if (_isStartOfSession)
        {
            _oKL = true;
            _okShort = true;
            if (_currentMP != 0)
            {
                _daysInTrade++;
            }
            _mySessionEntries = 0;
        }
        
        // Se giÃ  in posizione, disabilita entry nella stessa direzione
        if (_currentMP == 1) _oKL = false;
        if (_currentMP == -1) _okShort = false;
        
        // Calcola trigger levels con LevelShift
        var mySETrigger = highd1 + _levelShift * _myTick;
        var myLETrigger = lowd1 - _levelShift * _myTick;
        
        // Condizioni operative
        var inTimeWindow = currentTime >= _myStartTime && currentTime <= _myEndTime && 
                          (currentTime < _myStartPause || currentTime > _myEndPause);
        var sow = (int)currentDate.DayOfWeek; // Session of week
        
        // Traccia session entries quando MP cambia
        if (_currentMP != 0 && _lastEntryDate.HasValue && _lastEntryDate.Value.Date != currentDate.Date)
        {
            _mySessionEntries++;
        }
        
        if (inTimeWindow && _mySessionEntries < _maxTradesPerDay)
        {
            // BUY condition: mean reverting dopo falso breakout
            // Entra quando prezzo torna sopra MyLETrigger dopo essere stato sotto
            var prevClose = data.Length > 1 ? data[data.Length - 2].Close : currentPrice;
            
            if (_oKL && UAPtnBase(_ptnLY, ohlcValues) && !UAPtnBase(_ptnLN, ohlcValues) &&
                PatternFast(_ptnLY_ext, ohlcValues) && !PatternFast(_ptnLN_ext, ohlcValues) &&
                sow != _skipSessL && prevClose < myLETrigger && currentPrice > myLETrigger)
            {
                _currentMP = 1;
                _daysInTrade = 1;
                _lastEntryDate = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mycontracts,
                    StopLoss = _myStopL > 0 ? (decimal?)_myStopL : null,
                    TakeProfit = _myProfitL > 0 ? (decimal?)_myProfitL : null,
                    BreakEven = _myBreakevenL > 0 ? (decimal?)_myBreakevenL : null,
                    Reason = "LE"
                };
            }
            
            // SELLSHORT condition
            if (_okShort && UAPtnBase(_ptnSY, ohlcValues) && !UAPtnBase(_ptnSN, ohlcValues) &&
                PatternFast(_ptnSY_ext, ohlcValues) && !PatternFast(_ptnSN_ext, ohlcValues) &&
                sow != _skipSessS && prevClose > mySETrigger && currentPrice < mySETrigger)
            {
                _currentMP = -1;
                _daysInTrade = 1;
                _lastEntryDate = currentDate;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mycontracts,
                    StopLoss = _myStopS > 0 ? (decimal?)_myStopS : null,
                    TakeProfit = _myProfitS > 0 ? (decimal?)_myProfitS : null,
                    BreakEven = _myBreakevenS > 0 ? (decimal?)_myBreakevenS : null,
                    Reason = "SE"
                };
            }
        }
        
        // Exit conditions per MaxDays
        if (_currentMP == 1 && _maxDaysLong > 0 && _daysInTrade >= _maxDaysLong)
        {
            if (currentTime >= _flatTime && currentTime < endsession)
            {
                _currentMP = 0;
                _daysInTrade = 0;
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
        }
        
        if (_currentMP == -1 && _maxDaysShort > 0 && _daysInTrade >= _maxDaysShort)
        {
            if (currentTime >= _flatTime && currentTime < endsession)
            {
                _currentMP = 0;
                _daysInTrade = 0;
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
        
        // Aggiorna daysInTrade se MP cambia
        if (_lastEntryDate.HasValue && _lastEntryDate.Value.Date != currentDate.Date && _currentMP != 0)
        {
            _daysInTrade++;
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

