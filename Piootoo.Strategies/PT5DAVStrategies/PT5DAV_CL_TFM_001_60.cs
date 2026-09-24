using Piootoo.Strategies.PT5DAVStrategies.Engines;

namespace Piootoo.Strategies.PT5DAVStrategies;

/// <summary>
/// <b>PT5DAV_CL_TFM_001_60</b> — TF_M su CL 1h, codice della ricerca <c>CL-1H-TFM-3865b4</c>
/// (consegna PT5DAV v5.0 del 23/09/2026, <c>piootoo-repository/PT5DAV/</c>).
///
/// <para>Parametri riportati verbatim da <c>strategie_224.csv</c>; le regole comuni (ATR50, sessione
/// della ricerca, orari del CFD) stanno in <see cref="Pt5DavEngineBase"/>. Numeri della ricerca sul
/// CFD: storia 87 trade, netto $92,261; broker (27/08/2025 → 09/09/2026)
/// 14 trade, netto $15,344. Trade di riferimento:
/// <c>PT5DAV/trades_per_strategia/CL-1H-TFM-3865b4.csv</c>.</para>
///
/// <para><b>Dalla scheda della ricerca</b> (<c>schede_224.md</c>):</para>
/// <para>*Trend following simmetrico.* Compra quando il prezzo sale sopra il massimo di ieri, vende quando scende sotto il minimo di ieri. L'idea e' che chi rompe l'estremo del giorno prima continui nella stessa direzione.</para>
/// <para>Da sapere. Long e short usano lo stesso pattern a specchio: non si possono filtrare in modo indipendente. Se serve, il motore da usare e' TF_U.</para>
/// <para>Tutta la storia sul CFD (10/01/2012 → 30/05/2025; ogni trade riportato al prezzo di oggi, con spread, commissione e swap): netto $92,261 su 87 trade · $1,060 a trade · drawdown $15,237 · netto/DD 6.05 · anni in perdita 2 su 13 · notti a mercato 0.00 a trade.</para>
/// <para>Sul future (prezzi di allora, costi del future): $34,488 su 87 trade, drawdown $7,658.</para>
/// <para><b>Ordine STOP sugli estremi della sessione precedente</b></para>
/// <para>· LONG: stop buy a H_d1 (massimo della sessione precedente)</para>
/// <para>· SHORT: stop sell a L_d1 (minimo della sessione precedente)</para>
/// <para><b>Filtri pattern</b></para>
/// <para>Solo LONG</para>
/// <para>· deve essere VERO — direzionale -29: 'H_d0 &lt; H_d1 * (1 - 0.01)'</para>
/// <para>Solo SHORT</para>
/// <para>· deve essere VERO — direzionale -29: 'L_d0 &gt; L_d1 * (1 + 0.01)'</para>
/// <para><b>Quando puo' operare</b></para>
/// <para>· Nessun filtro orario: opera su tutte le 24 ore</para>
/// <para>· Al massimo una entrata per sessione e per direzione</para>
/// <para>· Non apre posizioni quando il CFD e' chiuso (orari misurati sul broker: aperto 18:05-16:50, America/New_York), ne' nelle prime 50 sessioni della storia (l'ATR50 non esiste ancora)</para>
/// <para>· Intraday: chiude all'ultima barra che finisce entro le 16:50 America/New_York (chiusura del CFD) e non apre da li' fino alla riapertura: nessuna notte a mercato, nessuno swap</para>
/// <para><b>Uscite</b></para>
/// <para>· Stop loss: 1.25 × ATR50 dal prezzo d'ingresso (ATR50 = media (Wilder) del range vero delle ultime 50 sessioni CHIUSE, ricalcolata a ogni sessione; vale quella della sessione d'ingresso per tutto il trade)</para>
/// <para>· Take profit: nessuno</para>
/// <para>· Nessuna uscita a tempo</para>
/// </summary>
public sealed class PT5DAV_CL_TFM_001_60 : Pt5DavTfMirroredEngine
{
    public override string Name => "PT5DAV_CL_TFM_001_60";

    public override string Description => "TF_M CL 1h, ricerca PT5DAV CL-1H-TFM-3865b4";

    public override string Symbol => "@CL";

    public override int TimeframeMinutes => 60;

    public override string ResearchCode => "CL-1H-TFM-3865b4";

    public PT5DAV_CL_TFM_001_60()
    {
        TradingWindow = ResearchWindow(-1, -1); // start_hour -1, end_hour -1, verbatim
        NeutralYes = 55;                        // ptn_neut_yes
        NeutralNo = 56;                         // ptn_neut_no
        DirectionalYes = -29;                   // ptn_dir_yes
        DirectionalNo = 53;                     // ptn_dir_no
        SkipDay = -1;                           // skip_day, pandas 0 = lunedi' (-1 = nessuno)
        IntradayOnly = true;                    // intraday_only 1 (intraday)
        MaxBars = 0;                            // max_bars (0 = nessun limite)
        StopAtr = 1.25m;                        // stop_atr: stop in ATR50 dal prezzo d'ingresso
        TargetAtr = 0m;                         // take_profit_atr (0 = nessun target)
    }
}
