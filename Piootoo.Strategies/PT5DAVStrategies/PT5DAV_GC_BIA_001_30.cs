using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_GC_BIA_001_30</b> — BIAS su GC 30m, codice della ricerca <c>GC-30M-BIAS-9d8a5e</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 3404 trade, netto $327,516; broker (27/08/2025 → 09/09/2026)
/// 250 trade, netto $75,977. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/GC-30M-BIAS-9d8a5e.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Entrata a un'ora fissa della sessione.* Non guarda i prezzi per decidere quando entrare: entra a una barra prefissata della sessione e esce a un'altra barra prefissata. L'idea e' che certe ore della giornata abbiano una direzione ricorrente.</para>
/// <para>Da sapere. Le barre si contano dall'inizio della sessione, non in ore: la stessa strategia su due timeframe diversi non e' la stessa ora. L'uscita e' forzata alla barra scelta, e comunque a fine sessione. Un lato si spegne mettendo il suo filtro su "sempre falso".</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $327,516 su 3404 trade · $96 a trade · drawdown $130,349 · netto/DD 2.51 · anni in perdita 4 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $98,032 su 3404 trade, drawdown $66,346.</para>
/// <para><b>Entrata a barra fissa della sessione</b></para>
/// <para>· LONG: MARKET all'apertura della barra 2 della sessione</para>
/// <para>· SHORT: MARKET all'apertura della barra 9 della sessione</para>
/// <para>· Le barre della sessione si contano da 0: la prima barra dopo l'inizio sessione è la numero 0.</para>
/// <para>· I filtri pattern si valutano alla chiusura della barra precedente a quella di entrata.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· DIREZIONE SPENTA: 'questo lato non opera mai'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para><b>Uscite</b></para>
/// <para>· Uscita obbligatoria alla barra 7 della sessione per il LONG e alla barra 41 per lo SHORT, market all'apertura di quella barra.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_GC_BIA_001_30 : Pt5DavBiasMarketEngine
{
    public override string Name => "PT5DAV_GC_BIA_001_30";

    public override string Description => "BIAS GC 30m, ricerca PT5DAV GC-30M-BIAS-9d8a5e";

    public override string Symbol => "@GC";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "GC-30M-BIAS-9d8a5e";

    public PT5DAV_GC_BIA_001_30()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        ArmBarLong = 2;                         // le_bar
        ExitBarLong = 7;                        // lx_bar
        EndLong = 45;                           // end_long
        ArmBarShort = 9;                        // se_bar
        ExitBarShort = 41;                      // sx_bar
        EndShort = 45;                          // end_short
        BreakoutBarsHigh = 3;                   // nhigh
        BreakoutBarsLow = 1;                    // nlow
        PatternLongYes = 153;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
