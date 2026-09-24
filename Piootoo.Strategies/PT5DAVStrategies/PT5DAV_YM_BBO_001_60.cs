using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_YM_BBO_001_60</b> — BIAS_BO su YM 1h, codice della ricerca <c>YM-1H-BIASBO-e5a983</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 1536 trade, netto $137,883; broker (27/08/2025 → 09/09/2026)
/// 128 trade, netto $12,080. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/YM-1H-BIASBO-e5a983.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ora fissa, poi rottura.* Come il BIAS decide l'ora, ma non entra subito: da quell'ora in poi mette un ordine sulla rottura degli estremi recenti, e entra solo se il prezzo si muove.</para>
/// <para>Da sapere. Al massimo un ingresso per sessione per direzione: dopo il fill l'ordine non si riarma. Solo intraday.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $137,883 su 1536 trade · $90 a trade · drawdown $37,361 · netto/DD 3.69 · anni in perdita 3 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $32,456 su 1536 trade, drawdown $30,320.</para>
/// <para><b>Breakout dentro una finestra di barre della sessione</b></para>
/// <para>· LONG: stop buy sul massimo delle 3 barre precedenti</para>
/// <para>· SHORT: stop sell sul minimo della barra precedente</para>
/// <para>· L'ordine LONG esiste solo dalla barra 10 (inclusa) alla barra 12 (esclusa) della sessione; lo SHORT dalla 1 alla 7.</para>
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
/// <para>· Uscita obbligatoria alla barra 3 della sessione per il LONG e alla barra 3 per lo SHORT, market all'apertura di quella barra.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.30 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_YM_BBO_001_60 : Pt5DavBiasBreakoutEngine
{
    public override string Name => "PT5DAV_YM_BBO_001_60";

    public override string Description => "BIAS_BO YM 1h, ricerca PT5DAV YM-1H-BIASBO-e5a983";

    public override string Symbol => "@YM";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "YM-1H-BIASBO-e5a983";

    public PT5DAV_YM_BBO_001_60()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        ArmBarLong = 10;                        // le_bar
        ExitBarLong = 3;                        // lx_bar
        EndLong = 12;                           // end_long
        ArmBarShort = 1;                        // se_bar
        ExitBarShort = 3;                       // sx_bar
        EndShort = 7;                           // end_short
        BreakoutBarsHigh = 3;                   // nhigh
        BreakoutBarsLow = 1;                    // nlow
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 153;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        NotEntryDayLong = -1;                   // not_le_day, pandas
        NotEntryDayShort = -1;                  // not_se_day, pandas
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1.3m;                       // take_profit_atr
    }
}
