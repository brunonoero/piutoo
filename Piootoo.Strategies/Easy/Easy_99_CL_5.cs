using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_99 — "weekly bias" short della domenica sera, CL 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_99_CL_5__7.txt</c>. È la strategia più
/// specifica del catalogo: opera <b>solo short</b>, <b>solo di domenica</b>
/// (<c>dayofweek(d) = 0</c>), in una finestra di quindici minuti fra le 19:45 e le 20:00, con un
/// unico ingresso di giornata.</para>
///
/// <para><b>Uscita.</b> L'originale chiude quando <i>non</i> è più domenica e l'orario cade fra
/// 09:45 e 10:30 — cioè la mattina del lunedì successivo. Poiché l'ingresso è vincolato alla
/// domenica sera, la deadline è deterministica e diventa <c>CloseAtUtc</c> alle 09:45 del giorno
/// seguente.</para>
///
/// <para><b>Contratto di riferimento:</b> CL, 1.000 barili, $1.000 per punto. Stop $1.600 = 1,6
/// punti, target $2.100 = 2,1 punti.</para>
/// </summary>
public sealed class Easy_99_CL_5 : TrendDeveloperEngine
{
    public override string Name => "Easy_99_CL_5";
    public override string Description => "Weekly bias short della domenica sera, CL 5m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 5;

    public Easy_99_CL_5()
    {
        SessionStartTime = 1800;  // sessionStartTimeA
        SessionEndTime = 1700;    // sessionEndTimeA
        Contracts = 1;

        MarketEntry = true;   // next bar market
        EnableLong = false;   // la strategia è solo short
        EnableShort = true;
        OnlyEntryDayShort = 0;  // ConditionShort1 = dayofweek(d) = 0 → domenica

        StartTrade = 1945;           // EntryShort1Beg
        EndTrade = 2000;             // EntryShort1End
        InclusiveWindowEnd = true;   // l'originale usa Time >= Beg and Time <= End
        MaxTradesPerDay = 1;         // entriestoday(d) = 0

        // Nessun gate neutro/direzionale: sentinelle.
        NeutralYes = 55;
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        FastYesShort = 100;  // PtnFastShortYes1
        FastNoShort = 36;    // PtnFastShortNo1
        // PtnFastShortNo2(1) è un secondo divieto: non essendoci uno slot dedicato, resta
        // documentato qui come scostamento noto dall'originale.

        CloseAtTime = 945;  // ExitShort1Beg, il mattino successivo

        StopMoney = 1600;    // MyStop
        ProfitMoney = 2100;  // MyProfit
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
