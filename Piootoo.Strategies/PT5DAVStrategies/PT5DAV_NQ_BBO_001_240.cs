using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_NQ_BBO_001_240</b> — BIAS_BO su NQ 4h, codice della ricerca <c>NQ-4H-BIASBO-860a60</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 2511 trade, netto $599,615; broker (27/08/2025 → 09/09/2026)
/// 183 trade, netto $-108,046. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/NQ-4H-BIASBO-860a60.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Ora fissa, poi rottura.* Come il BIAS decide l'ora, ma non entra subito: da quell'ora in poi mette un ordine sulla rottura degli estremi recenti, e entra solo se il prezzo si muove.</para>
/// <para>Da sapere. Al massimo un ingresso per sessione per direzione: dopo il fill l'ordine non si riarma. Solo intraday.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $599,615 su 2511 trade · $239 a trade · drawdown $139,504 · netto/DD 4.30 · anni in perdita 2 su 14 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $185,912 su 2511 trade, drawdown $65,132.</para>
/// <para><b>Breakout dentro una finestra di barre della sessione</b></para>
/// <para>· LONG: stop buy sul massimo della barra precedente</para>
/// <para>· SHORT: stop sell sul minimo della barra precedente</para>
/// <para>· L'ordine LONG esiste solo dalla barra 1 (inclusa) alla barra 2 (esclusa) della sessione; lo SHORT dalla 1 alla 2.</para>
/// <para>· La finestra si arma alla sua barra di partenza, e solo se i filtri pattern sono veri in quel preciso momento. Una volta armata resta attiva fino a fine finestra, anche se i pattern smettono di essere veri.</para>
/// <para>· Se la barra di partenza è maggiore di quella di fine, la finestra attraversa il cambio di sessione.</para>
/// <para>· Gli estremi rolling si leggono su barre già chiuse.</para>
/// <para>· Le barre della sessione si contano da 0: la prima barra dopo l'inizio sessione è la numero 0.</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Non apre posizioni LONG di mercoledì</para>
/// <para>· Non apre posizioni SHORT di mercoledì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para><b>Uscite</b></para>
/// <para>· Uscita obbligatoria alla barra 6 della sessione per il LONG e alla barra 2 per lo SHORT, market all'apertura di quella barra.</para>
/// <para>· È l'uscita principale del motore: stop e target qui sotto agiscono solo se scattano prima.</para>
/// <para>· Stop loss: 0.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 1.70 × ATR50 dal prezzo d'ingresso</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_NQ_BBO_001_240 : Pt5DavBiasBreakoutEngine
{
    public override string Name => "PT5DAV_NQ_BBO_001_240";

    public override string Description => "BIAS_BO NQ 4h, ricerca PT5DAV NQ-4H-BIASBO-860a60";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 240;

    public override string ResearchCode => "NQ-4H-BIASBO-860a60";

    public PT5DAV_NQ_BBO_001_240()
    {
        TradingWindow = ResearchWindow(-1, -1); // nessun filtro orario (-1/-1)
        ArmBarLong = 1;                         // le_bar
        ExitBarLong = 6;                        // lx_bar
        EndLong = 2;                            // end_long
        ArmBarShort = 1;                        // se_bar
        ExitBarShort = 2;                       // sx_bar
        EndShort = 2;                           // end_short
        BreakoutBarsHigh = 1;                   // nhigh
        BreakoutBarsLow = 1;                    // nlow
        PatternLongYes = 152;                   // ptn_ly_yes
        PatternLongNo = 153;                    // ptn_ly_no
        PatternShortYes = 152;                  // ptn_sy_yes
        PatternShortNo = 153;                   // ptn_sy_no
        NotEntryDayLong = 2;                    // not_le_day, pandas
        NotEntryDayShort = 2;                   // not_se_day, pandas
        StopAtr = 0.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 1.7m;                       // take_profit_atr
    }
}
