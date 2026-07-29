using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_291 — "UA Trend Developer" su GC a 15 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_291_GC_15__7.txt</c>. Rottura del massimo
/// (o del minimo) della sessione in corso, dentro una finestra oraria stretta, con gate di
/// pattern neutri e direzionali e un tetto di quattro ingressi al giorno.</para>
///
/// <para><b>Uscite.</b> <c>ID = 1</c> disattiva l'intero blocco di chiusura di fine sessione
/// dell'originale, quindi restano stop, target e il limite di <c>DStop = 432</c> barre —
/// che qui diventa <c>MaxBarsInPosition</c> sull'ingresso invece che un segnale a runtime. Tutto
/// dichiarabile all'ingresso: la strategia non è close-dependent.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $2.300 = 23 punti,
/// target $5.000 = 50 punti.</para>
/// </summary>
public sealed class Easy_291_GC_15 : TrendDeveloperEngine
{
    public override string Name => "Easy_291_GC_15";
    public override string Description => "Trend Developer, rottura estremi sessione, GC 15m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 15;

    public Easy_291_GC_15()
    {
        SessionStartTime = 1800;  // sessionStartTimeA
        SessionEndTime = 1700;    // sessionEndTimeA
        Contracts = 1;

        Trigger = TrendTrigger.CurrentSessionOhlc;  // MyTrigger = 1 → highd0 / lowd0

        StartTrade = 0;             // MyStartTrade
        EndTrade = 400;             // MyEndTrade
        InclusiveWindowEnd = false; // l'originale usa tw(), che esclude la fine
        MaxTradesPerDay = 4;        // MaxTradesPerDay

        NeutralYes = 16;      // PtnNeutYes
        NeutralNo = 30;       // PtnNeutNo
        DirectionalYes = 48;  // PtnDirYes
        DirectionalNo = 13;   // PtnDirNo

        StopMoney = 2300;   // MyStop
        ProfitMoney = 5000; // MyProfit
        MaxBars = 432;      // DStop
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
