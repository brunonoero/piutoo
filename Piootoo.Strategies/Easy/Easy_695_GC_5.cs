using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_695
/// Mirrored pattern breakout strategy for Gold 5 min
/// </summary>
public class Easy_695_GC_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1800;
    private int _sessionEndTimeC = 1700;
    private int _myContracts = 1;
    private int _ptnNeutYes = 3;
    private int _ptnNeutNo = 35;
    private int _ptnDirYes = -27;
    private int _ptnDirNo = 8;
    private int _myStartTrade = 0;
    private int _myEndTrade = 1500;
    private int _myStartPause = 1200;
    private int _myEndPause = 1100;
    private int _id = 0;
    private int _myStop = 1800;
    private int _myProfit = 0;

    // STATE
    private string _symbol = "@GC";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_695";
    private string _description = "Mirrored pattern breakout strategy";
    private int _currentMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("sessionStartTimeC", out var sst)) _sessionStartTimeC = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeC", out var set)) _sessionEndTimeC = Convert.ToInt32(set);
            if (parameters.TryGetValue("Mycontracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnDirNo", out var pdn)) _ptnDirNo = Convert.ToInt32(pdn);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("MyStartPause", out var msp)) _myStartPause = Convert.ToInt32(msp);
            if (parameters.TryGetValue("MyEndPause", out var mep)) _myEndPause = Convert.ToInt32(mep);
            if (parameters.TryGetValue("ID", out var id)) _id = Convert.ToInt32(id);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
        }
    }

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
        var currentHigh = data.Last().High;
        var currentLow = data.Last().Low;
        var currentTime = currentDate.Hour * 100 + currentDate.Minute;

        // Calcola OHLC
        decimal[] ohlcValues = new decimal[24];
        OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, currentDate, out ohlcValues);

        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];

        // Intraday exit
        if (_id == 0)
        {
            if (currentTime == 1655)
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
                        Reason = "LX End of Day"
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
                        Reason = "SX End of Day"
                    };
                }
            }
        }

        // Time window and pause check
        bool inTimeWindow = TimeWindow(_myStartTrade, _myEndTrade, currentDate);
        bool notInPause = currentTime < _myStartPause || currentTime > _myEndPause;

        // Entry conditions with mirrored patterns
        if (inTimeWindow && notInPause &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
        {
            // Long entry
            if (_currentMP <= 0 &&
                PatternDirectionalFast(_ptnDirYes, ohlcValues) && !PatternDirectionalFast(_ptnDirNo, ohlcValues))
            {
                if (currentHigh >= highd1)
                {
                    _currentMP = 1;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Buy,
                        Price = highd1,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "LE Pattern Breakout HighD1"
                    };
                }
            }

            // Short entry (mirrored pattern)
            if (_currentMP >= 0 &&
                PatternDirectionalFast(-_ptnDirYes, ohlcValues) && !PatternDirectionalFast(-_ptnDirNo, ohlcValues))
            {
                if (currentLow <= lowd1)
                {
                    _currentMP = -1;
                    return new TradeSignal
                    {
                        Date = currentDate,
                        Type = SignalType.Sell,
                        Price = lowd1,
                        StrategyName = Name,
                        Quantity = _myContracts,
                        Reason = "SE Pattern Breakout LowD1"
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
