using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_244
/// Strategia EasyLanguage convertita
/// </summary>
public class Easy_244_FDAX_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _myStop = 120;
    private int _myPtnLN = 42;
    private int _myEntryHour = 1745;
    private int _myExitHour = 900;
    private decimal _amt = 0.4m;

    // VARIABLES
    private int _mP = 0;
    private bool _massimocondition = false;
    private bool _andreacondition = false;
    private bool _carloscondition = false;

    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_244";
    private string _description = "Strategia EasyLanguage convertita";

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
        
        // DEFINITIONS - Calcola condizioni
        var closed0 = currentPrice;
        var highd1 = GetDailyHigh(data, currentDate, 1);
        var lowd1 = GetDailyLow(data, currentDate, 1);
        var closed1 = GetDailyClose(data, currentDate, 1);
        
        _massimocondition = closed0 < highd1 && closed0 > lowd1;
        _andreacondition = closed0 > (closed1 - closed1 * _amt / 100m);
        
        // Calcola OHLC per pattern
        decimal[] ohlcValues = new decimal[24];
        bool isStartOfSession = OHLCMulti5(800, 2200, data, currentDate, out ohlcValues);
        _carloscondition = !PtnBaseSA2(_myPtnLN, ohlcValues);
        
        // BUY condition: BIAS Overnight - entra all'ora di entry se condizioni sono soddisfatte
        if (currentTime == _myEntryHour && _massimocondition && _andreacondition && _carloscondition && 
            (int)currentDate.DayOfWeek != 5 && _currentMP == 0)
        {
            _currentMP = 1;
            _myCount = 1;
            _lastEntryDate = currentDate;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Buy,
                Price = currentPrice, // Entra all'open della prossima barra (semplificato: usa current price)
                StrategyName = Name,
                Quantity = 1,
                StopLoss = _myStop > 0 ? (decimal?)(_myStop * 1) : null, // BigPointValue semplificato a 1
                Reason = "LE_Overnight"
            };
        }
        
        // EXIT condition: esce all'ora di exit
        if (_currentMP == 1 && currentTime == _myExitHour)
        {
            _currentMP = 0;
            return new TradeSignal
            {
                Date = currentDate,
                Type = SignalType.Sell,
                Price = currentPrice,
                StrategyName = Name,
                Quantity = 1,
                Reason = "LX_ExitHour"
            };
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

