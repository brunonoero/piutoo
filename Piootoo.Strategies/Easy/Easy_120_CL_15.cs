using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_120
/// Breakout on N-sessions highest high / lowest low
/// </summary>
public class Easy_120_CL_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _mycontracts = 1;
    private int _sessBegin = 1800;

    // VARIABLES
    private int _hh = 0;
    private bool _isStartOfSession = false;

    // STATE
    private string _symbol = "@CL";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_120";
    private string _description = "Breakout on N-sessions highest high / lowest low";

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
            if (parameters.TryGetValue("SessBegin", out var sessbegin))
                _sessBegin = Convert.ToInt32(sessbegin);
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

