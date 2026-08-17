using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

// HYBRID: l'originale emette stop di uscita su LowD(0)/HighD(0) rivalutati a ogni barra e
// disattiva la chiusura di fine giornata quando l'utile aperto supera MycheckGain alle 17:00.
// Queste uscite non sono dichiarabili all'ingresso; restano in GenerateSignal.

/// <summary>
/// TOP_UA_32 — breakout highd0/lowd0 con gate <c>PtnBaseSA2</c>, FDAX 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_32_FDAX_15__7.txt</c>. L'ingresso è
/// modellato da <see cref="TrendDeveloperEngine"/>; le uscite strutturali su estremo opposto e
/// il ramo <c>ExitID</c> restano close-dependent.</para>
///
/// <para><b>Contratto di riferimento:</b> FDAX, €25 per punto. Stop €1.450.</para>
/// </summary>
public sealed class Easy_32_FDAX_15 : TrendDeveloperEngine
{
    private const int CheckTime = 1700;
    private const int CheckGain = 1500;
    private const int EndDayStart = 2145;
    private const int EndDayEnd = 2200;

    private int _exitId;

    public override string Name => "Easy_32_FDAX_15";
    public override string Description => "Breakout estremi sessione + uscite strutturali, FDAX 15m";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 15;

    public override bool IsPositionCloseDependent => true;

    public Easy_32_FDAX_15()
    {
        SessionStartTime = 800;
        SessionEndTime = 2200;
        Contracts = 1;

        Trigger = TrendTrigger.CurrentSessionOhlc;

        StartTrade = 1000;  // MyStartTime
        EndTrade = 1300;    // MyEndTime
        InclusiveWindowEnd = true;

        NeutralYes = 55;
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        BaseYesLong = 1;    // MyPtnLY
        BaseNoLong = 6;     // MyPtnLN
        BaseYesShort = 20;  // MyPtnSY
        BaseNoShort = 4;    // MyPtnSN

        NotEntryDayLong = 5;   // MyPauseDay — venerdì
        NotEntryDayShort = 5;

        StopMoney = 1450;  // MyStop
        ProfitMoney = 0;
        MaxBars = 100;     // MaxBarsinTrade_Long / _Short
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate)
    {
        if (data is null || data.Length < RequiredCandles)
            return Hold(data?.LastOrDefault()?.Close ?? 0m, currentDate, "Dati insufficienti");

        var bar = data[^1];
        var barTime = bar.DateTime;
        var time = Hhmm(barTime);

        BuildSessionOhlc(data, barTime, out var ohlc);
        var exit = EvaluateStructuralExits(bar, time, ohlc);
        if (exit.Type != SignalType.Hold)
            return exit;

        if (time == EndTrade)
            _exitId = 0;

        if (time == CheckTime && CurrentMP != 0)
            _exitId = 1;

        return EvaluateCore(data, currentDate);
    }

    private TradeSignal EvaluateStructuralExits(OhlcvData bar, int time, decimal[] ohlc)
    {
        if (CurrentMP == 0)
            return Hold(bar.Close, bar.DateTime);

        if (_exitId == 0 && time >= EndDayStart && time < EndDayEnd)
        {
            return new TradeSignal
            {
                Date = bar.DateTime,
                Type = CurrentMP == 1 ? SignalType.Sell : SignalType.Buy,
                Price = bar.Close,
                StrategyName = Name,
                Quantity = Contracts,
                OrderType = TradeOrderType.Market,
                Reason = CurrentMP == 1 ? "LX_EndDay" : "SX_EndDay"
            };
        }

        if (CurrentMP == 1 && bar.Close <= ohlc[2])
        {
            return new TradeSignal
            {
                Date = bar.DateTime,
                Type = SignalType.Sell,
                Price = ohlc[2],
                StrategyName = Name,
                Quantity = Contracts,
                OrderType = TradeOrderType.Stop,
                Reason = "LX_Stop"
            };
        }

        if (CurrentMP == -1 && bar.Close >= ohlc[1])
        {
            return new TradeSignal
            {
                Date = bar.DateTime,
                Type = SignalType.Buy,
                Price = ohlc[1],
                StrategyName = Name,
                Quantity = Contracts,
                OrderType = TradeOrderType.Stop,
                Reason = "SX_Stop"
            };
        }

        return Hold(bar.Close, bar.DateTime);
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("MySize", out var size))
            Contracts = Convert.ToInt32(size);
    }
}
