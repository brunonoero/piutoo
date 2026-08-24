using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_960 — BIAS a conteggio barre con ingresso a mercato, su GC a 60 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_960_GC_60__7.txt</c>. Come la 261,
/// l'originale non ha input di sessione e si affida alla definizione del grafico TradeStation
/// (<c>sessionlastbar</c>); qui si usa la sessione GC 18:00–17:00 delle strategie sorelle.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $3.000 = 30 punti. Nessun
/// target (<c>MyProfit = 0</c>): si esce per stop o per uscita a tempo.</para>
/// </summary>
public sealed class Easy_960_GC_60 : BiasBarCountEngine
{
    public override string Name => "Easy_960_GC_60";
    public override string Description => "BIAS bar-count + market entry, GC 60m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public Easy_960_GC_60()
    {
        SessionStartTime = 1800;
        SessionEndTime = 1700;
        Contracts = 1;

        ArmBarLong = 21;    // MyLEbar
        ArmBarShort = 8;    // MySEbar
        ExitBarLong = 8;    // MyLXbar
        ExitBarShort = 16;  // MySXbar

        PatternLibrary = EasyPatternLibrary.BaseSA;
        PatternLongYes = 31;   // MyPtnLY
        PatternLongNo = 34;    // MyPtnLN
        PatternShortYes = 14;  // MyPtnSY
        PatternShortNo = 30;   // MyPtnSN

        NotEntryDayLong = 3;   // MyNotLEDay
        NotEntryDayShort = 0;  // MyNotSEDay — domenica

        EntryType = BiasEntryType.MarketOnArmBar;

        StopMoney = 3000;  // MyStop
        ProfitMoney = 0;   // MyProfit = 0
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
