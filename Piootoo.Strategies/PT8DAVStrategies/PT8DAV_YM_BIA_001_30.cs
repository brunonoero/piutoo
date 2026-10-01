using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_YM_BIA_001_30</b> — BIAS su YM 30m, codice della ricerca <c>YM-30M-BIAS-1187a8</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>market</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>del motore</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 3408 trade, netto 331,426; fuori campione 06/2022 → 05/2025 netto
/// 37,328; broker (16/09/2025 → 09/09/2026) 251 trade, netto 36,242.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/YM-30M-BIAS-1187a8.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Entrata a un'ora fissa della sessione.* Non guarda i prezzi per decidere quando entrare: entra a una barra prefissata della sessione e esce a un'altra barra prefissata. L'idea e' che certe ore della giornata abbiano una direzione ricorrente.</para>
/// <para>Da sapere. Le barre si contano dall'inizio della sessione, non in ore: la stessa strategia su due timeframe diversi non e' la stessa ora. L'uscita e' forzata alla barra scelta, e comunque a fine sessione. Un lato si spegne mettendo il suo filtro su "sempre falso".</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $331,426 su 3408 trade · $97 a trade · drawdown $58,524 · netto/DD 5.66 · anni in perdita 2 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $93,148 su 3408 trade, drawdown $37,816.</para>
/// <para><b>Entrata a barra fissa della sessione</b></para>
/// <para>· LONG: MARKET all'apertura della barra 9 della sessione</para>
/// <para>· SHORT: MARKET all'apertura della barra 2 della sessione</para>
/// <para>· Le barre della sessione si contano da 0: la prima barra dopo l'inizio sessione è la numero 0.</para>
/// <para>· I filtri pattern si valutano alla chiusura della barra precedente a quella di entrata.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo SHORT</para>
/// <para>· DIREZIONE SPENTA: 'questo lato non opera mai'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para><b>Uscite</b></para>
/// <para>· Uscita obbligatoria alla barra 45 della sessione per il LONG e alla barra 7 per lo SHORT, market all'apertura di quella barra.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: nessuno — la posizione si chiude solo con le uscite qui sopra</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT8DAV_YM_BIA_001_30 : Pt5DavBiasMarketEngine
{
    public override string Name => "PT8DAV_YM_BIA_001_30";

    public override string Description => "BIAS YM 30m, ricerca v5.1 YM-30M-BIAS-1187a8";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "YM-30M-BIAS-1187a8";

    public PT8DAV_YM_BIA_001_30()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        ArmBarLong = 9;                         // le_bar
        ExitBarLong = 45;                       // lx_bar
        EndLong = 45;                           // end_long
        ArmBarShort = 2;                        // se_bar
        ExitBarShort = 7;                       // sx_bar
        EndShort = 45;                          // end_short
        BreakoutBarsHigh = 3;                   // nhigh
        BreakoutBarsLow = 1;                    // nlow
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 153;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        StopAtr = 0m;                           // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
