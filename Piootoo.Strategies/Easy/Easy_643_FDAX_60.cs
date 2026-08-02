using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_643 — breakout stop su HighRange/LowRange, FDAX 60 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_643_FDAX_60__7.txt</c>. Variante legacy
/// di <see cref="VolatilityBreakoutEngine"/> con ingresso stop sugli estremi delle ultime
/// <c>MyLenght</c> barre.</para>
///
/// <para><b>Finestra.</b> L'originale arma gli ordini solo alla barra delle 15:00; qui
/// <c>StartTrade = EndTrade = 1500</c> con fine esclusiva restringe l'operatività a quella
/// singola barra oraria.</para>
///
/// <para><b>Uscite.</b> Stop, breakeven short e limite barre long sono dichiarati all'ingresso.
/// Il conteggio barre short (34) differisce da quello long (50): si usa il massimo.</para>
///
/// <para><b>Contratto di riferimento:</b> FDAX, €25 per punto. Stop long €3.000, short €3.400,
/// target short €4.700, breakeven short €3.900.</para>
/// </summary>
public sealed class Easy_643_FDAX_60 : VolatilityBreakoutEngine
{
    public override string Name => "Easy_643_FDAX_60";
    public override string Description => "Breakout HighRange/LowRange, FDAX 60m";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 60;

    public override int RequiredCandles => Math.Max(SessionsToCandles(6), RangeBars + 2);

    public Easy_643_FDAX_60()
    {
        UseLegacyVariant = true;
        SessionStartTime = 800;   // sessionStartTimeA
        SessionEndTime = 2159;    // sessionEndTimeA
        Contracts = 1;            // MySize

        EntryOrderType = Shared.Enums.TradeOrderType.Stop;
        EntryLevel = VolatilityBreakoutLevel.RecentBarExtremes;
        RangeBars = 4;  // MyLenght

        StartTrade = 1500;  // MybeginTime — barra unica
        EndTrade = 1501;

        MaxEntriesPerSession = 1;  // EntriesToday(date) = 0

        NeutralYes = 16;  // PtnNeutYes
        NeutralNo = 10;   // PtnNeutNo

        BaseYesLong = 39;   // MyPtnLY
        BaseNoLong = 40;    // MyPtnLN
        BaseYesShort = 41;  // MyPtnSY
        BaseNoShort = 33;   // MyPtnSN

        UpRangeFactor = 0.1m;    // MultUp
        DownRangeFactor = 0.1m;  // MultDwn
        NotEntryDayShort = 1;    // NoDayShort(1) — lunedì

        StopMoney = 3400;       // max(MyStopL, MyStopS)
        BreakEvenMoney = 3900;  // MyBEShort
        ProfitMoney = 4700;     // MyProfitS
        MaxBars = 50;           // BarsL (BarsS = 34)
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
