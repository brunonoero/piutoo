using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_228
/// Sistema mean reverting che entra nella direzione opposta al gap quando viene superato il max o il min di giornata
/// </summary>
public class Easy_228_FDAX_30 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _mycontracts = 1;
    private int _sessionStartTimeA = 800;
    private int _sessionEndTimeA = 2200;
    private int _maxTradesPerDay = 1;
    private int _ptnLY = 41;
    private int _ptnLN = 42;
    private int _ptnSY = 41;
    private int _ptnSN = 42;
    private int _ptnLY_ext = 26;
    private int _ptnLN_ext = 43;
    private int _ptnSY_ext = 152;
    private int _ptnSN_ext = 153;
    private int _myStartTime = 800;
    private int _myEndTime = 1430;
    private int _myStartPause = 1200;
    private int _myEndPause = 1100;
    private int _maxDaysLong = 1;
    private int _maxDaysShort = 1;
    private int _flatTime = 1930;
    private int _skipSessL = -1;
    private int _skipSessS = 5;
    private int _myStopL = 1800;
    private int _myStopS = 1600;
    private int _myBreakevenL = 1500;
    private int _myBreakevenS = 2000;
    private int _myProfitL = 6500;
    private int _myProfitS = 6500;

    // VARIABLES
    private bool _oKL = true;
    private bool _okShort = true;
    private bool _isStartOfSession = false;
    private int _daysInTrade = 0;
    private int _mySessionEntries = 0;
    private decimal _myLETrigger = 0;
    private decimal _mySETrigger = 0;
    private bool _gapL = false;
    private bool _gapS = false;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 30;
    private string _name = "TOP_UA_228";
    private string _description = "Sistema mean reverting che entra nella direzione opposta al gap quando viene superato il max o il min di giornata";

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
        
        var opend0 = ohlcValues[0];
        var highd0 = ohlcValues[1];
        var lowd0 = ohlcValues[2];
        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];
        
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
            
            // Calcola gap
            _gapL = opend0 < lowd1;
            _gapS = opend0 > highd1;
            
            // Set trigger levels
            _myLETrigger = highd0;
            _mySETrigger = lowd0;
        }
        
        // Se giÃ  in posizione, disabilita entry nella stessa direzione
        if (_currentMP == 1) _oKL = false;
        if (_currentMP == -1) _okShort = false;
        
        // Traccia session entries quando MP cambia
        if (_currentMP != 0 && _lastEntryDate.HasValue && _lastEntryDate.Value.Date != currentDate.Date)
        {
            _mySessionEntries++;
        }
        
        // Condizioni operative
        var inTimeWindow = currentTime >= _myStartTime && currentTime <= _myEndTime && 
                          (currentTime < _myStartPause || currentTime > _myEndPause);
        var sow = (int)currentDate.DayOfWeek; // Session of week
        
        if (inTimeWindow && _mySessionEntries < _maxTradesPerDay)
        {
            // BUY condition: mean reverting dopo gap down e breakout sopra highd0
            var prevClose = data.Length > 1 ? data[data.Length - 2].Close : currentPrice;
            
            if (_oKL && _gapL && UAPtnBase(_ptnLY, ohlcValues) && !UAPtnBase(_ptnLN, ohlcValues) &&
                PatternFast(_ptnLY_ext, ohlcValues) && !PatternFast(_ptnLN_ext, ohlcValues) &&
                sow != _skipSessL && prevClose < _myLETrigger && currentPrice > _myLETrigger)
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
            
            // SELLSHORT condition: mean reverting dopo gap up e breakout sotto lowd0
            if (_okShort && _gapS && UAPtnBase(_ptnSY, ohlcValues) && !UAPtnBase(_ptnSN, ohlcValues) &&
                PatternFast(_ptnSY_ext, ohlcValues) && !PatternFast(_ptnSN_ext, ohlcValues) &&
                sow != _skipSessS && prevClose > _mySETrigger && currentPrice < _mySETrigger)
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
        
        // Reset DaysInTrade quando MP cambia
        if (_lastEntryDate.HasValue && _lastEntryDate.Value.Date != currentDate.Date && _currentMP != 0)
        {
            _daysInTrade = 1;
        }
        
        // Exit conditions per MaxDays
        var flatTimeFixed = endsession;
        if (_currentMP == 1 && _maxDaysLong > 0 && _daysInTrade >= _maxDaysLong)
        {
            if (currentTime >= _flatTime && currentTime < flatTimeFixed)
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
            if (currentTime >= _flatTime && currentTime < flatTimeFixed)
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
        
        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = currentPrice,
            StrategyName = Name
        };
    }
}

