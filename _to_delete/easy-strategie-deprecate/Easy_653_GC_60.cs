using Piootoo.Shared.Enums;
using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_653 — Trend Developer su rottura highd0/lowd0 con filtro midrange, GC 60 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_653_GC_60__7.txt</c>. Il long richiede
/// <c>HighD0</c> sopra il punto medio di <c>D1</c>; lo short richiede <c>LowD0</c> sotto.
/// Nessun gate di pattern.</para>
///
/// <para><b>Uscite.</b> Stop e target dichiarati all'ingresso; chiusura dopo
/// <c>TE_Day_close = 5</c> sessioni tramite <c>MaxDaysInTrade</c>.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $2.200, target $2.300.</para>
/// </summary>
public sealed class Easy_653_GC_60 : TrendDeveloperEngine
{
    public override string Name => "Easy_653_GC_60";
    public override string Description => "Trend Developer midrange, rottura estremi sessione, GC 60m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public Easy_653_GC_60()
    {
        SessionStartTime = 1800;  // sessionStartTimeC
        SessionEndTime = 1700;    // sessionEndTimeC
        Contracts = 1;

        Trigger = TrendTrigger.CurrentSessionOhlc;

        StartTrade = 1000;  // MyStartTime
        EndTrade = 1600;    // MyEndTime — fine esclusiva, come tw()
        InclusiveWindowEnd = false;

        // Nessun gate neutro/direzionale: sentinelle.
        NeutralYes = 55;
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        StopMoney = 2200;    // STOP_MyStop
        ProfitMoney = 2300;  // Target_MyProfit
        MaxDaysInTrade = 5;  // TE_Day_close
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    protected override bool PassesDirectionalExtraGates(
        SignalType side, decimal[] ohlc, OhlcvData[] data, DateTime barTime)
    {
        var highD1 = ohlc[5];
        var lowD1 = ohlc[6];
        var midRange = (highD1 - lowD1) * 0.5m;

        return side == SignalType.Buy
            ? ohlc[1] > lowD1 + midRange
            : ohlc[2] < highD1 - midRange;
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("mycontracts", out var contractsAlt))
            Contracts = Convert.ToInt32(contractsAlt);
    }
}
