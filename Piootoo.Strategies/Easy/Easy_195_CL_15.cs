using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_195
/// TITAN EXPORT
/// </summary>
public class Easy_195_CL_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _titanExportMode = 0;
    private int _compression_Lenght = 2;
    private int _startTrade = 2000;
    private int _mySize = 1;
    private int _levelShiftL = 5;
    private int _maxDaysInTrade = 3;
    private int _startSessionTimeC = 1800;
    private int _ptnNeutYes = 26;
    private int _skipDayLong = 0;

    // VARIABLES
    private int _daysintrade = 0;
    private int _trigger_long = 0;
    private bool _compression_condition = false;
    private bool _isstartofsession = false;

    // STATE
    private string _symbol = "@CL";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_195";
    private string _description = "TITAN EXPORT";

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
            if (parameters.TryGetValue("Compression_Lenght", out var compression_lenght))
                _compression_Lenght = Convert.ToInt32(compression_lenght);
            if (parameters.TryGetValue("StartTrade", out var starttrade))
                _startTrade = Convert.ToInt32(starttrade);
            if (parameters.TryGetValue("MySize", out var mysize))
                _mySize = Convert.ToInt32(mysize);
            if (parameters.TryGetValue("LevelShiftL", out var levelshiftl))
                _levelShiftL = Convert.ToInt32(levelshiftl);
            if (parameters.TryGetValue("MaxDaysInTrade", out var maxdaysintrade))
                _maxDaysInTrade = Convert.ToInt32(maxdaysintrade);
            if (parameters.TryGetValue("StartSessionTimeC", out var startsessiontimec))
                _startSessionTimeC = Convert.ToInt32(startsessiontimec);
            if (parameters.TryGetValue("PtnNeutYes", out var ptnneutyes))
                _ptnNeutYes = Convert.ToInt32(ptnneutyes);
            if (parameters.TryGetValue("SkipDayLong", out var skipdaylong))
                _skipDayLong = Convert.ToInt32(skipdaylong);
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

