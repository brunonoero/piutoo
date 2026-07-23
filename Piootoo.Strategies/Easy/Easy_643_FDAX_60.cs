using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_643
/// Strategia Trend medio periodo; ingresso su Breakout max/min di periodo; TimeFrame 1h
/// </summary>
public class Easy_643_FDAX_60 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _sessionStartTimeA = 800;
    private int _myPtnLY = 39;
    private int _myPtnLN = 40;
    private int _mySize = 1;
    private int _mybeginTime = 1500;
    private int _myStopL = 3000;
    private int _barsL = 50;
    private int _ptnNeutYes = 16;
    private int _ptnNeutNo = 10;
    private int _myLenght = 4;
    private int _myStopS = 3400;
    private decimal _multUp = 0.1m;
    private decimal _multDwn = 0.1m;
    private int _noDayShort = 1;
    private int _myPtnSY = 41;
    private int _myPtnSN = 33;
    private int _barsS = 34;

    // VARIABLES
    private decimal _pastO = 0;
    private decimal _highRange = 0;
    private decimal _lowRange = 0;
    private int _mP = 0;
    private bool _okShort = true;
    private bool _okLong = true;
    private bool _isStartOfSession = false;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 60;
    private string _name = "TOP_UA_643";
    private string _description = "Strategia Trend medio periodo; ingresso su Breakout max/min di periodo; TimeFrame 1h";

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
            if (parameters.TryGetValue("sessionStartTimeA", out var sessionstarttimea))
                _sessionStartTimeA = Convert.ToInt32(sessionstarttimea);
            if (parameters.TryGetValue("MyPtnLY", out var myptnly))
                _myPtnLY = Convert.ToInt32(myptnly);
            if (parameters.TryGetValue("MySize", out var mysize))
                _mySize = Convert.ToInt32(mysize);
            if (parameters.TryGetValue("MybeginTime", out var mybegintime))
                _mybeginTime = Convert.ToInt32(mybegintime);
            if (parameters.TryGetValue("MyStopL", out var mystopl))
                _myStopL = Convert.ToInt32(mystopl);
            if (parameters.TryGetValue("BarsL", out var barsl))
                _barsL = Convert.ToInt32(barsl);
            if (parameters.TryGetValue("PtnNeutYes", out var ptnneutyes))
                _ptnNeutYes = Convert.ToInt32(ptnneutyes);
            if (parameters.TryGetValue("MyLenght", out var mylenght))
                _myLenght = Convert.ToInt32(mylenght);
            if (parameters.TryGetValue("MyStopS", out var mystops))
                _myStopS = Convert.ToInt32(mystops);
            if (parameters.TryGetValue("MultUp", out var multup))
                _multUp = Convert.ToDecimal(multup);
            if (parameters.TryGetValue("MultDwn", out var multdwn))
                _multDwn = Convert.ToDecimal(multdwn);
            if (parameters.TryGetValue("NoDayShort", out var nodayshort))
                _noDayShort = Convert.ToInt32(nodayshort);
            if (parameters.TryGetValue("MyPtnLN", out var myptnln))
                _myPtnLN = Convert.ToInt32(myptnln);
            if (parameters.TryGetValue("PtnNeutNo", out var ptnneutno))
                _ptnNeutNo = Convert.ToInt32(ptnneutno);
            if (parameters.TryGetValue("MyPtnSY", out var myptnsy))
                _myPtnSY = Convert.ToInt32(myptnsy);
            if (parameters.TryGetValue("MyPtnSN", out var myptnsn))
                _myPtnSN = Convert.ToInt32(myptnsn);
            if (parameters.TryGetValue("BarsS", out var barss))
                _barsS = Convert.ToInt32(barss);
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
        
        // DEFINITIONS - Calcola variabili
        _okLong = true;
        _okShort = true;
        _highRange = (decimal)Highest(data, _myLenght, d => d.High);
        _lowRange = (decimal)Lowest(data, _myLenght, d => d.Low);
        
        // Calcola OHLC giornalieri
        var dailyData = GroupByDay(data, currentDate);
        if (dailyData.Count < 2)
        {
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Hold,
                Price = currentPrice,
                StrategyName = Name,
                Reason = "Dati giornalieri insufficienti"
            };
        }
        
        var dayO = dailyData[0].Open;
        var dayH = dailyData[0].High;
        var dayL = dailyData[0].Low;
        var pastO = dailyData[1].Open;
        var pastH = dailyData[1].High;
        var pastL = dailyData[1].Low;
        
        // Calcola ohlcValues per le funzioni pattern
        decimal[] ohlcValues = new decimal[24];
        _isStartOfSession = OHLCMulti5(_sessionStartTimeA, 2159, data, currentDate, out ohlcValues);
        
        // Aggiorna MP e MyCount
        // Reset MyCount se MP cambia
        _myCount++;
        
        // Aggiorna OkLong/OkShort basato su MP
        if (_currentMP == 1) _okLong = false;
        if (_currentMP == -1) _okShort = false;
        
        // Verifica se Ã¨ un nuovo giorno per reset MyCount
        if (_lastEntryDate.HasValue && _lastEntryDate.Value.Date != currentDate.Date)
        {
            if (_currentMP == 0) _myCount = 0;
        }
        _lastEntryDate = currentDate;
        
        // CONDITIONS - Condizioni di entry
        var entriesToday = EntriesToday(data, currentDate, _currentMP != 0);
        var ptnNeutYes = PatternNeutralFast(_ptnNeutYes, ohlcValues);
        var ptnNeutNo = PatternNeutralFast(_ptnNeutNo, ohlcValues);
        
        if (currentTime == _mybeginTime && entriesToday == 0 && ptnNeutYes && !ptnNeutNo)
        {
            // BUY condition
            if (_okLong && (dayH - dayO) > ((pastH - pastO) * _multUp) && 
                PtnBaseSA2(_myPtnLY, ohlcValues) && !PtnBaseSA2(_myPtnLN, ohlcValues))
            {
                _currentMP = 1;
                _myCount = 1;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = _highRange,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "LE Trend"
                };
            }
            
            // SELLSHORT condition
            if (_okShort && currentDate.DayOfWeek != (DayOfWeek)_noDayShort && 
                (dayO - dayL) > ((pastO - pastL) * _multDwn) &&
                PtnBaseSA2(_myPtnSY, ohlcValues) && !PtnBaseSA2(_myPtnSN, ohlcValues))
            {
                _currentMP = -1;
                _myCount = 1;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = _lowRange,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "SE Trend"
                };
            }
        }
        
        // EXIT ON BARCOUNT
        if (_myCount != 0)
        {
            if (_currentMP == 1 && _myCount >= _barsL)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Sell,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "LX-MaxBars"
                };
            }
            
            if (_currentMP == -1 && _myCount >= _barsS)
            {
                _currentMP = 0;
                return new TradeSignal
                {
                    Date = currentDate,
                    Type = SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _mySize,
                    Reason = "SX-MaxBars"
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

