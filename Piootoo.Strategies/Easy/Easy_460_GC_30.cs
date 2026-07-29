using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_460 — BIAS a conteggio barre con ingresso a mercato, su GC a 30 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_460_GC_30__7.txt</c>.</para>
///
/// <para><b>Conteggio barre sfalsato.</b> A differenza delle sorelle, questa variante azzera con
/// <c>mycount = 1</c> e incrementa subito dopo: sulla prima barra di sessione <c>mycount</c> vale
/// 2, non 1. Tutti i suoi indici sono quindi spostati di uno, ed è riprodotto qui con
/// <c>BarCountStartsAt = 2</c>. Trattarla come le altre sposterebbe ogni ingresso e ogni uscita
/// di una barra.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $1.700 = 17 punti,
/// target $4.000 = 40 punti.</para>
/// </summary>
public sealed class Easy_460_GC_30 : BiasBarCountEngine
{
    public override string Name => "Easy_460_GC_30";
    public override string Description => "BIAS bar-count + market entry, GC 30m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 30;

    public Easy_460_GC_30()
    {
        SessionStartTime = 1800;
        SessionEndTime = 1700;
        Contracts = 1;
        BarCountStartsAt = 2;

        ArmBarLong = 36;    // MyLEbar
        ArmBarShort = 10;   // MySEbar
        ExitBarLong = 10;   // MyLXbar
        ExitBarShort = 36;  // MySXbar

        PatternLibrary = EasyPatternLibrary.BaseSA;
        PatternLongYes = 18;   // MyPtnLY
        PatternLongNo = 10;    // MyPtnLN
        PatternShortYes = 25;  // MyPtnSY
        PatternShortNo = 28;   // MyPtnSN

        NotEntryDayLong = 4;   // MyNotLEDay
        NotEntryDayShort = 4;  // MyNotSEDay

        EntryType = BiasEntryType.MarketOnArmBar;

        StopMoney = 1700;    // MyStop
        ProfitMoney = 4000;  // MyProfit
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
