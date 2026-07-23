using System;
using System.Collections.Generic;
using System.Linq;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// Strategia EasyLanguage convertita: TOP_UA_152
/// Breakout strategy with patterns for NQ 5 min.
/// Emette intent stop per la barra successiva; fill e posizione sono dell'engine.
/// </summary>
public class Easy_152_NQ_5 : StatelessEasyStrategyBase
{
    // INPUTS
    private int _sessionStartTimeA = 1700;
    private int _sessionEndTimeA = 1559;
    private int _maxTradesPerDay = 3;
    private int _ptnDirYes = 52;
    private int _ptnNeutYes = 3;
    private int _ptnNeutNo = 23;
    private int _ptnLY = 12;
    private int _ptnLN = 40;
    private int _ptnSY = 12;
    private int _ptnSN = 40;
    private int _myStop = 1200;
    private int _myProfit = 0;
    private int _id = 0;
    private int _myTrigger = 1;
    private int _myDayNoLong = 2;
    private int _myDayNoShort = 2;
    private int _myContracts = 1;
    private int _myStartTrade = 930;
    private int _myEndTrade = 1530;
    private int _closeAtTime = 1555;
    private int _sessTest = 0;

    // VARIABLES
    private int _endSession = 1559;
    private int _myEndTime = 0;
    private decimal _myLE = 99999;
    private decimal _mySE = 0;
    private int _entriesToday = 0;

    // STATE
    private string _symbol = "@NQ";
    private int _timeframeMinutes = 5;
    private string _name = "TOP_UA_152";
    private string _description = "Breakout strategy with patterns for NQ";
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
            if (parameters.TryGetValue("sessionStartTimeA", out var sst)) _sessionStartTimeA = Convert.ToInt32(sst);
            if (parameters.TryGetValue("sessionEndTimeA", out var set)) _sessionEndTimeA = Convert.ToInt32(set);
            if (parameters.TryGetValue("MaxTradesPerDay", out var mtpd)) _maxTradesPerDay = Convert.ToInt32(mtpd);
            if (parameters.TryGetValue("PtnDirYes", out var pdy)) _ptnDirYes = Convert.ToInt32(pdy);
            if (parameters.TryGetValue("PtnNeutYes", out var pny)) _ptnNeutYes = Convert.ToInt32(pny);
            if (parameters.TryGetValue("PtnNeutNo", out var pnn)) _ptnNeutNo = Convert.ToInt32(pnn);
            if (parameters.TryGetValue("PtnLY", out var ply)) _ptnLY = Convert.ToInt32(ply);
            if (parameters.TryGetValue("PtnLN", out var pln)) _ptnLN = Convert.ToInt32(pln);
            if (parameters.TryGetValue("PtnSY", out var psy)) _ptnSY = Convert.ToInt32(psy);
            if (parameters.TryGetValue("PtnSN", out var psn)) _ptnSN = Convert.ToInt32(psn);
            if (parameters.TryGetValue("MyStop", out var ms)) _myStop = Convert.ToInt32(ms);
            if (parameters.TryGetValue("MyProfit", out var mp)) _myProfit = Convert.ToInt32(mp);
            if (parameters.TryGetValue("ID", out var id)) _id = Convert.ToInt32(id);
            if (parameters.TryGetValue("MyTrigger", out var mt)) _myTrigger = Convert.ToInt32(mt);
            if (parameters.TryGetValue("mydayNolong", out var mdnl)) _myDayNoLong = Convert.ToInt32(mdnl);
            if (parameters.TryGetValue("mydayNoshort", out var mdns)) _myDayNoShort = Convert.ToInt32(mdns);
            if (parameters.TryGetValue("MyContracts", out var mc)) _myContracts = Convert.ToInt32(mc);
            if (parameters.TryGetValue("MyStartTrade", out var mst)) _myStartTrade = Convert.ToInt32(mst);
            if (parameters.TryGetValue("MyEndTrade", out var met)) _myEndTrade = Convert.ToInt32(met);
            if (parameters.TryGetValue("CloseAtTime", out var cat)) _closeAtTime = Convert.ToInt32(cat);
            if (parameters.TryGetValue("sesstest", out var st)) _sessTest = Convert.ToInt32(st);
        }

        // End session setting: dopo i parametri, come in EasyLanguage.
        if (_sessionEndTimeA >= 2400) _endSession = _sessionStartTimeA;
        else _endSession = _sessionEndTimeA;
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

        OHLCMulti5(_sessionStartTimeA, _endSession, data, barTime, out var ohlcValues);

        var highd0 = ohlcValues[1];
        var lowd0 = ohlcValues[2];
        var highd1 = ohlcValues[5];
        var lowd1 = ohlcValues[6];

        if (_closeAtTime != 2500) _myEndTime = _closeAtTime;
        else _myEndTime = _endSession;

        // levels choice — trigger 0 usa high/low di sessione (highs(0)/lows(0) Unger)
        if (_myTrigger == 0 || _myTrigger == 1)
        {
            _myLE = highd0;
            _mySE = lowd0;
        }
        else if (_myTrigger == 2)
        {
            _myLE = highd1;
            _mySE = lowd1;
        }

        int calcStart = _sessTest == 0 ? _myStartTrade : _sessionStartTimeA + 200;
        int calcEnd = _sessTest == 0 ? _myEndTrade : _endSession - 200;
        if (calcStart > 2359) calcStart -= 2400;
        if (calcEnd < 0) calcEnd += 2400;

        bool timeWindow = TimeWindowInclusive(calcStart, calcEnd, barTime);

        // Exit fine sessione: next bar at market (CloseOnly)
        if (_id == 0 && _myTrigger >= 1 && currentTime == _myEndTime && _currentMP != 0)
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
                CloseOnly = true,
                IsPositionCloseDependent = true,
                Reason = _currentMP == 1 ? "EOSessLX" : "EOSessSX"
            };
        }

        if (_id == 0 && _myTrigger == 0)
        {
            // setexitonclose: l'engine chiude alla fine della sessione.
            // Gli intent di entry sotto riportano CloseAtUtc.
        }

        DateTime? sessionCloseUtc = null;
        if (_id == 0 && _myTrigger == 0)
        {
            sessionCloseUtc = CombineDateAndHhmm(barTime.Date, _endSession);
            if (GetHhmm(barTime) > _endSession)
            {
                sessionCloseUtc = sessionCloseUtc.Value.AddDays(1);
            }
        }

        var companions = new List<TradeSignal>();

        if (timeWindow && _entriesToday < _maxTradesPerDay &&
            PatternNeutralFast(_ptnNeutYes, ohlcValues) && !PatternNeutralFast(_ptnNeutNo, ohlcValues))
        {
            bool longSetup = _currentMP <= 0 &&
                             (int)barTime.DayOfWeek != _myDayNoLong &&
                             PtnBaseSA2(_ptnLY, ohlcValues) && !PtnBaseSA2(_ptnLN, ohlcValues) &&
                             PatternDirectionalFast(_ptnDirYes, ohlcValues);

            bool shortSetup = _currentMP >= 0 &&
                              (int)barTime.DayOfWeek != _myDayNoShort &&
                              PtnBaseSA2(_ptnSY, ohlcValues) && !PtnBaseSA2(_ptnSN, ohlcValues) &&
                              PatternDirectionalFast(-_ptnDirYes, ohlcValues);

            if (longSetup)
            {
                companions.Add(CreateStopIntent(
                    barTime, SignalType.Buy, _myLE, nextBarUtc, sessionCloseUtc, "LE Pattern Breakout"));
            }

            if (shortSetup)
            {
                companions.Add(CreateStopIntent(
                    barTime, SignalType.Sell, _mySE, nextBarUtc, sessionCloseUtc, "SE Pattern Breakout"));
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
}
