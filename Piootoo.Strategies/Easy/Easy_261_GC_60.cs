using Piootoo.Strategies.Easy.Engines;

namespace Piootoo.Strategies.Easy;

/// <summary>
/// TOP_UA_261 — BIAS a conteggio barre con ingresso a mercato, su GC a 60 minuti.
///
/// <para>Sorgente: <c>piootoo-repository/easy/s_TOP_UA_261_GC_60__7.txt</c>. È la variante più
/// semplice del motore: nessuna finestra di ingresso, l'armamento e l'ingresso coincidono sulla
/// barra prevista (<c>entrytype = 1</c>).</para>
///
/// <para><b>Sessione.</b> L'originale non ha input di sessione: azzera il contatore su
/// <c>sessionlastbar[1]</c>, cioè si affida alla definizione di sessione del grafico
/// TradeStation. Qui va dichiarata esplicitamente, e si usa la sessione GC 18:00–17:00, la
/// stessa delle altre strategie GC del workspace. La traduzione precedente usava 08:00–22:00,
/// un valore che non compare né nell'originale né nelle strategie sorelle: con quello il
/// conteggio barre partiva da un momento diverso e ogni indice — quindi ogni ingresso e ogni
/// uscita — cadeva su un'altra barra.</para>
///
/// <para><b>Pattern.</b> L'originale chiama <c>PtnBaseSA</c>, che legge gli OHLC di sessione dal
/// grafico. Le 43 formule sono identiche a <c>PtnBaseSA2</c>, che li riceve dall'array di
/// <c>_OHLCMulti5</c>: la resa è equivalente una volta fissata la sessione corretta.</para>
///
/// <para><b>Contratto di riferimento:</b> GC, $100 per punto. Stop $1.500 = 15 punti. Nessun
/// target (<c>MyProfit = 0</c>): la posizione esce solo per stop o per uscita a tempo.</para>
/// </summary>
public sealed class Easy_261_GC_60 : BiasBarCountEngine
{
    public override string Name => "Easy_261_GC_60";
    public override string Description => "BIAS bar-count + market entry, GC 60m";
    public override string Symbol => "@GC";
    public override int TimeframeMinutes => 60;

    public Easy_261_GC_60()
    {
        SessionStartTime = 1800;
        SessionEndTime = 1700;
        Contracts = 1;

        ArmBarLong = 23;    // MyLEbar
        ArmBarShort = 9;    // MySEbar
        ExitBarLong = 5;    // MyLXbar — barra 5 della sessione successiva
        ExitBarShort = 14;  // MySXbar

        PatternLibrary = EasyPatternLibrary.BaseSA;
        PatternLongYes = 5;   // MyPtnLY — (highd0-opend0) > (highd1-opend1) * 1.5
        PatternLongNo = 1;    // MyPtnLN — |opend1-closed1| < 0.5 * range1d
        PatternShortYes = 25; // MyPtnSY
        PatternShortNo = 4;   // MyPtnSN — (highd0-opend0) > (highd1-opend1)

        NotEntryDayLong = 4;   // MyNotLEDay — giovedì (0 = domenica)
        NotEntryDayShort = 4;  // MyNotSEDay

        EntryType = BiasEntryType.MarketOnArmBar;  // entrytype implicito nell'originale

        StopMoney = 1500;  // MyStop, dollari per contratto GC
        ProfitMoney = 0;   // MyProfit = 0 → nessun target
    }

    public void Initialize(Dictionary<string, object>? parameters = null)
    {
        if (parameters is null) return;
        if (parameters.TryGetValue("Contracts", out var contracts))
            Contracts = Convert.ToInt32(contracts);
    }
}
