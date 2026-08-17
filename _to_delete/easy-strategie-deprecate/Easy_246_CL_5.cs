using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_246 — Trend Developer sulla rottura degli estremi della sessione precedente, CL 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_246_CL_5__7.txt</c>.</para>
///
/// <para><b>Finestra a cavallo della mezzanotte:</b> <c>tw(2100, 900)</c> ha inizio maggiore
/// della fine, quindi copre 21:00–09:00 attraversando il cambio di giorno.</para>
///
/// <para><b>Uscita di fine sessione.</b> <c>ID = 0</c> e <c>MyTrigger = 2</c> attivano nel
/// sorgente la chiusura all'ultima barra della sessione, calcolata da <c>UACalcEndTime</c>. Qui è
/// espressa come <c>CloseAtUtc</c> sull'ingresso, all'orario di fine sessione: nell'originale è
/// un ordine emesso a runtime, che in <c>ExternalBroker</c> non verrebbe mai eseguito.</para>
///
/// <para><b>Contratto di riferimento:</b> CL, 1.000 barili, $1.000 per punto. Stop $1.625 = 1,625
/// punti, target $2.050 = 2,05 punti.</para>
/// </summary>
public sealed class Easy_246_CL_5 : TrendDeveloperEngine
{
    public override string Name => "Easy_246_CL_5";
    public override string Description => "Trend Developer, rottura estremi sessione precedente, CL 5m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 5;

    public Easy_246_CL_5()
    {
        SessionStartTime = 1800;  // sessionStartTimeA
        SessionEndTime = 1700;    // sessionEndTimeA
        Contracts = 1;

        Trigger = TrendTrigger.PreviousSessionOhlc;  // MyTrigger = 2 → highd1 / lowd1

        StartTrade = 2100;           // MyStartTrade
        EndTrade = 900;              // MyEndTrade — finestra a cavallo della mezzanotte
        InclusiveWindowEnd = false;  // l'originale usa tw()
        MaxTradesPerDay = 4;

        NeutralYes = 4;    // PtnNeutYes
        NeutralNo = 56;    // PtnNeutNo — sentinella "sempre falso": il gate non filtra
        DirectionalYes = 52;  // PtnDirYes — sentinella "sempre vero"
        DirectionalNo = 8;    // PtnDirNo

        BaseYesLong = 38;   // PtnLY
        BaseNoLong = 42;    // PtnLN — sentinella "sempre falso"
        BaseYesShort = 41;  // PtnSY — sentinella "sempre vero"
        BaseNoShort = 42;   // PtnSN

        CloseAtTime = SessionEndTime;  // ID = 0 → chiusura di fine sessione

        StopMoney = 1625;    // MyStop
        ProfitMoney = 2050;  // MyProfit
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
