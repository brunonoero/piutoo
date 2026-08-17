using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_695 — rottura degli estremi della sessione precedente, GC 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_695_GC_5__7.txt</c>.</para>
///
/// <para><b>Pausa disattivata.</b> L'originale passa <c>MyStartPause = 1200</c> e
/// <c>MyEndPause = 1100</c>: con inizio maggiore della fine, la condizione
/// <c>t &lt; 1200 or t &gt; 1100</c> è sempre vera e la pausa non filtra nulla. È riprodotto
/// così com'è, perché è il modo in cui quella strategia la spegne.</para>
///
/// <para><b>Uscita.</b> <c>ID = 0</c> attiva <c>setexitonclose</c>: chiusura alla fine della
/// sessione, qui espressa come <c>CloseAtUtc</c> sull'ingresso.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $1.800 = 18 punti. Nessun
/// target.</para>
/// </summary>
public sealed class Easy_695_GC_5 : TrendDeveloperEngine
{
    public override string Name => "Easy_695_GC_5";
    public override string Description => "Rottura estremi sessione precedente, GC 5m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 5;

    public Easy_695_GC_5()
    {
        SessionStartTime = 1800;  // sessionStartTimeC
        SessionEndTime = 1700;    // sessionEndTimeC
        Contracts = 1;

        Trigger = TrendTrigger.PreviousSessionOhlc;  // highd1 / lowd1

        StartTrade = 0;     // MyStartTrade
        EndTrade = 1500;    // MyEndTrade
        PauseStart = 1200;  // MyStartPause — coppia invertita: pausa inattiva
        PauseEnd = 1100;    // MyEndPause

        NeutralYes = 3;        // PtnNeutYes
        NeutralNo = 35;        // PtnNeutNo
        DirectionalYes = -27;  // PtnDirYes — negativo: i due versi si scambiano
        DirectionalNo = 8;     // PtnDirNo

        CloseAtTime = SessionEndTime;  // ID = 0 → setexitonclose

        StopMoney = 1800;  // MyStop
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
