using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BP_BRT_001_60</b> — BIAS_RT su BP 1h, codice della ricerca <c>BP-1H-BIASRT-8577be</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 2051 trade, netto $20,762; broker (27/08/2025 → 09/09/2026)
/// 159 trade, netto $1,195. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BP-1H-BIASRT-8577be.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ora fissa, poi ritracciamento.* Il rovescio del BIAS_BO: dall'ora scelta in poi aspetta che il prezzo torni indietro verso gli estremi recenti e compra li', a prezzo migliore.</para>
/// <para>Da sapere. Come il BIAS_BO: un ingresso per sessione per direzione, solo intraday.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $20,762 su 2051 trade · $10 a trade · drawdown $10,244 · netto/DD 2.03 · anni in perdita 4 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $-2,369 su 2051 trade, drawdown $15,855.</para>
/// <para><b>Ritracciamento dentro una finestra di barre della sessione</b></para>
/// <para>· LONG: limit buy sul minimo della barra precedente</para>
/// <para>· SHORT: limit sell sul massimo delle 3 barre precedenti</para>
/// <para>· Il fill richiede penetrazione stretta del livello: il tocco non basta.</para>
/// <para>· L'ordine LONG esiste solo dalla barra 1 (inclusa) alla barra 7 (esclusa) della sessione; lo SHORT dalla 1 alla 7.</para>
/// <para>· La finestra si arma alla sua barra di partenza, e solo se i filtri pattern sono veri in quel preciso momento. Una volta armata resta attiva fino a fine finestra, anche se i pattern smettono di essere veri.</para>
/// <para>· Se la barra di partenza è maggiore di quella di fine, la finestra attraversa il cambio di sessione.</para>
/// <para>· Gli estremi rolling si leggono su barre già chiuse.</para>
/// <para>· Le barre della sessione si contano da 0: la prima barra dopo l'inizio sessione è la numero 0.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· DIREZIONE SPENTA: 'questo lato non opera mai'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Non apre posizioni SHORT di mercoledì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, Europe/Rome), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para><b>Uscite</b></para>
/// <para>· Uscita obbligatoria alla barra 3 della sessione per il LONG e alla barra 14 per lo SHORT, market all'apertura di quella barra.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: 0.90 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.70 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_BP_BRT_001_60 : Pt5DavBiasRetracementEngine
{
    public override string Name => "PT5DAV_BP_BRT_001_60";

    public override string Description => "BIAS_RT BP 1h, ricerca PT5DAV BP-1H-BIASRT-8577be";

    public override string Symbol => "@BP";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "BP-1H-BIASRT-8577be";

    public PT5DAV_BP_BRT_001_60()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        ArmBarLong = 1;                         // le_bar
        ExitBarLong = 3;                        // lx_bar
        EndLong = 7;                            // end_long
        ArmBarShort = 1;                        // se_bar
        ExitBarShort = 14;                      // sx_bar
        EndShort = 7;                           // end_short
        BreakoutBarsHigh = 3;                   // nhigh
        BreakoutBarsLow = 1;                    // nlow
        PatternLongYes = 153;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = 2;                   // not_se_day, pandas
        StopAtr = 0.9m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1.7m;                       // take_profit_atr
    }
}
