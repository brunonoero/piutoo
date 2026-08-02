using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_195 — breakout stop su estremi d1 con finestra overnight, CL 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_195_CL_15____1440__7.txt</c>. Mappata su
/// <see cref="TrendDeveloperEngine"/> con trigger sulla sessione precedente; i gate di
/// compressione e volatilità ATR dell'originale non sono parametri del motore.</para>
///
/// <para><b>Livelli.</b> L'originale applica <c>LevelShift</c> ai trigger; il motore usa gli
/// estremi d1 puri (<c>highd1</c>/<c>lowd1</c>). Documentato come approssimazione accettabile
/// finché il motore non espone lo shift.</para>
///
/// <para><b>Uscite.</b> Stop, breakeven, target e deadline a 3 giorni dichiarati all'ingresso.</para>
///
/// <para><b>Contratto di riferimento:</b> CL, $1.000 per punto. Stop $1.400, breakeven $1.300,
/// target $2.400.</para>
/// </summary>
public sealed class Easy_195_CL_15 : TrendDeveloperEngine
{
    public override string Name => "Easy_195_CL_15";
    public override string Description => "Breakout d1 con finestra overnight, CL 15m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 15;

    public Easy_195_CL_15()
    {
        SessionStartTime = 1800;  // StartSessionTimeC
        SessionEndTime = 1700;    // EndSessionTimeC
        Contracts = 1;            // MySize

        Trigger = TrendTrigger.PreviousSessionOhlc;

        StartTrade = 2000;  // StartTrade
        EndTrade = 1200;    // EndTrade — fine esclusa
        PauseStart = 100;   // StartPause
        PauseEnd = 400;     // EndPause

        NeutralYes = 26;  // PtnNeutYes
        NeutralNo = 1;    // PtnNeutNo
        DirectionalYes = 27;  // PtnDirYes
        DirectionalNo = 8;    // PtnDirNo

        NotEntryDayLong = 0;   // SkipDayLong(0) — domenica
        NotEntryDayShort = 2;  // SkipDayShort(2) — martedì

        MaxDaysInTrade = 3;  // MaxDaysInTrade

        StopMoney = 1400;       // SL
        BreakEvenMoney = 1300;  // BE
        ProfitMoney = 2400;     // TP
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
