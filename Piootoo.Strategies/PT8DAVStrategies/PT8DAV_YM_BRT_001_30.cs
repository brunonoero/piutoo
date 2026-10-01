using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT8DAVStrategies;

/// <summary>
/// <b>PT8DAV_YM_BRT_001_30</b> — BIAS_RT su YM 30m, codice della ricerca <c>YM-30M-BIASRT-fb5c99</c>
/// (consegna v5.1 del 25/09/2026, <c>piootoo-repository/run-engine-v3/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_111.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>, la stessa base della serie
/// PT5DAV. Ordine <b>limit</b>, entrate: <b>una per sessione e per direzione</b>, tenuta: <b>del motore</b>.
/// Numeri della ricerca sul CFD, 1 future: storia (2012 → 05/2025, parametri scelti fino al
/// 31/05/2022) 2992 trade, netto 326,737; fuori campione 06/2022 → 05/2025 netto
/// 36,892; broker (16/09/2025 → 09/09/2026) 224 trade, netto 45,700.
/// Trade di riferimento: <c>run-engine-v3/trades_per_strategia/YM-30M-BIASRT-fb5c99.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_111.md</c>):</para>
/// <para>*Ora fissa, poi ritracciamento.* Il rovescio del BIAS_BO: dall'ora scelta in poi aspetta che il prezzo torni indietro verso gli estremi recenti e compra li', a prezzo migliore.</para>
/// <para>Da sapere. Come il BIAS_BO: un ingresso per sessione per direzione, solo intraday.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $326,737 su 2992 trade · $109 a trade · drawdown $55,101 · netto/DD 5.93 · anni in perdita 1 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $94,577 su 2992 trade, drawdown $31,372.</para>
/// <para><b>Ritracciamento dentro una finestra di barre della sessione</b></para>
/// <para>· LONG: limit buy sul minimo della barra precedente</para>
/// <para>· SHORT: limit sell sul massimo della barra precedente</para>
/// <para>· Il fill richiede penetrazione stretta del livello: il tocco non basta.</para>
/// <para>· L'ordine LONG esiste solo dalla barra 9 (inclusa) alla barra 14 (esclusa) della sessione; lo SHORT dalla 2 alla 14.</para>
/// <para>· La finestra si arma alla sua barra di partenza, e solo se i filtri pattern sono veri in quel preciso momento. Una volta armata resta attiva fino a fine finestra, anche se i pattern smettono di essere veri.</para>
/// <para>· Se la barra di partenza è maggiore di quella di fine, la finestra attraversa il cambio di sessione.</para>
/// <para>· Gli estremi rolling si leggono su barre già chiuse.</para>
/// <para>· Le barre della sessione si contano da 0: la prima barra dopo l'inizio sessione è la numero 0.</para>
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
public sealed class PT8DAV_YM_BRT_001_30 : Pt5DavBiasRetracementEngine
{
    public override string Name => "PT8DAV_YM_BRT_001_30";

    public override string Description => "BIAS_RT YM 30m, ricerca v5.1 YM-30M-BIASRT-fb5c99";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "YM-30M-BIASRT-fb5c99";

    public PT8DAV_YM_BRT_001_30()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        ArmBarLong = 9;                         // le_bar
        ExitBarLong = 45;                       // lx_bar
        EndLong = 14;                           // end_long
        ArmBarShort = 2;                        // se_bar
        ExitBarShort = 7;                       // sx_bar
        EndShort = 14;                          // end_short
        BreakoutBarsHigh = 1;                   // nhigh
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
