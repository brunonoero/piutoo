using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_99
/// @CL_BIAS_OnlyShort_TF5Min
/// </summary>
public class Easy_99_CL_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _ptnFastShortYes1 = 100;
    private int _myContracts = 1;
    private int _titanExportMode = 0;
    private int _sessionStartTimeA = 1800;
    private int _noTradingMonth = 0;

    // VARIABLES
    private bool _slb = false;
    private bool _isStartOfSession = false;

    // STATE
    private string _symbol = "@CL";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_99";
    private string _description = "@CL_BIAS_OnlyShort_TF5Min";

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
            if (parameters.TryGetValue("PtnFastShortYes1", out var ptnfastshortyes1))
                _ptnFastShortYes1 = Convert.ToInt32(ptnfastshortyes1);
            if (parameters.TryGetValue("MyContracts", out var mycontracts))
                _myContracts = Convert.ToInt32(mycontracts);
            if (parameters.TryGetValue("TitanExportMode", out var titanexportmode))
                _titanExportMode = Convert.ToInt32(titanexportmode);
            if (parameters.TryGetValue("sessionStartTimeA", out var sessionstarttimea))
                _sessionStartTimeA = Convert.ToInt32(sessionstarttimea);
            if (parameters.TryGetValue("NoTradingMonth", out var notradingmonth))
                _noTradingMonth = Convert.ToInt32(notradingmonth);
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

