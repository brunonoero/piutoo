using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_152 — Trend Developer su rottura highd0/lowd0, NQ 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_152_NQ_5__7.txt</c>. Gate
/// <c>PtnBaseSA2</c> per verso, finestra inclusiva 09:30–15:30 e chiusura forzata alle
/// 15:55 (<c>CloseAtTime</c>) perché <c>ID = 0</c> e <c>MyTrigger = 1</c>.</para>
///
/// <para><b>Uscite.</b> Stop dichiarato all'ingresso; nessun target (<c>MyProfit = 0</c>).
/// La chiusura alle 15:55 è <c>CloseAtUtc</c> sul segnale d'ingresso, non un segnale a
/// runtime — la strategia non è close-dependent.</para>
///
/// <para><b>Contratto di riferimento:</b> NQ, $20 per punto. Stop $1.200 = 60 punti.</para>
/// </summary>
public sealed class Easy_152_NQ_5 : TrendDeveloperEngine
{
    public override string Name => "Easy_152_NQ_5";
    public override string Description => "Trend Developer, rottura estremi sessione, NQ 5m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 5;

    public Easy_152_NQ_5()
    {
        SessionStartTime = 1700;  // sessionStartTimeA
        SessionEndTime = 1559;    // sessionEndTimeA
        Contracts = 1;

        Trigger = TrendTrigger.CurrentSessionOhlc;  // MyTrigger = 1

        StartTrade = 930;            // MyStartTrade
        EndTrade = 1530;             // MyEndTrade
        InclusiveWindowEnd = true;   // l'originale usa fine inclusiva su 930–1530
        MaxTradesPerDay = 3;

        NeutralYes = 3;       // PtnNeutYes
        NeutralNo = 23;       // PtnNeutNo
        DirectionalYes = 52;  // PtnDirYes

        BaseYesLong = 12;   // PtnLY
        BaseNoLong = 40;    // PtnLN
        BaseYesShort = 12;  // PtnSY
        BaseNoShort = 40;   // PtnSN

        NotEntryDayLong = 2;   // mydayNolong — martedì
        NotEntryDayShort = 2;  // mydayNoshort

        CloseAtTime = 1555;  // ID = 0, MyTrigger >= 1

        StopMoney = 1200;  // MyStop
        ProfitMoney = 0;   // MyProfit
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
