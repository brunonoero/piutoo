using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_BTC_TFM_002_30</b> — TF_M su BTC 30m, codice della ricerca <c>BTC-30M-TFM-9efa9d</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 434 trade, netto $1,125,103; broker (27/08/2025 → 09/09/2026)
/// 114 trade, netto $-48,298. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/BTC-30M-TFM-9efa9d.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Trend following simmetrico.* Compra quando il prezzo sale sopra il massimo di ieri, vende quando scende sotto il minimo di ieri. L'idea e' che chi rompe l'estremo del giorno prima continui nella stessa direzione.</para>
/// <para>Da sapere. Long e short usano lo stesso pattern a specchio: non si possono filtrare in modo indipendente. Se serve, il motore da usare e' TF_U.</para>
/// <para>Tutta la storia sul CFD (26/12/2017 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $1,125,103 su 434 trade · $2,592 a trade · drawdown $179,090 · netto/DD 6.28 · anni in perdita 2 su 8 · notti a mercato 3.17 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $462,974 su 434 trade, drawdown $81,811.</para>
/// <para><b>Ordine STOP sugli estremi della sessione precedente</b></para>
/// <para>· LONG: stop buy a H_d1 (massimo della sessione precedente)</para>
/// <para>· SHORT: stop sell a L_d1 (minimo della sessione precedente)</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere FALSO — direzionale 31: 'L_d0 &gt; L_d1 * (1 + 0.02)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere FALSO — direzionale 31: 'H_d0 &lt; H_d1 * (1 - 0.02)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Non apre posizioni di lunedì</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 00:00-00:00, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Overnight: puo' restare aperta oltre la sessione, al massimo 10 giornate (460 barre); paga lo swap a ogni rollover (17:00 New York)</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 0.20 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// </summary>
public sealed class PT5DAV_BTC_TFM_002_30 : Pt5DavTfMirroredEngine
{
    public override string Name => "PT5DAV_BTC_TFM_002_30";

    public override string Description => "TF_M BTC 30m, ricerca PT5DAV BTC-30M-TFM-9efa9d";

    public override string Symbol => "@BTC";

    public override int TimeframeMinutes => 30;

    public override string ResearchCode => "BTC-30M-TFM-9efa9d";

    public PT5DAV_BTC_TFM_002_30()
    {
        TradingWindow = ResearchWindow(-1, -1); // start_hour -1, end_hour -1, verbatim
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = 52;                    // ptn_dir_yes
        DirectionalNo = 31;                     // ptn_dir_no
        SkipDay = 0;                            // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = false;                   // intraday_only 0 (overnight 10 giornate)
        MaxBars = 460;                          // max_bars (la giornata della ricerca in barre)
        StopAtr = 0.2m;                         // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
