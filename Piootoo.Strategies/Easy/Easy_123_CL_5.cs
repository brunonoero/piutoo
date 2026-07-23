using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_123
/// ENTRATA INCROCIO AROON INDICATOR SIA LONG CHE SHORT, CL 5 MIN IS  ////
/// </summary>
public class Easy_123_CL_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1800;
    private int _myPtnLY = 41;
    private int _mylenght = 22;
    private int _titanExportMode = 0;
    private int _myStarttime = 0100;
    private int _numbar = 660;
    private int _myStop = 2100;

    // VARIABLES
    private int _mySize = 0;
    private int _bin = 0;
    private int _highd0 = 0;
    private bool _timeWindow = false;
    private string _aroonup = "0,data2";
    private bool _isStartOfSession = false;
    private int _daysinTrade = 0;

    // STATE
    private string _symbol = "@CL";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_123";
    private string _description = "ENTRATA INCROCIO AROON INDICATOR SIA LONG CHE SHORT, CL 5 MIN IS  ////";

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
            if (parameters.TryGetValue("SessionStartTimeA", out var sessionstarttimea))
                _sessionStartTimeA = Convert.ToInt32(sessionstarttimea);
            if (parameters.TryGetValue("MyPtnLY", out var myptnly))
                _myPtnLY = Convert.ToInt32(myptnly);
            if (parameters.TryGetValue("mylenght", out var mylenght))
                _mylenght = Convert.ToInt32(mylenght);
            if (parameters.TryGetValue("TitanExportMode", out var titanexportmode))
                _titanExportMode = Convert.ToInt32(titanexportmode);
            if (parameters.TryGetValue("MyStarttime", out var mystarttime))
                _myStarttime = Convert.ToInt32(mystarttime);
            if (parameters.TryGetValue("numbar", out var numbar))
                _numbar = Convert.ToInt32(numbar);
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

        // TODO: Implementare logica completa della strategia
        // Convertire condizioni buy/sellshort in TradeSignal
        // Per ora restituisce Hold - la logica deve essere implementata manualmente

        return new TradeSignal
        {
            Date = currentDate,
            Type = SignalType.Hold,
            Price = currentPrice,
            StrategyName = Name
        };
    }
}

