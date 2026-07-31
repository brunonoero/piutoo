using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_102 — rottura degli estremi della sessione in corso, FDAX 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_102_FDAX_5__7.txt</c>.</para>
///
/// <para><b>Sessione a giornata singola:</b> 08:00–22:00, con inizio minore della fine. È
/// l'unica del gruppo GC/CL a non attraversare la mezzanotte, e <c>_OHLCMulti5</c> ne cambia il
/// ramo di calcolo dei confini.</para>
///
/// <para><b>Gate su PatternFast per verso</b> invece della coppia neutro/direzionale: quattro
/// pattern indipendenti, due per il long e due per lo short.</para>
///
/// <para><b>Uscita a orario fisso:</b> <c>Texit = 2145</c>, espressa come <c>CloseAtUtc</c>
/// sull'ingresso.</para>
///
/// <para><b>Contratto di riferimento:</b> FDAX, €25 per punto — valore in EUR, che il sistema non
/// converte in USD. Stop €2.000 = 80 punti, target €3.750 = 150 punti.</para>
/// </summary>
public sealed class Easy_102_FDAX_5 : TrendDeveloperEngine
{
    public override string Name => "Easy_102_FDAX_5";
    public override string Description => "Rottura estremi sessione corrente, FDAX 5m";
    public override string Symbol => "@FDAX";
    public override int TimeframeMinutes => 5;

    public Easy_102_FDAX_5()
    {
        SessionStartTime = 800;   // sessionStartTimeC
        SessionEndTime = 2200;    // sessionEndTimeC
        Contracts = 1;

        Trigger = TrendTrigger.CurrentSessionOhlc;  // highd0 / lowd0

        StartTrade = 1100;  // MyStartTrade
        EndTrade = 1700;    // MyEndTrade
        PauseStart = 1200;  // coppia invertita: pausa inattiva
        PauseEnd = 1100;

        // Questa variante non usa i gate neutro/direzionale: le sentinelle li disattivano.
        NeutralYes = 55;
        NeutralNo = 56;
        DirectionalYes = 52;
        DirectionalNo = 53;

        FastYesLong = 4;     // MyPtnLY
        FastNoLong = 73;     // MyPtnLN
        FastYesShort = 106;  // MyPtnSY
        FastNoShort = 38;    // MyPtnSN

        CloseAtTime = 2145;  // Texit

        StopMoney = 2000;    // MyStop, euro per contratto
        ProfitMoney = 3750;  // MyProfit
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
