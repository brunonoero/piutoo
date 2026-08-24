using Piootoo.Shared.Models;
using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_181 — RBB_U su NQ a 30 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_181_NQ_30__7.txt</c>. Il recross della
/// banda inferiore apre long e quello verso il basso della banda superiore apre short, entrambi
/// a mercato sulla barra successiva. I quattro gate <c>PatternFast</c> sono indipendenti per
/// verso, quindi la variante è unmirrored.</para>
///
/// <para>Stop e target sono USD per contratto NQ e vengono dichiarati sul segnale d'ingresso;
/// nessuna uscita dipende da una rivalutazione successiva della strategia.</para>
/// </summary>
public sealed class Easy_181_NQ_30 : RbbUnmirroredEngine
{
    public override string Name => "Easy_181_NQ_30";
    public override string Description => "RBB_U Bollinger reversal, NQ 30m";
    public override string Symbol => "@NQ";
    public override int TimeframeMinutes => 30;

    public Easy_181_NQ_30()
    {
        SessionStartTime = 1700; // sessionStartTimeC
        SessionEndTime = 1600;   // sessionEndTimeC
        Contracts = 1;           // MyContracts
        FastYesLong = 152;       // MyPtnLY
        FastNoLong = 8;          // MyPtnLN
        FastYesShort = 100;      // MyPtnSY
        FastNoShort = 109;       // MyPtnSN
        StartTrade = 1800;       // MyStartTime
        EndTrade = 400;          // MyEndTime
        DayToFilter = -1;
        StopMoney = 3000;        // MyStop
        ProfitMoney = 6000;      // MyProfit
        BollingerLength = 20;    // Length
        BollingerNumDevs = 2m;   // NumDevs
    }

    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) =>
        EvaluateCore(data, currentDate);
}
