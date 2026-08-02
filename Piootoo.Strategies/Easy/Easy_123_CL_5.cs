using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

// HYBRID: richiede data2 (120m) per l'incrocio Aroon; senza serie aggiuntiva resta in Hold.
// Il contratto di conformance non fornisce ancora AdditionalOhlcv — resta in NotYetMigrated.

/// <summary>
/// TOP_UA_123 — incrocio Aroon su data2 (120m), CL 5 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_123_CL_5____120__7.txt</c>. La logica
/// vive in <see cref="AroonCrossoverEngine"/>; qui restano solo gli <c>input</c>.</para>
///
/// <para><b>Contratto di riferimento:</b> CL, $1.000 per punto. Stop $2.100, target $3.000,
/// breakeven $1.100, uscita a tempo dopo 660 barre.</para>
/// </summary>
public sealed class Easy_123_CL_5 : AroonCrossoverEngine
{
    public override string Name => "Easy_123_CL_5";
    public override string Description => "Aroon crossover su data2 120m, CL 5m";
    public override string Symbol => "@CL";
    public override int TimeframeMinutes => 5;

    public Easy_123_CL_5()
    {
        SessionStartTime = 1800;  // SessionStartTimeA
        SessionEndTime = 1700;    // SessionEndTimeA
        Contracts = 1;

        AroonLength = 22;              // mylenght
        HigherTimeframeMinutes = 120;  // data2 dal nome sorgente

        StartTrade = 100;   // MyStarttime
        EndTrade = 1515;    // MyendTime

        BaseYesLong = 41;   // MyPtnLY
        BaseNoLong = 7;     // MyPtnLN
        BaseYesShort = 41;  // MyPtnSY
        BaseNoShort = 5;    // MyPtnSN

        StopMoney = 2100;       // MyStop
        ProfitMoney = 3000;     // MyProfit
        BreakEvenMoney = 1100;  // MyBreakEven
        MaxBars = 660;          // numbar
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, null, currentDate);

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
