using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_156
/// Unmirrored Trend Following for NQ 15 min.
/// Emette intent stop per la barra successiva; fill e posizione sono dell'engine.
/// </summary>
public class Easy_156_NQ_15 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeC = 1700;
    private int _sessionEndTimeC = 1600;
    private int _myContracts = 1;
    private int _myPtnLY = 54;
    private int _myPtnSY = 111;
    private int _myPtnLN = 75;
    private int _myPtnSN = 31;
    private int _myStartTrade = 1000;
    private int _myEndTrade = 1500;
    private int _id = 0;
    private int _myStop = 1750;
    private int _myProfit = 4500;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 15;
    private string _name = "TOP_UA_156";
    private string _description = "Unmirrored Trend Following for NQ";
    private int _currentMP = 0;

    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;
    public override bool IsPositionCloseDependent => true;

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters != null)
        {
            if (parameters.TryGetValue("Symbol", out var sym)) _symbol = sym?.ToString() ?? _symbol;
            if (parameters.TryGetValue("TimeframeMinutes", out var tf)) _timeframeMinutes = Convert.ToInt32(tf);
            if (parameters.TryGetValue("sessionStartTimeC", out var sst)) _sessionStartTimeC = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeC", out var set)) _sessionEndTimeC = Convert.ToInt32(set);
            if (parameters.TryGetValue("Mycontracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("MyPtnLY", out var mply)) _myPtnLY = Convert.ToInt32(mply);
            if (parameters.TryGetValue("MyPtnSY", out var mpsy)) _myPtnSY = Convert.ToInt32(mpsy);
            if (parameters.TryGetValue("MyPtnLN", out var mpln)) _myPtnLN = Convert.ToInt32(mpln);
            if (parameters.TryGetValue("MyPtnSN", out var mpsn)) _myPtnSN = Convert.ToInt32(mpsn);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
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

        var barTime = data.Last().DateTime;
        var currentPrice = data.Last().Close;
        var currentTime = GetHhmm(barTime);
        var nextBarUtc = EstimateNextBarUtc(data, barTime);

        OHLCMulti5(_sessionStartTimeC, _sessionEndTimeC, data, barTime, out var ohlcValues);

        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];

        // setexitonclose + exit window 15:45–16:00 next bar market
        if (_id == 0)
        {
            if (currentTime >= 1545 && currentTime < 1600 && _currentMP != 0)
            {
                return new TradeSignal
                {
                    Date = barTime,
                    Type = _currentMP == 1 ? SignalType.Sell : SignalType.Buy,
                    Price = currentPrice,
                    StrategyName = Name,
                    Quantity = _myContracts,
                    OrderType = TradeOrderType.Market,
                    ValidFromUtc = nextBarUtc,
                            CloseAtUtc = ResolveSessionCloseUtc(barTime, _sessionStartTimeC, _sessionEndTimeC),
                    Reason = _currentMP == 1 ? "LX End of Day" : "SX End of Day"
                };
            }
        }

        DateTime? sessionCloseUtc = _id == 0
            ? ResolveSessionCloseUtc(barTime, _sessionStartTimeC, _sessionEndTimeC)
            : null;

        bool inTimeWindow = TimeWindow(_myStartTrade, _myEndTrade, barTime);
        var companions = new List<TradeSignal>();

        if (inTimeWindow)
        {
            if (_currentMP <= 0 &&
                PatternFast(_myPtnLY, ohlcValues) &&
                !PatternFast(_myPtnLN, ohlcValues))
            {
                companions.Add(CreateStopIntent(
                    barTime, SignalType.Buy, highd1, nextBarUtc, sessionCloseUtc, "LE Breakout HighD1"));
            }

            if (_currentMP >= 0 &&
                PatternFast(_myPtnSY, ohlcValues) &&
                !PatternFast(_myPtnSN, ohlcValues))
            {
                companions.Add(CreateStopIntent(
                    barTime, SignalType.Sell, lowd1, nextBarUtc, sessionCloseUtc, "SE Breakout LowD1"));
            }
        }

        if (companions.Count == 0)
        {
            return new TradeSignal
            {
                Date = barTime,
                Type = SignalType.Hold,
                Price = currentPrice,
                StrategyName = Name
            };
        }

        var primary = companions[0];
        if (companions.Count > 1)
        {
            primary.CompanionSignals = companions.Skip(1).ToList();
        }

        return primary;
    }

    private TradeSignal CreateStopIntent(
        DateTime barTime,
        SignalType type,
        decimal stopPrice,
        DateTime validFromUtc,
        DateTime? closeAtUtc,
        string reason)
    {
        return new TradeSignal
        {
            Date = barTime,
            Type = type,
            Price = stopPrice,
            StrategyName = Name,
            Quantity = _myContracts,
            OrderType = TradeOrderType.Stop,
            ValidFromUtc = validFromUtc,
            CloseAtUtc = closeAtUtc,
            StopLossMoneyPerFutureContract = _myStop > 0 ? _myStop : null,
            TakeProfitMoneyPerFutureContract = _myProfit > 0 ? _myProfit : null,
            Reason = reason
        };
    }

    private static DateTime ResolveSessionCloseUtc(DateTime barTime, int sessionStart, int sessionEnd)
    {
        var candidate = CombineDateAndHhmm(barTime.Date, sessionEnd);
        var hhmm = GetHhmm(barTime);

        if (sessionStart > sessionEnd)
        {
            // overnight: dopo lo start serale la chiusura è il giorno successivo
            if (hhmm > sessionStart)
            {
                candidate = candidate.AddDays(1);
            }
        }
        else if (hhmm > sessionEnd)
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }
}
