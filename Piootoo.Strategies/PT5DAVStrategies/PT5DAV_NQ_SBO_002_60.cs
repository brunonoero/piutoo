using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_NQ_SBO_002_60</b> — BO su NQ 1h, codice della ricerca <c>NQ-1H-BO-33a555</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 404 trade, netto $351,584; broker (27/08/2025 → 09/09/2026)
/// 26 trade, netto $-16,607. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/NQ-1H-BO-33a555.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $351,584 su 404 trade · $870 a trade · drawdown $62,892 · netto/DD 5.59 · anni in perdita 4 su 14 · notti a mercato 1.35 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $149,464 su 404 trade, drawdown $14,280.</para>
/// <para><b>Ordine STOP sul canale a 2 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Filtro comune a long e short</para>
/// <para>· deve essere VERO — neutrale 24: '|O_d5-C_d1| &lt; 0.25 * (HH5-LL5)'</para>
/// <para>· deve essere FALSO — neutrale 17: '|O_d5-C_d1| &gt; 0.5 * (H_d5-L_d1)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 17:00 e 03:00 (a cavallo della mezzanotte), CET: ordini emessi sulle barre che chiudono fra le 17:00 e le 03:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 3 giornate (69 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_NQ_SBO_002_60 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT5DAV_NQ_SBO_002_60";

    public override string Description => "BO NQ 1h, ricerca PT5DAV NQ-1H-BO-33a555";

    public override string Symbol => "@NQ";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "NQ-1H-BO-33a555";

    public PT5DAV_NQ_SBO_002_60()
    {
        TradingWindow = ResearchWindow(17, 3);  // start_hour 17, end_hour 3, verbatim
        Sessions = 2;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0m;                         // breakout_offset_atr
        NeutralYes = 24;                        // ptn_neut_yes
        NeutralNo = 17;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 3 giornate)
        MaxBars = 69;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
