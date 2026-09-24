using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_KC_SBO_001_30</b> — BO su KC 30m, codice della ricerca <c>KC-30M-BO-68452f</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 131 trade, netto $158,821; broker (27/08/2025 → 09/09/2026)
/// 7 trade, netto $-14,022. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/KC-30M-BO-68452f.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $158,821 su 131 trade · $1,212 a trade · drawdown $23,187 · netto/DD 6.85 · anni in perdita 3 su 14 · notti a mercato 6.82 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $90,861 su 131 trade, drawdown $21,553.</para>
/// <para><b>Ordine STOP sul canale a 2 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso + 0.50 × ATR50</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso − 0.50 × ATR50</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale 2: 'H_d0 - O_d0 &gt; (H_d1 - O_d1) * 0.5'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale 2: 'O_d0 - L_d0 &gt; (O_d1 - L_d1) * 0.5'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 16:00 e 11:00 (a cavallo della mezzanotte), CET: ordini emessi sulle barre che chiudono fra le 16:00 e le 11:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 04:20-13:30, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 5 giornate (95 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 2.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 5.50 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_KC_SBO_001_30 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT5DAV_KC_SBO_001_30";

    public override string Description => "BO KC 30m, ricerca PT5DAV KC-30M-BO-68452f";

    public override string Symbol => "@KC";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "KC-30M-BO-68452f";

    public PT5DAV_KC_SBO_001_30()
    {
        TradingWindow = ResearchWindow(16, 11); // start_hour 16, end_hour 11, verbatim
        Sessions = 2;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0.5m;                       // breakout_offset_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 2;                     // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 5 giornate)
        MaxBars = 95;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 2.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 5.5m;                       // take_profit_atr
    }
}
