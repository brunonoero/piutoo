using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

// HYBRID: l'originale chiude su tracciamento dell'utile aperto tra sessioni (Guadagno/vendita)
// e su uscite a mercato alle 16:45 — non dichiarabili come specifica d'ingresso.

/// <summary>
/// TOP_UA_851 — Trend Developer su rottura highd0/lowd0 filtrata da media mobile e ADX, GC 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_851_GC_5__7.txt</c>. L'ingresso long
/// richiede <c>close ≥ averageFC(200)</c>; lo short richiede <c>close &lt; averageFC(200)</c>.
/// Le uscite su deterioramento dell'utile restano close-dependent.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $2.000, breakeven $3.000.</para>
/// </summary>
public sealed class Easy_851_GC_5 : TrendDeveloperEngine
{
    private const int MaLength = 200;
    private const int AdxLength = 5;
    private const int AdxThreshold = 60;
    private const int ProfitCheckTime = 1630;
    private const int ExitTime = 1645;

    private decimal _adxValue;
    private decimal _adx0;
    private decimal _adx1;
    private decimal _adx2;
    private decimal _adx3;
    private decimal _profit;
    private decimal _previousProfit;
    private bool _profitDecreasing;
    private int _sessionsInTrade;

    public override string Name => "Easy_851_GC_5";
    public override string Description => "Trend Developer con filtro MA/ADX e uscite su utile, GC 5m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 5;

    public override int RequiredCandles => Math.Max(SessionsToCandles(6), MaLength + 1);

    public override bool IsPositionCloseDependent => true;

    private int MaxSessionsInTrade { get; set; } = 3;

    public Easy_851_GC_5()
    {
        SessionStartTime = 1800;  // sessionStartTimeC
        SessionEndTime = 1700;    // sessionEndTimeC
        Contracts = 1;

        Trigger = TrendTrigger.CurrentSessionOhlc;

        StartTrade = 2300;  // MyStartTrade
        EndTrade = 1000;    // MyEndTrade
        PauseStart = 1200;  // coppia invertita: pausa inattiva
        PauseEnd = 1100;

        NeutralYes = 4;       // PtnNeutYes
        NeutralNo = 30;       // PtnNeutNo
        DirectionalYes = 52;  // PtnDirYes
        DirectionalNo = -28;  // PtnDirNo

        StopMoney = 2000;       // MyStop
        ProfitMoney = 0;        // MyProfit
        BreakEvenMoney = 3000;  // Mybreakeven
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var time = Hhmm(barTime);
        var isStartOfSession = BuildSessionOhlc(data, barTime, out var ohlc);

        if (isStartOfSession)
            UpdateSessionAdx(ohlc);

        TrackSessions(isStartOfSession);

        if (time == ProfitCheckTime && CurrentMP != 0)
        {
            _previousProfit = _profit;
            _profit = 0m;
            _profitDecreasing = _profit < _previousProfit;
        }

        var exit = EvaluateProfitExits(bar, time);
        if (exit.Type != SignalType.Hold)
            return exit;

        if (!PassesAdxGate())
            return Hold(bar.Close, barTime);

        return EvaluateCore(data, currentDate);
    }

    protected override bool PassesExtraGates(decimal[] ohlc, OhlcvData[] data, DateTime barTime) =>
        PassesAdxGate() && OutsidePause(barTime);

    protected override bool PassesDirectionalExtraGates(
        SignalType side, decimal[] ohlc, OhlcvData[] data, DateTime barTime)
    {
        var average = AverageClose(data, MaLength);
        var close = data[^1].Close;

        if (side == SignalType.Buy)
            return close >= average;

        return close < average;
    }

    private bool PassesAdxGate() => _adxValue < AdxThreshold;

    private bool OutsidePause(DateTime barTime)
    {
        if (PauseStart < 0 || PauseEnd < 0) return true;
        var time = Hhmm(barTime);
        return time < PauseStart || time > PauseEnd;
    }

    private void TrackSessions(bool isStartOfSession)
    {
        if (CurrentMP == 0)
        {
            _sessionsInTrade = 0;
            return;
        }

        if (isStartOfSession)
            _sessionsInTrade++;

        if (_sessionsInTrade <= 0)
            _sessionsInTrade = 1;
    }

    private TradeSignal EvaluateProfitExits(OhlcvData bar, int time)
    {
        if (CurrentMP == 0 || time != ExitTime)
            return Hold(bar.Close, bar.DateTime);

        if (_sessionsInTrade == 1 && _profit < 0m)
        {
            return MarketClose(bar, "Exit FirstSession Loss");
        }

        if (_profitDecreasing && _sessionsInTrade > 1)
        {
            return MarketClose(bar, CurrentMP == 1 ? "LongDaGuadagno" : "ShortDaGuadagno");
        }

        if (MaxSessionsInTrade > 0 && _sessionsInTrade == MaxSessionsInTrade)
        {
            return MarketClose(bar, CurrentMP == 1 ? "LongMaxGG" : "ShortMaxGG");
        }

        return Hold(bar.Close, bar.DateTime);
    }

    private TradeSignal MarketClose(OhlcvData bar, string reason) =>
        new()
        {
            Date = bar.DateTime,
            Type = CurrentMP == 1 ? SignalType.Sell : SignalType.Buy,
            Price = bar.Close,
            StrategyName = Name,
            Quantity = Contracts,
            OrderType = TradeOrderType.Market,
            Reason = reason
        };

    private void UpdateSessionAdx(decimal[] ohlc)
    {
        var calc = new[] { _adx0, _adx1, _adx2, _adx3 };
        _adxValue = EasyLib.iADXOnArray(
            AdxLength,
            ohlc[5], ohlc[6], ohlc[7],
            ohlc[9], ohlc[10], ohlc[11],
            ref calc) * 100m;
        _adx0 = calc[0];
        _adx1 = calc[1];
        _adx2 = calc[2];
        _adx3 = calc[3];
    }

    private static decimal AverageClose(OhlcvData[] data, int length)
    {
        var count = Math.Min(length, data.Length);
        decimal sum = 0m;
        for (var index = data.Length - count; index < data.Length; index++)
            sum += data[index].Close;
        return sum / count;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("Numbarre", out var sessions))
            MaxSessionsInTrade = Convert.ToInt32(sessions);
    }
}
