using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_SBO_001_30</b> — BO su BTC 30m, codice della ricerca <c>BTC-30M-BO-284f81</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 416 trade, netto $715,379; broker (27/08/2025 → 09/09/2026)
/// 65 trade, netto $-11,702. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-30M-BO-284f81.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Rottura del canale di piu' sessioni.* Come il trend following, ma il livello non e' l'estremo di ieri: e' il massimo (o il minimo) delle ultime N sessioni complete. Serve una rottura piu' importante per entrare.</para>
/// <para>Da sapere. Con una sola sessione di canale e senza la sessione in corso, il BO diventa esattamente il TF_M. Non e' un difetto: e' il caso degenere.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $715,379 su 416 trade · $1,720 a trade · drawdown $190,904 · netto/DD 3.75 · anni in perdita 1 su 8 · notti a mercato 2.32 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $350,527 su 416 trade, drawdown $197,546.</para>
/// <para><b>Ordine STOP sul canale a 2 sessioni</b></para>
/// <para>· LONG: stop buy sul massimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para>· SHORT: stop sell sul minimo delle ultime 2 sessioni complete e del massimo/minimo della sessione corrente escludendo la barra in corso</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Nessun filtro pattern</para>
/// <para>· —: 'il motore entra su ogni segnale strutturale'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Opera solo fra 00:00 e 06:00, CET: ordini emessi sulle barre che chiudono fra le 00:00 e le 06:00 (estremi inclusi), cioè attivi da quell'ora in poi</para>
/// <para>· Non apre posizioni di venerdì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 2 giornate (92 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.60 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: 4.00 × ATR50 dal prezzo d'ingresso</para>
/// </summary>
public sealed class PT5DAV_BTC_SBO_001_30 : Pt5DavSessionBreakoutEngine
{
    public override string Name => "PT5DAV_BTC_SBO_001_30";

    public override string Description => "BO BTC 30m, ricerca PT5DAV BTC-30M-BO-284f81";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "BTC-30M-BO-284f81";

    public PT5DAV_BTC_SBO_001_30()
    {
        TradingWindow = ResearchWindow(-1, 6);  // start_hour -1, end_hour 6, verbatim
        Sessions = 2;                           // n_sess
        IncludeCurrentSession = true;           // lev_include_sess0 1
        OffsetAtr = 0m;                         // breakout_offset_atr
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = 4;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 2 giornate)
        MaxBars = 92;                           // max_bars (la giornata della ricerca in barre)
        StopAtr = 1.6m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 4m;                         // take_profit_atr
    }
}
