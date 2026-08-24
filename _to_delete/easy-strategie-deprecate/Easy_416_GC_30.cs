using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_416 — RBB_M su GC a 30 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_416_GC_30__7.txt</c>. L'originale entra
/// <c>next bar at market</c> sul cross delle bande; il motore emette invece limit sulla banda
/// valido solo per la barra successiva — stesso contratto Unger/Python di RBB_M. Il fill
/// richiede che la barra successiva penetri la banda, non basta il cross sulla barra di
/// segnale.</para>
///
/// <para><b>Uscite.</b> Stop dichiarato all'ingresso; uscita dopo <c>MaxDaysInTrade = 5</c>
/// sessioni tramite <c>MaxDaysInTrade</c> del motore.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $1.800.</para>
/// </summary>
public sealed class Easy_416_GC_30 : RbbMirroredEngine
{
    public override string Name => "Easy_416_GC_30";
    public override string Description => "RBB_M Bollinger reversal, GC 30m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 30;

    public Easy_416_GC_30()
    {
        SessionStartTime = 1800;  // sessionStartTimeC
        SessionEndTime = 1700;    // sessionEndTimeC
        Contracts = 1;

        NeutralYes = 16;      // PtnNeutYes
        NeutralNo = 48;       // PtnNeutNo
        DirectionalYes = -1;    // PtnDirYes — specchiato dal motore
        DirectionalNo = 37;     // PtnDirNo

        StartTrade = 1900;  // MyStartTime
        EndTrade = 200;     // MyEndTime
        DayToFilter = -1;   // DaytoFilter

        BollingerLength = 20;   // Length
        BollingerNumDevs = 2m;  // NumDevs

        IntradayOnly = false;

        StopMoney = 1800;       // MyStop
        ProfitMoney = 0;        // MyProfit
        MaxDaysInTrade = 5;     // MaxDaysInTrade
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
        if (parameters.TryGetValue("Mycontracts", out var contractsAlt))
            Contracts = Convert.ToInt32(contractsAlt);
    }
}
